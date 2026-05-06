using UnityEngine;

namespace KaleidoscopeEngine.Mirrors
{
    public enum KaleidoscopePrismMode
    {
        TwoMirror,
        ThreeMirrorTriangular,
        CustomRadial
    }

    public enum KaleidoscopeMaskMode
    {
        None,
        CircularEyepiece,
        HexagonalPrism,
        SoftVignette
    }

    public enum KaleidoscopeCenterMaskMode
    {
        PhysicalChamber,
        SoftOptical,
        Disabled
    }

    public enum KaleidoscopeShakeMode
    {
        Physical,
        UV,
        Optical,
        Hybrid
    }

    public enum KaleidoscopeBeautyPreset
    {
        WarmGlass,
        DeepJewel,
        NeonBloom,
        SoftOpal,
        DarkCathedral
    }

    public enum KaleidoscopeColorDepthMode
    {
        TwoColors,
        FourColors,
        EightColors,
        SixteenColors,
        ThirtyTwoColors,
        SixtyFourColors,
        OneHundredTwentyEightColors,
        TwoHundredFiftySixColors,
        FiveHundredTwelveColors,
        SixtyFiveThousandColors,
        FullColor
    }

    public enum EffectLevel
    {
        UltraLow,
        Low,
        Normal,
        High,
        UltraHigh
    }

    public enum KaleidoscopeCenterFillMode
    {
        Disabled,
        Clean,
        MirrorContinuation,
        SourceResample,
        RadialContinuation,
        SoftBlend
    }

    public enum KaleidoscopeSpinStabilityState
    {
        Stable,
        Fast,
        TooFastForComfort,
        AliasingRisk
    }

