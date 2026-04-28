using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class GemstonePhysicsSetup : MonoBehaviour
    {
        [Header("Runtime State")]
        [SerializeField] private GemstoneDefinition definition;

        [Header("Fallback Defaults")]
        [SerializeField] private float defaultDrag = 0.05f;
        [SerializeField] private float defaultAngularDrag = 0.9f;
        [SerializeField] private float microParticleDrag = 0.22f;
        [SerializeField] private float microParticleAngularDrag = 1.2f;
        [SerializeField] private float maxLinearVelocity = 5.5f;
        [SerializeField] private float maxAngularVelocity = 8f;
        [SerializeField] private float maxDepenetrationVelocity = 3.5f;

        private Rigidbody body;

        public GemstoneDefinition Definition => definition;
        public Rigidbody Body => body;

        public void Configure(GemstoneDefinition gemstoneDefinition, System.Random random, int gemLayer, int microParticleLayer)
        {
            definition = gemstoneDefinition;

            if (definition == null)
            {
                Debug.LogWarning($"{nameof(GemstonePhysicsSetup)} on {name} has no definition.", this);
                return;
            }

            gameObject.layer = definition.IsMicroParticle ? microParticleLayer : gemLayer;
            ApplyLayerToChildren(transform, gameObject.layer);

            transform.localScale = RandomVector(random, definition.minScale, definition.maxScale);

            EnsureCollider(definition);
            ConfigureRigidbody(random);
            ConfigureMotionPersonality(random);
        }

        private void ConfigureRigidbody(System.Random random)
        {
            body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            float volume = Mathf.Max(0.001f, transform.localScale.x * transform.localScale.y * transform.localScale.z);
            float densityMass = volume * definition.density;
            body.mass = Mathf.Clamp(densityMass, definition.minMass, definition.maxMass);
            body.drag = definition.IsMicroParticle ? microParticleDrag : defaultDrag;
            body.angularDrag = definition.IsMicroParticle ? microParticleAngularDrag : defaultAngularDrag;
            body.useGravity = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.sleepThreshold = definition.sleepThreshold;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = maxAngularVelocity;
            body.maxDepenetrationVelocity = maxDepenetrationVelocity;

            body.centerOfMass = RandomVector(
                random,
                -definition.centerOfMassOffsetRange,
                definition.centerOfMassOffsetRange);

            Vector3 axis = RandomUnitVector(random);
            float speed = RandomRange(random, definition.angularVelocityRange.x, definition.angularVelocityRange.y);
            body.angularVelocity = axis * speed;

            Vector3 impulse = Vector3.ProjectOnPlane(RandomUnitVector(random), Vector3.up).normalized;
            if (impulse.sqrMagnitude > 0.001f)
            {
                body.velocity += impulse * RandomRange(random, definition.spawnImpulseRange.x, definition.spawnImpulseRange.y) / body.mass;
            }

            GemstoneVelocityLimiter limiter = GetComponent<GemstoneVelocityLimiter>();
            if (limiter == null)
            {
                limiter = gameObject.AddComponent<GemstoneVelocityLimiter>();
            }

            limiter.Configure(body, maxLinearVelocity, maxAngularVelocity);
        }

        private void EnsureCollider(GemstoneDefinition gemstoneDefinition)
        {
            BuildCompoundCollider(gemstoneDefinition);

            if (GetComponentInChildren<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            PhysicMaterial material = new PhysicMaterial($"{gemstoneDefinition.displayName} Physics")
            {
                dynamicFriction = gemstoneDefinition.friction,
                staticFriction = Mathf.Clamp01(gemstoneDefinition.friction * 1.15f),
                bounciness = gemstoneDefinition.bounciness,
                frictionCombine = gemstoneDefinition.frictionCombine,
                bounceCombine = gemstoneDefinition.bounceCombine
            };

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.sharedMaterial = material;
            }
        }

        private void BuildCompoundCollider(GemstoneDefinition gemstoneDefinition)
        {
            Collider rootCollider = GetComponent<Collider>();
            if (rootCollider == null || gemstoneDefinition.prefab != null)
            {
                return;
            }

            if (gemstoneDefinition.shapeHint == GemstoneShapeHint.Rounded ||
                gemstoneDefinition.shapeHint == GemstoneShapeHint.Pebble ||
                gemstoneDefinition.shapeHint == GemstoneShapeHint.Elongated)
            {
                return;
            }

            BoxCollider main = rootCollider as BoxCollider;
            if (main == null)
            {
                main = gameObject.AddComponent<BoxCollider>();
            }

            main.size = gemstoneDefinition.shapeHint == GemstoneShapeHint.ThinShard
                ? new Vector3(1f, 0.42f, 0.72f)
                : new Vector3(0.9f, 0.82f, 1f);

            BoxCollider biteA = gameObject.AddComponent<BoxCollider>();
            biteA.center = new Vector3(0.24f, 0.08f, 0.12f);
            biteA.size = gemstoneDefinition.shapeHint == GemstoneShapeHint.ThinShard
                ? new Vector3(0.68f, 0.3f, 0.42f)
                : new Vector3(0.48f, 0.62f, 0.52f);

            BoxCollider biteB = gameObject.AddComponent<BoxCollider>();
            biteB.center = new Vector3(-0.22f, -0.06f, -0.18f);
            biteB.size = gemstoneDefinition.shapeHint == GemstoneShapeHint.ThinShard
                ? new Vector3(0.52f, 0.22f, 0.38f)
                : new Vector3(0.42f, 0.48f, 0.46f);
        }

        private void ConfigureMotionPersonality(System.Random random)
        {
            if (definition.restlessness <= 0f && definition.inertiaLag <= 0f)
            {
                return;
            }

            GemstoneMotionPersonality personality = GetComponent<GemstoneMotionPersonality>();
            if (personality == null)
            {
                personality = gameObject.AddComponent<GemstoneMotionPersonality>();
            }

            personality.Configure(definition, body, random);
        }

        private static void ApplyLayerToChildren(Transform root, int layer)
        {
            foreach (Transform child in root)
            {
                child.gameObject.layer = layer;
                ApplyLayerToChildren(child, layer);
            }
        }

        private static Vector3 RandomVector(System.Random random, Vector3 min, Vector3 max)
        {
            return new Vector3(
                RandomRange(random, min.x, max.x),
                RandomRange(random, min.y, max.y),
                RandomRange(random, min.z, max.z));
        }

        private static Vector3 RandomUnitVector(System.Random random)
        {
            Vector3 vector = new Vector3(
                RandomRange(random, -1f, 1f),
                RandomRange(random, -1f, 1f),
                RandomRange(random, -1f, 1f));

            return vector.sqrMagnitude > 0.001f ? vector.normalized : Vector3.up;
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            if (max < min)
            {
                (min, max) = (max, min);
            }

            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
