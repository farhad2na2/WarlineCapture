using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public sealed class BuildingDefenseAttackSystemTests
{
    private const int AllocationTowerCount = 32;
    private const int AllocationCandidateCount = 740;
    private const int AllocationWarmupFrames = 32;
    private const int AllocationMeasuredFrames = 64;
    private const float AllocationDeltaSeconds = 0.2f;

    public static void RunFocusedValidation()
    {
        int passed = 0;
        try
        {
            var tests = new BuildingDefenseAttackSystemTests();
            tests.GuardTowerDefense_IgnoresNeutralTargetsAndFiresAtHostileTarget();
            passed++;
            tests.GuardTowerDefense_IgnoresAircraftAndFiresAtGroundTarget();
            passed++;
            tests.GuardTowerDefense_IgnoresDebugFireTargetAndFiresAtEligibleHostile();
            passed++;
            tests.GuardTowerDefense_SelectsNearestHostileRegardlessOfCreationOrder();
            passed++;
            tests.GuardTowerDefense_FourConcurrentSlotsAttackFourNearestHostilesInOrder();
            passed++;
            tests.GuardTowerDefense_IgnoresDestroyedTargetAndFiresAtLiveHostile();
            passed++;
            tests.GuardTowerDefense_RemovedTargetBetweenUpdatesClearsSlotAndReacquiresAfterInterval();
            passed++;
            tests.GuardTowerDefense_PreExistingFeedbackComponents_AreOverwrittenOnHit();
            passed++;
            tests.GuardTowerDefense_ConfiguredShot_EmitsWeaponAudioAndMuzzleImpactVfxRequests();
            passed++;
            tests.WarmedTargetCollectionUpdate_ThirtyTwoTowersAndSevenHundredFortyCandidates_DoesNotAllocateManagedMemory();
            passed++;
            tests.BuildingDefenseSource_UsesPersistentDirectIterationScratchWithoutArraySnapshots();
            passed++;
            tests.BuildingDefenseSource_AuditsSingleCompletionAndNamedEntityManagerBoundaries();
            passed++;
            tests.BuildingDefenseSource_DefinesStableZeroAllocationPhaseMarkers();
            passed++;

            Debug.Log($"[BuildingDefenseAttackSystemValidation] result=Passed tests={passed}");
            ValidationExit.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError($"[BuildingDefenseAttackSystemValidation] result=Failed passed={passed}");
            ValidationExit.Exit(1);
        }
    }

    [Test]
    public void GuardTowerDefense_IgnoresNeutralTargetsAndFiresAtHostileTarget()
    {
        using World world = new("GuardTowerDefense_IgnoresNeutralTargetsAndFiresAtHostileTarget");
        EntityManager em = world.EntityManager;

        Entity tower = CreateGuardTower(
            em,
            new float3(0f, 0f, 0f),
            FactionIdentity.PlayerFactionId,
            maxConcurrentAttacks: 1);
        Entity neutralTarget = CreateTarget(
            em,
            new float3(2f, 0f, 0f),
            health: 100,
            factionId: FactionIdentity.NeutralFactionId);
        Entity hostileTarget = CreateTarget(
            em,
            new float3(6f, 0f, 0f),
            health: 100,
            factionId: FactionIdentity.EnemyFactionId);

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attackSystem, elapsedTime: 0.1d, deltaTime: 0.1f);

        Assert.AreEqual(100, GetHealth(em, neutralTarget), "Neutral targets must remain ineligible even when they are closest.");
        Assert.AreEqual(90, GetHealth(em, hostileTarget), "The nearest eligible hostile should still be attacked.");
        Assert.AreEqual(hostileTarget, em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0].Target);
        Assert.IsFalse(em.HasComponent<RecentAttacker>(neutralTarget));
        Assert.AreEqual(tower, em.GetComponentData<RecentAttacker>(hostileTarget).Attacker);
    }

    [Test]
    public void GuardTowerDefense_IgnoresAircraftAndFiresAtGroundTarget()
    {
        using World world = new("GuardTowerDefense_IgnoresAircraftAndFiresAtGroundTarget");
        EntityManager em = world.EntityManager;

        Entity tower = CreateGuardTower(
            em,
            new float3(0f, 0f, 0f),
            FactionIdentity.EnemyFactionId,
            maxConcurrentAttacks: 1);
        Entity airTarget = CreateTarget(
            em,
            new float3(2f, 12f, 0f),
            health: 100,
            factionId: FactionIdentity.PlayerFactionId,
            air: true);
        Entity groundTarget = CreateTarget(
            em,
            new float3(10f, 0f, 0f),
            health: 100,
            factionId: FactionIdentity.PlayerFactionId);

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attackSystem, elapsedTime: 0.1d, deltaTime: 0.1f);

        Assert.AreEqual(100, GetHealth(em, airTarget), "Guard towers must not use ground-defense fire against aircraft.");
        Assert.AreEqual(90, GetHealth(em, groundTarget), "Guard towers should still attack in-range ground units.");
        Assert.IsTrue(em.HasComponent<RecentAttacker>(groundTarget));
        Assert.AreEqual(tower, em.GetComponentData<RecentAttacker>(groundTarget).Attacker);
        Assert.IsFalse(em.HasComponent<RecentAttacker>(airTarget));
    }

    [Test]
    public void GuardTowerDefense_IgnoresDebugFireTargetAndFiresAtEligibleHostile()
    {
        using World world = new("GuardTowerDefense_IgnoresDebugFireTargetAndFiresAtEligibleHostile");
        EntityManager em = world.EntityManager;

        Entity tower = CreateGuardTower(
            em,
            new float3(0f, 0f, 0f),
            FactionIdentity.EnemyFactionId,
            maxConcurrentAttacks: 1);
        Entity debugTarget = CreateTarget(
            em,
            new float3(2f, 0f, 0f),
            health: 100,
            factionId: FactionIdentity.PlayerFactionId);
        em.AddComponentData(debugTarget, new DebugFireTargetTag { Source = tower });
        Entity hostileTarget = CreateTarget(
            em,
            new float3(6f, 0f, 0f),
            health: 100,
            factionId: FactionIdentity.PlayerFactionId);

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attackSystem, elapsedTime: 0.1d, deltaTime: 0.1f);

        Assert.AreEqual(100, GetHealth(em, debugTarget), "Debug fire targets must remain outside automatic building-defense acquisition.");
        Assert.AreEqual(90, GetHealth(em, hostileTarget));
        Assert.AreEqual(hostileTarget, em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0].Target);
        Assert.IsFalse(em.HasComponent<RecentAttacker>(debugTarget));
    }

    [Test]
    public void GuardTowerDefense_SelectsNearestHostileRegardlessOfCreationOrder()
    {
        using World world = new("GuardTowerDefense_SelectsNearestHostileRegardlessOfCreationOrder");
        EntityManager em = world.EntityManager;

        Entity tower = CreateGuardTower(
            em,
            new float3(0f, 0f, 0f),
            FactionIdentity.EnemyFactionId,
            maxConcurrentAttacks: 1);
        Entity farTarget = CreateTarget(em, new float3(9f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);
        Entity nearestTarget = CreateTarget(em, new float3(2f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);
        Entity middleTarget = CreateTarget(em, new float3(5f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attackSystem, elapsedTime: 0.1d, deltaTime: 0.1f);

        Assert.AreEqual(100, GetHealth(em, farTarget));
        Assert.AreEqual(90, GetHealth(em, nearestTarget));
        Assert.AreEqual(100, GetHealth(em, middleTarget));
        Assert.AreEqual(nearestTarget, em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0].Target);
    }

    [Test]
    public void GuardTowerDefense_FourConcurrentSlotsAttackFourNearestHostilesInOrder()
    {
        using World world = new("GuardTowerDefense_FourConcurrentSlotsAttackFourNearestHostilesInOrder");
        EntityManager em = world.EntityManager;

        Entity tower = CreateGuardTower(
            em,
            new float3(0f, 0f, 0f),
            FactionIdentity.EnemyFactionId,
            maxConcurrentAttacks: 4);
        Entity fifthTarget = CreateTarget(em, new float3(10f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);
        Entity secondTarget = CreateTarget(em, new float3(4f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);
        Entity fourthTarget = CreateTarget(em, new float3(8f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);
        Entity firstTarget = CreateTarget(em, new float3(2f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);
        Entity thirdTarget = CreateTarget(em, new float3(6f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attackSystem, elapsedTime: 0.1d, deltaTime: 0.1f);

        DynamicBuffer<BuildingDefenseAttackSlot> slots = em.GetBuffer<BuildingDefenseAttackSlot>(tower);
        Assert.AreEqual(4, slots.Length);
        Assert.AreEqual(firstTarget, slots[0].Target);
        Assert.AreEqual(secondTarget, slots[1].Target);
        Assert.AreEqual(thirdTarget, slots[2].Target);
        Assert.AreEqual(fourthTarget, slots[3].Target);

        Assert.AreEqual(90, GetHealth(em, firstTarget));
        Assert.AreEqual(90, GetHealth(em, secondTarget));
        Assert.AreEqual(90, GetHealth(em, thirdTarget));
        Assert.AreEqual(90, GetHealth(em, fourthTarget));
        Assert.AreEqual(100, GetHealth(em, fifthTarget), "Targets beyond the four-slot cap must not be attacked.");

        for (int i = 0; i < slots.Length; i++)
        {
            Assert.AreEqual(1, slots[i].ShotCounter);
            Assert.AreEqual(0.3f, slots[i].CooldownRemaining, 0.0001f);
        }
    }

    [Test]
    public void GuardTowerDefense_IgnoresDestroyedTargetAndFiresAtLiveHostile()
    {
        using World world = new("GuardTowerDefense_IgnoresDestroyedTargetAndFiresAtLiveHostile");
        EntityManager em = world.EntityManager;

        Entity tower = CreateGuardTower(
            em,
            new float3(0f, 0f, 0f),
            FactionIdentity.EnemyFactionId,
            maxConcurrentAttacks: 1);
        Entity destroyedTarget = CreateTarget(em, new float3(2f, 0f, 0f), 0, FactionIdentity.PlayerFactionId);
        Entity liveTarget = CreateTarget(em, new float3(5f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attackSystem, elapsedTime: 0.1d, deltaTime: 0.1f);

        Assert.AreEqual(0, GetHealth(em, destroyedTarget));
        Assert.AreEqual(90, GetHealth(em, liveTarget));
        Assert.AreEqual(liveTarget, em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0].Target);
        Assert.IsFalse(em.HasComponent<RecentAttacker>(destroyedTarget));
    }

    [Test]
    public void GuardTowerDefense_RemovedTargetBetweenUpdatesClearsSlotAndReacquiresAfterInterval()
    {
        using World world = new("GuardTowerDefense_RemovedTargetBetweenUpdatesClearsSlotAndReacquiresAfterInterval");
        EntityManager em = world.EntityManager;

        Entity tower = CreateGuardTower(
            em,
            new float3(0f, 0f, 0f),
            FactionIdentity.EnemyFactionId,
            maxConcurrentAttacks: 1);
        Entity removedTarget = CreateTarget(em, new float3(2f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);
        Entity fallbackTarget = CreateTarget(em, new float3(5f, 0f, 0f), 100, FactionIdentity.PlayerFactionId);

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attackSystem, elapsedTime: 1d, deltaTime: 0.1f);

        BuildingDefenseAttackSlot initialSlot = em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0];
        Assert.AreEqual(removedTarget, initialSlot.Target);
        Assert.AreEqual(90, GetHealth(em, removedTarget));
        Assert.AreEqual(100, GetHealth(em, fallbackTarget));

        em.DestroyEntity(removedTarget);
        Update(world, attackSystem, elapsedTime: 1.05d, deltaTime: 0.05f);

        BuildingDefenseAttackSlot clearedSlot = em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0];
        Assert.AreEqual(Entity.Null, clearedSlot.Target, "A removed target must be cleared before the next acquisition pass.");
        Assert.AreEqual(1, clearedSlot.ShotCounter);
        Assert.AreEqual(0.25f, clearedSlot.CooldownRemaining, 0.0001f, "Clearing a stale target must preserve cooldown progress.");
        Assert.AreEqual(100, GetHealth(em, fallbackTarget), "The system must not reacquire before the 0.12-second interval.");

        Update(world, attackSystem, elapsedTime: 1.13d, deltaTime: 0.08f);

        BuildingDefenseAttackSlot reacquiredSlot = em.GetBuffer<BuildingDefenseAttackSlot>(tower)[0];
        Assert.AreEqual(fallbackTarget, reacquiredSlot.Target);
        Assert.AreEqual(1, reacquiredSlot.ShotCounter);
        Assert.AreEqual(0.17f, reacquiredSlot.CooldownRemaining, 0.0001f);
        Assert.AreEqual(100, GetHealth(em, fallbackTarget), "Reacquisition must not bypass the slot cooldown.");
    }

    [Test]
    public void GuardTowerDefense_PreExistingFeedbackComponents_AreOverwrittenOnHit()
    {
        using World world = new(nameof(GuardTowerDefense_PreExistingFeedbackComponents_AreOverwrittenOnHit));
        EntityManager em = world.EntityManager;
        float3 towerPosition = new(2.2f, 0f, 3.3f);
        float3 targetPosition = new(8.2f, 0f, 9.3f);
        Entity staleEntity = em.CreateEntity();
        Entity tower = CreateGuardTower(
            em,
            towerPosition,
            FactionIdentity.EnemyFactionId,
            maxConcurrentAttacks: 1);
        Entity target = CreateTarget(
            em,
            targetPosition,
            health: 100,
            factionId: FactionIdentity.PlayerFactionId);

        em.AddComponentData(tower, new EngageTarget
        {
            Target = staleEntity,
            Cell = new int2(-10, -20),
            Position = new float3(-10f, -1f, -20f),
            IsCommanded = 1
        });
        em.AddComponentData(target, new RecentAttacker
        {
            Attacker = staleEntity,
            Cell = new int2(-30, -40),
            Position = new float3(-30f, -1f, -40f)
        });
        em.AddComponentData(target, new RecentDamageHealthBarVisibility { TimeRemaining = 0.25f });

        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        Update(world, attackSystem, elapsedTime: 0.1d, deltaTime: 0.1f);

        Assert.AreEqual(90, GetHealth(em, target));

        EngageTarget engageTarget = em.GetComponentData<EngageTarget>(tower);
        Assert.AreEqual(target, engageTarget.Target);
        Assert.AreEqual(new int2(8, 9), engageTarget.Cell);
        Assert.AreEqual(targetPosition, engageTarget.Position);
        Assert.AreEqual(0, engageTarget.IsCommanded);

        RecentAttacker recentAttacker = em.GetComponentData<RecentAttacker>(target);
        Assert.AreEqual(tower, recentAttacker.Attacker);
        Assert.AreEqual(new int2(2, 3), recentAttacker.Cell);
        Assert.AreEqual(towerPosition, recentAttacker.Position);
        Assert.AreEqual(
            2f,
            em.GetComponentData<RecentDamageHealthBarVisibility>(target).TimeRemaining,
            0.001f);
    }

    [Test]
    public void GuardTowerDefense_ConfiguredShot_EmitsWeaponAudioAndMuzzleImpactVfxRequests()
    {
        using World world = new(nameof(GuardTowerDefense_ConfiguredShot_EmitsWeaponAudioAndMuzzleImpactVfxRequests));
        EntityManager em = world.EntityManager;
        float3 towerPosition = new(1f, 0f, 2f);
        float3 targetPosition = new(5f, 0f, 2f);
        GameObject muzzlePrefab = new("DefenseMuzzleVfxPrefab");
        GameObject impactPrefab = new("DefenseImpactVfxPrefab");
        try
        {
            Entity tower = CreateGuardTower(
                em,
                towerPosition,
                FactionIdentity.EnemyFactionId,
                maxConcurrentAttacks: 1);
            Entity target = CreateTarget(
                em,
                targetPosition,
                health: 100,
                factionId: FactionIdentity.PlayerFactionId);
            Entity turret = em.CreateEntity(typeof(LocalToWorld));
            em.SetComponentData(
                turret,
                new LocalToWorld { Value = float4x4.Translate(new float3(1f, 1f, 2f)) });
            em.AddComponentData(tower, new UnitTurretReference { Turret = turret });
            em.AddComponentData(tower, new UnitMuzzleFlashVfxReference
            {
                Prefab = muzzlePrefab,
                HeightOffset = 0.25f,
                ForwardOffset = 0.5f
            });
            em.AddComponentData(tower, new UnitAttackImpactVfxReference { Prefab = impactPrefab });
            em.AddComponentData(tower, new UnitAttackTraceOriginPattern
            {
                OriginCount = 3,
                LateralOffset = 0.4f
            });

            SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
            Update(world, attackSystem, elapsedTime: 2d, deltaTime: 0.1f);

            DynamicBuffer<AudioPlaybackRequestElement> audioRequests = GetAudioRequests(em);
            Assert.AreEqual(1, audioRequests.Length, "Defense fire should emit only the configured weapon-fire request.");
            AudioPlaybackRequestElement audioRequest = audioRequests[0];
            Assert.AreEqual(AudioEventIds.GameplayWeaponFireSmallArms, audioRequest.EventId.ToString());
            Assert.AreEqual(AudioEventIds.GameplayWeaponFireSmallArmsHash, audioRequest.EventHash);
            Assert.AreEqual(tower, audioRequest.SourceEntity);
            Assert.AreEqual("SFX", audioRequest.BusId.ToString());
            Assert.AreEqual(AudioPlaybackPriority.Medium, audioRequest.Priority);
            Assert.AreEqual(AudioPlaybackRequestStatus.Pending, audioRequest.Status);
            Assert.AreEqual(1, audioRequest.Spatial);
            Assert.AreEqual(1, audioRequest.HasWorldPosition);
            Assert.AreEqual(towerPosition, audioRequest.WorldPosition);
            Assert.AreEqual(2f, audioRequest.RequestedAt, 0.001f);
            Assert.AreEqual(0.04f, audioRequest.CooldownSeconds, 0.001f);

            using EntityQuery vfxQuery = em.CreateEntityQuery(ComponentType.ReadOnly<UnitAttackVfxRequest>());
            using NativeArray<UnitAttackVfxRequest> vfxRequests =
                vfxQuery.ToComponentDataArray<UnitAttackVfxRequest>(Allocator.Temp);
            Assert.AreEqual(2, vfxRequests.Length, "A traced defense shot must enqueue one muzzle and one impact request.");

            UnitAttackVfxRequest muzzleRequest = FindVfxRequest(vfxRequests, UnitAttackVfxRequestKind.MuzzleFlash);
            Assert.AreEqual(tower, muzzleRequest.Source);
            Assert.AreEqual(target, muzzleRequest.Target);
            Assert.AreEqual(towerPosition, muzzleRequest.SourcePosition);
            Assert.AreEqual(targetPosition, muzzleRequest.TargetPosition);
            Assert.AreSame(muzzlePrefab, muzzleRequest.Prefab.Value);
            Assert.AreEqual(1.5f, muzzleRequest.PlaybackPosition.x, 0.001f);
            Assert.AreEqual(1.25f, muzzleRequest.PlaybackPosition.y, 0.001f);
            Assert.AreEqual(2f, muzzleRequest.PlaybackPosition.z, 0.001f);
            Assert.AreEqual(3, muzzleRequest.OriginCount);
            Assert.AreEqual(0.4f, muzzleRequest.LateralOffset, 0.001f);

            UnitAttackVfxRequest impactRequest = FindVfxRequest(vfxRequests, UnitAttackVfxRequestKind.Impact);
            Assert.AreEqual(tower, impactRequest.Source);
            Assert.AreEqual(target, impactRequest.Target);
            Assert.AreEqual(towerPosition, impactRequest.SourcePosition);
            Assert.AreEqual(targetPosition, impactRequest.TargetPosition);
            Assert.AreSame(impactPrefab, impactRequest.Prefab.Value);
            Assert.AreEqual(targetPosition, impactRequest.PlaybackPosition);
            Assert.AreEqual(1, impactRequest.OriginCount);
            Assert.AreEqual(0f, impactRequest.LateralOffset, 0.001f);
            Assert.AreEqual(90, GetHealth(em, target));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(muzzlePrefab);
            UnityEngine.Object.DestroyImmediate(impactPrefab);
        }
    }

    [Test]
    public void WarmedTargetCollectionUpdate_ThirtyTwoTowersAndSevenHundredFortyCandidates_DoesNotAllocateManagedMemory()
    {
        long fixtureSetupBefore = GC.GetAllocatedBytesForCurrentThread();
        using World world = new(nameof(WarmedTargetCollectionUpdate_ThirtyTwoTowersAndSevenHundredFortyCandidates_DoesNotAllocateManagedMemory));
        EntityManager em = world.EntityManager;
        var towers = new Entity[AllocationTowerCount];
        var candidates = new Entity[AllocationCandidateCount];

        for (int i = 0; i < candidates.Length; i++)
        {
            candidates[i] = CreateTarget(
                em,
                new float3(i + 1f, 0f, 0f),
                health: 100,
                factionId: FactionIdentity.PlayerFactionId);
        }

        for (int i = 0; i < towers.Length; i++)
            towers[i] = CreateWarmedAllocationTower(em);

        AudioEventRequestSystem.EnsureAudioEntity(em);
        SystemHandle attackSystem = world.CreateSystem<BuildingDefenseAttackSystem>();
        double elapsedTime = 0d;
        for (int frame = 0; frame < AllocationWarmupFrames; frame++)
        {
            elapsedTime += AllocationDeltaSeconds;
            Update(world, attackSystem, elapsedTime, AllocationDeltaSeconds);
        }

        long fixtureSetupAndWarmupAllocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() - fixtureSetupBefore;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long productionUpdateAllocatedBytes = 0;
        long maxUpdateAllocatedBytes = 0;
        int firstAllocatingFrame = -1;
        long firstAllocatingFrameBytes = 0;
        long measurementWindowBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < AllocationMeasuredFrames; frame++)
        {
            elapsedTime += AllocationDeltaSeconds;
            world.SetTime(new TimeData(elapsedTime, AllocationDeltaSeconds));

            long productionUpdateBefore = GC.GetAllocatedBytesForCurrentThread();
            attackSystem.Update(world.Unmanaged);
            long frameAllocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - productionUpdateBefore;

            productionUpdateAllocatedBytes += frameAllocatedBytes;
            if (frameAllocatedBytes > maxUpdateAllocatedBytes)
                maxUpdateAllocatedBytes = frameAllocatedBytes;
            if (frameAllocatedBytes > 0 && firstAllocatingFrame < 0)
            {
                firstAllocatingFrame = frame;
                firstAllocatingFrameBytes = frameAllocatedBytes;
            }

            em.CompleteAllTrackedJobs();
        }

        long measurementWindowAllocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() - measurementWindowBefore;
        long harnessAllocatedBytes = measurementWindowAllocatedBytes - productionUpdateAllocatedBytes;

        Assert.AreEqual(AllocationTowerCount, towers.Length);
        Assert.AreEqual(AllocationCandidateCount, candidates.Length);
        for (int towerIndex = 0; towerIndex < towers.Length; towerIndex++)
        {
            DynamicBuffer<BuildingDefenseAttackSlot> slots =
                em.GetBuffer<BuildingDefenseAttackSlot>(towers[towerIndex]);
            Assert.AreEqual(4, slots.Length);
            for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                Assert.AreEqual(
                    candidates[slotIndex],
                    slots[slotIndex].Target,
                    $"Tower {towerIndex} must preserve nearest-hostile ordering for slot {slotIndex}.");
                Assert.AreEqual(0, slots[slotIndex].ShotCounter);
                Assert.Greater(slots[slotIndex].CooldownRemaining, 0f);
            }
        }

        for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            Assert.AreEqual(100, GetHealth(em, candidates[slotIndex]));

        Debug.Log(
            $"[BuildingDefenseAttackSystemAllocationValidation] towers={AllocationTowerCount} candidates={AllocationCandidateCount} warmupFrames={AllocationWarmupFrames} measuredFrames={AllocationMeasuredFrames} productionUpdateAllocatedBytes={productionUpdateAllocatedBytes} maxUpdateAllocatedBytes={maxUpdateAllocatedBytes} firstAllocatingFrame={firstAllocatingFrame} firstAllocatingFrameBytes={firstAllocatingFrameBytes} measurementWindowAllocatedBytes={measurementWindowAllocatedBytes} harnessAllocatedBytes={harnessAllocatedBytes} fixtureSetupAndWarmupAllocatedBytes={fixtureSetupAndWarmupAllocatedBytes}");

        Assert.AreEqual(
            0,
            productionUpdateAllocatedBytes,
            $"Warmed BuildingDefenseAttackSystem.OnUpdate via SystemHandle.Update allocated {productionUpdateAllocatedBytes} managed bytes over {AllocationMeasuredFrames} target-acquisition updates with {AllocationTowerCount} towers and {AllocationCandidateCount} candidates; firstAllocatingFrame={firstAllocatingFrame}, firstAllocatingFrameBytes={firstAllocatingFrameBytes}, maxUpdateAllocatedBytes={maxUpdateAllocatedBytes}. The complete measurement window allocated {measurementWindowAllocatedBytes} bytes, of which {harnessAllocatedBytes} bytes came from time advancement, allocation sampling, and job completion outside SystemHandle.Update. Do not weaken this zero-byte gate; profile BuildingDefenseAttackSystem.OnUpdate when this assertion fails.");
        Assert.GreaterOrEqual(
            harnessAllocatedBytes,
            0,
            "Harness allocation is the complete measurement window minus bytes allocated inside SystemHandle.Update and cannot be negative.");
    }

    [Test]
    public void BuildingDefenseSource_UsesPersistentDirectIterationScratchWithoutArraySnapshots()
    {
        string source = ReadBuildingDefenseSource();
        StringAssert.Contains("NativeList<TargetCandidate>", source);
        StringAssert.Contains("Allocator.Persistent", source);
        StringAssert.Contains("public void OnDestroy(ref SystemState state)", source);
        StringAssert.Contains("_targetCandidates.IsCreated", source);
        StringAssert.Contains("_targetCandidates.Dispose()", source);
        StringAssert.Contains(
            "SystemAPI.Query<RefRO<UnitHealth>, RefRO<Faction>, RefRO<LocalTransform>>()",
            source);
        StringAssert.Contains(".WithNone<UnitAirMovement, DebugFireTargetTag>()", source);
        StringAssert.DoesNotContain("_targetQuery", source);
        StringAssert.DoesNotContain("ToEntityArray(", source);
        StringAssert.DoesNotContain("ToComponentDataArray<", source);
    }

    [Test]
    public void BuildingDefenseSource_AuditsSingleCompletionAndNamedEntityManagerBoundaries()
    {
        string source = ReadBuildingDefenseSource();

        Assert.AreEqual(
            1,
            CountOccurrences(source, "state.Dependency.Complete();"),
            "Building defense keeps one explicit completion for same-frame optional effect helpers and ECB playback.");
        StringAssert.Contains("Keep this completion explicit until", source);
        StringAssert.DoesNotContain("CompleteDependencyBefore", source);

        string[] removedDirectOperations =
        {
            "em.Exists(",
            "em.HasComponent<",
            "em.GetComponentData<",
            "em.SetComponentData("
        };
        for (int i = 0; i < removedDirectOperations.Length; i++)
        {
            StringAssert.DoesNotContain(
                removedDirectOperations[i],
                source,
                $"Local direct EntityManager operation `{removedDirectOperations[i]}` must use the audited lookup bundle.");
        }

        string[] remainingEntityManagerBoundaries =
        {
            "AudioEventRequestSystem.EnsureAudioEntity(em)",
            "GameplayAudioFeedbackSystemHelper.TryEmitWeaponFireAudio(em",
            "UnitAttackSystem.TryEmitUnitUnderAttackAudio(",
            "UnitAttackSystem.TryBuildAttackVfxRequest(em",
            "ecb.Playback(em)"
        };
        for (int i = 0; i < remainingEntityManagerBoundaries.Length; i++)
        {
            Assert.AreEqual(
                1,
                CountOccurrences(source, remainingEntityManagerBoundaries[i]),
                $"EntityManager boundary `{remainingEntityManagerBoundaries[i]}` must remain explicit and uniquely auditable.");
        }

        StringAssert.Contains("DirectComponentAccess", source);
        StringAssert.Contains("SystemAPI.GetEntityStorageInfoLookup()", source);
        StringAssert.Contains("SystemAPI.GetComponentLookup<UnitHealth>()", source);
    }

    [Test]
    public void BuildingDefenseSource_DefinesStableZeroAllocationPhaseMarkers()
    {
        string source = ReadBuildingDefenseSource();
        string captureSource = ReadCanvasMatchFpsValidationSource();

        StringAssert.Contains("using Unity.Profiling;", source);
        StringAssert.Contains(
            "RuntimeHelpers.RunClassConstructor(typeof(BuildingDefenseAttackSystem).TypeHandle);",
            captureSource,
            "The capture runner must register defense markers before opening the Match scene.");
        Assert.AreEqual(
            3,
            CountOccurrences(source, "private static readonly ProfilerMarker"),
            "Building defense must expose exactly the three APH-210 phase markers.");

        string[] markerNames =
        {
            "BuildingDefenseAttackSystem.TargetCollection",
            "BuildingDefenseAttackSystem.TargetSelection",
            "BuildingDefenseAttackSystem.EffectApplication"
        };
        for (int i = 0; i < markerNames.Length; i++)
        {
            Assert.AreEqual(
                1,
                CountOccurrences(source, $"new(\"{markerNames[i]}\")"),
                $"Profiler marker `{markerNames[i]}` must be a single stable static instance.");
            Assert.AreEqual(
                1,
                CountOccurrences(captureSource, $"\"{markerNames[i]}\""),
                $"Canvas Match capture must record `{markerNames[i]}` for comparable APH-210 metrics.");
        }

        Assert.AreEqual(1, CountOccurrences(source, "using (TargetCollectionMarker.Auto())"));
        Assert.AreEqual(1, CountOccurrences(source, "using (TargetSelectionMarker.Auto())"));
        Assert.AreEqual(
            2,
            CountOccurrences(source, "using (EffectApplicationMarker.Auto())"),
            "Effect timing must cover both immediate shot effects and deferred ECB playback.");
        StringAssert.DoesNotContain("Profiler.BeginSample", source);
        StringAssert.DoesNotContain("new ProfilerMarker", source);
    }

    private static string ReadBuildingDefenseSource()
    {
        string sourcePath = Path.Combine(
            Application.dataPath,
            "Game",
            "Scripts",
            "Systems",
            "BuildingDefenseAttackSystem.cs");
        Assert.IsTrue(File.Exists(sourcePath), $"Missing building-defense source at {sourcePath}.");
        return File.ReadAllText(sourcePath);
    }

    private static string ReadCanvasMatchFpsValidationSource()
    {
        string sourcePath = Path.Combine(
            Application.dataPath,
            "Game",
            "Scripts",
            "Editor",
            "CanvasMatchFpsValidation.cs");
        Assert.IsTrue(File.Exists(sourcePath), $"Missing Canvas Match capture source at {sourcePath}.");
        return File.ReadAllText(sourcePath);
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static void Update(World world, SystemHandle attackSystem, double elapsedTime, float deltaTime)
    {
        world.SetTime(new TimeData(elapsedTime, deltaTime));
        attackSystem.Update(world.Unmanaged);
        world.EntityManager.CompleteAllTrackedJobs();
    }

    private static int GetHealth(EntityManager em, Entity target)
    {
        return em.GetComponentData<UnitHealth>(target).Current;
    }

    private static DynamicBuffer<AudioPlaybackRequestElement> GetAudioRequests(EntityManager em)
    {
        Entity audioEntity = AudioEventRequestSystem.EnsureAudioEntity(em);
        return em.GetBuffer<AudioPlaybackRequestElement>(audioEntity);
    }

    private static UnitAttackVfxRequest FindVfxRequest(
        NativeArray<UnitAttackVfxRequest> requests,
        UnitAttackVfxRequestKind kind)
    {
        for (int i = 0; i < requests.Length; i++)
        {
            if (requests[i].Kind == (byte)kind)
                return requests[i];
        }

        Assert.Fail($"Missing defense VFX request kind {kind}.");
        return default;
    }

    private static Entity CreateGuardTower(
        EntityManager em,
        float3 position,
        byte factionId,
        byte maxConcurrentAttacks)
    {
        Entity entity = em.CreateEntity(
            typeof(RuntimeBuildingCombatTag),
            typeof(BuildingDefenseWeapon),
            typeof(UnitHealth),
            typeof(Faction),
            typeof(LocalTransform),
            typeof(UnitAttackTraceComponent));

        em.SetComponentData(entity, new BuildingDefenseWeapon
        {
            Range = 100f,
            CooldownSeconds = 0.3f,
            Damage = 10,
            MaxConcurrentAttacks = maxConcurrentAttacks,
            TraceVisibleSeconds = 0.1f,
            TracerEveryNthShot = 1
        });
        em.SetComponentData(entity, new UnitHealth { Current = 700, Max = 700 });
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        em.SetComponentData(entity, new UnitAttackTraceComponent());
        em.AddBuffer<BuildingDefenseAttackSlot>(entity);
        return entity;
    }

    private static Entity CreateWarmedAllocationTower(EntityManager em)
    {
        Entity tower = CreateGuardTower(
            em,
            float3.zero,
            FactionIdentity.EnemyFactionId,
            maxConcurrentAttacks: 4);

        BuildingDefenseWeapon weapon = em.GetComponentData<BuildingDefenseWeapon>(tower);
        weapon.Range = 1000f;
        em.SetComponentData(tower, weapon);

        DynamicBuffer<BuildingDefenseAttackSlot> slots = em.GetBuffer<BuildingDefenseAttackSlot>(tower);
        for (int i = 0; i < 4; i++)
        {
            slots.Add(new BuildingDefenseAttackSlot
            {
                Target = Entity.Null,
                CooldownRemaining = 1000f,
                ShotCounter = 0
            });
        }

        return tower;
    }

    private static Entity CreateTarget(
        EntityManager em,
        float3 position,
        int health,
        byte factionId,
        bool air = false)
    {
        Entity entity = em.CreateEntity(
            typeof(UnitHealth),
            typeof(Faction),
            typeof(LocalTransform));

        em.SetComponentData(entity, new UnitHealth { Current = health, Max = health });
        em.SetComponentData(entity, new Faction { Id = factionId });
        em.SetComponentData(entity, LocalTransform.FromPosition(position));
        if (air)
            em.AddComponentData(entity, new UnitAirMovement { CruiseHeight = math.max(1f, position.y), RunwayTaxiSpeed = 5f });
        return entity;
    }
}
