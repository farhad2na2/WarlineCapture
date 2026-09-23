using System;
using Game.Skirmish.Contracts;

namespace Game.Configs
{
    [Serializable]
    public struct SkirmishResolvedForceEntry
    {
        public byte FactionId;
        public string RoleId;
        public SkirmishRoleKind RoleKind;
        public string RuntimePrefabKey;
        public string SourceConfigPath;
        public int Quantity;
        public int SupplyCost;
        public string SpawnAnchorId;
        public string ObjectiveRoleId;
        public float SpawnWorldX;
        public float SpawnWorldZ;
    }

    [Serializable]
    public struct SkirmishResolvedStructureEntry
    {
        public byte FactionId;
        public string StructureId;
        public string SpawnAnchorId;
        public string ObjectiveRoleId;
        public bool DesignatedBase;
        public float SpawnWorldX;
        public float SpawnWorldZ;
        public float AcrossOffsetMetres;
        public float ForwardOffsetMetres;
    }

    [Serializable]
    public sealed class SkirmishResolvedSetup
    {
        public string CatalogId;
        public string DefinitionId;
        public string ScenarioSetupId;
        public string OperationMapId;
        public string LayoutId;
        public int ContentVersion;
        public string ContentHash;
        public uint SetupHash;
        public int Seed;
        public SkirmishDifficultyId DifficultyId;
        public SkirmishSizeId SizeId;
        public SkirmishArmyProfileId ArmyProfileId;
        public SkirmishStartPackageId StartPackageId;
        public SkirmishObjectiveKind ObjectiveKind;
        public SkirmishReadinessStage Readiness;
        public int DeadlineSeconds;
        public int PlayerFaction = 1;
        public int EnemyFaction = 2;
        public int MaterialsEach;
        public int OilEach;
        public int UsableFuelEach;
        public int MaterialsCapacityEach;
        public int OilCapacityEach;
        public int FuelCapacityEach;
        public int InfantryQueuesEach;
        public int VehicleQueuesEach;
        public int LogisticsQueuesEach;
        public int RefineryModulesEach;
        public int InfantryCapEach;
        public int GroundCapEach;
        public int TacticalAirCapEach;
        public int SupplyCapEach;
        public int LogisticsSupportCapEach;
        public int DeliveryCarrierCapEach;
        public int ObjectiveSupportExtraPlayer;
        public int PlayerBuiltStructureCapEach;
        public int BarrierSegmentCapEach;
        public int PlayerInfantry;
        public int PlayerGround;
        public int PlayerAir;
        public int PlayerCombat;
        public int PlayerSupply;
        public int PlayerLogisticsSupport;
        public int PlayerObjectiveTrucks;
        public int PlayerStartingStructures;
        public int PlayerDesignatedRifles;
        public int EnemyInfantry;
        public int EnemyGround;
        public int EnemyAir;
        public int EnemyCombat;
        public int EnemySupply;
        public int EnemyLogisticsSupport;
        public int EnemyObjectiveTrucks;
        public int EnemyStartingStructures;
        public int EnemyDesignatedRifles;
        public SkirmishResolvedForceEntry[] Forces = Array.Empty<SkirmishResolvedForceEntry>();
        public SkirmishResolvedStructureEntry[] Structures = Array.Empty<SkirmishResolvedStructureEntry>();
        public SkirmishRoleOverlay[] RoleOverlays = Array.Empty<SkirmishRoleOverlay>();
        public string PlayerBaseObjectId = "obj.base.player";
        public string EnemyBaseObjectId = "obj.base.enemy";
        public bool SharedFog = true;
        public bool DevelopmentFullVision = true;
        public int LastSeenExpireSeconds = 20;
        public string CheckpointVersion = "skirmish.checkpoint.v1";
        public string Report = string.Empty;
        // Localized copy keys compiled from the definition, so HUD/library copy
        // resolves for any expanded entry instead of a hard-coded scenario.
        public string TitleKey = string.Empty;
        public string BriefingKey = string.Empty;
        public string ObjectiveKey = string.Empty;
        public string ResultVictoryKey = string.Empty;
        public string ResultDefeatKey = string.Empty;
        public bool MeasuredLayoutBound;
        public float WorldWidthMetres;
        public float WorldDepthMetres;
        public float GridOriginX;
        public float GridOriginZ;
        public float CellSize;
        public float PlayerBaseWorldX;
        public float PlayerBaseWorldZ;
        public float EnemyBaseWorldX;
        public float EnemyBaseWorldZ;
        public float PlayerStagingWorldX;
        public float PlayerStagingWorldZ;
        public float EnemyStagingWorldX;
        public float EnemyStagingWorldZ;
        public float PlayerSpawnPadX;
        public float PlayerSpawnPadZ;
        public float PlayerRallyPadX;
        public float PlayerRallyPadZ;
        public float EnemySpawnPadX;
        public float EnemySpawnPadZ;
        public float EnemyRallyPadX;
        public float EnemyRallyPadZ;
        public string DefaultRouteId = string.Empty;
    }
}