    public enum KaleidoscopeCenterArtifactOverrideMode
    {
        SampleCleanSource
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeMirrorController : MonoBehaviour
    {
        private const int MaxSegmentCount = 64;

        private static readonly int[] ValidSegmentCounts =
        {
            6, 8, 10, 12, 16,
            24, 32, 40, 48, 56, 64
        };

        private static readonly int[] ColorDepthSteps =
        {
            2,
            4,
            8,
            16,
            32,
            64,
            128,
            256,
            512,
            65535,
            16777216
        };

        private static readonly string[] ColorDepthDisplayNames =
        {
            "2 colors",
            "4 colors",
            "8 colors",
            "16 colors",
            "32 colors",
            "64 colors",
            "128 colors",
            "256 colors",
            "512 colors",
            "65535 colors",
            "16 million colors / full color"
        };

        [Header("Mirror")]
        [SerializeField] private bool useMirrorAngleMode = true;
        [SerializeField] private KaleidoscopePrismMode prismMode = KaleidoscopePrismMode.ThreeMirrorTriangular;
        [SerializeField, Range(5f, 180f)] private float mirrorAngleDegrees = 60f;
        [SerializeField, Range(1, MaxSegmentCount)] private int segmentCountManual = 6;
        [Tooltip("Static angle offset in degrees before runtime rotation is applied.")]
        [SerializeField] private float mirrorAngleOffset;
        [Tooltip("Automatic pattern rotation in degrees per second.")]
        [SerializeField] private float patternRotationSpeed = 5f;
        [SerializeField] private float defaultPatternRotationSpeed = 5f;
        [SerializeField] private float requestedPatternRotationSpeed = 5f;
        [SerializeField] private float effectivePatternRotationSpeed;
        [SerializeField] private float rotationAcceleration = 12f;
        [SerializeField] private float rotationDamping = 5f;
        [SerializeField] private bool patternSpinEnabled = true;
        [SerializeField] private float patternSpinSpeedDeg = 5f;
        [SerializeField] private float patternSpinAcceleration = 280f;
        [SerializeField] private float patternSpinDamping = 180f;
        [SerializeField] private float keyboardRotationTargetSpeed = 300f;
        [SerializeField] private float keyboardRotationHoldSeconds = 10f;
        [SerializeField] private float keyboardRotationBaselineRestoreSpeed = 35f;
        [SerializeField] private float spinSmoothingTime = 0.16f;
        [SerializeField] private float spinJerkLimit = 7200f;
        [SerializeField] private bool smoothStop = true;
        [SerializeField] private bool smoothResume = true;
        [SerializeField] private float minPatternSpinSpeedDeg = -1000f;
        [SerializeField] private float maxPatternSpinSpeedDeg = 1000f;
        [SerializeField] private float comfortPatternSpinSpeedLimit = 360f;
        [SerializeField, Range(0f, 1f)] private float rotationSmoothing = 0.82f;
        [SerializeField, Range(0.25f, 4f)] private float patternZoom = 1.18f;
        [SerializeField] private float requestedPatternZoom = 1.18f;
        [SerializeField] private float effectivePatternZoom = 1.18f;
        [SerializeField] private float zoomSpeed = 0.9f;
        [SerializeField] private float zoomMin = 0.45f;
        [SerializeField] private float zoomMax = 3.5f;
        [SerializeField] private float zoomSmoothing = 0.12f;
        [SerializeField, Range(-0.5f, 0.5f)] private float centerOffsetX;
        [SerializeField, Range(-0.5f, 0.5f)] private float centerOffsetY;
        [SerializeField, Range(-1f, 1f)] private float radialDistortion = 0.08f;
        [SerializeField, Range(0f, 0.25f)] private float edgeSoftness = 0.018f;
        [SerializeField, Range(0f, 0.25f)] private float seamSoftness = 0.025f;
        [SerializeField] private float seamAlignmentOffset;
        [SerializeField] private bool seamChromaticAberrationEnabled;
        [SerializeField, Range(0f, 0.01f)] private float seamChromaticAberration = 0.0012f;
        [SerializeField] private bool seamBlendingEnabled = true;
        [SerializeField, Range(0f, 1f)] private float seamBlendStrength = 0.8f;
        [SerializeField, Range(0f, 0.12f)] private float seamFeatherWidth = 0.04f;
        [SerializeField, Range(0f, 1f)] private float continuityCorrection = 0.32f;
        [SerializeField, Range(0f, 0.12f)] private float radialEdgeSoftness = 0.035f;
        [SerializeField] private bool seamAntialiasingEnabled = true;
        [SerializeField, Range(0f, 0.04f)] private float seamAAWidth = 0.012f;
        [SerializeField, Range(0f, 1f)] private float seamLineSuppression = 0.72f;
        [SerializeField] private bool showSectorBoundaries;
        [SerializeField] private Color boundaryDebugColor = new Color(1f, 0.25f, 0.08f, 1f);

        [Header("Center Composition")]
        [SerializeField, Range(0.4f, 2.5f)] private float centerScale = 1.05f;
        [SerializeField, Range(0f, 3f)] private float centerBrightness = 0.82f;
        [SerializeField, Range(0f, 1f)] private float centerVignette = 0.12f;
        [SerializeField, Range(0f, 1f)] private float centerStabilization = 0.72f;
        [SerializeField] private bool centerMaskEnabled = true;
        [SerializeField] private KaleidoscopeCenterMaskMode centerMaskMode = KaleidoscopeCenterMaskMode.PhysicalChamber;
        [SerializeField, Range(0.02f, 0.6f)] private float centerMaskRadius = 0.18f;
        [SerializeField, Range(0f, 1.5f)] private float centerExposure = 0.62f;
        [SerializeField, Range(0.02f, 0.8f)] private float centerFalloff = 0.34f;
        [SerializeField, Range(0f, 2f)] private float centerContrast = 0.95f;
        [SerializeField, Range(0f, 0.6f)] private float centerGradientStrength = 0.22f;
        [SerializeField, Range(0f, 0.6f)] private float centerDetailBoost = 0.18f;
        [SerializeField, Range(0.2f, 2f)] private float centerBloomLimit = 0.72f;
        [SerializeField] private bool centerCleanEnabled;
        [SerializeField, Range(0.001f, 0.35f)] private float centerCleanRadius = 0.095f;
        [SerializeField, Range(0.001f, 0.3f)] private float centerCleanFeather = 0.055f;
        [SerializeField] private bool centerReconstructFromTexture = true;
        [SerializeField, Range(0f, 1f)] private float centerPatternContinuation = 0.92f;
        [SerializeField] private KaleidoscopeCenterFillMode centerFillMode = KaleidoscopeCenterFillMode.MirrorContinuation;
        [SerializeField, Range(0f, 0.35f)] private float centerWorkRadius = 0.095f;
        [SerializeField, Range(0.001f, 0.3f)] private float centerWorkFeather = 0.055f;
        [SerializeField, Range(0f, 1f)] private float centerBlendStrength = 0.78f;
        [SerializeField, Range(0f, 1f)] private float centerContinuationStrength = 0.76f;
        [SerializeField, Range(0f, 1f)] private float centerDetailAmount = 0.18f;
        [SerializeField, Range(0.25f, 2f)] private float centerSampleScale = 1f;
        [SerializeField, Range(0f, 1f)] private float centerReconstructionQuality = 0.7f;
        [SerializeField] private bool centerAffectedByQuality;
        [SerializeField] private bool centerOnlyDebugMode;
        [SerializeField] private bool centerMaskPreview;
        [SerializeField] private bool physicalCenterArtifacts = true;
        [SerializeField] private bool centerArtifactOverrideEnabled = true;
        [SerializeField, Range(0.001f, 0.35f)] private float centerArtifactOverrideRadius = 0.115f;
        [SerializeField] private KaleidoscopeCenterArtifactOverrideMode centerArtifactOverrideMode = KaleidoscopeCenterArtifactOverrideMode.SampleCleanSource;

        [Header("Mosaic Cohesion")]
        [SerializeField, Range(0f, 1f)] private float opticalDensity = 0.76f;
        [SerializeField, Range(0f, 0.4f)] private float visualNoiseAmount = 0.045f;
        [SerializeField, Range(0f, 2f)] private float foregroundWeight = 0.86f;
        [SerializeField, Range(0f, 2f)] private float midgroundWeight = 1.02f;
        [SerializeField, Range(0f, 2f)] private float backgroundWeight = 0.5f;
        [SerializeField, Range(0f, 1f)] private float depthFadeStrength = 0.16f;
        [SerializeField, Range(0f, 1f)] private float opticalDepthStrength = 0.3f;

        [Header("Optical Continuity")]
        [SerializeField, Range(1f, 1.6f)] private float sourceOverscanFactor = 1.24f;
        [SerializeField, Range(0f, 1f)] private float edgeRecursionBlend = 0.58f;
        [SerializeField, Range(0f, 1f)] private float centerConvergenceStrength = 0.66f;
        [SerializeField, Range(0f, 1f)] private float radialContinuation = 0.58f;
        [SerializeField, Range(0f, 1f)] private float centerRecursionBlend = 0.5f;
        [SerializeField, Range(0f, 1f)] private float innerPatternPropagation = 0.48f;
        [SerializeField, Range(0.25f, 2f)] private float seamSmoothingQuality = 1.4f;
        [SerializeField, Range(0.25f, 2f)] private float opticalDistortionQuality = 1.18f;

        [Header("Optical Mask")]
        [SerializeField] private bool maskEnabled = true;
        [SerializeField] private KaleidoscopeMaskMode maskMode = KaleidoscopeMaskMode.CircularEyepiece;
        [SerializeField, Range(0.1f, 1.2f)] private float maskRadius = 0.72f;
        [SerializeField, Range(0.001f, 0.5f)] private float maskSoftness = 0.16f;
        [SerializeField, Range(0f, 1f)] private float maskDarkness = 0.58f;
        [SerializeField] private float hexMaskRotation;
        [SerializeField] private bool vignetteEnabled = true;
        [SerializeField, Range(0f, 1f)] private float vignetteStrength = 0.12f;
        [SerializeField, Range(0.2f, 1.2f)] private float vignetteSoftness = 0.72f;
        [SerializeField, Range(0f, 1f)] private float edgeDarkening = 0.08f;
        [SerializeField, Range(0f, 0.25f)] private float opticalMaskFeather = 0.05f;
        [SerializeField, Range(0f, 0.08f)] private float lensImperfectionStrength = 0.006f;

        [Header("Color Hierarchy")]
        [SerializeField, Range(0f, 2f)] private float rubyWeight = 1.08f;
        [SerializeField, Range(0f, 2f)] private float emeraldWeight = 0.96f;
        [SerializeField, Range(0f, 2f)] private float opalWeight = 0.86f;
        [SerializeField, Range(0f, 2f)] private float quartzWeight = 0.78f;
        [SerializeField, Range(0f, 1f)] private float saturationCompression = 0.28f;
        [SerializeField] private Color highlightColorBias = new Color(1f, 0.88f, 0.72f, 1f);

        [Header("Visual Quality")]
        [SerializeField, Range(0f, 3f)] private float brightness = 1.05f;
        [SerializeField, Range(0f, 3f)] private float contrast = 1.2f;
        [SerializeField, Range(0f, 2f)] private float saturation = 1.25f;
        [SerializeField, Range(0f, 2f)] private float vibrance = 1.15f;
        [SerializeField, Range(0.1f, 3f)] private float gamma = 0.95f;
        [SerializeField, Range(0f, 0.25f)] private float blackLevel = 0.02f;
        [SerializeField, Range(0.5f, 2f)] private float whiteLevel = 1.1f;
        [SerializeField, Range(0f, 1f)] private float sharpness = 0.15f;

        [Header("UX Effect Levels")]
        [SerializeField] private EffectLevel saturationLevel = EffectLevel.Normal;
        [SerializeField] private EffectLevel contrastLevel = EffectLevel.Normal;
        [SerializeField] private EffectLevel bloomLevel = EffectLevel.Normal;
        [SerializeField] private EffectLevel motionLevel = EffectLevel.Normal;

        [Header("Color Depth / Palette")]
        [SerializeField] private KaleidoscopeColorDepthMode colorDepthMode = KaleidoscopeColorDepthMode.FullColor;
        [SerializeField] private float colorSteps = 16777216f;
        [SerializeField, Range(0f, 1f)] private float paletteQuantizationStrength;

        [Header("Organic Imperfections")]
        [SerializeField] private bool wobbleEnabled = true;
        [SerializeField, Range(0f, 0.05f)] private float wobbleStrength = 0.0035f;
        [SerializeField, Range(0f, 4f)] private float wobbleSpeed = 0.8f;
        [SerializeField] private bool breathingEnabled = true;
        [SerializeField, Range(0f, 0.05f)] private float breathingAmplitude = 0.006f;
        [SerializeField, Range(0f, 2f)] private float breathingSpeed = 0.28f;
        [SerializeField] private bool centerDriftEnabled = true;
        [SerializeField, Range(0f, 0.04f)] private float centerDriftStrength = 0.0025f;
        [SerializeField, Range(0f, 2f)] private float centerDriftSpeed = 0.22f;
        [SerializeField] private bool segmentVariationEnabled = true;
        [SerializeField, Range(0f, 0.04f)] private float segmentAngleVariation = 0.003f;
        [SerializeField, Range(0f, 0.12f)] private float segmentBrightnessVariation = 0.018f;
        [SerializeField] private bool temporalDriftEnabled = true;
        [SerializeField, Range(0f, 1f)] private float driftSpeed = 0.08f;
        [SerializeField, Range(0f, 0.04f)] private float driftAmount = 0.003f;
        [SerializeField] private bool asymmetryEnabled = true;
        [SerializeField, Range(0f, 0.05f)] private float asymmetryStrength = 0.003f;
        [SerializeField, Range(0f, 0.04f)] private float temporalDriftAmount = 0.0025f;
        [SerializeField, Range(0f, 0.04f)] private float rotationalDrift = 0.0015f;
        [SerializeField, Range(0f, 0.04f)] private float scaleDrift = 0.0025f;
        [SerializeField, Range(0f, 0.04f)] private float opticalBreathingAmount = 0.004f;
        [SerializeField] private bool highSpeedSpinAAEnabled = true;
        [SerializeField, Range(0f, 0.08f)] private float highSpeedSeamSoftening = 0.018f;
        [SerializeField] private bool highSpeedMotionBlurOptional;
        [SerializeField] private bool highSpeedTaaStabilization = true;
        [SerializeField] private float highSpeedNoiseFreezeThreshold = 360f;
        [SerializeField] private float aliasingAngularStepThreshold = 9f;

        [Header("Temporal Stability")]
        [SerializeField, Range(0f, 1f)] private float globalMotionDamping = 0.74f;
        [SerializeField, Range(0.01f, 4f)] private float opticalInertia = 2.2f;
        [SerializeField, Range(0f, 1f)] private float temporalSmoothing = 0.88f;
        [SerializeField, Range(0f, 1f)] private float patternPersistence = 0.88f;
        [SerializeField, Range(1f, 30f)] private float distortionUpdateRate = 12f;
        [SerializeField, Range(0f, 1f)] private float opticalFlowStrength = 0.72f;
        [SerializeField, Range(0.1f, 20f)] private float patternVelocityClamp = 4f;
        [SerializeField, Range(1f, 90f)] private float maxPatternAngularVelocity = 18f;
        [SerializeField, Range(0f, 0.2f)] private float radialMotionClamp = 0.025f;
        [SerializeField, Range(0f, 1f)] private float rhythmPhase;
        [SerializeField, Range(2f, 20f)] private float opticalBreathingPeriod = 8f;
        [SerializeField, Range(0f, 0.05f)] private float harmonicMotionStrength = 0.006f;

        [Header("Eyepiece Finish")]
        [SerializeField] private bool dirtyGlassEnabled = true;
        [SerializeField, Range(0f, 0.08f)] private float dirtyGlassStrength = 0.01f;
        [SerializeField, Range(16f, 320f)] private float dirtyGlassScale = 120f;

        [Header("Input Response")]
        [SerializeField] private float manualRotationDegreesPerSecond = 72f;
        [SerializeField] private float distortionStep = 0.35f;
        [SerializeField] private float densityStep = 0.04f;
        [SerializeField] private float centerExposureStep = 0.35f;
        [SerializeField] private float rotationalDriftStep = 0.002f;

        [Header("Texture Source Clean View")]
        [SerializeField] private bool removePhysicalCenterMaskForTextureSources = true;

        [Header("Direct Image Source Playback")]
        [SerializeField] private Texture secondarySourceTexture;
        [SerializeField] private Vector2 imageSourceOffset;
        [SerializeField, Range(0.25f, 3f)] private float imageSourceZoom = 1f;
        [SerializeField] private float imageSourceRotation;
        [SerializeField, Range(0f, 1f)] private float imageTransitionProgress;
        [SerializeField] private int imageTransitionMode;
        [SerializeField] private float imageScrollSpeed = 0.035f;
        [SerializeField] private float imageZoomSpeed = 0.08f;
        [SerializeField] private float imageRotationSpeed = 0.025f;
        [SerializeField] private float imageChangeInterval = 30f;
        [SerializeField] private float imageTransitionDuration = 2f;
        [SerializeField, Range(0f, 0.08f)] private float imageMobiusDrift = 0.018f;

        [Header("Beauty Mode")]
        [SerializeField] private bool beautyModeEnabled;
        [SerializeField] private KaleidoscopeBeautyPreset beautyPreset = KaleidoscopeBeautyPreset.WarmGlass;

        [Header("Shake")]
        [SerializeField] private float shakeStrength = 0.025f;
        [SerializeField] private float shakeDuration = 0.28f;
        [SerializeField] private float shakeDamping = 9f;
        [SerializeField] private KaleidoscopeShakeMode shakeMode = KaleidoscopeShakeMode.UV;

        private Material displayMaterial;
        private float runtimeRotation;
        private float smoothedRotationVelocity;
        private float opticalTime;
        private float sampledOrganicTime;
        private float smoothedWobbleStrength;
        private float smoothedBreathingAmplitude;
        private float smoothedCenterDriftStrength;
        private float smoothedAsymmetryStrength;
        private float smoothedDriftAmount;
        private float smoothedRotationalDrift;
        private float smoothedScaleDrift;
        private float zoomVelocity;
        private float angularStepPerFrame;
        private bool keyboardRotationHeld;
        private bool explicitPatternStopActive;
        private float lastKeyboardRotationInputTime = -1000f;
        private KaleidoscopeSpinStabilityState spinStabilityState = KaleidoscopeSpinStabilityState.Stable;
        private bool directTextureSource;
        private bool animatedImageSource;
        private bool cleanTextureOverrideActive;
        private bool savedMaskEnabled;
        private bool savedCenterMaskEnabled;
        private KaleidoscopeCenterMaskMode savedCenterMaskMode;
        private float savedCenterMaskRadius;
        private float savedCenterConvergenceStrength;
        private bool savedCenterCleanEnabled;
        private bool savedPhysicalCenterArtifacts;
        private bool imageModePerformanceBiasActive;
        private bool savedDirtyGlassEnabled;
        private bool savedVignetteEnabled;
        private bool savedWobbleEnabled;
        private bool savedBreathingEnabled;
        private bool savedCenterDriftEnabled;
        private bool savedSegmentVariationEnabled;
        private bool savedSeamChromaticAberrationEnabled;
        private float shakeTimeRemaining;
        private Vector2 sourceShakeOffset;
        private Vector2 sourceShakeVelocity;
        private float spinVelocity;

        public int SegmentCount => ComputedSegmentCount;
        public int ManualSegmentCount => segmentCountManual;
        public int ComputedSegmentCount => useMirrorAngleMode ? Mathf.Clamp(Mathf.RoundToInt(360f / Mathf.Max(1f, mirrorAngleDegrees)), 1, MaxSegmentCount) : Mathf.Clamp(segmentCountManual, 1, MaxSegmentCount);
        public bool UseMirrorAngleMode => useMirrorAngleMode;
        public string PrismModeName => prismMode.ToString();
        public float MirrorAngleDegrees => mirrorAngleDegrees;
        public float PatternZoom => patternZoom;
        public float RequestedPatternZoom => requestedPatternZoom;
        public float EffectivePatternZoom => effectivePatternZoom;
        public float PatternRotation => runtimeRotation + mirrorAngleOffset * Mathf.Deg2Rad;
        public float PatternRotationSpeedDeg => patternRotationSpeed;
        public float PatternSpinSpeedDeg => patternSpinSpeedDeg;
        public float RequestedPatternRotationSpeedDeg => requestedPatternRotationSpeed;
        public float EffectivePatternRotationSpeedDeg => effectivePatternRotationSpeed;
        public float BaselinePatternRotationSpeedDeg => defaultPatternRotationSpeed;
        public float PatternSpinMinDeg => minPatternSpinSpeedDeg;
        public float PatternSpinMaxDeg => maxPatternSpinSpeedDeg;
        public bool PatternSpinEnabled => patternSpinEnabled;
        public float RotationAcceleration => rotationAcceleration;
        public float RotationDamping => rotationDamping;
        public float RotationSmoothing => rotationSmoothing;
        public float RadialDistortion => radialDistortion;
        public float SeamSoftness => seamSoftness;
        public float SeamBlendStrength => seamBlendingEnabled ? seamBlendStrength : 0f;
        public float Brightness => brightness;
        public float Contrast => contrast;
        public float Saturation => saturation;
        public float Vibrance => vibrance;
        public float Gamma => gamma;
        public float BlackLevel => blackLevel;
        public float WhiteLevel => whiteLevel;
        public float Sharpness => sharpness;
        public EffectLevel SaturationLevel => saturationLevel;
        public EffectLevel ContrastLevel => contrastLevel;
        public EffectLevel BloomLevel => bloomLevel;
        public EffectLevel MotionLevel => motionLevel;
        public Vector2 CenterOffset => new Vector2(centerOffsetX, centerOffsetY);
        public KaleidoscopeColorDepthMode ColorDepthMode => colorDepthMode;
        public string ColorDepthModeName => ResolveColorDepthDisplayName(colorDepthMode);
        public float ColorSteps => colorSteps;
        public float PaletteQuantizationStrength => paletteQuantizationStrength;
        public float CenterMaskRadius => centerMaskRadius;
        public bool CenterMaskEnabled => centerMaskEnabled;
        public string CenterMaskModeName => centerMaskEnabled ? centerMaskMode.ToString() : "Off";
        public float CenterExposure => centerExposure;
        public float OpticalDensity => opticalDensity;
        public float OpticalComplexity => Mathf.Clamp01(
            opticalDensity * 0.3f +
            edgeRecursionBlend * 0.18f +
            centerRecursionBlend * 0.14f +
            innerPatternPropagation * 0.14f +
            centerBlendStrength * 0.12f +
            centerContinuationStrength * 0.12f);
        public float OpticalRecursion => Mathf.Clamp01((edgeRecursionBlend + centerRecursionBlend + innerPatternPropagation) / 3f);
        public float CompositionDepth => Mathf.Clamp01((foregroundWeight + midgroundWeight + backgroundWeight) / 4f + opticalDepthStrength * 0.25f);
        public float MosaicCohesionScore => Mathf.Clamp01(opticalDensity * 0.34f + SeamBlendStrength * 0.28f + saturationCompression * 0.18f + (vignetteEnabled ? 0.2f : 0f));
        public float SourceOverscanFactor => sourceOverscanFactor;
        public float EdgeRecursionBlend => edgeRecursionBlend;
        public float CenterConvergenceStrength => centerConvergenceStrength;
        public float RadialContinuation => radialContinuation;
        public float CenterRecursionBlend => centerRecursionBlend;
        public float InnerPatternPropagation => innerPatternPropagation;
        public string MaskModeName => maskEnabled ? maskMode.ToString() : "Off";
        public bool ShowSectorBoundaries => showSectorBoundaries;
        public bool VignetteEnabled => vignetteEnabled;
        public float VignetteStrength => vignetteStrength;
        public bool SeamBlendingEnabled => seamBlendingEnabled;
        public bool AsymmetryEnabled => asymmetryEnabled;
        public bool WobbleEnabled => wobbleEnabled;
        public bool BreathingEnabled => breathingEnabled;
        public bool CenterDriftEnabled => centerDriftEnabled;
        public bool SegmentVariationEnabled => segmentVariationEnabled;
        public float DriftAmount => driftAmount;
        public float RotationalDrift => rotationalDrift;
        public float GlobalMotionDamping => globalMotionDamping;
        public float OpticalInertia => opticalInertia;
        public float TemporalSmoothing => temporalSmoothing;
        public float PatternPersistence => patternPersistence;
        public float MaxPatternAngularVelocity => maxPatternAngularVelocity;
        public bool DirectTextureSource => directTextureSource;
        public bool AnimatedImageSource => animatedImageSource;
        public Texture SecondarySourceTexture => secondarySourceTexture;
        public Vector2 ImageSourceOffset => imageSourceOffset;
        public float ImageSourceZoom => imageSourceZoom;
        public float ImageSourceRotation => imageSourceRotation;
        public float ImageTransitionProgress => imageTransitionProgress;
        public int ImageTransitionMode => imageTransitionMode;
        public float ImageScrollSpeed => imageScrollSpeed;
        public float ImageZoomSpeed => imageZoomSpeed;
        public float ImageRotationSpeed => imageRotationSpeed;
        public float ImageChangeInterval => imageChangeInterval;
        public float ImageTransitionDuration => imageTransitionDuration;
        public bool RemovePhysicalCenterMaskForTextureSources => removePhysicalCenterMaskForTextureSources;
        public KaleidoscopeShakeMode ShakeMode => shakeMode;
        public bool CenterCleanEnabled => centerCleanEnabled;
        public float CenterCleanRadius => centerCleanRadius;
        public float CenterCleanFeather => centerCleanFeather;
        public float CenterPatternContinuation => centerPatternContinuation;
        public float CenterDetailBoost => centerDetailBoost;
        public float CenterWorkRadius => centerWorkRadius;
        public float CenterWorkFeather => centerWorkFeather;
        public float CenterBlendStrength => centerBlendStrength;
        public float CenterContinuationStrength => centerContinuationStrength;
        public float CenterDetailAmount => centerDetailAmount;
        public float CenterSampleScale => centerSampleScale;
        public float CenterReconstructionQuality => centerReconstructionQuality;
        public bool CenterAffectedByQuality => centerAffectedByQuality;
        public bool CenterOnlyDebugMode => centerOnlyDebugMode;
        public bool CenterMaskPreview => centerMaskPreview;
        public bool PhysicalCenterArtifacts => physicalCenterArtifacts;
        public string CenterFillModeName => centerFillMode.ToString();
        public bool BeautyModeEnabled => beautyModeEnabled;
        public KaleidoscopeBeautyPreset BeautyPreset => beautyPreset;
        public string SeamAAState => seamAntialiasingEnabled ? $"On ({seamAAWidth:F3})" : "Off";
        public float AngularStepPerFrame => angularStepPerFrame;
        public KaleidoscopeSpinStabilityState SpinStabilityState => spinStabilityState;
        public string SpinStabilityWarning => spinStabilityState == KaleidoscopeSpinStabilityState.AliasingRisk
            ? "Spin aliasing risk: reduce speed or enable high-speed stabilization."
            : string.Empty;

        public void Configure(Material material)
        {
            displayMaterial = material;
            requestedPatternRotationSpeed = Mathf.Approximately(requestedPatternRotationSpeed, 0f)
                ? patternRotationSpeed
                : requestedPatternRotationSpeed;
            patternSpinSpeedDeg = patternRotationSpeed;
            colorSteps = ResolveColorDepthSteps(colorDepthMode);
            if (colorDepthMode == KaleidoscopeColorDepthMode.FullColor)
            {
                paletteQuantizationStrength = 0f;
            }

            ApplyShaderValues();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            float dampingScale = Mathf.Lerp(1f, 0.28f, globalMotionDamping);
            float activeSpinLimit = Mathf.Max(Mathf.Abs(minPatternSpinSpeedDeg), Mathf.Abs(maxPatternSpinSpeedDeg));
            if (!keyboardRotationHeld &&
                !explicitPatternStopActive &&
                Time.time - lastKeyboardRotationInputTime >= Mathf.Max(0f, keyboardRotationHoldSeconds))
            {
                requestedPatternRotationSpeed = Mathf.MoveTowards(
                    requestedPatternRotationSpeed,
                    Mathf.Clamp(defaultPatternRotationSpeed, minPatternSpinSpeedDeg, maxPatternSpinSpeedDeg),
                    Mathf.Max(0.01f, keyboardRotationBaselineRestoreSpeed) * dt);
            }

            float spinAccelerationThisFrame = Mathf.Min(
                Mathf.Max(0.01f, rotationAcceleration),
                Mathf.Max(0.01f, spinJerkLimit) * dt);
            patternRotationSpeed = Mathf.MoveTowards(
                patternRotationSpeed,
                patternSpinEnabled ? Mathf.Clamp(requestedPatternRotationSpeed, -activeSpinLimit, activeSpinLimit) : 0f,
                spinAccelerationThisFrame * dt);
            if (Mathf.Approximately(requestedPatternRotationSpeed, 0f))
            {
                float damping = smoothStop ? patternSpinDamping : rotationDamping;
                patternRotationSpeed = Mathf.MoveTowards(patternRotationSpeed, 0f, Mathf.Max(0.01f, damping) * dt);
            }

            float targetVelocity = Mathf.Clamp(
                patternRotationSpeed * dampingScale,
                -activeSpinLimit,
                activeSpinLimit) * Mathf.Deg2Rad;
            float velocityResponse = Mathf.Max(0.001f, spinSmoothingTime);
            smoothedRotationVelocity = Mathf.SmoothDamp(
                smoothedRotationVelocity,
                targetVelocity,
                ref spinVelocity,
                velocityResponse,
                activeSpinLimit * Mathf.Deg2Rad / velocityResponse,
                dt);
            effectivePatternRotationSpeed = smoothedRotationVelocity * Mathf.Rad2Deg;
            patternSpinSpeedDeg = requestedPatternRotationSpeed;
            angularStepPerFrame = Mathf.Abs(effectivePatternRotationSpeed) * dt;
            UpdateSpinStability();
            float highSpeed01 = highSpeedSpinAAEnabled && highSpeedTaaStabilization
                ? Mathf.InverseLerp(highSpeedNoiseFreezeThreshold, maxPatternSpinSpeedDeg, Mathf.Abs(effectivePatternRotationSpeed))
                : 0f;
            runtimeRotation += Mathf.Clamp(
                smoothedRotationVelocity,
                -activeSpinLimit * Mathf.Deg2Rad,
                activeSpinLimit * Mathf.Deg2Rad) * dt;

            effectivePatternZoom = Mathf.SmoothDamp(
                effectivePatternZoom,
                requestedPatternZoom,
                ref zoomVelocity,
                Mathf.Max(0.001f, zoomSmoothing),
                Mathf.Max(0.01f, zoomSpeed * 8f),
                dt);
            patternZoom = Mathf.Clamp(effectivePatternZoom, zoomMin, zoomMax);

            UpdateShake(dt);

            opticalTime += dt * Mathf.Lerp(1f, 0.22f, globalMotionDamping) * Mathf.Lerp(1f, 0.18f, highSpeed01);
            float organicResponse = Mathf.Lerp(5f, 1.2f, patternPersistence) *
                Mathf.Lerp(0.75f, 1.25f, Mathf.InverseLerp(5f, 15f, distortionUpdateRate));
            sampledOrganicTime = Mathf.Lerp(
                sampledOrganicTime,
                opticalTime,
                1f - Mathf.Exp(-organicResponse * Mathf.Max(0f, dt)));

            SmoothTemporalParameters(dt);
            ApplyShaderValues();
        }

        public void AdjustSegmentCount(int delta)
        {
            segmentCountManual = FindNearestValidSegmentCount(segmentCountManual + delta);
            if (!useMirrorAngleMode)
            {
                ApplyShaderValues();
                return;
            }

            SetMirrorAngleForSegments(ComputedSegmentCount + delta);
            ApplyShaderValues();
        }

        public void AdjustZoom(float direction)
        {
            requestedPatternZoom = Mathf.Clamp(requestedPatternZoom + direction * zoomSpeed * Time.deltaTime, zoomMin, zoomMax);
            ApplyShaderValues();
        }

        public void AdjustRadialDistortion(float direction)
        {
            radialDistortion = Mathf.Clamp(radialDistortion + direction * distortionStep * Time.deltaTime, -1f, 1f);
            ApplyShaderValues();
        }

        public void AdjustOpticalDensity(float direction)
        {
            opticalDensity = Mathf.Clamp01(opticalDensity + direction * densityStep);
            visualNoiseAmount = Mathf.Clamp(visualNoiseAmount + direction * densityStep * 0.35f, 0f, 0.4f);
            ApplyShaderValues();
        }

        public void AdjustOpticalComplexity(float direction)
        {
            float signed = Mathf.Sign(direction);
            opticalDensity = Mathf.Clamp01(opticalDensity + signed * densityStep);
            edgeRecursionBlend = Mathf.Clamp01(edgeRecursionBlend + signed * 0.045f);
            centerRecursionBlend = Mathf.Clamp01(centerRecursionBlend + signed * 0.04f);
            innerPatternPropagation = Mathf.Clamp01(innerPatternPropagation + signed * 0.04f);
            opticalDepthStrength = Mathf.Clamp01(opticalDepthStrength + signed * 0.035f);
            foregroundWeight = Mathf.Clamp(foregroundWeight + signed * 0.04f, 0f, 2f);
            midgroundWeight = Mathf.Clamp(midgroundWeight + signed * 0.04f, 0f, 2f);
            backgroundWeight = Mathf.Clamp(backgroundWeight + signed * 0.03f, 0f, 2f);
            AdjustCenterQuality(direction);
        }

        public void AdjustCenterQuality(float direction)
        {
            float signed = Mathf.Sign(direction);
            centerAffectedByQuality = true;
            centerCleanEnabled = true;
            centerReconstructFromTexture = true;
            centerWorkRadius = Mathf.Clamp(centerWorkRadius - signed * 0.006f, 0.025f, 0.14f);
            centerWorkFeather = Mathf.Clamp(centerWorkFeather - signed * 0.004f, 0.012f, 0.09f);
            centerCleanRadius = centerWorkRadius;
            centerCleanFeather = centerWorkFeather;
            centerBlendStrength = Mathf.Clamp01(centerBlendStrength + signed * 0.08f);
            centerContinuationStrength = Mathf.Clamp01(centerContinuationStrength + signed * 0.08f);
            centerPatternContinuation = centerContinuationStrength;
            centerDetailAmount = Mathf.Clamp01(centerDetailAmount + signed * 0.05f);
            centerDetailBoost = Mathf.Clamp(centerDetailAmount, 0f, 0.6f);
            centerReconstructionQuality = Mathf.Clamp01(centerReconstructionQuality + signed * 0.1f);
            centerSampleScale = Mathf.Clamp(centerSampleScale + signed * 0.05f, 0.75f, 1.25f);
            centerFillMode = centerReconstructionQuality >= 0.72f
                ? KaleidoscopeCenterFillMode.SoftBlend
                : KaleidoscopeCenterFillMode.MirrorContinuation;
            ApplyShaderValues();
        }

        public void ToggleCenterMaskPreview()
        {
            centerMaskPreview = !centerMaskPreview;
            centerOnlyDebugMode = centerMaskPreview;
            ApplyShaderValues();
        }

        public void ApplyImageModePerformanceBias(bool enabled)
        {
            if (enabled)
            {
                if (!imageModePerformanceBiasActive)
                {
                    savedDirtyGlassEnabled = dirtyGlassEnabled;
                    savedVignetteEnabled = vignetteEnabled;
                    savedWobbleEnabled = wobbleEnabled;
                    savedBreathingEnabled = breathingEnabled;
                    savedCenterDriftEnabled = centerDriftEnabled;
                    savedSegmentVariationEnabled = segmentVariationEnabled;
                    savedSeamChromaticAberrationEnabled = seamChromaticAberrationEnabled;
                    imageModePerformanceBiasActive = true;
                }

                dirtyGlassEnabled = false;
                vignetteEnabled = false;
                wobbleEnabled = false;
                breathingEnabled = false;
                centerDriftEnabled = false;
                segmentVariationEnabled = false;
                seamChromaticAberrationEnabled = false;
            }
            else if (imageModePerformanceBiasActive)
            {
                dirtyGlassEnabled = savedDirtyGlassEnabled;
                vignetteEnabled = savedVignetteEnabled;
                wobbleEnabled = savedWobbleEnabled;
                breathingEnabled = savedBreathingEnabled;
                centerDriftEnabled = savedCenterDriftEnabled;
                segmentVariationEnabled = savedSegmentVariationEnabled;
                seamChromaticAberrationEnabled = savedSeamChromaticAberrationEnabled;
                imageModePerformanceBiasActive = false;
            }

            ApplyShaderValues();
        }

        public void AdjustCenterExposure(float direction)
        {
            centerExposure = Mathf.Clamp(centerExposure + direction * centerExposureStep * Time.deltaTime, 0f, 1.5f);
            ApplyShaderValues();
        }

        public void AdjustRotationalDrift(float direction)
        {
            rotationalDrift = Mathf.Clamp(rotationalDrift + direction * rotationalDriftStep, 0f, 0.04f);
            ApplyShaderValues();
        }

        public void RotatePattern(float direction)
        {
            float manualVelocity = Mathf.Clamp(manualRotationDegreesPerSecond, 0f, maxPatternAngularVelocity) * Mathf.Deg2Rad;
            smoothedRotationVelocity += direction * manualVelocity * Time.deltaTime;
            ApplyShaderValues();
        }

        public void AdjustPatternRotationSpeed(float deltaDegPerSecond)
        {
            AdjustPatternSpinSpeed(deltaDegPerSecond, false);
        }

        public void SetRotationBaseline(float speedDegPerSecond)
        {
            defaultPatternRotationSpeed = Mathf.Clamp(speedDegPerSecond, minPatternSpinSpeedDeg, maxPatternSpinSpeedDeg);
            if (!keyboardRotationHeld && !explicitPatternStopActive)
            {
                requestedPatternRotationSpeed = defaultPatternRotationSpeed;
                patternSpinEnabled = true;
            }

            ApplyShaderValues();
        }

        public void HoldKeyboardRotation(int direction, bool experimentalRange)
        {
            int signedDirection = direction < 0 ? -1 : direction > 0 ? 1 : 0;
            if (signedDirection == 0)
            {
                ReleaseKeyboardRotation();
                return;
            }

            float limit = experimentalRange
                ? maxPatternSpinSpeedDeg
                : Mathf.Min(maxPatternSpinSpeedDeg, Mathf.Max(comfortPatternSpinSpeedLimit, maxPatternAngularVelocity));
            float target = Mathf.Clamp(
                signedDirection * Mathf.Max(1f, keyboardRotationTargetSpeed),
                Mathf.Max(minPatternSpinSpeedDeg, -limit),
                Mathf.Min(maxPatternSpinSpeedDeg, limit));

            requestedPatternRotationSpeed = Mathf.MoveTowards(
                requestedPatternRotationSpeed,
                target,
                Mathf.Max(1f, patternSpinAcceleration) * Time.deltaTime);
            keyboardRotationHeld = true;
            explicitPatternStopActive = false;
            lastKeyboardRotationInputTime = Time.time;
            patternSpinEnabled = true;
            ApplyShaderValues();
        }

        public void ReleaseKeyboardRotation()
        {
            if (!keyboardRotationHeld)
            {
                return;
            }

            keyboardRotationHeld = false;
            lastKeyboardRotationInputTime = Time.time;
        }

        public void TriggerKeyboardSpinBurst(int direction)
        {
            int signedDirection = direction < 0 ? -1 : direction > 0 ? 1 : 0;
            if (signedDirection == 0)
            {
                return;
            }

            requestedPatternRotationSpeed = Mathf.Clamp(
                signedDirection * Mathf.Max(1f, keyboardRotationTargetSpeed),
                minPatternSpinSpeedDeg,
                maxPatternSpinSpeedDeg);
            keyboardRotationHeld = false;
            explicitPatternStopActive = false;
            lastKeyboardRotationInputTime = Time.time;
            patternSpinEnabled = true;
            ApplyShaderValues();
        }

        public void CancelHighSpeedRotationToBaseline()
        {
            keyboardRotationHeld = false;
            explicitPatternStopActive = false;
            lastKeyboardRotationInputTime = -1000f;
            requestedPatternRotationSpeed = Mathf.Clamp(defaultPatternRotationSpeed, minPatternSpinSpeedDeg, maxPatternSpinSpeedDeg);
            patternSpinEnabled = true;
            ApplyShaderValues();
        }

        public void AdjustPatternSpinSpeed(float direction, bool experimentalRange)
        {
            float limit = experimentalRange
                ? maxPatternSpinSpeedDeg
                : Mathf.Min(maxPatternSpinSpeedDeg, Mathf.Max(comfortPatternSpinSpeedLimit, maxPatternAngularVelocity));
            requestedPatternRotationSpeed = Mathf.Clamp(
                requestedPatternRotationSpeed + direction * Mathf.Max(1f, patternSpinAcceleration) * Time.deltaTime,
                Mathf.Max(minPatternSpinSpeedDeg, -limit),
                Mathf.Min(maxPatternSpinSpeedDeg, limit));
            patternSpinEnabled = true;
            explicitPatternStopActive = false;
            ApplyShaderValues();
        }

        public void SetPatternRotationSpeed(float speedDegPerSecond)
        {
            requestedPatternRotationSpeed = Mathf.Clamp(speedDegPerSecond, minPatternSpinSpeedDeg, maxPatternSpinSpeedDeg);
            patternSpinEnabled = !Mathf.Approximately(requestedPatternRotationSpeed, 0f);
            explicitPatternStopActive = Mathf.Approximately(requestedPatternRotationSpeed, 0f);
            ApplyShaderValues();
        }

        public void SetPatternZoomTarget(float zoom)
        {
            requestedPatternZoom = Mathf.Clamp(zoom, zoomMin, zoomMax);
            ApplyShaderValues();
        }

        public void SetMirrorAngleDegrees(float angleDegrees)
        {
            useMirrorAngleMode = true;
            mirrorAngleDegrees = Mathf.Clamp(angleDegrees, 5f, 180f);
            ApplyShaderValues();
        }

        public void SetCenterOffset(Vector2 offset)
        {
            centerOffsetX = Mathf.Clamp(offset.x, -0.5f, 0.5f);
            centerOffsetY = Mathf.Clamp(offset.y, -0.5f, 0.5f);
            ApplyShaderValues();
        }

        public void SetSeamSoftness(float softness)
        {
            seamSoftness = Mathf.Clamp(softness, 0f, 0.25f);
            ApplyShaderValues();
        }

        public void SetFinalColorGrading(float brightnessValue, float contrastValue, float saturationValue)
        {
            brightness = Mathf.Clamp(brightnessValue, 0f, 3f);
            contrast = Mathf.Clamp(contrastValue, 0f, 3f);
            saturation = Mathf.Clamp(saturationValue, 0f, 2f);
            ApplyShaderValues();
        }

        public void SetVignetteStrength(float strength)
        {
            vignetteStrength = Mathf.Clamp01(strength);
            ApplyShaderValues();
        }

        public void ApplyAutoVisualQuality()
        {
            ApplyPremiumVisualQualityPreset();
            if (TryEstimateAverageSourceLuminance(out float averageLuminance))
            {
                if (averageLuminance > 0.72f)
                {
                    brightness = 1.02f;
                    gamma = 1.0f;
                    blackLevel = 0.025f;
                    whiteLevel = 1.14f;
                }
                else if (averageLuminance < 0.24f)
                {
                    brightness = 1.12f;
                    gamma = 0.88f;
                    blackLevel = 0.01f;
                    whiteLevel = 1.08f;
                }
            }

            ApplyShaderValues();
        }

        public void SetColorDepthMode(KaleidoscopeColorDepthMode mode)
        {
            colorDepthMode = mode;
            colorSteps = ResolveColorDepthSteps(mode);
            paletteQuantizationStrength = mode == KaleidoscopeColorDepthMode.FullColor ? 0f : 1f;
            ApplyShaderValues();
        }

        public void StepColorDepthMode(int delta)
        {
            int count = System.Enum.GetValues(typeof(KaleidoscopeColorDepthMode)).Length;
            int next = ((int)colorDepthMode + delta) % count;
            if (next < 0)
            {
                next += count;
            }

            SetColorDepthMode((KaleidoscopeColorDepthMode)next);
        }

        public void SetPaletteQuantizationStrength(float strength)
        {
            paletteQuantizationStrength = Mathf.Clamp01(strength);
            ApplyShaderValues();
        }

        public void SetImageSourcePlayback(
            Texture secondaryTexture,
            Vector2 offset,
            float zoom,
            float rotationRadians,
            float transitionProgress,
            int transitionModeValue,
            float scrollSpeed,
            float zoomSpeed,
            float rotationSpeed,
            float changeInterval,
            float transitionDuration)
        {
            secondarySourceTexture = secondaryTexture;
            animatedImageSource = true;
            imageSourceOffset = offset;
            imageSourceZoom = Mathf.Clamp(zoom, 0.25f, 3f);
            imageSourceRotation = rotationRadians;
            imageTransitionProgress = Mathf.Clamp01(transitionProgress);
            imageTransitionMode = Mathf.Clamp(transitionModeValue, 0, 3);
            imageScrollSpeed = Mathf.Max(0f, scrollSpeed);
            imageZoomSpeed = Mathf.Max(0f, zoomSpeed);
            imageRotationSpeed = rotationSpeed;
            imageChangeInterval = Mathf.Max(0.5f, changeInterval);
            imageTransitionDuration = Mathf.Max(0.05f, transitionDuration);
            ApplyShaderValues();
        }

        private void UpdateSpinStability()
        {
            float speed = Mathf.Abs(effectivePatternRotationSpeed);
            if (angularStepPerFrame >= aliasingAngularStepThreshold)
            {
                spinStabilityState = KaleidoscopeSpinStabilityState.AliasingRisk;
            }
            else if (speed > comfortPatternSpinSpeedLimit)
            {
                spinStabilityState = KaleidoscopeSpinStabilityState.TooFastForComfort;
            }
            else if (speed > comfortPatternSpinSpeedLimit * 0.55f)
            {
                spinStabilityState = KaleidoscopeSpinStabilityState.Fast;
            }
            else
            {
                spinStabilityState = KaleidoscopeSpinStabilityState.Stable;
            }
        }

        public void StopPatternRotation()
        {
            requestedPatternRotationSpeed = 0f;
            keyboardRotationHeld = false;
            explicitPatternStopActive = true;
        }

        public void RestoreDefaultPatternRotation()
        {
            requestedPatternRotationSpeed = Mathf.Clamp(defaultPatternRotationSpeed, minPatternSpinSpeedDeg, maxPatternSpinSpeedDeg);
            patternSpinEnabled = true;
            keyboardRotationHeld = false;
            explicitPatternStopActive = false;
            if (!smoothResume)
            {
                patternRotationSpeed = requestedPatternRotationSpeed;
            }
        }

        public void BrakePatternSpin()
        {
            requestedPatternRotationSpeed = Mathf.MoveTowards(
                requestedPatternRotationSpeed,
                0f,
                Mathf.Max(1f, patternSpinDamping) * Time.deltaTime);
            if (Mathf.Abs(requestedPatternRotationSpeed) < 0.05f)
            {
                requestedPatternRotationSpeed = 0f;
            }
        }

        public void RestoreDefaultPatternSpin()
        {
            RestoreDefaultPatternRotation();
        }

        public void SetSegmentCountDirect(int segments)
        {
            int safeSegments = ClampSegmentCount(segments);
            segmentCountManual = safeSegments;
            useMirrorAngleMode = true;
            SetMirrorAngleForSegments(safeSegments);
            ApplyShaderValues();
        }

        public void SetSegmentCount(int segments)
        {
            SetSegmentCountDirect(segments);
        }

        public void MultiplySegmentCount(int factor)
        {
            SetSegmentCountDirect(ComputedSegmentCount * Mathf.Max(1, factor));
        }

        public void DivideSegmentCount(int divisor)
        {
            SetSegmentCountDirect(Mathf.Max(1, ComputedSegmentCount / Mathf.Max(1, divisor)));
        }

        public void AdjustSegmentCountByStep(int delta)
        {
            SetSegmentCountDirect(FindNextValidSegmentCount(ComputedSegmentCount, delta));
        }

        public void SetDirectTextureSourceRendering(bool enabled)
        {
            directTextureSource = enabled;
            if (!enabled)
            {
                animatedImageSource = false;
            }

            if (enabled && removePhysicalCenterMaskForTextureSources)
            {
                if (!cleanTextureOverrideActive)
                {
                    savedMaskEnabled = maskEnabled;
                    savedCenterMaskEnabled = centerMaskEnabled;
                    savedCenterMaskMode = centerMaskMode;
                    savedCenterMaskRadius = centerMaskRadius;
                    savedCenterConvergenceStrength = centerConvergenceStrength;
                    savedCenterCleanEnabled = centerCleanEnabled;
                    savedPhysicalCenterArtifacts = physicalCenterArtifacts;
                    cleanTextureOverrideActive = true;
                }

                maskEnabled = false;
                centerMaskEnabled = false;
                centerMaskMode = KaleidoscopeCenterMaskMode.Disabled;
                centerMaskRadius = 0.001f;
                centerConvergenceStrength = 1f;
                centerCleanEnabled = true;
                centerFillMode = centerReconstructionQuality > 0.68f
                    ? KaleidoscopeCenterFillMode.SoftBlend
                    : KaleidoscopeCenterFillMode.MirrorContinuation;
                physicalCenterArtifacts = false;
            }
            else if (!enabled && cleanTextureOverrideActive)
            {
                maskEnabled = savedMaskEnabled;
                centerMaskEnabled = savedCenterMaskEnabled;
                centerMaskMode = savedCenterMaskMode;
                centerMaskRadius = savedCenterMaskRadius;
                centerConvergenceStrength = savedCenterConvergenceStrength;
                centerCleanEnabled = savedCenterCleanEnabled;
                physicalCenterArtifacts = savedPhysicalCenterArtifacts;
                cleanTextureOverrideActive = false;
            }

            ApplyShaderValues();
        }

        public void SetAnimatedImageSourceRendering(bool enabled)
        {
            animatedImageSource = enabled;
            ApplyShaderValues();
        }

        public void ApplyShakeImpulse(bool physicalSource)
        {
            shakeMode = physicalSource ? KaleidoscopeShakeMode.Physical : KaleidoscopeShakeMode.UV;
            shakeTimeRemaining = Mathf.Max(0.01f, shakeDuration);
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.right;
            }

            sourceShakeVelocity = direction * Mathf.Max(0.001f, shakeStrength);
        }

