using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Unity.Collections;
using Unity.Entities;

namespace Game.Composition
{
    internal sealed class CampaignMissionOperationMapReferenceComponent : IComponentData
    {
        public FixedString64Bytes MissionId;
        public FixedString64Bytes ScenarioId;
        public FixedString64Bytes OperationMapId;
        public FixedString64Bytes SessionToken;
        public OperationMapDefinition Definition;
        public ulong TransitionToken;
        public uint SourceVersion;
        public int AttemptOrdinal;
    }

    internal readonly struct OperationMapLaunchSelection
    {
        public OperationMapLaunchSelection(
            FixedString64Bytes missionId,
            FixedString64Bytes scenarioId,
            FixedString64Bytes operationMapId,
            OperationMapDefinition definition,
            bool isCampaign)
        {
            MissionId = missionId;
            ScenarioId = scenarioId;
            OperationMapId = operationMapId;
            Definition = definition;
            IsCampaign = isCampaign;
        }

        public FixedString64Bytes MissionId { get; }
        public FixedString64Bytes ScenarioId { get; }
        public FixedString64Bytes OperationMapId { get; }
        public OperationMapDefinition Definition { get; }
        public bool IsCampaign { get; }
    }

}
