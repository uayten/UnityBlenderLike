using UnityEditor;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Blender's middle mouse in the Scene view: drag to orbit around the pivot, Shift + drag to
    /// pan. Unity pans with a plain middle drag; this takes the middle button before the Scene view
    /// does. Other middle drags (with Alt or Ctrl) stay Unity's, and so do Alt + left drag orbit,
    /// right drag fly and the wheel.
    /// </summary>
    [InitializeOnLoad]
    internal static class MiddleMouseNavigation
    {
        private const int MiddleButton = 2;

        /// <summary>Orbit speed, the same as Unity's own Alt + left drag orbit.</summary>
        private const float OrbitRadiansPerPoint = 0.003f;

        private static readonly int ControlHint = "UnityBlenderLike.MiddleMouseNavigation".GetHashCode();

        private static bool panning;

        static MiddleMouseNavigation()
        {
            SceneView.beforeSceneGui += OnBeforeSceneGui;
        }

        private static void OnBeforeSceneGui(SceneView sceneView)
        {
            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(ControlHint, FocusType.Passive);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button != MiddleButton || e.alt || e.control || e.command || ModalTransform.IsActive
                        || !BlenderLikeSettings.MiddleMouseNavigation)
                        return;

                    // A rotation-locked or 2D view can't orbit; leave it to Unity's pan.
                    panning = e.shift;
                    if (!panning && (sceneView.isRotationLocked || sceneView.in2DMode))
                        return;

                    GUIUtility.hotControl = controlId;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlId)
                        return;
                    if (panning)
                        Pan(sceneView, e.delta);
                    else
                        Orbit(sceneView, e.delta);
                    e.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlId || e.button != MiddleButton)
                        return;
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
        }

        /// <summary>Turns around the world's up and the view's own right, like Unity's orbit.</summary>
        private static void Orbit(SceneView sceneView, Vector2 delta)
        {
            float degreesPerPoint = OrbitRadiansPerPoint * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(delta.x * degreesPerPoint, Vector3.up) * sceneView.rotation;
            rotation *= Quaternion.AngleAxis(delta.y * degreesPerPoint, Vector3.right);
            sceneView.rotation = rotation;
            sceneView.Repaint();
        }

        /// <summary>Moves the view so the scene follows the mouse at the pivot's depth.</summary>
        private static void Pan(SceneView sceneView, Vector2 delta)
        {
            Camera camera = sceneView.camera;
            float viewHeight = camera.pixelHeight / EditorGUIUtility.pixelsPerPoint;
            if (viewHeight <= 0f)
                return;

            float worldHeight = camera.orthographic
                ? camera.orthographicSize * 2f
                : 2f * Vector3.Distance(camera.transform.position, sceneView.pivot)
                    * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float worldPerPoint = worldHeight / viewHeight;

            // GUI y points down: dragging down moves the scene down, so the view goes up.
            Transform view = camera.transform;
            sceneView.pivot += (-view.right * delta.x + view.up * delta.y) * worldPerPoint;
            sceneView.Repaint();
        }
    }
}
