#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Game.Authoring;
    using Game.Composition;
    using Game.Configs;
    using Game.Rendering;
    using Game.Runtime;
    using Unity.Scenes;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Builds candidate-only EntityScene definition + runtime binding ownership evidence.
    /// Never mutates production Addressables groups/labels or the production definition.
    /// </summary>
    internal static class OperationMapEntitySceneCandidateAddressablesLayoutBuilder
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem(
            "Game/Operation Maps/EntityScene Migration/Build Candidate EntityScene Addressables Layout")]
        public static void BuildCandidateEntitySceneAddressablesLayout()
        {
            BuildCandidateEntitySceneAddressablesLayout(false);
        }

        [MenuItem(
            "Game/Operation Maps/EntityScene Migration/Build Dense City Candidate EntityScene Addressables Layout")]
        public static void BuildDenseCityCandidateEntitySceneAddressablesLayout()
        {
            BuildCandidateEntitySceneAddressablesLayout(true);
        }

        private static void BuildCandidateEntitySceneAddressablesLayout(bool denseCity)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string definitionPath = denseCity
                ? OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateDefinitionPath
                : OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath;
            string runtimeBindingPath = denseCity
                ? OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath
                : OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath;
            string reportRelativePath = denseCity
                ? "Design/AgentReports/2026-07-24_dense_city_candidate_entityscene_addressables_layout.json"
                : "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_entityscene_addressables_layout.json";
            string summaryRelativePath = denseCity
                ? null
                : "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_entityscene_addressables_layout.md";
            var transactionPaths = new List<string>
            {
                definitionPath,
                definitionPath + ".meta",
                runtimeBindingPath,
                runtimeBindingPath + ".meta",
                reportRelativePath
            };
            if (!string.IsNullOrEmpty(summaryRelativePath))
                transactionPaths.Add(summaryRelativePath);

            OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction transaction =
                OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                    projectRoot,
                    transactionPaths);
            OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot protectedSnapshot =
                OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot.Capture(
                    projectRoot,
                    GetProtectedAssetPaths(denseCity),
                    new[] { "Assets/AddressableAssetsData" });
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                if (denseCity)
                {
                    EnsureDenseCityCandidateDefinition();
                    EnsureDenseCityCandidateRuntimeBindingScene();
                }
                else
                {
                    EnsureCandidateDefinition();
                    EnsureCandidateRuntimeBindingScene();
                }

                OperationMapDefinition candidateDefinition =
                    AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(definitionPath);
                if (candidateDefinition == null)
                    throw new InvalidOperationException("Candidate EntityScene definition is missing after build.");
                if (candidateDefinition.PresentationKind != OperationMapPresentationKind.EntityScene)
                    throw new InvalidOperationException("Candidate definition is not EntityScene.");
                if (!candidateDefinition.TryValidateLocalContentReferences(out string definitionError))
                    throw new InvalidOperationException($"Candidate EntityScene definition invalid: {definitionError}");

                OperationMapEntitySceneCandidateAddressablesLayoutPlan plan;
                string rejectionReason;
                bool planned = denseCity
                    ? OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreateDenseCityPlan(
                        out plan,
                        out rejectionReason)
                    : OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreatePlan(
                        out plan,
                        out rejectionReason);
                if (!planned)
                {
                    throw new InvalidOperationException(
                        $"Candidate EntityScene Addressables layout rejected: {rejectionReason}");
                }

                RequireCandidateAssetsExist(plan);
                RequireProductionAddressablesUntouched();

                string reportPath = Path.Combine(projectRoot, reportRelativePath);
                LayoutReport report = CreateReport(plan, reportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? projectRoot);
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true), Utf8WithoutBom);

                if (!string.IsNullOrEmpty(summaryRelativePath))
                {
                    string summaryPath = Path.Combine(projectRoot, summaryRelativePath);
                    File.WriteAllText(summaryPath, BuildMarkdown(report), Utf8WithoutBom);
                }

                protectedSnapshot.RequireUnchanged();
                Debug.Log(
                    $"[OperationMapEntitySceneCandidateAddressablesLayoutBuilder] status={report.result} " +
                    $"profile={(denseCity ? "DenseCity" : "Accepted")} " +
                    $"entries={report.entryCount} shared={report.sharedDependencyCount} " +
                    $"entitySceneGuid={report.entitySceneGuid} productionAddressablesMutated=0 " +
                    $"report={report.reportPath}");
            }
            catch (Exception exception)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        "Candidate Addressables layout failed and its byte rollback also failed.",
                        exception,
                        rollbackException);
                }
                throw;
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static IEnumerable<string> GetProtectedAssetPaths(bool denseCity)
        {
            var paths = new List<string>
            {
                OperationMapAddressablesLayoutBuilder.DefinitionPath,
                OperationMapAddressablesLayoutBuilder.DefinitionPath + ".meta",
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OperationMapAddressablesLayoutBuilder.SourceScenePath + ".meta",
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath,
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath + ".meta",
                OperationMapAddressablesLayoutBuilder.MapSurfacePath,
                OperationMapAddressablesLayoutBuilder.MapSurfacePath + ".meta",
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath,
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath + ".meta",
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath + ".meta",
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath + ".meta"
            };
            if (denseCity)
            {
                paths.Add(DenseCityCandidateAuthoringTransaction.CandidateMapScenePath);
                paths.Add(DenseCityCandidateAuthoringTransaction.CandidateMapScenePath + ".meta");
                paths.Add(DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
                paths.Add(DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath + ".meta");
                paths.Add(OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath);
                paths.Add(OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath + ".meta");
                paths.Add(OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath);
                paths.Add(OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath + ".meta");
            }

            return paths;
        }

        internal static OperationMapDefinition EnsureCandidateDefinition()
        {
            return EnsureCandidateDefinition(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath);
        }

        internal static OperationMapDefinition EnsureDenseCityCandidateDefinition()
        {
            return EnsureCandidateDefinition(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateDefinitionPath,
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath);
        }

        private static OperationMapDefinition EnsureCandidateDefinition(
            string candidatePath,
            string candidateSubScenePath,
            string candidateRuntimeBindingPath)
        {
            string folder = Path.GetDirectoryName(candidatePath);
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                string parent = "Assets/Game/Configs/OperationMaps";
                if (!AssetDatabase.IsValidFolder(parent + "/Candidates"))
                    AssetDatabase.CreateFolder(parent, "Candidates");
            }

            OperationMapDefinition production = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            if (production == null)
                throw new InvalidOperationException("Production operation-map definition is missing.");

            OperationMapDefinition candidate =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(candidatePath);
            bool candidateCreated = false;
            if (candidate == null)
            {
                candidate = ScriptableObject.CreateInstance<OperationMapDefinition>();
                EditorUtility.CopySerialized(production, candidate);
                AssetDatabase.CreateAsset(candidate, candidatePath);
                candidateCreated = true;
            }

            string candidateSubSceneGuid = AssetDatabase.AssetPathToGUID(
                candidateSubScenePath);
            if (string.IsNullOrEmpty(candidateSubSceneGuid))
                throw new InvalidOperationException("Candidate entity SubScene GUID is missing.");

            string runtimeBindingGuid = AssetDatabase.AssetPathToGUID(
                candidateRuntimeBindingPath);
            string mapSurfaceGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.MapSurfacePath);
            string minimapGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath);

            bool candidateChanged = ApplyCandidateDefinitionProperties(
                candidate,
                candidateSubSceneGuid,
                runtimeBindingGuid,
                mapSurfaceGuid,
                minimapGuid);
            if (candidateChanged)
                EditorUtility.SetDirty(candidate);
            if (candidateCreated || candidateChanged)
            {
                AssetDatabase.SaveAssetIfDirty(candidate);
                NormalizeAssetText(candidatePath);
                NormalizeAssetText(candidatePath + ".meta");
                AssetDatabase.ImportAsset(
                    candidatePath,
                    ImportAssetOptions.ForceSynchronousImport);
            }

            candidate = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(candidatePath);
            if (candidate == null ||
                string.IsNullOrEmpty(candidate.OperationMapId) ||
                candidate.PresentationKind != OperationMapPresentationKind.EntityScene)
            {
                throw new InvalidOperationException(
                    "Candidate EntityScene definition failed to persist after import.");
            }

            return candidate;
        }

        internal static bool ApplyCandidateDefinitionProperties(
            OperationMapDefinition candidate,
            string candidateSubSceneGuid,
            string runtimeBindingGuid,
            string mapSurfaceGuid,
            string minimapGuid)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            SerializedObject serialized = new(candidate);
            serialized.FindProperty("operationMapId").stringValue =
                OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId;
            serialized.FindProperty("presentationKind").enumValueIndex =
                (int)OperationMapPresentationKind.EntityScene;
            serialized.FindProperty("navigationMetadata")
                .FindPropertyRelative("authoredSubSceneGuid").stringValue = candidateSubSceneGuid;
            SetAssetReferenceGuid(serialized, "staticPresentationManifestReference", string.Empty);
            SetAssetReferenceGuid(serialized, "optionalHeavyMetadataReference", string.Empty);
            SetAssetReferenceGuid(serialized, "buildingPlacementsReference", string.Empty);
            SetAssetReferenceGuid(serialized, "vehiclePlacementsReference", string.Empty);
            SetAssetReferenceGuid(serialized, "mapSurfaceDataReference", mapSurfaceGuid);
            SetAssetReferenceGuid(serialized, "minimapRasterReference", minimapGuid);
            SetAssetReferenceGuid(serialized, "sourceSceneReference", runtimeBindingGuid);
            return serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureCandidateRuntimeBindingScene()
        {
            EnsureCandidateRuntimeBindingScene(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath);
        }

        internal static void EnsureDenseCityCandidateRuntimeBindingScene()
        {
            EnsureCandidateRuntimeBindingScene(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath,
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateDefinitionPath,
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
        }

        private static void EnsureCandidateRuntimeBindingScene(
            string outputPath,
            string candidateDefinitionPath,
            string candidateSubScenePath)
        {
            string folder = Path.GetDirectoryName(outputPath)?.Replace('\\', '/');
            EnsureFolder(folder);

            OperationMapDefinition definition =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(candidateDefinitionPath);
            if (definition == null ||
                string.IsNullOrEmpty(AssetDatabase.GetAssetPath(definition)))
            {
                throw new InvalidOperationException("Candidate definition must exist as a persistent asset.");
            }

            SceneAsset subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                candidateSubScenePath);
            if (subSceneAsset == null)
                throw new InvalidOperationException("Candidate entity SubScene asset is missing.");

            string productionBindingPath = OperationMapAddressablesLayoutBuilder.SourceScenePath;
            if (!File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", productionBindingPath))))
                throw new InvalidOperationException($"Production runtime binding missing: {productionBindingPath}");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string productionPhysicalPath = Path.GetFullPath(Path.Combine(projectRoot, productionBindingPath));
            string outputPhysicalPath = Path.GetFullPath(Path.Combine(projectRoot, outputPath));
            if (TryReuseExistingCandidateRuntimeBinding(
                    outputPath,
                    candidateDefinitionPath,
                    candidateSubScenePath,
                    out _))
            {
                return;
            }

            if (File.Exists(outputPhysicalPath))
            {
                // Preserve the candidate scene's .meta/GUID. Deleting and recopying this asset
                // changes its GUID every run and makes the candidate definition non-deterministic.
                UnityEngine.Object loadedOutput = AssetDatabase.LoadMainAssetAtPath(outputPath);
                if (loadedOutput != null)
                    Resources.UnloadAsset(loadedOutput);
                AssetDatabase.ReleaseCachedFileHandles();
                File.Copy(productionPhysicalPath, outputPhysicalPath, true);
            }
            else if (!AssetDatabase.CopyAsset(productionBindingPath, outputPath))
            {
                throw new InvalidOperationException($"Failed to copy production runtime binding to {outputPath}");
            }

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
            Scene scene = EditorSceneManager.OpenScene(outputPath, OpenSceneMode.Single);
            try
            {
                OperationMapSceneView view = FindSingleView(scene);
                SubScene subScene = view.MapSubScene;
                if (subScene == null)
                    throw new InvalidOperationException("Copied runtime binding is missing SubScene.");

                SerializedObject subSceneData = new(subScene);
                SerializedProperty sceneAssetProperty = subSceneData.FindProperty("m_SceneAsset") ??
                    subSceneData.FindProperty("_SceneAsset");
                if (sceneAssetProperty == null)
                {
                    // Unity.Scenes.SubScene stores the asset in SceneAsset property via public API.
                    subScene.SceneAsset = subSceneAsset;
                    subScene.AutoLoadScene = true;
                    EditorUtility.SetDirty(subScene);
                }
                else
                {
                    sceneAssetProperty.objectReferenceValue = subSceneAsset;
                    subSceneData.ApplyModifiedPropertiesWithoutUndo();
                }

                SerializedObject viewData = new(view);
                viewData.FindProperty("operationMapId").stringValue = definition.OperationMapId;
                viewData.FindProperty("definition").objectReferenceValue = definition;
                viewData.FindProperty("buildingPlacements").objectReferenceValue = null;
                viewData.FindProperty("vehiclePlacements").objectReferenceValue = null;
                viewData.FindProperty("canonicalPresentationMode").enumValueIndex =
                    (int)OperationMapCanonicalPresentationMode.EntityScene;
                viewData.FindProperty("presentationSourceSceneGuid").stringValue = string.Empty;
                viewData.FindProperty("presentationSourceScenePath").stringValue = string.Empty;
                viewData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);

                if (!EditorSceneManager.SaveScene(scene, outputPath, false))
                    throw new InvalidOperationException($"Failed to save candidate runtime binding: {outputPath}");
            }
            finally
            {
                CloseSceneKeepingEditorValid(scene);
            }

            // Fail-closed: Unity sometimes drops brand-new ScriptableObject refs in the same session.
            NormalizeAssetText(outputPath);
            NormalizeAssetText(outputPath + ".meta");
            PatchDefinitionReferenceIfMissing(outputPath, candidateDefinitionPath);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);

            if (!TryReuseExistingCandidateRuntimeBinding(
                    outputPath,
                    candidateDefinitionPath,
                    candidateSubScenePath,
                    out string validateError))
            {
                throw new InvalidOperationException(
                    $"Candidate EntityScene runtime binding invalid after reload: {validateError}");
            }

            string runtimeGuid = AssetDatabase.AssetPathToGUID(outputPath);
            definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(candidateDefinitionPath);
            if (definition == null)
                throw new InvalidOperationException("Candidate definition disappeared after runtime binding save.");
            SerializedObject definitionSerialized = new(definition);
            SetAssetReferenceGuid(definitionSerialized, "sourceSceneReference", runtimeGuid);
            if (definitionSerialized.ApplyModifiedPropertiesWithoutUndo())
                EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssetIfDirty(definition);
        }

        internal static bool TryReuseExistingCandidateRuntimeBinding(
            string outputPath,
            string candidateDefinitionPath,
            string candidateSubScenePath,
            out string error)
        {
            string physical = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", outputPath));
            if (!File.Exists(physical))
            {
                error = "Candidate runtime binding scene is missing.";
                return false;
            }

            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenScene(outputPath, OpenSceneMode.Single);
                OperationMapSceneView view = FindSingleView(scene);
                return OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                    scene,
                    view.OperationMapId,
                    candidateDefinitionPath,
                    candidateSubScenePath,
                    out error);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                CloseSceneKeepingEditorValid(scene);
            }
        }

        internal static bool NormalizeAssetText(string assetPath)
        {
            string physical = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            if (!File.Exists(physical))
                return false;

            string[] lines = File.ReadAllLines(physical);
            var normalized = new StringBuilder();
            for (int index = 0; index < lines.Length; index++)
                normalized.Append(lines[index].TrimEnd(' ', '\t')).Append('\n');
            byte[] normalizedBytes = Utf8WithoutBom.GetBytes(normalized.ToString());
            byte[] currentBytes = File.ReadAllBytes(physical);
            if (BytesEqual(currentBytes, normalizedBytes))
                return false;

            // Imported YAML assets can remain memory-mapped on Windows. Only release
            // Unity's cached handles when normalization has a real byte change; the
            // common second-run no-op must not rewrite the loaded candidate asset.
            UnityEngine.Object loadedAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (loadedAsset != null)
                Resources.UnloadAsset(loadedAsset);
            AssetDatabase.ReleaseCachedFileHandles();
            File.WriteAllBytes(physical, normalizedBytes);
            return true;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int index = 0; index < left.Length; index++)
            {
                if (left[index] != right[index])
                    return false;
            }
            return true;
        }

        private static void PatchDefinitionReferenceIfMissing(
            string scenePath,
            string definitionPath)
        {
            string physical = Path.GetFullPath(Path.Combine(Application.dataPath, "..", scenePath));
            string definitionGuid = AssetDatabase.AssetPathToGUID(definitionPath);
            if (string.IsNullOrEmpty(definitionGuid) || !File.Exists(physical))
                throw new InvalidOperationException("Cannot patch candidate runtime binding definition reference.");

            string text = File.ReadAllText(physical, Utf8WithoutBom);
            const string missing = "definition: {fileID: 0}";
            string replacement =
                $"definition: {{fileID: 11400000, guid: {definitionGuid}, type: 2}}";
            if (text.Contains(missing))
            {
                text = text.Replace(missing, replacement);
                File.WriteAllText(physical, text, Utf8WithoutBom);
                return;
            }

            string productionDefinitionGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            if (!string.IsNullOrEmpty(productionDefinitionGuid) &&
                text.Contains($"guid: {productionDefinitionGuid}"))
            {
                text = text.Replace(
                    $"guid: {productionDefinitionGuid}",
                    $"guid: {definitionGuid}");
                File.WriteAllText(physical, text, Utf8WithoutBom);
                return;
            }

            if (text.IndexOf($"guid: {definitionGuid}", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException(
                    "Candidate runtime binding does not reference the candidate definition after save.");
            }
        }

        private static void RequireCandidateAssetsExist(
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan)
        {
            for (int i = 0; i < plan.Entries.Count; i++)
            {
                OperationMapEntitySceneCandidateAddressablesLayoutEntry entry = plan.Entries[i];
                if (string.Equals(entry.Role, "shared-dependency", StringComparison.Ordinal))
                {
                    if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(entry.AssetPath)))
                        throw new InvalidOperationException($"Shared dependency missing: {entry.AssetPath}");
                    continue;
                }

                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(entry.AssetPath)))
                    throw new InvalidOperationException($"Candidate layout asset missing: {entry.AssetPath}");
            }
        }

        private static void RequireProductionAddressablesUntouched()
        {
            // Candidate builder never writes AddressableAssetSettings. Keep this explicit for fail-closed logs.
            OperationMapDefinition production = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            if (production == null)
                throw new InvalidOperationException("Production definition missing during candidate layout build.");
            if (production.PresentationKind != OperationMapPresentationKind.StaticSceneChunks)
            {
                throw new InvalidOperationException(
                    "Production definition presentation kind changed unexpectedly during candidate layout build.");
            }
        }

        private static LayoutReport CreateReport(
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
            string reportPath)
        {
            var entries = new List<LayoutEntryReport>(plan.Entries.Count);
            for (int i = 0; i < plan.Entries.Count; i++)
            {
                OperationMapEntitySceneCandidateAddressablesLayoutEntry entry = plan.Entries[i];
                entries.Add(
                    new LayoutEntryReport
                    {
                        role = entry.Role,
                        assetPath = entry.AssetPath,
                        address = entry.Address,
                        roleLabel = entry.RoleLabel,
                        guid = AssetDatabase.AssetPathToGUID(entry.AssetPath)
                    });
            }

            return new LayoutReport
            {
                schema = "warline.operation-map.entity-scene-candidate-addressables-layout",
                schemaVersion = 1,
                result = "CandidateEntitySceneAddressablesLayoutReady",
                operationMapId = plan.OperationMapId,
                packLabel = plan.PackLabel,
                addressPrefix = plan.AddressPrefix,
                entitySceneGuid = plan.EntitySceneGuid,
                entryCount = plan.Entries.Count,
                sharedDependencyCount = plan.SharedDependencyCount,
                staticManifestEntryCount = 0,
                presentationChunkEntryCount = 0,
                legacyPlacementEntryCount = 0,
                productionAddressablesMutated = 0,
                productionPresentationKind = OperationMapPresentationKind.StaticSceneChunks.ToString(),
                reportPath = reportPath.Replace('\\', '/'),
                entries = entries
            };
        }

        private static string BuildMarkdown(LayoutReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Phase 0A Candidate EntityScene Addressables Layout");
            sb.AppendLine();
            sb.AppendLine($"Date: 2026-07-21");
            sb.AppendLine($"Result: `{report.result}`");
            sb.AppendLine();
            sb.AppendLine("## Ownership");
            sb.AppendLine();
            sb.AppendLine($"- Operation map: `{report.operationMapId}`");
            sb.AppendLine($"- Pack label: `{report.packLabel}`");
            sb.AppendLine($"- Entity scene GUID: `{report.entitySceneGuid}`");
            sb.AppendLine($"- Entries: {report.entryCount}");
            sb.AppendLine($"- Shared art dependencies: {report.sharedDependencyCount}");
            sb.AppendLine($"- Static manifest entries: {report.staticManifestEntryCount}");
            sb.AppendLine($"- Presentation chunk entries: {report.presentationChunkEntryCount}");
            sb.AppendLine($"- Legacy placement entries: {report.legacyPlacementEntryCount}");
            sb.AppendLine($"- Production Addressables mutated: {report.productionAddressablesMutated}");
            sb.AppendLine($"- Production presentation kind: `{report.productionPresentationKind}`");
            sb.AppendLine();
            sb.AppendLine("## Hard stops honored");
            sb.AppendLine();
            sb.AppendLine("- Production definition remains `StaticSceneChunks`.");
            sb.AppendLine("- Production Addressables groups/labels were not rewritten.");
            sb.AppendLine("- Candidate layout excludes static manifest, chunk scenes, and legacy placement runtime entries.");
            return sb.ToString();
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (string.IsNullOrEmpty(assetFolder) || AssetDatabase.IsValidFolder(assetFolder))
                return;

            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static OperationMapSceneView FindSingleView(Scene scene)
        {
            OperationMapSceneView found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                OperationMapSceneView[] views = root.GetComponentsInChildren<OperationMapSceneView>(true);
                for (int i = 0; i < views.Length; i++)
                {
                    if (found != null)
                        throw new InvalidOperationException("Multiple operation-map views in production binding.");
                    found = views[i];
                }
            }

            return found ?? throw new InvalidOperationException("Production runtime binding view is missing.");
        }

        private static void CloseSceneKeepingEditorValid(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            if (SceneManager.sceneCount == 1)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                return;
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void SetAssetReferenceGuid(
            SerializedObject serialized,
            string fieldName,
            string guid)
        {
            SerializedProperty reference = serialized.FindProperty(fieldName);
            SerializedProperty assetGuid = reference?.FindPropertyRelative("m_AssetGUID");
            if (assetGuid == null)
                throw new InvalidOperationException($"Definition field missing: {fieldName}");
            assetGuid.stringValue = guid ?? string.Empty;
        }

        [Serializable]
        private sealed class LayoutReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string packLabel;
            public string addressPrefix;
            public string entitySceneGuid;
            public int entryCount;
            public int sharedDependencyCount;
            public int staticManifestEntryCount;
            public int presentationChunkEntryCount;
            public int legacyPlacementEntryCount;
            public int productionAddressablesMutated;
            public string productionPresentationKind;
            public string reportPath;
            public List<LayoutEntryReport> entries;
        }

        [Serializable]
        private sealed class LayoutEntryReport
        {
            public string role;
            public string assetPath;
            public string address;
            public string roleLabel;
            public string guid;
        }
    }
}

#endif
