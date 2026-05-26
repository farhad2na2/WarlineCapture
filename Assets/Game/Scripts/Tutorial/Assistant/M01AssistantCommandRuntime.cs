using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class M01AssistantCommandRuntime
{
    public const string MoveToCoverAnchorId = "tutorial.move_target.cover_01";

    public static TacticalCommandResult TrySelectRuntimeEntity(string runtimeEntityId)
    {
        return TrySelectRuntimeEntity(World.DefaultGameObjectInjectionWorld, runtimeEntityId);
    }

    public static TacticalCommandResult TryIssueMoveToAnchor(string anchorId)
    {
        return TryIssueMoveToAnchor(World.DefaultGameObjectInjectionWorld, anchorId);
    }

    public static TacticalCommandResult TryIssueAttackTarget(string runtimeEntityId)
    {
        return TryIssueAttackTarget(World.DefaultGameObjectInjectionWorld, runtimeEntityId);
    }

    public static TacticalCommandResult TrySelectRuntimeEntity(
        World world,
        string runtimeEntityId)
    {
        return ExecuteAssistantCommand(world, new M01AssistantCommandRequestElement
        {
            Kind = M01AssistantCommandKind.SelectRuntimeEntity,
            RuntimeEntityId = ToFixed128(runtimeEntityId)
        });
    }

    public static TacticalCommandResult TryIssueMoveToAnchor(
        World world,
        string anchorId)
    {
        if (!IsM01CommandAllowed())
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        if (anchorId != MoveToCoverAnchorId)
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);

        return ExecuteAssistantCommand(world, new M01AssistantCommandRequestElement
        {
            Kind = M01AssistantCommandKind.MoveSelectedUnitsToCell,
            TargetCell = Chapter01M01PlayableRuntime.GetMoveToCoverCell(),
            HasTargetCell = 1
        });
    }

    public static TacticalCommandResult TryIssueAttackTarget(
        World world,
        string runtimeEntityId)
    {
        return ExecuteAssistantCommand(world, new M01AssistantCommandRequestElement
        {
            Kind = M01AssistantCommandKind.AttackRuntimeEntity,
            RuntimeEntityId = ToFixed128(runtimeEntityId)
        });
    }

    public static TacticalCommandResult GetBuildCommandResult()
    {
        if (WarlineCaptureMissionRules.IsBuildAllowedForActiveMission())
            return TacticalCommandResult.Success();

        return TacticalCommandResult.Rejected(
            TacticalCommandReasonCode.MissionDoesNotAllowBuild,
            WarlineCaptureMissionRules.M01BuildDisabledMessage);
    }

    private static bool IsM01CommandAllowed()
    {
        return Chapter01M01PlayableRuntime.IsActiveMission();
    }

    public static bool HasTypedCommandHooks(World world)
    {
        if (!IsM01CommandAllowed() || world == null || !world.IsCreated)
            return false;

        return TryResolveRuntimeEntity(world.EntityManager, Chapter01M01PlayableRuntime.PlayerSquadEntityId, out Entity squad) &&
            IsAlive(world.EntityManager, squad) &&
            TryResolveRuntimeEntity(world.EntityManager, Chapter01M01PlayableRuntime.EnemyPatrolEntityId, out Entity patrol) &&
            world.EntityManager.Exists(patrol);
    }

    private static TacticalCommandResult ExecuteAssistantCommand(World world, M01AssistantCommandRequestElement request)
    {
        return new M01AssistantCommandRequestSystem().Execute(world, request);
    }

    private static bool TryResolveRuntimeEntity(EntityManager em, string runtimeEntityId, out Entity entity)
    {
        entity = Entity.Null;
        if (string.IsNullOrWhiteSpace(runtimeEntityId))
            return false;

        using EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<MissionRuntimeEntityId>());
        using NativeArray<Entity> entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
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

    private static bool IsAlive(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
            em.Exists(entity) &&
            (!em.HasComponent<UnitHealth>(entity) || em.GetComponentData<UnitHealth>(entity).Current > 0);
    }

    private static TacticalCommandResult Reject(TacticalCommandReasonCode reasonCode)
    {
        return TacticalCommandResult.Rejected(reasonCode);
    }

    private static FixedString128Bytes ToFixed128(string value)
    {
        FixedString128Bytes result = default;
        if (!string.IsNullOrEmpty(value))
            result.Append(value.Length <= 124 ? value : value.Substring(0, 124));
        return result;
    }
}
