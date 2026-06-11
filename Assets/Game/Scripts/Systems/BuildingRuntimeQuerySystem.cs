using System.Collections.Generic;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class BuildingRuntimeQuerySystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate string StringNormalizer(string value);
    public delegate bool BuildingPredicate(RuntimeBuildingEntity building);
    public delegate bool BuildingIdPredicate(RuntimeBuildingEntity building, string normalizedId);
    public delegate bool UnitPrefabPredicate(GameObject prefab, string normalizedId);
    public delegate bool TryResolveBuildingWorldPositionDelegate(RuntimeBuildingEntity building, out Vector3 worldPosition);
    public delegate bool TryGetBuildingApproachCellDelegate(RuntimeBuildingEntity building, int2 unitFootprint, int2 referenceCell, out int2 goal);
    public delegate bool IsBuildingApproachCellDelegate(RuntimeBuildingEntity building, int2 currentCell, int2 unitFootprint);
    public delegate bool BuildingDefinitionPredicate(BuildingDefinition definition);
    public delegate bool TryResolveBaseBreachTargetDelegate(
        byte attackerFactionId,
        Entity finalTarget,
        int2 finalTargetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition,
        out string reason);

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly StringNormalizer NormalizeId;
        public readonly BuildingPredicate IsHouseBuilding;
        public readonly BuildingIdPredicate RuntimeBuildingMatchesId;
        public readonly UnitPrefabPredicate UnitPrefabMatchesId;
        public readonly TryResolveBuildingWorldPositionDelegate TryResolveBuildingFocusWorldPosition;
        public readonly TryGetBuildingApproachCellDelegate TryGetBuildingApproachCell;
        public readonly IsBuildingApproachCellDelegate IsBuildingApproachCell;
        public readonly BuildingDefinitionPredicate IsWallGateDefinition;
        public readonly TryResolveBaseBreachTargetDelegate TryResolveBaseBreachTarget;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
            TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingProductionSystem productionSystem,
            StringNormalizer normalizeId,
            BuildingPredicate isHouseBuilding,
            BuildingIdPredicate runtimeBuildingMatchesId,
            UnitPrefabPredicate unitPrefabMatchesId,
            TryResolveBuildingWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
            TryGetBuildingApproachCellDelegate tryGetBuildingApproachCell,
            IsBuildingApproachCellDelegate isBuildingApproachCell,
            BuildingDefinitionPredicate isWallGateDefinition,
            TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget)
        {
            RuntimeBuildings = runtimeBuildings;
            TryGetEntityManager = tryGetEntityManager;
            ProductionSystem = productionSystem;
            NormalizeId = normalizeId;
            IsHouseBuilding = isHouseBuilding;
            RuntimeBuildingMatchesId = runtimeBuildingMatchesId;
            UnitPrefabMatchesId = unitPrefabMatchesId;
            TryResolveBuildingFocusWorldPosition = tryResolveBuildingFocusWorldPosition;
            TryGetBuildingApproachCell = tryGetBuildingApproachCell;
            IsBuildingApproachCell = isBuildingApproachCell;
            IsWallGateDefinition = isWallGateDefinition;
            TryResolveBaseBreachTarget = tryResolveBaseBreachTarget;
        }
    }

    public int CountRuntimeBuildingsForFaction(Context context, byte factionId)
    {
        int count = 0;
        if (context.RuntimeBuildings == null)
            return count;

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                count += CountBuildingForFaction(pair.Value, factionId);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                count += CountBuildingForFaction(pair.Value, factionId);
        }

        return count;
    }

    public int CountRuntimeBuildingsForFaction(Context context, byte factionId, string buildingId)
    {
        string normalized = Normalize(context, buildingId);
        if (string.IsNullOrEmpty(normalized))
            return CountRuntimeBuildingsForFaction(context, factionId);

        int count = 0;
        if (context.RuntimeBuildings == null)
            return count;

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                count += CountBuildingForFactionAndId(context, pair.Value, factionId, normalized);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                count += CountBuildingForFactionAndId(context, pair.Value, factionId, normalized);
        }

        return count;
    }

    public int CountRuntimeProducedUnitsForFaction(Context context, byte factionId, string unitId)
    {
        string normalized = Normalize(context, unitId);
        int count = 0;
        if (context.RuntimeBuildings == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return 0;
        }

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                count += CountProducedUnitsForBuilding(context, pair.Value, factionId, normalized, em);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                count += CountProducedUnitsForBuilding(context, pair.Value, factionId, normalized, em);
        }

        return count;
    }

    public int CountPendingProductionsForFaction(Context context, byte factionId, string unitId)
    {
        string normalized = Normalize(context, unitId);
        int count = 0;
        if (context.RuntimeBuildings == null)
            return count;

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                count += CountPendingProductionsForBuilding(context, pair.Value, factionId, normalized);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                count += CountPendingProductionsForBuilding(context, pair.Value, factionId, normalized);
        }

        return count;
    }

    public void GetRuntimeHouseBuildingIds(Context context, List<int> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (context.RuntimeBuildings == null)
            return;

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                AddHouseBuildingId(context, results, pair.Key, pair.Value);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                AddHouseBuildingId(context, results, pair.Key, pair.Value);
        }
    }

    public void GetRuntimeBuildingIdsByRole(Context context, BuildingRole role, List<int> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (context.RuntimeBuildings == null)
            return;

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in runtimeBuildingMap)
                AddBuildingIdByRole(results, role, pair.Key, pair.Value);
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                AddBuildingIdByRole(results, role, pair.Key, pair.Value);
        }
    }

    public bool TryGetRuntimeBuildingFocusWorldPosition(Context context, int buildingId, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingEntity building))
            return false;

        return context.TryResolveBuildingFocusWorldPosition != null &&
               context.TryResolveBuildingFocusWorldPosition(building, out worldPosition);
    }

    public bool TryGetRuntimeBuildingDestroyedState(Context context, int buildingId, out bool isDestroyed)
    {
        isDestroyed = false;
        if (!TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingEntity building))
            return false;

        isDestroyed = building.IsDestroyed;
        return true;
    }

    public bool TryGetRuntimeBuildingRefugeeSettings(Context context, int buildingId, out int refugeeCapacity, out int upkeepPerCitizenPerDay)
    {
        refugeeCapacity = 0;
        upkeepPerCitizenPerDay = 0;
        if (!TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingEntity building) || building?.Definition == null)
            return false;

        refugeeCapacity = Mathf.Max(0, building.Definition.RefugeeCapacity);
        upkeepPerCitizenPerDay = Mathf.Max(0, building.Definition.RefugeeUpkeepPerCitizenPerDay);
        return true;
    }

    public bool IsRuntimeBuildingCityGenerated(Context context, int buildingId)
    {
        return TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingEntity building) &&
               building != null &&
               building.IsCityGenerated;
    }

    public bool IsRuntimeBuildingWall(Context context, int buildingId)
    {
        return TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingEntity building) &&
               building?.Definition != null &&
               building.Definition.IsWall;
    }

    public bool TryGetRuntimeBuildingOwnerFaction(Context context, int buildingId, out byte factionId)
    {
        factionId = 0;
        if (!TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingEntity building) || building == null || !building.HasOwnerFaction)
            return false;

        factionId = building.OwnerFactionId;
        return true;
    }

    public bool TryGetRuntimeBuildingCombatInfo(Context context, Entity combatEntity, out bool isGate, out bool isWall, out byte ownerFactionId)
    {
        isGate = false;
        isWall = false;
        ownerFactionId = 0;
        if (!TryFindRuntimeBuildingByCombatEntity(context, combatEntity, out RuntimeBuildingEntity building) || building?.Definition == null)
            return false;

        isGate = context.IsWallGateDefinition != null && context.IsWallGateDefinition(building.Definition);
        isWall = building.Definition.IsWall;
        ownerFactionId = building.HasOwnerFaction ? building.OwnerFactionId : (byte)0;
        return true;
    }

    public bool TryGetRuntimeBuildingApproachCell(Context context, int buildingId, int2 unitFootprint, int2 referenceCell, out int2 goal)
    {
        goal = default;
        return TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingEntity building) &&
               building != null &&
               !building.IsDestroyed &&
               context.TryGetBuildingApproachCell != null &&
               context.TryGetBuildingApproachCell(building, unitFootprint, referenceCell, out goal);
    }

    public bool IsRuntimeBuildingApproachCell(Context context, int buildingId, int2 currentCell, int2 unitFootprint)
    {
        return TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingEntity building) &&
               building != null &&
               !building.IsDestroyed &&
               context.IsBuildingApproachCell != null &&
               context.IsBuildingApproachCell(building, currentCell, unitFootprint);
    }

    public bool TryResolveBaseBreachTarget(
        Context context,
        byte attackerFactionId,
        Entity finalTarget,
        int2 finalTargetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition,
        out string reason)
    {
        breachTarget = Entity.Null;
        breachCell = default;
        breachPosition = default;
        reason = string.Empty;

        return context.TryResolveBaseBreachTarget != null &&
               context.TryResolveBaseBreachTarget(
                   attackerFactionId,
                   finalTarget,
                   finalTargetCell,
                   attackerCell,
                   out breachTarget,
                   out breachCell,
                   out breachPosition,
                   out reason);
    }

    private static bool TryGetRuntimeBuilding(Context context, int buildingId, out RuntimeBuildingEntity building)
    {
        building = null;
        return context.RuntimeBuildings != null &&
               context.RuntimeBuildings.TryGetValue(buildingId, out building);
    }

    private static bool TryFindRuntimeBuildingByCombatEntity(Context context, Entity combatEntity, out RuntimeBuildingEntity building)
    {
        building = null;
        if (combatEntity == Entity.Null || context.RuntimeBuildings == null)
            return false;

        if (context.RuntimeBuildings is Dictionary<int, RuntimeBuildingEntity> runtimeBuildingMap)
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in runtimeBuildingMap)
            {
                if (IsBuildingCombatEntity(entry.Value, combatEntity))
                {
                    building = entry.Value;
                    return true;
                }
            }
        }
        else
        {
            foreach (KeyValuePair<int, RuntimeBuildingEntity> entry in context.RuntimeBuildings)
            {
                if (IsBuildingCombatEntity(entry.Value, combatEntity))
                {
                    building = entry.Value;
                    return true;
                }
            }
        }

        return false;
    }

    private static int CountBuildingForFaction(RuntimeBuildingEntity building, byte factionId)
    {
        return building != null &&
               !building.IsDestroyed &&
               building.HasOwnerFaction &&
               building.OwnerFactionId == factionId
            ? 1
            : 0;
    }

    private static int CountBuildingForFactionAndId(
        Context context,
        RuntimeBuildingEntity building,
        byte factionId,
        string normalized)
    {
        return CountBuildingForFaction(building, factionId) == 1 &&
               context.RuntimeBuildingMatchesId != null &&
               context.RuntimeBuildingMatchesId(building, normalized)
            ? 1
            : 0;
    }

    private static int CountProducedUnitsForBuilding(
        Context context,
        RuntimeBuildingEntity building,
        byte factionId,
        string normalized,
        EntityManager em)
    {
        if (building == null ||
            building.IsDestroyed ||
            !building.HasOwnerFaction ||
            building.OwnerFactionId != factionId ||
            building.ProducedUnits == null)
        {
            return 0;
        }

        int count = 0;
        context.ProductionSystem?.PruneProducedUnits(building.ProducedUnits, building.ProducedUnitSlots, building.ProducedUnitPrefabs, em);
        for (int i = 0; i < building.ProducedUnits.Count; i++)
        {
            Entity unit = building.ProducedUnits[i];
            if (em.HasComponent<Faction>(unit) && em.GetComponentData<Faction>(unit).Id != factionId)
                continue;
            if (!RuntimeProducedUnitMatchesId(context, building, unit, normalized, em))
                continue;

            count++;
        }

        return count;
    }

    private static int CountPendingProductionsForBuilding(
        Context context,
        RuntimeBuildingEntity building,
        byte factionId,
        string normalized)
    {
        if (building == null ||
            building.IsDestroyed ||
            !building.HasOwnerFaction ||
            building.OwnerFactionId != factionId ||
            building.PendingProductions == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < building.PendingProductions.Count; i++)
        {
            RuntimeBuildingEntity.PendingProduction pending = building.PendingProductions[i];
            if (pending == null)
                continue;
            if (context.UnitPrefabMatchesId == null || !context.UnitPrefabMatchesId(pending.Prefab, normalized))
                continue;

            count++;
        }

        return count;
    }

    private static void AddHouseBuildingId(
        Context context,
        List<int> results,
        int buildingId,
        RuntimeBuildingEntity building)
    {
        if (building == null || building.IsDestroyed || building.Instance == null)
            return;
        if (context.IsHouseBuilding == null || !context.IsHouseBuilding(building))
            return;

        results.Add(buildingId);
    }

    private static void AddBuildingIdByRole(
        List<int> results,
        BuildingRole role,
        int buildingId,
        RuntimeBuildingEntity building)
    {
        if (building?.Definition == null || building.IsDestroyed)
            return;
        if (building.Definition.Role != role)
            return;

        results.Add(buildingId);
    }

    private static bool IsBuildingCombatEntity(RuntimeBuildingEntity building, Entity combatEntity)
    {
        return building != null && building.CombatEntity == combatEntity;
    }

    private static bool RuntimeProducedUnitMatchesId(Context context, RuntimeBuildingEntity building, Entity unit, string normalizedUnitId, EntityManager em)
    {
        if (string.IsNullOrEmpty(normalizedUnitId))
            return true;
        if (building?.ProducedUnitPrefabs != null &&
            building.ProducedUnitPrefabs.TryGetValue(unit, out GameObject prefab) &&
            context.UnitPrefabMatchesId != null &&
            context.UnitPrefabMatchesId(prefab, normalizedUnitId))
        {
            return true;
        }
        if (unit != Entity.Null &&
            em.Exists(unit) &&
            em.HasComponent<UnitSourcePrefabKey>(unit))
        {
            string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(unit).Value.ToString();
            if (Normalize(context, sourceKey) == normalizedUnitId)
                return true;
        }

        return false;
    }

    private static string Normalize(Context context, string value)
    {
        return context.NormalizeId != null ? context.NormalizeId(value) : string.Empty;
    }
}
