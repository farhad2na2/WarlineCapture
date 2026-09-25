using Game.Components;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class OperationsIntroductionPauseTests
{
    [Test]
    public void BriefingFreezesSimulationAndHandoffRestoresItsPriorState()
    {
        float original = Time.timeScale;
        using var world = new World("Operations introduction pause");
        try
        {
            Time.timeScale = 1;
            var em = world.EntityManager;
            var gameplay = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
            em.SetComponentData(gameplay,new RuntimeGameplayStateComponent { PlayRequested=1,SimulationActive=1 });
            var root = em.CreateEntity(typeof(OperationsReconMissionComponent),typeof(OperationsReconIntroduction));
            em.SetComponentData(root,new OperationsReconMissionComponent { SessionId="session.operations.aabb0001",Phase=OperationsReconPhase.Playing });
            var pause = world.GetOrCreateSystem<MissionDefensePauseSystem>();
            pause.Update(world.Unmanaged);
            Assert.That(Time.timeScale,Is.Zero);
            Assert.That(em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive,Is.Zero);
            em.SetComponentData(root,new OperationsReconIntroduction { Stage=6 });
            pause.Update(world.Unmanaged);
            Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive,Is.EqualTo(1));
        }
        finally { Time.timeScale=original; }
    }
}
