using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    [DisallowMultipleComponent]
    public sealed class LiquidIllusionSource : ProceduralSourceTextureMode
    {
        [SerializeField] private float flowSpeed = 0.11f;
        [SerializeField] private Color oil = new Color(0.05f, 0.42f, 0.85f, 1f);
        [SerializeField] private Color mercury = new Color(0.76f, 0.8f, 0.86f, 1f);
        [SerializeField] private Color ink = new Color(0.84f, 0.08f, 0.36f, 1f);

        private float phase;

        public override string GetSourceModeName() => "Liquid Shader Source";

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
                    float swirl = Mathf.Sin((uv.x + uv.y + phase) * 6.28318f) * 0.04f;
                    float n1 = Mathf.PerlinNoise(uv.x * 3.4f + phase + swirl, uv.y * 3.4f - phase);
                    float n2 = Mathf.PerlinNoise(uv.x * 11.5f - phase * 0.6f, uv.y * 9.2f + phase * 0.3f);
                    float flow = Mathf.SmoothStep(0f, 1f, n1 * 0.72f + n2 * 0.28f);
                    Color color = flow < 0.5f ? LerpColor(oil, ink, flow * 2f) : LerpColor(ink, mercury, (flow - 0.5f) * 2f);
                    float specular = Mathf.Pow(Mathf.Clamp01(n2), 6f) * 0.32f;
                    pixels[y * textureSize + x] = color + new Color(specular, specular, specular, 0f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
        }
    }
}
