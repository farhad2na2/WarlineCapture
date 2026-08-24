#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Game.Authoring;
    using Game.Components;
    using Game.Configs;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Rendering;
    using Unity.Transforms;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Hash128 = Unity.Entities.Hash128;

    /// <summary>
    /// In-memory bake of the protected candidate SubScene authoring for Phase 0A validation.
    /// Does not mutate accepted source, production Addressables, or presentation mode.
    /// </summary>
    internal static class OperationMapEntityPresentationCandidateBakeValidator
    {
        internal const int ExpectedGameplayBuildings = 432;
        internal const int ExpectedGameplayVehicles = 22;
        internal const int ExpectedPresentationRoots = 3;
        internal const int ExpectedRenderOnlyOwners = 9090;
        internal const int ExpectedPresentationIdentities = 9544;
        internal const int ExpectedDenseGameplayBuildings = 4977;
        internal const int ExpectedDenseGeneratedGameplayBuildings = 4545;
        internal const int ExpectedDenseGeneratedRenderOnlyOwners = 31879;
        internal const int ExpectedDenseGeneratedIdentities = 36424;
        internal const int ExpectedDenseVirtualizedGameplayBuildings = 4530;
        internal const int ExpectedDenseResidentGameplayBuildings =
            ExpectedDenseGameplayBuildings - ExpectedDenseVirtualizedGameplayBuildings;

        private const string DenseCandidateBakeReportPath =
            "Design/AgentReports/2026-07-24_dense_city_generated_candidate_bake_validation.json";
        private const string MapWideConfigPath =
            "Assets/Game/Configs/OperationMaps/Skirmish/SkirmishDesertBase_MapWideCity_Config.asset";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/EntityScene Migration/Bake And Validate Candidate Entity Presentation")]
        public static void BakeAndValidateCandidateEntityPresentation()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string candidatePhysicalPath = ResolveProjectPath(projectRoot, candidatePath);
            if (!File.Exists(candidatePhysicalPath))
                throw new FileNotFoundException("Protected candidate SubScene has not been created.", candidatePhysicalPath);

            string acceptedSceneHash = ComputeSha256(
                ResolveProjectPath(projectRoot,
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath));
            string acceptedSubSceneHash = ComputeSha256(
                ResolveProjectPath(projectRoot,
                    OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath));

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            World bakeWorld = null;
            object blobStore = null;
            try
            {
                Scene sourceScene = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                    OpenSceneMode.Additive);
                Scene candidateScene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                RequireAuthoringCounts(candidateScene);

                bakeWorld = new World("OperationMapEntityPresentationCandidateBake");
                blobStore = CreateBlobAssetStore();
                if (!TryBakeScene(bakeWorld, candidateScene, candidatePath, blobStore, out string bakeError))
                    throw new InvalidOperationException($"Candidate bake failed: {bakeError}");

                OperationMapEntityPresentationCandidateBakeReport report = ValidateBakedWorld(bakeWorld.EntityManager);
                WriteReport(projectRoot, report);

                if (!report.Passed)
                    throw new InvalidOperationException($"Candidate bake validation failed: {report.RejectionReason}");

                OperationMapEntityPresentationTransformParityValidator.TransformParityReport parity =
                    OperationMapEntityPresentationTransformParityValidator.ValidateAndWrite(
                        projectRoot,
                        sourceScene,
                        candidateScene,
                        bakeWorld.EntityManager);

                RequireHashUnchanged(
                    acceptedSceneHash,
                    ResolveProjectPath(projectRoot,
                        OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath));
                RequireHashUnchanged(
                    acceptedSubSceneHash,
                    ResolveProjectPath(projectRoot,
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath));

                Debug.Log(
                    $"[OperationMapEntityPresentationCandidateBakeValidator] status=Validated " +
                    $"gameplayBuildings={report.GameplayBuildingCount} " +
                    $"gameplayVehicles={report.GameplayVehicleCount} " +
                    $"presentationRoots={report.PresentationRootCount} " +
                    $"presentationIdentities={report.PresentationIdentityCount} " +
                    $"renderMeshEntities={report.RenderMeshEntityCount} " +
                    $"buildingRenderChildren={report.BuildingRenderChildCount} " +
                    $"nonFiniteTransforms={report.NonFiniteTransformCount} " +
                    $"managedMapVisualCompanions={report.ManagedMapVisualCompanionCount} " +
                    $"transformParityRows={parity.candidateIdentityCount} " +
                    $"productionCutover=1 report={report.ReportPath}");
            }
            finally
            {
                if (bakeWorld != null)
                    bakeWorld.Dispose();
                DisposeBlobAssetStore(blobStore);
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        [MenuItem("Game/Operation Maps/EntityScene Migration/Bake And Validate Dense City Candidate")]
        public static void BakeAndValidateDenseCityCandidate()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string candidatePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string candidatePhysicalPath = ResolveProjectPath(projectRoot, candidatePath);
            if (!File.Exists(candidatePhysicalPath))
                throw new FileNotFoundException("Dense-city candidate entity scene has not been created.", candidatePhysicalPath);

            string[] protectedPaths =
            {
                OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                MapWideConfigPath
            };
            var protectedHashes = protectedPaths.ToDictionary(
                path => path,
                path => ComputeSha256(ResolveProjectPath(projectRoot, path)),
                StringComparer.Ordinal);

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            World bakeWorld = null;
            object blobStore = null;
            try
            {
                Scene candidateScene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                DenseAuthoringCounts authoring = RequireDenseAuthoringCounts(candidateScene);

                bakeWorld = new World("DenseCityEntityPresentationCandidateBake");
                blobStore = CreateBlobAssetStore();
                if (!TryBakeScene(bakeWorld, candidateScene, candidatePath, blobStore, out string bakeError))
                    throw new InvalidOperationException($"Dense-city candidate bake failed: {bakeError}");

                DenseCandidateBakeReport report =
                    ValidateDenseBakedWorld(bakeWorld.EntityManager, authoring);
                WriteDenseReport(projectRoot, report);
                if (!report.Passed)
                {
                    throw new InvalidOperationException(
                        $"Dense-city candidate bake validation failed: {report.rejectionReason}");
                }

                OperationMapDenseCityGeneratedTransformParityValidator.DenseCityGeneratedTransformParityReport parity =
                    OperationMapDenseCityGeneratedTransformParityValidator.ValidateAndWrite(
                        projectRoot,
                        candidateScene,
                        bakeWorld.EntityManager);
                OperationMapDenseCityRuntimeParityManifestWriter.DenseRuntimeParityManifestSummary
                    runtimeParityManifest =
                        OperationMapDenseCityRuntimeParityManifestWriter.Write(
                            projectRoot,
                            bakeWorld.EntityManager);

                foreach (KeyValuePair<string, string> protectedHash in protectedHashes)
                {
                    RequireHashUnchanged(
                        protectedHash.Value,
                        ResolveProjectPath(projectRoot, protectedHash.Key));
                }

                Debug.Log(
                    $"[OperationMapEntityPresentationCandidateBakeValidator] status=DenseCandidateValidated " +
                    $"gameplayBuildings={report.gameplayBuildingCount} " +
                    $"generatedIdentities={report.denseIdentityCount} " +
                    $"generatedBuildingIdentities={report.denseGameplayBuildingIdentityCount} " +
                    $"generatedRenderOnlyIdentities={report.denseRenderOnlyIdentityCount} " +
                    $"residentBuildingPresentations={report.buildingPresentationCount} " +
                    $"virtualizedBuildingPresentations={report.virtualizedBuildingPresentationCount} " +
                    $"renderMeshEntities={report.renderMeshEntityCount} " +
                    $"transformParityRows={parity.candidateIdentityCount} " +
                    $"runtimeParityManifestBytes={runtimeParityManifest.manifestBytes} " +
                    $"productionCutover=1 report={report.reportPath}");
            }
            finally
            {
                if (bakeWorld != null)
                    bakeWorld.Dispose();
                DisposeBlobAssetStore(blobStore);
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        public static void BakeAndValidateProductionDenseCityMaterialTransformParity()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string candidatePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string candidatePhysicalPath = ResolveProjectPath(projectRoot, candidatePath);
            if (!File.Exists(candidatePhysicalPath))
            {
                throw new FileNotFoundException(
                    "The production dense-city EntityScene is missing.",
                    candidatePhysicalPath);
            }

            RequireProductionDenseCityCandidateBinding();
            string[] protectedPaths =
            {
                OperationMapAddressablesLayoutBuilder.DefinitionPath,
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                    .DenseCandidateDefinitionPath,
                DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
                candidatePath,
                MapWideConfigPath
            };
            var protectedHashes = protectedPaths.ToDictionary(
                path => path,
                path => ComputeSha256(ResolveProjectPath(projectRoot, path)),
                StringComparer.Ordinal);

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            World bakeWorld = null;
            object blobStore = null;
            try
            {
                Scene candidateScene = EditorSceneManager.OpenScene(
                    candidatePath,
                    OpenSceneMode.Additive);
                bakeWorld = new World("DenseCityProductionMaterialTransformParityBake");
                blobStore = CreateBlobAssetStore();
                if (!TryBakeScene(
                        bakeWorld,
                        candidateScene,
                        candidatePath,
                        blobStore,
                        out string bakeError))
                {
                    throw new InvalidOperationException(
                        $"Production dense-city parity bake failed: {bakeError}");
                }

                OperationMapDenseCityGeneratedTransformParityValidator
                    .DenseCityGeneratedTransformParityReport parity =
                    OperationMapDenseCityGeneratedTransformParityValidator.ValidateAndWrite(
                        projectRoot,
                        candidateScene,
                        bakeWorld.EntityManager);

                foreach (KeyValuePair<string, string> protectedHash in protectedHashes)
                {
                    RequireHashUnchanged(
                        protectedHash.Value,
                        ResolveProjectPath(projectRoot, protectedHash.Key));
                }
                RequireProductionDenseCityCandidateBinding();

                Debug.Log(
                    "[OperationMapProductionDenseCityMaterialTransformParity] result=Passed " +
                    $"candidateIdentities={parity.candidateIdentityCount} " +
                    $"bakedIdentities={parity.bakedIdentityCount} " +
                    $"candidateRenderers={parity.generatedCandidateRendererEntityCount} " +
                    $"bakedRenderers={parity.generatedBakedRenderEntityCount} " +
                    $"meshMismatches={parity.generatedMeshMismatchCount} " +
                    $"materialMismatches={parity.generatedMaterialMismatchCount} " +
                    $"baseColorMismatches={parity.generatedBaseColorMismatchCount} " +
                    $"rejectedRows={parity.rejectedRowCount} productionCutover=1");
            }
            finally
            {
                if (bakeWorld != null)
                    bakeWorld.Dispose();
                DisposeBlobAssetStore(blobStore);
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        private static void RequireProductionDenseCityCandidateBinding()
        {
            OperationMapDefinition production =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    OperationMapAddressablesLayoutBuilder.DefinitionPath);
            OperationMapDefinition candidate =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateDefinitionPath);
            string entitySceneGuid = AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            string sourceSceneGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.SourceScenePath);

            if (production == null || candidate == null ||
                production.PresentationKind != OperationMapPresentationKind.EntityScene ||
                production.RenderResidencyMode !=
                    OperationMapRenderResidencyMode.VirtualizedProxyPool ||
                !string.Equals(
                    production.NavigationMetadata.AuthoredSubSceneGuid,
                    entitySceneGuid,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    candidate.NavigationMetadata.AuthoredSubSceneGuid,
                    entitySceneGuid,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    production.SourceSceneReference.AssetGUID,
                    sourceSceneGuid,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Production is not bound to the accepted dense-city EntityScene proxy-pool " +
                    "candidate and runtime source scene.");
            }
        }

        public static void BakeAndValidateDenseCityCandidateVehicleOwnership()
        {
            string candidatePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            World bakeWorld = null;
            object blobStore = null;
            try
            {
                Scene candidateScene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                bakeWorld = new World("DenseCityCandidateVehicleOwnershipBake");
                blobStore = CreateBlobAssetStore();
                if (!TryBakeScene(bakeWorld, candidateScene, candidatePath, blobStore, out string bakeError))
                    throw new InvalidOperationException($"Dense candidate vehicle ownership bake failed: {bakeError}");

                if (!TryValidateVehicleOwnership(
                        bakeWorld.EntityManager,
                        LoadExpectedVehicleFactions(),
                        out string rejectionReason))
                {
                    throw new InvalidOperationException(
                        $"Dense candidate vehicle ownership rejected: {rejectionReason}");
                }

                Debug.Log(
                    "[DenseCityCandidateVehicleOwnershipBake] result=Passed vehicles=22 " +
                    "placementFactionParity=22/22");
            }
            finally
            {
                if (bakeWorld != null)
                    bakeWorld.Dispose();
                DisposeBlobAssetStore(blobStore);
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] previousSetup)
        {
            if (previousSetup != null && previousSetup.Any(entry => entry.isLoaded && entry.isActive))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void RequireAuthoringCounts(Scene candidateScene)
        {
            int buildings = 0;
            int vehicles = 0;
            int roots = 0;
            int identities = 0;
            int renderOnlyOwners = 0;
            GameObject[] sceneRoots = candidateScene.GetRootGameObjects();
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                buildings += sceneRoots[i].GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length;
                vehicles += sceneRoots[i].GetComponentsInChildren<UnitGridAuthoring>(true).Length;
                roots += sceneRoots[i].GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true).Length;
                identities += sceneRoots[i]
                    .GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true).Length;
            }

            Transform renderOnly = RequirePath(candidateScene, "AuthoredOperationMapEntityPresentation/RenderOnly");
            renderOnlyOwners = renderOnly
                .GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true)
                .Count(identity => identity.Role == OperationMapEntityPresentationRole.RenderOnly);

            if (buildings != ExpectedGameplayBuildings)
                throw new InvalidOperationException($"Expected {ExpectedGameplayBuildings} building authorings, found {buildings}.");
            if (vehicles != ExpectedGameplayVehicles)
                throw new InvalidOperationException($"Expected {ExpectedGameplayVehicles} vehicle authorings, found {vehicles}.");
            if (roots != ExpectedPresentationRoots)
                throw new InvalidOperationException($"Expected {ExpectedPresentationRoots} presentation roots, found {roots}.");
            if (renderOnlyOwners != ExpectedRenderOnlyOwners)
                throw new InvalidOperationException($"Expected {ExpectedRenderOnlyOwners} render-only owners, found {renderOnlyOwners}.");
            if (identities != ExpectedPresentationIdentities)
                throw new InvalidOperationException($"Expected {ExpectedPresentationIdentities} presentation identities, found {identities}.");
        }

        private static DenseAuthoringCounts RequireDenseAuthoringCounts(Scene candidateScene)
        {
            var counts = new DenseAuthoringCounts();
            GameObject[] sceneRoots = candidateScene.GetRootGameObjects();
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                GameObject root = sceneRoots[i];
                counts.GameplayBuildings +=
                    root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length;
                counts.GameplayVehicles +=
                    root.GetComponentsInChildren<UnitGridAuthoring>(true).Length;
                counts.PresentationRoots +=
                    root.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true).Length;
                counts.LegacyPresentationIdentities += root
                    .GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true).Length;
                DenseCityPresentationIdentityAuthoring[] denseIdentities =
                    root.GetComponentsInChildren<DenseCityPresentationIdentityAuthoring>(true);
                counts.DenseIdentities += denseIdentities.Length;
                for (int identityIndex = 0; identityIndex < denseIdentities.Length; identityIndex++)
                {
                    DenseCityPresentationIdentityAuthoring identity = denseIdentities[identityIndex];
                    if (!identity.TryValidate(out string error))
                    {
                        throw new InvalidOperationException(
                            $"Invalid dense-city presentation identity '{identity.name}': {error}");
                    }

                    switch (identity.Role)
                    {
                        case OperationMapEntityPresentationRole.GameplayBuildings:
                            counts.DenseGameplayBuildingIdentities++;
                            break;
                        case OperationMapEntityPresentationRole.RenderOnly:
                            counts.DenseRenderOnlyIdentities++;
                            break;
                        default:
                            counts.DenseUnknownRoleIdentities++;
                            break;
                    }
                }
            }

            RequireCount(
                nameof(counts.GameplayBuildings),
                counts.GameplayBuildings,
                ExpectedDenseGameplayBuildings);
            RequireCount(nameof(counts.GameplayVehicles), counts.GameplayVehicles, ExpectedGameplayVehicles);
            RequireCount(nameof(counts.PresentationRoots), counts.PresentationRoots, ExpectedPresentationRoots);
            RequireCount(
                nameof(counts.LegacyPresentationIdentities),
                counts.LegacyPresentationIdentities,
                ExpectedPresentationIdentities);
            RequireCount(
                nameof(counts.DenseIdentities),
                counts.DenseIdentities,
                ExpectedDenseGeneratedIdentities);
            RequireCount(
                nameof(counts.DenseGameplayBuildingIdentities),
                counts.DenseGameplayBuildingIdentities,
                ExpectedDenseGeneratedGameplayBuildings);
            RequireCount(
                nameof(counts.DenseRenderOnlyIdentities),
                counts.DenseRenderOnlyIdentities,
                ExpectedDenseGeneratedRenderOnlyOwners);
            RequireCount(nameof(counts.DenseUnknownRoleIdentities), counts.DenseUnknownRoleIdentities, 0);
            return counts;
        }

        private static void RequireCount(string label, int actual, int expected)
        {
            if (actual != expected)
                throw new InvalidOperationException($"Expected {expected} {label}, found {actual}.");
        }

        private static bool TryBakeScene(
            World world,
            Scene scene,
            string scenePath,
            object blobStore,
            out string rejectionReason)
        {
            rejectionReason = null;
            try
            {
                Type bakingUtilityType = Type.GetType("Unity.Entities.BakingUtility, Unity.Entities.Hybrid", true);
                Type bakingSettingsType = Type.GetType("Unity.Entities.BakingSettings, Unity.Entities.Hybrid", true);
                object settings = Activator.CreateInstance(bakingSettingsType);
                string guid = AssetDatabase.AssetPathToGUID(scenePath);
                if (string.IsNullOrEmpty(guid))
                {
                    rejectionReason = "candidate-guid-missing";
                    return false;
                }

                bakingSettingsType.GetField("SceneGUID")?.SetValue(settings, new Hash128(guid));
                object assignName = Enum.Parse(bakingUtilityType.GetNestedType("BakingFlags"), "AssignName");
                object addGuid = Enum.Parse(bakingUtilityType.GetNestedType("BakingFlags"), "AddEntityGUID");
                object flags = Enum.ToObject(
                    bakingUtilityType.GetNestedType("BakingFlags"),
                    Convert.ToUInt32(assignName) | Convert.ToUInt32(addGuid));
                bakingSettingsType.GetProperty("BakingFlags")?.SetValue(settings, flags);
                bakingSettingsType.GetProperty("BlobAssetStore")?.SetValue(settings, blobStore);

                MethodInfo bakeScene = bakingUtilityType.GetMethod(
                    "BakeScene",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (bakeScene == null)
                {
                    rejectionReason = "BakeScene-method-missing";
                    return false;
                }

                object result = bakeScene.Invoke(null, new object[] { world, scene, settings, false, null });
                if (result is bool ok && !ok)
                {
                    rejectionReason = "BakeScene-returned-false";
                    return false;
                }

                return true;
            }
            catch (TargetInvocationException exception)
            {
                rejectionReason = exception.InnerException?.Message ?? exception.Message;
                return false;
            }
            catch (Exception exception)
            {
                rejectionReason = exception.Message;
                return false;
            }
        }

        private static OperationMapEntityPresentationCandidateBakeReport ValidateBakedWorld(EntityManager entityManager)
        {
            var report = new OperationMapEntityPresentationCandidateBakeReport
            {
                result = "CandidateBakeValidationFailed",
                gameplayBuildingCount = entityManager.CreateEntityQuery(typeof(OperationMapBuildingIdentity)).CalculateEntityCount(),
                gameplayVehicleCount = entityManager.CreateEntityQuery(
                    typeof(UnitGrid),
                    typeof(UnitMove),
                    typeof(UnitVehicleMovement)).CalculateEntityCount(),
                presentationRootCount = entityManager.CreateEntityQuery(typeof(OperationMapEntityPresentationRoot)).CalculateEntityCount(),
                presentationIdentityCount = entityManager.CreateEntityQuery(typeof(OperationMapEntityPresentationIdentity)).CalculateEntityCount(),
                buildingPresentationCount = entityManager.CreateEntityQuery(typeof(OperationMapBuildingPresentation)).CalculateEntityCount(),
                totalEntityCount = entityManager.UniversalQuery.CalculateEntityCount(),
                entityChunkCount = entityManager.UniversalQuery.CalculateChunkCount(),
                nonFiniteTransformCount = CountNonFiniteTransforms(entityManager),
                managedMapVisualCompanionCount = CountManagedMapVisualCompanions(entityManager),
                authoringBuildingCount = ExpectedGameplayBuildings,
                authoringRenderOnlyOwnerCount = ExpectedRenderOnlyOwners,
                entitySceneBytes = -1
            };
            CaptureIdentityRoleCounts(entityManager, report);
            CaptureEntityLayoutCounts(entityManager, report);
            CaptureRenderAssetCounts(entityManager, report);
            CaptureBuildingVisualOwnership(entityManager, report);

            if (!TryValidateVehicleOwnership(
                    entityManager,
                    LoadExpectedVehicleFactions(),
                    out string vehicleOwnershipRejection))
            {
                report.rejectionReason = vehicleOwnershipRejection;
                return report;
            }

            if (report.gameplayBuildingCount != ExpectedGameplayBuildings)
            {
                report.rejectionReason = $"gameplay-building-count:{report.gameplayBuildingCount}";
                return report;
            }

            if (report.gameplayVehicleCount != ExpectedGameplayVehicles)
            {
                report.rejectionReason = $"gameplay-vehicle-count:{report.gameplayVehicleCount}";
                return report;
            }

            if (report.presentationRootCount != ExpectedPresentationRoots)
            {
                report.rejectionReason = $"presentation-root-count:{report.presentationRootCount}";
                return report;
            }

            if (report.presentationIdentityCount != ExpectedPresentationIdentities)
            {
                report.rejectionReason = $"presentation-identity-count:{report.presentationIdentityCount}";
                return report;
            }

            if (report.gameplayBuildingIdentityCount != ExpectedGameplayBuildings ||
                report.gameplayVehicleIdentityCount != ExpectedGameplayVehicles ||
                report.renderOnlyIdentityCount != ExpectedRenderOnlyOwners ||
                report.unknownRoleIdentityCount != 0)
            {
                report.rejectionReason =
                    $"presentation-identity-role-counts:{report.gameplayBuildingIdentityCount}:" +
                    $"{report.gameplayVehicleIdentityCount}:{report.renderOnlyIdentityCount}:" +
                    $"{report.unknownRoleIdentityCount}";
                return report;
            }

            if (report.buildingPresentationCount != ExpectedGameplayBuildings)
            {
                report.rejectionReason = $"building-presentation-count:{report.buildingPresentationCount}";
                return report;
            }

            if (report.buildingRenderChildCount < ExpectedGameplayBuildings)
            {
                report.rejectionReason = $"building-render-child-count:{report.buildingRenderChildCount}";
                return report;
            }

            if (report.intactVisualRootCount != ExpectedGameplayBuildings ||
                report.missingIntactVisualRootCount != 0 ||
                report.sharedIntactDestroyedVisualRootCount != 0)
            {
                report.rejectionReason =
                    $"building-visual-ownership:{report.intactVisualRootCount}:" +
                    $"{report.destroyedVisualRootCount}:{report.missingIntactVisualRootCount}:" +
                    $"{report.missingDestroyedVisualRootCount}:{report.sharedIntactDestroyedVisualRootCount}";
                return report;
            }

            if (report.nonFiniteTransformCount != 0)
            {
                report.rejectionReason = $"non-finite-transforms:{report.nonFiniteTransformCount}";
                return report;
            }

            if (report.managedMapVisualCompanionCount != 0)
            {
                report.rejectionReason = $"managed-map-visual-companions:{report.managedMapVisualCompanionCount}";
                return report;
            }

            // Render-only owners contribute at least one renderable entity each in aggregate with
            // building visual children; require a lower bound rather than exact equality.
            int minimumRenderMeshes = ExpectedRenderOnlyOwners;
            if (report.renderMeshEntityCount < minimumRenderMeshes)
            {
                report.rejectionReason = $"render-mesh-entities-below-owner-count:{report.renderMeshEntityCount}";
                return report;
            }

            report.result = "CandidateBakeValidationPassed";
            report.rejectionReason = null;
            return report;
        }

        private static DenseCandidateBakeReport ValidateDenseBakedWorld(
            EntityManager entityManager,
            DenseAuthoringCounts authoring)
        {
            using EntityQuery renderQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MaterialMeshInfo>(),
                ComponentType.ReadOnly<RenderMeshArray>());
            var report = new DenseCandidateBakeReport
            {
                schema = "warline.dense-city.generated-candidate-bake-validation",
                schemaVersion = 2,
                operationMapId = OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                checkpoint = "accepted-editor-to-dense-candidate-subscene-to-in-memory-baked-ecs",
                result = "DenseCandidateBakeValidationFailed",
                gameplayBuildingCount =
                    entityManager.CreateEntityQuery(typeof(OperationMapBuildingIdentity)).CalculateEntityCount(),
                gameplayVehicleCount = entityManager.CreateEntityQuery(
                    typeof(UnitGrid),
                    typeof(UnitMove),
                    typeof(UnitVehicleMovement)).CalculateEntityCount(),
                presentationRootCount =
                    entityManager.CreateEntityQuery(typeof(OperationMapEntityPresentationRoot)).CalculateEntityCount(),
                legacyPresentationIdentityCount =
                    entityManager.CreateEntityQuery(typeof(OperationMapEntityPresentationIdentity)).CalculateEntityCount(),
                denseIdentityCount =
                    entityManager.CreateEntityQuery(typeof(DenseCityPresentationIdentity)).CalculateEntityCount(),
                buildingPresentationCount =
                    entityManager.CreateEntityQuery(typeof(OperationMapBuildingPresentation)).CalculateEntityCount(),
                virtualizedBuildingPresentationCount =
                    entityManager.CreateEntityQuery(
                        typeof(OperationMapVirtualizedBuildingPresentationComponent))
                        .CalculateEntityCount(),
                renderMeshEntityCount = renderQuery.CalculateEntityCount(),
                totalEntityCount = entityManager.UniversalQuery.CalculateEntityCount(),
                entityChunkCount = entityManager.UniversalQuery.CalculateChunkCount(),
                nonFiniteTransformCount = CountNonFiniteTransforms(entityManager),
                managedMapVisualCompanionCount = CountManagedMapVisualCompanions(entityManager),
                authoringGameplayBuildingCount = authoring.GameplayBuildings,
                authoringDenseIdentityCount = authoring.DenseIdentities,
                authoringDenseGameplayBuildingIdentityCount =
                    authoring.DenseGameplayBuildingIdentities,
                authoringDenseRenderOnlyIdentityCount = authoring.DenseRenderOnlyIdentities,
                productionCutover = 1
            };

            using NativeArray<DenseCityPresentationIdentity> identities =
                entityManager.CreateEntityQuery(typeof(DenseCityPresentationIdentity))
                    .ToComponentDataArray<DenseCityPresentationIdentity>(Allocator.Temp);
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < identities.Length; i++)
            {
                string stableId = identities[i].StableId.ToString();
                if (!stableIds.Add(stableId))
                    report.duplicateDenseIdentityCount++;

                switch ((OperationMapEntityPresentationRole)identities[i].Role)
                {
                    case OperationMapEntityPresentationRole.GameplayBuildings:
                        report.denseGameplayBuildingIdentityCount++;
                        break;
                    case OperationMapEntityPresentationRole.RenderOnly:
                        report.denseRenderOnlyIdentityCount++;
                        break;
                    default:
                        report.denseUnknownRoleIdentityCount++;
                        break;
                }
            }

            CaptureDenseEntityLayoutCounts(entityManager, report);
            CaptureDenseRenderAssetCounts(entityManager, report);
            CaptureDenseBuildingVisualOwnership(entityManager, report);

            bool vehicleOwnershipValid = TryValidateVehicleOwnership(
                entityManager,
                LoadExpectedVehicleFactions(),
                out string vehicleOwnershipRejection);

            if (!vehicleOwnershipValid)
                report.rejectionReason = vehicleOwnershipRejection;
            else if (report.gameplayBuildingCount != ExpectedDenseGameplayBuildings)
                report.rejectionReason = $"gameplay-building-count:{report.gameplayBuildingCount}";
            else if (report.gameplayVehicleCount != ExpectedGameplayVehicles)
                report.rejectionReason = $"gameplay-vehicle-count:{report.gameplayVehicleCount}";
            else if (report.presentationRootCount != ExpectedPresentationRoots)
                report.rejectionReason = $"presentation-root-count:{report.presentationRootCount}";
            else if (report.legacyPresentationIdentityCount != ExpectedPresentationIdentities)
                report.rejectionReason =
                    $"legacy-presentation-identity-count:{report.legacyPresentationIdentityCount}";
            else if (report.denseIdentityCount != ExpectedDenseGeneratedIdentities)
                report.rejectionReason = $"dense-presentation-identity-count:{report.denseIdentityCount}";
            else if (report.denseGameplayBuildingIdentityCount !=
                     ExpectedDenseGeneratedGameplayBuildings)
                report.rejectionReason =
                    $"dense-gameplay-building-identity-count:{report.denseGameplayBuildingIdentityCount}";
            else if (report.denseRenderOnlyIdentityCount != ExpectedDenseGeneratedRenderOnlyOwners)
                report.rejectionReason =
                    $"dense-render-only-identity-count:{report.denseRenderOnlyIdentityCount}";
            else if (report.denseUnknownRoleIdentityCount != 0)
                report.rejectionReason =
                    $"dense-unknown-role-identity-count:{report.denseUnknownRoleIdentityCount}";
            else if (report.duplicateDenseIdentityCount != 0)
                report.rejectionReason =
                    $"duplicate-dense-presentation-identity-count:{report.duplicateDenseIdentityCount}";
            else if (report.buildingPresentationCount != ExpectedDenseResidentGameplayBuildings ||
                     report.virtualizedBuildingPresentationCount !=
                     ExpectedDenseVirtualizedGameplayBuildings ||
                     report.buildingPresentationCount +
                     report.virtualizedBuildingPresentationCount != ExpectedDenseGameplayBuildings)
                report.rejectionReason =
                    $"building-presentation-count:{report.buildingPresentationCount}:" +
                    $"{report.virtualizedBuildingPresentationCount}";
            else if (report.renderMeshEntityCount <
                     ExpectedRenderOnlyOwners + ExpectedDenseGeneratedRenderOnlyOwners)
                report.rejectionReason =
                    $"render-mesh-entities-below-owner-count:{report.renderMeshEntityCount}";
            else if (report.entityArchetypeCount <= 0 || report.entityChunkCount <= 0)
                report.rejectionReason =
                    $"entity-layout-counts:{report.entityArchetypeCount}:{report.entityChunkCount}";
            else if (report.renderChildEntityCount <= 0 ||
                     report.sharedRenderMeshArrayIdentityCount <= 0 ||
                     report.sharedMeshAssetIdentityCount <= 0 ||
                     report.sharedMaterialAssetIdentityCount <= 0)
                report.rejectionReason =
                    $"render-asset-counts:{report.renderChildEntityCount}:" +
                    $"{report.sharedRenderMeshArrayIdentityCount}:" +
                    $"{report.sharedMeshAssetIdentityCount}:" +
                    $"{report.sharedMaterialAssetIdentityCount}";
            else if (report.intactVisualRootCount != ExpectedDenseResidentGameplayBuildings ||
                     report.missingIntactVisualRootCount != 0 ||
                     report.sharedIntactDestroyedVisualRootCount != 0 ||
                     report.destroyedVisualRootCount + report.missingDestroyedVisualRootCount !=
                     report.buildingPresentationCount)
                report.rejectionReason =
                    $"building-visual-ownership:{report.intactVisualRootCount}:" +
                    $"{report.destroyedVisualRootCount}:{report.missingIntactVisualRootCount}:" +
                    $"{report.missingDestroyedVisualRootCount}:" +
                    $"{report.sharedIntactDestroyedVisualRootCount}";
            else if (report.nonFiniteTransformCount != 0)
                report.rejectionReason =
                    $"non-finite-transforms:{report.nonFiniteTransformCount}";
            else if (report.managedMapVisualCompanionCount != 0)
                report.rejectionReason =
                    $"managed-map-visual-companions:{report.managedMapVisualCompanionCount}";
            else
            {
                report.result = "DenseCandidateBakeValidationPassed";
                report.rejectionReason = string.Empty;
            }

            report.Passed = string.Equals(
                report.result,
                "DenseCandidateBakeValidationPassed",
                StringComparison.Ordinal);
            return report;
        }

        internal static bool TryValidateVehicleOwnership(
            EntityManager entityManager,
            IReadOnlyList<byte> expectedFactions,
            out string rejectionReason)
        {
            if (expectedFactions == null || expectedFactions.Count != ExpectedGameplayVehicles)
            {
                rejectionReason = $"vehicle-faction-contract-count:{expectedFactions?.Count ?? -1}";
                return false;
            }

            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<OperationMapAuthoredVehiclePresentation>(),
                ComponentType.ReadOnly<Faction>());
            using NativeArray<OperationMapAuthoredVehiclePresentation> presentations =
                query.ToComponentDataArray<OperationMapAuthoredVehiclePresentation>(Allocator.Temp);
            using NativeArray<Faction> factions =
                query.ToComponentDataArray<Faction>(Allocator.Temp);
            if (presentations.Length != ExpectedGameplayVehicles || factions.Length != presentations.Length)
            {
                rejectionReason = $"vehicle-ownership-count:{presentations.Length}:{factions.Length}";
                return false;
            }

            var seen = new bool[ExpectedGameplayVehicles];
            for (int i = 0; i < presentations.Length; i++)
            {
                OperationMapAuthoredVehiclePresentation presentation = presentations[i];
                int placementIndex = presentation.PlacementIndex;
                if (placementIndex < 0 || placementIndex >= ExpectedGameplayVehicles || seen[placementIndex])
                {
                    rejectionReason = $"vehicle-ownership-placement:{placementIndex}";
                    return false;
                }

                byte expectedFaction = expectedFactions[placementIndex];
                if (presentation.FactionId != expectedFaction || factions[i].Id != expectedFaction)
                {
                    rejectionReason =
                        $"vehicle-ownership-faction:{placementIndex}:{presentation.FactionId}:" +
                        $"{factions[i].Id}:{expectedFaction}";
                    return false;
                }

                seen[placementIndex] = true;
            }

            rejectionReason = null;
            return true;
        }

        private static byte[] LoadExpectedVehicleFactions()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            OperationMapVehicleEcsConversionInventoryProbe.ConversionReport inventory =
                OperationMapVehicleCandidateMigrationEditor.LoadInventory(projectRoot);
            MapVehiclePlacementConfig placements =
                AssetDatabase.LoadAssetAtPath<MapVehiclePlacementConfig>(inventory.vehiclePlacementConfigPath);
            if (placements == null || placements.Placements.Count != ExpectedGameplayVehicles)
            {
                throw new InvalidOperationException(
                    "Authoritative vehicle placement factions are unavailable for baked ownership validation.");
            }

            var factions = new byte[ExpectedGameplayVehicles];
            for (int i = 0; i < factions.Length; i++)
                factions[i] = placements.Placements[i].FactionId;
            return factions;
        }

        private static void CaptureDenseEntityLayoutCounts(
            EntityManager entityManager,
            DenseCandidateBakeReport report)
        {
            using NativeArray<ArchetypeChunk> chunks =
                entityManager.UniversalQuery.ToArchetypeChunkArray(Allocator.Temp);
            var archetypes = new HashSet<EntityArchetype>();
            for (int i = 0; i < chunks.Length; i++)
                archetypes.Add(chunks[i].Archetype);
            report.entityArchetypeCount = archetypes.Count;
        }

        private static void CaptureDenseRenderAssetCounts(
            EntityManager entityManager,
            DenseCandidateBakeReport report)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MaterialMeshInfo>(),
                ComponentType.ReadOnly<RenderMeshArray>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var arrays = new HashSet<string>(StringComparer.Ordinal);
            var meshes = new HashSet<Mesh>();
            var materials = new HashSet<Material>();
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (entityManager.HasComponent<Parent>(entity))
                    report.renderChildEntityCount++;

                RenderMeshArray renderMeshArray =
                    entityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
                arrays.Add(renderMeshArray.GetHash128().ToString());
                if (renderMeshArray.MeshReferences != null)
                {
                    for (int meshIndex = 0;
                         meshIndex < renderMeshArray.MeshReferences.Length;
                         meshIndex++)
                    {
                        Mesh mesh = renderMeshArray.MeshReferences[meshIndex].Value;
                        if (mesh != null)
                            meshes.Add(mesh);
                    }
                }
                if (renderMeshArray.MaterialReferences != null)
                {
                    for (int materialIndex = 0;
                         materialIndex < renderMeshArray.MaterialReferences.Length;
                         materialIndex++)
                    {
                        Material material =
                            renderMeshArray.MaterialReferences[materialIndex].Value;
                        if (material != null)
                            materials.Add(material);
                    }
                }
            }

            report.sharedRenderMeshArrayIdentityCount = arrays.Count;
            report.sharedMeshAssetIdentityCount = meshes.Count;
            report.sharedMaterialAssetIdentityCount = materials.Count;
        }

        private static void CaptureDenseBuildingVisualOwnership(
            EntityManager entityManager,
            DenseCandidateBakeReport report)
        {
            var intactRoots = new HashSet<Entity>();
            var destroyedRoots = new HashSet<Entity>();
            using NativeArray<OperationMapBuildingPresentation> presentations =
                entityManager.CreateEntityQuery(typeof(OperationMapBuildingPresentation))
                    .ToComponentDataArray<OperationMapBuildingPresentation>(Allocator.Temp);
            for (int i = 0; i < presentations.Length; i++)
            {
                Entity intact = presentations[i].IntactVisualRoot;
                Entity destroyed = presentations[i].DestroyedVisualRoot;
                if (intact != Entity.Null && entityManager.Exists(intact))
                    intactRoots.Add(intact);
                else
                    report.missingIntactVisualRootCount++;
                if (destroyed != Entity.Null && entityManager.Exists(destroyed))
                    destroyedRoots.Add(destroyed);
                else
                    report.missingDestroyedVisualRootCount++;
            }

            report.intactVisualRootCount = intactRoots.Count;
            report.destroyedVisualRootCount = destroyedRoots.Count;
            foreach (Entity root in intactRoots)
            {
                if (destroyedRoots.Contains(root))
                    report.sharedIntactDestroyedVisualRootCount++;
            }
            report.buildingRenderChildCount =
                report.intactVisualRootCount + report.destroyedVisualRootCount;
        }

        private static void CaptureIdentityRoleCounts(
            EntityManager entityManager,
            OperationMapEntityPresentationCandidateBakeReport report)
        {
            using NativeArray<OperationMapEntityPresentationIdentity> identities =
                entityManager.CreateEntityQuery(typeof(OperationMapEntityPresentationIdentity))
                    .ToComponentDataArray<OperationMapEntityPresentationIdentity>(Allocator.Temp);
            for (int i = 0; i < identities.Length; i++)
            {
                switch ((OperationMapEntityPresentationRole)identities[i].Role)
                {
                    case OperationMapEntityPresentationRole.GameplayBuildings:
                        report.gameplayBuildingIdentityCount++;
                        break;
                    case OperationMapEntityPresentationRole.GameplayVehicles:
                        report.gameplayVehicleIdentityCount++;
                        break;
                    case OperationMapEntityPresentationRole.RenderOnly:
                        report.renderOnlyIdentityCount++;
                        break;
                    default:
                        report.unknownRoleIdentityCount++;
                        break;
                }
            }
        }

        private static void CaptureEntityLayoutCounts(
            EntityManager entityManager,
            OperationMapEntityPresentationCandidateBakeReport report)
        {
            using NativeArray<ArchetypeChunk> chunks =
                entityManager.UniversalQuery.ToArchetypeChunkArray(Allocator.Temp);
            var archetypes = new HashSet<EntityArchetype>();
            for (int i = 0; i < chunks.Length; i++)
                archetypes.Add(chunks[i].Archetype);
            report.entityArchetypeCount = archetypes.Count;
        }

        private static void CaptureRenderAssetCounts(
            EntityManager entityManager,
            OperationMapEntityPresentationCandidateBakeReport report)
        {
            using EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<MaterialMeshInfo>(),
                ComponentType.ReadOnly<RenderMeshArray>());
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            var arrays = new HashSet<string>(StringComparer.Ordinal);
            var meshes = new HashSet<Mesh>();
            var materials = new HashSet<Material>();
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (entityManager.HasComponent<Parent>(entity))
                    report.renderChildEntityCount++;

                RenderMeshArray renderMeshArray = entityManager.GetSharedComponentManaged<RenderMeshArray>(entity);
                arrays.Add(renderMeshArray.GetHash128().ToString());
                if (renderMeshArray.MeshReferences != null)
                {
                    for (int meshIndex = 0; meshIndex < renderMeshArray.MeshReferences.Length; meshIndex++)
                    {
                        Mesh mesh = renderMeshArray.MeshReferences[meshIndex].Value;
                        if (mesh != null)
                            meshes.Add(mesh);
                    }
                }
                if (renderMeshArray.MaterialReferences != null)
                {
                    for (int materialIndex = 0; materialIndex < renderMeshArray.MaterialReferences.Length; materialIndex++)
                    {
                        Material material = renderMeshArray.MaterialReferences[materialIndex].Value;
                        if (material != null)
                            materials.Add(material);
                    }
                }
            }

            report.renderMeshEntityCount = entities.Length;
            report.sharedRenderMeshArrayIdentityCount = arrays.Count;
            report.sharedMeshAssetIdentityCount = meshes.Count;
            report.sharedMaterialAssetIdentityCount = materials.Count;
        }

        private static void CaptureBuildingVisualOwnership(
            EntityManager entityManager,
            OperationMapEntityPresentationCandidateBakeReport report)
        {
            var intactRoots = new HashSet<Entity>();
            var destroyedRoots = new HashSet<Entity>();
            using NativeArray<OperationMapBuildingPresentation> presentations =
                entityManager.CreateEntityQuery(typeof(OperationMapBuildingPresentation))
                    .ToComponentDataArray<OperationMapBuildingPresentation>(Allocator.Temp);
            for (int i = 0; i < presentations.Length; i++)
            {
                Entity intact = presentations[i].IntactVisualRoot;
                Entity destroyed = presentations[i].DestroyedVisualRoot;
                if (intact != Entity.Null && entityManager.Exists(intact))
                    intactRoots.Add(intact);
                else
                    report.missingIntactVisualRootCount++;
                if (destroyed != Entity.Null && entityManager.Exists(destroyed))
                    destroyedRoots.Add(destroyed);
                else
                    report.missingDestroyedVisualRootCount++;
            }

            report.intactVisualRootCount = intactRoots.Count;
            report.destroyedVisualRootCount = destroyedRoots.Count;
            foreach (Entity root in intactRoots)
            {
                if (destroyedRoots.Contains(root))
                    report.sharedIntactDestroyedVisualRootCount++;
            }
            report.buildingRenderChildCount = report.intactVisualRootCount + report.destroyedVisualRootCount;
        }

        private static int CountNonFiniteTransforms(EntityManager entityManager)
        {
            int count = 0;
            using NativeArray<LocalTransform> transforms =
                entityManager.CreateEntityQuery(typeof(LocalTransform))
                    .ToComponentDataArray<LocalTransform>(Allocator.Temp);
            for (int i = 0; i < transforms.Length; i++)
            {
                float3 p = transforms[i].Position;
                float s = transforms[i].Scale;
                if (!math.isfinite(p.x) || !math.isfinite(p.y) || !math.isfinite(p.z) || !math.isfinite(s))
                    count++;
            }

            return count;
        }

        private static int CountManagedMapVisualCompanions(EntityManager entityManager)
        {
            // Building gameplay entities must not retain CompanionLink-managed visuals.
            Type companion = Type.GetType("Unity.Entities.CompanionLink, Unity.Entities.Hybrid") ??
                             Type.GetType("Unity.Entities.Hybrid.CompanionLink, Unity.Entities.Hybrid");
            if (companion == null)
                return 0;

            ComponentType companionType = ComponentType.ReadOnly(companion);
            int count = 0;
            using NativeArray<Entity> buildings =
                entityManager.CreateEntityQuery(typeof(OperationMapBuildingIdentity))
                    .ToEntityArray(Allocator.Temp);
            for (int i = 0; i < buildings.Length; i++)
            {
                if (entityManager.HasComponent(buildings[i], companionType))
                    count++;
            }

            return count;
        }

        private static void WriteReport(string projectRoot, OperationMapEntityPresentationCandidateBakeReport report)
        {
            string reportPath = Path.Combine(
                projectRoot,
                "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_validation.json");
            report.reportPath = reportPath.Replace('\\', '/');
            report.GameplayBuildingCount = report.gameplayBuildingCount;
            report.GameplayVehicleCount = report.gameplayVehicleCount;
            report.PresentationRootCount = report.presentationRootCount;
            report.PresentationIdentityCount = report.presentationIdentityCount;
            report.BuildingPresentationCount = report.buildingPresentationCount;
            report.TotalEntityCount = report.totalEntityCount;
            report.EntityArchetypeCount = report.entityArchetypeCount;
            report.EntityChunkCount = report.entityChunkCount;
            report.RenderMeshEntityCount = report.renderMeshEntityCount;
            report.BuildingRenderChildCount = report.buildingRenderChildCount;
            report.NonFiniteTransformCount = report.nonFiniteTransformCount;
            report.ManagedMapVisualCompanionCount = report.managedMapVisualCompanionCount;
            report.Passed = string.Equals(report.result, "CandidateBakeValidationPassed", StringComparison.Ordinal);
            report.RejectionReason = report.rejectionReason;
            report.ReportPath = report.reportPath;

            string json = JsonUtility.ToJson(report, true);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? projectRoot);
            File.WriteAllText(reportPath, json, Utf8WithoutBom);
        }

        private static void WriteDenseReport(string projectRoot, DenseCandidateBakeReport report)
        {
            string reportPath = Path.Combine(projectRoot, DenseCandidateBakeReportPath);
            report.reportPath = DenseCandidateBakeReportPath;
            string json = JsonUtility.ToJson(report, true);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? projectRoot);
            File.WriteAllText(reportPath, json + "\n", Utf8WithoutBom);
            AssetDatabase.ImportAsset(
                DenseCandidateBakeReportPath,
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static object CreateBlobAssetStore()
        {
            Type storeType =
                Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities") ??
                Type.GetType("Unity.Entities.BlobAssetStore, Unity.Entities.Hybrid");
            if (storeType == null)
                throw new InvalidOperationException("BlobAssetStore type was not found.");
            ConstructorInfo ctor = storeType.GetConstructor(new[] { typeof(int) });
            return ctor != null ? ctor.Invoke(new object[] { 128 }) : Activator.CreateInstance(storeType);
        }

        private static void DisposeBlobAssetStore(object store)
        {
            if (store is IDisposable disposable)
                disposable.Dispose();
        }

        private static Transform RequirePath(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(owner => owner.name == segments[0]);
            Transform current = root != null ? root.transform : null;
            for (int i = 1; i < segments.Length && current != null; i++)
                current = current.Find(segments[i]);
            return current ?? throw new InvalidOperationException($"Candidate hierarchy path is missing: {path}");
        }

        private static string ResolveProjectPath(string projectRoot, string repositoryPath) =>
            Path.GetFullPath(Path.Combine(projectRoot, repositoryPath));

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static void RequireHashUnchanged(string expected, string path)
        {
            if (!string.Equals(expected, ComputeSha256(path), StringComparison.Ordinal))
                throw new InvalidOperationException($"Protected accepted source changed: {path}");
        }

        [Serializable]
        private sealed class OperationMapEntityPresentationCandidateBakeReport
        {
            public string result;
            public string rejectionReason;
            public string reportPath;
            public int gameplayBuildingCount;
            public int gameplayVehicleCount;
            public int presentationRootCount;
            public int presentationIdentityCount;
            public int buildingPresentationCount;
            public int gameplayBuildingIdentityCount;
            public int gameplayVehicleIdentityCount;
            public int renderOnlyIdentityCount;
            public int unknownRoleIdentityCount;
            public int totalEntityCount;
            public int entityArchetypeCount;
            public int entityChunkCount;
            public int renderMeshEntityCount;
            public int renderChildEntityCount;
            public int sharedRenderMeshArrayIdentityCount;
            public int sharedMeshAssetIdentityCount;
            public int sharedMaterialAssetIdentityCount;
            public int buildingRenderChildCount;
            public int intactVisualRootCount;
            public int destroyedVisualRootCount;
            public int missingIntactVisualRootCount;
            public int missingDestroyedVisualRootCount;
            public int sharedIntactDestroyedVisualRootCount;
            public int nonFiniteTransformCount;
            public int managedMapVisualCompanionCount;
            public int authoringBuildingCount;
            public int authoringRenderOnlyOwnerCount;
            public long entitySceneBytes;

            // Convenience properties used by caller logging (not serialized by JsonUtility field rules).
            [NonSerialized] public bool Passed;
            [NonSerialized] public string RejectionReason;
            [NonSerialized] public string ReportPath;
            [NonSerialized] public int GameplayBuildingCount;
            [NonSerialized] public int GameplayVehicleCount;
            [NonSerialized] public int PresentationRootCount;
            [NonSerialized] public int PresentationIdentityCount;
            [NonSerialized] public int BuildingPresentationCount;
            [NonSerialized] public int TotalEntityCount;
            [NonSerialized] public int EntityArchetypeCount;
            [NonSerialized] public int EntityChunkCount;
            [NonSerialized] public int RenderMeshEntityCount;
            [NonSerialized] public int BuildingRenderChildCount;
            [NonSerialized] public int NonFiniteTransformCount;
            [NonSerialized] public int ManagedMapVisualCompanionCount;
        }

        private sealed class DenseAuthoringCounts
        {
            internal int GameplayBuildings;
            internal int GameplayVehicles;
            internal int PresentationRoots;
            internal int LegacyPresentationIdentities;
            internal int DenseIdentities;
            internal int DenseGameplayBuildingIdentities;
            internal int DenseRenderOnlyIdentities;
            internal int DenseUnknownRoleIdentities;
        }

        [Serializable]
        private sealed class DenseCandidateBakeReport
        {
            public string schema;
            public int schemaVersion;
            public string operationMapId;
            public string checkpoint;
            public string result;
            public string rejectionReason;
            public string reportPath;
            public int authoringGameplayBuildingCount;
            public int authoringDenseIdentityCount;
            public int authoringDenseGameplayBuildingIdentityCount;
            public int authoringDenseRenderOnlyIdentityCount;
            public int gameplayBuildingCount;
            public int gameplayVehicleCount;
            public int presentationRootCount;
            public int legacyPresentationIdentityCount;
            public int denseIdentityCount;
            public int denseGameplayBuildingIdentityCount;
            public int denseRenderOnlyIdentityCount;
            public int denseUnknownRoleIdentityCount;
            public int duplicateDenseIdentityCount;
            public int buildingPresentationCount;
            public int virtualizedBuildingPresentationCount;
            public int renderMeshEntityCount;
            public int totalEntityCount;
            public int entityArchetypeCount;
            public int entityChunkCount;
            public int renderChildEntityCount;
            public int sharedRenderMeshArrayIdentityCount;
            public int sharedMeshAssetIdentityCount;
            public int sharedMaterialAssetIdentityCount;
            public int buildingRenderChildCount;
            public int intactVisualRootCount;
            public int destroyedVisualRootCount;
            public int missingIntactVisualRootCount;
            public int missingDestroyedVisualRootCount;
            public int sharedIntactDestroyedVisualRootCount;
            public int nonFiniteTransformCount;
            public int managedMapVisualCompanionCount;
            public int productionCutover;

            [NonSerialized] public bool Passed;
        }
    }
}

#endif
