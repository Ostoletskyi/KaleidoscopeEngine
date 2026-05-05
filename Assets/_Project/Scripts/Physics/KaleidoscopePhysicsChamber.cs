using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopePhysicsChamber : MonoBehaviour
    {
        private const float MinimumAxialSpeed = -200f;
        private const float MaximumAxialSpeed = 200f;

        [Header("References")]
        [Tooltip("Transform that owns the chamber colliders and visible shell.")]
        [SerializeField] private Transform chamberTransform;

        [Header("Tilt")]
        [SerializeField] private float maxTiltDegrees = 28f;
        [SerializeField] private float tiltResponsiveness = 5f;

        [Header("Axial Tube Rotation")]
        [SerializeField] private bool axialRotationEnabled = true;
        [SerializeField] private float axialRotationSpeed = 12f;
        [SerializeField] private float defaultAxialRotationSpeed = 12f;
        [SerializeField] private float axialRotationDirection = 1f;
        [SerializeField] private float minSpeed = -200f;
        [SerializeField] private float maxSpeed = 200f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float damping = 5f;
        [SerializeField, Range(0.1f, 3f)] private float angularSmoothing = 0.55f;
        [SerializeField, Range(0.5f, 8f)] private float rotationalMass = 3.2f;
        [SerializeField, Range(0f, 1f)] private float opticalMomentum = 0.72f;

        [Header("World Rotation")]
        [SerializeField] private bool worldRotationEnabled;
        [SerializeField] private float maxManualWorldRotationSpeed = 45f;
        [SerializeField] private float passiveWorldRotationSpeed;

        [Header("Future Turbine Placeholder")]
        [SerializeField] private bool turbineModeEnabled;
        [SerializeField] private int turbineBladeCount = 12;
        [SerializeField] private float turbineSpinSpeed = 720f;
        // Future turbine mode will spin entrance blades against 24/48/72 fps multiples
        // to create a stroboscopic cyclic effect. This is intentionally not implemented in Stage 1.

        [Header("Procedural Drift")]
        [SerializeField] private bool proceduralDrift = true;
        [SerializeField] private float driftTiltDegrees = 2.4f;
        [SerializeField] private float driftPositionAmplitude = 0.018f;
        [SerializeField] private float driftFrequency = 0.23f;

        [Header("Micro Vibration")]
        [SerializeField] private bool microVibration;
        [SerializeField] private float vibrationRotationDegrees = 0.04f;
        [SerializeField] private float vibrationPositionAmplitude = 0.001f;
        [SerializeField] private float vibrationFrequency = 3.5f;

        [Header("Shake")]
        [SerializeField] private float shakeDuration = 0.28f;
        [SerializeField] private float shakePositionAmplitude = 0.055f;
        [SerializeField] private float shakeRotationAmplitude = 4.5f;
        [SerializeField] private float shakeFrequency = 34f;

        private Quaternion initialRotation;
        private Vector3 initialPosition;
        private Vector2 targetTiltInput;
        private Vector2 currentTiltInput;
        private float requestedWorldRotationSpeed;
        private float accumulatedWorldSpin;
        private float targetAxialRotationSpeed;
        private float currentAxialRotationSpeed;
        private float accumulatedAxialSpin;
        private float shakeTimer;
        private float shakeStrength;
        private Rigidbody chamberBody;
        private float adaptiveAxialSpeedCap = -1f;

        public Vector2 CurrentTiltInput => currentTiltInput;
        public float RequestedRotationSpeed => requestedWorldRotationSpeed;
        public bool AxialRotationEnabled => axialRotationEnabled;
        public float AxialRotationSpeed => axialRotationSpeed;
        public float TubeAxialRotationSpeedDeg => axialRotationSpeed;
        public float RequestedTubeAxialSpeedDeg => axialRotationSpeed;
        public float EffectiveTubeAxialSpeedDeg => currentAxialRotationSpeed;
        public float EffectiveAxialRotationSpeedCap => adaptiveAxialSpeedCap >= 0f ? adaptiveAxialSpeedCap : maxSpeed;
        public float MinAxialRotationSpeed => minSpeed;
        public float MaxAxialRotationSpeed => maxSpeed;
        public float RotationAcceleration => acceleration;
        public float RotationDamping => damping;
        public float RotationSmoothing => angularSmoothing;
        public float AngularSmoothing => angularSmoothing;
        public float RotationalMass => rotationalMass;
        public float OpticalMomentum => opticalMomentum;
        public float CurrentAxialRotationSpeed => currentAxialRotationSpeed;
        public bool WorldRotationEnabled => worldRotationEnabled;
        public bool TurbineModeEnabled => turbineModeEnabled;
        public int TurbineBladeCount => turbineBladeCount;
        public float TurbineSpinSpeed => turbineSpinSpeed;
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
            EnforceAxialSpeedRange();
            chamberTransform = ChamberTransform;
            initialRotation = chamberTransform.localRotation;
            initialPosition = chamberTransform.localPosition;
            PrepareKinematicBody();
        }

        private void OnValidate()
        {
            EnforceAxialSpeedRange();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            currentTiltInput = Vector2.Lerp(currentTiltInput, targetTiltInput, 1f - Mathf.Exp(-tiltResponsiveness * dt));

            float effectiveAxialSpeed = adaptiveAxialSpeedCap >= 0f
                ? Mathf.Min(Mathf.Abs(axialRotationSpeed), adaptiveAxialSpeedCap) * Mathf.Sign(axialRotationSpeed)
                : axialRotationSpeed;
            targetAxialRotationSpeed = axialRotationEnabled ? effectiveAxialSpeed * Mathf.Sign(Mathf.Approximately(axialRotationDirection, 0f) ? 1f : axialRotationDirection) : 0f;
            float speedResponse = (axialRotationEnabled ? acceleration : damping) * angularSmoothing / Mathf.Max(0.1f, rotationalMass);
            currentAxialRotationSpeed = Mathf.MoveTowards(currentAxialRotationSpeed, targetAxialRotationSpeed, speedResponse * dt);
            currentAxialRotationSpeed = Mathf.Lerp(currentAxialRotationSpeed, targetAxialRotationSpeed, (1f - opticalMomentum) * 0.08f);
            accumulatedAxialSpin += currentAxialRotationSpeed * dt;

            if (worldRotationEnabled)
            {
                accumulatedWorldSpin += (passiveWorldRotationSpeed + requestedWorldRotationSpeed * maxManualWorldRotationSpeed) * dt;
            }

            Quaternion tilt = Quaternion.Euler(
                currentTiltInput.y * maxTiltDegrees,
                0f,
                -currentTiltInput.x * maxTiltDegrees);
            Quaternion worldSpin = Quaternion.AngleAxis(accumulatedWorldSpin, Vector3.up);
            Quaternion axialSpin = Quaternion.AngleAxis(accumulatedAxialSpin, Vector3.right);

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
                initialRotation * worldSpin * tilt * driftRotation * axialSpin * vibrationRotation * shakeRotation);
        }

        public void Tilt(Vector2 input)
        {
            targetTiltInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void Rotate(float speed)
        {
            requestedWorldRotationSpeed = Mathf.Clamp(speed, -1f, 1f);
        }

        public void AdjustAxialRotationSpeed(float delta)
        {
            EnforceAxialSpeedRange();
            axialRotationSpeed = Mathf.Clamp(axialRotationSpeed + delta, minSpeed, maxSpeed);
            axialRotationEnabled = !Mathf.Approximately(axialRotationSpeed, 0f);
        }

        public void SetAxialRotationSpeed(float speed)
        {
            EnforceAxialSpeedRange();
            axialRotationSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
            axialRotationEnabled = !Mathf.Approximately(axialRotationSpeed, 0f);
        }

        public void StopAxialRotation()
        {
            axialRotationSpeed = 0f;
            axialRotationEnabled = false;
        }

        public void RestoreDefaultAxialRotation()
        {
            axialRotationSpeed = Mathf.Clamp(defaultAxialRotationSpeed, minSpeed, maxSpeed);
            axialRotationEnabled = true;
        }

        public void ApplyTemporalStability(float motionDamping, float smoothing, float maxComfortSpeed)
        {
            angularSmoothing = Mathf.Clamp01(Mathf.Max(angularSmoothing, smoothing));
            opticalMomentum = Mathf.Clamp01(Mathf.Max(opticalMomentum, motionDamping));
            acceleration = Mathf.Min(acceleration, Mathf.Lerp(12f, 4f, motionDamping));
            damping = Mathf.Max(damping, Mathf.Lerp(5f, 12f, motionDamping));
            vibrationFrequency = Mathf.Min(vibrationFrequency, 7f);
            vibrationRotationDegrees = Mathf.Min(vibrationRotationDegrees, 0.08f);
            vibrationPositionAmplitude = Mathf.Min(vibrationPositionAmplitude, 0.0018f);
            SetAdaptiveAxialSpeedCap(maxComfortSpeed);
        }

        public void ApplyEmergencyMotionStability()
        {
            SetAxialRotationSpeed(Mathf.Clamp(axialRotationSpeed, -8f, 8f));
            SetAdaptiveAxialSpeedCap(8f);
            acceleration = Mathf.Min(acceleration, 4f);
            damping = Mathf.Max(damping, 12f);
            angularSmoothing = Mathf.Max(angularSmoothing, 0.85f);
            opticalMomentum = Mathf.Max(opticalMomentum, 0.88f);
            microVibration = false;
        }

        public void ToggleAxialRotation()
        {
            axialRotationEnabled = !axialRotationEnabled;
        }

        public void SetAxialRotationEnabled(bool enabled)
        {
            axialRotationEnabled = enabled;
        }

        public void SetAdaptiveAxialSpeedCap(float cap)
        {
            EnforceAxialSpeedRange();
            adaptiveAxialSpeedCap = Mathf.Clamp(cap, 0f, Mathf.Max(0f, maxSpeed));
        }

        public void ClearAdaptiveAxialSpeedCap()
        {
            adaptiveAxialSpeedCap = -1f;
        }

        public void ToggleWorldRotation()
        {
            worldRotationEnabled = !worldRotationEnabled;
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
            requestedWorldRotationSpeed = 0f;
            accumulatedWorldSpin = 0f;
            accumulatedAxialSpin = 0f;
            currentAxialRotationSpeed = 0f;
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

        private void EnforceAxialSpeedRange()
        {
            minSpeed = MinimumAxialSpeed;
            maxSpeed = MaximumAxialSpeed;
            axialRotationSpeed = Mathf.Clamp(axialRotationSpeed, minSpeed, maxSpeed);
            defaultAxialRotationSpeed = Mathf.Clamp(defaultAxialRotationSpeed, minSpeed, maxSpeed);
            if (adaptiveAxialSpeedCap >= 0f)
            {
                adaptiveAxialSpeedCap = Mathf.Clamp(adaptiveAxialSpeedCap, 0f, maxSpeed);
            }
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
