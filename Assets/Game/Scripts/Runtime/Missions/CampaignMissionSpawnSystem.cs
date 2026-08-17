using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(CampaignMissionLaunchSystem))]
    public partial struct CampaignMissionSpawnSystem : ISystem
    {
        private EntityQuery _registryQuery;
        private EntityQuery _cameraFocusQuery;

        public void OnCreate(ref SystemState state)
        {
            _registryQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(), ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            _cameraFocusQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeCameraFocusRequestComponent>());
            state.RequireForUpdate<CampaignMissionRootComponent>();
            state.RequireForUpdate<OperationMapMetadataComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplayState) &&
                gameplayState.PlayRequested == 0)
                return;

            if (_registryQuery.IsEmptyIgnoreFilter ||
                !SystemAPI.TryGetSingleton(out OperationMapMetadataComponent metadata) || !metadata.Blob.IsCreated)
                return;
            EntityManager em = state.EntityManager;
            DynamicBuffer<UnitPrefabRegistryEntry> registry =
                em.GetBuffer<UnitPrefabRegistryEntry>(_registryQuery.GetSingletonEntity());
            NativeArray<Entity> prefabs = new(registry.Length, Allocator.Temp);
            for (int i = 0; i < registry.Length; i++) prefabs[i] = registry[i].Prefab;
            Entity root = Entity.Null;
            CampaignMissionCatalogComponent rootCatalog = default;
            CampaignMissionRuntimeComponent rootRuntime = default;
            CampaignMissionAttemptFactsComponent rootFacts = default;
            foreach ((RefRO<CampaignMissionCatalogComponent> catalogRef,
                      RefRO<CampaignMissionRuntimeComponent> runtimeRef,
                      RefRO<CampaignMissionAttemptFactsComponent> factsRef,
                      Entity entity)
                     in SystemAPI.Query<RefRO<CampaignMissionCatalogComponent>,
                         RefRO<CampaignMissionRuntimeComponent>, RefRO<CampaignMissionAttemptFactsComponent>>()
                         .WithEntityAccess())
            {
                root = entity;
                rootCatalog = catalogRef.ValueRO;
                rootRuntime = runtimeRef.ValueRO;
                rootFacts = factsRef.ValueRO;
                break;
            }
            if (root == Entity.Null || rootFacts.CommandSquadSpawned != 0 ||
                !TryFindDefinition(in rootCatalog, in rootRuntime, out int definitionIndex))
            {
                prefabs.Dispose();
                return;
            }
            ref CampaignMissionDefinitionBlob definition = ref rootCatalog.Blob.Value.Missions[definitionIndex];
            if (!HasRequiredRestrictions(ref definition) ||
                !TryValidate(em, prefabs, ref definition, ref metadata.Blob.Value))
            {
                prefabs.Dispose();
                return;
            }
            SpawnAll(
                em,
                prefabs,
                ref definition,
                ref metadata.Blob.Value,
                in rootRuntime,
                out float3 playerFocus,
                out float3 hostileFocus);
            prefabs.Dispose();
            RequestOpeningHostileCameraFocus(em, hostileFocus);
            SetOrAdd(em, root, new CampaignMissionOpeningPresentationComponent
            {
                SessionToken = rootRuntime.SessionToken,
                FriendlyFocus = playerFocus,
                ElapsedMilliseconds = 0,
                Stage = 1
            });
            rootFacts.CommandSquadSpawned = 1;
            rootFacts.CommandSquadAlive = 1;
            rootFacts.HostileTotalCount = CountHostiles(ref definition);
            em.SetComponentData(root, rootFacts);
        }

        private static bool TryValidate(
            EntityManager em, NativeArray<Entity> prefabs,
            ref CampaignMissionDefinitionBlob definition, ref OperationMapBlob map)
        {
            int total = 0;
            for (int groupIndex = 0; groupIndex < definition.ForceGroups.Length; groupIndex++)
            {
                ref CampaignMissionForceGroupBlob group = ref definition.ForceGroups[groupIndex];
                for (int unitIndex = 0; unitIndex < group.Units.Length; unitIndex++)
                {
                    ref CampaignMissionForceUnitBlob unit = ref group.Units[unitIndex];
                    total += unit.Count;
                    if (unit.Count < 1 || unit.RuntimePrefabSourceKey.IsEmpty ||
                        IsForbidden(unit.SourceKey) ||
                        !TryFindAnchor(ref map, unit.SpawnAnchorId, out _) ||
                        !TryResolvePrefab(
                            em, prefabs, unit.RuntimePrefabSourceKey, out _)) return false;
                }
            }
            return total == 7 && CountHostiles(ref definition) == 3;
        }

        private static void SpawnAll(
            EntityManager em, NativeArray<Entity> prefabs,
            ref CampaignMissionDefinitionBlob definition, ref OperationMapBlob map,
            in CampaignMissionRuntimeComponent runtime,
            out float3 playerFocus,
            out float3 hostileFocus)
        {
            int ordinal = 0;
            float3 playerPositionSum = float3.zero;
            float3 hostilePositionSum = float3.zero;
            int playerCount = 0;
            int hostileCount = 0;
            for (int groupIndex = 0; groupIndex < definition.ForceGroups.Length; groupIndex++)
            {
                ref CampaignMissionForceGroupBlob group = ref definition.ForceGroups[groupIndex];
                FixedString64Bytes routeId = FindRouteForGroup(ref definition, group.GroupId);
                for (int unitIndex = 0; unitIndex < group.Units.Length; unitIndex++)
                {
                    ref CampaignMissionForceUnitBlob unit = ref group.Units[unitIndex];
                    TryResolvePrefab(
                        em, prefabs, unit.RuntimePrefabSourceKey, out Entity prefab);
                    TryFindAnchor(
                        ref map, unit.SpawnAnchorId, out OperationMapAnchorBlob anchor);
                    for (int count = 0; count < unit.Count; count++)
                    {
                        Entity instance = em.Instantiate(prefab);
                        if (em.HasComponent<SelectedUnitTag>(instance))
                            em.RemoveComponent<SelectedUnitTag>(instance);
                        float3 position = OffsetInsideAnchor(
                            anchor.Position, anchor.Radius, ordinal++, runtime.DeterministicSeed);
                        SetOrAdd(
                            em, instance, LocalTransform.FromPositionRotationScale(position, anchor.Rotation, 1f));
                        SetOrAdd(em, instance, new UnitGrid { Cell = ToGridCell(position, map.Grid) });
                        SetOrAdd(em, instance, new UnitPrevWorldPos { Value = position });
                        SetOrAdd(em, instance, new UnitMoveVisualComponent());
                        SetOrAdd(em, instance, new Faction { Id = group.FactionId });
                        SetOrAdd(em, instance,
                            new UnitSourcePrefabKey { Value = unit.RuntimePrefabSourceKey });
                        SetOrAdd(em, instance, new CampaignMissionUnitRoleComponent
                        {
                            MissionRoleId = unit.MissionRoleId, UnitGroupId = group.GroupId, RouteId = routeId,
                            SessionToken = runtime.SessionToken
                        });
                        if (FactionIdentity.IsPlayerControlled(group.FactionId))
                        {
                            playerPositionSum += position;
                            playerCount++;
                        }
                        else
                        {
                            hostilePositionSum += position;
                            hostileCount++;
                        }
                    }
                }
            }

            playerFocus = playerCount > 0
                ? playerPositionSum / playerCount
                : float3.zero;
            hostileFocus = hostileCount > 0
                ? hostilePositionSum / hostileCount
                : playerFocus;
        }

        private void RequestOpeningHostileCameraFocus(EntityManager em, float3 hostileFocus)
        {
            if (_cameraFocusQuery.CalculateEntityCount() != 1)
                return;

            Entity focusEntity = _cameraFocusQuery.GetSingletonEntity();
            em.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
            {
                Requested = 1,
                Smooth = 0,
                UseTacticalRevealZoom = 1,
                World = hostileFocus
            });
        }

        internal static bool TryFindDefinition(
            in CampaignMissionCatalogComponent catalog, in CampaignMissionRuntimeComponent runtime, out int index)
        {
            index = -1;
            if (!catalog.Blob.IsCreated) return false;
            ref CampaignMissionCatalogBlob blob = ref catalog.Blob.Value;
            for (int i = 0; i < blob.Missions.Length; i++)
            {
                ref CampaignMissionDefinitionBlob candidate = ref blob.Missions[i];
                if (candidate.MissionId.Equals(runtime.MissionId) && candidate.ScenarioId.Equals(runtime.ScenarioId) &&
                    candidate.OperationMapId.Equals(runtime.OperationMapId)) { index = i; return true; }
            }
            return false;
        }

        internal static bool TryFindAnchor(
            ref OperationMapBlob map, FixedString64Bytes id, out OperationMapAnchorBlob anchor)
        {
            for (int i = 0; i < map.Anchors.Length; i++)
                if (map.Anchors[i].Id.Equals(id)) { anchor = map.Anchors[i]; return true; }
            anchor = default;
            return false;
        }

        private static bool TryResolvePrefab(
            EntityManager em, NativeArray<Entity> prefabs,
            FixedString64Bytes sourceKey, out Entity prefab)
        {
            for (int i = 0; i < prefabs.Length; i++)
            {
                Entity candidate = prefabs[i];
                if (candidate != Entity.Null && em.Exists(candidate) && em.HasComponent<UnitSourcePrefabKey>(candidate) &&
                    em.GetComponentData<UnitSourcePrefabKey>(candidate).Value.Equals(sourceKey))
                { prefab = candidate; return true; }
            }
            prefab = Entity.Null;
            return false;
        }

        private static bool HasRequiredRestrictions(ref CampaignMissionDefinitionBlob definition) =>
            definition.BuildingDisabled != 0 && definition.ProductionDisabled != 0 &&
            definition.EconomyDisabled != 0 && definition.TransportDisabled != 0 && definition.AirDisabled != 0;

        private static int CountHostiles(ref CampaignMissionDefinitionBlob definition)
        {
            int count = 0;
            for (int groupIndex = 0; groupIndex < definition.ForceGroups.Length; groupIndex++)
            {
                ref CampaignMissionForceGroupBlob group = ref definition.ForceGroups[groupIndex];
                if (group.FactionId <= 1) continue;
                for (int unitIndex = 0; unitIndex < group.Units.Length; unitIndex++) count += group.Units[unitIndex].Count;
            }
            return count;
        }

        private static FixedString64Bytes FindRouteForGroup(
            ref CampaignMissionDefinitionBlob definition, FixedString64Bytes groupId)
        {
            for (int i = 0; i < definition.PatrolRoutes.Length; i++)
                if (definition.PatrolRoutes[i].UnitGroupId.Equals(groupId)) return definition.PatrolRoutes[i].RouteId;
            return default;
        }

        private static bool IsForbidden(FixedString64Bytes key)
        {
            string value = key.ToString();
            return value.Contains("qassem") || value.Contains("heavy_gunner") || value.Contains("male_05") ||
                   value.Equals("unit.ash.male_02");
        }

        private static float3 OffsetInsideAnchor(float3 center, float radius, int ordinal, int seed)
        {
            uint hash = math.hash(new int2(seed, ordinal + 1));
            float angle = (hash & 1023u) * (2f * math.PI / 1024f);
            float distance = math.min(math.max(0.35f, radius * 0.45f), math.max(0.35f, radius - 0.25f));
            return center + new float3(math.cos(angle) * distance, 0f, math.sin(angle) * distance);
        }

        internal static int2 ToGridCell(float3 position, in OperationMapGridBlob grid) =>
            (int2)math.round((position.xz - grid.Origin.xz) / math.max(0.001f, grid.CellSize));

        private static void SetOrAdd<T>(EntityManager em, Entity entity, T value)
            where T : unmanaged, IComponentData
        {
            if (em.HasComponent<T>(entity)) em.SetComponentData(entity, value);
            else em.AddComponentData(entity, value);
        }
    }
}
