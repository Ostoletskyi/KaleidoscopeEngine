#if UNITY_EDITOR
using UnityEditor;

namespace LocalAI.LMStudioBridge
{
    public static class LMStudioBridgeSettings
    {
        private const string Prefix = "LocalAI.LMStudioBridge.";

        public static string BaseUrl
        {
            get => EditorPrefs.GetString(Prefix + nameof(BaseUrl), "http://127.0.0.1:7001/v1");
            set => EditorPrefs.SetString(Prefix + nameof(BaseUrl), value);
        }

        public static string Model
        {
            get => EditorPrefs.GetString(Prefix + nameof(Model), "google/gemma-4-31b");
            set => EditorPrefs.SetString(Prefix + nameof(Model), value);
        }

        public static float Temperature
        {
            get => EditorPrefs.GetFloat(Prefix + nameof(Temperature), 0.2f);
            set => EditorPrefs.SetFloat(Prefix + nameof(Temperature), value);
        }

        public static int MaxTokens
        {
            get => EditorPrefs.GetInt(Prefix + nameof(MaxTokens), 4096);
            set => EditorPrefs.SetInt(Prefix + nameof(MaxTokens), value);
        }

        public static int EditorLogTailLines
        {
            get => EditorPrefs.GetInt(Prefix + nameof(EditorLogTailLines), 250);
            set => EditorPrefs.SetInt(Prefix + nameof(EditorLogTailLines), value);
        }
    }
}
#endif
