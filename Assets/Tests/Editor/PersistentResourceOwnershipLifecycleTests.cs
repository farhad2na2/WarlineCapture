using System;
using System.Reflection;
using Game.Components;
using Game.Runtime;
using Game.Tactical.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public sealed class PersistentResourceOwnershipLifecycleTests
{
    public static void RunMenuStartupRuntimeSettingsOwnershipValidation()
    {
        try
        {
            var tests = new PersistentResourceOwnershipLifecycleTests();
            tests.MenuStartupRuntimeSettingsWorld_IsOwnedByEachMenuComposition();
            tests.MenuStartupRuntimeSettingsWorld_IsReleasedDuringShutdown();
            Debug.Log("[MenuStartupRuntimeSettingsOwnershipValidation] result=Passed tests=2");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[MenuStartupRuntimeSettingsOwnershipValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunUiGatewayLifecycleValidation()
    {
        try
        {
            var tests = new PersistentResourceOwnershipLifecycleTests();
            tests.UiGateway_SubsystemRegistrationReplacesStaleGateway();
            tests.UiGateway_WorldReplacementRebindsWithoutRetainingPreviousQueries();
            Debug.Log("[UiGatewayLifecycleValidation] result=Passed tests=2");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[UiGatewayLifecycleValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunScenarioLabWorldOwnershipValidation()
    {
        try
        {
            new PersistentResourceOwnershipLifecycleTests()
                .ScenarioLabPlayback_DoesNotRetainWorldOrQueryOwners();
            Debug.Log("[ScenarioLabWorldOwnershipValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[ScenarioLabWorldOwnershipValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunSelectionStartupWorldOwnershipValidation()
    {
        try
        {
            new PersistentResourceOwnershipLifecycleTests()
                .SelectionStartupQueries_RebindAfterWorldReplacement();
            Debug.Log("[SelectionStartupWorldOwnershipValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[SelectionStartupWorldOwnershipValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunSelectionBuildingInteractionWorldOwnershipValidation()
    {
        try
        {
            new PersistentResourceOwnershipLifecycleTests()
                .SelectionBuildingInteractionQueries_RebindAfterWorldReplacement();
            Debug.Log("[SelectionBuildingInteractionWorldOwnershipValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[SelectionBuildingInteractionWorldOwnershipValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunSelectionHudFeedbackWorldOwnershipValidation()
    {
        try
        {
            new PersistentResourceOwnershipLifecycleTests()
                .SelectionHudFeedbackState_RebindsAfterWorldReplacement();
            Debug.Log("[SelectionHudFeedbackWorldOwnershipValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[SelectionHudFeedbackWorldOwnershipValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunPathfindingPendingStateWorldReplacementValidation()
    {
        try
        {
            new PersistentResourceOwnershipLifecycleTests()
                .PathfindingPendingStateReader_FollowsReplacementWorld();
            Debug.Log("[PathfindingPendingStateWorldReplacementValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[PathfindingPendingStateWorldReplacementValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunSceneLifecycleWorldReplacementValidation()
    {
        try
        {
            new PersistentResourceOwnershipLifecycleTests()
                .SceneLifecycleQueue_RebindsAfterWorldReplacement();
            Debug.Log("[SceneLifecycleWorldReplacementValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[SceneLifecycleWorldReplacementValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunGameplayStartupCountsWorldReplacementValidation()
    {
        try
        {
            new PersistentResourceOwnershipLifecycleTests()
                .GameplayStartupCounts_RebindAfterWorldReplacement();
            Debug.Log("[GameplayStartupCountsWorldReplacementValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[GameplayStartupCountsWorldReplacementValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    public static void RunSelectionInputStateWorldReplacementValidation()
    {
        try
        {
            new PersistentResourceOwnershipLifecycleTests()
                .SelectionInputState_RebindsAfterWorldReplacement();
            Debug.Log("[SelectionInputStateWorldReplacementValidation] result=Passed tests=1");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError("[SelectionInputStateWorldReplacementValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [Test]
    public void SelectionInputState_RebindsAfterWorldReplacement()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var firstWorld = new World(nameof(SelectionInputState_RebindsAfterWorldReplacement) + "_First");
        var replacementWorld = new World(nameof(SelectionInputState_RebindsAfterWorldReplacement) + "_Replacement");
        var helper = new RtsSelectionInputStateCompositionSystemHelper();
        try
        {
            World.DefaultGameObjectInjectionWorld = firstWorld;
            Assert.IsTrue(helper.TryRead(out _, out RtsSelectionInputStateComponent firstState));
            firstState.QueuedMoveOrderFrame = 41;
            Assert.IsTrue(helper.TryWrite(firstState));
            Assert.IsTrue(helper.TryRead(out _, out firstState));
            Assert.AreEqual(41, firstState.QueuedMoveOrderFrame);

            firstWorld.Dispose();
            World.DefaultGameObjectInjectionWorld = replacementWorld;
            Assert.IsTrue(helper.TryRead(out EntityManager replacementEntityManager, out RtsSelectionInputStateComponent replacementState));

            Assert.AreEqual(-1, replacementState.QueuedMoveOrderFrame);
            Assert.IsTrue(helper.TryGetPointerRequests(out _, out DynamicBuffer<RtsSelectionPointerRequestElement> pointerRequests));
            Assert.AreEqual(0, pointerRequests.Length);
            using EntityQuery stateQuery = replacementEntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<RtsSelectionInputStateComponent>());
            Assert.AreEqual(1, stateQuery.CalculateEntityCount());
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            if (replacementWorld.IsCreated)
                replacementWorld.Dispose();
        }
    }

    [Test]
    public void GameplayStartupCounts_RebindAfterWorldReplacement()
    {
        var helper = new GameplayRuntimeUpdateCompositionSystemHelper();
        var firstWorld = new World(nameof(GameplayStartupCounts_RebindAfterWorldReplacement) + "_First");
        var replacementWorld = new World(nameof(GameplayStartupCounts_RebindAfterWorldReplacement) + "_Replacement");
        MethodInfo getCounts = typeof(GameplayRuntimeUpdateCompositionSystemHelper).GetMethod(
            "GetInitialSpawnCounts",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(getCounts);
        try
        {
            firstWorld.EntityManager.CreateEntity(typeof(InitialUnitsSpawnConfig));
            AssertCounts(helper, getCounts, firstWorld, expectedConfig: 1, expectedInitialized: 0, expectedProgress: 0);

            firstWorld.Dispose();
            replacementWorld.EntityManager.CreateEntity(
                typeof(InitialUnitsSpawnConfig),
                typeof(InitialUnitsSpawnInitialized),
                typeof(InitialUnitsSpawnProgress));
            AssertCounts(helper, getCounts, replacementWorld, expectedConfig: 1, expectedInitialized: 1, expectedProgress: 1);
        }
        finally
        {
            helper.Dispose();
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            if (replacementWorld.IsCreated)
                replacementWorld.Dispose();
        }
    }

    [Test]
    public void SceneLifecycleQueue_RebindsAfterWorldReplacement()
    {
        var helper = new SceneLifecycleSceneSystemHelper();
        var firstWorld = new World(nameof(SceneLifecycleQueue_RebindsAfterWorldReplacement) + "_First");
        var replacementWorld = new World(nameof(SceneLifecycleQueue_RebindsAfterWorldReplacement) + "_Replacement");
        try
        {
            Entity firstRoot = helper.EnsureLifecycleEntity(firstWorld.EntityManager);
            Assert.IsTrue(helper.QueueLoadMatch(firstWorld.EntityManager));
            Assert.AreEqual(
                1,
                firstWorld.EntityManager.GetBuffer<SceneLifecycleRequestElement>(firstRoot).Length);

            firstWorld.Dispose();
            Entity replacementRoot = helper.EnsureLifecycleEntity(replacementWorld.EntityManager);

            Assert.IsTrue(replacementWorld.EntityManager.Exists(replacementRoot));
            Assert.AreEqual(
                0,
                replacementWorld.EntityManager.GetBuffer<SceneLifecycleRequestElement>(replacementRoot).Length);
            Assert.IsTrue(helper.QueueLoadMatch(replacementWorld.EntityManager));
            Assert.AreEqual(
                1,
                replacementWorld.EntityManager.GetBuffer<SceneLifecycleRequestElement>(replacementRoot).Length);
        }
        finally
        {
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            if (replacementWorld.IsCreated)
                replacementWorld.Dispose();
        }
    }

    private static void AssertCounts(
        GameplayRuntimeUpdateCompositionSystemHelper helper,
        MethodInfo getCounts,
        World world,
        int expectedConfig,
        int expectedInitialized,
        int expectedProgress)
    {
        object[] arguments = { world, 0, 0, 0 };
        getCounts.Invoke(helper, arguments);
        Assert.AreEqual(expectedConfig, (int)arguments[1]);
        Assert.AreEqual(expectedInitialized, (int)arguments[2]);
        Assert.AreEqual(expectedProgress, (int)arguments[3]);
    }

    [Test]
    public void PathfindingPendingStateReader_FollowsReplacementWorld()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        var firstWorld = new World(nameof(PathfindingPendingStateReader_FollowsReplacementWorld) + "_First");
        var replacementWorld = new World(nameof(PathfindingPendingStateReader_FollowsReplacementWorld) + "_Replacement");
        var reader = new UnitPathfindingPendingStateReader();
        try
        {
            Entity firstState = firstWorld.EntityManager.CreateEntity(
                typeof(UnitPathfindingPendingStateComponent));
            firstWorld.EntityManager.SetComponentData(firstState, new UnitPathfindingPendingStateComponent
            {
                HasPendingPathJob = 1
            });
            World.DefaultGameObjectInjectionWorld = firstWorld;
            reader.Bind(firstWorld.EntityManager);
            Assert.IsTrue(reader.HasPendingPathJob());

            firstWorld.Dispose();
            Entity replacementState = replacementWorld.EntityManager.CreateEntity(
                typeof(UnitPathfindingPendingStateComponent));
            replacementWorld.EntityManager.SetComponentData(replacementState, new UnitPathfindingPendingStateComponent
            {
                HasPendingPathJob = 0
            });
            World.DefaultGameObjectInjectionWorld = replacementWorld;
            reader.Bind(replacementWorld.EntityManager);
            Assert.IsFalse(reader.HasPendingPathJob());

            replacementWorld.EntityManager.SetComponentData(replacementState, new UnitPathfindingPendingStateComponent
            {
                HasPendingPathJob = 1
            });
            Assert.IsTrue(reader.HasPendingPathJob());
        }
        finally
        {
            reader.Dispose();
            World.DefaultGameObjectInjectionWorld = previousWorld;
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            if (replacementWorld.IsCreated)
                replacementWorld.Dispose();
        }
    }

    [Test]
    public void RuntimeLogBuffer_SubsystemResetClearsStateAndAllowsReinitialization()
    {
        Type bufferType = typeof(MainMenuPlayUI).Assembly.GetType("Game.UI.Runtime.RuntimeLogBuffer", throwOnError: true);
        MethodInfo reset = bufferType.GetMethod(
            "ResetBeforeSubsystemRegistration",
            BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo initialize = bufferType.GetMethod(
            "InitializeBeforeSceneLoad",
            BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo initialized = bufferType.GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo entries = bufferType.GetField("Entries", BindingFlags.Static | BindingFlags.NonPublic);
        PropertyInfo count = entries.FieldType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);

        Assert.IsNotNull(reset);
        Assert.IsNotNull(initialize);
        Assert.IsNotNull(initialized);
        Assert.IsNotNull(entries);
        Assert.IsNotNull(count);

        try
        {
            reset.Invoke(null, null);
            initialize.Invoke(null, null);
            Assert.IsTrue((bool)initialized.GetValue(null));
            Assert.Greater((int)count.GetValue(entries.GetValue(null)), 0);

            reset.Invoke(null, null);
            Assert.IsFalse((bool)initialized.GetValue(null));
            Assert.AreEqual(0, (int)count.GetValue(entries.GetValue(null)));

            initialize.Invoke(null, null);
            Assert.IsTrue((bool)initialized.GetValue(null));
        }
        finally
        {
            reset.Invoke(null, null);
        }
    }

    [Test]
    public void UiGateway_SubsystemRegistrationReplacesStaleGateway()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World world = new(nameof(UiGateway_SubsystemRegistrationReplacesStaleGateway));
        try
        {
            CreateShellBoundary(world, UIRoute.MainMenu);
            World.DefaultGameObjectInjectionWorld = world;
            UiShellRuntimeGateway.Register(null);
            Assert.IsFalse(UiShellRuntimeGateway.TryReadShellState(out _));

            UiShellEcsGateway.RegisterAsRuntimeGateway();

            Assert.IsTrue(UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel state));
            Assert.AreEqual(UIRoute.MainMenu, state.ActiveRoute);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
        }
    }

    [Test]
    public void MenuStartupRuntimeSettingsWorld_IsOwnedByEachMenuComposition()
    {
        FieldInfo field = typeof(Game.Composition.MenuBootstrapCompositionSystemHelper).GetField(
            "startupRuntimeSettingsWorld",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        Assert.IsFalse(field.IsStatic);

        using World world = new(nameof(MenuStartupRuntimeSettingsWorld_IsOwnedByEachMenuComposition));
        var first = new Game.Composition.MenuBootstrapCompositionSystemHelper();
        var second = new Game.Composition.MenuBootstrapCompositionSystemHelper();
        field.SetValue(first, world);

        Assert.AreSame(world, field.GetValue(first));
        Assert.IsNull(field.GetValue(second));
    }

    [Test]
    public void MenuStartupRuntimeSettingsWorld_IsReleasedDuringShutdown()
    {
        FieldInfo field = typeof(Game.Composition.MenuBootstrapCompositionSystemHelper).GetField(
            "startupRuntimeSettingsWorld",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);

        using World world = new(nameof(MenuStartupRuntimeSettingsWorld_IsReleasedDuringShutdown));
        var composition = new Game.Composition.MenuBootstrapCompositionSystemHelper();
        field.SetValue(composition, world);

        composition.Shutdown(null);

        Assert.IsNull(field.GetValue(composition));
    }

    [Test]
    public void UiGateway_WorldReplacementRebindsWithoutRetainingPreviousQueries()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        World firstWorld = new(nameof(UiGateway_WorldReplacementRebindsWithoutRetainingPreviousQueries) + "_First");
        World secondWorld = new(nameof(UiGateway_WorldReplacementRebindsWithoutRetainingPreviousQueries) + "_Second");
        try
        {
            CreateShellBoundary(firstWorld, UIRoute.MainMenu);
            World.DefaultGameObjectInjectionWorld = firstWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            Assert.IsTrue(UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel first));
            Assert.AreEqual(UIRoute.MainMenu, first.ActiveRoute);

            CreateShellBoundary(secondWorld, UIRoute.Match);
            World.DefaultGameObjectInjectionWorld = secondWorld;
            Assert.IsTrue(UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel second));
            Assert.AreEqual(UIRoute.Match, second.ActiveRoute);

            firstWorld.Dispose();
            Assert.IsTrue(UiShellRuntimeGateway.TryReadShellState(out UiShellStateModel afterFirstWorldDisposal));
            Assert.AreEqual(UIRoute.Match, afterFirstWorldDisposal.ActiveRoute);
        }
        finally
        {
            World.DefaultGameObjectInjectionWorld = previousWorld;
            UiShellEcsGateway.RegisterAsRuntimeGateway();
            if (firstWorld.IsCreated)
                firstWorld.Dispose();
            if (secondWorld.IsCreated)
                secondWorld.Dispose();
        }
    }

    [Test]
    public void TacticalFollowQueryCache_DisposeIsIdempotentAndRejectsReuse()
    {
        using World world = new(nameof(TacticalFollowQueryCache_DisposeIsIdempotentAndRejectsReuse));
        var cache = new TacticalFollowCameraStateQueryCache();

        Assert.IsFalse(cache.HasValidPose(world.EntityManager));
        cache.Dispose();
        cache.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cache.HasValidPose(world.EntityManager));
    }

    [Test]
    public void SelectionShutdown_DisposesEveryTacticalFollowQueryOwner()
    {
        using World world = new(nameof(SelectionShutdown_DisposesEveryTacticalFollowQueryOwner));
        var runtimeCamera = new RtsSelectionRuntimeCameraSystemHelper();
        var selectionCamera = new SelectionUiCameraSystemHelper(null, null);
        var tacticalMode = new TacticalFollowCameraModeSystemHelper();
        TacticalFollowCameraStateQueryCache runtimeCache = ReadCache(
            runtimeCamera,
            "_tacticalFollowStateQueries");
        TacticalFollowCameraStateQueryCache selectionCache = ReadCache(
            selectionCamera,
            "_tacticalFollowCameraStateQueryCache");
        TacticalFollowCameraStateQueryCache modeCache = ReadCache(
            tacticalMode,
            "_stateQueryCache");

        Assert.IsFalse(runtimeCache.HasValidPose(world.EntityManager));
        Assert.IsFalse(selectionCache.HasValidPose(world.EntityManager));
        Assert.IsFalse(modeCache.HasValidPose(world.EntityManager));

        MethodInfo createDisposeAction = typeof(SelectionGameplayStartupSystemHelper).GetMethod(
            "CreateDisposeAction",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(createDisposeAction);
        var dispose = (Action)createDisposeAction.Invoke(
            null,
            new object[]
            {
                runtimeCamera,
                selectionCamera,
                tacticalMode,
                new SelectionOrderMarkerPresentationSystemHelper()
            });
        dispose.Invoke();
        dispose.Invoke();

        Assert.Throws<ObjectDisposedException>(() => runtimeCache.HasValidPose(world.EntityManager));
        Assert.Throws<ObjectDisposedException>(() => selectionCache.HasValidPose(world.EntityManager));
        Assert.Throws<ObjectDisposedException>(() => modeCache.HasValidPose(world.EntityManager));
    }

    [Test]
    public void SelectionStartupQueries_RebindAfterWorldReplacement()
    {
        World previousWorld = World.DefaultGameObjectInjectionWorld;
        using World firstWorld = new(nameof(SelectionStartupQueries_RebindAfterWorldReplacement) + "_First");
        using World replacementWorld = new(nameof(SelectionStartupQueries_RebindAfterWorldReplacement) + "_Replacement");
        SelectionGameplayStartupSystemHelper.Result result = default;
        try
        {
            World.DefaultGameObjectInjectionWorld = firstWorld;
            var startup = new SelectionGameplayStartupSystemHelper();
            result = startup.Initialize(
                null,
                null,
                null,
                null,
                null,
                null,
                default,
                null,
                default,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

            object closure = result.SelectionRuntimeUpdate.Target;
            Assert.IsNotNull(closure);
            MethodInfo ensureQueries = Array.Find(
                closure.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic),
                method => method.Name.Contains("EnsureSelectionRuntimeEntityQueries", StringComparison.Ordinal));
            FieldInfo queryWorld = closure.GetType().GetField(
                "selectionRuntimeQueryWorld",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(ensureQueries);
            Assert.IsNotNull(queryWorld);

            ensureQueries.Invoke(closure, new object[] { firstWorld.EntityManager });
            Assert.AreSame(firstWorld, queryWorld.GetValue(closure));

            World.DefaultGameObjectInjectionWorld = replacementWorld;
            ensureQueries.Invoke(closure, new object[] { replacementWorld.EntityManager });
            Assert.AreSame(replacementWorld, queryWorld.GetValue(closure));
        }
        finally
        {
            result.DisposeSelection?.Invoke();
            World.DefaultGameObjectInjectionWorld = previousWorld;
        }
    }

    [Test]
    public void SelectionBuildingInteractionQueries_RebindAfterWorldReplacement()
    {
        using World firstWorld = new(nameof(SelectionBuildingInteractionQueries_RebindAfterWorldReplacement) + "_First");
        using World replacementWorld = new(nameof(SelectionBuildingInteractionQueries_RebindAfterWorldReplacement) + "_Replacement");
        var helper = new SelectionBuildingInteractionCompositionSystemHelper();
        MethodInfo ensureQueries = typeof(SelectionBuildingInteractionCompositionSystemHelper).GetMethod(
            "EnsureEntityQueries",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo queryWorld = typeof(SelectionBuildingInteractionCompositionSystemHelper).GetField(
            "_queryWorld",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo gridConfigQuery = typeof(SelectionBuildingInteractionCompositionSystemHelper).GetField(
            "_gridConfigQuery",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(ensureQueries);
        Assert.IsNotNull(queryWorld);
        Assert.IsNotNull(gridConfigQuery);

        firstWorld.EntityManager.CreateEntity(typeof(GridConfig));
        ensureQueries.Invoke(helper, new object[] { firstWorld.EntityManager });
        Assert.AreSame(firstWorld, queryWorld.GetValue(helper));
        Assert.AreEqual(1, ((EntityQuery)gridConfigQuery.GetValue(helper)).CalculateEntityCount());

        replacementWorld.EntityManager.CreateEntity(typeof(GridConfig));
        replacementWorld.EntityManager.CreateEntity(typeof(GridConfig));
        ensureQueries.Invoke(helper, new object[] { replacementWorld.EntityManager });
        Assert.AreSame(replacementWorld, queryWorld.GetValue(helper));
        Assert.AreEqual(2, ((EntityQuery)gridConfigQuery.GetValue(helper)).CalculateEntityCount());
    }

    [Test]
    public void SelectionHudFeedbackState_RebindsAfterWorldReplacement()
    {
        using World firstWorld = new(nameof(SelectionHudFeedbackState_RebindsAfterWorldReplacement) + "_First");
        using World replacementWorld = new(nameof(SelectionHudFeedbackState_RebindsAfterWorldReplacement) + "_Replacement");
        EntityManager firstEntityManager = firstWorld.EntityManager;
        EntityManager replacementEntityManager = replacementWorld.EntityManager;
        var helper = new SelectionHudFeedbackUiSystemHelper();
        MethodInfo countSelected = typeof(SelectionHudFeedbackUiSystemHelper).GetMethod(
            "CountSelectedTagsCached",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(countSelected);

        firstEntityManager.CreateEntity(typeof(SelectedUnitTag));
        helper.QueueCommandMode(firstEntityManager, TacticalCommandMode.Move);
        Assert.AreEqual(1, (int)countSelected.Invoke(helper, new object[] { firstEntityManager }));

        replacementEntityManager.CreateEntity(typeof(SelectedUnitTag));
        replacementEntityManager.CreateEntity(typeof(SelectedUnitTag));
        helper.QueueCommandResult(
            replacementEntityManager,
            TacticalCommandResult.Success("Replacement match command."));
        Assert.AreEqual(2, (int)countSelected.Invoke(helper, new object[] { replacementEntityManager }));

        DynamicBuffer<SelectionHudFeedbackElement> firstFeedback = firstEntityManager.GetBuffer<SelectionHudFeedbackElement>(
            helper.EnsureFeedbackQueue(firstEntityManager));
        DynamicBuffer<SelectionHudFeedbackElement> replacementFeedback = replacementEntityManager.GetBuffer<SelectionHudFeedbackElement>(
            helper.EnsureFeedbackQueue(replacementEntityManager));
        Assert.AreEqual(1, firstFeedback.Length);
        Assert.AreEqual(SelectionHudFeedbackKind.CommandMode, firstFeedback[0].Kind);
        Assert.AreEqual(1, replacementFeedback.Length);
        Assert.AreEqual(SelectionHudFeedbackKind.CommandResult, replacementFeedback[0].Kind);
    }

    [Test]
    public void BuildingGameplayShutdown_DisposesResourceQueryCaches()
    {
        using World world = new(nameof(BuildingGameplayShutdown_DisposesResourceQueryCaches));
        var source = new BuildingGameplaySourceCompositionSystemHelper();
        GameObject buildingRoot = new("AM021_RuntimeBuildings");
        GameObject transportRoot = new("AM021_RuntimeTransports");
        SetField(source.BuildingPlacementStartupSystemHelper, "_buildingRoot", buildingRoot.transform);
        source.BuildingProductionTransportPresentationSystemHelper.SetRuntimeRoot(transportRoot.transform);
        FactionResourceCompositionSystemHelper factionResources = source.FactionResourceCompositionSystemHelper;
        BuildingResourceHaulerBridgeCompositionSystemHelper resourceHaulers =
            source.BuildingResourceHaulerBridgeCompositionSystemHelper;
        WorldScopedComponentQueryCache<BuildingResourceStorageComponent> storageCache = ReadCache<BuildingResourceStorageComponent>(
            factionResources,
            "_storageQueryCache");
        WorldScopedComponentQueryCache<UnitMoveOrderQueueComponent> moveOrderCache = ReadCache<UnitMoveOrderQueueComponent>(
            resourceHaulers,
            "_moveOrderQueueQueryCache");

        storageCache.Get(world.EntityManager);
        moveOrderCache.Get(world.EntityManager);

        new BuildingGameplayDisposalCompositionSystemHelper()
            .CreateDisposeAction(source, () => default)
            .Invoke();

        Assert.Throws<ObjectDisposedException>(() => storageCache.Get(world.EntityManager));
        Assert.Throws<ObjectDisposedException>(() => moveOrderCache.Get(world.EntityManager));
        Assert.IsTrue(buildingRoot == null, "Building gameplay shutdown must destroy its building presentation root.");
        Assert.IsTrue(transportRoot == null, "Building gameplay shutdown must destroy its transport presentation root.");
    }

    [Test]
    public void RuntimeCityShutdown_DestroysOwnedPresentationRoots()
    {
        GameObject runtimeRoot = new("AM021_RuntimeCity");
        var visualSystem = new RuntimeCityVisualPresentationSystemHelper();
        var mapSystem = new RuntimeCityRAndDMapCompositionSystemHelper();
        GameObject generatedRoot = new("AM021_GeneratedRuntimeCity");
        try
        {
            visualSystem.SetRuntimeRoot(runtimeRoot.transform);
            visualSystem.EnsureCityVisualRoot();
            Transform cityVisualRoot = runtimeRoot.transform.Find("RuntimeCityVisuals");
            Assert.IsNotNull(cityVisualRoot);

            SetField(mapSystem, "_generatedRoot", generatedRoot.transform);
            visualSystem.Dispose();
            mapSystem.Dispose();
            visualSystem.Dispose();
            mapSystem.Dispose();

            Assert.IsTrue(cityVisualRoot == null, "Runtime-city shutdown must destroy the visual root.");
            Assert.IsTrue(generatedRoot == null, "Runtime-city map shutdown must destroy the generated root.");
        }
        finally
        {
            if (runtimeRoot != null)
                UnityEngine.Object.DestroyImmediate(runtimeRoot);
            if (generatedRoot != null)
                UnityEngine.Object.DestroyImmediate(generatedRoot);
        }
    }

    [Test]
    public void ProcessVfxRootDestruction_ReleasesStaticOwnership()
    {
        DestroyNamedObject("MissileTrailVfxView");
        DestroyNamedObject("UnitAttackImpactVfxView");
        GameObject particlePrefab = new("AM021_ParticlePrefab");
        particlePrefab.AddComponent<ParticleSystem>();
        try
        {
            MissileTrailVfxView.Sync(Entity.Null, float3.zero, new float3(0f, 0f, 1f));
            UnitAttackImpactVfxView.Prewarm(particlePrefab, 1);
            GameObject missileRoot = GameObject.Find("MissileTrailVfxView");
            GameObject impactRoot = GameObject.Find("UnitAttackImpactVfxView");
            Assert.IsNotNull(missileRoot);
            Assert.IsNotNull(impactRoot);

            MissileTrailVfxView.ReleaseAll();
            UnitAttackImpactVfxView.ReleaseAll();

            Assert.IsNull(ReadStaticField(typeof(MissileTrailVfxView), "_instance"));
            Assert.IsNull(ReadStaticField(typeof(MissileTrailVfxView), "_smokeMaterial"));
            Assert.IsNull(ReadStaticField(typeof(MissileTrailVfxView), "_coreMaterial"));
            Assert.IsNull(ReadStaticField(typeof(UnitAttackImpactVfxView), "_instance"));
            Assert.IsTrue(missileRoot == null);
            Assert.IsTrue(impactRoot == null);
        }
        finally
        {
            DestroyNamedObject("MissileTrailVfxView");
            DestroyNamedObject("UnitAttackImpactVfxView");
            if (particlePrefab != null)
                UnityEngine.Object.DestroyImmediate(particlePrefab);
        }
    }

    [Test]
    public void ScenarioLabGrid_DoesNotRetainDuplicateNativeContainerOwners()
    {
        string[] removedMirrorFields =
        {
            "scenarioGridBlockerCounts",
            "scenarioGridBlocked",
            "scenarioGridOccupied",
            "scenarioGridFriendlyPassFactionIds",
            "scenarioGridPathPool"
        };

        for (int i = 0; i < removedMirrorFields.Length; i++)
        {
            Assert.IsNull(
                typeof(BattleScenarioLabVisualPlayback).GetField(
                    removedMirrorFields[i],
                    BindingFlags.Instance | BindingFlags.NonPublic),
                $"Scenario Lab must not mirror ECS-owned native container {removedMirrorFields[i]}.");
        }
    }

    [Test]
    public void ScenarioLabPlayback_DoesNotRetainWorldOrQueryOwners()
    {
        FieldInfo[] fields = typeof(BattleScenarioLabVisualPlayback).GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < fields.Length; i++)
        {
            Type fieldType = fields[i].FieldType;
            Assert.IsFalse(
                fieldType == typeof(World) ||
                fieldType == typeof(EntityManager) ||
                fieldType == typeof(EntityQuery),
                $"Scenario Lab playback must resolve the active World at the action boundary instead of retaining {fields[i].Name}.");
        }
    }

    private static void CreateShellBoundary(World world, UIRoute route)
    {
        Entity entity = world.EntityManager.CreateEntity(
            typeof(UiShellRootComponent),
            typeof(UiShellStateComponent));
        world.EntityManager.SetComponentData(entity, new UiShellStateComponent
        {
            ActiveRoute = route,
            CurrentMode = route == UIRoute.Match ? UiShellMode.MatchHud : UiShellMode.MainMenu,
            Phase = route == UIRoute.Match ? UiShellTransitionPhase.MatchHudReady : UiShellTransitionPhase.MenuReady
        });
    }

    private static TacticalFollowCameraStateQueryCache ReadCache(object owner, string fieldName)
    {
        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing query-cache owner field {owner.GetType().Name}.{fieldName}.");
        return (TacticalFollowCameraStateQueryCache)field.GetValue(owner);
    }

    private static WorldScopedComponentQueryCache<T> ReadCache<T>(object owner, string fieldName)
        where T : unmanaged, IComponentData
    {
        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing query-cache owner field {owner.GetType().Name}.{fieldName}.");
        return (WorldScopedComponentQueryCache<T>)field.GetValue(owner);
    }

    private static object ReadStaticField(Type owner, string fieldName)
    {
        FieldInfo field = owner.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing static ownership field {owner.Name}.{fieldName}.");
        return field.GetValue(null);
    }

    private static void SetField(object owner, string fieldName, object value)
    {
        FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing lifecycle owner field {owner.GetType().Name}.{fieldName}.");
        field.SetValue(owner, value);
    }

    private static void DestroyNamedObject(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
            UnityEngine.Object.DestroyImmediate(target);
    }
}
