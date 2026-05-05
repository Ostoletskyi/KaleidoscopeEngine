using System;
using System.Collections.Generic;
using System.Text;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.Scenario;
using KaleidoscopeEngine.Source;
using KaleidoscopeEngine.UI;
using UnityEngine;

namespace KaleidoscopeEngine.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioReactiveDirector : MonoBehaviour
    {
        private struct VisualPulse
        {
            public AudioReactiveEventType type;
            public float age;
            public float duration;
            public float magnitude;
            public float zoomImpulse;
            public float brightnessImpulse;
            public float contrastImpulse;
            public float saturationImpulse;
            public float vignetteImpulse;
            public float spinImpulse;
            public float seamImpulse;
            public float imageMotionImpulse;
            public bool freezeMotion;
        }

        private static readonly int[] MusicalSegments =
        {
            6, 8, 10, 12, 16,
            24, 32, 40, 48, 56, 64
        };

        [Header("References")]
        [SerializeField] private AudioAnalyzer analyzer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;
        [SerializeField] private KaleidoscopeScenarioOrchestrator scenarioOrchestrator;
        [SerializeField] private KaleidoscopeDebugPanel debugPanel;
        [SerializeField] private KaleidoscopeLauncherUI launcherUI;

        [Header("Director")]
        [SerializeField] private bool reactiveEnabled = true;
        [SerializeField] private bool beatDebugOverlay;
        [SerializeField] private EffectLevel intensityLevel = EffectLevel.Normal;
        [SerializeField, Range(4, 32)] private int maxQueuedEvents = 16;
        [SerializeField, Range(0.25f, 8f)] private float majorEventCooldown = 4f;
        [SerializeField, Range(0.1f, 2f)] private float visualReleaseSpeed = 0.9f;
        [SerializeField, Range(0f, 1f)] private float comfortLimiter = 0.72f;

        private readonly Queue<AudioReactiveEvent> pendingEvents = new Queue<AudioReactiveEvent>();
        private readonly List<VisualPulse> activePulses = new List<VisualPulse>();
        private readonly System.Random random = new System.Random(91877);
        private StringBuilder queueBuilder;
        private ImageWallpaperSourceMode imageMode;
        private float nextAudioSourceSearchTime;
        private float lastMajorEventTime = -10f;
        private float lastScenarioStepTime = -10f;
        private float buildPressure;
        private bool baselineValid;
        private float baseZoom;
        private float baseSpin;
        private float baseBrightness;
        private float baseContrast;
        private float baseSaturation;
        private float baseVignetteStrength;
        private float baseSeamSoftness;
        private float baseImageScroll;
        private float baseImageZoom;
        private float baseImageRotation;
        private float baseImageInterval;
        private int baseSegmentCount;

        public bool ReactiveEnabled => reactiveEnabled;
        public bool BeatDebugOverlay => beatDebugOverlay;
        public EffectLevel IntensityLevel => intensityLevel;
        public AudioMusicalState CurrentMusicalState => analyzer != null ? analyzer.MusicalState : AudioMusicalState.Silence;
        public float BassEnergy => analyzer != null ? analyzer.BassEnergy : 0f;
        public float MidEnergy => analyzer != null ? analyzer.MidEnergy : 0f;
        public float HighEnergy => analyzer != null ? analyzer.HighEnergy : 0f;
        public float OverallEnergy => analyzer != null ? analyzer.OverallEnergy : 0f;
        public float EnergyDelta => analyzer != null ? analyzer.EnergyDelta : 0f;
        public float BeatConfidence => analyzer != null ? analyzer.BeatConfidence : 0f;
        public float BpmEstimate => analyzer != null ? analyzer.BpmEstimate : 0f;
        public int PendingEventCount => pendingEvents.Count;
        public int ActivePulseCount => activePulses.Count;
        public string ActiveVisualEventQueue => BuildQueueSummary();

        public void Configure(
            AudioAnalyzer audioAnalyzer,
            KaleidoscopeMirrorController mirror,
            KaleidoscopeSourceModeController sourceController,
            KaleidoscopeScenarioOrchestrator scenario,
            KaleidoscopeDebugPanel panel,
            KaleidoscopeLauncherUI launcher)
        {
            SetAnalyzer(audioAnalyzer);
            mirrorController = mirror;
            sourceModeController = sourceController;
            scenarioOrchestrator = scenario;
            debugPanel = panel;
            launcherUI = launcher;
            ResolveImageMode();
            TryAcquireAudioSource(true);
            CaptureBaseline();
        }

        public void SetAudioSource(AudioSource source)
        {
            audioSource = source;
            if (analyzer == null)
            {
                analyzer = GetComponent<AudioAnalyzer>();
            }

            analyzer?.SetAudioSource(source);
            Resync();
        }

        public void ToggleReactiveMode()
        {
            SetReactiveEnabled(!reactiveEnabled);
        }

        public void SetReactiveEnabled(bool enabled)
        {
            if (reactiveEnabled == enabled)
            {
                return;
            }

            reactiveEnabled = enabled;
            if (reactiveEnabled)
            {
                CaptureBaseline();
                Resync();
            }
            else
            {
                RestoreBaseline();
                pendingEvents.Clear();
                activePulses.Clear();
            }

            debugPanel?.PostOperatorMessage(reactiveEnabled ? "Audio Reactive Director enabled" : "Audio Reactive Director disabled");
        }

        public void ToggleBeatDebugOverlay()
        {
            beatDebugOverlay = !beatDebugOverlay;
            debugPanel?.PostOperatorMessage(beatDebugOverlay ? "Beat debug overlay enabled" : "Beat debug overlay disabled");
        }

        public void Resync()
        {
            analyzer?.Resync();
            pendingEvents.Clear();
            activePulses.Clear();
            buildPressure = 0f;
            lastMajorEventTime = -10f;
            CaptureBaseline();
            debugPanel?.PostOperatorMessage("Audio Reactive Director resynced");
        }

        public void SetIntensityLevel(EffectLevel level)
        {
            intensityLevel = level;
        }

        private void Awake()
        {
            if (analyzer == null)
            {
                analyzer = GetComponent<AudioAnalyzer>();
            }

            SetAnalyzer(analyzer);
        }

        private void OnEnable()
        {
            SetAnalyzer(analyzer);
        }

        private void OnDisable()
        {
            if (analyzer != null)
            {
                analyzer.AudioEventDetected -= HandleAudioEvent;
            }
        }

        private void Update()
        {
            TryAcquireAudioSource(false);
            if (!reactiveEnabled || mirrorController == null)
            {
                return;
            }

            if (!baselineValid)
            {
                CaptureBaseline();
            }

            ResolveImageMode();
            ProcessEventQueue();
            UpdateBuildPressure();
            ApplyVisualPulses(Time.unscaledDeltaTime);
        }

        private void OnGUI()
        {
            if (!beatDebugOverlay || !reactiveEnabled)
            {
                return;
            }

            Rect rect = new Rect(16f, 16f, 260f, 142f);
            GUI.color = new Color(0f, 0f, 0f, 0.76f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 10f, rect.width - 24f, rect.height - 20f));
            GUILayout.Label("AUDIO REACTIVE");
            GUILayout.Label($"State: {CurrentMusicalState}");
            GUILayout.Label($"BPM: {(BpmEstimate > 0.1f ? BpmEstimate.ToString("F0") : "n/a")}");
            GUILayout.Label($"Bass: {BassEnergy:F2}  Overall: {OverallEnergy:F2}");
            GUILayout.Label($"Beat: {BeatConfidence:F2}  Queue: {pendingEvents.Count}/{activePulses.Count}");
            GUILayout.Label(ActiveVisualEventQueue);
            GUILayout.EndArea();
        }

        private void SetAnalyzer(AudioAnalyzer audioAnalyzer)
        {
            if (analyzer == audioAnalyzer && analyzer != null)
            {
                analyzer.AudioEventDetected -= HandleAudioEvent;
                analyzer.AudioEventDetected += HandleAudioEvent;
                return;
            }

            if (analyzer != null)
            {
                analyzer.AudioEventDetected -= HandleAudioEvent;
            }

            analyzer = audioAnalyzer;
            if (analyzer != null)
            {
                analyzer.AudioEventDetected += HandleAudioEvent;
            }
        }

        private void TryAcquireAudioSource(bool force)
        {
            if (!force && Time.unscaledTime < nextAudioSourceSearchTime)
            {
                return;
            }

            nextAudioSourceSearchTime = Time.unscaledTime + 1f;
            if (audioSource != null)
            {
                analyzer?.SetAudioSource(audioSource);
                return;
            }

            if (launcherUI != null && launcherUI.ActiveAudioSource != null)
            {
                SetAudioSource(launcherUI.ActiveAudioSource);
                return;
            }

            AudioSource[] sources = FindObjectsOfType<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null && sources[i].isPlaying)
                {
                    SetAudioSource(sources[i]);
                    return;
                }
            }
        }

        private void HandleAudioEvent(AudioReactiveEvent audioEvent)
        {
            if (!reactiveEnabled)
            {
                return;
            }

            if (pendingEvents.Count >= maxQueuedEvents)
            {
                pendingEvents.Dequeue();
            }

            pendingEvents.Enqueue(audioEvent);
        }

        private void ProcessEventQueue()
        {
            int budget = 4;
            while (pendingEvents.Count > 0 && budget-- > 0)
            {
                AudioReactiveEvent audioEvent = pendingEvents.Dequeue();
                VisualPulse pulse = CreatePulse(audioEvent);
                if (pulse.duration > 0f)
                {
                    activePulses.Add(pulse);
                }

                if (audioEvent.Type == AudioReactiveEventType.Drop)
                {
                    ApplyDropTransition(audioEvent);
                }
            }
        }

        private VisualPulse CreatePulse(AudioReactiveEvent audioEvent)
        {
            float intensity = IntensityScale();
            float confidence = Mathf.Lerp(0.55f, 1.35f, audioEvent.Confidence) * intensity * comfortLimiter;
            switch (audioEvent.Type)
            {
                case AudioReactiveEventType.StrongBeat:
                    return Pulse(audioEvent.Type, 0.34f, confidence, 0.075f, 0.09f, 0.035f, 0.055f, -0.012f, 18f, 0.014f, 0.02f, false);
                case AudioReactiveEventType.Snare:
                    return Pulse(audioEvent.Type, 0.24f, confidence, 0.018f, 0.025f, 0.02f, 0.02f, -0.006f, 9f, 0.018f, 0.01f, false);
                case AudioReactiveEventType.Drop:
                    return Pulse(audioEvent.Type, 2.15f, confidence, 0.13f, -0.1f, 0.14f, 0.1f, 0.1f, 135f, 0.035f, 0.09f, false);
                case AudioReactiveEventType.Break:
                    return Pulse(audioEvent.Type, 1.45f, confidence, -0.05f, -0.22f, -0.08f, -0.28f, 0.14f, -baseSpin, 0f, -0.03f, true);
                case AudioReactiveEventType.Silence:
                    return Pulse(audioEvent.Type, 1.8f, confidence, -0.08f, -0.28f, -0.1f, -0.34f, 0.16f, -baseSpin, 0f, -0.04f, true);
                case AudioReactiveEventType.Build:
                    return Pulse(audioEvent.Type, 0.8f, confidence, 0.035f, 0.0f, 0.04f, 0.06f, 0.035f, 24f, 0.012f, 0.035f, false);
                default:
                    return Pulse(audioEvent.Type, 0.22f, confidence, 0.045f, 0.055f, 0.018f, 0.032f, -0.008f, 6f, 0.01f, 0.016f, false);
            }
        }

        private static VisualPulse Pulse(
            AudioReactiveEventType type,
            float duration,
            float magnitude,
            float zoom,
            float brightness,
            float contrast,
            float saturation,
            float vignette,
            float spin,
            float seam,
            float imageMotion,
            bool freezeMotion)
        {
            return new VisualPulse
            {
                type = type,
                duration = Mathf.Max(0.01f, duration),
                magnitude = Mathf.Max(0f, magnitude),
                zoomImpulse = zoom,
                brightnessImpulse = brightness,
                contrastImpulse = contrast,
                saturationImpulse = saturation,
                vignetteImpulse = vignette,
                spinImpulse = spin,
                seamImpulse = seam,
                imageMotionImpulse = imageMotion,
                freezeMotion = freezeMotion
            };
        }

        private void ApplyDropTransition(AudioReactiveEvent audioEvent)
        {
            float now = Time.unscaledTime;
            if (now - lastMajorEventTime < majorEventCooldown)
            {
                return;
            }

            lastMajorEventTime = now;
            int segment = PickDropSegment();
            mirrorController?.SetSegmentCount(segment);
            baseSegmentCount = segment;
            if (scenarioOrchestrator != null && now - lastScenarioStepTime > majorEventCooldown * 1.5f)
            {
                scenarioOrchestrator.NextScenario();
                lastScenarioStepTime = now;
            }

            debugPanel?.PostOperatorMessage($"Audio drop: segments {segment}");
        }

        private void UpdateBuildPressure()
        {
            float target = analyzer != null && analyzer.MusicalState == AudioMusicalState.Build
                ? Mathf.Clamp01(analyzer.OverallEnergy * 1.7f + analyzer.EnergyDelta * 6f)
                : 0f;
            buildPressure = Mathf.MoveTowards(buildPressure, target, Time.unscaledDeltaTime * (target > buildPressure ? 0.55f : 1.25f));
        }

        private void ApplyVisualPulses(float dt)
        {
            float zoom = 0f;
            float brightness = 0f;
            float contrast = 0f;
            float saturation = 0f;
            float vignette = 0f;
            float spin = 0f;
            float seam = 0f;
            float imageMotion = 0f;
            float freeze = 0f;

            for (int i = activePulses.Count - 1; i >= 0; i--)
            {
                VisualPulse pulse = activePulses[i];
                pulse.age += Mathf.Max(0f, dt) * Mathf.Max(0.1f, visualReleaseSpeed);
                float t = Mathf.Clamp01(pulse.age / Mathf.Max(0.01f, pulse.duration));
                float envelope = CalculateEnvelope(pulse.type, t) * pulse.magnitude;
                zoom += pulse.zoomImpulse * envelope;
                brightness += pulse.brightnessImpulse * envelope;
                contrast += pulse.contrastImpulse * envelope;
                saturation += pulse.saturationImpulse * envelope;
                vignette += pulse.vignetteImpulse * envelope;
                spin += pulse.spinImpulse * envelope;
                seam += pulse.seamImpulse * envelope;
                imageMotion += pulse.imageMotionImpulse * envelope;
                freeze = Mathf.Max(freeze, pulse.freezeMotion ? envelope : 0f);

                if (t >= 1f)
                {
                    activePulses.RemoveAt(i);
                }
                else
                {
                    activePulses[i] = pulse;
                }
            }

            float intensity = IntensityScale() * comfortLimiter;
            zoom += buildPressure * 0.06f * intensity;
            contrast += buildPressure * 0.05f * intensity;
            saturation += buildPressure * 0.08f * intensity;
            vignette += buildPressure * 0.045f * intensity;
            spin += buildPressure * 48f * intensity;
            imageMotion += buildPressure * 0.045f * intensity;

            float targetSpin = Mathf.Lerp(baseSpin + spin, 0f, Mathf.Clamp01(freeze));
            mirrorController.SetPatternZoomTarget(Mathf.Clamp(baseZoom + zoom, 0.45f, 3.5f));
            mirrorController.SetPatternRotationSpeed(targetSpin);
            mirrorController.SetSeamSoftness(Mathf.Clamp(baseSeamSoftness + seam, 0f, 0.25f));
            mirrorController.SetVignetteStrength(Mathf.Clamp(baseVignetteStrength + vignette, 0f, 0.6f));
            mirrorController.SetFinalColorGrading(
                Mathf.Clamp(baseBrightness + brightness, 0.35f, 1.4f),
                Mathf.Clamp(baseContrast + contrast, 0.55f, 1.75f),
                Mathf.Clamp(baseSaturation + saturation, 0.35f, 1.75f));

            if (imageMode != null)
            {
                imageMode.SetImageMotion(
                    Mathf.Max(0f, baseImageScroll + imageMotion),
                    Mathf.Max(0f, baseImageZoom + imageMotion * 0.8f),
                    baseImageRotation + imageMotion * 0.45f,
                    Mathf.Max(8f, baseImageInterval - buildPressure * 8f),
                    Mathf.Lerp(2f, 0.75f, buildPressure),
                    KaleidoscopeImageTransitionMode.FilmRoll);
            }
        }

        private static float CalculateEnvelope(AudioReactiveEventType type, float t)
        {
            if (type == AudioReactiveEventType.Drop || type == AudioReactiveEventType.Break || type == AudioReactiveEventType.Silence)
            {
                return 1f - Smooth01(t);
            }

            float attack = smoothstep(0f, 0.12f, t);
            float release = 1f - Smooth01(t);
            return attack * release;
        }

        private static float smoothstep(float edge0, float edge1, float value)
        {
            float t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        private float IntensityScale()
        {
            return KaleidoscopeMirrorController.MapEffectLevel(intensityLevel);
        }

        private int PickDropSegment()
        {
            int current = mirrorController != null ? mirrorController.SegmentCount : baseSegmentCount;
            int nearestIndex = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < MusicalSegments.Length; i++)
            {
                int distance = Mathf.Abs(MusicalSegments[i] - current);
                if (distance < bestDistance)
                {
                    nearestIndex = i;
                    bestDistance = distance;
                }
            }

            int step = random.Next(1, 4);
            int nextIndex = Mathf.Clamp(nearestIndex + step, 0, MusicalSegments.Length - 1);
            if (nextIndex == nearestIndex && nearestIndex > 0)
            {
                nextIndex--;
            }

            return MusicalSegments[nextIndex];
        }

        private void ResolveImageMode()
        {
            if (imageMode != null)
            {
                return;
            }

            if (sourceModeController != null)
            {
                imageMode = sourceModeController.GetComponent<ImageWallpaperSourceMode>();
            }
        }

        private void CaptureBaseline()
        {
            if (mirrorController == null)
            {
                baselineValid = false;
                return;
            }

            ResolveImageMode();
            baseZoom = mirrorController.RequestedPatternZoom;
            baseSpin = mirrorController.RequestedPatternRotationSpeedDeg;
            baseBrightness = mirrorController.Brightness;
            baseContrast = mirrorController.Contrast;
            baseSaturation = mirrorController.Saturation;
            baseVignetteStrength = mirrorController.VignetteStrength;
            baseSeamSoftness = mirrorController.SeamSoftness;
            baseSegmentCount = mirrorController.SegmentCount;
            if (imageMode != null)
            {
                baseImageScroll = imageMode.ImageScrollSpeed;
                baseImageZoom = imageMode.ImageZoomSpeed;
                baseImageRotation = imageMode.ImageRotationSpeed;
                baseImageInterval = imageMode.ImageChangeInterval;
            }
            else
            {
                baseImageScroll = mirrorController.ImageScrollSpeed;
                baseImageZoom = mirrorController.ImageZoomSpeed;
                baseImageRotation = mirrorController.ImageRotationSpeed;
                baseImageInterval = mirrorController.ImageChangeInterval;
            }

            baselineValid = true;
        }

        private void RestoreBaseline()
        {
            if (!baselineValid || mirrorController == null)
            {
                return;
            }

            mirrorController.SetPatternZoomTarget(baseZoom);
            mirrorController.SetPatternRotationSpeed(baseSpin);
            mirrorController.SetSegmentCount(baseSegmentCount);
            mirrorController.SetSeamSoftness(baseSeamSoftness);
            mirrorController.SetVignetteStrength(baseVignetteStrength);
            mirrorController.SetFinalColorGrading(baseBrightness, baseContrast, baseSaturation);
            if (imageMode != null)
            {
                imageMode.SetImageMotion(
                    baseImageScroll,
                    baseImageZoom,
                    baseImageRotation,
                    baseImageInterval,
                    2f,
                    KaleidoscopeImageTransitionMode.FilmRoll);
            }
        }

        private string BuildQueueSummary()
        {
            if (queueBuilder == null)
            {
                queueBuilder = new StringBuilder(96);
            }

            queueBuilder.Length = 0;
            if (pendingEvents.Count == 0 && activePulses.Count == 0)
            {
                return "No active visual events.";
            }

            if (pendingEvents.Count > 0)
            {
                queueBuilder.Append("queued ").Append(pendingEvents.Count);
            }

            if (activePulses.Count > 0)
            {
                if (queueBuilder.Length > 0)
                {
                    queueBuilder.Append(", ");
                }

                queueBuilder.Append("active ");
                int count = Mathf.Min(3, activePulses.Count);
                for (int i = 0; i < count; i++)
                {
                    if (i > 0)
                    {
                        queueBuilder.Append("/");
                    }

                    queueBuilder.Append(activePulses[i].type);
                }
            }

            return queueBuilder.ToString();
        }
    }
}
