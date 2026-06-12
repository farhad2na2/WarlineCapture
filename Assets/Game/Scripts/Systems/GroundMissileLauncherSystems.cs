using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(UnitAttackSystem))]
public partial struct GroundMissileLauncherFireSystem : ISystem
{
    private const float MinimumProjectileDurationSeconds = 0.35f;
    private const float LaunchSmokeEmitSeconds = 1.1f;
    private const float LaunchSmokeActiveSeconds = 3f;
    private const float LaunchSmokeVerticalOffset = -0.45f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GroundMissileLauncherComponent>();
        state.RequireForUpdate<GroundMissileLauncherStateComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;
        ComponentLookup<LocalTransform> localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        ComponentLookup<LocalToWorld> localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (launcher, launcherState, selfTransform, faction, entity) in SystemAPI
                     .Query<RefRO<GroundMissileLauncherComponent>, RefRW<GroundMissileLauncherStateComponent>, RefRO<LocalTransform>, RefRO<Faction>>()
                     .WithNone<UnitDeathAnimationComponent>()
                     .WithEntityAccess())
        {
            ref GroundMissileLauncherStateComponent stateRw = ref launcherState.ValueRW;
            if (stateRw.Phase == (byte)GroundMissileLauncherPhase.Idle)
                continue;

            stateRw.Timer = math.max(0f, stateRw.Timer - dt);

            if (stateRw.Phase == (byte)GroundMissileLauncherPhase.Preparing)
            {
                if (stateRw.Timer > 0f)
                    continue;

                FireProjectile(
                    em,
                    ecb,
                    entity,
                    launcher.ValueRO,
                    stateRw,
                    selfTransform.ValueRO,
                    faction.ValueRO.Id,
                    localTransformLookup,
                    localToWorldLookup);
                LaunchRocketVisual(
                    em,
                    ecb,
                    entity,
                    launcher.ValueRO,
                    stateRw,
                    localToWorldLookup);

                stateRw.Phase = (byte)GroundMissileLauncherPhase.Launching;
                stateRw.Timer = GroundMissileLauncherTiming.PostLaunchHoldSeconds;
                continue;
            }

            if (stateRw.Phase == (byte)GroundMissileLauncherPhase.Launching)
            {
                if (stateRw.Timer > 0f)
                    continue;

                stateRw.Phase = (byte)GroundMissileLauncherPhase.Reloading;
                stateRw.Timer = math.max(0.01f, launcher.ValueRO.ReloadSeconds);
                continue;
            }

            if (stateRw.Phase == (byte)GroundMissileLauncherPhase.Reloading && stateRw.Timer <= 0f)
            {
                stateRw.Phase = (byte)GroundMissileLauncherPhase.Idle;
                stateRw.TargetEntity = Entity.Null;
                stateRw.TargetCell = default;
                stateRw.TargetWorldPosition = default;
                stateRw.Timer = 0f;
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static void FireProjectile(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity launcherEntity,
        GroundMissileLauncherComponent launcher,
        GroundMissileLauncherStateComponent launcherState,
        LocalTransform launcherTransform,
        byte factionId,
        ComponentLookup<LocalTransform> localTransformLookup,
        ComponentLookup<LocalToWorld> localToWorldLookup)
    {
        float3 sourcePosition = ResolveLaunchPosition(em, launcherEntity, launcherTransform.Position, localTransformLookup, localToWorldLookup);
        float3 targetPosition = launcherState.TargetWorldPosition;
        float distance = math.distance(new float2(sourcePosition.x, sourcePosition.z), new float2(targetPosition.x, targetPosition.z));
        float duration = math.max(MinimumProjectileDurationSeconds, distance / math.max(0.01f, launcher.RocketSpeed));
        float3 launchDirection = ResolveLaunchDirection(sourcePosition, targetPosition, launcher.BatteryElevatedAngleDegrees);
        quaternion launchRotation = quaternion.LookRotationSafe(launchDirection, math.up());

        if (em.HasComponent<GroundMissileLauncherVfxReferenceComponent>(launcherEntity))
        {
            GroundMissileLauncherVfxReferenceComponent vfx = em.GetComponentObject<GroundMissileLauncherVfxReferenceComponent>(launcherEntity);
            GameObject launchSmokePrefab = vfx?.LauncherBackfirePrefab != null
                ? vfx.LauncherBackfirePrefab
                : vfx?.RocketTrailPrefab;
            if (launchSmokePrefab != null)
            {
                float3 smokePosition = sourcePosition + math.up() * LaunchSmokeVerticalOffset;
                UnitAttackImpactVfxRuntime.PlayTimedLoop(
                    launchSmokePrefab,
                    smokePosition,
                    ToUnityQuaternion(quaternion.LookRotationSafe(-launchDirection, math.up())),
                    LaunchSmokeEmitSeconds,
                    LaunchSmokeActiveSeconds);
            }
        }

        Entity projectile = ecb.CreateEntity();
        ecb.AddComponent(projectile, LocalTransform.FromPositionRotation(sourcePosition, launchRotation));
        ecb.AddComponent(projectile, new GroundMissileProjectileComponent
        {
            Source = launcherEntity,
            TargetEntity = launcherState.TargetEntity,
            TargetCell = launcherState.TargetCell,
            StartPosition = sourcePosition,
            TargetPosition = targetPosition,
            ElapsedSeconds = 0f,
            DurationSeconds = duration,
            ArcHeight = launcher.ArcHeight,
            DamageRadius = launcher.DamageRadius,
            Damage = launcher.Damage,
            FactionId = factionId,
            Interceptable = 1
        });
        ecb.AddComponent(projectile, new MissileInterceptionTargetComponent
        {
            Source = launcherEntity,
            FactionId = factionId
        });

        var inFlight = new GroundMissileInFlightComponent
        {
            TargetEntity = launcherState.TargetEntity,
            TargetCell = launcherState.TargetCell,
            TargetWorldPosition = targetPosition
        };
        if (em.HasComponent<GroundMissileInFlightComponent>(launcherEntity))
            ecb.SetComponent(launcherEntity, inFlight);
        else
            ecb.AddComponent(launcherEntity, inFlight);

        if (em.HasComponent<EngageTarget>(launcherEntity))
            ecb.RemoveComponent<EngageTarget>(launcherEntity);
    }

    private static void LaunchRocketVisual(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity launcherEntity,
        GroundMissileLauncherComponent launcher,
        GroundMissileLauncherStateComponent launcherState,
        ComponentLookup<LocalToWorld> localToWorldLookup)
    {
        if (launcherState.SelectedRocketSlot < 0 ||
            !em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(launcherEntity))
        {
            return;
        }

        DynamicBuffer<GroundMissileLauncherRocketVisualComponent> rockets = em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(launcherEntity);
        for (int i = 0; i < rockets.Length; i++)
        {
            GroundMissileLauncherRocketVisualComponent rocket = rockets[i];
            if (rocket.SlotIndex != launcherState.SelectedRocketSlot ||
                rocket.Rocket == Entity.Null ||
                !em.HasComponent<LocalTransform>(rocket.Rocket) ||
                !localToWorldLookup.HasComponent(rocket.Rocket))
            {
                continue;
            }

            float3 startPosition = localToWorldLookup[rocket.Rocket].Position;
            float distance = math.distance(
                new float2(startPosition.x, startPosition.z),
                new float2(launcherState.TargetWorldPosition.x, launcherState.TargetWorldPosition.z));
            float duration = math.max(MinimumProjectileDurationSeconds, distance / math.max(0.01f, launcher.RocketSpeed));
            Entity originalParent = em.HasComponent<Parent>(rocket.Rocket)
                ? em.GetComponentData<Parent>(rocket.Rocket).Value
                : Entity.Null;
            float3 launchDirection = ResolveLaunchDirection(startPosition, launcherState.TargetWorldPosition, launcher.BatteryElevatedAngleDegrees);

            if (originalParent != Entity.Null)
                ecb.RemoveComponent<Parent>(rocket.Rocket);

            ecb.SetComponent(
                rocket.Rocket,
                LocalTransform.FromPositionRotationScale(
                    startPosition,
                    quaternion.LookRotationSafe(launchDirection, math.up()),
                    math.max(0.0001f, rocket.InitialLocalScale)));
            ecb.AddComponent(rocket.Rocket, new GroundMissileFlyingRocketVisualComponent
            {
                Launcher = launcherEntity,
                OriginalParent = originalParent,
                SlotIndex = rocket.SlotIndex,
                InitialLocalPosition = rocket.InitialLocalPosition,
                InitialLocalRotation = rocket.InitialLocalRotation,
                InitialLocalScale = rocket.InitialLocalScale,
                StartPosition = startPosition,
                TargetPosition = launcherState.TargetWorldPosition,
                LaunchDirection = launchDirection,
                ElapsedSeconds = 0f,
                DurationSeconds = duration,
                ArcHeight = launcher.ArcHeight
            });
            return;
        }
    }

    private static float3 ResolveLaunchPosition(
        EntityManager em,
        Entity launcherEntity,
        float3 fallback,
        ComponentLookup<LocalTransform> localTransformLookup,
        ComponentLookup<LocalToWorld> localToWorldLookup)
    {
        if (!em.HasComponent<GroundMissileLauncherVisualReferenceComponent>(launcherEntity))
            return fallback;

        GroundMissileLauncherVisualReferenceComponent visual = em.GetComponentData<GroundMissileLauncherVisualReferenceComponent>(launcherEntity);
        Entity launchEntity = visual.SmokeSpawn != Entity.Null ? visual.SmokeSpawn : visual.Battery;
        if (launchEntity != Entity.Null && localToWorldLookup.HasComponent(launchEntity))
            return localToWorldLookup[launchEntity].Position;
        if (launchEntity != Entity.Null && localTransformLookup.HasComponent(launchEntity))
            return localTransformLookup[launchEntity].Position;

        return fallback;
    }

    private static float3 ResolveLaunchDirection(float3 sourcePosition, float3 targetPosition, float batteryAngleDegrees)
    {
        float3 horizontal = targetPosition - sourcePosition;
        horizontal.y = 0f;
        horizontal = math.normalizesafe(horizontal, new float3(0f, 0f, 1f));

        float angleRadians = math.radians(math.clamp(math.abs(batteryAngleDegrees), 1f, 80f));
        return math.normalizesafe(
            horizontal * math.cos(angleRadians) + math.up() * math.sin(angleRadians),
            math.up());
    }

    private static Quaternion ToUnityQuaternion(quaternion rotation)
    {
        return new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
    }
}

[UpdateAfter(typeof(GroundMissileLauncherFireSystem))]
public partial struct GroundMissileLauncherVisualSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GroundMissileLauncherVisualReferenceComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;

        foreach (var (launcher, launcherState, visual, launcherTransform, entity) in SystemAPI
                     .Query<RefRO<GroundMissileLauncherComponent>, RefRO<GroundMissileLauncherStateComponent>, RefRO<GroundMissileLauncherVisualReferenceComponent>, RefRO<LocalTransform>>()
                     .WithEntityAccess())
        {
            ApplyBatteryRotation(em, launcher.ValueRO, launcherState.ValueRO, visual.ValueRO, launcherTransform.ValueRO);
            ApplyRocketSlotVisibility(em, launcherState.ValueRO, entity);
        }
    }

    private static void ApplyBatteryRotation(
        EntityManager em,
        GroundMissileLauncherComponent launcher,
        GroundMissileLauncherStateComponent launcherState,
        GroundMissileLauncherVisualReferenceComponent visual,
        LocalTransform launcherTransform)
    {
        if (visual.Battery == Entity.Null || !em.HasComponent<LocalTransform>(visual.Battery))
            return;

        float elevationFactor = ResolveElevationFactor(launcher, launcherState);
        quaternion yawRotation = ResolveBatteryYawRotation(launcherState, launcherTransform);
        quaternion elevatedRotation = math.mul(
            yawRotation,
            math.mul(
                visual.BatteryDefaultLocalRotation,
                quaternion.RotateX(math.radians(launcher.BatteryElevatedAngleDegrees))));

        LocalTransform transform = em.GetComponentData<LocalTransform>(visual.Battery);
        transform.Position = visual.BatteryDefaultLocalPosition;
        transform.Rotation = math.slerp(visual.BatteryDefaultLocalRotation, elevatedRotation, elevationFactor);
        em.SetComponentData(visual.Battery, transform);
    }

    private static quaternion ResolveBatteryYawRotation(
        GroundMissileLauncherStateComponent launcherState,
        LocalTransform launcherTransform)
    {
        if (launcherState.Phase != (byte)GroundMissileLauncherPhase.Preparing &&
            launcherState.Phase != (byte)GroundMissileLauncherPhase.Launching &&
            launcherState.Phase != (byte)GroundMissileLauncherPhase.Reloading)
        {
            return quaternion.identity;
        }

        float3 worldDirection = launcherState.TargetWorldPosition - launcherTransform.Position;
        worldDirection.y = 0f;
        if (math.lengthsq(worldDirection) < 1e-6f)
            return quaternion.identity;

        float3 localDirection = math.rotate(math.inverse(launcherTransform.Rotation), math.normalizesafe(worldDirection, new float3(0f, 0f, 1f)));
        float yawRadians = math.atan2(localDirection.x, localDirection.z);
        return quaternion.RotateY(yawRadians);
    }

    private static float ResolveElevationFactor(
        GroundMissileLauncherComponent launcher,
        GroundMissileLauncherStateComponent launcherState)
    {
        if (launcherState.Phase == (byte)GroundMissileLauncherPhase.Preparing)
        {
            float prepareSeconds = math.max(0.01f, launcher.PrepareSeconds);
            float openingTimer = math.max(0f, launcherState.Timer - GroundMissileLauncherTiming.PostOpenLaunchDelaySeconds);
            return math.saturate(1f - openingTimer / prepareSeconds);
        }

        if (launcherState.Phase == (byte)GroundMissileLauncherPhase.Launching)
            return 1f;

        if (launcherState.Phase == (byte)GroundMissileLauncherPhase.Reloading)
        {
            float reloadSeconds = math.max(0.01f, launcher.ReloadSeconds);
            float closeSeconds = math.min(math.max(0.01f, launcher.PrepareSeconds), reloadSeconds);
            float closeStartTimer = reloadSeconds;
            float closeEndTimer = reloadSeconds - closeSeconds;
            return math.saturate((launcherState.Timer - closeEndTimer) / (closeStartTimer - closeEndTimer));
        }

        return 0f;
    }

    private static void ApplyRocketSlotVisibility(
        EntityManager em,
        GroundMissileLauncherStateComponent launcherState,
        Entity launcherEntity)
    {
        if (!em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(launcherEntity))
            return;

        DynamicBuffer<GroundMissileLauncherRocketVisualComponent> rockets = em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(launcherEntity);
        bool hideSelected = launcherState.Phase == (byte)GroundMissileLauncherPhase.Launching ||
                            launcherState.Phase == (byte)GroundMissileLauncherPhase.Reloading;
        for (int i = 0; i < rockets.Length; i++)
        {
            GroundMissileLauncherRocketVisualComponent rocket = rockets[i];
            if (rocket.Rocket == Entity.Null || !em.HasComponent<LocalTransform>(rocket.Rocket))
                continue;
            if (em.HasComponent<GroundMissileFlyingRocketVisualComponent>(rocket.Rocket))
                continue;

            LocalTransform transform = em.GetComponentData<LocalTransform>(rocket.Rocket);
            transform.Position = rocket.InitialLocalPosition;
            transform.Rotation = rocket.InitialLocalRotation;
            transform.Scale = hideSelected && rocket.SlotIndex == launcherState.SelectedRocketSlot
                ? 0f
                : math.max(0.0001f, rocket.InitialLocalScale);
            em.SetComponentData(rocket.Rocket, transform);
        }
    }
}

