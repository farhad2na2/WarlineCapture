using Game.Composition;
using NUnit.Framework;
using Unity.Entities;

namespace Game.Tests.Editor
{
    public sealed class GpuAnimationTeardownFenceTests
    {
        private struct PendingTag : IComponentData
        {
        }

        [Test]
        public void TryFlushPendingStructuralChanges_ReturnsFalse_WhenWorldIsUnavailable()
        {
            Assert.That(GpuAnimationTeardownFence.TryFlushPendingStructuralChanges(null), Is.False);

            World disposedWorld = new("DisposedGpuAnimationTeardownFenceWorld");
            disposedWorld.Dispose();

            Assert.That(GpuAnimationTeardownFence.TryFlushPendingStructuralChanges(disposedWorld), Is.False);
        }

        [Test]
        public void TryFlushPendingStructuralChanges_ReturnsFalse_WhenPlaybackSystemIsUnavailable()
        {
            using World world = new("GpuAnimationTeardownFenceWithoutPlaybackSystem");

            Assert.That(GpuAnimationTeardownFence.TryFlushPendingStructuralChanges(world), Is.False);
        }

        [Test]
        public void TryFlushPendingStructuralChanges_PlaysBackCommandsBeforeTargetTeardown()
        {
            using World world = new("GpuAnimationTeardownFenceWorld");
            EndSimulationEntityCommandBufferSystem playback =
                world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            Entity target = world.EntityManager.CreateEntity();
            EntityCommandBuffer commands = playback.CreateCommandBuffer();
            commands.AddComponent<PendingTag>(target);

            Assert.That(world.EntityManager.HasComponent<PendingTag>(target), Is.False);
            Assert.That(GpuAnimationTeardownFence.TryFlushPendingStructuralChanges(world), Is.True);
            Assert.That(world.EntityManager.HasComponent<PendingTag>(target), Is.True);

            Assert.DoesNotThrow(() => world.EntityManager.DestroyEntity(target));
            Assert.DoesNotThrow(playback.Update);
        }
    }
}
