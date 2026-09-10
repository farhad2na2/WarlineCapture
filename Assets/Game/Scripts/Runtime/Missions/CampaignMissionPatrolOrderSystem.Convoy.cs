using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    public partial struct CampaignMissionPatrolOrderSystem
    {
        private EntityQuery _convoyMoveOrderQueueQuery;
        private void CreateConvoyOrderQuery(ref SystemState state) =>
            _convoyMoveOrderQueueQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitMoveOrderQueueComponent>());

        private void AdvanceConvoyRoutes(ref SystemState state, in CampaignMissionRuntimeComponent runtime,
            in CampaignMissionAttemptFactsComponent facts, ref CampaignMissionDefinitionBlob definition, ref OperationMapBlob map)
        {
            if (runtime.Outcome != MissionOutcomeKind.None || runtime.Phase != MissionPhaseKind.Engage) return;
            NativeList<Entity> targets = new(16, Allocator.Temp);
            NativeList<int2> cells = new(16, Allocator.Temp);
            foreach ((RefRW<CampaignMissionUnitRoleComponent> role, RefRW<CampaignMissionConvoyRouteProgress> progressRef, RefRO<UnitHealth> health,
                      RefRO<LocalTransform> transform, Entity entity) in
                     SystemAPI.Query<RefRW<CampaignMissionUnitRoleComponent>, RefRW<CampaignMissionConvoyRouteProgress>, RefRO<UnitHealth>, RefRO<LocalTransform>>()
                         .WithNone<CampaignMissionCombatSuppressedTag, CampaignMissionStationaryUnitTag>().WithEntityAccess())
            {
                CampaignMissionUnitRoleComponent current = role.ValueRO;
                if (!current.SessionToken.Equals(runtime.SessionToken) || current.RouteId.IsEmpty || health.ValueRO.Current <= 0 ||
                    !TryFindRoute(ref definition, current.RouteId, out int routeIndex)) continue;
                // Pursuers retain autonomous combat along the route. A manual move suppresses
                // acquisition and would make them walk past the stranded specialists.
                if (definition.Extraction.Enabled != 0 && state.EntityManager.HasComponent<EngageTarget>(entity)) continue;
                ref CampaignMissionPatrolRouteBlob route = ref definition.PatrolRoutes[routeIndex];
                if (facts.ElapsedMilliseconds < route.StartDelayMilliseconds || current.RouteIndex >= route.AnchorIds.Length) continue;
                var progress = progressRef.ValueRO;
                if (math.distancesq(progress.LastProgressPosition, transform.ValueRO.Position.xz) >= 1f)
                {
                    progress.LastProgressPosition = transform.ValueRO.Position.xz;
                    progress.LastProgressAtMilliseconds = facts.ElapsedMilliseconds;
                }
                bool issue = current.PatrolOrderVersion == 0;
                if (current.PatrolOrderVersion != 0)
                {
                    if (!CampaignMissionSpawnSystem.TryFindAnchor(ref map, route.AnchorIds[current.RouteIndex], out OperationMapAnchorBlob previous)) continue;
                    if (math.distancesq(transform.ValueRO.Position.xz, previous.Position.xz) <= math.max(9f, previous.Radius * previous.Radius))
                    { current.RouteIndex++; issue = true; }
                    else if (facts.ElapsedMilliseconds >= progress.NextRetryAtMilliseconds)
                        issue = (!state.EntityManager.HasComponent<UnitPathFollow>(entity) && !state.EntityManager.HasComponent<UnitPathRequest>(entity)) ||
                                facts.ElapsedMilliseconds - progress.LastProgressAtMilliseconds >= 10000;
                }
                if (issue && current.RouteIndex < route.AnchorIds.Length &&
                    CampaignMissionSpawnSystem.TryFindAnchor(ref map, route.AnchorIds[current.RouteIndex], out OperationMapAnchorBlob next))
                {
                    targets.Add(entity);
                    cells.Add(CampaignMissionSpawnSystem.ToGridCell(next.Position, map.Grid));
                    current.PatrolOrderVersion++;
                    progress.NextRetryAtMilliseconds = facts.ElapsedMilliseconds + 2000;
                    progress.LastProgressAtMilliseconds = facts.ElapsedMilliseconds;
                }
                role.ValueRW = current;
                progressRef.ValueRW = progress;
            }
            for (int i = 0; i < targets.Length; i++)
                UnitMoveOrderRequestSystem.EnqueueMoveOrder(state.EntityManager, targets[i], cells[i],
                    definition.Extraction.Enabled != 0 ? UnitMoveOrderRequestKind.TargetPathOnly : UnitMoveOrderRequestKind.Immediate,
                    true, false, 0, 0, _convoyMoveOrderQueueQuery);
            targets.Dispose();
            cells.Dispose();
        }
    }
}
