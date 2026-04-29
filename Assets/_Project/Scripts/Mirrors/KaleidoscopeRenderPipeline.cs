using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;
using UnityEngine.Rendering;

namespace KaleidoscopeEngine.Mirrors
{
    public enum KaleidoscopeSourceMode
    {
        RawTube,
        ObjectChamber
    }

    public enum KaleidoscopeViewMode
    {
        Kaleidoscope,
        RawTube,
        SourcePreview,
        DebugOrbit
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeRenderPipeline : MonoBehaviour
    {
        private const int FallbackDisplayLayer = 2; // Built-in Ignore Raycast layer, used only if the project layer is missing.

        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private Renderer displayRenderer;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private OpticalSourceChamber opticalSourceChamber;
        [SerializeField] private KaleidoscopeViewerCameraController viewerCameraController;
        [SerializeField] private ObjectChamberCameraController objectChamberCameraController;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private GemSparkleController sparkleController;

        [Header("Render Texture")]
        [SerializeField] private int baseResolution = 1024;
        [SerializeField, Range(0.5f, 2f)] private float resolutionScale = 1f;
        [SerializeField] private bool useHdr = true;

        [Header("Quality")]
        [SerializeField] private KaleidoscopeQualityLevel qualityLevel = KaleidoscopeQualityLevel.High;
        [SerializeField] private bool renderTextureInspectorDebug = true;

        [Header("Display")]
        [SerializeField] private KaleidoscopeViewMode viewMode = KaleidoscopeViewMode.Kaleidoscope;
        [SerializeField] private float displayDistance = 1.25f;
        [SerializeField] private string displayLayerName = "KaleidoscopeDisplay";

        [Header("Optical Source")]
        [SerializeField] private KaleidoscopeSourceMode sourceMode = KaleidoscopeSourceMode.ObjectChamber;
        [SerializeField] private bool ribsVisibleToSourceCamera;
        [SerializeField] private float objectChamberFieldOfView = 30f;
        [SerializeField] private Color objectSourceBackground = new Color(0.015f, 0.018f, 0.022f, 1f);
        [SerializeField] private string gemLayerName = "KaleidoscopeGems";
        [SerializeField] private string particleLayerName = "KaleidoscopeParticles";
        [SerializeField] private string opticalFxLayerName = "KaleidoscopeOpticalFX";
        [SerializeField] private string chamberVisualLayerName = "KaleidoscopeChamberVisual";
        [SerializeField] private string physicsOnlyLayerName = "KaleidoscopePhysicsOnly";
        [SerializeField] private string debugLayerName = "KaleidoscopeDebug";

        private RenderTexture sourceTexture;
        private Material displayMaterial;
        private int originalMainCullingMask = -1;
        private CameraClearFlags originalClearFlags;
        private Color originalBackgroundColor;
        private string diagnosticStatus = "Pipeline not validated yet.";
        private bool hasBlockingDiagnostic;
        private float nextValidationTime;
        private string lastLoggedDiagnostic;
        private KaleidoscopeQualityProfile activeQualityProfile;
        private bool activeQualityProfileInitialized;
        private float nextSourceRenderTime;

        public bool KaleidoscopeView => viewMode == KaleidoscopeViewMode.Kaleidoscope;
        public bool DisplayView => viewMode == KaleidoscopeViewMode.Kaleidoscope || viewMode == KaleidoscopeViewMode.SourcePreview;
        public KaleidoscopeViewMode CurrentViewMode => viewMode;
        public string ViewMode => viewMode.ToString();
        public string SourceModeName => sourceMode == KaleidoscopeSourceMode.ObjectChamber ? "Object Chamber" : "Raw Tube";
        public string SourceCameraCullingMaskMode => ribsVisibleToSourceCamera ? "Object + Ribs" : "Object Clean";
        public bool RibsVisibleToSourceCamera => ribsVisibleToSourceCamera;
        public string DiagnosticStatus => diagnosticStatus;
        public bool HasBlockingDiagnostic => hasBlockingDiagnostic;
        public int RenderTextureWidth => sourceTexture != null ? sourceTexture.width : 0;
        public int RenderTextureHeight => sourceTexture != null ? sourceTexture.height : 0;
        public string QualityPresetName => ActiveQualityProfile.DisplayName;
        public KaleidoscopeQualityLevel QualityLevel => qualityLevel;
        public float SupersamplingFactor => ActiveQualityProfile.SafeSupersamplingFactor;
        public int SourceUpdateRateLimit => ActiveQualityProfile.updateRateLimit;
        public bool RenderTextureInspectorDebug => renderTextureInspectorDebug;
        public string RenderTextureFormatName => sourceTexture != null ? sourceTexture.format.ToString() : ResolveRenderTextureFormat(ActiveQualityProfile).ToString();
        public string RenderTextureFilterModeName => sourceTexture != null ? sourceTexture.filterMode.ToString() : ActiveQualityProfile.filterMode.ToString();
        public float EstimatedRenderTextureMemoryMB => EstimateRenderTextureMemoryMB();
        public KaleidoscopeViewerCameraController ViewerCameraController => viewerCameraController;
        public Camera SourceCamera => sourceCamera;
        private int DisplayLayer => LayerMask.NameToLayer(displayLayerName) >= 0 ? LayerMask.NameToLayer(displayLayerName) : FallbackDisplayLayer;
        private KaleidoscopeQualityProfile ActiveQualityProfile
        {
            get
            {
                EnsureQualityProfile();
                return activeQualityProfile;
            }
        }

        public void Configure(Camera camera, KaleidoscopeMirrorController controller)
        {
            mainCamera = camera != null ? camera : Camera.main;
            mirrorController = controller;
            EnsureQualityProfile();
            BuildPipelineObjects();
            ApplyQualityProfile(false);
            SetViewMode(viewMode);
        }

        public void ConfigureOpticalSource(OpticalSourceChamber sourceChamber)
        {
            opticalSourceChamber = sourceChamber;
            sourceMode = sourceChamber != null ? KaleidoscopeSourceMode.ObjectChamber : KaleidoscopeSourceMode.RawTube;
            BuildPipelineObjects();
        }

        public void ConfigureQualityTargets(GemstoneSpawner gemstoneSpawner, GemSparkleController sparkles)
        {
            spawner = gemstoneSpawner;
            sparkleController = sparkles;
            ApplyQualityProfile(false);
        }

        private void Awake()
        {
            EnsureQualityProfile();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            BuildPipelineObjects();
            ApplyQualityProfile(false);
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                return;
            }

            EnsureRenderTexture();
            SyncSourceCamera();
            AlignDisplayToViewerCamera();
            ApplyViewMode();

            if (Time.unscaledTime >= nextValidationTime)
            {
                ValidateKaleidoscopePipeline(false);
                nextValidationTime = Time.unscaledTime + 1f;
            }
        }

