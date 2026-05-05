using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Audio;
using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.Performance;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.Scenario;
using KaleidoscopeEngine.Source;
using KaleidoscopeEngine.UI;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace KaleidoscopeEngine.EditorTools
{
    public sealed class KaleidoscopeOperatorConsoleWindow : EditorWindow
    {
        private static readonly string[] Tabs =
        {
            "Performance",
            "Source",
            "Geometry",
            "Rendering",
            "Scenario",
            "Audio",
            "Comfort",
            "Diagnostics",
            "Adaptive Quality",
            "Experimental"
        };

        private int selectedTab;
        private Vector2 scrollPosition;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle warningStyle;
        private GUIStyle criticalStyle;
        private GUIStyle smallStyle;

        [MenuItem("Window/Kaleidoscope/Operator Console")]
        public static void OpenWindow()
        {
            KaleidoscopeOperatorConsoleWindow window = GetWindow<KaleidoscopeOperatorConsoleWindow>("Operator Console");
            window.minSize = new Vector2(620f, 480f);
            window.Show();
            window.Focus();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EnsureStyles();

            RuntimeContext context = RuntimeContext.Find();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("KALEIDOSCOPE OPERATOR CONSOLE", titleStyle);
            EditorGUILayout.LabelField("Diagnostics, warnings, source state, and performance suggestions live here, not in the Game View.", smallStyle);
            EditorGUILayout.Space(8f);

            selectedTab = GUILayout.Toolbar(selectedTab, Tabs);
            EditorGUILayout.Space(8f);

            if (!context.HasRuntime)
            {
                EditorGUILayout.HelpBox("No active kaleidoscope runtime was found. Enter Play Mode in the Physics Sandbox scene to populate the console.", MessageType.Info);
                return;
            }

            DrawModeStrip(context);
            EditorGUILayout.Space(8f);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            switch (selectedTab)
            {
                case 0:
                    DrawPerformanceTab(context);
                    break;
                case 1:
                    DrawSourceTab(context);
                    break;
                case 2:
                    DrawGeometryTab(context);
                    break;
                case 3:
                    DrawRenderingTab(context);
                    break;
                case 4:
                    DrawScenarioTab(context);
                    break;
                case 5:
                    DrawAudioReactiveTab(context);
                    break;
                case 6:
                    DrawComfortTab(context);
                    break;
                case 7:
                    DrawDiagnosticsTab(context);
                    break;
                case 8:
                    DrawAdaptiveQualityTab(context);
                    break;
                default:
                    DrawExperimentalTab(context);
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawModeStrip(RuntimeContext context)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                DrawMetric("Mode", context.operatorMode != null ? context.operatorMode.CurrentMode.ToString() : "n/a");
                DrawMetric("Game View", "clean");
                int rebuildWarnings = context.rebuildGuard != null ? context.rebuildGuard.Warnings.Count : 0;
                DrawMetric("Warnings", context.analyzer != null ? (context.analyzer.Warnings.Count + rebuildWarnings).ToString() : rebuildWarnings.ToString());
            }
        }

        private void DrawPerformanceTab(RuntimeContext context)
        {
            DrawSection("Frame");
            DrawMetric("FPS", context.fpsMonitor != null ? $"{context.fpsMonitor.CurrentFps:F1}" : "n/a");
            DrawMetric("Smoothed FPS", context.fpsMonitor != null ? $"{context.fpsMonitor.SmoothedFps:F1}" : "n/a");
            DrawMetric("Frame Time", context.fpsMonitor != null ? $"{context.fpsMonitor.AverageFrameMs:F1} ms" : "n/a");
            DrawMetric("Frame Pacing Mode", context.framePacing != null ? context.framePacing.FramePacingMode.ToString() : "n/a");
            DrawMetric("VSync", context.framePacing != null ? context.framePacing.VSyncCount.ToString() : QualitySettings.vSyncCount.ToString());
            DrawMetric("TargetFrameRate", context.framePacing != null ? context.framePacing.TargetFrameRate.ToString() : Application.targetFrameRate.ToString());
            DrawMetric("Desired Preview FPS", context.framePacing != null ? (context.framePacing.DesiredPreviewFps <= 0 ? "Display/VSync" : context.framePacing.DesiredPreviewFps.ToString()) : "n/a");
            DrawMetric("Min Safe FPS", context.framePacing != null ? context.framePacing.MinSafeFps.ToString() : "24");
            DrawMetric("Primary Bottleneck", context.analyzer != null ? context.analyzer.BottleneckSummary : "n/a");

            DrawSection("Cost Scores");
            DrawScore("RenderTexture", context.analyzer != null ? context.analyzer.RenderTextureCost : 0f);
            DrawScore("Shader", context.analyzer != null ? context.analyzer.ShaderCost : 0f);
            DrawScore("Physics", context.analyzer != null ? context.analyzer.PhysicsCost : 0f);
            DrawScore("Particles", context.analyzer != null ? context.analyzer.ParticleCost : 0f);
            DrawScore("Sparkles", context.analyzer != null ? context.analyzer.SparkleCost : 0f);
            DrawScore("Update Frequency", context.analyzer != null ? context.analyzer.UpdateFrequencyCost : 0f);
            DrawScore("Source Density", context.analyzer != null ? context.analyzer.SourceDensityCost : 0f);
            DrawScore("Post Processing", context.analyzer != null ? context.analyzer.PostProcessingCost : 0f);
            DrawScore("Overdraw", context.analyzer != null ? context.analyzer.OverdrawCost : 0f);
            DrawScore("Stability", context.analyzer != null ? context.analyzer.StabilityCost : 0f);

            DrawWarnings(context);
        }

        private void DrawSourceTab(RuntimeContext context)
        {
            DrawSection("Source Library");
            DrawMetric("Current Source Type", context.sourceManager != null ? context.sourceManager.ActiveSourceType.ToString() : "n/a");
            DrawMetric("Category", context.sourceLibrary != null ? context.sourceLibrary.CategoryDisplayName(context.sourceLibrary.CurrentCategory) : "n/a");
            DrawMetric("Preset", context.sourceLibrary != null ? context.sourceLibrary.CurrentPresetName : "n/a");
            DrawMetric("Description", context.sourceLibrary != null ? context.sourceLibrary.CurrentDescription : "n/a");
            DrawMetric("Performance Cost", context.sourceLibrary != null ? $"{context.sourceLibrary.CurrentPerformanceCost:P0}" : "n/a");
            DrawMetric("Compatibility", context.sourceLibrary != null ? context.sourceLibrary.CurrentCompatibility : "n/a");
            DrawMetric("Runtime Mode", context.sourceMode != null ? context.sourceMode.CurrentModeName : "n/a");
            DrawMetric("Source Mode Uses Tube", context.sourceManager != null && context.sourceManager.SourceModeUsesTube ? "Yes" : "No");
            DrawMetric("UsesPhysicalCenter", context.sourceManager != null && context.sourceManager.UsesPhysicalCenterArtifacts ? "Yes" : "No");
            DrawMetric("UsesSourceCamera", context.sourceManager != null && context.sourceManager.UsesPhysicalSourceCamera ? "Yes" : "No");
            DrawMetric("Direct Texture Source", context.sourceManager != null && context.sourceManager.DirectTextureSource ? "Yes" : "No");
            DrawMetric("DirectTexturePipeline", context.sourceManager != null && context.sourceManager.DirectTexturePipeline ? "On" : "Off");
            DrawMetric("PhysicalPipeline", context.sourceManager != null && context.sourceManager.PhysicalPipeline ? "On" : "Off");
            DrawMetric("CenterArtifacts", context.sourceManager != null && context.sourceManager.CenterArtifactsActive ? "On" : "Off");
            DrawMetric("PhysicalArtifactRenderersActive", context.sourceManager != null ? context.sourceManager.PhysicalArtifactRenderersActive.ToString() : "n/a");
            DrawMetric("Artifact Kill Switch", context.sourceManager != null && context.sourceManager.DisablePhysicalCenterArtifactsForNonPhysicalSources ? "On" : "Off");
            DrawMetric("Physical Tube Active", context.sourceManager != null && context.sourceManager.PhysicalSourceActive ? "Yes" : "No");
            DrawMetric("Physics Paused/Sleeping", context.sourceManager != null ? $"{context.sourceManager.PhysicsPaused}/{context.sourceManager.PhysicalBodiesSleeping}" : "n/a");
            DrawMetric("Center Mask", context.mirror != null ? context.mirror.CenterMaskModeName : "n/a");
            DrawMetric("Center Clean", context.mirror != null && context.mirror.CenterCleanEnabled ? "On" : "Off");
            DrawMetric("Center Fill", context.mirror != null ? context.mirror.CenterFillModeName : "n/a");
            DrawMetric("Source Texture", context.sourceManager != null && context.sourceManager.CurrentSourceTexture != null ? context.sourceManager.CurrentSourceTexture.name : "n/a");
            DrawMetric("Source Resolution", context.pipeline != null ? $"{context.pipeline.RenderTextureWidth}x{context.pipeline.RenderTextureHeight}" : "n/a");
            DrawMetric("Switch Cooldown", context.sourceManager != null ? $"{context.sourceManager.SourceSwitchCooldownSeconds:F2}s" : "n/a");
            DrawMetric("Last Switch", context.sourceManager != null ? $"{context.sourceManager.LastSwitchTime:F1}s" : "n/a");
            DrawMetric("Source Coverage", context.spawner != null ? $"{context.spawner.SourceCoverageEstimate:F2}/{context.spawner.SourceCoverageTarget:F2}" : "n/a");

            DrawSection("Source Actions");
            DrawSourceButtons(context);

            DrawSection("Clean Texture Source Controls");
            DrawMetric("Arrow Left/Right", "Spin speed -/+");
            DrawMetric("Arrow Up/Down", "Zoom +/-");
            DrawMetric("Space", "Shake current source");
            DrawMetric("Numpad + / -", "Segments +/-2");
            DrawMetric("1 / 2 / 3", "Segments 6 / 12 / 24");

            DrawSection("Source Events");
            if (context.sourceLibrary != null)
            {
                DrawMessages(context.sourceLibrary.EventLog);
            }
        }

        private void DrawGeometryTab(RuntimeContext context)
        {
            DrawSection("Mirror Geometry");
            DrawMetric("Prism Mode", context.mirror != null ? context.mirror.PrismModeName : "n/a");
            DrawMetric("Segments", context.mirror != null ? context.mirror.SegmentCount.ToString() : "n/a");
            DrawMetric("Mirror Angle", context.mirror != null ? $"{context.mirror.MirrorAngleDegrees:F1} deg" : "n/a");
            DrawMetric("Requested Spin", context.mirror != null ? $"{context.mirror.RequestedPatternRotationSpeedDeg:+0;-0;0} deg/s" : "n/a");
            DrawMetric("Effective Spin", context.mirror != null ? $"{context.mirror.EffectivePatternRotationSpeedDeg:+0;-0;0} deg/s" : "n/a");
            DrawMetric("Spin Range", context.mirror != null ? $"{context.mirror.PatternSpinMinDeg:0}..{context.mirror.PatternSpinMaxDeg:0} deg/s" : "n/a");
            DrawMetric("Requested Zoom", context.mirror != null ? $"{context.mirror.RequestedPatternZoom:F2}x" : "n/a");
            DrawMetric("Effective Zoom", context.mirror != null ? $"{context.mirror.EffectivePatternZoom:F2}x" : "n/a");
            DrawMetric("Angular Step", context.mirror != null ? $"{context.mirror.AngularStepPerFrame:F2} deg/frame" : "n/a");
            DrawMetric("Spin Stability", context.mirror != null ? context.mirror.SpinStabilityState.ToString() : "n/a");
            DrawMetric("Asymmetry", context.mirror != null && context.mirror.AsymmetryEnabled ? "On" : "Off");
            DrawMetric("Seam Blend", context.mirror != null ? $"{context.mirror.SeamBlendStrength:F2}" : "n/a");
            DrawMetric("Seam AA", context.mirror != null ? context.mirror.SeamAAState : "n/a");
            DrawMetric("Center Convergence", context.mirror != null ? $"{context.mirror.CenterConvergenceStrength:F2}" : "n/a");
            DrawMetric("Radial Continuity", context.mirror != null ? $"{context.mirror.RadialContinuation:F2}" : "n/a");

            DrawSection("Object Field");
            DrawMetric("Active Physics Objects", context.spawner != null ? context.spawner.SpawnedObjects.Count.ToString() : "n/a");
            DrawMetric("Active Visual Chips", context.spawner != null ? context.spawner.EffectiveVisualMicroChipCount.ToString() : "n/a");
            DrawMetric("Hero Gems", context.spawner != null ? context.spawner.HeroGemCount.ToString() : "n/a");
            DrawMetric("Micro Crystals", context.spawner != null ? context.spawner.PhysicsMicroCrystalCount.ToString() : "n/a");

            DrawSection("Segment Actions");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("6")) context.mirror?.SetSegmentCountDirect(6);
                if (GUILayout.Button("8")) context.mirror?.SetSegmentCountDirect(8);
                if (GUILayout.Button("12")) context.mirror?.SetSegmentCountDirect(12);
                if (GUILayout.Button("16")) context.mirror?.SetSegmentCountDirect(16);
                if (GUILayout.Button("24")) context.mirror?.SetSegmentCountDirect(24);
                if (GUILayout.Button("32")) context.mirror?.SetSegmentCountDirect(32);
                if (GUILayout.Button("64")) context.mirror?.SetSegmentCountDirect(64);
                if (GUILayout.Button("Prev")) context.mirror?.AdjustSegmentCountByStep(-1);
                if (GUILayout.Button("Next")) context.mirror?.AdjustSegmentCountByStep(1);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Brake Spin")) context.mirror?.SetPatternRotationSpeed(0f);
                if (GUILayout.Button("Default Spin")) context.mirror?.RestoreDefaultPatternSpin();
                if (GUILayout.Button("Shake")) context.mirror?.ApplyShakeImpulse(context.sourceManager == null || context.sourceManager.SourceModeUsesTube);
            }
        }

        private void DrawRenderingTab(RuntimeContext context)
        {
            DrawSection("RenderTexture");
            DrawMetric("Resolution", context.pipeline != null ? $"{context.pipeline.RenderTextureWidth}x{context.pipeline.RenderTextureHeight}" : "n/a");
            DrawMetric("VRAM Estimate", context.pipeline != null ? $"{context.pipeline.EstimatedRenderTextureMemoryMB:F1} MB" : "n/a");
            DrawMetric("Format", context.pipeline != null ? context.pipeline.RenderTextureFormatName : "n/a");
            DrawMetric("Filter", context.pipeline != null ? context.pipeline.RenderTextureFilterModeName : "n/a");
            DrawMetric("Supersampling", context.pipeline != null ? $"{context.pipeline.SupersamplingFactor:F2}x" : "n/a");
            DrawMetric("Update Limit", context.pipeline != null && context.pipeline.SourceUpdateRateLimit > 0 ? $"{context.pipeline.SourceUpdateRateLimit} fps" : "Off");
            DrawMetric("Effective Update Limit", context.pipeline != null && context.pipeline.EffectiveSourceUpdateRateLimit > 0 ? $"{context.pipeline.EffectiveSourceUpdateRateLimit} fps" : "Off");
            DrawMetric("Quality Preset", context.pipeline != null ? context.pipeline.QualityPresetName : "n/a");
            DrawMetric("Color Depth", context.mirror != null ? context.mirror.ColorDepthModeName : "n/a");
            DrawMetric("Color Steps", context.mirror != null ? $"{context.mirror.ColorSteps:F0}" : "n/a");
            DrawMetric("Palette Strength", context.mirror != null ? $"{context.mirror.PaletteQuantizationStrength:F2}" : "n/a");
            DrawMetric("Brightness", context.mirror != null ? $"{context.mirror.Brightness:F2}" : "n/a");
            DrawMetric("Contrast", context.mirror != null ? $"{context.mirror.Contrast:F2}" : "n/a");
            DrawMetric("Saturation", context.mirror != null ? $"{context.mirror.Saturation:F2}" : "n/a");
            DrawMetric("Gamma", context.mirror != null ? $"{context.mirror.Gamma:F2}" : "n/a");
            DrawMetric("Levels", context.mirror != null ? $"C {context.mirror.ContrastLevel}, S {context.mirror.SaturationLevel}, M {context.mirror.MotionLevel}" : "n/a");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Auto Premium Quality")) context.mirror?.ApplyAutoVisualQuality();
            }

            DrawSection("Optics");
            DrawMetric("View Mode", context.pipeline != null ? context.pipeline.ViewMode : "n/a");
            DrawMetric("Source Mode", context.pipeline != null ? context.pipeline.SourceModeName : "n/a");
            DrawMetric("Optical Mask", context.mirror != null ? context.mirror.MaskModeName : "n/a");
            DrawMetric("Optical Density", context.mirror != null ? $"{context.mirror.OpticalDensity:F2}" : "n/a");
            DrawMetric("Beauty Mode", context.beauty != null && context.beauty.BeautyModeEnabled ? context.beauty.ActivePreset.ToString() : "Off");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Beauty On")) context.beauty?.SetBeautyMode(true);
                if (GUILayout.Button("Beauty Off")) context.beauty?.SetBeautyMode(false);
                if (GUILayout.Button("Warm Glass")) context.beauty?.ApplyPreset(KaleidoscopeBeautyPreset.WarmGlass);
                if (GUILayout.Button("Deep Jewel")) context.beauty?.ApplyPreset(KaleidoscopeBeautyPreset.DeepJewel);
                if (GUILayout.Button("Soft Opal")) context.beauty?.ApplyPreset(KaleidoscopeBeautyPreset.SoftOpal);
            }
        }

        private void DrawScenarioTab(RuntimeContext context)
        {
            DrawSection("Effect Scenario Orchestrator");
            DrawMetric("Enabled", context.scenario != null && context.scenario.OrchestratorEnabled ? "On" : "Off");
            DrawMetric("Current Scenario", context.scenario != null ? context.scenario.CurrentScenarioName : "n/a");
            DrawMetric("Next Transition", context.scenario != null ? $"{context.scenario.NextTransitionSeconds:F1}s" : "n/a");
            DrawMetric("Active Parameter Changes", context.scenario != null ? context.scenario.ActiveParameterChanges : "n/a");
            DrawMetric("Color Depth", context.mirror != null ? context.mirror.ColorDepthModeName : "n/a");
            DrawMetric("Image Motion", context.imageMode != null ? $"scroll {context.imageMode.ImageScrollSpeed:F3}, zoom {context.imageMode.ImageZoomSpeed:F3}, rot {context.imageMode.ImageRotationSpeed:F3}" : "n/a");
            DrawMetric("Image Change Interval", context.imageMode != null ? $"{context.imageMode.ImageChangeInterval:F1}s" : "n/a");
            DrawMetric("Image Transition", context.imageMode != null ? $"{context.imageMode.ImageTransitionModeName} {context.imageMode.ImageTransitionProgress:P0}" : "n/a");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Toggle Scenario")) context.scenario?.ToggleEnabled();
                if (GUILayout.Button("Next Scenario")) context.scenario?.NextScenario();
            }
        }

        private void DrawAudioReactiveTab(RuntimeContext context)
        {
            DrawSection("Audio Reactive Visual Director");
            DrawMetric("Enabled", context.audioDirector != null && context.audioDirector.ReactiveEnabled ? "On" : "Off");
            DrawMetric("Debug Overlay", context.audioDirector != null && context.audioDirector.BeatDebugOverlay ? "On" : "Off");
            DrawMetric("Intensity", context.audioDirector != null ? context.audioDirector.IntensityLevel.ToString() : "n/a");
            DrawMetric("Musical State", context.audioDirector != null ? context.audioDirector.CurrentMusicalState.ToString() : "n/a");
            DrawMetric("BPM Estimate", context.audioDirector != null && context.audioDirector.BpmEstimate > 0.1f ? $"{context.audioDirector.BpmEstimate:F0}" : "n/a");
            DrawMetric("Bass Energy", context.audioDirector != null ? $"{context.audioDirector.BassEnergy:F3}" : "n/a");
            DrawMetric("Mid Energy", context.audioDirector != null ? $"{context.audioDirector.MidEnergy:F3}" : "n/a");
            DrawMetric("High Energy", context.audioDirector != null ? $"{context.audioDirector.HighEnergy:F3}" : "n/a");
            DrawMetric("Overall Energy", context.audioDirector != null ? $"{context.audioDirector.OverallEnergy:F3}" : "n/a");
            DrawMetric("Energy Delta", context.audioDirector != null ? $"{context.audioDirector.EnergyDelta:+0.000;-0.000;0.000}" : "n/a");
            DrawMetric("Beat Confidence", context.audioDirector != null ? $"{context.audioDirector.BeatConfidence:F2}" : "n/a");
            DrawMetric("Event Queue", context.audioDirector != null ? context.audioDirector.ActiveVisualEventQueue : "n/a");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Toggle Reactive")) context.audioDirector?.ToggleReactiveMode();
                if (GUILayout.Button("Beat Overlay")) context.audioDirector?.ToggleBeatDebugOverlay();
                if (GUILayout.Button("Resync")) context.audioDirector?.Resync();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ultra Low")) context.audioDirector?.SetIntensityLevel(EffectLevel.UltraLow);
                if (GUILayout.Button("Low")) context.audioDirector?.SetIntensityLevel(EffectLevel.Low);
                if (GUILayout.Button("Normal")) context.audioDirector?.SetIntensityLevel(EffectLevel.Normal);
                if (GUILayout.Button("High")) context.audioDirector?.SetIntensityLevel(EffectLevel.High);
                if (GUILayout.Button("Ultra High")) context.audioDirector?.SetIntensityLevel(EffectLevel.UltraHigh);
            }
        }

        private void DrawComfortTab(RuntimeContext context)
        {
            DrawSection("Viewer Comfort");
            DrawMetric("Comfort Preset", context.comfort != null ? context.comfort.ComfortPreset.ToString() : "n/a");
            DrawMetric("Average Luminance", context.comfort != null ? $"{context.comfort.AverageLuminance:F2}" : "n/a");
            DrawMetric("Overexposed", context.comfort != null ? $"{context.comfort.OverexposedPixelRatio:P0}" : "n/a");
            DrawMetric("Underexposed", context.comfort != null ? $"{context.comfort.UnderexposedPixelRatio:P0}" : "n/a");
            DrawMetric("Contrast", context.comfort != null ? $"{context.comfort.ContrastScore:F2}" : "n/a");
            DrawMetric("Flicker Score", context.comfort != null ? $"{context.comfort.FlickerScore:F2}" : "n/a");
            DrawMetric("Safe Mode", context.stabilizer != null && context.stabilizer.SafeModeEnabled ? "On" : "Off");
            DrawMetric("Stability", context.stabilizer != null ? context.stabilizer.StabilityStatus : "n/a");
        }

        private void DrawDiagnosticsTab(RuntimeContext context)
        {
            DrawSection("Pipeline");
            DrawMetric("Pipeline Status", context.pipeline != null ? context.pipeline.DiagnosticStatus : "n/a");
            DrawMetric("Blocking Diagnostic", context.pipeline != null && context.pipeline.HasBlockingDiagnostic ? "Yes" : "No");
            DrawMetric("Analyzer", context.analyzer != null ? context.analyzer.BottleneckSummary : "n/a");

            DrawWarnings(context);

            DrawSection("Rebuild Guard");
            DrawMetric("Instantiates", context.rebuildGuard != null ? context.rebuildGuard.GameObjectInstantiateCount.ToString() : "n/a");
            DrawMetric("Destroys", context.rebuildGuard != null ? context.rebuildGuard.GameObjectDestroyCount.ToString() : "n/a");
            DrawMetric("RT Recreates", context.rebuildGuard != null ? context.rebuildGuard.RenderTextureRecreateCount.ToString() : "n/a");
            DrawMetric("Full Respawns", context.rebuildGuard != null ? context.rebuildGuard.FullRespawnCount.ToString() : "n/a");
            DrawMetric("Material Instances", context.rebuildGuard != null ? context.rebuildGuard.MaterialInstanceCount.ToString() : "n/a");
            DrawMetric("Source Rebuilds", context.rebuildGuard != null ? context.rebuildGuard.SourceModeRebuildCount.ToString() : "n/a");
            if (context.rebuildGuard != null)
            {
                DrawMessages(context.rebuildGuard.Warnings);
            }

            DrawSection("Input Conflicts");
            if (context.inputRegistry != null)
            {
                DrawMessages(context.inputRegistry.ConflictWarnings);
            }

            if (context.sourceManager != null && !string.IsNullOrEmpty(context.sourceManager.PipelineWarning))
            {
                EditorGUILayout.HelpBox(context.sourceManager.PipelineWarning, context.sourceManager.PhysicalArtifactRenderersActive > 0 ? MessageType.Error : MessageType.Warning);
            }

            DrawSection("Physical Artifact Errors");
            if (context.sourceManager != null)
            {
                DrawMessages(context.sourceManager.PhysicalArtifactErrors);
            }

            DrawSection("Layer Audit");
            if (context.sourceManager != null)
            {
                DrawMessages(context.sourceManager.LayerAuditLog);
            }

            if (context.mirror != null && !string.IsNullOrEmpty(context.mirror.SpinStabilityWarning))
            {
                EditorGUILayout.HelpBox(context.mirror.SpinStabilityWarning, MessageType.Warning);
            }

            DrawSection("Operator Messages");
            if (context.debugPanel != null)
            {
                DrawMessages(context.debugPanel.OperatorMessages);
            }
        }

        private void DrawAdaptiveQualityTab(RuntimeContext context)
        {
            DrawSection("Adaptive Quality");
            DrawMetric("Adaptive", context.adaptive != null && context.adaptive.AdaptiveQualityEnabled ? "On" : "Off");
            DrawMetric("Auto-Balance", context.adaptive != null && context.adaptive.AutoBalanceEnabled ? "On" : "Off");
            DrawMetric("Emergency", context.adaptive != null && context.adaptive.EmergencyMode ? "On" : "Off");
            DrawMetric("Can Throttle Updates", context.adaptive != null && context.adaptive.AdaptiveQualityCanThrottleUpdates ? "Yes" : "No");
            DrawMetric("Budget", context.adaptive != null ? $"{context.adaptive.Budget01:P0}" : "n/a");
            DrawMetric("Target FPS", context.adaptive != null ? $"{context.adaptive.TargetFps:F0}" : "n/a");
            DrawMetric("Min FPS", context.adaptive != null ? $"{context.adaptive.MinFpsHardLimit:F0}" : "n/a");
            DrawMetric("Microchips", context.adaptive != null ? $"{context.adaptive.EffectiveMicroChips}/{context.adaptive.RequestedMicroChips}" : "n/a");
            DrawMetric("Sparkles", context.adaptive != null ? $"{context.adaptive.EffectiveSparkles}/{context.adaptive.RequestedSparkles}" : "n/a");
            DrawMetric("Caustics", context.adaptive != null ? $"{context.adaptive.EffectiveCaustics}/{context.adaptive.RequestedCaustics}" : "n/a");
            DrawMetric("Axial Cap", context.adaptive != null ? $"{context.adaptive.AxialSpeedCap:F1} deg/s" : "n/a");
        }

        private void DrawExperimentalTab(RuntimeContext context)
        {
            DrawSection("Experimental");
            DrawMetric("Current Source", context.sourceLibrary != null ? context.sourceLibrary.CurrentPresetName : "n/a");
            DrawMetric("Guides", context.guides != null ? context.guides.ActiveGuides.ToString() : "n/a");
            DrawMetric("Temporal Change", context.stabilizer != null ? $"{context.stabilizer.TemporalChangeScore:F2}" : "n/a");
            DrawMetric("Last Warning", context.stabilizer != null ? context.stabilizer.LastActionableWarning : "n/a");
            DrawMetric("Analyzer Primary", context.analyzer != null ? context.analyzer.PrimaryBottleneck.ToString() : "n/a");
        }

        private void DrawSourceButtons(RuntimeContext context)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Gemstones")) context.sourceLibrary?.SetCategory(KaleidoscopeSourceCategory.TransparentGemstones);
                if (GUILayout.Button("Colored Glass")) context.sourceLibrary?.SetCategory(KaleidoscopeSourceCategory.ColoredGlass);
                if (GUILayout.Button("Image")) context.sourceLibrary?.SetCategory(KaleidoscopeSourceCategory.UserImages);
                if (GUILayout.Button("Blobs")) context.sourceLibrary?.SetCategory(KaleidoscopeSourceCategory.ProceduralColorBlobs);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Polygons")) context.sourceLibrary?.SetCategory(KaleidoscopeSourceCategory.PolygonalGeometry);
                if (GUILayout.Button("Liquids")) context.sourceLibrary?.SetCategory(KaleidoscopeSourceCategory.Liquids);
                if (GUILayout.Button("Hybrid")) context.sourceMode?.SetMode(KaleidoscopeSourceModeKind.Hybrid);
                if (GUILayout.Button("Experimental")) context.sourceLibrary?.SetCategory(KaleidoscopeSourceCategory.ExperimentalSources);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Image")) LoadImage(context.sourceLibrary);
                if (GUILayout.Button("Randomize")) context.sourceLibrary?.RandomizeCurrentSource();
                if (GUILayout.Button("Reset Source")) context.sourceLibrary?.ResetCurrentSource();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pause Physical Source")) context.sourceManager?.PausePhysicalSource();
                if (GUILayout.Button("Resume Physical Source")) context.sourceManager?.ResumePhysicalSource();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Kill Physical Artifacts Now")) context.sourceManager?.KillPhysicalArtifactsNow();
            }
        }

        private void LoadImage(KaleidoscopeSourceLibrary library)
        {
            if (library == null)
            {
                return;
            }

            string path = EditorUtility.OpenFilePanel("Open Kaleidoscope Source Image", string.Empty, "png,jpg,jpeg,tga");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return;
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            library.LoadUserImage(texture, texture.name);
        }

        private void DrawWarnings(RuntimeContext context)
        {
            DrawSection("Warnings");
            if (context.analyzer == null || context.analyzer.Warnings.Count == 0)
            {
                EditorGUILayout.HelpBox("No bottleneck warnings currently detected.", MessageType.None);
                return;
            }

            for (int i = 0; i < context.analyzer.Warnings.Count; i++)
            {
                KaleidoscopePerformanceWarning warning = context.analyzer.Warnings[i];
                GUIStyle style = warning.severity == KaleidoscopeDiagnosticSeverity.Critical ? criticalStyle : warningStyle;
                using (new EditorGUILayout.VerticalScope(style))
                {
                    EditorGUILayout.LabelField($"{warning.severity}: {warning.title}", sectionStyle);
                    EditorGUILayout.LabelField(warning.detail, smallStyle);
                    EditorGUILayout.LabelField($"Suggestion: {warning.suggestion}", smallStyle);
                    EditorGUILayout.LabelField(warning.adaptiveQualityMayApply ? "Adaptive quality may apply this class of reduction." : "Suggestion only; no automatic change will be applied.", smallStyle);
                }
            }
        }

        private void DrawMessages(System.Collections.Generic.IReadOnlyList<string> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                EditorGUILayout.LabelField("No messages yet.", smallStyle);
                return;
            }

            int count = Mathf.Min(18, messages.Count);
            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.LabelField(messages[i], smallStyle);
            }
        }

        private void DrawSection(string label)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(label, sectionStyle);
        }

        private void DrawMetric(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(170f));
                EditorGUILayout.LabelField(value ?? "n/a", smallStyle);
            }
        }

        private void DrawScore(string label, float score)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            Rect labelRect = new Rect(rect.x, rect.y, 170f, rect.height);
            Rect barRect = new Rect(rect.x + 174f, rect.y + 4f, rect.width - 174f, rect.height - 8f);
            EditorGUI.LabelField(labelRect, label);
            EditorGUI.DrawRect(barRect, new Color(0.09f, 0.11f, 0.12f, 1f));
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(score), barRect.height), ScoreColor(score));
            EditorGUI.LabelField(barRect, $"{score:P0}", smallStyle);
        }

        private Color ScoreColor(float score)
        {
            if (score >= 0.85f)
            {
                return new Color(1f, 0.25f, 0.18f, 0.8f);
            }

            if (score >= 0.65f)
            {
                return new Color(1f, 0.72f, 0.22f, 0.8f);
            }

            return new Color(0.3f, 0.86f, 0.95f, 0.72f);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fixedHeight = 24f
            };

            sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.72f, 0.9f, 1f) }
            };

            warningStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 7, 7),
                margin = new RectOffset(0, 0, 0, 7)
            };

            criticalStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 7, 7),
                margin = new RectOffset(0, 0, 0, 7),
                normal = { textColor = new Color(1f, 0.72f, 0.66f) }
            };

            smallStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.68f, 0.76f, 0.8f) }
            };
        }

        private struct RuntimeContext
        {
            public KaleidoscopeFpsMonitor fpsMonitor;
            public KaleidoscopePerformanceAnalyzer analyzer;
            public KaleidoscopeRenderPipeline pipeline;
            public KaleidoscopeMirrorController mirror;
            public KaleidoscopePhysicsChamber chamber;
            public GemstoneSpawner spawner;
            public GemSparkleController sparkles;
            public FakeCausticBunnyProjector caustics;
            public AdaptiveQualityController adaptive;
            public KaleidoscopeSourceModeController sourceMode;
            public KaleidoscopeSourceModeManager sourceManager;
            public KaleidoscopeSourceLibrary sourceLibrary;
            public ViewerComfortController comfort;
            public KaleidoscopeTemporalStabilizer stabilizer;
            public KaleidoscopeDebugPanel debugPanel;
            public KaleidoscopeOperatorModeController operatorMode;
            public KaleidoscopeGuideOverlay guides;
            public KaleidoscopeRebuildGuard rebuildGuard;
            public KaleidoscopeInputBindingRegistry inputRegistry;
            public KaleidoscopeFramePacingController framePacing;
            public KaleidoscopeBeautyModeController beauty;
            public KaleidoscopeScenarioOrchestrator scenario;
            public AudioReactiveDirector audioDirector;
            public AudioAnalyzer audioAnalyzer;
            public ImageWallpaperSourceMode imageMode;

            public bool HasRuntime => pipeline != null || mirror != null || sourceLibrary != null || analyzer != null;

            public static RuntimeContext Find()
            {
                RuntimeContext context = new RuntimeContext
                {
                    fpsMonitor = FindRuntime<KaleidoscopeFpsMonitor>(),
                    analyzer = FindRuntime<KaleidoscopePerformanceAnalyzer>(),
                    pipeline = FindRuntime<KaleidoscopeRenderPipeline>(),
                    mirror = FindRuntime<KaleidoscopeMirrorController>(),
                    chamber = FindRuntime<KaleidoscopePhysicsChamber>(),
                    spawner = FindRuntime<GemstoneSpawner>(),
                    sparkles = FindRuntime<GemSparkleController>(),
                    caustics = FindRuntime<FakeCausticBunnyProjector>(),
                    adaptive = FindRuntime<AdaptiveQualityController>(),
                    sourceMode = FindRuntime<KaleidoscopeSourceModeController>(),
                    sourceManager = FindRuntime<KaleidoscopeSourceModeManager>(),
                    sourceLibrary = FindRuntime<KaleidoscopeSourceLibrary>(),
                    comfort = FindRuntime<ViewerComfortController>(),
                    stabilizer = FindRuntime<KaleidoscopeTemporalStabilizer>(),
                    debugPanel = FindRuntime<KaleidoscopeDebugPanel>(),
                    operatorMode = FindRuntime<KaleidoscopeOperatorModeController>(),
                    guides = FindRuntime<KaleidoscopeGuideOverlay>(),
                    rebuildGuard = FindRuntime<KaleidoscopeRebuildGuard>(),
                    inputRegistry = FindRuntime<KaleidoscopeInputBindingRegistry>(),
                    framePacing = FindRuntime<KaleidoscopeFramePacingController>(),
                    beauty = FindRuntime<KaleidoscopeBeautyModeController>(),
                    scenario = FindRuntime<KaleidoscopeScenarioOrchestrator>(),
                    audioDirector = FindRuntime<AudioReactiveDirector>(),
                    audioAnalyzer = FindRuntime<AudioAnalyzer>(),
                    imageMode = FindRuntime<ImageWallpaperSourceMode>()
                };
                return context;
            }

            private static T FindRuntime<T>() where T : UnityEngine.Object
            {
                T active = UnityEngine.Object.FindObjectOfType<T>();
                if (active != null)
                {
                    return active;
                }

                T[] candidates = Resources.FindObjectsOfTypeAll<T>();
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (IsSceneObject(candidates[i]))
                    {
                        return candidates[i];
                    }
                }

                return null;
            }

            private static bool IsSceneObject(UnityEngine.Object candidate)
            {
                if (candidate is Component component)
                {
                    return component.gameObject.scene.IsValid();
                }

                if (candidate is GameObject gameObject)
                {
                    return gameObject.scene.IsValid();
                }

                return false;
            }
        }
    }
}
