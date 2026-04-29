using UnityEngine;

namespace KaleidoscopeEngine.Mirrors
{
    public enum KaleidoscopeViewerCompositionMode
    {
        Eyepiece,
        FullFrame,
        SafePreview
    }

    /// <summary>
    /// ViewerCamera role: this is the user's eye/eyepiece camera.
    /// It presents the final KaleidoscopeDisplay quad as the primary screen,
    /// while source and debug cameras remain implementation/detail views.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeViewerCameraController : MonoBehaviour
    {
        [Header("Viewer")]
        [SerializeField] private Camera viewerCamera;
        [SerializeField] private float viewerDistance = 0f;
        [SerializeField, Range(20f, 75f)] private float viewerFov = 42f;
        [SerializeField, Range(0.65f, 1f)] private float displayFillAmount = 0.97f;
        [SerializeField] private KaleidoscopeViewerCompositionMode viewerCompositionMode = KaleidoscopeViewerCompositionMode.Eyepiece;
        [SerializeField, Range(-0.12f, 0.12f)] private float autoCenterBias;
        [SerializeField, Range(0.8f, 1.03f)] private float fillFrameAmount = 0.98f;
        [SerializeField] private float framingRotation;
        [SerializeField] private float framingRotationSpeed = 32f;
        [SerializeField] private float viewerZoomSpeed = 0.36f;
        [SerializeField] private float displayPlaneDistance = 1.05f;
        [SerializeField] private bool lockToDisplay = true;
        [SerializeField] private bool autoAlignOnStart = true;
        [SerializeField] private bool safeFraming = true;

        private Renderer displayRenderer;
        private bool viewerModeActive;

        public float DisplayPlaneDistance => displayPlaneDistance;
        public float DisplayFillAmount => displayFillAmount;
        public float FillFrameAmount => fillFrameAmount;
        public string ViewerCompositionModeName => viewerCompositionMode.ToString();
        public float FramingRotation => framingRotation;
        public bool ViewerModeActive => viewerModeActive;

        public void Configure(Camera camera, Renderer kaleidoscopeDisplay)
        {
            viewerCamera = camera != null ? camera : Camera.main;
            displayRenderer = kaleidoscopeDisplay;

            if (autoAlignOnStart)
            {
                AlignDisplayToViewerCamera();
            }
        }

        public void SetViewerModeActive(bool active)
        {
            viewerModeActive = active;
            if (active)
            {
                AlignDisplayToViewerCamera();
            }
        }

        public void AdjustFramingRotation(float direction)
        {
            framingRotation += direction * framingRotationSpeed * Time.deltaTime;
            AlignDisplayToViewerCamera();
        }

        public void AdjustViewerZoom(float direction)
        {
            fillFrameAmount = Mathf.Clamp(fillFrameAmount + direction * viewerZoomSpeed * Time.deltaTime, 0.8f, 1.03f);
            AlignDisplayToViewerCamera();
        }

        public void ResetViewerComposition()
        {
            viewerCompositionMode = KaleidoscopeViewerCompositionMode.Eyepiece;
            autoCenterBias = 0f;
            fillFrameAmount = 0.98f;
            framingRotation = 0f;
            AlignDisplayToViewerCamera();
        }

        private void LateUpdate()
        {
            if (viewerModeActive && lockToDisplay)
            {
                AlignDisplayToViewerCamera();
            }
        }

        public void AlignDisplayToViewerCamera()
        {
            if (viewerCamera == null || displayRenderer == null)
            {
                return;
            }

            if (viewerModeActive)
            {
                viewerCamera.fieldOfView = viewerFov;
                viewerCamera.clearFlags = CameraClearFlags.SolidColor;
                viewerCamera.backgroundColor = Color.black;
            }

            Transform display = displayRenderer.transform;
            if (display.parent != viewerCamera.transform)
            {
                display.SetParent(viewerCamera.transform, false);
            }

            float safeDistance = Mathf.Max(0.05f, displayPlaneDistance);
            display.localRotation = Quaternion.Euler(0f, 0f, framingRotation);

            float requestedFill = Mathf.Max(displayFillAmount, fillFrameAmount);
            if (viewerCompositionMode == KaleidoscopeViewerCompositionMode.FullFrame)
            {
                requestedFill = Mathf.Max(requestedFill, 1f);
            }
            else if (viewerCompositionMode == KaleidoscopeViewerCompositionMode.SafePreview)
            {
                requestedFill = Mathf.Min(requestedFill, 0.92f);
            }

            float fill = safeFraming ? Mathf.Clamp(requestedFill, 0.65f, 1.0f) : requestedFill;
            float height = 2f * Mathf.Tan(viewerCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * safeDistance * fill;
            float width = height * Mathf.Max(0.1f, viewerCamera.aspect);
            display.localPosition = new Vector3(0f, autoCenterBias * height, safeDistance);
            display.localScale = new Vector3(width, height, 1f);

            if (!Mathf.Approximately(viewerDistance, 0f) && viewerModeActive)
            {
                viewerCamera.nearClipPlane = Mathf.Min(viewerCamera.nearClipPlane, safeDistance * 0.5f);
            }
        }
    }
}
