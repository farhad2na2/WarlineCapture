using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public struct MissionRuntimeEntityId : IComponentData
{
    public FixedString64Bytes Value;
}

public struct ActiveMissionSessionComponent : IComponentData
{
    public FixedString128Bytes MissionId;
    public int ReturnRoute;
}

public struct MissionRuntimeCommandSquadTag : IComponentData
{
}

public struct MissionRuntimeEnemyPatrolTag : IComponentData
{
}

public struct MissionRuntimeObjectiveTarget : IComponentData
{
    public FixedString64Bytes ObjectiveId;
}

public struct MissionRuntimeEcsVisualTag : IComponentData
{
}

public struct MissionRuntimeSelectionMarkerVisualTag : IComponentData
{
}

public struct MissionRuntimeTargetMarkerVisualTag : IComponentData
{
}

public struct MissionRuntimePatrolRoute : IComponentData
{
    public int2 WaypointA;
    public int2 WaypointB;
    public int2 WaypointC;
    public byte WaypointCount;
    public byte CurrentWaypointIndex;
    public byte HoldAtEnd;
}

public struct MissionRuntimeOpeningControlProtection : IComponentData
{
}
