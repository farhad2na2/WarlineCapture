using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Composition;

namespace Game.Editor
{
    public sealed class MapSurfaceBridgeBakeSystem
    {
        private readonly MapSurfaceRoadPriorityPolicy _roadPrioritySystem = new();

        public MapSurfaceMeshBakeSource CreateBridgeDeckSource(
            Mesh bridgeDeckMesh,
            Matrix4x4 localToWorld,
            MapSurfaceMovementMask movementMask,
            int layerId)
        {
            return new MapSurfaceMeshBakeSource(
                bridgeDeckMesh,
                localToWorld,
                MapSurfaceType.BridgeDeck,
                _roadPrioritySystem.NormalizeFlagsForSurfaceType(MapSurfaceType.BridgeDeck, MapSurfaceFlags.None),
                movementMask,
                math.max(1, layerId));
        }

        public MapSurfaceMeshBakeSource CreateBridgeApproachSource(
            Mesh approachMesh,
            Matrix4x4 localToWorld,
            MapSurfaceMovementMask movementMask,
            int layerId)
        {
            return new MapSurfaceMeshBakeSource(
                approachMesh,
                localToWorld,
                MapSurfaceType.Ramp,
                _roadPrioritySystem.NormalizeFlagsForSurfaceType(MapSurfaceType.Ramp, MapSurfaceFlags.None),
                movementMask,
                math.max(0, layerId));
        }

        public MapSurfaceMeshBakeSource CreateLowerPassThroughSource(
            Mesh lowerSurfaceMesh,
            Matrix4x4 localToWorld,
            MapSurfaceType lowerSurfaceType,
            MapSurfaceMovementMask movementMask,
            int layerId)
        {
            MapSurfaceType normalizedType = lowerSurfaceType == MapSurfaceType.Highway
                ? MapSurfaceType.Highway
                : MapSurfaceType.Road;

            return new MapSurfaceMeshBakeSource(
                lowerSurfaceMesh,
                localToWorld,
                normalizedType,
                _roadPrioritySystem.NormalizeFlagsForSurfaceType(normalizedType, MapSurfaceFlags.None),
                movementMask,
                math.max(0, layerId));
        }
    }
}
