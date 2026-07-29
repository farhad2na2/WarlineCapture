using System;
using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

namespace Game.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(UpdatePresentationSystemGroup))]
    [UpdateBefore(typeof(EntitiesGraphicsSystem))]
    [BurstCompile]
    public partial struct OperationMapRenderProxyApplySystem : ISystem
    {
        private EntityQuery _commandOwnerQuery;
        private EntityQuery _slotQuery;
        private NativeArray<OperationMapRenderSlotApplyFailure> _slotFailures;
        private ComponentTypeHandle<OperationMapRenderProxySlotComponent>
            _proxySlotType;
        private ComponentTypeHandle<LocalToWorld> _localToWorldType;
        private ComponentTypeHandle<RenderBounds> _renderBoundsType;
        private ComponentTypeHandle<MaterialMeshInfo> _materialMeshInfoType;
        private ComponentTypeHandle<URPMaterialPropertyBaseColor> _baseColorType;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _commandOwnerQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>(),
                ComponentType.ReadOnly<OperationMapRenderSlotCommandComponent>());
            _slotQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadWrite<
                        OperationMapRenderProxySlotComponent>(),
                    ComponentType.ReadWrite<LocalToWorld>(),
                    ComponentType.ReadWrite<RenderBounds>(),
                    ComponentType.ReadWrite<MaterialMeshInfo>(),
                    ComponentType.ReadWrite<URPMaterialPropertyBaseColor>()
                },
                Options = EntityQueryOptions.IgnoreComponentEnabledState
            });
            _proxySlotType = state.GetComponentTypeHandle<
                OperationMapRenderProxySlotComponent>(false);
            _localToWorldType =
                state.GetComponentTypeHandle<LocalToWorld>(false);
            _renderBoundsType =
                state.GetComponentTypeHandle<RenderBounds>(false);
            _materialMeshInfoType =
                state.GetComponentTypeHandle<MaterialMeshInfo>(false);
            _baseColorType = state.GetComponentTypeHandle<
                URPMaterialPropertyBaseColor>(false);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            CompleteAndDispose(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int commandOwnerCount = _commandOwnerQuery.CalculateEntityCount();
            if (commandOwnerCount == 0)
            {
                CompleteAndDispose(ref state);
                return;
            }
            if (commandOwnerCount != 1)
            {
                throw new InvalidOperationException(
                    "Render proxy apply requires exactly one slot-command owner.");
            }

            OperationMapRenderDatabaseComponent database =
                _commandOwnerQuery.GetSingleton<
                    OperationMapRenderDatabaseComponent>();
            DynamicBuffer<OperationMapRenderSlotCommandComponent> commands =
                _commandOwnerQuery.GetSingletonBuffer<
                    OperationMapRenderSlotCommandComponent>(true);
            int slotCount = _slotQuery.CalculateEntityCount();
            if (!database.Blob.IsCreated ||
                commands.Length <= 0 ||
                commands.Length != slotCount)
            {
                throw new InvalidOperationException(
                    "Render proxy apply requires one fixed command per fixed slot.");
            }

            EnsureFailureCapacity(ref state, commands);
            _proxySlotType.Update(ref state);
            _localToWorldType.Update(ref state);
            _renderBoundsType.Update(ref state);
            _materialMeshInfoType.Update(ref state);
            _baseColorType.Update(ref state);

            var applyJob = new OperationMapRenderSlotApplyJob
            {
                Database = database.Blob,
                SlotCommands = commands.AsNativeArray(),
                ProxySlotType = _proxySlotType,
                LocalToWorldType = _localToWorldType,
                RenderBoundsType = _renderBoundsType,
                MaterialMeshInfoType = _materialMeshInfoType,
                BaseColorType = _baseColorType,
                SlotFailures = _slotFailures
            };
            state.Dependency =
                applyJob.ScheduleParallel(_slotQuery, state.Dependency);
        }

        private void EnsureFailureCapacity(
            ref SystemState state,
            DynamicBuffer<OperationMapRenderSlotCommandComponent> commands)
        {
            if (_slotFailures.IsCreated &&
                _slotFailures.Length == commands.Length)
                return;

            CompleteAndDispose(ref state);
            for (int slotIndex = 0;
                 slotIndex < commands.Length;
                 slotIndex++)
            {
                if (commands[slotIndex].SlotIndex != slotIndex)
                {
                    throw new InvalidOperationException(
                        "Render proxy commands are not in immutable slot order.");
                }
            }
            _slotFailures =
                new NativeArray<OperationMapRenderSlotApplyFailure>(
                    commands.Length,
                    Allocator.Persistent,
                    NativeArrayOptions.ClearMemory);
        }

        private void CompleteAndDispose(ref SystemState state)
        {
            if (!_slotFailures.IsCreated)
                return;
            state.Dependency.Complete();
            _slotFailures.Dispose();
        }
    }
}
