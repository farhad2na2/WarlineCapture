using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class BuildingResourceProductionEcsSystemTests
{
    public static void RunFocusedValidation()
    {
        var tests = new BuildingResourceProductionEcsSystemTests();
        try
        {
            tests.ApplyTick_ExtractsOilUpToCapacity();
            tests.ApplyTick_ConvertsOilIntoFuel();
            tests.ApplyTick_DoesNotConvertOilWhenFuelStorageIsFull();
            tests.ProductionRuntimeTick_SyncsResourceStorageMirrorAfterProductionUpdate();
            tests.ProductionRuntimeTick_SyncsResourceStorageMirrorAfterHaulerUpdate();
            Debug.Log("[BuildingResourceProductionEcsFocusedValidation] result=Passed tests=5");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[BuildingResourceProductionEcsFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void ApplyTick_ExtractsOilUpToCapacity()
    {
        var storage = new BuildingResourceStorageComponent
        {
            OilStorageCapacity = 10,
            OilBarrelsPerDay = 20f,
            StoredOilBarrels = 9f
        };

        BuildingResourceProductionEcsSystem.TickResult result =
            BuildingResourceProductionEcsSystem.ApplyTick(ref storage, 10f, 1f, 2f);

        Assert.AreEqual(10f, storage.StoredOilBarrels);
        Assert.AreEqual(0f, storage.StoredFuelBarrels);
        Assert.AreEqual(1f, result.OilExtractedBarrels);
        Assert.AreEqual(0f, result.FuelProducedBarrels);
    }

    [Test]
    public void ApplyTick_ConvertsOilIntoFuel()
    {
        var storage = new BuildingResourceStorageComponent
        {
            FuelStorageCapacity = 10,
            FuelBarrelsPerDay = 10f,
            StoredOilBarrels = 8f,
            StoredFuelBarrels = 9.5f
        };

        BuildingResourceProductionEcsSystem.TickResult result =
            BuildingResourceProductionEcsSystem.ApplyTick(ref storage, 10f, 1f, 2f);

        Assert.AreEqual(7f, storage.StoredOilBarrels);
        Assert.AreEqual(10f, storage.StoredFuelBarrels);
        Assert.AreEqual(0f, result.OilExtractedBarrels);
        Assert.AreEqual(0.5f, result.FuelProducedBarrels);
    }

    [Test]
    public void ApplyTick_DoesNotConvertOilWhenFuelStorageIsFull()
    {
        var storage = new BuildingResourceStorageComponent
        {
            FuelStorageCapacity = 6,
            FuelBarrelsPerDay = 60f,
            StoredOilBarrels = 100f,
            StoredFuelBarrels = 6f
        };

        BuildingResourceProductionEcsSystem.TickResult result =
            BuildingResourceProductionEcsSystem.ApplyTick(ref storage, 10f, 5f, 2f);

        Assert.AreEqual(100f, storage.StoredOilBarrels);
        Assert.AreEqual(6f, storage.StoredFuelBarrels);
        Assert.AreEqual(0f, result.OilExtractedBarrels);
        Assert.AreEqual(0f, result.FuelProducedBarrels);
    }

    [Test]
    public void ProductionRuntimeTick_SyncsResourceStorageMirrorAfterProductionUpdate()
    {
        var building = new RuntimeBuildingEntity
        {
            Id = 12,
            Definition = new BuildingDefinition
            {
                OilStorageCapacity = 20,
                OilBarrelsPerDay = 4f
            },
            StoredOilBarrels = 7f
        };
        var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
        {
            { building.Id, building }
        };
        int syncCount = 0;
        float syncedOil = -1f;
        var context = new BuildingProductionRuntimeTickCompositionSystemHelper.Context(
            runtimeBuildings,
            null,
            new FactionResourceCompositionSystemHelper(),
            null,
            default,
            null,
            default,
            null,
            null,
            null,
            null,
            null,
            null,
            2f,
            syncedBuilding =>
            {
                syncCount++;
                syncedOil = syncedBuilding.StoredOilBarrels;
            });

        new BuildingProductionRuntimeTickCompositionSystemHelper().UpdateResourceProduction(context);

        Assert.AreEqual(1, syncCount);
        Assert.AreEqual(7f, syncedOil);
    }

    [Test]
    public void ProductionRuntimeTick_SyncsResourceStorageMirrorAfterHaulerUpdate()
    {
        var building = new RuntimeBuildingEntity
        {
            Id = 13,
            Definition = new BuildingDefinition
            {
                OilStorageCapacity = 30
            },
            StoredOilBarrels = 11f
        };
        var runtimeBuildings = new System.Collections.Generic.Dictionary<int, RuntimeBuildingEntity>
        {
            { building.Id, building }
        };
        int syncCount = 0;
        float syncedOil = -1f;
        var context = new BuildingProductionRuntimeTickCompositionSystemHelper.Context(
            runtimeBuildings,
            null,
            null,
            null,
            default,
            null,
            default,
            null,
            null,
            null,
            null,
            null,
            null,
            2f,
            syncedBuilding =>
            {
                syncCount++;
                syncedOil = syncedBuilding.StoredOilBarrels;
            });

        new BuildingProductionRuntimeTickCompositionSystemHelper().UpdateResourceHaulers(context);

        Assert.AreEqual(1, syncCount);
        Assert.AreEqual(11f, syncedOil);
    }
}
#endif
