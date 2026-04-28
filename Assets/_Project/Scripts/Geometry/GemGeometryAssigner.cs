using System.Collections.Generic;
using KaleidoscopeEngine.PhysicsSandbox;
using UnityEngine;

namespace KaleidoscopeEngine.Geometry
{
    [DisallowMultipleComponent]
    public sealed class GemGeometryAssigner : MonoBehaviour
    {
        [SerializeField] private bool proceduralGeometryEnabled = true;
        [SerializeField] private GemColliderMode defaultColliderMode = GemColliderMode.ConvexMesh;
        [SerializeField] private bool convexMeshCollider = true;

        private readonly Dictionary<string, GemGeometryProfile> profilesById = new Dictionary<string, GemGeometryProfile>();
        private readonly Dictionary<GameObject, Mesh> proceduralMeshes = new Dictionary<GameObject, Mesh>();
        private readonly Dictionary<GameObject, Mesh> debugMeshes = new Dictionary<GameObject, Mesh>();
        private int generatedMeshCount;
        private int totalVertexCount;

        public string GeometryMode => proceduralGeometryEnabled ? "Procedural Gems" : "Debug Geometry";
        public int ActiveMeshTypesCount => profilesById.Count;
        public string ColliderModeName => defaultColliderMode.ToString();
        public int TotalGeneratedMeshes => generatedMeshCount;
        public float AverageVertexCount => generatedMeshCount > 0 ? totalVertexCount / (float)generatedMeshCount : 0f;

        private void Awake()
        {
            EnsureProfiles();
        }

        public void ApplyTo(GameObject gemstoneObject, GemstoneDefinition definition, int seed)
        {
            if (gemstoneObject == null || definition == null)
            {
                return;
            }

            EnsureProfiles();
            GemGeometryProfile profile = FindProfile(definition);
            GemMeshType meshType = profile != null ? profile.meshType : ResolveMeshType(definition);
            Mesh proceduralMesh = ProceduralGemMeshFactory.CreateMesh(meshType, seed);
            Mesh debugMesh = CreateDebugMesh(definition);

            proceduralMeshes[gemstoneObject] = proceduralMesh;
            debugMeshes[gemstoneObject] = debugMesh;
            generatedMeshCount++;
            totalVertexCount += proceduralMesh.vertexCount;

            ApplyMesh(gemstoneObject, proceduralGeometryEnabled ? proceduralMesh : debugMesh);
            GemColliderMode colliderMode = profile != null ? profile.colliderMode : defaultColliderMode;
            bool convex = profile != null ? profile.convexMeshCollider : convexMeshCollider;
            GemColliderBuilder.ConfigureCollider(gemstoneObject, proceduralMesh, colliderMode, convex);
        }

        public void Release(GameObject gemstoneObject)
        {
            if (gemstoneObject == null)
            {
                return;
            }

            if (proceduralMeshes.TryGetValue(gemstoneObject, out Mesh proceduralMesh) && proceduralMesh != null)
            {
                Destroy(proceduralMesh);
            }

            if (debugMeshes.TryGetValue(gemstoneObject, out Mesh debugMesh) && debugMesh != null)
            {
                Destroy(debugMesh);
            }

            proceduralMeshes.Remove(gemstoneObject);
            debugMeshes.Remove(gemstoneObject);
        }

        public void ToggleGeometryMode()
        {
            proceduralGeometryEnabled = !proceduralGeometryEnabled;
            foreach (KeyValuePair<GameObject, Mesh> pair in proceduralMeshes)
            {
                GameObject gemstone = pair.Key;
                if (gemstone == null)
                {
                    continue;
                }

                Mesh mesh = proceduralGeometryEnabled ? pair.Value : debugMeshes[gemstone];
                ApplyMesh(gemstone, mesh);
            }
        }

        private void ApplyMesh(GameObject gemstoneObject, Mesh mesh)
        {
            MeshFilter meshFilter = gemstoneObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gemstoneObject.AddComponent<MeshFilter>();
            }

            meshFilter.sharedMesh = mesh;
        }

        private GemGeometryProfile FindProfile(GemstoneDefinition definition)
        {
            string id = definition.id != null ? definition.id.ToLowerInvariant() : string.Empty;
            return profilesById.TryGetValue(id, out GemGeometryProfile profile) ? profile : null;
        }

        private void EnsureProfiles()
        {
            if (profilesById.Count > 0)
            {
                return;
            }

            AddProfile("opal", GemMeshType.OpalPebble, GemColliderMode.ConvexMesh);
            AddProfile("ruby", GemMeshType.RubyFaceted, GemColliderMode.CompoundApproximation);
            AddProfile("emerald", GemMeshType.EmeraldPrism, GemColliderMode.CompoundApproximation);
            AddProfile("amethyst", GemMeshType.AmethystShard, GemColliderMode.CompoundApproximation);
            AddProfile("quartz", GemMeshType.QuartzShard, GemColliderMode.CompoundApproximation);
            AddProfile("glass_fragment", GemMeshType.GlassFragment, GemColliderMode.BoxApproximation);
            AddProfile("micro_particle", GemMeshType.MicroCrystal, GemColliderMode.ConvexMesh);
            AddProfile("dust", GemMeshType.MicroCrystal, GemColliderMode.ConvexMesh);
        }

        private void AddProfile(string id, GemMeshType meshType, GemColliderMode colliderMode)
        {
            GemGeometryProfile profile = ScriptableObject.CreateInstance<GemGeometryProfile>();
            profile.gemTypeId = id;
            profile.meshType = meshType;
            profile.colliderMode = colliderMode;
            profile.convexMeshCollider = true;
            profile.flatNormals = meshType != GemMeshType.OpalPebble;
            profilesById[id] = profile;
        }

        private static GemMeshType ResolveMeshType(GemstoneDefinition definition)
        {
            string id = definition.id != null ? definition.id.ToLowerInvariant() : string.Empty;
            if (id.Contains("opal")) return GemMeshType.OpalPebble;
            if (id.Contains("ruby")) return GemMeshType.RubyFaceted;
            if (id.Contains("emerald")) return GemMeshType.EmeraldPrism;
            if (id.Contains("amethyst")) return GemMeshType.AmethystShard;
            if (id.Contains("quartz")) return GemMeshType.QuartzShard;
            if (id.Contains("glass")) return GemMeshType.GlassFragment;
            return GemMeshType.MicroCrystal;
        }

        private static Mesh CreateDebugMesh(GemstoneDefinition definition)
        {
            PrimitiveType primitiveType = definition.shapeHint == GemstoneShapeHint.Rounded || definition.shapeHint == GemstoneShapeHint.Pebble
                ? PrimitiveType.Sphere
                : PrimitiveType.Cube;
            GameObject temporary = GameObject.CreatePrimitive(primitiveType);
            Mesh mesh = Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
            mesh.name = $"{definition.displayName} Debug Primitive Mesh";
            Destroy(temporary);
            return mesh;
        }
    }
}
