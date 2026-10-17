#if UNITY_EDITOR
using System;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // NEW scoped normal-budget canary. Install only after independent source review.
    // Shares existing normal Menu/industry/research/Airport Touch preparation.
    // No isolated fixture controller, funding/readiness/AI override or ECS writes.
    public static partial class SkirmishS002AriaRunHarness
    {
        static string planeCanaryOutput;
        static bool planeCanaryActive,planeCanaryLeased,planeCanaryBaseline,planeCanaryTicking;
        static int planeCanaryConstructionBefore,planeCanaryAirportCost,planeCanaryLastFrame;
        static double planeCanaryDeadline;
        static string planeCanaryAttempt;
        static SkirmishS005PlayerResumeFrame planeCanaryInputOwner;

        [Serializable] sealed class NormalPlaneLineageRow
        {
            public string stage="",attempt="",reason="",role="",phase="",stable="",airportStable="",catalog="",definition="",tracePath="";
            public int frame,seed,faction,materials,capacity,lifetimeFabricated,lifetimeImported,lifetimeRewarded,lifetimeSpent,productionSpent,constructionSpent,constructionBefore,airportCost;
            public int producerRuntimeId,airportIndex,airportVersion,airportHp,actorIndex,actorVersion,actorHp,paid,members,delivered,remaining,transit;
            public uint reservation,completed,dispatched,unexpected,interventions,generation;
            public int violations,device,stockStrikeReservations;
            public float timeScale,elapsed,homeDistance;
            public bool homeInitialized,usesRunway,normalMenu,normalBudget,actorNativeShared,canaryOnly=true,fullMissionCertified=false;
        }
        static void PlaneCanaryWrite(NormalPlaneLineageRow row)
        {
            row.frame=Time.frameCount;row.timeScale=Time.timeScale;row.attempt=planeCanaryAttempt??"";
            row.tracePath=LiveTracePath();row.normalBudget=true;
            if(playerResumeTouch!=null){row.completed=playerResumeTouch.CompletedGestures;row.dispatched=playerResumeTouch.DispatchedGestures;
                row.unexpected=playerResumeTouch.UnexpectedSamples;row.interventions=playerResumeTouch.PhysicalInterventions;
                row.device=playerResumeTouch.DeviceId;row.generation=playerResumeTouch.Generation;}
            row.violations=AriaCommandEvidence.Violations;
            File.AppendAllText(planeCanaryOutput,JsonUtility.ToJson(row)+"\n");
        }
        public static void RunS005NormalPaidPlaneLineageCanary()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||planeCanaryActive)throw new InvalidOperationException("Idle owned Editor required");
            planeCanaryOutput=Environment.GetEnvironmentVariable("WARLINE_S005_PLANE_CANARY_OUTPUT");
            if(string.IsNullOrEmpty(planeCanaryOutput)||!Path.IsPathRooted(planeCanaryOutput)||File.Exists(planeCanaryOutput))
                throw new InvalidOperationException("Fresh absolute canary evidence path required");
            Directory.CreateDirectory(Path.GetDirectoryName(planeCanaryOutput));
            using(File.Open(planeCanaryOutput,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}
            planeCanaryActive=true;planeCanaryLeased=planeCanaryBaseline=planeCanaryTicking=false;planeCanaryAttempt="";planeCanaryLastFrame=-1;
            planeCanaryConstructionBefore=planeCanaryAirportCost=0;planeCanaryDeadline=EditorApplication.timeSinceStartup+1500;
            // Existing public normal setup; this does not fund, promote, spawn or disable enemies.
            RunS005EditorStrikeCycleProbe();
            Canvas.willRenderCanvases+=TickNormalPaidPlaneCanary;
            PlaneCanaryWrite(new NormalPlaneLineageRow{stage="Started",reason="NormalMenuAndPaidAirportSetup;NoNormalMissionClaim"});
        }
        static void EndNormalPaidPlaneCanary(bool failed,string reason)
        {
            Canvas.willRenderCanvases-=TickNormalPaidPlaneCanary;planeCanaryActive=false;
            PlaneCanaryWrite(new NormalPlaneLineageRow{stage=failed?"Failure":"ScopedComplete",reason=reason});
            playerResumeProbeActive=false;
            if(playerResumeTouch!=null){playerResumeTouch.Dispose();playerResumeTouch=null;}
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY","1");
            Finish(failed,reason); // Explicit scoped exit; no fabricated Victory/result/return.
        }
        // Read-only native observation. No Touch calls, command queue writes or ECS mutation.
        static NormalPlaneLineageRow ObserveNormalPlaneLineage(EntityManager em,Entity session)
        {
            var state=em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            var row=new NormalPlaneLineageRow{stage="NativeSample",faction=1,constructionBefore=planeCanaryConstructionBefore,airportCost=planeCanaryAirportCost};
            row.elapsed=em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
            int bankCount=0;
            using(var banks=em.CreateEntityQuery(typeof(FactionTacticalMaterialsComponent)))
            using(var values=banks.ToComponentDataArray<FactionTacticalMaterialsComponent>(Allocator.Temp))
                foreach(var bank in values)if(bank.FactionId==1){bankCount++;row.materials=bank.Current;row.capacity=bank.Capacity;row.lifetimeFabricated=bank.LifetimeFabricated;
                    row.lifetimeImported=bank.LifetimeImported;row.lifetimeRewarded=bank.LifetimeRewarded;row.lifetimeSpent=bank.LifetimeSpent;row.productionSpent=bank.LifetimeProductionSpent;row.constructionSpent=bank.LifetimeConstructionSpent;}
            if(bankCount!=1)throw new InvalidOperationException("Unique native player Materials bank required");
            using(var setupQuery=em.CreateEntityQuery(typeof(SkirmishResolvedSetupRecord))){
                if(setupQuery.CalculateEntityCount()!=1)throw new InvalidOperationException("Unique native setup required");
                var setup=em.GetComponentObject<SkirmishResolvedSetupRecord>(setupQuery.GetSingletonEntity()).Setup;
                row.catalog=setup.CatalogId;row.definition=setup.DefinitionId;row.seed=setup.Seed;
                if(row.catalog!="S005"||row.definition!="skirmish.s005"||row.seed!=155926)throw new InvalidOperationException("Actual normal Menu S005 binding differs");}
            using(var airports=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishStructureIdentityComponent),typeof(RuntimeBuildingCombatInfo),typeof(UnitHealth)))
            using(var entities=airports.ToEntityArray(Allocator.Temp))
                foreach(var entity in entities){var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);var identity=em.GetComponentData<SkirmishStructureIdentityComponent>(entity);
                    if(own.FactionId!=1||!own.SessionId.Equals(state.SessionId)||identity.Producer!=SkirmishProducerKind.Airport||own.CapacityReleased!=0||em.GetComponentData<UnitHealth>(entity).Current<=0)continue;
                    if(row.airportIndex!=0)throw new InvalidOperationException("Unique normal paid Airport required");
                    row.airportIndex=entity.Index;row.airportVersion=entity.Version;row.airportStable=own.StableObjectId.ToString();row.airportHp=em.GetComponentData<UnitHealth>(entity).Current;
                    row.producerRuntimeId=em.GetComponentData<RuntimeBuildingCombatInfo>(entity).RuntimeBuildingId;
                    if(own.IsStructure==0||identity.DesignatedBase!=0||row.airportStable!="constructed."+row.producerRuntimeId||
                       em.GetComponentData<RuntimeBuildingCombatInfo>(entity).OwnerFactionId!=1||!em.HasComponent<SkirmishSharedActorTag>(entity))throw new InvalidOperationException("Airport is not a newly constructed owned facility");}
            if(em.HasBuffer<SkirmishProductionReservation>(session))foreach(var paid in em.GetBuffer<SkirmishProductionReservation>(session,true)){
                if(paid.FactionId==1&&paid.Role==SkirmishRoleKind.Strike&&paid.MaterialsPaid>0)row.stockStrikeReservations++;
                if(paid.FactionId!=1||paid.Role!=SkirmishRoleKind.TransportPlane||paid.Phase==SkirmishReservationPhase.Released)continue;
                if(row.reservation!=0&&row.reservation!=paid.ReservationId)throw new InvalidOperationException("Unique paid Plane reservation required");
                row.reservation=paid.ReservationId;row.role=paid.Role.ToString();row.phase=paid.Phase.ToString();row.paid=paid.MaterialsPaid;row.members=paid.MemberCount;
                row.delivered=paid.DeliveredMembers;row.remaining=paid.RemainingMembers;
                if(paid.MaterialsPaid!=440||paid.MemberCount!=1||paid.ProducerRuntimeId!=row.producerRuntimeId)throw new InvalidOperationException("Exact normal paid Plane440/owned Airport binding differs");
            }
            if(row.reservation!=0)using(var actors=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent),typeof(UnitHealth),typeof(UnitAirComponent),typeof(LocalTransform)))
            using(var entities=actors.ToEntityArray(Allocator.Temp))foreach(var entity in entities){var own=em.GetComponentData<SkirmishAttemptOwnedComponent>(entity);
                if(own.ReservationId!=row.reservation||own.FactionId!=1||!own.SessionId.Equals(state.SessionId)||own.CapacityReleased!=0)continue;
                if(row.actorIndex!=0||em.GetComponentData<SkirmishUnitRoleComponent>(entity).Role!=SkirmishRoleKind.TransportPlane)throw new InvalidOperationException("Exact unique native Plane actor required");
                var air=em.GetComponentData<UnitAirComponent>(entity);row.actorIndex=entity.Index;row.actorVersion=entity.Version;row.stable=own.StableObjectId.ToString();
                row.actorHp=em.GetComponentData<UnitHealth>(entity).Current;row.actorNativeShared=em.HasComponent<SkirmishSharedActorTag>(entity);
                row.homeInitialized=air.HomeInitialized!=0;row.usesRunway=air.UsesRunway!=0;row.homeDistance=Vector3.Distance(em.GetComponentData<LocalTransform>(entity).Position,air.HomePosition);
                row.transit=em.HasComponent<UnitSpawnTransitTag>(entity)?1:0;
            }
            return row;
        }
        // Actuation is limited to the existing driver and public presented catalog controls.
        static void DriveNormalPlanePurchase(double now,NormalPlaneLineageRow observation)
        {
            if(playerResumeTouch.IsBusy||now<playerResumeDue)return;
            var drawer=UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            var catalog=UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
            var controls=UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
            bool planeCard=drawer!=null&&drawer.IsOpen;
            bool productionAction=planeCard&&catalog!=null&&catalog.TryReadSelectedRequest(out var selected,out int cost,out var failure,out _)&&
                selected=="Unit_Veh_Plane_Transport"&&cost==440&&failure==BuildingUiCommandFailure.None;
            var button=planeCard?catalog?.ResolveCatalogTarget(BuildDrawerCategory.Aircrafts,"Unit_Veh_Plane_Transport"):controls?.BuildButton;
            if(PlayerResumeTap(button)){
                if(productionAction){observation.stage="PublicPlanePurchaseTouch";observation.reason="Presented selected Unit_Veh_Plane_Transport440 primary action via existing PlayerResumeTap";PlaneCanaryWrite(observation);}
            }else PlayerResumeScrollTo(button);
        }
        static void TickNormalPaidPlaneCanary()
        {
            // Placement may force Canvas before stage3 becomes stage4 in the same frame.
            // Observe every prelease callback; only the exclusive input owner is frame-throttled.
            if(!planeCanaryActive||planeCanaryTicking||(planeCanaryLeased&&Time.frameCount==planeCanaryLastFrame))return;
            planeCanaryTicking=true;
            planeCanaryLastFrame=Time.frameCount;
            try{
                if(EditorApplication.timeSinceStartup>planeCanaryDeadline)throw new InvalidOperationException("Bounded normal Plane canary timed out");
                if(!EditorApplication.isPlaying)return;var world=World.DefaultGameObjectInjectionWorld;if(world==null||!world.IsCreated)return;
                var em=world.EntityManager;if(!SkirmishLaunchProjection.TryGet(em,out var session,out var match))return;
                if(match.Phase==SkirmishPhase.Finished)throw new InvalidOperationException("Genuine match outcome occurred before scoped Plane proof");
                if(!SkirmishLaunchProjection.IsSimulationActive(em))return;
                if(!Mathf.Approximately(Time.timeScale,1))throw new InvalidOperationException("Normal speed required");
                planeCanaryAttempt=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId.ToString();
                if(playerResumeTouch==null)return;
                if(planeCanaryLeased)playerResumeTouch.Tick(Time.unscaledTime);
                if(playerResumeTouch.UnexpectedSamples!=0||playerResumeTouch.PhysicalInterventions!=0||AriaCommandEvidence.Violations!=0)throw new InvalidOperationException("Normal Touch audit failed");
                var row=ObserveNormalPlaneLineage(em,session);
                if(row.stockStrikeReservations!=0)throw new InvalidOperationException("Unexpected paid stock Strike reservation before/at Plane takeover");
                if(!planeCanaryBaseline&&airportPlacementStage==1){
                    if(row.airportIndex!=0)throw new InvalidOperationException("Airport predates paid construction baseline");
                    using(var configs=em.CreateEntityQuery(typeof(BuildingConfiguredSpawnableReadModel)))
                    {if(configs.CalculateEntityCount()!=1)throw new InvalidOperationException("Unique configured construction quote required");
                     int quotes=0;foreach(var quote in em.GetBuffer<BuildingConfiguredSpawnableReadModel>(configs.GetSingletonEntity(),true))
                         if(BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(quote.BuildingId.ToString())==BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey("Building_Airport")){
                             quotes++;planeCanaryAirportCost=quote.MaterialsCost;}
                     if(quotes!=1)throw new InvalidOperationException("Unique normalized native Airport quote required");}
                    if(planeCanaryAirportCost<=0)throw new InvalidOperationException("Exact native Airport Materials quote absent");
                    planeCanaryConstructionBefore=row.constructionSpent;planeCanaryBaseline=true;row.stage="AirportPaymentBaseline";row.airportCost=planeCanaryAirportCost;row.constructionBefore=row.constructionSpent;
                    PlaneCanaryWrite(row);
                }
                if(!planeCanaryLeased){
                    if(airportPlacementStage!=4)return;
                    if(!planeCanaryBaseline||!playerResumeProbeActive||!fixedWingCycleOnly||!airportPlacementOnly||playerResumeTouch.IsBusy)throw new InvalidOperationException("Exact settled public Airport handoff unavailable");
                    if(row.airportIndex<=0||row.constructionSpent-planeCanaryConstructionBefore!=planeCanaryAirportCost)throw new InvalidOperationException("Normal Airport payment/construction lineage differs");
                    // QA frame ownership only. Keep playerResumeProbeActive=true so the
                    // main harness remains suspended and native contacts retain PlayerResume audit scope.
                    var owners=UnityEngine.Object.FindObjectsByType<SkirmishS005PlayerResumeFrame>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
                    if(owners.Length!=1||!owners[0].enabled)throw new InvalidOperationException("Unique active QA input frame owner required");
                    planeCanaryInputOwner=owners[0];planeCanaryInputOwner.enabled=false;
                    planeCanaryLeased=true;row.stage="PaidAirportHandoff";row.normalMenu=true;PlaneCanaryWrite(row);return;
                }
                PlaneCanaryWrite(row);
                if(row.reservation==0){if(row.materials>=440)DriveNormalPlanePurchase(EditorApplication.timeSinceStartup,row);return;}
                // RemainingMembers tracks surviving live/reserved members, not undelivered count.
                if(row.phase!="Live"||row.delivered!=1||row.remaining!=1||row.actorIndex<=0||row.actorHp<=0||!row.actorNativeShared||!row.homeInitialized||!row.usesRunway||row.transit!=0||row.homeDistance>1.5f||playerResumeTouch.IsBusy||playerResumeTouch.CompletedGestures!=playerResumeTouch.DispatchedGestures)return;
                row.stage="PaidPlaneDeliveredAtHome";PlaneCanaryWrite(row);
                Debug.Log("[S005NormalPaidPlaneLineage] result=Passed scope=NormalAirportPlaneDeliveryOnly fullMissionCertified=0 playerReady=0");
                EndNormalPaidPlaneCanary(false,"normalPaidPlaneLineageOnly");
            }catch(Exception error){EndNormalPaidPlaneCanary(true,error.ToString());}
            finally{planeCanaryTicking=false;}
        }
    }
}
#endif
