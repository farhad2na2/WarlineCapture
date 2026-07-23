#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Authoring;
    using Game.Configs;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Debug = UnityEngine.Debug;

    /// <summary>
    /// Candidate-only transaction for the existing-map EntityScene migration. Production remains
    /// StaticSceneChunks until the separate visual, Editor, and Android acceptance gates pass.
    /// </summary>
    internal static class OperationMapEntitySceneCandidateBakeAll
    {
        internal const string ReportJsonPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_all.json";
        internal const string ReportMarkdownPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_all.md";

        private const int ExpectedBuildings = 432;
        private const int ExpectedVehicles = 22;
        private const int ExpectedRenderOnlyOwners = 9090;
        private const int ExpectedPresentationRoots = 3;
        private const int MinimumRenderMeshEntities = 9090;

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        private static readonly string[] CandidateOwnedPaths =
        {
            OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
            OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath + ".meta",
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath + ".meta",
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
            OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath + ".meta"
        };

        private static readonly string[] ProtectedProductionFiles =
        {
            OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
            OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
            OperationMapAddressablesLayoutBuilder.DefinitionPath,
            OperationMapAddressablesLayoutBuilder.SourceScenePath,
            "Assets/AddressableAssetsData/AddressableAssetSettings.asset"
        };

        private static readonly string[] ProtectedProductionDirectories =
        {
            OperationMapEntityPresentationCandidateSceneBuilder.StaticRollbackRoot,
            "Assets/AddressableAssetsData/AssetGroups"
        };

        [MenuItem("Game/Operation Maps/EntityScene Migration/Bake All Candidate EntityScene")]
        public static void BakeAllCandidateEntityScene()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            CandidateFileTransaction transaction = CandidateFileTransaction.Capture(
                projectRoot,
                CandidateOwnedPaths);
            ProtectedProductionSnapshot production = ProtectedProductionSnapshot.Capture(
                projectRoot,
                ProtectedProductionFiles,
                ProtectedProductionDirectories);
            var report = new CandidateBakeAllReport
            {
                schema = "warline.operation-map.entity-scene-candidate-bake-all",
                schemaVersion = 2,
                result = "CandidateBakeAllFailed",
                operationMapId = OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                productionPresentationKind = OperationMapPresentationKind.StaticSceneChunks.ToString(),
                productionCutover = 0,
                rollbackApplied = 0,
                stages = new List<CandidateBakeAllStageReport>()
            };
            OperationMapEntityPresentationTransformParityValidator.InvalidateEvidence(
                projectRoot,
                "candidate-bake-all-started");
            DenseCityPresentationBudgetValidator.InvalidateEvidence(
                projectRoot,
                "candidate-bake-all-started");

            try
            {
                RunStage(report, "preflight-isolation", () => RequirePreflight(production));
                RunStage(
                    report,
                    "source-physics-readiness",
                    OperationMapSourceScenePhysicsValidator.ValidateAcceptedSourcesBatch);
                RunStage(report, "candidate-population", EnsureCandidatePopulation);
                RunStage(report, "candidate-presentation-identities", () =>
                    OperationMapEntityPresentationIdentityBackfillEditor
                        .BackfillCandidatePresentationIdentities());
                RunStage(report, "candidate-source-transform-parity", () =>
                    ValidateSourceCandidateTransformParity(projectRoot));
                RunStage(report, "candidate-authoring-readiness", () =>
                {
                    CandidateAuthoringCounts counts = InspectCandidateAuthoring();
                    RequireAuthoringBudgets(counts);
                    report.buildingAuthoringCount = counts.Buildings;
                    report.vehicleAuthoringCount = counts.Vehicles;
                    report.renderOnlyOwnerCount = counts.RenderOnlyOwners;
                    report.presentationRootCount = counts.PresentationRoots;
                    report.presentationIdentityCount = counts.PresentationIdentities;
                    report.colliderCount = counts.Colliders;
                    report.rigidbodyCount = counts.Rigidbodies;
                });
                RunStage(report, "candidate-entity-bake", () =>
                    OperationMapEntityPresentationCandidateBakeValidator
                        .BakeAndValidateCandidateEntityPresentation());
                RunStage(report, "shared-art-budget", () =>
                {
                    OperationMapEntityPresentationSharedArtOwnershipProbe.ProveSharedArtOwnership();
                    RequireSharedArtBudget(projectRoot, report);
                });
                RunStage(report, "candidate-binding-layout", () =>
                    OperationMapEntitySceneCandidateAddressablesLayoutBuilder
                        .BuildCandidateEntitySceneAddressablesLayout());
                RunStage(
                    report,
                    "runtime-physics-readiness",
                    OperationMapEntitySceneRuntimePhysicsValidator.ValidateCurrentCandidateBatch);
                RunStage(report, "candidate-bake-budget", () =>
                    RequireBakeAndLayoutBudgets(projectRoot, report));
                RunStage(
                    report,
                    "presentation-budget",
                    DenseCityPresentationBudgetValidator.ValidateCurrentCandidateBatch);
                RunStage(report, "postflight-isolation", () => RequirePostflight(production));

                report.result = "CandidateBakeAllPassedPendingVisualAndRuntimeAcceptance";
                WriteReport(projectRoot, report);
                Debug.Log(
                    $"[OperationMapEntitySceneCandidateBakeAll] status={report.result} " +
                    $"buildings={report.buildingAuthoringCount} vehicles={report.vehicleAuthoringCount} " +
                    $"renderOnly={report.renderOnlyOwnerCount} renderMeshes={report.renderMeshEntityCount} " +
                    $"sharedDependencies={report.sharedDependencyCount} productionCutover=0 " +
                    $"report={ReportJsonPath}");
            }
            catch (Exception exception)
            {
                report.failure = exception.Message;
                try
                {
                    DenseCityPresentationBudgetValidator.InvalidateEvidence(
                        projectRoot,
                        $"candidate-bake-all-failed:{exception.GetType().Name}");
                }
                catch (Exception evidenceException)
                {
                    report.evidenceInvalidationFailure = evidenceException.Message;
                }
                try
                {
                    transaction.Rollback();
                    report.rollbackApplied = 1;
                    production.RequireUnchanged();
                }
                catch (Exception rollbackException)
                {
                    report.rollbackFailure = rollbackException.Message;
                }

                WriteReport(projectRoot, report);
                throw new InvalidOperationException(
                    $"Candidate Bake All failed and candidate-owned outputs were rolled back. " +
                    $"Reason: {exception.Message}",
                    exception);
            }
        }

        internal static void RequireBakeBudget(CandidateBakeBudget budget)
        {
            if (!string.Equals(
                    budget.Result,
                    "CandidateBakeValidationPassed",
                    StringComparison.Ordinal))
                throw new InvalidOperationException($"Candidate bake result rejected: {budget.Result}");
            if (budget.GameplayBuildingCount != ExpectedBuildings)
                throw new InvalidOperationException($"Candidate bake building count is {budget.GameplayBuildingCount}.");
            if (budget.PresentationRootCount != ExpectedPresentationRoots)
                throw new InvalidOperationException($"Candidate bake root count is {budget.PresentationRootCount}.");
            if (budget.RenderMeshEntityCount < MinimumRenderMeshEntities)
                throw new InvalidOperationException($"Candidate render-mesh count is {budget.RenderMeshEntityCount}.");
            if (budget.NonFiniteTransformCount != 0 || budget.ManagedMapVisualCompanionCount != 0)
            {
                throw new InvalidOperationException(
                    $"Candidate bake contains invalid transforms/managed companions: " +
                    $"{budget.NonFiniteTransformCount}/{budget.ManagedMapVisualCompanionCount}.");
            }
        }

        internal static void RequireLayoutBudget(CandidateLayoutBudget budget)
        {
            if (!string.Equals(
                    budget.Result,
                    "CandidateEntitySceneAddressablesLayoutReady",
                    StringComparison.Ordinal))
                throw new InvalidOperationException($"Candidate layout result rejected: {budget.Result}");
            if (budget.SharedDependencyCount != 0)
                throw new InvalidOperationException("Candidate layout has explicit shared-dependency ownership.");
            if (budget.StaticManifestEntryCount != 0 ||
                budget.PresentationChunkEntryCount != 0 ||
                budget.LegacyPlacementEntryCount != 0 ||
                budget.ProductionAddressablesMutated != 0)
            {
                throw new InvalidOperationException("Candidate layout includes legacy or production-owned content.");
            }
        }

        private static void RunStage(CandidateBakeAllReport report, string name, Action action)
        {
            var stage = new CandidateBakeAllStageReport { name = name, result = "Failed" };
            report.stages.Add(stage);
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                action();
                stage.result = "Passed";
            }
            catch (Exception exception)
            {
                stage.failure = exception.Message;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                stage.elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }
        }

        private static void RequirePreflight(ProtectedProductionSnapshot production)
        {
            production.RequireUnchanged();
            OperationMapDefinition productionDefinition =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    OperationMapAddressablesLayoutBuilder.DefinitionPath);
            if (productionDefinition == null ||
                productionDefinition.PresentationKind != OperationMapPresentationKind.StaticSceneChunks)
            {
                throw new InvalidOperationException("Production definition is not the protected StaticSceneChunks baseline.");
            }

            string candidateGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath);
            string acceptedGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);
            if (!string.IsNullOrEmpty(candidateGuid) &&
                string.Equals(candidateGuid, acceptedGuid, StringComparison.Ordinal))
                throw new InvalidOperationException("Candidate and accepted SubScene GUIDs collide.");

            if (!string.IsNullOrEmpty(candidateGuid))
            {
                string productionScene = File.ReadAllText(
                    ResolveProjectPath(OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath),
                    Utf8WithoutBom);
                if (productionScene.IndexOf(candidateGuid, StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("Production scene already references the candidate SubScene.");
            }
        }

        private static void EnsureCandidatePopulation()
        {
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(candidatePath) == null)
                OperationMapEntityPresentationCandidateSceneBuilder.CreateProtectedCandidateHierarchy();

            CandidateAuthoringCounts counts = InspectCandidateAuthoring();
            if (counts.Buildings == 0)
                OperationMapBuildingCandidateMigrationEditor.PopulateCandidateGameplayBuildings();
            else if (counts.Buildings != ExpectedBuildings)
                throw new InvalidOperationException($"Partial candidate building population: {counts.Buildings}.");

            counts = InspectCandidateAuthoring();
            if (counts.RenderOnlyOwners == 0)
                OperationMapRenderOnlyCandidateMigrationEditor.PopulateCandidateRenderOnlyOwners();
            else if (counts.RenderOnlyOwners != ExpectedRenderOnlyOwners)
                throw new InvalidOperationException($"Partial candidate render-only population: {counts.RenderOnlyOwners}.");

            counts = InspectCandidateAuthoring();
            if (counts.Vehicles == 0)
                OperationMapVehicleCandidateMigrationEditor.PopulateCandidateGameplayVehicles();
            else if (counts.Vehicles != ExpectedVehicles)
                throw new InvalidOperationException($"Partial candidate vehicle population: {counts.Vehicles}.");
        }

        private static CandidateAuthoringCounts InspectCandidateAuthoring()
        {
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                Transform root = RequirePath(scene, "AuthoredOperationMapEntityPresentation");
                Transform buildings = RequirePath(scene, "AuthoredOperationMapEntityPresentation/GameplayBuildings");
                Transform vehicles = RequirePath(scene, "AuthoredOperationMapEntityPresentation/GameplayVehicles");
                Transform renderOnly = RequirePath(scene, "AuthoredOperationMapEntityPresentation/RenderOnly");
                int renderOwners = renderOnly
                    .GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true)
                    .Count(identity => identity.Role == OperationMapEntityPresentationRole.RenderOnly);

                return new CandidateAuthoringCounts(
                    buildings.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length,
                    vehicles.childCount,
                    renderOwners,
                    root.GetComponentsInChildren<OperationMapEntityPresentationRootAuthoring>(true).Length,
                    root.GetComponentsInChildren<OperationMapEntityPresentationIdentityAuthoring>(true).Length,
                    root.GetComponentsInChildren<Collider>(true).Length,
                    root.GetComponentsInChildren<Rigidbody>(true).Length);
            }
            finally
            {
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        private static void ValidateSourceCandidateTransformParity(string projectRoot)
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene sourceScene = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                    OpenSceneMode.Additive);
                Scene candidateScene = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                    OpenSceneMode.Additive);
                OperationMapEntityPresentationTransformParityValidator.ValidateSourceCandidateAndWrite(
                    projectRoot,
                    sourceScene,
                    candidateScene);
            }
            finally
            {
                RestoreSceneSetupOrCreateEmpty(previousSetup);
            }
        }

        internal static bool HasRestorableSceneSetup(SceneSetup[] setup) =>
            setup != null && setup.Any(scene => scene.isLoaded && scene.isActive);

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] previousSetup)
        {
            if (HasRestorableSceneSetup(previousSetup))
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                return;
            }

            // Headless executeMethod can start without any loaded scene. Unity rejects restoring
            // that empty setup, so leave the transaction with one clean active scene instead.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void RequireAuthoringBudgets(CandidateAuthoringCounts counts)
        {
            if (counts.Buildings != ExpectedBuildings ||
                counts.Vehicles != ExpectedVehicles ||
                counts.RenderOnlyOwners != ExpectedRenderOnlyOwners ||
                counts.PresentationRoots != ExpectedPresentationRoots ||
                counts.PresentationIdentities !=
                OperationMapEntityPresentationIdentityBackfillEditor.ExpectedIdentityCount)
            {
                throw new InvalidOperationException(
                    $"Candidate authoring counts rejected: buildings={counts.Buildings}, " +
                    $"vehicles={counts.Vehicles}, renderOnly={counts.RenderOnlyOwners}, " +
                    $"roots={counts.PresentationRoots}, identities={counts.PresentationIdentities}.");
            }

            if (counts.Colliders != 0 || counts.Rigidbodies != 0)
                throw new InvalidOperationException($"Candidate contains physics components: {counts.Colliders}/{counts.Rigidbodies}.");
        }

        private static void RequireSharedArtBudget(string projectRoot, CandidateBakeAllReport report)
        {
            string path = Path.Combine(
                projectRoot,
                "Design/AgentReports/2026-07-21_dense_city_phase0a_shared_art_ownership.json");
            SharedArtBudget budget = JsonUtility.FromJson<SharedArtBudget>(File.ReadAllText(path, Utf8WithoutBom));
            if (budget == null ||
                !string.Equals(budget.result, "SharedArtOwnershipProven", StringComparison.Ordinal) ||
                budget.missingAssetCount != 0 || !budget.compactInstanceDataProven)
                throw new InvalidOperationException("Shared art ownership budget failed.");
            report.sharedArtSourceCount = budget.sourceCount;
            report.uniqueMeshAssetCount = budget.uniqueMeshAssetCount;
            report.uniqueMaterialAssetCount = budget.uniqueMaterialAssetCount;
            report.uniquePrefabAssetCount = budget.uniquePrefabAssetCount;
        }

        private static void RequireBakeAndLayoutBudgets(string projectRoot, CandidateBakeAllReport report)
        {
            CandidateBakeBudgetJson bakeJson = JsonUtility.FromJson<CandidateBakeBudgetJson>(
                File.ReadAllText(
                    Path.Combine(projectRoot, "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_bake_validation.json"),
                    Utf8WithoutBom));
            var bake = new CandidateBakeBudget(
                bakeJson.result,
                bakeJson.gameplayBuildingCount,
                bakeJson.presentationRootCount,
                bakeJson.renderMeshEntityCount,
                bakeJson.nonFiniteTransformCount,
                bakeJson.managedMapVisualCompanionCount);
            RequireBakeBudget(bake);

            CandidateLayoutBudgetJson layoutJson = JsonUtility.FromJson<CandidateLayoutBudgetJson>(
                File.ReadAllText(
                    Path.Combine(projectRoot, "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_entityscene_addressables_layout.json"),
                    Utf8WithoutBom));
            var layout = new CandidateLayoutBudget(
                layoutJson.result,
                layoutJson.sharedDependencyCount,
                layoutJson.staticManifestEntryCount,
                layoutJson.presentationChunkEntryCount,
                layoutJson.legacyPlacementEntryCount,
                layoutJson.productionAddressablesMutated);
            RequireLayoutBudget(layout);

            report.renderMeshEntityCount = bake.RenderMeshEntityCount;
            report.nonFiniteTransformCount = bake.NonFiniteTransformCount;
            report.managedMapVisualCompanionCount = bake.ManagedMapVisualCompanionCount;
            report.sharedDependencyCount = layout.SharedDependencyCount;
        }

        private static void RequirePostflight(ProtectedProductionSnapshot production)
        {
            production.RequireUnchanged();
            OperationMapDefinition candidate = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath);
            if (candidate == null)
                throw new InvalidOperationException("Candidate EntityScene definition is missing.");
            if (candidate.PresentationKind != OperationMapPresentationKind.EntityScene)
                throw new InvalidOperationException("Candidate definition is not EntityScene.");
            if (!candidate.TryValidateLocalContentReferences(out string error))
                throw new InvalidOperationException($"Candidate EntityScene definition is invalid: {error}");
            if (!OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreatePlan(
                    out _,
                    out string rejectionReason))
                throw new InvalidOperationException($"Candidate ownership layout is invalid: {rejectionReason}");
        }

        private static Transform RequirePath(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            Transform current = scene.GetRootGameObjects()
                .SingleOrDefault(owner => owner.name == segments[0])?.transform;
            for (int i = 1; i < segments.Length && current != null; i++)
                current = current.Find(segments[i]);
            return current ?? throw new InvalidOperationException($"Candidate hierarchy path is missing: {path}");
        }

        private static void WriteReport(string projectRoot, CandidateBakeAllReport report)
        {
            string jsonPath = Path.Combine(projectRoot, ReportJsonPath);
            Directory.CreateDirectory(Path.GetDirectoryName(jsonPath) ?? projectRoot);
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true), Utf8WithoutBom);

            var markdown = new StringBuilder();
            markdown.AppendLine("# Phase 0A Candidate Bake All");
            markdown.AppendLine();
            markdown.AppendLine($"Result: `{report.result}`");
            markdown.AppendLine($"Production cutover: `{report.productionCutover}`");
            markdown.AppendLine($"Rollback applied: `{report.rollbackApplied}`");
            if (!string.IsNullOrEmpty(report.failure))
                markdown.AppendLine($"Failure: `{report.failure}`");
            markdown.AppendLine();
            markdown.AppendLine("| Stage | Result | Milliseconds | Failure |");
            markdown.AppendLine("|---|---|---:|---|");
            foreach (CandidateBakeAllStageReport stage in report.stages)
                markdown.AppendLine($"| {stage.name} | {stage.result} | {stage.elapsedMilliseconds} | {stage.failure} |");
            markdown.AppendLine();
            markdown.AppendLine("Visual parity, Editor lifecycle acceptance, Android acceptance, and production cutover remain separate gates.");
            File.WriteAllText(Path.Combine(projectRoot, ReportMarkdownPath), markdown.ToString(), Utf8WithoutBom);
        }

        private static string ResolveProjectPath(string repositoryPath) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", repositoryPath));

        internal readonly struct CandidateBakeBudget
        {
            internal CandidateBakeBudget(
                string result,
                int gameplayBuildingCount,
                int presentationRootCount,
                int renderMeshEntityCount,
                int nonFiniteTransformCount,
                int managedMapVisualCompanionCount)
            {
                Result = result;
                GameplayBuildingCount = gameplayBuildingCount;
                PresentationRootCount = presentationRootCount;
                RenderMeshEntityCount = renderMeshEntityCount;
                NonFiniteTransformCount = nonFiniteTransformCount;
                ManagedMapVisualCompanionCount = managedMapVisualCompanionCount;
            }

            internal string Result { get; }
            internal int GameplayBuildingCount { get; }
            internal int PresentationRootCount { get; }
            internal int RenderMeshEntityCount { get; }
            internal int NonFiniteTransformCount { get; }
            internal int ManagedMapVisualCompanionCount { get; }
        }

        internal readonly struct CandidateLayoutBudget
        {
            internal CandidateLayoutBudget(
                string result,
                int sharedDependencyCount,
                int staticManifestEntryCount,
                int presentationChunkEntryCount,
                int legacyPlacementEntryCount,
                int productionAddressablesMutated)
            {
                Result = result;
                SharedDependencyCount = sharedDependencyCount;
                StaticManifestEntryCount = staticManifestEntryCount;
                PresentationChunkEntryCount = presentationChunkEntryCount;
                LegacyPlacementEntryCount = legacyPlacementEntryCount;
                ProductionAddressablesMutated = productionAddressablesMutated;
            }

            internal string Result { get; }
            internal int SharedDependencyCount { get; }
            internal int StaticManifestEntryCount { get; }
            internal int PresentationChunkEntryCount { get; }
            internal int LegacyPlacementEntryCount { get; }
            internal int ProductionAddressablesMutated { get; }
        }

        private readonly struct CandidateAuthoringCounts
        {
            internal CandidateAuthoringCounts(
                int buildings,
                int vehicles,
                int renderOnlyOwners,
                int presentationRoots,
                int presentationIdentities,
                int colliders,
                int rigidbodies)
            {
                Buildings = buildings;
                Vehicles = vehicles;
                RenderOnlyOwners = renderOnlyOwners;
                PresentationRoots = presentationRoots;
                PresentationIdentities = presentationIdentities;
                Colliders = colliders;
                Rigidbodies = rigidbodies;
            }

            internal int Buildings { get; }
            internal int Vehicles { get; }
            internal int RenderOnlyOwners { get; }
            internal int PresentationRoots { get; }
            internal int PresentationIdentities { get; }
            internal int Colliders { get; }
            internal int Rigidbodies { get; }
        }

        [Serializable]
        private sealed class CandidateBakeBudgetJson
        {
            public string result;
            public int gameplayBuildingCount;
            public int presentationRootCount;
            public int renderMeshEntityCount;
            public int nonFiniteTransformCount;
            public int managedMapVisualCompanionCount;
        }

        [Serializable]
        private sealed class CandidateLayoutBudgetJson
        {
            public string result;
            public int sharedDependencyCount;
            public int staticManifestEntryCount;
            public int presentationChunkEntryCount;
            public int legacyPlacementEntryCount;
            public int productionAddressablesMutated;
        }

        [Serializable]
        private sealed class SharedArtBudget
        {
            public string result;
            public int sourceCount;
            public int uniqueMeshAssetCount;
            public int uniqueMaterialAssetCount;
            public int uniquePrefabAssetCount;
            public int missingAssetCount;
            public bool compactInstanceDataProven;
        }

        [Serializable]
        private sealed class CandidateBakeAllReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string productionPresentationKind;
            public int productionCutover;
            public int rollbackApplied;
            public string failure;
            public string evidenceInvalidationFailure;
            public string rollbackFailure;
            public int buildingAuthoringCount;
            public int vehicleAuthoringCount;
            public int renderOnlyOwnerCount;
            public int presentationRootCount;
            public int presentationIdentityCount;
            public int colliderCount;
            public int rigidbodyCount;
            public int renderMeshEntityCount;
            public int nonFiniteTransformCount;
            public int managedMapVisualCompanionCount;
            public int sharedArtSourceCount;
            public int uniqueMeshAssetCount;
            public int uniqueMaterialAssetCount;
            public int uniquePrefabAssetCount;
            public int sharedDependencyCount;
            public List<CandidateBakeAllStageReport> stages;
        }

        [Serializable]
        private sealed class CandidateBakeAllStageReport
        {
            public string name;
            public string result;
            public long elapsedMilliseconds;
            public string failure;
        }

        internal sealed class CandidateFileTransaction
        {
            private readonly string projectRoot;
            private readonly List<FileState> files;

            private CandidateFileTransaction(string projectRoot, List<FileState> files)
            {
                this.projectRoot = projectRoot;
                this.files = files;
            }

            internal static CandidateFileTransaction Capture(string projectRoot, IEnumerable<string> paths)
            {
                var files = new List<FileState>();
                foreach (string path in paths.Distinct(StringComparer.Ordinal))
                {
                    string physical = Path.GetFullPath(Path.Combine(projectRoot, path));
                    files.Add(new FileState(path, File.Exists(physical), File.Exists(physical) ? File.ReadAllBytes(physical) : null));
                }
                return new CandidateFileTransaction(projectRoot, files);
            }

            internal void Rollback()
            {
                foreach (FileState state in files)
                {
                    string physical = Path.GetFullPath(Path.Combine(projectRoot, state.Path));
                    if (state.Existed)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(physical) ?? projectRoot);
                        File.WriteAllBytes(physical, state.Bytes);
                    }
                    else if (File.Exists(physical))
                    {
                        File.Delete(physical);
                    }
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            private readonly struct FileState
            {
                internal FileState(string path, bool existed, byte[] bytes)
                {
                    Path = path;
                    Existed = existed;
                    Bytes = bytes;
                }

                internal string Path { get; }
                internal bool Existed { get; }
                internal byte[] Bytes { get; }
            }
        }

        internal sealed class ProtectedProductionSnapshot
        {
            private readonly string projectRoot;
            private readonly Dictionary<string, string> hashes;

            private ProtectedProductionSnapshot(string projectRoot, Dictionary<string, string> hashes)
            {
                this.projectRoot = projectRoot;
                this.hashes = hashes;
            }

            internal static ProtectedProductionSnapshot Capture(
                string projectRoot,
                IEnumerable<string> files,
                IEnumerable<string> directories)
            {
                var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string path in files)
                    hashes["file:" + path] = ComputeFileHash(Path.Combine(projectRoot, path));
                foreach (string path in directories)
                    hashes["directory:" + path] = ComputeDirectoryHash(Path.Combine(projectRoot, path));
                return new ProtectedProductionSnapshot(projectRoot, hashes);
            }

            internal void RequireUnchanged()
            {
                foreach (KeyValuePair<string, string> pair in hashes)
                {
                    int separator = pair.Key.IndexOf(':');
                    string kind = pair.Key.Substring(0, separator);
                    string path = pair.Key.Substring(separator + 1);
                    string actual = string.Equals(kind, "file", StringComparison.Ordinal)
                        ? ComputeFileHash(Path.Combine(projectRoot, path))
                        : ComputeDirectoryHash(Path.Combine(projectRoot, path));
                    if (!string.Equals(pair.Value, actual, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Protected production {kind} changed: {path}");
                }
            }

            private static string ComputeFileHash(string path)
            {
                if (!File.Exists(path))
                    return "<missing>";
                using Stream stream = File.OpenRead(path);
                using SHA256 sha = SHA256.Create();
                return ToHex(sha.ComputeHash(stream));
            }

            private static string ComputeDirectoryHash(string path)
            {
                if (!Directory.Exists(path))
                    return "<missing>";
                using SHA256 sha = SHA256.Create();
                var builder = new StringBuilder();
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                             .OrderBy(value => value, StringComparer.Ordinal))
                {
                    builder.Append(Path.GetRelativePath(path, file).Replace('\\', '/'));
                    builder.Append(':');
                    builder.Append(ComputeFileHash(file));
                    builder.Append('\n');
                }
                return ToHex(sha.ComputeHash(Utf8WithoutBom.GetBytes(builder.ToString())));
            }

            private static string ToHex(byte[] bytes) =>
                string.Concat(bytes.Select(value => value.ToString("x2")));
        }
    }
}

#endif
