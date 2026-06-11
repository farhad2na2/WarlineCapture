using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public sealed class GroundMissileLauncherRuntimeTests
{
    public static void RunProjectileDependencyValidation()
    {
        try
        {
            var tests = new GroundMissileLauncherRuntimeTests();
            tests.MissileProjectileFlight_CompletesVehicleSlopeAlignmentTransformDependency();
            Debug.Log("[GroundMissileProjectileDependencyValidation] result=Passed tests=1");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("[GroundMissileProjectileDependencyValidation] result=Failed");
            EditorApplication.Exit(1);
        }
    }

    [Test]
    public void AttackSystem_ArmsGroundMissileLauncherWithoutImmediateDamage()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_Attack");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        Entity target = CreateTarget(em, new float3(120f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        em.AddComponentData(launcher, new EngageTarget
        {
            Target = target,
            Cell = new int2(120, 0),
            Position = new float3(120f, 0f, 0f),
            IsCommanded = 1
        });

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(world.Unmanaged);

        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(target).Current);
        GroundMissileLauncherStateComponent launcherState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
        Assert.AreEqual((byte)GroundMissileLauncherPhase.Preparing, launcherState.Phase);
        Assert.AreEqual(target, launcherState.TargetEntity);
        Assert.AreEqual(1.5f, launcherState.Timer, 0.001f);
        Assert.AreEqual(0f, em.GetComponentData<UnitAttackTraceComponent>(launcher).TimeRemaining, 0.001f);
    }

    [Test]
    public void MissileFire_WaitsForBatteryOpenAndHoldDelayBeforeLaunch()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_DelayedLaunch");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        Entity target = CreateTarget(em, new float3(120f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        em.AddComponentData(launcher, new EngageTarget
        {
            Target = target,
            Cell = new int2(120, 0),
            Position = new float3(120f, 0f, 0f),
            IsCommanded = 1
        });

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        SystemHandle fireSystem = world.CreateSystem<GroundMissileLauncherFireSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(world.Unmanaged);
        world.SetTime(new TimeData(0.6d, 0.5f));
        fireSystem.Update(world.Unmanaged);

        EntityQuery projectileQuery = em.CreateEntityQuery(typeof(GroundMissileProjectileComponent));
        Assert.AreEqual(0, projectileQuery.CalculateEntityCount(), "Launcher should not fire immediately when the battery has only just opened.");

        world.SetTime(new TimeData(1.6d, 1f));
        fireSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, projectileQuery.CalculateEntityCount(), "Launcher should fire after the one-second post-open hold.");
        Assert.IsFalse(em.HasComponent<EngageTarget>(launcher), "Missile launch should consume the one-shot attack order instead of re-arming after reload.");
        Assert.IsTrue(em.HasComponent<GroundMissileInFlightComponent>(launcher), "Launcher should expose an in-flight missile state for HUD order display.");

        world.SetTime(new TimeData(2.7d, 1.1f));
        fireSystem.Update(world.Unmanaged);
        world.SetTime(new TimeData(5.8d, 3.1f));
        fireSystem.Update(world.Unmanaged);
        attackSystem.Update(world.Unmanaged);

        Assert.AreEqual(1, projectileQuery.CalculateEntityCount(), "Launcher must not create a second projectile for the same clicked target after reload.");

        em.AddComponentData(launcher, new EngageTarget
        {
            Target = target,
            Cell = new int2(120, 0),
            Position = new float3(120f, 0f, 0f),
            IsCommanded = 0
        });
        attackSystem.Update(world.Unmanaged);
        Assert.AreEqual(1, projectileQuery.CalculateEntityCount(), "Launcher must not auto-fire again while the previous missile is still in flight.");
    }

    [Test]
    public void MissileVisual_YawsBatteryTowardTargetWhileOpen()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_BatteryYaw");
        EntityManager em = world.EntityManager;

        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        Entity battery = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(battery, LocalTransform.Identity);
        em.AddComponentData(launcher, new GroundMissileLauncherVisualReferenceComponent
        {
            Battery = battery,
            SmokeSpawn = Entity.Null,
            BatteryDefaultLocalPosition = float3.zero,
            BatteryDefaultLocalRotation = quaternion.identity
        });
        em.SetComponentData(launcher, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Preparing,
            TargetEntity = Entity.Null,
            TargetCell = new int2(10, 0),
            TargetWorldPosition = new float3(10f, 0f, 0f),
            Timer = GroundMissileLauncherTiming.PostOpenLaunchDelaySeconds,
            SelectedRocketSlot = -1
        });

        SystemHandle visualSystem = world.CreateSystem<GroundMissileLauncherVisualSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        visualSystem.Update(world.Unmanaged);

        LocalTransform batteryTransform = em.GetComponentData<LocalTransform>(battery);
        float3 forward = math.rotate(batteryTransform.Rotation, new float3(0f, 0f, 1f));
        Assert.Greater(forward.x, 0.7f, "Battery should yaw toward the target direction.");
        Assert.Less(math.abs(forward.z), 0.55f, "Battery should no longer aim mostly along its default forward axis.");
    }

    [Test]
    public void MissileFire_HoldsBatteryOpenAfterLaunchBeforeReloading()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_PostLaunchHold");
        EntityManager em = world.EntityManager;

        Entity target = CreateTarget(em, new float3(10f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.01f, reloadSeconds: 3f);
        Entity battery = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(battery, LocalTransform.Identity);
        em.AddComponentData(launcher, new GroundMissileLauncherVisualReferenceComponent
        {
            Battery = battery,
            SmokeSpawn = Entity.Null,
            BatteryDefaultLocalPosition = float3.zero,
            BatteryDefaultLocalRotation = quaternion.identity
        });
        em.SetComponentData(launcher, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Preparing,
            TargetEntity = target,
            TargetCell = new int2(10, 0),
            TargetWorldPosition = new float3(10f, 0f, 0f),
            Timer = 0f,
            SelectedRocketSlot = -1
        });

        SystemHandle fireSystem = world.CreateSystem<GroundMissileLauncherFireSystem>();
        SystemHandle visualSystem = world.CreateSystem<GroundMissileLauncherVisualSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        fireSystem.Update(world.Unmanaged);
        visualSystem.Update(world.Unmanaged);

        GroundMissileLauncherStateComponent launchState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
        Assert.AreEqual((byte)GroundMissileLauncherPhase.Launching, launchState.Phase);
        Assert.AreEqual(GroundMissileLauncherTiming.PostLaunchHoldSeconds, launchState.Timer, 0.001f);
        AssertBatteryStillElevated(em.GetComponentData<LocalTransform>(battery).Rotation);

        world.SetTime(new TimeData(0.6d, 0.5f));
        fireSystem.Update(world.Unmanaged);
        visualSystem.Update(world.Unmanaged);

        launchState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
        Assert.AreEqual((byte)GroundMissileLauncherPhase.Launching, launchState.Phase);
        AssertBatteryStillElevated(em.GetComponentData<LocalTransform>(battery).Rotation);

        world.SetTime(new TimeData(1.2d, 0.6f));
        fireSystem.Update(world.Unmanaged);

        launchState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
        Assert.AreEqual((byte)GroundMissileLauncherPhase.Reloading, launchState.Phase);
    }

    [Test]
    public void MissileVisual_ClosesBatteryAtPrepareSpeedDuringReload()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_CloseSpeed");
        EntityManager em = world.EntityManager;

        float prepareSeconds = 0.5f;
        float reloadSeconds = 3f;
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds, reloadSeconds);
        Entity battery = em.CreateEntity(typeof(LocalTransform));
        em.SetComponentData(battery, LocalTransform.Identity);
        em.AddComponentData(launcher, new GroundMissileLauncherVisualReferenceComponent
        {
            Battery = battery,
            SmokeSpawn = Entity.Null,
            BatteryDefaultLocalPosition = float3.zero,
            BatteryDefaultLocalRotation = quaternion.identity
        });
        em.SetComponentData(launcher, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Reloading,
            TargetEntity = Entity.Null,
            TargetCell = new int2(10, 0),
            TargetWorldPosition = new float3(10f, 0f, 0f),
            Timer = reloadSeconds,
            SelectedRocketSlot = -1
        });

        SystemHandle visualSystem = world.CreateSystem<GroundMissileLauncherVisualSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        visualSystem.Update(world.Unmanaged);
        AssertBatteryStillElevated(em.GetComponentData<LocalTransform>(battery).Rotation);

        GroundMissileLauncherStateComponent launcherState = em.GetComponentData<GroundMissileLauncherStateComponent>(launcher);
        launcherState.Timer = reloadSeconds - prepareSeconds;
        em.SetComponentData(launcher, launcherState);

        world.SetTime(new TimeData(0.6d, 0.5f));
        visualSystem.Update(world.Unmanaged);

        float3 forward = math.rotate(em.GetComponentData<LocalTransform>(battery).Rotation, new float3(0f, 0f, 1f));
        Assert.Less(math.abs(forward.y), 0.08f, "Battery should finish closing after one prepare-duration, not over the full reload duration.");
    }

    [Test]
    public void MissileProjectile_ImpactsAndDamagesEnemyArea()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_Impact");
        EntityManager em = world.EntityManager;

        Entity target = CreateTarget(em, new float3(10f, 0f, 0f), health: 100);
        Entity friendly = CreateFriendly(em, new float3(10f, 0f, 1f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.01f, reloadSeconds: 3f);
        em.SetComponentData(launcher, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Preparing,
            TargetEntity = target,
            TargetCell = new int2(10, 0),
            TargetWorldPosition = new float3(10f, 0f, 0f),
            Timer = 0f,
            SelectedRocketSlot = -1
        });

        SystemHandle fireSystem = world.CreateSystem<GroundMissileLauncherFireSystem>();
        SystemHandle flightSystem = world.CreateSystem<GroundMissileProjectileFlightSystem>();
        SystemHandle impactSystem = world.CreateSystem<GroundMissileImpactSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        fireSystem.Update(world.Unmanaged);

        EntityQuery projectileQuery = em.CreateEntityQuery(typeof(GroundMissileProjectileComponent));
        Assert.AreEqual(1, projectileQuery.CalculateEntityCount());
        Assert.IsTrue(em.HasComponent<GroundMissileInFlightComponent>(launcher));

        world.SetTime(new TimeData(1d, 1f));
        flightSystem.Update(world.Unmanaged);
        impactSystem.Update(world.Unmanaged);

        Assert.AreEqual(0, em.GetComponentData<UnitHealth>(target).Current);
        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(friendly).Current);
        Assert.IsFalse(em.HasComponent<GroundMissileInFlightComponent>(launcher));
    }

    [Test]
    public void MissileProjectileFlight_CompletesVehicleSlopeAlignmentTransformDependency()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_ProjectileDependency");
        EntityManager em = world.EntityManager;

        Entity vehicle = em.CreateEntity(
            typeof(LocalTransform),
            typeof(VehicleSurfaceAlignmentComponent),
            typeof(UnitSurfaceComponent),
            typeof(UnitFootprint),
            typeof(UnitMovementBehavior));
        em.SetComponentData(vehicle, LocalTransform.Identity);
        em.SetComponentData(vehicle, new UnitSurfaceComponent
        {
            LastSampledNormal = math.normalize(new float3(0.15f, 1f, 0.05f)),
            HasSurface = 1,
            IsGrounded = 1
        });
        em.SetComponentData(vehicle, new UnitFootprint { Size = new int2(2, 2) });
        em.SetComponentData(vehicle, new UnitMovementBehavior { UsesVehicleMotion = 1 });

        Entity projectile = em.CreateEntity(
            typeof(GroundMissileProjectileComponent),
            typeof(LocalTransform),
            typeof(MissileInterceptionTargetComponent));
        em.SetComponentData(projectile, LocalTransform.FromPosition(float3.zero));
        em.SetComponentData(projectile, new GroundMissileProjectileComponent
        {
            Source = Entity.Null,
            TargetEntity = Entity.Null,
            TargetCell = new int2(5, 0),
            StartPosition = float3.zero,
            TargetPosition = new float3(10f, 0f, 0f),
            ElapsedSeconds = 0f,
            DurationSeconds = 1f,
            ArcHeight = 0f,
            DamageRadius = 0f,
            Damage = 0,
            FactionId = FactionIdentitySystem.PlayerFactionId
        });

        SystemHandle slopeSystem = world.CreateSystem<VehicleSlopeAlignmentSystem>();
        SystemHandle flightSystem = world.CreateSystem<GroundMissileProjectileFlightSystem>();

        world.SetTime(new TimeData(0.25d, 0.25f));
        slopeSystem.Update(world.Unmanaged);

        Assert.DoesNotThrow(
            () => flightSystem.Update(world.Unmanaged),
            "Projectile flight must complete LocalTransform dependencies before main-thread lookup access.");
        em.CompleteAllTrackedJobs();

        LocalTransform projectileTransform = em.GetComponentData<LocalTransform>(projectile);
        Assert.Greater(projectileTransform.Position.x, 0f);
    }

    [Test]
    public void EngagementSystem_DoesNotAutoAcquireGroundMissileLauncherTargets()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_NoAutoEngage");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        CreateTarget(em, new float3(20f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.01f, reloadSeconds: 3f);

        var endSimulation = world.CreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        SystemHandle engagementSystem = world.CreateSystem<UnitEngagementSystem>();

        world.SetTime(new TimeData(0.2d, 0.2f));
        engagementSystem.Update(world.Unmanaged);
        em.CompleteAllTrackedJobs();
        endSimulation.Update();

        Assert.IsFalse(em.HasComponent<EngageTarget>(launcher), "Ground missile launchers should fire only from explicit attack/debug orders, not automatic target acquisition.");
    }

    [Test]
    public void MissileFire_DetachesAndRestoresSelectedRocketVisual()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_RocketVisual");
        EntityManager em = world.EntityManager;

        Entity target = CreateTarget(em, new float3(10f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.01f, reloadSeconds: 3f);
        Entity rocketParent = em.CreateEntity(typeof(LocalTransform));
        Entity rocket = em.CreateEntity(typeof(LocalTransform), typeof(LocalToWorld), typeof(Parent));
        em.SetComponentData(rocketParent, LocalTransform.Identity);
        em.SetComponentData(rocket, LocalTransform.FromPosition(new float3(1f, 0f, 0f)));
        em.SetComponentData(rocket, new LocalToWorld { Value = float4x4.Translate(new float3(1f, 0f, 0f)) });
        em.SetComponentData(rocket, new Parent { Value = rocketParent });
        DynamicBuffer<GroundMissileLauncherRocketVisualComponent> rockets =
            em.AddBuffer<GroundMissileLauncherRocketVisualComponent>(launcher);
        rockets.Add(new GroundMissileLauncherRocketVisualComponent
        {
            Rocket = rocket,
            SlotIndex = 0,
            InitialLocalPosition = new float3(1f, 0f, 0f),
            InitialLocalRotation = quaternion.identity,
            InitialLocalScale = 1f
        });
        em.SetComponentData(launcher, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Preparing,
            TargetEntity = target,
            TargetCell = new int2(10, 0),
            TargetWorldPosition = new float3(10f, 0f, 0f),
            Timer = 0f,
            SelectedRocketSlot = 0
        });

        SystemHandle fireSystem = world.CreateSystem<GroundMissileLauncherFireSystem>();
        SystemHandle rocketVisualSystem = world.CreateSystem<GroundMissileFlyingRocketVisualSystem>();

        world.SetTime(new TimeData(0.1d, 0.1f));
        fireSystem.Update(world.Unmanaged);

        Assert.IsTrue(em.HasComponent<GroundMissileFlyingRocketVisualComponent>(rocket));
        Assert.IsFalse(em.HasComponent<Parent>(rocket));
        GroundMissileFlyingRocketVisualComponent flying = em.GetComponentData<GroundMissileFlyingRocketVisualComponent>(rocket);
        Assert.Greater(flying.LaunchDirection.y, 0.45f, "The visible rocket should launch upward at the battery elevation angle.");
        Assert.IsFalse(
            em.HasComponent<GroundMissileProjectileTrailComponent>(em.CreateEntityQuery(typeof(GroundMissileProjectileComponent)).GetSingletonEntity()),
            "Missile projectile should not spawn repeated trail smoke components.");

        world.SetTime(new TimeData(0.12d, 0.02f));
        rocketVisualSystem.Update(world.Unmanaged);
        Assert.Greater(
            em.GetComponentData<LocalTransform>(rocket).Position.y,
            0.15f,
            "The visible rocket should climb immediately after launch instead of flying flat.");

        world.SetTime(new TimeData(1d, 1f));
        rocketVisualSystem.Update(world.Unmanaged);

        Assert.IsFalse(em.HasComponent<GroundMissileFlyingRocketVisualComponent>(rocket));
        Assert.IsTrue(em.HasComponent<Parent>(rocket));
        Assert.AreEqual(rocketParent, em.GetComponentData<Parent>(rocket).Value);
    }


    [Test]
    public void AttackOrder_RejectsGroundMissileTargetInsideMinimumRangeWithMessage()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_TooClose");
        EntityManager em = world.EntityManager;

        Entity target = CreateTarget(em, new float3(2f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        var selected = new NativeArray<Entity>(1, Allocator.Temp);
        selected[0] = launcher;

        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            result = new UnitTargetOrderSystem().IssueAttackTarget(em, selected, target);
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsFalse(result.CommandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, result.CommandResult.ReasonCode);
        Assert.AreEqual("Target too close for missile launcher.", result.CommandResult.Message);
        Assert.IsFalse(em.HasComponent<EngageTarget>(launcher));
    }

    [Test]
    public void AttackOrder_RejectsGroundMissileTargetOutsideMaximumRangeWithMessage()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_OutOfRange");
        EntityManager em = world.EntityManager;

        Entity target = CreateTarget(em, new float3(700f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        var selected = new NativeArray<Entity>(1, Allocator.Temp);
        selected[0] = launcher;

        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            result = new UnitTargetOrderSystem().IssueAttackTarget(em, selected, target);
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsFalse(result.CommandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, result.CommandResult.ReasonCode);
        Assert.AreEqual("Target out of missile range.", result.CommandResult.Message);
        Assert.IsFalse(em.HasComponent<EngageTarget>(launcher));
    }

    [Test]
    public void AttackOrder_GroundMissileLauncherAcceptsHostileRuntimeBuildingAtLongRange()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_LongRangeBuilding");
        EntityManager em = world.EntityManager;

        Entity targetBuilding = CreateRuntimeBuildingTarget(
            em,
            originCell: new int2(120, 8),
            footprintCells: new int2(4, 4),
            position: new float3(122f, 0f, 10f),
            factionId: FactionIdentitySystem.EnemyFactionId,
            health: 250);
        Entity breachTarget = CreateRuntimeBuildingTarget(
            em,
            originCell: new int2(20, 8),
            footprintCells: new int2(2, 2),
            position: new float3(21f, 0f, 9f),
            factionId: FactionIdentitySystem.EnemyFactionId,
            health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        var selected = new NativeArray<Entity>(1, Allocator.Temp);
        selected[0] = launcher;

        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            result = new UnitTargetOrderSystem().IssueAttackTarget(
                em,
                selected,
                targetBuilding,
                (
                    byte _,
                    Entity __,
                    int2 ___,
                    int2 ____,
                    out Entity breach,
                    out int2 breachCell,
                    out float3 breachPosition) =>
                {
                    breach = breachTarget;
                    breachCell = new int2(20, 8);
                    breachPosition = new float3(21f, 0f, 9f);
                    return true;
                });
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsTrue(result.CommandResult.Accepted);
        Assert.AreEqual("Missile launched.", result.CommandResult.Message);
        Assert.AreEqual(1, result.IssuedCount);
        Assert.IsTrue(em.HasComponent<EngageTarget>(launcher));
        EngageTarget engage = em.GetComponentData<EngageTarget>(launcher);
        Assert.AreEqual(targetBuilding, engage.Target);
        Assert.AreEqual(1, engage.IsCommanded);
        Assert.IsFalse(em.HasComponent<BaseBreachOrder>(launcher), "Missile launchers should fire at hostile buildings directly instead of receiving breach movement orders.");
        Assert.IsFalse(em.HasComponent<UnitPathRequest>(launcher));
    }

    [Test]
    public void AttackOrder_GroundMissileLauncherRejectsHostileRuntimeBuildingInsideMinimumRange()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_CloseBuilding");
        EntityManager em = world.EntityManager;

        Entity targetBuilding = CreateRuntimeBuildingTarget(
            em,
            originCell: new int2(2, 0),
            footprintCells: new int2(2, 2),
            position: new float3(2f, 0f, 0f),
            factionId: FactionIdentitySystem.EnemyFactionId,
            health: 250);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        var selected = new NativeArray<Entity>(1, Allocator.Temp);
        selected[0] = launcher;

        UnitTargetOrderSystem.AttackOrderIssueResult result;
        try
        {
            result = new UnitTargetOrderSystem().IssueAttackTarget(em, selected, targetBuilding);
        }
        finally
        {
            selected.Dispose();
        }

        Assert.IsFalse(result.CommandResult.Accepted);
        Assert.AreEqual(TacticalCommandReasonCode.TargetNotAttackable, result.CommandResult.ReasonCode);
        Assert.AreEqual("Target too close for missile launcher.", result.CommandResult.Message);
        Assert.IsFalse(em.HasComponent<EngageTarget>(launcher));
    }

    [Test]
    public void SelectionAttackRequest_PreservesGroundMissileRangeFeedbackMessage()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_RequestMessage");
        EntityManager em = world.EntityManager;

        Entity target = CreateTarget(em, new float3(2f, 0f, 0f), health: 100);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);
        em.AddComponent<SelectedUnitTag>(launcher);

        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputRequestQueueComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 7,
            ExplicitAttackTargetMode = 1,
            HasScreenPosition = 1,
            ScreenPosition = new float2(10f, 20f)
        });

        var requestSystem = new SelectionAttackCommandRequestSystem();
        requestSystem.ProcessPendingRequests(
            em,
            commandEntity,
            requests,
            results,
            new AttackOrderCommandSystem(),
            new UnitTargetOrderSystem(),
            (Vector2 screenPosition, EntityManager entityManager, out Entity clicked) =>
            {
                clicked = target;
                return true;
            },
            null,
            null,
            default);

        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(0, results[0].Accepted);
        Assert.AreEqual((int)TacticalCommandReasonCode.TargetNotAttackable, results[0].ReasonCode);
        Assert.AreEqual("Target too close for missile launcher.", results[0].Message.ToString());
    }

    [Test]
    public void SelectionAttackRequest_UsesFocusedAttackSourceWhenSelectedTagMissing()
    {
        using var world = new World("GroundMissileLauncherRuntimeTests_FocusedAttackSource");
        EntityManager em = world.EntityManager;

        Entity targetBuilding = CreateRuntimeBuildingTarget(
            em,
            originCell: new int2(120, 8),
            footprintCells: new int2(4, 4),
            position: new float3(122f, 0f, 10f),
            factionId: FactionIdentitySystem.EnemyFactionId,
            health: 250);
        Entity launcher = CreateLauncher(em, new float3(0f, 0f, 0f), prepareSeconds: 0.5f, reloadSeconds: 3f);

        Entity commandEntity = em.CreateEntity(typeof(RtsSelectionInputRequestQueueComponent));
        em.AddBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        em.AddBuffer<RtsSelectionCommandResultElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandIntentRequestElement> requests = em.GetBuffer<RtsSelectionCommandIntentRequestElement>(commandEntity);
        DynamicBuffer<RtsSelectionCommandResultElement> results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        requests.Add(new RtsSelectionCommandIntentRequestElement
        {
            Kind = RtsSelectionCommandIntentKind.Attack,
            RequestId = 8,
            ExplicitAttackTargetMode = 1,
            HasScreenPosition = 1,
            ScreenPosition = new float2(10f, 20f)
        });

        var requestSystem = new SelectionAttackCommandRequestSystem();
        requestSystem.ProcessPendingRequests(
            em,
            commandEntity,
            requests,
            results,
            new AttackOrderCommandSystem(),
            new UnitTargetOrderSystem(),
            (Vector2 screenPosition, EntityManager entityManager, out Entity clicked) =>
            {
                clicked = targetBuilding;
                return true;
            },
            (EntityManager entityManager, System.Collections.Generic.List<Entity> sources) => sources.Add(launcher),
            null,
            default);

        results = em.GetBuffer<RtsSelectionCommandResultElement>(commandEntity);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted);
        Assert.AreEqual(0, results[0].ReasonCode);
        Assert.IsTrue(em.HasComponent<EngageTarget>(launcher));
        Assert.AreEqual(targetBuilding, em.GetComponentData<EngageTarget>(launcher).Target);
    }


    private static void CreateGrid(EntityManager em)
    {
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 256,
            Height = 256,
            CellSize = 1f,
            Origin = float3.zero
        });
    }

    private static Entity CreateLauncher(EntityManager em, float3 position, float prepareSeconds, float reloadSeconds)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitMove),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitAttackTraceComponent),
            typeof(UnitAttackAnimationComponent),
            typeof(GroundMissileLauncherComponent),
            typeof(GroundMissileLauncherStateComponent),
            typeof(LocalTransform));

        em.SetComponentData(entity, new Faction { Id = FactionIdentitySystem.PlayerFactionId });
        em.SetComponentData(entity, new UnitGrid { Cell = GridUtils.WorldToCell(new GridConfig { Width = 256, Height = 256, CellSize = 1f, Origin = float3.zero }, position) });
        em.SetComponentData(entity, new UnitMove
        {
            Speed = 5f,
            WalkSpeed = 2f,
            RoadSpeedMultiplier = 1f,
            ArriveDistance = 0.05f
        });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(2, 2) });
        em.SetComponentData(entity, new UnitHealth { Current = 450, Max = 450 });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 1, AggroRangeCells = 120, ChaseBreakDistance = 120f });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = 600f,
            CooldownSeconds = 3f,
            Damage = 90,
            TraceVisibleSeconds = 0.08f
        });
        em.SetComponentData(entity, new UnitAttackCooldownComponent { CooldownRemaining = 0f });
        em.SetComponentData(entity, new UnitAttackTraceComponent { TimeRemaining = 0f, Phase = 0f });
        em.SetComponentData(entity, new UnitAttackAnimationComponent { TimeRemaining = 0f });
        em.SetComponentData(entity, new GroundMissileLauncherComponent
        {
            MinRange = 5f,
            MaxRange = 600f,
            PrepareSeconds = prepareSeconds,
            ReloadSeconds = reloadSeconds,
            BatteryElevatedAngleDegrees = -30f,
            RocketSpeed = 100f,
            ArcHeight = 10f,
            DamageRadius = 5f,
            Damage = 90
        });
        em.SetComponentData(entity, new GroundMissileLauncherStateComponent
        {
            Phase = (byte)GroundMissileLauncherPhase.Idle,
            TargetEntity = Entity.Null,
            TargetCell = default,
            TargetWorldPosition = default,
            Timer = 0f,
            SelectedRocketSlot = -1
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static void AssertBatteryStillElevated(quaternion rotation)
    {
        float3 forward = math.rotate(rotation, new float3(0f, 0f, 1f));
        Assert.Greater(forward.x, 0.7f, "Battery should keep yawing toward the target during post-launch hold.");
        Assert.Greater(forward.y, 0.35f, "Battery should remain elevated during post-launch hold instead of closing immediately.");
    }

    private static Entity CreateTarget(EntityManager em, float3 position, int health)
    {
        Entity entity = CreateHealthEntity(em, position, health, FactionIdentitySystem.EnemyFactionId);
        return entity;
    }

    private static Entity CreateFriendly(EntityManager em, float3 position, int health)
    {
        return CreateHealthEntity(em, position, health, FactionIdentitySystem.PlayerFactionId);
    }

    private static Entity CreateHealthEntity(EntityManager em, float3 position, int health, byte factionId)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = GridUtils.WorldToCell(new GridConfig { Width = 256, Height = 256, CellSize = 1f, Origin = float3.zero }, position) });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateRuntimeBuildingTarget(
        EntityManager em,
        int2 originCell,
        int2 footprintCells,
        float3 position,
        byte factionId,
        int health)
    {
        Entity entity = em.CreateEntity(
            typeof(Faction),
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitRespawnPrefab),
            typeof(RuntimeBuildingCombatTag),
            typeof(RuntimeBuildingCombatInfo),
            typeof(LocalTransform));
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, new UnitGrid { Cell = originCell + footprintCells / 2 });
        em.SetComponentData(entity, new UnitFootprint { Size = footprintCells });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, new UnitRespawnPrefab { Prefab = Entity.Null });
        em.SetComponentData(entity, new RuntimeBuildingCombatInfo
        {
            OwnerFactionId = factionId,
            OriginCell = originCell,
            FootprintCells = footprintCells,
            IsWall = 0,
            IsGate = 0
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
