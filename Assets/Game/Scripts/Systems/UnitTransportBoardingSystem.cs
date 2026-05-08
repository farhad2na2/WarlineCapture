using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct UnitTransportBoardingSystem : ISystem
{
    private const int BoardingClearanceCells = 4;
    private const int AirBoardingClearanceCells = 1;
    private const float AirBoardingGroundedHeightTolerance = 3f;
    private const int DiagnosticLogIntervalFrames = 180;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitTransportBoardingTarget>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
        EntityManager em = state.EntityManager;

        foreach (var (boarding, passengerGrid, passengerTransform, entity) in
                 SystemAPI.Query<RefRO<UnitTransportBoardingTarget>, RefRO<UnitGrid>, RefRO<LocalTransform>>()
                     .WithNone<Disabled>()
                     .WithEntityAccess())
        {
            Entity transport = boarding.ValueRO.Transport;
            if (!em.Exists(transport) ||
                !em.HasComponent<UnitTransportCapacity>(transport) ||
                !em.HasBuffer<UnitTransportPassengerElement>(transport) ||
                !em.HasComponent<UnitGrid>(transport) ||
                !em.HasComponent<UnitFootprint>(transport) ||
                !em.HasComponent<LocalTransform>(transport))
            {
                LogDiagnostic($"result=Cancel reason=TransportMissingOrInvalid passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)}");
                ecb.RemoveComponent<UnitTransportBoardingTarget>(entity);
                continue;
            }

            if (!IsTransportLandedForBoarding(em, transport))
            {
                LogPeriodic(entity, $"result=Waiting reason=TransportNotLanded passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)} {DescribeAirState(em, transport)}");
                continue;
            }

            DynamicBuffer<UnitTransportPassengerElement> passengers = em.GetBuffer<UnitTransportPassengerElement>(transport);
            int capacity = math.max(0, em.GetComponentData<UnitTransportCapacity>(transport).SoldierCapacity);
            if (passengers.Length >= capacity)
            {
                LogDiagnostic($"result=Cancel reason=NoSeats passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)} seats={passengers.Length}/{capacity}");
                ecb.RemoveComponent<UnitTransportBoardingTarget>(entity);
                continue;
            }

            int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
            int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
            int2 passengerCell = passengerGrid.ValueRO.Cell;
            int2 boardingGoal = boarding.ValueRO.Goal;
            float3 transportPosition = em.GetComponentData<LocalTransform>(transport).Position;
            float3 passengerPosition = passengerTransform.ValueRO.Position;
            passengerPosition.y = transportPosition.y;
            int boardingClearance = em.HasComponent<UnitAirMovement>(transport)
                ? AirBoardingClearanceCells
                : BoardingClearanceCells;

            bool movementFinished =
                !em.HasComponent<UnitTarget>(entity) &&
                !em.HasComponent<UnitPathRequest>(entity) &&
                !em.HasComponent<UnitPathFollow>(entity);
            bool airTransport = em.HasComponent<UnitAirMovement>(transport);
            int2 boardingTransportSize = airTransport ? new int2(1, 1) : transportSize;
            bool reachedBoardingGoal = passengerCell.Equals(boardingGoal);
            int distanceToBoardingGoal = math.max(math.abs(passengerCell.x - boardingGoal.x), math.abs(passengerCell.y - boardingGoal.y));
            bool settledNearBoardingGoal = movementFinished && distanceToBoardingGoal <= (airTransport ? 0 : boardingClearance);
            bool nearTransportFootprint = UnitFootprintUtility.ContainsCellWithPadding(transportCell, boardingTransportSize, passengerCell, boardingClearance);
            bool boardingGoalNearTransport = UnitFootprintUtility.ContainsCellWithPadding(transportCell, boardingTransportSize, boardingGoal, boardingClearance);
            float boardDistanceSq = airTransport ? 1.25f * 1.25f : 4f;
            int boardCellDistance = airTransport ? 1 : 2;
            bool reachedTransport =
                nearTransportFootprint ||
                (boardingGoalNearTransport && (reachedBoardingGoal || settledNearBoardingGoal)) ||
                math.distancesq(passengerPosition, transportPosition) <= boardDistanceSq ||
                math.max(math.abs(passengerCell.x - transportCell.x), math.abs(passengerCell.y - transportCell.y)) <= boardCellDistance;
            if (!reachedTransport)
            {
                LogPeriodic(
                    entity,
                    $"result=Waiting reason=NotReached passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)} " +
                    $"passengerCell={passengerCell} goal={boardingGoal} transportCell={transportCell} transportSize={transportSize} " +
                    $"distGoal={distanceToBoardingGoal} clearance={boardingClearance} movementFinished={(movementFinished ? 1 : 0)} " +
                    $"hasTarget={(em.HasComponent<UnitTarget>(entity) ? 1 : 0)} hasRequest={(em.HasComponent<UnitPathRequest>(entity) ? 1 : 0)} hasFollow={(em.HasComponent<UnitPathFollow>(entity) ? 1 : 0)} " +
                    $"reachedGoal={(reachedBoardingGoal ? 1 : 0)} settledNearGoal={(settledNearBoardingGoal ? 1 : 0)} nearTransport={(nearTransportFootprint ? 1 : 0)} seats={passengers.Length}/{capacity}");
                continue;
            }

            passengers.Add(new UnitTransportPassengerElement { Passenger = entity });
            LogDiagnostic($"result=Boarded passenger={DescribeBoardingEntity(em, entity)} transport={DescribeBoardingEntity(em, transport)} seats={passengers.Length}/{capacity}");
            UnitTransportVisualUtility.SetPassengerHidden(em, entity, ecb);
            ecb.RemoveComponent<UnitTransportBoardingTarget>(entity);
            RemoveIfPresent<UnitTarget>(ref ecb, em, entity);
            RemoveIfPresent<UnitPathRequest>(ref ecb, em, entity);
            RemoveIfPresent<UnitPathFollow>(ref ecb, em, entity);
            RemoveIfPresent<UnitPathRange>(ref ecb, em, entity);
            RemoveIfPresent<ManualMoveOrderTag>(ref ecb, em, entity);
            RemoveIfPresent<AutoWanderMoveTag>(ref ecb, em, entity);
            RemoveIfPresent<EngageTarget>(ref ecb, em, entity);
            RemoveIfPresent<SelectedUnitTag>(ref ecb, em, entity);
            ecb.AddComponent(entity, new UnitTransportPassenger { Transport = transport });
            ecb.AddComponent<Disabled>(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static void RemoveIfPresent<T>(ref EntityCommandBuffer ecb, EntityManager em, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (em.HasComponent<T>(entity))
            ecb.RemoveComponent<T>(entity);
    }

    private static bool IsTransportLandedForBoarding(EntityManager em, Entity transport)
    {
        if (!em.HasComponent<UnitAirMovement>(transport))
            return true;

        if (!em.HasComponent<UnitAirState>(transport) || !em.HasComponent<LocalTransform>(transport))
            return false;

        UnitAirState airState = em.GetComponentData<UnitAirState>(transport);
        LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
        bool physicallyGrounded = transform.Position.y <= groundY + AirBoardingGroundedHeightTolerance;
        return airState.Airborne == 0 &&
               airState.TakeoffRolling == 0 &&
               airState.LandingRolling == 0 &&
               physicallyGrounded &&
               !em.HasComponent<UnitTransportRopeDisembarkRequest>(transport);
    }

    private static void LogDiagnostic(string message)
    {
        if (InitialUnitsRuntimeState.TransportBoardingDiagnostics)
            Debug.Log($"[TransportBoard] {message}");
    }

    private static void LogPeriodic(Entity entity, string message)
    {
        if (!InitialUnitsRuntimeState.TransportBoardingDiagnostics)
            return;

        if (Time.frameCount % DiagnosticLogIntervalFrames == 0)
            Debug.Log($"[TransportBoard] {message}");
    }

    private static string DescribeBoardingEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null)
            return "null";
        if (!em.Exists(entity))
            return $"{entity}:missing";

        string sourceName = ResolveSourceName(em, entity);
        if (string.IsNullOrWhiteSpace(sourceName))
            sourceName = "<unnamed>";

        string cell = em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "no-cell";
        string faction = em.HasComponent<Faction>(entity)
            ? em.GetComponentData<Faction>(entity).Id.ToString()
            : "no-faction";
        string health = em.HasComponent<UnitHealth>(entity)
            ? $"{em.GetComponentData<UnitHealth>(entity).Current}/{em.GetComponentData<UnitHealth>(entity).Max}"
            : "no-health";

        return $"{sourceName} entity={entity} cell={cell} faction={faction} health={health}";
    }

    private static string DescribeAirState(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity) || !em.HasComponent<UnitAirMovement>(entity))
            return "air=none";
        if (!em.HasComponent<UnitAirState>(entity))
            return "air=missing-state";

        UnitAirState airState = em.GetComponentData<UnitAirState>(entity);
        return $"airborne={airState.Airborne} takeoff={airState.TakeoffRolling} landing={airState.LandingRolling} returning={airState.ReturningHome} rope={(em.HasComponent<UnitTransportRopeDisembarkRequest>(entity) ? 1 : 0)}";
    }

    private static string ResolveSourceName(EntityManager em, Entity entity)
    {
        if (!em.Exists(entity))
            return string.Empty;

        if (em.HasComponent<UnitSourcePrefabKey>(entity))
        {
            string sourceName = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString();
            if (!string.IsNullOrWhiteSpace(sourceName))
                return sourceName;
        }

        return em.GetName(entity);
    }
}