        private void OnDestroy()
        {
            if (sourceTexture != null)
            {
                sourceTexture.Release();
                Destroy(sourceTexture);
            }

            if (displayMaterial != null)
            {
                Destroy(displayMaterial);
            }
        }

        public void ToggleView()
        {
            CycleViewMode();
        }

        public void CycleViewMode()
        {
            switch (viewMode)
            {
                case KaleidoscopeViewMode.Kaleidoscope:
                    SetViewMode(KaleidoscopeViewMode.RawTube);
                    break;
                case KaleidoscopeViewMode.RawTube:
                    SetViewMode(KaleidoscopeViewMode.SourcePreview);
                    break;
                case KaleidoscopeViewMode.SourcePreview:
                    SetViewMode(KaleidoscopeViewMode.DebugOrbit);
                    break;
                default:
                    SetViewMode(KaleidoscopeViewMode.Kaleidoscope);
                    break;
            }
        }

        public void ToggleSourcePreview()
        {
            SetViewMode(viewMode == KaleidoscopeViewMode.SourcePreview
                ? KaleidoscopeViewMode.Kaleidoscope
                : KaleidoscopeViewMode.SourcePreview);
        }

        public void ToggleDebugOrbit()
        {
            SetViewMode(viewMode == KaleidoscopeViewMode.DebugOrbit
                ? KaleidoscopeViewMode.Kaleidoscope
                : KaleidoscopeViewMode.DebugOrbit);
        }

