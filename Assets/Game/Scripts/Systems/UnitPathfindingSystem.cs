using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using System.Text;
using UnityEngine;

public partial struct UnitPathfindingSystem : ISystem
{
    public static bool HasPendingPathJob { get; private set; }

    private static readonly bool EnablePathDiagnostics = false;
    private static readonly bool EnablePathFreezeLogs = false;
    private static readonly bool EnableHierarchicalPathValidationLog = false;
    private const double FreezeLogThresholdSeconds = 0.05d;
    private const double TargetPathJobWallSeconds = 0.008d;
    private const double LowPathJobWallSeconds = 0.006d;
    private const double HighPathJobWallSeconds = 0.012d;
    private const int MaxRequestsPerFrame = 32;
    private const int MinRequestsPerFrame = 1;
    private const int MaxManualInfantryRequestsPerFrame = 4;
    private const int StableManualInfantryBatchesBeforeIncrease = 2;
    private const int StableOneFrameBatchesBeforeIncrease = 3;
    private const int FreeTraversalCost = 10;
    private const int FreeDiagonalTraversalCost = 14;
    private const int PreferredSurfaceTraversalCost = 6;
    private const int PreferredSurfaceDiagonalTraversalCost = 8;
    private const int AvoidedSurfaceTraversalCost = 18;
    private const int AvoidedSurfaceDiagonalTraversalCost = 25;
    private const int OccupiedTraversalPenalty = 50;
    private const int VehicleOccupancyPaddingCells = 1;
    private const int InfantryGoalSearchRadius = 10;
    private const int VehicleGoalSearchRadius = 20;
    private const int InfantryAlternateGoalCandidates = 16;
    private const int VehicleAlternateGoalCandidates = 32;
    private const int InfantryMaxAStarExpansions = 450;
    private const int InfantrySegmentedMaxAStarExpansions = 30000;
    private const int VehicleMaxAStarExpansions = 1600;
    private const int InfantrySearchBoundsPaddingCells = 8;
    private const int InfantrySegmentedSearchBoundsPaddingCells = 180;
    private const int VehicleSearchBoundsPaddingCells = 12;
    private const float DefaultLongDistanceSegmentCells = 32f;
    private const float ManualInfantryLongDistanceSegmentCells = 1024f;
    private const float ManualVehicleLongDistanceSegmentCells = 128f;
    private const int HierarchicalSectorSizeCells = 32;
    private const int HierarchicalMaxExpandedSectors = 2048;
    private const int FailedManualRetryDelayFrames = 8;
    private const int ScratchEpochsPerRequest = 128;
    private const int ValidationStuckLogIntervalFrames = 180;
    private const int ValidationStuckLogFirstDelayFrames = 180;
    private const int ValidationStuckSampleCount = 6;
    private static readonly int2[] SearchDirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    private EntityQuery _requestQuery;
    private EntityQuery _liveUnitsQuery;
    private EntityQuery _pendingManualMoveQuery;
    private EntityQuery _pathFollowQuery;
    private EntityQuery _longDistanceMoveQuery;
    private EntityQuery _retryCooldownQuery;
    private EntityQuery _manualRequestQuery;
    private EntityQuery _manualPathFollowQuery;
    private int _scratchGridSize;
    private int _reservedGoalGridSize;
    private int _reservedGoalGeneration;
    private NativeArray<int> _scratchCameFrom;
    private NativeArray<int> _scratchGScore;
    private NativeArray<byte> _scratchClosed;
    private NativeArray<byte> _scratchInOpen;
    private NativeArray<int> _scratchEpoch;
    private NativeArray<int> _scratchOpen;
    private NativeArray<int> _scratchPath;
    private int _scratchSearchEpoch;
    private NativeArray<int> _reservedGoalEpochs;
    private NativeArray<int> _coarseCameFrom;
    private NativeArray<int> _coarseGScore;
    private NativeArray<int> _coarseEpoch;
    private NativeArray<int> _coarseClosedEpoch;
    private NativeArray<int> _coarseOpenEpoch;
    private NativeArray<int> _coarseOpen;
    private int _coarseWidth;
    private int _coarseHeight;
    private int _coarseSearchEpoch;
    private int _lastHierarchicalPathValidationFrame;
    private NativeList<Entity> _requestEntities;
    private NativeList<UnitGrid> _requestUnitGrids;
    private NativeList<UnitPathRequest> _requestGoals;
    private NativeList<UnitFootprint> _requestFootprints;
    private NativeList<UnitMovementBehavior> _requestMovementBehaviors;
    private NativeList<byte> _requestFactions;
    private NativeList<byte> _requestManualMoves;
    private NativeList<Entity> _requestIgnoredOccupancyEntities;
    private NativeList<int2> _requestIgnoredOccupancyCells;
    private NativeList<int2> _requestIgnoredOccupancySizes;
    private NativeList<int2> _assignedGoals;
    private NativeList<byte> _requestStatus;
    private NativeList<byte> _requestSegmented;
    private NativeList<byte> _requestContinuationMoves;
    private NativeList<byte> _requestCheapSegmentModes;
    private NativeList<byte> _requestAlternateSearchSkipped;
    private NativeList<int> _requestAlternateAttempts;
    private int _adaptiveRequestsPerFrame;
    private JobHandle _pendingPathHandle;
    private NativeStream _pendingPathStream;
    private bool _hasPendingPathJob;
    private NativeArray<Entity> _pendingLiveUnitEntities;
    private NativeArray<UnitGrid> _pendingLiveUnitGrids;
    private NativeArray<UnitFootprint> _pendingLiveUnitFootprints;
    private NativeArray<byte> _pendingLiveUnitManualGroupMembers;
    private int _pendingRequestCount;
    private int _pendingRequestBudget;
    private int _pendingLiveUnitCount;
    private int _pendingGridWidth;
    private int _pendingGridHeight;
    private int _pendingScheduleFrame;
    private double _pendingScheduleTime;
    private int _stableOneFrameBatchCount;
    private bool _pendingBudgetReduced;
    private bool _validationLogActive;
    private int _validationStartFrame;
    private int _validationPeakManualQueued;
    private int _validationPeakManualFollowing;
    private int _validationPeakLongMove;
    private int _validationPeakCooldown;
    private int _validationPeakScheduledBudget;
    private int _validationPeakNextBudget;
    private int _validationPeakPendingFrames;
    private double _validationPeakPendingWallMs;
    private int _validationPeakScheduledManual;
    private int _validationPeakScheduledVehicleLike;
    private int _validationPeakScheduledSegmented;
    private int _validationPeakScheduledContinuations;
    private int _validationPeakCheapSegments;
    private int _validationPeakAltReduced;
    private int _validationPeakAltAttempts;
    private int _validationCompletedTotal;
    private int _validationCompletedSegmentTotal;
    private int _validationManualCompletedTotal;
    private int _validationRetriedTotal;
    private int _validationRetriedSegmentTotal;
    private int _validationManualRetriedTotal;
    private int _validationAbandonedTotal;
    private int _nextValidationStuckLogFrame;

