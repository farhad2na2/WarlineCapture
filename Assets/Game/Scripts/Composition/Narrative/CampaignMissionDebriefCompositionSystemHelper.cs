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
        private bool hasMissionRootQuery;
        private FixedString64Bytes activeSession;
        private bool running;
        private bool handoffComplete;
        private bool returnToMenuQueued;
        private bool campaignRouteQueued;
        private bool configurationFailureLogged;

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

            if (handoffComplete)
            {
                AdvanceReturnRoute(entityManager, shellBoundary, in shellState);
                return;
            }

            if (!TryReadDebrief(
                    entityManager,
                    out CampaignMissionRuntimeComponent runtime,
                    out FixedString64Bytes debriefSequenceId))
                return;
            if (running && activeSession.Equals(runtime.SessionToken))
                return;
            if (!TryFindConfig(in debriefSequenceId, out NarrativeSequenceConfig config) ||
                view == null || speakers == null || punctuation == null)
            {
                if (!configurationFailureLogged)
                {
                    Debug.LogError(
                        $"[CampaignMissionDebrief] Missing presentation binding for {debriefSequenceId}.");
                    configurationFailureLogged = true;
                }
                return;
            }

            FirstLaunchNarrativeLanguage language = ReadLanguage();
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
                if (!configurationFailureLogged)
                {
                    Debug.LogError(
                        $"[CampaignMissionDebrief] Failed to start {debriefSequenceId}.");
                    configurationFailureLogged = true;
                }
                return;
            }

            activeSession = runtime.SessionToken;
            running = true;
            configurationFailureLogged = false;
        }

        public void Shutdown()
        {
            presentation.HandoffRequested -= HandleHandoff;
            presentation.Cancel();
            configs = Array.Empty<NarrativeSequenceConfig>();
            view = null;
            speakers = null;
            punctuation = null;
            persianLocale = null;
            baseTextResolver = null;
            queryWorld = null;
            hasMissionRootQuery = false;
            activeSession = default;
            running = false;
            handoffComplete = false;
            returnToMenuQueued = false;
            campaignRouteQueued = false;
            configurationFailureLogged = false;
        }

        private bool TryReadDebrief(
            EntityManager entityManager,
            out CampaignMissionRuntimeComponent runtime,
            out FixedString64Bytes sequenceId)
        {
            runtime = default;
            sequenceId = default;
            if (queryWorld != entityManager.World)
            {
                presentation.Cancel();
                view?.SetVisible(false);
                queryWorld = entityManager.World;
                missionRootQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                    ComponentType.ReadOnly<CampaignMissionRuntimeComponent>(),
                    ComponentType.ReadOnly<CampaignMissionCatalogComponent>());
                hasMissionRootQuery = true;
                activeSession = default;
                running = false;
                handoffComplete = false;
                returnToMenuQueued = false;
                campaignRouteQueued = false;
            }
            if (!hasMissionRootQuery || missionRootQuery.CalculateEntityCount() != 1)
                return false;

            Entity root = missionRootQuery.GetSingletonEntity();
            runtime = entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (runtime.Phase != MissionPhaseKind.DebriefFirstClear ||
                !runtime.MissionId.Equals(EstablishBaseMissionId))
                return false;
            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (!CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return false;
            sequenceId = catalog.Blob.Value.Missions[definitionIndex].DebriefSequenceId;
            return !sequenceId.IsEmpty;
        }

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

        private static FirstLaunchNarrativeLanguage ReadLanguage()
        {
            PlayerProfileSaveData profile = SaveService.CreateDefault().LoadProfile();
            return Enum.TryParse(
                    profile?.firstLaunchLanguage,
                    true,
                    out FirstLaunchNarrativeLanguage language) &&
                language == FirstLaunchNarrativeLanguage.Persian
                    ? FirstLaunchNarrativeLanguage.Persian
                    : FirstLaunchNarrativeLanguage.English;
        }

        private void HandleHandoff(NarrativeHandoffResult result)
        {
            presentation.Cancel();
            running = false;
            handoffComplete = true;
            view?.SetVisible(false);
        }
    }
}
