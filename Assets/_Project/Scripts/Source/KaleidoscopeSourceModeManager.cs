using System.Collections.Generic;
using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Comfort;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.Performance;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;
using Unity.Profiling;

namespace KaleidoscopeEngine.Source
{
    public enum KaleidoscopeSourceType
    {
        PhysicalGemstoneSource,
        TextureSource,
        ProceduralPatternSource,
        LiquidShaderSource,
        HybridSource
    }

    public enum KaleidoscopeSourceTransitionMode
    {
        Instant,
        Fade,
        Crossfade
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeSourceModeManager : MonoBehaviour
    {
        private struct TimedImageModeEvent
        {
            public readonly float Time;
            public readonly string Message;

            public TimedImageModeEvent(float time, string message)
            {
                Time = time;
                Message = message;
            }
        }

        [Header("References")]
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopePhysicsChamber physicsChamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private KaleidoscopeDebugPanel statusPanel;
        [SerializeField] private Transform physicalTubeRoot;
        [SerializeField] private OpticalSourceChamber opticalSourceChamber;
        [SerializeField] private GemSparkleController sparkleController;
        [SerializeField] private FakeCausticBunnyProjector causticProjector;

        [Header("Switching")]
        [SerializeField] private KaleidoscopeSourceTransitionMode sourceTransitionMode = KaleidoscopeSourceTransitionMode.Fade;
        [SerializeField, Range(0.05f, 2f)] private float sourceSwitchCooldownSeconds = 0.25f;
        [SerializeField] private bool removePhysicalCenterMaskForTextureSources = true;
        [SerializeField] private bool disablePhysicalCenterArtifactsForNonPhysicalSources = true;

        private readonly Dictionary<KaleidoscopeSourceModeKind, IKaleidoscopeSourceMode> modes = new Dictionary<KaleidoscopeSourceModeKind, IKaleidoscopeSourceMode>();
        private readonly List<string> layerAuditLog = new List<string>();
        private readonly List<string> physicalArtifactErrors = new List<string>();
        private IKaleidoscopeSourceMode activeMode;
        private KaleidoscopeSourceModeKind activeModeKind = KaleidoscopeSourceModeKind.Gemstones;
        private KaleidoscopeQualityLevel currentQuality = KaleidoscopeQualityLevel.High;
        private ViewerComfortPreset currentComfort = ViewerComfortPreset.Normal;
        private float lastSwitchTime = -100f;
        private bool physicalSourcePaused;
        private bool physicalBodiesSleeping;
        private int imageReloadBaseline;
        private int renderTextureRecreateBaseline;
        private int materialInstanceBaseline;
        private int sourceRebuildBaseline;
        private string lastImageSpikeReport = "No image spike detected.";
        private float nextImageSpikeReportTime;
        private ProfilerRecorder gcAllocatedInFrameRecorder;
        private float imageProfilerFps;
        private float imageProfilerFrameMs;
        private long imageProfilerGcAllocBytes;
        private float imageProfilerElapsed;
        private float imageProfilerMinFps = float.MaxValue;
        private float imageProfilerMaxFrameMs;
        private int imageProfilerSpikeCount;
        private float nextImageModeBudgetActionTime;
        private bool imageSecondaryEffectsDeprioritized;
        private string periodicImageStutterReport = "No periodic image stutter detected.";
        private readonly List<TimedImageModeEvent> imageModeEvents = new List<TimedImageModeEvent>();
        private readonly List<float> imageSpikeTimes = new List<float>();

        public KaleidoscopeSourceModeKind ActiveModeKind => activeModeKind;
        public string ActiveSourceModeName => activeMode != null ? activeMode.GetSourceModeName() : "None";
        public KaleidoscopeSourceType ActiveSourceType => ResolveSourceType(activeModeKind);
        public KaleidoscopeSourceTransitionMode SourceTransitionMode => sourceTransitionMode;
        public float SourceSwitchCooldownSeconds => sourceSwitchCooldownSeconds;
        public float LastSwitchTime => lastSwitchTime;
        public bool PhysicalSourceActive => !physicalSourcePaused;
        public bool SourceModeUsesTube => ActiveSourceType == KaleidoscopeSourceType.PhysicalGemstoneSource || ActiveSourceType == KaleidoscopeSourceType.HybridSource;
        public bool DirectTextureSource => !SourceModeUsesTube;
        public bool DirectTexturePipeline => DirectTextureSource;
        public bool PhysicalPipeline => SourceModeUsesTube && !physicalSourcePaused;
        public bool PhysicalPipelineActive => PhysicalPipeline;
        public bool SourceCameraActive => renderPipeline != null && renderPipeline.SourceCamera != null && renderPipeline.SourceCamera.enabled;
        public bool PhysicsActive => physicsChamber != null && physicsChamber.AxialRotationEnabled && !physicalBodiesSleeping && !physicalSourcePaused;
        public string CurrentSourceModeCost => ResolveCurrentSourceModeCost();
        public int ActivePhysicsBodyCount => CountActivePhysicsBodies();
        public int ImageReloadCount => CurrentImageMode != null ? CurrentImageMode.ImageReloadCount : 0;
        public int ImageTextureCreateCount => CurrentImageMode != null ? CurrentImageMode.TextureCreateCount : 0;
        public int ImageDiskReadCount => CurrentImageMode != null ? CurrentImageMode.ImageDiskReadCount : 0;
        public string LastImagePipelineEvent => CurrentImageMode != null ? CurrentImageMode.LastImagePipelineEvent : "n/a";
        public string LastImageSpikeReport => lastImageSpikeReport;
        public string PeriodicImageStutterReport => periodicImageStutterReport;
        public string RecentImageModeEvents => BuildRecentImageModeEvents();
        public float ImageModeProfilerFps => imageProfilerFps;
        public float ImageModeProfilerFrameMs => imageProfilerFrameMs;
        public long ImageModeProfilerGcAllocBytes => imageProfilerGcAllocBytes;
        public float ImageModeProfilerElapsed => imageProfilerElapsed;
        public float ImageModeProfilerMinFps => imageProfilerMinFps < float.MaxValue ? imageProfilerMinFps : 0f;
        public float ImageModeProfilerMaxFrameMs => imageProfilerMaxFrameMs;
        public int ImageModeProfilerSpikeCount => imageProfilerSpikeCount;
        public float TimeSinceLastImagePipelineEvent => CurrentImageMode != null ? CurrentImageMode.TimeSinceLastImagePipelineEvent : 0f;
        public int ImageTextureResolution => CurrentImageMode != null ? CurrentImageMode.ImageTextureResolution : 0;
        public float ImageMemoryMB => CurrentImageMode != null ? CurrentImageMode.ImageMemoryMB : 0f;
        public bool UsesPhysicalCenterArtifacts => SourceModeUsesTube;
        public bool UsesPhysicalSourceCamera => SourceModeUsesTube;
        public bool CenterArtifactsActive => UsesPhysicalCenterArtifacts && (mirrorController == null || mirrorController.PhysicalCenterArtifacts);
        public bool CenterCleanEnabled => mirrorController != null && mirrorController.CenterCleanEnabled;
        public int PhysicalArtifactRenderersActive { get; private set; }
        public int CenterArtifactRendererCount { get; private set; }
        public IReadOnlyList<string> LayerAuditLog => layerAuditLog;
        public IReadOnlyList<string> PhysicalArtifactErrors => physicalArtifactErrors;
        public string PipelineWarning => ResolvePipelineWarning();
        public bool RemovePhysicalCenterMaskForTextureSources => removePhysicalCenterMaskForTextureSources;
        public bool DisablePhysicalCenterArtifactsForNonPhysicalSources => disablePhysicalCenterArtifactsForNonPhysicalSources;
        public bool PhysicsPaused => physicalSourcePaused;
        public bool PhysicalBodiesSleeping => physicalBodiesSleeping;
        public Texture CurrentSourceTexture => activeMode?.GetSourceTexture();
        private ImageWallpaperSourceMode CurrentImageMode => ResolveMode(KaleidoscopeSourceModeKind.ImageWallpaper) as ImageWallpaperSourceMode;

        public void Configure(
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopeMirrorController mirror,
            KaleidoscopePhysicsChamber chamber,
            GemstoneSpawner gemstoneSpawner,
            KaleidoscopeDebugPanel panel,
            Transform tubeRoot = null,
            OpticalSourceChamber sourceChamber = null,
            GemSparkleController sparkles = null,
            FakeCausticBunnyProjector caustics = null)
        {
            renderPipeline = pipeline;
            mirrorController = mirror;
            physicsChamber = chamber;
            spawner = gemstoneSpawner;
            statusPanel = panel;
            physicalTubeRoot = tubeRoot;
            opticalSourceChamber = sourceChamber;
            sparkleController = sparkles;
            causticProjector = caustics;
            InitializeModes();
            ConfigureImageMode();
            SwitchTo(KaleidoscopeSourceModeKind.Gemstones, true);
        }

        private void Awake()
        {
            InitializeModes();
            StartGcRecorder();
        }

        private void OnEnable()
        {
            StartGcRecorder();
        }

        private void OnDisable()
        {
            if (gcAllocatedInFrameRecorder.Valid)
            {
                gcAllocatedInFrameRecorder.Dispose();
            }
        }

        private void Update()
        {
            activeMode?.Tick(Time.deltaTime);
            Texture texture = activeMode?.GetSourceTexture();
            if (ActiveSourceType == KaleidoscopeSourceType.PhysicalGemstoneSource || ActiveSourceType == KaleidoscopeSourceType.HybridSource)
            {
                renderPipeline?.ClearExternalSourceTexture();
                mirrorController?.SetDirectTextureSourceRendering(false);
                mirrorController?.SetAnimatedImageSourceRendering(false);
            }
            else if (texture != null)
            {
                renderPipeline?.SetExternalSourceTexture(texture, activeMode.GetSourceModeName());
                mirrorController?.SetDirectTextureSourceRendering(true);
                mirrorController?.SetAnimatedImageSourceRendering(activeModeKind == KaleidoscopeSourceModeKind.ImageWallpaper);
                renderPipeline?.ReturnToKaleidoscopeView();
            }

            TrackImageFrameSpike();
            UpdateImageModeProfiler();
        }

        public bool SwitchTo(KaleidoscopeSourceModeKind mode, bool force = false)
        {
            InitializeModes();
            if (!force && Time.unscaledTime - lastSwitchTime < sourceSwitchCooldownSeconds)
            {
                return false;
            }

            if (!force && activeModeKind == mode)
            {
                return true;
            }

            IKaleidoscopeSourceMode next = ResolveMode(mode);
            if (next == null)
            {
                return false;
            }

            activeMode?.SetActiveWithoutRebuild(false);
            activeMode?.Deactivate();
            activeModeKind = mode;
            activeMode = next;
            activeMode.SetQualityLevel(currentQuality);
            activeMode.SetComfortPreset(currentComfort);
            activeMode.Activate();
            RecordImageModeEvent($"Source switched: {activeMode.GetSourceModeName()}");
            ApplyPhysicalLifecycle(ResolveSourceType(mode));
            CapturePerformanceBaselines();
            AuditCameraVisibleRenderers();
            ValidatePhysicalArtifactLeak();
            lastSwitchTime = Time.unscaledTime;
            statusPanel?.PostOperatorMessage($"Source switched: {activeMode.GetSourceModeName()}");
            return true;
        }

        public void SetImageSourceTextures(Texture2D[] textures)
        {
            SetImageSourceTextures(textures, null);
        }

        public void SetImageSourceTextures(Texture2D[] textures, string[] imagePaths)
        {
            InitializeModes();
            ImageWallpaperSourceMode image = ResolveMode(KaleidoscopeSourceModeKind.ImageWallpaper) as ImageWallpaperSourceMode;
            image?.SetTextures(textures, imagePaths);
        }

        public void RecordImageDiskRead(string path)
        {
            InitializeModes();
            RecordImageModeEvent($"Disk read: {path}");
            CurrentImageMode?.RecordExternalImageDiskRead(path);
        }

        public void RecordImageTextureCreate(string reason)
        {
            InitializeModes();
            RecordImageModeEvent($"Texture created: {reason}");
            CurrentImageMode?.RecordExternalTextureCreate(reason);
        }

        public void ResetCurrentMode()
        {
            activeMode?.ResetMode();
        }

        public void RandomizeCurrentMode()
        {
            activeMode?.RandomizeMode();
        }

        public void RequestMoreDensity()
        {
            activeMode?.RequestMoreDensity();
        }

        public void SetQualityLevel(KaleidoscopeQualityLevel quality)
        {
            currentQuality = quality;
            activeMode?.SetQualityLevel(quality);
        }

        public void SetComfortPreset(ViewerComfortPreset preset)
        {
            currentComfort = preset;
            activeMode?.SetComfortPreset(preset);
        }

        public void PausePhysicalSource()
        {
            physicalSourcePaused = true;
            physicsChamber?.SetAxialRotationEnabled(false);
            SleepPhysicalBodies();
            SetPhysicalObjectsActive(false);
            SetPhysicalRenderersActive(false);
        }

        public void ResumePhysicalSource()
        {
            physicalSourcePaused = false;
            spawner?.SetPhysicalArtifactRenderingSuppressed(false);
            sparkleController?.SetPhysicalArtifactRenderingSuppressed(false);
            causticProjector?.SetPhysicalArtifactRenderingSuppressed(false);
            SetPhysicalRenderersActive(true);
            SetPhysicalObjectsActive(true);
            WakePhysicalBodies();
            physicsChamber?.SetAxialRotationEnabled(true);
        }

        public void KillPhysicalArtifactsNow()
        {
            KillAllCenterArtifacts();
            statusPanel?.PostOperatorMessage("Kill Physical Artifacts Now executed.");
        }

        public void KillAllCenterArtifacts()
        {
            ForceDisablePhysicalArtifacts();
            spawner?.KillAllCenterArtifacts();
            renderPipeline?.ForceRefreshSourceTexture();
            AuditCameraVisibleRenderers();
            ValidateCenterArtifactLeak();
            statusPanel?.PostOperatorMessage($"KillAllCenterArtifacts executed. CenterArtifactRendererCount={CenterArtifactRendererCount}");
        }

        public string DiagnoseImageModeStutter()
        {
            ImageWallpaperSourceMode image = CurrentImageMode;
            KaleidoscopeRebuildGuard guard = KaleidoscopeRebuildGuard.Instance;
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.Append($"Mode={ActiveSourceModeName}; ");
            report.Append($"physicalPipelineActive={PhysicalPipelineActive}; ");
            report.Append($"sourceCameraActive={SourceCameraActive}; ");
            report.Append($"physicsBodiesActive={ActivePhysicsBodyCount}; ");
            report.Append($"imageReloadCount={(image != null ? image.ImageReloadCount : 0)}; ");
            report.Append($"textureCreateCount={(image != null ? image.TextureCreateCount : 0)}; ");
            report.Append($"imageDiskReadCount={(image != null ? image.ImageDiskReadCount : 0)}; ");
            report.Append($"renderTextureRecreateCount={(guard != null ? guard.TotalRenderTextureRecreateCount : 0)}; ");
            report.Append($"materialInstanceCount={(guard != null ? guard.TotalMaterialInstanceCount : 0)}; ");
            report.Append($"sourceRebuildCount={(guard != null ? guard.TotalSourceModeRebuildCount : 0)}; ");
            report.Append($"sourceTextureReassignCount={(renderPipeline != null ? renderPipeline.SourceTextureReassignCount : 0)}; ");
            report.Append($"fps={imageProfilerFps:F1}; ");
            report.Append($"frameTimeMs={imageProfilerFrameMs:F2}; ");
            report.Append($"gcAllocKB={imageProfilerGcAllocBytes / 1024f:F1}; ");
            report.Append($"spikes={imageProfilerSpikeCount}; ");
            report.Append($"textureResolution={(image != null ? image.ImageTextureResolution : 0)}; ");
            report.Append($"RT={(renderPipeline != null ? $"{renderPipeline.RenderTextureWidth}x{renderPipeline.RenderTextureHeight}" : "n/a")}; ");
            report.Append($"activeCrystalCount={(spawner != null ? spawner.LightweightVisualCrystalCount : 0)}; ");
            report.Append($"activeSparkleCount={(sparkleController != null ? sparkleController.ActiveSparkles : 0)}; ");
            report.Append($"lastImagePipelineEvent={(image != null ? image.LastImagePipelineEvent : "n/a")}; ");
            report.Append($"timeSinceLastEvent={(image != null ? image.TimeSinceLastImagePipelineEvent : 0f):F1}s; ");
            report.Append($"lastExpensiveEvent={(guard != null ? guard.LastExpensiveEvent : "n/a")}; ");
            report.Append($"timeSinceExpensiveEvent={(guard != null ? guard.TimeSinceLastExpensiveEvent : 0f):F1}s; ");
            report.Append($"periodicStutter={periodicImageStutterReport}; ");
            report.Append($"recentImageEvents={BuildRecentImageModeEvents()}");

            if (ActiveSourceType == KaleidoscopeSourceType.TextureSource && PhysicalPipelineActive)
            {
                report.Append(" ERROR: Image mode is incorrectly running physical pipeline.");
            }

            if (ActiveSourceType == KaleidoscopeSourceType.TextureSource && ActivePhysicsBodyCount > 0)
            {
                report.Append(" ERROR: Image mode has active physics bodies.");
            }

            if (guard != null && guard.RenderTextureRecreateCount > 0)
            {
                report.Append(" ERROR: RenderTexture is being recreated during runtime.");
            }

            if (ActiveSourceType == KaleidoscopeSourceType.TextureSource && image != null && image.ImageReloadCount > imageReloadBaseline)
            {
                report.Append(" ERROR: Image texture is being reloaded during runtime.");
            }

            if (ActiveSourceType == KaleidoscopeSourceType.TextureSource && guard != null && guard.TotalRenderTextureRecreateCount > renderTextureRecreateBaseline)
            {
                report.Append(" ERROR: RenderTexture is being recreated during runtime.");
            }

            if (ActiveSourceType == KaleidoscopeSourceType.TextureSource && guard != null && guard.TotalMaterialInstanceCount > materialInstanceBaseline)
            {
                report.Append(" ERROR: Material instances are being created during image playback.");
            }

            if (ActiveSourceType == KaleidoscopeSourceType.TextureSource && guard != null && guard.TotalSourceModeRebuildCount > sourceRebuildBaseline)
            {
                report.Append(" ERROR: Source mode is being rebuilt during image playback.");
            }

            statusPanel?.PostOperatorMessage(report.ToString());
            return report.ToString();
        }

        public void SleepPhysicalBodies()
        {
            if (spawner == null)
            {
                return;
            }

            IReadOnlyList<GameObject> objects = spawner.SpawnedObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                Rigidbody body = objects[i] != null ? objects[i].GetComponent<Rigidbody>() : null;
                if (body != null)
                {
                    body.Sleep();
                    body.isKinematic = true;
                }
            }

            physicalBodiesSleeping = true;
        }

