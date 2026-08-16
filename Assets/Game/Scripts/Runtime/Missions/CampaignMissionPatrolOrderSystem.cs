using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(UnitMoveOrderRequestSystem))]
    [UpdateBefore(typeof(UnitEngagementSystem))]
    [UpdateBefore(typeof(CampaignMissionRuntimeSystem))]
    public partial struct CampaignMissionPatrolOrderSystem : ISystem
    {
        private const int SquadReturnFocusMilliseconds = 2000;
        private EntityQuery _cameraFocusQuery;

        public void OnCreate(ref SystemState state)
        {
            _cameraFocusQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeCameraFocusRequestComponent>());
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
            if (runtime.Outcome == MissionOutcomeKind.None)
            {
                bool holdCombat = runtime.Phase < MissionPhaseKind.Engage;
                bool releaseCombat = false;
                if (runtime.Phase == MissionPhaseKind.Engage)
                {
                    foreach (RefRW<CampaignMissionOpeningPresentationComponent> opening in
                             SystemAPI.Query<RefRW<CampaignMissionOpeningPresentationComponent>>())
                    {
                        CampaignMissionOpeningPresentationComponent current = opening.ValueRO;
                        if (!current.SessionToken.Equals(runtime.SessionToken) || current.Stage >= 3)
                            continue;
                        current.Stage = 3;
                        opening.ValueRW = current;
                        releaseCombat = true;
                    }
                }
                if (holdCombat || releaseCombat)
                {
                    foreach ((RefRW<UnitCombat> combat, RefRO<CampaignMissionUnitRoleComponent> role) in
                             SystemAPI.Query<RefRW<UnitCombat>, RefRO<CampaignMissionUnitRoleComponent>>())
                    {
                        if (!role.ValueRO.SessionToken.Equals(runtime.SessionToken))
                            continue;
                        UnitCombat current = combat.ValueRO;
                        current.AutoEngage = (byte)(releaseCombat && current.CanAttack != 0 ? 1 : 0);
                        combat.ValueRW = current;
                    }
                }
            }
            if (facts.ElapsedMilliseconds >= SquadReturnFocusMilliseconds &&
                _cameraFocusQuery.CalculateEntityCount() == 1)
            {
                Entity focusEntity = _cameraFocusQuery.GetSingletonEntity();
                RuntimeCameraFocusRequestComponent focus =
                    state.EntityManager.GetComponentData<RuntimeCameraFocusRequestComponent>(focusEntity);
                if (focus.Requested == 0)
                {
                    foreach (RefRW<CampaignMissionOpeningPresentationComponent> opening in
                             SystemAPI.Query<RefRW<CampaignMissionOpeningPresentationComponent>>())
                    {
                        CampaignMissionOpeningPresentationComponent current = opening.ValueRO;
                        if (current.Stage != 1 || !current.SessionToken.Equals(runtime.SessionToken)) continue;
                        state.EntityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
                        {
                            Requested = 1,
                            Smooth = 1,
                            World = current.FriendlyFocus
                        });
                        current.Stage = 2;
                        opening.ValueRW = current;
                        break;
                    }
                }
            }
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
