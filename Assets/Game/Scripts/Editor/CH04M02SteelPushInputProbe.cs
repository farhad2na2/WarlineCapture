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
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Isolated saves; shipping ARIA touch input. No health, position, objective or outcome injection.</summary>
    [InitializeOnLoad]
    public static partial class CH04M02SteelPushInputProbe
    {
        private const string Active="Warline.SteelPush.Input",Locale="Warline.SteelPush.Locale";
        private static string Output;
        private static int stage,actions;private static double started,due,lastLog;private static bool armorCombatSeen,fuelSpent;private static string error;
        private static CampaignMissionProgressStore store;
        private static SaveService inputSave; private static int startingCredits,startingXp;
        private static readonly HashSet<string> voices=new(),panels=new(),readyPanels=new();
        private static AudioClip comicClip;private static float comicProgress;private static double engageStarted,comicAdvanceAt,profileReadyDeadline;
        private static bool comicReadyCaptured;
        private static UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode savedEditorInput;
        private static UnityEngine.InputSystem.InputSettings.BackgroundBehavior savedBackgroundInput;
        private static bool inputRoutingConfigured;
        static CH04M02SteelPushInputProbe(){if(SessionState.GetBool(Active,false)){EditorApplication.update+=Tick;Application.logMessageReceived+=Observe;}}
        public static void RunEnglish(){SessionState.SetString(Locale,"en");Run();}
        public static void RunPersian(){SessionState.SetString(Locale,"fa-IR");Run();}
        public static void RunPersianBriefingReview()
        {
            SessionState.SetBool(Active+".BriefingOnly",true);RunPersian();
        }
        public static void RunFinalEnglish()
        {
            CH04M02SteelPushPresentationBuilder.BuildCheckpoint();
            SessionState.SetBool(Active+".ReviewWideBriefing",true);RunEnglish();
        }
        private static void Run()
        {
            Output=Path.GetFullPath("Design/AgentReports/MapVariantMissionRework/SteelPush/Evidence/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-steel-push-"+SessionState.GetString(Locale,"en"));
            Directory.CreateDirectory(Output);SessionState.SetBool(Active,true);stage=actions=0;started=EditorApplication.timeSinceStartup;due=0;error=null;armorCombatSeen=fuelSpent=false;voices.Clear();panels.Clear();readyPanels.Clear();comicReadyCaptured=false;comicClip=null;comicProgress=0;
            engageStarted=comicAdvanceAt=0;Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");MainMenuV3PrefabBuilder.SetGameViewResolution(SessionState.GetString(Locale,"en")=="fa-IR"?2400:1920,1080);Application.runInBackground=true;
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);AssetDatabase.DisallowAutoRefresh();
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Observe;Application.logMessageReceived+=Observe;EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(error!=null)throw new InvalidOperationException(error);
                if(started==0)started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>650)throw new TimeoutException("Steel Push input journey timed out at stage "+stage);
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;var em=world.EntityManager;
                using var q=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(q.CalculateEntityCount()!=1)return;var root=q.GetSingletonEntity();
                if(stage==0)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    string saveRoot=Path.Combine(Output,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(saveRoot);
                    if(SavedReplay){File.Copy(Path.GetFullPath(ReplayProfile),Path.Combine(saveRoot,"profile.json"));Debug.Log("[SteelPushReplayFixture] profile=copied-native-completed-journey rewards=not-injected outcome=not-injected");}
                    inputSave=new SaveService(new JsonSaveRepository(saveRoot));store=new CampaignMissionProgressStore(inputSave);
                    for(int i=0;i<16;i++)store.EnsureAvailable(CampaignMissionSequence.IdAt(i));
                    // Simulate a real old save whose M01 clear predated M02's addition.
                    store.Settle(CampaignMissionSequence.AirCorridor,"prior-air-clear",1,true,3,120000,string.Empty);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                    var initial=inputSave.LoadProfile();startingCredits=initial.credits;startingXp=initial.commanderXp;
                    ResetRefineryProbe();
                    replaying=SavedReplay;startingReplayCount=store.ReadAll().SingleOrDefault(x=>x.missionId==CampaignMissionSequence.SteelPush)?.successfulReplayCount??0;
                    EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                    // Wrapper-owned GUI checks may run behind Codex. Route their real
                    // touch device as a foreground Game view; never save these settings.
                    var input=UnityEngine.InputSystem.InputSystem.settings;
                    savedEditorInput=input.editorInputBehaviorInPlayMode;savedBackgroundInput=input.backgroundBehavior;
                    input.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    input.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                    inputRoutingConfigured=true;
                    AriaTouchInputUiSystemHelper.Evidence-=ObserveTouch;AriaTouchInputUiSystemHelper.Evidence+=ObserveTouch;
                    GameLocalization.SetLocale(SessionState.GetString(Locale,"en"),false);
                    profileReadyDeadline=EditorApplication.timeSinceStartup+15;stage=1;return;
                }
                if(stage==1)
                {
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign))return;
                    // Store replacement precedes the next PresentationSystemGroup update.
                    // Wait for that real projection; never inject the availability bit.
                    if((campaign.AvailableMissionMask&(1u<<16))==0)
                    {
                        if(EditorApplication.timeSinceStartup<profileReadyDeadline)return;
                        throw new InvalidOperationException("Steel Push not unlocked from existing Air Corridor clear");
                    }
                    var view=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>(FindObjectsInactive.Exclude);
                    if(view==null)
                    {
                        var home=UnityEngine.Object.FindAnyObjectByType<MainMenuCampaignCardView>();
                        if(home!=null)Click(typeof(MainMenuCampaignCardView).GetField("continueButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(home)as Button);
                        return;
                    }
                    if(!view.IsChapterFour){Click(view.ChapterFourButton);return;}
                    if(campaign.SelectedMission.MissionId!=CampaignMissionSequence.SteelPush){Click(view.MissionNodeButtons[1]);return;}
                    if(due==0){due=EditorApplication.timeSinceStartup+4;return;}if(EditorApplication.timeSinceStartup<due)return;
                    Shot(replaying?"replay-campaign":"campaign");if(!Click(view.LaunchMissionButton))return;stage=2;due=EditorApplication.timeSinceStartup+4;return;
                }
                if(stage==2)
                {
                    var view=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>(FindObjectsInactive.Exclude);if(view==null||EditorApplication.timeSinceStartup<due)return;
                    if(SessionState.GetBool(Active+".ReviewWideBriefing",false))
                    {
                        SessionState.SetBool(Active+".ReviewWideBriefing",false);
                        GameLocalization.SetLocale("fa-IR",false);MainMenuV3PrefabBuilder.SetGameViewResolution(2400,1080);
                        stage=5;due=EditorApplication.timeSinceStartup+4;return;
                    }
                    RequireBriefingIntelFits(view);
                    if(SessionState.GetBool(Active+".BriefingOnly",false))
                    {
                        Shot("briefing-final-review");stage=6;due=EditorApplication.timeSinceStartup+1;return;
                    }
                    Shot("briefing");if(!Click(view.DeployOperationButton))return;stage=3;due=0;return;
                }
                if(stage==5)
                {
                    var view=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>(FindObjectsInactive.Exclude);if(view==null||EditorApplication.timeSinceStartup<due)return;
                    RequireBriefingIntelFits(view);Shot("briefing-final-review");
                    stage=7;due=EditorApplication.timeSinceStartup+1;return;
                }
                if(stage==6)
                {
                    if(EditorApplication.timeSinceStartup<due)return;
                    SessionState.SetBool(Active+".BriefingOnly",false);
                    Complete(true,"locale=fa-IR briefing=readable aspect=20x9 visual-only");return;
                }
                if(stage==7)
                {
                    if(EditorApplication.timeSinceStartup<due)return;
                    GameLocalization.SetLocale("en",false);MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
                    stage=2;due=EditorApplication.timeSinceStartup+4;return;
                }
                if(stage==8)
                {
                    if(EditorApplication.timeSinceStartup<due)return;
                    Complete(true,$"locale={GameLocalization.CurrentLocaleCode} dialogue=7 armorCombat=live fuel=spent ARIA-actions={actions} manual-actions={manualActions} publicEntry=Passed source=RefineryDistrict victory=result=settlement=return");return;
                }
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                if(SessionState.GetBool(Active+".Review",false)&&runtime.Phase==MissionPhaseKind.InteractiveBrief)
                {
                    SessionState.SetBool(Active+".Review",false);SessionState.SetBool(Active,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;AriaTouchInputUiSystemHelper.Evidence-=ObserveTouch;
                    if(inputRoutingConfigured){var input=UnityEngine.InputSystem.InputSystem.settings;input.editorInputBehaviorInPlayMode=savedEditorInput;input.backgroundBehavior=savedBackgroundInput;inputRoutingConfigured=false;}
                    Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",null);AssetDatabase.AllowAutoRefresh();
                    Debug.Log("[SteelPushHumanReview] ready=True automation=Stopped isolatedProfile=True acceptance=Pending");return;
                }
                if(facts.HostileRosterIntegrityFault!=0)
                {
                    using var buildings=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
                    if(buildings.CalculateEntityCount()==1)foreach(var request in em.GetBuffer<BuildingRuntimeSpawnRequest>(buildings.GetSingletonEntity(),true))
                        Debug.Log($"[SteelPushStartup] building={request.BuildingId} id={request.RequestId} status={request.Status} reason={request.ResultCode} origin={request.PreferredOrigin} actual={request.ActualOrigin} footprint={request.ActualFootprint}");
                    if(em.HasComponent<CampaignMissionSteelPushState>(root)){var reserve=em.GetComponentData<CampaignMissionSteelPushState>(root);Debug.Log($"[SteelPushStartup] reserve={reserve.FuelReserve} initialized={reserve.Initialized} request={reserve.FuelSpawnRequestId}");}
                    throw new InvalidOperationException("Steel Push startup integrity fault; see authored building and roster diagnostics");
                }
                SampleMedia();
                if(CompleteNegative(em,root,in runtime,in facts))return;
                if(runtime.Phase==MissionPhaseKind.Engage)ObserveRefinery(em,root,in facts);
                if(em.HasComponent<CampaignMissionSteelPushState>(root))fuelSpent|=em.GetComponentData<CampaignMissionSteelPushState>(root).FuelSpent!=0;
                if(em.HasBuffer<CampaignMissionDefenseMember>(root))foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
                    if(member.FactionId!=1&&em.Exists(member.Entity)&&em.HasComponent<UnitHealth>(member.Entity))
                    {var health=em.GetComponentData<UnitHealth>(member.Entity);armorCombatSeen|=health.Max>0&&health.Current<health.Max;}
                if(EditorApplication.timeSinceStartup-lastLog>5)
                {
                    lastLog=EditorApplication.timeSinceStartup;var guide=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                    Debug.Log($"[SteelPushInputState] stage={stage} phase={runtime.Phase} outcome={runtime.Outcome} clock={facts.ElapsedMilliseconds} hostile={facts.HostileDefeatedCount}/{facts.HostileTotalCount} core={facts.CoreBreached} integrity={facts.HostileRosterIntegrityFault} guide={guide.GuidanceId} actions={actions} armorCombat={armorCombatSeen} fuelSpent={fuelSpent}");
                    if(runtime.Phase==MissionPhaseKind.Engage)Diagnose(em,root);
                }
                if(runtime.Outcome==MissionOutcomeKind.Defeat)throw new InvalidOperationException("Steel Push defeated: core="+facts.CoreBreached+" fuel="+facts.ForwardPostDestroyed+" integrity="+facts.HostileRosterIntegrityFault);
                if(stage==4)
                {
                    using var shellq=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));if(shellq.CalculateEntityCount()!=1)return;
                    var shell=shellq.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>();if(shell.ActiveRoute!=UIRoute.Campaign||shell.IsTransitionRunning!=0)return;
                    if(UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>(FindObjectsInactive.Exclude)==null)return;
                    if(due==0){due=EditorApplication.timeSinceStartup+4;return;}if(EditorApplication.timeSinceStartup<due)return;
                    if(!armorCombatSeen||(!ShortageFixture&&!fuelSpent)||Mathf.Max(actions,manualActions)<1)throw new InvalidOperationException("Missing armor combat, physical Fuel spending or normal touch-input evidence");
                    if(!replaying&&panels.Count!=7)throw new InvalidOperationException("Missing comic playback: "+panels.Count+"/7");
                    if(!replaying&&readyPanels.Count!=7)throw new InvalidOperationException("Missing settled comic captures: "+readyPanels.Count+"/7");
                    if(!store.ReadAll().Single(x=>x.missionId==CampaignMissionSequence.SteelPush).firstClearRewardSettled)throw new InvalidOperationException("Missing settlement");
                    string language=GameLocalization.CurrentLocaleCode.StartsWith("fa")?"fa":"en";
                    foreach(var line in (replaying?CH04M02SteelPushCopy.Comms:CH04M02SteelPushCopy.Brief.Concat(CH04M02SteelPushCopy.Comms).Concat(CH04M02SteelPushCopy.Debrief)))
                        if(File.Exists(CH04M02SteelPushNarrativeBuilder.VoiceRoot+"/"+language+"/"+line.Id+".wav")&&!voices.Contains(CH04M02SteelPushNarrativeBuilder.VoiceRoot+"/"+language+"/"+line.Id+".wav"))throw new InvalidOperationException("Missing playback "+line.Id);
                    ValidateRefineryJourney();Shot(replaying?"replay-return":"return");
                    if(SessionState.GetBool(Active+".Replay",false)&&!replaying)
                    {replaying=true;ResetRefineryProbe();actions=0;engageStarted=due=0;armorCombatSeen=fuelSpent=false;voices.Clear();panels.Clear();readyPanels.Clear();comicReadyCaptured=false;comicClip=null;comicProgress=0;stage=1;profileReadyDeadline=EditorApplication.timeSinceStartup+15;Debug.Log("[SteelPushReplay] publicLaunch=begin rewardsBaseline=first-clear");return;}
                    stage=8;due=EditorApplication.timeSinceStartup+1;return;
                }
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if(!UiShellRuntimeGateway.TryReadMissionResult(out var result))return;
                    if(!result.Defense.Applicable||!result.PrimaryActionEnabled||result.SettlementFailed)throw new InvalidOperationException("Missing ready result");
                    if(due==0){due=EditorApplication.timeSinceStartup+3;return;}if(EditorApplication.timeSinceStartup<due)return;Shot(replaying?"replay-result":"result");
                    var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();var button=view==null?null:typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view)as Button;
                    if(!Click(button))return;stage=4;due=0;return;
                }
                if(runtime.Phase!=MissionPhaseKind.Engage)return;
                if(engageStarted==0){engageStarted=EditorApplication.timeSinceStartup;Shot("hud");}
                if(SessionState.GetBool(Active+".ManualInput",false)){if(!DriveNegative(em,root,in facts))DriveManual(em,root);return;}
                var aria=UiShellRuntimeGateway.ReadAriaPlay();if(aria.Actions>actions){actions=aria.Actions;Shot("hud-aria-"+actions);Diagnose(em,root);}
                if(aria.Phase==AriaPlayPhase.Blocked){Diagnose(em,root);throw new InvalidOperationException("ARIA input blocked after "+actions+" actions");}
                if(aria.Active)return;
                if(actions>0)throw new InvalidOperationException("ARIA stopped before victory");
                var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude);
                bool clicked=Click(buttons.FirstOrDefault(x=>x.name=="ConfirmWatchAria"&&x.IsInteractable())??buttons.FirstOrDefault(x=>x.name=="WatchAriaPlay"&&x.IsInteractable()));
                if(!clicked&&EditorApplication.timeSinceStartup-engageStarted>30)throw new InvalidOperationException("Existing ARIA Play control unavailable; see assistant diagnostics");
            }
            catch(Exception ex){Debug.LogException(ex);Complete(false,ex.Message);}
        }
        private static void SampleMedia()
        {
            foreach(var source in UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
            {if(!source.isPlaying||source.clip==null||source.timeSamples<=0||source.mute||source.volume<=0)continue;string path=AssetDatabase.GetAssetPath(source.clip);if(path.StartsWith(CH04M02SteelPushNarrativeBuilder.VoiceRoot+"/"))voices.Add(path);}
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            var voice=view?.VoiceSource;var clip=voice!=null&&voice.isPlaying?voice.clip:null;
            if(clip!=comicClip)
            {
                if(comicClip!=null&&comicProgress<comicClip.length-.45f)throw new InvalidOperationException("Comic voice cut off: "+comicClip.name);
                comicClip=clip;comicProgress=0;
            }
            if(clip!=null)comicProgress=Mathf.Max(comicProgress,(float)voice.timeSamples/clip.frequency);
            if(view?.CurrentPanelSprite!=null&&view.DialogueView?.Phase==NarrativeDialoguePhase.AdvanceReady)
            {
                var caption=typeof(NarrativeDialogueView).GetField("dialogueText",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view.DialogueView)as TMPro.TMP_Text;
                if(caption!=null&&caption.maxVisibleCharacters==int.MaxValue&&readyPanels.Add(view.CurrentPanelSprite.name))Shot("comic-"+view.CurrentPanelSprite.name+"-ready");
            }
            if(view?.DialogueView!=null&&clip==null&&view.DialogueView.Phase==NarrativeDialoguePhase.AdvanceReady)
            {
                if(comicAdvanceAt==0){comicAdvanceAt=EditorApplication.timeSinceStartup+1;return;}
                if(EditorApplication.timeSinceStartup<comicAdvanceAt)return;
                if(!comicReadyCaptured){Shot("comic-"+view.CurrentPanelSprite?.name+"-ready");comicReadyCaptured=true;comicAdvanceAt=EditorApplication.timeSinceStartup+1;return;}
                Click(typeof(NarrativeDialogueView).GetField("inputButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view.DialogueView)as Button);comicAdvanceAt=0;comicReadyCaptured=false;
            }
            if(view?.CurrentPanelSprite!=null&&view.CurrentPanelSprite.name.StartsWith("CH04M02_"))
            {string name=view.CurrentPanelSprite.name;if(panels.Add(name))EditorApplication.delayCall+=()=>Shot("comic-"+name);}
        }
        private static bool Click(Button button)
        {if(button==null||!button.isActiveAndEnabled||!button.IsInteractable())return false;ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);return true;}
        private static void RequireBriefingIntelFits(MissionBriefingScreenView view)
        {
            var text=typeof(MissionBriefingScreenView).GetField("enemyIntelLabel",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view)as TMPro.TMP_Text;
            text?.ForceMeshUpdate();if(text!=null&&text.isTextTruncated)throw new InvalidOperationException("Briefing enemy intel is truncated: "+GameLocalization.CurrentLocaleCode);
        }
        private static void Shot(string name)=>ScreenCapture.CaptureScreenshot(Output+"/"+GameLocalization.CurrentLocaleCode+"-"+name+".png");
        private static void Diagnose(EntityManager em,Entity root)
        {
            bool panelReady=UiShellRuntimeGateway.TryReadMatchHudAssistantPanel(out var panel);
            bool locked=UiShellRuntimeGateway.TryReadMissionHudRestrictions(out var restrictions)&&restrictions.CinematicInteractionLocked;
            Debug.Log($"[SteelPushAssistant] ready={panelReady} recommendation={panel.HasRecommendation} step={panel.TutorialStep}/{panel.TutorialStepCount} locked={locked} body={panel.RecommendationBody}");
            using var shellq=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));
            var boundary=shellq.GetSingletonEntity();var shell=shellq.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>();
            using var matchq=em.CreateEntityQuery(typeof(MatchStartQueueComponent));var match=matchq.GetSingleton<MatchStartQueueComponent>();
            Debug.Log($"[SteelPushAssistantBoundary] route={shell.ActiveRoute} mode={shell.CurrentMode} transition={shell.IsTransitionRunning} started={match.HasStarted} pending={match.IsStartPending} state={em.HasComponent<AssistantStateComponent>(boundary)} recommendation={em.HasComponent<AssistantRecommendationReadModelComponent>(boundary)} message={em.HasComponent<AssistantMessageReadModelComponent>(boundary)} threat={em.HasComponent<AssistantThreatReadModelStateComponent>(boundary)} target={em.HasComponent<AssistantTargetLockReadModelComponent>(boundary)} objective={em.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary)} goals={em.HasBuffer<AssistantGoalReadModelElement>(boundary)} recommendations={em.HasBuffer<AssistantRecommendationElement>(boundary)} messages={em.HasBuffer<AssistantMessageElement>(boundary)}");
            if(em.HasComponent<Game.UI.Shell.Contracts.Ecs.AriaPlayObservationComponent>(boundary))
            {var observation=em.GetComponentData<Game.UI.Shell.Contracts.Ecs.AriaPlayObservationComponent>(boundary);var session=em.GetComponentData<Game.UI.Shell.Contracts.Ecs.AriaPlaySessionComponent>(boundary);var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(x=>x.GetEntityId().GetHashCode()==observation.TargetId);Debug.Log($"[SteelPushAriaTarget] phase={session.Phase} kind={observation.Kind} goal={observation.GoalId} target={observation.TargetId} button={button?.name} point={observation.Position} mask={em.GetComponentData<CampaignMissionDefenseStateComponent>(root).AcknowledgedGuidanceMask} stop={session.StopReason}");}
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
                if(!em.Exists(member.Entity)||!em.HasComponent<UnitHealth>(member.Entity))Debug.Log($"[SteelPushMissingMember] entity={member.Entity} element={member.ElementIndex} faction={member.FactionId} sensor={member.IsSensor} initialized={member.HealthInitialized} defeated={member.Defeated}");
                else if(em.HasComponent<Unity.Transforms.LocalTransform>(member.Entity))
                {
                    var health=em.GetComponentData<UnitHealth>(member.Entity);
                    if(health.Current<=0)continue;
                    var position=em.GetComponentData<Unity.Transforms.LocalTransform>(member.Entity).Position;
                    var target=em.HasComponent<EngageTarget>(member.Entity)?em.GetComponentData<EngageTarget>(member.Entity).Target:Entity.Null;
                    Debug.Log($"[SteelPushLiveMember] element={member.ElementIndex} faction={member.FactionId} health={health.Current}/{health.Max} position={position} target={target}");
                }
        }
        private static void Observe(string message,string stack,LogType type)
        {if(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,type))return;if(type is LogType.Exception or LogType.Assert||type==LogType.Error&&message.StartsWith("[CampaignMissionBootstrap]"))error=message;}
        private static void ObserveTouch(AriaTouchInputUiSystemHelper helper,string phase,Vector2 position)
        {
            if(phase!="Began"&&phase!="Ended")return;
            var hits=new List<RaycastResult>();
            EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
            Debug.Log($"[SteelPushTouch] phase={phase} point={position} hit={hits.FirstOrDefault().gameObject?.name} focus={EditorWindow.focusedWindow?.GetType().Name} module={EventSystem.current?.currentInputModule?.GetType().Name}");
        }
        private static void Complete(bool passed,string detail)
        {
            touch?.Dispose();touch=null;
            if(inputRoutingConfigured){var input=UnityEngine.InputSystem.InputSystem.settings;input.editorInputBehaviorInPlayMode=savedEditorInput;input.backgroundBehavior=savedBackgroundInput;inputRoutingConfigured=false;}
            SessionState.SetBool(Active,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;AriaTouchInputUiSystemHelper.Evidence-=ObserveTouch;Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",null);Debug.Log("[SteelPushInput] result="+(passed?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(passed);
        }
    }
}
