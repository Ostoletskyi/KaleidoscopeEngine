#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LocalAI.LMStudioBridge
{
    public sealed class LMStudioAssistantWindow : EditorWindow
    {
        private Vector2 scroll;
        private string taskPrompt = "Проверь выбранные файлы Unity. Найди root cause проблем, предложи минимальный патч и шаги проверки.";
        private string systemPrompt = "Ты senior Unity 2022.3 LTS engineer. Отвечай структурно: Суть → Причина → Исправление → Проверка → Риски. Не выдумывай файлы и API. Если контекста мало, явно скажи, чего не хватает.";
        private string contextPreview = string.Empty;
        private string answer = string.Empty;
        private bool includeEditorLogTail = true;
        private bool isBusy;

        [MenuItem("Tools/Local AI/LM Studio Assistant")]
        public static void Open()
        {
            var window = GetWindow<LMStudioAssistantWindow>("LM Studio Assistant");
            window.minSize = new Vector2(760, 620);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("LM Studio Local Assistant", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                LMStudioBridgeSettings.BaseUrl = EditorGUILayout.TextField("Base URL", LMStudioBridgeSettings.BaseUrl);
                LMStudioBridgeSettings.Model = EditorGUILayout.TextField("Model", LMStudioBridgeSettings.Model);
                LMStudioBridgeSettings.Temperature = EditorGUILayout.Slider("Temperature", LMStudioBridgeSettings.Temperature, 0f, 2f);
                LMStudioBridgeSettings.MaxTokens = EditorGUILayout.IntSlider("Max Tokens", LMStudioBridgeSettings.MaxTokens, 512, 32768);
                includeEditorLogTail = EditorGUILayout.Toggle("Include Editor.log tail", includeEditorLogTail);
                if (includeEditorLogTail)
                    LMStudioBridgeSettings.EditorLogTailLines = EditorGUILayout.IntSlider("Log tail lines", LMStudioBridgeSettings.EditorLogTailLines, 50, 1500);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("System Prompt");
            systemPrompt = EditorGUILayout.TextArea(systemPrompt, GUILayout.MinHeight(54));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Task Prompt");
            taskPrompt = EditorGUILayout.TextArea(taskPrompt, GUILayout.MinHeight(76));

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = !isBusy;
                if (GUILayout.Button("Build Context From Selection", GUILayout.Height(28)))
                {
                    contextPreview = LMStudioContextBuilder.BuildFromSelection(includeEditorLogTail);
                }

                if (GUILayout.Button("Send To LM Studio", GUILayout.Height(28)))
                {
                    Send();
                }

                if (GUILayout.Button("Copy Answer", GUILayout.Height(28)))
                {
                    EditorGUIUtility.systemCopyBuffer = answer;
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(6);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Context Preview", EditorStyles.boldLabel);
            contextPreview = EditorGUILayout.TextArea(contextPreview, GUILayout.MinHeight(180));

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Answer", EditorStyles.boldLabel);
            answer = EditorGUILayout.TextArea(answer, GUILayout.MinHeight(220));

            EditorGUILayout.EndScrollView();
        }

        private void Send()
        {
            if (string.IsNullOrWhiteSpace(contextPreview))
                contextPreview = LMStudioContextBuilder.BuildFromSelection(includeEditorLogTail);

            string userPrompt = taskPrompt + "\n\n" + contextPreview;
            answer = "Waiting for LM Studio...";
            isBusy = true;
            EditorUtility.DisplayProgressBar("LM Studio", "Waiting for local model response...", 0.35f);

            LMStudioBridgeClient.SendChat(
                systemPrompt,
                userPrompt,
                result =>
                {
                    answer = result;
                    isBusy = false;
                    Repaint();
                },
                error =>
                {
                    answer = "ERROR:\n" + error;
                    isBusy = false;
                    Repaint();
                });
        }
    }
}
#endif
