using UnityEngine;

namespace KaleidoscopeEngine.Source
{
    [DisallowMultipleComponent]
    public sealed class ProceduralColorBlobsSource : ProceduralSourceTextureMode
    {
        [SerializeField] private float driftSpeed = 0.08f;
        [SerializeField] private float blobScale = 4.4f;

        private float phase;

        public override string GetSourceModeName() => "Procedural Color Blobs";

        protected override void GenerateTexture(float deltaTime, bool force)
        {
            EnsureTexture();
            phase += deltaTime * driftSpeed;
            Color[] pixels = new Color[textureSize * textureSize];

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 uv = new Vector2(x / (float)textureSize, y / (float)textureSize);
                    float n1 = Mathf.PerlinNoise(uv.x * blobScale + phase, uv.y * blobScale - phase * 0.7f);
                    float n2 = Mathf.PerlinNoise(uv.x * blobScale * 1.9f - phase * 0.4f, uv.y * blobScale * 1.6f + phase);
                    float mix = Mathf.SmoothStep(0f, 1f, Mathf.Lerp(n1, n2, 0.35f));
                    float hue = Mathf.Repeat(mix * 0.35f + uv.x * 0.18f + phase * 0.08f, 1f);
                    Color color = Color.HSVToRGB(hue, Mathf.Lerp(0.52f, 0.86f, density), Mathf.Lerp(0.68f, 0.96f, mix));
                    pixels[y * textureSize + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
        }
    }
}
