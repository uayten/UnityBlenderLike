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
        /// World axis that the X / Y / Z key locks to. With Blender axes, Z is up and Y runs back
        /// to front, as FBX converts them (Blender X, Y, Z = Unity -X, -Z, Y). The vectors are
        /// chosen so a typed angle turns the same way it does in Blender.
        /// </summary>
        public static Vector3 GlobalAxis(int index)
        {
            if (Axes == AxisConvention.Unity)
                return index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;

            // Blender is right-handed and Unity left-handed: the mirror flips the axis a
            // rotation turns around, so each one is the negated image of Blender's axis.
            return index == 0 ? Vector3.right : index == 1 ? Vector3.forward : Vector3.down;
        }

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Preferences/Blender Like", SettingsScope.User)
            {
                label = "Blender Like",
                keywords = new[] { "Blender", "numpad", "view", "rotate", "axis" },
                guiHandler = _ =>
                {
                    EditorGUIUtility.labelWidth = 160f;
                    Axes = (AxisConvention)EditorGUILayout.EnumPopup(
                        new GUIContent("Axes", "Axis convention of the numpad views and the X / Y / Z axis locks."),
                        Axes);
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
