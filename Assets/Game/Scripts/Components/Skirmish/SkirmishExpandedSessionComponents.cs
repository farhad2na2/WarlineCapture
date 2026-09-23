using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct SkirmishExpandedSessionComponent : IComponentData
    {
        public FixedString64Bytes SessionId;
        public FixedString64Bytes CatalogId;
        public FixedString64Bytes DefinitionId;
        public int ContentVersion;
        public uint SetupHash;
        public int Seed;
        public SkirmishSizeId SizeId;
        public SkirmishDifficultyId DifficultyId;
        public SkirmishObjectiveKind ObjectiveKind;
        public SkirmishSessionPhase Phase;
        public byte IsLegacy;
        public byte IsCustom;
        public byte InitializationComplete;
        public byte SpawnComplete;
        public byte SpawnVisualPending;
        public SkirmishReasonCode FailureCode;
        public int TickClock;
    }

    public struct SkirmishResolvedSetupComponent : IComponentData
    {
        public uint SetupHash;
        public int ContentVersion;
        public FixedString64Bytes DefinitionId;
        public int DeadlineSeconds;
        public int PlayerFaction;
        public int EnemyFaction;
    }

    public struct SkirmishAttemptOwnedComponent : IComponentData
    {
        public FixedString64Bytes SessionId;
        public FixedString64Bytes StableObjectId;
        public byte FactionId;
        public byte IsStructure;
        public byte CapacityReleased;
        public uint ReservationId;
    }

    public struct SkirmishObjectiveStateComponent : IComponentData
    {
        public SkirmishObjectiveKind Kind;
        public SkirmishObjectiveStateKind State;
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind Reason;
        public byte Terminal;
    }

    public struct SkirmishObjectiveRoleComponent : IComponentData
    {
        public SkirmishObjectiveRoleKind Role;
        public FixedString64Bytes StableObjectId;
        public byte FactionId;
    }

    public struct SkirmishUnitRoleComponent : IComponentData
    {
        public SkirmishRoleKind Role;
        public SkirmishPopulationCategory Category;
        public int SupplyCost;
    }

    public struct SkirmishRoleOverlayComponent : IComponentData
    {
        public SkirmishRoleKind Role;
        public int MaxHealth;
        public int Damage;
        public float RangeWorld;
        public SkirmishProducerKind Producer;
        public SkirmishTargetDomain TargetDomains;
        public byte Applied;
    }

    public struct SkirmishStructureIdentityComponent : IComponentData
    {
        public FixedString64Bytes StructureId;
        public SkirmishProducerKind Producer;
        public byte DesignatedBase;
    }

    public struct SkirmishObjectiveClockComponent : IComponentData
    {
        public float ElapsedSeconds;
        public int DeadlineSeconds;
        public byte Paused;
        public byte Playing;
    }

    public struct SkirmishBaseAssaultFactComponent : IComponentData
    {
        public byte PlayerDesignatedAlive;
        public byte EnemyDesignatedAlive;
        public byte ReplacementBarracksPresent;
        public byte FieldArmyWiped;
    }

    public struct SkirmishCapacityComponent : IComponentData
    {
        public byte FactionId;
        public int InfantryCap;
        public int GroundCap;
        public int AirCap;
        public int SupplyCap;
        public int InfantryLive;
        public int GroundLive;
        public int AirLive;
        public int SupplyLive;
        public int InfantryReserved;
        public int GroundReserved;
        public int AirReserved;
        public int SupplyReserved;
        public uint NextReservationId;
    }

    public struct SkirmishEconomyStockComponent : IComponentData
    {
        public byte FactionId;
        public int Materials;
        public int Oil;
        public int Fuel;
        public int MaterialsCapacity;
        public int OilCapacity;
        public int FuelCapacity;
    }

    public struct SkirmishProductionReservation : IBufferElementData
    {
        public uint ReservationId;
        public SkirmishRoleKind Role;
        public SkirmishPopulationCategory Category;
        public int MemberCount;
        public int RemainingMembers;
        public int MaterialsPaid;
        public int SupplyCost;
        public SkirmishReservationPhase Phase;
        public byte FactionId;
        public SkirmishProducerKind Producer;
    }

    public struct SkirmishUpgradeStampComponent : IComponentData
    {
        public byte InfantryWeapons;
        public byte VehicleProtection;
        public byte AircraftEfficiency;
    }

    public struct SkirmishResearchStateComponent : IComponentData
    {
        public byte FactionId;
        public SkirmishReadinessStage Readiness;
        public byte InfantryWeapons;
        public byte VehicleProtection;
        public byte AircraftEfficiency;
        public uint NextResearchId;
    }

    public struct SkirmishResearchQueueItem : IBufferElementData
    {
        public uint ResearchId;
        public SkirmishResearchKind Kind;
        public SkirmishResearchPhase Phase;
        public int MaterialsPaid;
        public float RemainingSeconds;
        public byte FactionId;
        public SkirmishProducerKind Producer;
    }

    public struct SkirmishResultComponent : IComponentData
    {
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind Reason;
        public uint SetupHash;
        public FixedString64Bytes SessionId;
        public byte Frozen;
        public byte SaveAcknowledged;
    }

    public struct SkirmishExpandedCleanupRequest : IComponentData
    {
        public byte RemoveAttemptOwned;
    }

    public struct SkirmishArmyGroupMembershipComponent : IComponentData
    {
        public uint GroupId;
        public SkirmishPopulationCategory Domain;
        public byte Leader;
    }

    public struct SkirmishArmyGroupRecord : IBufferElementData
    {
        public uint GroupId;
        public byte FactionId;
        public SkirmishRoleKind Role;
        public SkirmishPopulationCategory Domain;
        public int MemberCount;
        public int AliveCount;
        public SkirmishGroupOrderKind LastOrder;
        public byte Selected;
    }

    public struct SkirmishArmySelectionComponent : IComponentData
    {
        public int SelectedGroupCount;
        public int PageIndex;
        public int PageSize;
        public uint NextGroupId;
    }

    public struct SkirmishFogStateComponent : IComponentData
    {
        public byte SharedFog;
        public byte DevelopmentFullVision;
        public int LastSeenExpireSeconds;
    }

    public struct SkirmishContactSightComponent : IComponentData
    {
        public SkirmishContactSight Sight;
    }

    public struct SkirmishEnemyStockComponent : IComponentData
    {
        public int Materials;
        public int Oil;
        public int Fuel;
        public int MaterialsCapacity;
        public int OilCapacity;
        public int FuelCapacity;
    }

    public struct SkirmishEnemyCapacityComponent : IComponentData
    {
        public int InfantryCap;
        public int GroundCap;
        public int AirCap;
        public int SupplyCap;
        public int InfantryLive;
        public int GroundLive;
        public int AirLive;
        public int SupplyLive;
        public int InfantryReserved;
        public int GroundReserved;
        public int AirReserved;
        public int SupplyReserved;
        public uint NextReservationId;
    }

    public struct SkirmishEnemyStrategyComponent : IComponentData
    {
        public SkirmishStrategyPriority Priority;
        public SkirmishRoleKind RecruitRole;
        public int LastScore;
        public byte Personality;
        public int FailedAttempts;
        public uint LastGroupId;
        public byte CounterCommitted;
    }

    public struct SkirmishGroundStagingStateComponent : IComponentData
    {
        public int VehicleQueues;
        public int LogisticsQueues;
        public float SpawnPadX;
        public float SpawnPadZ;
        public float RallyPadX;
        public float RallyPadZ;
    }

    public struct SkirmishVisualSpawnedComponent : IComponentData
    {
        public byte Spawned;
        public byte FromRegistry;
    }

    public struct SkirmishMoveIntentComponent : IComponentData
    {
        public float DestinationX;
        public float DestinationZ;
        public byte Active;
        public SkirmishGroupOrderKind Order;
        public Entity AttackTarget;
        public float Cooldown;
        public byte Engaged;
    }

    public struct SkirmishObservedHealthComponent : IComponentData
    {
        public int LastSeenHealth;
        public int LastSeenMax;
        public float AgeSeconds;
        public byte Known;
    }

    public struct SkirmishArmyDrawerSlot : IBufferElementData
    {
        public uint GroupId;
        public SkirmishRoleKind Role;
        public int AliveCount;
        public byte Selected;
        public SkirmishGroupOrderKind LastOrder;
    }

    public struct SkirmishExpandedReplayRequest : IComponentData
    {
        public byte Requested;
    }

    public struct SkirmishExpandedPauseRequest : IComponentData
    {
        public byte Paused;
    }
}
