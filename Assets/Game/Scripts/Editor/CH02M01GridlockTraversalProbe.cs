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
    /// <summary>Scripted mechanics probe. NOT manual-input, Watch ARIA, pacing or acceptance evidence.</summary>
    [InitializeOnLoad]
    public static class CH02M01GridlockTraversalProbe
    {
        private const string Active="Warline.Gridlock.TraversalProbe";
        private const string Output="/private/tmp/warline-gridlock";
        private static bool prepared,deployed,finished;
        private static double started,lastLog,lastOrder,lastClick;
        private static float3 departure;
        private static bool observedDeparture;
        private static float traveled;
        private static int step;
        static CH02M01GridlockTraversalProbe(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Tick;}
        public static void Run()
        {
            Directory.CreateDirectory(Output);SessionState.SetBool(Active,true);prepared=deployed=finished=false;observedDeparture=false;traveled=0;step=0;
            MainMenuV3PrefabBuilder.SetGameViewResolution(1920,1080);started=EditorApplication.timeSinceStartup;
            EditorSceneManager.OpenScene(M02EstablishBaseNarrativeConfigBuilder.MenuScenePath,OpenSceneMode.Single);
            AssetDatabase.DisallowAutoRefresh();EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
        }
        private static void Tick()
        {
            if(finished || !EditorApplication.isPlaying) return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                if(EditorApplication.timeSinceStartup-started>780)throw new TimeoutException("Gridlock traversal probe timeout");
                var world=World.DefaultGameObjectInjectionWorld;if(world==null || !world.IsCreated)return;var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;
                var root=roots.GetSingletonEntity();
                if(!prepared)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    var store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"probe-profile-"+Guid.NewGuid().ToString("N")))));
                    store.EnsureAvailable(CampaignMissionSequence.Gridlock);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                    GameLocalization.SetLocale("en",false);prepared=true;return;
                }
                if(!deployed)
                {
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign)||!campaign.IsValid)return;
                    if(campaign.SelectedMission.MissionId!=CampaignMissionSequence.Gridlock){UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,CampaignMissionSequence.Gridlock);return;}
                    deployed=UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy,CampaignMissionSequence.Gridlock);return;
                }
                SkipStoryForMechanicsProbe();
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                var g=em.HasComponent<CampaignMissionGridlockState>(root)?em.GetComponentData<CampaignMissionGridlockState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>5)
                {
                    lastLog=EditorApplication.timeSinceStartup;
                    var facts=em.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
                    Debug.Log($"[GridlockTraversal] step={step} phase={runtime.Phase} ready={g.Ready} failure={g.Failure} clock={g.ElapsedMilliseconds} rifles={g.LivingRifles} workers={g.LivingWorkers} fadi={g.LivingFadi} hostiles={facts.HostileDefeatedCount}/{facts.HostileTotalCount} connected={g.RouteConnected} arrived={g.VehicleArrived} hold={g.HoldMilliseconds} travel={traveled:F1}");
                }
                if(g.Failure!=GridlockFailure.None)
                {
                    Debug.Log($"[GridlockTraversal] failureDetail ready={g.Ready} preparation={g.PreparationMilliseconds} clock={g.ElapsedMilliseconds}");
                    foreach(var member in em.GetBuffer<CampaignMissionGridlockMember>(root,true))
                        Debug.Log($"[GridlockTraversal] member={member.Kind} exists={em.Exists(member.Entity)} health={Hp(em,member.Entity)} initialized={member.HealthInitialized} dead={member.Dead}");
                    foreach(var siteState in em.GetBuffer<CampaignMissionGridlockWorkSite>(root,true))
                        Debug.Log($"[GridlockTraversal] site={siteState.Center} request={siteState.SpawnRequestId} building={siteState.BuildingRuntimeId} initialized={siteState.Initialized} work={siteState.WorkMilliseconds} clear={siteState.ClearRequested} complete={siteState.Complete}");
                    using var boundaries=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeSpawnRequest));
                    if(boundaries.CalculateEntityCount()==1)
                        foreach(var spawn in em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaries.GetSingletonEntity(),true))
                            Debug.Log($"[GridlockTraversal] spawn={spawn.BuildingId} status={spawn.Status} resultCode={spawn.ResultCode} origin={spawn.PreferredOrigin} actualFootprint={spawn.ActualFootprint}");
                    throw new InvalidOperationException("Gridlock failure "+g.Failure);
                }
                if(em.Exists(g.Vehicle) && em.HasComponent<LocalTransform>(g.Vehicle))
                {
                    var position=em.GetComponentData<LocalTransform>(g.Vehicle).Position;
                    if(!observedDeparture){departure=position;observedDeparture=true;}traveled=math.max(traveled,math.distance(position,departure));
                }
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if(traveled<120 || g.Delivered==0 || g.HoldMilliseconds<20000)throw new InvalidOperationException("Victory without actual traversal");
                    ScreenCapture.CaptureScreenshot(Output+"/scripted-traversal.png");
                    Complete(true,"actualTravel="+traveled.ToString("F1")+" scope=scripted-mechanics-only aria=NotRun manual=NotRun");return;
                }
                if(runtime.Phase!=MissionPhaseKind.Engage || g.Ready==0 || EditorApplication.timeSinceStartup-lastOrder<3)return;
                lastOrder=EditorApplication.timeSinceStartup;
                using var members=em.GetBuffer<CampaignMissionGridlockMember>(root,true).ToNativeArray(Allocator.Temp);
                Entity enemy=Entity.Null;
                foreach(var m in members) if(m.Kind>=GridlockMemberKind.Hostile && Hp(em,m.Entity)>0){enemy=m.Entity;break;}
                foreach(var m in members)
                    if(m.Kind==GridlockMemberKind.Rifle && Hp(em,m.Entity)>0 && enemy!=Entity.Null)
                        UnitAttackOrderRequestSystem.EnqueueSourceAttackTarget(em,m.Entity,enemy);
                var sites=em.GetBuffer<CampaignMissionGridlockWorkSite>(root,true);
                if(sites.Length!=2)return;
                int site=sites[0].Complete==0?0:1;step=site+1;
                bool nearbyHostile=false;
                foreach(var m in members) if(m.Kind>=GridlockMemberKind.Hostile && Hp(em,m.Entity)>0 && em.HasComponent<LocalTransform>(m.Entity))
                    nearbyHostile|=math.distancesq(em.GetComponentData<LocalTransform>(m.Entity).Position.xz,sites[site].Center.xz)<25*25;
                if(nearbyHostile || sites[site].Complete!=0)return;
                using var moveQueue=em.CreateEntityQuery(typeof(UnitMoveOrderQueueComponent));
                foreach(var m in members)
                    if((m.Kind==GridlockMemberKind.Fadi || m.Kind==GridlockMemberKind.Worker) && Hp(em,m.Entity)>0)
                        UnitMoveOrderRequestSystem.EnqueueMoveOrder(em,m.Entity,new int2((int)sites[site].Center.x,(int)sites[site].Center.z),UnitMoveOrderRequestKind.Immediate,true,false,0,0,moveQueue);
            }
            catch(Exception e){Debug.LogException(e);Complete(false,e.Message);}
        }
        private static int Hp(EntityManager em,Entity entity)=>em.Exists(entity)&&em.HasComponent<UnitHealth>(entity)?em.GetComponentData<UnitHealth>(entity).Current:0;
        private static void SkipStoryForMechanicsProbe()
        {
            var view=UnityEngine.Object.FindAnyObjectByType<NarrativeSequenceView>(FindObjectsInactive.Include);
            if(view==null || !Visible(view,"rootGroup") || EditorApplication.timeSinceStartup-lastClick<.7)return;
            var confirm=view.SkipConfirmationView;bool confirming=confirm!=null&&Visible(confirm,"group");object owner=confirming?(object)confirm:view.PlaybackControlsView;
            var button=owner?.GetType().GetField(confirming?"confirmButton":"skipButton",BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner)as Button;
            if(button==null||!button.isActiveAndEnabled||!button.interactable)return;button.onClick.Invoke();lastClick=EditorApplication.timeSinceStartup;
        }
        private static bool Visible(object owner,string field){var group=owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(owner)as CanvasGroup;return group!=null&&group.alpha>.9f&&group.gameObject.activeInHierarchy;}
        private static void Complete(bool pass,string detail)
        {
            if(finished)return;finished=true;SessionState.SetBool(Active,false);EditorApplication.update-=Tick;
            Debug.Log("[GridlockTraversal] result="+(pass?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(pass);
        }
    }
}
