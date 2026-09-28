using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Components
{
    // Immutable authored knowledge; revisions must change when bounds, visibility, exclusions or route change.
    public struct SupportMissionContextComponent : IComponentData
    {
        public FixedString64Bytes MissionId,OperationMapId; public uint MissionSourceVersion,Revision;
        public float2 GroundMin,GroundMax; public float3 Entry,Release,Exit; public float Clearance;
        public int PopulationCeiling; public byte RouteAuthored;
    }
    public struct SupportAuthoredGroundRegionElement : IBufferElementData
    { public float2 Min,Max; public byte Visible,Protected; }
    public struct SupportMissionContextStampComponent : IComponentData
    { public Entity Source;public FixedString64Bytes SessionToken;public int AttemptOrdinal;public uint Revision,MissionSourceVersion; }
}
