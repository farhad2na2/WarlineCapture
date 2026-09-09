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
                dormant = definition.Defense.Enabled != 0 && definition.Defense.AuthoredMapDefensesDormant != 0 ||
                    definition.Extraction.Enabled != 0 && definition.Extraction.AuthoredMapDefensesDormant != 0;
            }
            EntityCommandBuffer changes = new(Allocator.Temp);
            if (dormant)
            {
                // Authored parked vehicles belong to the shared map, not M3's finite roster.
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
    }
}
