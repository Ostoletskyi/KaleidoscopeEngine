using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    public enum PolygonSourceShape
    {
        Hexagon = 6,
        Octagon = 8,
        Decagon = 10
    }

    [DisallowMultipleComponent]
    public sealed class PolygonSource : ProceduralSourceTextureMode
    {
        [SerializeField] private PolygonSourceShape shape = PolygonSourceShape.Hexagon;
        [SerializeField] private float panSpeed = 0.045f;
        [SerializeField] private float colorDrift = 0.12f;

        private float phase;

        public override string GetSourceModeName() => $"{shape} Geometry";

        public void SetShape(PolygonSourceShape nextShape)
        {
            shape = nextShape;
            GenerateTexture(0f, true);
        }

        public override void RandomizeMode()
        {
            base.RandomizeMode();
            int roll = UnityEngine.Random.Range(0, 3);
            shape = roll == 0 ? PolygonSourceShape.Hexagon : roll == 1 ? PolygonSourceShape.Octagon : PolygonSourceShape.Decagon;
        }

        protected override void GenerateTexture(float deltaTime, bool force)
        {
            EnsureTexture();
            phase += deltaTime * panSpeed;
            Color[] pixels = new Color[textureSize * textureSize];
            float sides = (int)shape;
            float cells = Mathf.Lerp(5f, 13f, density);
            float angleStep = Mathf.PI * 2f / sides;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 uv = new Vector2(x / (float)textureSize - 0.5f, y / (float)textureSize - 0.5f);
                    uv += new Vector2(phase * 0.16f, Mathf.Sin(phase) * 0.035f);
                    Vector2 cell = new Vector2(Mathf.Floor((uv.x + 0.5f) * cells), Mathf.Floor((uv.y + 0.5f) * cells));
                    Vector2 local = new Vector2(Mathf.Repeat((uv.x + 0.5f) * cells, 1f) - 0.5f, Mathf.Repeat((uv.y + 0.5f) * cells, 1f) - 0.5f);
                    float angle = Mathf.Atan2(local.y, local.x);
                    float radius = local.magnitude;
                    float edge = Mathf.Cos(Mathf.Floor(0.5f + angle / angleStep) * angleStep - angle) * radius;
                    float id = cell.x * 17.13f + cell.y * 31.77f;
                    float hue = Mathf.Repeat(id * 0.033f + phase * colorDrift, 1f);
                    Color fill = Color.HSVToRGB(hue, 0.62f, 0.86f);
                    Color line = Color.HSVToRGB(Mathf.Repeat(hue + 0.1f, 1f), 0.38f, 0.32f);
                    pixels[y * textureSize + x] = LerpColor(line, fill, Mathf.SmoothStep(0.21f, 0.33f, edge));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
        }
    }
}
