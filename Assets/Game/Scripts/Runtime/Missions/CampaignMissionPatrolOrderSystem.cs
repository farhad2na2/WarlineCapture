using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitMoveOrderRequestSystem))]
    [UpdateBefore(typeof(CampaignMissionRuntimeSystem))]
    public partial struct CampaignMissionPatrolOrderSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CampaignMissionCatalogComponent>();
            state.RequireForUpdate<CampaignMissionRuntimeComponent>();
            state.RequireForUpdate<CampaignMissionAttemptFactsComponent>();
            state.RequireForUpdate<OperationMapMetadataComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            CampaignMissionCatalogComponent catalog = SystemAPI.GetSingleton<CampaignMissionCatalogComponent>();
            CampaignMissionRuntimeComponent runtime = SystemAPI.GetSingleton<CampaignMissionRuntimeComponent>();
            CampaignMissionAttemptFactsComponent facts = SystemAPI.GetSingleton<CampaignMissionAttemptFactsComponent>();
            OperationMapMetadataComponent metadata = SystemAPI.GetSingleton<OperationMapMetadataComponent>();
            if (!metadata.Blob.IsCreated || facts.CommandSquadSpawned == 0 ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return;
            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            NativeList<Entity> targets = new(Allocator.Temp);
            NativeList<int2> goals = new(Allocator.Temp);
            foreach ((RefRW<CampaignMissionUnitRoleComponent> role, Entity entity) in
                     SystemAPI.Query<RefRW<CampaignMissionUnitRoleComponent>>().WithEntityAccess())
            {
                CampaignMissionUnitRoleComponent current = role.ValueRO;
                if (current.RouteId.IsEmpty || !current.SessionToken.Equals(runtime.SessionToken) ||
                    current.PatrolOrderVersion != 0 ||
                    !TryFindRoute(ref definition, current.RouteId, out int routeIndex)) continue;
                ref CampaignMissionPatrolRouteBlob route = ref definition.PatrolRoutes[routeIndex];
                if (facts.ElapsedMilliseconds < route.StartDelayMilliseconds || route.AnchorIds.Length == 0 ||
                    !CampaignMissionSpawnSystem.TryFindAnchor(
                        ref metadata.Blob.Value, route.AnchorIds[0], out OperationMapAnchorBlob anchor)) continue;
                targets.Add(entity);
                goals.Add(CampaignMissionSpawnSystem.ToGridCell(anchor.Position, metadata.Blob.Value.Grid));
                current.RouteIndex = 1;
                current.PatrolOrderVersion = 1;
                role.ValueRW = current;
            }
            for (int i = 0; i < targets.Length; i++)
                UnitMoveOrderRequestSystem.EnqueueTargetPathMoveOrder(state.EntityManager, targets[i], goals[i]);
            targets.Dispose();
            goals.Dispose();
        }

        private static bool TryFindRoute(
            ref CampaignMissionDefinitionBlob definition,
            Unity.Collections.FixedString64Bytes id, out int index)
        {
            for (int i = 0; i < definition.PatrolRoutes.Length; i++)
                if (definition.PatrolRoutes[i].RouteId.Equals(id)) { index = i; return true; }
            index = -1;
            return false;
        }
    }
}
