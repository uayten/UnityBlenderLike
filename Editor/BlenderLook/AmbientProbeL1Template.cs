namespace UnityBlenderLike
{
    /// <summary>
    /// Source of the runtime script Blender Look adds to the project. It has to live in the
    /// project, not in this package: it runs in builds, and the package is meant to stay on one
    /// machine.
    /// </summary>
    internal static class AmbientProbeL1Template
    {
        public const string ClassName = "AmbientProbeL1";

        public const string Source = @"using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Drops the L2 band of the ambient probe so environment lighting matches Blender's EEVEE,
// which lights diffuse surfaces from L1 spherical harmonics. L2 darkens downward-facing surfaces
// much more than EEVEE does. Unity reloads the probe on scene load and after a lighting bake,
// so the trim is reapplied whenever that happens, in Play Mode and in the Editor.
// Added by Unity Blender Like's Blender Look.
public static class AmbientProbeL1
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitRuntime()
    {
        SceneManager.sceneLoaded += (_, _) => Apply();
        SceneManager.activeSceneChanged += (_, _) => Apply();
    }

#if UNITY_EDITOR
    // The Editor reloads the probe at moments no event reports (after a domain reload, for one),
    // so it is checked every update; the check is a few float reads.
    [InitializeOnLoadMethod]
    static void InitEditor()
    {
        EditorApplication.update += () =>
        {
            if (HasL2(RenderSettings.ambientProbe))
                Apply();
        };
    }
#endif

    public static void Apply()
    {
        SphericalHarmonicsL2 probe = RenderSettings.ambientProbe;
        for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 4; coefficient < 9; coefficient++)
                probe[channel, coefficient] = 0f;
        RenderSettings.ambientProbe = probe;
    }

    static bool HasL2(SphericalHarmonicsL2 probe)
    {
        for (int channel = 0; channel < 3; channel++)
            for (int coefficient = 4; coefficient < 9; coefficient++)
                if (probe[channel, coefficient] != 0f)
                    return true;
        return false;
    }
}
";
    }
}
