using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        private void UpdateAuthoredDefensePolicy(ref SystemState state)
        {
            bool dormant = false;
            if (SystemAPI.TryGetSingleton(out CampaignMissionRuntimeComponent runtime) &&
                SystemAPI.TryGetSingleton(out CampaignMissionCatalogComponent catalog) &&
                SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) && gameplay.PlayRequested != 0 &&
                TryFindDefinition(in catalog, in runtime, out int index))
            {
                ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[index];
                dormant = IsPeacefulEstablishBase(ref definition) ||
                    definition.Defense.Enabled != 0 && definition.Defense.AuthoredMapDefensesDormant != 0 ||
                    definition.Extraction.Enabled != 0 && definition.Extraction.AuthoredMapDefensesDormant != 0;
            }
            EntityCommandBuffer changes = new(Allocator.Temp);
            if (dormant)
            {
                // Authored parked vehicles belong to the shared map, not the mission roster.
                // Disable only their gameplay root; the authored static presentation stays put.
                foreach (var (_, entity) in SystemAPI.Query<RefRO<OperationMapAuthoredVehiclePresentation>>()
                    .WithAll<UnitGrid, UnitMove>().WithNone<CampaignMissionUnitRoleComponent, CampaignMissionDormantMapUnitTag>().WithEntityAccess())
                {
                    changes.AddComponent<CampaignMissionDormantMapUnitTag>(entity);
                    changes.AddComponent<Disabled>(entity);
                }
                foreach (var (_, entity) in SystemAPI.Query<RefRO<BuildingDefenseWeapon>>()
                             .WithAll<OperationMapBuildingComponent>().WithNone<CampaignMissionDormantMapDefenseTag>().WithEntityAccess())
                    changes.AddComponent<CampaignMissionDormantMapDefenseTag>(entity);
            }
            else
            {
                foreach (var (_, entity) in SystemAPI.Query<RefRO<CampaignMissionDormantMapUnitTag>>()
                    .WithOptions(EntityQueryOptions.IncludeDisabledEntities).WithEntityAccess())
                {
                    changes.RemoveComponent<Disabled>(entity);
                    changes.RemoveComponent<CampaignMissionDormantMapUnitTag>(entity);
                }
                foreach (var (_, entity) in SystemAPI.Query<RefRO<CampaignMissionDormantMapDefenseTag>>().WithEntityAccess())
                    changes.RemoveComponent<CampaignMissionDormantMapDefenseTag>(entity);
            }
            changes.Playback(state.EntityManager);
            changes.Dispose();
        }

        private static bool IsPeacefulEstablishBase(ref CampaignMissionDefinitionBlob definition)
        {
            if (!definition.MissionId.Equals(EstablishBaseMissionId)) return false;
            // M2 teaches building and production. Its reserved combat remains opt-in,
            // under the same explicit Defend objective that permits the hostile wave.
            for (int i = 0; i < definition.Objectives.Length; i++)
                if (definition.Objectives[i].Rule == Game.Missions.Contracts.MissionObjectiveRuleKind.DefendMissionRole)
                    return false;
            return true;
        }
    }
}
