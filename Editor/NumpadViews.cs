using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Blender's numpad for the Scene View: 1 / 3 / 7 for front, right and top, Ctrl for the
    /// opposite side, 5 to toggle orthographic, . to frame the selection, / for local view. The
    /// views keep the current pivot and zoom, follow the axis convention in Preferences > Blender
    /// Like, and go back to perspective when orbited (Auto Perspective).
    /// </summary>
    [InitializeOnLoad]
    internal static class NumpadViews
    {
        // The Scene view's rotation and projection animate toward the view after a key; they're
        // internal, and only read to tell that animation apart from an orbit.
        private static readonly FieldInfo RotationField = typeof(SceneView).GetField(
            "m_Rotation", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo OrthographicField = typeof(SceneView).GetField(
            "m_Ortho", BindingFlags.Instance | BindingFlags.NonPublic);

        private const double FallbackAnimationSeconds = 1.0;

        /// <summary>Views made orthographic by a view key, with the rotation that key set.</summary>
        private static readonly Dictionary<SceneView, AutoOrthographicView> AutoOrthographicViews =
            new Dictionary<SceneView, AutoOrthographicView>();

        /// <summary>Where each view was before local view, to go back there on leaving it.</summary>
        private static readonly Dictionary<SceneView, SavedView> ViewsBeforeLocalView =
            new Dictionary<SceneView, SavedView>();

        private struct AutoOrthographicView
        {
            public Quaternion Rotation;
            public double Time;
        }

        private struct SavedView
        {
            public Vector3 Pivot;
            public Quaternion Rotation;
            public float Size;
            public bool Orthographic;
        }

        static NumpadViews()
        {
            SceneView.duringSceneGui += ApplyAutoPerspective;
        }

        [Shortcut("Blender Like/Front View", typeof(SceneView), KeyCode.Keypad1)]
        private static void Front(ShortcutArguments args) => SetView(args, Quaternion.identity);

        [Shortcut("Blender Like/Back View", typeof(SceneView), KeyCode.Keypad1, ShortcutModifiers.Action)]
        private static void Back(ShortcutArguments args) => SetView(args, Quaternion.Euler(0f, 180f, 0f));

        [Shortcut("Blender Like/Right View", typeof(SceneView), KeyCode.Keypad3)]
        private static void Right(ShortcutArguments args) => SetView(args, Quaternion.Euler(0f, -90f, 0f));

        [Shortcut("Blender Like/Left View", typeof(SceneView), KeyCode.Keypad3, ShortcutModifiers.Action)]
        private static void Left(ShortcutArguments args) => SetView(args, Quaternion.Euler(0f, 90f, 0f));

        [Shortcut("Blender Like/Top View", typeof(SceneView), KeyCode.Keypad7)]
        private static void Top(ShortcutArguments args) => SetView(args, Quaternion.Euler(90f, 0f, 0f));

        [Shortcut("Blender Like/Bottom View", typeof(SceneView), KeyCode.Keypad7, ShortcutModifiers.Action)]
        private static void Bottom(ShortcutArguments args) => SetView(args, Quaternion.Euler(-90f, 0f, 0f));

        [Shortcut("Blender Like/Toggle Orthographic", typeof(SceneView), KeyCode.Keypad5)]
        private static void ToggleOrthographic(ShortcutArguments args)
        {
            SceneView sceneView = GetSceneView(args);
            if (sceneView == null)
                return;

            // A view made orthographic here stays orthographic while orbiting, as in Blender.
            AutoOrthographicViews.Remove(sceneView);
            sceneView.orthographic = !sceneView.orthographic;
        }

        [Shortcut("Blender Like/Frame Selected", typeof(SceneView), KeyCode.KeypadPeriod)]
        private static void FrameSelected(ShortcutArguments args)
        {
            SceneView sceneView = GetSceneView(args);
            if (sceneView != null)
                sceneView.FrameSelected();
        }

        /// <summary>
        /// Blender's local view: only the selection shows, framed. Again to show everything and go
        /// back to the view from before. Uses Unity's isolation, which isn't saved in the scene.
        /// </summary>
        [Shortcut("Blender Like/Local View", typeof(SceneView), KeyCode.KeypadDivide)]
        private static void LocalView(ShortcutArguments args)
        {
            SceneView sceneView = GetSceneView(args);
            if (sceneView == null)
                return;

            SceneVisibilityManager visibility = SceneVisibilityManager.instance;
            if (visibility.IsCurrentStageIsolated())
            {
                visibility.ExitIsolation();
                if (ViewsBeforeLocalView.TryGetValue(sceneView, out SavedView saved))
                {
                    sceneView.LookAt(saved.Pivot, saved.Rotation, saved.Size, saved.Orthographic);
                    ViewsBeforeLocalView.Remove(sceneView);
                }
                return;
            }

            GameObject[] selected = Selection.gameObjects;
            if (selected.Length == 0)
                return;

            ViewsBeforeLocalView[sceneView] = new SavedView
            {
                Pivot = sceneView.pivot,
                Rotation = sceneView.rotation,
                Size = sceneView.size,
                Orthographic = sceneView.orthographic,
            };
            visibility.Isolate(selected, true);
            sceneView.FrameSelected();
        }

        /// <param name="rotation">The view in Unity's axes; the configured yaw is applied on top.</param>
        private static void SetView(ShortcutArguments args, Quaternion rotation)
        {
            SceneView sceneView = GetSceneView(args);
            if (sceneView == null)
                return;

            Quaternion viewRotation = BlenderLikeSettings.ViewYaw * rotation;
            if (BlenderLikeSettings.AutoPerspective && !sceneView.orthographic)
            {
                AutoOrthographicViews[sceneView] = new AutoOrthographicView
                {
                    Rotation = viewRotation,
                    Time = EditorApplication.timeSinceStartup,
                };
            }
            else if (AutoOrthographicViews.TryGetValue(sceneView, out AutoOrthographicView view))
            {
                // Already orthographic from a view key: keep going back to perspective on orbit.
                view.Rotation = viewRotation;
                view.Time = EditorApplication.timeSinceStartup;
                AutoOrthographicViews[sceneView] = view;
            }
            sceneView.LookAt(sceneView.pivot, viewRotation, sceneView.size, true);
        }

        /// <summary>Turns a view back to perspective once it's orbited away from the view key's rotation.</summary>
        private static void ApplyAutoPerspective(SceneView sceneView)
        {
            if (!AutoOrthographicViews.TryGetValue(sceneView, out AutoOrthographicView view))
                return;

            if (!sceneView.orthographic && !IsAnimating(sceneView, view))
            {
                // Made perspective some other way (Unity's gizmo, Numpad 5).
                AutoOrthographicViews.Remove(sceneView);
                return;
            }

            if (IsAnimating(sceneView, view) || Quaternion.Angle(sceneView.rotation, view.Rotation) < 0.5f)
                return;

            AutoOrthographicViews.Remove(sceneView);
            sceneView.orthographic = false;
        }

        private static bool IsAnimating(SceneView sceneView, AutoOrthographicView view)
        {
            if (RotationField?.GetValue(sceneView) is AnimQuaternion rotation
                && OrthographicField?.GetValue(sceneView) is AnimBool orthographic)
                return rotation.isAnimating || orthographic.isAnimating;
            return EditorApplication.timeSinceStartup - view.Time < FallbackAnimationSeconds;
        }

        private static SceneView GetSceneView(ShortcutArguments args)
        {
            return args.context as SceneView ?? SceneView.lastActiveSceneView;
        }
    }
}
