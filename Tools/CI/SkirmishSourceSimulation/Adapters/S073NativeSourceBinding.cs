using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    /// <summary>Read-only diagnostic join for the qualified S073 native binding observation.</summary>
    internal static class S073NativeSourceBinding
    {
        private const string ReceiptSha256 = "594bc068ae821c2858307124c11ee058a1b4e02a500988928bad044aabe31904";
        private const string ObservationSha256 = "b1b2bdd6c663cdb7d7f14509ebc6b47e8fb83c6d47141360267acf140ee3568f";
        private const string LogSha256 = "440b8efbcdc6cff1ea3f57c1033ad341c7cefbaff166ab5df77d20228f45d542";
        private const string Marker = "[S073NativeEntitySceneBinding] result=Recorded scope=actual_binding_outcome";

        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        /// <param name="repositoryRoot">Repository root containing Logs and the pinned source files.</param>
        /// <param name="actualSourceWorld">Current source-world read model with source_metadata and grid_frame objects.</param>
        /// <returns>A diagnostic JObject only; it does not mutate or transfer world/session state.</returns>
        internal static JObject Join(string repositoryRoot, JObject actualSourceWorld)
        {
            if (string.IsNullOrWhiteSpace(repositoryRoot) || actualSourceWorld == null)
                throw new ArgumentException("Repository root and current source-world read model are required");
            string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string dir = Path.Combine(root, "Logs", "SkirmishProduction", "s073-native-entity-scene-binding-20261101-r2");
            byte[] receiptBytes = File.ReadAllBytes(Path.Combine(dir, "receipt-strict-r3.json"));
            byte[] observationBytes = File.ReadAllBytes(Path.Combine(dir, "observation.json"));
            byte[] logBytes = File.ReadAllBytes(Path.Combine(dir, "editor.log"));
            if (Hash(receiptBytes) != ReceiptSha256 || Hash(observationBytes) != ObservationSha256 || Hash(logBytes) != LogSha256)
                throw new InvalidDataException("Qualified S073 native binding evidence changed");
            var receipt = JObject.Parse(Encoding.UTF8.GetString(receiptBytes));
            if ((string)receipt["kind"] != "S073NativeEntitySceneBindingFinalization" ||
                (int?)receipt["schema_version"] != 1 || (string)receipt["binding_status"] != "Available" ||
                (int?)receipt["unity_exit_code"] != 0 || (string)receipt["recorded_observation"] != "Recorded" ||
                (string)receipt["observation_sha256"] != ObservationSha256 || (string)receipt["unity_log_sha256"] != LogSha256 ||
                string.IsNullOrEmpty((string)receipt["observation_path"]) ||
                (bool?)receipt["mission_ready"] != false || (bool?)receipt["player_readiness"] != false ||
                (bool?)receipt["normal_input_playthrough"] != false || (bool?)receipt["source_fit_certified"] != false ||
                !Encoding.UTF8.GetString(logBytes).Contains(Marker + " binding_read_succeeded=True path=" + (string)receipt["observation_path"]))
                throw new InvalidDataException("Qualified non-readiness S073 binding receipt required");

            string observationPath = Path.Combine(dir, "observation.json");
            var observation = JObject.Parse(Encoding.UTF8.GetString(observationBytes));
            if ((string)observation["kind"] != "S073NativeEntitySceneBindingObservation" ||
                (int?)observation["schema_version"] != 1 || (bool?)observation["binding_read_succeeded"] != true ||
                (string)observation["session_id"] != (string)receipt["session_id"] ||
                (int?)observation["scenario_index"] != 3 || (int?)observation["seed"] != 104729 ||
                (string)observation["phase"] != "Playing" || (string)observation["scope"] != "ActualMatchSceneViewTryReadEntitySceneGridBindingOutcome_NotReadinessOrMissionAcceptance")
                throw new InvalidDataException("Exact recorded S073 native binding observation required");

            var pins = receipt["source_pins_sha256"] as JObject;
            var observationPins = observation["source_pins_sha256"] as JObject;
            if (pins == null || observationPins == null || pins.Count != 5 || observationPins.Count != pins.Count)
                throw new InvalidDataException("Complete current S073 source pins required");
            foreach (var pin in pins.Properties())
            {
                if ((string)observationPins[pin.Name] != (string)pin.Value) throw new InvalidDataException("Receipt/observation source pins differ");
                string path = Path.GetFullPath(Path.Combine(root, pin.Name));
                if (Path.IsPathRooted(pin.Name) || !path.StartsWith(root, StringComparison.Ordinal) || Hash(File.ReadAllBytes(path)) != (string)pin.Value)
                    throw new InvalidDataException("Current S073 binding source pin changed: " + pin.Name);
            }

            JObject metadata = actualSourceWorld["source_metadata"] as JObject ?? throw new InvalidDataException("Current source_metadata read model required");
            JObject frame = actualSourceWorld["grid_frame"] as JObject ?? throw new InvalidDataException("Current grid_frame read model required");
            JObject map = (JObject)observation["map"], grid = (JObject)observation["grid"];
            JObject binding = (JObject)observation["binding"];
            if ((string)binding["source_identity"] != (string)map["source_identity_hash"] ||
                (string)binding["source_content"] != (string)map["source_content_hash"] ||
                (string)binding["generated_metadata"] != (string)map["generated_metadata_hash"])
                throw new InvalidDataException("Observed scene binding metadata differs from the observed source map metadata");
            var origin = grid["origin"] as JArray;
            var currentOrigin = frame["origin"] as JArray;
            bool metadataMatches = (string)metadata["operation_map_id"] == (string)map["operation_map_id"] &&
                (string)metadata["source_identity_hash"] == (string)binding["source_identity"] &&
                (string)metadata["source_content_hash"] == (string)binding["source_content"] &&
                (string)metadata["generated_metadata_hash"] == (string)binding["generated_metadata"];
            bool frameMatches = (int?)frame["width"] == (int?)grid["width"] && (int?)frame["height"] == (int?)grid["height"] &&
                (double?)frame["cell_size"] == (double?)grid["cell_size"] && origin != null && currentOrigin != null &&
                origin.Count == 3 && currentOrigin.Count == 3 && !origin.Where((v, i) => (double)v != (double)currentOrigin[i]).Any();
            return new JObject
            {
                ["kind"] = "S073NativeSourceBindingDiagnostic",
                ["evidence_qualified"] = true,
                ["metadata_matches_current_source"] = metadataMatches,
                ["grid_frame_matches_current_source"] = frameMatches,
                ["binding_metadata_matches_source"] = metadataMatches && frameMatches,
                ["observed_operation_map_id"] = (string)map["operation_map_id"],
                ["observed_grid_width"] = (int)grid["width"], ["observed_grid_height"] = (int)grid["height"],
                ["mission_ready"] = false, ["source_fit_certified"] = false,
                ["normal_input_playthrough"] = false, ["player_readiness"] = false,
                ["state_transferred"] = false
            };
        }
    }
}
