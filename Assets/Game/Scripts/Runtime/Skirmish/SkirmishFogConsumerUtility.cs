using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Entities;
namespace Game.Runtime
{
    // Unowned Campaign actors retain their existing visibility/input contracts.
    public static class SkirmishFogConsumerUtility
    {
        public static bool PlayerVisible(EntityManager em, Entity contact) => SkirmishPublicObjectiveProjection.PlayerVisible(em, contact);

        public static bool PlayerImpactVisible(EntityManager em, Entity source, Entity target,
            Unity.Mathematics.float3 observedImpactPosition, byte detectorDomain)
        {
            if (!em.Exists(source))
            {
                if (!SkirmishFogObservedPointUtility.TryActiveSharedAttempt(em, out var currentAttempt))
                    return !SkirmishFogObservedPointUtility.HasActiveSharedAttempt(em); // Ambiguous expanded worlds fail closed.
                // A stale handle cannot prove precision ownership. Only the actual
                // request point in the current presented world may remain visible.
                return SkirmishFogObservedPointUtility.IsObserved(em, currentAttempt, observedImpactPosition, detectorDomain);
            }
            if (!em.HasComponent<SkirmishAttemptOwnedComponent>(source))
                return true; // Existing unowned Campaign playback contract.
            var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(source);
            if (em.Exists(target) && em.HasComponent<SkirmishAttemptOwnedComponent>(target) &&
                owner.SessionId.Equals(em.GetComponentData<SkirmishAttemptOwnedComponent>(target).SessionId) &&
                PlayerVisible(em, target)) return true;
            return SkirmishFogObservedPointUtility.IsObserved(em, source, observedImpactPosition, detectorDomain);
        }

        public static bool IsVisible(in ComponentLookup<SkirmishAttemptOwnedComponent> owned,
            in ComponentLookup<SkirmishContactSightComponent> sight,
            in BufferLookup<SkirmishContactKnowledgeElement> knowledge,
            Entity contact, byte observer)
        {
            if (!owned.HasComponent(contact)) return true;
            if (owned[contact].FactionId == observer) return true;
            if (TryKnowledge(knowledge, contact, observer, out var record))
                return record.Sight == SkirmishContactSight.Visible;
            return observer == 1 && sight.HasComponent(contact) &&
                   sight[contact].Sight == SkirmishContactSight.Visible;
        }

        public static bool TryKnowledge(in BufferLookup<SkirmishContactKnowledgeElement> lookup,
            Entity contact, byte observer, out SkirmishContactKnowledgeElement record)
        {
            record = default;
            if (!lookup.HasBuffer(contact)) return false;
            var buffer = lookup[contact];
            for (int i = 0; i < buffer.Length; i++)
                if (buffer[i].ObserverFactionId == observer) { record = buffer[i]; return true; }
            return false;
        }
    }
}
