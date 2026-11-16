#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Globalization;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Re-captures the retained S049 authored objective through the current compiler
    // and native metadata factories. The full mission and runtime state are never initialized.
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        const string S049ObjectiveSource = "Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishObjective_S049Candidate_20261028.asset";
        const string S049ObjectiveAsset = "Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishObjective_S049Candidate_20261028.asset";
        const string S049LayoutSource = "Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r1/layout.json";
        const string S049Map = "Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset";
        const string S049ObjectiveSha = "ee70c46ca4e2c07edf766124d717361ae6b761f76dfb7312908fa63d460c10fb";
        const string S049ObjectiveMetaSha = "04191e2f1916451ff77e709ae9316e248e5e2f3432b56da5218f109367fd1021";
        const string S049LayoutSha = "fad258a220248c4a60da9cd7b8f097c2eff08b20871bef8fbd3be919d260d7c7";
        const string S049MapSha = "181418e5b316d8a5625913d0251e5f5a9a83ab81f56baec49e4adcb2bb89e3b4";
        const string S049LayoutFingerprint = "9e94e1a6be531a77829fa04e8672b660c3a31e7f479a51653f1c89c92a9649cb";
        const string S049InitR7 = "1530cd7d8eb6d0f310deccc9546ba7c55eef48f2686f4661b0b4514c56ea0de7";
        const string S049InitR8 = "662d00b896e5a1b9cca36f420ff1e60b9b1de6525a76f55f633b624c8bcdd5d9";
        const string S049GraphR4 = "a643f41cfaf68ba267dd321915bc1040cbe89de55c8b64851f1811357d66ccfc";
        const string S049CompilerR6 = "4e0599e7f0ac194e5d175fbbde567dd4cc4c70be006f90ed7a21988d5af9a045";
        const string S049HistoricalMetadata = "Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/direct-metadata-projection-r3/s049-direct-metadata.json";
        const string S049HistoricalMetadataSha = "c8b588fd4708b1ccc94dd2d2c4d44ffa89a8510adeffc0d73421773fbe5edaec";
        const string S049LogicalBinding = "Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r6/logical-binding.json";
        const string S049LogicalBindingSha = "3bd7f60ae0ccf9e3ecba42247a12c00aa3b52a9d06b0f2494ebcbff01c3d95e5";

        public static void RunS049MetadataQualification()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            var args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-skirmishS049MetadataOutput");
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit fresh output directory required");
            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[at + 1]);
            string reportPath = Path.Combine(output, "s049-metadata.json");
            if (File.Exists(reportPath)) throw new InvalidOperationException("Output already exists");
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string objectiveSource = Path.Combine(projectRoot, S049ObjectiveSource);
            string objectiveMetaSource = objectiveSource + ".meta";
            string layoutPath = Path.Combine(projectRoot, S049LayoutSource);
            string objectivePath = Path.Combine(projectRoot, S049ObjectiveAsset);
            string objectiveMetaPath = objectivePath + ".meta";
            string evidenceRoot = Path.Combine(projectRoot, "Logs/private-candidates/s049-bundle-reconstruction-20261028-r1");
            string initR7Path = Path.Combine(evidenceRoot, "s049-partial-bundle-r7/partial-initialization.json");
            string initR8Path = Path.Combine(evidenceRoot, "s049-partial-bundle-r8/partial-initialization.json");
            string graphPath = Path.Combine(evidenceRoot, "s049-source-graph-r4.json");
            string compilerPath = Path.Combine(evidenceRoot, "s049-partial-bundle-r6/compiler-context.json");
            string logicalBindingPath = Path.Combine(projectRoot, S049LogicalBinding);
            RequireHash(objectiveSource, S049ObjectiveSha, "retained candidate objective");
            RequireHash(objectiveMetaSource, S049ObjectiveMetaSha, "retained candidate objective GUID");
            RequireHash(layoutPath, S049LayoutSha, "retained candidate layout");
            RequireHash(Path.Combine(projectRoot, S049Map), S049MapSha, "existing Campaign Armor Break source");
            RequireHash(initR7Path, S049InitR7, "r7 source-run initialization");
            RequireHash(initR8Path, S049InitR8, "r8 retained bundle initialization");
            RequireHash(graphPath, S049GraphR4, "retained r4 source graph");
            RequireHash(compilerPath, S049CompilerR6, "retained r6 compiler context");
            RequireHash(logicalBindingPath, S049LogicalBindingSha, "retained declared logical binding");
            JObject initR7 = JObject.Parse(File.ReadAllText(initR7Path));
            JObject initR8 = JObject.Parse(File.ReadAllText(initR8Path));
            foreach (string key in new[] { "mission_id", "selection", "typed_mission_configuration", "selected_operation_map", "source_config_graph", "mission_logical_binding" })
                if (!JToken.DeepEquals(initR7[key], initR8[key])) throw new InvalidOperationException("Retained r7/r8 S049 source context differs at " + key);
            if ((string)initR8["mission_logical_binding"]?["path"] != logicalBindingPath ||
                (string)initR8["mission_logical_binding"]?["sha256"] != S049LogicalBindingSha)
                throw new InvalidOperationException("Retained r8 logical binding descriptor differs from exact r6 artifact");
            if (File.Exists(objectivePath) || File.Exists(objectiveMetaPath))
                throw new InvalidOperationException("Temporary S049 objective import path is occupied; preserve its contents");

            var copiedHashes = new Dictionary<string, string>(StringComparer.Ordinal);
            SkirmishScenarioDefinitionConfig definition = null;
            SkirmishMapLayoutConfig layout = null;
            SkirmishObjectiveConfig objective = null;
            SkirmishMissionLogicalBindingConfig logicalBinding = null;
            var logicalViews = new List<SkirmishMissionViewProfile>();
            JObject report = null;
            var originalDefault = World.DefaultGameObjectInjectionWorld;
            try
            {
                File.Copy(objectiveSource, objectivePath, false);
                copiedHashes[objectivePath] = S049ObjectiveSha;
                File.Copy(objectiveMetaSource, objectiveMetaPath, false);
                copiedHashes[objectiveMetaPath] = S049ObjectiveMetaSha;
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                objective = AssetDatabase.LoadAssetAtPath<SkirmishObjectiveConfig>(S049ObjectiveAsset);
                if (objective == null) throw new InvalidOperationException("Retained candidate objective did not import as a typed asset");

                string layoutJson = File.ReadAllText(layoutPath);
                using (var text = new StringReader(layoutJson))
                using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
                {
                    var fields = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw new InvalidOperationException("Exactly one retained layout document required");
                    layout = SkirmishFrozenLayoutFieldImporter.Read(fields);
                }
                string layoutFingerprint = SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout);
                if (layoutFingerprint != S049LayoutFingerprint || layout.BindingValueFormat != SkirmishBindingValueFormat.CanonicalBinaryV2)
                    throw new InvalidOperationException("Retained typed S049 layout fingerprint or encoding differs: " + layoutFingerprint);

                var baseline = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
                var scene = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004"));
                var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(S049Map);
                if (baseline == null || baseline.buildingPlacement == null || scene == null || scene.UnitPrefabRegistryConfig == null || source == null)
                    throw new InvalidOperationException("Actual current production providers and Campaign source are required");
                if (source.ContentHash != "da42d5bc876e1f2eed02b3d7050d41759c010de973dfd7c51c51eba1a26e5a86")
                    throw new InvalidOperationException("Armor Break Campaign content hash differs from retained context: actual=" + source.ContentHash + " expected=da42d5bc876e1f2eed02b3d7050d41759c010de973dfd7c51c51eba1a26e5a86");
                string sourceFingerprint = SkirmishBindingCanonicalValues.SourceFingerprint(source);
                if (sourceFingerprint != "c001f6c1849acf24b7b54bdcfbd3a5bc75679c98f071f12924fa5ad6e59a1020")
                    throw new InvalidOperationException("Armor Break Campaign mission source fingerprint differs from retained compiler context: actual=" + sourceFingerprint + " expected=c001f6c1849acf24b7b54bdcfbd3a5bc75679c98f071f12924fa5ad6e59a1020");
                string fullSourceFingerprint = SkirmishBindingCanonicalValues.FullSourceFingerprint(source);

                string root = "Assets/Game/Configs/SkirmishExpansion/Shared/";
                var expansion = SkirmishExpansionCatalogConfig.Load();
                if (expansion == null) throw new InvalidOperationException("Current expansion profile assets unavailable");
                definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>();
                definition.Configure("S049", objective,
                    AssetDatabase.LoadAssetAtPath<SkirmishArmyProfileConfig>(root + "SkirmishArmy_GroundManeuver.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishStartPackageConfig>(root + "SkirmishStart_Field.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishEconomyConfig>(root + "SkirmishEconomy_V1.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishUpgradeConfig>(root + "SkirmishUpgrade_V1.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishIntelConfig>(root + "SkirmishIntel_SharedFog.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(root + "SkirmishRoleCatalog_V1.asset"), layout);
                definition.BindCampaignMap(source);
                JObject logicalDocument = JObject.Parse(File.ReadAllText(logicalBindingPath));
                JObject logicalTarget = logicalDocument["target_context"] as JObject;
                JObject portable = logicalDocument["portable_binding"] as JObject;
                if ((string)logicalDocument["kind"] != "OriginalCompilerDeclaredLogicalBinding" ||
                    (int?)logicalDocument["schema_version"] != 1 ||
                    (bool?)logicalDocument["native_capture"] != false || (bool?)logicalDocument["source_fit_certified"] != false ||
                    (bool?)logicalDocument["mission_ready"] != false ||
                    (string)logicalDocument["metadata_compiler_context"]?["sha256"] != S049CompilerR6 ||
                    logicalTarget == null || (string)logicalTarget["catalog_id"] != "S049" ||
                    (string)logicalTarget["definition_id"] != "skirmish.s049" || (int?)logicalTarget["content_version"] != 2 ||
                    (int?)logicalTarget["seed"] != 104734 || (string)logicalTarget["size"] != "Standard" ||
                    (string)logicalTarget["difficulty"] != "Regular" || (int?)logicalTarget["setup_hash"] != 971644661 ||
                    (string)logicalTarget["operation_map_id"] != source.OperationMapId ||
                    (string)logicalTarget["source_content_hash"] != source.ContentHash ||
                    (string)logicalTarget["source_fingerprint"] != sourceFingerprint ||
                    (string)logicalTarget["layout_fingerprint"] != S049LayoutFingerprint ||
                    (string)logicalTarget["layout_binding_fingerprint"] != "6ea8419e38992133108fc36cde04bac6053d68bb553f43d59845dce28ad429d8" ||
                    (string)logicalTarget["mission_logical_binding_fingerprint"] != "34813a76dd41a155f7d66a666eec8e36917b6a05eefcdf2c8af8a4add8ee5d6c" ||
                    (string)logicalDocument["metadata_compiler_context"]?["path"] != compilerPath ||
                    (int?)portable?["schema_version"] != 1 ||
                    (string)portable?["kind"] != "DeclaredOriginalLogicalBindingFieldsWithInlineViews" ||
                    (string)portable?["float_encoding"] != "IEEE754_float32_hex_bits" ||
                    (bool?)portable?["unity_object_ids_exported"] != false)
                    throw new InvalidOperationException("Retained S049 logical binding descriptor/context differs from exact source evidence");
                JObject logicalFields = portable["fields"] as JObject;
                if (logicalFields == null || (string)logicalFields["CatalogId"] != "S049" ||
                    (int?)logicalFields["ContentVersion"] != 1 || (string)logicalFields["DefinitionId"] != "skirmish.s049" ||
                    (int?)logicalFields["DefinitionVersion"] != 2 ||
                    (string)logicalFields["LayoutId"] != layout.LayoutId || (int?)logicalFields["ObjectiveKind"] != (int)objective.Kind ||
                    (string)logicalFields["ReviewedLayoutSemanticFingerprint"] != S049LayoutFingerprint ||
                    (string)logicalFields["SourceContentHash"] != source.ContentHash ||
                    (string)logicalFields["SourceOperationMapId"] != source.OperationMapId ||
                    (string)logicalFields["SourceSemanticFingerprint"] != sourceFingerprint ||
                    (int?)logicalFields["ValueFormat"] != (int)layout.BindingValueFormat ||
                    (string)logicalFields["SourceSceneGuid"] != source.SourceSceneReference.AssetGUID)
                    throw new InvalidOperationException("Retained S049 portable logical binding fields differ from actual source/layout identity");
                var decodedLogicalFields = DecodePortableLogicalValue(logicalFields, 0) as JObject;
                if (decodedLogicalFields == null) throw new InvalidOperationException("Retained S049 logical binding fields did not decode");
                logicalBinding = ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();
                JsonUtility.FromJsonOverwrite(decodedLogicalFields.ToString(Formatting.None), logicalBinding);
                // JsonUtility does not instantiate nested ScriptableObject references. The retained
                // portable artifact contains each original inline view's exact serialized fields,
                // so restore those typed objects explicitly and let the compiler verify their pins.
                var portableVariants = decodedLogicalFields["Variants"] as JArray;
                if (portableVariants == null || logicalBinding.Variants == null || portableVariants.Count != logicalBinding.Variants.Length)
                    throw new InvalidOperationException("Retained S049 inline view variants did not rehydrate one-to-one");
                for (int i = 0; i < portableVariants.Count; i++)
                {
                    JObject inlineView = portableVariants[i]?["View"] as JObject;
                    if (inlineView == null || logicalBinding.Variants[i] == null)
                        throw new InvalidOperationException("Retained S049 typed inline view is missing at variant " + i.ToString(CultureInfo.InvariantCulture));
                    var view = ScriptableObject.CreateInstance<SkirmishMissionViewProfile>();
                    logicalViews.Add(view);
                    JsonUtility.FromJsonOverwrite(inlineView.ToString(Formatting.None), view);
                    logicalBinding.Variants[i].View = view;
                }
                if (logicalBinding.CatalogId != "S049" || logicalBinding.ValueFormat != SkirmishBindingValueFormat.CanonicalBinaryV2 ||
                    logicalBinding.SchemaVersion != 1 || logicalBinding.ContentVersion != 1 || logicalBinding.DefinitionVersion != 2 ||
                    logicalBinding.DefinitionId != "skirmish.s049" || logicalBinding.SourceOperationMapId != source.OperationMapId ||
                    logicalBinding.SourceSceneGuid != source.SourceSceneReference.AssetGUID || logicalBinding.SourceContentHash != source.ContentHash ||
                    logicalBinding.SourceSemanticFingerprint != sourceFingerprint || logicalBinding.LayoutId != layout.LayoutId ||
                    logicalBinding.ReviewedLayoutSemanticFingerprint != S049LayoutFingerprint ||
                    logicalBinding.ObjectiveKind != objective.Kind ||
                    logicalBinding.Variants == null || logicalBinding.Variants.Length != 3 ||
                    logicalBinding.Variants.Any(v => v == null || v.Areas != null || v.Difficulty != SkirmishDifficultyId.Regular) ||
                    !logicalBinding.Variants.Select(v => v.Size).OrderBy(v => v).SequenceEqual(
                        new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar }.OrderBy(v => v)))
                    throw new InvalidOperationException("Retained S049 logical binding did not rehydrate to its exact three Regular typed variants");
                definition.BindMissionLogicalBinding(logicalBinding);
                var context = CaptureCompiledS049MetadataContext(104734, baseline.buildingPlacement, scene, definition,
                    "S049", SkirmishDifficultyId.Regular, SkirmishSizeId.Standard);
                uint? actualSetupHash = (uint?)context["setup_hash"];
                string actualMetadataKey = (string)context["metadata_key_sha256"];
                if (actualSetupHash != 971644661u || string.IsNullOrEmpty(actualMetadataKey))
                    throw new InvalidOperationException("Current S049 compiler context differs from retained exact setup hash: actual_setup_hash=" +
                        (actualSetupHash.HasValue ? actualSetupHash.Value.ToString("x8") : "<null>") + " expected_setup_hash=39ea1ef5 metadata_key=" +
                        (actualMetadataKey ?? "<null>") + " catalog=" + (string)context["catalog_id"] + " size=" + (string)context["size"] +
                        " difficulty=" + (string)context["difficulty"] + " seed=" + (string)context["seed"] +
                        " layout_binding=" + (string)context["layout_binding_fingerprint"] +
                        " mission_logical_binding=" + (string)context["mission_logical_binding_fingerprint"]);
                if ((string)context["layout_binding_fingerprint"] != (string)logicalTarget["layout_binding_fingerprint"] ||
                    (string)context["mission_logical_binding_fingerprint"] != (string)logicalTarget["mission_logical_binding_fingerprint"])
                    throw new InvalidOperationException("Current S049 compiler binding fingerprints differ from retained exact target context");
                string historicalPath = Path.Combine(projectRoot, S049HistoricalMetadata);
                RequireHash(historicalPath, S049HistoricalMetadataSha, "historical native S049 metadata projection");
                JObject historical = JObject.Parse(File.ReadAllText(historicalPath));
                JObject historicalContext = historical["context"] as JObject;
                if ((string)historical["kind"] != "NativeS049DirectMetadataProjection" ||
                    (string)historical["native_artifact_sha256"] != "5b1852e889ff1e19e9dd5cfdeedc157d253990dc0d5963c82fe78abbd2cce040" ||
                    historicalContext == null || (int?)historicalContext["setup_hash"] != 971644661 ||
                    !JToken.DeepEquals(context["metadata"], historicalContext["metadata"]) ||
                    (string)context["metadata_key_sha256"] != (string)historicalContext["metadata_key_sha256"] ||
                    !JToken.DeepEquals(context["selected_native_domain"], historicalContext["selected_native_domain"]))
                    throw new InvalidOperationException("Current S049 original factory metadata differs from exact historical native capture");
                if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld)) throw new InvalidOperationException("Default World changed");

                var pins = SourcePins(new[] { AssetDatabase.GetAssetPath(baseline), AssetDatabase.GetAssetPath(baseline.buildingPlacement),
                    AssetDatabase.GetAssetPath(scene), AssetDatabase.GetAssetPath(scene.UnitPrefabRegistryConfig), AssetDatabase.GetAssetPath(expansion),
                    AssetDatabase.GetAssetPath(source), AssetDatabase.GetAssetPath(objective),
                    "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv" });
                pins[Path.GetFullPath(layoutPath)] = S049LayoutSha;
                pins[Path.GetFullPath(historicalPath)] = S049HistoricalMetadataSha;
                pins[Path.GetFullPath(logicalBindingPath)] = S049LogicalBindingSha;
                foreach (var copy in copiedHashes) if (SkirmishOfflineDataExporter.HashFile(copy.Key) != copy.Value)
                    throw new InvalidOperationException("Temporary source input changed during capture: " + copy.Key);
                pins.Remove(S049ObjectiveAsset);
                pins.Remove(S049ObjectiveAsset + ".meta");
                foreach (var pin in pins) if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value) throw new InvalidOperationException("Source drift: " + pin.Key);
                report = new JObject { ["kind"] = "NativeS049MetadataOnlyRecapture", ["scope"] = "CurrentCompilerAndOriginalMetadataFactoriesOnly",
                    ["context"] = context, ["source_pins"] = JObject.FromObject(pins),
                    ["campaign_source"] = new JObject { ["operation_map_id"] = source.OperationMapId, ["content_hash"] = source.ContentHash,
                        ["mission_source_fingerprint"] = sourceFingerprint, ["expected_mission_source_fingerprint"] = "c001f6c1849acf24b7b54bdcfbd3a5bc75679c98f071f12924fa5ad6e59a1020",
                        ["full_layout_source_fingerprint_observed"] = fullSourceFingerprint },
                    ["historical_metadata_parity"] = new JObject { ["artifact"] = S049HistoricalMetadata,
                        ["artifact_sha256"] = S049HistoricalMetadataSha, ["native_artifact_sha256"] = (string)historical["native_artifact_sha256"],
                        ["metadata_equal"] = true, ["metadata_digest_equal"] = true, ["selected_domain_equal"] = true },
                    ["candidate_asset_origin"] = new JObject { ["retained_source"] = S049ObjectiveSource, ["sha256"] = S049ObjectiveSha,
                        ["meta_sha256"] = S049ObjectiveMetaSha, ["layout_source"] = S049LayoutSource, ["layout_sha256"] = S049LayoutSha,
                        ["logical_binding_source"] = S049LogicalBinding, ["logical_binding_sha256"] = S049LogicalBindingSha,
                        ["r7_initialization_sha256"] = S049InitR7, ["r8_initialization_sha256"] = S049InitR8,
                        ["source_graph_r4_sha256"] = S049GraphR4, ["compiler_context_r6_sha256"] = S049CompilerR6 },
                    ["physical_map_created_or_changed"] = false, ["mission_initialized"] = false, ["runtime_state_transferred"] = false,
                    ["native_launch_provider_selected"] = false, ["public_admission_changed"] = false };
            }
            finally
            {
                if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
                if (layout != null) UnityEngine.Object.DestroyImmediate(layout);
                if (logicalBinding != null) UnityEngine.Object.DestroyImmediate(logicalBinding);
                foreach (var view in logicalViews) if (view != null) UnityEngine.Object.DestroyImmediate(view);
                if (objective != null) Resources.UnloadAsset(objective);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var pair in copiedHashes)
                {
                    if (File.Exists(pair.Key) && SkirmishOfflineDataExporter.HashFile(pair.Key) == pair.Value) File.Delete(pair.Key);
                    else if (File.Exists(pair.Key)) throw new InvalidOperationException("Temporary imported S049 asset changed during capture; preserved for review: " + pair.Key);
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            if (report == null) throw new InvalidOperationException("S049 metadata report was not produced");
            Directory.CreateDirectory(output);
            byte[] bytes = new System.Text.UTF8Encoding(false).GetBytes(report.ToString(Formatting.Indented) + "\n");
            using (var stream = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            Debug.Log("[SkirmishS049Metadata] result=Passed setup=971644661 scope=MetadataOnly");
        }

        static JObject CaptureCompiledS049MetadataContext(int seed, BuildingPlacementSystemConfig baseline, BuildingPlacementSystemConfig scene,
            SkirmishScenarioDefinitionConfig definition, string expectedCatalog, SkirmishDifficultyId difficulty, SkirmishSizeId size)
        {
            if (expectedCatalog != "S049" || definition == null || definition.CatalogId != expectedCatalog) throw new InvalidOperationException("Exact compiled metadata definition required");
            if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string error)) throw new InvalidOperationException(error);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds ?? Array.Empty<string>() };
            if (!SkirmishSetupCompiler.TryCompile(definition, difficulty, size, seed,
                manifest, matrix, out var setup, out var reasons))
                throw new InvalidOperationException("S049 setup compile rejected the retained binding: " +
                    DiagnoseS049LogicalBinding(definition, difficulty, size, seed, manifest, matrix) +
                    " compiler_reasons=" + string.Join(";", reasons));
            RequireCompiledMetadataContext(setup, seed, expectedCatalog, difficulty, size);
            uint originalHash = setup.SetupHash;
            bool wrongHashRejected = false;
            try
            {
                setup.SetupHash = originalHash ^ 1u;
                try { RequireCompiledMetadataContext(setup, seed, expectedCatalog, difficulty, size); }
                catch (InvalidOperationException) { wrongHashRejected = true; }
            }
            finally { setup.SetupHash = originalHash; }
            if (!wrongHashRejected) throw new InvalidOperationException("Changed context hash was accepted");
            bool wrongSeedRejected = false;
            try { RequireCompiledMetadataContext(setup, seed == 1 ? 104731 : 1, expectedCatalog, difficulty, size); }
            catch (InvalidOperationException) { wrongSeedRejected = true; }
            if (!wrongSeedRejected) throw new InvalidOperationException("Wrong target seed was accepted");
            var actualTimingFactory = LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper").GetMethod("TryGetMetadata");
            var actualCreate = typeof(SkirmishProductionCatalog).GetMethods().Single(m => m.Name == "Create" && m.GetParameters().Length == 4);
            var timingResolver = Delegate.CreateDelegate(actualCreate.GetParameters()[3].ParameterType, actualTimingFactory);
            using var production = (SkirmishProductionCatalog)actualCreate.Invoke(null, new object[] { setup, baseline, scene, timingResolver });
            var buildings = production.Config.Spawnables.ToList();
            var units = production.Config.UnitPrefabRegistryConfig.UnitSpawnPrefabs.ToList();
            bool legacyS002Domain = expectedCatalog == "S002";
            if (expectedCatalog != "S049") throw new NotSupportedException("S049-only mirrored metadata capture requires exact S049 context");
            if (legacyS002Domain ? buildings.Count != 8 || units.Count != 12 :
                buildings.Count < 1 || buildings.Count > 64 || units.Count < 1 || units.Count > 64)
                throw new InvalidOperationException("Finite native source domain changed catalog=" + expectedCatalog + " buildings=" + buildings.Count + " units=" + units.Count + " expected=" + (legacyS002Domain ? "8/12" : "1..64/1..64"));
            var buildingKeys = CaptureUniqueCompiledDomain(buildings, expectedCatalog + "/buildings");
            var unitKeys = CaptureUniqueCompiledDomain(units, expectedCatalog + "/units");
            var missing = new JArray();
            var metadata = new JObject { ["providers"] = new JObject { ["baseline_asset"] = SourceAsset(baseline), ["scene_asset"] = SourceAsset(scene),
                ["unit_registry_asset"] = SourceAsset(scene.UnitPrefabRegistryConfig), ["baseline"] = CapturePrefabList(baseline.Spawnables),
                ["scene"] = CapturePrefabList(scene.Spawnables), ["units"] = CapturePrefabList(scene.UnitPrefabRegistryConfig.UnitSpawnPrefabs) },
                ["catalog_inputs"] = new JObject { ["catalog_id"] = setup.CatalogId, ["army_profile"] = setup.ArmyProfileId.ToString(),
                    ["structure_ids"] = new JArray(setup.Structures.Select(s => s.StructureId)),
                    ["role_overlays"] = CaptureDefinitionValue(setup.RoleOverlays, "catalog/roles", missing, 0) } };
            var recipes = new JArray();
            foreach (var recipe in production.Config.RuntimeProductionRecipes)
            {
                if (recipe.ProducerPrefab == null || recipe.UnitPrefab == null || !buildings.Contains(recipe.ProducerPrefab) || !units.Contains(recipe.UnitPrefab))
                    throw new InvalidOperationException("Original recipe outside selected native domain catalog=" + expectedCatalog + " buildings=" + buildings.Count + " units=" + units.Count + " recipe=" + recipes.Count + " producer=" + (recipe.ProducerPrefab == null ? "<null>" : recipe.ProducerPrefab.name) + " unit=" + (recipe.UnitPrefab == null ? "<null>" : recipe.UnitPrefab.name));
                recipes.Add(new JObject { ["ProducerPrefab"] = SourceAsset(recipe.ProducerPrefab), ["UnitPrefab"] = SourceAsset(recipe.UnitPrefab),
                    ["Quantity"] = recipe.Quantity, ["MaterialsCost"] = recipe.MaterialsCost, ["CreditsCost"] = recipe.CreditsCost,
                    ["UseProducerGroundExit"] = recipe.UseProducerGroundExit });
            }
            if (recipes.Count == 0) throw new InvalidOperationException("Original recipe output absent");
            metadata["recipes"] = recipes;
            var deliveries = new JArray();
            foreach (var overlay in setup.RoleOverlays)
            {
                bool found = production.TryReadProductionDeliverySeconds(overlay.RoleKind, out float seconds);
                deliveries.Add(new JObject { ["role"] = overlay.RoleKind.ToString(), ["found"] = found,
                    ["seconds"] = CaptureDefinitionValue(seconds, "catalog/delivery", missing, 0) });
            }
            metadata["delivery_seconds"] = deliveries;
            Type helperType = LoadedType("Game.Runtime.BuildingDefinitionPrefabSystemHelper");
            Type resolverType = LoadedType("Game.Composition.BuildingDefinitionAuthoringMetadataPrefabSystemHelper");
            object helper = Activator.CreateInstance(helperType, true);
            var configure = helperType.GetMethod("ConfigureAuthoringMetadataResolvers");
            var cp = configure.GetParameters();
            configure.Invoke(helper, new object[] {
                Delegate.CreateDelegate(cp[0].ParameterType, resolverType.GetMethod("TryGetBuildingDefinitionMetadata")),
                Delegate.CreateDelegate(cp[1].ParameterType, resolverType.GetMethod("TryGetUnitDefinitionMetadata")) });
            helperType.GetMethod("RebuildSpawnablesLookup").Invoke(helper, new object[] { buildings, units });
            helperType.GetMethod("ConfigureProductionRecipes").Invoke(helper, new object[] { production.Config.RuntimeProductionRecipes });
            var rebuild = helperType.GetMethod("RebuildConfiguredSpawnableDefinitions");
            object destroy = Delegate.CreateDelegate(rebuild.GetParameters()[1].ParameterType,
                typeof(SkirmishBakedActorSnapshotExporter).GetMethod(nameof(DestroyTwoSeedObject), BindingFlags.Static | BindingFlags.NonPublic));
            var rows = new JArray();
            using (var world = new World("TwoSeedMetadataOnly", WorldFlags.Game))
            {
                try
                {
                    var runway = world.GetOrCreateSystemManaged(LoadedType("Game.Runtime.BuildingRunwaySystem"));
                    foreach (var prefab in buildings)
                    {
                        object value = helperType.GetMethod("CreateRuntimeBuildingDefinition").Invoke(helper,
                            new object[] { prefab, prefab.name, "Operational building.", Vector2Int.one, 500, runway });
                        var fields = new JObject();
                        foreach (var field in value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                            fields[field.Name] = CaptureDefinitionValue(field.GetValue(value), prefab.name + "/" + field.Name, missing, 0);
                        rows.Add(new JObject { ["index"] = rows.Count, ["prefab"] = SourceAsset(prefab), ["fields"] = fields,
                            ["construction_index"] = baseline.Spawnables.ToList().IndexOf(prefab), ["scene_index"] = scene.Spawnables.ToList().IndexOf(prefab) });
                    }
                    rebuild.Invoke(helper, new object[] { runway, destroy });
                    MethodInfo get = helperType.GetMethods().Single(m => m.Name == "TryGetConfiguredSpawnable" && m.GetParameters()[0].ParameterType == typeof(GameObject));
                    for (int i = 0; i < buildings.Count; i++)
                    {
                        object[] call = { buildings[i], null };
                        if (!(bool)get.Invoke(helper, call)) throw new InvalidOperationException("Original configured factory rejected selected prefab");
                        rows[i]["configured_entry"] = CaptureMetadataFields(call[1], buildings[i].name + "/entry", missing);
                    }
                }
                finally { helperType.GetMethod("ClearConfiguredSpawnableDefinitions").Invoke(helper, new[] { destroy }); }
            }
            metadata["buildings"] = rows;
            var unitRows = new JArray();
            var transports = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            var productionFactory = LoadedType("Game.Composition.BuildingProductionUnitMetadataPrefabSystemHelper").GetMethod("TryGetMetadata");
            var definitionFactory = resolverType.GetMethod("TryGetUnitDefinitionMetadata");
            foreach (var prefab in units)
            {
                object[] pa = { prefab, null }, da = { prefab, null };
                bool hd = (bool)definitionFactory.Invoke(null, da), hp = (bool)productionFactory.Invoke(null, pa);
                var row = new JObject { ["index"] = unitRows.Count, ["prefab"] = SourceAsset(prefab), ["has_definition_metadata"] = hd, ["has_production_metadata"] = hp };
                if (hd) row["definition_metadata"] = CaptureMetadataFields(da[1], prefab.name + "/definition", missing);
                if (hd || hp) row["authoring_catalog_properties"] = CaptureUnitCatalogAuthoringProperties(prefab, missing);
                if (hp)
                {
                    row["production_metadata"] = CaptureMetadataFields(pa[1], prefab.name + "/production", missing);
                    var transport = pa[1].GetType().GetField("ProductionTransportPrefab").GetValue(pa[1]) as GameObject;
                    if (transport != null) transports[AssetDatabase.GetAssetPath(transport)] = transport;
                }
                unitRows.Add(row);
            }
            metadata["units"] = unitRows;
            var transportRows = new JArray();
            foreach (var pair in transports.OrderBy(p => p.Key, StringComparer.Ordinal)) transportRows.Add(CaptureSourceTransportGraph(pair.Value, missing));
            metadata["transports"] = transportRows;
            if (missing.Count != 0) throw new InvalidOperationException("Unsupported metadata values: " + missing);
            return new JObject { ["catalog_id"] = expectedCatalog, ["difficulty"] = difficulty.ToString(), ["size"] = size.ToString(), ["seed"] = seed,
                ["setup_hash"] = setup.SetupHash, ["layout_binding_fingerprint"] = setup.LayoutBindingFingerprint,
                ["mission_logical_binding_fingerprint"] = setup.MissionLogicalBindingFingerprint,
                ["wrong_context_hash_rejected"] = wrongHashRejected, ["wrong_target_seed_rejected"] = wrongSeedRejected,
                ["selected_native_domain"] = new JObject { ["building_count"] = buildings.Count, ["unit_count"] = units.Count,
                    ["building_keys"] = buildingKeys, ["unit_keys"] = unitKeys, ["recipe_count"] = recipes.Count,
                    ["all_recipe_producers_and_units_in_selected_domain"] = true }, ["metadata_key_sha256"] = MetadataDigest(metadata), ["metadata"] = metadata };
        }

        static string DiagnoseS049LogicalBinding(SkirmishScenarioDefinitionConfig definition, SkirmishDifficultyId difficulty,
            SkirmishSizeId size, int seed, SkirmishContentManifest manifest, IReadOnlyList<SkirmishSetupMatrixRow> matrix)
        {
            var retained = definition.MissionLogicalBinding;
            SkirmishResolvedSetup unboundSetup = null;
            List<SkirmishCompileReason> baseReasons = null;
            bool baseCompiled;
            try
            {
                definition.BindMissionLogicalBinding(null);
                baseCompiled = SkirmishSetupCompiler.TryCompile(definition, difficulty, size, seed, manifest, matrix,
                    out unboundSetup, out baseReasons);
            }
            finally { definition.BindMissionLogicalBinding(retained); }
            if (!baseCompiled) return "unbound_base_compile_failed=" + string.Join(";", baseReasons ?? new List<SkirmishCompileReason>());
            var mismatches = new List<string>();
            var config = definition.MissionLogicalBinding;
            var source = definition.CampaignMap;
            var layout = definition.MapLayout;
            if (config == null) mismatches.Add("binding=null");
            else
            {
                if (config.SchemaVersion != 1) mismatches.Add("schema=" + config.SchemaVersion);
                if (config.ContentVersion <= 0) mismatches.Add("binding_content_version=" + config.ContentVersion);
                if (config.ValueFormat != layout.BindingValueFormat) mismatches.Add("value_format=" + config.ValueFormat + "/" + layout.BindingValueFormat);
                if (config.CatalogId != definition.CatalogId) mismatches.Add("catalog=" + config.CatalogId + "/" + definition.CatalogId);
                if (config.DefinitionId != definition.DefinitionId) mismatches.Add("definition_id=" + config.DefinitionId + "/" + definition.DefinitionId);
                if (config.DefinitionVersion != definition.ContentVersion) mismatches.Add("definition_version=" + config.DefinitionVersion + "/" + definition.ContentVersion);
                if (config.ObjectiveKind != definition.ObjectiveConfig.Kind) mismatches.Add("objective_kind=" + config.ObjectiveKind + "/" + definition.ObjectiveConfig.Kind);
                if (config.SourceOperationMapId != source.OperationMapId) mismatches.Add("source_operation_map=" + config.SourceOperationMapId + "/" + source.OperationMapId);
                if (config.SourceSceneGuid != source.SourceSceneReference.AssetGUID) mismatches.Add("source_scene_guid=" + config.SourceSceneGuid + "/" + source.SourceSceneReference.AssetGUID);
                if (config.SourceContentHash != source.ContentHash) mismatches.Add("source_content_hash=" + config.SourceContentHash + "/" + source.ContentHash);
                if (config.LayoutId != layout.LayoutId) mismatches.Add("layout_id=" + config.LayoutId + "/" + layout.LayoutId);
                if (config.SourceSemanticFingerprint != SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source, config.ValueFormat))
                    mismatches.Add("source_semantic_fingerprint=" + config.SourceSemanticFingerprint + "/" + SkirmishMissionLogicalBindingCompiler.SourceFingerprint(source, config.ValueFormat));
                if (config.ReviewedLayoutSemanticFingerprint != SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout))
                    mismatches.Add("reviewed_layout_fingerprint=" + config.ReviewedLayoutSemanticFingerprint + "/" + SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout));
                if (config.Variants == null || config.Variants.Length == 0 || config.Variants.Length > 12)
                    mismatches.Add("variant_count=" + (config.Variants == null ? "<null>" : config.Variants.Length.ToString(CultureInfo.InvariantCulture)));
            }
            if (!source.TryValidateMetadata(out string sourceMetadataError)) mismatches.Add("source_metadata=" + sourceMetadataError);
            if (!source.TryValidateLocalContentReferences(out string sourceReferenceError)) mismatches.Add("source_references=" + sourceReferenceError);
            bool logicalCompiled = SkirmishMissionLogicalBindingCompiler.TryCompile(definition, unboundSetup,
                out var logical, out string logicalError);
            var viewDiagnostics = DiagnoseS049LogicalView(definition, unboundSetup);
            return "unbound_setup_hash=" + unboundSetup.SetupHash.ToString("x8") +
                " layout_binding=" + (unboundSetup.LayoutBindingFingerprint ?? "<null>") +
                " logical_binding_ok=" + logicalCompiled + " logical_binding_error=" + (logicalError ?? "<null>") +
                " logical_fingerprint=" + (logical?.Fingerprint ?? "<null>") +
                " identity_mismatches=" + (mismatches.Count == 0 ? "<none>" : string.Join(";", mismatches)) +
                " logical_view_checks=" + string.Join(";", viewDiagnostics);
        }

        static List<string> DiagnoseS049LogicalView(SkirmishScenarioDefinitionConfig definition, SkirmishResolvedSetup setup)
        {
            var checks = new List<string>();
            var config = definition?.MissionLogicalBinding;
            var selected = config?.Variants?.SingleOrDefault(v => v != null && v.Size == setup.SizeId && v.Difficulty == setup.DifficultyId);
            if (selected == null) { checks.Add("selected_variant=<missing-or-ambiguous>"); return checks; }
            var view = selected.View;
            checks.Add("source_elevation_min=" + (selected.SourceMinimumElevation == definition.CampaignMap.SurfaceMetadata.MinimumHeight));
            checks.Add("source_elevation_max=" + (selected.SourceMaximumElevation == definition.CampaignMap.SurfaceMetadata.MaximumHeight));
            checks.Add("view_nonnull=" + (view != null));
            if (view == null) return checks;
            checks.Add("view_valid=" + view.IsValid);
            checks.Add("view_uses_active_map=" + view.UsesActiveMapIdentity);
            checks.Add("view_opening_enum=" + Enum.IsDefined(typeof(SkirmishMissionOpeningFocusPolicy), view.OpeningFocusPolicy));
            checks.Add("view_opening_contract=" + (view.OpeningFocusPolicy == SkirmishMissionOpeningFocusPolicy.None ||
                view.OpeningFocusPolicy == SkirmishMissionOpeningFocusPolicy.FirstOpeningOwnedRifleGroup && view.UsesActiveMapIdentity ||
                !view.AuthoredNonBaseAssault && !view.AuthoredGenericBaseAssault && view.CatalogId == "S005" && view.DefinitionId == "skirmish.s005"));
            bool viewIdentityContract = view.AuthoredGenericBaseAssault && !view.AuthoredNonBaseAssault &&
                view.ObjectiveKind == SkirmishObjectiveKind.BaseAssault &&
                SkirmishMissionDispatch.RequiresSourceBoundDefinition(view.CatalogId, view.DefinitionVersion) &&
                view.CatalogId == definition.CatalogId && view.DefinitionId == definition.DefinitionId;
            checks.Add("view_identity_contract=" + viewIdentityContract);
            checks.Add("view_schema_versions=" + (view.SchemaVersion == 1 && view.ContentVersion > 0 && view.DefinitionVersion > 0));
            checks.Add("view_campaign_layout_hashes=" + (!string.IsNullOrWhiteSpace(view.CampaignSourceContentHash) &&
                !string.IsNullOrWhiteSpace(view.LayoutContentHash)));
            checks.Add("view_height_bounds=" + (float.IsFinite(view.MinHeight) && view.MinHeight > 0 && float.IsFinite(view.MaxHeight) &&
                view.MaxHeight >= view.MinHeight && view.NormalHeight >= view.MinHeight && view.NormalHeight <= view.MaxHeight &&
                view.BuildHeight >= view.NormalHeight && view.BuildHeight <= view.MaxHeight));
            checks.Add("view_lens_bounds=" + (float.IsFinite(view.Pitch) && view.Pitch > 0 && view.Pitch < 90 && float.IsFinite(view.Yaw) &&
                float.IsFinite(view.NormalFieldOfView) && view.NormalFieldOfView > 0 && view.NormalFieldOfView < 179 &&
                float.IsFinite(view.GroundReferenceY) && float.IsFinite(view.MaximumGroundCenterSlantDistance) && view.MaximumGroundCenterSlantDistance > 0 &&
                view.MaxHeight <= view.GroundReferenceY + view.MaximumGroundCenterSlantDistance * Mathf.Sin(view.Pitch * Mathf.Deg2Rad)));
            checks.Add("view_build_aircraft_bounds=" + (float.IsFinite(view.BuildFieldOfView) && view.BuildFieldOfView > 0 && view.BuildFieldOfView < 179 &&
                float.IsFinite(view.InfantryMinimumViewportHeight) && view.InfantryMinimumViewportHeight > 0 && view.InfantryMinimumViewportHeight < 1 &&
                float.IsFinite(view.PlaneMaximumViewportDimension) && view.PlaneMaximumViewportDimension > 0 && view.PlaneMaximumViewportDimension < 1 &&
                float.IsFinite(view.AircraftPitch) && view.AircraftPitch > 0 && view.AircraftPitch < 90 &&
                float.IsFinite(view.AircraftFieldOfView) && view.AircraftFieldOfView > 0 && view.AircraftFieldOfView < 179 &&
                float.IsFinite(view.AircraftMinimumDistance) && view.AircraftMinimumDistance > 0 &&
                float.IsFinite(view.AircraftMaximumDistance) && view.AircraftMaximumDistance >= view.AircraftMinimumDistance));
            checks.Add("catalog_match=" + (setup.CatalogId == view.CatalogId && definition.CatalogId == view.CatalogId));
            checks.Add("definition_match=" + (setup.DefinitionId == view.DefinitionId));
            checks.Add("definition_version_match=" + (setup.ContentVersion == view.DefinitionVersion && definition.ContentVersion == view.DefinitionVersion));
            checks.Add("campaign_source_hash_match=" + (definition.CampaignMap.ContentHash == view.CampaignSourceContentHash));
            checks.Add("layout_hash_match=" + (definition.MapLayout.ContentHash == view.LayoutContentHash));
            checks.Add("layout_id_match=" + (setup.LayoutId == definition.MapLayoutId));
            checks.Add("operation_map_match=" + (setup.OperationMapId == definition.OperationMapId));
            checks.Add("objective_match=" + (setup.ObjectiveKind == view.ObjectiveKind && definition.ObjectiveConfig.Kind == view.ObjectiveKind));
            checks.Add("authored_ba=" + view.AuthoredGenericBaseAssault);
            checks.Add("ground_reference_match=" + (view.GroundReferenceY == selected.SourceMinimumElevation));
            bool framingValid = SkirmishMissionLogicalBindingCompiler.ValidateFraming(selected, setup, out string framingError);
            checks.Add("framing_valid=" + framingValid + ":" + (framingError ?? "<null>"));
            string computedViewFingerprint = SkirmishMissionLogicalBindingCompiler.ViewFingerprint(selected);
            checks.Add("reviewed_view_fingerprint_match=" + (selected.ReviewedViewFingerprint == computedViewFingerprint));
            checks.Add("reviewed_view_fingerprint=" + selected.ReviewedViewFingerprint);
            checks.Add("computed_view_fingerprint=" + computedViewFingerprint);
            checks.Add("view_matches=" + view.Matches(setup, definition));
            return checks;
        }

        static JToken DecodePortableLogicalValue(JToken value, int depth)
        {
            if (depth > 16) throw new InvalidOperationException("Retained S049 logical binding nesting exceeded");
            if (value is JObject obj)
            {
                if (obj.Count == 1 && obj["float_bits"]?.Type == JTokenType.String)
                {
                    string encoded = (string)obj["float_bits"];
                    if (encoded == null || encoded.Length != 8 || !uint.TryParse(encoded, NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out uint bits)) throw new InvalidOperationException("Invalid retained logical float bits");
                    float number = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
                    if (!float.IsFinite(number)) throw new InvalidOperationException("Non-finite retained logical float");
                    return new JValue(number);
                }
                var decoded = new JObject();
                foreach (var property in obj.Properties()) decoded[property.Name] = DecodePortableLogicalValue(property.Value, depth + 1);
                return decoded;
            }
            if (value is JArray array)
            {
                if (array.Count > 256) throw new InvalidOperationException("Retained logical array exceeded bound");
                var decoded = new JArray();
                foreach (var item in array) decoded.Add(DecodePortableLogicalValue(item, depth + 1));
                return decoded;
            }
            return value?.DeepClone() ?? JValue.CreateNull();
        }

        static void RequireHash(string path, string expected, string label)
        {
            if (!File.Exists(path) || SkirmishOfflineDataExporter.HashFile(path) != expected)
                throw new InvalidOperationException("Exact " + label + " bytes required: " + path);
        }
    }
}
#endif
