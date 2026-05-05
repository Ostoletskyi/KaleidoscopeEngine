using UnityEngine;

namespace KaleidoscopeEngine.UI
{
    public enum KaleidoscopeLaunchSourceMode
    {
        MainKaleidoscope,
        UserImage
    }

    public enum KaleidoscopeResolutionPreset
    {
        HD,
        FullHD,
        TwoK,
        FourK,
        EightK
    }

    public enum KaleidoscopeWindowMode
    {
        Windowed,
        Fullscreen
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeRuntimeConfig : MonoBehaviour
    {
        [SerializeField] private KaleidoscopeLaunchSourceMode launchSourceMode = KaleidoscopeLaunchSourceMode.MainKaleidoscope;
        [SerializeField] private KaleidoscopeResolutionPreset resolutionPreset = KaleidoscopeResolutionPreset.FullHD;
        [SerializeField] private KaleidoscopeWindowMode windowMode = KaleidoscopeWindowMode.Windowed;
        [SerializeField] private string audioTrackPath;
        [SerializeField] private string audioFolderPath;
        [SerializeField] private string[] audioTrackPaths;
        [SerializeField] private string imageFilePath;
        [SerializeField] private string imageFolderPath;
        [SerializeField] private string[] imagePaths;
        [SerializeField] private Texture2D[] imageTextures;

        public static KaleidoscopeRuntimeConfig Current { get; private set; }

        public KaleidoscopeLaunchSourceMode LaunchSourceMode => launchSourceMode;
        public KaleidoscopeResolutionPreset ResolutionPreset => resolutionPreset;
        public KaleidoscopeWindowMode WindowMode => windowMode;
        public string AudioTrackPath => audioTrackPath;
        public string AudioFolderPath => audioFolderPath;
        public string[] AudioTrackPaths => audioTrackPaths;
        public string ImageFilePath => imageFilePath;
        public string ImageFolderPath => imageFolderPath;
        public string[] ImagePaths => imagePaths;
        public Texture2D[] ImageTextures => imageTextures;
        public bool HasImageSelection => imageTextures != null && imageTextures.Length > 0;

        private void Awake()
        {
            Current = this;
        }

        public void SetLaunchSourceMode(KaleidoscopeLaunchSourceMode mode)
        {
            launchSourceMode = mode;
        }

        public void SetResolutionPreset(KaleidoscopeResolutionPreset preset)
        {
            resolutionPreset = preset;
        }

        public void SetWindowMode(KaleidoscopeWindowMode mode)
        {
            windowMode = mode;
        }

        public void SetAudioTrack(string path)
        {
            audioTrackPath = path;
            audioFolderPath = string.Empty;
            audioTrackPaths = string.IsNullOrWhiteSpace(path) ? null : new[] { path };
        }

        public void SetAudioFolder(string folderPath, string[] trackPaths)
        {
            audioFolderPath = folderPath;
            audioTrackPath = string.Empty;
            audioTrackPaths = trackPaths;
        }

        public void SetImageFile(string path, Texture2D texture)
        {
            ReleaseOwnedImageTextures();
            imageFilePath = path;
            imageFolderPath = string.Empty;
            imagePaths = texture != null ? new[] { path } : null;
            imageTextures = texture != null ? new[] { texture } : null;
            launchSourceMode = KaleidoscopeLaunchSourceMode.UserImage;
        }

        public void SetImageFolder(string folderPath, string[] paths, Texture2D[] textures)
        {
            ReleaseOwnedImageTextures();
            imageFilePath = string.Empty;
            imageFolderPath = folderPath;
            imagePaths = paths;
            imageTextures = textures;
            launchSourceMode = KaleidoscopeLaunchSourceMode.UserImage;
        }

        public void ApplyResolution()
        {
            Vector2Int size = ResolveResolution(resolutionPreset);
            FullScreenMode screenMode = windowMode == KaleidoscopeWindowMode.Fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
            Screen.SetResolution(size.x, size.y, screenMode);
        }

        public static Vector2Int ResolveResolution(KaleidoscopeResolutionPreset preset)
        {
            switch (preset)
            {
                case KaleidoscopeResolutionPreset.HD:
                    return new Vector2Int(1280, 720);
                case KaleidoscopeResolutionPreset.TwoK:
                    return new Vector2Int(2560, 1440);
                case KaleidoscopeResolutionPreset.FourK:
                    return new Vector2Int(3840, 2160);
                case KaleidoscopeResolutionPreset.EightK:
                    return new Vector2Int(7680, 4320);
                default:
                    return new Vector2Int(1920, 1080);
            }
        }

        private void ReleaseOwnedImageTextures()
        {
            if (imageTextures == null)
            {
                return;
            }

            for (int i = 0; i < imageTextures.Length; i++)
            {
                Texture2D texture = imageTextures[i];
                if (texture == null)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(texture);
                }
                else
#endif
                {
                    Destroy(texture);
                }
            }
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
            }
        }
    }
}
