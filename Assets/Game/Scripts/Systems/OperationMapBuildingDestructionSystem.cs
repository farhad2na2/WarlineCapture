using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct OperationMapBuildingDestructionSystem : ISystem
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
        [WithDisabled(typeof(OperationMapBuildingDestroyedComponent))]
        private partial struct ApplyBuildingVisualStateJob : IJobEntity
        {
            public ComponentLookup<LocalTransform> LocalTransforms;

            private void Execute(
                in UnitHealth health,
                in OperationMapBuildingComponent building,
                ref OperationMapBuildingPresentation presentation,
                EnabledRefRW<OperationMapBuildingDestroyedComponent> destroyed)
            {
                if (destroyed.ValueRO)
                    return;

                if (health.Current <= 0)
                {
                    if (building.BlockerPolicy != OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked)
                        return;

                    SetScale(presentation.IntactVisualRoot, 0f);
                    SetScale(presentation.DestroyedVisualRoot, presentation.DestroyedVisibleScale);
                    presentation.State = 1;
                    destroyed.ValueRW = true;
                    return;
                }

                if (presentation.State != 0)
                {
                    SetScale(presentation.IntactVisualRoot, presentation.IntactVisibleScale);
                    SetScale(presentation.DestroyedVisualRoot, 0f);
                    presentation.State = 0;
                }
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
