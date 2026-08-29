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
        private static readonly FixedString64Bytes FirstContactMissionId = "saga.ch01.m01.first_contact";
        internal const float FirstContactHostileMinimumAttackRange = 60f;
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
            ResolveOpeningPresentationFocus(
                rootRuntime.MissionId,
                ref metadata.Blob.Value,
                playerFocus,
                hostileFocus,
                out float3 openingStartFocus,
                out float3 openingEndFocus,
                out float3 establishingFocus);
            CampaignMissionOpeningPresentationComponent opening = new()
            {
                SessionToken = rootRuntime.SessionToken,
                FriendlyFocus = openingStartFocus,
                HostileFocus = openingEndFocus,
                EstablishingFocus = establishingFocus,
                ElapsedMilliseconds = 0,
                // The HUD/camera composition initializes after mission entities. Stage zero
                // deliberately defers the establishing shot until that composition is visible,
                // preventing the normal match-intro zoom from overwriting the bazaar handoff.
                Stage = 0
            };
            if (_cameraFocusQuery.CalculateEntityCount() == 1)
            {
                QueueInitialRtsOverview(
                    em,
                    _cameraFocusQuery.GetSingletonEntity(),
                    openingStartFocus);
                opening.InitialRtsOverviewRequested = 1;
            }
            SetOrAdd(em, root, opening);
            bool finaleRequired = rootRuntime.MissionId.Equals(FirstContactMissionId) &&
                                  (rootRuntime.RunKind == Game.Missions.Contracts.MissionRunKind.FirstClear ||
                                   rootRuntime.ReplayTutorialEnabled != 0);
            SetOrAdd(em, root, new CampaignMissionFinalePresentationComponent
            {
                SessionToken = rootRuntime.SessionToken,
                FriendlyFocus = playerFocus,
                HostileFocus = hostileFocus,
                ElapsedMilliseconds = 0,
                Required = finaleRequired ? (byte)1 : (byte)0,
                Stage = 0
            });
            rootFacts.CommandSquadSpawned = 1;
            rootFacts.CommandSquadAlive = 1;
            rootFacts.HostileTotalCount = CountHostiles(ref definition);
            rootFacts.CivilianTotalCount = CountAmbientInstances(ref definition);
            rootFacts.CivilianLossCount = 0;
            rootFacts.FinalePresentationRequired = finaleRequired ? (byte)1 : (byte)0;
            rootFacts.FinalePresentationComplete = finaleRequired ? (byte)0 : (byte)1;
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
                        if (runtime.MissionId.Equals(FirstContactMissionId))
                            ApplyFirstContactCinematicVisualPolicy(em, instance);
                        float3 position = OffsetInsideAnchor(
                            anchor.Position, anchor.Radius, ordinal++, runtime.DeterministicSeed);
                        SetOrAdd(
                            em, instance, LocalTransform.FromPositionRotationScale(position, anchor.Rotation, 1f));
                        SetOrAdd(em, instance, new UnitGrid { Cell = ToGridCell(position, map.Grid) });
                        SetOrAdd(em, instance, new UnitPrevWorldPos { Value = position });
                        SetOrAdd(em, instance, new UnitMoveVisualComponent());
                        SetOrAdd(em, instance, new Faction { Id = group.FactionId });
                        if (runtime.MissionId.Equals(FirstContactMissionId))
                            ApplyFirstContactHostileCombatPolicy(em, instance, group.FactionId);
                        SetOrAdd(em, instance,
                            new UnitSourcePrefabKey { Value = unit.RuntimePrefabSourceKey });
                        SetOrAdd(em, instance, new CampaignMissionUnitRoleComponent
                        {
                            MissionRoleId = unit.MissionRoleId, UnitGroupId = group.GroupId, RouteId = routeId,
                            SessionToken = runtime.SessionToken
                        });
                        if ((ShouldUseTutorialFinale(in runtime) ||
                             CampaignMissionDelayedWaveUtility.ShouldSuppressAtSpawn(ref definition, group.GroupId)) &&
                            !em.HasComponent<CampaignMissionCombatSuppressedTag>(instance))
                        {
                            em.AddComponent<CampaignMissionCombatSuppressedTag>(instance);
                        }
                        CampaignMissionDelayedWaveUtility.ApplyCombatHoldAtSpawn(
                            em, instance, ref definition, group.GroupId);
                        if ((ShouldKeepStationary(runtime.MissionId, group.FactionId) ||
                             CampaignMissionDelayedWaveUtility.ShouldSuppressAtSpawn(ref definition, group.GroupId)) &&
                            !em.HasComponent<CampaignMissionStationaryUnitTag>(instance))
                        {
                            em.AddComponent<CampaignMissionStationaryUnitTag>(instance);
                        }
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

        internal static bool ShouldKeepStationary(in FixedString64Bytes missionId, byte factionId) =>
            missionId.Equals(FirstContactMissionId) && !FactionIdentity.IsPlayerControlled(factionId);

        internal static void QueueInitialRtsOverview(
            EntityManager entityManager,
            Entity cameraFocusEntity,
            float3 friendlyFocus)
        {
            entityManager.SetComponentData(cameraFocusEntity, new RuntimeCameraFocusRequestComponent
            {
                Requested = 1,
                UseTacticalRevealZoom = 4,
                World = friendlyFocus
            });
        }

        internal static void ApplyFirstContactHostileCombatPolicy(
            EntityManager em,
            Entity instance,
            byte factionId)
        {
            if (FactionIdentity.IsPlayerControlled(factionId) || !em.HasComponent<UnitAttack>(instance))
                return;

            UnitAttack attack = em.GetComponentData<UnitAttack>(instance);
            attack.Range = math.max(attack.Range, FirstContactHostileMinimumAttackRange);
            em.SetComponentData(instance, attack);
        }

        internal static void ApplyFirstContactCinematicVisualPolicy(EntityManager em, Entity instance)
        {
            // Seven close-camera actors are negligible beside the map render budget. Keep them on
            // their authored model throughout the opening and finale, including the first frame.
            if (em.HasComponent<UnitMidLodPrefabReference>(instance))
                em.RemoveComponent<UnitMidLodPrefabReference>(instance);
            if (em.HasComponent<UnitLowLodPrefabReference>(instance))
                em.RemoveComponent<UnitLowLodPrefabReference>(instance);
            if (!em.HasComponent<UnitForceDetailedVisualTag>(instance))
                em.AddComponent<UnitForceDetailedVisualTag>(instance);
            if (em.HasComponent<UnitRenderBudgetCulledUnitTag>(instance))
                em.RemoveComponent<UnitRenderBudgetCulledUnitTag>(instance);
            if (em.HasComponent<UnitRenderVisualExclusivityAppliedState>(instance))
                em.RemoveComponent<UnitRenderVisualExclusivityAppliedState>(instance);
            SetOrAdd(em, instance, new UnitRenderVisualComponent
            {
                Current = (byte)UnitRenderVisualKind.Detail,
                Desired = (byte)UnitRenderVisualKind.Detail,
                LastChangedFrame = 0
            });
        }

        internal static bool ShouldUseTutorialFinale(in CampaignMissionRuntimeComponent runtime) =>
            runtime.MissionId.Equals(FirstContactMissionId) &&
            (runtime.RunKind == Game.Missions.Contracts.MissionRunKind.FirstClear ||
             runtime.ReplayTutorialEnabled != 0);

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

        internal static bool HasRequiredRestrictions(ref CampaignMissionDefinitionBlob definition)
        {
            if (definition.MissionRuntimeEnabled != 0)
            {
                return definition.StartingCredits > 0 && definition.StartingMaterials > 0 &&
                       definition.BuildingDisabled == 0 && definition.ProductionDisabled == 0 &&
                       definition.EconomyDisabled == 0 && definition.TransportDisabled != 0 &&
                       definition.AirDisabled != 0;
            }

            return definition.BuildingDisabled != 0 && definition.ProductionDisabled != 0 &&
                   definition.EconomyDisabled != 0 && definition.TransportDisabled != 0 &&
                   definition.AirDisabled != 0;
        }

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

        private static int CountAmbientInstances(ref CampaignMissionDefinitionBlob definition)
        {
            int count = 0;
            for (int index = 0; index < definition.AmbientPresentations.Length; index++)
                count += math.max(0, definition.AmbientPresentations[index].InstanceCount);
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
            const float GoldenAngleRadians = 2.39996323f;
            uint seedHash = math.hash(new int2(seed, 1701));
            float angleOffset = (seedHash & 1023u) * (2f * math.PI / 1024f);
            float angle = angleOffset + ordinal * GoldenAngleRadians;
            float distance = math.min(math.max(0.35f, radius * 0.66f), math.max(0.35f, radius - 0.5f));
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
