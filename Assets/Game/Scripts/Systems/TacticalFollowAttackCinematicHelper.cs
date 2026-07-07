using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    /// <summary>
    /// Pure evaluation for the followed-jet attack cinematic: phase timing,
    /// slow-motion curve, and per-phase camera shots. Time inputs are
    /// unscaled seconds since the cinematic started.
    /// </summary>
    public static class TacticalFollowAttackCinematicHelper
    {
        public const float LaunchDurationSeconds = 1.1f;
        public const float MissilePathDurationSeconds = 1f;
        public const float ImpactDurationSeconds = 1.3f;
        public const float FlyoverDurationSeconds = 1.45f;
        public const float TotalDurationSeconds =
            LaunchDurationSeconds + MissilePathDurationSeconds + ImpactDurationSeconds + FlyoverDurationSeconds;
        public const float SlowMotionTimeScale = 0.3f;
        public const float TimeScaleRampSeconds = 0.35f;
        public const float RetriggerCooldownSeconds = 6f;
        public const float ProjectileLaunchBeatSeconds = 0.15f;
        public const float ImpactEventBeatSeconds = LaunchDurationSeconds + MissilePathDurationSeconds;

        private const float LaunchCameraBackDistance = 18f;
        private const float LaunchCameraSideDistance = 10f;
        private const float LaunchCameraHeightAboveJet = 6.5f;
        private const float LaunchPushInScale = 0.9f;
        private const float LaunchLookAheadDistance = 22f;
        private const float LaunchLookImpactBlend = 0.15f;
        private const float LaunchFieldOfView = 46f;
        private const float LaunchDampingSeconds = 0.16f;

        private const float MissilePathCameraBackDistance = 12f;
        private const float MissilePathCameraSideDistance = 20f;
        private const float MissilePathCameraHeight = 9f;
        private const float MissilePathLookAheadDistance = 11f;
        private const float MissilePathFieldOfView = 48f;
        private const float MissilePathDampingSeconds = 0.12f;

        private const float ImpactCameraForwardDistance = 23f;
        private const float ImpactCameraSideDistance = 16f;
        private const float ImpactCameraHeight = 10f;
        private const float ImpactOrbitDegrees = 10f;
        private const float ImpactLookHeight = 2.4f;
        private const float ImpactLookBackDistance = 4f;
        private const float ImpactFieldOfView = 48f;
        private const float ImpactDampingSeconds = 0.15f;

        private const float FlyoverCameraBackDistance = 18f;
        private const float FlyoverCameraSideDistance = 14f;
        private const float FlyoverCameraHeight = 10f;
        private const float FlyoverLookRampNormalized = 0.45f;
        private const float FlyoverLookJetWeight = 0.85f;
        private const float FlyoverExitBlendStartNormalized = 0.6f;
        private const float FlyoverExitBlendWeight = 0.65f;
        private const float FlyoverFollowBackDistance = 18f;
        private const float FlyoverFollowHeight = 9f;
        private const float FlyoverFieldOfView = 50f;
        private const float FlyoverDampingSeconds = 0.28f;

        private const float MinCinematicFieldOfView = 42f;
        private const float MaxCinematicFieldOfView = 54f;
        private const float ImpactHudSafeLookDrop = 1.15f;
        private const float FlyoverHudSafeLookDrop = 1.1f;
        private const float MinCameraClearanceAboveAction = 5.5f;
        private const float MinCameraDistanceFromLookAt = 14f;
        private const float MinCameraDistanceFromJet = 12f;
        private const float MinCameraDistanceFromImpact = 14f;
        private const float CinematicTargetRadius = 3f;
        private const float CinematicDesiredDistance = 14f;
        private const float CinematicDesiredHeight = 6f;
        private const float FollowMaxTransitionSpeed = 80f;

        public readonly struct ShotContext
        {
            public readonly float3 LaunchPosition;
            public readonly float3 ImpactPosition;
            public readonly float3 AttackDirection;
            public readonly float3 JetPosition;
            public readonly bool HasJet;

            public ShotContext(
                float3 launchPosition,
                float3 impactPosition,
                float3 attackDirection,
                float3 jetPosition,
                bool hasJet)
            {
                LaunchPosition = launchPosition;
                ImpactPosition = impactPosition;
                AttackDirection = NormalizeFlatOrFallback(attackDirection);
                JetPosition = jetPosition;
                HasJet = hasJet;
            }
        }

        public struct Shot
        {
            public float3 CameraPosition;
            public float3 LookAt;
            public float FieldOfView;
            public float PositionDampingSeconds;
        }

        public static TacticalFollowAttackCinematicStateComponent BuildInitialState(
            Entity sourceEntity,
            Entity targetEntity,
            float3 launchPosition,
            float3 impactPosition,
            float3 attackDirection,
            UnityObjectRef<UnityEngine.GameObject> launchVfxPrefab,
            UnityObjectRef<UnityEngine.GameObject> impactVfxPrefab,
            quaternion launchVfxRotation,
            quaternion impactVfxRotation,
            float requestedStartTime,
            float lastEndedElapsedTime)
        {
            float3 normalizedDirection = NormalizeFlatOrFallback(attackDirection);
            return new TacticalFollowAttackCinematicStateComponent
            {
                Active = 1,
                AttackKind = TacticalFollowAttackCinematicAttackKind.FollowedAirInstantHit,
                LastAppliedPhase = TacticalFollowAttackCinematicPhase.Launch,
                ElapsedUnscaledSeconds = 0f,
                RequestedStartTime = requestedStartTime,
                SourceEntity = sourceEntity,
                TargetEntity = targetEntity,
                LaunchPosition = launchPosition,
                ImpactPosition = impactPosition,
                AttackDirection = normalizedDirection,
                ProjectileProgress = 0f,
                ProjectilePosition = launchPosition,
                ProjectileDirection = normalizedDirection,
                LaunchVfxPrefab = launchVfxPrefab,
                ImpactVfxPrefab = impactVfxPrefab,
                LaunchVfxRotation = launchVfxRotation,
                ImpactVfxRotation = impactVfxRotation,
                LaunchEventTriggered = 0,
                ProjectileActive = 0,
                ImpactEventTriggered = 0,
                FlyoverEventTriggered = 0,
                Completed = 0,
                AbortReason = TacticalFollowAttackCinematicAbortReason.None,
                TimeScaleApplied = 0,
                SavedTimeScale = 1f,
                LastEndedElapsedTime = lastEndedElapsedTime,
                HasEnded = 0
            };
        }

        public static TacticalFollowAttackCinematicStateComponent EvaluateStateProgress(
            TacticalFollowAttackCinematicStateComponent state)
        {
            state.ProjectileProgress = EvaluateProjectileProgress(state.ElapsedUnscaledSeconds);
            state.ProjectilePosition = math.lerp(state.LaunchPosition, state.ImpactPosition, state.ProjectileProgress);
            state.ProjectileDirection = NormalizeFlatOrFallback(state.ImpactPosition - state.LaunchPosition);

            if (state.ElapsedUnscaledSeconds >= ProjectileLaunchBeatSeconds)
            {
                state.LaunchEventTriggered = 1;
                state.ProjectileActive = state.ImpactEventTriggered == 0 ? (byte)1 : (byte)0;
            }

            if (state.ElapsedUnscaledSeconds >= ImpactEventBeatSeconds)
            {
                state.ImpactEventTriggered = 1;
                state.ProjectileActive = 0;
            }

            TacticalFollowAttackCinematicPhase phase =
                EvaluatePhase(state.ElapsedUnscaledSeconds, out _);
            if (phase == TacticalFollowAttackCinematicPhase.Flyover)
                state.FlyoverEventTriggered = 1;

            if (IsFinished(state.ElapsedUnscaledSeconds))
            {
                state.Completed = 1;
                state.AbortReason = TacticalFollowAttackCinematicAbortReason.Completed;
            }

            return state;
        }

        public static float EvaluateProjectileProgress(float elapsedSeconds)
        {
            if (elapsedSeconds <= ProjectileLaunchBeatSeconds)
                return 0f;

            if (elapsedSeconds >= ImpactEventBeatSeconds)
                return 1f;

            float t = (elapsedSeconds - ProjectileLaunchBeatSeconds) /
                      (ImpactEventBeatSeconds - ProjectileLaunchBeatSeconds);
            return math.smoothstep(0f, 1f, t);
        }

        public static bool IsFinished(float elapsedSeconds)
        {
            return elapsedSeconds >= TotalDurationSeconds;
        }

        public static TacticalFollowAttackCinematicPhase EvaluatePhase(
            float elapsedSeconds,
            out float phaseElapsedSeconds)
        {
            if (elapsedSeconds < LaunchDurationSeconds)
            {
                phaseElapsedSeconds = math.max(0f, elapsedSeconds);
                return TacticalFollowAttackCinematicPhase.Launch;
            }

            float missilePathEnd = LaunchDurationSeconds + MissilePathDurationSeconds;
            if (elapsedSeconds < missilePathEnd)
            {
                phaseElapsedSeconds = elapsedSeconds - LaunchDurationSeconds;
                return TacticalFollowAttackCinematicPhase.MissilePath;
            }

            float impactEnd = missilePathEnd + ImpactDurationSeconds;
            if (elapsedSeconds < impactEnd)
            {
                phaseElapsedSeconds = elapsedSeconds - missilePathEnd;
                return TacticalFollowAttackCinematicPhase.Impact;
            }

            if (elapsedSeconds < TotalDurationSeconds)
            {
                phaseElapsedSeconds = elapsedSeconds - impactEnd;
                return TacticalFollowAttackCinematicPhase.Flyover;
            }

            phaseElapsedSeconds = 0f;
            return TacticalFollowAttackCinematicPhase.None;
        }

        public static float EvaluateTimeScale(float elapsedSeconds)
        {
            float rampEnd = ImpactEventBeatSeconds + ImpactDurationSeconds;
            float rampStart = rampEnd - TimeScaleRampSeconds;
            if (elapsedSeconds < rampStart)
                return SlowMotionTimeScale;

            if (elapsedSeconds < rampEnd)
            {
                float t = (elapsedSeconds - rampStart) / TimeScaleRampSeconds;
                return math.lerp(SlowMotionTimeScale, 1f, math.smoothstep(0f, 1f, t));
            }

            return 1f;
        }

        public static Shot EvaluateShot(
            TacticalFollowAttackCinematicPhase phase,
            float phaseElapsedSeconds,
            in ShotContext context)
        {
            switch (phase)
            {
                case TacticalFollowAttackCinematicPhase.Launch:
                    return EvaluateLaunchShot(phaseElapsedSeconds, context);
                case TacticalFollowAttackCinematicPhase.MissilePath:
                    return EvaluateMissilePathShot(phaseElapsedSeconds, context);
                case TacticalFollowAttackCinematicPhase.Impact:
                    return EvaluateImpactShot(phaseElapsedSeconds, context);
                default:
                    return EvaluateFlyoverShot(phaseElapsedSeconds, context);
            }
        }

        public static TacticalFollowCameraPoseComponent BuildPose(in Shot shot, bool snapToShot)
        {
            float damping = snapToShot ? 0f : math.max(0f, shot.PositionDampingSeconds);
            return new TacticalFollowCameraPoseComponent
            {
                Valid = 1,
                Source = TacticalFollowCameraPoseSource.TemporaryMissile,
                DesiredPosition = shot.CameraPosition,
                DesiredRotation = quaternion.LookRotationSafe(
                    math.normalizesafe(shot.LookAt - shot.CameraPosition, new float3(0f, 0f, 1f)),
                    new float3(0f, 1f, 0f)),
                LookAt = shot.LookAt,
                FieldOfView = shot.FieldOfView,
                OrthographicSize = 0f,
                Orthographic = 0,
                PositionDampingSeconds = damping,
                RotationDampingSeconds = damping,
                MaxTransitionSpeed = FollowMaxTransitionSpeed
            };
        }

        public static bool ShouldSnapToShot(
            TacticalFollowAttackCinematicPhase previousPhase,
            TacticalFollowAttackCinematicPhase currentPhase)
        {
            if (currentPhase == TacticalFollowAttackCinematicPhase.None)
                return false;

            return previousPhase == TacticalFollowAttackCinematicPhase.None ||
                   previousPhase != currentPhase;
        }

        public static TacticalFollowCameraTargetComponent BuildTarget(
            Entity targetEntity,
            float3 impactPosition,
            float3 attackDirection)
        {
            return new TacticalFollowCameraTargetComponent
            {
                Valid = 1,
                TargetKind = TacticalFollowCameraTargetKind.AttackImpact,
                TargetEntity = targetEntity,
                Center = impactPosition,
                LookAt = impactPosition + new float3(0f, CinematicTargetRadius * 0.5f, 0f),
                ForwardHint = NormalizeFlatOrFallback(attackDirection),
                BoundsRadius = CinematicTargetRadius,
                DesiredDistance = CinematicDesiredDistance,
                DesiredHeight = CinematicDesiredHeight
            };
        }

        private static Shot EvaluateMissilePathShot(float phaseElapsedSeconds, in ShotContext context)
        {
            float3 dir = context.AttackDirection;
            float3 right = math.normalizesafe(
                math.cross(new float3(0f, 1f, 0f), dir),
                new float3(1f, 0f, 0f));
            float globalElapsed = LaunchDurationSeconds + math.max(0f, phaseElapsedSeconds);
            float projectileProgress = EvaluateProjectileProgress(globalElapsed);
            float3 projectilePosition = math.lerp(
                context.LaunchPosition,
                context.ImpactPosition,
                projectileProgress);

            float3 cameraPosition = projectilePosition
                - dir * MissilePathCameraBackDistance
                + right * MissilePathCameraSideDistance
                + new float3(0f, MissilePathCameraHeight, 0f);
            cameraPosition = ApplyCommonShotSafety(cameraPosition, lookAt: projectilePosition, context);

            float3 lookAt = math.lerp(
                projectilePosition + dir * MissilePathLookAheadDistance,
                context.ImpactPosition + new float3(0f, 1.2f, 0f),
                math.smoothstep(0.65f, 1f, projectileProgress));
            cameraPosition = ApplyCommonShotSafety(cameraPosition, lookAt, context);

            return BuildShot(cameraPosition, lookAt, MissilePathFieldOfView, MissilePathDampingSeconds);
        }

        private static Shot EvaluateLaunchShot(float phaseElapsedSeconds, in ShotContext context)
        {
            float3 dir = context.AttackDirection;
            float3 right = math.normalizesafe(
                math.cross(new float3(0f, 1f, 0f), dir),
                new float3(1f, 0f, 0f));
            float3 anchor = context.HasJet ? context.JetPosition : context.LaunchPosition;

            float pushIn = math.lerp(
                1f,
                LaunchPushInScale,
                math.smoothstep(0f, 1f, phaseElapsedSeconds / LaunchDurationSeconds));
            float3 cameraPosition = anchor
                - dir * (LaunchCameraBackDistance * pushIn)
                + right * (LaunchCameraSideDistance * pushIn)
                + new float3(0f, LaunchCameraHeightAboveJet, 0f);

            float3 lookAhead = anchor + dir * LaunchLookAheadDistance;
            float3 lookAt = math.lerp(lookAhead, context.ImpactPosition, LaunchLookImpactBlend);
            cameraPosition = ApplyCommonShotSafety(cameraPosition, lookAt, context);

            return BuildShot(cameraPosition, lookAt, LaunchFieldOfView, LaunchDampingSeconds);
        }

        private static Shot EvaluateImpactShot(float phaseElapsedSeconds, in ShotContext context)
        {
            float3 dir = context.AttackDirection;
            float3 right = math.normalizesafe(
                math.cross(new float3(0f, 1f, 0f), dir),
                new float3(1f, 0f, 0f));

            // Camera sits past the target, off to the side, looking back into the
            // explosion with the jet approaching in the background; a slow orbital
            // drift keeps the shot alive.
            float driftRadians = math.radians(
                math.lerp(0f, ImpactOrbitDegrees, math.saturate(phaseElapsedSeconds / ImpactDurationSeconds)));
            float3 flatOffset = dir * ImpactCameraForwardDistance + right * ImpactCameraSideDistance;
            float3 rotatedOffset = math.rotate(quaternion.RotateY(driftRadians), flatOffset);
            float3 cameraPosition = context.ImpactPosition + rotatedOffset + new float3(0f, ImpactCameraHeight, 0f);

            float3 lookAt = context.ImpactPosition
                + new float3(0f, ImpactLookHeight - ImpactHudSafeLookDrop, 0f)
                - dir * ImpactLookBackDistance;
            cameraPosition = ApplyCommonShotSafety(cameraPosition, lookAt, context);

            return BuildShot(cameraPosition, lookAt, ImpactFieldOfView, ImpactDampingSeconds);
        }

        private static Shot EvaluateFlyoverShot(float phaseElapsedSeconds, in ShotContext context)
        {
            float3 dir = context.AttackDirection;
            float3 right = math.normalizesafe(
                math.cross(new float3(0f, 1f, 0f), dir),
                new float3(1f, 0f, 0f));
            float normalized = math.saturate(phaseElapsedSeconds / FlyoverDurationSeconds);

            float3 jetLook = context.HasJet
                ? context.JetPosition
                : context.ImpactPosition + dir * 30f + new float3(0f, 8f, 0f);

            // Pan up from the wreck to track the jet passing over it.
            float lookBlend = math.smoothstep(0f, FlyoverLookRampNormalized, normalized) * FlyoverLookJetWeight;
            float3 wreckLook = context.ImpactPosition + new float3(0f, 1.5f - FlyoverHudSafeLookDrop, 0f);
            float3 lookAt = math.lerp(wreckLook, jetLook, lookBlend);

            float3 cameraPosition = context.ImpactPosition
                - dir * FlyoverCameraBackDistance
                + right * FlyoverCameraSideDistance
                + new float3(0f, FlyoverCameraHeight, 0f);

            // Ease toward the follow pose near the end so the hand-back to the
            // third-person camera has a short distance to cover.
            if (normalized > FlyoverExitBlendStartNormalized)
            {
                float exitBlend = math.smoothstep(FlyoverExitBlendStartNormalized, 1f, normalized) * FlyoverExitBlendWeight;
                float3 followPosition = jetLook
                    - dir * FlyoverFollowBackDistance
                    + new float3(0f, FlyoverFollowHeight, 0f);
                cameraPosition = math.lerp(cameraPosition, followPosition, exitBlend);
            }

            cameraPosition = ApplyCommonShotSafety(cameraPosition, lookAt, context);

            return BuildShot(cameraPosition, lookAt, FlyoverFieldOfView, FlyoverDampingSeconds);
        }

        private static Shot BuildShot(
            float3 cameraPosition,
            float3 lookAt,
            float fieldOfView,
            float dampingSeconds)
        {
            return new Shot
            {
                CameraPosition = cameraPosition,
                LookAt = lookAt,
                FieldOfView = math.clamp(fieldOfView, MinCinematicFieldOfView, MaxCinematicFieldOfView),
                PositionDampingSeconds = dampingSeconds
            };
        }

        private static float3 NormalizeFlatOrFallback(float3 direction)
        {
            direction.y = 0f;
            float lengthSq = math.lengthsq(direction);
            return lengthSq <= 0.0001f
                ? new float3(0f, 0f, 1f)
                : direction * math.rsqrt(lengthSq);
        }

        private static float3 ApplyCommonShotSafety(
            float3 cameraPosition,
            float3 lookAt,
            in ShotContext context)
        {
            float actionY = math.max(context.LaunchPosition.y, context.ImpactPosition.y);
            if (context.HasJet)
                actionY = math.max(actionY, context.JetPosition.y);
            cameraPosition.y = math.max(cameraPosition.y, actionY + MinCameraClearanceAboveAction);

            cameraPosition = PushAwayFrom(cameraPosition, lookAt, MinCameraDistanceFromLookAt);
            cameraPosition = PushAwayFrom(cameraPosition, context.ImpactPosition, MinCameraDistanceFromImpact);
            if (context.HasJet)
                cameraPosition = PushAwayFrom(cameraPosition, context.JetPosition, MinCameraDistanceFromJet);

            return cameraPosition;
        }

        private static float3 PushAwayFrom(float3 cameraPosition, float3 anchor, float minDistance)
        {
            float3 offset = cameraPosition - anchor;
            float distanceSq = math.lengthsq(offset);
            float minDistanceSq = minDistance * minDistance;
            if (distanceSq >= minDistanceSq)
                return cameraPosition;

            float3 direction = math.normalizesafe(offset, new float3(0f, 1f, -1f));
            return anchor + direction * minDistance;
        }
    }
}
