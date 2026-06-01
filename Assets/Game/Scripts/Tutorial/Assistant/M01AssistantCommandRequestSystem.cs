using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public sealed class M01AssistantCommandRequestSystem
{
    private readonly UnitMoveOrderSystem _unitMoveOrderSystem = new();
    private readonly UnitTargetOrderSystem _unitTargetOrderSystem = new();
    private readonly AttackOrderCommandSystem _attackOrderCommandSystem = new();
    private readonly SelectionHudFeedbackSystem _hudFeedbackSystem = new();
    private Entity _commandEntity;
    private World _queryWorld;
    private EntityQuery _selectedMoveQuery;

    public TacticalCommandResult Execute(World world, M01AssistantCommandRequestElement request)
    {
        if (world == null || !world.IsCreated)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        EntityManager em = world.EntityManager;
        Entity entity = EnsureCommandEntity(em);
        M01AssistantCommandQueueComponent queue = em.GetComponentData<M01AssistantCommandQueueComponent>(entity);
        queue.LastRequestId++;
        request.RequestId = queue.LastRequestId;
        em.SetComponentData(entity, queue);
        em.GetBuffer<M01AssistantCommandRequestElement>(entity).Add(request);

        ProcessPendingRequests(world);
        return TryConsumeResult(em, entity, request.RequestId, out TacticalCommandResult result)
            ? result
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
    }

    public void ProcessPendingRequests(World world)
    {
        if (world == null || !world.IsCreated)
            return;

        EntityManager em = world.EntityManager;
        Entity entity = EnsureCommandEntity(em);
        _hudFeedbackSystem.EnsureFeedbackQueue(em);

        DynamicBuffer<M01AssistantCommandRequestElement> requestBuffer = em.GetBuffer<M01AssistantCommandRequestElement>(entity);
        if (requestBuffer.Length == 0)
            return;

        var requests = new List<M01AssistantCommandRequestElement>(requestBuffer.Length);
        for (int i = 0; i < requestBuffer.Length; i++)
            requests.Add(requestBuffer[i]);
        requestBuffer.Clear();

        for (int i = 0; i < requests.Count; i++)
        {
            M01AssistantCommandRequestElement request = requests[i];
            TacticalCommandResult result = ProcessRequest(em, request);
            DynamicBuffer<M01AssistantCommandResultElement> results = em.GetBuffer<M01AssistantCommandResultElement>(entity);
            results.Add(ToResultElement(request, result));
            _hudFeedbackSystem.QueueCommandResult(em, result);
        }

        _hudFeedbackSystem.ProcessPendingFeedback(em);
    }

    public Entity EnsureCommandEntity(EntityManager em)
    {
        if (_commandEntity != Entity.Null &&
            em.Exists(_commandEntity) &&
            em.HasComponent<M01AssistantCommandQueueComponent>(_commandEntity))
        {
            EnsureCommandBuffers(em, _commandEntity);
            return _commandEntity;
        }

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<M01AssistantCommandQueueComponent>());
        if (!query.IsEmptyIgnoreFilter)
        {
            _commandEntity = query.GetSingletonEntity();
            EnsureCommandBuffers(em, _commandEntity);
            return _commandEntity;
        }

        _commandEntity = em.CreateEntity(typeof(M01AssistantCommandQueueComponent));
        em.SetName(_commandEntity, "M01AssistantCommandQueue");
        em.AddBuffer<M01AssistantCommandRequestElement>(_commandEntity);
        em.AddBuffer<M01AssistantCommandResultElement>(_commandEntity);
        return _commandEntity;
    }

    private TacticalCommandResult ProcessRequest(EntityManager em, M01AssistantCommandRequestElement request)
    {
        if (!Chapter01M01PlayableRuntime.IsActiveMission())
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        return request.Kind switch
        {
            M01AssistantCommandKind.SelectRuntimeEntity => SelectRuntimeEntity(em, request.RuntimeEntityId.ToString()),
            M01AssistantCommandKind.MoveSelectedUnitsToCell => MoveSelectedUnitsToCell(em, request),
            M01AssistantCommandKind.AttackRuntimeEntity => AttackRuntimeEntity(em, request.RuntimeEntityId.ToString()),
            _ => TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable)
        };
    }

    private TacticalCommandResult SelectRuntimeEntity(EntityManager em, string runtimeEntityId)
    {
        if (runtimeEntityId != Chapter01M01PlayableRuntime.PlayerSquadEntityId ||
            !TryResolveRuntimeEntity(em, runtimeEntityId, out Entity entity))
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        TacticalCommandResult validation = ValidateControllableEntity(em, entity);
        if (!validation.Accepted)
            return validation;

        ClearSelectedUnits(em);
        if (!em.HasComponent<SelectedUnitTag>(entity))
            em.AddComponent<SelectedUnitTag>(entity);
        return TacticalCommandResult.Success();
    }

    private TacticalCommandResult MoveSelectedUnitsToCell(EntityManager em, M01AssistantCommandRequestElement request)
    {
        if (request.HasTargetCell == 0)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        EnsureEntityQueries(em);
        using NativeArray<Entity> selectedEntities = _selectedMoveQuery.ToEntityArray(Allocator.Temp);
        if (selectedEntities.Length == 0)
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.NoSelection);

        int issuedCount = 0;
        for (int i = 0; i < selectedEntities.Length; i++)
        {
            Entity entity = selectedEntities[i];
            TacticalCommandResult validation = ValidateControllableEntity(em, entity);
            if (!validation.Accepted)
                continue;

            _unitMoveOrderSystem.IssueImmediateMoveCommand(em, entity, request.TargetCell);
            issuedCount++;
        }

        return issuedCount > 0
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
    }

    private TacticalCommandResult AttackRuntimeEntity(EntityManager em, string runtimeEntityId)
    {
        if (runtimeEntityId != Chapter01M01PlayableRuntime.EnemyPatrolEntityId ||
            !TryResolveRuntimeEntity(em, Chapter01M01PlayableRuntime.PlayerSquadEntityId, out Entity squad) ||
            !IsAlive(em, squad) ||
            !TryResolveRuntimeEntity(em, runtimeEntityId, out Entity target) ||
            !IsAlive(em, target))
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        AttackOrderCommandSystem.Result result = _attackOrderCommandSystem.IssueAttackTarget(em, target, _unitTargetOrderSystem);
        return result.HasCommandResult
            ? result.CommandResult
            : TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
    }

    private void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitMove>());
        _attackOrderCommandSystem.EnsureEntityQueries(em);
    }

    private static void ClearSelectedUnits(EntityManager em)
    {
        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<SelectedUnitTag>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.Exists(entity) && em.HasComponent<SelectedUnitTag>(entity))
                em.RemoveComponent<SelectedUnitTag>(entity);
        }
    }

    private static bool TryResolveRuntimeEntity(EntityManager em, string runtimeEntityId, out Entity entity)
    {
        entity = Entity.Null;
        if (string.IsNullOrWhiteSpace(runtimeEntityId))
            return false;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeEntityId>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        for (int i = 0; i < entities.Length; i++)
        {
            Entity candidate = entities[i];
            if (!em.Exists(candidate))
                continue;

            string candidateId = em.GetComponentData<MissionRuntimeEntityId>(candidate).Value.ToString();
            if (candidateId != runtimeEntityId)
                continue;

            entity = candidate;
            return true;
        }

        return false;
    }

    private static TacticalCommandResult ValidateControllableEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null ||
            !em.Exists(entity) ||
            !em.HasComponent<Faction>(entity) ||
            !em.HasComponent<UnitMove>(entity))
        {
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        }

        if (!FactionIdentitySystem.IsPlayerControlled(em.GetComponentData<Faction>(entity).Id))
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);
        if (!IsAlive(em, entity))
            return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetNotAttackable);

        return TacticalCommandResult.Success();
    }

    private static bool IsAlive(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
            em.Exists(entity) &&
            (!em.HasComponent<UnitHealth>(entity) || em.GetComponentData<UnitHealth>(entity).Current > 0);
    }

    private static bool TryConsumeResult(EntityManager em, Entity entity, int requestId, out TacticalCommandResult result)
    {
        DynamicBuffer<M01AssistantCommandResultElement> results = em.GetBuffer<M01AssistantCommandResultElement>(entity);
        for (int i = results.Length - 1; i >= 0; i--)
        {
            M01AssistantCommandResultElement item = results[i];
            if (item.RequestId != requestId)
                continue;

            result = ToTacticalCommandResult(item);
            results.RemoveAt(i);
            return true;
        }

        result = default;
        return false;
    }

    private static M01AssistantCommandResultElement ToResultElement(
        M01AssistantCommandRequestElement request,
        TacticalCommandResult result)
    {
        FixedString512Bytes message = default;
        if (!string.IsNullOrWhiteSpace(result.Message))
            message.Append(result.Message.Length <= 508 ? result.Message : result.Message.Substring(0, 508));

        return new M01AssistantCommandResultElement
        {
            Kind = request.Kind,
            RequestId = request.RequestId,
            Accepted = result.Accepted ? (byte)1 : (byte)0,
            ReasonCode = (int)result.ReasonCode,
            Message = message
        };
    }

    private static TacticalCommandResult ToTacticalCommandResult(M01AssistantCommandResultElement result)
    {
        return result.Accepted != 0
            ? TacticalCommandResult.Success()
            : TacticalCommandResult.Rejected((TacticalCommandReasonCode)result.ReasonCode, result.Message.ToString());
    }

    private static void EnsureCommandBuffers(EntityManager em, Entity entity)
    {
        if (!em.HasBuffer<M01AssistantCommandRequestElement>(entity))
            em.AddBuffer<M01AssistantCommandRequestElement>(entity);
        if (!em.HasBuffer<M01AssistantCommandResultElement>(entity))
            em.AddBuffer<M01AssistantCommandResultElement>(entity);
    }
}
