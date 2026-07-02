using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateBefore(typeof(EngageTargetValidateSystem))]
    public partial struct BaseBreachOrderSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BaseBreachOrder>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            state.Dependency = new ProcessBaseBreachOrderJob
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                HealthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true),
                GridLookup = SystemAPI.GetComponentLookup<UnitGrid>(true),
                UnitTargetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true),
                UnitPathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true),
                UnitPathFollowLookup = SystemAPI.GetComponentLookup<UnitPathFollow>(true),
                UnitPathRangeLookup = SystemAPI.GetComponentLookup<UnitPathRange>(true),
                UnitPathRetryCooldownLookup = SystemAPI.GetComponentLookup<UnitPathRetryCooldown>(true),
                ManualMoveOrderLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true),
                ManualMoveGroupLookup = SystemAPI.GetComponentLookup<ManualMoveGroupMemberTag>(true),
                EngageTargetLookup = SystemAPI.GetComponentLookup<EngageTarget>(true),
                Ecb = ecb
            }.Schedule(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct ProcessBaseBreachOrderJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public ComponentLookup<UnitHealth> HealthLookup;
            [ReadOnly] public ComponentLookup<UnitGrid> GridLookup;
            [ReadOnly] public ComponentLookup<UnitTarget> UnitTargetLookup;
            [ReadOnly] public ComponentLookup<UnitPathRequest> UnitPathRequestLookup;
            [ReadOnly] public ComponentLookup<UnitPathFollow> UnitPathFollowLookup;
            [ReadOnly] public ComponentLookup<UnitPathRange> UnitPathRangeLookup;
            [ReadOnly] public ComponentLookup<UnitPathRetryCooldown> UnitPathRetryCooldownLookup;
            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveOrderLookup;
            [ReadOnly] public ComponentLookup<ManualMoveGroupMemberTag> ManualMoveGroupLookup;
            [ReadOnly] public ComponentLookup<EngageTarget> EngageTargetLookup;
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity, ref BaseBreachOrder breachOrder)
            {
                BaseBreachOrder order = breachOrder;

                if (!IsAliveTarget(order.FinalTarget))
                {
                    Ecb.RemoveComponent<BaseBreachOrder>(entity);
                    return;
                }

                if (order.Stage == BaseBreachOrder.StageMovingToEnemyBreach)
                {
                    if (!IsAliveTarget(order.BreachTarget))
                    {
                        order.Stage = BaseBreachOrder.StageMovingToFinalTarget;
                        breachOrder = order;
                        EnsurePathRequest(entity, order.FinalCell);
                        RemoveEngageTargetIfPresent(entity);
                        return;
                    }

                    if (!IsNearCell(entity, order.BreachCell))
                    {
                        EnsurePathRequest(entity, order.BreachCell);
                        RemoveEngageTargetIfPresent(entity);
                        return;
                    }

                    order.Stage = BaseBreachOrder.StageAttackingBreach;
                    breachOrder = order;
                    SetEngageTarget(entity, order.BreachTarget, order.BreachCell, order.BreachPosition, order.IsCommanded);
                    RemovePathingState(entity);
                    return;
                }

                if (order.Stage == BaseBreachOrder.StageMovingToFinalTarget)
                {
                    if (HasActivePathingState(entity))
                    {
                        EnsurePathRequest(entity, order.FinalCell);
                        RemoveEngageTargetIfPresent(entity);
                        return;
                    }

                    RemovePathingState(entity);
                    SetEngageTarget(entity, order.FinalTarget, order.FinalCell, order.FinalPosition, order.IsCommanded);
                    Ecb.RemoveComponent<BaseBreachOrder>(entity);
                    return;
                }

                if (EngageTargetLookup.HasComponent(entity))
                {
                    EngageTarget engage = EngageTargetLookup[entity];
                    if (engage.Target == order.FinalTarget)
                    {
                        Ecb.RemoveComponent<BaseBreachOrder>(entity);
                        return;
                    }

                    if (IsAliveTarget(engage.Target))
                        return;
                }

                if (IsAliveTarget(order.BreachTarget))
                {
                    SetEngageTarget(entity, order.BreachTarget, order.BreachCell, order.BreachPosition, order.IsCommanded);
                    return;
                }

                order.Stage = BaseBreachOrder.StageMovingToFinalTarget;
                breachOrder = order;
                EnsurePathRequest(entity, order.FinalCell);
                RemoveEngageTargetIfPresent(entity);
            }

            private bool IsAliveTarget(Entity target)
            {
                if (target == Entity.Null || !TransformLookup.HasComponent(target))
                    return false;

                return !HealthLookup.HasComponent(target) || HealthLookup[target].Current > 0;
            }

            private bool IsNearCell(Entity entity, int2 targetCell)
            {
                if (!GridLookup.HasComponent(entity))
                    return false;

                int2 delta = GridLookup[entity].Cell - targetCell;
                return math.abs(delta.x) <= 1 && math.abs(delta.y) <= 1;
            }

            private void EnsurePathRequest(Entity entity, int2 goal)
            {
                if (UnitTargetLookup.HasComponent(entity))
                    Ecb.SetComponent(entity, new UnitTarget { Cell = goal });
                else
                    Ecb.AddComponent(entity, new UnitTarget { Cell = goal });

                if (!UnitPathRequestLookup.HasComponent(entity) &&
                    !UnitPathFollowLookup.HasComponent(entity) &&
                    !UnitPathRetryCooldownLookup.HasComponent(entity))
                {
                    Ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
                }

                if (!ManualMoveOrderLookup.HasComponent(entity))
                    Ecb.AddComponent<ManualMoveOrderTag>(entity);
            }

            private void SetEngageTarget(Entity entity, Entity target, int2 cell, float3 position, byte isCommanded)
            {
                EngageTarget engage = new()
                {
                    Target = target,
                    Cell = cell,
                    Position = position,
                    IsCommanded = isCommanded
                };

                if (EngageTargetLookup.HasComponent(entity))
                    Ecb.SetComponent(entity, engage);
                else
                    Ecb.AddComponent(entity, engage);
            }

            private void RemovePathingState(Entity entity)
            {
                if (UnitPathRequestLookup.HasComponent(entity))
                    Ecb.RemoveComponent<UnitPathRequest>(entity);
                if (UnitPathFollowLookup.HasComponent(entity))
                    Ecb.RemoveComponent<UnitPathFollow>(entity);
                if (UnitPathRangeLookup.HasComponent(entity))
                    Ecb.RemoveComponent<UnitPathRange>(entity);
                if (UnitTargetLookup.HasComponent(entity))
                    Ecb.RemoveComponent<UnitTarget>(entity);
                if (ManualMoveOrderLookup.HasComponent(entity))
                    Ecb.RemoveComponent<ManualMoveOrderTag>(entity);
                if (ManualMoveGroupLookup.HasComponent(entity))
                    Ecb.RemoveComponent<ManualMoveGroupMemberTag>(entity);
            }

            private void RemoveEngageTargetIfPresent(Entity entity)
            {
                if (EngageTargetLookup.HasComponent(entity))
                    Ecb.RemoveComponent<EngageTarget>(entity);
            }

            private bool HasActivePathingState(Entity entity)
            {
                return UnitPathRequestLookup.HasComponent(entity) ||
                       UnitPathFollowLookup.HasComponent(entity) ||
                       UnitPathRetryCooldownLookup.HasComponent(entity);
            }
        }
    }
}
