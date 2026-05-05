using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using KaleidoscopeEngine.Audio;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using KaleidoscopeEngine.Source;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KaleidoscopeEngine.UI
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeLauncherUI : MonoBehaviour
    {
        [SerializeField] private KaleidoscopeRuntimeConfig runtimeConfig;
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;
        [SerializeField] private KaleidoscopeSourceModeManager sourceModeManager;
        [SerializeField] private KaleidoscopeSourceLibrary sourceLibrary;
        [SerializeField] private KaleidoscopeHelpOverlay helpOverlay;
        [SerializeField] private KaleidoscopeInputRouter inputRouter;
        [SerializeField] private KaleidoscopeDebugPanel debugPanel;
        [SerializeField] private bool menuVisible = true;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioReactiveDirector audioReactiveDirector;

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle labelStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle buttonStyle;
        private string statusMessage = "Ready.";
        private float previousTimeScale = 1f;
        private bool preStartPrepared;
        private bool runtimeStarted;
        private readonly List<AudioClip> playlist = new List<AudioClip>();
        private int playlistIndex = -1;

        public bool MenuVisible => menuVisible;
        public AudioSource ActiveAudioSource => audioSource;

        public void Configure(
            KaleidoscopeRuntimeConfig config,
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopeSourceModeController sourceController,
            KaleidoscopeSourceModeManager modeManager,
            KaleidoscopeSourceLibrary library,
            KaleidoscopeHelpOverlay overlay,
            KaleidoscopeInputRouter router,
            KaleidoscopeDebugPanel panel)
        {
            runtimeConfig = config;
            renderPipeline = pipeline;
            sourceModeController = sourceController;
            sourceModeManager = modeManager;
            sourceLibrary = library;
            helpOverlay = overlay;
            inputRouter = router;
            debugPanel = panel;
            PreparePreStartMenu();
        }

        public void ConfigureAudioReactiveDirector(AudioReactiveDirector director)
        {
            audioReactiveDirector = director;
            if (audioSource != null)
            {
                audioReactiveDirector?.SetAudioSource(audioSource);
            }
        }

        private void Awake()
        {
            if (runtimeConfig == null)
            {
                runtimeConfig = FindObjectOfType<KaleidoscopeRuntimeConfig>();
            }
        }

        private void Update()
        {
            if (!runtimeStarted && Input.GetMouseButtonDown(2))
            {
                Toggle();
            }

            UpdateAudioPlaylist();

            if (!menuVisible)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                helpOverlay?.Toggle();
            }
        }

        private void OnGUI()
        {
            if (!menuVisible)
            {
                return;
            }

            EnsureStyles();
            DrawMenu();
        }

        private void PreparePreStartMenu()
        {
            if (preStartPrepared)
            {
                return;
            }

            preStartPrepared = true;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (inputRouter != null)
            {
                inputRouter.enabled = false;
            }

            sourceModeManager?.PausePhysicalSource();
        }

        public void Toggle()
        {
            menuVisible = !menuVisible;
            if (!runtimeStarted && menuVisible)
            {
                PreparePreStartMenu();
            }

            statusMessage = menuVisible ? "Menu opened." : "Menu closed.";
        }

        private void DrawMenu()
        {
            float width = Mathf.Min(760f, Screen.width - 64f);
            float height = Mathf.Min(720f, Screen.height - 48f);
            Rect area = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUILayout.BeginArea(area, panelStyle);
            GUILayout.Label("KALEIDOSCOPE LAUNCHER", titleStyle);
            GUILayout.Space(8f);

            DrawSection("Audio");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Audio Track", buttonStyle, GUILayout.Height(30f))) SelectAudioTrack();
            if (GUILayout.Button("Select Audio Folder", buttonStyle, GUILayout.Height(30f))) SelectAudioFolder();
            GUILayout.EndHorizontal();
            GUILayout.Label(ResolveAudioSummary(), labelStyle);

            DrawSection("Source Mode");
            GUILayout.BeginHorizontal();
            DrawModeButton("Main Kaleidoscope", KaleidoscopeLaunchSourceMode.MainKaleidoscope);
            DrawModeButton("User Image", KaleidoscopeLaunchSourceMode.UserImage);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Image File", buttonStyle, GUILayout.Height(30f))) SelectImageFile();
            if (GUILayout.Button("Select Image Folder", buttonStyle, GUILayout.Height(30f))) SelectImageFolder();
            GUILayout.EndHorizontal();
            GUILayout.Label(ResolveImageSummary(), labelStyle);

            DrawSection("Resolution");
            GUILayout.BeginHorizontal();
            DrawResolutionButton("HD", KaleidoscopeResolutionPreset.HD);
            DrawResolutionButton("Full HD", KaleidoscopeResolutionPreset.FullHD);
            DrawResolutionButton("2K", KaleidoscopeResolutionPreset.TwoK);
            DrawResolutionButton("4K", KaleidoscopeResolutionPreset.FourK);
            DrawResolutionButton("8K", KaleidoscopeResolutionPreset.EightK);
            GUILayout.EndHorizontal();

            DrawSection("Window");
            GUILayout.BeginHorizontal();
            DrawWindowButton("Windowed", KaleidoscopeWindowMode.Windowed);
            DrawWindowButton("Fullscreen", KaleidoscopeWindowMode.Fullscreen);
            GUILayout.EndHorizontal();

            GUILayout.Space(12f);
            GUILayout.Label(statusMessage, labelStyle);
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Help", buttonStyle, GUILayout.Height(36f))) helpOverlay?.Toggle();
            if (GUILayout.Button("Start", selectedButtonStyle, GUILayout.Height(36f))) StartRuntime();
            if (GUILayout.Button("Exit", buttonStyle, GUILayout.Height(36f))) ExitRuntime();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSection(string label)
        {
            GUILayout.Space(12f);
            GUILayout.Label(label, sectionStyle);
        }

        private void DrawModeButton(string label, KaleidoscopeLaunchSourceMode mode)
        {
            GUIStyle style = runtimeConfig != null && runtimeConfig.LaunchSourceMode == mode ? selectedButtonStyle : buttonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(30f)))
            {
                runtimeConfig?.SetLaunchSourceMode(mode);
            }
        }

        private void DrawResolutionButton(string label, KaleidoscopeResolutionPreset preset)
        {
            GUIStyle style = runtimeConfig != null && runtimeConfig.ResolutionPreset == preset ? selectedButtonStyle : buttonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(30f)))
            {
                runtimeConfig?.SetResolutionPreset(preset);
                Vector2Int size = KaleidoscopeRuntimeConfig.ResolveResolution(preset);
                statusMessage = $"Resolution selected: {size.x}x{size.y}";
            }
        }

        private void DrawWindowButton(string label, KaleidoscopeWindowMode mode)
        {
            GUIStyle style = runtimeConfig != null && runtimeConfig.WindowMode == mode ? selectedButtonStyle : buttonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(30f)))
            {
                runtimeConfig?.SetWindowMode(mode);
                statusMessage = $"Window mode selected: {label}";
            }
        }

        private void SelectAudioTrack()
        {
            if (KaleidoscopeFileSelectionUtility.TrySelectAudioFile(out string path, out string message))
            {
                runtimeConfig?.SetAudioTrack(path);
                statusMessage = $"Audio track: {Path.GetFileName(path)}";
                return;
            }

            statusMessage = string.IsNullOrEmpty(message) ? "Audio track selection cancelled." : message;
        }

        private void SelectAudioFolder()
        {
            if (KaleidoscopeFileSelectionUtility.TrySelectAudioFolder(out string folderPath, out string[] audioPaths, out string message))
            {
                runtimeConfig?.SetAudioFolder(folderPath, audioPaths);
                statusMessage = message;
                return;
            }

            statusMessage = string.IsNullOrEmpty(message) ? "Audio folder selection cancelled." : message;
        }

        private void SelectImageFile()
        {
            if (KaleidoscopeFileSelectionUtility.TrySelectImageFile(out string path, out Texture2D texture, out string message))
            {
                runtimeConfig?.SetImageFile(path, texture);
                sourceModeController?.RecordImageDiskRead(path);
                sourceModeController?.RecordImageTextureCreate("Launcher image file");
                statusMessage = $"Image file: {Path.GetFileName(path)}";
                return;
            }

            statusMessage = string.IsNullOrEmpty(message) ? "Image selection cancelled." : message;
        }

        private void SelectImageFolder()
        {
            if (KaleidoscopeFileSelectionUtility.TrySelectImageFolder(out string folderPath, out string[] imagePaths, out Texture2D[] textures, out string message))
            {
                runtimeConfig?.SetImageFolder(folderPath, imagePaths, textures);
                for (int i = 0; i < imagePaths.Length; i++)
                {
                    sourceModeController?.RecordImageDiskRead(imagePaths[i]);
                }

                for (int i = 0; i < textures.Length; i++)
                {
                    sourceModeController?.RecordImageTextureCreate("Launcher image folder preload");
                }

                statusMessage = message;
                return;
            }

            statusMessage = string.IsNullOrEmpty(message) ? "Image folder selection cancelled." : message;
        }

        private void StartRuntime()
        {
            if (runtimeConfig == null)
            {
                statusMessage = "Runtime config missing.";
                return;
            }

            runtimeConfig.ApplyResolution();
            Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
            if (runtimeConfig.LaunchSourceMode == KaleidoscopeLaunchSourceMode.UserImage)
            {
                sourceModeController?.SetImageSourceTextures(runtimeConfig.ImageTextures, runtimeConfig.ImagePaths);
                sourceModeController?.SetMode(KaleidoscopeSourceModeKind.ImageWallpaper);
            }
            else
            {
                sourceLibrary?.SetCategory(KaleidoscopeSourceCategory.TransparentGemstones);
                sourceModeController?.SetMode(KaleidoscopeSourceModeKind.Gemstones);
                sourceModeManager?.ResumePhysicalSource();
            }

            renderPipeline?.ReturnToKaleidoscopeView();
            if (inputRouter != null)
            {
                inputRouter.enabled = true;
            }

            runtimeStarted = true;
            menuVisible = false;
            StartAudioRuntime();
            debugPanel?.PostOperatorMessage("Launcher Start");
        }

        private void StartAudioRuntime()
        {
            playlist.Clear();
            playlistIndex = -1;
            EnsureAudioSource();
            if (audioSource == null || runtimeConfig == null)
            {
                return;
            }

            audioReactiveDirector?.SetAudioSource(audioSource);
            audioSource.Stop();
            audioSource.clip = null;
            string singlePath = runtimeConfig.AudioTrackPath;
            if (!string.IsNullOrWhiteSpace(singlePath))
            {
                StartCoroutine(LoadSingleAudio(singlePath));
                return;
            }

            string[] paths = runtimeConfig.AudioTrackPaths;
            if (paths != null && paths.Length > 0)
            {
                StartCoroutine(LoadPlaylist(paths));
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioReactiveDirector?.SetAudioSource(audioSource);
        }

        private IEnumerator LoadSingleAudio(string path)
        {
            bool loaded = false;
            yield return LoadAudioClip(path, clip =>
            {
                if (clip == null || audioSource == null)
                {
                    return;
                }

                loaded = true;
                audioSource.loop = true;
                audioSource.clip = clip;
                audioSource.Play();
                statusMessage = $"Audio playing: {Path.GetFileName(path)}";
            });

            if (!loaded)
            {
                statusMessage = $"Audio load failed: {Path.GetFileName(path)}";
            }
        }

        private IEnumerator LoadPlaylist(string[] paths)
        {
            playlist.Clear();
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                yield return LoadAudioClip(path, clip =>
                {
                    if (clip != null)
                    {
                        playlist.Add(clip);
                    }
                });
            }

            if (playlist.Count == 0)
            {
                statusMessage = "Audio playlist is empty.";
                yield break;
            }

            PlayPlaylistIndex(0);
            statusMessage = $"Audio playlist: {playlist.Count} track(s)";
        }

        private IEnumerator LoadAudioClip(string path, Action<AudioClip> onLoaded)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                onLoaded?.Invoke(null);
                yield break;
            }

            string url = new Uri(path).AbsoluteUri;
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, ResolveAudioType(path)))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onLoaded?.Invoke(null);
                    yield break;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                {
                    clip.name = Path.GetFileNameWithoutExtension(path);
                }

                onLoaded?.Invoke(clip);
            }
        }

        private void UpdateAudioPlaylist()
        {
            if (!runtimeStarted || audioSource == null || playlist.Count <= 1 || audioSource.isPlaying)
            {
                return;
            }

            PlayPlaylistIndex((playlistIndex + 1) % playlist.Count);
        }

        private void PlayPlaylistIndex(int index)
        {
            if (audioSource == null || playlist.Count == 0)
            {
                return;
            }

            playlistIndex = Mathf.Clamp(index, 0, playlist.Count - 1);
            audioSource.loop = false;
            audioSource.clip = playlist[playlistIndex];
            audioSource.Play();
        }

        private static AudioType ResolveAudioType(string path)
        {
            string extension = Path.GetExtension(path);
            if (string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.WAV;
            }

            if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.MPEG;
            }

            if (string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.OGGVORBIS;
            }

            if (string.Equals(extension, ".aiff", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".aif", StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.AIFF;
            }

            return AudioType.UNKNOWN;
        }

        private void ExitRuntime()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private string ResolveAudioSummary()
        {
            if (runtimeConfig == null)
            {
                return "Audio: none";
            }

            if (!string.IsNullOrEmpty(runtimeConfig.AudioTrackPath))
            {
                return $"Audio: {Path.GetFileName(runtimeConfig.AudioTrackPath)}";
            }

            if (runtimeConfig.AudioTrackPaths != null && runtimeConfig.AudioTrackPaths.Length > 0)
            {
                return $"Audio folder: {runtimeConfig.AudioTrackPaths.Length} track(s)";
            }

            return "Audio: none selected";
        }

        private string ResolveImageSummary()
        {
            if (runtimeConfig == null || !runtimeConfig.HasImageSelection)
            {
                return "Image source: none selected; fallback source will be used in image mode.";
            }

            if (!string.IsNullOrEmpty(runtimeConfig.ImageFilePath))
            {
                return $"Image file: {Path.GetFileName(runtimeConfig.ImageFilePath)}";
            }

            return $"Image folder: {runtimeConfig.ImageTextures.Length} image(s)";
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            Texture2D background = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            background.SetPixel(0, 0, new Color(0.025f, 0.035f, 0.045f, 0.94f));
            background.Apply();

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(24, 24, 20, 20),
                normal = { background = background }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.84f, 0.96f, 1f, 1f) }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.62f, 0.86f, 1f, 1f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.9f, 0.94f, 0.92f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            selectedButtonStyle = new GUIStyle(buttonStyle)
            {
                normal = { textColor = new Color(0.7f, 0.95f, 1f, 1f) },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };
        }
    }
}