        public void ReturnToKaleidoscopeView()
        {
            SetViewMode(KaleidoscopeViewMode.Kaleidoscope);
        }

        public void ToggleRibsVisibleToSourceCamera()
        {
            ribsVisibleToSourceCamera = !ribsVisibleToSourceCamera;
            SyncSourceCamera();
        }

        public void AdjustQualityLevel(int delta)
        {
            int next = Mathf.Clamp((int)qualityLevel + delta, 0, (int)KaleidoscopeQualityLevel.Extreme);
            SetQualityLevel((KaleidoscopeQualityLevel)next, true);
        }

        public void SetQualityLevel(KaleidoscopeQualityLevel level, bool respawnEntropy)
        {
            if (qualityLevel == level && activeQualityProfileInitialized)
            {
                ApplyQualityProfile(respawnEntropy);
                return;
            }

            qualityLevel = level;
            activeQualityProfile = KaleidoscopeQualityProfile.ForLevel(qualityLevel);
            activeQualityProfileInitialized = true;
            ApplyQualityProfile(respawnEntropy);
            EnsureRenderTexture();
            SyncSourceCamera();
            ValidateKaleidoscopePipeline(true);
        }

        public void SetMinimumQuality()
        {
            SetQualityLevel(KaleidoscopeQualityLevel.Minimal, true);
        }

        public void SetMaximumQuality()
        {
            SetQualityLevel(KaleidoscopeQualityLevel.Extreme, true);
        }

        public void SetObjectChamberSourceMode(bool enabled)
        {
            sourceMode = enabled ? KaleidoscopeSourceMode.ObjectChamber : KaleidoscopeSourceMode.RawTube;
            SyncSourceCamera();
        }

        public void SetKaleidoscopeView(bool enabled)
        {
            SetViewMode(enabled ? KaleidoscopeViewMode.Kaleidoscope : KaleidoscopeViewMode.RawTube);
        }

        public void SetViewMode(KaleidoscopeViewMode mode)
        {
            BuildPipelineObjects();
            viewMode = mode;

            if (mainCamera == null)
            {
                return;
            }

            if (originalMainCullingMask < 0)
            {
                originalMainCullingMask = mainCamera.cullingMask;
                originalClearFlags = mainCamera.clearFlags;
                originalBackgroundColor = mainCamera.backgroundColor;
            }

            ApplyViewMode();
            ValidateKaleidoscopePipeline(true);
        }

        private void BuildPipelineObjects()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            EnsureRenderTexture();
            EnsureDisplayMaterial();
            EnsureSourceCamera();
            EnsureDisplayQuad();
            EnsureViewerCameraController();
            EnsureObjectChamberCameraController();

            if (mirrorController != null)
            {
                mirrorController.Configure(displayMaterial);
                mirrorController.SetSourceTexture(sourceTexture);
            }
        }

        private void EnsureRenderTexture()
        {
            KaleidoscopeQualityProfile profile = ActiveQualityProfile;
            int maxTextureSize = Mathf.Max(512, SystemInfo.maxTextureSize);
            int targetWidth = Mathf.Clamp(
                Mathf.RoundToInt(baseResolution * resolutionScale * profile.SafeSupersamplingFactor * profile.SafeTextureScale),
                256,
                maxTextureSize);
            int targetHeight = Mathf.Clamp(
                Mathf.RoundToInt(targetWidth / Mathf.Max(0.1f, mainCamera != null ? mainCamera.aspect : 1.777f)),
                256,
                maxTextureSize);
            RenderTextureFormat targetFormat = ResolveRenderTextureFormat(profile);
            int targetMsaa = profile.SafeAntiAliasingSamples;
            bool targetMipMaps = profile.useMipMaps && targetMsaa <= 1;

            if (sourceTexture != null &&
                sourceTexture.width == targetWidth &&
                sourceTexture.height == targetHeight &&
                sourceTexture.format == targetFormat &&
                sourceTexture.antiAliasing == targetMsaa &&
                sourceTexture.useMipMap == targetMipMaps)
            {
                ApplyRenderTextureSamplerSettings(profile);
                return;
            }

            if (sourceTexture != null)
            {
                sourceTexture.Release();
                Destroy(sourceTexture);
            }

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(targetWidth, targetHeight, targetFormat, 24)
            {
                msaaSamples = targetMsaa,
                useMipMap = targetMipMaps,
                autoGenerateMips = targetMipMaps,
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
            };
            sourceTexture = new RenderTexture(descriptor)
            {
                name = $"Runtime Kaleidoscope Source {profile.DisplayName}",
                wrapMode = TextureWrapMode.Clamp
            };
            ApplyRenderTextureSamplerSettings(profile);
            sourceTexture.Create();

            if (sourceCamera != null)
            {
                sourceCamera.targetTexture = sourceTexture;
            }

            if (mirrorController != null)
            {
                mirrorController.SetSourceTexture(sourceTexture);
            }
        }

