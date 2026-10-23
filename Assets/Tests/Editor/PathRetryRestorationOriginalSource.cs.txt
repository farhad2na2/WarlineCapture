using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateBefore(typeof(UnitPathfindingSystem))]
    public partial struct UnitManualMoveRetrySystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GridConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            state.Dependency = new ClearRetryCooldownJob
            {
                CurrentFrame = Time.frameCount,
                Ecb = ecb
            }.Schedule(state.Dependency);

            state.Dependency = new RemoveStaleGroupMemberJob
            {
                Ecb = ecb
            }.Schedule(state.Dependency);

            state.Dependency = new RestoreManualPathRequestJob
            {
                Ecb = ecb
            }.Schedule(state.Dependency);

            state.Dependency = new RestoreLongDistancePathRequestJob
            {
                TargetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true),
                ManualMoveLookup = SystemAPI.GetComponentLookup<ManualMoveOrderTag>(true),
                Ecb = ecb
            }.Schedule(state.Dependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private partial struct ClearRetryCooldownJob : IJobEntity
        {
            public int CurrentFrame;
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity, in UnitPathRetryCooldown cooldown)
            {
                if (cooldown.ResumeFrame <= CurrentFrame)
                    Ecb.RemoveComponent<UnitPathRetryCooldown>(entity);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ManualMoveGroupMemberTag))]
        [WithNone(typeof(ManualMoveOrderTag))]
        private partial struct RemoveStaleGroupMemberJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity)
            {
                Ecb.RemoveComponent<ManualMoveGroupMemberTag>(entity);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ManualMoveOrderTag))]
        [WithNone(
            typeof(UnitLongDistanceMove),
            typeof(UnitPathRetryCooldown),
            typeof(UnitPathRequest),
            typeof(UnitPathFollow),
            typeof(EngageTarget),
            typeof(UnitAirMovement))]
        private partial struct RestoreManualPathRequestJob : IJobEntity
        {
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity, in UnitTarget target)
            {
                Ecb.AddComponent(entity, new UnitPathRequest { Goal = target.Cell });
            }
        }

        [BurstCompile]
        [WithNone(
            typeof(UnitPathRetryCooldown),
            typeof(UnitPathRequest),
            typeof(UnitPathFollow),
            typeof(EngageTarget),
            typeof(UnitAirMovement))]
        private partial struct RestoreLongDistancePathRequestJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<UnitTarget> TargetLookup;
            [ReadOnly] public ComponentLookup<ManualMoveOrderTag> ManualMoveLookup;
            public EntityCommandBuffer Ecb;

            private void Execute(Entity entity, in UnitLongDistanceMove longMove)
            {
                if (TargetLookup.HasComponent(entity))
                    Ecb.SetComponent(entity, new UnitTarget { Cell = longMove.FinalGoal });
                else
                    Ecb.AddComponent(entity, new UnitTarget { Cell = longMove.FinalGoal });

                bool hasManualMove = ManualMoveLookup.HasComponent(entity);
                if (longMove.ManualMove != 0 && !hasManualMove)
                    Ecb.AddComponent<ManualMoveOrderTag>(entity);
                else if (longMove.ManualMove == 0 && hasManualMove)
                    Ecb.RemoveComponent<ManualMoveOrderTag>(entity);

                Ecb.AddComponent(entity, new UnitPathRequest { Goal = longMove.FinalGoal });
            }
        }
    }
}
