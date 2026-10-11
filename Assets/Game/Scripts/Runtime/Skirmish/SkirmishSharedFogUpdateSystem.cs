using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ScanIntelCommandSystem))]
    [UpdateBefore(typeof(UnitEngagementSystem))]
    [UpdateBefore(typeof(SkirmishEnemyStrategySystem))]
    public partial struct SkirmishSharedFogUpdateSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SkirmishFogStateComponent>();
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.Time.DeltaTime <= 0) return;
            state.Dependency.Complete();
            using var query = state.EntityManager.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishFogStateComponent));
            using var sessions = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < sessions.Length; i++) SkirmishFogService.UpdateSession(state.EntityManager, sessions[i]);
        }
    }
}
