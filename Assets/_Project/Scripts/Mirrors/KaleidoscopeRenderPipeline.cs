using UnityEngine;
using UnityEngine.Rendering;

namespace KaleidoscopeEngine.Mirrors
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeRenderPipeline : MonoBehaviour
    {
        private const int DisplayLayer = 2; // Built-in Ignore Raycast layer; used to hide display from SourceCamera.

        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private Renderer displayRenderer;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;

        [Header("Render Texture")]
        [SerializeField] private int baseResolution = 1024;
        [SerializeField, Range(0.5f, 2f)] private float resolutionScale = 1f;
        [SerializeField] private bool useHdr = true;

        [Header("Display")]
        [SerializeField] private bool kaleidoscopeView;
        [SerializeField] private float displayDistance = 1.25f;

        private RenderTexture sourceTexture;
        private Material displayMaterial;
        private int originalMainCullingMask = -1;
        private CameraClearFlags originalClearFlags;
        private Color originalBackgroundColor;

        public bool KaleidoscopeView => kaleidoscopeView;
        public string ViewMode => kaleidoscopeView ? "Kaleidoscope" : "Raw";
        public int RenderTextureWidth => sourceTexture != null ? sourceTexture.width : 0;
        public int RenderTextureHeight => sourceTexture != null ? sourceTexture.height : 0;

        public void Configure(Camera camera, KaleidoscopeMirrorController controller)
        {
            mainCamera = camera != null ? camera : Camera.main;
            mirrorController = controller;
            BuildPipelineObjects();
            SetKaleidoscopeView(kaleidoscopeView);
        }

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            BuildPipelineObjects();
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                return;
            }

            EnsureRenderTexture();
            SyncSourceCamera();
            UpdateDisplayQuad();
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
            SetKaleidoscopeView(!kaleidoscopeView);
        }

        public void SetKaleidoscopeView(bool enabled)
        {
            BuildPipelineObjects();
            kaleidoscopeView = enabled;

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

            if (sourceCamera != null)
            {
                sourceCamera.enabled = enabled;
            }

            if (displayRenderer != null)
            {
                displayRenderer.gameObject.SetActive(enabled);
            }

            if (enabled)
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

            if (mirrorController != null)
            {
                mirrorController.Configure(displayMaterial);
                mirrorController.SetSourceTexture(sourceTexture);
            }
        }

        private void EnsureRenderTexture()
        {
            int targetWidth = Mathf.Max(256, Mathf.RoundToInt(baseResolution * resolutionScale));
            int targetHeight = Mathf.Max(256, Mathf.RoundToInt(targetWidth / Mathf.Max(0.1f, mainCamera != null ? mainCamera.aspect : 1.777f)));

            if (sourceTexture != null && sourceTexture.width == targetWidth && sourceTexture.height == targetHeight)
            {
                return;
            }

            if (sourceTexture != null)
            {
                sourceTexture.Release();
                Destroy(sourceTexture);
            }

            sourceTexture = new RenderTexture(targetWidth, targetHeight, 24, useHdr ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default)
            {
                name = "Runtime Kaleidoscope Source",
                antiAliasing = 1,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                useMipMap = false
            };
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

            GameObject sourceObject = new GameObject("SourceCamera");
            sourceObject.transform.SetParent(transform, false);
            sourceCamera = sourceObject.AddComponent<Camera>();
            sourceCamera.enabled = false;
            sourceCamera.targetTexture = sourceTexture;
            sourceCamera.cullingMask = mainCamera.cullingMask & ~(1 << DisplayLayer);
            sourceCamera.depth = mainCamera.depth - 10f;
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
            displayObject.SetActive(kaleidoscopeView);
            UpdateDisplayQuad();
        }

        private void SyncSourceCamera()
        {
            if (sourceCamera == null || mainCamera == null)
            {
                return;
            }

            sourceCamera.transform.SetPositionAndRotation(mainCamera.transform.position, mainCamera.transform.rotation);
            sourceCamera.fieldOfView = mainCamera.fieldOfView;
            sourceCamera.nearClipPlane = mainCamera.nearClipPlane;
            sourceCamera.farClipPlane = mainCamera.farClipPlane;
            sourceCamera.clearFlags = mainCamera.clearFlags == CameraClearFlags.SolidColor && kaleidoscopeView
                ? originalClearFlags
                : mainCamera.clearFlags;
            sourceCamera.backgroundColor = originalBackgroundColor;
            sourceCamera.allowHDR = mainCamera.allowHDR;
            sourceCamera.allowMSAA = false;
            sourceCamera.cullingMask = (originalMainCullingMask >= 0 ? originalMainCullingMask : mainCamera.cullingMask) & ~(1 << DisplayLayer);
            sourceCamera.targetTexture = sourceTexture;
        }

        private void UpdateDisplayQuad()
        {
            if (displayRenderer == null || mainCamera == null)
            {
                return;
            }

            Transform quad = displayRenderer.transform;
            quad.localPosition = new Vector3(0f, 0f, displayDistance);
            quad.localRotation = Quaternion.identity;

            float height = 2f * Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * displayDistance;
            float width = height * mainCamera.aspect;
            quad.localScale = new Vector3(width, height, 1f);
        }
    }
}
