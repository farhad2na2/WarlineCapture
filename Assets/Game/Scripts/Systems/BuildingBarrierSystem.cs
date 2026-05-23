using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

internal sealed class BuildingBarrierSystem
{
    public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
    public delegate bool TryGetGridDataDelegate(out Entity gridEntity, out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerData blockerData);
    public delegate void EntityManagerAction(EntityManager entityManager);
    public delegate EntityQuery EntityQueryProvider();
    public delegate bool BuildingDefinitionPredicate(BuildingDefinition definition);
    public delegate bool RuntimeBuildingApproachCellDelegate(RuntimeBuildingData building, int2 unitFootprint, int2 referenceCell, out int2 goal);

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
        public readonly TryGetGridDataDelegate TryGetGridData;
        public readonly EntityManagerAction EnsureEntityQueries;
        public readonly EntityQueryProvider GetLiveFactionUnitsQuery;
        public readonly BuildingDefinitionPredicate IsWallGateDefinition;
        public readonly RuntimeBuildingApproachCellDelegate TryGetRuntimeBuildingApproachCell;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            TryGetEntityManagerDelegate tryGetEntityManager,
            TryGetGridDataDelegate tryGetGridData,
            EntityManagerAction ensureEntityQueries,
            EntityQueryProvider getLiveFactionUnitsQuery,
            BuildingDefinitionPredicate isWallGateDefinition,
            RuntimeBuildingApproachCellDelegate tryGetRuntimeBuildingApproachCell)
        {
            RuntimeBuildings = runtimeBuildings;
            TryGetEntityManager = tryGetEntityManager;
            TryGetGridData = tryGetGridData;
            EnsureEntityQueries = ensureEntityQueries;
            GetLiveFactionUnitsQuery = getLiveFactionUnitsQuery;
            IsWallGateDefinition = isWallGateDefinition;
            TryGetRuntimeBuildingApproachCell = tryGetRuntimeBuildingApproachCell;
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

        if (TryFindRuntimeBuildingByCombatEntity(context, finalTarget, out RuntimeBuildingData finalBuilding) &&
            finalBuilding?.Definition != null &&
            (finalBuilding.Definition.IsWall || IsWallGateDefinition(context, finalBuilding.Definition)))
        {
            return false;
        }

        if (!TryFindEnemyWallPerimeterContainingCell(context, attackerFactionId, finalTargetCell, out byte breachedFactionId, out RectInt breachedPerimeter))
            return false;

        if (HasOpenBaseBreach(context, breachedFactionId, breachedPerimeter))
            return false;

        if (!TryFindBreachBuilding(context, breachedFactionId, attackerCell, preferGate: true, out RuntimeBuildingData breachBuilding, out reason) &&
            !TryFindBreachBuilding(context, breachedFactionId, attackerCell, preferGate: false, out breachBuilding, out reason))
        {
            return false;
        }

        if (breachBuilding == null ||
            breachBuilding.CombatEntity == Entity.Null ||
            breachBuilding.CombatEntity == finalTarget ||
            context.TryGetEntityManager == null ||
            !context.TryGetEntityManager(out EntityManager em) ||
            !em.Exists(breachBuilding.CombatEntity) ||
            !em.HasComponent<UnitHealth>(breachBuilding.CombatEntity) ||
            em.GetComponentData<UnitHealth>(breachBuilding.CombatEntity).Current <= 0 ||
            !em.HasComponent<LocalTransform>(breachBuilding.CombatEntity))
        {
            return false;
        }

        breachTarget = breachBuilding.CombatEntity;
        int2 centerCell = new(
            breachBuilding.OriginCell.x + Mathf.Max(1, breachBuilding.Definition.FootprintCells.x) / 2,
            breachBuilding.OriginCell.y + Mathf.Max(1, breachBuilding.Definition.FootprintCells.y) / 2);
        breachCell = centerCell;

        if (context.TryGetGridData != null &&
            context.TryGetGridData(out Entity gridEntity, out GridConfig grid, out _, out DynamicBlockerData blockerData) &&
            em.HasComponent<DynamicOccupancyData>(gridEntity))
        {
            NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
            NativeBitArray occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
            if (TryFindBreachApproachCell(
                    grid,
                    walkable,
                    blockerData.Blocked,
                    blockerData.FriendlyPassFactionIds,
                    occupied,
                    breachBuilding.OriginCell,
                    breachBuilding.Definition.FootprintCells,
                    breachedPerimeter,
                    new int2(1, 1),
                    attackerCell,
                    attackerFactionId,
                    out int2 outsideApproachCell))
            {
                breachCell = outsideApproachCell;
            }
            else if (context.TryGetRuntimeBuildingApproachCell != null &&
                     context.TryGetRuntimeBuildingApproachCell(
                         breachBuilding,
                         new int2(1, 1),
                         attackerCell,
                         out int2 approachCell))
            {
                breachCell = approachCell;
            }
        }

        breachPosition = em.GetComponentData<LocalTransform>(breachBuilding.CombatEntity).Position;
        return true;
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

    private static bool TryFindBreachApproachCell(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        Vector2Int originCell,
        Vector2Int footprintCells,
        RectInt perimeterRect,
        int2 unitFootprint,
        int2 referenceCell,
        byte factionId,
        out int2 goal)
    {
        goal = default;
        RectInt breachRect = new(originCell, footprintCells);
        int2 outsideDirection = ResolvePerimeterOutsideDirection(breachRect, perimeterRect);
        if (outsideDirection.x == 0 && outsideDirection.y == 0)
            return false;

        int2 clampedUnitFootprint = UnitFootprintUtility.ClampSize(unitFootprint);
        int2 breachCenter = new(
            breachRect.xMin + Mathf.Max(1, breachRect.width) / 2,
            breachRect.yMin + Mathf.Max(1, breachRect.height) / 2);

        bool found = false;
        int bestScore = int.MaxValue;
        const int maxApproachDistance = 18;
        for (int distance = 1; distance <= maxApproachDistance; distance++)
        {
            int lateralPadding = math.min(6, distance + 2);
            if (outsideDirection.x != 0)
            {
                int x = outsideDirection.x < 0
                    ? breachRect.xMin - distance
                    : breachRect.xMax - 1 + distance;
                for (int y = breachRect.yMin - lateralPadding; y <= breachRect.yMax - 1 + lateralPadding; y++)
                    TryScoreBreachApproachCandidate(grid, walkable, blocked, friendlyPassFactionIds, occupied, perimeterRect, outsideDirection, clampedUnitFootprint, referenceCell, breachCenter, factionId, x, y, ref bestScore, ref goal, ref found);
            }
            else
            {
                int y = outsideDirection.y < 0
                    ? breachRect.yMin - distance
                    : breachRect.yMax - 1 + distance;
                for (int x = breachRect.xMin - lateralPadding; x <= breachRect.xMax - 1 + lateralPadding; x++)
                    TryScoreBreachApproachCandidate(grid, walkable, blocked, friendlyPassFactionIds, occupied, perimeterRect, outsideDirection, clampedUnitFootprint, referenceCell, breachCenter, factionId, x, y, ref bestScore, ref goal, ref found);
            }

            if (found)
                return true;
        }

        return false;
    }

    private static int2 ResolvePerimeterOutsideDirection(RectInt breachRect, RectInt perimeterRect)
    {
        float breachCenterX = breachRect.xMin + (Mathf.Max(1, breachRect.width) * 0.5f);
        float breachCenterY = breachRect.yMin + (Mathf.Max(1, breachRect.height) * 0.5f);
        int distLeft = Mathf.RoundToInt(Mathf.Abs(breachCenterX - perimeterRect.xMin));
        int distRight = Mathf.RoundToInt(Mathf.Abs(breachCenterX - (perimeterRect.xMax - 1)));
        int distBottom = Mathf.RoundToInt(Mathf.Abs(breachCenterY - perimeterRect.yMin));
        int distTop = Mathf.RoundToInt(Mathf.Abs(breachCenterY - (perimeterRect.yMax - 1)));
        int best = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

        if (best == distLeft)
            return new int2(-1, 0);
        if (best == distRight)
            return new int2(1, 0);
        if (best == distBottom)
            return new int2(0, -1);
        return new int2(0, 1);
    }

    private static void TryScoreBreachApproachCandidate(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        RectInt perimeterRect,
        int2 outsideDirection,
        int2 unitFootprint,
        int2 referenceCell,
        int2 breachCenter,
        byte factionId,
        int x,
        int y,
        ref int bestScore,
        ref int2 bestCell,
        ref bool found)
    {
        if ((uint)x >= (uint)grid.Width || (uint)y >= (uint)grid.Height)
            return;

        int2 candidate = new(x, y);
        if (!IsOutsidePerimeterOnSide(candidate, perimeterRect, outsideDirection))
            return;

        if (!UnitFootprintUtility.CanPlace(grid, walkable, blocked, friendlyPassFactionIds, occupied, candidate, unitFootprint, referenceCell, factionId))
            return;

        int referenceScore = math.abs(referenceCell.x - x) + math.abs(referenceCell.y - y);
        int breachScore = math.abs(breachCenter.x - x) + math.abs(breachCenter.y - y);
        int score = referenceScore + (breachScore * 2);
        if (found && score >= bestScore)
            return;

        bestScore = score;
        bestCell = candidate;
        found = true;
    }

    private static bool IsOutsidePerimeterOnSide(int2 cell, RectInt perimeterRect, int2 outsideDirection)
    {
        if (outsideDirection.x < 0)
            return cell.x < perimeterRect.xMin;
        if (outsideDirection.x > 0)
            return cell.x >= perimeterRect.xMax;
        if (outsideDirection.y < 0)
            return cell.y < perimeterRect.yMin;
        return cell.y >= perimeterRect.yMax;
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
