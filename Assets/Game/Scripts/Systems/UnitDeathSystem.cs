using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(UnitAttackSystem))]
public partial struct UnitDeathSystem : ISystem
{
    private const float VehicleWreckLifetimeSeconds = 5f;

    private struct GameStatsDeathRecordedTag : IComponentData
    {
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitHealth>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var queueEntity = RespawnQueueUtils.GetOrCreateQueue(ref state);
        var queueState = SystemAPI.GetComponent<RespawnQueueState>(queueEntity);
        var em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;
        double now = SystemAPI.Time.ElapsedTime;
        double respawnDelay = math.max(0.01f, queueState.RespawnDelaySeconds);

        var deathBeginEntities = new NativeList<Entity>(Allocator.Temp);
        var deathBeginDurations = new NativeList<float>(Allocator.Temp);
        foreach (var (health, animationSettings, entity) in SystemAPI
                 .Query<RefRO<UnitHealth>, RefRO<UnitAnimationSettings>>()
                 .WithNone<UnitDeathAnimationState, StaticGridBlocker>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.Current > 0)
                continue;

            deathBeginEntities.Add(entity);
            deathBeginDurations.Add(math.max(0.01f, animationSettings.ValueRO.DeathAnimationSeconds));
        }

        for (int i = 0; i < deathBeginEntities.Length; i++)
        {
            Entity entity = deathBeginEntities[i];
            if (!em.Exists(entity) || em.HasComponent<UnitDeathAnimationState>(entity))
                continue;

            if (!em.HasComponent<GameStatsDeathRecordedTag>(entity))
            {
                if (GameRuntimeStats.IsMilitarySoldierEntity(em, entity) && em.HasComponent<Faction>(entity))
                    GameRuntimeStats.RecordMilitaryDeath(em.GetComponentData<Faction>(entity).Id);
                em.AddComponentData(entity, new GameStatsDeathRecordedTag());
            }

            StripActiveUnitState(em, entity);
            if (TryBeginVehicleWreck(em, entity))
                continue;

            em.AddComponentData(entity, new UnitDeathAnimationState
            {
                TimeRemaining = deathBeginDurations[i]
            });
        }
        deathBeginEntities.Dispose();
        deathBeginDurations.Dispose();

        var finalizeEntities = new NativeList<Entity>(Allocator.Temp);
        foreach (var (health, deathState, entity) in SystemAPI
                 .Query<RefRO<UnitHealth>, RefRW<UnitDeathAnimationState>>()
                 .WithNone<StaticGridBlocker>()
                 .WithEntityAccess())
        {
            if (health.ValueRO.Current > 0)
                continue;

            deathState.ValueRW.TimeRemaining -= dt;
            if (deathState.ValueRW.TimeRemaining <= 0f)
                finalizeEntities.Add(entity);
        }

        for (int i = 0; i < finalizeEntities.Length; i++)
            FinalizeDeath(em, queueEntity, finalizeEntities[i], now, respawnDelay);

