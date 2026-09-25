using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityBlenderLike
{
    /// <summary>
    /// Blender's Outliner drag on disclosure triangles, for Unity's Hierarchy: press an item's
    /// expand arrow and drag up or down, and every item the mouse passes over gets the same state
    /// (all expanding, or all collapsing). Works with the UI Toolkit Hierarchy of Unity 6; the
    /// types are looked up by name, so on versions without it nothing happens.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyFoldoutDrag
    {
        private const string FoldoutToggleClass = "unity-tree-view__item-toggle";
        private const string HiddenToggleClass = "hierarchy-item__toggle--hidden";
        private const double WindowScanSeconds = 1.0;

        /// <summary>Distance between the points checked along a fast drag, so no row is skipped.</summary>
        private const float PickStep = 4f;

        private static readonly Type WindowType = FindType("Unity.Hierarchy.Editor.HierarchyWindow");
        private static readonly Type ItemType = FindType("Unity.Hierarchy.HierarchyViewItem");
        private static readonly FieldInfo NodeField = ItemType?.GetField("m_Node", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo ViewProperty = ItemType?.GetProperty("View", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly PropertyInfo ToggleProperty = ItemType?.GetProperty("Toggle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly MethodInfo IsExpandedMethod = FindViewMethod("IsExpanded");
        private static readonly MethodInfo ExpandMethod = FindViewMethod("Expand");
        private static readonly MethodInfo CollapseMethod = FindViewMethod("Collapse");

        private static readonly HashSet<VisualElement> HookedRoots = new HashSet<VisualElement>();

        private static double nextScan;
        private static bool dragging;
        private static bool expanding;
        private static VisualElement dragRoot;
        private static float pickX;
        private static float lastY;

        static HierarchyFoldoutDrag()
        {
            if (WindowType == null || NodeField == null || ViewProperty == null || ToggleProperty == null
                || IsExpandedMethod == null || ExpandMethod == null || CollapseMethod == null)
                return;
            EditorApplication.update += HookOpenWindows;
        }

        /// <summary>Hierarchy windows come and go, so new ones are picked up once a second.</summary>
        private static void HookOpenWindows()
        {
            if (EditorApplication.timeSinceStartup < nextScan)
                return;
            nextScan = EditorApplication.timeSinceStartup + WindowScanSeconds;

            HookedRoots.RemoveWhere(root => root.panel == null);
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll(WindowType))
            {
                VisualElement root = window.rootVisualElement;
                if (!HookedRoots.Add(root))
                    continue;

                // Trickle down: the press is taken before the arrow's own click or the row's
                // selection sees it.
                root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
                root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
                root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
                root.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            }
        }

        private static void OnPointerDown(PointerDownEvent evt)
        {
            // Alt+click keeps Unity's expand-all-children.
            if (evt.button != 0 || evt.altKey || evt.shiftKey || evt.actionKey)
                return;
            if (!IsFoldoutArrow(evt.target as VisualElement, out VisualElement item))
                return;
            if (!TryGetNode(item, out object view, out object node))
                return;

            expanding = !(bool)IsExpandedMethod.Invoke(view, new[] { node });
            SetExpanded(view, node);

            dragging = true;
            dragRoot = (VisualElement)evt.currentTarget;
            pickX = evt.position.x;
            lastY = evt.position.y;
            dragRoot.CapturePointer(evt.pointerId);
            evt.StopImmediatePropagation();
        }

        private static void OnPointerMove(PointerMoveEvent evt)
        {
            if (!dragging || evt.currentTarget != dragRoot)
                return;

            // Check every row between the last position and this one, like a brush stroke.
            float y = evt.position.y;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(y - lastY) / PickStep));
            for (int i = 1; i <= steps; i++)
                ApplyAt(new Vector2(pickX, Mathf.Lerp(lastY, y, (float)i / steps)));
            lastY = y;
            evt.StopImmediatePropagation();
        }

        private static void OnPointerUp(PointerUpEvent evt)
        {
            if (!dragging || evt.currentTarget != dragRoot)
                return;

            dragging = false;
            dragRoot.ReleasePointer(evt.pointerId);
            evt.StopImmediatePropagation();
        }

        private static void ApplyAt(Vector2 panelPosition)
        {
            VisualElement picked = dragRoot.panel?.Pick(panelPosition);
            VisualElement item = FindItem(picked);
            if (item == null || !HasChildren(item) || !TryGetNode(item, out object view, out object node))
                return;
            if ((bool)IsExpandedMethod.Invoke(view, new[] { node }) != expanding)
                SetExpanded(view, node);
        }

        private static void SetExpanded(object view, object node)
        {
            (expanding ? ExpandMethod : CollapseMethod).Invoke(view, new[] { node });
        }

        private static bool IsFoldoutArrow(VisualElement element, out VisualElement item)
        {
            item = null;
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (ItemType.IsInstanceOfType(current))
                    return false;
                if (current.ClassListContains(FoldoutToggleClass))
                {
                    item = FindItem(current);
                    return item != null && HasChildren(item);
                }
            }
            return false;
        }

        private static VisualElement FindItem(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (ItemType.IsInstanceOfType(current))
                    return current;
            }
            return null;
        }

        /// <summary>Items without children keep their arrow, hidden with a class.</summary>
        private static bool HasChildren(VisualElement item)
        {
            return ToggleProperty.GetValue(item) is VisualElement toggle && !toggle.ClassListContains(HiddenToggleClass);
        }

        private static bool TryGetNode(VisualElement item, out object view, out object node)
        {
            view = ViewProperty.GetValue(item);
            node = NodeField.GetValue(item);
            return view != null && node != null;
        }

        private static MethodInfo FindViewMethod(string name)
        {
            Type viewType = FindType("Unity.Hierarchy.HierarchyView");
            Type nodeType = FindType("Unity.Hierarchy.HierarchyNode");
            if (viewType == null || nodeType == null)
                return null;
            return viewType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public, null,
                new[] { nodeType.MakeByRefType() }, null);
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
