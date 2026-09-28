#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Game.Editor
{
    // Explicit, test-only Smoke grant on an existing real mission. All actions use the normal touch device.
    // This is test-encounter evidence; it does not enable Support in Steel Push production.
    public static class SupportSmokeJourneyProbe
    {
        private static TaskCompletionSource<int> completion;
        private static GameObject updateHost;
        private sealed class NativeInputUpdateHost : MonoBehaviour {private void Update()=>Tick();}
        private static AriaTouchInputUiSystemHelper hand;
        private static CampaignMissionProgressStore store,oldStore;
        private static int stage,smokeStage,observedDamage,lastEvent;
        private static double started,nextAction,firstCommitted,lastState;
        private static bool useSmoke,setup,coverSeen,expirySeen,resumingAfterSmoke,resultPopupInterruption,latePopupRequested,smokeVisualCaptured;
        private static uint dragPreviewVersion;private static Vector3 beforePan;
        private static Entity root,reserve;private static string output,locale,error;
        private static SupportAbilityKind journeyAbility;
        private static bool strikeAria,flightCaptured;
        private static Entity strikeFlight,landedSoldier;private static int strikeHits;private static float3 landedStart;private static Entity supplyCrate;private static int supplyMaterialsBefore,supplyRewardedBefore,landingCandidate,nativeZoomTaps;
        private static InputSettings.EditorInputBehaviorInPlayMode oldInput;
        private static InputSettings.BackgroundBehavior oldBackground;
        private static string oldValidation;private static bool oldRunBackground;
        public static Task<int> EnterPlay()
        {
            if(EditorApplication.isPlaying) return Task.FromResult(0);
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Game/Scenes/Menu.unity")
                throw new InvalidOperationException("Open Menu scene in the existing project before this journey.");
            EditorApplication.EnterPlaymode();
            Debug.Log("[SupportSmokeJourney] enteringPlay=true acceptance=pending");
            // Only acknowledge the transition here. The normal-input journey has
            // its own async receipt after Play's domain reload is complete.
            return Task.FromResult(0);
        }
        public static Task<int> RunEnglish()=>Begin("en",true);
        public static Task<int> RunPersian()=>Begin("fa-IR",true);
        public static Task<int> RunWithoutSupport()=>Begin("en",false);
        public static Task<int> RunStrikeAria()=>Begin("en",true,SupportAbilityKind.Strike,true);
        public static Task<int> RunStrikePlayer()=>Begin("en",true,SupportAbilityKind.Strike,false);
        public static Task<int> RunParatroopers()=>Begin("en",true,SupportAbilityKind.Paratroopers,false);
        public static Task<int> RunSupply()=>Begin("en",true,SupportAbilityKind.Supply,false);
        private static Task<int> Begin(string language,bool smoke,SupportAbilityKind kind=SupportAbilityKind.Smoke,bool aria=true)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Journey must start in the existing playing Editor.");
            if(completion!=null)throw new InvalidOperationException("A Support journey is already running.");
            completion=new TaskCompletionSource<int>();locale=language;useSmoke=smoke;stage=smokeStage=observedDamage=lastEvent=0;
            setup=coverSeen=expirySeen=resumingAfterSmoke=resultPopupInterruption=latePopupRequested=smokeVisualCaptured=false;firstCommitted=nextAction=lastState=0;error=null;started=EditorApplication.timeSinceStartup;
            journeyAbility=kind;strikeAria=aria;flightCaptured=false;strikeHits=0;strikeFlight=landedSoldier=supplyCrate=Entity.Null;landingCandidate=nativeZoomTaps=0;
            output="Design/AgentReports/SupportSystem/Evidence/SmokeJourney/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+language+(smoke?"-"+kind.ToString().ToLowerInvariant()+(kind==SupportAbilityKind.Strike?(aria?"-aria":"-player"):""):"-without-support");Directory.CreateDirectory(output);
            oldValidation=Environment.GetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION");Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION","1");
            oldRunBackground=Application.runInBackground;Application.runInBackground=true;
            oldInput=InputSystem.settings.editorInputBehaviorInPlayMode;oldBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            MainMenuV3PrefabBuilder.SetGameViewResolution(language=="fa-IR"?2400:1920,1080);GameLocalization.SetLocale(language,false);
            hand=new AriaTouchInputUiSystemHelper();if(!hand.Start()){var task=completion.Task;Complete(false,"Normal touch actuator could not start.");return task;}
            AriaTouchInputUiSystemHelper.Evidence+=TouchEvidence;Application.logMessageReceived+=Observe;
            updateHost=new GameObject("SupportNativeInputAcceptance",typeof(NativeInputUpdateHost)){hideFlags=HideFlags.DontSave};
            Debug.Log("[SupportSmokeJourney] started locale="+language+" testGrant="+smoke+" normalInput=touch save=isolated");return completion.Task;
        }
        private static void Tick()
        {
            if(completion==null)return;
            try
            {
                if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode ended before acceptance.");
                if(error!=null)throw new InvalidOperationException(error);
                if(EditorApplication.timeSinceStartup-started>650)throw new TimeoutException("Journey stage="+stage+" smokeStage="+smokeStage);
                hand?.Tick(Time.unscaledTime);
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;var em=world.EntityManager;
                using var q=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(q.CalculateEntityCount()!=1)return;root=q.GetSingletonEntity();
                if(stage==0)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    Debug.Log($"[SupportNativeInputContext] loop=player-update screen={Screen.width}x{Screen.height}");
                    var reference=em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root);oldStore=reference.Store;
                    store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository("/private/tmp/support-journey-save-"+Guid.NewGuid().ToString("N"))));
                    for(int i=0;i<16;i++)store.EnsureAvailable(CampaignMissionSequence.IdAt(i));
                    store.Settle(CampaignMissionSequence.AirCorridor,"prior-air-clear",1,true,3,120000,string.Empty);reference.Store=store;
                    UiShellRuntimeGateway.TryEnqueueRouteRequest(UiShellRouteIntent.OpenMenuRoute,UIRoute.Campaign,true);stage=1;return;
                }
                if(stage==1)
                {
                    var view=UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>();if(view==null)return;
                    if(!view.IsChapterFour){if(Tap(view.ChapterFourButton))return;}
                    var selected=UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign)&&campaign.SelectedMission.MissionId==CampaignMissionSequence.SteelPush;
                    if(!selected){if(view.MissionNodeButtons?.Length>1)Tap(view.MissionNodeButtons[1]);return;}
                    if(Tap(view.LaunchMissionButton)){stage=2;Shot("campaign");}return;
                }
                if(stage==2)
                {
                    var view=UnityEngine.Object.FindAnyObjectByType<MissionBriefingScreenView>();if(view==null)return;
                    if(Tap(view.DeployOperationButton)){stage=3;Shot("briefing");}return;
                }
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                if(EditorApplication.timeSinceStartup-lastState>5)
                {lastState=EditorApplication.timeSinceStartup;Debug.Log($"[SupportJourneyState] stage={stage} smokeStage={smokeStage} phase={runtime.Phase} outcome={runtime.Outcome} gestures={hand?.CompletedGestures} covered={coverSeen} damage={observedDamage}");}
                if(useSmoke && setup && journeyAbility==SupportAbilityKind.Smoke)ObserveNativeSmoke(em);
                var narrative=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>();
                if(narrative!=null&&journeyAbility==SupportAbilityKind.Strike&&!UiShellRuntimeGateway.ReadAriaPlay().Active)
                {
                    if(hand==null){hand=new AriaTouchInputUiSystemHelper();if(!hand.Start())return;}
                    if(!hand.IsRunning&&!hand.Start())return;
                    var confirmation=narrative.SkipConfirmationView;
                    var yes=confirmation==null?null:typeof(NarrativeSkipConfirmationView).GetField("confirmButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(confirmation)as Button;
                    if(yes!=null&&yes.IsActive()&&yes.IsInteractable()&&confirmation.GetComponent<CanvasGroup>().alpha>0){Tap(yes);return;}
                    var controls=narrative.PlaybackControlsView;
                    var skip=controls==null?null:typeof(NarrativePlaybackControlsView).GetField("skipButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(controls)as Button;
                    if(skip!=null&&skip.IsActive()&&skip.IsInteractable()&&narrative.DialogueView!=null)
                    {if(Tap(skip))return;}
                }
                if(narrative?.DialogueView?.Phase==NarrativeDialoguePhase.AdvanceReady)
                {
                    if(UiShellRuntimeGateway.ReadAriaPlay().Active)return;
                    if(hand==null){hand=new AriaTouchInputUiSystemHelper();if(!hand.Start())return;}
                    if(!hand.IsRunning && !hand.Start())return;
                    var button=typeof(NarrativeDialogueView).GetField("inputButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(narrative.DialogueView)as Button;
                    Tap(button);return;
                }
                if(stage==3&&useSmoke&&journeyAbility==SupportAbilityKind.Strike&&runtime.Outcome==MissionOutcomeKind.Victory)throw new InvalidOperationException("Mission finished before native Strike acceptance.");
                if(runtime.Outcome==MissionOutcomeKind.Defeat)throw new InvalidOperationException("Normal-input encounter ended in defeat.");
                if(stage==3)
                {
                    if(runtime.Phase==MissionPhaseKind.FindSquad)
                    {if(hand!=null&&!hand.IsBusy&&EditorApplication.timeSinceStartup>=nextAction&&!TapNamed("ContinueLessonButton"))TargetTap(em);return;}
                    if(runtime.Phase!=MissionPhaseKind.Engage)return;
                    if(useSmoke && !setup){InstallEncounter(em,runtime);return;}
                    if(useSmoke && !SmokeJourney(em))return;
                    if(!useSmoke)
                    {
                        using var zones=em.CreateEntityQuery(typeof(SupportSmokeZoneComponent));if(zones.CalculateEntityCount()!=0)throw new InvalidOperationException("No-Support run contains Smoke.");
                        if(em.HasComponent<SupportSessionComponent>(root)&&em.GetComponentData<SupportSessionComponent>(root).TestEncounter!=0)throw new InvalidOperationException("No-Support run inherited a test grant.");
                    }
                    hand.Dispose();hand=null;stage=4;nextAction=0;Shot("hud-before-aria");return;
                }
                if(stage==4)
                {
                    if(runtime.Outcome==MissionOutcomeKind.Victory){stage=5;Shot("result");return;}
                    if(resultPopupInterruption)return;
                    if(latePopupRequested)
                    {
                        if(UnityEngine.Object.FindAnyObjectByType<SupportPopupView>()!=null){resultPopupInterruption=true;Shot("support-before-result");Debug.Log("[SupportNativeInterruption] normalPlayerTakeover=true actualPopupVisible=true preparationComplete=true");return;}
                        if(hand.IsBusy)return;if(!hand.IsRunning&&!hand.Start())return;TapNamed("SupportCommand");return;
                    }
                    var aria=UiShellRuntimeGateway.ReadAriaPlay();if(aria.Phase==AriaPlayPhase.Blocked)throw new InvalidOperationException("ARIA normal input blocked.");
                    if(useSmoke&&locale=="en"&&aria.Phase==AriaPlayPhase.Waiting&&aria.Actions>0&&!aria.Pressed&&
                        (em.GetComponentData<CampaignMissionDefenseStateComponent>(root).AcknowledgedGuidanceMask&0x1FFu)==0x1FFu&&
                        em.GetComponentData<CampaignMissionAttemptFactsComponent>(root).ElapsedMilliseconds>=80000)
                    {
                        if(hand==null){hand=new AriaTouchInputUiSystemHelper();if(!hand.Start())return;}
                        if(TapNamed("SupportCommand")){latePopupRequested=true;resumingAfterSmoke=true;}
                        return;
                    }
                    if(aria.Active){resumingAfterSmoke=false;return;}
                    if(aria.Actions>0&&!resumingAfterSmoke)throw new InvalidOperationException("ARIA stopped before normal victory.");
                    if(hand==null){hand=new AriaTouchInputUiSystemHelper();if(!hand.Start())return;}
                    if(TapNamed("ContinueLessonButton"))return;
                    if(TapNamed("ConfirmWatchAria")||TapNamed("WatchAriaPlay")){stage=41;}return;
                }
                if(stage==41){if(hand.IsBusy)return;hand.Dispose();hand=null;stage=4;return;}
                if(stage==5)
                {
                    if(!UiShellRuntimeGateway.TryReadMissionResult(out var result)||!result.PrimaryActionEnabled)return;
                    if(UnityEngine.Object.FindAnyObjectByType<SupportPopupView>()!=null)throw new InvalidOperationException("Support overlay survived the mission result interruption.");
                    if(result.SettlementFailed)throw new InvalidOperationException("Mission settlement failed.");
                    if(hand==null){hand=new AriaTouchInputUiSystemHelper();if(!hand.Start())return;}
                    var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();var button=view==null?null:typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view)as Button;
                    if(Tap(button)){stage=6;nextAction=EditorApplication.timeSinceStartup+3;}return;
                }
                if(stage==6 && EditorApplication.timeSinceStartup>=nextAction && !hand.IsBusy)
                {
                    if(UnityEngine.Object.FindAnyObjectByType<CampaignOperationsScreenView>()==null)return;
                    if(!store.ReadAll().Single(x=>x.missionId==CampaignMissionSequence.SteelPush).firstClearRewardSettled)throw new InvalidOperationException("No real first-clear settlement.");
                    if(useSmoke && journeyAbility==SupportAbilityKind.Smoke && observedDamage==0)throw new InvalidOperationException("No actual combat damage was observed under native Smoke; automated damage tests alone cannot satisfy this journey.");
                    if(useSmoke && journeyAbility==SupportAbilityKind.Strike && (strikeHits!=1||!flightCaptured))throw new InvalidOperationException("Missing native Strike aircraft or exactly one impact.");
                    var save=(SaveService)typeof(CampaignMissionProgressStore).GetField("_saveService",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(store);
                    if(!save.LoadProfile().ownedSupportAbilityUnlocks.Contains("ability.smoke_screen"))throw new InvalidOperationException("Actual first-clear result did not grant Smoke ownership.");
                    Debug.Log("[SupportNativeCampaign] firstClear=settled smokeUnlock=owned");
                    Shot("return");Complete(true,$"locale={locale} smoke={useSmoke&&journeyAbility==SupportAbilityKind.Smoke} ability={journeyAbility} ARIA=normal-input victory=result=settlement=return covered={coverSeen} liveDamageEvents={observedDamage} expiry={expirySeen} strikeHits={strikeHits} aircraft={flightCaptured}");
                }
            }
            catch(Exception ex){Debug.LogException(ex);Complete(false,ex.Message);}
        }
        private static void InstallEncounter(EntityManager em,CampaignMissionRuntimeComponent runtime)
        {
            if(!em.HasComponent<SupportCatalogComponent>(root))throw new InvalidOperationException("Support catalog not installed in shipped shell.");
            if(!em.HasComponent<CampaignMissionSteelPushState>(root))return;var steel=em.GetComponentData<CampaignMissionSteelPushState>(root);if(steel.Initialized==0)return;
            reserve=steel.FuelReserve;if(!em.Exists(reserve))throw new InvalidOperationException("Authored physical reserve missing.");
            var session=em.GetComponentData<SupportSessionComponent>(root);session.TestEncounter=1;em.SetComponentData(root,session);
            byte mask=SupportTargetValidationUtilitySystemHelper.Mask(journeyAbility);
            var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);policy.TestGrantMask=policy.AllowedMask=mask;policy.PopulationCeiling=64;
            using var grids=em.CreateEntityQuery(typeof(GridConfig));if(grids.CalculateEntityCount()!=1)throw new InvalidOperationException("Explicit test ground grid missing.");var grid=grids.GetSingleton<GridConfig>();
            policy.GroundMin=grid.Origin.xz;policy.GroundMax=grid.Origin.xz+new float2(grid.Width,grid.Height)*grid.CellSize;em.SetComponentData(root,policy);
            var regions=em.GetBuffer<SupportGroundRegionElement>(root);regions.Clear();regions.Add(new SupportGroundRegionElement {Min=policy.GroundMin,Max=policy.GroundMax,Visible=1,Version=1});
            var abilities=em.GetBuffer<SupportAbilityStateElement>(root);for(int i=0;i<abilities.Length;i++){var a=abilities[i];a.Enabled=(byte)(a.Kind==journeyAbility?1:0);abilities[i]=a;}
            if(journeyAbility!=SupportAbilityKind.Smoke)
            {
                using var metadataQuery=em.CreateEntityQuery(typeof(OperationMapMetadataComponent));
                var metadata=metadataQuery.GetSingleton<OperationMapMetadataComponent>();float3 center=default;bool found=false;
                ref var anchors=ref metadata.Blob.Value.Anchors;
                for(int i=0;i<anchors.Length;i++)if(anchors[i].Id.Equals(new FixedString64Bytes(CH04M02SteelPushConfigBuilder.Prefix+"main_spawn"))){center=anchors[i].Position;found=true;break;}
                if(!found)throw new InvalidOperationException("Authored main_spawn anchor missing for test aircraft route.");
                em.AddComponentData(root,new SupportAirRouteComponent {SessionToken=runtime.SessionToken,AttemptOrdinal=runtime.AttemptOrdinal,Authored=1,Version=1,Clearance=5,
                    Entry=center+new float3(-45,16,-15),Release=center+new float3(0,16,-15),Exit=center+new float3(45,16,-15)});
            }
            em.AddComponentData(root,new SupportFuelScopeComponent {Required=1,Storage=reserve});setup=true;nextAction=EditorApplication.timeSinceStartup+2;
            Debug.Log($"[SupportTestEncounter] explicitGrant={journeyAbility} ground={policy.GroundMin}..{policy.GroundMax} reserve={reserve} productionEnabled=false attempt={runtime.AttemptOrdinal}");
        }
        private static bool SmokeJourney(EntityManager em)
        {
            if(journeyAbility==SupportAbilityKind.Strike && smokeStage==0){hand.Dispose();hand=null;smokeStage=110;}
            // Use the remaining charge in actual combat, rather than exhausting both during preparation.
            if(smokeStage==110)
            {
                if(hand==null){hand=new AriaTouchInputUiSystemHelper();if(!hand.Start())return false;}
                if(TapNamed("ContinueLessonButton"))return false;
                if(TapNamed("ConfirmWatchAria"))smokeStage=111;
                else TapNamed("WatchAriaPlay");
                return false;
            }
            if(smokeStage==111){if(hand.IsBusy)return false;hand.Dispose();hand=null;smokeStage=112;return false;}
            if(smokeStage==112)
            {
                var aria=UiShellRuntimeGateway.ReadAriaPlay();
                if(aria.Phase==AriaPlayPhase.Blocked)throw new InvalidOperationException("ARIA blocked before combat Smoke.");
                if(!aria.Active || aria.Pressed || !(journeyAbility==SupportAbilityKind.Strike?HasKnownStrikeTarget(em):HasFriendlyCombatDamage(em)))return false;
                if(hand==null){hand=new AriaTouchInputUiSystemHelper();if(!hand.Start())return false;}
                if(TapNamed("WatchAriaPlay"))smokeStage=113;
                return false;
            }
            if(smokeStage==113)
            {
                if(hand.IsBusy || UiShellRuntimeGateway.ReadAriaPlay().Active)return false;
                if(TapNamed("SupportCommand")){resumingAfterSmoke=true;smokeStage=12;}
                return false;
            }
            if(hand==null || hand.IsBusy || EditorApplication.timeSinceStartup<nextAction)return false;
            if(journeyAbility==SupportAbilityKind.Strike)return StrikeJourney(em);
            if(journeyAbility==SupportAbilityKind.Paratroopers)return ParatrooperJourney(em);
            if(journeyAbility==SupportAbilityKind.Supply)return SupplyJourney(em);
            if(!UiShellRuntimeGateway.TryReadSupport(out var model))throw new InvalidOperationException("Support read model missing.");
            using var zones=em.CreateEntityQuery(typeof(SupportSmokeZoneComponent));
            if(smokeStage==0){if(TapNamed("SupportCommand")){smokeStage=1;}return false;}
            if(smokeStage==1){if(UnityEngine.Object.FindAnyObjectByType<SupportPopupView>()==null)return false;Shot("support-popup");if(TapUnder<SupportPopupView>("Stop")){smokeStage=18;}return false;}
            if(smokeStage==18){RequireNoSpend(em);if(UiShellRuntimeGateway.ReadAriaPlay().Active)throw new InvalidOperationException("Header Stop did not restore player control.");if(TapUnder<SupportPopupView>("CloseButton")){Debug.Log("[SupportPopupNormalInput] headerStop=touch close=touch spend=0");smokeStage=2;}return false;}
            if(smokeStage==2){RequireNoSpend(em);if(UnityEngine.Object.FindAnyObjectByType<SupportPopupView>()!=null)throw new InvalidOperationException("Closed Support popup survived its hide transition.");if(TapNamed("SupportCommand"))smokeStage=3;return false;}
            if(smokeStage==3){if(TapUnder<SupportPopupView>("BeginTargeting"))smokeStage=4;return false;}
            if(smokeStage==4){if(model.Phase!=UiSupportPhase.Targeting)return false;if(TargetTap(em))smokeStage=5;return false;}
            if(smokeStage==5)
            {
                if(model.Phase!=UiSupportPhase.Preview||!model.Valid)return false;RequireNoSpend(em);Shot("target-preview");
                dragPreviewVersion=model.Version;beforePan=Camera.main.transform.position;
                var point=new Vector2(Screen.width*.5f,Screen.height*.55f);
                if(hand.TryGesture(point,point+new Vector2(90,0),.12f,.35f,Time.unscaledTime)){nextAction=EditorApplication.timeSinceStartup+1;smokeStage=51;}return false;
            }
            if(smokeStage==51)
            {
                RequireNoSpend(em);if(model.Version!=dragPreviewVersion)throw new InvalidOperationException("Camera drag replaced the confirmed target candidate.");
                if((Camera.main.transform.position-beforePan).sqrMagnitude<.001f)throw new InvalidOperationException("Normal camera drag did not pan during Support targeting.");
                if(TapUnder<SupportTargetingInputUiSystemHelper>("Propose"))smokeStage=6;return false;
            }
            if(smokeStage==6){if(!model.HasProposal)return false;RequireNoSpend(em);Shot("consent");if(TapUnder<SupportTargetingInputUiSystemHelper>("ShowTarget"))smokeStage=7;return false;}
            if(smokeStage==7){RequireNoSpend(em);if(TapUnder<SupportTargetingInputUiSystemHelper>("Decline"))smokeStage=8;return false;}
            if(smokeStage==8){if(model.HasProposal)return false;RequireNoSpend(em);if(TargetTap(em))smokeStage=81;return false;}
            if(smokeStage==81){if(model.Phase!=UiSupportPhase.Preview||!model.Valid)return false;RequireNoSpend(em);if(TapUnder<SupportTargetingInputUiSystemHelper>("Propose"))smokeStage=9;return false;}
            if(smokeStage==9){if(!model.HasProposal)return false;if(TapUnder<SupportTargetingInputUiSystemHelper>("Approve"))smokeStage=10;return false;}
            if(smokeStage==10)
            {
                var receipts=em.GetBuffer<SupportReceiptElement>(root);if(receipts.Length==0)return false;var receipt=receipts[receipts.Length-1];
                if(receipt.Reason!=SupportRejectionReason.None||receipt.SpentFuel!=1||receipt.Source!=SupportRequestSource.Aria)throw new InvalidOperationException("Approved Smoke did not commit exactly once: "+receipt.Reason);
                if(model.Smoke.Charges!=1||zones.CalculateEntityCount()!=1)throw new InvalidOperationException("First Smoke charge/effect mismatch.");
                firstCommitted=em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds;Shot("smoke-live");smokeStage=11;return false;
            }
            if(smokeStage==11)
            {
                var elapsed=em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds-firstCommitted;
                if(elapsed>=5 && elapsed<15 && !smokeVisualCaptured){Shot("smoke-established");smokeVisualCaptured=true;}
                if(elapsed>=16 && zones.CalculateEntityCount()==0)expirySeen=true;
                if(elapsed<36)return false;if(!expirySeen||!coverSeen)throw new InvalidOperationException("No native cover or expiry observed.");
                hand.Dispose();hand=null;smokeStage=110;return false;
            }
            if(smokeStage==12){if(TapUnder<SupportPopupView>("BeginTargeting"))smokeStage=13;return false;}
            if(smokeStage==13){if(model.Phase==UiSupportPhase.Targeting && TargetTap(em))smokeStage=14;return false;}
            if(smokeStage==14){if(model.Phase==UiSupportPhase.Preview&&model.Valid&&TapUnder<SupportTargetingInputUiSystemHelper>("Confirm"))smokeStage=15;return false;}
            if(smokeStage==15)
            {
                if(model.Smoke.Charges!=0)return false;var receipts=em.GetBuffer<SupportReceiptElement>(root);
                if(receipts.Length!=2||receipts[1].SpentFuel!=1||receipts[1].Source!=SupportRequestSource.Player)throw new InvalidOperationException("Player Smoke receipt mismatch.");
                Shot("smoke-combat");Debug.Log("[SupportNormalSmoke] commits=2 fuel=2 charges=0 openClosePreviewShowDecline=no-spend cooldown=real-time");
                if(TapNamed("SupportCommand"))smokeStage=16;return false;
            }
            if(smokeStage==16){if(UnityEngine.Object.FindAnyObjectByType<SupportPopupView>()==null){if(model.Active)TapNamed("SupportCommand");return false;}if(model.Smoke.Available)throw new InvalidOperationException("Exhausted Smoke is enabled.");Shot("exhausted");if(TapUnder<SupportPopupView>("CloseButton"))smokeStage=17;return false;}
            return smokeStage==17;
        }
        private static bool HasKnownStrikeTarget(EntityManager em)
        {
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
            {
                var e=member.Entity;
                if(member.FactionId!=2||!em.Exists(e)||!em.HasComponent<SupportTargetEligibilityComponent>(e)||!em.HasComponent<UnitHealth>(e))continue;
                var k=em.GetComponentData<SupportTargetEligibilityComponent>(e);
                if(k.CurrentlyVisible!=0&&k.HostileConfirmed!=0&&em.GetComponentData<UnitHealth>(e).Current>600)return true;
            }
            return false;
        }
        private static bool StrikeJourney(EntityManager em)
        {
            if(!UiShellRuntimeGateway.TryReadSupport(out var model))return false;
            if(smokeStage==12){if(TapUnder<SupportPopupView>("SupportCard2"))smokeStage=200;return false;}
            if(smokeStage==200){if(TapUnder<SupportPopupView>("BeginTargeting"))smokeStage=201;return false;}
            if(smokeStage==201||smokeStage==205)
            {
                Entity candidate=Entity.Null;Vector3 candidatePoint=default;int bestHealth=0;
                foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
                {
                    var entity=member.Entity;
                    if(member.FactionId!=2||!em.Exists(entity)||!em.HasComponent<SupportTargetEligibilityComponent>(entity)||
                       !em.HasComponent<UnitHealth>(entity))continue;
                    int health=em.GetComponentData<UnitHealth>(entity).Current;
                    var known=em.GetComponentData<SupportTargetEligibilityComponent>(entity);
                    if(health<=250||health<=bestHealth||known.CurrentlyVisible==0||known.HostileConfirmed==0)continue;
                    candidate=entity;bestHealth=health;var transform=em.GetComponentData<LocalTransform>(entity);var click=transform.Position;
                    if(em.HasComponent<UnitSelectionHitbox>(entity))click=math.transform(transform.ToMatrix(),em.GetComponentData<UnitSelectionHitbox>(entity).Center);
                    candidatePoint=Camera.main.WorldToScreenPoint(click);
                }
                if(candidate!=Entity.Null)
                {
                    var hits=new System.Collections.Generic.List<RaycastResult>();
                    EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=candidatePoint},hits);
                    if(candidatePoint.z>0&&candidatePoint.x>200&&candidatePoint.x<Screen.width-200&&candidatePoint.y>280&&candidatePoint.y<Screen.height-260&&hits.Count==0)
                    {if(Gesture(candidatePoint,candidatePoint))smokeStage=smokeStage==201?202:206;}
                    else
                    {
                        var center=new Vector2(Screen.width*.5f,Screen.height*.55f);
                        var delta=Vector2.ClampMagnitude(center-(Vector2)candidatePoint,360);
                        if(hand.TryGesture(center,center+delta,.12f,.4f,Time.unscaledTime))nextAction=EditorApplication.timeSinceStartup+.8;
                    }
                }
                return false;
            }
            if(smokeStage is 202 or 203 or 204 or 206 or 207)
            {
                if(em.GetBuffer<SupportReceiptElement>(root).Length!=0||model.Strike.Charges!=1)throw new InvalidOperationException("Strike preview/consent spent before confirmation.");
                if(smokeStage==202)
                {
                    if(model.Phase!=UiSupportPhase.Preview){if(model.Phase==UiSupportPhase.Targeting)smokeStage=201;return false;}
                    if(!model.Valid){Debug.Log($"[SupportNativeStrikePreview] reason={model.ReasonKey} target={model.TargetName}");smokeStage=201;return false;}Shot("strike-preview");
                    if(TapUnder<SupportTargetingInputUiSystemHelper>(strikeAria?"Propose":"Confirm"))smokeStage=strikeAria?203:209;return false;
                }
                if(smokeStage==203)
                {
                    if(!model.HasProposal)return false;
                    var caption=UnityEngine.Object.FindAnyObjectByType<SupportTargetingInputUiSystemHelper>()?.GetComponentsInChildren<TMPro.TMP_Text>().FirstOrDefault(x=>x.name=="Status");
                    if(caption==null||!caption.text.Contains("4")||!caption.text.Contains("PRECISION STRIKE"))throw new InvalidOperationException("Native Strike consent must show Precision Strike and four Fuel.");
                    Shot("strike-consent");if(TapUnder<SupportTargetingInputUiSystemHelper>("ShowTarget"))smokeStage=204;return false;
                }
                if(smokeStage==204){if(TapUnder<SupportTargetingInputUiSystemHelper>("Decline"))smokeStage=205;return false;}
                if(smokeStage==206){if(model.Valid&&TapUnder<SupportTargetingInputUiSystemHelper>("Propose"))smokeStage=207;return false;}
                if(smokeStage==207){if(model.HasProposal&&TapUnder<SupportTargetingInputUiSystemHelper>("Approve"))smokeStage=209;return false;}
            }
            if(smokeStage==209)
            {
                var receipts=em.GetBuffer<SupportReceiptElement>(root);if(receipts.Length==0)return false;var receipt=receipts[0];
                if(receipt.Reason!=SupportRejectionReason.None||receipt.Source!=(strikeAria?SupportRequestSource.Aria:SupportRequestSource.Player))throw new InvalidOperationException("Native Strike rejected: "+receipt.Reason);
                strikeFlight=receipt.Effect;firstCommitted=em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds;smokeStage=210;
            }
            if(smokeStage==210)
            {
                var receipt=em.GetBuffer<SupportReceiptElement>(root)[0];
                if(em.Exists(strikeFlight)&&(em.HasComponent<UnitModelInstanceReference>(strikeFlight)||em.HasComponent<UnitDetailedVisualReference>(strikeFlight))&&
                   em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds-firstCommitted>2 && !flightCaptured)
                {
                    if(em.HasComponent<UnitHealth>(strikeFlight)||em.HasComponent<UnitAirMovement>(strikeFlight))throw new InvalidOperationException("Support jet is an autonomous owned actor.");
                    var projected=Camera.main.WorldToScreenPoint(em.GetComponentData<LocalTransform>(strikeFlight).Position);
                    if(projected.z>0&&projected.x>50&&projected.x<Screen.width-50&&projected.y>160&&projected.y<Screen.height-160)
                    {Shot("strike-aircraft");flightCaptured=true;Debug.Log("[SupportNativeAircraft] inViewport=true screen="+projected);}

                }
                if(receipt.Phase==SupportExecutionPhase.Aborted)throw new InvalidOperationException("Native Strike aborted: "+receipt.Reason);
                if(receipt.Phase!=SupportExecutionPhase.Resolved)return false;
                if(receipt.SpentFuel!=4||receipt.ReservedFuel!=0||model.Strike.Charges!=0)throw new InvalidOperationException("Native Strike cost/charge mismatch.");
                using var observations=em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
                foreach(var hit in em.GetBuffer<CombatDamageObservationElement>(observations.GetSingletonEntity(),true))
                    if(hit.SourceKind==CombatDamageSourceKind.SupportStrike&&hit.SourceEntity==strikeFlight)
                    {strikeHits++;if(hit.DamageApplied!=250)throw new InvalidOperationException("Native Strike did not apply 250 actual damage.");Debug.Log($"[SupportNativeStrike] source={receipt.Source} damage={hit.DamageApplied} healthAfter={hit.TargetHealthAfter} fuel={receipt.SpentFuel}");}
                if(strikeHits!=1)throw new InvalidOperationException("Native Strike observation count is not one.");Shot("strike-impact");smokeStage=212;return false;
            }
            if(smokeStage==212){if(TapNamed("SupportCommand"))smokeStage=213;return false;}
            if(smokeStage==213)
            {
                if(UnityEngine.Object.FindAnyObjectByType<SupportPopupView>()==null){if(model.Active)TapNamed("SupportCommand");return false;}
                if(model.Strike.Available)throw new InvalidOperationException("Exhausted Strike remains enabled.");Shot("strike-exhausted");if(TapUnder<SupportPopupView>("CloseButton"))smokeStage=214;return false;
            }
            return smokeStage==214;
        }
        private static bool ParatrooperJourney(EntityManager em)
        {
            if(!UiShellRuntimeGateway.TryReadSupport(out var model))return false;
            if(smokeStage==0){if(TapNamed("ContinueLessonButton"))return false;if(nativeZoomTaps<3&&TapNamed("ZoomOutButton")){nativeZoomTaps++;return false;}if(TapNamed("SupportCommand"))smokeStage=300;return false;}
            if(smokeStage==300){if(TapUnder<SupportPopupView>("SupportCard3"))smokeStage=301;return false;}
            if(smokeStage==301){Shot("paratrooper-popup");if(TapUnder<SupportPopupView>("BeginTargeting"))smokeStage=302;return false;}
            if(smokeStage==302){if(model.Phase==UiSupportPhase.Targeting&&TargetTap(em))smokeStage=303;return false;}
            if(smokeStage==303)
            {
                if(model.Phase!=UiSupportPhase.Preview)return false;
                if(!model.Valid)throw new InvalidOperationException("Paratrooper landing preview: "+model.ReasonKey);
                Shot("paratrooper-preview");if(TapUnder<SupportTargetingInputUiSystemHelper>("Confirm"))smokeStage=304;return false;
            }
            if(smokeStage==304)
            {
                var receipts=em.GetBuffer<SupportReceiptElement>(root);if(receipts.Length==0)return false;
                var receipt=receipts[receipts.Length-1];if(receipt.Phase!=SupportExecutionPhase.Approaching)throw new InvalidOperationException("Paratrooper call rejected: "+receipt.Reason);
                if(receipt.ReservedFuel!=6||receipt.SpentFuel!=0)throw new InvalidOperationException("Paratrooper reservation differs from exact consent.");
                strikeFlight=receipt.Effect;firstCommitted=em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds;smokeStage=305;
            }
            if(smokeStage==305)
            {
                using var passengers=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<SupportPassengerOwnerComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
                if(passengers.CalculateEntityCount()!=4)throw new InvalidOperationException("Paratrooper manifest is not four soldiers.");
                if(!flightCaptured&&em.Exists(strikeFlight)&&em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds-firstCommitted>2)
                {if(!em.HasComponent<UnitDetailedVisualReference>(strikeFlight)&&!em.HasComponent<UnitModelInstanceReference>(strikeFlight))throw new InvalidOperationException("Transport model absent.");Shot("paratrooper-aircraft");flightCaptured=true;}
                using var drops=em.CreateEntityQuery(typeof(SupportPassengerOwnerComponent),typeof(UnitTransportParachuteDropComponent));
                if(drops.CalculateEntityCount()>0&&!smokeVisualCaptured&&em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds-firstCommitted>7.5){Shot("paratrooper-canopies");smokeVisualCaptured=true;}
                using var units=passengers.ToEntityArray(Allocator.Temp);foreach(var unit in units)if(em.GetComponentData<SupportPassengerOwnerComponent>(unit).Landed==0)return false;
                if(!smokeVisualCaptured||!flightCaptured)throw new InvalidOperationException("Airborne visual evidence missing.");
                foreach(var unit in units)if(em.HasComponent<Disabled>(unit)||em.HasComponent<CampaignMissionCombatSuppressedTag>(unit)||em.GetComponentData<Faction>(unit).Id!=1||em.GetComponentData<UnitHealth>(unit).Current<=0)throw new InvalidOperationException("Landed squad is not ordinary live friendly infantry.");
                var receipt=em.GetBuffer<SupportReceiptElement>(root)[0];if(receipt.Phase!=SupportExecutionPhase.Resolved||receipt.SpentFuel!=6||receipt.ReservedFuel!=0)throw new InvalidOperationException("Paratrooper cost was not settled once.");
                Shot("paratrooper-landed");Debug.Log("[SupportNativeParatroopers] canonicalSoldiers=4 faction=1 Fuel=6 landed=controllable normalTransportDescent=true");smokeStage=306;return false;
            }
            if(smokeStage==306)
            {
                using var q=em.CreateEntityQuery(typeof(SupportPassengerOwnerComponent),typeof(LocalTransform));using var units=q.ToEntityArray(Allocator.Temp);
                foreach(var unit in units)
                {
                    var transform=em.GetComponentData<LocalTransform>(unit);var pos=transform.Position;
                    if(em.HasComponent<UnitSelectionHitbox>(unit))pos=math.transform(transform.ToMatrix(),em.GetComponentData<UnitSelectionHitbox>(unit).Center);
                    var point=Camera.main.WorldToScreenPoint(pos);
                    if(point.z<=0||point.x<200||point.x>Screen.width-200||point.y<280||point.y>Screen.height-260)continue;
                    var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);if(hits.Count>0)continue;
                    if(Gesture(point,point)){landedSoldier=unit;landedStart=transform.Position;smokeStage=307;return false;}
                }
                return false;
            }
            if(smokeStage==307){if(!em.HasComponent<SelectedUnitTag>(landedSoldier)){smokeStage=306;return false;}if(TapNamed("MoveCommand"))smokeStage=308;return false;}
            if(smokeStage==308)
            {
                var point=Camera.main.WorldToScreenPoint(landedStart+new float3(0,0,8));
                if(Gesture(point,point))smokeStage=309;return false;
            }
            if(smokeStage==309)
            {
                if(!em.Exists(landedSoldier))throw new InvalidOperationException("Selected reinforcement died before ordinary movement acceptance.");
                if(math.distancesq(em.GetComponentData<LocalTransform>(landedSoldier).Position.xz,landedStart.xz)<4)return false;
                Shot("paratrooper-ordinary-move");Debug.Log("[SupportNativeParatroopers] ordinarySelect=touch ordinaryMove=touch movedMetres>=2");smokeStage=310;return false;
            }
            if(smokeStage==310){if(em.HasComponent<UnitPathRequest>(landedSoldier)||em.HasComponent<UnitPathFollow>(landedSoldier))return false;if(TapNamed("SelectCommand"))smokeStage=311;return false;}
            if(smokeStage==311)
            {
                var actor=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).SourceEntity;if(!em.Exists(actor))return false;
                var transform=em.GetComponentData<LocalTransform>(actor);var position=transform.Position;
                if(em.HasComponent<UnitSelectionHitbox>(actor))position=math.transform(transform.ToMatrix(),em.GetComponentData<UnitSelectionHitbox>(actor).Center);
                var point=Camera.main.WorldToScreenPoint(position);if(FreeWorldPoint(point)&&Gesture(point,point))smokeStage=312;return false;
            }
            if(smokeStage==312){var actor=em.GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).SourceEntity;if(!em.Exists(actor)||!em.HasComponent<SelectedUnitTag>(actor)){smokeStage=311;return false;}return true;}
            return false;
        }
        private static bool SupplyJourney(EntityManager em)
        {
            if(!UiShellRuntimeGateway.TryReadSupport(out var model))return false;
            if(smokeStage==0){if(TapNamed("ContinueLessonButton"))return false;if(nativeZoomTaps<3&&TapNamed("ZoomOutButton")){nativeZoomTaps++;return false;}if(TapNamed("SupportCommand"))smokeStage=400;return false;}
            if(smokeStage==400){if(TapUnder<SupportPopupView>("SupportCard4"))smokeStage=401;return false;}
            if(smokeStage==401){Shot("supply-popup");if(TapUnder<SupportPopupView>("BeginTargeting"))smokeStage=402;return false;}
            if(smokeStage==402){if(model.Phase is UiSupportPhase.Targeting or UiSupportPhase.Preview&&EmptyLandingTap(em))smokeStage=403;return false;}
            if(smokeStage==403)
            {
                if(model.Phase!=UiSupportPhase.Preview)return false;
                if(!model.Valid){Debug.Log("[SupportNativeSupplyPreview] reason="+model.ReasonKey);landingCandidate++;smokeStage=402;return false;}
                Shot("supply-preview");if(TapUnder<SupportTargetingInputUiSystemHelper>("Confirm"))smokeStage=404;return false;
            }
            if(smokeStage==404)
            {
                var receipts=em.GetBuffer<SupportReceiptElement>(root);if(receipts.Length==0)return false;var receipt=receipts[0];
                if(receipt.Phase!=SupportExecutionPhase.Approaching||receipt.ReservedFuel!=3||receipt.SpentFuel!=0)throw new InvalidOperationException("Supply call did not reserve exact three Fuel: "+receipt.Reason);
                strikeFlight=receipt.Effect;supplyCrate=em.GetComponentData<SupportFlightComponent>(strikeFlight).Payload;firstCommitted=em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds;smokeStage=405;
            }
            if(smokeStage==405)
            {
                if(!em.Exists(supplyCrate))throw new InvalidOperationException("Supply payload disappeared.");
                if(!flightCaptured&&em.Exists(strikeFlight)&&em.GetComponentData<SupportSessionComponent>(root).SimulationSeconds-firstCommitted>2){Shot("supply-aircraft");flightCaptured=true;}
                if(em.HasComponent<UnitTransportCargoDropComponent>(supplyCrate)&&!smokeVisualCaptured){Shot("supply-canopy");smokeVisualCaptured=true;}
                var crate=em.GetComponentData<SupportSupplyCrateComponent>(supplyCrate);if(crate.Landed==0)return false;
                if(crate.RemainingMaterials!=40||!smokeVisualCaptured)throw new InvalidOperationException("Supply manifest/descent missing.");
                var receipt=em.GetBuffer<SupportReceiptElement>(root)[0];if(receipt.SpentFuel!=3||receipt.ReservedFuel!=0)throw new InvalidOperationException("Supply Fuel not consumed once.");Shot("supply-landed");smokeStage=406;return false;
            }
            if(smokeStage==406){if(TapNamed("BuildCommand"))smokeStage=407;return false;}
            if(smokeStage==407)
            {
                var storage=SupportSupplyAdapterSystemHelper.MaterialsEntity(em,1);var materials=em.GetComponentData<FactionTacticalMaterialsComponent>(storage);
                if(materials.Current<materials.Capacity){supplyMaterialsBefore=materials.Current;supplyRewardedBefore=materials.LifetimeRewarded;smokeStage=408;return false;}
                var catalog=UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>();
                if(catalog!=null)Tap(catalog.ResolveCatalogTarget(BuildDrawerCategory.Soldiers,"Unit_Chr_Soldier_Male_02_Alt_04"));return false;
            }
            if(smokeStage==408){var view=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>();if(view!=null&&view.IsOpen){Tap(view.CloseButton);return false;}smokeStage=409;return false;}
            if(smokeStage==409)
            {
                var crate=em.GetComponentData<SupportSupplyCrateComponent>(supplyCrate);
                using var q=em.CreateEntityQuery(typeof(UnitHealth),typeof(LocalTransform),typeof(Faction));using var units=q.ToEntityArray(Allocator.Temp);
                foreach(var unit in units){if(!SupportSupplyAdapterSystemHelper.EligibleCollector(em,unit,crate)||em.HasComponent<UnitMovementBehavior>(unit)&&em.GetComponentData<UnitMovementBehavior>(unit).UsesVehicleMotion!=0)continue;var transform=em.GetComponentData<LocalTransform>(unit);var position=transform.Position;if(em.HasComponent<UnitSelectionHitbox>(unit))position=math.transform(transform.ToMatrix(),em.GetComponentData<UnitSelectionHitbox>(unit).Center);var point=Camera.main.WorldToScreenPoint(position);if(!FreeWorldPoint(point))continue;if(Gesture(point,point)){landedSoldier=unit;smokeStage=410;return false;}}
                return false;
            }
            if(smokeStage==410){if(!em.HasComponent<SelectedUnitTag>(landedSoldier)){smokeStage=409;return false;}if(TapNamed("SupportCommand"))smokeStage=411;return false;}
            if(smokeStage==411){if(TapUnder<SupportPopupView>("SupportCard4"))smokeStage=412;return false;}
            if(smokeStage==412){if(model.SupplyStock!=40||model.Supply.Available)throw new InvalidOperationException("Crate stock/exhausted charge UI mismatch.");Shot("supply-collect-detail");if(TapUnder<SupportPopupView>("BeginTargeting"))smokeStage=413;return false;}
            if(smokeStage==413){if(model.Phase!=UiSupportPhase.Collecting)return false;var point=Camera.main.WorldToScreenPoint(em.GetComponentData<SupportSupplyCrateComponent>(supplyCrate).LandingPosition);if(FreeWorldPoint(point)&&Gesture(point,point))smokeStage=414;return false;}
            if(smokeStage==414)
            {
                var storage=SupportSupplyAdapterSystemHelper.MaterialsEntity(em,1);var materials=em.GetComponentData<FactionTacticalMaterialsComponent>(storage);int gained=materials.LifetimeRewarded-supplyRewardedBefore;
                if(gained==0){if(em.HasComponent<SupportCollectionFeedbackComponent>(root)&&em.GetComponentData<SupportCollectionFeedbackComponent>(root).Reason!=SupportRejectionReason.None)throw new InvalidOperationException("Native collection failed: "+em.GetComponentData<SupportCollectionFeedbackComponent>(root).Reason);return false;}
                int accepted=math.min(40,materials.Capacity-supplyMaterialsBefore);int remaining=40-accepted;
                if(accepted<=0||gained!=accepted||materials.Current!=supplyMaterialsBefore+accepted||remaining==0&&em.Exists(supplyCrate)||remaining>0&&(!em.Exists(supplyCrate)||em.GetComponentData<SupportSupplyCrateComponent>(supplyCrate).RemainingMaterials!=remaining))throw new InvalidOperationException("Native collection must transfer only free capacity and retain the exact remainder.");
                Shot("supply-collected");Debug.Log($"[SupportNativeSupply] Fuel=3 normalProductionSpend=true explicitCollect=touch Materials={accepted} remaining={remaining} accountGrant=none");smokeStage=415;
            }
            if(smokeStage==415){if(!em.Exists(supplyCrate))return true;if(TapNamed("SupportCommand"))smokeStage=416;return false;}
            if(smokeStage==416){if(TapUnder<SupportPopupView>("SupportCard4"))smokeStage=417;return false;}
            if(smokeStage==417)
            {
                int remainder=40-math.min(40,em.GetComponentData<FactionTacticalMaterialsComponent>(SupportSupplyAdapterSystemHelper.MaterialsEntity(em,1)).Capacity-supplyMaterialsBefore);
                if(model.SupplyStock!=remainder||!model.SupplyFull||model.SupplyClaimed||model.Supply.Available)throw new InvalidOperationException("Native retained/full-storage UI differs from crate state.");
                Shot("supply-retained-full");if(TapUnder<SupportPopupView>("CloseButton"))smokeStage=418;return false;
            }
            return smokeStage==418;
        }
        private static bool FreeWorldPoint(Vector3 point)
        {
            if(point.z<=0||point.x<200||point.x>Screen.width-200||point.y<280||point.y>Screen.height-260)return false;
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);return hits.Count==0;
        }
        private static bool EmptyLandingTap(EntityManager em)
        {
            if(!SupportLandingUtilitySystemHelper.TryGrid(em,out _,out var grid))return false;
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
            {
                if(member.FactionId!=1||!em.Exists(member.Entity)||!em.HasComponent<LocalTransform>(member.Entity))continue;
                var origin=em.GetComponentData<LocalTransform>(member.Entity).Position;
                for(int i=0;i<24;i++){int ordinal=i+landingCandidate;float angle=ordinal*Mathf.PI/4;var cell=GridUtils.WorldToCell(grid,origin+new float3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(6+ordinal/8*2));var position=GridUtils.CellToWorldCenter(grid,cell);var request=new SupportRequestElement {TargetKind=SupportTargetKind.LandingZone,Position=position};if(SupportSupplyAdapterSystemHelper.Plan(em,root,request,out _,out position)!=SupportRejectionReason.None)continue;var point=Camera.main.WorldToScreenPoint(position);if(FreeWorldPoint(point)&&Gesture(point,point))return true;}
            }
            return false;
        }
        private static void ObserveNativeSmoke(EntityManager em)
        {
            using var zones=em.CreateEntityQuery(typeof(SupportSmokeZoneComponent));
            bool active=zones.CalculateEntityCount()>0;
            if(active)
            {
                using var units=em.CreateEntityQuery(typeof(SupportRangedCoverComponent),typeof(UnitHealth));
                using var covered=units.ToEntityArray(Allocator.Temp);foreach(var entity in covered)if(em.GetComponentData<SupportRangedCoverComponent>(entity).DirectDamagePermille==650)coverSeen=true;
            }
            using var observations=em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
            if(observations.CalculateEntityCount()==1)foreach(var e in em.GetBuffer<CombatDamageObservationElement>(observations.GetSingletonEntity(),true))
                if(e.EventId>lastEvent){lastEvent=e.EventId;if(active&&em.Exists(e.TargetEntity)&&em.HasComponent<SupportRangedCoverComponent>(e.TargetEntity)&&em.GetComponentData<SupportRangedCoverComponent>(e.TargetEntity).DirectDamagePermille==650){observedDamage++;Debug.Log($"[SupportLiveDamage] event={e.EventId} damage={e.DamageApplied} healthAfter={e.TargetHealthAfter} kind={e.SourceKind}");}}
        }
        private static bool HasFriendlyCombatDamage(EntityManager em)
        {
            using var q=em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));if(q.CalculateEntityCount()!=1)return false;
            var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            foreach(var e in em.GetBuffer<CombatDamageObservationElement>(q.GetSingletonEntity(),true))
                if(e.DamageApplied>0 && em.Exists(e.TargetEntity)&&em.HasComponent<Faction>(e.TargetEntity)&&em.GetComponentData<Faction>(e.TargetEntity).Id==1 &&
                   em.HasComponent<CampaignMissionUnitRoleComponent>(e.TargetEntity)&&em.GetComponentData<CampaignMissionUnitRoleComponent>(e.TargetEntity).SessionToken.Equals(runtime.SessionToken))return true;
            return false;
        }
        private static void RequireNoSpend(EntityManager em)
        {if(em.GetBuffer<SupportReceiptElement>(root).Length!=0||em.GetBuffer<SupportAbilityStateElement>(root)[0].ChargesRemaining!=2)throw new InvalidOperationException("Reading or consent spent Support.");}
        private static bool TargetTap(EntityManager em)
        {
            foreach(var member in em.GetBuffer<CampaignMissionDefenseMember>(root,true))
                if(member.FactionId==1 && em.Exists(member.Entity)&&em.HasComponent<LocalTransform>(member.Entity))
                {var pos=em.GetComponentData<LocalTransform>(member.Entity).Position;var point=Camera.main.WorldToScreenPoint(pos);if(point.z>0&&point.x>200&&point.x<Screen.width-200&&point.y>280&&point.y<Screen.height-180)return Gesture(point,point);}
            return Gesture(new Vector2(Screen.width*.5f,Screen.height*.55f),new Vector2(Screen.width*.5f,Screen.height*.55f));
        }
        private static bool TapUnder<T>(string name) where T:Component
        {var owner=UnityEngine.Object.FindAnyObjectByType<T>();return owner!=null&&Tap(owner.GetComponentsInChildren<Button>(false).FirstOrDefault(x=>x.name==name));}
        private static bool TapNamed(string name)=>Tap(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(x=>x.name==name&&x.IsInteractable()));
        private static bool Tap(Button button)
        {
            if(button==null||!button.isActiveAndEnabled||!button.IsInteractable()||hand==null||hand.IsBusy||EditorApplication.timeSinceStartup<nextAction)return false;
            var rect=button.transform as RectTransform;var canvas=button.GetComponentInParent<Canvas>();
            // Authored narrative InputSurface spans a card whose center can be covered
            // by noninteractive captions. Find a visible part of the same real button.
            var hits=new System.Collections.Generic.List<RaycastResult>();
            for(int i=0;i<4;i++)
            {
                var local=rect.rect.center;
                if(i>0)local+=new Vector2(rect.rect.width*.4f,rect.rect.height*(i==1?0:i==2?.25f:-.25f));
                var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(local));
                hits.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
                if(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button)return Gesture(point,point);
            }
            return false;
        }
        private static bool Gesture(Vector2 from,Vector2 to)
        {if(!hand.TryGesture(from,to,.12f,0,Time.unscaledTime))return false;nextAction=EditorApplication.timeSinceStartup+.8;return true;}
        private static void Shot(string name)=>ScreenCapture.CaptureScreenshot(output+"/"+name+".png");
        private static void TouchEvidence(AriaTouchInputUiSystemHelper source,string phase,Vector2 point)
        {if(phase is not("Began" or "Ended"))return;var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);Debug.Log($"[SupportJourneyTouch] phase={phase} point={point} hit={hits.FirstOrDefault().gameObject?.name}");}
        private static void Observe(string message,string stack,LogType type)
        {if(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,type))return;if(type is LogType.Exception or LogType.Assert||type==LogType.Error&&message.StartsWith("[CampaignMissionBootstrap]"))error=message;}
        private static void Complete(bool passed,string detail)
        {
            if(updateHost!=null)UnityEngine.Object.Destroy(updateHost);updateHost=null;
            Application.logMessageReceived-=Observe;AriaTouchInputUiSystemHelper.Evidence-=TouchEvidence;hand?.Dispose();hand=null;
            var world=World.DefaultGameObjectInjectionWorld;
            if(world!=null && world.IsCreated && world.EntityManager.Exists(root))
            {
                var em=world.EntityManager;
                if(em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root) && oldStore!=null)
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=oldStore;
                if(setup && em.HasComponent<SupportSessionComponent>(root))
                {
                    var session=em.GetComponentData<SupportSessionComponent>(root);session.TestEncounter=0;em.SetComponentData(root,session);
                    var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);policy.TestGrantMask=0;em.SetComponentData(root,policy);
                    em.GetBuffer<SupportGroundRegionElement>(root).Clear();em.RemoveComponent<SupportFuelScopeComponent>(root);
                    if(em.HasComponent<SupportAirRouteComponent>(root))em.RemoveComponent<SupportAirRouteComponent>(root);
                    var states=em.GetBuffer<SupportAbilityStateElement>(root);
                    for(int i=0;i<states.Length;i++){var state=states[i];state.Enabled=0;states[i]=state;}
                }
            }
            InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;InputSystem.settings.backgroundBehavior=oldBackground;Application.runInBackground=oldRunBackground;Environment.SetEnvironmentVariable("WARLINE_ARIA_BACKGROUND_VALIDATION",oldValidation);
            Debug.Log("[SupportSmokeJourney] result="+(passed?"Passed":"Failed")+" "+detail+" evidence="+output);
            var result=completion;completion=null;result.TrySetResult(passed?0:1);
        }
    }
}
#endif
