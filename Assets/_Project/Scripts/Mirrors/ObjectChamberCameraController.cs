using UnityEngine;

namespace KaleidoscopeEngine.Mirrors
{
    /// <summary>
    /// SourceCamera role: this camera samples the physical object chamber into
    /// the RenderTexture. It is not the user's eyepiece except in SourcePreview.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectChamberCameraController : MonoBehaviour
    {
        [Header("Source Framing")]
        [SerializeField] private Camera sourceCamera;
        [SerializeField] private OpticalSourceChamber sourceChamber;
        [SerializeField] private float sourceDistance = 1.9f;
        [SerializeField, Range(15f, 80f)] private float sourceFov = 34f;
        [SerializeField, Range(0.5f, 2.5f)] private float sourceZoom = 1f;
        [SerializeField] private Vector2 sourceCenterOffset;
        [SerializeField] private float sourceOrbitAngle;
        [SerializeField] private Vector3 sourceLookAtTarget = new Vector3(1.65f, 0f, 0f);
        [SerializeField] private Vector3 objectChamberFocusPoint = new Vector3(1.65f, 0f, 0f);

        [Header("Source Inclusion")]
        [SerializeField] private bool includeRibs;
        [SerializeField] private bool includeTubeWalls;
        [SerializeField] private bool includeDiffuser = true;

        public bool IncludeRibs => includeRibs;
        public bool IncludeTubeWalls => includeTubeWalls;
        public bool IncludeDiffuser => includeDiffuser;
        public float SourceFov => sourceFov;
        public Vector3 ObjectChamberFocusPoint => objectChamberFocusPoint;
        public Vector2 SourceCenterOffset => sourceCenterOffset;
        public float SourceOrbitAngle => sourceOrbitAngle;

        public void Configure(Camera camera, OpticalSourceChamber chamber)
        {
            sourceCamera = camera;
            sourceChamber = chamber;
            AlignSourceCamera();
        }

        public void SetIncludeRibs(bool enabled)
        {
            includeRibs = enabled;
        }

        public void SetIncludeTubeWalls(bool enabled)
        {
            includeTubeWalls = enabled;
        }

        public void AdjustSourceOrbit(float deltaDegrees)
        {
            sourceOrbitAngle += deltaDegrees;
            AlignSourceCamera();
        }

        public void AdjustSourceFraming(float delta)
        {
            sourceCenterOffset.y = Mathf.Clamp(sourceCenterOffset.y + delta, -0.45f, 0.45f);
            AlignSourceCamera();
        }

        private void LateUpdate()
        {
            AlignSourceCamera();
        }

        public void AlignSourceCamera()
        {
            if (sourceCamera == null || sourceChamber == null)
            {
                return;
            }

            Transform sourceTransform = sourceChamber.transform;
            Vector3 focus = objectChamberFocusPoint;
            Vector2 rotatedOffset = RotateOffset(sourceCenterOffset, sourceOrbitAngle);
            Vector3 target = sourceLookAtTarget + new Vector3(0f, rotatedOffset.y, rotatedOffset.x);
            Vector3 localCameraPosition = new Vector3(focus.x - Mathf.Max(0.2f, sourceDistance), rotatedOffset.y, rotatedOffset.x);

            Vector3 worldCameraPosition = sourceTransform.TransformPoint(localCameraPosition);
            Vector3 worldTargetPosition = sourceTransform.TransformPoint(target);
            Vector3 forward = worldTargetPosition - worldCameraPosition;
            if (forward.sqrMagnitude < 0.001f)
            {
                forward = sourceTransform.right;
            }

            Quaternion lookRotation = Quaternion.LookRotation(forward.normalized, sourceTransform.up);
            Quaternion roll = Quaternion.AngleAxis(sourceOrbitAngle, forward.normalized);
            sourceCamera.transform.SetPositionAndRotation(worldCameraPosition, roll * lookRotation);
            sourceCamera.fieldOfView = sourceFov / Mathf.Max(0.1f, sourceZoom);
        }

        private static Vector2 RotateOffset(Vector2 offset, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float s = Mathf.Sin(radians);
            float c = Mathf.Cos(radians);
            return new Vector2(offset.x * c - offset.y * s, offset.x * s + offset.y * c);
        }
    }
}
