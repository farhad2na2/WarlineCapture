using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct AttackOrderCommandSystem : ISystem
{
    public delegate bool TryGetClickedUnitEntityDelegate(Vector2 screenPosition, EntityManager em, out Entity entity);
    public delegate void CollectSelectedAttackSourcesDelegate(EntityManager em, List<Entity> sources);

    private EntityQuery _commandQueueQuery;
    private EntityQuery _selectedAttackQuery;
    private EntityTypeHandle _entityType;

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

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>(),
            ComponentType.ReadWrite<RtsSelectionCommandResultElement>());
        _selectedAttackQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitCombat>(),
            ComponentType.ReadOnly<UnitAttack>(),
            ComponentType.ReadOnly<LocalTransform>());
        _entityType = state.GetEntityTypeHandle();
    }

    public void OnUpdate(ref SystemState state)
    {
        _entityType.Update(ref state);
        ProcessPreResolvedAttackRequests(
            state.EntityManager,
            _commandQueueQuery,
            _selectedAttackQuery,
            _entityType);
    }

    public void EnsureEntityQueries(EntityManager em)
    {
        // Kept for managed transition callers while the command pipeline is moving to ECS-owned updates.
    }

    public Result TryIssueAttackOrderToClickedUnit(
        EntityManager em,
        Vector2 screenPosition,
        UnitTargetOrderSystem targetOrderSystem,
        TryGetClickedUnitEntityDelegate tryGetClickedUnitEntity,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources,
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        bool explicitAttackTargetModeActive,
        List<Entity> selectedAttackSourceScratch = null)
    {
        if (!tryGetClickedUnitEntity(screenPosition, em, out Entity targetEntity))
        {
            return explicitAttackTargetModeActive
                ? Result.Rejected(TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable))
                : Result.NoCommand();
        }

        TacticalCommandResult targetValidation = targetOrderSystem.ValidateAttackTarget(em, targetEntity);
        if (!targetValidation.Accepted)
            return explicitAttackTargetModeActive ? Result.Rejected(targetValidation) : Result.NoCommand();

        UnitTargetOrderSystem.TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget = null;
        if (buildingPlacementInteractionSystem != null)
        {
            tryResolveBaseBreachTarget = (
                byte factionId,
                Entity requestedTarget,
                int2 targetCell,
                int2 attackerCell,
                out Entity breachTarget,
                out int2 breachCell,
                out float3 breachPosition) =>
                TryResolveBaseBreachTargetForAttackOrder(
                    buildingPlacementInteractionSystem,
                    buildingPlacementInteractionContext,
                    factionId,
                    requestedTarget,
                    targetCell,
                    attackerCell,
                    out breachTarget,
                    out breachCell,
                    out breachPosition);
        }

        return IssueAttackTarget(
            em,
            targetEntity,
            targetOrderSystem,
            tryResolveBaseBreachTarget,
            collectSelectedAttackSources,
            selectedAttackSourceScratch);
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
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
        List<Entity> selectedAttackSourceScratch = null)
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

            if (request.HasTargetEntity != 0)
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
                request.ExplicitAttackTargetMode != 0,
                selectedAttackSourceScratch);

            AddCommandResult(em, commandEntity, commandResults, ToCommandResultElement(request, result));
            if (em.Exists(commandEntity))
            {
                if (em.HasBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity))
                    commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
                if (em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
                    commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            }
        }

        return handledAny;
    }

    public Result IssueAttackTarget(
        EntityManager em,
        Entity targetEntity,
        UnitTargetOrderSystem targetOrderSystem,
        UnitTargetOrderSystem.TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget = null,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources = null,
        List<Entity> selectedAttackSourceScratch = null)
    {
        using NativeList<Entity> selectedEntities = new(Allocator.Temp);
        using EntityQuery selectedAttackQuery = CreateSelectedAttackQuery(em);
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        CollectSelectedAttackSources(
            em,
            selectedAttackQuery,
            entityType,
            collectSelectedAttackSources,
            selectedAttackSourceScratch,
            selectedEntities);
        UnitTargetOrderSystem.AttackOrderIssueResult issueResult =
            targetOrderSystem.IssueAttackTarget(em, selectedEntities.AsArray(), targetEntity, tryResolveBaseBreachTarget);
        return issueResult.CommandResult.Accepted
            ? Result.Accepted(issueResult)
            : Result.Rejected(issueResult.CommandResult);
    }

    private static bool ProcessPreResolvedAttackRequests(
        EntityManager em,
        EntityQuery commandQueueQuery,
        EntityQuery selectedAttackQuery,
        EntityTypeHandle entityType)
    {
        if (commandQueueQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults =
            em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        bool handledAny = false;
        var targetOrderSystem = new UnitTargetOrderSystem();
        for (int i = 0; i < commandRequests.Length;)
        {
            RtsSelectionCommandIntentRequestElement request = commandRequests[i];
            if (request.Kind != RtsSelectionCommandIntentKind.Attack ||
                request.HasTargetEntity == 0)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
            handledAny = true;
            Result result;
            TacticalCommandResult targetValidation = targetOrderSystem.ValidateAttackTarget(em, request.TargetEntity);
            if (!targetValidation.Accepted)
            {
                result = Result.Rejected(targetValidation);
            }
            else
            {
                using NativeList<Entity> selectedEntities = new(Allocator.Temp);
                CollectSelectedAttackSourceEntities(em, selectedAttackQuery, entityType, selectedEntities);
                UnitTargetOrderSystem.AttackOrderIssueResult issueResult =
                    targetOrderSystem.IssueAttackTarget(em, selectedEntities.AsArray(), request.TargetEntity);
                result = issueResult.CommandResult.Accepted
                    ? Result.Accepted(issueResult)
                    : Result.Rejected(issueResult.CommandResult);
            }

            AddCommandResult(em, commandEntity, commandResults, ToCommandResultElement(request, result));
            if (em.Exists(commandEntity))
            {
                if (em.HasBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity))
                    commandRequests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
                if (em.HasBuffer<RtsSelectionCommandResultElement>(commandEntity))
                    commandResults = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
            }
        }

        return handledAny;
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

    private static EntityQuery CreateSelectedAttackQuery(EntityManager em)
    {
        return em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitMove>(),
            ComponentType.ReadOnly<UnitCombat>(),
            ComponentType.ReadOnly<UnitAttack>(),
            ComponentType.ReadOnly<LocalTransform>());
    }

    private static void CollectSelectedAttackSources(
        EntityManager em,
        EntityQuery selectedAttackQuery,
        EntityTypeHandle entityType,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources,
        List<Entity> selectedAttackSourceScratch,
        NativeList<Entity> selectedEntities)
    {
        selectedEntities.Clear();
        if (collectSelectedAttackSources != null)
        {
            List<Entity> scratch = selectedAttackSourceScratch ?? new List<Entity>();
            scratch.Clear();
            collectSelectedAttackSources(em, scratch);
            if (scratch.Count > 0)
            {
                for (int i = 0; i < scratch.Count; i++)
                    selectedEntities.Add(scratch[i]);
                return;
            }
        }

        CollectSelectedAttackSourceEntities(em, selectedAttackQuery, entityType, selectedEntities);
    }

    private static void CollectSelectedAttackSourceEntities(
        EntityManager em,
        EntityQuery selectedAttackQuery,
        EntityTypeHandle entityType,
        NativeList<Entity> selectedEntities)
    {
        selectedEntities.Clear();
        if (selectedAttackQuery.IsEmptyIgnoreFilter)
            return;

        using NativeArray<ArchetypeChunk> chunks = selectedAttackQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> chunkEntities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < chunkEntities.Length; i++)
                selectedEntities.Add(chunkEntities[i]);
        }
    }

    private static bool TryResolveBaseBreachTargetForAttackOrder(
        BuildingPlacementInteractionSystem buildingPlacementInteractionSystem,
        BuildingPlacementInteractionSystem.Context buildingPlacementInteractionContext,
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
        return buildingPlacementInteractionSystem != null &&
               buildingPlacementInteractionSystem.TryResolveBaseBreachTarget(
                   buildingPlacementInteractionContext,
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
