#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    // Captures the current packaged S003 compiler target and original metadata factories only.
    // This deliberately does not initialize a mission or transfer runtime state.
    public static partial class SkirmishBakedActorSnapshotExporter
    {
        const string S003CurrentGraphSha256 = "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb";
        const string S003CurrentGraphRelativePath = "Logs/SkirmishProduction/s003-current-config-graph-20261103-r1/graph-r5/source-config-graph.json";

        public static void RunS003MetadataCapture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Idle Editor required");

            string[] args = Environment.GetCommandLineArgs();
            int outputAt = Array.IndexOf(args, "-skirmishS003Metadata");
            int graphAt = Array.IndexOf(args, "-skirmishS003Graph");
            if (outputAt < 0 || outputAt + 1 >= args.Length || graphAt < 0 || graphAt + 1 >= args.Length)
                throw new ArgumentException("Explicit fresh output directory and current graph-r5 path required");

            string output = SkirmishOfflineDataExporter.ValidateOutputDirectory(args[outputAt + 1]);
            string reportPath = Path.Combine(output, "s003-native-metadata.json");
            if (File.Exists(reportPath)) throw new InvalidOperationException("Output already exists");

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string expectedGraphPath = Path.GetFullPath(Path.Combine(projectRoot, S003CurrentGraphRelativePath));
            string graphPath = Path.GetFullPath(args[graphAt + 1]);
            if (!string.Equals(graphPath, expectedGraphPath, StringComparison.Ordinal) ||
                SkirmishOfflineDataExporter.HashFile(graphPath) != S003CurrentGraphSha256)
                throw new InvalidOperationException("Exact current S003 graph-r5 identity required");

            JObject graph;
            using (var reader = new JsonTextReader(new StringReader(File.ReadAllText(graphPath))) { DateParseHandling = DateParseHandling.None })
                graph = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            string definitionPath = Path.GetFullPath(Path.Combine(projectRoot,
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset"));
            if ((string)graph["authority"] != "AuthoredConfigAssets" ||
                (graph["source_pins"] as JObject)?[definitionPath] == null)
                throw new InvalidOperationException("Current S003 authored config graph required");

            // Recheck every authored graph pin before trusting its S003 package closure.
            var pins = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (JProperty pin in ((JObject)graph["source_pins"]).Properties())
            {
                string path = Path.GetFullPath(Path.Combine(projectRoot, pin.Name));
                string sha = (string)pin.Value;
                if (string.IsNullOrEmpty(sha) || SkirmishOfflineDataExporter.HashFile(path) != sha)
                    throw new InvalidOperationException("Current graph source pin drift: " + pin.Name);
                pins.Add(path, sha);
            }

            var expansion = SkirmishExpansionCatalogConfig.Load();
            var definition = AssetDatabase.LoadAssetAtPath<SkirmishScenarioDefinitionConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/SkirmishScenario_S003.asset");
            var setupAsset = AssetDatabase.LoadAssetAtPath<ScenarioSetupConfig>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003/ScenarioSetup_S003.asset");
            if (expansion == null || definition == null || setupAsset == null || definition.CatalogId != "S003" ||
                definition.DefinitionId != "skirmish.s003" || setupAsset.ScenarioId != "scenario.skirmish.s003" ||
                setupAsset.OperationMapId != "opmap.skirmish.desert_base_01" || setupAsset.DeterministicSeed != 104732 ||
                !expansion.Definitions.Any(d => ReferenceEquals(d, definition)))
                throw new InvalidOperationException("Exact packaged S003 definition/catalog/setup target required");

            // These are the existing Desert Base physical/provider inputs used by the native factories.
            var baseline = SkirmishPresetConfig.Load(SkirmishPresetConfig.DesertBaseScenarioIndex);
            var scene = AssetDatabase.LoadAssetAtPath<BuildingPlacementSystemConfig>(
                AssetDatabase.GUIDToAssetPath("b2010000000000000000000000000004"));
            if (baseline == null || baseline.buildingPlacement == null || scene == null || scene.UnitPrefabRegistryConfig == null)
                throw new InvalidOperationException("Original Desert Base provider inputs are incomplete");
            var providerPins = SourcePins(AssetDatabase.GetAssetPath(baseline),
                AssetDatabase.GetAssetPath(baseline.buildingPlacement), AssetDatabase.GetAssetPath(scene),
                AssetDatabase.GetAssetPath(scene.UnitPrefabRegistryConfig), AssetDatabase.GetAssetPath(expansion),
                AssetDatabase.GetAssetPath(definition), AssetDatabase.GetAssetPath(setupAsset));
            foreach (var providerPin in providerPins)
            {
                if (pins.ContainsKey(providerPin.Key) && pins[providerPin.Key] != providerPin.Value)
                    throw new InvalidOperationException("S003 graph/native provider pin disagreement: " + providerPin.Key);
                pins[providerPin.Key] = providerPin.Value;
            }
            pins[graphPath] = S003CurrentGraphSha256;

            World originalDefault = World.DefaultGameObjectInjectionWorld;
            JObject context = CaptureCompiledMetadataContext(104732, baseline.buildingPlacement, scene,
                definition, "S003", SkirmishDifficultyId.Regular, SkirmishSizeId.Standard);
            if (!ReferenceEquals(originalDefault, World.DefaultGameObjectInjectionWorld))
                throw new InvalidOperationException("Default World changed during data-only metadata capture");
            if ((string)context["catalog_id"] != "S003" || (int?)context["seed"] != 104732 ||
                (string)context["difficulty"] != "Regular" || (string)context["size"] != "Standard")
                throw new InvalidOperationException("Original compiler returned a different S003 target");

            foreach (var pin in pins)
                if (SkirmishOfflineDataExporter.HashFile(pin.Key) != pin.Value)
                    throw new InvalidOperationException("Source changed during native metadata capture: " + pin.Key);

            string metadataKey = (string)context["metadata_key_sha256"];
            if (string.IsNullOrEmpty(metadataKey) || MetadataDigest(context["metadata"]) != metadataKey)
                throw new InvalidOperationException("Original native metadata digest mismatch");
            var report = new JObject
            {
                ["schema_version"] = 1,
                ["kind"] = "NativeS003OriginalCompilerFactoryMetadataCapture",
                ["scope"] = "CurrentPackagedS003CompilerAndOriginalMetadataFactories_DataOnly_NoMissionWorld",
                ["context"] = context,
                ["source_graph"] = new JObject { ["path"] = graphPath, ["sha256"] = S003CurrentGraphSha256 },
                ["source_pins"] = JObject.FromObject(pins),
                ["full_mission_initialized"] = false,
                ["utility_world_created"] = true,
                ["utility_world_disposed"] = true,
                ["mission_world_created"] = false,
                ["runtime_state_transferred"] = false,
                ["source_fit_certified"] = false,
                ["native_context_qualified"] = false,
                ["mission_ready"] = false
            };
            byte[] bytes = Encoding.UTF8.GetBytes(report.ToString(Formatting.Indented) + "\n");
            if (bytes.Length > 64 * 1024 * 1024) throw new InvalidOperationException("Bounded metadata output exceeded");
            Directory.CreateDirectory(output);
            using (var stream = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.Write(bytes, 0, bytes.Length);
            Debug.Log("[SkirmishS003MetadataCapture] result=Passed catalog=S003 seed=104732 scope=DataOnly native_context_qualified=false mission_ready=false");
        }
    }
}
#endif
