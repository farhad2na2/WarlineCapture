using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Game.Editor
{
    /// <summary>Public UI-entry Watch ARIA probe. Gameplay orders are issued only by ARIA after a real touch confirmation.</summary>
    [InitializeOnLoad]
    public static class CH02M03MarketLifelineInputProbe
    {
        private const string Active="Warline.MarketLifeline.InputProbe";
        private const string ManualEditorRun="Warline.MarketLifeline.InputProbe.ManualEditorRun";
        private const string PersianEditorRun="Warline.MarketLifeline.InputProbe.PersianEditorRun";
        private const string SignalTraceEditorRun="Warline.SignalTrace.InputProbe.EditorRun";
        private const string SafehouseSweepEditorRun="Warline.SafehouseSweep.InputProbe.EditorRun";
        private const string FalseFrontEditorRun="Warline.FalseFront.InputProbe.EditorRun";
        private const string EvidenceChainEditorRun="Warline.EvidenceChain.InputProbe.EditorRun";
        private const string EvidenceChainArmoredEditorRun="Warline.EvidenceChain.InputProbe.ArmoredEditorRun";
        private const string EvidenceChainHelipadDiagnosticRun="Warline.EvidenceChain.InputProbe.HelipadDiagnosticRun";
        private const string Output="/private/tmp/warline-market-lifeline";
        private static bool seeded, finished, watchStarted, won, sawDebrief, sawYasinPortrait, sawLocalizedYasinName, loggedCampaignProjection, capturedFalseFrontBrief, capturedFalseFrontDebrief, capturedEvidenceRouteBrief;
        private static double started, lastInput, lastLog, briefWaitStarted, touchStartWait, evidenceLaunchWaitStarted, deployWaitStarted;
        private static string lastTapDiagnostics;
        private static int winningClock;
        private static AriaTouchInputUiSystemHelper touch;
        private static CampaignMissionProgressStore probeStore;

        static CH02M03MarketLifelineInputProbe(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Tick;}

        [MenuItem("Game/Campaign/Market Lifeline/Run Normal Input Watch Probe")]
        public static void RunWatchFromOpenEditor()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);
            SessionState.SetBool(SafehouseSweepEditorRun,false);
            SessionState.SetBool(PersianEditorRun,false);
            SessionState.SetBool(ManualEditorRun,true);
            RunWatch();
        }

        [MenuItem("Game/Campaign/Market Lifeline/Run Persian Normal Input Watch Probe")]
        public static void RunPersianWatchFromOpenEditor()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);
            SessionState.SetBool(SafehouseSweepEditorRun,false);
            SessionState.SetBool(PersianEditorRun,true);
            SessionState.SetBool(ManualEditorRun,true);
            RunWatch();
        }

        [MenuItem("Game/Campaign/Signal Trace/Run Normal Input Watch Probe")]
        public static void RunSignalTraceWatchFromOpenEditor()
        {
            SessionState.SetBool(SignalTraceEditorRun,true);SessionState.SetBool(SafehouseSweepEditorRun,false);SessionState.SetBool(PersianEditorRun,false);SessionState.SetBool(ManualEditorRun,true);RunWatch();
        }

        [MenuItem("Game/Campaign/Safehouse Sweep/Run Normal Input Watch Probe")]
        public static void RunSafehouseSweepWatchFromOpenEditor()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);SessionState.SetBool(SafehouseSweepEditorRun,true);SessionState.SetBool(PersianEditorRun,false);SessionState.SetBool(ManualEditorRun,true);RunWatch();
        }

        [MenuItem("Game/Campaign/False Front/Run Normal Input Watch Probe")]
        public static void RunFalseFrontWatchFromOpenEditor()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);SessionState.SetBool(SafehouseSweepEditorRun,false);SessionState.SetBool(FalseFrontEditorRun,true);SessionState.SetBool(PersianEditorRun,false);SessionState.SetBool(ManualEditorRun,true);RunWatch();
        }

        public static void RunFalseFrontWatch()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);SessionState.SetBool(SafehouseSweepEditorRun,false);SessionState.SetBool(FalseFrontEditorRun,true);SessionState.SetBool(PersianEditorRun,false);SessionState.SetBool(ManualEditorRun,false);RunWatch();
        }

        public static void RunFalseFrontPersianWatch()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);SessionState.SetBool(SafehouseSweepEditorRun,false);SessionState.SetBool(FalseFrontEditorRun,true);SessionState.SetBool(PersianEditorRun,true);SessionState.SetBool(ManualEditorRun,false);RunWatch();
        }

        public static void RunEvidenceChainWatch()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);SessionState.SetBool(SafehouseSweepEditorRun,false);SessionState.SetBool(FalseFrontEditorRun,false);SessionState.SetBool(EvidenceChainEditorRun,true);SessionState.SetBool(EvidenceChainArmoredEditorRun,false);SessionState.SetBool(EvidenceChainHelipadDiagnosticRun,false);SessionState.SetBool(PersianEditorRun,false);SessionState.SetBool(ManualEditorRun,false);RunWatch();
        }

        public static void RunEvidenceChainArmoredWatch()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);SessionState.SetBool(SafehouseSweepEditorRun,false);SessionState.SetBool(FalseFrontEditorRun,false);SessionState.SetBool(EvidenceChainEditorRun,true);SessionState.SetBool(EvidenceChainArmoredEditorRun,true);SessionState.SetBool(PersianEditorRun,false);SessionState.SetBool(ManualEditorRun,false);RunWatch();
        }

        public static void RunEvidenceChainHelipadDiagnostic()
        {
            RunEvidenceChainWatch();
            SessionState.SetBool(EvidenceChainHelipadDiagnosticRun,true);
        }

        public static void RunEvidenceChainPersianWatch()
        {
            SessionState.SetBool(SignalTraceEditorRun,false);SessionState.SetBool(SafehouseSweepEditorRun,false);SessionState.SetBool(FalseFrontEditorRun,false);SessionState.SetBool(EvidenceChainEditorRun,true);SessionState.SetBool(EvidenceChainArmoredEditorRun,false);SessionState.SetBool(PersianEditorRun,true);SessionState.SetBool(ManualEditorRun,false);RunWatch();
        }

        public static void RunWatch()
        {
            try{if(IsEvidenceChain)CH03M04EvidenceChainPresentationBuilder.BuildCheckpoint();else if(IsFalseFront)CH03M03FalseFrontPresentationBuilder.BuildCheckpoint();else if(IsSafehouseSweep)CH03M02SafehouseSweepPresentationBuilder.BuildCheckpoint();else if(IsSignalTrace)CH03M01SignalTracePresentationBuilder.BuildLocalCheckpointWithoutVoices();else CH02M03MarketLifelinePresentationBuilder.BuildCheckpoint();}
            catch(Exception exception){Debug.LogException(exception);Complete(false,"Authoring failed: "+exception.Message);return;}
            Directory.CreateDirectory(Output);seeded=finished=watchStarted=won=sawDebrief=sawYasinPortrait=sawLocalizedYasinName=loggedCampaignProjection=capturedFalseFrontBrief=capturedFalseFrontDebrief=capturedEvidenceRouteBrief=false;winningClock=0;probeStore=null;lastTapDiagnostics=null;touchStartWait=evidenceLaunchWaitStarted=deployWaitStarted=0;
            EvidenceChainRouteChoice.Selected=EvidenceChainExtractionRoute.Air;
            SessionState.SetBool(Active,true);started=EditorApplication.timeSinceStartup;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            RepairSelectableRegistry();AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            if(finished||!EditorApplication.isPlaying)return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>600)throw new TimeoutException("Market Lifeline public-input Watch probe timed out.");
                World world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;EntityManager em=world.EntityManager;
                using EntityQuery roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;
                Entity root=roots.GetSingletonEntity();
                if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                CampaignMissionProgressStoreReferenceComponent storeReference=em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root);
                if(probeStore==null)
                {
                    probeStore=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"input-profile-"+Guid.NewGuid().ToString("N")))));
                    int targetIndex=CampaignMissionSequence.IndexOf(TargetMission);
                    for(int index=0;index<targetIndex;index++)
                    {
                        string priorMission=CampaignMissionSequence.IdAt(index);
                        probeStore.Settle(priorMission,"input-seed-"+index,1,true,3,1000,CampaignMissionSequence.Next(priorMission));
                    }
                    probeStore.EnsureAvailable(TargetMission);
                    if(IsChapterThree)probeStore.MarkChapterOpeningSeen(TargetMission);
                }
                if(storeReference.Store!=probeStore)storeReference.Store=probeStore;
                if(!seeded){GameLocalization.SetLocale(SessionState.GetBool(PersianEditorRun,false)?"fa-IR":"en",false);seeded=true;}
                CampaignMissionRuntimeComponent runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                CampaignMissionMarketLifelineState market=em.HasComponent<CampaignMissionMarketLifelineState>(root)?em.GetComponentData<CampaignMissionMarketLifelineState>(root):default;
                CampaignMissionAttemptFactsComponent facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                if(IsEvidenceChain && SessionState.GetBool(EvidenceChainHelipadDiagnosticRun,false) &&
                   runtime.Phase==MissionPhaseKind.Engage && facts.ElapsedMilliseconds>5000)
                {
                    CaptureHelipadInventory(em,root);
                    Complete(true,"helipadInventory=Captured scope=DiagnosticOnly");
                    return;
                }
                if(EditorApplication.timeSinceStartup-lastLog>8)
                {
                    CampaignMissionGuidanceProjectionComponent guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                    bool actorSelected=em.Exists(guidance.SourceEntity)&&em.HasComponent<SelectedUnitTag>(guidance.SourceEntity);
                    bool targetAvailable=UiShellRuntimeGateway.TryReadMissionTutorialTarget(out UiMissionTutorialTarget tutorial);
                    using EntityQuery observations=em.CreateEntityQuery(typeof(AriaPlayObservationComponent));
                    AriaPlayObservationComponent observation=observations.CalculateEntityCount()==1?observations.GetSingleton<AriaPlayObservationComponent>():default;
                    using EntityQuery recommendationQueries=em.CreateEntityQuery(typeof(AssistantRecommendationElement));
                    DynamicBuffer<AssistantRecommendationElement> recommendations=recommendationQueries.CalculateEntityCount()==1?recommendationQueries.GetSingletonBuffer<AssistantRecommendationElement>(true):default;
                    string recommendation=recommendations.IsCreated&&recommendations.Length>0?$"{recommendations[0].RecommendationId}/{recommendations[0].Title}":"none";
                    Debug.Log($"[MarketLifelineInput] phase={runtime.Phase} ready={market.Ready} manifests={market.LegitimateManifestInspected}/{market.CorruptManifestInspected} delivered={market.DeliveredCount}/3 verified={market.ManifestVerified} defeated={em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).HostileDefeatedCount} clock={market.ElapsedMilliseconds} watch={UiShellRuntimeGateway.ReadAriaPlay().Phase} guide={guidance.GuidanceId}/{guidance.Prompt}/{guidance.RecommendationKind}/can{guidance.CanExecute} recommendation={recommendation} actorSelected={actorSelected} target={targetAvailable}/{tutorial.NeedsSelection}/{tutorial.Moving}/{tutorial.BattleAction} observation={observation.Kind}/{observation.TargetId}/{observation.GoalId}");
                    if(IsEvidenceChain)Debug.Log($"[EvidenceChainInput] phase={runtime.Phase} aboard={facts.ExtractionPassengersAboard}/2 carrierLeg={facts.ExtractionCarrierLegCount}/2 delivered={facts.ExtractionPassengersDelivered}/2 civilianLoss={facts.CivilianLossCount} carrierLost={facts.ExtractionCarrierLost} aircraftLost={facts.ExtractionAircraftLost} departed={facts.ExtractionDeparted} clock={facts.ElapsedMilliseconds} watch={UiShellRuntimeGateway.ReadAriaPlay().Phase} lesson={guidance.GuidanceId} observation={observation.Kind}/{observation.TargetId}/{observation.GoalId}");
                    ScreenCapture.CaptureScreenshot(Output+"/input-current.png");lastLog=EditorApplication.timeSinceStartup;
                }
                if(!IsEvidenceChain&&market.Failure!=MarketLifelineFailure.None)throw new InvalidOperationException("Mission failed: "+market.Failure);
                if(IsEvidenceChain && runtime.Outcome==MissionOutcomeKind.Defeat)throw new InvalidOperationException("Evidence Chain failed: civilianLoss="+facts.CivilianLossCount+" carrierLost="+facts.ExtractionCarrierLost+" aircraftLost="+facts.ExtractionAircraftLost+" timedOut="+facts.ExtractionTimedOut);
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if(IsEvidenceChain)
                    {
                        if(facts.ExtractionPassengersDelivered!=2||facts.ExtractionCarrierLegCount!=2||facts.ExtractionDeparted==0||facts.CivilianLossCount!=0)
                            throw new InvalidOperationException("Evidence Chain victory lacks both protected passengers, APC leg, or departure.");
                    }
                    else if(market.LegitimateManifestInspected==0||market.CorruptManifestInspected==0||market.DeliveredCount<3||market.ManifestVerified==0||market.VictoryHoldMilliseconds<10000)throw new InvalidOperationException("Victory lacks both inspections, legitimate delivery, or market hold.");
                    if(!won){won=true;winningClock=IsEvidenceChain?facts.ElapsedMilliseconds:market.ElapsedMilliseconds;Debug.Log("[MarketLifelineInput] victory=Passed awaiting=debrief-and-return");}
                }
                var watch=UiShellRuntimeGateway.ReadAriaPlay();
                if(watch.Active){watchStarted=true;touch?.Dispose();touch=null;return;}
                if(watchStarted&&!won)
                {
                    using EntityQuery sessions=em.CreateEntityQuery(typeof(AriaPlaySessionComponent));
                    byte reason=sessions.CalculateEntityCount()==1?sessions.GetSingleton<AriaPlaySessionComponent>().StopReason:(byte)255;
                    throw new InvalidOperationException("Watch stopped before outcome: "+watch.Phase+" reason="+reason);
                }
                if(!EnsureTouch())return;touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<1)return;
                NarrativeSequenceView narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
                if(narrative!=null&&Visible(narrative,"rootGroup"))
                {
                    briefWaitStarted=0;
                    if((IsFalseFront||IsEvidenceChain) && (won ? !capturedFalseFrontDebrief : !capturedFalseFrontBrief))
                    {
                        string prefix=IsEvidenceChain?"evidence-chain":"false-front";
                        ScreenCapture.CaptureScreenshot(Output+(won?"/"+prefix+"-debrief.png":"/"+prefix+"-brief.png"));
                        if(won)capturedFalseFrontDebrief=true;else capturedFalseFrontBrief=true;
                        return;
                    }
                    if(!IsChapterThree&&!won&&runtime.MissionId.Equals(TargetMission)&&runtime.Phase==MissionPhaseKind.InteractiveBrief&&!sawYasinPortrait)
                    {
                        Sprite portrait=narrative.DialogueView.CurrentPortraitSprite;if(portrait==null)return;
                        const string expected="Assets/Game/Art/UI/Portraits/Generated/Portrait_Yasin_MarketLifeline.png";
                        string actual=AssetDatabase.GetAssetPath(portrait);
                        if(actual!=expected)throw new InvalidOperationException("Yasin dialogue portrait mismatch: "+actual);
                        TMP_Text speakerName=typeof(NarrativeDialogueView).GetField("speakerNameText",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(narrative.DialogueView) as TMP_Text;
                        string expectedName=GameLocalization.CurrentLocaleCode=="fa-IR"?"یاسین برکات":"Yasin Barakat";
                        string sourceName=speakerName is RTLTMPro.RTLTextMeshPro rtlName?rtlName.OriginalText:speakerName?.text;
                        if(sourceName!=expectedName)throw new InvalidOperationException($"Yasin dialogue name mismatch: expected '{expectedName}', actual '{sourceName ?? "<missing>"}'.");
                        sawLocalizedYasinName=true;
                        ScreenCapture.CaptureScreenshot(Output+"/yasin-persian-dialogue.png");sawYasinPortrait=true;
                        Debug.Log("[MarketLifelineInput] yasinPortrait=Passed yasinName=Passed asset="+actual+" name="+sourceName+" locale="+GameLocalization.CurrentLocaleCode);
                    }
                    if(won)sawDebrief=true;SkipNarrative(narrative);return;
                }
                if(won)
                {
                    MissionResultPopupView result=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                    if(result!=null){Tap(typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(result) as Button);return;}
                    CampaignOperationsScreenView returned=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                    if(returned!=null&&Ready(returned.LaunchMissionButton))
                    {
                        if(!sawDebrief)throw new InvalidOperationException("Mandatory debrief was not presented.");
                        string returnedMission=UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel returnedModel)?returnedModel.SelectedMission.MissionId:"unknown";
                        if(!IsChapterThree&&!sawYasinPortrait)throw new InvalidOperationException("Yasin portrait was not presented.");
                        if(!IsChapterThree&&!sawLocalizedYasinName)throw new InvalidOperationException("Yasin localized speaker name was not presented.");
                        Complete(true,$"publicEntry=Passed normalInput=Passed aria=Passed signalTrace={(IsSignalTrace?"Passed":"n/a")} safehouseSweep={(IsSafehouseSweep?"Passed":"n/a")} falseFront={(IsFalseFront?"Passed":"n/a")} evidenceChain={(IsEvidenceChain?"Passed":"n/a")} yasinPortrait={(IsChapterThree?"n/a":"Passed")} yasinName={(IsChapterThree?"n/a":"Passed")} locale={GameLocalization.CurrentLocaleCode} clock={winningClock} debrief=Passed resultReturn=Passed returned={returnedMission}");
                    }
                    return;
                }
                if(runtime.MissionId.Equals(TargetMission)&&runtime.Phase>=MissionPhaseKind.FindSquad)
                {
                    Button[] buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
                    Tap(buttons.FirstOrDefault(b=>b.name=="ConfirmWatchAria"&&Ready(b))??buttons.FirstOrDefault(b=>b.name=="WatchAriaPlay"&&Ready(b)));return;
                }
                MissionBriefingScreenView briefing=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();
                if(briefing!=null&&Ready(briefing.DeployOperationButton))
                {
                    if(deployWaitStarted==0)deployWaitStarted=EditorApplication.timeSinceStartup;
                    if(EditorApplication.timeSinceStartup-deployWaitStarted>35)
                        throw new InvalidOperationException("Deploy did not advance after 35 seconds of normal touches.");
                    if(IsEvidenceChain)
                    {
                        if(!capturedEvidenceRouteBrief)
                        {
                            ScreenCapture.CaptureScreenshot(Output+"/evidence-chain-route-brief.png");
                            capturedEvidenceRouteBrief=true;
                            return;
                        }
                        if(IsEvidenceChainArmored && EvidenceChainRouteChoice.Selected!=EvidenceChainExtractionRoute.Armored)
                        {
                            Tap(briefing.EvidenceChainArmoredRouteButton);
                            return;
                        }
                    }
                    Tap(briefing.DeployOperationButton);return;
                }
                CampaignOperationsScreenView campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                if(campaign!=null&&UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel model))
                {
                    if(IsChapterThree&&!loggedCampaignProjection)
                    {
                        CampaignMissionCatalogComponent projectedCatalog=em.GetComponentData<CampaignMissionCatalogComponent>(root);
                        ref CampaignMissionCatalogBlob blob=ref projectedCatalog.Blob.Value;
                        string[] projectedIds=new string[blob.Missions.Length];for(int i=0;i<projectedIds.Length;i++)projectedIds[i]=blob.Missions[i].MissionId.ToString();
                        string catalogIds=string.Join(",",projectedIds);
                        string progressIds=string.Join(",",probeStore.ReadAll().Select(entry=>$"{entry.missionId}:{entry.available}"));
                        Button chapterThree=campaign.ChapterThreeButton;
                        Debug.Log($"[SignalTraceInputProjection] selected={model.SelectedMission.MissionId} availableMask={model.AvailableMissionMask} modelVersion={model.Version} catalogCount={blob.Missions.Length} chapterThree={(chapterThree==null?"missing":$"active={chapterThree.IsActive()} interactable={chapterThree.IsInteractable()} enabled={chapterThree.enabled}")} catalog={catalogIds} progress={progressIds}");
                        loggedCampaignProjection=true;
                    }
                    if(IsChapterThree&&!campaign.IsChapterThree){Tap(campaign.ChapterThreeButton);return;}
                    if(!IsChapterThree&&!campaign.IsChapterTwo){Tap(Ready(campaign.ChapterTwoButton)?campaign.ChapterTwoButton:campaign.ChapterTwoOverviewButton);return;}
                    if(model.SelectedMission.MissionId!=TargetMission){Tap(campaign.MissionNodeButtons[IsEvidenceChain?3:IsFalseFront?2:IsSafehouseSweep?1:IsSignalTrace?0:2]);return;}
                    if(IsEvidenceChain)
                    {
                        if(evidenceLaunchWaitStarted==0)evidenceLaunchWaitStarted=EditorApplication.timeSinceStartup;
                        if(EditorApplication.timeSinceStartup-evidenceLaunchWaitStarted>30)
                            throw new InvalidOperationException("Start Briefing did not advance after 30 seconds of normal touches.");
                    }
                    Tap(campaign.LaunchMissionButton);return;
                }
                if(runtime.MissionId.Equals(TargetMission))
                {
                    if(runtime.Phase==MissionPhaseKind.InteractiveBrief)
                    {
                        if(briefWaitStarted==0)briefWaitStarted=EditorApplication.timeSinceStartup;
                        if(EditorApplication.timeSinceStartup-briefWaitStarted>45)throw new InvalidOperationException("Required "+TargetMission+" briefing did not become visible.");
                    }
                    return;
                }
                foreach(UIShellRouteButtonView route in UnityEngine.Object.FindObjectsByType<UIShellRouteButtonView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                    if(route.Route==UIRoute.Campaign&&Ready(route.GetComponent<Button>())){Tap(route.GetComponent<Button>());return;}
            }
            catch(Exception exception){Debug.LogException(exception);Complete(false,exception.Message);}
        }

        private static void SkipNarrative(NarrativeSequenceView view)
        {
            var confirm=view.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");object owner=confirming?(object)confirm:view.PlaybackControlsView;
            Tap(owner?.GetType().GetField(confirming?"confirmButton":"skipButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(owner) as Button);
        }
        private static bool Ready(Button button)=>button!=null&&button.IsActive()&&button.IsInteractable();
        private static bool Visible(object owner,string field){CanvasGroup group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as CanvasGroup;return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static bool EnsureTouch()
        {
            if(touch!=null&&touch.IsRunning){touchStartWait=0;return true;}
            if(touch==null)
            {
                EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                touch=new AriaTouchInputUiSystemHelper();
            }
            if(touch.Start()){touchStartWait=0;return true;}
            if(touchStartWait==0)touchStartWait=EditorApplication.timeSinceStartup;
            if(EditorApplication.timeSinceStartup-touchStartWait>20)
                throw new InvalidOperationException("Could not start public touch input after physical input release wait.");
            return false;
        }
        private static void RepairSelectableRegistry()
        {
            const BindingFlags flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic;Type type=typeof(Selectable);FieldInfo arrayField=type.GetField("s_Selectables",flags),countField=type.GetField("s_SelectableCount",flags),enabledField=type.GetField("m_EnableCalled",flags),indexField=type.GetField("m_CurrentIndex",flags);if(arrayField==null||countField==null||enabledField==null||indexField==null)return;
            Selectable[] all=Resources.FindObjectsOfTypeAll<Selectable>().Where(x=>x!=null&&x.gameObject.scene.IsValid()).ToArray();arrayField.SetValue(null,new Selectable[Mathf.NextPowerOfTwo(Mathf.Max(32,all.Length*2))]);countField.SetValue(null,0);foreach(Selectable selectable in all){enabledField.SetValue(selectable,false);indexField.SetValue(selectable,-1);}foreach(Selectable selectable in all.Where(x=>x.isActiveAndEnabled)){selectable.enabled=false;selectable.enabled=true;}Debug.Log($"[MarketLifelineInput] selectableRegistry=Rebuilt live={all.Count(x=>x.isActiveAndEnabled)} capacity={((Selectable[])arrayField.GetValue(null)).Length}");
        }
        private static void Tap(Button button)
        {
            if(!Ready(button))return;RectTransform rect=(RectTransform)button.transform;Canvas canvas=button.GetComponentInParent<Canvas>();Vector2 point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            string diagnostics=$"{button.name}@{point.x:0},{point.y:0}/{Screen.width}x{Screen.height}";
            if(lastTapDiagnostics!=diagnostics)
            {
                lastTapDiagnostics=diagnostics;Debug.Log("[MarketLifelineInput] target="+diagnostics);
                if(IsEvidenceChain && (button.name=="WatchAriaPlay" || button.name=="ConfirmWatchAria" || button.name=="LaunchMissionButton" || button.name=="DeployOperationButton") && EventSystem.current!=null)
                {
                    var hits=new List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                    Debug.Log("[EvidenceChainInput] watchRaycast="+string.Join(",",hits.Take(5).Select(hit=>hit.gameObject.name))+
                        " interactable="+button.IsInteractable());
                }
            }
            if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime)){lastInput=EditorApplication.timeSinceStartup;Debug.Log("[MarketLifelineInput] touch="+button.name);}
        }
        private static void Complete(bool pass,string detail)
        {
            if(finished)return;finished=true;touch?.Dispose();touch=null;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;ScreenCapture.CaptureScreenshot(Output+"/input-last.png");
            SessionState.SetBool(EvidenceChainHelipadDiagnosticRun,false);
            Debug.Log("[MarketLifelineInput] result="+(pass?"Passed":"Failed")+" "+detail);
            if(SessionState.GetBool(ManualEditorRun,false))
            {
                SessionState.SetBool(ManualEditorRun,false);
                SessionState.SetBool(PersianEditorRun,false);
                SessionState.SetBool(SignalTraceEditorRun,false);
                SessionState.SetBool(SafehouseSweepEditorRun,false);
                SessionState.SetBool(FalseFrontEditorRun,false);
                SessionState.SetBool(EvidenceChainEditorRun,false);
                SessionState.SetBool(EvidenceChainArmoredEditorRun,false);
                SessionState.SetBool(EvidenceChainHelipadDiagnosticRun,false);
                AssetDatabase.AllowAutoRefresh();
                EditorApplication.ExitPlaymode();
            }
            else MissionEditorValidationExit.Complete(pass);
        }
        private static bool IsSignalTrace=>SessionState.GetBool(SignalTraceEditorRun,false);
        private static bool IsSafehouseSweep=>SessionState.GetBool(SafehouseSweepEditorRun,false);
        private static bool IsFalseFront=>SessionState.GetBool(FalseFrontEditorRun,false);
        private static bool IsEvidenceChain=>SessionState.GetBool(EvidenceChainEditorRun,false);
        private static bool IsEvidenceChainArmored=>SessionState.GetBool(EvidenceChainArmoredEditorRun,false);

        private static void CaptureHelipadInventory(EntityManager em,Entity root)
        {
            var lines=new List<string>();
            var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
            lines.Add("missionAircraft="+extraction.Aircraft+" name="+(em.Exists(extraction.Aircraft)?em.GetName(extraction.Aircraft).ToString():"missing"));
            using(EntityQuery grids=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent)))
            {
                if(grids.CalculateEntityCount()==1 && em.Exists(extraction.Aircraft) && em.HasComponent<UnitGrid>(extraction.Aircraft))
                {
                    Entity gridEntity=grids.GetSingletonEntity();
                    GridConfig grid=em.GetComponentData<GridConfig>(gridEntity);
                    NativeArray<GridWalkable> walkable=em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
                    var blocked=em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked;
                    var occupied=em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
                    var aircraftCell=em.GetComponentData<UnitGrid>(extraction.Aircraft).Cell;
                    var handoffCell=GridUtils.WorldToCell(grid,extraction.HandoffCenter);
                    lines.Add($"grid={grid.Width}x{grid.Height} cellSize={grid.CellSize} origin={grid.Origin} aircraftCell={aircraftCell} handoffCell={handoffCell} aircraftFootprint={em.GetComponentData<UnitFootprint>(extraction.Aircraft).Size}");
                    int minX=Math.Min(aircraftCell.x,handoffCell.x)-12,maxX=Math.Max(aircraftCell.x,handoffCell.x)+12;
                    int minY=Math.Min(aircraftCell.y,handoffCell.y)-12,maxY=Math.Max(aircraftCell.y,handoffCell.y)+12;
                    for(int y=minY;y<=maxY;y++)
                    {
                        var row=new System.Text.StringBuilder();
                        for(int x=minX;x<=maxX;x++)
                        {
                            var cell=new Unity.Mathematics.int2(x,y);
                            if(!GridUtils.InBounds(cell,grid.Width,grid.Height)){row.Append(' ');continue;}
                            int index=GridUtils.CellToIndex(cell,grid.Width);
                            row.Append(cell.Equals(aircraftCell)?'H':cell.Equals(handoffCell)?'R':walkable[index].Value==0?'#':blocked.IsCreated&&blocked.IsSet(index)?'B':occupied.IsCreated&&occupied.IsSet(index)?'O':'.');
                        }
                        lines.Add($"gridRow y={y} x={minX} {row}");
                    }
                }
            }
            using NativeArray<Entity> entities=em.GetAllEntities(Allocator.Temp);
            foreach(Entity entity in entities)
            {
                string name=em.GetName(entity).ToString();
                bool heli=name.IndexOf("heli",StringComparison.OrdinalIgnoreCase)>=0;
                bool world=em.HasComponent<LocalToWorld>(entity);
                var position=world?em.GetComponentData<LocalToWorld>(entity).Position:default;
                bool nearby=world && (position.x-1009.9f)*(position.x-1009.9f)+(position.z-390.8f)*(position.z-390.8f)<25f*25f;
                if(!heli&&!nearby)continue;
                lines.Add($"entity={entity} name={name} world={(world?position.ToString():"none")} nearby={nearby} parent={(em.HasComponent<Parent>(entity)?em.GetComponentData<Parent>(entity).Value.ToString():"none")} children={(em.HasBuffer<Child>(entity)?em.GetBuffer<Child>(entity,true).Length:0)} disabled={em.HasComponent<Disabled>(entity)}");
            }
            foreach(Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if(!transform.gameObject.scene.IsValid()||transform.name.IndexOf("heli",StringComparison.OrdinalIgnoreCase)<0)continue;
                lines.Add($"gameObject={transform.name} world={transform.position} active={transform.gameObject.activeInHierarchy} scene={transform.gameObject.scene.name}");
            }
            File.WriteAllLines(Output+"/evidence-chain-helipad-inventory.txt",lines);
            Debug.Log("[EvidenceChainHelipad] inventory=Captured rows="+lines.Count);
        }
        private static bool IsChapterThree=>IsSignalTrace||IsSafehouseSweep||IsFalseFront||IsEvidenceChain;
        private static string TargetMission=>IsEvidenceChain?CampaignMissionSequence.EvidenceChain:IsFalseFront?CampaignMissionSequence.FalseFront:IsSafehouseSweep?CampaignMissionSequence.SafehouseSweep:IsSignalTrace?CampaignMissionSequence.SignalTrace:CampaignMissionSequence.MarketLifeline;
    }
}
