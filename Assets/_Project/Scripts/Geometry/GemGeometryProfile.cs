using UnityEngine;

namespace KaleidoscopeEngine.Geometry
{
    [CreateAssetMenu(
        fileName = "GemGeometryProfile",
        menuName = "Kaleidoscope Engine/Geometry/Gem Geometry Profile")]
    public sealed class GemGeometryProfile : ScriptableObject
    {
        public string gemTypeId = "opal";
        public GemMeshType meshType = GemMeshType.OpalPebble;
        [Range(0f, 0.35f)] public float minVertexDistortion = 0.03f;
        [Range(0f, 0.35f)] public float maxVertexDistortion = 0.14f;
        [Range(0.4f, 3f)] public float minElongation = 0.8f;
        [Range(0.4f, 3f)] public float maxElongation = 1.25f;
        public bool flatNormals = true;
        public GemColliderMode colliderMode = GemColliderMode.ConvexMesh;
        public bool convexMeshCollider = true;
        public Vector3 visualScaleMultiplier = Vector3.one;
        public Vector3 colliderScaleMultiplier = Vector3.one;
    }
}
