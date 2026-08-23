using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapDenseCityProductionCutover
    {
        private const string RollbackReportPath =
            "Design/AgentReports/2026-08-09_dense_city_production_cutover_rollback_checkpoint.json";
        private const string StaticRoot =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/Authorize Dense City Production EntityScene Cutover")]
        public static void RunAuthorizedCutover()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] transactionPaths = CollectTransactionPaths(projectRoot).ToArray();
            OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction transaction =
                OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                    projectRoot,
                    transactionPaths);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                OperationMapDefinition production = RequireDefinition(
                    OperationMapAddressablesLayoutBuilder.DefinitionPath);
                OperationMapDefinition candidate = RequireDefinition(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateDefinitionPath);
                string candidateError = null;
                if (candidate.PresentationKind != OperationMapPresentationKind.EntityScene ||
                    candidate.RenderResidencyMode !=
                        OperationMapRenderResidencyMode.VirtualizedProxyPool ||
                    !candidate.TryValidateLocalContentReferences(out candidateError))
                {
                    throw new InvalidOperationException(
                        candidateError ?? "Accepted dense candidate definition is invalid.");
                }

                RollbackCheckpoint report = CaptureRollbackCheckpoint(projectRoot);
                ApplyProductionDefinition(production, candidate);
                ApplyProductionRuntimeBinding(production);
                ApplyProductionAddressables();
                AssetDatabase.SaveAssets();

                if (!production.TryValidateLocalContentReferences(out string definitionError))
                    throw new InvalidOperationException(definitionError);
                if (!OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(
                        true,
                        out string layoutError))
                {
                    throw new InvalidOperationException(layoutError);
                }
                ValidateProductionRuntimeBinding();

                report.productionCutover = 1;
                report.productionDefinitionSha256 = ComputeFileHash(
                    Path.Combine(
                        projectRoot,
                        OperationMapAddressablesLayoutBuilder.DefinitionPath));
                report.productionRuntimeBindingSha256 = ComputeFileHash(
                    Path.Combine(
                        projectRoot,
                        OperationMapAddressablesLayoutBuilder.SourceScenePath));
                report.result = "Passed";
                string reportPhysical = Path.Combine(projectRoot, RollbackReportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(reportPhysical) ?? projectRoot);
                File.WriteAllText(
                    reportPhysical,
                    JsonUtility.ToJson(report, true),
                    Utf8WithoutBom);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log(
                    "[OperationMapDenseCityProductionCutover] result=Passed " +
                    "presentationKind=EntityScene renderResidency=VirtualizedProxyPool " +
                    $"entitySceneGuid={report.entitySceneGuid} " +
                    $"staticRollbackFiles={report.staticRollbackFileCount} " +
                    $"staticRollbackBytes={report.staticRollbackBytes} productionCutover=1");
            }
            catch (Exception cutoverError)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rollbackError)
                {
                    throw new AggregateException(
                        "Production cutover failed and byte-exact rollback also failed.",
                        cutoverError,
                        rollbackError);
                }
                throw;
            }
            finally
            {
                if (previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        [MenuItem("Game/Operation Maps/Repair EntityScene Runtime Surface Overlays")]
        public static void RepairEntitySceneRuntimeSurfaceOverlays()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                MapSurfaceSceneOverlayAuthoringData[] acceptedOverlays =
                    OperationMapRuntimeBindingSceneBuilder.CaptureSurfaceSceneOverlays(
                        StaticMapPresentationBaker.CurrentStagedOperationMapScenePath);
                MapSurfaceSceneOverlayAuthoringData[] denseOverlays =
                    OperationMapRuntimeBindingSceneBuilder.CaptureSurfaceSceneOverlays(
                        DenseCityCandidateAuthoringTransaction.CandidateMapScenePath);

                ApplySurfaceOverlaysToRuntimeBinding(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
                    acceptedOverlays);
                ApplySurfaceOverlaysToRuntimeBinding(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath,
                    denseOverlays);
                ApplySurfaceOverlaysToRuntimeBinding(
                    OperationMapAddressablesLayoutBuilder.SourceScenePath,
                    denseOverlays);
                AssetDatabase.SaveAssets();

                ValidateEntityRuntimeBinding(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
                    OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath);
                ValidateEntityRuntimeBinding(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateRuntimeBindingPath,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.DenseCandidateDefinitionPath,
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
                ValidateEntityRuntimeBinding(
                    OperationMapAddressablesLayoutBuilder.SourceScenePath,
                    OperationMapAddressablesLayoutBuilder.DefinitionPath,
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);

                Debug.Log(
                    "[OperationMapEntitySceneSurfaceOverlayRepair] result=Passed " +
                    $"acceptedOverlays={acceptedOverlays.Length} denseOverlays={denseOverlays.Length}");
            }
            finally
            {
                if (previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static OperationMapDefinition RequireDefinition(string path)
        {
            OperationMapDefinition definition =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(path);
            return definition != null
                ? definition
                : throw new InvalidOperationException($"Operation-map definition is missing: {path}");
        }

        private static void ApplyProductionDefinition(
            OperationMapDefinition production,
            OperationMapDefinition candidate)
        {
            string runtimeBindingGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.SourceScenePath);
            string entitySceneGuid = AssetDatabase.AssetPathToGUID(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            if (string.IsNullOrEmpty(runtimeBindingGuid) || string.IsNullOrEmpty(entitySceneGuid))
                throw new InvalidOperationException("Production runtime or accepted EntityScene GUID is missing.");

            var serialized = new SerializedObject(production);
            serialized.FindProperty("presentationKind").enumValueIndex =
                (int)OperationMapPresentationKind.EntityScene;
            serialized.FindProperty("renderResidencyMode").enumValueIndex =
                (int)OperationMapRenderResidencyMode.VirtualizedProxyPool;
            serialized.FindProperty("navigationMetadata")
                .FindPropertyRelative("authoredSubSceneGuid").stringValue = entitySceneGuid;
            SetReference(serialized, "sourceSceneReference", runtimeBindingGuid);
            SetReference(serialized, "staticPresentationManifestReference", string.Empty);
            SetReference(serialized, "buildingPlacementsReference", string.Empty);
            SetReference(serialized, "vehiclePlacementsReference", string.Empty);
            SetReference(
                serialized,
                "mapSurfaceDataReference",
                candidate.MapSurfaceDataReference.AssetGUID);
            SetReference(
                serialized,
                "minimapRasterReference",
                candidate.MinimapRasterReference.AssetGUID);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(production);
            AssetDatabase.SaveAssetIfDirty(production);
        }

        private static void ApplyProductionRuntimeBinding(OperationMapDefinition production)
        {
            SceneAsset entityScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath);
            if (entityScene == null)
                throw new InvalidOperationException("Accepted dense EntityScene asset is missing.");
            MapSurfaceSceneOverlayAuthoringData[] surfaceOverlays =
                OperationMapRuntimeBindingSceneBuilder.CaptureSurfaceSceneOverlays(
                    DenseCityCandidateAuthoringTransaction.CandidateMapScenePath);

            Scene scene = EditorSceneManager.OpenScene(
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OpenSceneMode.Single);
            try
            {
                OperationMapSceneView view = FindSingleView(scene);
                if (view.MapSubScene == null)
                    throw new InvalidOperationException("Production runtime binding SubScene is missing.");

                view.MapSubScene.SceneAsset = entityScene;
                view.MapSubScene.AutoLoadScene = true;
                EditorUtility.SetDirty(view.MapSubScene);

                var viewData = new SerializedObject(view);
                viewData.FindProperty("definition").objectReferenceValue = production;
                viewData.FindProperty("buildingPlacements").objectReferenceValue = null;
                viewData.FindProperty("vehiclePlacements").objectReferenceValue = null;
                viewData.FindProperty("canonicalPresentationMode").enumValueIndex =
                    (int)OperationMapCanonicalPresentationMode.EntityScene;
                viewData.FindProperty("presentationSourceSceneGuid").stringValue = string.Empty;
                viewData.FindProperty("presentationSourceScenePath").stringValue = string.Empty;
                viewData.ApplyModifiedPropertiesWithoutUndo();
                OperationMapRuntimeBindingSceneBuilder.ApplySurfaceSceneOverlays(
                    view.MapSurfaceAuthoring,
                    surfaceOverlays);
                EditorUtility.SetDirty(view);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(
                        scene,
                        OperationMapAddressablesLayoutBuilder.SourceScenePath,
                        false))
                {
                    throw new InvalidOperationException("Failed to save production runtime binding cutover.");
                }
            }
            finally
            {
                CloseSceneKeepingEditorValid(scene);
            }
        }

        private static void ApplyProductionAddressables()
        {
            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
                throw new InvalidOperationException("Addressables settings are required.");

            AddressableAssetGroup core = settings.FindGroup(
                OperationMapAddressablesLayoutBuilder.CoreGroupName);
            AddressableAssetGroup shared = settings.FindGroup(
                OperationMapAddressablesLayoutBuilder.SharedGroupName);
            AddressableAssetGroup presentation = settings.FindGroup(
                OperationMapAddressablesLayoutBuilder.PresentationGroupName);
            if (core == null || shared == null || presentation == null)
                throw new InvalidOperationException("Production operation-map groups are incomplete.");

            RemoveAllEntries(settings, shared);
            RemoveAllEntries(settings, presentation);
            RemoveEntry(settings, OperationMapAddressablesLayoutBuilder.ManifestPath);
            RemoveEntry(settings, OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath);
            RemoveEntry(settings, OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath);
            RemoveEntry(settings, OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);

            string entityScenePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string entitySceneGuid = AssetDatabase.AssetPathToGUID(entityScenePath);
            AddressableAssetEntry entry = settings.FindAssetEntry(entitySceneGuid);
            if (entry == null || entry.parentGroup != core)
                entry = settings.CreateOrMoveEntry(entitySceneGuid, core, false, false);
            entry.SetAddress(
                OperationMapAddressablesLayoutBuilder.AddressPrefix + "entity-scene",
                false);
            SetProductionLabels(
                settings,
                entry,
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner.EntitySceneRoleLabel);

            settings.SetDirty(
                AddressableAssetSettings.ModificationEvent.BatchModification,
                null,
                true,
                true);
            AssetDatabase.SaveAssets();
        }

        private static void RemoveAllEntries(
            AddressableAssetSettings settings,
            AddressableAssetGroup group)
        {
            foreach (AddressableAssetEntry entry in group.entries.ToArray())
                settings.RemoveAssetEntry(entry.guid, false);
        }

        private static void RemoveEntry(AddressableAssetSettings settings, string assetPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrEmpty(guid) && settings.FindAssetEntry(guid) != null)
                settings.RemoveAssetEntry(guid, false);
        }

        private static void SetProductionLabels(
            AddressableAssetSettings settings,
            AddressableAssetEntry entry,
            string roleLabel)
        {
            foreach (string label in entry.labels.ToArray())
            {
                if (label.StartsWith("operation-map", StringComparison.Ordinal))
                    entry.SetLabel(label, false, false, false);
            }

            foreach (string label in new[]
                     {
                         OperationMapAddressablesLayoutBuilder.OperationMapLabel,
                         OperationMapAddressablesLayoutBuilder.LocalLabel,
                         OperationMapAddressablesLayoutBuilder.PackLabel,
                         roleLabel
                     })
            {
                settings.AddLabel(label, false);
                entry.SetLabel(label, true, false, false);
            }
        }

        private static void ValidateProductionRuntimeBinding()
        {
            Scene scene = EditorSceneManager.OpenScene(
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OpenSceneMode.Single);
            try
            {
                if (!OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                        scene,
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        OperationMapAddressablesLayoutBuilder.DefinitionPath,
                        DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                        out string error))
                {
                    throw new InvalidOperationException(error);
                }
            }
            finally
            {
                CloseSceneKeepingEditorValid(scene);
            }
        }

        private static void ApplySurfaceOverlaysToRuntimeBinding(
            string runtimeBindingPath,
            MapSurfaceSceneOverlayAuthoringData[] overlays)
        {
            Scene scene = EditorSceneManager.OpenScene(runtimeBindingPath, OpenSceneMode.Single);
            try
            {
                OperationMapSceneView view = FindSingleView(scene);
                OperationMapRuntimeBindingSceneBuilder.ApplySurfaceSceneOverlays(
                    view.MapSurfaceAuthoring,
                    overlays);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, runtimeBindingPath, false))
                {
                    throw new InvalidOperationException(
                        $"Failed to save runtime surface overlays: {runtimeBindingPath}");
                }
            }
            finally
            {
                CloseSceneKeepingEditorValid(scene);
            }
        }

        private static void ValidateEntityRuntimeBinding(
            string runtimeBindingPath,
            string definitionPath,
            string subScenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(runtimeBindingPath, OpenSceneMode.Single);
            try
            {
                if (!OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                        scene,
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        definitionPath,
                        subScenePath,
                        out string error))
                {
                    throw new InvalidOperationException(error);
                }
            }
            finally
            {
                CloseSceneKeepingEditorValid(scene);
            }
        }

        private static OperationMapSceneView FindSingleView(Scene scene)
        {
            OperationMapSceneView found = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (OperationMapSceneView view in
                         root.GetComponentsInChildren<OperationMapSceneView>(true))
                {
                    if (found != null)
                        throw new InvalidOperationException("Runtime binding contains multiple views.");
                    found = view;
                }
            }
            return found ?? throw new InvalidOperationException("Runtime binding view is missing.");
        }

        private static void CloseSceneKeepingEditorValid(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;
            if (SceneManager.sceneCount == 1)
            {
                Scene transition = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                SceneManager.SetActiveScene(transition);
            }
            if (!EditorSceneManager.CloseScene(scene, true))
                throw new InvalidOperationException($"Failed to close scene '{scene.path}'.");
        }

        private static void SetReference(
            SerializedObject serialized,
            string propertyName,
            string guid)
        {
            SerializedProperty assetGuid = serialized.FindProperty(propertyName)
                ?.FindPropertyRelative("m_AssetGUID");
            if (assetGuid == null)
                throw new InvalidOperationException($"Definition field is missing: {propertyName}");
            assetGuid.stringValue = guid ?? string.Empty;
        }

        private static IEnumerable<string> CollectTransactionPaths(string projectRoot)
        {
            yield return OperationMapAddressablesLayoutBuilder.DefinitionPath;
            yield return OperationMapAddressablesLayoutBuilder.SourceScenePath;
            yield return RollbackReportPath;
            string addressablesRoot = Path.Combine(projectRoot, "Assets/AddressableAssetsData");
            foreach (string file in Directory.GetFiles(
                         addressablesRoot,
                         "*",
                         SearchOption.AllDirectories))
            {
                yield return Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
            }
        }

        private static RollbackCheckpoint CaptureRollbackCheckpoint(string projectRoot)
        {
            string staticPhysical = Path.Combine(projectRoot, StaticRoot);
            string[] staticFiles = Directory.GetFiles(
                    staticPhysical,
                    "*",
                    SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            return new RollbackCheckpoint
            {
                schemaVersion = 1,
                result = "Pending",
                sourceRevision = AndroidBuildReportGenerator.CaptureGitProvenance().ExactCommit,
                rollbackDefinitionSha256 = ComputeFileHash(Path.Combine(
                    projectRoot,
                    OperationMapAddressablesLayoutBuilder.DefinitionPath)),
                rollbackRuntimeBindingSha256 = ComputeFileHash(Path.Combine(
                    projectRoot,
                    OperationMapAddressablesLayoutBuilder.SourceScenePath)),
                rollbackAddressablesSha256 = ComputeDirectoryHash(Path.Combine(
                    projectRoot,
                    "Assets/AddressableAssetsData")),
                staticRollbackSha256 = ComputeDirectoryHash(staticPhysical),
                staticRollbackFileCount = staticFiles.Length,
                staticRollbackSceneCount = staticFiles.Count(path =>
                    path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)),
                staticRollbackBytes = staticFiles.Sum(path => new FileInfo(path).Length),
                entitySceneGuid = AssetDatabase.AssetPathToGUID(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath)
            };
        }

        private static string ComputeDirectoryHash(string directory)
        {
            var builder = new StringBuilder();
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                builder.Append(Path.GetRelativePath(directory, file).Replace('\\', '/'));
                builder.Append(':').Append(new FileInfo(file).Length).Append(':');
                builder.Append(ComputeFileHash(file)).Append('\n');
            }
            using SHA256 sha = SHA256.Create();
            return ToHex(sha.ComputeHash(Utf8WithoutBom.GetBytes(builder.ToString())));
        }

        private static string ComputeFileHash(string path)
        {
            using Stream stream = File.OpenRead(path);
            using SHA256 sha = SHA256.Create();
            return ToHex(sha.ComputeHash(stream));
        }

        private static string ToHex(byte[] bytes) =>
            string.Concat(bytes.Select(value => value.ToString("x2")));

        [Serializable]
        private sealed class RollbackCheckpoint
        {
            public int schemaVersion;
            public string result;
            public string sourceRevision;
            public string rollbackDefinitionSha256;
            public string rollbackRuntimeBindingSha256;
            public string rollbackAddressablesSha256;
            public string staticRollbackSha256;
            public int staticRollbackFileCount;
            public int staticRollbackSceneCount;
            public long staticRollbackBytes;
            public string entitySceneGuid;
            public string productionDefinitionSha256;
            public string productionRuntimeBindingSha256;
            public int productionCutover;
        }
    }
}