        private void EnsureDisplayMaterial()
        {
            if (displayMaterial != null)
            {
                return;
            }

            Shader shader = Shader.Find("KaleidoscopeEngine/KaleidoscopeMirror");
            displayMaterial = new Material(shader != null ? shader : Shader.Find("Unlit/Texture"))
            {
                name = "Runtime Kaleidoscope Display"
            };
            displayMaterial.SetTexture("_SourceTex", sourceTexture);
        }

        private void EnsureSourceCamera()
        {
            if (sourceCamera != null)
            {
                return;
            }

            GameObject sourceObject = new GameObject("ObjectChamberSourceCamera");
            sourceObject.transform.SetParent(transform, false);
            sourceCamera = sourceObject.AddComponent<Camera>();
            sourceCamera.enabled = false;
            sourceCamera.targetTexture = sourceTexture;
            sourceCamera.cullingMask = mainCamera.cullingMask & ~(1 << DisplayLayer);
            sourceCamera.depth = mainCamera.depth - 10f;
            objectChamberCameraController = sourceObject.AddComponent<ObjectChamberCameraController>();
            objectChamberCameraController.Configure(sourceCamera, opticalSourceChamber);
            SyncSourceCamera();
        }

        private void EnsureDisplayQuad()
        {
            if (displayRenderer != null)
            {
                return;
            }

            GameObject displayObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            displayObject.name = "KaleidoscopeDisplay";
            displayObject.layer = DisplayLayer;
            displayObject.transform.SetParent(mainCamera.transform, false);

            Collider collider = displayObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            displayRenderer = displayObject.GetComponent<Renderer>();
            displayRenderer.sharedMaterial = displayMaterial;
            displayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            displayRenderer.receiveShadows = false;
            displayObject.SetActive(DisplayView);
            AlignDisplayToViewerCamera();
        }

        private void EnsureViewerCameraController()
        {
            if (mainCamera == null || displayRenderer == null)
            {
                return;
            }

            if (viewerCameraController == null)
            {
                viewerCameraController = mainCamera.GetComponent<KaleidoscopeViewerCameraController>();
                if (viewerCameraController == null)
                {
                    viewerCameraController = mainCamera.gameObject.AddComponent<KaleidoscopeViewerCameraController>();
                }
            }

            viewerCameraController.Configure(mainCamera, displayRenderer);
        }

        private void EnsureObjectChamberCameraController()
        {
            if (sourceCamera == null)
            {
                return;
            }

            if (objectChamberCameraController == null)
            {
                objectChamberCameraController = sourceCamera.GetComponent<ObjectChamberCameraController>();
                if (objectChamberCameraController == null)
                {
                    objectChamberCameraController = sourceCamera.gameObject.AddComponent<ObjectChamberCameraController>();
                }
            }

            objectChamberCameraController.Configure(sourceCamera, opticalSourceChamber);
        }

