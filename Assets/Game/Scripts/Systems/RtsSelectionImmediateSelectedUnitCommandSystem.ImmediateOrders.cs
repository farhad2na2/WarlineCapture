using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Tactical.Contracts;
using Game.Components;

namespace Game.Runtime
{
    public partial struct RtsSelectionImmediateSelectedUnitCommandSystem : ISystem
    {
        private static int ApplyImmediateSelectedUnitOrder(
            EntityManager em,
            EntityQuery selectedMoveQuery,
            bool holdPosition)
        {
            int issuedCount = 0;
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = selectedMoveQuery.ToArchetypeChunkArray(Allocator.Temp);
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            try
            {
                for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    NativeArray<Entity> selectedEntities = chunks[chunkIndex].GetNativeArray(entityType);
                    for (int i = 0; i < selectedEntities.Length; i++)
                    {
                        Entity entity = selectedEntities[i];
                        if (!SelectionUiReadModelLookup.CanAcceptImmediateSelectedUnitCommand(em, entity, out _))
                            continue;

                        ClearImmediateOrderComponents(em, ecb, entity, holdPosition);
                        if (holdPosition)
                        {
                            // Clearing queues removal of an existing Hold tag; re-add it even
                            // for repeated Hold requests in the same command buffer.
                            ecb.AddComponent<HoldPositionOrderTag>(entity);
                            if (em.HasComponent<UnitCombat>(entity))
                            {
                                UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                                if (combat.CanAttack != 0)
                                {
                                    combat.AutoEngage = 1;
                                    ecb.SetComponent(entity, combat);
                                }
                            }
                        }
                        else
                        {
                            RemoveComponentIfPresent<HoldPositionOrderTag>(em, ecb, entity);
                            if (em.HasComponent<UnitCombat>(entity))
                            {
                                UnitCombat combat = em.GetComponentData<UnitCombat>(entity);
                                if (combat.CanAttack != 0)
                                {
                                    combat.AutoEngage = 0;
                                    ecb.SetComponent(entity, combat);
                                }
                            }
                        }

                        if (!em.HasComponent<ManualMoveOrderTag>(entity))
                            ecb.AddComponent<ManualMoveOrderTag>(entity);
                        issuedCount++;
                    }
                }

                ecb.Playback(em);
            }
            finally
            {
                ecb.Dispose();
            }

            return issuedCount;
        }

        private static void ClearImmediateOrderComponents(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity entity,
            bool holdPosition)
        {
            UnitMoveOrderRequestSystem.ClearMovementOrderComponents(em, ecb, entity);
            if (holdPosition)
                HoldRuntimeMotion(em, ecb, entity);
            else
                StopRuntimeMotion(em, ecb, entity);
        }

        private static void HoldRuntimeMotion(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        {
            StopVehicleKinematics(em, ecb, entity);
        }

        private static void StopRuntimeMotion(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        {
            StopVehicleKinematics(em, ecb, entity);

            if (!em.HasComponent<UnitAirComponent>(entity))
                return;

            UnitAirComponent airState = em.GetComponentData<UnitAirComponent>(entity);
            airState.ReturningHome = 0;
            airState.TakeoffRolling = 0;
            airState.LandingRolling = 0;
            airState.AttackRunActive = 0;
            airState.ReturnApproachInitialized = 0;
            ecb.SetComponent(entity, airState);
        }

        private static void StopVehicleKinematics(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        {
            if (em.HasComponent<UnitVehicleKinematics>(entity))
            {
                UnitVehicleKinematics kinematics = em.GetComponentData<UnitVehicleKinematics>(entity);
                kinematics.CurrentSpeed = 0f;
                kinematics.StallSeconds = 0f;
                ecb.SetComponent(entity, kinematics);
            }
        }

    }
}
