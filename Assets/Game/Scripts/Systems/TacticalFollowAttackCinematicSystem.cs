using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitAttackSystem))]
    [UpdateBefore(typeof(UnitAttackVfxRequestSystem))]
    public partial struct TacticalFollowAttackCinematicSystem : ISystem
    {
        private EntityQuery _modeQuery;
        private EntityQuery _targetQuery;
        private EntityQuery _poseQuery;
        private EntityQuery _cinematicQuery;

        public void OnCreate(ref SystemState state)
        {
            _modeQuery = state.GetEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraModeComponent>());
            _targetQuery = state.GetEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraTargetComponent>());
            _poseQuery = state.GetEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraPoseComponent>());
            _cinematicQuery = state.GetEntityQuery(ComponentType.ReadWrite<TacticalFollowAttackCinematicStateComponent>());
            state.RequireForUpdate(_modeQuery);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_cinematicQuery.IsEmptyIgnoreFilter)
                return;

            Entity cinematicEntity = _cinematicQuery.GetSingletonEntity();
            TacticalFollowAttackCinematicStateComponent cinematic =
                state.EntityManager.GetComponentData<TacticalFollowAttackCinematicStateComponent>(cinematicEntity);
            RestoreTimeScale(ref cinematic);
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            Entity cinematicEntity = EnsureCinematicStateEntity(em, _cinematicQuery);
            TacticalFollowAttackCinematicStateComponent cinematic =
                em.GetComponentData<TacticalFollowAttackCinematicStateComponent>(cinematicEntity);

            if (cinematic.Active != 0)
            {
                UpdateActiveCinematic(ref state, cinematicEntity, cinematic);
                return;
            }

            TryStartCinematic(ref state, cinematicEntity, cinematic);
        }

        private void TryStartCinematic(
            ref SystemState state,
            Entity cinematicEntity,
            TacticalFollowAttackCinematicStateComponent cinematic)
        {
            EntityManager em = state.EntityManager;
            Entity modeEntity = _modeQuery.GetSingletonEntity();
            TacticalFollowCameraModeComponent mode = em.GetComponentData<TacticalFollowCameraModeComponent>(modeEntity);
            float now = (float)SystemAPI.Time.ElapsedTime;
            if (mode.Enabled == 0 ||
                mode.HasBaseTarget == 0 ||
                IsTemporaryCutawayActive(mode, now) ||
                (cinematic.HasEnded != 0 &&
                 now - cinematic.LastEndedElapsedTime < TacticalFollowAttackCinematicHelper.RetriggerCooldownSeconds))
            {
                return;
            }

            bool found = false;
            bool hasLaunch = false;
            UnitAttackVfxRequest selectedRequest = default;
            float3 launchPosition = default;
            float3 impactPosition = default;
            UnityObjectRef<GameObject> launchVfxPrefab = default;
            UnityObjectRef<GameObject> impactVfxPrefab = default;
            quaternion launchVfxRotation = quaternion.identity;
            quaternion impactVfxRotation = quaternion.identity;
            foreach (RefRO<UnitAttackVfxRequest> requestRef in SystemAPI.Query<RefRO<UnitAttackVfxRequest>>())
            {
                UnitAttackVfxRequest request = requestRef.ValueRO;
                if (!IsAttackCutawayRequest(request) ||
                    !IsFollowedAirAttackSource(em, modeEntity, mode, request.Source))
                {
                    continue;
                }

                UnitAttackVfxRequestKind kind = (UnitAttackVfxRequestKind)request.Kind;
                if (!found)
                {
                    found = true;
                    selectedRequest = request;
                    launchPosition = kind == UnitAttackVfxRequestKind.MuzzleFlash &&
                                     math.lengthsq(request.PlaybackPosition) > 0.0001f
                        ? request.PlaybackPosition
                        : request.SourcePosition;
                    impactPosition = ResolveImpactPosition(request);
                    hasLaunch = kind == UnitAttackVfxRequestKind.MuzzleFlash;
                    if (kind == UnitAttackVfxRequestKind.MuzzleFlash)
                    {
                        launchVfxPrefab = request.Prefab;
                        launchVfxRotation = request.PlaybackRotation;
                    }
                    else if (kind == UnitAttackVfxRequestKind.Impact)
                    {
                        impactVfxPrefab = request.Prefab;
                        impactVfxRotation = request.PlaybackRotation;
                    }
                    continue;
                }

                if (request.Source != selectedRequest.Source)
                    continue;

                if (kind == UnitAttackVfxRequestKind.MuzzleFlash && !hasLaunch)
                {
                    selectedRequest = request;
                    launchPosition = math.lengthsq(request.PlaybackPosition) > 0.0001f
                        ? request.PlaybackPosition
                        : request.SourcePosition;
                    launchVfxPrefab = request.Prefab;
                    launchVfxRotation = request.PlaybackRotation;
                    hasLaunch = true;
                }
                else if (kind == UnitAttackVfxRequestKind.Impact)
                {
                    impactPosition = ResolveImpactPosition(request);
                    impactVfxPrefab = request.Prefab;
                    impactVfxRotation = request.PlaybackRotation;
                    if (selectedRequest.Target == Entity.Null)
                        selectedRequest.Target = request.Target;
                }
            }

            if (!found)
                return;

            cinematic = TacticalFollowAttackCinematicHelper.BuildInitialState(
                selectedRequest.Source,
                selectedRequest.Target,
                launchPosition,
                impactPosition,
                impactPosition - launchPosition,
                launchVfxPrefab,
                impactVfxPrefab,
                launchVfxRotation,
                impactVfxRotation,
                now,
                cinematic.LastEndedElapsedTime);

            TacticalFollowAttackCinematicHelper.ShotContext context = BuildShotContext(em, cinematic);
            TacticalFollowAttackCinematicHelper.Shot shot =
                TacticalFollowAttackCinematicCameraSystemHelper.EvaluateShotWithObstructionFallback(
                    TacticalFollowAttackCinematicPhase.Launch,
                    0f,
                    context);

            ApplyTimeScale(ref cinematic, 0f);
            em.SetComponentData(cinematicEntity, cinematic);

            mode.HasTemporaryTarget = 1;
            mode.TemporaryTargetKind = TacticalFollowCameraTargetKind.AttackImpact;
            mode.TemporaryTargetEntity = selectedRequest.Target;
            mode.TemporaryTargetStartedTime = now;
            mode.ReturnHoldUntilTime = 0f;
            em.SetComponentData(modeEntity, mode);
            em.SetComponentData(
                EnsureTargetEntity(em, _targetQuery),
                TacticalFollowAttackCinematicHelper.BuildTarget(
                    selectedRequest.Target,
                    impactPosition,
                    cinematic.AttackDirection));
            em.SetComponentData(
                EnsurePoseEntity(em, _poseQuery),
                TacticalFollowAttackCinematicHelper.BuildPose(shot, snapToShot: true));
        }

        private void UpdateActiveCinematic(
            ref SystemState state,
            Entity cinematicEntity,
            TacticalFollowAttackCinematicStateComponent cinematic)
        {
            EntityManager em = state.EntityManager;
            Entity modeEntity = _modeQuery.GetSingletonEntity();
            TacticalFollowCameraModeComponent mode = em.GetComponentData<TacticalFollowCameraModeComponent>(modeEntity);
            float now = (float)SystemAPI.Time.ElapsedTime;
            if (mode.Enabled == 0 ||
                mode.HasTemporaryTarget == 0 ||
                mode.TemporaryTargetKind != TacticalFollowCameraTargetKind.AttackImpact)
            {
                TacticalFollowAttackCinematicAbortReason reason = mode.Enabled == 0
                    ? TacticalFollowAttackCinematicAbortReason.FollowModeExited
                    : TacticalFollowAttackCinematicAbortReason.TemporaryTargetCleared;
                EndCinematicWithoutTouchingMode(em, cinematicEntity, ref cinematic, now, reason);
                return;
            }

            float previousElapsed = cinematic.ElapsedUnscaledSeconds;
            float divisor = 1f;
            if (cinematic.TimeScaleApplied != 0)
            {
                divisor = math.max(
                    0.01f,
                    math.max(0.01f, cinematic.SavedTimeScale) *
                    TacticalFollowAttackCinematicHelper.EvaluateTimeScale(previousElapsed));
            }

            cinematic.ElapsedUnscaledSeconds += SystemAPI.Time.DeltaTime / divisor;
            byte previousLaunchTriggered = cinematic.LaunchEventTriggered;
            byte previousImpactTriggered = cinematic.ImpactEventTriggered;
            cinematic = TacticalFollowAttackCinematicHelper.EvaluateStateProgress(cinematic);
            PlayTimelineVfx(cinematicEntity, cinematic, previousLaunchTriggered, previousImpactTriggered);
            if (TacticalFollowAttackCinematicHelper.IsFinished(cinematic.ElapsedUnscaledSeconds))
            {
                RestoreTimeScale(ref cinematic);
                TacticalFollowAttackCinematicVfxSystemHelper.ReleaseProjectile(cinematicEntity);
                cinematic.Active = 0;
                cinematic.HasEnded = 1;
                cinematic.Completed = 1;
                cinematic.AbortReason = TacticalFollowAttackCinematicAbortReason.Completed;
                cinematic.LastEndedElapsedTime = now;
                em.SetComponentData(cinematicEntity, cinematic);

                mode.HasTemporaryTarget = 0;
                mode.TemporaryTargetKind = TacticalFollowCameraTargetKind.None;
                mode.TemporaryTargetEntity = Entity.Null;
                mode.TemporaryTargetStartedTime = 0f;
                mode.ReturnHoldUntilTime = 0f;
                em.SetComponentData(modeEntity, mode);
                return;
            }

            TacticalFollowAttackCinematicPhase phase =
                TacticalFollowAttackCinematicHelper.EvaluatePhase(
                    cinematic.ElapsedUnscaledSeconds,
                    out float phaseElapsed);
            bool snapToShot = TacticalFollowAttackCinematicHelper.ShouldSnapToShot(
                cinematic.LastAppliedPhase,
                phase);
            TacticalFollowAttackCinematicHelper.ShotContext context = BuildShotContext(em, cinematic);
            TacticalFollowAttackCinematicHelper.Shot shot =
                TacticalFollowAttackCinematicCameraSystemHelper.EvaluateShotWithObstructionFallback(
                    phase,
                    phaseElapsed,
                    context);

            em.SetComponentData(
                EnsureTargetEntity(em, _targetQuery),
                TacticalFollowAttackCinematicHelper.BuildTarget(
                    cinematic.TargetEntity,
                    cinematic.ImpactPosition,
                    cinematic.AttackDirection));
            em.SetComponentData(
                EnsurePoseEntity(em, _poseQuery),
                TacticalFollowAttackCinematicHelper.BuildPose(shot, snapToShot));

            cinematic.LastAppliedPhase = phase;
            ApplyTimeScale(ref cinematic, cinematic.ElapsedUnscaledSeconds);
            em.SetComponentData(cinematicEntity, cinematic);
        }

        private static TacticalFollowAttackCinematicHelper.ShotContext BuildShotContext(
            EntityManager em,
            TacticalFollowAttackCinematicStateComponent cinematic)
        {
            bool hasJet = cinematic.SourceEntity != Entity.Null &&
                          em.Exists(cinematic.SourceEntity) &&
                          em.HasComponent<LocalTransform>(cinematic.SourceEntity);
            float3 jetPosition = hasJet
                ? em.GetComponentData<LocalTransform>(cinematic.SourceEntity).Position
                : cinematic.LaunchPosition;
            return new TacticalFollowAttackCinematicHelper.ShotContext(
                cinematic.LaunchPosition,
                cinematic.ImpactPosition,
                cinematic.AttackDirection,
                jetPosition,
                hasJet);
        }

        private static void EndCinematicWithoutTouchingMode(
            EntityManager em,
            Entity cinematicEntity,
            ref TacticalFollowAttackCinematicStateComponent cinematic,
            float now,
            TacticalFollowAttackCinematicAbortReason reason)
        {
            RestoreTimeScale(ref cinematic);
            TacticalFollowAttackCinematicVfxSystemHelper.ReleaseProjectile(cinematicEntity);
            cinematic.Active = 0;
            cinematic.HasEnded = 1;
            cinematic.AbortReason = reason;
            cinematic.LastEndedElapsedTime = now;
            em.SetComponentData(cinematicEntity, cinematic);
        }

        private static void PlayTimelineVfx(
            Entity cinematicEntity,
            TacticalFollowAttackCinematicStateComponent cinematic,
            byte previousLaunchTriggered,
            byte previousImpactTriggered)
        {
            if (previousLaunchTriggered == 0 &&
                cinematic.LaunchEventTriggered != 0)
            {
                TacticalFollowAttackCinematicVfxSystemHelper.PlayLaunch(cinematic);
            }

            if (cinematic.ProjectileActive != 0)
                TacticalFollowAttackCinematicVfxSystemHelper.SyncProjectile(cinematicEntity, cinematic);

            if (previousImpactTriggered == 0 &&
                cinematic.ImpactEventTriggered != 0)
            {
                TacticalFollowAttackCinematicVfxSystemHelper.ReleaseProjectile(cinematicEntity);
                TacticalFollowAttackCinematicVfxSystemHelper.PlayImpact(cinematic);
            }
        }

        private static void ApplyTimeScale(
            ref TacticalFollowAttackCinematicStateComponent cinematic,
            float elapsedSeconds)
        {
            if (!Application.isPlaying)
                return;

            if (cinematic.TimeScaleApplied == 0)
            {
                cinematic.SavedTimeScale = Time.timeScale;
                cinematic.TimeScaleApplied = 1;
            }

            Time.timeScale = math.max(
                0.01f,
                cinematic.SavedTimeScale *
                TacticalFollowAttackCinematicHelper.EvaluateTimeScale(elapsedSeconds));
        }

        private static void RestoreTimeScale(ref TacticalFollowAttackCinematicStateComponent cinematic)
        {
            if (!Application.isPlaying ||
                cinematic.TimeScaleApplied == 0)
            {
                return;
            }

            Time.timeScale = math.max(0.01f, cinematic.SavedTimeScale);
            cinematic.TimeScaleApplied = 0;
        }

        private static bool IsTemporaryCutawayActive(TacticalFollowCameraModeComponent mode, float now)
        {
            return mode.HasTemporaryTarget != 0 &&
                   (mode.ReturnHoldUntilTime <= 0f || now < mode.ReturnHoldUntilTime);
        }

        private static bool IsAttackCutawayRequest(UnitAttackVfxRequest request)
        {
            UnitAttackVfxRequestKind kind = (UnitAttackVfxRequestKind)request.Kind;
            return kind == UnitAttackVfxRequestKind.MuzzleFlash ||
                   kind == UnitAttackVfxRequestKind.Impact;
        }

        private static bool IsFollowedAirAttackSource(
            EntityManager em,
            Entity modeEntity,
            TacticalFollowCameraModeComponent mode,
            Entity source)
        {
            if (source == Entity.Null ||
                !em.Exists(source) ||
                !em.HasComponent<UnitAirMovement>(source))
            {
                return false;
            }

            if (mode.BaseTargetKind == TacticalFollowCameraTargetKind.Unit &&
                mode.BaseTargetEntity == source)
            {
                return true;
            }

            if (mode.BaseTargetKind != TacticalFollowCameraTargetKind.UnitGroup ||
                !em.HasBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity))
            {
                return false;
            }

            DynamicBuffer<TacticalFollowCameraBaseTargetElement> baseTargets =
                em.GetBuffer<TacticalFollowCameraBaseTargetElement>(modeEntity);
            for (int i = 0; i < baseTargets.Length; i++)
            {
                if (baseTargets[i].Entity == source)
                    return true;
            }

            return false;
        }

        private static Entity EnsureTargetEntity(EntityManager em, EntityQuery targetQuery)
        {
            if (!targetQuery.IsEmptyIgnoreFilter)
                return targetQuery.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(TacticalFollowCameraTargetComponent));
            em.SetName(entity, "TacticalFollowCameraTarget");
            return entity;
        }

        private static Entity EnsurePoseEntity(EntityManager em, EntityQuery poseQuery)
        {
            if (!poseQuery.IsEmptyIgnoreFilter)
                return poseQuery.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(TacticalFollowCameraPoseComponent));
            em.SetName(entity, "TacticalFollowCameraPose");
            return entity;
        }

        private static Entity EnsureCinematicStateEntity(EntityManager em, EntityQuery cinematicQuery)
        {
            if (!cinematicQuery.IsEmptyIgnoreFilter)
                return cinematicQuery.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(TacticalFollowAttackCinematicStateComponent));
            em.SetName(entity, "TacticalFollowAttackCinematicState");
            return entity;
        }

        private static float3 ResolveImpactPosition(UnitAttackVfxRequest request)
        {
            return (UnitAttackVfxRequestKind)request.Kind == UnitAttackVfxRequestKind.Impact
                ? request.PlaybackPosition
                : request.TargetPosition;
        }

        private static float3 NormalizeFlatOrFallback(float3 direction)
        {
            direction.y = 0f;
            float lengthSq = math.lengthsq(direction);
            return lengthSq <= 0.0001f
                ? new float3(0f, 0f, 1f)
                : direction * math.rsqrt(lengthSq);
        }
    }
}
