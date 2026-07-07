using Unity.Mathematics;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public static class TacticalFollowAttackCinematicCameraSystemHelper
    {
        private const float ObstructionProbeRadius = 0.55f;
        private const float CameraNearPadding = 0.85f;
        private const float TargetNearPadding = 2.25f;
        private const int MaxObstructionHits = 8;

        private static readonly RaycastHit[] ObstructionHits = new RaycastHit[MaxObstructionHits];

        public static TacticalFollowAttackCinematicHelper.Shot EvaluateShotWithObstructionFallback(
            TacticalFollowAttackCinematicPhase phase,
            float phaseElapsedSeconds,
            in TacticalFollowAttackCinematicHelper.ShotContext context)
        {
            TacticalFollowAttackCinematicHelper.Shot primary =
                TacticalFollowAttackCinematicHelper.EvaluateShot(phase, phaseElapsedSeconds, context);
            if (!IsObstructed(primary))
                return primary;

            for (int i = 1; i <= TacticalFollowAttackCinematicHelper.ObstructionFallbackCandidateCount; i++)
            {
                TacticalFollowAttackCinematicHelper.Shot fallback =
                    TacticalFollowAttackCinematicHelper.EvaluateFallbackShot(phase, phaseElapsedSeconds, context, i);
                if (!IsObstructed(fallback))
                    return fallback;
            }

            return TacticalFollowAttackCinematicHelper.EvaluateFallbackShot(
                phase,
                phaseElapsedSeconds,
                context,
                TacticalFollowAttackCinematicHelper.ObstructionFallbackCandidateCount);
        }

        private static bool IsObstructed(in TacticalFollowAttackCinematicHelper.Shot shot)
        {
            float3 delta = shot.LookAt - shot.CameraPosition;
            float distance = math.length(delta);
            float probeDistance = distance - CameraNearPadding - TargetNearPadding;
            if (probeDistance <= 0.25f)
                return false;

            float3 direction = delta / distance;
            Vector3 origin = (Vector3)(shot.CameraPosition + direction * CameraNearPadding);
            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                ObstructionProbeRadius,
                (Vector3)direction,
                ObstructionHits,
                probeDistance,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                if (ObstructionHits[i].collider != null)
                    return true;
            }

            return false;
        }
    }
}
