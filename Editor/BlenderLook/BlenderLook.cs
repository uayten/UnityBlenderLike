using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace UnityBlenderLike
{
    /// <summary>What Blender Look applies, as set in its window.</summary>
    [Serializable]
    internal sealed class BlenderLookOptions
    {
        public Cubemap hdri;
        public Material skybox;
        public float strength = 1f;
        public float exposure = 0.95f;
        public float rotation = 90f;
        public float sunThreshold = 10f;
        public bool sunShadows;
        public bool postProcessing = true;
        public bool ambientOcclusion = true;
        public float aoRadius = 0.03f;
        public float aoIntensity = 0.5f;
        public bool bake = true;
    }

    /// <summary>
    /// Sets up URP lighting to look like Blender's EEVEE Material Preview and renders with the
    /// Standard view transform. EEVEE lights a scene from an HDRI in two parts: pixels brighter
    /// than the world's sun threshold become a sun light, and the rest lights diffuse surfaces
    /// through L1 spherical harmonics. Blender Look rebuilds both parts in Unity: a copy of the
    /// HDRI clamped at the threshold for the skybox, a directional light with the energy taken
    /// out, and a script that drops the L2 band of Unity's ambient probe.
    /// </summary>
    internal static class BlenderLook
    {
        /// <summary>Suffix of the clamped copy, written next to the source HDRI.</summary>
        public const string ClampedSuffix = "_eevee";

        private const string SunName = "Blender Sun";
        private const string ScriptFolder = "Assets/BlenderLook";

        private struct Sun
        {
            public Color linearIrradiance;
            public Vector3 towardSun;
        }

        public static string Apply(BlenderLookOptions options)
        {
            var report = new StringBuilder();
            Cubemap source = SourceOf(options.hdri);
            if (source == null)
                return "Pick the HDRI first: an equirectangular .exr or .hdr imported as a Cubemap.";

            Cubemap clamped = ClampHdri(source, options.sunThreshold, options.rotation, out Sun sun);
            report.AppendLine($"Clamped HDRI at {options.sunThreshold}: {AssetDatabase.GetAssetPath(clamped)}");

            Material skybox = SetUpSkybox(options, source, clamped);
            report.AppendLine($"Skybox: {AssetDatabase.GetAssetPath(skybox)} (Tint 0.5, Rotation {options.rotation}, Exposure {options.exposure * options.strength})");

            SetUpScene(skybox);
            report.AppendLine(SetUpSun(sun, options));

            if (options.postProcessing)
                report.AppendLine(SetUpPostProcessing());
            if (options.ambientOcclusion)
                report.AppendLine(SetUpAmbientOcclusion(options));
            report.AppendLine(InstallAmbientScript());

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            if (options.bake)
            {
                Lightmapping.BakeAsync();
                report.AppendLine("Lighting bake started: the ambient light and reflections come from it.");
            }
            else
                report.AppendLine("Generate Lighting (Window > Rendering > Lighting) to update the ambient light and reflections.");
            return report.ToString();
        }

        /// <summary>The original HDRI, also when given the clamped copy a previous run made.</summary>
        public static Cubemap SourceOf(Cubemap hdri)
        {
            if (hdri == null)
                return null;
            string path = AssetDatabase.GetAssetPath(hdri);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!name.EndsWith(ClampedSuffix, StringComparison.Ordinal))
                return hdri;
            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string original = name.Substring(0, name.Length - ClampedSuffix.Length);
            foreach (string extension in new[] { ".exr", ".hdr" })
            {
                var found = AssetDatabase.LoadAssetAtPath<Cubemap>($"{folder}/{original}{extension}");
                if (found != null)
                    return found;
            }
            return hdri;
        }

        /// <summary>
        /// Writes the HDRI clamped at the threshold next to the source and measures what the clamp
        /// took out: its irradiance and the direction it comes from in Unity's world.
        /// </summary>
        private static Cubemap ClampHdri(Cubemap source, float threshold, float rotation, out Sun sun)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            Color[] pixels = ReadPixels(sourcePath, out int width, out int height);

            double red = 0, green = 0, blue = 0;
            Vector3 direction = Vector3.zero;
            for (int y = 0; y < height; y++)
            {
                double latitude = ((y + 0.5) / height - 0.5) * Math.PI;
                double solidAngle = Math.Cos(latitude) * (Math.PI / height) * (2 * Math.PI / width);
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    Color c = pixels[i];
                    float peak = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
                    if (peak <= threshold)
                        continue;
                    red += Mathf.Max(c.r - threshold, 0f) * solidAngle;
                    green += Mathf.Max(c.g - threshold, 0f) * solidAngle;
                    blue += Mathf.Max(c.b - threshold, 0f) * solidAngle;
                    double longitude = ((x + 0.5) / width - 0.5) * 2 * Math.PI;
                    direction += SkyDirection(longitude, latitude, rotation) * (float)((peak - threshold) * solidAngle);
                    pixels[i] = new Color(Mathf.Min(c.r, threshold), Mathf.Min(c.g, threshold), Mathf.Min(c.b, threshold), c.a);
                }
            }
            sun = new Sun
            {
                linearIrradiance = new Color((float)red, (float)green, (float)blue),
                towardSun = direction.normalized,
            };

            string folder = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            string clampedPath = $"{folder}/{Path.GetFileNameWithoutExtension(sourcePath)}{ClampedSuffix}.exr";
            var texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
            texture.SetPixels(pixels);
            File.WriteAllBytes(Path.GetFullPath(clampedPath), texture.EncodeToEXR(Texture2D.EXRFlags.CompressZIP));
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(clampedPath);

            var sourceImporter = (TextureImporter)AssetImporter.GetAtPath(sourcePath);
            var clampedImporter = (TextureImporter)AssetImporter.GetAtPath(clampedPath);
            var settings = new TextureImporterSettings();
            sourceImporter.ReadTextureSettings(settings);
            clampedImporter.SetTextureSettings(settings);
            clampedImporter.textureCompression = sourceImporter.textureCompression;
            clampedImporter.maxTextureSize = sourceImporter.maxTextureSize;
            clampedImporter.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Cubemap>(clampedPath);
        }

        /// <summary>
        /// Reads the source as a flat float image through a temporary copy, since the HDRI itself is
        /// imported as a compressed cubemap that can't be read back.
        /// </summary>
        private static Color[] ReadPixels(string sourcePath, out int width, out int height)
        {
            string tempPath = $"Assets/BlenderLookTemp{Path.GetExtension(sourcePath)}";
            AssetDatabase.CopyAsset(sourcePath, tempPath);
            try
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(tempPath);
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.sRGBTexture = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 16384;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                TextureImporterPlatformSettings platform = importer.GetDefaultPlatformTextureSettings();
                platform.format = TextureImporterFormat.RGBAFloat;
                platform.maxTextureSize = 16384;
                importer.SetPlatformTextureSettings(platform);
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(tempPath);
                width = texture.width;
                height = texture.height;
                return texture.GetPixels();
            }
            finally
            {
                AssetDatabase.DeleteAsset(tempPath);
            }
        }

        /// <summary>
        /// Direction in Unity's world of an equirectangular pixel, with the skybox turned by
        /// <paramref name="rotation"/> degrees the way Skybox/Cubemap's Rotation turns it. At 90
        /// degrees the sky lines up with Blender's world for models exported with the default FBX
        /// settings.
        /// </summary>
        private static Vector3 SkyDirection(double longitude, double latitude, float rotation)
        {
            float cosLat = (float)Math.Cos(latitude);
            var texel = new Vector3(-(float)Math.Sin(longitude) * cosLat, (float)Math.Sin(latitude), -(float)Math.Cos(longitude) * cosLat);
            float radians = rotation * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
            return new Vector3(cos * texel.x + sin * texel.z, texel.y, -sin * texel.x + cos * texel.z);
        }

        private static Material SetUpSkybox(BlenderLookOptions options, Cubemap source, Cubemap clamped)
        {
            Material skybox = options.skybox;
            if (skybox == null)
            {
                string sourcePath = AssetDatabase.GetAssetPath(source);
                string path = $"{Path.GetDirectoryName(sourcePath)?.Replace('\\', '/')}/{Path.GetFileNameWithoutExtension(sourcePath)} Blender.mat";
                skybox = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (skybox == null)
                {
                    skybox = new Material(Shader.Find("Skybox/Cubemap"));
                    AssetDatabase.CreateAsset(skybox, path);
                }
                options.skybox = skybox;
            }
            Undo.RecordObject(skybox, "Blender Look");
            // Skybox/Cubemap doubles its Tint, so 0.5 grey leaves the HDRI at its own brightness.
            skybox.SetTexture("_Tex", clamped);
            skybox.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 0.5f));
            skybox.SetFloat("_Rotation", options.rotation);
            skybox.SetFloat("_Exposure", options.exposure * options.strength);
            EditorUtility.SetDirty(skybox);
            return skybox;
        }

        private static void SetUpScene(Material skybox)
        {
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;
        }

        /// <summary>
        /// Blender lights diffuse surfaces with albedo / pi times the irradiance, Unity with albedo
        /// times the light's intensity, so the intensity is the irradiance over pi.
        /// </summary>
        private static string SetUpSun(Sun sun, BlenderLookOptions options)
        {
            Color irradiance = sun.linearIrradiance * (options.strength * options.exposure / Mathf.PI);
            float intensity = Mathf.Max(irradiance.r, Mathf.Max(irradiance.g, irradiance.b));
            Light light = FindSun();
            if (intensity <= 0f)
            {
                if (light != null)
                {
                    Undo.RecordObject(light.gameObject, "Blender Look");
                    light.gameObject.SetActive(false);
                }
                return "No pixel above the sun threshold: no sun light, and the scene's directional light was turned off.";
            }

            if (light == null)
            {
                var go = new GameObject(SunName);
                Undo.RegisterCreatedObjectUndo(go, "Blender Look");
                light = go.AddComponent<Light>();
            }
            Undo.RecordObject(light, "Blender Look");
            Undo.RecordObject(light.transform, "Blender Look");
            Undo.RecordObject(light.gameObject, "Blender Look");
            light.gameObject.SetActive(true);
            light.type = LightType.Directional;
            light.useColorTemperature = false;
            light.color = (irradiance / intensity).gamma;
            light.intensity = intensity;
            light.shadows = options.sunShadows ? LightShadows.Soft : LightShadows.None;
            light.transform.rotation = Quaternion.LookRotation(-sun.towardSun);
            RenderSettings.sun = light;
            Vector3 angles = Quaternion.LookRotation(sun.towardSun).eulerAngles;
            return $"Sun: {light.name}, intensity {intensity:F3}, {Mathf.DeltaAngle(0f, -angles.x):F0} degrees high, heading {angles.y:F0}";
        }

        private static Light FindSun()
        {
            if (RenderSettings.sun != null)
                return RenderSettings.sun;
            var scene = EditorSceneManager.GetActiveScene();
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
                if (light.type == LightType.Directional && light.gameObject.scene == scene)
                    return light;
            return null;
        }

        /// <summary>Standard view transform: no tonemapping, no vignette.</summary>
        private static string SetUpPostProcessing()
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline))
                return "Post-processing skipped: the project doesn't use URP.";
            VolumeProfile profile = pipeline.volumeProfile;
            if (profile == null)
                profile = GraphicsSettings.GetRenderPipelineSettings<URPDefaultVolumeProfileSettings>()?.volumeProfile;
            if (profile == null)
                return "Post-processing skipped: no Volume Profile on the URP asset or in Graphics settings.";

            Undo.RecordObject(profile, "Blender Look");
            Get<Tonemapping>(profile).mode.Override(TonemappingMode.None);
            Get<Vignette>(profile).intensity.Override(0f);
            EditorUtility.SetDirty(profile);

            var report = new StringBuilder($"Post-processing: Tonemapping None and Vignette 0 in {AssetDatabase.GetAssetPath(profile)}");
            foreach (Volume volume in Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
                if (volume.sharedProfile != null && volume.sharedProfile.TryGet(out Tonemapping tonemapping)
                    && tonemapping.active && tonemapping.mode.overrideState && tonemapping.mode.value != TonemappingMode.None)
                    report.Append($"\nThe Volume {volume.name} still sets Tonemapping {tonemapping.mode.value}, over the profile above.");
            return report.ToString();
        }

        private static T Get<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>();
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            component.active = true;
            return component;
        }

        /// <summary>
        /// EEVEE's horizon scan only darkens small gaps. URP's SSAO with a large radius darkens
        /// whole sides of a small model, so the radius is kept to the size of those gaps.
        /// </summary>
        private static string SetUpAmbientOcclusion(BlenderLookOptions options)
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset pipeline))
                return "Ambient occlusion skipped: the project doesn't use URP.";
            var changed = new List<string>();
            foreach (ScriptableRendererData data in pipeline.rendererDataList)
            {
                if (data == null)
                    continue;
                foreach (ScriptableRendererFeature feature in data.rendererFeatures)
                {
                    if (feature == null || feature.GetType().Name != "ScreenSpaceAmbientOcclusion")
                        continue;
                    var serialized = new SerializedObject(feature);
                    serialized.FindProperty("m_Settings.Radius").floatValue = options.aoRadius;
                    serialized.FindProperty("m_Settings.Intensity").floatValue = options.aoIntensity;
                    serialized.ApplyModifiedProperties();
                    feature.SetActive(true);
                    EditorUtility.SetDirty(feature);
                    EditorUtility.SetDirty(data);
                    changed.Add(data.name);
                }
            }
            return changed.Count > 0
                ? $"Ambient occlusion: radius {options.aoRadius}, intensity {options.aoIntensity} in {string.Join(", ", changed)}"
                : "Ambient occlusion skipped: no Screen Space Ambient Occlusion feature on the URP renderers.";
        }

        /// <summary>Adds the L1 ambient script to the project, unless it already has one.</summary>
        private static string InstallAmbientScript()
        {
            foreach (string guid in AssetDatabase.FindAssets($"{AmbientProbeL1Template.ClassName} t:MonoScript"))
            {
                string existing = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(existing) == AmbientProbeL1Template.ClassName)
                    return $"L1 ambient light: the project already has {existing}.";
            }
            Directory.CreateDirectory(Path.GetFullPath(ScriptFolder));
            string path = $"{ScriptFolder}/{AmbientProbeL1Template.ClassName}.cs";
            File.WriteAllText(Path.GetFullPath(path), AmbientProbeL1Template.Source);
            AssetDatabase.Refresh();
            return $"L1 ambient light: added {path}.";
        }
    }
}
