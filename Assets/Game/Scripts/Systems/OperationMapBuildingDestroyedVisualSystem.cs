using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct OperationMapBuildingDestroyedVisualSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransforms;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OperationMapBuildingPresentation>();
            _localTransforms = state.GetComponentLookup<LocalTransform>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localTransforms.Update(ref state);
            state.Dependency = new ApplyBuildingVisualStateJob
            {
                LocalTransforms = _localTransforms
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct ApplyBuildingVisualStateJob : IJobEntity
        {
            public ComponentLookup<LocalTransform> LocalTransforms;

            private void Execute(in UnitHealth health, ref OperationMapBuildingPresentation presentation)
            {
                byte targetState = health.Current <= 0 ? (byte)1 : (byte)0;
                if (presentation.State == targetState)
                    return;

                SetScale(
                    presentation.IntactVisualRoot,
                    targetState == 0 ? presentation.IntactVisibleScale : 0f);
                SetScale(
                    presentation.DestroyedVisualRoot,
                    targetState == 1 ? presentation.DestroyedVisibleScale : 0f);
                presentation.State = targetState;
            }

            private void SetScale(Entity entity, float scale)
            {
                if (entity == Entity.Null || !LocalTransforms.HasComponent(entity))
                    return;

                LocalTransform transform = LocalTransforms[entity];
                transform.Scale = scale;
                LocalTransforms[entity] = transform;
            }
        }
    }
}
