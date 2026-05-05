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
        Extreme,
        NativeSmoothMax
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
        public bool centerAffectedByQuality;
        public float centerCleanRadius;
        public float centerCleanFeather;
        public float centerPatternContinuation;
        public float centerDetailBoost;
        public float centerSampleScale;
        public float centerReconstructionQuality;
        public KaleidoscopeCenterFillMode centerFillMode;
        public float vignetteQuality;
        public float chromaticAberrationQuality;
        public int updateRateLimit;

        public string DisplayName
        {
            get
            {
                switch (level)
                {
                    case KaleidoscopeQualityLevel.Minimal:
                        return "Ultra Low";
                    case KaleidoscopeQualityLevel.Ultra:
                        return "Very High";
                    case KaleidoscopeQualityLevel.Extreme:
                        return "Ultra";
                    case KaleidoscopeQualityLevel.NativeSmoothMax:
                        return "Native Smooth / Max";
                    default:
                        return level.ToString();
                }
            }
        }

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
                        0);

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
                        1.4f,
                        1.18f,
                        0.78f,
                        0.72f,
                        72,
                        36f,
                        1.04f,
                        0.8f,
                        1.32f,
                        52,
                        true,
                        0.86f,
                        true,
                        1.24f,
                        0.58f,
                        0.66f,
                        0.58f,
                        0.5f,
                        0.48f,
                        0.7f,
                        0.045f,
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

                case KaleidoscopeQualityLevel.Extreme:
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

                default:
                    return Create(
                        KaleidoscopeQualityLevel.NativeSmoothMax,
                        4096,
                        true,
                        RenderTextureFormat.ARGBHalf,
                        FilterMode.Trilinear,
                        16,
                        -0.9f,
                        true,
                        4,
                        2f,
                        1f,
                        1.75f,
                        1.5f,
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
            KaleidoscopeQualityProfile profile = new KaleidoscopeQualityProfile
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

            ApplyCenterQuality(ref profile);
            return profile;
        }

        private static void ApplyCenterQuality(ref KaleidoscopeQualityProfile profile)
        {
            profile.centerAffectedByQuality = true;
            switch (profile.level)
            {
                case KaleidoscopeQualityLevel.Minimal:
                    profile.centerCleanRadius = 0.13f;
                    profile.centerCleanFeather = 0.085f;
                    profile.centerPatternContinuation = 0.42f;
                    profile.centerDetailBoost = 0.08f;
                    profile.centerSampleScale = 0.86f;
                    profile.centerReconstructionQuality = 0.28f;
                    profile.centerFillMode = KaleidoscopeCenterFillMode.Clean;
                    break;
                case KaleidoscopeQualityLevel.Low:
                    profile.centerCleanRadius = 0.115f;
                    profile.centerCleanFeather = 0.075f;
                    profile.centerPatternContinuation = 0.56f;
                    profile.centerDetailBoost = 0.12f;
                    profile.centerSampleScale = 0.94f;
                    profile.centerReconstructionQuality = 0.42f;
                    profile.centerFillMode = KaleidoscopeCenterFillMode.MirrorContinuation;
                    break;
                case KaleidoscopeQualityLevel.Medium:
                    profile.centerCleanRadius = 0.095f;
                    profile.centerCleanFeather = 0.06f;
                    profile.centerPatternContinuation = 0.72f;
                    profile.centerDetailBoost = 0.18f;
                    profile.centerSampleScale = 1.02f;
                    profile.centerReconstructionQuality = 0.58f;
                    profile.centerFillMode = KaleidoscopeCenterFillMode.MirrorContinuation;
                    break;
                case KaleidoscopeQualityLevel.High:
                    profile.centerCleanRadius = 0.078f;
                    profile.centerCleanFeather = 0.045f;
                    profile.centerPatternContinuation = 0.86f;
                    profile.centerDetailBoost = 0.25f;
                    profile.centerSampleScale = 1.12f;
                    profile.centerReconstructionQuality = 0.76f;
                    profile.centerFillMode = KaleidoscopeCenterFillMode.SoftBlend;
                    break;
                case KaleidoscopeQualityLevel.Ultra:
                    profile.centerCleanRadius = 0.062f;
                    profile.centerCleanFeather = 0.036f;
                    profile.centerPatternContinuation = 0.94f;
                    profile.centerDetailBoost = 0.32f;
                    profile.centerSampleScale = 1.22f;
                    profile.centerReconstructionQuality = 0.9f;
                    profile.centerFillMode = KaleidoscopeCenterFillMode.SoftBlend;
                    break;
                case KaleidoscopeQualityLevel.Extreme:
                    profile.centerCleanRadius = 0.048f;
                    profile.centerCleanFeather = 0.028f;
                    profile.centerPatternContinuation = 1f;
                    profile.centerDetailBoost = 0.4f;
                    profile.centerSampleScale = 1.35f;
                    profile.centerReconstructionQuality = 1f;
                    profile.centerFillMode = KaleidoscopeCenterFillMode.SoftBlend;
                    break;
                default:
                    profile.centerCleanRadius = 0.048f;
                    profile.centerCleanFeather = 0.028f;
                    profile.centerPatternContinuation = 1f;
                    profile.centerDetailBoost = 0.4f;
                    profile.centerSampleScale = 1.35f;
                    profile.centerReconstructionQuality = 1f;
                    profile.centerFillMode = KaleidoscopeCenterFillMode.SoftBlend;
                    break;
            }
        }
    }
}