        public void SetBeautyMode(bool enabled, KaleidoscopeBeautyPreset preset)
        {
            beautyModeEnabled = enabled;
            beautyPreset = preset;
            if (!enabled)
            {
                ApplyShaderValues();
                return;
            }

            centerCleanEnabled = true;
            centerMaskEnabled = false;
            centerMaskMode = KaleidoscopeCenterMaskMode.Disabled;
            centerFillMode = KaleidoscopeCenterFillMode.MirrorContinuation;
            physicalCenterArtifacts = false;
            showSectorBoundaries = false;
            seamAntialiasingEnabled = true;
            seamAAWidth = 0.014f;
            seamLineSuppression = 0.86f;
            maskEnabled = false;
            vignetteEnabled = true;
            dirtyGlassEnabled = false;
            centerConvergenceStrength = 1f;
            centerPatternContinuation = 1f;
            ApplyBeautyPreset(preset);
            ApplyShaderValues();
        }

        public void ApplyBeautyPreset(KaleidoscopeBeautyPreset preset)
        {
            beautyPreset = preset;
            switch (preset)
            {
                case KaleidoscopeBeautyPreset.DeepJewel:
                    brightness = 1.04f;
                    contrast = 1.28f;
                    saturation = 1.32f;
                    vibrance = 1.18f;
                    gamma = 0.92f;
                    blackLevel = 0.018f;
                    whiteLevel = 1.12f;
                    sharpness = 0.18f;
                    rubyWeight = 1.18f;
                    emeraldWeight = 1.02f;
                    opalWeight = 0.88f;
                    highlightColorBias = new Color(1f, 0.84f, 0.66f, 1f);
                    break;
                case KaleidoscopeBeautyPreset.SoftOpal:
                    brightness = 1.03f;
                    contrast = 1.08f;
                    saturation = 1.08f;
                    vibrance = 1.06f;
                    gamma = 0.96f;
                    blackLevel = 0.012f;
                    whiteLevel = 1.08f;
                    sharpness = 0.08f;
                    rubyWeight = 0.92f;
                    emeraldWeight = 0.92f;
                    opalWeight = 1.05f;
                    highlightColorBias = new Color(0.94f, 0.96f, 1f, 1f);
                    break;
                default:
                    brightness = 1.05f;
                    contrast = 1.2f;
                    saturation = 1.25f;
                    vibrance = 1.15f;
                    gamma = 0.95f;
                    blackLevel = 0.02f;
                    whiteLevel = 1.1f;
                    sharpness = 0.15f;
                    rubyWeight = 1.08f;
                    emeraldWeight = 0.96f;
                    opalWeight = 0.86f;
                    highlightColorBias = new Color(1f, 0.88f, 0.72f, 1f);
                    break;
            }
        }

