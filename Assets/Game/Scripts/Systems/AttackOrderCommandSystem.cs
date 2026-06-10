using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class AttackOrderCommandSystem
{
    public delegate bool TryGetClickedUnitEntityDelegate(Vector2 screenPosition, EntityManager em, out Entity entity);
    public delegate void CollectSelectedAttackSourcesDelegate(EntityManager em, List<Entity> sources);

    public readonly struct Result
    {
        public readonly bool Issued;
        public readonly bool HasCommandResult;
        public readonly TacticalCommandResult CommandResult;
        public readonly Entity TargetEntity;
        public readonly float3 TargetPosition;

        private Result(bool issued, bool hasCommandResult, TacticalCommandResult commandResult, Entity targetEntity, float3 targetPosition)
        {
            Issued = issued;
            HasCommandResult = hasCommandResult;
            CommandResult = commandResult;
            TargetEntity = targetEntity;
            TargetPosition = targetPosition;
        }

        public static Result NoCommand()
        {
            return new Result(false, false, default, Entity.Null, default);
        }

        public static Result Rejected(TacticalCommandResult commandResult)
        {
            return new Result(false, true, commandResult, Entity.Null, default);
        }

        public static Result Accepted(UnitTargetOrderSystem.AttackOrderIssueResult issueResult)
        {
            return new Result(true, true, issueResult.CommandResult, issueResult.TargetEntity, issueResult.TargetPosition);
        }
    }

    private World _queryWorld;
    private EntityQuery _selectedAttackQuery;
    private BuildingPlacementInteractionSystem _buildingPlacementInteractionSystem;
    private BuildingPlacementInteractionSystem.Context _buildingPlacementInteractionContext;
    private readonly List<Entity> _selectedAttackSourceScratch = new();

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
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources,
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

        return IssueAttackTarget(em, targetEntity, targetOrderSystem, TryResolveBaseBreachTargetForAttackOrder, collectSelectedAttackSources);
    }

    public Result IssueAttackTarget(
        EntityManager em,
        Entity targetEntity,
        UnitTargetOrderSystem targetOrderSystem,
        UnitTargetOrderSystem.TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget = null,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources = null)
    {
        EnsureEntityQueries(em);
        NativeArray<Entity> selectedEntities = CreateSelectedAttackSourceArray(em, collectSelectedAttackSources);
        try
        {
            UnitTargetOrderSystem.AttackOrderIssueResult issueResult =
                targetOrderSystem.IssueAttackTarget(em, selectedEntities, targetEntity, tryResolveBaseBreachTarget);
            return issueResult.CommandResult.Accepted
                ? Result.Accepted(issueResult)
                : Result.Rejected(issueResult.CommandResult);
        }
        finally
        {
            if (selectedEntities.IsCreated)
                selectedEntities.Dispose();
        }
    }

    private NativeArray<Entity> CreateSelectedAttackSourceArray(
        EntityManager em,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources)
    {
        _selectedAttackSourceScratch.Clear();
        collectSelectedAttackSources?.Invoke(em, _selectedAttackSourceScratch);
        if (_selectedAttackSourceScratch.Count == 0)
            return _selectedAttackQuery.ToEntityArray(Allocator.Temp);

        var selectedEntities = new NativeArray<Entity>(_selectedAttackSourceScratch.Count, Allocator.Temp);
        for (int i = 0; i < _selectedAttackSourceScratch.Count; i++)
            selectedEntities[i] = _selectedAttackSourceScratch[i];
        return selectedEntities;
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
