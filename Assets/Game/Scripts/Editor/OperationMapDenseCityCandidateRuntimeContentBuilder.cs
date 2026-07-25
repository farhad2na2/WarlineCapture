#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Configs;
    using Unity.Entities;
    using Unity.Entities.Build;
    using Unity.Entities.Content;
    using Unity.Scenes.Editor;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Build;
    using UnityEditor.AddressableAssets.Build.Layout;
    using UnityEditor.AddressableAssets.Build.DataBuilders;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;
    using UnityEngine;
    using Hash128 = Unity.Entities.Hash128;

    /// <summary>
    /// Builds dense candidate-only macOS Addressables and Entities content without using or
    /// persisting production Addressables settings.
    /// </summary>
    internal static class OperationMapDenseCityCandidateRuntimeContentBuilder
    {
        internal const string ReportPath =
            "Design/AgentReports/2026-07-24_dense_city_candidate_runtime_content.json";
        internal const string FrozenRollbackReportPath =
            "Design/AgentReports/2026-07-25_dense_city_frozen_rollback_byte_inventory.json";
        internal const string SourceHierarchyExclusionReportPath =
            "Design/AgentReports/2026-07-25_dense_city_source_hierarchy_exclusion.json";
        internal const string AddressablesOutputPath =
            "Library/OperationMapDenseCityRuntimeContent/Addressables";
        internal const string BuildLayoutOutputPath =
            "Library/OperationMapDenseCityRuntimeContent/BuildLayout";
        internal const string AddressablesBuildLayoutPath =
            BuildLayoutOutputPath + "/buildlayout.json";
        internal const string EntityContentOutputPath =
            "Library/OperationMapDenseCityRuntimeContent/Entities";
        internal const string FrozenRollbackRootPath =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/" +
            "desert_base_01";

        private const string SharedAddressablesOutputPath =
            "Library/com.unity.addressables/aa/OSX";
        private const string TransientSettingsFolder =
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/" +
            "opmap.skirmish.desert_base_01/Candidates/DenseCityRuntimeContentBuildTemp";
        private const string GroupName =
            "Operation Map - Validation Only - skirmish-desert-base-01 - Dense City EntityScene";
        private const long MinimumFreeDiskBytes = 15L * 1024L * 1024L * 1024L;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem(
            "Game/Operation Maps/EntityScene Migration/Build Dense City Candidate Runtime Parity Content")]
        public static void BuildDenseCityCandidateRuntimeParityContent()
        {
            RequireMacOsBuildTarget();
            OperationMapEntitySceneCandidateAddressablesLayoutBuilder
                .BuildDenseCityCandidateEntitySceneAddressablesLayout();
            if (!OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreateDenseCityPlan(
                    out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
                    out string planError))
            {
                throw new InvalidOperationException(
                    $"Dense candidate runtime-content plan rejected: {planError}");
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            RequireFreeDiskSpace(projectRoot);
            OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot protectedSnapshot =
                OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot.Capture(
                    projectRoot,
                    new[]
                    {
                        "Assets/AddressableAssetsData/AddressableAssetSettings.asset",
                        "Assets/AddressableAssetsData/AddressableAssetSettings.asset.meta",
                        OperationMapEntitySceneCandidateRuntimeContentBuilder.ReportPath
                    },
                    new[]
                    {
                        "Assets/AddressableAssetsData",
                        "Library/OperationMapCandidateRuntimeContent",
                        SharedAddressablesOutputPath
                    });
            OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction reportTransaction =
                OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                    projectRoot,
                    new[] { ReportPath });
            OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction
                productionSettingsTransaction =
                    OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                        projectRoot,
                        new[]
                        {
                            "Assets/AddressableAssetsData/AddressableAssetSettings.asset"
                        });
            using var outputTransaction = DenseRuntimeContentOutputTransaction.Begin(
                projectRoot,
                SharedAddressablesOutputPath,
                AddressablesOutputPath,
                EntityContentOutputPath,
                BuildLayoutOutputPath);

            try
            {
                AddressablesContentBuildResult addressablesResult =
                    BuildIsolatedAddressables(plan, outputTransaction);
                productionSettingsTransaction.Rollback();
                EntityContentBuildResult entityContentResult = BuildEntityContent(plan);
                FrozenRollbackContentResult frozenRollbackResult =
                    MeasureFrozenRollbackContent(projectRoot);
                RuntimeContentReport report = CreateReport(
                    projectRoot,
                    plan,
                    addressablesResult,
                    entityContentResult,
                    frozenRollbackResult);
                WriteReport(projectRoot, report);
                protectedSnapshot.RequireUnchanged();
                outputTransaction.Commit();
                Debug.Log(
                    $"[OperationMapDenseCityRuntimeContent] result=Passed " +
                    $"entitySceneGuid={plan.EntitySceneGuid} " +
                    $"addressablesBundles={report.addressablesBundleCount} " +
                    $"addressablesBytes={report.addressablesBytes} " +
                    $"sharedDependencyBytes={report.sharedDependencyBytes} " +
                    $"duplicatedDependencyBytes={report.duplicatedDependencyBytes} " +
                    $"entityArchives={report.entityContentArchiveCount} " +
                    $"entitySceneArchiveBytes={report.entitySceneArchiveBytes} " +
                    $"entityMetadataBytes={report.entityContentMetadataBytes} " +
                    $"entityContentBytes={report.entityContentBytes} productionCutover=0 " +
                    $"frozenRollbackChunks={report.frozenRollbackChunkCount} " +
                    $"frozenRollbackChunkBytes={report.frozenRollbackChunkBytes} " +
                    "productionSettingsMutated=0 sharedOutputRestored=1");
            }
            catch
            {
                productionSettingsTransaction.Rollback();
                reportTransaction.Rollback();
                throw;
            }
        }

        [MenuItem(
            "Game/Operation Maps/EntityScene Migration/Report Dense City Frozen Rollback Bytes")]
        public static void ReportDenseCityFrozenRollbackBytes()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            FrozenRollbackContentResult result = MeasureFrozenRollbackContent(projectRoot);
            ProductionStaticAddressablesResult production =
                MeasureCurrentProductionStaticAddressables();
            var report = new FrozenRollbackByteInventoryReport
            {
                schema = "warline.operation-map.dense-city-frozen-rollback-byte-inventory",
                schemaVersion = 2,
                result = "DenseCityFrozenRollbackByteInventoryPassed",
                operationMapId = OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                manifestPath = FrozenRollbackRootPath + "/StaticMapPresentationManifest.asset",
                manifestBytes = result.ManifestBytes,
                chunkDirectoryPath = FrozenRollbackRootPath + "/Scenes",
                chunkCount = result.ChunkCount,
                chunkBytes = result.ChunkBytes,
                productionPresentationKind = production.PresentationKind,
                productionStaticManifestEntryCount = production.ManifestEntryCount,
                productionPresentationChunkEntryCount = production.ChunkEntryCount,
                requiredStaticManifestEntryCountAfterCutover = 0,
                requiredPresentationChunkEntryCountAfterCutover = 0,
                postCutoverZeroCountsSatisfied = production.ZeroCountsSatisfied ? 1 : 0,
                productionCutover = 0
            };
            WriteJsonReport(projectRoot, FrozenRollbackReportPath, report);
            Debug.Log(
                "[DenseCityFrozenRollbackByteInventory] result=Passed " +
                $"manifestBytes={report.manifestBytes} chunks={report.chunkCount} " +
                $"chunkBytes={report.chunkBytes} " +
                $"productionKind={report.productionPresentationKind} " +
                $"productionStaticManifestEntries={report.productionStaticManifestEntryCount} " +
                $"productionChunkEntries={report.productionPresentationChunkEntryCount} " +
                $"postCutoverZeroCountsSatisfied={report.postCutoverZeroCountsSatisfied} " +
                "productionCutover=0");
        }

        [MenuItem(
            "Game/Operation Maps/EntityScene Migration/Report Dense City Source Hierarchy Exclusion")]
        public static void ReportDenseCitySourceHierarchyExclusion()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string[] explicitAddressablePaths =
            {
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                    .DenseCandidateDefinitionPath,
                OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                    .DenseCandidateRuntimeBindingPath
            };
            string[] enabledBuildScenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            SourceHierarchyExclusionResult result = MeasureSourceHierarchyExclusion(
                explicitAddressablePaths,
                enabledBuildScenePaths);
            RequireSourceHierarchyExclusion(result, expectedExplicitEntryCount: 2);
            var report = new SourceHierarchyExclusionReport
            {
                schema = "warline.operation-map.dense-city-source-hierarchy-exclusion",
                schemaVersion = 1,
                result = "DenseCitySourceHierarchyExplicitAndBuildSceneExclusionPassed",
                operationMapId =
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                explicitAddressableEntryCount = result.ExplicitAddressableEntryCount,
                enabledPlayerBuildSceneCount = result.EnabledPlayerBuildSceneCount,
                sourceHierarchyExplicitAddressableEntryCount =
                    result.SourceHierarchyExplicitAddressableEntryCount,
                sourceHierarchyPlayerBuildSceneCount =
                    result.SourceHierarchyPlayerBuildSceneCount,
                packedImplicitDependencyEvidenceComplete = 0,
                productionCutover = 0
            };
            WriteJsonReport(projectRoot, SourceHierarchyExclusionReportPath, report);
            Debug.Log(
                "[DenseCitySourceHierarchyExclusion] result=Passed " +
                $"explicitEntries={report.explicitAddressableEntryCount} " +
                $"enabledBuildScenes={report.enabledPlayerBuildSceneCount} " +
                $"sourceExplicitEntries={report.sourceHierarchyExplicitAddressableEntryCount} " +
                $"sourceBuildScenes={report.sourceHierarchyPlayerBuildSceneCount} " +
                "packedImplicitEvidence=0 productionCutover=0");
        }

        public static void ReportDenseCitySourceHierarchyExclusionBatch() =>
            ReportDenseCitySourceHierarchyExclusion();

        private static AddressablesContentBuildResult BuildIsolatedAddressables(
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
            DenseRuntimeContentOutputTransaction outputTransaction)
        {
            AddressableAssetSettings settings = null;
            BuildScriptPackedMode builder = null;
            using var buildLayoutCapture = BuildLayoutCaptureScope.Begin();
            try
            {
                DeleteTransientSettings();
                settings = AddressableAssetSettings.Create(
                    TransientSettingsFolder,
                    "DenseCityCandidateAddressableAssetSettings",
                    false,
                    true);
                if (settings == null)
                    throw new InvalidOperationException("Failed to create temporary Addressables settings.");
                string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                string temporaryBundleBuildPath = Path.GetFullPath(Path.Combine(
                        projectRoot,
                        SharedAddressablesOutputPath,
                        BuildTarget.StandaloneOSX.ToString()))
                    .Replace('\\', '/');
                string denseBundleLoadPath = Path.GetFullPath(Path.Combine(
                        projectRoot,
                        AddressablesOutputPath,
                        BuildTarget.StandaloneOSX.ToString()))
                    .Replace('\\', '/');
                settings.profileSettings.SetValue(
                    settings.activeProfileId,
                    AddressableAssetSettings.kLocalBuildPath,
                    temporaryBundleBuildPath);
                settings.profileSettings.SetValue(
                    settings.activeProfileId,
                    AddressableAssetSettings.kLocalLoadPath,
                    denseBundleLoadPath);

                AddressableAssetGroup group = settings.CreateGroup(
                    GroupName,
                    true,
                    false,
                    false,
                    null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));
                if (settings.DefaultGroup != group)
                {
                    throw new InvalidOperationException(
                        "Temporary dense Addressables group is not the default shared-bundle owner.");
                }
                BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    throw new InvalidOperationException("Dense candidate group has no bundled schema.");

                settings.BuildRemoteCatalog = false;
                settings.DisableCatalogUpdateOnStartup = true;
                schema.BuildPath.SetVariableByName(
                    settings,
                    AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(
                    settings,
                    AddressableAssetSettings.kLocalLoadPath);
                schema.UseDefaultSchemaSettings = false;
                schema.IncludeInBuild = true;
                schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                schema.UseAssetBundleCrc = true;
                schema.UseAssetBundleCrcForCachedBundles = true;
                schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                RequireLocalBundleDelivery(
                    MeasureLocalBundleDelivery(
                        settings.BuildRemoteCatalog,
                        settings.DisableCatalogUpdateOnStartup,
                        schema.IncludeInBuild,
                        schema.BuildPath.GetName(settings),
                        schema.LoadPath.GetName(settings),
                        schema.LoadPath.GetValue(settings, false)));

                AddEntry(
                    settings,
                    group,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateDefinitionPath,
                    plan.AddressPrefix + "definition");
                AddEntry(
                    settings,
                    group,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateRuntimeBindingPath,
                    plan.AddressPrefix + "source-scene");
                if (settings.groups.Count(groupAsset => groupAsset != null) != 1 ||
                    group.entries.Count != 2)
                {
                    throw new InvalidOperationException(
                        "Temporary dense Addressables settings contain unexpected groups or entries.");
                }
                SourceHierarchyExclusionResult sourceHierarchyExclusion =
                    MeasureSourceHierarchyExclusion(
                        group.entries.Select(entry => AssetDatabase.GUIDToAssetPath(entry.guid)),
                        EditorBuildSettings.scenes
                            .Where(scene => scene.enabled)
                            .Select(scene => scene.path));
                RequireSourceHierarchyExclusion(
                    sourceHierarchyExclusion,
                    expectedExplicitEntryCount: 2);

                outputTransaction.PrepareSharedOutputForBuild();
                builder = ScriptableObject.CreateInstance<BuildScriptPackedMode>();
                AssetDatabase.CreateAsset(
                    builder,
                    TransientSettingsFolder + "/DenseCityCandidatePackedBuilder.asset");
                if (!settings.AddDataBuilder(builder, false))
                    throw new InvalidOperationException("Failed to register the temporary packed builder.");
                settings.ActivePlayerDataBuilderIndex = 0;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                var input = new AddressablesDataBuilderInput(settings)
                {
                    RuntimeCatalogFilename = "catalog",
                    RuntimeSettingsFilename = "settings.json"
                };
                AddressablesPlayerBuildResult result =
                    builder.BuildData<AddressablesPlayerBuildResult>(input);
                if (result == null || !string.IsNullOrEmpty(result.Error))
                {
                    throw new InvalidOperationException(
                        result?.Error ?? "Dense Addressables content build returned no result.");
                }

                string sourceBuildLayoutPath =
                    buildLayoutCapture.RequireSingleGeneratedBuildLayoutPath();
                BuildLayout buildLayout = BuildLayout.Open(
                    sourceBuildLayoutPath,
                    readFullFile: true);
                if (buildLayout == null)
                    throw new InvalidOperationException(
                        $"Dense Addressables Build Layout could not be opened: {sourceBuildLayoutPath}");
                string[] sharedDependencyGuids = plan.Entries
                    .Where(entry =>
                        string.Equals(
                            entry.Role,
                            "shared-dependency",
                            StringComparison.Ordinal))
                    .Select(entry => AssetDatabase.AssetPathToGUID(entry.AssetPath))
                    .ToArray();
                PackedDependencyByteResult packedDependencyBytes =
                    MeasurePackedDependencyBytes(buildLayout, sharedDependencyGuids);
                RequireNoPackedDependencyDuplication(packedDependencyBytes);
                if (packedDependencyBytes.SharedDependencyGuidCount !=
                    plan.SharedDependencyCount)
                {
                    throw new InvalidOperationException(
                        "Dense Addressables Build Layout shared-dependency count drifted: " +
                        $"planned={plan.SharedDependencyCount}, " +
                        $"packed={packedDependencyBytes.SharedDependencyGuidCount}");
                }
                PackedSourceHierarchyResult packedSourceHierarchy =
                    MeasurePackedSourceHierarchy(buildLayout);
                RequirePackedSourceHierarchyExclusion(packedSourceHierarchy);

                outputTransaction.PublishBuiltAddressables();
                string publishedAddressablesPath = Path.GetFullPath(
                    Path.Combine(projectRoot, AddressablesOutputPath));
                PublishedLocalContentResult publishedLocalContent =
                    MeasurePublishedLocalContent(
                        publishedAddressablesPath,
                        Path.Combine(publishedAddressablesPath, "catalog.bin"));
                string publishedBuildLayoutPath = Path.GetFullPath(
                    Path.Combine(projectRoot, AddressablesBuildLayoutPath));
                Directory.CreateDirectory(
                    Path.GetDirectoryName(publishedBuildLayoutPath) ??
                    throw new InvalidOperationException(
                        "Dense Addressables Build Layout path has no parent."));
                File.Copy(sourceBuildLayoutPath, publishedBuildLayoutPath, true);
                return new AddressablesContentBuildResult(
                    result,
                    publishedBuildLayoutPath,
                    packedDependencyBytes,
                    packedSourceHierarchy,
                    publishedLocalContent);
            }
            finally
            {
                DeleteTransientSettings();
            }
        }

        private static void DeleteTransientSettings()
        {
            if (AssetDatabase.IsValidFolder(TransientSettingsFolder))
                AssetDatabase.DeleteAsset(TransientSettingsFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void AddEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Dense candidate runtime asset is missing: {assetPath}");
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.SetAddress(address, false);
        }

        internal static SourceHierarchyExclusionResult MeasureSourceHierarchyExclusion(
            IEnumerable<string> explicitAddressablePaths,
            IEnumerable<string> enabledPlayerBuildScenePaths)
        {
            if (explicitAddressablePaths == null)
                throw new ArgumentNullException(nameof(explicitAddressablePaths));
            if (enabledPlayerBuildScenePaths == null)
                throw new ArgumentNullException(nameof(enabledPlayerBuildScenePaths));

            string[] explicitPaths = explicitAddressablePaths
                .Select(NormalizeAssetPath)
                .ToArray();
            string[] buildScenePaths = enabledPlayerBuildScenePaths
                .Select(NormalizeAssetPath)
                .ToArray();
            var forbidden = new HashSet<string>(
                DenseSourceHierarchyPaths,
                StringComparer.Ordinal);
            return new SourceHierarchyExclusionResult(
                explicitPaths.Length,
                buildScenePaths.Length,
                explicitPaths.Count(forbidden.Contains),
                buildScenePaths.Count(forbidden.Contains));
        }

        internal static void RequireSourceHierarchyExclusion(
            SourceHierarchyExclusionResult result,
            int expectedExplicitEntryCount)
        {
            if (result.ExplicitAddressableEntryCount != expectedExplicitEntryCount ||
                result.SourceHierarchyExplicitAddressableEntryCount != 0 ||
                result.SourceHierarchyPlayerBuildSceneCount != 0)
            {
                throw new InvalidOperationException(
                    "Dense source hierarchy exclusion failed: " +
                    $"explicit={result.ExplicitAddressableEntryCount}/" +
                    $"{result.SourceHierarchyExplicitAddressableEntryCount}, " +
                    $"buildScenes={result.EnabledPlayerBuildSceneCount}/" +
                    $"{result.SourceHierarchyPlayerBuildSceneCount}.");
            }
        }

        private static string NormalizeAssetPath(string path) =>
            (path ?? string.Empty).Replace('\\', '/').Trim();

        private static string[] DenseSourceHierarchyPaths =>
            new[]
            {
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath,
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                DenseCityCandidateAuthoringTransaction.CandidateMapScenePath,
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath
            }.Select(NormalizeAssetPath).ToArray();

        internal static LocalBundleDeliveryResult MeasureLocalBundleDelivery(
            bool buildRemoteCatalog,
            bool disableCatalogUpdateOnStartup,
            bool includeInBuild,
            string buildPathVariableName,
            string loadPathVariableName,
            string loadPathValue)
        {
            string normalizedLoadPath = (loadPathValue ?? string.Empty).Trim();
            bool networkLoadPath =
                Uri.TryCreate(normalizedLoadPath, UriKind.Absolute, out Uri uri) &&
                !uri.IsFile;
            return new LocalBundleDeliveryResult(
                buildRemoteCatalog,
                disableCatalogUpdateOnStartup,
                includeInBuild,
                string.Equals(
                    buildPathVariableName,
                    AddressableAssetSettings.kLocalBuildPath,
                    StringComparison.Ordinal),
                string.Equals(
                    loadPathVariableName,
                    AddressableAssetSettings.kLocalLoadPath,
                    StringComparison.Ordinal),
                networkLoadPath);
        }

        internal static void RequireLocalBundleDelivery(LocalBundleDeliveryResult result)
        {
            if (result.BuildRemoteCatalog ||
                !result.DisableCatalogUpdateOnStartup ||
                !result.IncludeInBuild ||
                !result.UsesLocalBuildPath ||
                !result.UsesLocalLoadPath ||
                result.NetworkLoadPath)
            {
                throw new InvalidOperationException(
                    "Dense local bundle delivery failed: " +
                    $"remoteCatalog={(result.BuildRemoteCatalog ? 1 : 0)} " +
                    $"startupUpdatesDisabled={(result.DisableCatalogUpdateOnStartup ? 1 : 0)} " +
                    $"includeInBuild={(result.IncludeInBuild ? 1 : 0)} " +
                    $"localBuildPath={(result.UsesLocalBuildPath ? 1 : 0)} " +
                    $"localLoadPath={(result.UsesLocalLoadPath ? 1 : 0)} " +
                    $"networkLoadPath={(result.NetworkLoadPath ? 1 : 0)}.");
            }
        }

        internal static PublishedLocalContentResult MeasurePublishedLocalContent(
            string outputPath,
            string catalogPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath) || !Directory.Exists(outputPath))
                throw new InvalidOperationException(
                    $"Dense published Addressables output is missing: {outputPath}");
            string fullOutputPath = Path.GetFullPath(outputPath);
            string fullCatalogPath = string.IsNullOrWhiteSpace(catalogPath)
                ? string.Empty
                : Path.GetFullPath(catalogPath);
            string outputPrefix =
                fullOutputPath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            if (string.IsNullOrEmpty(fullCatalogPath) ||
                !fullCatalogPath.StartsWith(
                    outputPrefix,
                    StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(fullCatalogPath))
            {
                throw new InvalidOperationException(
                    $"Dense published local catalog is missing or outside its output: {catalogPath}");
            }

            long catalogBytes = new FileInfo(fullCatalogPath).Length;
            string[] bundlePaths = Directory
                .EnumerateFiles(fullOutputPath, "*.bundle", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            long bundleBytes = bundlePaths.Sum(path => new FileInfo(path).Length);
            if (catalogBytes <= 0 || bundlePaths.Length <= 0 || bundleBytes <= 0)
            {
                throw new InvalidOperationException(
                    "Dense published local content is empty: " +
                    $"catalogBytes={catalogBytes}, bundles={bundlePaths.Length}, " +
                    $"bundleBytes={bundleBytes}.");
            }
            var bundleManifest = new StringBuilder();
            for (int index = 0; index < bundlePaths.Length; index++)
            {
                string relativePath = Path
                    .GetRelativePath(fullOutputPath, bundlePaths[index])
                    .Replace('\\', '/');
                long bytes = new FileInfo(bundlePaths[index]).Length;
                string sha256 = ComputeSha256(bundlePaths[index]);
                bundleManifest
                    .Append(relativePath.Length)
                    .Append(':')
                    .Append(relativePath)
                    .Append('\n')
                    .Append(bytes)
                    .Append('\n')
                    .Append(sha256)
                    .Append('\n');
            }
            string bundleSetSha256;
            using (SHA256 algorithm = SHA256.Create())
            {
                bundleSetSha256 = string.Concat(
                    algorithm
                        .ComputeHash(Utf8WithoutBom.GetBytes(bundleManifest.ToString()))
                        .Select(value => value.ToString("x2")));
            }

            return new PublishedLocalContentResult(
                fullOutputPath,
                fullCatalogPath,
                catalogBytes,
                bundlePaths.Length,
                bundleBytes,
                bundleSetSha256);
        }

        private static EntityContentBuildResult BuildEntityContent(
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan)
        {
            var sceneGuid = new Hash128(plan.EntitySceneGuid);
            if (!sceneGuid.IsValid)
                throw new InvalidOperationException(
                    $"Dense candidate EntityScene GUID is invalid: {plan.EntitySceneGuid}");

            Hash128 playerGuid = DotsGlobalSettings.Instance.GetClientGUID();
            if (!playerGuid.IsValid)
                throw new InvalidOperationException("Entities client player GUID is invalid.");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputPath = Path.GetFullPath(Path.Combine(projectRoot, EntityContentOutputPath));
            Directory.CreateDirectory(outputPath);
            RemoteContentCatalogBuildUtility.BuildContent(
                new HashSet<Hash128> { sceneGuid },
                playerGuid,
                BuildTarget.StandaloneOSX,
                outputPath);

            string catalogPath = Path.Combine(outputPath, RuntimeContentManager.RelativeCatalogPath);
            if (!File.Exists(catalogPath))
                throw new InvalidOperationException(
                    $"Dense candidate Entities catalog was not produced: {catalogPath}");
            EntityContentBuildResult result = MeasureEntityContent(outputPath, catalogPath);
            if (result.ArchiveCount != 1)
                throw new InvalidOperationException(
                    "Dense candidate Entities content must contain exactly one EntityScene " +
                    $"archive, but found {result.ArchiveCount}: {outputPath}");
            return result;
        }

        internal static EntityContentBuildResult MeasureEntityContent(
            string outputPath,
            string catalogPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath) || !Directory.Exists(outputPath))
                throw new InvalidOperationException(
                    $"Dense candidate Entities content directory is missing: {outputPath}");
            if (string.IsNullOrWhiteSpace(catalogPath) || !File.Exists(catalogPath))
                throw new InvalidOperationException(
                    $"Dense candidate Entities catalog is missing: {catalogPath}");

            string[] archivePaths = Directory
                .EnumerateFiles(outputPath, "*.archive", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            long archiveBytes = archivePaths.Sum(path => new FileInfo(path).Length);
            long totalBytes = ComputeDirectoryBytes(outputPath);
            long metadataBytes = totalBytes - archiveBytes;
            if (archiveBytes <= 0 || metadataBytes < 0)
                throw new InvalidOperationException(
                    "Dense candidate Entities content byte inventory is invalid: " +
                    $"archives={archivePaths.Length}, archiveBytes={archiveBytes}, " +
                    $"totalBytes={totalBytes}");

            return new EntityContentBuildResult(
                outputPath,
                catalogPath,
                archivePaths.Length,
                archiveBytes,
                metadataBytes,
                totalBytes);
        }

        internal static FrozenRollbackContentResult MeasureFrozenRollbackContent(
            string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
                throw new InvalidOperationException(
                    $"Dense candidate project root is missing: {projectRoot}");

            string rollbackRoot = Path.GetFullPath(Path.Combine(
                projectRoot,
                FrozenRollbackRootPath));
            string manifestPath = Path.Combine(
                rollbackRoot,
                "StaticMapPresentationManifest.asset");
            string sceneDirectory = Path.Combine(rollbackRoot, "Scenes");
            if (!File.Exists(manifestPath))
                throw new InvalidOperationException(
                    $"Dense candidate frozen rollback manifest is missing: {manifestPath}");
            if (!Directory.Exists(sceneDirectory))
                throw new InvalidOperationException(
                    $"Dense candidate frozen rollback scene directory is missing: {sceneDirectory}");

            string[] scenePaths = Directory
                .EnumerateFiles(sceneDirectory, "*.unity", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            long manifestBytes = new FileInfo(manifestPath).Length;
            long chunkBytes = scenePaths.Sum(path => new FileInfo(path).Length);
            if (manifestBytes <= 0 || scenePaths.Length <= 0 || chunkBytes <= 0)
                throw new InvalidOperationException(
                    "Dense candidate frozen rollback byte inventory is invalid: " +
                    $"manifestBytes={manifestBytes}, chunks={scenePaths.Length}, " +
                    $"chunkBytes={chunkBytes}");

            return new FrozenRollbackContentResult(
                manifestBytes,
                scenePaths.Length,
                chunkBytes);
        }

        internal static ProductionStaticAddressablesResult MeasureProductionStaticAddressables(
            OperationMapPresentationKind presentationKind,
            IEnumerable<string> entryAssetPaths)
        {
            if (entryAssetPaths == null)
                throw new ArgumentNullException(nameof(entryAssetPaths));
            if (presentationKind != OperationMapPresentationKind.StaticSceneChunks &&
                presentationKind != OperationMapPresentationKind.EntityScene)
            {
                throw new InvalidOperationException(
                    $"Unknown production presentation kind: {presentationKind}");
            }

            string chunkPrefix = StaticMapPresentationBaker.SceneOutputFolder + "/";
            int manifestCount = 0;
            int chunkCount = 0;
            foreach (string path in entryAssetPaths)
            {
                if (string.Equals(
                        path,
                        OperationMapAddressablesLayoutBuilder.ManifestPath,
                        StringComparison.Ordinal))
                {
                    manifestCount++;
                }
                else if (!string.IsNullOrEmpty(path) &&
                         path.StartsWith(chunkPrefix, StringComparison.Ordinal) &&
                         path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    chunkCount++;
                }
            }

            bool zeroCountsSatisfied = manifestCount == 0 && chunkCount == 0;
            if (presentationKind == OperationMapPresentationKind.EntityScene &&
                !zeroCountsSatisfied)
            {
                throw new InvalidOperationException(
                    "EntityScene production still owns retired static Addressables entries: " +
                    $"manifests={manifestCount}, chunks={chunkCount}");
            }

            return new ProductionStaticAddressablesResult(
                presentationKind.ToString(),
                manifestCount,
                chunkCount,
                zeroCountsSatisfied);
        }

        internal static PackedDependencyByteResult MeasurePackedDependencyBytes(
            IEnumerable<PackedAssetOccurrence> occurrences,
            IEnumerable<string> sharedDependencyGuids)
        {
            if (occurrences == null)
                throw new ArgumentNullException(nameof(occurrences));
            if (sharedDependencyGuids == null)
                throw new ArgumentNullException(nameof(sharedDependencyGuids));

            var bytesByGuidAndBundle =
                new Dictionary<string, Dictionary<string, long>>(StringComparer.Ordinal);
            foreach (PackedAssetOccurrence occurrence in occurrences)
            {
                if (string.IsNullOrWhiteSpace(occurrence.AssetGuid) ||
                    string.IsNullOrWhiteSpace(occurrence.BundleName) ||
                    occurrence.Bytes <= 0)
                {
                    throw new InvalidOperationException(
                        "Packed dependency occurrence requires a GUID, bundle, and positive bytes.");
                }

                if (!bytesByGuidAndBundle.TryGetValue(
                        occurrence.AssetGuid,
                        out Dictionary<string, long> byBundle))
                {
                    byBundle = new Dictionary<string, long>(StringComparer.Ordinal);
                    bytesByGuidAndBundle.Add(occurrence.AssetGuid, byBundle);
                }

                if (!byBundle.TryGetValue(occurrence.BundleName, out long existing) ||
                    occurrence.Bytes > existing)
                {
                    byBundle[occurrence.BundleName] = occurrence.Bytes;
                }
            }

            string[] sharedGuids = sharedDependencyGuids
                .Distinct(StringComparer.Ordinal)
                .OrderBy(guid => guid, StringComparer.Ordinal)
                .ToArray();
            long sharedBytes = 0;
            for (int index = 0; index < sharedGuids.Length; index++)
            {
                string guid = sharedGuids[index];
                if (string.IsNullOrWhiteSpace(guid) ||
                    !bytesByGuidAndBundle.TryGetValue(
                        guid,
                        out Dictionary<string, long> sharedByBundle))
                {
                    throw new InvalidOperationException(
                        $"Packed shared dependency is missing from Build Layout evidence: {guid}");
                }

                checked
                {
                    sharedBytes += sharedByBundle.Values.Sum();
                }
            }

            int duplicatedGuidCount = 0;
            long duplicatedBytes = 0;
            foreach (Dictionary<string, long> byBundle in bytesByGuidAndBundle.Values)
            {
                if (byBundle.Count <= 1)
                    continue;
                duplicatedGuidCount++;
                checked
                {
                    duplicatedBytes += byBundle.Values.Sum() - byBundle.Values.Max();
                }
            }

            return new PackedDependencyByteResult(
                sharedGuids.Length,
                sharedBytes,
                duplicatedGuidCount,
                duplicatedBytes);
        }

        internal static void RequireNoPackedDependencyDuplication(
            PackedDependencyByteResult result)
        {
            if (result.DuplicatedDependencyGuidCount != 0 ||
                result.DuplicatedDependencyBytes != 0)
            {
                throw new InvalidOperationException(
                    "Dense Addressables Build Layout contains duplicated dependency payloads: " +
                    $"guids={result.DuplicatedDependencyGuidCount}, " +
                    $"excessBytes={result.DuplicatedDependencyBytes}.");
            }
        }

        internal static PackedDependencyByteResult MeasurePackedDependencyBytes(
            BuildLayout layout,
            IEnumerable<string> sharedDependencyGuids)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            IEnumerable<PackedAssetOccurrence> explicitOccurrences =
                BuildLayoutHelpers
                    .EnumerateAssets(layout)
                    .Where(asset =>
                        asset != null &&
                        asset.Bundle != null &&
                        asset.SerializedSize + asset.StreamedSize > 0)
                    .Select(asset => new PackedAssetOccurrence(
                        asset.Guid,
                        asset.Bundle.Name,
                        checked((long)(asset.SerializedSize + asset.StreamedSize))));
            IEnumerable<PackedAssetOccurrence> implicitOccurrences =
                BuildLayoutHelpers
                    .EnumerateBundles(layout)
                    .SelectMany(bundle => bundle.Files)
                    .SelectMany(file =>
                        file.OtherAssets.Concat(
                            file.Assets.SelectMany(asset =>
                                asset.InternalReferencedOtherAssets)))
                    .Where(asset =>
                        asset != null &&
                        asset.File != null &&
                        asset.File.Bundle != null &&
                        asset.SerializedSize + asset.StreamedSize > 0)
                    .Select(asset => new PackedAssetOccurrence(
                        asset.AssetGuid,
                        asset.File.Bundle.Name,
                        checked((long)(asset.SerializedSize + asset.StreamedSize))));

            return MeasurePackedDependencyBytes(
                explicitOccurrences.Concat(implicitOccurrences),
                sharedDependencyGuids);
        }

        internal static PackedSourceHierarchyResult MeasurePackedSourceHierarchy(
            IEnumerable<PackedAssetPathOccurrence> occurrences)
        {
            if (occurrences == null)
                throw new ArgumentNullException(nameof(occurrences));

            var forbidden = new HashSet<string>(
                DenseSourceHierarchyPaths,
                StringComparer.Ordinal);
            PackedAssetPathOccurrence[] unique = occurrences
                .Select(occurrence => new PackedAssetPathOccurrence(
                    NormalizeAssetPath(occurrence.AssetPath),
                    occurrence.BundleName,
                    occurrence.Explicit))
                .Where(occurrence => !string.IsNullOrEmpty(occurrence.AssetPath))
                .GroupBy(
                    occurrence =>
                        $"{occurrence.AssetPath}\n{occurrence.BundleName}\n" +
                        (occurrence.Explicit ? "explicit" : "implicit"),
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            return new PackedSourceHierarchyResult(
                unique.Length,
                unique.Count(occurrence =>
                    occurrence.Explicit && forbidden.Contains(occurrence.AssetPath)),
                unique.Count(occurrence =>
                    !occurrence.Explicit && forbidden.Contains(occurrence.AssetPath)));
        }

        internal static PackedSourceHierarchyResult MeasurePackedSourceHierarchy(
            BuildLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));

            IEnumerable<PackedAssetPathOccurrence> explicitOccurrences =
                BuildLayoutHelpers
                    .EnumerateAssets(layout)
                    .Where(asset => asset != null)
                    .Select(asset => new PackedAssetPathOccurrence(
                        asset.AssetPath,
                        asset.Bundle?.Name,
                        true));
            IEnumerable<PackedAssetPathOccurrence> implicitOccurrences =
                BuildLayoutHelpers
                    .EnumerateBundles(layout)
                    .SelectMany(bundle => bundle.Files)
                    .SelectMany(file =>
                        file.OtherAssets.Concat(
                            file.Assets.SelectMany(asset =>
                                asset.InternalReferencedOtherAssets)))
                    .Where(asset => asset != null)
                    .Select(asset => new PackedAssetPathOccurrence(
                        asset.AssetPath,
                        asset.File?.Bundle?.Name,
                        false));
            return MeasurePackedSourceHierarchy(
                explicitOccurrences.Concat(implicitOccurrences));
        }

        internal static void RequirePackedSourceHierarchyExclusion(
            PackedSourceHierarchyResult result)
        {
            if (result.SourceHierarchyExplicitAssetCount != 0 ||
                result.SourceHierarchyImplicitAssetCount != 0)
            {
                throw new InvalidOperationException(
                    "Dense Addressables Build Layout contains source hierarchy assets: " +
                    $"explicit={result.SourceHierarchyExplicitAssetCount}, " +
                    $"implicit={result.SourceHierarchyImplicitAssetCount}.");
            }
        }

        internal static string SelectSingleGeneratedBuildLayoutPath(
            IEnumerable<string> originalPaths,
            IEnumerable<string> currentPaths,
            Func<string, bool> fileExists)
        {
            if (originalPaths == null)
                throw new ArgumentNullException(nameof(originalPaths));
            if (currentPaths == null)
                throw new ArgumentNullException(nameof(currentPaths));
            if (fileExists == null)
                throw new ArgumentNullException(nameof(fileExists));

            var original = new HashSet<string>(
                originalPaths.Where(path => !string.IsNullOrWhiteSpace(path)),
                StringComparer.Ordinal);
            string[] generated = currentPaths
                .Where(path =>
                    !string.IsNullOrWhiteSpace(path) &&
                    !original.Contains(path) &&
                    path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                    fileExists(path))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (generated.Length != 1)
            {
                throw new InvalidOperationException(
                    "Dense Addressables build must produce exactly one new JSON Build Layout, " +
                    $"but found {generated.Length}.");
            }

            return generated[0];
        }

        private static ProductionStaticAddressablesResult
            MeasureCurrentProductionStaticAddressables()
        {
            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.GetSettings(false);
            OperationMapDefinition definition =
                AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    OperationMapAddressablesLayoutBuilder.DefinitionPath);
            if (settings == null || definition == null)
                throw new InvalidOperationException(
                    "Production Addressables settings and operation-map definition are required.");

            return MeasureProductionStaticAddressables(
                definition.PresentationKind,
                settings.groups
                    .Where(group => group != null)
                    .SelectMany(group => group.entries)
                    .Where(entry => entry != null)
                    .Select(entry => entry.AssetPath));
        }

        private static RuntimeContentReport CreateReport(
            string projectRoot,
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
            AddressablesContentBuildResult addressablesResult,
            EntityContentBuildResult entityContentResult,
            FrozenRollbackContentResult frozenRollbackResult)
        {
            string addressablesOutput = Path.GetFullPath(Path.Combine(
                projectRoot,
                AddressablesOutputPath));
            string addressablesCatalog = Path.Combine(addressablesOutput, "catalog.bin");
            if (!File.Exists(addressablesCatalog))
                throw new InvalidOperationException(
                    $"Dense candidate Addressables catalog is missing: {addressablesCatalog}");
            ProductionStaticAddressablesResult production =
                MeasureCurrentProductionStaticAddressables();

            return new RuntimeContentReport
            {
                schema = "warline.operation-map.dense-city-candidate-runtime-content",
                schemaVersion = 7,
                result = "DenseCityCandidateRuntimeContentBuilt",
                operationMapId = plan.OperationMapId,
                entitySceneGuid = plan.EntitySceneGuid,
                definitionAddress = plan.AddressPrefix + "definition",
                sourceSceneAddress = plan.AddressPrefix + "source-scene",
                plannedRootCount = plan.Entries.Count,
                explicitAddressableEntryCount = 2,
                sharedDependencyCount = plan.SharedDependencyCount,
                staticRuntimeEntryCount = 0,
                addressablesOutputPath = addressablesOutput,
                addressablesCatalogPath = addressablesCatalog,
                addressablesBundleCount = Directory
                    .EnumerateFiles(addressablesOutput, "*.bundle", SearchOption.AllDirectories)
                    .Count(),
                addressablesBytes = ComputeDirectoryBytes(addressablesOutput),
                publishedLocalContentEvidenceComplete = 1,
                publishedLocalCatalogBytes =
                    addressablesResult.PublishedLocalContent.CatalogBytes,
                publishedLocalBundleCount =
                    addressablesResult.PublishedLocalContent.BundleCount,
                publishedLocalBundleBytes =
                    addressablesResult.PublishedLocalContent.BundleBytes,
                publishedLocalBundleSetSha256 =
                    addressablesResult.PublishedLocalContent.BundleSetSha256,
                addressablesBuildLayoutPath = addressablesResult.BuildLayoutPath,
                addressablesBuildLayoutSha256 = ComputeSha256(
                    addressablesResult.BuildLayoutPath),
                packedDependencyMetricsComplete = 1,
                sharedDependencyGuidCount =
                    addressablesResult.PackedDependencyBytes.SharedDependencyGuidCount,
                sharedDependencyBytes =
                    addressablesResult.PackedDependencyBytes.SharedDependencyBytes,
                duplicatedDependencyGuidCount =
                    addressablesResult.PackedDependencyBytes.DuplicatedDependencyGuidCount,
                duplicatedDependencyBytes =
                    addressablesResult.PackedDependencyBytes.DuplicatedDependencyBytes,
                packedSourceHierarchyEvidenceComplete = 1,
                packedAssetPathCount =
                    addressablesResult.PackedSourceHierarchy.PackedAssetPathCount,
                packedSourceHierarchyExplicitAssetCount =
                    addressablesResult.PackedSourceHierarchy
                        .SourceHierarchyExplicitAssetCount,
                packedSourceHierarchyImplicitAssetCount =
                    addressablesResult.PackedSourceHierarchy
                        .SourceHierarchyImplicitAssetCount,
                entityContentOutputPath = entityContentResult.OutputPath,
                entityContentCatalogPath = entityContentResult.CatalogPath,
                entityContentArchiveCount = entityContentResult.ArchiveCount,
                entitySceneArchiveBytes = entityContentResult.ArchiveBytes,
                entityContentMetadataBytes = entityContentResult.MetadataBytes,
                entityContentBytes = entityContentResult.TotalBytes,
                frozenRollbackManifestBytes = frozenRollbackResult.ManifestBytes,
                frozenRollbackChunkCount = frozenRollbackResult.ChunkCount,
                frozenRollbackChunkBytes = frozenRollbackResult.ChunkBytes,
                productionPresentationKind = production.PresentationKind,
                productionStaticManifestEntryCount = production.ManifestEntryCount,
                productionPresentationChunkEntryCount = production.ChunkEntryCount,
                requiredStaticManifestEntryCountAfterCutover = 0,
                requiredPresentationChunkEntryCountAfterCutover = 0,
                postCutoverZeroCountsSatisfied = production.ZeroCountsSatisfied ? 1 : 0,
                candidateSubSceneSha256 = ComputeSha256(
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath),
                candidateDefinitionSha256 = ComputeSha256(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateDefinitionPath),
                candidateRuntimeBindingSha256 = ComputeSha256(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .DenseCandidateRuntimeBindingPath),
                directBakeParityReportSha256 = ComputeSha256(
                    OperationMapDenseCityGeneratedTransformParityValidator.DefaultReportPath),
                addressablesCatalogSha256 = ComputeSha256(addressablesCatalog),
                entityContentCatalogSha256 = ComputeSha256(entityContentResult.CatalogPath),
                buildResultOutputPath = addressablesResult.PlayerBuildResult.OutputPath,
                productionCutover = 0,
                productionSettingsMutated = 0,
                sharedOutputRestored = 1
            };
        }

        private static void WriteReport(string projectRoot, RuntimeContentReport report)
        {
            WriteJsonReport(projectRoot, ReportPath, report);
        }

        private static void WriteJsonReport(
            string projectRoot,
            string reportPath,
            object report)
        {
            string absolutePath = Path.Combine(projectRoot, reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? projectRoot);
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(report, true) + "\n",
                Utf8WithoutBom);
            AssetDatabase.ImportAsset(reportPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void RequireMacOsBuildTarget()
        {
            if (!BuildPipeline.IsBuildTargetSupported(
                    BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneOSX))
            {
                throw new InvalidOperationException(
                    "Dense candidate runtime parity requires macOS Standalone Build Support.");
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
            {
                throw new InvalidOperationException(
                    "Dense candidate runtime content must use -buildTarget StandaloneOSX. " +
                    "Android validation is user-triggered only.");
            }
        }

        private static void RequireFreeDiskSpace(string projectRoot)
        {
            string volumeRoot = Path.GetPathRoot(projectRoot);
            if (string.IsNullOrEmpty(volumeRoot))
                throw new InvalidOperationException(
                    $"Dense candidate runtime-content volume is unresolved: {projectRoot}");
            long availableBytes = new DriveInfo(volumeRoot).AvailableFreeSpace;
            if (availableBytes < MinimumFreeDiskBytes)
            {
                throw new InvalidOperationException(
                    $"Dense candidate runtime content requires at least " +
                    $"{MinimumFreeDiskBytes / (1024L * 1024L * 1024L)} GiB free; " +
                    $"{availableBytes / (1024L * 1024L * 1024L)} GiB is available.");
            }
        }

        private static string ComputeSha256(string path)
        {
            string physicalPath = Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            if (!File.Exists(physicalPath))
                throw new InvalidOperationException(
                    $"Dense runtime-content fingerprint input is missing: {physicalPath}");
            using FileStream stream = File.OpenRead(physicalPath);
            using SHA256 algorithm = SHA256.Create();
            return string.Concat(
                algorithm.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static long ComputeDirectoryBytes(string path) =>
            Directory
                .EnumerateFiles(path, "*", SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);

        internal readonly struct EntityContentBuildResult
        {
            internal EntityContentBuildResult(
                string outputPath,
                string catalogPath,
                int archiveCount,
                long archiveBytes,
                long metadataBytes,
                long totalBytes)
            {
                OutputPath = outputPath;
                CatalogPath = catalogPath;
                ArchiveCount = archiveCount;
                ArchiveBytes = archiveBytes;
                MetadataBytes = metadataBytes;
                TotalBytes = totalBytes;
            }

            internal string OutputPath { get; }
            internal string CatalogPath { get; }
            internal int ArchiveCount { get; }
            internal long ArchiveBytes { get; }
            internal long MetadataBytes { get; }
            internal long TotalBytes { get; }
        }

        internal readonly struct PublishedLocalContentResult
        {
            internal PublishedLocalContentResult(
                string outputPath,
                string catalogPath,
                long catalogBytes,
                int bundleCount,
                long bundleBytes,
                string bundleSetSha256)
            {
                OutputPath = outputPath;
                CatalogPath = catalogPath;
                CatalogBytes = catalogBytes;
                BundleCount = bundleCount;
                BundleBytes = bundleBytes;
                BundleSetSha256 = bundleSetSha256;
            }

            internal string OutputPath { get; }
            internal string CatalogPath { get; }
            internal long CatalogBytes { get; }
            internal int BundleCount { get; }
            internal long BundleBytes { get; }
            internal string BundleSetSha256 { get; }
        }

        internal readonly struct FrozenRollbackContentResult
        {
            internal FrozenRollbackContentResult(
                long manifestBytes,
                int chunkCount,
                long chunkBytes)
            {
                ManifestBytes = manifestBytes;
                ChunkCount = chunkCount;
                ChunkBytes = chunkBytes;
            }

            internal long ManifestBytes { get; }
            internal int ChunkCount { get; }
            internal long ChunkBytes { get; }
        }

        internal readonly struct ProductionStaticAddressablesResult
        {
            internal ProductionStaticAddressablesResult(
                string presentationKind,
                int manifestEntryCount,
                int chunkEntryCount,
                bool zeroCountsSatisfied)
            {
                PresentationKind = presentationKind;
                ManifestEntryCount = manifestEntryCount;
                ChunkEntryCount = chunkEntryCount;
                ZeroCountsSatisfied = zeroCountsSatisfied;
            }

            internal string PresentationKind { get; }
            internal int ManifestEntryCount { get; }
            internal int ChunkEntryCount { get; }
            internal bool ZeroCountsSatisfied { get; }
        }

        internal readonly struct PackedAssetOccurrence
        {
            internal PackedAssetOccurrence(string assetGuid, string bundleName, long bytes)
            {
                AssetGuid = assetGuid;
                BundleName = bundleName;
                Bytes = bytes;
            }

            internal string AssetGuid { get; }
            internal string BundleName { get; }
            internal long Bytes { get; }
        }

        internal readonly struct PackedAssetPathOccurrence
        {
            internal PackedAssetPathOccurrence(
                string assetPath,
                string bundleName,
                bool explicitAsset)
            {
                AssetPath = assetPath;
                BundleName = bundleName ?? string.Empty;
                Explicit = explicitAsset;
            }

            internal string AssetPath { get; }
            internal string BundleName { get; }
            internal bool Explicit { get; }
        }

        internal readonly struct PackedDependencyByteResult
        {
            internal PackedDependencyByteResult(
                int sharedDependencyGuidCount,
                long sharedDependencyBytes,
                int duplicatedDependencyGuidCount,
                long duplicatedDependencyBytes)
            {
                SharedDependencyGuidCount = sharedDependencyGuidCount;
                SharedDependencyBytes = sharedDependencyBytes;
                DuplicatedDependencyGuidCount = duplicatedDependencyGuidCount;
                DuplicatedDependencyBytes = duplicatedDependencyBytes;
            }

            internal int SharedDependencyGuidCount { get; }
            internal long SharedDependencyBytes { get; }
            internal int DuplicatedDependencyGuidCount { get; }
            internal long DuplicatedDependencyBytes { get; }
        }

        internal readonly struct PackedSourceHierarchyResult
        {
            internal PackedSourceHierarchyResult(
                int packedAssetPathCount,
                int sourceHierarchyExplicitAssetCount,
                int sourceHierarchyImplicitAssetCount)
            {
                PackedAssetPathCount = packedAssetPathCount;
                SourceHierarchyExplicitAssetCount = sourceHierarchyExplicitAssetCount;
                SourceHierarchyImplicitAssetCount = sourceHierarchyImplicitAssetCount;
            }

            internal int PackedAssetPathCount { get; }
            internal int SourceHierarchyExplicitAssetCount { get; }
            internal int SourceHierarchyImplicitAssetCount { get; }
        }

        private readonly struct AddressablesContentBuildResult
        {
            internal AddressablesContentBuildResult(
                AddressablesPlayerBuildResult playerBuildResult,
                string buildLayoutPath,
                PackedDependencyByteResult packedDependencyBytes,
                PackedSourceHierarchyResult packedSourceHierarchy,
                PublishedLocalContentResult publishedLocalContent)
            {
                PlayerBuildResult = playerBuildResult;
                BuildLayoutPath = buildLayoutPath;
                PackedDependencyBytes = packedDependencyBytes;
                PackedSourceHierarchy = packedSourceHierarchy;
                PublishedLocalContent = publishedLocalContent;
            }

            internal AddressablesPlayerBuildResult PlayerBuildResult { get; }
            internal string BuildLayoutPath { get; }
            internal PackedDependencyByteResult PackedDependencyBytes { get; }
            internal PackedSourceHierarchyResult PackedSourceHierarchy { get; }
            internal PublishedLocalContentResult PublishedLocalContent { get; }
        }

        private sealed class BuildLayoutCaptureScope : IDisposable
        {
            private readonly bool originalGenerateBuildLayout;
            private readonly ProjectConfigData.ReportFileFormat originalFormat;
            private readonly string[] originalPaths;
            private bool disposed;

            private BuildLayoutCaptureScope()
            {
                originalGenerateBuildLayout = ProjectConfigData.GenerateBuildLayout;
                originalFormat = ProjectConfigData.BuildLayoutReportFileFormat;
                originalPaths = ProjectConfigData.BuildReportFilePaths.ToArray();
                ProjectConfigData.BuildLayoutReportFileFormat =
                    ProjectConfigData.ReportFileFormat.JSON;
                ProjectConfigData.GenerateBuildLayout = true;
            }

            internal static BuildLayoutCaptureScope Begin() => new();

            internal string RequireSingleGeneratedBuildLayoutPath() =>
                SelectSingleGeneratedBuildLayoutPath(
                    originalPaths,
                    ProjectConfigData.BuildReportFilePaths,
                    File.Exists);

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;

                string[] generatedPaths = ProjectConfigData.BuildReportFilePaths
                    .Where(path =>
                        !string.IsNullOrWhiteSpace(path) &&
                        !originalPaths.Contains(path, StringComparer.Ordinal))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                ProjectConfigData.ClearBuildReportFilePaths();
                for (int index = 0; index < originalPaths.Length; index++)
                    ProjectConfigData.AddBuildReportFilePath(originalPaths[index]);
                ProjectConfigData.BuildLayoutReportFileFormat = originalFormat;
                ProjectConfigData.GenerateBuildLayout = originalGenerateBuildLayout;

                for (int index = 0; index < generatedPaths.Length; index++)
                {
                    if (File.Exists(generatedPaths[index]))
                        File.Delete(generatedPaths[index]);
                }
            }
        }

        private sealed class DenseRuntimeContentOutputTransaction : IDisposable
        {
            private readonly string backupRoot;
            private readonly DirectoryState shared;
            private readonly DirectoryState denseAddressables;
            private readonly DirectoryState denseEntities;
            private readonly DirectoryState denseBuildLayout;
            private bool completed;
            private bool sharedPrepared;

            private DenseRuntimeContentOutputTransaction(
                string backupRoot,
                DirectoryState shared,
                DirectoryState denseAddressables,
                DirectoryState denseEntities,
                DirectoryState denseBuildLayout)
            {
                this.backupRoot = backupRoot;
                this.shared = shared;
                this.denseAddressables = denseAddressables;
                this.denseEntities = denseEntities;
                this.denseBuildLayout = denseBuildLayout;
            }

            internal static DenseRuntimeContentOutputTransaction Begin(
                string projectRoot,
                string sharedPath,
                string denseAddressablesPath,
                string denseEntitiesPath,
                string denseBuildLayoutPath)
            {
                string backupRoot = Path.Combine(
                    projectRoot,
                    "Library",
                    "OperationMapDenseCityRuntimeContentTransactions",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(backupRoot);
                return new DenseRuntimeContentOutputTransaction(
                    backupRoot,
                    Capture(projectRoot, sharedPath, Path.Combine(backupRoot, "shared")),
                    Capture(
                        projectRoot,
                        denseAddressablesPath,
                        Path.Combine(backupRoot, "dense-addressables")),
                    Capture(
                        projectRoot,
                        denseEntitiesPath,
                        Path.Combine(backupRoot, "dense-entities")),
                    Capture(
                        projectRoot,
                        denseBuildLayoutPath,
                        Path.Combine(backupRoot, "dense-build-layout")));
            }

            internal void PrepareSharedOutputForBuild()
            {
                if (sharedPrepared)
                    throw new InvalidOperationException("Shared Addressables output already prepared.");
                DeleteDirectory(shared.Path);
                sharedPrepared = true;
            }

            internal void PublishBuiltAddressables()
            {
                if (!sharedPrepared || !Directory.Exists(shared.Path))
                {
                    throw new InvalidOperationException(
                        $"Dense Addressables build output is missing: {shared.Path}");
                }

                DeleteDirectory(denseAddressables.Path);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(denseAddressables.Path) ??
                    throw new InvalidOperationException("Dense Addressables output has no parent."));
                Directory.Move(shared.Path, denseAddressables.Path);
                RestoreCopy(shared);
            }

            internal void Commit()
            {
                if (completed)
                    throw new InvalidOperationException("Dense runtime-content transaction already completed.");
                if (!Directory.Exists(denseAddressables.Path) ||
                    !Directory.Exists(denseEntities.Path) ||
                    !Directory.Exists(denseBuildLayout.Path))
                {
                    throw new InvalidOperationException(
                        "Dense runtime-content transaction cannot commit incomplete outputs.");
                }

                Restore(shared);
                completed = true;
                DeleteDirectory(backupRoot);
            }

            internal void Rollback()
            {
                if (completed)
                    return;
                DeleteDirectory(shared.Path);
                DeleteDirectory(denseAddressables.Path);
                DeleteDirectory(denseEntities.Path);
                DeleteDirectory(denseBuildLayout.Path);
                Restore(shared);
                Restore(denseAddressables);
                Restore(denseEntities);
                Restore(denseBuildLayout);
                completed = true;
                DeleteDirectory(backupRoot);
            }

            public void Dispose()
            {
                if (!completed)
                    Rollback();
            }

            private static DirectoryState Capture(
                string projectRoot,
                string relativePath,
                string backupPath)
            {
                string path = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
                bool existed = Directory.Exists(path);
                if (existed)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath) ?? projectRoot);
                    Directory.Move(path, backupPath);
                }

                return new DirectoryState(path, backupPath, existed);
            }

            private static void Restore(DirectoryState state)
            {
                if (Directory.Exists(state.Path))
                    return;
                if (!state.Existed || !Directory.Exists(state.BackupPath))
                    return;

                Directory.CreateDirectory(
                    Path.GetDirectoryName(state.Path) ??
                    throw new InvalidOperationException("Restored output has no parent."));
                Directory.Move(state.BackupPath, state.Path);
            }

            private static void RestoreCopy(DirectoryState state)
            {
                if (!state.Existed || !Directory.Exists(state.BackupPath))
                    return;
                CopyDirectory(state.BackupPath, state.Path);
            }

            private static void CopyDirectory(string source, string destination)
            {
                Directory.CreateDirectory(destination);
                foreach (string directory in Directory.EnumerateDirectories(
                             source,
                             "*",
                             SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(
                        Path.Combine(destination, Path.GetRelativePath(source, directory)));
                }
                foreach (string file in Directory.EnumerateFiles(
                             source,
                             "*",
                             SearchOption.AllDirectories))
                {
                    string destinationFile = Path.Combine(
                        destination,
                        Path.GetRelativePath(source, file));
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(destinationFile) ?? destination);
                    File.Copy(file, destinationFile, true);
                }
            }

            private static void DeleteDirectory(string path)
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }

            private readonly struct DirectoryState
            {
                internal DirectoryState(string path, string backupPath, bool existed)
                {
                    Path = path;
                    BackupPath = backupPath;
                    Existed = existed;
                }

                internal string Path { get; }
                internal string BackupPath { get; }
                internal bool Existed { get; }
            }
        }

        internal readonly struct SourceHierarchyExclusionResult
        {
            internal SourceHierarchyExclusionResult(
                int explicitAddressableEntryCount,
                int enabledPlayerBuildSceneCount,
                int sourceHierarchyExplicitAddressableEntryCount,
                int sourceHierarchyPlayerBuildSceneCount)
            {
                ExplicitAddressableEntryCount = explicitAddressableEntryCount;
                EnabledPlayerBuildSceneCount = enabledPlayerBuildSceneCount;
                SourceHierarchyExplicitAddressableEntryCount =
                    sourceHierarchyExplicitAddressableEntryCount;
                SourceHierarchyPlayerBuildSceneCount =
                    sourceHierarchyPlayerBuildSceneCount;
            }

            internal int ExplicitAddressableEntryCount { get; }
            internal int EnabledPlayerBuildSceneCount { get; }
            internal int SourceHierarchyExplicitAddressableEntryCount { get; }
            internal int SourceHierarchyPlayerBuildSceneCount { get; }
        }

        internal readonly struct LocalBundleDeliveryResult
        {
            internal LocalBundleDeliveryResult(
                bool buildRemoteCatalog,
                bool disableCatalogUpdateOnStartup,
                bool includeInBuild,
                bool usesLocalBuildPath,
                bool usesLocalLoadPath,
                bool networkLoadPath)
            {
                BuildRemoteCatalog = buildRemoteCatalog;
                DisableCatalogUpdateOnStartup = disableCatalogUpdateOnStartup;
                IncludeInBuild = includeInBuild;
                UsesLocalBuildPath = usesLocalBuildPath;
                UsesLocalLoadPath = usesLocalLoadPath;
                NetworkLoadPath = networkLoadPath;
            }

            internal bool BuildRemoteCatalog { get; }
            internal bool DisableCatalogUpdateOnStartup { get; }
            internal bool IncludeInBuild { get; }
            internal bool UsesLocalBuildPath { get; }
            internal bool UsesLocalLoadPath { get; }
            internal bool NetworkLoadPath { get; }
        }

        [Serializable]
        private sealed class SourceHierarchyExclusionReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public int explicitAddressableEntryCount;
            public int enabledPlayerBuildSceneCount;
            public int sourceHierarchyExplicitAddressableEntryCount;
            public int sourceHierarchyPlayerBuildSceneCount;
            public int packedImplicitDependencyEvidenceComplete;
            public int productionCutover;
        }

        [Serializable]
        private sealed class FrozenRollbackByteInventoryReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string manifestPath;
            public long manifestBytes;
            public string chunkDirectoryPath;
            public int chunkCount;
            public long chunkBytes;
            public string productionPresentationKind;
            public int productionStaticManifestEntryCount;
            public int productionPresentationChunkEntryCount;
            public int requiredStaticManifestEntryCountAfterCutover;
            public int requiredPresentationChunkEntryCountAfterCutover;
            public int postCutoverZeroCountsSatisfied;
            public int productionCutover;
        }

        [Serializable]
        private sealed class RuntimeContentReport
        {
            public string schema;
            public int schemaVersion;
            public string result;
            public string operationMapId;
            public string entitySceneGuid;
            public string definitionAddress;
            public string sourceSceneAddress;
            public int plannedRootCount;
            public int explicitAddressableEntryCount;
            public int sharedDependencyCount;
            public int staticRuntimeEntryCount;
            public string addressablesOutputPath;
            public string addressablesCatalogPath;
            public int addressablesBundleCount;
            public long addressablesBytes;
            public int publishedLocalContentEvidenceComplete;
            public long publishedLocalCatalogBytes;
            public int publishedLocalBundleCount;
            public long publishedLocalBundleBytes;
            public string publishedLocalBundleSetSha256;
            public string addressablesBuildLayoutPath;
            public string addressablesBuildLayoutSha256;
            public int packedDependencyMetricsComplete;
            public int sharedDependencyGuidCount;
            public long sharedDependencyBytes;
            public int duplicatedDependencyGuidCount;
            public long duplicatedDependencyBytes;
            public int packedSourceHierarchyEvidenceComplete;
            public int packedAssetPathCount;
            public int packedSourceHierarchyExplicitAssetCount;
            public int packedSourceHierarchyImplicitAssetCount;
            public string entityContentOutputPath;
            public string entityContentCatalogPath;
            public int entityContentArchiveCount;
            public long entitySceneArchiveBytes;
            public long entityContentMetadataBytes;
            public long entityContentBytes;
            public long frozenRollbackManifestBytes;
            public int frozenRollbackChunkCount;
            public long frozenRollbackChunkBytes;
            public string productionPresentationKind;
            public int productionStaticManifestEntryCount;
            public int productionPresentationChunkEntryCount;
            public int requiredStaticManifestEntryCountAfterCutover;
            public int requiredPresentationChunkEntryCountAfterCutover;
            public int postCutoverZeroCountsSatisfied;
            public string candidateSubSceneSha256;
            public string candidateDefinitionSha256;
            public string candidateRuntimeBindingSha256;
            public string directBakeParityReportSha256;
            public string addressablesCatalogSha256;
            public string entityContentCatalogSha256;
            public string buildResultOutputPath;
            public int productionCutover;
            public int productionSettingsMutated;
            public int sharedOutputRestored;
        }
    }
}

#endif
