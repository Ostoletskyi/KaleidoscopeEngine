using System;
using System.Collections.Generic;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    public enum KaleidoscopeSourceCategory
    {
        TransparentGemstones,
        ColoredGlass,
        Liquids,
        Backgrounds,
        UserImages,
        ProceduralColorBlobs,
        PolygonalGeometry,
        ExperimentalSources
    }

    [Serializable]
    public sealed class KaleidoscopeSourcePreset
    {
        public string id;
        public KaleidoscopeSourceCategory category;
        public string displayName;
        [TextArea(2, 4)] public string description;
        [Range(0f, 1f)] public float performanceCost;
        public string compatibility;
        public Texture2D thumbnail;
        public Texture2D sourceTexture;
        public Color sourceBackground = new Color(0.045f, 0.048f, 0.055f, 1f);

        public KaleidoscopeSourcePreset(
            string id,
            KaleidoscopeSourceCategory category,
            string displayName,
            string description,
            float performanceCost,
            string compatibility,
            Color sourceBackground)
        {
            this.id = id;
            this.category = category;
            this.displayName = displayName;
            this.description = description;
            this.performanceCost = Mathf.Clamp01(performanceCost);
            this.compatibility = compatibility;
            this.sourceBackground = sourceBackground;
        }
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeSourceLibrary : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopeSourceModeController sourceModeController;
        [SerializeField] private KaleidoscopeSourceModeManager sourceModeManager;
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private KaleidoscopeDebugPanel statusPanel;

        [Header("Library")]
        [SerializeField] private KaleidoscopeSourceCategory currentCategory = KaleidoscopeSourceCategory.TransparentGemstones;
        [SerializeField] private List<KaleidoscopeSourcePreset> presets = new List<KaleidoscopeSourcePreset>();

        private readonly List<KaleidoscopeSourcePreset> categoryScratch = new List<KaleidoscopeSourcePreset>();
        private readonly List<string> eventLog = new List<string>();
        private int currentPresetIndex;
        private Texture2D importedUserTexture;

        public KaleidoscopeSourceCategory CurrentCategory => currentCategory;
        public int CurrentPresetIndex => currentPresetIndex;
        public IReadOnlyList<KaleidoscopeSourcePreset> Presets => presets;
        public IReadOnlyList<string> EventLog => eventLog;
        public KaleidoscopeSourcePreset CurrentPreset => GetPresetAt(currentCategory, currentPresetIndex);
        public string CurrentPresetName => CurrentPreset != null ? CurrentPreset.displayName : "None";
        public string CurrentDescription => CurrentPreset != null ? CurrentPreset.description : "No source preset loaded.";
        public float CurrentPerformanceCost => CurrentPreset != null ? CurrentPreset.performanceCost : 0f;
        public string CurrentCompatibility => CurrentPreset != null ? CurrentPreset.compatibility : "n/a";
        public Texture CurrentSourceTexture => sourceModeController != null ? sourceModeController.CurrentSourceTexture : renderPipeline != null ? renderPipeline.ActiveSourceTexture : null;

        public void Configure(
            KaleidoscopeSourceModeController sourceController,
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopeDebugPanel panel)
        {
            sourceModeController = sourceController;
            sourceModeManager = sourceController != null ? sourceController.ModeManager : null;
            renderPipeline = pipeline;
            statusPanel = panel;
            EnsureDefaultPresets();
            ApplyCurrentPreset(false);
        }

        public void ConfigureStatusPanel(KaleidoscopeDebugPanel panel)
        {
            statusPanel = panel;
        }

        private void Awake()
        {
            EnsureDefaultPresets();
        }

        public void SetCategory(KaleidoscopeSourceCategory category)
        {
            EnsureDefaultPresets();
            currentCategory = category;
            currentPresetIndex = Mathf.Clamp(currentPresetIndex, 0, Mathf.Max(0, CountPresets(category) - 1));
            ApplyCurrentPreset(true);
        }

        public void LoadPreset(KaleidoscopeSourcePreset preset)
        {
            if (preset == null)
            {
                return;
            }

            EnsureDefaultPresets();
            currentCategory = preset.category;
            currentPresetIndex = IndexOfPresetInCategory(preset);
            ApplyCurrentPreset(true);
        }

        public void NextPreset()
        {
            StepPreset(1);
        }

        public void PreviousPreset()
        {
            StepPreset(-1);
        }

        public void RandomizeCurrentSource()
        {
            sourceModeController?.RandomizeCurrentMode();
            PostEvent($"Randomized source: {CurrentPresetName}");
        }

        public void ResetCurrentSource()
        {
            sourceModeController?.ResetCurrentMode();
            if (currentCategory != KaleidoscopeSourceCategory.UserImages)
            {
                renderPipeline?.ClearExternalSourceTexture();
            }

            ApplyCurrentPreset(false);
            PostEvent($"Reset source: {CurrentPresetName}");
        }

        public void LoadUserImage(Texture2D texture, string displayName)
        {
            if (texture == null)
            {
                return;
            }

            if (importedUserTexture != null && importedUserTexture != texture)
            {
                Destroy(importedUserTexture);
            }

            importedUserTexture = texture;
            importedUserTexture.name = string.IsNullOrWhiteSpace(displayName) ? "User Image Source" : displayName;
            importedUserTexture.wrapMode = TextureWrapMode.Repeat;
            importedUserTexture.filterMode = FilterMode.Bilinear;

            EnsureDefaultPresets();
            KaleidoscopeSourcePreset preset = FindPresetById("user_image_import");
            if (preset == null)
            {
                preset = new KaleidoscopeSourcePreset(
                    "user_image_import",
                    KaleidoscopeSourceCategory.UserImages,
                    importedUserTexture.name,
                    "Imported local image used as a repeatable optical source.",
                    0.42f,
                    "ImageWallpaper runtime mode",
                    new Color(0.02f, 0.024f, 0.03f, 1f));
                presets.Add(preset);
            }

            preset.displayName = importedUserTexture.name;
            preset.sourceTexture = importedUserTexture;
            preset.thumbnail = importedUserTexture;

            currentCategory = KaleidoscopeSourceCategory.UserImages;
            currentPresetIndex = IndexOfPresetInCategory(preset);
            ApplyCurrentPreset(false);
            PostEvent($"Loaded user image: {importedUserTexture.name}");
        }

        public List<KaleidoscopeSourcePreset> GetPresets(KaleidoscopeSourceCategory category)
        {
            categoryScratch.Clear();
            for (int i = 0; i < presets.Count; i++)
            {
                KaleidoscopeSourcePreset preset = presets[i];
                if (preset != null && preset.category == category)
                {
                    categoryScratch.Add(preset);
                }
            }

            return categoryScratch;
        }

        public string CategoryDisplayName(KaleidoscopeSourceCategory category)
        {
            switch (category)
            {
                case KaleidoscopeSourceCategory.TransparentGemstones:
                    return "Transparent Gemstones";
                case KaleidoscopeSourceCategory.ColoredGlass:
                    return "Colored Glass";
                case KaleidoscopeSourceCategory.Liquids:
                    return "Liquids";
                case KaleidoscopeSourceCategory.Backgrounds:
                    return "Backgrounds";
                case KaleidoscopeSourceCategory.UserImages:
                    return "User Images";
                case KaleidoscopeSourceCategory.ProceduralColorBlobs:
                    return "Procedural Color Blobs";
                case KaleidoscopeSourceCategory.PolygonalGeometry:
                    return "Polygonal Geometry";
                case KaleidoscopeSourceCategory.ExperimentalSources:
                    return "Experimental Sources";
                default:
                    return category.ToString();
            }
        }

        private void StepPreset(int delta)
        {
            EnsureDefaultPresets();
            int count = CountPresets(currentCategory);
            if (count == 0)
            {
                return;
            }

            currentPresetIndex = (currentPresetIndex + delta + count) % count;
            ApplyCurrentPreset(true);
        }

        private void ApplyCurrentPreset(bool announce)
        {
            EnsureDefaultPresets();
            KaleidoscopeSourcePreset preset = CurrentPreset;
            KaleidoscopeSourceModeKind mode = ResolveRuntimeMode(currentCategory);

            if (preset != null)
            {
                renderPipeline?.SetSourceBackgroundColor(preset.sourceBackground);
            }

            if (currentCategory == KaleidoscopeSourceCategory.UserImages && preset != null && preset.sourceTexture != null)
            {
                sourceModeController?.SetImageSourceTextures(new[] { preset.sourceTexture });
            }
            else if (currentCategory == KaleidoscopeSourceCategory.Backgrounds && preset != null && preset.sourceTexture != null)
            {
                sourceModeController?.SetImageSourceTextures(new[] { preset.sourceTexture });
            }
            else if (mode == KaleidoscopeSourceModeKind.ImageWallpaper)
            {
                sourceModeController?.SetImageSourceTextures(null);
            }

            sourceModeController?.SetMode(mode);
            renderPipeline?.ReturnToKaleidoscopeView();

            if (currentCategory == KaleidoscopeSourceCategory.ColoredGlass ||
                currentCategory == KaleidoscopeSourceCategory.ProceduralColorBlobs ||
                currentCategory == KaleidoscopeSourceCategory.ExperimentalSources)
            {
                sourceModeController?.RandomizeCurrentMode();
            }

            if (announce)
            {
                PostEvent($"Source: {CategoryDisplayName(currentCategory)} / {CurrentPresetName}");
            }
        }

        private KaleidoscopeSourceModeKind ResolveRuntimeMode(KaleidoscopeSourceCategory category)
        {
            switch (category)
            {
                case KaleidoscopeSourceCategory.TransparentGemstones:
                    return KaleidoscopeSourceModeKind.Gemstones;
                case KaleidoscopeSourceCategory.ColoredGlass:
                    return KaleidoscopeSourceModeKind.ColoredGlassPhysical;
                case KaleidoscopeSourceCategory.PolygonalGeometry:
                    return KaleidoscopeSourceModeKind.PolygonGeometry;
                case KaleidoscopeSourceCategory.Liquids:
                    return KaleidoscopeSourceModeKind.LiquidIllusion;
                case KaleidoscopeSourceCategory.ExperimentalSources:
                    return KaleidoscopeSourceModeKind.Experimental;
                case KaleidoscopeSourceCategory.Backgrounds:
                case KaleidoscopeSourceCategory.UserImages:
                    return KaleidoscopeSourceModeKind.ImageWallpaper;
                case KaleidoscopeSourceCategory.ProceduralColorBlobs:
                    return KaleidoscopeSourceModeKind.ProceduralColorBlobs;
                default:
                    return KaleidoscopeSourceModeKind.Gemstones;
            }
        }

        private int CountPresets(KaleidoscopeSourceCategory category)
        {
            int count = 0;
            for (int i = 0; i < presets.Count; i++)
            {
                KaleidoscopeSourcePreset preset = presets[i];
                if (preset != null && preset.category == category)
                {
                    count++;
                }
            }

            return count;
        }

        private KaleidoscopeSourcePreset GetPresetAt(KaleidoscopeSourceCategory category, int categoryIndex)
        {
            int index = 0;
            for (int i = 0; i < presets.Count; i++)
            {
                KaleidoscopeSourcePreset preset = presets[i];
                if (preset == null || preset.category != category)
                {
                    continue;
                }

                if (index == categoryIndex)
                {
                    return preset;
                }

                index++;
            }

            return null;
        }

        private int IndexOfPresetInCategory(KaleidoscopeSourcePreset target)
        {
            int index = 0;
            for (int i = 0; i < presets.Count; i++)
            {
                KaleidoscopeSourcePreset preset = presets[i];
                if (preset == null || preset.category != target.category)
                {
                    continue;
                }

                if (ReferenceEquals(preset, target))
                {
                    return index;
                }

                index++;
            }

            return 0;
        }

        private KaleidoscopeSourcePreset FindPresetById(string id)
        {
            for (int i = 0; i < presets.Count; i++)
            {
                KaleidoscopeSourcePreset preset = presets[i];
                if (preset != null && preset.id == id)
                {
                    return preset;
                }
            }

            return null;
        }

        private void PostEvent(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            eventLog.Insert(0, $"{Time.realtimeSinceStartup:0000.0}s  {message}");
            if (eventLog.Count > 64)
            {
                eventLog.RemoveAt(eventLog.Count - 1);
            }

            statusPanel?.PostOperatorMessage(message);
        }

        private void EnsureDefaultPresets()
        {
            if (presets.Count > 0)
            {
                return;
            }

            presets.Add(new KaleidoscopeSourcePreset("gems_clear", KaleidoscopeSourceCategory.TransparentGemstones, "Clear gem chamber", "Physical transparent gemstones and micro fragments in the tube.", 0.54f, "Object chamber / HDRP", new Color(0.045f, 0.048f, 0.055f, 1f)));
            presets.Add(new KaleidoscopeSourcePreset("gems_dense", KaleidoscopeSourceCategory.TransparentGemstones, "Dense jewel field", "Higher density optical chamber look, ready for adaptive quality to trim cost.", 0.68f, "Object chamber / adaptive quality", new Color(0.035f, 0.04f, 0.052f, 1f)));

            presets.Add(new KaleidoscopeSourcePreset("glass_prismatic", KaleidoscopeSourceCategory.ColoredGlass, "Prismatic glass", "Colored glass shards represented by crisp polygonal optical cells.", 0.36f, "Procedural texture", new Color(0.028f, 0.03f, 0.04f, 1f)));
            presets.Add(new KaleidoscopeSourcePreset("glass_soft", KaleidoscopeSourceCategory.ColoredGlass, "Soft stained glass", "Gentler color fields for slower viewer-safe operation.", 0.32f, "Procedural texture", new Color(0.04f, 0.035f, 0.045f, 1f)));

            presets.Add(new KaleidoscopeSourcePreset("liquid_copper_blue", KaleidoscopeSourceCategory.Liquids, "Copper-blue flow", "Viscous flowing color texture without true fluid simulation.", 0.46f, "Procedural texture", new Color(0.015f, 0.025f, 0.035f, 1f)));
            presets.Add(new KaleidoscopeSourcePreset("liquid_spectral", KaleidoscopeSourceCategory.Liquids, "Spectral liquid", "Liquid source randomized on load for luminous transitions.", 0.48f, "Procedural texture", new Color(0.02f, 0.02f, 0.032f, 1f)));

            presets.Add(new KaleidoscopeSourcePreset("background_deep", KaleidoscopeSourceCategory.Backgrounds, "Deep optical ground", "Image wallpaper fallback pattern over a quiet dark optical background.", 0.26f, "Image wallpaper fallback", new Color(0.025f, 0.028f, 0.038f, 1f)));
            presets.Add(new KaleidoscopeSourcePreset("background_luminous", KaleidoscopeSourceCategory.Backgrounds, "Luminous backing", "Brighter field intended for silhouette and transfer studies.", 0.28f, "Image wallpaper fallback", new Color(0.055f, 0.058f, 0.066f, 1f)));

            presets.Add(new KaleidoscopeSourcePreset("user_image_empty", KaleidoscopeSourceCategory.UserImages, "Import image", "Use the Source Library or Operator Console button to load a local image.", 0.4f, "PNG/JPG in Editor", new Color(0.02f, 0.024f, 0.03f, 1f)));

            presets.Add(new KaleidoscopeSourcePreset("blobs_soft", KaleidoscopeSourceCategory.ProceduralColorBlobs, "Soft color blobs", "Procedural color cells tuned as broad blob-like color fields.", 0.34f, "Procedural texture", new Color(0.025f, 0.024f, 0.032f, 1f)));
            presets.Add(new KaleidoscopeSourcePreset("blobs_high_chroma", KaleidoscopeSourceCategory.ProceduralColorBlobs, "High-chroma blobs", "Randomized saturated fields for vivid optical studies.", 0.38f, "Procedural texture", new Color(0.025f, 0.02f, 0.026f, 1f)));

            presets.Add(new KaleidoscopeSourcePreset("geometry_hex", KaleidoscopeSourceCategory.PolygonalGeometry, "Hexagons", "Classic six-sided procedural source geometry.", 0.34f, "Procedural texture", new Color(0.03f, 0.032f, 0.04f, 1f)));
            presets.Add(new KaleidoscopeSourcePreset("geometry_oct_dec", KaleidoscopeSourceCategory.PolygonalGeometry, "Octagon / decagon study", "Polygonal source category prepared for octagon and decagon expansion.", 0.36f, "Current runtime maps to procedural polygon mode", new Color(0.032f, 0.032f, 0.042f, 1f)));

            presets.Add(new KaleidoscopeSourcePreset("experimental_flow", KaleidoscopeSourceCategory.ExperimentalSources, "Experimental flow", "Sandbox source for risky visual ideas without touching the viewer image by default.", 0.52f, "Procedural texture", new Color(0.015f, 0.018f, 0.026f, 1f)));
        }
    }
}
