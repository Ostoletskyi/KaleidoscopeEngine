using System.Collections.Generic;
using UnityEngine;

namespace KaleidoscopeEngine.Materials
{
    public static class GemstoneMaterialBootstrap
    {
        public static List<GemstoneMaterialProfile> CreateDefaultProfiles()
        {
            return new List<GemstoneMaterialProfile>
            {
                Profile("opal", "Opal", new Color(1f, 0.94f, 0.82f, 1f), 0.42f, 0.78f, 1.18f, new Color(0.72f, 0.82f, 1f), 0.08f, new Color(1f, 0.64f, 0.9f), 1.55f, new Color(0.58f, 0.82f, 1f), 0.22f, true, 0.18f, 1.25f, 13f),
                Profile("ruby", "Ruby", new Color(0.82f, 0.01f, 0.035f, 1f), 0.45f, 0.92f, 1.42f, new Color(0.18f, 0f, 0.012f), 1.25f, new Color(1f, 0.08f, 0.03f), 0.28f, new Color(1f, 0.02f, 0.015f), 0.18f, true, 0.12f, 1.05f, 24f),
                Profile("emerald", "Emerald", new Color(0.01f, 0.62f, 0.22f, 1f), 0.42f, 0.82f, 1.34f, new Color(0f, 0.12f, 0.04f), 1.12f, new Color(0.12f, 0.8f, 0.32f), 0.42f, new Color(0.02f, 0.42f, 0.14f), 0.08f, true, 0.18f, 0.75f, 18f),
                Profile("amethyst", "Amethyst", new Color(0.55f, 0.15f, 0.95f, 1f), 0.45f, 0.86f, 1.32f, new Color(0.13f, 0.02f, 0.22f), 0.72f, new Color(0.72f, 0.3f, 1f), 0.25f, new Color(0.35f, 0.08f, 0.9f), 0.12f, true, 0.1f, 0.7f, 18f),
                Profile("quartz", "Quartz", new Color(0.82f, 0.94f, 1f, 1f), 0.68f, 0.99f, 1.48f, new Color(0.22f, 0.42f, 0.62f), 0.28f, new Color(0.72f, 0.9f, 1f), 0.2f, new Color(0.7f, 0.9f, 1f), 0.025f, false, 0.035f, 2.05f, 42f),
                Profile("glass_fragment", "Glass Fragment", new Color(0.22f, 0.86f, 1f, 1f), 0.82f, 1f, 1.52f, new Color(0.01f, 0.08f, 0.16f), 0.75f, new Color(0.34f, 0.95f, 1f), 0.08f, new Color(0.18f, 0.86f, 1f), 0.015f, false, 0.01f, 2.55f, 58f),
                Profile("micro_particle", "Micro Particle", new Color(0.86f, 0.92f, 1f, 1f), 0.18f, 0.95f, 1.18f, new Color(0.22f, 0.25f, 0.34f), 0.18f, new Color(1f, 1f, 1f), 0.15f, new Color(0.8f, 0.9f, 1f), 0.06f, true, 0.04f, 2.25f, 52f),
                Profile("dust", "Dust", new Color(0.72f, 0.76f, 0.82f, 1f), 0.12f, 0.72f, 1.02f, new Color(0.24f, 0.25f, 0.28f), 0.12f, new Color(0.9f, 0.92f, 1f), 0.12f, new Color(0.8f, 0.84f, 1f), 0.05f, false, 0f, 1.15f, 38f)
            };
        }

        private static GemstoneMaterialProfile Profile(
            string id,
            string displayName,
            Color baseColor,
            float transparency,
            float smoothness,
            float fakeIOR,
            Color absorptionColor,
            float absorptionStrength,
            Color scatterColor,
            float scatterStrength,
            Color emissionColor,
            float emissionStrength,
            bool emission,
            float roughnessVariation,
            float sparkleStrength,
            float sparkleScale)
        {
            GemstoneMaterialProfile profile = ScriptableObject.CreateInstance<GemstoneMaterialProfile>();
            profile.id = id;
            profile.displayName = displayName;
            profile.baseColor = baseColor;
            profile.transparency = transparency;
            profile.smoothness = smoothness;
            profile.fakeIOR = fakeIOR;
            profile.absorptionColor = absorptionColor;
            profile.absorptionStrength = absorptionStrength;
            profile.internalScatterColor = scatterColor;
            profile.internalScatterStrength = scatterStrength;
            profile.emissionColor = emissionColor;
            profile.emissionStrength = emissionStrength;
            profile.useEmission = emission;
            profile.roughnessVariation = roughnessVariation;
            profile.sparkleStrength = sparkleStrength;
            profile.sparkleScale = sparkleScale;
            profile.specularIntensity = Mathf.Clamp(0.75f + sparkleStrength * 0.35f + smoothness * 0.25f, 0.6f, 2f);
            profile.fakeDispersion = Mathf.Clamp01(sparkleStrength * 0.25f);
            profile.facetHighlightBoost = Mathf.Clamp(sparkleStrength * 0.55f + smoothness * 0.35f, 0.2f, 2.4f);
            profile.sparkleResponse = Mathf.Clamp(sparkleStrength, 0.15f, 3f);
            profile.causticResponse = Mathf.Clamp01(transparency * 0.8f + sparkleStrength * 0.12f);
            profile.edgeGlowStrength = Mathf.Clamp01(fakeIOR * 0.18f + transparency * 0.22f);
            profile.fresnelPower = Mathf.Lerp(4.6f, 1.8f, Mathf.Clamp01(transparency));
            profile.emissionPulseStrength = emission ? Mathf.Clamp(emissionStrength * 1.8f + sparkleStrength * 0.08f, 0.05f, 1.2f) : Mathf.Clamp(sparkleStrength * 0.025f, 0f, 0.18f);
            return profile;
        }
    }
}
