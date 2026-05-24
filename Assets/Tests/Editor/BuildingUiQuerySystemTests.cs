#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingUiQuerySystemTests
{
    [Test]
    public void AddPendingProducedUnitEntries_AddsProgressCappedPendingEntries()
    {
        var prefab = new GameObject("UnitPrefab");
        try
        {
            var pending = new TestPendingProduction
            {
                Prefab = prefab,
                StartedAt = 0f,
                ReadyAt = 10f,
                TransportPrefab = new GameObject("Transport")
            };
            try
            {
                var entries = new List<BuildingUiQuerySystem.ProducedUnitUiEntry>();

                var uiQuery = new BuildingUiQuerySystem();
                uiQuery.AddPendingProducedUnitEntries(
                    new[] { pending },
                    new BuildingProductionSystem(),
                    9.9f,
                    entries);

                Assert.AreEqual(1, entries.Count);
                Assert.AreEqual(Entity.Null, entries[0].Unit);
                Assert.AreSame(prefab, entries[0].Prefab);
                Assert.IsFalse(entries[0].IsReady);
                Assert.AreEqual(0.97f, entries[0].Progress01, 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(pending.TransportPrefab);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void AddPendingProductionUiEntries_AddsRemainingDurationAndProgress()
    {
        var prefab = new GameObject("UnitPrefab");
        try
        {
            var pending = new TestPendingProduction
            {
                Prefab = prefab,
                StartedAt = 5f,
                ReadyAt = 15f
            };
            var entries = new List<BuildingUiQuerySystem.PendingProductionUiEntry>();

            var uiQuery = new BuildingUiQuerySystem();
            uiQuery.AddPendingProductionUiEntries(
                42,
                new[] { pending },
                new BuildingProductionSystem(),
                10f,
                entries);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(42, entries[0].BuildingId);
            Assert.AreSame(prefab, entries[0].Prefab);
            Assert.AreEqual(5f, entries[0].RemainingSeconds);
            Assert.AreEqual(10f, entries[0].DurationSeconds);
            Assert.AreEqual(0.5f, entries[0].Progress01);
            Assert.AreEqual(5f, entries[0].StartedAt);
            Assert.AreEqual(15f, entries[0].ReadyAt);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void GetProducedUnits_PrunesDeadProducedUnits()
    {
        using World world = new("BuildingUiQuerySystemTests");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 10, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });

        var produced = new List<Entity> { alive, dead, Entity.Null };
        var results = new List<Entity>();

        var uiQuery = new BuildingUiQuerySystem();
        uiQuery.GetProducedUnits(produced, entityManager, new BuildingProductionSystem(), results);

        Assert.AreEqual(1, produced.Count);
        Assert.AreEqual(alive, produced[0]);
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(alive, results[0]);
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
