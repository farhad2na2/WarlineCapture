using System;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public sealed class OperationMapBuildingDestructionSystemTests
{
    public static void RunFocusedValidation()
    {
        var suite = new OperationMapBuildingDestructionSystemTests();
        Action[] tests =
        {
            suite.Update_InitializesAndTransitionsCompleteBakedHierarchiesOnce,
            suite.Update_MissingDestroyedEquivalentHidesIntactHierarchyWithoutCreatingOrphans,
            suite.Update_VirtualizedBuildingChangesOnlyCanonicalStateAndAppendsOneEvent,
            suite.Update_VirtualizedBuildingWithoutUniqueEventBufferFailsClosed
        };

        foreach (Action test in tests)
            test();

        Debug.Log($"[OperationMapBuildingDestructionValidation] result=Passed tests={tests.Length}");
    }

    [Test]
    public void Update_InitializesAndTransitionsCompleteBakedHierarchiesOnce()
    {
        using var world = new World("OperationMapBuildingDestructionSystemTests");
        EntityManager entityManager = world.EntityManager;
        Entity intact = entityManager.CreateEntity(typeof(LocalTransform));
        Entity destroyed = entityManager.CreateEntity(typeof(LocalTransform));
        Entity building = CreateBuilding(entityManager, intact, destroyed, 100, byte.MaxValue);
        Entity intactRoof = CreateAttachment(entityManager, building, intact, 0);
        Entity intactInterior = CreateAttachment(entityManager, building, intact, 0);
        Entity intactShop = CreateAttachment(entityManager, building, intact, 0);
        Entity intactTent = CreateAttachment(entityManager, building, intact, 0);
        Entity destroyedRoof = CreateAttachment(entityManager, building, destroyed, 1);
        Entity intactRoofDetail = CreateVisualDescendant(entityManager, intactRoof);
        Entity intactInteriorDetail = CreateVisualDescendant(entityManager, intactInterior);
        Entity intactShopDetail = CreateVisualDescendant(entityManager, intactShop);
        Entity intactTentDetail = CreateVisualDescendant(entityManager, intactTent);
        Entity destroyedRoofDetail = CreateVisualDescendant(entityManager, destroyedRoof);
        entityManager.SetComponentData(intact, LocalTransform.FromPositionRotationScale(
            default, Unity.Mathematics.quaternion.identity, 1f));
        entityManager.SetComponentData(destroyed, LocalTransform.FromPositionRotationScale(
            default, Unity.Mathematics.quaternion.identity, 1f));
        UpdateSystem(world);
        Assert.That(entityManager.GetComponentData<LocalTransform>(intact).Scale, Is.EqualTo(1f));
        Assert.That(entityManager.GetComponentData<LocalTransform>(destroyed).Scale, Is.EqualTo(0f));
        Assert.That(entityManager.GetComponentData<OperationMapBuildingPresentation>(building).State, Is.Zero);
        Assert.That(entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(building), Is.False);

        entityManager.SetComponentData(building, new UnitHealth { Current = 0, Max = 100 });
        int entityCountBeforeDestruction = entityManager.UniversalQuery.CalculateEntityCount();
        UpdateSystem(world);

        Assert.That(entityManager.Exists(building), Is.True);
        Assert.That(entityManager.HasComponent<StaticGridBlocker>(building), Is.True);
        Assert.That(entityManager.GetComponentData<LocalTransform>(intact).Scale, Is.EqualTo(0f));
        Assert.That(entityManager.GetComponentData<LocalTransform>(destroyed).Scale, Is.EqualTo(1f));
        Assert.That(entityManager.GetComponentData<OperationMapBuildingPresentation>(building).State, Is.EqualTo(1));
        Assert.That(entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(building), Is.True);
        Assert.That(entityManager.UniversalQuery.CalculateEntityCount(), Is.EqualTo(entityCountBeforeDestruction));
        AssertAttachment(entityManager, intactRoof, building, intact, 0);
        AssertAttachment(entityManager, intactInterior, building, intact, 0);
        AssertAttachment(entityManager, intactShop, building, intact, 0);
        AssertAttachment(entityManager, intactTent, building, intact, 0);
        AssertAttachment(entityManager, destroyedRoof, building, destroyed, 1);
        AssertVisualDescendant(entityManager, intactRoofDetail, intactRoof);
        AssertVisualDescendant(entityManager, intactInteriorDetail, intactInterior);
        AssertVisualDescendant(entityManager, intactShopDetail, intactShop);
        AssertVisualDescendant(entityManager, intactTentDetail, intactTent);
        AssertVisualDescendant(entityManager, destroyedRoofDetail, destroyedRoof);

        entityManager.SetComponentData(building, new UnitHealth { Current = 100, Max = 100 });
        UpdateSystem(world);

        Assert.That(entityManager.GetComponentData<LocalTransform>(intact).Scale, Is.EqualTo(0f));
        Assert.That(entityManager.GetComponentData<LocalTransform>(destroyed).Scale, Is.EqualTo(1f));
        Assert.That(entityManager.GetComponentData<OperationMapBuildingPresentation>(building).State, Is.EqualTo(1));
        Assert.That(entityManager.UniversalQuery.CalculateEntityCount(), Is.EqualTo(entityCountBeforeDestruction));
        Assert.That(
            entityManager.GetComponentData<OperationMapBuildingComponent>(building).BlockerPolicy,
            Is.EqualTo(OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked));
    }

    [Test]
    public void Update_MissingDestroyedEquivalentHidesIntactHierarchyWithoutCreatingOrphans()
    {
        using var world = new World("OperationMapBuildingDestructionMissingDestroyedTests");
        EntityManager entityManager = world.EntityManager;
        Entity intact = entityManager.CreateEntity(typeof(LocalTransform));
        Entity building = CreateBuilding(entityManager, intact, Entity.Null, 0, 0);
        Entity nestedShop = CreateAttachment(entityManager, building, intact, 0);
        Entity nestedShopDetail = CreateVisualDescendant(entityManager, nestedShop);
        entityManager.SetComponentData(intact, LocalTransform.FromPositionRotationScale(
            default, Unity.Mathematics.quaternion.identity, 1f));
        int entityCount = entityManager.UniversalQuery.CalculateEntityCount();

        UpdateSystem(world);
        UpdateSystem(world);

        Assert.That(entityManager.GetComponentData<LocalTransform>(intact).Scale, Is.EqualTo(0f));
        Assert.That(entityManager.GetComponentData<OperationMapBuildingPresentation>(building).State, Is.EqualTo(1));
        Assert.That(entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(building), Is.True);
        Assert.That(entityManager.HasComponent<StaticGridBlocker>(building), Is.True);
        Assert.That(entityManager.UniversalQuery.CalculateEntityCount(), Is.EqualTo(entityCount));
        AssertAttachment(entityManager, nestedShop, building, intact, 0);
        AssertVisualDescendant(entityManager, nestedShopDetail, nestedShop);
    }

    [Test]
    public void Update_VirtualizedBuildingChangesOnlyCanonicalStateAndAppendsOneEvent()
    {
        using var world = new World("OperationMapVirtualizedBuildingDestructionTests");
        EntityManager entityManager = world.EntityManager;
        Entity bufferOwner = entityManager.CreateEntity();
        entityManager.AddBuffer<OperationMapRenderStateChangeComponent>(bufferOwner);
        Entity unrelatedProxy = entityManager.CreateEntity(typeof(LocalTransform));
        entityManager.SetComponentData(
            unrelatedProxy,
            LocalTransform.FromPositionRotationScale(
                new Unity.Mathematics.float3(4f, 5f, 6f),
                Unity.Mathematics.quaternion.identity,
                3f));
        Entity building = CreateVirtualizedBuilding(entityManager, 0, 7);
        int entityCountBeforeDestruction =
            entityManager.UniversalQuery.CalculateEntityCount();

        UpdateSystem(world);
        DynamicBuffer<OperationMapRenderStateChangeComponent> changes =
            entityManager.GetBuffer<OperationMapRenderStateChangeComponent>(bufferOwner);

        Assert.That(
            entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(
                building),
            Is.True);
        Assert.That(
            entityManager.HasComponent<OperationMapBuildingPresentation>(building),
            Is.False);
        Assert.That(entityManager.HasComponent<StaticGridBlocker>(building), Is.True);
        Assert.That(entityManager.UniversalQuery.CalculateEntityCount(),
            Is.EqualTo(entityCountBeforeDestruction));
        Assert.That(entityManager.GetComponentData<LocalTransform>(unrelatedProxy).Scale,
            Is.EqualTo(3f));
        Assert.That(changes.Length, Is.EqualTo(1));
        Assert.That(changes[0].StateOwnerIndex, Is.EqualTo(7));
        Assert.That(
            changes[0].VisualState,
            Is.EqualTo(OperationMapRenderVisualState.Destroyed));
        Assert.That(changes[0].ChangeVersion, Is.EqualTo(1u));

        entityManager.SetComponentData(
            building,
            new UnitHealth { Current = 100, Max = 100 });
        UpdateSystem(world);
        changes = entityManager.GetBuffer<OperationMapRenderStateChangeComponent>(
            bufferOwner);

        Assert.That(changes.Length, Is.EqualTo(1));
        Assert.That(
            entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(
                building),
            Is.True);
    }

    [Test]
    public void Update_VirtualizedBuildingWithoutUniqueEventBufferFailsClosed()
    {
        using var world = new World("OperationMapVirtualizedBuildingMissingBufferTests");
        CreateVirtualizedBuilding(world.EntityManager, 0, 0);

        Assert.That(
            () => UpdateSystem(world),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains(
                "exactly one map-owned state-change buffer"));
    }

    private static Entity CreateBuilding(
        EntityManager entityManager,
        Entity intact,
        Entity destroyed,
        int currentHealth,
        byte state)
    {
        Entity building = entityManager.CreateEntity(
            typeof(UnitHealth),
            typeof(StaticGridBlocker),
            typeof(OperationMapBuildingComponent),
            typeof(OperationMapBuildingPresentation),
            typeof(OperationMapBuildingDestroyedComponent));
        entityManager.SetComponentEnabled<OperationMapBuildingDestroyedComponent>(building, false);
        entityManager.SetComponentData(building, new UnitHealth { Current = currentHealth, Max = 100 });
        entityManager.SetComponentData(building, new OperationMapBuildingComponent
        {
            BlockerPolicy = OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked
        });
        entityManager.SetComponentData(building, new OperationMapBuildingPresentation
        {
            IntactVisualRoot = intact,
            DestroyedVisualRoot = destroyed,
            IntactVisibleScale = 1f,
            DestroyedVisibleScale = 1f,
            State = state
        });
        return building;
    }

    private static Entity CreateVirtualizedBuilding(
        EntityManager entityManager,
        int currentHealth,
        int stateOwnerIndex)
    {
        Entity building = entityManager.CreateEntity(
            typeof(UnitHealth),
            typeof(StaticGridBlocker),
            typeof(OperationMapBuildingComponent),
            typeof(OperationMapVirtualizedBuildingPresentationComponent),
            typeof(OperationMapBuildingDestroyedComponent));
        entityManager.SetComponentEnabled<OperationMapBuildingDestroyedComponent>(
            building,
            false);
        entityManager.SetComponentData(
            building,
            new UnitHealth { Current = currentHealth, Max = 100 });
        entityManager.SetComponentData(building, new OperationMapBuildingComponent
        {
            BlockerPolicy = OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked
        });
        entityManager.SetComponentData(
            building,
            new OperationMapVirtualizedBuildingPresentationComponent
            {
                StateOwnerIndex = stateOwnerIndex
            });
        return building;
    }

    private static Entity CreateAttachment(
        EntityManager entityManager,
        Entity building,
        Entity parent,
        byte visualState)
    {
        Entity attachment = entityManager.CreateEntity(
            typeof(LocalTransform),
            typeof(Parent),
            typeof(OperationMapBuildingAttachment));
        entityManager.SetComponentData(attachment, LocalTransform.Identity);
        entityManager.SetComponentData(attachment, new Parent { Value = parent });
        entityManager.SetComponentData(attachment, new OperationMapBuildingAttachment
        {
            Building = building,
            VisualState = visualState
        });
        return attachment;
    }

    private static void AssertAttachment(
        EntityManager entityManager,
        Entity attachment,
        Entity expectedBuilding,
        Entity expectedParent,
        byte expectedVisualState)
    {
        Assert.That(entityManager.Exists(attachment), Is.True);
        Assert.That(entityManager.GetComponentData<Parent>(attachment).Value, Is.EqualTo(expectedParent));
        OperationMapBuildingAttachment ownership =
            entityManager.GetComponentData<OperationMapBuildingAttachment>(attachment);
        Assert.That(ownership.Building, Is.EqualTo(expectedBuilding));
        Assert.That(ownership.VisualState, Is.EqualTo(expectedVisualState));
    }

    private static Entity CreateVisualDescendant(EntityManager entityManager, Entity parent)
    {
        Entity descendant = entityManager.CreateEntity(typeof(LocalTransform), typeof(Parent));
        entityManager.SetComponentData(descendant, LocalTransform.Identity);
        entityManager.SetComponentData(descendant, new Parent { Value = parent });
        return descendant;
    }

    private static void AssertVisualDescendant(
        EntityManager entityManager,
        Entity descendant,
        Entity expectedParent)
    {
        Assert.That(entityManager.Exists(descendant), Is.True);
        Assert.That(entityManager.GetComponentData<Parent>(descendant).Value, Is.EqualTo(expectedParent));
        Assert.That(entityManager.HasComponent<OperationMapBuildingAttachment>(descendant), Is.False);
    }

    private static void UpdateSystem(World world)
    {
        SystemHandle handle = world.CreateSystem<OperationMapBuildingDestructionSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<OperationMapBuildingDestructionSystem>(handle)
            .OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
        world.DestroySystem(handle);
    }
}
