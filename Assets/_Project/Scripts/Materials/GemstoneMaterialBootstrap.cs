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
                Profile("opal", "Opal", new Color(0.92f, 0.98f, 1f, 1f), 0.18f, 0.7f, 1.15f, new Color(0.45f, 0.7f, 1f), 0.18f, new Color(1f, 0.82f, 0.55f), 0.35f, new Color(0.8f, 0.95f, 1f), 0.22f, true, 0.18f, 0.55f, 12f),
                Profile("ruby", "Ruby", new Color(0.9f, 0.015f, 0.045f, 1f), 0.42f, 0.9f, 1.35f, new Color(0.23f, 0f, 0.015f), 0.95f, new Color(1f, 0.08f, 0.03f), 0.18f, new Color(1f, 0.03f, 0.02f), 0.22f, true, 0.22f, 0.9f, 20f),
                Profile("emerald", "Emerald", new Color(0.015f, 0.72f, 0.26f, 1f), 0.38f, 0.78f, 1.28f, new Color(0f, 0.16f, 0.06f), 0.9f, new Color(0.16f, 0.8f, 0.38f), 0.35f, new Color(0.02f, 0.5f, 0.16f), 0.1f, true, 0.12f, 0.55f, 16f),
                Profile("amethyst", "Amethyst", new Color(0.55f, 0.15f, 0.95f, 1f), 0.45f, 0.86f, 1.32f, new Color(0.13f, 0.02f, 0.22f), 0.72f, new Color(0.72f, 0.3f, 1f), 0.25f, new Color(0.35f, 0.08f, 0.9f), 0.12f, true, 0.1f, 0.7f, 18f),
                Profile("quartz", "Quartz", new Color(0.95f, 0.98f, 1f, 1f), 0.68f, 0.93f, 1.46f, new Color(0.62f, 0.72f, 0.82f), 0.24f, new Color(0.9f, 0.96f, 1f), 0.42f, new Color(0.72f, 0.9f, 1f), 0.05f, false, 0f, 1.25f, 28f),
                Profile("glass_fragment", "Glass Fragment", new Color(0.48f, 0.95f, 1f, 1f), 0.72f, 0.96f, 1.5f, new Color(0.04f, 0.16f, 0.22f), 0.42f, new Color(0.65f, 0.96f, 1f), 0.28f, new Color(0.28f, 0.9f, 1f), 0.04f, false, 0f, 1.55f, 36f),
                Profile("micro_particle", "Micro Particle", new Color(0.82f, 0.9f, 1f, 1f), 0.26f, 0.88f, 1.1f, new Color(0.25f, 0.28f, 0.34f), 0.22f, new Color(1f, 1f, 1f), 0.18f, new Color(0.8f, 0.9f, 1f), 0.08f, true, 0.08f, 1.7f, 44f),
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
            profile.fakeDispersion = Mathf.Clamp01(sparkleStrength * 0.25f);
            return profile;
        }
    }
}
