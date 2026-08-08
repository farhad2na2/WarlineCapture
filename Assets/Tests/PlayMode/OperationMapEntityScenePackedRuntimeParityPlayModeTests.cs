using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Game.Authoring;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
using Game.Runtime;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using Hash128 = Unity.Entities.Hash128;

public sealed partial class OperationMapEntityScenePackedRuntimeParityPlayModeTests
{
    private const string DefinitionAddress =
        "operation-map-candidate/opmap.skirmish.desert_base_01/definition";
    private const string ExpectedOperationMapId = "opmap.skirmish.desert_base_01";
    private const string ExpectedReportPath =
        "Design/AgentReports/2026-07-21_dense_city_phase0a_transform_parity.json";
    private const string RuntimeContentReportPath =
        "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_runtime_content.json";
    private const string CandidateSubScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
        "opmap_skirmish_desert_base_01_entity_presentation_candidate.unity";
    private const string CandidateDefinitionPath =
        "Assets/Game/Configs/OperationMaps/Candidates/" +
        "OperationMap_Compatibility_DesertBase01_EntityScene_Candidate.asset";
    private const string CandidateRuntimeBindingPath =
        "Assets/Game/GeneratedOperationMaps/RuntimeBinding/" +
        "opmap.skirmish.desert_base_01/Candidates/" +
        "opmap_skirmish_desert_base_01_entity_scene_runtime.unity";
    private const string AcceptedOperationMapScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/" +
        "opmap_skirmish_desert_base_01.unity";
    private const string AcceptedSubScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/" +
        "opmap_skirmish_desert_base_01_subscene.unity";
    private const string EntityContentPath =
        "Library/OperationMapCandidateRuntimeContent/Entities";
    private const float MaximumWaitSeconds = 300f;

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator PackedCandidate_TwoLoadCyclesMatchBakedMatricesAndBounds()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string catalogPath = Path.Combine(
            projectRoot,
            "Library/com.unity.addressables/aa/OSX/catalog.bin");
        Assert.That(File.Exists(catalogPath), Is.True,
            $"Build candidate runtime parity content before running this test: {catalogPath}");

        TransformParityReport expected = JsonUtility.FromJson<TransformParityReport>(
            File.ReadAllText(Path.Combine(projectRoot, ExpectedReportPath)));
        ValidateExpectedReport(expected);

