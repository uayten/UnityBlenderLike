using System;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityBlenderLike
{
    /// <summary>
    /// Blender's precision drag in the Inspector: while Shift is held, a drag moves at a tenth of
    /// the mouse's speed, so a slider (a material's Range property, for one) changes slowly.
    /// IMGUI sliders take their value from how far the mouse went since the press, so each
    /// Inspector IMGUI container's GUI is wrapped, and a drag's mouse position is replaced by a
    /// virtual one that advances by the scaled movement. Releasing Shift mid-drag keeps the value
    /// where it is and goes on at full speed.
    /// </summary>
    [InitializeOnLoad]
    internal static class ShiftPrecisionDrag
    {
        private const float PrecisionFactor = 0.1f;
        private const double WindowScanSeconds = 0.25;

        private static readonly Type PropertyEditorType = typeof(Editor).Assembly.GetType("UnityEditor.PropertyEditor");

        /// <summary>The wrapper installed on each container, to notice when the Inspector replaces it.</summary>
        private static readonly ConditionalWeakTable<IMGUIContainer, Action> Wrappers = new ConditionalWeakTable<IMGUIContainer, Action>();

        private static double nextScan;
        private static bool tracking;
        private static Vector2 lastReal;
        private static Vector2 offset;

        static ShiftPrecisionDrag()
        {
            if (PropertyEditorType == null)
                return;
            EditorApplication.update += WrapContainers;
        }

        /// <summary>The Inspector rebuilds its containers on every selection, so they're checked often.</summary>
        private static void WrapContainers()
        {
            if (EditorApplication.timeSinceStartup < nextScan)
                return;
            nextScan = EditorApplication.timeSinceStartup + WindowScanSeconds;

            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll(PropertyEditorType))
            {
                window.rootVisualElement.Query<IMGUIContainer>().ForEach(container =>
                {
                    Action original = container.onGUIHandler;
                    if (original == null || (Wrappers.TryGetValue(container, out Action installed) && installed == original))
                        return;

                    Action wrapper = () =>
                    {
                        Rewrite(Event.current);
                        original();
                    };
                    Wrappers.Remove(container);
                    Wrappers.Add(container, wrapper);
                    container.onGUIHandler = wrapper;
                });
            }
        }

        private static void Rewrite(Event e)
        {
            if (e == null)
                return;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp)
            {
                tracking = false;
                return;
            }
            if (e.type != EventType.MouseDrag || !BlenderLikeSettings.ShiftPrecisionDrag)
                return;

            bool shift = e.shift;
            if (!tracking)
            {
                // Only from the first Shift, so a plain drag keeps the cursor and the control in step.
                if (!shift || GUIUtility.hotControl == 0)
                    return;
                tracking = true;
                lastReal = e.mousePosition;
                offset = Vector2.zero;
            }

            Vector2 delta = e.mousePosition - lastReal;
            lastReal = e.mousePosition;
            if (shift)
            {
                offset += delta * (1f - PrecisionFactor);
                e.delta *= PrecisionFactor;
                // Number fields dragged by their label go four times faster with Shift; without it
                // they get just the slowdown.
                e.modifiers &= ~EventModifiers.Shift;
            }
            e.mousePosition -= offset;
        }
    }
}
