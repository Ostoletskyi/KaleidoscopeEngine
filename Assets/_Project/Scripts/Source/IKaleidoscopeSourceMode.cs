using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Mirrors;
using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    public interface IKaleidoscopeSourceMode
    {
        void Initialize();
        void Activate();
        void Deactivate();
        void Tick(float deltaTime);
        Texture GetSourceTexture();
        string GetSourceModeName();
        void SetQualityLevel(KaleidoscopeQualityLevel qualityLevel);
        void SetComfortPreset(ViewerComfortPreset comfortPreset);
        void SetActiveWithoutRebuild(bool active);
        void ResetMode();
        void RandomizeMode();
        void RequestMoreDensity();
    }
}
