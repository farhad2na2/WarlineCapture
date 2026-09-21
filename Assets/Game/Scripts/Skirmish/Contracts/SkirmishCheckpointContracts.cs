using System;

namespace Game.Skirmish.Contracts
{
    public enum SkirmishCheckpointSchemaKind : byte
    {
        None = 0,
        ExpandedV1 = 1
    }

    public enum SkirmishCheckpointStatus : byte
    {
        None = 0,
        Requested = 1,
        Written = 2,
        Restored = 3,
        Rejected = 4
    }

    public enum SkirmishReplayMode : byte
    {
        None = 0,
        FreshStart = 1
    }

    [Serializable]
    public sealed class SkirmishCheckpointHeader
    {
        public int SchemaVersion = 1;
        public SkirmishCheckpointSchemaKind Kind = SkirmishCheckpointSchemaKind.ExpandedV1;
        public string SessionId = string.Empty;
        public string CatalogId = string.Empty;
        public string DefinitionId = string.Empty;
        public int ContentVersion;
        public uint SetupHash;
        public int SimulationTick;
        public string Checksum = string.Empty;
    }

    [Serializable]
    public sealed class SkirmishCheckpointActor
    {
        public string StableObjectId = string.Empty;
        public byte FactionId;
        public byte IsStructure;
        public int RoleKind;
        public int CurrentHealth;
        public int MaxHealth;
        public string PrefabKey = string.Empty;
    }

    [Serializable]
    public sealed class SkirmishCheckpointReservation
    {
        public uint ReservationId;
        public int RoleKind;
        public int Category;
        public int MemberCount;
        public int RemainingMembers;
        public int MaterialsPaid;
        public int SupplyCost;
        public int Phase;
        public byte FactionId;
    }

    [Serializable]
    public sealed class SkirmishCheckpointPayload
    {
        public string SessionId = string.Empty;
        public string CatalogId = string.Empty;
        public string DefinitionId = string.Empty;
        public int ContentVersion;
        public uint SetupHash;
        public int Seed;
        public SkirmishSizeId SizeId;
        public SkirmishDifficultyId DifficultyId;
        public SkirmishObjectiveKind ObjectiveKind;
        public SkirmishSessionPhase Phase;
        public byte IsLegacy;
        public byte IsCustom;
        public int SimulationTick;
        public float ElapsedSeconds;
        public int DeadlineSeconds;
        public byte Paused;
        public byte Playing;
        public int PlayerMaterials;
        public int PlayerOil;
        public int PlayerFuel;
        public int EnemyMaterials;
        public int EnemyOil;
        public int EnemyFuel;
        public int PlayerInfantryLive;
        public int PlayerGroundLive;
        public int PlayerSupplyLive;
        public int EnemyInfantryLive;
        public int EnemyGroundLive;
        public int EnemySupplyLive;
        public byte PlayerDesignatedAlive;
        public byte EnemyDesignatedAlive;
        public string PlayerBaseObjectId = string.Empty;
        public string EnemyBaseObjectId = string.Empty;
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind EndReason;
        public SkirmishCheckpointActor[] Actors = Array.Empty<SkirmishCheckpointActor>();
        public SkirmishCheckpointReservation[] Reservations = Array.Empty<SkirmishCheckpointReservation>();
    }

    [Serializable]
    public sealed class SkirmishCheckpointDocument
    {
        public SkirmishCheckpointHeader Header = new SkirmishCheckpointHeader();
        public SkirmishCheckpointPayload Payload = new SkirmishCheckpointPayload();
    }

    [Serializable]
    public sealed class SkirmishResultReceipt
    {
        public string SessionId = string.Empty;
        public string CatalogId = string.Empty;
        public string DefinitionId = string.Empty;
        public int ContentVersion;
        public SkirmishDifficultyId DifficultyId;
        public SkirmishSizeId SizeId;
        public uint SetupHash;
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind Reason;
        public float ElapsedSeconds;
        public int Seed;
        public byte Settled;
        public byte IsCustom;
        public byte IsLegacy;
    }

    [Serializable]
    public sealed class SkirmishReplayRequest
    {
        public string SourceSessionId = string.Empty;
        public string CatalogId = string.Empty;
        public SkirmishDifficultyId DifficultyId;
        public SkirmishSizeId SizeId;
        public int Seed;
        public SkirmishReplayMode Mode = SkirmishReplayMode.FreshStart;
    }
}
