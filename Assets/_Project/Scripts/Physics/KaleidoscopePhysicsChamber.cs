using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopePhysicsChamber : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Transform that owns the chamber colliders and visible shell.")]
        [SerializeField] private Transform chamberTransform;

        [Header("Tilt")]
        [SerializeField] private float maxTiltDegrees = 28f;
        [SerializeField] private float tiltResponsiveness = 5f;

        [Header("Rotation")]
        [SerializeField] private float maxManualRotationSpeed = 75f;
        [SerializeField] private float passiveRotationSpeed = 4f;

        [Header("Procedural Drift")]
        [SerializeField] private bool proceduralDrift = true;
        [SerializeField] private float driftTiltDegrees = 2.4f;
        [SerializeField] private float driftPositionAmplitude = 0.018f;
        [SerializeField] private float driftFrequency = 0.23f;

        [Header("Micro Vibration")]
        [SerializeField] private bool microVibration = true;
        [SerializeField] private float vibrationRotationDegrees = 0.18f;
        [SerializeField] private float vibrationPositionAmplitude = 0.004f;
        [SerializeField] private float vibrationFrequency = 19f;

        [Header("Shake")]
        [SerializeField] private float shakeDuration = 0.28f;
        [SerializeField] private float shakePositionAmplitude = 0.055f;
        [SerializeField] private float shakeRotationAmplitude = 4.5f;
        [SerializeField] private float shakeFrequency = 34f;

        private Quaternion initialRotation;
        private Vector3 initialPosition;
        private Vector2 targetTiltInput;
        private Vector2 currentTiltInput;
        private float requestedRotationSpeed;
        private float accumulatedSpin;
        private float shakeTimer;
        private float shakeStrength;
        private Rigidbody chamberBody;

        public Vector2 CurrentTiltInput => currentTiltInput;
        public float RequestedRotationSpeed => requestedRotationSpeed;
        public float ShakeStrength => shakeTimer > 0f ? shakeStrength : 0f;

        private Transform ChamberTransform => chamberTransform != null ? chamberTransform : transform;

        public void SetChamberTransform(Transform target)
        {
            chamberTransform = target != null ? target : transform;
            initialRotation = chamberTransform.localRotation;
            initialPosition = chamberTransform.localPosition;
            PrepareKinematicBody();
        }

        private void Awake()
        {
            chamberTransform = ChamberTransform;
            initialRotation = chamberTransform.localRotation;
            initialPosition = chamberTransform.localPosition;
            PrepareKinematicBody();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            currentTiltInput = Vector2.Lerp(currentTiltInput, targetTiltInput, 1f - Mathf.Exp(-tiltResponsiveness * dt));
            accumulatedSpin += (passiveRotationSpeed + requestedRotationSpeed * maxManualRotationSpeed) * dt;

            Quaternion tilt = Quaternion.Euler(
                currentTiltInput.y * maxTiltDegrees,
                0f,
                -currentTiltInput.x * maxTiltDegrees);
            Quaternion spin = Quaternion.AngleAxis(accumulatedSpin, Vector3.up);

            Vector3 driftPosition = Vector3.zero;
            Quaternion driftRotation = Quaternion.identity;
            if (proceduralDrift)
            {
                float t = Time.time * driftFrequency;
                float x = Mathf.PerlinNoise(t, 1.7f) - 0.5f;
                float z = Mathf.PerlinNoise(3.1f, t) - 0.5f;
                float yaw = Mathf.Sin(t * 0.63f) * driftTiltDegrees * 0.45f;

                driftPosition = new Vector3(x, 0f, z) * driftPositionAmplitude;
                driftRotation = Quaternion.Euler(z * driftTiltDegrees, yaw, -x * driftTiltDegrees);
            }

            Vector3 vibrationPosition = Vector3.zero;
            Quaternion vibrationRotation = Quaternion.identity;
            if (microVibration)
            {
                float t = Time.time * vibrationFrequency;
                float waveA = Mathf.Sin(t);
                float waveB = Mathf.Sin(t * 1.71f + 0.9f);

                vibrationPosition = new Vector3(waveB, waveA * 0.35f, -waveA) * vibrationPositionAmplitude;
                vibrationRotation = Quaternion.Euler(
                    waveA * vibrationRotationDegrees,
                    waveB * vibrationRotationDegrees * 0.6f,
                    -waveB * vibrationRotationDegrees);
            }

            Vector3 shakePosition = Vector3.zero;
            Quaternion shakeRotation = Quaternion.identity;
            if (shakeTimer > 0f)
            {
                shakeTimer = Mathf.Max(0f, shakeTimer - dt);
                float envelope = Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, shakeDuration));
                float wave = Mathf.Sin(Time.time * shakeFrequency);
                float noise = Mathf.PerlinNoise(Time.time * shakeFrequency, 0.37f) - 0.5f;
                float amount = shakeStrength * envelope;

                shakePosition = new Vector3(noise, wave * 0.5f, -noise) * shakePositionAmplitude * amount;
                shakeRotation = Quaternion.Euler(
                    wave * shakeRotationAmplitude * amount,
                    noise * shakeRotationAmplitude * amount,
                    -wave * shakeRotationAmplitude * 0.7f * amount);
            }

            MoveChamber(
                initialPosition + driftPosition + vibrationPosition + shakePosition,
                initialRotation * tilt * driftRotation * spin * vibrationRotation * shakeRotation);
        }

        public void Tilt(Vector2 input)
        {
            targetTiltInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void Rotate(float speed)
        {
            requestedRotationSpeed = Mathf.Clamp(speed, -1f, 1f);
        }

        public void Shake(float strength)
        {
            shakeStrength = Mathf.Max(shakeStrength, Mathf.Max(0f, strength));
            shakeTimer = shakeDuration;
        }

        public void ResetPose()
        {
            targetTiltInput = Vector2.zero;
            currentTiltInput = Vector2.zero;
            requestedRotationSpeed = 0f;
            accumulatedSpin = 0f;
            shakeTimer = 0f;
            shakeStrength = 0f;
            MoveChamber(initialPosition, initialRotation);
        }

        private void PrepareKinematicBody()
        {
            if (chamberTransform == null)
            {
                return;
            }

            chamberBody = chamberTransform.GetComponent<Rigidbody>();
            if (chamberBody == null)
            {
                chamberBody = chamberTransform.gameObject.AddComponent<Rigidbody>();
            }

            chamberBody.isKinematic = true;
            chamberBody.useGravity = false;
            chamberBody.interpolation = RigidbodyInterpolation.Interpolate;
            chamberBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        private void MoveChamber(Vector3 localPosition, Quaternion localRotation)
        {
            Transform target = ChamberTransform;
            Transform parent = target.parent;

            if (chamberBody != null)
            {
                Vector3 worldPosition = parent != null ? parent.TransformPoint(localPosition) : localPosition;
                Quaternion worldRotation = parent != null ? parent.rotation * localRotation : localRotation;
                chamberBody.MovePosition(worldPosition);
                chamberBody.MoveRotation(worldRotation);
                return;
            }

            target.SetLocalPositionAndRotation(localPosition, localRotation);
        }
    }
}
