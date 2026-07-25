using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Composition;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Hash128 = Unity.Entities.Hash128;

public sealed partial class OperationMapEntityScenePackedRuntimeParityPlayModeTests
{
    private const uint DenseParityMagic = 0x57444350;
    private const int DenseParityFormatVersion = 3;
    private const int DenseExpectedLegacyIdentityCount = 9544;
    private const int DenseExpectedGeneratedIdentityCount = 36946;
    private const int DenseExpectedRenderRowCount = 82797;
    private const int DenseGraphicsStableFrameCount = 3;
    private const int DenseGraphicsReadinessFrameLimit = 120;
    private const string DenseDefinitionAddress =
        "operation-map-candidate/opmap.skirmish.desert_base_01/dense-city/definition";
    private const string DenseParityManifestPath =
        "Library/OperationMapDenseCityRuntimeParity/dense_candidate_runtime_parity.bin";
    private const string DenseParitySummaryPath =
        "Design/AgentReports/2026-07-24_dense_city_runtime_parity_manifest.json";
    private const string DenseRuntimeContentReportPath =
        "Design/AgentReports/2026-07-24_dense_city_candidate_runtime_content.json";
    private const string DenseAddressablesCatalogPath =
        "Library/OperationMapDenseCityRuntimeContent/Addressables/catalog.bin";
    private const string DenseEntityContentPath =
        "Library/OperationMapDenseCityRuntimeContent/Entities";
    private const string DenseCandidateSubScenePath =
        "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
        "opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
    private const string DenseCandidateDefinitionPath =
        "Assets/Game/Configs/OperationMaps/Candidates/" +
        "OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
    private const string DenseCandidateRuntimeBindingPath =
        "Assets/Game/GeneratedOperationMaps/RuntimeBinding/" +
        "opmap.skirmish.desert_base_01/Candidates/" +
        "opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity";
    private const string DenseDirectBakeParityReportPath =
        "Design/AgentReports/2026-07-24_dense_city_generated_transform_parity.json";
    private const string DenseEditorFixedCameraReportPath =
        "Design/AgentReports/2026-07-24_dense_city_editor_fixed_camera_baseline.json";
    private const string DenseRuntimeFixedCameraReportPath =
        "Design/AgentReports/2026-07-24_dense_city_runtime_fixed_camera_parity.json";
    private const string DenseRuntimeFixedCameraCaptureDirectory =
        "Design/AgentReports/Captures/2026-07-24_dense_city_runtime_fixed_camera_parity";

    [Test]
    public void DenseComparePixels_UniformColorDriftRemainsInteriorFailure()
    {
        const int width = 3;
        const int height = 3;
        var source = new Color32[width * height];
        var runtime = new Color32[width * height];
        for (int i = 0; i < source.Length; i++)
        {
            source[i] = new Color32(10, 10, 10, 255);
            runtime[i] = new Color32(20, 20, 20, 255);
        }

        DensePixelComparison comparison = DenseComparePixels(
            source,
            runtime,
            width,
            height,
            changedThreshold: 3,
            edgeThreshold: 16);

        Assert.That(comparison.RawChangedPixelRatio, Is.EqualTo(1f));
        Assert.That(comparison.InteriorChangedPixelRatio, Is.EqualTo(1f));
        Assert.That(comparison.EdgePixelRatio, Is.EqualTo(0f));
    }

