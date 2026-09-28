#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SupportParatrooperValidation
    {
        internal sealed class Encounter : IDisposable
        {
            internal readonly SupportValidationFixture F=new SupportValidationFixture();
            internal Entity Grid,Infantry,Canopy,Depot;
            internal SupportRequestElement Request;
            private BlobAssetReference<MapSurfaceBlob> surface;
            internal EntityManager Em=>F.Em;
            internal Encounter()
            {
                Request=SupportStrikeValidation.Prepare(F,out _);Request.Kind=SupportAbilityKind.Paratroopers;Request.Target=Entity.Null;Request.TargetKind=SupportTargetKind.LandingZone;Request.Position=new float3(10.5f,0,10.5f);Request.Cell=new int2(10,10);
                var old=Em.GetComponentData<SupportCatalogComponent>(F.Root);old.Blob.Dispose();
                using(var b=new BlobBuilder(Allocator.Temp)){ref var blob=ref b.ConstructRoot<SupportCatalogBlob>();var entries=b.Allocate(ref blob.Abilities,4);for(int i=0;i<4;i++)entries[i]=new SupportAbilityDefinition {Kind=(SupportAbilityKind)(i+1),FuelCost=i==2?6:4,Charges=1,ApproachSeconds=5,AircraftSourceKey="jet-test"};Em.SetComponentData(F.Root,new SupportCatalogComponent {Blob=b.CreateBlobAssetReference<SupportCatalogBlob>(Allocator.Persistent),Revision=1,OwnsBlob=1});}
                var policy=Em.GetComponentData<SupportMissionPolicyComponent>(F.Root);policy.PopulationCeiling=12;Em.SetComponentData(F.Root,policy);
                Em.GetBuffer<SupportAbilityStateElement>(F.Root).Add(new SupportAbilityStateElement {Kind=SupportAbilityKind.Paratroopers,ChargesRemaining=1,Enabled=1,StateVersion=1});
                Infantry=F.Unit(float3.zero,1,120);Em.AddComponent<Prefab>(Infantry);Em.AddComponent<UnitMovementBehavior>(Infantry);Em.AddComponentData(Infantry,new UnitFootprint {Size=new int2(1)});Em.AddComponentData(Infantry,new UnitMove {Speed=4});
                Canopy=Em.CreateEntity(typeof(Prefab),typeof(LocalTransform));Em.SetComponentData(Canopy,LocalTransform.Identity);
                Em.AddComponentData(F.Root,new SupportPayloadBindingsComponent {InfantryPrefab=Infantry,ParachutePrefab=Canopy,InfantryCount=4});
                Grid=Em.CreateEntity(typeof(GridConfig));Em.SetComponentData(Grid,new GridConfig {Width=30,Height=30,CellSize=1});var walk=Em.AddBuffer<GridWalkable>(Grid);for(int i=0;i<900;i++)walk.Add(new GridWalkable {Value=1});
                using(var b=new BlobBuilder(Allocator.Temp)) {ref var blob=ref b.ConstructRoot<MapSurfaceBlob>();blob.CellSize=1;blob.Dimensions=new int2(30);blob.RuntimeEncoding=MapSurfaceRuntimeEncoding.Full;var cells=b.Allocate(ref blob.Cells,900);var samples=b.Allocate(ref blob.Samples,900);for(int i=0;i<900;i++){cells[i]=new MapSurfaceCell {FirstSurfaceIndex=i,SurfaceCount=1};samples[i]=new MapSurfaceSample {Cell=new int2(i%30,i/30),SurfaceId=i,Normal=math.up(),MovementMask=MapSurfaceMovementMask.AllGroundUnits|MapSurfaceMovementMask.AirGrounded,SurfaceType=MapSurfaceType.Terrain};}b.Allocate(ref blob.Connections,0);b.Allocate(ref blob.CompactSamples,0);surface=b.CreateBlobAssetReference<MapSurfaceBlob>(Allocator.Persistent);}
                var s=Em.CreateEntity();Em.AddComponentData(s,new MapSurfaceComponent {SurfaceBlob=surface,CellSize=1,Dimensions=new int2(30),HasSurfaceData=1});
                Em.SetComponentData(F.Root,new SupportPreviewComponent {PreviewId=1,Request=Request,Valid=1,FuelCost=6});Depot=F.Store(6);
            }
            internal Entity Commit(){Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(Em,F.Root,Request));return Em.GetBuffer<SupportReceiptElement>(F.Root)[0].Effect;}
            internal void Approach(Entity flight){var s=Em.GetComponentData<SupportSessionComponent>(F.Root);s.SimulationSeconds=5;Em.SetComponentData(F.Root,s);SupportFlightSystem.Advance(Em,flight);}
            internal void DropAt(double seconds){F.World.SetTime(new TimeData(seconds,.1f));var h=F.World.GetOrCreateSystem<UnitTransportAirdropSystem>();h.Update(F.World.Unmanaged);F.World.EntityManager.CompleteAllTrackedJobs();}
            public void Dispose(){F.Dispose();surface.Dispose();}
        }
        public static void RunFocusedValidation()
        {
            try {var t=new SupportParatrooperValidation();t.ManifestAndPopulationFailClosed();t.LiveQueuedAndReservedPopulationCountOnce();t.AllSlotsReservedAndRealParachutesLand();t.BlockedFirstReleaseRestoresExactly();t.PartialDeliveryDoesNotRefund();t.ExternalFlightRemovalDestroysHeldPassengers();t.PausedFlightPreservesReservation();t.ExternalPartialFlightRemovalKeepsReleasedSquadAndReceipt();t.ReleasedPassengerDeathDestroysCanopy();Debug.Log("[SupportParatrooperValidation] result=Passed tests=9");ValidationExit.Exit(0);}
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportParatrooperValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        [Test] public void ManifestAndPopulationFailClosed()
        {
            using var e=new Encounter();var p=e.Em.GetComponentData<SupportMissionPolicyComponent>(e.F.Root);p.PopulationCeiling=0;e.Em.SetComponentData(e.F.Root,p);Assert.AreEqual(SupportRejectionReason.NotReady,SupportTargetValidationUtilitySystemHelper.Validate(e.Em,e.F.Root,e.Request));p.PopulationCeiling=3;e.Em.SetComponentData(e.F.Root,p);Assert.AreEqual(SupportRejectionReason.PopulationFull,SupportTargetValidationUtilitySystemHelper.Validate(e.Em,e.F.Root,e.Request));p.PopulationCeiling=12;e.Em.SetComponentData(e.F.Root,p);e.Em.DestroyEntity(e.Canopy);Assert.AreEqual(SupportRejectionReason.NotReady,SupportTargetValidationUtilitySystemHelper.Validate(e.Em,e.F.Root,e.Request));Assert.AreEqual(6,SupportFuelTransactionSystem.Available(e.Em,1));
        }
        [Test] public void LiveQueuedAndReservedPopulationCountOnce()
        {
            using var e=new Encounter();var unit=e.F.Unit(float3.zero,1);e.Em.AddComponent<UnitMovementBehavior>(unit);var b=e.Em.CreateEntity();e.Em.AddBuffer<BuildingConfiguredUnitReadModel>(b).Add(new BuildingConfiguredUnitReadModel {UnitId="rifle"});e.Em.AddBuffer<BuildingRuntimeUnitProductionSummary>(b).Add(new BuildingRuntimeUnitProductionSummary {FactionId=1,UnitId="rifle",ProducedCount=100,QueuedCount=2});Assert.IsTrue(SupportLandingUtilitySystemHelper.CountPopulation(e.Em,1,out var count));Assert.AreEqual(3,count);e.Commit();Assert.IsTrue(SupportLandingUtilitySystemHelper.CountPopulation(e.Em,1,out count));Assert.AreEqual(7,count);
        }
        [Test] public void AllSlotsReservedAndRealParachutesLand()
        {
            using var e=new Encounter();var flight=e.Commit();using var entries=e.Em.GetBuffer<SupportPassengerReservationElement>(flight).ToNativeArray(Allocator.Temp);Assert.AreEqual(4,entries.Length);
            for(int i=0;i<4;i++){var u=entries[i].Passenger;Assert.IsTrue(e.Em.HasComponent<Disabled>(u));Assert.AreEqual(120,e.Em.GetComponentData<UnitHealth>(u).Max);Assert.AreEqual(1,e.Em.GetComponentData<Faction>(u).Id);for(int j=0;j<i;j++)Assert.IsFalse(math.all(e.Em.GetComponentData<SupportPassengerOwnerComponent>(u).LandingCell==e.Em.GetComponentData<SupportPassengerOwnerComponent>(entries[j].Passenger).LandingCell));}
            Assert.AreEqual(.25f,e.Em.GetComponentData<LocalTransform>(flight).Scale);e.Approach(flight);Assert.AreEqual(.25f,e.Em.GetComponentData<LocalTransform>(flight).Scale);for(int i=0;i<4;i++)e.DropAt(5+i*.7);
            var plane=e.Em.GetComponentData<LocalTransform>(flight);var firstDrop=e.Em.GetComponentData<UnitTransportParachuteDropComponent>(entries[0].Passenger);var expectedDoor=plane.Position+math.rotate(plane.Rotation,new float3(0,-.5f,-4)*plane.Scale);Assert.Less(math.distancesq(firstDrop.StartPosition.xz,expectedDoor.xz),.00001f,"Support passengers must leave the scaled source door, not an unscaled offset.");
            Assert.AreEqual(0,e.Em.GetComponentData<BuildingResourceStorageComponent>(e.Depot).StoredFuelBarrels);Assert.AreEqual(6,e.Em.GetBuffer<SupportReceiptElement>(e.F.Root)[0].SpentFuel);
            foreach(var entry in entries){Assert.IsTrue(e.Em.HasComponent<UnitTransportParachuteDropComponent>(entry.Passenger));Assert.IsFalse(e.Em.HasComponent<Disabled>(entry.Passenger));Assert.IsTrue(e.Em.Exists(e.Em.GetComponentData<UnitTransportParachuteDropComponent>(entry.Passenger).VisualEntity));Assert.IsTrue(e.Em.HasComponent<SupportPassengerTransitTag>(entry.Passenger));Assert.IsFalse(e.Em.HasComponent<CampaignMissionCombatSuppressedTag>(entry.Passenger));}
            var attacker=e.F.Unit(float3.zero,2);var targetPredicate=typeof(UnitAttackSystem).GetMethod("IsTargetAvailableForCombat",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);Assert.IsTrue((bool)targetPredicate.Invoke(null,new object[]{e.Em,attacker,entries[0].Passenger}),"Descending infantry must remain vulnerable to normal combat.");
            e.DropAt(12);e.DropAt(15);var lifecycle=e.F.World.CreateSystem<SupportPassengerLifecycleSystem>();lifecycle.Update(e.F.World.Unmanaged);foreach(var entry in entries){Assert.AreEqual(1,e.Em.GetComponentData<SupportPassengerOwnerComponent>(entry.Passenger).Landed);Assert.IsFalse(e.Em.HasComponent<CampaignMissionCombatSuppressedTag>(entry.Passenger));Assert.IsFalse(e.Em.HasComponent<SupportPassengerTransitTag>(entry.Passenger));Assert.IsFalse(e.Em.HasComponent<UnitTransportPassenger>(entry.Passenger));}
        }
        [Test] public void BlockedFirstReleaseRestoresExactly()
        {
            using var e=new Encounter();var flight=e.Commit();using var entries=e.Em.GetBuffer<SupportPassengerReservationElement>(flight).ToNativeArray(Allocator.Temp);var cell=e.Em.GetComponentData<SupportPassengerOwnerComponent>(entries[3].Passenger).LandingCell;var walk=e.Em.GetBuffer<GridWalkable>(e.Grid);walk[cell.x+cell.y*30]=new GridWalkable();e.Approach(flight);e.DropAt(5);Assert.AreEqual(6,SupportFuelTransactionSystem.Available(e.Em,1));Assert.AreEqual(1,e.Em.GetBuffer<SupportAbilityStateElement>(e.F.Root)[2].ChargesRemaining);foreach(var entry in entries)Assert.IsFalse(e.Em.Exists(entry.Passenger));Assert.AreEqual(SupportExecutionPhase.Aborted,e.Em.GetBuffer<SupportReceiptElement>(e.F.Root)[0].Phase);
        }
        [Test] public void PartialDeliveryDoesNotRefund()
        {
            using var e=new Encounter();var flight=e.Commit();using var entries=e.Em.GetBuffer<SupportPassengerReservationElement>(flight).ToNativeArray(Allocator.Temp);e.Approach(flight);e.DropAt(5);var cell=e.Em.GetComponentData<SupportPassengerOwnerComponent>(entries[1].Passenger).LandingCell;var walk=e.Em.GetBuffer<GridWalkable>(e.Grid);walk[cell.x+cell.y*30]=new GridWalkable();e.DropAt(6);Assert.AreEqual(0,SupportFuelTransactionSystem.Available(e.Em,1));Assert.AreEqual(0,e.Em.GetBuffer<SupportAbilityStateElement>(e.F.Root)[2].ChargesRemaining);Assert.IsTrue(e.Em.Exists(entries[0].Passenger));for(int i=1;i<4;i++)Assert.IsFalse(e.Em.Exists(entries[i].Passenger));Assert.AreEqual(SupportExecutionPhase.PartiallyResolved,e.Em.GetBuffer<SupportReceiptElement>(e.F.Root)[0].Phase);
        }
        [Test] public void ExternalFlightRemovalDestroysHeldPassengers()
        {
            using var e=new Encounter();var flight=e.Commit();using var entries=e.Em.GetBuffer<SupportPassengerReservationElement>(flight).ToNativeArray(Allocator.Temp);e.Em.DestroyEntity(flight);var h=e.F.World.CreateSystem<SupportFlightSystem>();h.Update(e.F.World.Unmanaged);h.Update(e.F.World.Unmanaged);foreach(var entry in entries)Assert.IsFalse(e.Em.Exists(entry.Passenger));Assert.AreEqual(6,SupportFuelTransactionSystem.Available(e.Em,1));Assert.AreEqual(1,e.Em.GetBuffer<SupportAbilityStateElement>(e.F.Root)[2].ChargesRemaining);
        }
        [Test] public void ExternalPartialFlightRemovalKeepsReleasedSquadAndReceipt()
        {
            using var e=new Encounter();var flight=e.Commit();using var entries=e.Em.GetBuffer<SupportPassengerReservationElement>(flight).ToNativeArray(Allocator.Temp);e.Approach(flight);e.DropAt(5);e.Em.DestroyEntity(flight);var h=e.F.World.CreateSystem<SupportFlightSystem>();h.Update(e.F.World.Unmanaged);h.Update(e.F.World.Unmanaged);
            Assert.IsTrue(e.Em.Exists(entries[0].Passenger));for(int i=1;i<4;i++)Assert.IsFalse(e.Em.Exists(entries[i].Passenger));Assert.AreEqual(SupportExecutionPhase.PartiallyResolved,e.Em.GetBuffer<SupportReceiptElement>(e.F.Root)[0].Phase);Assert.AreEqual(0,SupportFuelTransactionSystem.Available(e.Em,1));Assert.AreEqual(0,e.Em.GetBuffer<SupportAbilityStateElement>(e.F.Root)[2].ChargesRemaining);
        }
        [Test] public void PausedFlightPreservesReservation()
        {
            using var e=new Encounter();var flight=e.Commit();var s=e.Em.GetComponentData<SupportSessionComponent>(e.F.Root);s.Active=0;s.SimulationSeconds=8;e.Em.SetComponentData(e.F.Root,s);SupportFlightSystem.Advance(e.Em,flight);Assert.IsFalse(e.Em.HasComponent<UnitTransportAirdropRequest>(flight));Assert.AreEqual(6,e.Em.GetComponentData<BuildingResourceStorageComponent>(e.Depot).StoredFuelBarrels);Assert.AreEqual(0,SupportFuelTransactionSystem.Available(e.Em,1));
        }
        [Test] public void ReleasedPassengerDeathDestroysCanopy()
        {
            using var e=new Encounter();var flight=e.Commit();var passenger=e.Em.GetBuffer<SupportPassengerReservationElement>(flight)[0].Passenger;e.Approach(flight);e.DropAt(5);
            var canopy=e.Em.GetComponentData<UnitTransportParachuteDropComponent>(passenger).VisualEntity;
            Assert.AreEqual(canopy,e.Em.GetComponentData<SupportPassengerCleanupComponent>(passenger).Canopy);
            e.Em.DestroyEntity(passenger);var h=e.F.World.CreateSystem<SupportPassengerLifecycleSystem>();h.Update(e.F.World.Unmanaged);
            Assert.IsFalse(e.Em.Exists(canopy));Assert.IsFalse(e.Em.Exists(passenger));Assert.AreEqual(0,SupportFuelTransactionSystem.Available(e.Em,1));Assert.AreEqual(0,e.Em.GetBuffer<SupportAbilityStateElement>(e.F.Root)[2].ChargesRemaining);
        }
    }
}
#endif
