using System;
using System.Collections.Generic;
using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.Source;
using UnityEngine;

namespace KaleidoscopeEngine.Performance
{
    public enum KaleidoscopeBottleneckKind
    {
        RenderTexture,
        Shader,
        Physics,
        Particles,
        Sparkles,
        UpdateFrequency,
        SourceDensity,
        PostProcessing,
        Overdraw,
        ImageLoading,
        ObjectCount,
        Stability
    }

    public enum KaleidoscopeDiagnosticSeverity
    {
        Info,
        Warning,
        Critical
    }

    [Serializable]
    public sealed class KaleidoscopePerformanceWarning
    {
        public KaleidoscopeBottleneckKind kind;
        public KaleidoscopeDiagnosticSeverity severity;
        public string title;
        public string detail;
        public string suggestion;
        public bool adaptiveQualityMayApply;

        public KaleidoscopePerformanceWarning(
            KaleidoscopeBottleneckKind kind,
            KaleidoscopeDiagnosticSeverity severity,
            string title,
            string detail,
            string suggestion,
            bool adaptiveQualityMayApply)
        {
            this.kind = kind;
            this.severity = severity;
            this.title = title;
            this.detail = detail;
            this.suggestion = suggestion;
            this.adaptiveQualityMayApply = adaptiveQualityMayApply;
        }
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopePerformanceAnalyzer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopeFpsMonitor fpsMonitor;
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopePhysicsChamber physicsChamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private GemSparkleController sparkleController;
        [SerializeField] private FakeCausticBunnyProjector causticProjector;
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;
        [SerializeField] private KaleidoscopeSourceLibrary sourceLibrary;
        [SerializeField] private ViewerComfortController comfortController;
        [SerializeField] private KaleidoscopeTemporalStabilizer temporalStabilizer;
        [SerializeField] private AdaptiveQualityController adaptiveQualityController;

        [Header("Sampling")]
        [SerializeField, Range(0.1f, 2f)] private float analysisInterval = 0.5f;

        private readonly List<KaleidoscopePerformanceWarning> warnings = new List<KaleidoscopePerformanceWarning>();
        private float nextAnalysisTime;

        public float RenderTextureCost { get; private set; }
        public float ShaderCost { get; private set; }
        public float PhysicsCost { get; private set; }
        public float ParticleCost { get; private set; }
        public float SparkleCost { get; private set; }
        public float UpdateFrequencyCost { get; private set; }
        public float SourceDensityCost { get; private set; }
        public float PostProcessingCost { get; private set; }
        public float OverdrawCost { get; private set; }
        public float ImageLoadingCost { get; private set; }
        public float ObjectCountCost { get; private set; }
        public float StabilityCost { get; private set; }
        public KaleidoscopeBottleneckKind PrimaryBottleneck { get; private set; }
        public string BottleneckSummary { get; private set; } = "No analysis yet.";
        public IReadOnlyList<KaleidoscopePerformanceWarning> Warnings => warnings;

        public void Configure(
            KaleidoscopeFpsMonitor monitor,
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopeMirrorController mirror,
            KaleidoscopePhysicsChamber chamber,
            GemstoneSpawner gemstoneSpawner,
            GemSparkleController sparkles,
            FakeCausticBunnyProjector caustics,
            KaleidoscopeSourceModeController sourceController,
            KaleidoscopeSourceLibrary library,
            ViewerComfortController comfort,
            KaleidoscopeTemporalStabilizer stabilizer,
            AdaptiveQualityController adaptiveController)
        {
            fpsMonitor = monitor;
            renderPipeline = pipeline;
            mirrorController = mirror;
            physicsChamber = chamber;
            spawner = gemstoneSpawner;
            sparkleController = sparkles;
            causticProjector = caustics;
            sourceModeController = sourceController;
            sourceLibrary = library;
            comfortController = comfort;
            temporalStabilizer = stabilizer;
            adaptiveQualityController = adaptiveController;
            Analyze();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextAnalysisTime)
            {
                return;
            }

            nextAnalysisTime = Time.unscaledTime + Mathf.Max(0.1f, analysisInterval);
            Analyze();
        }

