using System.Collections.Generic;
using Game.Components;
using UnityEngine;

namespace Game.Runtime
{
    using CommitContext = BuildingPlacementCommitCompositionSystemHelper.CommitContext;
    using CommitOutcome = BuildingPlacementCommitCompositionSystemHelper.CommitOutcome;
    using CommitRequest = BuildingPlacementCommitCompositionSystemHelper.CommitRequest;
    using WallRun = BuildingPlacementCommitCompositionSystemHelper.WallRun;

    internal static class BuildingWallPlacementCommitTransaction
    {
        public static CommitOutcome Commit(CommitRequest request, CommitContext context)
        {
            if (context.CreateVisual == null ||
                context.PositionVisual == null ||
                context.GetWallSegmentFootprint == null ||
                context.RollbackRuntimeBuilding == null ||
                context.DestroyRuntimeObject == null)
            {
                return default;
            }

            var runs = new List<WallRun>();
            BuildFinalRuns(request, runs);
            int expectedInstanceCount = CountInstances(runs);
            if (expectedInstanceCount == 0)
                return default;

            RuntimeBuildingEntity autoSelectBuilding = null;
            var committedBuildings = new List<RuntimeBuildingEntity>(expectedInstanceCount);
            for (int runIndex = 0; runIndex < runs.Count; runIndex++)
            {
                WallRun run = runs[runIndex];
                if (run.Origins == null || run.Origins.Count == 0)
                    continue;

                Vector2Int footprint = context.GetWallSegmentFootprint(request.Definition, run.Vertical);
                for (int originIndex = 0; originIndex < run.Origins.Count; originIndex++)
                {
                    GameObject instance = context.CreateVisual(request.Definition, context.BuildingRoot);
                    if (instance == null)
                        return Rollback(context, committedBuildings, expectedInstanceCount);

                    Vector2Int origin = run.Origins[originIndex];
                    context.PositionVisual(instance, origin, request.Definition, context.Grid, run.Vertical);
                    BuildingDefinition definition =
                        context.CloneDefinitionWithFootprint(request.Definition, footprint);
                    RuntimeBuildingEntity building =
                        context.RegisterRuntimeBuilding(definition, instance, origin, true);
                    if (building == null)
                    {
                        context.DestroyRuntimeObject(instance);
                        return Rollback(context, committedBuildings, expectedInstanceCount);
                    }

                    committedBuildings.Add(building);
                    if (BuildingPlacementCommitCompositionSystemHelper.ShouldAutoSelectAfterPlacement(
                            building.Definition))
                    {
                        autoSelectBuilding = building;
                    }
                }
            }

            if (request.PreviewInstance != null)
                context.DestroyRuntimeObject(request.PreviewInstance);

            return new CommitOutcome(autoSelectBuilding, committedBuildings.Count, expectedInstanceCount);
        }

        private static CommitOutcome Rollback(
            CommitContext context,
            List<RuntimeBuildingEntity> committedBuildings,
            int expectedInstanceCount)
        {
            int rollbackFailureCount = 0;
            for (int i = committedBuildings.Count - 1; i >= 0; i--)
            {
                RuntimeBuildingEntity building = committedBuildings[i];
                if (building == null || !context.RollbackRuntimeBuilding(building))
                {
                    rollbackFailureCount++;
                    continue;
                }

                if (building.Instance != null)
                    context.DestroyRuntimeObject(building.Instance);
            }

            return new CommitOutcome(null, rollbackFailureCount, expectedInstanceCount);
        }

        private static int CountInstances(List<WallRun> runs)
        {
            int count = 0;
            for (int i = 0; i < runs.Count; i++)
                count += runs[i].Origins?.Count ?? 0;
            return count;
        }

        private static void BuildFinalRuns(CommitRequest request, List<WallRun> runs)
        {
            if (request.CommittedWallRuns != null)
            {
                for (int i = 0; i < request.CommittedWallRuns.Count; i++)
                {
                    WallRun run = request.CommittedWallRuns[i];
                    if (run.Origins != null && run.Origins.Count > 0)
                        runs.Add(run);
                }
            }

            if (!request.HideCurrentWallPreview &&
                request.CurrentWallOrigins != null &&
                request.CurrentWallOrigins.Count > 0)
            {
                runs.Add(new WallRun(request.CurrentWallOrigins, request.CurrentWallVertical));
            }
        }
    }
}
