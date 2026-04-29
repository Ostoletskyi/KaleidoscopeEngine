using System.Collections.Generic;
using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;

namespace KaleidoscopeEngine.UI
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeHelpOverlay : MonoBehaviour
    {
        private struct HelpRow
        {
            public readonly string Key;
            public readonly string Action;
            public readonly string Hint;

            public HelpRow(string key, string action, string hint)
            {
                Key = key;
                Action = action;
                Hint = hint;
            }
        }

        [Header("References")]
        [SerializeField] private KaleidoscopeRenderPipeline mirrorPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private GemstoneSpawner spawner;

        [Header("Display")]
        [SerializeField] private bool visible;
        [SerializeField, Range(0.05f, 1f)] private float overlayOpacity = 0.84f;
        [SerializeField, Range(0.05f, 1f)] private float backgroundDim = 0.48f;
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private float feedbackDuration = 1.35f;

        private readonly Dictionary<string, List<HelpRow>> sections = new Dictionary<string, List<HelpRow>>();
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle keyStyle;
        private GUIStyle actionStyle;
        private GUIStyle hintStyle;
        private GUIStyle footerStyle;
        private Texture2D pixel;
        private float fade;
        private string feedbackText;
        private float feedbackUntil;

        public bool Visible => visible;

        public void Configure(
            KaleidoscopeRenderPipeline pipeline,
            KaleidoscopeMirrorController controller,
            GemstoneSpawner gemstoneSpawner)
        {
            mirrorPipeline = pipeline;
            mirrorController = controller;
            spawner = gemstoneSpawner;
            BuildSections();
        }

        public void Toggle()
        {
            visible = !visible;
        }

        public void Hide()
        {
            visible = false;
        }

        public void ShowFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            feedbackText = message;
            feedbackUntil = Time.unscaledTime + feedbackDuration;
        }

        private void Awake()
        {
            BuildSections();
        }

        private void Update()
        {
            float target = visible ? 1f : 0f;
            fade = Mathf.MoveTowards(fade, target, Time.unscaledDeltaTime * fadeSpeed);
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (fade > 0.001f)
            {
                DrawHelp();
            }

            DrawFeedback();
        }

        private void BuildSections()
        {
            sections.Clear();
            sections["VIEW MODES"] = new List<HelpRow>
            {
                new HelpRow("Insert", "Cycle view", "Eyepiece / Raw / Source / Orbit"),
                new HelpRow("Delete", "Eyepiece home", "Return to the final optical view"),
                new HelpRow("Home / End", "Center exposure", "Raise or lower the luminous origin"),
                new HelpRow("Page Up / Down", "Optical density", "Add or reduce mosaic source density")
            };

            sections["GEOMETRY"] = new List<HelpRow>
            {
                new HelpRow("Numpad 1", "6 sectors", "60 degree standard prism"),
                new HelpRow("Numpad 2", "8 sectors", "45 degree mirror angle"),
                new HelpRow("Numpad 3", "12 sectors", "30 degree mirror angle"),
                new HelpRow("Numpad 4 / 6", "Mirror rotation", "Turn the polar fold"),
                new HelpRow("Numpad 7", "Asymmetry", "Tiny optical irregularity"),
                new HelpRow("Numpad 8", "Seam blending", "Continuity at wedge joins"),
                new HelpRow("Numpad 9", "Optical mask", "Eyepiece edge"),
                new HelpRow("Numpad + / -", "Rotational drift", "More or less living motion"),
                new HelpRow("Numpad * / /", "Breathing / wobble", "Organic optics toggles"),
                new HelpRow("Numpad 0", "Diffuser", "Backlight object chamber")
            };

            sections["CAMERA"] = new List<HelpRow>
            {
                new HelpRow("Left / Right", "Viewer rotation", "Rotate the framed eyepiece"),
                new HelpRow("Up / Down", "Viewer zoom", "Fill or relax the frame"),
                new HelpRow("Shift + Left / Right", "Source orbit", "Rotate object chamber sampling"),
                new HelpRow("Shift + Up / Down", "Source framing", "Move source composition")
            };

            sections["DEBUG"] = new List<HelpRow>
            {
                new HelpRow("F1", "Help overlay", "Show or hide this operator manual"),
                new HelpRow("F2", "Compact debug", "Small technical status panel"),
                new HelpRow("F3", "Full debug", "Detailed system readout"),
                new HelpRow("F4", "Hide debug UI", "Clean composition"),
                new HelpRow("F5", "Reset tuning", "Restore visual defaults"),
                new HelpRow("F6", "Screenshot", "Capture current eyepiece")
            };
        }

        private void DrawHelp()
        {
            Color previousColor = GUI.color;
            float alpha = fade * overlayOpacity;
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, backgroundDim * fade));

            float width = Mathf.Min(980f, Screen.width - 80f);
            float height = Mathf.Min(680f, Screen.height - 80f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            DrawRect(panel, new Color(0.02f, 0.035f, 0.045f, alpha));
            DrawBorder(panel, new Color(0.46f, 0.82f, 1f, 0.28f * fade), 1f);

            GUILayout.BeginArea(new Rect(panel.x + 32f, panel.y + 24f, panel.width - 64f, panel.height - 48f));
            GUI.color = new Color(0.85f, 0.96f, 1f, fade);
            GUILayout.Label("KALEIDOSCOPE CONTROL SYSTEM", titleStyle);
            GUILayout.Space(8f);
            DrawStatusStrip();
            GUILayout.Space(18f);

            GUILayout.BeginHorizontal();
            DrawSection("VIEW MODES", "MODE");
            GUILayout.Space(18f);
            DrawSection("GEOMETRY", "GEO");
            GUILayout.EndHorizontal();
            GUILayout.Space(14f);
            GUILayout.BeginHorizontal();
            DrawSection("CAMERA", "CAM");
            GUILayout.Space(18f);
            DrawSection("DEBUG", "SYS");
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.Label("Operator console: navigation cluster = composition, numpad = optical geometry, arrows = viewer/source framing.", footerStyle);
            GUILayout.EndArea();

            GUI.color = previousColor;
        }

        private void DrawStatusStrip()
        {
            GUILayout.BeginHorizontal();
            DrawStatusChip("VIEW", mirrorPipeline != null ? mirrorPipeline.ViewMode : "n/a");
            DrawStatusChip("SEG", mirrorController != null ? mirrorController.SegmentCount.ToString() : "n/a");
            DrawStatusChip("DENSITY", mirrorController != null ? mirrorController.OpticalDensity.ToString("F2") : "n/a");
            DrawStatusChip("OBJECTS", spawner != null ? spawner.SpawnedObjects.Count.ToString() : "n/a");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawStatusChip(string label, string value)
        {
            GUILayout.BeginVertical(GUILayout.Width(116f));
            GUILayout.Label(label, hintStyle);
            GUILayout.Label(value, sectionStyle);
            GUILayout.EndVertical();
        }

        private void DrawSection(string sectionName, string icon)
        {
            GUILayout.BeginVertical(GUILayout.Width(430f));
            GUILayout.BeginHorizontal();
            GUILayout.Label(icon, keyStyle, GUILayout.Width(52f));
            GUILayout.Label(sectionName, sectionStyle);
            GUILayout.EndHorizontal();
            DrawLine(new Color(0.46f, 0.82f, 1f, 0.18f * fade));

            if (sections.TryGetValue(sectionName, out List<HelpRow> rows))
            {
                for (int i = 0; i < rows.Count; i++)
                {
                    DrawRow(rows[i]);
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawRow(HelpRow row)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(24f));
            GUILayout.Label(row.Key, keyStyle, GUILayout.Width(104f));
            GUILayout.Label(row.Action, actionStyle, GUILayout.Width(138f));
            GUILayout.Label(row.Hint, hintStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawFeedback()
        {
            float remaining = feedbackUntil - Time.unscaledTime;
            if (remaining <= 0f || string.IsNullOrEmpty(feedbackText))
            {
                return;
            }

            float alpha = Mathf.Clamp01(remaining / Mathf.Max(0.01f, feedbackDuration));
            alpha = Mathf.SmoothStep(0f, 1f, alpha);
            Rect rect = new Rect(Screen.width - 360f, Screen.height * 0.56f, 320f, 48f);
            DrawRect(rect, new Color(0.02f, 0.04f, 0.052f, 0.44f * alpha));
            DrawBorder(rect, new Color(0.55f, 0.88f, 1f, 0.22f * alpha), 1f);

            Color previous = GUI.color;
            GUI.color = new Color(0.86f, 0.96f, 1f, alpha);
            GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, rect.height - 18f), feedbackText, actionStyle);
            GUI.color = previous;
        }

        private void DrawLine(Color color)
        {
            Rect rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            DrawRect(rect, color);
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }

            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.82f, 0.94f, 1f, 0.96f) }
            };

            keyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.58f, 0.88f, 1f, 0.96f) }
            };

            actionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.97f, 1f, 0.96f) }
            };

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.72f, 0.84f, 0.9f, 0.82f) }
            };

            footerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.9f, 1f, 0.72f) }
            };
        }
    }
}
