using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    [UpdateAfter(typeof(UnitEngagementSystem))]
    public partial struct AirMissileLauncherSupportLinkSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirMissileLauncherComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;

            foreach (var (launcher, launcherState, launcherTransform, launcherFaction, entity) in SystemAPI
                         .Query<RefRO<AirMissileLauncherComponent>, RefRW<AirMissileLauncherStateComponent>, RefRO<LocalTransform>, RefRO<Faction>>()
                         .WithNone<UnitDeathAnimationComponent>()
                         .WithEntityAccess())
            {
                float rangeBonus = 0f;
                float lockMultiplier = 1f;
                float trackingBonus = 0f;
                float turnRateBonus = 0f;
                Entity radarProvider = Entity.Null;
                Entity satelliteProvider = Entity.Null;

                foreach (var (support, supportTransform, supportFaction, supportEntity) in SystemAPI
                             .Query<RefRO<AirDefenseSupportProviderComponent>, RefRO<LocalTransform>, RefRO<Faction>>()
                             .WithNone<UnitDeathAnimationComponent>()
                             .WithEntityAccess())
                {
                    if (supportFaction.ValueRO.Id != launcherFaction.ValueRO.Id)
                        continue;

                    AirDefenseSupportProviderComponent supportRo = support.ValueRO;
                    float radius = math.max(0f, supportRo.SupportRadius);
                    if (radius <= 0f)
                        continue;

                    float3 delta = supportTransform.ValueRO.Position - launcherTransform.ValueRO.Position;
                    delta.y = 0f;
                    if (math.lengthsq(delta) > radius * radius)
                        continue;

                    if (supportRo.Kind == (byte)AirDefenseSupportProviderKind.Radar)
                    {
                        if (supportRo.RangeBonus > rangeBonus)
                            radarProvider = supportEntity;
                    }
                    else if (supportRo.Kind == (byte)AirDefenseSupportProviderKind.Satellite)
                    {
                        satelliteProvider = supportEntity;
                    }

                    rangeBonus += math.max(0f, supportRo.RangeBonus);
                    lockMultiplier = math.min(lockMultiplier, math.clamp(supportRo.LockTimeMultiplier, 0.1f, 1f));
                    trackingBonus += math.max(0f, supportRo.TrackingBonus);
                    turnRateBonus += math.max(0f, supportRo.TurnRateBonus);
                }

                AirMissileLauncherComponent launcherRo = launcher.ValueRO;
                rangeBonus = math.min(rangeBonus, launcherRo.MaxSupportRangeBonus);
                trackingBonus = math.min(trackingBonus, launcherRo.MaxSupportTrackingBonus);

                if (em.HasComponent<AirDefenseSupportLinkComponent>(entity))
                {
                    em.SetComponentData(entity, new AirDefenseSupportLinkComponent
                    {
                        RangeBonus = rangeBonus,
                        LockTimeMultiplier = lockMultiplier,
                        TrackingBonus = trackingBonus,
                        TurnRateBonus = turnRateBonus,
                        RadarProvider = radarProvider,
                        SatelliteProvider = satelliteProvider
                    });
                }

                ref AirMissileLauncherStateComponent stateRw = ref launcherState.ValueRW;
                stateRw.EffectiveRange = math.min(launcherRo.MaxDetectionRange, launcherRo.BaseDetectionRange + rangeBonus);
                stateRw.EffectiveLockSeconds = math.max(0.01f, launcherRo.LockSeconds * lockMultiplier);
                stateRw.EffectiveTrackingQuality = math.saturate(launcherRo.TrackingQuality + trackingBonus);
                stateRw.EffectiveTurnRateDegreesPerSecond = math.max(1f, launcherRo.MissileTurnRateDegreesPerSecond + turnRateBonus);
            }
        }
    }

    [UpdateAfter(typeof(AirMissileLauncherSupportLinkSystem))]
    public partial struct AirMissileLauncherTargetAcquisitionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirMissileLauncherComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            float dt = math.max(0.0001f, SystemAPI.Time.DeltaTime);
            ComponentLookup<UnitPrevWorldPos> prevLookup = SystemAPI.GetComponentLookup<UnitPrevWorldPos>(true);
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (launcher, launcherState, transform, faction, entity) in SystemAPI
                         .Query<RefRO<AirMissileLauncherComponent>, RefRO<AirMissileLauncherStateComponent>, RefRO<LocalTransform>, RefRO<Faction>>()
                         .WithNone<UnitDeathAnimationComponent>()
                         .WithEntityAccess())
            {
                if (em.HasComponent<UnitHealth>(entity) && em.GetComponentData<UnitHealth>(entity).Current <= 0)
                {
                    ClearTarget(ecb, em, entity);
                    continue;
                }

                AirMissileLauncherComponent launcherRo = launcher.ValueRO;
                AirMissileLauncherStateComponent stateRo = launcherState.ValueRO;
                float effectiveRange = math.max(launcherRo.MinRange, stateRo.EffectiveRange > 0f ? stateRo.EffectiveRange : launcherRo.BaseDetectionRange);
                float minRange = math.max(0f, launcherRo.MinRange);
                float bestScore = float.MinValue;
                AirMissileLauncherTargetComponent best = default;
                bool found = false;

                foreach (var (missile, missileTransform, missileTargetEntity) in SystemAPI
                             .Query<RefRO<MissileInterceptionTargetComponent>, RefRO<LocalTransform>>()
                             .WithEntityAccess())
                {
                    if (missile.ValueRO.FactionId == faction.ValueRO.Id)
                        continue;

                    float3 targetPosition = missileTransform.ValueRO.Position;
                    if (!IsInRange(transform.ValueRO.Position, targetPosition, minRange, effectiveRange))
                        continue;

                    float3 velocity = ResolveVelocity(prevLookup, missileTargetEntity, targetPosition, dt);
                    float score = launcherRo.IncomingMissilePriority - HorizontalDistance(transform.ValueRO.Position, targetPosition) * 0.02f;
                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    best = BuildTarget(missileTargetEntity, AirMissileTargetKind.IncomingGroundMissile, targetPosition, velocity, score);
                    found = true;
                }

                foreach (var (airMovement, targetTransform, targetFaction, targetHealth, targetEntity) in SystemAPI
                             .Query<RefRO<UnitAirMovement>, RefRO<LocalTransform>, RefRO<Faction>, RefRO<UnitHealth>>()
                             .WithNone<UnitDeathAnimationComponent>()
                             .WithEntityAccess())
                {
                    if (targetEntity == entity ||
                        targetFaction.ValueRO.Id == faction.ValueRO.Id ||
                        targetHealth.ValueRO.Current <= 0)
                    {
                        continue;
                    }

                    float3 targetPosition = targetTransform.ValueRO.Position;
                    if (!IsInRange(transform.ValueRO.Position, targetPosition, minRange, effectiveRange))
                        continue;

                    float3 velocity = ResolveVelocity(prevLookup, targetEntity, targetPosition, dt);
                    float score = launcherRo.AirTargetPriority - HorizontalDistance(transform.ValueRO.Position, targetPosition) * 0.01f;
                    if (score <= bestScore)
                        continue;

                    bestScore = score;
                    best = BuildTarget(targetEntity, AirMissileTargetKind.EnemyAirUnit, targetPosition, velocity, score);
                    found = true;
                }

                if (!found)
                {
                    ClearTarget(ecb, em, entity);
                    continue;
                }

                if (em.HasComponent<AirMissileLauncherTargetComponent>(entity))
                    ecb.SetComponent(entity, best);
                else
                    ecb.AddComponent(entity, best);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        private static AirMissileLauncherTargetComponent BuildTarget(
            Entity target,
            AirMissileTargetKind kind,
            float3 position,
            float3 velocity,
            float score)
        {
            return new AirMissileLauncherTargetComponent
            {
                Target = target,
                TargetKind = (byte)kind,
                TargetWorldPosition = position,
                TargetVelocity = velocity,
                PredictedInterceptPosition = position + velocity * 0.35f,
                Score = score
            };
        }

        private static void ClearTarget(EntityCommandBuffer ecb, EntityManager em, Entity entity)
        {
            if (em.HasComponent<AirMissileLauncherTargetComponent>(entity))
                ecb.RemoveComponent<AirMissileLauncherTargetComponent>(entity);
        }

        private static bool IsInRange(float3 source, float3 target, float minRange, float maxRange)
        {
            float distance = HorizontalDistance(source, target);
            return distance >= minRange && distance <= maxRange;
        }

        private static float HorizontalDistance(float3 a, float3 b)
        {
            return math.distance(new float2(a.x, a.z), new float2(b.x, b.z));
        }

        private static float3 ResolveVelocity(
            ComponentLookup<UnitPrevWorldPos> prevLookup,
            Entity entity,
            float3 current,
            float dt)
        {
            if (!prevLookup.HasComponent(entity))
                return float3.zero;

            return (current - prevLookup[entity].Value) / dt;
        }
    }

    [UpdateAfter(typeof(AirMissileLauncherTargetAcquisitionSystem))]
    public partial struct AirMissileLauncherTurretAimSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirMissileLauncherVisualReferenceComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            float dt = SystemAPI.Time.DeltaTime;

            foreach (var (launcher, target, visual, launcherTransform) in SystemAPI
                         .Query<RefRO<AirMissileLauncherComponent>, RefRO<AirMissileLauncherTargetComponent>, RefRO<AirMissileLauncherVisualReferenceComponent>, RefRO<LocalTransform>>())
            {
                Entity turretEntity = visual.ValueRO.Turret;
                if (turretEntity == Entity.Null || !em.HasComponent<LocalTransform>(turretEntity))
                    continue;

                float desiredYaw = ResolveDesiredLocalYaw(launcherTransform.ValueRO, target.ValueRO.PredictedInterceptPosition);
                LocalTransform turretTransform = em.GetComponentData<LocalTransform>(turretEntity);
                float currentYaw = ResolveCurrentYaw(turretTransform.Rotation, visual.ValueRO.TurretDefaultLocalRotation);
                float maxStep = math.radians(math.max(1f, launcher.ValueRO.TurretYawSpeedDegreesPerSecond)) * dt;
                float newYaw = MoveAngleRadians(currentYaw, desiredYaw, maxStep);
                turretTransform.Position = visual.ValueRO.TurretDefaultLocalPosition;
                turretTransform.Rotation = math.mul(visual.ValueRO.TurretDefaultLocalRotation, quaternion.RotateY(newYaw));
                em.SetComponentData(turretEntity, turretTransform);
            }
        }

        internal static float ResolveDesiredLocalYaw(LocalTransform launcherTransform, float3 targetWorldPosition)
        {
            float3 worldDirection = targetWorldPosition - launcherTransform.Position;
            worldDirection.y = 0f;
            if (math.lengthsq(worldDirection) < 1e-6f)
                return 0f;

            float3 localDirection = math.rotate(math.inverse(launcherTransform.Rotation), math.normalizesafe(worldDirection, new float3(0f, 0f, 1f)));
            return math.atan2(localDirection.x, localDirection.z);
        }

        internal static float MoveAngleRadians(float current, float target, float maxStep)
        {
            float delta = math.atan2(math.sin(target - current), math.cos(target - current));
            if (math.abs(delta) <= maxStep)
                return target;

            return current + math.sign(delta) * maxStep;
        }

        private static float ResolveCurrentYaw(quaternion current, quaternion defaultRotation)
        {
            quaternion localDelta = math.mul(math.inverse(defaultRotation), current);
            float3 forward = math.rotate(localDelta, new float3(0f, 0f, 1f));
            forward.y = 0f;
            if (math.lengthsq(forward) < 1e-6f)
                return 0f;

            return math.atan2(forward.x, forward.z);
        }
    }

    [UpdateAfter(typeof(AirMissileLauncherTurretAimSystem))]
    public partial struct AirMissileLauncherFireControlSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirMissileLauncherComponent>();
            state.RequireForUpdate<AirMissileLauncherStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            float dt = SystemAPI.Time.DeltaTime;
            ComponentLookup<LocalToWorld> localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (launcher, launcherState, launcherTransform, faction, entity) in SystemAPI
                         .Query<RefRO<AirMissileLauncherComponent>, RefRW<AirMissileLauncherStateComponent>, RefRO<LocalTransform>, RefRO<Faction>>()
                         .WithNone<UnitDeathAnimationComponent>()
                         .WithEntityAccess())
            {
                ref AirMissileLauncherStateComponent stateRw = ref launcherState.ValueRW;
                if (stateRw.Phase == (byte)AirMissileLauncherPhase.Reloading)
                {
                    stateRw.Timer = math.max(0f, stateRw.Timer - dt);
                    if (stateRw.Timer <= 0f)
                        ResetLauncherState(ref stateRw, launcher.ValueRO);
                    continue;
                }

                if (!em.HasComponent<AirMissileLauncherTargetComponent>(entity))
                {
                    ResetLauncherState(ref stateRw, launcher.ValueRO);
                    continue;
                }

                AirMissileLauncherTargetComponent target = em.GetComponentData<AirMissileLauncherTargetComponent>(entity);
                if (!IsTargetStillValid(em, target))
                {
                    ResetLauncherState(ref stateRw, launcher.ValueRO);
                    continue;
                }

                if (IsDebugFireTargetForLauncher(em, target, entity))
                {
                    stateRw.Phase = (byte)AirMissileLauncherPhase.Launching;
                    stateRw.TargetEntity = target.Target;
                    stateRw.TargetKind = target.TargetKind;
                    stateRw.TargetWorldPosition = target.TargetWorldPosition;
                    stateRw.PredictedInterceptPosition = target.PredictedInterceptPosition;
                    stateRw.Timer = 0f;
                    LaunchMissile(em, ecb, entity, launcher.ValueRO, ref stateRw, launcherTransform.ValueRO, faction.ValueRO.Id, target, localToWorldLookup);
                    continue;
                }

                bool aimed = IsTurretAimed(em, launcherTransform.ValueRO, launcher.ValueRO, entity, target.PredictedInterceptPosition);
                if (!aimed)
                {
                    stateRw.Phase = (byte)AirMissileLauncherPhase.Tracking;
                    stateRw.TargetEntity = target.Target;
                    stateRw.TargetKind = target.TargetKind;
                    stateRw.TargetWorldPosition = target.TargetWorldPosition;
                    stateRw.PredictedInterceptPosition = target.PredictedInterceptPosition;
                    stateRw.Timer = stateRw.EffectiveLockSeconds + launcher.ValueRO.LaunchDelaySeconds;
                    continue;
                }

                if (stateRw.Phase != (byte)AirMissileLauncherPhase.Tracking &&
                    stateRw.Phase != (byte)AirMissileLauncherPhase.Locked &&
                    stateRw.Phase != (byte)AirMissileLauncherPhase.Launching)
                {
                    stateRw.Timer = stateRw.EffectiveLockSeconds + launcher.ValueRO.LaunchDelaySeconds;
                }

                stateRw.Phase = stateRw.Timer > launcher.ValueRO.LaunchDelaySeconds
                    ? (byte)AirMissileLauncherPhase.Tracking
                    : (byte)AirMissileLauncherPhase.Locked;
                stateRw.TargetEntity = target.Target;
                stateRw.TargetKind = target.TargetKind;
                stateRw.TargetWorldPosition = target.TargetWorldPosition;
                stateRw.PredictedInterceptPosition = target.PredictedInterceptPosition;
                stateRw.Timer = math.max(0f, stateRw.Timer - dt);
                if (stateRw.Timer > 0f)
                    continue;

                LaunchMissile(em, ecb, entity, launcher.ValueRO, ref stateRw, launcherTransform.ValueRO, faction.ValueRO.Id, target, localToWorldLookup);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        private static void LaunchMissile(
            EntityManager em,
            EntityCommandBuffer ecb,
            Entity launcherEntity,
            AirMissileLauncherComponent launcher,
            ref AirMissileLauncherStateComponent state,
            LocalTransform launcherTransform,
            byte factionId,
            AirMissileLauncherTargetComponent target,
            ComponentLookup<LocalToWorld> localToWorldLookup)
        {
            Entity missileEntity = Entity.Null;
            AirMissileLauncherMissileVisualComponent missileVisual = default;
            if (em.HasBuffer<AirMissileLauncherMissileVisualComponent>(launcherEntity))
            {
                DynamicBuffer<AirMissileLauncherMissileVisualComponent> missiles = em.GetBuffer<AirMissileLauncherMissileVisualComponent>(launcherEntity);
                if (missiles.Length > 0)
                {
                    int selectedIndex = (state.SelectedMissileSlot + 1 + missiles.Length) % missiles.Length;
                    missileVisual = missiles[selectedIndex];
                    missileEntity = missileVisual.Missile;
                    state.SelectedMissileSlot = missileVisual.SlotIndex;
                }
            }

            float3 start = ResolveLaunchPosition(em, launcherEntity, missileEntity, launcherTransform.Position, localToWorldLookup);
            float3 direction = math.normalizesafe(target.PredictedInterceptPosition - start, math.mul(launcherTransform.Rotation, new float3(0f, 0f, 1f)));
            quaternion rotation = quaternion.LookRotationSafe(direction, math.up());
            Entity projectileEntity = missileEntity != Entity.Null && em.Exists(missileEntity)
                ? missileEntity
                : ecb.CreateEntity();
            bool usesExistingMissileVisual = projectileEntity == missileEntity;

            Entity originalParent = Entity.Null;
            if (usesExistingMissileVisual)
            {
                originalParent = em.HasComponent<Parent>(missileEntity) ? em.GetComponentData<Parent>(missileEntity).Value : Entity.Null;
                if (originalParent != Entity.Null)
                    ecb.RemoveComponent<Parent>(missileEntity);
                ecb.AddComponent(missileEntity, new AirMissileFlyingVisualComponent
                {
                    Launcher = launcherEntity,
                    OriginalParent = originalParent,
                    SlotIndex = missileVisual.SlotIndex,
                    InitialLocalPosition = missileVisual.InitialLocalPosition,
                    InitialLocalRotation = missileVisual.InitialLocalRotation,
                    InitialLocalScale = missileVisual.InitialLocalScale
                });
            }

            LocalTransform projectileTransform = LocalTransform.FromPositionRotationScale(start, rotation, 1f);
            if (usesExistingMissileVisual)
                ecb.SetComponent(projectileEntity, projectileTransform);
            else
                ecb.AddComponent(projectileEntity, projectileTransform);
            ecb.AddComponent(projectileEntity, new AirMissileProjectileComponent
            {
                Source = launcherEntity,
                Target = target.Target,
                TargetKind = target.TargetKind,
                FactionId = factionId,
                Velocity = direction * math.max(0.01f, launcher.MissileSpeed),
                Speed = launcher.MissileSpeed,
                Acceleration = launcher.MissileAcceleration,
                TurnRateDegreesPerSecond = state.EffectiveTurnRateDegreesPerSecond > 0f
                    ? state.EffectiveTurnRateDegreesPerSecond
                    : launcher.MissileTurnRateDegreesPerSecond,
                LifetimeSeconds = launcher.MissileLifetimeSeconds,
                ProximityFuseRadius = launcher.ProximityFuseRadius,
                ElapsedSeconds = 0f,
                Damage = target.TargetKind == (byte)AirMissileTargetKind.IncomingGroundMissile
                    ? launcher.IncomingMissileDamage
                    : launcher.AirTargetDamage,
                TrackingQuality = state.EffectiveTrackingQuality > 0f ? state.EffectiveTrackingQuality : launcher.TrackingQuality
            });

            if (em.HasComponent<AirMissileLauncherVfxReferenceComponent>(launcherEntity))
            {
                AirMissileLauncherVfxReferenceComponent vfx = em.GetComponentData<AirMissileLauncherVfxReferenceComponent>(launcherEntity);
                CombatGameObjectVfxRequests.Enqueue(
                    ecb,
                    vfx.LaunchFlashPrefab,
                    start,
                    rotation,
                    CombatGameObjectVfxRequestKind.Play);
                if (!usesExistingMissileVisual || !em.HasComponent<AirMissileProjectileTrailComponent>(projectileEntity))
                {
                    ecb.AddComponent(projectileEntity, new AirMissileProjectileTrailComponent
                    {
                        TimeUntilNextTrail = 0f,
                        TrailIntervalSeconds = 0f
                    });
                }
            }

            state.Phase = (byte)AirMissileLauncherPhase.Reloading;
            state.Timer = math.max(0.01f, launcher.ReloadSeconds);
            state.TargetEntity = target.Target;
            state.TargetKind = target.TargetKind;
            state.TargetWorldPosition = target.TargetWorldPosition;
            state.PredictedInterceptPosition = target.PredictedInterceptPosition;
        }

        private static float3 ResolveLaunchPosition(
            EntityManager em,
            Entity launcher,
            Entity missile,
            float3 fallback,
            ComponentLookup<LocalToWorld> localToWorldLookup)
        {
            if (missile != Entity.Null && localToWorldLookup.HasComponent(missile))
                return localToWorldLookup[missile].Position;

            if (em.HasComponent<AirMissileLauncherVisualReferenceComponent>(launcher))
            {
                AirMissileLauncherVisualReferenceComponent visual = em.GetComponentData<AirMissileLauncherVisualReferenceComponent>(launcher);
                if (visual.LaunchSpawn != Entity.Null && localToWorldLookup.HasComponent(visual.LaunchSpawn))
                    return localToWorldLookup[visual.LaunchSpawn].Position;
                if (visual.Turret != Entity.Null && localToWorldLookup.HasComponent(visual.Turret))
                    return localToWorldLookup[visual.Turret].Position;
            }

            return fallback;
        }

        private static bool IsTargetStillValid(EntityManager em, AirMissileLauncherTargetComponent target)
        {
            if (target.Target == Entity.Null || !em.Exists(target.Target) || !em.HasComponent<LocalTransform>(target.Target))
                return false;

            if (target.TargetKind == (byte)AirMissileTargetKind.IncomingGroundMissile)
                return em.HasComponent<MissileInterceptionTargetComponent>(target.Target);

            return em.HasComponent<UnitAirMovement>(target.Target) &&
                   em.HasComponent<UnitHealth>(target.Target) &&
                   em.GetComponentData<UnitHealth>(target.Target).Current > 0;
        }

        private static bool IsDebugFireTargetForLauncher(
            EntityManager em,
            AirMissileLauncherTargetComponent target,
            Entity launcherEntity)
        {
            return target.Target != Entity.Null &&
                   em.Exists(target.Target) &&
                   em.HasComponent<DebugFireTargetTag>(target.Target) &&
                   em.GetComponentData<DebugFireTargetTag>(target.Target).Source == launcherEntity;
        }

        private static bool IsTurretAimed(
            EntityManager em,
            LocalTransform launcherTransform,
            AirMissileLauncherComponent launcher,
            Entity launcherEntity,
            float3 targetPosition)
        {
            if (!em.HasComponent<AirMissileLauncherVisualReferenceComponent>(launcherEntity))
                return true;

            AirMissileLauncherVisualReferenceComponent visual = em.GetComponentData<AirMissileLauncherVisualReferenceComponent>(launcherEntity);
            if (visual.Turret == Entity.Null || !em.HasComponent<LocalTransform>(visual.Turret))
                return true;

            float desiredYaw = AirMissileLauncherTurretAimSystem.ResolveDesiredLocalYaw(launcherTransform, targetPosition);
            LocalTransform turret = em.GetComponentData<LocalTransform>(visual.Turret);
            quaternion localDelta = math.mul(math.inverse(visual.TurretDefaultLocalRotation), turret.Rotation);
            float3 forward = math.rotate(localDelta, new float3(0f, 0f, 1f));
            forward.y = 0f;
            float currentYaw = math.lengthsq(forward) < 1e-6f ? 0f : math.atan2(forward.x, forward.z);
            float delta = math.degrees(math.abs(math.atan2(math.sin(desiredYaw - currentYaw), math.cos(desiredYaw - currentYaw))));
            return delta <= math.max(0.1f, launcher.AimToleranceDegrees);
        }

        private static void ResetLauncherState(ref AirMissileLauncherStateComponent state, AirMissileLauncherComponent launcher)
        {
            state.Phase = (byte)AirMissileLauncherPhase.Idle;
            state.TargetEntity = Entity.Null;
            state.TargetKind = (byte)AirMissileTargetKind.None;
            state.TargetWorldPosition = float3.zero;
            state.PredictedInterceptPosition = float3.zero;
            state.Timer = 0f;
            state.EffectiveRange = state.EffectiveRange > 0f ? state.EffectiveRange : launcher.BaseDetectionRange;
            state.EffectiveLockSeconds = state.EffectiveLockSeconds > 0f ? state.EffectiveLockSeconds : launcher.LockSeconds;
            state.EffectiveTrackingQuality = state.EffectiveTrackingQuality > 0f ? state.EffectiveTrackingQuality : launcher.TrackingQuality;
            state.EffectiveTurnRateDegreesPerSecond = state.EffectiveTurnRateDegreesPerSecond > 0f
                ? state.EffectiveTurnRateDegreesPerSecond
                : launcher.MissileTurnRateDegreesPerSecond;
        }

    }

    [UpdateAfter(typeof(AirMissileHomingProjectileSystem))]
    [UpdateBefore(typeof(AirMissileImpactSystem))]
    public partial struct AirMissileProjectileTrailSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirMissileProjectileTrailComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (projectile, transform, entity) in SystemAPI
                         .Query<RefRO<AirMissileProjectileComponent>, RefRO<LocalTransform>>()
                         .WithAll<AirMissileProjectileTrailComponent>()
                         .WithEntityAccess())
            {
                float3 direction = math.normalizesafe(projectile.ValueRO.Velocity, math.rotate(transform.ValueRO.Rotation, new float3(0f, 0f, 1f)));
                MissileTrailVfxView.Sync(entity, transform.ValueRO.Position, direction);
            }
        }
    }

    [UpdateAfter(typeof(AirMissileLauncherFireControlSystem))]
    [UpdateAfter(typeof(GroundMissileProjectileFlightSystem))]
    [BurstCompile]
    public partial struct AirMissileHomingProjectileSystem : ISystem
    {
        private EntityQuery _airTargetQuery;
        private EntityQuery _incomingMissileTargetQuery;
        private EntityQuery _incomingMissileVisualQuery;

        public void OnCreate(ref SystemState state)
        {
            _airTargetQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<UnitAirMovement>(),
                ComponentType.ReadOnly<LocalTransform>());
            _incomingMissileTargetQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<MissileInterceptionTargetComponent>(),
                ComponentType.ReadOnly<GroundMissileProjectileComponent>(),
                ComponentType.ReadOnly<LocalTransform>());
            _incomingMissileVisualQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<GroundMissileFlyingRocketVisualComponent>(),
                ComponentType.ReadOnly<LocalTransform>());
            state.RequireForUpdate<AirMissileProjectileComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            int targetCapacity = math.max(1, _airTargetQuery.CalculateEntityCount() + _incomingMissileTargetQuery.CalculateEntityCount());
            var targetSamples = new NativeParallelHashMap<Entity, MovingTargetSample>(targetCapacity, Allocator.TempJob);
            var visualSamplesByLauncher = new NativeParallelHashMap<Entity, MovingTargetSample>(
                math.max(1, _incomingMissileVisualQuery.CalculateEntityCount()),
                Allocator.TempJob);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);

            var targetCollectDependency = new CollectAirTargetPositionJob
            {
                TargetSamples = targetSamples.AsParallelWriter()
            }.ScheduleParallel(_airTargetQuery, state.Dependency);
            targetCollectDependency = new CollectIncomingMissileVisualPositionJob
            {
                DeltaTime = dt,
                VisualSamplesByLauncher = visualSamplesByLauncher.AsParallelWriter()
            }.ScheduleParallel(_incomingMissileVisualQuery, targetCollectDependency);
            targetCollectDependency = new CollectIncomingMissileTargetPositionJob
            {
                DeltaTime = dt,
                VisualSamplesByLauncher = visualSamplesByLauncher,
                TargetSamples = targetSamples.AsParallelWriter()
            }.ScheduleParallel(_incomingMissileTargetQuery, targetCollectDependency);

            state.Dependency = new HomingProjectileJob
            {
                DeltaTime = dt,
                TargetSamples = targetSamples,
                Ecb = ecb.AsParallelWriter()
            }.ScheduleParallel(targetCollectDependency);
            state.Dependency.Complete();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            targetSamples.Dispose();
            visualSamplesByLauncher.Dispose();
        }

        [BurstCompile]
        private partial struct CollectAirTargetPositionJob : IJobEntity
        {
            public NativeParallelHashMap<Entity, MovingTargetSample>.ParallelWriter TargetSamples;

            private void Execute(Entity entity, in UnitAirMovement airMovement, in LocalTransform transform)
            {
                TargetSamples.TryAdd(entity, new MovingTargetSample
                {
                    PreviousPosition = transform.Position,
                    CurrentPosition = transform.Position
                });
            }
        }

        [BurstCompile]
        private partial struct CollectIncomingMissileVisualPositionJob : IJobEntity
        {
            public float DeltaTime;
            public NativeParallelHashMap<Entity, MovingTargetSample>.ParallelWriter VisualSamplesByLauncher;

            private void Execute(
                in GroundMissileFlyingRocketVisualComponent flying,
                in LocalTransform transform)
            {
                if (flying.Launcher == Entity.Null)
                    return;

                float duration = math.max(0.01f, flying.DurationSeconds);
                float previousElapsed = math.max(0f, flying.ElapsedSeconds - math.max(0f, DeltaTime));
                VisualSamplesByLauncher.TryAdd(flying.Launcher, new MovingTargetSample
                {
                    PreviousPosition = EvaluateGroundMissileVisualPosition(flying, previousElapsed / duration),
                    CurrentPosition = transform.Position
                });
            }

            private static float3 EvaluateGroundMissileVisualPosition(GroundMissileFlyingRocketVisualComponent flying, float t)
            {
                float3 position = math.lerp(flying.StartPosition, flying.TargetPosition, math.saturate(t));
                position.y += math.sin(math.saturate(t) * math.PI) * math.max(0f, flying.ArcHeight);
                return position;
            }
        }

        [BurstCompile]
        private partial struct CollectIncomingMissileTargetPositionJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public NativeParallelHashMap<Entity, MovingTargetSample> VisualSamplesByLauncher;
            public NativeParallelHashMap<Entity, MovingTargetSample>.ParallelWriter TargetSamples;

            private void Execute(
                Entity entity,
                in MissileInterceptionTargetComponent interceptionTarget,
                in GroundMissileProjectileComponent projectile,
                in LocalTransform transform)
            {
                if (VisualSamplesByLauncher.TryGetValue(projectile.Source, out MovingTargetSample visualSample))
                {
                    TargetSamples.TryAdd(entity, visualSample);
                    return;
                }

                float duration = math.max(0.01f, projectile.DurationSeconds);
                float previousElapsed = math.max(0f, projectile.ElapsedSeconds - math.max(0f, DeltaTime));
                TargetSamples.TryAdd(entity, new MovingTargetSample
                {
                    PreviousPosition = EvaluateGroundMissilePosition(projectile, previousElapsed / duration),
                    CurrentPosition = transform.Position
                });
            }

            private static float3 EvaluateGroundMissilePosition(GroundMissileProjectileComponent projectile, float t)
            {
                float3 position = math.lerp(projectile.StartPosition, projectile.TargetPosition, math.saturate(t));
                position.y += math.sin(math.saturate(t) * math.PI) * math.max(0f, projectile.ArcHeight);
                return position;
            }
        }

        [BurstCompile]
        private partial struct HomingProjectileJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public NativeParallelHashMap<Entity, MovingTargetSample> TargetSamples;
            public EntityCommandBuffer.ParallelWriter Ecb;

            private void Execute(
                [EntityIndexInQuery] int entityIndexInQuery,
                Entity entity,
                ref AirMissileProjectileComponent projectile,
                ref LocalTransform transform)
            {
                projectile.ElapsedSeconds += DeltaTime;

                if (projectile.Target == entity ||
                    !TargetSamples.TryGetValue(projectile.Target, out MovingTargetSample targetSample))
                {
                    QueueImpact(Ecb, entityIndexInQuery, projectile, transform.Position, float.PositiveInfinity, entity);
                    return;
                }

                float3 targetPosition = targetSample.CurrentPosition;
                float3 desiredDirection = math.normalizesafe(targetPosition - transform.Position, math.normalizesafe(projectile.Velocity, new float3(0f, 0f, 1f)));
                float currentSpeed = math.length(projectile.Velocity);
                currentSpeed = math.max(0.01f, currentSpeed + projectile.Acceleration * DeltaTime);
                currentSpeed = math.min(currentSpeed, math.max(projectile.Speed, currentSpeed));
                float maxRadians = math.radians(math.max(1f, projectile.TurnRateDegreesPerSecond)) * DeltaTime;
                float3 currentDirection = math.normalizesafe(projectile.Velocity, desiredDirection);
                float3 newDirection = RotateTowards(currentDirection, desiredDirection, maxRadians);
                projectile.Velocity = newDirection * currentSpeed;

                float3 previousPosition = transform.Position;
                float3 newPosition = previousPosition + projectile.Velocity * DeltaTime;
                transform.Position = newPosition;
                transform.Rotation = quaternion.LookRotationSafe(newDirection, math.up());

                float proximityFuseRadius = math.max(0.1f, projectile.ProximityFuseRadius);
                SegmentClosestPoints(
                    previousPosition,
                    newPosition,
                    targetSample.PreviousPosition,
                    targetSample.CurrentPosition,
                    out float3 closestProjectilePoint,
                    out float3 closestTargetPoint,
                    out float closestApproachDistance);
                if (closestApproachDistance <= proximityFuseRadius)
                {
                    float3 impactPosition = projectile.TargetKind == (byte)AirMissileTargetKind.IncomingGroundMissile
                        ? closestTargetPoint
                        : closestProjectilePoint;
                    transform.Position = impactPosition;
                    QueueImpact(Ecb, entityIndexInQuery, projectile, impactPosition, closestApproachDistance, entity);
                    return;
                }

                if (projectile.ElapsedSeconds >= projectile.LifetimeSeconds)
                {
                    QueueMiss(Ecb, entityIndexInQuery, projectile, newPosition, entity);
                }
            }

            private static void SegmentClosestPoints(
                float3 p1,
                float3 q1,
                float3 p2,
                float3 q2,
                out float3 closest1,
                out float3 closest2,
                out float distance)
            {
                float3 d1 = q1 - p1;
                float3 d2 = q2 - p2;
                float3 r = p1 - p2;
                float a = math.lengthsq(d1);
                float e = math.lengthsq(d2);
                float f = math.dot(d2, r);
                float s;
                float t;

                if (a <= 1e-6f && e <= 1e-6f)
                {
                    s = 0f;
                    t = 0f;
                }
                else if (a <= 1e-6f)
                {
                    s = 0f;
                    t = math.saturate(f / e);
                }
                else
                {
                    float c = math.dot(d1, r);
                    if (e <= 1e-6f)
                    {
                        t = 0f;
                        s = math.saturate(-c / a);
                    }
                    else
                    {
                        float b = math.dot(d1, d2);
                        float denom = a * e - b * b;
                        s = denom != 0f ? math.saturate((b * f - c * e) / denom) : 0f;
                        t = (b * s + f) / e;

                        if (t < 0f)
                        {
                            t = 0f;
                            s = math.saturate(-c / a);
                        }
                        else if (t > 1f)
                        {
                            t = 1f;
                            s = math.saturate((b - c) / a);
                        }
                    }
                }

                closest1 = p1 + d1 * s;
                closest2 = p2 + d2 * t;
                distance = math.distance(closest1, closest2);
            }

            private static float3 RotateTowards(float3 current, float3 target, float maxRadians)
            {
                float dot = math.clamp(math.dot(current, target), -1f, 1f);
                float angle = math.acos(dot);
                if (angle <= maxRadians || angle < 1e-5f)
                    return target;

                float t = maxRadians / angle;
                return math.normalizesafe(math.lerp(current, target, t), target);
            }

            private static void QueueImpact(
                EntityCommandBuffer.ParallelWriter ecb,
                int sortKey,
                AirMissileProjectileComponent projectile,
                float3 position,
                float visualSeparation,
                Entity projectileEntity)
            {
                ecb.AddComponent(sortKey, projectileEntity, new AirMissileImpactRequestComponent
                {
                    Source = projectile.Source,
                    Target = projectile.Target,
                    TargetKind = projectile.TargetKind,
                    FactionId = projectile.FactionId,
                    Position = position,
                    VisualSeparation = visualSeparation,
                    Damage = projectile.Damage
                });
                ecb.RemoveComponent<AirMissileProjectileComponent>(sortKey, projectileEntity);
            }

            private static void QueueMiss(
                EntityCommandBuffer.ParallelWriter ecb,
                int sortKey,
                AirMissileProjectileComponent projectile,
                float3 position,
                Entity projectileEntity)
            {
                ecb.AddComponent(sortKey, projectileEntity, new AirMissileImpactRequestComponent
                {
                    Source = projectile.Source,
                    Target = Entity.Null,
                    TargetKind = (byte)AirMissileTargetKind.None,
                    FactionId = projectile.FactionId,
                    Position = position,
                    VisualSeparation = float.PositiveInfinity,
                    Damage = 0
                });
                ecb.RemoveComponent<AirMissileProjectileComponent>(sortKey, projectileEntity);
            }
        }

        private struct MovingTargetSample
        {
            public float3 PreviousPosition;
            public float3 CurrentPosition;
        }
    }

    [UpdateAfter(typeof(AirMissileHomingProjectileSystem))]
    public partial struct AirMissileImpactSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirMissileImpactRequestComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (impact, entity) in SystemAPI.Query<RefRO<AirMissileImpactRequestComponent>>().WithEntityAccess())
            {
                AirMissileImpactRequestComponent request = impact.ValueRO;

                if (request.TargetKind == (byte)AirMissileTargetKind.IncomingGroundMissile)
                {
                    if (em.Exists(request.Target) && em.HasComponent<GroundMissileProjectileComponent>(request.Target))
                    {
                        GroundMissileProjectileComponent interceptedProjectile =
                            em.GetComponentData<GroundMissileProjectileComponent>(request.Target);
                        if (!em.HasComponent<MissileInterceptedComponent>(entity))
                        {
                            ecb.AddComponent(entity, new MissileInterceptedComponent
                            {
                                Interceptor = entity,
                                VisualSeparation = math.max(0f, request.VisualSeparation)
                            });
                        }

                        ClearInterceptedGroundRocketVisual(em, ecb, interceptedProjectile.Source);
                        ecb.DestroyEntity(request.Target);
                    }
                }
                else if (em.Exists(request.Target) && em.HasComponent<UnitHealth>(request.Target))
                {
                    UnitHealth health = em.GetComponentData<UnitHealth>(request.Target);
                    health.Current = math.max(0, health.Current - math.max(0, request.Damage));
                    ecb.SetComponent(request.Target, health);
                }

                EnqueueImpactVfx(em, ecb, request);
                if (RestoreFlyingVisual(em, ecb, entity))
                    ecb.RemoveComponent<AirMissileImpactRequestComponent>(entity);
            }

            ecb.Playback(em);
            ecb.Dispose();
        }

        private static bool RestoreFlyingVisual(EntityManager em, EntityCommandBuffer ecb, Entity entity)
        {
            if (!em.HasComponent<AirMissileFlyingVisualComponent>(entity))
            {
                ecb.DestroyEntity(entity);
                return false;
            }

            AirMissileFlyingVisualComponent visual = em.GetComponentData<AirMissileFlyingVisualComponent>(entity);
            if (visual.OriginalParent != Entity.Null && !em.HasComponent<Parent>(entity))
                ecb.AddComponent(entity, new Parent { Value = visual.OriginalParent });

            ecb.SetComponent(
                entity,
                LocalTransform.FromPositionRotationScale(
                    visual.InitialLocalPosition,
                    visual.InitialLocalRotation,
                    0f));
            if (em.HasComponent<AirMissileProjectileTrailComponent>(entity))
                ecb.RemoveComponent<AirMissileProjectileTrailComponent>(entity);
            ecb.RemoveComponent<AirMissileFlyingVisualComponent>(entity);
            return true;
        }

        private static void ClearInterceptedGroundRocketVisual(EntityManager em, EntityCommandBuffer ecb, Entity launcher)
        {
            if (launcher == Entity.Null)
                return;

            using EntityQuery query = em.CreateEntityQuery(
                ComponentType.ReadOnly<GroundMissileFlyingRocketVisualComponent>(),
                ComponentType.ReadWrite<LocalTransform>());
            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                NativeArray<Entity> entities = chunks[chunkIndex].GetNativeArray(entityType);
                for (int i = 0; i < entities.Length; i++)
                {
                    Entity entity = entities[i];
                    if (!em.Exists(entity) || !em.HasComponent<GroundMissileFlyingRocketVisualComponent>(entity))
                        continue;

                    GroundMissileFlyingRocketVisualComponent visual =
                        em.GetComponentData<GroundMissileFlyingRocketVisualComponent>(entity);
                    if (visual.Launcher != launcher)
                        continue;

                    if (visual.OriginalParent != Entity.Null && !em.HasComponent<Parent>(entity))
                        ecb.AddComponent(entity, new Parent { Value = visual.OriginalParent });

                    ecb.SetComponent(
                        entity,
                        LocalTransform.FromPositionRotationScale(
                            visual.InitialLocalPosition,
                            visual.InitialLocalRotation,
                            0f));
                    ecb.RemoveComponent<GroundMissileFlyingRocketVisualComponent>(entity);
                }
            }
        }

        private static void EnqueueImpactVfx(
            EntityManager em,
            EntityCommandBuffer ecb,
            AirMissileImpactRequestComponent request)
        {
            if (!em.Exists(request.Source) || !em.HasComponent<AirMissileLauncherVfxReferenceComponent>(request.Source))
                return;

            AirMissileLauncherVfxReferenceComponent vfx = em.GetComponentData<AirMissileLauncherVfxReferenceComponent>(request.Source);
            CombatGameObjectVfxRequests.Enqueue(
                ecb,
                request.TargetKind == (byte)AirMissileTargetKind.IncomingGroundMissile
                    ? vfx.InterceptExplosionPrefab
                    : vfx.AirTargetImpactPrefab,
                request.Position,
                quaternion.identity,
                CombatGameObjectVfxRequestKind.Play,
                fallbackPrefab: vfx.AirburstExplosionPrefab);
        }
    }

    [UpdateAfter(typeof(AirMissileImpactSystem))]
    public partial struct AirMissileLauncherReloadVisualSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AirMissileLauncherStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;

            foreach (var (launcherState, entity) in SystemAPI
                         .Query<RefRO<AirMissileLauncherStateComponent>>()
                         .WithEntityAccess())
            {
                if (launcherState.ValueRO.Phase != (byte)AirMissileLauncherPhase.Idle ||
                    launcherState.ValueRO.SelectedMissileSlot < 0 ||
                    !em.HasBuffer<AirMissileLauncherMissileVisualComponent>(entity))
                {
                    continue;
                }

                DynamicBuffer<AirMissileLauncherMissileVisualComponent> missiles = em.GetBuffer<AirMissileLauncherMissileVisualComponent>(entity);
                for (int i = 0; i < missiles.Length; i++)
                {
                    AirMissileLauncherMissileVisualComponent missile = missiles[i];
                    if (missile.SlotIndex != launcherState.ValueRO.SelectedMissileSlot ||
                        missile.Missile == Entity.Null ||
                        !em.HasComponent<LocalTransform>(missile.Missile) ||
                        em.HasComponent<AirMissileProjectileComponent>(missile.Missile))
                    {
                        continue;
                    }

                    LocalTransform transform = em.GetComponentData<LocalTransform>(missile.Missile);
                    transform.Position = missile.InitialLocalPosition;
                    transform.Rotation = missile.InitialLocalRotation;
                    transform.Scale = math.max(0.0001f, missile.InitialLocalScale);
                    em.SetComponentData(missile.Missile, transform);
                    break;
                }
            }
        }
    }
}
