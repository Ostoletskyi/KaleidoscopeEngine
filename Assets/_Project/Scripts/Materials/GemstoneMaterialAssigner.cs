using System.Collections.Generic;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;

namespace KaleidoscopeEngine.Materials
{
    [DisallowMultipleComponent]
    public sealed class GemstoneMaterialAssigner : MonoBehaviour
    {
        [Header("Profiles")]
        [SerializeField] private List<GemstoneMaterialProfile> profiles = new List<GemstoneMaterialProfile>();
        [SerializeField] private bool useOpticalMaterials = true;

        [Header("Per Object Variation")]
        [SerializeField] private bool randomizePerObject = true;
        [Range(0f, 0.4f)] public float colorVariation = 0.08f;
        [Range(0f, 0.25f)] public float smoothnessVariation = 0.06f;
        [Range(0f, 0.6f)] public float emissionVariation = 0.15f;
        [Range(0f, 0.8f)] public float sparkleVariation = 0.2f;

        private readonly Dictionary<string, GemstoneMaterialProfile> profileById = new Dictionary<string, GemstoneMaterialProfile>();
        private readonly Dictionary<Renderer, Material> opticalMaterials = new Dictionary<Renderer, Material>();
        private readonly Dictionary<Renderer, Material> debugMaterials = new Dictionary<Renderer, Material>();
        private System.Random random = new System.Random(2241);

        public string MaterialMode => useOpticalMaterials ? "Gemstone Optical" : "Placeholder";
        public int ProfileCount => profiles.Count;

        public void SetProfiles(IEnumerable<GemstoneMaterialProfile> gemstoneProfiles)
        {
            profiles.Clear();
            profiles.AddRange(gemstoneProfiles);
            RebuildLookup();
        }

        private void Awake()
        {
            RebuildLookup();
        }

        public void ApplyTo(GameObject gemstoneObject)
        {
            if (gemstoneObject == null)
            {
                return;
            }

            GemstonePhysicsSetup setup = gemstoneObject.GetComponent<GemstonePhysicsSetup>();
            GemstoneDefinition definition = setup != null ? setup.Definition : null;
            if (definition == null)
            {
                return;
            }

            GemstoneMaterialProfile profile = FindProfile(definition.id);
            if (profile == null)
            {
                return;
            }

            foreach (Renderer renderer in gemstoneObject.GetComponentsInChildren<Renderer>(true))
            {
                if (!debugMaterials.ContainsKey(renderer))
                {
                    debugMaterials[renderer] = renderer.sharedMaterial;
                }

                Material optical = CreateMaterialInstance(profile, renderer.name);
                opticalMaterials[renderer] = optical;
                renderer.sharedMaterial = useOpticalMaterials ? optical : debugMaterials[renderer];

                GemstoneOpticalAnimator animator = renderer.GetComponentInParent<GemstoneOpticalAnimator>();
                if (animator == null)
                {
                    animator = renderer.gameObject.AddComponent<GemstoneOpticalAnimator>();
                }

                animator.Configure(renderer, profile, random);
                animator.enabled = useOpticalMaterials;
            }
        }

        public void Release(GameObject gemstoneObject)
        {
            if (gemstoneObject == null)
            {
                return;
            }

            foreach (Renderer renderer in gemstoneObject.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                if (opticalMaterials.TryGetValue(renderer, out Material opticalMaterial) && opticalMaterial != null)
                {
                    Destroy(opticalMaterial);
                }

                opticalMaterials.Remove(renderer);
                debugMaterials.Remove(renderer);
            }
        }

        public void ToggleMaterialMode()
        {
            useOpticalMaterials = !useOpticalMaterials;
            ApplyModeToKnownRenderers();
        }

        public void SetOpticalMaterialsEnabled(bool enabled)
        {
            useOpticalMaterials = enabled;
            ApplyModeToKnownRenderers();
        }

