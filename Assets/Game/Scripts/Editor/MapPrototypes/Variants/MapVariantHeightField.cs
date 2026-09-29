using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.MapVariants
{
    // Vertex heights and sampling share one triangulation, so a sampled height is exactly the rendered ground.
    internal sealed class MapVariantHeightField
    {
        private readonly float[] _heights;

        public MapVariantHeightField(Vector2 origin, Vector2 size, float cellSize)
        {
            if (cellSize <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            Origin = origin;
            CellSize = cellSize;
            CellsX = Mathf.CeilToInt(size.x / cellSize);
            CellsZ = Mathf.CeilToInt(size.y / cellSize);
            _heights = new float[(CellsX + 1) * (CellsZ + 1)];
        }

        public Vector2 Origin { get; }
        public float CellSize { get; }
        public int CellsX { get; }
        public int CellsZ { get; }
        public Vector2 Size => new(CellsX * CellSize, CellsZ * CellSize);

        public float GetVertex(int x, int z) => _heights[Index(x, z)];

        public void SetVertex(int x, int z, float height) => _heights[Index(x, z)] = height;

        public Vector3 VertexPosition(int x, int z) =>
            new(Origin.x + x * CellSize, GetVertex(x, z), Origin.y + z * CellSize);

        public void Apply(Func<float, float, float, float> modifier)
        {
            for (int z = 0; z <= CellsZ; z++)
            for (int x = 0; x <= CellsX; x++)
            {
                Vector3 p = VertexPosition(x, z);
                SetVertex(x, z, modifier(p.x, p.z, p.y));
            }
        }

        public float Sample(float worldX, float worldZ)
        {
            float gx = Mathf.Clamp((worldX - Origin.x) / CellSize, 0f, CellsX - 0.0001f);
            float gz = Mathf.Clamp((worldZ - Origin.y) / CellSize, 0f, CellsZ - 0.0001f);
            int ix = (int)gx;
            int iz = (int)gz;
            float fx = gx - ix;
            float fz = gz - iz;
            float h00 = GetVertex(ix, iz);
            float h10 = GetVertex(ix + 1, iz);
            float h01 = GetVertex(ix, iz + 1);
            float h11 = GetVertex(ix + 1, iz + 1);
            return fz >= fx
                ? h00 + (h11 - h01) * fx + (h01 - h00) * fz
                : h00 + (h10 - h00) * fx + (h11 - h10) * fz;
        }

        public void SampleRange(Rect area, out float min, out float max)
        {
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;
            int stepsX = Mathf.Max(2, Mathf.CeilToInt(area.width / (CellSize * 0.5f)) + 1);
            int stepsZ = Mathf.Max(2, Mathf.CeilToInt(area.height / (CellSize * 0.5f)) + 1);
            for (int z = 0; z < stepsZ; z++)
            for (int x = 0; x < stepsX; x++)
            {
                float h = Sample(
                    Mathf.Lerp(area.xMin, area.xMax, x / (float)(stepsX - 1)),
                    Mathf.Lerp(area.yMin, area.yMax, z / (float)(stepsZ - 1)));
                min = Mathf.Min(min, h);
                max = Mathf.Max(max, h);
            }
        }

        public Mesh BuildChunkMesh(int startX, int startZ, int countX, int countZ, Vector2 uv, string name)
        {
            countX = Mathf.Min(countX, CellsX - startX);
            countZ = Mathf.Min(countZ, CellsZ - startZ);
            int vertsX = countX + 1;
            int vertsZ = countZ + 1;
            var vertices = new Vector3[vertsX * vertsZ];
            var uvs = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];
            for (int z = 0; z < vertsZ; z++)
            for (int x = 0; x < vertsX; x++)
            {
                int gx = startX + x;
                int gz = startZ + z;
                int i = z * vertsX + x;
                vertices[i] = VertexPosition(gx, gz);
                uvs[i] = uv;
                float hl = GetVertex(Mathf.Max(0, gx - 1), gz);
                float hr = GetVertex(Mathf.Min(CellsX, gx + 1), gz);
                float hd = GetVertex(gx, Mathf.Max(0, gz - 1));
                float hu = GetVertex(gx, Mathf.Min(CellsZ, gz + 1));
                normals[i] = new Vector3(hl - hr, 2f * CellSize, hd - hu).normalized;
            }

            var triangles = new int[countX * countZ * 6];
            int t = 0;
            for (int z = 0; z < countZ; z++)
            for (int x = 0; x < countX; x++)
            {
                int v00 = z * vertsX + x;
                int v10 = v00 + 1;
                int v01 = v00 + vertsX;
                int v11 = v01 + 1;
                triangles[t++] = v00;
                triangles[t++] = v01;
                triangles[t++] = v11;
                triangles[t++] = v00;
                triangles[t++] = v11;
                triangles[t++] = v10;
            }

            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private int Index(int x, int z) => z * (CellsX + 1) + x;
    }
}
