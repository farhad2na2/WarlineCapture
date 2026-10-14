using System;
using System.Collections.Generic;
using Game.Components;
using Game.Runtime;
using Game.Skirmish.Contracts;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Tests.Editor
{
    public sealed class SkirmishGroundCarrierDeathTests
    {
        sealed class Fixture : IDisposable
        {
            internal readonly World World = new World("shared-ground-carrier-death");
            internal EntityManager Em => World.EntityManager;
            internal Entity Session, Carrier;
            internal readonly List<Entity> Cargo = new List<Entity>();
            readonly NativeArray<int> counts;
            readonly NativeArray<byte> pass;
            readonly NativeBitArray occupied;
            internal readonly NativeBitArray Blocked;
            readonly SystemHandle cleanup;
            internal Fixture(byte legacy = 0, bool air = false, bool sharedRing = false)
            {
                counts = new NativeArray<int>(144, Allocator.Persistent);
                pass = new NativeArray<byte>(144, Allocator.Persistent);
                Blocked = new NativeBitArray(144, Allocator.Persistent);
                occupied = new NativeBitArray(144, Allocator.Persistent);
                var em = Em;
                var grid = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
                em.SetComponentData(grid, new GridConfig { Width = 12, Height = 12, CellSize = 1, Origin = float3.zero });
                em.SetComponentData(grid, new DynamicBlockerComponent { GridSize = 144, Counts = counts, Blocked = Blocked, FriendlyPassFactionIds = pass });
                em.SetComponentData(grid, new DynamicOccupancyComponent { GridSize = 144, Occupied = occupied });
                var walk = em.AddBuffer<GridWalkable>(grid); walk.ResizeUninitialized(144);
                for (int i = 0; i < 144; i++) walk[i] = new GridWalkable { Value = 1 };
                Session = em.CreateEntity(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishEnemyCapacityComponent));
                em.SetComponentData(Session, new SkirmishExpandedSessionComponent { SessionId = "carrier-test", Phase = SkirmishSessionPhase.Playing, IsLegacy = legacy });
                em.SetComponentData(Session, new SkirmishEnemyCapacityComponent { InfantryCap = 48, InfantryLive = 4, GroundCap = 8, SupplyCap = 64, SupplyLive = 4 });
                Carrier = Actor("carrier", SkirmishRoleKind.ApcHeavy, new int2(sharedRing ? 3 : 5, 5), 0);
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(Carrier); owner.CapacityReleased = 1; em.SetComponentData(Carrier, owner);
                em.AddComponentData(Carrier, new VehicleWreckComponent { TimeRemaining = 0 });
                em.SetComponentData(Carrier, new UnitFootprint { Size = new int2(3, 3) });
                em.AddComponentData(Carrier, new UnitTransportCapacity { SoldierCapacity = 4 });
                em.AddBuffer<UnitTransportPassengerElement>(Carrier);
                if (air) em.AddComponentData(Carrier, new UnitAirComponent());
                for (int i = 0; i < 4; i++)
                {
                    var actor = Actor("rifle." + i, SkirmishRoleKind.Rifle, new int2(4, 5 + i), 100);
                    Cargo.Add(actor);
                    var ecb = new EntityCommandBuffer(Allocator.Temp);
                    try { new UnitTransportPassengerStateSystem().BoardPassenger(em, ref ecb,
                        em.GetBuffer<UnitTransportPassengerElement>(Carrier), actor, Carrier); ecb.Playback(em); }
                    finally { ecb.Dispose(); }
                }
                World.SetTime(new TimeData(1, .1f));
                cleanup = World.CreateSystem<VehicleWreckCleanupSystem>();
            }
            Entity Actor(string id, SkirmishRoleKind role, int2 cell, int hp)
            {
                var em = Em;
                var actor = em.CreateEntity(typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishUnitRoleComponent),
                    typeof(UnitHealth), typeof(Faction), typeof(UnitGrid), typeof(UnitFootprint), typeof(LocalTransform),
                    typeof(UnitSourcePrefabKey), typeof(SkirmishSharedActorTag));
                em.SetComponentData(actor, new SkirmishAttemptOwnedComponent { SessionId = "carrier-test", StableObjectId = new FixedString64Bytes(id), FactionId = 2 });
                em.SetComponentData(actor, new SkirmishUnitRoleComponent { Role = role, Category = role == SkirmishRoleKind.Rifle ? SkirmishPopulationCategory.Infantry : SkirmishPopulationCategory.Ground, SupplyCost = 1 });
                em.SetComponentData(actor, new UnitHealth { Current = hp, Max = role == SkirmishRoleKind.Rifle ? 100 : 360 });
                em.SetComponentData(actor, new Faction { Id = 2 });
                em.SetComponentData(actor, new UnitGrid { Cell = cell });
                em.SetComponentData(actor, new UnitFootprint { Size = new int2(1, 1) });
                em.SetComponentData(actor, LocalTransform.FromPositionRotationScale(new float3(cell.x + .5f, 0, cell.y + .5f), quaternion.identity, 2));
                em.SetComponentData(actor, new UnitSourcePrefabKey { Value = new FixedString64Bytes(role == SkirmishRoleKind.Rifle ? "Unit_Inf_Rifle" : "Unit_Veh_APC_Slow") });
                return actor;
            }
            internal Entity AddSecondCarrier()
            {
                var em = Em; var carrier = Actor("carrier.second", SkirmishRoleKind.ApcHeavy, new int2(7, 5), 0);
                em.AddComponentData(carrier, new VehicleWreckComponent());
                em.SetComponentData(carrier, new UnitFootprint { Size = new int2(3, 3) });
                em.AddComponentData(carrier, new UnitTransportCapacity { SoldierCapacity = 4 });
                em.AddBuffer<UnitTransportPassengerElement>(carrier);
                for (int i = 0; i < 4; i++)
                {
                    var actor = Actor("second.rifle." + i, SkirmishRoleKind.Rifle, new int2(9, 4 + i), 100); Cargo.Add(actor);
                    var ecb = new EntityCommandBuffer(Allocator.Temp);
                    try { new UnitTransportPassengerStateSystem().BoardPassenger(em, ref ecb,
                        em.GetBuffer<UnitTransportPassengerElement>(carrier), actor, carrier); ecb.Playback(em); }
                    finally { ecb.Dispose(); }
                }
                return carrier;
            }
            internal void Cleanup() => cleanup.Update(World.Unmanaged);
            public void Dispose() { World.Dispose(); counts.Dispose(); pass.Dispose(); Blocked.Dispose(); occupied.Dispose(); }
        }

        [Test] public void ActualWreckCleanupUnloadsHealthyCargoAndPreservesCapacityAndCheckpoint()
        {
            using var f = new Fixture(); var em = f.Em;
            string capacity = JsonUtility.ToJson(em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session));
            f.Cleanup();
            Assert.IsFalse(em.HasComponent<SkirmishAttemptOwnedComponent>(f.Carrier));
            var cells = new HashSet<int2>();
            foreach (var actor in f.Cargo)
            {
                Assert.AreEqual(100, em.GetComponentData<UnitHealth>(actor).Current);
                Assert.IsFalse(em.HasComponent<Disabled>(actor)); Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(actor));
                Assert.IsFalse(em.HasComponent<UnitTransportCargoPassenger>(actor));
                Assert.AreEqual(2, em.GetComponentData<LocalTransform>(actor).Scale);
                Assert.AreEqual(0, em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).CapacityReleased);
                Assert.IsTrue(cells.Add(em.GetComponentData<UnitGrid>(actor).Cell), "Native placement must separate each passenger.");
                var saved = SkirmishNativeCheckpointParts.Capture(em, actor, e => e == actor ? "passenger" : null);
                using var restored = new World("released-passenger-checkpoint");
                var copy = restored.EntityManager.CreateEntity();
                SkirmishNativeCheckpointParts.Apply(restored.EntityManager, copy, saved, id => { throw new InvalidOperationException("Unexpected carrier reference after physical unload: " + id); });
                Assert.AreEqual(100, restored.EntityManager.GetComponentData<UnitHealth>(copy).Current);
                Assert.AreEqual(em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).StableObjectId,
                    restored.EntityManager.GetComponentData<SkirmishAttemptOwnedComponent>(copy).StableObjectId);
                Assert.IsFalse(restored.EntityManager.HasComponent<UnitTransportPassenger>(copy));
            }
            Assert.AreEqual(capacity, JsonUtility.ToJson(em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session)));
        }
        [Test] public void BlockedGroundRetainsStableCarrierAndRetriesNativeLegalPlacement()
        {
            using var f = new Fixture(); var em = f.Em; f.Blocked.SetBits(0, true, 144);
            f.Cleanup(); Assert.IsTrue(em.HasComponent<SkirmishAttemptOwnedComponent>(f.Carrier));
            foreach (var actor in f.Cargo)
            {
                Assert.IsTrue(em.HasComponent<Disabled>(actor)); Assert.AreEqual(f.Carrier, em.GetComponentData<UnitTransportPassenger>(actor).Transport);
                Assert.AreEqual(100, em.GetComponentData<UnitHealth>(actor).Current);
                Assert.DoesNotThrow(() => SkirmishNativeCheckpointParts.Capture(em, actor, e => e == f.Carrier ? "carrier" : e == actor ? "passenger" : null));
            }
            f.Blocked.SetBits(0, false, 144); f.Cleanup();
            Assert.IsFalse(em.HasComponent<SkirmishAttemptOwnedComponent>(f.Carrier));
            foreach (var actor in f.Cargo) Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(actor));
        }
        [Test] public void PartialUnloadRetainsRemainingIdentityAndReservesReleasedCells()
        {
            using var f = new Fixture(); var em = f.Em; f.Blocked.SetBits(0, true, 144); f.Blocked.Set(5 + 0 * 12, false);
            f.Cleanup(); int released = 0;
            foreach (var actor in f.Cargo) if (!em.HasComponent<UnitTransportPassenger>(actor)) released++;
            Assert.AreEqual(1, released); Assert.IsTrue(em.HasComponent<SkirmishAttemptOwnedComponent>(f.Carrier));
            Assert.AreEqual(3, em.GetBuffer<UnitTransportPassengerElement>(f.Carrier).Length);
            foreach (var actor in f.Cargo) Assert.DoesNotThrow(() => SkirmishNativeCheckpointParts.Capture(em, actor,
                e => e == f.Carrier ? "carrier" : e == actor ? "passenger" : null));
            f.Blocked.SetBits(0, false, 144); f.Cleanup();
            var cells = new HashSet<int2>();
            foreach (var actor in f.Cargo) { Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(actor)); Assert.IsTrue(cells.Add(em.GetComponentData<UnitGrid>(actor).Cell)); }
        }
        [Test] public void SameCleanupBatchCannotStackTwoCarriersCargo()
        {
            using var f = new Fixture(sharedRing: true); var em = f.Em; var second = f.AddSecondCarrier();
            f.Blocked.SetBits(0, true, 144);
            foreach (var cell in new[] { new int2(5, 0), new int2(6, 0), new int2(5, 10), new int2(6, 10) }) f.Blocked.Set(cell.x + cell.y * 12, false);
            f.Cleanup(); var releasedCells = new HashSet<int2>();
            foreach (var actor in f.Cargo)
                if (!em.HasComponent<UnitTransportPassenger>(actor)) Assert.IsTrue(releasedCells.Add(em.GetComponentData<UnitGrid>(actor).Cell));
            Assert.AreEqual(4, releasedCells.Count);
            Assert.IsTrue(em.HasComponent<SkirmishAttemptOwnedComponent>(f.Carrier) || em.HasComponent<SkirmishAttemptOwnedComponent>(second));
            f.Blocked.SetBits(0, false, 144); f.Cleanup(); releasedCells.Clear();
            foreach (var actor in f.Cargo) { Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(actor)); Assert.IsTrue(releasedCells.Add(em.GetComponentData<UnitGrid>(actor).Cell)); }
            Assert.AreEqual(8, releasedCells.Count);
        }
        [Test] public void DeadCargoIsNotResurrectedByPhysicalDisembark()
        {
            using var f = new Fixture(); var em = f.Em; var actor = f.Cargo[0];
            em.SetComponentData(actor, new UnitHealth { Current = 0, Max = 100 });
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor); owner.CapacityReleased = 1; em.SetComponentData(actor, owner);
            string capacity = JsonUtility.ToJson(em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session));
            f.Cleanup(); Assert.AreEqual(0, em.GetComponentData<UnitHealth>(actor).Current);
            Assert.AreEqual(1, em.GetComponentData<SkirmishAttemptOwnedComponent>(actor).CapacityReleased);
            Assert.IsFalse(em.HasComponent<UnitTransportPassenger>(actor));
            Assert.AreEqual(capacity, JsonUtility.ToJson(em.GetComponentData<SkirmishEnemyCapacityComponent>(f.Session)));
        }
        [Test] public void PausedCleanupRetainsExactPhysicalCargoGraph()
        {
            using var f = new Fixture(); var em = f.Em; f.World.SetTime(new TimeData(1, 0));
            f.Cleanup(); Assert.IsTrue(em.HasComponent<SkirmishAttemptOwnedComponent>(f.Carrier));
            foreach (var actor in f.Cargo) { Assert.IsTrue(em.HasComponent<Disabled>(actor)); Assert.AreEqual(f.Carrier, em.GetComponentData<UnitTransportPassenger>(actor).Transport); }
        }
        [Test] public void LegacyCampaignAndAircraftRemainOutsideGroundSettlement()
        {
            using (var legacy = new Fixture(1)) Assert.IsTrue(TransportBoardingCommandSystem.CanFinalizeSharedGroundCarrier(legacy.Em, legacy.Carrier));
            using (var air = new Fixture(air: true)) Assert.IsTrue(TransportBoardingCommandSystem.CanFinalizeSharedGroundCarrier(air.Em, air.Carrier));
        }
        [Test] public void MalformedReciprocalGraphDefersRatherThanErasingIdentity()
        {
            using var f = new Fixture(); var em = f.Em;
            em.GetBuffer<UnitTransportPassengerElement>(f.Carrier).RemoveAt(0);
            f.Cleanup(); Assert.IsTrue(em.HasComponent<SkirmishAttemptOwnedComponent>(f.Carrier));
            Assert.AreEqual(f.Carrier, em.GetComponentData<UnitTransportPassenger>(f.Cargo[0]).Transport);
        }
        public static void RunFocusedValidation()
        {
            try
            {
                var t = new SkirmishGroundCarrierDeathTests();
                t.ActualWreckCleanupUnloadsHealthyCargoAndPreservesCapacityAndCheckpoint();
                t.BlockedGroundRetainsStableCarrierAndRetriesNativeLegalPlacement();
                t.PartialUnloadRetainsRemainingIdentityAndReservesReleasedCells();
                t.SameCleanupBatchCannotStackTwoCarriersCargo();
                t.DeadCargoIsNotResurrectedByPhysicalDisembark();
                t.PausedCleanupRetainsExactPhysicalCargoGraph();
                t.LegacyCampaignAndAircraftRemainOutsideGroundSettlement();
                t.MalformedReciprocalGraphDefersRatherThanErasingIdentity();
                new SkirmishNativeCheckpointPartsTests().DestroyedPassengerCarrierIsNeverDiscardedAsAnOptionalCombatTarget();
                Debug.Log("[SkirmishGroundCarrierDeathTests] result=Passed cases=9 scope=IsolatedActualSystemNotNormalMission"); ValidationExit.Passed();
            }
            catch (Exception e) { Debug.LogException(e); Debug.LogError("[SkirmishGroundCarrierDeathTests] result=Failed"); ValidationExit.Failed(); }
        }
    }
}
