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
    internal sealed partial class CampaignMissionDebriefCompositionSystemHelper
    {
        internal enum SequenceStage : byte
        {
            None = 0,
            Brief = 1,
            Comms = 2,
            Debrief = 3,
            ChapterOpening = 4,
            ChapterReplay = 5,
            ChapterClose = 6,
            CampaignFinale = 7
        }

        private static readonly FixedString64Bytes EstablishBaseMissionId =
            new("saga.ch01.m02.establish_base");

        private readonly FirstLaunchNarrativeSequencePresentationSystemHelper presentation = new();
        private NarrativeSequenceConfig[] configs = Array.Empty<NarrativeSequenceConfig>();
        private NarrativeSequenceConfig firstContactOpening;
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
        private bool running, handoffPending, pauseOwned, configurationFailureLogged;
        private readonly PlaybackPresentationSystemHelper playback = new();
        private FixedString64Bytes finaleSession;
        private int finaleAttempt = -1, finaleQueueIndex, finaleEmphasis;
        internal static string CommandNodeFinaleSequence(int index,int emphasis) => index switch {
            0=>"seq.ch05.m05.debrief",1=>"seq.ch05.close.protocol_fragment_05",2=>"seq.campaign.epilogue.canonical",
            3=>"seq.campaign.epilogue.trust_emphasis."+((emphasis&1)!=0?"high":"low"),
            4=>"seq.campaign.epilogue.evidence_emphasis."+((emphasis&2)!=0?"high":"low"),
            5=>"seq.campaign.epilogue.infrastructure_emphasis."+((emphasis&4)!=0?"high":"low"),
            6=>"seq.campaign.postscript.recovery_watch",_=>string.Empty};
        internal static string CommandNodeFinalePayload(int index,int emphasis) => "request.command_node."+(index switch {
            0=>"debrief",1=>"close",2=>"epilogue",3=>(emphasis&1)!=0?"trust_high":"trust_low",
            4=>(emphasis&2)!=0?"evidence_high":"evidence_low",5=>(emphasis&4)!=0?"infrastructure_high":"infrastructure_low",_=>"postscript"})+".complete";

        public void Initialize(MenuBootstrapView menuView, IGameTextResolver textResolver)
        {
            view = menuView?.FirstLaunchNarrativeView;
            speakers = menuView?.FirstLaunchSpeakerCatalog;
            punctuation = menuView?.FirstLaunchPunctuationProfile;
            persianLocale = menuView?.FirstLaunchPersianLocale;
            configs = menuView?.CampaignMissionNarrativeConfigs ?? Array.Empty<NarrativeSequenceConfig>();
            firstContactOpening = menuView?.FirstLaunchNarrativeConfig;
            baseTextResolver = textResolver ?? FallbackGameTextResolver.Instance;
            presentation.HandoffRequested -= HandleHandoff;
            presentation.HandoffRequested += HandleHandoff;
        }

        public void Tick(
            float unscaledDeltaTime,
            EntityManager entityManager,
            in UiShellStateComponent shellState)
        {
            if (queryWorld != entityManager.World)
                BindWorld(entityManager);
            if(shellState.ActiveRoute!=UIRoute.Campaign)
            {
                ClearChapterReplayRequest(entityManager);
                if(running && activeStage==SequenceStage.ChapterReplay)
                {
                    playback.Unbind();presentation.Cancel();view?.SetVisible(false);
                    running=handoffPending=false;activeStage=SequenceStage.None;
                    activeSession=default;activeAttemptOrdinal=-1;
                }
            }
            RefreshActivePresentation();
            if (running)
                presentation.Tick(unscaledDeltaTime);
            playback.Tick();
            if (handoffPending)
                CompleteActiveSequence(entityManager);
            if (!running && CampaignMissionResultDebriefTransitionUtility.TryQueueDebrief(
                    entityManager, missionRootQuery))
                return;

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
            IGameTextResolver legacyResolver = locale != null
                ? new FirstLaunchNarrativeLocaleTextCompositionSystemHelper(baseTextResolver, locale)
                : baseTextResolver;
            IGameTextResolver resolver =
                new FirstLaunchNarrativeCompositionSystemHelper.SharedLocaleCompositionSystemHelper(legacyResolver);
            bool started = presentation.Initialize(
                    config,
                    speakers,
                    punctuation,
                    view,
                    resolver,
                    SettingsService.Load(),
                    locale,
                    storyOnly: CampaignMissionNarrativePolicy.UsesFirstContactOpening(runtime));
            if (started)
            {
                // A new sequence player starts without a selected commander. Restore the
                // identity before any dialogue can choose the neutral fallback portrait/voice.
                var profile = SaveService.CreateDefault().LoadProfile();
                presentation.ApplyCommanderIdentity(new NarrativeCommanderIdentityData
                {
                    Callsign = profile.firstLaunchCommanderCallsign,
                    DisplayName = profile.firstLaunchCommanderDisplayName
                }, profile.firstLaunchCommanderPortraitIndex);
                started = presentation.Start();
            }
            if (!started)
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
            renderedLanguage = language;
            renderedWide = UnityEngine.Screen.height > 0 && (float)UnityEngine.Screen.width / UnityEngine.Screen.height >= 2f;
            playback.Bind(presentation, view);
            configurationFailureLogged = false;
            CampaignMissionNarrativeCompositionUtility.LogStage(
                "started", stage, in sequenceId, in activeSession, activeAttemptOrdinal);
        }

        public void Shutdown()
        {
            playback.Unbind();
            ReleasePause();
            presentation.HandoffRequested -= HandleHandoff;
            presentation.Cancel();
            DisposeQueries();
            configs = Array.Empty<NarrativeSequenceConfig>();
            firstContactOpening = null;
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
            running = handoffPending = pauseOwned = configurationFailureLogged = false;
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
            if(entityManager.HasComponent<GridlockChapterReplayRequest>(root) &&
                entityManager.GetComponentData<GridlockChapterReplayRequest>(root).Pending!=0)
            {
                stage=SequenceStage.ChapterReplay;
                sequenceId="seq.ch02.open.broken_grid";
                return true;
            }
            if (!CampaignMissionNarrativePolicy.UsesMissionSequences(runtime.MissionId) &&
                !CampaignMissionNarrativePolicy.UsesFirstContactOpening(runtime))
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
            if(runtime.MissionId.Equals(CampaignMissionSequence.CommandNode) && runtime.Phase==MissionPhaseKind.DebriefFirstClear)
            {
                if(finaleSession!=runtime.SessionToken || finaleAttempt!=runtime.AttemptOrdinal)
                { finaleSession=runtime.SessionToken;finaleAttempt=runtime.AttemptOrdinal;finaleQueueIndex=0;finaleEmphasis=entityManager.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store.ResolveRecordedRecoveryEmphasis(); }
                stage=finaleQueueIndex==0?SequenceStage.Debrief:SequenceStage.CampaignFinale;
                sequenceId=new FixedString64Bytes(CommandNodeFinaleSequence(finaleQueueIndex,finaleEmphasis));
                return !sequenceId.IsEmpty;
            }
            if (stage == SequenceStage.Brief &&
                (runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.Gridlock)) ||
                 runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.CitywideAlert))))
            {
                if (!entityManager.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root)) return false;
                var progress = entityManager.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store;
                if (progress == null) return false;
                if (!progress.HasSeenChapterOpening(runtime.MissionId.ToString()))
                {
                    stage = SequenceStage.ChapterOpening;
                    sequenceId = runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.CitywideAlert))
                        ? "seq.ch05.open.citywide_command" : "seq.ch02.open.broken_grid";
                    return true;
                }
            }
            if (CampaignMissionNarrativePolicy.UsesFirstContactOpening(runtime))
            {
                sequenceId = stage == SequenceStage.Brief && firstContactOpening != null
                    ? new FixedString64Bytes(firstContactOpening.SequenceId) : default;
                return !sequenceId.IsEmpty;
            }
            ref CampaignMissionDefinitionBlob definition =
                ref catalog.Blob.Value.Missions[definitionIndex];
            sequenceId = stage switch
            {
                SequenceStage.Brief => definition.BriefingSequenceId,
                SequenceStage.Comms => definition.CommsSequenceId,
                SequenceStage.ChapterClose => new FixedString64Bytes("seq.ch04.close.protocol_fragment_04"),
                SequenceStage.Debrief => CampaignMissionNarrativePolicy.ResolveDebrief(runtime.MissionId, facts, definition.DebriefSequenceId),
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
            if (CampaignMissionNarrativePolicy.UsesFirstContactOpening(runtime))
                return runtime.Phase == MissionPhaseKind.InteractiveBrief && !briefConsumed
                    ? SequenceStage.Brief : SequenceStage.None;
            if (!CampaignMissionNarrativePolicy.UsesMissionSequences(runtime.MissionId))
                return SequenceStage.None;
            if (runtime.Phase == MissionPhaseKind.DebriefFirstClear)
                return runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.ArmorBreak)) && facts.ArmorBreakDebriefCompleted != 0 && facts.ArmorBreakCloseCompleted == 0
                    ? SequenceStage.ChapterClose : SequenceStage.Debrief;
            if (runtime.Phase == MissionPhaseKind.InteractiveBrief && !briefConsumed)
                return SequenceStage.Brief;
            if (runtime.Phase is >= MissionPhaseKind.FindSquad and <= MissionPhaseKind.SecureCorridor &&
                CampaignMissionNarrativePolicy.UsesBlockingComms(runtime.MissionId, facts) &&
                !commsConsumed)
                return SequenceStage.Comms;
            return SequenceStage.None;
        }

        internal static bool RequiresSimulationPause(SequenceStage stage) =>
            stage is SequenceStage.Brief or SequenceStage.Comms or SequenceStage.ChapterOpening;

        internal static bool RequiresFinalResult(SequenceStage stage) =>
            stage is SequenceStage.Debrief or SequenceStage.ChapterClose or SequenceStage.CampaignFinale;

        private void BindWorld(EntityManager entityManager)
        {
            playback.Unbind();
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
            running = handoffPending = false;
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
            playback.Unbind();
            handoffPending = false;
            if((activeStage==SequenceStage.Debrief || activeStage==SequenceStage.CampaignFinale) && missionRootQuery.CalculateEntityCount()==1)
            {
                var finaleRoot=missionRootQuery.GetSingletonEntity();var finaleRuntime=entityManager.GetComponentData<CampaignMissionRuntimeComponent>(finaleRoot);
                if(finaleRuntime.MissionId.Equals(CampaignMissionSequence.CommandNode))
                {
                    if(!IsSameAttempt(finaleRuntime,activeSession,activeAttemptOrdinal)||finaleRuntime.Outcome!=MissionOutcomeKind.Victory||finaleRuntime.Phase!=MissionPhaseKind.DebriefFirstClear)
                    {activeStage=SequenceStage.None;return;}
                    if(finaleQueueIndex<6)
                    {finaleQueueIndex++;activeSession=default;activeAttemptOrdinal=-1;activeStage=SequenceStage.None;return;}
                    // Persist before allowing the ordinary completion transition; a save failure keeps the finale gate closed.
                    try {entityManager.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(finaleRoot).Store.RecordCommandNodeFinale(finaleRuntime.SessionToken.ToString(),finaleRuntime.AttemptOrdinal,finaleEmphasis);}
                    catch(Exception error){Debug.LogException(error);activeStage=SequenceStage.None;return;}
                }
            }
            if (activeStage is SequenceStage.Debrief or SequenceStage.ChapterClose && missionRootQuery.CalculateEntityCount() == 1)
            {
                var root = missionRootQuery.GetSingletonEntity();
                var current = entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
                if (current.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.ArmorBreak)))
                {
                    if (!IsSameAttempt(current, activeSession, activeAttemptOrdinal) || current.Outcome != MissionOutcomeKind.Victory || current.Phase != MissionPhaseKind.DebriefFirstClear)
                    { activeStage = SequenceStage.None; return; }
                    var facts = entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                    if (activeStage == SequenceStage.Debrief)
                    {
                        facts.ArmorBreakDebriefCompleted = 1;
                        entityManager.SetComponentData(root, facts);
                        activeSession = default; activeAttemptOrdinal = -1; activeStage = SequenceStage.None;
                        return;
                    }
                    facts.ArmorBreakCloseCompleted = 1;
                    entityManager.SetComponentData(root, facts);
                    if (entityManager.HasComponent<CampaignMissionArmorBreakState>(root))
                    { var armor = entityManager.GetComponentData<CampaignMissionArmorBreakState>(root); armor.CloseCompleted = 1; entityManager.SetComponentData(root, armor); }
                }
            }
            if (RequiresFinalResult(activeStage))
            {
                if (!CampaignMissionRuntimeProgressUtility.TryCompleteDebrief(
                        entityManager, missionRootQuery, activeSession, activeAttemptOrdinal))
                {
                    Debug.LogError(
                        "[CampaignMissionNarrative] Failed to complete the M2 debrief transition.");
                }
                activeSession = default;
                activeAttemptOrdinal = -1;
                activeStage = SequenceStage.None;
                return;
            }

            if (activeStage == SequenceStage.ChapterReplay)
            {
                ClearChapterReplayRequest(entityManager);
            }
            else if (activeStage == SequenceStage.ChapterOpening)
            {
                if (missionRootQuery.CalculateEntityCount() != 1) { ReleasePause(entityManager); activeStage=SequenceStage.None; return; }
                var root = missionRootQuery.GetSingletonEntity();
                var runtime = entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
                if (!IsSameAttempt(runtime, activeSession, activeAttemptOrdinal)) { ReleasePause(entityManager); activeStage=SequenceStage.None; return; }
                try
                {
                    entityManager.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root)
                        .Store.MarkChapterOpeningSeen(runtime.MissionId.ToString());
                }
                catch (Exception error)
                {
                    // Do not advance the briefing or silently consume an unsaved opening.
                    Debug.LogException(error);
                }
            }
            else if (activeStage == SequenceStage.Brief)
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

        private void ClearChapterReplayRequest(EntityManager entityManager)
        {
            if(!hasMissionRootQuery || missionRootQuery.CalculateEntityCount()!=1)return;
            var root=missionRootQuery.GetSingletonEntity();
            if(entityManager.HasComponent<GridlockChapterReplayRequest>(root) &&
                entityManager.GetComponentData<GridlockChapterReplayRequest>(root).Pending!=0)
                entityManager.SetComponentData(root,default(GridlockChapterReplayRequest));
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
            if (firstContactOpening != null && firstContactOpening.SequenceId == expected)
            {
                config = firstContactOpening;
                return true;
            }
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

        private void HandleHandoff(NarrativeHandoffResult result)
        {
            if(activeStage==SequenceStage.CampaignFinale || activeStage==SequenceStage.Debrief && activeSession==finaleSession && activeAttemptOrdinal==finaleAttempt)
            {
                if(result.Completion.PayloadId!=CommandNodeFinalePayload(finaleQueueIndex,finaleEmphasis))
                {Debug.LogError("[CommandNodeFinale] Rejected mismatched canonical queue receipt.");return;}
                Debug.Log($"[CommandNodeFinale] receipt=Passed queue={finaleQueueIndex} sequence={CommandNodeFinaleSequence(finaleQueueIndex,finaleEmphasis)} emphasis={finaleEmphasis}");
            }
            if (activeStage == SequenceStage.ChapterClose && result.Completion.PayloadId != "request.armor_break.close.complete")
            { Debug.LogError("[ArmorBreakNarrative] Rejected chapter close without canonical completion receipt."); return; }
            if (activeStage == SequenceStage.ChapterOpening && queryWorld != null && queryWorld.IsCreated && hasMissionRootQuery && missionRootQuery.CalculateEntityCount() == 1)
            {
                var runtime = queryWorld.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(missionRootQuery.GetSingletonEntity());
                if (runtime.MissionId.Equals(new FixedString64Bytes(CampaignMissionSequence.CitywideAlert)) &&
                    result.Completion.PayloadId != "request.citywide_alert.open.complete")
                { Debug.LogError("[CitywideAlertNarrative] Rejected chapter opening without canonical completion receipt."); return; }
            }
            presentation.Cancel();
            running = false;
            handoffPending = true;
            view?.SetVisible(false);
        }
    }
}
