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
    public static class CH04M01AirCorridorInputProbe
    {
        private const string Active="Warline.AirCorridor.Input",Locale="Warline.AirCorridor.Locale";
        private static string Output;
        private static int stage,actions;private static double started,due,lastLog;private static bool projectileSeen,radarLinked;private static string error;
        private static CampaignMissionProgressStore store;
        private static readonly HashSet<string> voices=new(),panels=new();
        private static AudioClip comicClip;private static float comicProgress;private static double engageStarted;
        private static UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode savedEditorInput;
        private static UnityEngine.InputSystem.InputSettings.BackgroundBehavior savedBackgroundInput;
        private static bool inputRoutingConfigured;
        static CH04M01AirCorridorInputProbe(){if(SessionState.GetBool(Active,false)){EditorApplication.update+=Tick;Application.logMessageReceived+=Observe;}}
        public static void RunEnglish(){SessionState.SetString(Locale,"en");Run();}
        public static void RunPersian(){SessionState.SetString(Locale,"fa-IR");Run();}
        public static void RunPersianBriefingReview()
        {
            SessionState.SetBool(Active+".BriefingOnly",true);RunPersian();
        }
        public static void RunFinalEnglish()
        {
            CH04M01AirCorridorPresentationBuilder.BuildCheckpoint();
            SessionState.SetBool(Active+".ReviewWideBriefing",true);RunEnglish();
        }
        private static void Run()
        {
            Output="Design/AgentReports/ImplementedMissionMonetization/Evidence/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-air-corridor-"+SessionState.GetString(Locale,"en");
            Directory.CreateDirectory(Output);SessionState.SetBool(Active,true);stage=actions=0;started=EditorApplication.timeSinceStartup;due=0;error=null;projectileSeen=radarLinked=false;voices.Clear();panels.Clear();comicClip=null;comicProgress=0;
            engageStarted=0;Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");MainMenuV3PrefabBuilder.SetGameViewResolution(SessionState.GetString(Locale,"en")=="fa-IR"?2400:1920,1080);Application.runInBackground=true;
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
                if(EditorApplication.timeSinceStartup-started>650)throw new TimeoutException("Air Corridor input journey timed out at stage "+stage);
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;var em=world.EntityManager;
                using var q=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(q.CalculateEntityCount()!=1)return;var root=q.GetSingletonEntity();
                if(stage==0)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,Guid.NewGuid().ToString("N")))));
                    for(int i=0;i<CampaignMissionSequence.RegisteredMissionCount;i++)store.EnsureAvailable(CampaignMissionSequence.IdAt(i));
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                    EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
                    // Wrapper-owned GUI checks may run behind Codex. Route their real
                    // touch device as a foreground Game view; never save these settings.
                    var input=UnityEngine.InputSystem.InputSystem.settings;
                    savedEditorInput=input.editorInputBehaviorInPlayMode;savedBackgroundInput=input.backgroundBehavior;
                    input.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    input.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                    inputRoutingConfigured=true;
                    AriaTouchInputUiSystemHelper.Evidence-=ObserveTouch;AriaTouchInputUiSystemHelper.Evidence+=ObserveTouch;
                    GameLocalization.SetLocale(SessionState.GetString(Locale,"en"),false);stage=1;return;
                }
                if(stage==1)
                {
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign))return;
                    if(campaign.SelectedMission.MissionId!=CampaignMissionSequence.AirCorridor){UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,CampaignMissionSequence.AirCorridor);return;}
                    var view=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>(FindObjectsInactive.Exclude);
                    if(view==null){UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,UIRoute.Campaign,true);return;}
                    if(due==0){due=EditorApplication.timeSinceStartup+4;return;}if(EditorApplication.timeSinceStartup<due)return;
                    Shot("campaign");if(!Click(view.LaunchMissionButton))return;stage=2;due=EditorApplication.timeSinceStartup+4;return;
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
                    Complete(true,$"locale={GameLocalization.CurrentLocaleCode} voices=6 missiles=live radar=linked ARIA-actions={actions} victory=result=settlement=return");return;
                }
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                SampleMedia();using(var missiles=em.CreateEntityQuery(typeof(AirMissileProjectileComponent)))projectileSeen|=missiles.CalculateEntityCount()>0;
                using(var links=em.CreateEntityQuery(typeof(AirDefenseSupportLinkComponent)))
                using(var values=links.ToComponentDataArray<AirDefenseSupportLinkComponent>(Unity.Collections.Allocator.Temp))
                    foreach(var link in values)radarLinked|=link.RadarProvider!=Entity.Null&&link.RangeBonus>0;
                if(EditorApplication.timeSinceStartup-lastLog>5)
                {
                    lastLog=EditorApplication.timeSinceStartup;var guide=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                    Debug.Log($"[AirCorridorInputState] stage={stage} phase={runtime.Phase} outcome={runtime.Outcome} clock={facts.ElapsedMilliseconds} hostile={facts.HostileDefeatedCount}/{facts.HostileTotalCount} core={facts.CoreBreached} integrity={facts.HostileRosterIntegrityFault} guide={guide.GuidanceId} actions={actions} missiles={projectileSeen} radar={radarLinked}");
                    if(runtime.Phase==MissionPhaseKind.Engage)Diagnose(em,root);
                }
                if(runtime.Outcome==MissionOutcomeKind.Defeat)throw new InvalidOperationException("Air Corridor defeated: core="+facts.CoreBreached+" radar="+facts.ForwardPostDestroyed+" integrity="+facts.HostileRosterIntegrityFault);
                if(stage==4)
                {
                    using var shellq=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));if(shellq.CalculateEntityCount()!=1)return;
                    var shell=shellq.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>();if(shell.ActiveRoute!=UIRoute.Campaign||shell.IsTransitionRunning!=0)return;
                    if(UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>(FindObjectsInactive.Exclude)==null)return;
                    if(due==0){due=EditorApplication.timeSinceStartup+4;return;}if(EditorApplication.timeSinceStartup<due)return;
                    if(!projectileSeen||!radarLinked||actions<1)throw new InvalidOperationException("Missing missile launch, radar support or normal touch-input evidence");
                    if(!store.ReadAll().Single(x=>x.missionId==CampaignMissionSequence.AirCorridor).firstClearRewardSettled)throw new InvalidOperationException("Missing settlement");
                    string language=GameLocalization.CurrentLocaleCode.StartsWith("fa")?"fa":"en";
                    foreach(var line in CH04M01AirCorridorCopy.Brief.Concat(CH04M01AirCorridorCopy.Comms).Concat(CH04M01AirCorridorCopy.Debrief))
                        if(!voices.Contains(CH04M01AirCorridorNarrativeBuilder.VoiceRoot+"/"+language+"/"+line.Id+".wav"))throw new InvalidOperationException("Missing playback "+line.Id);
                    Shot("return");stage=8;due=EditorApplication.timeSinceStartup+1;return;
                }
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if(!UiShellRuntimeGateway.TryReadMissionResult(out var result))return;
                    if(!result.Defense.Applicable||!result.PrimaryActionEnabled||result.SettlementFailed)throw new InvalidOperationException("Missing ready result");
                    if(due==0){Shot("result");due=EditorApplication.timeSinceStartup+3;return;}if(EditorApplication.timeSinceStartup<due)return;
                    var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();var button=view==null?null:typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view)as Button;
                    if(!Click(button))return;stage=4;due=0;return;
                }
                if(runtime.Phase!=MissionPhaseKind.Engage)return;
                if(engageStarted==0){engageStarted=EditorApplication.timeSinceStartup;Shot("hud");}
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
            {if(!source.isPlaying||source.clip==null||source.timeSamples<=0||source.mute||source.volume<=0)continue;string path=AssetDatabase.GetAssetPath(source.clip);if(path.StartsWith(CH04M01AirCorridorNarrativeBuilder.VoiceRoot+"/"))voices.Add(path);}
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            var voice=view?.VoiceSource;var clip=voice!=null&&voice.isPlaying?voice.clip:null;
            if(clip!=comicClip)
            {
                if(comicClip!=null&&comicProgress<comicClip.length-.45f)throw new InvalidOperationException("Comic voice cut off: "+comicClip.name);
                comicClip=clip;comicProgress=0;
            }
            if(clip!=null)comicProgress=Mathf.Max(comicProgress,(float)voice.timeSamples/clip.frequency);
            if(view?.DialogueView!=null&&clip==null&&view.DialogueView.Phase==NarrativeDialoguePhase.AdvanceReady)
                Click(typeof(NarrativeDialogueView).GetField("inputButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view.DialogueView)as Button);
            if(view?.CurrentPanelSprite!=null&&view.CurrentPanelSprite.name.StartsWith("CH04M01_"))
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
            Debug.Log($"[AirCorridorAssistant] ready={panelReady} recommendation={panel.HasRecommendation} step={panel.TutorialStep}/{panel.TutorialStepCount} locked={locked} body={panel.RecommendationBody}");
            using var shellq=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));
            var boundary=shellq.GetSingletonEntity();var shell=shellq.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>();
            using var matchq=em.CreateEntityQuery(typeof(MatchStartQueueComponent));var match=matchq.GetSingleton<MatchStartQueueComponent>();
            Debug.Log($"[AirCorridorAssistantBoundary] route={shell.ActiveRoute} mode={shell.CurrentMode} transition={shell.IsTransitionRunning} started={match.HasStarted} pending={match.IsStartPending} state={em.HasComponent<AssistantStateComponent>(boundary)} recommendation={em.HasComponent<AssistantRecommendationReadModelComponent>(boundary)} message={em.HasComponent<AssistantMessageReadModelComponent>(boundary)} threat={em.HasComponent<AssistantThreatReadModelStateComponent>(boundary)} target={em.HasComponent<AssistantTargetLockReadModelComponent>(boundary)} objective={em.HasComponent<MatchObjectiveRuntimeStateComponent>(boundary)} goals={em.HasBuffer<AssistantGoalReadModelElement>(boundary)} recommendations={em.HasBuffer<AssistantRecommendationElement>(boundary)} messages={em.HasBuffer<AssistantMessageElement>(boundary)}");
            if(em.HasComponent<Game.UI.Shell.Contracts.Ecs.AriaPlayObservationComponent>(boundary))
            {var observation=em.GetComponentData<Game.UI.Shell.Contracts.Ecs.AriaPlayObservationComponent>(boundary);var session=em.GetComponentData<Game.UI.Shell.Contracts.Ecs.AriaPlaySessionComponent>(boundary);var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(x=>x.GetEntityId().GetHashCode()==observation.TargetId);Debug.Log($"[AirCorridorAriaTarget] phase={session.Phase} kind={observation.Kind} goal={observation.GoalId} target={observation.TargetId} button={button?.name} point={observation.Position} mask={em.GetComponentData<CampaignMissionDefenseStateComponent>(root).AcknowledgedGuidanceMask} stop={session.StopReason}");}
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
                if(!em.Exists(member.Entity)||!em.HasComponent<UnitHealth>(member.Entity))Debug.Log($"[AirCorridorMissingMember] entity={member.Entity} element={member.ElementIndex} faction={member.FactionId} sensor={member.IsSensor} initialized={member.HealthInitialized} defeated={member.Defeated}");
        }
        private static void Observe(string message,string stack,LogType type)
        {if(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,type))return;if(type is LogType.Exception or LogType.Assert||type==LogType.Error&&message.StartsWith("[CampaignMissionBootstrap]"))error=message;}
        private static void ObserveTouch(AriaTouchInputUiSystemHelper helper,string phase,Vector2 position)
        {
            if(phase!="Began"&&phase!="Ended")return;
            var hits=new List<RaycastResult>();
            EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
            Debug.Log($"[AirCorridorTouch] phase={phase} point={position} hit={hits.FirstOrDefault().gameObject?.name} focus={EditorWindow.focusedWindow?.GetType().Name} module={EventSystem.current?.currentInputModule?.GetType().Name}");
        }
        private static void Complete(bool passed,string detail)
        {
            if(inputRoutingConfigured){var input=UnityEngine.InputSystem.InputSystem.settings;input.editorInputBehaviorInPlayMode=savedEditorInput;input.backgroundBehavior=savedBackgroundInput;inputRoutingConfigured=false;}
            SessionState.SetBool(Active,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;AriaTouchInputUiSystemHelper.Evidence-=ObserveTouch;Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",null);Debug.Log("[AirCorridorInput] result="+(passed?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(passed);
        }
    }
}
