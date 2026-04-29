using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.UI;
using UnityEngine;

namespace KaleidoscopeEngine.Performance
{
    [DisallowMultipleComponent]
    public sealed class AdaptiveQualityController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopeFpsMonitor fpsMonitor;
        [SerializeField] private PerformanceBudgetProfile budgetProfile;
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private GemSparkleController sparkleController;
        [SerializeField] private FakeCausticBunnyProjector causticProjector;
        [SerializeField] private KaleidoscopePhysicsChamber physicsChamber;
        [SerializeField] private KaleidoscopeHelpOverlay feedbackOverlay;

        [Header("Mode")]
        [SerializeField] private bool adaptiveQualityEnabled = true;
        [SerializeField] private bool autoBalanceEnabled = true;

        private float budget01 = 1f;
        private float nextDegradeTime;
        private float nextRecoverTime;
        private float nextEmergencyTime;
        private float nextFeedbackTime;
        private bool emergencyMode;

        public bool AdaptiveQualityEnabled => adaptiveQualityEnabled;
        public bool AutoBalanceEnabled => autoBalanceEnabled;
        public bool EmergencyMode => emergencyMode;
        public float Budget01 => budget01;
        public string PerformanceState => fpsMonitor != null ? fpsMonitor.PerformanceState.ToString() : "n/a";
        public float CurrentFps => fpsMonitor != null ? fpsMonitor.CurrentFps : 0f;
        public float SmoothedFps => fpsMonitor != null ? fpsMonitor.SmoothedFps : 0f;
        public float AverageFrameMs => fpsMonitor != null ? fpsMonitor.AverageFrameMs : 0f;
        public float TargetFps => budgetProfile != null ? budgetProfile.targetFps : 30f;
        public float MinFpsHardLimit => budgetProfile != null ? budgetProfile.minFpsHardLimit : 24f;
        public int RequestedMicroChips => spawner != null ? spawner.RequestedVisualMicroChipCount : 0;
        public int EffectiveMicroChips => spawner != null ? spawner.EffectiveVisualMicroChipCount : 0;
        public int RequestedSparkles => sparkleController != null ? sparkleController.RequestedMaxActiveSparkles : 0;
        public int EffectiveSparkles => sparkleController != null ? sparkleController.EffectiveMaxActiveSparkles : 0;
        public int RequestedCaustics => causticProjector != null ? causticProjector.RequestedSpotCount : 0;
        public int EffectiveCaustics => causticProjector != null ? causticProjector.EffectiveSpotCount : 0;
        public float AxialSpeedCap => physicsChamber != null ? physicsChamber.EffectiveAxialRotationSpeedCap : 0f;

        public void Configure(
            KaleidoscopeFpsMonitor monitor,
            KaleidoscopeRenderPipeline pipeline,
            GemstoneSpawner gemstoneSpawner,
            GemSparkleController sparkles,
            FakeCausticBunnyProjector caustics,
            KaleidoscopePhysicsChamber chamber,
            KaleidoscopeHelpOverlay overlay)
        {
            fpsMonitor = monitor;
            renderPipeline = pipeline;
            spawner = gemstoneSpawner;
            sparkleController = sparkles;
            causticProjector = caustics;
            physicsChamber = chamber;
            feedbackOverlay = overlay;
            EnsureBudgetProfile();
            fpsMonitor?.Configure(budgetProfile.minFpsHardLimit, budgetProfile.targetFps);
            ApplyBudget();
        }

        public void ToggleAdaptiveQuality()
        {
            adaptiveQualityEnabled = !adaptiveQualityEnabled;
            if (!adaptiveQualityEnabled)
            {
                emergencyMode = false;
                budget01 = 1f;
                ClearAdaptiveLimits();
            }

            Feedback(adaptiveQualityEnabled ? "Adaptive Quality Enabled" : "Adaptive Quality Disabled");
        }

        public void ToggleAutoBalance()
        {
            autoBalanceEnabled = !autoBalanceEnabled;
            Feedback(autoBalanceEnabled ? "Auto-Balance Enabled" : "Auto-Balance Disabled");
        }

        public void PerformancePresetDown()
        {
            budget01 = Mathf.Clamp01(budget01 - 0.18f);
            renderPipeline?.AdjustQualityLevel(-1);
            ApplyBudget();
            Feedback("Performance Preset Down");
        }

        public void PerformancePresetUp()
        {
            budget01 = Mathf.Clamp01(budget01 + 0.12f);
            if (!emergencyMode)
            {
                renderPipeline?.AdjustQualityLevel(1);
            }

            ApplyBudget();
            Feedback("Performance Preset Up");
        }

        public void ForceSafeMode()
        {
            EnterEmergencyMode("Safe Mode");
        }

        private void Awake()
        {
            EnsureBudgetProfile();
        }

