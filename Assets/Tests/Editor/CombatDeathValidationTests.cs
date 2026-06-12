using NUnit.Framework;
using System;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using Unity.Transforms;
using UnityEngine;

public sealed class CombatDeathValidationTests
{
    private NativeArray<int> _blockerCounts;
    private NativeBitArray _blocked;
    private NativeBitArray _occupied;
    private NativeArray<byte> _friendlyPassFactionIds;

    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new CombatDeathValidationTests();
            tests.SoldierAttack_KillsTargetDestroysEntityAndDoesNotRespawn();
            tests.VehicleWreckCleanup_FinalizesExpiredWreckAndDescendants();
            Debug.Log("[CombatDeathFocusedValidation] result=Passed tests=2");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[CombatDeathFocusedValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (_blockerCounts.IsCreated)
            _blockerCounts.Dispose();
        if (_blocked.IsCreated)
            _blocked.Dispose();
        if (_occupied.IsCreated)
            _occupied.Dispose();
        if (_friendlyPassFactionIds.IsCreated)
            _friendlyPassFactionIds.Dispose();
    }

    [Test]
    public void SoldierAttack_KillsTargetDestroysEntityAndDoesNotRespawn()
    {
        using var world = new World("CombatDeathValidationTests");
        EntityManager em = world.EntityManager;
        CreateGrid(em, 8, 8);

        Entity respawnPrefab = CreateRespawnPrefab(em);
        Entity target = CreateSoldier(
            em,
            factionId: FactionIdentitySystem.PlayerFactionId,
            cell: new int2(4, 4),
            position: new float3(4f, 0f, 4f),
            health: 100,
            damage: 10,
            respawnPrefab);
        Entity attacker = CreateSoldier(
            em,
            factionId: FactionIdentitySystem.EnemyFactionId,
            cell: new int2(4, 5),
            position: new float3(4f, 0f, 5f),
            health: 100,
            damage: 100,
            Entity.Null);
        em.AddComponentData(attacker, new EngageTarget
        {
            Target = target,
            Cell = new int2(4, 4),
            Position = new float3(4f, 0f, 4f),
            IsCommanded = 1
        });

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        SystemHandle deathSystem = world.CreateSystem<UnitDeathSystem>();
        SystemHandle respawnSystem = world.CreateSystem<UnitRespawnSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.Exists(target), "The target should still exist immediately after damage so death cleanup can run.");
        Assert.AreEqual(0, em.GetComponentData<UnitHealth>(target).Current);