        private void SyncSourceCamera()
        {
            if (sourceCamera == null || mainCamera == null)
            {
                return;
            }

            bool useObjectChamber = sourceMode == KaleidoscopeSourceMode.ObjectChamber && opticalSourceChamber != null;
            if (useObjectChamber)
            {
                if (objectChamberCameraController != null)
                {
                    objectChamberCameraController.SetIncludeRibs(ribsVisibleToSourceCamera);
                    objectChamberCameraController.SetIncludeTubeWalls(ribsVisibleToSourceCamera);
                    objectChamberCameraController.AlignSourceCamera();
                }
                else
                {
                    Transform sourceTransform = opticalSourceChamber.transform;
                    Vector3 cameraPosition = sourceTransform.TransformPoint(opticalSourceChamber.SourceCameraLocalPosition);
                    Vector3 targetPosition = sourceTransform.TransformPoint(opticalSourceChamber.SourceLookTargetLocalPosition);
                    sourceCamera.transform.SetPositionAndRotation(
                        cameraPosition,
                        Quaternion.LookRotation(targetPosition - cameraPosition, sourceTransform.up));
                    sourceCamera.fieldOfView = objectChamberFieldOfView;
                }

                sourceCamera.clearFlags = CameraClearFlags.SolidColor;
                sourceCamera.backgroundColor = objectSourceBackground;
            }
            else
            {
                sourceCamera.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
                sourceCamera.fieldOfView = mainCamera.fieldOfView;
                sourceCamera.clearFlags = mainCamera.clearFlags == CameraClearFlags.SolidColor && DisplayView
                    ? originalClearFlags
                    : mainCamera.clearFlags;
                sourceCamera.backgroundColor = originalBackgroundColor;
            }

            sourceCamera.allowHDR = mainCamera.allowHDR;
            sourceCamera.allowMSAA = ActiveQualityProfile.SafeAntiAliasingSamples > 1;
            sourceCamera.nearClipPlane = mainCamera.nearClipPlane;
            sourceCamera.farClipPlane = mainCamera.farClipPlane;
            sourceCamera.cullingMask = useObjectChamber
                ? BuildObjectChamberCullingMask()
                : (originalMainCullingMask >= 0 ? originalMainCullingMask : mainCamera.cullingMask) & ~(1 << DisplayLayer);
            sourceCamera.targetTexture = sourceTexture;
        }

        private int BuildObjectChamberCullingMask()
        {
            int mask = 0;
            mask |= LayerMaskForName(gemLayerName);
            mask |= LayerMaskForName(particleLayerName);
            mask |= LayerMaskForName(opticalFxLayerName);

            if (ribsVisibleToSourceCamera)
            {
                mask |= LayerMaskForName(chamberVisualLayerName);
                mask |= LayerMaskForName(physicsOnlyLayerName);
            }

            mask &= ~LayerMaskForName(debugLayerName);
            mask &= ~(1 << DisplayLayer);
            return mask != 0 ? mask : (originalMainCullingMask >= 0 ? originalMainCullingMask : mainCamera.cullingMask) & ~(1 << DisplayLayer);
        }

        private static int LayerMaskForName(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? 1 << layer : 0;
        }

        public void AlignDisplayToViewerCamera()
        {
            if (displayRenderer == null || mainCamera == null)
            {
                return;
            }

            if (viewerCameraController != null)
            {
                viewerCameraController.AlignDisplayToViewerCamera();
                return;
            }

            Transform quad = displayRenderer.transform;
            if (quad.parent != mainCamera.transform)
            {
                quad.SetParent(mainCamera.transform, false);
            }

            quad.localPosition = new Vector3(0f, 0f, displayDistance);
            quad.localRotation = Quaternion.identity;
            float height = 2f * Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * displayDistance;
            float width = height * mainCamera.aspect;
            quad.localScale = new Vector3(width, height, 1f);
        }

        public void AdjustSourceOrbit(float deltaDegrees)
        {
            if (objectChamberCameraController == null)
            {
                return;
            }

            objectChamberCameraController.AdjustSourceOrbit(deltaDegrees);
        }

        public void AdjustSourceFraming(float delta)
        {
            if (objectChamberCameraController == null)
            {
                return;
            }

            objectChamberCameraController.AdjustSourceFraming(delta);
        }