    public void OnCreate(ref SystemState state)
    {
        HasPendingPathJob = false;
        _scratchSearchEpoch = 1;
        _adaptiveRequestsPerFrame = 1;
        _stableOneFrameBatchCount = 0;
        _validationLogActive = false;
        _validationStartFrame = 0;
        _nextValidationStuckLogFrame = 0;

        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<DynamicBlockerData>();
        state.RequireForUpdate<DynamicOccupancyData>();
        state.RequireForUpdate<GridRoad>();
        state.RequireForUpdate<GridRoadSidewalk>();
        state.RequireForUpdate<GridRoadDirt>();
        state.RequireForUpdate<RuntimeGameplayStateComponent>();

        _requestQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitPathRequest>(),
                ComponentType.ReadOnly<UnitFootprint>(),
                ComponentType.ReadOnly<UnitMovementBehavior>(),
            }
        });
        _liveUnitsQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<UnitFootprint>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<StaticGridBlocker>(),
                ComponentType.ReadOnly<RuntimeBuildingCombatTag>(),
            }
        });
        _pendingManualMoveQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitTarget>(),
                ComponentType.ReadOnly<ManualMoveOrderTag>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<EngageTarget>(),
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        _pathFollowQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitPathFollow>(),
                ComponentType.ReadOnly<UnitPathRange>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        _longDistanceMoveQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitLongDistanceMove>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        _retryCooldownQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitPathRetryCooldown>(),
            }
        });
        _manualRequestQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitPathRequest>(),
                ComponentType.ReadOnly<ManualMoveOrderTag>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });
        _manualPathFollowQuery = state.GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitPathFollow>(),
                ComponentType.ReadOnly<UnitPathRange>(),
                ComponentType.ReadOnly<ManualMoveOrderTag>(),
            },
            None = new[]
            {
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<StaticGridBlocker>(),
            }
        });

        _requestEntities = new NativeList<Entity>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestUnitGrids = new NativeList<UnitGrid>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestGoals = new NativeList<UnitPathRequest>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestFootprints = new NativeList<UnitFootprint>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestMovementBehaviors = new NativeList<UnitMovementBehavior>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestFactions = new NativeList<byte>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestManualMoves = new NativeList<byte>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestIgnoredOccupancyEntities = new NativeList<Entity>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestIgnoredOccupancyCells = new NativeList<int2>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestIgnoredOccupancySizes = new NativeList<int2>(MaxRequestsPerFrame, Allocator.Persistent);
        _assignedGoals = new NativeList<int2>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestStatus = new NativeList<byte>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestSegmented = new NativeList<byte>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestContinuationMoves = new NativeList<byte>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestCheapSegmentModes = new NativeList<byte>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestAlternateSearchSkipped = new NativeList<byte>(MaxRequestsPerFrame, Allocator.Persistent);
        _requestAlternateAttempts = new NativeList<int>(MaxRequestsPerFrame, Allocator.Persistent);
    }

    public void OnDestroy(ref SystemState state)
    {
        DisposePendingPathJob(ref state);
        HasPendingPathJob = false;
        DisposeScratch();
        DisposeReservedGoals();
        DisposeCoarseScratch();
        if (_requestEntities.IsCreated) _requestEntities.Dispose();
        if (_requestUnitGrids.IsCreated) _requestUnitGrids.Dispose();
        if (_requestGoals.IsCreated) _requestGoals.Dispose();
        if (_requestFootprints.IsCreated) _requestFootprints.Dispose();
        if (_requestMovementBehaviors.IsCreated) _requestMovementBehaviors.Dispose();
        if (_requestFactions.IsCreated) _requestFactions.Dispose();
        if (_requestManualMoves.IsCreated) _requestManualMoves.Dispose();
        if (_requestIgnoredOccupancyEntities.IsCreated) _requestIgnoredOccupancyEntities.Dispose();
        if (_requestIgnoredOccupancyCells.IsCreated) _requestIgnoredOccupancyCells.Dispose();
        if (_requestIgnoredOccupancySizes.IsCreated) _requestIgnoredOccupancySizes.Dispose();
        if (_assignedGoals.IsCreated) _assignedGoals.Dispose();
        if (_requestStatus.IsCreated) _requestStatus.Dispose();
        if (_requestSegmented.IsCreated) _requestSegmented.Dispose();
        if (_requestContinuationMoves.IsCreated) _requestContinuationMoves.Dispose();
        if (_requestCheapSegmentModes.IsCreated) _requestCheapSegmentModes.Dispose();
        if (_requestAlternateSearchSkipped.IsCreated) _requestAlternateSearchSkipped.Dispose();
        if (_requestAlternateAttempts.IsCreated) _requestAlternateAttempts.Dispose();
    }

    private void AddIgnoredOccupancyForRequest(ref SystemState state, Entity entity)
    {
        Entity ignoredEntity = Entity.Null;
        int2 ignoredCell = default;
        int2 ignoredSize = default;

        EntityManager em = state.EntityManager;
        if (em.HasComponent<UnitTransportBoardingTarget>(entity))
        {
            Entity transport = em.GetComponentData<UnitTransportBoardingTarget>(entity).Transport;
            if (transport != Entity.Null &&
                em.Exists(transport) &&
                em.HasComponent<UnitGrid>(transport) &&
                em.HasComponent<UnitFootprint>(transport))
            {
                ignoredEntity = transport;
                ignoredCell = em.GetComponentData<UnitGrid>(transport).Cell;
                ignoredSize = em.GetComponentData<UnitFootprint>(transport).Size;
            }
        }

        _requestIgnoredOccupancyEntities.Add(ignoredEntity);
        _requestIgnoredOccupancyCells.Add(ignoredCell);
        _requestIgnoredOccupancySizes.Add(ignoredSize);
    }

    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.GetSingleton<RuntimeGameplayStateComponent>().PlayRequested == 0)
        {
            if (_hasPendingPathJob)
                DisposePendingPathJob(ref state);
            return;
        }

        if (_hasPendingPathJob)
        {
            if (!_pendingPathHandle.IsCompleted)
            {
                if (!_pendingBudgetReduced && Time.frameCount > _pendingScheduleFrame)
                {
                    _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, _pendingRequestBudget / 2);
                    _stableOneFrameBatchCount = 0;
                    _pendingBudgetReduced = true;
                }
                state.Dependency = _pendingPathHandle;
                return;
            }

            ApplyPendingPathJob(ref state);
        }

        double startTime = Time.realtimeSinceStartupAsDouble;
        int requestCountForLog = 0;
        int liveUnitCountForLog = 0;
        int gridWidthForLog = 0;
        int gridHeightForLog = 0;
        double afterGridTime = startTime;
        double afterScratchTime = startTime;
        double afterSnapshotTime = startTime;
        double afterRequestCollectTime = startTime;
        double afterGoalAssignTime = startTime;
        double afterScheduleTime = startTime;
        double afterCompleteTime = startTime;
        double afterApplyTime = startTime;
        bool scratchWasAllocated = false;
        int scratchCellsForLog = 0;
        int scratchThreadSlotsForLog = 0;
        int successCountForLog = 0;
        int failedCountForLog = 0;
        int segmentedCountForLog = 0;
        int requestBudgetForLog = math.clamp(_adaptiveRequestsPerFrame, MinRequestsPerFrame, MaxRequestsPerFrame);
        try
        {
            var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
            var grid = SystemAPI.GetSingleton<GridConfig>();
            gridWidthForLog = grid.Width;
            gridHeightForLog = grid.Height;
            int gridSize = grid.Width * grid.Height;
            afterGridTime = Time.realtimeSinceStartupAsDouble;
            scratchWasAllocated = EnsureScratch(gridSize, out scratchCellsForLog, out scratchThreadSlotsForLog);
            afterScratchTime = Time.realtimeSinceStartupAsDouble;

            if (_requestQuery.IsEmptyIgnoreFilter)
                return;

            var walkable = SystemAPI.GetBuffer<GridWalkable>(gridEntity);
            var roads = SystemAPI.GetBuffer<GridRoad>(gridEntity);
            var sidewalks = SystemAPI.GetBuffer<GridRoadSidewalk>(gridEntity);
            var dirtRoads = SystemAPI.GetBuffer<GridRoadDirt>(gridEntity);
            var dynamicBlockers = SystemAPI.GetComponent<DynamicBlockerData>(gridEntity).Blocked;
            var friendlyPassFactionIds = SystemAPI.GetComponent<DynamicBlockerData>(gridEntity).FriendlyPassFactionIds;
            var occupied = SystemAPI.GetComponent<DynamicOccupancyData>(gridEntity).Occupied;
            _pendingLiveUnitEntities = _liveUnitsQuery.ToEntityArray(Allocator.Persistent);
            _pendingLiveUnitGrids = _liveUnitsQuery.ToComponentDataArray<UnitGrid>(Allocator.Persistent);
            _pendingLiveUnitFootprints = _liveUnitsQuery.ToComponentDataArray<UnitFootprint>(Allocator.Persistent);
            _pendingLiveUnitManualGroupMembers = new NativeArray<byte>(_pendingLiveUnitEntities.Length, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < _pendingLiveUnitEntities.Length; i++)
                _pendingLiveUnitManualGroupMembers[i] = (byte)(state.EntityManager.HasComponent<ManualMoveGroupMemberTag>(_pendingLiveUnitEntities[i]) ? 1 : 0);
            var liveUnitEntities = _pendingLiveUnitEntities;
            var liveUnitGrids = _pendingLiveUnitGrids;
            var liveUnitFootprints = _pendingLiveUnitFootprints;
            var liveUnitManualGroupMembers = _pendingLiveUnitManualGroupMembers;
            liveUnitCountForLog = liveUnitEntities.Length;
            afterSnapshotTime = Time.realtimeSinceStartupAsDouble;

            _requestEntities.Clear();
            _requestUnitGrids.Clear();
            _requestGoals.Clear();
            _requestFootprints.Clear();
            _requestMovementBehaviors.Clear();
            _requestFactions.Clear();
            _requestManualMoves.Clear();
            _requestIgnoredOccupancyEntities.Clear();
            _requestIgnoredOccupancyCells.Clear();
            _requestIgnoredOccupancySizes.Clear();
            _requestContinuationMoves.Clear();

            foreach (var (unitGrid, request, footprint, movementBehavior, faction, _, entity) in SystemAPI
                         .Query<RefRO<UnitGrid>, RefRO<UnitPathRequest>, RefRO<UnitFootprint>, RefRO<UnitMovementBehavior>, RefRO<Faction>, RefRO<ManualMoveOrderTag>>()
                         .WithNone<UnitAirMovement>()
                         .WithEntityAccess())
            {
                _requestEntities.Add(entity);
                _requestUnitGrids.Add(unitGrid.ValueRO);
                _requestGoals.Add(request.ValueRO);
                _requestFootprints.Add(footprint.ValueRO);
                _requestMovementBehaviors.Add(movementBehavior.ValueRO);
                _requestFactions.Add(faction.ValueRO.Id);
                _requestManualMoves.Add(1);
                AddIgnoredOccupancyForRequest(ref state, entity);
                _requestContinuationMoves.Add((byte)(SystemAPI.HasComponent<UnitLongDistanceMove>(entity) ? 1 : 0));

                if (_requestEntities.Length >= requestBudgetForLog)
                    break;
            }

            foreach (var (unitGrid, request, footprint, movementBehavior, faction, entity) in SystemAPI
                         .Query<RefRO<UnitGrid>, RefRO<UnitPathRequest>, RefRO<UnitFootprint>, RefRO<UnitMovementBehavior>, RefRO<Faction>>()
                         .WithNone<UnitAirMovement>()
                         .WithEntityAccess())
            {
                if (SystemAPI.HasComponent<ManualMoveOrderTag>(entity))
                    continue;

                _requestEntities.Add(entity);
                _requestUnitGrids.Add(unitGrid.ValueRO);
                _requestGoals.Add(request.ValueRO);
                _requestFootprints.Add(footprint.ValueRO);
                _requestMovementBehaviors.Add(movementBehavior.ValueRO);
                _requestFactions.Add(faction.ValueRO.Id);
                _requestManualMoves.Add(0);
                AddIgnoredOccupancyForRequest(ref state, entity);
                _requestContinuationMoves.Add((byte)(SystemAPI.HasComponent<UnitLongDistanceMove>(entity) ? 1 : 0));

                if (_requestEntities.Length >= requestBudgetForLog)
                    break;
            }

            int requestCount = _requestEntities.Length;
            requestCountForLog = requestCount;
            afterRequestCollectTime = Time.realtimeSinceStartupAsDouble;
            if (requestCount == 0)
            {
                DisposePendingLiveSnapshots();
                return;
            }

            PrepareReservedGoals(gridSize);

            _assignedGoals.ResizeUninitialized(requestCount);
            _requestStatus.ResizeUninitialized(requestCount);
            _requestSegmented.ResizeUninitialized(requestCount);
            for (int i = 0; i < requestCount; i++)
            {
                _requestStatus[i] = 0;
                _requestSegmented[i] = 0;
            }

            var requestEntities = _requestEntities.AsArray();
            var requestUnitGrids = _requestUnitGrids.AsArray();
            var requestGoals = _requestGoals.AsArray();
            var requestFootprints = _requestFootprints.AsArray();
            var requestMovementBehaviors = _requestMovementBehaviors.AsArray();
            var requestFactions = _requestFactions.AsArray();
            var requestManualMoves = _requestManualMoves.AsArray();
            var requestIgnoredOccupancyEntities = _requestIgnoredOccupancyEntities.AsArray();
            var requestIgnoredOccupancyCells = _requestIgnoredOccupancyCells.AsArray();
            var requestIgnoredOccupancySizes = _requestIgnoredOccupancySizes.AsArray();
            var assignedGoals = _assignedGoals.AsArray();
            var status = _requestStatus.AsArray();
            var segmented = _requestSegmented.AsArray();
            _requestCheapSegmentModes.ResizeUninitialized(requestCount);
            _requestAlternateSearchSkipped.ResizeUninitialized(requestCount);
            _requestAlternateAttempts.ResizeUninitialized(requestCount);
            var cheapSegmentModes = _requestCheapSegmentModes.AsArray();
            var alternateSearchSkipped = _requestAlternateSearchSkipped.AsArray();
            var alternateAttempts = _requestAlternateAttempts.AsArray();
            var reservedGoalEpochs = _reservedGoalEpochs;
            int reservedGoalGeneration = _reservedGoalGeneration;
            int scratchSearchEpochBase = ReserveScratchEpochs(requestCount);
            int manualRequestCount = 0;
            int hierarchicalEligibleCount = 0;
            int hierarchicalWaypointCount = 0;
            int hierarchicalFallbackCount = 0;
            for (int i = 0; i < requestCount; i++)
            {
                int2 start = requestUnitGrids[i].Cell;
                int2 footprintSize = requestFootprints[i].Size;
                int startIndex = GridUtils.InBounds(start, grid.Width, grid.Height) ? GridUtils.CellToIndex(start, grid.Width) : -1;
                int2 requestedGoal = requestGoals[i].Goal;
                bool isVehicle = UnitVehicleMovementUtility.IsVehicle(requestFootprints[i], requestMovementBehaviors[i]);
                bool isManualMove = requestManualMoves[i] != 0;
                if (isManualMove)
                {
                    manualRequestCount++;
                    float2 delta = new float2(requestedGoal.x - start.x, requestedGoal.y - start.y);
                    if (math.length(delta) > GetMaxSegmentCells(true, isVehicle))
                        hierarchicalEligibleCount++;
                }
                int2 pathGoal = GetSegmentGoalHierarchical(
                    grid,
                    walkable.AsNativeArray(),
                    dynamicBlockers,
                    friendlyPassFactionIds,
                    start,
                    requestedGoal,
                    isManualMove,
                    isVehicle,
                    requestFactions[i],
                    out bool usedHierarchicalWaypoint,
                    out bool hierarchicalFallback);
                if (usedHierarchicalWaypoint)
                    hierarchicalWaypointCount++;
                if (hierarchicalFallback)
                    hierarchicalFallbackCount++;
                bool isSegmentedRequest = !pathGoal.Equals(requestedGoal);
                segmented[i] = (byte)(isSegmentedRequest ? 1 : 0);
                bool cheapSegmentMode = isManualMove && !isVehicle;
                cheapSegmentModes[i] = (byte)(cheapSegmentMode ? 1 : 0);
                bool skipAlternateSearch = isManualMove && !isVehicle;
                alternateSearchSkipped[i] = (byte)(skipAlternateSearch ? 1 : 0);
                alternateAttempts[i] = 0;
                int2 assignedGoal = FindNearestFreeGoal(
                    grid,
                    walkable.AsNativeArray(),
                    dynamicBlockers,
                    friendlyPassFactionIds,
                    occupied,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    reservedGoalEpochs,
                    reservedGoalGeneration,
                    requestEntities[i],
                    requestIgnoredOccupancyEntities[i],
                    requestIgnoredOccupancyCells[i],
                    requestIgnoredOccupancySizes[i],
                    pathGoal,
                    start,
                    footprintSize,
                    requestFactions[i],
                    startIndex);
                if (assignedGoal.Equals(start) && !pathGoal.Equals(start))
                    assignedGoal = pathGoal;
                assignedGoals[i] = assignedGoal;
            }
            if (EnableHierarchicalPathValidationLog &&
                manualRequestCount > 0 &&
                Time.frameCount - _lastHierarchicalPathValidationFrame >= 60)
            {
                _lastHierarchicalPathValidationFrame = Time.frameCount;
                Debug.Log($"[HierPathValidate] frame={Time.frameCount} requests={requestCount} manual={manualRequestCount} eligible={hierarchicalEligibleCount} hierarchical={hierarchicalWaypointCount} fallback={hierarchicalFallbackCount} sector={HierarchicalSectorSizeCells} infantryThreshold={ManualInfantryLongDistanceSegmentCells} vehicleThreshold={ManualVehicleLongDistanceSegmentCells} maxExpanded={HierarchicalMaxExpandedSectors}");
            }
            afterGoalAssignTime = Time.realtimeSinceStartupAsDouble;

            _pendingPathStream = new NativeStream(requestCount, Allocator.Persistent);

            var job = new PathfindBatchJob
            {
                Grid = grid,
                Walkable = walkable.AsNativeArray(),
                Roads = roads.AsNativeArray(),
                Sidewalks = sidewalks.AsNativeArray(),
                DirtRoads = dirtRoads.AsNativeArray(),
                DynamicBlocked = dynamicBlockers,
                FriendlyPassFactionIds = friendlyPassFactionIds,
                Occupied = occupied,
                LiveUnitEntities = liveUnitEntities,
                LiveUnitGrids = liveUnitGrids,
                LiveUnitFootprints = liveUnitFootprints,
                LiveUnitManualGroupMembers = liveUnitManualGroupMembers,
                Entities = requestEntities,
                UnitGrids = requestUnitGrids,
                Footprints = requestFootprints,
                MovementBehaviors = requestMovementBehaviors,
                Factions = requestFactions,
                ManualMoves = requestManualMoves,
                IgnoredOccupancyEntities = requestIgnoredOccupancyEntities,
                IgnoredOccupancyCells = requestIgnoredOccupancyCells,
                IgnoredOccupancySizes = requestIgnoredOccupancySizes,
                RequestedGoals = requestGoals,
                Goals = assignedGoals,
                Segmented = segmented,
                Output = _pendingPathStream.AsWriter(),
                Status = status,
                CheapSegmentModes = cheapSegmentModes,
                AlternateSearchSkipped = alternateSearchSkipped,
                AlternateAttempts = alternateAttempts,
                GridSize = gridSize,
                ScratchCameFrom = _scratchCameFrom,
                ScratchGScore = _scratchGScore,
                ScratchClosed = _scratchClosed,
                ScratchInOpen = _scratchInOpen,
                ScratchEpoch = _scratchEpoch,
                ScratchOpen = _scratchOpen,
                ScratchPath = _scratchPath,
                SearchEpochBase = scratchSearchEpochBase,
            };

            _pendingPathHandle = job.Schedule(requestCount, state.Dependency);
            afterScheduleTime = Time.realtimeSinceStartupAsDouble;
            afterCompleteTime = afterScheduleTime;
            afterApplyTime = afterScheduleTime;
            state.Dependency = _pendingPathHandle;
            _hasPendingPathJob = true;
            HasPendingPathJob = true;
            _pendingRequestCount = requestCount;
            _pendingRequestBudget = requestBudgetForLog;
            _pendingLiveUnitCount = liveUnitCountForLog;
            _pendingGridWidth = gridWidthForLog;
            _pendingGridHeight = gridHeightForLog;
            _pendingScheduleFrame = Time.frameCount;
            _pendingScheduleTime = afterScheduleTime;
            _pendingBudgetReduced = false;
        }
        finally
        {
            double elapsed = Time.realtimeSinceStartupAsDouble - startTime;

            if (EnablePathFreezeLogs && elapsed >= FreezeLogThresholdSeconds)
            {
                if (afterSnapshotTime < afterScratchTime)
                    afterSnapshotTime = afterScratchTime;
                if (afterRequestCollectTime < afterSnapshotTime)
                    afterRequestCollectTime = afterSnapshotTime;
                if (afterGoalAssignTime < afterRequestCollectTime)
                    afterGoalAssignTime = afterRequestCollectTime;
                if (afterScheduleTime < afterGoalAssignTime)
                    afterScheduleTime = afterGoalAssignTime;
                if (afterCompleteTime < afterScheduleTime)
                    afterCompleteTime = afterScheduleTime;
                if (afterApplyTime < afterCompleteTime)
                    afterApplyTime = afterCompleteTime;

                Debug.Log(
                    $"[FreezeDetect:ECS] UnitPathfindingSystem frame={Time.frameCount} {(elapsed * 1000d):F1}ms " +
                    $"requests={requestCountForLog} liveUnits={liveUnitCountForLog} grid={gridWidthForLog}x{gridHeightForLog}");
                Debug.Log(
                    $"[PathDiag] frame={Time.frameCount} total={(elapsed * 1000d):F1}ms " +
                    $"grid={(afterGridTime - startTime) * 1000d:F1}ms " +
                    $"scratch={(afterScratchTime - afterGridTime) * 1000d:F1}ms allocated={(scratchWasAllocated ? 1 : 0)} scratchCells={scratchCellsForLog} scratchThreads={scratchThreadSlotsForLog} " +
                    $"snapshot={(afterSnapshotTime - afterScratchTime) * 1000d:F1}ms " +
                    $"collect={(afterRequestCollectTime - afterSnapshotTime) * 1000d:F1}ms " +
                    $"goal={(afterGoalAssignTime - afterRequestCollectTime) * 1000d:F1}ms " +
                    $"schedule={(afterScheduleTime - afterGoalAssignTime) * 1000d:F1}ms " +
                    $"wait={(afterCompleteTime - afterScheduleTime) * 1000d:F1}ms " +
                    $"apply={(afterApplyTime - afterCompleteTime) * 1000d:F1}ms " +
                    $"requests={requestCountForLog} budget={requestBudgetForLog} nextBudget={_adaptiveRequestsPerFrame} success={successCountForLog} failed={failedCountForLog} segmented={segmentedCountForLog} liveUnits={liveUnitCountForLog}");
            }
        }
    }

    private void ApplyPendingPathJob(ref SystemState state)
    {
        double applyStart = Time.realtimeSinceStartupAsDouble;
        _pendingPathHandle.Complete();
        double afterComplete = Time.realtimeSinceStartupAsDouble;
        double pendingWallTime = afterComplete - _pendingScheduleTime;
        int pendingFrames = math.max(1, Time.frameCount - _pendingScheduleFrame);
        var manualMoves = _requestManualMoves.AsArray();
        int scheduledManualCountForBudget = 0;
        int scheduledVehicleLikeCountForBudget = 0;
        for (int i = 0; i < _pendingRequestCount; i++)
        {
            if (manualMoves[i] != 0)
                scheduledManualCountForBudget++;
            if (UnitVehicleMovementUtility.IsVehicle(_requestFootprints[i], _requestMovementBehaviors[i]))
                scheduledVehicleLikeCountForBudget++;
        }
        UpdateAdaptiveBudget(pendingFrames, pendingWallTime, _pendingRequestCount, scheduledManualCountForBudget, scheduledVehicleLikeCountForBudget);

        var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        var pool = state.EntityManager.GetComponentData<PathPoolData>(gridEntity);
        var requestEntities = _requestEntities.AsArray();
        var requestGoals = _requestGoals.AsArray();
        var assignedGoals = _assignedGoals.AsArray();
        var status = _requestStatus.AsArray();
        var segmented = _requestSegmented.AsArray();
        var requestContinuationMoves = _requestContinuationMoves.AsArray();
        var cheapSegmentModes = _requestCheapSegmentModes.AsArray();
        var alternateSearchSkipped = _requestAlternateSearchSkipped.AsArray();
        var alternateAttempts = _requestAlternateAttempts.AsArray();

        ApplyResults(
            ref state,
            gridEntity,
            ref pool,
            requestEntities,
            requestGoals,
            assignedGoals,
            segmented,
            manualMoves,
            _pendingPathStream,
            status,
            out int completedCount,
            out int completedSegmentCount,
            out int manualCompletedCount,
            out int retriedCount,
            out int retriedSegmentCount,
            out int manualRetriedCount,
            out int abandonedCount);
        state.EntityManager.SetComponentData(gridEntity, pool);

        int queuedCount = _requestQuery.CalculateEntityCount();
        int followingCount = _pathFollowQuery.CalculateEntityCount();
        int manualPendingCount = _pendingManualMoveQuery.CalculateEntityCount();
        int manualQueuedCount = _manualRequestQuery.CalculateEntityCount();
        int manualFollowingCount = _manualPathFollowQuery.CalculateEntityCount();
        int longDistanceCount = _longDistanceMoveQuery.CalculateEntityCount();
        int retryCooldownCount = _retryCooldownQuery.CalculateEntityCount();
        int stillPendingCount = queuedCount + followingCount;
        int segmentedCount = 0;
        int scheduledManualCount = 0;
        int scheduledVehicleLikeCount = 0;
        int scheduledSegmentedCount = 0;
        int scheduledContinuationCount = 0;
        int cheapSegmentCount = 0;
        int alternateReducedCount = 0;
        int alternateAttemptTotal = 0;
        for (int i = 0; i < _pendingRequestCount; i++)
        {
            if (UnitVehicleMovementUtility.IsVehicle(_requestFootprints[i], _requestMovementBehaviors[i]))
                scheduledVehicleLikeCount++;
            if (segmented[i] != 0)
            {
                segmentedCount++;
                scheduledSegmentedCount++;
            }
            if (manualMoves[i] != 0)
                scheduledManualCount++;
            if (requestContinuationMoves[i] != 0)
                scheduledContinuationCount++;
            if (cheapSegmentModes[i] != 0)
                cheapSegmentCount++;
            if (alternateSearchSkipped[i] != 0)
                alternateReducedCount++;
            alternateAttemptTotal += alternateAttempts[i];
        }

        double afterApply = Time.realtimeSinceStartupAsDouble;
        double applyElapsed = afterApply - applyStart;
        bool manualValidationActive =
            EnablePathDiagnostics &&
            (manualPendingCount > 0 ||
             manualQueuedCount > 0 ||
             manualFollowingCount > 0 ||
             longDistanceCount > 0 ||
             retryCooldownCount > 0);

        if (manualValidationActive && !_validationLogActive)
        {
            _validationLogActive = true;
            _validationStartFrame = Time.frameCount;
            _validationPeakManualQueued = manualQueuedCount;
            _validationPeakManualFollowing = manualFollowingCount;
            _validationPeakLongMove = longDistanceCount;
            _validationPeakCooldown = retryCooldownCount;
            _validationPeakScheduledBudget = _pendingRequestBudget;
            _validationPeakNextBudget = _adaptiveRequestsPerFrame;
            _validationPeakPendingFrames = pendingFrames;
            _validationPeakPendingWallMs = pendingWallTime * 1000d;
            _validationPeakScheduledManual = scheduledManualCount;
            _validationPeakScheduledVehicleLike = scheduledVehicleLikeCount;
            _validationPeakScheduledSegmented = scheduledSegmentedCount;
            _validationPeakScheduledContinuations = scheduledContinuationCount;
            _validationPeakCheapSegments = cheapSegmentCount;
            _validationPeakAltReduced = alternateReducedCount;
            _validationPeakAltAttempts = alternateAttemptTotal;
            _validationCompletedTotal = 0;
            _validationCompletedSegmentTotal = 0;
            _validationManualCompletedTotal = 0;
            _validationRetriedTotal = 0;
            _validationRetriedSegmentTotal = 0;
            _validationManualRetriedTotal = 0;
            _validationAbandonedTotal = 0;
            _nextValidationStuckLogFrame = Time.frameCount + ValidationStuckLogFirstDelayFrames;
            Debug.Log(
                $"[PathDiagValidate] START frame={Time.frameCount} manualPending={manualPendingCount} manualQueued={manualQueuedCount} manualFollowing={manualFollowingCount} manualIdle={math.max(0, manualPendingCount - manualQueuedCount - manualFollowingCount)} cooldown={retryCooldownCount} longMove={longDistanceCount} scheduledBudget={_pendingRequestBudget} nextBudget={_adaptiveRequestsPerFrame}");
        }

        if (manualValidationActive)
        {
            _validationPeakManualQueued = math.max(_validationPeakManualQueued, manualQueuedCount);
            _validationPeakManualFollowing = math.max(_validationPeakManualFollowing, manualFollowingCount);
            _validationPeakLongMove = math.max(_validationPeakLongMove, longDistanceCount);
            _validationPeakCooldown = math.max(_validationPeakCooldown, retryCooldownCount);
            _validationPeakScheduledBudget = math.max(_validationPeakScheduledBudget, _pendingRequestBudget);
            _validationPeakNextBudget = math.max(_validationPeakNextBudget, _adaptiveRequestsPerFrame);
            _validationPeakPendingFrames = math.max(_validationPeakPendingFrames, pendingFrames);
            _validationPeakPendingWallMs = math.max(_validationPeakPendingWallMs, pendingWallTime * 1000d);
            _validationPeakScheduledManual = math.max(_validationPeakScheduledManual, scheduledManualCount);
            _validationPeakScheduledVehicleLike = math.max(_validationPeakScheduledVehicleLike, scheduledVehicleLikeCount);
            _validationPeakScheduledSegmented = math.max(_validationPeakScheduledSegmented, scheduledSegmentedCount);
            _validationPeakScheduledContinuations = math.max(_validationPeakScheduledContinuations, scheduledContinuationCount);
            _validationPeakCheapSegments = math.max(_validationPeakCheapSegments, cheapSegmentCount);
            _validationPeakAltReduced = math.max(_validationPeakAltReduced, alternateReducedCount);
            _validationPeakAltAttempts = math.max(_validationPeakAltAttempts, alternateAttemptTotal);
            _validationCompletedTotal += completedCount;
            _validationCompletedSegmentTotal += completedSegmentCount;
            _validationManualCompletedTotal += manualCompletedCount;
            _validationRetriedTotal += retriedCount;
            _validationRetriedSegmentTotal += retriedSegmentCount;
            _validationManualRetriedTotal += manualRetriedCount;
            _validationAbandonedTotal += abandonedCount;

            if (_validationLogActive && Time.frameCount >= _nextValidationStuckLogFrame)
            {
                LogValidationStuck(
                    ref state,
                    manualPendingCount,
                    manualQueuedCount,
                    manualFollowingCount,
                    longDistanceCount,
                    retryCooldownCount,
                    queuedCount,
                    followingCount,
                    completedCount,
                    manualCompletedCount,
                    retriedCount,
                    manualRetriedCount,
                    abandonedCount);
                _nextValidationStuckLogFrame = Time.frameCount + ValidationStuckLogIntervalFrames;
            }
        }
        else if (_validationLogActive)
        {
            Debug.Log(
                $"[PathDiagValidate] END startFrame={_validationStartFrame} endFrame={Time.frameCount} peakManualQueued={_validationPeakManualQueued} peakManualFollowing={_validationPeakManualFollowing} peakLongMove={_validationPeakLongMove} peakCooldown={_validationPeakCooldown} peakScheduledBudget={_validationPeakScheduledBudget} peakNextBudget={_validationPeakNextBudget} peakPendingFrames={_validationPeakPendingFrames} peakPendingWallMs={_validationPeakPendingWallMs:F1} peakScheduledManual={_validationPeakScheduledManual} peakScheduledVehicleLike={_validationPeakScheduledVehicleLike} peakScheduledSegmented={_validationPeakScheduledSegmented} peakScheduledContinuations={_validationPeakScheduledContinuations} peakCheapSegments={_validationPeakCheapSegments} peakAltReduced={_validationPeakAltReduced} peakAltAttempts={_validationPeakAltAttempts} totalCompleted={_validationCompletedTotal} totalCompletedSegmented={_validationCompletedSegmentTotal} totalManualCompleted={_validationManualCompletedTotal} totalRetried={_validationRetriedTotal} totalRetriedSegmented={_validationRetriedSegmentTotal} totalManualRetried={_validationManualRetriedTotal} totalAbandoned={_validationAbandonedTotal}");
            _validationLogActive = false;
            _nextValidationStuckLogFrame = 0;
        }
        if (EnablePathDiagnostics && (applyElapsed >= FreezeLogThresholdSeconds || pendingWallTime >= FreezeLogThresholdSeconds))
        {
            Debug.Log(
                $"[PathDiagAsync] frame={Time.frameCount} applyTotal={(applyElapsed * 1000d):F1}ms " +
                $"pendingWall={(pendingWallTime * 1000d):F1}ms complete={(afterComplete - applyStart) * 1000d:F1}ms apply={(afterApply - afterComplete) * 1000d:F1}ms " +
                $"requests={_pendingRequestCount} budget={_pendingRequestBudget} nextBudget={_adaptiveRequestsPerFrame} " +
                $"success={completedCount} failed={retriedCount + abandonedCount} segmented={segmentedCount} liveUnits={_pendingLiveUnitCount} " +
                $"grid={_pendingGridWidth}x{_pendingGridHeight}");
        }

        DisposeCompletedPendingPathJob();
    }

    private void LogValidationStuck(
        ref SystemState state,
        int manualPendingCount,
        int manualQueuedCount,
        int manualFollowingCount,
        int longDistanceCount,
        int retryCooldownCount,
        int queuedCount,
        int followingCount,
        int completedCount,
        int manualCompletedCount,
        int retriedCount,
        int manualRetriedCount,
        int abandonedCount)
    {
        int manualIdleCount = math.max(0, manualPendingCount - manualQueuedCount - manualFollowingCount);
        string samples = BuildManualMoveSamples(ref state, ValidationStuckSampleCount);
        Debug.Log(
            $"[PathDiagStuck] frame={Time.frameCount} ageFrames={Time.frameCount - _validationStartFrame} " +
            $"manualPending={manualPendingCount} manualQueued={manualQueuedCount} manualFollowing={manualFollowingCount} manualIdle={manualIdleCount} " +
            $"cooldown={retryCooldownCount} longMove={longDistanceCount} queued={queuedCount} following={followingCount} " +
            $"pendingJob={(_hasPendingPathJob ? 1 : 0)} pendingRequests={_pendingRequestCount} scheduledBudget={_pendingRequestBudget} nextBudget={_adaptiveRequestsPerFrame} " +
            $"lastCompleted={completedCount} lastManualCompleted={manualCompletedCount} lastRetried={retriedCount} lastManualRetried={manualRetriedCount} lastAbandoned={abandonedCount} " +
            $"totalManualCompleted={_validationManualCompletedTotal} totalManualRetried={_validationManualRetriedTotal} totalAbandoned={_validationAbandonedTotal} " +
            $"samples={samples}");
    }

    private string BuildManualMoveSamples(ref SystemState state, int maxSamples)
    {
        var em = state.EntityManager;
        using var entities = _pendingManualMoveQuery.ToEntityArray(Allocator.Temp);
        if (entities.Length == 0)
            return "none";

        var builder = new StringBuilder(512);
        int written = 0;
        for (int i = 0; i < entities.Length && written < maxSamples; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            if (written > 0)
                builder.Append(" | ");

            builder.Append(entity);
            AppendSampleComponentState(ref builder, em, entity);
            written++;
        }

        if (entities.Length > written)
            builder.Append($" | more={entities.Length - written}");

        return written == 0 ? "none" : builder.ToString();
    }

    private static void AppendSampleComponentState(ref StringBuilder builder, EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitGrid>(entity))
            builder.Append($" cell={em.GetComponentData<UnitGrid>(entity).Cell}");
        else
            builder.Append(" cell=none");

        if (em.HasComponent<UnitTarget>(entity))
            builder.Append($" target={em.GetComponentData<UnitTarget>(entity).Cell}");
        else
            builder.Append(" target=none");

        if (em.HasComponent<UnitPathRequest>(entity))
            builder.Append($" req={em.GetComponentData<UnitPathRequest>(entity).Goal}");
        else
            builder.Append(" req=none");

        if (em.HasComponent<UnitPathFollow>(entity) && em.HasComponent<UnitPathRange>(entity))
        {
            UnitPathFollow follow = em.GetComponentData<UnitPathFollow>(entity);
            UnitPathRange range = em.GetComponentData<UnitPathRange>(entity);
            builder.Append($" follow={follow.PathIndex}/{range.Length}");
        }
        else
        {
            builder.Append(" follow=none");
        }

        if (em.HasComponent<UnitLongDistanceMove>(entity))
            builder.Append($" long={em.GetComponentData<UnitLongDistanceMove>(entity).FinalGoal}");
        else
            builder.Append(" long=none");

        if (em.HasComponent<UnitPathRetryCooldown>(entity))
            builder.Append($" cooldownUntil={em.GetComponentData<UnitPathRetryCooldown>(entity).ResumeFrame}");
        else
            builder.Append(" cooldown=none");

        builder.Append($" group={(em.HasComponent<ManualMoveGroupMemberTag>(entity) ? 1 : 0)}");

        if (em.HasComponent<UnitFootprint>(entity) && em.HasComponent<UnitMovementBehavior>(entity))
        {
            UnitFootprint footprint = em.GetComponentData<UnitFootprint>(entity);
            UnitMovementBehavior movementBehavior = em.GetComponentData<UnitMovementBehavior>(entity);
            builder.Append($" footprint={footprint.Size} vehicle={(UnitVehicleMovementUtility.IsVehicle(footprint, movementBehavior) ? 1 : 0)}");
        }
    }

    private void DisposePendingPathJob(ref SystemState state)
    {
        if (!_hasPendingPathJob)
            return;

        _pendingPathHandle.Complete();
        state.Dependency = default;
        DisposeCompletedPendingPathJob();
    }

    private void DisposeCompletedPendingPathJob()
    {
        if (_pendingPathStream.IsCreated)
            _pendingPathStream.Dispose();
        DisposePendingLiveSnapshots();
        _hasPendingPathJob = false;
        HasPendingPathJob = false;
        _pendingRequestCount = 0;
        _pendingRequestBudget = 0;
        _pendingLiveUnitCount = 0;
        _pendingGridWidth = 0;
        _pendingGridHeight = 0;
        _pendingScheduleFrame = 0;
        _pendingScheduleTime = 0d;
        _pendingBudgetReduced = false;
    }

    private void UpdateAdaptiveBudget(int pendingFrames, double pendingWallTime, int requestCount, int manualRequestCount, int vehicleLikeCount)
    {
        if (requestCount <= 0)
            return;

        bool allManualInfantry = manualRequestCount == requestCount && vehicleLikeCount == 0;
        if (pendingFrames > 1 || pendingWallTime >= FreezeLogThresholdSeconds)
        {
            _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, _pendingRequestBudget / 2);
            _stableOneFrameBatchCount = 0;
            return;
        }

        if (allManualInfantry)
        {
            if (requestCount >= _pendingRequestBudget)
            {
                _stableOneFrameBatchCount++;
                if (_stableOneFrameBatchCount >= StableManualInfantryBatchesBeforeIncrease)
                {
                    _adaptiveRequestsPerFrame = math.min(MaxManualInfantryRequestsPerFrame, _pendingRequestBudget + 1);
                    _stableOneFrameBatchCount = 0;
                }
                else
                {
                    _adaptiveRequestsPerFrame = math.max(_adaptiveRequestsPerFrame, _pendingRequestBudget);
                }
            }
            else
            {
                _stableOneFrameBatchCount = 0;
                _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, math.min(_adaptiveRequestsPerFrame, _pendingRequestBudget));
            }
            return;
        }

        if (pendingWallTime >= HighPathJobWallSeconds)
        {
            _adaptiveRequestsPerFrame = math.max(MinRequestsPerFrame, _pendingRequestBudget / 2);
            _stableOneFrameBatchCount = 0;
            return;
        }

        int targetBudget = math.clamp(
            (int)math.floor(TargetPathJobWallSeconds / math.max(1e-6d, pendingWallTime / requestCount)),
            MinRequestsPerFrame,
            MaxRequestsPerFrame);

        if (pendingWallTime <= LowPathJobWallSeconds && requestCount >= _pendingRequestBudget)
        {
            _stableOneFrameBatchCount++;
            if (_stableOneFrameBatchCount >= StableOneFrameBatchesBeforeIncrease)
            {
                _adaptiveRequestsPerFrame = math.min(targetBudget, _pendingRequestBudget + 1);
                _stableOneFrameBatchCount = 0;
            }
            else
            {
                _adaptiveRequestsPerFrame = math.min(_adaptiveRequestsPerFrame, targetBudget);
            }
        }
        else
        {
            _stableOneFrameBatchCount = 0;
            _adaptiveRequestsPerFrame = math.min(_pendingRequestBudget, targetBudget);
        }
    }

    private void DisposePendingLiveSnapshots()
    {
        if (_pendingLiveUnitEntities.IsCreated)
            _pendingLiveUnitEntities.Dispose();
        if (_pendingLiveUnitGrids.IsCreated)
            _pendingLiveUnitGrids.Dispose();
        if (_pendingLiveUnitFootprints.IsCreated)
            _pendingLiveUnitFootprints.Dispose();
        if (_pendingLiveUnitManualGroupMembers.IsCreated)
            _pendingLiveUnitManualGroupMembers.Dispose();
    }

    private static int HeuristicOctile(int2 a, int2 b)
    {
        int dx = math.abs(a.x - b.x);
        int dy = math.abs(a.y - b.y);
        int diagonal = math.min(dx, dy);
        int straight = math.max(dx, dy) - diagonal;
        return (diagonal * FreeDiagonalTraversalCost) + (straight * FreeTraversalCost);
    }

    private int2 GetSegmentGoalHierarchical(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        int2 start,
        int2 requestedGoal,
        bool manualMove,
        bool isVehicle,
        byte factionId,
        out bool usedHierarchicalWaypoint,
        out bool hierarchicalFallback)
    {
        usedHierarchicalWaypoint = false;
        hierarchicalFallback = false;
        float2 delta = new float2(requestedGoal.x - start.x, requestedGoal.y - start.y);
        float distance = math.length(delta);
        float maxSegmentCells = GetMaxSegmentCells(manualMove, isVehicle);
        if (distance <= maxSegmentCells || distance <= 0.001f)
            return requestedGoal;

        if (manualMove &&
            EnsureCoarseScratch(grid.Width, grid.Height) &&
            TryFindHierarchicalWaypoint(
                grid,
                walkable,
                dynamicBlocked,
                friendlyPassFactionIds,
                start,
                requestedGoal,
                maxSegmentCells,
                factionId,
                out int2 waypoint))
        {
            usedHierarchicalWaypoint = true;
            return waypoint;
        }

        hierarchicalFallback = manualMove;

        float2 dir = delta / distance;
        int2 segmentGoal = start + (int2)math.round(dir * maxSegmentCells);
        return segmentGoal;
    }

    private static float GetMaxSegmentCells(bool manualMove, bool isVehicle)
    {
        if (!manualMove)
            return DefaultLongDistanceSegmentCells;

        return isVehicle
            ? ManualVehicleLongDistanceSegmentCells
            : ManualInfantryLongDistanceSegmentCells;
    }

    private bool EnsureCoarseScratch(int gridWidth, int gridHeight)
    {
        int width = (gridWidth + HierarchicalSectorSizeCells - 1) / HierarchicalSectorSizeCells;
        int height = (gridHeight + HierarchicalSectorSizeCells - 1) / HierarchicalSectorSizeCells;
        int count = width * height;
        if (count <= 0)
            return false;

        if (_coarseCameFrom.IsCreated && _coarseWidth == width && _coarseHeight == height)
            return true;

        DisposeCoarseScratch();
        _coarseWidth = width;
        _coarseHeight = height;
        _coarseCameFrom = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _coarseGScore = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _coarseEpoch = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _coarseClosedEpoch = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _coarseOpenEpoch = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _coarseOpen = new NativeArray<int>(count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _coarseSearchEpoch = 1;
        return true;
    }

    private void DisposeCoarseScratch()
    {
        if (_coarseCameFrom.IsCreated) _coarseCameFrom.Dispose();
        if (_coarseGScore.IsCreated) _coarseGScore.Dispose();
        if (_coarseEpoch.IsCreated) _coarseEpoch.Dispose();
        if (_coarseClosedEpoch.IsCreated) _coarseClosedEpoch.Dispose();
        if (_coarseOpenEpoch.IsCreated) _coarseOpenEpoch.Dispose();
        if (_coarseOpen.IsCreated) _coarseOpen.Dispose();
        _coarseWidth = 0;
        _coarseHeight = 0;
        _coarseSearchEpoch = 1;
    }

    private int ReserveCoarseSearchEpoch()
    {
        if (_coarseSearchEpoch <= 0 || _coarseSearchEpoch == int.MaxValue)
        {
            if (_coarseEpoch.IsCreated) _coarseEpoch.AsSpan().Clear();
            if (_coarseClosedEpoch.IsCreated) _coarseClosedEpoch.AsSpan().Clear();
            if (_coarseOpenEpoch.IsCreated) _coarseOpenEpoch.AsSpan().Clear();
            _coarseSearchEpoch = 1;
        }

        return _coarseSearchEpoch++;
    }

    private bool TryFindHierarchicalWaypoint(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        int2 start,
        int2 requestedGoal,
        float maxSegmentCells,
        byte factionId,
        out int2 waypoint)
    {
        waypoint = start;
        if (!GridUtils.InBounds(start, grid.Width, grid.Height) ||
            !GridUtils.InBounds(requestedGoal, grid.Width, grid.Height))
        {
            return false;
        }

        int2 startSector = CellToSector(start);
        int2 goalSector = CellToSector(requestedGoal);
        if (startSector.Equals(goalSector))
            return false;

        if (!TryGetSectorRepresentative(grid, walkable, dynamicBlocked, friendlyPassFactionIds, startSector, start, factionId, out _) ||
            !TryGetSectorRepresentative(grid, walkable, dynamicBlocked, friendlyPassFactionIds, goalSector, requestedGoal, factionId, out _))
        {
            return false;
        }

        int epoch = ReserveCoarseSearchEpoch();
        int startIndex = CoarseIndex(startSector);
        int goalIndex = CoarseIndex(goalSector);
        int openCount = 0;
        int expanded = 0;

        _coarseCameFrom[startIndex] = startIndex;
        _coarseGScore[startIndex] = 0;
        _coarseEpoch[startIndex] = epoch;
        _coarseOpenEpoch[startIndex] = epoch;
        _coarseOpen[openCount++] = startIndex;

        while (openCount > 0 && expanded < HierarchicalMaxExpandedSectors)
        {
            int bestOpenSlot = 0;
            int bestIndex = _coarseOpen[0];
            int bestScore = _coarseGScore[bestIndex] + HeuristicOctile(CoarseToSector(bestIndex), goalSector);
            for (int i = 1; i < openCount; i++)
            {
                int candidateIndex = _coarseOpen[i];
                int score = _coarseGScore[candidateIndex] + HeuristicOctile(CoarseToSector(candidateIndex), goalSector);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestIndex = candidateIndex;
                bestOpenSlot = i;
            }

            openCount--;
            _coarseOpen[bestOpenSlot] = _coarseOpen[openCount];
            _coarseOpenEpoch[bestIndex] = 0;
            if (_coarseClosedEpoch[bestIndex] == epoch)
                continue;

            _coarseClosedEpoch[bestIndex] = epoch;
            expanded++;

            if (bestIndex == goalIndex)
                return TryChooseWaypointFromCoarsePath(grid, walkable, dynamicBlocked, friendlyPassFactionIds, start, requestedGoal, maxSegmentCells, factionId, startIndex, goalIndex, out waypoint);

            int2 currentSector = CoarseToSector(bestIndex);
            for (int i = 0; i < SearchDirs.Length; i++)
            {
                int2 nextSector = currentSector + SearchDirs[i];
                if (!CoarseInBounds(nextSector))
                    continue;

                int nextIndex = CoarseIndex(nextSector);
                if (_coarseClosedEpoch[nextIndex] == epoch)
                    continue;

                if (!TryGetSectorRepresentative(grid, walkable, dynamicBlocked, friendlyPassFactionIds, nextSector, SectorCenterCell(grid, nextSector), factionId, out _))
                    continue;

                int stepCost = math.abs(SearchDirs[i].x) + math.abs(SearchDirs[i].y) == 2
                    ? FreeDiagonalTraversalCost
                    : FreeTraversalCost;
                int nextG = _coarseGScore[bestIndex] + stepCost;
                if (_coarseEpoch[nextIndex] == epoch && nextG >= _coarseGScore[nextIndex])
                    continue;

                _coarseCameFrom[nextIndex] = bestIndex;
                _coarseGScore[nextIndex] = nextG;
                _coarseEpoch[nextIndex] = epoch;
                if (_coarseOpenEpoch[nextIndex] != epoch && openCount < _coarseOpen.Length)
                {
                    _coarseOpen[openCount++] = nextIndex;
                    _coarseOpenEpoch[nextIndex] = epoch;
                }
            }
        }

        return false;
    }

    private bool TryChooseWaypointFromCoarsePath(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        int2 start,
        int2 requestedGoal,
        float maxSegmentCells,
        byte factionId,
        int startIndex,
        int goalIndex,
        out int2 waypoint)
    {
        waypoint = start;
        int current = goalIndex;
        int chosen = -1;
        int guard = 0;

        while (current != startIndex && guard++ < _coarseCameFrom.Length)
        {
            int2 sector = CoarseToSector(current);
            int2 preferredCell = current == goalIndex ? requestedGoal : SectorCenterCell(grid, sector);
            if (TryGetSectorRepresentative(grid, walkable, dynamicBlocked, friendlyPassFactionIds, sector, preferredCell, factionId, out int2 candidate))
            {
                float distance = math.distance(new float2(start.x, start.y), new float2(candidate.x, candidate.y));
                if (distance <= maxSegmentCells)
                {
                    chosen = current;
                    break;
                }
            }

            int previous = _coarseCameFrom[current];
            if (previous == current)
                break;
            current = previous;
        }

        if (chosen < 0)
            return false;

        int2 chosenSector = CoarseToSector(chosen);
        int2 chosenPreferred = chosen == goalIndex ? requestedGoal : SectorCenterCell(grid, chosenSector);
        if (!TryGetSectorRepresentative(grid, walkable, dynamicBlocked, friendlyPassFactionIds, chosenSector, chosenPreferred, factionId, out waypoint))
            return false;

        return !waypoint.Equals(start);
    }

    private int2 CellToSector(int2 cell)
    {
        return new int2(
            math.clamp(cell.x / HierarchicalSectorSizeCells, 0, _coarseWidth - 1),
            math.clamp(cell.y / HierarchicalSectorSizeCells, 0, _coarseHeight - 1));
    }

    private int CoarseIndex(int2 sector) => sector.y * _coarseWidth + sector.x;

    private int2 CoarseToSector(int index) => new int2(index % _coarseWidth, index / _coarseWidth);

    private bool CoarseInBounds(int2 sector) =>
        (uint)sector.x < (uint)_coarseWidth &&
        (uint)sector.y < (uint)_coarseHeight;

    private static int2 SectorCenterCell(in GridConfig grid, int2 sector)
    {
        int minX = sector.x * HierarchicalSectorSizeCells;
        int minY = sector.y * HierarchicalSectorSizeCells;
        int maxX = math.min(minX + HierarchicalSectorSizeCells - 1, grid.Width - 1);
        int maxY = math.min(minY + HierarchicalSectorSizeCells - 1, grid.Height - 1);
        return new int2((minX + maxX) / 2, (minY + maxY) / 2);
    }

    private static bool TryGetSectorRepresentative(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        int2 sector,
        int2 preferredCell,
        byte factionId,
        out int2 representative)
    {
        representative = default;
        int minX = sector.x * HierarchicalSectorSizeCells;
        int minY = sector.y * HierarchicalSectorSizeCells;
        int maxX = math.min(minX + HierarchicalSectorSizeCells - 1, grid.Width - 1);
        int maxY = math.min(minY + HierarchicalSectorSizeCells - 1, grid.Height - 1);
        int2 clampedPreferred = new int2(
            math.clamp(preferredCell.x, minX, maxX),
            math.clamp(preferredCell.y, minY, maxY));

        if (IsCoarseCellPassable(grid, walkable, dynamicBlocked, friendlyPassFactionIds, clampedPreferred, factionId))
        {
            representative = clampedPreferred;
            return true;
        }

        for (int radius = 2; radius <= HierarchicalSectorSizeCells / 2; radius += 2)
        {
            int steps = radius * 8;
            for (int step = 0; step < steps; step += 2)
            {
                int2 candidate = clampedPreferred + SquareRingOffset(radius, step);
                if (candidate.x < minX || candidate.x > maxX || candidate.y < minY || candidate.y > maxY)
                    continue;

                if (!IsCoarseCellPassable(grid, walkable, dynamicBlocked, friendlyPassFactionIds, candidate, factionId))
                    continue;

                representative = candidate;
                return true;
            }
        }

        for (int y = minY; y <= maxY; y += 4)
        {
            for (int x = minX; x <= maxX; x += 4)
            {
                int2 candidate = new int2(x, y);
                if (!IsCoarseCellPassable(grid, walkable, dynamicBlocked, friendlyPassFactionIds, candidate, factionId))
                    continue;

                representative = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool IsCoarseCellPassable(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        int2 cell,
        byte factionId)
    {
        if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
            return false;

        int index = GridUtils.CellToIndex(cell, grid.Width);
        if ((uint)index >= (uint)walkable.Length || walkable[index].Value == 0)
            return false;

        if (!dynamicBlocked.IsCreated || !dynamicBlocked.IsSet(index))
            return true;

        return friendlyPassFactionIds.IsCreated &&
               (uint)index < (uint)friendlyPassFactionIds.Length &&
               friendlyPassFactionIds[index] == factionId;
    }

    private bool EnsureScratch(int gridSize, out int scratchCells, out int threadSlots)
    {
        scratchCells = _scratchGridSize;
        threadSlots = 1;
        if (_scratchGridSize == gridSize && _scratchCameFrom.IsCreated)
            return false;

        DisposeScratch();

        _scratchGridSize = gridSize;
        int total = gridSize;
        scratchCells = gridSize;
        threadSlots = 1;

        _scratchCameFrom = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _scratchGScore = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _scratchClosed = new NativeArray<byte>(total, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _scratchInOpen = new NativeArray<byte>(total, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _scratchEpoch = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _scratchOpen = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _scratchPath = new NativeArray<int>(total, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        _scratchSearchEpoch = 1;
        return true;
    }

    private void DisposeScratch()
    {
        if (_scratchCameFrom.IsCreated) _scratchCameFrom.Dispose();
        if (_scratchGScore.IsCreated) _scratchGScore.Dispose();
        if (_scratchClosed.IsCreated) _scratchClosed.Dispose();
        if (_scratchInOpen.IsCreated) _scratchInOpen.Dispose();
        if (_scratchEpoch.IsCreated) _scratchEpoch.Dispose();
        if (_scratchOpen.IsCreated) _scratchOpen.Dispose();
        if (_scratchPath.IsCreated) _scratchPath.Dispose();
        _scratchGridSize = 0;
        _scratchSearchEpoch = 1;
    }

    private int ReserveScratchEpochs(int requestCount)
    {
        int requestedEpochs = math.max(1, requestCount * ScratchEpochsPerRequest);
        if (_scratchSearchEpoch <= 0 || _scratchSearchEpoch > int.MaxValue - requestedEpochs)
        {
            if (_scratchEpoch.IsCreated)
                _scratchEpoch.AsSpan().Clear();
            _scratchSearchEpoch = 1;
        }

        int epochBase = _scratchSearchEpoch;
        _scratchSearchEpoch += requestedEpochs;
        return epochBase;
    }

    private void PrepareReservedGoals(int gridSize)
    {
        if (!_reservedGoalEpochs.IsCreated || _reservedGoalGridSize != gridSize)
        {
            DisposeReservedGoals();
            _reservedGoalEpochs = new NativeArray<int>(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _reservedGoalGridSize = gridSize;
            _reservedGoalGeneration = 1;
            return;
        }

        if (_reservedGoalGeneration == int.MaxValue)
        {
            _reservedGoalEpochs.AsSpan().Clear();
            _reservedGoalGeneration = 1;
            return;
        }

        _reservedGoalGeneration++;
    }

    private void DisposeReservedGoals()
    {
        if (_reservedGoalEpochs.IsCreated) _reservedGoalEpochs.Dispose();
        _reservedGoalGridSize = 0;
        _reservedGoalGeneration = 0;
    }

    private static void ApplyResults(
        ref SystemState state,
        Entity gridEntity,
        ref PathPoolData pool,
        NativeArray<Entity> entities,
        NativeArray<UnitPathRequest> requests,
        NativeArray<int2> assignedGoals,
        NativeArray<byte> segmented,
        NativeArray<byte> manualMoves,
        NativeStream stream,
        NativeArray<byte> status,
        out int completedCount,
        out int completedSegmentCount,
        out int manualCompletedCount,
        out int retriedCount,
        out int retriedSegmentCount,
        out int manualRetriedCount,
        out int abandonedCount)
    {
        completedCount = 0;
        completedSegmentCount = 0;
        manualCompletedCount = 0;
        retriedCount = 0;
        retriedSegmentCount = 0;
        manualRetriedCount = 0;
        abandonedCount = 0;
        var em = state.EntityManager;
        var reader = stream.AsReader();
        var follow = new UnitPathFollow { PathIndex = 0 };

        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            bool entityHasMatchingRequest =
                em.Exists(entity) &&
                em.HasComponent<UnitPathRequest>(entity) &&
                em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(requests[i].Goal);

            int count = reader.BeginForEachIndex(i);
            int start = pool.Cells.Length;
            if (entityHasMatchingRequest)
            {
                for (int j = 0; j < count; j++)
                    pool.Cells.Add(reader.Read<int2>());
            }
            else
            {
                for (int j = 0; j < count; j++)
                    reader.Read<int2>();
            }
            reader.EndForEachIndex();

            if (!entityHasMatchingRequest)
                continue;

            if (status[i] == 1 && count > 0)
            {
                completedCount++;
                if (segmented[i] != 0)
                    completedSegmentCount++;
                if (manualMoves[i] != 0)
                    manualCompletedCount++;
                if (em.HasComponent<UnitPathRetryCooldown>(entity))
                    em.RemoveComponent<UnitPathRetryCooldown>(entity);

                if (em.HasComponent<UnitTarget>(entity))
                    em.SetComponentData(entity, new UnitTarget { Cell = assignedGoals[i] });
                else
                    em.AddComponentData(entity, new UnitTarget { Cell = assignedGoals[i] });

                if (em.HasComponent<UnitPathFollow>(entity))
                    em.SetComponentData(entity, follow);
                else
                    em.AddComponentData(entity, follow);

                var range = new UnitPathRange { Start = start, Length = count };
                if (em.HasComponent<UnitPathRange>(entity))
                    em.SetComponentData(entity, range);
                else
                    em.AddComponentData(entity, range);

                if (segmented[i] != 0)
                {
                    if (em.HasComponent<UnitLongDistanceMove>(entity))
                        em.SetComponentData(entity, new UnitLongDistanceMove { FinalGoal = requests[i].Goal });
                    else
                        em.AddComponentData(entity, new UnitLongDistanceMove { FinalGoal = requests[i].Goal });
                }
                else if (em.HasComponent<UnitLongDistanceMove>(entity))
                {
                    em.RemoveComponent<UnitLongDistanceMove>(entity);
                }
            }
            else
            {
                if (em.HasComponent<UnitPathFollow>(entity))
                    em.RemoveComponent<UnitPathFollow>(entity);
                if (em.HasComponent<UnitPathRange>(entity))
                    em.RemoveComponent<UnitPathRange>(entity);
                if (em.HasComponent<AutoWanderMoveTag>(entity))
                    em.RemoveComponent<AutoWanderMoveTag>(entity);

                bool shouldRetryManualMove =
                    em.HasComponent<ManualMoveOrderTag>(entity) ||
                    em.HasComponent<UnitLongDistanceMove>(entity) ||
                    segmented[i] != 0;

                if (shouldRetryManualMove)
                {
                    retriedCount++;
                    if (segmented[i] != 0)
                        retriedSegmentCount++;
                    if (manualMoves[i] != 0)
                        manualRetriedCount++;
                    if (segmented[i] != 0)
                    {
                        if (em.HasComponent<UnitLongDistanceMove>(entity))
                            em.SetComponentData(entity, new UnitLongDistanceMove { FinalGoal = requests[i].Goal });
                        else
                            em.AddComponentData(entity, new UnitLongDistanceMove { FinalGoal = requests[i].Goal });

                        if (em.HasComponent<UnitTarget>(entity))
                            em.SetComponentData(entity, new UnitTarget { Cell = requests[i].Goal });
                        else
                            em.AddComponentData(entity, new UnitTarget { Cell = requests[i].Goal });
                    }

                    int resumeFrame = Time.frameCount + FailedManualRetryDelayFrames;
                    if (em.HasComponent<UnitPathRetryCooldown>(entity))
                        em.SetComponentData(entity, new UnitPathRetryCooldown { ResumeFrame = resumeFrame });
                    else
                        em.AddComponentData(entity, new UnitPathRetryCooldown { ResumeFrame = resumeFrame });
                }
                else
                {
                    abandonedCount++;
                    if (em.HasComponent<UnitLongDistanceMove>(entity))
                        em.RemoveComponent<UnitLongDistanceMove>(entity);
                    if (em.HasComponent<ManualMoveOrderTag>(entity))
                        em.RemoveComponent<ManualMoveOrderTag>(entity);
                    if (em.HasComponent<UnitTarget>(entity))
                        em.RemoveComponent<UnitTarget>(entity);
                    if (em.HasComponent<UnitPathRetryCooldown>(entity))
                        em.RemoveComponent<UnitPathRetryCooldown>(entity);
                }
            }

            if (em.HasComponent<UnitPathRequest>(entity))
                em.RemoveComponent<UnitPathRequest>(entity);
        }
    }

    [BurstCompile]
    private struct PathfindBatchJob : IJobFor
    {
        [ReadOnly] public GridConfig Grid;
        [ReadOnly] public NativeArray<GridWalkable> Walkable;
        [ReadOnly] public NativeArray<GridRoad> Roads;
        [ReadOnly] public NativeArray<GridRoadSidewalk> Sidewalks;
        [ReadOnly] public NativeArray<GridRoadDirt> DirtRoads;
        [ReadOnly] public NativeBitArray DynamicBlocked;
        [ReadOnly] public NativeArray<byte> FriendlyPassFactionIds;
        [ReadOnly] public NativeBitArray Occupied;
        [ReadOnly] public NativeArray<Entity> LiveUnitEntities;
        [ReadOnly] public NativeArray<UnitGrid> LiveUnitGrids;
        [ReadOnly] public NativeArray<UnitFootprint> LiveUnitFootprints;
        [ReadOnly] public NativeArray<byte> LiveUnitManualGroupMembers;
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<UnitGrid> UnitGrids;
        [ReadOnly] public NativeArray<UnitFootprint> Footprints;
        [ReadOnly] public NativeArray<UnitMovementBehavior> MovementBehaviors;
        [ReadOnly] public NativeArray<byte> Factions;
        [ReadOnly] public NativeArray<byte> ManualMoves;
        [ReadOnly] public NativeArray<Entity> IgnoredOccupancyEntities;
        [ReadOnly] public NativeArray<int2> IgnoredOccupancyCells;
        [ReadOnly] public NativeArray<int2> IgnoredOccupancySizes;
        [ReadOnly] public NativeArray<UnitPathRequest> RequestedGoals;
        public NativeArray<int2> Goals;
        [NativeDisableParallelForRestriction] public NativeArray<byte> Segmented;

        public NativeStream.Writer Output;
        public NativeArray<byte> Status; // 1 = found, 0 = none/invalid
        [ReadOnly] public NativeArray<byte> CheapSegmentModes;
        [ReadOnly] public NativeArray<byte> AlternateSearchSkipped;
        [NativeDisableParallelForRestriction] public NativeArray<int> AlternateAttempts;

        public int GridSize;

        [NativeDisableParallelForRestriction] public NativeArray<int> ScratchCameFrom;
        [NativeDisableParallelForRestriction] public NativeArray<int> ScratchGScore;
        [NativeDisableParallelForRestriction] public NativeArray<byte> ScratchClosed;
        [NativeDisableParallelForRestriction] public NativeArray<byte> ScratchInOpen;
        [NativeDisableParallelForRestriction] public NativeArray<int> ScratchEpoch;
        [NativeDisableParallelForRestriction] public NativeArray<int> ScratchOpen;
        [NativeDisableParallelForRestriction] public NativeArray<int> ScratchPath;
        public int SearchEpochBase;

        public void Execute(int index)
        {
            Output.BeginForEachIndex(index);

            int2 start = UnitGrids[index].Cell;
            UnitFootprint footprint = Footprints[index];
            int2 footprintSize = footprint.Size;
            int2 desiredGoal = RequestedGoals[index].Goal;
            int2 goal = Goals[index];
            byte factionId = Factions[index];
            bool isVehicle = UnitVehicleMovementUtility.IsVehicle(footprint, MovementBehaviors[index]);
            bool manualMove = ManualMoves[index] != 0;
            bool cheapSegmentMode = CheapSegmentModes[index] != 0;
            bool skipAlternateSearch = AlternateSearchSkipped[index] != 0;
            Entity movingEntity = Entities[index];
            Entity ignoredOccupancyEntity = IgnoredOccupancyEntities[index];
            int2 ignoredOccupancyCell = IgnoredOccupancyCells[index];
            int2 ignoredOccupancySize = IgnoredOccupancySizes[index];

            if (!GridUtils.InBounds(start, Grid.Width, Grid.Height))
            {
                Status[index] = 0;
                Output.EndForEachIndex();
                return;
            }

            int startIndex = GridUtils.CellToIndex(start, Grid.Width);
            if (Walkable[startIndex].Value == 0)
            {
                Status[index] = 0;
                Output.EndForEachIndex();
                return;
            }

            int searchEpoch = SearchEpochBase + (index * ScratchEpochsPerRequest);
            if (TryWritePath(movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, goal, footprintSize, isVehicle, manualMove, cheapSegmentMode, factionId, searchEpoch))
            {
                Status[index] = 1;
                Output.EndForEachIndex();
                return;
            }

            int2 searchCenter = goal;
            if (!GridUtils.InBounds(searchCenter, Grid.Width, Grid.Height))
            {
                searchCenter = new int2(
                    math.clamp(searchCenter.x, 0, Grid.Width - 1),
                    math.clamp(searchCenter.y, 0, Grid.Height - 1));
            }

            bool isFinalSegment = desiredGoal.Equals(goal);
            if (isFinalSegment && GridUtils.InBounds(desiredGoal, Grid.Width, Grid.Height))
                searchCenter = desiredGoal;

            int maxRadius = math.min(
                math.max(Grid.Width, Grid.Height),
                isVehicle ? VehicleGoalSearchRadius : InfantryGoalSearchRadius);
            int candidateAttempts = 0;
            if (!skipAlternateSearch)
            {
                int maxCandidateAttempts = isVehicle ? VehicleAlternateGoalCandidates : InfantryAlternateGoalCandidates;
                for (int radius = 1; radius <= maxRadius; radius++)
                {
                    int ringLen = math.max(1, 8 * radius);
                    for (int step = 0; step < ringLen; step++)
                    {
                        if (candidateAttempts >= maxCandidateAttempts)
                        {
                            radius = maxRadius + 1;
                            break;
                        }

                        int2 candidate = searchCenter + SquareRingOffset(radius, step);
                        if (!GridUtils.InBounds(candidate, Grid.Width, Grid.Height))
                            continue;

                        if (candidate.Equals(goal) || candidate.Equals(start))
                            continue;

                        if (!CanReachGoalCell(movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, candidate, footprintSize, isVehicle, manualMove, factionId))
                            continue;

                        candidateAttempts++;
                        if (TryWritePath(movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, candidate, footprintSize, isVehicle, manualMove, cheapSegmentMode, factionId, searchEpoch + candidateAttempts + 1))
                        {
                            AlternateAttempts[index] = candidateAttempts;
                            Goals[index] = candidate;
                            Status[index] = 1;
                            Output.EndForEachIndex();
                            return;
                        }
                    }
                }
            }

            AlternateAttempts[index] = candidateAttempts;

            if (manualMove && !isVehicle &&
                TryWriteSegmentProgressFallback(movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, desiredGoal, footprintSize, isVehicle, manualMove, factionId, out int2 fallbackGoal))
            {
                Goals[index] = fallbackGoal;
                Segmented[index] = 1;
                Status[index] = 1;
                Output.EndForEachIndex();
                return;
            }

            Status[index] = 0;
            Output.EndForEachIndex();
        }

        private bool TryWriteSegmentProgressFallback(
            Entity movingEntity,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            int2 start,
            int2 desiredGoal,
            int2 footprintSize,
            bool isVehicle,
            bool manualMove,
            byte factionId,
            out int2 fallbackGoal)
        {
            int bestDistanceSq = int.MaxValue;
            int2 bestCell = start;
            fallbackGoal = start;
            int maxRadius = math.min(16, math.max(Grid.Width, Grid.Height));

            for (int radius = maxRadius; radius >= 1; radius--)
            {
                int ringLen = math.max(1, 8 * radius);
                for (int step = 0; step < ringLen; step++)
                {
                    int2 candidate = start + SquareRingOffset(radius, step);
                    if (!GridUtils.InBounds(candidate, Grid.Width, Grid.Height))
                        continue;
                    if (candidate.Equals(start))
                        continue;
                    if (!CanReachGoalCell(movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, candidate, footprintSize, isVehicle, manualMove, factionId))
                        continue;
                    if (!HasDirectPath(Grid, Walkable, DynamicBlocked, FriendlyPassFactionIds, Occupied, LiveUnitEntities, LiveUnitGrids, LiveUnitFootprints, LiveUnitManualGroupMembers, movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, candidate, footprintSize, isVehicle, manualMove, factionId))
                        continue;

                    int dx = candidate.x - desiredGoal.x;
                    int dy = candidate.y - desiredGoal.y;
                    int distanceSq = (dx * dx) + (dy * dy);
                    if (distanceSq >= bestDistanceSq)
                        continue;

                    bestDistanceSq = distanceSq;
                    bestCell = candidate;
                }

                if (!bestCell.Equals(start))
                    break;
            }

            if (bestCell.Equals(start))
                return false;

            fallbackGoal = bestCell;
            Output.Write(start);
            Output.Write(bestCell);
            return true;
        }

        private bool CanReachGoalCell(Entity movingEntity, Entity ignoredOccupancyEntity, int2 ignoredOccupancyCell, int2 ignoredOccupancySize, int2 start, int2 goal, int2 footprintSize, bool isVehicle, bool manualMove, byte factionId)
        {
            int goalIndex = GridUtils.CellToIndex(goal, Grid.Width);
            if (Walkable[goalIndex].Value == 0)
                return false;

            return CanPlaceForPathing(
                Grid,
                Walkable,
                DynamicBlocked,
                FriendlyPassFactionIds,
                Occupied,
                LiveUnitEntities,
                LiveUnitGrids,
                LiveUnitFootprints,
                LiveUnitManualGroupMembers,
                movingEntity,
                goal,
                footprintSize,
                start,
                isVehicle,
                manualMove,
                factionId,
                ignoredOccupancyEntity,
                ignoredOccupancyCell,
                ignoredOccupancySize);
        }

        private bool TryWritePath(Entity movingEntity, Entity ignoredOccupancyEntity, int2 ignoredOccupancyCell, int2 ignoredOccupancySize, int2 start, int2 goal, int2 footprintSize, bool isVehicle, bool manualMove, bool cheapSegmentMode, byte factionId, int searchEpoch)
        {
            if (!GridUtils.InBounds(goal, Grid.Width, Grid.Height))
                return false;

            int startIndex = GridUtils.CellToIndex(start, Grid.Width);
            int goalIndex = GridUtils.CellToIndex(goal, Grid.Width);
            int searchBoundsPadding = isVehicle
                ? VehicleSearchBoundsPaddingCells
                : cheapSegmentMode ? InfantrySegmentedSearchBoundsPaddingCells : InfantrySearchBoundsPaddingCells;
            int minSearchX = math.max(0, math.min(start.x, goal.x) - searchBoundsPadding);
            int maxSearchX = math.min(Grid.Width - 1, math.max(start.x, goal.x) + searchBoundsPadding);
            int minSearchY = math.max(0, math.min(start.y, goal.y) - searchBoundsPadding);
            int maxSearchY = math.min(Grid.Height - 1, math.max(start.y, goal.y) + searchBoundsPadding);
            if (Walkable[goalIndex].Value == 0)
                return false;

            bool goalValid = CanPlaceForPathing(
                Grid,
                Walkable,
                DynamicBlocked,
                FriendlyPassFactionIds,
                Occupied,
                LiveUnitEntities,
                LiveUnitGrids,
                LiveUnitFootprints,
                LiveUnitManualGroupMembers,
                movingEntity,
                goal,
                footprintSize,
                start,
                isVehicle,
                manualMove,
                factionId,
                ignoredOccupancyEntity,
                ignoredOccupancyCell,
                ignoredOccupancySize);
            if (goalIndex != startIndex && !goalValid)
                return false;

            if (HasDirectPath(Grid, Walkable, DynamicBlocked, FriendlyPassFactionIds, Occupied, LiveUnitEntities, LiveUnitGrids, LiveUnitFootprints, LiveUnitManualGroupMembers, movingEntity, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize, start, goal, footprintSize, isVehicle, manualMove, factionId))
            {
                Output.Write(start);
                if (!start.Equals(goal))
                    Output.Write(goal);
                return true;
            }

            int threadOffset = 0;

            InitializeScratchNode(threadOffset, startIndex, searchEpoch);
            ScratchGScore[threadOffset + startIndex] = 0;
            ScratchOpen[threadOffset + 0] = startIndex;
            ScratchInOpen[threadOffset + startIndex] = 1;
            int openCount = 1;
            int expansions = 0;
            int maxExpansions = isVehicle
                ? VehicleMaxAStarExpansions
                : cheapSegmentMode ? InfantrySegmentedMaxAStarExpansions : InfantryMaxAStarExpansions;

            bool found = false;

            while (openCount > 0)
            {
                expansions++;
                if (expansions > maxExpansions)
                    return false;

                int bestOpenIdx = 0;
                int current = ScratchOpen[threadOffset + 0];
                int bestF = int.MaxValue;

                for (int i = 0; i < openCount; i++)
                {
                    int idx = ScratchOpen[threadOffset + i];
                    int g = ScratchGScore[threadOffset + idx];
                    int2 c = GridUtils.IndexToCell(idx, Grid.Width);
                    int f = g + HeuristicOctile(c, goal);
                    if (f < bestF)
                    {
                        bestF = f;
                        current = idx;
                        bestOpenIdx = i;
                    }
                }

                openCount--;
                ScratchOpen[threadOffset + bestOpenIdx] = ScratchOpen[threadOffset + openCount];
                ScratchInOpen[threadOffset + current] = 0;

                if (current == goalIndex)
                {
                    found = true;
                    break;
                }

                ScratchClosed[threadOffset + current] = 1;
                int2 currentCell = GridUtils.IndexToCell(current, Grid.Width);

                for (int d = 0; d < SearchDirs.Length; d++)
                {
                    int2 nextCell = currentCell + SearchDirs[d];
                    if (!GridUtils.InBounds(nextCell, Grid.Width, Grid.Height))
                        continue;
                    if (nextCell.x < minSearchX || nextCell.x > maxSearchX || nextCell.y < minSearchY || nextCell.y > maxSearchY)
                        continue;

                    int nextIndex = GridUtils.CellToIndex(nextCell, Grid.Width);
                    InitializeScratchNode(threadOffset, nextIndex, searchEpoch);
                    if (ScratchClosed[threadOffset + nextIndex] != 0)
                        continue;

                    if (Walkable[nextIndex].Value == 0)
                        continue;

                    bool diagonalStep = nextCell.x != currentCell.x && nextCell.y != currentCell.y;
                    if (diagonalStep)
                    {
                        int2 horizontalCell = new int2(nextCell.x, currentCell.y);
                        int2 verticalCell = new int2(currentCell.x, nextCell.y);
                        bool canPlaceHorizontal = CanPlaceForPathing(
                            Grid,
                            Walkable,
                            DynamicBlocked,
                            FriendlyPassFactionIds,
                            Occupied,
                            LiveUnitEntities,
                            LiveUnitGrids,
                            LiveUnitFootprints,
                            LiveUnitManualGroupMembers,
                            movingEntity,
                            horizontalCell,
                            footprintSize,
                            currentCell,
                            isVehicle,
                            manualMove,
                            factionId,
                            ignoredOccupancyEntity,
                            ignoredOccupancyCell,
                            ignoredOccupancySize);
                        bool canPlaceVertical = CanPlaceForPathing(
                            Grid,
                            Walkable,
                            DynamicBlocked,
                            FriendlyPassFactionIds,
                            Occupied,
                            LiveUnitEntities,
                            LiveUnitGrids,
                            LiveUnitFootprints,
                            LiveUnitManualGroupMembers,
                            movingEntity,
                            verticalCell,
                            footprintSize,
                            currentCell,
                            isVehicle,
                            manualMove,
                            factionId,
                            ignoredOccupancyEntity,
                            ignoredOccupancyCell,
                            ignoredOccupancySize);
                        if (!canPlaceHorizontal || !canPlaceVertical)
                            continue;
                    }

                    int addCost = GetTraversalCost(nextIndex, diagonalStep, isVehicle);
                    bool canPlaceNext = CanPlaceForPathing(
                        Grid,
                        Walkable,
                        DynamicBlocked,
                        FriendlyPassFactionIds,
                        Occupied,
                        LiveUnitEntities,
                        LiveUnitGrids,
                        LiveUnitFootprints,
                        LiveUnitManualGroupMembers,
                        movingEntity,
                        nextCell,
                        footprintSize,
                        currentCell,
                        isVehicle,
                        manualMove,
                        factionId,
                        ignoredOccupancyEntity,
                        ignoredOccupancyCell,
                        ignoredOccupancySize);
                    if (!canPlaceNext)
                        continue;

                    int currentG = ScratchGScore[threadOffset + current];
                    int tentative = currentG + addCost;
                    if (tentative >= ScratchGScore[threadOffset + nextIndex])
                        continue;

                    ScratchCameFrom[threadOffset + nextIndex] = current;
                    ScratchGScore[threadOffset + nextIndex] = tentative;

                    if (ScratchInOpen[threadOffset + nextIndex] == 0)
                    {
                        ScratchInOpen[threadOffset + nextIndex] = 1;
                        ScratchOpen[threadOffset + openCount] = nextIndex;
                        openCount++;
                    }
                }
            }

            if (!found)
                return false;

            int pathLen = 0;
            int cur = goalIndex;

            while (cur >= 0 && pathLen < GridSize)
            {
                ScratchPath[threadOffset + pathLen] = cur;
                pathLen++;
                if (cur == startIndex)
                    break;
                cur = ScratchCameFrom[threadOffset + cur];
            }

            if (pathLen == 0 || ScratchPath[threadOffset + (pathLen - 1)] != startIndex)
                return false;

            bool keepFullPath = !isVehicle;
            for (int i = pathLen - 1; i >= 0; i--)
            {
                int cellIndex = ScratchPath[threadOffset + i];
                int2 cell = GridUtils.IndexToCell(cellIndex, Grid.Width);

                bool isFirst = i == pathLen - 1;
                bool isLast = i == 0;
                if (keepFullPath || isFirst || isLast)
                {
                    Output.Write(cell);
                    continue;
                }

                int2 prevCell = GridUtils.IndexToCell(ScratchPath[threadOffset + i + 1], Grid.Width);
                int2 nextCell = GridUtils.IndexToCell(ScratchPath[threadOffset + i - 1], Grid.Width);
                int2 prevDir = cell - prevCell;
                int2 nextDir = nextCell - cell;

                if (!prevDir.Equals(nextDir))
                    Output.Write(cell);
            }

            return true;
        }

        private void InitializeScratchNode(int threadOffset, int cellIndex, int searchEpoch)
        {
            int offset = threadOffset + cellIndex;
            if (ScratchEpoch[offset] == searchEpoch)
                return;

            ScratchEpoch[offset] = searchEpoch;
            ScratchCameFrom[offset] = -1;
            ScratchGScore[offset] = int.MaxValue;
            ScratchClosed[offset] = 0;
            ScratchInOpen[offset] = 0;
        }

        private int GetTraversalCost(int cellIndex, bool diagonalStep, bool isVehicle)
        {
            bool isSidewalk = Sidewalks[cellIndex].Value != 0;
            bool isDirtRoad = DirtRoads[cellIndex].Value != 0;

            if (isVehicle)
            {
                if (isDirtRoad)
                    return diagonalStep ? PreferredSurfaceDiagonalTraversalCost : PreferredSurfaceTraversalCost;
                if (isSidewalk)
                    return diagonalStep ? AvoidedSurfaceDiagonalTraversalCost : AvoidedSurfaceTraversalCost;
                return diagonalStep ? FreeDiagonalTraversalCost : FreeTraversalCost;
            }

            if (isSidewalk)
                return diagonalStep ? PreferredSurfaceDiagonalTraversalCost : PreferredSurfaceTraversalCost;
            if (isDirtRoad)
                return diagonalStep ? AvoidedSurfaceDiagonalTraversalCost : AvoidedSurfaceTraversalCost;
            return diagonalStep ? FreeDiagonalTraversalCost : FreeTraversalCost;
        }

        private static bool HasDirectPath(
            in GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied,
            NativeArray<Entity> liveUnitEntities,
            NativeArray<UnitGrid> liveUnitGrids,
            NativeArray<UnitFootprint> liveUnitFootprints,
            NativeArray<byte> liveUnitManualGroupMembers,
            Entity movingEntity,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize,
            int2 start,
            int2 goal,
            int2 footprintSize,
            bool isVehicle,
            bool manualMove,
            byte factionId)
        {
            int dx = math.abs(goal.x - start.x);
            int dy = math.abs(goal.y - start.y);
            int steps = math.max(dx, dy);
            if (steps <= 1)
                return true;

            int2 current = start;
            for (int step = 1; step <= steps; step++)
            {
                float t = (float)step / steps;
                int2 next = new int2(
                    (int)math.round(math.lerp(start.x, goal.x, t)),
                    (int)math.round(math.lerp(start.y, goal.y, t)));

                if (next.Equals(current))
                    continue;

                if (!CanPlaceForPathing(grid, walkable, dynamicBlocked, friendlyPassFactionIds, occupied, liveUnitEntities, liveUnitGrids, liveUnitFootprints, liveUnitManualGroupMembers, movingEntity, next, footprintSize, current, isVehicle, manualMove, factionId, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize))
                    return false;

                current = next;
            }

            return true;
        }

        public static bool CanPlaceForPathing(
            in GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied,
            NativeArray<Entity> liveUnitEntities,
            NativeArray<UnitGrid> liveUnitGrids,
            NativeArray<UnitFootprint> liveUnitFootprints,
            NativeArray<byte> liveUnitManualGroupMembers,
            Entity movingEntity,
            int2 cell,
            int2 footprintSize,
            int2 currentCell,
            bool isVehicle,
            bool manualMove,
            byte factionId,
            Entity ignoredOccupancyEntity = default,
            int2 ignoredOccupancyCell = default,
            int2 ignoredOccupancySize = default)
        {
            bool canPlace = isVehicle
                ? CanVehiclePlaceForPathing(
                    grid,
                    walkable,
                    dynamicBlocked,
                    friendlyPassFactionIds,
                    occupied,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    movingEntity,
                    cell,
                    footprintSize,
                    currentCell,
                    factionId,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize)
                : CanInfantryPlaceForPathing(
                    grid,
                    walkable,
                    dynamicBlocked,
                    friendlyPassFactionIds,
                    occupied,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    liveUnitManualGroupMembers,
                    movingEntity,
                    cell,
                    footprintSize,
                    currentCell,
                    manualMove,
                    factionId,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize);
            if (!canPlace)
                return false;

            if (!isVehicle)
                return true;

            return true;
        }

        private static bool CanInfantryPlaceForPathing(
            in GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied,
            NativeArray<Entity> liveUnitEntities,
            NativeArray<UnitGrid> liveUnitGrids,
            NativeArray<UnitFootprint> liveUnitFootprints,
            NativeArray<byte> liveUnitManualGroupMembers,
            Entity movingEntity,
            int2 cell,
            int2 footprintSize,
            int2 currentCell,
            bool manualMove,
            byte factionId,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize)
        {
            int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
            int2 max = min + clamped;

            if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
                return false;

            for (int y = min.y; y < max.y; y++)
            {
                int row = y * grid.Width;
                for (int x = min.x; x < max.x; x++)
                {
                    int idx = row + x;
                    if (walkable[idx].Value == 0)
                        return false;
                    if (dynamicBlocked.IsCreated && dynamicBlocked.IsSet(idx) &&
                        (!friendlyPassFactionIds.IsCreated || (uint)idx >= (uint)friendlyPassFactionIds.Length || friendlyPassFactionIds[idx] != factionId))
                        return false;

                    bool isCurrentFootprintCell = UnitFootprintUtility.ContainsCell(currentCell, clamped, new int2(x, y));
                    bool isIgnoredOccupancyCell =
                        ignoredOccupancyEntity != Entity.Null &&
                        UnitFootprintUtility.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new int2(x, y));
                    if (!isCurrentFootprintCell && occupied.IsCreated && occupied.IsSet(idx) &&
                        !isIgnoredOccupancyCell &&
                        !ShouldIgnoreManualGroupOccupancy(manualMove, grid.Width, idx, liveUnitEntities, liveUnitGrids, liveUnitFootprints, liveUnitManualGroupMembers, movingEntity))
                        return false;
                }
            }

            for (int i = 0; i < liveUnitEntities.Length; i++)
            {
                Entity other = liveUnitEntities[i];
                if (other == movingEntity || other == ignoredOccupancyEntity)
                    continue;

                if (manualMove && i < liveUnitManualGroupMembers.Length && liveUnitManualGroupMembers[i] != 0)
                    continue;

                int2 otherCell = liveUnitGrids[i].Cell;
                int2 otherSize = liveUnitFootprints[i].Size;
                if (UnitFootprintUtility.Overlaps(cell, footprintSize, otherCell, otherSize) &&
                    !UnitFootprintUtility.Overlaps(currentCell, footprintSize, otherCell, otherSize))
                    return false;
            }

            return true;
        }

        private static bool ShouldIgnoreManualGroupOccupancy(
            bool manualMove,
            int gridWidth,
            int idx,
            NativeArray<Entity> liveUnitEntities,
            NativeArray<UnitGrid> liveUnitGrids,
            NativeArray<UnitFootprint> liveUnitFootprints,
            NativeArray<byte> liveUnitManualGroupMembers,
            Entity movingEntity)
        {
            if (!manualMove)
                return false;

            int2 cell = GridUtils.IndexToCell(idx, gridWidth);
            for (int i = 0; i < liveUnitEntities.Length; i++)
            {
                if (liveUnitEntities[i] == movingEntity)
                    continue;
                if (i >= liveUnitManualGroupMembers.Length || liveUnitManualGroupMembers[i] == 0)
                    continue;

                int2 otherSize = liveUnitFootprints[i].Size;
                if (UnitFootprintUtility.ContainsCell(liveUnitGrids[i].Cell, otherSize, cell))
                    return true;
            }

            return false;
        }

        private static bool CanVehiclePlaceForPathing(
            in GridConfig grid,
            NativeArray<GridWalkable> walkable,
            NativeBitArray dynamicBlocked,
            NativeArray<byte> friendlyPassFactionIds,
            NativeBitArray occupied,
            NativeArray<Entity> liveUnitEntities,
            NativeArray<UnitGrid> liveUnitGrids,
            NativeArray<UnitFootprint> liveUnitFootprints,
            Entity movingEntity,
            int2 cell,
            int2 footprintSize,
            int2 currentCell,
            byte factionId,
            Entity ignoredOccupancyEntity,
            int2 ignoredOccupancyCell,
            int2 ignoredOccupancySize)
        {
            int padding = math.max(0, VehicleOccupancyPaddingCells);
            int2 clamped = UnitFootprintUtility.ClampSize(footprintSize);
            int2 min = UnitFootprintUtility.GetMinCell(cell, clamped);
            int2 max = min + clamped;

            if (min.x < 0 || min.y < 0 || max.x > grid.Width || max.y > grid.Height)
                return false;

            int2 currentMin = UnitFootprintUtility.GetMinCell(currentCell, clamped);
            int2 currentMax = currentMin + clamped;
            int2 paddedMin = min - new int2(padding, padding);
            int2 paddedMax = max + new int2(padding, padding);

            if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > grid.Width || paddedMax.y > grid.Height)
                return false;

            for (int y = paddedMin.y; y < paddedMax.y; y++)
            {
                int row = y * grid.Width;
                for (int x = paddedMin.x; x < paddedMax.x; x++)
                {
                    bool insideActualFootprint = x >= min.x && x < max.x && y >= min.y && y < max.y;
                    int idx = row + x;
                    if (insideActualFootprint)
                    {
                        if (walkable[idx].Value == 0)
                            return false;
                        if (dynamicBlocked.IsCreated && dynamicBlocked.IsSet(idx) &&
                            (!friendlyPassFactionIds.IsCreated || (uint)idx >= (uint)friendlyPassFactionIds.Length || friendlyPassFactionIds[idx] != factionId))
                            return false;
                    }

                    bool isCurrentFootprintCell =
                        x >= currentMin.x && x < currentMax.x &&
                        y >= currentMin.y && y < currentMax.y;
                    bool isIgnoredOccupancyCell =
                        ignoredOccupancyEntity != Entity.Null &&
                        UnitFootprintUtility.ContainsCell(ignoredOccupancyCell, ignoredOccupancySize, new int2(x, y));
                    if (!isCurrentFootprintCell && occupied.IsCreated && occupied.IsSet(idx))
                    {
                        if (!isIgnoredOccupancyCell &&
                            !IsOnlySoftBlockerAtCell(grid.Width, idx, liveUnitEntities, liveUnitGrids, liveUnitFootprints, movingEntity))
                            return false;
                    }
                }
            }

            for (int i = 0; i < liveUnitEntities.Length; i++)
            {
                Entity other = liveUnitEntities[i];
                if (other == movingEntity || other == ignoredOccupancyEntity)
                    continue;

                int2 otherCell = liveUnitGrids[i].Cell;
                int2 otherSize = liveUnitFootprints[i].Size;
                if (UnitFootprintUtility.Overlaps(cell, footprintSize, otherCell, otherSize) &&
                    !UnitFootprintUtility.Overlaps(currentCell, footprintSize, otherCell, otherSize) &&
                    !IsSoftBlocker(otherSize))
                    return false;
            }

            return true;
        }

        private static bool IsOnlySoftBlockerAtCell(
            int gridWidth,
            int idx,
            NativeArray<Entity> liveUnitEntities,
            NativeArray<UnitGrid> liveUnitGrids,
            NativeArray<UnitFootprint> liveUnitFootprints,
            Entity movingEntity)
        {
            int2 cell = GridUtils.IndexToCell(idx, gridWidth);
            bool foundSoft = false;
            for (int i = 0; i < liveUnitEntities.Length; i++)
            {
                if (liveUnitEntities[i] == movingEntity)
                    continue;

                int2 otherSize = liveUnitFootprints[i].Size;
                if (!UnitFootprintUtility.ContainsCell(liveUnitGrids[i].Cell, otherSize, cell))
                    continue;

                if (!IsSoftBlocker(otherSize))
                    return false;

                foundSoft = true;
            }

            return foundSoft;
        }

        private static bool IsSoftBlocker(int2 size)
        {
            int2 clamped = UnitFootprintUtility.ClampSize(size);
            return clamped.x == 1 && clamped.y == 1;
        }

        private static int2 SquareRingOffset(int r, int step)
        {
            int topLen = (2 * r) + 1;
            if (step < topLen)
                return new int2(-r + step, r);

            step -= topLen;
            int rightLen = 2 * r;
            if (step < rightLen)
                return new int2(r, (r - 1) - step);

            step -= rightLen;
            int bottomLen = 2 * r;
            if (step < bottomLen)
                return new int2((r - 1) - step, -r);

            step -= bottomLen;
            return new int2(-r, (-r + 1) + step);
        }
    }

    private static bool IsFree(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        int cellIndex,
        byte factionId) =>
        (uint)cellIndex < (uint)(grid.Width * grid.Height) &&
        walkable[cellIndex].Value != 0 &&
        (!dynamicBlocked.IsSet(cellIndex) ||
         ((uint)cellIndex < (uint)friendlyPassFactionIds.Length && friendlyPassFactionIds[cellIndex] == factionId)) &&
        !occupied.IsSet(cellIndex);

    private static bool CanUseGoalCell(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        NativeArray<int> reservedGoalEpochs,
        int reservedGoalGeneration,
        Entity movingEntity,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        int2 cell,
        int2 footprintSize,
        int2 startCell,
        byte factionId)
    {
        bool isVehicle = footprintSize.x > 1 || footprintSize.y > 1;
        if (!PathfindBatchJob.CanPlaceForPathing(grid, walkable, dynamicBlocked, friendlyPassFactionIds, occupied, liveUnitEntities, liveUnitGrids, liveUnitFootprints, default, movingEntity, cell, footprintSize, startCell, isVehicle, false, factionId, ignoredOccupancyEntity, ignoredOccupancyCell, ignoredOccupancySize))
            return false;

        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                if (reservedGoalEpochs[row + x] == reservedGoalGeneration)
                    return false;
            }
        }

        return true;
    }

    private static void ReserveGoalFootprint(
        in GridConfig grid,
        NativeArray<int> reservedGoalEpochs,
        int reservedGoalGeneration,
        int2 cell,
        int2 footprintSize)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        for (int y = min.y; y < max.y; y++)
        {
            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
                reservedGoalEpochs[row + x] = reservedGoalGeneration;
        }
    }

    private static int2 FindNearestFreeGoal(
        in GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray dynamicBlocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
        NativeArray<Entity> liveUnitEntities,
        NativeArray<UnitGrid> liveUnitGrids,
        NativeArray<UnitFootprint> liveUnitFootprints,
        NativeArray<int> reservedGoalEpochs,
        int reservedGoalGeneration,
        Entity movingEntity,
        Entity ignoredOccupancyEntity,
        int2 ignoredOccupancyCell,
        int2 ignoredOccupancySize,
        int2 desiredGoal,
        int2 startCell,
        int2 footprintSize,
        byte factionId,
        int startIndex)
    {
        int2 bestCell = startCell;
        int bestDistanceSq = int.MaxValue;

        void ConsiderBest(int2 cell)
        {
            int dx = cell.x - desiredGoal.x;
            int dy = cell.y - desiredGoal.y;
            int distSq = (dx * dx) + (dy * dy);
            if (distSq < bestDistanceSq)
            {
                bestDistanceSq = distSq;
                bestCell = cell;
            }
        }

        if (GridUtils.InBounds(desiredGoal, grid.Width, grid.Height))
        {
            int desiredIndex = GridUtils.CellToIndex(desiredGoal, grid.Width);
            if (desiredIndex == startIndex)
                return desiredGoal;
            if (CanUseGoalCell(
                    grid,
                    walkable,
                    dynamicBlocked,
                    friendlyPassFactionIds,
                    occupied,
                    liveUnitEntities,
                    liveUnitGrids,
                    liveUnitFootprints,
                    reservedGoalEpochs,
                    reservedGoalGeneration,
                    movingEntity,
                    ignoredOccupancyEntity,
                    ignoredOccupancyCell,
                    ignoredOccupancySize,
                    desiredGoal,
                    footprintSize,
                    startCell,
                    factionId))
            {
                ReserveGoalFootprint(grid, reservedGoalEpochs, reservedGoalGeneration, desiredGoal, footprintSize);
                return desiredGoal;
            }
        }

        bool isVehicle = footprintSize.x > 1 || footprintSize.y > 1;
        int maxRadius = math.min(
            math.max(grid.Width, grid.Height),
            isVehicle ? VehicleGoalSearchRadius : InfantryGoalSearchRadius);
        uint seed = math.hash(new int3(desiredGoal.x, desiredGoal.y, startIndex));
        for (int r = 1; r <= maxRadius; r++)
        {
            int ringLen = 8 * r;
            int startStep = (int)(seed % (uint)ringLen);

            for (int step = 0; step < ringLen; step++)
            {
                int s = startStep + step;
                if (s >= ringLen) s -= ringLen;

                var offset = SquareRingOffset(r, s);
                var cell = desiredGoal + offset;
                if (!GridUtils.InBounds(cell, grid.Width, grid.Height))
                    continue;

                int idx = GridUtils.CellToIndex(cell, grid.Width);
                if (idx == startIndex)
                    continue;

                if (CanUseGoalCell(
                        grid,
                        walkable,
                        dynamicBlocked,
                        friendlyPassFactionIds,
                        occupied,
                        liveUnitEntities,
                        liveUnitGrids,
                        liveUnitFootprints,
                        reservedGoalEpochs,
                        reservedGoalGeneration,
                        movingEntity,
                        ignoredOccupancyEntity,
                        ignoredOccupancyCell,
                        ignoredOccupancySize,
                        cell,
                        footprintSize,
                        startCell,
                        factionId))
                {
                    ConsiderBest(cell);
                    ReserveGoalFootprint(grid, reservedGoalEpochs, reservedGoalGeneration, cell, footprintSize);
                    return bestCell;
                }
            }
        }

        if (bestDistanceSq != int.MaxValue)
        {
            ReserveGoalFootprint(grid, reservedGoalEpochs, reservedGoalGeneration, bestCell, footprintSize);
            return bestCell;
        }

        return startCell;
    }

    private static int2 SquareRingOffset(int r, int step)
    {
        // Perimeter of a square "ring" (Chebyshev distance r), clockwise, without repeating corners.
        // Total steps = 8*r.
        int topLen = (2 * r) + 1;
        if (step < topLen)
            return new int2(-r + step, r);

        step -= topLen;
        int rightLen = 2 * r;
        if (step < rightLen)
            return new int2(r, (r - 1) - step);

        step -= rightLen;
        int bottomLen = 2 * r;
        if (step < bottomLen)
            return new int2((r - 1) - step, -r);

        step -= bottomLen;
        // leftLen = 2*r - 1
        return new int2(-r, (-r + 1) + step);
    }
}
