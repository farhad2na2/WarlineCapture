using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    internal static class AIMaterialsRecoveryNeedMutationSystemHelper
    {
        internal static void Publish(
            EntityManager em,
            Entity planEntity,
            byte factionId,
            int requiredCredits,
            int requiredMaterials,
            in FactionTacticalMaterialsComponent materials,
            float now)
        {
            if (!em.HasComponent<AIMaterialsRecoveryNeedComponent>(planEntity))
                return;

            AIMaterialsRecoveryNeedComponent need =
                em.GetComponentData<AIMaterialsRecoveryNeedComponent>(planEntity);
            int safeRequiredMaterials = math.max(0, requiredMaterials);
            int missingMaterials = math.max(0, safeRequiredMaterials - math.max(0, materials.Current));
            bool sameNeed = need.Active != 0 &&
                            need.FactionId == factionId &&
                            need.RequiredCredits == math.max(0, requiredCredits) &&
                            need.RequiredMaterials == safeRequiredMaterials;
            float firstBlockedTime = sameNeed ? need.FirstBlockedTimeSeconds : now;
            byte active = missingMaterials > 0 && safeRequiredMaterials <= math.max(0, materials.Capacity)
                ? (byte)1
                : (byte)0;

            AIMaterialsRecoveryNeedComponent next = new()
            {
                FactionId = factionId,
                Active = active,
                RequiredCredits = math.max(0, requiredCredits),
                RequiredMaterials = safeRequiredMaterials,
                MissingMaterials = missingMaterials,
                FirstBlockedTimeSeconds = firstBlockedTime,
                LastEvaluatedTimeSeconds = now,
                Version = need.Version + 1u
            };
            em.SetComponentData(planEntity, next);
        }

        internal static void Clear(
            EntityManager em,
            Entity planEntity,
            byte factionId,
            float now)
        {
            if (!em.HasComponent<AIMaterialsRecoveryNeedComponent>(planEntity))
                return;

            AIMaterialsRecoveryNeedComponent need =
                em.GetComponentData<AIMaterialsRecoveryNeedComponent>(planEntity);
            if (need.Active == 0 &&
                need.FactionId == factionId &&
                need.RequiredCredits == 0 &&
                need.RequiredMaterials == 0 &&
                need.MissingMaterials == 0)
            {
                return;
            }

            need.FactionId = factionId;
            need.Active = 0;
            need.RequiredCredits = 0;
            need.RequiredMaterials = 0;
            need.MissingMaterials = 0;
            need.FirstBlockedTimeSeconds = now;
            need.LastEvaluatedTimeSeconds = now;
            need.Version++;
            em.SetComponentData(planEntity, need);
        }
    }
}
