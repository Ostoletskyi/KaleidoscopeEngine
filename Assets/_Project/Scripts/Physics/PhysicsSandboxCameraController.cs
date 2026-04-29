using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public enum PhysicsSandboxCameraMode
    {
        Front,
        Orbit,
        DebugSide
    }

    [DisallowMultipleComponent]
    public sealed class PhysicsSandboxCameraController : MonoBehaviour
    {
        // DebugCamera / OrbitCamera role: this controller is for inspecting the
        // physical tube, lights, source chamber, and colliders. It is not the
        // default eyepiece presentation; KaleidoscopeRenderPipeline owns that.
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform target;

        [Header("Framing")]
        [SerializeField] private float cameraDistance = 7.4f;
        [SerializeField] private float minDistance = 2.6f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private float targetHeight;
        [SerializeField] private float pitchDegrees;
        [SerializeField] private bool followTubeCenter = true;
        [SerializeField] private float smoothing = 9f;

        [Header("Orbit")]
        [SerializeField] private PhysicsSandboxCameraMode cameraMode = PhysicsSandboxCameraMode.Front;
        [SerializeField] private float staticYawDegrees = 90f;
        [SerializeField] private float orbitSpeedDegrees = 10f;
        [SerializeField] private float orbitSensitivity = 120f;
        [SerializeField] private float zoomSensitivity = 1.4f;
        [SerializeField] private float panSensitivity = 0.015f;
        [SerializeField] private float debugSideYawDegrees = 36f;
        [SerializeField] private float debugSidePitchDegrees = 18f;

        private float yawDegrees;
        private Vector3 panOffset;
        private Vector3 currentPosition;
        private Quaternion currentRotation;

        public string ModeName => cameraMode == PhysicsSandboxCameraMode.DebugSide ? "Debug Side" : cameraMode.ToString();
        public float ZoomDistance => cameraDistance;
        public bool IsFrontFacing => cameraMode == PhysicsSandboxCameraMode.Front;

        public void Configure(Camera cameraToControl, Transform lookTarget)
        {
            targetCamera = cameraToControl;
            target = lookTarget;
            yawDegrees = staticYawDegrees;
            pitchDegrees = 0f;
            panOffset = Vector3.zero;
            currentPosition = targetCamera.transform.position;
            currentRotation = targetCamera.transform.rotation;
            ApplyCameraPose(true);
        }

        private void LateUpdate()
        {
            if (targetCamera == null || target == null)
            {
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.001f)
            {
                cameraDistance = Mathf.Clamp(cameraDistance - scroll * zoomSensitivity, minDistance, maxDistance);
            }

            if (cameraMode == PhysicsSandboxCameraMode.Orbit)
            {
                yawDegrees += orbitSpeedDegrees * Time.deltaTime;
            }

            if (Input.GetMouseButton(1))
            {
                if (cameraMode == PhysicsSandboxCameraMode.Front)
                {
                    cameraMode = PhysicsSandboxCameraMode.Orbit;
                }

                yawDegrees += Input.GetAxisRaw("Mouse X") * orbitSensitivity * Time.deltaTime;
                pitchDegrees = Mathf.Clamp(
                    pitchDegrees - Input.GetAxisRaw("Mouse Y") * orbitSensitivity * 0.45f * Time.deltaTime,
                    -62f,
                    62f);
            }

            if (Input.GetMouseButton(2))
            {
                Vector3 right = targetCamera.transform.right;
                Vector3 up = targetCamera.transform.up;
                panOffset += (-right * Input.GetAxisRaw("Mouse X") - up * Input.GetAxisRaw("Mouse Y")) * panSensitivity * cameraDistance;
            }

            ApplyCameraPose(false);
        }

        public void ToggleMode()
        {
            if (cameraMode == PhysicsSandboxCameraMode.Front)
            {
                cameraMode = PhysicsSandboxCameraMode.Orbit;
                return;
            }

            if (cameraMode == PhysicsSandboxCameraMode.Orbit)
            {
                cameraMode = PhysicsSandboxCameraMode.DebugSide;
                yawDegrees = debugSideYawDegrees;
                pitchDegrees = debugSidePitchDegrees;
                return;
            }

            ResetToFrontView();
        }

        public void ResetToFrontView()
        {
            cameraMode = PhysicsSandboxCameraMode.Front;
            yawDegrees = staticYawDegrees;
            pitchDegrees = 0f;
            panOffset = Vector3.zero;
            ApplyCameraPose(true);
        }

        public void SetDebugOrbitView()
        {
            cameraMode = PhysicsSandboxCameraMode.DebugSide;
            yawDegrees = debugSideYawDegrees;
            pitchDegrees = debugSidePitchDegrees;
            ApplyCameraPose(true);
        }

        private void ApplyCameraPose(bool immediate)
        {
            Vector3 targetCenter = followTubeCenter ? target.position : Vector3.zero;
            Vector3 lookAt = targetCenter + Vector3.up * targetHeight + panOffset;
            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -cameraDistance);

            Vector3 desiredPosition = lookAt + offset;
            Quaternion desiredRotation = Quaternion.LookRotation(lookAt - desiredPosition, Vector3.up);
            float blend = immediate || !Application.isPlaying ? 1f : 1f - Mathf.Exp(-smoothing * Time.deltaTime);

            currentPosition = Vector3.Lerp(currentPosition, desiredPosition, blend);
            currentRotation = Quaternion.Slerp(currentRotation, desiredRotation, blend);

            targetCamera.transform.position = currentPosition;
            targetCamera.transform.rotation = currentRotation;
            targetCamera.fieldOfView = 42f;
            targetCamera.nearClipPlane = 0.03f;
        }
    }
}
