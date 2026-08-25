using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(CampaignMissionPatrolOrderSystem))]
    [UpdateBefore(typeof(UnitEngagementSystem))]
    [UpdateBefore(typeof(UnitAttackSystem))]
    public partial struct CampaignMissionDelayedWaveSystem : ISystem
    {
        private EntityQuery _rootQuery;
        private EntityQuery _warningStateQuery;
        private EntityQuery _waveQuery;

        public void OnCreate(ref SystemState state)
        {
            _rootQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>(),
                ComponentType.ReadOnly<CampaignMissionCatalogComponent>(),
                ComponentType.ReadOnly<CampaignMissionRuntimeComponent>(),
                ComponentType.ReadWrite<CampaignMissionAttemptFactsComponent>(),
                ComponentType.ReadWrite<CampaignMissionDelayedWaveStateComponent>());
            _warningStateQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<ThreatWarningRuntimeStateComponent>());
            _waveQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitCombat>(),
                ComponentType.ReadOnly<UnitHealth>());
            state.RequireForUpdate(_rootQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_rootQuery.CalculateEntityCount() != 1 ||
                (SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplayState) &&
                 (gameplayState.PlayRequested == 0 || gameplayState.SimulationActive == 0)))
                return;

            Entity root = _rootQuery.GetSingletonEntity();
            EntityManager entityManager = state.EntityManager;
            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionAttemptFactsComponent facts =
                entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root);
            CampaignMissionDelayedWaveStateComponent waveState =
                entityManager.GetComponentData<CampaignMissionDelayedWaveStateComponent>(root);

            if (runtime.Outcome != MissionOutcomeKind.None || facts.CommandSquadSpawned == 0 ||
                waveState.Initialized == 0 ||
                !waveState.SessionToken.Equals(runtime.SessionToken) ||
                waveState.AttemptOrdinal != runtime.AttemptOrdinal ||
                waveState.SourceVersion != catalog.SourceVersion ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return;

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (!CampaignMissionDelayedWaveUtility.TryResolveDefinition(
                    ref definition, out int expectedUnitCount, out byte expectedFactionId))
                return;

            if (CampaignMissionDelayedWaveUtility.ShouldIssueWarning(
                    facts.ElapsedMilliseconds,
                    definition.DelayedWaveWarningAtMilliseconds,
                    waveState.WarningIssued))
            {
                if (!TryValidateWave(
                        entityManager,
                        _waveQuery,
                        runtime.SessionToken,
                        definition.DelayedWaveUnitGroupId,
                        definition.DelayedWaveRouteId,
                        expectedFactionId,
                        expectedUnitCount,
                        requireSuppressed: true,
                        out NativeList<Entity> warningEntities))
                    return;
                warningEntities.Dispose();

                float etaSeconds = (definition.DelayedWaveActivationAtMilliseconds -
                                    definition.DelayedWaveWarningAtMilliseconds) / 1000f;
                if (!ThreatWarningRuntimeState.RequestWarning(
                        entityManager,
                        _warningStateQuery,
                        ThreatWarningType.Ground,
                        etaSeconds,
                        expectedUnitCount))
                    return;

                ThreatWarningAudioEventUtility.TryEmit(
                    entityManager,
                    ThreatWarningType.Ground,
                    etaSeconds,
                    expectedUnitCount,
                    (float)SystemAPI.Time.ElapsedTime);
                waveState.WarningIssued = 1;
                facts.DefenseWaveWarningIssued = 1;
                entityManager.SetComponentData(root, waveState);
                entityManager.SetComponentData(root, facts);
                return;
            }

            if (!CampaignMissionDelayedWaveUtility.ShouldActivate(
                    facts.ElapsedMilliseconds,
                    definition.DelayedWaveActivationAtMilliseconds,
                    waveState.WarningIssued,
                    waveState.Activated))
                return;

            if (!TryValidateWave(
                    entityManager,
                    _waveQuery,
                    runtime.SessionToken,
                    definition.DelayedWaveUnitGroupId,
                    definition.DelayedWaveRouteId,
                    expectedFactionId,
                    expectedUnitCount,
                    requireSuppressed: true,
                    out NativeList<Entity> waveEntities))
                return;

            EntityCommandBuffer commands = new(Allocator.Temp);
            for (int i = 0; i < waveEntities.Length; i++)
            {
                Entity entity = waveEntities[i];
                UnitCombat combat = entityManager.GetComponentData<UnitCombat>(entity);
                combat.AutoEngage = combat.CanAttack != 0 ? (byte)1 : (byte)0;
                commands.SetComponent(entity, combat);
                commands.RemoveComponent<CampaignMissionCombatSuppressedTag>(entity);
                commands.RemoveComponent<CampaignMissionStationaryUnitTag>(entity);
            }
            commands.Playback(entityManager);
            commands.Dispose();
            waveEntities.Dispose();

            waveState.Activated = 1;
            facts.DefenseWaveActivated = 1;
            entityManager.SetComponentData(root, waveState);
            entityManager.SetComponentData(root, facts);
        }

        private static bool TryValidateWave(
            EntityManager entityManager,
            EntityQuery query,
            in FixedString64Bytes sessionToken,
            in FixedString64Bytes unitGroupId,
            in FixedString64Bytes routeId,
            byte expectedFactionId,
            int expectedUnitCount,
            bool requireSuppressed,
            out NativeList<Entity> waveEntities)
        {
            waveEntities = new NativeList<Entity>(expectedUnitCount, Allocator.Temp);
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                CampaignMissionUnitRoleComponent role =
                    entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity);
                if (!role.SessionToken.Equals(sessionToken) || !role.UnitGroupId.Equals(unitGroupId))
                    continue;

                UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
                Faction faction = entityManager.GetComponentData<Faction>(entity);
                if (!role.RouteId.Equals(routeId) || faction.Id != expectedFactionId ||
                    health.Max <= 0 || health.Current <= 0 ||
                    requireSuppressed &&
                    (!entityManager.HasComponent<CampaignMissionCombatSuppressedTag>(entity) ||
                     !entityManager.HasComponent<CampaignMissionStationaryUnitTag>(entity)))
                {
                    waveEntities.Dispose();
                    waveEntities = default;
                    return false;
                }
                waveEntities.Add(entity);
            }

            if (waveEntities.Length == expectedUnitCount)
                return true;

            waveEntities.Dispose();
            waveEntities = default;
            return false;
        }
    }
}
