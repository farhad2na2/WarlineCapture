using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

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
    private const float TrailIntervalSeconds = 0.06f;
    private const float TrailEmitSeconds = 0.08f;
    private const float TrailActiveSeconds = 0.65f;

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
            AirMissileLauncherVfxReferenceComponent vfx = em.GetComponentObject<AirMissileLauncherVfxReferenceComponent>(launcherEntity);
            if (vfx?.LaunchSmokePrefab != null)
                UnitAttackImpactVfxRuntime.Play(vfx.LaunchSmokePrefab, start);
            if (vfx?.LaunchFlashPrefab != null)
                UnitAttackImpactVfxRuntime.Play(vfx.LaunchFlashPrefab, start);
            if (vfx?.MissileTrailPrefab != null)
            {
                UnitAttackImpactVfxRuntime.PlayTimedLoop(
                    vfx.MissileTrailPrefab,
                    start,
                    ToUnityQuaternion(quaternion.LookRotationSafe(-direction, math.up())),
                    TrailEmitSeconds,
                    TrailActiveSeconds);
                ecb.AddComponent(projectileEntity, new AirMissileProjectileTrailComponent
                {
                    TimeUntilNextTrail = TrailIntervalSeconds,
                    TrailIntervalSeconds = TrailIntervalSeconds
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

    private static Quaternion ToUnityQuaternion(quaternion rotation)
    {
        return new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
    }
}

[UpdateAfter(typeof(AirMissileLauncherFireControlSystem))]
[UpdateBefore(typeof(AirMissileHomingProjectileSystem))]
public partial struct AirMissileProjectileTrailSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AirMissileProjectileTrailComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (projectile, trail, transform) in SystemAPI
                     .Query<RefRO<AirMissileProjectileComponent>, RefRW<AirMissileProjectileTrailComponent>, RefRO<LocalTransform>>())
        {
            ref AirMissileProjectileTrailComponent trailRw = ref trail.ValueRW;
            trailRw.TimeUntilNextTrail -= dt;
            if (trailRw.TimeUntilNextTrail > 0f)
                continue;

            trailRw.TimeUntilNextTrail = math.max(0.02f, trailRw.TrailIntervalSeconds);
            if (!em.Exists(projectile.ValueRO.Source) ||
                !em.HasComponent<AirMissileLauncherVfxReferenceComponent>(projectile.ValueRO.Source))
            {
                continue;
            }

            AirMissileLauncherVfxReferenceComponent vfx = em.GetComponentObject<AirMissileLauncherVfxReferenceComponent>(projectile.ValueRO.Source);
            if (vfx?.MissileTrailPrefab == null)
                continue;

            float3 direction = math.normalizesafe(projectile.ValueRO.Velocity, math.rotate(transform.ValueRO.Rotation, new float3(0f, 0f, 1f)));
            UnitAttackImpactVfxRuntime.PlayTimedLoop(
                vfx.MissileTrailPrefab,
                transform.ValueRO.Position,
                ToUnityQuaternion(quaternion.LookRotationSafe(-direction, math.up())),
                0.08f,
                0.65f);
        }
    }

    private static Quaternion ToUnityQuaternion(quaternion rotation)
    {
        return new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
    }
}

[UpdateAfter(typeof(AirMissileLauncherFireControlSystem))]
[BurstCompile]
public partial struct AirMissileHomingProjectileSystem : ISystem
{
    private EntityQuery _airTargetQuery;
    private EntityQuery _incomingMissileTargetQuery;

    public void OnCreate(ref SystemState state)
    {
        _airTargetQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<UnitAirMovement>(),
            ComponentType.ReadOnly<LocalTransform>());
        _incomingMissileTargetQuery = state.GetEntityQuery(
            ComponentType.ReadOnly<MissileInterceptionTargetComponent>(),
            ComponentType.ReadOnly<LocalTransform>());
        state.RequireForUpdate<AirMissileProjectileComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        int targetCapacity = math.max(1, _airTargetQuery.CalculateEntityCount() + _incomingMissileTargetQuery.CalculateEntityCount());
        var targetPositions = new NativeParallelHashMap<Entity, float3>(targetCapacity, Allocator.TempJob);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        var targetCollectDependency = new CollectAirTargetPositionJob
        {
            TargetPositions = targetPositions.AsParallelWriter()
        }.ScheduleParallel(_airTargetQuery, state.Dependency);
        targetCollectDependency = new CollectIncomingMissileTargetPositionJob
        {
            TargetPositions = targetPositions.AsParallelWriter()
        }.ScheduleParallel(_incomingMissileTargetQuery, targetCollectDependency);

