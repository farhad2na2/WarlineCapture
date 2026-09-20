using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    /// <summary>
    /// One-shot-safe runtime pass: buildings already at grade no-op because
    /// their Y falls outside the stale raised band.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(OperationMapBuildingDestructionSystem))]
    public partial struct CityCrossroadsFloatingShelfBuildingCorrectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OperationMapBuildingComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new CorrectRaisedShelfBuildingsJob().ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(OperationMapBuildingComponent))]
        private partial struct CorrectRaisedShelfBuildingsJob : IJobEntity
        {
            public void Execute(ref LocalTransform transform)
            {
                CityCrossroadsFloatingShelfBuildingCorrection.TryCorrect(ref transform);
            }
        }
    }
}
