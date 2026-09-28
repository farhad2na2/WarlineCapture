using Game.Components;
using Unity.Entities;
using Unity.Burst;
namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct SupportCatalogDisposalSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state) { }
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            foreach(var catalog in SystemAPI.Query<RefRO<SupportCatalogComponent>>())
                if(catalog.ValueRO.OwnsBlob!=0 && catalog.ValueRO.Blob.IsCreated) catalog.ValueRO.Blob.Dispose();
        }
    }
}
