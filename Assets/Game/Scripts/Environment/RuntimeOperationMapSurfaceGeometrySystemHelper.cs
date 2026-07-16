using UnityEngine;

namespace Game.Runtime
{
    internal static class RuntimeOperationMapSurfaceGeometrySystemHelper
    {
        private const int SegmentCount = 28;

        public static GameObject CreateIrregularSurface(string objectName, uint seed, Transform parent)
        {
            var surface = new GameObject(objectName);
            surface.transform.SetParent(parent, false);

            Mesh mesh = CreateMesh(objectName, seed);
            MeshFilter filter = surface.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            surface.AddComponent<MeshRenderer>();
            return surface;
        }

        private static Mesh CreateMesh(string objectName, uint seed)
        {
            uint state = CombineSeed(seed, objectName);
            var vertices = new Vector3[SegmentCount + 1];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[SegmentCount * 3];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < SegmentCount; i++)
            {
                float angle = i * Mathf.PI * 2f / SegmentCount;
                float radius = Mathf.Lerp(0.82f, 1f, Next01(ref state));
                float x = Mathf.Cos(angle) * 0.5f * radius;
                float z = Mathf.Sin(angle) * 0.5f * radius;
                vertices[i + 1] = new Vector3(x, 0f, z);
                uv[i + 1] = new Vector2(x + 0.5f, z + 0.5f);

                int triangle = i * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = i == SegmentCount - 1 ? 1 : i + 2;
                triangles[triangle + 2] = i + 1;
            }

            var mesh = new Mesh
            {
                name = "RuntimeIrregularSurface_" + objectName,
                vertices = vertices,
                uv = uv,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static uint CombineSeed(uint seed, string value)
        {
            uint hash = seed == 0u ? 2166136261u : seed;
            if (value == null)
                return hash;

            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619u;
            }

            return hash == 0u ? 1u : hash;
        }

        private static float Next01(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0x00FFFFFFu) / 16777215f;
        }
    }
}
