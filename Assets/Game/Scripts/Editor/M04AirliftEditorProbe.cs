using System;
using System.IO;
using System.Reflection;
using System.Linq;
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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Game.Editor
{
    /// <summary>Uses an isolated save and real movement/transport command owners. Never writes rescue, health, position or outcome facts.</summary>
    [InitializeOnLoad]
    public static partial class M04AirliftEditorProbe
    {
        private const string Active="Warline.M04.EditorProbe",Full="Warline.M04.EditorProbe.Full",Both="Warline.M04.EditorProbe.Both";
        private const string Output="Design/AgentReports/M04Airlift/EditorProbe";
        private static bool prepared,deployed,finished,uiPassed;private static int step,passenger,frame;private static double started,lastLog,lastClick,stepAt;
        private static string error,oldLocale;private static CampaignMissionProgressStore store;private static SaveService save;
        static M04AirliftEditorProbe(){if(SessionState.GetBool(Active,false)){EditorApplication.update+=Tick;Application.logMessageReceived+=Observe;}}
        public static void RunLaunch(){SessionState.SetBool(Both,false);SessionState.SetBool(Full,false);Run();}
        public static void RunFull(){M04AirliftPresentationBuilder.Build();SessionState.SetBool(Both,false);SessionState.SetBool(Full,true);Run();}
        public static void RunFinal(){SessionState.SetBool(Both,true);SessionState.SetBool(Full,true);Run();}
        private static void Run()
        {
            Directory.CreateDirectory(Output);SessionState.SetBool(Active,true);prepared=deployed=finished=uiPassed=recoveryActive=false;step=passenger=frame=0;error=null;
            started=EditorApplication.timeSinceStartup;oldLocale=GameLocalization.CurrentLocaleCode;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Observe;Application.logMessageReceived+=Observe;
            EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished||!EditorApplication.isPlaying)return;
            try
            {
                if(error!=null)throw new InvalidOperationException(error);
                if(started==0)started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>850)throw new TimeoutException("M04 Editor rescue exceeded 850 seconds; step="+step);
                var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;var root=roots.GetSingletonEntity();
                if(!prepared)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    string path=Path.Combine(Path.GetTempPath(),"warline-m04-editor-probe",Guid.NewGuid().ToString("N"));
                    save=new SaveService(new JsonSaveRepository(path));store=new CampaignMissionProgressStore(save);store.EnsureAvailable(M04AirliftConfigBuilder.MissionId);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;prepared=true;return;
                }
                if(!deployed)
                {
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign)||!campaign.IsValid)return;
                    if(campaign.SelectedMission.MissionId!=M04AirliftConfigBuilder.MissionId){UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,M04AirliftConfigBuilder.MissionId);return;}
                    deployed=UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy,M04AirliftConfigBuilder.MissionId);return;
                }
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                SkipNarrative();
                if(recoveryActive){TickRecovery(em,root,runtime,facts);return;}
                if(step==30)
                {
                    using var shell=em.CreateEntityQuery(typeof(Game.UI.Shell.Contracts.Ecs.UiShellStateComponent));
                    if(shell.CalculateEntityCount()!=1)return;
                    var shellState=shell.GetSingleton<Game.UI.Shell.Contracts.Ecs.UiShellStateComponent>();var route=shellState.ActiveRoute;
                    if(route==UIRoute.Match || shellState.CurrentMode!=UiShellMode.MainMenu || shellState.IsTransitionRunning!=0)
                    {if(EditorApplication.timeSinceStartup-stepAt>60)throw new TimeoutException("Continue did not finish leaving Match");return;}
                    if(route!=UIRoute.Campaign)throw new InvalidOperationException("M04 Continue returned to "+route+" instead of Campaign");
                    if(SessionState.GetBool(Both,false)){BeginRecovery(em,root);return;}
                    Complete(true,"real APC board -> drive -> unload -> helicopter board -> clear 20s -> airborne departure -> debrief -> bilingual result -> saved three unit unlocks -> campaign return; no rescue/health/position/outcome edits");return;
                }
                var extraction=em.HasComponent<CampaignMissionExtractionState>(root)?em.GetComponentData<CampaignMissionExtractionState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>5)
                {
                    lastLog=EditorApplication.timeSinceStartup;string state=$"step={step} phase={runtime.Phase} ready={runtime.ReadyReadiness}/{runtime.RequiredReadiness} opening={(em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)?em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).Stage:0)} spawned={facts.CommandSquadSpawned} extractionReady={extraction.Ready} tourClock={(em.HasComponent<CampaignMissionOpeningPresentationComponent>(root)?em.GetComponentData<CampaignMissionOpeningPresentationComponent>(root).ElapsedMilliseconds:0)} watchdog={(em.HasComponent<CampaignMissionCameraTourState>(root)?em.GetComponentData<CampaignMissionCameraTourState>(root).OpeningWatchdogMilliseconds:0)} clock={facts.ElapsedMilliseconds} people={facts.ExtractionPassengerTotal} aboard={facts.ExtractionPassengersAboard} rode={facts.ExtractionCarrierLegCount} clear={facts.ExtractionSecureMilliseconds} delivered={facts.ExtractionPassengersDelivered} lost={facts.CivilianLossCount} integrity={facts.HostileRosterIntegrityFault}";
                    if(em.Exists(extraction.Carrier)&&em.HasComponent<LocalTransform>(extraction.Carrier))state+=" APC="+em.GetComponentData<LocalTransform>(extraction.Carrier).Position;
                    if(em.Exists(extraction.Aircraft)&&em.HasComponent<LocalTransform>(extraction.Aircraft))state+=" HELI="+em.GetComponentData<LocalTransform>(extraction.Aircraft).Position+" airborne="+em.GetComponentData<UnitAirComponent>(extraction.Aircraft).Airborne;
                    File.AppendAllText(Output+"/state.txt",state+"\n");Debug.Log("[M04EditorProbe] "+state);
                }
                if(runtime.Outcome==MissionOutcomeKind.Defeat)throw new InvalidOperationException("Rescue lost: specialist="+facts.CivilianLossCount+" apc="+facts.ExtractionCarrierLost+" aircraft="+facts.ExtractionAircraftLost+" timeout="+facts.ExtractionTimedOut+" integrity="+facts.HostileRosterIntegrityFault);
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if(facts.ExtractionPassengersDelivered!=4||facts.ExtractionCarrierLegCount!=4)throw new InvalidOperationException("Invalid victory manifest");
                    if(step<20){step=20;frame=Time.frameCount;stepAt=EditorApplication.timeSinceStartup;}
                    if(EditorApplication.timeSinceStartup-stepAt>90)throw new TimeoutException("Debrief/result/settlement did not complete");
                    if(Time.frameCount-frame<10)return;
                    if(!UiShellRuntimeGateway.TryReadMissionResult(out var result))return;
                    if(result.SettlementFailed)throw new InvalidOperationException("M04 settlement failed");
                    if(!result.Extraction.Applicable||!result.FirstClear||!result.PrimaryActionEnabled)throw new InvalidOperationException("M04 result lost extraction details or first-clear receipt");
                    if(!ResultVisible())return;
                    if(step==20)
                    {
                        var profile=save.LoadProfile();foreach(string id in new[]{"Unit_Chr_Pilot_Female_01","Unit_Veh_APC_Fast","Unit_Veh_Helicopter_Transport"})
                            if(profile.ownedUnitUnlocks.Count(x=>x==id)!=1)throw new InvalidOperationException("Missing/duplicate reward "+id);
                        if(!store.ReadAll().Single(x=>x.missionId==M04AirliftConfigBuilder.MissionId).firstClearRewardSettled)throw new InvalidOperationException("Missing settled receipt");
                        GameLocalization.SetLocale("en",false);Next();return;
                    }
                    if(step==21){ScreenCapture.CaptureScreenshot(Output+"/result-en-16x9.png");Next();return;}
                    if(step==22){GameLocalization.SetLocale("fa-IR",false);Next();return;}
                    if(step==23){ScreenCapture.CaptureScreenshot(Output+"/result-fa-16x9.png");Next();return;}
                    if(step==24)
                    {
                        using(var victoryMembers=em.GetBuffer<CampaignMissionExtractionMember>(root).ToNativeArray(Allocator.Temp))previousAttemptMembers=victoryMembers.ToArray();
                        var view=UnityEngine.Object.FindAnyObjectByType<MissionResultPopupView>();
                        var button=typeof(MissionResultPopupView).GetField("primaryButton",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(view)as Button;
                        if(button==null||!button.isActiveAndEnabled||!button.interactable)return;
                        button.onClick.Invoke();step=30;frame=Time.frameCount;stepAt=EditorApplication.timeSinceStartup;return;
                    }
                    return;
                }
                if(runtime.Phase!=MissionPhaseKind.Engage||facts.ElapsedMilliseconds<1500)return;
                if(em.HasComponent<CampaignMissionCameraTourState>(root))
                {
                    var tour=em.GetComponentData<CampaignMissionCameraTourState>(root);
                    if(tour.OpeningWatchdogMilliseconds>=30000)throw new InvalidOperationException("Normal M04 tour relied on the watchdog fallback");
                }
                if(extraction.Ready==0||facts.ExtractionPassengerTotal!=4||em.GetBuffer<CampaignMissionExtractionMember>(root).Length!=18)throw new InvalidOperationException("M04 canonical manifest mismatch");
                if(!SessionState.GetBool(Full,false) || SessionState.GetBool(Both,false)&&!uiPassed)
                {
                    TickLaunchUi(facts);return;
                }
                CaptureMovingVehicle(em,step is 1 or 3?extraction.Carrier:extraction.Aircraft,step);
                if(Time.frameCount-frame<3)return;
                using var members=em.GetBuffer<CampaignMissionExtractionMember>(root,true).ToNativeArray(Allocator.Temp);var team=new System.Collections.Generic.List<Entity>();
                for(int i=0;i<members.Length;i++)if(members[i].Kind==1)team.Add(members[i].Entity);
                switch(step)
                {
                    case 0:
                        UiShellRuntimeGateway.TryRequestExtractionAction(UiMissionExtractionAction.ContinuePlan);
                        Move(em,extraction.Carrier,new int2(812,427));
                        for(int i=0;i<members.Length;i++)if(members[i].Kind==0)Move(em,members[i].Entity,new int2(830+(i%4)*2,420+(i/4)*3));
                        Next();break;
                    case 1:
                        if(!Near(em,extraction.Carrier,new float3(812,0,427),8))return;
                        passenger=0;Next();break;
                    case 2:
                        if(passenger<4){Board(em,extraction.Carrier,team[passenger++]);frame=Time.frameCount;return;}
                        if(facts.ExtractionCarrierLegCount<4||facts.ExtractionPassengersAboard<4)return;
                        ScreenCapture.CaptureScreenshot(Output+"/apc-loaded.png");Move(em,extraction.Carrier,new int2(1046,428));Next();break;
                    case 3:
                        if(!Near(em,extraction.Carrier,new float3(1046,0,428),7))return;
                        Transport(em,new RtsSelectionCommandIntentRequestElement{Kind=RtsSelectionCommandIntentKind.DisembarkTransport,TargetEntity=extraction.Carrier,HasTargetEntity=1});Next();break;
                    case 4:
                        if(facts.ExtractionPassengersAboard!=0)return;
                        passenger=0;ScreenCapture.CaptureScreenshot(Output+"/transfer.png");Next();break;
                    case 5:
                        if(passenger<4){Board(em,extraction.Aircraft,team[passenger++]);frame=Time.frameCount;return;}
                        if(facts.ExtractionPassengersAboard<4)return;
                        for(int i=0;i<team.Count;i++)if(!em.HasComponent<UnitTransportPassenger>(team[i])||em.GetComponentData<UnitTransportPassenger>(team[i]).Transport!=extraction.Aircraft)return;
                        Next();break;
                    case 6:
                        if(extraction.DepartureCleared==0)return;
                        ScreenCapture.CaptureScreenshot(Output+"/cleared.png");Move(em,extraction.Aircraft,new int2(1085,465));Next();break;
                    case 7:
                        if(EditorApplication.timeSinceStartup-stepAt>120)throw new TimeoutException("Helicopter did not reach airborne departure");break;
                }
            }
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);}
        }
        private static void Move(EntityManager em,Entity e,int2 cell)
        {var result=new UnitMoveOrderSystem().IssueGroupedManualMoveOrder(em,e,cell,true,false,Time.frameCount,Time.frameCount);if(!result.Issued)throw new InvalidOperationException("Move rejected: "+result.RejectionReasonCode);}
        private static bool Near(EntityManager em,Entity e,float3 target,float distance)=>em.Exists(e)&&math.distancesq(em.GetComponentData<LocalTransform>(e).Position.xz,target.xz)<distance*distance;
        private static void Board(EntityManager em,Entity transport,Entity person)=>Transport(em,new RtsSelectionCommandIntentRequestElement{Kind=RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger,TargetEntity=transport,SecondaryTargetEntity=person,HasTargetEntity=1,HasSecondaryTargetEntity=1});
        private static void Transport(EntityManager em,RtsSelectionCommandIntentRequestElement request){request.Frame=Time.frameCount;if(!new RtsSelectionInputStateCompositionSystemHelper(em).TryEnqueueCommandRequest(request))throw new InvalidOperationException("Transport command queue rejected request");}
        private static void Next(){step++;frame=Time.frameCount;stepAt=EditorApplication.timeSinceStartup;}
        private static void SkipNarrative()
        {
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);if(view==null||!Visible(view,"rootGroup")||EditorApplication.timeSinceStartup-lastClick<.7)return;
            var confirm=view.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");object owner=confirming?(object)confirm:view.PlaybackControlsView;
            var button=owner?.GetType().GetField(confirming?"confirmButton":"skipButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner)as Button;
            if(button==null||!button.isActiveAndEnabled||!button.interactable)return;button.onClick.Invoke();lastClick=EditorApplication.timeSinceStartup;
        }
        private static bool Visible(object owner,string field){var group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner)as CanvasGroup;return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static void Observe(string message,string stack,LogType type){if(type is LogType.Exception or LogType.Assert)error=message;}
        private static void Complete(bool pass,string detail)
        {
            if(finished)return;finished=true;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;
            if(oldLocale!=null)GameLocalization.SetLocale(oldLocale,false);Time.timeScale=1;
            Debug.Log("[M04EditorProbe] result="+(pass?"Passed":"Failed")+" "+detail);File.AppendAllText(Output+"/state.txt","result="+(pass?"Passed":"Failed")+" "+detail+"\n");
            void Exited(PlayModeStateChange state){if(state!=PlayModeStateChange.EnteredEditMode)return;EditorApplication.playModeStateChanged-=Exited;AssetDatabase.AllowAutoRefresh();double until=EditorApplication.timeSinceStartup+3;
                void Exit(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<until)return;EditorApplication.update-=Exit;EditorApplication.Exit(pass?0:1);}EditorApplication.update+=Exit;}
            EditorApplication.playModeStateChanged+=Exited;EditorApplication.ExitPlaymode();
        }
    }
}
