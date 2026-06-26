using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class FocusedUnitUiReadModelUiSystemHelper
{
    private World _queryWorld;
    private EntityQuery _readModelQuery;
    private readonly List<SelectionUiReadModelLookup.TransportPassengerUiInfo> _passengerScratch = new();

    public void Publish(
        EntityManager em,
        SelectionStateSystem selectionStateSystem,
        SelectionUiReadModelLookup selectionUiReadModelLookup,
        UnitTransportCapacitySystem transportCapacitySystem,
        float timeSeconds)
    {
        Entity readModelEntity = EnsureReadModelEntity(em);
        DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengerBuffer =
            em.GetBuffer<FocusedUnitPassengerUiReadModelElement>(readModelEntity);
        passengerBuffer.Clear();

        Entity focusedUnit = selectionStateSystem.FocusedUnit;
        if (!selectionUiReadModelLookup.HasFocusedUnit(em, focusedUnit))
        {
            em.SetComponentData(readModelEntity, EmptyModel());
            return;
        }

        FocusedUnitUiReadModelComponent model = new()
        {
            FocusedUnit = focusedUnit,
            HasFocusedUnit = 1,
            OwnedByPlayer = selectionUiReadModelLookup.IsOwnedByPlayer(em, focusedUnit) ? (byte)1 : (byte)0,
            IsVehicle = selectionUiReadModelLookup.IsVehicleUnit(em, focusedUnit) ? (byte)1 : (byte)0,
            CanAttack = selectionUiReadModelLookup.CanAttack(em, focusedUnit) ? (byte)1 : (byte)0,
            Label = ToFixed64(selectionUiReadModelLookup.ResolveFocusedUnitName(em, focusedUnit)),
            Description = ToFixed128(selectionUiReadModelLookup.ResolveFocusedUnitDescription(em, focusedUnit)),
            HealthText = ToFixed32(selectionUiReadModelLookup.ResolveFocusedUnitHealthText(em, focusedUnit)),
            Status = (int)selectionUiReadModelLookup.GetFocusedUnitUiStatus(em, focusedUnit)
        };

        model.CanHold = selectionUiReadModelLookup.CanHoldPosition(em, focusedUnit, out TacticalCommandReasonCode holdReason) ? (byte)1 : (byte)0;
        model.HoldDisabledReason = (int)holdReason;
        model.CanStop = selectionUiReadModelLookup.CanStop(em, focusedUnit, out TacticalCommandReasonCode stopReason) ? (byte)1 : (byte)0;
        model.StopDisabledReason = (int)stopReason;
        model.CanScan = selectionUiReadModelLookup.CanScan(em, focusedUnit, out TacticalCommandReasonCode scanReason) ? (byte)1 : (byte)0;
        model.ScanDisabledReason = (int)scanReason;

        if (selectionUiReadModelLookup.TryGetFocusedUnitHealth(em, focusedUnit, out int healthCurrent, out int healthMax))
        {
            model.HasHealth = 1;
            model.HealthCurrent = healthCurrent;
            model.HealthMax = healthMax;
        }

        if (selectionUiReadModelLookup.TryGetFocusedUnitCapacityInfo(
                em,
                focusedUnit,
                timeSeconds,
                out int capacityCurrent,
                out int capacityMax,
                out float capacityProgress01))
        {
            model.HasCapacity = 1;
            model.CapacityCurrent = capacityCurrent;
            model.CapacityMax = capacityMax;
            model.CapacityProgress01 = capacityProgress01;
        }

        if (selectionUiReadModelLookup.TryGetTransportPassengerBreakdown(
                em,
                focusedUnit,
                transportCapacitySystem,
                out int soldierPassengerCount,
                out int soldierPassengerCapacity,
                out int vehiclePassengerCount,
                out int vehiclePassengerCapacity))
        {
            model.TransportSoldierPassengerCount = soldierPassengerCount;
            model.TransportSoldierPassengerCapacity = soldierPassengerCapacity;
            model.TransportVehiclePassengerCount = vehiclePassengerCount;
            model.TransportVehiclePassengerCapacity = vehiclePassengerCapacity;
            model.PassengerCount = soldierPassengerCount + vehiclePassengerCount;
            model.TransportPassengerCapacity = soldierPassengerCapacity + vehiclePassengerCapacity;
        }
        _passengerScratch.Clear();
        selectionUiReadModelLookup.GetTransportPassengers(em, focusedUnit, transportCapacitySystem, _passengerScratch);
        for (int i = 0; i < _passengerScratch.Count; i++)
        {
            SelectionUiReadModelLookup.TransportPassengerUiInfo passenger = _passengerScratch[i];
            passengerBuffer.Add(new FocusedUnitPassengerUiReadModelElement
            {
                Passenger = passenger.Entity,
                DisplayName = ToFixed64(passenger.DisplayName),
                HealthCurrent = passenger.HealthCurrent,
                HealthMax = passenger.HealthMax
            });
        }

        if (selectionUiReadModelLookup.TryGetFocusedUnitWorldPosition(em, focusedUnit, out Vector3 worldPosition))
        {
            model.HasWorldPosition = 1;
            model.WorldPosition = worldPosition;
        }

        if (selectionUiReadModelLookup.TryGetFocusedUnitPortraitPose(em, focusedUnit, out Vector3 portraitWorldPosition, out Vector3 portraitForward))
        {
            model.HasPortraitPose = 1;
            model.PortraitWorldPosition = portraitWorldPosition;
            model.PortraitForward = portraitForward;
        }

        em.SetComponentData(readModelEntity, model);
    }

    public bool TryRead(
        EntityManager em,
        out FocusedUnitUiReadModelComponent model,
        out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers)
    {
        Entity readModelEntity = EnsureReadModelEntity(em);
        model = em.GetComponentData<FocusedUnitUiReadModelComponent>(readModelEntity);
        passengers = em.GetBuffer<FocusedUnitPassengerUiReadModelElement>(readModelEntity);
        return true;
    }

    private Entity EnsureReadModelEntity(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld != world || world == null || !world.IsCreated)
        {
            _queryWorld = world;
            _readModelQuery = em.CreateEntityQuery(
                ComponentType.ReadWrite<FocusedUnitUiReadModelComponent>(),
                ComponentType.ReadWrite<FocusedUnitPassengerUiReadModelElement>());
        }

        if (!_readModelQuery.IsEmptyIgnoreFilter)
            return _readModelQuery.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(FocusedUnitUiReadModelComponent));
        em.SetName(entity, "FocusedUnitUiReadModel");
        em.AddBuffer<FocusedUnitPassengerUiReadModelElement>(entity);
        em.SetComponentData(entity, EmptyModel());
        return entity;
    }

    private static FocusedUnitUiReadModelComponent EmptyModel()
    {
        return new FocusedUnitUiReadModelComponent
        {
            FocusedUnit = Entity.Null,
            Label = ToFixed64("Unit"),
            Description = ToFixed128("Select a unit to inspect it."),
            HealthText = ToFixed32("Health: -"),
            PortraitForward = new float3(0f, 0f, 1f)
        };
    }

    private static FixedString32Bytes ToFixed32(string value)
    {
        FixedString32Bytes result = default;
        result.Append(Trim(value, 29));
        return result;
    }

    private static FixedString64Bytes ToFixed64(string value)
    {
        FixedString64Bytes result = default;
        result.Append(Trim(value, 61));
        return result;
    }

    private static FixedString128Bytes ToFixed128(string value)
    {
        FixedString128Bytes result = default;
        result.Append(Trim(value, 125));
        return result;
    }

    private static string Trim(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}
