using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using Unity.Entities;
using UnityEngine;

public sealed class FactionResourceCompositionSystemHelperTests
{
    public static void RunFocusedValidation()
    {
        var tests = new FactionResourceCompositionSystemHelperTests();
        try
        {
            tests.GetResourceTotals_CountsStorageBuildingsOnly();
            tests.TryGetFactionResourceEconomy_SumsFactionStorageAndRates();
            tests.DrainFactionResource_DrainsRequestedResourceAcrossFactionBuildings();
            tests.GetResourceTotals_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale();
            tests.TryGetFactionResourceEconomy_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale();
            tests.DrainFactionResource_PrefersLiveEcsStorageAndMirrorsResult();
            tests.TryGetPrimaryCapacityInfo_DerivesOilCapacityForFuelProducer();
            tests.UpdateResourceProduction_ExtractsOilUpToCapacity();
            tests.UpdateResourceProduction_ConvertsOilIntoFuel();
            tests.UpdateResourceProduction_ClampsOilAndFuelWhenLargeDeltaOverfillsStorage();
            tests.UpdateResourceProduction_DoesNotConvertOilWhenFuelStorageIsFull();
            tests.UpdateResourceProduction_IgnoresDestroyedBuildings();
            Debug.Log("[FactionResourceFocusedValidation] result=Passed tests=12");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[FactionResourceFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void GetResourceTotals_CountsStorageBuildingsOnly()
    {
        var buildings = new Dictionary<int, TestResourceBuilding>
        {
            { 1, new TestResourceBuilding { OilStorageCapacity = 100, StoredOilBarrels = 42f } },
            { 2, new TestResourceBuilding { FuelStorageCapacity = 50, StoredFuelBarrels = 11.9f } },
            { 3, new TestResourceBuilding { OilStorageCapacity = 100, OilBarrelsPerDay = 5f, StoredOilBarrels = 99f } },
            { 4, new TestResourceBuilding { OilStorageCapacity = 100, StoredOilBarrels = 8f, IsDestroyed = true } }
        };

        var system = new FactionResourceCompositionSystemHelper();
        system.GetResourceTotals(buildings, out int oil, out int fuel);

        Assert.AreEqual(42, oil);
        Assert.AreEqual(11, fuel);
    }

    [Test]
    public void TryGetFactionResourceEconomy_SumsFactionStorageAndRates()
    {
        var buildings = new Dictionary<int, TestResourceBuilding>
        {
            { 1, new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 2, OilStorageCapacity = 100, StoredOilBarrels = 12.5f, OilBarrelsPerDay = 3f } },
            { 2, new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 2, FuelStorageCapacity = 50, StoredFuelBarrels = 7f, FuelBarrelsPerDay = 2f } },
            { 3, new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 1, OilStorageCapacity = 100, StoredOilBarrels = 99f, OilBarrelsPerDay = 9f } }
        };

        var system = new FactionResourceCompositionSystemHelper();
        bool found = system.TryGetFactionResourceEconomy(buildings, 2, out FactionResourceCompositionSystemHelper.ResourceEconomySnapshot snapshot);

        Assert.IsTrue(found);
        Assert.AreEqual(2, snapshot.ResourceBuildingCount);
        Assert.AreEqual(12.5f, snapshot.StoredOilBarrels);
        Assert.AreEqual(7f, snapshot.StoredFuelBarrels);
        Assert.AreEqual(3f, snapshot.OilBarrelsPerDay);
        Assert.AreEqual(2f, snapshot.FuelBarrelsPerDay);
    }