        private void Update()
        {
            if (!adaptiveQualityEnabled || fpsMonitor == null || budgetProfile == null)
            {
                return;
            }

            KaleidoscopePerformanceState state = fpsMonitor.PerformanceState;
            float now = Time.unscaledTime;

            if (fpsMonitor.SmoothedFps <= budgetProfile.minFpsHardLimit && now >= nextEmergencyTime)
            {
                EnterEmergencyMode("Critical FPS");
                nextEmergencyTime = now + budgetProfile.emergencyDelay;
                return;
            }

            if (state == KaleidoscopePerformanceState.Critical || state == KaleidoscopePerformanceState.Warning)
            {
                if (now >= nextDegradeTime)
                {
                    float step = state == KaleidoscopePerformanceState.Critical ? 0.24f : 0.12f;
                    budget01 = Mathf.Clamp01(budget01 - step * budgetProfile.adaptationSpeed);
                    if (state == KaleidoscopePerformanceState.Critical)
                    {
                        renderPipeline?.AdjustQualityLevel(-1);
                    }

                    ApplyBudget();
                    Feedback(state == KaleidoscopePerformanceState.Critical
                        ? "Critical FPS: lowering RenderTexture"
                        : "FPS Warning: reducing sparkle density");
                    nextDegradeTime = now + budgetProfile.degradationDelay;
                    nextRecoverTime = now + budgetProfile.recoveryDelay;
                }

                return;
            }

            bool comfortablyAboveTarget = fpsMonitor.SmoothedFps >= budgetProfile.targetFps + budgetProfile.safetyMarginFps + budgetProfile.hysteresis;
            if (autoBalanceEnabled && comfortablyAboveTarget && now >= nextRecoverTime)
            {
                emergencyMode = false;
                budget01 = Mathf.Clamp01(budget01 + 0.08f * budgetProfile.adaptationSpeed);
                if (budget01 > 0.82f)
                {
                    renderPipeline?.AdjustQualityLevel(1);
                }

                ApplyBudget();
                Feedback("Recovered: restoring optical density");
                nextRecoverTime = now + budgetProfile.recoveryDelay;
            }
        }

        private void EnterEmergencyMode(string reason)
        {
            emergencyMode = true;
            budget01 = 0f;
            if (renderPipeline != null && renderPipeline.QualityLevel > budgetProfile.emergencyQuality)
            {
                renderPipeline.SetQualityLevel(budgetProfile.emergencyQuality, false);
            }

            ApplyBudget();
            Feedback($"{reason}: safe mode");
        }

        private void ApplyBudget()
        {
            EnsureBudgetProfile();
            float shapedBudget = Mathf.SmoothStep(0f, 1f, budget01);

            if (spawner != null)
            {
                int requested = spawner.RequestedVisualMicroChipCount;
                int effective = Mathf.RoundToInt(Mathf.Lerp(
                    budgetProfile.minimumVisualMicroChips,
                    Mathf.Min(requested, budgetProfile.maximumVisualMicroChips),
                    shapedBudget));
                spawner.SetAdaptiveVisualMicroChipLimit(effective);
            }

            if (sparkleController != null)
            {
                int requested = sparkleController.RequestedMaxActiveSparkles;
                int effective = Mathf.RoundToInt(Mathf.Lerp(
                    budgetProfile.minimumSparkles,
                    Mathf.Min(requested, budgetProfile.maximumSparkles),
                    shapedBudget));
                sparkleController.SetAdaptiveSparkleLimit(effective, Mathf.Lerp(0.25f, 1f, shapedBudget));
            }

            if (causticProjector != null)
            {
                int effective = Mathf.RoundToInt(Mathf.Lerp(
                    budgetProfile.minimumCausticSpots,
                    Mathf.Min(causticProjector.RequestedSpotCount, budgetProfile.maximumCausticSpots),
                    shapedBudget));
                causticProjector.SetAdaptiveSpotLimit(effective);
            }

            if (physicsChamber != null)
            {
                float cap = Mathf.Lerp(
                    budgetProfile.minimumAxialSpeedCap,
                    budgetProfile.maximumAxialSpeedCap,
                    shapedBudget);
                physicsChamber.SetAdaptiveAxialSpeedCap(cap);
            }
        }

        private void ClearAdaptiveLimits()
        {
            spawner?.ClearAdaptiveVisualMicroChipLimit();
            sparkleController?.ClearAdaptiveSparkleLimit();
            causticProjector?.ClearAdaptiveSpotLimit();
            physicsChamber?.ClearAdaptiveAxialSpeedCap();
        }

        private void EnsureBudgetProfile()
        {
            if (budgetProfile == null)
            {
                budgetProfile = PerformanceBudgetProfile.CreateRuntimeDefault();
            }
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
    }
}
