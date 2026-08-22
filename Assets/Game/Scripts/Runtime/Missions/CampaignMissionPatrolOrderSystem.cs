using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EngageTargetValidateSystem))]
    [UpdateBefore(typeof(UnitMoveOrderRequestSystem))]
    [UpdateBefore(typeof(UnitEngagementSystem))]
    [UpdateBefore(typeof(CampaignMissionRuntimeSystem))]
    public partial struct CampaignMissionPatrolOrderSystem : ISystem
    {
        private static readonly FixedString64Bytes FirstContactMissionId = "saga.ch01.m01.first_contact";
        private const int InitialRtsHoldMilliseconds = 2500;
        private const int EstablishingArrivalMilliseconds = 5500;
        private const int EstablishingHoldMilliseconds = 7000;
        private const int HostileArrivalMilliseconds = 10000;
        private const int HostileHoldMilliseconds = 12000;
        private const int RtsReturnArrivalMilliseconds = 15000;
        private const int FinaleCameraArrivalMilliseconds = 1800;
        private const int FinalePostKillHoldMilliseconds = 3000;
        private const float CinematicGlideSmoothTimeSeconds = 2.25f;
        private const float FinaleCameraSmoothTimeSeconds = 1.65f;
        private EntityQuery _cameraFocusQuery;
        private EntityQuery _missionCombatantsQuery;
        private EntityQuery _renderVirtualizationStateQuery;

        public void OnCreate(ref SystemState state)
        {
            _cameraFocusQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeCameraFocusRequestComponent>());
            _missionCombatantsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CampaignMissionUnitRoleComponent>(),
                ComponentType.ReadOnly<Faction>(),
                ComponentType.ReadOnly<UnitHealth>(),
                ComponentType.ReadOnly<UnitCombat>(),
                ComponentType.ReadOnly<LocalTransform>());
            _renderVirtualizationStateQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderVirtualizationStateComponent>());
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
                if (current.SessionToken.Equals(runtime.SessionToken) && current.Stage <= 6)
                {
                    routeElapsedMilliseconds = current.Stage < 6
                        ? 0
                        : math.max(0, current.ElapsedMilliseconds - RtsReturnArrivalMilliseconds);
                    break;
                }
            }
            bool tutorialFinaleActive = false;
            byte tutorialFinaleStage = 0;
            if (SystemAPI.TryGetSingleton(out CampaignMissionFinalePresentationComponent finaleState) &&
                finaleState.Required != 0 && finaleState.SessionToken.Equals(runtime.SessionToken))
            {
                tutorialFinaleActive = true;
                tutorialFinaleStage = finaleState.Stage;
            }
            if (runtime.Outcome == MissionOutcomeKind.None)
            {
                bool holdCombat = runtime.Phase < MissionPhaseKind.Engage;
                bool releaseCombat = ShouldReleaseCombat(
                    runtime.Phase,
                    tutorialFinaleActive,
                    tutorialFinaleStage);
                if (runtime.Phase == MissionPhaseKind.Engage)
                {
                    foreach (RefRW<CampaignMissionOpeningPresentationComponent> opening in
                             SystemAPI.Query<RefRW<CampaignMissionOpeningPresentationComponent>>())
                    {
                        CampaignMissionOpeningPresentationComponent current = opening.ValueRO;
                        if (!current.SessionToken.Equals(runtime.SessionToken) || current.Stage != 6)
                            continue;
                        current.Stage = 7;
                        opening.ValueRW = current;
                        routeElapsedMilliseconds = 0;
                    }
                }
                if (holdCombat || releaseCombat)
                {
                    EntityCommandBuffer preEngageCleanup = new(Allocator.Temp);
                    foreach ((RefRW<UnitCombat> combat, RefRO<CampaignMissionUnitRoleComponent> role,
                              RefRO<Faction> faction, Entity entity) in
                             SystemAPI.Query<RefRW<UnitCombat>, RefRO<CampaignMissionUnitRoleComponent>, RefRO<Faction>>()
                                 .WithEntityAccess())
                    {
                        if (!role.ValueRO.SessionToken.Equals(runtime.SessionToken))
                            continue;
                        UnitCombat current = combat.ValueRO;
                        ApplyTutorialCombatPolicy(ref current, releaseCombat);
                        combat.ValueRW = current;
                        if (holdCombat && state.EntityManager.HasComponent<EngageTarget>(entity) &&
                            ShouldRemovePreEngageTarget(
                                state.EntityManager.GetComponentData<EngageTarget>(entity),
                                faction.ValueRO.Id))
                        {
                            preEngageCleanup.RemoveComponent<EngageTarget>(entity);
                        }
                    }
                    preEngageCleanup.Playback(state.EntityManager);
                    preEngageCleanup.Dispose();

                    if (releaseCombat && facts.AttackIssued != 0)
                        CampaignMissionGroupAttackUtility.ContinueCommandedSquadAttack(
                            state.EntityManager,
                            _missionCombatantsQuery,
                            runtime.SessionToken);
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
                    if (current.Stage > 6 || !current.SessionToken.Equals(runtime.SessionToken))
                        continue;

                    if (current.Stage <= 5 && IsOpeningVisible(state.EntityManager))
                        current.ElapsedMilliseconds = SaturatingAddMilliseconds(
                            current.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
                    if (current.Stage == 0 &&
                        current.ElapsedMilliseconds >= InitialRtsHoldMilliseconds &&
                        focus.Requested == 0 && IsOpeningVisible(state.EntityManager))
                    {
                        state.EntityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
                        {
                            Requested = 1,
                            Smooth = 1,
                            UseTacticalRevealZoom = 3,
                            SmoothTimeSeconds = CinematicGlideSmoothTimeSeconds,
                            World = current.EstablishingFocus
                        });
                        current.Stage = 1;
                    }
                    if (current.Stage == 1 && current.ElapsedMilliseconds >= EstablishingArrivalMilliseconds)
                        current.Stage = 2;
                    if (current.Stage == 2 && current.ElapsedMilliseconds >= EstablishingHoldMilliseconds &&
                        focus.Requested == 0)
                    {
                        state.EntityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
                        {
                            Requested = 1,
                            Smooth = 1,
                            UseTacticalRevealZoom = 1,
                            SmoothTimeSeconds = CinematicGlideSmoothTimeSeconds,
                            World = current.HostileFocus
                        });
                        current.Stage = 3;
                    }
                    if (current.Stage == 3 && current.ElapsedMilliseconds >= HostileArrivalMilliseconds)
                        current.Stage = 4;
                    if (current.Stage == 4 && current.ElapsedMilliseconds >= HostileHoldMilliseconds &&
                        focus.Requested == 0)
                    {
                        state.EntityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
                        {
                            Requested = 1,
                            Smooth = 1,
                            UseTacticalRevealZoom = 4,
                            SmoothTimeSeconds = CinematicGlideSmoothTimeSeconds,
                            World = current.FriendlyFocus
                        });
                        current.Stage = 5;
                    }
                    if (current.Stage == 5 && current.ElapsedMilliseconds >= RtsReturnArrivalMilliseconds)
                        current.Stage = 6;
                    opening.ValueRW = current;
                    break;
                }

                EntityCommandBuffer finaleStructuralChanges = new(Allocator.Temp);
                foreach (RefRW<CampaignMissionFinalePresentationComponent> finale in
                         SystemAPI.Query<RefRW<CampaignMissionFinalePresentationComponent>>())
                {
                    CampaignMissionFinalePresentationComponent current = finale.ValueRO;
                    if (current.Required == 0 || !current.SessionToken.Equals(runtime.SessionToken) ||
                        current.Stage >= 4)
                        continue;

                    if (current.Stage == 0 && runtime.Phase == MissionPhaseKind.Engage &&
                        facts.AttackIssued != 0 && focus.Requested == 0)
                    {
                        float3 direction = current.HostileFocus - current.FriendlyFocus;
                        float yaw = ComputeCombatRevealYaw(direction);
                        state.EntityManager.SetComponentData(focusEntity, new RuntimeCameraFocusRequestComponent
                        {
                            Requested = 1,
                            Smooth = 1,
                            UseTacticalRevealZoom = 5,
                            UseExplicitYaw = 1,
                            SmoothTimeSeconds = FinaleCameraSmoothTimeSeconds,
                            YawDegrees = yaw,
                            World = math.lerp(current.FriendlyFocus, current.HostileFocus, 0.48f)
                        });
                        current.Stage = 1;
                        current.ElapsedMilliseconds = 0;
                    }

                    if (current.Stage == 1)
                    {
                        current.ElapsedMilliseconds = SaturatingAddMilliseconds(
                            current.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
                        if (current.ElapsedMilliseconds >= FinaleCameraArrivalMilliseconds)
                        {
                            current.Stage = 2;
                            current.ElapsedMilliseconds = 0;
                            QueueCombatSuppressionRemoval(
                                state.EntityManager,
                                _missionCombatantsQuery,
                                runtime.SessionToken,
                                ref finaleStructuralChanges);
                        }
                    }
                    else if (current.Stage == 2 && runtime.Phase == MissionPhaseKind.SecureCorridor)
                    {
                        current.Stage = 3;
                        current.ElapsedMilliseconds = 0;
                    }
                    else if (current.Stage == 3)
                    {
                        current.ElapsedMilliseconds = SaturatingAddMilliseconds(
                            current.ElapsedMilliseconds, SystemAPI.Time.DeltaTime);
                        if (current.ElapsedMilliseconds >= FinalePostKillHoldMilliseconds)
                        {
                            current.Stage = 4;
                            RefRW<CampaignMissionAttemptFactsComponent> factsRw =
                                SystemAPI.GetSingletonRW<CampaignMissionAttemptFactsComponent>();
                            CampaignMissionAttemptFactsComponent completedFacts = factsRw.ValueRO;
                            completedFacts.FinalePresentationComplete = 1;
                            factsRw.ValueRW = completedFacts;
                        }
                    }

                    finale.ValueRW = current;
                    break;
                }
                finaleStructuralChanges.Playback(state.EntityManager);
                finaleStructuralChanges.Dispose();
            }
            ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[definitionIndex];
            NativeList<Entity> targets = new(Allocator.Temp);
            NativeList<int2> goals = new(Allocator.Temp);
            foreach ((RefRW<CampaignMissionUnitRoleComponent> role, Entity entity) in
                     SystemAPI.Query<RefRW<CampaignMissionUnitRoleComponent>>().WithEntityAccess())
            {
                CampaignMissionUnitRoleComponent current = role.ValueRO;
                if (!ShouldIssuePatrolRoute(runtime.MissionId, runtime.Phase) ||
                    current.RouteId.IsEmpty || !current.SessionToken.Equals(runtime.SessionToken) ||
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

        internal static void ApplyTutorialCombatPolicy(ref UnitCombat combat, bool releaseCombat)
        {
            // Keep the authored manual-attack capability available so the player can arm Attack
            // and then choose a target. The tutorial hold suppresses only autonomous acquisition
            // until the authoritative Engage phase begins. Reapply the release throughout Engage
            // so the squad always acquires another nearby patrol member after its clicked target dies.
            combat.AutoEngage = (byte)(releaseCombat && combat.CanAttack != 0 ? 1 : 0);
        }

        internal static bool ShouldReleaseCombat(MissionPhaseKind phase) =>
            phase is MissionPhaseKind.Engage or MissionPhaseKind.SecureCorridor;

        internal static bool ShouldReleaseCombat(
            MissionPhaseKind phase,
            bool tutorialFinaleActive,
            byte tutorialFinaleStage) =>
            ShouldReleaseCombat(phase) && (!tutorialFinaleActive || tutorialFinaleStage >= 2);

        internal static float ComputeCombatRevealYaw(float3 direction)
        {
            float2 groundDirection = direction.xz;
            if (!math.all(math.isfinite(groundDirection)) || math.lengthsq(groundDirection) < 0.0001f)
                return RuntimeCameraFocusRequestUtility.TacticalRevealYaw;
            groundDirection = math.normalize(groundDirection);
            return math.degrees(math.atan2(groundDirection.x, groundDirection.y));
        }

        internal static bool ShouldIssuePatrolRoute(
            in FixedString64Bytes missionId,
            MissionPhaseKind phase) =>
            // First Contact presents the hostile trio as a fixed firing line. They may attack
            // in place after Engage, but never consume their legacy authored patrol route.
            !missionId.Equals(FirstContactMissionId) &&
            phase is MissionPhaseKind.Engage or MissionPhaseKind.SecureCorridor;

        internal static bool ShouldRemovePreEngageTarget(in EngageTarget target, byte sourceFactionId)
        {
            // Automatic/retaliation targets are forbidden before Engage, but the player's
            // explicit hostile click is the authoritative input that confirms the threat and
            // advances the mission into Engage. Removing that commanded target here deadlocks
            // the tutorial before CampaignMissionRuntimeSystem can observe it.
            return !FactionIdentity.IsPlayerControlled(sourceFactionId) || target.IsCommanded == 0;
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

        private bool IsOpeningVisible(EntityManager entityManager)
        {
            int renderStateCount = _renderVirtualizationStateQuery.CalculateEntityCount();
            if (renderStateCount > 1)
                return false;
            if (renderStateCount == 1)
            {
                OperationMapRenderVirtualizationStateComponent renderState =
                    entityManager.GetComponentData<OperationMapRenderVirtualizationStateComponent>(
                        _renderVirtualizationStateQuery.GetSingletonEntity());
                if (renderState.Initialized == 0 || renderState.InitialViewApplied == 0)
                    return false;
            }

            // SimulationActive and the applied render state are the neutral runtime readiness
            // boundaries. UI-shell transition state remains owned by composition and UI assemblies.
            return true;
        }

        private static int SaturatingAddMilliseconds(int current, float deltaSeconds)
        {
            int delta = (int)math.min(int.MaxValue, math.max(0f, math.round(deltaSeconds * 1000f)));
            return current >= int.MaxValue - delta ? int.MaxValue : current + delta;
        }

        internal static void QueueCombatSuppressionRemoval(
            EntityManager entityManager,
            EntityQuery missionCombatantsQuery,
            in FixedString64Bytes sessionToken,
            ref EntityCommandBuffer structuralChanges)
        {
            using NativeArray<Entity> entities = missionCombatantsQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!entityManager.GetComponentData<CampaignMissionUnitRoleComponent>(entity)
                        .SessionToken.Equals(sessionToken))
                    continue;
                structuralChanges.RemoveComponent<CampaignMissionCombatSuppressedTag>(entity);
            }
        }
    }
}