        public void ApplyTemporalStability(
            float globalSmoothing,
            float flickerSuppression,
            float microDetailPersistence,
            float mirrorMotionDamping,
            float noiseUpdateRateHz)
        {
            temporalSmoothing = Mathf.Clamp01(Mathf.Max(temporalSmoothing, globalSmoothing));
            globalMotionDamping = Mathf.Clamp01(Mathf.Max(globalMotionDamping, mirrorMotionDamping));
            patternPersistence = Mathf.Clamp01(Mathf.Max(patternPersistence, microDetailPersistence));
            distortionUpdateRate = Mathf.Clamp(noiseUpdateRateHz, 5f, 12f);
            visualNoiseAmount = Mathf.Min(visualNoiseAmount, Mathf.Lerp(0.12f, 0.035f, flickerSuppression));
            wobbleStrength = Mathf.Min(wobbleStrength, Mathf.Lerp(0.012f, 0.003f, flickerSuppression));
            asymmetryStrength = Mathf.Min(asymmetryStrength, Mathf.Lerp(0.01f, 0.0025f, flickerSuppression));
            centerDriftStrength = Mathf.Min(centerDriftStrength, Mathf.Lerp(0.008f, 0.002f, flickerSuppression));
            temporalDriftAmount = Mathf.Min(temporalDriftAmount, Mathf.Lerp(0.012f, 0.003f, flickerSuppression));
            rotationalDrift = Mathf.Min(rotationalDrift, Mathf.Lerp(0.008f, 0.001f, flickerSuppression));
            rotationSmoothing = Mathf.Clamp01(Mathf.Max(rotationSmoothing, globalSmoothing));
            ApplyShaderValues();
        }