    [Test]
    public void DenseComparePixels_OnePixelSilhouetteShiftIsEdgeDiagnostic()
    {
        const int width = 5;
        const int height = 3;
        var source = new Color32[width * height];
        var runtime = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                source[index] = x < 2
                    ? new Color32(0, 0, 0, 255)
                    : new Color32(255, 255, 255, 255);
                runtime[index] = x < 3
                    ? new Color32(0, 0, 0, 255)
                    : new Color32(255, 255, 255, 255);
            }
        }

        DensePixelComparison comparison = DenseComparePixels(
            source,
            runtime,
            width,
            height,
            changedThreshold: 3,
            edgeThreshold: 16);

        Assert.That(comparison.RawChangedPixelRatio, Is.EqualTo(3f / 15f));
        Assert.That(comparison.InteriorChangedPixelRatio, Is.EqualTo(0f));
        Assert.That(comparison.EdgePixelRatio, Is.GreaterThan(0f));
    }

    [UnityTest]
    [Timeout(600000)]
    public IEnumerator DensePackedCandidate_TwoLoadCyclesMatchManifestMatricesAndBounds()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string manifestPath = DenseResolve(projectRoot, DenseParityManifestPath);
        string summaryPath = DenseResolve(projectRoot, DenseParitySummaryPath);
        string runtimeContentReportPath =
            DenseResolve(projectRoot, DenseRuntimeContentReportPath);
        string addressablesCatalogPath =
            DenseResolve(projectRoot, DenseAddressablesCatalogPath);
        string entityContentRoot = DenseResolve(projectRoot, DenseEntityContentPath);
        string entityCatalogPath = Path.Combine(
            entityContentRoot,
            RuntimeContentManager.RelativeCatalogPath);

        DenseRequireFile(manifestPath);
        DenseRequireFile(summaryPath);
        DenseRequireFile(runtimeContentReportPath);
        DenseRequireFile(addressablesCatalogPath);
        DenseRequireFile(entityCatalogPath);

        DenseParitySummary summary = JsonUtility.FromJson<DenseParitySummary>(
            File.ReadAllText(summaryPath));
        DenseRuntimeContentReport runtimeContent =
            JsonUtility.FromJson<DenseRuntimeContentReport>(
                File.ReadAllText(runtimeContentReportPath));
        DenseParityManifest expected = DenseParityManifest.Read(manifestPath);
        DenseValidateFingerprints(
            projectRoot,
            manifestPath,
            addressablesCatalogPath,
            entityCatalogPath,
            summary,
            runtimeContent,
            expected);

        AsyncOperationHandle<IResourceLocator> catalogHandle = default;
        AsyncOperationHandle<OperationMapDefinition> definitionHandle = default;
        World parityWorld = null;
        bool airMovementStateCaptured = false;
        bool airMovementWasEnabled = false;
        bool bladeSpinStateCaptured = false;
        bool bladeSpinWasEnabled = false;
        bool transportDoorStateCaptured = false;
        bool transportDoorWasEnabled = false;
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
            Assert.That(catalogHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                catalogHandle.OperationException?.Message);

            definitionHandle =
                Addressables.LoadAssetAsync<OperationMapDefinition>(DenseDefinitionAddress);
            yield return definitionHandle;
            Assert.That(definitionHandle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                definitionHandle.OperationException?.Message);
            Assert.That(definitionHandle.Result, Is.Not.Null);
            Assert.That(
                definitionHandle.Result.OperationMapId,
                Is.EqualTo(expected.OperationMapId));
            Assert.That(
                definitionHandle.Result.NavigationMetadata.AuthoredSubSceneGuid,
                Is.EqualTo(expected.EntitySceneGuid));

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
            {
                yield return DenseRunLoadCaptureUnloadCycle(
                    definitionHandle.Result,
                    expected,
                    cycle);
            }
        }
        finally
        {
            if (parityWorld != null && parityWorld.IsCreated)
            {
                if (airMovementStateCaptured)
                    SetSystemEnabled<UnitAirMovementSystem>(parityWorld, airMovementWasEnabled);
                if (bladeSpinStateCaptured)
                    SetSystemEnabled<UnitHelicopterBladeSpinSystem>(parityWorld, bladeSpinWasEnabled);
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
            LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            Application.logMessageReceived -= logCallback;
        }
        Assert.That(
            unexpectedErrors,
            Is.Empty,
            "Dense packed parity emitted unexpected error logs.");
    }

    private static IEnumerator DenseRunLoadCaptureUnloadCycle(
        OperationMapDefinition definition,
        DenseParityManifest expected,
        int cycle)
    {
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
            $"Dense candidate source scene did not become ready in cycle {cycle}.");
        Assert.That(loader.Manifest, Is.Null,
            "Dense EntityScene presentation must not resolve a static presentation manifest.");

        World world = World.DefaultGameObjectInjectionWorld;
        Assert.That(world, Is.Not.Null);
        Assert.That(world.IsCreated, Is.True);

        var sceneGuid = new Hash128(expected.EntitySceneGuid);
        Assert.That(sceneGuid.IsValid, Is.True);
        Entity sceneEntity = Entity.Null;
        deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (Time.realtimeSinceStartup < deadline)
        {
            sceneEntity = SceneSystem.GetSceneEntity(world.Unmanaged, sceneGuid);
            if (sceneEntity != Entity.Null &&
                SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity))
                break;
            yield return null;
        }

        Assert.That(sceneEntity, Is.Not.EqualTo(Entity.Null),
            $"Dense EntityScene root was not created in cycle {cycle}.");
        Assert.That(SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity), Is.True,
            $"Dense EntityScene did not finish streaming in cycle {cycle}.");

        Scene sourceScene = loader.SceneView.gameObject.scene;
        Assert.That(sourceScene.IsValid(), Is.True);
        Assert.That(sourceScene.isLoaded, Is.True);
        Entity[] sectionEntities = GetResolvedSectionEntities(
            world.EntityManager,
            sceneEntity);
        DenseRuntimeCapture actual = DenseCapture(
            world.EntityManager,
            sectionEntities);
        DenseCompare(expected, actual, cycle);
        DenseAuditPackedMaterialDependencies(world.EntityManager);
        DenseAssertOperationMapBuildingsRetainAuthoredMaterials(world.EntityManager);
        yield return DenseCaptureFixedCameraRuntime(expected, cycle);
        DenseValidateGeneratedBuildingDestructionUsesBakedEntitiesOnly(
            world,
            cycle);

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
            $"Dense candidate source scene did not unload in cycle {cycle}.");

        deadline = Time.realtimeSinceStartup + MaximumWaitSeconds;
        while (Time.realtimeSinceStartup < deadline &&
               world.EntityManager.Exists(sceneEntity))
            yield return null;

        Assert.That(world.EntityManager.Exists(sceneEntity), Is.False,
            $"Dense EntityScene metadata remained after cycle {cycle}.");
        for (int i = 0; i < sectionEntities.Length; i++)
        {
            Assert.That(world.EntityManager.Exists(sectionEntities[i]), Is.False,
                $"Dense EntityScene section metadata {i} remained after cycle {cycle}.");
        }
        Assert.That(sourceScene.isLoaded, Is.False,
            $"Dense thin runtime-binding scene remained loaded after cycle {cycle}.");
    }

    private static void DenseValidateGeneratedBuildingDestructionUsesBakedEntitiesOnly(
        World world,
        int cycle)
    {
        EntityManager entityManager = world.EntityManager;
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadWrite<UnitHealth>(),
            ComponentType.ReadOnly<OperationMapBuildingComponent>(),
            ComponentType.ReadOnly<OperationMapBuildingIdentity>(),
            ComponentType.ReadWrite<OperationMapBuildingPresentation>());
        using NativeArray<Entity> buildings = query.ToEntityArray(Allocator.Temp);

        Entity building = Entity.Null;
        OperationMapBuildingPresentation presentation = default;
        for (int index = 0; index < buildings.Length; index++)
        {
            Entity candidate = buildings[index];
            OperationMapBuildingIdentity identity =
                entityManager.GetComponentData<OperationMapBuildingIdentity>(candidate);
            OperationMapBuildingComponent buildingData =
                entityManager.GetComponentData<OperationMapBuildingComponent>(candidate);
            OperationMapBuildingPresentation candidatePresentation =
                entityManager.GetComponentData<OperationMapBuildingPresentation>(candidate);
            if (!OperationMapIdentityRules.IsValidGeneratedStableId(
                    identity.StableId.ToString()) ||
                buildingData.BlockerPolicy !=
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

        Assert.That(
            building,
            Is.Not.EqualTo(Entity.Null),
            $"Dense packed cycle {cycle} contains no complete generated building.");
        Assert.That(
            UnityEngine.Object.FindObjectsByType<RuntimeBuildingEntityLink>(
                FindObjectsInactive.Include),
            Is.Empty,
            "Dense-generated buildings must not create managed runtime entity links.");

        HashSet<int> gameObjectIdsBefore = CaptureLoadedGameObjectIds();
        int entityCountBefore = entityManager.UniversalQuery.CalculateEntityCount();
        UnitHealth health = entityManager.GetComponentData<UnitHealth>(building);
        health.Current = 0;
        entityManager.SetComponentData(building, health);

        SystemHandle handle =
            world.Unmanaged.GetExistingUnmanagedSystem<OperationMapBuildingDestructionSystem>();
        Assert.That(handle, Is.Not.EqualTo(SystemHandle.Null));
        ref SystemState state = ref world.Unmanaged.ResolveSystemStateRef(handle);
        world.Unmanaged.GetUnsafeSystemRef<OperationMapBuildingDestructionSystem>(handle)
            .OnUpdate(ref state);
        state.Dependency.Complete();
        entityManager.CompleteAllTrackedJobs();

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
            "Dense building destruction must not instantiate replacement entities.");
        Assert.That(
            CaptureLoadedGameObjectIds(),
            Is.EquivalentTo(gameObjectIdsBefore),
            "Dense building destruction must not instantiate or destroy GameObject replacements.");
    }

    private static void DenseAssertOperationMapBuildingsRetainAuthoredMaterials(
        EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<FactionTintTarget>(),
            ComponentType.ReadOnly<Parent>());
        using NativeArray<Entity> targets = query.ToEntityArray(Allocator.Temp);
        int operationMapBuildingTargetCount = 0;
        int legacyIdentityTargetCount = 0;
        int denseIdentityTargetCount = 0;
        for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
        {
            Entity current = targets[targetIndex];
            for (int depth = 0; depth < 64; depth++)
            {
                if (entityManager.HasComponent<OperationMapEntityPresentationIdentity>(current))
                    legacyIdentityTargetCount++;
                if (entityManager.HasComponent<DenseCityPresentationIdentity>(current))
                    denseIdentityTargetCount++;
                if (entityManager.HasComponent<OperationMapBuildingComponent>(current))
                {
                    operationMapBuildingTargetCount++;
                    break;
                }

                if (!entityManager.HasComponent<Parent>(current))
                    break;

                current = entityManager.GetComponentData<Parent>(current).Value;
            }
        }

        Debug.Log(
            $"[DensePackedMaterialOverrideAudit] factionTintTargets={targets.Length} " +
            $"operationMapBuildingTargets={operationMapBuildingTargetCount} " +
            $"legacyIdentityTargets={legacyIdentityTargetCount} " +
            $"denseIdentityTargets={denseIdentityTargetCount}");
        DenseAuditUrpBaseColorOverrides(entityManager);
        Assert.That(
            operationMapBuildingTargetCount,
            Is.Zero,
            "Permanent operation-map building renderers must retain authored material colors.");
    }

    private static void DenseAuditPackedMaterialDependencies(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<MaterialMeshInfo>(),
            ComponentType.ReadOnly<RenderMeshArray>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        var materials = new HashSet<Material>();
        int unresolvedMaterialRows = 0;
        for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
        {
            Entity entity = entities[entityIndex];
            MaterialMeshInfo materialMeshInfo =
                entityManager.GetComponentData<MaterialMeshInfo>(entity);
            RenderMeshArray renderMeshArray =
                entityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
            Material material = renderMeshArray.GetMaterial(materialMeshInfo);
            if (material == null)
            {
                unresolvedMaterialRows++;
                continue;
            }

            materials.Add(material);
        }

        int baseMapPropertyCount = 0;
        int resolvedBaseMapCount = 0;
        int missingBaseMapCount = 0;
        int nonWhiteBaseColorCount = 0;
        var shaderNames = new HashSet<string>(StringComparer.Ordinal);
        var missingBaseMapSamples = new StringBuilder();
        var nonWhiteBaseColorSamples = new StringBuilder();
        foreach (Material material in materials)
        {
            shaderNames.Add(material.shader != null ? material.shader.name : "<null>");
            if (material.HasProperty("_BaseColor") &&
                material.GetColor("_BaseColor") != Color.white)
            {
                nonWhiteBaseColorCount++;
                if (nonWhiteBaseColorSamples.Length < 1024)
                {
                    if (nonWhiteBaseColorSamples.Length > 0)
                        nonWhiteBaseColorSamples.Append(", ");
                    Color color = material.GetColor("_BaseColor");
                    nonWhiteBaseColorSamples.Append(material.name)
                        .Append('=')
                        .Append(color)
                        .Append('@')
                        .Append(material.shader != null ? material.shader.name : "<null>");
                }
            }
            if (!material.HasProperty("_BaseMap"))
                continue;

            baseMapPropertyCount++;
            if (material.GetTexture("_BaseMap") != null)
            {
                resolvedBaseMapCount++;
                continue;
            }

            missingBaseMapCount++;
            if (missingBaseMapSamples.Length < 512)
            {
                if (missingBaseMapSamples.Length > 0)
                    missingBaseMapSamples.Append(", ");
                missingBaseMapSamples.Append(material.name);
            }
        }

        Debug.Log(
            $"[DensePackedMaterialDependencyAudit] renderRows={entities.Length} " +
            $"materials={materials.Count} shaders={shaderNames.Count} " +
            $"unresolvedMaterialRows={unresolvedMaterialRows} " +
            $"baseMapProperties={baseMapPropertyCount} " +
            $"resolvedBaseMaps={resolvedBaseMapCount} missingBaseMaps={missingBaseMapCount} " +
            $"nonWhiteBaseColors={nonWhiteBaseColorCount} " +
            $"shaderNames=[{string.Join(", ", shaderNames)}] " +
            $"missingBaseMapSamples=[{missingBaseMapSamples}] " +
            $"nonWhiteBaseColorSamples=[{nonWhiteBaseColorSamples}]");
        Assert.That(
            unresolvedMaterialRows,
            Is.Zero,
            "Every packed render row must resolve its material from RenderMeshArray.");
    }

    private static void DenseAuditUrpBaseColorOverrides(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<URPMaterialPropertyBaseColor>(),
            ComponentType.ReadOnly<MaterialMeshInfo>(),
            ComponentType.ReadOnly<RenderMeshArray>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        int legacyIdentityCount = 0;
        int denseIdentityCount = 0;
        int nonWhiteCount = 0;
        int unresolvedMaterialCount = 0;
        int materialColorMismatchCount = 0;
        var materialColorMismatchSamples = new StringBuilder();
        for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
        {
            Entity entity = entities[entityIndex];
            float4 value = entityManager
                .GetComponentData<URPMaterialPropertyBaseColor>(entity)
                .Value;
            if (!math.all(value == new float4(1f, 1f, 1f, 1f)))
                nonWhiteCount++;

            MaterialMeshInfo materialMeshInfo =
                entityManager.GetComponentData<MaterialMeshInfo>(entity);
            RenderMeshArray renderMeshArray =
                entityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
            Material material = renderMeshArray.GetMaterial(materialMeshInfo);
            if (material == null || !material.HasProperty("_BaseColor"))
            {
                unresolvedMaterialCount++;
            }
            else
            {
                Color color = material.GetColor("_BaseColor").linear;
                var expected = new float4(color.r, color.g, color.b, color.a);
                if (!math.all(math.abs(value - expected) <= 0.0001f))
                {
                    materialColorMismatchCount++;
                    if (materialColorMismatchSamples.Length < 2048)
                    {
                        if (materialColorMismatchSamples.Length > 0)
                            materialColorMismatchSamples.Append(", ");
                        materialColorMismatchSamples.Append(material.name)
                            .Append('@')
                            .Append(material.shader != null ? material.shader.name : "<null>")
                            .Append(":expected=")
                            .Append(expected)
                            .Append(":actual=")
                            .Append(value);
                    }
                }
            }

            Entity current = entity;
            for (int depth = 0; depth < 64; depth++)
            {
                if (entityManager.HasComponent<OperationMapEntityPresentationIdentity>(current))
                    legacyIdentityCount++;
                if (entityManager.HasComponent<DenseCityPresentationIdentity>(current))
                    denseIdentityCount++;
                if (!entityManager.HasComponent<Parent>(current))
                    break;
                current = entityManager.GetComponentData<Parent>(current).Value;
            }
        }

        Debug.Log(
            $"[DensePackedUrpBaseColorAudit] overrides={entities.Length} " +
            $"nonWhite={nonWhiteCount} legacyIdentityOverrides={legacyIdentityCount} " +
            $"denseIdentityOverrides={denseIdentityCount} " +
            $"unresolvedMaterials={unresolvedMaterialCount} " +
            $"materialColorMismatches={materialColorMismatchCount} " +
            $"mismatchSamples=[{materialColorMismatchSamples}]");
        Assert.That(
            unresolvedMaterialCount,
            Is.Zero,
            "Every packed base-color override must resolve a material with _BaseColor.");
        Assert.That(
            materialColorMismatchCount,
            Is.Zero,
            "Packed base-color overrides must preserve each resolved material's exact linear color.");
    }

    private static IEnumerator DenseCaptureFixedCameraRuntime(
        DenseParityManifest expected,
        int cycle)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string baselinePath = DenseResolve(projectRoot, DenseEditorFixedCameraReportPath);
        DenseRequireFile(baselinePath);
        DenseEditorFixedCameraReport baseline =
            JsonUtility.FromJson<DenseEditorFixedCameraReport>(
                File.ReadAllText(baselinePath));
        Assert.That(
            baseline.schema,
            Is.EqualTo("warline.operation-map.dense-city-editor-fixed-camera-baseline"));
        Assert.That(baseline.schemaVersion, Is.EqualTo(1));
        Assert.That(baseline.operationMapId, Is.EqualTo(expected.OperationMapId));
        Assert.That(baseline.candidateSubSceneSha256, Is.EqualTo(expected.SubSceneSha256));
        Assert.That(baseline.width, Is.EqualTo(1280));
        Assert.That(baseline.height, Is.EqualTo(720));
        Assert.That(baseline.viewCount, Is.EqualTo(5));
        Assert.That(baseline.rows, Has.Length.EqualTo(baseline.viewCount));

        string captureDirectory =
            DenseResolve(projectRoot, DenseRuntimeFixedCameraCaptureDirectory);
        Directory.CreateDirectory(captureDirectory);
        var runtimeRows = new DenseRuntimeFixedCameraRow[baseline.rows.Length];
        Camera[] existingCameras = Camera.allCameras;
        var cameraStates = new bool[existingCameras.Length];
        Light[] existingLights = UnityEngine.Object.FindObjectsByType<Light>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        var lightStates = new bool[existingLights.Length];
        var cameraObject = new GameObject("DensePackedFixedCamera");
        var lightObject = new GameObject("DensePackedFixedCameraLight");
        Camera camera = cameraObject.AddComponent<Camera>();
        Light light = lightObject.AddComponent<Light>();
        Color previousAmbientLight = RenderSettings.ambientLight;
        float previousAmbientIntensity = RenderSettings.ambientIntensity;
        UnityEngine.Rendering.AmbientMode previousAmbientMode =
            RenderSettings.ambientMode;
        ShadowQuality previousShadowQuality = QualitySettings.shadows;
        try
        {
            for (int i = 0; i < existingCameras.Length; i++)
            {
                cameraStates[i] = existingCameras[i].enabled;
                existingCameras[i].enabled = false;
            }
            for (int i = 0; i < existingLights.Length; i++)
            {
                lightStates[i] = existingLights[i].enabled;
                existingLights[i].enabled = false;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20000f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.aspect = baseline.width / (float)baseline.height;
            camera.enabled = true;

            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f, 1f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
            light.enabled = true;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.72f, 1f);
            RenderSettings.ambientIntensity = 1f;
            QualitySettings.shadows = ShadowQuality.Disable;

            for (int i = 0; i < baseline.rows.Length; i++)
            {
                DenseEditorFixedCameraRow source = baseline.rows[i];
                string sourcePath = DenseResolve(projectRoot, source.editorPath);
                DenseRequireFile(sourcePath);
                Assert.That(ComputeSha256(sourcePath), Is.EqualTo(source.editorSha256));

                camera.transform.SetPositionAndRotation(
                    DenseVector3(source.cameraPosition),
                    Quaternion.Euler(DenseVector3(source.cameraRotation)));
                camera.orthographic = source.orthographic != 0;
                camera.fieldOfView = source.fieldOfView;
                camera.orthographicSize = source.orthographicSize;

                Color32[] runtimePixels = null;
                byte[] runtimePng = null;
                yield return DenseRenderCamera(
                    camera,
                    baseline.width,
                    baseline.height,
                    (pixels, png) =>
                    {
                        runtimePixels = pixels;
                        runtimePng = png;
                    });
                Assert.That(runtimePixels, Is.Not.Null);
                Assert.That(runtimePng, Is.Not.Null);

                byte[] sourcePng = File.ReadAllBytes(sourcePath);
                var sourceTexture =
                    new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                try
                {
                    Assert.That(sourceTexture.LoadImage(sourcePng, markNonReadable: false), Is.True);
                    Color32[] sourcePixels = sourceTexture.GetPixels32();
                    DensePixelComparison comparison = DenseComparePixels(
                        sourcePixels,
                        runtimePixels,
                        baseline.width,
                        baseline.height,
                        3,
                        16);
                    string runtimeRelative =
                        $"{DenseRuntimeFixedCameraCaptureDirectory}/" +
                        $"{source.view}_cycle_{cycle:D2}_runtime.png";
                    string runtimePath = DenseResolve(projectRoot, runtimeRelative);
                    File.WriteAllBytes(runtimePath, runtimePng);
                    bool passed =
                        comparison.MeanChannelDelta <= baseline.maximumMeanChannelDelta &&
                        comparison.InteriorChangedPixelRatio <=
                            baseline.maximumChangedPixelRatio &&
                        comparison.SourceLumaVariance > 0.0001f &&
                        comparison.RuntimeLumaVariance > 0.0001f;
                    runtimeRows[i] = new DenseRuntimeFixedCameraRow
                    {
                        view = source.view,
                        result = passed ? "Passed" : "Rejected",
                        editorPath = source.editorPath,
                        runtimePath = runtimeRelative,
                        editorSha256 = source.editorSha256,
                        runtimeSha256 = ComputeSha256(runtimePath),
                        meanChannelDelta = comparison.MeanChannelDelta,
                        maximumChannelDelta = comparison.MaximumChannelDelta,
                        changedPixelRatio = comparison.RawChangedPixelRatio,
                        interiorChangedPixelRatio =
                            comparison.InteriorChangedPixelRatio,
                        edgePixelRatio = comparison.EdgePixelRatio,
                        editorLumaVariance = comparison.SourceLumaVariance,
                        runtimeLumaVariance = comparison.RuntimeLumaVariance
                    };
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(sourceTexture);
                }
            }
        }
        finally
        {
            QualitySettings.shadows = previousShadowQuality;
            RenderSettings.ambientMode = previousAmbientMode;
            RenderSettings.ambientLight = previousAmbientLight;
            RenderSettings.ambientIntensity = previousAmbientIntensity;
            for (int i = 0; i < existingLights.Length; i++)
            {
                if (existingLights[i] != null)
                    existingLights[i].enabled = lightStates[i];
            }
            for (int i = 0; i < existingCameras.Length; i++)
            {
                if (existingCameras[i] != null)
                    existingCameras[i].enabled = cameraStates[i];
            }
            UnityEngine.Object.DestroyImmediate(lightObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        string reportPath = DenseResolve(projectRoot, DenseRuntimeFixedCameraReportPath);
        var allRows = new List<DenseRuntimeFixedCameraRow>(baseline.rows.Length * cycle);
        if (cycle > 1)
        {
            DenseRequireFile(reportPath);
            DenseRuntimeFixedCameraReport previous =
                JsonUtility.FromJson<DenseRuntimeFixedCameraReport>(
                    File.ReadAllText(reportPath));
            Assert.That(previous.cycleCount, Is.EqualTo(cycle - 1));
            Assert.That(previous.rows, Has.Length.EqualTo(baseline.rows.Length * (cycle - 1)));
            allRows.AddRange(previous.rows);
        }
        allRows.AddRange(runtimeRows);

        int rejected = 0;
        for (int i = 0; i < allRows.Count; i++)
        {
            if (!string.Equals(allRows[i].result, "Passed", StringComparison.Ordinal))
                rejected++;
        }
        var report = new DenseRuntimeFixedCameraReport
        {
            schema = "warline.operation-map.dense-city-runtime-fixed-camera-parity",
            schemaVersion = 2,
            operationMapId = expected.OperationMapId,
            result = rejected == 0
                ? "DenseCityRuntimeFixedCameraParityPassed"
                : "DenseCityRuntimeFixedCameraParityRejected",
            candidateSubSceneSha256 = expected.SubSceneSha256,
            manifestSha256 = ComputeSha256(
                DenseResolve(projectRoot, DenseParityManifestPath)),
            width = baseline.width,
            height = baseline.height,
            editorRendererCount = baseline.rendererCount,
            runtimeRenderRowCount = DenseExpectedRenderRowCount,
            cycleCount = cycle,
            viewCount = allRows.Count,
            rejectedViewCount = rejected,
            maximumMeanChannelDelta = baseline.maximumMeanChannelDelta,
            maximumChangedPixelRatio = baseline.maximumChangedPixelRatio,
            productionCutover = 0,
            rows = allRows.ToArray()
        };
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + "\n");
        Assert.That(
            rejected,
            Is.Zero,
            $"Dense fixed-camera parity rejected {rejected}/{allRows.Count} views. " +
            $"Report: {DenseRuntimeFixedCameraReportPath}");
    }

    private static IEnumerator DenseRenderCamera(
        Camera camera,
        int width,
        int height,
        Action<Color32[], byte[]> completed)
    {
        var destination = new RenderTexture(
            width,
            height,
            24,
            RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1,
            name = "DensePackedFixedCameraParityTarget"
        };
        try
        {
            var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest
            {
                destination = destination
            };
            Assert.That(
                UnityEngine.Rendering.RenderPipeline.SupportsRenderRequest(camera, request),
                Is.True);
            World world = World.DefaultGameObjectInjectionWorld;
            Assert.That(world, Is.Not.Null);
            EntitiesGraphicsSystem graphics =
                world.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            Assert.That(graphics, Is.Not.Null);

            int stableFrameCount = 0;
            int previousBatchCount = -1;
            int previousChunkTotal = -1;
            int previousRenderedInstanceCount = -1;
            int previousDrawCommandCount = -1;
            EntitiesGraphicsStats stats = default;
            for (int frame = 0;
                 frame < DenseGraphicsReadinessFrameLimit &&
                 stableFrameCount < DenseGraphicsStableFrameCount;
                 frame++)
            {
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, request);
                yield return null;

                stats = graphics.Stats;
                bool ready =
                    stats.BatchCount > 0 &&
                    stats.ChunkTotal > 0 &&
                    stats.RenderedInstanceCount > 0 &&
                    stats.DrawCommandCount > 0;
                bool unchanged =
                    ready &&
                    stats.BatchCount == previousBatchCount &&
                    stats.ChunkTotal == previousChunkTotal &&
                    stats.RenderedInstanceCount == previousRenderedInstanceCount &&
                    stats.DrawCommandCount == previousDrawCommandCount;
                stableFrameCount = unchanged ? stableFrameCount + 1 : ready ? 1 : 0;
                previousBatchCount = stats.BatchCount;
                previousChunkTotal = stats.ChunkTotal;
                previousRenderedInstanceCount = stats.RenderedInstanceCount;
                previousDrawCommandCount = stats.DrawCommandCount;
            }
            Assert.That(
                stableFrameCount,
                Is.GreaterThanOrEqualTo(DenseGraphicsStableFrameCount),
                "Entities Graphics did not reach stable nonzero culling/draw readiness: " +
                $"batches={stats.BatchCount}, chunks={stats.ChunkTotal}, " +
                $"instances={stats.RenderedInstanceCount}, draws={stats.DrawCommandCount}.");

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = destination;
            var texture = new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false,
                false);
            try
            {
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply(false, false);
                completed(texture.GetPixels32(), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                RenderTexture.active = previous;
            }
        }
        finally
        {
            destination.Release();
            UnityEngine.Object.DestroyImmediate(destination);
        }
    }

    private static DensePixelComparison DenseComparePixels(
        IReadOnlyList<Color32> source,
        IReadOnlyList<Color32> runtime,
        int width,
        int height,
        byte changedThreshold,
        byte edgeThreshold)
    {
        Assert.That(runtime.Count, Is.EqualTo(source.Count));
        Assert.That(source.Count, Is.EqualTo(width * height));
        long totalDelta = 0;
        int maximumDelta = 0;
        int rawChanged = 0;
        int interiorChanged = 0;
        int edgePixels = 0;
        double sourceLuma = 0d;
        double sourceLumaSquared = 0d;
        double runtimeLuma = 0d;
        double runtimeLumaSquared = 0d;
        for (int i = 0; i < source.Count; i++)
        {
            int red = Math.Abs(source[i].r - runtime[i].r);
            int green = Math.Abs(source[i].g - runtime[i].g);
            int blue = Math.Abs(source[i].b - runtime[i].b);
            int alpha = Math.Abs(source[i].a - runtime[i].a);
            int pixelMaximum = Math.Max(Math.Max(red, green), Math.Max(blue, alpha));
            totalDelta += red + green + blue + alpha;
            maximumDelta = Math.Max(maximumDelta, pixelMaximum);
            bool edge = DenseIsEdgePixel(
                source,
                runtime,
                width,
                height,
                i,
                edgeThreshold);
            if (edge)
                edgePixels++;
            if (pixelMaximum > changedThreshold)
            {
                rawChanged++;
                if (!edge)
                    interiorChanged++;
            }
            double sourceValue =
                (0.2126d * source[i].r + 0.7152d * source[i].g +
                 0.0722d * source[i].b) / 255d;
            double runtimeValue =
                (0.2126d * runtime[i].r + 0.7152d * runtime[i].g +
                 0.0722d * runtime[i].b) / 255d;
            sourceLuma += sourceValue;
            sourceLumaSquared += sourceValue * sourceValue;
            runtimeLuma += runtimeValue;
            runtimeLumaSquared += runtimeValue * runtimeValue;
        }
        double count = source.Count;
        double sourceMean = sourceLuma / count;
        double runtimeMean = runtimeLuma / count;
        return new DensePixelComparison(
            (float)(totalDelta / (count * 4d * 255d)),
            maximumDelta / 255f,
            rawChanged / (float)count,
            interiorChanged / (float)count,
            edgePixels / (float)count,
            (float)Math.Max(0d, sourceLumaSquared / count - sourceMean * sourceMean),
            (float)Math.Max(0d, runtimeLumaSquared / count - runtimeMean * runtimeMean));
    }

    private static bool DenseIsEdgePixel(
        IReadOnlyList<Color32> source,
        IReadOnlyList<Color32> runtime,
        int width,
        int height,
        int index,
        byte edgeThreshold)
    {
        int centerX = index % width;
        int centerY = index / width;
        int sourceMinR = 255, sourceMinG = 255, sourceMinB = 255;
        int sourceMaxR = 0, sourceMaxG = 0, sourceMaxB = 0;
        int runtimeMinR = 255, runtimeMinG = 255, runtimeMinB = 255;
        int runtimeMaxR = 0, runtimeMaxG = 0, runtimeMaxB = 0;
        for (int y = Math.Max(0, centerY - 1);
             y <= Math.Min(height - 1, centerY + 1);
             y++)
        {
            for (int x = Math.Max(0, centerX - 1);
                 x <= Math.Min(width - 1, centerX + 1);
                 x++)
            {
                Color32 sourcePixel = source[y * width + x];
                Color32 runtimePixel = runtime[y * width + x];
                sourceMinR = Math.Min(sourceMinR, sourcePixel.r);
                sourceMinG = Math.Min(sourceMinG, sourcePixel.g);
                sourceMinB = Math.Min(sourceMinB, sourcePixel.b);
                sourceMaxR = Math.Max(sourceMaxR, sourcePixel.r);
                sourceMaxG = Math.Max(sourceMaxG, sourcePixel.g);
                sourceMaxB = Math.Max(sourceMaxB, sourcePixel.b);
                runtimeMinR = Math.Min(runtimeMinR, runtimePixel.r);
                runtimeMinG = Math.Min(runtimeMinG, runtimePixel.g);
                runtimeMinB = Math.Min(runtimeMinB, runtimePixel.b);
                runtimeMaxR = Math.Max(runtimeMaxR, runtimePixel.r);
                runtimeMaxG = Math.Max(runtimeMaxG, runtimePixel.g);
                runtimeMaxB = Math.Max(runtimeMaxB, runtimePixel.b);
            }
        }
        return sourceMaxR - sourceMinR > edgeThreshold ||
               sourceMaxG - sourceMinG > edgeThreshold ||
               sourceMaxB - sourceMinB > edgeThreshold ||
               runtimeMaxR - runtimeMinR > edgeThreshold ||
               runtimeMaxG - runtimeMinG > edgeThreshold ||
               runtimeMaxB - runtimeMinB > edgeThreshold;
    }

    private static Vector3 DenseVector3(IReadOnlyList<float> values)
    {
        Assert.That(values.Count, Is.EqualTo(3));
        return new Vector3(values[0], values[1], values[2]);
    }

    private static DenseRuntimeCapture DenseCapture(
        EntityManager entityManager,
        IReadOnlyList<Entity> sectionEntities)
    {
        var legacyRows =
            new List<DenseLegacyIdentityRow>(DenseExpectedLegacyIdentityCount);
        var denseRows =
            new List<DenseGeneratedIdentityRow>(DenseExpectedGeneratedIdentityCount);
        var renderRows = new List<DenseRenderRow>(DenseExpectedRenderRowCount);
        var legacyIds = new HashSet<string>(StringComparer.Ordinal);
        var denseIds = new HashSet<string>(StringComparer.Ordinal);

        using EntityQuery legacyQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<OperationMapEntityPresentationIdentity>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<SceneTag>()
            }
        });
        using EntityQuery denseQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly<DenseCityPresentationIdentity>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<SceneTag>()
            }
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

        for (int sectionIndex = 0; sectionIndex < sectionEntities.Count; sectionIndex++)
        {
            var sceneTag = new SceneTag { SceneEntity = sectionEntities[sectionIndex] };

            legacyQuery.SetSharedComponentFilter(sceneTag);
            using (NativeArray<OperationMapEntityPresentationIdentity> identities =
                   legacyQuery.ToComponentDataArray<OperationMapEntityPresentationIdentity>(
                       Allocator.Temp))
            using (NativeArray<LocalToWorld> matrices =
                   legacyQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp))
            {
                Assert.That(matrices.Length, Is.EqualTo(identities.Length));
                for (int i = 0; i < identities.Length; i++)
                {
                    string sourceId = identities[i].SourceGlobalObjectId.ToString();
                    Assert.That(
                        identities[i].OperationMapId.ToString(),
                        Is.EqualTo(ExpectedOperationMapId));
                    Assert.That(legacyIds.Add(sourceId), Is.True,
                        $"Duplicate dense-runtime legacy identity: {sourceId}");
                    legacyRows.Add(new DenseLegacyIdentityRow(
                        sourceId,
                        identities[i].Role,
                        identities[i].PlacementIndex,
                        DenseMatrixToArray(matrices[i].Value)));
                }
            }

            denseQuery.SetSharedComponentFilter(sceneTag);
            using (NativeArray<DenseCityPresentationIdentity> identities =
                   denseQuery.ToComponentDataArray<DenseCityPresentationIdentity>(
                       Allocator.Temp))
            using (NativeArray<LocalToWorld> matrices =
                   denseQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp))
            {
                Assert.That(matrices.Length, Is.EqualTo(identities.Length));
                for (int i = 0; i < identities.Length; i++)
                {
                    string stableId = identities[i].StableId.ToString();
                    Assert.That(denseIds.Add(stableId), Is.True,
                        $"Duplicate dense-runtime generated identity: {stableId}");
                    denseRows.Add(new DenseGeneratedIdentityRow(
                        stableId,
                        identities[i].Role,
                        DenseMatrixToArray(matrices[i].Value)));
                }
            }

            renderQuery.SetSharedComponentFilter(sceneTag);
            using (NativeArray<Entity> entities =
                   renderQuery.ToEntityArray(Allocator.Temp))
            using (NativeArray<RenderBounds> bounds =
                   renderQuery.ToComponentDataArray<RenderBounds>(Allocator.Temp))
            using (NativeArray<LocalToWorld> matrices =
                   renderQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp))
            {
                Assert.That(entities.Length, Is.EqualTo(bounds.Length));
                Assert.That(matrices.Length, Is.EqualTo(bounds.Length));
                for (int i = 0; i < bounds.Length; i++)
                {
                    float[] matrix = DenseMatrixToArray(matrices[i].Value);
                    float3 center = bounds[i].Value.Center;
                    float3 extents = bounds[i].Value.Extents;
                    renderRows.Add(DenseRenderRow.Create(
                        matrix,
                        center,
                        extents,
                        DenseResolveRenderOwner(entityManager, entities[i])));
                }
            }
        }

        legacyQuery.ResetFilter();
        denseQuery.ResetFilter();
        renderQuery.ResetFilter();
        legacyRows.Sort(DenseLegacyIdentityRowComparer.Instance);
        denseRows.Sort(DenseGeneratedIdentityRowComparer.Instance);
        return new DenseRuntimeCapture(
            legacyRows.ToArray(),
            denseRows.ToArray(),
            renderRows.ToArray());
    }

    private static void DenseCompare(
        DenseParityManifest expected,
        DenseRuntimeCapture actual,
        int cycle)
    {
        Assert.That(actual.LegacyRows.Length, Is.EqualTo(expected.LegacyRows.Length),
            $"Dense legacy identity count differs in cycle {cycle}.");
        for (int i = 0; i < expected.LegacyRows.Length; i++)
        {
            DenseLegacyIdentityRow expectedRow = expected.LegacyRows[i];
            DenseLegacyIdentityRow actualRow = actual.LegacyRows[i];
            Assert.That(actualRow.Id, Is.EqualTo(expectedRow.Id),
                $"Dense legacy identity {i} differs in cycle {cycle}.");
            Assert.That(actualRow.Role, Is.EqualTo(expectedRow.Role),
                $"Dense legacy role for {expectedRow.Id} differs in cycle {cycle}.");
            Assert.That(actualRow.PlacementIndex, Is.EqualTo(expectedRow.PlacementIndex),
                $"Dense legacy placement for {expectedRow.Id} differs in cycle {cycle}.");
            DenseAssertValuesWithin(
                expectedRow.Matrix,
                actualRow.Matrix,
                expected.MatrixTolerance,
                $"dense legacy matrix {expectedRow.Id} cycle {cycle}");
        }

        Assert.That(actual.DenseRows.Length, Is.EqualTo(expected.DenseRows.Length),
            $"Dense generated identity count differs in cycle {cycle}.");
        for (int i = 0; i < expected.DenseRows.Length; i++)
        {
            DenseGeneratedIdentityRow expectedRow = expected.DenseRows[i];
            DenseGeneratedIdentityRow actualRow = actual.DenseRows[i];
            Assert.That(actualRow.Id, Is.EqualTo(expectedRow.Id),
                $"Dense generated identity {i} differs in cycle {cycle}.");
            Assert.That(actualRow.Role, Is.EqualTo(expectedRow.Role),
                $"Dense generated role for {expectedRow.Id} differs in cycle {cycle}.");
            DenseAssertValuesWithin(
                expectedRow.Matrix,
                actualRow.Matrix,
                expected.MatrixTolerance,
                $"dense generated matrix {expectedRow.Id} cycle {cycle}");
        }

        Assert.That(actual.RenderRows.Length, Is.EqualTo(expected.RenderRows.Length),
            $"Dense render-row count differs in cycle {cycle}.");
        DenseCompareRenderBuckets(expected, actual.RenderRows, cycle);
    }

    private static void DenseCompareRenderBuckets(
        DenseParityManifest expected,
        IReadOnlyList<DenseRenderRow> actual,
        int cycle)
    {
        float bucketWidth = expected.BoundsTolerance * 4f;
        var expectedRows = (DenseRenderRow[])expected.RenderRows.Clone();
        Array.Sort(expectedRows, DenseRenderRowComparer.Instance);
        Dictionary<DenseSpatialBucketKey, List<DenseRenderRow>> actualBuckets =
            DenseBuildRenderBuckets(actual, bucketWidth);
        int remainingRows = actual.Count;

        for (int expectedIndex = 0; expectedIndex < expectedRows.Length; expectedIndex++)
        {
            DenseRenderRow expectedRow = expectedRows[expectedIndex];
            DenseSpatialBucketKey origin = expectedRow.BuildBucketKey(bucketWidth);
            List<DenseRenderRow> bestBucket = null;
            int bestIndex = -1;
            float bestResidual = float.PositiveInfinity;
            for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            for (int z = -1; z <= 1; z++)
            {
                var key = new DenseSpatialBucketKey(
                    origin.X + x,
                    origin.Y + y,
                    origin.Z + z);
                if (!actualBuckets.TryGetValue(key, out List<DenseRenderRow> candidates))
                    continue;
                for (int candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++)
                {
                    DenseRenderRow candidate = candidates[candidateIndex];
                    float matrixResidual =
                        DenseMaximumResidual(expectedRow.Matrix, candidate.Matrix);
                    float localResidual =
                        DenseMaximumResidual(expectedRow.LocalBounds, candidate.LocalBounds);
                    float worldResidual =
                        DenseMaximumResidual(expectedRow.WorldBounds, candidate.WorldBounds);
                    if (matrixResidual > expected.MatrixTolerance ||
                        localResidual > expected.BoundsTolerance ||
                        worldResidual > expected.BoundsTolerance)
                        continue;
                    float residual = math.max(
                        matrixResidual / expected.MatrixTolerance,
                        math.max(
                            localResidual / expected.BoundsTolerance,
                            worldResidual / expected.BoundsTolerance));
                    if (residual >= bestResidual)
                        continue;
                    bestResidual = residual;
                    bestBucket = candidates;
                    bestIndex = candidateIndex;
                }
            }

            if (bestIndex < 0)
            {
                Assert.Fail(
                    $"No section-owned runtime render row matched dense manifest row " +
                    $"{expectedIndex} in cycle {cycle}. " +
                    DenseDescribeNearestRenderRow(expectedRow, actual));
            }
            DenseRenderRow actualRow = bestBucket[bestIndex];
            bestBucket.RemoveAt(bestIndex);
            remainingRows--;
            DenseAssertValuesWithin(
                expectedRow.Matrix,
                actualRow.Matrix,
                expected.MatrixTolerance,
                $"dense render matrix row {expectedIndex} cycle {cycle}");
            DenseAssertValuesWithin(
                expectedRow.LocalBounds,
                actualRow.LocalBounds,
                expected.BoundsTolerance,
                $"dense local bounds row {expectedIndex} cycle {cycle}");
            DenseAssertValuesWithin(
                expectedRow.WorldBounds,
                actualRow.WorldBounds,
                expected.BoundsTolerance,
                $"dense world bounds row {expectedIndex} cycle {cycle}");
        }
        Assert.That(remainingRows, Is.Zero,
            $"Unmatched section-owned dense render rows remained in cycle {cycle}.");
    }

    private static string DenseDescribeNearestRenderRow(
        DenseRenderRow expected,
        IReadOnlyList<DenseRenderRow> actual)
    {
        int bestIndex = -1;
        float bestResidual = float.PositiveInfinity;
        float bestMatrixResidual = float.PositiveInfinity;
        float bestLocalResidual = float.PositiveInfinity;
        float bestWorldResidual = float.PositiveInfinity;
        for (int i = 0; i < actual.Count; i++)
        {
            float matrixResidual = DenseMaximumResidual(expected.Matrix, actual[i].Matrix);
            float localResidual =
                DenseMaximumResidual(expected.LocalBounds, actual[i].LocalBounds);
            float worldResidual =
                DenseMaximumResidual(expected.WorldBounds, actual[i].WorldBounds);
            float residual = math.max(matrixResidual, math.max(localResidual, worldResidual));
            if (residual >= bestResidual)
                continue;
            bestIndex = i;
            bestResidual = residual;
            bestMatrixResidual = matrixResidual;
            bestLocalResidual = localResidual;
            bestWorldResidual = worldResidual;
        }

        return
            $"nearest={bestIndex},matrixResidual={bestMatrixResidual:R}," +
            $"localResidual={bestLocalResidual:R},worldResidual={bestWorldResidual:R}," +
            $"runtimeOwner={actual[bestIndex].Owner}," +
            DenseDescribeLargestDifference(
                "matrix",
                expected.Matrix,
                actual[bestIndex].Matrix) + "," +
            DenseDescribeLargestDifference(
                "worldBounds",
                expected.WorldBounds,
                actual[bestIndex].WorldBounds) + "," +
            $"expectedWorldMin=({expected.WorldBounds[0]:R}," +
            $"{expected.WorldBounds[1]:R},{expected.WorldBounds[2]:R})";
    }

    private static string DenseResolveRenderOwner(
        EntityManager entityManager,
        Entity entity)
    {
        Entity current = entity;
        for (int depth = 0; depth < 128 && current != Entity.Null; depth++)
        {
            if (entityManager.HasComponent<DenseCityPresentationIdentity>(current))
            {
                DenseCityPresentationIdentity identity =
                    entityManager.GetComponentData<DenseCityPresentationIdentity>(current);
                return $"dense:{identity.StableId}";
            }
            if (entityManager.HasComponent<OperationMapEntityPresentationIdentity>(current))
            {
                OperationMapEntityPresentationIdentity identity =
                    entityManager.GetComponentData<OperationMapEntityPresentationIdentity>(
                        current);
                return $"legacy:{identity.SourceGlobalObjectId}";
            }
            if (!entityManager.HasComponent<Parent>(current))
                break;
            current = entityManager.GetComponentData<Parent>(current).Value;
        }
        return $"unowned:{entity.Index}:{entity.Version}";
    }

    private static string DenseDescribeLargestDifference(
        string label,
        IReadOnlyList<float> expected,
        IReadOnlyList<float> actual)
    {
        int bestIndex = -1;
        float bestResidual = -1f;
        for (int i = 0; i < expected.Count; i++)
        {
            float residual = math.abs(expected[i] - actual[i]);
            if (residual <= bestResidual)
                continue;
            bestIndex = i;
            bestResidual = residual;
        }
        return
            $"{label}Delta[index={bestIndex},expected={expected[bestIndex]:R}," +
            $"actual={actual[bestIndex]:R},residual={bestResidual:R}]";
    }

    private static Dictionary<DenseSpatialBucketKey, List<DenseRenderRow>>
        DenseBuildRenderBuckets(
        IReadOnlyList<DenseRenderRow> rows,
        float bucketWidth)
    {
        var buckets =
            new Dictionary<DenseSpatialBucketKey, List<DenseRenderRow>>(rows.Count);
        for (int i = 0; i < rows.Count; i++)
        {
            DenseSpatialBucketKey key = rows[i].BuildBucketKey(bucketWidth);
            if (!buckets.TryGetValue(key, out List<DenseRenderRow> bucket))
            {
                bucket = new List<DenseRenderRow>(1);
                buckets.Add(key, bucket);
            }
            bucket.Add(rows[i]);
        }
        foreach (List<DenseRenderRow> bucket in buckets.Values)
            bucket.Sort(DenseRenderRowComparer.Instance);
        return buckets;
    }

    private static float DenseMaximumResidual(
        IReadOnlyList<float> expected,
        IReadOnlyList<float> actual)
    {
        float maximum = 0f;
        for (int i = 0; i < expected.Count; i++)
            maximum = math.max(maximum, math.abs(expected[i] - actual[i]));
        return maximum;
    }

    private static void DenseValidateFingerprints(
        string projectRoot,
        string manifestPath,
        string addressablesCatalogPath,
        string entityCatalogPath,
        DenseParitySummary summary,
        DenseRuntimeContentReport runtimeContent,
        DenseParityManifest manifest)
    {
        Assert.That(summary, Is.Not.Null);
        Assert.That(
            summary.schema,
            Is.EqualTo("warline.operation-map.dense-city-runtime-parity-manifest"));
        Assert.That(summary.schemaVersion, Is.EqualTo(1));
        Assert.That(summary.result, Is.EqualTo("DenseCityRuntimeParityManifestWritten"));
        Assert.That(summary.productionCutover, Is.Zero);
        Assert.That(summary.formatVersion, Is.EqualTo(DenseParityFormatVersion));
        Assert.That(summary.operationMapId, Is.EqualTo(manifest.OperationMapId));
        Assert.That(summary.entitySceneGuid, Is.EqualTo(manifest.EntitySceneGuid));
        Assert.That(summary.candidateSubSceneSha256, Is.EqualTo(manifest.SubSceneSha256));
        Assert.That(
            summary.directBakeParityReportSha256,
            Is.EqualTo(manifest.DirectBakeReportSha256));
        Assert.That(summary.matrixTolerance, Is.EqualTo(manifest.MatrixTolerance));
        Assert.That(summary.boundsTolerance, Is.EqualTo(manifest.BoundsTolerance));
        Assert.That(summary.legacyIdentityCount, Is.EqualTo(manifest.LegacyRows.Length));
        Assert.That(summary.denseIdentityCount, Is.EqualTo(manifest.DenseRows.Length));
        Assert.That(summary.renderRowCount, Is.EqualTo(manifest.RenderRows.Length));
        Assert.That(summary.manifestBytes, Is.EqualTo(new FileInfo(manifestPath).Length));
        Assert.That(summary.manifestSha256, Is.EqualTo(ComputeSha256(manifestPath)));

        Assert.That(runtimeContent, Is.Not.Null);
        Assert.That(
            runtimeContent.schema,
            Is.EqualTo("warline.operation-map.dense-city-candidate-runtime-content"));
        Assert.That(runtimeContent.schemaVersion, Is.EqualTo(1));
        Assert.That(
            runtimeContent.result,
            Is.EqualTo("DenseCityCandidateRuntimeContentBuilt"));
        Assert.That(runtimeContent.productionCutover, Is.Zero);
        Assert.That(runtimeContent.productionSettingsMutated, Is.Zero);
        Assert.That(runtimeContent.sharedOutputRestored, Is.EqualTo(1));
        Assert.That(runtimeContent.staticRuntimeEntryCount, Is.Zero);
        Assert.That(runtimeContent.sharedDependencyCount, Is.Zero);
        Assert.That(runtimeContent.definitionAddress, Is.EqualTo(DenseDefinitionAddress));
        Assert.That(runtimeContent.operationMapId, Is.EqualTo(manifest.OperationMapId));
        Assert.That(runtimeContent.entitySceneGuid, Is.EqualTo(manifest.EntitySceneGuid));

        string subSceneSha = ComputeSha256(
            DenseResolve(projectRoot, DenseCandidateSubScenePath));
        string directReportSha = ComputeSha256(
            DenseResolve(projectRoot, DenseDirectBakeParityReportPath));
        Assert.That(manifest.SubSceneSha256, Is.EqualTo(subSceneSha));
        Assert.That(manifest.DirectBakeReportSha256, Is.EqualTo(directReportSha));
        Assert.That(runtimeContent.candidateSubSceneSha256, Is.EqualTo(subSceneSha));
        Assert.That(runtimeContent.directBakeParityReportSha256, Is.EqualTo(directReportSha));
        Assert.That(
            runtimeContent.candidateDefinitionSha256,
            Is.EqualTo(ComputeSha256(
                DenseResolve(projectRoot, DenseCandidateDefinitionPath))));
        Assert.That(
            runtimeContent.candidateRuntimeBindingSha256,
            Is.EqualTo(ComputeSha256(
                DenseResolve(projectRoot, DenseCandidateRuntimeBindingPath))));
        Assert.That(
            runtimeContent.addressablesCatalogSha256,
            Is.EqualTo(ComputeSha256(addressablesCatalogPath)));
        Assert.That(
            runtimeContent.entityContentCatalogSha256,
            Is.EqualTo(ComputeSha256(entityCatalogPath)));
        Assert.That(
            Path.GetFullPath(runtimeContent.addressablesCatalogPath),
            Is.EqualTo(Path.GetFullPath(addressablesCatalogPath)));
        Assert.That(
            Path.GetFullPath(runtimeContent.entityContentCatalogPath),
            Is.EqualTo(Path.GetFullPath(entityCatalogPath)));
    }

    private static void DenseAssertValuesWithin(
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
            $"{label} residual {maximumResidual:R} exceeds {tolerance:R}.");
    }

    private static float[] DenseMatrixToArray(float4x4 matrix)
    {
        var values = new float[16];
        values[0] = matrix.c0.x;
        values[1] = matrix.c0.y;
        values[2] = matrix.c0.z;
        values[3] = matrix.c0.w;
        values[4] = matrix.c1.x;
        values[5] = matrix.c1.y;
        values[6] = matrix.c1.z;
        values[7] = matrix.c1.w;
        values[8] = matrix.c2.x;
        values[9] = matrix.c2.y;
        values[10] = matrix.c2.z;
        values[11] = matrix.c2.w;
        values[12] = matrix.c3.x;
        values[13] = matrix.c3.y;
        values[14] = matrix.c3.z;
        values[15] = matrix.c3.w;
        return values;
    }

    private static float[] DenseTransformBounds(
        float3 center,
        float3 extents,
        IReadOnlyList<float> matrix)
    {
        Vector3 minimum = new(
            float.PositiveInfinity,
            float.PositiveInfinity,
            float.PositiveInfinity);
        Vector3 maximum = new(
            float.NegativeInfinity,
            float.NegativeInfinity,
            float.NegativeInfinity);
        var world = new Matrix4x4();
        for (int i = 0; i < 16; i++)
            world[i] = matrix[i];
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 corner = (Vector3)center +
                             Vector3.Scale((Vector3)extents, new Vector3(x, y, z));
            Vector3 point = world.MultiplyPoint3x4(corner);
            minimum = Vector3.Min(minimum, point);
            maximum = Vector3.Max(maximum, point);
        }
        return new[]
        {
            minimum.x, minimum.y, minimum.z,
            maximum.x, maximum.y, maximum.z
        };
    }

    private static string DenseResolve(string projectRoot, string relativePath) =>
        Path.GetFullPath(Path.Combine(projectRoot, relativePath));

    private static void DenseRequireFile(string path) =>
        Assert.That(File.Exists(path), Is.True, $"Dense parity input is missing: {path}");

    private sealed class DenseParityManifest
    {
        private DenseParityManifest(
            string operationMapId,
            string entitySceneGuid,
            string subSceneSha256,
            string directBakeReportSha256,
            float matrixTolerance,
            float boundsTolerance,
            DenseLegacyIdentityRow[] legacyRows,
            DenseGeneratedIdentityRow[] denseRows,
            DenseRenderRow[] renderRows)
        {
            OperationMapId = operationMapId;
            EntitySceneGuid = entitySceneGuid;
            SubSceneSha256 = subSceneSha256;
            DirectBakeReportSha256 = directBakeReportSha256;
            MatrixTolerance = matrixTolerance;
            BoundsTolerance = boundsTolerance;
            LegacyRows = legacyRows;
            DenseRows = denseRows;
            RenderRows = renderRows;
        }

        public string OperationMapId { get; }
        public string EntitySceneGuid { get; }
        public string SubSceneSha256 { get; }
        public string DirectBakeReportSha256 { get; }
        public float MatrixTolerance { get; }
        public float BoundsTolerance { get; }
        public DenseLegacyIdentityRow[] LegacyRows { get; }
        public DenseGeneratedIdentityRow[] DenseRows { get; }
        public DenseRenderRow[] RenderRows { get; }

        public static DenseParityManifest Read(string path)
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                1024 * 1024,
                FileOptions.SequentialScan);
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            Assert.That(reader.ReadUInt32(), Is.EqualTo(DenseParityMagic));
            Assert.That(reader.ReadInt32(), Is.EqualTo(DenseParityFormatVersion));
            string operationMapId = DenseReadString(reader);
            string entitySceneGuid = DenseReadString(reader);
            string subSceneSha256 = DenseReadString(reader);
            string directBakeReportSha256 = DenseReadString(reader);
            float matrixTolerance = reader.ReadSingle();
            float boundsTolerance = reader.ReadSingle();
            int legacyCount = DenseReadCount(reader, "legacy identities");
            int denseCount = DenseReadCount(reader, "dense identities");
            int renderCount = DenseReadCount(reader, "render rows");
            Assert.That(
                legacyCount,
                Is.EqualTo(DenseExpectedLegacyIdentityCount));
            Assert.That(
                denseCount,
                Is.EqualTo(DenseExpectedGeneratedIdentityCount));
            Assert.That(
                renderCount,
                Is.EqualTo(DenseExpectedRenderRowCount));
            Assert.That(matrixTolerance, Is.GreaterThan(0f));
            Assert.That(boundsTolerance, Is.GreaterThan(0f));

            var legacyRows = new DenseLegacyIdentityRow[legacyCount];
            for (int i = 0; i < legacyRows.Length; i++)
            {
                legacyRows[i] = new DenseLegacyIdentityRow(
                    DenseReadString(reader),
                    reader.ReadByte(),
                    reader.ReadInt32(),
                    DenseReadFloats(reader, 16));
            }

            var denseRows = new DenseGeneratedIdentityRow[denseCount];
            for (int i = 0; i < denseRows.Length; i++)
            {
                denseRows[i] = new DenseGeneratedIdentityRow(
                    DenseReadString(reader),
                    reader.ReadByte(),
                    DenseReadFloats(reader, 16));
            }

            var renderRows = new DenseRenderRow[renderCount];
            for (int i = 0; i < renderRows.Length; i++)
            {
                renderRows[i] = new DenseRenderRow(
                    DenseReadFloats(reader, 16),
                    DenseReadFloats(reader, 6),
                    DenseReadFloats(reader, 6));
            }
            Assert.That(stream.Position, Is.EqualTo(stream.Length),
                "Dense parity manifest has trailing data.");
            Assert.That(
                operationMapId,
                Is.EqualTo(ExpectedOperationMapId));
            return new DenseParityManifest(
                operationMapId,
                entitySceneGuid,
                subSceneSha256,
                directBakeReportSha256,
                matrixTolerance,
                boundsTolerance,
                legacyRows,
                denseRows,
                renderRows);
        }

        private static int DenseReadCount(BinaryReader reader, string label)
        {
            int value = reader.ReadInt32();
            Assert.That(value, Is.GreaterThan(0), $"Dense manifest {label} count is invalid.");
            return value;
        }

        private static string DenseReadString(BinaryReader reader)
        {
            int byteCount = reader.ReadInt32();
            Assert.That(byteCount, Is.InRange(0, 4096),
                "Dense manifest string length is invalid.");
            byte[] bytes = reader.ReadBytes(byteCount);
            Assert.That(bytes.Length, Is.EqualTo(byteCount),
                "Dense manifest string is truncated.");
            return Encoding.UTF8.GetString(bytes);
        }

        private static float[] DenseReadFloats(BinaryReader reader, int count)
        {
            var values = new float[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = reader.ReadSingle();
                Assert.That(float.IsFinite(values[i]), Is.True,
                    "Dense manifest contains a non-finite value.");
            }
            return values;
        }
    }

    private sealed class DenseRuntimeCapture
    {
        public DenseRuntimeCapture(
            DenseLegacyIdentityRow[] legacyRows,
            DenseGeneratedIdentityRow[] denseRows,
            DenseRenderRow[] renderRows)
        {
            LegacyRows = legacyRows;
            DenseRows = denseRows;
            RenderRows = renderRows;
        }

        public DenseLegacyIdentityRow[] LegacyRows { get; }
        public DenseGeneratedIdentityRow[] DenseRows { get; }
        public DenseRenderRow[] RenderRows { get; }
    }

    private readonly struct DenseLegacyIdentityRow
    {
        public DenseLegacyIdentityRow(
            string id,
            byte role,
            int placementIndex,
            float[] matrix)
        {
            Id = id;
            Role = role;
            PlacementIndex = placementIndex;
            Matrix = matrix;
        }

        public string Id { get; }
        public byte Role { get; }
        public int PlacementIndex { get; }
        public float[] Matrix { get; }
    }

    private sealed class DenseLegacyIdentityRowComparer :
        IComparer<DenseLegacyIdentityRow>
    {
        public static readonly DenseLegacyIdentityRowComparer Instance = new();

        public int Compare(DenseLegacyIdentityRow left, DenseLegacyIdentityRow right)
        {
            int identity = string.CompareOrdinal(left.Id, right.Id);
            if (identity != 0)
                return identity;
            int role = left.Role.CompareTo(right.Role);
            return role != 0 ? role : left.PlacementIndex.CompareTo(right.PlacementIndex);
        }
    }

    private readonly struct DenseGeneratedIdentityRow
    {
        public DenseGeneratedIdentityRow(string id, byte role, float[] matrix)
        {
            Id = id;
            Role = role;
            Matrix = matrix;
        }

        public string Id { get; }
        public byte Role { get; }
        public float[] Matrix { get; }
    }

    private sealed class DenseGeneratedIdentityRowComparer :
        IComparer<DenseGeneratedIdentityRow>
    {
        public static readonly DenseGeneratedIdentityRowComparer Instance = new();

        public int Compare(DenseGeneratedIdentityRow left, DenseGeneratedIdentityRow right)
        {
            int identity = string.CompareOrdinal(left.Id, right.Id);
            return identity != 0 ? identity : left.Role.CompareTo(right.Role);
        }
    }

    private readonly struct DenseRenderRow
    {
        public DenseRenderRow(
            float[] matrix,
            float[] localBounds,
            float[] worldBounds,
            string owner = "manifest:unattributed")
        {
            Matrix = matrix;
            LocalBounds = localBounds;
            WorldBounds = worldBounds;
            Owner = owner;
        }

        public float[] Matrix { get; }
        public float[] LocalBounds { get; }
        public float[] WorldBounds { get; }
        public string Owner { get; }

        public static DenseRenderRow Create(
            float[] matrix,
            float3 center,
            float3 extents,
            string owner)
        {
            float[] localBounds =
            {
                center.x, center.y, center.z,
                extents.x, extents.y, extents.z
            };
            return new DenseRenderRow(
                matrix,
                localBounds,
                DenseTransformBounds(center, extents, matrix),
                owner);
        }

        public DenseSpatialBucketKey BuildBucketKey(float bucketWidth)
        {
            return new DenseSpatialBucketKey(
                (int)Math.Floor(WorldBounds[0] / bucketWidth),
                (int)Math.Floor(WorldBounds[1] / bucketWidth),
                (int)Math.Floor(WorldBounds[2] / bucketWidth));
        }
    }

    private readonly struct DenseSpatialBucketKey : IEquatable<DenseSpatialBucketKey>
    {
        public DenseSpatialBucketKey(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public bool Equals(DenseSpatialBucketKey other) =>
            X == other.X && Y == other.Y && Z == other.Z;

        public override bool Equals(object value) =>
            value is DenseSpatialBucketKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X;
                hash = (hash * 397) ^ Y;
                return (hash * 397) ^ Z;
            }
        }
    }

    private sealed class DenseRenderRowComparer : IComparer<DenseRenderRow>
    {
        public static readonly DenseRenderRowComparer Instance = new();

        public int Compare(DenseRenderRow left, DenseRenderRow right)
        {
            int value = DenseCompareValues(left.Matrix, right.Matrix);
            if (value != 0)
                return value;
            value = DenseCompareValues(left.LocalBounds, right.LocalBounds);
            return value != 0
                ? value
                : DenseCompareValues(left.WorldBounds, right.WorldBounds);
        }

        private static int DenseCompareValues(
            IReadOnlyList<float> left,
            IReadOnlyList<float> right)
        {
            for (int i = 0; i < left.Count; i++)
            {
                int value = left[i].CompareTo(right[i]);
                if (value != 0)
                    return value;
            }
            return left.Count.CompareTo(right.Count);
        }
    }

    private readonly struct DensePixelComparison
    {
        public DensePixelComparison(
            float meanChannelDelta,
            float maximumChannelDelta,
            float rawChangedPixelRatio,
            float interiorChangedPixelRatio,
            float edgePixelRatio,
            float sourceLumaVariance,
            float runtimeLumaVariance)
        {
            MeanChannelDelta = meanChannelDelta;
            MaximumChannelDelta = maximumChannelDelta;
            RawChangedPixelRatio = rawChangedPixelRatio;
            InteriorChangedPixelRatio = interiorChangedPixelRatio;
            EdgePixelRatio = edgePixelRatio;
            SourceLumaVariance = sourceLumaVariance;
            RuntimeLumaVariance = runtimeLumaVariance;
        }

        public float MeanChannelDelta { get; }
        public float MaximumChannelDelta { get; }
        public float RawChangedPixelRatio { get; }
        public float InteriorChangedPixelRatio { get; }
        public float EdgePixelRatio { get; }
        public float SourceLumaVariance { get; }
        public float RuntimeLumaVariance { get; }
    }

    [Serializable]
    private sealed class DenseEditorFixedCameraReport
    {
        public string schema;
        public int schemaVersion;
        public string operationMapId;
        public string result;
        public string candidateSubSceneSha256;
        public int width;
        public int height;
        public int rendererCount;
        public int legacyIdentityCount;
        public int denseIdentityCount;
        public int expectedRuntimeRenderRowCount;
        public int viewCount;
        public float maximumMeanChannelDelta;
        public float maximumChangedPixelRatio;
        public int productionCutover;
        public DenseEditorFixedCameraRow[] rows;
    }

    [Serializable]
    private sealed class DenseEditorFixedCameraRow
    {
        public string view;
        public string editorPath;
        public string editorSha256;
        public float editorLumaVariance;
        public float[] cameraPosition;
        public float[] cameraRotation;
        public int orthographic;
        public float fieldOfView;
        public float orthographicSize;
    }

    [Serializable]
    private sealed class DenseRuntimeFixedCameraReport
    {
        public string schema;
        public int schemaVersion;
        public string operationMapId;
        public string result;
        public string candidateSubSceneSha256;
        public string manifestSha256;
        public int width;
        public int height;
        public int editorRendererCount;
        public int runtimeRenderRowCount;
        public int cycleCount;
        public int viewCount;
        public int rejectedViewCount;
        public float maximumMeanChannelDelta;
        public float maximumChangedPixelRatio;
        public int productionCutover;
        public DenseRuntimeFixedCameraRow[] rows;
    }

    [Serializable]
    private sealed class DenseRuntimeFixedCameraRow
    {
        public string view;
        public string result;
        public string editorPath;
        public string runtimePath;
        public string editorSha256;
        public string runtimeSha256;
        public float meanChannelDelta;
        public float maximumChannelDelta;
        public float changedPixelRatio;
        public float interiorChangedPixelRatio;
        public float edgePixelRatio;
        public float editorLumaVariance;
        public float runtimeLumaVariance;
    }

    [Serializable]
    private sealed class DenseParitySummary
    {
        public string schema;
        public int schemaVersion;
        public string result;
        public string operationMapId;
        public string entitySceneGuid;
        public string candidateSubSceneSha256;
        public string directBakeParityReportSha256;
        public int formatVersion;
        public float matrixTolerance;
        public float boundsTolerance;
        public int legacyIdentityCount;
        public int denseIdentityCount;
        public int renderRowCount;
        public long manifestBytes;
        public string manifestSha256;
        public int productionCutover;
    }

    [Serializable]
    private sealed class DenseRuntimeContentReport
    {
        public string schema;
        public int schemaVersion;
        public string result;
        public string operationMapId;
        public string entitySceneGuid;
        public string definitionAddress;
        public int sharedDependencyCount;
        public int staticRuntimeEntryCount;
        public string addressablesCatalogPath;
        public string entityContentCatalogPath;
        public string candidateSubSceneSha256;
        public string candidateDefinitionSha256;
        public string candidateRuntimeBindingSha256;
        public string directBakeParityReportSha256;
        public string addressablesCatalogSha256;
        public string entityContentCatalogSha256;
        public int productionCutover;
        public int productionSettingsMutated;
        public int sharedOutputRestored;
    }
}
