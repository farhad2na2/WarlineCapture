using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class FocusedUnitUiReadModelSystem
{
    private World _queryWorld;
    private EntityQuery _readModelQuery;
    private readonly List<SelectionUiQuerySystem.TransportPassengerUiInfo> _passengerScratch = new();

    public void Publish(
        EntityManager em,
        SelectionStateSystem selectionStateSystem,
        SelectionUiQuerySystem selectionUiQuerySystem,
        UnitTransportCapacitySystem transportCapacitySystem,
        float timeSeconds)
    {
        Entity readModelEntity = EnsureReadModelEntity(em);
        DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengerBuffer =
            em.GetBuffer<FocusedUnitPassengerUiReadModelElement>(readModelEntity);
        passengerBuffer.Clear();

        Entity focusedUnit = selectionStateSystem.FocusedUnit;
        if (!selectionUiQuerySystem.HasFocusedUnit(em, focusedUnit))
        {
            em.SetComponentData(readModelEntity, EmptyModel());
            return;
        }

        FocusedUnitUiReadModelComponent model = new()
        {
            FocusedUnit = focusedUnit,
            HasFocusedUnit = 1,
            OwnedByPlayer = selectionUiQuerySystem.IsOwnedByPlayer(em, focusedUnit) ? (byte)1 : (byte)0,
            IsVehicle = selectionUiQuerySystem.IsVehicleUnit(em, focusedUnit) ? (byte)1 : (byte)0,
            CanAttack = selectionUiQuerySystem.CanAttack(em, focusedUnit) ? (byte)1 : (byte)0,
            Label = ToFixed64(selectionUiQuerySystem.ResolveFocusedUnitName(em, focusedUnit)),
            Description = ToFixed128(selectionUiQuerySystem.ResolveFocusedUnitDescription(em, focusedUnit)),
            HealthText = ToFixed32(selectionUiQuerySystem.ResolveFocusedUnitHealthText(em, focusedUnit)),
            Status = (int)selectionUiQuerySystem.GetFocusedUnitUiStatus(em, focusedUnit)
        };

        if (selectionUiQuerySystem.TryGetFocusedUnitHealth(em, focusedUnit, out int healthCurrent, out int healthMax))
        {
            model.HasHealth = 1;
            model.HealthCurrent = healthCurrent;
            model.HealthMax = healthMax;
        }

        if (selectionUiQuerySystem.TryGetFocusedUnitCapacityInfo(
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

        model.PassengerCount = selectionUiQuerySystem.GetTransportPassengerCount(em, focusedUnit, transportCapacitySystem);
        _passengerScratch.Clear();
        selectionUiQuerySystem.GetTransportPassengers(em, focusedUnit, transportCapacitySystem, _passengerScratch);
        for (int i = 0; i < _passengerScratch.Count; i++)
        {
            SelectionUiQuerySystem.TransportPassengerUiInfo passenger = _passengerScratch[i];
            passengerBuffer.Add(new FocusedUnitPassengerUiReadModelElement
            {
                Passenger = passenger.Entity,
                DisplayName = ToFixed64(passenger.DisplayName),
                HealthCurrent = passenger.HealthCurrent,
                HealthMax = passenger.HealthMax
            });
        }

        if (selectionUiQuerySystem.TryGetFocusedUnitWorldPosition(em, focusedUnit, out Vector3 worldPosition))
        {
            model.HasWorldPosition = 1;
            model.WorldPosition = worldPosition;
        }

        if (selectionUiQuerySystem.TryGetFocusedUnitPortraitPose(em, focusedUnit, out Vector3 portraitWorldPosition, out Vector3 portraitForward))
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
