using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    [DisallowMultipleComponent]
    public sealed class LiquidSourceMode : ProceduralSourceTextureMode
    {
        [SerializeField] private float flowSpeed = 0.16f;
        [SerializeField] private float viscosity = 0.72f;
        [SerializeField] private Color colorA = new Color(0.02f, 0.48f, 0.72f, 1f);
        [SerializeField] private Color colorB = new Color(0.9f, 0.28f, 0.08f, 1f);
        [SerializeField] private Color colorC = new Color(0.82f, 0.86f, 0.9f, 1f);

        private float phase;

        public override string GetSourceModeName() => "Liquid Illusion";

        protected override void GenerateTexture(float deltaTime, bool force)
        {
            EnsureTexture();
            phase += deltaTime * flowSpeed;
            Color[] pixels = new Color[textureSize * textureSize];

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 uv = new Vector2(x / (float)textureSize, y / (float)textureSize);
                    float n1 = Mathf.PerlinNoise(uv.x * 3.2f + phase, uv.y * 3.2f - phase * 0.7f);
                    float n2 = Mathf.PerlinNoise(uv.x * 9.5f - phase * 0.35f, uv.y * 9.5f + phase * 0.25f);
                    float flow = Mathf.SmoothStep(0f, 1f, Mathf.Lerp(n1, n2, 1f - viscosity));
                    Color color = flow < 0.52f
                        ? LerpColor(colorA, colorB, flow / 0.52f)
                        : LerpColor(colorB, colorC, (flow - 0.52f) / 0.48f);
                    float highlight = Mathf.Pow(Mathf.Clamp01(n2), 4f) * 0.24f;
                    pixels[y * textureSize + x] = color + new Color(highlight, highlight, highlight, 0f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
        }
    }
}
