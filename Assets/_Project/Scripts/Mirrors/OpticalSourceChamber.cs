using UnityEngine;
using UnityEngine.Rendering;

namespace KaleidoscopeEngine.Mirrors
{
    [DisallowMultipleComponent]
    public sealed class OpticalSourceChamber : MonoBehaviour
    {
        [Header("Diffuser")]
        [SerializeField] private bool diffuserEnabled = true;
        [SerializeField, Range(0f, 8f)] private float diffuserBrightness = 1.85f;
        [SerializeField] private Color diffuserColor = new Color(0.92f, 0.97f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float diffuserOpacity = 0.34f;
        [SerializeField] private float diffuserDistance = 0.34f;

        [Header("Backlight")]
        [SerializeField] private bool backlightEnabled = true;
        [SerializeField, Range(0f, 20f)] private float backlightIntensity = 5.5f;
        [SerializeField] private float backlightRange = 5.2f;

        private Transform chamber;
        private float tubeRadius = 1f;
        private float tubeLength = 5f;
        private Renderer diffuserRenderer;
        private Light backlight;
        private Material diffuserMaterial;
        private int opticalFxLayer;

        public bool DiffuserEnabled => diffuserEnabled;
        public float DiffuserBrightness => diffuserBrightness;
        public float DiffuserOpacity => diffuserOpacity;
        public float BacklightIntensity => backlightIntensity;
        public Vector3 SourceCameraLocalPosition => new Vector3(-tubeLength * 0.22f, 0f, 0f);
        public Vector3 SourceLookTargetLocalPosition => new Vector3(tubeLength * 0.31f, 0f, 0f);

        public void Configure(Transform chamberTransform, float radius, float length, string opticalFxLayerName)
        {
            chamber = chamberTransform;
            tubeRadius = Mathf.Max(0.1f, radius);
            tubeLength = Mathf.Max(0.5f, length);
            opticalFxLayer = ResolveLayer(opticalFxLayerName);
            BuildObjects();
            ApplyState();
        }

        public void ToggleDiffuserModule()
        {
            diffuserEnabled = !diffuserEnabled;
            backlightEnabled = diffuserEnabled;
            ApplyState();
        }

        private void LateUpdate()
        {
            ApplyState();
        }

        private void BuildObjects()
        {
            if (diffuserRenderer == null)
            {
                GameObject diffuser = GameObject.CreatePrimitive(PrimitiveType.Quad);
                diffuser.name = "Object Chamber Diffuser";
                diffuser.layer = opticalFxLayer;
                diffuser.transform.SetParent(transform, false);
                diffuser.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                diffuser.transform.localScale = Vector3.one * tubeRadius * 2.28f;

                Collider collider = diffuser.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                diffuserRenderer = diffuser.GetComponent<Renderer>();
                diffuserRenderer.shadowCastingMode = ShadowCastingMode.Off;
                diffuserRenderer.receiveShadows = false;
            }

            if (diffuserMaterial == null)
            {
                diffuserMaterial = new Material(Shader.Find("HDRP/Unlit") ?? Shader.Find("Unlit/Transparent"))
                {
                    name = "Runtime Object Chamber Diffuser"
                };
                diffuserMaterial.renderQueue = (int)RenderQueue.Transparent + 40;
            }

            diffuserRenderer.sharedMaterial = diffuserMaterial;

            if (backlight == null)
            {
                GameObject lightObject = new GameObject("Object Chamber Backlight");
                lightObject.layer = opticalFxLayer;
                lightObject.transform.SetParent(transform, false);
                backlight = lightObject.AddComponent<Light>();
                backlight.type = LightType.Point;
                backlight.color = diffuserColor;
                backlight.shadows = LightShadows.None;
            }
        }

        private void ApplyState()
        {
            if (chamber != null)
            {
                transform.SetPositionAndRotation(chamber.position, chamber.rotation);
            }

            Vector3 diffuserLocal = new Vector3(tubeLength * 0.5f + diffuserDistance, 0f, 0f);
            if (diffuserRenderer != null)
            {
                diffuserRenderer.transform.localPosition = diffuserLocal;
                diffuserRenderer.gameObject.SetActive(diffuserEnabled);
            }

            if (diffuserMaterial != null)
            {
                Color color = diffuserColor * diffuserBrightness;
                color.a = diffuserOpacity;
                SetColor(diffuserMaterial, "_UnlitColor", color);
                SetColor(diffuserMaterial, "_BaseColor", color);
                SetFloat(diffuserMaterial, "_SurfaceType", 1f);
                SetFloat(diffuserMaterial, "_BlendMode", 0f);
                SetFloat(diffuserMaterial, "_AlphaCutoffEnable", 0f);
                SetFloat(diffuserMaterial, "_DoubleSidedEnable", 1f);
            }

            if (backlight != null)
            {
                backlight.transform.localPosition = new Vector3(tubeLength * 0.5f + diffuserDistance * 1.5f, 0f, 0f);
                backlight.enabled = diffuserEnabled && backlightEnabled;
                backlight.intensity = backlightIntensity;
                backlight.range = backlightRange;
                backlight.color = diffuserColor;
            }
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
    }
}
