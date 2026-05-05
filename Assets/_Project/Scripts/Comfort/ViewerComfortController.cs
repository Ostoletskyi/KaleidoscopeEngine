using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.Source;
using UnityEngine;

namespace KaleidoscopeEngine.Comfort
{
    public enum ViewerComfortPreset
    {
        Calm,
        Normal,
        Energetic,
        Experimental
    }

    public enum KaleidoscopeBackgroundMode
    {
        DarkNeutral,
        SoftGray,
        DiffusedWhite,
        TransparentDebug
    }

    [DisallowMultipleComponent]
    public sealed class ViewerComfortController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private KaleidoscopePhysicsChamber physicsChamber;
        [SerializeField] private GemSparkleController sparkleController;
        [SerializeField] private FakeCausticBunnyProjector causticProjector;
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;

        [Header("Preset")]
        [SerializeField] private ViewerComfortPreset comfortPreset = ViewerComfortPreset.Normal;
        [SerializeField] private KaleidoscopeBackgroundMode backgroundMode = KaleidoscopeBackgroundMode.DarkNeutral;

        [Header("Global Comfort")]
        [SerializeField] private float maxVisualChangePerSecond = 0.55f;
        [SerializeField] private float flickerLimit = 0.18f;
        [SerializeField, Range(0f, 1f)] private float sparklePersistence = 0.82f;
        [SerializeField, Range(0f, 1f)] private float motionDamping = 0.74f;
        [SerializeField, Range(0f, 1f)] private float temporalSmoothing = 0.82f;
        [SerializeField] private float maxRotationSpeedForComfort = 200f;
        [SerializeField] private float maxBrightness = 1.18f;
        [SerializeField] private float minContrast = 0.76f;

        [Header("Update Rates")]
        [SerializeField] private float sparkleUpdateRate = 12f;
        [SerializeField] private float causticUpdateRate = 10f;
        [SerializeField] private float microShimmerUpdateRate = 7f;
        [SerializeField] private float breathingRate = 0.25f;

        [Header("Diagnostics")]
        [SerializeField] private float averageLuminance = 0.34f;
        [SerializeField] private float overexposedPixelRatio;
        [SerializeField] private float underexposedPixelRatio;
        [SerializeField] private float contrastScore = 0.72f;
        [SerializeField] private float flickerScore;
        [SerializeField] private bool unreadableImageWarning;

        private float previousLuminance = 0.34f;
        private float nextDiagnosticTime;

        public ViewerComfortPreset ComfortPreset => comfortPreset;
        public KaleidoscopeBackgroundMode BackgroundMode => backgroundMode;
        public float AverageLuminance => averageLuminance;
        public float OverexposedPixelRatio => overexposedPixelRatio;
        public float UnderexposedPixelRatio => underexposedPixelRatio;
        public float ContrastScore => contrastScore;
        public float FlickerScore => flickerScore;
        public bool UnreadableImageWarning => unreadableImageWarning;
        public float MaxBrightness => maxBrightness;
        public float MinContrast => minContrast;

        public void Configure(
            KaleidoscopeMirrorController mirror,
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopePhysicsChamber chamber,
            GemSparkleController sparkles,
            FakeCausticBunnyProjector caustics,
            KaleidoscopeSourceModeController sourceController)
        {
            mirrorController = mirror;
            renderPipeline = pipeline;
            physicsChamber = chamber;
            sparkleController = sparkles;
            causticProjector = caustics;
            sourceModeController = sourceController;
            ApplyPreset(comfortPreset);
        }

        private void Awake()
        {
            ApplyPreset(comfortPreset);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextDiagnosticTime)
            {
                return;
            }

