using UnityEngine;

namespace KaleidoscopeEngine.Lighting
{
    [DisallowMultipleComponent]
    public sealed class LightOrbitController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float orbitRadius = 3.8f;
        [SerializeField] private float orbitSpeed = 24f;
        [SerializeField] private float verticalOffset = 0.8f;
        [SerializeField] private float verticalDrift = 0.25f;
        [SerializeField] private bool randomDrift = true;

        private float angle;
        private float phase;

        public void Configure(Transform orbitTarget, float radius, float speed, float offset, int seed)
        {
            target = orbitTarget;
            orbitRadius = radius;
            orbitSpeed = speed;
            verticalOffset = offset;
            phase = seed * 0.173f;
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            angle += orbitSpeed * Time.deltaTime;
            float radians = angle * Mathf.Deg2Rad;
            float drift = randomDrift ? Mathf.Sin(Time.time * 0.73f + phase) * verticalDrift : 0f;
            Vector3 position = target.position + new Vector3(
                Mathf.Cos(radians) * orbitRadius,
                verticalOffset + drift,
                Mathf.Sin(radians) * orbitRadius);

            transform.position = position;
            transform.LookAt(target.position + Vector3.up * 0.1f);
        }
    }
}
