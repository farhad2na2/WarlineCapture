using Game.Components;
using Game.UI.Contracts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.UI.Shell.Ecs
{
    public sealed partial class UiShellEcsGateway
    {
        internal static float3 ResolveExtractionCameraTarget(EntityManager em, Entity root,
            in CampaignMissionExtractionState extraction, UiMissionExtractionAction action, int lesson)
        {
            if (action == UiMissionExtractionAction.FocusDeparture) return extraction.DepartureCenter;
            if (action == UiMissionExtractionAction.FocusLanding) return extraction.LandingCenter;
            if (action == UiMissionExtractionAction.ShowLesson)
            {
                Entity subject = lesson is 2 or 5 or 6 or 7 ? extraction.Carrier :
                    lesson is 8 or 9 or 11 or 12 ? extraction.Aircraft : Entity.Null;
                if (TryGetLiveExtractionPosition(em, subject, out float3 position)) return position;
                if (lesson == 10) return extraction.LandingCenter;
            }

            float3 center = default;
            int count = 0;
            if (em.HasBuffer<CampaignMissionExtractionMember>(root))
            {
                var members = em.GetBuffer<CampaignMissionExtractionMember>(root, true);
                foreach (var member in members)
                {
                    if (member.Kind != 1 || !TryGetLiveExtractionPosition(em, member.Entity, out float3 position)) continue;
                    // Hidden passengers retain their boarding position; the transport is their current location.
                    if (em.HasComponent<UnitTransportPassenger>(member.Entity) &&
                        TryGetLiveExtractionPosition(em, em.GetComponentData<UnitTransportPassenger>(member.Entity).Transport, out float3 aboard))
                        position = aboard;
                    center += position;
                    count++;
                }
            }
            if (count > 0) return center / count;
            if (TryGetLiveExtractionPosition(em, extraction.Carrier, out float3 carrier)) return carrier;
            return extraction.LandingCenter;
        }

        private static bool TryGetLiveExtractionPosition(EntityManager em, Entity actor, out float3 position)
        {
            position = default;
            if (actor == Entity.Null || !em.HasComponent<LocalTransform>(actor) ||
                !em.HasComponent<UnitHealth>(actor) || em.GetComponentData<UnitHealth>(actor).Current <= 0) return false;
            position = em.GetComponentData<LocalTransform>(actor).Position;
            return math.all(math.isfinite(position));
        }
    }
}
