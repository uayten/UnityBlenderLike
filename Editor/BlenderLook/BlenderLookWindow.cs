using UnityEditor;
using UnityEngine;

namespace UnityBlenderLike
{
    /// <summary>Window → Blender Like → Blender Look: sets up the active scene to light like Blender.</summary>
    internal sealed class BlenderLookWindow : EditorWindow
    {
        [SerializeField] private BlenderLookOptions options = new BlenderLookOptions();
        [SerializeField] private string report = "";
        private Vector2 scroll;

        [MenuItem("Window/Blender Like/Blender Look")]
        private static void Open()
        {
            var window = GetWindow<BlenderLookWindow>("Blender Look");
            window.FillFromScene();
        }

        /// <summary>Starts from the scene's own skybox, so a second run updates it in place.</summary>
        private void FillFromScene()
        {
            Material skybox = RenderSettings.skybox;
            if (skybox == null || skybox.shader.name != "Skybox/Cubemap")
                return;
            options.skybox = skybox;
            options.hdri = BlenderLook.SourceOf(skybox.GetTexture("_Tex") as Cubemap);
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox(
                "Lights the active scene like Blender's EEVEE with the Standard view transform. Changes project assets: the skybox, the URP Volume Profile and renderer, and adds a script to the project.",
                MessageType.Info);

            EditorGUILayout.LabelField("World", EditorStyles.boldLabel);
            options.hdri = (Cubemap)EditorGUILayout.ObjectField(
                new GUIContent("HDRI", "The same .exr or .hdr as Blender's world, imported as a Cubemap."),
                options.hdri, typeof(Cubemap), false);
            options.skybox = (Material)EditorGUILayout.ObjectField(
                new GUIContent("Skybox Material", "Skybox/Cubemap material to set up. Empty creates one next to the HDRI."),
                options.skybox, typeof(Material), false);
            options.strength = EditorGUILayout.FloatField(
                new GUIContent("Strength", "The Background node's Strength in Blender."), options.strength);
            options.exposure = EditorGUILayout.FloatField(
                new GUIContent("Exposure", "Fine trim over Strength. 0.95 matched EEVEE in side-by-side renders."), options.exposure);
            options.rotation = EditorGUILayout.FloatField(
                new GUIContent("Rotation", "Skybox rotation. 90 lines the HDRI up with Blender's world for default FBX exports."), options.rotation);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Sun", EditorStyles.boldLabel);
            options.sunThreshold = EditorGUILayout.FloatField(
                new GUIContent("Threshold", "World > Sun > Threshold in Blender. Brighter pixels become the sun light."), options.sunThreshold);
            options.sunShadows = EditorGUILayout.Toggle(
                new GUIContent("Shadows", "World > Sun > Shadow in Blender."), options.sunShadows);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("URP", EditorStyles.boldLabel);
            options.postProcessing = EditorGUILayout.Toggle(
                new GUIContent("Standard View Transform", "Tonemapping None and Vignette 0 in the URP asset's Volume Profile."), options.postProcessing);
            options.ambientOcclusion = EditorGUILayout.Toggle(
                new GUIContent("Ambient Occlusion", "Sets the renderer's SSAO to darken only small gaps, like EEVEE."), options.ambientOcclusion);
            using (new EditorGUI.DisabledScope(!options.ambientOcclusion))
            {
                EditorGUI.indentLevel++;
                options.aoRadius = EditorGUILayout.FloatField(
                    new GUIContent("Radius", "In meters. About an eighth of the models' height."), options.aoRadius);
                options.aoIntensity = EditorGUILayout.FloatField(new GUIContent("Intensity"), options.aoIntensity);
                EditorGUI.indentLevel--;
            }
            options.bake = EditorGUILayout.Toggle(
                new GUIContent("Generate Lighting", "Bakes the scene's ambient light and reflections after applying."), options.bake);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(options.hdri == null || EditorApplication.isPlayingOrWillChangePlaymode))
                if (GUILayout.Button("Apply to Active Scene", GUILayout.Height(28)))
                    report = BlenderLook.Apply(options);

            if (!string.IsNullOrEmpty(report))
                EditorGUILayout.HelpBox(report.TrimEnd(), MessageType.None);
            EditorGUILayout.EndScrollView();
        }
    }
}
