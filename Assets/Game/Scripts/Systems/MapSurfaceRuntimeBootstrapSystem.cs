using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

internal sealed class MapSurfaceRuntimeBootstrapSystem
{
    private const float SceneOverlayPadding = 0.1f;

    public bool Ensure(World world, MapSurfaceAuthoring authoring)
    {
        if (authoring == null)
            return false;

        bool ensured = Ensure(world, authoring.BakedSurfaceData);
        if (ensured)
            PublishSceneOverlays(world, authoring);
        return ensured;
    }

    public bool Ensure(World world, MapSurfaceDataAsset surfaceData)
    {
        if (world == null || !world.IsCreated || surfaceData == null)
            return false;

        EntityManager entityManager = world.EntityManager;
        if (HasNonOwnedBakedSurface(entityManager, out Entity nonOwnedSurfaceEntity))
        {
            RemoveOwnedRuntimeSurfaces(entityManager, nonOwnedSurfaceEntity);
            return true;
        }

        if (!surfaceData.TryCreateRuntimeBlobAsset(Allocator.Persistent, out BlobAssetReference<MapSurfaceBlob> surfaceBlob))
        {
            Debug.LogWarning("[MapSurfaceRuntimeBootstrap] missingRuntimeSurfaceBlob");
            return false;
        }

        Entity surfaceEntity = ResolveOwnedSurfaceEntity(entityManager);
        DisposeOwnedSurfaceBlob(entityManager, surfaceEntity);

        if (!entityManager.HasComponent<MapSurfaceComponent>(surfaceEntity))
            entityManager.AddComponent<MapSurfaceComponent>(surfaceEntity);

        ref MapSurfaceBlob blob = ref surfaceBlob.Value;
        GetSurfaceFeatureFlags(ref blob, out byte hasLayeredCells, out byte hasRoadSurfaces, out byte hasBridgeSurfaces);
        entityManager.SetComponentData(surfaceEntity, new MapSurfaceComponent
        {
            SurfaceBlob = surfaceBlob,
            GridOrigin = blob.GridOrigin,
            CellSize = blob.CellSize,
            Dimensions = blob.Dimensions,
            HasSurfaceData = 1,
            HasLayeredCells = hasLayeredCells,
            HasRoadSurfaces = hasRoadSurfaces,
            HasBridgeSurfaces = hasBridgeSurfaces
        });

        if (!entityManager.HasComponent<MapSurfacePathCostComponent>(surfaceEntity))
            entityManager.AddComponent<MapSurfacePathCostComponent>(surfaceEntity);

        entityManager.SetComponentData(surfaceEntity, new MapSurfacePathCostComponent
        {
            EnableSlopeCost = 0,
            GentleSlopeTraversalCost = 0,
            SteepSlopeTraversalCost = 0
        });

        if (!entityManager.HasComponent<MapSurfaceRuntimeBakedBlobTag>(surfaceEntity))
            entityManager.AddComponent<MapSurfaceRuntimeBakedBlobTag>(surfaceEntity);
        if (entityManager.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(surfaceEntity))
            entityManager.RemoveComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(surfaceEntity);

        entityManager.SetName(surfaceEntity, "RuntimeBakedMapSurface");
        return true;
    }

