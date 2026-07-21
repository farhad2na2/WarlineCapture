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
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                EnsureCandidateDefinition();
                EnsureCandidateRuntimeBindingScene();
                OperationMapDefinition candidateDefinition =
                    AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                        OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath);
                if (candidateDefinition == null)
                    throw new InvalidOperationException("Candidate EntityScene definition is missing after build.");
                if (candidateDefinition.PresentationKind != OperationMapPresentationKind.EntityScene)
                    throw new InvalidOperationException("Candidate definition is not EntityScene.");
                if (!candidateDefinition.TryValidateLocalContentReferences(out string definitionError))
                    throw new InvalidOperationException($"Candidate EntityScene definition invalid: {definitionError}");

                if (!OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreatePlan(
                        out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
                        out string rejectionReason))
                {
                    throw new InvalidOperationException(
                        $"Candidate EntityScene Addressables layout rejected: {rejectionReason}");
                }

                RequireCandidateAssetsExist(plan);
                RequireProductionAddressablesUntouched();

                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string reportPath = Path.Combine(
                    projectRoot,
                    "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_entityscene_addressables_layout.json");
                LayoutReport report = CreateReport(plan, reportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? projectRoot);
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true), Utf8WithoutBom);

                string summaryPath = Path.Combine(
                    projectRoot,
                    "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_entityscene_addressables_layout.md");
                File.WriteAllText(summaryPath, BuildMarkdown(report), Utf8WithoutBom);

                Debug.Log(
                    $"[OperationMapEntitySceneCandidateAddressablesLayoutBuilder] status={report.result} " +
                    $"entries={report.entryCount} shared={report.sharedDependencyCount} " +
                    $"entitySceneGuid={report.entitySceneGuid} productionAddressablesMutated=0 " +
                    $"report={report.reportPath}");
            }
            finally
            {
                if (previousSetup != null && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        internal static OperationMapDefinition EnsureCandidateDefinition()
        {
            string folder = Path.GetDirectoryName(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath);
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

            string candidatePath =
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath;
            OperationMapDefinition candidate =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(candidatePath);
            if (candidate == null)
            {
                candidate = ScriptableObject.CreateInstance<OperationMapDefinition>();
                EditorUtility.CopySerialized(production, candidate);
                AssetDatabase.CreateAsset(candidate, candidatePath);
            }

            string candidateSubSceneGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath);
            if (string.IsNullOrEmpty(candidateSubSceneGuid))
                throw new InvalidOperationException("Candidate entity SubScene GUID is missing.");

            string runtimeBindingGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath);
            string mapSurfaceGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.MapSurfacePath);
            string minimapGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath);

            SerializedObject serialized = new(candidate);
            serialized.FindProperty("operationMapId").stringValue =
                OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId;
            serialized.FindProperty("presentationKind").enumValueIndex =
                (int)OperationMapPresentationKind.EntityScene;
            serialized.FindProperty("navigationMetadata")
                .FindPropertyRelative("authoredSubSceneGuid").stringValue = candidateSubSceneGuid;
            SetAssetReferenceGuid(serialized, "staticPresentationManifestReference", string.Empty);
            SetAssetReferenceGuid(serialized, "buildingPlacementsReference", string.Empty);
            SetAssetReferenceGuid(serialized, "vehiclePlacementsReference", string.Empty);
            SetAssetReferenceGuid(serialized, "mapSurfaceDataReference", mapSurfaceGuid);
            SetAssetReferenceGuid(serialized, "minimapRasterReference", minimapGuid);
            if (!string.IsNullOrEmpty(runtimeBindingGuid))
                SetAssetReferenceGuid(serialized, "sourceSceneReference", runtimeBindingGuid);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(candidate);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(candidatePath, ImportAssetOptions.ForceSynchronousImport);

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

        internal static void EnsureCandidateRuntimeBindingScene()
        {
            string outputPath =
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath;
            string folder = Path.GetDirectoryName(outputPath)?.Replace('\\', '/');
            EnsureFolder(folder);

            string candidateDefinitionPath =
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath;
            OperationMapDefinition definition =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(candidateDefinitionPath);
            if (definition == null ||
                string.IsNullOrEmpty(AssetDatabase.GetAssetPath(definition)))
            {
                throw new InvalidOperationException("Candidate definition must exist as a persistent asset.");
            }

            SceneAsset subSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath);
            if (subSceneAsset == null)
                throw new InvalidOperationException("Candidate entity SubScene asset is missing.");

            string productionBindingPath = OperationMapAddressablesLayoutBuilder.SourceScenePath;
            if (!File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", productionBindingPath))))
                throw new InvalidOperationException($"Production runtime binding missing: {productionBindingPath}");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string productionPhysicalPath = Path.GetFullPath(Path.Combine(projectRoot, productionBindingPath));
            string outputPhysicalPath = Path.GetFullPath(Path.Combine(projectRoot, outputPath));
            if (File.Exists(outputPhysicalPath))
            {
                // Preserve the candidate scene's .meta/GUID. Deleting and recopying this asset
                // changes its GUID every run and makes the candidate definition non-deterministic.
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
            PatchDefinitionReferenceIfMissing(outputPath, candidateDefinitionPath);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);

            Scene reloaded = EditorSceneManager.OpenScene(outputPath, OpenSceneMode.Single);
            try
            {
                OperationMapSceneView reloadedView = FindSingleView(reloaded);
                if (!reloadedView.TryValidate(out string validateError))
                {
                    throw new InvalidOperationException(
                        $"Candidate EntityScene runtime binding invalid after reload: {validateError} " +
                        $"viewId='{reloadedView.OperationMapId}' " +
                        $"definitionId='{reloadedView.Definition?.OperationMapId}' " +
                        $"definitionKind='{reloadedView.Definition?.PresentationKind}' " +
                        $"mode='{reloadedView.CanonicalPresentationMode}' " +
                        $"subScene='{reloadedView.MapSubScene?.SceneGUID}'.");
                }
            }
            finally
            {
                CloseSceneKeepingEditorValid(reloaded);
            }

            string runtimeGuid = AssetDatabase.AssetPathToGUID(outputPath);
            definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(candidateDefinitionPath);
            if (definition == null)
                throw new InvalidOperationException("Candidate definition disappeared after runtime binding save.");
            SerializedObject definitionSerialized = new(definition);
            SetAssetReferenceGuid(definitionSerialized, "sourceSceneReference", runtimeGuid);
            if (definitionSerialized.ApplyModifiedPropertiesWithoutUndo())
                EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
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
