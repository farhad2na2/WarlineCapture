using System;
using Game.Components;
using Game.Configs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Composition
{
    internal sealed class OperationMapRuntimeBootstrapSceneSystemHelper : IDisposable
    {
        private readonly World createdWorld;
        private BlobAssetReference<OperationMapBlob> ownedMetadataBlob;
        private bool disposed;

        public OperationMapRuntimeBootstrapSceneSystemHelper(World world)
        {
            createdWorld = world;
        }

        public bool TryPublish(
            OperationMapCatalogConfig catalog,
            string operationMapId,
            in FixedString64Bytes scenarioId,
            in FixedString64Bytes missionId,
            int generation,
            OperationMapReadinessFlags readyFlags,
            OperationMapReadinessFlags requiredFlags,
            out Entity rootEntity,
            out string error)
        {
            rootEntity = Entity.Null;
            if (catalog == null)
            {
                error = "Operation-map catalog is required.";
                return false;
            }

            if (!catalog.TryValidate(out error))
                return false;

            if (!catalog.TryResolve(operationMapId, out OperationMapDefinition definition))
            {
                error = $"Operation-map id '{operationMapId ?? "<null>"}' is not present in the catalog.";
                return false;
            }

            return TryPublish(
                definition,
                scenarioId,
                missionId,
                generation,
                readyFlags,
                requiredFlags,
                out rootEntity,
                out error);
        }

        public bool TryUpdateReadiness(
            int generation,
            OperationMapReadinessFlags readyFlags,
            OperationMapReadinessFlags failedFlags,
            out string error)
        {
            if (disposed)
            {
                error = "Operation-map metadata bootstrap is disposed.";
                return false;
            }

            if (generation <= 0 || (readyFlags & failedFlags) != 0)
            {
                error = "Operation-map readiness requires a positive generation and non-overlapping ready/failed flags.";
                return false;
            }

            if (!TryGetLiveEntityManager(out EntityManager entityManager))
            {
                error = "Operation-map readiness requires a live ECS World.";
                return false;
            }
            if (!OperationMapRuntimeRootContract.TryResolveSingle(
                    entityManager,
                    out Entity rootEntity,
                    out error))
                return false;
            if (rootEntity == Entity.Null)
            {
                error = "Operation-map readiness requires one published root.";
                return false;
            }

            return OperationMapRuntimeReadinessProjection.TryApply(
                entityManager,
                rootEntity,
                generation,
                readyFlags,
                failedFlags,
                out error);
        }

        public bool TryPublish(
            OperationMapDefinition definition,
            in FixedString64Bytes scenarioId,
            in FixedString64Bytes missionId,
            int generation,
            OperationMapReadinessFlags readyFlags,
            OperationMapReadinessFlags requiredFlags,
            out Entity rootEntity,
            out string error)
        {
            rootEntity = Entity.Null;
            if (disposed)
            {
                error = "Operation-map metadata bootstrap is disposed.";
                return false;
            }

            if (definition == null)
            {
                error = "Operation-map definition is required.";
                return false;
            }

            if (scenarioId.IsEmpty || missionId.IsEmpty || generation <= 0)
            {
                error = "Scenario id, mission id, and a positive generation are required.";
                return false;
            }

            if (!TryGetLiveEntityManager(out EntityManager entityManager))
            {
                error = "Operation-map metadata bootstrap requires a live ECS World.";
                return false;
            }

            if (CampaignMissionOperationMapReuseUtility.TryReuse(
                    entityManager, definition, out rootEntity, out error))
                return true;

            if (!OperationMapRuntimeRootContract.TryResolveSingle(
                    entityManager,
                    out rootEntity,
                    out error))
                return false;

            if (rootEntity != Entity.Null &&
                entityManager.HasComponent<ActiveOperationMapComponent>(rootEntity) &&
                entityManager.GetComponentData<ActiveOperationMapComponent>(rootEntity).Generation >= generation)
            {
                error = "Operation-map generation must increase monotonically.";
                rootEntity = Entity.Null;
                return false;
            }

            if (!definition.TryCreatePersistentMetadataBlob(
                    out BlobAssetReference<OperationMapBlob> newMetadataBlob,
                    out error))
            {
                rootEntity = Entity.Null;
                return false;
            }

            try
            {
                if (rootEntity == Entity.Null)
                    rootEntity = OperationMapRuntimeRootContract.Create(entityManager);
                else
                    OperationMapRuntimeRootContract.Ensure(entityManager, rootEntity);

                OperationMapBoundsConfig sourceBounds = definition.Bounds;
                entityManager.SetComponentData(rootEntity, new ActiveOperationMapComponent
                {
                    OperationMapId = new FixedString64Bytes(definition.OperationMapId),
                    ScenarioId = scenarioId,
                    MissionId = missionId,
                    SchemaVersion = definition.SchemaVersion,
                    ContentVersion = definition.ContentVersion,
                    Generation = generation
                });
                entityManager.SetComponentData(rootEntity, new OperationMapBoundsComponent
                {
                    WorldMin = ToFloat3(sourceBounds.WorldMin),
                    WorldMax = ToFloat3(sourceBounds.WorldMax),
                    PlayableMin = ToFloat3(sourceBounds.PlayableMin),
                    PlayableMax = ToFloat3(sourceBounds.PlayableMax),
                    CameraMin = ToFloat3(sourceBounds.CameraMin),
                    CameraMax = ToFloat3(sourceBounds.CameraMax)
                });
                entityManager.SetComponentData(rootEntity, new OperationMapMetadataComponent
                {
                    Blob = newMetadataBlob,
                    MetadataHash = new FixedString128Bytes(definition.GeneratedMetadataHash),
                    Generation = generation
                });
                entityManager.SetComponentData(rootEntity, new OperationMapReadinessComponent
                {
                    Generation = generation,
                    ReadyFlags = readyFlags,
                    RequiredFlags = requiredFlags,
                    FailedFlags = OperationMapReadinessFlags.None
                });
                entityManager.SetComponentData(rootEntity, new OperationMapLoadStateComponent
                {
                    ActiveRequestId = 0,
                    Generation = generation,
                    Progress01 = OperationMapRuntimeReadinessProjection.HasRequired(
                        readyFlags,
                        requiredFlags) ? 1f : 0f,
                    Status = OperationMapRuntimeReadinessProjection.HasRequired(
                        readyFlags,
                        requiredFlags)
                        ? OperationMapLoadStatusKind.Ready
                        : OperationMapLoadStatusKind.BindingMetadata,
                    Readiness = readyFlags,
                    IsBusy = 0
                });
                entityManager.SetName(rootEntity, "OperationMapRuntimeRoot");

                BlobAssetReference<OperationMapBlob> previousOwnedBlob = ownedMetadataBlob;
                ownedMetadataBlob = newMetadataBlob;
                newMetadataBlob = default;
                if (previousOwnedBlob.IsCreated)
                    previousOwnedBlob.Dispose();

                error = null;
                return true;
            }
            finally
            {
                if (newMetadataBlob.IsCreated)
                    newMetadataBlob.Dispose();
            }
        }

        public void ClearPublishedState()
        {
            if (TryGetLiveEntityManager(out EntityManager entityManager))
            {
                using EntityQuery query = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<OperationMapRootComponent>());
                using NativeArray<Entity> roots = query.ToEntityArray(Allocator.Temp);
                for (int index = 0; index < roots.Length; index++)
                {
                    if (entityManager.Exists(roots[index]))
                        entityManager.DestroyEntity(roots[index]);
                }
            }

            DisposeOwnedBlob();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            ClearPublishedState();
            disposed = true;
        }

        private bool TryGetLiveEntityManager(out EntityManager entityManager)
        {
            entityManager = default;
            if (createdWorld == null || !createdWorld.IsCreated)
                return false;

            try
            {
                entityManager = createdWorld.EntityManager;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void DisposeOwnedBlob()
        {
            if (ownedMetadataBlob.IsCreated)
                ownedMetadataBlob.Dispose();
            ownedMetadataBlob = default;
        }

        private static float3 ToFloat3(UnityEngine.Vector3 value) => new(value.x, value.y, value.z);
    }
}
