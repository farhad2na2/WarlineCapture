using NUnit.Framework;
using System;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEditor;
using Unity.Transforms;
using UnityEngine;
using Game.Components;
using Game.Rendering;
using Game.Runtime;

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
            tests.AirVehicleDeath_WithDestroyedVisual_HidesAliveVisualAndSpawnsDestroyedVisualWithoutGrid();
            tests.UnitModelSpawn_DoesNotSpawnDuplicateDetailModelWhenDetailedVisualAlreadyExists();
            tests.UnitRenderVisualExclusivity_HidesInactiveLodRootsRecursively();
            tests.UnitRenderVisualExclusivity_DestroyedVehicleHidesAliveRootsAndShowsDestroyedRoot();
            tests.VehicleWreckCleanup_FinalizesExpiredWreckAndDescendants();
            Debug.Log("[CombatDeathFocusedValidation] result=Passed tests=6");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[CombatDeathFocusedValidation] result=Failed");
            ValidationExit.Exit(1);
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
            factionId: FactionIdentity.PlayerFactionId,
            cell: new int2(4, 4),
            position: new float3(4f, 0f, 4f),
            health: 100,
            damage: 10,
            respawnPrefab);
        Entity attacker = CreateSoldier(
            em,
            factionId: FactionIdentity.EnemyFactionId,
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

        Assert.AreEqual(0, CountLivingRuntimeSoldiers(em, FactionIdentity.PlayerFactionId), "The killed soldier must not respawn later.");
        Assert.IsTrue(em.Exists(attacker), "The attacking soldier should remain alive.");
    }

    [Test]
    public void AirVehicleDeath_WithDestroyedVisual_HidesAliveVisualAndSpawnsDestroyedVisualWithoutGrid()
    {
        using var world = new World(nameof(AirVehicleDeath_WithDestroyedVisual_HidesAliveVisualAndSpawnsDestroyedVisualWithoutGrid));
        EntityManager em = world.EntityManager;
        Entity aliveVisual = CreateVisualTree(em);
        Entity linkedOnlyVisual = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(linkedOnlyVisual, LocalTransform.Identity);
        DynamicBuffer<LinkedEntityGroup> linkedVisuals = em.AddBuffer<LinkedEntityGroup>(aliveVisual);
        linkedVisuals.Add(new LinkedEntityGroup { Value = aliveVisual });
        linkedVisuals.Add(new LinkedEntityGroup { Value = linkedOnlyVisual });
        Entity destroyedVisualPrefab = em.CreateEntity(typeof(Prefab), typeof(LocalTransform));
        em.SetComponentData(destroyedVisualPrefab, LocalTransform.Identity);
        Entity airVehicle = em.CreateEntity(
            typeof(UnitHealth),
            typeof(UnitAnimationSettings),
            typeof(UnitAirComponent),
            typeof(LocalTransform),
            typeof(UnitDetailedVisualReference),
            typeof(VehicleDestroyedVisualPrefabReference));
        em.SetComponentData(airVehicle, new UnitHealth { Current = 0, Max = 100 });
        em.SetComponentData(airVehicle, new UnitAnimationSettings { DeathAnimationSeconds = 1f });
        em.SetComponentData(airVehicle, new UnitAirComponent
        {
            HomePosition = new float3(12f, 5f, 0f),
            HomeInitialized = 1,
            Airborne = 1
        });
        em.SetComponentData(airVehicle, LocalTransform.FromPositionRotationScale(new float3(12f, 18f, 0f), quaternion.identity, 1f));
        em.SetComponentData(airVehicle, new UnitDetailedVisualReference { Root = aliveVisual });
        em.SetComponentData(airVehicle, new VehicleDestroyedVisualPrefabReference { Prefab = destroyedVisualPrefab });

        SystemHandle deathSystem = world.CreateSystem<UnitDeathSystem>();
        SystemHandle destroyedVisualSystem = world.CreateSystem<VehicleDestroyedVisualSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        deathSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.Exists(airVehicle), "Air vehicles with destroyed visuals should remain briefly as wreck visuals.");
        Assert.IsTrue(em.HasComponent<VehicleWreckComponent>(airVehicle), "Air vehicles with destroyed visuals should use the same wreck cleanup lifetime.");
        Assert.IsTrue(em.HasComponent<VehicleDestroyedVisualSpawnRequest>(airVehicle), "A destroyed air vehicle should request its destroyed visual even without grid components.");
        Assert.IsFalse(em.HasComponent<StaticGridBlocker>(airVehicle), "Air vehicles without grid data must not become ground blockers.");
        Assert.IsFalse(em.HasComponent<UnitDeathAnimationComponent>(airVehicle), "Destroyed visual vehicles should not also enter the generic death animation path.");

        destroyedVisualSystem.Update(world.Unmanaged);

        AssertVehicleDestroyedAliveHidden(em, aliveVisual, "The alive air vehicle model tree must be hidden when the destroyed visual appears.");
        AssertVehicleDestroyedAliveEntityHidden(em, linkedOnlyVisual, "Linked alive visual entities must also be hidden when the destroyed visual appears.");
        Assert.IsTrue(em.HasComponent<VehicleDestroyedVisualInstanceReference>(airVehicle), "The destroyed visual should be spawned through the production presentation system.");
        Entity destroyedVisual = em.GetComponentData<VehicleDestroyedVisualInstanceReference>(airVehicle).Instance;
        Assert.IsTrue(em.Exists(destroyedVisual));
        Assert.AreEqual(airVehicle, em.GetComponentData<Parent>(destroyedVisual).Value);
    }

    [Test]
    public void UnitModelSpawn_DoesNotSpawnDuplicateDetailModelWhenDetailedVisualAlreadyExists()
    {
        using var world = new World(nameof(UnitModelSpawn_DoesNotSpawnDuplicateDetailModelWhenDetailedVisualAlreadyExists));
        EntityManager em = world.EntityManager;
        Entity detailedVisual = em.CreateEntity(typeof(LocalTransform));
        Entity modelPrefab = em.CreateEntity(typeof(Prefab), typeof(LocalTransform));
        Entity unit = em.CreateEntity(
            typeof(UnitModelPrefabReference),
            typeof(UnitDetailedVisualReference),
            typeof(UnitModelLocalTransform),
            typeof(LocalTransform));
        em.SetComponentData(unit, new UnitModelPrefabReference { Prefab = modelPrefab });
        em.SetComponentData(unit, new UnitDetailedVisualReference { Root = detailedVisual });
        em.SetComponentData(unit, new UnitModelLocalTransform
        {
            Position = float3.zero,
            Rotation = quaternion.identity,
            Scale = 1f
        });
        em.SetComponentData(unit, LocalTransform.Identity);

        SystemHandle modelSpawnSystem = world.CreateSystem<UnitModelSpawnSystem>();
        modelSpawnSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitModelInstanceReference>(unit), "A unit with an authored detailed visual must not spawn a duplicate detail model.");
        Assert.IsFalse(em.HasComponent<UnitModelPrefabReference>(unit), "The detail model spawn request should be consumed when an authored detailed visual already exists.");
    }

    [Test]
    public void UnitRenderVisualExclusivity_HidesInactiveLodRootsRecursively()
    {
        using var world = new World(nameof(UnitRenderVisualExclusivity_HidesInactiveLodRootsRecursively));
        EntityManager em = world.EntityManager;
        Entity detailRoot = CreateVisualTree(em);
        Entity midRoot = CreateVisualTree(em);
        Entity lowRoot = CreateVisualTree(em);
        Entity detailUnit = em.CreateEntity(
            typeof(UnitDetailedVisualReference),
            typeof(UnitMidLodInstanceReference),
            typeof(UnitLowLodInstanceReference),
            typeof(UnitRenderVisualComponent));
        em.SetComponentData(detailUnit, new UnitDetailedVisualReference { Root = detailRoot });
        em.SetComponentData(detailUnit, new UnitMidLodInstanceReference { Instance = midRoot });
        em.SetComponentData(detailUnit, new UnitLowLodInstanceReference { Instance = lowRoot });
        em.SetComponentData(detailUnit, new UnitRenderVisualComponent
        {
            Current = (byte)UnitRenderVisualKind.Detail,
            Desired = (byte)UnitRenderVisualKind.Detail
        });

        Entity midDetailRoot = CreateVisualTree(em);
        Entity midActiveRoot = CreateVisualTree(em);
        Entity midLowRoot = CreateVisualTree(em);
        Entity midUnit = em.CreateEntity(
            typeof(UnitDetailedVisualReference),
            typeof(UnitMidLodInstanceReference),
            typeof(UnitLowLodInstanceReference),
            typeof(UnitRenderVisualComponent));
        em.SetComponentData(midUnit, new UnitDetailedVisualReference { Root = midDetailRoot });
        em.SetComponentData(midUnit, new UnitMidLodInstanceReference { Instance = midActiveRoot });
        em.SetComponentData(midUnit, new UnitLowLodInstanceReference { Instance = midLowRoot });
        em.SetComponentData(midUnit, new UnitRenderVisualComponent
        {
            Current = (byte)UnitRenderVisualKind.Mid,
            Desired = (byte)UnitRenderVisualKind.Mid
        });

        SystemHandle system = world.CreateSystem<UnitRenderVisualExclusivitySystem>();
        system.Update(world.Unmanaged);

        AssertVisible(em, detailRoot, "The detailed root should remain visible when detail is the active visual.");
        AssertHidden(em, midRoot, "The mid LOD root must be hidden while detail is active.");
        AssertHidden(em, lowRoot, "The low LOD root must be hidden while detail is active.");
        AssertHidden(em, midDetailRoot, "The detailed root must be hidden while mid LOD is active.");
        AssertVisible(em, midActiveRoot, "The mid LOD root should become the only visible LOD root.");
        AssertHidden(em, midLowRoot, "The low LOD root must remain hidden while mid LOD is active.");
    }

    [Test]
    public void UnitRenderVisualExclusivity_DestroyedVehicleHidesAliveRootsAndShowsDestroyedRoot()
    {
        using var world = new World(nameof(UnitRenderVisualExclusivity_DestroyedVehicleHidesAliveRootsAndShowsDestroyedRoot));
        EntityManager em = world.EntityManager;
        Entity detailRoot = CreateVisualTree(em);
        Entity midRoot = CreateVisualTree(em);
        Entity unreferencedLinkedRoot = CreateVisualTree(em);
        Entity destroyedRoot = CreateVisualTree(em);
        HideTree(em, destroyedRoot);
        Entity unit = em.CreateEntity(
            typeof(UnitHealth),
            typeof(UnitDetailedVisualReference),
            typeof(UnitMidLodInstanceReference),
            typeof(VehicleDestroyedVisualInstanceReference),
            typeof(UnitRenderVisualComponent));
        DynamicBuffer<LinkedEntityGroup> linkedGroup = em.AddBuffer<LinkedEntityGroup>(unit);
        linkedGroup.Add(new LinkedEntityGroup { Value = unit });
        linkedGroup.Add(new LinkedEntityGroup { Value = unreferencedLinkedRoot });
        em.SetComponentData(unit, new UnitHealth { Current = 0, Max = 100 });
        em.SetComponentData(unit, new UnitDetailedVisualReference { Root = detailRoot });
        em.SetComponentData(unit, new UnitMidLodInstanceReference { Instance = midRoot });
        em.SetComponentData(unit, new VehicleDestroyedVisualInstanceReference { Instance = destroyedRoot });
        em.SetComponentData(unit, new UnitRenderVisualComponent
        {
            Current = (byte)UnitRenderVisualKind.Mid,
            Desired = (byte)UnitRenderVisualKind.Mid
        });

        SystemHandle system = world.CreateSystem<UnitRenderVisualExclusivitySystem>();
        system.Update(world.Unmanaged);

        AssertHidden(em, detailRoot, "Destroyed vehicles must keep the detailed alive model hidden.");
        AssertHidden(em, midRoot, "Destroyed vehicles must keep LOD alive models hidden.");
        AssertHidden(em, unreferencedLinkedRoot, "Destroyed vehicles must also hide original linked visual roots that are not covered by detail/mid/low references.");
        AssertVisible(em, destroyedRoot, "Destroyed vehicles should show only the destroyed visual root.");
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

    private static Entity CreateVisualTree(EntityManager em)
    {
        Entity root = em.CreateEntity(typeof(LocalTransform));
        Entity child = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(root, LocalTransform.Identity);
        em.SetComponentData(child, LocalTransform.Identity);
        DynamicBuffer<Child> children = em.AddBuffer<Child>(root);
        children.Add(new Child { Value = child });
        em.AddComponentData(child, new Parent { Value = root });
        return root;
    }

    private static void HideTree(EntityManager em, Entity root)
    {
        DynamicBuffer<Child> children = em.GetBuffer<Child>(root);
        using NativeList<Entity> childEntities = new(Allocator.Temp);
        for (int i = 0; i < children.Length; i++)
            childEntities.Add(children[i].Value);
        em.AddComponent<DisableRendering>(root);
        em.AddComponent<UnitRenderBudgetCulledTag>(root);
        for (int i = 0; i < childEntities.Length; i++)
        {
            Entity child = childEntities[i];
            em.AddComponent<DisableRendering>(child);
            em.AddComponent<UnitRenderBudgetCulledTag>(child);
        }
    }

    private static void AssertHidden(EntityManager em, Entity root, string message)
    {
        Assert.IsFalse(em.HasComponent<Disabled>(root), message);
        Assert.IsTrue(em.HasComponent<DisableRendering>(root), message);
        Assert.IsTrue(em.HasComponent<UnitRenderBudgetCulledTag>(root), message);
        DynamicBuffer<Child> children = em.GetBuffer<Child>(root);
        for (int i = 0; i < children.Length; i++)
        {
            Entity child = children[i].Value;
            Assert.IsFalse(em.HasComponent<Disabled>(child), message);
            Assert.IsTrue(em.HasComponent<DisableRendering>(child), message);
            Assert.IsTrue(em.HasComponent<UnitRenderBudgetCulledTag>(child), message);
        }
    }

    private static void AssertVisible(EntityManager em, Entity root, string message)
    {
        Assert.IsFalse(em.HasComponent<DisableRendering>(root), message);
        Assert.IsFalse(em.HasComponent<UnitRenderBudgetCulledTag>(root), message);
        DynamicBuffer<Child> children = em.GetBuffer<Child>(root);
        for (int i = 0; i < children.Length; i++)
        {
            Entity child = children[i].Value;
            Assert.IsFalse(em.HasComponent<DisableRendering>(child), message);
            Assert.IsFalse(em.HasComponent<UnitRenderBudgetCulledTag>(child), message);
        }
    }

    private static void AssertVehicleDestroyedAliveHidden(EntityManager em, Entity root, string message)
    {
        AssertVehicleDestroyedAliveEntityHidden(em, root, message);
        DynamicBuffer<Child> children = em.GetBuffer<Child>(root);
        for (int i = 0; i < children.Length; i++)
        {
            Entity child = children[i].Value;
            AssertVehicleDestroyedAliveEntityHidden(em, child, message);
        }
    }

    private static void AssertVehicleDestroyedAliveEntityHidden(EntityManager em, Entity entity, string message)
    {
        Assert.AreEqual(0f, em.GetComponentData<LocalTransform>(entity).Scale, message);
        Assert.IsTrue(em.HasComponent<Disabled>(entity), message);
        Assert.IsTrue(em.HasComponent<UnitRenderBudgetCulledTag>(entity), message);
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
