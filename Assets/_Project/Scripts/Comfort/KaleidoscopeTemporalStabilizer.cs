using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.UI;
using UnityEngine;

namespace KaleidoscopeEngine.Comfort
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeTemporalStabilizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ViewerComfortController comfortController;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private OpticalSourceChamber opticalSourceChamber;
        [SerializeField] private KaleidoscopePhysicsChamber physicsChamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private GemSparkleController sparkleController;
        [SerializeField] private FakeCausticBunnyProjector causticProjector;
        [SerializeField] private KaleidoscopeHelpOverlay feedbackOverlay;

        [Header("Temporal Stabilization")]
        [SerializeField, Range(0f, 1f)] private float globalTemporalSmoothing = 0.88f;
        [SerializeField] private float maxVisualChangePerSecond = 0.42f;
        [SerializeField, Range(0f, 1f)] private float flickerSuppression = 0.86f;
        [SerializeField, Range(0f, 1f)] private float sparklePersistence = 0.9f;
        [SerializeField, Range(0f, 1f)] private float microDetailPersistence = 0.9f;
        [SerializeField, Range(0f, 1f)] private float sourceMotionDamping = 0.82f;
        [SerializeField, Range(0f, 1f)] private float mirrorMotionDamping = 0.86f;
        [SerializeField, Range(5f, 15f)] private float noiseUpdateRateHz = 8f;
        [SerializeField] private bool flickerSuppressionEnabled = true;

        [Header("Center Exposure Safety")]
        [SerializeField] private float centerMaxLuminance = 1.08f;
        [SerializeField] private float centerExposureClamp = 0.88f;
        [SerializeField] private float centerBloomClamp = 0.84f;
        [SerializeField] private float diffuserMaxBrightness = 0.86f;
        [SerializeField] private float backlightMaxIntensity = 1.75f;
        [SerializeField] private float centerDetailMinimum = 0.18f;

        [Header("Safe Mode")]
        [SerializeField] private bool safeModeEnabled;
        [SerializeField] private float criticalFlickerThreshold = 0.75f;
        [SerializeField] private float criticalOverexposureThreshold = 0.32f;
        [SerializeField] private float lowCoverageThreshold = 0.74f;

        private float previousFlickerScore;
        private float temporalChangeScore;
        private float nextCheckTime;
        private float nextFeedbackTime;
        private string lastActionableWarning = "OK";

        public bool SafeModeEnabled => safeModeEnabled;
        public bool FlickerSuppressionEnabled => flickerSuppressionEnabled;
        public float TemporalChangeScore => temporalChangeScore;
        public string FlickerStatus => ScoreStatus(comfortController != null ? comfortController.FlickerScore : 0f, 0.35f, criticalFlickerThreshold);
        public string ExposureStatus
        {
            get
            {
                if (comfortController == null)
                {
                    return "n/a";
                }

                if (comfortController.OverexposedPixelRatio > criticalOverexposureThreshold)
                {
                    return "Too Bright";
                }

                return comfortController.UnderexposedPixelRatio > 0.62f ? "Too Dark" : "OK";
            }
        }

        public string SourceCoverageStatus
        {
            get
            {
                if (spawner == null)
                {
                    return "n/a";
                }

                float coverage = spawner.SourceCoverageEstimate;
                if (coverage < lowCoverageThreshold)
                {
                    return "Low";
                }

                return coverage > 0.9f ? "Dense" : "Good";
            }
        }

        public string StabilityStatus => safeModeEnabled ? "Safe Mode" : temporalChangeScore > 0.5f ? "Noisy" : "Stable";
        public string LastActionableWarning => lastActionableWarning;
        public float MaxVisualChangePerSecond => maxVisualChangePerSecond;
        public float CenterMaxLuminance => centerMaxLuminance;
        public float DiffuserMaxBrightness => diffuserMaxBrightness;
        public float BacklightMaxIntensity => backlightMaxIntensity;

        public void Configure(
            ViewerComfortController comfort,
            KaleidoscopeMirrorController mirror,
            KaleidoscopeRenderPipeline pipeline,
            OpticalSourceChamber sourceChamber,
            KaleidoscopePhysicsChamber chamber,
            GemstoneSpawner gemstoneSpawner,
            GemSparkleController sparkles,
            FakeCausticBunnyProjector caustics,
            KaleidoscopeHelpOverlay overlay)
        {
            comfortController = comfort;
            mirrorController = mirror;
            renderPipeline = pipeline;
            opticalSourceChamber = sourceChamber;
            physicsChamber = chamber;
            spawner = gemstoneSpawner;
            sparkleController = sparkles;
            causticProjector = caustics;
            feedbackOverlay = overlay;
            ApplyStabilization(false);
        }

        private void Update()
        {
            if (!flickerSuppressionEnabled || Time.unscaledTime < nextCheckTime)
            {
                return;
            }

            nextCheckTime = Time.unscaledTime + 0.25f;
            float flicker = comfortController != null ? comfortController.FlickerScore : 0f;
            temporalChangeScore = Mathf.Lerp(temporalChangeScore, Mathf.Abs(flicker - previousFlickerScore) / Mathf.Max(0.001f, maxVisualChangePerSecond), 0.25f);
            previousFlickerScore = flicker;

            if (flicker > criticalFlickerThreshold)
            {
                EnterSafeMode("Flicker critical: freezing micro shimmer");
                return;
            }

            if (comfortController != null && comfortController.OverexposedPixelRatio > criticalOverexposureThreshold)
            {
                ApplyExposureGuard("Center clipped: reducing diffuser");
            }

            if (spawner != null && spawner.SourceCoverageEstimate < lowCoverageThreshold)
            {
                spawner.EnsureViewerDensityFloor(false);
                lastActionableWarning = "Source sparse: increasing visual chips";
            }
        }

        public void ToggleSafeMode()
        {
            if (safeModeEnabled)
            {
                safeModeEnabled = false;
                lastActionableWarning = "Safe mode released";
                comfortController?.ApplyPreset(ViewerComfortPreset.Normal);
                ApplyStabilization(false);
                Feedback("Safe Mode Off");
                return;
            }

            EnterSafeMode("Safe Mode");
        }

        public void EnterSafeMode(string reason)
        {
            safeModeEnabled = true;
            renderPipeline?.SetSafeModeQuality();
            comfortController?.ApplyPreset(ViewerComfortPreset.Calm);
            ApplyStabilization(true);
            ApplyExposureGuard(reason);
            lastActionableWarning = reason;
            Feedback($"{reason}: stable image");
        }

        public void ApplyStabilization(bool emergency)
        {
            mirrorController?.ApplyTemporalStability(globalTemporalSmoothing, flickerSuppression, microDetailPersistence, mirrorMotionDamping, noiseUpdateRateHz);
            mirrorController?.ApplyCenterExposureSafety(centerMaxLuminance, centerExposureClamp, centerBloomClamp, centerDetailMinimum);
            opticalSourceChamber?.ApplyExposureSafety(diffuserMaxBrightness, backlightMaxIntensity);
            physicsChamber?.ApplyTemporalStability(sourceMotionDamping, globalTemporalSmoothing, emergency ? 24f : 200f);
            sparkleController?.ApplyViewerComfort(12f, sparklePersistence, Mathf.Lerp(0.18f, 0.06f, flickerSuppression), centerMaxLuminance);
            causticProjector?.ApplyViewerComfort(9f, Mathf.Lerp(0.12f, 0.04f, flickerSuppression), mirrorMotionDamping);
            spawner?.EnsureViewerDensityFloor(false);
            renderPipeline?.SetStableHighQuality();

            if (emergency)
            {
                mirrorController?.ApplyEmergencyVisualStability();
                physicsChamber?.ApplyEmergencyMotionStability();
                sparkleController?.ApplyEmergencyStability();
                causticProjector?.ApplyEmergencyStability();
            }
        }

        private void ApplyExposureGuard(string warning)
        {
            mirrorController?.ApplyCenterExposureSafety(centerMaxLuminance, centerExposureClamp, centerBloomClamp, centerDetailMinimum);
            opticalSourceChamber?.ApplyExposureSafety(diffuserMaxBrightness, backlightMaxIntensity);
            comfortController?.SetBackgroundMode(KaleidoscopeBackgroundMode.DarkNeutral);
            lastActionableWarning = warning;
        }

        private void Feedback(string message)
        {
            if (feedbackOverlay == null || Time.unscaledTime < nextFeedbackTime)
            {
                return;
            }

            nextFeedbackTime = Time.unscaledTime + 1.2f;
            feedbackOverlay.ShowFeedback(message);
        }

        private static string ScoreStatus(float score, float warning, float critical)
        {
            if (score >= critical)
            {
                return "Critical";
            }

            return score >= warning ? "Warning" : "OK";
        }
    }
}
