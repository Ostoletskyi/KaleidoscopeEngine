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
        public GemstoneMaterialProfile Profile => profile;

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

            float profileSparkle = GetProfileSparkleMultiplier();
            CurrentSparkle = Mathf.Clamp01(glint * profile.sparkleStrength * profile.sparkleResponse * profileSparkle * lightExposure);
            Color pearlyTint = GetProfileScatterTint() * Mathf.Clamp01(profile.internalScatterStrength);
            Color highlightTint = GetProfileHighlightTint();
            Color animatedBase = Color.Lerp(baseColor, pearlyTint, profile.fakeDispersion * (0.25f + CurrentSparkle * 0.5f));
            animatedBase = Color.Lerp(animatedBase, highlightTint, CurrentSparkle * shimmerAmount * GetProfileHighlightAmount());
            animatedBase.a = Mathf.Clamp01(baseColor.a);
            Color animatedSpecular = GetProfileSpecularTint() * (profile.specularIntensity + CurrentSparkle * GetProfileSpecularBoost() * profile.facetHighlightBoost);

            Color animatedEmission = emissionColor *
                (profile.useEmission ? profile.emissionStrength * (1f + CurrentSparkle * emissionPulseAmount * profile.emissionPulseStrength) : 0f);
            animatedEmission += GetProfileReactiveGlow(CurrentSparkle, lightExposure);

            targetRenderer.GetPropertyBlock(propertyBlock);
            SetColor("_BaseColor", animatedBase);
            SetColor("_Color", animatedBase);
            SetColor("_EmissiveColor", animatedEmission);
            SetColor("_SpecularColor", animatedSpecular);
            SetFloat("_Smoothness", Mathf.Clamp01(profile.smoothness + shimmer * profile.roughnessVariation));
            SetFloat("_CoatMask", GetProfileCoat(CurrentSparkle));
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

        private float GetProfileSparkleMultiplier()
        {
            if (profile.id == "opal")
            {
                return 0.75f;
            }

            if (profile.id == "quartz")
            {
                return 1.35f;
            }

            if (profile.id == "glass_fragment")
            {
                return 1.65f;
            }

            return 1f;
        }

        private Color GetProfileScatterTint()
        {
            if (profile.id == "opal")
            {
                float hueShift = Mathf.PerlinNoise(Time.time * 0.28f + phase, phase * 0.07f);
                return Color.Lerp(new Color(0.55f, 0.85f, 1f, 1f), new Color(1f, 0.58f, 0.9f, 1f), hueShift);
            }

            if (profile.id == "quartz")
            {
                return new Color(0.62f, 0.84f, 1f, 1f);
            }

            if (profile.id == "glass_fragment")
            {
                return new Color(0.2f, 0.95f, 1f, 1f);
            }

            return profile.internalScatterColor;
        }

        private Color GetProfileHighlightTint()
        {
            if (profile.id == "opal")
            {
                return new Color(1f, 0.86f, 1f, 1f);
            }

            if (profile.id == "quartz")
            {
                return new Color(1.2f, 1.35f, 1.5f, 1f);
            }

            if (profile.id == "glass_fragment")
            {
                return new Color(0.75f, 1.35f, 1.6f, 1f);
            }

            return Color.white;
        }

        private Color GetProfileSpecularTint()
        {
            if (profile.id == "opal")
            {
                return new Color(0.82f, 0.96f, 1.15f, 1f);
            }

            if (profile.id == "quartz")
            {
                return new Color(1.15f, 1.3f, 1.45f, 1f);
            }

            if (profile.id == "glass_fragment")
            {
                return new Color(1.1f, 1.55f, 1.75f, 1f);
            }

            if (profile.id == "micro_particle" || profile.id == "dust")
            {
                return new Color(1.1f, 1.18f, 1.28f, 1f);
            }

            return Color.white;
        }

        private float GetProfileSpecularBoost()
        {
            if (profile.id == "opal")
            {
                return 0.28f;
            }

            if (profile.id == "quartz")
            {
                return 0.75f;
            }

            if (profile.id == "glass_fragment")
            {
                return 1.05f;
            }

            if (profile.id == "micro_particle" || profile.id == "dust")
            {
                return 1.25f;
            }

            return 0.45f;
        }

        private float GetProfileHighlightAmount()
        {
            if (profile.id == "opal")
            {
                return 0.55f;
            }

            if (profile.id == "quartz")
            {
                return 1.25f;
            }

            if (profile.id == "glass_fragment")
            {
                return 1.6f;
            }

            return 1f;
        }

        private Color GetProfileReactiveGlow(float sparkle, float exposure)
        {
            if (profile.id == "opal")
            {
                return new Color(0.36f, 0.58f, 1f, 1f) * (sparkle * exposure * 0.08f);
            }

            if (profile.id == "quartz")
            {
                return new Color(0.55f, 0.82f, 1f, 1f) * (sparkle * exposure * 0.025f);
            }

            if (profile.id == "glass_fragment")
            {
                return new Color(0.1f, 0.92f, 1f, 1f) * (sparkle * exposure * 0.035f);
            }

            return Color.black;
        }

        private float GetProfileCoat(float sparkle)
        {
            if (profile.id == "opal")
            {
                return Mathf.Lerp(0.22f, 0.38f, sparkle);
            }

            if (profile.id == "quartz")
            {
                return Mathf.Lerp(0.62f, 0.85f, sparkle);
            }

            if (profile.id == "glass_fragment")
            {
                return Mathf.Lerp(0.82f, 1f, sparkle);
            }

            return Mathf.Clamp01(profile.sparkleStrength * 0.22f + sparkle * 0.18f);
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
