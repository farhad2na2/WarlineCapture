#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

public sealed class UnitManualMoveRetrySystemTests
{
    [Test]
    public void ManualMoveRetry_ExpiredCooldownClearsBeforePathRestoresOnNextUpdate()
    {
        using var world = new World("UnitManualMoveRetrySystemTests_Cooldown");
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity unit = em.CreateEntity(
            typeof(UnitTarget),
            typeof(ManualMoveOrderTag),
            typeof(UnitPathRetryCooldown));
        em.SetComponentData(unit, new UnitTarget { Cell = new int2(4, 5) });
        em.SetComponentData(unit, new UnitPathRetryCooldown { ResumeFrame = int.MinValue });

        SystemHandle system = world.CreateSystem<UnitManualMoveRetrySystem>();
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<UnitPathRetryCooldown>(unit));
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(unit));

        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitPathRequest>(unit));
        Assert.AreEqual(new int2(4, 5), em.GetComponentData<UnitPathRequest>(unit).Goal);
    }

    [Test]
    public void ManualMoveRetry_RemovesStaleGroupMemberTag()
    {
        using var world = new World("UnitManualMoveRetrySystemTests_StaleGroup");
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity unit = em.CreateEntity(typeof(ManualMoveGroupMemberTag));

        SystemHandle system = world.CreateSystem<UnitManualMoveRetrySystem>();
        system.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<ManualMoveGroupMemberTag>(unit));
    }

    [Test]
    public void ManualMoveRetry_RestoresManualPathRequest()
    {
        using var world = new World("UnitManualMoveRetrySystemTests_ManualPath");
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity unit = em.CreateEntity(typeof(UnitTarget), typeof(ManualMoveOrderTag));
        em.SetComponentData(unit, new UnitTarget { Cell = new int2(6, 7) });

        SystemHandle system = world.CreateSystem<UnitManualMoveRetrySystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitPathRequest>(unit));
        Assert.AreEqual(new int2(6, 7), em.GetComponentData<UnitPathRequest>(unit).Goal);
    }

    [Test]
    public void ManualMoveRetry_RestoresLongDistanceFinalGoal()
    {
        using var world = new World("UnitManualMoveRetrySystemTests_LongDistance");
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity unit = em.CreateEntity(typeof(UnitLongDistanceMove));
        em.SetComponentData(unit, new UnitLongDistanceMove { FinalGoal = new int2(9, 10) });

        SystemHandle system = world.CreateSystem<UnitManualMoveRetrySystem>();
        system.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<UnitTarget>(unit));
        Assert.IsTrue(em.HasComponent<ManualMoveOrderTag>(unit));
        Assert.IsTrue(em.HasComponent<UnitPathRequest>(unit));
        Assert.AreEqual(new int2(9, 10), em.GetComponentData<UnitTarget>(unit).Cell);
        Assert.AreEqual(new int2(9, 10), em.GetComponentData<UnitPathRequest>(unit).Goal);
    }

    private static Entity CreateGrid(EntityManager em)
    {
        Entity grid = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(grid, new GridConfig
        {
            Width = 32,
            Height = 32,
            CellSize = 1f,
            Origin = float3.zero
        });
        return grid;
    }
}
#endif
