using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Mathematics;

namespace Game.Runtime
{
    internal static class CampaignMissionFinalKillCinematicHelper
    {
        internal const float EaseInSeconds = 0.12f;
        internal const float HoldEndSeconds = 1.7f;
        internal const float DurationSeconds = 2.2f;
        internal const float SlowTimeScale = 0.18f;
        internal const float MaxFrameAdvanceSeconds = 0.1f;
        internal const float ShotSmoothTimeSeconds = 0.35f;
        internal const float RestoreSmoothTimeSeconds = 0.5f;
        internal const float ShotHeightAboveVictim = 7.5f;
        internal const float ShotPitchDegrees = 24f;
        internal const float ShotFieldOfView = 34f;
        internal const float KillerFramingFraction = 0.3f;
        internal const float MaxStandOffDistance = 6f;

        internal static float EvaluateTimeScale(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f)
                return 1f;
            if (elapsedSeconds < EaseInSeconds)
                return math.lerp(1f, SlowTimeScale, math.smoothstep(0f, EaseInSeconds, elapsedSeconds));
            if (elapsedSeconds < HoldEndSeconds)
                return SlowTimeScale;
            if (elapsedSeconds < DurationSeconds)
                return math.lerp(SlowTimeScale, 1f, math.smoothstep(HoldEndSeconds, DurationSeconds, elapsedSeconds));
            return 1f;
        }

        internal static bool IsFinished(float elapsedSeconds) => elapsedSeconds >= DurationSeconds;

        internal static bool IsCombatPhase(MissionPhaseKind phase) =>
            phase is >= MissionPhaseKind.FindSquad and <= MissionPhaseKind.SecureCorridor;

        internal static bool ShouldTrigger(
            int previousLiveHostiles,
            int liveHostiles,
            MissionPhaseKind previousPhase,
            MissionPhaseKind phase,
            MissionOutcomeKind outcome)
        {
            if (previousLiveHostiles <= 0 || liveHostiles != 0 || outcome == MissionOutcomeKind.Defeat)
                return false;
            return IsCombatPhase(phase) ||
                   phase == MissionPhaseKind.Result && IsCombatPhase(previousPhase);
        }

        // SecureCorridor finales and the result screen own the camera after the shot.
        internal static bool ShouldRestoreCamera(MissionPhaseKind phase, MissionOutcomeKind outcome) =>
            outcome == MissionOutcomeKind.None &&
            phase is >= MissionPhaseKind.FindSquad and <= MissionPhaseKind.Engage;

        internal static bool IsActive(
            in CampaignMissionFinalKillCinematicComponent cinematic,
            in FixedString64Bytes sessionToken) =>
            cinematic.Active != 0 && cinematic.SessionToken.Equals(sessionToken);

        internal static RuntimeCameraFocusRequestComponent BuildShot(
            float3 victimPosition,
            float3 viewOrigin,
            bool hasViewOrigin,
            float fallbackYawDegrees)
        {
            float3 direction = victimPosition - viewOrigin;
            direction.y = 0f;
            float distance = math.length(direction);
            float3 focus = victimPosition;
            float yaw = fallbackYawDegrees;
            if (hasViewOrigin && distance > 0.5f)
            {
                float3 forward = direction / distance;
                yaw = math.degrees(math.atan2(forward.x, forward.z));
                focus = victimPosition - forward * math.min(distance * KillerFramingFraction, MaxStandOffDistance);
            }

            yaw = math.fmod(yaw, 360f);
            if (yaw < 0f)
                yaw += 360f;

            return new RuntimeCameraFocusRequestComponent
            {
                Requested = 1,
                Smooth = 1,
                UseExplicitPerspective = 1,
                SmoothTimeSeconds = ShotSmoothTimeSeconds,
                Perspective = new float4(
                    victimPosition.y + ShotHeightAboveVictim,
                    ShotPitchDegrees,
                    yaw,
                    ShotFieldOfView),
                World = focus
            };
        }
    }
}
