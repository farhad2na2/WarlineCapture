using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;

public sealed partial class BuildingDefenseAttackSystemTests
{
    [Test]
    public void FinishedSkirmish_DoesNotAcquireFireOrCreateFeedback()
    {
        using var world = new World(nameof(FinishedSkirmish_DoesNotAcquireFireOrCreateFeedback));
        var em = world.EntityManager;
        var match = em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(match, new SkirmishMatchState { Phase = SkirmishPhase.Finished });
        var tower = CreateGuardTower(em, float3.zero, 2, 2);
        var target = CreateTarget(em, new float3(5, 0, 0), 100, 1);
        GetAudioRequests(em).Clear();
        var attack = world.CreateSystem<BuildingDefenseAttackSystem>();

        Update(world, attack, 1, 1);

        Assert.AreEqual(100, GetHealth(em, target));
        Assert.AreEqual(0, em.GetBuffer<BuildingDefenseAttackSlot>(tower).Length);
        Assert.AreEqual(0, GetAudioRequests(em).Length);
        Assert.IsFalse(em.HasComponent<RecentAttacker>(target));
        Assert.IsFalse(em.HasComponent<RecentDamageHealthBarVisibility>(target));
    }

    [Test]
    public void SkirmishFinishesWithCachedTarget_DoesNotFireReadyShot()
    {
        using var world = new World(nameof(SkirmishFinishesWithCachedTarget_DoesNotFireReadyShot));
        var em = world.EntityManager;
        var match = em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(match, new SkirmishMatchState { Phase = SkirmishPhase.Playing });
        var tower = CreateGuardTower(em, float3.zero, 2, 1);
        var target = CreateTarget(em, new float3(5, 0, 0), 100, 1);
        var attack = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attack, .1, .1f);
        Assert.AreEqual(90, GetHealth(em, target));
        Assert.AreEqual(1, em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0].ShotCounter);
        GetAudioRequests(em).Clear();
        em.SetComponentData(match, new SkirmishMatchState { Phase = SkirmishPhase.Finished });

        Update(world, attack, 2, 1.9f);

        Assert.AreEqual(90, GetHealth(em, target));
        Assert.AreEqual(1, em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0].ShotCounter);
        Assert.AreEqual(0, GetAudioRequests(em).Length);
    }
}
