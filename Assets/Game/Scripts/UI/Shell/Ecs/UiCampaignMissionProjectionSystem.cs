using Game.Components;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;

namespace Game.UI.Shell.Ecs
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UiCampaignMissionProjectionSystem : ISystem
    {
        public const string M01MissionId = "saga.ch01.m01.first_contact";
        public const string M02MissionId = "saga.ch01.m02.establish_base";

        private EntityQuery _uiRootQuery;
        private EntityQuery _campaignRootQuery;

        public void OnCreate(ref SystemState state)
        {
            _uiRootQuery = state.GetEntityQuery(ComponentType.ReadOnly<UiShellRootComponent>());
            _campaignRootQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                ComponentType.ReadOnly<CampaignMissionCatalogComponent>(),
                ComponentType.ReadOnly<CampaignMissionProgressStoreReferenceComponent>());
            state.RequireForUpdate(_uiRootQuery);
            state.RequireForUpdate(_campaignRootQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_uiRootQuery.CalculateEntityCount() != 1 || _campaignRootQuery.CalculateEntityCount() != 1)
                return;

            EntityManager entityManager = state.EntityManager;
            Entity uiRoot = _uiRootQuery.GetSingletonEntity();
            Entity campaignRoot = _campaignRootQuery.GetSingletonEntity();
            if (!entityManager.HasComponent<UiCampaignOperationsComponent>(uiRoot))
                entityManager.AddComponentData(uiRoot, default(UiCampaignOperationsComponent));
            if (!entityManager.HasBuffer<UiCampaignMissionActionRequestElement>(uiRoot))
                entityManager.AddBuffer<UiCampaignMissionActionRequestElement>(uiRoot);

            UiCampaignOperationsComponent current =
                entityManager.GetComponentData<UiCampaignOperationsComponent>(uiRoot);
            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(campaignRoot);
            if (!catalog.Blob.IsCreated || catalog.Blob.Value.Missions.Length == 0)
                return;

            DynamicBuffer<UiCampaignMissionActionRequestElement> requests =
                entityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot);
            bool refreshRequested = requests.Length > 0;
            FixedString64Bytes selectedMissionId = current.SelectedMissionId;
            for (int index = 0; index < requests.Length; index++)
            {
                UiCampaignMissionActionRequestElement request = requests[index];
                if (request.Action == UiCampaignMissionActionKind.Select &&
                    request.MissionId.Equals(new FixedString64Bytes(M01MissionId)))
                    selectedMissionId = request.MissionId;
            }
            requests.Clear();

            uint settlementSourceVersion = ReadLatestSettlementSourceVersion(entityManager, campaignRoot);
            bool sourceChanged = current.Version == 0 || current.CatalogSourceVersion != catalog.SourceVersion ||
                                 current.ObservedSettlementSourceVersion != settlementSourceVersion;
            if (!sourceChanged && !refreshRequested)
                return;

            CampaignMissionProgressStore store = entityManager
                .GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(campaignRoot).Store;
            if (store == null)
                return;

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[0];
            UiCampaignOperationsComponent next = Project(
                catalog.SourceVersion, settlementSourceVersion,
                definition.MissionId, definition.ScenarioId, definition.OperationMapId,
                store.ReadAll(), in current);
            if (!selectedMissionId.IsEmpty && selectedMissionId.Equals(next.SelectedMissionId))
                next.SelectedMissionId = selectedMissionId;
            entityManager.SetComponentData(uiRoot, next);
        }

        public static UiCampaignOperationsComponent Project(
            uint catalogSourceVersion,
            uint settlementSourceVersion,
            FixedString64Bytes missionId,
            FixedString64Bytes scenarioId,
            FixedString64Bytes operationMapId,
            CampaignMissionProgressSaveData[] progress,
            in UiCampaignOperationsComponent current)
        {
            CampaignMissionProgressSaveData m01 = Find(progress, M01MissionId);
            CampaignMissionProgressSaveData m02 = Find(progress, M02MissionId);
            bool available = m01 == null || m01.available;
            bool completed = m01 != null && m01.firstClearCompleted;
            bool pending = m01 != null && m01.pendingResume;
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
                DisplayName = new FixedString64Bytes("M01 - FIRST CONTACT"),
                PrimaryActionLabel = label,
                NextMissionId = new FixedString64Bytes(M02MissionId),
                BestStars = m01?.bestStars ?? 0,
                BestCompletionMilliseconds = m01?.bestCompletionMilliseconds ?? 0,
                SuccessfulReplayCount = m01?.successfulReplayCount ?? 0,
                PrimaryAction = action,
                Available = available ? (byte)1 : (byte)0,
                FirstClearCompleted = completed ? (byte)1 : (byte)0,
                PendingResume = pending ? (byte)1 : (byte)0,
                NextMissionRevealed = m02 != null && m02.available ? (byte)1 : (byte)0
            };
            next.Version = HasSameProjection(in current, in next)
                ? current.Version
                : current.Version == uint.MaxValue ? 1u : current.Version + 1u;
            return next;
        }

        private static CampaignMissionProgressSaveData Find(
            CampaignMissionProgressSaveData[] progress, string missionId)
        {
            if (progress == null) return null;
            for (int index = 0; index < progress.Length; index++)
                if (progress[index]?.missionId == missionId) return progress[index];
            return null;
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

        private static bool HasSameProjection(
            in UiCampaignOperationsComponent left, in UiCampaignOperationsComponent right) =>
            left.CatalogSourceVersion == right.CatalogSourceVersion &&
            left.ProgressSourceVersion == right.ProgressSourceVersion &&
            left.SelectedMissionId.Equals(right.SelectedMissionId) && left.ScenarioId.Equals(right.ScenarioId) &&
            left.OperationMapId.Equals(right.OperationMapId) && left.PrimaryAction == right.PrimaryAction &&
            left.BestStars == right.BestStars &&
            left.BestCompletionMilliseconds == right.BestCompletionMilliseconds &&
            left.SuccessfulReplayCount == right.SuccessfulReplayCount && left.Available == right.Available &&
            left.FirstClearCompleted == right.FirstClearCompleted && left.PendingResume == right.PendingResume &&
            left.NextMissionRevealed == right.NextMissionRevealed;
    }
}
