using System.Collections.Generic;
using KaleidoscopeEngine.Lighting;
using KaleidoscopeEngine.Materials;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;
using UnityEngine.Rendering;

namespace KaleidoscopeEngine.FX
{
    [DisallowMultipleComponent]
    public sealed class GemSparkleController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private KaleidoscopeLightingRig lightingRig;
        [SerializeField] private Camera mainCamera;

        [Header("Sparkle")]
        [SerializeField] private bool sparkleEnabled = true;
        [SerializeField] private float sparkleIntensity = 0.85f;
        [SerializeField] private float sparkleThreshold = 0.62f;
        [SerializeField] private float sparkleSize = 0.055f;
        [SerializeField] private float sparkleLifetime = 0.42f;
        [SerializeField] private float sparkleFrequency = 26f;
        [SerializeField] private Color sparkleColor = new Color(0.86f, 0.96f, 1f, 1f);
        [SerializeField] private float sparkleRandomness = 0.18f;
        [SerializeField] private float cameraAngleInfluence = 0.65f;
        [SerializeField] private float lightAngleInfluence = 0.8f;
        [SerializeField] private int maxActiveSparkles = 36;
        [SerializeField] private string opticalFxLayerName = "KaleidoscopeOpticalFX";

        [Header("Temporal Stability")]
        [SerializeField, Range(1f, 30f)] private float sparkleUpdateRate = 15f;
        [SerializeField, Range(1f, 30f)] private float rendererRefreshRate = 8f;
        [SerializeField, Range(0f, 1f)] private float sparkleClamp = 0.68f;
        [SerializeField, Range(0f, 1f)] private float subpixelStability = 0.72f;
        [SerializeField, Range(0f, 1f)] private float highlightPersistence = 0.78f;

        private readonly List<Sparkle> sparkles = new List<Sparkle>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private Material sparkleMaterial;
        private float spawnAccumulator;
        private float nextSparkleUpdateTime;
        private float nextRendererRefreshTime;
        private System.Random random = new System.Random(9137);
        private int requestedMaxActiveSparkles = -1;
        private float requestedSparkleFrequency = -1f;

        public bool SparkleEnabled => sparkleEnabled;
        public float SparkleIntensity => sparkleIntensity;
        public int RequestedMaxActiveSparkles => requestedMaxActiveSparkles >= 0 ? requestedMaxActiveSparkles : maxActiveSparkles;
        public int EffectiveMaxActiveSparkles => maxActiveSparkles;
        public float RequestedSparkleFrequency => requestedSparkleFrequency >= 0f ? requestedSparkleFrequency : sparkleFrequency;
        public float EffectiveSparkleFrequency => sparkleFrequency;
        public int ActiveSparkles { get; private set; }

        public void Configure(GemstoneSpawner gemstoneSpawner, KaleidoscopeLightingRig rig, Camera camera)
        {
            spawner = gemstoneSpawner;
            lightingRig = rig;
            mainCamera = camera != null ? camera : Camera.main;
            EnsurePool();
        }

        public void ToggleSparkles()
        {
            sparkleEnabled = !sparkleEnabled;
            if (!sparkleEnabled)
            {
                HideAll();
            }
        }

        public void AdjustSparkleIntensity(float delta)
        {
            sparkleIntensity = Mathf.Clamp(sparkleIntensity + delta, 0f, 3f);
        }

        public void ApplyQualityProfile(KaleidoscopeQualityProfile profile)
        {
            requestedMaxActiveSparkles = Mathf.Clamp(profile.sparkleCount, 8, 160);
            requestedSparkleFrequency = Mathf.Clamp(profile.sparkleFrequency, 4f, 100f);
            maxActiveSparkles = requestedMaxActiveSparkles;
            sparkleFrequency = requestedSparkleFrequency;
            sparkleIntensity = Mathf.Clamp(profile.sparkleIntensity, 0f, 3f);
            EnsurePool();
            HideSparklesBeyondQualityLimit();
        }

        public void ApplyVisibleSparkleTarget(int target)
        {
            int safeTarget = Mathf.Clamp(target, 0, 160);
            requestedMaxActiveSparkles = Mathf.Max(RequestedMaxActiveSparkles, safeTarget);
            requestedSparkleFrequency = Mathf.Max(RequestedSparkleFrequency, safeTarget * 0.55f);
            maxActiveSparkles = Mathf.Max(maxActiveSparkles, safeTarget);
            sparkleFrequency = Mathf.Max(sparkleFrequency, safeTarget * 0.55f);
            EnsurePool();
        }

