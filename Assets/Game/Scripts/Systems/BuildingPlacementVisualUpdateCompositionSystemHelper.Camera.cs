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
                if(TryFitActualPlacement(context,placement,grid))return;
                context.DependencySystem.FocusPlacementImmediately(
                    ResolveCurrentPlacementFocusWorldPosition(context, placement, grid), placement.IsValid);
            }
        }

        private static bool TryFitActualPlacement(Context context,PlacementState placement,GridConfig grid)
        {
            if(placement?.Definition==null||!placement.Definition.HasLocalBounds||placement.PreviewInstance==null||
                placement.PreviewInstance.transform.childCount==0||BuildingBarrierUtilitySystemHelper.IsLinearWallDefinition(placement.Definition))return false;
            var points=new Vector3[12];var bounds=placement.Definition.LocalBounds;
            var model=placement.PreviewInstance.transform.GetChild(0).localToWorldMatrix;
            for(int i=0;i<8;i++)points[i]=model.MultiplyPoint3x4(bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));
            var footprint=context.GetPlacementFootprint(placement.Definition,placement.AutoRotateVertical);
            float minX=grid.Origin.x+placement.OriginCell.x*grid.CellSize,minZ=grid.Origin.z+placement.OriginCell.y*grid.CellSize;
            for(int i=0;i<4;i++)points[8+i]=new Vector3(minX+((i&1)==0?0:footprint.x*grid.CellSize),context.StartupSystem.BuildPlaneY,minZ+((i&2)==0?0:footprint.y*grid.CellSize));
            bool fit=context.DependencySystem.TryFitPlacementPreview(placement.CameraToken,points,placement.PreviewInstance.transform.rotation,placement.IsValid,out var release);
            if(fit)placement.ReleaseCamera=release;
            return fit;
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
