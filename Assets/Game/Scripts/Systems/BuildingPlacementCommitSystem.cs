using System.Collections.Generic;
using UnityEngine;
using RuntimeBuildingData = BuildingPlacementSystem.RuntimeBuildingData;

internal sealed class BuildingPlacementCommitSystem
{
    public readonly struct WallRun
    {
        public readonly IReadOnlyList<Vector2Int> Origins;
        public readonly bool Vertical;

        public WallRun(IReadOnlyList<Vector2Int> origins, bool vertical)
        {
            Origins = origins;
            Vertical = vertical;
        }
    }

    public readonly struct CommitRequest
    {
        public readonly BuildingPlacementSystem.BuildingDefinition Definition;
        public readonly GameObject PreviewInstance;
        public readonly Vector2Int OriginCell;
        public readonly bool AutoRotateVertical;
        public readonly bool IsWall;
        public readonly bool HideCurrentWallPreview;
        public readonly IReadOnlyList<WallRun> CommittedWallRuns;
        public readonly IReadOnlyList<Vector2Int> CurrentWallOrigins;
        public readonly bool CurrentWallVertical;

        public CommitRequest(
            BuildingPlacementSystem.BuildingDefinition definition,
            GameObject previewInstance,
            Vector2Int originCell,
            bool autoRotateVertical,
            bool isWall,
            bool hideCurrentWallPreview,
            IReadOnlyList<WallRun> committedWallRuns,
            IReadOnlyList<Vector2Int> currentWallOrigins,
            bool currentWallVertical)
        {
            Definition = definition;
            PreviewInstance = previewInstance;
            OriginCell = originCell;
            AutoRotateVertical = autoRotateVertical;
            IsWall = isWall;
            HideCurrentWallPreview = hideCurrentWallPreview;
            CommittedWallRuns = committedWallRuns;
            CurrentWallOrigins = currentWallOrigins;
            CurrentWallVertical = currentWallVertical;
        }
    }

    public readonly struct CommitContext
    {
        public readonly Transform BuildingRoot;
        public readonly bool HasGrid;
        public readonly GridConfig Grid;
        public readonly CreateVisualDelegate CreateVisual;
        public readonly PositionVisualDelegate PositionVisual;
        public readonly RegisterRuntimeBuildingDelegate RegisterRuntimeBuilding;
        public readonly CloneDefinitionWithFootprintDelegate CloneDefinitionWithFootprint;
        public readonly GetPlacementFootprintDelegate GetPlacementFootprint;
        public readonly GetWallSegmentFootprintDelegate GetWallSegmentFootprint;
        public readonly DestroyRuntimeObjectDelegate DestroyRuntimeObject;

        public CommitContext(
            Transform buildingRoot,
            bool hasGrid,
            GridConfig grid,
            CreateVisualDelegate createVisual,
            PositionVisualDelegate positionVisual,
            RegisterRuntimeBuildingDelegate registerRuntimeBuilding,
            CloneDefinitionWithFootprintDelegate cloneDefinitionWithFootprint,
            GetPlacementFootprintDelegate getPlacementFootprint,
            GetWallSegmentFootprintDelegate getWallSegmentFootprint,
            DestroyRuntimeObjectDelegate destroyRuntimeObject)
        {
            BuildingRoot = buildingRoot;
            HasGrid = hasGrid;
            Grid = grid;
            CreateVisual = createVisual;
            PositionVisual = positionVisual;
            RegisterRuntimeBuilding = registerRuntimeBuilding;
            CloneDefinitionWithFootprint = cloneDefinitionWithFootprint;
            GetPlacementFootprint = getPlacementFootprint;
            GetWallSegmentFootprint = getWallSegmentFootprint;
            DestroyRuntimeObject = destroyRuntimeObject;
        }
    }

    public delegate GameObject CreateVisualDelegate(BuildingPlacementSystem.BuildingDefinition definition, Transform parent);
    public delegate void PositionVisualDelegate(GameObject instance, Vector2Int originCell, BuildingPlacementSystem.BuildingDefinition definition, GridConfig grid, bool rotateVertical);
    public delegate RuntimeBuildingData RegisterRuntimeBuildingDelegate(BuildingPlacementSystem.BuildingDefinition definition, GameObject instance, Vector2Int originCell, bool removeOverlappingBlockers);
    public delegate BuildingPlacementSystem.BuildingDefinition CloneDefinitionWithFootprintDelegate(BuildingPlacementSystem.BuildingDefinition definition, Vector2Int footprintCells);
    public delegate Vector2Int GetPlacementFootprintDelegate(BuildingPlacementSystem.BuildingDefinition definition, bool rotateVertical);
    public delegate Vector2Int GetWallSegmentFootprintDelegate(BuildingPlacementSystem.BuildingDefinition definition, bool vertical);
    public delegate void DestroyRuntimeObjectDelegate(UnityEngine.Object target);

