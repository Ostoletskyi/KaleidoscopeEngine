using System.Collections.Generic;
using KaleidoscopeEngine.Lighting;
using UnityEngine;
using UnityEngine.Rendering;

namespace KaleidoscopeEngine.FX
{
    [DisallowMultipleComponent]
    public sealed class FakeCausticBunnyProjector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform tube;
        [SerializeField] private KaleidoscopeLightingRig lightingRig;
        [SerializeField] private Camera mainCamera;

        [Header("Fake Caustics")]
        [SerializeField] private bool causticsEnabled = true;
        [SerializeField] private float intensity = 0.42f;
        [SerializeField] private int spotCount = 18;
        [SerializeField] private float spotSizeMin = 0.08f;
        [SerializeField] private float spotSizeMax = 0.22f;
        [SerializeField] private float movementSpeed = 0.18f;
        [SerializeField] private float flickerAmount = 0.12f;
        [SerializeField] private Color colorTint = new Color(0.75f, 0.95f, 1f, 1f);
        [SerializeField] private float fadeDistance = 5.5f;
        [SerializeField] private bool attachToTubeInterior = true;
        [SerializeField] private bool reactToAxialRotation = true;
        [SerializeField] private float tubeRadius = 1.18f;
        [SerializeField] private float tubeLength = 5.1f;
        [SerializeField] private string opticalFxLayerName = "KaleidoscopeOpticalFX";

        [Header("Temporal Stability")]
        [SerializeField, Range(1f, 30f)] private float causticUpdateRate = 10f;
        [SerializeField, Range(0f, 1f)] private float opticalInertia = 0.74f;
        [SerializeField, Range(0f, 1f)] private float lowFrequencyShimmer = 0.68f;

        private readonly List<CausticSpot> spots = new List<CausticSpot>();
        private Material spotMaterial;
        private System.Random random = new System.Random(7351);
        private int requestedSpotCount = -1;
        private int effectiveSpotCount = -1;
        private float nextCausticUpdateTime;

        public bool CausticsEnabled => causticsEnabled;
        public int SpotCount => EffectiveSpotCount;
        public int RequestedSpotCount => requestedSpotCount >= 0 ? requestedSpotCount : spotCount;
        public int EffectiveSpotCount => effectiveSpotCount >= 0 ? Mathf.Min(effectiveSpotCount, RequestedSpotCount) : spotCount;

        public void Configure(Transform tubeTransform, KaleidoscopeLightingRig rig, Camera camera, float radius, float length)
        {
            tube = tubeTransform;
            lightingRig = rig;
            mainCamera = camera != null ? camera : Camera.main;
            tubeRadius = Mathf.Max(0.1f, radius);
            tubeLength = Mathf.Max(0.1f, length);
            BuildSpots();
        }

        public void ToggleCaustics()
        {
            causticsEnabled = !causticsEnabled;
            ApplyVisibility();
        }

        public void SetAdaptiveSpotLimit(int maxSpots)
        {
            requestedSpotCount = RequestedSpotCount;
            effectiveSpotCount = Mathf.Clamp(maxSpots, 0, Mathf.Max(0, RequestedSpotCount));
            ApplyVisibility();
        }

        public void ClearAdaptiveSpotLimit()
        {
            effectiveSpotCount = -1;
            ApplyVisibility();
        }

        public void SetOpticalFxLayerName(string layerName)
        {
            if (!string.IsNullOrWhiteSpace(layerName))
            {
                opticalFxLayerName = layerName;
            }
        }

        private void Awake()
        {
            mainCamera = mainCamera != null ? mainCamera : Camera.main;
            BuildSpots();
        }

