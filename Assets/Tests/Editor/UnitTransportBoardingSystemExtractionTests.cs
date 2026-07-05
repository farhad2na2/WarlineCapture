using Game.Components;
using Game.Runtime;
using Game.Tactical.Contracts;
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
    public void IsTransportPlaneName_RecognizesTransportPlaneSources()
    {
        var capacitySystem = new UnitTransportCapacitySystem();

        Assert.IsTrue(capacitySystem.IsTransportPlaneName("Unit_Veh_Plane_Transport"));
        Assert.IsTrue(capacitySystem.IsTransportPlaneName("SM_Veh_TransportPlane_01"));
        Assert.IsFalse(capacitySystem.IsTransportPlaneName("Unit_Veh_Jet_Fighter"));
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
    public void TryEnsureTransportCapacity_AddsCargoCapacityForTransportPlane()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        var capacitySystem = new UnitTransportCapacitySystem();
        EntityManager entityManager = world.EntityManager;
        Entity transport = entityManager.CreateEntity(typeof(UnitSourcePrefabKey));
        entityManager.SetComponentData(transport, new UnitSourcePrefabKey { Value = new FixedString64Bytes("Unit_Veh_Plane_Transport") });

        Assert.IsTrue(capacitySystem.TryEnsureTransportCapacity(entityManager, transport));
        Assert.AreEqual(24, entityManager.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
        Assert.IsTrue(entityManager.HasComponent<UnitTransportCargoCapacity>(transport));
        UnitTransportCargoCapacity cargoCapacity = entityManager.GetComponentData<UnitTransportCargoCapacity>(transport);
        Assert.AreEqual(24, cargoCapacity.SoldierCapacity);
        Assert.AreEqual(2, cargoCapacity.VehicleCapacity);
        Assert.AreEqual(0, cargoCapacity.CargoWeightCapacity);
        Assert.IsTrue(entityManager.HasBuffer<UnitTransportPassengerElement>(transport));
    }

    [Test]
    public void ResolveTransportCargoCapacity_PreservesAuthoredCargoCapacity()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        var capacitySystem = new UnitTransportCapacitySystem();
        EntityManager entityManager = world.EntityManager;
        Entity transport = entityManager.CreateEntity(typeof(UnitTransportCapacity), typeof(UnitTransportCargoCapacity));
        entityManager.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 12 });
        entityManager.SetComponentData(transport, new UnitTransportCargoCapacity
        {
            SoldierCapacity = 24,
            VehicleCapacity = 3,
            CargoWeightCapacity = 40
        });

        UnitTransportCargoCapacity capacity = capacitySystem.ResolveTransportCargoCapacity(entityManager, transport);

        Assert.AreEqual(24, capacity.SoldierCapacity);
        Assert.AreEqual(3, capacity.VehicleCapacity);
        Assert.AreEqual(40, capacity.CargoWeightCapacity);
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

    [Test]
    public void CommandRoutingHelper_IdentifiesTransportIntentKinds()
    {
        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(RtsSelectionCommandIntentKind.BoardTransport));
        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(RtsSelectionCommandIntentKind.BoardSelectedTransport));
        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger));
        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(RtsSelectionCommandIntentKind.BoardNearestSoldiers));
        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(RtsSelectionCommandIntentKind.BoardAllSelectedTransport));
        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(RtsSelectionCommandIntentKind.DisembarkTransport));
        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(RtsSelectionCommandIntentKind.DisembarkTransportPassenger));
        Assert.IsFalse(TransportBoardingCommandRoutingSystemHelper.IsTransportCommandIntent(RtsSelectionCommandIntentKind.Move));
    }

    [Test]
    public void CommandRoutingHelper_PreResolvedTransportIntentRequiresResolvedTargets()
    {
        Entity target = new() { Index = 10, Version = 1 };
        Entity secondary = new() { Index = 11, Version = 1 };

        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsPreResolvedTransportCommandIntent(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport,
            HasTargetEntity = 1,
            TargetEntity = target
        }));
        Assert.IsFalse(TransportBoardingCommandRoutingSystemHelper.IsPreResolvedTransportCommandIntent(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport
        }));
        Assert.IsTrue(TransportBoardingCommandRoutingSystemHelper.IsPreResolvedTransportCommandIntent(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger,
            HasTargetEntity = 1,
            HasSecondaryTargetEntity = 1,
            TargetEntity = target,
            SecondaryTargetEntity = secondary
        }));
        Assert.IsFalse(TransportBoardingCommandRoutingSystemHelper.IsPreResolvedTransportCommandIntent(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardSelectedTransportPassenger,
            HasTargetEntity = 1,
            TargetEntity = target
        }));
        Assert.IsFalse(TransportBoardingCommandRoutingSystemHelper.IsPreResolvedTransportCommandIntent(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Move,
            HasTargetEntity = 1,
            TargetEntity = target
        }));
    }

    [Test]
    public void CommandRoutingHelper_MapsBoardingResultToCommandResultElement()
    {
        RtsSelectionCommandIntentRequestElement request = new()
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport,
            RequestId = 42,
            Frame = 19,
            HasTargetEntity = 1,
            TargetEntity = new Entity { Index = 12, Version = 1 },
            ScreenPosition = new float2(5f, 7f)
        };
        TransportBoardingCommandSystem.Result accepted = TransportBoardingCommandSystem.Result.AcceptedAt(
            new int2(3, 4),
            new float3(3.5f, 0f, 4.5f),
            FactionIdentity.PlayerFactionId,
            "Boarding.");

        RtsSelectionCommandResultElement result =
            TransportBoardingCommandRoutingSystemHelper.ToBoardingCommandResultElement(request, accepted);

        Assert.AreEqual(request.Kind, result.Kind);
        Assert.AreEqual(42, result.RequestId);
        Assert.AreEqual(19, result.Frame);
        Assert.AreEqual((int)TacticalCommandMode.Board, result.CommandMode);
        Assert.AreEqual(1, result.HasCommandResult);
        Assert.AreEqual(1, result.Accepted);
        Assert.AreEqual(0, result.ReasonCode);
        Assert.AreEqual(RtsSelectionCommandTargetKind.Cell, result.TargetKind);
        Assert.AreEqual(new int2(3, 4), result.TargetCell);
        Assert.AreEqual(1, result.HasTargetEntity);
        Assert.AreEqual(1, result.HasTargetCell);
        Assert.AreEqual(1, result.HasWorldPosition);
        Assert.AreEqual(1, result.ShowWorldMarkers);
        Assert.AreEqual(new FixedString64Bytes("Boarding."), result.Message);
    }

    [Test]
    public void CommandRoutingHelper_AddCommandResultPrefersLiveCommandBuffer()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        EntityManager entityManager = world.EntityManager;
        Entity commandEntity = entityManager.CreateEntity();
        entityManager.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Entity fallbackEntity = entityManager.CreateEntity();
        DynamicBuffer<RtsSelectionCommandResultElement> fallback =
            entityManager.AddBuffer<RtsSelectionCommandResultElement>(fallbackEntity);
        RtsSelectionCommandResultElement result = new()
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport,
            RequestId = 77
        };

        TransportBoardingCommandRoutingSystemHelper.AddCommandResult(entityManager, commandEntity, fallback, result);

        Assert.AreEqual(1, entityManager.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Length);
        Assert.AreEqual(77, entityManager.GetBuffer<RtsSelectionCommandResultElement>(commandEntity)[0].RequestId);
        Assert.AreEqual(0, entityManager.GetBuffer<RtsSelectionCommandResultElement>(fallbackEntity).Length);
    }

    [Test]
    public void CommandRoutingHelper_RefreshCommandBuffersReacquiresLiveBuffers()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        EntityManager entityManager = world.EntityManager;
        Entity commandEntity = entityManager.CreateEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            entityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults =
            entityManager.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        commandRequests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.BoardTransport,
            RequestId = 12
        });
        Entity fallbackEntity = entityManager.CreateEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> fallbackRequests =
            entityManager.AddBuffer<RtsSelectionCommandIntentRequestElement>(fallbackEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> fallbackResults =
            entityManager.AddBuffer<RtsSelectionCommandResultElement>(fallbackEntity);

        TransportBoardingCommandRoutingSystemHelper.RefreshCommandBuffers(
            entityManager,
            commandEntity,
            ref fallbackRequests,
            ref fallbackResults);

        Assert.AreEqual(1, fallbackRequests.Length);
        Assert.AreEqual(12, fallbackRequests[0].RequestId);
        Assert.AreEqual(0, fallbackResults.Length);
        commandResults.Add(new RtsSelectionCommandResultElement { RequestId = 13 });
        TransportBoardingCommandRoutingSystemHelper.RefreshCommandBuffers(
            entityManager,
            commandEntity,
            ref fallbackRequests,
            ref fallbackResults);
        Assert.AreEqual(1, fallbackResults.Length);
        Assert.AreEqual(13, fallbackResults[0].RequestId);
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
