using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Missions.Contracts;

namespace Game.Components
{
    public struct CampaignMissionDormantMapDefenseTag : IComponentData { }
    public struct CampaignMissionDormantMapUnitTag : IComponentData { }

    public struct CampaignMissionConvoyRouteProgress : IComponentData
    {
        public float2 LastProgressPosition;
        public int LastProgressAtMilliseconds;
        public int NextRetryAtMilliseconds;
    }

    public struct CampaignMissionDefenseDefinitionBlob
    {
        public byte Enabled;
        public byte VehiclesSelfSupplied;
        public byte AuthoredMapDefensesDormant;
        public FixedString128Bytes ForwardPostStableId;
        public FixedString64Bytes InnerCoreAnchorId;
        public float InnerCoreRadius;
        public FixedString64Bytes SensorMissionRoleId;
        public FixedString64Bytes InitialProducerAnchorId;
        public FixedString128Bytes InitialProducerConfigId;
        public int RadarPingCharges;
        public int RadarPingCooldownMilliseconds;
        public BlobArray<CampaignMissionConvoyElementBlob> Elements;
        public BlobArray<CampaignMissionGuidanceStepBlob> GuidanceSteps;
        public MissionCameraTourBlob CameraTour;
    }

    public struct CampaignMissionGuidanceStepBlob
    {
        public FixedString64Bytes StepId, TitleKey, BodyKey;
        public MissionGuidanceActionKind Action;
        public MissionGuidanceCompletionKind Completion;
        public byte Optional;
    }

    public struct CampaignMissionConvoyElementBlob
    {
        public FixedString64Bytes ElementId;
        public FixedString64Bytes UnitGroupId;
        public FixedString64Bytes RouteId;
        public FixedString64Bytes ContactAnchorId;
        public int WarningAtMilliseconds;
        public int ActivationAtMilliseconds;
        public int ContactAtMilliseconds;
    }

    public struct CampaignMissionDefenseStateComponent : IComponentData
    {
        public FixedString64Bytes SessionToken;
        public int AttemptOrdinal;
        public uint SourceVersion;
        public float3 InnerCoreCenter;
        public int InitialProducerRequestId;
        public int RuntimeBuildingBaselineId;
        public int ProducedUnitBaselineCount;
        public int NextGuidanceAtMilliseconds;
        public uint AcknowledgedGuidanceMask;
        public byte Initialized;
        public byte OpeningComplete;
        public byte InitialProducerReady;
        public byte PlayerRequestedFocus;
        public byte FocusReturnAvailable;
        public float3 FocusReturnPosition;
        public float4 FocusReturnPerspective;
        public byte PingUsed;
        public int LastSelectionCommandRequestId;
        public int LastMoveOrderRequestId;
        public byte SelectionCommandBaselineSet;
        public byte StopAccepted;
        public byte MoveAccepted;
        public int ReinforcedRifleCount;
    }

    [InternalBufferCapacity(4)]
    public struct CampaignMissionConvoyElementState : IBufferElementData
    {
        public byte WarningIssued;
        public byte Activated;
        public byte Resolved;
    }

    [InternalBufferCapacity(24)]
    public struct CampaignMissionDefenseMember : IBufferElementData
    {
        public Entity Entity;
        public int ElementIndex;
        public byte FactionId;
        public byte HealthInitialized;
        public byte Defeated;
        public byte IsSensor;
    }
}