        public void Analyze()
        {
            KaleidoscopeSourceModeManager modeManager = sourceModeController != null ? sourceModeController.ModeManager : null;
            bool directImageMode = modeManager != null && modeManager.ActiveSourceType == KaleidoscopeSourceType.TextureSource;
            float smoothedFps = fpsMonitor != null && fpsMonitor.SmoothedFps > 0f ? fpsMonitor.SmoothedFps : 60f;
            float targetFps = fpsMonitor != null ? Mathf.Max(1f, fpsMonitor.TargetFps) : 30f;
            float framePressure = Mathf.Clamp01(1f - smoothedFps / targetFps);
            float averageFrameMs = fpsMonitor != null ? fpsMonitor.AverageFrameMs : 0f;

            float rtPixels = renderPipeline != null
                ? renderPipeline.RenderTextureWidth * renderPipeline.RenderTextureHeight
                : 0f;
            RenderTextureCost = ClampScore(rtPixels / (3072f * 3072f) * 0.48f +
                                           (renderPipeline != null ? renderPipeline.EstimatedRenderTextureMemoryMB / 128f : 0f) * 0.34f +
                                           framePressure * 0.42f);

            ShaderCost = ClampScore((mirrorController != null ? mirrorController.MosaicCohesionScore : 0f) * 0.28f +
                                    (mirrorController != null ? mirrorController.EdgeRecursionBlend : 0f) * 0.18f +
                                    (mirrorController != null && mirrorController.WobbleEnabled ? 0.12f : 0f) +
                                    (mirrorController != null && mirrorController.BreathingEnabled ? 0.08f : 0f) +
                                    (mirrorController != null && mirrorController.AsymmetryEnabled ? 0.08f : 0f) +
                                    framePressure * 0.26f);

            int activeObjects = directImageMode || spawner == null ? 0 : spawner.SpawnedObjects.Count;
            float axialLoad = directImageMode || physicsChamber == null ? 0f : Mathf.Abs(physicsChamber.EffectiveTubeAxialSpeedDeg) / 200f;
            PhysicsCost = ClampScore(activeObjects / 240f * 0.42f +
                                     (!directImageMode && spawner != null ? spawner.PhysicsMicroCrystalCount / 120f : 0f) * 0.26f +
                                     axialLoad * 0.18f +
                                     framePressure * 0.34f);

            int activeSparkles = directImageMode || sparkleController == null ? 0 : sparkleController.ActiveSparkles;
            int effectiveSparkles = sparkleController != null ? Mathf.Max(1, sparkleController.EffectiveMaxActiveSparkles) : 1;
            SparkleCost = ClampScore(activeSparkles / (float)effectiveSparkles * 0.52f +
                                     (!directImageMode && sparkleController != null ? sparkleController.EffectiveSparkleFrequency / 18f : 0f) * 0.24f +
                                     framePressure * 0.32f);

            int effectiveCaustics = directImageMode || causticProjector == null ? 0 : causticProjector.EffectiveSpotCount;
            ParticleCost = ClampScore(SparkleCost * 0.52f +
                                      effectiveCaustics / 18f * 0.3f +
                                      framePressure * 0.24f);

            int sourceRate = renderPipeline != null ? renderPipeline.SourceUpdateRateLimit : 0;
            UpdateFrequencyCost = ClampScore((sourceRate <= 0 ? 0.62f : sourceRate / 60f) * 0.38f +
                                             (sparkleController != null ? sparkleController.EffectiveSparkleFrequency / 24f : 0f) * 0.24f +
                                             framePressure * 0.38f);

            float coverage = directImageMode || spawner == null ? 0f : spawner.SourceCoverageEstimate;
            float targetCoverage = directImageMode || spawner == null ? 1f : Mathf.Max(0.01f, spawner.SourceCoverageTarget);
            SourceDensityCost = ClampScore(coverage / targetCoverage * 0.42f +
                                           (!directImageMode && sourceLibrary != null ? sourceLibrary.CurrentPerformanceCost : 0f) * 0.28f +
                                           (!directImageMode && spawner != null ? spawner.EffectiveVisualMicroChipCount / 1400f : 0f) * 0.24f +
                                           framePressure * 0.22f);

            PostProcessingCost = ClampScore((comfortController != null ? comfortController.OverexposedPixelRatio : 0f) * 0.38f +
                                            (comfortController != null ? comfortController.FlickerScore : 0f) * 0.34f +
                                            (comfortController != null ? Mathf.Clamp01(0.24f - comfortController.ContrastScore) : 0f) * 0.3f +
                                            framePressure * 0.22f);

            OverdrawCost = ClampScore(SourceDensityCost * 0.38f +
                                      SparkleCost * 0.24f +
                                      PostProcessingCost * 0.22f +
                                      (comfortController != null ? comfortController.OverexposedPixelRatio : 0f) * 0.3f);

            ImageWallpaperSourceMode imageMode = sourceModeController != null && sourceModeController.CurrentMode == KaleidoscopeSourceModeKind.ImageWallpaper
                ? sourceModeController.CurrentSourceTexture != null ? sourceModeController.GetComponent<ImageWallpaperSourceMode>() : null
                : null;
            ImageLoadingCost = ClampScore((modeManager != null ? modeManager.ImageReloadCount / 4f : imageMode != null ? imageMode.ImageReloadCount / 4f : 0f) * 0.5f +
                                          (directImageMode && modeManager != null && modeManager.PhysicalPipelineActive ? 0.6f : renderPipeline != null && renderPipeline.PhysicalPipeline ? 0.35f : 0f) +
                                          framePressure * 0.3f);
            ObjectCountCost = ClampScore(activeObjects / 100f * 0.45f +
                                         (!directImageMode && spawner != null ? spawner.LightweightVisualCrystalCount / 7000f : 0f) * 0.22f +
                                         framePressure * 0.28f);

            StabilityCost = ClampScore((temporalStabilizer != null ? temporalStabilizer.TemporalChangeScore : 0f) * 0.44f +
                                       (comfortController != null ? comfortController.FlickerScore : 0f) * 0.34f +
                                       Mathf.Clamp01(Mathf.Abs(fpsMonitor != null ? fpsMonitor.FpsTrend : 0f) / 4f) * 0.22f);

            ResolvePrimaryBottleneck();
            BuildWarnings(smoothedFps, targetFps, averageFrameMs, directImageMode, modeManager);
        }

