using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public readonly struct UnitTransportBoardingRuleSystem
{
    public const int BoardingClearanceCells = 4;
    public const int AirBoardingClearanceCells = 1;
    public const float AirBoardingGroundedHeightTolerance = 3f;

    public readonly struct ReachState
    {
        public readonly int2 TransportCell;
        public readonly int2 TransportSize;
        public readonly int2 PassengerCell;
        public readonly int2 BoardingGoal;
        public readonly int BoardingClearance;
        public readonly bool MovementFinished;
        public readonly bool AirTransport;
        public readonly bool ReachedBoardingGoal;
        public readonly int DistanceToBoardingGoal;
        public readonly bool SettledNearBoardingGoal;
        public readonly bool NearTransportFootprint;
        public readonly bool BoardingGoalNearTransport;
        public readonly bool ReachedTransport;

        public ReachState(
            int2 transportCell,
            int2 transportSize,
            int2 passengerCell,
            int2 boardingGoal,
            int boardingClearance,
            bool movementFinished,
            bool airTransport,
            bool reachedBoardingGoal,
            int distanceToBoardingGoal,
            bool settledNearBoardingGoal,
            bool nearTransportFootprint,
            bool boardingGoalNearTransport,
            bool reachedTransport)
        {
            TransportCell = transportCell;
            TransportSize = transportSize;
            PassengerCell = passengerCell;
            BoardingGoal = boardingGoal;
            BoardingClearance = boardingClearance;
            MovementFinished = movementFinished;
            AirTransport = airTransport;
            ReachedBoardingGoal = reachedBoardingGoal;
            DistanceToBoardingGoal = distanceToBoardingGoal;
            SettledNearBoardingGoal = settledNearBoardingGoal;
            NearTransportFootprint = nearTransportFootprint;
            BoardingGoalNearTransport = boardingGoalNearTransport;
            ReachedTransport = reachedTransport;
        }
    }

    public bool IsTransportLandedForBoarding(EntityManager em, Entity transport)
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

    public int GetTransportBoardingDirectCells(EntityManager em, Entity transport)
    {
        return em.HasComponent<UnitAirMovement>(transport)
            ? AirBoardingClearanceCells
            : BoardingClearanceCells;
    }

    public ReachState EvaluateReach(
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

        return new ReachState(
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
