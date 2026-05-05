using System;
using System.Text;
using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.Source;
using KaleidoscopeEngine.UI;
using UnityEngine;

namespace KaleidoscopeEngine.Scenario
{
    public enum KaleidoscopeScenarioPreset
    {
        CalmFlow,
        JewelStorm,
        SlowHypnosis,
        FastGeometry,
        MusicVideo,
        Experimental
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeScenarioOrchestrator : MonoBehaviour
    {
        private static readonly int[] ValidSegments =
        {
            6, 8, 10, 12, 16,
            24, 32, 40, 48, 56, 64
        };

        private struct ScenarioDefinition
        {
            public KaleidoscopeScenarioPreset preset;
            public string displayName;
            public Vector2 zoomRange;
            public Vector2 spinRange;
            public Vector2 brightnessRange;
            public Vector2 contrastRange;
            public Vector2 saturationRange;
            public Vector2 centerOffsetRange;
            public Vector2 seamSoftnessRange;
            public Vector2 imageScrollRange;
            public Vector2 imageZoomRange;
            public Vector2 imageRotationRange;
            public Vector2 imageIntervalRange;
            public float holdDuration;
            public float transitionDuration;
            public float randomness;
            public float sourceVariationChance;
            public int minSegments;
            public int maxSegments;
            public KaleidoscopeColorDepthMode minColorDepth;
            public KaleidoscopeColorDepthMode maxColorDepth;
        }

        private struct ParameterState
        {
            public float zoom;
            public float spin;
            public float brightness;
            public float contrast;
            public float saturation;
            public Vector2 centerOffset;
            public float seamSoftness;
            public float mirrorAngle;
            public int segmentCount;
            public KaleidoscopeColorDepthMode colorDepth;
            public float imageScrollSpeed;
            public float imageZoomSpeed;
            public float imageRotationSpeed;
            public float imageChangeInterval;
            public KaleidoscopeBeautyPreset beautyPreset;
        }

        [Header("References")]
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;
        [SerializeField] private KaleidoscopeSourceLibrary sourceLibrary;
        [SerializeField] private KaleidoscopeBeautyModeController beautyModeController;
        [SerializeField] private ViewerComfortController comfortController;
        [SerializeField] private KaleidoscopeDebugPanel debugPanel;

        [Header("Scenario")]
        [SerializeField] private bool orchestratorEnabled = true;
        [SerializeField] private KaleidoscopeScenarioPreset currentScenario = KaleidoscopeScenarioPreset.CalmFlow;
        [SerializeField] private EffectLevel intensityLevel = EffectLevel.Normal;

        private readonly System.Random random = new System.Random(71317);
        private ParameterState startState;
        private ParameterState targetState;
        private float holdTimer;
        private float transitionTimer;
        private float holdDuration;
        private float transitionDuration;
        private bool transitioning;
        private string activeParameterChanges = "No active transition.";
        private KaleidoscopeColorDepthMode lastAppliedColorDepth;
        private KaleidoscopeBeautyPreset lastAppliedBeautyPreset;
        private ImageWallpaperSourceMode imageMode;

        public bool OrchestratorEnabled => orchestratorEnabled;
        public KaleidoscopeScenarioPreset CurrentScenario => currentScenario;
        public string CurrentScenarioName => ResolveDefinition(currentScenario).displayName;
        public float IntensityScale => KaleidoscopeMirrorController.MapEffectLevel(intensityLevel);
        public float NextTransitionSeconds => transitioning
            ? Mathf.Max(0f, transitionDuration - transitionTimer)
            : Mathf.Max(0f, holdDuration - holdTimer);
        public string ActiveParameterChanges => activeParameterChanges;

        public void Configure(
            KaleidoscopeMirrorController mirror,
            KaleidoscopeSourceModeController sourceController,
            KaleidoscopeSourceLibrary library,
            KaleidoscopeBeautyModeController beauty,
            ViewerComfortController comfort,
            KaleidoscopeDebugPanel panel)
        {
            mirrorController = mirror;
            sourceModeController = sourceController;
            sourceLibrary = library;
            beautyModeController = beauty;
            comfortController = comfort;
            debugPanel = panel;
            imageMode = sourceModeController != null ? sourceModeController.GetComponent<ImageWallpaperSourceMode>() : null;
            ResetTimeline(true);
        }

        private void Awake()
        {
            ResetTimeline(true);
        }

        private void Update()
        {
            if (!orchestratorEnabled || mirrorController == null)
            {
                return;
            }

            float dt = Mathf.Max(0f, Time.deltaTime);
            if (transitioning)
            {
                transitionTimer += dt;
                float t = Smooth01(transitionTimer / Mathf.Max(0.05f, transitionDuration));
                ApplyState(Interpolate(startState, targetState, t));
                if (transitionTimer >= transitionDuration)
                {
                    transitioning = false;
                    holdTimer = 0f;
                    ApplyState(targetState);
                    activeParameterChanges = "Holding target.";
                }

                return;
            }

            holdTimer += dt;
            if (holdTimer >= holdDuration)
            {
                BeginNextTransition();
            }
        }

        public void ToggleEnabled()
        {
            SetEnabled(!orchestratorEnabled);
        }

        public void ToggleAutoMode()
        {
            ToggleEnabled();
        }

        public void SetEnabled(bool enabled)
        {
            orchestratorEnabled = enabled;
            if (enabled)
            {
                ResetTimeline(false);
            }

            debugPanel?.PostOperatorMessage(orchestratorEnabled ? $"Scenario enabled: {CurrentScenarioName}" : "Scenario disabled: manual control");
        }

        public void SetScenario(KaleidoscopeScenarioPreset preset)
        {
            currentScenario = preset;
            ResetTimeline(false);
            debugPanel?.PostOperatorMessage($"Scenario: {CurrentScenarioName}");
        }

        public void NextScenario()
        {
            int count = Enum.GetValues(typeof(KaleidoscopeScenarioPreset)).Length;
            SetScenario((KaleidoscopeScenarioPreset)(((int)currentScenario + 1) % count));
        }

        private void ResetTimeline(bool silent)
        {
            startState = ReadCurrentState();
            targetState = startState;
            ScenarioDefinition definition = ResolveDefinition(currentScenario);
            holdDuration = definition.holdDuration;
            transitionDuration = definition.transitionDuration;
            holdTimer = 0f;
            transitionTimer = 0f;
            transitioning = false;
            lastAppliedColorDepth = mirrorController != null ? mirrorController.ColorDepthMode : KaleidoscopeColorDepthMode.FullColor;
            lastAppliedBeautyPreset = mirrorController != null ? mirrorController.BeautyPreset : KaleidoscopeBeautyPreset.WarmGlass;
            activeParameterChanges = "Waiting for next transition.";
            if (!silent)
            {
                BeginNextTransition();
            }
        }

        private void BeginNextTransition()
        {
            ScenarioDefinition definition = ResolveDefinition(currentScenario);
            startState = ReadCurrentState();
            targetState = BuildTargetState(definition);
            holdDuration = Mathf.Max(1f, definition.holdDuration * Mathf.Lerp(0.8f, 1.2f, Next01()));
            transitionDuration = Mathf.Max(0.25f, definition.transitionDuration * Mathf.Lerp(0.85f, 1.2f, Next01()));
            holdTimer = 0f;
            transitionTimer = 0f;
            transitioning = true;
            activeParameterChanges = DescribeChanges(startState, targetState);
            MaybeVarySource(definition);

            if (beautyModeController != null && targetState.beautyPreset != lastAppliedBeautyPreset)
            {
                beautyModeController.ApplyPreset(targetState.beautyPreset);
                lastAppliedBeautyPreset = targetState.beautyPreset;
            }
        }

        private ParameterState ReadCurrentState()
        {
            ParameterState state = new ParameterState
            {
                zoom = mirrorController != null ? mirrorController.RequestedPatternZoom : 1.18f,
                spin = mirrorController != null ? mirrorController.RequestedPatternRotationSpeedDeg : 4f,
                brightness = mirrorController != null ? mirrorController.Brightness : 1.05f,
                contrast = mirrorController != null ? mirrorController.Contrast : 1.2f,
                saturation = mirrorController != null ? mirrorController.Saturation : 1.25f,
                centerOffset = mirrorController != null ? mirrorController.CenterOffset : Vector2.zero,
                seamSoftness = mirrorController != null ? mirrorController.SeamSoftness : 0.025f,
                mirrorAngle = mirrorController != null ? mirrorController.MirrorAngleDegrees : 60f,
                segmentCount = mirrorController != null ? mirrorController.SegmentCount : 6,
                colorDepth = mirrorController != null ? mirrorController.ColorDepthMode : KaleidoscopeColorDepthMode.FullColor,
                imageScrollSpeed = imageMode != null ? imageMode.ImageScrollSpeed : 0.035f,
                imageZoomSpeed = imageMode != null ? imageMode.ImageZoomSpeed : 0.08f,
                imageRotationSpeed = imageMode != null ? imageMode.ImageRotationSpeed : 0.025f,
                imageChangeInterval = imageMode != null ? imageMode.ImageChangeInterval : 30f,
                beautyPreset = mirrorController != null ? mirrorController.BeautyPreset : KaleidoscopeBeautyPreset.WarmGlass
            };
            return state;
        }

        private ParameterState BuildTargetState(ScenarioDefinition definition)
        {
            int segmentTarget = PickSegmentTarget(definition.minSegments, definition.maxSegments);
            float intensity = IntensityScale;
            float comfortBrightnessMax = comfortController != null ? comfortController.MaxBrightness : 1.18f;
            float comfortContrastMin = comfortController != null ? comfortController.MinContrast : 0.76f;
            ParameterState state = new ParameterState
            {
                zoom = Range(definition.zoomRange),
                spin = ClampSpinForComfort(Range(definition.spinRange) * intensity),
                brightness = Mathf.Min(Range(definition.brightnessRange), comfortBrightnessMax),
                contrast = Mathf.Max(Range(definition.contrastRange), comfortContrastMin),
                saturation = Range(definition.saturationRange),
                centerOffset = new Vector2(Range(definition.centerOffsetRange), Range(definition.centerOffsetRange)) * intensity,
                seamSoftness = Range(definition.seamSoftnessRange),
                mirrorAngle = 360f / Mathf.Max(1, segmentTarget),
                segmentCount = segmentTarget,
                colorDepth = PickColorDepth(definition.minColorDepth, definition.maxColorDepth),
                imageScrollSpeed = Range(definition.imageScrollRange) * intensity,
                imageZoomSpeed = Range(definition.imageZoomRange) * intensity,
                imageRotationSpeed = Range(definition.imageRotationRange) * intensity,
                imageChangeInterval = Range(definition.imageIntervalRange),
                beautyPreset = PickBeautyPreset()
            };
            return state;
        }

        private void ApplyState(ParameterState state)
        {
            mirrorController.SetPatternZoomTarget(state.zoom);
            mirrorController.SetPatternRotationSpeed(state.spin);
            mirrorController.SetSegmentCount(state.segmentCount);
            mirrorController.SetCenterOffset(state.centerOffset);
            mirrorController.SetSeamSoftness(state.seamSoftness);
            mirrorController.SetFinalColorGrading(state.brightness, state.contrast, state.saturation);
            if (state.colorDepth != lastAppliedColorDepth)
            {
                mirrorController.SetColorDepthMode(state.colorDepth);
                lastAppliedColorDepth = state.colorDepth;
            }

            if (imageMode == null && sourceModeController != null)
            {
                imageMode = sourceModeController.GetComponent<ImageWallpaperSourceMode>();
            }

            imageMode?.SetImageMotion(
                state.imageScrollSpeed,
                state.imageZoomSpeed,
                state.imageRotationSpeed,
                state.imageChangeInterval,
                Mathf.Max(1f, transitionDuration * 0.35f),
                KaleidoscopeImageTransitionMode.FilmRoll);
        }

        private void MaybeVarySource(ScenarioDefinition definition)
        {
            if (sourceLibrary == null || definition.sourceVariationChance <= 0f || Next01() > definition.sourceVariationChance)
            {
                return;
            }

            sourceLibrary.NextPreset();
        }

        private ParameterState Interpolate(ParameterState a, ParameterState b, float t)
        {
            ParameterState state = new ParameterState
            {
                zoom = Mathf.Lerp(a.zoom, b.zoom, t),
                spin = Mathf.Lerp(a.spin, b.spin, t),
                brightness = Mathf.Lerp(a.brightness, b.brightness, t),
                contrast = Mathf.Lerp(a.contrast, b.contrast, t),
                saturation = Mathf.Lerp(a.saturation, b.saturation, t),
                centerOffset = Vector2.Lerp(a.centerOffset, b.centerOffset, t),
                seamSoftness = Mathf.Lerp(a.seamSoftness, b.seamSoftness, t),
                segmentCount = t < 0.5f ? a.segmentCount : b.segmentCount,
                mirrorAngle = 360f / Mathf.Max(1, t < 0.5f ? a.segmentCount : b.segmentCount),
                colorDepth = t < 0.5f ? a.colorDepth : b.colorDepth,
                imageScrollSpeed = Mathf.Lerp(a.imageScrollSpeed, b.imageScrollSpeed, t),
                imageZoomSpeed = Mathf.Lerp(a.imageZoomSpeed, b.imageZoomSpeed, t),
                imageRotationSpeed = Mathf.Lerp(a.imageRotationSpeed, b.imageRotationSpeed, t),
                imageChangeInterval = Mathf.Lerp(a.imageChangeInterval, b.imageChangeInterval, t),
                beautyPreset = t < 0.5f ? a.beautyPreset : b.beautyPreset
            };
            return state;
        }

        private string DescribeChanges(ParameterState a, ParameterState b)
        {
            StringBuilder builder = new StringBuilder();
            AppendChange(builder, "zoom", a.zoom, b.zoom, "F2");
            AppendChange(builder, "spin", a.spin, b.spin, "F0");
            if (a.segmentCount != b.segmentCount)
            {
                AppendText(builder, $"segments {b.segmentCount}");
            }
            AppendChange(builder, "seam", a.seamSoftness, b.seamSoftness, "F3");
            AppendChange(builder, "image scroll", a.imageScrollSpeed, b.imageScrollSpeed, "F3");
            if (a.colorDepth != b.colorDepth)
            {
                AppendText(builder, $"color depth {b.colorDepth}");
            }

            return builder.Length > 0 ? builder.ToString() : "Subtle hold refinement.";
        }

        private static void AppendChange(StringBuilder builder, string label, float a, float b, string format)
        {
            if (Mathf.Abs(a - b) < 0.001f)
            {
                return;
            }

            AppendText(builder, $"{label} {b.ToString(format)}");
        }

        private static void AppendText(StringBuilder builder, string text)
        {
            if (builder.Length > 0)
            {
                builder.Append(", ");
            }

            builder.Append(text);
        }

        private float ClampSpinForComfort(float spin)
        {
            float limit = mirrorController != null ? Mathf.Min(220f, mirrorController.MaxPatternAngularVelocity) : 180f;
            return Mathf.Clamp(spin, -limit, limit);
        }

        private int PickSegmentTarget(int minSegments, int maxSegments)
        {
            int safeMin = Mathf.Max(1, minSegments);
            int safeMax = Mathf.Min(64, Mathf.Max(safeMin, maxSegments));
            for (int i = 0; i < 12; i++)
            {
                int candidate = ValidSegments[random.Next(0, ValidSegments.Length)];
                if (candidate >= safeMin && candidate <= safeMax)
                {
                    return candidate;
                }
            }

            int nearest = ValidSegments[0];
            int bestDistance = int.MaxValue;
            int targetCenter = Mathf.RoundToInt((safeMin + safeMax) * 0.5f);
            for (int i = 0; i < ValidSegments.Length; i++)
            {
                int candidate = ValidSegments[i];
                if (candidate < safeMin || candidate > safeMax)
                {
                    continue;
                }

                int distance = Mathf.Abs(candidate - targetCenter);
                if (distance < bestDistance)
                {
                    nearest = candidate;
                    bestDistance = distance;
                }
            }

            if (bestDistance < int.MaxValue)
            {
                return nearest;
            }

            for (int i = 0; i < ValidSegments.Length; i++)
            {
                int candidate = ValidSegments[i];
                int distance = Mathf.Abs(candidate - targetCenter);
                if (distance < bestDistance)
                {
                    nearest = candidate;
                    bestDistance = distance;
                }
            }

            return nearest;
        }

        private KaleidoscopeColorDepthMode PickColorDepth(KaleidoscopeColorDepthMode minMode, KaleidoscopeColorDepthMode maxMode)
        {
            int min = Mathf.Min((int)minMode, (int)maxMode);
            int max = Mathf.Max((int)minMode, (int)maxMode);
            return (KaleidoscopeColorDepthMode)random.Next(min, max + 1);
        }

        private KaleidoscopeBeautyPreset PickBeautyPreset()
        {
            int count = Enum.GetValues(typeof(KaleidoscopeBeautyPreset)).Length;
            return (KaleidoscopeBeautyPreset)random.Next(0, count);
        }

        private float Range(Vector2 range)
        {
            return Mathf.Lerp(range.x, range.y, Next01());
        }

        private float Next01()
        {
            return (float)random.NextDouble();
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static ScenarioDefinition ResolveDefinition(KaleidoscopeScenarioPreset preset)
        {
            switch (preset)
            {
                case KaleidoscopeScenarioPreset.JewelStorm:
                    return Definition(preset, "Jewel Storm", 0.82f, 1.75f, -120f, 120f, 0.92f, 1.15f, 1.04f, 1.28f, 1.05f, 1.32f, 0.0f, 0.055f, 0.012f, 0.06f, 0.035f, 0.095f, 0.05f, 0.14f, -0.045f, 0.045f, 18f, 34f, 8f, 4f, 0.45f, 0.08f, 8, 18, KaleidoscopeColorDepthMode.ThirtyTwoColors, KaleidoscopeColorDepthMode.FullColor);
                case KaleidoscopeScenarioPreset.SlowHypnosis:
                    return Definition(preset, "Slow Hypnosis", 0.92f, 1.45f, -14f, 14f, 0.9f, 1.05f, 0.96f, 1.14f, 0.9f, 1.1f, 0.0f, 0.025f, 0.018f, 0.045f, 0.012f, 0.032f, 0.02f, 0.055f, -0.012f, 0.012f, 30f, 45f, 16f, 9f, 0.25f, 0.0f, 6, 12, KaleidoscopeColorDepthMode.TwoHundredFiftySixColors, KaleidoscopeColorDepthMode.FullColor);
                case KaleidoscopeScenarioPreset.FastGeometry:
                    return Definition(preset, "Fast Geometry", 0.72f, 2.05f, -180f, 180f, 0.9f, 1.12f, 1.0f, 1.26f, 0.98f, 1.24f, 0.0f, 0.065f, 0.02f, 0.085f, 0.05f, 0.12f, 0.06f, 0.18f, -0.06f, 0.06f, 12f, 24f, 6f, 3.5f, 0.55f, 0.12f, 8, 24, KaleidoscopeColorDepthMode.EightColors, KaleidoscopeColorDepthMode.TwoHundredFiftySixColors);
                case KaleidoscopeScenarioPreset.MusicVideo:
                    return Definition(preset, "Music Video", 0.78f, 1.9f, -200f, 200f, 0.92f, 1.18f, 1.02f, 1.32f, 1.0f, 1.36f, 0.0f, 0.06f, 0.018f, 0.075f, 0.05f, 0.13f, 0.06f, 0.16f, -0.055f, 0.055f, 10f, 22f, 6f, 3.2f, 0.6f, 0.18f, 8, 20, KaleidoscopeColorDepthMode.SixteenColors, KaleidoscopeColorDepthMode.FullColor);
                case KaleidoscopeScenarioPreset.Experimental:
                    return Definition(preset, "Experimental", 0.64f, 2.35f, -220f, 220f, 0.82f, 1.18f, 0.92f, 1.36f, 0.82f, 1.42f, 0.0f, 0.08f, 0.02f, 0.11f, 0.06f, 0.16f, 0.08f, 0.2f, -0.07f, 0.07f, 7f, 18f, 5f, 3f, 0.85f, 0.28f, 6, 24, KaleidoscopeColorDepthMode.TwoColors, KaleidoscopeColorDepthMode.FullColor);
                default:
                    return Definition(preset, "Calm Flow", 0.95f, 1.38f, -32f, 32f, 0.92f, 1.06f, 1.0f, 1.14f, 0.94f, 1.12f, 0.0f, 0.028f, 0.018f, 0.052f, 0.018f, 0.045f, 0.025f, 0.075f, -0.015f, 0.015f, 26f, 38f, 12f, 7f, 0.22f, 0.0f, 6, 12, KaleidoscopeColorDepthMode.TwoHundredFiftySixColors, KaleidoscopeColorDepthMode.FullColor);
            }
        }

        private static ScenarioDefinition Definition(
            KaleidoscopeScenarioPreset preset,
            string displayName,
            float zoomMin,
            float zoomMax,
            float spinMin,
            float spinMax,
            float brightnessMin,
            float brightnessMax,
            float contrastMin,
            float contrastMax,
            float saturationMin,
            float saturationMax,
            float centerOffsetMin,
            float centerOffsetMax,
            float seamMin,
            float seamMax,
            float imageScrollMin,
            float imageScrollMax,
            float imageZoomMin,
            float imageZoomMax,
            float imageRotationMin,
            float imageRotationMax,
            float imageIntervalMin,
            float imageIntervalMax,
            float holdDuration,
            float transitionDuration,
            float randomness,
            float sourceVariationChance,
            int minSegments,
            int maxSegments,
            KaleidoscopeColorDepthMode minColorDepth,
            KaleidoscopeColorDepthMode maxColorDepth)
        {
            return new ScenarioDefinition
            {
                preset = preset,
                displayName = displayName,
                zoomRange = new Vector2(zoomMin, zoomMax),
                spinRange = new Vector2(spinMin, spinMax),
                brightnessRange = new Vector2(brightnessMin, brightnessMax),
                contrastRange = new Vector2(contrastMin, contrastMax),
                saturationRange = new Vector2(saturationMin, saturationMax),
                centerOffsetRange = new Vector2(centerOffsetMin, centerOffsetMax),
                seamSoftnessRange = new Vector2(seamMin, seamMax),
                imageScrollRange = new Vector2(imageScrollMin, imageScrollMax),
                imageZoomRange = new Vector2(imageZoomMin, imageZoomMax),
                imageRotationRange = new Vector2(imageRotationMin, imageRotationMax),
                imageIntervalRange = new Vector2(imageIntervalMin, imageIntervalMax),
                holdDuration = holdDuration,
                transitionDuration = transitionDuration,
                randomness = randomness,
                sourceVariationChance = sourceVariationChance,
                minSegments = minSegments,
                maxSegments = maxSegments,
                minColorDepth = minColorDepth,
                maxColorDepth = maxColorDepth
            };
        }
    }
}
