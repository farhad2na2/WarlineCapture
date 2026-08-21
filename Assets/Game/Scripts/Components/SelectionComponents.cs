using Unity.Entities;

namespace Game.Components
{
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

    /// <summary>
    /// Baked references to the mutually exclusive visuals inside the shared infantry/vehicle
    /// selection marker. Runtime entity names are diagnostic only and must not decide which
    /// renderer is visible.
    /// </summary>
    public struct SelectionMarkerVariantVisuals : IComponentData
    {
        public Entity InfantryGroundRing;
        public Entity VehicleFootprintFill;
        public Entity VehicleCornerBrackets;
        public Entity VehicleBoundsFrame;
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

    /// <summary>
    /// Marks a selection marker whose optional object-outline sources have already been
    /// resolved. Infantry deliberately use only their authored ground ring, so an empty
    /// outline buffer is a valid completed result rather than a reason to rescan the full
    /// render hierarchy every presentation frame.
    /// </summary>
    public struct SelectionObjectOutlineResolvedTag : IComponentData
    {
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
}
