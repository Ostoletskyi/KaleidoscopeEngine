using System.Collections.Generic;
using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Geometry;
using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Lighting;
using KaleidoscopeEngine.Materials;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.Performance;
using KaleidoscopeEngine.Scenario;
using KaleidoscopeEngine.Source;
using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public enum KaleidoscopeDebugPanelMode
    {
        Hidden,
        Compact,
        Full
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopePhysicsChamber chamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private PhysicsSandboxCameraController cameraController;
        [SerializeField] private PhysicsSandboxChamberDebugView chamberDebugView;
        [SerializeField] private PhysicsSandboxMetrics metrics;
        [SerializeField] private GemstoneMaterialAssigner materialAssigner;
        [SerializeField] private KaleidoscopeLightingRig lightingRig;
        [SerializeField] private GemGeometryAssigner geometryAssigner;
        [SerializeField] private GemSparkleController sparkleController;
        [SerializeField] private FakeCausticBunnyProjector causticProjector;
        [SerializeField] private KaleidoscopeRenderPipeline mirrorPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private OpticalSourceChamber opticalSourceChamber;
        [SerializeField] private EntropyCompressionVolume entropyCompression;
        [SerializeField] private KaleidoscopeTubeChamberSettings tubeSettings;
        [SerializeField] private KaleidoscopeFpsMonitor fpsMonitor;
        [SerializeField] private AdaptiveQualityController adaptiveQualityController;
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;
        [SerializeField] private ViewerComfortController comfortController;
        [SerializeField] private KaleidoscopeTemporalStabilizer temporalStabilizer;
        [SerializeField] private KaleidoscopeScenarioOrchestrator scenarioOrchestrator;

        [Header("Display")]
        [SerializeField] private KaleidoscopeDebugPanelMode mode = KaleidoscopeDebugPanelMode.Hidden;
        [SerializeField] private bool allowGameViewDiagnostics;
        [SerializeField, Range(0.15f, 1f)] private float panelOpacity = 0.48f;
        [SerializeField] private bool operatorWindowVisible;
        [SerializeField] private float operatorMessageDuration = 2.6f;

        private GUIStyle panelStyle;
        private GUIStyle operatorPanelStyle;
        private GUIStyle titleStyle;
        private GUIStyle operatorTitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle operatorMessageStyle;
        private float fps;
        private string operatorMessage;
        private float operatorMessageUntil;
        private readonly List<string> operatorMessages = new List<string>();
        private float nextPanelMetricUpdateTime;

        public KaleidoscopeDebugPanelMode Mode => mode;
        public bool AllowGameViewDiagnostics => allowGameViewDiagnostics;
        public string LatestOperatorMessage => operatorMessage;
        public IReadOnlyList<string> OperatorMessages => operatorMessages;

        public void Configure(KaleidoscopePhysicsChamber physicsChamber, GemstoneSpawner gemstoneSpawner)
        {
            chamber = physicsChamber;
            spawner = gemstoneSpawner;
        }

        public void ConfigureDebugSystems(
            PhysicsSandboxCameraController sandboxCamera,
            PhysicsSandboxChamberDebugView debugView,
            PhysicsSandboxMetrics sandboxMetrics,
            GemstoneMaterialAssigner opticalAssigner = null,
            KaleidoscopeLightingRig opticalLightingRig = null,
            GemGeometryAssigner proceduralGeometryAssigner = null,
            GemSparkleController gemSparkles = null,
            FakeCausticBunnyProjector fakeCaustics = null,
            KaleidoscopeRenderPipeline kaleidoscopePipeline = null,
            KaleidoscopeMirrorController kaleidoscopeMirror = null,
            OpticalSourceChamber sourceChamber = null,
            EntropyCompressionVolume compressionVolume = null)
        {
            cameraController = sandboxCamera;
            chamberDebugView = debugView;
            metrics = sandboxMetrics;
            materialAssigner = opticalAssigner;
            lightingRig = opticalLightingRig;
            geometryAssigner = proceduralGeometryAssigner;
            sparkleController = gemSparkles;
            causticProjector = fakeCaustics;
            mirrorPipeline = kaleidoscopePipeline;
            mirrorController = kaleidoscopeMirror;
            opticalSourceChamber = sourceChamber;
            entropyCompression = compressionVolume;
        }

        public void ConfigureTubeSettings(KaleidoscopeTubeChamberSettings settings)
        {
            tubeSettings = settings;
        }

        public void ConfigurePerformanceSystems(KaleidoscopeFpsMonitor monitor, AdaptiveQualityController controller)
        {
            fpsMonitor = monitor;
            adaptiveQualityController = controller;
        }

        public void ConfigureViewerSystems(
            KaleidoscopeSourceModeController sourceController,
            ViewerComfortController viewerComfort,
            KaleidoscopeTemporalStabilizer stabilizer = null)
        {
            sourceModeController = sourceController;
            comfortController = viewerComfort;
            temporalStabilizer = stabilizer;
        }

        public void ConfigureScenarioOrchestrator(KaleidoscopeScenarioOrchestrator orchestrator)
        {
            scenarioOrchestrator = orchestrator;
        }

        public void TogglePanel()
        {
            mode = mode == KaleidoscopeDebugPanelMode.Hidden
                ? KaleidoscopeDebugPanelMode.Compact
                : KaleidoscopeDebugPanelMode.Hidden;
        }

        public void SetCompactMode()
        {
            mode = KaleidoscopeDebugPanelMode.Compact;
        }

        public void SetFullMode()
        {
            mode = KaleidoscopeDebugPanelMode.Full;
        }

        public void Hide()
        {
            mode = KaleidoscopeDebugPanelMode.Hidden;
            operatorWindowVisible = false;
            operatorMessage = null;
        }

        public void SetGameViewDiagnosticsEnabled(bool enabled)
        {
            allowGameViewDiagnostics = enabled;
            if (!allowGameViewDiagnostics)
            {
                mode = KaleidoscopeDebugPanelMode.Hidden;
                operatorWindowVisible = false;
            }
        }

        public void PostOperatorMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            operatorMessage = message;
            operatorMessageUntil = Time.unscaledTime + Mathf.Max(0.25f, operatorMessageDuration);
            operatorWindowVisible = allowGameViewDiagnostics;
            operatorMessages.Insert(0, $"{Time.realtimeSinceStartup:0000.0}s  {message}");
            if (operatorMessages.Count > 96)
            {
                operatorMessages.RemoveAt(operatorMessages.Count - 1);
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextPanelMetricUpdateTime)
            {
                return;
            }

            nextPanelMetricUpdateTime = Time.unscaledTime + 0.25f;
            float delta = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float instantFps = 1f / delta;
            fps = fps <= 0.01f ? instantFps : Mathf.Lerp(fps, instantFps, 0.08f);

            if (allowGameViewDiagnostics &&
                mode == KaleidoscopeDebugPanelMode.Hidden &&
                mirrorPipeline != null &&
                mirrorPipeline.HasBlockingDiagnostic)
            {
                mode = KaleidoscopeDebugPanelMode.Compact;
            }
        }

        private void OnGUI()
        {
            if (!allowGameViewDiagnostics)
            {
                return;
            }

            if (mode == KaleidoscopeDebugPanelMode.Hidden)
            {
                DrawOperatorWindow();
                return;
            }

            EnsureStyles();

            float width = mode == KaleidoscopeDebugPanelMode.Full ? 340f : 220f;
            float height = mode == KaleidoscopeDebugPanelMode.Full ? Mathf.Min(Screen.height - 28f, 760f) : 124f;
            Rect area = new Rect(Screen.width - width - 14f, 14f, width, height);

            GUILayout.BeginArea(area, panelStyle);
            GUILayout.Label("KALEIDOSCOPE", titleStyle);
            DrawLine("View", mirrorPipeline != null ? mirrorPipeline.ViewMode : "Raw");
            DrawLine("Source", sourceModeController != null ? sourceModeController.CurrentModeName : mirrorPipeline != null ? mirrorPipeline.SourceModeName : "n/a");
            DrawLine("Comfort", comfortController != null ? comfortController.ComfortPreset.ToString() : "n/a");
            DrawLine("Flicker", temporalStabilizer != null ? $"{temporalStabilizer.FlickerStatus} {comfortController?.FlickerScore:F2}" : comfortController != null ? $"{comfortController.FlickerScore:F2}" : "n/a");
            DrawLine("Segments", mirrorController != null ? mirrorController.SegmentCount.ToString() : "n/a");
            DrawLine("Color Depth", mirrorController != null ? mirrorController.ColorDepthModeName : "n/a");
            DrawLine("Scenario", scenarioOrchestrator != null && scenarioOrchestrator.OrchestratorEnabled ? scenarioOrchestrator.CurrentScenarioName : "Off");
            DrawLine("Quality", mirrorPipeline != null ? mirrorPipeline.QualityPresetName : "n/a");
            DrawLine("FPS", fpsMonitor != null ? $"{fpsMonitor.SmoothedFps:F0}" : $"{fps:F0}");
            DrawLine("Tube Speed", chamber != null ? $"{chamber.AxialRotationSpeed:F1}/{chamber.CurrentAxialRotationSpeed:F1}" : "n/a");
            if (mirrorPipeline != null && mirrorPipeline.HasBlockingDiagnostic)
            {
                DrawLine("Status", "Check pipeline");
            }

            if (mode == KaleidoscopeDebugPanelMode.Full)
            {
                KaleidoscopeSourceModeManager modeManager = sourceModeController != null ? sourceModeController.ModeManager : null;
                KaleidoscopeRebuildGuard rebuildGuard = KaleidoscopeRebuildGuard.Instance;
                if (modeManager != null && modeManager.ActiveSourceType == KaleidoscopeSourceType.TextureSource)
                {
                    DrawImageModePerformancePanel(modeManager, rebuildGuard);
                    GUILayout.EndArea();
                    DrawOperatorWindow();
                    return;
                }

                GUILayout.Space(6f);
                DrawLine("Current FPS", fpsMonitor != null ? $"{fpsMonitor.CurrentFps:F1}" : $"{fps:F0}");
                DrawLine("Smoothed FPS", fpsMonitor != null ? $"{fpsMonitor.SmoothedFps:F1}" : "n/a");
                DrawLine("Frame Time", fpsMonitor != null ? $"{fpsMonitor.AverageFrameMs:F1} ms" : "n/a");
                DrawLine("Perf State", adaptiveQualityController != null ? adaptiveQualityController.PerformanceState : "n/a");
                DrawLine("Background", comfortController != null ? comfortController.BackgroundMode.ToString() : "n/a");
                DrawLine("Avg Luma", comfortController != null ? $"{comfortController.AverageLuminance:F2}" : "n/a");
                DrawLine("Overexposed", comfortController != null ? $"{comfortController.OverexposedPixelRatio:P0}" : "n/a");
                DrawLine("Underexposed", comfortController != null ? $"{comfortController.UnderexposedPixelRatio:P0}" : "n/a");
                DrawLine("Contrast", comfortController != null ? $"{comfortController.ContrastScore:F2}" : "n/a");
                DrawLine("Exposure", temporalStabilizer != null ? temporalStabilizer.ExposureStatus : "n/a");
                DrawLine("Coverage Status", temporalStabilizer != null ? temporalStabilizer.SourceCoverageStatus : "n/a");
                DrawLine("Stability", temporalStabilizer != null ? temporalStabilizer.StabilityStatus : "n/a");
                DrawLine("Temporal Change", temporalStabilizer != null ? $"{temporalStabilizer.TemporalChangeScore:F2}" : "n/a");
                DrawLine("Safe Mode", temporalStabilizer != null && temporalStabilizer.SafeModeEnabled ? "On" : "Off");
                DrawLine("Warning", temporalStabilizer != null ? temporalStabilizer.LastActionableWarning : "n/a");
                DrawLine("Source Mode", sourceModeController != null ? sourceModeController.CurrentModeName : "n/a");
                DrawLine("Source Adaptive", sourceModeController != null && sourceModeController.AdaptiveQualityEnabled ? "On" : "Off");
                DrawLine("Target FPS", adaptiveQualityController != null ? $"{adaptiveQualityController.TargetFps:F0}" : "n/a");
                DrawLine("Min FPS", adaptiveQualityController != null ? $"{adaptiveQualityController.MinFpsHardLimit:F0}" : "n/a");
                DrawLine("Adaptive", adaptiveQualityController != null && adaptiveQualityController.AdaptiveQualityEnabled ? "On" : "Off");
                DrawLine("Auto-Balance", adaptiveQualityController != null && adaptiveQualityController.AutoBalanceEnabled ? "On" : "Off");
                DrawLine("Perf Budget", adaptiveQualityController != null ? $"{adaptiveQualityController.Budget01:P0}" : "n/a");
                DrawLine("Emergency", adaptiveQualityController != null && adaptiveQualityController.EmergencyMode ? "On" : "Off");
                DrawLine("Objects", spawner != null ? spawner.SpawnedObjects.Count.ToString() : "n/a");
                DrawLine("Physics Active", modeManager != null ? (modeManager.PhysicsActive ? "Yes" : "No") : "n/a");
                DrawLine("SourceCamera", modeManager != null ? (modeManager.SourceCameraActive ? "Active" : "Off") : "n/a");
                DrawLine("Physical Pipe", modeManager != null ? (modeManager.PhysicalPipelineActive ? "true" : "false") : "n/a");
                DrawLine("Mode Cost", modeManager != null ? modeManager.CurrentSourceModeCost : "n/a");
                DrawLine("Image FPS", modeManager != null ? $"{modeManager.ImageModeProfilerFps:F1}" : "n/a");
                DrawLine("Image Frame", modeManager != null ? $"{modeManager.ImageModeProfilerFrameMs:F2} ms" : "n/a");
                DrawLine("Image GC", modeManager != null ? $"{modeManager.ImageModeProfilerGcAllocBytes / 1024f:F1} KB" : "n/a");
                DrawLine("Image Min FPS", modeManager != null ? $"{modeManager.ImageModeProfilerMinFps:F1}" : "n/a");
                DrawLine("Image Max Frame", modeManager != null ? $"{modeManager.ImageModeProfilerMaxFrameMs:F2} ms" : "n/a");
                DrawLine("Image Spikes", modeManager != null ? modeManager.ImageModeProfilerSpikeCount.ToString() : "n/a");
                DrawLine("Image Reloads", modeManager != null ? modeManager.ImageReloadCount.ToString() : "n/a");
                DrawLine("Image Tex Creates", modeManager != null ? modeManager.ImageTextureCreateCount.ToString() : "n/a");
                DrawLine("Image Disk Reads", modeManager != null ? modeManager.ImageDiskReadCount.ToString() : "n/a");
                DrawLine("RT Recreates", rebuildGuard != null ? rebuildGuard.TotalRenderTextureRecreateCount.ToString() : "n/a");
                DrawLine("Material Instances", rebuildGuard != null ? rebuildGuard.TotalMaterialInstanceCount.ToString() : "n/a");
                DrawLine("Source Rebuilds", rebuildGuard != null ? rebuildGuard.TotalSourceModeRebuildCount.ToString() : "n/a");
                DrawLine("Last Expensive", rebuildGuard != null ? rebuildGuard.LastExpensiveEvent : "n/a");
                DrawLine("Expensive Age", rebuildGuard != null ? $"{rebuildGuard.TimeSinceLastExpensiveEvent:F1}s" : "n/a");
                DrawLine("Image Last Event", modeManager != null ? modeManager.LastImagePipelineEvent : "n/a");
                DrawLine("Event Age", modeManager != null ? $"{modeManager.TimeSinceLastImagePipelineEvent:F1}s" : "n/a");
                DrawLine("Image Spike", modeManager != null ? modeManager.LastImageSpikeReport : "n/a");
                DrawLine("Image Texture", modeManager != null ? $"{modeManager.ImageTextureResolution}px {modeManager.ImageMemoryMB:F1}MB" : "n/a");
                DrawLine("Visible", metrics != null ? metrics.VisibleGemCount.ToString() : "n/a");
                DrawLine("Sleeping", metrics != null ? metrics.SleepingBodyCount.ToString() : "n/a");
                DrawLine("Sparkles", sparkleController != null ? $"{sparkleController.ActiveSparkles}" : "n/a");
                DrawLine("Caustics", causticProjector != null && causticProjector.CausticsEnabled ? "On" : "Off");
                DrawLine("Lighting", lightingRig != null ? lightingRig.ActivePresetName : "n/a");
                DrawLine("Materials", materialAssigner != null ? materialAssigner.MaterialMode : "n/a");
                DrawLine("Geometry", geometryAssigner != null ? geometryAssigner.GeometryMode : "n/a");
                DrawLine("Render Quality", mirrorPipeline != null ? mirrorPipeline.QualityPresetName : "n/a");
                DrawLine("RenderTexture", mirrorPipeline != null ? $"{mirrorPipeline.RenderTextureWidth}x{mirrorPipeline.RenderTextureHeight}" : "n/a");
                DrawLine("Render Scale", mirrorPipeline != null ? $"{mirrorPipeline.RenderScale:F2}x" : "n/a");
                DrawLine("RT Format", mirrorPipeline != null ? mirrorPipeline.RenderTextureFormatName : "n/a");
                DrawLine("RT Filter", mirrorPipeline != null ? mirrorPipeline.RenderTextureFilterModeName : "n/a");
                DrawLine("Supersample", mirrorPipeline != null ? $"{mirrorPipeline.SupersamplingFactor:F2}x" : "n/a");
                DrawLine("AA Mode", mirrorPipeline != null ? $"MSAA x{mirrorPipeline.AntiAliasingSamples}" : "n/a");
                DrawLine("TAA Quality", mirrorPipeline != null ? $"{mirrorPipeline.TaaQuality:F2}" : "n/a");
                DrawLine("Pixelation", mirrorPipeline != null ? $"{mirrorPipeline.RenderPixelationFactor:F2}x" : "n/a");
                DrawLine("Color Depth", mirrorController != null ? mirrorController.ColorDepthModeName : "n/a");
                DrawLine("Color Steps", mirrorController != null ? $"{mirrorController.ColorSteps:F0}" : "n/a");
                DrawLine("Palette Strength", mirrorController != null ? $"{mirrorController.PaletteQuantizationStrength:F2}" : "n/a");
                DrawLine("Brightness", mirrorController != null ? $"{mirrorController.Brightness:F2}" : "n/a");
                DrawLine("Contrast", mirrorController != null ? $"{mirrorController.Contrast:F2}" : "n/a");
                DrawLine("Saturation", mirrorController != null ? $"{mirrorController.Saturation:F2}" : "n/a");
                DrawLine("Gamma", mirrorController != null ? $"{mirrorController.Gamma:F2}" : "n/a");
                DrawLine("Pixel Density", mirrorPipeline != null ? $"{mirrorPipeline.EffectivePixelDensity:F2}x" : "n/a");
                DrawLine("Update Limit", mirrorPipeline != null && mirrorPipeline.SourceUpdateRateLimit > 0 ? $"{mirrorPipeline.SourceUpdateRateLimit} fps" : "Off");
                DrawLine("RT VRAM", mirrorPipeline != null ? $"{mirrorPipeline.EstimatedRenderTextureMemoryMB:F1} MB" : "n/a");
                DrawLine("Prism Mode", mirrorController != null ? mirrorController.PrismModeName : "n/a");
                DrawLine("Scenario", scenarioOrchestrator != null && scenarioOrchestrator.OrchestratorEnabled ? scenarioOrchestrator.CurrentScenarioName : "Off");
                DrawLine("Scenario Next", scenarioOrchestrator != null ? $"{scenarioOrchestrator.NextTransitionSeconds:F1}s" : "n/a");
                DrawLine("Scenario Changes", scenarioOrchestrator != null ? scenarioOrchestrator.ActiveParameterChanges : "n/a");
                DrawLine("Mirror Angle", mirrorController != null ? $"{mirrorController.MirrorAngleDegrees:F1} deg" : "n/a");
                DrawLine("Computed Seg", mirrorController != null ? mirrorController.ComputedSegmentCount.ToString() : "n/a");
                DrawLine("Manual Seg", mirrorController != null ? mirrorController.ManualSegmentCount.ToString() : "n/a");
                DrawLine("Angle Mode", mirrorController != null && mirrorController.UseMirrorAngleMode ? "On" : "Off");
                DrawLine("Source Mask", mirrorPipeline != null ? mirrorPipeline.SourceCameraCullingMaskMode : "n/a");
                DrawLine("Source Ribs", mirrorPipeline != null && mirrorPipeline.RibsVisibleToSourceCamera ? "On" : "Off");
                DrawLine("Diffuser", opticalSourceChamber != null && opticalSourceChamber.DiffuserEnabled ? "On" : "Off");
                DrawLine("Optical Mask", mirrorController != null ? mirrorController.MaskModeName : "n/a");
                DrawLine("Seam Soft", mirrorController != null ? $"{mirrorController.SeamSoftness:F3}" : "n/a");
                DrawLine("Seam Blend", mirrorController != null ? $"{mirrorController.SeamBlendStrength:F2}" : "n/a");
                DrawLine("Center Mode", mirrorController != null ? mirrorController.CenterFillModeName : "n/a");
                DrawLine("Center Radius", mirrorController != null ? $"{mirrorController.CenterMaskRadius:F3}" : "n/a");
                DrawLine("Center Clean Radius", mirrorController != null ? $"{mirrorController.CenterCleanRadius:F3}" : "n/a");
                DrawLine("Center Clean Feather", mirrorController != null ? $"{mirrorController.CenterCleanFeather:F3}" : "n/a");
                DrawLine("Center Work Radius", mirrorController != null ? $"{mirrorController.CenterWorkRadius:F3}" : "n/a");
                DrawLine("Center Work Feather", mirrorController != null ? $"{mirrorController.CenterWorkFeather:F3}" : "n/a");
                DrawLine("Center Blend", mirrorController != null ? $"{mirrorController.CenterBlendStrength:F2}" : "n/a");
                DrawLine("Center Continue", mirrorController != null ? $"{mirrorController.CenterContinuationStrength:F2}" : "n/a");
                DrawLine("Center Mask Preview", mirrorController != null ? (mirrorController.CenterMaskPreview ? "On" : "Off") : "n/a");
                DrawLine("Center Reconstruction Quality", mirrorController != null ? $"{mirrorController.CenterReconstructionQuality:F2}" : "n/a");
                DrawLine("Center Sample Scale", mirrorController != null ? $"{mirrorController.CenterSampleScale:F2}" : "n/a");
                DrawLine("Center Affected By Quality", mirrorController != null ? (mirrorController.CenterAffectedByQuality ? "true" : "false") : "n/a");
                DrawLine("Center Exposure", mirrorController != null ? $"{mirrorController.CenterExposure:F2}" : "n/a");
                DrawLine("Optical Complexity", mirrorController != null ? $"{mirrorController.OpticalComplexity:F2}" : "n/a");
                DrawLine("Optical Density", mirrorController != null ? $"{mirrorController.OpticalDensity:F2}" : "n/a");
                DrawLine("Optical Recursion", mirrorController != null ? $"{mirrorController.OpticalRecursion:F2}" : "n/a");
                DrawLine("Composition Depth", mirrorController != null ? $"{mirrorController.CompositionDepth:F2}" : "n/a");
                DrawLine("Coverage", spawner != null ? $"{spawner.SourceCoverageEstimate:F2} / {spawner.SourceCoverageTarget:F2}" : "n/a");
                DrawLine("Filler Detail", spawner != null ? spawner.OpticalFillerParticles.ToString() : "n/a");
                DrawLine("Density Preset", spawner != null ? spawner.DensityPresetName : "n/a");
                DrawLine("Source Visible", spawner != null ? $"{spawner.LastVisibleEntropyCount}/{spawner.VisibleEntropyTarget}" : "n/a");
                DrawLine("Hero Physics", spawner != null ? spawner.HeroGemCount.ToString() : "n/a");
                DrawLine("Visible Large", spawner != null ? $"{spawner.LastVisibleLargeCount}/{spawner.VisibleLargeGemTarget}" : "n/a");
                DrawLine("Visible Medium", spawner != null ? $"{spawner.LastVisibleMediumCount}/{spawner.VisibleMediumShardTarget}" : "n/a");
                DrawLine("Visible Micro", spawner != null ? $"{spawner.LastVisibleMicroCount}/{spawner.VisibleMicroCrystalTarget}" : "n/a");
                DrawLine("Refill Adds", spawner != null ? spawner.LastVisibilityRefillCount.ToString() : "n/a");
                DrawLine("Rear Fill", spawner != null && spawner.RearWallFillEnabled ? spawner.RearWallFillCount.ToString() : "Off");
                DrawLine("Visual Chips", adaptiveQualityController != null ? $"{adaptiveQualityController.EffectiveMicroChips}/{adaptiveQualityController.RequestedMicroChips}" : spawner != null && spawner.VisualOnlyMicroChips ? spawner.VisualMicroChipCount.ToString() : "Off");
                DrawLine("Visual Crystals", spawner != null ? spawner.LightweightVisualCrystalCount.ToString() : "n/a");
                DrawLine("Crystals Source", spawner != null ? spawner.CrystalsInSourceTexture.ToString() : "n/a");
                DrawLine("Crystals Visible", spawner != null ? spawner.CrystalsVisibleToSourceCamera.ToString() : "n/a");
                DrawLine("After Mirror", spawner != null ? spawner.CrystalsRenderedAfterMirrorPass.ToString() : "n/a");
                DrawLine("Sparkle Budget", adaptiveQualityController != null ? $"{adaptiveQualityController.EffectiveSparkles}/{adaptiveQualityController.RequestedSparkles}" : "n/a");
                DrawLine("Caustic Budget", adaptiveQualityController != null ? $"{adaptiveQualityController.EffectiveCaustics}/{adaptiveQualityController.RequestedCaustics}" : "n/a");
                DrawLine("Axial Cap", adaptiveQualityController != null ? $"{adaptiveQualityController.AxialSpeedCap:F1}" : "n/a");
                DrawLine("Tube Requested", chamber != null ? $"{chamber.RequestedTubeAxialSpeedDeg:F1} deg/s" : "n/a");
                DrawLine("Tube Effective", chamber != null ? $"{chamber.EffectiveTubeAxialSpeedDeg:F1} deg/s" : "n/a");
                DrawLine("Tube Range", chamber != null ? $"{chamber.MinAxialRotationSpeed:F0}..{chamber.MaxAxialRotationSpeed:F0} deg/s" : "n/a");
                DrawLine("Pattern Requested", mirrorController != null ? $"{mirrorController.RequestedPatternRotationSpeedDeg:F1} deg/s" : "n/a");
                DrawLine("Pattern Effective", mirrorController != null ? $"{mirrorController.EffectivePatternRotationSpeedDeg:F1} deg/s" : "n/a");
                DrawLine("Adaptive Clamp", chamber != null ? $"{chamber.EffectiveAxialRotationSpeedCap:F1} deg/s" : "n/a");
                DrawLine("Chip Scale", spawner != null ? $"{spawner.VisualMicroChipScale:F3}" : "n/a");
                DrawLine("Chip Depth", spawner != null ? $"{spawner.ChipLayerDepth:F2}" : "n/a");
                DrawLine("Entropy Depth", entropyCompression != null ? $"{entropyCompression.EffectiveDepth:F2} m" : "n/a");
                DrawLine("Entropy Radius", entropyCompression != null ? $"{entropyCompression.EffectiveRadius:F2} m" : "n/a");
                DrawLine("Compression", entropyCompression != null ? $"{entropyCompression.CompressionStrength:F1}" : "n/a");
                DrawLine("Packing", entropyCompression != null ? $"{entropyCompression.ObjectPackingDensity:F2}" : "n/a");
                DrawLine("Mosaic Score", mirrorController != null ? $"{mirrorController.MosaicCohesionScore:F2}" : "n/a");
                DrawLine("Dominant Ratio", spawner != null ? $"{spawner.DominantGemRatio:P0}" : "n/a");
                DrawLine("Shard Participation", spawner != null ? $"{spawner.MediumShardCount}/{spawner.SpawnedObjects.Count}" : "n/a");
                DrawLine("Medium Shards", spawner != null ? spawner.MediumShardCount.ToString() : "n/a");
                DrawLine("Edge Recursion", mirrorController != null ? $"{mirrorController.EdgeRecursionBlend:F2}" : "n/a");
                DrawLine("Center Conv", mirrorController != null ? $"{mirrorController.CenterConvergenceStrength:F2}" : "n/a");
                DrawLine("Center Pattern", mirrorController != null ? $"{mirrorController.CenterPatternContinuation:F2}" : "n/a");
                DrawLine("Center Detail", mirrorController != null ? $"{mirrorController.CenterDetailBoost:F2}" : "n/a");
                DrawLine("Radial Continuity", mirrorController != null ? $"{mirrorController.RadialContinuation:F2}" : "n/a");
                DrawLine("Wobble", mirrorController != null && mirrorController.WobbleEnabled ? "On" : "Off");
                DrawLine("Breathing", mirrorController != null && mirrorController.BreathingEnabled ? "On" : "Off");
                DrawLine("Center Drift", mirrorController != null && mirrorController.CenterDriftEnabled ? "On" : "Off");
                DrawLine("Segments Var", mirrorController != null && mirrorController.SegmentVariationEnabled ? "On" : "Off");
                DrawLine("Asymmetry", mirrorController != null && mirrorController.AsymmetryEnabled ? "On" : "Off");
                DrawLine("Vignette", mirrorController != null && mirrorController.VignetteEnabled ? "On" : "Off");
                DrawLine("Drift Amount", mirrorController != null ? $"{mirrorController.DriftAmount:F3}" : "n/a");
                DrawLine("Motion Damping", mirrorController != null ? $"{mirrorController.GlobalMotionDamping:F2}" : "n/a");
                DrawLine("Optical Inertia", mirrorController != null ? $"{mirrorController.OpticalInertia:F2}" : "n/a");
                DrawLine("Temporal Smooth", mirrorController != null ? $"{mirrorController.TemporalSmoothing:F2}" : "n/a");
                DrawLine("Pattern Persist", mirrorController != null ? $"{mirrorController.PatternPersistence:F2}" : "n/a");
                DrawLine("Max Pattern Vel", mirrorController != null ? $"{mirrorController.MaxPatternAngularVelocity:F1}" : "n/a");
                DrawLine("Rot Mass", chamber != null ? $"{chamber.RotationalMass:F1}" : "n/a");
                DrawLine("Momentum", chamber != null ? $"{chamber.OpticalMomentum:F2}" : "n/a");
                DrawLine("Tube Visuals", chamberDebugView != null && chamberDebugView.ShowChamberVisuals ? "On" : "Off");
                DrawLine("Ribs", tubeSettings != null && tubeSettings.internalRibsEnabled ? $"{tubeSettings.internalRibCount}" : "Off");
                if (sourceModeController != null &&
                    sourceModeController.ModeManager != null &&
                    GUILayout.Button("Diagnose Image Mode Stutter"))
                {
                    sourceModeController.ModeManager.DiagnoseImageModeStutter();
                }
                GUILayout.Space(6f);
                GUILayout.Label(mirrorPipeline != null ? mirrorPipeline.DiagnosticStatus : "No pipeline diagnostics.", labelStyle);
            }

            GUILayout.EndArea();
            DrawOperatorWindow();
        }

        private void DrawImageModePerformancePanel(KaleidoscopeSourceModeManager modeManager, KaleidoscopeRebuildGuard rebuildGuard)
        {
            GUILayout.Space(6f);
            DrawLine("Image FPS", $"{modeManager.ImageModeProfilerFps:F1}");
            DrawLine("Image Frame", $"{modeManager.ImageModeProfilerFrameMs:F2} ms");
            DrawLine("Image GC", $"{modeManager.ImageModeProfilerGcAllocBytes / 1024f:F1} KB");
            DrawLine("Image Min FPS", $"{modeManager.ImageModeProfilerMinFps:F1}");
            DrawLine("Image Max Frame", $"{modeManager.ImageModeProfilerMaxFrameMs:F2} ms");
            DrawLine("Image Spikes", modeManager.ImageModeProfilerSpikeCount.ToString());
            DrawLine("Physical Pipe", modeManager.PhysicalPipelineActive ? "true" : "false");
            DrawLine("SourceCamera", modeManager.SourceCameraActive ? "Active" : "Off");
            DrawLine("Physics Bodies", modeManager.ActivePhysicsBodyCount.ToString());
            DrawLine("Image Reloads", modeManager.ImageReloadCount.ToString());
            DrawLine("Image Tex Creates", modeManager.ImageTextureCreateCount.ToString());
            DrawLine("Image Disk Reads", modeManager.ImageDiskReadCount.ToString());
            DrawLine("RT Recreates", rebuildGuard != null ? rebuildGuard.TotalRenderTextureRecreateCount.ToString() : "n/a");
            DrawLine("Material Instances", rebuildGuard != null ? rebuildGuard.TotalMaterialInstanceCount.ToString() : "n/a");
            DrawLine("Source Rebuilds", rebuildGuard != null ? rebuildGuard.TotalSourceModeRebuildCount.ToString() : "n/a");
            DrawLine("Source Reassign", mirrorPipeline != null ? mirrorPipeline.SourceTextureReassignCount.ToString() : "n/a");
            DrawLine("Image Texture", $"{modeManager.ImageTextureResolution}px {modeManager.ImageMemoryMB:F1}MB");
            DrawLine("Render Quality", mirrorPipeline != null ? mirrorPipeline.QualityPresetName : "n/a");
            DrawLine("RenderTexture", mirrorPipeline != null ? $"{mirrorPipeline.RenderTextureWidth}x{mirrorPipeline.RenderTextureHeight}" : "n/a");
            DrawLine("Render Scale", mirrorPipeline != null ? $"{mirrorPipeline.RenderScale:F2}x" : "n/a");
            DrawLine("Supersample", mirrorPipeline != null ? $"{mirrorPipeline.SupersamplingFactor:F2}x" : "n/a");
            DrawLine("AA Mode", mirrorPipeline != null ? $"MSAA x{mirrorPipeline.AntiAliasingSamples}" : "n/a");
            DrawLine("TAA Quality", mirrorPipeline != null ? $"{mirrorPipeline.TaaQuality:F2}" : "n/a");
            DrawLine("Pixelation", mirrorPipeline != null ? $"{mirrorPipeline.RenderPixelationFactor:F2}x" : "n/a");
            DrawLine("Last Image Event", modeManager.LastImagePipelineEvent);
            DrawLine("Event Age", $"{modeManager.TimeSinceLastImagePipelineEvent:F1}s");
            DrawLine("Last Expensive", rebuildGuard != null ? rebuildGuard.LastExpensiveEvent : "n/a");
            DrawLine("Expensive Age", rebuildGuard != null ? $"{rebuildGuard.TimeSinceLastExpensiveEvent:F1}s" : "n/a");
            DrawLine("Last Spike", modeManager.LastImageSpikeReport);

            if (GUILayout.Button("Diagnose Image Mode Stutter"))
            {
                modeManager.DiagnoseImageModeStutter();
            }
        }

        private void DrawLine(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(112f));
            GUILayout.Label(value, labelStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawOperatorWindow()
        {
            if (!operatorWindowVisible ||
                string.IsNullOrEmpty(operatorMessage) ||
                Time.unscaledTime > operatorMessageUntil)
            {
                return;
            }

            EnsureStyles();
            const float width = 360f;
            const float height = 68f;
            Rect area = new Rect(Screen.width - width - 14f, Screen.height - height - 14f, width, height);

            GUILayout.BeginArea(area, operatorPanelStyle);
            GUILayout.Label("OPERATOR STATUS", operatorTitleStyle);
            GUILayout.Label(operatorMessage, operatorMessageStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            Texture2D background = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            background.SetPixel(0, 0, new Color(0.03f, 0.045f, 0.055f, panelOpacity));
            background.Apply();

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                normal = { background = background }
            };

            Texture2D operatorBackground = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            operatorBackground.SetPixel(0, 0, new Color(0.03f, 0.05f, 0.065f, 0.72f));
            operatorBackground.Apply();

            operatorPanelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(12, 12, 8, 8),
                normal = { background = operatorBackground }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.68f, 0.92f, 1f, 0.92f) }
            };

            operatorTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.78f, 0.48f, 0.95f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.9f, 0.96f, 1f, 0.86f) }
            };

            operatorMessageStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.96f, 0.99f, 1f, 0.96f) }
            };
        }
    }
}
