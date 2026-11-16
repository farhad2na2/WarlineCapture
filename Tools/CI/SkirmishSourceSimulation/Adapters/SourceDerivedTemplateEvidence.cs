using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Game.Configs;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // This qualifies a source-only target's use of historical template facts. It never
    // changes the captured mission identity or claims the historical capture is current.
    internal static class SourceDerivedTemplateEvidence
    {
        private const string OriginalDescriptorSha = "f81035bef542d036f81fae58c3ff5a32a62d1c4b8f085f89fb817003c18c993a";
        private const string HistoricalAuditSha = "02a86b7c1bfe443cdfbd7caa101532c0a360574b2d2558d50453328c69208999";
        private const string ImportControlSha = "503e132294ce087b216765010d861942c4230329c5560703e80b538e8741573c";

        private static JObject Read(JToken value, long maximum)
        {
            if (!(value is JObject d) || d.Count != 2 || d["path"]?.Type != JTokenType.String ||
                d["sha256"]?.Type != JTokenType.String || !Regex.IsMatch((string)d["sha256"], "^[0-9a-f]{64}$"))
                throw new InvalidDataException("Exact derived template evidence descriptor required");
            return JObject.Parse(Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)d["path"], (string)d["sha256"], maximum)),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        internal static void Validate(JObject descriptor, JObject request, SkirmishResolvedSetup setup, JObject context, JObject domain, string sourceRoot)
        {
            var proof = Read(descriptor["s003_derived_target_proof"], 1024 * 1024);
            string[] names = { "schema_version", "kind", "original_descriptor", "historical_pin_audit", "disposable_import_control", "compiler_context", "target_source_graph", "exporter_control", "export_authoring_request", "export_authoring_result", "native_capture", "source_fit_certified", "mission_ready" };
            if (proof.Count != names.Length || proof.Properties().Any(p => !names.Contains(p.Name)) || (int?)proof["schema_version"] != 1 ||
                (string)proof["kind"] != "S003DerivedTargetTemplateEvidence" || (bool?)proof["native_capture"] != false ||
                (bool?)proof["source_fit_certified"] != false || (bool?)proof["mission_ready"] != false ||
                (string)proof["original_descriptor"]?["sha256"] != OriginalDescriptorSha ||
                (string)proof["historical_pin_audit"]?["sha256"] != HistoricalAuditSha ||
                (string)proof["disposable_import_control"]?["sha256"] != ImportControlSha ||
                (string)proof["exporter_control"]?["sha256"] != "6e6d592066bbdb6f104752ffd418975e985fdcd723499cd19aa16341ce7426f9" ||
                (string)proof["export_authoring_request"]?["sha256"] != "db8d505a7eb27955de3a215dfad49c1ef40378afb8ff73a0902b90d197ae98f8" ||
                (string)proof["export_authoring_result"]?["sha256"] != "fe799f53375d776cb505091b9e16714718edad2bc5258afb007760120a859a25" ||
                !JToken.DeepEquals(proof["compiler_context"], request["metadata_compiler_context"]) ||
                (string)proof["target_source_graph"]?["sha256"] != "edc4081763a3252716679fc4e36dae2b8341aa6e9c8426fa552e143096d510cb" ||
                !JToken.DeepEquals(proof["target_source_graph"], request["source_config_graph"]))
                throw new InvalidDataException("Exact source-only S003 derived template evidence required");
            var targetGraph = Read(proof["target_source_graph"], 16 * 1024 * 1024);
            var expectedContext = Read(proof["compiler_context"], 4 * 1024 * 1024);
            if (!JToken.DeepEquals(expectedContext, context) || (string)context["kind"] != "OriginalS003DerivedNativeFactoryContext" ||
                setup == null || (string)context["target_context"]?["catalog_id"] != setup.CatalogId ||
                (string)context["target_context"]?["definition_id"] != setup.DefinitionId ||
                (int?)context["target_context"]?["content_version"] != setup.ContentVersion ||
                (string)context["target_context"]?["size"] != setup.SizeId.ToString() ||
                (string)context["target_context"]?["difficulty"] != setup.DifficultyId.ToString() ||
                (string)context["target_context"]?["operation_map_id"] != setup.OperationMapId ||
                (int?)context["target_context"]?["seed"] != setup.Seed || (uint?)context["target_context"]?["setup_hash"] != setup.SetupHash)
                throw new InvalidDataException("Derived template target differs from actual compiler setup");
            var authorRequest = Read(proof["export_authoring_request"], 4 * 1024 * 1024);
            var authorResult = Read(proof["export_authoring_result"], 8 * 1024 * 1024);
            var exporter = Read(proof["exporter_control"], 4 * 1024 * 1024);
            string resultDirectory = Path.GetDirectoryName((string)proof["export_authoring_result"]["path"]);
            if (!JToken.DeepEquals(authorRequest["graph"], proof["target_source_graph"]) ||
                (string)authorRequest["output_directory"] != resultDirectory ||
                (string)authorRequest["catalog_id"] != setup.CatalogId || (int?)authorRequest["seed"] != setup.Seed ||
                (string)authorResult["status"] != "ConfigurationCompiled" || (bool?)authorResult["compiler_accepted"] != true ||
                (string)authorResult["catalog_id"] != setup.CatalogId || (int?)authorResult["seed"] != setup.Seed ||
                (string)authorResult["definition_id"] != setup.DefinitionId || (string)authorResult["size"] != setup.SizeId.ToString() ||
                (string)authorResult["difficulty"] != setup.DifficultyId.ToString() || (string)authorResult["operation_map_id"] != setup.OperationMapId ||
                !JToken.DeepEquals(authorResult["source_content_hash"], context["target_context"]["source_content_hash"]) ||
                !JToken.DeepEquals(authorResult["source_fingerprint"], context["target_context"]["source_fingerprint"]) ||
                (uint?)authorResult["resolved_setup"]?["setup_hash"] != setup.SetupHash ||
                !JToken.DeepEquals(authorResult["layout_fingerprint"], context["target_context"]["layout_fingerprint"]) ||
                (bool?)authorResult["world_allocated"] != false || (bool?)authorResult["source_fit_verified"] != false ||
                (bool?)authorResult["mission_ready"] != false || (string)exporter["status"] != "PassedSourceOnlyCompilerContext" ||
                !JToken.DeepEquals(exporter["source_context"], proof["compiler_context"]) ||
                !JToken.DeepEquals(exporter["target"], context["target_context"]))
                throw new InvalidDataException("Pinned compiler replay did not consume this target source graph");
            var authorPins = authorResult["input_pins"] as JObject;
            var graphPins = targetGraph["source_pins"] as JObject;
            string graphPath = (string)proof["target_source_graph"]["path"];
            if (authorPins == null || graphPins == null ||
                !JToken.DeepEquals(authorPins[graphPath]?["sha256"], proof["target_source_graph"]["sha256"]) ||
                graphPins.Properties().Any(p => !JToken.DeepEquals(authorPins[p.Name]?["sha256"], p.Value)))
                throw new InvalidDataException("Compiler replay input pins do not cover the target source graph");
            foreach (var graphPin in graphPins.Properties())
            {
                if (graphPin.Value.Type != JTokenType.String || !Regex.IsMatch((string)graphPin.Value, "^[0-9a-f]{64}$"))
                    throw new InvalidDataException("Canonical target source graph hash required");
                string graphInput = SourceConfigurationQualification.ResolveCurrentS003CapturePin(graphPin.Name, sourceRoot);
                AutonomousWorld.PinnedFile(graphInput, (string)graphPin.Value, 4 * 1024 * 1024);
            }
            var exportLog = exporter["run_log"] as JObject;
            if (exportLog == null || exportLog.Count != 2) throw new InvalidDataException("Pinned compiler replay log required");
            AutonomousWorld.PinnedFile((string)exportLog["path"], (string)exportLog["sha256"], 4 * 1024 * 1024);
            var original = Read(proof["original_descriptor"], 4 * 1024 * 1024);
            var withoutProof = (JObject)descriptor.DeepClone(); withoutProof.Remove("s003_derived_target_proof");
            if (!JToken.DeepEquals(original, withoutProof)) throw new InvalidDataException("Historical template descriptor changed");
            var importer = Read(proof["disposable_import_control"], 4 * 1024 * 1024);
            if ((string)importer["kind"] != "S075S003DisposableTemplateImportDiagnostic" || (string)importer["status"] != "PassedDisposableLogicalManagerImport" ||
                (string)importer["target"]?["catalog_id"] != setup.CatalogId || (int?)importer["target"]?["seed"] != setup.Seed ||
                (uint?)importer["target"]?["setup_hash"] != setup.SetupHash || (bool?)importer["shim_world_object_created"] != true ||
                (bool?)importer["unity_runtime_world_created"] != false || (bool?)importer["mission_world_created"] != false ||
                (bool?)importer["foreign_root_rejected_before_allocation"] != true || (bool?)importer["disposable_manager_cleanup"] != true ||
                (bool?)importer["historical_capture_pin_closure_admitted"] != false || (bool?)importer["source_fit_certified"] != false ||
                (bool?)importer["mission_ready"] != false || (string)importer["prefab_capture"]?["sha256"] != (string)descriptor["source_reference_sha256"] ||
                !JToken.DeepEquals(importer["movement_source"], request["unit_move_authoring_source"]))
                throw new InvalidDataException("Exact current disposable importer control required");
            var pinnedDomain = SourceHistoricalTemplatePinAudit.ValidatePinnedCapture((JObject)proof["original_descriptor"], (JObject)proof["historical_pin_audit"], sourceRoot);
            if (!JToken.DeepEquals(pinnedDomain, domain)) throw new InvalidDataException("Historical template domain differs");
            var observed = Read(importer["observations"], 1024 * 1024);
            if ((string)observed["target"] != setup.CatalogId || (int?)observed["required_roots"] != 19 || (int?)observed["imported_roots"] != 19 ||
                (int?)observed["imported_entities"] != 647 || (int?)observed["imported_components"] != 3965 ||
                (int?)observed["omitted_presentation_components"] != 26 || (int?)observed["identities_allocated"] != 647 ||
                (bool?)observed["foreign_root_rejected_before_allocation"] != true || (bool?)observed["disposable_manager_cleanup"] != true ||
                (bool?)observed["unity_runtime_world_created"] != false || (bool?)observed["mission_world_created"] != false ||
                (bool?)observed["historical_capture_pin_closure_admitted"] != false || (bool?)observed["mission_ready"] != false)
                throw new InvalidDataException("Disposable importer observations differ");
            foreach (string name in new[] { "run_log", "compile_log", "compile_response", "executable", "harness", "movement_source", "prefab_capture" })
            {
                var d = importer[name] as JObject;
                if (d == null || d.Count != 2) throw new InvalidDataException("Disposable importer input missing: " + name);
                AutonomousWorld.PinnedFile((string)d["path"], (string)d["sha256"], 128 * 1024 * 1024);
            }
            var pins = Read(importer["post_build_source_pins"], 16 * 1024 * 1024);
            if (pins.Count != (int?)importer["post_build_source_pin_count"] || pins.Count != 739)
                throw new InvalidDataException("Exact current importer source closure required");
            string importerPath = Path.Combine(Path.GetFullPath(sourceRoot), "Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.cs");
            if (pins[importerPath]?.Type != JTokenType.String) throw new InvalidDataException("Current importer source must be in closure");
            foreach (var p in pins.Properties()) AutonomousWorld.PinnedFile(p.Name, (string)p.Value, 128 * 1024 * 1024);

            var native = Read(context["native_factory_reference"], 64 * 1024 * 1024);
            var captured = Read(descriptor["independent_capture_proof"]?["metadata_capture"], 64 * 1024 * 1024);
            if (!JToken.DeepEquals(native["context"]?["metadata"], captured["context"]?["metadata"]) ||
                !JToken.DeepEquals(native["context"]?["metadata_key_sha256"], captured["context"]?["metadata_key_sha256"]))
                throw new InvalidDataException("Historical template and compiler factory metadata differ");
            var unitKeys = ((JArray)context["derived_metadata"]["units"]).Select(x => (string)x["prefab"]["key"]).ToArray();
            var roots = ((JObject)domain["opening_prefabs"]).Properties().Select(x => x.Name).ToArray();
            if (unitKeys.Length != 19 || unitKeys.Distinct(StringComparer.Ordinal).Count() != 19 || roots.Length != 19 ||
                roots.Distinct(StringComparer.Ordinal).Count() != 19 || unitKeys.Except(roots, StringComparer.Ordinal).Any())
                throw new InvalidDataException("Actual compiled factory unit and historical template domains differ");
        }
    }
}
