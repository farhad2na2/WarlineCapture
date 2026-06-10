using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(UnitAttackSystem))]
public partial struct GroundMissileLauncherFireSystem : ISystem
{
    private const float MinimumProjectileDurationSeconds = 0.35f;
    private const float TrailIntervalSeconds = 0.22f;

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

        if (em.HasComponent<GroundMissileLauncherVfxReferenceComponent>(launcherEntity))
        {
            GroundMissileLauncherVfxReferenceComponent vfx = em.GetComponentObject<GroundMissileLauncherVfxReferenceComponent>(launcherEntity);
            if (vfx?.LauncherBackfirePrefab != null)
                UnitAttackImpactVfxRuntime.Play(vfx.LauncherBackfirePrefab, sourcePosition);
        }

        Entity projectile = ecb.CreateEntity();
        ecb.AddComponent(projectile, LocalTransform.FromPosition(sourcePosition));
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
        ecb.AddComponent(projectile, new GroundMissileProjectileTrailComponent
        {
            TimeUntilNextTrail = 0f,
            TrailIntervalSeconds = TrailIntervalSeconds
        });
        ecb.AddComponent(projectile, new MissileInterceptionTargetComponent
        {
            Source = launcherEntity,
            FactionId = factionId
        });
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
            float3 launchDirection = launcherState.TargetWorldPosition - startPosition;
            launchDirection.y = math.max(0.15f, launchDirection.y);
            launchDirection = math.normalizesafe(launchDirection, new float3(0f, 0f, 1f));

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

        foreach (var (launcher, launcherState, visual, entity) in SystemAPI
                     .Query<RefRO<GroundMissileLauncherComponent>, RefRO<GroundMissileLauncherStateComponent>, RefRO<GroundMissileLauncherVisualReferenceComponent>>()
                     .WithEntityAccess())
        {
            ApplyBatteryRotation(em, launcher.ValueRO, launcherState.ValueRO, visual.ValueRO);
            ApplyRocketSlotVisibility(em, launcherState.ValueRO, entity);
        }
    }

    private static void ApplyBatteryRotation(
        EntityManager em,
        GroundMissileLauncherComponent launcher,
        GroundMissileLauncherStateComponent launcherState,
        GroundMissileLauncherVisualReferenceComponent visual)
    {
        if (visual.Battery == Entity.Null || !em.HasComponent<LocalTransform>(visual.Battery))
            return;

        float elevationFactor = ResolveElevationFactor(launcher, launcherState);
        quaternion elevatedRotation = math.mul(
            visual.BatteryDefaultLocalRotation,
            quaternion.RotateX(math.radians(launcher.BatteryElevatedAngleDegrees)));

        LocalTransform transform = em.GetComponentData<LocalTransform>(visual.Battery);
        transform.Position = visual.BatteryDefaultLocalPosition;
        transform.Rotation = math.slerp(visual.BatteryDefaultLocalRotation, elevatedRotation, elevationFactor);
        em.SetComponentData(visual.Battery, transform);
    }

    private static float ResolveElevationFactor(
        GroundMissileLauncherComponent launcher,
        GroundMissileLauncherStateComponent launcherState)
    {
        if (launcherState.Phase == (byte)GroundMissileLauncherPhase.Preparing)
        {
            float prepareSeconds = math.max(0.01f, launcher.PrepareSeconds);
            return math.saturate(1f - launcherState.Timer / prepareSeconds);
        }

        if (launcherState.Phase == (byte)GroundMissileLauncherPhase.Reloading)
        {
            float reloadSeconds = math.max(0.01f, launcher.ReloadSeconds);
            return math.saturate(launcherState.Timer / reloadSeconds);
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
        bool hideSelected = launcherState.Phase == (byte)GroundMissileLauncherPhase.Reloading;
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
            float3 position = math.lerp(flyingRw.StartPosition, flyingRw.TargetPosition, t);
            position.y += math.sin(t * math.PI) * math.max(0f, flyingRw.ArcHeight);

            float nextT = math.saturate((flyingRw.ElapsedSeconds + math.min(0.05f, dt)) / duration);
            float3 nextPosition = math.lerp(flyingRw.StartPosition, flyingRw.TargetPosition, nextT);
            nextPosition.y += math.sin(nextT * math.PI) * math.max(0f, flyingRw.ArcHeight);
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
                if (launcherState.Phase == (byte)GroundMissileLauncherPhase.Reloading &&
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
}

[UpdateAfter(typeof(GroundMissileFlyingRocketVisualSystem))]
public partial struct GroundMissileProjectileFlightSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GroundMissileProjectileComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityManager em = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (projectile, transform, trail, entity) in SystemAPI
                     .Query<RefRW<GroundMissileProjectileComponent>, RefRW<LocalTransform>, RefRW<GroundMissileProjectileTrailComponent>>()
                     .WithEntityAccess())
        {
            ref GroundMissileProjectileComponent projectileRw = ref projectile.ValueRW;
            projectileRw.ElapsedSeconds += dt;
            float duration = math.max(0.01f, projectileRw.DurationSeconds);
            float t = math.saturate(projectileRw.ElapsedSeconds / duration);
            float3 position = math.lerp(projectileRw.StartPosition, projectileRw.TargetPosition, t);
            position.y += math.sin(t * math.PI) * math.max(0f, projectileRw.ArcHeight);
            transform.ValueRW.Position = position;

            ref GroundMissileProjectileTrailComponent trailRw = ref trail.ValueRW;
            trailRw.TimeUntilNextTrail -= dt;
            if (trailRw.TimeUntilNextTrail <= 0f)
            {
                trailRw.TimeUntilNextTrail = math.max(0.05f, trailRw.TrailIntervalSeconds);
                if (em.HasComponent<GroundMissileLauncherVfxReferenceComponent>(projectileRw.Source))
                {
                    GroundMissileLauncherVfxReferenceComponent vfx = em.GetComponentObject<GroundMissileLauncherVfxReferenceComponent>(projectileRw.Source);
                    if (vfx?.RocketTrailPrefab != null)
                        UnitAttackImpactVfxRuntime.Play(vfx.RocketTrailPrefab, position);
                }
            }

            if (projectileRw.ElapsedSeconds < duration)
                continue;

            ecb.AddComponent(entity, new GroundMissileImpactRequestComponent
            {
                Source = projectileRw.Source,
                TargetEntity = projectileRw.TargetEntity,
                TargetCell = projectileRw.TargetCell,
                Position = projectileRw.TargetPosition,
                DamageRadius = projectileRw.DamageRadius,
                Damage = projectileRw.Damage,
                FactionId = projectileRw.FactionId
            });
            ecb.RemoveComponent<GroundMissileProjectileComponent>(entity);
            ecb.RemoveComponent<GroundMissileProjectileTrailComponent>(entity);
            ecb.RemoveComponent<MissileInterceptionTargetComponent>(entity);
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
            if (damage > 0)
            {
                foreach (var (health, transform, faction, target) in SystemAPI
                             .Query<RefRW<UnitHealth>, RefRO<LocalTransform>, RefRO<Faction>>()
                             .WithNone<StaticGridBlocker>()
                             .WithEntityAccess())
                {
                    if (health.ValueRO.Current <= 0 || faction.ValueRO.Id == request.FactionId)
                        continue;

                    float3 delta = transform.ValueRO.Position - request.Position;
                    delta.y = 0f;
                    if (math.lengthsq(delta) > radiusSq)
                        continue;

                    health.ValueRW.Current = math.max(0, health.ValueRO.Current - damage);
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
            }

            PlayImpactVfx(em, request);
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(em);
        ecb.Dispose();
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
