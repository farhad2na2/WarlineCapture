using Game.Components;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
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
            EnsureUiBoundary(entityManager, uiRoot);

            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(campaignRoot);
            if (!catalog.Blob.IsCreated || catalog.Blob.Value.Missions.Length != 1)
                return;
            CampaignMissionProgressStore store = entityManager
                .GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(campaignRoot).Store;
            if (store == null)
                return;

            UiCampaignOperationsComponent current =
                entityManager.GetComponentData<UiCampaignOperationsComponent>(uiRoot);
            UiMissionBriefingComponent currentBriefing =
                entityManager.GetComponentData<UiMissionBriefingComponent>(uiRoot);
            UiMissionBriefingComponent storedBriefing = currentBriefing;
            if (currentBriefing.DeployQueued != 0 && IsLaunchTerminal(
                    entityManager, campaignRoot, currentBriefing.DeployTransitionToken))
            {
                currentBriefing.DeployQueued = 0;
                currentBriefing.DeployTransitionToken = 0;
                currentBriefing.Version = NextVersion(currentBriefing.Version);
            }
            DynamicBuffer<UiCampaignMissionActionRequestElement> requests =
                entityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot);
            CampaignMissionProgressSaveData[] progress = store.ReadAll();
            uint settlementSourceVersion = ReadLatestSettlementSourceVersion(entityManager, campaignRoot);
            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[0];

            UiCampaignOperationsComponent next = Project(
                catalog.SourceVersion, settlementSourceVersion,
                definition.MissionId, definition.ScenarioId, definition.OperationMapId,
                progress, in current);
            bool replayTutorial = currentBriefing.Version != 0 &&
                                  currentBriefing.MissionId.Equals(definition.MissionId)
                ? currentBriefing.ReplayTutorialEnabled != 0
                : definition.ReplayTutorialDefaultEnabled != 0;
            bool deployRequested = false;
            bool actionRequested = requests.Length > 0;
            for (int index = 0; index < requests.Length; index++)
            {
                UiCampaignMissionActionRequestElement request = requests[index];
                if (!request.MissionId.Equals(definition.MissionId))
                    continue;
                switch (request.Action)
                {
                    case UiCampaignMissionActionKind.Select:
                    case UiCampaignMissionActionKind.OpenBriefing:
                        next.SelectedMissionId = request.MissionId;
                        break;
                    case UiCampaignMissionActionKind.SetReplayTutorial:
                        if (next.FirstClearCompleted != 0 && definition.ReplayAllowed != 0)
                            replayTutorial = request.Value != 0;
                        break;
                    case UiCampaignMissionActionKind.Deploy:
                        deployRequested = true;
                        break;
                }
            }
            requests.Clear();

            UiMissionBriefingComponent nextBriefing = ProjectBriefing(
                ref definition, in next, replayTutorial, in currentBriefing);
            if (deployRequested && next.Available != 0 && nextBriefing.DeployQueued == 0 &&
                TryQueueLaunch(entityManager, campaignRoot, ref definition, in next, in nextBriefing,
                    out ulong transitionToken))
            {
                nextBriefing.DeployQueued = 1;
                nextBriefing.DeployTransitionToken = transitionToken;
                nextBriefing.Version = NextVersion(nextBriefing.Version);
            }

            bool sourceChanged = current.Version == 0 || current.CatalogSourceVersion != catalog.SourceVersion ||
                                 current.ObservedSettlementSourceVersion != settlementSourceVersion;
            if (sourceChanged || actionRequested || !SameOperations(in current, in next))
                entityManager.SetComponentData(uiRoot, next);
            if (sourceChanged || actionRequested || !SameBriefing(in storedBriefing, in nextBriefing))
                entityManager.SetComponentData(uiRoot, nextBriefing);
        }

        private static void EnsureUiBoundary(EntityManager entityManager, Entity uiRoot)
        {
            if (!entityManager.HasComponent<UiCampaignOperationsComponent>(uiRoot))
                entityManager.AddComponentData(uiRoot, default(UiCampaignOperationsComponent));
            if (!entityManager.HasComponent<UiMissionBriefingComponent>(uiRoot))
                entityManager.AddComponentData(uiRoot, default(UiMissionBriefingComponent));
            if (!entityManager.HasBuffer<UiCampaignMissionActionRequestElement>(uiRoot))
                entityManager.AddBuffer<UiCampaignMissionActionRequestElement>(uiRoot);
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
                LastAttemptOrdinal = m01?.lastAttemptOrdinal ?? -1,
                PrimaryAction = action,
                Available = available ? (byte)1 : (byte)0,
                FirstClearCompleted = completed ? (byte)1 : (byte)0,
                PendingResume = pending ? (byte)1 : (byte)0,
                NextMissionRevealed = m02 != null && m02.available ? (byte)1 : (byte)0
            };
            next.Version = SameOperations(in current, in next)
                ? current.Version
                : NextVersion(current.Version);
            return next;
        }

        public static UiMissionBriefingComponent ProjectBriefing(
            ref CampaignMissionDefinitionBlob definition,
            in UiCampaignOperationsComponent operations,
            bool replayTutorial,
            in UiMissionBriefingComponent current)
        {
            bool replay = operations.FirstClearCompleted != 0;
            UiMissionBriefingComponent next = new()
            {
                MissionId = definition.MissionId,
                ScenarioId = definition.ScenarioId,
                OperationMapId = definition.OperationMapId,
                DisplayNameKey = definition.DisplayNameKey,
                DisplaySummaryKey = definition.DisplaySummaryKey,
                LocationNameKey = definition.LocationNameKey,
                BuildingDisabled = definition.BuildingDisabled,
                ProductionDisabled = definition.ProductionDisabled,
                EconomyDisabled = definition.EconomyDisabled,
                TransportDisabled = definition.TransportDisabled,
                AirDisabled = definition.AirDisabled,
                Replay = replay ? (byte)1 : (byte)0,
                ReplayAllowed = definition.ReplayAllowed,
                ReplayTutorialEnabled = replay && replayTutorial ? (byte)1 : (byte)0,
                ReplayTutorialToggleVisible = replay && definition.ReplayAllowed != 0 ? (byte)1 : (byte)0,
                DeployQueued = current.DeployQueued,
                DeployTransitionToken = current.DeployTransitionToken
            };
            for (int index = 0; index < definition.Objectives.Length; index++)
            {
                ref CampaignMissionObjectiveBlob source = ref definition.Objectives[index];
                next.Objectives.Add(new UiMissionObjectiveProjectionData
                {
                    ObjectiveId = source.ObjectiveId,
                    DisplayTextKey = source.DisplayTextKey,
                    MissionRoleId = source.MissionRoleId,
                    Rule = (UiMissionObjectiveRuleKind)source.Rule,
                    RequiredCount = source.RequiredCount,
                    FailureOnRuleBreak = source.FailureOnRuleBreak
                });
                if (source.Rule == MissionObjectiveRuleKind.DestroyMissionRole)
                    next.HostileUnitCount += CountRole(ref definition, source.MissionRoleId);
            }

            ref BlobArray<CampaignMissionRewardBlob> rewards = ref replay
                ? ref definition.ReplayRewards
                : ref definition.FirstClearRewards;
            for (int index = 0; index < rewards.Length; index++)
            {
                ref CampaignMissionRewardBlob source = ref rewards[index];
                next.Rewards.Add(new UiMissionRewardProjectionData
                {
                    Kind = (UiMissionRewardKind)source.Kind,
                    RewardConfigId = source.RewardConfigId,
                    DisplayTextKey = source.DisplayTextKey,
                    Amount = source.Amount
                });
            }
            next.Version = SameBriefing(in current, in next)
                ? current.Version
                : NextVersion(current.Version);
            return next;
        }

        private static int CountRole(
            ref CampaignMissionDefinitionBlob definition, FixedString64Bytes missionRoleId)
        {
            int count = 0;
            for (int groupIndex = 0; groupIndex < definition.ForceGroups.Length; groupIndex++)
            {
                ref CampaignMissionForceGroupBlob group = ref definition.ForceGroups[groupIndex];
                for (int unitIndex = 0; unitIndex < group.Units.Length; unitIndex++)
                    if (group.Units[unitIndex].MissionRoleId.Equals(missionRoleId))
                        count += group.Units[unitIndex].Count;
            }
            return count;
        }

        private static bool TryQueueLaunch(
            EntityManager entityManager,
            Entity campaignRoot,
            ref CampaignMissionDefinitionBlob definition,
            in UiCampaignOperationsComponent operations,
            in UiMissionBriefingComponent briefing,
            out ulong transitionToken)
        {
            transitionToken = 0;
            DynamicBuffer<CampaignMissionLaunchRequestElement> launches =
                entityManager.GetBuffer<CampaignMissionLaunchRequestElement>(campaignRoot);
            if (launches.Length != 0)
                return false;
            CampaignMissionLaunchQueueComponent queue =
                entityManager.GetComponentData<CampaignMissionLaunchQueueComponent>(campaignRoot);
            if (queue.LastTransitionToken == ulong.MaxValue || operations.LastAttemptOrdinal == int.MaxValue)
                return false;

            transitionToken = queue.LastTransitionToken + 1UL;
            MissionRunKind runKind = operations.PendingResume != 0
                ? MissionRunKind.Retry
                : operations.FirstClearCompleted != 0 ? MissionRunKind.Replay : MissionRunKind.FirstClear;
            NarrativeGuidanceMode guidance = ResolveGuidance(entityManager);
            MissionLaunchPayload payload = MissionLaunchPayloadFactory.Create(
                definition.MissionId.ToString(), definition.ScenarioId.ToString(),
                definition.OperationMapId.ToString(), MissionLaunchOriginKind.CampaignOperations,
                runKind, guidance, briefing.ReplayTutorialEnabled != 0,
                transitionToken, $"campaign-m01-{transitionToken:x16}",
                operations.LastAttemptOrdinal + 1, definition.DeterministicSeed);
            launches.Add(FirstLaunchMissionHandoffOperation.ToRequest(in payload));
            return true;
        }

        private static bool IsLaunchTerminal(
            EntityManager entityManager, Entity campaignRoot, ulong transitionToken)
        {
            if (transitionToken == 0)
                return true;
            CampaignMissionLaunchQueueComponent queue =
                entityManager.GetComponentData<CampaignMissionLaunchQueueComponent>(campaignRoot);
            if (queue.LastTransitionToken >= transitionToken)
                return true;
            DynamicBuffer<CampaignMissionLaunchResultElement> results =
                entityManager.GetBuffer<CampaignMissionLaunchResultElement>(campaignRoot, true);
            for (int index = 0; index < results.Length; index++)
                if (results[index].TransitionToken == transitionToken)
                    return true;
            return false;
        }

        private static NarrativeGuidanceMode ResolveGuidance(EntityManager entityManager)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<AssistantSettingsComponent>());
            if (query.CalculateEntityCount() != 1)
                return NarrativeGuidanceMode.Full;
            AssistantSettingsComponent settings = query.GetSingleton<AssistantSettingsComponent>();
            return settings.GuidanceLevel switch
            {
                AssistantGuidanceLevel.HintsOnly => NarrativeGuidanceMode.Contextual,
                AssistantGuidanceLevel.Minimal => NarrativeGuidanceMode.Minimal,
                AssistantGuidanceLevel.Off => NarrativeGuidanceMode.Minimal,
                _ => NarrativeGuidanceMode.Full
            };
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

        private static uint NextVersion(uint current) => current == uint.MaxValue ? 1u : current + 1u;

        private static bool SameOperations(
            in UiCampaignOperationsComponent left, in UiCampaignOperationsComponent right) =>
            left.CatalogSourceVersion == right.CatalogSourceVersion &&
            left.ProgressSourceVersion == right.ProgressSourceVersion &&
            left.SelectedMissionId.Equals(right.SelectedMissionId) && left.ScenarioId.Equals(right.ScenarioId) &&
            left.OperationMapId.Equals(right.OperationMapId) && left.PrimaryAction == right.PrimaryAction &&
            left.BestStars == right.BestStars &&
            left.BestCompletionMilliseconds == right.BestCompletionMilliseconds &&
            left.SuccessfulReplayCount == right.SuccessfulReplayCount &&
            left.LastAttemptOrdinal == right.LastAttemptOrdinal && left.Available == right.Available &&
            left.FirstClearCompleted == right.FirstClearCompleted && left.PendingResume == right.PendingResume &&
            left.NextMissionRevealed == right.NextMissionRevealed;

        private static bool SameBriefing(
            in UiMissionBriefingComponent left, in UiMissionBriefingComponent right)
        {
            if (!left.MissionId.Equals(right.MissionId) || !left.ScenarioId.Equals(right.ScenarioId) ||
                !left.OperationMapId.Equals(right.OperationMapId) ||
                !left.DisplayNameKey.Equals(right.DisplayNameKey) ||
                !left.DisplaySummaryKey.Equals(right.DisplaySummaryKey) ||
                !left.LocationNameKey.Equals(right.LocationNameKey) ||
                left.HostileUnitCount != right.HostileUnitCount ||
                left.BuildingDisabled != right.BuildingDisabled || left.ProductionDisabled != right.ProductionDisabled ||
                left.EconomyDisabled != right.EconomyDisabled || left.TransportDisabled != right.TransportDisabled ||
                left.AirDisabled != right.AirDisabled || left.Replay != right.Replay ||
                left.ReplayAllowed != right.ReplayAllowed ||
                left.ReplayTutorialEnabled != right.ReplayTutorialEnabled ||
                left.ReplayTutorialToggleVisible != right.ReplayTutorialToggleVisible ||
                left.DeployQueued != right.DeployQueued ||
                left.DeployTransitionToken != right.DeployTransitionToken ||
                left.Objectives.Length != right.Objectives.Length ||
                left.Rewards.Length != right.Rewards.Length)
                return false;
            for (int index = 0; index < left.Objectives.Length; index++)
            {
                UiMissionObjectiveProjectionData a = left.Objectives[index];
                UiMissionObjectiveProjectionData b = right.Objectives[index];
                if (!a.ObjectiveId.Equals(b.ObjectiveId) || !a.DisplayTextKey.Equals(b.DisplayTextKey) ||
                    !a.MissionRoleId.Equals(b.MissionRoleId) || a.Rule != b.Rule ||
                    a.RequiredCount != b.RequiredCount || a.FailureOnRuleBreak != b.FailureOnRuleBreak)
                    return false;
            }
            for (int index = 0; index < left.Rewards.Length; index++)
            {
                UiMissionRewardProjectionData a = left.Rewards[index];
                UiMissionRewardProjectionData b = right.Rewards[index];
                if (a.Kind != b.Kind || !a.RewardConfigId.Equals(b.RewardConfigId) ||
                    !a.DisplayTextKey.Equals(b.DisplayTextKey) || a.Amount != b.Amount)
                    return false;
            }
            return true;
        }
    }
}
