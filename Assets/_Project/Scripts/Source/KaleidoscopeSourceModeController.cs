using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Mirrors;
using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    public enum KaleidoscopeSourceModeKind
    {
        Gemstones,
        ColoredGlassPhysical,
        Hexagons,
        ImageWallpaper,
        ProceduralColorBlobs,
        PolygonGeometry,
        LiquidIllusion,
        Hybrid,
        Experimental
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeSourceModeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private KaleidoscopeSourceModeManager modeManager;

        [Header("Mode")]
        [SerializeField] private KaleidoscopeSourceModeKind currentMode = KaleidoscopeSourceModeKind.Gemstones;
        [SerializeField] private bool adaptiveQualityEnabled = true;

        private GemstoneSourceMode gemstoneMode;
        private HexagonSourceMode hexagonMode;
        private ImageWallpaperSourceMode imageWallpaperMode;
        private LiquidSourceMode liquidMode;
        private IKaleidoscopeSourceMode activeMode;
        private KaleidoscopeQualityLevel currentQuality = KaleidoscopeQualityLevel.High;
        private ViewerComfortPreset currentComfort = ViewerComfortPreset.Normal;

        public string CurrentModeName => modeManager != null ? modeManager.ActiveSourceModeName : activeMode != null ? activeMode.GetSourceModeName() : currentMode.ToString();
        public KaleidoscopeSourceModeKind CurrentMode => currentMode;
        public bool AdaptiveQualityEnabled => adaptiveQualityEnabled;
        public Texture CurrentSourceTexture => modeManager != null ? modeManager.CurrentSourceTexture : activeMode?.GetSourceTexture();
        public KaleidoscopeSourceModeManager ModeManager => modeManager;

        public void Configure(KaleidoscopeRenderPipeline pipeline)
        {
            renderPipeline = pipeline;
            EnsureManager();
            EnsureModes();
            SetMode(currentMode);
        }

        private void Awake()
        {
            EnsureManager();
            EnsureModes();
        }

        private void Update()
        {
            if (modeManager != null)
            {
                currentMode = modeManager.ActiveModeKind;
                return;
            }

            activeMode?.Tick(Time.deltaTime);
            Texture texture = activeMode?.GetSourceTexture();
            if (activeMode is GemstoneSourceMode)
            {
                renderPipeline?.ClearExternalSourceTexture();
            }
            else if (texture != null)
            {
                renderPipeline?.SetExternalSourceTexture(texture, activeMode.GetSourceModeName());
                renderPipeline?.ReturnToKaleidoscopeView();
            }
        }

        public void CycleMode()
        {
            int count = System.Enum.GetValues(typeof(KaleidoscopeSourceModeKind)).Length;
            SetMode((KaleidoscopeSourceModeKind)(((int)currentMode + 1) % count));
        }

        public void SetMode(KaleidoscopeSourceModeKind mode)
        {
            if (modeManager != null)
            {
                if (modeManager.SwitchTo(NormalizeMode(mode)))
                {
                    currentMode = NormalizeMode(mode);
                }

                return;
            }

            EnsureModes();
            activeMode?.Deactivate();
            currentMode = NormalizeMode(mode);
            activeMode = ResolveMode(currentMode);
            if (activeMode == null)
            {
                return;
            }

            activeMode.SetQualityLevel(currentQuality);
            activeMode.SetComfortPreset(currentComfort);
            activeMode.Activate();
        }

        public void SetImageSourceTextures(Texture2D[] textures)
        {
            SetImageSourceTextures(textures, null);
        }

        public void SetImageSourceTextures(Texture2D[] textures, string[] imagePaths)
        {
            if (modeManager != null)
            {
                modeManager.SetImageSourceTextures(textures, imagePaths);
                return;
            }

            EnsureModes();
            imageWallpaperMode.SetTextures(textures, imagePaths);
        }

        public void RecordImageDiskRead(string path)
        {
            if (modeManager != null)
            {
                modeManager.RecordImageDiskRead(path);
                return;
            }

            EnsureModes();
            imageWallpaperMode.RecordExternalImageDiskRead(path);
        }

        public void RecordImageTextureCreate(string reason)
        {
            if (modeManager != null)
            {
                modeManager.RecordImageTextureCreate(reason);
                return;
            }

            EnsureModes();
            imageWallpaperMode.RecordExternalTextureCreate(reason);
        }

        public void ResetCurrentMode()
        {
            if (modeManager != null) modeManager.ResetCurrentMode();
            else activeMode?.ResetMode();
        }

        public void RandomizeCurrentMode()
        {
            if (modeManager != null) modeManager.RandomizeCurrentMode();
            else activeMode?.RandomizeMode();
        }

        public void ApplyQualityLevel(KaleidoscopeQualityLevel quality)
        {
            currentQuality = quality;
            if (modeManager != null) modeManager.SetQualityLevel(quality);
            else activeMode?.SetQualityLevel(quality);
        }

        public void ApplyComfortPreset(ViewerComfortPreset preset)
        {
            currentComfort = preset;
            if (modeManager != null) modeManager.SetComfortPreset(preset);
            else activeMode?.SetComfortPreset(preset);
        }

        public void SetMicroShimmerRate(float rate)
        {
            if (hexagonMode != null)
            {
                hexagonMode.SetComfortPreset(currentComfort);
            }
        }

        public void RequestMoreSourceDensity()
        {
            if (modeManager != null) modeManager.RequestMoreDensity();
            else activeMode?.RequestMoreDensity();
        }

        public void ToggleAdaptiveQuality()
        {
            adaptiveQualityEnabled = !adaptiveQualityEnabled;
        }

        private IKaleidoscopeSourceMode ResolveMode(KaleidoscopeSourceModeKind mode)
        {
            switch (mode)
            {
                case KaleidoscopeSourceModeKind.Hexagons:
                case KaleidoscopeSourceModeKind.PolygonGeometry:
                case KaleidoscopeSourceModeKind.ProceduralColorBlobs:
                case KaleidoscopeSourceModeKind.ColoredGlassPhysical:
                    return hexagonMode;
                case KaleidoscopeSourceModeKind.ImageWallpaper:
                    return imageWallpaperMode;
                case KaleidoscopeSourceModeKind.LiquidIllusion:
                case KaleidoscopeSourceModeKind.Experimental:
                    return liquidMode;
                default:
                    return gemstoneMode;
            }
        }

        private void EnsureModes()
        {
            if (gemstoneMode == null)
            {
                gemstoneMode = gameObject.AddComponent<GemstoneSourceMode>();
                gemstoneMode.Configure(renderPipeline);
            }

            if (hexagonMode == null)
            {
                hexagonMode = gameObject.AddComponent<HexagonSourceMode>();
            }

            if (imageWallpaperMode == null)
            {
                imageWallpaperMode = gameObject.AddComponent<ImageWallpaperSourceMode>();
            }

            if (liquidMode == null)
            {
                liquidMode = gameObject.AddComponent<LiquidSourceMode>();
            }

            gemstoneMode.Configure(renderPipeline);
            gemstoneMode.Initialize();
            hexagonMode.Initialize();
            imageWallpaperMode.Initialize();
            liquidMode.Initialize();
            gemstoneMode.enabled = false;
            hexagonMode.enabled = false;
            imageWallpaperMode.enabled = false;
            liquidMode.enabled = false;
        }

        private void EnsureManager()
        {
            if (modeManager == null)
            {
                modeManager = GetComponent<KaleidoscopeSourceModeManager>();
            }
        }

        private KaleidoscopeSourceModeKind NormalizeMode(KaleidoscopeSourceModeKind mode)
        {
            return mode == KaleidoscopeSourceModeKind.Hexagons ? KaleidoscopeSourceModeKind.PolygonGeometry : mode;
        }
    }
}
