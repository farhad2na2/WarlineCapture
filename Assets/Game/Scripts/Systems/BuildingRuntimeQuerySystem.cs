using System.Collections.Generic;
using SnivelerCode.GpuAnimation.Scripts.Authoring;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RuntimeBuildingData = BuildingPlacementSystem.RuntimeBuildingData;
using BuildingDefinition = BuildingPlacementSystem.BuildingDefinition;

internal sealed class BuildingRuntimeQuerySystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate string StringNormalizer(string value);
    public delegate bool BuildingPredicate(RuntimeBuildingData building);
    public delegate bool BuildingIdPredicate(RuntimeBuildingData building, string normalizedId);
    public delegate bool UnitPrefabPredicate(GameObject prefab, string normalizedId);
    public delegate bool TryResolveBuildingWorldPositionDelegate(RuntimeBuildingData building, out Vector3 worldPosition);
    public delegate bool TryGetBuildingApproachCellDelegate(RuntimeBuildingData building, int2 unitFootprint, int2 referenceCell, out int2 goal);
    public delegate bool IsBuildingApproachCellDelegate(RuntimeBuildingData building, int2 currentCell, int2 unitFootprint);
    public delegate bool BuildingDefinitionPredicate(BuildingDefinition definition);

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
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

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            TryGetEntityManagerDelegate tryGetEntityManager,
            BuildingProductionSystem productionSystem,
            StringNormalizer normalizeId,
            BuildingPredicate isHouseBuilding,
            BuildingIdPredicate runtimeBuildingMatchesId,
            UnitPrefabPredicate unitPrefabMatchesId,
            TryResolveBuildingWorldPositionDelegate tryResolveBuildingFocusWorldPosition,
            TryGetBuildingApproachCellDelegate tryGetBuildingApproachCell,
            IsBuildingApproachCellDelegate isBuildingApproachCell,
            BuildingDefinitionPredicate isWallGateDefinition)
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
        }
    }

    public int CountRuntimeBuildingsForFaction(Context context, byte factionId)
    {
        int count = 0;
        if (context.RuntimeBuildings == null)
            return count;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;

            count++;
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

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;
            if (context.RuntimeBuildingMatchesId == null || !context.RuntimeBuildingMatchesId(building, normalized))
                continue;

            count++;
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

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;
            if (building.ProducedUnits == null)
                continue;

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
        }

        return count;
    }

    public int CountPendingProductionsForFaction(Context context, byte factionId, string unitId)
    {
        string normalized = Normalize(context, unitId);
        int count = 0;
        if (context.RuntimeBuildings == null)
            return count;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || !building.HasOwnerFaction || building.OwnerFactionId != factionId)
                continue;
            if (building.PendingProductions == null)
                continue;

            for (int i = 0; i < building.PendingProductions.Count; i++)
            {
                RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
                if (pending == null)
                    continue;
                if (context.UnitPrefabMatchesId == null || !context.UnitPrefabMatchesId(pending.Prefab, normalized))
                    continue;

                count++;
            }
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

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.IsDestroyed || building.Instance == null)
                continue;
            if (context.IsHouseBuilding == null || !context.IsHouseBuilding(building))
                continue;

            results.Add(pair.Key);
        }
    }

    public void GetRuntimeBuildingIdsByRole(Context context, BuildingRole role, List<int> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (context.RuntimeBuildings == null)
            return;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building?.Definition == null || building.IsDestroyed)
                continue;

            if (building.Definition.Role != role)
                continue;

            results.Add(pair.Key);
        }
    }

    public bool TryGetRuntimeBuildingFocusWorldPosition(Context context, int buildingId, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        if (!TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingData building))
            return false;

        return context.TryResolveBuildingFocusWorldPosition != null &&
               context.TryResolveBuildingFocusWorldPosition(building, out worldPosition);
    }

    public bool TryGetRuntimeBuildingDestroyedState(Context context, int buildingId, out bool isDestroyed)
    {
        isDestroyed = false;
        if (!TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingData building))
            return false;

        isDestroyed = building.IsDestroyed;
        return true;
    }

    public bool TryGetRuntimeBuildingRefugeeSettings(Context context, int buildingId, out int refugeeCapacity, out int upkeepPerCitizenPerDay)
    {
        refugeeCapacity = 0;
        upkeepPerCitizenPerDay = 0;
        if (!TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingData building) || building?.Definition == null)
            return false;

        refugeeCapacity = Mathf.Max(0, building.Definition.RefugeeCapacity);
        upkeepPerCitizenPerDay = Mathf.Max(0, building.Definition.RefugeeUpkeepPerCitizenPerDay);
        return true;
    }

    public bool IsRuntimeBuildingCityGenerated(Context context, int buildingId)
    {
        return TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingData building) &&
               building != null &&
               building.IsCityGenerated;
    }

    public bool IsRuntimeBuildingWall(Context context, int buildingId)
    {
        return TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingData building) &&
               building?.Definition != null &&
               building.Definition.IsWall;
    }

    public bool TryGetRuntimeBuildingOwnerFaction(Context context, int buildingId, out byte factionId)
    {
        factionId = 0;
        if (!TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingData building) || building == null || !building.HasOwnerFaction)
            return false;

        factionId = building.OwnerFactionId;
        return true;
    }

    public bool TryGetRuntimeBuildingCombatInfo(Context context, Entity combatEntity, out bool isGate, out bool isWall, out byte ownerFactionId)
    {
        isGate = false;
        isWall = false;
        ownerFactionId = 0;
        if (!TryFindRuntimeBuildingByCombatEntity(context, combatEntity, out RuntimeBuildingData building) || building?.Definition == null)
            return false;

        isGate = context.IsWallGateDefinition != null && context.IsWallGateDefinition(building.Definition);
        isWall = building.Definition.IsWall;
        ownerFactionId = building.HasOwnerFaction ? building.OwnerFactionId : (byte)0;
        return true;
    }

    public bool TryGetRuntimeBuildingApproachCell(Context context, int buildingId, int2 unitFootprint, int2 referenceCell, out int2 goal)
    {
        goal = default;
        return TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingData building) &&
               building != null &&
               !building.IsDestroyed &&
               context.TryGetBuildingApproachCell != null &&
               context.TryGetBuildingApproachCell(building, unitFootprint, referenceCell, out goal);
    }

    public bool IsRuntimeBuildingApproachCell(Context context, int buildingId, int2 currentCell, int2 unitFootprint)
    {
        return TryGetRuntimeBuilding(context, buildingId, out RuntimeBuildingData building) &&
               building != null &&
               !building.IsDestroyed &&
               context.IsBuildingApproachCell != null &&
               context.IsBuildingApproachCell(building, currentCell, unitFootprint);
    }

    private static bool TryGetRuntimeBuilding(Context context, int buildingId, out RuntimeBuildingData building)
    {
        building = null;
        return context.RuntimeBuildings != null &&
               context.RuntimeBuildings.TryGetValue(buildingId, out building);
    }

    private static bool TryFindRuntimeBuildingByCombatEntity(Context context, Entity combatEntity, out RuntimeBuildingData building)
    {
        building = null;
        if (combatEntity == Entity.Null || context.RuntimeBuildings == null)
            return false;

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData candidate = entry.Value;
            if (candidate == null || candidate.CombatEntity != combatEntity)
                continue;

            building = candidate;
            return true;
        }

        return false;
    }

    private static bool RuntimeProducedUnitMatchesId(Context context, RuntimeBuildingData building, Entity unit, string normalizedUnitId, EntityManager em)
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
