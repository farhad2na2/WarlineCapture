using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    internal static class S050FocusedPrefabImporterControls
    {
        private static int negatives;
        private static void Need(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Rejects(string name, Action action)
        {
            try { action(); }
            catch (Exception) { negatives++; return; }
            throw new InvalidOperationException("Negative control accepted: " + name);
        }

        private static JObject ReadDescriptor(JObject descriptor)
        {
            var bytes = AutonomousWorld.PinnedFile((string)descriptor["path"], (string)descriptor["sha256"], 16 * 1024 * 1024);
            return JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        private static void RejectOwner(JObject proof, JObject context, JObject units, JObject author, string root)
        {
            var method = typeof(GeneratedReferenceTemplateView).GetMethod("ValidateActualUnitOwnerProof",
                BindingFlags.NonPublic | BindingFlags.Static);
            Need(method != null, "Direct-owner reader validator missing");
            try { method.Invoke(null, new object[] { proof, context, units, author, root }); }
            catch (TargetInvocationException e) when (e.InnerException != null) { negatives++; return; }
            throw new InvalidOperationException("Direct-owner negative control was accepted");
        }

        internal static int Main(string[] args)
        {
            try
            {
                Need(args.Length == 2, "Expected exact projection descriptor and repository root");
                string descriptorPath = Path.GetFullPath(args[0]);
                string root = Path.GetFullPath(args[1]);
                var descriptor = JObject.Parse(File.ReadAllText(descriptorPath),
                    new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                var proofDescriptor = descriptor["source_only_capture_proof"] as JObject;
                Need(proofDescriptor != null, "Source-only failed-capture proof descriptor required");
                var proof = ReadDescriptor(proofDescriptor);
                var initialization = new JObject {
                    ["mission_id"] = "S050",
                    ["metadata_compiler_context"] = proof["source_context"].DeepClone(),
                    ["configuration_qualification"] = proof["configuration_qualification"].DeepClone(),
                    ["source_config_graph"] = proof["source_config_graph"].DeepClone(),
                    ["building_definitions"] = proof["building_definitions"].DeepClone(),
                    ["unit_production_metadata"] = proof["unit_production_metadata"].DeepClone()
                };
                string binaryDirectory;
                JObject domain = GeneratedReferenceTemplateView.Read(descriptor, out binaryDirectory, root, initialization);
                var owner = proof["unit_owner_evidence"] as JObject;
                Need(owner != null && (string)owner["mode"] == "OriginalConfiguredAuthoringFactories_CompilerSelected",
                    "Strict direct actual-owner proof mode required");
                var keys = owner["keys"].Values<string>().ToArray();
                JObject positive = GeneratedReferenceTemplateImporter.Preflight(domain, binaryDirectory, keys);
                Need((int?)positive["roots"] == 16 && (int?)positive["entities"] == ((JArray)domain["entities"]).Count &&
                    (int?)positive["omitted_presentation_components"] ==
                        ((JObject)proof["presentation_components_omitted_by_existing_importer"]).Properties().Sum(p => (int)p.Value) &&
                    (int?)positive["unsupported_unconsumed_values"] == 23 &&
                    (bool?)positive["world_allocated"] == false && (int?)positive["entity_identities_allocated"] == 0,
                    "Compiled importer source-only decode counters or nonclaims changed");

                Rejects("missing-required-root", () => GeneratedReferenceTemplateImporter.Preflight(
                    domain, binaryDirectory, keys.Concat(new[] { "not-a-captured-prefab" })));
                JObject badUnsupported = (JObject)domain.DeepClone();
                badUnsupported["unsupported_values"][0]["reason"] = "wrong-reason";
                Rejects("tampered-unsupported-reason", () => GeneratedReferenceTemplateImporter.Preflight(
                    badUnsupported, binaryDirectory, keys));
                JObject badComponent = (JObject)domain.DeepClone();
                badComponent["unsupported_values"][0]["path"] = ((string)badComponent["unsupported_values"][0]["path"])
                    .Replace("Game.Components.UnitAttackImpactVfxReference", "Game.Components.UnitGridAuthoring");
                Rejects("non-presentation-unsupported-component", () => GeneratedReferenceTemplateImporter.Preflight(
                    badComponent, binaryDirectory, keys));
                JObject badKeySet = (JObject)domain.DeepClone();
                badKeySet["requested_prefab_keys"][0] = "not-a-captured-prefab";
                Rejects("tampered-requested-root", () => GeneratedReferenceTemplateImporter.Preflight(
                    badKeySet, binaryDirectory, keys));

                JObject context = ReadDescriptor(proof["source_context"] as JObject);
                JObject units = ReadDescriptor(proof["unit_production_metadata"] as JObject);
                JObject author = ReadDescriptor(proof["authoring_result"] as JObject);
                JObject badOwnerProof = (JObject)proof.DeepClone();
                badOwnerProof["unit_owner_evidence"]["logical_layout_binding_fingerprint"] = new string('0', 64);
                RejectOwner(badOwnerProof, context, units, author, root);
                JObject badRequestable = (JObject)units.DeepClone();
                badRequestable["units"][0]["authoring_catalog_properties"]["fields"]["CanRequest"]["value"] = false;
                RejectOwner(proof, context, badRequestable, author, root);
                JObject badSelection = (JObject)units.DeepClone();
                badSelection["compiler_selection"]["result"]["sha256"] = new string('0', 64);
                RejectOwner(proof, context, badSelection, author, root);
                Need(negatives == 7, "Exact seven negative controls required");
                Console.WriteLine("[S050FocusedPrefabImporterControls] result=Passed positive=1 negative=7 worldAllocated=False missionReady=False");
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[S050FocusedPrefabImporterControls] result=Failed type=" + e.GetType().Name + " message=" + e.Message);
                return 1;
            }
        }
    }
}