    [Test]
    public void DrainFactionResource_DrainsRequestedResourceAcrossFactionBuildings()
    {
        var first = new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 2, OilStorageCapacity = 100, StoredOilBarrels = 3f };
        var second = new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 2, OilStorageCapacity = 100, StoredOilBarrels = 10f };
        var otherFaction = new TestResourceBuilding { HasOwnerFaction = true, OwnerFactionId = 1, OilStorageCapacity = 100, StoredOilBarrels = 20f };
        var buildings = new Dictionary<int, TestResourceBuilding>
        {
            { 1, first },
            { 2, second },
            { 3, otherFaction }
        };

        var system = new FactionResourceCompositionSystemHelper();
        float drained = system.DrainFactionResource(buildings, 2, 8f, FactionResourceCompositionSystemHelper.ResourceKind.Oil);

        Assert.AreEqual(8f, drained);
        Assert.AreEqual(0f, first.StoredOilBarrels);
        Assert.AreEqual(5f, second.StoredOilBarrels);
        Assert.AreEqual(20f, otherFaction.StoredOilBarrels);
    }

    [Test]
    public void GetResourceTotals_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale()
    {
        var world = new World(nameof(GetResourceTotals_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale));
        try
        {
            EntityManager entityManager = world.EntityManager;
            Entity entity = entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
            entityManager.SetComponentData(entity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 21,
                OilStorageCapacity = 100,
                StoredOilBarrels = 9f
            });

            var building = new RuntimeBuildingEntity
            {
                Id = 21,
                Definition = new BuildingDefinition
                {
                    OilStorageCapacity = 100
                },
                CombatEntity = entity,
                StoredOilBarrels = 2f
            };
            var buildings = new Dictionary<int, RuntimeBuildingEntity> { { building.Id, building } };

            new FactionResourceCompositionSystemHelper().GetResourceTotals(
                entityManager,
                buildings,
                out int oil,
                out int fuel);

            Assert.AreEqual(9, oil);
            Assert.AreEqual(0, fuel);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void TryGetFactionResourceEconomy_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale()
    {
        var world = new World(nameof(TryGetFactionResourceEconomy_PrefersLiveEcsStorageWhenRuntimeMirrorIsStale));
        try
        {
            EntityManager entityManager = world.EntityManager;
            Entity entity = entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
            entityManager.SetComponentData(entity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 22,
                OwnerFactionId = 2,
                OilStorageCapacity = 100,
                OilBarrelsPerDay = 4f,
                StoredOilBarrels = 13f
            });

            var building = new RuntimeBuildingEntity
            {
                Id = 22,
                Definition = new BuildingDefinition
                {
                    OilStorageCapacity = 100,
                    OilBarrelsPerDay = 4f
                },
                HasOwnerFaction = true,
                OwnerFactionId = 2,
                CombatEntity = entity,
                StoredOilBarrels = 3f
            };
            var buildings = new Dictionary<int, RuntimeBuildingEntity> { { building.Id, building } };

            bool found = new FactionResourceCompositionSystemHelper().TryGetFactionResourceEconomy(
                entityManager,
                buildings,
                2,
                out FactionResourceCompositionSystemHelper.ResourceEconomySnapshot snapshot);

            Assert.IsTrue(found);
            Assert.AreEqual(1, snapshot.ResourceBuildingCount);
            Assert.AreEqual(13f, snapshot.StoredOilBarrels);
            Assert.AreEqual(0f, snapshot.StoredFuelBarrels);
            Assert.AreEqual(4f, snapshot.OilBarrelsPerDay);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void DrainFactionResource_PrefersLiveEcsStorageAndMirrorsResult()
    {
        var world = new World(nameof(DrainFactionResource_PrefersLiveEcsStorageAndMirrorsResult));
        try
        {
            EntityManager entityManager = world.EntityManager;
            Entity entity = entityManager.CreateEntity(typeof(BuildingResourceStorageComponent));
            entityManager.SetComponentData(entity, new BuildingResourceStorageComponent
            {
                RuntimeBuildingId = 23,
                OwnerFactionId = 2,
                OilStorageCapacity = 100,
                StoredOilBarrels = 9f
            });

            var building = new RuntimeBuildingEntity
            {
                Id = 23,
                Definition = new BuildingDefinition
                {
                    OilStorageCapacity = 100
                },
                HasOwnerFaction = true,
                OwnerFactionId = 2,
                CombatEntity = entity,
                StoredOilBarrels = 2f
            };
            var buildings = new Dictionary<int, RuntimeBuildingEntity> { { building.Id, building } };

            float drained = new FactionResourceCompositionSystemHelper().DrainFactionResource(
                entityManager,
                buildings,
                2,
                5f,
                FactionResourceCompositionSystemHelper.ResourceKind.Oil);

            BuildingResourceStorageComponent storage =
                entityManager.GetComponentData<BuildingResourceStorageComponent>(entity);
            Assert.AreEqual(5f, drained);
            Assert.AreEqual(4f, storage.StoredOilBarrels);
            Assert.AreEqual(4f, building.StoredOilBarrels);
        }
        finally
        {
            world.Dispose();
        }
    }

    [Test]
    public void TryGetPrimaryCapacityInfo_DerivesOilCapacityForFuelProducer()
    {
        var building = new TestResourceBuilding
        {
            FuelStorageCapacity = 10,
            FuelBarrelsPerDay = 5f,
            StoredOilBarrels = 7.2f
        };

        var system = new FactionResourceCompositionSystemHelper();
        bool found = system.TryGetPrimaryCapacityInfo(building, 2f, out int current, out int max, out float progress01);

        Assert.IsTrue(found);
        Assert.AreEqual(8, current);
        Assert.AreEqual(20, max);
        Assert.AreEqual(0.36f, progress01, 0.0001f);
    }

    [Test]
    public void UpdateResourceProduction_ExtractsOilUpToCapacity()
    {
        var oilPump = new TestResourceBuilding
        {
            OilStorageCapacity = 10,
            OilBarrelsPerDay = 20f,
            StoredOilBarrels = 9f
        };
        var buildings = new Dictionary<int, TestResourceBuilding> { { 1, oilPump } };

        var system = new FactionResourceCompositionSystemHelper();
        FactionResourceCompositionSystemHelper.ResourceProductionTickResult result = system.UpdateResourceProduction(buildings, 10f, 1f, 2f);

        Assert.AreEqual(10f, oilPump.StoredOilBarrels);
        Assert.AreEqual(1f, result.OilExtractedBarrels);
        Assert.AreEqual(0f, result.FuelProducedBarrels);
    }

    [Test]
    public void UpdateResourceProduction_ConvertsOilIntoFuel()
    {
        var refinery = new TestResourceBuilding
        {
            FuelStorageCapacity = 10,
            FuelBarrelsPerDay = 10f,
            StoredOilBarrels = 8f,
            StoredFuelBarrels = 9.5f
        };
        var buildings = new Dictionary<int, TestResourceBuilding> { { 1, refinery } };

        var system = new FactionResourceCompositionSystemHelper();
        FactionResourceCompositionSystemHelper.ResourceProductionTickResult result = system.UpdateResourceProduction(buildings, 10f, 1f, 2f);

        Assert.AreEqual(7f, refinery.StoredOilBarrels);
        Assert.AreEqual(10f, refinery.StoredFuelBarrels);
        Assert.AreEqual(0f, result.OilExtractedBarrels);
        Assert.AreEqual(0.5f, result.FuelProducedBarrels);
    }

    [Test]
    public void UpdateResourceProduction_ClampsOilAndFuelWhenLargeDeltaOverfillsStorage()
    {
        var oilPump = new TestResourceBuilding
        {
            OilStorageCapacity = 12,
            OilBarrelsPerDay = 120f,
            StoredOilBarrels = 9f
        };
        var refinery = new TestResourceBuilding
        {
            FuelStorageCapacity = 6,
            FuelBarrelsPerDay = 60f,
            StoredOilBarrels = 100f,
            StoredFuelBarrels = 5f
        };
        var buildings = new Dictionary<int, TestResourceBuilding>
        {
            { 1, oilPump },
            { 2, refinery }
        };

        var system = new FactionResourceCompositionSystemHelper();
        FactionResourceCompositionSystemHelper.ResourceProductionTickResult result = system.UpdateResourceProduction(buildings, 10f, 5f, 2f);

        Assert.AreEqual(12f, oilPump.StoredOilBarrels);
        Assert.AreEqual(98f, refinery.StoredOilBarrels);
        Assert.AreEqual(6f, refinery.StoredFuelBarrels);
        Assert.AreEqual(3f, result.OilExtractedBarrels);
        Assert.AreEqual(1f, result.FuelProducedBarrels);
    }

    [Test]
    public void UpdateResourceProduction_DoesNotConvertOilWhenFuelStorageIsFull()
    {
        var refinery = new TestResourceBuilding
        {
            FuelStorageCapacity = 6,
            FuelBarrelsPerDay = 60f,
            StoredOilBarrels = 100f,
            StoredFuelBarrels = 6f
        };
        var buildings = new Dictionary<int, TestResourceBuilding> { { 1, refinery } };

        var system = new FactionResourceCompositionSystemHelper();
        FactionResourceCompositionSystemHelper.ResourceProductionTickResult result = system.UpdateResourceProduction(buildings, 10f, 5f, 2f);

        Assert.AreEqual(100f, refinery.StoredOilBarrels);
        Assert.AreEqual(6f, refinery.StoredFuelBarrels);
        Assert.AreEqual(0f, result.OilExtractedBarrels);
        Assert.AreEqual(0f, result.FuelProducedBarrels);
    }

    [Test]
    public void UpdateResourceProduction_IgnoresDestroyedBuildings()
    {
        var building = new TestResourceBuilding
        {
            IsDestroyed = true,
            OilStorageCapacity = 10,
            OilBarrelsPerDay = 100f,
            FuelStorageCapacity = 10,
            FuelBarrelsPerDay = 100f,
            StoredOilBarrels = 5f,
            StoredFuelBarrels = 5f
        };
        var buildings = new Dictionary<int, TestResourceBuilding> { { 1, building } };

        var system = new FactionResourceCompositionSystemHelper();
        FactionResourceCompositionSystemHelper.ResourceProductionTickResult result = system.UpdateResourceProduction(buildings, 10f, 5f, 2f);

        Assert.AreEqual(5f, building.StoredOilBarrels);
        Assert.AreEqual(5f, building.StoredFuelBarrels);
        Assert.AreEqual(0f, result.OilExtractedBarrels);
        Assert.AreEqual(0f, result.FuelProducedBarrels);
    }

    private sealed class TestResourceBuilding : FactionResourceCompositionSystemHelper.IResourceBuilding
    {
        public bool IsDestroyed { get; set; }
        public bool HasOwnerFaction { get; set; }
        public byte OwnerFactionId { get; set; }
        public int OilStorageCapacity { get; set; }
        public int FuelStorageCapacity { get; set; }
        public float OilBarrelsPerDay { get; set; }
        public float FuelBarrelsPerDay { get; set; }
        public float StoredOilBarrels { get; set; }
        public float StoredFuelBarrels { get; set; }
    }
}
#endif