        public void ApplyCenterExposureSafety(
            float centerMaxLuminance,
            float exposureClamp,
            float bloomClamp,
            float detailMinimum)
        {
            centerBrightness = Mathf.Min(centerBrightness, centerMaxLuminance);
            centerExposure = Mathf.Min(centerExposure, exposureClamp);
            centerBloomLimit = Mathf.Min(centerBloomLimit, bloomClamp);
            centerDetailBoost = Mathf.Max(centerDetailBoost, detailMinimum);
            ApplyShaderValues();
        }

        public void ApplyEmergencyVisualStability()
        {
            requestedPatternRotationSpeed = Mathf.Clamp(requestedPatternRotationSpeed, -4f, 4f);
            maxPatternAngularVelocity = Mathf.Min(maxPatternAngularVelocity, 8f);
            patternVelocityClamp = Mathf.Min(patternVelocityClamp, 3f);
            globalMotionDamping = Mathf.Max(globalMotionDamping, 0.9f);
            temporalSmoothing = Mathf.Max(temporalSmoothing, 0.92f);
            patternPersistence = Mathf.Max(patternPersistence, 0.92f);
            visualNoiseAmount = Mathf.Min(visualNoiseAmount, 0.035f);
            wobbleStrength = Mathf.Min(wobbleStrength, 0.0035f);
            asymmetryStrength = Mathf.Min(asymmetryStrength, 0.002f);
            centerDriftStrength = Mathf.Min(centerDriftStrength, 0.0018f);
            driftAmount = Mathf.Min(driftAmount, 0.003f);
            centerExposure = Mathf.Min(centerExposure, 0.62f);
            centerBrightness = Mathf.Min(centerBrightness, 0.82f);
            centerBloomLimit = Mathf.Min(centerBloomLimit, 0.62f);
            centerDetailBoost = Mathf.Max(centerDetailBoost, 0.2f);
            ApplyShaderValues();
        }

