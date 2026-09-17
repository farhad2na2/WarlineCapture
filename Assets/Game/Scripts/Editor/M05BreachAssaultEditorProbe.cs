using System;
using System.IO;
using System.Reflection;
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
    /// <summary>Isolated profile, real attack/move queues. Never changes health, transforms, objective facts or outcomes.</summary>
    [InitializeOnLoad]
    public static partial class M05BreachAssaultEditorProbe
    {
        private const string Active="Warline.M05.EditorProbe";
        private const string Output="/private/tmp/warline-m05-editor-probe";
        private static bool prepared,deployed,finished;private static double started,lastLog,lastClick,lastOrder;private static int step;
        private static double victoryAt;
        private static string narrativeShot;private static double narrativePanelAt;
        private static string error;private static CampaignMissionProgressStore store;
        static M05BreachAssaultEditorProbe(){if(SessionState.GetBool(Active,false)){EditorApplication.update+=Tick;Application.logMessageReceived+=Observe;}}
        public static void Run()
        {
            Directory.CreateDirectory(Output);SessionState.SetBool(Active,true);prepared=deployed=finished=false;step=0;guideAuditStage=0;error=null;
            MissionMotionEditorAudit.Begin();started=EditorApplication.timeSinceStartup;victoryAt=0;recoveryStage=0;replay=false;MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived-=Observe;Application.logMessageReceived+=Observe;
            EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished || !EditorApplication.isPlaying) return;
            try
            {
                if(error!=null) throw new InvalidOperationException(error);
                if(started==0) started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>1000) throw new TimeoutException("M5 journey exceeded 1000 seconds at step "+step);
                var world=World.DefaultGameObjectInjectionWorld;if(world==null || !world.IsCreated)return;var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;var root=roots.GetSingletonEntity();
                if(!prepared)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    var save=new SaveService(new JsonSaveRepository(Path.Combine(Output,Guid.NewGuid().ToString("N"))));
                    store=new CampaignMissionProgressStore(save);store.EnsureAvailable(M05BreachAssaultConfigBuilder.MissionId);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;GameLocalization.SetLocale(SessionState.GetBool("Warline.M05.Guided",false) || SessionState.GetBool("Warline.M05.EnglishCombat",false)?"en":"fa-IR",false);prepared=true;return;
                }
                if(!deployed)
                {
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign)||!campaign.IsValid)return;
                    if(campaign.SelectedMission.MissionId!=M05BreachAssaultConfigBuilder.MissionId){UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,M05BreachAssaultConfigBuilder.MissionId);return;}
                    deployed=UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy,M05BreachAssaultConfigBuilder.MissionId);return;
                }
                SampleVoices();SkipNarrative();
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                MissionMotionEditorAudit.Sample(em,root,in runtime,in facts);
                if(SessionState.GetBool("Warline.M05.LifecycleAudit",false)) SampleLifecycleUi(em,root);
                var breach=em.HasComponent<CampaignMissionBreachState>(root)?em.GetComponentData<CampaignMissionBreachState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>5)
                {
                    lastLog=EditorApplication.timeSinceStartup;
                    string log=$"step={step} phase={runtime.Phase} ready={runtime.ReadyReadiness}/{runtime.RequiredReadiness} spawned={facts.CommandSquadSpawned} breachReady={breach.Ready} gate={breach.Gate} hp={Hp(em,breach.Gate)} core={breach.Core} hp={Hp(em,breach.Core)} clock={facts.ElapsedMilliseconds} hostiles={facts.HostileDefeatedCount}/{facts.HostileTotalCount} hold={breach.SecureHoldMilliseconds} integrity={facts.HostileRosterIntegrityFault} gateRequest={breach.GateRequestId} coreRequest={breach.CoreRequestId}";
                    Debug.Log("[M05EditorProbe] "+log);File.AppendAllText(Output+"/state.txt",log+"\n");
                }
                if (InspectRadarFootprint(em, breach)) return;
                if(SessionState.GetBool("Warline.M05.RetryProbe",false)){TickRetry(em,root,runtime,facts,breach);return;}
                if(recoveryStage!=0) {TickReturn(em,root,runtime,breach);return;}
                if(runtime.Outcome==MissionOutcomeKind.Defeat) throw new InvalidOperationException("M5 defeat: timeout="+facts.BreachTimedOut+" integrity="+facts.HostileRosterIntegrityFault+" losses="+facts.SquadLossCount);
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    Time.timeScale=1;
                    if(!UiShellRuntimeGateway.TryReadMissionResult(out var result))return;
                    if(!result.Breach.Applicable || !result.PrimaryActionEnabled)throw new InvalidOperationException("Missing M5 result or settlement");
                    if(victoryAt==0) {victoryAt=EditorApplication.timeSinceStartup;ScreenCapture.CaptureScreenshot(Output+"/victory-"+GameLocalization.CurrentLocaleCode+".png");return;}
                    if(EditorApplication.timeSinceStartup-victoryAt<2)return;
                    BeginReturn(em,root,result);return;
                }
                if(runtime.Phase!=MissionPhaseKind.Engage || breach.Ready==0) return;
                if(TickGuideAudit())return;
                if(SessionState.GetBool("Warline.M05.Guided",false)) {TickGuided();return;}
                if(step==0)
                {
                    ScreenCapture.CaptureScreenshot(Output+"/launch-fa.png");Debug.Log("[M05EditorProbe] gateCenter="+breach.GateCenter+" coreCenter="+breach.CoreCenter+" archive="+breach.ArchiveCenter);
                    if(!UiShellRuntimeGateway.TryContinueBreachPlan())return;step=1;lastOrder=EditorApplication.timeSinceStartup;return;
                }
                if(EditorApplication.timeSinceStartup-lastOrder<3) return;
                lastOrder=EditorApplication.timeSinceStartup;
                if(step==1) {ScreenCapture.CaptureScreenshot(Output+"/tutorial-fa.png");step=2;}
                if(SessionState.GetBool("Warline.M05.ScreenTapAudit",false) && TickGateScreenTap(em,breach))return;
                using var members=em.GetBuffer<CampaignMissionBreachMember>(root,true).ToNativeArray(Allocator.Temp);
                Entity enemy=Entity.Null;
                foreach(var member in members) if(member.Kind>=2 && Hp(em,member.Entity)>0) {enemy=member.Entity;break;}
                var target=breach.GateDestroyed==0?breach.Gate:breach.CoreDestroyed==0?breach.Core:enemy;
                foreach(var member in members)
                {
                    if(member.Kind>1 || Hp(em,member.Entity)<=0)continue;
                    if(target!=Entity.Null && Hp(em,target)>0) UnitAttackOrderRequestSystem.EnqueueSourceAttackTarget(em,member.Entity,target);
                    else if(!Near(em,member.Entity,breach.ArchiveCenter,4))
                    {
                        var cell=new int2((int)breach.ArchiveCenter.x,(int)breach.ArchiveCenter.z);
                        var move=new UnitMoveOrderSystem().IssueGroupedManualMoveOrder(em,member.Entity,cell,true,false,Time.frameCount,Time.frameCount);
                        if(!move.Issued) Debug.LogWarning("[M05EditorProbe] move rejected: "+move.RejectionReasonCode);
                    }
                }
            }
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);}
        }
        private static int Hp(EntityManager em,Entity entity)=>em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)?em.GetComponentData<UnitHealth>(entity).Current:0;
        private static bool Near(EntityManager em,Entity e,float3 target,float radius)=>em.HasComponent<LocalTransform>(e)&&math.distancesq(em.GetComponentData<LocalTransform>(e).Position.xz,target.xz)<radius*radius;
        private static void SkipNarrative()
        {
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);if(view==null||!Visible(view,"rootGroup")||EditorApplication.timeSinceStartup-lastClick<.7)return;
            if(!SessionState.GetBool("Warline.M05.SkipComics",false) && !SessionState.GetBool("Warline.M05.GuideOnly",false) && !SessionState.GetBool("Warline.M05.RetryProbe",false) && view.CurrentPanelSprite!=null && view.CurrentPanelSprite.name.StartsWith("M05-",StringComparison.Ordinal))
            {
                string shot=GameLocalization.CurrentLocaleCode+"-"+view.CurrentPanelSprite.name;
                if(narrativeShot!=shot){narrativeShot=shot;narrativePanelAt=EditorApplication.timeSinceStartup;return;}
                if(EditorApplication.timeSinceStartup-narrativePanelAt>2 && EditorApplication.timeSinceStartup-narrativePanelAt<3)
                    ScreenCapture.CaptureScreenshot(Output+"/comic-"+shot+".png");
                return;
            }
            var confirm=view.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");object owner=confirming?(object)confirm:view.PlaybackControlsView;
            var button=owner?.GetType().GetField(confirming?"confirmButton":"skipButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner)as Button;
            if(button==null||!button.isActiveAndEnabled||!button.interactable)return;button.onClick.Invoke();lastClick=EditorApplication.timeSinceStartup;
        }
        private static bool Visible(object owner,string field){var group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner)as CanvasGroup;return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static void Observe(string message,string stack,LogType type)
        {
            if(MissionEditorQaLogClassification.IsEditorCloudTokenFailure(message,stack,type))return;
            if(type is LogType.Exception or LogType.Assert || type==LogType.Error && message.StartsWith("[CampaignMissionBootstrap]",StringComparison.Ordinal))error=message;
        }
        private static void Complete(bool pass,string detail)
        {
            if(finished)return;if(pass)ValidatePlayedVoices();finished=true;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;Application.logMessageReceived-=Observe;Time.timeScale=1;
            Debug.Log("[M05EditorProbe] result="+(pass?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(pass);
        }
    }
}
