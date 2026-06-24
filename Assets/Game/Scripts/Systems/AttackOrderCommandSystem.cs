using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(UnitAttackOrderRequestSystem))]
public partial struct AttackOrderCommandSystem : ISystem
{
    public delegate bool TryGetClickedUnitEntityDelegate(Vector2 screenPosition, EntityManager em, out Entity entity);
    public delegate void CollectSelectedAttackSourcesDelegate(EntityManager em, List<Entity> sources);
    public delegate bool TryResolveBaseBreachTargetDelegate(
        byte factionId,
        Entity targetEntity,
        int2 targetCell,
        int2 attackerCell,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition);

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

        public static Result Accepted(TacticalCommandResult commandResult, Entity targetEntity, float3 targetPosition)
        {
            return new Result(true, true, commandResult, targetEntity, targetPosition);
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
        UnitAttackOrderRequestSystem.EnsureCommandEntity(state.EntityManager);
        state.RequireForUpdate(_commandQueueQuery);
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

    public Result TryRequestAttackOrderToClickedUnit(
        EntityManager em,
        Vector2 screenPosition,
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

        TacticalCommandResult targetValidation = ValidateAttackTarget(em, targetEntity);
        if (!targetValidation.Accepted)
            return explicitAttackTargetModeActive ? Result.Rejected(targetValidation) : Result.NoCommand();

        TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget = null;
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
            tryResolveBaseBreachTarget,
            collectSelectedAttackSources,
            selectedAttackSourceScratch);
    }

    public bool ProcessCommandIntentRequests(
        EntityManager em,
        Entity commandEntity,
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests,
        DynamicBuffer<RtsSelectionCommandResultElement> commandResults,
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
            Result result = TryRequestAttackOrderToClickedUnit(
                em,
                screenPosition,
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
        TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget = null,
        CollectSelectedAttackSourcesDelegate collectSelectedAttackSources = null,
        List<Entity> selectedAttackSourceScratch = null)
    {
        if (tryResolveBaseBreachTarget == null && collectSelectedAttackSources == null)
        {
            int attackRequestId = UnitAttackOrderRequestSystem.EnqueueSelectedAttackTarget(em, targetEntity);
            UnitAttackOrderRequestSystem.ProcessPendingRequests(em);
            return UnitAttackOrderRequestSystem.TryGetResult(
                em,
                attackRequestId,
                out UnitAttackOrderResultElement attackResult)
                ? ToAttackResult(attackResult)
                : Result.Rejected(TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable));
        }

        using NativeList<Entity> selectedEntities = new(Allocator.Temp);
        using NativeList<int> attackRequestIds = new(Allocator.Temp);
        using EntityQuery selectedAttackQuery = CreateSelectedAttackQuery(em);
        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        CollectSelectedAttackSources(
            em,
            selectedAttackQuery,
            entityType,
            collectSelectedAttackSources,
            selectedAttackSourceScratch,
            selectedEntities);
        if (selectedEntities.Length == 0)
            return Result.Rejected(TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection));

        TacticalCommandResult targetValidation = ValidateAttackTarget(em, targetEntity);
        if (!targetValidation.Accepted)
            return Result.Rejected(targetValidation);

