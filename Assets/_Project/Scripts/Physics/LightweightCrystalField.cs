using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class LightweightCrystalField : MonoBehaviour
    {
        private const int BatchSize = 1023;

        [Header("Counts")]
        [SerializeField, Range(0, 8000)] private int visualCrystalCount = 3500;
        [SerializeField, Range(0, 3000)] private int microSparkleChipCount = 1500;

        [Header("Shape")]
        [SerializeField] private bool useSimpleDenseMeshes = true;
        [SerializeField, Range(0.004f, 0.2f)] private float visualCrystalScaleMin = 0.018f;
        [SerializeField, Range(0.01f, 0.35f)] private float visualCrystalScaleMax = 0.08f;
        [SerializeField, Range(0.05f, 1f)] private float visualCrystalAlpha = 0.62f;
        [SerializeField, Range(0f, 1f)] private float visualCrystalColorVariance = 0.45f;

        [Header("Cassette")]
        [SerializeField, Range(0.05f, 0.8f)] private float cassetteDepth = 0.5f;
        [SerializeField, Range(0.05f, 3f)] private float cassetteRadius = 0.95f;
        [SerializeField] private float rearWallOffset;
        [SerializeField, Range(0.2f, 3f)] private float packingDensity = 1.35f;
        [SerializeField, Min(0.5f)] private float tubeLength = 10f;

        [Header("Pile")]
        [SerializeField, Range(0.05f, 3f)] private float pileRadius = 0.95f;
        [SerializeField, Range(0.05f, 2f)] private float pileHeight = 0.72f;
        [SerializeField, Range(0.05f, 1.2f)] private float pileDepth = 0.5f;
        [SerializeField] private Vector3 pileCenterOffset = new Vector3(-0.18f, -0.08f, 0f);
        [SerializeField, Range(0f, 1f)] private float pileRandomDepth = 0.75f;
        [SerializeField, Range(0.1f, 3f)] private float pileSlope = 1.45f;
        [SerializeField, Range(1, 12)] private int pileLayerCount = 6;
        [SerializeField] private bool pileMixWithHeroGems = true;

        [Header("Visual Mixing")]
        [SerializeField, Range(0f, 0.4f)] private float visualPileShakeStrength = 0.12f;
        [SerializeField, Range(0.1f, 8f)] private float visualPileSettleSpeed = 2.8f;
        [SerializeField, Range(0f, 1f)] private float visualPileNoise = 0.32f;
        [SerializeField, Range(0f, 1f)] private float visualPileInertia = 0.55f;

        [Header("Rendering")]
        [SerializeField] private string visualLayerName = "KaleidoscopeParticles";
        [SerializeField] private bool fieldActive = true;

        private readonly List<Matrix4x4>[] batches =
        {
            new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>(),
            new List<Matrix4x4>(), new List<Matrix4x4>(), new List<Matrix4x4>(),
            new List<Matrix4x4>(), new List<Matrix4x4>()
        };
        private readonly Matrix4x4[] drawBuffer = new Matrix4x4[BatchSize];
        private Mesh[] meshes;
        private Material[] materials;
        private System.Random random = new System.Random(31415);
        private bool generated;
        private int renderLayer;
        private int lastVisibleToSourceCamera;
        private int lastRenderedAfterMirrorPass;
        private Vector3 pileShakeOffset;
        private Vector3 pileShakeVelocity;
        private float shakeSeed;
        private OpticalMixingChamber opticalMixingChamber;

        public int VisualCrystalCount => visualCrystalCount;
        public int MicroSparkleChipCount => microSparkleChipCount;
        public int TotalVisualCount => fieldActive ? visualCrystalCount + microSparkleChipCount : 0;
        public bool UseSimpleDenseMeshes => useSimpleDenseMeshes;
        public float CassetteDepth => cassetteDepth;
        public float CassetteRadius => cassetteRadius;
        public bool FieldActive => fieldActive;
        public int CrystalsVisibleToSourceCamera => lastVisibleToSourceCamera;
        public int CrystalsInSourceTexture => lastVisibleToSourceCamera;
        public int CrystalsRenderedAfterMirrorPass => lastRenderedAfterMirrorPass;

        private void LateUpdate()
        {
            if (!fieldActive)
            {
                return;
            }

            UpdatePileShake();
            EnsureGenerated();
            Draw();
        }

        public void Configure(float radius, float length, PackedRearCrystalCassette cassette)
        {
            cassetteRadius = Mathf.Max(0.05f, radius);
            pileRadius = cassetteRadius;
            tubeLength = Mathf.Max(0.5f, length);
            if (cassette != null)
            {
                cassetteDepth = Mathf.Clamp(cassette.CassetteDepth, 0.3f, 0.5f);
                pileDepth = cassetteDepth;
                rearWallOffset = cassette.RearWallPosition - tubeLength * 0.5f;
                cassetteRadius = cassette.CassetteRadius;
                pileRadius = cassetteRadius;
            }

            generated = false;
        }

        public void Configure(OpticalMixingChamber chamber)
        {
            opticalMixingChamber = chamber;
            if (opticalMixingChamber == null)
            {
                return;
            }

            cassetteRadius = opticalMixingChamber.ChamberRadius;
            pileRadius = opticalMixingChamber.ChamberRadius;
            cassetteDepth = opticalMixingChamber.ChamberDepth;
            pileDepth = opticalMixingChamber.ChamberDepth;
            packingDensity = opticalMixingChamber.PackingDensity;
            pileCenterOffset = opticalMixingChamber.ChamberCenterOffset + new Vector3(0f, -opticalMixingChamber.ChamberRadius * 0.08f, 0f);
            rearWallOffset = 0f;
            generated = false;
        }

        public void SetFieldActive(bool active)
        {
            fieldActive = active;
        }

        public void SetVisualCrystalCount(int count)
        {
            visualCrystalCount = Mathf.Clamp(count, 0, 8000);
            generated = false;
        }

        public void ApplyQuality(KaleidoscopeEngine.Mirrors.KaleidoscopeQualityLevel quality)
        {
            switch (quality)
            {
                case KaleidoscopeEngine.Mirrors.KaleidoscopeQualityLevel.Minimal:
                    visualCrystalCount = Mathf.Max(visualCrystalCount, 1000);
                    microSparkleChipCount = Mathf.Max(microSparkleChipCount, 300);
                    break;
                case KaleidoscopeEngine.Mirrors.KaleidoscopeQualityLevel.Low:
                    visualCrystalCount = Mathf.Max(visualCrystalCount, 1800);
                    microSparkleChipCount = Mathf.Max(microSparkleChipCount, 700);
                    break;
                case KaleidoscopeEngine.Mirrors.KaleidoscopeQualityLevel.Medium:
                    visualCrystalCount = Mathf.Max(visualCrystalCount, 2600);
                    microSparkleChipCount = Mathf.Max(microSparkleChipCount, 1200);
                    break;
                case KaleidoscopeEngine.Mirrors.KaleidoscopeQualityLevel.Ultra:
                    visualCrystalCount = Mathf.Max(visualCrystalCount, 5000);
                    microSparkleChipCount = Mathf.Max(microSparkleChipCount, 2500);
                    break;
                case KaleidoscopeEngine.Mirrors.KaleidoscopeQualityLevel.Extreme:
                    visualCrystalCount = Mathf.Max(visualCrystalCount, 6500);
                    microSparkleChipCount = Mathf.Max(microSparkleChipCount, 3000);
                    break;
                default:
                    visualCrystalCount = Mathf.Max(visualCrystalCount, 3500);
                    microSparkleChipCount = Mathf.Max(microSparkleChipCount, 1500);
                    break;
            }

            generated = false;
        }

        private void EnsureGenerated()
        {
            if (generated)
            {
                return;
            }

            EnsureResources();
            renderLayer = ResolveLayer(visualLayerName);
            for (int i = 0; i < batches.Length; i++)
            {
                batches[i].Clear();
            }

            random = new System.Random(31415);
            int total = Mathf.Clamp(visualCrystalCount + microSparkleChipCount, 0, 11000);
            for (int i = 0; i < total; i++)
            {
                float scale = i < visualCrystalCount
                    ? RandomRange(visualCrystalScaleMin, visualCrystalScaleMax)
                    : RandomRange(visualCrystalScaleMin * 0.35f, visualCrystalScaleMin * 0.9f);
                Vector3 local = RandomPilePoint(scale);
                Quaternion rotation = RandomRotation();
                Vector3 stretch = new Vector3(
                    scale * RandomRange(0.55f, 1.8f),
                    scale * RandomRange(0.25f, 1.1f),
                    scale * RandomRange(0.55f, 2.2f));
                int meshIndex = Mathf.Clamp(Mathf.FloorToInt((float)random.NextDouble() * batches.Length), 0, batches.Length - 1);
                batches[meshIndex].Add(Matrix4x4.TRS(transform.TransformPoint(local + pileShakeOffset), transform.rotation * rotation, stretch));
            }

            generated = true;
        }

        private void Draw()
        {
            for (int i = 0; i < batches.Length; i++)
            {
                List<Matrix4x4> matrices = batches[i];
                Mesh mesh = meshes[Mathf.Min(i, meshes.Length - 1)];
                Material material = materials[Mathf.Min(i, materials.Length - 1)];
                for (int start = 0; start < matrices.Count; start += BatchSize)
                {
                    int count = Mathf.Min(BatchSize, matrices.Count - start);
                    matrices.CopyTo(start, drawBuffer, 0, count);
                    Graphics.DrawMeshInstanced(mesh, 0, material, drawBuffer, count, null, ShadowCastingMode.Off, false, renderLayer);
                }
            }
        }

        private void EnsureResources()
        {
            if (meshes == null || meshes.Length == 0)
            {
                meshes = new[]
                {
                    CreateTetrahedron(),
                    CreateOctahedron(),
                    CreatePrism(),
                    CreateFacetedShard(),
                    CreateBeveledTriangle()
                };
            }

            if (materials != null && materials.Length == batches.Length)
            {
                return;
            }

            Color[] palette =
            {
                new Color(0.02f, 0.82f, 0.32f, visualCrystalAlpha),
                new Color(0.86f, 0.02f, 0.08f, visualCrystalAlpha),
                new Color(0.04f, 0.22f, 0.95f, visualCrystalAlpha),
                new Color(1f, 0.48f, 0.04f, visualCrystalAlpha),
                new Color(0.45f, 0.02f, 0.15f, visualCrystalAlpha),
                new Color(1f, 0.06f, 0.02f, visualCrystalAlpha),
                new Color(0.18f, 0.88f, 1f, visualCrystalAlpha),
                new Color(0.95f, 1f, 1f, visualCrystalAlpha * 0.62f)
            };

            materials = new Material[batches.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                Color color = Color.Lerp(palette[i], Color.white, RandomRange(0f, visualCrystalColorVariance * 0.25f));
                Material material = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"))
                {
                    name = $"Runtime Lightweight Crystal Field {i}",
                    enableInstancing = true,
                    renderQueue = (int)RenderQueue.Transparent + 20 + i
                };
                SetColor(material, "_BaseColor", color);
                SetColor(material, "_Color", color);
                SetFloat(material, "_SurfaceType", 1f);
                SetFloat(material, "_BlendMode", 0f);
                SetFloat(material, "_DoubleSidedEnable", 1f);
                SetFloat(material, "_Smoothness", 0.92f);
                SetFloat(material, "_Metallic", 0f);
                SetFloat(material, "_AlphaCutoffEnable", 0f);
                SetFloat(material, "_SpecularOcclusionMode", 1f);
                SetFloat(material, "_ReceivesSSR", 1f);
                materials[i] = material;
            }
        }

        public void ValidateCameraParticipation(Camera sourceCamera, Camera viewerCamera)
        {
            if (!fieldActive)
            {
                lastVisibleToSourceCamera = 0;
                lastRenderedAfterMirrorPass = 0;
                return;
            }

            EnsureGenerated();
            lastVisibleToSourceCamera = CountVisibleToCamera(sourceCamera);
            int viewerLayerMask = viewerCamera != null ? viewerCamera.cullingMask : 0;
            lastRenderedAfterMirrorPass = (viewerLayerMask & (1 << renderLayer)) != 0 ? CountVisibleToCamera(viewerCamera) : 0;
        }

        public void ApplyPileShake(float strength)
        {
            shakeSeed += 11.37f;
            Vector3 impulse = new Vector3(
                Mathf.PerlinNoise(shakeSeed, 0.17f) - 0.5f,
                Mathf.PerlinNoise(0.31f, shakeSeed) - 0.5f,
                Mathf.PerlinNoise(shakeSeed, 0.73f) - 0.5f);
            pileShakeVelocity += impulse * visualPileShakeStrength * Mathf.Max(0f, strength);
            generated = false;
        }

        public void SetVisualLayerName(string layerName)
        {
            if (string.IsNullOrWhiteSpace(layerName))
            {
                return;
            }

            visualLayerName = layerName;
            generated = false;
        }

        private Vector3 RandomLocalPoint(float objectRadius)
        {
            float rear = tubeLength * 0.5f + rearWallOffset;
            if (opticalMixingChamber != null)
            {
                rear = opticalMixingChamber.ChamberCenterOffset.x + opticalMixingChamber.ChamberDepth * 0.5f;
            }
            float safeRadius = Mathf.Max(0.05f, cassetteRadius - objectRadius);
            float angle = RandomRange(0f, Mathf.PI * 2f);
            float radial = Mathf.Pow(Mathf.Clamp01((float)random.NextDouble()), Mathf.Lerp(0.28f, 0.62f, packingDensity / 3f)) * safeRadius;
            float depth = RandomRange(0f, Mathf.Clamp(cassetteDepth, 0.3f, 0.5f));
            return new Vector3(rear - depth - objectRadius * 0.5f, Mathf.Cos(angle) * radial, Mathf.Sin(angle) * radial);
        }

        private Vector3 RandomPilePoint(float objectRadius)
        {
            float rear = tubeLength * 0.5f + rearWallOffset;
            float safeRadius = Mathf.Max(0.05f, pileRadius - objectRadius);
            float angle = RandomRange(0f, Mathf.PI * 2f);
            float radial01 = Mathf.Pow(Mathf.Clamp01((float)random.NextDouble()), Mathf.Lerp(0.35f, 0.68f, packingDensity / 3f));
            float radial = radial01 * safeRadius;
            float layer = pileLayerCount <= 1 ? 0f : Mathf.Floor(RandomRange(0f, pileLayerCount)) / Mathf.Max(1f, pileLayerCount - 1f);
            float rimFalloff = Mathf.Pow(Mathf.Clamp01(1f - radial01), pileSlope);
            float heapHeight = pileHeight * rimFalloff;
            float y = -pileHeight * 0.48f + heapHeight * Mathf.Lerp(layer, (float)random.NextDouble(), 0.45f);
            float depthRoll = Mathf.Lerp(layer, (float)random.NextDouble(), pileRandomDepth);
            float x = rear - depthRoll * Mathf.Clamp(pileDepth, 0.3f, 0.5f) - objectRadius * 0.5f;
            if (pileMixWithHeroGems)
            {
                x += RandomRange(-0.04f, 0.04f);
                radial += RandomRange(-0.04f, 0.04f);
            }

            return new Vector3(
                x + pileCenterOffset.x,
                y + pileCenterOffset.y,
                Mathf.Sin(angle) * radial + pileCenterOffset.z + Mathf.Cos(angle) * radial * 0.12f);
        }

        private void UpdatePileShake()
        {
            if (pileShakeVelocity.sqrMagnitude < 0.000001f && pileShakeOffset.sqrMagnitude < 0.000001f)
            {
                return;
            }

            float dt = Mathf.Max(0.001f, Time.unscaledDeltaTime);
            float noiseX = Mathf.PerlinNoise(Time.unscaledTime * 0.37f, shakeSeed) - 0.5f;
            float noiseZ = Mathf.PerlinNoise(shakeSeed, Time.unscaledTime * 0.41f) - 0.5f;
            Vector3 noise = new Vector3(noiseX, -Mathf.Abs(noiseX) * 0.35f, noiseZ) * visualPileNoise * 0.02f;
            pileShakeOffset = Vector3.Lerp(pileShakeOffset, pileShakeOffset + pileShakeVelocity * dt + noise, Mathf.Clamp01(1f - visualPileInertia));
            pileShakeOffset = Vector3.Lerp(pileShakeOffset, Vector3.zero, 1f - Mathf.Exp(-visualPileSettleSpeed * dt));
            pileShakeVelocity = Vector3.Lerp(pileShakeVelocity, Vector3.zero, 1f - Mathf.Exp(-visualPileSettleSpeed * dt));
            generated = false;
        }

        private Quaternion RandomRotation()
        {
            return Quaternion.Euler(RandomRange(0f, 360f), RandomRange(0f, 360f), RandomRange(0f, 360f));
        }

        private float RandomRange(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private static Mesh CreateTetrahedron()
        {
            Mesh mesh = new Mesh { name = "Lightweight Tetrahedron" };
            mesh.vertices = new[] { new Vector3(1, 1, 1), new Vector3(-1, -1, 1), new Vector3(-1, 1, -1), new Vector3(1, -1, -1) };
            mesh.triangles = new[] { 0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateOctahedron()
        {
            Mesh mesh = new Mesh { name = "Lightweight Octahedron" };
            mesh.vertices = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            mesh.triangles = new[] { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreatePrism()
        {
            Mesh mesh = new Mesh { name = "Lightweight Low Poly Prism" };
            mesh.vertices = new[] { new Vector3(-1, -0.6f, -0.5f), new Vector3(1, -0.6f, -0.5f), new Vector3(0, 0.9f, -0.5f), new Vector3(-1, -0.6f, 0.5f), new Vector3(1, -0.6f, 0.5f), new Vector3(0, 0.9f, 0.5f) };
            mesh.triangles = new[] { 0, 2, 1, 3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateFacetedShard()
        {
            Mesh mesh = new Mesh { name = "Lightweight Faceted Glass Shard" };
            mesh.vertices = new[]
            {
                new Vector3(-1f, -0.08f, -0.28f),
                new Vector3(0.8f, -0.08f, -0.46f),
                new Vector3(1.15f, -0.08f, 0.24f),
                new Vector3(-0.45f, -0.08f, 0.58f),
                new Vector3(-0.72f, 0.1f, -0.18f),
                new Vector3(0.56f, 0.14f, -0.3f),
                new Vector3(0.82f, 0.12f, 0.18f),
                new Vector3(-0.32f, 0.1f, 0.42f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 6, 5, 4, 7, 6,
                0, 4, 5, 0, 5, 1,
                1, 5, 6, 1, 6, 2,
                2, 6, 7, 2, 7, 3,
                3, 7, 4, 3, 4, 0
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateBeveledTriangle()
        {
            Mesh mesh = new Mesh { name = "Lightweight Beveled Triangle" };
            mesh.vertices = new[] { new Vector3(-0.8f, -0.5f, -0.08f), new Vector3(0.8f, -0.5f, -0.08f), new Vector3(0f, 0.8f, -0.08f), new Vector3(-0.62f, -0.36f, 0.08f), new Vector3(0.62f, -0.36f, 0.08f), new Vector3(0f, 0.62f, 0.08f) };
            mesh.triangles = new[] { 0, 2, 1, 3, 4, 5, 0, 1, 4, 0, 4, 3, 1, 2, 5, 1, 5, 4, 2, 0, 3, 2, 3, 5 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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

        private int CountVisibleToCamera(Camera camera)
        {
            if (camera == null || !fieldActive || (camera.cullingMask & (1 << renderLayer)) == 0)
            {
                return 0;
            }

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
            int visible = 0;
            Vector3 boundsSize = Vector3.one * Mathf.Max(visualCrystalScaleMax * 2f, 0.05f);
            for (int batch = 0; batch < batches.Length; batch++)
            {
                List<Matrix4x4> matrices = batches[batch];
                for (int i = 0; i < matrices.Count; i++)
                {
                    Bounds bounds = new Bounds(matrices[i].GetColumn(3), boundsSize);
                    if (GeometryUtility.TestPlanesAABB(planes, bounds))
                    {
                        visible++;
                    }
                }
            }

            return visible;
        }
    }
}
