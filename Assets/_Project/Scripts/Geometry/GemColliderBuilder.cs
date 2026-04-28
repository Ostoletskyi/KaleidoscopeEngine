using UnityEngine;

namespace KaleidoscopeEngine.Geometry
{
    public static class GemColliderBuilder
    {
        public static void ConfigureCollider(GameObject target, Mesh mesh, GemColliderMode mode, bool convex)
        {
            foreach (Collider collider in target.GetComponents<Collider>())
            {
                Object.Destroy(collider);
            }

            if (mode == GemColliderMode.BoxApproximation)
            {
                BoxCollider box = target.AddComponent<BoxCollider>();
                box.size = mesh.bounds.size;
                box.center = mesh.bounds.center;
                return;
            }

            if (mode == GemColliderMode.CompoundApproximation)
            {
                Bounds bounds = mesh.bounds;
                BoxCollider main = target.AddComponent<BoxCollider>();
                main.center = bounds.center;
                main.size = bounds.size;

                BoxCollider bite = target.AddComponent<BoxCollider>();
                bite.center = bounds.center + new Vector3(bounds.extents.x * 0.2f, bounds.extents.y * 0.1f, 0f);
                bite.size = Vector3.Scale(bounds.size, new Vector3(0.55f, 0.55f, 0.7f));
                return;
            }

            MeshCollider meshCollider = target.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = convex;
        }
    }
}
