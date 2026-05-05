using System.Collections.Generic;
using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class OpticalMixingChamber : MonoBehaviour
    {
        [Header("Active Optical Volume")]
        [SerializeField, Range(0.3f, 0.8f)] private float chamberDepth = 0.62f;
        [SerializeField, Range(0.2f, 2f)] private float chamberRadius = 0.92f;
        [SerializeField, Range(0.2f, 3f)] private float packingDensity = 1.55f;
        [SerializeField] private Vector3 chamberCenterOffset = new Vector3(2.15f, -0.05f, 0f);

        [Header("Compression Forces")]
        [SerializeField, Range(0f, 6f)] private float radialBias = 2.2f;
        [SerializeField, Range(0f, 3f)] private float turbulenceStrength = 0.38f;
        [SerializeField, Range(0f, 6f)] private float opticalCompression = 2.8f;
        [SerializeField, Range(0f, 1f)] private float shardPackingAmount = 0.82f;

        [Header("Containment")]
        [SerializeField] private bool buildTransparentSurfaces = true;
        [SerializeField] private string physicsOnlyLayerName = "KaleidoscopePhysicsOnly";
        [SerializeField] private string chamberVisualLayerName = "KaleidoscopeChamberVisual";

        private Transform chamberTransform;
        private Transform volumeRoot;
        private BoxCollider frontBarrier;
        private BoxCollider rearBarrier;
        private readonly List<BoxCollider> radialBarriers = new List<BoxCollider>();
        private Material glassMaterial;
        private PhysicMaterial boundaryMaterial;

        public float ChamberDepth => chamberDepth;
        public float ChamberRadius => chamberRadius;
        public float PackingDensity => packingDensity;
        public float RadialBias => radialBias;
        public float TurbulenceStrength => turbulenceStrength;
        public float OpticalCompression => opticalCompression;
        public float ShardPackingAmount => shardPackingAmount;
        public Vector3 ChamberCenterOffset => chamberCenterOffset;
        public float FrontLocalX => chamberCenterOffset.x - chamberDepth * 0.5f;
        public float RearLocalX => chamberCenterOffset.x + chamberDepth * 0.5f;

        public void Configure(Transform targetChamber, float radius, float tubeLength, string physicsLayer, string visualLayer)
        {
            chamberTransform = targetChamber != null ? targetChamber : transform;
            chamberRadius = Mathf.Clamp(radius * 0.72f, 0.2f, 2f);
            chamberDepth = Mathf.Clamp(chamberDepth, 0.3f, 0.8f);
            chamberCenterOffset = new Vector3(tubeLength * 0.5f - chamberDepth * 0.55f, -0.05f, 0f);
            if (!string.IsNullOrWhiteSpace(physicsLayer)) physicsOnlyLayerName = physicsLayer;
            if (!string.IsNullOrWhiteSpace(visualLayer)) chamberVisualLayerName = visualLayer;
            BuildVolume();
        }

        private void FixedUpdate()
        {
            ApplyForces();
        }

        private void LateUpdate()
        {
            BuildVolume();
        }

        public Vector3 RandomLocalPoint(System.Random random, float objectRadius)
        {
            float safeRadius = Mathf.Max(0.05f, chamberRadius - objectRadius);
            float angle = RandomRange(random, 0f, Mathf.PI * 2f);
            float radialRoll = Mathf.Clamp01((float)random.NextDouble());
            float radial = Mathf.Pow(radialRoll, Mathf.Lerp(0.28f, 0.64f, packingDensity / 3f)) * safeRadius;
            float depth = RandomRange(random, -chamberDepth * 0.48f, chamberDepth * 0.48f);
            float y = Mathf.Cos(angle) * radial * Mathf.Lerp(0.72f, 1f, shardPackingAmount) - chamberRadius * 0.12f;
            float z = Mathf.Sin(angle) * radial;
            return chamberCenterOffset + new Vector3(depth, y, z);
        }

        public bool ContainsLocalPoint(Vector3 localPoint, float padding)
        {
            float radius = new Vector2(localPoint.y - chamberCenterOffset.y, localPoint.z - chamberCenterOffset.z).magnitude;
            return localPoint.x >= FrontLocalX - padding &&
                localPoint.x <= RearLocalX + padding &&
                radius <= chamberRadius + padding;
        }

        private void BuildVolume()
        {
            if (chamberTransform == null)
            {
                chamberTransform = transform;
            }

            if (volumeRoot == null)
            {
                GameObject root = new GameObject("Optical Mixing Chamber");
                root.layer = ResolveLayer(physicsOnlyLayerName);
                root.transform.SetParent(chamberTransform, false);
                volumeRoot = root.transform;
            }

            if (boundaryMaterial == null)
            {
                boundaryMaterial = new PhysicMaterial("Optical Mixing Chamber Boundary")
                {
                    dynamicFriction = 0.72f,
                    staticFriction = 0.86f,
                    bounciness = 0.025f,
                    frictionCombine = PhysicMaterialCombine.Average,
                    bounceCombine = PhysicMaterialCombine.Minimum
                };
            }

            frontBarrier = EnsureBox(frontBarrier, "Front Transparent Containment");
            rearBarrier = EnsureBox(rearBarrier, "Rear Transparent Containment");
            ConfigureBox(frontBarrier, FrontLocalX - 0.025f, chamberRadius * 2.15f);
            ConfigureBox(rearBarrier, RearLocalX + 0.025f, chamberRadius * 2.15f);
            EnsureRadialLimiters();

            if (buildTransparentSurfaces)
            {
                EnsureGlassDisc("Front Glass Surface", FrontLocalX, 0);
                EnsureGlassDisc("Rear Glass Surface", RearLocalX, 1);
            }
        }

        private BoxCollider EnsureBox(BoxCollider collider, string name)
        {
            if (collider != null) return collider;
            GameObject obj = new GameObject(name);
            obj.layer = ResolveLayer(physicsOnlyLayerName);
            obj.transform.SetParent(volumeRoot, false);
            collider = obj.AddComponent<BoxCollider>();
            collider.sharedMaterial = boundaryMaterial;
            return collider;
        }

        private void ConfigureBox(BoxCollider collider, float localX, float diameter)
        {
            collider.transform.localPosition = new Vector3(localX, chamberCenterOffset.y, chamberCenterOffset.z);
            collider.transform.localRotation = Quaternion.identity;
            collider.size = new Vector3(0.05f, diameter, diameter);
        }

        private void EnsureRadialLimiters()
        {
            const int segments = 16;
            while (radialBarriers.Count < segments)
            {
                GameObject obj = new GameObject($"Radial Optical Boundary {radialBarriers.Count:00}");
                obj.layer = ResolveLayer(physicsOnlyLayerName);
                obj.transform.SetParent(volumeRoot, false);
                BoxCollider collider = obj.AddComponent<BoxCollider>();
                collider.sharedMaterial = boundaryMaterial;
                radialBarriers.Add(collider);
            }

            float wallThickness = Mathf.Max(0.04f, chamberRadius * 0.055f);
            for (int i = 0; i < radialBarriers.Count; i++)
            {
                float angle = i * Mathf.PI * 2f / radialBarriers.Count;
                Vector3 radial = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 tangent = new Vector3(0f, -Mathf.Sin(angle), Mathf.Cos(angle));
                BoxCollider collider = radialBarriers[i];
                collider.transform.localPosition = chamberCenterOffset + radial * chamberRadius;
                collider.transform.localRotation = Quaternion.LookRotation(tangent, radial);
                collider.size = new Vector3(chamberDepth + wallThickness, wallThickness, chamberRadius * 0.56f);
            }
        }

        private void EnsureGlassDisc(string name, float localX, int siblingIndex)
        {
            Transform existing = volumeRoot.Find(name);
            Renderer renderer;
            if (existing == null)
            {
                GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                disc.name = name;
                disc.layer = ResolveLayer(chamberVisualLayerName);
                disc.transform.SetParent(volumeRoot, false);
                Object.Destroy(disc.GetComponent<Collider>());
                renderer = disc.GetComponent<Renderer>();
            }
            else
            {
                renderer = existing.GetComponent<Renderer>();
            }

            if (renderer == null) return;
            renderer.transform.localPosition = new Vector3(localX, chamberCenterOffset.y, chamberCenterOffset.z);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            renderer.transform.localScale = new Vector3(chamberRadius * 2f, 0.012f, chamberRadius * 2f);
            renderer.sharedMaterial = EnsureGlassMaterial();
            renderer.transform.SetSiblingIndex(siblingIndex);
        }

        private Material EnsureGlassMaterial()
        {
            if (glassMaterial != null) return glassMaterial;
            glassMaterial = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"))
            {
                name = "Runtime Optical Mixing Chamber Glass",
                renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 4
            };
            SetColor(glassMaterial, "_BaseColor", new Color(0.78f, 0.92f, 1f, 0.12f));
            SetColor(glassMaterial, "_Color", new Color(0.78f, 0.92f, 1f, 0.12f));
            SetFloat(glassMaterial, "_SurfaceType", 1f);
            SetFloat(glassMaterial, "_BlendMode", 0f);
            SetFloat(glassMaterial, "_Smoothness", 0.96f);
            SetFloat(glassMaterial, "_Metallic", 0f);
            SetFloat(glassMaterial, "_DoubleSidedEnable", 1f);
            return glassMaterial;
        }

        private void ApplyForces()
        {
            Rigidbody[] bodies = chamberTransform != null ? chamberTransform.root.GetComponentsInChildren<Rigidbody>() : GetComponentsInChildren<Rigidbody>();
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body.isKinematic) continue;

                Vector3 local = chamberTransform.InverseTransformPoint(body.worldCenterOfMass);
                if (!ContainsLocalPoint(local, chamberRadius * 0.25f)) continue;

                Vector3 radialLocal = new Vector3(0f, local.y - chamberCenterOffset.y, local.z - chamberCenterOffset.z);
                float radialDistance = new Vector2(radialLocal.y, radialLocal.z).magnitude;
                Vector3 force = Vector3.zero;
                if (radialDistance > 0.001f)
                {
                    float targetRadius = chamberRadius * Mathf.Lerp(0.84f, 0.58f, shardPackingAmount);
                    float radialError = Mathf.Clamp01((radialDistance - targetRadius) / Mathf.Max(0.05f, chamberRadius - targetRadius));
                    force -= chamberTransform.TransformDirection(radialLocal.normalized) * radialError * radialBias;
                }

                float xError = Mathf.Clamp((chamberCenterOffset.x - local.x) / Mathf.Max(0.05f, chamberDepth), -1f, 1f);
                force += chamberTransform.right * xError * opticalCompression;

                if (turbulenceStrength > 0f)
                {
                    float t = Time.time * 0.7f + i * 0.173f;
                    Vector3 swirl = new Vector3(0f, Mathf.Sin(t), Mathf.Cos(t * 1.31f)) * turbulenceStrength;
                    force += chamberTransform.TransformDirection(swirl);
                }

                body.AddForce(force, ForceMode.Acceleration);
                body.AddTorque(Random.insideUnitSphere * turbulenceStrength * 0.2f, ForceMode.Acceleration);
            }
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private static void SetColor(Material material, string property, Color value)
        {
            if (material.HasProperty(property)) material.SetColor(property, value);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property)) material.SetFloat(property, value);
        }

        private static int ResolveLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }
    }
}
