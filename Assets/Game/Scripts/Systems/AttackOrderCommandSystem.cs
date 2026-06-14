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

    public bool ProcessCommandIntentRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
        UnitTargetOrderSystem targetOrderSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext)
    {
        bool handledAny = false;
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Attack)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            handledAny = true;
            Vector2 screenPosition = new(request.ScreenPosition.x, request.ScreenPosition.y);
            Result result = TryIssueAttackOrderToClickedUnit(
                em,
                screenPosition,
                targetOrderSystem,
                tryGetClickedUnitEntity,
                collectSelectedAttackSources,
                buildingPlacementInteractionSystem,
                buildingPlacementInteractionContext,
                request.ExplicitAttackTargetMode != 0);

            AddCommandResult(em, commandEntity, commandResults, ToCommandResultElement(request, result));
        }

        return handledAny;
    }

    public Result IssueAttackTarget(
        EntityManager em,
        Entity targetEntity,
        UnitTargetOrderSystem targetOrderSystem,
        UnitTargetOrderSystem.TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget = null,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources = null)
    {
        EnsureEntityQueries(em);
        using NativeList<Entity> selectedEntities = CreateSelectedAttackSourceList(em, collectSelectedAttackSources);
        UnitTargetOrderSystem.AttackOrderIssueResult issueResult =
            targetOrderSystem.IssueAttackTarget(em, selectedEntities.AsArray(), targetEntity, tryResolveBaseBreachTarget);
        return issueResult.CommandResult.Accepted
            ? Result.Accepted(issueResult)
            : Result.Rejected(issueResult.CommandResult);
    }

    private static void AddCommandResult(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandResultElement> fallbackResults,
        RtsSelectionCommandResultElement result)
    {
        if (commandEntity != Entity.Null && em.Exists(commandEntity) && em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
        {
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity).Add(result);
            return;
        }

        fallbackResults.Add(result);
    }

    private static RtsSelectionCommandResultElement ToCommandResultElement(
        RtsSelectionCommandIntentRequestElement request,
        Result result)
    {
        TacticalCommandResult commandResult = result.HasCommandResult
            ? result.CommandResult
            : default;
        return new RtsSelectionCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Frame = request.Frame,
            TargetEntity = result.TargetEntity,
            ScreenPosition = request.ScreenPosition,
            WorldPosition = result.TargetPosition,
            TargetKind = result.TargetEntity != Entity.Null
                ? RtsSelectionCommandTargetKind.Entity
                : result.Issued
                    ? RtsSelectionCommandTargetKind.WorldPosition
                    : RtsSelectionCommandTargetKind.None,
            CommandMode = (int)TacticalCommandMode.Attack,
            HasCommandResult = result.HasCommandResult ? (byte)1 : (byte)0,
            Accepted = result.Issued ? (byte)1 : (byte)0,
            ReasonCode = result.HasCommandResult ? (int)commandResult.ReasonCode : 0,
            FeedbackLifetime = result.HasCommandResult
                ? RtsSelectionCommandFeedbackLifetime.Transient
                : RtsSelectionCommandFeedbackLifetime.Hidden,
            Message = result.HasCommandResult ? new FixedString64Bytes(commandResult.Message ?? string.Empty) : default,
            EmitScreenMarker = result.Issued ? (byte)1 : (byte)0,
            HasTargetEntity = result.Issued && result.TargetEntity != Entity.Null ? (byte)1 : (byte)0,
            HasWorldPosition = result.Issued ? (byte)1 : (byte)0,
            ShowWorldMarkers = result.Issued ? (byte)1 : (byte)0
        };
    }

    private NativeList<Entity> CreateSelectedAttackSourceList(
        EntityManager em,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources)
    {
        _selectedAttackSourceScratch.Clear();
        collectSelectedAttackSources?.Invoke(em, _selectedAttackSourceScratch);
        if (_selectedAttackSourceScratch.Count > 0)
        {
            var selectedEntities = new NativeList<Entity>(_selectedAttackSourceScratch.Count, Allocator.Temp);
            for (int i = 0; i < _selectedAttackSourceScratch.Count; i++)
                selectedEntities.Add(_selectedAttackSourceScratch[i]);
            return selectedEntities;
        }

        return CollectSelectedAttackSourceEntities(em);
    }

    private NativeList<Entity> CollectSelectedAttackSourceEntities(EntityManager em)
    {
        var selectedEntities = new NativeList<Entity>(_selectedAttackQuery.CalculateEntityCount(), Allocator.Temp);
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = _selectedAttackQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> chunkEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < chunkEntities.Length; i++)
                selectedEntities.Add(chunkEntities[i]);
        }

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
