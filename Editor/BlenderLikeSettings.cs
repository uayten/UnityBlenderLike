using UnityEditor;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>Which axis convention the numpad views and the X / Y / Z axis locks follow.</summary>
    internal enum AxisConvention
    {
        Blender,
        Unity,
    }

    /// <summary>
    /// Package settings, in Preferences > Blender Like. Stored per project and per user in
    /// UserSettings/, which Unity projects keep out of version control.
    /// </summary>
    internal static class BlenderLikeSettings
    {
        private const string AxesKey = "UnityBlenderLike.Axes";
        private const string AutoPerspectiveKey = "UnityBlenderLike.AutoPerspective";

        public static AxisConvention Axes
        {
            get => EditorUserSettings.GetConfigValue(AxesKey) == nameof(AxisConvention.Unity)
                ? AxisConvention.Unity
                : AxisConvention.Blender;
            set => EditorUserSettings.SetConfigValue(AxesKey, value.ToString());
        }

        /// <summary>
        /// Turn applied to every numpad view. A default Blender FBX export brings Blender's +Y to
        /// Unity's -Z, so Blender's top view is Unity's top view turned 180 degrees around Y.
        /// </summary>
        public static Quaternion ViewYaw => Quaternion.Euler(0f, Axes == AxisConvention.Blender ? 180f : 0f, 0f);

        /// <summary>
        /// Direction of the global X / Y / Z axis in Unity's world. With Blender axes, Z is up and
        /// Y runs back to front, as a default FBX export converts them (Blender X, Y, Z = Unity -X,
        /// -Z, Y), so G X 1 moves the same way as in Blender.
        /// </summary>
        public static Vector3 GlobalDirection(int index)
        {
            if (Axes == AxisConvention.Unity)
                return index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;
            return index == 0 ? Vector3.left : index == 1 ? Vector3.back : Vector3.up;
        }

        /// <summary>
        /// Axis a rotation locked to global X / Y / Z turns around, chosen so a typed angle turns
        /// the same way as in Blender. Blender is right-handed and Unity left-handed: the mirror
        /// between them flips the axis, so with Blender axes it's the negated direction.
        /// </summary>
        public static Vector3 GlobalRotationAxis(int index)
        {
            Vector3 direction = GlobalDirection(index);
            return Axes == AxisConvention.Unity ? direction : -direction;
        }

        /// <summary>
        /// Like Blender's Auto Perspective: a view made orthographic by a numpad view key goes back
        /// to perspective when it's orbited.
        /// </summary>
        public static bool AutoPerspective
        {
            get => EditorUserSettings.GetConfigValue(AutoPerspectiveKey) != bool.FalseString;
            set => EditorUserSettings.SetConfigValue(AutoPerspectiveKey, value.ToString());
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Preferences/Blender Like", SettingsScope.User)
            {
                label = "Blender Like",
                keywords = new[] { "Blender", "numpad", "view", "move", "rotate", "scale", "axis", "perspective" },
                guiHandler = _ =>
                {
                    EditorGUIUtility.labelWidth = 160f;
                    Axes = (AxisConvention)EditorGUILayout.EnumPopup(
                        new GUIContent("Axes", "Axis convention of the numpad views and of X / Y / Z in G / R / S."),
                        Axes);
                    AutoPerspective = EditorGUILayout.Toggle(
                        new GUIContent("Auto Perspective", "A view made orthographic by Numpad 1 / 3 / 7 goes back to perspective when you orbit it."),
                        AutoPerspective);
                    EditorGUILayout.HelpBox(
                        "Blender: views and axis locks match Blender for models exported with the default FBX "
                        + "settings. Numpad 7 shows what Blender's top view shows, and Z is up.\n"
                        + "Unity: views and axis locks use Unity's own axes (Y up, +Z forward).\n\n"
                        + "Saved for this project only, on this machine.",
                        MessageType.None);
                },
            };
        }
    }
}
