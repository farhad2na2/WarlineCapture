using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;
using SnivelerCode.GpuAnimation.Scripts.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct UnitDeathSystem : ISystem
    {
        private const float VehicleWreckLifetimeSeconds = 5f;
        private const float CorpseViewportPadding = 0.1f;
        private NativeList<DeathBeginCandidate> _deathBeginCandidates;
        private NativeList<Entity> _finalizeEntities;
        private EntityQuery _respawnQueueQuery;
        private EntityQuery _deathBeginQuery;
        private EntityQuery _finalizeQuery;
        private EntityQuery _cameraSnapshotQuery;

        private struct DeathBeginCandidate
        {
            public Entity Entity;
            public float Duration;
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitHealth>();
            _respawnQueueQuery = state.GetEntityQuery(ComponentType.ReadOnly<RespawnQueueTag>());
            _deathBeginQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<UnitAnimationSettings>(),
                ComponentType.Exclude<UnitDeathAnimationComponent>(),
                ComponentType.Exclude<StaticGridBlocker>());
            _finalizeQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<UnitDeathAnimationComponent>(),
                ComponentType.Exclude<StaticGridBlocker>());
            _cameraSnapshotQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<RuntimeCameraSnapshotComponent>());
            _deathBeginCandidates = new NativeList<DeathBeginCandidate>(64, Allocator.Persistent);
            _finalizeEntities = new NativeList<Entity>(64, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_deathBeginCandidates.IsCreated)
                _deathBeginCandidates.Dispose();
            if (_finalizeEntities.IsCreated)
                _finalizeEntities.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var queueEntity = RespawnQueueUtility.GetOrCreateQueue(ref state, _respawnQueueQuery);
            var queueState = SystemAPI.GetComponent<RespawnQueueComponent>(queueEntity);
            var em = state.EntityManager;
            AudioEventRequestSystem.EnsureAudioEntity(em);
            float dt = SystemAPI.Time.DeltaTime;
            double now = SystemAPI.Time.ElapsedTime;
            double respawnDelay = math.max(0.01f, queueState.RespawnDelaySeconds);

            int beginCapacity = _deathBeginQuery.CalculateEntityCount();
            if (_deathBeginCandidates.Capacity < beginCapacity)
                _deathBeginCandidates.SetCapacity(beginCapacity);
            _deathBeginCandidates.Clear();
            state.Dependency = new CollectDeathBeginCandidatesJob
            {
                Candidates = _deathBeginCandidates.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            for (int i = 0; i < _deathBeginCandidates.Length; i++)
            {
                DeathBeginCandidate candidate = _deathBeginCandidates[i];
                Entity entity = candidate.Entity;
                if (!em.Exists(entity) || em.HasComponent<UnitDeathAnimationComponent>(entity))
                    continue;

                StripActiveUnitState(em, entity);
                if (TryBeginVehicleWreck(em, entity))
                {
                    if (em.HasComponent<LocalTransform>(entity))
                    {
                        CombatAudioEventUtility.EmitVehicleDestroyed(
                            em,
                            entity,
                            em.GetComponentData<LocalTransform>(entity).Position,
                            (float)now);
                    }
                    continue;
                }

                float playbackDuration = UnitDeathAnimationPlaybackUtility.Prepare(
                    em,
                    entity,
                    candidate.Duration);
                em.AddComponentData(entity, new UnitDeathAnimationComponent
                {
                    TimeRemaining = playbackDuration
                });
            }

            int finalizeCapacity = _finalizeQuery.CalculateEntityCount();
            if (_finalizeEntities.Capacity < finalizeCapacity)
                _finalizeEntities.SetCapacity(finalizeCapacity);
            _finalizeEntities.Clear();
            state.Dependency = new CollectDeathAnimationFinalizeJob
            {
                DeltaTime = dt,
                FinalizeEntities = _finalizeEntities.AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            bool hasValidCamera = TryGetValidCameraSnapshot(out RuntimeCameraSnapshotComponent camera);
            for (int i = 0; i < _finalizeEntities.Length; i++)
            {
                Entity entity = _finalizeEntities[i];
                if (!em.Exists(entity) || !em.HasComponent<UnitDeathAnimationComponent>(entity))
                    continue;

                UnitDeathAnimationComponent deathState = em.GetComponentData<UnitDeathAnimationComponent>(entity);
                if (deathState.PoseFrozen == 0)
                {
                    FreezeDeathPose(em, entity);
                    deathState.TimeRemaining = 0f;
                    deathState.PoseFrozen = 1;
                    em.SetComponentData(entity, deathState);
                    continue;
                }

                if (!hasValidCamera)
                    continue;
                bool insideViewport = em.HasComponent<LocalTransform>(entity) &&
                                      IsInsideCameraViewport(camera, em.GetComponentData<LocalTransform>(entity).Position);
                if (insideViewport)
                {
                    continue;
                }

                FinalizeDeath(em, queueEntity, entity, now, respawnDelay);
            }
        }

        [BurstCompile]
        [WithNone(typeof(UnitDeathAnimationComponent), typeof(StaticGridBlocker))]
        [WithChangeFilter(typeof(UnitHealth))]
        private partial struct CollectDeathBeginCandidatesJob : IJobEntity
        {
            public NativeList<DeathBeginCandidate>.ParallelWriter Candidates;

            private void Execute(Entity entity, in UnitHealth health, in UnitAnimationSettings animationSettings)
            {
                if (health.Current > 0)
                    return;

                Candidates.AddNoResize(new DeathBeginCandidate
                {
                    Entity = entity,
                    Duration = math.max(0.01f, animationSettings.DeathAnimationSeconds)
                });
            }
        }

        [BurstCompile]
        [WithNone(typeof(StaticGridBlocker))]
        private partial struct CollectDeathAnimationFinalizeJob : IJobEntity
        {
            public float DeltaTime;
            public NativeList<Entity>.ParallelWriter FinalizeEntities;

            private void Execute(Entity entity, in UnitHealth health, ref UnitDeathAnimationComponent deathState)
            {
                if (health.Current > 0)
                    return;

                if (deathState.PoseFrozen != 0)
                {
                    FinalizeEntities.AddNoResize(entity);
                    return;
                }

                deathState.TimeRemaining = math.max(0f, deathState.TimeRemaining - DeltaTime);
                if (deathState.TimeRemaining <= 0f)
                    FinalizeEntities.AddNoResize(entity);
            }
        }

        private bool TryGetValidCameraSnapshot(out RuntimeCameraSnapshotComponent camera)
        {
            camera = default;
            if (_cameraSnapshotQuery.CalculateEntityCount() != 1)
                return false;

            camera = _cameraSnapshotQuery.GetSingleton<RuntimeCameraSnapshotComponent>();
            return camera.IsValid != 0;
        }

        internal static bool IsInsideCameraViewport(
            in RuntimeCameraSnapshotComponent camera,
            float3 worldPosition)
        {
            if (camera.IsValid == 0)
                return false;

            float4 homogeneousPosition = new(worldPosition, 1f);
            float4 cameraPosition = math.mul(camera.WorldToCamera, homogeneousPosition);
            float4 clipPosition = math.mul(camera.ViewProjection, homogeneousPosition);
            float invW = math.abs(clipPosition.w) > 0.000001f ? 1f / clipPosition.w : 0f;
            float viewportX = clipPosition.x * invW * 0.5f + 0.5f;
            float viewportY = clipPosition.y * invW * 0.5f + 0.5f;
            float viewportZ = -cameraPosition.z;
            return viewportZ > 0f &&
                   viewportX >= -CorpseViewportPadding && viewportX <= 1f + CorpseViewportPadding &&
                   viewportY >= -CorpseViewportPadding && viewportY <= 1f + CorpseViewportPadding;
        }

        private static void FreezeDeathPose(EntityManager em, Entity unit)
        {
            using NativeList<Entity> visualEntities = new(256, Allocator.Temp);
            using NativeHashSet<Entity> visited = new(256, Allocator.Temp);
            CollectVisualEntities(em, unit, visualEntities, visited);
            if (em.HasComponent<UnitDetailedVisualReference>(unit))
                CollectVisualEntities(em, em.GetComponentData<UnitDetailedVisualReference>(unit).Root, visualEntities, visited);
            if (em.HasComponent<UnitModelInstanceReference>(unit))
                CollectVisualEntities(em, em.GetComponentData<UnitModelInstanceReference>(unit).Instance, visualEntities, visited);
            if (em.HasComponent<UnitMidLodInstanceReference>(unit))
                CollectVisualEntities(em, em.GetComponentData<UnitMidLodInstanceReference>(unit).Instance, visualEntities, visited);
            if (em.HasComponent<UnitLowLodInstanceReference>(unit))
                CollectVisualEntities(em, em.GetComponentData<UnitLowLodInstanceReference>(unit).Instance, visualEntities, visited);

            for (int i = 0; i < visualEntities.Length; i++)
            {
                Entity visualEntity = visualEntities[i];
                if (!em.HasComponent<MaterialAnimationData>(visualEntity) ||
                    !em.HasComponent<MaterialAnimationIndex>(visualEntity) ||
                    !em.HasComponent<MaterialAnimatorLink>(visualEntity) ||
                    em.HasComponent<UnitDeathPoseFreezeTag>(visualEntity))
                {
                    continue;
                }

                em.AddComponent<UnitDeathPoseFreezeTag>(visualEntity);
            }
        }

        private static void CollectVisualEntities(
            EntityManager em,
            Entity entity,
            NativeList<Entity> visualEntities,
            NativeHashSet<Entity> visited)
        {
            if (entity == Entity.Null || !em.Exists(entity) || !visited.Add(entity))
                return;

            visualEntities.Add(entity);
            if (em.HasBuffer<LinkedEntityGroup>(entity))
            {
                DynamicBuffer<LinkedEntityGroup> linkedEntities = em.GetBuffer<LinkedEntityGroup>(entity);
                for (int i = 0; i < linkedEntities.Length; i++)
                    CollectVisualEntities(em, linkedEntities[i].Value, visualEntities, visited);
            }

            if (!em.HasBuffer<Child>(entity))
                return;

            DynamicBuffer<Child> children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++)
                CollectVisualEntities(em, children[i].Value, visualEntities, visited);
        }

        internal static void StripActiveUnitState(EntityManager em, Entity entity)
        {
            if (em.HasComponent<SelectedUnitTag>(entity))
                em.RemoveComponent<SelectedUnitTag>(entity);
            if (em.HasComponent<ManualMoveOrderTag>(entity))
                em.RemoveComponent<ManualMoveOrderTag>(entity);
            if (em.HasComponent<AutoWanderMoveTag>(entity))
                em.RemoveComponent<AutoWanderMoveTag>(entity);
            if (em.HasComponent<EngageTarget>(entity))
                em.RemoveComponent<EngageTarget>(entity);
            if (em.HasComponent<RecentAttacker>(entity))
                em.RemoveComponent<RecentAttacker>(entity);
            if (em.HasComponent<RecentDamageHealthBarVisibility>(entity))
                em.RemoveComponent<RecentDamageHealthBarVisibility>(entity);
            if (em.HasComponent<UnitPathFollow>(entity))
                em.RemoveComponent<UnitPathFollow>(entity);
            if (em.HasComponent<UnitPathRange>(entity))
                em.RemoveComponent<UnitPathRange>(entity);
            if (em.HasComponent<UnitPathRequest>(entity))
                em.RemoveComponent<UnitPathRequest>(entity);
            if (em.HasComponent<UnitAttackAnimationComponent>(entity))
                em.SetComponentData(entity, new UnitAttackAnimationComponent { TimeRemaining = 0f });
            if (em.HasComponent<UnitMoveVisualComponent>(entity))
                em.SetComponentData(entity, new UnitMoveVisualComponent { IsMoving = 0, StillSeconds = 0f });

            if (em.HasBuffer<UnitAttachedLightSetupElement>(entity) && !em.HasComponent<UnitAttachedLightCleanupRequest>(entity))
                em.AddComponent<UnitAttachedLightCleanupRequest>(entity);
        }

        private static bool TryBeginVehicleWreck(EntityManager em, Entity entity)
        {
            bool hasConfiguredDestroyedVisual = em.HasComponent<VehicleDestroyedVisualPrefabReference>(entity);
            bool hasLegacyDestroyedVisual = em.HasComponent<UnitDestroyedVisualReference>(entity);
            if (!hasConfiguredDestroyedVisual && !hasLegacyDestroyedVisual)
            {
                return false;
            }

            if (em.HasComponent<UnitAirComponent>(entity) && em.HasComponent<LocalTransform>(entity))
            {
                UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
                LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
                float groundedY = ResolveAirWreckGroundY(em, entity, transform, airState);
                transform.Position.y = groundedY;
                em.SetComponentData(entity, transform);

                airState.Airborne = 0;
                airState.ReturningHome = 0;
                airState.TakeoffRolling = 0;
                airState.LandingRolling = 0;
                airState.AttackRunActive = 0;
                airState.ReturnApproachInitialized = 0;
                em.SetComponentData(entity, airState);
            }

            if (hasConfiguredDestroyedVisual)
            {
                if (!em.HasComponent<VehicleDestroyedVisualSpawnRequest>(entity))
                    em.AddComponent<VehicleDestroyedVisualSpawnRequest>(entity);
            }
            else
            {
                UnitDestroyedVisualReference visualRef = em.GetComponentData<UnitDestroyedVisualReference>(entity);
                if (em.HasBuffer<Child>(entity))
                {
                    var children = em.GetBuffer<Child>(entity);
                    for (int i = 0; i < children.Length; i++)
                    {
                        Entity child = children[i].Value;
                        UnitDestroyedVisualSystem.SetChildVisible(em, child, child == visualRef.DestroyedVisual);
                    }
                }
                else
                {
                    UnitDestroyedVisualSystem.SetChildVisible(em, visualRef.AliveVisual, false);
                    if (em.HasComponent<UnitTurretReference>(entity))
                        UnitDestroyedVisualSystem.SetChildVisible(em, em.GetComponentData<UnitTurretReference>(entity).Turret, false);
                    UnitDestroyedVisualSystem.SetChildVisible(em, visualRef.DestroyedVisual, true);
                }
            }

            if (em.HasComponent<UnitFootprint>(entity) && em.HasComponent<UnitGrid>(entity))
                AddGroundWreckBlocker(em, entity);

            if (em.HasComponent<VehicleWreckComponent>(entity))
            {
                em.SetComponentData(entity, new VehicleWreckComponent { TimeRemaining = VehicleWreckLifetimeSeconds });
            }
            else
            {
                em.AddComponentData(entity, new VehicleWreckComponent { TimeRemaining = VehicleWreckLifetimeSeconds });
            }

            return true;
        }

        private static void AddGroundWreckBlocker(EntityManager em, Entity entity)
        {
            if (!em.HasComponent<StaticGridBlocker>(entity))
                em.AddComponent<StaticGridBlocker>(entity);

            int2 footprint = em.GetComponentData<UnitFootprint>(entity).Size;
            if (em.HasComponent<GridBlockerSize>(entity))
            {
                em.SetComponentData(entity, new GridBlockerSize { Size = footprint });
            }
            else
            {
                em.AddComponentData(entity, new GridBlockerSize { Size = footprint });
            }
        }

        private static float ResolveAirWreckGroundY(
            EntityManager em,
            Entity entity,
            in LocalTransform transform,
            in UnitAirComponent airState)
        {
            float groundOffset = ResolveGroundOffset(em, entity);
            if (em.HasComponent<UnitSurfaceComponent>(entity))
            {
                UnitSurfaceComponent surface = em.GetComponentData<UnitSurfaceComponent>(entity);
                if (surface.HasSurface != 0)
                    return surface.LastSampledHeight + groundOffset;
            }

            if (em.HasComponent<UnitGrid>(entity) && TryGetRuntimeGrid(em, out GridConfig grid))
            {
                float3 worldPosition = transform.Position;
                var groundingSystem = new MapSurfaceSpawnGrounding();
                if (groundingSystem.TryGroundCellCenter(
                        em,
                        grid,
                        em.GetComponentData<UnitGrid>(entity).Cell,
                        ref worldPosition,
                        out _,
                        groundOffset))
                {
                    return worldPosition.y;
                }
            }

            if (airState.HomeInitialized != 0)
                return airState.HomePosition.y;

            return transform.Position.y;
        }

        private static float ResolveGroundOffset(EntityManager em, Entity entity)
        {
            return em.HasComponent<UnitGroundOffsetComponent>(entity)
                ? em.GetComponentData<UnitGroundOffsetComponent>(entity).Value
                : 0f;
        }

        private static bool TryGetRuntimeGrid(EntityManager em, out GridConfig grid)
        {
            grid = default;
            using EntityQuery runtimeGridQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<GridConfig>(),
                ComponentType.ReadOnly<RuntimeGridBootstrapGridTag>());
            int runtimeGridCount = runtimeGridQuery.CalculateEntityCount();
            if (runtimeGridCount == 1)
            {
                Entity gridEntity = runtimeGridQuery.GetSingletonEntity();
                grid = em.GetComponentData<GridConfig>(gridEntity);
                return true;
            }

            using EntityQuery gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (gridQuery.CalculateEntityCount() != 1)
                return false;

            Entity fallbackGridEntity = gridQuery.GetSingletonEntity();
            grid = em.GetComponentData<GridConfig>(fallbackGridEntity);
            return true;
        }

        internal static void FinalizeDeath(EntityManager em, Entity queueEntity, Entity entity, double now, double respawnDelay)
        {
            if (!em.Exists(entity))
                return;

            var destroySet = new HashSet<Entity> { entity };
            CollectDescendants(em, entity, destroySet);
            CollectLinkedEntities(em, entity, destroySet);

            var entities = new NativeArray<Entity>(destroySet.Count, Allocator.Temp);
            int index = 0;
            foreach (Entity e in destroySet)
                entities[index++] = e;

            em.DestroyEntity(entities);
            entities.Dispose();
        }

        private static void CollectDescendants(EntityManager em, Entity entity, HashSet<Entity> destroySet)
        {
            if (!em.HasBuffer<Child>(entity))
                return;

            var children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++)
            {
                Entity child = children[i].Value;
                if (!destroySet.Add(child))
                    continue;

                CollectDescendants(em, child, destroySet);
            }
        }

        private static void CollectLinkedEntities(EntityManager em, Entity entity, HashSet<Entity> destroySet)
        {
            if (!em.HasBuffer<LinkedEntityGroup>(entity))
                return;

            var linked = em.GetBuffer<LinkedEntityGroup>(entity);
            for (int i = 0; i < linked.Length; i++)
                destroySet.Add(linked[i].Value);
        }
    }
}
