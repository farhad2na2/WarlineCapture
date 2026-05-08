using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class GridAuthoring : MonoBehaviour
{
    [SerializeField] private GridAuthoringConfig config;
    private int Width => config != null ? config.Width : 16;
    private int Height => config != null ? config.Height : 16;
    private float CellSize => config != null ? config.CellSize : 1f;
    private Vector2Int[] BlockedCells => config != null ? config.BlockedCells : null;
    private bool DrawGrid => config == null || config.DrawGrid;
    private bool DrawWhenNotSelected => config == null || config.DrawWhenNotSelected;
    private bool DrawRuntimeDebugInPlayMode => config == null || config.DrawRuntimeDebugInPlayMode;
    private bool FillWalkableCells => config != null && config.FillWalkableCells;
    private bool FillRoadCells => config == null || config.FillRoadCells;
    private bool FillSidewalkCells => config == null || config.FillSidewalkCells;
    private float RoadCellDebugScale => config != null ? config.RoadCellDebugScale : 0.35f;
    private bool FillBuildingCells => config == null || config.FillBuildingCells;
    private bool FillRuntimeBlockerCells => config == null || config.FillRuntimeBlockerCells;
    private bool FillVehicleFootprintCells => config == null || config.FillVehicleFootprintCells;
    private bool DrawUnitPaths => config == null || config.DrawUnitPaths;
    private int MaxGridLinesPerAxis => config != null ? config.MaxGridLinesPerAxis : 256;
    private int MaxFilledDebugCells => config != null ? config.MaxFilledDebugCells : 250000;
    private Color GridLineColor => config != null ? config.GridLineColor : new Color(1f, 1f, 1f, 0.15f);
    private Color WalkableFillColor => config != null ? config.WalkableFillColor : new Color(0.2f, 1f, 0.2f, 0.05f);
    private Color RoadFillColor => config != null ? config.RoadFillColor : new Color(0.2f, 0.7f, 1f, 0.28f);
    private Color SidewalkFillColor => config != null ? config.SidewalkFillColor : new Color(0.2f, 0.85f, 0.25f, 0.5f);
    private Color BuildingFillColor => config != null ? config.BuildingFillColor : new Color(1f, 0.65f, 0.2f, 0.3f);
    private Color RuntimeBlockerFillColor => config != null ? config.RuntimeBlockerFillColor : new Color(0.18f, 0.18f, 0.18f, 0.55f);
    private Color VehicleFootprintFillColor => config != null ? config.VehicleFootprintFillColor : new Color(0.08f, 0.5f, 0.82f, 0.4f);
    private Color UnitPathColor => config != null ? config.UnitPathColor : new Color(0.15f, 1f, 0.9f, 0.9f);
    private Color StuckUnitPathColor => config != null ? config.StuckUnitPathColor : new Color(1f, 0.15f, 0.15f, 0.95f);
    private Color BlockedFillColor => config != null ? config.BlockedFillColor : new Color(1f, 0.2f, 0.2f, 0.25f);

    public void Configure(GridAuthoringConfig config)
    {
        this.config = config;
    }

    private class GridBaker : Baker<GridAuthoring>
    {
        public override void Bake(GridAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new GridConfig
            {
                Width = authoring.Width,
                Height = authoring.Height,
                CellSize = authoring.CellSize,
                Origin = (float3)authoring.transform.position
            });

            var walkable = AddBuffer<GridWalkable>(entity);
            var roads = AddBuffer<GridRoad>(entity);
            var sidewalks = AddBuffer<GridRoadSidewalk>(entity);
            var dirtRoads = AddBuffer<GridRoadDirt>(entity);
            int size = authoring.Width * authoring.Height;
            walkable.ResizeUninitialized(size);
            roads.ResizeUninitialized(size);
            sidewalks.ResizeUninitialized(size);
            dirtRoads.ResizeUninitialized(size);
            for (int i = 0; i < size; i++)
            {
                walkable[i] = new GridWalkable { Value = 1 };
                roads[i] = new GridRoad { Value = 0 };
                sidewalks[i] = new GridRoadSidewalk { Value = 0 };
                dirtRoads[i] = new GridRoadDirt { Value = 0 };
            }

            if (authoring.BlockedCells != null)
            {
                foreach (var v in authoring.BlockedCells)
                {
                    if ((uint)v.x >= (uint)authoring.Width || (uint)v.y >= (uint)authoring.Height)
                        continue;

                    int index = v.x + v.y * authoring.Width;
                    walkable[index] = new GridWalkable { Value = 0 };
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (DrawWhenNotSelected)
            DrawGizmosInternal();
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmosInternal();
    }

    private void DrawGizmosInternal()
    {
        if (!DrawGrid || Width <= 0 || Height <= 0 || CellSize <= 0f)
            return;
        if (Application.isPlaying && !DrawRuntimeDebugInPlayMode)
            return;

        Gizmos.matrix = transform.localToWorldMatrix;

        int gridLineStride = GetDebugStride(Mathf.Max(Width, Height), Mathf.Max(1, MaxGridLinesPerAxis));
        int filledCellStride = GetDebugStride(Width * Height, Mathf.Max(1, MaxFilledDebugCells));

        Gizmos.color = GridLineColor;
        for (int x = 0; x <= Width; x += gridLineStride)
        {
            float fx = x * CellSize;
            Gizmos.DrawLine(new Vector3(fx, 0f, 0f), new Vector3(fx, 0f, Height * CellSize));
        }
        if (Width % gridLineStride != 0)
        {
            float fx = Width * CellSize;
            Gizmos.DrawLine(new Vector3(fx, 0f, 0f), new Vector3(fx, 0f, Height * CellSize));
        }

        for (int y = 0; y <= Height; y += gridLineStride)
        {
            float fy = y * CellSize;
            Gizmos.DrawLine(new Vector3(0f, 0f, fy), new Vector3(Width * CellSize, 0f, fy));
        }
        if (Height % gridLineStride != 0)
        {
            float fy = Height * CellSize;
            Gizmos.DrawLine(new Vector3(0f, 0f, fy), new Vector3(Width * CellSize, 0f, fy));
        }

        if (FillWalkableCells)
        {
            Gizmos.color = WalkableFillColor;
            for (int y = 0; y < Height; y += filledCellStride)
            {
                for (int x = 0; x < Width; x += filledCellStride)
                {
                    var center = new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize);
                    float debugCellSize = CellSize * Mathf.Max(1f, filledCellStride);
                    Gizmos.DrawCube(center, new Vector3(debugCellSize, 0.01f, debugCellSize));
                }
            }
        }

        if (FillRoadCells && TryGetRuntimeRoadBuffer(out var roads, out int roadWidth, out int roadHeight))
        {
            Gizmos.color = RoadFillColor;
            int maxX = Mathf.Min(Width, roadWidth);
            int maxY = Mathf.Min(Height, roadHeight);
            float roadMarkerSize = Mathf.Clamp(RoadCellDebugScale, 0.05f, 1f) * CellSize;
            for (int y = 0; y < maxY; y += filledCellStride)
            {
                for (int x = 0; x < maxX; x += filledCellStride)
                {
                    int index = x + y * roadWidth;
                    if (roads[index].Value == 0)
                        continue;

                    var center = new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize);
                    Gizmos.DrawCube(center, new Vector3(roadMarkerSize, 0.03f, roadMarkerSize));
                }
            }
        }

        if (FillSidewalkCells && TryGetRuntimeSidewalkRoadBuffer(out var sidewalks, out int sidewalkWidth, out int sidewalkHeight))
        {
            Gizmos.color = SidewalkFillColor;
            int maxX = Mathf.Min(Width, sidewalkWidth);
            int maxY = Mathf.Min(Height, sidewalkHeight);
            float roadMarkerSize = Mathf.Clamp(RoadCellDebugScale, 0.05f, 1f) * CellSize;
            for (int y = 0; y < maxY; y += filledCellStride)
            {
                for (int x = 0; x < maxX; x += filledCellStride)
                {
                    int index = x + y * sidewalkWidth;
                    if (sidewalks[index].Value == 0)
                        continue;

                    var center = new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize);
                    Gizmos.DrawCube(center, new Vector3(roadMarkerSize, 0.035f, roadMarkerSize));
                }
            }
        }

        if (FillBuildingCells && TryGetRuntimeBuildingBlockers(out var blocked, out int blockerWidth, out int blockerHeight))
        {
            RuntimeGridBlockerSystem blockerManager = RuntimeGridBlockerSystem.Instance;
            Gizmos.color = BuildingFillColor;
            int maxX = Mathf.Min(Width, blockerWidth);
            int maxY = Mathf.Min(Height, blockerHeight);
            for (int y = 0; y < maxY; y += filledCellStride)
            {
                for (int x = 0; x < maxX; x += filledCellStride)
                {
                    int index = x + y * blockerWidth;
                    if (!blocked.IsSet(index))
                        continue;
                    if (blockerManager != null && blockerManager.IsRuntimeBlockerCell(x, y, blockerWidth, blockerHeight))
                        continue;

                    var center = new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize);
                    Gizmos.DrawCube(center, new Vector3(CellSize, 0.04f, CellSize));
                }
            }
        }

        if (FillRuntimeBlockerCells && RuntimeGridBlockerSystem.Instance != null)
        {
            RuntimeGridBlockerSystem blockerManager = RuntimeGridBlockerSystem.Instance;
            Gizmos.color = RuntimeBlockerFillColor;
            for (int y = 0; y < Height; y += filledCellStride)
            {
                for (int x = 0; x < Width; x += filledCellStride)
                {
                    if (!blockerManager.IsRuntimeBlockerCell(x, y, Width, Height))
                        continue;

                    var center = new Vector3((x + 0.5f) * CellSize, 0f, (y + 0.5f) * CellSize);
                    Gizmos.DrawCube(center, new Vector3(CellSize, 0.045f, CellSize));
                }
            }
        }

        if (FillVehicleFootprintCells && TryGetRuntimeVehicleFootprints(out List<int2> vehicleCells))
        {
            Gizmos.color = VehicleFootprintFillColor;
            for (int i = 0; i < vehicleCells.Count; i++)
            {
                int2 cell = vehicleCells[i];
                if ((uint)cell.x >= (uint)Width || (uint)cell.y >= (uint)Height)
                    continue;

                var center = new Vector3((cell.x + 0.5f) * CellSize, 0f, (cell.y + 0.5f) * CellSize);
                Gizmos.DrawCube(center, new Vector3(CellSize, 0.06f, CellSize));
            }
        }

        if (DrawUnitPaths)
            DrawRuntimeUnitPaths();

        if (BlockedCells == null)
            return;

        Gizmos.color = BlockedFillColor;
        foreach (var v in BlockedCells)
        {
            var center = new Vector3((v.x + 0.5f) * CellSize, 0f, (v.y + 0.5f) * CellSize);
            Gizmos.DrawCube(center, new Vector3(CellSize, 0.05f, CellSize));
        }
    }

    private static bool TryGetRuntimeRoadBuffer(out DynamicBuffer<GridRoad> roads, out int width, out int height)
    {
        roads = default;
        width = 0;
        height = 0;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        var entityManager = world.EntityManager;
        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(), ComponentType.ReadOnly<GridRoad>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = query.GetSingletonEntity();
        var grid = entityManager.GetComponentData<GridConfig>(gridEntity);
        roads = entityManager.GetBuffer<GridRoad>(gridEntity);
        width = grid.Width;
        height = grid.Height;
        return true;
    }

    private static int GetDebugStride(int itemCount, int maxItems)
    {
        if (itemCount <= maxItems)
            return 1;

        return Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(itemCount / (float)maxItems)));
    }

    private static bool TryGetRuntimeSidewalkRoadBuffer(out DynamicBuffer<GridRoadSidewalk> sidewalks, out int width, out int height)
    {
        sidewalks = default;
        width = 0;
        height = 0;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        var entityManager = world.EntityManager;
        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(), ComponentType.ReadOnly<GridRoadSidewalk>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = query.GetSingletonEntity();
        var grid = entityManager.GetComponentData<GridConfig>(gridEntity);
        sidewalks = entityManager.GetBuffer<GridRoadSidewalk>(gridEntity);
        width = grid.Width;
        height = grid.Height;
        return true;
    }

    private static bool TryGetRuntimeBuildingBlockers(out NativeBitArray blocked, out int width, out int height)
    {
        blocked = default;
        width = 0;
        height = 0;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        var entityManager = world.EntityManager;
        var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(), ComponentType.ReadOnly<DynamicBlockerData>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        Entity gridEntity = query.GetSingletonEntity();
        var grid = entityManager.GetComponentData<GridConfig>(gridEntity);
        var blockerData = entityManager.GetComponentData<DynamicBlockerData>(gridEntity);
        if (!blockerData.Blocked.IsCreated)
            return false;

        blocked = blockerData.Blocked;
        width = grid.Width;
        height = grid.Height;
        return true;
    }

    private static bool TryGetRuntimeVehicleFootprints(out List<int2> cells)
    {
        cells = null;

        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return false;

        var entityManager = world.EntityManager;
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitFootprint>(),
            ComponentType.ReadOnly<UnitMovementBehavior>());
        if (query.IsEmptyIgnoreFilter)
            return false;

        using var entities = query.ToEntityArray(Allocator.Temp);
        cells = new List<int2>();
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entityManager.HasComponent<Prefab>(entity) || entityManager.HasComponent<StaticGridBlocker>(entity))
                continue;

            UnitMovementBehavior behavior = entityManager.GetComponentData<UnitMovementBehavior>(entity);
            if (behavior.AllowIdleWander != 0)
                continue;

            int2 centerCell = entityManager.GetComponentData<UnitGrid>(entity).Cell;
            int2 size = entityManager.GetComponentData<UnitFootprint>(entity).Size;
            int2 min = UnitFootprintUtility.GetMinCell(centerCell, size);
            int2 max = min + UnitFootprintUtility.ClampSize(size);
            for (int y = min.y; y < max.y; y++)
            {
                for (int x = min.x; x < max.x; x++)
                    cells.Add(new int2(x, y));
            }
        }

        return cells.Count > 0;
    }

    private void DrawRuntimeUnitPaths()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
            return;

        var entityManager = world.EntityManager;
        var gridQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>(), ComponentType.ReadOnly<PathPoolData>());
        if (gridQuery.IsEmptyIgnoreFilter)
            return;

        Entity gridEntity = gridQuery.GetSingletonEntity();
        GridConfig grid = entityManager.GetComponentData<GridConfig>(gridEntity);
        var pathPool = entityManager.GetComponentData<PathPoolData>(gridEntity).Cells.AsArray();

        var unitQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitTarget>(),
            ComponentType.ReadOnly<ManualMoveOrderTag>());
        if (unitQuery.IsEmptyIgnoreFilter)
            return;

        using var entities = unitQuery.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entityManager.HasComponent<Prefab>(entity) || entityManager.HasComponent<StaticGridBlocker>(entity))
                continue;

            UnitGrid unitGrid = entityManager.GetComponentData<UnitGrid>(entity);
            UnitTarget target = entityManager.GetComponentData<UnitTarget>(entity);
            UnitVehicleKinematics kinematics = entityManager.HasComponent<UnitVehicleKinematics>(entity)
                ? entityManager.GetComponentData<UnitVehicleKinematics>(entity)
                : default;
            bool stuck = kinematics.StallSeconds >= 0.2f ||
                         (!entityManager.HasComponent<UnitPathFollow>(entity) && !entityManager.HasComponent<UnitPathRequest>(entity));

            Gizmos.color = stuck ? StuckUnitPathColor : UnitPathColor;

            Vector3 current = transform.InverseTransformPoint(GridUtils.CellToWorldCenter(grid, unitGrid.Cell));
            Vector3 targetPos = transform.InverseTransformPoint(GridUtils.CellToWorldCenter(grid, target.Cell));

            if (entityManager.HasComponent<UnitPathFollow>(entity) && entityManager.HasComponent<UnitPathRange>(entity))
            {
                UnitPathFollow follow = entityManager.GetComponentData<UnitPathFollow>(entity);
                UnitPathRange range = entityManager.GetComponentData<UnitPathRange>(entity);
                int startIndex = math.clamp(follow.PathIndex, 0, range.Length);

                for (int pathIndex = startIndex; pathIndex < range.Length; pathIndex++)
                {
                    int poolIndex = range.Start + pathIndex;
                    if ((uint)poolIndex >= (uint)pathPool.Length)
                        break;

                    Vector3 next = transform.InverseTransformPoint(GridUtils.CellToWorldCenter(grid, pathPool[poolIndex]));
                    Gizmos.DrawLine(current + Vector3.up * 0.2f, next + Vector3.up * 0.2f);
                    Gizmos.DrawSphere(next + Vector3.up * 0.2f, CellSize * 0.08f);
                    current = next;
                }
            }

            Gizmos.DrawSphere(targetPos + Vector3.up * 0.24f, CellSize * 0.12f);
        }
    }
}
