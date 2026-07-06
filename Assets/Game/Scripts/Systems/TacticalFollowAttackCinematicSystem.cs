using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitAttackSystem))]
    [UpdateBefore(typeof(UnitAttackVfxRequestSystem))]
    public partial struct TacticalFollowAttackCinematicSystem : ISystem
    {
        private const float AttackImpactHoldSeconds = 1.15f;
        private const float AttackImpactRadius = 3f;
        private const float AttackImpactDesiredDistance = 14f;
        private const float AttackImpactDesiredHeight = 6f;

        private EntityQuery _modeQuery;
        private EntityQuery _requestQuery;
        private EntityQuery _targetQuery;

        public void OnCreate(ref SystemState state)
        {
            _modeQuery = state.GetEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraModeComponent>());
            _requestQuery = state.GetEntityQuery(ComponentType.ReadOnly<UnitAttackVfxRequest>());
            _targetQuery = state.GetEntityQuery(ComponentType.ReadWrite<TacticalFollowCameraTargetComponent>());
            state.RequireForUpdate(_modeQuery);
            state.RequireForUpdate(_requestQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            Entity modeEntity = _modeQuery.GetSingletonEntity();
            TacticalFollowCameraModeComponent mode = em.GetComponentData<TacticalFollowCameraModeComponent>(modeEntity);
            if (mode.Enabled == 0 ||
                mode.HasBaseTarget == 0 ||
                IsTemporaryCutawayActive(mode, (float)SystemAPI.Time.ElapsedTime))
            {
                return;
            }

            float now = (float)SystemAPI.Time.ElapsedTime;
            foreach (RefRO<UnitAttackVfxRequest> requestRef in SystemAPI.Query<RefRO<UnitAttackVfxRequest>>())
            {
                UnitAttackVfxRequest request = requestRef.ValueRO;
                if (!IsAttackCutawayRequest(request) ||
                    !IsFollowedAirAttackSource(em, modeEntity, mode, request.Source))
                {
                    continue;
                }

                TacticalFollowCameraTargetComponent target = BuildAttackImpactTarget(request);
                mode.HasTemporaryTarget = 1;
                mode.TemporaryTargetKind = TacticalFollowCameraTargetKind.AttackImpact;
                mode.TemporaryTargetEntity = target.TargetEntity;
                mode.TemporaryTargetStartedTime = now;
                mode.ReturnHoldUntilTime = now + AttackImpactHoldSeconds;
                em.SetComponentData(modeEntity, mode);
                em.SetComponentData(EnsureTargetEntity(em, _targetQuery), target);
                return;
            }
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

        private static TacticalFollowCameraTargetComponent BuildAttackImpactTarget(UnitAttackVfxRequest request)
        {
            UnitAttackVfxRequestKind kind = (UnitAttackVfxRequestKind)request.Kind;
            float3 center = kind == UnitAttackVfxRequestKind.Impact
                ? request.PlaybackPosition
                : request.TargetPosition;
            float3 forward = NormalizeFlatOrFallback(center - request.SourcePosition);
            return new TacticalFollowCameraTargetComponent
            {
                Valid = 1,
                TargetKind = TacticalFollowCameraTargetKind.AttackImpact,
                TargetEntity = request.Target,
                Center = center,
                LookAt = center + new float3(0f, AttackImpactRadius * 0.5f, 0f),
                ForwardHint = forward,
                BoundsRadius = AttackImpactRadius,
                DesiredDistance = AttackImpactDesiredDistance,
                DesiredHeight = AttackImpactDesiredHeight
            };
        }

        private static Entity EnsureTargetEntity(EntityManager em, EntityQuery targetQuery)
        {
            if (!targetQuery.IsEmptyIgnoreFilter)
                return targetQuery.GetSingletonEntity();

            Entity entity = em.CreateEntity(typeof(TacticalFollowCameraTargetComponent));
            em.SetName(entity, "TacticalFollowCameraTarget");
            return entity;
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
