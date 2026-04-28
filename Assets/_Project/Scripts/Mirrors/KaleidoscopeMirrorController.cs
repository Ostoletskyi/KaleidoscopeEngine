using UnityEngine;

namespace KaleidoscopeEngine.Mirrors
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeMirrorController : MonoBehaviour
    {
        [Header("Mirror")]
        [SerializeField, Range(1, 24)] private int segmentCount = 8;
        [SerializeField] private float mirrorAngleOffset;
        [SerializeField] private float patternRotationSpeed = 4f;
        [SerializeField, Range(0.25f, 4f)] private float patternZoom = 1.18f;
        [SerializeField] private Vector2 centerOffset;
        [SerializeField, Range(-1f, 1f)] private float radialDistortion = 0.08f;
        [SerializeField, Range(0f, 0.25f)] private float edgeSoftness = 0.018f;

        [Header("Image")]
        [SerializeField, Range(0f, 3f)] private float brightness = 1.05f;
        [SerializeField, Range(0f, 3f)] private float contrast = 1.08f;
        [SerializeField, Range(0f, 2f)] private float saturation = 1.1f;

        [Header("Input Response")]
        [SerializeField] private float manualRotationDegreesPerSecond = 72f;
        [SerializeField] private float zoomStep = 0.7f;
        [SerializeField] private float distortionStep = 0.35f;

        private Material displayMaterial;
        private float runtimeRotation;

        public int SegmentCount => segmentCount;
        public float PatternZoom => patternZoom;
        public float PatternRotation => runtimeRotation + mirrorAngleOffset;
        public float RadialDistortion => radialDistortion;

        public void Configure(Material material)
        {
            displayMaterial = material;
            ApplyShaderValues();
        }

        private void LateUpdate()
        {
            runtimeRotation += patternRotationSpeed * Mathf.Deg2Rad * Time.deltaTime;
            ApplyShaderValues();
        }

        public void AdjustSegmentCount(int delta)
        {
            segmentCount = Mathf.Clamp(segmentCount + delta, 1, 24);
            ApplyShaderValues();
        }

        public void AdjustZoom(float direction)
        {
            patternZoom = Mathf.Clamp(patternZoom + direction * zoomStep * Time.deltaTime, 0.25f, 4f);
            ApplyShaderValues();
        }

        public void AdjustRadialDistortion(float direction)
        {
            radialDistortion = Mathf.Clamp(radialDistortion + direction * distortionStep * Time.deltaTime, -1f, 1f);
            ApplyShaderValues();
        }

        public void RotatePattern(float direction)
        {
            runtimeRotation += direction * manualRotationDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime;
            ApplyShaderValues();
        }

        public void SetSourceTexture(Texture sourceTexture)
        {
            if (displayMaterial != null)
            {
                displayMaterial.SetTexture("_SourceTex", sourceTexture);
            }
        }

        private void ApplyShaderValues()
        {
            if (displayMaterial == null)
            {
                return;
            }

            displayMaterial.SetFloat("_SegmentCount", segmentCount);
            displayMaterial.SetFloat("_Rotation", runtimeRotation + mirrorAngleOffset);
            displayMaterial.SetFloat("_Zoom", patternZoom);
            displayMaterial.SetVector("_CenterOffset", centerOffset);
            displayMaterial.SetFloat("_RadialDistortion", radialDistortion);
            displayMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
            displayMaterial.SetFloat("_Brightness", brightness);
            displayMaterial.SetFloat("_Contrast", contrast);
            displayMaterial.SetFloat("_Saturation", saturation);
        }
    }
}
