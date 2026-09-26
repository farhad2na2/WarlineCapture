using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
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
    /// <summary>Public UI-entry Route Reopened probe. Mission orders are issued only through Watch ARIA after a real touch confirmation.</summary>
    [InitializeOnLoad]
    public static class CH02M05RouteReopenedInputProbe
    {
        private const string Active="Warline.RouteReopened.InputProbe",Manual="Warline.RouteReopened.InputProbe.ManualEditorRun",Output="/private/tmp/warline-route-reopened";
        private static bool seeded,finished,watchStarted,won,sawDebrief;private static double started,lastInput,lastLog,briefWait;private static int winningClock;private static AriaTouchInputUiSystemHelper touch;private static CampaignMissionProgressStore store;
        static CH02M05RouteReopenedInputProbe(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Tick;}
        [MenuItem("Game/Campaign/Route Reopened/Run Normal Input Watch Probe")]
        public static void RunFromOpenEditor(){SessionState.SetBool(Manual,true);Run();}
        public static void Run()
        {
            try{CH02M05RouteReopenedRulesValidation.Run();CH02M05RouteReopenedConfigBuilder.Build();CH02M05RouteReopenedPresentationBuilder.Build();CH02M05RouteReopenedNarrativeBuilder.BuildCaptionedArtWithoutVoices();}catch(Exception e){Debug.LogException(e);Complete(false,"Authoring failed: "+e.Message);return;}
            Directory.CreateDirectory(Output);seeded=finished=watchStarted=won=sawDebrief=false;winningClock=0;store=null;SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled=false;SessionState.SetBool(Active,true);started=EditorApplication.timeSinceStartup;MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);RepairSelectableRegistry();AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished||!EditorApplication.isPlaying)return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>600)throw new TimeoutException("Route Reopened public-input Watch probe timed out.");World world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;EntityManager em=world.EntityManager;using EntityQuery roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;Entity root=roots.GetSingletonEntity();if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;var reference=em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root);
                if(store==null){store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"input-profile-"+Guid.NewGuid().ToString("N")))));store.EnsureAvailable(CampaignMissionSequence.RouteReopened);}if(reference.Store!=store)reference.Store=store;if(!seeded){Game.Configs.GameLocalization.SetLocale("en",false);seeded=true;}
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var routeState=em.HasComponent<CampaignMissionRouteReopenedState>(root)?em.GetComponentData<CampaignMissionRouteReopenedState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>8){var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);Debug.Log($"[RouteReopenedInput] phase={runtime.Phase} ready={routeState.Ready} relief={routeState.ReliefDelivered} fuel={routeState.FuelDelivered} link={routeState.LinkRestored} hub={routeState.HubEntered} garrison={routeState.GarrisonCleared} records={routeState.RecordsPreserved}/{routeState.RecordsHoldMilliseconds} clock={routeState.ElapsedMilliseconds} watch={UiShellRuntimeGateway.ReadAriaPlay().Phase} guide={guidance.GuidanceId}/{guidance.Prompt}/{guidance.RecommendationKind}/can{guidance.CanExecute} {RouteMemberStatus(em,root,in routeState)} {RouteLifecycleStatus(em,root)}");ScreenCapture.CaptureScreenshot(Output+"/input-current.png");lastLog=EditorApplication.timeSinceStartup;}
                if(routeState.Failure!=RouteReopenedFailure.None)throw new InvalidOperationException("Mission failed: "+routeState.Failure);
                if(runtime.Outcome==MissionOutcomeKind.Victory){if(routeState.ReliefDelivered==0||routeState.FuelDelivered==0||routeState.LinkRestored==0||routeState.HubEntered==0||routeState.GarrisonCleared==0||routeState.RecordsPreserved==0||routeState.RecordsHoldMilliseconds<10000)throw new InvalidOperationException("Victory lacks a lifeline, link repair, controlled hub capture, or preserved archive.");if(!won){won=true;winningClock=routeState.ElapsedMilliseconds;Debug.Log("[RouteReopenedInput] victory=Passed awaiting=debrief-and-return");}}
                var watch=UiShellRuntimeGateway.ReadAriaPlay();if(watch.Active){watchStarted=true;touch?.Dispose();touch=null;return;}if(watchStarted&&!won)throw new InvalidOperationException("Watch stopped before outcome: "+watch.Phase);
                EnsureTouch();touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<1)return;NarrativeSequenceView narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);if(narrative!=null&&Visible(narrative,"rootGroup")){briefWait=0;if(won)sawDebrief=true;SkipNarrative(narrative);return;}
                if(won){var result=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();if(result!=null){Tap(typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(result) as Button);return;}var returned=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();if(returned!=null&&Ready(returned.LaunchMissionButton)){if(!sawDebrief)throw new InvalidOperationException("Mandatory debrief was not presented.");Complete(true,$"publicEntry=Passed normalInput=Passed aria=Passed protectedRoute=Passed exposedRoute=Avoided clock={winningClock} debrief=Passed resultReturn=Passed");}return;}
                if(runtime.MissionId.Equals(CampaignMissionSequence.RouteReopened)&&runtime.Phase>=MissionPhaseKind.FindSquad){Button[] buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);Tap(buttons.FirstOrDefault(b=>b.name=="ConfirmWatchAria"&&Ready(b))??buttons.FirstOrDefault(b=>b.name=="WatchAriaPlay"&&Ready(b)));return;}
                var briefing=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();if(briefing!=null&&Ready(briefing.DeployOperationButton)){Tap(briefing.DeployOperationButton);return;}var campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();if(campaign!=null&&UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel model)){if(!campaign.IsChapterTwo){Tap(Ready(campaign.ChapterTwoButton)?campaign.ChapterTwoButton:campaign.ChapterTwoOverviewButton);return;}if(model.SelectedMission.MissionId!=CampaignMissionSequence.RouteReopened){Tap(campaign.MissionNodeButtons[4]);return;}Tap(campaign.LaunchMissionButton);return;}
                if(runtime.MissionId.Equals(CampaignMissionSequence.RouteReopened)){if(runtime.Phase==MissionPhaseKind.InteractiveBrief){if(briefWait==0)briefWait=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-briefWait>45)throw new InvalidOperationException("Required Route Reopened briefing did not become visible.");}return;}
                foreach(var route in UnityEngine.Object.FindObjectsByType<UIShellRouteButtonView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))if(route.Route==UIRoute.Campaign&&Ready(route.GetComponent<Button>())){Tap(route.GetComponent<Button>());return;}
            }
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);}
        }
        private static void SkipNarrative(NarrativeSequenceView view){var confirm=view.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");object owner=confirming?(object)confirm:view.PlaybackControlsView;Tap(owner?.GetType().GetField(confirming?"confirmButton":"skipButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(owner) as Button);}
        private static bool Ready(Button b)=>b!=null&&b.IsActive()&&b.IsInteractable();private static bool Visible(object owner,string field){var group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as CanvasGroup;return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static void EnsureTouch(){if(touch!=null)return;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Could not start public touch input.");}
        private static string RouteMemberStatus(EntityManager em,Entity root,in CampaignMissionRouteReopenedState routeState)
        {
            if(!em.HasBuffer<CampaignMissionRouteReopenedMember>(root))return "family=missing-buffer";
            string engineerStatus="";
            foreach(var member in em.GetBuffer<CampaignMissionRouteReopenedMember>(root,true))
            {
                if(member.Kind!=1||!em.Exists(member.Entity))continue;
                string engineerCell=em.HasComponent<UnitGrid>(member.Entity)?em.GetComponentData<UnitGrid>(member.Entity).Cell.ToString():"none";
                string engineerPosition=em.HasComponent<Unity.Transforms.LocalTransform>(member.Entity)?em.GetComponentData<Unity.Transforms.LocalTransform>(member.Entity).Position.xz.ToString():"none";
                engineerStatus+=$" engineer={engineerCell}/{engineerPosition}/moving{(em.HasComponent<UnitPathRequest>(member.Entity)||em.HasComponent<UnitPathFollow>(member.Entity)?1:0)}";
            }
            foreach(var member in em.GetBuffer<CampaignMissionRouteReopenedMember>(root,true))
            {
                if(member.Kind!=2||!em.Exists(member.Entity))continue;
                string cell=em.HasComponent<UnitGrid>(member.Entity)?em.GetComponentData<UnitGrid>(member.Entity).Cell.ToString():"none";
                string position=em.HasComponent<Unity.Transforms.LocalTransform>(member.Entity)?em.GetComponentData<Unity.Transforms.LocalTransform>(member.Entity).Position.xz.ToString():"none";
                string target=em.HasComponent<UnitTarget>(member.Entity)?em.GetComponentData<UnitTarget>(member.Entity).Cell.ToString():"none";
                string mode=UiShellRuntimeGateway.TryReadMatchHudCommandState(out var commands)?commands.ActiveCommandMode.ToString():"unavailable";
                string footprint=em.HasComponent<UnitFootprint>(member.Entity)?em.GetComponentData<UnitFootprint>(member.Entity).Size.ToString():"none";
                return $"{engineerStatus} reliefCell={cell} reliefPos={position} footprint={footprint} goal={routeState.ReliefGoalCell} selected={(em.HasComponent<SelectedUnitTag>(member.Entity)?1:0)} mode={mode} target={target} manual={(em.HasComponent<ManualMoveOrderTag>(member.Entity)?1:0)} pathReq={(em.HasComponent<UnitPathRequest>(member.Entity)?1:0)} pathFollow={(em.HasComponent<UnitPathFollow>(member.Entity)?1:0)} retry={(em.HasComponent<UnitPathRetryCooldown>(member.Entity)?1:0)}";
            }
            return "family=missing";
        }
        private static string RouteLifecycleStatus(EntityManager em,Entity root)
        {
            var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            string result=em.HasComponent<CampaignMissionResultComponent>(root)
                ? $"yes/v{em.GetComponentData<CampaignMissionResultComponent>(root).SourceVersion}" : "no";
            int settlements=em.HasBuffer<CampaignMissionSettlementResultElement>(root)
                ? em.GetBuffer<CampaignMissionSettlementResultElement>(root,true).Length : -1;
            string settlement="none";
            if(settlements>0)
            {
                var item=em.GetBuffer<CampaignMissionSettlementResultElement>(root,true)[settlements-1];
                settlement=$"{item.Accepted}/{item.ReasonCode}";
            }
            return $"facts=hostile:{facts.HostileDefeatedCount}/{facts.HostileTotalCount},civilian:{facts.CivilianLossCount}/{facts.CivilianTotalCount},relief:{facts.RouteReliefDelivered},fuel:{facts.RouteFuelDelivered},link:{facts.RouteLinkRestored},hub:{facts.RouteHubEntered},records:{facts.RouteRecordsPreserved},failure:{facts.RouteReopenedFailure} result={result} settlements={settlements}/{settlement}";
        }
        private static void RepairSelectableRegistry()
        {
            const BindingFlags flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic;
            var type=typeof(Selectable);var arrayField=type.GetField("s_Selectables",flags);var countField=type.GetField("s_SelectableCount",flags);
            var enabledField=type.GetField("m_EnableCalled",flags);var indexField=type.GetField("m_CurrentIndex",flags);
            if(arrayField==null||countField==null||enabledField==null||indexField==null)return;
            var all=Resources.FindObjectsOfTypeAll<Selectable>().Where(x=>x!=null&&x.gameObject.scene.IsValid()).ToArray();
            arrayField.SetValue(null,new Selectable[Mathf.NextPowerOfTwo(Mathf.Max(32,all.Length*2))]);countField.SetValue(null,0);
            foreach(var selectable in all){enabledField.SetValue(selectable,false);indexField.SetValue(selectable,-1);}
            foreach(var selectable in all.Where(x=>x.isActiveAndEnabled)){selectable.enabled=false;selectable.enabled=true;}
            Debug.Log($"[RouteReopenedInput] selectableRegistry=Rebuilt live={all.Count(x=>x.isActiveAndEnabled)} capacity={((Selectable[])arrayField.GetValue(null)).Length}");
        }
        private static void Tap(Button b){if(!Ready(b))return;var rect=(RectTransform)b.transform;var canvas=b.GetComponentInParent<Canvas>();Vector2 point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime)){lastInput=EditorApplication.timeSinceStartup;Debug.Log("[RouteReopenedInput] touch="+b.name);}}
        private static void Complete(bool pass,string detail){if(finished)return;finished=true;SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled=false;touch?.Dispose();touch=null;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;ScreenCapture.CaptureScreenshot(Output+"/input-last.png");Debug.Log("[RouteReopenedInput] result="+(pass?"Passed":"Failed")+" "+detail);if(SessionState.GetBool(Manual,false)){SessionState.SetBool(Manual,false);AssetDatabase.AllowAutoRefresh();EditorApplication.ExitPlaymode();}else MissionEditorValidationExit.Complete(pass);}
    }
}