        private void LateUpdate()
        {
            if (!causticsEnabled)
            {
                return;
            }

            if (Time.unscaledTime < nextCausticUpdateTime)
            {
                return;
            }

            nextCausticUpdateTime = Time.unscaledTime + 1f / Mathf.Max(1f, causticUpdateRate);

            mainCamera = mainCamera != null ? mainCamera : Camera.main;
            EnsureSpotCount();

            float lightBoost = lightingRig != null && lightingRig.MovingLightEnabled ? 1f : 0.55f;
            for (int i = 0; i < spots.Count; i++)
            {
                CausticSpot spot = spots[i];
                if (spot.transform == null)
                {
                    continue;
                }

                float time = Time.time * movementSpeed + spot.phase;
                float x = Mathf.Lerp(-tubeLength * 0.46f, tubeLength * 0.46f, Mathf.Repeat(spot.x01 + Mathf.Sin(time * 0.37f) * 0.08f, 1f));
                float angle = spot.angle + Mathf.Sin(time * 0.83f) * 0.38f;
                if (reactToAxialRotation && tube != null)
                {
                    angle += tube.localEulerAngles.x * Mathf.Deg2Rad * 0.22f;
                }

                Vector3 radial = new Vector3(0f, Mathf.Cos(angle), Mathf.Sin(angle));
                Vector3 localPosition = new Vector3(x, radial.y * tubeRadius, radial.z * tubeRadius);
                Quaternion localRotation = Quaternion.LookRotation(-radial, Vector3.right);

                float flicker = 1f + Mathf.Sin(time * Mathf.Lerp(1.2f, 0.35f, lowFrequencyShimmer) + spot.phase) * flickerAmount;
                float distanceFade = EstimateDistanceFade(spot.transform.position);
                float alpha = Mathf.Clamp01(intensity * spot.alpha * flicker * lightBoost * distanceFade);
                float size = Mathf.Lerp(spotSizeMin, spotSizeMax, spot.size01) * (0.85f + flicker * 0.12f);
                spot.transform.localPosition = Vector3.Lerp(spot.transform.localPosition, localPosition, 1f - opticalInertia * 0.82f);
                spot.transform.localRotation = Quaternion.Slerp(spot.transform.localRotation, localRotation, 1f - opticalInertia * 0.82f);
                spot.transform.localScale = Vector3.Lerp(spot.transform.localScale, new Vector3(size, size, size), 1f - opticalInertia * 0.72f);

                spot.propertyBlock ??= new MaterialPropertyBlock();
                Color color = colorTint * Mathf.Lerp(0.55f, 1.55f, spot.alpha);
                color.a = alpha;
                spot.propertyBlock.SetColor("_UnlitColor", color);
                spot.propertyBlock.SetColor("_BaseColor", color);
                spot.renderer.SetPropertyBlock(spot.propertyBlock);
            }
        }

        private void BuildSpots()
        {
            EnsureMaterial();
            EnsureSpotCount();
            ApplyVisibility();
        }

        private void EnsureSpotCount()
        {
            EnsureMaterial();
            requestedSpotCount = Mathf.Max(RequestedSpotCount, spotCount);
            int targetCount = Mathf.Max(RequestedSpotCount, EffectiveSpotCount);
            while (spots.Count < targetCount)
            {
                GameObject spotObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                spotObject.name = $"Fake Caustic Bunny_{spots.Count:00}";
                spotObject.layer = ResolveLayer(opticalFxLayerName);
                spotObject.transform.SetParent(attachToTubeInterior && tube != null ? tube : transform, false);

                Collider collider = spotObject.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = spotObject.GetComponent<Renderer>();
                renderer.sharedMaterial = spotMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                spots.Add(new CausticSpot
                {
                    transform = spotObject.transform,
                    renderer = renderer,
                    phase = RandomRange(0f, 100f),
                    angle = RandomRange(0f, Mathf.PI * 2f),
                    x01 = RandomRange(0f, 1f),
                    size01 = RandomRange(0f, 1f),
                    alpha = RandomRange(0.35f, 1f)
                });
            }

            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].transform != null)
                {
                    spots[i].transform.gameObject.SetActive(causticsEnabled && i < EffectiveSpotCount);
                }
            }
        }

        private void EnsureMaterial()
        {
            if (spotMaterial != null)
            {
                return;
            }

            spotMaterial = new Material(Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Transparent"))
            {
                name = "Runtime Fake Caustic Bunny Material"
            };
            SetColor(spotMaterial, "_UnlitColor", colorTint);
            SetColor(spotMaterial, "_BaseColor", colorTint);
            SetFloat(spotMaterial, "_SurfaceType", 1f);
            SetFloat(spotMaterial, "_BlendMode", 0f);
            SetFloat(spotMaterial, "_AlphaCutoffEnable", 0f);
            SetFloat(spotMaterial, "_DoubleSidedEnable", 1f);
            spotMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 70;
        }

        private void ApplyVisibility()
        {
            for (int i = 0; i < spots.Count; i++)
            {
                if (spots[i].transform != null)
                {
                    spots[i].transform.gameObject.SetActive(causticsEnabled && i < EffectiveSpotCount);
                }
            }
        }

        private float EstimateDistanceFade(Vector3 worldPosition)
        {
            if (mainCamera == null)
            {
                return 1f;
            }

            float distance = Vector3.Distance(mainCamera.transform.position, worldPosition);
            return Mathf.Clamp01(1f - distance / Mathf.Max(0.1f, fadeDistance));
        }

        private float RandomRange(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }

        private static void SetColor(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static int ResolveLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : 0;
        }

        private sealed class CausticSpot
        {
            public Transform transform;
            public Renderer renderer;
            public MaterialPropertyBlock propertyBlock;
            public float phase;
            public float angle;
            public float x01;
            public float size01;
            public float alpha;
        }
    }
}
