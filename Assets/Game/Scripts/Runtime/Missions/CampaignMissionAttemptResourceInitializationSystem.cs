using Game.Components;
using Unity.Burst;
using Unity.Entities;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InitialUnitsSpawnSystem))]
    public partial struct CampaignMissionAttemptResourceInitializationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CampaignMissionRootComponent>();
            state.RequireForUpdate<CampaignMissionRuntimeComponent>();
            state.RequireForUpdate<CampaignMissionCatalogComponent>();
            state.RequireForUpdate<CampaignMissionAttemptResourceInitializationComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root))
                return;

            EntityManager entityManager = state.EntityManager;
            CampaignMissionRuntimeComponent runtime =
                entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionAttemptResourceInitializationComponent attempt =
                entityManager.GetComponentData<CampaignMissionAttemptResourceInitializationComponent>(root);
            if (attempt.Applied != 0 && attempt.SessionToken.Equals(runtime.SessionToken) &&
                attempt.AttemptOrdinal == runtime.AttemptOrdinal)
                return;

            CampaignMissionCatalogComponent catalog =
                entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
            if (!CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return;

            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            if (definition.MissionRuntimeEnabled == 0)
            {
                MarkApplied(entityManager, root, in runtime);
                return;
            }

            // The map's normal startup can arrive after the campaign root. Apply the
            // attempt budget only after those defaults, or they overwrite it once.
            if (!SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) || gameplay.PlayRequested == 0 ||
                !entityManager.HasComponent<CampaignMissionAttemptFactsComponent>(root) ||
                entityManager.GetComponentData<CampaignMissionAttemptFactsComponent>(root).CommandSquadSpawned == 0)
                return;
            foreach ((RefRO<InitialUnitsSpawnConfig> _, Entity entity) in
                     SystemAPI.Query<RefRO<InitialUnitsSpawnConfig>>().WithEntityAccess())
            {
                if (entityManager.HasComponent<InitialUnitsSpawnInitialized>(entity)) continue;
                if (!entityManager.HasComponent<InitialUnitsSpawnProgress>(entity) ||
                    entityManager.GetComponentData<InitialUnitsSpawnProgress>(entity).InitialResourcesApplied == 0)
                    return;
            }

            Entity playerResources = Entity.Null;
            int playerResourceOwnerCount = 0;
            foreach ((RefRO<FactionEconomy> economy,
                      RefRO<FactionTacticalMaterialsComponent> _,
                      Entity entity)
                     in SystemAPI.Query<RefRO<FactionEconomy>,
                         RefRO<FactionTacticalMaterialsComponent>>().WithEntityAccess())
            {
                if (!FactionIdentity.IsPlayerControlled(economy.ValueRO.FactionId))
                    continue;
                playerResources = entity;
                playerResourceOwnerCount++;
            }

            if (playerResourceOwnerCount != 1)
                return;

            FactionEconomy currentEconomy = entityManager.GetComponentData<FactionEconomy>(playerResources);
            FactionTacticalMaterialsComponent currentMaterials =
                entityManager.GetComponentData<FactionTacticalMaterialsComponent>(playerResources);
            if (!TryCreateMissionAttemptResourceTotals(
                    in currentEconomy,
                    in currentMaterials,
                    definition.StartingCredits,
                    definition.StartingMaterials,
                    out FactionEconomy nextEconomy,
                    out FactionTacticalMaterialsComponent nextMaterials))
                return;

            entityManager.SetComponentData(playerResources, nextEconomy);
            entityManager.SetComponentData(playerResources, nextMaterials);
            MarkApplied(entityManager, root, in runtime);
        }

        private static bool TryCreateMissionAttemptResourceTotals(
            in FactionEconomy currentEconomy,
            in FactionTacticalMaterialsComponent currentMaterials,
            int startingCredits,
            int startingMaterials,
            out FactionEconomy nextEconomy,
            out FactionTacticalMaterialsComponent nextMaterials)
        {
            nextEconomy = currentEconomy;
            nextMaterials = currentMaterials;
            if (!FactionIdentity.IsPlayerControlled(currentEconomy.FactionId) ||
                currentMaterials.FactionId != currentEconomy.FactionId ||
                startingCredits <= 0 || startingMaterials <= 0)
                return false;

            nextEconomy.Money = startingCredits;
            nextEconomy.Oil = 0f;
            nextEconomy.Fuel = 0f;
            nextMaterials = new FactionTacticalMaterialsComponent
            {
                FactionId = currentMaterials.FactionId,
                Current = startingMaterials,
                Capacity = startingMaterials,
                Version = currentMaterials.Version == uint.MaxValue ? 1u : currentMaterials.Version + 1u
            };
            return true;
        }

        private static void MarkApplied(
            EntityManager entityManager,
            Entity root,
            in CampaignMissionRuntimeComponent runtime)
        {
            entityManager.SetComponentData(root,
                new CampaignMissionAttemptResourceInitializationComponent
                {
                    SessionToken = runtime.SessionToken,
                    AttemptOrdinal = runtime.AttemptOrdinal,
                    Applied = 1
                });
        }
    }
}
