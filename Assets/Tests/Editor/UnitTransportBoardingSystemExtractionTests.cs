#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class UnitTransportBoardingSystemExtractionTests
{
    [Test]
    public void IsPersonnelTransportName_RecognizesKnownPersonnelTransports()
    {
        var capacitySystem = new UnitTransportCapacitySystem();

        Assert.IsTrue(capacitySystem.IsPersonnelTransportName("Unit_Veh_APC_Fast"));
        Assert.IsTrue(capacitySystem.IsPersonnelTransportName("Unit_Veh_Helicopter_Transport"));
        Assert.IsFalse(capacitySystem.IsPersonnelTransportName("Unit_Veh_Tank_Heavy"));
    }

    [Test]
    public void TryEnsureTransportCapacity_AddsCapacityAndPassengerBufferForKnownTransport()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        var capacitySystem = new UnitTransportCapacitySystem();
        EntityManager entityManager = world.EntityManager;
        Entity transport = entityManager.CreateEntity(typeof(UnitSourcePrefabKey));
        entityManager.SetComponentData(transport, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_APC_01") });

        Assert.IsTrue(capacitySystem.TryEnsureTransportCapacity(entityManager, transport));
        Assert.AreEqual(10, entityManager.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
        Assert.IsTrue(entityManager.HasBuffer<UnitTransportPassengerElement>(transport));
    }

    [Test]
    public void IsSoldierBoardingCandidate_AcceptsPlayerCharactersAndRejectsVehicles()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        EntityManager entityManager = world.EntityManager;
        Entity soldier = CreateBoardingCandidate(entityManager, "Unit_Chr_Rifleman");
        Entity vehicle = CreateBoardingCandidate(entityManager, "Unit_Veh_APC_01");

        Assert.IsTrue(TransportBoardingCommandSystem.IsSoldierBoardingCandidate(entityManager, soldier));
        Assert.IsFalse(TransportBoardingCommandSystem.IsSoldierBoardingCandidate(entityManager, vehicle));
    }

    [Test]
    public void ReserveFootprintCells_ReservesAllFootprintCellsWithinGrid()
    {
        GridConfig grid = new() { Width = 8, Height = 8 };
        HashSet<int> reserved = new();

        TransportBoardingCommandSystem.ReserveFootprintCells(grid, new int2(2, 2), new int2(2, 1), reserved);

        CollectionAssert.AreEquivalent(new[] { 18, 19 }, reserved);
    }

    private static Entity CreateBoardingCandidate(EntityManager entityManager, string sourceName)
    {
        Entity entity = entityManager.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior),
            typeof(UnitSourcePrefabKey));
        entityManager.SetComponentData(entity, new Faction { Id = FactionIdentity.PlayerFactionId });
        entityManager.SetComponentData(entity, new UnitGrid { Cell = new int2(1, 1) });
        entityManager.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        entityManager.SetComponentData(entity, new UnitSourcePrefabKey { Value = new FixedString64Bytes(sourceName) });
        return entity;
    }
}
#endif
