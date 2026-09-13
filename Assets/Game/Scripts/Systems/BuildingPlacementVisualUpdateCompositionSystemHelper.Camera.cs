using System.Collections.Generic;
using UnityEngine;
using Game.Components;
namespace Game.Runtime
{
    using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;
    internal sealed partial class BuildingPlacementVisualUpdateCompositionSystemHelper
    {
        internal void FocusActivePlacement(Context context, PlacementState placement)
        {
            if (placement != null &&
                context.TryGetGridData(out _, out GridConfig grid, out _, out _))
            {
                context.DependencySystem.SmoothMoveCameraGroundCenterTo(
                    ResolveCurrentPlacementFocusWorldPosition(context, placement, grid));
            }
        }

        internal Vector3 ResolveCurrentPlacementFocusWorldPosition(Context context, PlacementState placement, GridConfig grid)
        {
            if (placement == null)
                return Vector3.zero;

            if (BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(placement.Definition))
            {
                bool vertical = context.InputSystem.IsWallPlacementVertical(placement);
                Vector2Int wallFootprint = BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint(placement.Definition, vertical);
                IReadOnlyList<Vector2Int> currentOrigins = context.InputSystem.BuildWallPlacementOriginsScratch(placement, BuildingPlacementCommitCompositionSystemHelper.GetWallSegmentFootprint);
                IReadOnlyList<Vector2Int> allOrigins = context.InputSystem.GetAllWallPlacementOriginsScratch(placement, currentOrigins);
                return context.GridSystem.ResolvePlacementFocusWorldPosition(
                    placement,
                    allOrigins,
                    grid,
                    wallFootprint,
                    context.StartupSystem.BuildPlaneY);
            }

            bool rotateVertical = context.BarrierSystem.ResolvePlacementRotateVertical(
                context.CreateBuildingBarrierContext(),
                context.InputSystem,
                placement);
            Vector2Int footprint = context.GetPlacementFootprint(placement.Definition, rotateVertical);
            return context.GetFootprintCenter(placement.OriginCell, footprint, grid);
        }
    }
}