        public void WakePhysicalBodies()
        {
            if (spawner == null)
            {
                return;
            }

            IReadOnlyList<GameObject> objects = spawner.SpawnedObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                Rigidbody body = objects[i] != null ? objects[i].GetComponent<Rigidbody>() : null;
                if (body != null)
                {
                    body.isKinematic = false;
                    body.WakeUp();
                }
            }

            physicalBodiesSleeping = false;
        }

        private void ApplyPhysicalLifecycle(KaleidoscopeSourceType type)
        {
            if (type == KaleidoscopeSourceType.PhysicalGemstoneSource || type == KaleidoscopeSourceType.HybridSource)
            {
                renderPipeline?.ClearExternalSourceTexture();
                mirrorController?.SetDirectTextureSourceRendering(false);
                mirrorController?.SetAnimatedImageSourceRendering(false);
                mirrorController?.ApplyImageModePerformanceBias(false);
                spawner?.SetLightweightFieldActive(true);
                ResumePhysicalSource();
                renderPipeline?.ReturnToKaleidoscopeView();
            }
            else
            {
                PausePhysicalSource();
                spawner?.SetLightweightFieldActive(false);
                mirrorController?.SetDirectTextureSourceRendering(true);
                mirrorController?.SetAnimatedImageSourceRendering(activeModeKind == KaleidoscopeSourceModeKind.ImageWallpaper);
                mirrorController?.ApplyImageModePerformanceBias(true);
                Texture texture = activeMode?.GetSourceTexture();
                if (texture != null)
                {
                    renderPipeline?.SetExternalSourceTexture(texture, activeMode.GetSourceModeName());
                }
                renderPipeline?.ReturnToKaleidoscopeView();

                if (disablePhysicalCenterArtifactsForNonPhysicalSources)
                {
                    ForceDisablePhysicalArtifacts();
                }
            }
        }