        string entityContentRoot = Path.Combine(projectRoot, EntityContentPath);
        string entityCatalogPath = Path.Combine(
            entityContentRoot,
            RuntimeContentManager.RelativeCatalogPath);
        Assert.That(File.Exists(entityCatalogPath), Is.True,
            $"Build candidate Entities runtime content before running this test: {entityCatalogPath}");
        RuntimeContentReport runtimeContentReport =
            JsonUtility.FromJson<RuntimeContentReport>(
                File.ReadAllText(Path.Combine(projectRoot, RuntimeContentReportPath)));
        ValidateRuntimeContentReport(
            runtimeContentReport,
            projectRoot,
            catalogPath,
            entityCatalogPath);
        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
        AsyncOperationHandle<OperationMapDefinition> definitionHandle = default;
        World parityWorld = null;
        bool airMovementStateCaptured = false;
        bool airMovementWasEnabled = false;
        bool bladeSpinStateCaptured = false;
        bool bladeSpinWasEnabled = false;
        bool transportDoorStateCaptured = false;
        bool transportDoorWasEnabled = false;
        try
        {
            RuntimeContentManager.Cleanup(out _);
            RuntimeContentManager.Initialize();
            Assert.That(
                RuntimeContentManager.LoadLocalCatalogData(
                    entityCatalogPath,
                    RuntimeContentManager.DefaultContentFileNameFunc,
                    file => Path.Combine(
                        entityContentRoot,
                        RuntimeContentManager.DefaultArchivePathFunc(file))),
                Is.True,
                $"Candidate Entities runtime catalog failed to load: {entityCatalogPath}");

            catalogHandle =
                Addressables.LoadContentCatalogAsync(catalogPath, autoReleaseHandle: false);
            yield return catalogHandle;
            Assert.That(catalogHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                catalogHandle.OperationException?.Message);

            definitionHandle =
                Addressables.LoadAssetAsync<OperationMapDefinition>(DefinitionAddress);
            yield return definitionHandle;
            Assert.That(definitionHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                definitionHandle.OperationException?.Message);

            parityWorld = World.DefaultGameObjectInjectionWorld;
            Assert.That(parityWorld, Is.Not.Null);
            Assert.That(parityWorld.IsCreated, Is.True);
            airMovementWasEnabled =
                SetSystemEnabled<UnitAirMovementSystem>(parityWorld, false);
            airMovementStateCaptured = true;
            bladeSpinWasEnabled =
                SetSystemEnabled<UnitHelicopterBladeSpinSystem>(parityWorld, false);
            bladeSpinStateCaptured = true;
            transportDoorWasEnabled =
                SetSystemEnabled<UnitTransportPlaneDoorSystem>(parityWorld, false);
            transportDoorStateCaptured = true;

            for (int cycle = 1; cycle <= 2; cycle++)
                yield return RunLoadCaptureUnloadCycle(definitionHandle.Result, expected, cycle);
        }
        finally
        {
            if (parityWorld != null && parityWorld.IsCreated)
            {
                if (airMovementStateCaptured)
                {
                    SetSystemEnabled<UnitAirMovementSystem>(
                        parityWorld,
                        airMovementWasEnabled);
                }
                if (bladeSpinStateCaptured)
                {
                    SetSystemEnabled<UnitHelicopterBladeSpinSystem>(
                        parityWorld,
                        bladeSpinWasEnabled);
                }
                if (transportDoorStateCaptured)
                {
                    SetSystemEnabled<UnitTransportPlaneDoorSystem>(
                        parityWorld,
                        transportDoorWasEnabled);
                }
            }
            if (definitionHandle.IsValid())
                Addressables.Release(definitionHandle);
            if (catalogHandle.IsValid())
            {
                if (catalogHandle.Status == AsyncOperationStatus.Succeeded)
                    Addressables.RemoveResourceLocator(catalogHandle.Result);
                Addressables.Release(catalogHandle);
            }
            RuntimeContentManager.Cleanup(out _);
            RuntimeContentManager.Initialize();
        }
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator PackedCandidate_TwoMatchToMenuCyclesReleaseAllOwnershipWithoutStaticDrain()
    {
        yield return RunPackedCandidateRoute(
            cycleCount: 2,
            validateCameraTraversal: false,
            validateSteadyStateAllocation: false,
            validateBuildingDestruction: false,
            validateVehicleMovement: false);
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator PackedCandidate_CameraTraversalChangesCullingWithoutSceneStreaming()
    {
        if (SystemInfo.graphicsDeviceType ==
            UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Assert.Ignore("Entities Graphics culling visibility requires a graphics device.");
        }

        yield return RunPackedCandidateRoute(
            cycleCount: 1,
            validateCameraTraversal: true,
            validateSteadyStateAllocation: false,
            validateBuildingDestruction: false,
            validateVehicleMovement: false);
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator PackedCandidate_ReadyOperationMapOrchestrationAllocatesZeroBytes()
    {
        yield return RunPackedCandidateRoute(
            cycleCount: 1,
            validateCameraTraversal: false,
            validateSteadyStateAllocation: true,
            validateBuildingDestruction: false,
            validateVehicleMovement: false);
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator PackedCandidate_BuildingDestructionUsesBakedEntityVisualsOnly()
    {
        yield return RunPackedCandidateRoute(
            cycleCount: 1,
            validateCameraTraversal: false,
            validateSteadyStateAllocation: false,
            validateBuildingDestruction: true,
            validateVehicleMovement: false);
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator PackedCandidate_AuthoredGroundVehicleMovesAndRetainsEcsPresentation()
    {
        yield return RunPackedCandidateRoute(
            cycleCount: 1,
            validateCameraTraversal: false,
            validateSteadyStateAllocation: false,
            validateBuildingDestruction: false,
            validateVehicleMovement: true);
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator PackedCandidate_ReadinessFailureResetsAndRetriesWithoutStaleOwnership()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string catalogPath = Path.Combine(
            projectRoot,
            "Library/com.unity.addressables/aa/OSX/catalog.bin");
        string entityContentRoot = Path.Combine(projectRoot, EntityContentPath);
        string entityCatalogPath = Path.Combine(
            entityContentRoot,
            RuntimeContentManager.RelativeCatalogPath);
        Assert.That(File.Exists(catalogPath), Is.True,
            $"Build candidate runtime parity content before running this test: {catalogPath}");
        Assert.That(File.Exists(entityCatalogPath), Is.True,
            $"Build candidate Entities runtime content before running this test: {entityCatalogPath}");

        RuntimeContentReport runtimeContentReport =
            JsonUtility.FromJson<RuntimeContentReport>(
                File.ReadAllText(Path.Combine(projectRoot, RuntimeContentReportPath)));
        ValidateRuntimeContentReport(
            runtimeContentReport,
            projectRoot,
            catalogPath,
            entityCatalogPath);

        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
        AsyncOperationHandle<OperationMapDefinition> definitionHandle = default;
        OperationMapSceneLoadingSceneSystemHelper loader = null;
        try
        {
            RuntimeContentManager.Cleanup(out _);
            RuntimeContentManager.Initialize();
            Assert.That(
                RuntimeContentManager.LoadLocalCatalogData(
                    entityCatalogPath,
                    RuntimeContentManager.DefaultContentFileNameFunc,
                    file => Path.Combine(
                        entityContentRoot,
                        RuntimeContentManager.DefaultArchivePathFunc(file))),
                Is.True,
                $"Candidate Entities runtime catalog failed to load: {entityCatalogPath}");

            catalogHandle =
                Addressables.LoadContentCatalogAsync(catalogPath, autoReleaseHandle: false);
            yield return catalogHandle;
            Assert.That(catalogHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                catalogHandle.OperationException?.Message);

            definitionHandle =
                Addressables.LoadAssetAsync<OperationMapDefinition>(DefinitionAddress);
            yield return definitionHandle;
            Assert.That(definitionHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                definitionHandle.OperationException?.Message);

            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            Assert.That(world.IsCreated, Is.True);
            EntityManager entityManager = world.EntityManager;
            var sceneGuid = new Hash128(
                definitionHandle.Result.NavigationMetadata.AuthoredSubSceneGuid);
            var failReadinessOnceApi = new FailReadinessOncePackedEntitySceneApi();
            loader = new OperationMapSceneLoadingSceneSystemHelper(
                entitySceneApi: failReadinessOnceApi);

            Assert.That(
                loader.TryStart(definitionHandle.Result, out string startError),
                Is.True,
                startError);
            float deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
            while (!loader.HasFailed &&
                   !loader.IsReady &&
                   Time.realtimeSinceStartup < deadline)
            {
                loader.Update();
                yield return null;
            }

            Assert.That(loader.HasFailed, Is.True);
            Assert.That(loader.IsReady, Is.False);
            Assert.That(loader.FailureCode,
                Is.EqualTo(OperationMapLoadResultCode.MetadataBindFailed));
            Assert.That(loader.Failure, Does.Contain("different operation map"));
            Assert.That(loader.Manifest, Is.Null);
            Assert.That(failReadinessOnceApi.FailureCount, Is.EqualTo(1));
            Assert.That(failReadinessOnceApi.FailedSceneEntity, Is.Not.EqualTo(Entity.Null));
            Assert.That(failReadinessOnceApi.FailedSectionEntities.Length, Is.GreaterThan(0));

            bool reset = false;
            string resetError = null;
            deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
            while (!reset && Time.realtimeSinceStartup < deadline)
            {
                reset = loader.TryReset(out resetError);
                if (!reset)
                {
                    Assert.That(
                        resetError,
                        Does.Contain("cleanup is still in progress"));
                    yield return null;
                }
            }

            Assert.That(reset, Is.True, resetError);
            Assert.That(loader.HasFailed, Is.False);
            Assert.That(loader.FailureCode, Is.EqualTo(OperationMapLoadResultCode.None));

            deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
            while (Time.realtimeSinceStartup < deadline &&
                   entityManager.Exists(failReadinessOnceApi.FailedSceneEntity))
            {
                yield return null;
            }

            Assert.That(
                entityManager.Exists(failReadinessOnceApi.FailedSceneEntity),
                Is.False,
                "Failed packed EntityScene metadata remained after reset cleanup.");
            for (int sectionIndex = 0;
                 sectionIndex < failReadinessOnceApi.FailedSectionEntities.Length;
                 sectionIndex++)
            {
                Assert.That(
                    entityManager.Exists(
                        failReadinessOnceApi.FailedSectionEntities[sectionIndex]),
                    Is.False,
                    $"Failed packed section {sectionIndex} remained after reset cleanup.");
            }
            Assert.That(
                SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid),
                Is.EqualTo(Entity.Null));

            Assert.That(
                loader.TryStart(definitionHandle.Result, out startError),
                Is.True,
                startError);
            deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
            while (!loader.IsReady &&
                   !loader.HasFailed &&
                   Time.realtimeSinceStartup < deadline)
            {
                loader.Update();
                yield return null;
            }

            Assert.That(loader.HasFailed, Is.False, loader.Failure);
            Assert.That(loader.IsReady, Is.True);
            Assert.That(loader.Manifest, Is.Null);
            Assert.That(failReadinessOnceApi.FailureCount, Is.EqualTo(1),
                "The one-shot readiness fault must not affect the retry.");

            Entity retrySceneEntity =
                SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid);
            Assert.That(retrySceneEntity, Is.Not.EqualTo(Entity.Null));
            Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged, retrySceneEntity), Is.True);
            Entity[] retrySections =
                GetResolvedSectionEntities(entityManager, retrySceneEntity);
            Assert.That(
                CountEntitiesForSections(entityManager, retrySections),
                Is.GreaterThan(0));
            Assert.That(
                OperationMapEntityPresentationReadinessUtility.TryValidate(
                    entityManager,
                    retrySceneEntity,
                    ExpectedOperationMapId,
                    out string readinessError),
                Is.True,
                readinessError);

            Assert.That(
                loader.TryBeginUnload(out string unloadError),
                Is.True,
                unloadError);
            deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
            while (!loader.UnloadComplete &&
                   !loader.HasFailed &&
                   Time.realtimeSinceStartup < deadline)
            {
                loader.Update();
                yield return null;
            }

            Assert.That(loader.HasFailed, Is.False, loader.Failure);
            Assert.That(loader.UnloadComplete, Is.True);
            Assert.That(entityManager.Exists(retrySceneEntity), Is.False);
            for (int sectionIndex = 0; sectionIndex < retrySections.Length; sectionIndex++)
            {
                Assert.That(
                    entityManager.Exists(retrySections[sectionIndex]),
                    Is.False,
                    $"Retry section {sectionIndex} remained after successful unload.");
            }
            Assert.That(
                SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid),
                Is.EqualTo(Entity.Null));
            AssertAcceptedAuthoringScenesNotLoaded();
        }
        finally
        {
            loader?.Dispose();
            if (definitionHandle.IsValid())
                Addressables.Release(definitionHandle);
            if (catalogHandle.IsValid())
            {
                if (catalogHandle.Status == AsyncOperationStatus.Succeeded)
                    Addressables.RemoveResourceLocator(catalogHandle.Result);
                Addressables.Release(catalogHandle);
            }
            RuntimeContentManager.Cleanup(out _);
            RuntimeContentManager.Initialize();
        }
    }

    private static IEnumerator RunPackedCandidateRoute(
        int cycleCount,
        bool validateCameraTraversal,
        bool validateSteadyStateAllocation,
        bool validateBuildingDestruction,
        bool validateVehicleMovement)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string catalogPath = Path.Combine(
            projectRoot,
            "Library/com.unity.addressables/aa/OSX/catalog.bin");
        string entityContentRoot = Path.Combine(projectRoot, EntityContentPath);
        string entityCatalogPath = Path.Combine(
            entityContentRoot,
            RuntimeContentManager.RelativeCatalogPath);
        Assert.That(File.Exists(catalogPath), Is.True,
            $"Build candidate runtime parity content before running this test: {catalogPath}");
        Assert.That(File.Exists(entityCatalogPath), Is.True,
            $"Build candidate Entities runtime content before running this test: {entityCatalogPath}");

        RuntimeContentReport runtimeContentReport =
            JsonUtility.FromJson<RuntimeContentReport>(
                File.ReadAllText(Path.Combine(projectRoot, RuntimeContentReportPath)));
        ValidateRuntimeContentReport(
            runtimeContentReport,
            projectRoot,
            catalogPath,
            entityCatalogPath);

        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
        AsyncOperationHandle<OperationMapDefinition> definitionHandle = default;
        OperationMapCatalogConfig candidateCatalog = null;
        var route = new Aph805MenuMatchMenuLifecyclePlayModeTests.TransitionContext
        {
            OperationMapSceneName = Path.GetFileNameWithoutExtension(CandidateRuntimeBindingPath)
        };
        try
        {
            RuntimeContentManager.Cleanup(out _);
            RuntimeContentManager.Initialize();
            Assert.That(
                RuntimeContentManager.LoadLocalCatalogData(
                    entityCatalogPath,
                    RuntimeContentManager.DefaultContentFileNameFunc,
                    file => Path.Combine(
                        entityContentRoot,
                        RuntimeContentManager.DefaultArchivePathFunc(file))),
                Is.True,
                $"Candidate Entities runtime catalog failed to load: {entityCatalogPath}");

            catalogHandle =
                Addressables.LoadContentCatalogAsync(catalogPath, autoReleaseHandle: false);
            yield return catalogHandle;
            Assert.That(catalogHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                catalogHandle.OperationException?.Message);

            definitionHandle =
                Addressables.LoadAssetAsync<OperationMapDefinition>(DefinitionAddress);
            yield return definitionHandle;
            Assert.That(definitionHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                definitionHandle.OperationException?.Message);

            candidateCatalog = CreateCandidateCatalog(definitionHandle.Result);
            MatchSceneView.SetEditorOperationMapCatalogOverrideForTests(candidateCatalog);

            yield return Aph805MenuMatchMenuLifecyclePlayModeTests.PrepareStableMenu(route);
            StaticMapPresentationStreamer staticStreamer =
                ResolveStaticPresentationStreamer(route.Menu);
            Assert.That(staticStreamer.DrainComplete, Is.False);
            Assert.That(staticStreamer.PendingOperationCount, Is.Zero);
            Assert.That(staticStreamer.HasActiveOperation, Is.False);

            for (int cycle = 1; cycle <= cycleCount; cycle++)
            {
                yield return RunPackedMatchToMenuCycle(
                    route,
                    definitionHandle.Result,
                    staticStreamer,
                    cycle,
                    validateCameraTraversal && cycle == 1,
                    validateSteadyStateAllocation && cycle == 1,
                    validateBuildingDestruction && cycle == 1,
                    validateVehicleMovement && cycle == 1);
            }
        }
        finally
        {
            MatchSceneView.SetEditorOperationMapCatalogOverrideForTests(null);
            if (candidateCatalog != null)
                UnityEngine.Object.Destroy(candidateCatalog);
            if (definitionHandle.IsValid())
                Addressables.Release(definitionHandle);
            if (catalogHandle.IsValid())
            {
                if (catalogHandle.Status == AsyncOperationStatus.Succeeded)
                    Addressables.RemoveResourceLocator(catalogHandle.Result);
                Addressables.Release(catalogHandle);
            }
            RuntimeContentManager.Cleanup(out _);
            RuntimeContentManager.Initialize();
        }
    }

    private static IEnumerator RunPackedMatchToMenuCycle(
        Aph805MenuMatchMenuLifecyclePlayModeTests.TransitionContext route,
        OperationMapDefinition definition,
        StaticMapPresentationStreamer staticStreamer,
        int cycle,
        bool validateCameraTraversal,
        bool validateSteadyStateAllocation,
        bool validateBuildingDestruction,
        bool validateVehicleMovement,
        bool validateCameraSimulationState = false,
        bool validateCameraVisualEvidence = false,
        Action<World, Entity[]> validateLoadedContent = null)
    {
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnterStableMatch(route);
        Assert.That(
            route.Match.CanonicalPresentationMode,
            Is.EqualTo(OperationMapCanonicalPresentationMode.EntityScene));
        Assert.That(
            route.Match.MatchBootstrap.RuntimeCity,
            Is.Null,
            "EntityScene presentation must not construct the legacy runtime city generator.");
        Assert.That(
            route.Match.MatchBootstrap.RuntimeGridBlockers,
            Is.Null,
            "EntityScene presentation must not construct legacy runtime blocker visuals.");
        Assert.That(
            route.Match.MatchBootstrap.RuntimeDecorations,
            Is.Null,
            "EntityScene presentation must not construct legacy runtime decoration visuals.");
        AssertNoManagedMapVisualOwnership(route.Match.MatchBootstrap);
        Assert.That(staticStreamer.DrainComplete, Is.False);
        Assert.That(staticStreamer.PendingOperationCount, Is.Zero);
        Assert.That(staticStreamer.HasActiveOperation, Is.False);

        World world = route.World;
        EntityManager entityManager = world.EntityManager;
        var sceneGuid = new Hash128(definition.NavigationMetadata.AuthoredSubSceneGuid);
        Entity sceneEntity = SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid);
        Assert.That(sceneEntity, Is.Not.EqualTo(Entity.Null),
            $"Cycle {cycle} did not create the candidate EntityScene.");
        Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity), Is.True);
        Entity[] resolvedSectionEntities =
            GetResolvedSectionEntities(entityManager, sceneEntity);
        Assert.That(
            CountEntitiesForSections(entityManager, resolvedSectionEntities),
            Is.GreaterThan(0));
        AssertSinglePublishedOperationMapRoot(entityManager);
        AssertAcceptedAuthoringScenesNotLoaded();
        validateLoadedContent?.Invoke(world, resolvedSectionEntities);

        if (validateCameraTraversal)
        {
            yield return ValidateCameraTraversalKeepsEntitySceneResident(
                route,
                sceneEntity,
                resolvedSectionEntities,
                staticStreamer,
                definition.RenderResidencyMode ==
                OperationMapRenderResidencyMode.VirtualizedProxyPool,
                validateCameraSimulationState);
        }

        if (validateCameraVisualEvidence)
        {
            yield return DenseCaptureProductionCameraVisualEvidence(
                route);
        }

        if (validateSteadyStateAllocation)
        {
            ValidateReadyOperationMapOrchestrationAllocatesZeroBytes(
                route.Menu,
                route.Match,
                world,
                sceneEntity,
                resolvedSectionEntities,
                staticStreamer);
        }

        if (validateBuildingDestruction)
            ValidateBuildingDestructionUsesBakedEntitiesOnly(route.Match.MatchBootstrap, world);

        if (validateVehicleMovement)
        {
            yield return ValidateAuthoredGroundVehicleMovement(
                route.Match.MatchBootstrap,
                world,
                sceneEntity,
                resolvedSectionEntities,
                staticStreamer);
        }

        if (SystemInfo.graphicsDeviceType ==
            UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            LogAssert.Expect(LogType.Error, "RenderTexture.Create failed");
            LogAssert.Expect(LogType.Error, "RenderTexture.Create failed");
        }
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.ReturnToStableMenu(route);

        Assert.That(entityManager.Exists(sceneEntity), Is.False,
            $"Cycle {cycle} retained EntityScene metadata.");
        Assert.That(
            SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid),
            Is.EqualTo(Entity.Null),
            $"Cycle {cycle} retained the candidate scene lookup.");
        for (int sectionIndex = 0; sectionIndex < resolvedSectionEntities.Length; sectionIndex++)
        {
            Assert.That(
                entityManager.Exists(resolvedSectionEntities[sectionIndex]),
                Is.False,
                $"Cycle {cycle} retained section metadata {sectionIndex}.");
        }
        Assert.That(
            CountEntitiesForSections(entityManager, resolvedSectionEntities),
            Is.Zero,
            $"Cycle {cycle} retained candidate EntityScene entities.");
        AssertNoPublishedOperationMapMetadata(entityManager);
        AssertAcceptedAuthoringScenesNotLoaded();
        Assert.That(staticStreamer.DrainComplete, Is.False);
        Assert.That(staticStreamer.PendingOperationCount, Is.Zero);
        Assert.That(staticStreamer.HasActiveOperation, Is.False);
    }

    private static IEnumerator ValidateAuthoredGroundVehicleMovement(
        MatchBootstrapCompositionSystemHelper matchBootstrap,
        World world,
        Entity sceneEntity,
        IReadOnlyList<Entity> resolvedSectionEntities,
        StaticMapPresentationStreamer staticStreamer)
    {
        EntityManager entityManager = world.EntityManager;
        int expectedSectionEntityCount =
            CountEntitiesForSections(entityManager, resolvedSectionEntities);
        Entity[] expectedSceneRequests =
            GetEntitiesWithComponent<RequestSceneLoaded>(entityManager);

        Entity[] vehicles = GetMovableAuthoredGroundVehicles(entityManager);
        Assert.That(vehicles.Length, Is.GreaterThan(0),
            "Packed candidate has no movable authored ground vehicle.");

        Entity vehicle = Entity.Null;
        int2 goal = default;
        for (int vehicleIndex = 0; vehicleIndex < vehicles.Length; vehicleIndex++)
        {
            Entity candidate = vehicles[vehicleIndex];
            UnitMovementBehavior behavior =
                entityManager.GetComponentData<UnitMovementBehavior>(candidate);
            if (behavior.UsesVehicleMotion == 0)
                continue;
            if (TryFindNearbyVehicleGoal(entityManager, candidate, out goal))
            {
                vehicle = candidate;
                break;
            }
        }

        Assert.That(vehicle, Is.Not.EqualTo(Entity.Null),
            "No packed authored ground vehicle had a valid nearby movement goal.");
        SceneTag vehicleSceneTag = entityManager.GetSharedComponent<SceneTag>(vehicle);
        Assert.That(resolvedSectionEntities, Does.Contain(vehicleSceneTag.SceneEntity));

        UnitDetailedVisualReference expectedVisualReference =
            entityManager.GetComponentData<UnitDetailedVisualReference>(vehicle);
        Assert.That(expectedVisualReference.Root, Is.Not.EqualTo(Entity.Null));
        Assert.That(entityManager.Exists(expectedVisualReference.Root), Is.True);
        Assert.That(
            entityManager.HasComponent<OperationMapEntityPresentationIdentity>(
                expectedVisualReference.Root),
            Is.True,
            "The authored source identity belongs to the baked vehicle visual root.");
        OperationMapEntityPresentationIdentity expectedIdentity =
            entityManager.GetComponentData<OperationMapEntityPresentationIdentity>(
                expectedVisualReference.Root);
        Assert.That(
            IsEntityDescendantOf(entityManager, expectedVisualReference.Root, vehicle),
            Is.True,
            "The baked detailed visual must remain in the vehicle transform hierarchy.");

        float3 startPosition =
            entityManager.GetComponentData<LocalTransform>(vehicle).Position;
        float3 startVisualPosition =
            entityManager.GetComponentData<LocalToWorld>(expectedVisualReference.Root).Position;
        int2 startCell = entityManager.GetComponentData<UnitGrid>(vehicle).Cell;
        Assert.That(goal, Is.Not.EqualTo(startCell));
        byte vehicleFactionId = entityManager.GetComponentData<Faction>(vehicle).Id;
        Entity temporaryFuelStorage =
            CreateTemporaryVehicleFuelStorage(entityManager, vehicleFactionId);
        UnitMoveOrderSystem.MoveOrderCommandResult moveOrderResult =
            new UnitMoveOrderSystem().IssueGroupedManualMoveOrder(
            entityManager,
            vehicle,
            goal,
            issueGroundPathNow: true,
            useGroundPathRetryCooldown: false,
            resumeFrame: 0,
            currentFrame: Time.frameCount);
        Assert.That(
            moveOrderResult.Issued,
            Is.True,
            $"Packed vehicle {entityManager.GetName(vehicle)} rejected goal {goal}.");
        Assert.That(entityManager.HasComponent<UnitTarget>(vehicle), Is.True);
        Assert.That(entityManager.GetComponentData<UnitTarget>(vehicle).Cell, Is.EqualTo(goal));
        Assert.That(entityManager.HasComponent<UnitPathRequest>(vehicle), Is.True);
        Assert.That(entityManager.GetComponentData<UnitPathRequest>(vehicle).Goal, Is.EqualTo(goal));

        float deadline = Time.realtimeSinceStartup + 8f;
        bool pathResolved = false;
        bool moved = false;
        while (Time.realtimeSinceStartup < deadline)
        {
            yield return null;
            if (!entityManager.Exists(vehicle))
                Assert.Fail("Packed authored vehicle was destroyed while executing a move order.");

            pathResolved |= entityManager.HasComponent<UnitPathRange>(vehicle) &&
                            entityManager.GetComponentData<UnitPathRange>(vehicle).Length > 0;
            float3 currentPosition =
                entityManager.GetComponentData<LocalTransform>(vehicle).Position;
            if (math.distance(startPosition, currentPosition) > 0.1f)
            {
                moved = true;
                break;
            }
        }

        RuntimeGameplayStateComponent gameplayState =
            GetSingletonComponent<RuntimeGameplayStateComponent>(entityManager);
        UnitPathfindingPendingStateComponent pendingState =
            GetSingletonComponent<UnitPathfindingPendingStateComponent>(entityManager);
        Assert.That(pathResolved, Is.True,
            $"Packed vehicle {entityManager.GetName(vehicle)} never resolved a path " +
            $"from {startCell} to {goal}. request=" +
            $"{entityManager.HasComponent<UnitPathRequest>(vehicle)} " +
            $"retry={entityManager.HasComponent<UnitPathRetryCooldown>(vehicle)} " +
            $"play={gameplayState.PlayRequested} simulation={gameplayState.SimulationActive} " +
            $"pending={pendingState.HasPendingPathJob}:{pendingState.RequestCount}:" +
            $"{pendingState.ScheduledFrame}.");
        Assert.That(moved, Is.True,
            $"Packed vehicle {entityManager.GetName(vehicle)} did not move from {startCell} toward {goal}.");
        yield return null;

        float3 movedPosition =
            entityManager.GetComponentData<LocalTransform>(vehicle).Position;
        float3 movedVisualPosition =
            entityManager.GetComponentData<LocalToWorld>(expectedVisualReference.Root).Position;
        float vehicleDistance = math.distance(startPosition, movedPosition);
        float visualDistance = math.distance(startVisualPosition, movedVisualPosition);
        Assert.That(visualDistance, Is.GreaterThan(0.05f),
            "The baked detailed visual did not follow the moving vehicle.");
        Assert.That(math.abs(vehicleDistance - visualDistance), Is.LessThan(0.15f),
            "Vehicle and baked detailed visual movement diverged.");
        Assert.That(
            entityManager.GetComponentData<OperationMapEntityPresentationIdentity>(
                expectedVisualReference.Root),
            Is.EqualTo(expectedIdentity));
        Assert.That(
            entityManager.GetComponentData<UnitDetailedVisualReference>(vehicle),
            Is.EqualTo(expectedVisualReference));
        Assert.That(
            IsEntityDescendantOf(entityManager, expectedVisualReference.Root, vehicle),
            Is.True);
        Assert.That(entityManager.Exists(sceneEntity), Is.True);
        Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity), Is.True);
        Assert.That(
            CountEntitiesForSections(entityManager, resolvedSectionEntities),
            Is.EqualTo(expectedSectionEntityCount));
        Assert.That(
            GetEntitiesWithComponent<RequestSceneLoaded>(entityManager),
            Is.EqualTo(expectedSceneRequests));
        AssertSinglePublishedOperationMapRoot(entityManager);
        AssertNoManagedMapVisualOwnership(matchBootstrap);
        Assert.That(staticStreamer.DrainComplete, Is.False);
        Assert.That(staticStreamer.PendingOperationCount, Is.Zero);
        Assert.That(staticStreamer.HasActiveOperation, Is.False);

        Assert.That(
            UnitMoveOrderRequestSystem.EnqueueAndProcessClearMovementOrder(
                entityManager,
                vehicle),
            Is.True);
        entityManager.DestroyEntity(temporaryFuelStorage);
        yield return null;
        Debug.Log(
            $"[OperationMapPackedVehicle] entity={entityManager.GetName(vehicle)} " +
            $"startCell={startCell} goal={goal} moved={vehicleDistance:R} " +
            $"visualMoved={visualDistance:R}");
    }

    private static Entity CreateTemporaryVehicleFuelStorage(
        EntityManager entityManager,
        byte factionId)
    {
        Entity storage = entityManager.CreateEntity(
            ComponentType.ReadWrite<BuildingResourceStorageComponent>());
        entityManager.SetComponentData(storage, new BuildingResourceStorageComponent
        {
            RuntimeBuildingId = int.MinValue,
            OwnerFactionId = factionId,
            FuelStorageCapacity = 1000,
            StoredFuelBarrels = 1000f,
            Version = 1
        });
        return storage;
    }

    private static Entity[] GetMovableAuthoredGroundVehicles(
        EntityManager entityManager)
    {
        using EntityQuery vehicleQuery = entityManager.CreateEntityQuery(
            new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<UnitMove>(),
                    ComponentType.ReadOnly<UnitFootprint>(),
                    ComponentType.ReadOnly<UnitMovementBehavior>(),
                    ComponentType.ReadOnly<UnitVehicleMovement>(),
                    ComponentType.ReadOnly<UnitVehicleKinematics>(),
                    ComponentType.ReadOnly<UnitDetailedVisualReference>(),
                    ComponentType.ReadOnly<LocalTransform>(),
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<SceneTag>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<UnitAirMovement>(),
                    ComponentType.ReadOnly<Prefab>(),
                    ComponentType.ReadOnly<Disabled>()
                }
            });
        using NativeArray<Entity> vehicles =
            vehicleQuery.ToEntityArray(Allocator.Temp);
        return vehicles.ToArray();
    }

    private static T GetSingletonComponent<T>(EntityManager entityManager)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query =
            entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        Assert.That(query.CalculateEntityCount(), Is.EqualTo(1));
        return query.GetSingleton<T>();
    }

    private static bool TryFindNearbyVehicleGoal(
        EntityManager entityManager,
        Entity vehicle,
        out int2 goal)
    {
        using EntityQuery gridQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalkable>(),
            ComponentType.ReadOnly<DynamicBlockerComponent>(),
            ComponentType.ReadOnly<DynamicOccupancyComponent>());
        Assert.That(gridQuery.CalculateEntityCount(), Is.EqualTo(1));
        Entity gridEntity = gridQuery.GetSingletonEntity();
        GridConfig grid = entityManager.GetComponentData<GridConfig>(gridEntity);
        NativeArray<GridWalkable> walkable =
            entityManager.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        DynamicBlockerComponent blockers =
            entityManager.GetComponentData<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancy =
            entityManager.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        using EntityQuery surfaceQuery =
            entityManager.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
        MapSurfaceComponent surface = surfaceQuery.CalculateEntityCount() == 1
            ? surfaceQuery.GetSingleton<MapSurfaceComponent>()
            : default;
        byte hasSurfaceData =
            (byte)(surface.HasSurfaceData != 0 && surface.SurfaceBlob.IsCreated ? 1 : 0);
        UnitGrid unitGrid = entityManager.GetComponentData<UnitGrid>(vehicle);
        int2 footprint = entityManager.GetComponentData<UnitFootprint>(vehicle).Size;
        byte factionId = entityManager.GetComponentData<Faction>(vehicle).Id;
        if (!CanVehicleTraverseSurfaceFootprint(
                surface,
                hasSurfaceData,
                grid,
                unitGrid.Cell,
                footprint))
        {
            goal = default;
            return false;
        }
        int2[] directions =
        {
            new(1, 0),
            new(-1, 0),
            new(0, 1),
            new(0, -1),
            new(1, 1),
            new(-1, 1),
            new(1, -1),
            new(-1, -1)
        };
        for (int radius = 3; radius <= 12; radius++)
        {
            for (int directionIndex = 0;
                 directionIndex < directions.Length;
                 directionIndex++)
            {
                int2 candidate = unitGrid.Cell + directions[directionIndex] * radius;
                bool directRouteValid = true;
                for (int step = 1; step <= radius; step++)
                {
                    int2 routeCell =
                        unitGrid.Cell + directions[directionIndex] * step;
                    if (!UnitFootprintUtility.CanPlaceWithPadding(
                            grid,
                            walkable,
                            blockers.Blocked,
                            blockers.FriendlyPassFactionIds,
                            occupancy.Occupied,
                            routeCell,
                            footprint,
                            unitGrid.Cell,
                            occupiedPadding: 1,
                            factionId: factionId) ||
                        !CanVehicleTraverseSurfaceFootprint(
                            surface,
                            hasSurfaceData,
                            grid,
                            routeCell,
                            footprint))
                    {
                        directRouteValid = false;
                        break;
                    }
                }
                if (!directRouteValid)
                    continue;

                goal = candidate;
                return true;
            }
        }

        goal = default;
        return false;
    }

    private static bool CanVehicleTraverseSurfaceFootprint(
        MapSurfaceComponent surface,
        byte hasSurfaceData,
        in GridConfig grid,
        int2 cell,
        int2 footprintSize)
    {
        if (hasSurfaceData == 0)
            return true;

        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 minimum = UnitFootprintUtility.GetMinCell(cell, size);
        int2 maximum = minimum + size;
        if (minimum.x < 0 || minimum.y < 0 ||
            maximum.x > grid.Width || maximum.y > grid.Height)
        {
            return false;
        }

        ref MapSurfaceBlob blob = ref surface.SurfaceBlob.Value;
        MapSurfaceMovementMask vehicleMask =
            MapSurfaceMovementMask.WheeledVehicle |
            MapSurfaceMovementMask.TrackedVehicle;
        for (int y = minimum.y; y < maximum.y; y++)
        {
            for (int x = minimum.x; x < maximum.x; x++)
            {
                if (!MapSurfaceBlobAccess.TryGetSurfaceRange(
                        ref blob,
                        new int2(x, y),
                        out MapSurfaceCellSurfaceRange range))
                {
                    return false;
                }

                bool traversable = false;
                for (int surfaceIndex = 0;
                     surfaceIndex < range.SurfaceCount;
                     surfaceIndex++)
                {
                    if (!MapSurfaceBlobAccess.TryGetSurface(
                            ref blob,
                            range,
                            surfaceIndex,
                            out MapSurfaceSample sample) ||
                        (sample.MovementMask & vehicleMask) == 0 ||
                        sample.SurfaceType == MapSurfaceType.Blocked ||
                        (sample.Flags & MapSurfaceFlags.Reserved) != 0)
                    {
                        continue;
                    }

                    bool roadLike =
                        sample.SurfaceType == MapSurfaceType.Road ||
                        sample.SurfaceType == MapSurfaceType.DirtRoad ||
                        sample.SurfaceType == MapSurfaceType.Highway ||
                        sample.SurfaceType == MapSurfaceType.BridgeDeck ||
                        sample.SurfaceType == MapSurfaceType.Ramp ||
                        (sample.Flags & MapSurfaceFlags.Road) != 0;
                    if (roadLike || math.max(0f, sample.SlopeDegrees) <= 18f)
                    {
                        traversable = true;
                        break;
                    }
                }

                if (!traversable)
                    return false;
            }
        }

        return true;
    }

    private static bool IsEntityDescendantOf(
        EntityManager entityManager,
        Entity entity,
        Entity expectedAncestor)
    {
        Entity current = entity;
        for (int depth = 0; depth < 32 && current != Entity.Null; depth++)
        {
            if (current == expectedAncestor)
                return true;
            if (!entityManager.HasComponent<Parent>(current))
                return false;
            current = entityManager.GetComponentData<Parent>(current).Value;
        }

        return false;
    }

    private static void ValidateReadyOperationMapOrchestrationAllocatesZeroBytes(
        MenuBootstrapView menu,
        MatchSceneView match,
        World world,
        Entity sceneEntity,
        IReadOnlyList<Entity> resolvedSectionEntities,
        StaticMapPresentationStreamer staticStreamer)
    {
        FieldInfo boundField = typeof(MatchSceneView).GetField(
            "matchRuntimeBound",
            BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo loaderField = typeof(MatchSceneView).GetField(
            "operationMapSceneLoadingSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(boundField, Is.Not.Null);
        Assert.That(loaderField, Is.Not.Null);
        Assert.That((bool)boundField.GetValue(match), Is.True,
            "The production Match route must stop polling map loading after binding.");
        Assert.That(loaderField.GetValue(match), Is.Not.Null);
        Assert.That(match.OperationMapContentReady, Is.True);
        Assert.That(match.OperationMapReadinessPublicationAvailable, Is.True);
        MenuBootstrapCompositionSystemHelper menuBootstrap =
            ResolveMenuBootstrapSystem(menu);

        EntityManager entityManager = world.EntityManager;
        int expectedSectionEntityCount =
            CountEntitiesForSections(entityManager, resolvedSectionEntities);
        Assert.That(expectedSectionEntityCount, Is.GreaterThan(0));

        const int WarmupIterations = 256;
        const int MeasuredIterations = 2048;
        for (int iteration = 0; iteration < WarmupIterations; iteration++)
        {
            menuBootstrap.UpdateStaticMapPresentationForLoadedMatch(
                isMatchRoute: true,
                match);
        }

        long maximumInvocationBytes = 0;
        int firstAllocatingIteration = -1;
        long firstAllocationBytes = 0;
        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < MeasuredIterations; iteration++)
        {
            long invocationStart = GC.GetAllocatedBytesForCurrentThread();
            menuBootstrap.UpdateStaticMapPresentationForLoadedMatch(
                isMatchRoute: true,
                match);
            long invocationBytes =
                GC.GetAllocatedBytesForCurrentThread() - invocationStart;
            if (invocationBytes > maximumInvocationBytes)
                maximumInvocationBytes = invocationBytes;
            if (invocationBytes > 0 && firstAllocatingIteration < 0)
            {
                firstAllocatingIteration = iteration;
                firstAllocationBytes = invocationBytes;
            }
        }
        long allocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        Assert.That(allocatedBytes, Is.Zero,
            "Ready operation-map orchestration must allocate 0 B after warmup.");
        Assert.That(maximumInvocationBytes, Is.Zero,
            $"First allocating iteration={firstAllocatingIteration}, " +
            $"bytes={firstAllocationBytes}.");
        Assert.That(match.OperationMapContentReady, Is.True);
        Assert.That(entityManager.Exists(sceneEntity), Is.True);
        Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity), Is.True);
        Assert.That(
            CountEntitiesForSections(entityManager, resolvedSectionEntities),
            Is.EqualTo(expectedSectionEntityCount));
        AssertSinglePublishedOperationMapRoot(entityManager);
        Assert.That(staticStreamer.DrainComplete, Is.False);
        Assert.That(staticStreamer.PendingOperationCount, Is.Zero);
        Assert.That(staticStreamer.HasActiveOperation, Is.False);
        Debug.Log(
            $"[OperationMapPackedAllocation] warmup={WarmupIterations} " +
            $"samples={MeasuredIterations} allocatedBytes={allocatedBytes} " +
            $"maximumInvocationBytes={maximumInvocationBytes} " +
            $"sectionEntities={expectedSectionEntityCount}");
    }

    private static IEnumerator ValidateCameraTraversalKeepsEntitySceneResident(
        Aph805MenuMatchMenuLifecyclePlayModeTests.TransitionContext route,
        Entity sceneEntity,
        IReadOnlyList<Entity> resolvedSectionEntities,
        StaticMapPresentationStreamer staticStreamer,
        bool constrainTravelToMapEnvelope,
        bool validateSimulationState)
    {
        World world = route.World;
        EntityManager entityManager = world.EntityManager;
        Camera worldCamera = route.Match.WorldCamera;
        Assert.That(worldCamera, Is.Not.Null);
        Entity gameplayStateEntity = Entity.Null;
        RuntimeGameplayStateComponent originalGameplayState = default;
        bool gameplaySimulationPaused = false;
        EntitiesGraphicsSystem graphicsSystem =
            world.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        Assert.That(graphicsSystem, Is.Not.Null);

        Entity[] expectedEntities =
            GetSectionTaggedEntities(entityManager, resolvedSectionEntities);
        Entity[] expectedSceneRequests =
            GetEntitiesWithComponent<RequestSceneLoaded>(entityManager);
        string[] expectedLoadedScenes = GetLoadedSceneSignatures();
        int expectedSourceSceneLoads = route.Match.OperationMapSourceSceneLoadOperationCount;
        int expectedManifestLoads = route.Match.OperationMapPresentationManifestLoadOperationCount;
        int expectedEntitySceneLoads = route.Match.OperationMapPackedEntitySceneLoadRequestCount;
        int expectedSourceSceneUnloads = route.Match.OperationMapSourceSceneUnloadOperationCount;
        int expectedEntitySceneUnloads = route.Match.OperationMapPackedEntitySceneUnloadRequestCount;
        int expectedStaticStreamerOperations = staticStreamer.StartedOperationCount;
        Bounds mapBounds =
            CalculateSectionRenderBounds(entityManager, resolvedSectionEntities);
        Assert.That(expectedEntities.Length, Is.GreaterThan(0));

        Vector3 originalPosition = worldCamera.transform.position;
        Quaternion originalRotation = worldCamera.transform.rotation;
        bool originalOrthographic = worldCamera.orthographic;
        float originalOrthographicSize = worldCamera.orthographicSize;
        float originalNearClip = worldCamera.nearClipPlane;
        float originalFarClip = worldCamera.farClipPlane;
        ShadowQuality originalShadowQuality = QualitySettings.shadows;
        Camera[] cameras = Camera.allCameras;
        var cameraEnabledStates = new bool[cameras.Length];
        try
        {
            for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
            {
                cameraEnabledStates[cameraIndex] = cameras[cameraIndex].enabled;
                if (cameras[cameraIndex] != worldCamera)
                    cameras[cameraIndex].enabled = false;
            }

            worldCamera.enabled = true;
            QualitySettings.shadows = ShadowQuality.Disable;
            float mapSpan = math.max(mapBounds.size.x, mapBounds.size.z);
            Vector3 center = mapBounds.center;
            float cameraHeight = originalPosition.y;
            if (!constrainTravelToMapEnvelope)
            {
                worldCamera.orthographic = true;
                worldCamera.nearClipPlane = 0.1f;
                cameraHeight = mapBounds.max.y + math.max(100f, mapSpan);
                worldCamera.orthographicSize = math.max(
                    mapBounds.extents.z,
                    mapBounds.extents.x / math.max(0.1f, worldCamera.aspect)) * 0.35f;
                worldCamera.farClipPlane = math.max(1000f, mapSpan * 4f);
                worldCamera.transform.SetPositionAndRotation(
                    new Vector3(center.x, cameraHeight, center.z),
                    Quaternion.Euler(90f, 0f, 0f));
                worldCamera.ResetProjectionMatrix();
            }
            var nearSample = new EntitiesGraphicsCullingSample();
            yield return CaptureEntitiesGraphicsCullingSample(
                graphicsSystem,
                worldCamera,
                nearSample,
                "detail");
            AssertCameraTraversalResidency(
                world,
                entityManager,
                sceneEntity,
                resolvedSectionEntities,
                expectedEntities,
                expectedSceneRequests,
                expectedLoadedScenes,
                staticStreamer,
                route.Match,
                expectedSourceSceneLoads,
                expectedManifestLoads,
                expectedEntitySceneLoads,
                expectedSourceSceneUnloads,
                expectedEntitySceneUnloads,
                expectedStaticStreamerOperations,
                "detail");
            PackedSimulationSnapshot simulationBefore = null;
            if (validateSimulationState)
            {
                using (EntityQuery gameplayStateQuery = entityManager.CreateEntityQuery(
                           ComponentType.ReadWrite<RuntimeGameplayStateComponent>()))
                {
                    Assert.That(gameplayStateQuery.CalculateEntityCount(), Is.EqualTo(1));
                    gameplayStateEntity = gameplayStateQuery.GetSingletonEntity();
                    originalGameplayState = entityManager.GetComponentData<
                        RuntimeGameplayStateComponent>(gameplayStateEntity);
                    RuntimeGameplayStateComponent quiescentGameplayState =
                        originalGameplayState;
                    quiescentGameplayState.SimulationActive = 0;
                    entityManager.SetComponentData(
                        gameplayStateEntity,
                        quiescentGameplayState);
                }
                gameplaySimulationPaused = true;
                yield return null;
                entityManager.CompleteAllTrackedJobs();
                simulationBefore = CapturePackedSimulationSnapshot(entityManager);
            }

            Vector3 travelPosition = constrainTravelToMapEnvelope
                ? new Vector3(
                    center.x + mapBounds.extents.x * 0.15f,
                    cameraHeight,
                    center.z + mapBounds.extents.z * 0.15f)
                : new Vector3(
                    center.x + mapSpan * 6f,
                    cameraHeight,
                    center.z + mapSpan * 6f);
            worldCamera.transform.SetPositionAndRotation(
                travelPosition,
                constrainTravelToMapEnvelope
                    ? originalRotation
                    : Quaternion.Euler(-90f, 0f, 0f));
            string travelCheckpoint = constrainTravelToMapEnvelope ? "map-travel" : "off-map";
            var offMapSample = new EntitiesGraphicsCullingSample();
            yield return CaptureEntitiesGraphicsCullingSample(
                graphicsSystem,
                worldCamera,
                offMapSample,
                travelCheckpoint);
            AssertCameraTraversalResidency(
                world,
                entityManager,
                sceneEntity,
                resolvedSectionEntities,
                expectedEntities,
                expectedSceneRequests,
                expectedLoadedScenes,
                staticStreamer,
                route.Match,
                expectedSourceSceneLoads,
                expectedManifestLoads,
                expectedEntitySceneLoads,
                expectedSourceSceneUnloads,
                expectedEntitySceneUnloads,
                expectedStaticStreamerOperations,
                travelCheckpoint);
            if (validateSimulationState)
            {
                PackedSimulationSnapshot simulationAfter =
                    CapturePackedSimulationSnapshot(entityManager);
                AssertPackedSimulationSnapshotUnchanged(
                    simulationBefore,
                    simulationAfter);
                Debug.Log(
                    "[PackedCameraSimulationState] result=Passed " +
                    $"simulationEntities={simulationAfter.SimulationEntityCount} " +
                    $"buildings={simulationAfter.BuildingCount} " +
                    $"vehicles={simulationAfter.VehicleCount} " +
                    $"healthOwners={simulationAfter.HealthOwnerCount} " +
                    $"canonicalStates={simulationAfter.CanonicalStates.Length}");
            }

            Assert.That(nearSample.MaximumBatchCount, Is.GreaterThan(0));
            Assert.That(nearSample.MaximumChunkTotal, Is.GreaterThan(0));
            Assert.That(nearSample.MaximumRenderedInstanceCount, Is.GreaterThan(0));
            Assert.That(nearSample.MaximumDrawCommandCount, Is.GreaterThan(0));
            Assert.That(nearSample.MaximumInstanceTests, Is.GreaterThan(0));
            Assert.That(nearSample.MaximumCulledChunkCount, Is.GreaterThan(0));
            Assert.That(offMapSample.MaximumCameraMoveDistance, Is.GreaterThan(0f));
            if (!constrainTravelToMapEnvelope)
            {
                Assert.That(
                    offMapSample.MinimumRenderedInstanceCount,
                    Is.LessThan(nearSample.MaximumRenderedInstanceCount),
                    "Camera traversal did not change Entities Graphics visibility.");
            }

            int mapLodEntityCount =
                CountSectionEntitiesWithComponent<MeshLODComponent>(
                    entityManager,
                    resolvedSectionEntities);
            Debug.Log(
                $"[PackedCameraCulling] mapLodEntities={mapLodEntityCount} " +
                $"lodChunksTested=" +
                $"{math.max(nearSample.MaximumLodChunksTested, offMapSample.MaximumLodChunksTested)} " +
                "lodTransitionAcceptance=deferred-to-android");
            Debug.Log(
                "[PackedCameraTravelOperations] result=Passed " +
                "mapSceneOperations=0 addressablesOperations=0 " +
                "staticStreamerOperations=0");
        }
        finally
        {
            if (gameplaySimulationPaused &&
                entityManager.Exists(gameplayStateEntity))
            {
                entityManager.SetComponentData(
                    gameplayStateEntity,
                    originalGameplayState);
            }
            worldCamera.transform.SetPositionAndRotation(
                originalPosition,
                originalRotation);
            worldCamera.orthographic = originalOrthographic;
            worldCamera.orthographicSize = originalOrthographicSize;
            worldCamera.nearClipPlane = originalNearClip;
            worldCamera.farClipPlane = originalFarClip;
            QualitySettings.shadows = originalShadowQuality;
            for (int cameraIndex = 0; cameraIndex < cameras.Length; cameraIndex++)
            {
                if (cameras[cameraIndex] != null)
                    cameras[cameraIndex].enabled = cameraEnabledStates[cameraIndex];
            }
        }
    }

    private static PackedSimulationSnapshot CapturePackedSimulationSnapshot(
        EntityManager entityManager)
    {
        using EntityQuery simulationQuery = entityManager.CreateEntityQuery(
            new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<Faction>(),
                    ComponentType.ReadOnly<UnitHealth>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });
        using NativeArray<Entity> simulationEntities =
            simulationQuery.ToEntityArray(Allocator.Temp);
        var rows = new List<string>(simulationEntities.Length);
        int buildingCount = 0;
        int vehicleCount = 0;
        for (int index = 0; index < simulationEntities.Length; index++)
        {
            Entity entity = simulationEntities[index];
            UnitGrid grid = entityManager.GetComponentData<UnitGrid>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            UnitHealth health = entityManager.GetComponentData<UnitHealth>(entity);
            string identity;
            int destroyed = -1;
            int presentationState = -1;
            int lastProductionRequest = -1;
            if (entityManager.HasComponent<OperationMapBuildingComponent>(entity))
            {
                OperationMapBuildingComponent building =
                    entityManager.GetComponentData<OperationMapBuildingComponent>(entity);
                identity = $"building:{building.StableId}:{building.PlacementIndex}";
                buildingCount++;
                if (entityManager.HasComponent<OperationMapBuildingDestroyedComponent>(entity))
                {
                    destroyed = entityManager.IsComponentEnabled<
                        OperationMapBuildingDestroyedComponent>(entity)
                        ? 1
                        : 0;
                }
                if (entityManager.HasComponent<OperationMapBuildingPresentation>(entity))
                {
                    presentationState = entityManager
                        .GetComponentData<OperationMapBuildingPresentation>(entity)
                        .State;
                }
                if (entityManager.HasComponent<
                        OperationMapBuildingProductionQueueComponent>(entity))
                {
                    lastProductionRequest = entityManager
                        .GetComponentData<OperationMapBuildingProductionQueueComponent>(entity)
                        .LastRequestId;
                }
            }
            else if (entityManager.HasComponent<
                         OperationMapAuthoredVehiclePresentation>(entity))
            {
                OperationMapAuthoredVehiclePresentation vehicle = entityManager
                    .GetComponentData<OperationMapAuthoredVehiclePresentation>(entity);
                identity = $"vehicle:{vehicle.PlacementIndex}:{vehicle.FactionId}";
                vehicleCount++;
            }
            else
            {
                identity = $"entity:{entity.Index}:{entity.Version}";
            }

            rows.Add(
                $"{identity}|grid={grid.Cell.x},{grid.Cell.y}|faction={faction.Id}|" +
                $"health={health.Current},{health.Max}|destroyed={destroyed}|" +
                $"presentation={presentationState}|production={lastProductionRequest}");
        }
        rows.Sort(StringComparer.Ordinal);

        using EntityQuery buildingQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapBuildingComponent>());
        using EntityQuery vehicleQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>());
        using EntityQuery healthQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<UnitHealth>());
        using EntityQuery canonicalStateQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRenderCanonicalStateComponent>());
        Assert.That(canonicalStateQuery.CalculateEntityCount(), Is.EqualTo(1));
        Entity canonicalStateOwner = canonicalStateQuery.GetSingletonEntity();
        DynamicBuffer<OperationMapRenderCanonicalStateComponent> canonicalStateBuffer =
            entityManager.GetBuffer<OperationMapRenderCanonicalStateComponent>(
                canonicalStateOwner,
                isReadOnly: true);
        using NativeArray<OperationMapRenderCanonicalStateComponent> canonicalStates =
            canonicalStateBuffer.ToNativeArray(Allocator.Temp);

        return new PackedSimulationSnapshot
        {
            SimulationEntityCount = simulationEntities.Length,
            BuildingCount = buildingQuery.CalculateEntityCount(),
            VehicleCount = vehicleQuery.CalculateEntityCount(),
            HealthOwnerCount = healthQuery.CalculateEntityCount(),
            GameplayState =
                GetSingletonComponent<RuntimeGameplayStateComponent>(entityManager),
            EntityStateRows = rows.ToArray(),
            CanonicalStates = canonicalStates.ToArray()
        };
    }

    private static void AssertPackedSimulationSnapshotUnchanged(
        PackedSimulationSnapshot before,
        PackedSimulationSnapshot after)
    {
        Assert.That(before, Is.Not.Null);
        Assert.That(after.SimulationEntityCount, Is.EqualTo(before.SimulationEntityCount));
        Assert.That(after.BuildingCount, Is.EqualTo(before.BuildingCount));
        Assert.That(after.VehicleCount, Is.EqualTo(before.VehicleCount));
        Assert.That(after.HealthOwnerCount, Is.EqualTo(before.HealthOwnerCount));
        Assert.That(after.EntityStateRows, Is.EqualTo(before.EntityStateRows));
        Assert.That(after.CanonicalStates, Is.EqualTo(before.CanonicalStates));
        Assert.That(after.GameplayState.PlayRequested, Is.EqualTo(before.GameplayState.PlayRequested));
        Assert.That(after.GameplayState.SimulationActive, Is.EqualTo(before.GameplayState.SimulationActive));
        Assert.That(after.GameplayState.SelectionModeActive, Is.EqualTo(before.GameplayState.SelectionModeActive));
        Assert.That(after.GameplayState.BuildModeActive, Is.EqualTo(before.GameplayState.BuildModeActive));
        Assert.That(after.GameplayState.FullscreenMapOpen, Is.EqualTo(before.GameplayState.FullscreenMapOpen));
        Assert.That(after.GameplayState.FullscreenMapIsoMode, Is.EqualTo(before.GameplayState.FullscreenMapIsoMode));
        Assert.That(after.GameplayState.SuppressNextWorldClick, Is.EqualTo(before.GameplayState.SuppressNextWorldClick));
        Assert.That(after.GameplayState.PlayerAutoModeEnabled, Is.EqualTo(before.GameplayState.PlayerAutoModeEnabled));
    }

    private sealed class PackedSimulationSnapshot
    {
        public int SimulationEntityCount;
        public int BuildingCount;
        public int VehicleCount;
        public int HealthOwnerCount;
        public RuntimeGameplayStateComponent GameplayState;
        public string[] EntityStateRows;
        public OperationMapRenderCanonicalStateComponent[] CanonicalStates;
    }

    private static IEnumerator CaptureEntitiesGraphicsCullingSample(
        EntitiesGraphicsSystem graphicsSystem,
        Camera camera,
        EntitiesGraphicsCullingSample sample,
        string checkpoint)
    {
        const int warmupFrames = 2;
        const int measuredFrames = 6;
        sample.MinimumRenderedInstanceCount = int.MaxValue;
        RenderTexture destination = RenderTexture.GetTemporary(
            320,
            240,
            24,
            RenderTextureFormat.ARGB32);
        try
        {
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest
            {
                destination = destination
            };
            Assert.That(
                UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(
                    camera,
                    request),
                Is.True,
                "The active render pipeline does not support explicit camera requests.");

            for (int frame = 0; frame < warmupFrames + measuredFrames; frame++)
            {
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(
                    camera,
                    request);
                yield return null;
                EntitiesGraphicsStats stats = graphicsSystem.Stats;
                sample.MaximumCameraMoveDistance =
                    math.max(sample.MaximumCameraMoveDistance, stats.CameraMoveDistance);
                if (frame < warmupFrames)
                    continue;

                int batchCount = math.max(0, stats.BatchCount);
                int chunkTotal = math.max(0, stats.ChunkTotal);
                int instanceTests = math.max(0, stats.InstanceTests);
                int drawCommandCount = math.max(0, stats.DrawCommandCount);
                int lodChunksTested = math.max(0, stats.LodChunksTested);
                int renderedInstanceCount =
                    math.max(0, stats.RenderedInstanceCount);
                int culledChunkCount = math.max(
                    0,
                    stats.ChunkCountAnyLod -
                    stats.ChunkCountFullyIn -
                    stats.ChunkCountInstancesProcessed);
                sample.MaximumBatchCount =
                    math.max(sample.MaximumBatchCount, batchCount);
                sample.MaximumChunkTotal =
                    math.max(sample.MaximumChunkTotal, chunkTotal);
                sample.MaximumInstanceTests =
                    math.max(sample.MaximumInstanceTests, instanceTests);
                sample.MaximumDrawCommandCount =
                    math.max(sample.MaximumDrawCommandCount, drawCommandCount);
                sample.MaximumLodChunksTested =
                    math.max(sample.MaximumLodChunksTested, lodChunksTested);
                sample.MaximumCulledChunkCount =
                    math.max(sample.MaximumCulledChunkCount, culledChunkCount);
                sample.MaximumRenderedInstanceCount =
                    math.max(sample.MaximumRenderedInstanceCount, renderedInstanceCount);
                sample.MinimumRenderedInstanceCount =
                    math.min(sample.MinimumRenderedInstanceCount, renderedInstanceCount);
            }
        }
        finally
        {
            RenderTexture.ReleaseTemporary(destination);
        }
        Debug.Log(
            $"[PackedCameraCulling] checkpoint={checkpoint} " +
            $"batches={sample.MaximumBatchCount} chunks={sample.MaximumChunkTotal} " +
            $"instanceTests={sample.MaximumInstanceTests} " +
            $"culledChunks={sample.MaximumCulledChunkCount} " +
            $"rendered={sample.MinimumRenderedInstanceCount}.." +
            $"{sample.MaximumRenderedInstanceCount} " +
            $"drawCommands={sample.MaximumDrawCommandCount} " +
            $"lodChunksTested={sample.MaximumLodChunksTested} " +
            $"cameraMove={sample.MaximumCameraMoveDistance:R}");
    }

    private static void AssertCameraTraversalResidency(
        World world,
        EntityManager entityManager,
        Entity sceneEntity,
        IReadOnlyList<Entity> resolvedSectionEntities,
        IReadOnlyList<Entity> expectedEntities,
        IReadOnlyList<Entity> expectedSceneRequests,
        IReadOnlyList<string> expectedLoadedScenes,
        StaticMapPresentationStreamer staticStreamer,
        MatchSceneView match,
        int expectedSourceSceneLoads,
        int expectedManifestLoads,
        int expectedEntitySceneLoads,
        int expectedSourceSceneUnloads,
        int expectedEntitySceneUnloads,
        int expectedStaticStreamerOperations,
        string checkpoint)
    {
        Assert.That(entityManager.Exists(sceneEntity), Is.True, checkpoint);
        Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity), Is.True, checkpoint);
        Assert.That(
            GetResolvedSectionEntities(entityManager, sceneEntity),
            Is.EqualTo(resolvedSectionEntities),
            checkpoint);
        Assert.That(
            GetSectionTaggedEntities(entityManager, resolvedSectionEntities),
            Is.EqualTo(expectedEntities),
            checkpoint);
        Assert.That(
            GetEntitiesWithComponent<RequestSceneLoaded>(entityManager),
            Is.EqualTo(expectedSceneRequests),
            checkpoint);
        Assert.That(GetLoadedSceneSignatures(), Is.EqualTo(expectedLoadedScenes), checkpoint);
        AssertSinglePublishedOperationMapRoot(entityManager);
        Assert.That(staticStreamer.DrainComplete, Is.False, checkpoint);
        Assert.That(staticStreamer.PendingOperationCount, Is.Zero, checkpoint);
        Assert.That(staticStreamer.HasActiveOperation, Is.False, checkpoint);
        Assert.That(
            match.OperationMapSourceSceneLoadOperationCount,
            Is.EqualTo(expectedSourceSceneLoads),
            checkpoint);
        Assert.That(
            match.OperationMapPresentationManifestLoadOperationCount,
            Is.EqualTo(expectedManifestLoads),
            checkpoint);
        Assert.That(
            match.OperationMapPackedEntitySceneLoadRequestCount,
            Is.EqualTo(expectedEntitySceneLoads),
            checkpoint);
        Assert.That(
            match.OperationMapSourceSceneUnloadOperationCount,
            Is.EqualTo(expectedSourceSceneUnloads),
            checkpoint);
        Assert.That(
            match.OperationMapPackedEntitySceneUnloadRequestCount,
            Is.EqualTo(expectedEntitySceneUnloads),
            checkpoint);
        Assert.That(
            staticStreamer.StartedOperationCount,
            Is.EqualTo(expectedStaticStreamerOperations),
            checkpoint);
    }

    private static Entity[] GetSectionTaggedEntities(
        EntityManager entityManager,
        IReadOnlyList<Entity> sectionEntities)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<SceneTag>() },
            Options = EntityQueryOptions.IncludeDisabledEntities |
                      EntityQueryOptions.IncludePrefab
        });
        var entities = new List<Entity>();
        for (int sectionIndex = 0; sectionIndex < sectionEntities.Count; sectionIndex++)
        {
            query.SetSharedComponentFilter(new SceneTag
            {
                SceneEntity = sectionEntities[sectionIndex]
            });
            using NativeArray<Entity> section =
                query.ToEntityArray(Allocator.Temp);
            entities.AddRange(section);
        }
        query.ResetFilter();
        entities.Sort(CompareEntities);
        return entities.ToArray();
    }

    private static Entity[] GetEntitiesWithComponent<T>(EntityManager entityManager)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<T>() },
            Options = EntityQueryOptions.IncludeDisabledEntities |
                      EntityQueryOptions.IncludePrefab
        });
        using NativeArray<Entity> values = query.ToEntityArray(Allocator.Temp);
        Entity[] entities = values.ToArray();
        Array.Sort(entities, CompareEntities);
        return entities;
    }

    private static int CountSectionEntitiesWithComponent<T>(
        EntityManager entityManager,
        IReadOnlyList<Entity> sectionEntities)
        where T : unmanaged, IComponentData
    {
        using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<T>(),
                ComponentType.ReadOnly<SceneTag>()
            },
            Options = EntityQueryOptions.IncludeDisabledEntities |
                      EntityQueryOptions.IncludePrefab
        });
        int count = 0;
        for (int sectionIndex = 0; sectionIndex < sectionEntities.Count; sectionIndex++)
        {
            query.SetSharedComponentFilter(new SceneTag
            {
                SceneEntity = sectionEntities[sectionIndex]
            });
            count += query.CalculateEntityCount();
        }
        query.ResetFilter();
        return count;
    }

    private static Bounds CalculateSectionRenderBounds(
        EntityManager entityManager,
        IReadOnlyList<Entity> sectionEntities)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<WorldRenderBounds>(),
                ComponentType.ReadOnly<SceneTag>()
            },
            Options = EntityQueryOptions.IncludeDisabledEntities |
                      EntityQueryOptions.IncludePrefab
        });
        bool hasBounds = false;
        float3 minimum = new(float.MaxValue);
        float3 maximum = new(float.MinValue);
        for (int sectionIndex = 0; sectionIndex < sectionEntities.Count; sectionIndex++)
        {
            query.SetSharedComponentFilter(new SceneTag
            {
                SceneEntity = sectionEntities[sectionIndex]
            });
            using NativeArray<WorldRenderBounds> bounds =
                query.ToComponentDataArray<WorldRenderBounds>(Allocator.Temp);
            for (int boundsIndex = 0; boundsIndex < bounds.Length; boundsIndex++)
            {
                minimum = math.min(minimum, bounds[boundsIndex].Value.Min);
                maximum = math.max(maximum, bounds[boundsIndex].Value.Max);
                hasBounds = true;
            }
        }
        query.ResetFilter();
        Assert.That(hasBounds, Is.True, "Candidate sections have no world render bounds.");
        return new Bounds(
            (Vector3)((minimum + maximum) * 0.5f),
            (Vector3)(maximum - minimum));
    }

    private static string[] GetLoadedSceneSignatures()
    {
        var signatures = new string[SceneManager.sceneCount];
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            signatures[sceneIndex] =
                $"{scene.handle}:{scene.path}:{scene.name}:{scene.isLoaded}";
        }
        Array.Sort(signatures, StringComparer.Ordinal);
        return signatures;
    }

    private static int CompareEntities(Entity left, Entity right)
    {
        int index = left.Index.CompareTo(right.Index);
        return index != 0 ? index : left.Version.CompareTo(right.Version);
    }

    private sealed class EntitiesGraphicsCullingSample
    {
        public int MaximumBatchCount;
        public int MaximumChunkTotal;
        public int MaximumInstanceTests;
        public int MaximumDrawCommandCount;
        public int MaximumCulledChunkCount;
        public int MaximumLodChunksTested;
        public int MaximumRenderedInstanceCount;
        public int MinimumRenderedInstanceCount;
        public float MaximumCameraMoveDistance;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        MatchSceneView.SetEditorOperationMapCatalogOverrideForTests(null);
        yield return Aph805MenuMatchMenuLifecyclePlayModeTests.EnsureMatchIsUnloaded();
    }

    private static OperationMapCatalogConfig CreateCandidateCatalog(
        OperationMapDefinition definition)
    {
        Assert.That(definition, Is.Not.Null);
        var catalog = ScriptableObject.CreateInstance<OperationMapCatalogConfig>();
        var contentPack = new OperationMapContentPackConfig(
            "opmap-pack." + definition.OperationMapId.Substring("opmap.".Length),
            OperationMapDeliveryKind.BuiltInLocal,
            definition.ContentVersion,
            definition.ContentHash);
        var entry = new OperationMapCatalogEntryConfig(definition, contentPack);
        SetPrivateField(catalog, "definitions", new[] { definition });
        SetPrivateField(catalog, "entries", new[] { entry });
        Assert.That(catalog.TryValidate(out string error), Is.True, error);
        return catalog;
    }

    private static StaticMapPresentationStreamer ResolveStaticPresentationStreamer(
        MenuBootstrapView menu)
    {
        MenuBootstrapCompositionSystemHelper helper =
            ResolveMenuBootstrapSystem(menu);
        FieldInfo streamerField = typeof(MenuBootstrapCompositionSystemHelper).GetField(
            "staticMapPresentationStreamer",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(streamerField, Is.Not.Null);
        return (StaticMapPresentationStreamer)streamerField.GetValue(helper);
    }

    private static MenuBootstrapCompositionSystemHelper ResolveMenuBootstrapSystem(
        MenuBootstrapView menu)
    {
        Assert.That(menu, Is.Not.Null);
        FieldInfo helperField = typeof(MenuBootstrapView).GetField(
            "menuBootstrapSystem",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(helperField, Is.Not.Null);
        var helper = helperField.GetValue(menu) as MenuBootstrapCompositionSystemHelper;
        Assert.That(helper, Is.Not.Null);
        return helper;
    }

    private static int CountEntitiesForSections(
        EntityManager entityManager,
        IReadOnlyList<Entity> sectionEntities)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<SceneTag>() },
            Options = EntityQueryOptions.IncludeDisabledEntities |
                      EntityQueryOptions.IncludePrefab
        });
        int count = 0;
        for (int sectionIndex = 0; sectionIndex < sectionEntities.Count; sectionIndex++)
        {
            query.SetSharedComponentFilter(new SceneTag
            {
                SceneEntity = sectionEntities[sectionIndex]
            });
            count += query.CalculateEntityCount();
        }
        query.ResetFilter();
        return count;
    }

    private static void AssertSinglePublishedOperationMapRoot(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRootComponent>(),
            ComponentType.ReadOnly<OperationMapMetadataComponent>(),
            ComponentType.ReadOnly<OperationMapReadinessComponent>());
        Assert.That(query.CalculateEntityCount(), Is.EqualTo(1));
    }

    private static void AssertNoPublishedOperationMapMetadata(EntityManager entityManager)
    {
        using EntityQuery rootQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRootComponent>());
        using EntityQuery metadataQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapMetadataComponent>());
        using EntityQuery readinessQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapReadinessComponent>());
        Assert.That(rootQuery.CalculateEntityCount(), Is.Zero);
        Assert.That(metadataQuery.CalculateEntityCount(), Is.Zero);
        Assert.That(readinessQuery.CalculateEntityCount(), Is.Zero);
    }

    private static void AssertAcceptedAuthoringScenesNotLoaded()
    {
        Assert.That(
            SceneManager.GetSceneByPath(AcceptedOperationMapScenePath).isLoaded,
            Is.False,
            "The accepted operation-map authoring scene must not load at runtime.");
        Assert.That(
            SceneManager.GetSceneByPath(AcceptedSubScenePath).isLoaded,
            Is.False,
            "The accepted authoring SubScene must not load at runtime.");
    }

    private static void AssertNoManagedMapVisualOwnership(
        MatchBootstrapCompositionSystemHelper matchBootstrap)
    {
        Assert.That(
            matchBootstrap.ManagedRuntimeBuildingCount,
            Is.Zero,
            "Permanent EntityScene buildings must not enter the managed runtime-building registry.");
        Assert.That(
            matchBootstrap.RuntimeBuildingEntityLinkCount,
            Is.Zero,
            "Permanent EntityScene buildings must not enter the managed entity-link registry.");
        Assert.That(
            UnityEngine.Object.FindObjectsByType<RuntimeBuildingEntityLink>(
                FindObjectsInactive.Include),
            Is.Empty,
            "EntityScene presentation must not create RuntimeBuildingEntityLink components.");
        Assert.That(
            UnityEngine.Object.FindObjectsByType<MapAuthoredBuildingVisualComponent>(
                FindObjectsInactive.Include),
            Is.Empty,
            "EntityScene presentation must not create legacy authored-building visual GameObjects.");
        Assert.That(
            UnityEngine.Object.FindObjectsByType<OperationMapBuildingAuthoring>(
                FindObjectsInactive.Include),
            Is.Empty,
            "Candidate building authoring GameObjects must not survive baking into runtime.");
        Assert.That(
            UnityEngine.Object.FindObjectsByType<OperationMapBuildingAttachmentAuthoring>(
                FindObjectsInactive.Include),
            Is.Empty,
            "Candidate attachment authoring GameObjects must not survive baking into runtime.");

        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include);
        for (int index = 0; index < transforms.Length; index++)
        {
            Transform current = transforms[index];
            if (current == null)
                continue;

            Assert.That(
                current.name,
                Is.Not.EqualTo("BuildingPlacementVisualPool"),
                "Permanent EntityScene map visuals must not create building GameObject pool entries.");
            if (current.name != "RuntimeCity" &&
                current.name != "RuntimeBuildings" &&
                current.name != "RuntimeBlockers")
                continue;

            Renderer[] renderers = current.GetComponentsInChildren<Renderer>(true);
            Assert.That(
                renderers,
                Is.Empty,
                $"{current.name} must remain an empty gameplay shell for EntityScene presentation.");
        }
    }

    private static void ValidateBuildingDestructionUsesBakedEntitiesOnly(
        MatchBootstrapCompositionSystemHelper matchBootstrap,
        World world)
    {
        EntityManager entityManager = world.EntityManager;
        using EntityQuery buildingQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<UnitHealth>(),
            ComponentType.ReadOnly<OperationMapBuildingComponent>(),
            ComponentType.ReadWrite<OperationMapBuildingPresentation>());
        using NativeArray<Entity> buildings =
            buildingQuery.ToEntityArray(Allocator.Temp);

        Entity building = Entity.Null;
        OperationMapBuildingPresentation presentation = default;
        for (int index = 0; index < buildings.Length; index++)
        {
            Entity candidate = buildings[index];
            OperationMapBuildingComponent buildingData =
                entityManager.GetComponentData<OperationMapBuildingComponent>(candidate);
            OperationMapBuildingPresentation candidatePresentation =
                entityManager.GetComponentData<OperationMapBuildingPresentation>(candidate);
            if (buildingData.BlockerPolicy !=
                    OperationMapBuildingBlockerPolicy.RubbleRemainsBlocked ||
                candidatePresentation.IntactVisualRoot == Entity.Null ||
                candidatePresentation.DestroyedVisualRoot == Entity.Null ||
                !entityManager.Exists(candidatePresentation.IntactVisualRoot) ||
                !entityManager.Exists(candidatePresentation.DestroyedVisualRoot))
            {
                continue;
            }

            building = candidate;
            presentation = candidatePresentation;
            break;
        }

        Assert.That(building, Is.Not.EqualTo(Entity.Null),
            "Packed candidate contains no complete destructible building presentation.");
        Assert.That(
            entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(building),
            Is.False);

        HashSet<int> gameObjectIdsBefore = CaptureLoadedGameObjectIds();
        int entityCountBefore = entityManager.UniversalQuery.CalculateEntityCount();
        entityManager.SetComponentData(building, new UnitHealth
        {
            Current = 0,
            Max = entityManager.GetComponentData<UnitHealth>(building).Max
        });

        SystemHandle handle =
            world.Unmanaged.GetExistingUnmanagedSystem<OperationMapBuildingDestructionSystem>();
        Assert.That(handle, Is.Not.EqualTo(SystemHandle.Null));
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<OperationMapBuildingDestructionSystem>(handle)
            .OnUpdate(ref state);
        state.Dependency.Complete();
        entityManager.CompleteAllTrackedJobs();

        OperationMapBuildingPresentation destroyedPresentation =
            entityManager.GetComponentData<OperationMapBuildingPresentation>(building);
        Assert.That(destroyedPresentation.State, Is.EqualTo(1));
        Assert.That(
            entityManager.IsComponentEnabled<OperationMapBuildingDestroyedComponent>(building),
            Is.True);
        Assert.That(
            entityManager.GetComponentData<LocalTransform>(presentation.IntactVisualRoot).Scale,
            Is.Zero);
        Assert.That(
            entityManager.GetComponentData<LocalTransform>(presentation.DestroyedVisualRoot).Scale,
            Is.EqualTo(presentation.DestroyedVisibleScale));
        Assert.That(
            entityManager.UniversalQuery.CalculateEntityCount(),
            Is.EqualTo(entityCountBefore),
            "Operation-map destruction must not instantiate replacement entities.");
        Assert.That(
            CaptureLoadedGameObjectIds(),
            Is.EquivalentTo(gameObjectIdsBefore),
            "Operation-map destruction must not instantiate or destroy GameObject replacements.");
        AssertNoManagedMapVisualOwnership(matchBootstrap);
    }

    private static HashSet<int> CaptureLoadedGameObjectIds()
    {
        GameObject[] gameObjects = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include);
        var ids = new HashSet<int>();
        for (int index = 0; index < gameObjects.Length; index++)
            ids.Add(gameObjects[index].GetEntityId().GetHashCode());
        return ids;
    }

    private static void SetPrivateField(object owner, string fieldName, object value)
    {
        FieldInfo field = owner.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'.");
        field.SetValue(owner, value);
    }

    private static bool SetSystemEnabled<T>(World world, bool enabled)
        where T : unmanaged, ISystem
    {
        SystemHandle handle = world.Unmanaged.GetExistingUnmanagedSystem<T>();
        Assert.That(handle, Is.Not.EqualTo(SystemHandle.Null),
            $"Required parity isolation system is missing: {typeof(T).FullName}");
        ref SystemState systemState =
            ref world.Unmanaged.ResolveSystemStateRef(handle);
        bool previous = systemState.Enabled;
        systemState.Enabled = enabled;
        return previous;
    }

    private sealed class FailReadinessOncePackedEntitySceneApi :
        IOperationMapEntitySceneApi
    {
        private readonly OperationMapEntitySceneApi inner = new();

        public int FailureCount { get; private set; }
        public Entity FailedSceneEntity { get; private set; }
        public Entity[] FailedSectionEntities { get; private set; } =
            Array.Empty<Entity>();

        public bool TryEnsureReady(
            string sceneGuid,
            string expectedOperationMapId,
            OperationMapRenderResidencyMode renderResidencyMode,
            ref Entity sceneEntity,
            ref bool ownsScene,
            out bool ready,
            out string error)
        {
            World world = World.DefaultGameObjectInjectionWorld;
            if (FailureCount == 0 &&
                world != null &&
                world.IsCreated &&
                sceneEntity != Entity.Null &&
                world.EntityManager.Exists(sceneEntity) &&
                SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
            {
                EntityManager entityManager = world.EntityManager;
                FailedSceneEntity = sceneEntity;
                FailedSectionEntities =
                    GetResolvedSectionEntities(entityManager, sceneEntity);
                FailureCount++;
                ready = false;
                bool unexpectedlyAccepted =
                    OperationMapEntityPresentationReadinessUtility.TryValidate(
                        entityManager,
                        sceneEntity,
                        expectedOperationMapId + ".forced-readiness-failure",
                        renderResidencyMode,
                        out error);
                Assert.That(unexpectedlyAccepted, Is.False);
                return false;
            }

            return inner.TryEnsureReady(
                sceneGuid,
                expectedOperationMapId,
                renderResidencyMode,
                ref sceneEntity,
                ref ownsScene,
                out ready,
                out error);
        }

        public bool TryReleaseOwned(
            ref Entity sceneEntity,
            ref bool ownsScene,
            ref bool releaseStarted,
            out bool complete,
            out string error)
        {
            return inner.TryReleaseOwned(
                ref sceneEntity,
                ref ownsScene,
                ref releaseStarted,
                out complete,
                out error);
        }
    }

    private static IEnumerator RunLoadCaptureUnloadCycle(
        OperationMapDefinition definition,
        TransformParityReport expected,
        int cycle)
    {
        Assert.That(definition, Is.Not.Null);
        Assert.That(definition.OperationMapId, Is.EqualTo(ExpectedOperationMapId));

        using var loader = new OperationMapSceneLoadingSceneSystemHelper();
        Assert.That(loader.TryStart(definition, out string startError), Is.True, startError);

        float deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (!loader.IsReady &&
               !loader.HasFailed &&
               Time.realtimeSinceStartup < deadline)
        {
            loader.Update();
            yield return null;
        }

        Assert.That(loader.HasFailed, Is.False, loader.Failure);
        Assert.That(loader.IsReady, Is.True,
            $"Candidate source scene did not become ready in cycle {cycle}.");
        Assert.That(loader.Manifest, Is.Null,
            "EntityScene presentation must not resolve a static presentation manifest.");

        World world = World.DefaultGameObjectInjectionWorld;
        Assert.That(world, Is.Not.Null);
        Assert.That(world.IsCreated, Is.True);

        var sceneGuid = new Hash128(
            definition.NavigationMetadata.AuthoredSubSceneGuid);
        Assert.That(sceneGuid.IsValid, Is.True);
        Entity sceneEntity = Entity.Null;
        deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            sceneEntity = SceneSystem.GetSceneEntity(
                world.Unmanaged,
                sceneGuid);
            if (sceneEntity != Entity.Null &&
                SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                break;
            yield return null;
        }

        Assert.That(sceneEntity, Is.Not.EqualTo(Entity.Null),
            $"Candidate EntityScene root was not created in cycle {cycle}.");
        Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity), Is.True,
            $"Candidate EntityScene did not finish streaming in cycle {cycle}.");

        Scene sourceScene = loader.SceneView.gameObject.scene;
        Assert.That(sourceScene.IsValid(), Is.True);
        Assert.That(sourceScene.isLoaded, Is.True);
        Entity[] resolvedSectionEntities = GetResolvedSectionEntities(
            world.EntityManager,
            sceneEntity);
        RuntimeCapture actual = Capture(world.EntityManager, sceneEntity);
        Compare(expected, actual, cycle);

        Assert.That(loader.TryBeginUnload(out string unloadError), Is.True, unloadError);
        deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (!loader.UnloadComplete &&
               !loader.HasFailed &&
               Time.realtimeSinceStartup < deadline)
        {
            loader.Update();
            yield return null;
        }

        Assert.That(loader.HasFailed, Is.False, loader.Failure);
        Assert.That(loader.UnloadComplete, Is.True,
            $"Candidate source scene did not unload in cycle {cycle}.");

        deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (Time.realtimeSinceStartup < deadline &&
               world.EntityManager.Exists(sceneEntity) &&
               SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
            yield return null;

        Assert.That(world.EntityManager.Exists(sceneEntity), Is.False,
            $"Candidate EntityScene metadata remained after cycle {cycle}.");
        for (int sectionIndex = 0; sectionIndex < resolvedSectionEntities.Length; sectionIndex++)
        {
            Assert.That(
                world.EntityManager.Exists(resolvedSectionEntities[sectionIndex]),
                Is.False,
                $"Candidate EntityScene section metadata {sectionIndex} remained after cycle {cycle}.");
        }
        Assert.That(sourceScene.isLoaded, Is.False,
            $"Candidate thin runtime-binding scene remained loaded after cycle {cycle}.");
    }

    private static Entity[] GetResolvedSectionEntities(
        EntityManager entityManager,
        Entity sceneEntity)
    {
        Assert.That(entityManager.HasBuffer<ResolvedSectionEntity>(sceneEntity), Is.True,
            "Loaded EntityScene has no resolved section buffer.");
        DynamicBuffer<ResolvedSectionEntity> sections =
            entityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
        Assert.That(sections.Length, Is.GreaterThan(0));

        var resolved = new Entity[sections.Length];
        for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
            resolved[sectionIndex] = sections[sectionIndex].SectionEntity;
        return resolved;
    }

    private static RuntimeCapture Capture(EntityManager entityManager, Entity sceneEntity)
    {
        Assert.That(entityManager.HasBuffer<ResolvedSectionEntity>(sceneEntity), Is.True,
            "Loaded EntityScene has no resolved section buffer.");
        DynamicBuffer<ResolvedSectionEntity> sections =
            entityManager.GetBuffer<ResolvedSectionEntity>(sceneEntity);
        Assert.That(sections.Length, Is.GreaterThan(0));

        var identities = new HashSet<string>(StringComparer.Ordinal);
        var renderRows = new List<RuntimeRenderRow>();
        var authoredVisualRoots = new HashSet<Entity>();
        var authoredOwnership = new List<string>();
        int authoredVehicleCount = 0;
        int unitGridCount = 0;
        int unitGridPrefabCount = 0;
        int unitGridDisabledCount = 0;
        int unitGridIdentityCount = 0;
        int unitGridDetailedReferenceCount = 0;
        using EntityQuery authoredVehicleQuery =
            entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                    ComponentType.ReadOnly<UnitGrid>(),
                    ComponentType.ReadOnly<SceneTag>()
                }
            });
        using EntityQuery identityQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<OperationMapEntityPresentationIdentity>(),
                ComponentType.ReadOnly<SceneTag>()
            }
        });
        using EntityQuery unitGridQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<UnitGrid>(),
                ComponentType.ReadOnly<SceneTag>()
            },
            Options = EntityQueryOptions.IncludeDisabledEntities |
                      EntityQueryOptions.IncludePrefab
        });
        using EntityQuery renderQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<RenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<SceneTag>()
            }
        });

        for (int sectionIndex = 0; sectionIndex < sections.Length; sectionIndex++)
        {
            Entity sectionEntity = sections[sectionIndex].SectionEntity;
            var sceneTag = new SceneTag { SceneEntity = sectionEntity };

            authoredVehicleQuery.SetSharedComponentFilter(sceneTag);
            authoredVehicleCount += authoredVehicleQuery.CalculateEntityCount();
            using NativeArray<Entity> authoredVehicleEntities =
                authoredVehicleQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < authoredVehicleEntities.Length; i++)
            {
                Entity vehicle = authoredVehicleEntities[i];
                Entity visualRoot = entityManager.HasComponent<UnitDetailedVisualReference>(vehicle)
                    ? entityManager.GetComponentData<UnitDetailedVisualReference>(vehicle).Root
                    : Entity.Null;
                authoredVisualRoots.Add(visualRoot);
                authoredOwnership.Add(
                    $"{vehicle.Index}:{entityManager.GetName(vehicle)}->" +
                    $"{visualRoot.Index}:{entityManager.GetName(visualRoot)}");
            }

            unitGridQuery.SetSharedComponentFilter(sceneTag);
            using NativeArray<Entity> unitGridEntities =
                unitGridQuery.ToEntityArray(Allocator.Temp);
            unitGridCount += unitGridEntities.Length;
            for (int i = 0; i < unitGridEntities.Length; i++)
            {
                Entity unit = unitGridEntities[i];
                unitGridPrefabCount += entityManager.HasComponent<Prefab>(unit) ? 1 : 0;
                unitGridDisabledCount += entityManager.HasComponent<Disabled>(unit) ? 1 : 0;
                unitGridIdentityCount +=
                    entityManager.HasComponent<OperationMapEntityPresentationIdentity>(unit) ? 1 : 0;
                unitGridDetailedReferenceCount +=
                    entityManager.HasComponent<UnitDetailedVisualReference>(unit) ? 1 : 0;
            }

            identityQuery.SetSharedComponentFilter(sceneTag);
            using NativeArray<OperationMapEntityPresentationIdentity> sectionIdentities =
                identityQuery.ToComponentDataArray<OperationMapEntityPresentationIdentity>(
                    Allocator.Temp);
            for (int i = 0; i < sectionIdentities.Length; i++)
            {
                OperationMapEntityPresentationIdentity identity = sectionIdentities[i];
                Assert.That(identity.OperationMapId.ToString(), Is.EqualTo(ExpectedOperationMapId));
                string sourceId = identity.SourceGlobalObjectId.ToString();
                Assert.That(identities.Add(sourceId), Is.True,
                    $"Duplicate packed runtime presentation identity: {sourceId}");
            }

            renderQuery.SetSharedComponentFilter(sceneTag);
            using NativeArray<Entity> renderEntities =
                renderQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<RenderBounds> localBounds =
                renderQuery.ToComponentDataArray<RenderBounds>(Allocator.Temp);
            using NativeArray<LocalToWorld> worldTransforms =
                renderQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);
            Assert.That(worldTransforms.Length, Is.EqualTo(localBounds.Length));
            Assert.That(renderEntities.Length, Is.EqualTo(localBounds.Length));
            for (int i = 0; i < localBounds.Length; i++)
            {
                Matrix4x4 world = ToMatrix(worldTransforms[i].Value);
                renderRows.Add(new RuntimeRenderRow
                {
                    WorldMatrix = ToArray(world),
                    LocalBounds = new[]
                    {
                        localBounds[i].Value.Center.x,
                        localBounds[i].Value.Center.y,
                        localBounds[i].Value.Center.z,
                        localBounds[i].Value.Extents.x,
                        localBounds[i].Value.Extents.y,
                        localBounds[i].Value.Extents.z
                    },
                    WorldBounds = TransformBounds(
                        localBounds[i].Value.Center,
                        localBounds[i].Value.Extents,
                        world),
                    ComponentSignature = BuildComponentSignature(
                        entityManager,
                        renderEntities[i]),
                    AncestorSignature = BuildAncestorSignature(
                        entityManager,
                        renderEntities[i],
                        authoredVisualRoots)
                });
            }
        }

        authoredVehicleQuery.ResetFilter();
        identityQuery.ResetFilter();
        renderQuery.ResetFilter();
        unitGridQuery.ResetFilter();
        Assert.That(authoredVehicleCount, Is.EqualTo(22),
            "Packed candidate must contain exactly 22 authored operation-map vehicle roots.");
        return new RuntimeCapture(
            identities,
            renderRows,
            $"unitGrids={unitGridCount},prefabs={unitGridPrefabCount}," +
            $"disabled={unitGridDisabledCount},identities={unitGridIdentityCount}," +
            $"detailedRefs={unitGridDetailedReferenceCount};" +
            string.Join(",", authoredOwnership.OrderBy(value => value, StringComparer.Ordinal)));
    }

    private static void Compare(
        TransformParityReport expected,
        RuntimeCapture actual,
        int cycle)
    {
        HashSet<string> expectedIdentities = expected.rows
            .Select(row => row.sourceGlobalObjectId)
            .ToHashSet(StringComparer.Ordinal);
        Assert.That(actual.Identities.Count, Is.EqualTo(expected.expectedIdentityCount),
            $"Packed identity count differs in cycle {cycle}.");
        CollectionAssert.AreEquivalent(
            expectedIdentities,
            actual.Identities,
            $"Packed identity set differs in cycle {cycle}.");

        string componentSummary = BuildRenderDifferenceSummary(
            expected.bakedRenderEntities,
            actual.RenderRows,
            math.max(expected.matrixTolerance, expected.boundsTolerance) * 10f);
        Assert.That(actual.RenderRows.Count, Is.EqualTo(expected.bakedRenderEntityCount),
            $"Packed render-entity count differs in cycle {cycle}. " +
            $"Runtime component signatures: {componentSummary}");
        Assert.That(
            componentSummary,
            Does.StartWith("unmatchedRuntime=0, missingAccepted=0"),
            $"Packed render-entity multiset differs in cycle {cycle}: {componentSummary}; " +
            $"authoredOwnership={actual.AuthoredOwnership}");
        AssertRenderRowsWithinTolerance(
            expected.bakedRenderEntities,
            actual.RenderRows,
            expected.matrixTolerance,
            expected.boundsTolerance,
            cycle);
    }

    private static void ValidateExpectedReport(TransformParityReport report)
    {
        Assert.That(report, Is.Not.Null);
        Assert.That(report.schema, Is.EqualTo("warline.operation-map.transform-parity"));
        Assert.That(report.schemaVersion, Is.EqualTo(3));
        Assert.That(report.result, Is.EqualTo("SourceCandidateBakedParityPassed"));
        Assert.That(report.operationMapId, Is.EqualTo(ExpectedOperationMapId));
        Assert.That(report.rows, Is.Not.Null);
        Assert.That(report.rows.Length, Is.EqualTo(report.expectedIdentityCount));
        Assert.That(report.bakedRenderEntities, Is.Not.Null);
        Assert.That(report.bakedRenderEntities.Length, Is.EqualTo(report.bakedRenderEntityCount));
        Assert.That(report.matrixTolerance, Is.GreaterThan(0f));
        Assert.That(report.boundsTolerance, Is.GreaterThan(0f));
    }

    private static void ValidateRuntimeContentReport(
        RuntimeContentReport report,
        string projectRoot,
        string addressablesCatalogPath,
        string entityCatalogPath)
    {
        Assert.That(report, Is.Not.Null);
        Assert.That(report.schema, Is.EqualTo("warline.operation-map.candidate-runtime-content"));
        Assert.That(report.schemaVersion, Is.EqualTo(3));
        Assert.That(report.result, Is.EqualTo("CandidateRuntimeContentBuilt"));
        Assert.That(report.operationMapId, Is.EqualTo(ExpectedOperationMapId));
        Assert.That(report.entitySceneGuid, Is.Not.Empty);
        Assert.That(report.productionCutover, Is.Zero);
        Assert.That(report.temporaryGroupRetained, Is.Zero);
        Assert.That(
            report.candidateSubSceneSha256,
            Is.EqualTo(ComputeSha256(Path.Combine(projectRoot, CandidateSubScenePath))));
        Assert.That(
            report.candidateDefinitionSha256,
            Is.EqualTo(ComputeSha256(Path.Combine(projectRoot, CandidateDefinitionPath))));
        Assert.That(
            report.candidateRuntimeBindingSha256,
            Is.EqualTo(ComputeSha256(Path.Combine(projectRoot, CandidateRuntimeBindingPath))));
        Assert.That(
            report.transformParityReportSha256,
            Is.EqualTo(ComputeSha256(Path.Combine(projectRoot, ExpectedReportPath))));
        Assert.That(
            report.addressablesCatalogSha256,
            Is.EqualTo(ComputeSha256(addressablesCatalogPath)));
        Assert.That(
            report.entityContentCatalogSha256,
            Is.EqualTo(ComputeSha256(entityCatalogPath)));
    }

    private static string ComputeSha256(string path)
    {
        Assert.That(File.Exists(path), Is.True, $"Fingerprint input is missing: {path}");
        using FileStream stream = File.OpenRead(path);
        using SHA256 algorithm = SHA256.Create();
        return string.Concat(
            algorithm.ComputeHash(stream).Select(value => value.ToString("x2")));
    }

    private static void AssertArraysWithin(
        IReadOnlyList<float> expected,
        IReadOnlyList<float> actual,
        float tolerance,
        string label)
    {
        Assert.That(actual.Count, Is.EqualTo(expected.Count), label);
        float maximumResidual = 0f;
        for (int i = 0; i < expected.Count; i++)
            maximumResidual = math.max(maximumResidual, math.abs(expected[i] - actual[i]));
        Assert.That(maximumResidual, Is.LessThanOrEqualTo(tolerance),
            $"{label} residual {maximumResidual} exceeds {tolerance}.");
    }

    private static void AssertRenderRowsWithinTolerance(
        IReadOnlyList<ExpectedRenderRow> expected,
        IReadOnlyList<RuntimeRenderRow> actual,
        float matrixTolerance,
        float boundsTolerance,
        int cycle)
    {
        float quantum = math.max(matrixTolerance, boundsTolerance) * 10f;
        var actualBuckets = actual
            .GroupBy(
                row => BuildRenderKey(row.WorldMatrix, row.LocalBounds, quantum),
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.ToList(),
                StringComparer.Ordinal);

        for (int expectedIndex = 0; expectedIndex < expected.Count; expectedIndex++)
        {
            ExpectedRenderRow expectedRow = expected[expectedIndex];
            string key = BuildRenderKey(
                expectedRow.worldMatrix,
                expectedRow.localBounds,
                quantum);
            Assert.That(
                actualBuckets.TryGetValue(key, out List<RuntimeRenderRow> candidates),
                Is.True,
                $"No runtime render bucket matched accepted row {expectedIndex} in cycle {cycle}.");

            int bestIndex = -1;
            float bestResidual = float.MaxValue;
            for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
            {
                RuntimeRenderRow candidate = candidates[candidateIndex];
                float residual = math.max(
                    MaximumResidual(expectedRow.worldMatrix, candidate.WorldMatrix),
                    math.max(
                        MaximumResidual(expectedRow.localBounds, candidate.LocalBounds),
                        MaximumResidual(expectedRow.worldBounds, candidate.WorldBounds)));
                if (residual >= bestResidual)
                    continue;

                bestResidual = residual;
                bestIndex = candidateIndex;
            }

            Assert.That(bestIndex, Is.GreaterThanOrEqualTo(0),
                $"Runtime render bucket was exhausted for accepted row {expectedIndex} " +
                $"in cycle {cycle}.");
            RuntimeRenderRow actualRow = candidates[bestIndex];
            candidates.RemoveAt(bestIndex);
            if (candidates.Count == 0)
                actualBuckets.Remove(key);

            AssertArraysWithin(
                expectedRow.worldMatrix,
                actualRow.WorldMatrix,
                matrixTolerance,
                $"world matrix row {expectedIndex} cycle {cycle}");
            AssertArraysWithin(
                expectedRow.localBounds,
                actualRow.LocalBounds,
                boundsTolerance,
                $"local bounds row {expectedIndex} cycle {cycle}");
            AssertArraysWithin(
                expectedRow.worldBounds,
                actualRow.WorldBounds,
                boundsTolerance,
                $"world bounds row {expectedIndex} cycle {cycle}");
        }

        Assert.That(actualBuckets.Count, Is.Zero,
            $"Unconsumed runtime render buckets remained in cycle {cycle}.");
    }

    private static Matrix4x4 ToMatrix(float4x4 value)
    {
        var matrix = new Matrix4x4();
        matrix.SetColumn(0, new Vector4(value.c0.x, value.c0.y, value.c0.z, value.c0.w));
        matrix.SetColumn(1, new Vector4(value.c1.x, value.c1.y, value.c1.z, value.c1.w));
        matrix.SetColumn(2, new Vector4(value.c2.x, value.c2.y, value.c2.z, value.c2.w));
        matrix.SetColumn(3, new Vector4(value.c3.x, value.c3.y, value.c3.z, value.c3.w));
        return matrix;
    }

    private static float[] ToArray(Matrix4x4 matrix)
    {
        var values = new float[16];
        for (int i = 0; i < values.Length; i++)
            values[i] = matrix[i];
        return values;
    }

    private static float[] TransformBounds(float3 center, float3 extents, Matrix4x4 matrix)
    {
        Vector3 worldCenter = matrix.MultiplyPoint3x4(new Vector3(center.x, center.y, center.z));
        Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
        Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
        Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
        Vector3 worldExtents = new(
            math.abs(axisX.x) + math.abs(axisY.x) + math.abs(axisZ.x),
            math.abs(axisX.y) + math.abs(axisY.y) + math.abs(axisZ.y),
            math.abs(axisX.z) + math.abs(axisY.z) + math.abs(axisZ.z));
        Vector3 minimum = worldCenter - worldExtents;
        Vector3 maximum = worldCenter + worldExtents;
        return new[] { minimum.x, minimum.y, minimum.z, maximum.x, maximum.y, maximum.z };
    }

    private static string BuildComponentSignature(
        EntityManager entityManager,
        Entity entity)
    {
        using NativeArray<ComponentType> componentTypes =
            entityManager.GetComponentTypes(entity, Allocator.Temp);
        return string.Join(
            ",",
            componentTypes
                .Select(component => component.GetManagedType()?.Name ?? component.ToString())
                .OrderBy(name => name, StringComparer.Ordinal));
    }

    private static string BuildRenderDifferenceSummary(
        IReadOnlyList<ExpectedRenderRow> expected,
        IReadOnlyList<RuntimeRenderRow> actual,
        float quantum)
    {
        var remaining = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < expected.Count; i++)
        {
            string key = BuildRenderKey(
                expected[i].worldMatrix,
                expected[i].localBounds,
                quantum);
            remaining.TryGetValue(key, out int count);
            remaining[key] = count + 1;
        }

        var extras = new List<RuntimeRenderRow>();
        for (int i = 0; i < actual.Count; i++)
        {
            string key = BuildRenderKey(
                actual[i].WorldMatrix,
                actual[i].LocalBounds,
                quantum);
            if (remaining.TryGetValue(key, out int count) && count > 0)
            {
                if (count == 1)
                    remaining.Remove(key);
                else
                    remaining[key] = count - 1;
            }
            else
            {
                extras.Add(actual[i]);
            }
        }

        int missing = remaining.Values.Sum();
        string signatures = string.Join(
            "; ",
            extras
                .GroupBy(row => row.ComponentSignature, StringComparer.Ordinal)
                .OrderByDescending(group => group.Count())
                .Take(12)
                .Select(group => $"{group.Count()}x[{group.Key}]"));
        string residuals = BuildNearestResidualSummary(expected, extras);
        string ancestor = extras.Count > 0 ? extras[0].AncestorSignature : "none";
        return $"unmatchedRuntime={extras.Count}, missingAccepted={missing}, " +
               $"nearestResiduals={residuals}, firstAncestorChain={ancestor}, " +
               $"unmatchedSignatures={signatures}";
    }

    private static string BuildNearestResidualSummary(
        IReadOnlyList<ExpectedRenderRow> expected,
        IReadOnlyList<RuntimeRenderRow> extras)
    {
        if (extras.Count == 0)
            return "none";

        float minimumMatrixResidual = float.MaxValue;
        float maximumMatrixResidual = 0f;
        float minimumBoundsResidual = float.MaxValue;
        float maximumBoundsResidual = 0f;
        int sampleCount = math.min(extras.Count, 24);
        for (int actualIndex = 0; actualIndex < sampleCount; actualIndex++)
        {
            RuntimeRenderRow actual = extras[actualIndex];
            float bestScore = float.MaxValue;
            float bestMatrix = float.MaxValue;
            float bestBounds = float.MaxValue;
            for (int expectedIndex = 0; expectedIndex < expected.Count; expectedIndex++)
            {
                ExpectedRenderRow candidate = expected[expectedIndex];
                float matrixResidual = MaximumResidual(
                    candidate.worldMatrix,
                    actual.WorldMatrix);
                float boundsResidual = MaximumResidual(
                    candidate.localBounds,
                    actual.LocalBounds);
                float score = matrixResidual + boundsResidual;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestMatrix = matrixResidual;
                bestBounds = boundsResidual;
            }

            minimumMatrixResidual = math.min(minimumMatrixResidual, bestMatrix);
            maximumMatrixResidual = math.max(maximumMatrixResidual, bestMatrix);
            minimumBoundsResidual = math.min(minimumBoundsResidual, bestBounds);
            maximumBoundsResidual = math.max(maximumBoundsResidual, bestBounds);
        }

        return $"samples={sampleCount},matrix={minimumMatrixResidual:R}..{maximumMatrixResidual:R}," +
               $"localBounds={minimumBoundsResidual:R}..{maximumBoundsResidual:R}";
    }

    private static float MaximumResidual(
        IReadOnlyList<float> left,
        IReadOnlyList<float> right)
    {
        float residual = 0f;
        int count = math.min(left.Count, right.Count);
        for (int i = 0; i < count; i++)
            residual = math.max(residual, math.abs(left[i] - right[i]));
        return residual;
    }

    private static string BuildAncestorSignature(
        EntityManager entityManager,
        Entity entity,
        HashSet<Entity> authoredVisualRoots)
    {
        var rows = new List<string>(16);
        Entity current = entity;
        for (int depth = 0; depth < 32 && entityManager.Exists(current); depth++)
        {
            string sourceIdentity = entityManager.HasComponent<OperationMapEntityPresentationIdentity>(current)
                ? $"Source={entityManager.GetComponentData<OperationMapEntityPresentationIdentity>(current).SourceGlobalObjectId},"
                : string.Empty;
            rows.Add(
                $"{depth}:{current.Index}:{entityManager.GetName(current)}[" +
                $"{(entityManager.HasComponent<UnitGrid>(current) ? "Grid," : string.Empty)}" +
                $"{(entityManager.HasComponent<Faction>(current) ? "Faction," : string.Empty)}" +
                $"{(entityManager.HasComponent<Prefab>(current) ? "Prefab," : string.Empty)}" +
                $"{(entityManager.HasComponent<OperationMapEntityPresentationIdentity>(current) ? "Identity," : string.Empty)}" +
                $"{(entityManager.HasComponent<OperationMapAuthoredVehiclePresentation>(current) ? "AuthoredVehicle," : string.Empty)}" +
                $"{(authoredVisualRoots.Contains(current) ? "AuthoredVisualRoot," : string.Empty)}" +
                $"{(entityManager.HasComponent<UnitDetailedVisualReference>(current) ? "DetailedRef," : string.Empty)}" +
                $"{(entityManager.HasComponent<UnitModelPrefabReference>(current) ? "ModelPrefabRef," : string.Empty)}" +
                $"{(entityManager.HasComponent<UnitModelInstanceReference>(current) ? "ModelInstanceRef," : string.Empty)}" +
                $"{(entityManager.HasComponent<UnitMidLodPrefabReference>(current) ? "MidLodRef," : string.Empty)}" +
                sourceIdentity +
                $"{(entityManager.HasComponent<SceneTag>(current) ? "SceneTag" : string.Empty)}]");
            if (!entityManager.HasComponent<Parent>(current))
                break;
            current = entityManager.GetComponentData<Parent>(current).Value;
        }

        return string.Join(">", rows);
    }

    private static string BuildRenderKey(
        IReadOnlyList<float> worldMatrix,
        IReadOnlyList<float> localBounds,
        float quantum)
    {
        return string.Join(
            ",",
            worldMatrix
                .Concat(localBounds)
                .Select(value => Math.Round(value / quantum).ToString()));
    }

    private sealed class RuntimeCapture
    {
        public RuntimeCapture(
            HashSet<string> identities,
            List<RuntimeRenderRow> renderRows,
            string authoredOwnership)
        {
            Identities = identities;
            RenderRows = renderRows;
            AuthoredOwnership = authoredOwnership;
        }

        public HashSet<string> Identities { get; }
        public List<RuntimeRenderRow> RenderRows { get; }
        public string AuthoredOwnership { get; }
    }

    private sealed class RuntimeRenderRow
    {
        public float[] WorldMatrix;
        public float[] LocalBounds;
        public float[] WorldBounds;
        public string ComponentSignature;
        public string AncestorSignature;
    }

    [Serializable]
    private sealed class TransformParityReport
    {
        public string schema;
        public int schemaVersion;
        public string operationMapId;
        public string result;
        public int expectedIdentityCount;
        public float matrixTolerance;
        public float boundsTolerance;
        public IdentityRow[] rows;
        public int bakedRenderEntityCount;
        public ExpectedRenderRow[] bakedRenderEntities;
    }

    [Serializable]
    private sealed class RuntimeContentReport
    {
        public string schema;
        public int schemaVersion;
        public string result;
        public string operationMapId;
        public string entitySceneGuid;
        public string candidateSubSceneSha256;
        public string candidateDefinitionSha256;
        public string candidateRuntimeBindingSha256;
        public string transformParityReportSha256;
        public string addressablesCatalogSha256;
        public string entityContentCatalogSha256;
        public int productionCutover;
        public int temporaryGroupRetained;
    }

    [Serializable]
    private sealed class IdentityRow
    {
        public string sourceGlobalObjectId;
    }

    [Serializable]
    private sealed class ExpectedRenderRow
    {
        public float[] worldMatrix;
        public float[] localBounds;
        public float[] worldBounds;
    }

}
