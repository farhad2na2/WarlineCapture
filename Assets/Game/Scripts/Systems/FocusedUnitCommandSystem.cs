using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public sealed class FocusedUnitCommandSystem
{
    public enum MissileLauncherTargetMode
    {
        None,
        Ground,
        Air
    }

    private World _queryWorld;
    private EntityQuery _respawnQueueQuery;
    private EntityQuery _selectedMoveQuery;

    public void EnsureEntityQueries(EntityManager em)
    {
        World world = em.World;
        if (_queryWorld == world && world != null && world.IsCreated)
            return;

        _queryWorld = world;
        _respawnQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<RespawnQueueTag>(),
            ComponentType.ReadOnly<RespawnQueueComponent>());
        _selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
    }

    public void DestroyFocusedUnit(EntityManager em, Entity entity)
    {
        if (em.HasComponent<SelectedUnitTag>(entity))
            em.RemoveComponent<SelectedUnitTag>(entity);

        if (em.HasComponent<UnitHealth>(entity))
        {
            UnitHealth health = em.GetComponentData<UnitHealth>(entity);
            health.Current = 0;
            em.SetComponentData(entity, health);
        }
        else
        {
            em.DestroyEntity(entity);
        }
    }

    public bool ReturnFocusedUnitToBase(EntityManager em, Entity entity, UnitMoveOrderSystem moveOrderSystem)
    {
        EnsureEntityQueries(em);
        if (_respawnQueueQuery.IsEmptyIgnoreFilter)
            return false;

        Entity queueEntity = _respawnQueueQuery.GetSingletonEntity();
        byte factionId = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
        int2 goal = default;
        if (em.HasBuffer<RespawnFactionSpawnPoint>(queueEntity))
        {
            DynamicBuffer<RespawnFactionSpawnPoint> points = em.GetBuffer<RespawnFactionSpawnPoint>(queueEntity);
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].FactionId != factionId)
                    continue;

                goal = points[i].SpawnCell;
                break;
            }
        }

        moveOrderSystem.IssueImmediateMoveCommand(em, entity, goal);
        return true;
    }

    public void EnableFocusedUnitAutoAttack(EntityManager em, Entity entity, UnitTargetOrderSystem targetOrderSystem)
    {
        targetOrderSystem.ClearCommandedAttackOrderComponents(em, entity);
    }

    public bool TryIssueFocusedMissileLauncherRadarAttack(
        EntityManager em,
        Entity launcher,
        UnitTargetOrderSystem targetOrderSystem,
        out float3 targetPosition)
    {
        targetPosition = default;
        if (!em.HasComponent<UnitCombat>(launcher) || em.GetComponentData<UnitCombat>(launcher).CanAttack == 0)
            return false;

        MissileLauncherTargetMode mode = ResolveMissileLauncherTargetMode(em, launcher);
        if (mode == MissileLauncherTargetMode.None)
            return false;

        byte factionId = em.GetComponentData<Faction>(launcher).Id;
        if (!targetOrderSystem.TryFindRadarTargetForMissileLauncher(
                em,
                factionId,
                mode == MissileLauncherTargetMode.Air,
                launcher,
                out Entity target,
                out int2 targetCell,
                out targetPosition))
        {
            return false;
        }

        targetOrderSystem.IssueDirectAttackTarget(em, launcher, target, targetCell, targetPosition);
        return true;
    }

    public bool IssueImmediateSelectedUnitOrder(
        EntityManager em,
        bool clearEngageTarget,
        UnitMoveOrderSystem moveOrderSystem)
    {
        return IssueImmediateSelectedUnitOrder(em, clearEngageTarget, false, moveOrderSystem);
    }

    public bool IssueImmediateSelectedUnitOrder(
        EntityManager em,
        bool clearEngageTarget,
        bool holdPosition,
        UnitMoveOrderSystem moveOrderSystem)
    {
        EnsureEntityQueries(em);
        using NativeList<Entity> selectedEntities = CollectSelectedMoveEntities(em);
        NativeArray<Entity> entities = selectedEntities.AsArray();
        if (entities.Length == 0)
            return false;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!em.Exists(entity))
                continue;

            ClearImmediateOrderComponents(em, entity, clearEngageTarget, moveOrderSystem);
            if (holdPosition)
            {
                if (!em.HasComponent<HoldPositionOrderTag>(entity))
                    em.AddComponent<HoldPositionOrderTag>(entity);
                if (em.HasComponent<UnitCombat>(entity))
                {
                    UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                    if (combat.CanAttack != 0)
                    {
                        combat.AutoEngage = 1;
                        em.SetComponentData(entity, combat);
                    }
                }
            }
            else
            {
                moveOrderSystem.RemoveComponentIfPresent<HoldPositionOrderTag>(em, entity);
                if (clearEngageTarget && em.HasComponent<UnitCombat>(entity))
                {
                    UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                    if (combat.CanAttack != 0)
                    {
                        combat.AutoEngage = 0;
                        em.SetComponentData(entity, combat);
                    }
                }
            }
            if (!em.HasComponent<ManualMoveOrderTag>(entity))
                em.AddComponent<ManualMoveOrderTag>(entity);
        }

        return true;
    }

    private NativeList<Entity> CollectSelectedMoveEntities(EntityManager em)
    {
        int count = _selectedMoveQuery.CalculateEntityCount();
        NativeList<Entity> selectedEntities = new(count, Allocator.Temp);
        if (count <= 0)
            return selectedEntities;

        EntityTypeHandle entityType = em.GetEntityTypeHandle();
        using NativeArray<ArchetypeChunk> chunks = _selectedMoveQuery.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
            for (int i = 0; i < entities.Length; i++)
                selectedEntities.Add(entities[i]);
        }

        return selectedEntities;
    }

    private static void ClearImmediateOrderComponents(
        EntityManager em,
        Entity entity,
        bool clearEngageTarget,
        UnitMoveOrderSystem moveOrderSystem)
    {
        moveOrderSystem.RemoveComponentIfPresent<UnitTarget>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitPathRequest>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitPathFollow>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitPathRange>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitPathRetryCooldown>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitLongDistanceMove>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<ManualMoveGroupMemberTag>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<AutoWanderMoveTag>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<BaseBreachOrder>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitTransportBoardingTarget>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitTransportRopeDisembarkRequest>(em, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitResourceHaulOrder>(em, entity);
        if (clearEngageTarget)
            moveOrderSystem.RemoveComponentIfPresent<EngageTarget>(em, entity);

        StopRuntimeMotion(em, entity);
    }

    private static void StopRuntimeMotion(EntityManager em, Entity entity)
    {
        if (em.HasComponent<UnitVehicleKinematics>(entity))
        {
            UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(entity);
            kinematics.CurrentSpeed = 0f;
            kinematics.StallSeconds = 0f;
            em.SetComponentData(entity, kinematics);
        }

        if (!em.HasComponent<UnitAirComponent>(entity))
            return;

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
        airState.ReturningHome = 0;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.AttackRunActive = 0;
        airState.ReturnApproachInitialized = 0;
        em.SetComponentData(entity, airState);
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
}
