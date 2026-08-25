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
