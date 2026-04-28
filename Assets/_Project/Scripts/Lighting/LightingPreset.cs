using UnityEngine;

namespace KaleidoscopeEngine.Lighting
{
    [CreateAssetMenu(
        fileName = "LightingPreset",
        menuName = "Kaleidoscope Engine/Lighting/Lighting Preset")]
    public sealed class LightingPreset : ScriptableObject
    {
        public string displayName = "Jewelry Studio";
        public Color keyColor = new Color(1f, 0.95f, 0.86f);
        public Color fillColor = new Color(0.55f, 0.72f, 1f);
        public Color rimColor = new Color(0.7f, 0.9f, 1f);
        public Color accentColor = new Color(1f, 0.72f, 0.45f);
        [Range(0f, 8f)] public float keyIntensity = 2.4f;
        [Range(0f, 8f)] public float fillIntensity = 0.9f;
        [Range(0f, 8f)] public float rimIntensity = 1.3f;
        [Range(0f, 8f)] public float accentIntensity = 1.6f;
        [Range(0f, 8f)] public float movingLightIntensity = 1.4f;
        [Range(0f, 4f)] public float bloomIntensity = 0.45f;
        [Range(-2f, 2f)] public float exposureCompensation = 0f;
    }
}
