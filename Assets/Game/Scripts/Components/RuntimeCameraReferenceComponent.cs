using Unity.Entities;
using Unity.Mathematics;

// Runtime camera object references are owned by RuntimeCameraReferenceSystem managed state.
// Hot rendering systems consume this value snapshot instead of reading Camera directly.

namespace Game.Components
{
    public struct RuntimeCameraSnapshotComponent : IComponentData
    {
        public byte IsValid;
        public float3 Position;
        public quaternion Rotation;
        public float4x4 WorldToCamera;
        public float4x4 Projection;
        public float4x4 ViewProjection;
    }
}
