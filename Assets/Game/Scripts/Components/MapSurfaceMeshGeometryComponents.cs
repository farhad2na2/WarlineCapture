using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct MapSurfaceMeshTriangle : IBufferElementData
    { public float3 A,B,C; }

    public struct MapSurfaceMeshInstance : IBufferElementData
    {
        public float4x4 WorldToMesh;
        public int FirstTriangle,TriangleCount;
    }
}