    public RuntimeBuildingData CommitPlacement(CommitRequest request, CommitContext context)
    {
        if (request.Definition == null || context.RegisterRuntimeBuilding == null || context.CloneDefinitionWithFootprint == null)
            return null;

        if (request.IsWall && context.HasGrid)
            return CommitWallPlacement(request, context);

        return CommitSinglePlacement(request, context);
    }

    private RuntimeBuildingData CommitWallPlacement(CommitRequest request, CommitContext context)
    {
        RuntimeBuildingData lastBuilding = null;
        var wallRuns = new List<WallRun>();
        BuildFinalWallRuns(request, wallRuns);
        for (int runIndex = 0; runIndex < wallRuns.Count; runIndex++)
        {
            WallRun run = wallRuns[runIndex];
            if (run.Origins == null || run.Origins.Count == 0)
                continue;

            Vector2Int wallFootprint = context.GetWallSegmentFootprint(request.Definition, run.Vertical);
            for (int i = 0; i < run.Origins.Count; i++)
            {
                GameObject instance = context.CreateVisual(request.Definition, context.BuildingRoot);
                if (instance == null)
                    continue;

                context.PositionVisual(instance, run.Origins[i], request.Definition, context.Grid, run.Vertical);
                BuildingPlacementSystem.BuildingDefinition segmentDefinition = context.CloneDefinitionWithFootprint(request.Definition, wallFootprint);
                lastBuilding = context.RegisterRuntimeBuilding(segmentDefinition, instance, run.Origins[i], true);
            }
        }

        if (request.PreviewInstance != null)
            context.DestroyRuntimeObject(request.PreviewInstance);

        return ShouldAutoSelectAfterPlacement(lastBuilding?.Definition) ? lastBuilding : null;
    }

    private RuntimeBuildingData CommitSinglePlacement(CommitRequest request, CommitContext context)
    {
        if (request.PreviewInstance == null)
            return null;

        if (context.HasGrid && context.PositionVisual != null)
            context.PositionVisual(request.PreviewInstance, request.OriginCell, request.Definition, context.Grid, request.AutoRotateVertical);

        Vector2Int footprint = context.GetPlacementFootprint(request.Definition, request.AutoRotateVertical);
        BuildingPlacementSystem.BuildingDefinition committedDefinition = context.CloneDefinitionWithFootprint(request.Definition, footprint);
        RuntimeBuildingData building = context.RegisterRuntimeBuilding(committedDefinition, request.PreviewInstance, request.OriginCell, true);
        return ShouldAutoSelectAfterPlacement(building?.Definition) ? building : null;
    }

    private static void BuildFinalWallRuns(CommitRequest request, List<WallRun> runs)
    {
        if (request.CommittedWallRuns != null)
        {
            for (int i = 0; i < request.CommittedWallRuns.Count; i++)
            {
                WallRun run = request.CommittedWallRuns[i];
                if (run.Origins == null || run.Origins.Count == 0)
                    continue;

                runs.Add(run);
            }
        }

        if (!request.HideCurrentWallPreview && request.CurrentWallOrigins != null && request.CurrentWallOrigins.Count > 0)
            runs.Add(new WallRun(request.CurrentWallOrigins, request.CurrentWallVertical));
    }

    private static bool ShouldAutoSelectAfterPlacement(BuildingPlacementSystem.BuildingDefinition definition)
    {
        if (definition == null)
            return false;

        if (definition.ProductionSlots != null)
        {
            for (int i = 0; i < definition.ProductionSlots.Count; i++)
            {
                if (definition.ProductionSlots[i]?.SpawnUnitPrefab != null)
                    return true;
            }
        }

        return definition.SpawnUnitPrefab != null ||
               definition.SecondarySpawnUnitPrefab != null ||
               definition.TertiarySpawnUnitPrefab != null ||
               definition.QuaternarySpawnUnitPrefab != null;
    }
}
