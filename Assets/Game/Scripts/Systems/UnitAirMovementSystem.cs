using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(EngageTargetValidateSystem))]
    [UpdateAfter(typeof(EngageTargetSyncSystem))]
    [UpdateBefore(typeof(UnitEngagedMovementSystem))]
    [UpdateBefore(typeof(UnitGridMovementSystem))]
    public partial struct UnitAirMovementSystem : ISystem
    {
        private const float AirborneSurfaceLookaheadSeconds = 1.35f;
        private const float RunwayTakeoffLiftoffFraction = 0.65f;
        private const float RunwayTakeoffMinGroundRollFraction = 0.35f;
        private const float RunwayTakeoffEndSafetyFraction = 0.2f;
        private const float RunwayTakeoffEndSafetyCells = 2f;
        private const float RunwayTakeoffRotationStartFraction = 0.45f;
        private const float RunwayTakeoffPitchDegrees = 14f;
        private const float RunwayTakeoffInitialClearance = 1.25f;
        private const float FixedWingManeuverTurnRateDegreesPerSecond = 45f;
        private const float FixedWingManeuverDirectPassAngleDegrees = 12f;
        private const float FixedWingManeuverLineupAngleDegrees = 10f;
        private const float FixedWingManeuverBankMaxDegrees = 30f;
        private const float FixedWingManeuverBankRateDegreesPerSecond = 55f;
        private const float FixedWingManeuverPitchMaxDegrees = 16f;
        private const float FixedWingManeuverPitchRateDegreesPerSecond = 30f;
        private const float FixedWingManeuverTurnEntryRadii = 2.5f;
        private const float FixedWingManeuverGoAroundRadii = 1.4f;
        private const float FixedWingManeuverMinExtendSeconds = 1.25f;
        private const float FixedWingFinalPassLookaheadCells = 6f;
        private const float FixedWingReturnApproachMinCells = 24f;
        private const float FixedWingReturnFinalStraightCells = 10f;
        private const float FixedWingReturnLineupSlackCells = 6f;
        private const byte FixedWingPassFinalPhase = 1;
        private const byte FixedWingPassManeuverPhase = 2;
        private const byte FixedWingPassExtendPhase = 3;

        private EntityQuery _gridQuery;
        private EntityQuery _surfaceQuery;

        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            _surfaceQuery = state.GetEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<UnitAirMovement>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            GridConfig grid = state.EntityManager.GetComponentData<GridConfig>(gridEntity);
            MapSurfaceComponent surface = default;
            bool hasSurface = !_surfaceQuery.IsEmptyIgnoreFilter;
            if (hasSurface)
            {
                surface = _surfaceQuery.GetSingleton<MapSurfaceComponent>();
                hasSurface = surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated;
            }

            float dt = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            var targetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true);
            var engageLookup = SystemAPI.GetComponentLookup<EngageTarget>(true);
            var healthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true);
            var debugFireStateLookup = SystemAPI.GetComponentLookup<SelectedUnitDebugFireState>(true);
            var debugFireTargetLookup = SystemAPI.GetComponentLookup<DebugFireTargetTag>(true);
            var transitLookup = SystemAPI.GetComponentLookup<UnitSpawnTransitTag>(true);
            var ropeDisembarkLookup = SystemAPI.GetComponentLookup<UnitTransportRopeDisembarkRequest>(true);
            var holdPositionLookup = SystemAPI.GetComponentLookup<HoldPositionOrderTag>(true);
            var scanOrderLookup = SystemAPI.GetComponentLookup<UnitScanOrder>(true);
            var attackLookup = SystemAPI.GetComponentLookup<UnitAttack>(true);
            var airdropLookup = SystemAPI.GetComponentLookup<UnitTransportAirdropRequest>();

            foreach (var (transform, unitGrid, move, airMovement, airState, entity) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<UnitGrid>, RefRO<UnitMove>, RefRO<UnitAirMovement>, RefRW<UnitAirComponent>>()
                         .WithNone<StaticGridBlocker>()
                         .WithNone<UnitDeathAnimationComponent>()
                         .WithEntityAccess())
            {
                if (move.ValueRO.Speed <= 0.05f)
                {
                    ref var frozenState = ref airState.ValueRW;
                    frozenState.ReturningHome = 0;
                    frozenState.Airborne = 0;
                    frozenState.TakeoffRolling = 0;
                    frozenState.LandingRolling = 0;
                    frozenState.AttackRunActive = 0;
                    frozenState.ReturnApproachInitialized = 0;
                    if (targetLookup.HasComponent(entity))
                        ecb.RemoveComponent<UnitTarget>(entity);
                    if (engageLookup.HasComponent(entity))
                        ecb.RemoveComponent<EngageTarget>(entity);
                    if (state.EntityManager.HasComponent<UnitPathRequest>(entity))
                        ecb.RemoveComponent<UnitPathRequest>(entity);
                    if (state.EntityManager.HasComponent<ManualMoveOrderTag>(entity))
                        ecb.RemoveComponent<ManualMoveOrderTag>(entity);
                    continue;
                }

                ref var stateRw = ref airState.ValueRW;
                float3 position = transform.ValueRO.Position;
                float groundY = stateRw.HomeInitialized != 0 ? stateRw.HomePosition.y : position.y;
                if (stateRw.HomeInitialized == 0)
                {
                    stateRw.HomePosition = position;
                    stateRw.HomeCell = unitGrid.ValueRO.Cell;
                    stateRw.HomeInitialized = 1;
                    groundY = position.y;
                }

                if (ropeDisembarkLookup.HasComponent(entity))
                {
                    float hoverY = ResolveAirCruiseY(
                        surface,
                        hasSurface,
                        grid,
                        transform.ValueRO.Position,
                        transform.ValueRO.Position,
                        move.ValueRO.Speed,
                        dt,
                        groundY,
                        airMovement.ValueRO.CruiseHeight,
                        stateRw.UsesRunway != 0);
                    float3 hoverPosition = transform.ValueRO.Position;
                    if (hoverPosition.y < hoverY)
                    {
                        hoverPosition.y = hoverY;
                        transform.ValueRW.Position = hoverPosition;
                    }

                    stateRw.ReturningHome = 0;
                    stateRw.Airborne = 1;
                    stateRw.TakeoffRolling = 0;
                    stateRw.LandingRolling = 0;
                    stateRw.AttackRunActive = 0;
                    stateRw.ReturnApproachInitialized = 0;
                    if (targetLookup.HasComponent(entity))
                        ecb.RemoveComponent<UnitTarget>(entity);
                    if (engageLookup.HasComponent(entity))
                        ecb.RemoveComponent<EngageTarget>(entity);
                    if (state.EntityManager.HasComponent<UnitPathRequest>(entity))
                        ecb.RemoveComponent<UnitPathRequest>(entity);
                    if (state.EntityManager.HasComponent<ManualMoveOrderTag>(entity))
                        ecb.RemoveComponent<ManualMoveOrderTag>(entity);
                    continue;
                }

                if (airdropLookup.HasComponent(entity))
                {
                    UnitTransportAirdropRequest airdrop = airdropLookup[entity];
                    HandleTransportAirdropPass(
                        ref transform.ValueRW,
                        ref unitGrid.ValueRW,
                        ref stateRw,
                        ref airdrop,
                        grid,
                        move.ValueRO,
                        airMovement.ValueRO,
                        dt,
                        groundY,
                        surface,
                        hasSurface);
                    airdropLookup[entity] = airdrop;

                    if (targetLookup.HasComponent(entity))
                        ecb.RemoveComponent<UnitTarget>(entity);
                    if (engageLookup.HasComponent(entity))
                        ecb.RemoveComponent<EngageTarget>(entity);
                    if (state.EntityManager.HasComponent<UnitPathRequest>(entity))
                        ecb.RemoveComponent<UnitPathRequest>(entity);
                    if (state.EntityManager.HasComponent<ManualMoveOrderTag>(entity))
                        ecb.RemoveComponent<ManualMoveOrderTag>(entity);
                    continue;
                }

                bool hasValidEngage = false;
                Entity engageTarget = Entity.Null;
                float3 engageTargetPosition = default;
                bool hasActiveDebugFire = IsActiveDebugFireSource(debugFireStateLookup, debugFireTargetLookup, entity);
                if (engageLookup.HasComponent(entity))
                {
                    EngageTarget engage = engageLookup[entity];
                    if (engage.Target != Entity.Null &&
                        healthLookup.HasComponent(engage.Target) &&
                        healthLookup[engage.Target].Current > 0)
                    {
                        hasValidEngage = true;
                        engageTarget = engage.Target;
                        engageTargetPosition = engage.Position;
                    }
                }

                if (!hasValidEngage && hasActiveDebugFire)
                {
                    SuppressDebugFireMovement(ref stateRw);
                    continue;
                }

                if (hasValidEngage)
                {
                    stateRw.ReturningHome = 0;
                    if (IsDebugFireTargetForSource(debugFireTargetLookup, engageTarget, entity))
                    {
                        SuppressDebugFireMovement(ref stateRw);
                        FaceTarget(ref transform.ValueRW, engageTargetPosition);
                        continue;
                    }

                    if (hasActiveDebugFire)
                    {
                        SuppressDebugFireMovement(ref stateRw);
                        continue;
                    }

                    float runwayGroundY = ResolveRunwayGroundY(stateRw, groundY);
                    if (stateRw.UsesRunway != 0 && stateRw.Airborne == 0)
                    {
                        if (stateRw.TakeoffRolling == 0)
                        {
                            bool reachedRunwayStart = SteerTowards(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                grid,
                                math.max(0.01f, airMovement.ValueRO.RunwayTaxiSpeed),
                                dt,
                                runwayGroundY,
                                stateRw.RunwayTakeoffPosition,
                                false,
                                5f);

                            if (reachedRunwayStart)
                            {
                                stateRw.TakeoffRolling = 1;
                                unitGrid.ValueRW.Cell = stateRw.RunwayTakeoffCell;
                            }
                        }
                        else
                        {
                            RunwayTakeoffRoll(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                ref stateRw,
                                grid,
                                move.ValueRO,
                                dt,
                                runwayGroundY);
                        }

                        continue;
                    }

                    float cruiseY = ResolveAirCruiseY(
                        surface,
                        hasSurface,
                        grid,
                        transform.ValueRO.Position,
                        engageTargetPosition,
                        move.ValueRO.Speed,
                        dt,
                        groundY,
                        airMovement.ValueRO.CruiseHeight,
                        stateRw.UsesRunway != 0);
                    if (stateRw.UsesRunway != 0)
                    {
                        float3 attackPassTarget = new float3(engageTargetPosition.x, cruiseY, engageTargetPosition.z);
                        float attackRange = attackLookup.HasComponent(entity)
                            ? math.max(0.01f, attackLookup[entity].Range)
                            : math.max(grid.CellSize * 12f, 30f);
                        float overshootDistance = math.max(attackRange * 8f, grid.CellSize * 40f);
                        bool inFinalPass = UpdateFixedWingApproach(
                            ref transform.ValueRW,
                            ref unitGrid.ValueRW,
                            ref stateRw,
                            grid,
                            move.ValueRO.Speed,
                            dt,
                            cruiseY,
                            attackPassTarget,
                            overshootDistance);

                        if (inFinalPass)
                        {
                            float3 attackSteerTarget = ResolveFixedWingFinalSteerTarget(
                                attackPassTarget,
                                stateRw.AttackRunExitPosition,
                                transform.ValueRO.Position,
                                grid,
                                move.ValueRO.Speed);
                            bool completedAttackRun = SteerFixedWingTowards(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                grid,
                                move.ValueRO.Speed,
                                dt,
                                cruiseY,
                                attackSteerTarget);

                            float3 exitVector = stateRw.AttackRunExitPosition - attackPassTarget;
                            float3 progressedVector = transform.ValueRO.Position - attackPassTarget;
                            exitVector.y = 0f;
                            progressedVector.y = 0f;
                            float exitLengthSq = math.lengthsq(exitVector);
                            if (exitLengthSq > 1e-6f && math.dot(progressedVector, exitVector) >= exitLengthSq * 0.92f)
                                completedAttackRun = true;

                            if (completedAttackRun)
                            {
                                stateRw.AttackRunActive = 0;
                                stateRw.ReturningHome = 1;
                                stateRw.ReturnApproachInitialized = 0;
                                ecb.RemoveComponent<EngageTarget>(entity);
                            }
                        }
                    }
                    else
                    {
                        FlyTowards(ref transform.ValueRW, ref unitGrid.ValueRW, grid, move.ValueRO.Speed, dt, cruiseY, engageTargetPosition, true);
                    }

                    continue;
                }

                bool hasActiveScanOrder = scanOrderLookup.HasComponent(entity);
                bool holdingPosition = holdPositionLookup.HasComponent(entity);

                bool hasDirectTarget = targetLookup.HasComponent(entity);
                // A move order owns its pass state across frames; command systems reset it when a
                // new order is issued. Only clear it here once no order remains.
                if (!hasDirectTarget)
                    stateRw.AttackRunActive = 0;
                if (hasDirectTarget)
                {
                    int2 goalCell = targetLookup[entity].Cell;
                    bool hasManualMove = state.EntityManager.HasComponent<ManualMoveOrderTag>(entity);
                    if (!hasValidEngage &&
                        state.EntityManager.HasComponent<SelectedUnitTag>(entity) &&
                        hasManualMove)
                    {
                        int2 deltaToGoal = goalCell - unitGrid.ValueRO.Cell;
                        if (math.abs(deltaToGoal.x) <= 1 && math.abs(deltaToGoal.y) <= 1)
                        {
                            ecb.RemoveComponent<UnitTarget>(entity);
                            if (state.EntityManager.HasComponent<UnitPathRequest>(entity))
                                ecb.RemoveComponent<UnitPathRequest>(entity);
                            ecb.RemoveComponent<ManualMoveOrderTag>(entity);
                            stateRw.AttackRunActive = 0;
                            if (stateRw.UsesRunway != 0 && stateRw.Airborne != 0)
                                stateRw.ReturningHome = 1;
                            continue;
                        }
                    }
                    float3 targetWorld = GridUtils.CellToWorldCenter(grid, goalCell);
                    bool isSpawnTransit = transitLookup.HasComponent(entity);
                    bool reached;
                    if (stateRw.UsesRunway != 0 && !isSpawnTransit)
                    {
                        float runwayGroundY = ResolveRunwayGroundY(stateRw, groundY);
                        if (stateRw.Airborne == 0)
                        {
                            if (stateRw.TakeoffRolling == 0)
                            {
                                bool reachedRunwayStart = SteerTowards(
                                    ref transform.ValueRW,
                                    ref unitGrid.ValueRW,
                                    grid,
                                    math.max(0.01f, airMovement.ValueRO.RunwayTaxiSpeed),
                                    dt,
                                    runwayGroundY,
                                    stateRw.RunwayTakeoffPosition,
                                    false,
                                    5f);

                                if (reachedRunwayStart)
                                {
                                    stateRw.TakeoffRolling = 1;
                                    unitGrid.ValueRW.Cell = stateRw.RunwayTakeoffCell;
                                }

                                reached = false;
                            }
                            else
                            {
                                RunwayTakeoffRoll(
                                    ref transform.ValueRW,
                                    ref unitGrid.ValueRW,
                                    ref stateRw,
                                    grid,
                                    move.ValueRO,
                                    dt,
                                    runwayGroundY);

                                reached = false;
                            }
                        }
                        else
                        {
                            float cruiseY = ResolveAirCruiseY(
                                surface,
                                hasSurface,
                                grid,
                                transform.ValueRO.Position,
                                targetWorld,
                                move.ValueRO.Speed,
                                dt,
                                groundY,
                                airMovement.ValueRO.CruiseHeight,
                                stateRw.UsesRunway != 0);
                            float3 movePassTarget = new float3(targetWorld.x, cruiseY, targetWorld.z);
                            float moveOvershootDistance = math.max(grid.CellSize * 10f, move.ValueRO.Speed * 1.5f);
                            // Replan the pass when the ordered goal changes mid-flight (new click
                            // or pathfinding goal remap); a stale exit line would fly the plane
                            // straight past the old goal.
                            if (stateRw.AttackRunActive != 0 && math.any(stateRw.AttackPassGoalCell != goalCell))
                                stateRw.AttackRunActive = 0;
                            stateRw.AttackPassGoalCell = goalCell;
                            bool completedPass = false;
                            bool inFinalPass = UpdateFixedWingApproach(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                ref stateRw,
                                grid,
                                move.ValueRO.Speed,
                                dt,
                                cruiseY,
                                movePassTarget,
                                moveOvershootDistance);

                            if (inFinalPass)
                            {
                                float3 moveSteerTarget = ResolveFixedWingFinalSteerTarget(
                                    movePassTarget,
                                    stateRw.AttackRunExitPosition,
                                    transform.ValueRO.Position,
                                    grid,
                                    move.ValueRO.Speed);
                                completedPass = SteerFixedWingTowards(
                                    ref transform.ValueRW,
                                    ref unitGrid.ValueRW,
                                    grid,
                                    move.ValueRO.Speed,
                                    dt,
                                    cruiseY,
                                    moveSteerTarget);

                                float3 exitVector = stateRw.AttackRunExitPosition - movePassTarget;
                                float3 progressedVector = transform.ValueRO.Position - movePassTarget;
                                exitVector.y = 0f;
                                progressedVector.y = 0f;
                                float exitLengthSq = math.lengthsq(exitVector);
                                if (exitLengthSq > 1e-6f && math.dot(progressedVector, exitVector) >= exitLengthSq * 0.85f)
                                    completedPass = true;
                            }

                            reached = completedPass;

                            if (completedPass)
                            {
                                stateRw.AttackRunActive = 0;
                                if (!hasActiveScanOrder)
                                    stateRw.ReturningHome = 1;
                                stateRw.ReturnApproachInitialized = 0;
                            }
                        }
                    }
                    else
                    {
                        reached = FlyTowards(
                            ref transform.ValueRW,
                            ref unitGrid.ValueRW,
                            grid,
                            isSpawnTransit ? math.max(0.01f, airMovement.ValueRO.RunwayTaxiSpeed) : move.ValueRO.Speed,
                            dt,
                            isSpawnTransit
                                ? groundY
                                : ResolveAirCruiseY(
                                    surface,
                                    hasSurface,
                                    grid,
                                    transform.ValueRO.Position,
                                    targetWorld,
                                    move.ValueRO.Speed,
                                    dt,
                                    groundY,
                                    airMovement.ValueRO.CruiseHeight,
                                    stateRw.UsesRunway != 0),
                            targetWorld,
                            !isSpawnTransit);
                    }
                    if (reached)
                    {
                        if (targetLookup.HasComponent(entity))
                            ecb.RemoveComponent<UnitTarget>(entity);
                        if (state.EntityManager.HasComponent<ManualMoveOrderTag>(entity))
                            ecb.RemoveComponent<ManualMoveOrderTag>(entity);
                        if (state.EntityManager.HasComponent<UnitPathRequest>(entity))
                            ecb.RemoveComponent<UnitPathRequest>(entity);
                        if (isSpawnTransit)
                        {
                            if (state.EntityManager.HasComponent<UnitSpawnTransitTag>(entity))
                                ecb.RemoveComponent<UnitSpawnTransitTag>(entity);
                            stateRw.ReturningHome = 0;
                            stateRw.Airborne = 0;
                            stateRw.AttackRunActive = 0;
                            stateRw.TakeoffRolling = 0;
                            stateRw.LandingRolling = 0;
                            stateRw.ReturnApproachInitialized = 0;
                            stateRw.HomePosition = transform.ValueRO.Position;
                            stateRw.HomeCell = unitGrid.ValueRO.Cell;
                        }
                        else if (stateRw.UsesRunway == 0)
                        {
                            if (!hasActiveScanOrder)
                                stateRw.ReturningHome = 1;
                        }
                    }
                    continue;
                }

                if (hasActiveScanOrder &&
                    stateRw.Airborne != 0 &&
                    stateRw.ReturningHome == 0 &&
                    stateRw.TakeoffRolling == 0 &&
                    stateRw.LandingRolling == 0)
                {
                    stateRw.AttackRunActive = 0;
                    stateRw.ReturnApproachInitialized = 0;
                    continue;
                }

                if (holdingPosition)
                {
                    stateRw.AttackRunActive = 0;
                    if (stateRw.Airborne != 0 &&
                        stateRw.ReturningHome == 0 &&
                        stateRw.TakeoffRolling == 0 &&
                        stateRw.LandingRolling == 0)
                    {
                        stateRw.ReturnApproachInitialized = 0;
                        continue;
                    }

                    if (stateRw.Airborne == 0 &&
                        stateRw.ReturningHome == 0 &&
                        stateRw.TakeoffRolling == 0 &&
                        stateRw.LandingRolling == 0)
                    {
                        stateRw.ReturnApproachInitialized = 0;
                        continue;
                    }
                }

                if (stateRw.ReturningHome != 0 || stateRw.Airborne != 0 || stateRw.LandingRolling != 0 || stateRw.TakeoffRolling != 0)
                {
                    if (stateRw.UsesRunway != 0)
                    {
                        float runwayGroundY = ResolveRunwayGroundY(stateRw, groundY);
                        if (stateRw.Airborne != 0)
                        {
                            float cruiseY = ResolveAirCruiseY(
                                surface,
                                hasSurface,
                                grid,
                                transform.ValueRO.Position,
                                stateRw.RunwayTakeoffPosition,
                                move.ValueRO.Speed,
                                dt,
                                groundY,
                                airMovement.ValueRO.CruiseHeight,
                                stateRw.UsesRunway != 0);
                            float3 runwayDirection = stateRw.RunwayLandingPosition - stateRw.RunwayTakeoffPosition;
                            runwayDirection.y = 0f;
                            runwayDirection = math.normalizesafe(runwayDirection, new float3(0f, 0f, 1f));
                            float requiredStraightInDistance = math.max(
                                grid.CellSize * FixedWingReturnFinalStraightCells,
                                move.ValueRO.Speed * 1.5f);
                            float requiredLineupDistance = ResolveFixedWingTurnRadius(move.ValueRO.Speed) * 1.5f;
                            float approachDistance = math.max(
                                math.max(
                                    grid.CellSize * FixedWingReturnApproachMinCells,
                                    math.distance(stateRw.RunwayTakeoffPosition, stateRw.RunwayLandingPosition) * 1.25f),
                                requiredStraightInDistance + requiredLineupDistance + grid.CellSize * FixedWingReturnLineupSlackCells);
                            float3 approachPoint = new float3(
                                stateRw.RunwayTakeoffPosition.x - runwayDirection.x * approachDistance,
                                cruiseY,
                                stateRw.RunwayTakeoffPosition.z - runwayDirection.z * approachDistance);

                            if (stateRw.ReturnApproachInitialized == 0)
                            {
                                float3 approachProgressVector = transform.ValueRO.Position - approachPoint;
                                approachProgressVector.y = 0f;
                                float alongApproach = math.dot(approachProgressVector, runwayDirection);
                                float3 approachLateralVector = approachProgressVector - runwayDirection * alongApproach;
                                float3 returnForward = UnitVehicleMovementUtility.Forward(transform.ValueRO.Rotation);
                                float returnRunwayAlignment = math.dot(returnForward, runwayDirection);
                                float approachCaptureRadius = math.max(
                                    grid.CellSize * 3f,
                                    move.ValueRO.Speed * math.max(dt, 1f / 30f) * 2f);
                                float finalCaptureEnd = math.max(
                                    grid.CellSize * 1f,
                                    approachDistance - math.min(requiredStraightInDistance, approachDistance * 0.75f));
                                float finalCaptureLimit = finalCaptureEnd + approachCaptureRadius;
                                bool laterallyCaptured = math.lengthsq(approachLateralVector) <= approachCaptureRadius * approachCaptureRadius;
                                bool insideEntryWindow =
                                    alongApproach >= -approachCaptureRadius &&
                                    alongApproach <= finalCaptureLimit &&
                                    laterallyCaptured &&
                                    returnRunwayAlignment >= 0.95f;

                                if (!insideEntryWindow)
                                {
                                    stateRw.AttackRunActive = 0;
                                    if (laterallyCaptured &&
                                        alongApproach >= -approachCaptureRadius &&
                                        alongApproach <= finalCaptureLimit)
                                    {
                                        float maxYawStep = math.radians(FixedWingManeuverTurnRateDegreesPerSecond) * math.max(0f, dt);
                                        float signedYawToRunway = UnitVehicleMovementUtility.SignedAngleY(returnForward, runwayDirection);
                                        float yawStep = math.clamp(signedYawToRunway, -maxYawStep, maxYawStep);
                                        ApplyFixedWingFlight(
                                            ref transform.ValueRW,
                                            ref unitGrid.ValueRW,
                                            grid,
                                            move.ValueRO.Speed,
                                            dt,
                                            cruiseY,
                                            yawStep,
                                            maxYawStep);
                                        continue;
                                    }

                                    float lineLookahead = math.max(
                                        grid.CellSize * 8f,
                                        move.ValueRO.Speed * 1.5f);
                                    float targetAlong = alongApproach > finalCaptureEnd
                                        ? 0f
                                        : math.clamp(alongApproach + lineLookahead, 0f, finalCaptureEnd);
                                    float3 lineCaptureTarget = new float3(
                                        approachPoint.x + runwayDirection.x * targetAlong,
                                        cruiseY,
                                        approachPoint.z + runwayDirection.z * targetAlong);
                                    SteerFixedWingTowards(
                                        ref transform.ValueRW,
                                        ref unitGrid.ValueRW,
                                        grid,
                                        move.ValueRO.Speed,
                                        dt,
                                        cruiseY,
                                        lineCaptureTarget);
                                    continue;
                                }

                                transform.ValueRW.Rotation = quaternion.LookRotationSafe(runwayDirection, math.up());
                                stateRw.AttackRunActive = 0;
                                stateRw.ReturnApproachInitialized = 1;
                            }

                            bool reachedTouchdown = SteerTowards(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                grid,
                                move.ValueRO.Speed,
                                dt,
                                runwayGroundY,
                                stateRw.RunwayTakeoffPosition,
                                false,
                                1.15f);
                            if (!reachedTouchdown)
                            {
                                float3 touchdownDelta = transform.ValueRO.Position - stateRw.RunwayTakeoffPosition;
                                touchdownDelta.y = 0f;
                                float touchdownCaptureRadius = math.max(
                                    grid.CellSize * 8f,
                                    move.ValueRO.Speed * math.max(dt, 1f / 30f) * 2f);
                                reachedTouchdown =
                                    math.abs(transform.ValueRO.Position.y - runwayGroundY) <= grid.CellSize &&
                                    math.lengthsq(touchdownDelta) <= touchdownCaptureRadius * touchdownCaptureRadius;
                            }

                            if (reachedTouchdown)
                            {
                                stateRw.Airborne = 0;
                                stateRw.LandingRolling = 1;
                                stateRw.ReturnApproachInitialized = 0;
                                unitGrid.ValueRW.Cell = stateRw.RunwayTakeoffCell;
                            }
                        }
                        else if (stateRw.LandingRolling != 0)
                        {
                            bool reachedRunwayCenter = FlyTowards(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                grid,
                                math.max(0.01f, move.ValueRO.Speed),
                                dt,
                                runwayGroundY,
                                stateRw.RunwayLandingPosition,
                                false);

                            if (reachedRunwayCenter)
                            {
                                stateRw.LandingRolling = 0;
                                stateRw.ReturningHome = 1;
                                stateRw.Airborne = 0;
                                unitGrid.ValueRW.Cell = stateRw.RunwayLandingCell;
                            }
                        }
                        else
                        {
                            bool reachedHome = FlyTowards(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                grid,
                                math.max(0.01f, airMovement.ValueRO.RunwayTaxiSpeed),
                                dt,
                                groundY,
                                stateRw.HomePosition,
                                false);

                            if (reachedHome)
                            {
                                stateRw.ReturningHome = 0;
                                stateRw.Airborne = 0;
                                stateRw.TakeoffRolling = 0;
                                stateRw.LandingRolling = 0;
                                stateRw.ReturnApproachInitialized = 0;
                                unitGrid.ValueRW.Cell = stateRw.HomeCell;
                                if (state.EntityManager.HasComponent<UnitSpawnTransitTag>(entity))
                                    ecb.RemoveComponent<UnitSpawnTransitTag>(entity);
                            }
                        }
                    }
                    else
                    {
                        float homeDesiredY = ResolveReturnHomeY(
                            surface,
                            hasSurface,
                            grid,
                            transform.ValueRO.Position,
                            stateRw.HomePosition,
                            move.ValueRO.Speed,
                            dt,
                            groundY,
                            airMovement.ValueRO.CruiseHeight,
                            stateRw.UsesRunway != 0);
                        bool reachedHome = FlyTowards(ref transform.ValueRW, ref unitGrid.ValueRW, grid, move.ValueRO.Speed, dt, homeDesiredY, stateRw.HomePosition, false);
                        if (reachedHome)
                        {
                            stateRw.ReturningHome = 0;
                            stateRw.Airborne = 0;
                            unitGrid.ValueRW.Cell = stateRw.HomeCell;
                        }
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private static void HandleTransportAirdropPass(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            ref UnitAirComponent state,
            ref UnitTransportAirdropRequest request,
            in GridConfig grid,
            in UnitMove move,
            in UnitAirMovement airMovement,
            float deltaTime,
            float groundY,
            MapSurfaceComponent surface,
            bool hasSurface)
        {
            state.ReturningHome = 0;
            state.ReturnApproachInitialized = 0;

            float groundedHeightTolerance = math.max(0.25f, TransportBoardingData.AirBoardingGroundedHeightTolerance);
            bool physicallyAirborne = transform.Position.y > groundY + groundedHeightTolerance;
            if (state.Airborne == 0 && physicallyAirborne)
            {
                state.Airborne = 1;
                state.TakeoffRolling = 0;
                state.LandingRolling = 0;
            }

            if (state.UsesRunway != 0 && state.Airborne == 0)
            {
                float runwayGroundY = ResolveRunwayGroundY(state, groundY);
                if (state.TakeoffRolling == 0)
                {
                    bool reachedRunwayStart = SteerTowards(
                        ref transform,
                        ref unitGrid,
                        grid,
                        math.max(0.01f, airMovement.RunwayTaxiSpeed),
                        deltaTime,
                        runwayGroundY,
                        state.RunwayTakeoffPosition,
                        false,
                        5f);

                    if (reachedRunwayStart)
                    {
                        state.TakeoffRolling = 1;
                        unitGrid.Cell = state.RunwayTakeoffCell;
                    }
                }
                else
                {
                    RunwayTakeoffRoll(
                        ref transform,
                        ref unitGrid,
                        ref state,
                        grid,
                        move,
                        deltaTime,
                        runwayGroundY);
                }

                request.PassReady = 0;
                state.AttackRunActive = 0;
                return;
            }

            float3 dropWorld = GridUtils.CellToWorldCenter(grid, request.DropReferenceCell);
            float cruiseY = ResolveAirCruiseY(
                surface,
                hasSurface,
                grid,
                transform.Position,
                dropWorld,
                move.Speed,
                deltaTime,
                groundY,
                airMovement.CruiseHeight,
                state.UsesRunway != 0);
            dropWorld.y = cruiseY;

            float sequenceSeconds = math.max(2f, math.max(0.1f, request.DropIntervalSeconds) * math.max(1, request.DropCount));
            float overshootDistance = math.max(grid.CellSize * 24f, math.max(0.01f, move.Speed) * (sequenceSeconds + 1.5f));
            bool inFinalPass = UpdateFixedWingApproach(
                ref transform,
                ref unitGrid,
                ref state,
                grid,
                math.max(0.01f, move.Speed),
                deltaTime,
                cruiseY,
                dropWorld,
                overshootDistance);
            if (!inFinalPass)
            {
                request.PassReady = 0;
                return;
            }

            float3 exitVector = state.AttackRunExitPosition - dropWorld;
            exitVector.y = 0f;
            float3 passForward = math.normalizesafe(exitVector, math.mul(transform.Rotation, new float3(0f, 0f, 1f)));
            passForward.y = 0f;
            passForward = math.normalizesafe(passForward, new float3(0f, 0f, 1f));

            float3 toDrop = dropWorld - transform.Position;
            float horizontalDistanceToDrop = math.length(new float3(toDrop.x, 0f, toDrop.z));
            float3 progressedVector = transform.Position - dropWorld;
            progressedVector.y = 0f;
            float passReadyDistance = math.max(
                grid.CellSize * 4f,
                math.max(0.01f, move.Speed) * math.max(deltaTime, 1f / 30f) * 2f);
            if (request.PassReady == 0 &&
                (horizontalDistanceToDrop <= passReadyDistance ||
                 math.dot(progressedVector, passForward) >= 0f))
            {
                request.PassReady = 1;
                request.NextDropAt = 0f;
            }

            float3 airdropSteerTarget = ResolveFixedWingFinalSteerTarget(dropWorld, state.AttackRunExitPosition, transform.Position, grid, move.Speed);
            bool completedPass = SteerFixedWingTowards(
                ref transform,
                ref unitGrid,
                grid,
                math.max(0.01f, move.Speed),
                deltaTime,
                cruiseY,
                airdropSteerTarget);

            float exitLengthSq = math.lengthsq(exitVector);
            progressedVector = transform.Position - dropWorld;
            progressedVector.y = 0f;
            if (exitLengthSq > 1e-6f && math.dot(progressedVector, exitVector) >= exitLengthSq * 0.92f)
                completedPass = true;

            if (completedPass)
            {
                float extendDistance = math.max(grid.CellSize * 20f, math.max(0.01f, move.Speed) * 2.5f);
                state.AttackRunExitPosition = new float3(
                    state.AttackRunExitPosition.x + passForward.x * extendDistance,
                    cruiseY,
                    state.AttackRunExitPosition.z + passForward.z * extendDistance);
                state.AttackRunActive = 1;
            }
        }

        private static float ResolveAirCruiseY(
            MapSurfaceComponent surface,
            bool hasSurface,
            in GridConfig grid,
            float3 currentWorld,
            float3 targetWorld,
            float speed,
            float deltaTime,
            float fallbackGroundY,
            float cruiseHeight,
            bool fixedWing)
        {
            float groundY = ResolveAirReferenceGroundY(
                surface,
                hasSurface,
                grid,
                currentWorld,
                targetWorld,
                speed,
                deltaTime,
                fallbackGroundY,
                fixedWing);
            return groundY + ResolveAirClearance(cruiseHeight, fixedWing);
        }

        private static float ResolveReturnHomeY(
            MapSurfaceComponent surface,
            bool hasSurface,
            in GridConfig grid,
            float3 currentWorld,
            float3 homeWorld,
            float speed,
            float deltaTime,
            float fallbackGroundY,
            float cruiseHeight,
            bool fixedWing)
        {
            float3 toHome = homeWorld - currentWorld;
            toHome.y = 0f;
            float descendDistance = math.max(grid.CellSize * 2f, math.max(0.01f, speed) * math.max(deltaTime, 1f / 30f) * 2f);
            if (math.lengthsq(toHome) <= descendDistance * descendDistance)
                return homeWorld.y;

            return ResolveAirCruiseY(
                surface,
                hasSurface,
                grid,
                currentWorld,
                homeWorld,
                speed,
                deltaTime,
                fallbackGroundY,
                cruiseHeight,
                fixedWing);
        }

        private static float ResolveAirReferenceGroundY(
            MapSurfaceComponent surface,
            bool hasSurface,
            in GridConfig grid,
            float3 currentWorld,
            float3 targetWorld,
            float speed,
            float deltaTime,
            float fallbackGroundY,
            bool fixedWing)
        {
            if (!hasSurface ||
                surface.HasSurfaceData == 0 ||
                !surface.SurfaceBlob.IsCreated ||
                surface.CellSize <= 0f)
            {
                return fallbackGroundY;
            }

            float bestHeight = fallbackGroundY;
            bool found = false;
            SampleMaxSurfaceHeight(surface, currentWorld, ref bestHeight, ref found);
            SampleMaxSurfaceHeight(surface, targetWorld, ref bestHeight, ref found);

            float3 horizontalDelta = targetWorld - currentWorld;
            horizontalDelta.y = 0f;
            float horizontalDistance = math.length(horizontalDelta);
            if (horizontalDistance > 1e-4f)
            {
                float3 direction = horizontalDelta / horizontalDistance;
                float lookaheadDistance = math.min(
                    horizontalDistance,
                    math.max(grid.CellSize * (fixedWing ? 6f : 3f),
                        math.max(0.01f, speed) * AirborneSurfaceLookaheadSeconds * (fixedWing ? 1.75f : 1f)));
                SampleMaxSurfaceHeight(surface, currentWorld + direction * lookaheadDistance, ref bestHeight, ref found);
                SampleMaxSurfaceHeight(surface, currentWorld + direction * (lookaheadDistance * 0.5f), ref bestHeight, ref found);
            }

            return found ? bestHeight : fallbackGroundY;
        }

        private static bool RunwayTakeoffRoll(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            ref UnitAirComponent state,
            in GridConfig grid,
            in UnitMove move,
            float deltaTime,
            float runwayGroundY)
        {
            bool reachedLiftoffPoint = SteerTowards(
                ref transform,
                ref unitGrid,
                grid,
                math.max(0.01f, move.Speed),
                deltaTime,
                runwayGroundY,
                ResolveRunwayLiftoffPosition(state, grid),
                false,
                3.25f);

            ApplyRunwayTakeoffPitch(ref transform, state, grid, reachedLiftoffPoint ? 1f : 0f);
            if (reachedLiftoffPoint)
                CompleteRunwayLiftoff(ref transform, ref unitGrid, ref state, grid, runwayGroundY);

            return reachedLiftoffPoint;
        }

        private static float3 ResolveRunwayLiftoffPosition(in UnitAirComponent state, in GridConfig grid)
        {
            if (!TryResolveRunwayDirection(state, out float3 runwayDirection, out float runwayLength))
                return state.RunwayLandingPosition;

            float endSafetyDistance = math.min(
                runwayLength * 0.4f,
                math.max(grid.CellSize * RunwayTakeoffEndSafetyCells, runwayLength * RunwayTakeoffEndSafetyFraction));
            float minGroundRollDistance = runwayLength * RunwayTakeoffMinGroundRollFraction;
            float maxGroundRollDistance = math.max(minGroundRollDistance, runwayLength - endSafetyDistance);
            float liftoffDistance = math.clamp(
                runwayLength * RunwayTakeoffLiftoffFraction,
                minGroundRollDistance,
                maxGroundRollDistance);

            return state.RunwayTakeoffPosition + runwayDirection * liftoffDistance;
        }

        private static void CompleteRunwayLiftoff(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            ref UnitAirComponent state,
            in GridConfig grid,
            float runwayGroundY)
        {
            state.TakeoffRolling = 0;
            state.Airborne = 1;
            transform.Position.y = math.max(transform.Position.y, runwayGroundY + RunwayTakeoffInitialClearance);
            ApplyRunwayTakeoffPitch(ref transform, state, grid, 1f);

            int2 currentCell = GridUtils.WorldToCell(grid, transform.Position);
            if (GridUtils.InBounds(currentCell, grid.Width, grid.Height))
                unitGrid.Cell = currentCell;
        }

        private static void ApplyRunwayTakeoffPitch(
            ref LocalTransform transform,
            in UnitAirComponent state,
            in GridConfig grid,
            float minimumPitchProgress)
        {
            if (!TryResolveRunwayDirection(state, out float3 runwayDirection, out _))
                return;

            float3 liftoffPosition = ResolveRunwayLiftoffPosition(state, grid);
            float liftoffDistance = math.max(
                0.01f,
                math.dot(new float3(
                    liftoffPosition.x - state.RunwayTakeoffPosition.x,
                    0f,
                    liftoffPosition.z - state.RunwayTakeoffPosition.z), runwayDirection));
            float travelled = math.dot(
                new float3(
                    transform.Position.x - state.RunwayTakeoffPosition.x,
                    0f,
                    transform.Position.z - state.RunwayTakeoffPosition.z),
                runwayDirection);
            float progressToLiftoff = math.saturate(travelled / liftoffDistance);
            float pitchProgress = math.max(
                math.saturate(minimumPitchProgress),
                math.smoothstep(RunwayTakeoffRotationStartFraction, 1f, progressToLiftoff));
            if (pitchProgress <= 0f)
            {
                transform.Rotation = quaternion.LookRotationSafe(runwayDirection, math.up());
                return;
            }

            float pitchRadians = math.radians(RunwayTakeoffPitchDegrees * pitchProgress);
            float3 pitchedForward = runwayDirection * math.cos(pitchRadians) + new float3(0f, math.sin(pitchRadians), 0f);
            transform.Rotation = quaternion.LookRotationSafe(pitchedForward, math.up());
        }

        private static bool TryResolveRunwayDirection(
            in UnitAirComponent state,
            out float3 runwayDirection,
            out float runwayLength)
        {
            float3 runwayDelta = state.RunwayLandingPosition - state.RunwayTakeoffPosition;
            runwayDelta.y = 0f;
            runwayLength = math.length(runwayDelta);
            if (runwayLength <= 1e-4f)
            {
                runwayDirection = new float3(0f, 0f, 1f);
                return false;
            }

            runwayDirection = runwayDelta / runwayLength;
            return true;
        }

        private static void SampleMaxSurfaceHeight(
            MapSurfaceComponent surface,
            float3 worldPosition,
            ref float bestHeight,
            ref bool found)
        {
            int2 cell = SurfaceWorldToCell(surface, worldPosition);
            ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
            if (!MapSurfaceBlobAccess.TryGetSurfaceRange(ref blob, cell, out MapSurfaceCellSurfaceRange range))
                return;

            for (int i = 0; i < range.SurfaceCount; i++)
            {
                if (!MapSurfaceBlobAccess.TryGetSurface(ref blob, range, i, out MapSurfaceSample sample))
                    continue;

                bestHeight = found ? math.max(bestHeight, sample.Height) : sample.Height;
                found = true;
            }
        }

        private static int2 SurfaceWorldToCell(MapSurfaceComponent surface, float3 worldPosition)
        {
            int2 cell = (int2)math.floor(new float2(
                (worldPosition.x - surface.GridOrigin.x) / surface.CellSize,
                (worldPosition.z - surface.GridOrigin.z) / surface.CellSize));
            return math.clamp(cell, int2.zero, surface.Dimensions - 1);
        }

        private static float ResolveAirClearance(float cruiseHeight, bool fixedWing)
        {
            float minimumClearance = fixedWing ? 28f : 8f;
            return math.max(minimumClearance, math.max(0f, cruiseHeight));
        }

        private static float ResolveRunwayGroundY(in UnitAirComponent state, float fallbackY)
        {
            float3 runwayDelta = state.RunwayLandingPosition - state.RunwayTakeoffPosition;
            runwayDelta.y = 0f;
            if (math.lengthsq(runwayDelta) <= 1e-6f)
                return fallbackY;

            return (state.RunwayTakeoffPosition.y + state.RunwayLandingPosition.y) * 0.5f;
        }

        private static bool FlyTowards(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            in GridConfig grid,
            float speed,
            float deltaTime,
            float desiredY,
            float3 targetWorld,
            bool stayAirborne)
        {
            float3 target = targetWorld;
            target.y = desiredY;
            float3 delta = target - transform.Position;
            float distance = math.length(delta);
            if (distance <= math.max(0.05f, speed * deltaTime))
            {
                transform.Position = target;
                if (!stayAirborne)
                    transform.Position.y = desiredY;
                int2 currentCell = GridUtils.WorldToCell(grid, transform.Position);
                if (GridUtils.InBounds(currentCell, grid.Width, grid.Height))
                    unitGrid.Cell = currentCell;
                return true;
            }

            float3 direction = math.normalizesafe(delta);
            transform.Position += direction * math.max(0.01f, speed) * deltaTime;
            if (math.lengthsq(new float3(direction.x, 0f, direction.z)) > 1e-8f)
                transform.Rotation = quaternion.LookRotationSafe(new float3(direction.x, 0f, direction.z), math.up());

            int2 movedCell = GridUtils.WorldToCell(grid, transform.Position);
            if (GridUtils.InBounds(movedCell, grid.Width, grid.Height))
                unitGrid.Cell = movedCell;
            return false;
        }

        private static bool IsDebugFireTargetForSource(
            ComponentLookup<DebugFireTargetTag> debugFireTargetLookup,
            Entity target,
            Entity source)
        {
            return target != Entity.Null &&
                   debugFireTargetLookup.HasComponent(target) &&
                   debugFireTargetLookup[target].Source == source;
        }

        private static bool IsActiveDebugFireSource(
            ComponentLookup<SelectedUnitDebugFireState> debugFireStateLookup,
            ComponentLookup<DebugFireTargetTag> debugFireTargetLookup,
            Entity source)
        {
            if (!debugFireStateLookup.HasComponent(source))
                return false;

            Entity target = debugFireStateLookup[source].Target;
            return IsDebugFireTargetForSource(debugFireTargetLookup, target, source);
        }

        private static void SuppressDebugFireMovement(ref UnitAirComponent state)
        {
            state.ReturningHome = 0;
            state.TakeoffRolling = 0;
            state.LandingRolling = 0;
            state.AttackRunActive = 0;
            state.ReturnApproachInitialized = 0;
        }

        private static void FaceTarget(ref LocalTransform transform, float3 targetPosition)
        {
            float3 toTarget = targetPosition - transform.Position;
            toTarget.y = 0f;
            if (math.lengthsq(toTarget) > 1e-8f)
                transform.Rotation = quaternion.LookRotationSafe(math.normalizesafe(toTarget, new float3(0f, 0f, 1f)), math.up());
        }

        private static bool SteerTowards(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            in GridConfig grid,
            float speed,
            float deltaTime,
            float desiredY,
            float3 targetWorld,
            bool stayAirborne,
            float turnResponsiveness)
        {
            float3 target = targetWorld;
            target.y = desiredY;
            float3 toTarget = target - transform.Position;
            float3 horizontalToTarget = new float3(toTarget.x, 0f, toTarget.z);
            float horizontalDistance = math.length(horizontalToTarget);
            float verticalDistance = math.abs(target.y - transform.Position.y);
            float stepDistance = math.max(0.01f, speed) * deltaTime;

            if (horizontalDistance <= math.max(0.05f, stepDistance) && verticalDistance <= math.max(0.05f, stepDistance))
            {
                transform.Position = target;
                if (!stayAirborne)
                    transform.Position.y = desiredY;
                int2 currentCell = GridUtils.WorldToCell(grid, transform.Position);
                if (GridUtils.InBounds(currentCell, grid.Width, grid.Height))
                    unitGrid.Cell = currentCell;
                return true;
            }

            float3 currentForward = math.mul(transform.Rotation, new float3(0f, 0f, 1f));
            currentForward.y = 0f;
            currentForward = math.normalizesafe(currentForward, new float3(0f, 0f, 1f));

            float3 desiredForward = math.normalizesafe(horizontalToTarget, currentForward);
            quaternion desiredRotation = quaternion.LookRotationSafe(desiredForward, math.up());
            transform.Rotation = math.slerp(transform.Rotation, desiredRotation, math.saturate(turnResponsiveness * deltaTime));

            float3 steeredForward = math.mul(transform.Rotation, new float3(0f, 0f, 1f));
            steeredForward.y = 0f;
            steeredForward = math.normalizesafe(steeredForward, desiredForward);
            transform.Position += steeredForward * stepDistance;

            float yStep = math.min(stepDistance, verticalDistance);
            if (target.y > transform.Position.y)
                transform.Position.y += yStep;
            else if (target.y < transform.Position.y)
                transform.Position.y -= yStep;

            if (!stayAirborne && verticalDistance <= yStep)
                transform.Position.y = desiredY;

            int2 movedCell = GridUtils.WorldToCell(grid, transform.Position);
            if (GridUtils.InBounds(movedCell, grid.Width, grid.Height))
                unitGrid.Cell = movedCell;
            return false;
        }

        private static float ResolveFixedWingTurnRadius(float speed)
        {
            return math.max(0.01f, speed) / math.radians(FixedWingManeuverTurnRateDegreesPerSecond);
        }

        private static float ResolveFixedWingTurnEntryDistance(in GridConfig grid, float speed)
        {
            return math.max(
                ResolveFixedWingTurnRadius(speed) * FixedWingManeuverTurnEntryRadii,
                grid.CellSize * 16f);
        }

        private static void StartFixedWingPass(
            ref UnitAirComponent state,
            in LocalTransform transform,
            in GridConfig grid,
            float3 passFocus,
            float cruiseY,
            float speed,
            float overshootDistance)
        {
            float3 currentForward = UnitVehicleMovementUtility.Forward(transform.Rotation);
            float3 toFocus = passFocus - transform.Position;
            toFocus.y = 0f;
            float focusDistance = math.length(toFocus);
            float3 focusDirection = math.normalizesafe(toFocus, currentForward);
            float setupTurnRadians = math.abs(UnitVehicleMovementUtility.SignedAngleY(currentForward, focusDirection));
            if (math.degrees(setupTurnRadians) <= FixedWingManeuverDirectPassAngleDegrees)
            {
                StartFixedWingFinalPass(ref state, transform, passFocus, cruiseY, overshootDistance);
                return;
            }

            // Climb out straight ahead until the focus sits far enough away to carve one wide
            // banked turn back onto it, then arc in the maneuver phase.
            float turnEntryDistance = ResolveFixedWingTurnEntryDistance(grid, speed);
            float minExtendDistance = math.max(
                grid.CellSize * 6f,
                math.max(0.01f, speed) * FixedWingManeuverMinExtendSeconds);
            float extendDistance = minExtendDistance;
            float lateralDistance = focusDistance * math.sin(setupTurnRadians);
            if (focusDistance < turnEntryDistance && lateralDistance < turnEntryDistance)
            {
                float alongDistance = focusDistance * math.cos(setupTurnRadians);
                float reachDistance = math.sqrt(math.max(
                    0f,
                    turnEntryDistance * turnEntryDistance - lateralDistance * lateralDistance));
                extendDistance = math.max(minExtendDistance, alongDistance + reachDistance);
            }

            float3 extendWaypoint = transform.Position + currentForward * extendDistance;
            extendWaypoint.y = cruiseY;
            state.AttackRunActive = FixedWingPassExtendPhase;
            state.AttackRunExitPosition = extendWaypoint;
        }

        // Runs the extend and arc phases of a fixed-wing pass. Returns true once the pass is in
        // the final phase and the caller should fly its straight run over the focus.
        private static bool UpdateFixedWingApproach(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            ref UnitAirComponent state,
            in GridConfig grid,
            float speed,
            float deltaTime,
            float cruiseY,
            float3 passFocus,
            float overshootDistance)
        {
            if (state.AttackRunActive == 0)
                StartFixedWingPass(ref state, transform, grid, passFocus, cruiseY, speed, overshootDistance);

            if (state.AttackRunActive == FixedWingPassExtendPhase)
            {
                bool reachedWaypoint = SteerFixedWingTowards(
                    ref transform,
                    ref unitGrid,
                    grid,
                    speed,
                    deltaTime,
                    cruiseY,
                    state.AttackRunExitPosition);

                float3 toFocus = passFocus - transform.Position;
                toFocus.y = 0f;
                float turnEntryDistance = ResolveFixedWingTurnEntryDistance(grid, speed);
                if (reachedWaypoint || math.lengthsq(toFocus) >= turnEntryDistance * turnEntryDistance)
                {
                    float3 forward = UnitVehicleMovementUtility.Forward(transform.Rotation);
                    float signedTurn = UnitVehicleMovementUtility.SignedAngleY(
                        forward,
                        math.normalizesafe(toFocus, forward));
                    state.AttackRunActive = FixedWingPassManeuverPhase;
                    state.AttackManeuverTurnSign = (sbyte)(signedTurn < 0f ? -1 : 1);
                }

                return false;
            }

            if (state.AttackRunActive == FixedWingPassManeuverPhase)
            {
                float3 forward = UnitVehicleMovementUtility.Forward(transform.Rotation);
                float3 toFocus = passFocus - transform.Position;
                toFocus.y = 0f;
                float focusDistance = math.length(toFocus);
                float3 focusDirection = math.normalizesafe(toFocus, forward);
                float signedToFocus = UnitVehicleMovementUtility.SignedAngleY(forward, focusDirection);
                float toFocusDegrees = math.degrees(math.abs(signedToFocus));

                if (toFocusDegrees <= FixedWingManeuverLineupAngleDegrees)
                {
                    StartFixedWingFinalPass(ref state, transform, passFocus, cruiseY, overshootDistance);
                    return true;
                }

                if (focusDistance < ResolveFixedWingTurnRadius(speed) * FixedWingManeuverGoAroundRadii &&
                    toFocusDegrees > 90f)
                {
                    // Too close to line up on this circuit; extend away and go around.
                    StartFixedWingPass(ref state, transform, grid, passFocus, cruiseY, speed, overshootDistance);
                    SteerFixedWingTowards(
                        ref transform,
                        ref unitGrid,
                        grid,
                        speed,
                        deltaTime,
                        cruiseY,
                        state.AttackRunExitPosition);
                    return false;
                }

                float maxYawStep = math.radians(FixedWingManeuverTurnRateDegreesPerSecond) * math.max(0f, deltaTime);
                float yawStep = math.clamp(signedToFocus, -maxYawStep, maxYawStep);
                ApplyFixedWingFlight(ref transform, ref unitGrid, grid, speed, deltaTime, cruiseY, yawStep, maxYawStep);
                return false;
            }

            return state.AttackRunActive == FixedWingPassFinalPhase;
        }

        private static void StartFixedWingFinalPass(
            ref UnitAirComponent state,
            in LocalTransform transform,
            float3 passFocus,
            float cruiseY,
            float overshootDistance)
        {
            float3 focus = passFocus;
            focus.y = cruiseY;
            float3 passDirection = focus - transform.Position;
            passDirection.y = 0f;
            passDirection = math.normalizesafe(passDirection, UnitVehicleMovementUtility.Forward(transform.Rotation));

            state.AttackRunActive = FixedWingPassFinalPhase;
            state.AttackRunExitPosition = new float3(
                focus.x + passDirection.x * overshootDistance,
                cruiseY,
                focus.z + passDirection.z * overshootDistance);
        }

        private static float3 ResolveFixedWingFinalSteerTarget(
            float3 passFocus,
            float3 passExit,
            float3 currentPosition,
            in GridConfig grid,
            float speed)
        {
            float3 exitVector = passExit - passFocus;
            exitVector.y = 0f;
            float exitLength = math.length(exitVector);
            if (exitLength <= 1e-5f)
                return passExit;

            float3 passDirection = exitVector / exitLength;
            float3 progressedVector = currentPosition - passFocus;
            progressedVector.y = 0f;
            float passProgress = math.dot(progressedVector, passDirection);
            if (passProgress >= exitLength * 0.08f)
                return passExit;

            float lookaheadDistance = math.max(grid.CellSize * FixedWingFinalPassLookaheadCells, math.max(0.01f, speed) * 1.2f);
            float targetProgress = math.clamp(passProgress + lookaheadDistance, 0f, exitLength);
            return new float3(
                passFocus.x + passDirection.x * targetProgress,
                passExit.y,
                passFocus.z + passDirection.z * targetProgress);
        }

        private static bool SteerFixedWingTowards(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            in GridConfig grid,
            float speed,
            float deltaTime,
            float desiredY,
            float3 targetWorld)
        {
            float3 target = targetWorld;
            target.y = desiredY;
            float3 toTarget = target - transform.Position;
            float3 horizontalToTarget = new float3(toTarget.x, 0f, toTarget.z);
            float horizontalDistance = math.length(horizontalToTarget);
            float verticalDistance = math.abs(target.y - transform.Position.y);
            float stepDistance = math.max(0.01f, speed) * deltaTime;

            if (horizontalDistance <= math.max(0.05f, stepDistance) &&
                verticalDistance <= math.max(0.05f, stepDistance))
            {
                transform.Position = target;
                int2 currentCell = GridUtils.WorldToCell(grid, transform.Position);
                if (GridUtils.InBounds(currentCell, grid.Width, grid.Height))
                    unitGrid.Cell = currentCell;
                return true;
            }

            float3 currentForward = UnitVehicleMovementUtility.Forward(transform.Rotation);
            float3 desiredForward = math.normalizesafe(horizontalToTarget, currentForward);
            float signedYawAngle = UnitVehicleMovementUtility.SignedAngleY(currentForward, desiredForward);
            float maxYawStep = math.radians(FixedWingManeuverTurnRateDegreesPerSecond) * math.max(0f, deltaTime);
            float yawStep = math.clamp(signedYawAngle, -maxYawStep, maxYawStep);
            ApplyFixedWingFlight(ref transform, ref unitGrid, grid, speed, deltaTime, desiredY, yawStep, maxYawStep);
            return false;
        }

        // Advances one frame of fixed-wing flight: yaws by the rate-limited step, rolls into the
        // turn, pitches with climb or descent, and moves forward at full speed.
        private static void ApplyFixedWingFlight(
            ref LocalTransform transform,
            ref UnitGrid unitGrid,
            in GridConfig grid,
            float speed,
            float deltaTime,
            float desiredY,
            float yawStepRadians,
            float maxYawStepRadians)
        {
            float3 currentForward = UnitVehicleMovementUtility.Forward(transform.Rotation);
            float3 newForward = math.normalizesafe(
                math.rotate(quaternion.RotateY(yawStepRadians), currentForward),
                currentForward);

            float stepDistance = math.max(0.01f, speed) * deltaTime;
            transform.Position += newForward * stepDistance;

            float verticalDelta = desiredY - transform.Position.y;
            float yStep = math.min(stepDistance, math.abs(verticalDelta));
            transform.Position.y += math.sign(verticalDelta) * yStep;

            float bankFraction = maxYawStepRadians > 1e-6f
                ? math.clamp(yawStepRadians / maxYawStepRadians, -1f, 1f)
                : 0f;
            float desiredBank = -bankFraction * math.radians(FixedWingManeuverBankMaxDegrees);
            float3 currentRight = math.mul(transform.Rotation, new float3(1f, 0f, 0f));
            float currentBank = math.asin(math.clamp(currentRight.y, -1f, 1f));
            float bankStep = math.radians(FixedWingManeuverBankRateDegreesPerSecond) * math.max(0f, deltaTime);
            float newBank = currentBank + math.clamp(desiredBank - currentBank, -bankStep, bankStep);

            float3 rawForward = math.mul(transform.Rotation, new float3(0f, 0f, 1f));
            float currentPitch = math.asin(math.clamp(rawForward.y, -1f, 1f));
            float maxPitch = math.radians(FixedWingManeuverPitchMaxDegrees);
            float desiredPitch = stepDistance > 1e-6f
                ? math.clamp(math.atan2(math.sign(verticalDelta) * yStep, stepDistance), -maxPitch, maxPitch)
                : 0f;
            float pitchStep = math.radians(FixedWingManeuverPitchRateDegreesPerSecond) * math.max(0f, deltaTime);
            float newPitch = currentPitch + math.clamp(desiredPitch - currentPitch, -pitchStep, pitchStep);

            float3 pitchedForward = newForward * math.cos(newPitch) + new float3(0f, math.sin(newPitch), 0f);
            transform.Rotation = math.mul(
                quaternion.LookRotationSafe(pitchedForward, math.up()),
                quaternion.RotateZ(newBank));

            int2 movedCell = GridUtils.WorldToCell(grid, transform.Position);
            if (GridUtils.InBounds(movedCell, grid.Width, grid.Height))
                unitGrid.Cell = movedCell;
        }

    }
}
