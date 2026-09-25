using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Blender's numpad views for the Scene View: 1 / 3 / 7 for front, right and top, Ctrl for the
    /// opposite side, 5 to toggle orthographic. The views keep the current pivot and zoom, and
    /// follow the axis convention in Preferences > Blender Like.
    /// </summary>
    internal static class NumpadViews
    {
        [Shortcut("Blender Like/Front View", typeof(SceneView), KeyCode.Keypad1)]
        private static void Front(ShortcutArguments args) => SetView(args, Quaternion.identity);

        [Shortcut("Blender Like/Back View", typeof(SceneView), KeyCode.Keypad1, ShortcutModifiers.Action)]
        private static void Back(ShortcutArguments args) => SetView(args, Quaternion.Euler(0f, 180f, 0f));

        [Shortcut("Blender Like/Right View", typeof(SceneView), KeyCode.Keypad3)]
        private static void Right(ShortcutArguments args) => SetView(args, Quaternion.Euler(0f, -90f, 0f));

        [Shortcut("Blender Like/Left View", typeof(SceneView), KeyCode.Keypad3, ShortcutModifiers.Action)]
        private static void Left(ShortcutArguments args) => SetView(args, Quaternion.Euler(0f, 90f, 0f));

        [Shortcut("Blender Like/Top View", typeof(SceneView), KeyCode.Keypad7)]
        private static void Top(ShortcutArguments args) => SetView(args, Quaternion.Euler(90f, 0f, 0f));

        [Shortcut("Blender Like/Bottom View", typeof(SceneView), KeyCode.Keypad7, ShortcutModifiers.Action)]
        private static void Bottom(ShortcutArguments args) => SetView(args, Quaternion.Euler(-90f, 0f, 0f));

        [Shortcut("Blender Like/Toggle Orthographic", typeof(SceneView), KeyCode.Keypad5)]
        private static void ToggleOrthographic(ShortcutArguments args)
        {
            SceneView sceneView = GetSceneView(args);
            if (sceneView != null)
                sceneView.orthographic = !sceneView.orthographic;
        }

        /// <param name="rotation">The view in Unity's axes; the configured yaw is applied on top.</param>
        private static void SetView(ShortcutArguments args, Quaternion rotation)
        {
            SceneView sceneView = GetSceneView(args);
            if (sceneView != null)
                sceneView.LookAt(sceneView.pivot, BlenderLikeSettings.ViewYaw * rotation, sceneView.size, true);
        }

        private static SceneView GetSceneView(ShortcutArguments args)
        {
            return args.context as SceneView ?? SceneView.lastActiveSceneView;
        }
    }
}