    private static void PublishSceneOverlays(World world, MapSurfaceAuthoring authoring)
    {
        if (world == null || !world.IsCreated || authoring == null)
            return;

        EntityManager entityManager = world.EntityManager;
        Entity surfaceEntity = ResolveOwnedSurfaceEntity(entityManager);
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
                if (!TryResolveSceneOverlaySettings(group, filter.transform, out MapSurfaceType type, out MapSurfaceFlags flags, out MapSurfaceMovementMask mask, out int layerId))
                    continue;

                Renderer renderer = filter.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                Bounds bounds = renderer.bounds;
                if (bounds.extents.x <= 0.01f || bounds.extents.z <= 0.01f)
                    continue;

                overlays.Add(new MapSurfaceSceneOverlay
                {
                    Center = new float3(bounds.center.x, bounds.center.y, bounds.center.z),
                    Rotation = quaternion.identity,
                    HalfExtents = new float2(bounds.extents.x + SceneOverlayPadding, bounds.extents.z + SceneOverlayPadding),
                    Height = bounds.max.y,
                    Normal = new float3(0f, 1f, 0f),
                    SurfaceType = type,
                    MovementMask = mask,
                    Flags = flags,
                    LayerId = layerId
                });
            }
        }
    }

    private static bool TryResolveSceneOverlaySettings(
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

        MapBakeGroupAuthoring nearestGroup = filter.GetComponentInParent<MapBakeGroupAuthoring>(true);
        return nearestGroup == ownerGroup;
    }

    public void Dispose(World world)
    {
        if (world == null || !world.IsCreated)
            return;

        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MapSurfaceComponent>(),
            ComponentType.ReadOnly<MapSurfaceRuntimeBakedBlobTag>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            DisposeOwnedSurfaceBlob(entityManager, entity);
            if (entityManager.Exists(entity))
                entityManager.DestroyEntity(entity);
        }
    }

    private static bool HasNonOwnedBakedSurface(EntityManager entityManager, out Entity surfaceEntity)
    {
        surfaceEntity = Entity.Null;
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entityManager.HasComponent<MapSurfaceRuntimeBakedBlobTag>(entity) ||
                entityManager.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity))
            {
                continue;
            }

            surfaceEntity = entity;
            return true;
        }

        return false;
    }

    private static Entity ResolveOwnedSurfaceEntity(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entityManager.HasComponent<MapSurfaceRuntimeBakedBlobTag>(entity) ||
                entityManager.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity))
            {
                return entity;
            }
        }

        return entityManager.CreateEntity();
    }

    private static void RemoveOwnedRuntimeSurfaces(EntityManager entityManager, Entity keepEntity)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (entity == keepEntity ||
                (!entityManager.HasComponent<MapSurfaceRuntimeBakedBlobTag>(entity) &&
                 !entityManager.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity)))
            {
                continue;
            }

            DisposeOwnedSurfaceBlob(entityManager, entity);
            if (entityManager.Exists(entity))
                entityManager.DestroyEntity(entity);
        }
    }

    private static void DisposeOwnedSurfaceBlob(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.Exists(entity) ||
            !entityManager.HasComponent<MapSurfaceComponent>(entity) ||
            (!entityManager.HasComponent<MapSurfaceRuntimeBakedBlobTag>(entity) &&
             !entityManager.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity)))
        {
            return;
        }

        MapSurfaceComponent surface = entityManager.GetComponentData<MapSurfaceComponent>(entity);
        if (surface.SurfaceBlob.IsCreated)
            surface.SurfaceBlob.Dispose();
    }

    private static void GetSurfaceFeatureFlags(
        ref MapSurfaceBlob blob,
        out byte hasLayeredCells,
        out byte hasRoadSurfaces,
        out byte hasBridgeSurfaces)
    {
        hasLayeredCells = 0;
        hasRoadSurfaces = 0;
        hasBridgeSurfaces = 0;

        for (int i = 0; i < blob.Cells.Length; i++)
        {
            if (blob.Cells[i].SurfaceCount > 1)
            {
                hasLayeredCells = 1;
                break;
            }
        }

        for (int i = 0; i < blob.Samples.Length; i++)
        {
            MapSurfaceSample sample = blob.Samples[i];
            if (sample.SurfaceType == MapSurfaceType.Road ||
                sample.SurfaceType == MapSurfaceType.DirtRoad ||
                sample.SurfaceType == MapSurfaceType.Highway ||
                sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                sample.SurfaceType == MapSurfaceType.Ramp ||
                (sample.Flags & MapSurfaceFlags.Road) != 0)
            {
                hasRoadSurfaces = 1;
            }

            if (sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                (sample.Flags & MapSurfaceFlags.Bridge) != 0)
            {
                hasBridgeSurfaces = 1;
            }

            if (hasRoadSurfaces != 0 && hasBridgeSurfaces != 0)
                break;
        }
    }
}
