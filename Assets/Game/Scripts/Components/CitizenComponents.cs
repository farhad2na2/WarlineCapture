using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    public struct CitizenTag : IComponentData
    {
    }

    public struct CitizenHouseholdTag : IComponentData
    {
    }

    public struct CitizenIdentity : IComponentData
    {
        public int CitizenId;
        public byte Gender;
        public byte LifeState;
        public byte Status;
    }

    public struct CitizenHouseholdRef : IComponentData
    {
        public int HouseholdId;
        public Entity HouseholdEntity;
    }

    public struct CitizenHomeTarget : IComponentData
    {
        public int HomeBuildingId;
        public int CurrentTargetBuildingId;
    }

    public struct CitizenAssignmentsComponent : IComponentData
    {
        public int WorkBuildingId;
        public int PreferredShopBuildingId;
        public int LunchShopBuildingId;
        public int PreferredWalkBuildingId;
        public int PreferredCityHallBuildingId;
    }

    public struct CitizenTimersComponent : IComponentData
    {
        public float StateStartedAt;
        public float StateEndsAt;
    }

    public struct CitizenHouseholdComponent : IComponentData
    {
        public int HouseholdId;
        public int HomeBuildingId;
        public int MaleCitizenId;
        public int FemaleCitizenId;
        public int RefugeeTentBuildingId;
        public byte IsDisplaced;
    }

    public struct CitizenPopulationSummary : IComponentData
    {
        public int Households;
        public int TotalCitizens;
        public int HousedCitizens;
        public int RefugeeCitizens;
        public int DeadCitizens;
    }

    public struct CitizenPopulationSummaryTag : IComponentData
    {
    }

    public struct CivilianUnitTag : IComponentData
    {
    }

    public struct CitizenVisibleUnitState : IComponentData
    {
        public int CitizenId;
        public FixedString64Bytes SourceKey;
        public byte OwnerFactionId;
        public CitizenLifeState LifeState;
        public CitizenStatus Status;
        public int TargetBuildingId;
        public int2 GoalCell;
    }

    public struct CitizenMovementCommandQueueComponent : IComponentData
    {
        public int LastRequestId;
    }

    public struct CitizenMoveCommandRequestElement : IBufferElementData
    {
        public int RequestId;
        public Entity UnitEntity;
        public int2 Goal;
    }

    public struct CitizenMoveCommandResultElement : IBufferElementData
    {
        public int RequestId;
        public Entity UnitEntity;
        public int2 Goal;
        public byte Accepted;
    }
}
