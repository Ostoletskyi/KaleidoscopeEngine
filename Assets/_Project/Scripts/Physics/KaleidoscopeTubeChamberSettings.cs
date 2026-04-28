using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    [DisallowMultipleComponent]
    public sealed class KaleidoscopeTubeChamberSettings : MonoBehaviour
    {
        [Header("Tube Dimensions")]
        public float tubeRadius = 1.28f;
        public float tubeLength = 5.4f;
        public float wallThickness = 0.16f;
        public float capThickness = 0.2f;

        [Header("Visibility")]
        public bool showFrontCap;
        public bool showBackCap = true;
        public bool showTubeVisual = true;
        [Range(0f, 1f)] public float tubeTransparency = 0.22f;
        [Min(32)] public int tubeVisualSegments = 128;
        [Min(24)] public int tubeColliderSegments = 48;
        [Header("Internal Ribs")]
        public bool internalRibsEnabled = true;
        [Range(0, 8)] public int internalRibCount = 5;
        public float internalRibHeight = 0.12f;
        public float internalRibWidth = 0.16f;
        [Header("Depth Readability")]
        public bool showDepthGuideRings = true;
        public bool debugColliderVisibility;

        public void Configure(
            float radius,
            float length,
            float wall,
            float cap,
            bool frontCapVisible,
            bool backCapVisible,
            bool tubeVisible,
            float transparency,
            int visualSegments,
            int colliderSegments,
            bool ribsEnabled,
            int ribCount,
            float ribHeight,
            float ribWidth,
            bool depthGuideRingsVisible,
            bool colliderDebugVisible)
        {
            tubeRadius = radius;
            tubeLength = length;
            wallThickness = wall;
            capThickness = cap;
            showFrontCap = frontCapVisible;
            showBackCap = backCapVisible;
            showTubeVisual = tubeVisible;
            tubeTransparency = transparency;
            tubeVisualSegments = Mathf.Max(32, visualSegments);
            tubeColliderSegments = Mathf.Max(24, colliderSegments);
            internalRibsEnabled = ribsEnabled;
            internalRibCount = Mathf.Clamp(ribCount, 0, 8);
            internalRibHeight = Mathf.Max(0f, ribHeight);
            internalRibWidth = Mathf.Max(0f, ribWidth);
            showDepthGuideRings = depthGuideRingsVisible;
            debugColliderVisibility = colliderDebugVisible;
        }
    }
}
