using UnityEngine;

namespace KaleidoscopeEngine.Mirrors
{
    public enum KaleidoscopeQualityLevel
    {
        Minimal,
        Low,
        Medium,
        High,
        Ultra,
        Extreme
    }

    [System.Serializable]
    public struct KaleidoscopeQualityProfile
    {
        public KaleidoscopeQualityLevel level;
        public int renderTextureResolution;
        public bool hdrEnabled;
        public RenderTextureFormat renderTextureFormat;
        public FilterMode filterMode;
        public int anisotropicFiltering;
        public float mipBias;
        public bool useMipMaps;
        public int antiAliasingSamples;
        public float supersamplingFactor;
        public float textureScale;
        public float seamSmoothingQuality;
        public float opticalDistortionQuality;
        public float bloomQuality;
        public float taaQuality;
        public int sparkleCount;
        public float sparkleFrequency;
        public float sparkleIntensity;
        public float microDetailDensity;
        public float shardFieldMultiplier;
        public int opticalFillerParticles;
        public bool reflectiveDustLayer;
        public float sourceCoverageTarget;
        public bool dynamicEntropyBalancing;
        public float sourceOverscanFactor;
        public float edgeRecursionBlend;
        public float centerConvergenceStrength;
        public float radialContinuation;
        public float centerRecursionBlend;
        public float innerPatternPropagation;
        public float vignetteQuality;
        public float chromaticAberrationQuality;
        public int updateRateLimit;

        public string DisplayName => level.ToString();

        public int ColorBytesPerPixel
        {
            get
            {
                switch (renderTextureFormat)
                {
                    case RenderTextureFormat.ARGBFloat:
                        return 16;
                    case RenderTextureFormat.ARGBHalf:
                    case RenderTextureFormat.DefaultHDR:
                        return 8;
                    default:
                        return 4;
                }
            }
        }

        public int SafeAntiAliasingSamples => Mathf.Clamp(antiAliasingSamples <= 1 ? 1 : Mathf.NextPowerOfTwo(antiAliasingSamples), 1, 8);
        public int SafeAnisotropicFiltering => Mathf.Clamp(anisotropicFiltering, 0, 16);
        public float SafeSupersamplingFactor => Mathf.Clamp(supersamplingFactor, 1f, 2f);
        public float SafeTextureScale => Mathf.Clamp(textureScale, 0.5f, 1.5f);

        public static KaleidoscopeQualityProfile ForLevel(KaleidoscopeQualityLevel level)
        {
            switch (level)
            {
                case KaleidoscopeQualityLevel.Minimal:
                    return Create(
                        level,
                        512,
                        false,
                        RenderTextureFormat.Default,
                        FilterMode.Bilinear,
                        0,
                        0f,
                        false,
                        1,
                        1f,
                        1f,
                        0.65f,
                        0.55f,
                        0.35f,
                        0.25f,
                        18,
                        18f,
                        0.65f,
                        0.46f,
                        0.82f,
                        0,
                        false,
                        0.58f,
                        false,
                        1.04f,
                        0.16f,
                        0.22f,
                        0.18f,
                        0.12f,
                        0.12f,
                        0.45f,
                        0f,
                        30);

                case KaleidoscopeQualityLevel.Low:
                    return Create(
                        level,
                        1024,
                        true,
                        RenderTextureFormat.DefaultHDR,
                        FilterMode.Bilinear,
                        1,
                        -0.15f,
                        false,
                        1,
                        1f,
                        1f,
                        0.85f,
                        0.7f,
                        0.45f,
                        0.4f,
                        28,
                        22f,
                        0.75f,
                        0.56f,
                        0.95f,
                        10,
                        true,
                        0.66f,
                        true,
                        1.08f,
                        0.24f,
                        0.32f,
                        0.26f,
                        0.2f,
                        0.18f,
                        0.55f,
                        0.02f,
                        0);

                case KaleidoscopeQualityLevel.Medium:
                    return Create(
                        level,
                        2048,
                        true,
                        RenderTextureFormat.ARGBHalf,
                        FilterMode.Trilinear,
                        4,
                        -0.35f,
                        true,
                        2,
                        1f,
                        1f,
                        1.05f,
                        0.9f,
                        0.62f,
                        0.58f,
                        44,
                        28f,
                        0.85f,
                        0.66f,
                        1.1f,
                        24,
                        true,
                        0.74f,
                        true,
                        1.14f,
                        0.36f,
                        0.46f,
                        0.38f,
                        0.32f,
                        0.28f,
                        0.68f,
                        0.04f,
                        0);

                case KaleidoscopeQualityLevel.High:
                    return Create(
                        level,
                        3072,
                        true,
                        RenderTextureFormat.ARGBHalf,
                        FilterMode.Trilinear,
                        8,
                        -0.55f,
                        true,
                        4,
                        1f,
                        1f,
                        1.25f,
                        1.08f,
                        0.78f,
                        0.72f,
                        60,
                        34f,
                        0.95f,
                        0.74f,
                        1.24f,
                        38,
                        true,
                        0.82f,
                        true,
                        1.2f,
                        0.5f,
                        0.6f,
                        0.5f,
                        0.44f,
                        0.4f,
                        0.78f,
                        0.06f,
                        0);

                case KaleidoscopeQualityLevel.Ultra:
                    return Create(
                        level,
                        4096,
                        true,
                        RenderTextureFormat.ARGBHalf,
                        FilterMode.Trilinear,
                        12,
                        -0.7f,
                        true,
                        4,
                        1.25f,
                        1f,
                        1.45f,
                        1.25f,
                        0.9f,
                        0.85f,
                        76,
                        40f,
                        1.05f,
                        0.82f,
                        1.38f,
                        56,
                        true,
                        0.88f,
                        true,
                        1.28f,
                        0.62f,
                        0.74f,
                        0.64f,
                        0.56f,
                        0.52f,
                        0.86f,
                        0.08f,
                        0);

                default:
                    return Create(
                        KaleidoscopeQualityLevel.Extreme,
                        4096,
                        true,
                        RenderTextureFormat.ARGBFloat,
                        FilterMode.Trilinear,
                        16,
                        -0.85f,
                        true,
                        8,
                        1.5f,
                        1f,
                        1.7f,
                        1.45f,
                        1f,
                        1f,
                        96,
                        48f,
                        1.15f,
                        0.9f,
                        1.55f,
                        82,
                        true,
                        0.94f,
                        true,
                        1.36f,
                        0.74f,
                        0.86f,
                        0.76f,
                        0.68f,
                        0.64f,
                        0.94f,
                        0.12f,
                        0);
            }
        }

