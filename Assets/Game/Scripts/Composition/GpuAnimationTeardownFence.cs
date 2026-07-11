using Unity.Entities;

namespace Game.Composition
{
    internal static class GpuAnimationTeardownFence
    {
        public static bool TryFlushPendingStructuralChanges(World world)
        {
            if (world == null || !world.IsCreated)
                return false;

            EndSimulationEntityCommandBufferSystem endSimulation =
                world.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            if (endSimulation == null || !endSimulation.Enabled)
                return false;

            world.EntityManager.CompleteAllTrackedJobs();
            endSimulation.Update();
            return true;
        }
    }
}