        world.SetTime(new TimeData(0.2d, 0.1f));
        deathSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.Exists(target), "A soldier reduced to zero health must be destroyed after its death animation window.");

        Entity queueEntity = GetRespawnQueueEntity(em);
        DynamicBuffer<RespawnRequest> requests = em.GetBuffer<RespawnRequest>(queueEntity);
        Assert.AreEqual(0, requests.Length, "Combat deaths should not queue a replacement soldier.");

        world.SetTime(new TimeData(30d, 0.1f));
        respawnSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, CountLivingRuntimeSoldiers(em, FactionIdentitySystem.PlayerFactionId), "The killed soldier must not respawn later.");
        Assert.IsTrue(em.Exists(attacker), "The attacking soldier should remain alive.");
    }

    [Test]
    public void VehicleWreckCleanup_FinalizesExpiredWreckAndDescendants()
    {
        using var world = new World(nameof(VehicleWreckCleanup_FinalizesExpiredWreckAndDescendants));
        EntityManager em = world.EntityManager;

        Entity wreck = em.CreateEntity(
            typeof(UnitHealth),
            typeof(VehicleWreckComponent),
            typeof(Child));
        em.SetComponentData(wreck, new UnitHealth { Current = 0, Max = 100 });
        em.SetComponentData(wreck, new VehicleWreckComponent { TimeRemaining = 0.05f });
        Entity child = em.CreateEntity(typeof(LocalTransform));
        DynamicBuffer<Child> children = em.GetBuffer<Child>(wreck);
        children.Add(new Child { Value = child });

        SystemHandle system = world.CreateSystem<VehicleWreckCleanupSystem>();
        world.SetTime(new TimeData(1d, 0.1f));
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<VehicleWreckComponent>(wreck), "Expired wrecks should leave the active wreck state after finalization.");
        Assert.IsFalse(em.HasComponent<UnitHealth>(wreck), "Expired wrecks should leave active gameplay components after finalization.");
        Assert.IsFalse(em.Exists(child), "Finalizing a wreck must also destroy descendant visual/runtime entities.");
        Entity queueEntity = GetRespawnQueueEntity(em);
        Assert.IsTrue(em.Exists(queueEntity), "Wreck cleanup should preserve the respawn queue created by the finalization boundary.");
    }

    private void CreateGrid(EntityManager em, int width, int height)
    {
        int gridSize = width * height;
        _blockerCounts = new NativeArray<int>(gridSize, Allocator.Persistent);
        _blocked = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _occupied = new NativeBitArray(gridSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        _friendlyPassFactionIds = new NativeArray<byte>(gridSize, Allocator.Persistent);

        Entity gridEntity = em.CreateEntity(typeof(GridConfig), typeof(DynamicBlockerComponent), typeof(DynamicOccupancyComponent));
        em.SetComponentData(gridEntity, new GridConfig { Width = width, Height = height, CellSize = 1f, Origin = float3.zero });
        em.SetComponentData(gridEntity, new DynamicBlockerComponent
        {
            GridSize = gridSize,
            Counts = _blockerCounts,
            Blocked = _blocked,
            FriendlyPassFactionIds = _friendlyPassFactionIds
        });
        em.SetComponentData(gridEntity, new DynamicOccupancyComponent
        {
            GridSize = gridSize,
            Occupied = _occupied
        });

        DynamicBuffer<GridRoad> roads = em.AddBuffer<GridRoad>(gridEntity);
        roads.ResizeUninitialized(gridSize);
        for (int i = 0; i < roads.Length; i++)
            roads[i] = new GridRoad { Value = 0 };

        DynamicBuffer<GridWalkable> walkable = em.AddBuffer<GridWalkable>(gridEntity);
        walkable.ResizeUninitialized(gridSize);
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = new GridWalkable { Value = 1 };
    }

    private static Entity CreateRespawnPrefab(EntityManager em)
    {
        Entity entity = em.CreateEntity(
            typeof(Prefab),
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitRespawnPrefab),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitIdleWanderComponent),
            typeof(UnitPrevWorldPos),
            typeof(UnitMoveVisualComponent),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = 0 });
        em.SetComponentData(entity, new UnitGrid { Cell = new int2(0, 0) });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitRespawnPrefab { Prefab = entity });
        em.SetComponentData(entity, LocalTransform.FromPosition(float3.zero));
        return entity;
    }

    private static Entity CreateSoldier(
        EntityManager em,
        byte factionId,
        int2 cell,
        float3 position,
        int health,
        int damage,
        Entity respawnPrefab)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitAttackTraceComponent),
            typeof(UnitAttackAnimationComponent),
            typeof(UnitAnimationSettings),
            typeof(UnitRespawnPrefab),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = 2f,
            CooldownSeconds = 1f,
            Damage = damage,
            TraceVisibleSeconds = 0.01f
        });
        em.SetComponentData(entity, new UnitAnimationSettings
        {
            AttackAnimationSeconds = 0.01f,
            DeathAnimationSeconds = 0.01f
        });
        em.SetComponentData(entity, new UnitRespawnPrefab { Prefab = respawnPrefab });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity GetRespawnQueueEntity(EntityManager em)
    {
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<RespawnQueueTag>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        query.Dispose();
        Assert.AreEqual(1, entities.Length);
        return entities[0];
    }

    private static int CountLivingRuntimeSoldiers(EntityManager em, byte factionId)
    {
        EntityQuery query = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitHealth>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        int count = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.HasComponent<Prefab>(entity))
                continue;
            if (em.GetComponentData<Faction>(entity).Id != factionId)
                continue;
            if (em.GetComponentData<UnitHealth>(entity).Current <= 0)
                continue;

            count++;
        }

        return count;
    }
}