        int2 targetCell = em.HasComponent<UnitGrid>(targetEntity)
            ? em.GetComponentData<UnitGrid>(targetEntity).Cell
            : default;
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity sourceEntity = selectedEntities[i];
            if (TryResolveBaseBreachRequest(
                    em,
                    sourceEntity,
                    targetEntity,
                    targetCell,
                    tryResolveBaseBreachTarget,
                    out Entity breachTarget,
                    out int2 breachCell,
                    out float3 breachPosition))
            {
                attackRequestIds.Add(UnitAttackOrderRequestSystem.EnqueueSourceBaseBreachAttackTarget(
                    em,
                    sourceEntity,
                    targetEntity,
                    breachTarget,
                    breachCell,
                    breachPosition));
            }
            else
            {
                attackRequestIds.Add(UnitAttackOrderRequestSystem.EnqueueSourceAttackTarget(em, sourceEntity, targetEntity));
            }
        }

        UnitAttackOrderRequestSystem.ProcessPendingRequests(em);
        return CombineSourceAttackResults(em, attackRequestIds.AsArray());
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
            int attackRequestId = UnitAttackOrderRequestSystem.EnqueueSelectedAttackTarget(em, request.TargetEntity);
            UnitAttackOrderRequestSystem.ProcessPendingRequests(em, selectedAttackQuery, entityType);
            Result result = UnitAttackOrderRequestSystem.TryGetResult(
                em,
                attackRequestId,
                out UnitAttackOrderResultElement attackResult)
                ? ToAttackResult(attackResult)
                : Result.Rejected(TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable));

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

    private static Result ToAttackResult(UnitAttackOrderResultElement result)
    {
        TacticalCommandResult commandResult = result.Accepted != 0
            ? TacticalCommandResult.Success(result.Message.ToString())
            : TacticalCommandResult.Rejected((TacticalCommandReasonCode)result.ReasonCode, result.Message.ToString());
        return result.Issued != 0
            ? Result.Accepted(commandResult, result.TargetEntity, result.TargetPosition)
            : Result.Rejected(commandResult);
    }

    private static Result CombineSourceAttackResults(EntityManager em, NativeArray<int> attackRequestIds)
    {
        int issuedCount = 0;
        Entity targetEntity = Entity.Null;
        float3 targetPosition = default;
        string acceptedMessage = string.Empty;
        TacticalCommandResult rejection = TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        for (int i = 0; i < attackRequestIds.Length; i++)
        {
            if (!UnitAttackOrderRequestSystem.TryGetResult(em, attackRequestIds[i], out UnitAttackOrderResultElement result))
            {
                rejection = TacticalCommandResult.Rejected(TacticalCommandReasonCode.CommandUnavailable);
                continue;
            }

            if (result.Issued != 0)
            {
                issuedCount += result.IssuedCount;
                targetEntity = result.TargetEntity;
                targetPosition = result.TargetPosition;
                string message = result.Message.ToString();
                if (!string.IsNullOrEmpty(message))
                    acceptedMessage = message;
                continue;
            }

            rejection = TacticalCommandResult.Rejected(
                (TacticalCommandReasonCode)result.ReasonCode,
                result.Message.ToString());
        }

        return issuedCount > 0
            ? Result.Accepted(TacticalCommandResult.Success(acceptedMessage), targetEntity, targetPosition)
            : Result.Rejected(rejection);
    }

    private static TacticalCommandResult ValidateAttackTarget(EntityManager em, Entity targetEntity)
    {
        if (targetEntity == Entity.Null ||
            !em.Exists(targetEntity) ||
            !em.HasComponent<Faction>(targetEntity) ||
            !em.HasComponent<LocalTransform>(targetEntity))
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (!FactionIdentity.IsHostileToPlayer(em.GetComponentData<Faction>(targetEntity).Id))
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        if (em.HasComponent<UnitHealth>(targetEntity) &&
            em.GetComponentData<UnitHealth>(targetEntity).Current <= 0)
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        return TacticalCommandResult.Success();
    }

    private static bool TryResolveBaseBreachRequest(
        EntityManager em,
        Entity sourceEntity,
        Entity targetEntity,
        int2 targetCell,
        TryResolveBaseBreachTargetDelegate tryResolveBaseBreachTarget,
        out Entity breachTarget,
        out int2 breachCell,
        out float3 breachPosition)
    {
        breachTarget = Entity.Null;
        breachCell = default;
        breachPosition = default;
        return tryResolveBaseBreachTarget != null &&
               sourceEntity != Entity.Null &&
               em.Exists(sourceEntity) &&
               !em.HasComponent<GroundMissileLauncherComponent>(sourceEntity) &&
               em.HasComponent<Faction>(sourceEntity) &&
               em.HasComponent<UnitGrid>(sourceEntity) &&
               tryResolveBaseBreachTarget(
                   em.GetComponentData<Faction>(sourceEntity).Id,
                   targetEntity,
                   targetCell,
                   em.GetComponentData<UnitGrid>(sourceEntity).Cell,
                   out breachTarget,
                   out breachCell,
                   out breachPosition);
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
