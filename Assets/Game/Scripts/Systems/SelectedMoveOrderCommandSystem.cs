using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class SelectedMoveOrderCommandSystem
{
    public delegate bool ClickedUnitResolver(Vector2 screenPosition, EntityManager em, out Entity entity);
    public delegate bool ClickedCellResolver(Vector2 screenPosition, EntityManager em, out int2 cell, out Vector3 worldPoint);

    private const bool EnableMoveOrderDiagnostics = false;
    private static readonly bool EnableGroupMoveValidationLog = false;
    private const int GroupMoveStaggerMinGroundUnits = 12;
    private const int GroupMoveImmediatePathRequests = 8;
    private const int GroupMovePathRequestsPerFrame = 8;

    public readonly struct Result
    {
        public readonly TacticalCommandResult CommandResult;
        public readonly bool EmitScreenMarker;
        public readonly bool ShowWorldMarkers;

        private Result(TacticalCommandResult commandResult, bool emitScreenMarker, bool showWorldMarkers)
        {
            CommandResult = commandResult;
            EmitScreenMarker = emitScreenMarker;
            ShowWorldMarkers = showWorldMarkers;
        }

        public static Result Success() => new(TacticalCommandResult.Success(), true, true);

        public static Result Rejected(TacticalCommandReasonCode reasonCode)
        {
            return new(TacticalCommandResult.Rejected(reasonCode), false, false);
        }
    }

    public Result TryIssueMoveOrder(
        EntityManager em,
        Vector2 screenPosition,
        EntityQuery selectedMoveQuery,
        EntityQuery gridConfigQuery,
        EntityQuery mapSurfaceQuery,
        UnitMoveOrderSystem moveOrderSystem,
        SelectionOrderMarkerSystem orderMarkerSystem,
        ClickedUnitResolver tryGetClickedUnit,
        ClickedCellResolver tryGetClickedCell,
        int currentFrame)
    {
        SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"selectedMoveStart frame={currentFrame} screen={screenPosition}");
        if (tryGetClickedUnit != null && tryGetClickedUnit(screenPosition, em, out Entity clickedUnit))
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"selectedMoveRejected reason=ClickedUnit screen={screenPosition} clicked={DescribeMoveEntity(em, clickedUnit)}");
            return Result.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        using NativeList<Entity> selectedEntities = CollectSelectedMoveEntities(em, selectedMoveQuery);
        NativeArray<Entity> entities = selectedEntities.AsArray();
        if (entities.Length == 0)
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"selectedMoveRejected reason=NoSelection screen={screenPosition}");
            return Result.Rejected(TacticalCommandReasonCode.NoSelection);
        }

        if (tryGetClickedCell == null || !tryGetClickedCell(screenPosition, em, out int2 goal, out Vector3 clickWorldPoint))
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"selectedMoveRejected reason=NoClickedCell screen={screenPosition} selected={entities.Length}");
            return Result.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
            $"selectedMoveTarget screen={screenPosition} desiredGoal={goal} clickWorld={clickWorldPoint} " +
            $"selected={entities.Length} first={DescribeMoveEntity(em, entities[0])}");

        byte factionId = 0;
        if (em.HasComponent<Faction>(entities[0]))
            factionId = em.GetComponentData<Faction>(entities[0]).Id;
        orderMarkerSystem.ShowMoveOrderMarker(em, goal, clickWorldPoint, factionId);

        Entity gridEntity = gridConfigQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        DynamicBlockerComponent blockerData = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        NativeBitArray blocked = blockerData.Blocked;
        NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied;
        MapSurfacePathfindingReadSystem surfaceReadSystem = new();
        MapSurfacePathfindingReadSystem.Context surfaceContext =
            surfaceReadSystem.TryCreateContext(em, mapSurfaceQuery, out MapSurfacePathfindingReadSystem.Context resolvedSurfaceContext)
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
            if (i < 12)
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"selectedMoveCandidate index={i} entity={DescribeMoveEntity(em, entity)} " +
                    $"desiredGoal={goal} issuedGoal={issuedGoal} selectedCurrent={ResolveUnitCell(em, entity)}");
            }

            if (IsAlreadyMovingToGoal(em, entity, issuedGoal))
            {
                skipIssue[i] = true;
                skippedAlreadyMovingCount++;
                if (i < 12)
                    SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"selectedMoveSkip index={i} reason=AlreadyMoving issuedGoal={issuedGoal} entity={DescribeMoveEntity(em, entity)}");
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

            UnitMoveOrderSystem.MoveOrderCommandResult commandResult = moveOrderSystem.IssueGroupedManualMoveOrder(
                em,
                entity,
                issuedGoal,
                issuePathNow,
                groundUnit && !issuePathNow,
                resumeFrame,
                currentFrame);
            if (i < 12)
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
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
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"selectedMoveRejected reason=TargetBlocked selected={entities.Length} desiredGoal={goal} " +
                $"skippedAlreadyMoving={skippedAlreadyMovingCount} groundCandidates={groundPathCandidateCount}");
            return Result.Rejected(TacticalCommandReasonCode.TargetBlocked);
        }

        if (EnableGroupMoveValidationLog && entities.Length > 1)
        {
            SelectionRuntimeDiagnosticsSystem.LogSelectionClickDebug(
                $"[GroupMoveValidate] selected={entities.Length} ground={groundPathCandidateCount} immediate={pathRequestCount} " +
                $"staggered={staggeredPathRequestCount} perFrame={GroupMovePathRequestsPerFrame} maxDelayFrames={maxStaggerDelayFrames} " +
                $"uniqueGoals={uniqueGoalCount} skippedSameGoal={skippedAlreadyMovingCount} air={airUnitCount} goal={goal}");
        }

        if (EnableMoveOrderDiagnostics && entities.Length > 1)
            SelectionRuntimeDiagnosticsSystem.LogSelectionClickDebug(
                $"[MoveOrderDiag] frame={currentFrame} selected={entities.Length} pathRequests={pathRequestCount} " +
                $"airUnits={airUnitCount} skippedSameGoal={skippedAlreadyMovingCount} structuralAdds={structuralAdds} structuralRemoves={structuralRemoves} " +
                $"uniqueGoals={uniqueGoalCount} staggeredPathRequests={staggeredPathRequestCount} goal={goal}");

        SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
            $"selectedMoveSuccess frame={currentFrame} selected={entities.Length} desiredGoal={goal} " +
            $"pathRequests={pathRequestCount} staggeredPathRequests={staggeredPathRequestCount} skippedAlreadyMoving={skippedAlreadyMovingCount} " +
            $"groundCandidates={groundPathCandidateCount} structuralAdds={structuralAdds} structuralRemoves={structuralRemoves}");
        return Result.Success();
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

    private static NativeList<Entity> CollectSelectedMoveEntities(EntityManager em, EntityQuery selectedMoveQuery)
    {
        int count = selectedMoveQuery.CalculateEntityCount();
        NativeList<Entity> entities = new(count, Allocator.Temp);
        if (count <= 0)
            return entities;

        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = selectedMoveQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> chunkEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < chunkEntities.Length; i++)
                entities.Add(chunkEntities[i]);
        }

        return entities;
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
