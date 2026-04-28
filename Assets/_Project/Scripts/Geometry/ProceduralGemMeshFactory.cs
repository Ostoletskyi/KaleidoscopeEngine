using System.Collections.Generic;
using UnityEngine;

namespace KaleidoscopeEngine.Geometry
{
    public static class ProceduralGemMeshFactory
    {
        public static Mesh CreateMesh(GemMeshType type, int seed)
        {
            return type switch
            {
                GemMeshType.OpalPebble => CreateOpalPebble(seed),
                GemMeshType.RubyFaceted => CreateRubyFaceted(seed),
                GemMeshType.EmeraldPrism => CreateEmeraldPrism(seed),
                GemMeshType.AmethystShard => CreateAmethystShard(seed),
                GemMeshType.QuartzShard => CreateQuartzShard(seed),
                GemMeshType.GlassFragment => CreateGlassFragment(seed),
                GemMeshType.MicroCrystal => CreateMicroCrystal(seed),
                _ => CreateRubyFaceted(seed)
            };
        }

        public static Mesh CreateOpalPebble(int seed)
        {
            System.Random random = new System.Random(seed);
            const int longitude = 10;
            const int latitude = 7;
            Vector3[] vertices = new Vector3[(longitude + 1) * (latitude + 1)];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[longitude * latitude * 6];

            float squashY = RandomRange(random, 0.58f, 0.76f);
            float squashZ = RandomRange(random, 0.72f, 0.95f);
            float ripplePhase = RandomRange(random, 0f, 10f);

            for (int y = 0; y <= latitude; y++)
            {
                float v = y / (float)latitude;
                float phi = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, v);

                for (int x = 0; x <= longitude; x++)
                {
                    float u = x / (float)longitude;
                    float theta = u * Mathf.PI * 2f;
                    float ripple = 1f + Mathf.Sin(theta * 2.3f + phi * 1.7f + ripplePhase) * 0.07f;
                    ripple += Mathf.Cos(theta * 4.1f + ripplePhase) * 0.035f;
                    Vector3 unit = new Vector3(Mathf.Cos(phi) * Mathf.Cos(theta), Mathf.Sin(phi), Mathf.Cos(phi) * Mathf.Sin(theta));
                    vertices[y * (longitude + 1) + x] = Vector3.Scale(unit * ripple, new Vector3(0.96f, squashY, squashZ));
                    uvs[y * (longitude + 1) + x] = new Vector2(u, v);
                }
            }

            int index = 0;
            for (int y = 0; y < latitude; y++)
            {
                for (int x = 0; x < longitude; x++)
                {
                    int a = y * (longitude + 1) + x;
                    int b = a + 1;
                    int c = a + longitude + 1;
                    int d = c + 1;
                    triangles[index++] = a;
                    triangles[index++] = c;
                    triangles[index++] = b;
                    triangles[index++] = b;
                    triangles[index++] = c;
                    triangles[index++] = d;
                }
            }

            return BuildMesh("Opal Procedural Pebble", vertices, triangles, smoothNormals: true);
        }

        public static Mesh CreateRubyFaceted(int seed)
        {
            System.Random random = new System.Random(seed);
            return CreateFacetedBipyramid(
                "Ruby Procedural Faceted",
                8,
                RandomRange(random, 0.5f, 0.62f),
                RandomRange(random, 0.3f, 0.42f),
                RandomRange(random, 0.13f, 0.22f),
                random);
        }

        public static Mesh CreateEmeraldPrism(int seed)
        {
            System.Random random = new System.Random(seed);
            return CreateHexPrism(
                "Emerald Procedural Hex Prism",
                RandomRange(random, 0.34f, 0.46f),
                RandomRange(random, 1.08f, 1.48f),
                RandomRange(random, 0.16f, 0.28f),
                random);
        }

        public static Mesh CreateAmethystShard(int seed)
        {
            System.Random random = new System.Random(seed);
            return CreateShard("Amethyst Procedural Shard", RandomRange(random, 0.5f, 0.75f), RandomRange(random, 0.9f, 1.35f), RandomRange(random, 0.24f, 0.42f), random, jagged: true);
        }

        public static Mesh CreateQuartzShard(int seed)
        {
            System.Random random = new System.Random(seed);
            return CreatePointedShard("Quartz Procedural Pointed Shard", RandomRange(random, 0.34f, 0.5f), RandomRange(random, 1.15f, 1.62f), RandomRange(random, 0.18f, 0.34f), random);
        }