        public void SetAdaptiveSparkleLimit(int maxSparkles, float frequencyMultiplier)
        {
            maxActiveSparkles = Mathf.Clamp(maxSparkles, 0, Mathf.Max(0, RequestedMaxActiveSparkles));
            sparkleFrequency = Mathf.Clamp(RequestedSparkleFrequency * Mathf.Clamp01(frequencyMultiplier), 0f, RequestedSparkleFrequency);
            EnsurePool();
            HideSparklesBeyondQualityLimit();
        }

        public void ClearAdaptiveSparkleLimit()
        {
            maxActiveSparkles = RequestedMaxActiveSparkles;
            sparkleFrequency = RequestedSparkleFrequency;
            EnsurePool();
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
            EnsurePool();
        }

        private void LateUpdate()
        {
            if (!sparkleEnabled || spawner == null)
            {
                ActiveSparkles = 0;
                return;
            }

            mainCamera = mainCamera != null ? mainCamera : Camera.main;
            if (Time.unscaledTime >= nextRendererRefreshTime)
            {
                RefreshRenderers();
                nextRendererRefreshTime = Time.unscaledTime + 1f / Mathf.Max(1f, rendererRefreshRate);
            }

            if (Time.unscaledTime >= nextSparkleUpdateTime)
            {
                UpdateSparkles();
                nextSparkleUpdateTime = Time.unscaledTime + 1f / Mathf.Max(1f, sparkleUpdateRate);
            }

            float lightFactor = lightingRig != null && lightingRig.MovingLightEnabled ? 1.25f : 0.75f;
            spawnAccumulator += Time.deltaTime * sparkleFrequency * sparkleIntensity * lightFactor * Mathf.Lerp(1f, 0.38f, subpixelStability);
            while (spawnAccumulator >= 1f)
            {
                spawnAccumulator -= 1f;
                TrySpawnSparkle();
            }
        }

        private void EnsurePool()
        {
            if (sparkleMaterial == null)
            {
                sparkleMaterial = new Material(Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Transparent"))
                {
                    name = "Runtime Gem Sparkle Material"
                };
                SetColor(sparkleMaterial, "_UnlitColor", sparkleColor);
                SetColor(sparkleMaterial, "_BaseColor", sparkleColor);
                SetFloat(sparkleMaterial, "_SurfaceType", 1f);
                SetFloat(sparkleMaterial, "_BlendMode", 0f);
                SetFloat(sparkleMaterial, "_AlphaCutoffEnable", 0f);
                SetFloat(sparkleMaterial, "_DoubleSidedEnable", 1f);
                sparkleMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 80;
            }

            while (sparkles.Count < maxActiveSparkles)
            {
                GameObject sparkleObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                sparkleObject.name = $"Gem Sparkle_{sparkles.Count:00}";
                sparkleObject.layer = ResolveLayer(opticalFxLayerName);
                sparkleObject.transform.SetParent(transform, false);
                sparkleObject.SetActive(false);

                Collider collider = sparkleObject.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = sparkleObject.GetComponent<Renderer>();
                renderer.sharedMaterial = sparkleMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                sparkles.Add(new Sparkle
                {
                    transform = sparkleObject.transform,
                    renderer = renderer,
                    endTime = -1f
                });
            }
        }

        private void RefreshRenderers()
        {
            renderers.Clear();
            IReadOnlyList<GameObject> spawned = spawner.SpawnedObjects;
            for (int i = 0; i < spawned.Count; i++)
            {
                GameObject gemstone = spawned[i];
                if (gemstone == null || !gemstone.activeInHierarchy)
                {
                    continue;
                }

                Renderer renderer = gemstone.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.enabled)
                {
                    renderers.Add(renderer);
                }
            }
        }

        private void UpdateSparkles()
        {
            ActiveSparkles = 0;
            for (int i = 0; i < sparkles.Count; i++)
            {
                Sparkle sparkle = sparkles[i];
                if (sparkle.transform == null || !sparkle.transform.gameObject.activeSelf)
                {
                    continue;
                }

                if (i >= maxActiveSparkles)
                {
                    sparkle.transform.gameObject.SetActive(false);
                    continue;
                }

                float remaining = sparkle.endTime - Time.time;
                if (remaining <= 0f)
                {
                    sparkle.transform.gameObject.SetActive(false);
                    continue;
                }

                ActiveSparkles++;
                float fade = Mathf.Clamp01(remaining / Mathf.Max(0.01f, sparkleLifetime));
                float pulse = Mathf.Sin(fade * Mathf.PI);
                float size = sparkle.baseSize * (0.82f + pulse * 0.28f);
                Vector3 targetScale = new Vector3(size, size, size);
                sparkle.transform.localScale = Vector3.Lerp(sparkle.transform.localScale, targetScale, 1f - highlightPersistence * 0.72f);
                if (mainCamera != null)
                {
                    sparkle.transform.LookAt(mainCamera.transform.position, Vector3.up);
                }
            }
        }

