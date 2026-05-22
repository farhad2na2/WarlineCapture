#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingProductionSystemTests
{
    [Test]
    public void InitializePendingProduction_SetsReadyTimeAndTransportFields()
    {
        var pending = new TestPendingProduction();
        var system = new BuildingProductionSystem();

        system.InitializePendingProduction(
            pending,
            productionIndex: 2,
            spawnUnitPrefab: null,
            now: 10f,
            productionDurationSeconds: 4f,
            reservedProductionSlotIndex: 1,
            transportPrefab: null,
            transportArrivalSeconds: 3f,
            transportHoldForNextReadySeconds: 5f,
            transportMaxConcurrent: 2,
            transportMode: BuildingProductionSystem.ProductionTransportMode.Plane,
            transportRequiresAirportRunway: true);

        Assert.AreEqual(2, pending.ProductionIndex);
        Assert.AreEqual(10f, pending.StartedAt);
        Assert.AreEqual(14f, pending.ReadyAt);
        Assert.AreEqual(1, pending.ReservedProductionSlotIndex);
        Assert.AreEqual(3f, pending.TransportArrivalSeconds);
        Assert.AreEqual(5f, pending.TransportHoldForNextReadySeconds);
        Assert.AreEqual(2, pending.TransportMaxConcurrent);
        Assert.AreEqual(BuildingProductionSystem.ProductionTransportMode.Plane, pending.TransportMode);
        Assert.IsTrue(pending.TransportRequiresAirportRunway);
    }

    [Test]
    public void GetProgress_ComputesRemainingAndCanCapTransportProgress()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var pending = new TestPendingProduction
            {
                StartedAt = 0f,
                ReadyAt = 10f,
                TransportPrefab = transportPrefab
            };

            var system = new BuildingProductionSystem();
            BuildingProductionSystem.PendingProductionProgress progress = system.GetProgress(pending, 9.9f, true);

            Assert.AreEqual(10f, progress.DurationSeconds);
            Assert.AreEqual(0.1f, progress.RemainingSeconds, 0.0001f);
            Assert.AreEqual(0.97f, progress.Progress01, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void TransportLaunchAndDelay_UsesArrivalWindow()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var pending = new TestPendingProduction
            {
                StartedAt = 0f,
                ReadyAt = 20f,
                TransportPrefab = transportPrefab,
                TransportArrivalSeconds = 5f
            };

            var system = new BuildingProductionSystem();

            Assert.AreEqual(15f, system.GetTransportLaunchAt(pending));
            Assert.IsFalse(system.ShouldLaunchTransport(pending, 14.9f));
            Assert.IsTrue(system.ShouldLaunchTransport(pending, 15f));

            system.DelayPendingProduction(pending, 2f);

            Assert.AreEqual(2f, pending.StartedAt);
            Assert.AreEqual(22f, pending.ReadyAt);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void ReadinessHelpers_ReportReadyAndSoonStates()
    {
        var pending = new TestPendingProduction
        {
            ReadyAt = 20f
        };

        var system = new BuildingProductionSystem();

        Assert.IsFalse(system.IsReady(pending, 19.9f));
        Assert.IsTrue(system.IsReady(pending, 20f));
        Assert.IsTrue(system.IsReadyWithin(pending, 18f, 2.5f));
        Assert.IsFalse(system.IsReadyWithin(pending, 16f, 2.5f));
    }

    [Test]
    public void PruneProducedUnits_RemovesDeadUnitsAndClearsDeadSlots()
    {
        using World world = new("BuildingProductionSystemTests");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 5, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });
        var producedUnits = new List<Entity> { alive, dead, Entity.Null };
        GameObject deadPrefab = new("DeadPrefab");
        var producedUnitPrefabs = new Dictionary<Entity, GameObject>
        {
            [dead] = deadPrefab
        };
        Entity[] slots = { dead, alive };

        try
        {
            var system = new BuildingProductionSystem();
            system.PruneProducedUnits(producedUnits, slots, producedUnitPrefabs, entityManager);

            Assert.AreEqual(1, producedUnits.Count);
            Assert.AreEqual(alive, producedUnits[0]);
            Assert.IsFalse(producedUnitPrefabs.ContainsKey(dead));
            Assert.AreEqual(Entity.Null, slots[0]);
            Assert.AreEqual(alive, slots[1]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(deadPrefab);
        }
    }

    [Test]
    public void TryReserveProductionSlot_SkipsPendingAndOccupiedSlots()
    {
        using World world = new("BuildingProductionSystemTests");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 5, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });
        var pending = new List<BuildingProductionSystem.IPendingProduction>
        {
            new TestPendingProduction { ReservedProductionSlotIndex = 0 }
        };
        Entity[] slots = { Entity.Null, alive, dead };

        var system = new BuildingProductionSystem();
        bool reserved = system.TryReserveProductionSlot(pending, slots, 3, entityManager, out int slotIndex);

        Assert.IsTrue(reserved);
        Assert.AreEqual(2, slotIndex);
        Assert.AreEqual(Entity.Null, slots[2]);
    }

    [Test]
    public void TransportPendingQueries_FindReadyAndSoonEntries()
    {
        var transportPrefab = new GameObject("Transport");
        try
        {
            var ready = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 10f
            };
            var soon = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 13f
            };
            var later = new TestPendingProduction
            {
                TransportPrefab = transportPrefab,
                ReadyAt = 20f
            };
            var pending = new List<TestPendingProduction> { soon, ready, later };

            var system = new BuildingProductionSystem();

            Assert.AreSame(ready, system.FindNextReadyTransportPending(pending, transportPrefab, 10f));
            Assert.AreSame(soon, system.FindNextSoonTransportPending(pending, transportPrefab, 10f, 4f));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(transportPrefab);
        }
    }

    [Test]
    public void PendingRemovalHelpers_RemoveByReferenceAndIndex()
    {
        var first = new TestPendingProduction();
        var second = new TestPendingProduction();
        var third = new TestPendingProduction();
        var pending = new List<TestPendingProduction> { first, second, third };

        var system = new BuildingProductionSystem();

        Assert.IsTrue(system.RemovePendingProduction(pending, second));
        Assert.AreEqual(2, pending.Count);
        Assert.AreSame(first, pending[0]);
        Assert.AreSame(third, pending[1]);

        Assert.IsTrue(system.RemovePendingAt(pending, 0));
        Assert.AreEqual(1, pending.Count);
        Assert.AreSame(third, pending[0]);
    }

    private sealed class TestPendingProduction : BuildingProductionSystem.IPendingProduction
    {
        public int ProductionIndex { get; set; }
        public GameObject Prefab { get; set; }
        public float StartedAt { get; set; }
        public float ReadyAt { get; set; }
        public int ReservedProductionSlotIndex { get; set; }
        public GameObject TransportPrefab { get; set; }
        public float TransportArrivalSeconds { get; set; }
        public float TransportHoldForNextReadySeconds { get; set; }
        public int TransportMaxConcurrent { get; set; }
        public BuildingProductionSystem.ProductionTransportMode TransportMode { get; set; }
        public bool TransportRequiresAirportRunway { get; set; }
    }
}
#endif
