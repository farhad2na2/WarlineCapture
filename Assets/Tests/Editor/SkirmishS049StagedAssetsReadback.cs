#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    // Read-only independent check of staged S049 logical assets. It never writes
    // project assets or modifies either runtime catalog.
    public static class SkirmishS049StagedAssetsReadback
    {
        const string StageRoot = "Assets/Game/Configs/SkirmishExpansion/StagedS049";
        const string ObjectiveSource = "Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishObjective_S049Candidate_20261028.asset";
        const string ObjectiveMetaSha = "04191e2f1916451ff77e709ae9316e248e5e2f3432b56da5218f109367fd1021";
        const string LayoutSource = "Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r1/layout.json";
        const string BindingSource = "Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r6/logical-binding.json";
        const string MapAsset = "Assets/Game/GeneratedOperationMaps/CH04M05ArmorBreak/Definition.asset";
        const string ExpansionCatalog = "Assets/Game/Resources/SkirmishExpansionCatalog.asset";
        const string BattleCatalog = "Assets/Game/Resources/SkirmishBattleCatalog.asset";
        const string SourceContentHash = "da42d5bc876e1f2eed02b3d7050d41759c010de973dfd7c51c51eba1a26e5a86";
        const string SourceFingerprint = "c001f6c1849acf24b7b54bdcfbd3a5bc75679c98f071f12924fa5ad6e59a1020";
        const string SourceMapSha = "181418e5b316d8a5625913d0251e5f5a9a83ab81f56baec49e4adcb2bb89e3b4";
        const int Seed = 104734;

        public static void RunReadOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle Editor required for read-only S049 staged-asset verification");
            string[] args = Environment.GetCommandLineArgs();
            string input = Arg(args, "-skirmishS049StageVerifyInput");
            string output = Path.GetFullPath(Arg(args, "-skirmishS049StageVerifyOutput"));
            if (!string.Equals(Path.GetDirectoryName(output), "/private/tmp", StringComparison.Ordinal) || File.Exists(output))
                throw new InvalidOperationException("Fresh create-new verifier receipt directly under /private/tmp required");
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            JObject stagedReceipt = JObject.Parse(File.ReadAllText(input));
            Require((string)stagedReceipt["kind"] == "S049StagedLogicalRuntimeAssets" &&
                (string)stagedReceipt["result"] == "Passed" && (string)stagedReceipt["asset_root"] == StageRoot,
                "Exact passed staged-asset receipt required");
            Require((bool?)stagedReceipt["runtime_catalog_added"] == false &&
                (bool?)stagedReceipt["runtime_catalog_unchanged"] == true &&
                (bool?)stagedReceipt["source_fit_certified"] == false && (bool?)stagedReceipt["active_water_qualified"] == false &&
                (bool?)stagedReceipt["camera_qualified"] == false && (bool?)stagedReceipt["mission_ready"] == false &&
                (bool?)stagedReceipt["player_readiness"] == false, "Staged evidence must preserve all readiness gates as false");
            Require(!StageRoot.Contains("/Resources/", StringComparison.Ordinal), "Staged S049 assets must remain outside Resources");

            var originalWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var catalog = AssetDatabase.LoadAssetAtPath<SkirmishExpansionCatalogConfig>(ExpansionCatalog);
            var battle = AssetDatabase.LoadAssetAtPath<SkirmishBattleCatalogConfig>(BattleCatalog);
            Require(catalog != null && battle != null && !EditorUtility.IsDirty(catalog) && !EditorUtility.IsDirty(battle),
                "Clean persistent runtime catalogs required");
            string expansionSha = Hash(Path.Combine(root, ExpansionCatalog));
            string battleSha = Hash(Path.Combine(root, BattleCatalog));
            JObject catalogPins = (JObject)stagedReceipt["catalog_hashes"];
            Require(expansionSha == (string)catalogPins[ExpansionCatalog] && battleSha == (string)catalogPins[BattleCatalog],
                "Runtime catalog bytes differ from staging receipt");
            Require(!catalog.Definitions.Any(d => d != null && d.CatalogId == "S049"),
                "S049 must remain absent from runtime expansion catalog");
            Require(battle.TryGet("S049", out var planned) && planned.Status == SkirmishBattleCatalogStatus.Planned &&
                planned.PlayableScenarioIndex < 0 && !planned.IsPlayable, "S049 battle row must remain planned and unpublished");

            VerifyInputPins(root, stagedReceipt);
            var sourcePins = stagedReceipt["current_source_pins"] as JObject ?? throw new InvalidOperationException("Complete original source-pin receipt required");
            int checkedPins = VerifySourcePins(root, sourcePins);
            var assetPins = stagedReceipt["asset_hashes"] as JObject ?? throw new InvalidOperationException("Staged asset SHA receipt required");
            Require(assetPins.Count == 7, "Exact complete staged S049 logical asset inventory required");
            foreach (var pin in assetPins.Properties())
                Require(Hash(Path.Combine(root, pin.Name)) == (string)pin.Value, "Staged asset changed: " + pin.Name);

            var map = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(MapAsset);
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(StageRoot + "/SkirmishScenario_S049.asset");
            var objective = AssetDatabase.LoadAssetAtPath<SkirmishObjectiveConfig>(StageRoot + "/SkirmishObjective_S049.asset");
            var layout = AssetDatabase.LoadAssetAtPath<SkirmishMapLayoutConfig>(StageRoot + "/Logical/Layout_S049.asset");
            var binding = AssetDatabase.LoadAssetAtPath<SkirmishMissionLogicalBindingConfig>(StageRoot + "/Logical/Binding_S049.asset");
            Require(map != null && definition != null && objective != null && layout != null && binding != null,
                "All staged typed S049 references must load");
            Require(definition.CatalogId == "S049" && definition.DefinitionId == "skirmish.s049" &&
                definition.ContentVersion == 2 && definition.CampaignMap == map && definition.ObjectiveConfig == objective &&
                definition.MapLayout == layout && definition.MissionLogicalBinding == binding &&
                map.OperationMapId == "opmap.ch04.armor_break" && map.ContentHash == SourceContentHash &&
                SkirmishBindingCanonicalValues.SourceFingerprint(map) == SourceFingerprint &&
                Hash(Path.Combine(root, MapAsset)) == SourceMapSha,
                "Staged definition must bind the exact existing Armor Break Campaign source");
            Require(binding.Variants != null && binding.Variants.Length == 3 && layout.LayoutId == "layout.s049.measured-pad-candidate.v2",
                "Three retained typed Regular variants and measured logical layout required");
            Require(binding.SourceOperationMapId == map.OperationMapId && binding.SourceContentHash == map.ContentHash &&
                binding.SourceSceneGuid == map.SourceSceneReference.AssetGUID && binding.ReviewedLayoutSemanticFingerprint == "9e94e1a6be531a77829fa04e8672b660c3a31e7f479a51653f1c89c92a9649cb" &&
                SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout) == binding.ReviewedLayoutSemanticFingerprint,
                "Staged binding/layout must preserve the retained exact source and semantic fingerprints");
            string sourceMeta = Path.Combine(root, ObjectiveSource + ".meta");
            string stagedObjectivePath = StageRoot + "/SkirmishObjective_S049.asset";
            string sourceGuid = AssetDatabase.AssetPathToGUID(stagedObjectivePath);
            Match sourceGuidMatch = Regex.Match(File.ReadAllText(sourceMeta), "(?m)^guid:\\s*([0-9a-f]{32})\\s*$");
            Require(Hash(sourceMeta) == ObjectiveMetaSha && !string.IsNullOrEmpty(sourceGuid) &&
                sourceGuidMatch.Success && sourceGuid == sourceGuidMatch.Groups[1].Value &&
                AssetDatabase.GUIDToAssetPath(sourceGuid) == stagedObjectivePath,
                "Pinned candidate objective GUID must map uniquely to the staged objective asset");

            Require(SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix, out string matrixError), matrixError);
            var manifest = new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds };
            var expectedSizes = (JArray)stagedReceipt["sizes_compiled"];
            var verifiedSizes = new JArray();
            foreach (SkirmishSizeId size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
            {
                Require(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, size, Seed, manifest, matrix,
                    out var setup, out var reasons), "Original compiler rejected " + size + ": " + string.Join(";", reasons));
                Require(SkirmishMissionLogicalBindingCompiler.TryCompile(definition, setup, out var logical, out string logicalError),
                    "Original logical-binding compiler rejected " + size + ": " + logicalError);
                var variant = binding.Variants.Single(v => v.Size == size && v.Difficulty == SkirmishDifficultyId.Regular);
                string viewPath = StageRoot + "/Logical/View_S049_" + size + "_Regular.asset";
                if (!SkirmishMissionLogicalBindingCompiler.ValidateFraming(variant, setup, out string framingError))
                    throw new InvalidOperationException("Typed view framing failed for " + size + ": " + framingError);
                Require(variant.View != null && AssetDatabase.Contains(variant.View) && AssetDatabase.GetAssetPath(variant.View) == viewPath &&
                    logical.View == variant.View && logical.Fingerprint == setup.MissionLogicalBindingFingerprint &&
                    logical.ViewFingerprint == variant.ReviewedViewFingerprint,
                    "Typed view/reference binding failed for " + size);
                JObject expected = expectedSizes.OfType<JObject>().Single(r => (string)r["size"] == size.ToString());
                Require((uint)expected["setup_hash"] == setup.SetupHash &&
                    (string)expected["logical_binding_fingerprint"] == logical.Fingerprint &&
                    (string)expected["view_fingerprint"] == logical.ViewFingerprint,
                    "Independent compiler output differs from staged receipt for " + size);
                if (size == SkirmishSizeId.Standard)
                    Require(setup.SetupHash == 971644661 && logical.Fingerprint == "34813a76dd41a155f7d66a666eec8e36917b6a05eefcdf2c8af8a4add8ee5d6c",
                        "Pinned Standard S049 compiler identity changed");
                verifiedSizes.Add(new JObject { ["size"] = size.ToString(), ["setup_hash"] = setup.SetupHash,
                    ["logical_binding_fingerprint"] = logical.Fingerprint, ["view_fingerprint"] = logical.ViewFingerprint });
            }

            foreach (var pin in assetPins.Properties())
                Require(Hash(Path.Combine(root, pin.Name)) == (string)pin.Value, "Read-only compiler changed staged asset bytes: " + pin.Name);
            Require(!EditorUtility.IsDirty(map) && !EditorUtility.IsDirty(definition) && !EditorUtility.IsDirty(objective) &&
                !EditorUtility.IsDirty(layout) && !EditorUtility.IsDirty(binding) && binding.Variants.All(v => v.View != null && !EditorUtility.IsDirty(v.View)),
                "Read-only compiler must leave all staged assets clean");
            Require(Hash(Path.Combine(root, ExpansionCatalog)) == expansionSha && Hash(Path.Combine(root, BattleCatalog)) == battleSha &&
                !EditorUtility.IsDirty(catalog) && !EditorUtility.IsDirty(battle), "Readback must leave runtime catalogs byte-identical and clean");
            Require(ReferenceEquals(Unity.Entities.World.DefaultGameObjectInjectionWorld, originalWorld), "Verifier must not change the default World");
            var receipt = new JObject {
                ["kind"] = "S049StagedLogicalAssetReadback",
                ["result"] = "Passed",
                ["staging_receipt"] = input,
                ["asset_root"] = StageRoot,
                ["verified_source_pins"] = checkedPins,
                ["verified_sizes"] = verifiedSizes,
                ["source_map_id"] = map.OperationMapId,
                ["source_content_hash"] = map.ContentHash,
                ["runtime_catalog_added"] = false,
                ["runtime_catalog_unchanged"] = true,
                ["source_fit_certified"] = false,
                ["active_water_qualified"] = false,
                ["camera_qualified"] = false,
                ["mission_ready"] = false,
                ["player_readiness"] = false,
                ["catalog_hashes"] = new JObject { [ExpansionCatalog] = expansionSha, [BattleCatalog] = battleSha },
                ["staged_asset_hashes"] = assetPins.DeepClone()
            };
            using (var f = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var w = new StreamWriter(f, new System.Text.UTF8Encoding(false))) { w.Write(receipt.ToString(Formatting.Indented) + "\n"); w.Flush(); f.Flush(true); }
            Debug.Log("[SkirmishS049StageAssetsReadback] result=Passed sourcePins=" + checkedPins + " sizes=3 catalogs=unchanged readiness=0");
        }

        static void VerifyInputPins(string root, JObject receipt)
        {
            var pins = receipt["input_pins"] as JObject ?? throw new InvalidOperationException("Pinned S049 authoring inputs missing");
            Require(pins.Count == 4, "Exact retained S049 input pin set required");
            foreach (var pin in pins.Properties()) Require(Hash(Path.Combine(root, pin.Name)) == (string)pin.Value, "Input changed: " + pin.Name);
            Require((string)pins[ObjectiveSource] == "ee70c46ca4e2c07edf766124d717361ae6b761f76dfb7312908fa63d460c10fb" &&
                (string)pins[LayoutSource] == "fad258a220248c4a60da9cd7b8f097c2eff08b20871bef8fbd3be919d260d7c7" &&
                (string)pins[BindingSource] == "3bd7f60ae0ccf9e3ecba42247a12c00aa3b52a9d06b0f2494ebcbff01c3d95e5" &&
                (string)pins[MapAsset] == SourceMapSha, "S049 input identities differ from retained source pins");
        }

        static int VerifySourcePins(string root, JObject pins)
        {
            if (pins.Count < 1000) throw new InvalidOperationException("Complete captured source closure required");
            foreach (var pin in pins.Properties())
            {
                string path = Path.IsPathRooted(pin.Name) ? pin.Name : Path.Combine(root, pin.Name);
                if (!File.Exists(path) || Hash(path) != (string)pin.Value)
                    throw new InvalidOperationException("Captured source pin changed: " + path);
            }
            return pins.Count;
        }

        static string Arg(string[] args, string key)
        {
            int at = Array.IndexOf(args, key);
            if (at < 0 || at + 1 >= args.Length) throw new ArgumentException("Explicit " + key + " required");
            return args[at + 1];
        }

        static string Hash(string path)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
