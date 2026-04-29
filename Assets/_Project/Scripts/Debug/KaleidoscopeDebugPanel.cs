using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Geometry;
using KaleidoscopeEngine.Lighting;
using KaleidoscopeEngine.Materials;
using KaleidoscopeEngine.Mirrors;
using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public enum KaleidoscopeDebugPanelMode
    {
        Hidden,
        Compact,
        Full
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeDebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopePhysicsChamber chamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private PhysicsSandboxCameraController cameraController;
        [SerializeField] private PhysicsSandboxChamberDebugView chamberDebugView;
        [SerializeField] private PhysicsSandboxMetrics metrics;
        [SerializeField] private GemstoneMaterialAssigner materialAssigner;
        [SerializeField] private KaleidoscopeLightingRig lightingRig;
        [SerializeField] private GemGeometryAssigner geometryAssigner;
        [SerializeField] private GemSparkleController sparkleController;
        [SerializeField] private FakeCausticBunnyProjector causticProjector;
        [SerializeField] private KaleidoscopeRenderPipeline mirrorPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private OpticalSourceChamber opticalSourceChamber;
        [SerializeField] private KaleidoscopeTubeChamberSettings tubeSettings;

        [Header("Display")]
        [SerializeField] private KaleidoscopeDebugPanelMode mode = KaleidoscopeDebugPanelMode.Hidden;
        [SerializeField, Range(0.15f, 1f)] private float panelOpacity = 0.48f;

        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private float fps;

        public KaleidoscopeDebugPanelMode Mode => mode;

        public void Configure(KaleidoscopePhysicsChamber physicsChamber, GemstoneSpawner gemstoneSpawner)
        {
            chamber = physicsChamber;
            spawner = gemstoneSpawner;
        }

        public void ConfigureDebugSystems(
            PhysicsSandboxCameraController sandboxCamera,
            PhysicsSandboxChamberDebugView debugView,
            PhysicsSandboxMetrics sandboxMetrics,
            GemstoneMaterialAssigner opticalAssigner = null,
            KaleidoscopeLightingRig opticalLightingRig = null,
            GemGeometryAssigner proceduralGeometryAssigner = null,
            GemSparkleController gemSparkles = null,
            FakeCausticBunnyProjector fakeCaustics = null,
            KaleidoscopeRenderPipeline kaleidoscopePipeline = null,
            KaleidoscopeMirrorController kaleidoscopeMirror = null,
            OpticalSourceChamber sourceChamber = null)
        {
            cameraController = sandboxCamera;
            chamberDebugView = debugView;
            metrics = sandboxMetrics;
            materialAssigner = opticalAssigner;
            lightingRig = opticalLightingRig;
            geometryAssigner = proceduralGeometryAssigner;
            sparkleController = gemSparkles;
            causticProjector = fakeCaustics;
            mirrorPipeline = kaleidoscopePipeline;
            mirrorController = kaleidoscopeMirror;
            opticalSourceChamber = sourceChamber;
        }

        public void ConfigureTubeSettings(KaleidoscopeTubeChamberSettings settings)
        {
            tubeSettings = settings;
        }

        public void TogglePanel()
        {
            mode = mode == KaleidoscopeDebugPanelMode.Hidden
                ? KaleidoscopeDebugPanelMode.Compact
                : KaleidoscopeDebugPanelMode.Hidden;
        }

        public void SetCompactMode()
        {
            mode = KaleidoscopeDebugPanelMode.Compact;
        }

        public void SetFullMode()
        {
            mode = KaleidoscopeDebugPanelMode.Full;
        }

        public void Hide()
        {
            mode = KaleidoscopeDebugPanelMode.Hidden;
        }

        private void Update()
        {
            float delta = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float instantFps = 1f / delta;
            fps = fps <= 0.01f ? instantFps : Mathf.Lerp(fps, instantFps, 0.08f);

            if (mode == KaleidoscopeDebugPanelMode.Hidden &&
                mirrorPipeline != null &&
                mirrorPipeline.HasBlockingDiagnostic)
            {
                mode = KaleidoscopeDebugPanelMode.Compact;
            }
        }

        private void OnGUI()
        {
            if (mode == KaleidoscopeDebugPanelMode.Hidden)
            {
                return;
            }

            EnsureStyles();

            float width = mode == KaleidoscopeDebugPanelMode.Full ? 330f : 210f;
            float height = mode == KaleidoscopeDebugPanelMode.Full ? 640f : 108f;
            Rect area = new Rect(Screen.width - width - 14f, 14f, width, height);

            GUILayout.BeginArea(area, panelStyle);
            GUILayout.Label("KALEIDOSCOPE", titleStyle);
            DrawLine("View", mirrorPipeline != null ? mirrorPipeline.ViewMode : "Raw");
            DrawLine("Source", mirrorPipeline != null ? mirrorPipeline.SourceModeName : "n/a");
            DrawLine("Segments", mirrorController != null ? mirrorController.SegmentCount.ToString() : "n/a");
            DrawLine("Axial Speed", chamber != null ? $"{chamber.CurrentAxialRotationSpeed:F1} deg/s" : "n/a");
            if (mirrorPipeline != null && mirrorPipeline.HasBlockingDiagnostic)
            {
                DrawLine("Status", "Check pipeline");
            }

            if (mode == KaleidoscopeDebugPanelMode.Full)
            {
                GUILayout.Space(6f);
                DrawLine("FPS", $"{fps:F0}");
                DrawLine("Objects", spawner != null ? spawner.SpawnedObjects.Count.ToString() : "n/a");
                DrawLine("Visible", metrics != null ? metrics.VisibleGemCount.ToString() : "n/a");
                DrawLine("Sleeping", metrics != null ? metrics.SleepingBodyCount.ToString() : "n/a");
                DrawLine("Sparkles", sparkleController != null ? $"{sparkleController.ActiveSparkles}" : "n/a");
                DrawLine("Caustics", causticProjector != null && causticProjector.CausticsEnabled ? "On" : "Off");
                DrawLine("Lighting", lightingRig != null ? lightingRig.ActivePresetName : "n/a");
                DrawLine("Materials", materialAssigner != null ? materialAssigner.MaterialMode : "n/a");
                DrawLine("Geometry", geometryAssigner != null ? geometryAssigner.GeometryMode : "n/a");
                DrawLine("RenderTexture", mirrorPipeline != null ? $"{mirrorPipeline.RenderTextureWidth}x{mirrorPipeline.RenderTextureHeight}" : "n/a");
                DrawLine("Prism Mode", mirrorController != null ? mirrorController.PrismModeName : "n/a");
                DrawLine("Mirror Angle", mirrorController != null ? $"{mirrorController.MirrorAngleDegrees:F1} deg" : "n/a");
                DrawLine("Computed Seg", mirrorController != null ? mirrorController.ComputedSegmentCount.ToString() : "n/a");
                DrawLine("Manual Seg", mirrorController != null ? mirrorController.ManualSegmentCount.ToString() : "n/a");
                DrawLine("Angle Mode", mirrorController != null && mirrorController.UseMirrorAngleMode ? "On" : "Off");
                DrawLine("Source Mask", mirrorPipeline != null ? mirrorPipeline.SourceCameraCullingMaskMode : "n/a");
                DrawLine("Source Ribs", mirrorPipeline != null && mirrorPipeline.RibsVisibleToSourceCamera ? "On" : "Off");
                DrawLine("Diffuser", opticalSourceChamber != null && opticalSourceChamber.DiffuserEnabled ? "On" : "Off");
                DrawLine("Optical Mask", mirrorController != null ? mirrorController.MaskModeName : "n/a");
                DrawLine("Seam Soft", mirrorController != null ? $"{mirrorController.SeamSoftness:F3}" : "n/a");
                DrawLine("Seam Blend", mirrorController != null ? $"{mirrorController.SeamBlendStrength:F2}" : "n/a");
                DrawLine("Center Radius", mirrorController != null ? $"{mirrorController.CenterMaskRadius:F3}" : "n/a");
                DrawLine("Center Exposure", mirrorController != null ? $"{mirrorController.CenterExposure:F2}" : "n/a");
                DrawLine("Optical Density", mirrorController != null ? $"{mirrorController.OpticalDensity:F2}" : "n/a");
                DrawLine("Mosaic Score", mirrorController != null ? $"{mirrorController.MosaicCohesionScore:F2}" : "n/a");
                DrawLine("Dominant Ratio", spawner != null ? $"{spawner.DominantGemRatio:P0}" : "n/a");
                DrawLine("Medium Shards", spawner != null ? spawner.MediumShardCount.ToString() : "n/a");
                DrawLine("Wobble", mirrorController != null && mirrorController.WobbleEnabled ? "On" : "Off");
                DrawLine("Breathing", mirrorController != null && mirrorController.BreathingEnabled ? "On" : "Off");
                DrawLine("Center Drift", mirrorController != null && mirrorController.CenterDriftEnabled ? "On" : "Off");
                DrawLine("Segments Var", mirrorController != null && mirrorController.SegmentVariationEnabled ? "On" : "Off");
                DrawLine("Asymmetry", mirrorController != null && mirrorController.AsymmetryEnabled ? "On" : "Off");
                DrawLine("Vignette", mirrorController != null && mirrorController.VignetteEnabled ? "On" : "Off");
                DrawLine("Drift Amount", mirrorController != null ? $"{mirrorController.DriftAmount:F3}" : "n/a");
                DrawLine("Tube Visuals", chamberDebugView != null && chamberDebugView.ShowChamberVisuals ? "On" : "Off");
                DrawLine("Ribs", tubeSettings != null && tubeSettings.internalRibsEnabled ? $"{tubeSettings.internalRibCount}" : "Off");
                GUILayout.Space(6f);
                GUILayout.Label(mirrorPipeline != null ? mirrorPipeline.DiagnosticStatus : "No pipeline diagnostics.", labelStyle);
            }

            GUILayout.EndArea();
        }

        private void DrawLine(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(112f));
            GUILayout.Label(value, labelStyle);
            GUILayout.EndHorizontal();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            Texture2D background = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            background.SetPixel(0, 0, new Color(0.03f, 0.045f, 0.055f, panelOpacity));
            background.Apply();

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                normal = { background = background }
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.68f, 0.92f, 1f, 0.92f) }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.9f, 0.96f, 1f, 0.86f) }
            };
        }
    }
}
