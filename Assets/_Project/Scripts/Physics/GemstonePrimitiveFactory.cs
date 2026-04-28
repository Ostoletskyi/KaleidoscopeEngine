using KaleidoscopeEngine.Geometry;
using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public static class GemstonePrimitiveFactory
    {
        public static GameObject CreatePrimitive(GemstoneDefinition definition)
        {
            GameObject root = new GameObject(definition.displayName);
            Mesh mesh = ProceduralGemMeshFactory.CreateMesh(ResolveMeshType(definition), definition.id != null ? definition.id.GetHashCode() : 17);

            MeshFilter meshFilter = root.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreatePlaceholderMaterial(definition);

            MeshCollider meshCollider = root.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;

            return root;
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

        private static Material CreatePlaceholderMaterial(GemstoneDefinition definition)
        {
            Material material = new Material(Shader.Find("HDRP/Lit") ?? Shader.Find("Standard"))
            {
                name = $"{definition.displayName} Placeholder"
            };

            material.color = definition.placeholderColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", definition.placeholderColor);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.82f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            return material;
        }
    }
}
