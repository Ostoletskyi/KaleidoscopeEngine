using System;
using System.IO;
using KaleidoscopeEngine.Source;
using UnityEditor;
using UnityEngine;

namespace KaleidoscopeEngine.EditorTools
{
    public sealed class KaleidoscopeSourceBrowserWindow : EditorWindow
    {
        private KaleidoscopeSourceCategory selectedCategory = KaleidoscopeSourceCategory.TransparentGemstones;
        private Vector2 scrollPosition;
        private GUIStyle titleStyle;
        private GUIStyle categoryStyle;
        private GUIStyle cardStyle;
        private GUIStyle smallStyle;

        [MenuItem("Window/Kaleidoscope/Source Library")]
        public static void OpenWindow()
        {
            KaleidoscopeSourceBrowserWindow window = GetWindow<KaleidoscopeSourceBrowserWindow>("Source Library");
            window.minSize = new Vector2(560f, 460f);
            window.Show();
            window.Focus();
        }

        private void OnGUI()
        {
            EnsureStyles();

            KaleidoscopeSourceLibrary library = UnityEngine.Object.FindObjectOfType<KaleidoscopeSourceLibrary>();
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("KALEIDOSCOPE SOURCE LIBRARY", titleStyle);
            EditorGUILayout.LabelField("Operator-side source and image management. The Game View stays clean.", smallStyle);
            EditorGUILayout.Space(8f);

            Rect dropArea = EditorGUILayout.GetControlRect(false, 52f);
            DrawDropArea(dropArea, library);
            EditorGUILayout.Space(8f);

            if (library == null)
            {
                EditorGUILayout.HelpBox("No KaleidoscopeSourceLibrary is active. Enter Play Mode in the Physics Sandbox scene, or add the runtime component to a scene object.", MessageType.Info);
                return;
            }

            DrawCategorySelector(library);
            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Current Source", categoryStyle);
                DrawMetric("Category", library.CategoryDisplayName(library.CurrentCategory));
                DrawMetric("Preset", library.CurrentPresetName);
                DrawMetric("Performance Cost", $"{library.CurrentPerformanceCost:P0}");
                DrawMetric("Compatibility", library.CurrentCompatibility);
                EditorGUILayout.LabelField(library.CurrentDescription, smallStyle);
            }

            EditorGUILayout.Space(8f);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawPresetList(library);
            EditorGUILayout.EndScrollView();
        }

        private void DrawCategorySelector(KaleidoscopeSourceLibrary library)
        {
            EditorGUILayout.LabelField("Categories", categoryStyle);
            KaleidoscopeSourceCategory[] categories = (KaleidoscopeSourceCategory[])Enum.GetValues(typeof(KaleidoscopeSourceCategory));
            int columns = 2;
            for (int i = 0; i < categories.Length; i += columns)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns && i + column < categories.Length; column++)
                    {
                        KaleidoscopeSourceCategory category = categories[i + column];
                        bool active = selectedCategory == category;
                        GUIStyle style = active ? categoryStyle : EditorStyles.miniButton;
                        if (GUILayout.Button(library.CategoryDisplayName(category), style, GUILayout.Height(28f)))
                        {
                            selectedCategory = category;
                            library.SetCategory(category);
                            Repaint();
                        }
                    }
                }
            }
        }

        private void DrawPresetList(KaleidoscopeSourceLibrary library)
        {
            EditorGUILayout.LabelField("Presets", categoryStyle);
            var presets = library.GetPresets(selectedCategory);
            if (presets.Count == 0)
            {
                EditorGUILayout.HelpBox("No presets in this category yet.", MessageType.None);
                return;
            }

            for (int i = 0; i < presets.Count; i++)
            {
                KaleidoscopeSourcePreset preset = presets[i];
                using (new EditorGUILayout.HorizontalScope(cardStyle))
                {
                    Rect thumbRect = GUILayoutUtility.GetRect(72f, 72f, GUILayout.Width(72f), GUILayout.Height(72f));
                    DrawThumbnail(thumbRect, preset);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(preset.displayName, categoryStyle);
                        EditorGUILayout.LabelField(preset.description, smallStyle);
                        DrawMetric("Cost", $"{preset.performanceCost:P0}");
                        DrawMetric("Compatibility", preset.compatibility);
                    }

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(92f)))
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Load", GUILayout.Height(28f)))
                        {
                            library.LoadPreset(preset);
                            Repaint();
                        }
                    }
                }
            }
        }

        private void DrawDropArea(Rect area, KaleidoscopeSourceLibrary library)
        {
            EditorGUI.DrawRect(area, new Color(0.08f, 0.11f, 0.13f, 1f));
            GUI.Label(area, "Drag PNG / JPG / TGA here, or use Ctrl+O in Play Mode", categoryStyle);
            EditorGUI.DrawRect(new Rect(area.x, area.yMax - 1f, area.width, 1f), new Color(0.4f, 0.75f, 1f, 0.45f));

            Event evt = Event.current;
            if (!area.Contains(evt.mousePosition))
            {
                return;
            }

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    for (int i = 0; i < DragAndDrop.paths.Length; i++)
                    {
                        TryLoadImage(DragAndDrop.paths[i], library);
                    }
                }

                evt.Use();
            }
        }

        private void TryLoadImage(string path, KaleidoscopeSourceLibrary library)
        {
            if (library == null || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg" && extension != ".tga")
            {
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                return;
            }

            texture.name = Path.GetFileNameWithoutExtension(path);
            library.LoadUserImage(texture, texture.name);
        }

        private void DrawThumbnail(Rect rect, KaleidoscopeSourcePreset preset)
        {
            Texture texture = preset.thumbnail != null ? preset.thumbnail : preset.sourceTexture;
            if (texture != null)
            {
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(rect, ResolveCategoryColor(preset.category));
            }

            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0.4f, 0.75f, 1f, 0.4f));
        }

        private Color ResolveCategoryColor(KaleidoscopeSourceCategory category)
        {
            switch (category)
            {
                case KaleidoscopeSourceCategory.TransparentGemstones:
                    return new Color(0.42f, 0.82f, 1f, 0.35f);
                case KaleidoscopeSourceCategory.ColoredGlass:
                    return new Color(1f, 0.35f, 0.55f, 0.35f);
                case KaleidoscopeSourceCategory.Liquids:
                    return new Color(0.1f, 0.72f, 0.9f, 0.35f);
                case KaleidoscopeSourceCategory.Backgrounds:
                    return new Color(0.18f, 0.2f, 0.25f, 0.8f);
                case KaleidoscopeSourceCategory.UserImages:
                    return new Color(0.8f, 0.86f, 0.92f, 0.35f);
                case KaleidoscopeSourceCategory.ProceduralColorBlobs:
                    return new Color(0.95f, 0.62f, 0.25f, 0.35f);
                case KaleidoscopeSourceCategory.PolygonalGeometry:
                    return new Color(0.5f, 0.95f, 0.65f, 0.35f);
                default:
                    return new Color(0.65f, 0.55f, 1f, 0.35f);
            }
        }

        private void DrawMetric(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(128f));
                EditorGUILayout.LabelField(value ?? "n/a", smallStyle);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fixedHeight = 24f
            };

            categoryStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.72f, 0.9f, 1f) },
                wordWrap = true
            };

            cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 8, 8),
                margin = new RectOffset(0, 0, 0, 8)
            };

            smallStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                normal = { textColor = new Color(0.68f, 0.76f, 0.8f) }
            };
        }
    }
}
