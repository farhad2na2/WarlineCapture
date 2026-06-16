using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[DisableAutoCreation]
public partial struct FocusedUnitCommandSystem : ISystem
{
    private ulong _queryWorldSequenceNumber;
    private bool _queriesInitialized;
    private EntityQuery _respawnQueueQuery;
    private EntityQuery _selectedMoveQuery;

    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
        _queryWorldSequenceNumber = state.WorldUnmanaged.SequenceNumber;
        _queriesInitialized = true;
        _respawnQueueQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<RespawnQueueTag>(),
            ComponentType.ReadOnly<RespawnQueueComponent>());
        _selectedMoveQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public void EnsureEntityQueries(EntityManager em)
    {
        Unity.Entities.World world = em.World;
        ulong worldSequenceNumber = world != null && world.IsCreated ? world.SequenceNumber : 0;
        if (_queriesInitialized && _queryWorldSequenceNumber == worldSequenceNumber)
            return;

        _queryWorldSequenceNumber = worldSequenceNumber;
        _queriesInitialized = true;
        _respawnQueueQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<RespawnQueueTag>(),
            ComponentType.ReadOnly<RespawnQueueComponent>());
        _selectedMoveQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<SelectedUnitTag>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitMove>());
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

        UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em, entity, goal);
        return true;
    }

    public void EnableFocusedUnitAutoAttack(EntityManager em, Entity entity)
    {
        UnitAttackOrderRequestSystem.EnqueueAndProcessClearCommandedAttackOrder(em, entity);
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

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.Exists(entity))
                    continue;

                ClearImmediateOrderComponents(em, ecb, entity, clearEngageTarget, moveOrderSystem);
                if (holdPosition)
                {
                    if (!em.HasComponent<HoldPositionOrderTag>(entity))
                        ecb.AddComponent<HoldPositionOrderTag>(entity);
                    if (em.HasComponent<UnitCombat>(entity))
                    {
                        UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                        if (combat.CanAttack != 0)
                        {
                            combat.AutoEngage = 1;
                            ecb.SetComponent(entity, combat);
                        }
                    }
                }
                else
                {
                    moveOrderSystem.RemoveComponentIfPresent<HoldPositionOrderTag>(em, ecb, entity);
                    if (clearEngageTarget && em.HasComponent<UnitCombat>(entity))
                    {
                        UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                        if (combat.CanAttack != 0)
                        {
                            combat.AutoEngage = 0;
                            ecb.SetComponent(entity, combat);
                        }
                    }
                }
                if (!em.HasComponent<ManualMoveOrderTag>(entity))
                    ecb.AddComponent<ManualMoveOrderTag>(entity);
            }

            ecb.Playback(em);
        }
        finally
        {
            ecb.Dispose();
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
        EntityCommandBuffer ecb,
        Entity entity,
        bool clearEngageTarget,
        UnitMoveOrderSystem moveOrderSystem)
    {
        moveOrderSystem.RemoveComponentIfPresent<UnitTarget>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitPathRequest>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitPathFollow>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitPathRange>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitPathRetryCooldown>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitLongDistanceMove>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<ManualMoveGroupMemberTag>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<AutoWanderMoveTag>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<BaseBreachOrder>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitTransportBoardingTarget>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitTransportRopeDisembarkRequest>(em, ecb, entity);
        moveOrderSystem.RemoveComponentIfPresent<UnitResourceHaulOrder>(em, ecb, entity);
        if (clearEngageTarget)
            moveOrderSystem.RemoveComponentIfPresent<EngageTarget>(em, ecb, entity);

        StopRuntimeMotion(em, ecb, entity);
    }

    private static void StopRuntimeMotion(EntityManager em, EntityCommandBuffer ecb, Entity entity)
    {
        if (em.HasComponent<UnitVehicleKinematics>(entity))
        {
            UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(entity);
            kinematics.CurrentSpeed = 0f;
            kinematics.StallSeconds = 0f;
            ecb.SetComponent(entity, kinematics);
        }

        if (!em.HasComponent<UnitAirComponent>(entity))
            return;

        UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
        airState.ReturningHome = 0;
        airState.TakeoffRolling = 0;
        airState.LandingRolling = 0;
        airState.AttackRunActive = 0;
        airState.ReturnApproachInitialized = 0;
        ecb.SetComponent(entity, airState);
    }

}