        finalizeEntities.Dispose();
    }

    internal static void StripActiveUnitState(EntityManager em, Entity entity)
    {
        if (em.HasComponent<SelectedUnitTag>(entity))
            em.RemoveComponent<SelectedUnitTag>(entity);
        if (em.HasComponent<ManualMoveOrderTag>(entity))
            em.RemoveComponent<ManualMoveOrderTag>(entity);
        if (em.HasComponent<AutoWanderMoveTag>(entity))
            em.RemoveComponent<AutoWanderMoveTag>(entity);
        if (em.HasComponent<EngageTarget>(entity))
            em.RemoveComponent<EngageTarget>(entity);
        if (em.HasComponent<RecentAttacker>(entity))
            em.RemoveComponent<RecentAttacker>(entity);
        if (em.HasComponent<RecentDamageHealthBarVisibility>(entity))
            em.RemoveComponent<RecentDamageHealthBarVisibility>(entity);
        if (em.HasComponent<UnitPathFollow>(entity))
            em.RemoveComponent<UnitPathFollow>(entity);
        if (em.HasComponent<UnitPathRange>(entity))
            em.RemoveComponent<UnitPathRange>(entity);
        if (em.HasComponent<UnitPathRequest>(entity))
            em.RemoveComponent<UnitPathRequest>(entity);
        if (em.HasComponent<UnitAttackAnimationState>(entity))
            em.SetComponentData(entity, new UnitAttackAnimationState { TimeRemaining = 0f });
        if (em.HasComponent<UnitMoveVisualState>(entity))
            em.SetComponentData(entity, new UnitMoveVisualState { IsMoving = 0, StillSeconds = 0f });

        if (em.HasComponent<UnitAttachedLightRuntime>(entity))
        {
            UnitAttachedLightRuntime runtime = em.GetComponentObject<UnitAttachedLightRuntime>(entity);
            if (runtime?.Instances != null)
            {
                for (int i = 0; i < runtime.Instances.Length; i++)
                {
                    if (runtime.Instances[i] != null)
                        Object.Destroy(runtime.Instances[i]);
                }
            }

            em.RemoveComponent<UnitAttachedLightRuntime>(entity);
        }
    }

    private static bool TryBeginVehicleWreck(EntityManager em, Entity entity)
    {
        if (!em.HasComponent<UnitDestroyedVisualReference>(entity) ||
            !em.HasComponent<UnitFootprint>(entity) ||
            !em.HasComponent<UnitGrid>(entity))
        {
            return false;
        }

        if (em.HasComponent<UnitAirState>(entity) && em.HasComponent<LocalTransform>(entity))
        {
            UnitAirState airState = em.GetComponentData<UnitAirState>(entity);
            LocalTransform transform = em.GetComponentData<LocalTransform>(entity);
            float groundedY = airState.HomeInitialized != 0 ? airState.HomePosition.y : transform.Position.y;
            transform.Position.y = groundedY;
            em.SetComponentData(entity, transform);

            airState.Airborne = 0;
            airState.ReturningHome = 0;
            airState.TakeoffRolling = 0;
            airState.LandingRolling = 0;
            airState.AttackRunActive = 0;
            airState.ReturnApproachInitialized = 0;
            em.SetComponentData(entity, airState);
        }

        UnitDestroyedVisualReference visualRef = em.GetComponentData<UnitDestroyedVisualReference>(entity);
        if (em.HasBuffer<Child>(entity))
        {
            var children = em.GetBuffer<Child>(entity);
            for (int i = 0; i < children.Length; i++)
            {
                Entity child = children[i].Value;
                UnitDestroyedVisualSystem.SetChildVisible(em, child, child == visualRef.DestroyedVisual);
            }
        }
        else
        {
            UnitDestroyedVisualSystem.SetChildVisible(em, visualRef.AliveVisual, false);
            if (em.HasComponent<UnitTurretReference>(entity))
                UnitDestroyedVisualSystem.SetChildVisible(em, em.GetComponentData<UnitTurretReference>(entity).Turret, false);
            UnitDestroyedVisualSystem.SetChildVisible(em, visualRef.DestroyedVisual, true);
        }

        if (!em.HasComponent<StaticGridBlocker>(entity))
            em.AddComponent<StaticGridBlocker>(entity);

        int2 footprint = em.GetComponentData<UnitFootprint>(entity).Size;
        if (em.HasComponent<GridBlockerSize>(entity))
        {
            em.SetComponentData(entity, new GridBlockerSize { Size = footprint });
        }
        else
        {
            em.AddComponentData(entity, new GridBlockerSize { Size = footprint });
        }

        if (em.HasComponent<VehicleWreckState>(entity))
        {
            em.SetComponentData(entity, new VehicleWreckState { TimeRemaining = VehicleWreckLifetimeSeconds });
        }
        else
        {
            em.AddComponentData(entity, new VehicleWreckState { TimeRemaining = VehicleWreckLifetimeSeconds });
        }

        return true;
    }

    internal static void FinalizeDeath(EntityManager em, Entity queueEntity, Entity entity, double now, double respawnDelay)
    {
        if (!em.Exists(entity))
            return;

        var destroySet = new HashSet<Entity> { entity };
        CollectDescendants(em, entity, destroySet);
        CollectLinkedEntities(em, entity, destroySet);

        var entities = new NativeArray<Entity>(destroySet.Count, Allocator.Temp);
        int index = 0;
        foreach (Entity e in destroySet)
            entities[index++] = e;

        em.DestroyEntity(entities);
        entities.Dispose();
    }

    private static void CollectDescendants(EntityManager em, Entity entity, HashSet<Entity> destroySet)
    {
        if (!em.HasBuffer<Child>(entity))
            return;

        var children = em.GetBuffer<Child>(entity);
        for (int i = 0; i < children.Length; i++)
        {
            Entity child = children[i].Value;
            if (!destroySet.Add(child))
                continue;

            CollectDescendants(em, child, destroySet);
        }
    }

    private static void CollectLinkedEntities(EntityManager em, Entity entity, HashSet<Entity> destroySet)
    {
        if (!em.HasBuffer<LinkedEntityGroup>(entity))
            return;

        var linked = em.GetBuffer<LinkedEntityGroup>(entity);
        for (int i = 0; i < linked.Length; i++)
            destroySet.Add(linked[i].Value);
    }
}
