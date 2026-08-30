#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Editor;
using Game.Missions.Contracts;
using Game.Narrative.Contracts;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Game.UI.Shell.Ecs;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

public sealed class M02EstablishBaseLaunchTests
{
    private const string Marker = "[M02EstablishBaseLaunchValidation] result=Passed tests=18";
    private const string M01MissionPath =
        "Assets/Game/Configs/Missions/Chapter01/MissionDefinition_Ch01_M01_FirstContact.asset";
    private const string M01ScenarioPath =
        "Assets/Game/Configs/Scenarios/Chapter01/ScenarioSetup_Ch01_M01_FirstContact.asset";
    private const string M02MissionId = "saga.ch01.m02.establish_base";
    private const string M02ScenarioId = "scenario.ch01.m02.establish_base";
    private const string M02MapId = "opmap.ch01.forward_post_01";
    private const int M02Seed = 2002001;

    [MenuItem("Game/Validation/Run M02 Establish Base Launch Focused")]
    public static void RunFocusedValidation()
    {
        try
        {
            M02EstablishBaseLaunchTests tests = new();
            tests.ChapterCatalogProjectsCanonicalM01AndM02();
            tests.CatalogEntryWithoutScenarioFailsClosed();
            tests.DuplicateCanonicalScenarioFailsClosed();
            tests.SameVersionCatalogContentChangeReprojects();
            tests.ChapterCatalogDoesNotFallBackToLegacyMission();
            tests.CampaignDeployQueuesCanonicalM02PayloadAndRoute();
            tests.CompletedChapterDefaultsToLatestAvailableM02();
            tests.CompletedM02ReplayKeepsRequiredTutorialGuidance();
            tests.IncompleteM02RetryKeepsFullTutorialGuidance();
            tests.CampaignDeployBootstrapsForwardPostAndAccepts();
            tests.ReusedCampaignMapPublishesItsActualGeneration();
            tests.M02RetryPreservesSeedAndIncrementsAttemptIdentity();
            tests.M02RetryLaunchClearsPriorAttemptState();
            tests.M02ReplayUsesCanonicalIdentityAndSeed();
            tests.M02LaunchPreservesCallerSeedAndRejectsWrongMap();
            tests.M01LaunchFromChapterCatalogRemainsUnchanged();
            tests.SwitchingFromM01ToM02AdvancesTheSoleMapGeneration();
            tests.MenuBootstrapRebindsAfterWorldRecreation();
            Debug.Log(Marker);
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseLaunchValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    [MenuItem("Game/Validation/Run M02 Establish Base Launch Regressions")]
    public static void RunRegressionValidation()
    {
        try
        {
            RunValidation(M02EstablishBaseContractValidation.RunFocusedValidation);
            RunValidation(M01FirstContactContractValidation.RunFocusedValidation);
            RunValidation(ProductionSourceGrowthArchitectureTests.RunFocusedValidation);
            Debug.Log("[M02EstablishBaseLaunchRegressionValidation] result=Passed suites=3");
            ValidationExit.Passed();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.Log("[M02EstablishBaseLaunchRegressionValidation] result=Failed");
            ValidationExit.Failed();
        }
    }

    private static void RunValidation(Action validation)
    {
        ValidationExit.ClearLastExitCode();
        using (ValidationExit.SuppressProcessExit())
            validation();
        if (ValidationExit.LastExitCode is int exitCode && exitCode != 0)
            throw new InvalidOperationException(
                $"{validation.Method.DeclaringType?.Name}.{validation.Method.Name} failed validation.");
    }

    [Test]
    public void ChapterCatalogDoesNotFallBackToLegacyMission()
    {
        MissionDefinitionConfig m01Mission =
            AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(M01MissionPath);
        ScenarioSetupConfig m01Scenario =
            AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(M01ScenarioPath);
        MissionDefinitionConfig m02Mission =
            AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(M02EstablishBaseConfigBuilder.MissionPath);
        ScenarioSetupConfig m02Scenario =
            AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(M02EstablishBaseConfigBuilder.ScenarioPath);
        MissionDefinitionCatalogConfig m02Only =
            ScriptableObject.CreateInstance<MissionDefinitionCatalogConfig>();
        SetField(m02Only, "entries", new[]
        {
            new MissionDefinitionCatalogEntryConfig(m02Mission.MissionId, m02Mission, m02Scenario)
        });

        using World world = new("m02-catalog-authority");
        World previous = World.DefaultGameObjectInjectionWorld;
        GameObject menuObject = new("M02 Catalog Authority Bootstrap");
        menuObject.SetActive(false);
        CampaignMissionMenuBootstrapRuntime bootstrap = new();
        try
        {
            LoadChapter(out _, out OperationMapCatalogConfig maps);
            World.DefaultGameObjectInjectionWorld = world;
            MenuBootstrapView view = menuObject.AddComponent<MenuBootstrapView>();
            view.Configure(null, null, null, null, null, null,
                configuredCampaignMissionDefinition: m01Mission,
                configuredCampaignScenarioSetup: m01Scenario,
                configuredCampaignOperationMapCatalog: maps,
                configuredCampaignMissionCatalog: m02Only);
            bootstrap.Update(view);
            using EntityQuery missionQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<CampaignMissionRootComponent>());
            Assert.AreEqual(1, missionQuery.CalculateEntityCount());
            Entity root = missionQuery.GetSingletonEntity();
            world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root).Add(
                Request(
                    new FixedString64Bytes(MissionDefinitionContractValidation.FirstContactMissionId),
                    new FixedString64Bytes(m01Scenario.ScenarioId),
                    new FixedString64Bytes(m01Mission.OperationMapId),
                    FirstLaunchMissionHandoffOperation.DeterministicSeed,
                    MissionRunKind.FirstClear,
                    "campaign-m01-not-in-catalog",
                    0,
                    202));

            bootstrap.Update(view);
            using EntityQuery mapQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>());
            Assert.AreEqual(0, mapQuery.CalculateEntityCount(),
                "A chapter-catalog miss must not create a map through the legacy M01 fallback.");
            DisposeCatalog(world.EntityManager, root);
        }
        finally
        {
            bootstrap.Shutdown();
            World.DefaultGameObjectInjectionWorld = previous;
            UnityEngine.Object.DestroyImmediate(menuObject);
            UnityEngine.Object.DestroyImmediate(m02Only);
        }
    }

    [Test]
    public void ChapterCatalogProjectsCanonicalM01AndM02()
    {
        using World world = new("m02-chapter-catalog");
        Assert.IsTrue(ProjectChapter(world.EntityManager, 11, out Entity root, out string error), error);
        CampaignMissionCatalogComponent catalog =
            world.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        Assert.AreEqual(2, catalog.Blob.Value.Missions.Length);
        Assert.AreEqual(MissionDefinitionContractValidation.FirstContactMissionId,
            catalog.Blob.Value.Missions[0].MissionId.ToString());
        Assert.AreEqual(M02MissionId, catalog.Blob.Value.Missions[1].MissionId.ToString());
        Assert.AreEqual(M02ScenarioId, catalog.Blob.Value.Missions[1].ScenarioId.ToString());
        Assert.AreEqual(M02MapId, catalog.Blob.Value.Missions[1].OperationMapId.ToString());
        Assert.AreEqual(M02Seed, catalog.Blob.Value.Missions[1].DeterministicSeed);
        Assert.AreEqual("Building_Barrack",
            catalog.Blob.Value.Missions[1].Objectives[0].TargetConfigId.ToString());
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void CatalogEntryWithoutScenarioFailsClosed()
    {
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(M01MissionPath);
        MissionDefinitionCatalogConfig incomplete = ScriptableObject.CreateInstance<MissionDefinitionCatalogConfig>();
        try
        {
            SetField(incomplete, "entries", new[]
            {
                new MissionDefinitionCatalogEntryConfig(mission.MissionId, mission)
            });
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateCatalog(incomplete, out string catalogError));
            StringAssert.Contains("canonical scenario", catalogError);
            LoadChapter(out _, out OperationMapCatalogConfig maps);
            using World world = new("m02-missing-scenario");
            Assert.IsFalse(CampaignMissionCatalogProjection.TryProject(
                world.EntityManager, incomplete, maps, 1, out Entity root, out string error));
            Assert.AreEqual(Entity.Null, root);
            StringAssert.Contains("canonical scenario", error);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(incomplete);
        }
    }

    [Test]
    public void DuplicateCanonicalScenarioFailsClosed()
    {
        MissionDefinitionConfig m01Mission =
            AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(M01MissionPath);
        ScenarioSetupConfig m01Scenario =
            AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(M01ScenarioPath);
        ScenarioSetupConfig m02Scenario = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            M02EstablishBaseConfigBuilder.ScenarioPath);
        MissionDefinitionCatalogConfig mismatched =
            ScriptableObject.CreateInstance<MissionDefinitionCatalogConfig>();
        ScenarioSetupConfig duplicate = UnityEngine.Object.Instantiate(m01Scenario);
        try
        {
            SetField(mismatched, "entries", new[]
            {
                new MissionDefinitionCatalogEntryConfig(m01Mission.MissionId, m01Mission, m02Scenario)
            });
            Assert.IsFalse(MissionDefinitionContractValidation.TryValidateCatalog(mismatched, out string error));
            StringAssert.Contains("scenario identity", error);

            MethodInfo resolver = typeof(M01FirstContactConfigBuilder).GetMethod(
                "ResolveScenario", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(resolver);
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() =>
                resolver.Invoke(null, new object[]
                {
                    new[] { m01Scenario, duplicate }, m01Scenario.ScenarioId
                }));
            Assert.IsInstanceOf<InvalidOperationException>(exception.InnerException);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(duplicate);
            UnityEngine.Object.DestroyImmediate(mismatched);
        }
    }

    [Test]
    public void SameVersionCatalogContentChangeReprojects()
    {
        MissionDefinitionConfig mission = AssetDatabase.LoadAssetAtPath<MissionDefinitionConfig>(
            M02EstablishBaseConfigBuilder.MissionPath);
        ScenarioSetupConfig canonical = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
            M02EstablishBaseConfigBuilder.ScenarioPath);
        ScenarioSetupConfig scenario = UnityEngine.Object.Instantiate(canonical);
        MissionDefinitionCatalogConfig missions =
            ScriptableObject.CreateInstance<MissionDefinitionCatalogConfig>();
        Entity root = Entity.Null;
        using World world = new("m02-same-version-content-change");
        try
        {
            SetField(missions, "entries", new[]
            {
                new MissionDefinitionCatalogEntryConfig(mission.MissionId, mission, scenario)
            });
            LoadChapter(out _, out OperationMapCatalogConfig maps);
            Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
                world.EntityManager, missions, maps, 17, out root, out string error), error);
            Assert.AreEqual(M02Seed, world.EntityManager
                .GetComponentData<CampaignMissionCatalogComponent>(root)
                .Blob.Value.Missions[0].DeterministicSeed);

            SetField(scenario, "deterministicSeed", M02Seed + 17);
            Assert.IsTrue(CampaignMissionCatalogProjection.TryProject(
                world.EntityManager, missions, maps, 17, out root, out error), error);
            Assert.AreEqual(M02Seed + 17, world.EntityManager
                .GetComponentData<CampaignMissionCatalogComponent>(root)
                .Blob.Value.Missions[0].DeterministicSeed);
        }
        finally
        {
            DisposeCatalog(world.EntityManager, root);
            UnityEngine.Object.DestroyImmediate(missions);
            UnityEngine.Object.DestroyImmediate(scenario);
        }
    }

    [Test]
    public void CampaignDeployQueuesCanonicalM02PayloadAndRoute()
    {
        using ProjectionFixture fixture = CreateProjectionFixture(m02Completed: false);
        Entity settings = fixture.World.EntityManager.CreateEntity(typeof(AssistantSettingsComponent));
        fixture.World.EntityManager.SetComponentData(settings, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.Minimal
        });
        CampaignMissionLaunchRequestElement request = QueueM02Deploy(fixture.World, fixture.UiRoot);
        Assert.AreEqual(M02MissionId, request.MissionId.ToString());
        Assert.AreEqual(M02ScenarioId, request.ScenarioId.ToString());
        Assert.AreEqual(M02MapId, request.OperationMapId.ToString());
        Assert.AreEqual(MissionLaunchOriginKind.CampaignOperations, request.LaunchOrigin);
        Assert.AreEqual(MissionRunKind.FirstClear, request.RunKind);
        Assert.AreEqual(NarrativeGuidanceMode.Full, request.Guidance,
            "The first-clear M2 tutorial must not inherit a saved minimal-guidance preference.");
        Assert.AreEqual(M02Seed, request.DeterministicSeed);
        StringAssert.StartsWith("campaign-m02-", request.SessionToken.ToString());
        DynamicBuffer<UiShellRouteRequestComponent> routes =
            fixture.World.EntityManager.GetBuffer<UiShellRouteRequestComponent>(fixture.UiRoot);
        Assert.AreEqual(1, routes.Length);
        Assert.AreEqual(UiShellRouteIntent.EnterMatch, routes[0].Intent);
        Assert.AreEqual(UIRoute.Match, routes[0].Route);
    }

    [Test]
    public void IncompleteM02RetryKeepsFullTutorialGuidance()
    {
        using ProjectionFixture fixture = CreateProjectionFixture(m02Completed: false, pendingResume: true);
        Entity settings = fixture.World.EntityManager.CreateEntity(typeof(AssistantSettingsComponent));
        fixture.World.EntityManager.SetComponentData(settings, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.Minimal
        });
        CampaignMissionLaunchRequestElement request = QueueM02Deploy(fixture.World, fixture.UiRoot);
        Assert.AreEqual(MissionRunKind.Retry, request.RunKind);
        Assert.AreEqual(NarrativeGuidanceMode.Full, request.Guidance,
            "An incomplete M2 retry must retain the required build tutorial.");
    }

    [Test]
    public void CompletedChapterDefaultsToLatestAvailableM02()
    {
        using ProjectionFixture fixture = CreateProjectionFixture(m02Completed: true);
        UpdateProjection(fixture.World);

        UiCampaignOperationsComponent operations = fixture.World.EntityManager
            .GetComponentData<UiCampaignOperationsComponent>(fixture.UiRoot);
        Assert.AreEqual(M02MissionId, operations.SelectedMissionId.ToString());
        Assert.AreEqual(M02MapId, operations.OperationMapId.ToString());
        Assert.AreEqual(UiCampaignMissionPrimaryActionKind.Replay, operations.PrimaryAction);
    }

    [Test]
    public void CompletedM02ReplayKeepsRequiredTutorialGuidance()
    {
        using ProjectionFixture fixture = CreateProjectionFixture(m02Completed: true);
        Entity settings = fixture.World.EntityManager.CreateEntity(typeof(AssistantSettingsComponent));
        fixture.World.EntityManager.SetComponentData(settings, new AssistantSettingsComponent
        {
            GuidanceLevel = AssistantGuidanceLevel.Minimal
        });

        CampaignMissionLaunchRequestElement request = QueueM02Deploy(fixture.World, fixture.UiRoot);
        using EntityQuery briefingQuery = fixture.World.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UiMissionBriefingComponent>());
        UiMissionBriefingComponent briefing = briefingQuery.GetSingleton<UiMissionBriefingComponent>();
        Assert.AreEqual(MissionRunKind.Replay, request.RunKind);
        Assert.AreEqual(NarrativeGuidanceMode.Full, request.Guidance);
        Assert.AreEqual(1, request.ReplayTutorialEnabled);
        Assert.AreEqual(1, briefing.ReplayTutorialEnabled);
        Assert.AreEqual(0, briefing.ReplayTutorialToggleVisible);
    }

    [Test]
    public void CampaignDeployBootstrapsForwardPostAndAccepts()
    {
        using ProjectionFixture fixture = CreateProjectionFixture(m02Completed: false);
        QueueM02Deploy(fixture.World, fixture.UiRoot);
        LoadChapter(out MissionDefinitionCatalogConfig missions, out OperationMapCatalogConfig maps);
        World previous = World.DefaultGameObjectInjectionWorld;
        GameObject menuObject = new("M02 Campaign Bootstrap");
        menuObject.SetActive(false);
        CampaignMissionMenuBootstrapRuntime bootstrap = new();
        try
        {
            World.DefaultGameObjectInjectionWorld = fixture.World;
            MenuBootstrapView view = menuObject.AddComponent<MenuBootstrapView>();
            view.Configure(null, null, null, null, null, null,
                configuredCampaignOperationMapCatalog: maps,
                configuredCampaignMissionCatalog: missions);
            bootstrap.Update(view);

            Assert.IsTrue(CampaignMissionOperationMapLaunchResolver.TryResolve(
                fixture.World,
                "skirmish",
                "scenario.skirmish.desert_base_standard",
                "opmap.skirmish.desert_base_01",
                out OperationMapLaunchSelection launchSelection,
                out OperationMapLoadResultCode launchFailureCode,
                out string launchError), launchError);
            Assert.AreEqual(OperationMapLoadResultCode.None, launchFailureCode);
            Assert.IsTrue(launchSelection.IsCampaign);
            Assert.AreEqual(M02MissionId, launchSelection.MissionId.ToString());
            Assert.AreEqual(M02ScenarioId, launchSelection.ScenarioId.ToString());
            Assert.AreEqual(M02MapId, launchSelection.OperationMapId.ToString());
            Assert.AreEqual(M02MapId, launchSelection.Definition.OperationMapId);
            Assert.AreEqual("dad0bd13fb20943dfb2f881cbe225f05",
                launchSelection.Definition.SourceSceneReference.AssetGUID);

            using EntityQuery mapQuery = fixture.World.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ActiveOperationMapComponent>(),
                ComponentType.ReadOnly<OperationMapReadinessComponent>());
            Assert.AreEqual(1, mapQuery.CalculateEntityCount());
            Entity mapRoot = mapQuery.GetSingletonEntity();
            ActiveOperationMapComponent active =
                fixture.World.EntityManager.GetComponentData<ActiveOperationMapComponent>(mapRoot);
            Assert.AreEqual(M02MapId, active.OperationMapId.ToString());
            OperationMapReadinessComponent readiness =
                fixture.World.EntityManager.GetComponentData<OperationMapReadinessComponent>(mapRoot);
            readiness.ReadyFlags = readiness.RequiredFlags;
            fixture.World.EntityManager.SetComponentData(mapRoot, readiness);

            UpdateLaunch(fixture.World);
            CampaignMissionRuntimeComponent runtime =
                fixture.World.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(fixture.MissionRoot);
            Assert.AreEqual(M02MissionId, runtime.MissionId.ToString());
            Assert.AreEqual(M02MapId, runtime.OperationMapId.ToString());
            Assert.AreEqual(MissionPhaseKind.Preparing, runtime.Phase);
            Assert.AreEqual(M02Seed, runtime.DeterministicSeed);
            Assert.AreEqual(1, fixture.World.EntityManager
                .GetBuffer<CampaignMissionLaunchResultElement>(fixture.MissionRoot)[0].Accepted);
        }
        finally
        {
            bootstrap.Shutdown();
            UnityEngine.Object.DestroyImmediate(menuObject);
            World.DefaultGameObjectInjectionWorld = previous;
        }
    }

    [Test]
    public void ReusedCampaignMapPublishesItsActualGeneration()
    {
        using World world = new("m02-reused-campaign-generation");
        Assert.IsTrue(ProjectChapter(world.EntityManager, 1, out Entity missionRoot, out string error), error);
        CampaignMissionLaunchRequestElement request = Request(
            MissionRunKind.Replay, "campaign-m02-generation", attempt: 2, transition: 505);
        world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot).Add(request);
        LoadChapter(out _, out OperationMapCatalogConfig maps);
        Assert.IsTrue(maps.TryResolve(M02MapId, out OperationMapDefinition definition));
        FixedString64Bytes scenarioId = new(M02ScenarioId);
        FixedString64Bytes missionId = new(M02MissionId);
        OperationMapReadinessFlags initial = OperationMapReadinessFlags.Metadata;
        OperationMapReadinessFlags required = AllReadiness();

        using OperationMapRuntimeBootstrapSceneSystemHelper menuBootstrap = new(world);
        using OperationMapRuntimeBootstrapSceneSystemHelper matchBootstrap = new(world);
        Assert.IsTrue(menuBootstrap.TryPublish(
            definition, in scenarioId, in missionId, 2, initial, required,
            out Entity menuRoot, out string menuError), menuError);
        Assert.AreEqual(2, menuBootstrap.PublishedGeneration);
        Assert.IsTrue(matchBootstrap.TryPublish(
            definition, in scenarioId, in missionId, 1, initial, required,
            out Entity reusedRoot, out string matchError), matchError);
        Assert.AreEqual(menuRoot, reusedRoot);
        Assert.AreEqual(2, matchBootstrap.PublishedGeneration);
        Assert.IsTrue(matchBootstrap.TryUpdateReadiness(
            matchBootstrap.PublishedGeneration,
            required,
            OperationMapReadinessFlags.None,
            out string readinessError), readinessError);
        Assert.AreEqual(2, world.EntityManager
            .GetComponentData<OperationMapReadinessComponent>(reusedRoot).Generation);

        DisposeCatalog(world.EntityManager, missionRoot);
    }

    [Test]
    public void M02RetryPreservesSeedAndIncrementsAttemptIdentity()
    {
        using World world = new("m02-retry");
        Assert.IsTrue(ProjectChapter(world.EntityManager, 1, out Entity root, out string error), error);
        CampaignMissionRuntimeComponent runtime = Runtime(
            MissionRunKind.FirstClear, "campaign-m02-retry", attempt: 2, transition: 303);
        runtime.Phase = MissionPhaseKind.Result;
        runtime.Outcome = MissionOutcomeKind.Defeat;
        world.EntityManager.SetComponentData(root, runtime);
        world.EntityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Add(new()
        {
            Action = MissionActionKind.Retry,
            TransitionToken = runtime.TransitionToken,
            SessionToken = runtime.SessionToken,
            AttemptOrdinal = runtime.AttemptOrdinal
        });

        Assert.IsTrue(CampaignMissionRuntimeSystem.TryConsumeAction(world.EntityManager, root));
        CampaignMissionLaunchRequestElement retry =
            world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root)[0];
        Assert.AreEqual(runtime.SessionToken, retry.SessionToken);
        Assert.AreEqual(runtime.TransitionToken + 1, retry.TransitionToken);
        Assert.AreEqual(runtime.AttemptOrdinal + 1, retry.AttemptOrdinal);
        Assert.AreEqual(runtime.DeterministicSeed, retry.DeterministicSeed);
        Assert.AreEqual(MissionRunKind.Retry, retry.RunKind);
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void M02RetryLaunchClearsPriorAttemptState()
    {
        CampaignMissionLaunchRequestElement request = Request(
            MissionRunKind.Retry, "campaign-m02-cleanup", attempt: 3, transition: 404);
        using World world = CreateLaunchWorld(request, out Entity root, out _);
        Entity stale = world.EntityManager.CreateEntity(typeof(CampaignMissionUnitRoleComponent));
        world.EntityManager.SetComponentData(root, new CampaignMissionAttemptFactsComponent
        {
            ElapsedMilliseconds = 1234,
            HostileDefeatedCount = 2
        });
        world.EntityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Add(default);
        CampaignMissionGuidanceProjectionComponent guidance = default;
        guidance.Active = 1;
        world.EntityManager.SetComponentData(root, guidance);
        world.EntityManager.AddComponentData(root, new CampaignMissionResultComponent
        {
            Outcome = MissionOutcomeKind.Defeat
        });

        UpdateLaunch(world);
        Assert.IsFalse(world.EntityManager.Exists(stale));
        Assert.AreEqual(0, world.EntityManager
            .GetComponentData<CampaignMissionAttemptFactsComponent>(root).ElapsedMilliseconds);
        Assert.AreEqual(0, world.EntityManager.GetBuffer<CampaignMissionActionRequestElement>(root).Length);
        Assert.AreEqual(0, world.EntityManager
            .GetComponentData<CampaignMissionGuidanceProjectionComponent>(root).Active);
        Assert.AreEqual(MissionOutcomeKind.None,
            world.EntityManager.GetComponentData<CampaignMissionResultComponent>(root).Outcome);
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void M02ReplayUsesCanonicalIdentityAndSeed()
    {
        using ProjectionFixture fixture = CreateProjectionFixture(m02Completed: true);
        CampaignMissionLaunchRequestElement request = QueueM02Deploy(fixture.World, fixture.UiRoot);
        Assert.AreEqual(MissionRunKind.Replay, request.RunKind);
        Assert.AreEqual(M02MissionId, request.MissionId.ToString());
        Assert.AreEqual(M02Seed, request.DeterministicSeed);
        StringAssert.StartsWith("campaign-m02-", request.SessionToken.ToString());
    }

    [Test]
    public void M02LaunchPreservesCallerSeedAndRejectsWrongMap()
    {
        CampaignMissionLaunchRequestElement callerSeed = Request(
            MissionRunKind.FirstClear, "campaign-m02-caller-seed", 1, 505);
        callerSeed.DeterministicSeed++;
        CampaignMissionRuntimeComponent runtime = RunAccepted(callerSeed);
        Assert.AreEqual(callerSeed.DeterministicSeed, runtime.DeterministicSeed);

        CampaignMissionLaunchRequestElement wrongMap = Request(
            MissionRunKind.FirstClear, "campaign-m02-wrong-map", 1, 506);
        using World world = CreateLaunchWorld(wrongMap, out Entity root, out Entity mapRoot);
        ActiveOperationMapComponent active = world.EntityManager.GetComponentData<ActiveOperationMapComponent>(mapRoot);
        active.OperationMapId = new FixedString64Bytes("opmap.ch01.district_edge_01");
        world.EntityManager.SetComponentData(mapRoot, active);
        UpdateLaunch(world);
        Assert.AreEqual("operation-map-mismatch", world.EntityManager
            .GetBuffer<CampaignMissionLaunchResultElement>(root)[0].ReasonCode.ToString());
        DisposeCatalog(world.EntityManager, root);
    }

    [Test]
    public void M01LaunchFromChapterCatalogRemainsUnchanged()
    {
        using World source = new("m01-chapter-source");
        Assert.IsTrue(ProjectChapter(source.EntityManager, 1, out Entity sourceRoot, out string error), error);
        CampaignMissionCatalogComponent catalog =
            source.EntityManager.GetComponentData<CampaignMissionCatalogComponent>(sourceRoot);
        ref CampaignMissionDefinitionBlob definition = ref catalog.Blob.Value.Missions[0];
        CampaignMissionLaunchRequestElement request = Request(
            definition.MissionId, definition.ScenarioId, definition.OperationMapId,
            104729, MissionRunKind.FirstClear,
            "campaign-m01-compat", 0, 707);
        DisposeCatalog(source.EntityManager, sourceRoot);

        CampaignMissionRuntimeComponent runtime = RunAccepted(request);
        Assert.AreEqual(MissionDefinitionContractValidation.FirstContactMissionId, runtime.MissionId.ToString());
        Assert.AreEqual(request.ScenarioId, runtime.ScenarioId);
        Assert.AreEqual(request.OperationMapId, runtime.OperationMapId);
        Assert.AreEqual(request.DeterministicSeed, runtime.DeterministicSeed);
    }

    [Test]
    public void MenuBootstrapRebindsAfterWorldRecreation()
    {
        LoadChapter(out MissionDefinitionCatalogConfig missions, out OperationMapCatalogConfig maps);
        GameObject menuObject = new("M02 World Rebind Bootstrap");
        menuObject.SetActive(false);
        MenuBootstrapView view = menuObject.AddComponent<MenuBootstrapView>();
        view.Configure(null, null, null, null, null, null,
            configuredCampaignOperationMapCatalog: maps,
            configuredCampaignMissionCatalog: missions);
        CampaignMissionMenuBootstrapRuntime bootstrap = new();
        World previous = World.DefaultGameObjectInjectionWorld;
        try
        {
            using (World first = new("m02-bootstrap-world-a"))
            {
                World.DefaultGameObjectInjectionWorld = first;
                bootstrap.Update(view);
                AssertSingleCampaignRoot(first.EntityManager, out Entity root);
                DisposeCatalog(first.EntityManager, root);
                bootstrap.Shutdown();
            }
            using (World second = new("m02-bootstrap-world-b"))
            {
                World.DefaultGameObjectInjectionWorld = second;
                bootstrap.Update(view);
                AssertSingleCampaignRoot(second.EntityManager, out Entity root);
                DisposeCatalog(second.EntityManager, root);
                bootstrap.Shutdown();
            }
        }
        finally
        {
            bootstrap.Shutdown();
            World.DefaultGameObjectInjectionWorld = previous;
            UnityEngine.Object.DestroyImmediate(menuObject);
        }
    }

    [Test]
    public void SwitchingFromM01ToM02AdvancesTheSoleMapGeneration()
    {
        LoadChapter(out MissionDefinitionCatalogConfig missions, out OperationMapCatalogConfig maps);
        using World world = new("m02-bootstrap-map-switch");
        World previous = World.DefaultGameObjectInjectionWorld;
        GameObject menuObject = new("M02 Map Switch Bootstrap");
        menuObject.SetActive(false);
        CampaignMissionMenuBootstrapRuntime bootstrap = new();
        Entity missionRoot = Entity.Null;
        try
        {
            World.DefaultGameObjectInjectionWorld = world;
            MenuBootstrapView view = menuObject.AddComponent<MenuBootstrapView>();
            view.Configure(null, null, null, null, null, null,
                configuredCampaignOperationMapCatalog: maps,
                configuredCampaignMissionCatalog: missions);
            bootstrap.Update(view);
            AssertSingleCampaignRoot(world.EntityManager, out missionRoot);

            DynamicBuffer<CampaignMissionLaunchRequestElement> requests =
                world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot);
            requests.Add(Request(
                new FixedString64Bytes(MissionDefinitionContractValidation.FirstContactMissionId),
                new FixedString64Bytes("scenario.ch01.m01.first_contact"),
                new FixedString64Bytes("opmap.ch01.district_edge_01"),
                FirstLaunchMissionHandoffOperation.DeterministicSeed,
                MissionRunKind.FirstClear,
                "campaign-m01-map-switch",
                0,
                808));
            bootstrap.Update(view);

            using EntityQuery mapQuery = world.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRootComponent>(),
                ComponentType.ReadOnly<ActiveOperationMapComponent>());
            Assert.AreEqual(1, mapQuery.CalculateEntityCount());
            Entity mapRoot = mapQuery.GetSingletonEntity();
            ActiveOperationMapComponent first =
                world.EntityManager.GetComponentData<ActiveOperationMapComponent>(mapRoot);
            Assert.AreEqual("opmap.ch01.district_edge_01", first.OperationMapId.ToString());

            requests = world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(missionRoot);
            requests.Clear();
            requests.Add(Request(
                MissionRunKind.FirstClear,
                "campaign-m02-map-switch",
                0,
                809));
            bootstrap.Update(view);

            Assert.AreEqual(1, mapQuery.CalculateEntityCount());
            Assert.AreEqual(mapRoot, mapQuery.GetSingletonEntity());
            ActiveOperationMapComponent second =
                world.EntityManager.GetComponentData<ActiveOperationMapComponent>(mapRoot);
            Assert.AreEqual(M02MissionId, second.MissionId.ToString());
            Assert.AreEqual(M02ScenarioId, second.ScenarioId.ToString());
            Assert.AreEqual(M02MapId, second.OperationMapId.ToString());
            Assert.Greater(second.Generation, first.Generation);
        }
        finally
        {
            bootstrap.Shutdown();
            if (missionRoot != Entity.Null && world.EntityManager.Exists(missionRoot))
                DisposeCatalog(world.EntityManager, missionRoot);
            World.DefaultGameObjectInjectionWorld = previous;
            UnityEngine.Object.DestroyImmediate(menuObject);
        }
    }

    private static ProjectionFixture CreateProjectionFixture(bool m02Completed, bool pendingResume = false)
    {
        World world = new("m02-campaign-projection");
        Assert.IsTrue(ProjectChapter(world.EntityManager, 1, out Entity missionRoot, out string error), error);
        string saveRoot = NewSaveRoot(m02Completed ? "replay" : "first-clear");
        CampaignMissionProgressStore store = new(new SaveService(new JsonSaveRepository(saveRoot)));
        Assert.IsTrue(store.Settle(
            MissionDefinitionContractValidation.FirstContactMissionId,
            "m01-complete", 0, true, 3, 60000, M02MissionId));
        if (m02Completed)
            Assert.IsTrue(store.Settle(M02MissionId, "m02-complete", 1, true, 3, 60000, null));
        else if (pendingResume)
            Assert.IsTrue(store.SetPendingResume(M02MissionId, true, 2));
        world.EntityManager.GetComponentObject<CampaignMissionProgressStoreReferenceComponent>(missionRoot).Store = store;
        Entity uiRoot = world.EntityManager.CreateEntity(typeof(UiShellRootComponent));
        world.EntityManager.AddBuffer<UiShellRouteRequestComponent>(uiRoot);
        return new ProjectionFixture(world, missionRoot, uiRoot, saveRoot);
    }

    private static CampaignMissionLaunchRequestElement QueueM02Deploy(World world, Entity uiRoot)
    {
        UpdateProjection(world);
        DynamicBuffer<UiCampaignMissionActionRequestElement> actions =
            world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot);
        actions.Add(new UiCampaignMissionActionRequestElement
        {
            Action = UiCampaignMissionActionKind.OpenBriefing,
            MissionId = new FixedString64Bytes(M02MissionId)
        });
        UpdateProjection(world);
        Assert.AreEqual(M02MissionId,
            world.EntityManager.GetComponentData<UiCampaignOperationsComponent>(uiRoot)
                .SelectedMissionId.ToString());
        actions = world.EntityManager.GetBuffer<UiCampaignMissionActionRequestElement>(uiRoot);
        actions.Add(new UiCampaignMissionActionRequestElement
        {
            Action = UiCampaignMissionActionKind.Deploy,
            MissionId = new FixedString64Bytes(M02MissionId)
        });
        UpdateProjection(world);
        using EntityQuery query = world.EntityManager.CreateEntityQuery(
            ComponentType.ReadOnly<CampaignMissionRootComponent>());
        Entity root = query.GetSingletonEntity();
        DynamicBuffer<CampaignMissionLaunchRequestElement> launches =
            world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root);
        Assert.AreEqual(1, launches.Length);
        return launches[0];
    }

    private static CampaignMissionRuntimeComponent RunAccepted(CampaignMissionLaunchRequestElement request)
    {
        using World world = CreateLaunchWorld(request, out Entity root, out _);
        UpdateLaunch(world);
        DynamicBuffer<CampaignMissionLaunchResultElement> results =
            world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(root);
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(1, results[0].Accepted, results[0].ReasonCode.ToString());
        CampaignMissionRuntimeComponent runtime =
            world.EntityManager.GetComponentData<CampaignMissionRuntimeComponent>(root);
        DisposeCatalog(world.EntityManager, root);
        return runtime;
    }

    private static void AssertRejected(CampaignMissionLaunchRequestElement request, string reason)
    {
        using World world = CreateLaunchWorld(request, out Entity root, out _);
        UpdateLaunch(world);
        CampaignMissionLaunchResultElement result =
            world.EntityManager.GetBuffer<CampaignMissionLaunchResultElement>(root)[0];
        Assert.AreEqual(0, result.Accepted);
        Assert.AreEqual(reason, result.ReasonCode.ToString());
        DisposeCatalog(world.EntityManager, root);
    }

    private static World CreateLaunchWorld(
        CampaignMissionLaunchRequestElement request,
        out Entity root,
        out Entity mapRoot)
    {
        World world = new("m02-launch");
        Assert.IsTrue(ProjectChapter(world.EntityManager, 1, out root, out string error), error);
        world.EntityManager.GetBuffer<CampaignMissionLaunchRequestElement>(root).Add(request);
        mapRoot = world.EntityManager.CreateEntity(
            typeof(ActiveOperationMapComponent), typeof(OperationMapReadinessComponent));
        world.EntityManager.SetComponentData(mapRoot, new ActiveOperationMapComponent
        {
            OperationMapId = request.OperationMapId,
            ScenarioId = request.ScenarioId,
            MissionId = request.MissionId,
            SchemaVersion = 1,
            ContentVersion = 1,
            Generation = 1
        });
        OperationMapReadinessFlags readiness = AllReadiness();
        world.EntityManager.SetComponentData(mapRoot, new OperationMapReadinessComponent
        {
            Generation = 1,
            ReadyFlags = readiness,
            RequiredFlags = readiness
        });
        return world;
    }

    private static bool ProjectChapter(
        EntityManager entityManager,
        uint sourceVersion,
        out Entity root,
        out string error)
    {
        LoadChapter(out MissionDefinitionCatalogConfig missions, out OperationMapCatalogConfig maps);
        return CampaignMissionCatalogProjection.TryProject(
            entityManager, missions, maps, sourceVersion, out root, out error);
    }

    private static void LoadChapter(
        out MissionDefinitionCatalogConfig missions,
        out OperationMapCatalogConfig maps)
    {
        missions = AssetDatabase.LoadAssetAtPath<MissionDefinitionCatalogConfig>(
            M02EstablishBaseConfigBuilder.MissionCatalogPath);
        maps = AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(
            M02EstablishBaseConfigBuilder.OperationMapCatalogPath);
    }

    private static CampaignMissionLaunchRequestElement Request(
        MissionRunKind runKind,
        string session,
        int attempt,
        ulong transition) => Request(
        new FixedString64Bytes(M02MissionId), new FixedString64Bytes(M02ScenarioId),
        new FixedString64Bytes(M02MapId), M02Seed, runKind, session, attempt, transition);

    private static CampaignMissionLaunchRequestElement Request(
        FixedString64Bytes missionId,
        FixedString64Bytes scenarioId,
        FixedString64Bytes mapId,
        int seed,
        MissionRunKind runKind,
        string session,
        int attempt,
        ulong transition) => new()
    {
        SchemaVersion = MissionLaunchPayloadFactory.CurrentSchemaVersion,
        MissionId = missionId,
        ScenarioId = scenarioId,
        OperationMapId = mapId,
        LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
        RunKind = runKind,
        Guidance = NarrativeGuidanceMode.Contextual,
        TransitionToken = transition,
        SessionToken = new FixedString64Bytes(session),
        AttemptOrdinal = attempt,
        DeterministicSeed = seed
    };

    private static CampaignMissionRuntimeComponent Runtime(
        MissionRunKind runKind,
        string session,
        int attempt,
        ulong transition) => new()
    {
        MissionId = new FixedString64Bytes(M02MissionId),
        ScenarioId = new FixedString64Bytes(M02ScenarioId),
        OperationMapId = new FixedString64Bytes(M02MapId),
        SessionToken = new FixedString64Bytes(session),
        Phase = MissionPhaseKind.Preparing,
        LaunchOrigin = MissionLaunchOriginKind.CampaignOperations,
        RunKind = runKind,
        Guidance = NarrativeGuidanceMode.Contextual,
        TransitionToken = transition,
        AttemptOrdinal = attempt,
        DeterministicSeed = M02Seed
    };

    private static void UpdateProjection(World world)
    {
        SystemHandle handle = world.CreateSystem<UiCampaignMissionProjectionSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<UiCampaignMissionProjectionSystem>(handle).OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
        world.DestroySystem(handle);
    }

    private static void UpdateLaunch(World world)
    {
        SystemHandle handle = world.CreateSystem<CampaignMissionLaunchSystem>();
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<CampaignMissionLaunchSystem>(handle).OnUpdate(ref state);
        state.Dependency.Complete();
        world.EntityManager.CompleteAllTrackedJobs();
        world.DestroySystem(handle);
    }

    private static OperationMapReadinessFlags AllReadiness() =>
        OperationMapReadinessFlags.SourceContent | OperationMapReadinessFlags.SubScene |
        OperationMapReadinessFlags.Metadata | OperationMapReadinessFlags.MapSurface |
        OperationMapReadinessFlags.AuthoredConversion | OperationMapReadinessFlags.PresentationManifest |
        OperationMapReadinessFlags.RequiredPresentationPreload;

    private static void AssertSingleCampaignRoot(EntityManager manager, out Entity root)
    {
        using EntityQuery query = manager.CreateEntityQuery(ComponentType.ReadOnly<CampaignMissionRootComponent>());
        Assert.AreEqual(1, query.CalculateEntityCount());
        root = query.GetSingletonEntity();
        CampaignMissionCatalogComponent catalog = manager.GetComponentData<CampaignMissionCatalogComponent>(root);
        Assert.AreEqual(2, catalog.Blob.Value.Missions.Length);
    }

    private static void DisposeCatalog(EntityManager entityManager, Entity root)
    {
        if (root == Entity.Null || !entityManager.Exists(root))
            return;
        CampaignMissionCatalogComponent catalog =
            entityManager.GetComponentData<CampaignMissionCatalogComponent>(root);
        CampaignMissionCatalogDisposalSystem.DisposeOwned(ref catalog);
        entityManager.SetComponentData(root, catalog);
    }

    private static void SetField<T>(object target, string name, T value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, name);
        field.SetValue(target, value);
    }

    private static string NewSaveRoot(string name) => Path.Combine(
        Path.GetTempPath(), "WarlineCapture", "M02EstablishBaseLaunchTests", name, Guid.NewGuid().ToString("N"));

    private static void DeleteSaveRoot(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    private sealed class ProjectionFixture : IDisposable
    {
        public ProjectionFixture(World world, Entity missionRoot, Entity uiRoot, string saveRoot)
        {
            World = world;
            MissionRoot = missionRoot;
            UiRoot = uiRoot;
            SaveRoot = saveRoot;
        }

        public World World { get; }
        public Entity MissionRoot { get; }
        public Entity UiRoot { get; }
        private string SaveRoot { get; }

        public void Dispose()
        {
            DisposeCatalog(World.EntityManager, MissionRoot);
            World.Dispose();
            DeleteSaveRoot(SaveRoot);
        }
    }
}
#endif
