using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace KaleidoscopeEngine.Lighting
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeLightingRig : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Lights")]
        [SerializeField] private Light keyLight;
        [SerializeField] private Light fillLight;
        [SerializeField] private Light rimLight;
        [SerializeField] private Light accentLight;
        [SerializeField] private Light movingLight;
        [SerializeField] private LightOrbitController movingLightOrbit;

        [Header("Parameters")]
        [SerializeField] private float keyIntensity = 2.4f;
        [SerializeField] private float fillIntensity = 0.9f;
        [SerializeField] private float rimIntensity = 1.3f;
        [SerializeField] private float accentIntensity = 1.6f;
        [SerializeField] private float movingLightIntensity = 1.4f;
        [SerializeField] private float orbitRadius = 3.8f;
        [SerializeField] private float orbitSpeed = 28f;
        [SerializeField] private bool movingLightEnabled = true;
        [SerializeField] private int seed = 4217;

        [Header("Post FX Targets")]
        [SerializeField] private float bloomIntensity = 0.45f;
        [SerializeField] private float bloomThreshold = 1.15f;
        [SerializeField] private float exposureCompensation;
        [SerializeField] private float saturation = 4f;
        [SerializeField] private float contrast = 8f;

        private readonly List<LightingPreset> presets = new List<LightingPreset>();
        private int activePresetIndex;
        private Volume postProcessVolume;

        public string ActivePresetName => presets.Count > 0 ? presets[activePresetIndex].displayName : "Runtime Jewelry Studio";
        public bool MovingLightEnabled => movingLightEnabled;
        public float KeyIntensity => keyLight != null ? keyLight.intensity : keyIntensity;
        public float AccentIntensity => accentLight != null ? accentLight.intensity : accentIntensity;
        public float BloomIntensity => bloomIntensity;

        public void Configure(Transform lightingTarget)
        {
            target = lightingTarget;
            BuildLights();
            BuildRuntimePresets();
            ApplyPreset(0);
            BuildPostProcessVolume();
        }

        private void BuildLights()
        {
            keyLight = CreateLight("Optical Key Light", LightType.Directional, new Vector3(44f, -34f, 0f), new Vector3(0f, 0f, 0f));
            fillLight = CreateLight("Optical Fill Light", LightType.Point, Vector3.zero, new Vector3(-2.9f, 1.7f, -2.5f));
            rimLight = CreateLight("Optical Rim Light", LightType.Point, Vector3.zero, new Vector3(2.2f, 1.9f, 2.7f));
            accentLight = CreateLight("Optical Accent Light", LightType.Spot, new Vector3(34f, -126f, 0f), new Vector3(-1.8f, 2.4f, 2.2f));
            movingLight = CreateLight("Moving Spark Light", LightType.Point, Vector3.zero, new Vector3(0f, 1.4f, -3.8f));

            movingLight.range = 7f;
            movingLightOrbit = movingLight.gameObject.AddComponent<LightOrbitController>();
            movingLightOrbit.Configure(target, orbitRadius, orbitSpeed, 0.55f, seed);
        }

        private Light CreateLight(string lightName, LightType type, Vector3 rotation, Vector3 position)
        {
            GameObject lightObject = new GameObject(lightName);
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.position = position;
            lightObject.transform.rotation = Quaternion.Euler(rotation);

            Light light = lightObject.AddComponent<Light>();
            light.type = type;
            light.range = 8f;
            light.spotAngle = 42f;
            light.shadows = type == LightType.Directional ? LightShadows.Soft : LightShadows.None;
            return light;
        }

        private void BuildRuntimePresets()
        {
            presets.Clear();
            presets.Add(Preset("Jewelry Studio", new Color(1f, 0.95f, 0.86f), new Color(0.5f, 0.68f, 1f), new Color(0.78f, 0.92f, 1f), new Color(1f, 0.72f, 0.42f), 2.4f, 0.85f, 1.25f, 1.45f, 1.25f, 0.45f));
            presets.Add(Preset("Opal Softbox", new Color(0.9f, 0.96f, 1f), new Color(0.8f, 0.88f, 1f), new Color(1f, 0.72f, 0.92f), new Color(0.6f, 0.95f, 1f), 1.65f, 1.35f, 0.9f, 1.1f, 1.0f, 0.55f));
            presets.Add(Preset("Ruby Deep Glow", new Color(1f, 0.84f, 0.72f), new Color(0.34f, 0.12f, 0.16f), new Color(0.8f, 0.16f, 0.08f), new Color(1f, 0.06f, 0.03f), 2.1f, 0.55f, 1.1f, 1.8f, 1.45f, 0.5f));
            presets.Add(Preset("Emerald Temple", new Color(0.85f, 1f, 0.82f), new Color(0.08f, 0.32f, 0.18f), new Color(0.4f, 1f, 0.58f), new Color(0.1f, 0.95f, 0.36f), 2.0f, 0.65f, 1.35f, 1.7f, 1.25f, 0.38f));
            presets.Add(Preset("Crystal Laboratory", new Color(0.88f, 0.94f, 1f), new Color(0.5f, 0.7f, 1f), new Color(0.82f, 0.95f, 1f), new Color(0.68f, 0.9f, 1f), 2.55f, 0.95f, 1.5f, 1.9f, 1.7f, 0.42f));
        }

        private LightingPreset Preset(string name, Color key, Color fill, Color rim, Color accent, float keyI, float fillI, float rimI, float accentI, float movingI, float bloom)
        {
            LightingPreset preset = ScriptableObject.CreateInstance<LightingPreset>();
            preset.displayName = name;
            preset.keyColor = key;
            preset.fillColor = fill;
            preset.rimColor = rim;
            preset.accentColor = accent;
            preset.keyIntensity = keyI;
            preset.fillIntensity = fillI;
            preset.rimIntensity = rimI;
            preset.accentIntensity = accentI;
            preset.movingLightIntensity = movingI;
            preset.bloomIntensity = bloom;
            return preset;
        }

        public void ToggleMovingLight()
        {
            movingLightEnabled = !movingLightEnabled;
            if (movingLight != null)
            {
                movingLight.enabled = movingLightEnabled;
            }
        }

        public void NextPreset()
        {
            if (presets.Count == 0)
            {
                return;
            }

            ApplyPreset((activePresetIndex + 1) % presets.Count);
        }

        private void ApplyPreset(int index)
        {
            if (presets.Count == 0)
            {
                return;
            }

            activePresetIndex = Mathf.Clamp(index, 0, presets.Count - 1);
            LightingPreset preset = presets[activePresetIndex];

            keyIntensity = preset.keyIntensity;
            fillIntensity = preset.fillIntensity;
            rimIntensity = preset.rimIntensity;
            accentIntensity = preset.accentIntensity;
            movingLightIntensity = preset.movingLightIntensity;
            bloomIntensity = preset.bloomIntensity;
            exposureCompensation = preset.exposureCompensation;

            ApplyLight(keyLight, preset.keyColor, keyIntensity);
            ApplyLight(fillLight, preset.fillColor, fillIntensity);
            ApplyLight(rimLight, preset.rimColor, rimIntensity);
            ApplyLight(accentLight, preset.accentColor, accentIntensity);
            ApplyLight(movingLight, Color.white, movingLightIntensity);
            if (movingLight != null)
            {
                movingLight.enabled = movingLightEnabled;
            }

            ApplyPostProcessValues();
        }

        private static void ApplyLight(Light light, Color color, float intensity)
        {
            if (light == null)
            {
                return;
            }

            light.color = color;
            light.intensity = intensity;
        }

        private void BuildPostProcessVolume()
        {
            GameObject volumeObject = new GameObject("Optical Gemstone Post FX");
            volumeObject.transform.SetParent(transform, false);
            postProcessVolume = volumeObject.AddComponent<Volume>();
            postProcessVolume.isGlobal = true;
            postProcessVolume.priority = 10f;
            postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            ApplyPostProcessValues();
        }

        private void ApplyPostProcessValues()
        {
            if (postProcessVolume == null || postProcessVolume.profile == null)
            {
                return;
            }

            // HDRP post-processing components are package-specific. This runtime volume
            // is configured by reflection so this script remains safe if HDRP package names move.
            TryConfigureHdrpPostProcess();
        }

        private void TryConfigureHdrpPostProcess()
        {
            AddAndSetComponent(
                "UnityEngine.Rendering.HighDefinition.Bloom, Unity.RenderPipelines.HighDefinition.Runtime",
                ("intensity", bloomIntensity),
                ("threshold", bloomThreshold));

            AddAndSetComponent(
                "UnityEngine.Rendering.HighDefinition.ColorAdjustments, Unity.RenderPipelines.HighDefinition.Runtime",
                ("saturation", saturation),
                ("contrast", contrast),
                ("postExposure", exposureCompensation));

            AddAndSetComponent(
                "UnityEngine.Rendering.HighDefinition.Vignette, Unity.RenderPipelines.HighDefinition.Runtime",
                ("intensity", 0.08f));
        }

        private void AddAndSetComponent(string typeName, params (string fieldName, object value)[] values)
        {
            Type componentType = Type.GetType(typeName);
            if (componentType == null || postProcessVolume == null || postProcessVolume.profile == null)
            {
                return;
            }

            VolumeComponent component = FindVolumeComponent(componentType);
            if (component == null)
            {
                component = postProcessVolume.profile.Add(componentType, true);
            }

            if (component == null)
            {
                return;
            }

            for (int i = 0; i < values.Length; i++)
            {
                SetVolumeParameter(component, values[i].fieldName, values[i].value);
            }
        }

        private VolumeComponent FindVolumeComponent(Type componentType)
        {
            if (postProcessVolume == null || postProcessVolume.profile == null)
            {
                return null;
            }

            foreach (VolumeComponent component in postProcessVolume.profile.components)
            {
                if (component != null && component.GetType() == componentType)
                {
                    return component;
                }
            }

            return null;
        }

        private static void SetVolumeParameter(VolumeComponent component, string fieldName, object value)
        {
            FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            object parameter = field != null ? field.GetValue(component) : null;
            if (parameter == null)
            {
                return;
            }

            PropertyInfo overrideState = parameter.GetType().GetProperty("overrideState");
            PropertyInfo valueProperty = parameter.GetType().GetProperty("value");
            overrideState?.SetValue(parameter, true);
            valueProperty?.SetValue(parameter, value);
        }
    }
}
