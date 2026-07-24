#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Unity.Entities;
    using Unity.Entities.Build;
    using Unity.Entities.Content;
    using Unity.Scenes.Editor;
    using UnityEditor;
    using UnityEditor.AddressableAssets.Build;
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
        internal const string AddressablesOutputPath =
            "Library/OperationMapDenseCityRuntimeContent/Addressables";
        internal const string EntityContentOutputPath =
            "Library/OperationMapDenseCityRuntimeContent/Entities";

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
                EntityContentOutputPath);

            try
            {
                AddressablesPlayerBuildResult addressablesResult =
                    BuildIsolatedAddressables(plan, outputTransaction);
                productionSettingsTransaction.Rollback();
                EntityContentBuildResult entityContentResult = BuildEntityContent(plan);
                RuntimeContentReport report = CreateReport(
                    projectRoot,
                    plan,
                    addressablesResult,
                    entityContentResult);
                WriteReport(projectRoot, report);
                protectedSnapshot.RequireUnchanged();
                outputTransaction.Commit();
                Debug.Log(
                    $"[OperationMapDenseCityRuntimeContent] result=Passed " +
                    $"entitySceneGuid={plan.EntitySceneGuid} " +
                    $"addressablesBundles={report.addressablesBundleCount} " +
                    $"addressablesBytes={report.addressablesBytes} " +
                    $"entityArchives={report.entityContentArchiveCount} " +
                    $"entityBytes={report.entityContentBytes} productionCutover=0 " +
                    "productionSettingsMutated=0 sharedOutputRestored=1");
            }
            catch
            {
                productionSettingsTransaction.Rollback();
                reportTransaction.Rollback();
                throw;
            }
        }

        private static AddressablesPlayerBuildResult BuildIsolatedAddressables(
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
            DenseRuntimeContentOutputTransaction outputTransaction)
        {
            AddressableAssetSettings settings = null;
            BuildScriptPackedMode builder = null;
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

                schema.BuildPath.SetVariableByName(
                    settings,
                    AddressableAssetSettings.kLocalBuildPath);
                schema.LoadPath.SetVariableByName(
                    settings,
                    AddressableAssetSettings.kLocalLoadPath);
                schema.UseDefaultSchemaSettings = false;
                schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                schema.UseAssetBundleCrc = true;
                schema.UseAssetBundleCrcForCachedBundles = true;
                schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;
                schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

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

                outputTransaction.PublishBuiltAddressables();
                return result;
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
            int archiveCount = Directory
                .EnumerateFiles(outputPath, "*.archive", SearchOption.AllDirectories)
                .Count();
            if (archiveCount == 0)
                throw new InvalidOperationException(
                    $"Dense candidate Entities content has no archives: {outputPath}");

            return new EntityContentBuildResult(
                outputPath,
                catalogPath,
                archiveCount,
                ComputeDirectoryBytes(outputPath));
        }

        private static RuntimeContentReport CreateReport(
            string projectRoot,
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
            AddressablesPlayerBuildResult addressablesResult,
            EntityContentBuildResult entityContentResult)
        {
            string addressablesOutput = Path.GetFullPath(Path.Combine(
                projectRoot,
                AddressablesOutputPath));
            string addressablesCatalog = Path.Combine(addressablesOutput, "catalog.bin");
            if (!File.Exists(addressablesCatalog))
                throw new InvalidOperationException(
                    $"Dense candidate Addressables catalog is missing: {addressablesCatalog}");

            return new RuntimeContentReport
            {
                schema = "warline.operation-map.dense-city-candidate-runtime-content",
                schemaVersion = 1,
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
                entityContentOutputPath = entityContentResult.OutputPath,
                entityContentCatalogPath = entityContentResult.CatalogPath,
                entityContentArchiveCount = entityContentResult.ArchiveCount,
                entityContentBytes = entityContentResult.Bytes,
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
                buildResultOutputPath = addressablesResult.OutputPath,
                productionCutover = 0,
                productionSettingsMutated = 0,
                sharedOutputRestored = 1
            };
        }

        private static void WriteReport(string projectRoot, RuntimeContentReport report)
        {
            string absolutePath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? projectRoot);
            File.WriteAllText(
                absolutePath,
                JsonUtility.ToJson(report, true) + "\n",
                Utf8WithoutBom);
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceSynchronousImport);
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

        private readonly struct EntityContentBuildResult
        {
            internal EntityContentBuildResult(
                string outputPath,
                string catalogPath,
                int archiveCount,
                long bytes)
            {
                OutputPath = outputPath;
                CatalogPath = catalogPath;
                ArchiveCount = archiveCount;
                Bytes = bytes;
            }

            internal string OutputPath { get; }
            internal string CatalogPath { get; }
            internal int ArchiveCount { get; }
            internal long Bytes { get; }
        }

        private sealed class DenseRuntimeContentOutputTransaction : IDisposable
        {
            private readonly string backupRoot;
            private readonly DirectoryState shared;
            private readonly DirectoryState denseAddressables;
            private readonly DirectoryState denseEntities;
            private bool completed;
            private bool sharedPrepared;

            private DenseRuntimeContentOutputTransaction(
                string backupRoot,
                DirectoryState shared,
                DirectoryState denseAddressables,
                DirectoryState denseEntities)
            {
                this.backupRoot = backupRoot;
                this.shared = shared;
                this.denseAddressables = denseAddressables;
                this.denseEntities = denseEntities;
            }

            internal static DenseRuntimeContentOutputTransaction Begin(
                string projectRoot,
                string sharedPath,
                string denseAddressablesPath,
                string denseEntitiesPath)
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
                        Path.Combine(backupRoot, "dense-entities")));
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
                    !Directory.Exists(denseEntities.Path))
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
                Restore(shared);
                Restore(denseAddressables);
                Restore(denseEntities);
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
            public string entityContentOutputPath;
            public string entityContentCatalogPath;
            public int entityContentArchiveCount;
            public long entityContentBytes;
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
