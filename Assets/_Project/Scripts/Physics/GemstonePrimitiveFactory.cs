using UnityEngine;

namespace KaleidoscopeEngine.PhysicsSandbox
{
    public static class GemstonePrimitiveFactory
    {
        public static GameObject CreatePrimitive(GemstoneDefinition definition)
        {
            PrimitiveType primitiveType = definition.shapeHint switch
            {
                GemstoneShapeHint.Rounded => PrimitiveType.Sphere,
                GemstoneShapeHint.Pebble => PrimitiveType.Sphere,
                GemstoneShapeHint.Elongated => PrimitiveType.Capsule,
                _ => PrimitiveType.Cube
            };

            GameObject root = GameObject.CreatePrimitive(primitiveType);
            ApplyShapeScale(root.transform, definition.shapeHint);

            Renderer renderer = root.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreatePlaceholderMaterial(definition);
            }

            return root;
        }

        private static void ApplyShapeScale(Transform transform, GemstoneShapeHint shapeHint)
        {
            transform.localScale = shapeHint switch
            {
                GemstoneShapeHint.Faceted => new Vector3(1f, 0.75f, 1.25f),
                GemstoneShapeHint.Elongated => new Vector3(0.72f, 1.35f, 0.72f),
                GemstoneShapeHint.Shard => new Vector3(1.25f, 0.5f, 0.72f),
                GemstoneShapeHint.ThinShard => new Vector3(1.55f, 0.18f, 0.62f),
                GemstoneShapeHint.Pebble => new Vector3(1f, 0.7f, 0.85f),
                _ => new Vector3(1f, 0.88f, 1.12f)
            };
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
                material.SetFloat("_Smoothness", 0.58f);
            }

            return material;
        }
    }
}
