using System;
using System.Collections.Generic;
using UnityEngine;

namespace KaleidoscopeEngine.UI
{
    public enum KaleidoscopeInputContext
    {
        RuntimeGameView,
        EditorWindow,
        DebugOnly,
        ViewerMode,
        OperatorMode
    }

    [Serializable]
    public sealed class KaleidoscopeInputBinding
    {
        public string actionId;
        public string displayName;
        public string defaultBinding;
        public string alternateBinding;
        public KaleidoscopeInputContext context;
        public string conflictWarning;

        public KaleidoscopeInputBinding(string actionId, string displayName, string defaultBinding, string alternateBinding, KaleidoscopeInputContext context)
        {
            this.actionId = actionId;
            this.displayName = displayName;
            this.defaultBinding = defaultBinding;
            this.alternateBinding = alternateBinding;
            this.context = context;
        }
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeInputBindingRegistry : MonoBehaviour
    {
        [SerializeField] private List<KaleidoscopeInputBinding> bindings = new List<KaleidoscopeInputBinding>();
        private readonly List<string> conflictWarnings = new List<string>();

        public IReadOnlyList<KaleidoscopeInputBinding> Bindings => bindings;
        public IReadOnlyList<string> ConflictWarnings => conflictWarnings;

        public void InitializeDefaults()
        {
            if (bindings.Count > 0)
            {
                DetectUnityShortcutConflicts();
                return;
            }

            Add("source.gemstones", "Gemstones / physical source", "Alt+1", "Numpad 1", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.glass", "Colored glass physical source", "Alt+2", "Numpad 2", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.image", "Image / wallpaper source", "Alt+3", "Source Library button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.blobs", "Procedural color blobs", "Alt+4", "Source Library button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.polygons", "Polygon geometry source", "Alt+5", "Source Library button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.liquid", "Liquid illusion source", "Alt+6", "Source Library button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.hybrid", "Hybrid source", "Alt+7", "Source Library button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.experimental", "Experimental source", "Alt+8", "Source Library button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.prev", "Previous source preset", "Alt+Left", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.next", "Next source preset", "Alt+Right", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.random", "Randomize current source", "Alt+R", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("source.reset", "Reset current source", "Alt+Backspace", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("image.colorDepthPrev", "Previous color depth", "<", "Comma key", KaleidoscopeInputContext.RuntimeGameView);
            Add("image.colorDepthNext", "Next color depth", ">", "Period key", KaleidoscopeInputContext.RuntimeGameView);
            Add("image.autoQuality", "Auto visual quality", "Ctrl+F", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("launcher.toggle", "Toggle launcher menu", "Middle Mouse", "Launcher button", KaleidoscopeInputContext.RuntimeGameView);
            Add("audio.reactiveToggle", "Toggle audio reactive director", "Ctrl+M", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("audio.beatDebug", "Toggle beat debug overlay", "Ctrl+B", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("audio.resync", "Resync audio director", "Ctrl+R", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("scenario.autoToggle", "Toggle auto scenario mode", "Ctrl+A", "Shift+F10", KaleidoscopeInputContext.RuntimeGameView);
            Add("scenario.toggle", "Toggle scenario orchestrator", "Shift+F10", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("scenario.next", "Next scenario preset", "Shift+F11", "Operator Console button", KaleidoscopeInputContext.RuntimeGameView);
            Add("console.operator", "Operator Mode", "Shift+F2", "Operator Console button", KaleidoscopeInputContext.OperatorMode);
            Add("console.viewer", "Viewer Mode", "Shift+F1", "Operator Console button", KaleidoscopeInputContext.ViewerMode);
            DetectUnityShortcutConflicts();
        }

        public void DetectUnityShortcutConflicts()
        {
            conflictWarnings.Clear();
            for (int i = 0; i < bindings.Count; i++)
            {
                KaleidoscopeInputBinding binding = bindings[i];
                binding.conflictWarning = string.Empty;
                if (IsReservedUnityShortcut(binding.defaultBinding))
                {
                    binding.conflictWarning = $"Default binding {binding.defaultBinding} may conflict with Unity Editor shortcuts.";
                    conflictWarnings.Add($"{binding.displayName}: {binding.conflictWarning}");
                }
            }
        }

        private void Add(string actionId, string displayName, string defaultBinding, string alternateBinding, KaleidoscopeInputContext context)
        {
            bindings.Add(new KaleidoscopeInputBinding(actionId, displayName, defaultBinding, alternateBinding, context));
        }

        private static bool IsReservedUnityShortcut(string binding)
        {
            if (string.IsNullOrWhiteSpace(binding))
            {
                return false;
            }

            string normalized = binding.Replace(" ", string.Empty).ToUpperInvariant();
            return normalized == "CTRL+S" ||
                   normalized == "CTRL+O" ||
                   normalized == "CTRL+Z" ||
                   normalized == "CTRL+Y" ||
                   normalized == "CTRL+D" ||
                   normalized == "CTRL+P" ||
                   normalized == "CTRL+C" ||
                   normalized == "CTRL+V" ||
                   normalized == "CTRL+X" ||
                   normalized == "CTRL+A";
        }
    }
}
