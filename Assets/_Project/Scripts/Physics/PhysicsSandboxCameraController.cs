using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public enum PhysicsSandboxCameraMode
    {
        ThreeQuarter,
        Orbit
    }

    [DisallowMultipleComponent]
    public sealed class PhysicsSandboxCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform target;

        [Header("Framing")]
        [SerializeField] private float zoomDistance = 7.2f;
        [SerializeField] private float minZoomDistance = 4.2f;
        [SerializeField] private float maxZoomDistance = 12f;
        [SerializeField] private float targetHeight = 0.2f;
        [SerializeField] private float pitchDegrees = 24f;

        [Header("Orbit")]
        [SerializeField] private PhysicsSandboxCameraMode cameraMode = PhysicsSandboxCameraMode.ThreeQuarter;
        [SerializeField] private float staticYawDegrees = 42f;
        [SerializeField] private float orbitSpeedDegrees = 10f;
        [SerializeField] private float manualOrbitSpeedDegrees = 75f;
        [SerializeField] private float scrollZoomSpeed = 1.4f;

        private float yawDegrees;

        public string ModeName => cameraMode.ToString();
        public float ZoomDistance => zoomDistance;

        public void Configure(Camera cameraToControl, Transform lookTarget)
        {
            targetCamera = cameraToControl;
            target = lookTarget;
            yawDegrees = staticYawDegrees;
            ApplyCameraPose();
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
                zoomDistance = Mathf.Clamp(zoomDistance - scroll * scrollZoomSpeed, minZoomDistance, maxZoomDistance);
            }

            if (cameraMode == PhysicsSandboxCameraMode.Orbit)
            {
                yawDegrees += orbitSpeedDegrees * Time.deltaTime;
            }

            if (Input.GetMouseButton(1))
            {
                yawDegrees += Input.GetAxisRaw("Mouse X") * manualOrbitSpeedDegrees * Time.deltaTime;
                pitchDegrees = Mathf.Clamp(
                    pitchDegrees - Input.GetAxisRaw("Mouse Y") * manualOrbitSpeedDegrees * 0.45f * Time.deltaTime,
                    8f,
                    62f);
            }

            ApplyCameraPose();
        }

        public void ToggleMode()
        {
            cameraMode = cameraMode == PhysicsSandboxCameraMode.ThreeQuarter
                ? PhysicsSandboxCameraMode.Orbit
                : PhysicsSandboxCameraMode.ThreeQuarter;

            if (cameraMode == PhysicsSandboxCameraMode.ThreeQuarter)
            {
                yawDegrees = staticYawDegrees;
            }
        }

        private void ApplyCameraPose()
        {
            Vector3 lookAt = target.position + Vector3.up * targetHeight;
            Quaternion rotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -zoomDistance);

            targetCamera.transform.position = lookAt + offset;
            targetCamera.transform.rotation = Quaternion.LookRotation(lookAt - targetCamera.transform.position, Vector3.up);
            targetCamera.fieldOfView = 42f;
            targetCamera.nearClipPlane = 0.03f;
        }
    }
}
