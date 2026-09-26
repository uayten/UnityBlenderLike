using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Alt + drag on the move gizmo duplicates the selection and moves the copy, leaving the
    /// original in place, like Unreal. The selection is duplicated when the handle is pressed, before
    /// Unity's move tool records what it drags, so the drag carries the copy. A press released
    /// without dragging takes the copy back. Duplicate and move undo as one step.
    /// </summary>
    [InitializeOnLoad]
    internal static class AltDragDuplicate
    {
        // Distance from the mouse to the nearest handle, measured by Unity in the last Layout pass.
        // The Scene view's click-to-select control always sits at the pick distance, so anything
        // closer is a handle under the mouse.
        private static readonly FieldInfo NearestDistanceField = typeof(HandleUtility).GetField(
            "s_NearestDistance", BindingFlags.Static | BindingFlags.NonPublic);

        private const float PickDistance = 5f;

        private static bool pressed;
        private static bool dragged;
        private static int undoGroup;
        private static Object[] originals;

        static AltDragDuplicate()
        {
            SceneView.beforeSceneGui += OnBeforeSceneGui;
        }

        private static void OnBeforeSceneGui(SceneView sceneView)
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (!ShouldDuplicate(e))
                        return;

                    originals = Selection.objects;
                    Undo.IncrementCurrentGroup();
                    undoGroup = Undo.GetCurrentGroup();
                    if (!EditorApplication.ExecuteMenuItem("Edit/Duplicate"))
                        return;

                    pressed = true;
                    dragged = false;
                    // Without Alt the click goes to the handle, not to Unity's Alt + drag orbit.
                    e.modifiers &= ~EventModifiers.Alt;
                    break;

                case EventType.MouseDrag:
                    if (!pressed)
                        return;
                    dragged = true;
                    e.modifiers &= ~EventModifiers.Alt;
                    break;

                case EventType.MouseUp:
                    if (!pressed || e.button != 0)
                        return;
                    pressed = false;
                    e.modifiers &= ~EventModifiers.Alt;

                    // After the move tool has finished with this release and recorded its undo.
                    int group = undoGroup;
                    Object[] previousSelection = originals;
                    if (dragged)
                    {
                        EditorApplication.delayCall += () =>
                        {
                            Undo.SetCurrentGroupName("Duplicate and Move");
                            Undo.CollapseUndoOperations(group);
                        };
                    }
                    else
                    {
                        EditorApplication.delayCall += () =>
                        {
                            Undo.RevertAllDownToGroup(group);
                            Selection.objects = previousSelection;
                        };
                    }
                    break;
            }
        }

        private static bool ShouldDuplicate(Event e)
        {
            if (e.button != 0 || !e.alt || e.control || e.shift || e.command)
                return false;
            if (!BlenderLikeSettings.AltDragDuplicate || ModalTransform.IsActive)
                return false;
            if (Tools.current != Tool.Move && Tools.current != Tool.Transform)
                return false;
            if (Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable).Length == 0)
                return false;
            return IsOverHandle();
        }

        private static bool IsOverHandle()
        {
            if (HandleUtility.nearestControl == 0 || NearestDistanceField == null)
                return false;
            return (float)NearestDistanceField.GetValue(null) < PickDistance;
        }
    }
}
