using UnityEngine;

namespace Game.Runtime
{
    using PlacementState = BuildingPlacementLifecycleCompositionSystemHelper.PlacementState;
    internal sealed partial class BuildingBarrierUtilitySystemHelper
    {
        public bool ShouldAlignGateToNearbyWall(Context context, Vector2Int originCell, BuildingDefinition definition, out bool vertical)
        {
            vertical = false;
            return false; // Road barriers align to the carriageway, never to nearby walls.
        }

        public bool ResolvePlacementRotateVertical(Context context, BuildingPlacementInputUiSystemHelper inputSystem, PlacementState placement)
        {
            if (placement?.Definition == null)
                return false;

            if (IsLinearWallDefinition(placement.Definition))
                return inputSystem != null && inputSystem.IsWallPlacementVertical(placement);

            if (!placement.ManualRotation && !placement.LastRoadAlignmentOrigin.HasValue && ShouldAlignGateToNearbyWall(context, placement.OriginCell, placement.Definition, out bool gateVertical))
                return gateVertical;

            return placement.AutoRotateVertical;
        }

    }
}
