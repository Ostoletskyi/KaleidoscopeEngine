using System.Collections.Generic;
using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class GemstoneSpawner : MonoBehaviour
    {
        [Header("Definitions")]
        [SerializeField] private List<GemstoneDefinition> definitions = new List<GemstoneDefinition>();

        [Header("Counts")]
        [Min(0)] public int totalCount = 64;
        [Range(0f, 0.9f)] public float microParticleRatio = 0.28f;

        [Header("Spawn Volume")]
        [SerializeField] private Transform spawnVolume;
        [SerializeField] private Vector3 spawnVolumeSize = new Vector3(3.8f, 2.2f, 3.8f);
        [SerializeField] private int placementAttemptsPerObject = 20;
        [SerializeField] private float overlapPadding = 0.9f;
        [SerializeField] private AnimationCurve verticalDensity = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f);
        [SerializeField] private float centerClustering = 0.35f;

        [Header("Randomization")]
        [Tooltip("0 or less means a new random seed each respawn. Positive values are deterministic.")]
        public int randomSeed = 12345;
        public bool clearBeforeSpawn = true;
        public bool spawnOnStart = true;

        [Header("Hierarchy")]
        [SerializeField] private Transform parentTransform;

        [Header("Layers")]
        [Tooltip("Layer names are expected in ProjectSettings/TagManager.asset. Falls back to Default if missing.")]
        [SerializeField] private string gemLayerName = "KaleidoscopeGem";
        [SerializeField] private string microParticleLayerName = "KaleidoscopeMicroParticle";

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private System.Random random;
        private int activeSeed;

        public IReadOnlyList<GameObject> SpawnedObjects => spawnedObjects;
        public int ActiveSeed => activeSeed;
        public Vector3 SpawnVolumeSize
        {
            get => spawnVolumeSize;
            set => spawnVolumeSize = value;
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

            for (int i = 0; i < totalCount; i++)
            {
                bool wantMicro = random.NextDouble() < microParticleRatio;
                GemstoneDefinition definition = PickDefinition(wantMicro);
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

                GemstonePhysicsSetup setup = instance.GetComponent<GemstonePhysicsSetup>();
                if (setup == null)
                {
                    setup = instance.AddComponent<GemstonePhysicsSetup>();
                }

                setup.Configure(definition, random, gemLayer, microLayer);
                instance.SetActive(true);
                spawnedObjects.Add(instance);
            }
        }

        public void Clear()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
                    Destroy(spawnedObjects[i]);
                }
            }

            spawnedObjects.Clear();
        }

        public void Respawn()
        {
            Spawn();
        }

        private GemstoneDefinition PickDefinition(bool preferMicro)
        {
            float totalWeight = 0f;
            for (int i = 0; i < definitions.Count; i++)
            {
                bool categoryMatches = CategoryMatches(definitions[i], preferMicro);
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
                bool categoryMatches = totalWeight <= 0f || CategoryMatches(definitions[i], preferMicro);
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

        private static bool CategoryMatches(GemstoneDefinition definition, bool preferMicro)
        {
            if (preferMicro)
            {
                return definition.IsMicroParticle;
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
                Vector3 local = new Vector3(
                    RandomClusteredAxis(halfSize.x),
                    RandomVerticalAxis(halfSize.y),
                    RandomClusteredAxis(halfSize.z));

                Vector3 world = volume.TransformPoint(local);
                if (!Physics.CheckSphere(world, radius * overlapPadding, checkMask, QueryTriggerInteraction.Ignore))
                {
                    return world;
                }
            }

            Vector3 fallback = new Vector3(
                RandomClusteredAxis(halfSize.x),
                RandomVerticalAxis(halfSize.y),
                RandomClusteredAxis(halfSize.z));
            return volume.TransformPoint(fallback);
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
            Vector3 scale = Vector3.Lerp(definition.minScale, definition.maxScale, 0.5f);
            return Mathf.Max(scale.x, scale.y, scale.z) * 0.55f;
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
