using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityBlenderLike
{
    /// <summary>
    /// Like Blender, where keys go to the area under the mouse: with the mouse over a Scene view,
    /// a key pressed while another window has the keyboard focus (after clicking in the Hierarchy,
    /// say) goes to the Scene view instead. The Scene view takes the focus and gets the key, so
    /// every Scene view shortcut answers: this package's, Unity's and the project's own. Modifier
    /// keys alone and typing in a text field are left alone.
    /// </summary>
    [InitializeOnLoad]
    internal static class KeysFollowMouse
    {
        // Internal field the Shortcut Manager listens on; this handler goes first so the focused
        // window never sees the key.
        private static readonly FieldInfo GlobalEventHandlerField = typeof(EditorApplication).GetField(
            "globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly EditorApplication.CallbackFunction Handler = OnGlobalEvent;

        /// <summary>
        /// A key that types a character comes as two KeyDown events: the key, then the character.
        /// After sending the key on, the character is swallowed too, so the window that had the
        /// focus doesn't type it (the Hierarchy would jump to a matching name).
        /// </summary>
        private static bool swallowCharacter;

        static KeysFollowMouse()
        {
            if (GlobalEventHandlerField == null)
                return;

            // Wait a frame, so the Shortcut Manager has registered its own handler to go ahead of.
            EditorApplication.delayCall += () =>
            {
                var current = (EditorApplication.CallbackFunction)GlobalEventHandlerField.GetValue(null);
                GlobalEventHandlerField.SetValue(null, Handler + (current - Handler));
            };
        }

        private static void OnGlobalEvent()
        {
            Event e = Event.current;
            if (e == null || e.type != EventType.KeyDown)
                return;

            if (e.keyCode == KeyCode.None)
            {
                if (swallowCharacter)
                    e.Use();
                swallowCharacter = false;
                return;
            }
            swallowCharacter = false;

            if (!(EditorWindow.mouseOverWindow is SceneView sceneView) || EditorWindow.focusedWindow == sceneView)
                return;
            if (IsModifier(e.keyCode) || IsTyping(EditorWindow.focusedWindow))
                return;

            SendTo(sceneView, e);
        }

        /// <summary>Gives the Scene view the focus and the key, and keeps the key from the window that had it.</summary>
        internal static void SendTo(SceneView sceneView, Event e)
        {
            var key = new Event(e);
            e.Use();
            swallowCharacter = true;
            sceneView.Focus();
            sceneView.SendEvent(key);
        }

        private static bool IsModifier(KeyCode keyCode)
        {
            switch (keyCode)
            {
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                case KeyCode.AltGr:
                case KeyCode.LeftCommand:
                case KeyCode.RightCommand:
                case KeyCode.LeftWindows:
                case KeyCode.RightWindows:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsTyping(EditorWindow focusedWindow)
        {
            if (EditorGUIUtility.editingTextField)
                return true;
            Focusable focused = focusedWindow != null ? focusedWindow.rootVisualElement.panel?.focusController?.focusedElement : null;
            return focused is VisualElement element
                && (element is TextField || element.GetFirstAncestorOfType<TextField>() != null);
        }
    }
}
