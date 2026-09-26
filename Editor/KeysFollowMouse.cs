using System.Collections.Generic;
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
    /// <remarks>
    /// Keys reach windows two ways. IMGUI windows get them through the Shortcut Manager's global
    /// event handler; UI Toolkit windows (Unity 6's Hierarchy, for one) get them straight from their
    /// panel, without that handler. Both are watched.
    /// </remarks>
    [InitializeOnLoad]
    internal static class KeysFollowMouse
    {
        private const double WindowScanSeconds = 1.0;

        // Internal field the Shortcut Manager listens on; this handler goes first so the focused
        // window never sees the key.
        private static readonly FieldInfo GlobalEventHandlerField = typeof(EditorApplication).GetField(
            "globalEventHandler", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly EditorApplication.CallbackFunction Handler = OnGlobalEvent;

        private static readonly HashSet<VisualElement> HookedPanelRoots = new HashSet<VisualElement>();

        /// <summary>
        /// A key that types a character comes as two key-down events: the key, then the character.
        /// After sending the key on, the character is swallowed too, so the window that had the
        /// focus doesn't type it (the Hierarchy would jump to a matching name).
        /// </summary>
        private static bool swallowCharacter;

        private static double nextScan;

        static KeysFollowMouse()
        {
            EditorApplication.update += HookPanels;
            if (GlobalEventHandlerField == null)
                return;

            // Wait a frame, so the Shortcut Manager has registered its own handler to go ahead of.
            EditorApplication.delayCall += () =>
            {
                var current = (EditorApplication.CallbackFunction)GlobalEventHandlerField.GetValue(null);
                GlobalEventHandlerField.SetValue(null, Handler + (current - Handler));
            };
        }

        // ---- IMGUI windows ----

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

            if (!ShouldRoute(e.keyCode, out SceneView sceneView))
                return;

            var key = new Event(e);
            e.Use();
            SendTo(sceneView, key);
        }

        // ---- UI Toolkit windows ----

        /// <summary>Editor windows come and go, so new panels are picked up once a second.</summary>
        private static void HookPanels()
        {
            if (EditorApplication.timeSinceStartup < nextScan)
                return;
            nextScan = EditorApplication.timeSinceStartup + WindowScanSeconds;

            HookedPanelRoots.RemoveWhere(root => root.panel == null);
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window is SceneView)
                    continue;
                VisualElement panelRoot = window.rootVisualElement?.panel?.visualTree;
                if (panelRoot == null || !HookedPanelRoots.Add(panelRoot))
                    continue;

                // Trickle down from the panel's root: the key is taken before any element in the
                // window sees it, and so are the navigation events a key turns into.
                panelRoot.RegisterCallback<KeyDownEvent>(OnPanelKeyDown, TrickleDown.TrickleDown);
                panelRoot.RegisterCallback<NavigationMoveEvent>(OnPanelNavigation, TrickleDown.TrickleDown);
                panelRoot.RegisterCallback<NavigationSubmitEvent>(OnPanelNavigation, TrickleDown.TrickleDown);
                panelRoot.RegisterCallback<NavigationCancelEvent>(OnPanelNavigation, TrickleDown.TrickleDown);
            }
        }

        private static void OnPanelKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.None)
            {
                if (swallowCharacter)
                    evt.StopImmediatePropagation();
                swallowCharacter = false;
                return;
            }
            swallowCharacter = false;

            if (!ShouldRoute(evt.keyCode, out SceneView sceneView))
                return;

            evt.StopImmediatePropagation();
            SendTo(sceneView, new Event
            {
                type = EventType.KeyDown,
                keyCode = evt.keyCode,
                modifiers = evt.modifiers,
                character = evt.character,
            });
        }

        /// <summary>Arrows, Home, End, Enter and Esc also move the window's selection as navigation events.</summary>
        private static void OnPanelNavigation(EventBase evt)
        {
            if (EditorWindow.mouseOverWindow is SceneView && !(EditorWindow.focusedWindow is SceneView)
                && !IsGameView(EditorWindow.focusedWindow) && !IsTyping(EditorWindow.focusedWindow))
                evt.StopImmediatePropagation();
        }

        // ---- Routing ----

        private static bool ShouldRoute(KeyCode keyCode, out SceneView sceneView)
        {
            sceneView = EditorWindow.mouseOverWindow as SceneView;
            return sceneView != null
                && EditorWindow.focusedWindow != sceneView
                && !IsGameView(EditorWindow.focusedWindow)
                && !IsModifier(keyCode)
                && !IsTyping(EditorWindow.focusedWindow);
        }

        /// <summary>The Game view keeps its keys: while playing, the mouse can drift over the Scene view.</summary>
        private static bool IsGameView(EditorWindow window)
        {
            return window != null && window.GetType().Name == "GameView";
        }

        /// <summary>Gives the Scene view the focus and the key.</summary>
        internal static void SendTo(SceneView sceneView, Event key)
        {
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
