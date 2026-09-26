using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityBlenderLike
{
    /// <summary>
    /// Alt + drag on the move gizmo duplicates the selection and moves the copy along the gizmo
    /// part grabbed (an arrow, a plane square or the center), leaving the original in place, like
    /// Unreal. Unity binds Alt + left drag to its Orbit shortcut, which takes the press before the
    /// gizmo sees it, so the press is taken from the Scene view's panel first and the drag is done by
    /// the package's own move. Released without moving, the copy is taken back.
    /// </summary>
    [InitializeOnLoad]
    internal static class AltDragDuplicate
    {
        private const double PanelScanSeconds = 1.0;

        /// <summary>How close, in points, the mouse must be to an arrow of the gizmo.</summary>
        private const float ArrowPickDistance = 7f;

        private static readonly HashSet<VisualElement> HookedPanelRoots = new HashSet<VisualElement>();

        private static double nextScan;

        // The gizmo part under the mouse, worked out on every Scene view event (where the gizmo's
        // on-screen geometry is at hand) for the press to use.
        private static SceneView hoverView;
        private static int hoverAxis;
        private static bool hoverPlane;
        private static bool hoverOnGizmo;
        private static readonly Vector3[] HoverAxes = new Vector3[3];

        static AltDragDuplicate()
        {
            SceneView.duringSceneGui += TrackGizmoHover;
            EditorApplication.update += HookPanels;
        }

        private static bool CanDuplicate()
        {
            return BlenderLikeSettings.AltDragDuplicate
                && !ModalTransform.IsActive
                && !Tools.hidden
                && (Tools.current == Tool.Move || Tools.current == Tool.Transform)
                && Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable).Length > 0;
        }

        /// <summary>Finds which part of the move gizmo, if any, is under the mouse.</summary>
        private static void TrackGizmoHover(SceneView sceneView)
        {
            Event e = Event.current;
            if (e.type != EventType.Layout && e.type != EventType.MouseMove && e.type != EventType.Repaint)
                return;

            hoverView = sceneView;
            hoverOnGizmo = false;
            if (!CanDuplicate())
                return;

            Vector3 position = Tools.handlePosition;
            Quaternion rotation = Tools.handleRotation;
            float size = HandleUtility.GetHandleSize(position);
            HoverAxes[0] = rotation * Vector3.right;
            HoverAxes[1] = rotation * Vector3.up;
            HoverAxes[2] = rotation * Vector3.forward;

            // Arrows first: the closest one within reach.
            float best = ArrowPickDistance;
            for (int i = 0; i < 3; i++)
            {
                float distance = HandleUtility.DistanceToLine(position + HoverAxes[i] * size * 0.1f, position + HoverAxes[i] * size);
                if (distance < best)
                {
                    best = distance;
                    hoverAxis = i;
                    hoverPlane = false;
                    hoverOnGizmo = true;
                }
            }
            if (hoverOnGizmo)
                return;

            // Then the plane squares, which sit between two arrows near the center.
            Vector2 mouse = e.mousePosition;
            float squareReach = (HandleUtility.WorldToGUIPoint(position + HoverAxes[0] * size * 0.12f) - HandleUtility.WorldToGUIPoint(position)).magnitude;
            squareReach = Mathf.Max(squareReach, 8f);
            float bestSquare = squareReach;
            for (int i = 0; i < 3; i++)
            {
                Vector3 a = HoverAxes[(i + 1) % 3];
                Vector3 b = HoverAxes[(i + 2) % 3];
                Vector2 center = HandleUtility.WorldToGUIPoint(position + (a + b) * size * 0.25f);
                float distance = Vector2.Distance(mouse, center);
                if (distance < bestSquare)
                {
                    bestSquare = distance;
                    hoverAxis = i;
                    hoverPlane = true;
                    hoverOnGizmo = true;
                }
            }
            if (hoverOnGizmo)
                return;

            // Then the center: free in the view plane.
            if (Vector2.Distance(mouse, HandleUtility.WorldToGUIPoint(position)) < 8f)
            {
                hoverAxis = -1;
                hoverPlane = false;
                hoverOnGizmo = true;
            }
        }

        /// <summary>Scene views come and go, so new ones are picked up once a second.</summary>
        private static void HookPanels()
        {
            if (EditorApplication.timeSinceStartup < nextScan)
                return;
            nextScan = EditorApplication.timeSinceStartup + PanelScanSeconds;

            HookedPanelRoots.RemoveWhere(root => root.panel == null);
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                VisualElement panelRoot = sceneView.rootVisualElement?.panel?.visualTree;
                if (panelRoot != null && HookedPanelRoots.Add(panelRoot))
                    panelRoot.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            }
        }

        private static void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !evt.altKey || evt.shiftKey || evt.ctrlKey || evt.commandKey)
                return;
            if (!hoverOnGizmo || !(EditorWindow.mouseOverWindow is SceneView sceneView) || sceneView != hoverView || !CanDuplicate())
                return;

            // Taken before Unity's Alt + drag Orbit shortcut and the gizmo see it.
            evt.StopImmediatePropagation();

            Object[] originals = Selection.objects;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            if (!EditorApplication.ExecuteMenuItem("Edit/Duplicate"))
                return;
            Undo.SetCurrentGroupName("Duplicate and Move");

            var axes = new[] { HoverAxes[0], HoverAxes[1], HoverAxes[2] };
            ModalTransform.BeginDrag(sceneView, undoGroup, axes, hoverAxis, hoverPlane, originals);
        }
    }
}
