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
    public static class CH05M03NetworkCollapseInputProbe
    {
        private const string Active="Warline.NetworkCollapse.Input",LocaleKey=Active+".Locale",WatchKey=Active+".Watch",LiveKey=Active+".Live";
        private static string Output=>SessionState.GetString(Active+".Output","Design/AgentReports/CH05M03NetworkCollapse/Evidence/input");
        private static double started,lastInput,lastLog,focusAt;
        private static int lastStep=-1,focusStep=-1;
        private static bool finished,won,sourceChecked,resultCaptured,sawDebrief,watchStarted;
        private static double resultCaptureAt;
        private static CampaignMissionProgressStore store;
        private static AriaTouchInputUiSystemHelper touch;
        private static InputSettings originalSettings,fixtureSettings;
        private static readonly List<InputDevice> isolated=new();
        private static readonly HashSet<string> panels=new(StringComparer.Ordinal);
        private static readonly HashSet<string> capturedPanels=new(StringComparer.Ordinal);
        private static readonly Dictionary<string,double> panelFirstSeen=new(StringComparer.Ordinal);
        private const string SavedPlayOptions=Active+".SavedPlayOptions";
        private static string error;
        private static int supportStep;
        private static bool supportChecked;
        private static int supportCharges,materialsBefore;
        private static float fuelBefore;
        private static int presentationEngageClock=-1;
        private static string SupportDecision=>SessionState.GetString(Active+".SupportDecision","approve");
        static CH05M03NetworkCollapseInputProbe()
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
        public static void RunEnglishCaptioned()=>Run("en",false,false,true);
        public static void RunPersianCaptioned()=>Run("fa-IR",false,false,true);
        public static void RunEnglishWatchCaptioned()=>Run("en",true,false,true);
        public static void RunPersianWatchCaptioned()=>Run("fa-IR",true,false,true);
        public static void RunGameplay()=>Run("en",false,false,true,false,true);
        public static void RunPersianWatchGameplay()=>Run("fa-IR",true,false,true,false,true);
        public static void RunPersianPresentation()=>Run("fa-IR",false,false,true,false,true,true);
        private static void Run(string locale,bool watch,bool live,bool captioned=true,bool recruitmentOnly=false,bool gameplayOnly=false,bool presentationOnly=false)
        {
            SessionState.SetString(Active+".SupportDecision",locale=="fa-IR"?"decline":"approve");
            supportStep=0;supportChecked=false;presentationEngageClock=-1;
            SessionState.SetBool(Active+".PresentationOnly",presentationOnly);
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Network Collapse input probe requires edit mode");
            if(ExistingEditorValidation.IsRunning)
            {
                CH05M03NetworkCollapseRulesValidation.Run();
            }
            SessionState.SetBool(Active+".GameplayOnly",gameplayOnly);SessionState.SetBool(Active+".RecruitmentOnly",recruitmentOnly);SessionState.SetBool(Active+".Captioned",captioned);SessionState.SetString(LocaleKey,locale);SessionState.SetBool(WatchKey,watch);SessionState.SetBool(LiveKey,live);
            SessionState.SetString(Active+".Output","Design/AgentReports/CH05M03NetworkCollapse/Evidence/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+locale+(watch?"-aria":"-manual")+(captioned?"-captioned":""));
            Directory.CreateDirectory(Output);finished=won=sourceChecked=resultCaptured=sawDebrief=watchStarted=false;lastStep=focusStep=-1;store=null;error=null;panels.Clear();capturedPanels.Clear();panelFirstSeen.Clear();lastInput=lastLog=focusAt=resultCaptureAt=0;
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
                if(EditorApplication.timeSinceStartup-started>1200)throw new TimeoutException("Network Collapse input timed out at step "+lastStep);
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;var root=roots.GetSingletonEntity();
                if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                if(store==null)
                {
                    store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"profile-"+Guid.NewGuid().ToString("N")))));
                    int target=CampaignMissionSequence.IndexOf(CampaignMissionSequence.NetworkCollapse);
                    for(int i=0;i<target;i++){string prior=CampaignMissionSequence.IdAt(i);var definition=AssetDatabase.FindAssets("t:MissionDefinitionConfig",new[]{"Assets/Game/Configs/Missions"}).Select(g=>AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(x=>x!=null&&x.MissionId==prior);
                        var grants=definition==null?Array.Empty<CampaignMissionRewardGrant>():PrerequisiteRewards(definition);
                        store.SettleWithRewards(prior,"input-prerequisite-"+i,1,true,3,1000,CampaignMissionSequence.Next(prior),grants,false);}
                    store.EnsureAvailable(CampaignMissionSequence.NetworkCollapse);
                    GameLocalization.SetLocale(SessionState.GetString(LocaleKey,"en"),false);
                }
                em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                var mission=em.HasComponent<CampaignMissionNetworkCollapseState>(root)?em.GetComponentData<CampaignMissionNetworkCollapseState>(root):default;
                if(runtime.MissionId.Equals(CampaignMissionSequence.NetworkCollapse)&&mission.Initialized!=0&&mission.Failure!=NetworkCollapseFailure.None)
                    throw new InvalidOperationException("Network Collapse failure: "+mission.Failure);
                if(EditorApplication.timeSinceStartup-lastLog>5)
                {lastLog=EditorApplication.timeSinceStartup;Debug.Log($"[NetworkCollapseInputState] phase={runtime.Phase} outcome={runtime.Outcome} step={lastStep} clock={facts.ElapsedMilliseconds} initialized={mission.Initialized} verified={mission.NodeOneVerified}/{mission.NodeTwoVerified}/{mission.NodeThreeVerified} disabled={mission.NodeOneDisabled}/{mission.NodeTwoDisabled}/{mission.NodeThreeDisabled} audit={mission.AuditRecovered} aboard={mission.EngineerAboard} extracted={mission.Extracted} failure={mission.Failure}");Shot("current");}
                SampleMedia();
                if(runtime.MissionId.Equals(CampaignMissionSequence.NetworkCollapse)&&runtime.Outcome==MissionOutcomeKind.Defeat)throw new InvalidOperationException("Network Collapse defeat: "+mission.Failure);
                if(runtime.MissionId.Equals(CampaignMissionSequence.NetworkCollapse)&&runtime.Outcome==MissionOutcomeKind.Victory&&!won)
                {
                    if(mission.NodeOneVerified==0||mission.NodeTwoVerified==0||mission.NodeThreeVerified==0||mission.NodeOneDisabled==0||mission.NodeTwoDisabled==0||mission.NodeThreeDisabled==0||mission.AuditRecovered==0||mission.EngineerAboard==0||mission.Extracted==0||mission.MilitaryCleared==0||facts.HostileTotalCount!=9||facts.HostileDefeatedCount!=9||facts.CivilianLossCount!=0)
                        throw new InvalidOperationException("Victory lacks ordered recon, real nodes, audit custody and original-team extraction");
                    won=true;Debug.Log("[NetworkCollapseOrdinaryVictory] result=Passed verification=orderedRecon nodes=normalAttack audit=originalEngineer extraction=actualBoardAndMove military=9 staff=alive Fuel=actualReserve");
                }
                var activeNarrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
                bool ownNarrativeVisible=activeNarrative!=null&&Visible(activeNarrative,"rootGroup");
                if(SessionState.GetBool(Active+".PresentationOnly",false) && runtime.MissionId.Equals(CampaignMissionSequence.NetworkCollapse) && runtime.Phase==MissionPhaseKind.Engage && runtime.Outcome==MissionOutcomeKind.None)
                {
                    if(presentationEngageClock<0)presentationEngageClock=facts.ElapsedMilliseconds;
                    if(!ownNarrativeVisible)
                    {
                        using var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                        if(maps.CalculateEntityCount()!=1)return;
                        var map=maps.GetSingleton<OperationMapMetadataComponent>();
                        if(!map.Blob.IsCreated||map.PhysicalSourceValidated==0||!map.Blob.Value.OperationMapId.Equals(CH05M03NetworkCollapseConfigBuilder.MapId))
                            throw new InvalidOperationException("Presentation capture lacks actual Network physical source");
                        var tutorial=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();
                        var guidance=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root);
                        if(tutorial==null||!tutorial.IsPresentationVisible||guidance.Active==0||guidance.GuidanceId<70001||guidance.GuidanceId>70008)return;
                        if(facts.ElapsedMilliseconds-presentationEngageClock<10000)return;
                        sourceChecked=true;
                        // Complete retains the final actual HUD in last.png; same-frame captures coalesce.
                        Complete(true,"scope=PresentationOnly locale=fa-IR source=Validated guidance=Visible engage=10s gameplay=NotClaimed voices=NotRequested humanDeviceAcceptance=pending");
                        return;
                    }
                }
                if(!won && runtime.Phase==MissionPhaseKind.Engage && mission.CommsReady!=0 && panels.Any(x=>x.Contains("Comms")) && !ownNarrativeVisible && !supportChecked)
                { EnsureTouch();touch.Tick(Time.unscaledTime);if(!touch.IsBusy&&EditorApplication.timeSinceStartup-lastInput>=.9)DriveSupport(em,root,in mission);return; }
                var watch=UiShellRuntimeGateway.ReadAriaPlay();
                if(watch.Active){watchStarted=true;touch?.Dispose();touch=null;return;}
                if(SessionState.GetBool(WatchKey,false)&&watchStarted&&!won)throw new InvalidOperationException("ARIA stopped before victory: "+watch.Phase);
                EnsureTouch();touch.Tick(Time.unscaledTime);if(touch.IsBusy||EditorApplication.timeSinceStartup-lastInput<.9)return;
                var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
                if(narrative!=null&&Visible(narrative,"rootGroup"))
                {
                    using var shells=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));if(shells.CalculateEntityCount()==1&&shells.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>().IsTransitionRunning!=0)return;
                    if(narrative.CurrentPanelSprite!=null&&IsOwnPanel(narrative.CurrentPanelSprite.name))
                    {
                        if(SessionState.GetBool(Active+".RecruitmentOnly",false)||SessionState.GetBool(Active+".GameplayOnly",false))
                        {
                            var skipConfirm=narrative.SkipConfirmationView;bool skipConfirming=skipConfirm!=null&&Visible(skipConfirm,"group");
                            Tap(Field<Button>(skipConfirming?(object)skipConfirm:narrative.PlaybackControlsView,skipConfirming?"confirmButton":"skipButton"));return;
                        }
                        if(won)sawDebrief=true;
                        // Advance locally authored captions through the normal dialogue control.
                        if(narrative.VoiceSource!=null&&narrative.VoiceSource.isPlaying)return;

                        if(narrative.DialogueView!=null&&narrative.DialogueView.Phase==NarrativeDialoguePhase.AdvanceReady)
                        {
                            Shot("comic-"+narrative.CurrentPanelSprite.name);
                            Tap(Field<Button>(narrative.DialogueView,"inputButton"));
                        }return;
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
                        bool gameplayOnly=SessionState.GetBool(Active+".GameplayOnly",false);
                        if((!gameplayOnly&&(panels.Count!=7||!sawDebrief))||!sourceChecked||!resultCaptured)throw new InvalidOperationException("Missing comic/source/result evidence panels="+panels.Count);
                        if(!supportChecked)throw new InvalidOperationException("Native optional Support decision not observed");
                        if(!store.ReadAll().Single(x=>x.missionId==CampaignMissionSequence.NetworkCollapse).firstClearRewardSettled)throw new InvalidOperationException("Ordinary settlement missing");
                        if(!store.ReadSupportUnlocks().Contains("ability.supply_drop"))throw new InvalidOperationException("Prior Trust first clear did not provide Supply availability");
                        Shot("return");Complete(true,"scope="+(gameplayOnly?"GameplayOnly voices=NotRequested dialogue=NativeSkip":"CaptionedGameplay voices=NotRequested dialogue=7")+" normalInput=Passed result=Passed settlement=Passed return=Passed route=OrderedReconGroundRaid audit=Recovered extraction=OriginalEngineerInCarrier Supply=Optional optionalSupport="+SupportDecision+" humanDeviceAcceptance=pending");
                    }return;
                }
                if(runtime.MissionId.Equals(CampaignMissionSequence.NetworkCollapse)&&runtime.Phase==MissionPhaseKind.Engage)
                {
                    if(!sourceChecked)
                    {using var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));if(maps.CalculateEntityCount()!=1)return;var map=maps.GetSingleton<OperationMapMetadataComponent>();if(!map.Blob.IsCreated||map.PhysicalSourceValidated==0||!map.Blob.Value.OperationMapId.Equals(CH05M03NetworkCollapseConfigBuilder.MapId))throw new InvalidOperationException("Incorrect Network Collapse physical map");sourceChecked=true;
                        foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                            if(button.name is "ConfirmSplitFrontShot" or "CancelSplitFrontShot")throw new InvalidOperationException("Obsolete gameplay launcher control present: "+button.name);
                        Shot("hud");}
                    if(SessionState.GetBool(WatchKey,false)){var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);Tap(buttons.FirstOrDefault(x=>x.name=="ConfirmWatchAria"&&Ready(x))??buttons.FirstOrDefault(x=>x.name=="WatchAriaPlay"&&Ready(x)));}
                    else DriveManual(em,root);
                    return;
                }
                var brief=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();if(brief!=null&&Ready(brief.DeployOperationButton)){Shot("briefing");Tap(brief.DeployOperationButton);return;}
                var campaign=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();
                if(campaign!=null&&UiShellRuntimeGateway.TryReadCampaignOperations(out var model))
                {
                    if(model.SelectedMission.MissionId!=CampaignMissionSequence.NetworkCollapse)
                    {if(!campaign.IsChapterFive){Tap(campaign.ChapterFiveButton);return;}if(campaign.MissionNodeButtons.Length>2)Tap(campaign.MissionNodeButtons[2]);return;}
                    Shot("campaign");Tap(campaign.LaunchMissionButton);return;
                }
                var home=UnityEngine.Object.FindAnyObjectByType<MainMenuCampaignCardView>();Tap(Field<Button>(home,"continueButton"));
            }
            catch(Exception exception){Debug.LogException(exception);Complete(false,exception.Message);}
        }
        private static void DriveManual(EntityManager em,Entity root)
        {
            var drawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();if(drawer!=null&&drawer.IsOpen&&Ready(drawer.CloseButton)){Tap(drawer.CloseButton);return;}
            if(!UiShellRuntimeGateway.TryReadMissionTutorialTarget(out var target))return;
            var guide=UnityEngine.Object.FindAnyObjectByType<AriaTutorialBriefingView>();if(guide==null||!guide.IsPresentationVisible)return;
            int step=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).GuidanceId-70000;
            if(step!=lastStep){lastStep=step;focusStep=-1;Shot("hud-step-"+step);}
            if(Ready(guide.ContinueButton)){Tap(guide.ContinueButton);return;}
            if(target.Moving||target.ExecutingAttack||target.BattleAction==UiTutorialBattleAction.Watch)return;
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();var camera=Camera.main;
            if(controls==null||camera==null||!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var state))return;
            var mode=target.NeedsSelection?TacticalCommandMode.Select:target.BattleAction==UiTutorialBattleAction.Attack?TacticalCommandMode.Attack:step==8&&em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).RecommendationKind==AssistantRecommendationKind.Logistics?TacticalCommandMode.Board:TacticalCommandMode.Move;
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
                {lastInput=EditorApplication.timeSinceStartup;Debug.Log("[NetworkCollapseInput] normalTouch=dragSelection step="+step);}
            }
            else Gesture(from,"step-"+step+"-"+mode);
        }
        private static CampaignMissionRewardGrant[] PrerequisiteRewards(MissionDefinitionConfig definition)
        {
            var source=definition.FirstClearRewards;var grants=new CampaignMissionRewardGrant[source.Length];
            for(int i=0;i<source.Length;i++)grants[i]=new CampaignMissionRewardGrant(source[i].Kind,source[i].RewardConfigId,source[i].Amount);return grants;
        }
        private static bool TapNamed(string name)=>Tap(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).FirstOrDefault(x=>x.name==name&&Ready(x)));
        private static bool TapUnder<T>(string name) where T:Component
        {var owner=UnityEngine.Object.FindAnyObjectByType<T>();return owner!=null&&Tap(owner.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.name==name&&Ready(x)));}
        private static int Materials(EntityManager em)
        {using var q=em.CreateEntityQuery(typeof(FactionTacticalMaterialsComponent));using var entities=q.ToEntityArray(Unity.Collections.Allocator.Temp);foreach(var e in entities){var m=em.GetComponentData<FactionTacticalMaterialsComponent>(e);if(m.FactionId==1)return m.Current;}throw new InvalidOperationException("Actual tactical Materials owner missing");}
        private static bool TouchWorld(Vector3 position,string label)
        {
            var camera=Camera.main;if(camera==null)return false;var target=camera.WorldToScreenPoint(position);
            if(target.z<=0||!WorldPointUsable(target))
            {var center=new Vector2(Screen.width*.5f,Screen.height*.45f);var direction=center-(Vector2)target;if(target.z<=0)direction=-direction;var delta=Vector2.ClampMagnitude(direction,180);if(touch.TryGesture(center,center+delta,.15f,.5f,Time.unscaledTime))lastInput=EditorApplication.timeSinceStartup;return false;}
            return Gesture(target,label);
        }
        private static void DriveSupport(EntityManager em,Entity root,in CampaignMissionNetworkCollapseState mission)
        {
            if(!UiShellRuntimeGateway.TryReadSupport(out var model))return;
            if(supportStep==0)
            {
                if(UiShellRuntimeGateway.ReadAriaPlay().Active){if(TapNamed("WatchAriaPlay"))watchStarted=false;return;}
                if(!em.HasComponent<SelectedUnitTag>(mission.Engineer))
                {
                    var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                    if(!UiShellRuntimeGateway.TryReadMatchHudCommandState(out var mode))return;
                    if(mode.ActiveCommandMode!=TacticalCommandMode.Select){Tap(controls?.SelectButton);return;}
                    TouchWorld((Vector3)em.GetComponentData<LocalTransform>(mission.Engineer).Position+Vector3.up,"supply-select-original-engineer");return;
                }
                if(TapNamed("SupportCommand")){supportCharges=model.Supply.Charges;fuelBefore=model.Fuel;materialsBefore=Materials(em);supportStep=1;watchStarted=false;}return;
            }
            if(supportStep==1){if(TapUnder<SupportPopupView>("SupportCard4"))supportStep=2;return;}
            if(supportStep==2){if(TapUnder<SupportPopupView>("BeginTargeting")){Shot("support-supply");supportStep=3;}return;}
            if(supportStep==3)
            {if(model.Phase!=UiSupportPhase.Targeting)return;if(TouchWorld(new Vector3(1006,0,330),"supply-safe-rear-ground"))supportStep=4;return;}
            if(supportStep==4)
            {if(model.Phase!=UiSupportPhase.Preview)return;if(!model.Valid)throw new InvalidOperationException("Native Supply preview rejected: "+model.ReasonKey);if(TapUnder<SupportTargetingInputUiSystemHelper>("Propose")){Shot("support-proposal");supportStep=5;}return;}
            if(supportStep==5)
            {if(!model.HasProposal)return;Shot("support-consent");if(TapUnder<SupportTargetingInputUiSystemHelper>(SupportDecision=="decline"?"Decline":"Approve"))supportStep=6;return;}
            if(supportStep==6)
            {
                if(SupportDecision=="decline")
                {
                    if(model.HasProposal)return;if(model.Supply.Charges!=supportCharges||Materials(em)!=materialsBefore)throw new InvalidOperationException("Decline consumed Supply or granted Materials");
                    if(em.HasBuffer<SupportReceiptElement>(root))foreach(var r in em.GetBuffer<SupportReceiptElement>(root,true))if(r.Kind==SupportAbilityKind.Supply&&r.EffectApplied!=0)throw new InvalidOperationException("Decline executed Supply");
                    if(TapUnder<SupportTargetingInputUiSystemHelper>("Cancel"))FinishSupport();return;
                }
                if(!em.HasBuffer<SupportReceiptElement>(root))return;
                int accepted=0;foreach(var r in em.GetBuffer<SupportReceiptElement>(root,true))if(r.Kind==SupportAbilityKind.Supply&&r.EffectApplied!=0)
                {if(r.Source!=SupportRequestSource.Aria||r.Reason!=SupportRejectionReason.None||r.SpentFuel!=3)throw new InvalidOperationException("Bounded Supply approval receipt invalid");accepted++;}
                if(accepted==0)return;if(accepted!=1||model.Supply.Charges!=supportCharges-1)throw new InvalidOperationException("One-action Supply allowance violated");
                if(!model.SupplyReady)return;if(model.SupplyStock!=40)throw new InvalidOperationException("Supply crate stock differs from approved forty Materials");
                Shot("support-crate-landed");if(TapNamed("SupportCommand"))supportStep=7;return;
            }
            if(supportStep==7){if(TapUnder<SupportPopupView>("SupportCard4"))supportStep=8;return;}
            if(supportStep==8){if(TapUnder<SupportPopupView>("BeginTargeting"))supportStep=9;return;}
            if(supportStep==9)
            {
                if(model.Phase!=UiSupportPhase.Collecting)return;
                if(!em.HasComponent<SupportSupplyReadModelComponent>(root))return;var supply=em.GetComponentData<SupportSupplyReadModelComponent>(root);
                if(!em.Exists(supply.Crate))throw new InvalidOperationException("Landed actual Supply crate missing");
                if(TouchWorld(em.GetComponentData<SupportSupplyCrateComponent>(supply.Crate).LandingPosition,"supply-collect-actual-crate"))supportStep=10;return;
            }
            if(supportStep==10)
            {
                if(!string.IsNullOrEmpty(model.CollectionReasonKey)&&model.CollectionReasonKey!="support.reason.0")throw new InvalidOperationException("Supply collection rejected: "+model.CollectionReasonKey);
                if(Materials(em)<materialsBefore+40)return;
                if(Materials(em)!=materialsBefore+40||model.SupplyStock!=0||model.Fuel>fuelBefore-3+.01f)throw new InvalidOperationException("Supply stock/actual forty Materials/real Fuel receipt mismatch");
                Shot("support-collected");FinishSupport();
            }
        }
        private static void FinishSupport()
        {
            supportChecked=true;supportStep=11;watchStarted=false;Shot("support-"+SupportDecision+"-complete");
            Debug.Log("[NetworkSupplyInput] result=Passed decision="+SupportDecision+" normalTouch=Propose,decision"+(SupportDecision=="approve"?",collect actualMaterials=40 crate=Empty":" effect=none"));
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
            from=to=default;drag=target.NeedsSelection&&target.DragSelection;
            if(drag)
            {
                from=new Vector2(float.MaxValue,float.MaxValue);to=new Vector2(float.MinValue,float.MinValue);
                for(int i=0;i<8;i++)
                {
                    var corner=new Vector3((i&1)==0?target.SelectionMin.x:target.SelectionMax.x,
                        (i&2)==0?target.SelectionMin.y:target.SelectionMax.y,(i&4)==0?target.SelectionMin.z:target.SelectionMax.z);
                    var screen=camera.WorldToScreenPoint(corner);if(screen.z<=0)return false;
                    from=Vector2.Min(from,screen);to=Vector2.Max(to,screen);
                }
                from-=Vector2.one*8;to+=Vector2.one*8;
                return WorldPointUsable(from)&&WorldPointUsable(to)&&WorldPointUsable((from+to)*.5f);
            }
            var point=camera.WorldToScreenPoint(target.NeedsSelection?target.Selection+Vector3.up:target.Destination+(target.BattleAction==UiTutorialBattleAction.Attack?Vector3.up:Vector3.zero));
            from=to=point;return point.z>0&&WorldPointUsable(from);
        }
        private static bool IsOwnPanel(string name)=>name.StartsWith("CH05M03_",StringComparison.Ordinal);
        private static bool WorldPointUsable(Vector2 point)
        {
            if(Camera.main==null||!Camera.main.pixelRect.Contains(point)||UnityEngine.EventSystems.EventSystem.current==null)return false;
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point},hits);return hits.Count==0;
        }
        private static void SampleMedia()
        {
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
            if(view?.CurrentPanelSprite==null||!view.IsPanelVisible||!IsOwnPanel(view.CurrentPanelSprite.name))return;
            if(view.VoiceSource!=null&&view.VoiceSource.clip!=null)throw new InvalidOperationException("Caption-only mission inherited an unintended voice clip");
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
            Debug.Log("[NetworkCollapseInput] inputFixture=background-game-view-only nativeInputIsolated="+isolated.Count);
        }
        private static void EnsureTouch(){if(touch!=null)return;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();touch=new AriaTouchInputUiSystemHelper();if(!touch.Start())throw new InvalidOperationException("Public touch input unavailable.");}
        private static T Field<T>(object owner,string name) where T:class=>owner?.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner) as T;
        private static bool Visible(object owner,string field){var group=Field<CanvasGroup>(owner,field);return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static bool Ready(Button b)
        {
            if(b==null||!b.IsActive()||!b.IsInteractable())return false;
            var canvas=b.GetComponentInParent<Canvas>();
            if(canvas==null||!canvas.isActiveAndEnabled)return false;
            foreach(var group in b.GetComponentsInParent<CanvasGroup>())
                if(group.alpha<=.01f||!group.blocksRaycasts)return false;
            return true;
        }
        private static bool Tap(Button b)
        {if(!Ready(b))return false;var rect=(RectTransform)b.transform;var canvas=b.GetComponentInParent<Canvas>();var wedge=b.targetGraphic as V3RadialWedgeGraphic;var point=wedge!=null?wedge.GuidanceTouchPoint:rect.TransformPoint(rect.rect.center);var p=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,point);var eventSystem=UnityEngine.EventSystems.EventSystem.current;if(eventSystem==null)return false;
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(new UnityEngine.EventSystems.PointerEventData(eventSystem){position=p},hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)return false;
            return Gesture(p,b.name);}
        private static bool Gesture(Vector2 p,string label)
        {if(!touch.TryGesture(p,p,.18f,0,Time.unscaledTime))return false;lastInput=EditorApplication.timeSinceStartup;Debug.Log("[NetworkCollapseInput] touch="+label);return true;}
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
            string marker="[NetworkCollapseInput] result="+(passed?"Passed":"Failed")+" "+detail;File.WriteAllText(Output+"/result.txt",marker+"\n");Debug.Log(marker);if(!ExistingEditorValidation.IsRunning)AssetDatabase.AllowAutoRefresh();
            Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",null);
            if(SessionState.GetBool(LiveKey,true))EditorApplication.ExitPlaymode();else MissionEditorValidationExit.Complete(passed);
        }
    }
}
