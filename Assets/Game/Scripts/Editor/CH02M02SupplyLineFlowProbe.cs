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
    public static class CH02M02SupplyLineFlowProbe
    {
        private const string Active="Warline.SupplyLine.TraversalProbe";
        private const string Output="/private/tmp/warline-supply-line";
        private static bool prepared,deployed,finished;
        private static double started,lastLog,lastOrder,lastClick;
        private static bool captured;
        private static int2 lastOilTruckCell;
        private static int oilTruckStoppedSince;
        static CH02M02SupplyLineFlowProbe(){if(SessionState.GetBool(Active,false))EditorApplication.update+=Tick;}
        public static void Run()
        {
            try { CH02M02SupplyLinePresentationBuilder.BuildCheckpoint(); }
            catch(Exception exception)
            {
                Debug.LogException(exception);
                Complete(false,"Authoring failed: "+exception.Message);
                return;
            }
            Directory.CreateDirectory(Output);SessionState.SetBool(Active,true);prepared=deployed=finished=captured=false;
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
                if(EditorApplication.timeSinceStartup-started>900)throw new TimeoutException("SupplyLine traversal probe timeout");
                var world=World.DefaultGameObjectInjectionWorld;if(world==null || !world.IsCreated)return;var em=world.EntityManager;
                using var roots=em.CreateEntityQuery(typeof(CampaignMissionRootComponent),typeof(CampaignMissionRuntimeComponent));if(roots.CalculateEntityCount()!=1)return;
                var root=roots.GetSingletonEntity();
                if(!prepared)
                {
                    if(!em.HasComponent<CampaignMissionProgressStoreReferenceComponent>(root))return;
                    var store=new CampaignMissionProgressStore(new SaveService(new JsonSaveRepository(Path.Combine(Output,"probe-profile-"+Guid.NewGuid().ToString("N")))));
                    store.EnsureAvailable(CampaignMissionSequence.SupplyLine);
                    em.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(root).Store=store;
                    GameLocalization.SetLocale("en",false);prepared=true;return;
                }
                if(!deployed)
                {
                    if(!UiShellRuntimeGateway.TryReadCampaignOperations(out var campaign)||!campaign.IsValid)return;
                    if(campaign.SelectedMission.MissionId!=CampaignMissionSequence.SupplyLine){UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Select,CampaignMissionSequence.SupplyLine);return;}
                    deployed=UiShellRuntimeGateway.TryEnqueueCampaignMissionAction(UiCampaignMissionActionKind.Deploy,CampaignMissionSequence.SupplyLine);return;
                }
                SkipStoryForMechanicsProbe();
                var runtime=em.GetComponentData<CampaignMissionRuntimeComponent>(root);
                var g=em.HasComponent<CampaignMissionSupplyLineState>(root)?em.GetComponentData<CampaignMissionSupplyLineState>(root):default;
                if(EditorApplication.timeSinceStartup-lastLog>10)
                {
                    lastLog=EditorApplication.timeSinceStartup;
                    Debug.Log($"[SupplyLineFlow] phase={runtime.Phase} ready={g.Ready} failure={g.Failure} clock={g.ElapsedMilliseconds} oil={g.ObservedOil} refined={g.ObservedRefinedFuel} stored={g.StoredFuel} hold={g.HoldMilliseconds}");
                    if(em.HasBuffer<CampaignMissionSupplyLineLink>(root))foreach(var link in em.GetBuffer<CampaignMissionSupplyLineLink>(root,true))Debug.Log($"[SupplyLineFlow] link={link.BuildingId} request={link.SpawnRequestId} initialized={link.Initialized} origin={link.Origin} actual={(em.Exists(link.Entity) && em.HasComponent<RuntimeBuildingCombatInfo>(link.Entity)?em.GetComponentData<RuntimeBuildingCombatInfo>(link.Entity).OriginCell:default)}");
                    if(em.HasBuffer<CampaignMissionSupplyLineMember>(root))foreach(var m in em.GetBuffer<CampaignMissionSupplyLineMember>(root,true))
                    {
                        if(m.Kind!=1 && m.Kind!=2)continue;
                        var h=em.HasComponent<UnitResourceHauler>(m.Entity)?em.GetComponentData<UnitResourceHauler>(m.Entity):default;
                        var status=em.HasComponent<UnitResourceHaulStatus>(m.Entity)?em.GetComponentData<UnitResourceHaulStatus>(m.Entity):default;
                        Debug.Log($"[SupplyLineFlow] grid={em.GetComponentData<UnitGrid>(m.Entity).Cell} pathRequest={em.HasComponent<UnitPathRequest>(m.Entity)} pathFollow={em.HasComponent<UnitPathFollow>(m.Entity)} hauler={m.Kind} capacity={h.BarrelCapacity} oil={h.CargoOilBarrels} fuel={h.CargoFuelBarrels} status={status.StatusCode} reason={status.ReasonCode}");
                    }
                }
                if(g.Failure!=SupplyLineFailure.None)
                {
                    using var boundaries=em.CreateEntityQuery(typeof(BuildingRuntimeStateTag),typeof(BuildingRuntimeSpawnRequest));
                    if(boundaries.CalculateEntityCount()==1)foreach(var spawn in em.GetBuffer<BuildingRuntimeSpawnRequest>(boundaries.GetSingletonEntity(),true))Debug.Log($"[SupplyLineFlow] spawn={spawn.BuildingId} status={spawn.Status} result={spawn.ResultCode} origin={spawn.PreferredOrigin} actual={spawn.ActualOrigin} footprint={spawn.ActualFootprint}");
                    throw new InvalidOperationException("Supply Line failure "+g.Failure);
                }
                if(g.Ready!=0 && runtime.Phase==MissionPhaseKind.Engage)
                {
                    foreach(var member in em.GetBuffer<CampaignMissionSupplyLineMember>(root,true))
                    {
                        if(member.Kind!=1 || !em.Exists(member.Entity) || !em.HasComponent<UnitResourceHaulOrder>(member.Entity))continue;
                        var order=em.GetComponentData<UnitResourceHaulOrder>(member.Entity);
                        var cell=em.GetComponentData<UnitGrid>(member.Entity).Cell;
                        if(!cell.Equals(lastOilTruckCell) || (order.Phase!=1 && order.Phase!=3))oilTruckStoppedSince=g.ElapsedMilliseconds;
                        lastOilTruckCell=cell;
                        if(g.ElapsedMilliseconds-oilTruckStoppedSince>60000)throw new InvalidOperationException("Oil truck has made no travel progress for sixty simulation seconds.");
                    }
                }
                if(!captured && g.Ready!=0 && g.ElapsedMilliseconds>5000)
                {ScreenCapture.CaptureScreenshot(Output+"/supply-line-opening.png");captured=true;}
                if(g.OilTransferred!=0 && g.RouteRecovered==0 && g.RerouteOrdered==0)
                {
                    foreach(var member in em.GetBuffer<CampaignMissionSupplyLineMember>(root,true).ToNativeArray(Allocator.Temp))
                        if(member.Kind==1 && em.Exists(member.Entity) && em.GetComponentData<UnitResourceHauler>(member.Entity).CargoOilBarrels<=0)
                            UnitMoveOrderRequestSystem.EnqueueGroupedManualMoveOrder(em,member.Entity,g.AlternateLane,true,false,0,Time.frameCount);
                }
                if(g.StoredFuel>=20 && g.AllocatedCivilianBarrels==0)UiShellRuntimeGateway.TryAllocateSupplyLineReserve();
                if(runtime.Outcome==MissionOutcomeKind.Victory)
                {
                    if(g.OilTransferred==0 || g.FuelTransferred==0 || g.StoredFuel<40 || g.HoldMilliseconds<20000)throw new InvalidOperationException("Victory without real chain facts");
                    ScreenCapture.CaptureScreenshot(Output+"/scripted-flow.png");
                    Complete(true,$"oil={g.ObservedOil} refined={g.ObservedRefinedFuel} stored={g.StoredFuel} scope=scripted-mechanics-only manual=NotRun aria=NotRun");return;
                }
                if(runtime.Phase!=MissionPhaseKind.Engage || g.Ready==0 || EditorApplication.timeSinceStartup-lastOrder<3)return;
                lastOrder=EditorApplication.timeSinceStartup;
                using var members=em.GetBuffer<CampaignMissionSupplyLineMember>(root,true).ToNativeArray(Allocator.Temp);
                Entity enemy=Entity.Null;
                foreach(var m in members)if(m.Kind==3 && Hp(em,m.Entity)>0){enemy=m.Entity;break;}
                foreach(var m in members)if(m.Kind==0 && Hp(em,m.Entity)>0 && enemy!=Entity.Null)UnitAttackOrderRequestSystem.EnqueueSourceAttackTarget(em,m.Entity,enemy);
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
            Debug.Log("[SupplyLineFlow] result="+(pass?"Passed":"Failed")+" "+detail);MissionEditorValidationExit.Complete(pass);
        }
    }
}
