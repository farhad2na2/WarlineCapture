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
        UnitMoveOrderSystem moveOrderSystem,
        SelectionOrderMarkerSystem orderMarkerSystem,
        ClickedUnitResolver tryGetClickedUnit,
        ClickedCellResolver tryGetClickedCell,
        int currentFrame)
    {
        if (tryGetClickedUnit != null && tryGetClickedUnit(screenPosition, em, out _))
            return Result.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        using var entities = selectedMoveQuery.ToEntityArray(Allocator.Temp);
        if (entities.Length == 0)
            return Result.Rejected(TacticalCommandReasonCode.NoSelection);

        if (tryGetClickedCell == null || !tryGetClickedCell(screenPosition, em, out int2 goal, out Vector3 clickWorldPoint))
            return Result.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        byte factionId = 0;
        if (em.HasComponent<Faction>(entities[0]))
            factionId = em.GetComponentData<Faction>(entities[0]).Id;
        orderMarkerSystem.ShowMoveOrderMarker(em, goal, clickWorldPoint, factionId);

        Entity gridEntity = gridConfigQuery.GetSingletonEntity();
        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable = em.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        DynamicBlockerData blockerData = em.GetComponentData<DynamicBlockerData>(gridEntity);
        NativeBitArray blocked = blockerData.Blocked;
        NativeArray<byte> friendlyPassFactionIds = blockerData.FriendlyPassFactionIds;
        NativeBitArray occupied = em.GetComponentData<DynamicOccupancyData>(gridEntity).Occupied;
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
                i);
            issuedGoals[i] = issuedGoal;

            if (IsAlreadyMovingToGoal(em, entity, issuedGoal))
            {
                skipIssue[i] = true;
                skippedAlreadyMovingCount++;
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
            return Result.Rejected(TacticalCommandReasonCode.TargetBlocked);

        if (EnableGroupMoveValidationLog && entities.Length > 1)
        {
            Debug.Log(
                $"[GroupMoveValidate] selected={entities.Length} ground={groundPathCandidateCount} immediate={pathRequestCount} " +
                $"staggered={staggeredPathRequestCount} perFrame={GroupMovePathRequestsPerFrame} maxDelayFrames={maxStaggerDelayFrames} " +
                $"uniqueGoals={uniqueGoalCount} skippedSameGoal={skippedAlreadyMovingCount} air={airUnitCount} goal={goal}");
        }

        if (EnableMoveOrderDiagnostics && entities.Length > 1)
            Debug.Log(
                $"[MoveOrderDiag] frame={currentFrame} selected={entities.Length} pathRequests={pathRequestCount} " +
                $"airUnits={airUnitCount} skippedSameGoal={skippedAlreadyMovingCount} structuralAdds={structuralAdds} structuralRemoves={structuralRemoves} " +
                $"uniqueGoals={uniqueGoalCount} staggeredPathRequests={staggeredPathRequestCount} goal={goal}");

        return Result.Success();
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
