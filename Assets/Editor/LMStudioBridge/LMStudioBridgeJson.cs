#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LocalAI.LMStudioBridge
{
    [Serializable]
    internal sealed class LMStudioChatResponse
    {
        public LMStudioChoice[] choices;
        public LMStudioError error;
    }

    [Serializable]
    internal sealed class LMStudioChoice
    {
        public LMStudioMessage message;
    }

    [Serializable]
    internal sealed class LMStudioMessage
    {
        public string role;
        public string content;
    }

    [Serializable]
    internal sealed class LMStudioError
    {
        public string message;
        public string type;
    }

    internal static class LMStudioBridgeJson
    {
        public static string BuildChatRequest(string model, string system, string user, float temperature, int maxTokens)
        {
            var sb = new StringBuilder(8192);
            sb.Append('{');
            AppendProp(sb, "model", model); sb.Append(',');
            sb.Append("\"messages\":[");
            AppendMessage(sb, "system", system); sb.Append(',');
            AppendMessage(sb, "user", user);
            sb.Append("],");
            sb.Append("\"temperature\":").Append(temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(',');
            sb.Append("\"max_tokens\":").Append(Math.Max(1, maxTokens));
            sb.Append('}');
            return sb.ToString();
        }

        public static string ExtractAssistantText(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            try
            {
                var parsed = JsonUtility.FromJson<LMStudioChatResponse>(json);
                if (parsed != null)
                {
                    if (parsed.error != null && !string.IsNullOrEmpty(parsed.error.message))
                        return "LM Studio error: " + parsed.error.message;

                    if (parsed.choices != null && parsed.choices.Length > 0 && parsed.choices[0].message != null)
                        return parsed.choices[0].message.content ?? string.Empty;
                }
            }
            catch { }

            return json;
        }

        private static void AppendMessage(StringBuilder sb, string role, string content)
        {
            sb.Append('{');
            AppendProp(sb, "role", role); sb.Append(',');
            AppendProp(sb, "content", content);
            sb.Append('}');
        }

        private static void AppendProp(StringBuilder sb, string name, string value)
        {
            sb.Append('"').Append(Escape(name)).Append("\":\"").Append(Escape(value ?? string.Empty)).Append('"');
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length + 32);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
#endif
