using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    [DisallowMultipleComponent]
    public sealed class HexagonSourceMode : ProceduralSourceTextureMode
    {
        [SerializeField] private float panSpeed = 0.08f;
        [SerializeField] private float colorDrift = 0.18f;

        private float phase;

        public override string GetSourceModeName() => "Hexagons";

        protected override void GenerateTexture(float deltaTime, bool force)
        {
            EnsureTexture();
            phase += deltaTime * panSpeed;
            Color[] pixels = new Color[textureSize * textureSize];
            float cells = Mathf.Lerp(6f, 14f, density);
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 uv = new Vector2(x / (float)textureSize, y / (float)textureSize);
                    float q = (uv.x + phase) * cells;
                    float r = (uv.y + Mathf.Sin(phase) * 0.05f) * cells * 1.1547f;
                    float id = Mathf.Floor(q + Mathf.Floor(r) * 19.17f);
                    float edge = Mathf.Abs(Mathf.Sin(q * Mathf.PI) * Mathf.Sin((q * 0.5f + r) * Mathf.PI));
                    float hue = Mathf.Repeat(id * 0.071f + phase * colorDrift, 1f);
                    Color baseColor = Color.HSVToRGB(hue, 0.58f, 0.82f);
                    Color edgeColor = Color.HSVToRGB(Mathf.Repeat(hue + 0.08f, 1f), 0.4f, 0.42f);
                    pixels[y * textureSize + x] = LerpColor(edgeColor, baseColor, Mathf.SmoothStep(0.08f, 0.22f, edge));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
        }
    }
}
