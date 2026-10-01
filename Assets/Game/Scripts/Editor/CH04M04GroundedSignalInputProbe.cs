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
    /// <summary>Isolated campaign save; native InputSystem touch controls. Read-only mission observation, no outcome/order injection.</summary>
    [InitializeOnLoad]
    public static class CH04M04GroundedSignalInputProbe
    {
        private const string Active="Warline.GroundedSignal.Input",LocaleKey=Active+".Locale",WatchKey=Active+".Watch",LiveKey=Active+".Live";
        private static string Output=>SessionState.GetString(Active+".Output","Design/AgentReports/CH04M04GroundedSignal/Evidence/input");
        private static double started,lastInput,lastLog,focusAt;
        private static int lastStep=-1,focusStep=-1;
        private static bool finished,won,sourceChecked,resultCaptured,sawDebrief,watchStarted;
        private static double resultCaptureAt;
        private static CampaignMissionProgressStore store;
        private static AriaTouchInputUiSystemHelper touch;
        private static InputSettings originalSettings,fixtureSettings;
        private static readonly List<InputDevice> isolated=new();
        private static readonly HashSet<string> panels=new(StringComparer.Ordinal),voices=new(StringComparer.Ordinal);
        private static readonly HashSet<string> capturedPanels=new(StringComparer.Ordinal),completedVoices=new(StringComparer.Ordinal);
        private static readonly Dictionary<string,float> voiceProgress=new(StringComparer.Ordinal);
        private static readonly Dictionary<string,double> panelFirstSeen=new(StringComparer.Ordinal);
        private const string SavedPlayOptions=Active+".SavedPlayOptions";
        private static string error;
        static CH04M04GroundedSignalInputProbe()
        {
            if(SessionState.GetBool(SavedPlayOptions,false))
            {
                EditorApplication.playModeStateChanged+=RestorePlayOptionsAfterExit;
                if(!EditorApplication.isPlayingOrWillChangePlaymode)EditorApplication.delayCall+=RestorePlayOptions;
            }
            if(!SessionState.GetBool(Active,false))return;
            // Enter Play mode reloads static fields. The isolated save, fixture and
            // coverage sets are created on the first live tick; retain the run clock.
            if(!double.TryParse(SessionState.GetString(Active+".Started",string.Empty),
                System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out started))
                started=EditorApplication.timeSinceStartup;
            lastStep=focusStep=-1;
            EditorApplication.update+=Tick;Application.logMessageReceived+=Observe;
        }
        public static void StartEnglish()=>Run("en",false,true);
        public static void StartPersian()=>Run("fa-IR",false,true);
        public static void StartEnglishWatch()=>Run("en",true,true);
        public static void StartPersianWatch()=>Run("fa-IR",true,true);
        public static void RunEnglish()=>Run("en",false,false);
        public static void RunPersian()=>Run("fa-IR",false,false);
        public static void RunEnglishWatch()=>Run("en",true,false);
        public static void RunPersianWatch()=>Run("fa-IR",true,false);
        private static void Run(string locale,bool watch,bool live)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Grounded Signal input probe requires edit mode");
            if(ExistingEditorValidation.IsRunning)
            {
                CH04M04GroundedSignalRulesValidation.Run();
                CH04M04GroundedSignalObjectiveValidation.Run();
                CH04M04GroundedSignalMapReuseValidation.Run();
            }
            SessionState.SetString(LocaleKey,locale);SessionState.SetBool(WatchKey,watch);SessionState.SetBool(LiveKey,live);
            SessionState.SetString(Active+".Output","Design/AgentReports/CH04M04GroundedSignal/Evidence/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+locale+(watch?"-aria":"-manual"));
            Directory.CreateDirectory(Output);finished=won=sourceChecked=resultCaptured=sawDebrief=watchStarted=false;lastStep=focusStep=-1;store=null;error=null;panels.Clear();voices.Clear();capturedPanels.Clear();completedVoices.Clear();voiceProgress.Clear();panelFirstSeen.Clear();lastInput=lastLog=focusAt=resultCaptureAt=0;
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");SessionState.SetBool(Active,true);started=EditorApplication.timeSinceStartup;SessionState.SetString(Active+".Started",started.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            PrepareWrapperPlayOptions();AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Observe;Application.logMessageReceived+=Observe;EditorApplication.EnterPlaymode();
        }
        private static void PrepareWrapperPlayOptions()
        {
            if(!ExistingEditorValidation.IsRunning)return;
            if(!SessionState.GetBool(SavedPlayOptions,false))
            {
                SessionState.SetBool(SavedPlayOptions+".Enabled",EditorSettings.enterPlayModeOptionsEnabled);
                SessionState.SetInt(SavedPlayOptions+".Flags",(int)EditorSettings.enterPlayModeOptions);
                SessionState.SetBool(SavedPlayOptions,true);
            }
            // Keep the wrapper's receipt/log capture alive across this probe's Play
            // transition. Restore the user's exact preference only after Edit mode.
            EditorSettings.enterPlayModeOptions|=EnterPlayModeOptions.DisableDomainReload;
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorApplication.playModeStateChanged-=RestorePlayOptionsAfterExit;
            EditorApplication.playModeStateChanged+=RestorePlayOptionsAfterExit;
        }
        private static void RestorePlayOptionsAfterExit(PlayModeStateChange state)
        {if(state==PlayModeStateChange.EnteredEditMode)RestorePlayOptions();}
        private static void RestorePlayOptions()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!SessionState.GetBool(SavedPlayOptions,false))return;
            EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)SessionState.GetInt(SavedPlayOptions+".Flags",0);
            EditorSettings.enterPlayModeOptionsEnabled=SessionState.GetBool(SavedPlayOptions+".Enabled",false);
            SessionState.SetBool(SavedPlayOptions,false);
            EditorApplication.playModeStateChanged-=RestorePlayOptionsAfterExit;
        }
        private static void Tick()
        {
            if(finished||!EditorApplication.isPlaying)return;
            try
            {
                if(started<=0)started=EditorApplication.timeSinceStartup;
                ConfigureFixture();if(error!=null)throw new InvalidOperationException(error);
                if(EditorApplication.timeSinceStartup-started>1200)throw new TimeoutException("Grounded Signal input timed out at step "+lastStep);
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;var root=roots.GetSingletonEntity();
                if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                if(store==null)
                {
                    store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"profile-"+Guid.NewGuid().ToString("N")))));
                    int target=CampaignMissionSequence.IndexOf(CampaignMissionSequence.GroundedSignal);
                    for(int i=0;i<target;i++){string prior=CampaignMissionSequence.IdAt(i);store.Settle(prior,"input-prerequisite-"+i,1,true,3,1000,CampaignMissionSequence.Next(prior));}
                    store.EnsureAvailable(CampaignMissionSequence.GroundedSignal);store.MarkChapterOpeningSeen(CampaignMissionSequence.GroundedSignal);
                    GameLocalization.SetLocale(SessionState.GetString(LocaleKey,"en"),false);
                }
                em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                var mission=em.HasComponent<CampaignMissionGroundedSignalState>(root)?em.GetComponentData<CampaignMissionGroundedSignalState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>5)
                {lastLog=EditorApplication.timeSinceStartup;Debug.Log($"[GroundedSignalInputState] phase={runtime.Phase} outcome={runtime.Outcome} step={lastStep} clock={facts.ElapsedMilliseconds} initialized={mission.Initialized} inserted={mission.Inserted} relay={mission.RelayDisabled} hardware={mission.HardwareRecovered} recovery={mission.RecoveryHoldMilliseconds} aboard={mission.SpecialistsAboardCarrier} secure={facts.ExtractionSecureMilliseconds} failure={mission.Failure}");Shot("current");}
                SampleMedia();
                if(runtime.MissionId.Equals(CampaignMissionSequence.GroundedSignal)&&runtime.Outcome==MissionOutcomeKind.Defeat)throw new InvalidOperationException("Grounded Signal defeat: "+mission.Failure);
                if(runtime.MissionId.Equals(CampaignMissionSequence.GroundedSignal)&&runtime.Outcome==MissionOutcomeKind.Victory&&!won)
                {
                    if(mission.Inserted==0||mission.RelayDisabled==0||mission.HardwareRecovered==0||mission.SpecialistsAboardCarrier!=2||facts.CivilianLossCount!=0)
                        throw new InvalidOperationException("Victory lacks observed ordinary insertion/recovery/extraction");
                    won=true;Debug.Log("[GroundedSignalOrdinaryVictory] result=Passed insertion=unload relay=normalAttack hardware=hold passengers=2 protected=alive");
                }
                var watch=UiShellRuntimeGateway.ReadAriaPlay();
                if(watch.Active){watchStarted=true;touch?.Dispose();touch=null;return;}
                if(SessionState.GetBool(WatchKey,false)&&watchStarted&&!won)throw new InvalidOperationException("ARIA stopped before victory: "+watch.Phase);
                EnsureTouch();touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<.9)return;
                var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
                if(narrative!=null&&Visible(narrative,"rootGroup"))
                {
                    using var shells=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));if(shells.CalculateEntityCount()==1&&shells.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>().IsTransitionRunning!=0)return;
                    if(narrative.CurrentPanelSprite!=null&&narrative.CurrentPanelSprite.name.StartsWith("CH04M04_",StringComparison.Ordinal))
                    {
                        if(won)sawDebrief=true;
                        // Let every installed voice reach its natural end before Next.
                        if(narrative.VoiceSource!=null&&narrative.VoiceSource.isPlaying)return;
                        if(narrative.DialogueView!=null&&narrative.DialogueView.Phase==NarrativeDialoguePhase.AdvanceReady)Tap(Field<Button>(narrative.DialogueView,"inputButton"));return;
                    }
                    var confirm=narrative.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");Tap(Field<Button>(confirming?(object)confirm:narrative.PlaybackControlsView,confirming?"confirmButton":"skipButton"));return;
                }
                if(won)
                {
                    var result=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                    if(result!=null&&Ready(Field<Button>(result,"primaryButton")))
                    {
                        if(!resultCaptured)
                        {
                            if(resultCaptureAt==0){resultCaptureAt=EditorApplication.timeSinceStartup;return;}
                            // Three earned stars settle at 1.54 seconds; retain the
                            // native result only after the reveal has completed.
                            if(EditorApplication.timeSinceStartup-resultCaptureAt<2)return;
                            Shot("result");resultCaptured=true;resultCaptureAt=EditorApplication.timeSinceStartup;return;
                        }
                        if(EditorApplication.timeSinceStartup-resultCaptureAt>.75)Tap(Field<Button>(result,"primaryButton"));return;
                    }
                    var returned=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                    if(returned!=null&&Ready(returned.LaunchMissionButton))
                    {
                        string language=SessionState.GetString(LocaleKey,"en")=="fa-IR"?"fa":"en";
                        foreach(var line in CH04M04GroundedSignalCopy.Brief.Concat(CH04M04GroundedSignalCopy.Comms).Concat(CH04M04GroundedSignalCopy.Debrief))
                            if(!completedVoices.Contains(CH04M04GroundedSignalNarrativeBuilder.VoiceRoot+"/"+language+"/"+line.Id+".wav"))throw new InvalidOperationException("Missing complete natural voice playback: "+line.Id);
                        if(panels.Count!=8||!sawDebrief||!sourceChecked||!resultCaptured)throw new InvalidOperationException("Missing comic/source/result evidence panels="+panels.Count);
                        if(!store.ReadAll().Single(x=>x.missionId==CampaignMissionSequence.GroundedSignal).firstClearRewardSettled)throw new InvalidOperationException("Ordinary settlement missing");
                        Shot("return");Complete(true,"normalInput=Passed dialogue=8 voices=8 result=Passed settlement=Passed return=Passed route=RunwayUnloadAPC humanDeviceAcceptance=pending");
                    }return;
                }
                if(runtime.MissionId.Equals(CampaignMissionSequence.GroundedSignal)&&runtime.Phase==MissionPhaseKind.Engage)
                {
                    if(!sourceChecked)
                    {using var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));if(maps.CalculateEntityCount()!=1)return;var map=maps.GetSingleton<OperationMapMetadataComponent>();if(!map.Blob.IsCreated||map.PhysicalSourceValidated==0||!map.Blob.Value.OperationMapId.Equals(CH04M04GroundedSignalConfigBuilder.MapId))throw new InvalidOperationException("Incorrect Grounded Signal physical map");sourceChecked=true;Shot("hud");}
                    if(SessionState.GetBool(WatchKey,false)){var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);Tap(buttons.FirstOrDefault(x=>x.name=="ConfirmWatchAria"&&Ready(x))??buttons.FirstOrDefault(x=>x.name=="WatchAriaPlay"&&Ready(x)));}
                    else DriveManual(em,root);
                    return;
                }
                var brief=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();if(brief!=null&&Ready(brief.DeployOperationButton)){Shot("briefing");Tap(brief.DeployOperationButton);return;}
                var campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                if(campaign!=null&&UiShellRuntimeGateway.TryReadCampaignOperations(out var model))
                {
                    if(model.SelectedMission.MissionId!=CampaignMissionSequence.GroundedSignal)
                    {if(!campaign.IsChapterFour){Tap(campaign.ChapterFourButton);return;}if(campaign.MissionNodeButtons.Length>3)Tap(campaign.MissionNodeButtons[3]);return;}
                    Shot("campaign");Tap(campaign.LaunchMissionButton);return;
                }
                var home=UnityEngine.Object.FindAnyObjectByType<MainMenuCampaignCardView>();Tap(Field<Button>(home,"continueButton"));
            }
            catch(Exception exception){Debug.LogException(exception);Complete(false,exception.Message);}
        }
        private static void DriveManual(EntityManager em,Entity root)
        {
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))return;
            var guide=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();if(guide==null||!guide.IsPresentationVisible)return;
            int step=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).GuidanceId-66000;
            if(step!=lastStep){lastStep=step;focusStep=-1;Shot("hud-step-"+step);}
            if(Ready(guide.ContinueButton)){Tap(guide.ContinueButton);return;}
            if(target.Moving||target.ExecutingAttack||target.BattleAction==UiTutorialBattleAction.Watch)return;
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();var camera=Camera.main;
            if(controls==null||camera==null||!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state))return;
            var mode=target.NeedsSelection?TacticalCommandMode.Select:step==2&&target.BattleAction==UiTutorialBattleAction.Attack?TacticalCommandMode.Attack:step==4?TacticalCommandMode.Board:TacticalCommandMode.Move;
            if(step==1&&!target.NeedsSelection)
            {foreach(var panel in UnityEngine.Object.FindObjectsByType<MatchHudSelectionPanelView>(FindObjectsSortMode.None)){var b=typeof(MatchHudSelectionPanelView).GetMethod("ResolvePassengerTutorialButton",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(panel,null) as Button;if(Ready(b)){Tap(b);return;}}return;}
            if(state.ActiveCommandMode!=mode)
            {
                if(Tap(mode==TacticalCommandMode.Select?controls.SelectButton:mode==TacticalCommandMode.Attack?controls.AttackButton:mode==TacticalCommandMode.Board?controls.CommandWheelPanel.NextBoardButton:controls.MoveButton))focusStep=-1;
                return;
            }
            // Use the already visible direct target; Show Me is intentionally
            // hidden when the player can perform the action without a camera pan.
            bool usable=TryGestureGeometry(em,root,step,in target,camera,out var from,out var to,out bool drag);
            int focusKey=step*32+(target.NeedsSelection?0:16)+(int)mode;
            if(focusStep!=focusKey)
            {
                if(usable){focusStep=focusKey;focusAt=EditorApplication.timeSinceStartup-15;}
                else
                {
                    if(Tap(guide.ShowMeButton)){focusStep=focusKey;focusAt=EditorApplication.timeSinceStartup;}
                    return;
                }
            }
            double panElapsed=EditorApplication.timeSinceStartup-focusAt;
            // The authored camera can finish much later than the requested ease
            // duration. Observe its authoritative transition before retrying focus.
            if(panElapsed<.5||panElapsed<15&&CameraTransitionRunning(em))return;
            if(!usable){focusStep=-1;return;}
            if(drag)
            {
                if(touch.TryGesture(from,to,.55f,.9f,Time.unscaledTime))
                {lastInput=EditorApplication.timeSinceStartup;Debug.Log("[GroundedSignalInput] normalTouch=dragSelection step="+step);}
            }
            else Gesture(from,"step-"+step+"-"+mode);
        }
        private static bool CameraTransitionRunning(EntityManager em)
        {
            using var cameras=em.CreateEntityQuery(typeof(RtsCameraStateComponent));
            if(cameras.CalculateEntityCount()!=1)return false;
            var state=cameras.GetSingleton<RtsCameraStateComponent>();
            return state.HasSmoothFocusTarget!=0||state.HasSmoothPerspectiveTarget!=0;
        }
        private static bool TryGestureGeometry(EntityManager em,Entity root,int step,in UiMissionTutorialTarget target,Camera camera,
            out Vector2 from,out Vector2 to,out bool drag)
        {
            from=to=default;drag=target.NeedsSelection&&step is 2 or 3 or 4;
            if(drag&&step==2)
            {
                var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                var extraction=em.GetComponentData<CampaignMissionExtractionState>(root);
                if(guidance.SourceEntity==extraction.Carrier)drag=false;
            }
            if(drag)
            {
                var positions=new List<Vector3>();
                if(step is 3 or 4)
                {foreach(var member in em.GetBuffer<CampaignMissionGroundedSignalSpecialist>(root,true))if(em.Exists(member.Entity)&&em.HasComponent<LocalTransform>(member.Entity))positions.Add(em.GetComponentData<LocalTransform>(member.Entity).Position);}
                else
                {using var actors=em.CreateEntityQuery(typeof(CampaignMissionUnitRoleComponent),typeof(LocalTransform));using var entities=actors.ToEntityArray(Unity.Collections.Allocator.Temp);foreach(var entity in entities)if(em.GetComponentData<CampaignMissionUnitRoleComponent>(entity).MissionRoleId.Equals("role.friendly.command_squad")&&
                    em.HasComponent<UnitHealth>(entity)&&em.GetComponentData<UnitHealth>(entity).Current>0&&!em.HasComponent<Disabled>(entity))positions.Add(em.GetComponentData<LocalTransform>(entity).Position);}
                if(positions.Count<2)
                {
                    if(step!=2)throw new InvalidOperationException("Group selection missing members at step "+step);
                    drag=false;
                    var survivor=camera.WorldToScreenPoint(positions.Count==1?positions[0]+Vector3.up:target.Selection+Vector3.up);
                    from=to=survivor;return survivor.z>0&&WorldPointUsable(from);
                }
                from=new Vector2(float.MaxValue,float.MaxValue);to=new Vector2(float.MinValue,float.MinValue);
                foreach(var p in positions){var screen=camera.WorldToScreenPoint(p+Vector3.up);if(screen.z<=0)return false;from=Vector2.Min(from,screen);to=Vector2.Max(to,screen);}
                from-=Vector2.one*18;to+=Vector2.one*18;
                return WorldPointUsable(from)&&WorldPointUsable(to)&&WorldPointUsable((from+to)*.5f);
            }
            var point=camera.WorldToScreenPoint(target.NeedsSelection?target.Selection+Vector3.up:target.Destination+(step is 2 or 4?Vector3.up:Vector3.zero));
            from=to=point;
            return point.z>0&&WorldPointUsable(from);
        }
        private static bool WorldPointUsable(Vector2 point)
        {
            if(Camera.main==null||!Camera.main.pixelRect.Contains(point)||UnityEngine.EventSystems.EventSystem.current==null)return false;
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point},hits);return hits.Count==0;
        }
        private static void SampleMedia()
        {
            foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
                if(source.isPlaying&&source.clip!=null&&source.timeSamples>0&&!source.mute&&source.volume>0)
                {
                    string path=AssetDatabase.GetAssetPath(source.clip);
                    if(!path.StartsWith(CH04M04GroundedSignalNarrativeBuilder.VoiceRoot+"/",StringComparison.Ordinal))continue;
                    voices.Add(path);
                    float progress=(float)source.timeSamples/source.clip.frequency;
                    if(!voiceProgress.TryGetValue(path,out float observed)||progress>observed)voiceProgress[path]=progress;
                    // Near-end samples establish full natural playback even when the
                    // audio source resets timeSamples at the next panel transition.
                    if(progress>=source.clip.length-.45f)completedVoices.Add(path);
                }
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            if(view?.CurrentPanelSprite==null||!view.IsPanelVisible||!view.CurrentPanelSprite.name.StartsWith("CH04M04_",StringComparison.Ordinal))return;
            string panel=view.CurrentPanelSprite.name;
            if(panels.Add(panel))panelFirstSeen[panel]=EditorApplication.timeSinceStartup;
            // Panel binding can precede the new dialogue/caption by a frame. Retain
            // playback coverage immediately, then capture the settled native screen.
            if(!capturedPanels.Contains(panel)&&panelFirstSeen.TryGetValue(panel,out double firstSeen)&&
                EditorApplication.timeSinceStartup-firstSeen>=1.5)
            {Shot("comic-"+panel);capturedPanels.Add(panel);}

        }
        private static void ConfigureFixture()
        {
            Application.runInBackground=true;if(!AriaTouchInputUiSystemHelper.AllowBackgroundValidation||fixtureSettings!=null)return;
            originalSettings=InputSystem.settings;fixtureSettings=UnityEngine.Object.Instantiate(originalSettings);fixtureSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;fixtureSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=fixtureSettings;
            foreach(var device in InputSystem.devices)if(device.native&&device.enabled&&(device is UnityEngine.InputSystem.Pointer||device is Keyboard)){InputSystem.ResetDevice(device);InputSystem.DisableDevice(device);isolated.Add(device);}
            Debug.Log("[GroundedSignalInput] inputFixture=background-game-view-only nativeInputIsolated="+isolated.Count);
        }
        private static void EnsureTouch(){if(touch!=null)return;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Public touch input unavailable.");}
        private static T Field<T>(object owner,string name) where T:class=>owner?.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as T;
        private static bool Visible(object owner,string field){var group=Field<CanvasGroup>(owner,field);return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static bool Ready(Button b)=>b!=null&&b.IsActive()&&b.IsInteractable();
        private static bool Tap(Button b)
        {if(!Ready(b))return false;var rect=(RectTransform)b.transform;var canvas=b.GetComponentInParent<Canvas>();var wedge=b.targetGraphic as V3RadialWedgeGraphic;var point=wedge!=null?wedge.GuidanceTouchPoint:rect.TransformPoint(rect.rect.center);var p=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,point);return Gesture(p,b.name);}
        private static bool Gesture(Vector2 p,string label)
        {if(!touch.TryGesture(p,p,.18f,0,Time.unscaledTime))return false;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[GroundedSignalInput] touch="+label);return true;}
        /// <summary>Stop only this failed probe, restore its input fixture, and leave the Editor open.</summary>
        public static void StopFailed(string detail)=>Complete(false,detail);
        private static void Observe(string message,string stack,LogType type)
        {
            if(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,type))return;
            bool missionMarker=message.StartsWith("[CampaignMissionBootstrap]",StringComparison.Ordinal)||
                message.IndexOf("OperationMapSourceScene",StringComparison.Ordinal)>=0;
            if(type is LogType.Exception or LogType.Assert || type==LogType.Error&&missionMarker)error=message;
        }
        private static void Shot(string name)=>ScreenCapture.CaptureScreenshot(Output+"/"+name+".png");
        private static void Complete(bool passed,string detail)
        {
            if(finished)return;finished=true;touch?.Dispose();touch=null;
            foreach(var device in isolated)if(device.added)InputSystem.EnableDevice(device);isolated.Clear();
            if(fixtureSettings!=null){InputSystem.settings=originalSettings;UnityEngine.Object.Destroy(fixtureSettings);fixtureSettings=null;}
            SessionState.SetBool(Active,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;Shot("last");
            string marker="[GroundedSignalInput] result="+(passed?"Passed":"Failed")+" "+detail;File.WriteAllText(Output+"/result.txt",marker+"\n");Debug.Log(marker);if(!ExistingEditorValidation.IsRunning)AssetDatabase.AllowAutoRefresh();
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",null);
            if(SessionState.GetBool(LiveKey,true))EditorApplication.ExitPlaymode();else MissionEditorValidationExit.Complete(passed);
        }
    }
}
