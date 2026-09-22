using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateBefore(typeof(InitialUnitsSpawnSystem))]
    public partial struct OperationsReconStartupSystem : ISystem
    {
        private EntityQuery legacySpawns;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OperationsReconMissionComponent>();
            legacySpawns = new EntityQueryBuilder(Allocator.Temp).WithAll<InitialUnitsSpawnConfig>()
                .WithNone<CustomGameStartupStateComponent>().Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Streamed scenery must not add its legacy army to the finite mission force.
            if (legacySpawns.IsEmptyIgnoreFilter) return;
            var commands = new EntityCommandBuffer(Allocator.Temp);
            commands.RemoveComponent<InitialUnitsSpawnConfig>(legacySpawns, EntityQueryCaptureMode.AtPlayback);
            commands.RemoveComponent<InitialUnitsBlockerChurnConfig>(legacySpawns, EntityQueryCaptureMode.AtRecord);
            commands.RemoveComponent<InitialUsableFuelStorageSeedPending>(legacySpawns, EntityQueryCaptureMode.AtRecord);
            commands.Playback(state.EntityManager);
            commands.Dispose();
        }
    }
}
