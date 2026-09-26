using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityBlenderLike
{
    /// <summary>
    /// Like Blender, where keys go to the area under the mouse: with the mouse over a Scene view,
    /// the package's shortcuts act there even while another window has the keyboard focus (after
    /// clicking something in the Hierarchy, say). The Scene view takes the focus and the shortcut
    /// runs. Typing in a text field is left alone.
    /// </summary>
    [InitializeOnLoad]
    internal static class KeysFollowMouse
    {
        private const string ShortcutPrefix = "Blender Like/";

        // Internal field the Shortcut Manager listens on; this handler goes first so the key
        // reaches the Scene view before the focused window's shortcuts.
        private static readonly FieldInfo GlobalEventHandlerField = typeof(EditorApplication).GetField(
            "globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly EditorApplication.CallbackFunction Handler = OnGlobalEvent;

        /// <summary>The package's Scene view shortcuts, by ID, found through their attributes.</summary>
        private static readonly Dictionary<string, MethodInfo> Shortcuts = FindShortcuts();

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
            if (e == null || e.type != EventType.KeyDown || e.keyCode == KeyCode.None)
                return;
            if (!(EditorWindow.mouseOverWindow is SceneView sceneView) || EditorWindow.focusedWindow == sceneView)
                return;
            if (IsTyping(EditorWindow.focusedWindow))
                return;
            if (!TryFindShortcut(e, out MethodInfo shortcut))
                return;

            sceneView.Focus();
            object[] arguments = shortcut.GetParameters().Length == 0
                ? null
                : new object[] { new ShortcutArguments { context = sceneView, stage = ShortcutStage.End } };
            shortcut.Invoke(null, arguments);
            e.Use();
        }

        private static bool IsTyping(EditorWindow focusedWindow)
        {
            if (EditorGUIUtility.editingTextField)
                return true;
            Focusable focused = focusedWindow != null ? focusedWindow.rootVisualElement.panel?.focusController?.focusedElement : null;
            return focused is VisualElement element
                && (element is TextField || element.GetFirstAncestorOfType<TextField>() != null);
        }

        private static bool TryFindShortcut(Event e, out MethodInfo shortcut)
        {
            ShortcutModifiers modifiers = ShortcutModifiers.None;
            if (e.alt)
                modifiers |= ShortcutModifiers.Alt;
            if (e.shift)
                modifiers |= ShortcutModifiers.Shift;
            if (Application.platform == RuntimePlatform.OSXEditor ? e.command : e.control)
                modifiers |= ShortcutModifiers.Action;

            foreach (KeyValuePair<string, MethodInfo> entry in Shortcuts)
            {
                // Rebinding in Edit > Shortcuts is respected: the current binding is what matches.
                foreach (KeyCombination combination in ShortcutManager.instance.GetShortcutBinding(entry.Key).keyCombinationSequence)
                {
                    if (combination.keyCode == e.keyCode && combination.modifiers == modifiers)
                    {
                        shortcut = entry.Value;
                        return true;
                    }
                    break; // Only single-key bindings.
                }
            }
            shortcut = null;
            return false;
        }

        private static Dictionary<string, MethodInfo> FindShortcuts()
        {
            var shortcuts = new Dictionary<string, MethodInfo>();
            foreach (Type type in typeof(KeysFollowMouse).Assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    // The ID is the attribute's first constructor argument (its property is internal).
                    foreach (CustomAttributeData attribute in method.GetCustomAttributesData())
                    {
                        if (attribute.AttributeType == typeof(ShortcutAttribute)
                            && attribute.ConstructorArguments.Count > 0
                            && attribute.ConstructorArguments[0].Value is string id
                            && id.StartsWith(ShortcutPrefix))
                            shortcuts[id] = method;
                    }
                }
            }
            return shortcuts;
        }
    }
}