        private void CapturePerformanceBaselines()
        {
            ImageWallpaperSourceMode image = CurrentImageMode;
            KaleidoscopeRebuildGuard guard = KaleidoscopeRebuildGuard.Instance;
            imageReloadBaseline = image != null ? image.ImageReloadCount : 0;
            renderTextureRecreateBaseline = guard != null ? guard.TotalRenderTextureRecreateCount : 0;
            materialInstanceBaseline = guard != null ? guard.TotalMaterialInstanceCount : 0;
            sourceRebuildBaseline = guard != null ? guard.TotalSourceModeRebuildCount : 0;
            ResetImageProfilerWindow();
        }

        private void ForceDisablePhysicalArtifacts()
        {
            physicalSourcePaused = true;
            physicsChamber?.SetAxialRotationEnabled(false);
            SleepPhysicalBodies();
            SetPhysicalObjectsActive(false);
            spawner?.SetPhysicalArtifactRenderingSuppressed(true);
            spawner?.SetLightweightFieldActive(false);
            sparkleController?.SetPhysicalArtifactRenderingSuppressed(true);
            causticProjector?.SetPhysicalArtifactRenderingSuppressed(true);
            SetPhysicalRenderersActive(false);
            SetRendererTreeActive(spawner != null ? spawner.transform : null, false);
            SetRendererTreeActive(sparkleController != null ? sparkleController.transform : null, false);
            SetRendererTreeActive(causticProjector != null ? causticProjector.transform : null, false);
        }

