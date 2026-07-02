using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitTransportAirdropSystem))]
    public partial struct UnitTransportDeployAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitTransportDeployAttackTarget>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new(Allocator.TempJob);
            try
            {
                state.Dependency = new ApplyDeployAttackJob
                {
                    Ecb = ecb.AsParallelWriter(),
                    EntityStorageInfoLookup = state.GetEntityStorageInfoLookup(),
                    UnitCombatLookup = SystemAPI.GetComponentLookup<UnitCombat>(true),
                    UnitAttackLookup = SystemAPI.GetComponentLookup<UnitAttack>(true),
                    UnitHealthLookup = SystemAPI.GetComponentLookup<UnitHealth>(true),
                    EngageTargetLookup = SystemAPI.GetComponentLookup<EngageTarget>(true),
                    UnitTargetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true),
                    UnitPathRequestLookup = SystemAPI.GetComponentLookup<UnitPathRequest>(true),
                    UnitPathFollowLookup = SystemAPI.GetComponentLookup<UnitPathFollow>(true),
                    UnitPathRangeLookup = SystemAPI.GetComponentLookup<UnitPathRange>(true),
                    ManualMoveOrderLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true)
                }.ScheduleParallel(state.Dependency);

                state.Dependency.Complete();
                ecb.Playback(state.EntityManager);
            }
            finally
            {
                ecb.Dispose();
            }
        }

        [BurstCompile]
        [WithNone(typeof(Disabled))]
        [WithNone(typeof(UnitTransportPassenger))]
        [WithNone(typeof(UnitTransportCargoPassenger))]
        [WithNone(typeof(UnitTransportParachuteDropComponent))]
        [WithNone(typeof(UnitTransportCargoDropComponent))]
        [WithNone(typeof(UnitTransportAirdropSettleComponent))]
        private partial struct ApplyDeployAttackJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
            [ReadOnly] public ComponentLookup<UnitCombat> UnitCombatLookup;
            [ReadOnly] public ComponentLookup<UnitAttack> UnitAttackLookup;
            [ReadOnly] public ComponentLookup<UnitHealth> UnitHealthLookup;
            [ReadOnly] public ComponentLookup<EngageTarget> EngageTargetLookup;
            [ReadOnly] public ComponentLookup<UnitTarget> UnitTargetLookup;
            [ReadOnly] public ComponentLookup<UnitPathRequest> UnitPathRequestLookup;
            [ReadOnly] public ComponentLookup<UnitPathFollow> UnitPathFollowLookup;
            [ReadOnly] public ComponentLookup<UnitPathRange> UnitPathRangeLookup;
            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveOrderLookup;

            private void Execute(
                [ChunkIndexInQuery] int sortKey,
                Entity entity,
                in UnitTransportDeployAttackTarget target)
            {
                if (!CanPassengerAttack(entity) || !IsLiveAttackTarget(target.TargetEntity))
                {
                    Ecb.RemoveComponent<UnitTransportDeployAttackTarget>(sortKey, entity);
                    return;
                }

                EngageTarget engageTarget = new()
                {
                    Target = target.TargetEntity,
                    Cell = target.TargetCell,
                    Position = target.TargetPosition,
                    IsCommanded = 1
                };
                if (EngageTargetLookup.HasComponent(entity))
                    Ecb.SetComponent(sortKey, entity, engageTarget);
                else
                    Ecb.AddComponent(sortKey, entity, engageTarget);

                RemoveIfPresent(UnitTargetLookup, sortKey, entity);
                RemoveIfPresent(UnitPathRequestLookup, sortKey, entity);
                RemoveIfPresent(UnitPathFollowLookup, sortKey, entity);
                RemoveIfPresent(UnitPathRangeLookup, sortKey, entity);
                RemoveIfPresent(ManualMoveOrderLookup, sortKey, entity);
                Ecb.RemoveComponent<UnitTransportDeployAttackTarget>(sortKey, entity);
            }

            private bool CanPassengerAttack(Entity entity)
            {
                if (!UnitCombatLookup.HasComponent(entity) ||
                    !UnitAttackLookup.HasComponent(entity))
                {
                    return false;
                }

                if (UnitCombatLookup[entity].CanAttack == 0)
                    return false;

                return !UnitHealthLookup.HasComponent(entity) ||
                       UnitHealthLookup[entity].Current > 0;
            }

            private bool IsLiveAttackTarget(Entity target)
            {
                if (target == Entity.Null ||
                    !EntityStorageInfoLookup.Exists(target))
                {
                    return false;
                }

                return !UnitHealthLookup.HasComponent(target) ||
                       UnitHealthLookup[target].Current > 0;
            }

            private void RemoveIfPresent<T>(
                ComponentLookup<T> lookup,
                int sortKey,
                Entity entity)
                where T : unmanaged, IComponentData
            {
                if (lookup.HasComponent(entity))
                    Ecb.RemoveComponent<T>(sortKey, entity);
            }
        }
    }
}
