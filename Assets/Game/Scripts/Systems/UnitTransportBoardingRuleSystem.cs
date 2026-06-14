using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public readonly struct UnitTransportBoardingRuleSystem
{
    public const int BoardingClearanceCells = TransportBoardingData.BoardingClearanceCells;
    public const int AirBoardingClearanceCells = TransportBoardingData.AirBoardingClearanceCells;
    public const float AirBoardingGroundedHeightTolerance = TransportBoardingData.AirBoardingGroundedHeightTolerance;

    public bool IsTransportLandedForBoarding(EntityManager em, Entity transport)
    {
        if (!em.HasComponent<UnitAirMovement>(transport))
            return true;

        if (!em.HasComponent<UnitAirComponent>(transport) || !em.HasComponent<LocalTransform>(transport))
            return false;

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(transport);
        LocalTransform transform = em.GetComponentData<LocalTransform>(transport);
        float groundY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
        bool physicallyGrounded = transform.Position.y <= groundY + AirBoardingGroundedHeightTolerance;
        return airState.Airborne == 0 &&
               airState.TakeoffRolling == 0 &&
               airState.LandingRolling == 0 &&
               physicallyGrounded &&
               !em.HasComponent<UnitTransportRopeDisembarkRequest>(transport);
    }

    public int GetTransportBoardingDirectCells(EntityManager em, Entity transport)
    {
        return em.HasComponent<UnitAirMovement>(transport)
            ? AirBoardingClearanceCells
            : BoardingClearanceCells;
    }

    public TransportBoardingReachState EvaluateReach(
        EntityManager em,
        Entity passenger,
        Entity transport,
        int2 passengerCell,
        int2 boardingGoal,
        float3 passengerPosition)
    {
        int2 transportCell = em.GetComponentData<UnitGrid>(transport).Cell;
        int2 transportSize = em.GetComponentData<UnitFootprint>(transport).Size;
        float3 transportPosition = em.GetComponentData<LocalTransform>(transport).Position;
        passengerPosition.y = transportPosition.y;
        bool airTransport = em.HasComponent<UnitAirMovement>(transport);
        int boardingClearance = airTransport ? AirBoardingClearanceCells : BoardingClearanceCells;
        bool movementFinished =
            !em.HasComponent<UnitTarget>(passenger) &&
            !em.HasComponent<UnitPathRequest>(passenger) &&
            !em.HasComponent<UnitPathFollow>(passenger);
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

        return new TransportBoardingReachState(
            transportCell,
            transportSize,
            passengerCell,
            boardingGoal,
            boardingClearance,
            movementFinished,
            airTransport,
            reachedBoardingGoal,
            distanceToBoardingGoal,
            settledNearBoardingGoal,
            nearTransportFootprint,
            boardingGoalNearTransport,
            reachedTransport);
    }
}
