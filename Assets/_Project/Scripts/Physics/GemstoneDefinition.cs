using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public enum GemstoneParticleCategory
    {
        HeavyAnchor,
        Gem,
        Slider,
        GlassFragment,
        MicroParticle,
        Dust
    }

    public enum GemstoneShapeHint
    {
        Rounded,
        Faceted,
        Elongated,
        Shard,
        ThinShard,
        Pebble
    }

    [CreateAssetMenu(
        fileName = "GemstoneDefinition",
        menuName = "Kaleidoscope Engine/Physics/Gemstone Definition")]
    public sealed class GemstoneDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id used by spawners, debug output, and later rendering stages.")]
        public string id = "opal";

        public string displayName = "Opal";

        [Tooltip("Optional prefab. If unset, the spawner creates a primitive placeholder from the shape hint.")]
        public GameObject prefab;

        [Header("Placeholder Shape")]
        public GemstoneShapeHint shapeHint = GemstoneShapeHint.Rounded;
        public Color placeholderColor = Color.white;

        [Header("Scale")]
        public Vector3 minScale = Vector3.one * 0.16f;
        public Vector3 maxScale = Vector3.one * 0.32f;

        [Header("Mass")]
        [Min(0.001f)] public float minMass = 0.08f;
        [Min(0.001f)] public float maxMass = 0.28f;
        [Tooltip("Relative material density used to make large and small pieces belong to a coherent mass ecology.")]
        [Min(0.001f)] public float density = 1f;

        [Header("Surface")]
        [Range(0f, 2f)] public float friction = 0.75f;
        [Range(0f, 1f)] public float bounciness = 0.08f;
        public PhysicMaterialCombine frictionCombine = PhysicMaterialCombine.Average;
        public PhysicMaterialCombine bounceCombine = PhysicMaterialCombine.Minimum;

        [Header("Motion Variation")]
        [Tooltip("Maximum absolute local center-of-mass offset applied per axis after scale is chosen.")]
        public Vector3 centerOfMassOffsetRange = new Vector3(0.03f, 0.03f, 0.03f);

        [Min(0f)] public float spawnWeight = 1f;
        public Vector2 angularVelocityRange = new Vector2(0.5f, 7f);
        [Tooltip("Extra sideways impulse on spawn. Higher values make shards separate instead of forming a static clump.")]
        public Vector2 spawnImpulseRange = new Vector2(0.01f, 0.08f);

        [Header("Settling")]
        [Range(0f, 0.3f)] public float sleepThreshold = 0.035f;
        [Range(0f, 1f)] public float lowMotionWakeThreshold = 0.055f;
        [Range(0f, 0.08f)] public float restlessness = 0.012f;
        [Range(0f, 1f)] public float inertiaLag = 0.35f;

        [Header("Category")]
        public GemstoneParticleCategory particleCategory = GemstoneParticleCategory.Gem;

        public bool IsMicroParticle =>
            particleCategory == GemstoneParticleCategory.MicroParticle ||
            particleCategory == GemstoneParticleCategory.Dust;
    }
}