        public void ToggleWobble()
        {
            wobbleEnabled = !wobbleEnabled;
            ApplyShaderValues();
        }

        public void ToggleBreathing()
        {
            breathingEnabled = !breathingEnabled;
            ApplyShaderValues();
        }

        public void ToggleCenterDrift()
        {
            centerDriftEnabled = !centerDriftEnabled;
            ApplyShaderValues();
        }

        public void ToggleSegmentVariation()
        {
            segmentVariationEnabled = !segmentVariationEnabled;
            ApplyShaderValues();
        }

        public void ToggleMirrorAngleMode()
        {
            useMirrorAngleMode = !useMirrorAngleMode;
            ApplyShaderValues();
        }

        public void ToggleOpticalMask()
        {
            maskEnabled = !maskEnabled;
            ApplyShaderValues();
        }

        public void ToggleVignette()
        {
            vignetteEnabled = !vignetteEnabled;
            ApplyShaderValues();
        }

        public void ToggleSeamBlending()
        {
            seamBlendingEnabled = !seamBlendingEnabled;
            ApplyShaderValues();
        }

        public void ToggleAsymmetry()
        {
            asymmetryEnabled = !asymmetryEnabled;
            ApplyShaderValues();
        }

        public void ToggleSectorBoundaryDebug()
        {
            showSectorBoundaries = !showSectorBoundaries;
            ApplyShaderValues();
        }

        public void SetStandardPrism60()
        {
            prismMode = KaleidoscopePrismMode.ThreeMirrorTriangular;
            useMirrorAngleMode = true;
            mirrorAngleDegrees = 60f;
            ApplyShaderValues();
        }

        public void SetMirrorAngle45()
        {
            prismMode = KaleidoscopePrismMode.TwoMirror;
            useMirrorAngleMode = true;
            mirrorAngleDegrees = 45f;
            ApplyShaderValues();
        }

        public void SetMirrorAngle30()
        {
            prismMode = KaleidoscopePrismMode.CustomRadial;
            useMirrorAngleMode = true;
            mirrorAngleDegrees = 30f;
            ApplyShaderValues();
        }

        public void ResetVisualTuningDefaults()
        {
            useMirrorAngleMode = true;
            prismMode = KaleidoscopePrismMode.ThreeMirrorTriangular;
            mirrorAngleDegrees = 60f;
            segmentCountManual = 6;
            runtimeRotation = 0f;
            patternRotationSpeed = defaultPatternRotationSpeed;
            requestedPatternRotationSpeed = defaultPatternRotationSpeed;
            effectivePatternRotationSpeed = 0f;
            keyboardRotationHeld = false;
            explicitPatternStopActive = false;
            lastKeyboardRotationInputTime = -1000f;
            patternZoom = 1.18f;
            requestedPatternZoom = 1.18f;
            effectivePatternZoom = 1.18f;
            radialDistortion = 0.08f;
            seamBlendingEnabled = true;
            seamBlendStrength = 0.8f;
            seamFeatherWidth = 0.04f;
            continuityCorrection = 0.32f;
            radialEdgeSoftness = 0.035f;
            seamAntialiasingEnabled = true;
            seamAAWidth = 0.012f;
            seamLineSuppression = 0.72f;
            showSectorBoundaries = false;
            centerBrightness = 0.82f;
            centerVignette = 0.12f;
            centerStabilization = 0.72f;
            centerMaskEnabled = true;
            centerMaskMode = KaleidoscopeCenterMaskMode.PhysicalChamber;
            centerMaskRadius = 0.18f;
            centerExposure = 0.62f;
            centerFalloff = 0.34f;
            centerContrast = 0.95f;
            centerGradientStrength = 0.22f;
            centerDetailBoost = 0.18f;
            centerBloomLimit = 0.72f;
            centerCleanEnabled = false;
            centerFillMode = KaleidoscopeCenterFillMode.MirrorContinuation;
            centerWorkRadius = 0.095f;
            centerWorkFeather = 0.055f;
            centerBlendStrength = 0.78f;
            centerContinuationStrength = 0.76f;
            centerDetailAmount = 0.18f;
            centerSampleScale = 1f;
            centerReconstructionQuality = 0.7f;
            centerAffectedByQuality = false;
            centerOnlyDebugMode = false;
            centerMaskPreview = false;
            physicalCenterArtifacts = true;
            opticalDensity = 0.76f;
            visualNoiseAmount = 0.045f;
            foregroundWeight = 0.86f;
            midgroundWeight = 1.02f;
            backgroundWeight = 0.5f;
            opticalDepthStrength = 0.3f;
            sourceOverscanFactor = 1.24f;
            edgeRecursionBlend = 0.58f;
            centerConvergenceStrength = 0.66f;
            radialContinuation = 0.58f;
            centerRecursionBlend = 0.5f;
            innerPatternPropagation = 0.48f;
            seamSmoothingQuality = 1.4f;
            opticalDistortionQuality = 1.18f;
            maskEnabled = true;
            maskDarkness = 0.58f;
            vignetteEnabled = true;
            vignetteStrength = 0.12f;
            edgeDarkening = 0.08f;
            lensImperfectionStrength = 0.006f;
            rubyWeight = 1.08f;
            emeraldWeight = 0.96f;
            opalWeight = 0.86f;
            quartzWeight = 0.78f;
            saturationCompression = 0.28f;
            highlightColorBias = new Color(1f, 0.88f, 0.72f, 1f);
            brightness = 1.05f;
            contrast = 1.2f;
            saturation = 1.25f;
            vibrance = 1.15f;
            gamma = 0.95f;
            blackLevel = 0.02f;
            whiteLevel = 1.1f;
            sharpness = 0.15f;
            saturationLevel = EffectLevel.Normal;
            contrastLevel = EffectLevel.Normal;
            bloomLevel = EffectLevel.Normal;
            motionLevel = EffectLevel.Normal;
            colorDepthMode = KaleidoscopeColorDepthMode.FullColor;
            colorSteps = ResolveColorDepthSteps(colorDepthMode);
            paletteQuantizationStrength = 0f;
            wobbleEnabled = true;
            breathingEnabled = true;
            centerDriftEnabled = true;
            segmentVariationEnabled = true;
            temporalDriftEnabled = true;
            asymmetryEnabled = true;
            wobbleStrength = 0.0035f;
            breathingAmplitude = 0.006f;
            centerDriftStrength = 0.0025f;
            segmentAngleVariation = 0.003f;
            segmentBrightnessVariation = 0.018f;
            driftAmount = 0.003f;
            asymmetryStrength = 0.003f;
            temporalDriftAmount = 0.0025f;
            rotationalDrift = 0.0015f;
            scaleDrift = 0.0025f;
            opticalBreathingAmount = 0.004f;
            globalMotionDamping = 0.74f;
            opticalInertia = 2.2f;
            temporalSmoothing = 0.88f;
            patternPersistence = 0.88f;
            distortionUpdateRate = 12f;
            opticalFlowStrength = 0.72f;
            patternVelocityClamp = 4f;
            maxPatternAngularVelocity = 360f;
            radialMotionClamp = 0.025f;
            rhythmPhase = 0f;
            opticalBreathingPeriod = 8f;
            harmonicMotionStrength = 0.006f;
            dirtyGlassStrength = 0.01f;
            beautyModeEnabled = false;
            ApplyShaderValues();
        }

        public void ApplyQualityProfile(KaleidoscopeQualityProfile profile)
        {
            seamSmoothingQuality = Mathf.Clamp(profile.seamSmoothingQuality, 0.25f, 2f);
            opticalDistortionQuality = Mathf.Clamp(profile.opticalDistortionQuality, 0.25f, 2f);
            sourceOverscanFactor = Mathf.Clamp(profile.sourceOverscanFactor, 1f, 1.6f);
            edgeRecursionBlend = Mathf.Clamp01(profile.edgeRecursionBlend);
            centerConvergenceStrength = Mathf.Clamp01(profile.centerConvergenceStrength);
            radialContinuation = Mathf.Clamp01(profile.radialContinuation);
            centerRecursionBlend = Mathf.Clamp01(profile.centerRecursionBlend);
            innerPatternPropagation = Mathf.Clamp01(profile.innerPatternPropagation);
            seamSoftness = Mathf.Clamp(0.018f * seamSmoothingQuality, 0.006f, 0.08f);
            seamFeatherWidth = Mathf.Clamp(0.032f * seamSmoothingQuality, 0.012f, 0.1f);
            continuityCorrection = Mathf.Clamp01(0.24f + 0.36f * seamSmoothingQuality);
            radialEdgeSoftness = Mathf.Clamp(0.024f * seamSmoothingQuality, 0.01f, 0.1f);
            opticalDensity = Mathf.Clamp01(Mathf.Lerp(0.62f, 0.88f, profile.sourceCoverageTarget));
            visualNoiseAmount = Mathf.Clamp(profile.microDetailDensity * 0.06f, 0.025f, 0.12f);
            opticalDepthStrength = Mathf.Clamp01(0.22f + profile.opticalDistortionQuality * 0.1f);
            vignetteStrength = Mathf.Clamp01(0.08f + profile.vignetteQuality * 0.05f);
            edgeDarkening = Mathf.Clamp01(0.06f + profile.vignetteQuality * 0.05f);
            seamChromaticAberration = Mathf.Clamp(profile.chromaticAberrationQuality * 0.0012f, 0f, 0.002f);
            ApplyShaderValues();
        }