        private void ApplyViewMode()
        {
            bool displayMode = DisplayView;
            if (sourceCamera != null)
            {
                sourceCamera.enabled = displayMode && ShouldSourceCameraRenderThisFrame();
            }

            if (displayRenderer != null)
            {
                displayRenderer.gameObject.SetActive(displayMode);
                displayRenderer.sharedMaterial = displayMaterial;
            }

            if (displayMaterial != null)
            {
                displayMaterial.SetFloat("_PreviewSource", viewMode == KaleidoscopeViewMode.SourcePreview ? 1f : 0f);
                displayMaterial.SetTexture("_SourceTex", sourceTexture);
            }

            if (viewerCameraController != null)
            {
                viewerCameraController.SetViewerModeActive(displayMode);
            }

            if (mainCamera == null || originalMainCullingMask < 0)
            {
                return;
            }

            if (displayMode)
            {
                mainCamera.cullingMask = 1 << DisplayLayer;
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = Color.black;
            }
            else
            {
                mainCamera.cullingMask = originalMainCullingMask;
                mainCamera.clearFlags = originalClearFlags;
                mainCamera.backgroundColor = originalBackgroundColor;
            }
        }

        public bool ValidateKaleidoscopePipeline(bool logWarnings)
        {
            System.Text.StringBuilder diagnostics = new System.Text.StringBuilder();
            bool ok = true;

            if (mainCamera == null)
            {
                ok = false;
                diagnostics.AppendLine("MainCamera missing: viewer camera cannot present the eyepiece.");
            }

            if (sourceCamera == null)
            {
                ok = false;
                diagnostics.AppendLine("SourceCamera missing: ObjectChamberSourceCamera was not created.");
            }
            else if (DisplayView && !sourceCamera.enabled && ActiveQualityProfile.updateRateLimit <= 0)
            {
                ok = false;
                diagnostics.AppendLine("SourceCamera inactive: display modes need the RenderTexture updated.");
            }
            else if (DisplayView && sourceCamera.targetTexture != sourceTexture)
            {
                ok = false;
                diagnostics.AppendLine("SourceCamera target mismatch: it is not rendering into the active kaleidoscope RenderTexture.");
            }

            if (sourceTexture == null || !sourceTexture.IsCreated())
            {
                ok = false;
                diagnostics.AppendLine("RenderTexture missing: source camera has nowhere to render.");
            }

            if (displayRenderer == null)
            {
                ok = false;
                diagnostics.AppendLine("KaleidoscopeDisplay missing: final image quad was not created.");
            }
            else if (DisplayView && !displayRenderer.gameObject.activeInHierarchy)
            {
                ok = false;
                diagnostics.AppendLine("KaleidoscopeDisplay inactive: viewer camera has no final screen.");
            }
            else if (DisplayView && displayRenderer.gameObject.layer != DisplayLayer)
            {
                ok = false;
                diagnostics.AppendLine("KaleidoscopeDisplay layer mismatch: display quad must be on the viewer-only display layer.");
            }

            if (displayMaterial == null)
            {
                ok = false;
                diagnostics.AppendLine("Display material missing: mirror shader is not assigned.");
            }
            else
            {
                if (displayMaterial.shader == null || !displayMaterial.shader.name.Contains("KaleidoscopeMirror"))
                {
                    ok = false;
                    diagnostics.AppendLine("Mirror shader inactive: display material is not using KaleidoscopeMirror.");
                }

                if (displayMaterial.GetTexture("_SourceTex") != sourceTexture)
                {
                    ok = false;
                    diagnostics.AppendLine("Source texture not bound: display material _SourceTex is not the active RenderTexture.");
                }
            }

            if (mirrorController == null || mirrorController.SegmentCount < 1)
            {
                ok = false;
                diagnostics.AppendLine("Invalid segment count: mirror controller is missing or has no valid prism segments.");
            }

            if (DisplayView && mainCamera != null && (mainCamera.cullingMask & (1 << DisplayLayer)) == 0)
            {
                ok = false;
                diagnostics.AppendLine("MainCamera culling mask excludes KaleidoscopeDisplay layer.");
            }

            if (DisplayView && sourceCamera != null && CountPotentialSourceRenderers() == 0)
            {
                ok = false;
                diagnostics.AppendLine("Source camera culling mask sees no renderers. Check gem/particle/optical FX layers.");
            }

            if (ok)
            {
                diagnosticStatus = "OK: viewer, source camera, RenderTexture, mirror shader, and display quad are aligned.";
                hasBlockingDiagnostic = false;
                return true;
            }

            diagnosticStatus = diagnostics.ToString().Trim();
            hasBlockingDiagnostic = true;
            if (logWarnings && diagnosticStatus != lastLoggedDiagnostic)
            {
                Debug.LogWarning($"Kaleidoscope pipeline diagnostics:\n{diagnosticStatus}", this);
                lastLoggedDiagnostic = diagnosticStatus;
            }

            return false;
        }

