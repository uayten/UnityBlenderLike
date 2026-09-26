using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Moves the mouse cursor, which Unity has no API for: user32 on Windows, CoreGraphics on
    /// macOS. On other platforms nothing moves and <see cref="TryWarp"/> returns false.
    /// </summary>
    internal static class CursorWarp
    {
        /// <param name="guiPoint">Point in the current GUI's coordinates (call from inside OnGUI).</param>
        public static bool TryWarp(Vector2 guiPoint)
        {
            Vector2 screen = GUIUtility.GUIToScreenPoint(guiPoint);
#if UNITY_EDITOR_WIN
            // Windows takes physical pixels; Unity's screen points are scaled by the display scale.
            float scale = EditorGUIUtility.pixelsPerPoint;
            return SetCursorPos(Mathf.RoundToInt(screen.x * scale), Mathf.RoundToInt(screen.y * scale));
#elif UNITY_EDITOR_OSX
            // macOS takes points, from the top-left of the main display, like Unity's screen points.
            if (CGWarpMouseCursorPosition(new CGPoint { X = screen.x, Y = screen.y }) != 0)
                return false;
            // Without this, macOS freezes the cursor for a moment after every warp.
            CGAssociateMouseAndMouseCursorPosition(1);
            return true;
#else
            return false;
#endif
        }

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);
#elif UNITY_EDITOR_OSX
        [StructLayout(LayoutKind.Sequential)]
        private struct CGPoint
        {
            public double X;
            public double Y;
        }

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern int CGWarpMouseCursorPosition(CGPoint point);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern int CGAssociateMouseAndMouseCursorPosition(int connected);
#endif
    }
}
