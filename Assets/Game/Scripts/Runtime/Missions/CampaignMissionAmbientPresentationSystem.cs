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
            if (!TryResolveContract(ref definition, ref metadata.Blob.Value, out var ambient,
                    out OperationMapAnchorBlob player, out OperationMapAnchorBlob hostile))
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
                ref ambient, in player, in hostile);
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

        private static bool TryResolveContract(
            ref CampaignMissionDefinitionBlob definition, ref OperationMapBlob map,
            out CampaignMissionAmbientPresentationBlob ambient,
            out OperationMapAnchorBlob player, out OperationMapAnchorBlob hostile)
        {
            ambient = default;
            player = default;
            hostile = default;
            if (definition.AmbientPresentations.Length != 1)
                return false;
            ambient = definition.AmbientPresentations[0];
            return ambient.InstanceCount is > 0 and <= MaxCivilianPresentations &&
                   !ambient.PresentationId.IsEmpty && !ambient.RouteId.IsEmpty &&
                   CampaignMissionSpawnSystem.TryFindAnchor(ref map, ambient.AnchorId, out _) &&
                   CampaignMissionSpawnSystem.TryFindAnchor(
                       ref map, new FixedString64Bytes("anchor.ch01.m01.civilian_evacuation"), out _) &&
                   CampaignMissionSpawnSystem.TryFindAnchor(
                       ref map, new FixedString64Bytes("anchor.ch01.m01.player_spawn"), out player) &&
                   CampaignMissionSpawnSystem.TryFindAnchor(
                       ref map, new FixedString64Bytes("anchor.ch01.m01.patrol_spawn"), out hostile);
        }

        private static bool EnsurePresentation(
            ref SystemState state, EntityQuery query, EntityQuery prefabRegistryQuery,
            in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionAmbientPresentationBlob ambient,
            in OperationMapAnchorBlob player, in OperationMapAnchorBlob hostile)
        {
            EntityManager em = state.EntityManager;
            if (query.CalculateEntityCount() == ambient.InstanceCount)
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
            FixedList128Bytes<Entity> prefabs = ResolveCivilianPrefabs(em, prefabRegistryQuery);
            if (prefabs.Length != CivilianPrefabKeyCount)
                return false;
            for (int i = 0; i < ambient.InstanceCount; i++)
            {
                PanicRoute route = CreatePanicRoute(in player, in hostile, i, runtime.DeterministicSeed);
                Entity entity = em.Instantiate(prefabs[i % prefabs.Length]);
                StripGameplayComponents(em, entity);
                SetOrAdd(em, entity, new CivilianUnitTag());
                SetOrAdd(em, entity, new UnitForceDetailedVisualTag());
                SetOrAdd(em, entity, new CampaignMissionAmbientCivilianComponent
                {
                    PresentationId = ambient.PresentationId, RouteId = ambient.RouteId,
                    SessionToken = runtime.SessionToken, RouteIndex = route.RouteIndex,
                    AttemptOrdinal = runtime.AttemptOrdinal, Evacuating = 1
                });
                SetOrAdd(em, entity, new CampaignMissionAmbientCivilianMotionComponent
                {
                    AlleyMerge = route.AlleyMerge,
                    SquadPass = route.SquadPass,
                    Exit = route.Exit,
                    Speed = route.Speed,
                    DelaySeconds = route.DelaySeconds
                });
                SetOrAdd(em, entity, new UnitPrevWorldPos { Value = route.Start });
                SetOrAdd(em, entity, new UnitMoveVisualComponent());
                SetRunAnimation(em, entity);
                em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                    route.Start,
                    quaternion.LookRotationSafe(route.AlleyMerge - route.Start, math.up()),
                    1f));
            }
            return true;
        }

        private static FixedList128Bytes<Entity> ResolveCivilianPrefabs(
            EntityManager em, EntityQuery query)
        {
            FixedList128Bytes<Entity> result = default;
            if (query.CalculateEntityCount() != 1)
                return result;
            DynamicBuffer<UnitPrefabRegistryEntry> registry =
                em.GetBuffer<UnitPrefabRegistryEntry>(query.GetSingletonEntity());
            for (int keyIndex = 0; keyIndex < CivilianPrefabKeyCount; keyIndex++)
            {
                FixedString64Bytes key = CivilianPrefabKey(keyIndex);
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

        private static FixedString64Bytes CivilianPrefabKey(int index) => index switch
        {
            0 => new FixedString64Bytes("Unit_Chr_Civilian_Male_01"),
            1 => new FixedString64Bytes("Unit_Chr_Civilian_Female_01"),
            2 => new FixedString64Bytes("Unit_Chr_Civilian_Male_02"),
            _ => new FixedString64Bytes("Unit_Chr_Civilian_Female_02")
        };

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

        private static void SetRunAnimation(EntityManager em, Entity entity)
        {
            if (!em.HasBuffer<UnitAnimationOrderEntry>(entity))
                return;

            DynamicBuffer<UnitAnimationOrderEntry> animationOrder = em.GetBuffer<UnitAnimationOrderEntry>(entity);
            byte animationIndex = byte.MaxValue;
            for (int i = 0; i < animationOrder.Length; i++)
            {
                if (animationOrder[i].Kind == (byte)UnitAnimationKind.Run)
                {
                    animationIndex = (byte)((byte)UnitAnimationKind.Run + 1);
                    break;
                }
                if (animationIndex == byte.MaxValue && animationOrder[i].Kind == (byte)UnitAnimationKind.Walk)
                    animationIndex = (byte)((byte)UnitAnimationKind.Walk + 1);
            }

            if (animationIndex != byte.MaxValue)
                SetOrAdd(em, entity, new UnitResolvedAnimationIndex { Value = animationIndex, Changed = 1, Updated = 1 });
        }

        private static PanicRoute CreatePanicRoute(
            in OperationMapAnchorBlob player,
            in OperationMapAnchorBlob hostile,
            int ordinal,
            int seed)
        {
            uint hashA = math.hash(new int3(seed ^ 0x51A7, ordinal + 1, 0x2B));
            uint hashB = math.hash(new int3(seed ^ 0x2C31, ordinal + 1, 0x73));
            float3 towardSquad = math.normalizesafe(
                player.Position - hostile.Position,
                new float3(0f, 0f, -1f));
            towardSquad.y = 0f;
            towardSquad = math.normalizesafe(towardSquad, new float3(0f, 0f, -1f));
            float3 lateral = new(towardSquad.z, 0f, -towardSquad.x);
            int routeIndex = ordinal & 7;
            float side = (routeIndex & 1) == 0 ? 1f : -1f;
            float alongJitter = SignedUnit(hashA, 0);
            float lateralJitter = SignedUnit(hashA, 8);
            float waypointJitter = SignedUnit(hashA, 16);
            float exitJitter = SignedUnit(hashB, 8);
            float speedJitter = UnsignedUnit(hashB, 24);

            float3 start;
            float3 alleyMerge;
            if (routeIndex <= 1)
            {
                start = hostile.Position + towardSquad * (7f + alongJitter * 5f) +
                        lateral * (side * (17f + lateralJitter * 3f));
                alleyMerge = hostile.Position + towardSquad * (18f + waypointJitter * 3f) +
                             lateral * (side * (7f + lateralJitter * 2f));
            }
            else if (routeIndex <= 3)
            {
                start = hostile.Position + towardSquad * (5f + alongJitter * 8f) +
                        lateral * (side * (9f + lateralJitter * 3f));
                alleyMerge = hostile.Position + towardSquad * (19f + waypointJitter * 4f) +
                             lateral * (side * (4f + lateralJitter * 2f));
            }
            else if (routeIndex <= 5)
            {
                start = hostile.Position + towardSquad * (3f + alongJitter * 6f) +
                        lateral * (side * (4.5f + lateralJitter * 2f));
                alleyMerge = hostile.Position + towardSquad * (20f + waypointJitter * 4f) +
                             lateral * (side * (6f + lateralJitter * 3f));
            }
            else
            {
                start = hostile.Position + towardSquad * (4f + alongJitter * 8f) +
                        lateral * (side * (15f + lateralJitter * 4f));
                alleyMerge = hostile.Position + towardSquad * (11f + waypointJitter * 5f) +
                             lateral * (side * (27f + lateralJitter * 5f));
            }

            bool towardFriendlyLine = routeIndex <= 5;
            float3 squadPass = towardFriendlyLine
                ? player.Position - towardSquad * (10f + waypointJitter * 5f) +
                  lateral * (side * (8f + lateralJitter * 6f))
                : hostile.Position + towardSquad * (19f + waypointJitter * 5f) +
                  lateral * (side * (38f + lateralJitter * 6f));
            float3 exit = towardFriendlyLine
                ? player.Position + towardSquad * (27f + exitJitter * 7f) +
                  lateral * (side * (19f + lateralJitter * 8f))
                : player.Position - towardSquad * (2f + exitJitter * 7f) +
                  lateral * (side * (55f + lateralJitter * 8f));

            return new PanicRoute
            {
                Start = start,
                AlleyMerge = alleyMerge,
                SquadPass = squadPass,
                Exit = exit,
                Speed = 6.4f + speedJitter * 1.6f,
                DelaySeconds = 0f,
                RouteIndex = routeIndex
            };
        }

        private static float SignedUnit(uint hash, int shift) =>
            UnsignedUnit(hash, shift) * 2f - 1f;

        private static float UnsignedUnit(uint hash, int shift) =>
            ((hash >> shift) & 255u) / 255f;

        private struct PanicRoute
        {
            public float3 Start;
            public float3 AlleyMerge;
            public float3 SquadPass;
            public float3 Exit;
            public float Speed;
            public float DelaySeconds;
            public int RouteIndex;
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
                        Ecb.DestroyEntity(sortKey, entity);
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
