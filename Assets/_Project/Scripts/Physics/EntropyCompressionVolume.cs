using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class EntropyCompressionVolume : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform chamberTransform;
        [SerializeField] private GemstoneSpawner spawner;

        [Header("Compression Volume")]
        [SerializeField, Range(0.6f, 3.5f)] private float entropyDepthLimit = 1.72f;
        [SerializeField, Range(0f, 24f)] private float compressionStrength = 7.5f;
        [SerializeField, Range(0.05f, 0.9f)] private float compressionSoftness = 0.38f;
        [SerializeField, Range(0.08f, 1.1f)] private float rearBarrierOffset = 0.36f;
        [SerializeField, Range(0f, 1f)] private float objectPackingDensity = 0.72f;

        [Header("Bias Force")]
        [SerializeField] private bool softForwardBiasEnabled = true;
        [SerializeField, Range(0f, 1f)] private float radialPackingBias = 0.34f;
        [SerializeField, Range(0f, 1f)] private float wakeOutsideVolumeStrength = 0.55f;

        [Header("Hard Limits")]
        [SerializeField] private bool hardBarriersEnabled = true;
        [SerializeField, Range(0.02f, 0.4f)] private float barrierThickness = 0.12f;
        [SerializeField, Range(1f, 1.25f)] private float barrierRadiusPadding = 1.08f;
        [SerializeField] private string physicsOnlyLayerName = "KaleidoscopePhysicsOnly";

        private const float MinimumTubeRadius = 0.2f;
        private const float MinimumTubeLength = 0.8f;

        private float tubeRadius = 1f;
        private float tubeLength = 5f;
        private Transform volumeRoot;
        private Transform spawnAnchor;
        private BoxCollider frontBarrier;
        private BoxCollider rearBarrier;
        private PhysicMaterial barrierMaterial;

        public float EntropyDepthLimit => entropyDepthLimit;
        public float CompressionStrength => compressionStrength;
        public float CompressionSoftness => compressionSoftness;
        public float RearBarrierOffset => rearBarrierOffset;
        public float ObjectPackingDensity => objectPackingDensity;
        public bool SoftForwardBiasEnabled => softForwardBiasEnabled;
        public float EffectiveDepth => Mathf.Clamp(entropyDepthLimit * Mathf.Lerp(1.08f, 0.78f, objectPackingDensity), 0.55f, Mathf.Max(0.6f, tubeLength - rearBarrierOffset * 2f));
        public float EffectiveRadius => Mathf.Max(MinimumTubeRadius, tubeRadius * Mathf.Lerp(0.96f, 0.68f, objectPackingDensity));
        public float FrontLimitLocalX => RearLimitLocalX - EffectiveDepth;
        public float RearLimitLocalX => tubeLength * 0.5f - rearBarrierOffset;
        public float ZoneCenterLocalX => (FrontLimitLocalX + RearLimitLocalX) * 0.5f;

        public void Configure(
            Transform chamber,
            GemstoneSpawner gemstoneSpawner,
            float radius,
            float length,
            string physicsLayerName)
        {
            chamberTransform = chamber;
            spawner = gemstoneSpawner;
            tubeRadius = Mathf.Max(MinimumTubeRadius, radius);
            tubeLength = Mathf.Max(MinimumTubeLength, length);

            if (!string.IsNullOrWhiteSpace(physicsLayerName))
            {
                physicsOnlyLayerName = physicsLayerName;
            }

            BuildVolumeObjects();
            ApplyToSpawner(false);
        }

        public void ApplyToSpawner(bool respawn)
        {
            if (spawner == null || spawnAnchor == null)
            {
                return;
            }

            spawner.SetSpawnVolume(spawnAnchor);
            spawner.SetCylindricalSpawnVolume(EffectiveRadius, EffectiveDepth);
            if (respawn)
            {
                spawner.Respawn();
            }
        }

        private void Awake()
        {
            if (chamberTransform != null)
            {
                BuildVolumeObjects();
            }
        }

        private void OnValidate()
        {
            entropyDepthLimit = Mathf.Max(0.6f, entropyDepthLimit);
            compressionStrength = Mathf.Max(0f, compressionStrength);
            compressionSoftness = Mathf.Max(0.05f, compressionSoftness);
            rearBarrierOffset = Mathf.Max(0.08f, rearBarrierOffset);
            objectPackingDensity = Mathf.Clamp01(objectPackingDensity);
            radialPackingBias = Mathf.Clamp01(radialPackingBias);
            wakeOutsideVolumeStrength = Mathf.Clamp01(wakeOutsideVolumeStrength);
            barrierThickness = Mathf.Max(0.02f, barrierThickness);
            barrierRadiusPadding = Mathf.Max(1f, barrierRadiusPadding);
        }

        private void FixedUpdate()
        {
            if (chamberTransform == null || spawner == null || compressionStrength <= 0f)
            {
                return;
            }

            ApplyCompressionForces();
        }

        private void LateUpdate()
        {
            if (chamberTransform == null)
            {
                return;
            }

            BuildVolumeObjects();
            ApplyBarrierState();
        }

        private void BuildVolumeObjects()
        {
            if (chamberTransform == null)
            {
                return;
            }

            if (volumeRoot == null)
            {
                GameObject root = new GameObject("EntropyCompressionVolume");
                root.layer = ResolveLayer(physicsOnlyLayerName);
                root.transform.SetParent(chamberTransform, false);
                volumeRoot = root.transform;
            }

            if (spawnAnchor == null)
            {
                GameObject anchor = new GameObject("CompressedEntropySpawnAnchor");
                anchor.layer = ResolveLayer(physicsOnlyLayerName);
                anchor.transform.SetParent(volumeRoot, false);
                spawnAnchor = anchor.transform;
            }

            if (barrierMaterial == null)
            {
                barrierMaterial = new PhysicMaterial("Entropy Compression Boundary")
                {
                    dynamicFriction = 0.78f,
                    staticFriction = 0.92f,
                    bounciness = 0.02f,
                    frictionCombine = PhysicMaterialCombine.Average,
                    bounceCombine = PhysicMaterialCombine.Minimum
                };
            }

            frontBarrier = EnsureBarrier(frontBarrier, "Soft Front Entropy Limit");
            rearBarrier = EnsureBarrier(rearBarrier, "Rear Entropy Cushion");
            ApplyBarrierState();
        }

        private BoxCollider EnsureBarrier(BoxCollider barrier, string barrierName)
        {
            if (barrier != null)
            {
                return barrier;
            }

            GameObject barrierObject = new GameObject(barrierName);
            barrierObject.layer = ResolveLayer(physicsOnlyLayerName);
            barrierObject.transform.SetParent(volumeRoot, false);
            barrier = barrierObject.AddComponent<BoxCollider>();
            barrier.sharedMaterial = barrierMaterial;
            return barrier;
        }

        private void ApplyBarrierState()
        {
            if (spawnAnchor != null)
            {
                spawnAnchor.localPosition = new Vector3(ZoneCenterLocalX, 0f, 0f);
                spawnAnchor.localRotation = Quaternion.identity;
                spawnAnchor.localScale = Vector3.one;
            }

            float diameter = tubeRadius * 2f * barrierRadiusPadding;
            ConfigureBarrier(frontBarrier, FrontLimitLocalX - barrierThickness * 0.5f, diameter);
            ConfigureBarrier(rearBarrier, RearLimitLocalX + barrierThickness * 0.5f, diameter);
        }

        private void ConfigureBarrier(BoxCollider barrier, float localX, float diameter)
        {
            if (barrier == null)
            {
                return;
            }

            barrier.enabled = hardBarriersEnabled;
            Transform barrierTransform = barrier.transform;
            barrierTransform.localPosition = new Vector3(localX, 0f, 0f);
            barrierTransform.localRotation = Quaternion.identity;
            barrierTransform.localScale = Vector3.one;
            barrier.size = new Vector3(barrierThickness, diameter, diameter);
            barrier.center = Vector3.zero;
            barrier.sharedMaterial = barrierMaterial;
        }

        private void ApplyCompressionForces()
        {
            float frontLimit = FrontLimitLocalX;
            float rearLimit = RearLimitLocalX;
            float depth = Mathf.Max(0.1f, rearLimit - frontLimit);
            float targetX = Mathf.Lerp(frontLimit + depth * 0.42f, rearLimit - depth * 0.24f, objectPackingDensity);
            float targetRadius = EffectiveRadius * Mathf.Lerp(0.92f, 0.7f, objectPackingDensity);

            System.Collections.Generic.IReadOnlyList<GameObject> spawned = spawner.SpawnedObjects;
            for (int i = 0; i < spawned.Count; i++)
            {
                GameObject gemstone = spawned[i];
                if (gemstone == null || !gemstone.activeInHierarchy)
                {
                    continue;
                }

                Rigidbody body = gemstone.GetComponent<Rigidbody>();
                if (body == null)
                {
                    continue;
                }

                Vector3 localPosition = chamberTransform.InverseTransformPoint(body.worldCenterOfMass);
                Vector3 worldForce = Vector3.zero;
                float frontProximity = 1f - Mathf.InverseLerp(frontLimit, frontLimit + compressionSoftness, localPosition.x);
                float rearProximity = Mathf.InverseLerp(rearLimit - compressionSoftness, rearLimit, localPosition.x);
                frontProximity = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(frontProximity));
                rearProximity = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(rearProximity));

                if (frontProximity > 0f)
                {
                    worldForce += chamberTransform.right * (frontProximity * compressionStrength);
                }

                if (rearProximity > 0f)
                {
                    worldForce -= chamberTransform.right * (rearProximity * compressionStrength * 0.7f);
                }

                if (softForwardBiasEnabled)
                {
                    float axisBias = Mathf.Clamp((targetX - localPosition.x) / depth, -1f, 1f);
                    worldForce += chamberTransform.right * (axisBias * compressionStrength * 0.18f);
                }

                Vector2 radial = new Vector2(localPosition.y, localPosition.z);
                float radialDistance = radial.magnitude;
                if (radialDistance > targetRadius && radialDistance > 0.001f)
                {
                    Vector3 localRadialDirection = new Vector3(0f, radial.x / radialDistance, radial.y / radialDistance);
                    float radialExcess = Mathf.Clamp01((radialDistance - targetRadius) / Mathf.Max(0.05f, tubeRadius - targetRadius));
                    worldForce -= chamberTransform.TransformDirection(localRadialDirection) * (radialExcess * compressionStrength * radialPackingBias);
                }

                if (worldForce.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                if (body.IsSleeping() && ShouldWake(localPosition, frontLimit, rearLimit, targetRadius))
                {
                    body.WakeUp();
                }

                body.AddForce(worldForce, ForceMode.Acceleration);
            }
        }

        private bool ShouldWake(Vector3 localPosition, float frontLimit, float rearLimit, float targetRadius)
        {
            if (wakeOutsideVolumeStrength <= 0f)
            {
                return false;
            }

            float radialDistance = new Vector2(localPosition.y, localPosition.z).magnitude;
            bool outsideDepth = localPosition.x < frontLimit + compressionSoftness * wakeOutsideVolumeStrength ||
                localPosition.x > rearLimit - compressionSoftness * wakeOutsideVolumeStrength;
            bool outsideRadius = radialDistance > targetRadius + compressionSoftness * wakeOutsideVolumeStrength;
            return outsideDepth || outsideRadius;
        }

        private static int ResolveLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }
    }
}
