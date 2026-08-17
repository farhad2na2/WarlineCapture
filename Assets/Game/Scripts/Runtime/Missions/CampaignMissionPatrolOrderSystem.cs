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
        private const int SquadReturnFocusMilliseconds = 20000;
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
            if (SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplayState) &&
                (gameplayState.PlayRequested == 0 || gameplayState.SimulationActive == 0))
                return;
            if (!metadata.Blob.IsCreated || facts.CommandSquadSpawned == 0 ||
                !CampaignMissionSpawnSystem.TryFindDefinition(in catalog, in runtime, out int definitionIndex))
                return;
            int routeElapsedMilliseconds = facts.ElapsedMilliseconds;
            foreach (RefRO<CampaignMissionOpeningPresentationComponent> opening in
                     SystemAPI.Query<RefRO<CampaignMissionOpeningPresentationComponent>>())
            {
                CampaignMissionOpeningPresentationComponent current = opening.ValueRO;
                if (current.SessionToken.Equals(runtime.SessionToken) && current.Stage is 0 or 1 or 2)
                {
                    routeElapsedMilliseconds = current.Stage < 2 ? 0 : current.ElapsedMilliseconds;
                    break;
                }
            }
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
                        if (!current.SessionToken.Equals(runtime.SessionToken) || current.Stage != 2)
                            continue;
                        current.Stage = 3;
                        opening.ValueRW = current;
                        releaseCombat = true;
                        routeElapsedMilliseconds = 0;
                    }
                }
                if (holdCombat || releaseCombat)
                {
                    EntityCommandBuffer preEngageCleanup = new(Allocator.Temp);
                    foreach ((RefRW<UnitCombat> combat, RefRO<CampaignMissionUnitRoleComponent> role, Entity entity) in
                             SystemAPI.Query<RefRW<UnitCombat>, RefRO<CampaignMissionUnitRoleComponent>>()
                                 .WithEntityAccess())
                    {
                        if (!role.ValueRO.SessionToken.Equals(runtime.SessionToken))
                            continue;
                        UnitCombat current = combat.ValueRO;
                        current.CanAttack = (byte)(releaseCombat ? 1 : 0);
                        current.AutoEngage = (byte)(releaseCombat && current.CanAttack != 0 ? 1 : 0);
                        combat.ValueRW = current;
                        if (holdCombat && state.EntityManager.HasComponent<EngageTarget>(entity))
                            preEngageCleanup.RemoveComponent<EngageTarget>(entity);
                    }
                    preEngageCleanup.Playback(state.EntityManager);
                    preEngageCleanup.Dispose();
                }
            }
            if (_cameraFocusQuery.CalculateEntityCount() == 1)
            {
                Entity focusEntity = _cameraFocusQuery.GetSingletonEntity();
                RuntimeCameraFocusRequestComponent focus =
                    state.EntityManager.GetComponentData<RuntimeCameraFocusRequestComponent>(focusEntity);
                foreach (RefRW<CampaignMissionOpeningPresentationComponent> opening in
                         SystemAPI.Query<RefRW<CampaignMissionOpeningPresentationComponent>>())
                {
                    CampaignMissionOpeningPresentationComponent current = opening.ValueRO;
                    if (current.Stage is not (0 or 1 or 2) || !current.SessionToken.Equals(runtime.SessionToken))
                        continue;

                    if (current.Stage == 0 && focus.Requested == 0)
                    {
                        state.EntityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
                        {
                            Requested = 1,
                            Smooth = 0,
                            UseTacticalRevealZoom = 1,
                            World = current.HostileFocus
                        });
                        current.Stage = 1;
                    }
                    if (current.Stage == 1)
                        current.ElapsedMilliseconds = SaturatingAddMilliseconds(
                            current.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
                    if (current.Stage == 1 && current.ElapsedMilliseconds >= SquadReturnFocusMilliseconds &&
                        focus.Requested == 0)
                    {
                        state.EntityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
                        {
                            Requested = 1,
                            Smooth = 1,
                            UseTacticalRevealZoom = 2,
                            World = current.FriendlyFocus
                        });
                        current.Stage = 2;
                    }
                    opening.ValueRW = current;
                    break;
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
                if (routeElapsedMilliseconds < route.StartDelayMilliseconds || route.AnchorIds.Length == 0 ||
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

        private static int SaturatingAddMilliseconds(int current, float deltaSeconds)
        {
            int delta = (int)math.min(int.MaxValue, math.max(0f, math.round(deltaSeconds * 1000f)));
            return current >= int.MaxValue - delta ? int.MaxValue : current + delta;
        }
    }
}
