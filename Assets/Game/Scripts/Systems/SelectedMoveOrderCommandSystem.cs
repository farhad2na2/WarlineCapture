using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct SelectedMoveOrderCommandSystem : ISystem
{
    public delegate bool ClickedUnitResolver(Vector2 screenPosition, EntityManager em, out Entity entity);
    public delegate bool ClickedCellResolver(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint);

    private const bool EnableMoveOrderDiagnostics = false;
    private static readonly bool EnableGroupMoveValidationLog = false;
    private const int GroupMoveStaggerMinGroundUnits = 12;
    private const int GroupMoveImmediatePathRequests = 8;
    private const int GroupMovePathRequestsPerFrame = 8;
    private EntityQuery _commandQueueQuery;
    private EntityQuery _selectedMoveQuery;
    private EntityQuery _gridConfigQuery;
    private EntityQuery _mapSurfaceQuery;
    private EntityTypeHandle _entityType;

    public readonly struct Result
    {
        public readonly TacticalCommandResult CommandResult;
        public readonly bool EmitScreenMarker;
        public readonly bool ShowWorldMarkers;
        public readonly int2 MarkerCell;
        public readonly float3 MarkerPosition;
        public readonly byte MarkerFactionId;

        private Result(
            TacticalCommandResult commandResult,
            bool emitScreenMarker,
            bool showWorldMarkers,
            int2 markerCell,
            float3 markerPosition,
            byte markerFactionId)
        {
            CommandResult = commandResult;
            EmitScreenMarker = emitScreenMarker;
            ShowWorldMarkers = showWorldMarkers;
            MarkerCell = markerCell;
            MarkerPosition = markerPosition;
            MarkerFactionId = markerFactionId;
        }

        public static Result Success(int2 markerCell, float3 markerPosition, byte markerFactionId)
        {
            return new Result(TacticalCommandResult.Success(), true, true, markerCell, markerPosition, markerFactionId);
        }

        public static Result Rejected(TacticalCommandReasonCode reasonCode)
        {
            return new Result(TacticalCommandResult.Rejected(reasonCode), false, false, default, default, 0);
        }
    }

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>(),
            ComponentType.ReadWrite<RtsSelectionCommandResultElement>());
        _selectedMoveQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
        _gridConfigQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
        _mapSurfaceQuery = state.GetEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        _entityType = state.GetEntityTypeHandle();
    }

    public void OnUpdate(ref SystemState state)
    {
        _entityType.Update(ref state);
        ProcessPreResolvedMoveRequests(
            state.EntityManager,
            _commandQueueQuery,
            _selectedMoveQuery,
            _gridConfigQuery,
            _mapSurfaceQuery,
            _entityType);
    }

    public Result TryIssueMoveOrder(
        EntityManager em,
        Vector2 screenPosition,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        UnitMoveOrderSystem moveOrderSystem,
        ClickedUnitResolver tryGetClickedUnit,
        ClickedCellResolver tryGetClickedCell,
        int currentFrame,
        IReadOnlyList<Entity> cachedSelectedMoveEntities = null)
    {
        if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
            SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace($"selectedMoveStart frame={currentFrame} screen={screenPosition}");
        using NativeList<Entity> selectedEntities = new(Allocator.Temp);
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        CollectSelectedMoveEntities(em, selectedMoveQuery, entityType, cachedSelectedMoveEntities, selectedEntities);
        NativeArray<Entity> entities = selectedEntities.AsArray();
        if (entities.Length == 0)
        {
            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace($"selectedMoveRejected reason=NoSelection screen={screenPosition}");
            return Result.Rejected(TacticalCommandReasonCode.NoSelection);
        }

        if (tryGetClickedCell == null || !tryGetClickedCell(screenPosition, em, out int2 goal, out Vector3 clickWorldPoint))
        {
            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace($"selectedMoveRejected reason=NoClickedCell screen={screenPosition} selected={entities.Length}");
            return Result.Rejected(TacticalCommandReasonCode.TargetBlocked);
        }

        if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                $"selectedMoveTarget screen={screenPosition} desiredGoal={goal} clickWorld={clickWorldPoint} " +
                $"selected={entities.Length} first={DescribeMoveEntity(em, entities[0])}");
        }

        byte factionId = 0;
        if (em.HasComponent<Faction>(entities[0]))
            factionId = em.GetComponentData<Faction>(entities[0]).Id;

        // Command marker projection is owned by command-result flushing after move validation accepts.
        return TryIssueMoveOrderToCell(
            em,
            entities,
            gridConfigQuery,
            mapSurfaceQuery,
            moveOrderSystem,
            goal,
            clickWorldPoint,
            currentFrame,
            factionId);
    }

    public Result TryIssueMoveOrderToCell(
        EntityManager em,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        UnitMoveOrderSystem moveOrderSystem,
        int2 goal,
        Vector3 clickWorldPoint,
        int currentFrame,
        IReadOnlyList<Entity> cachedSelectedMoveEntities = null)
    {
        using NativeList<Entity> selectedEntities = new(Allocator.Temp);
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        CollectSelectedMoveEntities(em, selectedMoveQuery, entityType, cachedSelectedMoveEntities, selectedEntities);
        NativeArray<Entity> entities = selectedEntities.AsArray();
        if (entities.Length == 0)
            return Result.Rejected(TacticalCommandReasonCode.NoSelection);

        byte factionId = 0;
        if (em.HasComponent<Faction>(entities[0]))
            factionId = em.GetComponentData<Faction>(entities[0]).Id;

        return TryIssueMoveOrderToCell(
            em,
            entities,
            gridConfigQuery,
            mapSurfaceQuery,
            moveOrderSystem,
            goal,
            clickWorldPoint,
            currentFrame,
            factionId);
    }

    private static Result TryIssueMoveOrderToCell(
        EntityManager em,
        NativeArray<Entity> entities,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        UnitMoveOrderSystem moveOrderSystem,
        int2 goal,
        Vector3 clickWorldPoint,
        int currentFrame,
        byte factionId)
    {
        if (gridConfigQuery.IsEmptyIgnoreFilter)
            return Result.Rejected(TacticalCommandReasonCode.TargetBlocked);

        Entity gridEntity = gridConfigQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        NativeBitArray blocked = blockerData.Blocked;
        NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
        MapSurfacePathfindingSnapshot surfaceReadSystem = new();
        MapSurfacePathfindingSnapshot.Context surfaceContext =
            surfaceReadSystem.TryCreateContext(em, mapSurfaceQuery, out MapSurfacePathfindingSnapshot.Context resolvedSurfaceContext)
                ? resolvedSurfaceContext
                : surfaceReadSystem.CreateFlatFallbackContext();
        var reservedGoalCells = new HashSet<int>();
        HashSet<int> selectedCurrentCells = moveOrderSystem.BuildSelectedCurrentFootprintCells(em, grid, entities);
        var issuedGoals = new int2[entities.Length];
        var skipIssue = new bool[entities.Length];
        bool issuedMoveOrder = false;
        int pathRequestCount = 0;
        int staggeredPathRequestCount = 0;
        int maxStaggerDelayFrames = 0;
        int skippedAlreadyMovingCount = 0;
        int airUnitCount = 0;
        int structuralAdds = 0;
        int structuralRemoves = 0;
        int uniqueGoalCount = 0;
        int groundPathCandidateCount = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            int2 issuedGoal = moveOrderSystem.FindManualMoveGoal(
                em,
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                reservedGoalCells,
                selectedCurrentCells,
                entity,
                goal,
                i,
                surfaceContext);
            issuedGoals[i] = issuedGoal;
            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace && i < 12)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"selectedMoveCandidate index={i} entity={DescribeMoveEntity(em, entity)} " +
                    $"desiredGoal={goal} issuedGoal={issuedGoal} selectedCurrent={ResolveUnitCell(em, entity)}");
            }

            if (IsAlreadyMovingToGoal(em, entity, issuedGoal))
            {
                skipIssue[i] = true;
                skippedAlreadyMovingCount++;
                if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace && i < 12)
                    SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace($"selectedMoveSkip index={i} reason=AlreadyMoving issuedGoal={issuedGoal} entity={DescribeMoveEntity(em, entity)}");
            }
            else if (!em.HasComponent<UnitAirMovement>(entity))
            {
                groundPathCandidateCount++;
            }
        }

        bool staggerGroundPathRequests = groundPathCandidateCount >= GroupMoveStaggerMinGroundUnits;
        int immediateGroundPathRequests = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            if (skipIssue[i])
                continue;

            Entity entity = entities[i];
            int2 issuedGoal = issuedGoals[i];

            bool groundUnit = !em.HasComponent<UnitAirMovement>(entity);
            bool issuePathNow = groundUnit &&
                                (!staggerGroundPathRequests ||
                                 immediateGroundPathRequests < GroupMoveImmediatePathRequests);
            int resumeFrame = groundUnit && !issuePathNow
                ? currentFrame + 1 + (staggeredPathRequestCount / GroupMovePathRequestsPerFrame)
                : 0;

            int moveRequestId = UnitMoveOrderRequestSystem.EnqueueGroupedManualMoveOrder(
                em,
                entity,
                issuedGoal,
                issuePathNow,
                groundUnit && !issuePathNow,
                resumeFrame,
                currentFrame);
            UnitMoveOrderRequestSystem.ProcessPendingRequests(em);
            UnitMoveOrderSystem.MoveOrderCommandResult commandResult =
                UnitMoveOrderRequestSystem.TryGetResult(em, moveRequestId, out UnitMoveOrderResultElement moveOrderResult)
                    ? ToMoveOrderCommandResult(moveOrderResult)
                    : default;
            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace && i < 12)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"selectedMoveIssued index={i} entity={DescribeMoveEntity(em, entity)} " +
                    $"issuedGoal={issuedGoal} issuePathNow={issuePathNow} resumeFrame={resumeFrame} " +
                    $"targetNow={(em.HasComponent<UnitTarget>(entity) ? em.GetComponentData<UnitTarget>(entity).Cell.ToString() : "none")} " +
                    $"pathRequestNow={(em.HasComponent<UnitPathRequest>(entity) ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString() : "none")}");
            }

            structuralAdds += commandResult.StructuralAdds;
            structuralRemoves += commandResult.StructuralRemoves;
            pathRequestCount += commandResult.PathRequests;
            staggeredPathRequestCount += commandResult.StaggeredPathRequests;
            maxStaggerDelayFrames = math.max(maxStaggerDelayFrames, commandResult.MaxStaggerDelayFrames);
            airUnitCount += commandResult.AirUnits;
            if (commandResult.PathRequests > 0)
                immediateGroundPathRequests += commandResult.PathRequests;

            issuedMoveOrder = true;
            uniqueGoalCount++;
        }

        if (!issuedMoveOrder)
        {
            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"selectedMoveRejected reason=TargetBlocked selected={entities.Length} desiredGoal={goal} " +
                    $"skippedAlreadyMoving={skippedAlreadyMovingCount} groundCandidates={groundPathCandidateCount}");
            }

            return Result.Rejected(TacticalCommandReasonCode.TargetBlocked);
        }

        if (EnableGroupMoveValidationLog && entities.Length > 1)
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogSelectionClickDebug(
                $"[GroupMoveValidate] selected={entities.Length} ground={groundPathCandidateCount} immediate={pathRequestCount} " +
                $"staggered={staggeredPathRequestCount} perFrame={GroupMovePathRequestsPerFrame} maxDelayFrames={maxStaggerDelayFrames} " +
                $"uniqueGoals={uniqueGoalCount} skippedSameGoal={skippedAlreadyMovingCount} air={airUnitCount} goal={goal}");
        }

        if (EnableMoveOrderDiagnostics && entities.Length > 1)
            SelectionRuntimeDiagnosticsSystemHelper.LogSelectionClickDebug(
                $"[MoveOrderDiag] frame={currentFrame} selected={entities.Length} pathRequests={pathRequestCount} " +
                $"airUnits={airUnitCount} skippedSameGoal={skippedAlreadyMovingCount} structuralAdds={structuralAdds} structuralRemoves={structuralRemoves} " +
                $"uniqueGoals={uniqueGoalCount} staggeredPathRequests={staggeredPathRequestCount} goal={goal}");

        if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                $"selectedMoveSuccess frame={currentFrame} selected={entities.Length} desiredGoal={goal} " +
                $"pathRequests={pathRequestCount} staggeredPathRequests={staggeredPathRequestCount} skippedAlreadyMoving={skippedAlreadyMovingCount} " +
                $"groundCandidates={groundPathCandidateCount} structuralAdds={structuralAdds} structuralRemoves={structuralRemoves}");
        }

        return Result.Success(goal, clickWorldPoint, factionId);
    }

    private static UnitMoveOrderSystem.MoveOrderCommandResult ToMoveOrderCommandResult(UnitMoveOrderResultElement result)
    {
        return new UnitMoveOrderSystem.MoveOrderCommandResult
        {
            Issued = result.Issued != 0,
            StructuralAdds = result.StructuralAdds,
            StructuralRemoves = result.StructuralRemoves,
            PathRequests = result.PathRequests,
            StaggeredPathRequests = result.StaggeredPathRequests,
            MaxStaggerDelayFrames = result.MaxStaggerDelayFrames,
            AirUnits = result.AirUnits
        };
    }

    public bool ProcessCommandIntentRequests(
        EntityManager em,
        Entity commandEntity,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        IReadOnlyList<Entity> cachedSelectedMoveEntities,
        UnitMoveOrderSystem moveOrderSystem,
        ClickedUnitResolver tryGetClickedUnit,
        ClickedCellResolver tryGetClickedCell)
    {
        return TryGetCommandBuffers(
                   em,
                   commandEntity,
                   out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
                   out DynamicBuffer<RtsSelectionCommandResultElement> commandResults) &&
               ProcessCommandIntentRequests(
                   em,
                   commandEntity,
                   commandRequests,
                   commandResults,
                   selectedMoveQuery,
                   gridConfigQuery,
                   mapSurfaceQuery,
                   cachedSelectedMoveEntities,
                   moveOrderSystem,
                   tryGetClickedUnit,
                   tryGetClickedCell);
    }

    public bool ProcessCommandIntentRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        IReadOnlyList<Entity> cachedSelectedMoveEntities,
        UnitMoveOrderSystem moveOrderSystem,
        ClickedUnitResolver tryGetClickedUnit,
        ClickedCellResolver tryGetClickedCell)
    {
        _ = commandResults;
        using NativeList<RtsSelectionCommandIntentRequestElement> pendingRequests = new(Allocator.Temp);
        int pendingMoveRequestCount = RemovePendingMoveRequests(
            commandRequests,
            pendingRequests,
            SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace);
        if (pendingRequests.Length == 0)
            return false;

        using NativeArray<RtsSelectionCommandIntentRequestElement> pendingRequestArray =
            CopyRequestsToIndependentArray(pendingRequests);
        for (int i = 0; i < pendingRequestArray.Length; i++)
        {
            RtsSelectionCommandIntentRequestElement request = pendingRequestArray[i];
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"requestProcess requestId={request.RequestId} requestFrame={request.Frame} " +
                    $"screen={screenPosition} pendingCount={pendingMoveRequestCount}");
            }

            Result result = TryIssueMoveOrder(
                em,
                screenPosition,
                selectedMoveQuery,
                gridConfigQuery,
                mapSurfaceQuery,
                moveOrderSystem,
                tryGetClickedUnit,
                tryGetClickedCell,
                request.Frame,
                cachedSelectedMoveEntities);

            if (SelectionRuntimeDiagnosticsSystemHelper.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystemHelper.LogMoveCommandTrace(
                    $"requestResult requestId={request.RequestId} accepted={result.CommandResult.Accepted} " +
                    $"reason={result.CommandResult.ReasonCode} emitMarker={result.EmitScreenMarker} showWorldMarkers={result.ShowWorldMarkers}");
            }

            AddCommandResult(em, commandEntity, ToCommandResultElement(request, result));
        }

        return true;
    }

    private static bool TryGetCommandBuffers(
        EntityManager em,
        Entity commandEntity,
        out DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        out DynamicBuffer<RtsSelectionCommandResultElement> commandResults)
    {
        commandRequests = default;
        commandResults = default;
        if (commandEntity == Entity.Null ||
            !em.Exists(commandEntity) ||
            !em.HasBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity) ||
            !em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
        {
            return false;
        }

        commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        return true;
    }

    private static bool ProcessPreResolvedMoveRequests(
        EntityManager em,
        EntityQuery commandQueueQuery,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        EntityTypeHandle entityType)
    {
        if (commandQueueQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        using NativeList<RtsSelectionCommandIntentRequestElement> pendingRequests = new(Allocator.Temp);
        RemovePendingPreResolvedMoveRequests(commandRequests, pendingRequests);
        if (pendingRequests.Length == 0)
            return false;

        using NativeArray<RtsSelectionCommandIntentRequestElement> pendingRequestArray =
            CopyRequestsToIndependentArray(pendingRequests);
        var moveOrderSystem = new UnitMoveOrderSystem();
        for (int i = 0; i < pendingRequestArray.Length; i++)
        {
            RtsSelectionCommandIntentRequestElement request = pendingRequestArray[i];
            using NativeList<Entity> selectedEntities = new(Allocator.Temp);
            CollectSelectedMoveEntities(em, selectedMoveQuery, entityType, null, selectedEntities);
            NativeArray<Entity> entities = selectedEntities.AsArray();
            Vector3 worldPoint = new(request.WorldPosition.x, request.WorldPosition.y, request.WorldPosition.z);
            Result result = entities.Length == 0
                ? Result.Rejected(TacticalCommandReasonCode.NoSelection)
                : TryIssueMoveOrderToCell(
                    em,
                    entities,
                    gridConfigQuery,
                    mapSurfaceQuery,
                    moveOrderSystem,
                    request.TargetCell,
                    worldPoint,
                    request.Frame,
                    ResolveMarkerFaction(em, entities));
            AddCommandResult(em, commandEntity, ToCommandResultElement(request, result));
        }

        return true;
    }

    private static int RemovePendingMoveRequests(
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        NativeList<RtsSelectionCommandIntentRequestElement> pendingRequests,
        bool countMoveRequests)
    {
        int pendingMoveRequestCount = 0;
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind == RtsSelectionCommandIntentKind.Move && countMoveRequests)
                pendingMoveRequestCount++;

            if (request.Kind != RtsSelectionCommandIntentKind.Move ||
                IsPreResolvedMoveRequest(request))
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            pendingRequests.Add(request);
        }

        return pendingMoveRequestCount;
    }

    private static void RemovePendingPreResolvedMoveRequests(
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        NativeList<RtsSelectionCommandIntentRequestElement> pendingRequests)
    {
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Move ||
                request.HasTargetCell == 0 ||
                request.HasWorldPosition == 0)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            pendingRequests.Add(request);
        }
    }

    private static NativeArray<RtsSelectionCommandIntentRequestElement> CopyRequestsToIndependentArray(
        NativeList<RtsSelectionCommandIntentRequestElement> pendingRequests)
    {
        NativeArray<RtsSelectionCommandIntentRequestElement> source = pendingRequests.AsArray();
        NativeArray<RtsSelectionCommandIntentRequestElement> copy = new(source.Length, Allocator.Temp);
        for (int i = 0; i < source.Length; i++)
            copy[i] = source[i];
        return copy;
    }

    private static bool IsPreResolvedMoveRequest(RtsSelectionCommandIntentRequestElement request)
    {
        return request.HasTargetCell != 0 && request.HasWorldPosition != 0;
    }

    private static byte ResolveMarkerFaction(EntityManager em, NativeArray<Entity> entities)
    {
        if (entities.Length == 0 || !em.HasComponent<Faction>(entities[0]))
            return 0;

        return em.GetComponentData<Faction>(entities[0]).Id;
    }

    private static void AddCommandResult(
        EntityManager em,
        Entity commandEntity,
        RtsSelectionCommandResultElement result)
    {
        if (commandEntity != Entity.Null && em.Exists(commandEntity) && em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
        {
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Add(result);
        }
    }

    private static RtsSelectionCommandResultElement ToCommandResultElement(
        RtsSelectionCommandIntentRequestElement request,
        Result result)
    {
        TacticalCommandResult commandResult = result.CommandResult;
        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            ScreenPosition = request.ScreenPosition,
            TargetCell = result.MarkerCell,
            WorldPosition = result.MarkerPosition,
            TargetKind = commandResult.Accepted ? RtsSelectionCommandTargetKind.Cell : RtsSelectionCommandTargetKind.None,
            CommandMode = (int)TacticalCommandMode.Move,
            HasCommandResult = 1,
            Accepted = commandResult.Accepted ? (byte)1 : (byte)0,
            ReasonCode = (int)commandResult.ReasonCode,
            FeedbackLifetime = RtsSelectionCommandFeedbackLifetime.Transient,
            EmitScreenMarker = result.EmitScreenMarker ? (byte)1 : (byte)0,
            MarkerFactionId = result.MarkerFactionId,
            HasTargetCell = commandResult.Accepted ? (byte)1 : (byte)0,
            HasWorldPosition = commandResult.Accepted ? (byte)1 : (byte)0,
            ShowWorldMarkers = result.ShowWorldMarkers ? (byte)1 : (byte)0
        };
    }

    private static string DescribeMoveEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "null";

        string name = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        byte faction = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
        string gridCell = em.HasComponent<UnitGrid>(entity) ? em.GetComponentData<UnitGrid>(entity).Cell.ToString() : "none";
        string targetCell = em.HasComponent<UnitTarget>(entity) ? em.GetComponentData<UnitTarget>(entity).Cell.ToString() : "none";
        string pathGoal = em.HasComponent<UnitPathRequest>(entity) ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString() : "none";
        int2 footprintSize = em.HasComponent<UnitFootprint>(entity) ? em.GetComponentData<UnitFootprint>(entity).Size : new int2(1, 1);
        UnitMovementBehavior movementBehavior = em.HasComponent<UnitMovementBehavior>(entity)
            ? em.GetComponentData<UnitMovementBehavior>(entity)
            : default;
        bool isVehicle = UnitVehicleMovementUtility.IsVehicle(new UnitFootprint { Size = footprintSize }, movementBehavior);
        bool selected = em.HasComponent<SelectedUnitTag>(entity);
        bool move = em.HasComponent<UnitMove>(entity);
        bool pathFollow = em.HasComponent<UnitPathFollow>(entity);
        bool manual = em.HasComponent<ManualMoveOrderTag>(entity);
        bool longMove = em.HasComponent<UnitLongDistanceMove>(entity);
        bool disabled = em.HasComponent<Disabled>(entity);
        return $"{entity}/{name}/faction={faction}/selected={selected}/move={move}/vehicle={isVehicle}/footprint={footprintSize}/grid={gridCell}/target={targetCell}/pathRequest={pathGoal}/pathFollow={pathFollow}/manual={manual}/longMove={longMove}/disabled={disabled}";
    }

    private static string ResolveUnitCell(EntityManager em, Entity entity)
    {
        return entity != Entity.Null && em.Exists(entity) && em.HasComponent<UnitGrid>(entity)
            ? em.GetComponentData<UnitGrid>(entity).Cell.ToString()
            : "none";
    }

    private static void CollectSelectedMoveEntities(
        EntityManager em,
        EntityQuery selectedMoveQuery,
        EntityTypeHandle entityType,
        IReadOnlyList<Entity> cachedSelectedMoveEntities,
        NativeList<Entity> selectedEntities)
    {
        selectedEntities.Clear();
        if (cachedSelectedMoveEntities != null && cachedSelectedMoveEntities.Count > 0)
        {
            for (int i = 0; i < cachedSelectedMoveEntities.Count; i++)
            {
                Entity entity = cachedSelectedMoveEntities[i];
                if (SelectionStateSystem.IsCacheableSelectedMoveEntity(em, entity) &&
                    em.HasComponent<SelectedUnitTag>(entity))
                {
                    selectedEntities.Add(entity);
                }
            }

            if (selectedEntities.Length > 0)
                return;
        }

        int count = selectedMoveQuery.CalculateEntityCount();
        if (count <= 0)
            return;

        using NativeArray<ArchetypeChunk> chunks = selectedMoveQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> chunkEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < chunkEntities.Length; i++)
                selectedEntities.Add(chunkEntities[i]);
        }
    }

    private static bool IsAlreadyMovingToGoal(EntityManager em, Entity entity, int2 goal)
    {
        if (!em.Exists(entity))
            return false;

        bool sameTarget =
            em.HasComponent<UnitTarget>(entity) &&
            em.GetComponentData<UnitTarget>(entity).Cell.Equals(goal);
        bool samePendingRequest =
            em.HasComponent<UnitPathRequest>(entity) &&
            em.GetComponentData<UnitPathRequest>(entity).Goal.Equals(goal);
        bool hasActiveMovement =
            em.HasComponent<UnitPathFollow>(entity) ||
            em.HasComponent<UnitPathRequest>(entity);

        return sameTarget && (samePendingRequest || hasActiveMovement);
    }
}
