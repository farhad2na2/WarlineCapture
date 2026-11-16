#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Creates reviewable, unreferenced logical assets from the retained S049
    // authoring inputs. This deliberately never edits either runtime catalog.
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        const string StageRoot = "Assets/Game/Configs/SkirmishExpansion/StagedS049";
        const string CandidateObjective = "Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishObjective_S049Candidate_20261028.asset";
        const string RetainedLayout = "Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r1/layout.json";
        const string RetainedBinding = "Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r6/logical-binding.json";
        const string ArmorBreakMap = "Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset";
        const string CandidateObjectiveSha = "ee70c46ca4e2c07edf766124d717361ae6b761f76dfb7312908fa63d460c10fb";
        const string CandidateMetaSha = "04191e2f1916451ff77e709ae9316e248e5e2f3432b56da5218f109367fd1021";
        const string LayoutSha = "fad258a220248c4a60da9cd7b8f097c2eff08b20871bef8fbd3be919d260d7c7";
        const string MapSha = "181418e5b316d8a5625913d0251e5f5a9a83ab81f56baec49e4adcb2bb89e3b4";
        const string BindingSha = "3bd7f60ae0ccf9e3ecba42247a12c00aa3b52a9d06b0f2494ebcbff01c3d95e5";
        const string SourceContentHash = "da42d5bc876e1f2eed02b3d7050d41759c010de973dfd7c51c51eba1a26e5a86";
        const string SourceFingerprint = "c001f6c1849acf24b7b54bdcfbd3a5bc75679c98f071f12924fa5ad6e59a1020";
        const string LayoutFingerprint = "9e94e1a6be531a77829fa04e8672b660c3a31e7f479a51653f1c89c92a9649cb";
        const int Seed = 104734;

        public static void RunS049StagedLogicalAssetBuilder()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Idle Editor required");
            var args = Environment.GetCommandLineArgs();
            int arg = Array.IndexOf(args, "-skirmishS049StageReceipt");
            if (arg < 0 || arg + 1 >= args.Length) throw new ArgumentException("Explicit fresh /private/tmp receipt path required");
            string receiptPath = Path.GetFullPath(args[arg + 1]);
            if (!string.Equals(Path.GetDirectoryName(receiptPath), "/private/tmp", StringComparison.Ordinal) || File.Exists(receiptPath))
                throw new InvalidOperationException("Fresh create-new receipt directly under /private/tmp required");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string stage = Path.Combine(root, StageRoot);
            if (!Directory.Exists(Path.GetDirectoryName(stage)))
                throw new InvalidOperationException("Existing SkirmishExpansion parent required for staging; builder will not create ancestor folders");
            if (Directory.Exists(stage) || File.Exists(stage) || File.Exists(stage + ".meta") || AssetDatabase.LoadMainAssetAtPath(StageRoot) != null)
                throw new InvalidOperationException("Fresh unreferenced S049 staging root required; existing assets are never overwritten");

            var createdPaths = new List<string>();
            var ownedFileHashes = new Dictionary<string, string>(StringComparer.Ordinal);
            var createdObjects = new List<UnityEngine.Object>();
            var createdFolders = new List<string>();
            var createdFolderMetaHashes = new Dictionary<string, string>(StringComparer.Ordinal);
            var originalWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            bool completed = false;
            string expansionPath = "Assets/Game/Resources/SkirmishExpansionCatalog.asset";
            string battlePath = "Assets/Game/Resources/SkirmishBattleCatalog.asset";
            string expansionSha = SkirmishOfflineDataExporter.HashFile(Path.Combine(root, expansionPath));
            string battleSha = SkirmishOfflineDataExporter.HashFile(Path.Combine(root, battlePath));
            string[] pinRoots = { ArmorBreakMap, expansionPath, battlePath,
                "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv" };
            var sourcePins = SourcePins(pinRoots);
            try
            {
                string objectiveSource = Path.Combine(root, CandidateObjective);
                string objectiveMetaSource = objectiveSource + ".meta";
                string layoutSource = Path.Combine(root, RetainedLayout);
                string bindingSource = Path.Combine(root, RetainedBinding);
                RequireHash(objectiveSource, CandidateObjectiveSha, "retained S049 objective");
                RequireHash(objectiveMetaSource, CandidateMetaSha, "retained S049 objective GUID");
                RequireHash(layoutSource, LayoutSha, "retained S049 layout");
                RequireHash(bindingSource, BindingSha, "retained S049 logical binding");
                RequireHash(Path.Combine(root, ArmorBreakMap), MapSha, "existing Armor Break Campaign source");
                string objectiveMeta = File.ReadAllText(objectiveMetaSource);
                Match guidMatch = Regex.Match(objectiveMeta, "(?m)^guid:\\s*([0-9a-f]{32})\\s*$");
                if (!guidMatch.Success || !Regex.IsMatch(guidMatch.Groups[1].Value, "^[0-9a-f]{32}$"))
                    throw new InvalidOperationException("Retained S049 objective meta must contain one valid GUID");
                string existingGuidPath = AssetDatabase.GUIDToAssetPath(guidMatch.Groups[1].Value);
                if (!string.IsNullOrEmpty(existingGuidPath))
                    throw new InvalidOperationException("Retained S049 objective GUID is already assigned to " + existingGuidPath);

                Directory.CreateDirectory(stage); createdFolders.Add(stage);
                string objectivePath = StageRoot + "/SkirmishObjective_S049.asset";
                File.Copy(objectiveSource, Path.Combine(root, objectivePath), false); createdPaths.Add(objectivePath);
                File.Copy(objectiveMetaSource, Path.Combine(root, objectivePath + ".meta"), false); createdPaths.Add(objectivePath + ".meta");
                ownedFileHashes[objectivePath] = CandidateObjectiveSha;
                ownedFileHashes[objectivePath + ".meta"] = CandidateMetaSha;
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (string folder in createdFolders)
                {
                    string metaPath = folder + ".meta";
                    if (File.Exists(metaPath)) createdFolderMetaHashes[folder] = SkirmishOfflineDataExporter.HashFile(metaPath);
                }
                var objective = AssetDatabase.LoadAssetAtPath<SkirmishObjectiveConfig>(objectivePath);
                var source = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(ArmorBreakMap);
                if (objective == null || source == null || source.ContentHash != SourceContentHash ||
                    SkirmishBindingCanonicalValues.SourceFingerprint(source) != SourceFingerprint)
                    throw new InvalidOperationException("Pinned S049 objective or Campaign source did not import with exact identity");

                JObject layoutDoc;
                using (var text = new StringReader(File.ReadAllText(layoutSource)))
                using (var reader = new JsonTextReader(text) { DateParseHandling = DateParseHandling.None })
                {
                    layoutDoc = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    if (reader.Read()) throw new InvalidOperationException("Exactly one retained S049 layout object required");
                }
                var layout = SkirmishFrozenLayoutFieldImporter.Read(layoutDoc);
                if (layout == null || layout.LayoutId != "layout.s049.measured-pad-candidate.v2" ||
                    layout.BindingValueFormat != SkirmishBindingValueFormat.CanonicalBinaryV2 ||
                    SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout) != LayoutFingerprint)
                    throw new InvalidOperationException("Retained exact typed S049 layout failed original importer/fingerprint");

                JObject bindingDoc = JObject.Parse(File.ReadAllText(bindingSource));
                JObject portable = bindingDoc["portable_binding"] as JObject;
                JObject fields = portable?["fields"] as JObject;
                if ((string)bindingDoc["kind"] != "OriginalCompilerDeclaredLogicalBinding" ||
                    (bool?)bindingDoc["native_capture"] != false || (bool?)bindingDoc["source_fit_certified"] != false ||
                    (bool?)bindingDoc["mission_ready"] != false || fields == null ||
                    (string)bindingDoc["target_context"]?["catalog_id"] != "S049" ||
                    (int?)bindingDoc["target_context"]?["seed"] != Seed ||
                    (int?)bindingDoc["target_context"]?["setup_hash"] != 971644661 ||
                    (string)bindingDoc["target_context"]?["source_content_hash"] != SourceContentHash ||
                    (string)bindingDoc["target_context"]?["source_fingerprint"] != SourceFingerprint ||
                    (string)bindingDoc["target_context"]?["layout_fingerprint"] != LayoutFingerprint)
                    throw new InvalidOperationException("Retained S049 binding is not the exact non-ready compiler-declared target");
                var decoded = DecodePortableLogicalValue(fields, 0) as JObject;
                if (decoded == null) throw new InvalidOperationException("Portable logical binding could not be decoded");
                var binding = ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>(); createdObjects.Add(binding);
                JsonUtility.FromJsonOverwrite(decoded.ToString(Formatting.None), binding);
                JArray variants = decoded["Variants"] as JArray;
                if (variants == null || binding.Variants == null || binding.Variants.Length != 3 || variants.Count != 3)
                    throw new InvalidOperationException("Three retained typed logical variants required");
                var viewObjects = new List<SkirmishMissionViewProfile>();
                for (int i = 0; i < variants.Count; i++)
                {
                    JObject viewFields = variants[i]?["View"] as JObject;
                    if (viewFields == null || binding.Variants[i] == null) throw new InvalidOperationException("Typed view variant missing");
                    var view = ScriptableObject.CreateInstance<SkirmishMissionViewProfile>(); createdObjects.Add(view); viewObjects.Add(view);
                    JsonUtility.FromJsonOverwrite(viewFields.ToString(Formatting.None), view);
                    binding.Variants[i].View = view;
                }
                if (binding.CatalogId != "S049" || binding.DefinitionId != "skirmish.s049" || binding.DefinitionVersion != 2 ||
                    binding.SourceContentHash != SourceContentHash || binding.SourceOperationMapId != source.OperationMapId ||
                    binding.ReviewedLayoutSemanticFingerprint != LayoutFingerprint)
                    throw new InvalidOperationException("Rehydrated S049 logical binding identity differs");

                string shared = "Assets/Game/Configs/SkirmishExpansion/Shared/";
                var definition = ScriptableObject.CreateInstance<SkirmishScenarioDefinitionConfig>(); createdObjects.Add(definition);
                definition.Configure("S049", objective,
                    AssetDatabase.LoadAssetAtPath<SkirmishArmyProfileConfig>(shared + "SkirmishArmy_GroundManeuver.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishStartPackageConfig>(shared + "SkirmishStart_Field.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishEconomyConfig>(shared + "SkirmishEconomy_V1.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishUpgradeConfig>(shared + "SkirmishUpgrade_V1.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishIntelConfig>(shared + "SkirmishIntel_SharedFog.asset"),
                    AssetDatabase.LoadAssetAtPath<SkirmishRoleCatalogConfig>(shared + "SkirmishRoleCatalog_V1.asset"), layout);
                definition.BindCampaignMap(source); definition.BindMissionLogicalBinding(binding);
                if (definition.CampaignMap != source || definition.MissionLogicalBinding != binding || definition.ContentVersion != 2)
                    throw new InvalidOperationException("S049 in-memory candidate references differ");

                if (!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError))
                    throw new InvalidOperationException(matrixError);
                var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
                var sizeResults = CompileSizes(definition, manifest, matrix);
                if (sizeResults.Count != 3 || (int)sizeResults[0]["setup_hash"] != 971644661)
                    throw new InvalidOperationException("S049 original compiler did not reproduce the pinned Standard hash and all sizes");

                var persistedLayout = UnityEngine.Object.Instantiate(layout); createdObjects.Add(persistedLayout);
                var persistedBinding = UnityEngine.Object.Instantiate(binding); createdObjects.Add(persistedBinding);
                var persistedViews = new List<SkirmishMissionViewProfile>();
                for (int i = 0; i < persistedBinding.Variants.Length; i++)
                {
                    var v = UnityEngine.Object.Instantiate(binding.Variants[i].View); createdObjects.Add(v); persistedViews.Add(v);
                    persistedBinding.Variants[i].View = v;
                }
                var persistedDefinition = UnityEngine.Object.Instantiate(definition); createdObjects.Add(persistedDefinition);
                var so = new SerializedObject(persistedDefinition);
                so.FindProperty("mapLayout").objectReferenceValue = persistedLayout;
                so.FindProperty("objectiveConfig").objectReferenceValue = objective;
                so.FindProperty("missionLogicalBinding").objectReferenceValue = persistedBinding;
                so.ApplyModifiedPropertiesWithoutUndo();

                var planned = new List<(UnityEngine.Object asset, string path)> {
                    (persistedLayout, StageRoot + "/Logical/Layout_S049.asset"),
                    (objective, objectivePath),
                };
                foreach (var variant in persistedBinding.Variants)
                {
                    var view = persistedViews[persistedBinding.Variants.ToList().IndexOf(variant)];
                    planned.Add((view, StageRoot + "/Logical/View_S049_" + variant.Size + "_" + variant.Difficulty + ".asset"));
                }
                planned.Add((persistedBinding, StageRoot + "/Logical/Binding_S049.asset"));
                planned.Add((persistedDefinition, StageRoot + "/SkirmishScenario_S049.asset"));
                if (planned.Select(p => p.path).Distinct().Count() != planned.Count ||
                    planned.Any(p => p.path != objectivePath && (File.Exists(Path.Combine(root, p.path)) || AssetDatabase.LoadMainAssetAtPath(p.path) != null)))
                    throw new InvalidOperationException("Every staged logical asset destination must be fresh");
                string logicalFolder = Path.Combine(root, StageRoot + "/Logical");
                Directory.CreateDirectory(logicalFolder); createdFolders.Add(logicalFolder);
                foreach (var item in planned)
                {
                    if (item.path == objectivePath) continue;
                    createdPaths.Add(item.path);
                    AssetDatabase.CreateAsset(item.asset, item.path);
                    string fullPath = Path.Combine(root, item.path);
                    string metaPath = fullPath + ".meta";
                    if (File.Exists(fullPath)) ownedFileHashes[item.path] = SkirmishOfflineDataExporter.HashFile(fullPath);
                    if (File.Exists(metaPath)) ownedFileHashes[item.path + ".meta"] = SkirmishOfflineDataExporter.HashFile(metaPath);
                }
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (var item in planned)
                {
                    if (item.path == objectivePath) continue;
                    ownedFileHashes[item.path] = SkirmishOfflineDataExporter.HashFile(Path.Combine(root, item.path));
                    ownedFileHashes[item.path + ".meta"] = SkirmishOfflineDataExporter.HashFile(Path.Combine(root, item.path + ".meta"));
                }

                var readback = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(StageRoot + "/SkirmishScenario_S049.asset");
                if (readback == null || readback.CampaignMap != source || readback.ObjectiveConfig != objective ||
                    readback.MapLayout == null || readback.MissionLogicalBinding == null)
                    throw new InvalidOperationException("Staged asset reload/reference readback failed");
                var persistedSizeResults = CompileSizes(readback, manifest, matrix);
                if (persistedSizeResults.Count != 3) throw new InvalidOperationException("Staged S049 assets failed size readback");
                var standardSetup = CompileOne(readback, manifest, matrix, SkirmishSizeId.Standard);
                var standardVariant = readback.MissionLogicalBinding.Variants.Single(v => v.Size == SkirmishSizeId.Standard && v.Difficulty == SkirmishDifficultyId.Regular);
                if (!SkirmishMissionLogicalBindingCompiler.ValidateFraming(standardVariant, standardSetup, out string framingError))
                    throw new InvalidOperationException("Staged Standard view readback failed: " + framingError);
                if (SkirmishOfflineDataExporter.HashFile(Path.Combine(root, expansionPath)) != expansionSha ||
                    SkirmishOfflineDataExporter.HashFile(Path.Combine(root, battlePath)) != battleSha)
                    throw new InvalidOperationException("Runtime expansion or battle catalog changed during staged asset authoring");
                var afterPins = SourcePins(pinRoots);
                if (sourcePins.Count != afterPins.Count || sourcePins.Any(p => !afterPins.TryGetValue(p.Key, out string after) || after != p.Value))
                    throw new InvalidOperationException("Pinned source closure changed during staged authoring");
                if (!ReferenceEquals(Unity.Entities.World.DefaultGameObjectInjectionWorld, originalWorld))
                    throw new InvalidOperationException("Default World changed during staged asset authoring");

                var receipt = new JObject {
                    ["kind"] = "S049StagedLogicalRuntimeAssets",
                    ["result"] = "Passed",
                    ["catalog_id"] = "S049",
                    ["asset_root"] = StageRoot,
                    ["sizes_compiled"] = persistedSizeResults,
                    ["runtime_catalog_added"] = false,
                    ["runtime_catalog_unchanged"] = true,
                    ["source_fit_certified"] = false,
                    ["active_water_qualified"] = false,
                    ["camera_qualified"] = false,
                    ["mission_ready"] = false,
                    ["player_readiness"] = false,
                    ["source_map_id"] = source.OperationMapId,
                    ["source_content_hash"] = source.ContentHash,
                    ["input_pins"] = new JObject { [CandidateObjective] = CandidateObjectiveSha,
                        [RetainedLayout] = LayoutSha, [RetainedBinding] = BindingSha, [ArmorBreakMap] = MapSha },
                    ["current_source_pins"] = JObject.FromObject(sourcePins),
                    ["catalog_hashes"] = new JObject { [expansionPath] = expansionSha, [battlePath] = battleSha },
                    ["asset_hashes"] = JObject.FromObject(planned.ToDictionary(
                        p => p.path, p => SkirmishOfflineDataExporter.HashFile(Path.Combine(root, p.path)), StringComparer.Ordinal))
                };
                using (var f = new FileStream(receiptPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var w = new StreamWriter(f, new System.Text.UTF8Encoding(false))) { w.Write(receipt.ToString(Formatting.Indented) + "\n"); w.Flush(); f.Flush(true); }
                completed = true;
            }
            catch
            {
                for (int i = createdPaths.Count - 1; i >= 0; i--)
                {
                    string path = createdPaths[i];
                    if (path.EndsWith(".meta", StringComparison.Ordinal)) continue;
                    string full = Path.Combine(root, path);
                    if (!ownedFileHashes.TryGetValue(path, out string expected) || !File.Exists(full) ||
                        SkirmishOfflineDataExporter.HashFile(full) != expected)
                    {
                        if (File.Exists(full)) Debug.LogError("[SkirmishS049StageAssets] Preserved changed/unowned file during rollback: " + full);
                        continue;
                    }
                    if (path.EndsWith(".asset", StringComparison.Ordinal) && AssetDatabase.LoadMainAssetAtPath(path) != null)
                    {
                        string meta = full + ".meta";
                        bool metaOwned = ownedFileHashes.TryGetValue(path + ".meta", out string metaExpected) && File.Exists(meta) &&
                            SkirmishOfflineDataExporter.HashFile(meta) == metaExpected;
                        if (metaOwned) AssetDatabase.DeleteAsset(path);
                        else { Debug.LogError("[SkirmishS049StageAssets] Preserved asset with changed/unowned meta during rollback: " + full); }
                    }
                    else
                    {
                        File.Delete(full);
                        string meta = full + ".meta";
                        if (ownedFileHashes.TryGetValue(path + ".meta", out string metaExpected) && File.Exists(meta) &&
                            SkirmishOfflineDataExporter.HashFile(meta) == metaExpected) File.Delete(meta);
                        else if (File.Exists(meta)) Debug.LogError("[SkirmishS049StageAssets] Preserved changed/unowned meta during rollback: " + meta);
                    }
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                foreach (string folder in createdFolders.OrderByDescending(p => p.Length))
                {
                    string meta = folder + ".meta";
                    if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any() &&
                        createdFolderMetaHashes.TryGetValue(folder, out string metaExpected) && File.Exists(meta) &&
                        SkirmishOfflineDataExporter.HashFile(meta) == metaExpected)
                    { Directory.Delete(folder); File.Delete(meta); }
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                foreach (var value in createdObjects) if (value != null && !AssetDatabase.Contains(value)) UnityEngine.Object.DestroyImmediate(value);
            }
            if (completed) Debug.Log("[SkirmishS049StageAssets] result=Passed sizes=3 runtimeCatalog=unchanged sourceFit=0 water=0 camera=0 missionReady=0");
        }

        static SkirmishResolvedSetup CompileOne(SkirmishScenarioDefinitionConfig definition, SkirmishContentManifest manifest,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix, SkirmishSizeId size)
        {
            if (!SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, Seed, manifest, matrix,
                out var setup, out var reasons)) throw new InvalidOperationException("S049 " + size + " compile failed: " + string.Join(";", reasons));
            return setup;
        }

        static JArray CompileSizes(SkirmishScenarioDefinitionConfig definition, SkirmishContentManifest manifest,
            IReadOnlyList<SkirmishSetupMatrixRow> matrix)
        {
            var result = new JArray();
            foreach (SkirmishSizeId size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            {
                var setup = CompileOne(definition, manifest, matrix, size);
                var variant = definition.MissionLogicalBinding.Variants.Single(v => v.Size == size && v.Difficulty == SkirmishDifficultyId.Regular);
                if (!SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant, setup, out string error))
                    throw new InvalidOperationException("S049 " + size + " view framing failed: " + error);
                result.Add(new JObject { ["size"] = size.ToString(), ["setup_hash"] = setup.SetupHash,
                    ["logical_binding_fingerprint"] = setup.MissionLogicalBindingFingerprint,
                    ["view_fingerprint"] = variant.ReviewedViewFingerprint });
            }
            return result;
        }

    }
}
#endif
