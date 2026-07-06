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
    public void CapacityHelper_ResolvesTransportSlotAvailabilityFromPassengersAndCapacity()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        EntityManager entityManager = world.EntityManager;
        Entity transport = entityManager.CreateEntity(
            typeof(UnitTransportCapacity),
            typeof(UnitTransportCargoCapacity));
        entityManager.SetComponentData(transport, new UnitTransportCapacity { SoldierCapacity = 4 });
        entityManager.SetComponentData(transport, new UnitTransportCargoCapacity
        {
            SoldierCapacity = 5,
            VehicleCapacity = 2,
            CargoWeightCapacity = 0
        });
        DynamicBuffer<UnitTransportPassengerElement> passengers =
            entityManager.AddBuffer<UnitTransportPassengerElement>(transport);
        Entity soldierPassenger = entityManager.CreateEntity();
        Entity vehiclePassenger = entityManager.CreateEntity(typeof(UnitTransportCargoPassenger));
        entityManager.SetComponentData(vehiclePassenger, new UnitTransportCargoPassenger
        {
            Transport = transport,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 1
        });
        passengers.Add(new UnitTransportPassengerElement { Passenger = soldierPassenger });
        passengers.Add(new UnitTransportPassengerElement { Passenger = vehiclePassenger });

        TransportSlotAvailability availability =
            TransportBoardingCapacitySystemHelper.ResolveTransportSlotAvailability(entityManager, transport);

        Assert.AreEqual(1, availability.OccupiedSoldierSeats);
        Assert.AreEqual(5, availability.SoldierCapacity);
        Assert.AreEqual(4, availability.AvailableSoldierSeats);
        Assert.AreEqual(1, availability.OccupiedVehicleSlots);
        Assert.AreEqual(2, availability.VehicleCapacity);
        Assert.AreEqual(1, availability.AvailableVehicleSlots);
    }

    [Test]
    public void CapacityHelper_ResolvesLoadedPassengerKindFromCargoPassenger()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        EntityManager entityManager = world.EntityManager;
        Entity transport = entityManager.CreateEntity();
        Entity vehiclePassenger = entityManager.CreateEntity(typeof(UnitTransportCargoPassenger));
        Entity soldierPassenger = entityManager.CreateEntity(typeof(UnitTransportCargoPassenger));
        entityManager.SetComponentData(vehiclePassenger, new UnitTransportCargoPassenger
        {
            Transport = transport,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 1
        });
        entityManager.SetComponentData(soldierPassenger, new UnitTransportCargoPassenger
        {
            Transport = transport,
            PassengerKind = 99,
            CargoWeight = 0
        });

        Assert.AreEqual(
            UnitTransportPassengerKind.Vehicle,
            TransportBoardingCapacitySystemHelper.ResolveLoadedPassengerKind(entityManager, transport, vehiclePassenger));
        Assert.AreEqual(
            UnitTransportPassengerKind.Soldier,
            TransportBoardingCapacitySystemHelper.ResolveLoadedPassengerKind(entityManager, transport, soldierPassenger));
        Assert.AreEqual(
            UnitTransportPassengerKind.Soldier,
            TransportBoardingCapacitySystemHelper.ResolveLoadedPassengerKind(entityManager, transport, Entity.Null));
    }

    [Test]
    public void CapacityHelper_CountsLoadedPassengerKindsWithinLimit()
    {
        using var world = new World("UnitTransportBoardingSystemExtractionTests");
        EntityManager entityManager = world.EntityManager;
        Entity transport = entityManager.CreateEntity();
        DynamicBuffer<UnitTransportPassengerElement> passengers =
            entityManager.AddBuffer<UnitTransportPassengerElement>(transport);
        Entity soldierPassenger = entityManager.CreateEntity();
        Entity firstVehiclePassenger = entityManager.CreateEntity(typeof(UnitTransportCargoPassenger));
        Entity secondVehiclePassenger = entityManager.CreateEntity(typeof(UnitTransportCargoPassenger));
        entityManager.SetComponentData(firstVehiclePassenger, new UnitTransportCargoPassenger
        {
            Transport = transport,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 1
        });
        entityManager.SetComponentData(secondVehiclePassenger, new UnitTransportCargoPassenger
        {
            Transport = transport,
            PassengerKind = UnitTransportPassengerKind.Vehicle,
            CargoWeight = 1
        });
        passengers.Add(new UnitTransportPassengerElement { Passenger = soldierPassenger });
        passengers.Add(new UnitTransportPassengerElement { Passenger = firstVehiclePassenger });
        passengers.Add(new UnitTransportPassengerElement { Passenger = secondVehiclePassenger });

        TransportBoardingCapacitySystemHelper.CountLoadedPassengerKinds(
            entityManager,
            transport,
            passengers,
            countLimit: 2,
            out int limitedSoldierCount,
            out int limitedVehicleCount);
        TransportBoardingCapacitySystemHelper.CountLoadedPassengerKinds(
            entityManager,
            transport,
            passengers,
            countLimit: 10,
            out int allSoldierCount,
            out int allVehicleCount);

        Assert.AreEqual(1, limitedSoldierCount);
        Assert.AreEqual(1, limitedVehicleCount);
        Assert.AreEqual(1, allSoldierCount);
        Assert.AreEqual(2, allVehicleCount);
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

    [Test]
    public void OrderPlanningHelper_ReservesSoldierAndVehicleSlots()
    {
        int plannedSoldierSeats = 0;
        int plannedVehicleSlots = 0;

        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
            UnitTransportPassengerKind.Soldier,
            availableSoldierSeats: 2,
            availableVehicleSlots: 1,
            ref plannedSoldierSeats,
            ref plannedVehicleSlots));
        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
            UnitTransportPassengerKind.Vehicle,
            availableSoldierSeats: 2,
            availableVehicleSlots: 1,
            ref plannedSoldierSeats,
            ref plannedVehicleSlots));

        Assert.AreEqual(1, plannedSoldierSeats);
        Assert.AreEqual(1, plannedVehicleSlots);
    }

    [Test]
    public void OrderPlanningHelper_RejectsFullPlannedSlotsWithoutIncrementing()
    {
        int plannedSoldierSeats = 1;
        int plannedVehicleSlots = 1;

        Assert.IsFalse(TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
            UnitTransportPassengerKind.Soldier,
            availableSoldierSeats: 1,
            availableVehicleSlots: 1,
            ref plannedSoldierSeats,
            ref plannedVehicleSlots));
        Assert.IsFalse(TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
            UnitTransportPassengerKind.Vehicle,
            availableSoldierSeats: 1,
            availableVehicleSlots: 1,
            ref plannedSoldierSeats,
            ref plannedVehicleSlots));

        Assert.AreEqual(1, plannedSoldierSeats);
        Assert.AreEqual(1, plannedVehicleSlots);
    }

    [Test]
    public void OrderPlanningHelper_ReservesStructPlannedSlotsAndReportsOccupancy()
    {
        TransportBoardingPlannedSlotCounts plannedSlots = default;

        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
            UnitTransportPassengerKind.Soldier,
            availableSoldierSeats: 2,
            availableVehicleSlots: 1,
            ref plannedSlots));
        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
            UnitTransportPassengerKind.Vehicle,
            availableSoldierSeats: 2,
            availableVehicleSlots: 1,
            ref plannedSlots));
        Assert.IsFalse(TransportBoardingOrderPlanningSystemHelper.TryReservePlannedBoardingSlot(
            UnitTransportPassengerKind.Vehicle,
            availableSoldierSeats: 2,
            availableVehicleSlots: 1,
            ref plannedSlots));

        Assert.AreEqual(1, plannedSlots.SoldierSeats);
        Assert.AreEqual(1, plannedSlots.VehicleSlots);
        Assert.AreEqual(4, TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSoldierOccupancy(3, plannedSlots));
        Assert.AreEqual(3, TransportBoardingOrderPlanningSystemHelper.ResolvePlannedVehicleOccupancy(2, plannedSlots));
    }

    [Test]
    public void OrderPlanningHelper_AppendsPlannedOrdersAndReservesSlots()
    {
        List<PendingTransportBoardingOrder> plannedOrders = new();
        TransportBoardingPlannedSlotCounts plannedSlots = default;
        PendingTransportBoardingOrder soldierOrder = new()
        {
            Passenger = new Entity { Index = 21, Version = 1 },
            PassengerKind = UnitTransportPassengerKind.Soldier
        };
        PendingTransportBoardingOrder vehicleOrder = new()
        {
            Passenger = new Entity { Index = 22, Version = 1 },
            PassengerKind = UnitTransportPassengerKind.Vehicle
        };

        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.TryAppendPlannedBoardingOrder(
            plannedOrders,
            soldierOrder,
            availableSoldierSeats: 1,
            availableVehicleSlots: 1,
            ref plannedSlots));
        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.TryAppendPlannedBoardingOrder(
            plannedOrders,
            vehicleOrder,
            availableSoldierSeats: 1,
            availableVehicleSlots: 1,
            ref plannedSlots));
        Assert.IsFalse(TransportBoardingOrderPlanningSystemHelper.TryAppendPlannedBoardingOrder(
            plannedOrders,
            vehicleOrder,
            availableSoldierSeats: 1,
            availableVehicleSlots: 1,
            ref plannedSlots));

        Assert.AreEqual(2, plannedOrders.Count);
        Assert.AreEqual(1, plannedSlots.SoldierSeats);
        Assert.AreEqual(1, plannedSlots.VehicleSlots);
        Assert.AreEqual(21, plannedOrders[0].Passenger.Index);
        Assert.AreEqual(22, plannedOrders[1].Passenger.Index);
    }

    [Test]
    public void OrderPlanningHelper_AppendsPlannedOrdersWithSlotAvailability()
    {
        List<PendingTransportBoardingOrder> plannedOrders = new();
        TransportBoardingPlannedSlotCounts plannedSlots = default;
        TransportSlotAvailability availability = new(
            occupiedSoldierSeats: 0,
            soldierCapacity: 1,
            occupiedVehicleSlots: 0,
            vehicleCapacity: 0);
        PendingTransportBoardingOrder soldierOrder = new()
        {
            Passenger = new Entity { Index = 31, Version = 1 },
            PassengerKind = UnitTransportPassengerKind.Soldier
        };

        Assert.AreEqual(
            TransportBoardingPlannedSlotRejectionKind.None,
            TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSlotRejection(
                soldierOrder.PassengerKind,
                availability,
                plannedSlots));
        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.TryAppendPlannedBoardingOrder(
            plannedOrders,
            soldierOrder,
            availability,
            ref plannedSlots));
        Assert.AreEqual(
            TransportBoardingPlannedSlotRejectionKind.NoSoldierSeats,
            TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSlotRejection(
                soldierOrder.PassengerKind,
                availability,
                plannedSlots));

        Assert.AreEqual(1, plannedOrders.Count);
        Assert.AreEqual(1, plannedSlots.SoldierSeats);
        Assert.AreEqual(0, plannedSlots.VehicleSlots);
        Assert.AreEqual(31, plannedOrders[0].Passenger.Index);
    }

    [Test]
    public void OrderPlanningHelper_CreatesPlannedOrderListWithResolvedCapacity()
    {
        int capacity = TransportBoardingOrderPlanningSystemHelper.ResolvePlannedOrderCapacity(
            candidateCount: 5,
            totalAvailableSlots: 3);
        List<PendingTransportBoardingOrder> plannedOrders =
            TransportBoardingOrderPlanningSystemHelper.CreatePlannedBoardingOrderList(capacity);

        Assert.AreEqual(3, capacity);
        Assert.AreEqual(0, TransportBoardingOrderPlanningSystemHelper.ResolvePlannedOrderCapacity(-2, 3));
        Assert.AreEqual(0, plannedOrders.Count);
        Assert.AreEqual(3, plannedOrders.Capacity);
    }

    [Test]
    public void OrderPlanningHelper_BoardAllCandidatesSortByDistanceThenEntityIndex()
    {
        List<BoardAllTransportCandidate> candidates = new()
        {
            new BoardAllTransportCandidate(new Entity { Index = 12, Version = 1 }, distance: 4),
            new BoardAllTransportCandidate(new Entity { Index = 9, Version = 1 }, distance: 4),
            new BoardAllTransportCandidate(new Entity { Index = 30, Version = 1 }, distance: 2)
        };

        candidates.Sort();

        Assert.AreEqual(30, candidates[0].Entity.Index);
        Assert.AreEqual(9, candidates[1].Entity.Index);
        Assert.AreEqual(12, candidates[2].Entity.Index);
    }

    [Test]
    public void OrderPlanningHelper_TransportSlotAvailabilityReportsCountsByPassengerKind()
    {
        TransportSlotAvailability availability = new(
            occupiedSoldierSeats: 3,
            soldierCapacity: 10,
            occupiedVehicleSlots: 1,
            vehicleCapacity: 2);

        availability.GetPassengerKindCounts(
            UnitTransportPassengerKind.Soldier,
            out int occupiedSoldierSeats,
            out int soldierCapacity,
            out int availableSoldierSeats);
        availability.GetPassengerKindCounts(
            UnitTransportPassengerKind.Vehicle,
            out int occupiedVehicleSlots,
            out int vehicleCapacity,
            out int availableVehicleSlots);

        Assert.IsTrue(availability.HasAnyAvailableSlot);
        Assert.AreEqual(8, availability.TotalAvailableSlots);
        Assert.AreEqual(3, occupiedSoldierSeats);
        Assert.AreEqual(10, soldierCapacity);
        Assert.AreEqual(7, availableSoldierSeats);
        Assert.AreEqual(1, occupiedVehicleSlots);
        Assert.AreEqual(2, vehicleCapacity);
        Assert.AreEqual(1, availableVehicleSlots);
    }

    [Test]
    public void OrderPlanningHelper_TransportSlotAvailabilityKeepsMinimumTotalCapacity()
    {
        TransportSlotAvailability availability = new(
            occupiedSoldierSeats: 10,
            soldierCapacity: 10,
            occupiedVehicleSlots: 2,
            vehicleCapacity: 2);

        Assert.IsFalse(availability.HasAnyAvailableSlot);
        Assert.AreEqual(1, availability.TotalAvailableSlots);
    }

    [Test]
    public void OrderPlanningHelper_CreatesPendingBoardingOrderWithDirectFlag()
    {
        Entity passenger = new() { Index = 31, Version = 1 };

        PendingTransportBoardingOrder directOrder =
            TransportBoardingOrderPlanningSystemHelper.CreatePendingBoardingOrder(
                passenger,
                passengerCell: new int2(2, 3),
                goal: new int2(2, 3),
                passengerKind: UnitTransportPassengerKind.Soldier,
                cargoWeight: 0);
        PendingTransportBoardingOrder movingOrder =
            TransportBoardingOrderPlanningSystemHelper.CreatePendingBoardingOrder(
                passenger,
                passengerCell: new int2(2, 3),
                goal: new int2(4, 3),
                passengerKind: UnitTransportPassengerKind.Vehicle,
                cargoWeight: 2);

        Assert.AreEqual(passenger, directOrder.Passenger);
        Assert.AreEqual(new int2(2, 3), directOrder.PassengerCell);
        Assert.AreEqual(new int2(2, 3), directOrder.Goal);
        Assert.AreEqual(UnitTransportPassengerKind.Soldier, directOrder.PassengerKind);
        Assert.AreEqual(0, directOrder.CargoWeight);
        Assert.IsTrue(directOrder.DirectBoarding);
        Assert.AreEqual(new int2(4, 3), movingOrder.Goal);
        Assert.AreEqual(UnitTransportPassengerKind.Vehicle, movingOrder.PassengerKind);
        Assert.AreEqual(2, movingOrder.CargoWeight);
        Assert.IsFalse(movingOrder.DirectBoarding);
    }

    [Test]
    public void OrderPlanningHelper_ReportsAvailabilityByPassengerKind()
    {
        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.HasPlannedBoardingSlot(
            UnitTransportPassengerKind.Soldier,
            availableSoldierSeats: 2,
            availableVehicleSlots: 0,
            plannedSoldierSeats: 1,
            plannedVehicleSlots: 0));
        Assert.IsFalse(TransportBoardingOrderPlanningSystemHelper.HasPlannedBoardingSlot(
            UnitTransportPassengerKind.Soldier,
            availableSoldierSeats: 1,
            availableVehicleSlots: 1,
            plannedSoldierSeats: 1,
            plannedVehicleSlots: 0));
        Assert.IsTrue(TransportBoardingOrderPlanningSystemHelper.HasPlannedBoardingSlot(
            UnitTransportPassengerKind.Vehicle,
            availableSoldierSeats: 0,
            availableVehicleSlots: 2,
            plannedSoldierSeats: 0,
            plannedVehicleSlots: 1));
        Assert.IsFalse(TransportBoardingOrderPlanningSystemHelper.HasPlannedBoardingSlot(
            UnitTransportPassengerKind.Vehicle,
            availableSoldierSeats: 1,
            availableVehicleSlots: 1,
            plannedSoldierSeats: 0,
            plannedVehicleSlots: 1));
    }

    [Test]
    public void OrderPlanningHelper_ReportsPlannedSlotRejectionByPassengerKind()
    {
        Assert.AreEqual(
            TransportBoardingPlannedSlotRejectionKind.None,
            TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSlotRejection(
                UnitTransportPassengerKind.Soldier,
                availableSoldierSeats: 2,
                availableVehicleSlots: 0,
                plannedSoldierSeats: 1,
                plannedVehicleSlots: 0));
        Assert.AreEqual(
            TransportBoardingPlannedSlotRejectionKind.NoSoldierSeats,
            TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSlotRejection(
                UnitTransportPassengerKind.Soldier,
                availableSoldierSeats: 1,
                availableVehicleSlots: 2,
                plannedSoldierSeats: 1,
                plannedVehicleSlots: 0));
        Assert.AreEqual(
            TransportBoardingPlannedSlotRejectionKind.NoVehicleSlots,
            TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSlotRejection(
                UnitTransportPassengerKind.Vehicle,
                availableSoldierSeats: 2,
                availableVehicleSlots: 1,
                plannedSoldierSeats: 0,
                plannedVehicleSlots: 1));
    }

    [Test]
    public void OrderPlanningHelper_ResolvesSelectedTransportCandidateDecisions()
    {
        Entity transport = new() { Index = 10, Version = 1 };
        Entity soldier = new() { Index = 20, Version = 1 };
        Entity vehicle = new() { Index = 30, Version = 1 };
        TransportSlotAvailability availability = new(
            occupiedSoldierSeats: 0,
            soldierCapacity: 1,
            occupiedVehicleSlots: 0,
            vehicleCapacity: 1);

        Assert.AreEqual(
            SelectedTransportBoardingCandidateDecisionKind.SkipTransport,
            TransportBoardingOrderPlanningSystemHelper.ResolveSelectedTransportCandidateDecision(
                transport,
                transport,
                hasPassengerKind: false,
                passengerKind: UnitTransportPassengerKind.Soldier,
                availability,
                default));
        Assert.AreEqual(
            SelectedTransportBoardingCandidateDecisionKind.SkipNotBoardingCandidate,
            TransportBoardingOrderPlanningSystemHelper.ResolveSelectedTransportCandidateDecision(
                soldier,
                transport,
                hasPassengerKind: false,
                passengerKind: UnitTransportPassengerKind.Soldier,
                availability,
                default));
        Assert.AreEqual(
            SelectedTransportBoardingCandidateDecisionKind.Accept,
            TransportBoardingOrderPlanningSystemHelper.ResolveSelectedTransportCandidateDecision(
                soldier,
                transport,
                hasPassengerKind: true,
                passengerKind: UnitTransportPassengerKind.Soldier,
                availability,
                default));
        Assert.AreEqual(
            SelectedTransportBoardingCandidateDecisionKind.SkipNoSoldierSeats,
            TransportBoardingOrderPlanningSystemHelper.ResolveSelectedTransportCandidateDecision(
                soldier,
                transport,
                hasPassengerKind: true,
                passengerKind: UnitTransportPassengerKind.Soldier,
                availability,
                new TransportBoardingPlannedSlotCounts(soldierSeats: 1, vehicleSlots: 0)));
        Assert.AreEqual(
            SelectedTransportBoardingCandidateDecisionKind.SkipNoVehicleSlots,
            TransportBoardingOrderPlanningSystemHelper.ResolveSelectedTransportCandidateDecision(
                vehicle,
                transport,
                hasPassengerKind: true,
                passengerKind: UnitTransportPassengerKind.Vehicle,
                availability,
                new TransportBoardingPlannedSlotCounts(soldierSeats: 0, vehicleSlots: 1)));
    }

    [Test]
    public void OrderPlanningHelper_ResolvesBoardAllTransportCandidateDecisions()
    {
        TransportSlotAvailability availability = new(
            occupiedSoldierSeats: 0,
            soldierCapacity: 1,
            occupiedVehicleSlots: 0,
            vehicleCapacity: 1);

        Assert.AreEqual(
            BoardAllTransportBoardingCandidateDecisionKind.SkipNotBoardingCandidate,
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllTransportCandidateDecision(
                hasPassengerKind: false,
                passengerKind: UnitTransportPassengerKind.Soldier,
                availability,
                default));
        Assert.AreEqual(
            BoardAllTransportBoardingCandidateDecisionKind.Accept,
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllTransportCandidateDecision(
                hasPassengerKind: true,
                passengerKind: UnitTransportPassengerKind.Soldier,
                availability,
                default));
        Assert.AreEqual(
            BoardAllTransportBoardingCandidateDecisionKind.SkipNoSoldierSeats,
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllTransportCandidateDecision(
                hasPassengerKind: true,
                passengerKind: UnitTransportPassengerKind.Soldier,
                availability,
                new TransportBoardingPlannedSlotCounts(soldierSeats: 1, vehicleSlots: 0)));
        Assert.AreEqual(
            BoardAllTransportBoardingCandidateDecisionKind.SkipNoVehicleSlots,
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllTransportCandidateDecision(
                hasPassengerKind: true,
                passengerKind: UnitTransportPassengerKind.Vehicle,
                availability,
                new TransportBoardingPlannedSlotCounts(soldierSeats: 0, vehicleSlots: 1)));
    }

    [Test]
    public void OrderPlanningHelper_ReportsStructPlannedSlotRejectionAndAcceptedMessage()
    {
        TransportBoardingPlannedSlotCounts plannedSlots = new(soldierSeats: 1, vehicleSlots: 1);

        Assert.AreEqual(
            TransportBoardingPlannedSlotRejectionKind.NoSoldierSeats,
            TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSlotRejection(
                UnitTransportPassengerKind.Soldier,
                availableSoldierSeats: 1,
                availableVehicleSlots: 2,
                plannedSlots));
        Assert.AreEqual(
            TransportBoardingPlannedSlotRejectionKind.NoVehicleSlots,
            TransportBoardingOrderPlanningSystemHelper.ResolvePlannedSlotRejection(
                UnitTransportPassengerKind.Vehicle,
                availableSoldierSeats: 2,
                availableVehicleSlots: 1,
                plannedSlots));
        Assert.AreEqual(
            "Loading troops and cargo.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(
                cargoPlaneTransport: true,
                plannedSlots));
    }

    [Test]
    public void OrderPlanningHelper_ResolvesBatchAcceptedMessages()
    {
        Assert.AreEqual(
            "Boarding transport.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(
                cargoPlaneTransport: false,
                plannedSoldierSeats: 1,
                plannedVehicleSlots: 1));
        Assert.AreEqual(
            "Loading troops and cargo.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(
                cargoPlaneTransport: true,
                plannedSoldierSeats: 1,
                plannedVehicleSlots: 1));
        Assert.AreEqual(
            "Loading cargo.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(
                cargoPlaneTransport: true,
                plannedSoldierSeats: 0,
                plannedVehicleSlots: 1));
        Assert.AreEqual(
            "Boarding transport plane.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(
                cargoPlaneTransport: true,
                plannedSoldierSeats: 1,
                plannedVehicleSlots: 0));
    }

    [Test]
    public void OrderPlanningHelper_ResolvesSinglePassengerAcceptedMessages()
    {
        Assert.AreEqual(
            "Loading transport.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(
                cargoPlaneTransport: false,
                passengerKind: UnitTransportPassengerKind.Soldier));
        Assert.AreEqual(
            "Loading cargo.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(
                cargoPlaneTransport: true,
                passengerKind: UnitTransportPassengerKind.Vehicle));
        Assert.AreEqual(
            "Boarding transport plane.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardingAcceptedMessage(
                cargoPlaneTransport: true,
                passengerKind: UnitTransportPassengerKind.Soldier));
    }

    [Test]
    public void OrderPlanningHelper_ResolvesBoardAllAcceptedMessages()
    {
        Assert.AreEqual(
            "Boarding 1 unit.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllAcceptedMessage(1));
        Assert.AreEqual(
            "Boarding 2 units.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllAcceptedMessage(2));
        Assert.AreEqual(
            "Boarding 0 units.",
            TransportBoardingOrderPlanningSystemHelper.ResolveBoardAllAcceptedMessage(0));
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