        public void ApplyViewerComfort(
            float maxRotationSpeed,
            float motionDamping,
            float smoothing,
            float maximumBrightness,
            float minimumContrast,
            float breathingRate)
        {
            maxPatternAngularVelocity = Mathf.Clamp(maxRotationSpeed, 1f, maxPatternSpinSpeedDeg);
            patternVelocityClamp = Mathf.Max(patternVelocityClamp, maxPatternAngularVelocity);
            patternRotationSpeed = Mathf.Clamp(patternRotationSpeed, minPatternSpinSpeedDeg, maxPatternSpinSpeedDeg);
            comfortPatternSpinSpeedLimit = Mathf.Clamp(maxRotationSpeed, 60f, maxPatternSpinSpeedDeg);
            manualRotationDegreesPerSecond = Mathf.Min(manualRotationDegreesPerSecond, maxPatternAngularVelocity * 3f);
            globalMotionDamping = Mathf.Clamp01(motionDamping);
            temporalSmoothing = Mathf.Clamp01(smoothing);
            patternPersistence = Mathf.Max(patternPersistence, Mathf.Lerp(0.62f, 0.92f, temporalSmoothing));
            distortionUpdateRate = Mathf.Clamp(distortionUpdateRate, 5f, 12f);
            centerExposure = Mathf.Min(centerExposure, maximumBrightness);
            centerBrightness = Mathf.Min(centerBrightness, maximumBrightness);
            centerBloomLimit = Mathf.Min(centerBloomLimit, maximumBrightness);
            brightness = Mathf.Min(brightness, Mathf.Max(1f, maximumBrightness));
            contrast = Mathf.Max(contrast, minimumContrast);
            centerContrast = Mathf.Max(centerContrast, minimumContrast);
            visualNoiseAmount = Mathf.Min(visualNoiseAmount, 0.12f);
            wobbleStrength = Mathf.Min(wobbleStrength, 0.012f);
            asymmetryStrength = Mathf.Min(asymmetryStrength, 0.01f);
            centerDriftStrength = Mathf.Min(centerDriftStrength, 0.008f);
            breathingSpeed = Mathf.Clamp(breathingRate, 0.1f, 0.5f);
            opticalBreathingPeriod = Mathf.Clamp(1f / Mathf.Max(0.01f, breathingRate), 2f, 10f);
            ApplyShaderValues();
        }

        public static float MapEffectLevel(EffectLevel level)
        {
            switch (level)
            {
                case EffectLevel.UltraLow:
                    return 0.25f;
                case EffectLevel.Low:
                    return 0.5f;
                case EffectLevel.High:
                    return 1.5f;
                case EffectLevel.UltraHigh:
                    return 2f;
                default:
                    return 1f;
            }
        }

        public void ApplyEffectLevel(EffectLevel level)
        {
            saturationLevel = level;
            contrastLevel = level;
            bloomLevel = level;
            motionLevel = level;
            ApplyShaderValues();
        }

        private void ApplyPremiumVisualQualityPreset()
        {
            brightness = 1.08f;
            contrast = 1.25f;
            saturation = 1.3f;
            vibrance = 1.15f;
            gamma = 0.92f;
            blackLevel = 0.015f;
            whiteLevel = 1.12f;
            sharpness = 0.16f;
        }

        private bool TryEstimateAverageSourceLuminance(out float averageLuminance)
        {
            averageLuminance = 0f;
            if (displayMaterial == null)
            {
                return false;
            }

            Texture2D sourceTexture = displayMaterial.GetTexture("_SourceTex") as Texture2D;
            if (sourceTexture == null)
            {
                return false;
            }

            try
            {
                const int samplesPerAxis = 5;
                float total = 0f;
                int count = 0;
                for (int y = 0; y < samplesPerAxis; y++)
                {
                    float v = (y + 0.5f) / samplesPerAxis;
                    for (int x = 0; x < samplesPerAxis; x++)
                    {
                        float u = (x + 0.5f) / samplesPerAxis;
                        Color sample = sourceTexture.GetPixelBilinear(u, v);
                        total += sample.r * 0.2126f + sample.g * 0.7152f + sample.b * 0.0722f;
                        count++;
                    }
                }

                averageLuminance = count > 0 ? total / count : 0f;
                return count > 0;
            }
            catch (UnityException)
            {
                averageLuminance = 0f;
                return false;
            }
        }

        private static float ApplyLevelAroundNeutral(float value, float neutral, EffectLevel level)
        {
            return neutral + (value - neutral) * MapEffectLevel(level);
        }

        private void SetMirrorAngleForSegments(int segments)
        {
            int safeSegments = ClampSegmentCount(segments);
            mirrorAngleDegrees = 360f / safeSegments;
        }

        private static int ResolveColorDepthSteps(KaleidoscopeColorDepthMode mode)
        {
            int index = Mathf.Clamp((int)mode, 0, ColorDepthSteps.Length - 1);
            return ColorDepthSteps[index];
        }

        private static string ResolveColorDepthDisplayName(KaleidoscopeColorDepthMode mode)
        {
            int index = Mathf.Clamp((int)mode, 0, ColorDepthDisplayNames.Length - 1);
            return ColorDepthDisplayNames[index];
        }

        private int ClampSegmentCount(int segments)
        {
            return FindNearestValidSegmentCount(segments);
        }

        private static int FindNearestValidSegmentCount(int segments)
        {
            int requested = Mathf.Clamp(segments, ValidSegmentCounts[0], MaxSegmentCount);
            int nearest = ValidSegmentCounts[0];
            int bestDistance = int.MaxValue;
            for (int i = 0; i < ValidSegmentCounts.Length; i++)
            {
                int distance = Mathf.Abs(ValidSegmentCounts[i] - requested);
                if (distance < bestDistance)
                {
                    nearest = ValidSegmentCounts[i];
                    bestDistance = distance;
                }
            }

            return nearest;
        }

        private static int FindNextValidSegmentCount(int current, int delta)
        {
            int nearest = FindNearestValidSegmentCount(current);
            int index = 0;
            for (int i = 0; i < ValidSegmentCounts.Length; i++)
            {
                if (ValidSegmentCounts[i] == nearest)
                {
                    index = i;
                    break;
                }
            }

            int direction = delta >= 0 ? 1 : -1;
            index = Mathf.Clamp(index + direction, 0, ValidSegmentCounts.Length - 1);
            return ValidSegmentCounts[index];
        }

        public void SetSourceTexture(Texture sourceTexture)
        {
            if (displayMaterial != null)
            {
                displayMaterial.SetTexture("_SourceTex", sourceTexture);
                if (secondarySourceTexture == null)
                {
                    displayMaterial.SetTexture("_SourceTexB", sourceTexture);
                }
            }
        }

