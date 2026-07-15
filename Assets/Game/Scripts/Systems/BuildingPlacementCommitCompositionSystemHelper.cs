using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingPlacementCommitCompositionSystemHelper
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
            public readonly BuildingDefinition Definition;
            public readonly GameObject PreviewInstance;
            public readonly Vector2Int OriginCell;
            public readonly bool AutoRotateVertical;
            public readonly bool IsWall;
            public readonly bool HideCurrentWallPreview;
            public readonly IReadOnlyList<WallRun> CommittedWallRuns;
            public readonly IReadOnlyList<Vector2Int> CurrentWallOrigins;
            public readonly bool CurrentWallVertical;

            public CommitRequest(
                BuildingDefinition definition,
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
            public readonly RollbackRuntimeBuildingDelegate RollbackRuntimeBuilding;
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
                RollbackRuntimeBuildingDelegate rollbackRuntimeBuilding,
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
                RollbackRuntimeBuilding = rollbackRuntimeBuilding;
                CloneDefinitionWithFootprint = cloneDefinitionWithFootprint;
                GetPlacementFootprint = getPlacementFootprint;
                GetWallSegmentFootprint = getWallSegmentFootprint;
                DestroyRuntimeObject = destroyRuntimeObject;
            }
        }

        public readonly struct CommitOutcome
        {
            public readonly RuntimeBuildingEntity AutoSelectBuilding;
            public readonly int CommittedInstanceCount;
            public readonly int ExpectedInstanceCount;

            public bool PlacementCommitted => CommittedInstanceCount > 0;
            public bool FullyCommitted => ExpectedInstanceCount > 0 && CommittedInstanceCount == ExpectedInstanceCount;

            public CommitOutcome(RuntimeBuildingEntity autoSelectBuilding, int committedInstanceCount)
                : this(autoSelectBuilding, committedInstanceCount, committedInstanceCount)
            {
            }

            public CommitOutcome(RuntimeBuildingEntity autoSelectBuilding, int committedInstanceCount, int expectedInstanceCount)
            {
                AutoSelectBuilding = autoSelectBuilding;
                CommittedInstanceCount = committedInstanceCount;
                ExpectedInstanceCount = expectedInstanceCount;
            }
        }

        public delegate GameObject CreateVisualDelegate(BuildingDefinition definition, Transform parent);
        public delegate void PositionVisualDelegate(GameObject instance, Vector2Int originCell, BuildingDefinition definition, GridConfig grid, bool rotateVertical);
        public delegate RuntimeBuildingEntity RegisterRuntimeBuildingDelegate(BuildingDefinition definition, GameObject instance, Vector2Int originCell, bool removeOverlappingBlockers);
        public delegate bool RollbackRuntimeBuildingDelegate(RuntimeBuildingEntity building);
        public delegate BuildingDefinition CloneDefinitionWithFootprintDelegate(BuildingDefinition definition, Vector2Int footprintCells);
        public delegate Vector2Int GetPlacementFootprintDelegate(BuildingDefinition definition, bool rotateVertical);
        public delegate Vector2Int GetWallSegmentFootprintDelegate(BuildingDefinition definition, bool vertical);
        public delegate void DestroyRuntimeObjectDelegate(UnityEngine.Object target);

        public static List<Vector2Int> BuildWallRunOrigins(Vector2Int start, Vector2Int end, Vector2Int footprint, bool vertical)
        {
            var origins = new List<Vector2Int> { start };
            if (start == end)
                return origins;

            if (vertical)
            {
                int stepCells = Mathf.Max(1, footprint.y);
                int delta = end.y - start.y;
                int direction = delta >= 0 ? 1 : -1;
                int segmentCount = Mathf.Abs(delta) / stepCells;
                for (int i = 1; i <= segmentCount; i++)
                    origins.Add(new Vector2Int(start.x, start.y + direction * stepCells * i));
            }
            else
            {
                int stepCells = Mathf.Max(1, footprint.x);
                int delta = end.x - start.x;
                int direction = delta >= 0 ? 1 : -1;
                int segmentCount = Mathf.Abs(delta) / stepCells;
                for (int i = 1; i <= segmentCount; i++)
                    origins.Add(new Vector2Int(start.x + direction * stepCells * i, start.y));
            }

            return origins;
        }

        public static Vector2Int GetWallSegmentFootprint(BuildingDefinition definition, bool vertical)
        {
            if (definition == null)
                return Vector2Int.one;

            int lengthCells = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(
                Mathf.Abs(definition.LocalBounds.size.x),
                Mathf.Abs(definition.LocalBounds.size.z))));
            int thicknessCells = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(
                Mathf.Abs(definition.LocalBounds.size.x),
                Mathf.Abs(definition.LocalBounds.size.z))));

            Vector2Int footprint = new(lengthCells, thicknessCells);
            return vertical ? new Vector2Int(footprint.y, footprint.x) : footprint;
        }

        public static Quaternion ResolvePlacementWorldRotation(BuildingDefinition definition, bool rotateVertical)
        {
            bool rotateNinety = rotateVertical;
            if (BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(definition) && IsWallLengthAxisLocalZ(definition))
                rotateNinety = !rotateNinety;

            return rotateNinety ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        }

        public CommitOutcome CommitPlacement(CommitRequest request, CommitContext context)
        {
            if (request.Definition == null || context.RegisterRuntimeBuilding == null || context.CloneDefinitionWithFootprint == null)
                return default;

            if (request.IsWall && context.HasGrid)
                return BuildingWallPlacementCommitTransaction.Commit(request, context);

            return CommitSinglePlacement(request, context);
        }

        private CommitOutcome CommitSinglePlacement(CommitRequest request, CommitContext context)
        {
            if (request.PreviewInstance == null || context.GetPlacementFootprint == null)
                return default;

            if (context.HasGrid && context.PositionVisual != null)
                context.PositionVisual(request.PreviewInstance, request.OriginCell, request.Definition, context.Grid, request.AutoRotateVertical);

            Vector2Int footprint = context.GetPlacementFootprint(request.Definition, request.AutoRotateVertical);
            BuildingDefinition committedDefinition = context.CloneDefinitionWithFootprint(request.Definition, footprint);
            RuntimeBuildingEntity building = context.RegisterRuntimeBuilding(committedDefinition, request.PreviewInstance, request.OriginCell, true);
            if (building == null)
                return new CommitOutcome(null, 0, 1);

            RuntimeBuildingEntity autoSelectBuilding = ShouldAutoSelectAfterPlacement(building.Definition) ? building : null;
            return new CommitOutcome(autoSelectBuilding, 1, 1);
        }

        internal static bool ShouldAutoSelectAfterPlacement(BuildingDefinition definition)
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

        private static bool IsWallLengthAxisLocalZ(BuildingDefinition definition)
        {
            if (definition == null || !definition.HasLocalBounds)
                return false;

            return Mathf.Abs(definition.LocalBounds.size.z) > Mathf.Abs(definition.LocalBounds.size.x);
        }
    }
}
