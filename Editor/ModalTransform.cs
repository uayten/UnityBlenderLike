using System.Globalization;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Blender-style modal transforms for the Scene View: G moves, R rotates and S scales the
    /// selection, following the mouse until a click confirms. Rotation follows the cursor angle
    /// around the pivot, full turns included (the built-in rotate gizmo projects the drag onto a
    /// fixed tangent, so it can't follow the mouse around the pivot).
    /// While transforming: G / R / S switch mode keeping what was done, X / Y / Z lock to a global
    /// axis (press again for local, again to free it), digits type an exact value, Ctrl snaps, left
    /// click or Enter confirms as one undo step, right click or Esc cancels. Global axes follow the
    /// convention in Preferences > Blender Like. Respects the Pivot / Center toggle.
    /// </summary>
    internal static class ModalTransform
    {
        public enum Mode { Move, Rotate, Scale }

        private enum AxisSpace { Free, Global, Local }

        private const float RotateSnapStep = 5f;
        private const float ScaleSnapStep = 0.1f;

        // Internal field the Shortcut Manager listens on. Putting our handler first lets the modal
        // swallow keys (X is Delete, digits switch views...) before any shortcut fires.
        private static readonly FieldInfo GlobalEventHandlerField = typeof(EditorApplication).GetField(
            "globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly EditorApplication.CallbackFunction KeyHandler = OnGlobalEvent;

        private static bool active;
        private static bool awaitingMouseUp;
        private static Mode mode;
        private static SceneView sceneView;
        private static Transform[] targets;
        private static int collapseUndoGroup = -1;

        // Where the selection was when the modal began: cancel returns here, Undo records from here.
        private static TransformState[] original;

        // Where the selection was when the current mode began: switching G / R / S keeps what the
        // previous mode did.
        private static TransformState[] start;

        private static Vector3 pivot;
        private static bool aroundCenter;
        private static Quaternion localAxesRotation;
        private static Vector3 viewForward;

        private static bool hasMouseStart;
        private static Vector2 mouseStart;
        private static Vector2 mousePosition;
        private static Vector2 pivotGui;
        private static Ray startRay;
        private static Ray currentRay;
        private static float scaleStartDistance;
        private static float lastMouseAngle;
        private static float accumulatedMouseAngle;
        private static bool snapping;

        private static AxisSpace axisSpace;
        private static int axisIndex;
        private static string typedValue = string.Empty;

        private static bool previousWantsMouseMove;
        private static bool previousToolsHidden;

        private struct TransformState
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 LocalScale;
        }

        [Shortcut("Blender Like/Move", typeof(SceneView), KeyCode.G)]
        private static void BeginMove(ShortcutArguments args) => Begin(Mode.Move, args.context as SceneView);

        [Shortcut("Blender Like/Rotate", typeof(SceneView), KeyCode.R)]
        private static void BeginRotate(ShortcutArguments args) => Begin(Mode.Rotate, args.context as SceneView);

        [Shortcut("Blender Like/Scale", typeof(SceneView), KeyCode.S)]
        private static void BeginScale(ShortcutArguments args) => Begin(Mode.Scale, args.context as SceneView);

        /// <param name="undoGroup">
        /// Undo group to merge the confirmed transform into (Shift+D passes the duplicate's), or -1.
        /// </param>
        public static void Begin(Mode newMode, SceneView view, int undoGroup = -1)
        {
            if (active || awaitingMouseUp)
                return;

            if (view == null)
                view = SceneView.lastActiveSceneView;
            Transform[] selected = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable);
            if (view == null || selected.Length == 0)
                return;

            sceneView = view;
            targets = selected;
            collapseUndoGroup = undoGroup;
            original = Capture();

            pivot = Tools.handlePosition;
            aroundCenter = Tools.pivotMode == PivotMode.Center;
            Transform reference = Selection.activeTransform != null ? Selection.activeTransform : targets[0];
            localAxesRotation = reference.rotation;
            viewForward = view.rotation * Vector3.forward;
            snapping = false;
            StartMode(newMode);

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

        private static void StartMode(Mode newMode)
        {
            mode = newMode;
            start = Capture();
            hasMouseStart = false;
            accumulatedMouseAngle = 0f;
            axisSpace = AxisSpace.Free;
            axisIndex = 0;
            typedValue = string.Empty;
        }

        private static void SwitchMode(Mode newMode)
        {
            if (newMode == mode)
                return;

            // Rotating and scaling leave the pivot in place; moving carries it along.
            if (mode == Mode.Move)
                pivot += MoveDelta();
            StartMode(newMode);
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

            pivotGui = HandleUtility.WorldToGUIPoint(pivot);
            if (!hasMouseStart)
            {
                mouseStart = e.mousePosition;
                mousePosition = mouseStart;
                startRay = HandleUtility.GUIPointToWorldRay(mouseStart);
                currentRay = startRay;
                scaleStartDistance = Vector2.Distance(mouseStart, pivotGui);
                lastMouseAngle = MouseAngle(mouseStart);
                hasMouseStart = true;
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
                    currentRay = HandleUtility.GUIPointToWorldRay(mousePosition);
                    snapping = e.control;
                    Apply();
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
                    Apply();
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

                case KeyCode.G: SwitchMode(Mode.Move); break;
                case KeyCode.R: SwitchMode(Mode.Rotate); break;
                case KeyCode.S: SwitchMode(Mode.Scale); break;

                case KeyCode.X: CycleAxis(0); break;
                case KeyCode.Y: CycleAxis(1); break;
                case KeyCode.Z: CycleAxis(2); break;

                case KeyCode.Minus:
                case KeyCode.KeypadMinus:
                    typedValue = typedValue.StartsWith("-") ? typedValue.Substring(1) : "-" + typedValue;
                    break;

                case KeyCode.Period:
                case KeyCode.Comma:
                case KeyCode.KeypadPeriod:
                    if (!typedValue.Contains("."))
                        typedValue += ".";
                    break;

                case KeyCode.Backspace:
                    if (typedValue.Length > 0)
                        typedValue = typedValue.Substring(0, typedValue.Length - 1);
                    break;

                default:
                    if (e.keyCode >= KeyCode.Alpha0 && e.keyCode <= KeyCode.Alpha9)
                        typedValue += (char)('0' + (e.keyCode - KeyCode.Alpha0));
                    else if (e.keyCode >= KeyCode.Keypad0 && e.keyCode <= KeyCode.Keypad9)
                        typedValue += (char)('0' + (e.keyCode - KeyCode.Keypad0));
                    break;
            }

            // Every other key is swallowed too, so no shortcut runs in the middle of the transform.
            e.Use();
            if (active)
            {
                Apply();
                sceneView.Repaint();
            }
        }

        private static void CycleAxis(int index)
        {
            if (axisSpace == AxisSpace.Free || axisIndex != index)
                axisSpace = AxisSpace.Global;
            else if (axisSpace == AxisSpace.Global)
                axisSpace = AxisSpace.Local;
            else
                axisSpace = AxisSpace.Free;
            axisIndex = index;
        }

        /// <summary>
        /// Direction of the locked axis in Unity's world. Local axes are the object's own, named
        /// by the same convention as the global ones, so with Blender axes local Z is the object's
        /// up, as the Z-up Transform Inspector shows it.
        /// </summary>
        private static Vector3 AxisDirection()
        {
            Vector3 direction = BlenderLikeSettings.GlobalDirection(axisIndex);
            return axisSpace == AxisSpace.Local ? localAxesRotation * direction : direction;
        }

        /// <summary>Unity's own axis for a component index of Transform.localScale.</summary>
        private static Vector3 UnityUnit(int index)
        {
            return index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;
        }

        private static bool TryGetTypedValue(out float value)
        {
            return float.TryParse(typedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static float MouseAngle(Vector2 guiPosition)
        {
            Vector2 offset = guiPosition - pivotGui;
            if (offset.sqrMagnitude < 1f)
                return lastMouseAngle;
            return Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        }

        // ---- Move ----

        private static Vector3 MoveDelta()
        {
            if (TryGetTypedValue(out float typed))
            {
                // Like Blender, a value typed with no axis locked moves along X.
                Vector3 direction = axisSpace == AxisSpace.Free
                    ? BlenderLikeSettings.GlobalDirection(0)
                    : AxisDirection();
                return direction * typed;
            }

            if (!hasMouseStart)
                return Vector3.zero;

            if (axisSpace != AxisSpace.Free)
            {
                Vector3 direction = AxisDirection();
                if (TryClosestOnAxis(startRay, direction, out float from) && TryClosestOnAxis(currentRay, direction, out float to))
                {
                    float distance = to - from;
                    if (snapping)
                        distance = Snap(distance, EditorSnapSettings.move.x);
                    return direction * distance;
                }
            }

            var plane = new Plane(viewForward, pivot);
            if (!plane.Raycast(startRay, out float startHit) || !plane.Raycast(currentRay, out float currentHit))
                return Vector3.zero;
            Vector3 delta = currentRay.GetPoint(currentHit) - startRay.GetPoint(startHit);
            if (axisSpace != AxisSpace.Free)
                delta = Vector3.Project(delta, AxisDirection());
            if (snapping)
            {
                Vector3 step = EditorSnapSettings.move;
                delta = new Vector3(Snap(delta.x, step.x), Snap(delta.y, step.y), Snap(delta.z, step.z));
            }
            return delta;
        }

        /// <summary>Point on the axis through the pivot closest to the ray, as a distance along the axis.</summary>
        private static bool TryClosestOnAxis(Ray ray, Vector3 axis, out float distance)
        {
            float b = Vector3.Dot(axis, ray.direction);
            float denominator = 1f - b * b;
            distance = 0f;
            if (denominator < 1e-4f)
                return false;

            Vector3 w = pivot - ray.origin;
            float d = Vector3.Dot(axis, w);
            float e = Vector3.Dot(ray.direction, w);
            distance = (b * e - d) / denominator;
            return true;
        }

        // ---- Rotate ----

        private static Vector3 RotationAxis()
        {
            switch (axisSpace)
            {
                case AxisSpace.Global: return BlenderLikeSettings.GlobalRotationAxis(axisIndex);
                case AxisSpace.Local: return localAxesRotation * BlenderLikeSettings.GlobalRotationAxis(axisIndex);
                default: return viewForward;
            }
        }

        /// <summary>
        /// Angle around <see cref="RotationAxis"/>, so a typed 90 turns like it does in Blender (or
        /// like the Inspector, with Unity axes). With the mouse, the selection turns the same way
        /// the cursor goes around the pivot.
        /// </summary>
        private static float RotationAngle()
        {
            if (TryGetTypedValue(out float typed))
                return typed;

            // GUI y points down, so a positive GUI angle is clockwise on screen; AngleAxis turns
            // counterclockwise as seen when looking along the axis.
            float screenAngle = -accumulatedMouseAngle;
            if (snapping)
                screenAngle = Snap(screenAngle, RotateSnapStep);
            return Vector3.Dot(RotationAxis(), viewForward) >= 0f ? screenAngle : -screenAngle;
        }

        // ---- Scale ----

        private static float ScaleFactor()
        {
            if (TryGetTypedValue(out float typed))
                return typed;
            if (!hasMouseStart || scaleStartDistance < 1f)
                return 1f;

            float factor = Vector2.Distance(mousePosition, pivotGui) / scaleStartDistance;
            return snapping ? Snap(factor, ScaleSnapStep) : factor;
        }

        /// <summary>
        /// Unity can't scale an object along an arbitrary world axis (that would shear it), so a
        /// locked scale acts on the object's local axis that runs closest to the locked one.
        /// </summary>
        private static Vector3 ScaledLocalScale(TransformState state, float factor)
        {
            if (axisSpace == AxisSpace.Free)
                return state.LocalScale * factor;

            Vector3 direction = AxisDirection();
            int closest = 0;
            float best = -1f;
            for (int i = 0; i < 3; i++)
            {
                float alignment = Mathf.Abs(Vector3.Dot(state.Rotation * UnityUnit(i), direction));
                if (alignment > best)
                {
                    best = alignment;
                    closest = i;
                }
            }

            Vector3 scale = state.LocalScale;
            scale[closest] *= factor;
            return scale;
        }

        private static Vector3 ScaledOffset(Vector3 offset, float factor)
        {
            if (axisSpace == AxisSpace.Free)
                return offset * factor;
            Vector3 direction = AxisDirection();
            return offset + direction * (Vector3.Dot(offset, direction) * (factor - 1f));
        }

        // ---- Applying ----

        private static float Snap(float value, float step)
        {
            return step > 0f ? Mathf.Round(value / step) * step : value;
        }

        private static TransformState[] Capture()
        {
            var states = new TransformState[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                    continue;
                states[i] = new TransformState
                {
                    Position = targets[i].position,
                    Rotation = targets[i].rotation,
                    LocalScale = targets[i].localScale,
                };
            }
            return states;
        }

        private static void Restore(TransformState[] states)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                    continue;
                targets[i].SetPositionAndRotation(states[i].Position, states[i].Rotation);
                targets[i].localScale = states[i].LocalScale;
            }
        }

        private static void Apply()
        {
            var states = new TransformState[targets.Length];
            switch (mode)
            {
                case Mode.Move:
                    Vector3 delta = MoveDelta();
                    for (int i = 0; i < states.Length; i++)
                    {
                        states[i] = start[i];
                        states[i].Position += delta;
                    }
                    break;

                case Mode.Rotate:
                    Quaternion rotation = Quaternion.AngleAxis(RotationAngle(), RotationAxis());
                    for (int i = 0; i < states.Length; i++)
                    {
                        states[i] = start[i];
                        states[i].Rotation = rotation * start[i].Rotation;
                        if (aroundCenter)
                            states[i].Position = pivot + rotation * (start[i].Position - pivot);
                    }
                    break;

                case Mode.Scale:
                    float factor = ScaleFactor();
                    for (int i = 0; i < states.Length; i++)
                    {
                        states[i] = start[i];
                        states[i].LocalScale = ScaledLocalScale(start[i], factor);
                        if (aroundCenter)
                            states[i].Position = pivot + ScaledOffset(start[i].Position - pivot, factor);
                    }
                    break;
            }
            Restore(states);
        }

        private static void Confirm()
        {
            if (!active)
                return;

            // Go back to the start so the whole modal becomes a single Undo step.
            TransformState[] final = Capture();
            Restore(original);
            Undo.RecordObjects(System.Array.FindAll(targets, t => t != null), mode.ToString());
            Restore(final);
            if (collapseUndoGroup >= 0)
                Undo.CollapseUndoOperations(collapseUndoGroup);
            End();
        }

        private static void Cancel()
        {
            if (!active)
                return;

            Restore(original);
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

        // ---- Overlay ----

        /// <summary>Unity's color for the world axis the locked line runs along.</summary>
        private static Color AxisColor(Vector3 direction)
        {
            if (Mathf.Abs(direction.x) > 0.5f)
                return Handles.xAxisColor;
            return Mathf.Abs(direction.y) > 0.5f ? Handles.yAxisColor : Handles.zAxisColor;
        }

        private static void DrawOverlay()
        {
            if (axisSpace != AxisSpace.Free)
            {
                Vector3 direction = AxisDirection();
                Handles.color = AxisColor(BlenderLikeSettings.GlobalDirection(axisIndex));
                float length = HandleUtility.GetHandleSize(pivot) * 1000f;
                Handles.DrawLine(pivot - direction * length, pivot + direction * length);
            }

            Handles.BeginGUI();
            if (mode != Mode.Move)
            {
                Handles.color = Color.white;
                Handles.DrawDottedLine(pivotGui, mousePosition, 4f);
            }

            string axisLabel = axisSpace == AxisSpace.Free
                ? (mode == Mode.Rotate ? "view" : "free")
                : "XYZ"[axisIndex] + (axisSpace == AxisSpace.Global ? " global" : " local");
            string valueLabel;
            if (typedValue.Length > 0)
                valueLabel = "[" + typedValue + "]";
            else if (mode == Mode.Move)
                valueLabel = MoveDelta().magnitude.ToString("0.###", CultureInfo.InvariantCulture) + " m";
            else if (mode == Mode.Rotate)
                valueLabel = RotationAngle().ToString("0.#", CultureInfo.InvariantCulture) + "°";
            else
                valueLabel = ScaleFactor().ToString("0.###", CultureInfo.InvariantCulture);

            string text = mode + " " + valueLabel + "   axis: " + axisLabel
                + "      G/R/S mode · X/Y/Z axis · digits value · Ctrl snap · Enter/click confirm · Esc/right click cancel";

            float height = sceneView.camera.pixelHeight / EditorGUIUtility.pixelsPerPoint;
            var rect = new Rect(8f, height - 30f, 780f, 22f);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, rect.height - 4f), text, EditorStyles.boldLabel);
            Handles.EndGUI();
        }
    }
}
