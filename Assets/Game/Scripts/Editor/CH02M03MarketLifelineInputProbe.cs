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

namespace Game.Editor
{
    /// <summary>Public UI-entry Watch ARIA probe. Gameplay orders are issued only by ARIA after a real touch confirmation.</summary>
    [InitializeOnLoad]
    public static class CH02M03MarketLifelineInputProbe
    {
        private const string Active="Warline.MarketLifeline.InputProbe";
        private const string ManualEditorRun="Warline.MarketLifeline.InputProbe.ManualEditorRun";
        private const string Output="/private/tmp/warline-market-lifeline";
        private static bool seeded, finished, watchStarted, won, sawDebrief;
        private static double started, lastInput, lastLog, briefWaitStarted;
        private static int winningClock;
        private static AriaTouchInputUiSystemHelper touch;
        private static CampaignMissionProgressStore probeStore;

        static CH02M03MarketLifelineInputProbe(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Tick;}

        [MenuItem("Game/Campaign/Market Lifeline/Run Normal Input Watch Probe")]
        public static void RunWatchFromOpenEditor()
        {
            SessionState.SetBool(ManualEditorRun,true);
            RunWatch();
        }

        public static void RunWatch()
        {
            try{CH02M03MarketLifelinePresentationBuilder.BuildCheckpoint();}
            catch(Exception exception){Debug.LogException(exception);Complete(false,"Authoring failed: "+exception.Message);return;}
            Directory.CreateDirectory(Output);seeded=finished=watchStarted=won=sawDebrief=false;winningClock=0;probeStore=null;
            SessionState.SetBool(Active,true);started=EditorApplication.timeSinceStartup;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
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
                    probeStore.EnsureAvailable(CampaignMissionSequence.MarketLifeline);
                }
                if(storeReference.Store!=probeStore)storeReference.Store=probeStore;
                if(!seeded){GameLocalization.SetLocale("en",false);seeded=true;}
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
                        Complete(true,$"publicEntry=Passed normalInput=Passed aria=Passed clock={winningClock} debrief=Passed resultReturn=Passed returned={returnedMission}");
                    }
                    return;
                }
                if(runtime.MissionId.Equals(CampaignMissionSequence.MarketLifeline)&&runtime.Phase>=MissionPhaseKind.FindSquad)
                {
                    Button[] buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
                    Tap(buttons.FirstOrDefault(b=>b.name=="ConfirmWatchAria"&&Ready(b))??buttons.FirstOrDefault(b=>b.name=="WatchAriaPlay"&&Ready(b)));return;
                }
                MissionBriefingScreenView briefing=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();
                if(briefing!=null&&Ready(briefing.DeployOperationButton)){Tap(briefing.DeployOperationButton);return;}
                CampaignOperationsScreenView campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                if(campaign!=null&&UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel model))
                {
                    if(!campaign.IsChapterTwo){Tap(Ready(campaign.ChapterTwoButton)?campaign.ChapterTwoButton:campaign.ChapterTwoOverviewButton);return;}
                    if(model.SelectedMission.MissionId!=CampaignMissionSequence.MarketLifeline){Tap(campaign.MissionNodeButtons[2]);return;}
                    Tap(campaign.LaunchMissionButton);return;
                }
                if(runtime.MissionId.Equals(CampaignMissionSequence.MarketLifeline))
                {
                    if(runtime.Phase==MissionPhaseKind.InteractiveBrief)
                    {
                        if(briefWaitStarted==0)briefWaitStarted=EditorApplication.timeSinceStartup;
                        if(EditorApplication.timeSinceStartup-briefWaitStarted>45)throw new InvalidOperationException("Required Market Lifeline briefing did not become visible.");
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
        private static void EnsureTouch(){if(touch!=null)return;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Could not start public touch input.");}
        private static void Tap(Button button)
        {
            if(!Ready(button))return;RectTransform rect=(RectTransform)button.transform;Canvas canvas=button.GetComponentInParent<Canvas>();Vector2 point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime)){lastInput=EditorApplication.timeSinceStartup;Debug.Log("[MarketLifelineInput] touch="+button.name);}
        }
        private static void Complete(bool pass,string detail)
        {
            if(finished)return;finished=true;touch?.Dispose();touch=null;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;ScreenCapture.CaptureScreenshot(Output+"/input-last.png");
            Debug.Log("[MarketLifelineInput] result="+(pass?"Passed":"Failed")+" "+detail);
            if(SessionState.GetBool(ManualEditorRun,false))
            {
                SessionState.SetBool(ManualEditorRun,false);
                AssetDatabase.AllowAutoRefresh();
                EditorApplication.ExitPlaymode();
            }
            else MissionEditorValidationExit.Complete(pass);
        }
    }
}
