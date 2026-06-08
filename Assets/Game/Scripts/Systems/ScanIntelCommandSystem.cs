using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class ScanIntelCommandSystem
{
    public const int DefaultScanRadiusCells = 12;

    public readonly struct Result
    {
        public readonly TacticalCommandResult CommandResult;
        public readonly int2 CenterCell;
        public readonly float3 CenterWorld;
        public readonly int RadiusCells;
        public readonly int RevealedCount;
        public readonly bool HasWorldPosition;

        private Result(
            TacticalCommandResult commandResult,
            int2 centerCell,
            float3 centerWorld,
            int radiusCells,
            int revealedCount,
            bool hasWorldPosition)
        {
            CommandResult = commandResult;
            CenterCell = centerCell;
            CenterWorld = centerWorld;
            RadiusCells = radiusCells;
            RevealedCount = revealedCount;
            HasWorldPosition = hasWorldPosition;
        }

        public static Result Success(int2 centerCell, float3 centerWorld, int radiusCells, int revealedCount)
        {
            return new Result(TacticalCommandResult.Success(), centerCell, centerWorld, radiusCells, revealedCount, true);
        }

        public static Result Rejected(TacticalCommandReasonCode reasonCode)
        {
            return new Result(TacticalCommandResult.Rejected(reasonCode), default, default, DefaultScanRadiusCells, 0, false);
        }
    }

    private World _queryWorld;
    private EntityQuery _unitScanTargetQuery;
    private EntityQuery _buildingScanTargetQuery;
    private EntityQuery _feedQueueQuery;

    public Result TryIssueScan(
        EntityManager em,
        Vector2 screenPosition,
        int requestId,
        int frame,
        EntityQuery gridConfigQuery,
        SelectedMoveOrderCommandSystem.ClickedCellResolver tryGetClickedCell)
    {
        EnsureEntityQueries(em);
        if (gridConfigQuery.IsEmptyIgnoreFilter)
            return Result.Rejected(TacticalCommandReasonCode.ScanUnavailable);

        if (tryGetClickedCell == null ||
            !tryGetClickedCell(screenPosition, em, out int2 centerCell, out Vector3 centerWorld))
        {
            return Result.Rejected(TacticalCommandReasonCode.TargetOutOfBounds);
        }

        GridConfig grid = em.GetComponentData<GridConfig>(gridConfigQuery.GetSingletonEntity());
        if (!GridUtils.InBounds(centerCell, grid.Width, grid.Height))
            return Result.Rejected(TacticalCommandReasonCode.TargetOutOfBounds);

        int radiusCells = DefaultScanRadiusCells;
        int revealedCount = RevealUnits(em, grid, centerCell, radiusCells, frame);
        revealedCount += RevealBuildings(em, grid, centerCell, radiusCells, frame);
        AppendFeedEntry(em, requestId, frame, centerCell, centerWorld, radiusCells, revealedCount);

        return Result.Success(centerCell, centerWorld, radiusCells, revealedCount);
    }

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _unitScanTargetQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>());
        _buildingScanTargetQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<RuntimeBuildingCombatInfo>());
        _feedQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<ScanIntelFeedQueueTag>(),
            ComponentType.ReadWrite<ScanIntelFeedEntry>());
    }

    private int RevealUnits(
        EntityManager em,
        in GridConfig grid,
        int2 centerCell,
        int radiusCells,
        int frame)
    {
        if (_unitScanTargetQuery.IsEmptyIgnoreFilter)
            return 0;

        int revealed = 0;
        using NativeArray<Entity> entities = _unitScanTargetQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsRevealableScanTarget(em, entity))
                continue;
            if (em.HasComponent<RuntimeBuildingCombatInfo>(entity))
                continue;

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            if (ChebyshevDistance(centerCell, cell) > radiusCells)
                continue;

            float3 position = em.HasComponent<LocalTransform>(entity)
                ? em.GetComponentData<LocalTransform>(entity).Position
                : GridUtils.CellToWorldCenter(grid, cell);
            RevealEntity(em, entity, cell, position, frame);
            revealed++;
        }

        return revealed;
    }

    private int RevealBuildings(
        EntityManager em,
        in GridConfig grid,
        int2 centerCell,
        int radiusCells,
        int frame)
    {
        if (_buildingScanTargetQuery.IsEmptyIgnoreFilter)
            return 0;

        int revealed = 0;
        using NativeArray<Entity> entities = _buildingScanTargetQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!IsRevealableScanTarget(em, entity))
                continue;

            RuntimeBuildingCombatInfo building = em.GetComponentData<RuntimeBuildingCombatInfo>(entity);
            if (DistanceToFootprint(centerCell, building.OriginCell, building.FootprintCells) > radiusCells)
                continue;

            int2 center = building.OriginCell + math.max(new int2(1, 1), building.FootprintCells) / 2;
            float3 position = em.HasComponent<LocalTransform>(entity)
                ? em.GetComponentData<LocalTransform>(entity).Position
                : GridUtils.CellToWorldCenter(grid, center);
            RevealEntity(em, entity, center, position, frame);
            revealed++;
        }

        return revealed;
    }

    private static bool IsRevealableScanTarget(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity) || !em.HasComponent<Faction>(entity))
            return false;
        if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
            return false;

        byte factionId = em.GetComponentData<Faction>(entity).Id;
        return FactionIdentitySystem.IsHostileToPlayer(factionId);
    }

    private static void RevealEntity(EntityManager em, Entity entity, int2 cell, float3 position, int frame)
    {
        if (!em.HasComponent<ScanIntelRevealedTag>(entity))
            em.AddComponent<ScanIntelRevealedTag>(entity);

        ScanIntelLastSeen lastSeen = new()
        {
            Cell = cell,
            Position = position,
            LastScanFrame = frame,
            FactionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0
        };

        if (em.HasComponent<ScanIntelLastSeen>(entity))
            em.SetComponentData(entity, lastSeen);
        else
            em.AddComponentData(entity, lastSeen);
    }

    private void AppendFeedEntry(
        EntityManager em,
        int requestId,
        int frame,
        int2 centerCell,
        float3 centerWorld,
        int radiusCells,
        int revealedCount)
    {
        Entity feedEntity = EnsureFeedQueue(em);
        DynamicBuffer<ScanIntelFeedEntry> feed = em.GetBuffer<ScanIntelFeedEntry>(feedEntity);
        feed.Add(new ScanIntelFeedEntry
        {
            RequestId = requestId,
            Frame = frame,
            CenterCell = centerCell,
            CenterWorld = centerWorld,
            RadiusCells = radiusCells,
            RevealedCount = revealedCount
        });
    }

    private Entity EnsureFeedQueue(EntityManager em)
    {
        if (!_feedQueueQuery.IsEmptyIgnoreFilter)
            return _feedQueueQuery.GetSingletonEntity();

        Entity entity = em.CreateEntity(typeof(ScanIntelFeedQueueTag));
        em.SetName(entity, "ScanIntelFeedQueue");
        em.AddBuffer<ScanIntelFeedEntry>(entity);
        return entity;
    }

    private static int ChebyshevDistance(int2 a, int2 b)
    {
        int2 delta = math.abs(a - b);
        return math.max(delta.x, delta.y);
    }

    private static int DistanceToFootprint(int2 cell, int2 origin, int2 footprint)
    {
        int2 size = math.max(new int2(1, 1), footprint);
        int minX = origin.x;
        int minY = origin.y;
        int maxX = origin.x + size.x - 1;
        int maxY = origin.y + size.y - 1;
        int dx = cell.x < minX ? minX - cell.x : (cell.x > maxX ? cell.x - maxX : 0);
        int dy = cell.y < minY ? minY - cell.y : (cell.y > maxY ? cell.y - maxY : 0);
        return math.max(dx, dy);
    }
}