        private void SetPhysicalObjectsActive(bool active)
        {
            if (spawner == null)
            {
                return;
            }

            IReadOnlyList<GameObject> objects = spawner.SpawnedObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    objects[i].SetActive(active);
                }
            }
        }

        private void SetPhysicalRenderersActive(bool active)
        {
            SetRendererTreeActive(physicalTubeRoot, active);
            if (opticalSourceChamber != null)
            {
                SetRendererTreeActive(opticalSourceChamber.transform, active);
            }
        }

        private static void SetRendererTreeActive(Transform root, bool active)
        {
            if (root == null)
            {
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = active;
                }
            }
        }

        private void AuditCameraVisibleRenderers()
        {
            layerAuditLog.Clear();
            Camera viewerCamera = Camera.main;
            Camera sourceCamera = renderPipeline != null ? renderPipeline.SourceCamera : null;
            AppendCameraAudit("ViewerCamera", viewerCamera);
            AppendCameraAudit("SourceCamera", sourceCamera);
        }

        private void AppendCameraAudit(string cameraLabel, Camera camera)
        {
            if (camera == null)
            {
                layerAuditLog.Add($"{cameraLabel}: missing");
                return;
            }

            Renderer[] renderers = FindObjectsOfType<Renderer>(true);
            int visibleCount = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!RendererVisibleToCamera(renderer, camera))
                {
                    continue;
                }

                visibleCount++;
                layerAuditLog.Add($"{cameraLabel}: {BuildRendererAuditLine(renderer)}");
            }

            if (visibleCount == 0)
            {
                layerAuditLog.Add($"{cameraLabel}: no enabled renderers in culling mask");
            }
        }

        private void ValidatePhysicalArtifactLeak()
        {
            physicalArtifactErrors.Clear();
            PhysicalArtifactRenderersActive = 0;
            CenterArtifactRendererCount = 0;
            if (SourceModeUsesTube)
            {
                CenterArtifactRendererCount = spawner != null ? spawner.CenterArtifactRendererCount : 0;
                return;
            }

            Renderer[] renderers = FindObjectsOfType<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy || !IsPhysicalArtifactRenderer(renderer))
                {
                    continue;
                }

                PhysicalArtifactRenderersActive++;
                CenterArtifactRendererCount++;
                physicalArtifactErrors.Add($"ERROR: Physical artifact renderer still active: {BuildTransformPath(renderer.transform)}");
            }
        }

        private void ValidateCenterArtifactLeak()
        {
            ValidatePhysicalArtifactLeak();
            if (spawner != null)
            {
                CenterArtifactRendererCount += spawner.CenterArtifactRendererCount;
            }
        }

        private bool IsPhysicalArtifactRenderer(Renderer renderer)
        {
            Transform transform = renderer.transform;
            return IsChildOf(transform, physicalTubeRoot)
                || IsChildOf(transform, opticalSourceChamber != null ? opticalSourceChamber.transform : null)
                || IsChildOf(transform, spawner != null ? spawner.transform : null)
                || IsChildOf(transform, sparkleController != null ? sparkleController.transform : null)
                || IsChildOf(transform, causticProjector != null ? causticProjector.transform : null)
                || NameSuggestsPhysicalArtifact(transform.name);
        }

        private static bool RendererVisibleToCamera(Renderer renderer, Camera camera)
        {
            return renderer != null
                && camera != null
                && renderer.enabled
                && renderer.gameObject.activeInHierarchy
                && (camera.cullingMask & (1 << renderer.gameObject.layer)) != 0;
        }

        private static bool IsChildOf(Transform transform, Transform root)
        {
            return transform != null && root != null && (transform == root || transform.IsChildOf(root));
        }

        private static bool NameSuggestsPhysicalArtifact(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            string lower = objectName.ToLowerInvariant();
            return lower.Contains("gem")
                || lower.Contains("chip")
                || lower.Contains("debris")
                || lower.Contains("sparkle")
                || lower.Contains("caustic")
                || lower.Contains("diffuser")
                || lower.Contains("chamber particle");
        }

        private static string BuildRendererAuditLine(Renderer renderer)
        {
            string layerName = LayerMask.LayerToName(renderer.gameObject.layer);
            if (string.IsNullOrEmpty(layerName))
            {
                layerName = renderer.gameObject.layer.ToString();
            }

            Material material = renderer.sharedMaterial;
            string materialName = material != null ? material.name : "no material";
            return $"{BuildTransformPath(renderer.transform)} | layer={layerName} | material={materialName}";
        }

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "null";
            }

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        private string ResolvePipelineWarning()
        {
            if (SourceModeUsesTube)
            {
                return string.Empty;
            }

            if (PhysicalArtifactRenderersActive > 0 && physicalArtifactErrors.Count > 0)
            {
                return physicalArtifactErrors[0];
            }

            if (mirrorController == null || mirrorController.PhysicalCenterArtifacts || !CenterCleanEnabled)
            {
                return "Non-physical source is leaking physical center artifacts.";
            }

            return string.Empty;
        }

        private string ResolveCurrentSourceModeCost()
        {
            if (ActiveSourceType == KaleidoscopeSourceType.PhysicalGemstoneSource || ActiveSourceType == KaleidoscopeSourceType.HybridSource)
            {
                return $"Physics:{(PhysicsActive ? "On" : "Off")} Visual:{(spawner != null ? spawner.LightweightVisualCrystalCount : 0)}";
            }

            if (ActiveSourceType == KaleidoscopeSourceType.TextureSource)
            {
                ImageWallpaperSourceMode image = ResolveMode(KaleidoscopeSourceModeKind.ImageWallpaper) as ImageWallpaperSourceMode;
                return image != null ? $"Image {image.ImageTextureResolution}px {image.ImageMemoryMB:F1}MB reloads:{image.ImageReloadCount}" : "Image";
            }

            return ActiveSourceType.ToString();
        }

        private int CountActivePhysicsBodies()
        {
            if (spawner == null)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<GameObject> objects = spawner.SpawnedObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                Rigidbody body = objects[i] != null && objects[i].activeInHierarchy ? objects[i].GetComponent<Rigidbody>() : null;
                if (body != null && !body.isKinematic && !body.IsSleeping())
                {
                    count++;
                }
            }

            return count;
        }

        private void TrackImageFrameSpike()
        {
            if (ActiveSourceType != KaleidoscopeSourceType.TextureSource || Time.unscaledDeltaTime < 0.045f)
            {
                return;
            }

            float now = Time.unscaledTime;
            ImageWallpaperSourceMode image = CurrentImageMode;
            KaleidoscopeRebuildGuard guard = KaleidoscopeRebuildGuard.Instance;
            string lastExpensiveEvent = ResolveLastExpensiveImageEvent(image, guard);
            imageSpikeTimes.Add(now);
            TrimImageSpikeTimes(now);
            string periodicReport = DetectPeriodicImageStutter(now, lastExpensiveEvent);
            if (!string.IsNullOrEmpty(periodicReport))
            {
                periodicImageStutterReport = periodicReport;
            }

            lastImageSpikeReport = image != null
                ? $"Image frame spike {Time.unscaledDeltaTime * 1000f:F1} ms; GC {imageProfilerGcAllocBytes / 1024f:F1} KB; last image event '{image.LastImagePipelineEvent}' ({image.TimeSinceLastImagePipelineEvent:F1}s ago); last expensive event '{lastExpensiveEvent}' ({(guard != null ? guard.TimeSinceLastExpensiveEvent : 0f):F1}s ago); {periodicImageStutterReport}"
                : $"Image frame spike {Time.unscaledDeltaTime * 1000f:F1} ms; no image mode diagnostics.";
            imageProfilerSpikeCount++;
            if (now < nextImageSpikeReportTime)
            {
                return;
            }

            nextImageSpikeReportTime = Time.unscaledTime + 1f;
            statusPanel?.PostOperatorMessage(lastImageSpikeReport);
        }

        private void RecordImageModeEvent(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            float now = Time.unscaledTime;
            imageModeEvents.Add(new TimedImageModeEvent(now, message));
            TrimImageModeEvents(now);
        }

        private void TrimImageModeEvents(float now)
        {
            for (int i = imageModeEvents.Count - 1; i >= 0; i--)
            {
                if (now - imageModeEvents[i].Time > 5f || imageModeEvents.Count > 24)
                {
                    imageModeEvents.RemoveAt(i);
                }
            }
        }

        private void TrimImageSpikeTimes(float now)
        {
            for (int i = imageSpikeTimes.Count - 1; i >= 0; i--)
            {
                if (now - imageSpikeTimes[i] > 5f)
                {
                    imageSpikeTimes.RemoveAt(i);
                }
            }
        }

        private string DetectPeriodicImageStutter(float now, string lastExpensiveEvent)
        {
            if (imageSpikeTimes.Count < 3)
            {
                return null;
            }

            int first = Mathf.Max(0, imageSpikeTimes.Count - 4);
            int intervals = 0;
            float totalInterval = 0f;
            float maxDeviation = 0f;
            for (int i = first + 1; i < imageSpikeTimes.Count; i++)
            {
                float interval = imageSpikeTimes[i] - imageSpikeTimes[i - 1];
                totalInterval += interval;
                intervals++;
            }

            if (intervals <= 0)
            {
                return null;
            }

            float average = totalInterval / intervals;
            for (int i = first + 1; i < imageSpikeTimes.Count; i++)
            {
                float interval = imageSpikeTimes[i] - imageSpikeTimes[i - 1];
                maxDeviation = Mathf.Max(maxDeviation, Mathf.Abs(interval - average));
            }

            if (average >= 1.2f && average <= 1.8f && maxDeviation <= 0.35f)
            {
                return $"Periodic image stutter detected. Last expensive event: {lastExpensiveEvent}. Interval {average:F2}s.";
            }

            return null;
        }

        private string ResolveLastExpensiveImageEvent(ImageWallpaperSourceMode image, KaleidoscopeRebuildGuard guard)
        {
            if (guard != null && !string.IsNullOrWhiteSpace(guard.LastExpensiveEvent) && guard.TimeSinceLastExpensiveEvent <= 5f)
            {
                return guard.LastExpensiveEvent;
            }

            if (image != null && !string.IsNullOrWhiteSpace(image.LastImagePipelineEvent))
            {
                return image.LastImagePipelineEvent;
            }

            return "n/a";
        }

        private string BuildRecentImageModeEvents()
        {
            if (imageModeEvents.Count == 0)
            {
                return "none";
            }

            TrimImageModeEvents(Time.unscaledTime);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < imageModeEvents.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Time.unscaledTime - imageModeEvents[i].Time < 0.05f ? "now" : $"{Time.unscaledTime - imageModeEvents[i].Time:F1}s ago");
                builder.Append(": ");
                builder.Append(imageModeEvents[i].Message);
            }

            return builder.ToString();
        }

        private void StartGcRecorder()
        {
            if (!gcAllocatedInFrameRecorder.Valid)
            {
                gcAllocatedInFrameRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            }
        }

        private void UpdateImageModeProfiler()
        {
            if (ActiveSourceType != KaleidoscopeSourceType.TextureSource)
            {
                return;
            }

            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            float fps = 1f / dt;
            imageProfilerFps = imageProfilerFps <= 0.01f ? fps : Mathf.Lerp(imageProfilerFps, fps, 0.08f);
            imageProfilerFrameMs = dt * 1000f;
            imageProfilerElapsed += dt;
            imageProfilerMinFps = Mathf.Min(imageProfilerMinFps, fps);
            imageProfilerMaxFrameMs = Mathf.Max(imageProfilerMaxFrameMs, imageProfilerFrameMs);
            imageProfilerGcAllocBytes = gcAllocatedInFrameRecorder.Valid ? gcAllocatedInFrameRecorder.LastValue : 0L;

            if (imageProfilerFps < 60f && Time.unscaledTime >= nextImageModeBudgetActionTime)
            {
                DeprioritizeSecondaryImageEffects();
                nextImageModeBudgetActionTime = Time.unscaledTime + 0.5f;
            }
        }

        private void ResetImageProfilerWindow()
        {
            imageProfilerFps = 0f;
            imageProfilerFrameMs = 0f;
            imageProfilerElapsed = 0f;
            imageProfilerMinFps = float.MaxValue;
            imageProfilerMaxFrameMs = 0f;
            imageProfilerSpikeCount = 0;
            imageProfilerGcAllocBytes = 0L;
            lastImageSpikeReport = "No image spike detected.";
        }

        private void DeprioritizeSecondaryImageEffects()
        {
            mirrorController?.ApplyImageModePerformanceBias(true);
            spawner?.SetLightweightFieldActive(false);
            spawner?.SetPhysicalArtifactRenderingSuppressed(true);
            sparkleController?.SetPhysicalArtifactRenderingSuppressed(true);
            causticProjector?.SetPhysicalArtifactRenderingSuppressed(true);
        }

        private void InitializeModes()
        {
            if (modes.Count > 0)
            {
                return;
            }

            AddMode(KaleidoscopeSourceModeKind.Gemstones, GetOrAdd<GemstoneSourceMode>());
            AddMode(KaleidoscopeSourceModeKind.ColoredGlassPhysical, GetOrAdd<GemstoneSourceMode>());
            AddMode(KaleidoscopeSourceModeKind.ImageWallpaper, GetOrAdd<ImageWallpaperSourceMode>());
            AddMode(KaleidoscopeSourceModeKind.ProceduralColorBlobs, GetOrAdd<ProceduralColorBlobsSource>());
            AddMode(KaleidoscopeSourceModeKind.PolygonGeometry, GetOrAdd<PolygonSource>());
            AddMode(KaleidoscopeSourceModeKind.LiquidIllusion, GetOrAdd<LiquidIllusionSource>());
            AddMode(KaleidoscopeSourceModeKind.Hybrid, GetOrAdd<GemstoneSourceMode>());
            AddMode(KaleidoscopeSourceModeKind.Experimental, GetOrAdd<LiquidSourceMode>());

            foreach (KeyValuePair<KaleidoscopeSourceModeKind, IKaleidoscopeSourceMode> pair in modes)
            {
                pair.Value.Initialize();
                pair.Value.SetActiveWithoutRebuild(false);
            }
        }

        private void ConfigureImageMode()
        {
            ImageWallpaperSourceMode image = ResolveMode(KaleidoscopeSourceModeKind.ImageWallpaper) as ImageWallpaperSourceMode;
            image?.Configure(mirrorController);
        }

        private void AddMode(KaleidoscopeSourceModeKind kind, IKaleidoscopeSourceMode mode)
        {
            if (!modes.ContainsKey(kind))
            {
                modes.Add(kind, mode);
            }
        }

        private T GetOrAdd<T>() where T : Component, IKaleidoscopeSourceMode
        {
            T mode = GetComponent<T>();
            if (mode == null)
            {
                KaleidoscopeRebuildGuard.RecordGameObjectInstantiate(typeof(T).Name);
                mode = gameObject.AddComponent<T>();
            }

            if (mode is GemstoneSourceMode gemstone)
            {
                gemstone.Configure(renderPipeline);
            }

            if (mode is ImageWallpaperSourceMode image)
            {
                image.Configure(mirrorController);
            }

            return mode;
        }

        private IKaleidoscopeSourceMode ResolveMode(KaleidoscopeSourceModeKind mode)
        {
            modes.TryGetValue(mode, out IKaleidoscopeSourceMode sourceMode);
            return sourceMode;
        }

        private KaleidoscopeSourceType ResolveSourceType(KaleidoscopeSourceModeKind mode)
        {
            switch (mode)
            {
                case KaleidoscopeSourceModeKind.Gemstones:
                case KaleidoscopeSourceModeKind.ColoredGlassPhysical:
                    return KaleidoscopeSourceType.PhysicalGemstoneSource;
                case KaleidoscopeSourceModeKind.ImageWallpaper:
                    return KaleidoscopeSourceType.TextureSource;
                case KaleidoscopeSourceModeKind.ProceduralColorBlobs:
                case KaleidoscopeSourceModeKind.PolygonGeometry:
                    return KaleidoscopeSourceType.ProceduralPatternSource;
                case KaleidoscopeSourceModeKind.LiquidIllusion:
                case KaleidoscopeSourceModeKind.Experimental:
                    return KaleidoscopeSourceType.LiquidShaderSource;
                case KaleidoscopeSourceModeKind.Hybrid:
                    return KaleidoscopeSourceType.HybridSource;
                default:
                    return KaleidoscopeSourceType.PhysicalGemstoneSource;
            }
        }
    }
}