        private void ApplyShaderValues()
        {
            if (displayMaterial == null)
            {
                return;
            }

            float motionScale = MapEffectLevel(motionLevel);
            float effectiveContrast = Mathf.Clamp(ApplyLevelAroundNeutral(contrast, 1f, contrastLevel), 0f, 3f);
            float effectiveSaturation = Mathf.Clamp(ApplyLevelAroundNeutral(saturation, 1f, saturationLevel), 0f, 2f);
            float effectiveCenterBloomLimit = Mathf.Clamp(centerBloomLimit * MapEffectLevel(bloomLevel), 0.2f, 2f);

            displayMaterial.SetFloat("_SegmentCount", ComputedSegmentCount);
            displayMaterial.SetFloat("colorDepthMode", (float)colorDepthMode);
            displayMaterial.SetFloat("colorSteps", Mathf.Max(2f, colorSteps));
            displayMaterial.SetFloat("paletteQuantizationStrength", paletteQuantizationStrength);
            displayMaterial.SetFloat("_UseMirrorAngleMode", useMirrorAngleMode ? 1f : 0f);
            displayMaterial.SetFloat("_MirrorAngleDegrees", mirrorAngleDegrees);
            displayMaterial.SetFloat("_PrismMode", (float)prismMode);
            displayMaterial.SetFloat("_Rotation", PatternRotation);
            displayMaterial.SetFloat("_Zoom", patternZoom);
            displayMaterial.SetVector("_CenterOffset", new Vector4(centerOffsetX, centerOffsetY, 0f, 0f));
            displayMaterial.SetFloat("_RadialDistortion", radialDistortion);
            displayMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
            displayMaterial.SetFloat("_SeamSoftness", seamSoftness);
            displayMaterial.SetFloat("_SeamAlignmentOffset", seamAlignmentOffset * Mathf.Deg2Rad);
            displayMaterial.SetFloat("_SeamChromaticAberrationEnabled", seamChromaticAberrationEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_SeamChromaticAberration", seamChromaticAberration);
            displayMaterial.SetFloat("_SeamBlendStrength", seamBlendingEnabled ? seamBlendStrength : 0f);
            displayMaterial.SetFloat("_SeamFeatherWidth", seamBlendingEnabled ? seamFeatherWidth : 0f);
            displayMaterial.SetFloat("_ContinuityCorrection", seamBlendingEnabled ? continuityCorrection : 0f);
            displayMaterial.SetFloat("_RadialEdgeSoftness", radialEdgeSoftness);
            displayMaterial.SetFloat("_SeamAntialiasingEnabled", seamAntialiasingEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_SeamAAWidth", seamAAWidth);
            displayMaterial.SetFloat("_SeamLineSuppression", seamLineSuppression);
            displayMaterial.SetFloat("_HighSpeedSpinAAEnabled", highSpeedSpinAAEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_HighSpeedSeamSoftening", highSpeedSeamSoftening * Mathf.InverseLerp(highSpeedNoiseFreezeThreshold, maxPatternSpinSpeedDeg, Mathf.Abs(effectivePatternRotationSpeed)));
            displayMaterial.SetFloat("_ShowSectorBoundaries", showSectorBoundaries ? 1f : 0f);
            displayMaterial.SetColor("_BoundaryDebugColor", boundaryDebugColor);
            displayMaterial.SetFloat("_CenterScale", centerScale);
            displayMaterial.SetFloat("_CenterBrightness", centerBrightness);
            displayMaterial.SetFloat("_CenterVignette", centerVignette);
            displayMaterial.SetFloat("_CenterStabilization", centerStabilization);
            displayMaterial.SetFloat("_CenterMaskEnabled", centerMaskEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_CenterMaskMode", (float)centerMaskMode);
            displayMaterial.SetFloat("_CenterMaskRadius", centerMaskRadius);
            displayMaterial.SetFloat("_CenterExposure", centerExposure);
            displayMaterial.SetFloat("_CenterFalloff", centerFalloff);
            displayMaterial.SetFloat("_CenterContrast", centerContrast);
            displayMaterial.SetFloat("_CenterGradientStrength", centerGradientStrength);
            displayMaterial.SetFloat("_CenterDetailBoost", centerDetailBoost);
            displayMaterial.SetFloat("_CenterBloomLimit", effectiveCenterBloomLimit);
            displayMaterial.SetFloat("_CenterCleanEnabled", centerCleanEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_CenterCleanRadius", centerCleanRadius);
            displayMaterial.SetFloat("_CenterCleanFeather", centerCleanFeather);
            displayMaterial.SetFloat("_CenterReconstructFromTexture", centerReconstructFromTexture ? 1f : 0f);
            displayMaterial.SetFloat("_CenterPatternContinuation", centerPatternContinuation);
            displayMaterial.SetFloat("_CenterFillMode", (float)centerFillMode);
            displayMaterial.SetFloat("_CenterWorkRadius", centerWorkRadius);
            displayMaterial.SetFloat("_CenterWorkFeather", centerWorkFeather);
            displayMaterial.SetFloat("_CenterBlendStrength", centerBlendStrength);
            displayMaterial.SetFloat("_CenterContinuationStrength", centerContinuationStrength);
            displayMaterial.SetFloat("_CenterDetailAmount", centerDetailAmount);
            displayMaterial.SetFloat("_CenterSampleScale", centerSampleScale);
            displayMaterial.SetFloat("_CenterReconstructionQuality", centerReconstructionQuality);
            displayMaterial.SetFloat("_CenterOnlyDebugMode", centerOnlyDebugMode ? 1f : 0f);
            displayMaterial.SetFloat("_CenterMaskPreview", centerMaskPreview ? 1f : 0f);
            displayMaterial.SetFloat("_PhysicalCenterArtifacts", physicalCenterArtifacts ? 1f : 0f);
            displayMaterial.SetFloat("_CenterArtifactOverrideEnabled", centerArtifactOverrideEnabled && directTextureSource ? 1f : 0f);
            displayMaterial.SetFloat("_CenterArtifactOverrideRadius", centerArtifactOverrideRadius);
            displayMaterial.SetFloat("_CenterArtifactOverrideMode", (float)centerArtifactOverrideMode);
            displayMaterial.SetFloat("_OpticalDensity", opticalDensity);
            displayMaterial.SetFloat("_VisualNoiseAmount", visualNoiseAmount);
            displayMaterial.SetFloat("_ForegroundWeight", foregroundWeight);
            displayMaterial.SetFloat("_MidgroundWeight", midgroundWeight);
            displayMaterial.SetFloat("_BackgroundWeight", backgroundWeight);
            displayMaterial.SetFloat("_DepthFadeStrength", depthFadeStrength);
            displayMaterial.SetFloat("_OpticalDepthStrength", opticalDepthStrength);
            displayMaterial.SetFloat("_SourceOverscanFactor", sourceOverscanFactor);
            displayMaterial.SetFloat("_EdgeRecursionBlend", edgeRecursionBlend);
            displayMaterial.SetFloat("_CenterConvergenceStrength", centerConvergenceStrength);
            displayMaterial.SetFloat("_RadialContinuation", radialContinuation);
            displayMaterial.SetFloat("_CenterRecursionBlend", centerRecursionBlend);
            displayMaterial.SetFloat("_InnerPatternPropagation", innerPatternPropagation);
            displayMaterial.SetFloat("_SeamSmoothingQuality", seamSmoothingQuality);
            displayMaterial.SetFloat("_OpticalDistortionQuality", opticalDistortionQuality);
            displayMaterial.SetFloat("_MaskEnabled", maskEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_MaskMode", (float)maskMode);
            displayMaterial.SetFloat("_MaskRadius", maskRadius);
            displayMaterial.SetFloat("_MaskSoftness", maskSoftness);
            displayMaterial.SetFloat("_MaskDarkness", maskDarkness);
            displayMaterial.SetFloat("_HexMaskRotation", hexMaskRotation * Mathf.Deg2Rad);
            displayMaterial.SetFloat("_VignetteEnabled", vignetteEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_VignetteStrength", vignetteStrength);
            displayMaterial.SetFloat("_VignetteSoftness", vignetteSoftness);
            displayMaterial.SetFloat("_EdgeDarkening", edgeDarkening);
            displayMaterial.SetFloat("_OpticalMaskFeather", opticalMaskFeather);
            displayMaterial.SetFloat("_LensImperfectionStrength", lensImperfectionStrength);
            displayMaterial.SetFloat("_RubyWeight", rubyWeight);
            displayMaterial.SetFloat("_EmeraldWeight", emeraldWeight);
            displayMaterial.SetFloat("_OpalWeight", opalWeight);
            displayMaterial.SetFloat("_QuartzWeight", quartzWeight);
            displayMaterial.SetFloat("_SaturationCompression", saturationCompression);
            displayMaterial.SetColor("_HighlightColorBias", highlightColorBias);
            displayMaterial.SetFloat("_Brightness", brightness);
            displayMaterial.SetFloat("_Contrast", effectiveContrast);
            displayMaterial.SetFloat("_Saturation", effectiveSaturation);
            displayMaterial.SetFloat("_Vibrance", vibrance);
            displayMaterial.SetFloat("_Gamma", gamma);
            displayMaterial.SetFloat("_BlackLevel", blackLevel);
            displayMaterial.SetFloat("_WhiteLevel", Mathf.Max(blackLevel + 0.01f, whiteLevel));
            displayMaterial.SetFloat("_Sharpness", sharpness);
            displayMaterial.SetFloat("_OrganicTime", sampledOrganicTime + rhythmPhase);
            displayMaterial.SetFloat("_WobbleEnabled", wobbleEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_WobbleStrength", smoothedWobbleStrength * motionScale);
            displayMaterial.SetFloat("_WobbleSpeed", wobbleSpeed * Mathf.Lerp(1f, 0.24f, globalMotionDamping));
            displayMaterial.SetFloat("_BreathingEnabled", breathingEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_BreathingAmplitude", (smoothedBreathingAmplitude + harmonicMotionStrength) * motionScale);
            displayMaterial.SetFloat("_BreathingSpeed", breathingSpeed * Mathf.Lerp(1f, 0.2f, globalMotionDamping));
            displayMaterial.SetFloat("_CenterDriftEnabled", centerDriftEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_CenterDriftStrength", smoothedCenterDriftStrength * motionScale);
            displayMaterial.SetFloat("_CenterDriftSpeed", centerDriftSpeed * Mathf.Lerp(1f, 0.25f, globalMotionDamping));
            displayMaterial.SetFloat("_SegmentVariationEnabled", segmentVariationEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_SegmentAngleVariation", segmentAngleVariation * Mathf.Lerp(1f, 0.45f, globalMotionDamping) * motionScale);
            displayMaterial.SetFloat("_SegmentBrightnessVariation", segmentBrightnessVariation * Mathf.Lerp(1f, 0.5f, globalMotionDamping));
            displayMaterial.SetFloat("_TemporalDriftEnabled", temporalDriftEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_DriftSpeed", driftSpeed * Mathf.Lerp(1f, 0.18f, globalMotionDamping));
            displayMaterial.SetFloat("_DriftAmount", smoothedDriftAmount * motionScale);
            displayMaterial.SetFloat("_AsymmetryEnabled", asymmetryEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_AsymmetryStrength", smoothedAsymmetryStrength * motionScale);
            displayMaterial.SetFloat("_TemporalDriftAmount", Mathf.Min(temporalDriftAmount * Mathf.Lerp(1f, 0.36f, globalMotionDamping) * motionScale, radialMotionClamp));
            displayMaterial.SetFloat("_RotationalDrift", smoothedRotationalDrift * motionScale);
            displayMaterial.SetFloat("_ScaleDrift", smoothedScaleDrift * motionScale);
            displayMaterial.SetFloat("_OpticalBreathingAmount", Mathf.Min(opticalBreathingAmount * Mathf.Lerp(1f, 0.42f, globalMotionDamping) * motionScale, radialMotionClamp));
            displayMaterial.SetFloat("_DirtyGlassEnabled", dirtyGlassEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_DirtyGlassStrength", dirtyGlassStrength);
            displayMaterial.SetFloat("_DirtyGlassScale", dirtyGlassScale);
            displayMaterial.SetFloat("_DirectTextureSource", directTextureSource ? 1f : 0f);
            displayMaterial.SetFloat("_AnimatedImageSource", animatedImageSource ? 1f : 0f);
            displayMaterial.SetVector("_SourceUvShake", new Vector4(sourceShakeOffset.x, sourceShakeOffset.y, 0f, 0f));
            displayMaterial.SetTexture("_SourceTexB", secondarySourceTexture != null ? secondarySourceTexture : displayMaterial.GetTexture("_SourceTex"));
            displayMaterial.SetFloat("imageScrollSpeed", imageScrollSpeed);
            displayMaterial.SetFloat("imageZoomSpeed", imageZoomSpeed);
            displayMaterial.SetFloat("imageRotationSpeed", imageRotationSpeed);
            displayMaterial.SetFloat("imageChangeInterval", imageChangeInterval);
            displayMaterial.SetFloat("imageTransitionDuration", imageTransitionDuration);
            displayMaterial.SetFloat("imageTransitionMode", imageTransitionMode);
            displayMaterial.SetVector("_ImageSourceOffset", new Vector4(imageSourceOffset.x, imageSourceOffset.y, 0f, 0f));
            displayMaterial.SetFloat("_ImageSourceZoom", imageSourceZoom);
            displayMaterial.SetFloat("_ImageSourceRotation", imageSourceRotation);
            displayMaterial.SetFloat("_ImageTransitionProgress", imageTransitionProgress);
            displayMaterial.SetFloat("_ImageMobiusDrift", imageMobiusDrift);
        }

        private void UpdateShake(float dt)
        {
            if (shakeTimeRemaining <= 0f)
            {
                sourceShakeOffset = Vector2.Lerp(sourceShakeOffset, Vector2.zero, 1f - Mathf.Exp(-shakeDamping * dt));
                return;
            }

            shakeTimeRemaining -= dt;
            sourceShakeOffset += sourceShakeVelocity * dt * 60f;
            sourceShakeVelocity = Vector2.Lerp(sourceShakeVelocity, Vector2.zero, 1f - Mathf.Exp(-shakeDamping * dt));
        }

        private void SmoothTemporalParameters(float dt)
        {
            float coherence = Mathf.Clamp01(temporalSmoothing * opticalFlowStrength);
            float response = Mathf.Lerp(12f, 1.4f, coherence);
            float blend = 1f - Mathf.Exp(-response * Mathf.Max(0f, dt));
            float dampingScale = Mathf.Lerp(1f, 0.34f, globalMotionDamping);
            float breathingFrequency = 1f / Mathf.Max(0.01f, opticalBreathingPeriod);
            float slowBreath = Mathf.Sin((opticalTime * breathingFrequency + rhythmPhase) * Mathf.PI * 2f) * harmonicMotionStrength;

            smoothedWobbleStrength = Mathf.Lerp(smoothedWobbleStrength, wobbleStrength * dampingScale, blend);
            smoothedBreathingAmplitude = Mathf.Lerp(smoothedBreathingAmplitude, breathingAmplitude * dampingScale + slowBreath, blend);
            smoothedCenterDriftStrength = Mathf.Lerp(smoothedCenterDriftStrength, centerDriftStrength * dampingScale, blend);
            smoothedAsymmetryStrength = Mathf.Lerp(smoothedAsymmetryStrength, asymmetryStrength * dampingScale, blend);
            smoothedDriftAmount = Mathf.Lerp(smoothedDriftAmount, Mathf.Min(driftAmount * dampingScale, radialMotionClamp), blend);
            smoothedRotationalDrift = Mathf.Lerp(smoothedRotationalDrift, rotationalDrift * dampingScale, blend);
            smoothedScaleDrift = Mathf.Lerp(smoothedScaleDrift, Mathf.Min(scaleDrift * dampingScale, radialMotionClamp), blend);
        }
    }
}
