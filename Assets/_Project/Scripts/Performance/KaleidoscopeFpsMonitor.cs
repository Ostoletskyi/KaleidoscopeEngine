using UnityEngine;

namespace KaleidoscopeEngine.Performance
{
    public enum KaleidoscopePerformanceState
    {
        Excellent,
        Stable,
        Warning,
        Critical,
        Recovery
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeFpsMonitor : MonoBehaviour
    {
        [Header("Sampling")]
        [SerializeField, Range(0.25f, 5f)] private float sampleWindowSeconds = 1.25f;
        [SerializeField, Range(0.01f, 0.5f)] private float smoothingFactor = 0.08f;

        [Header("Thresholds")]
        [SerializeField, Min(1f)] private float warningFps = 27f;
        [SerializeField, Min(1f)] private float criticalFps = 24f;
        [SerializeField, Min(1f)] private float targetFps = 30f;

        private float windowTimer;
        private int framesInWindow;
        private float frameMsSum;
        private float minFps = float.MaxValue;
        private float maxFps;
        private float previousSmoothedFps;

        public float CurrentFps { get; private set; }
        public float SmoothedFps { get; private set; }
        public float AverageFrameMs { get; private set; }
        public float MinFps => minFps < float.MaxValue ? minFps : 0f;
        public float MaxFps => maxFps;
        public float FpsTrend { get; private set; }
        public KaleidoscopePerformanceState PerformanceState { get; private set; } = KaleidoscopePerformanceState.Stable;
        public float WarningFps => warningFps;
        public float CriticalFps => criticalFps;
        public float TargetFps => targetFps;

        public void Configure(float minFpsHardLimit, float desiredTargetFps)
        {
            criticalFps = Mathf.Max(1f, minFpsHardLimit);
            targetFps = Mathf.Max(criticalFps + 1f, desiredTargetFps);
            warningFps = Mathf.Max(criticalFps + 1f, Mathf.Lerp(criticalFps, targetFps, 0.55f));
        }

        private void Update()
        {
            float delta = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            CurrentFps = 1f / delta;
            SmoothedFps = SmoothedFps <= 0.01f
                ? CurrentFps
                : Mathf.Lerp(SmoothedFps, CurrentFps, smoothingFactor);
            FpsTrend = SmoothedFps - previousSmoothedFps;
            previousSmoothedFps = Mathf.Lerp(previousSmoothedFps, SmoothedFps, 0.18f);

            framesInWindow++;
            frameMsSum += delta * 1000f;
            windowTimer += delta;
            minFps = Mathf.Min(minFps, CurrentFps);
            maxFps = Mathf.Max(maxFps, CurrentFps);

            if (windowTimer >= sampleWindowSeconds)
            {
                AverageFrameMs = frameMsSum / Mathf.Max(1, framesInWindow);
                frameMsSum = 0f;
                framesInWindow = 0;
                windowTimer = 0f;
            }

            PerformanceState = ResolveState();
        }

        private KaleidoscopePerformanceState ResolveState()
        {
            if (SmoothedFps <= criticalFps)
            {
                return KaleidoscopePerformanceState.Critical;
            }

            if (SmoothedFps <= warningFps || FpsTrend < -0.65f)
            {
                return KaleidoscopePerformanceState.Warning;
            }

            if (FpsTrend > 0.35f && SmoothedFps < targetFps)
            {
                return KaleidoscopePerformanceState.Recovery;
            }

            if (SmoothedFps >= targetFps + 8f)
            {
                return KaleidoscopePerformanceState.Excellent;
            }

            return KaleidoscopePerformanceState.Stable;
        }
    }
}
