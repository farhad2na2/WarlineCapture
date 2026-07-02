using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingUiQuerySystemTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new BuildingUiQuerySystemTests();
            tests.AddPendingProducedUnitEntries_AddsProgressCappedPendingEntries();
            tests.GetProducedUnits_PrunesDeadProducedUnits();
            tests.AddProducedUnitEntries_ResolvesReadyPrefabFromPassivePreviewDelegate();
            tests.SelectedBuildingProducedUnits_ReadsProducedUnitReadModel();
            tests.GetFriendlyPendingProductionUiEntries_IncludesPlayerOwnedProducerQueues();
            Debug.Log("[BuildingUiQueryValidation] result=Passed tests=5");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BuildingUiQueryValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

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
                var entries = new List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry>();

                var uiQuery = new BuildingUiQueryUiSystemHelper();
                uiQuery.AddPendingProducedUnitEntries(
                    new[] { pending },
                    new BuildingProductionQueueCompositionSystemHelper(),
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
            var entries = new List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry>();

            var uiQuery = new BuildingUiQueryUiSystemHelper();
            uiQuery.AddPendingProductionUiEntries(
                42,
                new[] { pending },
                new BuildingProductionQueueCompositionSystemHelper(),
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

        var uiQuery = new BuildingUiQueryUiSystemHelper();
        uiQuery.GetProducedUnits(produced, entityManager, new BuildingProductionQueueCompositionSystemHelper(), results);

        Assert.AreEqual(1, produced.Count);
        Assert.AreEqual(alive, produced[0]);
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(alive, results[0]);
    }

    [Test]
    public void AddProducedUnitEntries_ResolvesReadyPrefabFromPassivePreviewDelegate()
    {
        using World world = new("BuildingUiQuerySystemTests_ProducedUnitPreview");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 10, Max = 10 });

        GameObject prefab = new("Unit_Infantry_SourceKeyPreview");
        try
        {
            var produced = new List<Entity> { alive };
            var entries = new List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry>();

            var uiQuery = new BuildingUiQueryUiSystemHelper();
            uiQuery.AddProducedUnitEntries(
                produced,
                null,
                null,
                null,
                entityManager,
                new BuildingProductionQueueCompositionSystemHelper(),
                0f,
                entries,
                (Entity unit, out GameObject resolvedPrefab) =>
                {
                    resolvedPrefab = unit == alive ? prefab : null;
                    return resolvedPrefab != null;
                });

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(alive, entries[0].Unit);
            Assert.AreSame(prefab, entries[0].Prefab);
            Assert.IsTrue(entries[0].IsReady);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void SelectedBuildingProducedUnits_ReadsProducedUnitReadModel()
    {
        using World world = new("BuildingUiQuerySystemTests_ProducedUnitReadModel");
        EntityManager entityManager = world.EntityManager;
        Entity alive = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(alive, new UnitHealth { Current = 10, Max = 10 });
        Entity dead = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(dead, new UnitHealth { Current = 0, Max = 10 });
        Entity otherBuildingUnit = entityManager.CreateEntity(typeof(UnitHealth));
        entityManager.SetComponentData(otherBuildingUnit, new UnitHealth { Current = 10, Max = 10 });

        Entity boundaryEntity = entityManager.CreateEntity(typeof(BuildingRuntimeStateTag));
        DynamicBuffer<BuildingProducedUnitReadModel> producedUnits =
            entityManager.AddBuffer<BuildingProducedUnitReadModel>(boundaryEntity);
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 7,
            Unit = alive,
            UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
        });
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 7,
            Unit = dead,
            UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
        });
        producedUnits.Add(new BuildingProducedUnitReadModel
        {
            BuildingRuntimeId = 8,
            Unit = otherBuildingUnit,
            UnitSourceKey = new FixedString64Bytes("unit_inf_regular")
        });

        RuntimeBuildingEntity selectedBuilding = new()
        {
            Id = 7
        };
        var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity>
        {
            [selectedBuilding.Id] = selectedBuilding
        };
        GameObject previewPrefab = new("UnitPreview");
        try
        {
            BuildingUiQueryUiSystemHelper.Context context = new(
                runtimeBuildings,
                () => selectedBuilding.Id,
                (out EntityManager em) =>
                {
                    em = entityManager;
                    return true;
                },
                new BuildingProductionQueueCompositionSystemHelper(),
                () => 10f,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                (Entity unit, out GameObject prefab) =>
                {
                    prefab = unit == alive ? previewPrefab : null;
                    return prefab != null;
                });
            var uiQuery = new BuildingUiQueryUiSystemHelper();
            var producedUnitResults = new List<Entity>();
            uiQuery.GetSelectedBuildingProducedUnits(context, producedUnitResults);

            Assert.AreEqual(1, producedUnitResults.Count);
            Assert.AreEqual(alive, producedUnitResults[0]);
            Assert.IsNull(selectedBuilding.ProducedUnits);

            var entries = new List<BuildingUiQueryUiSystemHelper.ProducedUnitUiEntry>();
            uiQuery.GetSelectedBuildingProducedUnitEntries(context, entries);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(alive, entries[0].Unit);
            Assert.AreSame(previewPrefab, entries[0].Prefab);
            Assert.IsTrue(entries[0].IsReady);
            Assert.IsNull(selectedBuilding.ProducedUnits);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(previewPrefab);
        }
    }

    [Test]
    public void GetFriendlyPendingProductionUiEntries_IncludesPlayerOwnedProducerQueues()
    {
        GameObject prefab = new("Attack Helicopter");
        try
        {
            RuntimeBuildingEntity playerProducer = new()
            {
                Id = 7,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.PlayerFactionId,
                Definition = new BuildingDefinition { DisplayName = "Player Helipad" },
                PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>
                {
                    new()
                    {
                        Prefab = prefab,
                        StartedAt = 10f,
                        ReadyAt = 20f
                    }
                }
            };
            RuntimeBuildingEntity enemyProducer = new()
            {
                Id = 8,
                HasOwnerFaction = true,
                OwnerFactionId = FactionIdentity.EnemyFactionId,
                Definition = new BuildingDefinition { DisplayName = "Enemy Helipad" },
                PendingProductions = new List<RuntimeBuildingEntity.PendingProduction>
                {
                    new()
                    {
                        Prefab = prefab,
                        StartedAt = 10f,
                        ReadyAt = 20f
                    }
                }
            };
            var runtimeBuildings = new Dictionary<int, RuntimeBuildingEntity>
            {
                [playerProducer.Id] = playerProducer,
                [enemyProducer.Id] = enemyProducer
            };
            BuildingUiQueryUiSystemHelper.Context context = new(
                runtimeBuildings,
                null,
                null,
                new BuildingProductionQueueCompositionSystemHelper(),
                () => 12.5f,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
            var entries = new List<BuildingUiQueryUiSystemHelper.PendingProductionUiEntry>();

            var uiQuery = new BuildingUiQueryUiSystemHelper();
            uiQuery.GetFriendlyPendingProductionUiEntries(context, entries);

            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(playerProducer.Id, entries[0].BuildingId);
            Assert.AreSame(prefab, entries[0].Prefab);
            Assert.AreEqual("Player Helipad", entries[0].ProducerDisplayName);
            Assert.AreEqual(0.25f, entries[0].Progress01, 0.0001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    private sealed class TestPendingProduction : BuildingProductionQueueCompositionSystemHelper.IPendingProduction
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
        public BuildingProductionQueueCompositionSystemHelper.ProductionTransportMode TransportMode { get; set; }
        public bool TransportRequiresAirportRunway { get; set; }
    }
}
#endif
