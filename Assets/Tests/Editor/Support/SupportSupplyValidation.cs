#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
namespace Game.Tests.Editor
{
    public sealed class SupportSupplyValidation
    {
        private sealed class Supply : IDisposable
        {
            internal readonly SupportParatrooperValidation.Encounter E=new SupportParatrooperValidation.Encounter();
            internal EntityManager Em=>E.Em;internal Entity Root=>E.F.Root;internal Entity Store;internal SupportRequestElement Request;
            internal Supply()
            {
                var old=Em.GetComponentData<SupportCatalogComponent>(Root);old.Blob.Dispose();using(var b=new BlobBuilder(Allocator.Temp)){ref var blob=ref b.ConstructRoot<SupportCatalogBlob>();var entries=b.Allocate(ref blob.Abilities,4);for(int i=0;i<4;i++)entries[i]=new SupportAbilityDefinition {Kind=(SupportAbilityKind)(i+1),FuelCost=i==3?3:6,Charges=1,ApproachSeconds=5,Materials=i==3?40:0,AircraftSourceKey="jet-test"};Em.SetComponentData(Root,new SupportCatalogComponent {Blob=b.CreateBlobAssetReference<SupportCatalogBlob>(Allocator.Persistent),Revision=1,OwnsBlob=1});}
                var bindings=Em.GetComponentData<SupportPayloadBindingsComponent>(Root);bindings.CratePrefab=Em.CreateEntity(typeof(Prefab),typeof(LocalTransform));Em.SetComponentData(bindings.CratePrefab,LocalTransform.Identity);Em.SetComponentData(Root,bindings);
                Em.GetBuffer<SupportAbilityStateElement>(Root).Add(new SupportAbilityStateElement {Kind=SupportAbilityKind.Supply,ChargesRemaining=1,Enabled=1,StateVersion=1});
                Store=Em.CreateEntity(typeof(FactionTacticalMaterialsComponent));Em.SetComponentData(Store,new FactionTacticalMaterialsComponent {FactionId=1,Current=590,Capacity=600,Version=1});
                Em.AddComponent<SupportCollectionFeedbackComponent>(Root);Em.AddComponent<SupportSupplyReadModelComponent>(Root);Em.AddBuffer<SupportCollectRequestElement>(Root);
                Request=E.Request;Request.Kind=SupportAbilityKind.Supply;Em.SetComponentData(Root,new SupportPreviewComponent {PreviewId=1,Request=Request,Valid=1,FuelCost=3});
            }
            internal Entity Commit(){Assert.AreEqual(SupportRejectionReason.None,SupportAbilityRequestSystem.Process(Em,Root,Request));return Em.GetBuffer<SupportReceiptElement>(Root)[0].Effect;}
            internal Entity Land()
            {
                var flight=Commit();var s=Em.GetComponentData<SupportSessionComponent>(Root);s.SimulationSeconds=5;Em.SetComponentData(Root,s);SupportFlightSystem.Advance(Em,flight,5);var crate=Em.GetComponentData<SupportFlightComponent>(flight).Payload;E.DropAt(10);Tick();Assert.AreEqual(1,Em.GetComponentData<SupportSupplyCrateComponent>(crate).Landed);return crate;
            }
            internal Entity Collector(byte faction=1){var unit=E.F.Unit(Request.Position,faction,120);Em.AddComponentData(unit,new UnitMove {Speed=4});Em.AddComponent<UnitMovementBehavior>(unit);Em.AddComponentData(unit,new UnitFootprint {Size=new int2(1)});Em.SetComponentData(unit,new UnitGrid {Cell=Request.Cell});return unit;}
            internal void Tick(){var h=E.F.World.GetOrCreateSystem<SupportSupplySystem>();h.Update(E.F.World.Unmanaged);}
            public void Dispose()=>E.Dispose();
        }
        public static void RunFocusedValidation()
        {
            try {var t=new SupportSupplyValidation();t.RealFlightAndCargoDescentConsumeOnce();t.CapacityRemainderAndFullStorage();t.TwoCollectorsCannotDuplicateStock();t.ReplacementMoveCancelsEvenSameGoal();t.DeadEnemyAirAndWrongAttemptReject();t.ExternalFlightRemovalReleasesHeldCrate();t.BlockedLandingIsNoSpend();t.AttemptCleanupDestroysCrateAndCanopy();t.PathFallbackMustReachInteractionRange();t.MultipleSessionCollectionQueuesAreProcessed();t.NativeCollectionRejectsForeignActorsAndBanks();t.NativeTerminalClearsCargoWithoutReward();t.PausedCollectionCannotTransfer();Debug.Log("[SupportSupplyValidation] result=Passed tests=13");ValidationExit.Exit(0);}
            catch(Exception ex){Debug.LogException(ex);Debug.LogError("[SupportSupplyValidation] result=Failed");ValidationExit.Exit(1);throw;}
        }
        private static Entity Native(Supply s)
        {
            var state=s.Em.GetComponentData<SupportSessionComponent>(s.Root);
            var session=s.Em.CreateEntity(typeof(SkirmishExpandedSessionComponent),typeof(SkirmishSharedMaterialsInitialized));
            s.Em.SetComponentData(session,new SkirmishExpandedSessionComponent {SessionId=state.SessionToken,Phase=Game.Skirmish.Contracts.SkirmishSessionPhase.Playing,SpawnComplete=1});
            s.Em.AddComponentData(s.Root,new SkirmishSupportOwnerComponent {Session=session,SessionId=state.SessionToken});
            s.Em.AddComponentData(s.Store,new FactionEconomy {FactionId=1});
            return session;
        }
        [Test] public void NativeCollectionRejectsForeignActorsAndBanks()
        {
            using var s=new Supply();var crateEntity=s.Land();Native(s);var collector=s.Collector();
            var crate=s.Em.GetComponentData<SupportSupplyCrateComponent>(crateEntity);
            Assert.IsFalse(SupportSupplyAdapterSystemHelper.EligibleCollector(s.Em,collector,crate));
            s.Em.AddComponent<SkirmishSharedActorTag>(collector);
            s.Em.AddComponentData(collector,new SkirmishAttemptOwnedComponent {SessionId="foreign",FactionId=1});
            Assert.IsFalse(SupportSupplyAdapterSystemHelper.EligibleCollector(s.Em,collector,crate));
            s.Em.SetComponentData(collector,new SkirmishAttemptOwnedComponent {SessionId=crate.SessionToken,FactionId=1});
            Assert.IsTrue(SupportSupplyAdapterSystemHelper.EligibleCollector(s.Em,collector,crate));
            var foreign=s.Em.CreateEntity(typeof(FactionTacticalMaterialsComponent));
            s.Em.SetComponentData(foreign,new FactionTacticalMaterialsComponent {FactionId=1,Capacity=600});
            Assert.AreEqual(s.Store,SupportSupplyAdapterSystemHelper.MaterialsEntity(s.Em,s.Root,1));
            Assert.AreEqual(SupportRejectionReason.None,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,collector,crateEntity));
            crate=s.Em.GetComponentData<SupportSupplyCrateComponent>(crateEntity);
            Assert.AreEqual(0,SupportSupplyAdapterSystemHelper.Transfer(s.Em,crateEntity,ref crate,foreign));
            Assert.AreEqual(40,crate.RemainingMaterials);
            Assert.AreEqual(0,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(foreign).Current);
            s.Em.AddComponentData(foreign,new FactionEconomy {FactionId=1});
            Assert.AreEqual(Entity.Null,SupportSupplyAdapterSystemHelper.MaterialsEntity(s.Em,s.Root,1));
            Assert.AreEqual(0,SupportSupplyAdapterSystemHelper.Transfer(s.Em,crateEntity,ref crate,s.Store));
            s.Em.DestroyEntity(foreign);
            Assert.AreEqual(10,SupportSupplyAdapterSystemHelper.Transfer(s.Em,crateEntity,ref crate,s.Store));
            Assert.AreEqual(600,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
        }
        [Test] public void NativeTerminalClearsCargoWithoutReward()
        {
            using var s=new Supply();var crateEntity=s.Land();var session=Native(s);
            var canopy=s.Em.GetComponentData<SupportSupplyCrateComponent>(crateEntity).Canopy;
            var native=s.Em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            native.Phase=Game.Skirmish.Contracts.SkirmishSessionPhase.Finished;s.Em.SetComponentData(session,native);
            s.Tick();Assert.IsFalse(s.Em.Exists(crateEntity));Assert.IsFalse(s.Em.Exists(canopy));
            Assert.AreEqual(590,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
        }
        [Test] public void PausedCollectionCannotTransfer()
        {
            using var s=new Supply();var crateEntity=s.Land();var collector=s.Collector();
            Assert.AreEqual(SupportRejectionReason.None,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,collector,crateEntity));
            var state=s.Em.GetComponentData<SupportSessionComponent>(s.Root);state.Active=0;s.Em.SetComponentData(s.Root,state);
            var crate=s.Em.GetComponentData<SupportSupplyCrateComponent>(crateEntity);
            Assert.AreEqual(0,SupportSupplyAdapterSystemHelper.Transfer(s.Em,crateEntity,ref crate,s.Store));
            s.Tick();Assert.IsTrue(s.Em.Exists(crateEntity));Assert.AreEqual(40,crate.RemainingMaterials);
            state.Active=1;s.Em.SetComponentData(s.Root,state);s.Tick();
            Assert.AreEqual(600,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
        }
        [Test] public void MultipleSessionCollectionQueuesAreProcessed()
        {
            using var f=new SupportValidationFixture();f.Em.AddBuffer<SupportCollectRequestElement>(f.Root);
            var other=f.Em.Instantiate(f.Root);var session=f.Em.GetComponentData<SupportSessionComponent>(other);session.FactionId=2;f.Em.SetComponentData(other,session);
            foreach(var root in new[]{f.Root,other})f.Em.GetBuffer<SupportCollectRequestElement>(root).Add(new SupportCollectRequestElement {SessionToken="wrong-attempt",AttemptOrdinal=1});
            f.World.CreateSystem<SupportSupplySystem>().Update(f.World.Unmanaged);
            foreach(var root in new[]{f.Root,other})
            {
                Assert.AreEqual(0,f.Em.GetBuffer<SupportCollectRequestElement>(root).Length);
                Assert.AreEqual(SupportRejectionReason.WrongAttempt,f.Em.GetComponentData<SupportCollectionFeedbackComponent>(root).Reason);
            }
        }
        [Test] public void RealFlightAndCargoDescentConsumeOnce()
        {
            using var s=new Supply();var flight=s.Commit();var crate=s.Em.GetComponentData<SupportFlightComponent>(flight).Payload;Assert.IsTrue(s.Em.HasComponent<Disabled>(crate));Assert.IsFalse(s.Em.HasComponent<UnitHealth>(crate));Assert.IsFalse(s.Em.HasComponent<UnitGrid>(crate));Assert.AreEqual(6,s.Em.GetComponentData<BuildingResourceStorageComponent>(s.E.Depot).StoredFuelBarrels);
            var session=s.Em.GetComponentData<SupportSessionComponent>(s.Root);session.SimulationSeconds=5;s.Em.SetComponentData(s.Root,session);SupportFlightSystem.Advance(s.Em,flight,5);SupportFlightSystem.Advance(s.Em,flight,5);Assert.AreEqual(3,s.Em.GetComponentData<BuildingResourceStorageComponent>(s.E.Depot).StoredFuelBarrels);Assert.AreEqual(3,s.Em.GetBuffer<SupportReceiptElement>(s.Root)[0].SpentFuel);Assert.IsTrue(s.Em.HasComponent<UnitTransportCargoDropComponent>(crate));var drop=s.Em.GetComponentData<UnitTransportCargoDropComponent>(crate);Assert.AreEqual(4.8f,drop.DurationSeconds);Assert.IsTrue(s.Em.Exists(drop.VisualEntity));s.E.DropAt(8);Assert.IsTrue(s.Em.HasComponent<UnitTransportCargoDropComponent>(crate));s.E.DropAt(10);s.Tick();Assert.AreEqual(1,s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).Landed);Assert.AreEqual(590,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
        }
        [Test] public void CapacityRemainderAndFullStorage()
        {
            using var s=new Supply();var crate=s.Land();var collector=s.Collector();Assert.AreEqual(SupportRejectionReason.None,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,collector,crate));s.Tick();var materials=s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store);Assert.AreEqual(600,materials.Current);Assert.AreEqual(10,materials.LifetimeRewarded);Assert.AreEqual(30,s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).RemainingMaterials);
            Assert.AreEqual(SupportRejectionReason.None,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,collector,crate));s.Tick();s.Tick();Assert.AreEqual(materials.Version,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Version);Assert.AreEqual(30,s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).RemainingMaterials);Assert.AreEqual(1,s.Em.GetComponentData<SupportSupplyReadModelComponent>(s.Root).Full);
            var session=s.Em.GetComponentData<SupportSessionComponent>(s.Root);session.SimulationSeconds=120;s.Em.SetComponentData(s.Root,session);s.Tick();Assert.IsTrue(s.Em.HasComponent<SupportCollectOrderComponent>(collector));
            materials.Current=570;s.Em.SetComponentData(s.Store,materials);s.Tick();Assert.AreEqual(600,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);Assert.AreEqual(40,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).LifetimeRewarded);Assert.IsFalse(s.Em.Exists(crate));Assert.AreEqual(3,s.Em.GetComponentData<BuildingResourceStorageComponent>(s.E.Depot).StoredFuelBarrels);
        }
        [Test] public void TwoCollectorsCannotDuplicateStock()
        {
            using var s=new Supply();var crate=s.Land();var one=s.Collector();var two=s.Collector();Assert.AreEqual(SupportRejectionReason.None,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,one,crate));Assert.AreEqual(SupportRejectionReason.AlreadyProcessed,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,two,crate));Assert.IsFalse(s.Em.HasComponent<SupportCollectOrderComponent>(two));s.Tick();s.Tick();Assert.AreEqual(10,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).LifetimeRewarded);Assert.AreEqual(30,s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).RemainingMaterials);
        }
        [Test] public void ReplacementMoveCancelsEvenSameGoal()
        {
            using var s=new Supply();var crate=s.Land();var collector=s.Collector();Assert.AreEqual(SupportRejectionReason.None,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,collector,crate));var order=s.Em.GetComponentData<SupportCollectOrderComponent>(collector);new UnitMoveOrderSystem().IssueGroupedManualMoveOrder(s.Em,collector,order.Goal,true,false,0,0);Assert.IsFalse(s.Em.HasComponent<SupportCollectOrderComponent>(collector));s.Tick();Assert.AreEqual(Entity.Null,s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).Claimant);Assert.AreEqual(590,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
            Assert.AreEqual(SupportRejectionReason.None,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,collector,crate));UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(s.Em,collector,order.Goal);s.Tick();Assert.AreEqual(40,s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).RemainingMaterials);
        }
        [Test] public void DeadEnemyAirAndWrongAttemptReject()
        {
            using var s=new Supply();var crate=s.Land();var enemy=s.Collector(2);Assert.AreEqual(SupportRejectionReason.InvalidTargetType,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,enemy,crate));var dead=s.Collector();s.Em.SetComponentData(dead,new UnitHealth {Max=120,Current=0});Assert.AreEqual(SupportRejectionReason.InvalidTargetType,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,dead,crate));var air=s.Collector();s.Em.AddComponent<UnitAirMovement>(air);Assert.AreEqual(SupportRejectionReason.InvalidTargetType,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,air,crate));var c=s.Em.GetComponentData<SupportSupplyCrateComponent>(crate);c.AttemptOrdinal++;s.Em.SetComponentData(crate,c);Assert.AreEqual(SupportRejectionReason.WrongAttempt,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,s.Collector(),crate));Assert.AreEqual(590,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
        }
        [Test] public void ExternalFlightRemovalReleasesHeldCrate()
        {
            using var s=new Supply();var flight=s.Commit();var crate=s.Em.GetComponentData<SupportFlightComponent>(flight).Payload;s.Em.DestroyEntity(flight);var h=s.E.F.World.GetOrCreateSystem<SupportFlightSystem>();h.Update(s.E.F.World.Unmanaged);h.Update(s.E.F.World.Unmanaged);Assert.IsFalse(s.Em.Exists(crate));Assert.AreEqual(6,SupportFuelTransactionSystem.Available(s.Em,1));Assert.AreEqual(1,s.Em.GetBuffer<SupportAbilityStateElement>(s.Root)[3].ChargesRemaining);
        }
        [Test] public void BlockedLandingIsNoSpend()
        {
            using var s=new Supply();var walk=s.Em.GetBuffer<GridWalkable>(s.E.Grid);walk[s.Request.Cell.x+s.Request.Cell.y*30]=new GridWalkable();Assert.AreEqual(SupportRejectionReason.LandingBlocked,SupportAbilityRequestSystem.Process(s.Em,s.Root,s.Request));Assert.AreEqual(6,SupportFuelTransactionSystem.Available(s.Em,1));Assert.AreEqual(1,s.Em.GetBuffer<SupportAbilityStateElement>(s.Root)[3].ChargesRemaining);Assert.AreEqual(590,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
        }
        [Test] public void PathFallbackMustReachInteractionRange()
        {
            using var s=new Supply();var crate=s.Land();var collector=s.Collector();
            Assert.AreEqual(SupportRejectionReason.None,SupportSupplyAdapterSystemHelper.BeginCollection(s.Em,s.Root,collector,crate));
            var grid=s.Em.GetComponentData<GridConfig>(s.E.Grid);
            Assert.IsTrue(SupportSupplyAdapterSystemHelper.ValidateCollectionPathResult(s.Em,collector,grid,s.Request.Cell,true,false));
            Assert.IsTrue(SupportSupplyAdapterSystemHelper.ValidateCollectionPathResult(s.Em,collector,grid,s.Request.Cell+new int2(4),true,true));
            Assert.IsFalse(SupportSupplyAdapterSystemHelper.ValidateCollectionPathResult(s.Em,collector,grid,s.Request.Cell+new int2(2,2),true,false));
            Assert.IsFalse(s.Em.HasComponent<SupportCollectOrderComponent>(collector));s.Tick();
            Assert.AreEqual(Entity.Null,s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).Claimant);
            Assert.AreEqual(40,s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).RemainingMaterials);
            Assert.AreEqual(590,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
            Assert.AreEqual(SupportRejectionReason.LandingBlocked,s.Em.GetComponentData<SupportCollectionFeedbackComponent>(s.Root).Reason);
        }
        [Test] public void AttemptCleanupDestroysCrateAndCanopy()
        {
            using var s=new Supply();var crate=s.Land();var canopy=s.Em.GetComponentData<SupportSupplyCrateComponent>(crate).Canopy;var session=s.Em.GetComponentData<SupportSessionComponent>(s.Root);session.AttemptOrdinal++;s.Em.SetComponentData(s.Root,session);s.Tick();Assert.IsFalse(s.Em.Exists(crate));Assert.IsFalse(s.Em.Exists(canopy));Assert.AreEqual(590,s.Em.GetComponentData<FactionTacticalMaterialsComponent>(s.Store).Current);
        }
    }
}
#endif
