using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;
using Game.Runtime.Pathfinding;

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
                if (PathRetryRestorationKernel.ShouldClearCooldown(cooldown.ResumeFrame, CurrentFrame))
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
                var intent = PathRetryRestorationKernel.Plan(new PathRetryRestorationFacts { HasGroupMember = true }, 0);
                if (intent.RemoveStaleGroupMember)
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
                var intent = PathRetryRestorationKernel.Plan(new PathRetryRestorationFacts
                {
                    HasManual = true, HasTarget = true,
                    Target = new PlacementCell(target.Cell.x, target.Cell.y)
                }, 0);
                if (intent.RestoreRequest)
                    Ecb.AddComponent(entity, new UnitPathRequest { Goal = new Unity.Mathematics.int2(intent.Goal.x, intent.Goal.y) });
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
                var intent = PathRetryRestorationKernel.Plan(new PathRetryRestorationFacts
                {
                    HasLongDistance = true, HasTarget = TargetLookup.HasComponent(entity),
                    HasManual = ManualMoveLookup.HasComponent(entity), LongDistanceManual = longMove.ManualMove,
                    FinalGoal = new PlacementCell(longMove.FinalGoal.x, longMove.FinalGoal.y)
                }, 0);
                var goal = new Unity.Mathematics.int2(intent.Goal.x, intent.Goal.y);
                if (intent.TargetAction == RetryTargetAction.Set)
                    Ecb.SetComponent(entity, new UnitTarget { Cell = goal });
                else if (intent.TargetAction == RetryTargetAction.Add)
                    Ecb.AddComponent(entity, new UnitTarget { Cell = goal });
                if (intent.ManualAction == RetryManualAction.Add)
                    Ecb.AddComponent<ManualMoveOrderTag>(entity);
                else if (intent.ManualAction == RetryManualAction.Remove)
                    Ecb.RemoveComponent<ManualMoveOrderTag>(entity);
                if (intent.RestoreRequest)
                    Ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });
            }
        }
    }
}
