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

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeMirrorController : MonoBehaviour
    {
        [Header("Mirror")]
        [SerializeField] private bool useMirrorAngleMode = true;
        [SerializeField] private KaleidoscopePrismMode prismMode = KaleidoscopePrismMode.ThreeMirrorTriangular;
        [SerializeField, Range(5f, 180f)] private float mirrorAngleDegrees = 60f;
        [SerializeField, Range(1, 24)] private int segmentCountManual = 6;
        [Tooltip("Static angle offset in degrees before runtime rotation is applied.")]
        [SerializeField] private float mirrorAngleOffset;
        [Tooltip("Automatic pattern rotation in degrees per second.")]
        [SerializeField] private float patternRotationSpeed = 4f;
        [SerializeField, Range(0.25f, 4f)] private float patternZoom = 1.18f;
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
        [SerializeField] private bool showSectorBoundaries;
        [SerializeField] private Color boundaryDebugColor = new Color(1f, 0.25f, 0.08f, 1f);

        [Header("Center Composition")]
        [SerializeField, Range(0.4f, 2.5f)] private float centerScale = 1.05f;
        [SerializeField, Range(0f, 3f)] private float centerBrightness = 0.94f;
        [SerializeField, Range(0f, 1f)] private float centerVignette = 0.18f;
        [SerializeField, Range(0f, 1f)] private float centerStabilization = 0.45f;
        [SerializeField, Range(0.02f, 0.6f)] private float centerMaskRadius = 0.18f;
        [SerializeField, Range(0f, 1.5f)] private float centerExposure = 0.68f;
        [SerializeField, Range(0.02f, 0.8f)] private float centerFalloff = 0.34f;
        [SerializeField, Range(0f, 2f)] private float centerContrast = 0.82f;
        [SerializeField, Range(0f, 0.6f)] private float centerGradientStrength = 0.16f;
        [SerializeField, Range(0f, 0.6f)] private float centerDetailBoost = 0.12f;
        [SerializeField, Range(0.2f, 2f)] private float centerBloomLimit = 0.82f;

        [Header("Mosaic Cohesion")]
        [SerializeField, Range(0f, 1f)] private float opticalDensity = 0.68f;
        [SerializeField, Range(0f, 0.4f)] private float visualNoiseAmount = 0.08f;
        [SerializeField, Range(0f, 2f)] private float foregroundWeight = 0.72f;
        [SerializeField, Range(0f, 2f)] private float midgroundWeight = 0.9f;
        [SerializeField, Range(0f, 2f)] private float backgroundWeight = 0.42f;
        [SerializeField, Range(0f, 1f)] private float depthFadeStrength = 0.16f;
        [SerializeField, Range(0f, 1f)] private float opticalDepthStrength = 0.24f;

        [Header("Optical Continuity")]
        [SerializeField, Range(1f, 1.6f)] private float sourceOverscanFactor = 1.2f;
        [SerializeField, Range(0f, 1f)] private float edgeRecursionBlend = 0.5f;
        [SerializeField, Range(0f, 1f)] private float centerConvergenceStrength = 0.6f;
        [SerializeField, Range(0f, 1f)] private float radialContinuation = 0.5f;
        [SerializeField, Range(0f, 1f)] private float centerRecursionBlend = 0.44f;
        [SerializeField, Range(0f, 1f)] private float innerPatternPropagation = 0.4f;
        [SerializeField, Range(0.25f, 2f)] private float seamSmoothingQuality = 1.25f;
        [SerializeField, Range(0.25f, 2f)] private float opticalDistortionQuality = 1.08f;

        [Header("Optical Mask")]
        [SerializeField] private bool maskEnabled = true;
        [SerializeField] private KaleidoscopeMaskMode maskMode = KaleidoscopeMaskMode.CircularEyepiece;
        [SerializeField, Range(0.1f, 1.2f)] private float maskRadius = 0.72f;
        [SerializeField, Range(0.001f, 0.5f)] private float maskSoftness = 0.16f;
        [SerializeField, Range(0f, 1f)] private float maskDarkness = 0.82f;
        [SerializeField] private float hexMaskRotation;
        [SerializeField] private bool vignetteEnabled = true;
        [SerializeField, Range(0f, 1f)] private float vignetteStrength = 0.2f;
        [SerializeField, Range(0.2f, 1.2f)] private float vignetteSoftness = 0.72f;
        [SerializeField, Range(0f, 1f)] private float edgeDarkening = 0.16f;
        [SerializeField, Range(0f, 0.25f)] private float opticalMaskFeather = 0.05f;
        [SerializeField, Range(0f, 0.08f)] private float lensImperfectionStrength = 0.012f;

        [Header("Color Hierarchy")]
        [SerializeField, Range(0f, 2f)] private float rubyWeight = 0.72f;
        [SerializeField, Range(0f, 2f)] private float emeraldWeight = 1.08f;
        [SerializeField, Range(0f, 2f)] private float opalWeight = 1.04f;
        [SerializeField, Range(0f, 2f)] private float quartzWeight = 0.92f;
        [SerializeField, Range(0f, 1f)] private float saturationCompression = 0.28f;
        [SerializeField] private Color highlightColorBias = new Color(0.82f, 0.9f, 1f, 1f);

        [Header("Image")]
        [SerializeField, Range(0f, 3f)] private float brightness = 1.05f;
        [SerializeField, Range(0f, 3f)] private float contrast = 1.08f;
        [SerializeField, Range(0f, 2f)] private float saturation = 1.1f;

        [Header("Organic Imperfections")]
        [SerializeField] private bool wobbleEnabled = true;
        [SerializeField, Range(0f, 0.05f)] private float wobbleStrength = 0.012f;
        [SerializeField, Range(0f, 4f)] private float wobbleSpeed = 0.8f;
        [SerializeField] private bool breathingEnabled = true;
        [SerializeField, Range(0f, 0.05f)] private float breathingAmplitude = 0.012f;
        [SerializeField, Range(0f, 2f)] private float breathingSpeed = 0.28f;
        [SerializeField] private bool centerDriftEnabled = true;
        [SerializeField, Range(0f, 0.04f)] private float centerDriftStrength = 0.008f;
        [SerializeField, Range(0f, 2f)] private float centerDriftSpeed = 0.22f;
        [SerializeField] private bool segmentVariationEnabled = true;
        [SerializeField, Range(0f, 0.04f)] private float segmentAngleVariation = 0.01f;
        [SerializeField, Range(0f, 0.12f)] private float segmentBrightnessVariation = 0.035f;
        [SerializeField] private bool temporalDriftEnabled = true;
        [SerializeField, Range(0f, 1f)] private float driftSpeed = 0.08f;
        [SerializeField, Range(0f, 0.04f)] private float driftAmount = 0.01f;
        [SerializeField] private bool asymmetryEnabled = true;
        [SerializeField, Range(0f, 0.05f)] private float asymmetryStrength = 0.009f;
        [SerializeField, Range(0f, 0.04f)] private float temporalDriftAmount = 0.012f;
        [SerializeField, Range(0f, 0.04f)] private float rotationalDrift = 0.008f;
        [SerializeField, Range(0f, 0.04f)] private float scaleDrift = 0.01f;
        [SerializeField, Range(0f, 0.04f)] private float opticalBreathingAmount = 0.01f;

        [Header("Temporal Stability")]
        [SerializeField, Range(0f, 1f)] private float globalMotionDamping = 0.62f;
        [SerializeField, Range(0.01f, 4f)] private float opticalInertia = 1.6f;
        [SerializeField, Range(0f, 1f)] private float temporalSmoothing = 0.72f;
        [SerializeField, Range(0f, 1f)] private float patternPersistence = 0.68f;
        [SerializeField, Range(1f, 30f)] private float distortionUpdateRate = 10f;
        [SerializeField, Range(0f, 1f)] private float opticalFlowStrength = 0.72f;
        [SerializeField, Range(0.1f, 20f)] private float patternVelocityClamp = 4f;
        [SerializeField, Range(1f, 90f)] private float maxPatternAngularVelocity = 18f;
        [SerializeField, Range(0f, 0.2f)] private float radialMotionClamp = 0.025f;
        [SerializeField, Range(0f, 1f)] private float rhythmPhase;
        [SerializeField, Range(2f, 20f)] private float opticalBreathingPeriod = 8f;
        [SerializeField, Range(0f, 0.05f)] private float harmonicMotionStrength = 0.006f;

        [Header("Eyepiece Finish")]
        [SerializeField] private bool dirtyGlassEnabled = true;
        [SerializeField, Range(0f, 0.08f)] private float dirtyGlassStrength = 0.018f;
        [SerializeField, Range(16f, 320f)] private float dirtyGlassScale = 120f;

        [Header("Input Response")]
        [SerializeField] private float manualRotationDegreesPerSecond = 72f;
        [SerializeField] private float zoomStep = 0.7f;
        [SerializeField] private float distortionStep = 0.35f;
        [SerializeField] private float densityStep = 0.04f;
        [SerializeField] private float centerExposureStep = 0.35f;
        [SerializeField] private float rotationalDriftStep = 0.002f;

        private Material displayMaterial;
        private float runtimeRotation;
        private float smoothedRotationVelocity;
        private float opticalTime;
        private float nextDistortionUpdateTime;
        private float sampledOrganicTime;
        private float smoothedWobbleStrength;
        private float smoothedBreathingAmplitude;
        private float smoothedCenterDriftStrength;
        private float smoothedAsymmetryStrength;
        private float smoothedDriftAmount;
        private float smoothedRotationalDrift;
        private float smoothedScaleDrift;

        public int SegmentCount => ComputedSegmentCount;
        public int ManualSegmentCount => segmentCountManual;
        public int ComputedSegmentCount => useMirrorAngleMode ? Mathf.Clamp(Mathf.RoundToInt(360f / Mathf.Max(1f, mirrorAngleDegrees)), 1, 24) : segmentCountManual;
        public bool UseMirrorAngleMode => useMirrorAngleMode;
        public string PrismModeName => prismMode.ToString();
        public float MirrorAngleDegrees => mirrorAngleDegrees;
        public float PatternZoom => patternZoom;
        public float PatternRotation => runtimeRotation + mirrorAngleOffset * Mathf.Deg2Rad;
        public float RadialDistortion => radialDistortion;
        public float SeamSoftness => seamSoftness;
        public float SeamBlendStrength => seamBlendingEnabled ? seamBlendStrength : 0f;
        public float CenterMaskRadius => centerMaskRadius;
        public float CenterExposure => centerExposure;
        public float OpticalDensity => opticalDensity;
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

        public void Configure(Material material)
        {
            displayMaterial = material;
            ApplyShaderValues();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            float dampingScale = Mathf.Lerp(1f, 0.28f, globalMotionDamping);
            float targetVelocity = Mathf.Clamp(
                patternRotationSpeed * dampingScale,
                -maxPatternAngularVelocity,
                maxPatternAngularVelocity) * Mathf.Deg2Rad;
            float velocityResponse = Mathf.Max(0.01f, opticalInertia);
            smoothedRotationVelocity = Mathf.Lerp(
                smoothedRotationVelocity,
                targetVelocity,
                1f - Mathf.Exp(-velocityResponse * dt));
            runtimeRotation += Mathf.Clamp(
                smoothedRotationVelocity,
                -patternVelocityClamp * Mathf.Deg2Rad,
                patternVelocityClamp * Mathf.Deg2Rad) * dt;

            opticalTime += dt * Mathf.Lerp(1f, 0.22f, globalMotionDamping);
            if (Time.unscaledTime >= nextDistortionUpdateTime)
            {
                sampledOrganicTime = Mathf.Lerp(sampledOrganicTime, opticalTime, 1f - patternPersistence * 0.82f);
                nextDistortionUpdateTime = Time.unscaledTime + 1f / Mathf.Max(1f, distortionUpdateRate);
            }

            SmoothTemporalParameters(dt);
            ApplyShaderValues();
        }

        public void AdjustSegmentCount(int delta)
        {
            segmentCountManual = Mathf.Clamp(segmentCountManual + delta, 1, 24);
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
            patternZoom = Mathf.Clamp(patternZoom + direction * zoomStep * Time.deltaTime, 0.25f, 4f);
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
            patternZoom = 1.18f;
            radialDistortion = 0.08f;
            seamBlendingEnabled = true;
            seamBlendStrength = 0.8f;
            seamFeatherWidth = 0.04f;
            continuityCorrection = 0.32f;
            radialEdgeSoftness = 0.035f;
            showSectorBoundaries = false;
            centerBrightness = 0.94f;
            centerExposure = 0.68f;
            centerFalloff = 0.34f;
            centerContrast = 0.82f;
            centerGradientStrength = 0.16f;
            centerDetailBoost = 0.12f;
            centerBloomLimit = 0.82f;
            opticalDensity = 0.68f;
            visualNoiseAmount = 0.08f;
            sourceOverscanFactor = 1.2f;
            edgeRecursionBlend = 0.5f;
            centerConvergenceStrength = 0.6f;
            radialContinuation = 0.5f;
            centerRecursionBlend = 0.44f;
            innerPatternPropagation = 0.4f;
            seamSmoothingQuality = 1.25f;
            opticalDistortionQuality = 1.08f;
            maskEnabled = true;
            vignetteEnabled = true;
            rubyWeight = 0.72f;
            emeraldWeight = 1.08f;
            opalWeight = 1.04f;
            quartzWeight = 0.92f;
            saturationCompression = 0.28f;
            wobbleEnabled = true;
            breathingEnabled = true;
            centerDriftEnabled = true;
            segmentVariationEnabled = true;
            temporalDriftEnabled = true;
            asymmetryEnabled = true;
            asymmetryStrength = 0.009f;
            temporalDriftAmount = 0.012f;
            rotationalDrift = 0.008f;
            scaleDrift = 0.01f;
            opticalBreathingAmount = 0.01f;
            globalMotionDamping = 0.62f;
            opticalInertia = 1.6f;
            temporalSmoothing = 0.72f;
            patternPersistence = 0.68f;
            distortionUpdateRate = 10f;
            opticalFlowStrength = 0.72f;
            patternVelocityClamp = 4f;
            maxPatternAngularVelocity = 18f;
            radialMotionClamp = 0.025f;
            rhythmPhase = 0f;
            opticalBreathingPeriod = 8f;
            harmonicMotionStrength = 0.006f;
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
            visualNoiseAmount = Mathf.Clamp(profile.microDetailDensity * 0.1f, 0.04f, 0.18f);
            opticalDepthStrength = Mathf.Clamp01(0.18f + profile.opticalDistortionQuality * 0.08f);
            vignetteStrength = Mathf.Clamp01(0.16f + profile.vignetteQuality * 0.08f);
            edgeDarkening = Mathf.Clamp01(0.12f + profile.vignetteQuality * 0.08f);
            seamChromaticAberration = Mathf.Clamp(profile.chromaticAberrationQuality * 0.0012f, 0f, 0.002f);
            ApplyShaderValues();
        }

        private void SetMirrorAngleForSegments(int segments)
        {
            int safeSegments = Mathf.Clamp(segments, 1, 24);
            mirrorAngleDegrees = 360f / safeSegments;
        }

        public void SetSourceTexture(Texture sourceTexture)
        {
            if (displayMaterial != null)
            {
                displayMaterial.SetTexture("_SourceTex", sourceTexture);
            }
        }

        private void ApplyShaderValues()
        {
            if (displayMaterial == null)
            {
                return;
            }

            displayMaterial.SetFloat("_SegmentCount", ComputedSegmentCount);
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
            displayMaterial.SetFloat("_ShowSectorBoundaries", showSectorBoundaries ? 1f : 0f);
            displayMaterial.SetColor("_BoundaryDebugColor", boundaryDebugColor);
            displayMaterial.SetFloat("_CenterScale", centerScale);
            displayMaterial.SetFloat("_CenterBrightness", centerBrightness);
            displayMaterial.SetFloat("_CenterVignette", centerVignette);
            displayMaterial.SetFloat("_CenterStabilization", centerStabilization);
            displayMaterial.SetFloat("_CenterMaskRadius", centerMaskRadius);
            displayMaterial.SetFloat("_CenterExposure", centerExposure);
            displayMaterial.SetFloat("_CenterFalloff", centerFalloff);
            displayMaterial.SetFloat("_CenterContrast", centerContrast);
            displayMaterial.SetFloat("_CenterGradientStrength", centerGradientStrength);
            displayMaterial.SetFloat("_CenterDetailBoost", centerDetailBoost);
            displayMaterial.SetFloat("_CenterBloomLimit", centerBloomLimit);
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
            displayMaterial.SetFloat("_Contrast", contrast);
            displayMaterial.SetFloat("_Saturation", saturation);
            displayMaterial.SetFloat("_OrganicTime", sampledOrganicTime + rhythmPhase);
            displayMaterial.SetFloat("_WobbleEnabled", wobbleEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_WobbleStrength", smoothedWobbleStrength);
            displayMaterial.SetFloat("_WobbleSpeed", wobbleSpeed * Mathf.Lerp(1f, 0.24f, globalMotionDamping));
            displayMaterial.SetFloat("_BreathingEnabled", breathingEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_BreathingAmplitude", smoothedBreathingAmplitude + harmonicMotionStrength);
            displayMaterial.SetFloat("_BreathingSpeed", breathingSpeed * Mathf.Lerp(1f, 0.2f, globalMotionDamping));
            displayMaterial.SetFloat("_CenterDriftEnabled", centerDriftEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_CenterDriftStrength", smoothedCenterDriftStrength);
            displayMaterial.SetFloat("_CenterDriftSpeed", centerDriftSpeed * Mathf.Lerp(1f, 0.25f, globalMotionDamping));
            displayMaterial.SetFloat("_SegmentVariationEnabled", segmentVariationEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_SegmentAngleVariation", segmentAngleVariation * Mathf.Lerp(1f, 0.45f, globalMotionDamping));
            displayMaterial.SetFloat("_SegmentBrightnessVariation", segmentBrightnessVariation * Mathf.Lerp(1f, 0.5f, globalMotionDamping));
            displayMaterial.SetFloat("_TemporalDriftEnabled", temporalDriftEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_DriftSpeed", driftSpeed * Mathf.Lerp(1f, 0.18f, globalMotionDamping));
            displayMaterial.SetFloat("_DriftAmount", smoothedDriftAmount);
            displayMaterial.SetFloat("_AsymmetryEnabled", asymmetryEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_AsymmetryStrength", smoothedAsymmetryStrength);
            displayMaterial.SetFloat("_TemporalDriftAmount", Mathf.Min(temporalDriftAmount * Mathf.Lerp(1f, 0.36f, globalMotionDamping), radialMotionClamp));
            displayMaterial.SetFloat("_RotationalDrift", smoothedRotationalDrift);
            displayMaterial.SetFloat("_ScaleDrift", smoothedScaleDrift);
            displayMaterial.SetFloat("_OpticalBreathingAmount", Mathf.Min(opticalBreathingAmount * Mathf.Lerp(1f, 0.42f, globalMotionDamping), radialMotionClamp));
            displayMaterial.SetFloat("_DirtyGlassEnabled", dirtyGlassEnabled ? 1f : 0f);
            displayMaterial.SetFloat("_DirtyGlassStrength", dirtyGlassStrength);
            displayMaterial.SetFloat("_DirtyGlassScale", dirtyGlassScale);
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
