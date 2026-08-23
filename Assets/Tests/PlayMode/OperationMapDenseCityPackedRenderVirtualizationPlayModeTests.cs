using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;
using Hash128 = Unity.Entities.Hash128;

public sealed partial class OperationMapEntityScenePackedRuntimeParityPlayModeTests
{
    private const int DenseExpectedVirtualizedPlacementCount = 40460;
    private const int DenseExpectedVirtualizedRowCount = 61813;
    private const int DenseExpectedVirtualizedRendererCount = 61783;
    private const int DenseExpectedVirtualizedResidentRowCount = 14017;
    private const int DenseExpectedVirtualizedSlotCount = 7765;
    private const int DenseExpectedVirtualizedPrototypeCount = 9107;
    private const int DenseExpectedVirtualizedPartCount = 12181;
    private const int DenseExpectedVirtualizedCellCount = 1934;
    private const int DenseExpectedVirtualizedPoolBucketCount = 4;
    private const int DenseExpectedVirtualizedGeneratedBuildingCount = 4530;
    private const int DenseExpectedVirtualizedGeneratedRenderOnlyCount = 31400;
    private const int DenseExpectedRetainedVirtualizedGeneratedBuildingCount = 4530;
    private const int DenseExpectedRetainedVirtualizedGeneratedRenderOnlyCount = 5758;
    private const int DenseExpectedVirtualizedDatabaseSchemaVersion =
        OperationMapRenderDatabaseBakeConfig.CurrentSchemaVersion;
    private const string DenseExpectedVirtualizedContentHash =
        "bfb350f0c8d1474aa05252dc04c87eede4c1210adcee9c92dcdbecc35897896e";

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator DenseVirtualizedPackedCandidate_TwoLoadCyclesReachReadinessAndUnloadCleanly()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string addressablesCatalogPath =
            DenseResolve(projectRoot, DenseAddressablesCatalogPath);
        string entityContentRoot = DenseResolve(projectRoot, DenseEntityContentPath);
        string entityCatalogPath = Path.Combine(
            entityContentRoot,
            RuntimeContentManager.RelativeCatalogPath);
        DenseRequireFile(addressablesCatalogPath);
        DenseRequireFile(entityCatalogPath);

        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
        AsyncOperationHandle<OperationMapDefinition> definitionHandle = default;
        bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
        var unexpectedErrors = new List<string>();
        Application.LogCallback logCallback = (condition, _, type) =>
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            if (DenseIsKnownEntityNameCapacityDiagnostic(condition) ||
                DenseIsKnownEditorRelayDiagnostic(condition))
                return;
            unexpectedErrors.Add(condition);
        };
        Application.logMessageReceived += logCallback;
        LogAssert.ignoreFailingMessages = true;
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
                $"Dense candidate Entities content catalog failed to load: {entityCatalogPath}");

            catalogHandle = Addressables.LoadContentCatalogAsync(
                addressablesCatalogPath,
                autoReleaseHandle: false);
            yield return catalogHandle;
            Assert.That(
                catalogHandle.Status,
                Is.EqualTo(AsyncOperationStatus.Succeeded),
                catalogHandle.OperationException?.Message);

            definitionHandle =
                Addressables.LoadAssetAsync<OperationMapDefinition>(DenseDefinitionAddress);
            yield return definitionHandle;
            Assert.That(
                definitionHandle.Status,
                Is.EqualTo(AsyncOperationStatus.Succeeded),
                definitionHandle.OperationException?.Message);
            Assert.That(definitionHandle.Result, Is.Not.Null);
            Assert.That(
                definitionHandle.Result.RenderResidencyMode,
                Is.EqualTo(OperationMapRenderResidencyMode.VirtualizedProxyPool));

            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            Assert.That(world.IsCreated, Is.True);
            var sceneGuid = new Hash128(
                definitionHandle.Result.NavigationMetadata.AuthoredSubSceneGuid);
            Assert.That(sceneGuid.IsValid, Is.True);

            for (int cycle = 1; cycle <= 2; cycle++)
            {
                using var loader = new OperationMapSceneLoadingSceneSystemHelper();
                Assert.That(
                    loader.TryStart(definitionHandle.Result, out string startError),
                    Is.True,
                    startError);
                yield return DenseWaitForPackedLoaderReady(loader, $"cycle {cycle}");

                Entity sceneEntity =
                    SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid);
                Assert.That(sceneEntity, Is.Not.EqualTo(Entity.Null));
                Entity[] sectionEntities =
                    GetResolvedSectionEntities(world.EntityManager, sceneEntity);
                DenseAssertCurrentVirtualizedPackedReadiness(
                    world.EntityManager,
                    sceneEntity,
                    sectionEntities,
                    $"cycle {cycle}");

                Assert.That(
                    loader.TryBeginUnload(out string unloadError),
                    Is.True,
                    unloadError);
                yield return DenseWaitForPackedLoaderUnload(loader, $"cycle {cycle}");
                yield return DenseWaitForVirtualizedPackedCleanup(
                    world,
                    sceneEntity,
                    sectionEntities,
                    $"cycle {cycle}");
                AssertAcceptedAuthoringScenesNotLoaded();
            }

            Debug.Log(
                "[DenseVirtualizedPackedLifecycle] result=Passed cycles=2 " +
                $"placements={DenseExpectedVirtualizedPlacementCount} " +
                $"rows={DenseExpectedVirtualizedRowCount} " +
                $"residentRows={DenseExpectedVirtualizedResidentRowCount} " +
                $"slots={DenseExpectedVirtualizedSlotCount} staleEntities=0");
        }
        finally
        {
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
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            Application.logMessageReceived -= logCallback;
        }
        Assert.That(
            unexpectedErrors,
            Is.Empty,
            "Dense packed lifecycle emitted unexpected error logs.");
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator DenseVirtualizedPackedCandidate_ReadinessFailureResetsAndRetriesCleanly()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string addressablesCatalogPath =
            DenseResolve(projectRoot, DenseAddressablesCatalogPath);
        string entityContentRoot = DenseResolve(projectRoot, DenseEntityContentPath);
        string entityCatalogPath = Path.Combine(
            entityContentRoot,
            RuntimeContentManager.RelativeCatalogPath);
        DenseRequireFile(addressablesCatalogPath);
        DenseRequireFile(entityCatalogPath);

        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
        AsyncOperationHandle<OperationMapDefinition> definitionHandle = default;
        OperationMapSceneLoadingSceneSystemHelper loader = null;
        bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
        var unexpectedErrors = new List<string>();
        Application.LogCallback logCallback = (condition, _, type) =>
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;
            if (DenseIsKnownEntityNameCapacityDiagnostic(condition) ||
                DenseIsKnownEditorRelayDiagnostic(condition))
                return;
            unexpectedErrors.Add(condition);
        };
        Application.logMessageReceived += logCallback;
        LogAssert.ignoreFailingMessages = true;
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
                $"Dense candidate Entities content catalog failed to load: {entityCatalogPath}");

            catalogHandle = Addressables.LoadContentCatalogAsync(
                addressablesCatalogPath,
                autoReleaseHandle: false);
            yield return catalogHandle;
            Assert.That(catalogHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded));
            definitionHandle =
                Addressables.LoadAssetAsync<OperationMapDefinition>(DenseDefinitionAddress);
            yield return definitionHandle;
            Assert.That(definitionHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded));
            Assert.That(definitionHandle.Result, Is.Not.Null);

            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            Assert.That(world.IsCreated, Is.True);
            var sceneGuid = new Hash128(
                definitionHandle.Result.NavigationMetadata.AuthoredSubSceneGuid);
            Assert.That(sceneGuid.IsValid, Is.True);
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
            Assert.That(
                loader.FailureCode,
                Is.EqualTo(OperationMapLoadResultCode.MetadataBindFailed));
            Assert.That(failReadinessOnceApi.FailureCount, Is.EqualTo(1));

            bool reset = false;
            string resetError = null;
            deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
            while (!reset && Time.realtimeSinceStartup < deadline)
            {
                reset = loader.TryReset(out resetError);
                if (!reset)
                {
                    Assert.That(resetError, Does.Contain("cleanup is still in progress"));
                    yield return null;
                }
            }
            Assert.That(reset, Is.True, resetError);
            yield return DenseWaitForVirtualizedPackedCleanup(
                world,
                failReadinessOnceApi.FailedSceneEntity,
                failReadinessOnceApi.FailedSectionEntities,
                "failed readiness reset");
            Assert.That(
                SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid),
                Is.EqualTo(Entity.Null));

            Assert.That(
                loader.TryStart(definitionHandle.Result, out startError),
                Is.True,
                startError);
            yield return DenseWaitForPackedLoaderReady(loader, "retry");
            Assert.That(failReadinessOnceApi.FailureCount, Is.EqualTo(1));

            Entity retrySceneEntity =
                SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid);
            Assert.That(retrySceneEntity, Is.Not.EqualTo(Entity.Null));
            Entity[] retrySections =
                GetResolvedSectionEntities(world.EntityManager, retrySceneEntity);
            DenseAssertCurrentVirtualizedPackedReadiness(
                world.EntityManager,
                retrySceneEntity,
                retrySections,
                "retry");

            Assert.That(
                loader.TryBeginUnload(out string unloadError),
                Is.True,
                unloadError);
            yield return DenseWaitForPackedLoaderUnload(loader, "retry");
            yield return DenseWaitForVirtualizedPackedCleanup(
                world,
                retrySceneEntity,
                retrySections,
                "retry unload");
            AssertAcceptedAuthoringScenesNotLoaded();

            Debug.Log(
                "[DenseVirtualizedPackedFailureResetRetry] result=Passed " +
                "failures=1 resets=1 retries=1 staleEntities=0");
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
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            Application.logMessageReceived -= logCallback;
        }
        Assert.That(
            unexpectedErrors,
            Is.Empty,
            "Dense packed failure/reset/retry emitted unexpected error logs.");
    }

    private static IEnumerator DenseWaitForPackedLoaderReady(
        OperationMapSceneLoadingSceneSystemHelper loader,
        string stage)
    {
        float deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (!loader.IsReady &&
               !loader.HasFailed &&
               Time.realtimeSinceStartup < deadline)
        {
            loader.Update();
            yield return null;
        }
        Assert.That(loader.HasFailed, Is.False, $"{stage}: {loader.Failure}");
        Assert.That(loader.IsReady, Is.True, $"{stage}: packed loader did not become ready.");
        Assert.That(loader.Manifest, Is.Null, $"{stage}: EntityScene must not use static manifest.");
    }

    private static IEnumerator DenseWaitForPackedLoaderUnload(
        OperationMapSceneLoadingSceneSystemHelper loader,
        string stage)
    {
        float deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (!loader.UnloadComplete &&
               !loader.HasFailed &&
               Time.realtimeSinceStartup < deadline)
        {
            loader.Update();
            yield return null;
        }
        Assert.That(loader.HasFailed, Is.False, $"{stage}: {loader.Failure}");
        Assert.That(loader.UnloadComplete, Is.True, $"{stage}: packed unload did not complete.");
    }

    private static IEnumerator DenseWaitForVirtualizedPackedCleanup(
        World world,
        Entity sceneEntity,
        IReadOnlyList<Entity> sectionEntities,
        string stage)
    {
        EntityManager entityManager = world.EntityManager;
        using EntityQuery databaseQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>());
        using EntityQuery slotQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>() },
            Options = EntityQueryOptions.IncludeDisabledEntities |
                      EntityQueryOptions.IgnoreComponentEnabledState
        });
        float deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            bool metadataRemoved =
                sceneEntity == Entity.Null || !entityManager.Exists(sceneEntity);
            bool sectionsRemoved = true;
            if (sectionEntities != null)
            {
                for (int index = 0; index < sectionEntities.Count; index++)
                {
                    if (entityManager.Exists(sectionEntities[index]))
                    {
                        sectionsRemoved = false;
                        break;
                    }
                }
            }
            if (metadataRemoved &&
                sectionsRemoved &&
                databaseQuery.CalculateEntityCount() == 0 &&
                slotQuery.CalculateEntityCount() == 0)
            {
                yield break;
            }
            yield return null;
        }

        Assert.That(
            sceneEntity == Entity.Null || !entityManager.Exists(sceneEntity),
            Is.True,
            $"{stage}: packed scene metadata remained after cleanup.");
        if (sectionEntities != null)
        {
            for (int index = 0; index < sectionEntities.Count; index++)
            {
                Assert.That(
                    entityManager.Exists(sectionEntities[index]),
                    Is.False,
                    $"{stage}: packed section {index} remained after cleanup.");
            }
        }
        Assert.That(databaseQuery.CalculateEntityCount(), Is.Zero, $"{stage}: database remained.");
        Assert.That(slotQuery.CalculateEntityCount(), Is.Zero, $"{stage}: proxy slots remained.");
    }

    private static void DenseAssertCurrentVirtualizedPackedReadiness(
        EntityManager entityManager,
        Entity sceneEntity,
        IReadOnlyList<Entity> sectionEntities,
        string stage)
    {
        Assert.That(sectionEntities, Is.Not.Null, stage);
        Assert.That(sectionEntities.Count, Is.GreaterThan(0), stage);
        Assert.That(
            CountEntitiesForSections(entityManager, sectionEntities),
            Is.GreaterThan(0),
            stage);
        Assert.That(
            OperationMapEntityPresentationReadinessUtility.TryValidate(
                entityManager,
                sceneEntity,
                ExpectedOperationMapId,
                OperationMapRenderResidencyMode.VirtualizedProxyPool,
                out string readinessError),
            Is.True,
            $"{stage}: {readinessError}");

        using EntityQuery databaseQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>(),
            ComponentType.ReadOnly<OperationMapRenderPackedReadinessComponent>(),
            ComponentType.ReadOnly<OperationMapRenderVirtualizationStateComponent>(),
            ComponentType.ReadOnly<OperationMapRenderVirtualizationMetricsComponent>());
        Assert.That(databaseQuery.CalculateEntityCount(), Is.EqualTo(1), stage);
        Entity databaseEntity = databaseQuery.GetSingletonEntity();
        OperationMapRenderDatabaseComponent database =
            entityManager.GetComponentData<OperationMapRenderDatabaseComponent>(databaseEntity);
        OperationMapRenderPackedReadinessComponent readiness =
            entityManager.GetComponentData<OperationMapRenderPackedReadinessComponent>(databaseEntity);
        Assert.That(database.Blob.IsCreated, Is.True, stage);
        Assert.That(
            database.ContentHash.ToString(),
            Is.EqualTo(DenseExpectedVirtualizedContentHash),
            stage);
        Assert.That(
            database.SchemaVersion,
            Is.EqualTo(DenseExpectedVirtualizedDatabaseSchemaVersion),
            stage);
        Assert.That(
            readiness.ResidencyMode,
            Is.EqualTo((byte)OperationMapRenderResidencyMode.VirtualizedProxyPool),
            stage);
        Assert.That(
            readiness.EligibleSourceRowCount,
            Is.EqualTo(DenseExpectedVirtualizedRowCount),
            stage);
        Assert.That(
            readiness.EligibleSourceRendererCount,
            Is.EqualTo(DenseExpectedVirtualizedRendererCount),
            stage);
        Assert.That(
            readiness.ResidentSourceRowCount,
            Is.EqualTo(DenseExpectedVirtualizedResidentRowCount),
            stage);
        Assert.That(
            readiness.ProxySlotCount,
            Is.EqualTo(DenseExpectedVirtualizedSlotCount),
            stage);
        Assert.That(readiness.VirtualizedAcceptedBuildingIdentityCount, Is.Zero, stage);
        Assert.That(readiness.VirtualizedAcceptedRenderOnlyIdentityCount, Is.Zero, stage);
        Assert.That(
            readiness.VirtualizedGeneratedBuildingIdentityCount,
            Is.EqualTo(DenseExpectedVirtualizedGeneratedBuildingCount),
            stage);
        Assert.That(
            readiness.VirtualizedGeneratedRenderOnlyIdentityCount,
            Is.EqualTo(DenseExpectedVirtualizedGeneratedRenderOnlyCount),
            stage);
        Assert.That(
            readiness.RetainedVirtualizedGeneratedBuildingIdentityCount,
            Is.EqualTo(DenseExpectedRetainedVirtualizedGeneratedBuildingCount),
            stage);
        Assert.That(
            readiness.RetainedVirtualizedGeneratedRenderOnlyIdentityCount,
            Is.EqualTo(DenseExpectedRetainedVirtualizedGeneratedRenderOnlyCount),
            stage);
        Assert.That(
            readiness.RetainedVirtualizedAcceptedBuildingIdentityCount,
            Is.InRange(0, readiness.VirtualizedAcceptedBuildingIdentityCount),
            stage);
        Assert.That(
            readiness.RetainedVirtualizedAcceptedRenderOnlyIdentityCount,
            Is.InRange(0, readiness.VirtualizedAcceptedRenderOnlyIdentityCount),
            stage);

        ref OperationMapRenderDatabaseBlob blob = ref database.Blob.Value;
        Assert.That(blob.Prototypes.Length, Is.EqualTo(DenseExpectedVirtualizedPrototypeCount), stage);
        Assert.That(blob.Parts.Length, Is.EqualTo(DenseExpectedVirtualizedPartCount), stage);
        Assert.That(blob.Placements.Length, Is.EqualTo(DenseExpectedVirtualizedPlacementCount), stage);
        Assert.That(blob.Cells.Length, Is.EqualTo(DenseExpectedVirtualizedCellCount), stage);
        Assert.That(blob.PoolBuckets.Length, Is.EqualTo(DenseExpectedVirtualizedPoolBucketCount), stage);
        Assert.That(
            entityManager.GetBuffer<OperationMapRenderResidentSourceRowComponent>(
                databaseEntity,
                true).Length,
            Is.EqualTo(DenseExpectedVirtualizedResidentRowCount),
            stage);

        using EntityQuery eligibleSourceQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<OperationMapRenderEligibleSourceComponent>());
        Assert.That(
            eligibleSourceQuery.CalculateEntityCount(),
            Is.Zero,
            $"{stage}: eligible source render entities leaked into packed content.");
        using EntityQuery slotQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[] { ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>() },
            Options = EntityQueryOptions.IncludeDisabledEntities |
                      EntityQueryOptions.IgnoreComponentEnabledState
        });
        Assert.That(
            slotQuery.CalculateEntityCount(),
            Is.EqualTo(DenseExpectedVirtualizedSlotCount),
            stage);
    }

    private static bool DenseIsKnownEntityNameCapacityDiagnostic(string condition) =>
        condition.StartsWith("[Worker", StringComparison.Ordinal) &&
        condition.EndsWith(
            "Max unique Entity Name capacity exceeded. If you require more storage, " +
            "edit EntityNameStorage.cs and change the value of kMaxEntries to " +
            "pre-allocate more space.",
            StringComparison.Ordinal);

    private static bool DenseIsKnownEditorRelayDiagnostic(string condition) =>
        condition != null &&
        condition.StartsWith("connection.state_change", StringComparison.Ordinal) &&
        ((condition.Contains(
              "oldState=Connecting newState=Failed",
              StringComparison.Ordinal) &&
          condition.Contains(
              "Relay process exited (exit code -1073741819)",
              StringComparison.Ordinal)) ||
         (condition.Contains(
              "oldState=Running newState=Failed",
              StringComparison.Ordinal) &&
          condition.Contains(
              "Process exited unexpectedly. code=-1073741819",
              StringComparison.Ordinal)));

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator DensePackedCandidate_VirtualizedPilotCameraRouteIsBoundedAndDeterministic()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string addressablesCatalogPath =
            DenseResolve(projectRoot, DenseAddressablesCatalogPath);
        string entityContentRoot = DenseResolve(projectRoot, DenseEntityContentPath);
        string entityCatalogPath = Path.Combine(
            entityContentRoot,
            RuntimeContentManager.RelativeCatalogPath);
        DenseRequireFile(addressablesCatalogPath);
        DenseRequireFile(entityCatalogPath);

        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
        AsyncOperationHandle<OperationMapDefinition> definitionHandle = default;
        RuntimeCameraReferenceSystem cameraSystem = null;
        GameObject cameraObject = null;
        World world = null;
        Entity activeMapEntity = Entity.Null;
        bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
        var unexpectedErrors = new List<string>();
        Application.LogCallback logCallback = (condition, _, type) =>
        {
            if (type != LogType.Error &&
                type != LogType.Exception &&
                type != LogType.Assert)
                return;
            if (condition.StartsWith("[Worker", StringComparison.Ordinal) &&
                condition.EndsWith(
                    "Max unique Entity Name capacity exceeded. If you require more storage, " +
                    "edit EntityNameStorage.cs and change the value of kMaxEntries to " +
                    "pre-allocate more space.",
                    StringComparison.Ordinal))
                return;
            unexpectedErrors.Add(condition);
        };
        Application.logMessageReceived += logCallback;
        LogAssert.ignoreFailingMessages = true;
        using var loader = new OperationMapSceneLoadingSceneSystemHelper();
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
                $"Dense candidate Entities content catalog failed to load: {entityCatalogPath}");

            catalogHandle = Addressables.LoadContentCatalogAsync(
                addressablesCatalogPath,
                autoReleaseHandle: false);
            yield return catalogHandle;
            Assert.That(
                catalogHandle.Status,
                Is.EqualTo(AsyncOperationStatus.Succeeded),
                catalogHandle.OperationException?.Message);

            definitionHandle =
                Addressables.LoadAssetAsync<OperationMapDefinition>(DenseDefinitionAddress);
            yield return definitionHandle;
            Assert.That(
                definitionHandle.Status,
                Is.EqualTo(AsyncOperationStatus.Succeeded),
                definitionHandle.OperationException?.Message);
            Assert.That(definitionHandle.Result, Is.Not.Null);
            Assert.That(
                definitionHandle.Result.RenderResidencyMode,
                Is.EqualTo(OperationMapRenderResidencyMode.VirtualizedProxyPool));

            world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            Assert.That(world.IsCreated, Is.True);
            cameraSystem = world.GetExistingSystemManaged<RuntimeCameraReferenceSystem>();
            Assert.That(cameraSystem, Is.Not.Null);
            cameraObject = new GameObject("DensePackedVirtualizationRouteCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(
                new Vector3(0f, 180f, 0f),
                Quaternion.Euler(60f, 0f, 0f));
            cameraSystem.SetWorldCamera(camera);

            Assert.That(
                loader.TryStart(definitionHandle.Result, out string startError),
                Is.True,
                startError);
            float deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
            while (!loader.IsReady &&
                   !loader.HasFailed &&
                   Time.realtimeSinceStartup < deadline)
            {
                loader.Update();
                yield return null;
            }
            Assert.That(loader.HasFailed, Is.False, loader.Failure);
            Assert.That(loader.IsReady, Is.True);

            EntityManager entityManager = world.EntityManager;
            using (EntityQuery activeMapQuery = entityManager.CreateEntityQuery(
                       ComponentType.ReadOnly<ActiveOperationMapComponent>()))
            {
                Assert.That(
                    activeMapQuery.CalculateEntityCount(),
                    Is.Zero,
                    "The low-level packed fixture must explicitly own its active-map projection.");
            }
            activeMapEntity =
                entityManager.CreateEntity(typeof(ActiveOperationMapComponent));
            entityManager.SetComponentData(
                activeMapEntity,
                new ActiveOperationMapComponent
                {
                    OperationMapId =
                        new FixedString64Bytes(definitionHandle.Result.OperationMapId),
                    Generation = 1
                });
            using EntityQuery databaseQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapRenderDatabaseComponent>(),
                ComponentType.ReadOnly<OperationMapRenderPackedReadinessComponent>(),
                ComponentType.ReadOnly<OperationMapRenderVirtualizationStateComponent>(),
                ComponentType.ReadOnly<OperationMapRenderVirtualizationMetricsComponent>());
            deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
            while (databaseQuery.CalculateEntityCount() != 1 &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(databaseQuery.CalculateEntityCount(), Is.EqualTo(1));

            Entity databaseEntity = databaseQuery.GetSingletonEntity();
            OperationMapRenderDatabaseComponent database =
                entityManager.GetComponentData<OperationMapRenderDatabaseComponent>(
                    databaseEntity);
            OperationMapRenderPackedReadinessComponent readiness =
                entityManager.GetComponentData<OperationMapRenderPackedReadinessComponent>(
                    databaseEntity);
            Assert.That(database.Blob.IsCreated, Is.True);
            Assert.That(
                readiness.ResidencyMode,
                Is.EqualTo((byte)OperationMapRenderResidencyMode.VirtualizedProxyPool));
            Assert.That(
                readiness.VirtualizedGeneratedRenderOnlyIdentityCount,
                Is.EqualTo(DenseExpectedVirtualizedPlacementCount));
            Assert.That(
                readiness.EligibleSourceRowCount,
                Is.EqualTo(DenseExpectedVirtualizedRowCount));
            Assert.That(
                readiness.ProxySlotCount,
                Is.EqualTo(DenseExpectedVirtualizedSlotCount));

            BlobAssetReference<OperationMapRenderDatabaseBlob> databaseBlob =
                database.Blob;
            int originCellIndex = DenseFindRouteOriginCell(databaseBlob);
            int fastCellIndex = DenseFindSeparatedCell(
                databaseBlob,
                originCellIndex,
                minimumDistance: 4,
                chooseFarthest: false);
            int teleportCellIndex = DenseFindSeparatedCell(
                databaseBlob,
                originCellIndex,
                minimumDistance: 8,
                chooseFarthest: true);
            int2 originCoordinate =
                databaseBlob.Value.Cells[originCellIndex].Coordinate;
            int2 slowCoordinate =
                DenseChooseAdjacentCoordinate(databaseBlob, originCoordinate);

            camera.transform.position =
                DenseCellCameraPosition(originCoordinate, databaseBlob.Value.CellSize);
            yield return DenseWaitForVirtualizationRebuild(
                entityManager,
                databaseEntity,
                1,
                "initial");
            DenseVirtualizationObservation initial =
                DenseObserveVirtualization(entityManager, databaseEntity);
            DenseAssertBounded(initial, "initial");
            DenseSlotBinding[] initialBindings =
                DenseCaptureActiveBindings(entityManager);

            camera.fieldOfView = 35f;
            camera.transform.rotation = Quaternion.Euler(68f, 37f, 0f);
            yield return null;
            yield return null;
            yield return null;
            DenseVirtualizationObservation rotated =
                DenseObserveVirtualization(entityManager, databaseEntity);
            DenseAssertNoRebuild(initial, rotated, "zoom/rotation");

            Vector3 slowTarget =
                DenseCellCameraPosition(
                    slowCoordinate,
                    databaseBlob.Value.CellSize);
            Vector3 slowStart = camera.transform.position;
            for (int step = 1; step <= 4; step++)
            {
                camera.transform.position =
                    Vector3.Lerp(slowStart, slowTarget, step / 4f);
                yield return null;
            }
            yield return null;
            DenseVirtualizationObservation slow =
                DenseObserveVirtualization(entityManager, databaseEntity);
            DenseAssertNoRebuild(initial, slow, "slow guard-band pan");

            camera.transform.position = DenseCellCameraPosition(
                databaseBlob.Value.Cells[fastCellIndex].Coordinate,
                databaseBlob.Value.CellSize);
            yield return DenseWaitForVirtualizationRebuild(
                entityManager,
                databaseEntity,
                initial.State.RebuildCount + 1,
                "fast pan");
            DenseVirtualizationObservation fast =
                DenseObserveVirtualization(entityManager, databaseEntity);
            DenseAssertBounded(fast, "fast pan");

            camera.transform.position = DenseCellCameraPosition(
                databaseBlob.Value.Cells[teleportCellIndex].Coordinate,
                databaseBlob.Value.CellSize);
            yield return DenseWaitForVirtualizationRebuild(
                entityManager,
                databaseEntity,
                fast.State.RebuildCount + 1,
                "teleport");
            DenseVirtualizationObservation teleported =
                DenseObserveVirtualization(entityManager, databaseEntity);
            DenseAssertBounded(teleported, "teleport");

            camera.transform.position =
                DenseCellCameraPosition(
                    originCoordinate,
                    databaseBlob.Value.CellSize);
            yield return DenseWaitForVirtualizationRebuild(
                entityManager,
                databaseEntity,
                teleported.State.RebuildCount + 1,
                "return");
            DenseVirtualizationObservation returned =
                DenseObserveVirtualization(entityManager, databaseEntity);
            DenseAssertBounded(returned, "return");
            DenseSlotBinding[] returnedBindings =
                DenseCaptureActiveBindings(entityManager);
            Assert.That(
                returnedBindings,
                Is.EqualTo(initialBindings),
                "Returning to the same packed camera envelope must reproduce exact slot bindings.");

            Debug.Log(
                "[DensePackedRenderVirtualizationRoute] result=Passed " +
                $"placements={returned.Metrics.LogicalPlacementCount} " +
                $"rows={returned.Metrics.LogicalPartCount} " +
                $"capacity={returned.Metrics.Capacity} " +
                $"active={returned.State.ActiveSlotCount} " +
                $"rebuilds={returned.State.RebuildCount} overflow=0");

            entityManager.DestroyEntity(activeMapEntity);
            activeMapEntity = Entity.Null;
            yield return null;
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
            Assert.That(loader.UnloadComplete, Is.True);
        }
        finally
        {
            if (world != null &&
                world.IsCreated &&
                activeMapEntity != Entity.Null &&
                world.EntityManager.Exists(activeMapEntity))
            {
                world.EntityManager.DestroyEntity(activeMapEntity);
            }
            cameraSystem?.ClearWorldCamera();
            if (cameraObject != null)
                UnityEngine.Object.DestroyImmediate(cameraObject);
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
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            Application.logMessageReceived -= logCallback;
        }
        Assert.That(
            unexpectedErrors,
            Is.Empty,
            "Dense packed virtualization route emitted unexpected error logs.");
    }

    private static IEnumerator DenseWaitForVirtualizationRebuild(
        EntityManager entityManager,
        Entity databaseEntity,
        int minimumRebuildCount,
        string stage)
    {
        float deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            DenseVirtualizationObservation observation =
                DenseObserveVirtualization(entityManager, databaseEntity);
            if (observation.State.InitialViewApplied != 0 &&
                observation.State.RebuildCount >= minimumRebuildCount &&
                observation.State.OverflowCount == 0 &&
                observation.Metrics.OverflowCount == 0 &&
                observation.Metrics.HighestDeficit == 0)
            {
                yield break;
            }
            yield return null;
        }
        Assert.Fail($"Dense packed virtualization did not complete stage '{stage}'.");
    }

    private static DenseVirtualizationObservation DenseObserveVirtualization(
        EntityManager entityManager,
        Entity databaseEntity)
    {
        return new DenseVirtualizationObservation
        {
            State = entityManager.GetComponentData<
                OperationMapRenderVirtualizationStateComponent>(databaseEntity),
            Metrics = entityManager.GetComponentData<
                OperationMapRenderVirtualizationMetricsComponent>(databaseEntity),
            CommandState = entityManager.GetComponentData<
                OperationMapRenderSlotCommandStateComponent>(databaseEntity)
        };
    }

    private static void DenseAssertBounded(
        DenseVirtualizationObservation observation,
        string stage)
    {
        Assert.That(observation.State.Initialized, Is.EqualTo(1), stage);
        Assert.That(observation.State.InitialViewApplied, Is.EqualTo(1), stage);
        Assert.That(observation.State.ActiveSlotCount, Is.GreaterThan(0), stage);
        Assert.That(
            observation.State.ActiveSlotCount,
            Is.EqualTo(observation.Metrics.EnabledSlotCount),
            stage);
        Assert.That(observation.State.OverflowCount, Is.Zero, stage);
        Assert.That(observation.Metrics.OverflowCount, Is.Zero, stage);
        Assert.That(observation.Metrics.HighestDeficit, Is.Zero, stage);
        Assert.That(
            observation.Metrics.EnabledSlotCount +
            observation.Metrics.DisabledSlotCount,
            Is.EqualTo(DenseExpectedVirtualizedSlotCount),
            stage);
        Assert.That(
            observation.Metrics.Capacity,
            Is.EqualTo(DenseExpectedVirtualizedSlotCount),
            stage);
        Assert.That(
            observation.Metrics.LogicalPlacementCount,
            Is.EqualTo(DenseExpectedVirtualizedPlacementCount),
            stage);
        Assert.That(
            observation.Metrics.LogicalPartCount,
            Is.EqualTo(DenseExpectedVirtualizedRowCount),
            stage);
        Assert.That(
            observation.Metrics.CommandVersion,
            Is.EqualTo(observation.CommandState.Version),
            stage);
    }

    private static void DenseAssertNoRebuild(
        DenseVirtualizationObservation before,
        DenseVirtualizationObservation after,
        string stage)
    {
        DenseAssertBounded(after, stage);
        Assert.That(after.State.RebuildCount, Is.EqualTo(before.State.RebuildCount), stage);
        Assert.That(after.Metrics.CommandVersion, Is.EqualTo(before.Metrics.CommandVersion), stage);
        Assert.That(after.CommandState.Version, Is.EqualTo(before.CommandState.Version), stage);
    }

    private static int DenseFindRouteOriginCell(
        BlobAssetReference<OperationMapRenderDatabaseBlob> database)
    {
        ref OperationMapRenderDatabaseBlob blob = ref database.Value;
        int2 center = new(
            (int)math.round(blob.GridOrigin.x / blob.CellSize) +
            blob.GridDimensions.x / 2,
            (int)math.round(blob.GridOrigin.z / blob.CellSize) +
            blob.GridDimensions.y / 2);
        int selected = -1;
        int selectedDistance = int.MaxValue;
        for (int index = 0; index < blob.Cells.Length; index++)
        {
            ref OperationMapRenderCellBlob cell = ref blob.Cells[index];
            if (cell.PlacementIndexCount <= 0)
                continue;
            int distance = math.csum(math.abs(cell.Coordinate - center));
            if (distance < selectedDistance)
            {
                selected = index;
                selectedDistance = distance;
            }
        }
        Assert.That(selected, Is.GreaterThanOrEqualTo(0));
        return selected;
    }

    private static int DenseFindSeparatedCell(
        BlobAssetReference<OperationMapRenderDatabaseBlob> database,
        int originCellIndex,
        int minimumDistance,
        bool chooseFarthest)
    {
        ref OperationMapRenderDatabaseBlob blob = ref database.Value;
        int2 origin = blob.Cells[originCellIndex].Coordinate;
        int selected = -1;
        int selectedDistance = chooseFarthest ? -1 : int.MaxValue;
        for (int index = 0; index < blob.Cells.Length; index++)
        {
            ref OperationMapRenderCellBlob cell = ref blob.Cells[index];
            if (cell.PlacementIndexCount <= 0)
                continue;
            int distance = math.csum(math.abs(cell.Coordinate - origin));
            if (distance < minimumDistance)
                continue;
            if ((chooseFarthest && distance > selectedDistance) ||
                (!chooseFarthest && distance < selectedDistance))
            {
                selected = index;
                selectedDistance = distance;
            }
        }
        Assert.That(selected, Is.GreaterThanOrEqualTo(0));
        return selected;
    }

    private static int2 DenseChooseAdjacentCoordinate(
        BlobAssetReference<OperationMapRenderDatabaseBlob> database,
        int2 coordinate)
    {
        ref OperationMapRenderDatabaseBlob blob = ref database.Value;
        int2 minimum = new(
            (int)math.round(blob.GridOrigin.x / blob.CellSize),
            (int)math.round(blob.GridOrigin.z / blob.CellSize));
        int2 maximum = minimum + blob.GridDimensions - 1;
        if (coordinate.x < maximum.x)
            return coordinate + new int2(1, 0);
        if (coordinate.x > minimum.x)
            return coordinate - new int2(1, 0);
        if (coordinate.y < maximum.y)
            return coordinate + new int2(0, 1);
        Assert.That(coordinate.y, Is.GreaterThan(minimum.y));
        return coordinate - new int2(0, 1);
    }

    private static Vector3 DenseCellCameraPosition(int2 coordinate, float cellSize)
    {
        return new Vector3(
            (coordinate.x + 0.5f) * cellSize,
            180f,
            (coordinate.y + 0.5f) * cellSize);
    }

    private static DenseSlotBinding[] DenseCaptureActiveBindings(
        EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(
            new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<OperationMapRenderProxySlotComponent>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities |
                          EntityQueryOptions.IgnoreComponentEnabledState
            });
        using NativeArray<OperationMapRenderProxySlotComponent> slots =
            query.ToComponentDataArray<OperationMapRenderProxySlotComponent>(
                Allocator.Temp);
        var bindings = new List<DenseSlotBinding>();
        for (int index = 0; index < slots.Length; index++)
        {
            OperationMapRenderProxySlotComponent slot = slots[index];
            if (slot.PlacementIndex < 0)
                continue;
            bindings.Add(new DenseSlotBinding
            {
                SlotIndex = slot.SlotIndex,
                PlacementIndex = slot.PlacementIndex,
                PartIndex = slot.PartIndex,
                PoolBucketIndex = slot.PoolBucketIndex
            });
        }
        bindings.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
        return bindings.ToArray();
    }

    private struct DenseVirtualizationObservation
    {
        public OperationMapRenderVirtualizationStateComponent State;
        public OperationMapRenderVirtualizationMetricsComponent Metrics;
        public OperationMapRenderSlotCommandStateComponent CommandState;
    }

    private struct DenseSlotBinding : IEquatable<DenseSlotBinding>
    {
        public int SlotIndex;
        public int PlacementIndex;
        public int PartIndex;
        public int PoolBucketIndex;

        public bool Equals(DenseSlotBinding other) =>
            SlotIndex == other.SlotIndex &&
            PlacementIndex == other.PlacementIndex &&
            PartIndex == other.PartIndex &&
            PoolBucketIndex == other.PoolBucketIndex;

        public override bool Equals(object obj) =>
            obj is DenseSlotBinding other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                SlotIndex,
                PlacementIndex,
                PartIndex,
                PoolBucketIndex);
    }
}