        private void ResolvePrimaryBottleneck()
        {
            PrimaryBottleneck = KaleidoscopeBottleneckKind.RenderTexture;
            float max = RenderTextureCost;
            CheckPrimary(KaleidoscopeBottleneckKind.Shader, ShaderCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.Physics, PhysicsCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.Particles, ParticleCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.Sparkles, SparkleCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.UpdateFrequency, UpdateFrequencyCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.SourceDensity, SourceDensityCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.PostProcessing, PostProcessingCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.Overdraw, OverdrawCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.ImageLoading, ImageLoadingCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.ObjectCount, ObjectCountCost, ref max);
            CheckPrimary(KaleidoscopeBottleneckKind.Stability, StabilityCost, ref max);

            BottleneckSummary = max < 0.55f
                ? "No dominant bottleneck detected."
                : $"{PrimaryBottleneck} pressure {max:P0}.";
        }

        private void CheckPrimary(KaleidoscopeBottleneckKind kind, float score, ref float max)
        {
            if (score <= max)
            {
                return;
            }

            max = score;
            PrimaryBottleneck = kind;
        }

        private void BuildWarnings(float smoothedFps, float targetFps, float averageFrameMs, bool directImageMode, KaleidoscopeSourceModeManager modeManager)
        {
            warnings.Clear();
            bool adaptiveMayApply = adaptiveQualityController != null && adaptiveQualityController.AdaptiveQualityEnabled;

            if (directImageMode && modeManager != null && modeManager.PhysicalPipelineActive)
            {
                AddWarning(KaleidoscopeBottleneckKind.ImageLoading, KaleidoscopeDiagnosticSeverity.Critical,
                    "Image mode is running physical pipeline",
                    "Physical source systems are active while using a direct user image source.",
                    "Disable tube, source camera, crystals, sparkles, and caustics for Image Mode.",
                    false);
            }

            if (RenderTextureCost > 0.72f && smoothedFps < targetFps)
            {
                AddWarning(KaleidoscopeBottleneckKind.RenderTexture, SeverityFor(RenderTextureCost),
                    "RT resolution too high for current FPS",
                    $"RT {RenderTextureSize()} costs about {renderPipeline?.EstimatedRenderTextureMemoryMB:F1} MB; frame time {averageFrameMs:F1} ms.",
                    "Lower RT quality or supersampling before increasing source density.",
                    adaptiveMayApply);
            }

            if (!directImageMode && SparkleCost > 0.76f)
            {
                AddWarning(KaleidoscopeBottleneckKind.Sparkles, SeverityFor(SparkleCost),
                    "Sparkle update cost excessive",
                    $"Sparkles {sparkleController?.ActiveSparkles}/{sparkleController?.EffectiveMaxActiveSparkles}, frequency {sparkleController?.EffectiveSparkleFrequency:F1}.",
                    "Reduce sparkle count or update rate.",
                    adaptiveMayApply);
            }

            if (!directImageMode && PhysicsCost > 0.76f)
            {
                AddWarning(KaleidoscopeBottleneckKind.Physics, SeverityFor(PhysicsCost),
                    "Physics micro objects causing instability",
                    $"Active objects {spawner?.SpawnedObjects.Count}, physics micro crystals {spawner?.PhysicsMicroCrystalCount}.",
                    "Reduce microchip density, large active body count, or tube speed.",
                    adaptiveMayApply);
            }

            if (!directImageMode && SourceDensityCost > 0.78f)
            {
                AddWarning(KaleidoscopeBottleneckKind.SourceDensity, SeverityFor(SourceDensityCost),
                    "Source density exceeds safe budget",
                    $"Coverage {spawner?.SourceCoverageEstimate:F2}/{spawner?.SourceCoverageTarget:F2}, visual chips {spawner?.EffectiveVisualMicroChipCount}.",
                    "Lower source density or use a lighter source preset.",
                    adaptiveMayApply);
            }

            if (OverdrawCost > 0.76f || PostProcessingCost > 0.78f)
            {
                AddWarning(KaleidoscopeBottleneckKind.Overdraw, SeverityFor(Mathf.Max(OverdrawCost, PostProcessingCost)),
                    "Bloom overdraw detected",
                    $"Overexposed pixels {comfortController?.OverexposedPixelRatio:P0}, flicker {comfortController?.FlickerScore:F2}.",
                    "Reduce bloom-like brightness, sparkle intensity, or source density.",
                    adaptiveMayApply);
            }

            if (ShaderCost > 0.82f || StabilityCost > 0.82f)
            {
                AddWarning(KaleidoscopeBottleneckKind.Shader, SeverityFor(Mathf.Max(ShaderCost, StabilityCost)),
                    "Shader instability risk",
                    $"Mosaic score {mirrorController?.MosaicCohesionScore:F2}, temporal change {temporalStabilizer?.TemporalChangeScore:F2}.",
                    "Simplify asymmetry, wobble, edge recursion, or temporal motion.",
                    false);
            }
        }

