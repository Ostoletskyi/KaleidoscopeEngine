using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class GemstoneMotionPersonality : MonoBehaviour
    {
        [SerializeField] private GemstoneDefinition definition;
        [SerializeField] private Rigidbody body;
        [SerializeField] private float phase;
        [SerializeField] private float wakeCheckInterval = 0.65f;

        private float nextWakeCheck;
        private Vector3 localInertiaBias;

        public void Configure(GemstoneDefinition gemstoneDefinition, Rigidbody rigidbody, System.Random random)
        {
            definition = gemstoneDefinition;
            body = rigidbody;
            phase = (float)random.NextDouble() * 1000f;
            localInertiaBias = RandomUnitVector(random) * definition.inertiaLag;
            nextWakeCheck = Time.time + RandomRange(random, 0f, wakeCheckInterval);
        }

        private void FixedUpdate()
        {
            if (definition == null || body == null)
            {
                return;
            }

            ApplyInertiaLayer();
            PreventDeadSleep();
        }

        private void ApplyInertiaLayer()
        {
            if (definition.inertiaLag <= 0f || body.velocity.sqrMagnitude < 0.0004f)
            {
                return;
            }

            Vector3 lateral = Vector3.ProjectOnPlane(transform.TransformDirection(localInertiaBias), Vector3.up);
            body.AddForce(lateral * definition.inertiaLag * body.mass * 0.018f, ForceMode.Force);
        }

        private void PreventDeadSleep()
        {
            if (Time.time < nextWakeCheck)
            {
                return;
            }

            nextWakeCheck = Time.time + wakeCheckInterval;
            float speed = body.velocity.magnitude + body.angularVelocity.magnitude * 0.08f;
            if (speed > definition.lowMotionWakeThreshold)
            {
                return;
            }

            float pulse = Mathf.Sin(Time.time * 2.7f + phase) * 0.5f + 0.5f;
            Vector3 direction = Vector3.ProjectOnPlane(transform.right + transform.forward * pulse, Vector3.up).normalized;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.right;
            }

            float impulse = definition.restlessness * body.mass;
            body.WakeUp();
            body.AddForce(direction * impulse, ForceMode.Impulse);
            body.AddTorque((transform.up + transform.right * pulse).normalized * impulse * 0.7f, ForceMode.Impulse);
        }

        private static Vector3 RandomUnitVector(System.Random random)
        {
            Vector3 vector = new Vector3(
                RandomRange(random, -1f, 1f),
                RandomRange(random, -1f, 1f),
                RandomRange(random, -1f, 1f));

            return vector.sqrMagnitude > 0.001f ? vector.normalized : Vector3.right;
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
