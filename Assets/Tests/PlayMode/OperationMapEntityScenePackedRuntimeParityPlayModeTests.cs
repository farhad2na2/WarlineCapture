using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using Hash128 = Unity.Entities.Hash128;

public sealed class OperationMapEntityScenePackedRuntimeParityPlayModeTests
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

        Assert.That(
            world.EntityManager.Exists(sceneEntity) &&
            SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity),
            Is.False,
            $"Candidate EntityScene remained loaded after cycle {cycle}.");
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
