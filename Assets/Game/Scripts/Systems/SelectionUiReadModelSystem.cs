using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public sealed class SelectionUiReadModelSystem
{
    public readonly struct TransportPassengerUiInfo
    {
        public readonly Entity Entity;
        public readonly string DisplayName;
        public readonly int HealthCurrent;
        public readonly int HealthMax;

        public TransportPassengerUiInfo(Entity entity, string displayName, int healthCurrent, int healthMax)
        {
            Entity = entity;
            DisplayName = displayName;
            HealthCurrent = healthCurrent;
            HealthMax = healthMax;
        }
    }

    public enum FocusedUnitUiStatus
    {
        Idle = 0,
        Moving = 1,
        Engaged = 2,
        ReturningToBase = 3,
        MissileLaunched = 4
    }

    private readonly FocusedUnitUiReadModelSystem _focusedUnitUiReadModelSystem = new();
    private readonly SelectionUiQuerySystem _selectionUiQuerySystem = new();
    private readonly VisibleUnitSelectionSystem _visibleUnitSelectionSystem = new();

    private World _queryWorld;
    private EntityQuery _selectedTagQuery;

    public bool HasFocusedUnit =>
        TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
        model.HasFocusedUnit != 0;

    public bool HasAnySelectedUnits
    {
        get
        {
            if (!TryGetDefaultEntityManager(out EntityManager em))
                return false;

            EnsureEntityQueries(em);
            return _selectionUiQuerySystem.HasAnySelectedUnits(_selectedTagQuery);
        }
    }

    public string FocusedUnitLabel =>
        TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
            ? model.Label.ToString()
            : "Unit";

    public bool CanDestroyFocusedUnit => FocusedUnitOwnedByPlayer;

    public bool FocusedUnitOwnedByPlayer =>
        TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
        model.OwnedByPlayer != 0;

    public bool FocusedUnitIsVehicle =>
        TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
        model.IsVehicle != 0;

    public bool FocusedUnitCanAttack =>
        TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) &&
        model.CanAttack != 0;

    public int FocusedTransportPassengerCount =>
        TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
            ? model.PassengerCount
            : 0;

    public bool CanDisembarkFocusedTransport => FocusedTransportPassengerCount > 0;

    public bool TryGetFocusedUnitHealth(out int current, out int max)
    {
        current = 0;
        max = 0;

        if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasHealth == 0)
            return false;

        current = model.HealthCurrent;
        max = model.HealthMax;
        return true;
    }

    public bool TryGetFocusedUnitEntityForUi(out Entity entity)
    {
        entity = Entity.Null;
        if (!TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model) || model.HasFocusedUnit == 0)
            return false;

        entity = model.FocusedUnit;
        return true;
    }

    public FocusedUnitUiStatus GetFocusedUnitUiStatus()
    {
        return TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
            ? ToFocusedUnitUiStatus(model.Status)
            : FocusedUnitUiStatus.Idle;
    }

    public void GetFocusedTransportPassengers(List<TransportPassengerUiInfo> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (!TryReadFocusedUnitUiModel(
                out _,
                out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers))
        {
            return;
        }

        for (int i = 0; i < passengers.Length; i++)
        {
            FocusedUnitPassengerUiReadModelElement passenger = passengers[i];
            results.Add(new TransportPassengerUiInfo(
                passenger.Passenger,
                passenger.DisplayName.ToString(),
                passenger.HealthCurrent,
                passenger.HealthMax));
        }
    }

    public void GetSelectedUnitEntities(List<Entity> entities)
    {
        if (entities == null)
            return;

        if (!TryGetDefaultEntityManager(out EntityManager em))
        {
            entities.Clear();
            return;
        }

        EnsureEntityQueries(em);
        using NativeArray<Entity> selectedEntities = _selectedTagQuery.ToEntityArray(Allocator.Temp);
        _selectionUiQuerySystem.GetSelectedUnitEntities(em, selectedEntities, entities);
    }

    public bool HasVisiblePlayerUnits(Camera worldCamera)
    {
        return HasVisiblePlayerUnits(worldCamera, VisibleUnitSelectionSystem.Filter.All);
    }

    public bool HasVisiblePlayerSoldiers(Camera worldCamera)
    {
        return HasVisiblePlayerUnits(worldCamera, VisibleUnitSelectionSystem.Filter.Soldiers);
    }

    public bool HasVisiblePlayerVehicles(Camera worldCamera)
    {
        return HasVisiblePlayerUnits(worldCamera, VisibleUnitSelectionSystem.Filter.Vehicles);
    }

    private bool HasVisiblePlayerUnits(Camera worldCamera, VisibleUnitSelectionSystem.Filter filter)
    {
        if (worldCamera == null || !TryGetDefaultEntityManager(out EntityManager em))
            return false;

        EnsureEntityQueries(em);
        Rect screenRect = new(0f, 0f, Screen.width, Screen.height);
        return _visibleUnitSelectionSystem.HasVisiblePlayerUnits(
            em,
            worldCamera,
            _selectionUiQuerySystem,
            screenRect,
            filter);
    }

    private bool TryReadFocusedUnitUiModel(out FocusedUnitUiReadModelComponent model)
    {
        return TryReadFocusedUnitUiModel(out model, out _);
    }

    private bool TryReadFocusedUnitUiModel(
        out FocusedUnitUiReadModelComponent model,
        out DynamicBuffer<FocusedUnitPassengerUiReadModelElement> passengers)
    {
        model = default;
        passengers = default;
        if (!TryGetDefaultEntityManager(out EntityManager em))
            return false;

        _focusedUnitUiReadModelSystem.TryRead(em, out model, out passengers);
        return true;
    }

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _selectedTagQuery = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        _visibleUnitSelectionSystem.EnsureEntityQueries(em);
    }

    private static bool TryGetDefaultEntityManager(out EntityManager em)
    {
        em = default;
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        em = world.EntityManager;
        return true;
    }

    private static FocusedUnitUiStatus ToFocusedUnitUiStatus(int status)
    {
        return (SelectionUiQuerySystem.FocusedUnitUiStatus)status switch
        {
            SelectionUiQuerySystem.FocusedUnitUiStatus.Moving => FocusedUnitUiStatus.Moving,
            SelectionUiQuerySystem.FocusedUnitUiStatus.Engaged => FocusedUnitUiStatus.Engaged,
            SelectionUiQuerySystem.FocusedUnitUiStatus.ReturningToBase => FocusedUnitUiStatus.ReturningToBase,
            SelectionUiQuerySystem.FocusedUnitUiStatus.MissileLaunched => FocusedUnitUiStatus.MissileLaunched,
            _ => FocusedUnitUiStatus.Idle
        };
    }
}
