using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Runtime
{
    internal struct MapSurfaceMeshCache
    {
        public NativeArray<MapSurfaceMeshTriangle> Triangles;
        public NativeArray<MapSurfaceMeshInstance> Instances;
        private Entity owner;
        private uint revision;
        public void Update(EntityManager em,Entity surface,uint currentRevision,JobHandle dependency)
        {
            if(Triangles.IsCreated && owner==surface && revision==currentRevision)return;
            dependency.Complete();Dispose();owner=surface;revision=currentRevision;
            Triangles=em.HasBuffer<MapSurfaceMeshTriangle>(surface)?em.GetBuffer<MapSurfaceMeshTriangle>(surface,true).ToNativeArray(Allocator.Persistent):new NativeArray<MapSurfaceMeshTriangle>(0,Allocator.Persistent);
            Instances=em.HasBuffer<MapSurfaceMeshInstance>(surface)?em.GetBuffer<MapSurfaceMeshInstance>(surface,true).ToNativeArray(Allocator.Persistent):new NativeArray<MapSurfaceMeshInstance>(0,Allocator.Persistent);
        }
        public void Dispose(){if(Triangles.IsCreated)Triangles.Dispose();if(Instances.IsCreated)Instances.Dispose();}
    }
}
