using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Runtime;

namespace Game.Composition
{
    internal sealed class MapSurfaceRuntimeBootstrapSceneSystemHelper
    {
        private readonly World createdWorld;
        private bool runtimeSurfaceDisposed;

        public MapSurfaceRuntimeBootstrapSceneSystemHelper(World world)
        {
            createdWorld = world;
        }

        public bool Ensure(MapSurfaceAuthoring authoring)
        {
            return Ensure(authoring, out _);
        }

        public bool Ensure(MapSurfaceAuthoring authoring, out string error)
        {
            if (authoring == null)
            {
                error = "Map surface authoring is missing.";
                return false;
            }

            bool ensured = EnsureActiveSurface(authoring.BakedSurfaceData, out Entity surfaceEntity, out error);
            if (ensured && TryGetLiveEntityManager(out EntityManager entityManager))
                MapSurfaceSceneOverlayPresentation.Publish(authoring, entityManager, surfaceEntity);
            return ensured;
        }

        public bool Ensure(MapSurfaceDataAsset surfaceData)
        {
            return EnsureActiveSurface(surfaceData, out _, out _);
        }

        public bool Ensure(MapSurfaceDataAsset surfaceData, out string error)
        {
            return EnsureActiveSurface(surfaceData, out _, out error);
        }

        private bool EnsureActiveSurface(MapSurfaceDataAsset surfaceData, out Entity surfaceEntity, out string error)
        {
            surfaceEntity = Entity.Null;
            if (surfaceData == null)
            {
                error = "Map surface data is missing.";
                return false;
            }

            if (!TryGetLiveEntityManager(out EntityManager entityManager))
            {
                error = "The ECS world is unavailable for map surface publication.";
                return false;
            }

            bool resolvedActiveSurface = OperationMapMetadataUtility.TryResolveActiveSurfaceMetadata(
                entityManager,
                out OperationMapSurfaceMetadataBlob expectedSurface,
                out OperationMapGridBlob expectedGrid,
                out bool hasActiveMap,
                out error);
            if (!resolvedActiveSurface && hasActiveMap)
                return false;
            if (resolvedActiveSurface && !MatchesActiveMap(surfaceData, in expectedSurface, in expectedGrid, out error))
                return false;

            if (!surfaceData.TryCreateRuntimeBlobAsset(Allocator.Persistent, out BlobAssetReference<MapSurfaceBlob> surfaceBlob))
            {
                error = "The map surface runtime blob could not be created.";
                Debug.LogWarning("[MapSurfaceRuntimeBootstrap] missingRuntimeSurfaceBlob");
                return false;
            }

            ref MapSurfaceBlob blob = ref surfaceBlob.Value;
            if (resolvedActiveSurface && !MatchesActiveMap(ref blob, in expectedSurface, in expectedGrid, out error))
            {
                surfaceBlob.Dispose();
                return false;
            }

            surfaceEntity = ResolveRuntimeSurfaceEntity(entityManager);
            DisposeOwnedSurfaceBlob(entityManager, surfaceEntity);

            if (!entityManager.HasComponent<MapSurfaceComponent>(surfaceEntity))
                entityManager.AddComponent<MapSurfaceComponent>(surfaceEntity);

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
            RemoveOtherSurfaceEntities(entityManager, surfaceEntity);
            runtimeSurfaceDisposed = false;
            error = null;
            return true;
        }

        private static bool MatchesActiveMap(
            MapSurfaceDataAsset surfaceData,
            in OperationMapSurfaceMetadataBlob expectedSurface,
            in OperationMapGridBlob expectedGrid,
            out string error)
        {
            FixedString64Bytes runtimeHash = new(surfaceData.ComputeRuntimeBlobHash().ToString());
            Vector2Int dimensions = surfaceData.Dimensions;
            if (!runtimeHash.Equals(expectedSurface.RuntimeBlobHash) ||
                surfaceData.SurfaceCount != expectedSurface.SurfaceCount ||
                surfaceData.PayloadVersion != expectedSurface.PayloadVersion ||
                surfaceData.PayloadEncoding != expectedSurface.PayloadEncoding ||
                dimensions.x != expectedGrid.Dimensions.x ||
                dimensions.y != expectedGrid.Dimensions.y ||
                surfaceData.CellSize != expectedGrid.CellSize ||
                !math.all((float3)surfaceData.GridOrigin == expectedGrid.Origin))
            {
                error = "Serialized map surface data does not match the active operation-map metadata.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool MatchesActiveMap(
            ref MapSurfaceBlob blob,
            in OperationMapSurfaceMetadataBlob expectedSurface,
            in OperationMapGridBlob expectedGrid,
            out string error)
        {
            if (MapSurfaceBlobAccess.SurfaceCount(ref blob) != expectedSurface.SurfaceCount ||
                !math.all(blob.Dimensions == expectedGrid.Dimensions) ||
                blob.CellSize != expectedGrid.CellSize ||
                !math.all(blob.GridOrigin == expectedGrid.Origin))
            {
                error = "Created map surface blob does not match the active operation-map metadata.";
                return false;
            }

            error = null;
            return true;
        }

        public void DisposeRuntimeSurface()
        {
            if (runtimeSurfaceDisposed)
                return;

            if (!TryGetLiveEntityManager(out EntityManager entityManager))
            {
                runtimeSurfaceDisposed = true;
                return;
            }

            try
            {
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
            catch (System.InvalidOperationException)
            {
                runtimeSurfaceDisposed = true;
                return;
            }

            runtimeSurfaceDisposed = true;
        }

        private bool TryGetLiveEntityManager(out EntityManager entityManager)
        {
            entityManager = default;
            World world = createdWorld;
            if (world == null || !world.IsCreated)
                return false;

            try
            {
                entityManager = world.EntityManager;
                return true;
            }
            catch (System.InvalidOperationException)
            {
                return false;
            }
        }

        private static Entity ResolveRuntimeSurfaceEntity(EntityManager entityManager)
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

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!entityManager.HasComponent<MapSurfaceRuntimeBakedBlobTag>(entity) &&
                    !entityManager.HasComponent<MapSurfaceFlatEquivalentRuntimeBlobTag>(entity))
                    return entity;
            }

            return entityManager.CreateEntity();
        }

        private static void RemoveOtherSurfaceEntities(EntityManager entityManager, Entity keepEntity)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (entity == keepEntity)
                    continue;

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

            hasLayeredCells = (byte)(MapSurfaceBlobAccess.IsLayered(ref blob) ? 1 : 0);

            int surfaceCount = MapSurfaceBlobAccess.SurfaceCount(ref blob);
            for (int i = 0; i < surfaceCount; i++)
            {
                if (!MapSurfaceBlobAccess.TryGetSurfaceByIndex(ref blob, i, out MapSurfaceSample sample))
                    continue;

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
}
