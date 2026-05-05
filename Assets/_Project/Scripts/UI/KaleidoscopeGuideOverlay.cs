using KaleidoscopeEngine.Mirrors;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;

namespace KaleidoscopeEngine.UI
{
    [System.Flags]
    public enum KaleidoscopeGuideFlags
    {
        None = 0,
        MirrorWedgeBoundaries = 1 << 0,
        SourceCoverage = 1 << 1,
        SourceToMirrorTransfer = 1 << 2,
        OpticalConvergence = 1 << 3,
        CenterComposition = 1 << 4,
        SafeViewingZones = 1 << 5,
        SourceDensityHeatmap = 1 << 6,
        RenderTexturePreview = 1 << 7,
        OpticalFlow = 1 << 8
    }

    [DisallowMultipleComponent]
    public sealed class KaleidoscopeGuideOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KaleidoscopeRenderPipeline renderPipeline;
        [SerializeField] private KaleidoscopeMirrorController mirrorController;
        [SerializeField] private GemstoneSpawner spawner;

        [Header("Guides")]
        [SerializeField] private bool operatorMode;
        [SerializeField] private KaleidoscopeGuideFlags activeGuides = KaleidoscopeGuideFlags.None;
        [SerializeField, Range(0.05f, 0.45f)] private float guideOpacity = 0.22f;

        private Texture2D pixel;

        public bool OperatorMode => operatorMode;
        public KaleidoscopeGuideFlags ActiveGuides => activeGuides;
        public bool AnyGuideVisible => operatorMode && activeGuides != KaleidoscopeGuideFlags.None;

        public void Configure(KaleidoscopeRenderPipeline pipeline, KaleidoscopeMirrorController mirror, GemstoneSpawner gemstoneSpawner)
        {
            renderPipeline = pipeline;
            mirrorController = mirror;
            spawner = gemstoneSpawner;
        }

        public void SetOperatorMode(bool enabled)
        {
            operatorMode = enabled;
            if (!operatorMode)
            {
                activeGuides = KaleidoscopeGuideFlags.None;
            }
        }

        public void ToggleGuide(KaleidoscopeGuideFlags guide)
        {
            if (guide == KaleidoscopeGuideFlags.None)
            {
                return;
            }

            operatorMode = true;
            activeGuides = (activeGuides & guide) != 0
                ? activeGuides & ~guide
                : activeGuides | guide;
        }

        public void HideAllGuides()
        {
            activeGuides = KaleidoscopeGuideFlags.None;
        }

        private void OnGUI()
        {
            if (!operatorMode || activeGuides == KaleidoscopeGuideFlags.None)
            {
                return;
            }

            EnsurePixel();

            Rect frame = GetOpticalFrame();
            Color cyan = new Color(0.55f, 0.92f, 1f, guideOpacity);
            Color softWhite = new Color(0.93f, 0.98f, 1f, guideOpacity * 0.72f);

            if ((activeGuides & KaleidoscopeGuideFlags.SourceDensityHeatmap) != 0)
            {
                DrawDensityHeatmap(frame);
            }

            if ((activeGuides & KaleidoscopeGuideFlags.SafeViewingZones) != 0)
            {
                DrawSafeZones(frame, softWhite);
            }

            if ((activeGuides & KaleidoscopeGuideFlags.MirrorWedgeBoundaries) != 0)
            {
                DrawMirrorWedges(frame, cyan);
            }

            if ((activeGuides & KaleidoscopeGuideFlags.SourceCoverage) != 0)
            {
                DrawCoverageMask(frame, cyan);
            }

            if ((activeGuides & KaleidoscopeGuideFlags.SourceToMirrorTransfer) != 0)
            {
                DrawTransferRegion(frame, cyan);
            }

            if ((activeGuides & KaleidoscopeGuideFlags.OpticalConvergence) != 0)
            {
                DrawConvergence(frame, softWhite);
            }

            if ((activeGuides & KaleidoscopeGuideFlags.CenterComposition) != 0)
            {
                DrawCenterComposition(frame, softWhite);
            }

            if ((activeGuides & KaleidoscopeGuideFlags.OpticalFlow) != 0)
            {
                DrawOpticalFlow(frame, cyan);
            }

            if ((activeGuides & KaleidoscopeGuideFlags.RenderTexturePreview) != 0)
            {
                DrawRenderTexturePreview();
            }
        }