        private void TrySpawnSparkle()
        {
            if (renderers.Count == 0)
            {
                return;
            }

            Renderer target = renderers[random.Next(renderers.Count)];
            if (target == null)
            {
                return;
            }

            GemstoneMaterialProfile profile = FindProfile(target);
            float profileResponse = profile != null ? profile.sparkleResponse : 0.5f;
            float angleScore = EstimateAngleScore(target);
            float randomness = Mathf.Lerp(1f, (float)random.NextDouble(), sparkleRandomness);
            float score = Mathf.Min(angleScore * profileResponse * sparkleIntensity * randomness, Mathf.Lerp(1.6f, 0.8f, sparkleClamp));
            if (score < sparkleThreshold)
            {
                return;
            }

            Sparkle sparkle = FindFreeSparkle();
            if (sparkle == null || sparkle.transform == null)
            {
                return;
            }

            Bounds bounds = target.bounds;
            Vector3 randomOffset = new Vector3(
                RandomRange(-0.45f, 0.45f),
                RandomRange(-0.45f, 0.45f),
                RandomRange(-0.45f, 0.45f));
            Vector3 position = bounds.center + Vector3.Scale(randomOffset, bounds.extents);

            float size = sparkleSize * Mathf.Lerp(0.65f, 1.8f, Mathf.Clamp01(profileResponse * 0.45f));
            sparkle.transform.position = position;
            sparkle.transform.localScale = Vector3.one * size;
            sparkle.baseSize = size;
            sparkle.endTime = Time.time + sparkleLifetime * RandomRange(0.88f, 1.22f);
            sparkle.transform.gameObject.SetActive(true);

            Color color = profile != null
                ? Color.Lerp(sparkleColor, profile.internalScatterColor, Mathf.Clamp01(profile.fakeDispersion + profile.edgeGlowStrength))
                : sparkleColor;
            sparkle.propertyBlock ??= new MaterialPropertyBlock();
            sparkle.renderer.GetPropertyBlock(sparkle.propertyBlock);
            sparkle.propertyBlock.SetColor("_UnlitColor", color * Mathf.Lerp(1.2f, 2.1f, Mathf.Clamp01(score)));
            sparkle.propertyBlock.SetColor("_BaseColor", color * Mathf.Lerp(1.2f, 2.1f, Mathf.Clamp01(score)));
            sparkle.renderer.SetPropertyBlock(sparkle.propertyBlock);
        }

        private Sparkle FindFreeSparkle()
        {
            EnsurePool();
            int usableSparkles = Mathf.Min(sparkles.Count, maxActiveSparkles);
            for (int i = 0; i < usableSparkles; i++)
            {
                if (sparkles[i].transform != null && !sparkles[i].transform.gameObject.activeSelf)
                {
                    return sparkles[i];
                }
            }

            return null;
        }

        private float EstimateAngleScore(Renderer target)
        {
            Vector3 viewScoreDirection = mainCamera != null
                ? (mainCamera.transform.position - target.bounds.center).normalized
                : Vector3.back;
            Vector3 lightDirection = Vector3.up;
            Light[] lights = FindObjectsOfType<Light>();
            float strongest = 0f;
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null || !light.enabled)
                {
                    continue;
                }

                float intensity = light.intensity;
                if (intensity > strongest)
                {
                    strongest = intensity;
                    lightDirection = light.type == LightType.Directional
                        ? -light.transform.forward
                        : (light.transform.position - target.bounds.center).normalized;
                }
            }

            Vector3 normalGuess = target.transform.up;
            float cameraScore = Mathf.Clamp01(Vector3.Dot(normalGuess, viewScoreDirection) * 0.5f + 0.5f);
            float lightScore = Mathf.Clamp01(Vector3.Dot(normalGuess, lightDirection) * 0.5f + 0.5f);
            return cameraScore * cameraAngleInfluence + lightScore * lightAngleInfluence;
        }

        private GemstoneMaterialProfile FindProfile(Renderer target)
        {
            GemstoneOpticalAnimator animator = target.GetComponentInParent<GemstoneOpticalAnimator>();
            return animator != null ? animator.Profile : null;
        }

        private void HideAll()
        {
            for (int i = 0; i < sparkles.Count; i++)
            {
                if (sparkles[i].transform != null)
                {
                    sparkles[i].transform.gameObject.SetActive(false);
                }
            }

            ActiveSparkles = 0;
        }

        private void HideSparklesBeyondQualityLimit()
        {
            for (int i = maxActiveSparkles; i < sparkles.Count; i++)
            {
                if (sparkles[i].transform != null)
                {
                    sparkles[i].transform.gameObject.SetActive(false);
                }
            }
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

        private sealed class Sparkle
        {
            public Transform transform;
            public Renderer renderer;
            public MaterialPropertyBlock propertyBlock;
            public float endTime;
            public float baseSize;
        }
    }
}
