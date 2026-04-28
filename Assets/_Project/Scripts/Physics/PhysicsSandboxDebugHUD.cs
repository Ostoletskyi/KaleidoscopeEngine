using UnityEngine;
using KaleidoscopeEngine.FX;
using KaleidoscopeEngine.Geometry;
using KaleidoscopeEngine.Lighting;
using KaleidoscopeEngine.Materials;
using KaleidoscopeEngine.Mirrors;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class PhysicsSandboxDebugHUD : MonoBehaviour
    {
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
        [SerializeField] private KaleidoscopeTubeChamberSettings tubeSettings;
        [SerializeField] private bool visible = true;

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
            KaleidoscopeMirrorController kaleidoscopeMirror = null)
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
            tubeSettings = debugView != null ? debugView.GetComponentInChildren<KaleidoscopeTubeChamberSettings>() : null;
        }

        public void ConfigureTubeSettings(KaleidoscopeTubeChamberSettings settings)
        {
            tubeSettings = settings;
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16f, 16f, 520f, 600f), GUI.skin.box);
            GUILayout.Label("Physics Sandbox");
            GUILayout.Label("Chamber Type: Tube");
            GUILayout.Label($"Tube View: {(cameraController != null && cameraController.IsFrontFacing ? "Front-facing" : "Inspection")}");
            GUILayout.Label($"Objects: {(spawner != null ? spawner.SpawnedObjects.Count : 0)}");
            GUILayout.Label($"Visible: {(metrics != null ? metrics.VisibleGemCount : 0)}");
            GUILayout.Label($"Escaped: {(metrics != null ? metrics.EscapedGemCount : 0)}");
            GUILayout.Label($"Average Velocity: {(metrics != null ? metrics.AverageVelocity.ToString("F2") : "n/a")}");
            GUILayout.Label($"Sleeping Bodies: {(metrics != null ? metrics.SleepingBodyCount : 0)}");
            GUILayout.Label($"Seed: {(spawner != null ? spawner.ActiveSeed : 0)}");
            GUILayout.Label($"Tilt: {(chamber != null ? chamber.CurrentTiltInput.ToString("F2") : "n/a")}");
            GUILayout.Label($"Axial Rotation: {(chamber != null && chamber.AxialRotationEnabled ? "On" : "Off")}");
            GUILayout.Label($"Axial Speed: {(chamber != null ? chamber.AxialRotationSpeed.ToString("F1") : "n/a")} deg/s");
            GUILayout.Label($"Current Axial Speed: {(chamber != null ? chamber.CurrentAxialRotationSpeed.ToString("F1") : "n/a")} deg/s");
            GUILayout.Label($"World Rotation: {(chamber != null && chamber.WorldRotationEnabled ? "On" : "Off")}");
            GUILayout.Label($"World Rotation Input: {(chamber != null ? chamber.RequestedRotationSpeed.ToString("F2") : "n/a")}");
            GUILayout.Label($"Shake: {(chamber != null ? chamber.ShakeStrength.ToString("F2") : "n/a")}");
            GUILayout.Label($"Camera Mode: {(cameraController != null ? cameraController.ModeName : "n/a")} / Distance {(cameraController != null ? cameraController.ZoomDistance.ToString("F1") : "n/a")}");
            GUILayout.Label($"Tube Visual Segments: {(tubeSettings != null ? tubeSettings.tubeVisualSegments : 0)}");
            GUILayout.Label($"Tube Collider Segments: {(tubeSettings != null ? tubeSettings.tubeColliderSegments : 0)}");
            GUILayout.Label($"Tumbler Ribs: {(tubeSettings != null && tubeSettings.internalRibsEnabled ? "Rifled back-feed" : "Off")} / {(tubeSettings != null ? tubeSettings.internalRibCount : 0)}");
            GUILayout.Label($"Depth Guides: {(tubeSettings != null && tubeSettings.showDepthGuideRings ? "On" : "Off")}");
            GUILayout.Label($"Front Cap: {(chamberDebugView != null && !chamberDebugView.CutawayMode ? "Enabled" : "Disabled")}");
            GUILayout.Label($"Cutaway: {(chamberDebugView != null && chamberDebugView.CutawayMode ? "On" : "Off")}");
            GUILayout.Label($"Chamber Visuals: {(chamberDebugView != null && chamberDebugView.ShowChamberVisuals ? "On" : "Off")}");
            GUILayout.Label($"Collider Gizmos: {(chamberDebugView != null && chamberDebugView.ShowCollidersDebug ? "On" : "Off")}");
            GUILayout.Label($"Geometry Mode: {(geometryAssigner != null ? geometryAssigner.GeometryMode : "Legacy")}");
            GUILayout.Label("Facet Mode: Enhanced");
            GUILayout.Label($"Mesh Types: {(geometryAssigner != null ? geometryAssigner.ActiveMeshTypesCount : 0)}");
            GUILayout.Label($"Gem Collider Mode: {(geometryAssigner != null ? geometryAssigner.ColliderModeName : "n/a")}");
            GUILayout.Label($"Generated Meshes: {(geometryAssigner != null ? geometryAssigner.TotalGeneratedMeshes : 0)}");
            GUILayout.Label($"Avg Vertices: {(geometryAssigner != null ? geometryAssigner.AverageVertexCount.ToString("F1") : "n/a")}");
            GUILayout.Label($"Material Mode: {(materialAssigner != null ? materialAssigner.MaterialMode : "Placeholder")}");
            GUILayout.Label($"Gemstone Profiles: {(materialAssigner != null ? materialAssigner.ProfileCount : 0)}");
            GUILayout.Label($"Active Lighting Preset: {(lightingRig != null ? lightingRig.ActivePresetName : "n/a")}");
            GUILayout.Label($"Moving Light: {(lightingRig != null && lightingRig.MovingLightEnabled ? "On" : "Off")}");
            GUILayout.Label($"Moving Light Intensity: {(lightingRig != null ? lightingRig.MovingLightIntensity.ToString("F2") : "n/a")}");
            GUILayout.Label($"Key Light: {(lightingRig != null ? lightingRig.KeyIntensity.ToString("F2") : "n/a")}");
            GUILayout.Label($"Accent Light: {(lightingRig != null ? lightingRig.AccentIntensity.ToString("F2") : "n/a")}");
            GUILayout.Label($"Bloom Target: {(lightingRig != null ? lightingRig.BloomIntensity.ToString("F2") : "n/a")}");
            GUILayout.Label("White Stone Mode: Opal pearly / Quartz clear / Glass cyan");
            GUILayout.Label($"Sparkles: {(sparkleController != null && sparkleController.SparkleEnabled ? "On" : "Off")}");
            GUILayout.Label($"Active Sparkles: {(sparkleController != null ? sparkleController.ActiveSparkles : 0)}");
            GUILayout.Label($"Sparkle Intensity: {(sparkleController != null ? sparkleController.SparkleIntensity.ToString("F2") : "n/a")}");
            GUILayout.Label($"Fake Caustics: {(causticProjector != null && causticProjector.CausticsEnabled ? "On" : "Off")}");
            GUILayout.Label($"Caustic Spot Count: {(causticProjector != null ? causticProjector.SpotCount : 0)}");
            GUILayout.Label($"View Mode: {(mirrorPipeline != null ? mirrorPipeline.ViewMode : "Raw")}");
            GUILayout.Label($"Segments: {(mirrorController != null ? mirrorController.SegmentCount : 0)}");
            GUILayout.Label($"Pattern Zoom: {(mirrorController != null ? mirrorController.PatternZoom.ToString("F2") : "n/a")}");
            GUILayout.Label($"Pattern Rotation: {(mirrorController != null ? (mirrorController.PatternRotation * Mathf.Rad2Deg).ToString("F1") : "n/a")} deg");
            GUILayout.Label($"Radial Distortion: {(mirrorController != null ? mirrorController.RadialDistortion.ToString("F2") : "n/a")}");
            GUILayout.Label($"RenderTexture: {(mirrorPipeline != null ? $"{mirrorPipeline.RenderTextureWidth}x{mirrorPipeline.RenderTextureHeight}" : "n/a")}");
            GUILayout.Label("WASD / ЦФЫВ tilt, Q/E / ЙУ axial speed, X / Ч axial toggle");
            GUILayout.Label("Space shake, R / К reset, T / Е respawn, F / А front view");
            GUILayout.Label("C / С camera mode, H / Р tube visual, B / И front cap");
            GUILayout.Label("G gizmos, J / О geometry, L / Д light, P / З preset, O / Щ materials");
            GUILayout.Label("[ / Х moving light -, ] / Ъ moving light +");
            GUILayout.Label("K / Л sparkles, N / Т caustics, , / Б sparkle -, . / Ю sparkle +");
            GUILayout.Label("M / Ь raw/kaleidoscope, 1/2 segments, 3/4 zoom, 5/6 distortion");
            GUILayout.Label("Z / Я pattern left, X / Ч pattern right in kaleidoscope mode");
            GUILayout.Label("Wheel zoom, RMB orbit, MMB pan");
            GUILayout.EndArea();
        }
    }
}
