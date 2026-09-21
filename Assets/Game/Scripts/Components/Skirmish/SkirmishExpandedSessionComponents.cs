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
        public int SupplyReserved;
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
}
