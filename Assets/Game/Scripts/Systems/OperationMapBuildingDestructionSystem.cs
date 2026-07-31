using Game.Components;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitAttackSystem))]
    public partial struct OperationMapBuildingDestructionSystem : ISystem
    {
        private EntityQuery _presenceQuery;
        private EntityQuery _residentQuery;
        private EntityQuery _virtualizedQuery;
        private EntityQuery _overlapQuery;
        private EntityQuery _stateChangeBufferQuery;
        private ComponentLookup<OperationMapRenderStateChangeSequenceComponent>
            _stateChangeSequenceLookup;
        private ComponentLookup<LocalTransform> _localTransforms;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _presenceQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                Any = new[]
                {
                    ComponentType.ReadOnly<OperationMapBuildingPresentation>(),
                    ComponentType.ReadOnly<
                        OperationMapVirtualizedBuildingPresentationComponent>()
                }
            });
            _residentQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<OperationMapBuildingPresentation>());
            _virtualizedQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<
                    OperationMapVirtualizedBuildingPresentationComponent>());
            _overlapQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<OperationMapBuildingPresentation>(),
                ComponentType.ReadOnly<
                    OperationMapVirtualizedBuildingPresentationComponent>());
            _stateChangeBufferQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<OperationMapRenderStateChangeComponent>(),
                ComponentType.ReadWrite<
                    OperationMapRenderStateChangeSequenceComponent>());
            state.RequireForUpdate(_presenceQuery);
            _localTransforms = state.GetComponentLookup<LocalTransform>();
            _stateChangeSequenceLookup = state.GetComponentLookup<
                OperationMapRenderStateChangeSequenceComponent>(false);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_overlapQuery.IsEmptyIgnoreFilter)
            {
                throw new System.InvalidOperationException(
                    "A building cannot own resident and virtualized presentation simultaneously.");
            }

            JobHandle dependency = state.Dependency;
            if (!_residentQuery.IsEmptyIgnoreFilter)
            {
                _localTransforms.Update(ref state);
                dependency = new ApplyResidentBuildingVisualStateJob
                {
                    LocalTransforms = _localTransforms
                }.Schedule(dependency);
            }

            if (!_virtualizedQuery.IsEmptyIgnoreFilter)
            {
                int virtualizedBuildingCount =
                    _virtualizedQuery.CalculateEntityCount();
                int bufferOwnerCount = _stateChangeBufferQuery.CalculateEntityCount();
                if (bufferOwnerCount != 1)
                {
                    throw new System.InvalidOperationException(
                        "Virtualized building destruction requires exactly one map-owned " +
                        $"state-change buffer, found {bufferOwnerCount}.");
                }

                Entity bufferOwner = _stateChangeBufferQuery.GetSingletonEntity();
                DynamicBuffer<OperationMapRenderStateChangeComponent> stateChanges =
                    state.EntityManager.GetBuffer<
                        OperationMapRenderStateChangeComponent>(bufferOwner);
                OperationMapRenderStateChangeSequenceComponent sequence =
                    state.EntityManager.GetComponentData<
                        OperationMapRenderStateChangeSequenceComponent>(bufferOwner);
                if (stateChanges.Length > virtualizedBuildingCount ||
                    (ulong)sequence.LastPublishedVersion +
                    (uint)virtualizedBuildingCount > uint.MaxValue)
                {
                    throw new System.InvalidOperationException(
                        "Virtualized building state-change records must remain bounded " +
                        "and their versions must not overflow.");
                }

                _stateChangeSequenceLookup.Update(ref state);
                dependency = new ApplyVirtualizedBuildingStateJob
                {
                    StateChanges = stateChanges,
                    StateChangeSequenceLookup = _stateChangeSequenceLookup,
                    StateChangeOwner = bufferOwner,
                    NextChangeVersion = sequence.LastPublishedVersion + 1
                }.Schedule(dependency);
            }

            state.Dependency = dependency;
        }

        [BurstCompile]
        [WithDisabled(typeof(OperationMapBuildingDestroyedComponent))]
        private partial struct ApplyResidentBuildingVisualStateJob : IJobEntity
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

        [BurstCompile]
        [WithDisabled(typeof(OperationMapBuildingDestroyedComponent))]
        private partial struct ApplyVirtualizedBuildingStateJob : IJobEntity
        {
            public DynamicBuffer<OperationMapRenderStateChangeComponent> StateChanges;
            public ComponentLookup<
                OperationMapRenderStateChangeSequenceComponent>
                StateChangeSequenceLookup;
            public Entity StateChangeOwner;
            public uint NextChangeVersion;

            private void Execute(
                in UnitHealth health,
                in OperationMapBuildingComponent building,
                in OperationMapVirtualizedBuildingPresentationComponent presentation,
                EnabledRefRW<OperationMapBuildingDestroyedComponent> destroyed)
            {
                if (destroyed.ValueRO ||
                    health.Current > 0 ||
                    building.BlockerPolicy !=
                    OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked)
                {
                    return;
                }

                StateChanges.Add(new OperationMapRenderStateChangeComponent
                {
                    StateOwnerIndex = presentation.StateOwnerIndex,
                    VisualState = OperationMapRenderVisualState.Destroyed,
                    ChangeVersion = NextChangeVersion
                });
                StateChangeSequenceLookup[StateChangeOwner] =
                    new OperationMapRenderStateChangeSequenceComponent
                    {
                        LastPublishedVersion = NextChangeVersion
                    };
                NextChangeVersion++;
                destroyed.ValueRW = true;
            }
        }
    }
}
