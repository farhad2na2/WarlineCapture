using System;
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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        private const string Output="/private/tmp/warline-market-lifeline";
        private static bool seeded, finished, watchStarted, won, sawDebrief, sawYasinPortrait, sawLocalizedYasinName, loggedCampaignProjection, capturedFalseFrontBrief, capturedFalseFrontDebrief;
        private static double started, lastInput, lastLog, briefWaitStarted;
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

        public static void RunWatch()
        {
            try{if(IsFalseFront)CH03M03FalseFrontPresentationBuilder.BuildCheckpoint();else if(IsSafehouseSweep)CH03M02SafehouseSweepPresentationBuilder.BuildCheckpoint();else if(IsSignalTrace)CH03M01SignalTracePresentationBuilder.BuildLocalCheckpointWithoutVoices();else CH02M03MarketLifelinePresentationBuilder.BuildCheckpoint();}
            catch(Exception exception){Debug.LogException(exception);Complete(false,"Authoring failed: "+exception.Message);return;}
            Directory.CreateDirectory(Output);seeded=finished=watchStarted=won=sawDebrief=sawYasinPortrait=sawLocalizedYasinName=loggedCampaignProjection=capturedFalseFrontBrief=capturedFalseFrontDebrief=false;winningClock=0;probeStore=null;lastTapDiagnostics=null;
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
                    ScreenCapture.CaptureScreenshot(Output+"/input-current.png");lastLog=EditorApplication.timeSinceStartup;
                }
                if(market.Failure!=MarketLifelineFailure.None)throw new InvalidOperationException("Mission failed: "+market.Failure);
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if(market.LegitimateManifestInspected==0||market.CorruptManifestInspected==0||market.DeliveredCount<3||market.ManifestVerified==0||market.VictoryHoldMilliseconds<10000)throw new InvalidOperationException("Victory lacks both inspections, legitimate delivery, or market hold.");
                    if(!won){won=true;winningClock=market.ElapsedMilliseconds;Debug.Log("[MarketLifelineInput] victory=Passed awaiting=debrief-and-return");}
                }
                var watch=UiShellRuntimeGateway.ReadAriaPlay();
                if(watch.Active){watchStarted=true;touch?.Dispose();touch=null;return;}
                if(watchStarted&&!won)
                {
                    using EntityQuery sessions=em.CreateEntityQuery(typeof(AriaPlaySessionComponent));
                    byte reason=sessions.CalculateEntityCount()==1?sessions.GetSingleton<AriaPlaySessionComponent>().StopReason:(byte)255;
                    throw new InvalidOperationException("Watch stopped before outcome: "+watch.Phase+" reason="+reason);
                }
                EnsureTouch();touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<1)return;
                NarrativeSequenceView narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
                if(narrative!=null&&Visible(narrative,"rootGroup"))
                {
                    briefWaitStarted=0;
                    if(IsFalseFront && (won ? !capturedFalseFrontDebrief : !capturedFalseFrontBrief))
                    {
                        ScreenCapture.CaptureScreenshot(Output+(won?"/false-front-debrief.png":"/false-front-brief.png"));
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
                        Complete(true,$"publicEntry=Passed normalInput=Passed aria=Passed signalTrace={(IsSignalTrace?"Passed":"n/a")} safehouseSweep={(IsSafehouseSweep?"Passed":"n/a")} falseFront={(IsFalseFront?"Passed":"n/a")} yasinPortrait={(IsChapterThree?"n/a":"Passed")} yasinName={(IsChapterThree?"n/a":"Passed")} locale={GameLocalization.CurrentLocaleCode} clock={winningClock} debrief=Passed resultReturn=Passed returned={returnedMission}");
                    }
                    return;
                }
                if(runtime.MissionId.Equals(TargetMission)&&runtime.Phase>=MissionPhaseKind.FindSquad)
                {
                    Button[] buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
                    Tap(buttons.FirstOrDefault(b=>b.name=="ConfirmWatchAria"&&Ready(b))??buttons.FirstOrDefault(b=>b.name=="WatchAriaPlay"&&Ready(b)));return;
                }
                MissionBriefingScreenView briefing=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();
                if(briefing!=null&&Ready(briefing.DeployOperationButton)){Tap(briefing.DeployOperationButton);return;}
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
                    if(model.SelectedMission.MissionId!=TargetMission){Tap(campaign.MissionNodeButtons[IsFalseFront?2:IsSafehouseSweep?1:IsSignalTrace?0:2]);return;}
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
        private static void EnsureTouch()
        {
            if(touch!=null&&touch.IsRunning)return;
            touch?.Dispose();
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            touch=new AriaTouchInputUiSystemHelper();
            if(!touch.Start())throw new InvalidOperationException("Could not start public touch input.");
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
            if(lastTapDiagnostics!=diagnostics){lastTapDiagnostics=diagnostics;Debug.Log("[MarketLifelineInput] target="+diagnostics);}
            if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime)){lastInput=EditorApplication.timeSinceStartup;Debug.Log("[MarketLifelineInput] touch="+button.name);}
        }
        private static void Complete(bool pass,string detail)
        {
            if(finished)return;finished=true;touch?.Dispose();touch=null;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;ScreenCapture.CaptureScreenshot(Output+"/input-last.png");
            Debug.Log("[MarketLifelineInput] result="+(pass?"Passed":"Failed")+" "+detail);
            if(SessionState.GetBool(ManualEditorRun,false))
            {
                SessionState.SetBool(ManualEditorRun,false);
                SessionState.SetBool(PersianEditorRun,false);
                SessionState.SetBool(SignalTraceEditorRun,false);
                SessionState.SetBool(SafehouseSweepEditorRun,false);
                SessionState.SetBool(FalseFrontEditorRun,false);
                AssetDatabase.AllowAutoRefresh();
                EditorApplication.ExitPlaymode();
            }
            else MissionEditorValidationExit.Complete(pass);
        }
        private static bool IsSignalTrace=>SessionState.GetBool(SignalTraceEditorRun,false);
        private static bool IsSafehouseSweep=>SessionState.GetBool(SafehouseSweepEditorRun,false);
        private static bool IsFalseFront=>SessionState.GetBool(FalseFrontEditorRun,false);
        private static bool IsChapterThree=>IsSignalTrace||IsSafehouseSweep||IsFalseFront;
        private static string TargetMission=>IsFalseFront?CampaignMissionSequence.FalseFront:IsSafehouseSweep?CampaignMissionSequence.SafehouseSweep:IsSignalTrace?CampaignMissionSequence.SignalTrace:CampaignMissionSequence.MarketLifeline;
    }
}
