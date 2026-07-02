using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal struct UnitPathRetry
    {
        public const int FailedManualRetryDelayFrames = 8;

        public bool ShouldRetryManualMove(EntityManager em, Entity entity, byte segmented)
        {
            return em.HasComponent<ManualMoveOrderTag>(entity) ||
                   em.HasComponent<UnitLongDistanceMove>(entity) ||
                   segmented != 0;
        }

        public void ApplyRetry(
            EntityManager em,
            Entity entity,
            UnitPathRequest request,
            byte segmented,
            byte manualMove,
            ref int retriedCount,
            ref int retriedSegmentCount,
            ref int manualRetriedCount)
        {
            retriedCount++;
            if (segmented != 0)
                retriedSegmentCount++;
            if (manualMove != 0)
                manualRetriedCount++;
            if (segmented != 0)
            {
                if (em.HasComponent<UnitLongDistanceMove>(entity))
                    em.SetComponentData(entity, new UnitLongDistanceMove { FinalGoal = request.Goal });
                else
                    em.AddComponentData(entity, new UnitLongDistanceMove { FinalGoal = request.Goal });

                if (em.HasComponent<UnitTarget>(entity))
                    em.SetComponentData(entity, new UnitTarget { Cell = request.Goal });
                else
                    em.AddComponentData(entity, new UnitTarget { Cell = request.Goal });
            }

            int resumeFrame = Time.frameCount + FailedManualRetryDelayFrames;
            if (em.HasComponent<UnitPathRetryCooldown>(entity))
                em.SetComponentData(entity, new UnitPathRetryCooldown { ResumeFrame = resumeFrame });
            else
                em.AddComponentData(entity, new UnitPathRetryCooldown { ResumeFrame = resumeFrame });
        }

        public void ApplyAbandon(EntityManager em, Entity entity, ref int abandonedCount)
        {
            abandonedCount++;
            if (em.HasComponent<UnitLongDistanceMove>(entity))
                em.RemoveComponent<UnitLongDistanceMove>(entity);
            if (em.HasComponent<ManualMoveOrderTag>(entity))
                em.RemoveComponent<ManualMoveOrderTag>(entity);
            if (em.HasComponent<UnitTarget>(entity))
                em.RemoveComponent<UnitTarget>(entity);
            if (em.HasComponent<UnitPathRetryCooldown>(entity))
                em.RemoveComponent<UnitPathRetryCooldown>(entity);
        }
    }
}
