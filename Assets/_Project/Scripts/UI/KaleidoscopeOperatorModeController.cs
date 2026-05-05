using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;

namespace KaleidoscopeEngine.UI
{
    public enum KaleidoscopeGlobalMode
    {
        Viewer,
        Operator
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeOperatorModeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopeDebugPanel statusPanel;
        [SerializeField] private KaleidoscopeHelpOverlay helpOverlay;
        [SerializeField] private KaleidoscopeGuideOverlay guideOverlay;
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;

        [Header("Mode")]
        [SerializeField] private KaleidoscopeGlobalMode currentMode = KaleidoscopeGlobalMode.Viewer;

        public KaleidoscopeGlobalMode CurrentMode => currentMode;
        public bool ViewerMode => currentMode == KaleidoscopeGlobalMode.Viewer;
        public bool OperatorMode => currentMode == KaleidoscopeGlobalMode.Operator;

        public void Configure(
            KaleidoscopeDebugPanel panel,
            KaleidoscopeHelpOverlay overlay,
            KaleidoscopeGuideOverlay guides,
            KaleidoscopeRenderPipeline pipeline)
        {
            statusPanel = panel;
            helpOverlay = overlay;
            guideOverlay = guides;
            renderPipeline = pipeline;
            ApplyMode(false);
        }

        public void SetViewerMode()
        {
            currentMode = KaleidoscopeGlobalMode.Viewer;
            ApplyMode(true);
        }

        public void SetOperatorMode()
        {
            currentMode = KaleidoscopeGlobalMode.Operator;
            ApplyMode(true);
        }

        public void ToggleMode()
        {
            currentMode = currentMode == KaleidoscopeGlobalMode.Viewer
                ? KaleidoscopeGlobalMode.Operator
                : KaleidoscopeGlobalMode.Viewer;
            ApplyMode(true);
        }

        private void ApplyMode(bool announce)
        {
            if (currentMode == KaleidoscopeGlobalMode.Viewer)
            {
                helpOverlay?.Hide();
                guideOverlay?.SetOperatorMode(false);
                renderPipeline?.ReturnToKaleidoscopeView();
                statusPanel?.Hide();
            }
            else
            {
                guideOverlay?.SetOperatorMode(true);
                statusPanel?.SetGameViewDiagnosticsEnabled(false);
            }

            if (announce)
            {
                statusPanel?.PostOperatorMessage(currentMode == KaleidoscopeGlobalMode.Viewer
                    ? "Viewer Mode"
                    : "Operator Mode");
            }
        }
    }
}
