using Unity.Burst;
using Unity.Burst.Intrinsics;
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
        float3 sourcePosition = ResolveLaunchPosition(
            em,
            launcherEntity,
            launcherState.SelectedRocketSlot,
            launcherTransform.Position,
            localTransformLookup,
            localToWorldLookup);
        float3 targetPosition = launcherState.TargetWorldPosition;
        float distance = math.distance(new float2(sourcePosition.x, sourcePosition.z), new float2(targetPosition.x, targetPosition.z));
        float duration = math.max(MinimumProjectileDurationSeconds, distance / math.max(0.01f, launcher.RocketSpeed));
        float3 launchDirection = ResolveLaunchDirection(sourcePosition, targetPosition, launcher.BatteryElevatedAngleDegrees);
        quaternion launchRotation = quaternion.LookRotationSafe(launchDirection, math.up());

        if (em.HasComponent<GroundMissileLauncherVfxReferenceComponent>(launcherEntity))
        {
            GroundMissileLauncherVfxReferenceComponent vfx = em.GetComponentData<GroundMissileLauncherVfxReferenceComponent>(launcherEntity);
            float3 smokePosition = sourcePosition + math.up() * LaunchSmokeVerticalOffset;
            CombatGameObjectVfxRequests.Enqueue(
                ecb,
                vfx.LauncherBackfirePrefab,
                smokePosition,
                quaternion.LookRotationSafe(-launchDirection, math.up()),
                CombatGameObjectVfxRequestKind.TimedLoop,
                LaunchSmokeEmitSeconds,
                LaunchSmokeActiveSeconds,
                vfx.RocketTrailPrefab);
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
        int selectedRocketSlot,
        float3 fallback,
        ComponentLookup<LocalTransform> localTransformLookup,
        ComponentLookup<LocalToWorld> localToWorldLookup)
    {
        if (selectedRocketSlot >= 0 && em.HasBuffer<GroundMissileLauncherRocketVisualComponent>(launcherEntity))
        {
            DynamicBuffer<GroundMissileLauncherRocketVisualComponent> rockets =
                em.GetBuffer<GroundMissileLauncherRocketVisualComponent>(launcherEntity);
            for (int i = 0; i < rockets.Length; i++)
            {
                GroundMissileLauncherRocketVisualComponent rocket = rockets[i];
                if (rocket.SlotIndex != selectedRocketSlot || rocket.Rocket == Entity.Null)
                    continue;

                if (localToWorldLookup.HasComponent(rocket.Rocket))
                    return localToWorldLookup[rocket.Rocket].Position;
                if (localTransformLookup.HasComponent(rocket.Rocket))
                    return localTransformLookup[rocket.Rocket].Position;
            }
        }

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
[BurstCompile]
public partial struct GroundMissileFlyingRocketVisualSystem : ISystem
{
    private ComponentLookup<GroundMissileLauncherStateComponent> _launcherStateLookup;
    private ComponentLookup<Parent> _parentLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _launcherStateLookup = state.GetComponentLookup<GroundMissileLauncherStateComponent>(true);
        _parentLookup = state.GetComponentLookup<Parent>(true);
        state.RequireForUpdate<GroundMissileFlyingRocketVisualComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        _launcherStateLookup.Update(ref state);
        _parentLookup.Update(ref state);

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        state.Dependency = new FlyingRocketVisualJob
        {
            DeltaTime = dt,
            LauncherStateLookup = _launcherStateLookup,
            ParentLookup = _parentLookup,
            Ecb = ecb.AsParallelWriter()
        }.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private partial struct FlyingRocketVisualJob : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<GroundMissileLauncherStateComponent> LauncherStateLookup;
        [ReadOnly] public ComponentLookup<Parent> ParentLookup;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(
            [EntityIndexInQuery] int entityIndexInQuery,
            Entity entity,
            ref GroundMissileFlyingRocketVisualComponent flying,
            ref LocalTransform transform)
        {
            flying.ElapsedSeconds += DeltaTime;
            float duration = math.max(0.01f, flying.DurationSeconds);
            float t = math.saturate(flying.ElapsedSeconds / duration);
            float3 position = EvaluateRocketArc(flying, t);

            float nextT = math.saturate((flying.ElapsedSeconds + math.min(0.05f, DeltaTime)) / duration);
            float3 nextPosition = EvaluateRocketArc(flying, nextT);
            float3 direction = math.normalizesafe(nextPosition - position, new float3(0f, 0f, 1f));

            transform.Position = position;
            transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
            transform.Scale = math.max(0.0001f, flying.InitialLocalScale);

            if (flying.ElapsedSeconds < duration)
                return;

            float restoreScale = math.max(0.0001f, flying.InitialLocalScale);
            if (LauncherStateLookup.HasComponent(flying.Launcher))
            {
                GroundMissileLauncherStateComponent launcherState = LauncherStateLookup[flying.Launcher];
                if ((launcherState.Phase == (byte)GroundMissileLauncherPhase.Launching ||
                     launcherState.Phase == (byte)GroundMissileLauncherPhase.Reloading) &&
                    launcherState.SelectedRocketSlot == flying.SlotIndex)
                {
                    restoreScale = 0f;
                }
            }

            if (flying.OriginalParent != Entity.Null && !ParentLookup.HasComponent(entity))
                Ecb.AddComponent(entityIndexInQuery, entity, new Parent { Value = flying.OriginalParent });
            Ecb.SetComponent(
                entityIndexInQuery,
                entity,
                LocalTransform.FromPositionRotationScale(
                    flying.InitialLocalPosition,
                    flying.InitialLocalRotation,
                    restoreScale));
            Ecb.RemoveComponent<GroundMissileFlyingRocketVisualComponent>(entityIndexInQuery, entity);
        }
    }

    private static float3 EvaluateRocketArc(GroundMissileFlyingRocketVisualComponent flying, float t)
    {
        float3 p0 = flying.StartPosition;
        float3 p1 = flying.TargetPosition;
        float3 position = math.lerp(p0, p1, math.saturate(t));
        position.y += math.sin(math.saturate(t) * math.PI) * math.max(0f, flying.ArcHeight);
        return position;
    }
}

