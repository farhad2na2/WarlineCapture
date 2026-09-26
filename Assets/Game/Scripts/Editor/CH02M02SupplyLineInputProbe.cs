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
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    // Test-only profile seeding; all navigation and mission commands use ordinary screen input.
    [InitializeOnLoad]
    public static class CH02M02SupplyLineInputProbe
    {
        private const string Active="Warline.SupplyLine.InputProbe",Mode="Warline.SupplyLine.InputMode";
        private static string Output=>System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../"))+"/"+new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(Application.dataPath)).Name+"-evidence";
        private static bool seeded,finished,watchStarted,won,sawDebrief;
        private static int winningClock;
        private static double started,lastInput,lastCapture,lastLog;
        private static AriaTouchInputUiSystemHelper touch;
        private static bool Manual=>SessionState.GetBool(Mode,false);
        static CH02M02SupplyLineInputProbe(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Tick;}
        [MenuItem("Game/Campaign/Supply Line/Run Normal Input Watch Probe")]
        public static void RunWatch(){SessionState.SetBool(Mode,false);Begin();}
        [MenuItem("Game/Campaign/Supply Line/Run Normal Input Manual Probe")]
        public static void RunManual(){SessionState.SetBool(Mode,true);Begin();}
        private static void Begin()
        {
            try{CH02M02SupplyLinePresentationBuilder.BuildCheckpoint();}
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);return;}
            Directory.CreateDirectory(Output);seeded=finished=watchStarted=won=sawDebrief=false;winningClock=0;
            SessionState.SetBool(Active,true);started=EditorApplication.timeSinceStartup;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished || !EditorApplication.isPlaying)return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>900)throw new TimeoutException("Supply Line public-input probe timed out.");
                var world=World.DefaultGameObjectInjectionWorld;if(world==null || !world.IsCreated)return;
                var em=world.EntityManager;using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));
                if(roots.CalculateEntityCount()!=1)return;var root=roots.GetSingletonEntity();
                if(!seeded)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    var store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"input-profile-"+Guid.NewGuid().ToString("N")))));
                    store.EnsureAvailable(CampaignMissionSequence.Gridlock);store.EnsureAvailable(CampaignMissionSequence.SupplyLine);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                    GameLocalization.SetLocale("en",false);seeded=true;
                }
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                var state=em.HasComponent<CampaignMissionSupplyLineState>(root)?em.GetComponentData<CampaignMissionSupplyLineState>(root):default;
                if(EditorApplication.timeSinceStartup-lastCapture>5)
                {ScreenCapture.CaptureScreenshot(Output+"/input-current.png");lastCapture=EditorApplication.timeSinceStartup;}
                if(EditorApplication.timeSinceStartup-lastLog>10)
                {
                    Debug.Log($"[SupplyLineInput] mode={(Manual?"Manual":"Watch")} phase={runtime.Phase} ready={state.Ready} clock={state.ElapsedMilliseconds} route={state.RouteRecovered} fuel={state.StoredFuel} allocated={state.AllocatedCivilianBarrels} watch={UiShellRuntimeGateway.ReadAriaPlay().Phase}");
                    lastLog=EditorApplication.timeSinceStartup;
                }
                if(state.Failure!=SupplyLineFailure.None)throw new InvalidOperationException("Mission failed: "+state.Failure);
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if(state.RouteRecovered==0 || state.AllocatedCivilianBarrels<20 || state.StoredFuel<40 || state.HoldMilliseconds<20000)throw new InvalidOperationException("Victory lacks required facts.");
                    if(!won){won=true;winningClock=state.ElapsedMilliseconds;Debug.Log("[SupplyLineInput] victory=Passed awaiting=debrief-and-return");}
                }
                var watch=UiShellRuntimeGateway.ReadAriaPlay();
                if(!Manual && watch.Active)
                {watchStarted=true;touch?.Dispose();touch=null;return;}
                if(watchStarted && !won)
                {
                    using var sessions=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.AriaPlaySessionComponent));
                    var stopped=sessions.GetSingleton<Game.UI.Shell.Contracts.Ecs.AriaPlaySessionComponent>();
                    throw new InvalidOperationException("Watch stopped before outcome: "+watch.Phase+" reason="+stopped.StopReason);
                }
                EnsureTouch();touch.Tick(Time.unscaledTime);
                if(touch.IsBusy || EditorApplication.timeSinceStartup-lastInput<1)return;
                var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
                if(narrative!=null && Visible(narrative,"rootGroup"))
                {
                    if(won)sawDebrief=true;
                    var confirm=narrative.SkipConfirmationView;bool confirming=confirm!=null && Visible(confirm,"group");
                    object owner=confirming?(object)confirm:narrative.PlaybackControlsView;
                    Tap(owner?.GetType().GetField(confirming?"confirmButton":"skipButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(owner) as Button);return;
                }
                if(won)
                {
                    var result=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                    if(result!=null){Tap(typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(result) as Button);return;}
                    var returned=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                    if(returned!=null && Ready(returned.LaunchMissionButton))
                    {
                        if(!sawDebrief)throw new InvalidOperationException("Mandatory debrief was not presented.");
                        Complete(true,$"publicEntry=Passed mode={(Manual?"Manual":"Watch")} route=Passed reserve=Passed clock={winningClock} debrief=Passed resultReturn=Passed");
                    }
                    return;
                }
                if(runtime.MissionId.Equals(CampaignMissionSequence.SupplyLine) && runtime.Phase>=MissionPhaseKind.FindSquad)
                {
                    if(Manual)return;
                    var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
                    Tap(buttons.FirstOrDefault(b=>b.name=="ConfirmWatchAria" && Ready(b))??buttons.FirstOrDefault(b=>b.name=="WatchAriaPlay" && Ready(b)));return;
                }
                var briefing=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();
                if(briefing!=null && Ready(briefing.DeployOperationButton)){Tap(briefing.DeployOperationButton);return;}
                var campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                if(campaign!=null && UiShellRuntimeGateway.TryReadCampaignOperations(out var model))
                {
                    if(!campaign.IsChapterTwo){Tap(Ready(campaign.ChapterTwoButton)?campaign.ChapterTwoButton:campaign.ChapterTwoOverviewButton);return;}
                    if(model.SelectedMission.MissionId!=CampaignMissionSequence.SupplyLine){Tap(campaign.MissionNodeButtons[1]);return;}
                    Tap(campaign.LaunchMissionButton);return;
                }
                foreach(var route in UnityEngine.Object.FindObjectsByType<UIShellRouteButtonView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                    if(route.Route==UIRoute.Campaign && Ready(route.GetComponent<Button>())){Tap(route.GetComponent<Button>());return;}
            }
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);}
        }
        private static bool Ready(Button b)=>b!=null && b.IsActive() && b.IsInteractable();
        private static bool Visible(object owner,string field){var group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as CanvasGroup;return group!=null && group.alpha>.9f && group.gameObject.activeInHierarchy;}
        private static void EnsureTouch()
        {
            if(touch!=null)return;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Could not start public touch input.");
        }
        private static void Tap(Button b)
        {
            if(!Ready(b))return;var rect=(RectTransform)b.transform;var canvas=b.GetComponentInParent<Canvas>();
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
            if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime)){lastInput=EditorApplication.timeSinceStartup;Debug.Log("[SupplyLineInput] touch="+b.name);}
        }
        public static bool SubmitManualTouch(float x,float y,float endX,float endY,bool drag)
        {
            if(!Manual || touch==null || touch.IsBusy)return false;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            return touch.TryGesture(new Vector2(x,y),new Vector2(endX,endY),drag ? .5f : .3f,drag ? .9f : 0,Time.unscaledTime);
        }
        private static void Complete(bool pass,string detail)
        {
            if(finished)return;finished=true;touch?.Dispose();touch=null;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;
            ScreenCapture.CaptureScreenshot(Output+"/input-last.png");Debug.Log("[SupplyLineInput] result="+(pass?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(pass);
        }
    }
}