        public static Mesh CreateGlassFragment(int seed)
        {
            System.Random random = new System.Random(seed);
            return CreateShard("Glass Procedural Fragment", RandomRange(random, 0.52f, 0.95f), RandomRange(random, 0.95f, 1.5f), RandomRange(random, 0.06f, 0.14f), random, jagged: true);
        }

        public static Mesh CreateMicroCrystal(int seed)
        {
            System.Random random = new System.Random(seed);
            if (random.NextDouble() < 0.5)
            {
                return CreateTetra("Micro Procedural Tetra", random);
            }

            return CreateOcta("Micro Procedural Octa", random);
        }

        private static Mesh CreateFacetedBipyramid(string name, int sides, float radius, float height, float tableRadius, System.Random random)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Vector3 top = Vector3.up * height * RandomRange(random, 0.9f, 1.18f);
            Vector3 bottom = Vector3.down * height * RandomRange(random, 0.9f, 1.22f);
            Vector3[] girdle = Ring(sides, radius, 0f, random, 0.08f);
            Vector3[] table = Ring(sides, tableRadius, height * 0.55f, random, 0.04f);
            Vector3[] lower = Ring(sides, radius * 0.72f, -height * 0.38f, random, 0.07f, 0.5f);

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                AddQuad(vertices, triangles, table[i], table[next], girdle[next], girdle[i]);
                AddQuad(vertices, triangles, girdle[i], girdle[next], lower[next], lower[i]);
                AddTriangle(vertices, triangles, table[next], table[i], top);
                AddTriangle(vertices, triangles, lower[i], lower[next], bottom);
            }

            return BuildMesh(name, vertices.ToArray(), triangles.ToArray(), smoothNormals: false);
        }

        private static Mesh CreateHexPrism(string name, float radius, float length, float bevel, System.Random random)
        {
            const int sides = 6;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Vector3[] centerA = RingX(sides, radius, -length * 0.5f + bevel, random, 0.06f);
            Vector3[] centerB = RingX(sides, radius * RandomRange(random, 0.9f, 1.08f), length * 0.5f - bevel, random, 0.06f, 0.5f);
            Vector3[] capA = RingX(sides, radius * 0.5f, -length * 0.5f, random, 0.04f);
            Vector3[] capB = RingX(sides, radius * 0.5f, length * 0.5f, random, 0.04f, 0.5f);
            Vector3 tipA = Vector3.left * (length * 0.5f + bevel * 0.18f);
            Vector3 tipB = Vector3.right * (length * 0.5f + bevel * 0.18f);

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                AddQuad(vertices, triangles, centerA[i], centerA[next], centerB[next], centerB[i]);
                AddQuad(vertices, triangles, capA[next], capA[i], centerA[i], centerA[next]);
                AddQuad(vertices, triangles, centerB[i], centerB[next], capB[next], capB[i]);
                AddTriangle(vertices, triangles, capA[i], capA[next], tipA);
                AddTriangle(vertices, triangles, capB[next], capB[i], tipB);
            }

            return BuildMesh(name, vertices.ToArray(), triangles.ToArray(), smoothNormals: false);
        }

        private static Mesh CreatePointedShard(string name, float width, float length, float thickness, System.Random random)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Vector3 leftTip = Vector3.left * length * 0.58f;
            Vector3 rightTip = Vector3.right * length * 0.62f;
            Vector3[] mid = RingX(5, width, 0f, random, 0.18f, 0.12f);
            Vector3[] back = RingX(5, width * 0.56f, -length * 0.28f, random, 0.16f, 0.52f);
            for (int i = 0; i < mid.Length; i++)
            {
                int next = (i + 1) % mid.Length;
                mid[i].z *= thickness / Mathf.Max(0.001f, width);
                back[i].z *= thickness / Mathf.Max(0.001f, width);
                AddQuad(vertices, triangles, back[i], back[next], mid[next], mid[i]);
                AddTriangle(vertices, triangles, mid[i], mid[next], rightTip);
                AddTriangle(vertices, triangles, back[next], back[i], leftTip);
            }

            return BuildMesh(name, vertices.ToArray(), triangles.ToArray(), smoothNormals: false);
        }

        private static Mesh CreateShard(string name, float width, float length, float thickness, System.Random random, bool jagged)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            int sides = jagged ? 7 : 5;
            Vector2[] outline = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = i / (float)sides * Mathf.PI * 2f;
                float radial = RandomRange(random, 0.55f, 1f);
                outline[i] = new Vector2(Mathf.Cos(angle) * length * 0.5f * radial, Mathf.Sin(angle) * width * 0.5f * radial);
            }

            Vector3 frontCenter = Vector3.forward * thickness * 0.5f;
            Vector3 backCenter = Vector3.back * thickness * 0.5f;
            for (int i = 0; i < outline.Length; i++)
            {
                int next = (i + 1) % outline.Length;
                Vector3 a = new Vector3(outline[i].x, outline[i].y, thickness * 0.5f);
                Vector3 b = new Vector3(outline[next].x, outline[next].y, thickness * RandomRange(random, 0.22f, 0.55f));
                Vector3 c = new Vector3(outline[next].x, outline[next].y, -thickness * 0.5f);
                Vector3 d = new Vector3(outline[i].x, outline[i].y, -thickness * RandomRange(random, 0.22f, 0.55f));
                AddTriangle(vertices, triangles, frontCenter, a, b);
                AddTriangle(vertices, triangles, backCenter, c, d);
                AddQuad(vertices, triangles, a, d, c, b);
            }

            return BuildMesh(name, vertices.ToArray(), triangles.ToArray(), smoothNormals: false);
        }

        private static Mesh CreateTetra(string name, System.Random random)
        {
            Vector3[] vertices =
            {
                Distort(new Vector3(0f, 0.58f, 0f), random, 0.08f),
                Distort(new Vector3(-0.48f, -0.34f, -0.28f), random, 0.08f),
                Distort(new Vector3(0.5f, -0.32f, -0.24f), random, 0.08f),
                Distort(new Vector3(0f, -0.26f, 0.54f), random, 0.08f)
            };
            int[] triangles = { 0, 1, 2, 0, 2, 3, 0, 3, 1, 1, 3, 2 };
            return BuildMesh(name, vertices, triangles, smoothNormals: false);
        }

        private static Mesh CreateOcta(string name, System.Random random)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            Vector3 top = Distort(Vector3.up * 0.58f, random, 0.06f);
            Vector3 bottom = Distort(Vector3.down * 0.46f, random, 0.06f);
            Vector3 left = Distort(Vector3.left * 0.48f, random, 0.06f);
            Vector3 right = Distort(Vector3.right * 0.52f, random, 0.06f);
            Vector3 front = Distort(Vector3.forward * 0.42f, random, 0.06f);
            Vector3 back = Distort(Vector3.back * 0.5f, random, 0.06f);
            AddTriangle(vertices, triangles, top, front, right);
            AddTriangle(vertices, triangles, top, right, back);
            AddTriangle(vertices, triangles, top, back, left);
            AddTriangle(vertices, triangles, top, left, front);
            AddTriangle(vertices, triangles, bottom, right, front);
            AddTriangle(vertices, triangles, bottom, back, right);
            AddTriangle(vertices, triangles, bottom, left, back);
            AddTriangle(vertices, triangles, bottom, front, left);
            return BuildMesh(name, vertices.ToArray(), triangles.ToArray(), smoothNormals: false);
        }

        private static Vector3[] Ring(int sides, float radius, float y, System.Random random, float distortion, float offset = 0f)
        {
            Vector3[] ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = ((i + offset) / sides) * Mathf.PI * 2f;
                ring[i] = Distort(new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius), random, distortion);
            }

            return ring;
        }

        private static Vector3[] RingX(int sides, float radius, float x, System.Random random, float distortion, float offset = 0f)
        {
            Vector3[] ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = ((i + offset) / sides) * Mathf.PI * 2f;
                ring[i] = Distort(new Vector3(x, Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius), random, distortion);
            }

            return ring;
        }

        private static Vector3 Distort(Vector3 value, System.Random random, float amount)
        {
            return value + new Vector3(RandomRange(random, -amount, amount), RandomRange(random, -amount, amount), RandomRange(random, -amount, amount));
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(vertices, triangles, a, b, c);
            AddTriangle(vertices, triangles, a, c, d);
        }

        private static Mesh BuildMesh(string name, Vector3[] vertices, int[] triangles, bool smoothNormals)
        {
            Mesh mesh = new Mesh
            {
                name = name,
                vertices = vertices,
                triangles = triangles
            };

            if (smoothNormals)
            {
                mesh.RecalculateNormals();
            }
            else
            {
                Vector3[] normals = new Vector3[vertices.Length];
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i];
                    int b = triangles[i + 1];
                    int c = triangles[i + 2];
                    Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
                    normals[a] = normal;
                    normals[b] = normal;
                    normals[c] = normal;
                }

                mesh.normals = normals;
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private static float RandomRange(System.Random random, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
