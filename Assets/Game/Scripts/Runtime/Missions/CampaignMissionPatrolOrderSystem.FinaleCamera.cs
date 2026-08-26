using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public partial struct CampaignMissionPatrolOrderSystem
    {
        internal static float ComputeCombatRevealYaw(float3 direction)
        {
            float2 groundDirection = direction.xz;
            if (!math.all(math.isfinite(groundDirection)) || math.lengthsq(groundDirection) < 0.0001f)
                return RuntimeCameraFocusRequestUtility.TacticalRevealYaw;
            groundDirection = math.normalize(groundDirection);
            return math.degrees(math.atan2(groundDirection.x, groundDirection.y));
        }

        internal static float3 ComputeCombatRevealFocus(float3 friendlyFocus, float3 hostileFocus) =>
            math.lerp(friendlyFocus, hostileFocus, FinaleFocusTowardHostiles);

        internal static float3 ComputeCasualtyRevealFocus(float3 friendlyFocus, float3 casualtyFocus) =>
            math.lerp(friendlyFocus, casualtyFocus, FinaleCasualtyFocusTowardHostiles);

        internal static bool ShouldCompleteFinale(int elapsedMilliseconds) =>
            elapsedMilliseconds >= FinaleCameraArrivalMilliseconds + FinalePostKillHoldMilliseconds;

        internal static bool TryComputeLiveCombatFocus(
            EntityManager entityManager,
            EntityQuery missionCombatantsQuery,
            in FixedString64Bytes sessionToken,
            out float3 friendlyFocus,
            out float3 hostileFocus) =>
            CampaignMissionFinaleCameraUtility.TryComputeLiveCombatFocus(
                entityManager,
                missionCombatantsQuery,
                sessionToken,
                out friendlyFocus,
                out hostileFocus);
    }
}
