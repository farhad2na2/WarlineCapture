using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Transforms;

public sealed class OperationMapBuildingDestroyedVisualSystemTests
{
    [Test]
    public void Update_InitializesAndTransitionsBakedVisualRootsWithoutDestroyingBuilding()
    {
        using var world = new World("OperationMapBuildingDestroyedVisualSystemTests");
        EntityManager entityManager = world.EntityManager;
        Entity intact = entityManager.CreateEntity(typeof(LocalTransform));
        Entity destroyed = entityManager.CreateEntity(typeof(LocalTransform));
        Entity building = entityManager.CreateEntity(
            typeof(UnitHealth),
            typeof(StaticGridBlocker),
            typeof(OperationMapBuildingPresentation));
        entityManager.SetComponentData(intact, LocalTransform.FromPositionRotationScale(
            default, Unity.Mathematics.quaternion.identity, 1f));
        entityManager.SetComponentData(destroyed, LocalTransform.FromPositionRotationScale(
            default, Unity.Mathematics.quaternion.identity, 1f));
        entityManager.SetComponentData(building, new UnitHealth { Current = 100, Max = 100 });
        entityManager.SetComponentData(building, new OperationMapBuildingPresentation
        {
            IntactVisualRoot = intact,
            DestroyedVisualRoot = destroyed,
            IntactVisibleScale = 1f,
            DestroyedVisibleScale = 1f,
            State = byte.MaxValue
        });

        UpdateSystem(world);
        Assert.That(entityManager.GetComponentData<LocalTransform>(intact).Scale, Is.EqualTo(1f));
        Assert.That(entityManager.GetComponentData<LocalTransform>(destroyed).Scale, Is.EqualTo(0f));
        Assert.That(entityManager.GetComponentData<OperationMapBuildingPresentation>(building).State, Is.Zero);

        entityManager.SetComponentData(building, new UnitHealth { Current = 0, Max = 100 });
        UpdateSystem(world);

        Assert.That(entityManager.Exists(building), Is.True);
        Assert.That(entityManager.HasComponent<StaticGridBlocker>(building), Is.True);
        Assert.That(entityManager.GetComponentData<LocalTransform>(intact).Scale, Is.EqualTo(0f));
        Assert.That(entityManager.GetComponentData<LocalTransform>(destroyed).Scale, Is.EqualTo(1f));
        Assert.That(entityManager.GetComponentData<OperationMapBuildingPresentation>(building).State, Is.EqualTo(1));
    }

    private static void UpdateSystem(World world)
    {
        SystemHandle handle = world.CreateSystem<OperationMapBuildingDestroyedVisualSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<OperationMapBuildingDestroyedVisualSystem>(handle)
            .OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
        world.DestroySystem(handle);
    }
}
