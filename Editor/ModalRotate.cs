using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Blender-style modal rotation for the Scene View. Press R, move the mouse around the pivot and
    /// the selection follows the cursor angle, full turns included (the built-in rotate gizmo projects
    /// the drag onto a fixed tangent, so it can't follow the mouse around the pivot).
    /// While rotating: X/Y/Z lock to a global axis (press again for local, again to free it), digits
    /// type an exact angle, Ctrl snaps to 5 degrees, left click or Enter confirms, right click or Esc
    /// cancels. Global axes follow the convention in Preferences > Blender Like. Respects the
    /// Pivot/Center toggle. Rebindable in Edit > Shortcuts.
    /// </summary>
    internal static class ModalRotate
    {
        private const string ShortcutId = "Blender Like/Rotate";
        private const float SnapStep = 5f;

        private enum AxisSpace { View, Global, Local }

        // Internal field the Shortcut Manager listens on. Putting our handler first lets the modal
        // swallow keys (X is Delete, digits switch views...) before any shortcut fires.
        private static readonly FieldInfo GlobalEventHandlerField = typeof(EditorApplication).GetField(
            "globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly EditorApplication.CallbackFunction KeyHandler = OnGlobalEvent;

        private static bool active;
        private static bool awaitingMouseUp;
        private static SceneView sceneView;
        private static Transform[] targets;
        private static Vector3[] startPositions;
        private static Quaternion[] startRotations;
        private static Vector3 pivot;
        private static bool aroundCenter;
        private static Quaternion localAxesRotation;
        private static Vector3 viewForward;
        private static bool hasMouseAngle;
        private static float lastMouseAngle;
        private static float accumulatedMouseAngle;
        private static Vector2 mousePosition;
        private static bool snapping;
        private static AxisSpace axisSpace;
        private static int axisIndex;
        private static string typedAngle = string.Empty;
        private static bool previousWantsMouseMove;
        private static bool previousToolsHidden;

        [Shortcut(ShortcutId, typeof(SceneView), KeyCode.R)]
        private static void Begin(ShortcutArguments args)
        {
            if (active || awaitingMouseUp)
                return;

            SceneView view = args.context as SceneView ?? SceneView.lastActiveSceneView;
            Transform[] selected = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);
            if (view == null || selected.Length == 0)
                return;

            sceneView = view;
            targets = selected;
            startPositions = new Vector3[targets.Length];
            startRotations = new Quaternion[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                startPositions[i] = targets[i].position;
                startRotations[i] = targets[i].rotation;
            }

            pivot = Tools.handlePosition;
            aroundCenter = Tools.pivotMode == PivotMode.Center;
            Transform reference = Selection.activeTransform != null ? Selection.activeTransform : targets[0];
            localAxesRotation = reference.rotation;
            viewForward = view.rotation * Vector3.forward;

            hasMouseAngle = false;
            accumulatedMouseAngle = 0f;
            snapping = false;
            axisSpace = AxisSpace.View;
            axisIndex = 0;
            typedAngle = string.Empty;

            previousWantsMouseMove = view.wantsMouseMove;
            view.wantsMouseMove = true;
            previousToolsHidden = Tools.hidden;
            Tools.hidden = true;

            HookKeys();
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload += Cancel;
            active = true;
            view.Repaint();
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (view != sceneView)
                return;

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);

            if (awaitingMouseUp)
            {
                // Swallow the release of the click that ended the modal, so it doesn't select
                // something or open the context menu.
                if (e.type == EventType.MouseUp || e.type == EventType.ContextClick)
                {
                    e.Use();
                    if (e.type == EventType.MouseUp)
                        Unsubscribe();
                }
                else if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    e.Use();
                }
                return;
            }

            if (!active)
                return;

            if (!hasMouseAngle)
            {
                mousePosition = e.mousePosition;
                lastMouseAngle = MouseAngle(mousePosition);
                hasMouseAngle = true;
            }

            switch (e.type)
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;

                case EventType.MouseMove:
                case EventType.MouseDrag:
                    float angle = MouseAngle(e.mousePosition);
                    accumulatedMouseAngle += Mathf.DeltaAngle(lastMouseAngle, angle);
                    lastMouseAngle = angle;
                    mousePosition = e.mousePosition;
                    snapping = e.control;
                    Apply(CurrentRotation());
                    e.Use();
                    break;

                case EventType.MouseDown:
                    if (e.button == 0)
                        Confirm();
                    else
                        Cancel();
                    awaitingMouseUp = true;
                    e.Use();
                    break;

                case EventType.ContextClick:
                    e.Use();
                    break;

                case EventType.Repaint:
                    DrawOverlay();
                    break;
            }
        }

        private static void OnGlobalEvent()
        {
            Event e = Event.current;
            if (!active || e == null || e.type != EventType.KeyDown)
                return;

            switch (e.keyCode)
            {
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                case KeyCode.LeftCommand:
                case KeyCode.RightCommand:
                    snapping = e.control;
                    Apply(CurrentRotation());
                    sceneView.Repaint();
                    return;

                case KeyCode.Escape:
                    Cancel();
                    Unsubscribe();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Confirm();
                    Unsubscribe();
                    break;

                case KeyCode.X: CycleAxis(0); break;
                case KeyCode.Y: CycleAxis(1); break;
                case KeyCode.Z: CycleAxis(2); break;

                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    typedAngle = typedAngle.StartsWith("-") ? typedAngle.Substring(1) : "-" + typedAngle;
                    break;

                case KeyCode.Period:
                case KeyCode.Comma:
                case KeyCode.KeypadPeriod:
                    if (!typedAngle.Contains("."))
                        typedAngle += ".";
                    break;

                case KeyCode.Backspace:
                    if (typedAngle.Length > 0)
                        typedAngle = typedAngle.Substring(0, typedAngle.Length - 1);
                    break;

                default:
                    if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
                        typedAngle += (char)('0' + (e.keyCode - KeyCode.Alpha0));
                    else if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
                        typedAngle += (char)('0' + (e.keyCode - KeyCode.Keypad0));
                    break;
            }

            // Every other key is swallowed too, so no shortcut runs in the middle of the rotation.
            e.Use();
            if (active)
            {
                Apply(CurrentRotation());
                sceneView.Repaint();
            }
        }

        private static void CycleAxis(int index)
        {
            if (axisSpace == AxisSpace.View || axisIndex != index)
                axisSpace = AxisSpace.Global;
            else if (axisSpace == AxisSpace.Global)
                axisSpace = AxisSpace.Local;
            else
                axisSpace = AxisSpace.View;
            axisIndex = index;
        }

        private static Vector3 Axis()
        {
            switch (axisSpace)
            {
                case AxisSpace.Global: return BlenderLikeSettings.GlobalAxis(axisIndex);
                case AxisSpace.Local: return localAxesRotation * LocalUnit(axisIndex);
                default: return viewForward;
            }
        }

        /// <summary>Local axes are the object's own, named as Unity names them.</summary>
        private static Vector3 LocalUnit(int index)
        {
            return index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;
        }

        /// <summary>Unity's color for the axis the locked line runs along.</summary>
        private static Color AxisColor()
        {
            Vector3 unit = axisSpace == AxisSpace.Global
                ? BlenderLikeSettings.GlobalAxis(axisIndex)
                : LocalUnit(axisIndex);
            if (Mathf.Abs(unit.x) > 0.5f)
                return Handles.xAxisColor;
            return Mathf.Abs(unit.y) > 0.5f ? Handles.yAxisColor : Handles.zAxisColor;
        }

        /// <summary>
        /// Angle around <see cref="Axis"/> in Unity's convention, so a typed 90 on Y adds 90 to the
        /// Inspector's Y. With the mouse, the selection turns the same way the cursor goes around.
        /// </summary>
        private static float CurrentAngle()
        {
            if (TryGetTypedAngle(out float typed))
                return typed;

            // GUI y points down, so a positive GUI angle is clockwise on screen; AngleAxis turns
            // counterclockwise as seen when looking along the axis.
            float screenAngle = -accumulatedMouseAngle;
            if (snapping)
                screenAngle = Mathf.Round(screenAngle / SnapStep) * SnapStep;
            return Vector3.Dot(Axis(), viewForward) >= 0f ? screenAngle : -screenAngle;
        }

        private static bool TryGetTypedAngle(out float angle)
        {
            return float.TryParse(typedAngle, NumberStyles.Float, CultureInfo.InvariantCulture, out angle);
        }

        private static Quaternion CurrentRotation()
        {
            return Quaternion.AngleAxis(CurrentAngle(), Axis());
        }

        private static float MouseAngle(Vector2 guiPosition)
        {
            Vector2 offset = guiPosition - HandleUtility.WorldToGUIPoint(pivot);
            if (offset.sqrMagnitude < 1f)
                return lastMouseAngle;
            return Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        }

        private static void Apply(Quaternion rotation)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                    continue;
                targets[i].rotation = rotation * startRotations[i];
                targets[i].position = aroundCenter
                    ? pivot + rotation * (startPositions[i] - pivot)
                    : startPositions[i];
            }
        }

        private static void Confirm()
        {
            if (!active)
                return;

            // Go back to the start so the whole drag becomes a single Undo step.
            Quaternion rotation = CurrentRotation();
            Apply(Quaternion.identity);
            Undo.RecordObjects(System.Array.FindAll(targets, t => t != null), "Rotate");
            Apply(rotation);
            End();
        }

        private static void Cancel()
        {
            if (!active)
                return;

            Apply(Quaternion.identity);
            End();
        }

        private static void End()
        {
            active = false;
            UnhookKeys();
            AssemblyReloadEvents.beforeAssemblyReload -= Cancel;
            if (sceneView != null)
            {
                sceneView.wantsMouseMove = previousWantsMouseMove;
                sceneView.Repaint();
            }
            Tools.hidden = previousToolsHidden;
        }

        private static void Unsubscribe()
        {
            awaitingMouseUp = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private static void HookKeys()
        {
            if (GlobalEventHandlerField == null)
                return;
            var current = (EditorApplication.CallbackFunction)GlobalEventHandlerField.GetValue(null);
            GlobalEventHandlerField.SetValue(null, KeyHandler + (current - KeyHandler));
        }

        private static void UnhookKeys()
        {
            if (GlobalEventHandlerField == null)
                return;
            var current = (EditorApplication.CallbackFunction)GlobalEventHandlerField.GetValue(null);
            GlobalEventHandlerField.SetValue(null, current - KeyHandler);
        }

        private static void DrawOverlay()
        {
            if (axisSpace != AxisSpace.View)
            {
                Handles.color = AxisColor();
                Vector3 axis = Axis();
                float length = HandleUtility.GetHandleSize(pivot) * 1000f;
                Handles.DrawLine(pivot - axis * length, pivot + axis * length);
            }

            Handles.BeginGUI();
            Handles.color = Color.white;
            Handles.DrawDottedLine(HandleUtility.WorldToGUIPoint(pivot), mousePosition, 4f);

            string axisLabel = axisSpace == AxisSpace.View
                ? "view"
                : "XYZ"[axisIndex] + (axisSpace == AxisSpace.Global ? " global" : " local");
            string angleLabel = typedAngle.Length > 0
                ? "[" + typedAngle + "]"
                : CurrentAngle().ToString("0.#", CultureInfo.InvariantCulture);
            string text = "Rotate " + angleLabel + "°   axis: " + axisLabel
                + "      X/Y/Z axis · digits angle · Ctrl snap · Enter/click confirm · Esc/right click cancel";

            float height = sceneView.camera.pixelHeight / EditorGUIUtility.pixelsPerPoint;
            var rect = new Rect(8f, height - 30f, 720f, 22f);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, rect.height - 4f), text, EditorStyles.boldLabel);
            Handles.EndGUI();
        }
    }
}
