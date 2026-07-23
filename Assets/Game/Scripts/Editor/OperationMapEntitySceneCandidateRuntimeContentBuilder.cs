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
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Build;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;
    using UnityEngine;
    using Hash128 = Unity.Entities.Hash128;

    /// <summary>
    /// Builds local candidate content for runtime parity without retaining candidate entries in
    /// production Addressables settings. Production groups and entries are never moved or relabeled.
    /// </summary>
    internal static class OperationMapEntitySceneCandidateRuntimeContentBuilder
    {
        internal const string CandidateGroupName =
            "Operation Map - Validation Only - skirmish-desert-base-01 - EntityScene";
        internal const string ReportPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_candidate_runtime_content.json";
        internal const string EntityContentOutputPath =
            "Library/OperationMapCandidateRuntimeContent/Entities";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/EntityScene Migration/Build Candidate Runtime Parity Content")]
        public static void BuildCandidateRuntimeParityContent()
        {
            if (!BuildPipeline.IsBuildTargetSupported(
                    BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneOSX))
            {
                throw new InvalidOperationException(
                    "Candidate Editor runtime parity requires the macOS Standalone Build Support " +
                    "module for this Unity version. Android content/build validation is user-triggered only.");
            }
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneOSX)
            {
                throw new InvalidOperationException(
                    "Candidate Editor runtime parity content must be built with " +
                    "-buildTarget StandaloneOSX; Android content is user-triggered only.");
            }

            OperationMapEntitySceneCandidateAddressablesLayoutBuilder
                .BuildCandidateEntitySceneAddressablesLayout();
            if (!OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreatePlan(
                    out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
                    out string planError))
            {
                throw new InvalidOperationException(
                    $"Candidate runtime-content plan rejected: {planError}");
            }

            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (settings == null)
                throw new InvalidOperationException("Addressables settings are required.");
            if (settings.FindGroup(CandidateGroupName) != null)
            {
                throw new InvalidOperationException(
                    $"Stale candidate validation group exists: {CandidateGroupName}");
            }

            string settingsPath = AssetDatabase.GetAssetPath(settings);
            string settingsPhysicalPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                settingsPath));
            byte[] settingsSnapshot = File.ReadAllBytes(settingsPhysicalPath);
            AddressableAssetGroup candidateGroup = null;
            AddressablesPlayerBuildResult buildResult = null;
            EntityContentBuildResult entityContentResult = default;
            try
            {
                candidateGroup = CreateCandidateGroup(settings);
                AddCandidateEntry(
                    settings,
                    candidateGroup,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath,
                    plan.AddressPrefix + "definition");
                AddCandidateEntry(
                    settings,
                    candidateGroup,
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
                    plan.AddressPrefix + "source-scene");
                AssetDatabase.SaveAssets();

                AddressableAssetSettings.BuildPlayerContent(out buildResult);

                if (buildResult == null || !string.IsNullOrEmpty(buildResult.Error))
                {
                    throw new InvalidOperationException(
                        buildResult?.Error ?? "Candidate Addressables content build returned no result.");
                }

                entityContentResult = BuildCandidateEntityContent(plan);
                WriteReport(plan, buildResult, entityContentResult);
                Debug.Log(
                    $"[OperationMapCandidateRuntimeContent] result=Passed " +
                    $"entitySceneGuid={plan.EntitySceneGuid} output={buildResult.OutputPath} " +
                    $"entityContent={entityContentResult.OutputPath} " +
                    $"entityArchives={entityContentResult.ArchiveCount} " +
                    "productionCutover=0 temporaryGroupRetained=0");
            }
            finally
            {
                if (candidateGroup != null && settings.FindGroup(CandidateGroupName) != null)
                    settings.RemoveGroup(candidateGroup);
                AssetDatabase.SaveAssets();
                File.WriteAllBytes(settingsPhysicalPath, settingsSnapshot);
                AssetDatabase.ImportAsset(settingsPath, ImportAssetOptions.ForceSynchronousImport);
            }

            AddressableAssetSettings restoredSettings =
                AddressableAssetSettingsDefaultObject.GetSettings(false);
            if (restoredSettings == null || restoredSettings.FindGroup(CandidateGroupName) != null)
                throw new InvalidOperationException("Candidate validation group survived transactional cleanup.");
        }

        private static AddressableAssetGroup CreateCandidateGroup(AddressableAssetSettings settings)
        {
            AddressableAssetGroup group = settings.CreateGroup(
                CandidateGroupName,
                false,
                false,
                false,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));
            BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
                throw new InvalidOperationException("Candidate validation group has no bundled schema.");

            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.UseDefaultSchemaSettings = false;
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            schema.UseAssetBundleCrc = true;
            schema.UseAssetBundleCrcForCachedBundles = true;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.FileNameHash;
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            return group;
        }

        private static void AddCandidateEntry(
            AddressableAssetSettings settings,
            AddressableAssetGroup group,
            string assetPath,
            string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException($"Candidate runtime asset is missing: {assetPath}");
            if (settings.FindAssetEntry(guid) != null)
            {
                throw new InvalidOperationException(
                    $"Candidate runtime asset already belongs to production Addressables: {assetPath}");
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.SetAddress(address, false);
        }

        private static void WriteReport(
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
            AddressablesPlayerBuildResult result,
            EntityContentBuildResult entityContentResult)
        {
            var report = new RuntimeContentReport
            {
                schema = "warline.operation-map.candidate-runtime-content",
                schemaVersion = 3,
                result = "CandidateRuntimeContentBuilt",
                operationMapId = plan.OperationMapId,
                entitySceneGuid = plan.EntitySceneGuid,
                definitionAddress = plan.AddressPrefix + "definition",
                sourceSceneAddress = plan.AddressPrefix + "source-scene",
                outputPath = result.OutputPath,
                entityContentOutputPath = entityContentResult.OutputPath,
                entityContentCatalogPath = entityContentResult.CatalogPath,
                entityContentArchiveCount = entityContentResult.ArchiveCount,
                candidateSubSceneSha256 = ComputeSha256(
                    OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath),
                candidateDefinitionSha256 = ComputeSha256(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .CandidateDefinitionPath),
                candidateRuntimeBindingSha256 = ComputeSha256(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner
                        .CandidateRuntimeBindingPath),
                transformParityReportSha256 = ComputeSha256(
                    OperationMapEntityPresentationTransformParityValidator.ReportPath),
                addressablesCatalogSha256 = ComputeSha256(Path.Combine(
                    Path.GetDirectoryName(result.OutputPath) ??
                    throw new InvalidOperationException(
                        $"Addressables output has no directory: {result.OutputPath}"),
                    "catalog.bin")),
                entityContentCatalogSha256 = ComputeSha256(entityContentResult.CatalogPath),
                productionCutover = 0,
                temporaryGroupRetained = 0
            };
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string absolutePath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? projectRoot);
            File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true) + "\n", Utf8WithoutBom);
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static string ComputeSha256(string path)
        {
            string physicalPath = Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            if (!File.Exists(physicalPath))
                throw new InvalidOperationException(
                    $"Candidate runtime-content fingerprint input is missing: {physicalPath}");

            using FileStream stream = File.OpenRead(physicalPath);
            using SHA256 algorithm = SHA256.Create();
            return string.Concat(
                algorithm.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static EntityContentBuildResult BuildCandidateEntityContent(
            OperationMapEntitySceneCandidateAddressablesLayoutPlan plan)
        {
            var sceneGuid = new Hash128(plan.EntitySceneGuid);
            if (!sceneGuid.IsValid)
                throw new InvalidOperationException(
                    $"Candidate EntityScene GUID is invalid: {plan.EntitySceneGuid}");

            Hash128 playerGuid = DotsGlobalSettings.Instance.GetClientGUID();
            if (!playerGuid.IsValid)
                throw new InvalidOperationException("Entities client player GUID is invalid.");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputPath = Path.GetFullPath(Path.Combine(
                projectRoot,
                EntityContentOutputPath));
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);

            RemoteContentCatalogBuildUtility.BuildContent(
                new HashSet<Hash128> { sceneGuid },
                playerGuid,
                BuildTarget.StandaloneOSX,
                outputPath);

            string catalogPath = Path.Combine(
                outputPath,
                RuntimeContentManager.RelativeCatalogPath);
            if (!File.Exists(catalogPath))
                throw new InvalidOperationException(
                    $"Candidate Entities content catalog was not produced: {catalogPath}");

            int archiveCount = Directory
                .EnumerateFiles(outputPath, "*", SearchOption.AllDirectories)
                .Count(path => path.EndsWith(
                    ".archive",
                    StringComparison.OrdinalIgnoreCase));
            if (archiveCount == 0)
                throw new InvalidOperationException(
                    $"Candidate Entities content has no archives: {outputPath}");

            return new EntityContentBuildResult(
                outputPath,
                catalogPath,
                archiveCount);
        }

        private readonly struct EntityContentBuildResult
        {
            public EntityContentBuildResult(
                string outputPath,
                string catalogPath,
                int archiveCount)
            {
                OutputPath = outputPath;
                CatalogPath = catalogPath;
                ArchiveCount = archiveCount;
            }

            public string OutputPath { get; }
            public string CatalogPath { get; }
            public int ArchiveCount { get; }
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
            public string outputPath;
            public string entityContentOutputPath;
            public string entityContentCatalogPath;
            public int entityContentArchiveCount;
            public string candidateSubSceneSha256;
            public string candidateDefinitionSha256;
            public string candidateRuntimeBindingSha256;
            public string transformParityReportSha256;
            public string addressablesCatalogSha256;
            public string entityContentCatalogSha256;
            public int productionCutover;
            public int temporaryGroupRetained;
        }
    }
}

#endif
