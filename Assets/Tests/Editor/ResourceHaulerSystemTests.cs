#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using Unity.Mathematics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ResourceHaulerSystemTests
{
    public static void RunFocusedValidation()
    {
        var tests = new ResourceHaulerSystemTests();
        try
        {
            tests.CreateOrder_InitializesTravelToSource();
            tests.SetTravelPhase_UpdatesTargetAndClearsTimer();
            tests.AdvanceTimedAction_StartsWaitsThenCompletes();
            tests.TryCompleteLoad_MovesOilFromSourceIntoHaulerCargo();
            tests.TryCompleteLoad_DoesNotLoadWhenSourceIsShort();
            tests.RevertLoad_ReturnsCargoToSource();
            tests.TryCompleteUnload_ClampsDestinationCapacityAndClearsCargo();
            tests.TryCompleteUnload_WaitsWhenDestinationCannotFitCargo();
            tests.Classification_DetectsHaulerSourceAndDestinationRoles();
            Debug.Log("[ResourceHaulerFocusedValidation] result=Passed tests=9");
            ValidationExit.Exit(0);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ResourceHaulerFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void CreateOrder_InitializesTravelToSource()
    {
        var system = new ResourceHaulerSystem();
        UnitResourceHaulOrder order = system.CreateOrder(
            sourceBuildingId: 12,
            destinationBuildingId: 34,
            targetCell: new int2(5, 7),
            resourceKind: ResourceHaulerSystem.ResourceHaulKind.Fuel);

        Assert.AreEqual(12, order.SourceBuildingId);
        Assert.AreEqual(34, order.DestinationBuildingId);
        Assert.AreEqual(new int2(5, 7), order.TargetCell);
        Assert.AreEqual((byte)ResourceHaulerSystem.ResourceHaulPhase.ToSource, order.Phase);
        Assert.AreEqual((byte)ResourceHaulerSystem.ResourceHaulKind.Fuel, order.ResourceKind);
        Assert.AreEqual(0f, order.ActionEndsAt);
    }

    [Test]
    public void SetTravelPhase_UpdatesTargetAndClearsTimer()
    {
        UnitResourceHaulOrder order = new()
        {
            ActionEndsAt = 25f
        };

        var system = new ResourceHaulerSystem();
        system.SetTravelPhase(ref order, ResourceHaulerSystem.ResourceHaulPhase.ToDestination, new int2(9, 4));

        Assert.AreEqual((byte)ResourceHaulerSystem.ResourceHaulPhase.ToDestination, order.Phase);
        Assert.AreEqual(new int2(9, 4), order.TargetCell);
        Assert.AreEqual(0f, order.ActionEndsAt);
    }

    [Test]
    public void AdvanceTimedAction_StartsWaitsThenCompletes()
    {
        UnitResourceHaulOrder order = default;

        var system = new ResourceHaulerSystem();
        ResourceHaulerSystem.TimedActionState started = system.AdvanceTimedAction(ref order, now: 10f, durationSeconds: 3f);
        ResourceHaulerSystem.TimedActionState waiting = system.AdvanceTimedAction(ref order, now: 12f, durationSeconds: 3f);
        ResourceHaulerSystem.TimedActionState ready = system.AdvanceTimedAction(ref order, now: 13f, durationSeconds: 3f);

        Assert.AreEqual(ResourceHaulerSystem.TimedActionState.Started, started);
        Assert.AreEqual(13f, order.ActionEndsAt);
        Assert.AreEqual(ResourceHaulerSystem.TimedActionState.Waiting, waiting);
        Assert.AreEqual(ResourceHaulerSystem.TimedActionState.Ready, ready);
    }

    [Test]
    public void TryCompleteLoad_MovesOilFromSourceIntoHaulerCargo()
    {
        var source = new TestHaulerBuilding
        {
            OilStorageCapacity = 100,
            OilBarrelsPerDay = 12f,
            StoredOilBarrels = 30f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 4f
        };

        var system = new ResourceHaulerSystem();
        bool loaded = system.TryCompleteLoad(source, ResourceHaulerSystem.ResourceHaulKind.Oil, 10f, ref hauler);

        Assert.IsTrue(loaded);
        Assert.AreEqual(20f, source.StoredOilBarrels);
        Assert.AreEqual(10f, hauler.CargoOilBarrels);
        Assert.AreEqual(0f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void TryCompleteLoad_DoesNotLoadWhenSourceIsShort()
    {
        var source = new TestHaulerBuilding
        {
            OilStorageCapacity = 100,
            StoredOilBarrels = 9.5f
        };
        UnitResourceHauler hauler = default;

        var system = new ResourceHaulerSystem();
        bool loaded = system.TryCompleteLoad(source, ResourceHaulerSystem.ResourceHaulKind.Oil, 10f, ref hauler);

        Assert.IsFalse(loaded);
        Assert.AreEqual(9.5f, source.StoredOilBarrels);
        Assert.AreEqual(0f, hauler.CargoOilBarrels);
    }

    [Test]
    public void RevertLoad_ReturnsCargoToSource()
    {
        var source = new TestHaulerBuilding
        {
            FuelStorageCapacity = 50,
            FuelBarrelsPerDay = 10f,
            StoredFuelBarrels = 12f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 5f
        };

        var system = new ResourceHaulerSystem();
        system.RevertLoad(source, ResourceHaulerSystem.ResourceHaulKind.Fuel, 5f, ref hauler);

        Assert.AreEqual(17f, source.StoredFuelBarrels);
        Assert.AreEqual(0f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void TryCompleteUnload_ClampsDestinationCapacityAndClearsCargo()
    {
        var destination = new TestHaulerBuilding
        {
            FuelStorageCapacity = 20,
            StoredFuelBarrels = 15f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 5f
        };

        var system = new ResourceHaulerSystem();
        bool unloaded = system.TryCompleteUnload(destination, ResourceHaulerSystem.ResourceHaulKind.Fuel, ref hauler);

        Assert.IsTrue(unloaded);
        Assert.AreEqual(20f, destination.StoredFuelBarrels);
        Assert.AreEqual(0f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void TryCompleteUnload_WaitsWhenDestinationCannotFitCargo()
    {
        var destination = new TestHaulerBuilding
        {
            FuelStorageCapacity = 20,
            StoredFuelBarrels = 16f
        };
        UnitResourceHauler hauler = new()
        {
            CargoFuelBarrels = 5f
        };

        var system = new ResourceHaulerSystem();
        bool unloaded = system.TryCompleteUnload(destination, ResourceHaulerSystem.ResourceHaulKind.Fuel, ref hauler);

        Assert.IsFalse(unloaded);
        Assert.AreEqual(16f, destination.StoredFuelBarrels);
        Assert.AreEqual(5f, hauler.CargoFuelBarrels);
    }

    [Test]
    public void Classification_DetectsHaulerSourceAndDestinationRoles()
    {
        var oilSource = new TestHaulerBuilding
        {
            OilStorageCapacity = 100,
            OilBarrelsPerDay = 10f
        };
        var fuelSource = new TestHaulerBuilding
        {
            FuelStorageCapacity = 20,
            FuelBarrelsPerDay = 5f,
            StoredFuelBarrels = 2f
        };

        var system = new ResourceHaulerSystem();

        Assert.IsTrue(system.IsOilSourceBuilding(oilSource));
        Assert.IsTrue(system.IsFuelBuilding(fuelSource));
        Assert.IsTrue(system.HasAvailableFuelForHauler(fuelSource));
    }

    private sealed class TestHaulerBuilding : FactionResourceSystem.IResourceBuilding
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
