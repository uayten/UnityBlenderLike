using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Blender's object shortcuts for the Scene View: hide and reveal, clear location / rotation /
    /// scale, duplicate and move, parent and unparent, select all and none.
    /// </summary>
    internal static class ObjectShortcuts
    {
        // ---- Hide and reveal (Scene view visibility: it isn't saved in the scene) ----

        [Shortcut("Blender Like/Hide Selected", typeof(SceneView), KeyCode.H)]
        private static void HideSelected()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length > 0)
                SceneVisibilityManager.instance.Hide(selected, true);
        }

        [Shortcut("Blender Like/Hide Unselected", typeof(SceneView), KeyCode.H, ShortcutModifiers.Shift)]
        private static void HideUnselected()
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length == 0)
                return;
            SceneVisibilityManager.instance.HideAll();
            SceneVisibilityManager.instance.Show(selected, true);
        }

        [Shortcut("Blender Like/Reveal Hidden", typeof(SceneView), KeyCode.H, ShortcutModifiers.Alt)]
        private static void RevealHidden()
        {
            SceneVisibilityManager.instance.ShowAll();
        }

        // ---- Clear transforms ----

        [Shortcut("Blender Like/Clear Location", typeof(SceneView), KeyCode.G, ShortcutModifiers.Alt)]
        private static void ClearLocation() => ClearTransforms("Clear Location", t => t.localPosition = Vector3.zero);

        [Shortcut("Blender Like/Clear Rotation", typeof(SceneView), KeyCode.R, ShortcutModifiers.Alt)]
        private static void ClearRotation() => ClearTransforms("Clear Rotation", t => t.localRotation = Quaternion.identity);

        [Shortcut("Blender Like/Clear Scale", typeof(SceneView), KeyCode.S, ShortcutModifiers.Alt)]
        private static void ClearScale() => ClearTransforms("Clear Scale", t => t.localScale = Vector3.one);

        private static void ClearTransforms(string undoName, System.Action<Transform> clear)
        {
            Transform[] selected = Selection.GetTransforms(SelectionMode.Editable);
            if (selected.Length == 0)
                return;
            Undo.RecordObjects(selected, undoName);
            foreach (Transform transform in selected)
                clear(transform);
        }

        // ---- Duplicate ----

        [Shortcut("Blender Like/Duplicate and Move", typeof(SceneView), KeyCode.D, ShortcutModifiers.Shift)]
        private static void DuplicateAndMove(ShortcutArguments args)
        {
            if (Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable).Length == 0)
                return;

            // Duplicate and move undo together, as in Blender.
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            if (!EditorApplication.ExecuteMenuItem("Edit/Duplicate"))
                return;
            ModalTransform.Begin(ModalTransform.Mode.Move, args.context as SceneView, undoGroup);
        }

        // ---- Parent ----

        /// <summary>Parents the selected objects to the active one (select the children first, the parent last).</summary>
        [Shortcut("Blender Like/Parent", typeof(SceneView), KeyCode.P, ShortcutModifiers.Action)]
        private static void Parent()
        {
            Transform parent = Selection.activeTransform;
            if (parent == null)
                return;

            foreach (Transform child in Selection.GetTransforms(SelectionMode.Editable))
            {
                if (child == parent || child.parent == parent)
                    continue;
                if (parent.IsChildOf(child))
                {
                    Debug.LogWarning($"Blender Like: can't parent {child.name} to {parent.name}, which is inside it.", child);
                    continue;
                }
                if (CanChangeParent(child))
                    Undo.SetTransformParent(child, parent, "Parent");
            }
        }

        /// <summary>Takes the selected objects out of their parents, keeping where they are.</summary>
        [Shortcut("Blender Like/Clear Parent", typeof(SceneView), KeyCode.P, ShortcutModifiers.Alt)]
        private static void ClearParent()
        {
            foreach (Transform child in Selection.GetTransforms(SelectionMode.Editable))
            {
                if (child.parent != null && CanChangeParent(child))
                    Undo.SetTransformParent(child, null, "Clear Parent");
            }
        }

        private static bool CanChangeParent(Transform transform)
        {
            // Unity keeps the hierarchy inside a prefab instance fixed; only its root can move.
            if (PrefabUtility.IsPartOfPrefabInstance(transform)
                && !PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject))
            {
                Debug.LogWarning($"Blender Like: {transform.name} is inside a prefab instance, so its parent can't change here. Open the prefab to change it.", transform);
                return false;
            }
            return true;
        }

        // ---- Select ----

        [Shortcut("Blender Like/Select All", typeof(SceneView), KeyCode.A)]
        private static void SelectAll()
        {
            EditorApplication.ExecuteMenuItem("Edit/Select All");
        }

        [Shortcut("Blender Like/Select None", typeof(SceneView), KeyCode.A, ShortcutModifiers.Alt)]
        private static void SelectNone()
        {
            Selection.objects = new Object[0];
        }
    }
}
