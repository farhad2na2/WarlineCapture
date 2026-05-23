using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RuntimeBuildingData = BuildingPlacementSystem.RuntimeBuildingData;
using BuildingDefinition = BuildingPlacementSystem.BuildingDefinition;

internal sealed class BuildingBarrierSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate void EntityManagerAction(EntityManager entityManager);
    public delegate EntityQuery EntityQueryProvider();
    public delegate bool BuildingDefinitionPredicate(BuildingDefinition definition);

    private readonly struct RuntimeBaseBreach
    {
        public readonly byte OwnerFactionId;
        public readonly RectInt Rect;

        public RuntimeBaseBreach(byte ownerFactionId, RectInt rect)
        {
            OwnerFactionId = ownerFactionId;
            Rect = rect;
        }
    }

    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
        public readonly TryGetEntityManagerDelegate TryGetEntityManager;
        public readonly EntityManagerAction EnsureEntityQueries;
        public readonly EntityQueryProvider GetLiveFactionUnitsQuery;
        public readonly BuildingDefinitionPredicate IsWallGateDefinition;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            TryGetEntityManagerDelegate tryGetEntityManager,
            EntityManagerAction ensureEntityQueries,
            EntityQueryProvider getLiveFactionUnitsQuery,
            BuildingDefinitionPredicate isWallGateDefinition)
        {
            RuntimeBuildings = runtimeBuildings;
            TryGetEntityManager = tryGetEntityManager;
            EnsureEntityQueries = ensureEntityQueries;
            GetLiveFactionUnitsQuery = getLiveFactionUnitsQuery;
            IsWallGateDefinition = isWallGateDefinition;
        }
    }

    private readonly List<RuntimeBaseBreach> _openBaseBreaches = new();

    private const float BarrierDoorOpenCloseSpeed = 2f;
    private const int BarrierDoorDetectPaddingCells = 8;

    public void RememberOpenBaseBreach(Context context, RuntimeBuildingData building)
    {
        if (building?.Definition == null ||
            !building.HasOwnerFaction ||
            (!building.Definition.IsWall && !IsWallGateDefinition(context, building.Definition)))
        {
            return;
        }

        RectInt rect = new(building.OriginCell, building.Definition.FootprintCells);
        for (int i = 0; i < _openBaseBreaches.Count; i++)
        {
            RuntimeBaseBreach existing = _openBaseBreaches[i];
            if (existing.OwnerFactionId == building.OwnerFactionId && existing.Rect == rect)
                return;
        }

        _openBaseBreaches.Add(new RuntimeBaseBreach(building.OwnerFactionId, rect));
    }

    public bool HasOpenBaseBreach(Context context, byte ownerFactionId, RectInt perimeterRect)
    {
        for (int i = 0; i < _openBaseBreaches.Count; i++)
        {
            RuntimeBaseBreach breach = _openBaseBreaches[i];
            if (breach.OwnerFactionId != ownerFactionId)
                continue;
            if (!RectTouchesPerimeter(breach.Rect, perimeterRect))
                continue;
            if (HasActiveWallOrGateOverlapping(context, breach.Rect, ownerFactionId))
                continue;

            return true;
        }

        return false;
    }

    public bool TryFindEnemyWallPerimeterContainingCell(
        Context context,
        byte attackerFactionId,
        int2 targetCell,
        out byte breachedFactionId,
        out RectInt breachedPerimeter)
    {
        breachedFactionId = 0;
        breachedPerimeter = default;
        var perimeters = new Dictionary<byte, RectInt>();

        if (context.RuntimeBuildings == null)
            return false;

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.Definition == null ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId == attackerFactionId ||
                (!building.Definition.IsWall && !IsWallGateDefinition(context, building.Definition)))
            {
                continue;
            }

            RectInt rect = new(building.OriginCell, building.Definition.FootprintCells);
            if (perimeters.TryGetValue(building.OwnerFactionId, out RectInt existing))
                perimeters[building.OwnerFactionId] = UnionRects(existing, rect);
            else
                perimeters.Add(building.OwnerFactionId, rect);
        }

        int bestArea = int.MaxValue;
        foreach (var pair in perimeters)
        {
            RectInt rect = pair.Value;
            if (targetCell.x < rect.xMin ||
                targetCell.x >= rect.xMax ||
                targetCell.y < rect.yMin ||
                targetCell.y >= rect.yMax)
            {
                continue;
            }

            int area = Mathf.Max(1, rect.width) * Mathf.Max(1, rect.height);
            if (area >= bestArea)
                continue;

            bestArea = area;
            breachedFactionId = pair.Key;
            breachedPerimeter = rect;
        }

        return bestArea < int.MaxValue;
    }

    public bool TryFindBreachBuilding(
        Context context,
        byte breachedFactionId,
        int2 attackerCell,
        bool preferGate,
        out RuntimeBuildingData breachBuilding,
        out string reason)
    {
        breachBuilding = null;
        reason = preferGate ? "Gate" : "Wall";
        int bestScore = int.MaxValue;

        if (context.RuntimeBuildings == null ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em))
        {
            return false;
        }

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.Definition == null ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId != breachedFactionId ||
                building.CombatEntity == Entity.Null)
            {
                continue;
            }

            bool isGate = IsWallGateDefinition(context, building.Definition);
            bool isWall = building.Definition.IsWall;
            if (preferGate ? !isGate : (!isWall || isGate))
                continue;

            if (!em.Exists(building.CombatEntity) ||
                !em.HasComponent<UnitHealth>(building.CombatEntity) ||
                em.GetComponentData<UnitHealth>(building.CombatEntity).Current <= 0)
            {
                continue;
            }

            int2 center = new(
                building.OriginCell.x + Mathf.Max(1, building.Definition.FootprintCells.x) / 2,
                building.OriginCell.y + Mathf.Max(1, building.Definition.FootprintCells.y) / 2);
            int2 delta = center - attackerCell;
            int score = delta.x * delta.x + delta.y * delta.y;
            if (score >= bestScore)
                continue;

            bestScore = score;
            breachBuilding = building;
        }

        return breachBuilding != null;
    }

    public void UpdateRoadBarrierDoors(Context context, float deltaTime)
    {
        if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0)
            return;

        bool hasRoadGate = false;
        foreach (var entry in context.RuntimeBuildings)
        {
            if (IsActiveRoadGateBuilding(context, entry.Value))
            {
                hasRoadGate = true;
                break;
            }
        }
        if (!hasRoadGate)
            return;

        if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))
            return;

        context.EnsureEntityQueries?.Invoke(em);
        if (context.GetLiveFactionUnitsQuery == null)
        {
            foreach (var entry in context.RuntimeBuildings)
            {
                RuntimeBuildingData building = entry.Value;
                if (IsActiveRoadGateBuilding(context, building))
                    UpdateRoadBarrierDoorVisual(context, building, false, deltaTime);
            }
            return;
        }

        EntityQuery liveFactionUnitsQuery = context.GetLiveFactionUnitsQuery();
        if (liveFactionUnitsQuery.IsEmptyIgnoreFilter)
        {
            foreach (var entry in context.RuntimeBuildings)
            {
                RuntimeBuildingData building = entry.Value;
                if (IsActiveRoadGateBuilding(context, building))
                    UpdateRoadBarrierDoorVisual(context, building, false, deltaTime);
            }
            return;
        }

        using var factions = liveFactionUnitsQuery.ToComponentDataArray<Faction>(Allocator.Temp);
        using var unitGrids = liveFactionUnitsQuery.ToComponentDataArray<UnitGrid>(Allocator.Temp);
        using var footprints = liveFactionUnitsQuery.ToComponentDataArray<UnitFootprint>(Allocator.Temp);

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (!IsActiveRoadGateBuilding(context, building))
                continue;

            bool shouldOpen = building.HasOwnerFaction &&
                HasNearbyFriendlyUnit(building, factions, unitGrids, footprints, building.OwnerFactionId);
            UpdateRoadBarrierDoorVisual(context, building, shouldOpen, deltaTime);
        }
    }

    public int GetRuntimeRoadBarrierGateRects(Context context, byte factionId, List<RectInt> rects, List<int> buildingIds = null)
    {
        rects?.Clear();
        buildingIds?.Clear();
        int count = 0;
        if (context.RuntimeBuildings == null)
            return count;

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (!IsActiveRoadGateBuilding(context, building) ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId != factionId)
            {
                continue;
            }

            count++;
            rects?.Add(new RectInt(building.OriginCell, building.Definition.FootprintCells));
            buildingIds?.Add(building.Id);
        }

        return count;
    }

    public bool IsActiveRoadGateBuilding(Context context, RuntimeBuildingData building)
    {
        return building != null &&
               !building.IsDestroyed &&
               building.DoorZ != null &&
               IsWallGateDefinition(context, building.Definition);
    }

    public void UpdateRoadBarrierDoorVisual(Context context, RuntimeBuildingData building, bool shouldOpen, float deltaTime)
    {
        if (building == null || building.IsDestroyed || building.DoorZ == null)
            return;
        if (!IsWallGateDefinition(context, building.Definition))
            return;

        float target = shouldOpen ? 1f : 0f;
        building.DoorOpen01 = Mathf.MoveTowards(building.DoorOpen01, target, deltaTime * BarrierDoorOpenCloseSpeed);
        SetBarrierDoorOpen01(building, building.DoorOpen01);
    }

    public void SetBarrierDoorOpen01(RuntimeBuildingData building, float open01)
    {
        if (building?.DoorZ == null)
            return;

        Vector3 localEuler = building.DoorZ.localEulerAngles;
        localEuler.z = Mathf.LerpAngle(building.DoorClosedLocalEulerZ, building.DoorOpenLocalEulerZ, Mathf.Clamp01(open01));
        building.DoorZ.localEulerAngles = localEuler;
    }

    public bool TryResolveNearbyWallVertical(Context context, Vector2Int originCell, BuildingDefinition definition, out bool vertical)
    {
        vertical = false;
        if (definition == null || context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0)
            return false;

        RectInt gateRect = new(originCell, definition.FootprintCells);
        int bestDistance = int.MaxValue;
        bool found = false;

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building?.Definition == null || !IsLinearWallDefinition(building.Definition))
                continue;

            Vector2Int wallSize = building.Definition.FootprintCells;
            RectInt wallRect = new(building.OriginCell, wallSize);
            int dx = AxisDistance(gateRect.xMin, gateRect.xMax, wallRect.xMin, wallRect.xMax);
            int dy = AxisDistance(gateRect.yMin, gateRect.yMax, wallRect.yMin, wallRect.yMax);
            int distance = dx + dy;
            if (distance > 1 || distance >= bestDistance)
                continue;

            bestDistance = distance;
            vertical = wallSize.y > wallSize.x;
            found = true;
        }

        return found;
    }

    public bool ShouldAlignGateToNearbyWall(Context context, Vector2Int originCell, BuildingDefinition definition, out bool vertical)
    {
        vertical = false;
        return IsWallGateDefinition(definition) && TryResolveNearbyWallVertical(context, originCell, definition, out vertical);
    }

    public static bool IsLinearWallDefinition(BuildingDefinition definition)
    {
        return definition != null && definition.IsWall;
    }

    public static bool IsWallGateDefinition(BuildingDefinition definition)
    {
        if (definition == null)
            return false;

        string displayName = definition.DisplayName ?? string.Empty;
        string prefabName = definition.Prefab != null ? definition.Prefab.name : string.Empty;
        return displayName.IndexOf("Road_Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               prefabName.IndexOf("Road_Barrier", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool ShouldUseExpandedSelectionArea(BuildingDefinition definition)
    {
        if (definition == null)
            return false;

        return IsLinearWallDefinition(definition) || IsWallGateDefinition(definition);
    }

    private bool HasActiveWallOrGateOverlapping(Context context, RectInt rect, byte ownerFactionId)
    {
        if (context.RuntimeBuildings == null)
            return false;

        foreach (var entry in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = entry.Value;
            if (building == null ||
                building.IsDestroyed ||
                building.Definition == null ||
                !building.HasOwnerFaction ||
                building.OwnerFactionId != ownerFactionId ||
                (!building.Definition.IsWall && !IsWallGateDefinition(context, building.Definition)))
            {
                continue;
            }

            RectInt buildingRect = new(building.OriginCell, building.Definition.FootprintCells);
            if (RectsOverlap(rect, buildingRect))
                return true;
        }

        return false;
    }

    private static bool RectTouchesPerimeter(RectInt rect, RectInt perimeterRect)
    {
        return RectsOverlap(rect, perimeterRect) ||
               (rect.xMin <= perimeterRect.xMin && rect.xMax > perimeterRect.xMin && rect.yMin < perimeterRect.yMax && rect.yMax > perimeterRect.yMin) ||
               (rect.xMin < perimeterRect.xMax && rect.xMax >= perimeterRect.xMax && rect.yMin < perimeterRect.yMax && rect.yMax > perimeterRect.yMin) ||
               (rect.yMin <= perimeterRect.yMin && rect.yMax > perimeterRect.yMin && rect.xMin < perimeterRect.xMax && rect.xMax > perimeterRect.xMin) ||
               (rect.yMin < perimeterRect.yMax && rect.yMax >= perimeterRect.yMax && rect.xMin < perimeterRect.xMax && rect.xMax > perimeterRect.xMin);
    }

    private static bool RectsOverlap(RectInt a, RectInt b)
    {
        return a.xMin < b.xMax &&
               a.xMax > b.xMin &&
               a.yMin < b.yMax &&
               a.yMax > b.yMin;
    }

    private static int AxisDistance(int minA, int maxA, int minB, int maxB)
    {
        if (maxA <= minB)
            return minB - maxA;

        if (maxB <= minA)
            return minA - maxB;

        return 0;
    }

    private static RectInt UnionRects(RectInt a, RectInt b)
    {
        int xMin = Mathf.Min(a.xMin, b.xMin);
        int yMin = Mathf.Min(a.yMin, b.yMin);
        int xMax = Mathf.Max(a.xMax, b.xMax);
        int yMax = Mathf.Max(a.yMax, b.yMax);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private static bool HasNearbyFriendlyUnit(
        RuntimeBuildingData building,
        NativeArray<Faction> factions,
        NativeArray<UnitGrid> unitGrids,
        NativeArray<UnitFootprint> footprints,
        byte factionId)
    {
        if (building?.Definition == null)
            return false;

        Vector2Int origin = building.OriginCell;
        Vector2Int size = building.Definition.FootprintCells;
        int minX = origin.x - BarrierDoorDetectPaddingCells;
        int minY = origin.y - BarrierDoorDetectPaddingCells;
        int maxX = origin.x + size.x + BarrierDoorDetectPaddingCells;
        int maxY = origin.y + size.y + BarrierDoorDetectPaddingCells;

        int count = Mathf.Min(factions.Length, Mathf.Min(unitGrids.Length, footprints.Length));
        for (int i = 0; i < count; i++)
        {
            if (factions[i].Id != factionId)
                continue;

            int2 unitSize = UnitFootprintUtility.ClampSize(footprints[i].Size);
            int2 unitMin = UnitFootprintUtility.GetMinCell(unitGrids[i].Cell, unitSize);
            int2 unitMax = unitMin + unitSize;
            if (unitMin.x < maxX && unitMax.x > minX &&
                unitMin.y < maxY && unitMax.y > minY)
                return true;
        }

        return false;
    }

    private static bool IsWallGateDefinition(Context context, BuildingDefinition definition)
    {
        return context.IsWallGateDefinition != null
            ? context.IsWallGateDefinition(definition)
            : IsWallGateDefinition(definition);
    }
}
