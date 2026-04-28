using System.Collections.Generic;
using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class PhysicsSandboxMetrics : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private Transform chamberTransform;

        [Header("Bounds")]
        [SerializeField] private Vector3 chamberInnerSize = new Vector3(4.2f, 2.6f, 4.2f);
        [SerializeField] private bool useTubeBounds;
        [SerializeField] private float tubeRadius = 1.28f;
        [SerializeField] private float tubeLength = 5.4f;
        [SerializeField] private float escapeTolerance = 0.45f;
        [SerializeField] private bool tintEscapedObjects = true;
        [SerializeField] private Color escapedColor = new Color(1f, 0.82f, 0.05f, 1f);

        [Header("Sampling")]
        [SerializeField] private float sampleInterval = 0.2f;

        private readonly HashSet<GameObject> escapedObjects = new HashSet<GameObject>();
        private float nextSampleTime;

        public int VisibleGemCount { get; private set; }
        public int EscapedGemCount => escapedObjects.Count;
        public int SleepingBodyCount { get; private set; }
        public float AverageVelocity { get; private set; }

        public void Configure(GemstoneSpawner gemstoneSpawner, Transform chamber, Vector3 innerSize)
        {
            spawner = gemstoneSpawner;
            chamberTransform = chamber;
            chamberInnerSize = innerSize;
            useTubeBounds = false;
        }

        public void ConfigureTube(GemstoneSpawner gemstoneSpawner, Transform chamber, float radius, float length)
        {
            spawner = gemstoneSpawner;
            chamberTransform = chamber;
            tubeRadius = Mathf.Max(0.1f, radius);
            tubeLength = Mathf.Max(0.1f, length);
            useTubeBounds = true;
        }

        private void Update()
        {
            if (Time.time < nextSampleTime)
            {
                return;
            }

            nextSampleTime = Time.time + sampleInterval;
            Sample();
        }

        public void ResetEscapes()
        {
            escapedObjects.Clear();
        }

        private void Sample()
        {
            VisibleGemCount = 0;
            SleepingBodyCount = 0;
            AverageVelocity = 0f;

            if (spawner == null)
            {
                return;
            }

            int velocitySamples = 0;
            foreach (GameObject spawnedObject in spawner.SpawnedObjects)
            {
                if (spawnedObject == null || !spawnedObject.activeInHierarchy)
                {
                    continue;
                }

                Renderer renderer = spawnedObject.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    VisibleGemCount++;
                }

                Rigidbody body = spawnedObject.GetComponent<Rigidbody>();
                if (body != null)
                {
                    AverageVelocity += body.velocity.magnitude;
                    velocitySamples++;

                    if (body.IsSleeping())
                    {
                        SleepingBodyCount++;
                    }
                }

                if (!escapedObjects.Contains(spawnedObject) && HasEscaped(spawnedObject.transform.position))
                {
                    escapedObjects.Add(spawnedObject);
                    MarkEscaped(spawnedObject);
                    Debug.LogWarning($"{spawnedObject.name} escaped the physics chamber.", spawnedObject);
                }
            }

            if (velocitySamples > 0)
            {
                AverageVelocity /= velocitySamples;
            }
        }

        private bool HasEscaped(Vector3 worldPosition)
        {
            if (chamberTransform == null)
            {
                return false;
            }

            Vector3 local = chamberTransform.InverseTransformPoint(worldPosition);
            if (useTubeBounds)
            {
                float radialDistance = new Vector2(local.y, local.z).magnitude;
                return Mathf.Abs(local.x) > tubeLength * 0.5f + escapeTolerance ||
                       radialDistance > tubeRadius + escapeTolerance;
            }

            Vector3 half = chamberInnerSize * 0.5f + Vector3.one * escapeTolerance;
            return Mathf.Abs(local.x) > half.x ||
                   Mathf.Abs(local.y) > half.y ||
                   Mathf.Abs(local.z) > half.z;
        }

        private void MarkEscaped(GameObject escapedObject)
        {
            if (!tintEscapedObjects)
            {
                return;
            }

            Renderer renderer = escapedObject.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material material = renderer.material;
            material.color = escapedColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", escapedColor);
            }
        }
    }
}
