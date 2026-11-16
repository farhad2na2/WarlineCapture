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
#if UNITY_EDITOR
        internal static Action<string> PublicationFailureProbe;
        private static void Probe(string stage) => PublicationFailureProbe?.Invoke(stage);
#else
        private static void Probe(string stage) { }
#endif
        private struct ComponentSnapshot<T>
#if WARLINE_SOURCE_DECLARED_COMPILER_EXPORT || WARLINE_SOURCE_GRID_WITNESS
            where T : struct, IComponentData
#else
            where T : unmanaged, IComponentData
#endif
        {
            internal bool Present;
            internal T Value;
            internal static ComponentSnapshot<T> Capture(EntityManager em, Entity entity) =>
                entity != Entity.Null && em.Exists(entity) && em.HasComponent<T>(entity)
                    ? new ComponentSnapshot<T> { Present = true, Value = em.GetComponentData<T>(entity) } : default;
            internal void Restore(EntityManager em, Entity entity)
            {
                bool now = em.HasComponent<T>(entity);
                if (Present) { if (now) em.SetComponentData(entity, Value); else em.AddComponentData(entity, Value); }
                else if (now) em.RemoveComponent<T>(entity);
            }
        }
        private sealed class PublicationSnapshot
        {
            private readonly Entity entity, grid;
            private readonly bool requestBuffer, resultBuffer;
            private readonly string name;
            private readonly ComponentSnapshot<OperationMapQueueComponent> queue;
            private readonly ComponentSnapshot<OperationMapLoadStateComponent> load;
            private readonly ComponentSnapshot<ActiveOperationMapComponent> active;
            private readonly ComponentSnapshot<OperationMapBoundsComponent> bounds;
            private readonly ComponentSnapshot<OperationMapMetadataComponent> metadata;
            private readonly ComponentSnapshot<OperationMapReadinessComponent> readiness;
            private readonly ComponentSnapshot<OperationMapGridSourceRecord> source;
            private readonly ComponentSnapshot<GridWalkableSourceWitness> witness;
            internal PublicationSnapshot(EntityManager em, Entity root)
            {
                entity=root; bool exists=root!=Entity.Null && em.Exists(root);
                requestBuffer=exists && em.HasBuffer<OperationMapLoadRequestElement>(root);
                resultBuffer=exists && em.HasBuffer<OperationMapLoadResultElement>(root);
                name=exists ? em.GetName(root).ToString() : null;
                queue=ComponentSnapshot<OperationMapQueueComponent>.Capture(em,root);
                load=ComponentSnapshot<OperationMapLoadStateComponent>.Capture(em,root);
                active=ComponentSnapshot<ActiveOperationMapComponent>.Capture(em,root);
                bounds=ComponentSnapshot<OperationMapBoundsComponent>.Capture(em,root);
                metadata=ComponentSnapshot<OperationMapMetadataComponent>.Capture(em,root);
                readiness=ComponentSnapshot<OperationMapReadinessComponent>.Capture(em,root);
                source=ComponentSnapshot<OperationMapGridSourceRecord>.Capture(em,root);
                using var q=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkableSourceWitness),typeof(GridWalkable));
                grid=q.CalculateEntityCount()==1 ? q.GetSingletonEntity() : Entity.Null;
                witness=ComponentSnapshot<GridWalkableSourceWitness>.Capture(em,grid);
            }
            internal void Restore(EntityManager em, Entity current)
            {
                if (entity==Entity.Null) { if(current!=Entity.Null && em.Exists(current)) em.DestroyEntity(current); }
                else if (em.Exists(entity)) {
                    queue.Restore(em,entity); load.Restore(em,entity); active.Restore(em,entity);
                    bounds.Restore(em,entity); metadata.Restore(em,entity); readiness.Restore(em,entity); source.Restore(em,entity);
                    if(!requestBuffer && em.HasBuffer<OperationMapLoadRequestElement>(entity)) em.RemoveComponent<OperationMapLoadRequestElement>(entity);
                    if(!resultBuffer && em.HasBuffer<OperationMapLoadResultElement>(entity)) em.RemoveComponent<OperationMapLoadResultElement>(entity);
                    em.SetName(entity,name);
                }
                if(grid!=Entity.Null && em.Exists(grid)) witness.Restore(em,grid);
            }
        }
        private readonly World createdWorld;
        private BlobAssetReference<OperationMapBlob> ownedMetadataBlob;
        private bool disposed;
        private bool rollbackPoisoned;
        private BlobAssetReference<OperationMapBlob> rollbackRetainedBlob;

        internal int PublishedGeneration { get; private set; }
        private Entity publishedRoot;

        // Read the exact root actually returned by this producer, never adopt a new singleton.
        internal bool TryGetPublishedRoot(World expectedWorld, out Entity root)
        {
            root = Entity.Null;
            if (disposed || createdWorld != expectedWorld || !TryGetLiveEntityManager(out var em) ||
                publishedRoot == Entity.Null || !em.Exists(publishedRoot) ||
                !em.HasComponent<ActiveOperationMapComponent>(publishedRoot) ||
                !em.HasComponent<OperationMapMetadataComponent>(publishedRoot)) return false;
            var active = em.GetComponentData<ActiveOperationMapComponent>(publishedRoot);
            var metadata = em.GetComponentData<OperationMapMetadataComponent>(publishedRoot);
            if (PublishedGeneration <= 0 || active.Generation != PublishedGeneration || metadata.Generation != PublishedGeneration) return false;
            root = publishedRoot; return true;
        }

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

            if (rollbackPoisoned) { error = "Prior publication rollback failed; clear the publisher before reuse."; return false; }

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
            {
                var rollbackReuse = new PublicationSnapshot(entityManager, rootEntity);
                int oldGenerationReuse = PublishedGeneration; Entity oldRootReuse = publishedRoot;
                try
                {
                    Probe("reuse-before-bind");
                    int reusedGeneration = entityManager.GetComponentData<ActiveOperationMapComponent>(rootEntity).Generation;
                    OperationMapGridSourceRecordWriter.Bind(entityManager, rootEntity, definition, reusedGeneration);
                    Probe("reuse-after-bind");
                    PublishedGeneration = reusedGeneration; publishedRoot = rootEntity;
                    return true;
                }
                catch (Exception failure)
                {
                    try
                    {
                        Probe("reuse-rollback-before-restore");
                        rollbackReuse.Restore(entityManager, rootEntity);
                        PublishedGeneration = oldGenerationReuse; publishedRoot = oldRootReuse;
                        error = "Operation-map reuse rolled back: " + failure.Message;
                    }
                    catch (Exception rollbackFailure)
                    {
                        rollbackPoisoned = true;
                        error = "Operation-map reuse rollback failed; publisher disabled: " + rollbackFailure.Message;
                    }
                    rootEntity = Entity.Null;
                    return false;
                }
            }

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

            PublicationSnapshot rollback = null;
            int oldGeneration = PublishedGeneration; Entity oldPublishedRoot = publishedRoot; bool committed = false;
            try
            {
                rollback = new PublicationSnapshot(entityManager, rootEntity);
                Probe("before-root");
                if (rootEntity == Entity.Null)
                    rootEntity = OperationMapRuntimeRootContract.Create(entityManager);
                else
                    OperationMapRuntimeRootContract.Ensure(entityManager, rootEntity);

                Probe("root-ready");
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
                Probe("active-written");
                entityManager.SetComponentData(rootEntity, new OperationMapBoundsComponent
                {
                    WorldMin = ToFloat3(sourceBounds.WorldMin),
                    WorldMax = ToFloat3(sourceBounds.WorldMax),
                    PlayableMin = ToFloat3(sourceBounds.PlayableMin),
                    PlayableMax = ToFloat3(sourceBounds.PlayableMax),
                    CameraMin = ToFloat3(sourceBounds.CameraMin),
                    CameraMax = ToFloat3(sourceBounds.CameraMax)
                });
                Probe("bounds-written");
                entityManager.SetComponentData(rootEntity, new OperationMapMetadataComponent
                {
                    Blob = newMetadataBlob,
                    MetadataHash = new FixedString128Bytes(definition.GeneratedMetadataHash),
                    Generation = generation
                });
                Probe("metadata-written");
                entityManager.SetComponentData(rootEntity, new OperationMapReadinessComponent
                {
                    Generation = generation,
                    ReadyFlags = readyFlags,
                    RequiredFlags = requiredFlags,
                    FailedFlags = OperationMapReadinessFlags.None
                });
                Probe("readiness-written");
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
                Probe("load-written");
                entityManager.SetName(rootEntity, "OperationMapRuntimeRoot");
                PublishedGeneration = generation;
                publishedRoot = rootEntity;
                OperationMapGridSourceRecordWriter.Bind(entityManager, rootEntity, definition, generation);

                Probe("identity-bound");
                BlobAssetReference<OperationMapBlob> previousOwnedBlob = ownedMetadataBlob;
                ownedMetadataBlob = newMetadataBlob;
                newMetadataBlob = default;
                committed = true;
                if (previousOwnedBlob.IsCreated)
                    previousOwnedBlob.Dispose();

                error = null;
                return true;
            }
            catch (Exception failure)
            {
                if (committed) throw;
                try
                {
                    Probe("rollback-before-restore");
                    rollback?.Restore(entityManager, rootEntity);
                    PublishedGeneration = oldGeneration; publishedRoot = oldPublishedRoot;
                    rootEntity = Entity.Null; error = "Operation-map publication rolled back: " + failure.Message;
                }
                catch (Exception rollbackFailure)
                {
                    // Either old or new metadata can still be reachable. Retain both
                    // until ClearPublishedState has destroyed all published roots.
                    rollbackPoisoned = true;
                    rollbackRetainedBlob = newMetadataBlob;
                    newMetadataBlob = default;
                    rootEntity = Entity.Null;
                    error = "Operation-map rollback failed; publisher disabled: " + rollbackFailure.Message;
                }
                return false;
            }
            finally
            {
                if (newMetadataBlob.IsCreated)
                    newMetadataBlob.Dispose();
            }
        }

        public void ClearPublishedState()
        {
            publishedRoot = Entity.Null;
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
            if (rollbackRetainedBlob.IsCreated) rollbackRetainedBlob.Dispose();
            rollbackRetainedBlob = default; rollbackPoisoned = false;
            PublishedGeneration = 0;
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
