using KaleidoscopeEngine.Mirrors;
using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    public enum KaleidoscopeImageTransitionMode
    {
        Crossfade,
        Dissolve,
        Slide,
        FilmRoll
    }

    [DisallowMultipleComponent]
    public sealed class ImageWallpaperSourceMode : ProceduralSourceTextureMode
    {
        [SerializeField] private Texture2D[] sourceTextures;
        [SerializeField] private string[] cachedImagePaths;
        [SerializeField] private float imageScrollSpeed = 0.035f;
        [SerializeField] private float imageZoomSpeed = 0.08f;
        [SerializeField] private float imageRotationSpeed = 0.025f;
        [SerializeField] private float imageChangeInterval = 30f;
        [SerializeField] private float imageTransitionDuration = 2f;
        [SerializeField] private KaleidoscopeImageTransitionMode imageTransitionMode = KaleidoscopeImageTransitionMode.Crossfade;
        [SerializeField] private Vector2 panOffset;
        [SerializeField] private float zoom = 1.08f;
        [SerializeField] private bool tiling = true;
        [SerializeField] private Color colorTint = Color.white;
        [SerializeField, Range(0.25f, 2f)] private float contrast = 1f;
        [SerializeField, Range(-0.5f, 0.5f)] private float brightness;
        [SerializeField] private int maxImageTextureSize = 2048;

        private KaleidoscopeMirrorController mirrorController;
        private Texture2D[] preparedTextures;
        private bool[] ownsPreparedTextures;
        private Color[] pixelBuffer;
        private float phase;
        private float nextImageChangeTime;
        private float transitionTimer;
        private int activeIndex;
        private int nextIndex = -1;
        private bool transitionActive;
        private int imageReloadCount;
        private int textureCreateCount;
        private int imageDiskReadCount;
        private string lastImagePipelineEvent = "No image loaded.";
        private float lastImagePipelineEventTime;

        public override string GetSourceModeName() => "Image Wallpaper";

        public int ImageTextureResolution
        {
            get
            {
                Texture2D active = ActivePreparedTexture;
                return active != null ? Mathf.Max(active.width, active.height) : texture != null ? Mathf.Max(texture.width, texture.height) : 0;
            }
        }

        public float ImageMemoryMB
        {
            get
            {
                float bytes = 0f;
                if (preparedTextures != null)
                {
                    for (int i = 0; i < preparedTextures.Length; i++)
                    {
                        Texture2D prepared = preparedTextures[i];
                        if (prepared != null)
                        {
                            bytes += prepared.width * prepared.height * 4f;
                        }
                    }
                }

                return bytes / (1024f * 1024f);
            }
        }

        public int ImageReloadCount => imageReloadCount;
        public int TextureCreateCount => textureCreateCount;
        public int ImageDiskReadCount => imageDiskReadCount;
        public string CachedImagePath => cachedImagePaths != null && cachedImagePaths.Length > 0 ? cachedImagePaths[Mathf.Clamp(activeIndex, 0, cachedImagePaths.Length - 1)] : string.Empty;
        public int CachedImagePathCount => cachedImagePaths != null ? cachedImagePaths.Length : 0;
        public string ActiveImageName => ActivePreparedTexture != null ? ActivePreparedTexture.name : "Fallback Pattern";
        public string LastImagePipelineEvent => lastImagePipelineEvent;
        public float TimeSinceLastImagePipelineEvent => Time.realtimeSinceStartup - lastImagePipelineEventTime;
        public int MaxImageTextureSize => maxImageTextureSize;
        public float ImageScrollSpeed => imageScrollSpeed;
        public float ImageZoomSpeed => imageZoomSpeed;
        public float ImageRotationSpeed => imageRotationSpeed;
        public float ImageChangeInterval => imageChangeInterval;
        public float ImageTransitionDuration => imageTransitionDuration;
        public string ImageTransitionModeName => imageTransitionMode.ToString();
        public float ImageTransitionProgress => transitionActive ? Mathf.Clamp01(transitionTimer / Mathf.Max(0.05f, imageTransitionDuration)) : 0f;

        private Texture2D ActivePreparedTexture => PreparedTextureAt(activeIndex);

        public void Configure(KaleidoscopeMirrorController mirror)
        {
            mirrorController = mirror;
        }

        public void SetTextures(Texture2D[] textures)
        {
            SetTextures(textures, null);
        }

        public void SetTextures(Texture2D[] textures, string[] imagePaths)
        {
            if (SameTextureArray(sourceTextures, textures) && SameStringArray(cachedImagePaths, imagePaths))
            {
                return;
            }

            ReleasePreparedTexturesIfOwned();
            sourceTextures = textures;
            cachedImagePaths = imagePaths;
            preparedTextures = null;
            ownsPreparedTextures = null;
            activeIndex = 0;
            nextIndex = -1;
            phase = 0f;
            transitionTimer = 0f;
            transitionActive = false;
            PrepareAllSourceTextures();
            ScheduleNextImageChange();
            UpdateMirrorPlaybackState(true);
        }

        public void SetImageMotion(
            float scrollSpeed,
            float zoomSpeed,
            float rotationSpeed,
            float changeInterval,
            float transitionDuration,
            KaleidoscopeImageTransitionMode transitionMode)
        {
            imageScrollSpeed = Mathf.Max(0f, scrollSpeed);
            imageZoomSpeed = Mathf.Max(0f, zoomSpeed);
            imageRotationSpeed = rotationSpeed;
            imageChangeInterval = Mathf.Max(0.5f, changeInterval);
            imageTransitionDuration = Mathf.Max(0.05f, transitionDuration);
            imageTransitionMode = transitionMode;
            UpdateMirrorPlaybackState(false);
        }

        public void RecordExternalImageDiskRead(string path)
        {
            imageDiskReadCount++;
            RecordImageEvent($"Disk read: {path}");
        }

        public void RecordExternalTextureCreate(string reason)
        {
            textureCreateCount++;
            RecordImageEvent($"Texture created: {reason}");
        }

        public override void Activate()
        {
            Initialize();
            enabled = true;
            PrepareAllSourceTextures();
            EnsureTexture();
            ScheduleNextImageChange();
            UpdateMirrorPlaybackState(true);
        }

        public override void Tick(float deltaTime)
        {
            phase += Mathf.Max(0f, deltaTime);
            if (PreparedTextureCount > 1)
            {
                UpdateImageSequence(deltaTime);
            }

            if (ActivePreparedTexture == null)
            {
                base.Tick(deltaTime);
            }

            UpdateMirrorPlaybackState(false);
        }

        public override Texture GetSourceTexture()
        {
            Texture2D active = ActivePreparedTexture;
            if (active != null)
            {
                return active;
            }

            EnsureTexture();
            return texture;
        }

        public override void RandomizeMode()
        {
            if (PreparedTextureCount > 0)
            {
                activeIndex = random.Next(0, PreparedTextureCount);
                ScheduleNextImageChange();
            }

            zoom = Mathf.Lerp(0.96f, 1.24f, (float)random.NextDouble());
            imageScrollSpeed = Mathf.Lerp(0.018f, 0.065f, (float)random.NextDouble());
            imageZoomSpeed = Mathf.Lerp(0.035f, 0.12f, (float)random.NextDouble());
            imageRotationSpeed = Mathf.Lerp(-0.035f, 0.035f, (float)random.NextDouble());
            UpdateMirrorPlaybackState(false);
        }

        public override void SetQualityLevel(KaleidoscopeQualityLevel qualityLevel)
        {
            // User images are cached once and sampled directly by the mirror shader.
            // Render quality changes must not recreate user image textures.
        }

        protected override void GenerateTexture(float deltaTime, bool force)
        {
            if (ActivePreparedTexture != null)
            {
                return;
            }

            EnsureTexture();
            int pixelCount = textureSize * textureSize;
            if (pixelBuffer == null || pixelBuffer.Length != pixelCount)
            {
                pixelBuffer = new Color[pixelCount];
            }

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 uv = new Vector2(
                        x / (float)(textureSize - 1),
                        y / (float)(textureSize - 1));
                    Color color = FallbackPattern(uv + panOffset + new Vector2(0f, phase * imageScrollSpeed));
                    color *= colorTint;
                    color = new Color(
                        Mathf.Clamp01((color.r - 0.5f) * contrast + 0.5f + brightness),
                        Mathf.Clamp01((color.g - 0.5f) * contrast + 0.5f + brightness),
                        Mathf.Clamp01((color.b - 0.5f) * contrast + 0.5f + brightness),
                        1f);
                    pixelBuffer[y * textureSize + x] = color;
                }
            }

            texture.SetPixels(pixelBuffer);
            texture.Apply(false);
        }

        private void UpdateImageSequence(float deltaTime)
        {
            if (!transitionActive && Time.unscaledTime >= nextImageChangeTime)
            {
                BeginTransition();
            }

            if (!transitionActive)
            {
                return;
            }

            transitionTimer += Mathf.Max(0f, deltaTime);
            if (transitionTimer < imageTransitionDuration)
            {
                return;
            }

            activeIndex = Mathf.Clamp(nextIndex, 0, PreparedTextureCount - 1);
            nextIndex = -1;
            transitionTimer = 0f;
            transitionActive = false;
            ScheduleNextImageChange();
            RecordImageEvent($"Image active: {ActiveImageName}");
        }

        private void BeginTransition()
        {
            if (PreparedTextureCount <= 1)
            {
                return;
            }

            nextIndex = (activeIndex + 1) % PreparedTextureCount;
            transitionTimer = 0f;
            transitionActive = true;
            Texture2D nextTexture = PreparedTextureAt(nextIndex);
            RecordImageEvent($"Image transition: {ActiveImageName} -> {(nextTexture != null ? nextTexture.name : "Fallback Pattern")}");
        }

        private void UpdateMirrorPlaybackState(bool force)
        {
            if (mirrorController == null && !force)
            {
                return;
            }

            Texture secondary = transitionActive ? PreparedTextureAt(nextIndex) : ActivePreparedTexture;
            float progress = ImageTransitionProgress;
            float zoomPulse = zoom * (1f + Mathf.Sin(phase * Mathf.PI * 2f * imageZoomSpeed) * 0.055f);
            Vector2 offset = panOffset + new Vector2(
                Mathf.Sin(phase * 0.17f) * 0.035f,
                phase * imageScrollSpeed);
            float rotation = phase * imageRotationSpeed;

            mirrorController?.SetImageSourcePlayback(
                secondary != null ? secondary : ActivePreparedTexture,
                offset,
                zoomPulse,
                rotation,
                progress,
                (int)imageTransitionMode,
                imageScrollSpeed,
                imageZoomSpeed,
                imageRotationSpeed,
                imageChangeInterval,
                imageTransitionDuration);
        }

        private void ScheduleNextImageChange()
        {
            nextImageChangeTime = Time.unscaledTime + Mathf.Max(0.5f, imageChangeInterval);
        }

        private int PreparedTextureCount => preparedTextures != null ? preparedTextures.Length : 0;

        private Texture2D PreparedTextureAt(int index)
        {
            if (preparedTextures == null || preparedTextures.Length == 0)
            {
                return null;
            }

            return preparedTextures[Mathf.Clamp(index, 0, preparedTextures.Length - 1)];
        }

        private void PrepareAllSourceTextures()
        {
            if (preparedTextures != null || sourceTextures == null || sourceTextures.Length == 0)
            {
                return;
            }

            preparedTextures = new Texture2D[sourceTextures.Length];
            ownsPreparedTextures = new bool[sourceTextures.Length];
            for (int i = 0; i < sourceTextures.Length; i++)
            {
                Texture2D input = sourceTextures[i];
                if (input == null)
                {
                    continue;
                }

                preparedTextures[i] = PrepareTexture(input, out ownsPreparedTextures[i]);
                imageReloadCount++;
            }

            RecordImageEvent($"Image list cached: {PreparedTextureCount} texture(s)");
        }

        private Texture2D PrepareTexture(Texture2D input, out bool ownsTexture)
        {
            ownsTexture = false;
            int maxSize = Mathf.Max(256, maxImageTextureSize);
            int sourceMax = Mathf.Max(input.width, input.height);
            if (sourceMax <= maxSize)
            {
                input.wrapMode = tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                input.filterMode = FilterMode.Trilinear;
                input.anisoLevel = Mathf.Max(input.anisoLevel, 2);
                return input;
            }

            float scale = maxSize / (float)sourceMax;
            int width = Mathf.Max(1, Mathf.RoundToInt(input.width * scale));
            int height = Mathf.Max(1, Mathf.RoundToInt(input.height * scale));
            RenderTexture temporary = null;
            RenderTexture previous = RenderTexture.active;

            try
            {
                temporary = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(input, temporary);
                RenderTexture.active = temporary;

                Texture2D downscaled = new Texture2D(width, height, TextureFormat.RGBA32, true, QualitySettings.activeColorSpace == ColorSpace.Linear)
                {
                    name = $"{input.name} Cached {width}x{height}",
                    wrapMode = tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 2
                };
                downscaled.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                downscaled.Apply(true, false);
                textureCreateCount++;
                ownsTexture = true;
                return downscaled;
            }
            finally
            {
                RenderTexture.active = previous;
                if (temporary != null)
                {
                    RenderTexture.ReleaseTemporary(temporary);
                }
            }
        }

        private void ReleasePreparedTexturesIfOwned()
        {
            if (preparedTextures == null || ownsPreparedTextures == null)
            {
                return;
            }

            for (int i = 0; i < preparedTextures.Length; i++)
            {
                if (!ownsPreparedTextures[i] || preparedTextures[i] == null)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(preparedTextures[i]);
                }
                else
#endif
                {
                    Destroy(preparedTextures[i]);
                }
            }

            preparedTextures = null;
            ownsPreparedTextures = null;
        }

        private void OnDestroy()
        {
            ReleasePreparedTexturesIfOwned();
        }

        private void RecordImageEvent(string message)
        {
            lastImagePipelineEvent = message;
            lastImagePipelineEventTime = Time.realtimeSinceStartup;
        }

        private static bool SameTextureArray(Texture2D[] a, Texture2D[] b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameStringArray(string[] a, string[] b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static Color FallbackPattern(Vector2 uv)
        {
            float wave = Mathf.Sin(uv.x * 31f) * Mathf.Sin(uv.y * 23f);
            return Color.HSVToRGB(
                Mathf.Repeat(wave * 0.08f + uv.x * 0.15f + 0.58f, 1f),
                0.45f,
                0.72f);
        }
    }
}
