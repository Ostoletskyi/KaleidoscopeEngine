using System.Collections.Generic;
using System.IO;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.Performance;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;

namespace KaleidoscopeEngine.UI
{
    public enum KaleidoscopeControlZone
    {
        ViewModes,
        Geometry,
        Camera,
        Debug
    }

    public enum KaleidoscopeOperatorAction
    {
        CycleView,
        ReturnEyepiece,
        CenterExposureUp,
        CenterExposureDown,
        DensityUp,
        DensityDown,
        SixSectorMode,
        EightSectorMode,
        TwelveSectorMode,
        MirrorRotateLeft,
        MirrorRotateRight,
        ToggleAsymmetry,
        ToggleSeamBlend,
        ToggleOpticalMask,
        DriftUp,
        DriftDown,
        ToggleBreathing,
        ToggleWobble,
        ToggleDiffuser,
        ViewerRotateLeft,
        ViewerRotateRight,
        ViewerZoomIn,
        ViewerZoomOut,
        SourceOrbitLeft,
        SourceOrbitRight,
        SourceFrameUp,
        SourceFrameDown,
        ToggleHelp,
        CompactDebug,
        FullDebug,
        HideDebug,
        ResetVisualTuning,
        CaptureScreenshot,
        QualityDown,
        QualityUp,
        QualityMinimal,
        QualityExtreme,
        ToggleAdaptiveQuality,
        PerformancePresetDown,
        PerformancePresetUp,
        ForceSafeMode,
        ToggleAutoBalance
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeInputRouter : MonoBehaviour
    {
        private struct Binding
        {
            public readonly KaleidoscopeControlZone Zone;
            public readonly KaleidoscopeOperatorAction Action;
            public readonly KeyCode Key;
            public readonly bool Shift;

            public Binding(KaleidoscopeControlZone zone, KaleidoscopeOperatorAction action, KeyCode key, bool shift = false)
            {
                Zone = zone;
                Action = action;
                Key = key;
                Shift = shift;
            }
        }

        [Header("References")]
        [SerializeField] private KaleidoscopePhysicsChamber chamber;
        [SerializeField] private GemstoneSpawner spawner;
        [SerializeField] private PhysicsSandboxCameraController cameraController;
        [SerializeField] private KaleidoscopeRenderPipeline mirrorPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private KaleidoscopeViewerCameraController viewerCameraController;
        [SerializeField] private KaleidoscopeDebugPanel debugPanel;
        [SerializeField] private KaleidoscopeHelpOverlay helpOverlay;
        [SerializeField] private OpticalSourceChamber opticalSourceChamber;
        [SerializeField] private AdaptiveQualityController adaptiveQualityController;

        [Header("Response")]
        [SerializeField] private int densityCountStep = 12;
        [SerializeField] private float sourceRotationDegreesPerSecond = 36f;
        [SerializeField] private float sourceFramingUnitsPerSecond = 0.35f;

        private readonly List<Binding> bindings = new List<Binding>();
        private float nextHeldFeedbackTime;

        public void Configure(
            KaleidoscopePhysicsChamber physicsChamber,
            GemstoneSpawner gemstoneSpawner,
            PhysicsSandboxCameraController sandboxCamera,
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopeMirrorController mirror,
            KaleidoscopeViewerCameraController viewerCamera,
            KaleidoscopeDebugPanel panel,
            KaleidoscopeHelpOverlay overlay,
            OpticalSourceChamber sourceChamber,
            AdaptiveQualityController adaptiveController = null)
        {
            chamber = physicsChamber;
            spawner = gemstoneSpawner;
            cameraController = sandboxCamera;
            mirrorPipeline = pipeline;
            mirrorController = mirror;
            viewerCameraController = viewerCamera;
            debugPanel = panel;
            helpOverlay = overlay;
            opticalSourceChamber = sourceChamber;
            adaptiveQualityController = adaptiveController;
            BuildBindings();
        }

        public void ConfigureAdaptiveQuality(AdaptiveQualityController adaptiveController)
        {
            adaptiveQualityController = adaptiveController;
        }

        private void Awake()
        {
            BuildBindings();
        }

        private void Update()
        {
            HandleViewModeZone();
            HandleGeometryZone();
            HandleCameraZone();
            HandleDebugZone();
        }

        private void BuildBindings()
        {
            bindings.Clear();
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.CycleView, KeyCode.Insert);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.ReturnEyepiece, KeyCode.Delete);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.CenterExposureUp, KeyCode.Home);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.CenterExposureDown, KeyCode.End);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.DensityUp, KeyCode.PageUp);
            Add(KaleidoscopeControlZone.ViewModes, KaleidoscopeOperatorAction.DensityDown, KeyCode.PageDown);

            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.SixSectorMode, KeyCode.Keypad1);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.EightSectorMode, KeyCode.Keypad2);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.TwelveSectorMode, KeyCode.Keypad3);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.MirrorRotateLeft, KeyCode.Keypad4);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.MirrorRotateRight, KeyCode.Keypad6);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleAsymmetry, KeyCode.Keypad7);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleSeamBlend, KeyCode.Keypad8);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleOpticalMask, KeyCode.Keypad9);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.DriftUp, KeyCode.KeypadPlus);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.DriftDown, KeyCode.KeypadMinus);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleBreathing, KeyCode.KeypadMultiply);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleWobble, KeyCode.KeypadDivide);
            Add(KaleidoscopeControlZone.Geometry, KaleidoscopeOperatorAction.ToggleDiffuser, KeyCode.Keypad0);

            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.ViewerRotateLeft, KeyCode.LeftArrow);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.ViewerRotateRight, KeyCode.RightArrow);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.ViewerZoomIn, KeyCode.UpArrow);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.ViewerZoomOut, KeyCode.DownArrow);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.SourceOrbitLeft, KeyCode.LeftArrow, true);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.SourceOrbitRight, KeyCode.RightArrow, true);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.SourceFrameUp, KeyCode.UpArrow, true);
            Add(KaleidoscopeControlZone.Camera, KaleidoscopeOperatorAction.SourceFrameDown, KeyCode.DownArrow, true);

            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ToggleHelp, KeyCode.F1);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.CompactDebug, KeyCode.F2);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.FullDebug, KeyCode.F3);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.HideDebug, KeyCode.F4);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ResetVisualTuning, KeyCode.F5);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.CaptureScreenshot, KeyCode.F6);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.QualityDown, KeyCode.F7);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.QualityUp, KeyCode.F8);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.QualityMinimal, KeyCode.F7, true);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.QualityExtreme, KeyCode.F8, true);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ToggleAdaptiveQuality, KeyCode.F9);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ToggleAutoBalance, KeyCode.F9, true);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.PerformancePresetDown, KeyCode.F10);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.PerformancePresetUp, KeyCode.F11);
            Add(KaleidoscopeControlZone.Debug, KaleidoscopeOperatorAction.ForceSafeMode, KeyCode.F12);
        }

        private void Add(KaleidoscopeControlZone zone, KaleidoscopeOperatorAction action, KeyCode key, bool shift = false)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                Binding existing = bindings[i];
                if (existing.Key == key && existing.Shift == shift)
                {
                    Debug.LogWarning($"Duplicate kaleidoscope binding ignored: {key} for {action}.", this);
                    return;
                }
            }

            bindings.Add(new Binding(zone, action, key, shift));
        }

        private void HandleViewModeZone()
        {
            if (Pressed(KeyCode.Insert))
            {
                mirrorPipeline?.CycleViewMode();
                if (mirrorPipeline != null && mirrorPipeline.CurrentViewMode == KaleidoscopeViewMode.DebugOrbit)
                {
                    cameraController?.SetDebugOrbitView();
                }

                Feedback($"View: {(mirrorPipeline != null ? mirrorPipeline.ViewMode : "Cycle")}");
            }

            if (Pressed(KeyCode.Delete))
            {
                mirrorPipeline?.ReturnToKaleidoscopeView();
                cameraController?.ResetToFrontView();
                Feedback("Eyepiece View");
            }

            if (Held(KeyCode.Home))
            {
                mirrorController?.AdjustCenterExposure(1f);
                HeldFeedback("Center Exposure +");
            }

            if (Held(KeyCode.End))
            {
                mirrorController?.AdjustCenterExposure(-1f);
                HeldFeedback("Center Exposure -");
            }

            if (Pressed(KeyCode.PageUp))
            {
                spawner?.AdjustOpticalDensity(densityCountStep);
                mirrorController?.AdjustOpticalDensity(1f);
                Feedback("Optical Density +");
            }

            if (Pressed(KeyCode.PageDown))
            {
                spawner?.AdjustOpticalDensity(-densityCountStep);
                mirrorController?.AdjustOpticalDensity(-1f);
                Feedback("Optical Density -");
            }
        }

        private void HandleGeometryZone()
        {
            if (Pressed(KeyCode.Keypad1))
            {
                mirrorController?.SetStandardPrism60();
                Feedback("6 Sector Mode");
            }

            if (Pressed(KeyCode.Keypad2))
            {
                mirrorController?.SetMirrorAngle45();
                Feedback("8 Sector Mode");
            }

            if (Pressed(KeyCode.Keypad3))
            {
                mirrorController?.SetMirrorAngle30();
                Feedback("12 Sector Mode");
            }

            if (Held(KeyCode.Keypad4))
            {
                mirrorController?.RotatePattern(-1f);
                HeldFeedback("Mirror Rotation -");
            }

            if (Held(KeyCode.Keypad6))
            {
                mirrorController?.RotatePattern(1f);
                HeldFeedback("Mirror Rotation +");
            }

            if (Pressed(KeyCode.Keypad7))
            {
                mirrorController?.ToggleAsymmetry();
                Feedback(mirrorController != null && mirrorController.AsymmetryEnabled ? "Asymmetry Enabled" : "Asymmetry Disabled");
            }

            if (Pressed(KeyCode.Keypad8))
            {
                mirrorController?.ToggleSeamBlending();
                Feedback(mirrorController != null && mirrorController.SeamBlendingEnabled ? "Seam Blend Enabled" : "Seam Blend Disabled");
            }

            if (Pressed(KeyCode.Keypad9))
            {
                mirrorController?.ToggleOpticalMask();
                Feedback($"Optical Mask: {(mirrorController != null ? mirrorController.MaskModeName : "Toggle")}");
            }

            if (Pressed(KeyCode.KeypadPlus))
            {
                mirrorController?.AdjustRotationalDrift(1f);
                Feedback("Rotational Drift +");
            }

            if (Pressed(KeyCode.KeypadMinus))
            {
                mirrorController?.AdjustRotationalDrift(-1f);
                Feedback("Rotational Drift -");
            }

            if (Pressed(KeyCode.KeypadMultiply))
            {
                mirrorController?.ToggleBreathing();
                Feedback(mirrorController != null && mirrorController.BreathingEnabled ? "Breathing Enabled" : "Breathing Disabled");
            }

            if (Pressed(KeyCode.KeypadDivide))
            {
                mirrorController?.ToggleWobble();
                Feedback(mirrorController != null && mirrorController.WobbleEnabled ? "Wobble Enabled" : "Wobble Disabled");
            }

            if (Pressed(KeyCode.Keypad0))
            {
                opticalSourceChamber?.ToggleDiffuserModule();
                Feedback(opticalSourceChamber != null && opticalSourceChamber.DiffuserEnabled ? "Diffuser Enabled" : "Diffuser Disabled");
            }
        }

        private void HandleCameraZone()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shift)
            {
                if (Held(KeyCode.LeftArrow))
                {
                    mirrorPipeline?.AdjustSourceOrbit(-sourceRotationDegreesPerSecond * Time.deltaTime);
                    HeldFeedback("Source Orbit -");
                }

                if (Held(KeyCode.RightArrow))
                {
                    mirrorPipeline?.AdjustSourceOrbit(sourceRotationDegreesPerSecond * Time.deltaTime);
                    HeldFeedback("Source Orbit +");
                }

                if (Held(KeyCode.UpArrow))
                {
                    mirrorPipeline?.AdjustSourceFraming(sourceFramingUnitsPerSecond * Time.deltaTime);
                    HeldFeedback("Source Framing +");
                }

                if (Held(KeyCode.DownArrow))
                {
                    mirrorPipeline?.AdjustSourceFraming(-sourceFramingUnitsPerSecond * Time.deltaTime);
                    HeldFeedback("Source Framing -");
                }

                return;
            }

            if (Held(KeyCode.LeftArrow))
            {
                viewerCameraController?.AdjustFramingRotation(-1f);
                HeldFeedback("Viewer Rotation -");
            }

            if (Held(KeyCode.RightArrow))
            {
                viewerCameraController?.AdjustFramingRotation(1f);
                HeldFeedback("Viewer Rotation +");
            }

            if (Held(KeyCode.UpArrow))
            {
                viewerCameraController?.AdjustViewerZoom(1f);
                HeldFeedback("Viewer Zoom +");
            }

            if (Held(KeyCode.DownArrow))
            {
                viewerCameraController?.AdjustViewerZoom(-1f);
                HeldFeedback("Viewer Zoom -");
            }
        }

        private void HandleDebugZone()
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Pressed(KeyCode.F1))
            {
                KaleidoscopeHelpOverlay.ToggleRuntimeOverlay(helpOverlay);
            }

            if (Pressed(KeyCode.F2))
            {
                debugPanel?.SetCompactMode();
                Feedback("Compact Debug");
            }

            if (Pressed(KeyCode.F3))
            {
                debugPanel?.SetFullMode();
                Feedback("Full Debug");
            }

            if (Pressed(KeyCode.F4))
            {
                debugPanel?.Hide();
                helpOverlay?.Hide();
                Feedback("Debug Hidden");
            }

            if (Pressed(KeyCode.F5))
            {
                mirrorController?.ResetVisualTuningDefaults();
                spawner?.ResetMosaicDefaults(true);
                viewerCameraController?.ResetViewerComposition();
                Feedback("Visual Tuning Reset");
            }

            if (Pressed(KeyCode.F6))
            {
                CaptureScreenshot();
            }

            if (Pressed(KeyCode.F7))
            {
                if (shift)
                {
                    mirrorPipeline?.SetMinimumQuality();
                    Feedback("Quality: Minimal");
                }
                else
                {
                    mirrorPipeline?.AdjustQualityLevel(-1);
                    Feedback($"Quality: {(mirrorPipeline != null ? mirrorPipeline.QualityPresetName : "Down")}");
                }
            }

            if (Pressed(KeyCode.F8))
            {
                if (shift)
                {
                    mirrorPipeline?.SetMaximumQuality();
                    Feedback("Quality: Extreme");
                }
                else
                {
                    mirrorPipeline?.AdjustQualityLevel(1);
                    Feedback($"Quality: {(mirrorPipeline != null ? mirrorPipeline.QualityPresetName : "Up")}");
                }
            }

            if (Pressed(KeyCode.F9))
            {
                if (shift)
                {
                    adaptiveQualityController?.ToggleAutoBalance();
                    Feedback(adaptiveQualityController != null && adaptiveQualityController.AutoBalanceEnabled ? "Auto-Balance Enabled" : "Auto-Balance Disabled");
                }
                else
                {
                    adaptiveQualityController?.ToggleAdaptiveQuality();
                    Feedback(adaptiveQualityController != null && adaptiveQualityController.AdaptiveQualityEnabled ? "Adaptive Quality Enabled" : "Adaptive Quality Disabled");
                }
            }

            if (Pressed(KeyCode.F10))
            {
                adaptiveQualityController?.PerformancePresetDown();
                Feedback("Performance Preset Down");
            }

            if (Pressed(KeyCode.F11))
            {
                adaptiveQualityController?.PerformancePresetUp();
                Feedback("Performance Preset Up");
            }

            if (Pressed(KeyCode.F12))
            {
                adaptiveQualityController?.ForceSafeMode();
                Feedback("Emergency Safe Mode");
            }
        }

        private bool Pressed(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        private bool Held(KeyCode key)
        {
            return Input.GetKey(key);
        }

        private void Feedback(string message)
        {
            helpOverlay?.ShowFeedback(message);
        }

        private void HeldFeedback(string message)
        {
            if (Time.unscaledTime < nextHeldFeedbackTime)
            {
                return;
            }

            nextHeldFeedbackTime = Time.unscaledTime + 0.32f;
            Feedback(message);
        }

        private void CaptureScreenshot()
        {
            string directory = Path.Combine(Application.persistentDataPath, "KaleidoscopeCaptures");
            Directory.CreateDirectory(directory);
            string fileName = $"kaleidoscope_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            string path = Path.Combine(directory, fileName);
            ScreenCapture.CaptureScreenshot(path, 2);
            Feedback($"Screenshot: {fileName}");
        }
    }
}
