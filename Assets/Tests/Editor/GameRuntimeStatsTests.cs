using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class GameRuntimeStatsTests
{
    [SetUp]
    public void SetUp()
    {
        GameRuntimeStats.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        GameRuntimeStats.Reset();
    }

    [Test]
    public void Snapshot_AccumulatesResourceAndBuildStats()
    {
        GameRuntimeStats.RecordOilExtracted(2.75f);
        GameRuntimeStats.RecordFuelProduced(1.25f);
        GameRuntimeStats.RecordBuildingBuilt();
        GameRuntimeStats.RecordBuildingBuilt();

        GameRuntimeStats.Snapshot snapshot = GameRuntimeStats.GetSnapshot();

        Assert.AreEqual(2, snapshot.OilExtracted);
        Assert.AreEqual(1, snapshot.FuelProduced);
        Assert.AreEqual(2, snapshot.BuildingsBuilt);
    }

    [Test]
    public void Snapshot_AccumulatesMissionObjectiveStats()
    {
        GameRuntimeStats.RecordMissionElapsed(42.9f);
        GameRuntimeStats.RecordMissionElapsed(17.2f);
        GameRuntimeStats.RecordCiviliansProtected(3);
        GameRuntimeStats.RecordCapturedOrDestroyedBuilding();
        GameRuntimeStats.RecordCapturedOrDestroyedBuilding();

        GameRuntimeStats.Snapshot snapshot = GameRuntimeStats.GetSnapshot();

        Assert.AreEqual(60, snapshot.MissionElapsedSeconds);
        Assert.AreEqual(3, snapshot.CiviliansProtected);
        Assert.AreEqual(2, snapshot.CapturedOrDestroyedBuildings);
    }

    [Test]
    public void RecordUnitOrdered_ClassifiesSoldiersVehiclesAndAmmo()
    {
        var soldier = new GameObject("Unit_Chr_Soldier_Test");
        var vehicle = new GameObject("Unit_Veh_Truck_Test");
        var ammo = new GameObject("Ammo_Crate_Test");

        try
        {
            GameRuntimeStats.RecordUnitOrdered(soldier);
            GameRuntimeStats.RecordUnitOrdered(vehicle);
            GameRuntimeStats.RecordUnitOrdered(ammo);

            GameRuntimeStats.Snapshot snapshot = GameRuntimeStats.GetSnapshot();

            Assert.AreEqual(1, snapshot.SoldiersOrdered);
            Assert.AreEqual(1, snapshot.VehiclesOrdered);
            Assert.AreEqual(1, snapshot.AmmoOrdered);
        }
        finally
        {
            Object.DestroyImmediate(ammo);
            Object.DestroyImmediate(vehicle);
            Object.DestroyImmediate(soldier);
        }
    }

    [Test]
    public void IsMilitarySoldierEntity_RejectsCivilianAndVehicleFootprints()
    {
        using var world = new World("GameRuntimeStatsTests");
        EntityManager em = world.EntityManager;

        Entity soldier = em.CreateEntity(typeof(UnitFootprint));
        em.SetComponentData(soldier, new UnitFootprint { Size = new int2(1, 1) });

        Entity civilian = em.CreateEntity(typeof(UnitFootprint), typeof(CivilianUnitTag));
        em.SetComponentData(civilian, new UnitFootprint { Size = new int2(1, 1) });

        Entity vehicle = em.CreateEntity(typeof(UnitFootprint));
        em.SetComponentData(vehicle, new UnitFootprint { Size = new int2(2, 1) });

        Assert.IsTrue(GameRuntimeStats.IsMilitarySoldierEntity(em, soldier));
        Assert.IsFalse(GameRuntimeStats.IsMilitarySoldierEntity(em, civilian));
        Assert.IsFalse(GameRuntimeStats.IsMilitarySoldierEntity(em, vehicle));
    }
}
