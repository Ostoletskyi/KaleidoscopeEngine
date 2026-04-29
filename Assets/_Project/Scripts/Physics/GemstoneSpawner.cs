using System.Collections.Generic;
using KaleidoscopeEngine.Geometry;
using KaleidoscopeEngine.Materials;
using KaleidoscopeEngine.Mirrors;
using UnityEngine;
using UnityEngine.Rendering;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public enum EntropyDensityPreset
    {
        Sparse,
        Normal,
        Dense,
        Packed,
        Extreme
    }

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
        [SerializeField, Range(0.4f, 1f)] private float sourceCoverageTarget = 0.82f;
        [SerializeField] private bool dynamicEntropyBalancing = true;
        [SerializeField, Range(0.5f, 2f)] private float shardFieldMultiplier = 1.24f;
        [SerializeField, Min(0)] private int opticalFillerParticles = 38;
        [SerializeField] private bool reflectiveDustLayer = true;
        private const float DefaultMicroCrystalDensity = 0.62f;
        private const float DefaultDensityDistribution = 0.72f;
        private const float DefaultVisualNoiseAmount = 0.08f;
        private const float DefaultShardSizeVariance = 0.42f;
        private const float DefaultSourceCoverageTarget = 0.82f;
        private const float DefaultShardFieldMultiplier = 1.24f;
        private const int DefaultOpticalFillerParticles = 38;
        private const int MaxQualityTotalCount = 420;

        [Header("Source Visibility Targets")]
        [SerializeField] private EntropyDensityPreset densityPreset = EntropyDensityPreset.Packed;
        [SerializeField, Min(0)] private int heroGemCount = 5;
        [SerializeField, Min(0)] private int visibleLargeGemTarget = 5;
        [SerializeField, Min(0)] private int visibleMediumShardTarget = 125;
        [SerializeField, Min(0)] private int visibleMicroCrystalTarget = 220;
        [SerializeField, Min(0)] private int visibleSparkleTarget = 72;
        [SerializeField] private Camera sourceVisibilityCamera;
        [SerializeField] private bool refillSourceFrustumAfterSpawn = true;
        [SerializeField, Range(0f, 1f)] private float visibleSourcePlaneBias = 0.72f;
        [SerializeField, Range(0, 4)] private int visibilityRefillMaxPasses = 2;

        [Header("Rear Wall Packing Layer")]
        [SerializeField] private bool rearWallFillEnabled = true;
        [SerializeField, Min(0)] private int rearWallFillCount = 140;
        [SerializeField, Range(0.02f, 0.6f)] private float rearWallFillDepth = 0.22f;
        [SerializeField, Range(0.1f, 2f)] private float rearWallFillRadius = 0.92f;
        [SerializeField] private bool visualOnlyMicroChips = true;
        [SerializeField, Min(0)] private int visualOnlyChipCount = 190;
        [SerializeField, Min(0)] private int visualMicroChipCount = 2000;
        [SerializeField, Range(0.004f, 0.12f)] private float visualMicroChipScale = 0.032f;
        [SerializeField, Range(0.1f, 3f)] private float rearWallChipDensity = 1.35f;
        [SerializeField, Range(0.02f, 0.8f)] private float chipLayerDepth = 0.34f;
        [SerializeField, Range(0f, 1f)] private float chipRandomRotation = 1f;
        [SerializeField, Range(0f, 1f)] private float chipColorVariation = 0.72f;
        [SerializeField, Range(0f, 1f)] private float chipSparkleVariation = 0.42f;
        [SerializeField, Min(0)] private int physicsMicroCrystalCount = 160;
        [SerializeField, Min(0)] private int mediumShardBoost = 85;
        [SerializeField, Range(0f, 1f)] private float largeGemReduction = 0.55f;

        [Header("Scale Rebalance")]
        [SerializeField, Range(0.25f, 1.25f)] private float dominantGemScaleMultiplier = 0.78f;
        [SerializeField, Range(0.25f, 1.5f)] private float mediumGemScaleMultiplier = 0.92f;
        [SerializeField, Range(0.25f, 2f)] private float microGemScaleMultiplier = 1.18f;
        [SerializeField, Range(0.12f, 0.8f)] private float maxVisibleGemArea = 0.38f;
        private const float DefaultDominantGemScaleMultiplier = 0.78f;
        private const float DefaultMediumGemScaleMultiplier = 0.92f;
        private const float DefaultMicroGemScaleMultiplier = 1.18f;
        private const float DefaultMaxVisibleGemArea = 0.38f;
        private const int InstancedChipBatchSize = 1023;

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
        private readonly List<Matrix4x4>[] visualMicroChipBatches =
        {
            new List<Matrix4x4>(),
            new List<Matrix4x4>(),
            new List<Matrix4x4>(),
            new List<Matrix4x4>()
        };
        private System.Random random;
        private int activeSeed;
        private int dominantGemCount;
        private int lastVisibleLargeCount;
        private int lastVisibleMediumCount;
        private int lastVisibleMicroCount;
        private int lastVisibilityRefillCount;
        private int qualityVisualMicroChipLimit = 2000;
        private int effectiveVisualMicroChipCount = -1;
        private Mesh visualMicroChipMesh;
        private Material[] visualMicroChipMaterials;
        private int visualMicroChipLayer;
        private readonly Matrix4x4[] instancedChipDrawBuffer = new Matrix4x4[InstancedChipBatchSize];

        public IReadOnlyList<GameObject> SpawnedObjects => spawnedObjects;
        public int ActiveSeed => activeSeed;
        public int MediumShardCount => mediumShardCount;
        public float MicroCrystalDensity => microCrystalDensity;
        public float DensityDistribution => densityDistribution;
        public float VisualNoiseAmount => visualNoiseAmount;
        public float DominantGemRatio => spawnedObjects.Count > 0 ? dominantGemCount / (float)spawnedObjects.Count : 0f;
        public float OpticalDensity => Mathf.Clamp01(totalCount / 180f * 0.42f + microCrystalDensity * 0.36f + mediumShardCount / 80f * 0.22f);
        public float SourceCoverageTarget => sourceCoverageTarget;
        public bool DynamicEntropyBalancing => dynamicEntropyBalancing;
        public float SourceCoverageEstimate => Mathf.Clamp01(OpticalDensity * 0.82f + microParticleRatio * 0.12f + densityDistribution * 0.06f);
        public int OpticalFillerParticles => opticalFillerParticles;
        public float ShardFieldMultiplier => shardFieldMultiplier;
        public EntropyDensityPreset DensityPreset => densityPreset;
        public string DensityPresetName => densityPreset.ToString();
        public int HeroGemCount => heroGemCount;
        public int VisibleLargeGemTarget => visibleLargeGemTarget;
        public int VisibleMediumShardTarget => visibleMediumShardTarget;
        public int VisibleMicroCrystalTarget => visibleMicroCrystalTarget;
        public int VisibleSparkleTarget => visibleSparkleTarget;
        public int LastVisibleLargeCount => lastVisibleLargeCount;
        public int LastVisibleMediumCount => lastVisibleMediumCount;
        public int LastVisibleMicroCount => lastVisibleMicroCount;
        public int LastVisibleEntropyCount => lastVisibleLargeCount + lastVisibleMediumCount + lastVisibleMicroCount;
        public int VisibleEntropyTarget => visibleLargeGemTarget + visibleMediumShardTarget + visibleMicroCrystalTarget;
        public int LastVisibilityRefillCount => lastVisibilityRefillCount;
        public bool RearWallFillEnabled => rearWallFillEnabled;
        public int RearWallFillCount => rearWallFillCount;
        public float RearWallFillDepth => rearWallFillDepth;
        public float RearWallFillRadius => rearWallFillRadius;
        public bool VisualOnlyMicroChips => visualOnlyMicroChips;
        public int VisualOnlyChipCount => visualMicroChipCount;
        public int VisualMicroChipCount => visualMicroChipCount;
        public int RequestedVisualMicroChipCount => visualMicroChipCount;
        public int EffectiveVisualMicroChipCount => EffectiveVisualChipCount;
        public float VisualMicroChipScale => visualMicroChipScale;
        public float RearWallChipDensity => rearWallChipDensity;
        public float ChipLayerDepth => chipLayerDepth;
        public float ChipRandomRotation => chipRandomRotation;
        public float ChipColorVariation => chipColorVariation;
        public float ChipSparkleVariation => chipSparkleVariation;
        public int PhysicsMicroCrystalCount => physicsMicroCrystalCount;
        public int MediumShardBoost => mediumShardBoost;
        public float LargeGemReduction => largeGemReduction;
        public Vector3 SpawnVolumeSize
        {
            get => spawnVolumeSize;
            set => spawnVolumeSize = value;
        }

        private int EffectiveVisualChipCount => effectiveVisualMicroChipCount >= 0
            ? Mathf.Min(effectiveVisualMicroChipCount, visualMicroChipCount)
            : visualMicroChipCount;

        public void SetCylindricalSpawnVolume(float radius, float length)
        {
            spawnInsideCylinder = true;
            cylinderRadius = Mathf.Max(0.1f, radius);
            cylinderLength = Mathf.Max(0.1f, length);
        }

        public void SetSourceVisibilityCamera(Camera camera)
        {
            sourceVisibilityCamera = camera;
        }

        public void SetAdaptiveVisualMicroChipLimit(int maxVisibleChips)
        {
            effectiveVisualMicroChipCount = Mathf.Clamp(maxVisibleChips, 0, Mathf.Max(0, visualMicroChipCount));
        }

        public void ClearAdaptiveVisualMicroChipLimit()
        {
            effectiveVisualMicroChipCount = -1;
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
            ApplyDensityPreset(densityPreset, false);

            int gemLayer = ResolveLayer(gemLayerName);
            int microLayer = ResolveLayer(microParticleLayerName);
            dominantGemCount = 0;
            lastVisibilityRefillCount = 0;
            microParticleRatio = Mathf.Clamp01(Mathf.Max(microParticleRatio, microCrystalDensity));
            int targetMediumShards = Mathf.Clamp(mediumShardCount + mediumShardBoost, 0, totalCount);
            int fillerStartIndex = Mathf.Max(0, totalCount - opticalFillerParticles);
            int mediumSpawned = 0;
            int microSpawned = 0;
            int largeSpawned = 0;

            for (int i = 0; i < totalCount; i++)
            {
                bool wantFillerDetail = reflectiveDustLayer && i >= fillerStartIndex;
                bool wantLarge = largeSpawned < heroGemCount;
                bool wantMicro = !wantLarge && (wantFillerDetail ||
                    microSpawned < physicsMicroCrystalCount ||
                    random.NextDouble() < microParticleRatio);
                bool wantMediumShard = !wantLarge && !wantMicro && mediumSpawned < targetMediumShards;
                GemstoneDefinition definition = wantLarge
                    ? PickDefinitionByCategory(GemstoneParticleCategory.HeavyAnchor)
                    : PickDefinition(wantMicro, wantMediumShard);
                if (definition == null)
                {
                    continue;
                }

                bool rearBias = rearWallFillEnabled && i < rearWallFillCount;
                GameObject instance = SpawnPhysicsInstance(definition, i, gemLayer, microLayer, rearBias);
                if (instance == null)
                {
                    continue;
                }

                if (IsDominantDefinition(definition, instance.transform.localScale))
                {
                    dominantGemCount++;
                    largeSpawned++;
                }

                if (definition.IsMicroParticle)
                {
                    microSpawned++;
                }
                else if (IsMediumDefinition(definition))
                {
                    mediumSpawned++;
                }
            }

            BuildRearWallVisualOnlyChips(microLayer);
            EvaluateSourceVisibilityAndRefill(gemLayer, microLayer);
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

            for (int i = 0; i < visualMicroChipBatches.Length; i++)
            {
                visualMicroChipBatches[i].Clear();
            }
        }

        public void Respawn()
        {
            Spawn();
        }

        public void AdjustOpticalDensity(int countDelta)
        {
            totalCount = Mathf.Clamp(totalCount + countDelta, 48, MaxQualityTotalCount);
            mediumShardCount = Mathf.Clamp(mediumShardCount + Mathf.RoundToInt(countDelta * 0.45f), 8, totalCount);
            microCrystalDensity = Mathf.Clamp01(microCrystalDensity + Mathf.Sign(countDelta) * 0.025f);
            microParticleRatio = Mathf.Clamp(microParticleRatio + Mathf.Sign(countDelta) * 0.025f, 0.1f, 0.85f);
            BalanceEntropyCoverage(false);
            Respawn();
        }

        public void ApplyQualityProfile(KaleidoscopeQualityProfile profile, bool respawn)
        {
            sourceCoverageTarget = Mathf.Clamp(profile.sourceCoverageTarget, 0.4f, 1f);
            dynamicEntropyBalancing = profile.dynamicEntropyBalancing;
            shardFieldMultiplier = Mathf.Clamp(profile.shardFieldMultiplier, 0.5f, 2f);
            opticalFillerParticles = Mathf.Clamp(profile.opticalFillerParticles, 0, 120);
            reflectiveDustLayer = profile.reflectiveDustLayer;
            microCrystalDensity = Mathf.Clamp01(Mathf.Max(profile.microDetailDensity, DefaultMicroCrystalDensity * 0.7f));
            densityDistribution = Mathf.Clamp01(Mathf.Lerp(DefaultDensityDistribution, 0.86f, Mathf.Clamp01(profile.sourceCoverageTarget)));
            visualNoiseAmount = Mathf.Clamp(profile.microDetailDensity * 0.12f, 0.04f, 0.22f);
            totalCount = Mathf.Clamp(Mathf.RoundToInt(DefaultTotalCount * shardFieldMultiplier) + opticalFillerParticles, 64, MaxQualityTotalCount);
            mediumShardCount = Mathf.Clamp(Mathf.RoundToInt(DefaultMediumShardCount * Mathf.Lerp(1f, shardFieldMultiplier, 0.85f)), 12, totalCount);
            microParticleRatio = Mathf.Clamp(Mathf.Max(DefaultMicroParticleRatio, microCrystalDensity * 0.92f), 0.2f, 0.88f);
            qualityVisualMicroChipLimit = ResolveVisualChipLimit(profile.level);
            visualMicroChipCount = Mathf.Max(visualMicroChipCount, qualityVisualMicroChipLimit);
            visualOnlyChipCount = visualMicroChipCount;
            BalanceEntropyCoverage(false);
            ApplyDensityPreset(densityPreset, false);

            if (respawn)
            {
                Respawn();
            }
        }

        public void ApplyDensityPreset(EntropyDensityPreset preset, bool respawn)
        {
            densityPreset = preset;
            switch (densityPreset)
            {
                case EntropyDensityPreset.Sparse:
                    SetDensityPresetValues(8, 40, 70, 24, 30, 200, 45, 18, 0.15f, 0.78f, 0.92f, 1.08f, 0.38f, 0.58f);
                    break;
                case EntropyDensityPreset.Normal:
                    SetDensityPresetValues(8, 64, 110, 36, 60, 500, 70, 32, 0.25f, 0.72f, 0.9f, 1.12f, 0.34f, 0.64f);
                    break;
                case EntropyDensityPreset.Dense:
                    SetDensityPresetValues(7, 92, 155, 52, 96, 1000, 110, 55, 0.38f, 0.64f, 0.86f, 1.18f, 0.3f, 0.72f);
                    break;
                case EntropyDensityPreset.Extreme:
                    SetDensityPresetValues(3, 165, 310, 96, 210, 5200, 230, 120, 0.72f, 0.42f, 0.72f, 1.32f, 0.2f, 0.9f);
                    break;
                default:
                    SetDensityPresetValues(5, 125, 220, 72, 140, 2000, 160, 85, 0.55f, 0.52f, 0.78f, 1.26f, 0.24f, 0.82f);
                    break;
            }

            int minimumPhysicsPopulation = visibleLargeGemTarget +
                Mathf.RoundToInt((visibleMediumShardTarget + mediumShardBoost) * 0.72f) +
                physicsMicroCrystalCount;
            totalCount = Mathf.Clamp(Mathf.Max(totalCount, minimumPhysicsPopulation), 64, MaxQualityTotalCount);
            mediumShardCount = Mathf.Clamp(Mathf.Max(mediumShardCount, visibleMediumShardTarget), 12, totalCount);
            microParticleRatio = Mathf.Clamp(Mathf.Max(microParticleRatio, microCrystalDensity), 0.2f, 0.92f);
            opticalFillerParticles = Mathf.Clamp(Mathf.Max(opticalFillerParticles, physicsMicroCrystalCount / 3), 0, 180);
            visualMicroChipCount = Mathf.Max(visualMicroChipCount, visualOnlyChipCount);

            if (respawn)
            {
                Respawn();
            }
        }

        public void BalanceEntropyCoverage(bool respawn)
        {
            if (!dynamicEntropyBalancing)
            {
                return;
            }

            float deficit = sourceCoverageTarget - SourceCoverageEstimate;
            if (deficit <= 0.01f)
            {
                return;
            }

            int countBoost = Mathf.CeilToInt(deficit * 92f);
            totalCount = Mathf.Clamp(totalCount + countBoost, 64, MaxQualityTotalCount);
            mediumShardCount = Mathf.Clamp(mediumShardCount + Mathf.CeilToInt(countBoost * 0.36f), 12, totalCount);
            opticalFillerParticles = Mathf.Clamp(opticalFillerParticles + Mathf.CeilToInt(countBoost * 0.44f), 0, 140);
            microCrystalDensity = Mathf.Clamp01(microCrystalDensity + deficit * 0.18f);
            microParticleRatio = Mathf.Clamp(microParticleRatio + deficit * 0.16f, 0.2f, 0.9f);

            if (respawn)
            {
                Respawn();
            }
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
            sourceCoverageTarget = DefaultSourceCoverageTarget;
            dynamicEntropyBalancing = true;
            shardFieldMultiplier = DefaultShardFieldMultiplier;
            opticalFillerParticles = DefaultOpticalFillerParticles;
            reflectiveDustLayer = true;
            ApplyDensityPreset(EntropyDensityPreset.Packed, false);

            if (respawn)
            {
                Respawn();
            }
        }

        private void LateUpdate()
        {
            DrawVisualMicroChips();
        }

        private void OnDestroy()
        {
            if (visualMicroChipMesh != null)
            {
                Destroy(visualMicroChipMesh);
            }

            if (visualMicroChipMaterials == null)
            {
                return;
            }

            for (int i = 0; i < visualMicroChipMaterials.Length; i++)
            {
                if (visualMicroChipMaterials[i] != null)
                {
                    Destroy(visualMicroChipMaterials[i]);
                }
            }
        }

        private void SetDensityPresetValues(
            int largeTarget,
            int mediumTarget,
            int microTarget,
            int sparkleTarget,
            int rearFill,
            int visualChips,
            int physicsMicro,
            int mediumBoost,
            float largeReduction,
            float dominantScale,
            float mediumScale,
            float microScale,
            float maxArea,
            float microDensity)
        {
            heroGemCount = largeTarget;
            visibleLargeGemTarget = largeTarget;
            visibleMediumShardTarget = mediumTarget;
            visibleMicroCrystalTarget = microTarget;
            visibleSparkleTarget = sparkleTarget;
            rearWallFillCount = rearFill;
            visualMicroChipCount = Mathf.Max(visualChips, qualityVisualMicroChipLimit);
            visualOnlyChipCount = visualMicroChipCount;
            physicsMicroCrystalCount = physicsMicro;
            mediumShardBoost = mediumBoost;
            this.largeGemReduction = largeReduction;
            dominantGemScaleMultiplier = dominantScale;
            mediumGemScaleMultiplier = mediumScale;
            microGemScaleMultiplier = microScale;
            maxVisibleGemArea = maxArea;
            microCrystalDensity = Mathf.Max(microCrystalDensity, microDensity);
            densityDistribution = Mathf.Max(densityDistribution, Mathf.Lerp(0.68f, 0.9f, microDensity));
            rearWallFillEnabled = rearFill > 0;
            visualOnlyMicroChips = visualChips > 0;
        }

        private static int ResolveVisualChipLimit(KaleidoscopeQualityLevel level)
        {
            switch (level)
            {
                case KaleidoscopeQualityLevel.Minimal:
                    return 200;
                case KaleidoscopeQualityLevel.Low:
                    return 500;
                case KaleidoscopeQualityLevel.Medium:
                    return 1000;
                case KaleidoscopeQualityLevel.Ultra:
                    return 3500;
                case KaleidoscopeQualityLevel.Extreme:
                    return 5200;
                default:
                    return 2000;
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

        private GemstoneDefinition PickDefinitionByCategory(GemstoneParticleCategory category)
        {
            float totalWeight = 0f;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].particleCategory == category)
                {
                    totalWeight += definitions[i].spawnWeight;
                }
            }

            if (totalWeight <= 0f)
            {
                return PickDefinition(false, false);
            }

            float roll = (float)random.NextDouble() * totalWeight;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].particleCategory != category)
                {
                    continue;
                }

                roll -= definitions[i].spawnWeight;
                if (roll <= 0f)
                {
                    return definitions[i];
                }
            }

            return PickDefinition(false, false);
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

        private GameObject SpawnPhysicsInstance(
            GemstoneDefinition definition,
            int index,
            int gemLayer,
            int microLayer,
            bool rearWallBias)
        {
            GameObject instance = CreateInstance(definition, index);
            instance.SetActive(false);
            instance.transform.SetParent(parentTransform != null ? parentTransform : transform, false);

            float radius = EstimateRadius(definition);
            instance.transform.position = FindSpawnPosition(radius, rearWallBias);
            instance.transform.rotation = RandomRotation();
            geometryAssigner?.ApplyTo(instance, definition, activeSeed + index * 92821);

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
            return instance;
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
            return FindSpawnPosition(radius, false);
        }

        private Vector3 FindSpawnPosition(float radius, bool rearWallBias)
        {
            Transform volume = spawnVolume != null ? spawnVolume : transform;
            Vector3 halfSize = spawnVolumeSize * 0.5f;
            LayerMask checkMask = Physics.AllLayers;

            for (int attempt = 0; attempt < placementAttemptsPerObject; attempt++)
            {
                bool useRearWall = rearWallBias && random.NextDouble() < visibleSourcePlaneBias;
                Vector3 local = useRearWall
                    ? RandomPointInRearWallLayer(radius)
                    : spawnInsideCylinder
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
                ? (rearWallBias ? RandomPointInRearWallLayer(radius) : RandomPointInCylinder(radius))
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

        private Vector3 RandomPointInRearWallLayer(float objectRadius)
        {
            float safeRadius = Mathf.Max(0.05f, Mathf.Min(cylinderRadius, rearWallFillRadius) - objectRadius * 1.15f);
            float angle = RandomRange(0f, Mathf.PI * 2f);
            float radial = Mathf.Pow(Mathf.Clamp01((float)random.NextDouble()), 0.58f) * safeRadius;
            float depth = RandomRange(0f, Mathf.Max(0.02f, rearWallFillDepth));
            float x = cylinderLength * 0.5f - depth - objectRadius * 0.5f;
            return new Vector3(x, Mathf.Cos(angle) * radial, Mathf.Sin(angle) * radial);
        }

        private void EvaluateSourceVisibilityAndRefill(int gemLayer, int microLayer)
        {
            CountVisibleSourceObjects(out lastVisibleLargeCount, out lastVisibleMediumCount, out lastVisibleMicroCount);
            if (!refillSourceFrustumAfterSpawn || sourceVisibilityCamera == null)
            {
                return;
            }

            for (int pass = 0; pass < visibilityRefillMaxPasses; pass++)
            {
                int missingMedium = Mathf.Max(0, visibleMediumShardTarget - lastVisibleMediumCount);
                int missingMicro = Mathf.Max(0, visibleMicroCrystalTarget - lastVisibleMicroCount);
                if (missingMedium <= 0 && missingMicro <= 0)
                {
                    break;
                }

                int refillMedium = Mathf.Min(missingMedium, Mathf.Max(8, visibleMediumShardTarget / 4));
                int refillMicro = Mathf.Min(missingMicro, Mathf.Max(16, visibleMicroCrystalTarget / 4));
                for (int i = 0; i < refillMedium; i++)
                {
                    GemstoneDefinition definition = PickDefinition(false, true);
                    if (definition != null)
                    {
                        SpawnPhysicsInstance(definition, spawnedObjects.Count + 1000, gemLayer, microLayer, true);
                        lastVisibilityRefillCount++;
                    }
                }

                for (int i = 0; i < refillMicro; i++)
                {
                    GemstoneDefinition definition = PickDefinition(true, false);
                    if (definition != null)
                    {
                        SpawnPhysicsInstance(definition, spawnedObjects.Count + 2000, gemLayer, microLayer, true);
                        lastVisibilityRefillCount++;
                    }
                }

                CountVisibleSourceObjects(out lastVisibleLargeCount, out lastVisibleMediumCount, out lastVisibleMicroCount);
            }
        }

        private void CountVisibleSourceObjects(out int large, out int medium, out int micro)
        {
            large = 0;
            medium = 0;
            micro = 0;
            Plane[] planes = sourceVisibilityCamera != null
                ? GeometryUtility.CalculateFrustumPlanes(sourceVisibilityCamera)
                : null;

            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                GameObject instance = spawnedObjects[i];
                if (instance == null || !instance.activeInHierarchy)
                {
                    continue;
                }

                Renderer renderer = instance.GetComponentInChildren<Renderer>();
                if (renderer == null || (planes != null && !GeometryUtility.TestPlanesAABB(planes, renderer.bounds)))
                {
                    continue;
                }

                GemstoneDefinition definition = ResolveDefinition(instance);
                if (definition != null && definition.IsMicroParticle)
                {
                    micro++;
                }
                else if (definition != null && IsMediumDefinition(definition))
                {
                    medium++;
                }
                else
                {
                    large++;
                }
            }

            micro += EstimateVisibleInstancedChipCount(planes);
        }

        private void BuildRearWallVisualOnlyChips(int microLayer)
        {
            for (int i = 0; i < visualMicroChipBatches.Length; i++)
            {
                visualMicroChipBatches[i].Clear();
            }

            if (!rearWallFillEnabled || !visualOnlyMicroChips || visualMicroChipCount <= 0)
            {
                return;
            }

            EnsureVisualMicroChipResources();
            visualMicroChipLayer = microLayer;
            Transform volume = spawnVolume != null ? spawnVolume : transform;
            int safeCount = Mathf.Clamp(visualMicroChipCount, 0, 6000);
            for (int i = 0; i < safeCount; i++)
            {
                Vector3 localPosition = RandomPointInChipLayer(0.006f);
                Vector3 worldPosition = volume.TransformPoint(localPosition);
                Quaternion worldRotation = chipRandomRotation > 0.001f
                    ? volume.rotation * Quaternion.Slerp(Quaternion.identity, RandomRotation(), chipRandomRotation)
                    : volume.rotation;
                float size = visualMicroChipScale * RandomRange(0.45f, 1.85f);
                Vector3 scale = new Vector3(
                    size * RandomRange(0.4f, 2.4f),
                    size * RandomRange(0.08f, 0.55f),
                    size * RandomRange(0.35f, 1.75f));
                Matrix4x4 matrix = Matrix4x4.TRS(worldPosition, worldRotation, scale);
                int materialIndex = Mathf.Clamp(Mathf.FloorToInt((float)random.NextDouble() * visualMicroChipBatches.Length), 0, visualMicroChipBatches.Length - 1);
                visualMicroChipBatches[materialIndex].Add(matrix);
            }
        }

        private Vector3 RandomPointInChipLayer(float objectRadius)
        {
            float safeRadius = Mathf.Max(0.05f, Mathf.Min(cylinderRadius, rearWallFillRadius) - objectRadius);
            float angle = RandomRange(0f, Mathf.PI * 2f);
            float radialRoll = Mathf.Clamp01((float)random.NextDouble());
            float radial = Mathf.Pow(radialRoll, Mathf.Lerp(0.38f, 0.72f, rearWallChipDensity / 3f)) * safeRadius;
            float depth = RandomRange(0f, Mathf.Max(0.02f, chipLayerDepth));
            float x = cylinderLength * 0.5f - depth - objectRadius;
            return new Vector3(x, Mathf.Cos(angle) * radial, Mathf.Sin(angle) * radial);
        }

        private int EstimateVisibleInstancedChipCount(Plane[] planes)
        {
            int total = CountInstancedChips();
            if (total == 0)
            {
                return 0;
            }

            if (planes == null)
            {
                return Mathf.Min(total, EffectiveVisualChipCount);
            }

            int visible = 0;
            int evaluated = 0;
            Vector3 boundsSize = Vector3.one * Mathf.Max(visualMicroChipScale * 2.5f, 0.02f);
            for (int batch = 0; batch < visualMicroChipBatches.Length; batch++)
            {
                List<Matrix4x4> matrices = visualMicroChipBatches[batch];
                for (int i = 0; i < matrices.Count; i++)
                {
                    if (evaluated >= EffectiveVisualChipCount)
                    {
                        return visible;
                    }

                    evaluated++;
                    Bounds bounds = new Bounds(matrices[i].GetColumn(3), boundsSize);
                    if (GeometryUtility.TestPlanesAABB(planes, bounds))
                    {
                        visible++;
                    }
                }
            }

            return visible;
        }

        private void DrawVisualMicroChips()
        {
            if (!visualOnlyMicroChips || CountInstancedChips() == 0)
            {
                return;
            }

            EnsureVisualMicroChipResources();
            int drawn = 0;
            for (int materialIndex = 0; materialIndex < visualMicroChipBatches.Length; materialIndex++)
            {
                List<Matrix4x4> matrices = visualMicroChipBatches[materialIndex];
                Material material = visualMicroChipMaterials[materialIndex];
                int remaining = EffectiveVisualChipCount - drawn;
                int materialCount = Mathf.Min(matrices.Count, Mathf.Max(0, remaining));
                for (int start = 0; start < materialCount; start += InstancedChipBatchSize)
                {
                    int count = Mathf.Min(InstancedChipBatchSize, materialCount - start);
                    matrices.CopyTo(start, instancedChipDrawBuffer, 0, count);
                    Graphics.DrawMeshInstanced(
                        visualMicroChipMesh,
                        0,
                        material,
                        instancedChipDrawBuffer,
                        count,
                        null,
                        ShadowCastingMode.Off,
                        false,
                        visualMicroChipLayer);
                    drawn += count;
                }
            }
        }

        private int CountInstancedChips()
        {
            int count = 0;
            for (int i = 0; i < visualMicroChipBatches.Length; i++)
            {
                count += visualMicroChipBatches[i].Count;
            }

            return count;
        }

        private void EnsureVisualMicroChipResources()
        {
            if (visualMicroChipMesh == null)
            {
                visualMicroChipMesh = CreateVisualMicroChipMesh();
            }

            if (visualMicroChipMaterials != null && visualMicroChipMaterials.Length == visualMicroChipBatches.Length)
            {
                return;
            }

            visualMicroChipMaterials = new Material[visualMicroChipBatches.Length];
            Color[] palette =
            {
                new Color(0.58f, 0.9f, 1f, 0.58f),
                new Color(1f, 0.86f, 0.64f, 0.62f),
                new Color(0.92f, 0.74f, 1f, 0.56f),
                new Color(0.76f, 1f, 0.78f, 0.54f)
            };

            for (int i = 0; i < visualMicroChipMaterials.Length; i++)
            {
                Color tint = Color.Lerp(Color.white, palette[i], chipColorVariation);
                float shimmer = Mathf.Lerp(0.85f, 1.85f, chipSparkleVariation);
                Material material = new Material(Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Color"))
                {
                    name = $"Runtime Instanced Micro Chips {i}",
                    enableInstancing = true
                };
                SetMaterialColor(material, "_BaseColor", tint * shimmer);
                SetMaterialColor(material, "_UnlitColor", tint * shimmer);
                if (material.HasProperty("_SurfaceType"))
                {
                    material.SetFloat("_SurfaceType", 1f);
                }

                if (material.HasProperty("_BlendMode"))
                {
                    material.SetFloat("_BlendMode", 0f);
                }

                if (material.HasProperty("_DoubleSidedEnable"))
                {
                    material.SetFloat("_DoubleSidedEnable", 1f);
                }

                material.renderQueue = (int)RenderQueue.Transparent + 30 + i;
                visualMicroChipMaterials[i] = material;
            }
        }

        private static Mesh CreateVisualMicroChipMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "Instanced Visual Micro Chip"
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.35f),
                new Vector3(0.55f, 0f, -0.18f),
                new Vector3(0.28f, 0f, 0.44f),
                new Vector3(-0.34f, 0f, 0.32f),
                new Vector3(0.05f, 0.22f, 0.03f),
                new Vector3(0.02f, -0.12f, -0.02f)
            };
            mesh.triangles = new[]
            {
                0, 4, 1,
                1, 4, 2,
                2, 4, 3,
                3, 4, 0,
                1, 5, 0,
                2, 5, 1,
                3, 5, 2,
                0, 5, 3
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SetMaterialColor(Material material, string propertyName, Color color)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, color);
            }
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
                return dominantGemScaleMultiplier * Mathf.Lerp(1f, 0.42f, largeGemReduction);
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

        private static bool IsMediumDefinition(GemstoneDefinition definition)
        {
            return definition != null &&
                !definition.IsMicroParticle &&
                definition.particleCategory != GemstoneParticleCategory.HeavyAnchor;
        }

        private static GemstoneDefinition ResolveDefinition(GameObject instance)
        {
            GemstonePhysicsSetup setup = instance != null ? instance.GetComponent<GemstonePhysicsSetup>() : null;
            return setup != null ? setup.Definition : null;
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
