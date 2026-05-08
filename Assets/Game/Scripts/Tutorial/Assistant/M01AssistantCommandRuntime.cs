using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public static class M01AssistantCommandRuntime
{
    public const string MoveToCoverAnchorId = "tutorial.move_target.cover_01";

    public static TacticalCommandResult TrySelectRuntimeEntity(string runtimeEntityId)
    {
        return TrySelectRuntimeEntity(World.DefaultGameObjectInjectionWorld, RTSSelectionSystem.Instance, runtimeEntityId);
    }

    public static TacticalCommandResult TryIssueMoveToAnchor(TacticalMapRuntimeLoader loader, string anchorId)
    {
        return TryIssueMoveToAnchor(World.DefaultGameObjectInjectionWorld, loader, RTSSelectionSystem.Instance, anchorId);
    }

    public static TacticalCommandResult TryIssueMoveToAnchor(string anchorId)
    {
        return TryIssueMoveToAnchor(World.DefaultGameObjectInjectionWorld, ResolveActiveLoader(), RTSSelectionSystem.Instance, anchorId);
    }

    public static TacticalCommandResult TryIssueAttackTarget(string runtimeEntityId)
    {
        return TryIssueAttackTarget(World.DefaultGameObjectInjectionWorld, RTSSelectionSystem.Instance, runtimeEntityId);
    }

    public static TacticalCommandResult TrySelectRuntimeEntity(
        World world,
        RTSSelectionSystem selectionSystem,
        string runtimeEntityId)
    {
        if (!IsM01CommandAllowed() || runtimeEntityId != Chapter01M01PlayableRuntime.PlayerSquadEntityId)
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        if (!TryResolveRuntimeEntity(world, runtimeEntityId, out Entity entity))
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        if (selectionSystem == null)
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);

        return selectionSystem.TrySelectRuntimeEntity(entity);
    }

    public static TacticalCommandResult TryIssueMoveToAnchor(
        World world,
        TacticalMapRuntimeLoader loader,
        RTSSelectionSystem selectionSystem,
        string anchorId)
    {
        if (!IsM01CommandAllowed())
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        if (anchorId != MoveToCoverAnchorId || loader == null || !loader.TryGetAnchorCell(anchorId, out Vector2Int cell))
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        if (selectionSystem == null)
            return Reject(TacticalCommandReasonCode.NoSelection);
        if (!TryResolveRuntimeEntity(world, Chapter01M01PlayableRuntime.PlayerSquadEntityId, out Entity squad) ||
            !IsAlive(world.EntityManager, squad))
        {
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        }

        return selectionSystem.TryIssueMoveToCell(new int2(cell.x, cell.y));
    }

    public static TacticalCommandResult TryIssueAttackTarget(
        World world,
        RTSSelectionSystem selectionSystem,
        string runtimeEntityId)
    {
        if (!IsM01CommandAllowed() || runtimeEntityId != Chapter01M01PlayableRuntime.EnemyPatrolEntityId)
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        if (!TryResolveRuntimeEntity(world, Chapter01M01PlayableRuntime.PlayerSquadEntityId, out Entity squad) ||
            !IsAlive(world.EntityManager, squad))
        {
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        }
        if (!TryResolveRuntimeEntity(world, runtimeEntityId, out Entity target) || !IsAlive(world.EntityManager, target))
            return Reject(TacticalCommandReasonCode.TargetNotAttackable);
        if (selectionSystem == null)
            return Reject(TacticalCommandReasonCode.NoSelection);

        return selectionSystem.TryIssueAttackTarget(target);
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

    private static bool TryResolveRuntimeEntity(World world, string runtimeEntityId, out Entity entity)
    {
        entity = Entity.Null;
        if (world == null || !world.IsCreated || string.IsNullOrWhiteSpace(runtimeEntityId))
            return false;

        EntityManager em = world.EntityManager;
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

    private static TacticalMapRuntimeLoader ResolveActiveLoader()
    {
        TacticalMapRuntimeLoader[] loaders = Resources.FindObjectsOfTypeAll<TacticalMapRuntimeLoader>();
        for (int i = 0; i < loaders.Length; i++)
        {
            TacticalMapRuntimeLoader loader = loaders[i];
            if (loader == null || !loader.gameObject.scene.IsValid())
                continue;

            return loader;
        }

        return null;
    }

    private static bool IsAlive(EntityManager em, Entity entity)
    {
        return entity != Entity.Null &&
            em.Exists(entity) &&
            (!em.HasComponent<UnitHealth>(entity) || em.GetComponentData<UnitHealth>(entity).Current > 0);
    }

    private static TacticalCommandResult Reject(TacticalCommandReasonCode reasonCode)
    {
        TacticalCommandResult result = TacticalCommandResult.Rejected(reasonCode);
        BattleHudGameplayBridge.ResolveActive()?.ApplyCommandResult(result);
        return result;
    }
}
