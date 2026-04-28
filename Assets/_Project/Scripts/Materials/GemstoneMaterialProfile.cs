using UnityEngine;

namespace KaleidoscopeEngine.Materials
{
    [CreateAssetMenu(
        fileName = "GemstoneMaterialProfile",
        menuName = "Kaleidoscope Engine/Materials/Gemstone Material Profile")]
    public sealed class GemstoneMaterialProfile : ScriptableObject
    {
        [Header("Identity")]
        public string id = "opal";
        public string displayName = "Opal";

        [Header("Surface")]
        public Color baseColor = Color.white;
        [Range(0f, 1f)] public float transparency = 0.35f;
        [Range(0f, 1f)] public float smoothness = 0.82f;
        [Range(0f, 1f)] public float metallic;
        [Range(0f, 2f)] public float specularIntensity = 1f;

        [Header("Fake Optics")]
        [Range(1f, 2.5f)] public float fakeIOR = 1.45f;
        [Range(0f, 1f)] public float fakeDispersion = 0.25f;
        public Color absorptionColor = Color.black;
        [Range(0f, 2f)] public float absorptionStrength = 0.5f;
        public Color internalScatterColor = Color.white;
        [Range(0f, 2f)] public float internalScatterStrength = 0.25f;

        [Header("Glow")]
        public bool useEmission;
        public Color emissionColor = Color.black;
        [Range(0f, 4f)] public float emissionStrength = 0.15f;

        [Header("Sparkle")]
        [Range(0f, 3f)] public float sparkleStrength = 0.5f;
        [Range(0.1f, 60f)] public float sparkleScale = 16f;
        [Range(0f, 1f)] public float roughnessVariation = 0.08f;
        [Range(0f, 3f)] public float facetHighlightBoost = 0.6f;
        [Range(0f, 3f)] public float sparkleResponse = 0.6f;
        [Range(0f, 2f)] public float causticResponse = 0.25f;
        [Range(0f, 2f)] public float edgeGlowStrength = 0.15f;
        [Range(0.5f, 8f)] public float fresnelPower = 3f;
        [Range(0f, 2f)] public float emissionPulseStrength = 0.25f;

        [Header("Material")]
        public int renderQueueOffset;
        public Material materialTemplate;
    }
}
