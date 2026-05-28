using Unity.Collections;
using Unity.Entities;

public readonly struct InitialSpawnStructuralApplySystem
{
    public struct Context
    {
        public EntityCommandBuffer Ecb;

        public Context(Allocator allocator)
        {
            Ecb = new EntityCommandBuffer(allocator);
        }
    }

    public Context Create(Allocator allocator)
    {
        return new Context(allocator);
    }

    public void PlaybackAndDispose(EntityManager em, ref Context context)
    {
        context.Ecb.Playback(em);
        context.Ecb.Dispose();
    }
}