        private static KaleidoscopeQualityProfile Create(
            KaleidoscopeQualityLevel level,
            int renderTextureResolution,
            bool hdrEnabled,
            RenderTextureFormat renderTextureFormat,
            FilterMode filterMode,
            int anisotropicFiltering,
            float mipBias,
            bool useMipMaps,
            int antiAliasingSamples,
            float supersamplingFactor,
            float textureScale,
            float seamSmoothingQuality,
            float opticalDistortionQuality,
            float bloomQuality,
            float taaQuality,
            int sparkleCount,
            float sparkleFrequency,
            float sparkleIntensity,
            float microDetailDensity,
            float shardFieldMultiplier,
            int opticalFillerParticles,
            bool reflectiveDustLayer,
            float sourceCoverageTarget,
            bool dynamicEntropyBalancing,
            float sourceOverscanFactor,
            float edgeRecursionBlend,
            float centerConvergenceStrength,
            float radialContinuation,
            float centerRecursionBlend,
            float innerPatternPropagation,
            float vignetteQuality,
            float chromaticAberrationQuality,
            int updateRateLimit)
        {
            return new KaleidoscopeQualityProfile
            {
                level = level,
                renderTextureResolution = renderTextureResolution,
                hdrEnabled = hdrEnabled,
                renderTextureFormat = renderTextureFormat,
                filterMode = filterMode,
                anisotropicFiltering = anisotropicFiltering,
                mipBias = mipBias,
                useMipMaps = useMipMaps,
                antiAliasingSamples = antiAliasingSamples,
                supersamplingFactor = supersamplingFactor,
                textureScale = textureScale,
                seamSmoothingQuality = seamSmoothingQuality,
                opticalDistortionQuality = opticalDistortionQuality,
                bloomQuality = bloomQuality,
                taaQuality = taaQuality,
                sparkleCount = sparkleCount,
                sparkleFrequency = sparkleFrequency,
                sparkleIntensity = sparkleIntensity,
                microDetailDensity = microDetailDensity,
                shardFieldMultiplier = shardFieldMultiplier,
                opticalFillerParticles = opticalFillerParticles,
                reflectiveDustLayer = reflectiveDustLayer,
                sourceCoverageTarget = sourceCoverageTarget,
                dynamicEntropyBalancing = dynamicEntropyBalancing,
                sourceOverscanFactor = sourceOverscanFactor,
                edgeRecursionBlend = edgeRecursionBlend,
                centerConvergenceStrength = centerConvergenceStrength,
                radialContinuation = radialContinuation,
                centerRecursionBlend = centerRecursionBlend,
                innerPatternPropagation = innerPatternPropagation,
                vignetteQuality = vignetteQuality,
                chromaticAberrationQuality = chromaticAberrationQuality,
                updateRateLimit = updateRateLimit
            };
        }
    }
}
