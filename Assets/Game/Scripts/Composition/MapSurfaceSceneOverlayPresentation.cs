using Game.Authoring;
using Game.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Composition
{
    internal static class MapSurfaceSceneOverlayPresentation
    {
        private const float SceneOverlayPadding = 0.1f;

        public static void Publish(
            MapSurfaceAuthoring authoring,
            EntityManager entityManager,
            Entity surfaceEntity)
        {
            if (authoring == null || surfaceEntity == Entity.Null || !entityManager.Exists(surfaceEntity))
                return;

            if (!entityManager.HasBuffer<MapSurfaceSceneOverlay>(surfaceEntity))
                entityManager.AddBuffer<MapSurfaceSceneOverlay>(surfaceEntity);

            DynamicBuffer<MapSurfaceSceneOverlay> overlays = entityManager.GetBuffer<MapSurfaceSceneOverlay>(surfaceEntity);
            overlays.Clear();

            MapBakeGroupAuthoring[] groups = authoring.GetComponentsInChildren<MapBakeGroupAuthoring>(true);
            for (int i = 0; i < groups.Length; i++)
            {
                MapBakeGroupAuthoring group = groups[i];
                if (group == null)
                    continue;

                MeshFilter[] filters = group.GetComponentsInChildren<MeshFilter>(group.IncludeInactiveChildren);
                for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
                {
                    MeshFilter filter = filters[filterIndex];
                    if (filter == null || filter.sharedMesh == null || !IsOwnedByGroup(filter, group))
                        continue;
                    if (!TryResolveSettings(group, filter.transform, out MapSurfaceType type, out MapSurfaceFlags flags, out MapSurfaceMovementMask mask, out int layerId))
                        continue;

                    Renderer renderer = filter.GetComponent<Renderer>();
                    if (renderer == null)
                        continue;

                    Bounds bounds = renderer.bounds;
                    if (bounds.extents.x <= 0.01f || bounds.extents.z <= 0.01f)
                        continue;

                    overlays.Add(new MapSurfaceSceneOverlay
                    {
                        Center = bounds.center,
                        Rotation = quaternion.identity,
                        HalfExtents = new float2(
                            bounds.extents.x + SceneOverlayPadding,
                            bounds.extents.z + SceneOverlayPadding),
                        Height = bounds.max.y,
                        Normal = math.up(),
                        SurfaceType = type,
                        MovementMask = mask,
                        Flags = flags,
                        LayerId = layerId
                    });
                }
            }
        }

        private static bool TryResolveSettings(
            MapBakeGroupAuthoring group,
            Transform surfaceTransform,
            out MapSurfaceType type,
            out MapSurfaceFlags flags,
            out MapSurfaceMovementMask movementMask,
            out int layerId)
        {
            type = MapSurfaceType.Terrain;
            flags = MapSurfaceFlags.None;
            movementMask = group != null && group.MovementMask != MapSurfaceMovementMask.None
                ? group.MovementMask
                : MapSurfaceMovementMask.AllGroundUnits;
            layerId = group != null ? group.LayerId : 0;

            if (group != null)
            {
                switch (group.Role)
                {
                    case MapBakeGroupRole.Road:
                        type = MapSurfaceType.Road;
                        flags = MapSurfaceFlags.Road;
                        return true;
                    case MapBakeGroupRole.Bridge:
                        type = MapSurfaceType.BridgeDeck;
                        flags = MapSurfaceFlags.Road | MapSurfaceFlags.Bridge;
                        layerId = Mathf.Max(1, layerId);
                        return true;
                    case MapBakeGroupRole.Ramp:
                        type = MapSurfaceType.Ramp;
                        flags = MapSurfaceFlags.Road | MapSurfaceFlags.Ramp;
                        return true;
                }
            }

            string name = surfaceTransform != null ? surfaceTransform.name : string.Empty;
            if (name.IndexOf("Runway", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Highway", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                type = MapSurfaceType.Road;
                flags = MapSurfaceFlags.Road;
                return true;
            }

            return false;
        }

        private static bool IsOwnedByGroup(MeshFilter filter, MapBakeGroupAuthoring ownerGroup)
        {
            if (filter == null || ownerGroup == null)
                return false;

            return filter.GetComponentInParent<MapBakeGroupAuthoring>(true) == ownerGroup;
        }
    }
}