        private void AddWarning(
            KaleidoscopeBottleneckKind kind,
            KaleidoscopeDiagnosticSeverity severity,
            string title,
            string detail,
            string suggestion,
            bool adaptiveMayApply)
        {
            warnings.Add(new KaleidoscopePerformanceWarning(kind, severity, title, detail, suggestion, adaptiveMayApply));
        }

        private KaleidoscopeDiagnosticSeverity SeverityFor(float score)
        {
            if (score >= 0.9f)
            {
                return KaleidoscopeDiagnosticSeverity.Critical;
            }

            return score >= 0.72f ? KaleidoscopeDiagnosticSeverity.Warning : KaleidoscopeDiagnosticSeverity.Info;
        }

        private string RenderTextureSize()
        {
            return renderPipeline != null
                ? $"{renderPipeline.RenderTextureWidth}x{renderPipeline.RenderTextureHeight}"
                : "n/a";
        }

        public string ExplainCurrentBottleneck()
        {
            switch (PrimaryBottleneck)
            {
                case KaleidoscopeBottleneckKind.Physics:
                    return "physics bottleneck: active rigidbodies or tube motion dominate frame pressure.";
                case KaleidoscopeBottleneckKind.RenderTexture:
                case KaleidoscopeBottleneckKind.Shader:
                    return "RT/shader bottleneck: render texture size or kaleidoscope shader complexity is the main pressure.";
                case KaleidoscopeBottleneckKind.ImageLoading:
                    return "image loading bottleneck: image reloads, downscale work, or physical pipeline leakage is active.";
                case KaleidoscopeBottleneckKind.ObjectCount:
                case KaleidoscopeBottleneckKind.SourceDensity:
                    return "object count bottleneck: source objects or dense visual crystals are too high for the current budget.";
                case KaleidoscopeBottleneckKind.Overdraw:
                case KaleidoscopeBottleneckKind.Particles:
                case KaleidoscopeBottleneckKind.Sparkles:
                    return "overdraw bottleneck: transparent crystals, sparkles, or bright layers are filling too much screen area.";
                default:
                    return BottleneckSummary;
            }
        }

        private static float ClampScore(float score)
        {
            return Mathf.Clamp01(float.IsNaN(score) || float.IsInfinity(score) ? 0f : score);
        }
    }
}
