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
using TMPro;

namespace Game.Editor
{
    /// <summary>Public UI-entry Power Relay probe. Mission orders are issued only through Watch ARIA after a real touch confirmation.</summary>
    [InitializeOnLoad]
    public static class CH02M04PowerRelayInputProbe
    {
        private const string Active="Warline.PowerRelay.InputProbe",Manual="Warline.PowerRelay.InputProbe.ManualEditorRun",Output="/private/tmp/warline-power-relay";
        private static bool seeded,finished,watchStarted,won,sawDebrief,sawLinaPortrait,sawLocalizedLina,sawMissionMatch,loggedBackdrop;private static double started,lastInput,lastLog,briefWait,matchWait;private static int winningClock;private static AriaTouchInputUiSystemHelper touch;private static CampaignMissionProgressStore store;
        static CH02M04PowerRelayInputProbe(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Tick;}
        [MenuItem("Game/Campaign/Power Relay/Run Normal Input Watch Probe")]
        public static void RunFromOpenEditor(){SessionState.SetBool(Manual,true);Run();}
        public static void Run()
        {
            try{CH02M04PowerRelayPresentationBuilder.BuildCheckpoint();}catch(Exception e){Debug.LogException(e);Complete(false,"Authoring failed: "+e.Message);return;}
            Directory.CreateDirectory(Output);seeded=finished=watchStarted=won=sawDebrief=sawLinaPortrait=sawLocalizedLina=sawMissionMatch=loggedBackdrop=false;briefWait=matchWait=0;winningClock=0;store=null;SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled=false;SessionState.SetBool(Active,true);started=EditorApplication.timeSinceStartup;MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);RepairSelectableRegistry();AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished||!EditorApplication.isPlaying)return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>600)throw new TimeoutException("Power Relay public-input Watch probe timed out.");World world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;EntityManager em=world.EntityManager;using EntityQuery roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;Entity root=roots.GetSingletonEntity();if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;var reference=em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root);
                if(store==null){store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"input-profile-"+Guid.NewGuid().ToString("N")))));store.EnsureAvailable(CampaignMissionSequence.PowerRelay);}if(reference.Store!=store)reference.Store=store;if(!seeded){Game.Configs.GameLocalization.SetLocale("fa-IR",false);seeded=true;}
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var power=em.HasComponent<CampaignMissionPowerRelayState>(root)?em.GetComponentData<CampaignMissionPowerRelayState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>8){var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);Debug.Log($"[PowerRelayInput] phase={runtime.Phase} ready={power.Ready} route={power.SafeRouteConfirmed} sheltered={power.FamiliesSheltered} fuel={power.FuelDelivered} restored={power.PowerRestored} hold={power.VictoryHoldMilliseconds} clock={power.ElapsedMilliseconds} watch={UiShellRuntimeGateway.ReadAriaPlay().Phase} guide={guidance.GuidanceId}/{guidance.Prompt}/{guidance.RecommendationKind}/can{guidance.CanExecute} {PowerMemberStatus(em,root,in power)} {PowerLifecycleStatus(em,root)}");ScreenCapture.CaptureScreenshot(Output+"/input-current.png");lastLog=EditorApplication.timeSinceStartup;}
                if(power.Failure!=PowerRelayFailure.None)throw new InvalidOperationException("Mission failed: "+power.Failure);
                if(runtime.Outcome==MissionOutcomeKind.Victory){if(power.SafeRouteConfirmed==0||power.FamiliesSheltered==0||power.FuelDelivered==0||power.PowerRestored==0||power.VictoryHoldMilliseconds<10000||power.ExposedRouteUsed!=0)throw new InvalidOperationException("Victory lacks the protected route, shelter, Fuel, repair, hold, or used the exposed route.");if(!won){won=true;winningClock=power.ElapsedMilliseconds;Debug.Log("[PowerRelayInput] victory=Passed awaiting=debrief-and-return");}}
                var watch=UiShellRuntimeGateway.ReadAriaPlay();if(watch.Active){watchStarted=true;touch?.Dispose();touch=null;return;}if(watchStarted&&!won)throw new InvalidOperationException("Watch stopped before outcome: "+watch.Phase);
                EnsureTouch();touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<1)return;NarrativeSequenceView narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);if(narrative!=null&&Visible(narrative,"rootGroup")){using EntityQuery shellStates=em.CreateEntityQuery(typeof(UiShellStateComponent));if(shellStates.CalculateEntityCount()==1&&shellStates.GetSingleton<UiShellStateComponent>().IsTransitionRunning!=0)throw new InvalidOperationException("Power Relay narrative appeared before the loading/match transition completed.");briefWait=0;if(won){sawDebrief=true;SkipNarrative(narrative);return;}if(runtime.MissionId.Equals(CampaignMissionSequence.PowerRelay)&&runtime.Phase==MissionPhaseKind.InteractiveBrief){if(!loggedBackdrop){LogBackdropDiagnostics();loggedBackdrop=true;}if(narrative.CurrentPanelSprite!=null||narrative.IsPanelVisible)throw new InvalidOperationException("Power Relay briefing covered the live match with an active narrative panel"+(narrative.CurrentPanelSprite!=null?": "+narrative.CurrentPanelSprite.name:"."));var portrait=narrative.DialogueView?.CurrentPortraitSprite;if(portrait!=null&&portrait.name=="Portrait_Lina_PowerRelay"){sawLinaPortrait=true;TMP_Text name=typeof(NarrativeDialogueView).GetField("speakerNameText",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(narrative.DialogueView) as TMP_Text;TMP_Text role=typeof(NarrativeDialogueView).GetField("speakerRoleText",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(narrative.DialogueView) as TMP_Text;string actualName=SourceText(name),actualRole=SourceText(role);if(actualName!="دکتر لینا منصور"||actualRole!="پزشک پناهگاه منطقهٔ شرقی")throw new InvalidOperationException($"Lina Persian identity mismatch: name='{actualName}', role='{actualRole}'.");sawLocalizedLina=true;}AdvanceNarrative(narrative);return;}SkipNarrative(narrative);return;}
                if(won){var result=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();if(result!=null){Tap(typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(result) as Button);return;}var returned=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();if(returned!=null&&Ready(returned.LaunchMissionButton)){if(!sawDebrief)throw new InvalidOperationException("Mandatory debrief was not presented.");if(!sawLinaPortrait)throw new InvalidOperationException("Lina's comic portrait was not presented.");if(!sawLocalizedLina)throw new InvalidOperationException("Lina's Persian name and role were not presented.");if(!sawMissionMatch)throw new InvalidOperationException("Power Relay never reached the visible match HUD.");Complete(true,$"publicEntry=Passed normalInput=Passed loadingHandoff=Passed liveMatchBackdrop=Passed matchHud=Passed linaPortrait=Passed linaPersianIdentity=Passed locale={Game.Configs.GameLocalization.CurrentLocaleCode} aria=Passed protectedRoute=Passed exposedRoute=Avoided clock={winningClock} debrief=Passed resultReturn=Passed");}return;}
                if(runtime.MissionId.Equals(CampaignMissionSequence.PowerRelay)&&runtime.Phase>=MissionPhaseKind.FindSquad){if(UiShellRuntimeGateway.TryReadMatchHudHeader(out _)){sawMissionMatch=true;matchWait=0;}else{if(matchWait==0)matchWait=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-matchWait>15)throw new InvalidOperationException("Power Relay left its briefing but the match HUD never became available.");return;}Button[] buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);Tap(buttons.FirstOrDefault(b=>b.name=="ConfirmWatchAria"&&Ready(b))??buttons.FirstOrDefault(b=>b.name=="WatchAriaPlay"&&Ready(b)));return;}
                var briefing=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();if(briefing!=null&&Ready(briefing.DeployOperationButton)){Tap(briefing.DeployOperationButton);return;}var campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();if(campaign!=null&&UiShellRuntimeGateway.TryReadCampaignOperations(out UiCampaignOperationsModel model)){if(!campaign.IsChapterTwo){Tap(Ready(campaign.ChapterTwoButton)?campaign.ChapterTwoButton:campaign.ChapterTwoOverviewButton);return;}if(model.SelectedMission.MissionId!=CampaignMissionSequence.PowerRelay){Tap(campaign.MissionNodeButtons[3]);return;}Tap(campaign.LaunchMissionButton);return;}
                if(runtime.MissionId.Equals(CampaignMissionSequence.PowerRelay)){if(runtime.Phase==MissionPhaseKind.InteractiveBrief){if(briefWait==0)briefWait=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-briefWait>45)throw new InvalidOperationException("Required Power Relay briefing did not become visible.");}return;}
                foreach(var route in UnityEngine.Object.FindObjectsByType<UIShellRouteButtonView>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))if(route.Route==UIRoute.Campaign&&Ready(route.GetComponent<Button>())){Tap(route.GetComponent<Button>());return;}
            }
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);}
        }
        private static void SkipNarrative(NarrativeSequenceView view){var confirm=view.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");object owner=confirming?(object)confirm:view.PlaybackControlsView;Tap(owner?.GetType().GetField(confirming?"confirmButton":"skipButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(owner) as Button);}
        private static void AdvanceNarrative(NarrativeSequenceView view){Tap(typeof(NarrativeDialogueView).GetField("inputButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view.DialogueView) as Button);}
        private static string SourceText(TMP_Text text)=>text is RTLTMPro.RTLTextMeshPro rtl?rtl.OriginalText:text?.text;
        private static void LogBackdropDiagnostics()
        {
            string cameras=string.Join(" | ",UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(c=>$"{c.gameObject.scene.name}/{c.name}:active={c.gameObject.activeInHierarchy},enabled={c.enabled},depth={c.depth},clear={c.clearFlags},color={c.backgroundColor}"));
            Vector2 center=new(Screen.width*.5f,Screen.height*.5f);string graphics=string.Join(" | ",UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(g=>g.enabled&&g.color.a>.01f&&RectTransformUtility.RectangleContainsScreenPoint(g.rectTransform,center,g.canvas!=null&&g.canvas.renderMode!=RenderMode.ScreenSpaceOverlay?g.canvas.worldCamera:null)).Select(g=>$"{g.gameObject.scene.name}/{PathOf(g.transform)}:{g.GetType().Name},color={g.color},sprite={(g is Image image&&image.sprite!=null?image.sprite.name:"none")}"));
            Debug.Log("[PowerRelayBackdrop] cameras="+cameras);Debug.Log("[PowerRelayBackdrop] centerGraphics="+graphics);ScreenCapture.CaptureScreenshot(Output+"/briefing-backdrop.png");
        }
        private static string PathOf(Transform value){string path=value.name;while(value.parent!=null){value=value.parent;path=value.name+"/"+path;}return path;}
        private static bool Ready(Button b)=>b!=null&&b.IsActive()&&b.IsInteractable();private static bool Visible(object owner,string field){var group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as CanvasGroup;return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static void EnsureTouch(){if(touch!=null)return;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Could not start public touch input.");}
        private static string PowerMemberStatus(EntityManager em,Entity root,in CampaignMissionPowerRelayState power)
        {
            if(!em.HasBuffer<CampaignMissionPowerRelayMember>(root))return "family=missing-buffer";
            foreach(var member in em.GetBuffer<CampaignMissionPowerRelayMember>(root,true))
            {
                if(member.Kind!=2||!em.Exists(member.Entity))continue;
                string cell=em.HasComponent<UnitGrid>(member.Entity)?em.GetComponentData<UnitGrid>(member.Entity).Cell.ToString():"none";
                string position=em.HasComponent<Unity.Transforms.LocalTransform>(member.Entity)?em.GetComponentData<Unity.Transforms.LocalTransform>(member.Entity).Position.xz.ToString():"none";
                string target=em.HasComponent<UnitTarget>(member.Entity)?em.GetComponentData<UnitTarget>(member.Entity).Cell.ToString():"none";
                string mode=UiShellRuntimeGateway.TryReadMatchHudCommandState(out var commands)?commands.ActiveCommandMode.ToString():"unavailable";
                string footprint=em.HasComponent<UnitFootprint>(member.Entity)?em.GetComponentData<UnitFootprint>(member.Entity).Size.ToString():"none";
                return $"familyCell={cell} familyPos={position} footprint={footprint} safe={power.SafeRouteCell} selected={(em.HasComponent<SelectedUnitTag>(member.Entity)?1:0)} mode={mode} target={target} manual={(em.HasComponent<ManualMoveOrderTag>(member.Entity)?1:0)} pathReq={(em.HasComponent<UnitPathRequest>(member.Entity)?1:0)} pathFollow={(em.HasComponent<UnitPathFollow>(member.Entity)?1:0)} retry={(em.HasComponent<UnitPathRetryCooldown>(member.Entity)?1:0)}";
            }
            return "family=missing";
        }
        private static string PowerLifecycleStatus(EntityManager em,Entity root)
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
            return $"facts=hostile:{facts.HostileDefeatedCount}/{facts.HostileTotalCount},civilian:{facts.CivilianLossCount}/{facts.CivilianTotalCount},secured:{facts.PowerRelaySecured},failure:{facts.PowerRelayFailure} result={result} settlements={settlements}/{settlement}";
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
            Debug.Log($"[PowerRelayInput] selectableRegistry=Rebuilt live={all.Count(x=>x.isActiveAndEnabled)} capacity={((Selectable[])arrayField.GetValue(null)).Length}");
        }
        private static void Tap(Button b){if(!Ready(b))return;var rect=(RectTransform)b.transform;var canvas=b.GetComponentInParent<Canvas>();Vector2 point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));if(touch.TryGesture(point,point,.18f,0,Time.unscaledTime)){lastInput=EditorApplication.timeSinceStartup;Debug.Log("[PowerRelayInput] touch="+b.name);}}
        private static void Complete(bool pass,string detail){if(finished)return;finished=true;SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled=false;touch?.Dispose();touch=null;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;ScreenCapture.CaptureScreenshot(Output+"/input-last.png");Debug.Log("[PowerRelayInput] result="+(pass?"Passed":"Failed")+" "+detail);if(SessionState.GetBool(Manual,false)){SessionState.SetBool(Manual,false);AssetDatabase.AllowAutoRefresh();EditorApplication.ExitPlaymode();}else MissionEditorValidationExit.Complete(pass);}
    }
}
