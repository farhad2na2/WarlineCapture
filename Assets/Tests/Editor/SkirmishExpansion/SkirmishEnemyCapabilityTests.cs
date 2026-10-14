#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.Tactical.Contracts;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SkirmishEnemyCapabilityTests
    {
        sealed class Fixture : IDisposable
        {
            public readonly World World = new World("Shared-actor-command-prerequisites");
            public EntityManager Em => World.EntityManager;
            public readonly Entity Session, Grid, Queue;
            NativeArray<int> counts; NativeArray<byte> friendly;
            NativeBitArray blocked, occupied; NativeList<int2> pool;
            public TransportBoardingCommandSystem Commands;
            public Fixture()
            {
                const int width=40,height=40,size=width*height;
                counts=new NativeArray<int>(size,Allocator.Persistent);
                friendly=new NativeArray<byte>(size,Allocator.Persistent);
                blocked=new NativeBitArray(size,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                occupied=new NativeBitArray(size,Allocator.Persistent,NativeArrayOptions.ClearMemory);
                pool=new NativeList<int2>(1024,Allocator.Persistent);
                for(int i=0;i<size;i++)friendly[i]=byte.MaxValue;
                Grid=Em.CreateEntity(typeof(GridConfig),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent),typeof(PathPoolComponent));
                Em.SetComponentData(Grid,new GridConfig{Width=width,Height=height,CellSize=1,Origin=float3.zero});
                Em.SetComponentData(Grid,new DynamicBlockerComponent{GridSize=size,Counts=counts,Blocked=blocked,FriendlyPassFactionIds=friendly});
                Em.SetComponentData(Grid,new DynamicOccupancyComponent{GridSize=size,Occupied=occupied});
                Em.SetComponentData(Grid,new PathPoolComponent{Cells=pool});
                Em.AddBuffer<GridWalkable>(Grid);Em.AddBuffer<GridRoad>(Grid);Em.AddBuffer<GridRoadSidewalk>(Grid);Em.AddBuffer<GridRoadDirt>(Grid);
                var w=Em.GetBuffer<GridWalkable>(Grid);var r=Em.GetBuffer<GridRoad>(Grid);var s=Em.GetBuffer<GridRoadSidewalk>(Grid);var d=Em.GetBuffer<GridRoadDirt>(Grid);
                for(int i=0;i<size;i++){w.Add(new GridWalkable{Value=1});r.Add(default);s.Add(default);d.Add(default);}
                Session=Em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishObjectiveClockComponent));
                Em.SetComponentData(Session,new SkirmishExpandedSessionComponent{SessionId=new FixedString64Bytes("owned-attempt"),Phase=SkirmishSessionPhase.Playing});
                Em.SetComponentData(Session,new SkirmishObjectiveClockComponent{Playing=1});
                Queue=Em.CreateEntity(typeof(RespawnQueueTag),typeof(RespawnQueueComponent));
                Em.AddBuffer<RespawnFactionSpawnPoint>(Queue);
                Em.GetBuffer<RespawnFactionSpawnPoint>(Queue).Add(new RespawnFactionSpawnPoint{FactionId=1,SpawnCell=new int2(4,4)});
                Em.GetBuffer<RespawnFactionSpawnPoint>(Queue).Add(new RespawnFactionSpawnPoint{FactionId=2,SpawnCell=new int2(30,30)});
                World.SetTime(new TimeData(1,.1f));
            }
            public Entity Actor(byte faction,int2 cell,bool owned=true,bool vehicle=false)
            {
                var e=Em.CreateEntity(typeof(Faction),typeof(UnitGrid),typeof(UnitFootprint),typeof(UnitMove),typeof(UnitMovementBehavior),typeof(UnitHealth),typeof(LocalTransform),typeof(LocalToWorld),typeof(UnitSourcePrefabKey));
                Em.SetComponentData(e,new Faction{Id=faction});Em.SetComponentData(e,new UnitGrid{Cell=cell});
                Em.SetComponentData(e,new UnitFootprint{Size=vehicle?new int2(3,3):new int2(1,1)});
                Em.SetComponentData(e,new UnitMove{Speed=4,WalkSpeed=1.5f,RoadSpeedMultiplier=1,ArriveDistance=.05f});
                Em.SetComponentData(e,new UnitMovementBehavior{UsesVehicleMotion=(byte)(vehicle?1:0)});
                Em.SetComponentData(e,new UnitHealth{Current=100,Max=100});
                var pos=new float3(cell.x+.5f,0,cell.y+.5f);
                Em.SetComponentData(e,LocalTransform.FromPosition(pos));Em.SetComponentData(e,new LocalToWorld{Value=float4x4.Translate(pos)});
                Em.SetComponentData(e,new UnitSourcePrefabKey{Value=new FixedString64Bytes(vehicle?"Unit_Veh_APC_Fast":"Unit_Chr_Rifle")});
                Em.AddBuffer<UnitTransportHiddenVisualScale>(e);
                if(owned)Em.AddComponentData(e,new SkirmishAttemptOwnedComponent{SessionId=new FixedString64Bytes("owned-attempt"),StableObjectId=new FixedString64Bytes("actor-"+e.Index),FactionId=faction});
                return e;
            }
            public Entity Carrier(byte faction,bool owned=true,bool plane=false,bool airborne=false)
            {
                var e=Actor(faction,new int2(12,12),owned,true);
                Em.AddComponentData(e,new UnitTransportCapacity{SoldierCapacity=1});Em.AddBuffer<UnitTransportPassengerElement>(e);
                if(plane||airborne)
                {
                    Em.AddComponentData(e,new UnitAirMovement{CruiseHeight=8,RunwayTaxiSpeed=5});
                    Em.AddComponentData(e,new UnitAirComponent{HomeInitialized=1,HomeCell=new int2(12,12),HomePosition=new float3(12.5f,0,12.5f),Airborne=(byte)(airborne?1:0)});
                    Em.SetComponentData(e,new UnitSourcePrefabKey{Value=new FixedString64Bytes(plane?"Unit_Veh_Plane_Transport":"Unit_Veh_Helicopter_Transport")});
                    if(airborne)Em.SetComponentData(e,LocalTransform.FromPosition(new float3(12.5f,8,12.5f)));
                }
                if(plane)Em.AddComponentData(e,new UnitTransportCargoCapacity{SoldierCapacity=1,VehicleCapacity=1});
                return e;
            }
            public TransportBoardingCommandSystem.Result Board(byte faction,Entity transport,Entity passenger)
                => Commands.TryIssueOwnedBoardOrderToPassenger(Em,Session,faction,transport,passenger,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem());
            public void LoadFixtureCargo(Entity transport,Entity passenger)
            {
                // Isolated loaded input for the real native unload owner, not a fabricated command result.
                Em.AddComponentData(passenger,new UnitTransportPassenger{Transport=transport});Em.AddComponent<Disabled>(passenger);
                Em.GetBuffer<UnitTransportPassengerElement>(transport).Add(new UnitTransportPassengerElement{Passenger=passenger});
            }
            public bool Unload(byte faction,Entity carrier,out TacticalCommandReasonCode reason)
            {
                using var query=Em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent));
                return TransportBoardingCommandSystem.TryIssueOwnedDisembark(Em,Session,faction,carrier,new UnitTransportCapacitySystem(),new UnitMoveOrderSystem(),query,out reason);
            }
            public void Dispose(){Em.CompleteAllTrackedJobs();World.Dispose();pool.Dispose();occupied.Dispose();blocked.Dispose();friendly.Dispose();counts.Dispose();}
        }
        static SkirmishResolvedSetup Setup()
        {
            // This isolated policy deliberately uses its original 5–10 second test cadence.
            // Invoke the actual internal14-parameter snapshot constructor, including optional pads.
            var constructor=typeof(SkirmishEnemyPolicySnapshot).GetConstructor(
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,
                new[]{typeof(string),typeof(SkirmishDifficultyId),typeof(SkirmishReadinessStage),typeof(float),typeof(float),typeof(float),
                    typeof(float),typeof(float),typeof(byte),typeof(uint),typeof(SkirmishEnemyRouteSnapshot[]),typeof(uint),typeof(string),typeof(SkirmishEnemyConstructionPad[])},null);
            Assert.That(constructor,Is.Not.Null,"Actual policy snapshot constructor changed");
            var policy=(SkirmishEnemyPolicySnapshot)constructor.Invoke(new object[]{"test-owned",SkirmishDifficultyId.Regular,SkirmishReadinessStage.Field,
                1f,2f,5f,10f,0f,(byte)0,123u,Array.Empty<SkirmishEnemyRouteSnapshot>(),1u,"test-owned",Array.Empty<SkirmishEnemyConstructionPad>()});
            return new SkirmishResolvedSetup{EnemyFaction=2,PlayerFaction=1,DeadlineSeconds=1080,PlayerStagingWorldX=22,PlayerStagingWorldZ=22,EnemyPolicy=policy};
        }
        static void Paid(Fixture f,Entity actor,SkirmishRoleKind role,uint group,uint receipt)
        {
            f.Em.AddComponent<SkirmishSharedActorTag>(actor);
            f.Em.AddComponentData(actor,new CombatTargetPolicy{Visible=1});
            f.Em.AddComponentData(actor,new SkirmishUnitRoleComponent{Role=role,Category=SkirmishPopulationCategory.Air});
            f.Em.AddComponentData(actor,new SkirmishArmyGroupMembershipComponent{GroupId=group,Domain=SkirmishPopulationCategory.Air});
            var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);own.ReservationId=receipt;f.Em.SetComponentData(actor,own);
            if(!f.Em.HasBuffer<SkirmishProductionReservation>(f.Session))f.Em.AddBuffer<SkirmishProductionReservation>(f.Session);
            f.Em.GetBuffer<SkirmishProductionReservation>(f.Session).Add(new SkirmishProductionReservation{
                ReservationId=receipt,Role=role,FactionId=2,MemberCount=1,DeliveredMembers=1,ProductionGroupId=group,ProducerRuntimeId=7,Phase=SkirmishReservationPhase.Live});
            if(!f.Em.HasBuffer<SkirmishArmyGroupRecord>(f.Session))f.Em.AddBuffer<SkirmishArmyGroupRecord>(f.Session);
            f.Em.GetBuffer<SkirmishArmyGroupRecord>(f.Session).Add(new SkirmishArmyGroupRecord{GroupId=group,Role=role,FactionId=2,MemberCount=1,AliveCount=1});
        }
        static SkirmishEnemyGroupAllocation.Package Viable()
        {
            var p=new SkirmishEnemyGroupAllocation.Package();
            p.Assault.Add(new SkirmishEnemyPerceptionBuilder.Group{Id=10});p.Escort.Add(new SkirmishEnemyPerceptionBuilder.Group{Id=11});
            p.Reserve.Add(new SkirmishEnemyPerceptionBuilder.Group{Id=12});return p;
        }
        [Test]
        public void OwnProjectionRequiresSameAttemptAliveDeliveredReceipt()
        {
            using var f=new Fixture();var own=f.Carrier(2,plane:true);Paid(f,own,SkirmishRoleKind.Fighter,1,1);
            var foreign=f.Carrier(1,plane:true);Paid(f,foreign,SkirmishRoleKind.Fighter,2,2);
            var stale=f.Carrier(2,plane:true);Paid(f,stale,SkirmishRoleKind.Fighter,3,3);
            var ownership=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(stale);ownership.SessionId=new FixedString64Bytes("old");f.Em.SetComponentData(stale,ownership);
            var pending=f.Carrier(2,plane:true);Paid(f,pending,SkirmishRoleKind.Fighter,4,4);
            var receipts=f.Em.GetBuffer<SkirmishProductionReservation>(f.Session);var row=receipts[3];row.Phase=SkirmishReservationPhase.Producing;row.DeliveredMembers=0;receipts[3]=row;
            var dead=f.Carrier(2,plane:true);Paid(f,dead,SkirmishRoleKind.Fighter,5,5);f.Em.SetComponentData(dead,new UnitHealth{Current=0,Max=100});
            var actors=SkirmishEnemyCapabilityService.ReadOwnActors(f.Em,f.Session);Assert.That(actors.Count,Is.EqualTo(1));Assert.That(actors[0].Entity,Is.EqualTo(own));
        }
        [TestCase("defend")][TestCase("short")][TestCase("unavailable")][TestCase("aircounter")]
        public void OptionalRoleSelectionPreservesForceTimeAndRealFeasibility(string mode)
        {
            var actors=new List<SkirmishEnemyCapabilityService.OwnActor>();var facts=new SkirmishEnemyPerceptionBuilder.Snapshot();
            facts.Visible.Add(new SkirmishEnemyPerceptionBuilder.Contact{Category=SkirmishPopulationCategory.Air});
            var role=SkirmishEnemyCapabilityService.ChooseDesiredRole(actors,facts,Viable(),mode=="defend",mode=="short"?179:900,
                candidate=>mode!="unavailable"&&candidate==SkirmishRoleKind.Fighter);
            Assert.That(role,Is.EqualTo(mode=="aircounter"?SkirmishRoleKind.Fighter:SkirmishRoleKind.None));
        }
        [Test]
        public void PartialCapabilityStateIsIncompatibleAndNeverReset()
        {
            using var f=new Fixture();f.Em.AddComponentData(f.Session,new SkirmishEnemyPlannerV1Component());
            f.Em.AddComponentData(f.Session,new SkirmishEnemyCapabilityStateV1Component{Version=1,NextOperationId=77});
            Assert.That(SkirmishEnemyCapabilityService.EnsureSession(f.Em,f.Session),Is.False);
            Assert.That(f.Em.GetComponentData<SkirmishEnemyCapabilityStateV1Component>(f.Session).NextOperationId,Is.EqualTo(77));
            Assert.That(f.Em.HasBuffer<SkirmishEnemyCapabilityOperationV1>(f.Session),Is.False);
        }
        [Test]
        public void OwnedDroneScanUsesNativeExactSourceAndGlobalUniqueToken()
        {
            using var f=new Fixture();var drone=f.Carrier(2,airborne:true);Paid(f,drone,SkirmishRoleKind.Drone,1,1);
            f.Em.SetComponentData(drone,new UnitSourcePrefabKey{Value=new FixedString64Bytes("Unit_Veh_Drone")});
            f.Em.AddComponentData(drone,new UnitCombat{CanAttack=0});f.Em.AddComponentData(f.Session,new SkirmishFogStateComponent{SharedFog=1,LastSeenExpireSeconds=20});
            var queue=f.Em.CreateEntity(typeof(ScanIntelCommandQueueComponent));f.Em.SetComponentData(queue,new ScanIntelCommandQueueComponent{LastRequestId=51});
            f.Em.AddBuffer<ScanIntelCommandRequestElement>(queue);f.Em.AddBuffer<ScanIntelCommandResultElement>(queue);
            Assert.That(SkirmishEnemyCapabilityCommands.TryScan(f.Em,f.Session,drone,new float3(22.5f,0,22.5f),out int request,out var reason),Is.True,reason.ToString());
            Assert.That(request,Is.EqualTo(52));var order=f.Em.GetComponentData<UnitScanOrder>(drone);
            Assert.That(order.SourceEntity,Is.EqualTo(drone));Assert.That(order.RequestId,Is.EqualTo(request));Assert.That(order.RadiusCells,Is.EqualTo(12));
            Assert.That(SkirmishEnemyCapabilityCommands.TryScan(f.Em,f.Session,drone,new float3(20.5f,0,20.5f),out _,out _),Is.False);
            Assert.That(f.Em.GetComponentData<ScanIntelCommandQueueComponent>(queue).LastRequestId,Is.EqualTo(52));
        }
        [TestCase("foreign")][TestCase("attempt")][TestCase("dead")][TestCase("paused")]
        public void OwnedScanRejectsUnauthorizedSourceWithoutRevealOrQueue(string mode)
        {
            using var f=new Fixture();var drone=f.Carrier(2,airborne:true);Paid(f,drone,SkirmishRoleKind.Drone,1,1);
            f.Em.AddComponentData(drone,new UnitCombat{CanAttack=0});f.Em.AddComponentData(f.Session,new SkirmishFogStateComponent{SharedFog=1});
            if(mode=="foreign")f.Em.SetComponentData(drone,new Faction{Id=1});
            if(mode=="attempt"){var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(drone);own.SessionId=new FixedString64Bytes("old");f.Em.SetComponentData(drone,own);}
            if(mode=="dead")f.Em.SetComponentData(drone,new UnitHealth{Current=0,Max=100});
            if(mode=="paused")f.Em.SetComponentData(f.Session,new SkirmishObjectiveClockComponent{Playing=1,Paused=1});
            Assert.That(SkirmishEnemyCapabilityCommands.TryScan(f.Em,f.Session,drone,new float3(22,0,22),out _,out _),Is.False);
            using var q=f.Em.CreateEntityQuery(typeof(ScanIntelCommandQueueComponent));Assert.That(q.IsEmptyIgnoreFilter,Is.True);
            Assert.That(f.Em.HasComponent<UnitScanOrder>(drone),Is.False);
        }
        [Test]
        public void OwnedPlaneDeployPreservesCargoUntilActualNativeDropOwner()
        {
            using var f=new Fixture();var carrier=f.Carrier(2,plane:true,airborne:true);var passenger=f.Actor(2,new int2(0,0));f.LoadFixtureCargo(carrier,passenger);
            var visual=f.Em.CreateEntity(typeof(Prefab),typeof(LocalTransform));f.Em.SetComponentData(visual,LocalTransform.Identity);
            f.Em.AddComponentData(carrier,new UnitTransportAirdropVisualPrefabs{SoldierParachuteVisualPrefab=visual,VehicleEmergencyDropVisualPrefab=visual});
            using var grid=f.Em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable),typeof(DynamicBlockerComponent),typeof(DynamicOccupancyComponent));
            Assert.That(TransportBoardingCommandSystem.TryIssueOwnedDeploy(f.Em,f.Session,2,carrier,new UnitTransportCapacitySystem(),new UnitMoveOrderSystem(),grid,new int2(22,22),out var reason),Is.True,reason.ToString());
            Assert.That(f.Em.HasComponent<UnitTransportAirdropRequest>(carrier),Is.True);
            Assert.That(f.Em.GetBuffer<UnitTransportPassengerElement>(carrier).Length,Is.EqualTo(1));
            Assert.That(f.Em.GetComponentData<UnitTransportPassenger>(passenger).Transport,Is.EqualTo(carrier));
        }
        static SkirmishNativeCheckpointSnapshotService.Snapshot Saved(Fixture f,Entity actor)
        {
            string Id(Entity entity)=>entity==f.Session?"$session":f.Em.HasComponent<SkirmishAttemptOwnedComponent>(entity)?
                f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).StableObjectId.ToString():"other";
            var actors=new List<SkirmishNativeCheckpointSnapshotService.Actor>();
            using var q=f.Em.CreateEntityQuery(new EntityQueryDesc{All=new[]{ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var entities=q.ToEntityArray(Allocator.Temp);
            foreach(var entity in entities)actors.Add(new SkirmishNativeCheckpointSnapshotService.Actor{Id=Id(entity),Parts=SkirmishNativeCheckpointParts.Capture(f.Em,entity,Id)});
            return new SkirmishNativeCheckpointSnapshotService.Snapshot{SessionId="owned-attempt",SessionParts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,Id),
                Actors=actors.ToArray(),Banks=Array.Empty<SkirmishNativeCheckpointSnapshotService.Bank>(),MapReferences=Array.Empty<SkirmishNativeCheckpointSnapshotService.MapReference>()};
        }
        [Test]
        public void NativePartsRoundTripPreservesStableOperationAndObjectiveTimers()
        {
            using var f=new Fixture();var drone=f.Carrier(2);Paid(f,drone,SkirmishRoleKind.Drone,1,1);f.Em.AddComponentData(f.Session,new SkirmishEnemyPlannerV1Component());
            Assert.That(SkirmishEnemyCapabilityService.EnsureSession(f.Em,f.Session),Is.True);
            f.Em.SetComponentData(f.Session,new SkirmishEnemyCapabilityStateV1Component{Version=1,NextOperationId=2});
            var op=new SkirmishEnemyCapabilityOperationV1{OperationId=1,GroupId=1,ReservationId=1,Role=SkirmishRoleKind.Drone,Kind=SkirmishEnemyCapabilityKind.Recon,
                Phase=SkirmishEnemyCapabilityPhase.Scanning,SourceStableId=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(drone).StableObjectId,
                ScanRequestId=52,DestinationX=22,DestinationZ=22,StartedAt=5,RetryAt=7,NextSurveyAt=80};
            f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(f.Session).Add(op);
            f.Em.SetComponentData(f.Session,new SkirmishObjectiveClockComponent{Playing=1,ElapsedSeconds=10});
            var saved=Saved(f,drone);Assert.That(SkirmishEnemyCapabilityCheckpoint.Validate(saved,Setup()),Is.True);
            var rebased=SkirmishNativeCheckpointParts.RebaseClocks(saved.SessionParts,500,700,900);
            Assert.That(SkirmishNativeCheckpointParts.TryReadBuffer(rebased,out SkirmishEnemyCapabilityOperationV1[] restored),Is.True);Assert.That(restored[0],Is.EqualTo(op));
            saved.SessionParts=rebased;Assert.That(SkirmishEnemyCapabilityCheckpoint.Validate(saved,Setup()),Is.True);
            var buffer=f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(f.Session);var corrupt=buffer[0];corrupt.GroupId=999;buffer[0]=corrupt;
            Assert.That(SkirmishEnemyCapabilityCheckpoint.Validate(Saved(f,drone),Setup()),Is.False);
        }
        [Test]
        public void PausedCapabilityTickCannotMutateSavedOperationsOrIssueOrders()
        {
            using var f=new Fixture();f.Em.AddComponentData(f.Session,new SkirmishEnemyPlannerV1Component());SkirmishEnemyCapabilityService.EnsureSession(f.Em,f.Session);
            var before=f.Em.GetComponentData<SkirmishEnemyCapabilityStateV1Component>(f.Session);
            f.Em.SetComponentData(f.Session,new SkirmishObjectiveClockComponent{Playing=1,Paused=1});
            var planner=f.Em.GetComponentData<SkirmishEnemyPlannerV1Component>(f.Session);
            var desired=SkirmishEnemyCapabilityService.Tick(f.Em,f.Session,Setup(),null,new SkirmishEnemyPerceptionBuilder.Snapshot(),Viable(),false,0,ref planner,out int dispatched);
            Assert.That(desired,Is.EqualTo(SkirmishRoleKind.None));Assert.That(dispatched,Is.Zero);
            Assert.That(f.Em.GetComponentData<SkirmishEnemyCapabilityStateV1Component>(f.Session),Is.EqualTo(before));
            Assert.That(f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(f.Session).Length,Is.Zero);
        }
        static SkirmishEnemyCapabilityService.OwnActor TransportInput(Fixture f,Entity carrier,Entity passenger,out SkirmishEnemyCapabilityOperationV1 op)
        {
            Paid(f,carrier,SkirmishRoleKind.ApcFast,1,1);Paid(f,passenger,SkirmishRoleKind.Rifle,2,2);
            f.Em.SetComponentData(passenger,new SkirmishArmyGroupMembershipComponent{GroupId=2,Domain=SkirmishPopulationCategory.Infantry});
            f.Em.AddComponentData(f.Session,new SkirmishEnemyPlannerV1Component());SkirmishEnemyCapabilityService.EnsureSession(f.Em,f.Session);
            f.Em.SetComponentData(f.Session,new SkirmishEnemyCapabilityStateV1Component{Version=1,NextOperationId=2});
            var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(carrier);
            op=new SkirmishEnemyCapabilityOperationV1{OperationId=1,GroupId=1,ReservationId=1,Role=SkirmishRoleKind.ApcFast,Kind=SkirmishEnemyCapabilityKind.Transport,
                SourceStableId=own.StableObjectId,DestinationX=30,DestinationZ=30,StartedAt=1};
            f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(f.Session).Add(op);
            f.Em.GetBuffer<SkirmishEnemyTransportPlanV1>(f.Session).Add(new SkirmishEnemyTransportPlanV1{OperationId=1,PassengerGroupId=2,
                PassengerStableId=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(passenger).StableObjectId});
            return new SkirmishEnemyCapabilityService.OwnActor{Entity=carrier,StableId=own.StableObjectId,GroupId=1,ReservationId=1,Role=SkirmishRoleKind.ApcFast,Carrier=true,
                Position=f.Em.GetComponentData<LocalTransform>(carrier).Position};
        }
        [Test]
        public void ThirdTransportRejectKeepsBoundedCleanupAndPersistedAuthority()
        {
            using var f=new Fixture();var carrier=f.Carrier(2);var passenger=f.Actor(2,new int2(13,12));TransportInput(f,carrier,passenger,out var op);
            op.Phase=SkirmishEnemyCapabilityPhase.Transit;
            for(int i=0;i<3;i++)SkirmishEnemyCapabilityService.Reject(ref op,7,10+i);
            Assert.That(op.Phase,Is.EqualTo(SkirmishEnemyCapabilityPhase.Aborting));Assert.That(op.RetryAt,Is.EqualTo(57));Assert.That(op.Retries,Is.Zero);
            var operations=f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(f.Session);operations[0]=op;
            Assert.That(SkirmishEnemyCapabilityService.ReservedGroups(f.Em,f.Session),Is.EquivalentTo(new uint[]{1,2}));
            f.Em.SetComponentData(f.Session,new SkirmishObjectiveClockComponent{Playing=1,ElapsedSeconds=12});
            Assert.That(SkirmishEnemyCapabilityCheckpoint.Validate(Saved(f,carrier),Setup()),Is.True);
            var parts=SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,e=>"unused");var rebuilt=f.Em.CreateEntity();
            SkirmishNativeCheckpointParts.Apply(f.Em,rebuilt,parts,id=>Entity.Null);
            Assert.That(f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(rebuilt)[0],Is.EqualTo(op));
            Assert.That(SkirmishEnemyCapabilityService.ReservedGroups(f.Em,rebuilt),Is.EquivalentTo(new uint[]{1,2}));
            op.Phase=SkirmishEnemyCapabilityPhase.Returning;
            for(int i=0;i<3;i++)SkirmishEnemyCapabilityService.Reject(ref op,7,60+i);
            Assert.That(op.Phase,Is.EqualTo(SkirmishEnemyCapabilityPhase.Returning));Assert.That(op.RetryAt,Is.EqualTo(107));
        }
        [Test]
        public void AbortingCancelsNativePendingBoardBeforeSeparateReturn()
        {
            using var f=new Fixture();var carrier=f.Carrier(2);var passenger=f.Actor(2,new int2(13,12));var actor=TransportInput(f,carrier,passenger,out var op);
            Assert.That(f.Board(2,carrier,passenger).Accepted,Is.True);Assert.That(f.Em.HasComponent<UnitTransportBoardingTarget>(passenger),Is.True);
            op.Phase=SkirmishEnemyCapabilityPhase.Aborting;
            Assert.That(SkirmishEnemyCapabilityService.StepTransport(f.Em,f.Session,actor,ref op,5,Setup(),new SkirmishEnemyPerceptionBuilder.Snapshot()),Is.True);
            Assert.That(f.Em.HasComponent<UnitTransportBoardingTarget>(passenger),Is.False);Assert.That(op.Phase,Is.EqualTo(SkirmishEnemyCapabilityPhase.Aborting));
            Assert.That(SkirmishEnemyCapabilityService.StepTransport(f.Em,f.Session,actor,ref op,10,Setup(),new SkirmishEnemyPerceptionBuilder.Snapshot()),Is.True);
            Assert.That(op.Phase,Is.EqualTo(SkirmishEnemyCapabilityPhase.Returning));Assert.That(f.Em.HasComponent<UnitTarget>(carrier),Is.True);
            var target=f.Em.GetComponentData<UnitTarget>(carrier);
            Assert.That(SkirmishEnemyCapabilityService.StepTransport(f.Em,f.Session,actor,ref op,11,Setup(),new SkirmishEnemyPerceptionBuilder.Snapshot()),Is.False);
            Assert.That(f.Em.GetComponentData<UnitTarget>(carrier),Is.EqualTo(target));
        }
        [Test]
        public void SettledReturningInputNativeUnloadReleasesGroupsOnlyAfterPhysicalCargo()
        {
            using var f=new Fixture();var carrier=f.Carrier(2);var passenger=f.Actor(2,new int2(0,0));var actor=TransportInput(f,carrier,passenger,out var op);
            var spawnPoints=f.Em.GetBuffer<RespawnFactionSpawnPoint>(f.Queue);spawnPoints[1]=new RespawnFactionSpawnPoint{FactionId=2,SpawnCell=new int2(12,12)};
            f.LoadFixtureCargo(carrier,passenger);op.Phase=SkirmishEnemyCapabilityPhase.Returning;op.DestinationX=actor.Position.x;op.DestinationZ=actor.Position.z;
            Assert.That(SkirmishEnemyCapabilityService.StepTransport(f.Em,f.Session,actor,ref op,5,Setup(),new SkirmishEnemyPerceptionBuilder.Snapshot()),Is.True);
            Assert.That(f.Em.HasComponent<UnitTransportPassenger>(passenger),Is.False);Assert.That(f.Em.HasComponent<Disabled>(passenger),Is.False);
            Assert.That(op.Phase,Is.EqualTo(SkirmishEnemyCapabilityPhase.Returning));
            Assert.That(SkirmishEnemyCapabilityService.StepTransport(f.Em,f.Session,actor,ref op,10,Setup(),new SkirmishEnemyPerceptionBuilder.Snapshot()),Is.False);
            Assert.That(op.Phase,Is.EqualTo(SkirmishEnemyCapabilityPhase.Cooldown));
            var operations=f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(f.Session);operations[0]=op;
            Assert.That(f.Em.GetBuffer<SkirmishEnemyTransportPlanV1>(f.Session).Length,Is.Zero);Assert.That(SkirmishEnemyCapabilityService.ReservedGroups(f.Em,f.Session).Count,Is.Zero);
        }
        [Test]
        public void RestoredAcceptedTransitDoesNotReissueMovementOrReleaseCargo()
        {
            using var f=new Fixture();var carrier=f.Carrier(2);var passenger=f.Actor(2,new int2(0,0));var actor=TransportInput(f,carrier,passenger,out var op);
            f.LoadFixtureCargo(carrier,passenger);op.Phase=SkirmishEnemyCapabilityPhase.Transit;op.CommandAccepted=1;op.LastIssuedAt=1;
            var operations=f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(f.Session);operations[0]=op;
            f.Em.SetComponentData(f.Session,new SkirmishObjectiveClockComponent{Playing=1,ElapsedSeconds=10});
            Assert.That(SkirmishEnemyCapabilityCheckpoint.Validate(Saved(f,carrier),Setup()),Is.True);
            var parts=SkirmishNativeCheckpointParts.RebaseClocks(SkirmishNativeCheckpointParts.Capture(f.Em,f.Session,e=>"unused"),100,200,300);
            var rebuilt=f.Em.CreateEntity();SkirmishNativeCheckpointParts.Apply(f.Em,rebuilt,parts,id=>Entity.Null);
            var restored=f.Em.GetBuffer<SkirmishEnemyCapabilityOperationV1>(rebuilt)[0];
            Assert.That(SkirmishEnemyCapabilityService.StepTransport(f.Em,rebuilt,actor,ref restored,10,Setup(),new SkirmishEnemyPerceptionBuilder.Snapshot()),Is.False);
            Assert.That(restored,Is.EqualTo(op));Assert.That(f.Em.HasComponent<UnitTarget>(carrier),Is.False);
            Assert.That(f.Em.GetComponentData<UnitTransportPassenger>(passenger).Transport,Is.EqualTo(carrier));
        }
        [TestCase("opening")][TestCase("notdue")][TestCase("due")]
        public void NativeReconHonorsOpeningAndSharedScoutCadence(string mode)
        {
            using var f=new Fixture();var drone=f.Carrier(2,airborne:true);Paid(f,drone,SkirmishRoleKind.Drone,1,1);
            f.Em.SetComponentData(drone,new UnitSourcePrefabKey{Value=new FixedString64Bytes("Unit_Veh_Drone")});
            var air=f.Em.GetComponentData<UnitAirComponent>(drone);air.Airborne=0;f.Em.SetComponentData(drone,air);
            f.Em.AddComponentData(drone,new UnitCombat{CanAttack=0});f.Em.AddComponentData(f.Session,new SkirmishFogStateComponent{SharedFog=1});
            f.Em.SetComponentData(f.Session,new SkirmishObjectiveClockComponent{Playing=1,ElapsedSeconds=100});
            var planner=new SkirmishEnemyPlannerV1Component{OpeningUntil=mode=="opening"?150:50,NextScoutAt=mode=="notdue"?110:90,RandomState=123};
            f.Em.AddComponentData(f.Session,planner);SkirmishEnemyCapabilityService.EnsureSession(f.Em,f.Session);
            SkirmishEnemyCapabilityService.Tick(f.Em,f.Session,Setup(),null,new SkirmishEnemyPerceptionBuilder.Snapshot(),Viable(),false,100,ref planner,out int dispatched);
            Assert.That(dispatched,Is.EqualTo(mode=="due"?1:0));Assert.That(f.Em.HasComponent<UnitScanOrder>(drone),Is.EqualTo(mode=="due"));
            if(mode=="due")Assert.That(planner.NextScoutAt,Is.InRange(105f,110f));
            else Assert.That(planner.NextScoutAt,Is.EqualTo(mode=="notdue"?110f:90f));
        }
        [Test]
        public void OpeningAllowsOnlyActualLocalVisibleAirDefense()
        {
            Assert.That(SkirmishEnemyCapabilityService.AllowsProactiveOrLocalDefense(false,true,float3.zero,new float3(40,0,0)),Is.True);
            Assert.That(SkirmishEnemyCapabilityService.AllowsProactiveOrLocalDefense(false,true,float3.zero,new float3(100,0,0)),Is.False);
            Assert.That(SkirmishEnemyCapabilityService.AllowsProactiveOrLocalDefense(false,false,float3.zero,new float3(40,0,0)),Is.False);
            Assert.That(SkirmishEnemyCapabilityService.AllowsProactiveOrLocalDefense(true,true,float3.zero,new float3(100,0,0)),Is.True);
        }
        [Test]
        public void ReplenishmentCensusCountsEmbarkedAndReservedOnceWithoutTacticalAllocation()
        {
            using var f=new Fixture();var carrier=f.Carrier(2);var passenger=f.Actor(2,new int2(0,0));TransportInput(f,carrier,passenger,out var op);f.LoadFixtureCargo(carrier,passenger);
            foreach(string kind in new[]{"foreign","old","dead","released"})
            {
                var extra=f.Actor(kind=="foreign"?(byte)1:(byte)2,new int2(2,2));
                f.Em.AddComponentData(extra,new SkirmishUnitRoleComponent{Role=SkirmishRoleKind.Rifle});f.Em.AddComponentData(extra,new SkirmishArmyGroupMembershipComponent{GroupId=3});
                var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(extra);if(kind=="old")own.SessionId=new FixedString64Bytes("old");if(kind=="released")own.CapacityReleased=1;f.Em.SetComponentData(extra,own);
                if(kind=="dead")f.Em.SetComponentData(extra,new UnitHealth{Current=0,Max=100});
            }
            var counts=SkirmishEnemyCapabilityService.ReadLivingRoleCounts(f.Em,f.Session);Assert.That(counts[SkirmishRoleKind.Rifle],Is.EqualTo(1));Assert.That(counts[SkirmishRoleKind.ApcFast],Is.EqualTo(1));
            var facts=new SkirmishEnemyPerceptionBuilder.Snapshot();facts.Groups.Add(new SkirmishEnemyPerceptionBuilder.Group{Id=2,Role=SkirmishRoleKind.Rifle,Members=1,Supply=4});facts.CombatSupply=4;
            SkirmishEnemyCapabilityService.RemoveReservedGroundGroups(f.Em,f.Session,facts);Assert.That(facts.Groups.Count,Is.Zero);Assert.That(facts.CombatSupply,Is.Zero);
            // Isolate authored-replenishment rule from optional new-scout demand.
            facts.Groups.Add(new SkirmishEnemyPerceptionBuilder.Group{Id=10,Role=SkirmishRoleKind.Rocketeer,Members=1,Domains=SkirmishTargetDomain.Structure,Health=100,MaximumHealth=100});
            var package=Viable();package.Scout=new SkirmishEnemyPerceptionBuilder.Group{Id=13,Role=SkirmishRoleKind.Rifle};
            var setup=Setup();setup.Forces=new[]{new SkirmishResolvedForceEntry{FactionId=2,RoleKind=SkirmishRoleKind.Rifle,Quantity=1}};
            Assert.That(SkirmishEnemyCommanderService.WantedRole(facts,package,setup),Is.EqualTo(SkirmishRoleKind.Rifle));
            Assert.That(SkirmishEnemyCommanderService.WantedRole(facts,package,setup,counts),Is.EqualTo(SkirmishRoleKind.None));
        }
        public static Task<int> RunFocusedValidation()
        {
            try
            {
                var t=new SkirmishEnemyCapabilityTests();t.OwnProjectionRequiresSameAttemptAliveDeliveredReceipt();
                foreach(string x in new[]{"defend","short","unavailable","aircounter"})t.OptionalRoleSelectionPreservesForceTimeAndRealFeasibility(x);
                t.PartialCapabilityStateIsIncompatibleAndNeverReset();t.OwnedDroneScanUsesNativeExactSourceAndGlobalUniqueToken();
                foreach(string x in new[]{"foreign","attempt","dead","paused"})t.OwnedScanRejectsUnauthorizedSourceWithoutRevealOrQueue(x);
                t.OwnedPlaneDeployPreservesCargoUntilActualNativeDropOwner();t.NativePartsRoundTripPreservesStableOperationAndObjectiveTimers();t.PausedCapabilityTickCannotMutateSavedOperationsOrIssueOrders();
                t.ThirdTransportRejectKeepsBoundedCleanupAndPersistedAuthority();t.AbortingCancelsNativePendingBoardBeforeSeparateReturn();
                t.SettledReturningInputNativeUnloadReleasesGroupsOnlyAfterPhysicalCargo();t.RestoredAcceptedTransitDoesNotReissueMovementOrReleaseCargo();
                foreach(string x in new[]{"opening","notdue","due"})t.NativeReconHonorsOpeningAndSharedScoutCadence(x);t.OpeningAllowsOnlyActualLocalVisibleAirDefense();
                t.ReplenishmentCensusCountsEmbarkedAndReservedOnceWithoutTacticalAllocation();
                Debug.Log("[SkirmishEnemyCapabilityTests] result=Passed cases=23");return Task.FromResult(0);
            }
            catch(Exception error){Debug.LogException(error);return Task.FromResult(1);}
        }
    }
}
#endif
