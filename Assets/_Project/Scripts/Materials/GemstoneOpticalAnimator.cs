using UnityEngine;

namespace KaleidoscopeEngine.Materials
{
    [DisallowMultipleComponent]
    public sealed class GemstoneOpticalAnimator : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private GemstoneMaterialProfile profile;

        [Header("Animation")]
        [SerializeField] private bool sparkleEnabled = true;
        [SerializeField] private float shimmerSpeed = 1.6f;
        [SerializeField] private float shimmerAmount = 0.08f;
        [SerializeField] private float emissionPulseAmount = 0.25f;
        [SerializeField] private float noiseScale = 3.7f;
        [SerializeField] private float lightReactionStrength = 0.65f;

        private MaterialPropertyBlock propertyBlock;
        private Color baseColor;
        private Color emissionColor;
        private float phase;
        private Light[] lights;

        public float CurrentSparkle { get; private set; }

        public void Configure(Renderer renderer, GemstoneMaterialProfile materialProfile, System.Random random)
        {
            EnsurePropertyBlock();
            targetRenderer = renderer;
            profile = materialProfile;
            phase = (float)random.NextDouble() * 1000f;
            baseColor = profile != null ? profile.baseColor : Color.white;
            emissionColor = profile != null ? profile.emissionColor : Color.black;
            lights = FindObjectsOfType<Light>();
        }

        private void Awake()
        {
            EnsurePropertyBlock();
        }

        private void LateUpdate()
        {
            if (!sparkleEnabled || targetRenderer == null || profile == null)
            {
                return;
            }

            EnsurePropertyBlock();

            float lightExposure = EstimateLightExposure();
            float shimmer = Mathf.PerlinNoise(Time.time * shimmerSpeed + phase, phase * 0.013f) - 0.5f;
            float glint = Mathf.PerlinNoise(
                transform.position.x * noiseScale + Time.time * shimmerSpeed + phase,
                transform.position.z * noiseScale - Time.time * shimmerSpeed * 0.31f);

            CurrentSparkle = Mathf.Clamp01(glint * profile.sparkleStrength * lightExposure);
            Color animatedBase = Color.Lerp(baseColor, Color.white, CurrentSparkle * shimmerAmount);
            animatedBase.a = Mathf.Clamp01(baseColor.a);

            Color animatedEmission = emissionColor *
                (profile.useEmission ? profile.emissionStrength * (1f + CurrentSparkle * emissionPulseAmount) : 0f);

            targetRenderer.GetPropertyBlock(propertyBlock);
            SetColor("_BaseColor", animatedBase);
            SetColor("_Color", animatedBase);
            SetColor("_EmissiveColor", animatedEmission);
            SetFloat("_Smoothness", Mathf.Clamp01(profile.smoothness + shimmer * profile.roughnessVariation));
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private float EstimateLightExposure()
        {
            if (lights == null || lights.Length == 0)
            {
                return 1f;
            }

            float exposure = 0.25f;
            Vector3 normal = transform.up;
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null || !light.enabled)
                {
                    continue;
                }

                Vector3 direction = light.type == LightType.Directional
                    ? -light.transform.forward
                    : (light.transform.position - transform.position).normalized;

                float facing = Mathf.Clamp01(Vector3.Dot(normal, direction) * 0.5f + 0.5f);
                exposure += facing * light.intensity * lightReactionStrength * 0.08f;
            }

            return Mathf.Clamp01(exposure);
        }

        private void SetColor(string propertyName, Color value)
        {
            if (targetRenderer.sharedMaterial != null && targetRenderer.sharedMaterial.HasProperty(propertyName))
            {
                propertyBlock.SetColor(propertyName, value);
            }
        }

        private void SetFloat(string propertyName, float value)
        {
            if (targetRenderer.sharedMaterial != null && targetRenderer.sharedMaterial.HasProperty(propertyName))
            {
                propertyBlock.SetFloat(propertyName, value);
            }
        }
    }
}
