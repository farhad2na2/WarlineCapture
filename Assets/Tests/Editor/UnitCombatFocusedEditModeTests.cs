using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class UnitCombatFocusedEditModeTests
{
    public static void RunFocusedValidation()
    {
        try
        {
            var tests = new UnitCombatFocusedEditModeTests();
            tests.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();
            tests.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();
            tests.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();
            tests.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();
            tests.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
            Debug.Log("[UnitCombatFocusedEditModeValidation] result=Passed tests=5");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UnitCombatFocusedEditModeValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy()
    {
        foreach (var targetPolicy in new[]
        {
            new CombatTargetPolicy { Domain = CombatTargetDomain.Air, Visible = 1 },
            new CombatTargetPolicy { Domain = CombatTargetDomain.Ground, Visible = 0 },
            new CombatTargetPolicy { Domain = CombatTargetDomain.Ground, Visible = 1 }
        })
        {
            using var world = new World(nameof(StandardAttack_RespectsAuthoredDomainAndVisibilityPolicy));
            var em = world.EntityManager;
            CreateGrid(em);
            Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), 100);
            Entity attacker = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, 35);
            em.AddComponentData(attacker, new CombatTargetPolicy
            { AllowedTargets = CombatTargetDomain.Ground, Domain = CombatTargetDomain.Infantry, Visible = 1 });
            em.AddComponentData(target, targetPolicy);
            var attack = world.CreateSystem<UnitAttackSystem>();
            world.SetTime(new TimeData(0.1d, 0.1f));
            attack.Update(world.Unmanaged);
            bool legal = targetPolicy.Domain == CombatTargetDomain.Ground && targetPolicy.Visible != 0;
            Assert.AreEqual(legal ? 65 : 100, em.GetComponentData<UnitHealth>(target).Current);
            Assert.AreEqual(legal ? 1 : 0, em.GetComponentData<UnitAttackTraceComponent>(attacker).ShotCounter);
        }
    }

    [Test]
    public void StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState()
    {
        using var world = new World("StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState");
        EntityManager em = world.EntityManager;
        CreateGrid(em);

        Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), health: 100);
        Entity attacker = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, damage: 35);

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(world.Unmanaged);

        Assert.AreEqual(65, em.GetComponentData<UnitHealth>(target).Current);
        Assert.IsTrue(em.HasComponent<RecentAttacker>(target), "Damage should record who last attacked the target.");
        RecentAttacker recentAttacker = em.GetComponentData<RecentAttacker>(target);
        Assert.AreEqual(attacker, recentAttacker.Attacker);
        Assert.AreEqual(new int2(4, 5), recentAttacker.Cell);
        Assert.AreEqual(new float3(4f, 0f, 5f), recentAttacker.Position);

        Assert.IsTrue(em.HasComponent<RecentDamageHealthBarVisibility>(target), "Non-lethal combat damage should show the recent-damage health bar window.");
        Assert.AreEqual(2f, em.GetComponentData<RecentDamageHealthBarVisibility>(target).TimeRemaining, 0.001f);
        Assert.IsFalse(em.HasComponent<UnitDeathAnimationComponent>(target), "Non-lethal damage must not start death cleanup.");

        UnitAttackCooldownComponent cooldown = em.GetComponentData<UnitAttackCooldownComponent>(attacker);
        UnitAttackTraceComponent trace = em.GetComponentData<UnitAttackTraceComponent>(attacker);
        Assert.AreEqual(1f, cooldown.CooldownRemaining, 0.001f);
        Assert.AreEqual(1, trace.ShotCounter);
        Assert.Greater(trace.TimeRemaining, 0f);
    }

    [Test]
    public void StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition()
    {
        using var world = new World(nameof(StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition));
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), health: 100);
        Entity attacker = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, damage: 35);
        em.SetComponentData(attacker, new UnitAttackCooldownComponent { CooldownRemaining = 0.18f });
        em.SetComponentData(attacker, new UnitAttackTraceComponent { TimeRemaining = 0.2f, ShotCounter = 7 });
        em.SetComponentData(attacker, new UnitAttackAnimationComponent { TimeRemaining = 0.5f });
        em.RemoveComponent<EngageTarget>(attacker);

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(world.Unmanaged);
        Assert.That(em.GetComponentData<UnitAttackCooldownComponent>(attacker).CooldownRemaining,
            Is.EqualTo(0.08f).Within(0.001f));
        Assert.That(em.GetComponentData<UnitAttackTraceComponent>(attacker).TimeRemaining,
            Is.EqualTo(0.1f).Within(0.001f));
        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(target).Current);
        Assert.AreEqual(7, em.GetComponentData<UnitAttackTraceComponent>(attacker).ShotCounter);

        world.SetTime(new TimeData(0.2d, 0.1f));
        attackSystem.Update(world.Unmanaged);
        Assert.That(em.GetComponentData<UnitAttackCooldownComponent>(attacker).CooldownRemaining,
            Is.EqualTo(0f).Within(0.001f));
        Assert.That(em.GetComponentData<UnitAttackTraceComponent>(attacker).TimeRemaining,
            Is.EqualTo(0f).Within(0.001f));

        em.AddComponentData(attacker, new EngageTarget
        {
            Target = target,
            Cell = new int2(4, 4),
            Position = new float3(4f, 0f, 4f),
            IsCommanded = 1
        });
        world.SetTime(new TimeData(0.3d, 0.1f));
        attackSystem.Update(world.Unmanaged);
        Assert.AreEqual(65, em.GetComponentData<UnitHealth>(target).Current,
            "Reacquisition should use the recovered cooldown to produce real damage.");
        Assert.AreEqual(8, em.GetComponentData<UnitAttackTraceComponent>(attacker).ShotCounter);
    }

    [Test]
    public void StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick()
    {
        using var world = new World(nameof(StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick));
        EntityManager em = world.EntityManager;
        CreateGrid(em);
        Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), health: 100);
        Entity engaged = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, damage: 35);
        Entity recovering = CreateAttacker(em, new int2(5, 5), new float3(5f, 0f, 5f), target, damage: 35);
        em.RemoveComponent<EngageTarget>(recovering);

        Entity suppressed = CreateAttacker(em, new int2(6, 5), new float3(6f, 0f, 5f), target, damage: 35);
        em.RemoveComponent<EngageTarget>(suppressed);
        em.AddComponent<CampaignMissionCombatSuppressedTag>(suppressed);
        Entity transit = CreateAttacker(em, new int2(7, 5), new float3(7f, 0f, 5f), target, damage: 35);
        em.RemoveComponent<EngageTarget>(transit);
        em.AddComponent<SupportPassengerTransitTag>(transit);
        Entity dead = CreateAttacker(em, new int2(8, 5), new float3(8f, 0f, 5f), target, damage: 35);
        em.RemoveComponent<EngageTarget>(dead);
        em.AddComponent<UnitDeathAnimationComponent>(dead);
        Entity blocker = CreateAttacker(em, new int2(9, 5), new float3(9f, 0f, 5f), target, damage: 35);
        em.RemoveComponent<EngageTarget>(blocker);
        em.AddComponent<StaticGridBlocker>(blocker);
        Entity airMissile = CreateAttacker(em, new int2(10, 5), new float3(10f, 0f, 5f), target, damage: 35);
        em.RemoveComponent<EngageTarget>(airMissile);
        em.AddComponent<AirMissileLauncherComponent>(airMissile);
        Entity disarmed = CreateAttacker(em, new int2(11, 5), new float3(11f, 0f, 5f), target, damage: 35);
        em.RemoveComponent<EngageTarget>(disarmed);
        em.SetComponentData(disarmed, new UnitCombat { CanAttack = 0, AutoEngage = 0 });

        foreach (Entity actor in new[] { engaged, recovering, suppressed, transit, dead, blocker, airMissile, disarmed })
        {
            em.SetComponentData(actor, new UnitAttackCooldownComponent { CooldownRemaining = 0.18f });
            em.SetComponentData(actor, new UnitAttackTraceComponent { TimeRemaining = 0.2f });
        }

        SystemHandle attackSystem = world.CreateSystem<UnitAttackSystem>();
        world.SetTime(new TimeData(0.1d, 0.1f));
        attackSystem.Update(world.Unmanaged);
        foreach (Entity actor in new[] { engaged, recovering })
        {
            Assert.That(em.GetComponentData<UnitAttackCooldownComponent>(actor).CooldownRemaining,
                Is.EqualTo(0.08f).Within(0.001f), "Each eligible actor should receive exactly one timer tick.");
            Assert.That(em.GetComponentData<UnitAttackTraceComponent>(actor).TimeRemaining,
                Is.EqualTo(0.1f).Within(0.001f));
        }
        foreach (Entity actor in new[] { suppressed, transit, dead, blocker, airMissile, disarmed })
        {
            Assert.That(em.GetComponentData<UnitAttackCooldownComponent>(actor).CooldownRemaining,
                Is.EqualTo(0.18f).Within(0.001f), "Excluded actors must retain their cooldown.");
            Assert.That(em.GetComponentData<UnitAttackTraceComponent>(actor).TimeRemaining,
                Is.EqualTo(0.2f).Within(0.001f));
        }
        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(target).Current);

        world.SetTime(new TimeData(0.1d, 0f));
        attackSystem.Update(world.Unmanaged);
        Assert.That(em.GetComponentData<UnitAttackCooldownComponent>(recovering).CooldownRemaining,
            Is.EqualTo(0.08f).Within(0.001f), "Paused simulation must not advance recovery.");

        Entity match = em.CreateEntity(typeof(SkirmishMatchState));
        em.SetComponentData(match, new SkirmishMatchState { Phase = SkirmishPhase.Finished });
        world.SetTime(new TimeData(0.2d, 0.1f));
        attackSystem.Update(world.Unmanaged);
        Assert.That(em.GetComponentData<UnitAttackCooldownComponent>(recovering).CooldownRemaining,
            Is.EqualTo(0.08f).Within(0.001f), "A finished match must not advance recovery.");
        Assert.AreEqual(100, em.GetComponentData<UnitHealth>(target).Current);
    }

    [Test]
    public void AttackVfxRequestResolution_PopulatesMuzzlePlaybackData()
    {
        using var world = new World("AttackVfxRequestResolution_PopulatesMuzzlePlaybackData");
        EntityManager em = world.EntityManager;
        GameObject prefab = new("MuzzleVfxPrefab");
        try
        {
            Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), health: 100);
            Entity attacker = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, damage: 35);
            Entity turret = em.CreateEntity(typeof(LocalToWorld));
            em.SetComponentData(turret, new LocalToWorld { Value = float4x4.Translate(new float3(4.25f, 1.5f, 5f)) });
            em.AddComponentData(attacker, new UnitTurretReference { Turret = turret });
            em.AddComponentData(attacker, new UnitMuzzleFlashVfxReference
            {
                Prefab = prefab,
                HeightOffset = 0.25f,
                ForwardOffset = 0.5f
            });
            em.AddComponentData(attacker, new UnitAttackTraceOriginPattern
            {
                OriginCount = 3,
                LateralOffset = 0.4f
            });

            bool built = UnitAttackSystem.TryBuildAttackVfxRequest(
                em,
                UnitAttackVfxRequestKind.MuzzleFlash,
                attacker,
                target,
                new float3(4f, 0f, 5f),
                new float3(4f, 0f, 4f),
                out UnitAttackVfxRequest request);

            Assert.IsTrue(built);
            Assert.AreSame(prefab, request.Prefab.Value);
            Assert.AreEqual((byte)UnitAttackVfxRequestKind.MuzzleFlash, request.Kind);
            Assert.AreEqual((byte)3, request.OriginCount);
            Assert.AreEqual(0.4f, request.LateralOffset, 0.001f);
            Assert.AreEqual(4.25f, request.PlaybackPosition.x, 0.001f);
            Assert.AreEqual(1.75f, request.PlaybackPosition.y, 0.001f);
            Assert.Less(request.PlaybackPosition.z, 5f, "Forward offset should move the muzzle toward the target.");
            Assert.Greater(math.lengthsq(request.SideRight), 0.9f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void AttackVfxRequestResolution_PopulatesImpactPlaybackData()
    {
        using var world = new World("AttackVfxRequestResolution_PopulatesImpactPlaybackData");
        EntityManager em = world.EntityManager;
        GameObject prefab = new("ImpactVfxPrefab");
        try
        {
            Entity target = CreateTarget(em, new int2(4, 4), new float3(4f, 0f, 4f), health: 100);
            Entity attacker = CreateAttacker(em, new int2(4, 5), new float3(4f, 0f, 5f), target, damage: 35);
            em.AddComponentData(attacker, new UnitAttackImpactVfxReference { Prefab = prefab });

            bool built = UnitAttackSystem.TryBuildAttackVfxRequest(
                em,
                UnitAttackVfxRequestKind.Impact,
                attacker,
                target,
                new float3(4f, 0f, 5f),
                new float3(4f, 0f, 4f),
                out UnitAttackVfxRequest request);

            Assert.IsTrue(built);
            Assert.AreSame(prefab, request.Prefab.Value);
            Assert.AreEqual((byte)UnitAttackVfxRequestKind.Impact, request.Kind);
            Assert.AreEqual(new float3(4f, 0f, 4f), request.PlaybackPosition);
            Assert.AreEqual((byte)1, request.OriginCount);
            Assert.AreEqual(0f, request.LateralOffset, 0.001f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }


    // Isolated native-system characterization; no mission/world/ARIA acceptance.
    [Serializable] private sealed class AttackCadenceStep
    {
        public int step, shotsBefore, shotsAfter, healthBefore, healthAfter;
        public float dt, elapsed, cooldownBefore, cooldownAfterAttack;
        public float animationBefore, animationAfterAttack, animationAfterVisual;
    }
    [Serializable] private sealed class AttackCadenceReport
    {
        public string profile;
        public bool configuredAnimation;
        public int shots, damage, damageEvents, damageEventTotal, frames;
        public float elapsed, cooldownSeconds, animationSeconds;
        public AttackCadenceStep[] steps;
    }

    public static void RunConfiguredRocketeerAttackCadenceCharacterization()
    {
        var config = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.UnitGridAuthoringPrefabConfigAsset>(
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset");
        Assert.IsNotNull(config);
        Assert.AreEqual(.25f, config.AttackAnimationSeconds, .0001f);
        Assert.Greater(config.AnimationOrder.Count, 0);
        Assert.IsTrue(Game.Configs.SkirmishRoleOverlayCatalog.TryGet(
            Game.Configs.SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice(),
            Game.Skirmish.Contracts.SkirmishRoleKind.Rocketeer, out var overlay));
        Assert.AreEqual(24f, overlay.RangeWorld);
        Assert.AreEqual(22, overlay.Damage);
        var fast = ExerciseRocketeerAttackCadence(config, overlay, "fast016", new[] { .016f }, true);
        var slow = ExerciseRocketeerAttackCadence(config, overlay, "slow333", new[] { .333f }, true);
        var irregular = ExerciseRocketeerAttackCadence(config, overlay, "irregular", new[] { .333f, .016f, .1f, .333f }, true);
        // Isolation control changes only the animation-order buffer, not weapon rules.
        var noOrder = ExerciseRocketeerAttackCadence(config, overlay, "slow333-no-order", new[] { .333f }, false);
        Assert.Greater(fast.shots, 0, "Fast configured native weapon must actually damage the target.");
        Assert.Greater(noOrder.shots, 0, "Slow native attack without windup buffer must remain a viable control.");
        Debug.Log("[RocketeerAttackCadenceSummary] fastShots=" + fast.shots + " slowShots=" + slow.shots +
            " irregularShots=" + irregular.shots + " slowNoOrderShots=" + noOrder.shots +
            " configuredSlowStarvationObserved=" + (slow.shots == 0 && noOrder.shots > 0) +
            " nativeMissionAcceptance=Pending");
        Debug.Log("[RocketeerAttackCadenceCharacterization] result=Collected profiles=4 productionChanged=0 missionReady=0");
    }


    private static AttackCadenceReport ExerciseRocketeerAttackCadence(
        Game.Configs.UnitGridAuthoringPrefabConfigAsset config,
        Game.Skirmish.Contracts.SkirmishRoleOverlay overlay,
        string label, float[] cadence, bool configuredAnimation)
    {
        using var world = new World("Native Rocketeer cadence " + label);
        var em = world.EntityManager;
        CreateGrid(em);
        using (var gridQuery = em.CreateEntityQuery(typeof(GridConfig)))
            em.SetComponentData(gridQuery.GetSingletonEntity(), new GridConfig
            { Width = 2048, Height = 1024, CellSize = 1, Origin = float3.zero });
        using var queueQuery = em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent));
        Entity queue = Game.Runtime.Combat.CombatDamageObservationUtility.EnsureQueue(em, queueQuery);
        Entity target = CreateTarget(em, new int2(1370, 679), new float3(1370, 0, 679.5f), 800);
        em.AddComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
        em.AddComponent<RuntimeBuildingCombatTag>(target);
        em.AddComponentData(target, new CombatTargetPolicy { Domain = CombatTargetDomain.Structure, Visible = 1 });
        Entity attacker = CreateAttacker(em, new int2(1347, 681),
            new float3(1347.8575439453125f, 0, 681.5f), target, overlay.Damage);
        em.AddComponentData(attacker, new Faction { Id = FactionIdentity.PlayerFactionId });
        SkirmishSharedCombatBinding.Bind(em, attacker, overlay);
        em.SetComponentData(attacker, new EngageTarget
        { Target = target, Cell = new int2(1370, 679), Position = new float3(1370, 0, 679.5f), IsCommanded = 1 });
        em.AddComponentData(attacker, new UnitAnimationSettings { AttackAnimationSeconds = config.AttackAnimationSeconds });
        em.AddComponent<UnitMoveVisualComponent>(attacker);
        em.AddComponent<UnitResolvedAnimationIndex>(attacker);
        if (configuredAnimation)
        {
            var order = em.AddBuffer<UnitAnimationOrderEntry>(attacker);
            foreach (var kind in config.AnimationOrder) order.Add(new UnitAnimationOrderEntry { Kind = (byte)kind });
        }
        Assert.IsFalse(em.HasComponent<GroundMissileLauncherComponent>(attacker));
        Assert.IsFalse(em.HasComponent<AirMissileLauncherComponent>(attacker));
        var weapon = em.GetComponentData<UnitAttack>(attacker);
        Assert.AreEqual(24f, weapon.Range);
        Assert.AreEqual(1f, weapon.CooldownSeconds);
        Assert.AreEqual(22, weapon.Damage);
        var attack = world.CreateSystem<UnitAttackSystem>();
        var animation = world.CreateSystem<UnitAnimationIndexSystem>();
        // Verify actual sorting authority, including reverse registration.
        var group = world.CreateSystemManaged<SimulationSystemGroup>();
        group.AddSystemToUpdateList(animation);
        group.AddSystemToUpdateList(attack);
        group.SortSystems();
        using (var ordered = group.GetAllSystems())
        {
            Assert.AreEqual(2, ordered.Length);
            Assert.AreEqual(attack, ordered[0]);
            Assert.AreEqual(animation, ordered[1]);
        }

        var rows = new System.Collections.Generic.List<AttackCadenceStep>(256);
        double time = 0;
        for (int i = 0; time < 4d - .000001d; i++)
        {
            Assert.Less(i, 512, "Bound the diagnostic; no persistent update hook.");
            float dt = math.min(cadence[i % cadence.Length], (float)(4d - time));
            Assert.Greater(dt, 0);
            var row = new AttackCadenceStep
            {
                step = i, dt = dt, elapsed = (float)(time + dt),
                shotsBefore = em.GetComponentData<UnitAttackTraceComponent>(attacker).ShotCounter,
                healthBefore = em.GetComponentData<UnitHealth>(target).Current,
                cooldownBefore = em.GetComponentData<UnitAttackCooldownComponent>(attacker).CooldownRemaining,
                animationBefore = em.GetComponentData<UnitAttackAnimationComponent>(attacker).TimeRemaining
            };
            time += dt;
            world.SetTime(new TimeData(time, dt));
            // Real systems only; manual calls expose their intermediate state in verified native order.
            attack.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            row.cooldownAfterAttack = em.GetComponentData<UnitAttackCooldownComponent>(attacker).CooldownRemaining;
            row.animationAfterAttack = em.GetComponentData<UnitAttackAnimationComponent>(attacker).TimeRemaining;
            row.shotsAfter = em.GetComponentData<UnitAttackTraceComponent>(attacker).ShotCounter;
            row.healthAfter = em.GetComponentData<UnitHealth>(target).Current;
            animation.Update(world.Unmanaged);
            em.CompleteAllTrackedJobs();
            row.animationAfterVisual = em.GetComponentData<UnitAttackAnimationComponent>(attacker).TimeRemaining;
            Assert.AreEqual(row.healthAfter, em.GetComponentData<UnitHealth>(target).Current);
            Assert.AreEqual(row.shotsAfter, em.GetComponentData<UnitAttackTraceComponent>(attacker).ShotCounter);
            Assert.AreEqual(target, em.GetComponentData<EngageTarget>(attacker).Target);
            rows.Add(row);
        }
        int damageTotal = 0;
        var events = em.GetBuffer<CombatDamageObservationElement>(queue);
        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            Assert.AreEqual(attacker, e.SourceEntity);
            Assert.AreEqual(target, e.TargetEntity);
            Assert.AreEqual(CombatDamageSourceKind.DirectFire, e.SourceKind);
            damageTotal += e.DamageApplied;
        }
        var report = new AttackCadenceReport
        {
            profile = label, configuredAnimation = configuredAnimation,
            shots = em.GetComponentData<UnitAttackTraceComponent>(attacker).ShotCounter,
            damage = 800 - em.GetComponentData<UnitHealth>(target).Current,
            damageEvents = events.Length, damageEventTotal = damageTotal,
            frames = rows.Count, elapsed = (float)time,
            cooldownSeconds = weapon.CooldownSeconds, animationSeconds = config.AttackAnimationSeconds,
            steps = rows.ToArray()
        };
        Assert.AreEqual(report.shots * overlay.Damage, report.damage);
        Assert.AreEqual(report.damage, damageTotal);
        Assert.AreEqual(report.shots, report.damageEvents);
        Assert.LessOrEqual(report.shots, (int)math.floor(report.elapsed / weapon.CooldownSeconds) + 1,
            "Native weapon cadence must not bypass authored rate; retain after any future fix.");
        Debug.Log("[RocketeerAttackCadenceProfile] " + JsonUtility.ToJson(report));
        return report;
    }


    // Conditional regressions: run only after native characterization confirms the defect.
    public static void RunConfiguredRocketeerAttackCadenceRegression()
    {
        foreach (float dt in new[] { .016f, .333f })
        {
            using var rig = new AttackCadenceRig();
            rig.Step(dt);
            Assert.AreEqual(0, rig.Shots, "Configured startup must still raise the weapon before firing.");
            Assert.Greater(rig.Em.GetComponentData<UnitAttackCooldownComponent>(rig.Self).CooldownRemaining, 0);
            rig.WaitForShot(dt);
        }
        var config = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.UnitGridAuthoringPrefabConfigAsset>(
            "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset");
        Assert.IsNotNull(config);
        Assert.IsTrue(Game.Configs.SkirmishRoleOverlayCatalog.TryGet(
            Game.Configs.SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice(),
            Game.Skirmish.Contracts.SkirmishRoleKind.Rocketeer, out var overlay));
        foreach (var profile in new[] { new[] { .016f }, new[] { .333f }, new[] { .333f, .016f, .1f, .333f } })
        {
            var report = ExerciseRocketeerAttackCadence(config, overlay, "regression", profile, true);
            Assert.GreaterOrEqual(report.shots, 2, "Configured native fire must not starve at supported cadence.");
        }
        CadenceLossRegression();
        CadenceRangeRegression();
        CadenceRetargetRegression();
        foreach (string invalid in new[] { "dead", "hidden", "domain", "suppressed" })
        {
            using var rig = new AttackCadenceRig();
            rig.WaitForShot(.016f);
            int before = rig.Shots;
            if (invalid == "dead") rig.Em.SetComponentData(rig.Other, new UnitHealth { Current = 0, Max = 800 });
            if (invalid == "hidden") rig.Em.SetComponentData(rig.Other,
                new CombatTargetPolicy { Domain = CombatTargetDomain.Structure, Visible = 0 });
            if (invalid == "domain") rig.Em.SetComponentData(rig.Other,
                new CombatTargetPolicy { Domain = CombatTargetDomain.Air, Visible = 1 });
            if (invalid == "suppressed") rig.Em.AddComponent<CampaignMissionCombatSuppressedTag>(rig.Other);
            rig.Point(rig.Other);
            rig.Advance(2f, .333f);
            Assert.AreEqual(before, rig.Shots, invalid + " target must not take a ready-boundary shot.");
        }
        var old = new UnitCombatFocusedEditModeTests();
        old.StandardAttack_NonLethalHitDamagesTargetAndRecordsFeedbackState();
        old.StandardAttack_CooldownRecoversAcrossLostEngagementAndCanFireOnReacquisition();
        old.StandardAttack_NoEngagementRecoverySkipsIneligibleActorsAndDoesNotDoubleTick();
        old.AttackVfxRequestResolution_PopulatesMuzzlePlaybackData();
        old.AttackVfxRequestResolution_PopulatesImpactPlaybackData();
        Game.Tests.Editor.SkirmishS002AriaHarnessTests.RunHomePadWitnessAndBoundaryFocusedValidation();
        Debug.Log("[RocketeerAttackCadenceRegression] result=Passed startup=2 cadence=3 transitions=3 refusals=4 legacy=5 missionReady=0");
    }


    private static void CadenceLossRegression()
    {
        using var rig = new AttackCadenceRig();
        rig.WaitForShot(.016f);
        int before = rig.Shots;
        rig.Em.RemoveComponent<EngageTarget>(rig.Self);
        rig.Advance(2f, .333f);
        Assert.AreEqual(before, rig.Shots);
        rig.Point(rig.Target);
        rig.Step(.333f);
        Assert.AreEqual(before, rig.Shots, "Idle reacquisition must still wind up.");
        rig.WaitForShot(.333f);
        before = rig.Shots;
        rig.Em.RemoveComponent<EngageTarget>(rig.Self);
        rig.Step(.1f);
        rig.Point(rig.Target);
        rig.Step(.1f);
        Assert.AreEqual(before, rig.Shots, "Early reacquisition cannot bypass remaining cooldown.");
        Assert.Greater(rig.Em.GetComponentData<UnitAttackCooldownComponent>(rig.Self).CooldownRemaining, 0);
        rig.WaitForShot(.1f);
    }
    private static void CadenceRangeRegression()
    {
        using var rig = new AttackCadenceRig();
        rig.WaitForShot(.016f);
        int before = rig.Shots;
        rig.Position(new float3(1300, 0, 600));
        rig.Advance(2f, .333f);
        Assert.AreEqual(before, rig.Shots, "Range gate remains mandatory when timer expires.");
        rig.Position(AttackCadenceRig.Origin);
        rig.Step(.333f);
        Assert.AreEqual(before, rig.Shots, "Return after idle weapon expiry starts a new windup.");
        rig.WaitForShot(.333f);
        before = rig.Shots;
        rig.Position(new float3(1300, 0, 600));
        rig.Step(.1f);
        float remaining = rig.Em.GetComponentData<UnitAttackCooldownComponent>(rig.Self).CooldownRemaining;
        rig.Position(AttackCadenceRig.Origin);
        rig.Step(.1f);
        Assert.AreEqual(before, rig.Shots);
        Assert.Less(rig.Em.GetComponentData<UnitAttackCooldownComponent>(rig.Self).CooldownRemaining, remaining,
            "Brief range exit does not restart an authored weapon cooldown.");
        rig.WaitForShot(.333f);
    }
    private static void CadenceRetargetRegression()
    {
        using var rig = new AttackCadenceRig();
        rig.WaitForShot(.016f);
        int oldHealth = rig.Em.GetComponentData<UnitHealth>(rig.Target).Current;
        int before = rig.Shots;
        rig.Point(rig.Other);
        rig.Step(.1f);
        Assert.AreEqual(before, rig.Shots, "Retarget cannot bypass a running cooldown.");
        rig.WaitForShot(.333f);
        Assert.AreEqual(oldHealth, rig.Em.GetComponentData<UnitHealth>(rig.Target).Current);
        Assert.AreEqual(778, rig.Em.GetComponentData<UnitHealth>(rig.Other).Current);
        before = rig.Shots;
        rig.Em.RemoveComponent<EngageTarget>(rig.Self);
        rig.Advance(2f, .333f);
        rig.Point(rig.Target);
        rig.Step(.333f);
        Assert.AreEqual(before, rig.Shots, "Retarget after full idle requires startup windup.");
        rig.WaitForShot(.333f);
        Assert.AreEqual(778, rig.Em.GetComponentData<UnitHealth>(rig.Other).Current);
    }


    private sealed class AttackCadenceRig : IDisposable
    {
        private readonly World world;
        private readonly SystemHandle attack, animation;
        private readonly Entity queue;
        private double elapsed, lastShotAt = -1;
        private int observedDamage;
        public static readonly float3 Origin = new float3(1347.8575439453125f, 0, 681.5f);
        public readonly Entity Self, Target, Other;
        public EntityManager Em => world.EntityManager;
        public int Shots => Em.GetComponentData<UnitAttackTraceComponent>(Self).ShotCounter;
        public AttackCadenceRig()
        {
            world = new World("Conditional configured weapon regression");
            try
            {
                var em = Em;
                var config = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.UnitGridAuthoringPrefabConfigAsset>(
                    "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Ghillie_Male_01_Config.asset");
                Assert.IsNotNull(config);
                Assert.AreEqual(.25f, config.AttackAnimationSeconds, .0001f);
                Assert.IsTrue(Game.Configs.SkirmishRoleOverlayCatalog.TryGet(
                    Game.Configs.SkirmishRoleOverlayCatalog.CreateCombinedArmsSlice(),
                    Game.Skirmish.Contracts.SkirmishRoleKind.Rocketeer, out var overlay));
                CreateGrid(em);
                using (var q = em.CreateEntityQuery(typeof(GridConfig)))
                    em.SetComponentData(q.GetSingletonEntity(), new GridConfig
                    { Width = 2048, Height = 1024, CellSize = 1, Origin = float3.zero });
                using (var q = em.CreateEntityQuery(typeof(CombatDamageObservationQueueComponent)))
                    queue = Game.Runtime.Combat.CombatDamageObservationUtility.EnsureQueue(em, q);
                Target = TargetAt(new float3(1370, 0, 679.5f));
                Other = TargetAt(new float3(1370, 0, 683.5f));
                Self = CreateAttacker(em, new int2(1347, 681), Origin, Target, overlay.Damage);
                em.AddComponentData(Self, new Faction { Id = FactionIdentity.PlayerFactionId });
                SkirmishSharedCombatBinding.Bind(em, Self, overlay);
                Assert.AreEqual(24f, em.GetComponentData<UnitAttack>(Self).Range);
                Assert.AreEqual(1f, em.GetComponentData<UnitAttack>(Self).CooldownSeconds);
                Assert.AreEqual(22, em.GetComponentData<UnitAttack>(Self).Damage);
                em.AddComponentData(Self, new UnitAnimationSettings { AttackAnimationSeconds = config.AttackAnimationSeconds });
                em.AddComponent<UnitMoveVisualComponent>(Self);
                em.AddComponent<UnitResolvedAnimationIndex>(Self);
                var order = em.AddBuffer<UnitAnimationOrderEntry>(Self);
                foreach (var kind in config.AnimationOrder) order.Add(new UnitAnimationOrderEntry { Kind = (byte)kind });
                Point(Target);
                attack = world.CreateSystem<UnitAttackSystem>();
                animation = world.CreateSystem<UnitAnimationIndexSystem>();
                var group = world.CreateSystemManaged<SimulationSystemGroup>();
                group.AddSystemToUpdateList(animation);
                group.AddSystemToUpdateList(attack);
                group.SortSystems();
                using var sorted = group.GetAllSystems();
                Assert.AreEqual(2, sorted.Length);
                Assert.AreEqual(attack, sorted[0]);
                Assert.AreEqual(animation, sorted[1]);
            }
            catch { world.Dispose(); throw; }
        }

        private Entity TargetAt(float3 position)
        {
            var target = CreateTarget(Em, new int2((int)math.floor(position.x), (int)math.floor(position.z)), position, 800);
            Em.AddComponentData(target, new Faction { Id = FactionIdentity.EnemyFactionId });
            Em.AddComponent<RuntimeBuildingCombatTag>(target);
            Em.AddComponentData(target, new CombatTargetPolicy { Domain = CombatTargetDomain.Structure, Visible = 1 });
            return target;
        }
        public void Point(Entity target)
        {
            var p = Em.GetComponentData<LocalTransform>(target).Position;
            var value = new EngageTarget
            { Target = target, Cell = new int2((int)math.floor(p.x), (int)math.floor(p.z)), Position = p, IsCommanded = 1 };
            if (Em.HasComponent<EngageTarget>(Self)) Em.SetComponentData(Self, value);
            else Em.AddComponentData(Self, value);
        }
        public void Position(float3 position)
        {
            Em.SetComponentData(Self, LocalTransform.FromPosition(position));
            Em.SetComponentData(Self, new UnitGrid
            { Cell = new int2((int)math.floor(position.x), (int)math.floor(position.z)) });
        }
        public void Step(float dt)
        {
            Assert.Greater(dt, 0);
            int before = Shots;
            int healthBefore = Em.GetComponentData<UnitHealth>(Target).Current + Em.GetComponentData<UnitHealth>(Other).Current;
            elapsed += dt;
            world.SetTime(new TimeData(elapsed, dt));
            attack.Update(world.Unmanaged);
            Em.CompleteAllTrackedJobs();
            animation.Update(world.Unmanaged);
            Em.CompleteAllTrackedJobs();
            int delta = Shots - before;
            Assert.That(delta, Is.InRange(0, 1), "No catch-up burst in one native update.");
            int applied = healthBefore - Em.GetComponentData<UnitHealth>(Target).Current - Em.GetComponentData<UnitHealth>(Other).Current;
            Assert.AreEqual(delta * 22, applied, "Actual native shot/damage relation.");
            observedDamage += applied;
            if (delta != 0)
            {
                if (lastShotAt >= 0) Assert.GreaterOrEqual(elapsed - lastShotAt, 1d - .0001d, "Authored1s cooldown ceiling.");
                lastShotAt = elapsed;
                Debug.Log("[RocketeerCadenceRegressionShot] elapsed=" + elapsed + " shots=" + Shots +
                    " targetHp=" + Em.GetComponentData<UnitHealth>(Target).Current +
                    " otherHp=" + Em.GetComponentData<UnitHealth>(Other).Current);
            }

            int sum = 0;
            var events = Em.GetBuffer<CombatDamageObservationElement>(queue);
            for (int i = 0; i < events.Length; i++)
            {
                Assert.AreEqual(Self, events[i].SourceEntity);
                Assert.IsTrue(events[i].TargetEntity == Target || events[i].TargetEntity == Other);
                Assert.AreEqual(CombatDamageSourceKind.DirectFire, events[i].SourceKind);
                sum += events[i].DamageApplied;
            }
            Assert.AreEqual(Shots, events.Length);
            Assert.AreEqual(observedDamage, sum);
        }
        public void Advance(float seconds, float dt)
        {
            double end = elapsed + seconds;
            for (int i = 0; elapsed < end - .000001d; i++)
            {
                Assert.Less(i, 512);
                Step(math.min(dt, (float)(end - elapsed)));
            }
        }
        public void WaitForShot(float dt)
        {
            int before = Shots;
            double start = elapsed;
            for (int i = 0; i < 512 && Shots == before && elapsed - start < 2.1d; i++) Step(dt);
            Assert.AreEqual(before + 1, Shots, "Actual configured weapon must progress after readiness.");
        }
        public void Dispose() { world.Dispose(); }
    }

    private static void CreateGrid(EntityManager em)
    {
        Entity gridEntity = em.CreateEntity(typeof(GridConfig));
        em.SetComponentData(gridEntity, new GridConfig
        {
            Width = 16,
            Height = 16,
            CellSize = 1f,
            Origin = float3.zero
        });
    }

    private static Entity CreateTarget(EntityManager em, int2 cell, float3 position, int health)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }

    private static Entity CreateAttacker(EntityManager em, int2 cell, float3 position, Entity target, int damage)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitGrid),
            typeof(UnitFootprint),
            typeof(UnitHealth),
            typeof(UnitCombat),
            typeof(UnitAttack),
            typeof(UnitAttackCooldownComponent),
            typeof(UnitAttackTraceComponent),
            typeof(UnitAttackAnimationComponent),
            typeof(EngageTarget),
            typeof(LocalTransform));
        em.SetComponentData(entity, new UnitGrid { Cell = cell });
        em.SetComponentData(entity, new UnitFootprint { Size = new int2(1, 1) });
        em.SetComponentData(entity, new UnitHealth { Current = 100, Max = 100 });
        em.SetComponentData(entity, new UnitCombat { CanAttack = 1, AutoEngage = 0 });
        em.SetComponentData(entity, new UnitAttack
        {
            Range = 2f,
            CooldownSeconds = 1f,
            Damage = damage,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 1
        });
        em.SetComponentData(entity, new EngageTarget
        {
            Target = target,
            Cell = new int2(4, 4),
            Position = new float3(4f, 0f, 4f),
            IsCommanded = 1
        });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        return entity;
    }
}
#endif
