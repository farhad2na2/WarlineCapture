using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;

namespace Game.UI.Shell.Ecs
{
    public partial struct UiCampaignMissionProjectionSystem
    {
        private static int FindDefinitionIndex(
            ref CampaignMissionCatalogBlob catalog,
            in FixedString64Bytes missionId)
        {
            if (missionId.IsEmpty)
                return -1;
            for (int index = 0; index < catalog.Missions.Length; index++)
                if (catalog.Missions[index].MissionId.Equals(missionId))
                    return index;
            return -1;
        }

        private static UiCampaignOperationsComponent ProjectDefinition(
            uint catalogSourceVersion,
            uint settlementSourceVersion,
            ref CampaignMissionDefinitionBlob definition,
            CampaignMissionProgressSaveData[] progress,
            in UiCampaignOperationsComponent current)
        {
            bool m01 = definition.MissionId.Equals(new FixedString64Bytes(M01MissionId));
            FixedString64Bytes displayName = m01
                ? new FixedString64Bytes("M01 - FIRST CONTACT")
                : new FixedString64Bytes("M02 - ESTABLISH THE BASE");
            FixedString64Bytes nextMissionId = m01
                ? new FixedString64Bytes(M02MissionId)
                : default;
            return ProjectMission(
                catalogSourceVersion, settlementSourceVersion,
                definition.MissionId, definition.ScenarioId, definition.OperationMapId,
                displayName, nextMissionId, progress, in current);
        }

        private static UiCampaignOperationsComponent ProjectMission(
            uint catalogSourceVersion,
            uint settlementSourceVersion,
            FixedString64Bytes missionId,
            FixedString64Bytes scenarioId,
            FixedString64Bytes operationMapId,
            FixedString64Bytes displayName,
            FixedString64Bytes nextMissionId,
            CampaignMissionProgressSaveData[] progress,
            in UiCampaignOperationsComponent current)
        {
            CampaignMissionProgressSaveData entry = Find(progress, missionId);
            bool isM01 = missionId.Equals(new FixedString64Bytes(M01MissionId));
            bool available = entry != null ? entry.available : isM01;
            bool completed = entry != null && entry.firstClearCompleted;
            bool pending = entry != null && entry.pendingResume;
            UiCampaignMissionPrimaryActionKind action = !available
                ? UiCampaignMissionPrimaryActionKind.Locked
                : pending ? UiCampaignMissionPrimaryActionKind.Continue
                : completed ? UiCampaignMissionPrimaryActionKind.Replay
                : UiCampaignMissionPrimaryActionKind.Start;
            FixedString64Bytes label = new(action switch
            {
                UiCampaignMissionPrimaryActionKind.Start => "START OPERATION",
                UiCampaignMissionPrimaryActionKind.Continue => "CONTINUE",
                UiCampaignMissionPrimaryActionKind.Replay => "REPLAY",
                _ => "LOCKED"
            });

            UiCampaignOperationsComponent next = new()
            {
                CatalogSourceVersion = catalogSourceVersion,
                ProgressSourceVersion = HashProgress(progress),
                ObservedSettlementSourceVersion = settlementSourceVersion,
                SelectedMissionId = missionId,
                ScenarioId = scenarioId,
                OperationMapId = operationMapId,
                DisplayName = displayName,
                PrimaryActionLabel = label,
                NextMissionId = nextMissionId,
                BestStars = entry?.bestStars ?? 0,
                BestCompletionMilliseconds = entry?.bestCompletionMilliseconds ?? 0,
                SuccessfulReplayCount = entry?.successfulReplayCount ?? 0,
                LastAttemptOrdinal = entry?.lastAttemptOrdinal ?? -1,
                PrimaryAction = action,
                Available = available ? (byte)1 : (byte)0,
                FirstClearCompleted = completed ? (byte)1 : (byte)0,
                PendingResume = pending ? (byte)1 : (byte)0,
                NextMissionRevealed = !nextMissionId.IsEmpty &&
                                      Find(progress, nextMissionId)?.available == true
                    ? (byte)1
                    : (byte)0
            };
            next.Version = SameOperations(in current, in next)
                ? current.Version
                : NextVersion(current.Version);
            return next;
        }
    }
}
