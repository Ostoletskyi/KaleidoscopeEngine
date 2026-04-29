using KaleidoscopeEngine.Mirrors;
using UnityEngine;

namespace KaleidoscopeEngine.Performance
{
    [CreateAssetMenu(
        fileName = "PerformanceBudgetProfile",
        menuName = "Kaleidoscope Engine/Performance/Budget Profile")]
    public sealed class PerformanceBudgetProfile : ScriptableObject
    {
        [Header("FPS")]
        [Min(1f)] public float minFpsHardLimit = 24f;
        [Min(1f)] public float targetFps = 30f;
        [Min(0f)] public float safetyMarginFps = 3f;

        [Header("Timing")]
        [Min(0.1f)] public float degradationDelay = 0.7f;
        [Min(0.1f)] public float recoveryDelay = 4f;
        [Min(0.05f)] public float emergencyDelay = 0.25f;
        [Range(0.05f, 1f)] public float adaptationSpeed = 0.35f;
        [Range(0f, 8f)] public float hysteresis = 2f;

        [Header("Budget Floors")]
        [Min(0)] public int minimumVisualMicroChips = 200;
        [Min(0)] public int minimumSparkles = 8;
        [Min(0)] public int minimumCausticSpots = 4;
        [Min(0f)] public float minimumAxialSpeedCap = 8f;
        public KaleidoscopeQualityLevel emergencyQuality = KaleidoscopeQualityLevel.Low;

        [Header("Budget Ceilings")]
        [Min(0)] public int maximumVisualMicroChips = 5200;
        [Min(0)] public int maximumSparkles = 160;
        [Min(0)] public int maximumCausticSpots = 48;
        [Min(0f)] public float maximumAxialSpeedCap = 90f;

        public static PerformanceBudgetProfile CreateRuntimeDefault()
        {
            PerformanceBudgetProfile profile = CreateInstance<PerformanceBudgetProfile>();
            profile.name = "Runtime Default Performance Budget";
            return profile;
        }
    }
}
