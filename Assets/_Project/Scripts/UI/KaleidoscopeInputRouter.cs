using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Audio;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.Performance;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.Scenario;
using KaleidoscopeEngine.Source;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KaleidoscopeEngine.UI
{
    public enum KaleidoscopeControlZone
    {
        ViewModes,
        Geometry,
        Camera,
        Debug
    }

    public enum KaleidoscopeOperatorAction
    {
        CycleView,
        CycleViewBack,
        ResolutionUp,
        ResolutionDown,
        DensityUp,
        DensityDown,
        SixSectorMode,
        TwelveSectorMode,
        TwentyFourSectorMode,
        MirrorRotateLeft,
        MirrorRotateRight,
        CinematicPulse,
        CrystalDensity,
        BeautyShot,
        MirrorCountMultiply,
        MirrorCountDivide,
        ToggleBreathing,
        ToggleWobble,
        ToggleInversion,
        ViewerRotateLeft,
        ViewerRotateRight,
        ViewerZoomIn,
        ViewerZoomOut,
        SourceOrbitLeft,
        SourceOrbitRight,
        SourceFrameUp,
        SourceFrameDown,
        ToggleHelp,
        CompactDebug,
        FullDebug,
        HideDebug,
        ResetVisualTuning,
        CaptureScreenshot,
        QualityDown,
        QualityUp,
        QualityMinimal,
        QualityExtreme,
        ToggleAdaptiveQuality,
        ColorDepthDown,
        ColorDepthUp,
        PerformancePresetDown,
        PerformancePresetUp,
        ForceSafeMode,
        ToggleScenarioOrchestrator,
        NextScenario,
        StopAllRotation,
        RestoreDefaultRotation,
        ToggleAutoBalance,
        CycleSourceMode,
        ResetSourceMode,
        RandomizeSourceMode
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeInputRouter : MonoBehaviour
    {
        private struct Binding
        {
            public readonly KaleidoscopeControlZone Zone;
            public readonly KaleidoscopeOperatorAction Action;
            public readonly KeyCode Key;
            public readonly bool Shift;

            public Binding(KaleidoscopeControlZone zone, KaleidoscopeOperatorAction action, KeyCode key, bool shift = false)
            {
                Zone = zone;
                Action = action;
                Key = key;
                Shift = shift;
            }
        }

        [Header("References")]
        [SerializeField] private KaleidoscopePhysicsChamber chamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private PhysicsSandboxCameraController cameraController;
        [SerializeField] private KaleidoscopeRenderPipeline mirrorPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopeViewerCameraController viewerCameraController;
        [SerializeField] private KaleidoscopeDebugPanel debugPanel;
        [SerializeField] private KaleidoscopeHelpOverlay helpOverlay;
        [SerializeField] private OpticalSourceChamber opticalSourceChamber;
        [SerializeField] private AdaptiveQualityController adaptiveQualityController;
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;
        [SerializeField] private KaleidoscopeSourceLibrary sourceLibrary;
        [SerializeField] private KaleidoscopeGuideOverlay guideOverlay;
        [SerializeField] private KaleidoscopeOperatorModeController operatorModeController;
        [SerializeField] private KaleidoscopePerformanceAnalyzer performanceAnalyzer;
        [SerializeField] private KaleidoscopeInputBindingRegistry inputBindingRegistry;
        [SerializeField] private ViewerComfortController comfortController;
        [SerializeField] private KaleidoscopeTemporalStabilizer temporalStabilizer;
        [SerializeField] private KaleidoscopeScenarioOrchestrator scenarioOrchestrator;
        [SerializeField] private KaleidoscopeLauncherUI launcherUI;
        [SerializeField] private AudioReactiveDirector audioReactiveDirector;

        [Header("Response")]
        [SerializeField] private float patternSpeedStep = 8f;
        [SerializeField] private float tubeSpeedStep = 4f;
        [SerializeField] private float sourceFramingUnitsPerSecond = 0.35f;
        [SerializeField] private bool debugInputLogging;

        private readonly List<Binding> bindings = new List<Binding>();
        private float nextHeldFeedbackTime;

        public void Configure(
            KaleidoscopePhysicsChamber physicsChamber,
            GemstoneSpawner gemstoneSpawner,
            PhysicsSandboxCameraController sandboxCamera,
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopeMirrorController mirror,
            KaleidoscopeViewerCameraController viewerCamera,
            KaleidoscopeDebugPanel panel,
            KaleidoscopeHelpOverlay overlay,
            OpticalSourceChamber sourceChamber,
            AdaptiveQualityController adaptiveController = null,
            KaleidoscopeSourceModeController sourceController = null,
            ViewerComfortController viewerComfort = null,
            KaleidoscopeTemporalStabilizer stabilizer = null,
            KaleidoscopeSourceLibrary library = null,
            KaleidoscopeGuideOverlay guides = null,
            KaleidoscopeOperatorModeController modeController = null,
            KaleidoscopePerformanceAnalyzer analyzer = null,
            KaleidoscopeInputBindingRegistry bindingRegistry = null)
        {
            chamber = physicsChamber;
            spawner = gemstoneSpawner;
            cameraController = sandboxCamera;
            mirrorPipeline = pipeline;
            mirrorController = mirror;
            viewerCameraController = viewerCamera;
            debugPanel = panel;
            helpOverlay = overlay;
            opticalSourceChamber = sourceChamber;
            adaptiveQualityController = adaptiveController;
            sourceModeController = sourceController;
            sourceLibrary = library;
            guideOverlay = guides;
            operatorModeController = modeController;
            performanceAnalyzer = analyzer;
            inputBindingRegistry = bindingRegistry;
            inputBindingRegistry?.InitializeDefaults();
            comfortController = viewerComfort;
            temporalStabilizer = stabilizer;
            BuildBindings();
        }

        public void ConfigureScenarioOrchestrator(KaleidoscopeScenarioOrchestrator orchestrator)
        {
            scenarioOrchestrator = orchestrator;
        }

        public void ConfigureLauncher(KaleidoscopeLauncherUI launcher)
        {
            launcherUI = launcher;
        }

        public void ConfigureAudioReactiveDirector(AudioReactiveDirector director)
        {
            audioReactiveDirector = director;
        }

        public void ConfigureAdaptiveQuality(AdaptiveQualityController adaptiveController)
        {
            adaptiveQualityController = adaptiveController;
        }

        private void Awake()
        {
            BuildBindings();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(2))
            {
                if (launcherUI == null)
                {
                    launcherUI = FindObjectOfType<KaleidoscopeLauncherUI>();
                }

                launcherUI?.Toggle();
                Feedback(launcherUI != null && launcherUI.MenuVisible ? "Launcher Menu Open" : "Launcher Menu Closed");
                return;
            }

            if (HandleControlHotkeys())
            {
                return;
            }

            HandleViewModeZone();
            HandleGeometryZone();
            HandleCameraZone();
            HandleDebugZone();
        }

        private bool HandleControlHotkeys()
        {
            bool alt = AltHeld();
            bool ctrl = ControlHeld();
            bool shift = ShiftHeld();

            if (shift && Pressed(KeyCode.F1))
            {
                operatorModeController?.SetViewerMode();
                Feedback("Viewer Mode");
                return true;
            }

            if (shift && Pressed(KeyCode.F2))
            {
                operatorModeController?.SetOperatorMode();
                RequestOperatorConsole();
                Feedback("Operator Mode");
                return true;
            }

            if (PressedColorDepthPrevious())
            {
                mirrorController?.StepColorDepthMode(-1);
                Feedback($"Color Depth: {(mirrorController != null ? mirrorController.ColorDepthModeName : "Previous")}");
                return true;
            }

            if (PressedColorDepthNext())
            {
                mirrorController?.StepColorDepthMode(1);
                Feedback($"Color Depth: {(mirrorController != null ? mirrorController.ColorDepthModeName : "Next")}");
                return true;
            }

            // CTRL + digits: visual guide overlays only.
            // ALT + digits: source category switching only.
            // This avoids the old conflict where Alt+1..8 were consumed before guide controls.
            if (ctrl)
            {
                if (Pressed(KeyCode.M))
                {
                    audioReactiveDirector?.ToggleReactiveMode();
                    Feedback(audioReactiveDirector != null && audioReactiveDirector.ReactiveEnabled ? "Audio Reactive Enabled" : "Audio Reactive Disabled");
                    return true;
                }

                if (Pressed(KeyCode.B))
                {
                    audioReactiveDirector?.ToggleBeatDebugOverlay();
                    Feedback(audioReactiveDirector != null && audioReactiveDirector.BeatDebugOverlay ? "Beat Debug Overlay Enabled" : "Beat Debug Overlay Disabled");
                    return true;
                }

                if (Pressed(KeyCode.R))
                {
                    audioReactiveDirector?.Resync();
                    Feedback("Audio Reactive Resync");
                    return true;
                }

                if (Pressed(KeyCode.A))
                {
                    scenarioOrchestrator?.ToggleAutoMode();
                    Feedback(scenarioOrchestrator != null && scenarioOrchestrator.OrchestratorEnabled ? $"Auto Scenario: {scenarioOrchestrator.CurrentScenarioName}" : "Auto Scenario Disabled");
                    return true;
                }

                if (Pressed(KeyCode.F))
                {
                    mirrorController?.ApplyAutoVisualQuality();
                    Feedback("Auto Visual Quality: Premium Look");
                    return true;
                }

                if (Pressed(KeyCode.Alpha1) || Pressed(KeyCode.Keypad1))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.MirrorWedgeBoundaries, "Guide: Mirror Wedges");
                    return true;
                }

                if (Pressed(KeyCode.Alpha2) || Pressed(KeyCode.Keypad2))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.SourceCoverage, "Guide: Source Coverage");
                    return true;
                }

                if (Pressed(KeyCode.Alpha3) || Pressed(KeyCode.Keypad3))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.SourceToMirrorTransfer, "Guide: Transfer Region");
                    return true;
                }

                if (Pressed(KeyCode.Alpha4) || Pressed(KeyCode.Keypad4))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.OpticalConvergence, "Guide: Convergence");
                    return true;
                }

                if (Pressed(KeyCode.Alpha5) || Pressed(KeyCode.Keypad5))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.CenterComposition, "Guide: Center Composition");
                    return true;
                }

                if (Pressed(KeyCode.Alpha6) || Pressed(KeyCode.Keypad6))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.SafeViewingZones, "Guide: Safe Zones");
                    return true;
                }

                if (Pressed(KeyCode.Alpha7) || Pressed(KeyCode.Keypad7))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.SourceDensityHeatmap, "Guide: Density Heatmap");
                    return true;
                }

                if (Pressed(KeyCode.Alpha8) || Pressed(KeyCode.Keypad8))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.RenderTexturePreview, "Guide: RT Preview");
                    return true;
                }

                if (Pressed(KeyCode.Alpha9) || Pressed(KeyCode.Keypad9))
                {
                    ToggleGuide(KaleidoscopeGuideFlags.OpticalFlow, "Guide: Optical Flow");
                    return true;
                }

                if (Pressed(KeyCode.Alpha0) || Pressed(KeyCode.Keypad0))
                {
                    guideOverlay?.HideAllGuides();
                    Feedback("Guides Hidden");
                    return true;
                }

                return false;
            }

            if (alt)
            {
                if (Pressed(KeyCode.Alpha1))
                {
                    SelectSourceCategory(KaleidoscopeSourceCategory.TransparentGemstones);
                    return true;
                }

                if (Pressed(KeyCode.Alpha2))
                {
                    SelectSourceCategory(KaleidoscopeSourceCategory.ColoredGlass);
                    return true;
                }

                if (Pressed(KeyCode.Alpha3))
                {
                    SelectSourceCategory(KaleidoscopeSourceCategory.UserImages);
                    return true;
                }

                if (Pressed(KeyCode.Alpha4))
                {
                    SelectSourceCategory(KaleidoscopeSourceCategory.ProceduralColorBlobs);
                    return true;
                }

                if (Pressed(KeyCode.Alpha5))
                {
                    SelectSourceCategory(KaleidoscopeSourceCategory.PolygonalGeometry);
                    return true;
                }

                if (Pressed(KeyCode.Alpha6))
                {
                    SelectSourceCategory(KaleidoscopeSourceCategory.Liquids);
                    return true;
                }

                if (Pressed(KeyCode.Alpha7))
                {
                    sourceModeController?.SetMode(KaleidoscopeSourceModeKind.Hybrid);
                    Feedback("Source: Hybrid");
                    return true;
                }

                if (Pressed(KeyCode.Alpha8))
                {
                    SelectSourceCategory(KaleidoscopeSourceCategory.ExperimentalSources);
                    return true;
                }

                if (Pressed(KeyCode.LeftArrow))
                {
                    sourceLibrary?.PreviousPreset();
                    Feedback("Previous Source Preset");
                    return true;
                }

                if (Pressed(KeyCode.RightArrow))
                {
                    sourceLibrary?.NextPreset();
                    Feedback("Next Source Preset");
                    return true;
                }

                if (Pressed(KeyCode.Backspace))
                {
                    sourceLibrary?.ResetCurrentSource();
                    sourceModeController?.ResetCurrentMode();
                    Feedback("Source Reset");
                    return true;
                }

                if (Pressed(KeyCode.R))
                {
                    sourceLibrary?.RandomizeCurrentSource();
                    sourceModeController?.RandomizeCurrentMode();
                    Feedback("Source Randomized");
                    return true;
                }

                if (Pressed(KeyCode.O))
                {
                    OpenUserImageBrowser();
                    return true;
                }

                return true;
            }

            return false;
        }

        private void BuildBindings()
        {
            bindings.Clear();
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.CycleView, KeyCode.Insert);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.CycleViewBack, KeyCode.Delete);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.ResolutionUp, KeyCode.Home);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.ResolutionDown, KeyCode.End);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.QualityUp, KeyCode.PageUp);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.QualityDown, KeyCode.PageDown);

            // Russian layout quality hotkeys are handled manually by PressedRenderQualityDown/Up().
            // They are intentionally kept out of this passive binding table to avoid duplicate/false dispatch.

            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.SixSectorMode, KeyCode.Keypad1);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.TwelveSectorMode, KeyCode.Keypad2);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.TwentyFourSectorMode, KeyCode.Keypad3);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.MirrorRotateLeft, KeyCode.Keypad4);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.MirrorRotateRight, KeyCode.Keypad6);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.StopAllRotation, KeyCode.Keypad5);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.CinematicPulse, KeyCode.Keypad7);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.CrystalDensity, KeyCode.Keypad8);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.BeautyShot, KeyCode.Keypad9);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.RestoreDefaultRotation, KeyCode.KeypadEnter);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.CycleSourceMode, KeyCode.KeypadEnter, true);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleInversion, KeyCode.Keypad0);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.RandomizeSourceMode, KeyCode.KeypadPeriod);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.MirrorCountMultiply, KeyCode.KeypadPlus);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.MirrorCountDivide, KeyCode.KeypadMinus);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleBreathing, KeyCode.KeypadMultiply);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleWobble, KeyCode.KeypadDivide);

            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.ViewerRotateLeft, KeyCode.LeftArrow);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.ViewerRotateRight, KeyCode.RightArrow);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.ViewerZoomIn, KeyCode.UpArrow);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.ViewerZoomOut, KeyCode.DownArrow);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.SourceOrbitLeft, KeyCode.LeftArrow, true);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.SourceOrbitRight, KeyCode.RightArrow, true);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.SourceFrameUp, KeyCode.UpArrow, true);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.SourceFrameDown, KeyCode.DownArrow, true);

            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ToggleHelp, KeyCode.F1);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.CompactDebug, KeyCode.F2);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.FullDebug, KeyCode.F3);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.HideDebug, KeyCode.F4);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ResetVisualTuning, KeyCode.F5);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.CaptureScreenshot, KeyCode.F6);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.QualityDown, KeyCode.F7);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.QualityUp, KeyCode.F8);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.QualityMinimal, KeyCode.F7, true);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.QualityExtreme, KeyCode.F8, true);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ColorDepthDown, KeyCode.Comma);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ColorDepthUp, KeyCode.Period);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ToggleAdaptiveQuality, KeyCode.F9);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ToggleAutoBalance, KeyCode.F9, true);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ToggleScenarioOrchestrator, KeyCode.F10, true);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.NextScenario, KeyCode.F11, true);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.PerformancePresetDown, KeyCode.F10);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.PerformancePresetUp, KeyCode.F11);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ForceSafeMode, KeyCode.F12);
        }

        private void Add(KaleidoscopeControlZone zone, KaleidoscopeOperatorAction action, KeyCode key, bool shift = false)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                Binding existing = bindings[i];
                if (existing.Key == key && existing.Shift == shift)
                {
                    UnityEngine.Debug.LogWarning($"Duplicate kaleidoscope binding ignored: {key} for {action}.", this);
                    return;
                }
            }

            bindings.Add(new Binding(zone, action, key, shift));
        }

        private void HandleViewModeZone()
        {
            if (Pressed(KeyCode.Insert))
            {
                mirrorPipeline?.CycleViewMode(1);
                if (mirrorPipeline != null && mirrorPipeline.CurrentViewMode == KaleidoscopeViewMode.DebugOrbit)
                {
                    cameraController?.SetDebugOrbitView();
                }

                Feedback($"View: {(mirrorPipeline != null ? mirrorPipeline.ViewMode : "Cycle")}");
            }

            if (Pressed(KeyCode.Delete))
            {
                mirrorPipeline?.CycleViewMode(-1);
                if (mirrorPipeline != null && mirrorPipeline.CurrentViewMode == KaleidoscopeViewMode.DebugOrbit)
                {
                    cameraController?.SetDebugOrbitView();
                }

                Feedback($"View: {(mirrorPipeline != null ? mirrorPipeline.ViewMode : "Cycle Back")}");
            }

            if (Pressed(KeyCode.Home))
            {
                KaleidoscopeRuntimeConfig.Current?.AdjustResolutionPreset(1);
                Feedback($"Resolution: {(KaleidoscopeRuntimeConfig.Current != null ? KaleidoscopeRuntimeConfig.Current.ResolutionPreset.ToString() : "Up")}");
            }

            if (Pressed(KeyCode.End))
            {
                KaleidoscopeRuntimeConfig.Current?.AdjustResolutionPreset(-1);
                Feedback($"Resolution: {(KaleidoscopeRuntimeConfig.Current != null ? KaleidoscopeRuntimeConfig.Current.ResolutionPreset.ToString() : "Down")}");
            }

            if (Pressed(KeyCode.PageUp) || PressedRenderQualityUp())
            {
                mirrorPipeline?.AdjustQualityLevel(1);
                FeedbackRenderQuality("Render Quality +");
            }

            if (Pressed(KeyCode.PageDown) || PressedRenderQualityDown())
            {
                mirrorPipeline?.AdjustQualityLevel(-1);
                FeedbackRenderQuality("Render Quality -");
            }

            if (Pressed(KeyCode.C) && ShiftHeld())
            {
                mirrorController?.ToggleCenterMaskPreview();
                Feedback("Center Mask Preview");
            }
        }

        private void HandleGeometryZone()
        {
            if (Pressed(KeyCode.Keypad1))
            {
                mirrorController?.SetSegmentCountDirect(6);
                Feedback("Mirrors: 6");
            }

            if (Pressed(KeyCode.Keypad2))
            {
                mirrorController?.SetSegmentCountDirect(12);
                Feedback("Mirrors: 12");
            }

            if (Pressed(KeyCode.Keypad3))
            {
                mirrorController?.SetSegmentCountDirect(24);
                Feedback("Mirrors: 24");
            }

            if (Pressed(KeyCode.Keypad4))
            {
                mirrorController?.TriggerKeyboardSpinBurst(1);
                Feedback("Fast Spin +");
            }

            if (Pressed(KeyCode.Keypad6))
            {
                mirrorController?.TriggerKeyboardSpinBurst(-1);
                Feedback("Fast Spin -");
            }

            if (Pressed(KeyCode.Keypad5))
            {
                mirrorController?.CancelHighSpeedRotationToBaseline();
                Feedback("Rotation Baseline Restored");
            }

            if (Pressed(KeyCode.Keypad7))
            {
                Feedback("Cinematic Pulse reserved for Stage 03");
            }

            if (Pressed(KeyCode.Keypad8))
            {
                Feedback("Crystal Density reserved for Stage 04");
            }

            if (Pressed(KeyCode.Keypad9))
            {
                Feedback("Beauty Shot reserved for Stage 04");
            }

            if (Pressed(KeyCode.KeypadEnter))
            {
                bool shift = ShiftHeld();
                if (shift)
                {
                    sourceModeController?.CycleMode();
                    Feedback($"Source: {(sourceModeController != null ? sourceModeController.CurrentModeName : "Cycle")}");
                }
                else
                {
                    mirrorController?.RestoreDefaultPatternRotation();
                    chamber?.RestoreDefaultAxialRotation();
                    Feedback("Default Rotation Restored");
                }
            }

            if (Pressed(KeyCode.Keypad0))
            {
                Feedback("Inversion reserved for Stage 03");
            }

            if (Pressed(KeyCode.KeypadPeriod))
            {
                sourceModeController?.RandomizeCurrentMode();
                Feedback("Source Randomized");
            }

            if (Pressed(KeyCode.KeypadPlus))
            {
                mirrorController?.MultiplySegmentCount(2);
                Feedback($"Mirrors: {(mirrorController != null ? mirrorController.ComputedSegmentCount.ToString() : "x2")}");
            }

            if (Pressed(KeyCode.KeypadMinus))
            {
                mirrorController?.DivideSegmentCount(2);
                Feedback($"Mirrors: {(mirrorController != null ? mirrorController.ComputedSegmentCount.ToString() : "/2")}");
            }

            if (Pressed(KeyCode.KeypadMultiply))
            {
                mirrorController?.ToggleBreathing();
                Feedback(mirrorController != null && mirrorController.BreathingEnabled ? "Breathing Enabled" : "Breathing Disabled");
            }

            if (Pressed(KeyCode.KeypadDivide))
            {
                mirrorController?.ToggleWobble();
                Feedback(mirrorController != null && mirrorController.WobbleEnabled ? "Wobble Enabled" : "Wobble Disabled");
            }
        }

        private void HandleCameraZone()
        {
            bool shift = ShiftHeld();
            if (shift)
            {
                if (Held(KeyCode.LeftArrow))
                {
                    chamber?.AdjustAxialRotationSpeed(-tubeSpeedStep * Time.deltaTime * 3f);
                    HeldFeedback($"Tube Rotation: {(chamber != null ? chamber.AxialRotationSpeed.ToString("F1") : "n/a")} deg/s");
                }

                if (Held(KeyCode.RightArrow))
                {
                    chamber?.AdjustAxialRotationSpeed(tubeSpeedStep * Time.deltaTime * 3f);
                    HeldFeedback($"Tube Rotation: {(chamber != null ? chamber.AxialRotationSpeed.ToString("F1") : "n/a")} deg/s");
                }

                if (Held(KeyCode.UpArrow))
                {
                    mirrorPipeline?.AdjustSourceFraming(sourceFramingUnitsPerSecond * Time.deltaTime);
                    HeldFeedback("Source Framing +");
                }

                if (Held(KeyCode.DownArrow))
                {
                    mirrorPipeline?.AdjustSourceFraming(-sourceFramingUnitsPerSecond * Time.deltaTime);
                    HeldFeedback("Source Framing -");
                }

                return;
            }

            if (Pressed(KeyCode.Space))
            {
                bool usesTube = sourceModeController == null || sourceModeController.ModeManager == null || sourceModeController.ModeManager.SourceModeUsesTube;
                if (usesTube)
                {
                    chamber?.Shake(1f);
                }

                mirrorController?.ApplyShakeImpulse(usesTube);
                Feedback("Shake");
            }

            int spinDirection = 0;
            if (Held(KeyCode.LeftArrow))
            {
                spinDirection--;
            }

            if (Held(KeyCode.RightArrow))
            {
                spinDirection++;
            }

            if (spinDirection != 0)
            {
                mirrorController?.HoldKeyboardRotation(spinDirection, IsExperimentalSourceMode());
                HeldFeedback($"Spin: {(mirrorController != null ? mirrorController.RequestedPatternRotationSpeedDeg.ToString("+0;-0;0") : "n/a")} deg/s");
            }
            else
            {
                mirrorController?.ReleaseKeyboardRotation();
            }

            if (Held(KeyCode.UpArrow))
            {
                mirrorController?.AdjustZoom(1f);
                HeldFeedback($"Zoom: {(mirrorController != null ? mirrorController.RequestedPatternZoom.ToString("F2") : "n/a")}x");
            }

            if (Held(KeyCode.DownArrow))
            {
                mirrorController?.AdjustZoom(-1f);
                HeldFeedback($"Zoom: {(mirrorController != null ? mirrorController.RequestedPatternZoom.ToString("F2") : "n/a")}x");
            }
        }

        private void HandleDebugZone()
        {
            bool shift = ShiftHeld();

            if (Pressed(KeyCode.F1))
            {
                KaleidoscopeHelpOverlay.ToggleRuntimeOverlay(helpOverlay);
            }

            if (Pressed(KeyCode.F2))
            {
                operatorModeController?.SetOperatorMode();
                RequestOperatorConsole();
                Feedback("Operator Console");
            }

            if (Pressed(KeyCode.F3))
            {
                operatorModeController?.SetOperatorMode();
                RequestOperatorConsole();
                Feedback("Diagnostics Console");
            }

            if (Pressed(KeyCode.F4))
            {
                debugPanel?.Hide();
                helpOverlay?.Hide();
                guideOverlay?.HideAllGuides();
            }

            if (Pressed(KeyCode.F5))
            {
                mirrorController?.ResetVisualTuningDefaults();
                spawner?.ResetMosaicDefaults(true);
                viewerCameraController?.ResetViewerComposition();
                Feedback("Visual Tuning Reset");
            }

            if (Pressed(KeyCode.F6))
            {
                CaptureScreenshot();
            }

            if (Pressed(KeyCode.F7))
            {
                if (shift)
                {
                    mirrorPipeline?.SetMinimumQuality();
                    Feedback("Quality: Minimal");
                }
                else
                {
                    mirrorPipeline?.AdjustQualityLevel(-1);
                    FeedbackRenderQuality("Render Quality -");
                }
            }

            if (Pressed(KeyCode.F8))
            {
                if (shift)
                {
                    mirrorPipeline?.SetMaximumQuality();
                    FeedbackRenderQuality("Render Quality: Max");
                }
                else
                {
                    mirrorPipeline?.AdjustQualityLevel(1);
                    FeedbackRenderQuality("Render Quality +");
                }
            }

            if (Pressed(KeyCode.F9))
            {
                if (shift)
                {
                    adaptiveQualityController?.ToggleAutoBalance();
                    Feedback(adaptiveQualityController != null && adaptiveQualityController.AutoBalanceEnabled ? "Auto-Balance Enabled" : "Auto-Balance Disabled");
                }
                else
                {
                    adaptiveQualityController?.ToggleAdaptiveQuality();
                    Feedback(adaptiveQualityController != null && adaptiveQualityController.AdaptiveQualityEnabled ? "Adaptive Quality Enabled" : "Adaptive Quality Disabled");
                }
            }

            if (Pressed(KeyCode.F10))
            {
                if (shift)
                {
                    scenarioOrchestrator?.ToggleEnabled();
                    Feedback(scenarioOrchestrator != null && scenarioOrchestrator.OrchestratorEnabled ? $"Scenario Enabled: {scenarioOrchestrator.CurrentScenarioName}" : "Scenario Disabled");
                    return;
                }

                adaptiveQualityController?.PerformancePresetDown();
                Feedback("Performance Preset Down");
            }

            if (Pressed(KeyCode.F11))
            {
                if (shift)
                {
                    scenarioOrchestrator?.NextScenario();
                    Feedback($"Scenario: {(scenarioOrchestrator != null ? scenarioOrchestrator.CurrentScenarioName : "Next")}");
                    return;
                }

                adaptiveQualityController?.PerformancePresetUp();
                Feedback("Performance Preset Up");
            }

            if (Pressed(KeyCode.F12))
            {
                temporalStabilizer?.EnterSafeMode("Safe Mode");
                adaptiveQualityController?.ForceSafeMode();
                Feedback("Emergency Safe Mode");
            }
        }

        private bool Pressed(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        private bool PressedRenderQualityDown()
        {
            // Russian 'Х' normally appears as inputString on Russian layout.
            // '[' is the physical key fallback for the same area on many layouts.
            bool pressed = Input.GetKeyDown(KeyCode.X) ||
                           Input.GetKeyDown(KeyCode.LeftBracket) ||
                           InputStringContains('х', 'Х', 'x', 'X', '[');

            if (pressed)
            {
                LogInputDebug($"Render quality DOWN detected. inputString='{Input.inputString}'");
            }

            return pressed;
        }

        private bool PressedRenderQualityUp()
        {
            // Russian 'Ъ' normally maps to the physical ']' key area.
            bool pressed = Input.GetKeyDown(KeyCode.RightBracket) ||
                           InputStringContains('ъ', 'Ъ', ']', '}');

            if (pressed)
            {
                LogInputDebug($"Render quality UP detected. inputString='{Input.inputString}'");
            }

            return pressed;
        }

        private bool PressedColorDepthPrevious()
        {
            return Input.GetKeyDown(KeyCode.Comma) || InputStringContains('<');
        }

        private bool PressedColorDepthNext()
        {
            return Input.GetKeyDown(KeyCode.Period) || InputStringContains('>');
        }

        private static bool InputStringContains(params char[] candidates)
        {
            string input = Input.inputString;
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            for (int i = 0; i < input.Length; i++)
            {
                char value = input[i];
                for (int j = 0; j < candidates.Length; j++)
                {
                    if (value == candidates[j])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool Held(KeyCode key)
        {
            return Input.GetKey(key);
        }

        private bool ControlHeld()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private bool ShiftHeld()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        private bool AltHeld()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        private void SelectSourceCategory(KaleidoscopeSourceCategory category)
        {
            if (sourceLibrary != null)
            {
                sourceLibrary.SetCategory(category);
                Feedback($"Source Category: {category}");
                return;
            }

            if (sourceModeController == null)
            {
                return;
            }

            switch (category)
            {
                case KaleidoscopeSourceCategory.TransparentGemstones:
                    sourceModeController.SetMode(KaleidoscopeSourceModeKind.Gemstones);
                    break;
                case KaleidoscopeSourceCategory.PolygonalGeometry:
                    sourceModeController.SetMode(KaleidoscopeSourceModeKind.PolygonGeometry);
                    break;
                case KaleidoscopeSourceCategory.ColoredGlass:
                    sourceModeController.SetMode(KaleidoscopeSourceModeKind.ColoredGlassPhysical);
                    break;
                case KaleidoscopeSourceCategory.ProceduralColorBlobs:
                    sourceModeController.SetMode(KaleidoscopeSourceModeKind.ProceduralColorBlobs);
                    break;
                case KaleidoscopeSourceCategory.Liquids:
                    sourceModeController.SetMode(KaleidoscopeSourceModeKind.LiquidIllusion);
                    break;
                case KaleidoscopeSourceCategory.ExperimentalSources:
                    sourceModeController.SetMode(KaleidoscopeSourceModeKind.Experimental);
                    break;
                case KaleidoscopeSourceCategory.Backgrounds:
                case KaleidoscopeSourceCategory.UserImages:
                    sourceModeController.SetMode(KaleidoscopeSourceModeKind.ImageWallpaper);
                    break;
            }

            Feedback($"Source: {sourceModeController.CurrentModeName}");
        }

        private void ToggleGuide(KaleidoscopeGuideFlags guide, string message)
        {
            operatorModeController?.SetOperatorMode();
            guideOverlay?.ToggleGuide(guide);
            Feedback(message);
        }

        private bool IsExperimentalSourceMode()
        {
            return sourceModeController != null && sourceModeController.CurrentMode == KaleidoscopeSourceModeKind.Experimental;
        }

        private void OpenUserImageBrowser()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("Open Kaleidoscope Source Image", string.Empty, "png,jpg,jpeg,tga");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                sourceModeController?.RecordImageDiskRead(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                sourceModeController?.RecordImageTextureCreate("Imported user image Texture2D");
                if (!texture.LoadImage(bytes))
                {
                    Destroy(texture);
                    Feedback("Image import failed");
                    return;
                }

                texture.name = Path.GetFileNameWithoutExtension(path);
                if (sourceLibrary != null)
                {
                    sourceLibrary.LoadUserImage(texture, texture.name);
                    Feedback($"Image Source: {texture.name}");
                }
                else
                {
                    sourceModeController?.SetImageSourceTextures(new[] { texture });
                    sourceModeController?.SetMode(KaleidoscopeSourceModeKind.ImageWallpaper);
                    Feedback($"Image Source: {texture.name}");
                }
            }
            catch (Exception exception)
            {
                Feedback($"Image import failed: {exception.Message}");
            }
#else
            Feedback("Image browser is available in the Unity Editor");
#endif
        }

        private void RequestOperatorConsole()
        {
#if UNITY_EDITOR
            Type windowType = Type.GetType("KaleidoscopeEngine.EditorTools.KaleidoscopeOperatorConsoleWindow, Assembly-CSharp-Editor");
            if (windowType == null)
            {
                return;
            }

            EditorWindow window = EditorWindow.GetWindow(windowType, false, "Operator Console");
            window.Show();
#endif
        }

        private void Feedback(string message)
        {
            if (debugPanel != null)
            {
                debugPanel.PostOperatorMessage(message);
                return;
            }

            helpOverlay?.ShowFeedback(message);
        }

        private void FeedbackRenderQuality(string prefix)
        {
            Feedback(mirrorPipeline != null
                ? $"{prefix}: {mirrorPipeline.QualityClampStatus}; RT {mirrorPipeline.RenderTextureWidth}x{mirrorPipeline.RenderTextureHeight}; SSAA {mirrorPipeline.SupersamplingFactor:F2}x; AA {mirrorPipeline.AntiAliasingSamples}x"
                : prefix);
        }

        private void LogInputDebug(string message)
        {
            if (debugInputLogging)
            {
                debugPanel?.PostOperatorMessage(message);
            }
        }

        private void HeldFeedback(string message)
        {
            if (Time.unscaledTime < nextHeldFeedbackTime)
            {
                return;
            }

            nextHeldFeedbackTime = Time.unscaledTime + 0.32f;
            Feedback(message);
        }

        private void CaptureScreenshot()
        {
            string directory = Path.Combine(UnityEngine.Application.persistentDataPath, "KaleidoscopeCaptures");
            Directory.CreateDirectory(directory);
            string fileName = $"kaleidoscope_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(directory, fileName);
            ScreenCapture.CaptureScreenshot(path, 2);
            Feedback($"Screenshot: {fileName}");
        }
    }
}