[UpdateAfter(typeof(GroundMissileFlyingRocketVisualSystem))]
[UpdateBefore(typeof(GroundMissileProjectileFlightSystem))]
public partial struct GroundMissileRocketTrailSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GroundMissileFlyingRocketVisualComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, entity) in SystemAPI
                     .Query<RefRO<LocalTransform>>()
                     .WithAll<GroundMissileFlyingRocketVisualComponent>()
                     .WithEntityAccess())
        {
            float3 direction = math.rotate(transform.ValueRO.Rotation, new float3(0f, 0f, 1f));
            MissileTrailVfxView.Sync(entity, transform.ValueRO.Position, direction);
        }
    }
}

[UpdateAfter(typeof(GroundMissileRocketTrailSystem))]
[BurstCompile]
public partial struct GroundMissileProjectileFlightSystem : ISystem
{
    private EntityQuery _projectileQuery;
    private EntityTypeHandle _entityType;
    private ComponentTypeHandle<GroundMissileProjectileComponent> _projectileType;
    private ComponentTypeHandle<LocalTransform> _transformType;

    public void OnCreate(ref SystemState state)
    {
        _projectileQuery = state.GetEntityQuery(
            ComponentType.ReadWrite<GroundMissileProjectileComponent>(),
            ComponentType.ReadWrite<LocalTransform>());
        _entityType = state.GetEntityTypeHandle();
        _projectileType = state.GetComponentTypeHandle<GroundMissileProjectileComponent>(false);
        _transformType = state.GetComponentTypeHandle<LocalTransform>(false);
        state.RequireForUpdate(_projectileQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        _entityType.Update(ref state);
        _projectileType.Update(ref state);
        _transformType.Update(ref state);

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        state.Dependency = new ProjectileFlightJob
        {
            DeltaTime = dt,
            EntityType = _entityType,
            ProjectileType = _projectileType,
            TransformType = _transformType,
            Ecb = ecb.AsParallelWriter()
        }.ScheduleParallel(_projectileQuery, state.Dependency);
        state.Dependency.Complete();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    private struct ProjectileFlightJob : IJobChunk
    {
        public float DeltaTime;
        [ReadOnly] public EntityTypeHandle EntityType;
        public ComponentTypeHandle<GroundMissileProjectileComponent> ProjectileType;
        public ComponentTypeHandle<LocalTransform> TransformType;
        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(
            in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);
            NativeArray<GroundMissileProjectileComponent> projectiles = chunk.GetNativeArray(ref ProjectileType);
            NativeArray<LocalTransform> transforms = chunk.GetNativeArray(ref TransformType);
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                GroundMissileProjectileComponent projectile = projectiles[i];
                LocalTransform transform = transforms[i];

                projectile.ElapsedSeconds += DeltaTime;
                float duration = math.max(0.01f, projectile.DurationSeconds);
                float t = math.saturate(projectile.ElapsedSeconds / duration);
                float3 position = math.lerp(projectile.StartPosition, projectile.TargetPosition, t);
                position.y += math.sin(t * math.PI) * math.max(0f, projectile.ArcHeight);
                transform.Position = position;

                projectiles[i] = projectile;
                transforms[i] = transform;

                if (projectile.ElapsedSeconds < duration)
                    continue;

                Ecb.AddComponent(unfilteredChunkIndex, entity, new GroundMissileImpactRequestComponent
                {
                    Source = projectile.Source,
                    TargetEntity = projectile.TargetEntity,
                    TargetCell = projectile.TargetCell,
                    Position = projectile.TargetPosition,
                    DamageRadius = projectile.DamageRadius,
                    Damage = projectile.Damage,
                    FactionId = projectile.FactionId
                });
                Ecb.RemoveComponent<GroundMissileProjectileComponent>(unfilteredChunkIndex, entity);
                Ecb.RemoveComponent<MissileInterceptionTargetComponent>(unfilteredChunkIndex, entity);
            }
        }
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

            EnqueueImpactVfx(em, ecb, request);
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

    private static void EnqueueImpactVfx(
        EntityManager em,
        EntityCommandBuffer ecb,
        GroundMissileImpactRequestComponent request)
    {
        if (!em.HasComponent<GroundMissileLauncherVfxReferenceComponent>(request.Source))
            return;

        GroundMissileLauncherVfxReferenceComponent vfx = em.GetComponentData<GroundMissileLauncherVfxReferenceComponent>(request.Source);
        CombatGameObjectVfxRequests.Enqueue(
            ecb,
            vfx.ImpactExplosionPrefab,
            request.Position,
            quaternion.identity,
            CombatGameObjectVfxRequestKind.Play);
        CombatGameObjectVfxRequests.Enqueue(
            ecb,
            vfx.ImpactSmokePrefab,
            request.Position,
            quaternion.identity,
            CombatGameObjectVfxRequestKind.Play);
    }
}