        private Rect GetOpticalFrame()
        {
            float size = Mathf.Min(Screen.width, Screen.height) * 0.82f;
            return new Rect((Screen.width - size) * 0.5f, (Screen.height - size) * 0.5f, size, size);
        }

        private void DrawMirrorWedges(Rect frame, Color color)
        {
            int segmentCount = mirrorController != null ? Mathf.Max(1, mirrorController.SegmentCount) : 6;
            Vector2 center = frame.center;
            float radius = frame.width * 0.5f;
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = (Mathf.PI * 2f * i) / segmentCount - Mathf.PI * 0.5f;
                Vector2 end = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawDashedLine(center, end, color, 1f, 16f, 10f);
            }
        }

        private void DrawCoverageMask(Rect frame, Color color)
        {
            float coverage = spawner != null ? Mathf.Clamp01(spawner.SourceCoverageEstimate) : 0.72f;
            Rect coverageRect = ScaleRect(frame, Mathf.Lerp(0.55f, 0.96f, coverage));
            DrawCircle(frame.center, coverageRect.width * 0.5f, new Color(color.r, color.g, color.b, color.a * 0.7f), 1f, 96);
            DrawRectBorder(coverageRect, new Color(color.r, color.g, color.b, color.a * 0.45f), 1f);
        }

        private void DrawTransferRegion(Rect frame, Color color)
        {
            Rect inner = ScaleRect(frame, 0.62f);
            Rect outer = ScaleRect(frame, 0.92f);
            DrawRectBorder(outer, new Color(color.r, color.g, color.b, color.a * 0.35f), 1f);
            DrawRectBorder(inner, new Color(color.r, color.g, color.b, color.a * 0.72f), 1f);
            DrawDashedLine(new Vector2(inner.xMin, inner.center.y), new Vector2(outer.xMin, outer.center.y), color, 1f, 10f, 8f);
            DrawDashedLine(new Vector2(inner.xMax, inner.center.y), new Vector2(outer.xMax, outer.center.y), color, 1f, 10f, 8f);
        }

        private void DrawConvergence(Rect frame, Color color)
        {
            Vector2 center = frame.center;
            DrawCircle(center, frame.width * 0.16f, color, 1f, 72);
            DrawCircle(center, frame.width * 0.31f, new Color(color.r, color.g, color.b, color.a * 0.55f), 1f, 96);
            DrawDashedLine(new Vector2(frame.xMin, center.y), new Vector2(frame.xMax, center.y), color, 1f, 12f, 10f);
            DrawDashedLine(new Vector2(center.x, frame.yMin), new Vector2(center.x, frame.yMax), color, 1f, 12f, 10f);
        }

        private void DrawCenterComposition(Rect frame, Color color)
        {
            Vector2 center = frame.center;
            float radius = frame.width * 0.08f;
            DrawCircle(center, radius, color, 1f, 48);
            DrawCircle(center, radius * 2f, new Color(color.r, color.g, color.b, color.a * 0.42f), 1f, 64);
            DrawLine(center + Vector2.left * radius * 2.6f, center + Vector2.left * radius * 1.25f, color, 1f);
            DrawLine(center + Vector2.right * radius * 1.25f, center + Vector2.right * radius * 2.6f, color, 1f);
            DrawLine(center + Vector2.up * radius * 1.25f, center + Vector2.up * radius * 2.6f, color, 1f);
            DrawLine(center + Vector2.down * radius * 1.25f, center + Vector2.down * radius * 2.6f, color, 1f);
        }

        private void DrawSafeZones(Rect frame, Color color)
        {
            DrawRectBorder(ScaleRect(frame, 0.92f), new Color(color.r, color.g, color.b, color.a * 0.36f), 1f);
            DrawRectBorder(ScaleRect(frame, 0.78f), new Color(color.r, color.g, color.b, color.a * 0.52f), 1f);
            DrawCircle(frame.center, frame.width * 0.46f, new Color(color.r, color.g, color.b, color.a * 0.34f), 1f, 112);
        }

        private void DrawDensityHeatmap(Rect frame)
        {
            float coverage = spawner != null ? Mathf.Clamp01(spawner.SourceCoverageEstimate) : 0.7f;
            Color cool = new Color(0.2f, 0.88f, 1f, guideOpacity * 0.22f);
            Color warm = new Color(1f, 0.58f, 0.24f, guideOpacity * 0.18f);
            for (int i = 0; i < 6; i++)
            {
                float scale = Mathf.Lerp(0.24f, 0.96f, (i + 1f) / 6f);
                Color color = Color.Lerp(cool, warm, Mathf.Clamp01(coverage + i * 0.06f - 0.48f));
                DrawFilledCircle(frame.center, frame.width * scale * 0.5f, color, 64);
            }
        }

        private void DrawOpticalFlow(Rect frame, Color color)
        {
            Vector2 center = frame.center;
            float radius = frame.width * 0.38f;
            float phase = Time.unscaledTime * 0.65f;
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f + phase;
                Vector2 a = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * 0.72f;
                Vector2 b = center + new Vector2(Mathf.Cos(angle + 0.16f), Mathf.Sin(angle + 0.16f)) * radius;
                DrawLine(a, b, new Color(color.r, color.g, color.b, color.a * 0.55f), 1f);
            }
        }

        private void DrawRenderTexturePreview()
        {
            Texture texture = renderPipeline != null ? renderPipeline.ActiveSourceTexture : null;
            if (texture == null)
            {
                return;
            }

            float size = Mathf.Min(180f, Screen.height * 0.22f);
            Rect area = new Rect(18f, Screen.height - size - 18f, size, size);
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.82f);
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, false);
            GUI.color = previous;
            DrawRectBorder(area, new Color(0.65f, 0.95f, 1f, guideOpacity * 1.2f), 1f);
        }

        private Rect ScaleRect(Rect rect, float scale)
        {
            float width = rect.width * scale;
            float height = rect.height * scale;
            return new Rect(rect.center.x - width * 0.5f, rect.center.y - height * 0.5f, width, height);
        }

        private void DrawCircle(Vector2 center, float radius, Color color, float thickness, int segments)
        {
            Vector2 previous = center + Vector2.right * radius;
            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector2 next = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                DrawLine(previous, next, color, thickness);
                previous = next;
            }
        }

        private void DrawFilledCircle(Vector2 center, float radius, Color color, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float angle0 = (i / (float)segments) * Mathf.PI * 2f;
                float angle1 = ((i + 1f) / segments) * Mathf.PI * 2f;
                Vector2 a = center + new Vector2(Mathf.Cos(angle0), Mathf.Sin(angle0)) * radius;
                Vector2 b = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
                DrawLine(a, b, color, 3f);
            }
        }

        private void DrawDashedLine(Vector2 start, Vector2 end, Color color, float thickness, float dash, float gap)
        {
            float length = Vector2.Distance(start, end);
            if (length <= 0.01f)
            {
                return;
            }

            Vector2 direction = (end - start) / length;
            float cursor = 0f;
            while (cursor < length)
            {
                float next = Mathf.Min(cursor + dash, length);
                DrawLine(start + direction * cursor, start + direction * next, color, thickness);
                cursor += dash + gap;
            }
        }

        private void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            DrawLine(new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMax, rect.yMin), color, thickness);
            DrawLine(new Vector2(rect.xMax, rect.yMin), new Vector2(rect.xMax, rect.yMax), color, thickness);
            DrawLine(new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMin, rect.yMax), color, thickness);
            DrawLine(new Vector2(rect.xMin, rect.yMax), new Vector2(rect.xMin, rect.yMin), color, thickness);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float length = Vector2.Distance(start, end);

            GUI.color = color;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, length, thickness), pixel);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private void EnsurePixel()
        {
            if (pixel != null)
            {
                return;
            }

            pixel = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
    }
}
