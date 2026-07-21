#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEditor.AddressableAssets;
    using UnityEditor.AddressableAssets.Build;
    using UnityEditor.AddressableAssets.Settings;
    using UnityEditor.AddressableAssets.Settings.GroupSchemas;
    using UnityEngine;

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

                using (OperationMapEntitySceneBuildAdditions.UseCurrentProcessSceneOverride(
                           OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath))
                {
                    AddressableAssetSettings.BuildPlayerContent(out buildResult);
                }

                if (buildResult == null || !string.IsNullOrEmpty(buildResult.Error))
                {
                    throw new InvalidOperationException(
                        buildResult?.Error ?? "Candidate Addressables content build returned no result.");
                }

                WriteReport(plan, buildResult);
                Debug.Log(
                    $"[OperationMapCandidateRuntimeContent] result=Passed " +
                    $"entitySceneGuid={plan.EntitySceneGuid} output={buildResult.OutputPath} " +
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
            AddressablesPlayerBuildResult result)
        {
            var report = new RuntimeContentReport
            {
                schema = "warline.operation-map.candidate-runtime-content",
                schemaVersion = 1,
                result = "CandidateRuntimeContentBuilt",
                operationMapId = plan.OperationMapId,
                entitySceneGuid = plan.EntitySceneGuid,
                definitionAddress = plan.AddressPrefix + "definition",
                sourceSceneAddress = plan.AddressPrefix + "source-scene",
                outputPath = result.OutputPath,
                productionCutover = 0,
                temporaryGroupRetained = 0
            };
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string absolutePath = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? projectRoot);
            File.WriteAllText(absolutePath, JsonUtility.ToJson(report, true) + "\n", Utf8WithoutBom);
            AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceSynchronousImport);
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
            public int productionCutover;
            public int temporaryGroupRetained;
        }
    }
}

#endif