            nextDiagnosticTime = Time.unscaledTime + 0.35f;
            UpdateDiagnostics();
            ApplyExposureSafety();
        }

        public void ApplyPreset(ViewerComfortPreset preset)
        {
            comfortPreset = preset;
            switch (preset)
            {
                case ViewerComfortPreset.Calm:
                    SetValues(0.32f, 0.1f, 0.9f, 0.88f, 0.9f, 80f, 1.05f, 0.8f, 10f, 8f, 5f, 0.16f);
                    break;
                case ViewerComfortPreset.Energetic:
                    SetValues(0.75f, 0.24f, 0.72f, 0.58f, 0.68f, 200f, 1.25f, 0.72f, 15f, 12f, 10f, 0.42f);
                    break;
                case ViewerComfortPreset.Experimental:
                    SetValues(1.2f, 0.42f, 0.52f, 0.28f, 0.42f, 200f, 1.32f, 0.62f, 24f, 18f, 15f, 0.65f);
                    break;
                default:
                    SetValues(0.55f, 0.18f, 0.82f, 0.74f, 0.82f, 200f, 1.18f, 0.76f, 12f, 10f, 7f, 0.25f);
                    break;
            }

            ApplyComfort();
        }

        public void CyclePreset()
        {
            int next = ((int)comfortPreset + 1) % System.Enum.GetValues(typeof(ViewerComfortPreset)).Length;
            ApplyPreset((ViewerComfortPreset)next);
        }

        public void SetBackgroundMode(KaleidoscopeBackgroundMode mode)
        {
            backgroundMode = mode;
            ApplyBackground();
        }

        public void CycleBackgroundMode()
        {
            int next = ((int)backgroundMode + 1) % System.Enum.GetValues(typeof(KaleidoscopeBackgroundMode)).Length;
            SetBackgroundMode((KaleidoscopeBackgroundMode)next);
        }

        public void ApplyComfort()
        {
            mirrorController?.ApplyViewerComfort(maxRotationSpeedForComfort, motionDamping, temporalSmoothing, maxBrightness, minContrast, breathingRate);
            sparkleController?.ApplyViewerComfort(sparkleUpdateRate, sparklePersistence, flickerLimit, maxBrightness);
            causticProjector?.ApplyViewerComfort(causticUpdateRate, flickerLimit, motionDamping);
            physicsChamber?.SetAdaptiveAxialSpeedCap(maxRotationSpeedForComfort);
            sourceModeController?.ApplyComfortPreset(comfortPreset);
            sourceModeController?.SetMicroShimmerRate(microShimmerUpdateRate);
            ApplyBackground();
        }

        private void SetValues(
            float visualChange,
            float flicker,
            float sparkle,
            float motion,
            float smoothing,
            float rotation,
            float brightness,
            float contrast,
            float sparkleRate,
            float causticRate,
            float shimmerRate,
            float breathing)
        {
            maxVisualChangePerSecond = visualChange;
            flickerLimit = flicker;
            sparklePersistence = sparkle;
            motionDamping = motion;
            temporalSmoothing = smoothing;
            maxRotationSpeedForComfort = rotation;
            maxBrightness = brightness;
            minContrast = contrast;
            sparkleUpdateRate = sparkleRate;
            causticUpdateRate = causticRate;
            microShimmerUpdateRate = shimmerRate;
            breathingRate = breathing;
        }

        private void ApplyBackground()
        {
            Color color = BackgroundColor(backgroundMode);
            renderPipeline?.SetSourceBackgroundColor(color);
            Camera main = Camera.main;
            if (main != null)
            {
                main.backgroundColor = color;
            }
        }

        private static Color BackgroundColor(KaleidoscopeBackgroundMode mode)
        {
            switch (mode)
            {
                case KaleidoscopeBackgroundMode.SoftGray:
                    return new Color(0.42f, 0.43f, 0.46f, 1f);
                case KaleidoscopeBackgroundMode.DiffusedWhite:
                    return new Color(0.84f, 0.85f, 0.87f, 1f);
                case KaleidoscopeBackgroundMode.TransparentDebug:
                    return new Color(0f, 0f, 0f, 0f);
                default:
                    return new Color(0.045f, 0.048f, 0.055f, 1f);
            }
        }

        private void UpdateDiagnostics()
        {
            Texture source = sourceModeController != null ? sourceModeController.CurrentSourceTexture : null;
            if (source is Texture2D texture && texture.isReadable)
            {
                SampleReadableTexture(texture);
            }
            else
            {
                averageLuminance = Mathf.Clamp01(Mathf.Lerp(averageLuminance, 0.34f, 0.08f));
                contrastScore = Mathf.Max(minContrast, contrastScore);
                overexposedPixelRatio = Mathf.Max(0f, overexposedPixelRatio - 0.04f);
                underexposedPixelRatio = Mathf.Max(0f, underexposedPixelRatio - 0.04f);
            }

            float delta = Mathf.Abs(averageLuminance - previousLuminance);
            flickerScore = Mathf.Lerp(flickerScore, delta / Mathf.Max(0.001f, maxVisualChangePerSecond), 0.28f);
            previousLuminance = averageLuminance;
            unreadableImageWarning = overexposedPixelRatio > 0.45f || underexposedPixelRatio > 0.65f || contrastScore < minContrast * 0.72f;
        }

        private void SampleReadableTexture(Texture2D texture)
        {
            Color[] pixels = texture.GetPixels();
            if (pixels == null || pixels.Length == 0)
            {
                return;
            }

            int step = Mathf.Max(1, pixels.Length / 2048);
            float sum = 0f;
            float min = 1f;
            float max = 0f;
            int over = 0;
            int under = 0;
            int count = 0;
            for (int i = 0; i < pixels.Length; i += step)
            {
                Color c = pixels[i];
                float luma = c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;
                sum += luma;
                min = Mathf.Min(min, luma);
                max = Mathf.Max(max, luma);
                over += luma > 0.92f ? 1 : 0;
                under += luma < 0.035f ? 1 : 0;
                count++;
            }

            averageLuminance = sum / Mathf.Max(1, count);
            overexposedPixelRatio = over / (float)Mathf.Max(1, count);
            underexposedPixelRatio = under / (float)Mathf.Max(1, count);
            contrastScore = Mathf.Clamp01(max - min);
        }

        private void ApplyExposureSafety()
        {
            if (!unreadableImageWarning)
            {
                return;
            }

            if (overexposedPixelRatio > 0.45f)
            {
                mirrorController?.AdjustCenterExposure(-0.75f);
                SetBackgroundMode(KaleidoscopeBackgroundMode.DarkNeutral);
            }
            else if (underexposedPixelRatio > 0.65f)
            {
                SetBackgroundMode(KaleidoscopeBackgroundMode.SoftGray);
                sourceModeController?.RequestMoreSourceDensity();
            }
        }
    }
}
