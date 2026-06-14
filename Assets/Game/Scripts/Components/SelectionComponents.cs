using Unity.Entities;

public struct SelectedUnitTag : IComponentData
{
}

public struct SelectionMarkerTag : IComponentData
{
}

public struct SelectionMarkerVisualChild : IComponentData
{
    public Entity Value;
    public float VisibleScale;
    public float VisibleScaleX;
    public float VisibleScaleZ;
}

public struct SelectionMarkerOwner : IComponentData
{
    public Entity Value;
}

public struct SelectionObjectOutlineTag : IComponentData
{
}

public struct SelectionObjectOutlineVisibleScale : IComponentData
{
    public float Value;
}

public struct SelectionObjectOutlineInstanceElement : IBufferElementData
{
    public Entity Value;
}

public struct SelectionMarkerAirOutlineFilteredTag : IComponentData
{
}

public struct ManualMoveOrderTag : IComponentData
{
}

public struct HoldPositionOrderTag : IComponentData
{
}

public struct ManualMoveGroupMemberTag : IComponentData
{
}

public struct AIControlledTag : IComponentData
{
}

public struct AICombatOrderTag : IComponentData
{
}

public struct ManualControlledTag : IComponentData
{
}

public struct FactionControlConfigTag : IComponentData
{
}

public struct FactionControlEntry : IBufferElementData
{
    public byte FactionId;
    public byte AIControlled;
    public byte IsPlayerFaction;
    public float LastLogTime;
}
