using Game.Configs;
using Game.Components;
using Game.Missions.Contracts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct CampaignMissionAmbientPresentationSystem : ISystem
    {
        internal const int MaxCivilianPresentations = 32;
        private const int CivilianPrefabKeyCount = 4;
        private EntityQuery _ambientQuery;
        private EntityQuery _prefabRegistryQuery;
        private FixedString64Bytes _processedSessionToken;
        private int _processedAttemptOrdinal;
        private uint _processedSourceVersion;
        private byte _presentationStarted;
        private byte _capacityUnavailable;

        public void OnCreate(ref SystemState state)
        {
            _ambientQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CampaignMissionAmbientCivilianComponent>());
            _prefabRegistryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            _processedAttemptOrdinal = -1;
            state.RequireForUpdate<CampaignMissionRootComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) ||
                !SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) ||
                !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) ||
                !catalog.Blob.IsCreated || !metadata.Blob.IsCreated ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
            {
                Cleanup(ref state, _ambientQuery, default, -1);
                return;
            }

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (!TryValidatePresentations(ref definition, ref metadata.Blob.Value, out int instanceCount))
            {
                Cleanup(ref state, _ambientQuery, default, -1);
                return;
            }

            bool sameAttempt = _processedSessionToken.Equals(runtime.SessionToken) &&
                               _processedAttemptOrdinal == runtime.AttemptOrdinal;
            if (sameAttempt &&
                _processedSourceVersion == runtime.SourceVersion &&
                (_presentationStarted != 0 || _capacityUnavailable != 0))
                return;

            Cleanup(ref state, _ambientQuery, runtime.SessionToken, runtime.AttemptOrdinal);
            bool created = EnsurePresentation(
                ref state, _ambientQuery, _prefabRegistryQuery, in runtime,
                ref definition, ref metadata.Blob.Value, instanceCount);
            _capacityUnavailable = created ? (byte)0 : (byte)1;
            _presentationStarted = created ? (byte)1 : (byte)0;
            _processedSessionToken = runtime.SessionToken;
            _processedAttemptOrdinal = runtime.AttemptOrdinal;
            _processedSourceVersion = runtime.SourceVersion;
        }

        public void OnDestroy(ref SystemState state)
        {
            if (!_ambientQuery.IsEmptyIgnoreFilter)
                DestroyIndividually(state.EntityManager, _ambientQuery);
        }

        private static bool TryValidatePresentations(
            ref CampaignMissionDefinitionBlob definition,
            ref OperationMapBlob map,
            out int instanceCount)
        {
            instanceCount = 0;
            if (definition.AmbientPresentations.Length == 0)
                return false;

            for (int index = 0; index < definition.AmbientPresentations.Length; index++)
            {
                ref CampaignMissionAmbientPresentationBlob ambient =
                    ref definition.AmbientPresentations[index];
                if (ambient.InstanceCount <= 0 || !TryResolveRouteContract(
                        ref map, ref ambient, out _, out _))
                    return false;
                instanceCount += ambient.InstanceCount;
                if (instanceCount > MaxCivilianPresentations)
                    return false;
            }

            return instanceCount > 0;
        }

        private static bool EnsurePresentation(
            ref SystemState state, EntityQuery query, EntityQuery prefabRegistryQuery,
            in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionDefinitionBlob definition,
            ref OperationMapBlob map,
            int instanceCount)
        {
            EntityManager em = state.EntityManager;
            if (query.CalculateEntityCount() == instanceCount)
            {
                bool matches = true;
                using NativeArray<CampaignMissionAmbientCivilianComponent> civilians =
                    query.ToComponentDataArray<CampaignMissionAmbientCivilianComponent>(Allocator.Temp);
                for (int i = 0; i < civilians.Length; i++)
                {
                    matches &= civilians[i].SessionToken.Equals(runtime.SessionToken) &&
                               civilians[i].AttemptOrdinal == runtime.AttemptOrdinal;
                }
                if (matches) return true;
            }

            if (!query.IsEmptyIgnoreFilter)
                DestroyIndividually(em, query);
            for (int presentationIndex = 0;
                 presentationIndex < definition.AmbientPresentations.Length;
                 presentationIndex++)
            {
                ref CampaignMissionAmbientPresentationBlob ambient =
                    ref definition.AmbientPresentations[presentationIndex];
                if (!TryResolveRouteContract(
                        ref map, ref ambient, out byte presentationKind, out AmbientRouteAnchors anchors))
                {
                    DestroyIndividually(em, query);
                    return false;
                }

                FixedList128Bytes<Entity> prefabs = ResolvePresentationPrefabs(
                    em, prefabRegistryQuery, presentationKind);
                if (prefabs.Length != CivilianPrefabKeyCount)
                {
                    DestroyIndividually(em, query);
                    return false;
                }

                for (int instanceIndex = 0; instanceIndex < ambient.InstanceCount; instanceIndex++)
                {
                    AmbientRoute route = CreateAmbientRoute(
                        presentationKind,
                        in anchors,
                        instanceIndex,
                        runtime.DeterministicSeed);
                    Entity entity = em.Instantiate(prefabs[instanceIndex % prefabs.Length]);
                    StripGameplayComponents(em, entity);
                    if (presentationKind != BasePersonnelPresentationKind)
                        SetOrAdd(em, entity, new CivilianUnitTag());
                    else
                        RemoveIfPresent<CivilianUnitTag>(em, entity);
                    SetOrAdd(em, entity, new UnitForceDetailedVisualTag());
                    SetOrAdd(em, entity, new CampaignMissionAmbientCivilianComponent
                    {
                        PresentationId = ambient.PresentationId,
                        RouteId = ambient.RouteId,
                        SessionToken = runtime.SessionToken,
                        RouteIndex = route.RouteIndex,
                        AttemptOrdinal = runtime.AttemptOrdinal,
                        Evacuating = presentationKind == PanicCivilianPresentationKind ? (byte)1 : (byte)0
                    });
                    SetOrAdd(em, entity, new CampaignMissionAmbientCivilianMotionComponent
                    {
                        AlleyMerge = route.AlleyMerge,
                        SquadPass = route.SquadPass,
                        Exit = route.Exit,
                        Speed = route.Speed,
                        DelaySeconds = route.DelaySeconds,
                        Loop = route.Loop
                    });
                    SetOrAdd(em, entity, new UnitPrevWorldPos { Value = route.Start });
                    SetOrAdd(em, entity, new UnitMoveVisualComponent());
                    SetLocomotionAnimation(
                        em,
                        entity,
                        presentationKind == PanicCivilianPresentationKind
                            ? UnitAnimationKind.Run
                            : UnitAnimationKind.Walk);
                    em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                        route.Start,
                        quaternion.LookRotationSafe(route.AlleyMerge - route.Start, math.up()),
                        1f));
                }
            }
            return true;
        }

        private static FixedList128Bytes<Entity> ResolvePresentationPrefabs(
            EntityManager em,
            EntityQuery query,
            byte presentationKind)
        {
            FixedList128Bytes<Entity> result = default;
            if (query.CalculateEntityCount() != 1)
                return result;
            DynamicBuffer<UnitPrefabRegistryEntry> registry =
                em.GetBuffer<UnitPrefabRegistryEntry>(query.GetSingletonEntity());
            for (int keyIndex = 0; keyIndex < CivilianPrefabKeyCount; keyIndex++)
            {
                FixedString64Bytes key = PresentationPrefabKey(presentationKind, keyIndex);
                for (int i = 0; i < registry.Length; i++)
                {
                    Entity prefab = registry[i].Prefab;
                    if (prefab != Entity.Null && em.Exists(prefab) &&
                        em.HasComponent<UnitSourcePrefabKey>(prefab) &&
                        em.GetComponentData<UnitSourcePrefabKey>(prefab).Value.Equals(key))
                    {
                        result.Add(prefab);
                        break;
                    }
                }
            }
            return result;
        }

        private static void StripGameplayComponents(EntityManager em, Entity entity)
        {
            RemoveIfPresent<Faction>(em, entity);
            RemoveIfPresent<UnitHealth>(em, entity);
            RemoveIfPresent<UnitCombat>(em, entity);
            RemoveIfPresent<UnitAttack>(em, entity);
            RemoveIfPresent<ThreatDetector>(em, entity);
            RemoveIfPresent<SelectedUnitTag>(em, entity);
            RemoveIfPresent<EngageTarget>(em, entity);
            RemoveIfPresent<UnitTarget>(em, entity);
            RemoveIfPresent<AIControlledTag>(em, entity);
            RemoveIfPresent<ManualControlledTag>(em, entity);
            RemoveIfPresent<UnitGrid>(em, entity);
            RemoveIfPresent<UnitMove>(em, entity);
            RemoveIfPresent<UnitFootprint>(em, entity);
            RemoveIfPresent<UnitMovementBehavior>(em, entity);
            RemoveIfPresent<UnitPathRequest>(em, entity);
            RemoveIfPresent<UnitPathFollow>(em, entity);
            RemoveIfPresent<UnitPathRange>(em, entity);
            RemoveIfPresent<UnitLongDistanceMove>(em, entity);
            RemoveIfPresent<UnitIdleWanderComponent>(em, entity);
            RemoveIfPresent<AutoWanderMoveTag>(em, entity);
            RemoveIfPresent<ManualMoveOrderTag>(em, entity);
            RemoveIfPresent<UnitMidLodPrefabReference>(em, entity);
            RemoveIfPresent<UnitLowLodPrefabReference>(em, entity);
        }

        private static void RemoveIfPresent<T>(EntityManager em, Entity entity)
            where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(entity)) em.RemoveComponent<T>(entity);
        }

        private static void SetOrAdd<T>(EntityManager em, Entity entity, T value)
            where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(entity)) em.SetComponentData(entity, value);
            else em.AddComponentData(entity, value);
        }

        private static void Cleanup(
            ref SystemState state, EntityQuery query,
            FixedString64Bytes sessionToken, int attemptOrdinal)
        {
            EntityManager em = state.EntityManager;
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            using NativeArray<CampaignMissionAmbientCivilianComponent> civilians =
                query.ToComponentDataArray<CampaignMissionAmbientCivilianComponent>(Allocator.Temp);
            NativeList<Entity> stale = new(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (attemptOrdinal < 0 || !civilians[i].SessionToken.Equals(sessionToken) ||
                    civilians[i].AttemptOrdinal != attemptOrdinal)
                    stale.Add(entities[i]);
            }
            for (int i = 0; i < stale.Length; i++)
            {
                if (em.Exists(stale[i]))
                    em.DestroyEntity(stale[i]);
            }
            stale.Dispose();
        }

        private static void DestroyIndividually(EntityManager entityManager, EntityQuery query)
        {
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (entityManager.Exists(entities[i]))
                    entityManager.DestroyEntity(entities[i]);
            }
        }

    }

    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CampaignMissionPatrolOrderSystem))]
    [UpdateBefore(typeof(UnitSurfaceTrackingSystem))]
    public partial struct CampaignMissionAmbientCivilianMotionSystem : ISystem
    {
        private EntityQuery _ecbSingletonQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder = new(Allocator.Temp);
            _ecbSingletonQuery = builder
                .WithAll<EndSimulationEntityCommandBufferSystem.Singleton>()
                .WithOptions(EntityQueryOptions.IncludeSystems)
                .Build(ref state);
            builder.Dispose();
            state.RequireForUpdate<CampaignMissionAmbientCivilianMotionComponent>();
            state.RequireForUpdate(_ecbSingletonQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton(out CampaignMissionOpeningPresentationComponent opening))
                return;

            Entity ecbEntity = _ecbSingletonQuery.GetSingletonEntity();
            EndSimulationEntityCommandBufferSystem.Singleton ecbSystem =
                state.EntityManager.GetComponentData<EndSimulationEntityCommandBufferSystem.Singleton>(ecbEntity);
            state.Dependency = new MoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        private partial struct MoveJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(
                [EntityIndexInQuery] int sortKey,
                Entity entity,
                ref LocalTransform transform,
                ref CampaignMissionAmbientCivilianMotionComponent motion,
                ref UnitMoveVisualComponent moveVisual)
            {
                if (motion.DelaySeconds > 0f)
                {
                    motion.DelaySeconds = math.max(0f, motion.DelaySeconds - DeltaTime);
                    moveVisual.IsMoving = 0;
                    moveVisual.StillSeconds = 0f;
                    return;
                }

                float3 target = motion.Segment switch
                {
                    0 => motion.AlleyMerge,
                    1 => motion.SquadPass,
                    _ => motion.Exit
                };
                float3 delta = target - transform.Position;
                delta.y = 0f;
                float distance = math.length(delta);
                float step = math.max(0f, motion.Speed * DeltaTime);
                if (distance <= math.max(0.12f, step))
                {
                    float3 position = target;
                    position.y = transform.Position.y;
                    transform.Position = position;
                    motion.Segment++;
                    moveVisual.IsMoving = 1;
                    moveVisual.StillSeconds = 0f;
                    if (motion.Segment > 2)
                    {
                        if (motion.Loop != 0)
                            motion.Segment = 0;
                        else
                            Ecb.DestroyEntity(sortKey, entity);
                    }
                    return;
                }

                float3 direction = delta / math.max(distance, 0.0001f);
                float3 next = transform.Position + direction * step;
                next.y = transform.Position.y;
                transform.Position = next;
                quaternion facing = quaternion.LookRotationSafe(direction, math.up());
                transform.Rotation = math.slerp(
                    transform.Rotation,
                    facing,
                    math.saturate(DeltaTime * 10f));
                moveVisual.IsMoving = 1;
                moveVisual.StillSeconds = 0f;
            }
        }
    }
}
