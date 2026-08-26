using System;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Composition
{
    internal sealed class CampaignMissionDebriefCompositionSystemHelper
    {
        internal enum SequenceStage : byte
        {
            None = 0,
            Brief = 1,
            Comms = 2,
            Debrief = 3
        }

        private static readonly FixedString64Bytes EstablishBaseMissionId =
            new("saga.ch01.m02.establish_base");

        private readonly FirstLaunchNarrativeSequencePresentationSystemHelper presentation = new();
        private NarrativeSequenceConfig[] configs = Array.Empty<NarrativeSequenceConfig>();
        private NarrativeSequenceView view;
        private NarrativeSpeakerCatalog speakers;
        private NarrativePunctuationConfig punctuation;
        private NarrativeLocaleConfig persianLocale;
        private IGameTextResolver baseTextResolver;
        private World queryWorld;
        private EntityQuery missionRootQuery;
        private EntityQuery gameplayStateQuery;
        private bool hasMissionRootQuery;
        private bool hasGameplayStateQuery;
        private FixedString64Bytes activeSession;
        private int activeAttemptOrdinal = -1;
        private SequenceStage activeStage;
        private FixedString64Bytes completedBriefSession;
        private int completedBriefAttemptOrdinal = -1;
        private FixedString64Bytes completedCommsSession;
        private int completedCommsAttemptOrdinal = -1;
        private bool running, handoffPending, handoffComplete, pauseOwned, returnToMenuQueued,
            campaignRouteQueued, configurationFailureLogged;

        public void Initialize(MenuBootstrapView menuView, IGameTextResolver textResolver)
        {
            view = menuView?.FirstLaunchNarrativeView;
            speakers = menuView?.FirstLaunchSpeakerCatalog;
            punctuation = menuView?.FirstLaunchPunctuationProfile;
            persianLocale = menuView?.FirstLaunchPersianLocale;
            configs = menuView?.CampaignMissionNarrativeConfigs ?? Array.Empty<NarrativeSequenceConfig>();
            baseTextResolver = textResolver ?? FallbackGameTextResolver.Instance;
            presentation.HandoffRequested -= HandleHandoff;
            presentation.HandoffRequested += HandleHandoff;
        }

        public void Tick(
            float unscaledDeltaTime,
            EntityManager entityManager,
            Entity shellBoundary,
            in UiShellStateComponent shellState)
        {
            if (running)
                presentation.Tick(unscaledDeltaTime);
            if (handoffPending)
                CompleteActiveSequence(entityManager);

            if (handoffComplete)
            {
                AdvanceReturnRoute(entityManager, shellBoundary, in shellState);
                return;
            }

            if (running || !TryReadSequence(
                    entityManager,
                    out CampaignMissionRuntimeComponent runtime,
                    out FixedString64Bytes sequenceId,
                    out SequenceStage stage))
                return;
            if (!CampaignMissionNarrativeCompositionUtility.IsPresentationReady(stage, in shellState))
                return;
            if (!TryFindConfig(in sequenceId, out NarrativeSequenceConfig config) ||
                view == null || speakers == null || punctuation == null)
            {
                if (!configurationFailureLogged)
                {
                    Debug.LogError(
                        $"[CampaignMissionNarrative] Missing presentation binding for {sequenceId}.");
                    configurationFailureLogged = true;
                }
                return;
            }
            if (!TryPauseForSequence(entityManager, stage))
                return;

            FirstLaunchNarrativeLanguage language =
                CampaignMissionNarrativeCompositionUtility.ReadLanguage();
            NarrativeLocaleConfig locale = language == FirstLaunchNarrativeLanguage.Persian
                ? persianLocale
                : null;
            IGameTextResolver resolver = locale != null
                ? new FirstLaunchNarrativeLocaleTextCompositionSystemHelper(baseTextResolver, locale)
                : baseTextResolver;
            if (!presentation.Initialize(
                    config,
                    speakers,
                    punctuation,
                    view,
                    resolver,
                    SettingsService.Load(),
                    locale) ||
                !presentation.Start())
            {
                ReleasePause(entityManager);
                if (!configurationFailureLogged)
                {
                    Debug.LogError(
                        $"[CampaignMissionNarrative] Failed to start {sequenceId}.");
                    configurationFailureLogged = true;
                }
                return;
            }

            activeSession = runtime.SessionToken;
            activeAttemptOrdinal = runtime.AttemptOrdinal;
            activeStage = stage;
            running = true;
            configurationFailureLogged = false;
            CampaignMissionNarrativeCompositionUtility.LogStage(
                "started", stage, in sequenceId, in activeSession, activeAttemptOrdinal);
        }

        public void Shutdown()
        {
            ReleasePause();
            presentation.HandoffRequested -= HandleHandoff;
            presentation.Cancel();
            DisposeQueries();
            configs = Array.Empty<NarrativeSequenceConfig>();
            view = null;
            speakers = null;
            punctuation = null;
            persianLocale = null;
            baseTextResolver = null;
            queryWorld = null;
            activeSession = default;
            activeAttemptOrdinal = -1;
            activeStage = SequenceStage.None;
            completedBriefSession = default;
            completedBriefAttemptOrdinal = -1;
            completedCommsSession = default;
            completedCommsAttemptOrdinal = -1;
            running = handoffPending = handoffComplete = pauseOwned = false;
            returnToMenuQueued = campaignRouteQueued = configurationFailureLogged = false;
        }

        private bool TryReadSequence(
            EntityManager entityManager,
            out CampaignMissionRuntimeComponent runtime,
            out FixedString64Bytes sequenceId,
            out SequenceStage stage)
        {
            runtime = default;
            sequenceId = default;
            stage = SequenceStage.None;
            if (queryWorld != entityManager.World)
                BindWorld(entityManager);
            if (!hasMissionRootQuery || missionRootQuery.CalculateEntityCount() != 1)
                return false;

            Entity root = missionRootQuery.GetSingletonEntity();
            runtime = entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (!runtime.MissionId.Equals(EstablishBaseMissionId))
                return false;
            CampaignMissionAttemptFactsComponent facts =
                entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (!CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return false;
            bool briefConsumed = IsSameAttempt(
                in runtime, in completedBriefSession, completedBriefAttemptOrdinal);
            bool commsConsumed = IsSameAttempt(
                in runtime, in completedCommsSession, completedCommsAttemptOrdinal);
            stage = ResolveStage(in runtime, in facts, briefConsumed, commsConsumed);
            ref CampaignMissionDefinitionBlob definition =
                ref catalog.Blob.Value.Missions[definitionIndex];
            sequenceId = stage switch
            {
                SequenceStage.Brief => definition.BriefingSequenceId,
                SequenceStage.Comms => definition.CommsSequenceId,
                SequenceStage.Debrief => definition.DebriefSequenceId,
                _ => default
            };
            return !sequenceId.IsEmpty;
        }

        internal static SequenceStage ResolveStage(
            in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts,
            bool briefConsumed,
            bool commsConsumed)
        {
            if (!runtime.MissionId.Equals(EstablishBaseMissionId))
                return SequenceStage.None;
            if (runtime.Phase == MissionPhaseKind.DebriefFirstClear)
                return SequenceStage.Debrief;
            if (runtime.Phase == MissionPhaseKind.InteractiveBrief && !briefConsumed)
                return SequenceStage.Brief;
            if (runtime.Phase is >= MissionPhaseKind.FindSquad and <= MissionPhaseKind.SecureCorridor &&
                facts.DefenseWaveWarningIssued != 0 && facts.DefenseWaveActivated == 0 &&
                !commsConsumed)
                return SequenceStage.Comms;
            return SequenceStage.None;
        }

        internal static bool RequiresSimulationPause(SequenceStage stage) =>
            stage is SequenceStage.Brief or SequenceStage.Comms;

        internal static bool ReturnsToCampaign(SequenceStage stage) =>
            stage == SequenceStage.Debrief;

        private void BindWorld(EntityManager entityManager)
        {
            ReleasePause();
            presentation.Cancel();
            view?.SetVisible(false);
            DisposeQueries();
            queryWorld = entityManager.World;
            missionRootQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>(),
                ComponentType.ReadOnly<CampaignMissionAttemptFactsComponent>(),
                ComponentType.ReadOnly<CampaignMissionCatalogComponent>());
            gameplayStateQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<RuntimeGameplayStateComponent>());
            hasMissionRootQuery = true;
            hasGameplayStateQuery = true;
            activeSession = default;
            activeAttemptOrdinal = -1;
            activeStage = SequenceStage.None;
            completedBriefSession = default;
            completedBriefAttemptOrdinal = -1;
            completedCommsSession = default;
            completedCommsAttemptOrdinal = -1;
            running = handoffPending = handoffComplete = false;
            returnToMenuQueued = campaignRouteQueued = false;
        }

        private bool TryPauseForSequence(EntityManager entityManager, SequenceStage stage)
        {
            if (!RequiresSimulationPause(stage) || pauseOwned)
                return true;
            if (!hasGameplayStateQuery || gameplayStateQuery.CalculateEntityCount() != 1)
                return false;
            Entity stateEntity = gameplayStateQuery.GetSingletonEntity();
            RuntimeGameplayStateComponent gameplayState =
                entityManager.GetComponentData<RuntimeGameplayStateComponent>(stateEntity);
            if (gameplayState.PlayRequested == 0 || gameplayState.SimulationActive == 0)
                return false;
            gameplayState.SimulationActive = 0;
            entityManager.SetComponentData(stateEntity, gameplayState);
            pauseOwned = true;
            return true;
        }

        private void CompleteActiveSequence(EntityManager entityManager)
        {
            handoffPending = false;
            if (ReturnsToCampaign(activeStage))
            {
                handoffComplete = true;
                activeStage = SequenceStage.None;
                return;
            }

            if (activeStage == SequenceStage.Brief)
            {
                if (!CampaignMissionRuntimeProgressUtility.TryCompleteBrief(
                    entityManager, missionRootQuery, activeSession, activeAttemptOrdinal))
                { ReleasePause(entityManager); activeStage = SequenceStage.None; return; }
                completedBriefSession = activeSession;
                completedBriefAttemptOrdinal = activeAttemptOrdinal;
            }
            else if (activeStage == SequenceStage.Comms)
            {
                completedCommsSession = activeSession;
                completedCommsAttemptOrdinal = activeAttemptOrdinal;
            }
            ReleasePause(entityManager);
            activeSession = default;
            activeAttemptOrdinal = -1;
            activeStage = SequenceStage.None;
        }

        private void ReleasePause()
        {
            if (!pauseOwned)
                return;
            if (queryWorld != null && queryWorld.IsCreated)
                ReleasePause(queryWorld.EntityManager);
            else
                pauseOwned = false;
        }

        private void ReleasePause(EntityManager entityManager)
        {
            if (!pauseOwned)
                return;
            if (hasGameplayStateQuery && gameplayStateQuery.CalculateEntityCount() == 1)
            {
                Entity stateEntity = gameplayStateQuery.GetSingletonEntity();
                RuntimeGameplayStateComponent gameplayState =
                    entityManager.GetComponentData<RuntimeGameplayStateComponent>(stateEntity);
                if (gameplayState.PlayRequested != 0)
                {
                    gameplayState.SimulationActive = 1;
                    entityManager.SetComponentData(stateEntity, gameplayState);
                }
            }
            pauseOwned = false;
        }

        private void DisposeQueries()
        {
            if (hasMissionRootQuery && queryWorld != null && queryWorld.IsCreated)
                missionRootQuery.Dispose();
            if (hasGameplayStateQuery && queryWorld != null && queryWorld.IsCreated)
                gameplayStateQuery.Dispose();
            hasMissionRootQuery = false;
            hasGameplayStateQuery = false;
        }

        private static bool IsSameAttempt(
            in CampaignMissionRuntimeComponent runtime,
            in FixedString64Bytes session,
            int attemptOrdinal) =>
            attemptOrdinal == runtime.AttemptOrdinal && session.Equals(runtime.SessionToken);

        private bool TryFindConfig(
            in FixedString64Bytes sequenceId,
            out NarrativeSequenceConfig config)
        {
            string expected = sequenceId.ToString();
            for (int index = 0; index < configs.Length; index++)
            {
                NarrativeSequenceConfig candidate = configs[index];
                if (candidate != null && string.Equals(
                        candidate.SequenceId,
                        expected,
                        StringComparison.Ordinal))
                {
                    config = candidate;
                    return true;
                }
            }

            config = null;
            return false;
        }

        private void AdvanceReturnRoute(
            EntityManager entityManager,
            Entity shellBoundary,
            in UiShellStateComponent shellState)
        {
            DynamicBuffer<UiShellRouteRequestComponent> requests =
                entityManager.GetBuffer<UiShellRouteRequestComponent>(shellBoundary);
            if (!returnToMenuQueued)
            {
                requests.Add(new UiShellRouteRequestComponent
                {
                    Intent = UiShellRouteIntent.ReturnToMainMenu,
                    Route = UIRoute.MainMenu,
                    PushHistory = 0
                });
                returnToMenuQueued = true;
                return;
            }

            if (campaignRouteQueued || shellState.IsTransitionRunning != 0 ||
                shellState.CurrentMode != UiShellMode.MainMenu ||
                shellState.ActiveRoute != UIRoute.MainMenu)
                return;
            requests.Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.OpenMenuRoute,
                Route = UIRoute.Campaign,
                PushHistory = 0
            });
            campaignRouteQueued = true;
        }

        private void HandleHandoff(NarrativeHandoffResult result)
        {
            presentation.Cancel();
            running = false;
            handoffPending = true;
            view?.SetVisible(false);
        }
    }
}
