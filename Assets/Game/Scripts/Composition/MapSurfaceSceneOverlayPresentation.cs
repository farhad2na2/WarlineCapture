using System.Collections.Generic;
using Game.Authoring;
using Game.Components;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Composition
{
    public static class MapSurfaceSceneOverlayPresentation
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

            MapSurfaceSceneOverlayAuthoringData[] authoredOverlays = authoring.SceneOverlays;
            if (authoredOverlays.Length == 0)
                authoredOverlays = Capture(authoring);

            for (int i = 0; i < authoredOverlays.Length; i++)
                overlays.Add(authoredOverlays[i].ToRuntimeOverlay());

            uint revision = 1;
            if (entityManager.HasComponent<MapSurfaceSceneOverlayRevision>(surfaceEntity))
            {
                revision = entityManager.GetComponentData<MapSurfaceSceneOverlayRevision>(surfaceEntity).Value + 1;
                if (revision == 0)
                    revision = 1;
            }
            else
            {
                entityManager.AddComponent<MapSurfaceSceneOverlayRevision>(surfaceEntity);
            }
            entityManager.SetComponentData(
                surfaceEntity,
                new MapSurfaceSceneOverlayRevision { Value = revision });
        }

        public static MapSurfaceSceneOverlayAuthoringData[] Capture(MapSurfaceAuthoring authoring)
        {
            if (authoring == null)
                return System.Array.Empty<MapSurfaceSceneOverlayAuthoringData>();

            return Capture(authoring.transform);
        }

        public static MapSurfaceSceneOverlayAuthoringData[] Capture(OperationMapSceneView view)
        {
            if (view == null || !view.gameObject.scene.IsValid())
                return System.Array.Empty<MapSurfaceSceneOverlayAuthoringData>();

            var overlays = new List<MapSurfaceSceneOverlayAuthoringData>();
            GameObject[] roots = view.gameObject.scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                Append(roots[rootIndex].transform, overlays);
            return overlays.ToArray();
        }

        private static MapSurfaceSceneOverlayAuthoringData[] Capture(Transform root)
        {
            if (root == null)
                return System.Array.Empty<MapSurfaceSceneOverlayAuthoringData>();

            var overlays = new List<MapSurfaceSceneOverlayAuthoringData>();
            Append(root, overlays);
            return overlays.ToArray();
        }

        private static void Append(
            Transform root,
            List<MapSurfaceSceneOverlayAuthoringData> overlays)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int filterIndex = 0; filterIndex < filters.Length; filterIndex++)
            {
                MeshFilter filter = filters[filterIndex];
                if (filter == null || filter.sharedMesh == null)
                    continue;
                MapBakeGroupAuthoring group =
                    filter.GetComponentInParent<MapBakeGroupAuthoring>(true);
                if (!TryResolveSettings(
                        group,
                        filter.transform,
                        out MapSurfaceType type,
                        out MapSurfaceFlags flags,
                        out MapSurfaceMovementMask mask,
                        out int layerId))
                    continue;

                Renderer renderer = filter.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                Bounds bounds = renderer.bounds;
                if (bounds.extents.x <= 0.01f || bounds.extents.z <= 0.01f)
                    continue;

                overlays.Add(new MapSurfaceSceneOverlayAuthoringData
                {
                    Center = bounds.center,
                    Rotation = Quaternion.identity,
                    HalfExtents = new Vector2(
                        bounds.extents.x + SceneOverlayPadding,
                        bounds.extents.z + SceneOverlayPadding),
                    Height = bounds.max.y,
                    Normal = Vector3.up,
                    SurfaceType = type,
                    MovementMask = mask,
                    Flags = flags,
                    LayerId = layerId
                });
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
    }
}
