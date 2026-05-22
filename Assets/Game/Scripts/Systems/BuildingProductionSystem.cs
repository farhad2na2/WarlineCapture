using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingProductionSystem
{
    public enum ProductionTransportMode : byte
    {
        Helicopter = 0,
        Plane = 1,
        AirSelf = 2
    }

    public interface IPendingProduction
    {
        int ProductionIndex { get; set; }
        GameObject Prefab { get; set; }
        float StartedAt { get; set; }
        float ReadyAt { get; set; }
        int ReservedProductionSlotIndex { get; set; }
        GameObject TransportPrefab { get; set; }
        float TransportArrivalSeconds { get; set; }
        float TransportHoldForNextReadySeconds { get; set; }
        int TransportMaxConcurrent { get; set; }
        ProductionTransportMode TransportMode { get; set; }
        bool TransportRequiresAirportRunway { get; set; }
    }

    public readonly struct PendingProductionProgress
    {
        public readonly float DurationSeconds;
        public readonly float RemainingSeconds;
        public readonly float Progress01;

        public PendingProductionProgress(float durationSeconds, float remainingSeconds, float progress01)
        {
            DurationSeconds = durationSeconds;
            RemainingSeconds = remainingSeconds;
            Progress01 = progress01;
        }
    }

    public void InitializePendingProduction(
        IPendingProduction pending,
        int productionIndex,
        GameObject spawnUnitPrefab,
        float now,
        float productionDurationSeconds,
        int reservedProductionSlotIndex,
        GameObject transportPrefab,
        float transportArrivalSeconds,
        float transportHoldForNextReadySeconds,
        int transportMaxConcurrent,
        ProductionTransportMode transportMode,
        bool transportRequiresAirportRunway)
    {
        if (pending == null)
            return;

        pending.ProductionIndex = productionIndex;
        pending.Prefab = spawnUnitPrefab;
        pending.StartedAt = now;
        pending.ReadyAt = now + Mathf.Max(0.01f, productionDurationSeconds);
        pending.ReservedProductionSlotIndex = reservedProductionSlotIndex;
        pending.TransportPrefab = transportPrefab;
        pending.TransportArrivalSeconds = transportArrivalSeconds;
        pending.TransportHoldForNextReadySeconds = transportHoldForNextReadySeconds;
        pending.TransportMaxConcurrent = transportMaxConcurrent;
        pending.TransportMode = transportMode;
        pending.TransportRequiresAirportRunway = transportRequiresAirportRunway;
    }

    public PendingProductionProgress GetProgress(IPendingProduction pending, float now, bool capTransportProgress)
    {
        if (pending == null)
            return new PendingProductionProgress(0f, 0f, 0f);

        float duration = Mathf.Max(0.01f, pending.ReadyAt - pending.StartedAt);
        float remaining = Mathf.Max(0f, pending.ReadyAt - now);
        float progress = Mathf.Clamp01((now - pending.StartedAt) / duration);
        if (capTransportProgress && pending.TransportPrefab != null)
            progress = Mathf.Min(progress, 0.97f);

        return new PendingProductionProgress(duration, remaining, progress);
    }

    public bool IsReady(IPendingProduction pending, float now)
    {
        return pending != null && now >= pending.ReadyAt;
    }

    public bool IsReadyWithin(IPendingProduction pending, float now, float maxSeconds)
    {
        if (pending == null)
            return false;

        float remaining = pending.ReadyAt - now;
        return remaining > 0f && remaining <= Mathf.Max(0f, maxSeconds);
    }

    public float GetTransportLaunchAt(IPendingProduction pending)
    {
        if (pending == null)
            return float.PositiveInfinity;

        return pending.ReadyAt - Mathf.Max(0.5f, pending.TransportArrivalSeconds);
    }

    public bool ShouldLaunchTransport(IPendingProduction pending, float now)
    {
        return pending != null &&
               pending.TransportPrefab != null &&
               now >= GetTransportLaunchAt(pending);
    }

    public void DelayPendingProduction(IPendingProduction pending, float deltaTime)
    {
        if (pending == null)
            return;

        float delay = Mathf.Max(0f, deltaTime);
        pending.StartedAt += delay;
        pending.ReadyAt += delay;
    }

    public TPending FindNextReadyTransportPending<TPending>(
        IReadOnlyList<TPending> pendingProductions,
        GameObject transportPrefab,
        float now)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || transportPrefab == null)
            return null;

        for (int i = 0; i < pendingProductions.Count; i++)
        {
            TPending pending = pendingProductions[i];
            if (pending == null || pending.TransportPrefab != transportPrefab)
                continue;
            if (IsReady(pending, now))
                return pending;
        }

        return null;
    }

    public TPending FindNextSoonTransportPending<TPending>(
        IReadOnlyList<TPending> pendingProductions,
        GameObject transportPrefab,
        float now,
        float maxSeconds)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || transportPrefab == null)
            return null;

        for (int i = 0; i < pendingProductions.Count; i++)
        {
            TPending pending = pendingProductions[i];
            if (pending == null || pending.TransportPrefab != transportPrefab)
                continue;
            if (IsReadyWithin(pending, now, maxSeconds))
                return pending;
        }

        return null;
    }

    public bool RemovePendingProduction<TPending>(IList<TPending> pendingProductions, TPending pending)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || pending == null)
            return false;

        int index = pendingProductions.IndexOf(pending);
        if (index < 0)
            return false;

        pendingProductions.RemoveAt(index);
        return true;
    }

    public bool RemovePendingAt<TPending>(IList<TPending> pendingProductions, int index)
        where TPending : class, IPendingProduction
    {
        if (pendingProductions == null || index < 0 || index >= pendingProductions.Count)
            return false;

        pendingProductions.RemoveAt(index);
        return true;
    }

    public void PruneProducedUnits(
        List<Entity> producedUnits,
        Entity[] producedUnitSlots,
        Dictionary<Entity, GameObject> producedUnitPrefabs,
        EntityManager entityManager)
    {
        if (producedUnits != null)
        {
            for (int i = producedUnits.Count - 1; i >= 0; i--)
            {
                Entity unit = producedUnits[i];
                if (IsProducedUnitAlive(unit, entityManager))
                    continue;

                producedUnitPrefabs?.Remove(unit);
                producedUnits.RemoveAt(i);
            }
        }

        if (producedUnitSlots == null)
            return;

        for (int i = 0; i < producedUnitSlots.Length; i++)
        {
            Entity unit = producedUnitSlots[i];
            if (unit != Entity.Null && !IsProducedUnitAlive(unit, entityManager))
                producedUnitSlots[i] = Entity.Null;
        }
    }

    public bool TryReserveProductionSlot(
        IReadOnlyList<IPendingProduction> pendingProductions,
        Entity[] producedUnitSlots,
        int productionSpawnSlotCount,
        EntityManager entityManager,
        out int reservedProductionSlotIndex)
    {
        reservedProductionSlotIndex = -1;
        if (producedUnitSlots == null || productionSpawnSlotCount <= 0)
            return false;

        int count = Mathf.Min(productionSpawnSlotCount, producedUnitSlots.Length);
        for (int i = 0; i < count; i++)
        {
            if (IsSlotReservedByPending(pendingProductions, i))
                continue;

            Entity occupant = producedUnitSlots[i];
            if (IsProducedUnitAlive(occupant, entityManager))
                continue;

            if (occupant != Entity.Null)
                producedUnitSlots[i] = Entity.Null;

            reservedProductionSlotIndex = i;
            return true;
        }

        return false;
    }

    private static bool IsSlotReservedByPending(IReadOnlyList<IPendingProduction> pendingProductions, int slotIndex)
    {
        if (pendingProductions == null)
            return false;

        for (int i = 0; i < pendingProductions.Count; i++)
        {
            IPendingProduction pending = pendingProductions[i];
            if (pending != null && pending.ReservedProductionSlotIndex == slotIndex)
                return true;
        }

        return false;
    }

    private static bool IsProducedUnitAlive(Entity unit, EntityManager entityManager)
    {
        if (unit == Entity.Null || !entityManager.Exists(unit))
            return false;

        return !entityManager.HasComponent<UnitHealth>(unit) ||
               entityManager.GetComponentData<UnitHealth>(unit).Current > 0;
    }
}