        state.Dependency = new HomingProjectileJob
        {
            DeltaTime = dt,
            TargetPositions = targetPositions,
            Ecb = ecb.AsParallelWriter()
        }.ScheduleParallel(targetCollectDependency);
        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        targetPositions.Dispose();
    }

    [BurstCompile]
    private partial struct CollectAirTargetPositionJob : IJobEntity
    {
        public NativeParallelHashMap<Entity, float3>.ParallelWriter TargetPositions;

        private void Execute(Entity entity, in UnitAirMovement airMovement, in LocalTransform transform)
        {
            TargetPositions.TryAdd(entity, transform.Position);
        }
    }

    [BurstCompile]
    private partial struct CollectIncomingMissileTargetPositionJob : IJobEntity
    {
        public NativeParallelHashMap<Entity, float3>.ParallelWriter TargetPositions;

        private void Execute(Entity entity, in MissileInterceptionTargetComponent interceptionTarget, in LocalTransform transform)
        {
            TargetPositions.TryAdd(entity, transform.Position);
        }
    }

    [BurstCompile]
    private partial struct HomingProjectileJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public NativeParallelHashMap<Entity, float3> TargetPositions;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(
            [EntityIndexInQuery] int entityIndexInQuery,
            Entity entity,
            ref AirMissileProjectileComponent projectile,
            ref LocalTransform transform)
        {
            projectile.ElapsedSeconds += DeltaTime;

            if (projectile.Target == entity ||
                !TargetPositions.TryGetValue(projectile.Target, out float3 targetPosition))
            {
                QueueImpact(Ecb, entityIndexInQuery, projectile, transform.Position, entity);
                return;
            }

            float3 desiredDirection = math.normalizesafe(targetPosition - transform.Position, math.normalizesafe(projectile.Velocity, new float3(0f, 0f, 1f)));
            float currentSpeed = math.length(projectile.Velocity);
            currentSpeed = math.max(0.01f, currentSpeed + projectile.Acceleration * DeltaTime);
            currentSpeed = math.min(currentSpeed, math.max(projectile.Speed, currentSpeed));
            float maxRadians = math.radians(math.max(1f, projectile.TurnRateDegreesPerSecond)) * DeltaTime;
            float3 currentDirection = math.normalizesafe(projectile.Velocity, desiredDirection);
            float3 newDirection = RotateTowards(currentDirection, desiredDirection, maxRadians);
            projectile.Velocity = newDirection * currentSpeed;

            float3 newPosition = transform.Position + projectile.Velocity * DeltaTime;
            transform.Position = newPosition;
            transform.Rotation = quaternion.LookRotationSafe(newDirection, math.up());

            float distance = math.distance(newPosition, targetPosition);
            if (distance <= math.max(0.1f, projectile.ProximityFuseRadius) ||
                projectile.ElapsedSeconds >= projectile.LifetimeSeconds)
            {
                QueueImpact(Ecb, entityIndexInQuery, projectile, newPosition, entity);
            }
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
            Entity projectileEntity)
        {
            ecb.AddComponent(sortKey, projectileEntity, new AirMissileImpactRequestComponent
            {
                Source = projectile.Source,
                Target = projectile.Target,
                TargetKind = projectile.TargetKind,
                FactionId = projectile.FactionId,
                Position = position,
                Damage = projectile.Damage
            });
            ecb.RemoveComponent<AirMissileProjectileComponent>(sortKey, projectileEntity);
        }
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
                    ecb.DestroyEntity(request.Target);
            }
            else if (em.Exists(request.Target) && em.HasComponent<UnitHealth>(request.Target))
            {
                UnitHealth health = em.GetComponentData<UnitHealth>(request.Target);
                health.Current = math.max(0, health.Current - math.max(0, request.Damage));
                ecb.SetComponent(request.Target, health);
            }

            PlayImpactVfx(em, request);
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

    private static void PlayImpactVfx(EntityManager em, AirMissileImpactRequestComponent request)
    {
        if (!em.Exists(request.Source) || !em.HasComponent<AirMissileLauncherVfxReferenceComponent>(request.Source))
            return;

        AirMissileLauncherVfxReferenceComponent vfx = em.GetComponentObject<AirMissileLauncherVfxReferenceComponent>(request.Source);
        GameObject prefab = request.TargetKind == (byte)AirMissileTargetKind.IncomingGroundMissile
            ? vfx?.InterceptExplosionPrefab
            : vfx?.AirTargetImpactPrefab;
        if (prefab == null)
            prefab = vfx?.AirburstExplosionPrefab;
        if (prefab != null)
            UnitAttackImpactVfxRuntime.Play(prefab, request.Position);
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
