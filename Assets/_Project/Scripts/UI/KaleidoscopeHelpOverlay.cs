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
        [SerializeField] private KaleidoscopeDebugPanel statusPanel;

        [Header("Display")]
        [SerializeField] private bool visible;
        [SerializeField, Range(0.05f, 1f)] private float overlayOpacity = 0.84f;
        [SerializeField, Range(0.05f, 1f)] private float backgroundDim = 0.48f;
        [SerializeField] private float fadeSpeed = 8f;

        private readonly Dictionary<string, List<HelpRow>> sections = new Dictionary<string, List<HelpRow>>();
        private readonly string[] sectionOrder =
        {
            "ИСТОЧНИКИ",
            "НАПРАВЛЯЮЩИЕ",
            "ОКУЛЯР И ВРАЩЕНИЕ",
            "ГЕОМЕТРИЯ",
            "ДИАГНОСТИКА",
            "КАЧЕСТВО И FPS"
        };
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle keyStyle;
        private GUIStyle actionStyle;
        private GUIStyle hintStyle;
        private GUIStyle footerStyle;
        private Texture2D pixel;
        private float fade;
        private const float RuntimeToggleDebounceSeconds = 0.08f;
        private static int lastRuntimeToggleFrame = -1;
        private static float lastRuntimeToggleRealtime = -100f;

        public bool Visible => visible;

        public static bool ToggleRuntimeOverlay(KaleidoscopeHelpOverlay preferredOverlay = null)
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            int currentFrame = Time.frameCount;
            float currentRealtime = Time.realtimeSinceStartup;
            if (currentFrame == lastRuntimeToggleFrame || currentRealtime - lastRuntimeToggleRealtime < RuntimeToggleDebounceSeconds)
            {
                return true;
            }

            KaleidoscopeHelpOverlay overlay = preferredOverlay != null
                ? preferredOverlay
                : FindObjectOfType<KaleidoscopeHelpOverlay>();

            if (overlay == null)
            {
                Debug.LogWarning("Runtime help overlay requested, but no KaleidoscopeHelpOverlay exists in the active scene.");
                return false;
            }

            lastRuntimeToggleFrame = currentFrame;
            lastRuntimeToggleRealtime = currentRealtime;
            overlay.Toggle();
            return true;
        }

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

        public void ConfigureStatusPanel(KaleidoscopeDebugPanel panel)
        {
            statusPanel = panel;
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

            if (statusPanel == null)
            {
                statusPanel = FindObjectOfType<KaleidoscopeDebugPanel>();
            }

            statusPanel?.PostOperatorMessage(message);
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

        }

        private void BuildSections()
        {
            sections.Clear();
            sections["ИСТОЧНИКИ"] = new List<HelpRow>
            {
                new HelpRow("Alt + 1", "Кристаллы", "Физическая камера с прозрачными камнями"),
                new HelpRow("Alt + 2", "Цветное стекло", "Физические цветные осколки"),
                new HelpRow("Alt + 3", "Иллюстрации", "Пользовательская картинка / обои"),
                new HelpRow("Alt + O", "Открыть картинку", "Загрузить изображение с компьютера"),
                new HelpRow("Alt + 4", "Цветные пятна", "Процедурный источник без физики"),
                new HelpRow("Alt + 5", "Геометрия", "Многоугольники и абстрактные формы"),
                new HelpRow("Alt + 6", "Жидкости", "Иллюзия масла / воды / ртути"),
                new HelpRow("Alt + 7", "Hybrid", "Комбинированный режим"),
                new HelpRow("Alt + 8", "Experimental", "Экспериментальные источники"),
                new HelpRow("Alt + Left / Right", "Пресеты", "Предыдущий / следующий источник"),
                new HelpRow("Alt + R", "Случайно", "Рандомизировать текущий источник"),
                new HelpRow("Alt + Backspace", "Сброс", "Сбросить текущий источник")
            };

            sections["НАПРАВЛЯЮЩИЕ"] = new List<HelpRow>
            {
                new HelpRow("Ctrl + 1", "Сектора зеркал", "Показать границы клиньев"),
                new HelpRow("Ctrl + 2", "Покрытие источника", "Где источник попадает в отражения"),
                new HelpRow("Ctrl + 3", "Передача в зеркало", "Область, которая уходит в зеркальный шейдер"),
                new HelpRow("Ctrl + 4", "Схождение", "Оптические линии схождения"),
                new HelpRow("Ctrl + 5", "Центр", "Контроль центральной композиции"),
                new HelpRow("Ctrl + 6", "Безопасные зоны", "Комфортная зона просмотра"),
                new HelpRow("Ctrl + 7", "Плотность", "Тепловая карта плотности источника"),
                new HelpRow("Ctrl + 8", "RT preview", "Просмотр RenderTexture"),
                new HelpRow("Ctrl + 9", "Поток", "Направление оптического движения"),
                new HelpRow("Ctrl + 0", "Скрыть", "Убрать все направляющие")
            };

            sections["ОКУЛЯР И ВРАЩЕНИЕ"] = new List<HelpRow>
            {
                new HelpRow("Insert", "Сменить вид", "Kaleidoscope / Raw / Source / Orbit"),
                new HelpRow("Delete", "Окуляр", "Вернуться к финальной картинке"),
                new HelpRow("Left / Right", "Разгон вращения", "Удерживать: плавно менять скорость от -1000 до +1000"),
                new HelpRow("Up / Down", "Масштаб", "Приблизить или отдалить изображение"),
                new HelpRow("< / >", "Глубина цвета", "Предыдущий / следующий режим палитры"),
                new HelpRow("Ctrl + F", "Автокачество", "Вернуть premium look без пересборки сцены"),
                new HelpRow("Ctrl + M", "Музыка", "Включить или выключить audio-reactive director"),
                new HelpRow("Ctrl + B", "Биты", "Показать отладку beat detector"),
                new HelpRow("Ctrl + R", "Resync", "Синхронизировать audio director заново"),
                new HelpRow("Space", "Встряхнуть", "Физический импульс или UV-встряска"),
                new HelpRow("Home / End", "Экспозиция центра", "Светлее / темнее центральная зона"),
                new HelpRow("Shift + Arrows", "Источник / труба", "Тонкая настройка source framing и физической трубы"),
                new HelpRow("Shift + F1", "Viewer Mode", "Чистый режим просмотра"),
                new HelpRow("Shift + F2", "Operator Mode", "Пульт оператора и диагностика")
            };

            sections["ГЕОМЕТРИЯ"] = new List<HelpRow>
            {
                new HelpRow("1 / 2 / 3", "Пресеты сегментов", "6 / 12 / 24 сектора"),
                new HelpRow("Numpad 1", "6 секторов", "60 градусов, классический призматический режим"),
                new HelpRow("Numpad 2", "12 секторов", "Более дробная симметрия"),
                new HelpRow("Numpad 3", "24 сектора", "Плотный орнамент"),
                new HelpRow("Numpad + / -", "Сегменты ±2", "Дробление картинки от 6 до 48"),
                new HelpRow("Numpad 4 / 6", "Повернуть узор", "Ручной поворот полярной схемы"),
                new HelpRow("Numpad 5", "Стоп", "Остановить вращение"),
                new HelpRow("Numpad Enter", "По умолчанию", "Вернуть комфортное движение"),
                new HelpRow("Numpad 7", "Асимметрия", "Небольшая органическая неровность"),
                new HelpRow("Numpad 8", "Стыки", "Сглаживание границ секторов"),
                new HelpRow("Numpad 9", "Маска", "Край окуляра"),
                new HelpRow("Numpad * / /", "Дыхание / wobble", "Органическое движение оптики")
            };

            sections["ДИАГНОСТИКА"] = new List<HelpRow>
            {
                new HelpRow("F1", "Помощь", "Показать или скрыть эту таблицу"),
                new HelpRow("Middle Mouse", "Меню", "Открыть или закрыть launcher"),
                new HelpRow("F2 / F3", "Пульт оператора", "Открыть диагностику и производительность"),
                new HelpRow("F4", "Чистая картинка", "Скрыть помощь, направляющие и UI"),
                new HelpRow("F5", "Сброс красоты", "Вернуть визуальные настройки"),
                new HelpRow("F6", "Скриншот", "Сохранить текущий кадр"),
                new HelpRow("F12", "Safe Mode", "Аварийное восстановление")
            };

            sections["КАЧЕСТВО И FPS"] = new List<HelpRow>
            {
                new HelpRow("PageUp / PageDown", "Качество рендера", "Только чёткость/пикселизация, без смены композиции"),
                new HelpRow("Х / Ъ", "Качество рендера", "Х ниже, Ъ выше; дублирует шаги качества"),
                new HelpRow("F7 / F8", "Качество", "Шаг качества вниз / вверх"),
                new HelpRow("Shift + F7 / F8", "Минимум / максимум", "Прыжок к крайним уровням"),
                new HelpRow("F9", "Adaptive Quality", "FPS-защита"),
                new HelpRow("Shift + F9", "Auto-balance", "Автобаланс производительности"),
                new HelpRow("Ctrl + A", "Авто режим", "Включить или выключить сценарный оркестратор"),
                new HelpRow("Shift + F10", "Сценарий", "Включить или выключить автооркестратор"),
                new HelpRow("Shift + F11", "Сценарий +", "Следующий сценарий эффектов"),
                new HelpRow("F10 / F11", "Performance preset", "Бюджет производительности вниз / вверх")
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

            float sectionWidth = (panel.width - 64f - 18f) * 0.5f;
            for (int i = 0; i < sectionOrder.Length; i += 2)
            {
                GUILayout.BeginHorizontal();
                DrawSection(sectionOrder[i], ResolveIcon(sectionOrder[i]), sectionWidth);
                if (i + 1 < sectionOrder.Length)
                {
                    GUILayout.Space(18f);
                    DrawSection(sectionOrder[i + 1], ResolveIcon(sectionOrder[i + 1]), sectionWidth);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10f);
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Viewer Mode is for the clean optical image. Operator Mode is for guides and the separate console.", footerStyle);
            GUILayout.EndArea();

            GUI.color = previousColor;
        }

        private void DrawStatusStrip()
        {
            GUILayout.BeginHorizontal();
            DrawStatusChip("VIEWER", "clean image");
            DrawStatusChip("OPERATOR", "console + guides");
            DrawStatusChip("GAME VIEW", "no diagnostics");
            DrawStatusChip("TOOLS", "off-canvas");
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

        private void DrawSection(string sectionName, string icon, float width)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
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

        private string ResolveIcon(string sectionName)
        {
            switch (sectionName)
            {
                case "SOURCE MODES":
                    return "SRC";
                case "OPTICAL GUIDES":
                    return "GDE";
                case "VIEWER CONTROLS":
                    return "VIEW";
                case "GEOMETRY":
                    return "GEO";
                case "DIAGNOSTICS":
                    return "DIA";
                case "PERFORMANCE":
                    return "FPS";
                default:
                    return "SYS";
            }
        }

        private void DrawRow(HelpRow row)
        {
            GUILayout.BeginHorizontal(GUILayout.Height(24f));
            GUILayout.Label(row.Key, keyStyle, GUILayout.Width(104f));
            GUILayout.Label(row.Action, actionStyle, GUILayout.Width(138f));
            GUILayout.Label(row.Hint, hintStyle);
            GUILayout.EndHorizontal();
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
