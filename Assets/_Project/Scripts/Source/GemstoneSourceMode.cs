using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Mirrors;
using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    [DisallowMultipleComponent]
    public sealed class GemstoneSourceMode : MonoBehaviour, IKaleidoscopeSourceMode
    {
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        private bool initialized;

        public void Configure(KaleidoscopeRenderPipeline pipeline)
        {
            renderPipeline = pipeline;
        }

        public void Initialize()
        {
            initialized = true;
            enabled = false;
        }

        public void Activate()
        {
            if (!initialized)
            {
                Initialize();
            }

            renderPipeline?.ClearExternalSourceTexture();
            enabled = true;
        }

        public void Deactivate()
        {
            enabled = false;
        }

        public void Tick(float deltaTime) { }

        public Texture GetSourceTexture()
        {
            return renderPipeline != null ? renderPipeline.ActiveSourceTexture : null;
        }

        public string GetSourceModeName() => "Gemstones";
        public string GetModeName() => GetSourceModeName();

        public void SetQualityLevel(KaleidoscopeQualityLevel qualityLevel) { }
        public void ApplyQualityLevel(KaleidoscopeQualityLevel qualityLevel) => SetQualityLevel(qualityLevel);

        public void SetComfortPreset(ViewerComfortPreset comfortPreset) { }
        public void ApplyComfortPreset(ViewerComfortPreset comfortPreset) => SetComfortPreset(comfortPreset);

        public void SetActiveWithoutRebuild(bool active)
        {
            enabled = active;
        }

        public void ResetMode()
        {
            renderPipeline?.ClearExternalSourceTexture();
        }

        public void RandomizeMode() { }

        public void RequestMoreDensity() { }
    }
}
