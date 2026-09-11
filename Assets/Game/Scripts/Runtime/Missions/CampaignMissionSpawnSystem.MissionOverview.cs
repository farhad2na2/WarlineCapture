using Game.Components;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionSpawnSystem
    {
        internal static void QueueMissionOpeningOverview(EntityManager em, Entity camera,
            in CampaignMissionOpeningPresentationComponent opening, bool frameMissionSubjects, bool defenseCommandView = false)
        {
            if (defenseCommandView)
            {
                em.SetComponentData(camera, CreateDefenseCommandView(opening.FriendlyFocus));
                return;
            }
            if (!frameMissionSubjects)
            {
                QueueInitialRtsOverview(em, camera, opening.FriendlyFocus);
                return;
            }
            em.SetComponentData(camera, CreateMissionOverviewRequest(opening.FriendlyFocus,
                opening.EstablishingFocus, opening.HostileFocus));
        }

        internal static RuntimeCameraFocusRequestComponent CreateDefenseCommandView(float3 squad) => new()
        {
            Requested = 1, UseExplicitPerspective = 1, World = squad,
            Perspective = new float4(squad.y + 85f, 60f, 0f, 50f)
        };

        internal static RuntimeCameraFocusRequestComponent CreateMissionOverviewRequest(
            float3 commandArea, float3 firstSubject, float3 secondSubject)
        {
            float3 lower = math.min(commandArea, math.min(firstSubject, secondSubject));
            float3 upper = math.max(commandArea, math.max(firstSubject, secondSubject));
            float3 center = (lower + upper) * .5f;
            float2 halfSize = (upper - lower).xz * .5f + 18f;
            const float pitch = 67f, fieldOfView = 55f, minimumAspect = 16f / 9f;
            float sine = math.sin(math.radians(pitch));
            float tangent = math.tan(math.radians(fieldOfView * .5f));
            // Keep the subjects inside the central battlefield between the selection panel and ARIA, above the command tray.
            float height = math.max(80f, math.max(halfSize.x * sine / (tangent * minimumAspect * .44f),
                halfSize.y * sine * sine / (tangent * .56f)));
            return new RuntimeCameraFocusRequestComponent
            {
                Requested = 1, UseExplicitPerspective = 1, World = center,
                Perspective = new float4(center.y + height, pitch, 0f, fieldOfView)
            };
        }
    }
}
