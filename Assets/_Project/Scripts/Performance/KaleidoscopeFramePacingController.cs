using UnityEngine;

namespace KaleidoscopeEngine.Performance
{
    public enum KaleidoscopeFramePacingMode
    {
        Uncapped,
        VSync,
        Fps60,
        Fps30,
        Fps24Safe
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeFramePacingController : MonoBehaviour
    {
        [SerializeField] private KaleidoscopeFramePacingMode framePacingMode = KaleidoscopeFramePacingMode.VSync;
        [SerializeField] private int desiredPreviewFps = 0;
        [SerializeField] private int minSafeFps = 24;
        [SerializeField] private bool adaptiveQualityEnabled = true;
        [SerializeField] private bool adaptiveQualityCanThrottleUpdates;

        public KaleidoscopeFramePacingMode FramePacingMode => framePacingMode;
        public int DesiredPreviewFps => desiredPreviewFps;
        public int MinSafeFps => minSafeFps;
        public bool AdaptiveQualityEnabled => adaptiveQualityEnabled;
        public bool AdaptiveQualityCanThrottleUpdates => adaptiveQualityCanThrottleUpdates;
        public int TargetFrameRate => Application.targetFrameRate;
        public int VSyncCount => QualitySettings.vSyncCount;

        private void Awake()
        {
            ApplyFramePacing();
        }

        public void SetFramePacingMode(KaleidoscopeFramePacingMode mode)
        {
            framePacingMode = mode;
            ApplyFramePacing();
        }

        public void SetAdaptiveQualityState(bool enabled, bool canThrottleUpdates)
        {
            adaptiveQualityEnabled = enabled;
            adaptiveQualityCanThrottleUpdates = canThrottleUpdates;
        }

        public void ApplyFramePacing()
        {
            switch (framePacingMode)
            {
                case KaleidoscopeFramePacingMode.Uncapped:
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = -1;
                    desiredPreviewFps = 0;
                    break;
                case KaleidoscopeFramePacingMode.Fps60:
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = 60;
                    desiredPreviewFps = 60;
                    break;
                case KaleidoscopeFramePacingMode.Fps30:
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = 30;
                    desiredPreviewFps = 30;
                    break;
                case KaleidoscopeFramePacingMode.Fps24Safe:
                    QualitySettings.vSyncCount = 0;
                    Application.targetFrameRate = 24;
                    desiredPreviewFps = 24;
                    break;
                default:
                    QualitySettings.vSyncCount = 1;
                    Application.targetFrameRate = -1;
                    desiredPreviewFps = 0;
                    break;
            }
        }
    }
}
