using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    /// <summary>
    /// Validates the durable evidence for two same-session native water observations
    /// and compares their mask hash with a caller-provided S073 source mask hash.
    /// This is a diagnostic only: it does not change source state or admit gameplay.
    /// </summary>
    internal static class SourceS073NativeStartupWaterParity
    {
        const int Width = 2048;
        const int Height = 1024;
        const int CellCount = Width * Height;
        const string Identity = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        const string Content = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        const string JointScope = "SameActualSession_OriginalPreparingCachedMaskVsPlayingOriginalCreateMask_NotReadinessOrSourceFit";
        const string StartupKind = "S073NativeStartupWaterMaskObservation";
        const string PlayingKind = "S073NativeWaterMaskObservation";
        const string Encoding = "uint8-bool01-row-major-xfast";
        const string R2JointReceiptSha256 = "59cb4faaef863cc56f9de5107945bf5c057f21a56980de5caf1341cbfa0a81aa";
        const string R5JointReceiptSha256 = "623f886f91605bab63c3190383b843aeb7ba114d7aaeb24a5acef9cd8078a966";
        const string R1CurrentJointReceiptSha256 = "a277dc72fc56b7ade4c2a4232a91ecce1103ca70fdeb1b1ba60a8e57dd279bd2";
        const string LoadedSourceR2JointReceiptSha256 = "a1991821818bf53b31d66c7773ef2a68e10645ed9af744236266e0c04031d8c1";
        const string R3NativeOccupancyJointReceiptSha256 = "ad2dc2aa0bd7c4009628ae1ec53885a713feef43a9abcdb43d3da069eb6e1e4a";
        const string WrapperExitSha256 = "e89df0eae5ec7db81135aed16463968955c578bcc4f2ec43172eff351f080c17";

        internal static JObject Validate(string repositoryRoot, string jointReceiptPath, string jointReceiptSha256,
            string providedS073WaterMaskSha256)
        {
            string repo = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string jointPath = Path.GetFullPath(jointReceiptPath);
            Require(jointPath.StartsWith(repo, StringComparison.Ordinal), "Joint receipt must be inside the repository");
            VerifyArtifact(jointPath, jointReceiptSha256, "Joint receipt");
            RequireDigest(providedS073WaterMaskSha256, "Provided S073 water mask hash");
            JObject joint = ReadJson(jointPath);
            Require((string)joint["kind"] == "S073JointNativeWaterMaskComparison" &&
                    (int?)joint["schema_version"] == 1 && (string)joint["evidence_validation"] == "Passed" &&
                    (string)joint["parity_status"] == "Match" && (bool?)joint["masks_match"] == true &&
                    (int?)joint["differing_cells"] == 0 && (int?)joint["wrapper_exit_code"] == 0 &&
                    (bool?)joint["required_markers_present"] == true && (string)joint["comparison_scope"] == JointScope,
                "Qualified passing S073 joint startup/playing comparison required");
            Require((bool?)joint["campaign_gameplay_state_copied"] == false &&
                    (bool?)joint["source_fit_certified"] == false && (bool?)joint["mission_ready"] == false &&
                    (bool?)joint["normal_input_playthrough"] == false && (bool?)joint["player_readiness"] == false,
                "Joint evidence must remain source-only and non-readiness evidence");

            string evidenceDirectory = Path.GetDirectoryName(jointPath);
            JObject startupCapture = joint["startup_capture"] as JObject ?? throw new InvalidDataException("Startup capture descriptor required");
            JObject playingCapture = joint["playing_capture"] as JObject ?? throw new InvalidDataException("Playing capture descriptor required");
            string evidenceRelative = Path.GetRelativePath(repo, evidenceDirectory).Replace('\\', '/');
            string startupObservationName, playingObservationName, startupMaskName, playingMaskName;
            string startupOriginalObservationName, playingOriginalObservationName, startupOriginalMaskName, playingOriginalMaskName;
            bool wrapperExitFileRequired;
            bool useOriginalCapturePaths;
            bool currentR3 = false;
            if (evidenceRelative == "Logs/SkirmishProduction/s073-native-startup-playing-water-20261101-r2" &&
                jointReceiptSha256 == R2JointReceiptSha256)
            {
                startupObservationName = "startup-snapshot.json";
                playingObservationName = "playing-snapshot.json";
                startupMaskName = "startup-mask.bin";
                playingMaskName = "playing-mask.bin";
                startupOriginalObservationName = "warline-s073-start-water-20261101-r2.json";
                playingOriginalObservationName = "warline-s073-play-water-20261101-r2.json";
                startupOriginalMaskName = "warline-s073-start-water-20261101-r2.water.bin";
                playingOriginalMaskName = "warline-s073-play-water-20261101-r2.water.bin";
                wrapperExitFileRequired = true;
                useOriginalCapturePaths = false;
            }
            else if (evidenceRelative == "Logs/SkirmishProduction/s073-native-startup-playing-water-20261101-r5" &&
                     jointReceiptSha256 == R5JointReceiptSha256)
            {
                startupObservationName = "startup-snapshot.json";
                playingObservationName = "snapshot.json";
                startupMaskName = "startup-snapshot.water.bin";
                playingMaskName = "snapshot.water.bin";
                startupOriginalObservationName = "warline-s073-start-water-20261101-r5.json";
                playingOriginalObservationName = "warline-s073-play-water-20261101-r5.json";
                startupOriginalMaskName = "warline-s073-start-water-20261101-r5.water.bin";
                playingOriginalMaskName = "warline-s073-play-water-20261101-r5.water.bin";
                wrapperExitFileRequired = true;
                useOriginalCapturePaths = false;
            }
            else if (evidenceRelative == "Logs/SkirmishProduction/s073-native-startup-playing-water-20261102-r1" &&
                     jointReceiptSha256 == R1CurrentJointReceiptSha256)
            {
                startupObservationName = "startup-snapshot.json";
                playingObservationName = "snapshot.json";
                startupMaskName = "startup-snapshot.water.bin";
                playingMaskName = "snapshot.water.bin";
                startupOriginalObservationName = "s073-water-current-20261102-r1-start.json";
                playingOriginalObservationName = "s073-water-current-20261102-r1-playing.json";
                startupOriginalMaskName = "s073-water-current-20261102-r1-start.water.bin";
                playingOriginalMaskName = "s073-water-current-20261102-r1-playing.water.bin";
                wrapperExitFileRequired = true;
                useOriginalCapturePaths = true;
            }
            else if (evidenceRelative == "Logs/SkirmishProduction/s073-loaded-source-fit-20261102-r2" &&
                     jointReceiptSha256 == LoadedSourceR2JointReceiptSha256)
            {
                startupObservationName = "s073-loaded-source-fit-20261102-r2-start.json";
                playingObservationName = "s073-loaded-source-fit-20261102-r2-playing.json";
                startupMaskName = "s073-loaded-source-fit-20261102-r2-start.water.bin";
                playingMaskName = "s073-loaded-source-fit-20261102-r2-playing.water.bin";
                startupOriginalObservationName = "s073-loaded-source-fit-20261102-r2-start.json";
                playingOriginalObservationName = "s073-loaded-source-fit-20261102-r2-playing.json";
                startupOriginalMaskName = "s073-loaded-source-fit-20261102-r2-start.water.bin";
                playingOriginalMaskName = "s073-loaded-source-fit-20261102-r2-playing.water.bin";
                wrapperExitFileRequired = false;
                useOriginalCapturePaths = true;
            }
            else if (evidenceRelative == "Logs/SkirmishProduction/s073-native-playing-occupancy-20261102-r3-finalized-r2" &&
                     jointReceiptSha256 == R3NativeOccupancyJointReceiptSha256)
            {
                startupObservationName = "startup-snapshot.json";
                playingObservationName = "snapshot.json";
                startupMaskName = "startup-snapshot.water.bin";
                playingMaskName = "snapshot.water.bin";
                startupOriginalObservationName = "s073-native-playing-occupancy-20261102-r3-start.json";
                playingOriginalObservationName = "s073-native-playing-occupancy-20261102-r3-playing.json";
                startupOriginalMaskName = "s073-native-playing-occupancy-20261102-r3-start.water.bin";
                playingOriginalMaskName = "s073-native-playing-occupancy-20261102-r3-playing.water.bin";
                wrapperExitFileRequired = false;
                useOriginalCapturePaths = true;
                currentR3 = true;
            }
            else
                throw new InvalidDataException("Only exact qualified S073 r2, r5, current r1, or native occupancy r3 joint water receipt revisions are admitted");

            string startupSnapshotPath = Path.Combine(evidenceDirectory, startupObservationName);
            string playingSnapshotPath = Path.Combine(evidenceDirectory, playingObservationName);
            string startupMaskPath = Path.Combine(evidenceDirectory, startupMaskName);
            string playingMaskPath = Path.Combine(evidenceDirectory, playingMaskName);
            string waterReceiptPath = Path.Combine(evidenceDirectory, "water-receipt.json");
            string editorLogPath = Path.Combine(evidenceDirectory, "editor.log");
            string wrapperExitPath = Path.Combine(evidenceDirectory, "wrapper-exit.json");
            string startupOriginalObservationPath = (string)startupCapture["observation_path"];
            string playingOriginalObservationPath = (string)playingCapture["observation_path"];
            string startupOriginalMaskPath = (string)startupCapture["mask_path"];
            string playingOriginalMaskPath = (string)playingCapture["mask_path"];
            if (useOriginalCapturePaths)
            {
                startupSnapshotPath = startupOriginalObservationPath;
                playingSnapshotPath = playingOriginalObservationPath;
                startupMaskPath = startupOriginalMaskPath;
                playingMaskPath = playingOriginalMaskPath;
            }
            RequireOriginalPath(startupOriginalObservationPath, startupOriginalObservationName, "Original startup observation");
            RequireOriginalPath(startupOriginalMaskPath, startupOriginalMaskName, "Original startup mask");
            RequireOriginalPath(playingOriginalObservationPath, playingOriginalObservationName, "Original playing observation");
            RequireOriginalPath(playingOriginalMaskPath, playingOriginalMaskName, "Original playing mask");
            Require(Path.GetDirectoryName(startupOriginalObservationPath) == Path.GetDirectoryName(playingOriginalObservationPath) &&
                    Path.GetDirectoryName(startupOriginalObservationPath) == Path.GetDirectoryName(startupOriginalMaskPath) &&
                    Path.GetDirectoryName(startupOriginalObservationPath) == Path.GetDirectoryName(playingOriginalMaskPath) &&
                    (string)playingCapture["receipt_path"] == waterReceiptPath &&
                    (string)joint["editor_log_path"] == editorLogPath &&
                    joint["wrapper_exit_path"] == null,
                "Native observation, mask, receipt, log, and wrapper paths must match the exact selected revision");

            byte[] startupSnapshotBytes = File.ReadAllBytes(startupSnapshotPath);
            byte[] playingSnapshotBytes = File.ReadAllBytes(playingSnapshotPath);
            byte[] startupMaskBytes = File.ReadAllBytes(startupMaskPath);
            byte[] playingMaskBytes = File.ReadAllBytes(playingMaskPath);
            Require(File.Exists(startupOriginalObservationPath) && File.Exists(playingOriginalObservationPath) &&
                    File.Exists(startupOriginalMaskPath) && File.Exists(playingOriginalMaskPath) &&
                    File.ReadAllBytes(startupOriginalObservationPath).SequenceEqual(startupSnapshotBytes) &&
                    File.ReadAllBytes(playingOriginalObservationPath).SequenceEqual(playingSnapshotBytes) &&
                    File.ReadAllBytes(startupOriginalMaskPath).SequenceEqual(startupMaskBytes) &&
                    File.ReadAllBytes(playingOriginalMaskPath).SequenceEqual(playingMaskBytes),
                "Revision-specific original captures must byte-match their durable receipt-derived copies");
            Require(Hash(startupSnapshotBytes) == (string)startupCapture["observation_sha256"] &&
                    Hash(startupSnapshotBytes) == (string)joint["startup_capture"]["observation_sha256"],
                "Durable startup snapshot must match the original separate capture hash");
            Require(Hash(playingSnapshotBytes) == (string)playingCapture["observation_sha256"] &&
                    Hash(playingSnapshotBytes) == (string)joint["playing_capture"]["observation_sha256"],
                "Durable playing snapshot must match the original separate capture hash");
            Require(Hash(startupMaskBytes) == (string)startupCapture["mask_sha256"] &&
                    Hash(startupMaskBytes) == (string)joint["startup_capture"]["mask_sha256"],
                "Durable startup mask must match the original separate capture hash");
            Require(Hash(playingMaskBytes) == (string)playingCapture["mask_sha256"] &&
                    Hash(playingMaskBytes) == (string)joint["playing_capture"]["mask_sha256"],
                "Durable playing mask must match the original separate capture hash");

            JObject startup = ReadJson(startupSnapshotPath);
            JObject playing = ReadJson(playingSnapshotPath);
            ValidateObservation(startup, StartupKind, "Preparing", startupCapture, startupMaskBytes);
            ValidateObservation(playing, PlayingKind, "Playing", playingCapture, playingMaskBytes);
            Require((string)startup["session_id"] == (string)playing["session_id"] &&
                    (string)joint["session_id"] == (string)startup["session_id"] &&
                    (string)joint["world_name"] == (string)startup["world_name"] &&
                    (string)joint["world_sequence"] == (string)startup["world_sequence"] &&
                    (string)startup["world_name"] == (string)playing["world_name"] &&
                    (string)startup["world_sequence"] == (string)playing["world_sequence"],
                "Original preparing and playing captures must be from the same session and world");
            Require((string)startup["mask_sha256"] == (string)playing["mask_sha256"] &&
                    startupMaskBytes.SequenceEqual(playingMaskBytes) &&
                    (string)startupCapture["mask_sha256"] == (string)playingCapture["mask_sha256"] &&
                    (string)joint["playing_capture"]["mask_sha256"] == (string)playing["mask_sha256"],
                "Separate original startup and playing mask bytes must match exactly");
            Require((string)startup["source_identity_hash"] == Identity &&
                    (string)playing["source_identity_hash"] == Identity &&
                    (string)joint["source_identity_hash"] == Identity &&
                    (string)startup["source_content_hash"] == Content &&
                    (string)playing["source_content_hash"] == Content &&
                    (string)joint["source_content_hash"] == Content,
                "Exact S073 map identity and content required in both original observations");
            JObject grid = startup["grid"] as JObject;
            Require(grid != null && JToken.DeepEquals(grid, playing["grid"]) && JToken.DeepEquals(grid, joint["grid"]) &&
                    (int?)grid["width"] == Width && (int?)grid["height"] == Height && (float?)grid["cell_size"] == 1f &&
                    ExactZeroOrigin(grid["origin"] as JArray), "Exact shared S073 grid frame required");

            VerifySourcePins(repo, startup["source_pins_sha256"] as JObject, playing["source_pins_sha256"] as JObject,
                joint["shared_source_pins_sha256"] as JObject);
            JObject waterReceipt = ReadJson(waterReceiptPath);
            byte[] waterReceiptBytes = File.ReadAllBytes(waterReceiptPath);
            VerifyArtifact((string)playingCapture["receipt_path"], (string)playingCapture["receipt_sha256"], "Water wrapper receipt");
            Require((string)playingCapture["receipt_path"] == waterReceiptPath &&
                    waterReceiptBytes.SequenceEqual(File.ReadAllBytes((string)playingCapture["receipt_path"])) &&
                    Hash(waterReceiptBytes) == (string)playingCapture["receipt_sha256"],
                "Durable water receipt must match the receipt named by the joint capture");
            string expectedPlayingObservationHash = currentR3
                ? (string)waterReceipt["original_observation_sha256"]
                : (string)waterReceipt["observation_sha256"];
            Require((string)waterReceipt["kind"] == "S073NativeWaterMaskCaptureReceipt" &&
                    (int?)waterReceipt["schema_version"] == 1 && (string)waterReceipt["validation_status"] == "Passed" &&
                    (int?)waterReceipt["wrapper_exit_code"] == 0 && (bool?)waterReceipt["required_marker_present"] == true &&
                    expectedPlayingObservationHash == (string)playingCapture["observation_sha256"] &&
                    (!currentR3 || (string)waterReceipt["original_observation_path"] == playingOriginalObservationPath) &&
                    (string)waterReceipt["mask_sha256"] == (string)playingCapture["mask_sha256"] &&
                    JToken.DeepEquals(waterReceipt["source_pins_sha256"], playing["source_pins_sha256"]),
                "Water wrapper receipt must bind the durable playing observation and its source pins");
            VerifyArtifact((string)joint["editor_log_path"], (string)joint["editor_log_sha256"], "Native editor log");
            Require((string)joint["editor_log_path"] == editorLogPath &&
                    (string)waterReceipt["editor_log_sha256"] == (string)joint["editor_log_sha256"],
                "Durable editor log hash must agree across joint and water receipts");
            if (wrapperExitFileRequired)
            {
                JObject wrapperExit = ReadJson(wrapperExitPath);
                Require(HashFile(wrapperExitPath) == WrapperExitSha256 && (int?)wrapperExit["exit_code"] == 0,
                    "Revision-qualified durable checked-wrapper exit must match its exact pinned bytes");
            }
            else
                Require((int?)joint["wrapper_exit_code"] == 0 &&
                        (int?)waterReceipt["wrapper_exit_code"] == 0 &&
                        (bool?)joint["required_markers_present"] == true &&
                        (bool?)waterReceipt["required_marker_present"] == true,
                    "Current wrapper exit and markers must be bound by both strict finalizer receipts");
            string log = File.ReadAllText(editorLogPath);
            Require(log.Contains("[S073NativeStartupWaterMask] result=Recorded frame=" + (int)startup["frame"] + " cells=" + CellCount + " waterCells=" + (int)startup["water_cells"] + " path=", StringComparison.Ordinal) &&
                    log.Contains("[S073NativeWaterMask] result=Recorded cells=" + CellCount + " waterCells=" + (int)playing["water_cells"] + " path=", StringComparison.Ordinal) &&
                    log.Contains("path=" + (string)startupCapture["observation_path"], StringComparison.Ordinal) &&
                    log.Contains("path=" + (string)playingCapture["observation_path"], StringComparison.Ordinal),
                "Both separate native capture markers must appear in the durable editor log");

            bool s073MaskMatches = providedS073WaterMaskSha256 == (string)playing["mask_sha256"];
            return new JObject
            {
                ["kind"] = "S073NativeStartupWaterParityDiagnostic",
                ["status"] = s073MaskMatches ? "JointNativeEvidenceValid_S073MaskMatches" : "JointNativeEvidenceValid_S073MaskDiffers",
                ["diagnostic_only"] = true,
                ["comparison_scope"] = "OriginalSeparatePreparingAndPlayingNativeMasks_ComparedByBytesAndAgainstProvidedS073MaskHash",
                ["joint_receipt_sha256"] = jointReceiptSha256,
                ["startup_observation_sha256"] = (string)startupCapture["observation_sha256"],
                ["playing_observation_sha256"] = (string)playingCapture["observation_sha256"],
                ["startup_mask_sha256"] = (string)startup["mask_sha256"],
                ["playing_mask_sha256"] = (string)playing["mask_sha256"],
                ["provided_s073_mask_sha256"] = providedS073WaterMaskSha256,
                ["startup_playing_bytes_match"] = startupMaskBytes.SequenceEqual(playingMaskBytes),
                ["s073_mask_matches_playing_mask_hash"] = s073MaskMatches,
                ["mask_cells"] = CellCount,
                ["startup_water_cells"] = (int)startup["water_cells"],
                ["playing_water_cells"] = (int)playing["water_cells"],
                ["session_id"] = (string)joint["session_id"],
                ["world_name"] = (string)joint["world_name"],
                ["world_sequence"] = (string)joint["world_sequence"],
                ["source_identity_hash"] = Identity,
                ["source_content_hash"] = Content,
                ["campaign_gameplay_state_copied"] = false,
                ["source_fit_certified"] = false,
                ["scheduler_admitted"] = false,
                ["mission_ready"] = false,
                ["normal_input_playthrough"] = false,
                ["player_readiness"] = false
            };
        }

        static void ValidateObservation(JObject snapshot, string kind, string phase, JObject descriptor, byte[] mask)
        {
            Require((string)snapshot["kind"] == kind && (int?)snapshot["schema_version"] == 1 &&
                    (string)snapshot["phase"] == phase && (int?)snapshot["scenario_index"] == 3 &&
                    (int?)snapshot["seed"] == 104729 && (string)snapshot["operation_map_id"] == "opmap.skirmish.industrial_basin" &&
                    (bool?)snapshot["physical_source_validated"] == true &&
                    (bool?)snapshot["campaign_gameplay_state_copied"] == false &&
                    (bool?)snapshot["source_fit_certified"] == false && (bool?)snapshot["mission_ready"] == false &&
                    (bool?)snapshot["normal_input_playthrough"] == false && (bool?)snapshot["player_readiness"] == false,
                "Original S073 observation identity or non-readiness scope changed: " + kind);
            Require((string)snapshot["mask_encoding"] == Encoding && (int?)snapshot["mask_cells"] == CellCount &&
                    (string)snapshot["mask_sha256"] == (string)descriptor["mask_sha256"] &&
                    (string)snapshot["mask_path"] == (string)descriptor["mask_path"] &&
                    mask.Length == CellCount && mask.All(value => value <= 1) &&
                    mask.Count(value => value != 0) == (int?)snapshot["water_cells"],
                "Complete row-major bool01 mask and population required: " + kind);
            Require((string)snapshot["provider_type"] == "Game.Rendering.RenderBoundsPresentation+Reader, Game.Rendering, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" &&
                    File.Exists((string)snapshot["provider_assembly_path"]) &&
                    HashFile((string)snapshot["provider_assembly_path"]) == (string)snapshot["provider_assembly_sha256"],
                "Captured original water provider assembly changed: " + kind);
            if (phase == "Playing")
                Require((bool?)snapshot["provider_reference_unchanged"] == true &&
                        (long?)snapshot["entity_order_version_before"] == (long?)snapshot["entity_order_version_after"],
                    "Playing observation must retain provider and non-structural-write evidence");
            else
                Require((long?)snapshot["entity_order_version_before"] == (long?)snapshot["entity_order_version_after"],
                    "Preparing observation must retain non-structural-write evidence");
        }

        static void VerifySourcePins(string repo, JObject startupPins, JObject playingPins, JObject sharedPins)
        {
            Require(startupPins != null && playingPins != null && sharedPins != null,
                "Startup, playing, and shared source pins are required");
            VerifyPins(repo, startupPins);
            VerifyPins(repo, playingPins);
            var intersection = startupPins.Properties().Where(pin => playingPins[pin.Name] != null).ToDictionary(pin => pin.Name, pin => (string)pin.Value);
            Require(intersection.Count == sharedPins.Count && sharedPins.Properties().All(pin =>
                    intersection.TryGetValue(pin.Name, out string value) && value == (string)pin.Value),
                "Joint shared source pin set must equal the exact startup/playing pin intersection");
            foreach (JProperty pin in sharedPins.Properties())
                Require((string)startupPins[pin.Name] == (string)pin.Value && (string)playingPins[pin.Name] == (string)pin.Value,
                    "Joint shared source pin differs from an original observation: " + pin.Name);
        }

        static void VerifyPins(string repo, JObject pins)
        {
            Require(pins.Count >= 5, "Complete observation source pin set required");
            foreach (JProperty pin in pins.Properties())
            {
                string relative = pin.Name;
                if (Path.IsPathRooted(relative) || relative.Contains("\\") ||
                    relative.Split('/').Any(part => part == ".." || part == "." || part.Length == 0))
                    throw new InvalidDataException("Unsafe native water source pin path: " + relative);
                RequireDigest((string)pin.Value, "Source pin " + relative);
                string path = Path.GetFullPath(Path.Combine(repo, relative.Replace('/', Path.DirectorySeparatorChar)));
                Require(path.StartsWith(repo, StringComparison.Ordinal) && File.Exists(path) && HashFile(path) == (string)pin.Value,
                    "Current source pin changed: " + relative);
            }
        }

        static void VerifyArtifact(string path, string expectedSha, string label)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !IsDigest(expectedSha) ||
                !File.Exists(path) || HashFile(path) != expectedSha)
                throw new InvalidDataException(label + " descriptor or SHA-256 mismatch");
        }

        static void RequireOriginalPath(string path, string expectedName, string label)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || Path.GetFileName(path) != expectedName)
                throw new InvalidDataException(label + " must retain its exact revision-qualified original capture path");
        }

        static JObject ReadJson(string path)
        {
            using (var file = File.OpenText(path))
            using (var reader = new JsonTextReader(file) { MaxDepth = 64, DateParseHandling = DateParseHandling.None })
                return JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        static bool ExactZeroOrigin(JArray origin) => origin != null && origin.Count == 3 &&
            (float?)origin[0] == 0f && (float?)origin[1] == 0f && (float?)origin[2] == 0f;
        static bool IsDigest(string value) => value != null && value.Length == 64 &&
            value.All(character => character >= '0' && character <= '9' || character >= 'a' && character <= 'f');
        static void RequireDigest(string value, string label)
        {
            if (!IsDigest(value)) throw new InvalidDataException(label + " must be lower-case SHA-256");
        }
        static string HashFile(string path) => Hash(File.ReadAllBytes(path));
        static string Hash(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }
    }
}
