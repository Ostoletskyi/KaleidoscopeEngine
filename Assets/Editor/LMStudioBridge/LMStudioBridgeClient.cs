#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace LocalAI.LMStudioBridge
{
    internal static class LMStudioBridgeClient
    {
        public static void SendChat(string systemPrompt, string userPrompt, Action<string> onDone, Action<string> onError)
        {
            string baseUrl = NormalizeBaseUrl(LMStudioBridgeSettings.BaseUrl);
            string url = baseUrl + "/chat/completions";
            string body = LMStudioBridgeJson.BuildChatRequest(
                LMStudioBridgeSettings.Model,
                systemPrompt,
                userPrompt,
                LMStudioBridgeSettings.Temperature,
                LMStudioBridgeSettings.MaxTokens
            );

            var request = new UnityWebRequest(url, "POST");
            byte[] payload = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(payload);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer lm-studio");
            request.timeout = 300;

            UnityWebRequestAsyncOperation op;
            try
            {
                op = request.SendWebRequest();
            }
            catch (Exception ex)
            {
                request.Dispose();
                onError?.Invoke(ex.Message);
                return;
            }

            op.completed += _ =>
            {
                try
                {
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onError?.Invoke($"HTTP error: {request.responseCode}\n{request.error}\n{request.downloadHandler.text}");
                        return;
                    }

                    onDone?.Invoke(LMStudioBridgeJson.ExtractAssistantText(request.downloadHandler.text));
                }
                finally
                {
                    request.Dispose();
                    EditorUtility.ClearProgressBar();
                }
            };
        }

        private static string NormalizeBaseUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "http://127.0.0.1:1234/v1";
            value = value.Trim().TrimEnd('/');
            if (!value.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) value += "/v1";
            return value;
        }
    }
}
#endif
