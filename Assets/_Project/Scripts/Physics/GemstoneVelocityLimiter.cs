using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class GemstoneVelocityLimiter : MonoBehaviour
    {
        [SerializeField] private Rigidbody targetBody;
        [SerializeField] private float maxLinearVelocity = 5.5f;
        [SerializeField] private float maxAngularVelocity = 8f;

        public void Configure(Rigidbody body, float linearLimit, float angularLimit)
        {
            targetBody = body;
            maxLinearVelocity = Mathf.Max(0.1f, linearLimit);
            maxAngularVelocity = Mathf.Max(0.1f, angularLimit);
        }

        private void FixedUpdate()
        {
            if (targetBody == null)
            {
                targetBody = GetComponent<Rigidbody>();
                if (targetBody == null)
                {
                    return;
                }
            }

            targetBody.velocity = Vector3.ClampMagnitude(targetBody.velocity, maxLinearVelocity);
            targetBody.angularVelocity = Vector3.ClampMagnitude(targetBody.angularVelocity, maxAngularVelocity);
        }
    }
}
