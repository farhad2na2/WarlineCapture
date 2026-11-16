using UnityEngine;
namespace Game.Runtime {
    // Pure value initialization shared by native preview presentation and source hosts.
    // It grants no placement validity, touch reachability, or building birth.
    public readonly struct BuildingPlacementInitialPreviewState {
        public readonly Vector2Int OriginCell, CommittedOriginCell, DragStartOriginCell, DragCurrentOriginCell;
        public readonly bool AutoRotateVertical, HideCurrentWallPreview;
        private BuildingPlacementInitialPreviewState(Vector2Int origin) {
            OriginCell=origin;CommittedOriginCell=origin;DragStartOriginCell=origin;DragCurrentOriginCell=origin;
            AutoRotateVertical=false;HideCurrentWallPreview=false;
        }
        public static Vector2Int CenterCellToOrigin(Vector2Int center, Vector2Int footprint)=>new Vector2Int(center.x-Mathf.FloorToInt(footprint.x*0.5f),center.y-Mathf.FloorToInt(footprint.y*0.5f));
        public static BuildingPlacementInitialPreviewState FromResolvedOrigin(Vector2Int origin)=>new BuildingPlacementInitialPreviewState(origin);
        // Native ApplyPointerHover preserves the drag start and updates these three values.
        public static int HoverAxis(Vector2Int dragStart, Vector2Int origin) {
            var delta = origin - dragStart;
            if (delta.x == 0 && delta.y == 0) return 0;
            return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? 1 : 2;
        }
    }
}
