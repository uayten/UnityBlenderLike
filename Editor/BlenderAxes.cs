using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Converts transform values between Unity's axes and Blender's, as a default Blender FBX
    /// export relates them: Blender X = Unity -X, Blender Y = Unity -Z, Blender Z (up) = Unity Y.
    /// </summary>
    internal static class BlenderAxes
    {
        public static Vector3 PositionToBlender(Vector3 unity) => Clean(new Vector3(-unity.x, -unity.z, unity.y));

        public static Vector3 PositionFromBlender(Vector3 blender) => new Vector3(-blender.x, blender.z, -blender.y);

        /// <summary>Scale along each axis is a length, so it only swaps Y and Z; the signs stay.</summary>
        public static Vector3 ScaleToBlender(Vector3 unity) => new Vector3(unity.x, unity.z, unity.y);

        public static Vector3 ScaleFromBlender(Vector3 blender) => new Vector3(blender.x, blender.z, blender.y);

        /// <summary>
        /// Blender's XYZ Euler angles, in degrees: the rotation is Z(z) * Y(y) * X(x) in Blender's
        /// right-handed axes, which is what Blender's N panel shows.
        /// </summary>
        public static Vector3 RotationToBlender(Quaternion unity)
        {
            // The axis conversion is a mirror: it maps the rotation axis and flips it.
            Vector3 axis = -PositionToBlender(new Vector3(unity.x, unity.y, unity.z));
            Matrix4x4 m = Matrix4x4.Rotate(new Quaternion(axis.x, axis.y, axis.z, unity.w));

            float x, y, z;
            if (Mathf.Abs(m.m20) < 0.99999f)
            {
                y = Mathf.Asin(-m.m20);
                x = Mathf.Atan2(m.m21, m.m22);
                z = Mathf.Atan2(m.m10, m.m00);
            }
            else
            {
                // Gimbal lock: X and Z turn around the same axis, so Z takes none of it.
                y = m.m20 < 0f ? Mathf.PI / 2f : -Mathf.PI / 2f;
                x = Mathf.Atan2(-m.m12, m.m11);
                z = 0f;
            }
            return Clean(Round(new Vector3(x, y, z) * Mathf.Rad2Deg));
        }

        public static Quaternion RotationFromBlender(Vector3 euler)
        {
            Quaternion blender = Quaternion.AngleAxis(euler.z, Vector3.forward)
                * Quaternion.AngleAxis(euler.y, Vector3.up)
                * Quaternion.AngleAxis(euler.x, Vector3.right);
            Vector3 axis = -PositionFromBlender(new Vector3(blender.x, blender.y, blender.z));
            return new Quaternion(axis.x, axis.y, axis.z, blender.w);
        }

        /// <summary>Drops the float noise from the trigonometry, so 90 doesn't show as 89.99999.</summary>
        private static Vector3 Round(Vector3 value)
        {
            const float precision = 10000f;
            return new Vector3(
                Mathf.Round(value.x * precision) / precision,
                Mathf.Round(value.y * precision) / precision,
                Mathf.Round(value.z * precision) / precision);
        }

        /// <summary>Turns -0 into 0, so the Inspector doesn't show "-0".</summary>
        private static Vector3 Clean(Vector3 value)
        {
            return new Vector3(value.x + 0f, value.y + 0f, value.z + 0f);
        }
    }
}
