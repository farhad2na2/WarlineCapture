using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(EngageTargetValidateSystem))]
[UpdateAfter(typeof(EngageTargetSyncSystem))]
[UpdateBefore(typeof(UnitEngagedMovementSystem))]
[UpdateBefore(typeof(UnitGridMovementSystem))]
public partial struct UnitAirMovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitAirMovement>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingleton<GridConfig>();
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

        foreach (var (transform, unitGrid, move, attack, airMovement, airState, entity) in SystemAPI
                     .Query<RefRW<LocalTransform>, RefRW<UnitGrid>, RefRO<UnitMove>, RefRO<UnitAttack>, RefRO<UnitAirMovement>, RefRW<UnitAirComponent>>()
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
                float hoverY = groundY + airMovement.ValueRO.CruiseHeight;
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
                        bool reachedLiftoffPoint = SteerTowards(
                            ref transform.ValueRW,
                            ref unitGrid.ValueRW,
                            grid,
                            math.max(0.01f, move.ValueRO.Speed),
                            dt,
                            runwayGroundY,
                            stateRw.RunwayLandingPosition,
                            false,
                            3.25f);

                        if (reachedLiftoffPoint)
                        {
                            stateRw.TakeoffRolling = 0;
                            stateRw.Airborne = 1;
                            unitGrid.ValueRW.Cell = stateRw.RunwayLandingCell;
                        }
                    }

                    continue;
                }

                float cruiseY = groundY + airMovement.ValueRO.CruiseHeight;
                if (stateRw.UsesRunway != 0)
                {
                    float3 horizontalToTarget = engageTargetPosition - transform.ValueRO.Position;
                    horizontalToTarget.y = 0f;
                    float attackRange = math.max(0.01f, attack.ValueRO.Range);
                    if (stateRw.AttackRunActive == 0)
                    {
                        float3 passDirection = math.normalizesafe(horizontalToTarget);
                        if (math.lengthsq(passDirection) <= 1e-6f)
                        {
                            passDirection = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                            passDirection.y = 0f;
                            passDirection = math.normalizesafe(passDirection, new float3(0f, 0f, 1f));
                        }

                        float overshootDistance = math.max(attackRange * 8f, grid.CellSize * 40f);
                        stateRw.AttackRunActive = 1;
                        stateRw.AttackRunExitPosition = new float3(
                            engageTargetPosition.x + passDirection.x * overshootDistance,
                            cruiseY,
                            engageTargetPosition.z + passDirection.z * overshootDistance);
                    }

                    bool completedAttackRun = SteerTowards(
                        ref transform.ValueRW,
                        ref unitGrid.ValueRW,
                        grid,
                        move.ValueRO.Speed,
                        dt,
                        cruiseY,
                        stateRw.AttackRunExitPosition,
                        true,
                        1.15f);

                    if (stateRw.AttackRunActive != 0)
                    {
                        completedAttackRun = FlyTowards(
                            ref transform.ValueRW,
                            ref unitGrid.ValueRW,
                            grid,
                            move.ValueRO.Speed,
                            dt,
                            cruiseY,
                            stateRw.AttackRunExitPosition,
                            true);
                    }

                    if (stateRw.AttackRunActive != 0)
                    {
                        float3 exitVector = stateRw.AttackRunExitPosition - new float3(engageTargetPosition.x, cruiseY, engageTargetPosition.z);
                        float3 progressedVector = transform.ValueRO.Position - new float3(engageTargetPosition.x, cruiseY, engageTargetPosition.z);
                        exitVector.y = 0f;
                        progressedVector.y = 0f;
                        float exitLengthSq = math.lengthsq(exitVector);
                        if (exitLengthSq > 1e-6f && math.dot(progressedVector, exitVector) >= exitLengthSq * 0.92f)
                            completedAttackRun = true;
                    }

                    if (stateRw.AttackRunActive != 0 && completedAttackRun)
                    {
                        stateRw.AttackRunActive = 0;
                        stateRw.ReturningHome = 1;
                        stateRw.ReturnApproachInitialized = 0;
                        ecb.RemoveComponent<EngageTarget>(entity);
                    }
                }
                else
                {
                    FlyTowards(ref transform.ValueRW, ref unitGrid.ValueRW, grid, move.ValueRO.Speed, dt, cruiseY, engageTargetPosition, true);
                }

                continue;
            }

            stateRw.AttackRunActive = 0;
            bool hasActiveScanOrder = scanOrderLookup.HasComponent(entity);
            bool holdingPosition = holdPositionLookup.HasComponent(entity);

            bool hasDirectTarget = targetLookup.HasComponent(entity);
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
                            bool reachedLiftoffPoint = SteerTowards(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                grid,
                                math.max(0.01f, move.ValueRO.Speed),
                                dt,
                                runwayGroundY,
                                stateRw.RunwayLandingPosition,
                                false,
                                3.25f);

                            if (reachedLiftoffPoint)
                            {
                                stateRw.TakeoffRolling = 0;
                                stateRw.Airborne = 1;
                                unitGrid.ValueRW.Cell = stateRw.RunwayLandingCell;
                            }

                            reached = false;
                        }
                    }
                    else
                    {
                        float cruiseY = groundY + airMovement.ValueRO.CruiseHeight;
                        float3 movePassTarget = new float3(targetWorld.x, cruiseY, targetWorld.z);
                        if (stateRw.AttackRunActive == 0)
                        {
                            float3 passDirection = math.normalizesafe(movePassTarget - transform.ValueRO.Position);
                            if (math.lengthsq(passDirection) <= 1e-6f)
                            {
                                passDirection = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                                passDirection.y = 0f;
                                passDirection = math.normalizesafe(passDirection, new float3(0f, 0f, 1f));
                            }

                            float overshootDistance = math.max(grid.CellSize * 10f, move.ValueRO.Speed * 1.5f);
                            stateRw.AttackRunActive = 1;
                            stateRw.AttackRunExitPosition = new float3(
                                movePassTarget.x + passDirection.x * overshootDistance,
                                cruiseY,
                                movePassTarget.z + passDirection.z * overshootDistance);
                        }

                        bool completedPass = SteerTowards(
                            ref transform.ValueRW,
                            ref unitGrid.ValueRW,
                            grid,
                            move.ValueRO.Speed,
                            dt,
                            cruiseY,
                            stateRw.AttackRunExitPosition,
                            true,
                            1.15f);

                        if (stateRw.AttackRunActive != 0)
                        {
                            completedPass = FlyTowards(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                grid,
                                move.ValueRO.Speed,
                                dt,
                                cruiseY,
                                stateRw.AttackRunExitPosition,
                                true);
                        }

                        float3 exitVector = stateRw.AttackRunExitPosition - movePassTarget;
                        float3 progressedVector = transform.ValueRO.Position - movePassTarget;
                        exitVector.y = 0f;
                        progressedVector.y = 0f;
                        float exitLengthSq = math.lengthsq(exitVector);
                        if (exitLengthSq > 1e-6f && math.dot(progressedVector, exitVector) >= exitLengthSq * 0.85f)
                            completedPass = true;

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
                        isSpawnTransit ? groundY : groundY + airMovement.ValueRO.CruiseHeight,
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
                        float cruiseY = groundY + airMovement.ValueRO.CruiseHeight;
                        float3 runwayDirection = stateRw.RunwayLandingPosition - stateRw.RunwayTakeoffPosition;
                        runwayDirection.y = 0f;
                        runwayDirection = math.normalizesafe(runwayDirection, new float3(0f, 0f, 1f));
                        float approachDistance = math.max(grid.CellSize * 18f, math.distance(stateRw.RunwayTakeoffPosition, stateRw.RunwayLandingPosition) * 0.75f);
                        float3 approachPoint = new float3(
                            stateRw.RunwayTakeoffPosition.x - runwayDirection.x * approachDistance,
                            cruiseY,
                            stateRw.RunwayTakeoffPosition.z - runwayDirection.z * approachDistance);

                        if (stateRw.ReturnApproachInitialized == 0)
                        {
                            bool reachedApproachPoint = FlyTowards(
                                ref transform.ValueRW,
                                ref unitGrid.ValueRW,
                                grid,
                                move.ValueRO.Speed,
                                dt,
                                cruiseY,
                                approachPoint,
                                true);
                            if (!reachedApproachPoint)
                                continue;

                            transform.ValueRW.Rotation = quaternion.LookRotationSafe(runwayDirection, math.up());
                            stateRw.ReturnApproachInitialized = 1;
                        }

                        float3 currentForward = math.mul(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
                        currentForward.y = 0f;
                        currentForward = math.normalizesafe(currentForward, runwayDirection);
                        float runwayAlignment = math.dot(currentForward, runwayDirection);

                        float3 toRunwayStart = stateRw.RunwayTakeoffPosition - transform.ValueRO.Position;
                        float alongRunway = math.dot(new float3(toRunwayStart.x, 0f, toRunwayStart.z), runwayDirection);
                        float3 lateralVector = new float3(toRunwayStart.x, 0f, toRunwayStart.z) - runwayDirection * alongRunway;
                        bool canDescendToRunway = runwayAlignment >= 0.995f && math.lengthsq(lateralVector) <= grid.CellSize * grid.CellSize * 2f && alongRunway >= 0f;

                        float3 airborneTarget = canDescendToRunway
                            ? stateRw.RunwayTakeoffPosition
                            : approachPoint;
                        float airborneTargetY = canDescendToRunway ? runwayGroundY : cruiseY;

                        bool reachedTouchdown = SteerTowards(
                            ref transform.ValueRW,
                            ref unitGrid.ValueRW,
                            grid,
                            move.ValueRO.Speed,
                            dt,
                            airborneTargetY,
                            airborneTarget,
                            !canDescendToRunway,
                            1.15f);

                        if (canDescendToRunway && reachedTouchdown)
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
                    bool reachedHome = FlyTowards(ref transform.ValueRW, ref unitGrid.ValueRW, grid, move.ValueRO.Speed, dt, groundY, stateRw.HomePosition, false);
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

}
