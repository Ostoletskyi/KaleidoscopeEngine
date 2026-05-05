using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public enum CrystalCassetteQuality
    {
        Minimal,
        Low,
        Medium,
        High,
        Ultra,
        Extreme
    }

    [DisallowMultipleComponent]
    public sealed class PackedRearCrystalCassette : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField, Min(0.5f)] private float tubeLength = 10f;
        [SerializeField] private float rearWallPosition = 5f;
        [SerializeField, Range(0.05f, 0.8f)] private float cassetteDepth = 0.5f;
        [SerializeField, Range(0.05f, 4f)] private float cassetteRadius = 1f;
        [SerializeField, Range(0.2f, 3f)] private float packingDensity = 1.25f;
        [SerializeField, Range(0f, 0.2f)] private float depthJitter = 0.035f;
        [SerializeField, Range(0f, 0.25f)] private float radialJitter = 0.04f;
        [SerializeField] private bool keepNearRearWall = true;

        [Header("Counts")]
        [SerializeField, Min(0)] private int crystalCount = 2200;
        [SerializeField, Min(0)] private int visualOnlyCrystalCount = 2000;
        [SerializeField, Range(0, 100)] private int physicsCrystalCount = 80;
        [SerializeField] private CrystalCassetteQuality quality = CrystalCassetteQuality.High;
        [SerializeField] private bool useVisualOnlyCrystals = true;
        [SerializeField] private bool usePhysicsCrystals = true;

        [Header("Containment")]
        [SerializeField] private bool buildInvisibleBarriers = true;
        [SerializeField, Range(0f, 2f)] private float softRearBiasForce = 0.35f;
        [SerializeField, Range(0f, 2f)] private float microTumbleTorque = 0.18f;
        [SerializeField] private string physicsOnlyLayerName = "KaleidoscopePhysicsOnly";

        [Header("Palette Weights")]
        [SerializeField, Min(0f)] private float greenWeight = 1.2f;
        [SerializeField, Min(0f)] private float redWeight = 1f;
        [SerializeField, Min(0f)] private float blueWeight = 1.05f;
        [SerializeField, Min(0f)] private float orangeWeight = 0.85f;
        [SerializeField, Min(0f)] private float burgundyWeight = 0.75f;
        [SerializeField, Min(0f)] private float scarletWeight = 0.9f;
        [SerializeField, Min(0f)] private float cyanWeight = 0.45f;
        [SerializeField, Min(0f)] private float clearWeight = 0.35f;

        [Header("Palette Variation")]
        [SerializeField, Range(0f, 0.12f)] private float hueVariation = 0.035f;
        [SerializeField, Range(0f, 0.35f)] private float saturationVariation = 0.14f;
        [SerializeField, Range(0f, 0.45f)] private float brightnessVariation = 0.2f;
        [SerializeField, Range(0f, 0.45f)] private float transparencyVariation = 0.16f;
        [SerializeField, Range(0f, 1f)] private float sparkleResponseVariation = 0.42f;

        private Transform barrierRoot;

        public float TubeLength => tubeLength;
        public float RearWallPosition => rearWallPosition;
        public float CassetteDepth => cassetteDepth;
        public float CassetteRadius => cassetteRadius;
        public float FrontLimiterPosition => rearWallPosition - cassetteDepth;
        public int CrystalCount => crystalCount;
        public int VisualOnlyCrystalCount => visualOnlyCrystalCount;
        public int PhysicsCrystalCount => physicsCrystalCount;
        public bool UseVisualOnlyCrystals => useVisualOnlyCrystals;
        public bool UsePhysicsCrystals => usePhysicsCrystals;

        private void Awake()
        {
            ApplyQuality(quality);
            if (buildInvisibleBarriers)
            {
                BuildBarriers();
            }
        }

        private void FixedUpdate()
        {
            if (!keepNearRearWall || (!usePhysicsCrystals && softRearBiasForce <= 0f && microTumbleTorque <= 0f))
            {
                return;
            }

            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>();
            Vector3 rearPlane = transform.TransformPoint(new Vector3(rearWallPosition, 0f, 0f));
            Vector3 rearDirection = transform.right;
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body.isKinematic)
                {
                    continue;
                }

                float distanceFromRear = Vector3.Dot(body.worldCenterOfMass - rearPlane, rearDirection);
                if (distanceFromRear < -cassetteDepth)
                {
                    body.AddForce(rearDirection * softRearBiasForce, ForceMode.Acceleration);
                }

                if (microTumbleTorque > 0f)
                {
                    body.AddTorque(Random.insideUnitSphere * microTumbleTorque, ForceMode.Acceleration);
                }
            }
        }

        public void Configure(float radius, float length)
        {
            cassetteRadius = Mathf.Max(0.05f, radius);
            tubeLength = Mathf.Max(0.5f, length);
            rearWallPosition = tubeLength * 0.5f;
            BuildBarriers();
        }

        public Vector3 RandomLocalPoint(System.Random random, float objectRadius)
        {
            float safeRadius = Mathf.Max(0.05f, cassetteRadius - objectRadius);
            float angle = RandomRange(random, 0f, Mathf.PI * 2f);
            float radial = Mathf.Pow(Mathf.Clamp01((float)random.NextDouble()), Mathf.Lerp(0.28f, 0.62f, packingDensity / 3f)) * safeRadius;
            radial += RandomRange(random, -radialJitter, radialJitter);
            float depth = RandomRange(random, 0f, Mathf.Max(0.02f, cassetteDepth));
            depth += RandomRange(random, -depthJitter, depthJitter);
            float x = rearWallPosition - Mathf.Clamp(depth, 0f, cassetteDepth) - objectRadius * 0.5f;
            return new Vector3(x, Mathf.Cos(angle) * radial, Mathf.Sin(angle) * radial);
        }

        public Color PickColor(System.Random random)
        {
            Color baseColor = PickWeightedBaseColor(random);
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            h = Mathf.Repeat(h + RandomRange(random, -hueVariation, hueVariation), 1f);
            s = Mathf.Clamp01(s + RandomRange(random, -saturationVariation, saturationVariation));
            v = Mathf.Clamp01(v + RandomRange(random, -brightnessVariation, brightnessVariation));
            Color varied = Color.HSVToRGB(h, s, v);
            varied.a = Mathf.Clamp01(baseColor.a + RandomRange(random, -transparencyVariation, transparencyVariation));
            return varied;
        }

        public float PickSparkleResponse(System.Random random)
        {
            return Mathf.Lerp(0.65f, 1.8f, Mathf.Clamp01((float)random.NextDouble() * sparkleResponseVariation));
        }

        public void ApplyQuality(CrystalCassetteQuality targetQuality)
        {
            quality = targetQuality;
            visualOnlyCrystalCount = Mathf.Max(visualOnlyCrystalCount, ResolveVisualCount(quality));
            physicsCrystalCount = Mathf.Clamp(physicsCrystalCount, 40, 100);
            crystalCount = visualOnlyCrystalCount + physicsCrystalCount;
        }

        public bool ContainsLocalPoint(Vector3 localPoint, float padding)
        {
            float front = rearWallPosition - cassetteDepth - Mathf.Max(0f, padding);
            float rear = rearWallPosition + Mathf.Max(0f, padding);
            float radial = new Vector2(localPoint.y, localPoint.z).magnitude;
            return localPoint.x >= front && localPoint.x <= rear && radial <= cassetteRadius + padding;
        }

        private void BuildBarriers()
        {
            if (!buildInvisibleBarriers)
            {
                return;
            }

            if (barrierRoot == null)
            {
                GameObject root = new GameObject("Packed Rear Crystal Cassette Barriers");
                root.layer = ResolveLayer(physicsOnlyLayerName);
                root.transform.SetParent(transform, false);
                barrierRoot = root.transform;
            }

            EnsureBoxBarrier("Rear Barrier", new Vector3(rearWallPosition + 0.025f, 0f, 0f), new Vector3(0.05f, cassetteRadius * 2.2f, cassetteRadius * 2.2f));
            EnsureBoxBarrier("Front Limiter", new Vector3(rearWallPosition - cassetteDepth - 0.025f, 0f, 0f), new Vector3(0.05f, cassetteRadius * 2.2f, cassetteRadius * 2.2f));
            EnsureRadialBarrierRing();
        }

        private void EnsureBoxBarrier(string objectName, Vector3 localPosition, Vector3 size)
        {
            Transform child = barrierRoot.Find(objectName);
            BoxCollider collider = child != null ? child.GetComponent<BoxCollider>() : null;
            if (collider == null)
            {
                GameObject barrier = new GameObject(objectName);
                barrier.layer = barrierRoot.gameObject.layer;
                barrier.transform.SetParent(barrierRoot, false);
                collider = barrier.AddComponent<BoxCollider>();
            }

            collider.transform.localPosition = localPosition;
            collider.transform.localRotation = Quaternion.identity;
            collider.size = size;
        }

        private void EnsureRadialBarrierRing()
        {
            const int segmentCount = 12;
            float wallThickness = Mathf.Max(0.05f, cassetteRadius * 0.08f);
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / segmentCount;
                Vector3 radial = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 tangent = new Vector3(0f, -Mathf.Sin(angle), Mathf.Cos(angle));
                string objectName = $"Radial Limiter {i:00}";
                Transform child = barrierRoot.Find(objectName);
                BoxCollider collider = child != null ? child.GetComponent<BoxCollider>() : null;
                if (collider == null)
                {
                    GameObject barrier = new GameObject(objectName);
                    barrier.layer = barrierRoot.gameObject.layer;
                    barrier.transform.SetParent(barrierRoot, false);
                    collider = barrier.AddComponent<BoxCollider>();
                }

                collider.transform.localPosition = new Vector3(rearWallPosition - cassetteDepth * 0.5f, radial.y * cassetteRadius, radial.z * cassetteRadius);
                collider.transform.localRotation = Quaternion.LookRotation(tangent, radial);
                collider.size = new Vector3(cassetteDepth + wallThickness, wallThickness, cassetteRadius * 0.72f);
            }
        }

        private Color PickWeightedBaseColor(System.Random random)
        {
            float total = greenWeight + redWeight + blueWeight + orangeWeight + burgundyWeight + scarletWeight + cyanWeight + clearWeight;
            float roll = RandomRange(random, 0f, Mathf.Max(0.001f, total));
            if ((roll -= greenWeight) <= 0f) return new Color(0.02f, 0.82f, 0.32f, 0.86f); // EmeraldGreen
            if ((roll -= redWeight) <= 0f) return new Color(0.86f, 0.02f, 0.08f, 0.86f); // RubyRed
            if ((roll -= blueWeight) <= 0f) return new Color(0.04f, 0.22f, 0.95f, 0.86f); // SapphireBlue
            if ((roll -= orangeWeight) <= 0f) return new Color(1f, 0.48f, 0.04f, 0.82f); // AmberOrange
            if ((roll -= burgundyWeight) <= 0f) return new Color(0.45f, 0.02f, 0.15f, 0.84f); // Burgundy
            if ((roll -= scarletWeight) <= 0f) return new Color(1f, 0.06f, 0.02f, 0.86f); // Scarlet
            if ((roll -= cyanWeight) <= 0f) return new Color(0.22f, 0.95f, 1f, 0.56f); // CyanGlass
            return new Color(1f, 1f, 1f, 0.42f); // ClearQuartz
        }

        private static int ResolveVisualCount(CrystalCassetteQuality targetQuality)
        {
            switch (targetQuality)
            {
                case CrystalCassetteQuality.Minimal: return 200;
                case CrystalCassetteQuality.Low: return 500;
                case CrystalCassetteQuality.Medium: return 1000;
                case CrystalCassetteQuality.Ultra: return 3500;
                case CrystalCassetteQuality.Extreme: return 5200;
                default: return 2000;
            }
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private static int ResolveLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }
    }
}
