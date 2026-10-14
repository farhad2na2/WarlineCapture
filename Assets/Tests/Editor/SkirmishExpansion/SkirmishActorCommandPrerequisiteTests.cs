#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Game.Components;
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
    // Small owned ECS Worlds: isolated geometry/actors, no live mission, assets or Editor Play.
    public sealed class SkirmishActorCommandPrerequisiteTests
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

        [TestCase((byte)1)][TestCase((byte)2)]
        public void OwnedReturnUsesNativeLandingWithoutMoveToParking(byte faction)
        {
            using var f=new Fixture();var e=f.Carrier(faction,plane:true,airborne:true);
            var air=f.Em.GetComponentData<UnitAirComponent>(e);air.AttackRunActive=1;air.AttackRunExitPosition=new float3(2,3,4);f.Em.SetComponentData(e,air);
            f.Em.AddComponentData(e,new UnitCombat{AutoEngage=1});f.Em.AddComponentData(e,new UnitTarget{Cell=new int2(20,20)});
            Assert.That(UnitReturnToBaseCommandService.TryIssueOwned(f.Em,f.Session,faction,e),Is.True);
            air=f.Em.GetComponentData<UnitAirComponent>(e);Assert.That(air.ReturningHome,Is.EqualTo(1));Assert.That(air.AttackRunActive,Is.Zero);
            Assert.That(air.HomeCell,Is.EqualTo(new int2(12,12)));Assert.That(f.Em.GetComponentData<UnitCombat>(e).AutoEngage,Is.Zero);
            Assert.That(f.Em.HasComponent<UnitTarget>(e),Is.False);Assert.That(f.Em.HasComponent<UnitPathRequest>(e),Is.False);
        }
        [Test]
        public void PlayerReturnAndOwnedReturnProduceSameNativeState()
        {
            using var f=new Fixture();var legacy=f.Carrier(1,false,plane:true,airborne:true);var owned=f.Carrier(1,true,plane:true,airborne:true);
            using var buildings=f.Em.CreateEntityQuery(typeof(BuildingRuntimeStateTag));
            Assert.That(UnitReturnToBaseCommandService.TryIssue(f.Em,f.Queue,buildings,legacy),Is.True);
            Assert.That(UnitReturnToBaseCommandService.TryIssueOwned(f.Em,f.Session,1,owned),Is.True);
            Assert.That(f.Em.GetComponentData<UnitAirComponent>(legacy),Is.EqualTo(f.Em.GetComponentData<UnitAirComponent>(owned)));
        }
        [TestCase("foreign")][TestCase("attempt")][TestCase("dead")][TestCase("paused")][TestCase("inactive")]
        public void UnauthorizedReturnDoesNotAlterAircraft(string rejection)
        {
            using var f=new Fixture();var e=f.Carrier(2,plane:true,airborne:true);Invalidate(f,e,rejection);
            var before=f.Em.GetComponentData<UnitAirComponent>(e);
            Assert.That(UnitReturnToBaseCommandService.TryIssueOwned(f.Em,f.Session,2,e),Is.False);
            Assert.That(f.Em.GetComponentData<UnitAirComponent>(e),Is.EqualTo(before));
        }
        [TestCase((byte)1)][TestCase((byte)2)]
        public void OwnedBoardQueuesNativePassengerOrderButDoesNotInventLoadedCargo(byte faction)
        {
            using var f=new Fixture();var carrier=f.Carrier(faction);var passenger=f.Actor(faction,new int2(16,12));
            var result=f.Board(faction,carrier,passenger);Assert.That(result.Accepted,Is.True,result.ReasonCode.ToString());
            Assert.That(f.Em.GetComponentData<UnitTransportBoardingTarget>(passenger).Transport,Is.EqualTo(carrier));
            Assert.That(f.Em.GetBuffer<UnitTransportPassengerElement>(carrier).Length,Is.Zero);
            Assert.That(f.Em.HasComponent<UnitTransportPassenger>(passenger),Is.False);
        }
        [TestCase("foreign")][TestCase("attempt")][TestCase("dead")][TestCase("released")][TestCase("full")]
        public void OwnedBoardRejectsIllegalPassengerOrFullNativeCapacity(string rejection)
        {
            using var f=new Fixture();var carrier=f.Carrier(2);var passenger=f.Actor(2,new int2(16,12));
            if(rejection=="full")f.LoadFixtureCargo(carrier,f.Actor(2,new int2(20,20)));else Invalidate(f,passenger,rejection);
            int count=f.Em.GetBuffer<UnitTransportPassengerElement>(carrier).Length;
            var result=f.Board(2,carrier,passenger);Assert.That(result.Accepted,Is.False);
            if(rejection=="full")Assert.That(result.ReasonCode,Is.EqualTo(TacticalCommandReasonCode.TransportFull));
            Assert.That(f.Em.HasComponent<UnitTransportBoardingTarget>(passenger),Is.False);
            Assert.That(f.Em.GetBuffer<UnitTransportPassengerElement>(carrier).Length,Is.EqualTo(count));
        }
        [Test]
        public void OwnedPlaneAcceptsNativeVehicleCargoKindAndWeight()
        {
            using var f=new Fixture();var carrier=f.Carrier(2,plane:true);var passenger=f.Actor(2,new int2(18,12),vehicle:true);
            var result=f.Board(2,carrier,passenger);Assert.That(result.Accepted,Is.True,result.ReasonCode.ToString());
            var target=f.Em.GetComponentData<UnitTransportBoardingTarget>(passenger);
            Assert.That(target.PassengerKind,Is.EqualTo(UnitTransportPassengerKind.Vehicle));Assert.That(target.CargoWeight,Is.EqualTo(9));
        }
        [Test]
        public void FactionTwoAirPickupUsesRealSharedLandingPlanner()
        {
            using var f=new Fixture();var carrier=f.Carrier(2,airborne:true);var passenger=f.Actor(2,new int2(22,22));
            var before=f.Em.GetComponentData<UnitAirComponent>(carrier);
            var result=f.Board(2,carrier,passenger);Assert.That(result.Accepted,Is.True,result.ReasonCode.ToString());
            var after=f.Em.GetComponentData<UnitAirComponent>(carrier);
            Assert.That(after.HomeCell,Is.Not.EqualTo(before.HomeCell));Assert.That(after.HomeInitialized,Is.EqualTo(1));
            Assert.That(f.Em.GetComponentData<UnitTarget>(carrier).Cell,Is.EqualTo(after.HomeCell));
            Assert.That(f.Em.GetComponentData<UnitTransportBoardingTarget>(passenger).Transport,Is.EqualTo(carrier));
        }
        [Test]
        public void PlayerConvenienceBoardRemainsPlayerOnlyAndAllowsLegacyActors()
        {
            using var f=new Fixture();var carrier=f.Carrier(1,false);var passenger=f.Actor(1,new int2(16,12),false);
            var result=f.Commands.TryIssueBoardSelectedTransportOrderToPassenger(f.Em,carrier,passenger,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem());
            Assert.That(result.Accepted,Is.True,result.ReasonCode.ToString());
            var enemy=f.Carrier(2,false);var enemyPassenger=f.Actor(2,new int2(16,12),false);
            result=f.Commands.TryIssueBoardSelectedTransportOrderToPassenger(f.Em,enemy,enemyPassenger,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem());
            Assert.That(result.Accepted,Is.False);Assert.That(result.ReasonCode,Is.EqualTo(TacticalCommandReasonCode.InvalidTransport));
        }
        [Test]
        public void PlayerAirPickupWithoutExpandedOwnershipRetainsNativeBehavior()
        {
            using var f=new Fixture();var carrier=f.Carrier(1,false,airborne:true);var passenger=f.Actor(1,new int2(22,22),false);
            var before=f.Em.GetComponentData<UnitAirComponent>(carrier);
            var result=f.Commands.TryIssueBoardSelectedTransportOrderToPassenger(f.Em,carrier,passenger,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem());
            Assert.That(result.Accepted,Is.True,result.ReasonCode.ToString());
            Assert.That(f.Em.GetComponentData<UnitAirComponent>(carrier).HomeCell,Is.Not.EqualTo(before.HomeCell));
            Assert.That(f.Em.GetComponentData<UnitTransportBoardingTarget>(passenger).Transport,Is.EqualTo(carrier));
        }
        [TestCase((byte)1)][TestCase((byte)2)]
        public void OwnedLandedPlaneUnloadUsesNativeGroundExitWithoutAirdrop(byte faction)
        {
            using var f=new Fixture();var carrier=f.Carrier(faction,plane:true);var passenger=f.Actor(faction,new int2(0,0));f.LoadFixtureCargo(carrier,passenger);
            Assert.That(f.Unload(faction,carrier,out var reason),Is.True,reason.ToString());
            Assert.That(f.Em.GetBuffer<UnitTransportPassengerElement>(carrier).Length,Is.Zero);
            Assert.That(f.Em.HasComponent<UnitTransportPassenger>(passenger),Is.False);Assert.That(f.Em.HasComponent<Disabled>(passenger),Is.False);
            Assert.That(f.Em.HasComponent<UnitTransportAirdropRequest>(carrier),Is.False);
            Assert.That(f.Em.HasComponent<UnitTransportParachuteDropComponent>(passenger),Is.False);
            Assert.That(f.Em.GetComponentData<UnitGrid>(passenger).Cell,Is.Not.EqualTo(new int2(0,0)));
        }
        [TestCase("foreign")][TestCase("attempt")][TestCase("dead")]
        public void OwnedUnloadRejectsUnauthorizedLoadedCargoWithoutPartialRelease(string rejection)
        {
            using var f=new Fixture();var carrier=f.Carrier(2,plane:true);var passenger=f.Actor(2,new int2(0,0));f.LoadFixtureCargo(carrier,passenger);Invalidate(f,passenger,rejection);
            Assert.That(f.Unload(2,carrier,out _),Is.False);
            Assert.That(f.Em.GetBuffer<UnitTransportPassengerElement>(carrier).Length,Is.EqualTo(1));
            Assert.That(f.Em.HasComponent<UnitTransportPassenger>(passenger),Is.True);Assert.That(f.Em.HasComponent<Disabled>(passenger),Is.True);
        }
        static void Invalidate(Fixture f,Entity actor,string reason)
        {
            if(reason=="foreign")f.Em.SetComponentData(actor,new Faction{Id=1});
            if(reason=="dead")f.Em.SetComponentData(actor,new UnitHealth{Current=0,Max=100});
            if(reason=="attempt"||reason=="released") {var own=f.Em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);if(reason=="attempt")own.SessionId=new FixedString64Bytes("old-attempt");else own.CapacityReleased=1;f.Em.SetComponentData(actor,own);}
            if(reason=="paused")f.Em.SetComponentData(f.Session,new SkirmishObjectiveClockComponent{Playing=1,Paused=1});
            if(reason=="inactive"){var gate=f.Em.CreateEntity(typeof(RuntimeGameplayStateComponent));f.Em.SetComponentData(gate,new RuntimeGameplayStateComponent{PlayRequested=1,SimulationActive=0});}
        }
        public static Task<int> RunFocusedValidation()
        {
            try
            {
                var t=new SkirmishActorCommandPrerequisiteTests();
                foreach(byte faction in new byte[]{1,2}){t.OwnedReturnUsesNativeLandingWithoutMoveToParking(faction);t.OwnedBoardQueuesNativePassengerOrderButDoesNotInventLoadedCargo(faction);t.OwnedLandedPlaneUnloadUsesNativeGroundExitWithoutAirdrop(faction);}
                t.PlayerReturnAndOwnedReturnProduceSameNativeState();
                foreach(string x in new[]{"foreign","attempt","dead","paused","inactive"})t.UnauthorizedReturnDoesNotAlterAircraft(x);
                foreach(string x in new[]{"foreign","attempt","dead","released","full"})t.OwnedBoardRejectsIllegalPassengerOrFullNativeCapacity(x);
                t.OwnedPlaneAcceptsNativeVehicleCargoKindAndWeight();t.FactionTwoAirPickupUsesRealSharedLandingPlanner();t.PlayerConvenienceBoardRemainsPlayerOnlyAndAllowsLegacyActors();t.PlayerAirPickupWithoutExpandedOwnershipRetainsNativeBehavior();
                foreach(string x in new[]{"foreign","attempt","dead"})t.OwnedUnloadRejectsUnauthorizedLoadedCargoWithoutPartialRelease(x);
                Debug.Log("[SkirmishActorCommandPrerequisiteTests] result=Passed cases=24 isolatedNativeWorlds=24");
                return Task.FromResult(0);
            }
            catch(Exception error){Debug.LogException(error);return Task.FromResult(1);}
        }
    }
}
#endif
