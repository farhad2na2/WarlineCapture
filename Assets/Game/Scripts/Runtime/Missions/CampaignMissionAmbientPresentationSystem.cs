using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(CampaignMissionObjectiveProjectionSystem))]
    public partial struct CampaignMissionAmbientPresentationSystem : ISystem
    {
        internal const int MaxCivilianPresentations = 12;
        private const int CivilianPrefabKeyCount = 4;
        private EntityQuery _ambientQuery;
        private EntityQuery _prefabRegistryQuery;
        private FixedString64Bytes _processedSessionToken;
        private int _processedAttemptOrdinal;
        private uint _processedSourceVersion;
        private byte _processedEvacuating;
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
                Cleanup(ref state, default, -1);
                return;
            }

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (!TryResolveContract(ref definition, ref metadata.Blob.Value, out var ambient,
                    out OperationMapAnchorBlob safe, out OperationMapAnchorBlob evacuation))
            {
                Cleanup(ref state, default, -1);
                return;
            }

            bool sameAttempt = _processedSessionToken.Equals(runtime.SessionToken) &&
                               _processedAttemptOrdinal == runtime.AttemptOrdinal;
            bool evacuating = runtime.Outcome == MissionOutcomeKind.Victory ||
                              runtime.Phase == MissionPhaseKind.SecureCorridor ||
                              (sameAttempt && _processedEvacuating != 0);
            if (sameAttempt &&
                _processedSourceVersion == runtime.SourceVersion &&
                _processedEvacuating == (evacuating ? (byte)1 : (byte)0) &&
                (_ambientQuery.CalculateEntityCount() == ambient.InstanceCount ||
                 (_capacityUnavailable != 0 && _ambientQuery.IsEmptyIgnoreFilter)))
                return;

            Cleanup(ref state, runtime.SessionToken, runtime.AttemptOrdinal);
            _capacityUnavailable = EnsurePresentation(
                ref state, _ambientQuery, _prefabRegistryQuery, in runtime,
                ref ambient, in safe, in evacuation, evacuating) ? (byte)0 : (byte)1;
            _processedSessionToken = runtime.SessionToken;
            _processedAttemptOrdinal = runtime.AttemptOrdinal;
            _processedSourceVersion = runtime.SourceVersion;
            _processedEvacuating = evacuating ? (byte)1 : (byte)0;
        }

        public void OnDestroy(ref SystemState state)
        {
            if (!_ambientQuery.IsEmptyIgnoreFilter)
                state.EntityManager.DestroyEntity(_ambientQuery);
        }

        private static bool TryResolveContract(
            ref CampaignMissionDefinitionBlob definition, ref OperationMapBlob map,
            out CampaignMissionAmbientPresentationBlob ambient,
            out OperationMapAnchorBlob safe, out OperationMapAnchorBlob evacuation)
        {
            ambient = default;
            safe = default;
            evacuation = default;
            if (definition.AmbientPresentations.Length != 1)
                return false;
            ambient = definition.AmbientPresentations[0];
            return ambient.InstanceCount is > 0 and <= MaxCivilianPresentations &&
                   !ambient.PresentationId.IsEmpty && !ambient.RouteId.IsEmpty &&
                   CampaignMissionSpawnSystem.TryFindAnchor(ref map, ambient.AnchorId, out safe) &&
                   CampaignMissionSpawnSystem.TryFindAnchor(
                       ref map, new FixedString64Bytes("anchor.ch01.m01.civilian_evacuation"), out evacuation);
        }

        private static bool EnsurePresentation(
            ref SystemState state, EntityQuery query, EntityQuery prefabRegistryQuery,
            in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionAmbientPresentationBlob ambient,
            in OperationMapAnchorBlob safe, in OperationMapAnchorBlob evacuation, bool evacuating)
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
                               civilians[i].AttemptOrdinal == runtime.AttemptOrdinal &&
                               civilians[i].Evacuating == (evacuating ? (byte)1 : (byte)0);
                }
                if (matches) return true;
            }

            if (!query.IsEmptyIgnoreFilter)
                em.DestroyEntity(query);
            FixedList128Bytes<Entity> prefabs = ResolveOptionalPrefabs(em, prefabRegistryQuery);
            if (prefabs.IsEmpty)
                return false;
            float3 center = evacuating ? evacuation.Position : safe.Position;
            float radius = evacuating ? evacuation.Radius : safe.Radius;
            for (int i = 0; i < ambient.InstanceCount; i++)
            {
                Entity entity = em.Instantiate(prefabs[i % prefabs.Length]);
                StripGameplayComponents(em, entity);
                SetOrAdd(em, entity, new CivilianUnitTag());
                SetOrAdd(em, entity, new CampaignMissionAmbientCivilianComponent
                {
                    PresentationId = ambient.PresentationId, RouteId = ambient.RouteId,
                    SessionToken = runtime.SessionToken, RouteIndex = evacuating ? 1 : 0,
                    AttemptOrdinal = runtime.AttemptOrdinal, Evacuating = evacuating ? (byte)1 : (byte)0
                });
                em.SetComponentData(entity, LocalTransform.FromPositionRotationScale(
                    Position(center, radius, i, runtime.DeterministicSeed),
                    evacuating ? evacuation.Rotation : safe.Rotation, 1f));
            }
            return true;
        }

        private static FixedList128Bytes<Entity> ResolveOptionalPrefabs(
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
            ref SystemState state, FixedString64Bytes sessionToken, int attemptOrdinal)
        {
            EntityManager em = state.EntityManager;
            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionAmbientCivilianComponent>());
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
            if (stale.Length > 0)
                em.DestroyEntity(stale.AsArray());
            stale.Dispose();
        }

        private static float3 Position(float3 center, float radius, int ordinal, int seed)
        {
            uint hash = math.hash(new int2(seed ^ 0x51A7, ordinal + 1));
            float angle = (hash & 2047u) * (2f * math.PI / 2048f);
            float distance = math.min(math.max(0.6f, radius * 0.55f), math.max(0.6f, radius - 0.3f));
            return center + new float3(math.cos(angle) * distance, 0f, math.sin(angle) * distance);
        }
    }
}
