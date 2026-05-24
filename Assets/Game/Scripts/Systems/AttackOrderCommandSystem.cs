using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class AttackOrderCommandSystem
{
    public delegate bool TryGetClickedUnitEntityDelegate(Vector2 screenPosition, EntityManager em, out Entity entity);

    public readonly struct Result
    {
        public readonly bool Issued;
        public readonly bool HasCommandResult;
        public readonly TacticalCommandResult CommandResult;
        public readonly float3 TargetPosition;

        private Result(bool issued, bool hasCommandResult, TacticalCommandResult commandResult, float3 targetPosition)
        {
            Issued = issued;
            HasCommandResult = hasCommandResult;
            CommandResult = commandResult;
            TargetPosition = targetPosition;
        }

        public static Result NoCommand()
        {
            return new Result(false, false, default, default);
        }

        public static Result Rejected(TacticalCommandResult commandResult)
        {
            return new Result(false, true, commandResult, default);
        }

        public static Result Accepted(UnitTargetOrderSystem.AttackOrderIssueResult issueResult)
        {
            return new Result(true, true, issueResult.CommandResult, issueResult.TargetPosition);
        }
    }

    private World _queryWorld;
    private EntityQuery _selectedAttackQuery;
    private BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _selectedAttackQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitCombat>(),
            ComponentType.ReadOnly<UnitAttack>(),
            ComponentType.ReadOnly<LocalTransform>());
    }

    public Result TryIssueAttackOrderToClickedUnit(
        EntityManager em,
        Vector2 screenPosition,
        UnitTargetOrderSystem targetOrderSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        bool explicitAttackTargetModeActive)
    {
        EnsureEntityQueries(em);
        _buildingPlacementInteractionSystem = buildingPlacementInteractionSystem;
        _buildingPlacementInteractionContext = buildingPlacementInteractionContext;
        if (!tryGetClickedUnitEntity(screenPosition, em, out Entity targetEntity))
        {
            return explicitAttackTargetModeActive
                ? Result.Rejected(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable))
                : Result.NoCommand();
        }

        TacticalCommandResult targetValidation = targetOrderSystem.ValidateAttackTarget(em, targetEntity);
        if (!targetValidation.Accepted)
            return explicitAttackTargetModeActive ? Result.Rejected(targetValidation) : Result.NoCommand();

        return IssueAttackTarget(em, targetEntity, targetOrderSystem, TryResolveBaseBreachTargetForAttackOrder);
    }

    public Result IssueAttackTarget(
        EntityManager em,
        Entity targetEntity,
        UnitTargetOrderSystem targetOrderSystem,
        UnitTargetOrderSystem.TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget = null)
    {
        EnsureEntityQueries(em);
        using NativeArray<Entity> selectedEntities = _selectedAttackQuery.ToEntityArray(Allocator.Temp);
        UnitTargetOrderSystem.AttackOrderIssueResult issueResult =
            targetOrderSystem.IssueAttackTarget(em, selectedEntities, targetEntity, tryResolveBaseBreachTarget);
        return issueResult.CommandResult.Accepted
            ? Result.Accepted(issueResult)
            : Result.Rejected(issueResult.CommandResult);
    }

    private bool TryResolveBaseBreachTargetForAttackOrder(
        byte factionId,
        Entity targetEntity,
        int2 targetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition)
    {
        breachTarget = Entity.Null;
        breachCell = default;
        breachPosition = default;
        return _buildingPlacementInteractionSystem != null &&
               _buildingPlacementInteractionSystem.TryResolveBaseBreachTarget(
                   _buildingPlacementInteractionContext,
                   factionId,
                   targetEntity,
                   targetCell,
                   attackerCell,
                   out breachTarget,
                   out breachCell,
                   out breachPosition,
                   out _);
    }
}