        private void ApplyModeToKnownRenderers()
        {
            foreach (KeyValuePair<Renderer, Material> pair in opticalMaterials)
            {
                Renderer renderer = pair.Key;
                if (renderer == null)
                {
                    continue;
                }

                renderer.sharedMaterial = useOpticalMaterials
                    ? pair.Value
                    : FindDebugMaterial(renderer, pair.Value);

                GemstoneOpticalAnimator animator = renderer.GetComponent<GemstoneOpticalAnimator>();
                if (animator != null)
                {
                    animator.enabled = useOpticalMaterials;
                }

                if (!useOpticalMaterials)
                {
                    renderer.SetPropertyBlock(null);
                }
            }
        }

        private Material FindDebugMaterial(Renderer renderer, Material fallback)
        {
            return debugMaterials.TryGetValue(renderer, out Material debugMaterial) ? debugMaterial : fallback;
        }

        private GemstoneMaterialProfile FindProfile(string id)
        {
            if (profileById.Count != profiles.Count)
            {
                RebuildLookup();
            }

            return id != null && profileById.TryGetValue(id, out GemstoneMaterialProfile profile) ? profile : null;
        }

        private void RebuildLookup()
        {
            profileById.Clear();
            foreach (GemstoneMaterialProfile profile in profiles)
            {
                if (profile != null && !string.IsNullOrWhiteSpace(profile.id))
                {
                    profileById[profile.id] = profile;
                }
            }
        }

        private Material CreateMaterialInstance(GemstoneMaterialProfile profile, string rendererName)
        {
            Material material = profile.materialTemplate != null
                ? new Material(profile.materialTemplate)
                : new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"));

            material.name = $"{profile.displayName} Optical ({rendererName})";
            ApplyProfile(material, profile);
            return material;
        }

        private void ApplyProfile(Material material, GemstoneMaterialProfile profile)
        {
            Color baseColor = profile.baseColor;
            baseColor.a = Mathf.Clamp01(1f - profile.transparency);
            if (randomizePerObject)
            {
                baseColor = VaryColor(baseColor, colorVariation);
            }

            float smoothness = profile.smoothness + RandomRange(-smoothnessVariation, smoothnessVariation);
            float emissionStrength = profile.emissionStrength + RandomRange(-emissionVariation, emissionVariation);

            SetColor(material, "_BaseColor", baseColor);
            SetColor(material, "_Color", baseColor);
            SetFloat(material, "_SurfaceType", profile.transparency > 0.02f ? 1f : 0f);
            SetFloat(material, "_BlendMode", 0f);
            SetFloat(material, "_AlphaCutoffEnable", 0f);
            SetFloat(material, "_Smoothness", Mathf.Clamp01(smoothness));
            SetFloat(material, "_Metallic", profile.metallic);
            SetFloat(material, "_SpecularAAScreenSpaceVariance", Mathf.Clamp01(profile.specularIntensity * 0.1f));
            SetColor(material, "_EmissiveColor", profile.useEmission ? profile.emissionColor * Mathf.Max(0f, emissionStrength) : Color.black);
            SetFloat(material, "_EmissiveIntensity", profile.useEmission ? Mathf.Max(0f, emissionStrength) : 0f);
            SetColor(material, "_TransmittanceColor", Color.Lerp(profile.baseColor, profile.absorptionColor, profile.absorptionStrength * 0.35f));
            SetFloat(material, "_RefractionModel", 2f);
            SetFloat(material, "_Ior", profile.fakeIOR);

            if (profile.transparency > 0.02f)
            {
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + profile.renderQueueOffset;
            }
            else
            {
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry + profile.renderQueueOffset;
            }

            material.globalIlluminationFlags = profile.useEmission
                ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : MaterialGlobalIlluminationFlags.None;
        }

        private Color VaryColor(Color color, float amount)
        {
            float hue;
            float saturation;
            float value;
            Color.RGBToHSV(color, out hue, out saturation, out value);
            hue = Mathf.Repeat(hue + RandomRange(-amount, amount) * 0.08f, 1f);
            saturation = Mathf.Clamp01(saturation + RandomRange(-amount, amount));
            value = Mathf.Clamp01(value + RandomRange(-amount, amount));
            Color varied = Color.HSVToRGB(hue, saturation, value);
            varied.a = color.a;
            return varied;
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
    }
}
