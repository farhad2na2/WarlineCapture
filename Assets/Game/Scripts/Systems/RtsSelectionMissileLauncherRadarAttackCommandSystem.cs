using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[DisableAutoCreation]
public partial struct RtsSelectionMissileLauncherRadarAttackCommandSystem : ISystem
{
    private EntityQuery _commandQueueQuery;

    private enum MissileLauncherTargetMode : byte
    {
        None,
        Ground,
        Air
    }

    public void OnCreate(ref SystemState state)
    {
        _commandQueueQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        state.RequireForUpdate(_commandQueueQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public static bool TryIssuePendingFocusedRadarAttack(
        EntityManager em,
        Entity launcher,
        out float3 targetPosition)
    {
        using EntityQuery commandQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadWrite<RtsSelectionInputStateComponent>(),
            ComponentType.ReadWrite<RtsSelectionCommandIntentRequestElement>());
        return TryIssuePendingFocusedRadarAttack(em, commandQueueQuery, launcher, out targetPosition);
    }

    private static bool TryIssuePendingFocusedRadarAttack(
        EntityManager em,
        EntityQuery commandQueueQuery,
        Entity launcher,
        out float3 targetPosition)
    {
        targetPosition = default;
        if (commandQueueQuery.IsEmptyIgnoreFilter)
            return false;

        Entity commandEntity = commandQueueQuery.GetSingletonEntity();
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests =
            em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        if (!HasToggleAttackTargetModeRequest(commandRequests))
            return false;

        if (!IsPlayerMissileLauncher(em, launcher, out byte factionId, out MissileLauncherTargetMode mode))
            return false;

        RemoveToggleAttackTargetModeRequests(commandRequests);
        bool issued = UnitAttackOrderRequestSystem.EnqueueAndProcessRadarAttackTarget(
            em,
            launcher,
            factionId,
            mode == MissileLauncherTargetMode.Air,
            out UnitAttackOrderResultElement result);
        if (issued)
            targetPosition = result.TargetPosition;
        return issued;
    }

    private static bool IsPlayerMissileLauncher(
        EntityManager em,
        Entity launcher,
        out byte factionId,
        out MissileLauncherTargetMode mode)
    {
        factionId = 0;
        mode = MissileLauncherTargetMode.None;
        if (launcher == Entity.Null ||
            !em.Exists(launcher) ||
            !em.HasComponent<Faction>(launcher) ||
            !FactionIdentity.IsPlayerControlled(em.GetComponentData<Faction>(launcher).Id) ||
            !em.HasComponent<UnitCombat>(launcher) ||
            em.GetComponentData<UnitCombat>(launcher).CanAttack == 0)
        {
            return false;
        }

        mode = ResolveMissileLauncherTargetMode(em, launcher);
        if (mode == MissileLauncherTargetMode.None)
            return false;

        factionId = em.GetComponentData<Faction>(launcher).Id;
        return true;
    }

    private static MissileLauncherTargetMode ResolveMissileLauncherTargetMode(EntityManager em, Entity launcher)
    {
        if (!em.HasComponent<UnitSourcePrefabKey>(launcher))
            return MissileLauncherTargetMode.None;

        string sourceKey = em.GetComponentData<UnitSourcePrefabKey>(launcher).Value.ToString();
        if (string.Equals(sourceKey, "Unit_Veh_Missle_Launcher_Ground", System.StringComparison.OrdinalIgnoreCase))
            return MissileLauncherTargetMode.Ground;

        return MissileLauncherTargetMode.None;
    }

    private static bool HasToggleAttackTargetModeRequest(
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests)
    {
        for (int i = 0; i < commandRequests.Length; i++)
        {
            if (commandRequests[i].Kind == RtsSelectionCommandIntentKind.ToggleAttackTargetMode)
                return true;
        }

        return false;
    }

    private static void RemoveToggleAttackTargetModeRequests(
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> commandRequests)
    {
        for (int i = 0; i < commandRequests.Length;)
        {
            if (commandRequests[i].Kind != RtsSelectionCommandIntentKind.ToggleAttackTargetMode)
            {
                i++;
                continue;
            }

            commandRequests.RemoveAt(i);
        }
    }
}
