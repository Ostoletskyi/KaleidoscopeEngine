using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Mirrors;
using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    public abstract class ProceduralSourceTextureMode : MonoBehaviour, IKaleidoscopeSourceMode
    {
        [SerializeField] protected int textureSize = 256;
        [SerializeField] protected float updateRate = 60f;
        [SerializeField] protected float density = 0.65f;

        protected Texture2D texture;
        protected float nextUpdateTime;
        protected System.Random random = new System.Random(17);
        private bool initialized;

        public virtual void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            EnsureTexture();
            enabled = false;
        }

        public virtual void Activate()
        {
            Initialize();
            EnsureTexture();
            enabled = true;
            nextUpdateTime = 0f;
            GenerateTexture(0f, true);
        }

        public virtual void Deactivate()
        {
            enabled = false;
        }

        public virtual void Tick(float deltaTime)
        {
            if (Time.unscaledTime < nextUpdateTime)
            {
                return;
            }

            nextUpdateTime = Time.unscaledTime + 1f / Mathf.Max(1f, updateRate);
            GenerateTexture(deltaTime, false);
        }

        public void SetActiveWithoutRebuild(bool active)
        {
            enabled = active;
        }

        public virtual Texture GetSourceTexture()
        {
            EnsureTexture();
            return texture;
        }

        public abstract string GetSourceModeName();
        public string GetModeName() => GetSourceModeName();

        public virtual void SetQualityLevel(KaleidoscopeQualityLevel qualityLevel)
        {
            int nextSize = qualityLevel >= KaleidoscopeQualityLevel.High ? 384 : 256;
            updateRate = Mathf.Clamp(updateRate, 30f, 60f);
            if (textureSize != nextSize)
            {
                textureSize = nextSize;
                RecreateTexture();
            }
        }

        public void ApplyQualityLevel(KaleidoscopeQualityLevel qualityLevel) => SetQualityLevel(qualityLevel);

        public virtual void SetComfortPreset(ViewerComfortPreset comfortPreset)
        {
            switch (comfortPreset)
            {
                case ViewerComfortPreset.Calm:
                    updateRate = 30f;
                    density = Mathf.Clamp01(density * 0.88f);
                    break;
                case ViewerComfortPreset.Energetic:
                    updateRate = 60f;
                    break;
                case ViewerComfortPreset.Experimental:
                    updateRate = 90f;
                    break;
                default:
                    updateRate = 60f;
                    break;
            }
        }

        public void ApplyComfortPreset(ViewerComfortPreset comfortPreset) => SetComfortPreset(comfortPreset);

        public virtual void ResetMode()
        {
            density = 0.65f;
            nextUpdateTime = 0f;
            GenerateTexture(0f, true);
        }

        public virtual void RandomizeMode()
        {
            random = new System.Random(UnityEngine.Random.Range(1, int.MaxValue));
            GenerateTexture(0f, true);
        }

        public virtual void RequestMoreDensity()
        {
            density = Mathf.Clamp01(density + 0.08f);
            GenerateTexture(0f, true);
        }

        protected abstract void GenerateTexture(float deltaTime, bool force);

        protected void EnsureTexture()
        {
            if (texture != null && texture.width == textureSize)
            {
                return;
            }

            RecreateTexture();
        }

        protected void RecreateTexture()
        {
            if (texture != null)
            {
                KaleidoscopeEngine.Performance.KaleidoscopeRebuildGuard.RecordSourceModeRebuild(GetModeName());
                Destroy(texture);
            }

            texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                name = $"{GetModeName()} Source Texture",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
        }

        protected static Color LerpColor(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
        }
    }
}
