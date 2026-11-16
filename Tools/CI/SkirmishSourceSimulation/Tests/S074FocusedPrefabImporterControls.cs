using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Exercises the real pinned S074 template reader and importer without creating a world.
    internal static class S074FocusedPrefabImporterControls
    {
        internal static int Main(string[] args)
        {
            try
            {
                if (args == null || args.Length != 2) throw new ArgumentException("S074 template descriptor and source root required");
                JObject result = Run(args[0], args[1]);
                Console.WriteLine(result.ToString(Newtonsoft.Json.Formatting.None));
                Console.WriteLine("[S074FocusedPrefabImporterControls] result=Passed positive=1 negative=" + (int)result["negative_count"] + " worldAllocated=False missionReady=False");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("[S074FocusedPrefabImporterControls] result=Failed " + exception);
                return 1;
            }
        }

        private static JObject Run(string descriptorPath, string sourceRoot)
        {
            JObject descriptor = ReadObject(descriptorPath);
            JObject proof = ReadObject((string)descriptor["s074_capture_proof"]["path"]);
            JObject initialization = Initialization(proof);
            string binaryDirectory;
            JObject domain = GeneratedReferenceTemplateView.Read(descriptor, out binaryDirectory, sourceRoot, initialization);
            JObject unitMetadata = ReadObject((string)proof["unit_production_metadata"]["path"]);
            string[] keys = ((JArray)unitMetadata["units"]).Values<JObject>().Select(x => (string)x["key"]).ToArray();
            JObject preflight = GeneratedReferenceTemplateImporter.Preflight(domain, binaryDirectory, keys);
            Need((int?)preflight["roots"] == 16 && (int?)preflight["entities"] == 528 &&
                 (int?)preflight["decoded_components"] == 3250 && (int?)preflight["omitted_presentation_components"] == 20 &&
                 (int?)preflight["unsupported_unconsumed_values"] == 23 && (bool?)preflight["world_allocated"] == false &&
                 (int?)preflight["entity_identities_allocated"] == 0 && (bool?)domain["replay_ready"] == false &&
                 (bool?)domain["full_mission_initialized"] == false && (bool?)domain["native_visual_readiness"] == false,
                 "Actual S074 template importer preflight domain changed");

            NeedReject(descriptor, With(initialization, "mission_id", "S026"), sourceRoot, "wrong mission identity");
            JObject wrongSeed = (JObject)initialization.DeepClone(); wrongSeed["selection"]["seed"] = 104804;
            NeedReject(descriptor, wrongSeed, sourceRoot, "wrong selected seed");
            JObject wrongQualification = (JObject)initialization.DeepClone(); wrongQualification["configuration_qualification"]["sha256"] = new string('0', 64);
            NeedReject(descriptor, wrongQualification, sourceRoot, "changed configuration qualification pin");
            JObject wrongUnitOwner = (JObject)initialization.DeepClone(); wrongUnitOwner["unit_production_metadata"]["sha256"] = new string('0', 64);
            NeedReject(descriptor, wrongUnitOwner, sourceRoot, "changed selected unit owner pin");
            JObject wrongProof = (JObject)descriptor.DeepClone(); wrongProof["s074_capture_proof"]["sha256"] = new string('0', 64);
            NeedReject(wrongProof, initialization, sourceRoot, "changed proof pin");

            return new JObject
            {
                ["kind"] = "S074FocusedPrefabImporterControls",
                ["status"] = "Passed",
                ["positive_actual_reader_and_importer_preflight"] = true,
                ["roots"] = preflight["roots"], ["entities"] = preflight["entities"],
                ["decoded_components"] = preflight["decoded_components"],
                ["omitted_presentation_components"] = preflight["omitted_presentation_components"],
                ["unsupported_unconsumed_values"] = preflight["unsupported_unconsumed_values"],
                ["negative_count"] = 5,
                ["wrong_mission_rejected"] = true, ["wrong_seed_rejected"] = true,
                ["changed_qualification_pin_rejected"] = true, ["changed_unit_owner_pin_rejected"] = true,
                ["changed_proof_pin_rejected"] = true,
                ["world_allocated"] = false, ["replay_ready"] = false, ["mission_ready"] = false,
                ["native_visual_readiness"] = false
            };
        }

        private static JObject Initialization(JObject proof)
        {
            var initial = new JObject
            {
                ["mission_id"] = "S074",
                ["metadata_compiler_context"] = proof["source_context"].DeepClone(),
                ["configuration_qualification"] = proof["configuration_qualification"].DeepClone(),
                ["source_config_graph"] = proof["source_config_graph"].DeepClone(),
                ["building_definitions"] = proof["building_definitions"].DeepClone(),
                ["unit_production_metadata"] = proof["unit_production_metadata"].DeepClone(),
                ["selection"] = new JObject { ["seed"] = 104803, ["size_id"] = 1, ["difficulty_id"] = 2 }
            };
            // Only the receipt-driven source fixture uses this selector; the
            // Python admission layer verifies the actual author descriptors.
            if ((string)proof["kind"] == "S074BothVehicleTenMetreFocusedPrefabSourceOnlyCaptureProof")
                initial["s074_candidate_author"] = new JObject();
            return initial;
        }

        private static JObject With(JObject source, string property, JToken value)
        {
            JObject result = (JObject)source.DeepClone(); result[property] = value; return result;
        }

        private static void NeedReject(JObject descriptor, JObject initialization, string sourceRoot, string label)
        {
            try { string unused; GeneratedReferenceTemplateView.Read(descriptor, out unused, sourceRoot, initialization); }
            catch (InvalidDataException) { return; }
            catch (InvalidOperationException) { return; }
            catch (NotSupportedException) { return; }
            throw new InvalidOperationException("S074 tamper control accepted " + label);
        }

        private static JObject ReadObject(string path)
        {
            return JObject.Parse(File.ReadAllText(path), new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        private static void Need(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }
    }
}
