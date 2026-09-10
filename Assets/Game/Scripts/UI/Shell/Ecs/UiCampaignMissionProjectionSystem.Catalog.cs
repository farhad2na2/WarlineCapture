using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    public partial struct UiCampaignMissionProjectionSystem
    {
        private static bool EnsureChapterProgressionCompatibility(CampaignMissionProgressStore store)
        {
            bool changed=store.EnsureAvailableAfterFirstClear(M01MissionId,M02MissionId);
            changed|=store.EnsureAvailableAfterFirstClear(M02MissionId,"saga.ch01.m03.radar_warning");
            changed|=store.EnsureAvailableAfterFirstClear("saga.ch01.m03.radar_warning","saga.ch01.m04.airlift");
            return changed;
        }
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

        private static int FindDefaultDefinitionIndex(
            ref CampaignMissionCatalogBlob catalog,
            CampaignMissionProgressSaveData[] progress)
        {
            int latestAvailableIndex = -1;
            for (int index = 0; index < catalog.Missions.Length; index++)
            {
                ref CampaignMissionDefinitionBlob definition = ref catalog.Missions[index];
                if (!IsDefinitionAvailable(ref definition, progress))
                    continue;
                latestAvailableIndex = index;
                CampaignMissionProgressSaveData entry = Find(progress, definition.MissionId);
                if (entry == null || !entry.firstClearCompleted)
                    return index;
            }

            return latestAvailableIndex >= 0 ? latestAvailableIndex : 0;
        }

        private static UiCampaignOperationsComponent ProjectDefinition(
            uint catalogSourceVersion,
            uint settlementSourceVersion,
            ref CampaignMissionDefinitionBlob definition,
            CampaignMissionProgressSaveData[] progress,
            in UiCampaignOperationsComponent current, byte availableMissionMask)
        {
            bool m01 = definition.MissionId.Equals(new FixedString64Bytes(M01MissionId));
            bool m03 = definition.Defense.Enabled != 0;
            bool m04 = definition.Extraction.Enabled != 0;
            FixedString64Bytes displayName = m04 ? new FixedString64Bytes("M04 - AIRLIFT") : m03 ? new FixedString64Bytes("M03 - RADAR WARNING") : m01
                ? new FixedString64Bytes("M01 - FIRST CONTACT")
                : new FixedString64Bytes("M02 - ESTABLISH THE BASE");
            FixedString64Bytes nextMissionId = m04 ? default : m01
                ? new FixedString64Bytes(M02MissionId)
                : new FixedString64Bytes(m03 ? "saga.ch01.m04.airlift" : "saga.ch01.m03.radar_warning");
            return ProjectMission(
                catalogSourceVersion, settlementSourceVersion,
                definition.MissionId, definition.ScenarioId, definition.OperationMapId,
                displayName, nextMissionId, progress, in current, availableMissionMask);
        }

        private static byte AvailableMissionMask(ref CampaignMissionCatalogBlob catalog, CampaignMissionProgressSaveData[] progress)
        {
            byte mask = 0;
            for (int i = 0; i < catalog.Missions.Length; i++)
            {
                ref CampaignMissionDefinitionBlob mission = ref catalog.Missions[i];
                if (!IsDefinitionAvailable(ref mission, progress)) continue;
                int index = mission.MissionId.Equals(new FixedString64Bytes(M01MissionId)) ? 0 :
                    mission.MissionId.Equals(new FixedString64Bytes(M02MissionId)) ? 1 :
                    mission.MissionId.Equals(new FixedString64Bytes("saga.ch01.m03.radar_warning")) ? 2 :
                    mission.MissionId.Equals(new FixedString64Bytes("saga.ch01.m04.airlift")) ? 3 : -1;
                if (index >= 0) mask |= (byte)(1 << index);
            }
            return mask;
        }

        private static byte CompletedMissionMask(CampaignMissionProgressSaveData[] progress)
        {
            byte mask = 0;
            string[] ids = { M01MissionId, M02MissionId, "saga.ch01.m03.radar_warning", "saga.ch01.m04.airlift" };
            for (int i = 0; i < ids.Length; i++)
                if (Find(progress, new FixedString64Bytes(ids[i]))?.firstClearCompleted == true)
                    mask |= (byte)(1 << i);
            return mask;
        }

        private static bool IsDefinitionAvailable(
            ref CampaignMissionDefinitionBlob definition,
            CampaignMissionProgressSaveData[] progress)
        {
            CampaignMissionProgressSaveData entry = Find(progress, definition.MissionId);
            return entry != null
                ? entry.available
                : definition.MissionId.Equals(new FixedString64Bytes(M01MissionId));
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
            in UiCampaignOperationsComponent current, byte availableMissionMask = 0)
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
                AvailableMissionMask = availableMissionMask,
                CompletedMissionMask = CompletedMissionMask(progress),
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
        private static uint ReadLatestSettlementSourceVersion(EntityManager entityManager, Entity campaignRoot)
        {
            if (!entityManager.HasBuffer<CampaignMissionSettlementResultElement>(campaignRoot)) return 0;
            DynamicBuffer<CampaignMissionSettlementResultElement> results =
                entityManager.GetBuffer<CampaignMissionSettlementResultElement>(campaignRoot, true);
            uint version = 0;
            for (int index = 0; index < results.Length; index++)
                if (results[index].SourceVersion > version) version = results[index].SourceVersion;
            return version;
        }

        private static uint HashProgress(CampaignMissionProgressSaveData[] progress)
        {
            uint hash = 2166136261u;
            if (progress == null) return hash;
            for (int index = 0; index < progress.Length; index++)
            {
                CampaignMissionProgressSaveData entry = progress[index];
                if (entry == null) continue;
                Hash(ref hash, entry.missionId);
                Hash(ref hash, entry.available ? 1 : 0);
                Hash(ref hash, entry.firstClearCompleted ? 1 : 0);
                Hash(ref hash, entry.pendingResume ? 1 : 0);
                Hash(ref hash, entry.bestStars);
                Hash(ref hash, entry.bestCompletionMilliseconds);
                Hash(ref hash, entry.successfulReplayCount);
                Hash(ref hash, entry.lastAttemptOrdinal);
            }
            return hash;
        }

        private static void Hash(ref uint hash, string value)
        {
            if (value == null) return;
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= 16777619u;
            }
        }

        private static void Hash(ref uint hash, int value)
        {
            hash ^= unchecked((uint)value);
            hash *= 16777619u;
        }

    }
}
