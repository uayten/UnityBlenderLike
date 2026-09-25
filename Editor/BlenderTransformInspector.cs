using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>
    /// Transform Inspector in Blender's axes (Z up): Location, Rotation and Scale show and edit the
    /// values Blender's N panel would show for the same object. Only the display changes; the
    /// object keeps Unity's values, so scenes, prefabs and code are untouched. With Unity axes, or
    /// the Z-up Inspector turned off, Unity's own Transform Inspector draws instead.
    /// </summary>
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    internal sealed class BlenderTransformInspector : Editor
    {
        private const string ContextMenuPath = "CONTEXT/Transform/Blender Like: Z-Up Values";

        private static readonly Type UnityInspectorType = typeof(Editor).Assembly.GetType("UnityEditor.TransformInspector");

        private static readonly GUIContent[] AxisLabels =
        {
            new GUIContent("X"),
            new GUIContent("Y"),
            new GUIContent("Z"),
        };

        private const string Tooltip = "Blender axes: Z up, Y from front to back. Right click the Transform header to switch to Unity's values.";

        private static readonly GUIContent LocationLabel = new GUIContent("Location", Tooltip);
        private static readonly GUIContent RotationLabel = new GUIContent("Rotation", Tooltip + "\nBlender's XYZ Euler, in degrees.");
        private static readonly GUIContent ScaleLabel = new GUIContent("Scale", Tooltip);

        /// <summary>
        /// Last angles typed per transform. Many angle sets give the same rotation (190 and -170);
        /// while the rotation hasn't changed, the Inspector keeps showing what was typed.
        /// </summary>
        private static readonly Dictionary<Transform, Vector3> RotationHints = new Dictionary<Transform, Vector3>();

        private Editor unityInspector;
        private SerializedProperty positionProperty;
        private SerializedProperty rotationProperty;
        private SerializedProperty scaleProperty;

        /// <summary>
        /// An Inspector already open when this editor first compiles can keep Unity's Transform
        /// Inspector until the selection changes; rebuilding it picks this one up right away.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void RebuildOpenInspectors()
        {
            EditorApplication.delayCall += () => ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private void OnEnable()
        {
            positionProperty = serializedObject.FindProperty("m_LocalPosition");
            rotationProperty = serializedObject.FindProperty("m_LocalRotation");
            scaleProperty = serializedObject.FindProperty("m_LocalScale");
        }

        private void OnDisable()
        {
            if (unityInspector != null)
                DestroyImmediate(unityInspector);
        }

        public override void OnInspectorGUI()
        {
            if (!BlenderLikeSettings.ZUpTransformActive)
            {
                DrawUnityInspector();
                return;
            }

            serializedObject.Update();
            DrawRow(LocationLabel, positionProperty,
                t => BlenderAxes.PositionToBlender(t.localPosition),
                (t, value) => t.localPosition = BlenderAxes.PositionFromBlender(value));
            DrawRow(RotationLabel, rotationProperty, GetRotation, SetRotation);
            DrawRow(ScaleLabel, scaleProperty,
                t => BlenderAxes.ScaleToBlender(t.localScale),
                (t, value) => t.localScale = BlenderAxes.ScaleFromBlender(value));
        }

        private void DrawUnityInspector()
        {
            if (UnityInspectorType == null)
            {
                base.OnInspectorGUI();
                return;
            }
            if (unityInspector == null)
                unityInspector = CreateEditor(targets, UnityInspectorType);
            unityInspector.OnInspectorGUI();
        }

        /// <summary>
        /// One Vector3 row. With several objects selected, a component that differs shows as mixed
        /// and editing it sets only that component on each object, like Unity's own Inspector.
        /// </summary>
        private void DrawRow(GUIContent label, SerializedProperty property, Func<Transform, Vector3> get, Action<Transform, Vector3> set)
        {
            Rect rect = EditorGUILayout.GetControlRect();

            // BeginProperty keeps the prefab override marks and the right-click revert menu.
            EditorGUI.BeginProperty(rect, label, property);
            Rect fields = EditorGUI.PrefixLabel(rect, label);

            int previousIndent = EditorGUI.indentLevel;
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;
            EditorGUIUtility.labelWidth = 13f;

            Vector3 value = get((Transform)target);
            const float spacing = 4f;
            float width = (fields.width - spacing * 2f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var fieldRect = new Rect(fields.x + (width + spacing) * i, fields.y, width, fields.height);
                EditorGUI.showMixedValue = IsMixed(get, i, value[i]);
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUI.FloatField(fieldRect, AxisLabels[i], value[i]);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(targets, "Inspector");
                    foreach (Transform transform in targets)
                    {
                        Vector3 current = get(transform);
                        current[i] = newValue;
                        set(transform, current);
                    }
                }
            }

            EditorGUI.showMixedValue = false;
            EditorGUIUtility.labelWidth = previousLabelWidth;
            EditorGUI.indentLevel = previousIndent;
            EditorGUI.EndProperty();
        }

        private bool IsMixed(Func<Transform, Vector3> get, int component, float value)
        {
            foreach (Transform transform in targets)
            {
                if (!Mathf.Approximately(get(transform)[component], value))
                    return true;
            }
            return false;
        }

        private static Vector3 GetRotation(Transform transform)
        {
            Quaternion rotation = transform.localRotation;
            if (RotationHints.TryGetValue(transform, out Vector3 hint)
                && Quaternion.Angle(BlenderAxes.RotationFromBlender(hint), rotation) < 0.001f)
                return hint;
            return BlenderAxes.RotationToBlender(rotation);
        }

        private static void SetRotation(Transform transform, Vector3 euler)
        {
            transform.localRotation = BlenderAxes.RotationFromBlender(euler);
            RotationHints[transform] = euler;
        }

        [MenuItem(ContextMenuPath)]
        private static void ToggleZUpValues()
        {
            BlenderLikeSettings.ZUpTransformInspector = !BlenderLikeSettings.ZUpTransformInspector;
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem(ContextMenuPath, true)]
        private static bool ValidateToggleZUpValues()
        {
            Menu.SetChecked(ContextMenuPath, BlenderLikeSettings.ZUpTransformInspector);
            return BlenderLikeSettings.Axes == AxisConvention.Blender;
        }
    }
}