        private int CountPotentialSourceRenderers()
        {
            if (sourceCamera == null)
            {
                return 0;
            }

            int count = 0;
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null &&
                    renderer.enabled &&
                    renderer.gameObject.activeInHierarchy &&
                    (sourceCamera.cullingMask & (1 << renderer.gameObject.layer)) != 0)
                {
                    count++;
                }
            }

            return count;
        }

        private bool ShouldSourceCameraRenderThisFrame()
        {
            if (!DisplayView)
            {
                return false;
            }

            int updateRateLimit = ActiveQualityProfile.updateRateLimit;
            if (updateRateLimit <= 0)
            {
                return true;
            }

            float now = Time.unscaledTime;
            if (now < nextSourceRenderTime)
            {
                return false;
            }

            nextSourceRenderTime = now + 1f / Mathf.Max(1, updateRateLimit);
            return true;
        }

        private void EnsureQualityProfile()
        {
            if (activeQualityProfileInitialized)
            {
                return;
            }

            activeQualityProfile = KaleidoscopeQualityProfile.ForLevel(qualityLevel);
            activeQualityProfileInitialized = true;
            baseResolution = activeQualityProfile.renderTextureResolution;
            resolutionScale = activeQualityProfile.SafeTextureScale;
            useHdr = activeQualityProfile.hdrEnabled;
        }

        private void ApplyQualityProfile(bool respawnEntropy)
        {
            KaleidoscopeQualityProfile profile = ActiveQualityProfile;
            baseResolution = profile.renderTextureResolution;
            resolutionScale = profile.SafeTextureScale;
            useHdr = profile.hdrEnabled;
            mirrorController?.ApplyQualityProfile(profile);
            spawner?.ApplyQualityProfile(profile, respawnEntropy && Application.isPlaying);
            sparkleController?.ApplyQualityProfile(profile);
        }

        private void ApplyRenderTextureSamplerSettings(KaleidoscopeQualityProfile profile)
        {
            if (sourceTexture == null)
            {
                return;
            }

            sourceTexture.filterMode = profile.filterMode;
            sourceTexture.anisoLevel = profile.SafeAnisotropicFiltering;
            sourceTexture.mipMapBias = profile.mipBias;
            sourceTexture.wrapMode = TextureWrapMode.Clamp;
        }

        private static RenderTextureFormat ResolveRenderTextureFormat(KaleidoscopeQualityProfile profile)
        {
            RenderTextureFormat targetFormat = profile.hdrEnabled ? profile.renderTextureFormat : RenderTextureFormat.Default;
            if (SystemInfo.SupportsRenderTextureFormat(targetFormat))
            {
                return targetFormat;
            }

            if (profile.hdrEnabled && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR))
            {
                return RenderTextureFormat.DefaultHDR;
            }

            return RenderTextureFormat.Default;
        }

        private float EstimateRenderTextureMemoryMB()
        {
            KaleidoscopeQualityProfile profile = ActiveQualityProfile;
            int width = RenderTextureWidth > 0 ? RenderTextureWidth : Mathf.Max(256, Mathf.RoundToInt(profile.renderTextureResolution * profile.SafeSupersamplingFactor));
            int height = RenderTextureHeight > 0 ? RenderTextureHeight : Mathf.RoundToInt(width / Mathf.Max(0.1f, mainCamera != null ? mainCamera.aspect : 1.777f));
            int colorBytes = profile.ColorBytesPerPixel;
            int depthBytes = 4;
            float mipMultiplier = profile.useMipMaps && profile.SafeAntiAliasingSamples <= 1 ? 1.33f : 1f;
            float bytes = width * height * (colorBytes + depthBytes) * profile.SafeAntiAliasingSamples * mipMultiplier;
            return bytes / (1024f * 1024f);
        }
    }
}
