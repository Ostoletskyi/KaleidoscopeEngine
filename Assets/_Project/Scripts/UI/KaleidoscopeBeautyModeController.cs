using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.Performance;
using KaleidoscopeEngine.Source;
using UnityEngine;

namespace KaleidoscopeEngine.UI
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeBeautyModeController : MonoBehaviour
    {
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;
        [SerializeField] private KaleidoscopeFramePacingController framePacingController;
        [SerializeField] private bool beautyModeEnabled;
        [SerializeField] private KaleidoscopeBeautyPreset activePreset = KaleidoscopeBeautyPreset.WarmGlass;

        public bool BeautyModeEnabled => beautyModeEnabled;
        public KaleidoscopeBeautyPreset ActivePreset => activePreset;

        public void Configure(
            KaleidoscopeMirrorController mirror,
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopeSourceModeController sourceController,
            KaleidoscopeFramePacingController pacingController)
        {
            mirrorController = mirror;
            renderPipeline = pipeline;
            sourceModeController = sourceController;
            framePacingController = pacingController;
        }

        public void SetBeautyMode(bool enabled)
        {
            beautyModeEnabled = enabled;
            mirrorController?.SetBeautyMode(enabled, activePreset);
            if (enabled)
            {
                renderPipeline?.SetStableHighQuality();
                framePacingController?.SetFramePacingMode(KaleidoscopeFramePacingMode.VSync);
                if (sourceModeController != null && sourceModeController.CurrentMode == KaleidoscopeSourceModeKind.Gemstones)
                {
                    sourceModeController.SetMode(KaleidoscopeSourceModeKind.ImageWallpaper);
                }
            }
        }

        public void ApplyPreset(KaleidoscopeBeautyPreset preset)
        {
            activePreset = preset;
            mirrorController?.ApplyBeautyPreset(preset);
            if (beautyModeEnabled)
            {
                mirrorController?.SetBeautyMode(true, preset);
            }
        }
    }
}
