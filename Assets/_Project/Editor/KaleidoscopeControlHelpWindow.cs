using System.Collections.Generic;
using KaleidoscopeEngine.UI;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace KaleidoscopeEngine.EditorTools
{
    public sealed class KaleidoscopeControlHelpWindow : EditorWindow
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

        private static readonly Dictionary<string, HelpRow[]> Sections = new Dictionary<string, HelpRow[]>
        {
            {
                "View Modes",
                new[]
                {
                    new HelpRow("Insert", "Cycle view", "Eyepiece / Raw / Source / Orbit"),
                    new HelpRow("Delete", "Eyepiece home", "Return to the final optical view"),
                    new HelpRow("Home / End", "Center exposure", "Raise or lower the luminous origin"),
                    new HelpRow("Page Up / Down", "Optical density", "Add or reduce mosaic source density")
                }
            },
            {
                "Geometry",
                new[]
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
                }
            },
            {
                "Camera",
                new[]
                {
                    new HelpRow("Left / Right", "Viewer rotation", "Rotate the framed eyepiece"),
                    new HelpRow("Up / Down", "Viewer zoom", "Fill or relax the frame"),
                    new HelpRow("Shift + Left / Right", "Source orbit", "Rotate object chamber sampling"),
                    new HelpRow("Shift + Up / Down", "Source framing", "Move source composition")
                }
            },
            {
                "Debug",
                new[]
                {
                    new HelpRow("F1", "Help reference", "Play Mode toggles runtime overlay; Edit Mode opens this window"),
                    new HelpRow("F2", "Compact debug", "Small technical status panel"),
                    new HelpRow("F3", "Full debug", "Detailed system readout"),
                    new HelpRow("F4", "Hide debug UI", "Clean composition"),
                    new HelpRow("F5", "Reset tuning", "Restore visual defaults"),
                    new HelpRow("F6", "Screenshot", "Capture current eyepiece"),
                    new HelpRow("F7 / F8", "Quality", "Step fidelity down or up"),
                    new HelpRow("Shift + F7 / F8", "Quality extremes", "Jump to Minimal or Extreme")
                }
            }
        };

        private static readonly string[] SectionOrder = { "View Modes", "Geometry", "Camera", "Debug" };

        private Vector2 scrollPosition;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle keyStyle;
        private GUIStyle actionStyle;
        private GUIStyle hintStyle;
        private GUIStyle panelStyle;

        [MenuItem("Window/Kaleidoscope/Control Help")]
        public static void OpenWindow()
        {
            KaleidoscopeControlHelpWindow window = GetWindow<KaleidoscopeControlHelpWindow>("Control Help");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
            window.Focus();
        }

        [Shortcut("Kaleidoscope/Control Help", KeyCode.F1)]
        private static void HandleF1Shortcut()
        {
            if (EditorApplication.isPlaying)
            {
                KaleidoscopeHelpOverlay.ToggleRuntimeOverlay();
                return;
            }

            OpenWindow();
        }

        private void OnGUI()
        {
            EnsureStyles();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space(12f);

            EditorGUILayout.LabelField("KALEIDOSCOPE CONTROL SYSTEM", titleStyle);
            EditorGUILayout.LabelField("Operator reference for the shader-driven eyepiece console.", subtitleStyle);
            EditorGUILayout.Space(10f);

            DrawRuntimeOverlayButton();
            EditorGUILayout.Space(12f);

            for (int i = 0; i < SectionOrder.Length; i++)
            {
                DrawSection(SectionOrder[i]);
                EditorGUILayout.Space(8f);
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox("Window > Kaleidoscope > Operator Panel is reserved for a future tuning console. This task implements the Control Help window only.", MessageType.Info);

            EditorGUILayout.EndScrollView();
        }

        private void DrawRuntimeOverlayButton()
        {
            using (new EditorGUILayout.VerticalScope(panelStyle))
            {
                EditorGUILayout.LabelField("Runtime Help Overlay", sectionStyle);
                EditorGUILayout.LabelField("The Game View remains clean by default. Use this only when you want an in-play reference.", hintStyle);
                EditorGUILayout.Space(4f);

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                {
                    if (GUILayout.Button("Toggle Runtime Help Overlay", GUILayout.Height(28f)))
                    {
                        if (!KaleidoscopeHelpOverlay.ToggleRuntimeOverlay())
                        {
                            Debug.LogWarning("Could not toggle runtime help overlay. Enter Play Mode and ensure PhysicsSandboxBootstrap created the operator console.");
                        }
                    }
                }

                if (!EditorApplication.isPlaying)
                {
                    EditorGUILayout.HelpBox("Enter Play Mode to toggle the runtime overlay. In Edit Mode, F1 opens this help window.", MessageType.None);
                }
            }
        }

        private void DrawSection(string sectionName)
        {
            if (!Sections.TryGetValue(sectionName, out HelpRow[] rows))
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(panelStyle))
            {
                EditorGUILayout.LabelField(sectionName, sectionStyle);
                EditorGUILayout.Space(4f);

                DrawHeaderRow();
                for (int i = 0; i < rows.Length; i++)
                {
                    DrawControlRow(rows[i], i % 2 == 0);
                }
            }
        }

        private void DrawHeaderRow()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.16f, 0.18f, 0.95f));

            float keyWidth = Mathf.Min(150f, rect.width * 0.28f);
            float actionWidth = Mathf.Min(180f, rect.width * 0.32f);
            Rect keyRect = new Rect(rect.x + 8f, rect.y + 2f, keyWidth - 12f, rect.height);
            Rect actionRect = new Rect(rect.x + keyWidth, rect.y + 2f, actionWidth - 8f, rect.height);
            Rect hintRect = new Rect(rect.x + keyWidth + actionWidth, rect.y + 2f, rect.width - keyWidth - actionWidth - 8f, rect.height);

            EditorGUI.LabelField(keyRect, "Key", keyStyle);
            EditorGUI.LabelField(actionRect, "Action", actionStyle);
            EditorGUI.LabelField(hintRect, "Purpose", hintStyle);
        }

        private void DrawControlRow(HelpRow row, bool shaded)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 24f);
            if (shaded)
            {
                EditorGUI.DrawRect(rect, new Color(0.09f, 0.12f, 0.14f, 0.42f));
            }

            float keyWidth = Mathf.Min(150f, rect.width * 0.28f);
            float actionWidth = Mathf.Min(180f, rect.width * 0.32f);
            Rect keyRect = new Rect(rect.x + 8f, rect.y + 3f, keyWidth - 12f, rect.height);
            Rect actionRect = new Rect(rect.x + keyWidth, rect.y + 3f, actionWidth - 8f, rect.height);
            Rect hintRect = new Rect(rect.x + keyWidth + actionWidth, rect.y + 3f, rect.width - keyWidth - actionWidth - 8f, rect.height);

            EditorGUI.LabelField(keyRect, row.Key, keyStyle);
            EditorGUI.LabelField(actionRect, row.Action, actionStyle);
            EditorGUI.LabelField(hintRect, row.Hint, hintStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                fixedHeight = 26f
            };

            subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.62f, 0.72f, 0.78f) }
            };

            sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            keyStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.44f, 0.78f, 1f) }
            };

            actionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };

            hintStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(0.68f, 0.76f, 0.8f) }
            };

            panelStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 10)
            };
        }
    }
}