[UpdateAfter(typeof(GroundMissileLauncherVisualSystem))]
public partial struct GroundMissileFlyingRocketVisualSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GroundMissileFlyingRocketVisualComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (flying, transform, entity) in SystemAPI
                     .Query<RefRW<GroundMissileFlyingRocketVisualComponent>, RefRW<LocalTransform>>()
                     .WithEntityAccess())
        {
            ref GroundMissileFlyingRocketVisualComponent flyingRw = ref flying.ValueRW;
            flyingRw.ElapsedSeconds += dt;
            float duration = math.max(0.01f, flyingRw.DurationSeconds);
            float t = math.saturate(flyingRw.ElapsedSeconds / duration);
            float3 position = EvaluateRocketArc(flyingRw, t);

            float nextT = math.saturate((flyingRw.ElapsedSeconds + math.min(0.05f, dt)) / duration);
            float3 nextPosition = EvaluateRocketArc(flyingRw, nextT);
            float3 direction = math.normalizesafe(nextPosition - position, new float3(0f, 0f, 1f));

            transform.ValueRW.Position = position;
            transform.ValueRW.Rotation = quaternion.LookRotationSafe(direction, math.up());
            transform.ValueRW.Scale = math.max(0.0001f, flyingRw.InitialLocalScale);

            if (flyingRw.ElapsedSeconds < duration)
                continue;

            float restoreScale = math.max(0.0001f, flyingRw.InitialLocalScale);
            if (em.HasComponent<GroundMissileLauncherStateComponent>(flyingRw.Launcher))
            {
                GroundMissileLauncherStateComponent launcherState = em.GetComponentData<GroundMissileLauncherStateComponent>(flyingRw.Launcher);
                if ((launcherState.Phase == (byte)GroundMissileLauncherPhase.Launching ||
                     launcherState.Phase == (byte)GroundMissileLauncherPhase.Reloading) &&
                    launcherState.SelectedRocketSlot == flyingRw.SlotIndex)
                {
                    restoreScale = 0f;
                }
            }

            if (flyingRw.OriginalParent != Entity.Null && !em.HasComponent<Parent>(entity))
                ecb.AddComponent(entity, new Parent { Value = flyingRw.OriginalParent });
            ecb.SetComponent(
                entity,
                LocalTransform.FromPositionRotationScale(
                    flyingRw.InitialLocalPosition,
                    flyingRw.InitialLocalRotation,
                    restoreScale));
            ecb.RemoveComponent<GroundMissileFlyingRocketVisualComponent>(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static float3 EvaluateRocketArc(GroundMissileFlyingRocketVisualComponent flying, float t)
    {
        float3 p0 = flying.StartPosition;
        float3 p3 = flying.TargetPosition;
        float3 horizontalDelta = p3 - p0;
        horizontalDelta.y = 0f;
        float3 horizontalDirection = math.normalizesafe(horizontalDelta, new float3(0f, 0f, 1f));
        float distance = math.distance(new float2(p0.x, p0.z), new float2(p3.x, p3.z));
        float launchControlDistance = math.clamp(distance * 0.35f, 8f, 90f);
        float apexHeight = math.max(
            math.max(0f, flying.ArcHeight),
            math.clamp(distance * 0.25f, 8f, 80f));
        float3 launchDirection = math.normalizesafe(flying.LaunchDirection, math.up());
        float3 p1 = p0 + launchDirection * launchControlDistance;
        float3 p2 = p0 + horizontalDirection * math.max(launchControlDistance, distance * 0.65f) + math.up() * apexHeight;

        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * oneMinusT * p0 +
               3f * oneMinusT * oneMinusT * t * p1 +
               3f * oneMinusT * t * t * p2 +
               t * t * t * p3;
    }
}

[UpdateAfter(typeof(GroundMissileFlyingRocketVisualSystem))]
public partial struct GroundMissileProjectileFlightSystem : ISystem
{
    private EntityQuery _projectileQuery;

    public void OnCreate(ref SystemState state)
    {
        _projectileQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<GroundMissileProjectileComponent>(),
            ComponentType.ReadWrite<LocalTransform>());
        state.RequireForUpdate(_projectileQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;

        state.EntityManager.CompleteDependencyBeforeRW<GroundMissileProjectileComponent>();
        state.EntityManager.CompleteDependencyBeforeRW<LocalTransform>();

        EntityTypeHandle entityType = state.GetEntityTypeHandle();
        ComponentTypeHandle<GroundMissileProjectileComponent> projectileType = state.GetComponentTypeHandle<GroundMissileProjectileComponent>(false);
        ComponentTypeHandle<LocalTransform> transformType = state.GetComponentTypeHandle<LocalTransform>(false);
        using NativeArray<ArchetypeChunk> chunks = _projectileQuery.ToArchetypeChunkArray(Allocator.Temp);
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<GroundMissileProjectileComponent> projectiles = chunk.GetNativeArray(ref projectileType);
            NativeArray<LocalTransform> transforms = chunk.GetNativeArray(ref transformType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                GroundMissileProjectileComponent projectile = projectiles[i];
                LocalTransform transform = transforms[i];

                projectile.ElapsedSeconds += dt;
                float duration = math.max(0.01f, projectile.DurationSeconds);
                float t = math.saturate(projectile.ElapsedSeconds / duration);
                float3 position = math.lerp(projectile.StartPosition, projectile.TargetPosition, t);
                position.y += math.sin(t * math.PI) * math.max(0f, projectile.ArcHeight);
                transform.Position = position;

                projectiles[i] = projectile;
                transforms[i] = transform;

                if (projectile.ElapsedSeconds < duration)
                    continue;

                ecb.AddComponent(entity, new GroundMissileImpactRequestComponent
                {
                    Source = projectile.Source,
                    TargetEntity = projectile.TargetEntity,
                    TargetCell = projectile.TargetCell,
                    Position = projectile.TargetPosition,
                    DamageRadius = projectile.DamageRadius,
                    Damage = projectile.Damage,
                    FactionId = projectile.FactionId
                });
                ecb.RemoveComponent<GroundMissileProjectileComponent>(entity);
                ecb.RemoveComponent<MissileInterceptionTargetComponent>(entity);
            }
        }

        ecb.Playback(em);
        ecb.Dispose();
    }
}

