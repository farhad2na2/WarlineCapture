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
    private const int DenseExpectedGeneratedIdentityCount = 35796;
    private const int DenseExpectedRenderRowCount = 78325;
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
        }
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
