using System.Collections.Generic;
using KaleidoscopeEngine.Geometry;
using KaleidoscopeEngine.Materials;
using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class GemstoneSpawner : MonoBehaviour
    {
        [Header("Definitions")]
        [SerializeField] private List<GemstoneDefinition> definitions = new List<GemstoneDefinition>();

        [Header("Counts")]
        [Min(0)] public int totalCount = 148;
        [Range(0f, 0.9f)] public float microParticleRatio = 0.56f;
        [SerializeField, Min(0)] private int mediumShardCount = 52;
        private const int DefaultTotalCount = 156;
        private const float DefaultMicroParticleRatio = 0.58f;
        private const int DefaultMediumShardCount = 52;

        [Header("Spawn Volume")]
        [SerializeField] private Transform spawnVolume;
        [SerializeField] private Vector3 spawnVolumeSize = new Vector3(3.8f, 2.2f, 3.8f);
        [SerializeField] private bool spawnInsideCylinder;
        [SerializeField] private float cylinderRadius = 1.15f;
        [SerializeField] private float cylinderLength = 4.8f;
        [SerializeField] private int placementAttemptsPerObject = 20;
        [SerializeField] private float overlapPadding = 0.9f;
        [SerializeField] private AnimationCurve verticalDensity = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f);
        [SerializeField] private float centerClustering = 0.35f;

        [Header("Mosaic Density")]
        [SerializeField, Range(0f, 1f)] private float microCrystalDensity = 0.62f;
        [SerializeField, Range(0f, 1f)] private float densityDistribution = 0.72f;
        [SerializeField, Range(0f, 0.4f)] private float visualNoiseAmount = 0.08f;
        [SerializeField, Range(0f, 1f)] private float shardSizeVariance = 0.42f;
        private const float DefaultMicroCrystalDensity = 0.62f;
        private const float DefaultDensityDistribution = 0.72f;
        private const float DefaultVisualNoiseAmount = 0.08f;
        private const float DefaultShardSizeVariance = 0.42f;

        [Header("Scale Rebalance")]
        [SerializeField, Range(0.25f, 1.25f)] private float dominantGemScaleMultiplier = 0.78f;
        [SerializeField, Range(0.25f, 1.5f)] private float mediumGemScaleMultiplier = 0.92f;
        [SerializeField, Range(0.25f, 2f)] private float microGemScaleMultiplier = 1.18f;
        [SerializeField, Range(0.12f, 0.8f)] private float maxVisibleGemArea = 0.38f;
        private const float DefaultDominantGemScaleMultiplier = 0.78f;
        private const float DefaultMediumGemScaleMultiplier = 0.92f;
        private const float DefaultMicroGemScaleMultiplier = 1.18f;
        private const float DefaultMaxVisibleGemArea = 0.38f;

        [Header("Randomization")]
        [Tooltip("0 or less means a new random seed each respawn. Positive values are deterministic.")]
        public int randomSeed = 12345;
        public bool clearBeforeSpawn = true;
        public bool spawnOnStart = true;

        [Header("Hierarchy")]
        [SerializeField] private Transform parentTransform;

        [Header("Materials")]
        [SerializeField] private GemstoneMaterialAssigner materialAssigner;

        [Header("Geometry")]
        [SerializeField] private GemGeometryAssigner geometryAssigner;

        [Header("Layers")]
        [Tooltip("Layer names are expected in ProjectSettings/TagManager.asset. Falls back to Default if missing.")]
        [SerializeField] private string gemLayerName = "KaleidoscopeGem";
        [SerializeField] private string microParticleLayerName = "KaleidoscopeMicroParticle";

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private System.Random random;
        private int activeSeed;
        private int dominantGemCount;

        public IReadOnlyList<GameObject> SpawnedObjects => spawnedObjects;
        public int ActiveSeed => activeSeed;
        public int MediumShardCount => mediumShardCount;
        public float MicroCrystalDensity => microCrystalDensity;
        public float DensityDistribution => densityDistribution;
        public float VisualNoiseAmount => visualNoiseAmount;
        public float DominantGemRatio => spawnedObjects.Count > 0 ? dominantGemCount / (float)spawnedObjects.Count : 0f;
        public float OpticalDensity => Mathf.Clamp01(totalCount / 180f * 0.42f + microCrystalDensity * 0.36f + mediumShardCount / 80f * 0.22f);
        public Vector3 SpawnVolumeSize
        {
            get => spawnVolumeSize;
            set => spawnVolumeSize = value;
        }

        public void SetCylindricalSpawnVolume(float radius, float length)
        {
            spawnInsideCylinder = true;
            cylinderRadius = Mathf.Max(0.1f, radius);
            cylinderLength = Mathf.Max(0.1f, length);
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                Spawn();
            }
        }

        public void SetDefinitions(IEnumerable<GemstoneDefinition> gemstoneDefinitions)
        {
            definitions.Clear();
            definitions.AddRange(gemstoneDefinitions);
        }

        public void SetSpawnVolume(Transform volume)
        {
            spawnVolume = volume;
        }

        public void SetParentTransform(Transform parent)
        {
            parentTransform = parent;
        }

        public void SetMaterialAssigner(GemstoneMaterialAssigner assigner)
        {
            materialAssigner = assigner;
        }

        public void SetGeometryAssigner(GemGeometryAssigner assigner)
        {
            geometryAssigner = assigner;
        }

        public void SetLayerNames(string gemLayer, string microParticleLayer)
        {
            if (!string.IsNullOrWhiteSpace(gemLayer))
            {
                gemLayerName = gemLayer;
            }

            if (!string.IsNullOrWhiteSpace(microParticleLayer))
            {
                microParticleLayerName = microParticleLayer;
            }
        }

        public void Spawn()
        {
            if (clearBeforeSpawn)
            {
                Clear();
            }

            if (definitions.Count == 0)
            {
                Debug.LogWarning($"{nameof(GemstoneSpawner)} has no gemstone definitions.", this);
                return;
            }

            activeSeed = randomSeed > 0 ? randomSeed : UnityEngine.Random.Range(1, int.MaxValue);
            random = new System.Random(activeSeed);

            int gemLayer = ResolveLayer(gemLayerName);
            int microLayer = ResolveLayer(microParticleLayerName);
            dominantGemCount = 0;
            microParticleRatio = Mathf.Clamp01(Mathf.Max(microParticleRatio, microCrystalDensity));
            int targetMediumShards = Mathf.Clamp(mediumShardCount, 0, totalCount);

            for (int i = 0; i < totalCount; i++)
            {
                bool wantMicro = random.NextDouble() < microParticleRatio;
                bool wantMediumShard = !wantMicro && i < targetMediumShards;
                GemstoneDefinition definition = PickDefinition(wantMicro, wantMediumShard);
                if (definition == null)
                {
                    continue;
                }

                GameObject instance = CreateInstance(definition, i);
                instance.SetActive(false);
                instance.transform.SetParent(parentTransform != null ? parentTransform : transform, false);

                float radius = EstimateRadius(definition);
                instance.transform.position = FindSpawnPosition(radius);
                instance.transform.rotation = RandomRotation();
                geometryAssigner?.ApplyTo(instance, definition, activeSeed + i * 92821);

                GemstonePhysicsSetup setup = instance.GetComponent<GemstonePhysicsSetup>();
                if (setup == null)
                {
                    setup = instance.AddComponent<GemstonePhysicsSetup>();
                }

                float scaleMultiplier = ResolveScaleMultiplier(definition) * ResolveShardVariance(definition);
                setup.Configure(definition, random, gemLayer, microLayer, scaleMultiplier, maxVisibleGemArea);
                materialAssigner?.ApplyTo(instance);
                instance.SetActive(true);
                spawnedObjects.Add(instance);

                if (IsDominantDefinition(definition, instance.transform.localScale))
                {
                    dominantGemCount++;
                }
            }
        }

        public void Clear()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
                    geometryAssigner?.Release(spawnedObjects[i]);
                    materialAssigner?.Release(spawnedObjects[i]);
                    Destroy(spawnedObjects[i]);
                }
            }

            spawnedObjects.Clear();
        }

        public void Respawn()
        {
            Spawn();
        }

        public void AdjustOpticalDensity(int countDelta)
        {
            totalCount = Mathf.Clamp(totalCount + countDelta, 48, 240);
            mediumShardCount = Mathf.Clamp(mediumShardCount + Mathf.RoundToInt(countDelta * 0.45f), 8, totalCount);
            microCrystalDensity = Mathf.Clamp01(microCrystalDensity + Mathf.Sign(countDelta) * 0.025f);
            microParticleRatio = Mathf.Clamp(microParticleRatio + Mathf.Sign(countDelta) * 0.025f, 0.1f, 0.85f);
            Respawn();
        }

        public void ResetMosaicDefaults(bool respawn)
        {
            totalCount = DefaultTotalCount;
            microParticleRatio = DefaultMicroParticleRatio;
            mediumShardCount = DefaultMediumShardCount;
            microCrystalDensity = DefaultMicroCrystalDensity;
            densityDistribution = DefaultDensityDistribution;
            visualNoiseAmount = DefaultVisualNoiseAmount;
            shardSizeVariance = DefaultShardSizeVariance;
            dominantGemScaleMultiplier = DefaultDominantGemScaleMultiplier;
            mediumGemScaleMultiplier = DefaultMediumGemScaleMultiplier;
            microGemScaleMultiplier = DefaultMicroGemScaleMultiplier;
            maxVisibleGemArea = DefaultMaxVisibleGemArea;

            if (respawn)
            {
                Respawn();
            }
        }

        private GemstoneDefinition PickDefinition(bool preferMicro, bool preferMediumShard)
        {
            float totalWeight = 0f;
            for (int i = 0; i < definitions.Count; i++)
            {
                bool categoryMatches = CategoryMatches(definitions[i], preferMicro, preferMediumShard);
                if (categoryMatches)
                {
                    totalWeight += definitions[i].spawnWeight;
                }
            }

            if (totalWeight <= 0f)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    totalWeight += definitions[i].spawnWeight;
                }
            }

            float roll = (float)random.NextDouble() * totalWeight;
            for (int i = 0; i < definitions.Count; i++)
            {
                bool categoryMatches = totalWeight <= 0f || CategoryMatches(definitions[i], preferMicro, preferMediumShard);
                if (!categoryMatches)
                {
                    continue;
                }

                roll -= definitions[i].spawnWeight;
                if (roll <= 0f)
                {
                    return definitions[i];
                }
            }

            return definitions[definitions.Count - 1];
        }

        private static bool CategoryMatches(GemstoneDefinition definition, bool preferMicro, bool preferMediumShard)
        {
            if (preferMicro)
            {
                return definition.IsMicroParticle;
            }

            if (preferMediumShard)
            {
                return definition.particleCategory == GemstoneParticleCategory.Gem ||
                    definition.particleCategory == GemstoneParticleCategory.Slider ||
                    definition.particleCategory == GemstoneParticleCategory.GlassFragment;
            }

            return !definition.IsMicroParticle;
        }

        private GameObject CreateInstance(GemstoneDefinition definition, int index)
        {
            GameObject instance = definition.prefab != null
                ? Instantiate(definition.prefab)
                : GemstonePrimitiveFactory.CreatePrimitive(definition);

            instance.name = $"{definition.displayName}_{index:00}";
            return instance;
        }

        private Vector3 FindSpawnPosition(float radius)
        {
            Transform volume = spawnVolume != null ? spawnVolume : transform;
            Vector3 halfSize = spawnVolumeSize * 0.5f;
            LayerMask checkMask = Physics.AllLayers;

            for (int attempt = 0; attempt < placementAttemptsPerObject; attempt++)
            {
                Vector3 local = spawnInsideCylinder
                    ? RandomPointInCylinder(radius)
                    : new Vector3(
                        RandomClusteredAxis(halfSize.x),
                        RandomVerticalAxis(halfSize.y),
                        RandomClusteredAxis(halfSize.z));

                Vector3 world = volume.TransformPoint(local);
                if (!Physics.CheckSphere(world, radius * overlapPadding, checkMask, QueryTriggerInteraction.Ignore))
                {
                    return world;
                }
            }

            Vector3 fallback = spawnInsideCylinder
                ? RandomPointInCylinder(radius)
                : new Vector3(
                    RandomClusteredAxis(halfSize.x),
                    RandomVerticalAxis(halfSize.y),
                    RandomClusteredAxis(halfSize.z));
            return volume.TransformPoint(fallback);
        }

        private Vector3 RandomPointInCylinder(float objectRadius)
        {
            float safeRadius = Mathf.Max(0.05f, cylinderRadius - objectRadius * 1.35f);
            float angle = RandomRange(0f, Mathf.PI * 2f);
            float radialRoll = Mathf.Clamp01((float)random.NextDouble());
            float uniformRadial = Mathf.Sqrt(radialRoll);
            float layeredRadial = Mathf.Pow(radialRoll, Mathf.Lerp(0.45f, 1.65f, densityDistribution));
            float radial = Mathf.Lerp(uniformRadial, layeredRadial, densityDistribution) * safeRadius;
            float uniformX = RandomRange(-cylinderLength * 0.5f, cylinderLength * 0.5f);
            float layeredX = (RandomRange(-cylinderLength * 0.5f, cylinderLength * 0.5f) +
                RandomRange(-cylinderLength * 0.5f, cylinderLength * 0.5f)) * 0.5f;
            float x = Mathf.Lerp(uniformX, layeredX, densityDistribution);

            return new Vector3(
                x,
                Mathf.Cos(angle) * radial,
                Mathf.Sin(angle) * radial);
        }

        private Quaternion RandomRotation()
        {
            return Quaternion.Euler(
                RandomRange(0f, 360f),
                RandomRange(0f, 360f),
                RandomRange(0f, 360f));
        }

        private float EstimateRadius(GemstoneDefinition definition)
        {
            Vector3 scale = Vector3.Lerp(definition.minScale, definition.maxScale, 0.5f) * ResolveScaleMultiplier(definition);
            return Mathf.Max(scale.x, scale.y, scale.z) * 0.55f;
        }

        private float ResolveScaleMultiplier(GemstoneDefinition definition)
        {
            if (definition == null)
            {
                return 1f;
            }

            if (definition.particleCategory == GemstoneParticleCategory.HeavyAnchor)
            {
                return dominantGemScaleMultiplier;
            }

            if (definition.IsMicroParticle)
            {
                return microGemScaleMultiplier;
            }

            return mediumGemScaleMultiplier;
        }

        private float ResolveShardVariance(GemstoneDefinition definition)
        {
            float variance = definition != null && definition.IsMicroParticle ? shardSizeVariance * 0.55f : shardSizeVariance;
            float min = Mathf.Lerp(1f, 0.72f, variance);
            float max = Mathf.Lerp(1f, 1.18f, variance);
            return RandomRange(min, max);
        }

        private static bool IsDominantDefinition(GemstoneDefinition definition, Vector3 scale)
        {
            if (definition == null)
            {
                return false;
            }

            float largestAxis = Mathf.Max(scale.x, scale.y, scale.z);
            return definition.particleCategory == GemstoneParticleCategory.HeavyAnchor || largestAxis > 0.42f;
        }

        private float RandomRange(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private float RandomClusteredAxis(float halfExtent)
        {
            float uniform = RandomRange(-halfExtent, halfExtent);
            float clustered = RandomRange(-halfExtent, halfExtent) + RandomRange(-halfExtent, halfExtent);
            clustered *= 0.5f;
            return Mathf.Lerp(uniform, clustered, centerClustering);
        }

        private float RandomVerticalAxis(float halfExtent)
        {
            float roll = Mathf.Clamp01((float)random.NextDouble());
            float shaped = verticalDensity != null ? Mathf.Clamp01(verticalDensity.Evaluate(roll)) : roll;
            return Mathf.Lerp(-halfExtent, halfExtent, shaped);
        }

        private static int ResolveLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }
    }
}
