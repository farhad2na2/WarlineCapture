using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Campaign entry and full extraction through visible touch controls; no mission fact or command injection.</summary>
    [InitializeOnLoad]
    public static class M04AirliftAirfieldInputProbe
    {
        private const string Active="Warline.AirliftAirfield.Input",LocaleKey="Warline.AirliftAirfield.Locale",WatchKey="Warline.AirliftAirfield.Watch";
        private static string Output=>"/private/tmp/warline-airlift-markers/"+SessionState.GetString(LocaleKey,"en")+(SessionState.GetBool(WatchKey,true)?"-watch":"-manual");
        private static double started,lastInput,lastLog,stoppedAt;
        private static bool finished,won,watchStarted,sawDebrief,sourceChecked,resultCaptured;
        private static double resultCaptureAt;
        private static int lastLesson=-1;
        private static AriaTouchInputUiSystemHelper touch;
        private static CampaignMissionProgressStore store;
        private static InputSettings originalSettings,fixtureSettings;
        private static readonly List<InputDevice> isolated=new();
        private static readonly HashSet<string> panels=new(StringComparer.Ordinal);
        private static readonly HashSet<int> targetsChecked=new();
        static M04AirliftAirfieldInputProbe(){if(SessionState.GetBool(Active,false)){EditorApplication.update+=Tick;SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled=true;AriaTouchInputUiSystemHelper.Evidence+=RecordTouch;}}
        public static void RunWatchEnglish()=>Run("en",true);
        public static void RunWatchPersian()=>Run("fa-IR",true);
        public static void RunManualEnglish()=>Run("en",false);
        public static void RunManualPersian()=>Run("fa-IR",false);
        private static void Run(string locale,bool watch)
        {
            M04AirliftConfigBuilder.Build();SessionState.SetString(LocaleKey,locale);SessionState.SetBool(WatchKey,watch);
            Directory.CreateDirectory(Output);finished=won=watchStarted=sawDebrief=sourceChecked=resultCaptured=false;resultCaptureAt=0;store=null;panels.Clear();targetsChecked.Clear();lastLesson=-1;stoppedAt=lastInput=lastLog=0;
            SessionState.SetBool(Active,true);started=EditorApplication.timeSinceStartup;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
            SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled=true;
            AriaTouchInputUiSystemHelper.Evidence-=RecordTouch;
            AriaTouchInputUiSystemHelper.Evidence+=RecordTouch;
        }
        private static void RecordTouch(AriaTouchInputUiSystemHelper source,string kind,Vector2 position)
        {
            if(kind!="Ended"||Camera.main==null||!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))return;
            var camera=Camera.main;var ray=camera.ScreenPointToRay(position);var ground=new Plane(Vector3.up,new Vector3(0,.03f,0));
            string point=ground.Raycast(ray,out var distance)?ray.GetPoint(distance).ToString():"NoGround";
            Debug.Log($"[AirliftTouch] release={position} camera={camera.name} cameraPosition={camera.transform.position} destination={target.Destination} destinationScreen={camera.WorldToScreenPoint(target.Destination)} ground={point}");
            if(UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel) && panel.TutorialStep is 3 or 6 or 11 &&
                Vector2.Distance(position,camera.WorldToScreenPoint(target.Destination))<30)
                ScreenCapture.CaptureScreenshot(Output+"/destination-"+panel.TutorialStep+".png");
        }
        private static void Tick()
        {
            if(finished||!EditorApplication.isPlaying)return;
            try
            {
                ConfigureFixture();
                if(EditorApplication.timeSinceStartup-started>1000)throw new TimeoutException("Airlift public-input journey timed out.");
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;
                var root=roots.GetSingletonEntity();if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                if(store==null){store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"profile-"+Guid.NewGuid().ToString("N")))));store.EnsureAvailable(M04AirliftConfigBuilder.MissionId);GameLocalization.SetLocale(SessionState.GetString(LocaleKey,"en"),false);}
                em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                var extraction=em.HasComponent<CampaignMissionExtractionState>(root)?em.GetComponentData<CampaignMissionExtractionState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>8){Debug.Log($"[AirliftInput] phase={runtime.Phase} clock={facts.ElapsedMilliseconds} ready={extraction.Ready} passengers={facts.ExtractionPassengerTotal} aboard={facts.ExtractionPassengersAboard} carrierLeg={facts.ExtractionCarrierLegCount} delivered={facts.ExtractionPassengersDelivered} departed={facts.ExtractionDeparted} hold={facts.ExtractionSecureMilliseconds} lost={facts.CivilianLossCount} aria={UiShellRuntimeGateway.ReadAriaPlay().Phase}");RecordCarrier(em,extraction.Carrier);ScreenCapture.CaptureScreenshot(Output+"/current.png");lastLog=EditorApplication.timeSinceStartup;}
                if(runtime.Phase==MissionPhaseKind.Engage && facts.ElapsedMilliseconds>5000)ValidateGroundMarkerPresentation();
                if(runtime.Outcome==MissionOutcomeKind.Defeat)throw new InvalidOperationException("Airlift settled defeat.");
                if(runtime.Outcome==MissionOutcomeKind.Victory&&!won){if(facts.ExtractionPassengersDelivered!=4||facts.ExtractionCarrierLegCount!=4||facts.ExtractionDeparted==0||facts.CivilianLossCount!=0)throw new InvalidOperationException("Victory lacks complete two-leg passenger accounting.");won=true;Debug.Log("[AirliftInput] victory=Passed passengers=4 carrierLeg=4 aircraftDeparture=Passed");}
                // ARIA owns touches while Watch is active, but its comms comic must
                // still be observed before returning from this evidence tick.
                var visibleComic=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
                if(runtime.MissionId.Equals(M04AirliftConfigBuilder.MissionId)&&visibleComic!=null&&Visible(visibleComic,"rootGroup"))
                {
                    var sprite=visibleComic.CurrentPanelSprite;
                    if(sprite!=null&&visibleComic.IsPanelVisible&&sprite.name.StartsWith("M04-",StringComparison.Ordinal))
                    {if(panels.Add(sprite.name))ScreenCapture.CaptureScreenshot(Output+"/comic-"+sprite.name+".png");if(won)sawDebrief=true;}
                }
                if(extraction.Ready!=0&&!sourceChecked)ValidateSource(em);
                if(UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var action)&&action.TutorialStep is 3 or 6 &&
                    !targetsChecked.Contains(action.TutorialStep)&&UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var liveTarget))
                {
                    var expected=action.TutorialStep==3?M04AirliftConfigBuilder.PickupCell:M04AirliftConfigBuilder.TransferCell;
                    if(Mathf.Abs(liveTarget.Destination.x-expected.x)>.1f||Mathf.Abs(liveTarget.Destination.z-expected.y)>.1f)throw new InvalidOperationException("Tutorial ignored authored Airlift boarding anchor.");
                    targetsChecked.Add(action.TutorialStep);Debug.Log($"[AirliftInput] routeTarget=Passed lesson={action.TutorialStep} destination={liveTarget.Destination}");
                }
                if(SessionState.GetBool(WatchKey,true)&&UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var lesson)&&lesson.TutorialStep!=lastLesson)
                {lastLesson=lesson.TutorialStep;ScreenCapture.CaptureScreenshot(Output+"/lesson-"+lastLesson+".png");}
                var watch=UiShellRuntimeGateway.ReadAriaPlay();if(watch.Active){watchStarted=true;stoppedAt=0;touch?.Dispose();touch=null;return;}
                if(watchStarted&&!won){if(stoppedAt==0)stoppedAt=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-stoppedAt>8)throw new InvalidOperationException("ARIA stopped before extraction outcome: "+watch.Phase);return;}
                EnsureTouch();touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<1)return;
                var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
                if(narrative!=null&&Visible(narrative,"rootGroup"))
                {
                    using var shells=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));if(shells.CalculateEntityCount()==1&&shells.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>().IsTransitionRunning!=0)return;
                    if(runtime.MissionId.Equals(M04AirliftConfigBuilder.MissionId))
                    {var panel=narrative.CurrentPanelSprite;if(panel!=null&&narrative.IsPanelVisible&&panel.name.StartsWith("M04-",StringComparison.Ordinal)){if(panels.Add(panel.name))ScreenCapture.CaptureScreenshot(Output+"/comic-"+panel.name+".png");if(won)sawDebrief=true;Tap(Field<Button>(narrative.DialogueView,"inputButton"));return;}}
                    var confirm=narrative.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");Tap(Field<Button>(confirming?(object)confirm:narrative.PlaybackControlsView,confirming?"confirmButton":"skipButton"));return;
                }
                if(won)
                {
                    var result=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();if(result!=null&&Ready(Field<Button>(result,"primaryButton"))){if(!resultCaptured){ScreenCapture.CaptureScreenshot(Output+"/result.png");resultCaptured=true;resultCaptureAt=EditorApplication.timeSinceStartup;return;}if(EditorApplication.timeSinceStartup-resultCaptureAt>.5)Tap(Field<Button>(result,"primaryButton"));return;}
                    var returned=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();if(returned!=null&&Ready(returned.LaunchMissionButton)){if(!sawDebrief||!HasRequiredComics()||!sourceChecked||targetsChecked.Count!=2)throw new InvalidOperationException($"Missing debrief/source/comics/route: panels={panels.Count} targets={targetsChecked.Count}");Complete(true,$"publicEntry=Passed sourceHash={M04AirliftConfigBuilder.PreparedHash} controls={(SessionState.GetBool(WatchKey,true)?"ARIA":"Manual")} passengerAccounting=4/4 comics=6/6 comms=InactiveByExistingPolicy routeTargets=2/2 debrief=Passed resultReturn=Passed");}return;
                }
                if(runtime.MissionId.Equals(M04AirliftConfigBuilder.MissionId)&&runtime.Phase>=MissionPhaseKind.FindSquad)
                {
                    if(!UiShellRuntimeGateway.TryReadMatchHudHeader(out _))return;
                    if(extraction.Ready!=0&&!sourceChecked)ValidateSource(em);
                    if(SessionState.GetBool(WatchKey,true)){var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);Tap(buttons.FirstOrDefault(b=>b.name=="ConfirmWatchAria"&&Ready(b))??buttons.FirstOrDefault(b=>b.name=="WatchAriaPlay"&&Ready(b)));}
                    else DriveManual();
                    return;
                }
                var brief=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();if(brief!=null&&Ready(brief.DeployOperationButton)){Tap(brief.DeployOperationButton);return;}
                var campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();if(campaign!=null&&UiShellRuntimeGateway.TryReadCampaignOperations(out var model)){if(campaign.IsChapterTwo){Tap(campaign.ChapterOneButton);return;}if(model.SelectedMission.MissionId!=M04AirliftConfigBuilder.MissionId){Tap(campaign.MissionNodeButtons[3]);return;}Tap(campaign.LaunchMissionButton);return;}
                var home=UnityEngine.Object.FindAnyObjectByType<MainMenuCampaignCardView>();Tap(Field<Button>(home,"continueButton"));
            }
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);}
        }
        private static readonly HashSet<string> markerStates=new();
        private static string pendingMarkerCapture;
        private static double markerCaptureDue;
        private static void ValidateGroundMarkerPresentation()
        {
            if(!UiShellRuntimeGateway.TryReadMissionExtraction(out var model))return;
            var markers=UnityEngine.Object.FindObjectsByType<TacticalGroundMarkerView>(FindObjectsSortMode.None);
            if(markers.Length==0)return; // Cinematic visibility owns creation/removal.
            foreach(var marker in markers)
            {
                if(marker.GetComponentInChildren<GraphicRaycaster>(true)!=null || marker.GetComponentInChildren<Button>(true)!=null)
                    throw new InvalidOperationException("World marker intercepted gameplay input.");
                foreach(var line in marker.GetComponentsInChildren<LineRenderer>(true))
                    if(line.loop || line.positionCount!=2)throw new InvalidOperationException("Continuous ring survived marker replacement.");
                foreach(var label in marker.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    if(label.font==null || label.raycastTarget)throw new InvalidOperationException("Marker font or input contract failed.");
            }
            var landing=Array.Find(markers,m=>m.name=="Landing perimeter");
            if(landing!=null)
            {
                foreach(var line in landing.GetComponentsInChildren<LineRenderer>(true))
                {
                    Vector3 midpoint=(line.GetPosition(0)+line.GetPosition(1))*.5f;
                    if(Mathf.Abs(Vector2.Distance(new Vector2(midpoint.x,midpoint.z),new Vector2(model.LandingCenter.x,model.LandingCenter.z))-model.LandingRadius)>.05f)
                        throw new InvalidOperationException("Perimeter ticks misrepresented the gameplay radius.");
                }
            }
            string state=model.Contested?"contested":model.Cleared?"clear":"securing";
            if(markerStates.Add(state)){Debug.Log("[AirliftMarkers] result=Passed state="+state+" sparsePerimeter=Passed radius=Passed labels=Localized input=NonInteractive");pendingMarkerCapture=state;markerCaptureDue=EditorApplication.timeSinceStartup+.25;}
            if(pendingMarkerCapture!=null && EditorApplication.timeSinceStartup>=markerCaptureDue)
            {ScreenCapture.CaptureScreenshot(Output+"/marker-"+pendingMarkerCapture+".png");pendingMarkerCapture=null;}
        }
        private static void RecordCarrier(EntityManager em,Entity carrier)
        {
            if(!em.Exists(carrier)||!em.HasComponent<LocalTransform>(carrier))return;
            string row="[AirliftCarrier] position="+em.GetComponentData<LocalTransform>(carrier).Position;
            if(em.HasComponent<UnitTarget>(carrier))row+=" target="+em.GetComponentData<UnitTarget>(carrier).Cell;
            if(em.HasComponent<UnitPathRequest>(carrier))row+=" request="+em.GetComponentData<UnitPathRequest>(carrier).Goal;
            if(em.HasComponent<UnitLongDistanceMove>(carrier))row+=" final="+em.GetComponentData<UnitLongDistanceMove>(carrier).FinalGoal;
            if(em.HasComponent<UnitPathFollow>(carrier))row+=" pathIndex="+em.GetComponentData<UnitPathFollow>(carrier).PathIndex;
            if(em.HasComponent<UnitPathRange>(carrier)){var range=em.GetComponentData<UnitPathRange>(carrier);row+=" pathStart="+range.Start+" pathLength="+range.Length;}
            Debug.Log(row);
        }
        private static void ValidateSource(EntityManager em)
        {
            using var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));if(maps.CalculateEntityCount()!=1)throw new InvalidOperationException("Expected one active map metadata owner.");var metadata=maps.GetSingleton<OperationMapMetadataComponent>();ref var map=ref metadata.Blob.Value;
            if(!map.OperationMapId.Equals(M04AirliftConfigBuilder.MapId)||!map.SourceContentHash.Equals(M04AirliftConfigBuilder.PreparedHash))throw new InvalidOperationException("Airlift loaded the wrong logical or physical source.");
            using var sockets=em.CreateEntityQuery(typeof(OperationMapMissionPresentationSocket));using var hidden=em.CreateEntityQuery(typeof(OperationMapMissionPresentationHiddenTag));
            if(sockets.CalculateEntityCount()!=1||hidden.CalculateEntityCount()==0)throw new InvalidOperationException("The operational pad's decorative aircraft was not retired.");
            sourceChecked=true;Debug.Log($"[AirliftInput] source=Passed logical={map.OperationMapId} physical={map.SourceOperationMapId} hash={map.SourceContentHash} socketOwners=1 retiredRenderers={hidden.CalculateEntityCount()}");
        }
        private static bool HasRequiredComics()=>new[]{"M04-B01-16x9","M04-B02-16x9","M04-B03-16x9","M04-D01-16x9","M04-D02-16x9","M04-D03-16x9"}.All(panels.Contains);
        private static void DriveManual()
        {
            var aria=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();if(aria==null||!aria.IsPresentationVisible||!UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel))return;
            if(panel.TutorialStep!=lastLesson){lastLesson=panel.TutorialStep;ScreenCapture.CaptureScreenshot(Output+"/lesson-"+lastLesson+".png");Debug.Log("[AirliftInput] manualLesson="+lastLesson);}
            if(lastLesson==1){Tap(aria.ContinueButton);return;}
            if(Ready(aria.ShowMeButton)){Tap(aria.ShowMeButton);return;}
            if(lastLesson is 10 or 12)return;
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target)||target.Moving)return;
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();if(controls==null||!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state))return;
            var camera=Camera.main;if(camera==null)return;
            if(target.NeedsSelection)
            {
                if(state.ActiveCommandMode!=TacticalCommandMode.Select){Tap(controls.SelectButton);return;}
                if(target.DragSelection)
                {
                    var min=new Vector2(float.MaxValue,float.MaxValue);var max=new Vector2(float.MinValue,float.MinValue);
                    for(int i=0;i<8;i++)
                    {
                        var world=new Vector3((i&1)==0?target.SelectionMin.x:target.SelectionMax.x,(i&2)==0?target.SelectionMin.y:target.SelectionMax.y,(i&4)==0?target.SelectionMin.z:target.SelectionMax.z);
                        var screen=camera.WorldToScreenPoint(world);if(screen.z<=0)return;min=Vector2.Min(min,screen);max=Vector2.Max(max,screen);
                    }
                    min-=Vector2.one*8;max+=Vector2.one*8;
                    if(!WorldPointUsable(min)||!WorldPointUsable(max)){Tap(aria.ShowMeButton);return;}
                    if(touch.TryGesture(min,max,.5f,.9f,Time.unscaledTime)){lastInput=EditorApplication.timeSinceStartup;Debug.Log("[AirliftInput] touch=world-selection-drag");}
                }
                else
                {
                    var screen=camera.WorldToScreenPoint(target.Selection+Vector3.up);if(screen.z<=0||!WorldPointUsable(screen)){Tap(aria.ShowMeButton);return;}
                    Gesture(screen,"world-selection");
                }
                return;
            }
            if(lastLesson is 2 or 4 or 8)return;
            if(lastLesson==7){foreach(var selection in UnityEngine.Object.FindObjectsByType<MatchHudSelectionPanelView>(FindObjectsSortMode.None)){var button=typeof(MatchHudSelectionPanelView).GetMethod("ResolvePassengerTutorialButton",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(selection,null) as Button;if(Ready(button)){Tap(button);return;}}return;}
            if(lastLesson is 5 or 9){if(state.ActiveCommandMode!=TacticalCommandMode.Board){Tap(controls.CommandWheelPanel.NextBoardButton);return;}}
            else if(state.ActiveCommandMode!=TacticalCommandMode.Move){Tap(controls.MoveButton);return;}
            Vector3 p=camera.WorldToScreenPoint(target.Destination+(lastLesson is 5 or 9?Vector3.up:Vector3.zero));if(p.z<=0||!WorldPointUsable(p)){Tap(aria.ShowMeButton);return;}
            Gesture(new Vector2(p.x,p.y),"world-target");
        }
        private static bool WorldPointUsable(Vector2 p)
        {
            var events=UnityEngine.EventSystems.EventSystem.current;if(events==null||!Screen.safeArea.Contains(p))return false;
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events){position=p},hits);
            return hits.Count==0;
        }
        private static void ConfigureFixture()
        {
            Application.runInBackground=true;if(!AriaTouchInputUiSystemHelper.AllowBackgroundValidation||fixtureSettings!=null)return;
            originalSettings=InputSystem.settings;fixtureSettings=UnityEngine.Object.Instantiate(originalSettings);fixtureSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;fixtureSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=fixtureSettings;
            foreach(var device in InputSystem.devices)if(device.native&&device.enabled&&(device is UnityEngine.InputSystem.Pointer||device is Keyboard)){InputSystem.ResetDevice(device);InputSystem.DisableDevice(device);isolated.Add(device);}
            Debug.Log("[AirliftInput] inputFixture=background-game-view-only nativeInputIsolated="+isolated.Count);
        }
        private static void EnsureTouch(){if(touch!=null)return;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Public touch input unavailable.");}
        private static T Field<T>(object owner,string name) where T:class=>owner?.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as T;
        private static bool Visible(object owner,string field){var group=Field<CanvasGroup>(owner,field);return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static bool Ready(Button b)=>b!=null&&b.IsActive()&&b.IsInteractable();
        private static void Tap(Button b){if(!Ready(b))return;var rect=(RectTransform)b.transform;var canvas=b.GetComponentInParent<Canvas>();var wedge=b.targetGraphic as V3RadialWedgeGraphic;var point=wedge!=null?wedge.GuidanceTouchPoint:rect.TransformPoint(rect.rect.center);var p=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,point);Gesture(p,b.name);}
        private static void Gesture(Vector2 p,string label){if(touch.TryGesture(p,p,.18f,0,Time.unscaledTime)){lastInput=EditorApplication.timeSinceStartup;Debug.Log("[AirliftInput] touch="+label);}}
        private static void Complete(bool passed,string detail)
        {
            if(finished)return;finished=true;touch?.Dispose();touch=null;foreach(var device in isolated)if(device.added)InputSystem.EnableDevice(device);isolated.Clear();if(fixtureSettings!=null){InputSystem.settings=originalSettings;UnityEngine.Object.Destroy(fixtureSettings);fixtureSettings=null;}
            AriaTouchInputUiSystemHelper.Evidence-=RecordTouch;
            SelectionRuntimeDiagnosticsSystemHelper.EditorMoveCommandTraceEnabled=false;
            SessionState.SetBool(Active,false);EditorApplication.update-=Tick;ScreenCapture.CaptureScreenshot(Output+"/last.png");Debug.Log("[AirliftInput] result="+(passed?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(passed);
        }
    }
}
