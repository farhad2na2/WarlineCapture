using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public sealed class BuildingCombatSystem
{
    public enum RuntimeCombatState : byte
    {
        Active = 0,
        MissingCombatEntity = 1,
        DeadCombatEntity = 2
    }

    public interface IRuntimeBuilding
    {
        int Id { get; }
        bool IsDestroyed { get; set; }
        float DestroyedCleanupAt { get; set; }
        Entity CombatEntity { get; set; }
        Entity BlockerEntity { get; set; }
    }

    public bool TryMarkDestroyed(IRuntimeBuilding building, float now, float destroyedLifetimeSeconds)
    {
        if (building == null || building.IsDestroyed)
            return false;

        building.IsDestroyed = true;
        building.DestroyedCleanupAt = now + Mathf.Max(0f, destroyedLifetimeSeconds);
        return true;
    }

    public List<int> CollectDestroyedCleanupIds<TBuilding>(IReadOnlyDictionary<int, TBuilding> buildings, float now)
        where TBuilding : class, IRuntimeBuilding
    {
        if (buildings == null || buildings.Count == 0)
            return null;

        List<int> cleanupIds = null;
        foreach (var entry in buildings)
        {
            TBuilding building = entry.Value;
            if (building == null || !building.IsDestroyed || now < building.DestroyedCleanupAt)
                continue;

            cleanupIds ??= new List<int>();
            cleanupIds.Add(entry.Key);
        }

        return cleanupIds;
    }

    public RuntimeCombatState ResolveRuntimeCombatState(IRuntimeBuilding building, EntityManager entityManager)
    {
        if (building == null || building.IsDestroyed || building.CombatEntity == Entity.Null)
            return RuntimeCombatState.Active;

        if (!entityManager.Exists(building.CombatEntity))
            return RuntimeCombatState.MissingCombatEntity;

        if (!entityManager.HasComponent<UnitHealth>(building.CombatEntity))
            return RuntimeCombatState.Active;

        UnitHealth health = entityManager.GetComponentData<UnitHealth>(building.CombatEntity);
        return health.Current <= 0 ? RuntimeCombatState.DeadCombatEntity : RuntimeCombatState.Active;
    }

    public void DestroyBlockerEntity(IRuntimeBuilding building, EntityManager entityManager)
    {
        if (building == null)
            return;

        Entity blockerEntity = building.BlockerEntity;
        if (blockerEntity != Entity.Null && entityManager.Exists(blockerEntity))
            entityManager.DestroyEntity(blockerEntity);

        building.BlockerEntity = Entity.Null;
    }
}
