using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Components
{
    public struct CampaignMissionRootComponent : IComponentData
    {
    }

    public struct CampaignMissionCatalogComponent : IComponentData
    {
        public BlobAssetReference<CampaignMissionCatalogBlob> Blob;
        public uint SourceVersion;
        public byte OwnsBlob;
    }

    public struct CampaignMissionLaunchQueueComponent : IComponentData
    {
        public ulong LastTransitionToken;
        public uint Version;
    }

    [InternalBufferCapacity(2)]
    public struct CampaignMissionLaunchRequestElement : IBufferElementData
    {
        public int SchemaVersion;
        public FixedString64Bytes MissionId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes OperationMapId;
        public MissionLaunchOriginKind LaunchOrigin;
        public MissionRunKind RunKind;
        public NarrativeGuidanceMode Guidance;
        public byte ReplayTutorialEnabled;
        public ulong TransitionToken;
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public int DeterministicSeed;
    }

    [InternalBufferCapacity(2)]
    public struct CampaignMissionLaunchResultElement : IBufferElementData
    {
        public ulong TransitionToken;
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public byte Accepted;
        public FixedString64Bytes ReasonCode;
    }

    public struct CampaignMissionRuntimeComponent : IComponentData
    {
        public FixedString64Bytes MissionId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes SessionToken;
        public MissionPhaseKind Phase;
        public MissionOutcomeKind Outcome;
        public MissionLaunchOriginKind LaunchOrigin;
        public MissionRunKind RunKind;
        public NarrativeGuidanceMode Guidance;
        public MissionReturnDestinationKind ReturnDestination;
        public ulong TransitionToken;
        public uint Version;
        public uint SourceVersion;
        public int AttemptOrdinal;
        public int DeterministicSeed;
        public OperationMapReadinessFlags RequiredReadiness;
        public OperationMapReadinessFlags ReadyReadiness;
        public byte ReplayTutorialEnabled;
    }

    public struct CampaignMissionAttemptFactsComponent : IComponentData
    {
        public int ElapsedMilliseconds;
        public int SquadLossCount;
        public int HostileTotalCount;
        public int HostileDefeatedCount;
        public byte CommandSquadSpawned;
        public byte CommandSquadAlive;
        public byte MoveToCoverComplete;
        public byte ThreatConfirmed;
        public byte AttackIssued;
    }

    [InternalBufferCapacity(4)]
    public struct CampaignMissionActionRequestElement : IBufferElementData
    {
        public MissionActionKind Action;
        public ulong TransitionToken;
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public byte ReplayTutorialEnabled;
    }

    [InternalBufferCapacity(4)]
    public struct CampaignMissionActionResultElement : IBufferElementData
    {
        public MissionActionKind Action;
        public byte Accepted;
        public ulong TransitionToken;
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public FixedString64Bytes ReasonCode;
    }

    public struct CampaignMissionResultComponent : IComponentData
    {
        public FixedString64Bytes MissionId;
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public MissionOutcomeKind Outcome;
        public MissionReturnDestinationKind ReturnDestination;
        public byte Stars;
        public int ElapsedMilliseconds;
        public int SquadLossCount;
    }

    [InternalBufferCapacity(1)]
    public struct CampaignMissionSettlementRequestElement : IBufferElementData
    {
        public uint SourceVersion;
        public FixedString64Bytes MissionId;
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public MissionOutcomeKind Outcome;
    }

    [InternalBufferCapacity(1)]
    public struct CampaignMissionSettlementResultElement : IBufferElementData
    {
        public uint SourceVersion;
        public FixedString64Bytes SessionToken;
        public byte Accepted;
        public FixedString64Bytes ReasonCode;
    }

    public struct CampaignMissionUnitRoleComponent : IComponentData
    {
        public FixedString64Bytes MissionRoleId;
        public FixedString64Bytes UnitGroupId;
        public FixedString64Bytes RouteId;
        public FixedString64Bytes SessionToken;
        public int RouteIndex;
        public uint PatrolOrderVersion;
    }

    public struct CampaignMissionAmbientCivilianComponent : IComponentData
    {
        public FixedString64Bytes PresentationId;
        public FixedString64Bytes RouteId;
        public FixedString64Bytes SessionToken;
        public int RouteIndex;
        public int AttemptOrdinal;
        public byte Evacuating;
    }

    public struct CampaignMissionCatalogBlob
    {
        public int SchemaVersion;
        public BlobArray<CampaignMissionDefinitionBlob> Missions;
    }

    public struct CampaignMissionDefinitionBlob
    {
        public FixedString64Bytes MissionId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes OperationMapId;
        public int SchemaVersion;
        public int DeterministicSeed;
        public int EncounterStartMilliseconds;
        public byte BuildingDisabled;
        public byte ProductionDisabled;
        public byte EconomyDisabled;
        public byte TransportDisabled;
        public byte AirDisabled;
        public BlobArray<CampaignMissionForceGroupBlob> ForceGroups;
        public BlobArray<CampaignMissionPatrolRouteBlob> PatrolRoutes;
        public BlobArray<CampaignMissionAmbientPresentationBlob> AmbientPresentations;
        public BlobArray<CampaignMissionStarRuleBlob> StarRules;
        public BlobArray<CampaignMissionRewardBlob> FirstClearRewards;
        public BlobArray<CampaignMissionRewardBlob> ReplayRewards;
    }

    public struct CampaignMissionForceGroupBlob
    {
        public FixedString64Bytes GroupId;
        public byte FactionId;
        public BlobArray<CampaignMissionForceUnitBlob> Units;
    }

    public struct CampaignMissionForceUnitBlob
    {
        public FixedString64Bytes SourceKey;
        public FixedString64Bytes RuntimePrefabSourceKey;
        public FixedString64Bytes ExpectedAssetGuid;
        public FixedString64Bytes SpawnAnchorId;
        public FixedString64Bytes MissionRoleId;
        public int Count;
    }

    public struct CampaignMissionPatrolRouteBlob
    {
        public FixedString64Bytes RouteId;
        public FixedString64Bytes UnitGroupId;
        public int StartDelayMilliseconds;
        public BlobArray<FixedString64Bytes> AnchorIds;
    }

    public struct CampaignMissionAmbientPresentationBlob
    {
        public FixedString64Bytes PresentationId;
        public FixedString64Bytes AnchorId;
        public FixedString64Bytes RouteId;
        public int InstanceCount;
    }

    public struct CampaignMissionStarRuleBlob
    {
        public FixedString64Bytes DisplayTextKey;
        public MissionStarRuleKind Rule;
        public int Threshold;
        public byte StarIndex;
    }

    public struct CampaignMissionRewardBlob
    {
        public MissionRewardKind Kind;
        public FixedString64Bytes RewardConfigId;
        public int Amount;
    }
}
