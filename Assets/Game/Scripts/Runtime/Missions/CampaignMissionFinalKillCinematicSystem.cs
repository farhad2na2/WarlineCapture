using Game.Components;
using Game.Missions.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UnitDeathSystem))]
    [UpdateBefore(typeof(CampaignMissionRuntimeSystem))]
    public partial struct CampaignMissionFinalKillCinematicSystem : ISystem
    {
        private const string ReducedMotionPreferenceKey = "Game.Settings.Accessibility.ReducedMotion";

        private EntityQuery _cameraFocusQuery;
        private EntityQuery _cameraSnapshotQuery;
        private EntityQuery _attackCinematicQuery;
        private FixedString64Bytes _trackedSession;
        private int _trackedAttempt;
        private int _previousLiveHostiles;
        private MissionPhaseKind _previousPhase;
        private Entity _candidate;
        private float3 _candidatePosition;
        private float3 _restoreFocus;
        private float4 _restorePerspective;
        private byte _hasRestorePose;
        private byte _timeScaleApplied;
        private byte _timeScaleForfeited;
        private byte _timeScaleRestorePending;
        private float _baseTimeScale;
        private float _appliedTimeScale;

        public void OnCreate(ref SystemState state)
        {
            _cameraFocusQuery = state.GetEntityQuery(ComponentType.ReadWrite<RuntimeCameraFocusRequestComponent>());
            _cameraSnapshotQuery = state.GetEntityQuery(ComponentType.ReadOnly<RuntimeCameraSnapshotComponent>());
            _attackCinematicQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<TacticalFollowAttackCinematicStateComponent>());
            state.RequireForUpdate<CampaignMissionRootComponent>();
            state.RequireForUpdate<CampaignMissionRuntimeComponent>();
        }

        public void OnDestroy(ref SystemState state)
        {
            RestoreTimeScale();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<CampaignMissionRootComponent>(out Entity root))
                return;

            EntityManager em = state.EntityManager;
            if (_timeScaleRestorePending != 0)
                RestoreTimeScale();

            CampaignMissionRuntimeComponent runtime = em.GetComponentData<CampaignMissionRuntimeComponent>(root);
            if (!em.HasComponent<CampaignMissionFinalKillCinematicComponent>(root))
                em.AddComponentData(root, default(CampaignMissionFinalKillCinematicComponent));
            CampaignMissionFinalKillCinematicComponent cinematic =
                em.GetComponentData<CampaignMissionFinalKillCinematicComponent>(root);

            if (!_trackedSession.Equals(runtime.SessionToken) || _trackedAttempt != runtime.AttemptOrdinal)
            {
                if (cinematic.Active != 0)
                    End(em, root, ref cinematic, restoreCamera: false);
                _trackedSession = runtime.SessionToken;
                _trackedAttempt = runtime.AttemptOrdinal;
                _previousLiveHostiles = 0;
                _previousPhase = runtime.Phase;
                _candidate = Entity.Null;
            }

            if (cinematic.Active != 0)
            {
                Advance(em, root, in runtime, ref cinematic);
                _previousPhase = runtime.Phase;
                return;
            }

            if (!CampaignMissionFinalKillCinematicHelper.IsCombatPhase(runtime.Phase) &&
                !CampaignMissionFinalKillCinematicHelper.IsCombatPhase(_previousPhase))
            {
                _previousLiveHostiles = 0;
                _previousPhase = runtime.Phase;
                return;
            }

            int liveHostiles = CountLiveHostiles(ref state, in runtime.SessionToken);
            if (CampaignMissionFinalKillCinematicHelper.ShouldTrigger(
                    _previousLiveHostiles, liveHostiles, _previousPhase, runtime.Phase, runtime.Outcome))
            {
                float3 victim = _candidate != Entity.Null && em.HasComponent<LocalTransform>(_candidate)
                    ? em.GetComponentData<LocalTransform>(_candidate).Position
                    : _candidatePosition;
                TryStart(ref state, root, in runtime, victim, ref cinematic);
            }

            _previousLiveHostiles = liveHostiles;
            _previousPhase = runtime.Phase;
        }

        public static bool IsHoldingMissionResult(EntityManager entityManager, Entity root)
        {
            if (root == Entity.Null ||
                !entityManager.HasComponent<CampaignMissionFinalKillCinematicComponent>(root) ||
                !entityManager.HasComponent<CampaignMissionRuntimeComponent>(root))
                return false;
            CampaignMissionRuntimeComponent runtime = entityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
            CampaignMissionFinalKillCinematicComponent cinematic =
                entityManager.GetComponentData<CampaignMissionFinalKillCinematicComponent>(root);
            return CampaignMissionFinalKillCinematicHelper.IsActive(in cinematic, in runtime.SessionToken);
        }

        private int CountLiveHostiles(ref SystemState state, in FixedString64Bytes sessionToken)
        {
            int live = 0;
            int weakestHealth = int.MaxValue;
            Entity weakest = Entity.Null;
            float3 weakestPosition = default;
            foreach ((RefRO<CampaignMissionUnitRoleComponent> role,
                      RefRO<Faction> faction,
                      RefRO<UnitHealth> health,
                      RefRO<UnitCombat> combat,
                      RefRO<LocalTransform> pose,
                      Entity entity) in
                     SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>, RefRO<Faction>, RefRO<UnitHealth>,
                         RefRO<UnitCombat>, RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (health.ValueRO.Current <= 0 || combat.ValueRO.CanAttack == 0 ||
                    !FactionIdentity.IsHostileToPlayer(faction.ValueRO.Id) ||
                    !role.ValueRO.SessionToken.Equals(sessionToken))
                    continue;

                live++;
                if (health.ValueRO.Current < weakestHealth)
                {
                    weakestHealth = health.ValueRO.Current;
                    weakest = entity;
                    weakestPosition = pose.ValueRO.Position;
                }
            }

            if (live > 0)
            {
                _candidate = weakest;
                _candidatePosition = weakestPosition;
            }

            return live;
        }

        private bool TryFindNearestFriendly(ref SystemState state, in FixedString64Bytes sessionToken,
            float3 victim, out float3 friendly)
        {
            friendly = default;
            float bestDistanceSq = float.MaxValue;
            foreach ((RefRO<CampaignMissionUnitRoleComponent> role,
                      RefRO<Faction> faction,
                      RefRO<UnitHealth> health,
                      RefRO<UnitCombat> combat,
                      RefRO<LocalTransform> pose) in
                     SystemAPI.Query<RefRO<CampaignMissionUnitRoleComponent>, RefRO<Faction>, RefRO<UnitHealth>,
                         RefRO<UnitCombat>, RefRO<LocalTransform>>())
            {
                if (health.ValueRO.Current <= 0 || combat.ValueRO.CanAttack == 0 ||
                    !FactionIdentity.IsPlayerControlled(faction.ValueRO.Id) ||
                    !role.ValueRO.SessionToken.Equals(sessionToken))
                    continue;

                float distanceSq = math.distancesq(pose.ValueRO.Position, victim);
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    friendly = pose.ValueRO.Position;
                }
            }

            return bestDistanceSq < float.MaxValue;
        }

        private void TryStart(ref SystemState state, Entity root, in CampaignMissionRuntimeComponent runtime,
            float3 victim, ref CampaignMissionFinalKillCinematicComponent cinematic)
        {
            EntityManager em = state.EntityManager;
            if (IsReducedMotion(em, root, in runtime) || IsAttackCinematicActive() ||
                _cameraFocusQuery.CalculateEntityCount() != 1)
                return;

            _hasRestorePose = 0;
            if (_cameraSnapshotQuery.CalculateEntityCount() == 1 &&
                CampaignMissionPatrolOrderSystem.TryCaptureTourStart(
                    _cameraSnapshotQuery.GetSingleton<RuntimeCameraSnapshotComponent>(),
                    victim.y,
                    out _restoreFocus,
                    out _restorePerspective))
            {
                _hasRestorePose = 1;
            }

            bool hasFriendly = TryFindNearestFriendly(ref state, in runtime.SessionToken, victim, out float3 friendly);
            float fallbackYaw = _hasRestorePose != 0 ? _restorePerspective.z : 0f;
            em.SetComponentData(
                _cameraFocusQuery.GetSingletonEntity(),
                CampaignMissionFinalKillCinematicHelper.BuildShot(victim, friendly, hasFriendly, fallbackYaw));

            cinematic.SessionToken = runtime.SessionToken;
            cinematic.AttemptOrdinal = runtime.AttemptOrdinal;
            cinematic.VictimPosition = victim;
            cinematic.ElapsedUnscaledSeconds = 0f;
            cinematic.Version++;
            cinematic.Active = 1;
            em.SetComponentData(root, cinematic);

            _timeScaleForfeited = 0;
            ApplyTimeScale(CampaignMissionFinalKillCinematicHelper.EvaluateTimeScale(0f));
        }

        private void Advance(EntityManager em, Entity root, in CampaignMissionRuntimeComponent runtime,
            ref CampaignMissionFinalKillCinematicComponent cinematic)
        {
            if (runtime.Outcome == MissionOutcomeKind.Defeat)
            {
                End(em, root, ref cinematic, restoreCamera: false);
                return;
            }

            if (Time.timeScale > 0f)
            {
                cinematic.ElapsedUnscaledSeconds += math.min(
                    Time.unscaledDeltaTime,
                    CampaignMissionFinalKillCinematicHelper.MaxFrameAdvanceSeconds);
            }

            if (CampaignMissionFinalKillCinematicHelper.IsFinished(cinematic.ElapsedUnscaledSeconds))
            {
                End(em, root, ref cinematic,
                    CampaignMissionFinalKillCinematicHelper.ShouldRestoreCamera(runtime.Phase, runtime.Outcome));
                return;
            }

            ApplyTimeScale(CampaignMissionFinalKillCinematicHelper.EvaluateTimeScale(cinematic.ElapsedUnscaledSeconds));
            em.SetComponentData(root, cinematic);
        }

        private void End(EntityManager em, Entity root, ref CampaignMissionFinalKillCinematicComponent cinematic,
            bool restoreCamera)
        {
            RestoreTimeScale();
            if (restoreCamera && _hasRestorePose != 0 && _cameraFocusQuery.CalculateEntityCount() == 1)
            {
                em.SetComponentData(_cameraFocusQuery.GetSingletonEntity(), new RuntimeCameraFocusRequestComponent
                {
                    Requested = 1,
                    Smooth = 1,
                    UseExplicitPerspective = 1,
                    SmoothTimeSeconds = CampaignMissionFinalKillCinematicHelper.RestoreSmoothTimeSeconds,
                    Perspective = _restorePerspective,
                    World = _restoreFocus
                });
            }

            _hasRestorePose = 0;
            cinematic.Active = 0;
            em.SetComponentData(root, cinematic);
        }

        private bool IsAttackCinematicActive() =>
            _attackCinematicQuery.CalculateEntityCount() == 1 &&
            _attackCinematicQuery.GetSingleton<TacticalFollowAttackCinematicStateComponent>().Active != 0;

        private static bool IsReducedMotion(EntityManager em, Entity root, in CampaignMissionRuntimeComponent runtime)
        {
            if (PlayerPrefs.GetInt(ReducedMotionPreferenceKey, 0) != 0)
                return true;
            if (!em.HasComponent<CampaignMissionCameraTourState>(root))
                return false;
            CampaignMissionCameraTourState tour = em.GetComponentData<CampaignMissionCameraTourState>(root);
            return tour.ReducedMotion != 0 && tour.SessionToken.Equals(runtime.SessionToken);
        }

        // The pause bridge and the tactical attack cinematic also write Time.timeScale.
        // Yield when another owner changes it, and never restore over a pause.
        private void ApplyTimeScale(float factor)
        {
            if (!Application.isPlaying || _timeScaleForfeited != 0)
                return;

            float current = Time.timeScale;
            if (current <= 0f)
                return;

            if (_timeScaleApplied == 0)
            {
                _baseTimeScale = current;
                _timeScaleApplied = 1;
            }
            else if (math.abs(current - _appliedTimeScale) > 0.0001f)
            {
                _timeScaleApplied = 0;
                _timeScaleForfeited = 1;
                return;
            }

            _appliedTimeScale = math.max(0.01f, _baseTimeScale * factor);
            Time.timeScale = _appliedTimeScale;
        }

        private void RestoreTimeScale()
        {
            if (!Application.isPlaying || _timeScaleApplied == 0)
            {
                _timeScaleRestorePending = 0;
                return;
            }

            float current = Time.timeScale;
            if (current <= 0f)
            {
                _timeScaleRestorePending = 1;
                return;
            }

            if (math.abs(current - _appliedTimeScale) <= 0.0001f)
                Time.timeScale = _baseTimeScale;
            _timeScaleApplied = 0;
            _timeScaleRestorePending = 0;
        }
    }
}
