using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Game.Components;
using Game.Runtime;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

public sealed class WorldLifecycleRecoveryMatrixTests
{
    private const string AuthorityPath =
        "Design/AgentReports/ArchitectureMaturity/am021_persistent_resource_ownership.json";

    [Test]
    public void AcceptedAuthority_HasZeroGapsAndEveryProductionPathResolves()
    {
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string authority = File.ReadAllText(Path.Combine(root, AuthorityPath));

        StringAssert.Contains("\"totalResourceCount\": 575", authority);
        StringAssert.Contains("\"explicitOwnerCount\": 553", authority);
        StringAssert.Contains("\"gapCount\": 0", authority);
        StringAssert.Contains("RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage", authority);
        StringAssert.Contains("RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage", authority);

        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(
                     authority,
                     "\\\"path\\\": \\\"(Assets/Game/Scripts/[^\\\"]+)\\\""))
        {
            paths.Add(match.Groups[1].Value);
        }

        Assert.Greater(paths.Count, 0);
        foreach (string relativePath in paths)
            Assert.IsTrue(File.Exists(Path.Combine(root, relativePath)), relativePath);
    }

    [Test]
    public void WorldReplacement_RebindsGovernedQueryAndGatewayOwners()
    {
        var cacheTests = new WorldScopedComponentQueryCacheTests();
        cacheTests.Cache_RebuildsAgainstDifferentWorld();
        cacheTests.Dispose_IsSafeAfterBoundWorldIsDestroyed();

        new PersistentResourceOwnershipLifecycleTests()
            .UiGateway_WorldReplacementRebindsWithoutRetainingPreviousQueries();
    }

    [Test]
    public void MissingDestroyedAndDuplicateSingletons_FailClosedThenRecover()
    {
        var cacheTests = new WorldScopedComponentQueryCacheTests();
        cacheTests.SingletonCache_CachesNegativeLookupUntilInvalidated();
        cacheTests.SingletonCache_RecoversAfterResolvedEntityIsDestroyed();
        cacheTests.SingletonCache_RecoversAfterResolvedEntityLosesComponent();
        cacheTests.SingletonCache_FailsClosedAfterPositiveCardinalityChanges();

        var threatTests = new ThreatWarningValidationTests();
        threatTests.ThreatWarningRuntimeState_MissingOrDuplicateSingletonFailsClosed();
        threatTests.MatchIntroEcsStateQuery_DuplicateBoundaryFailsClosed();
    }

    [Test]
    public void CommandEntityReplacement_RepairsBuffersAndPreservesRequiredIdentity()
    {
        var roadTests = new RoadBuildCommandCompositionSystemHelperTests();
        roadTests.CommandEntityCache_RebindsWhenWorldChanges();
        roadTests.CommandEntityCache_RecoversWhenCachedEntityIsDestroyed();
        roadTests.CommandEntityCache_AdoptsExistingQueueAndRepairsBuffers();

        var placementTests = new BuildingPlacementValidationUtilitySystemHelperTests();
        placementTests.BuildingUiPlacementCommandEntityCache_RebindsWhenWorldChanges();
        placementTests.BuildingUiPlacementCommandEntityCache_RecoversDestroyedEntityAndRepairsBuffers();
        placementTests.BuildingUiPlacementEconomyTransactionId_SurvivesQueueEntityRecreation();
    }

    [Test]
    public void SubsystemReset_IsIdempotentAndReinitializesExactlyOnce()
    {
        new PersistentResourceOwnershipLifecycleTests()
            .RuntimeLogBuffer_SubsystemResetClearsStateAndAllowsReinitialization();
        new ThreatWarningValidationTests().ThreatWarningRuntimeState_ResetIsIdempotent();
    }

    [Test]
    public void SystemRecreation_GetOrCreateReturnsOneExactSystemHandle()
    {
        using World world = new(nameof(SystemRecreation_GetOrCreateReturnsOneExactSystemHandle));

        SystemHandle first = world.GetOrCreateSystem<ThreatDetectionWarningSystem>();
        SystemHandle second = world.GetOrCreateSystem<ThreatDetectionWarningSystem>();

        Assert.AreEqual(first, second);
        Assert.AreNotEqual(SystemHandle.Null, first);
    }

    [Test]
    public void IntegratedRecovery_WorldCacheAuthorityCommandAndSystemRecoverWithoutStaleIdentity()
    {
        World previousDefault = World.DefaultGameObjectInjectionWorld;
        World firstWorld = new(nameof(IntegratedRecovery_WorldCacheAuthorityCommandAndSystemRecoverWithoutStaleIdentity) + ".First");
        World secondWorld = new(nameof(IntegratedRecovery_WorldCacheAuthorityCommandAndSystemRecoverWithoutStaleIdentity) + ".Second");
        var cache = new WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent>(readOnly: true);
        try
        {
            World.DefaultGameObjectInjectionWorld = firstWorld;
            Entity firstEntity = firstWorld.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
            Assert.IsTrue(cache.TryGetSingleton(firstWorld.EntityManager, out Entity firstResolved));
            Assert.AreEqual(firstEntity, firstResolved);

            firstWorld.Dispose();
            World.DefaultGameObjectInjectionWorld = secondWorld;
            Assert.IsFalse(cache.TryGetSingleton(secondWorld.EntityManager, out _));

            Entity replacement = secondWorld.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
            cache.Invalidate();
            Assert.IsTrue(cache.TryGetSingleton(secondWorld.EntityManager, out Entity replacementResolved));
            Assert.AreEqual(replacement, replacementResolved);

            secondWorld.EntityManager.DestroyEntity(replacement);
            Entity recovered = secondWorld.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
            Assert.IsTrue(cache.TryGetSingleton(secondWorld.EntityManager, out Entity recoveredResolved));
            Assert.AreEqual(recovered, recoveredResolved);

            Entity duplicate = secondWorld.EntityManager.CreateEntity(typeof(UnitMoveOrderQueueComponent));
            Assert.Throws<InvalidOperationException>(() => cache.TryGetSingleton(secondWorld.EntityManager, out _));
            secondWorld.EntityManager.DestroyEntity(duplicate);
            Assert.IsTrue(cache.TryGetSingleton(secondWorld.EntityManager, out Entity afterDuplicate));
            Assert.AreEqual(recovered, afterDuplicate);

            SystemHandle firstSystem = secondWorld.GetOrCreateSystem<ThreatDetectionWarningSystem>();
            SystemHandle secondSystem = secondWorld.GetOrCreateSystem<ThreatDetectionWarningSystem>();
            Assert.AreEqual(firstSystem, secondSystem);

            new BuildingPlacementValidationUtilitySystemHelperTests()
                .BuildingUiPlacementEconomyTransactionId_SurvivesQueueEntityRecreation();
        }
        finally
        {
            cache.Dispose();
            World.DefaultGameObjectInjectionWorld = previousDefault;
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            if (secondWorld.IsCreated)
                secondWorld.Dispose();
        }
    }
}