[UpdateAfter(typeof(GroundMissileProjectileFlightSystem))]
public partial struct GroundMissileImpactSystem : ISystem
{
    private const float DamageHealthBarVisibleSeconds = 2f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GroundMissileImpactRequestComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (impact, entity) in SystemAPI
                     .Query<RefRO<GroundMissileImpactRequestComponent>>()
                     .WithEntityAccess())
        {
            GroundMissileImpactRequestComponent request = impact.ValueRO;
            float radius = math.max(0f, request.DamageRadius);
            float radiusSq = radius * radius;
            int damage = math.max(0, request.Damage);
            ApplyDirectHitDamage(em, ecb, request);
            if (damage > 0)
            {
                foreach (var (health, transform, faction, target) in SystemAPI
                             .Query<RefRW<UnitHealth>, RefRO<LocalTransform>, RefRO<Faction>>()
                             .WithNone<StaticGridBlocker>()
                             .WithEntityAccess())
                {
                    if (target == request.TargetEntity ||
                        health.ValueRO.Current <= 0 ||
                        faction.ValueRO.Id == request.FactionId)
                    {
                        continue;
                    }

                    float3 delta = transform.ValueRO.Position - request.Position;
                    delta.y = 0f;
                    if (math.lengthsq(delta) > radiusSq)
                        continue;

                    health.ValueRW.Current = math.max(0, health.ValueRO.Current - damage);
                    ApplyRecentDamageState(em, ecb, target, request);
                }
            }

            PlayImpactVfx(em, request);
            if (request.Source != Entity.Null &&
                em.Exists(request.Source) &&
                em.HasComponent<GroundMissileInFlightComponent>(request.Source))
            {
                ecb.RemoveComponent<GroundMissileInFlightComponent>(request.Source);
            }
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static void ApplyDirectHitDamage(
        EntityManager em,
        EntityCommandBuffer ecb,
        GroundMissileImpactRequestComponent request)
    {
        Entity target = request.TargetEntity;
        if (target == Entity.Null ||
            !em.Exists(target) ||
            !em.HasComponent<UnitHealth>(target) ||
            !em.HasComponent<Faction>(target))
        {
            return;
        }

        if (em.GetComponentData<Faction>(target).Id == request.FactionId)
            return;

        UnitHealth health = em.GetComponentData<UnitHealth>(target);
        if (health.Current <= 0)
            return;

        int damage = math.max(0, request.Damage);
        if (damage <= 0)
            return;

        health.Current = math.max(0, health.Current - damage);
        em.SetComponentData(target, health);
        ApplyRecentDamageState(em, ecb, target, request);
    }

    private static void ApplyRecentDamageState(
        EntityManager em,
        EntityCommandBuffer ecb,
        Entity target,
        GroundMissileImpactRequestComponent request)
    {
        if (em.HasComponent<RecentAttacker>(target))
        {
            em.SetComponentData(target, new RecentAttacker
            {
                Attacker = request.Source,
                Cell = request.TargetCell,
                Position = request.Position
            });
        }
        else
        {
            ecb.AddComponent(target, new RecentAttacker
            {
                Attacker = request.Source,
                Cell = request.TargetCell,
                Position = request.Position
            });
        }

        if (em.HasComponent<RecentDamageHealthBarVisibility>(target))
        {
            em.SetComponentData(target, new RecentDamageHealthBarVisibility
            {
                TimeRemaining = DamageHealthBarVisibleSeconds
            });
        }
        else
        {
            ecb.AddComponent(target, new RecentDamageHealthBarVisibility
            {
                TimeRemaining = DamageHealthBarVisibleSeconds
            });
        }
    }

    private static void PlayImpactVfx(EntityManager em, GroundMissileImpactRequestComponent request)
    {
        if (!em.HasComponent<GroundMissileLauncherVfxReferenceComponent>(request.Source))
            return;

        GroundMissileLauncherVfxReferenceComponent vfx = em.GetComponentObject<GroundMissileLauncherVfxReferenceComponent>(request.Source);
        if (vfx?.ImpactExplosionPrefab != null)
            UnitAttackImpactVfxRuntime.Play(vfx.ImpactExplosionPrefab, request.Position);
        if (vfx?.ImpactSmokePrefab != null)
            UnitAttackImpactVfxRuntime.Play(vfx.ImpactSmokePrefab, request.Position);
    }
}
