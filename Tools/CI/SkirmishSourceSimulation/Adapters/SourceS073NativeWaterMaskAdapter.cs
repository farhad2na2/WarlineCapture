using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    /// <summary>
    /// Validates an opt-in live S073 native water-mask observation and compares
    /// it with the water mask produced by the already loaded placement domain.
    /// This reports source parity only; it does not admit gameplay readiness.
    /// </summary>
    public static class SourceS073NativeWaterMaskAdapter
    {
        const string Identity = "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900";
        const string Content = "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c";
        const string SharedCampaignWaterReference = "acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5";
        const string BoundCampaignScene = "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";
        const string BoundCampaignSceneGuid = "c00140f2e94a04c3084c8dcb0c18cbd0";
        const string LoadedSourceR2ReceiptSha256 = "ba28b7dc90e6db335686df62b64c1ef6246a056cfaa1648f94ca4cf5a23f561a";
        const string LoadedSourceR2ObservationSha256 = "9a5edbc286106da65bb4daec8c2b00bb694f46d7f4ab17ceb1f00a9e2729c4c8";
        const string LoadedSourceR2NormalizedObservationSha256 = "165c1efdc74e6c5ab00e5a9cd5a90504ef0a32e26d1a538b2d585d48d95fad3f";
        const string LoadedSourceR2MaskSha256 = "5647f05ec18958947d32874eeb788fa396a05d0bab7c1b71f112ceb7e9b31eee";
        const int Width = 2048;
        const int Height = 1024;

        static readonly string[] RequiredPins =
        {
            "Assets/Game/Scripts/Editor/Skirmish/S073NativeWaterMaskSnapshot.cs",
            "Assets/Game/Scripts/Systems/SkirmishWaterPlacement.cs",
            "Assets/Game/Scripts/Systems/BuildingPlacementAuthoredRoadCache.cs",
            "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs",
            "Assets/Game/Scripts/Rendering/Contracts/IRenderBoundsPresentation.cs",
            "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
            "Assets/Game/Scenes/Match.unity",
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity",
            BoundCampaignScene,
            BoundCampaignScene + ".meta"
        };

        /// <summary>
        /// Reads the exact native mask and returns it as bool[]. The comparison
        /// object labels the shared Campaign source reference explicitly; parity
        /// remains static geometry evidence and never implies mission readiness.
        /// </summary>
        public static bool[] ReadQualified(
            SourcePlacementDomain domain,
            string repositoryRoot,
            string snapshotPath,
            string snapshotSha256,
            string maskPath,
            string maskSha256,
            string receiptPath,
            string receiptSha256,
            out JObject comparison)
        {
            comparison = null;
            if (domain == null) throw new ArgumentNullException(nameof(domain));
            if (string.IsNullOrWhiteSpace(repositoryRoot) || !Path.IsPathRooted(repositoryRoot))
                throw new InvalidDataException("Explicit absolute S073 source repository root required");
            VerifyArtifact(snapshotPath, snapshotSha256, "S073 native water snapshot");
            VerifyArtifact(maskPath, maskSha256, "S073 native water mask");
            VerifyArtifact(receiptPath, receiptSha256, "S073 native water receipt");

            JObject snapshot = ReadJson(snapshotPath);
            JObject receipt = ReadJson(receiptPath);
            string repository = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            bool loadedSourceR2 = receiptSha256 == LoadedSourceR2ReceiptSha256 &&
                Path.GetFullPath(receiptPath) == Path.Combine(repository, "Logs", "SkirmishProduction", "s073-loaded-source-fit-20261102-r2", "water-receipt.json") &&
                snapshotSha256 == LoadedSourceR2NormalizedObservationSha256 && maskSha256 == LoadedSourceR2MaskSha256;
            if (loadedSourceR2)
            {
                string originalPath = (string)receipt["original_observation_path"];
                string expectedOriginalPath = Path.Combine(Path.GetPathRoot(repositoryRoot), "private", "tmp", "s073-loaded-source-fit-20261102-r2-playing.json");
                Require(Path.GetFullPath(originalPath) == expectedOriginalPath &&
                        (string)receipt["original_observation_sha256"] == LoadedSourceR2ObservationSha256 &&
                        (string)receipt["normalized_observation_path"] == Path.GetFullPath(snapshotPath) &&
                        (string)receipt["normalized_observation_sha256"] == snapshotSha256 &&
                        File.Exists(originalPath) && HashFile(originalPath) == LoadedSourceR2ObservationSha256,
                    "Loaded-source r2 receipt must bind its exact original and normalized observations");
            }
            Require((string)snapshot["kind"] == "S073NativeWaterMaskObservation" &&
                    (int?)snapshot["schema_version"] == 1 &&
                    (string)snapshot["scope"] == "SameSessionOriginalSkirmishWaterPlacementCreateMask_NotReadinessOrMissionAcceptance" &&
                    (string)snapshot["authority"] == "OriginalSkirmishWaterPlacement.CreateMask_OriginalRenderBoundsGatewayProvider",
                "Exact opt-in S073 original-owner water observation required");
            Require((int?)snapshot["scenario_index"] == 3 && (int?)snapshot["seed"] == 104729 &&
                    (string)snapshot["phase"] == "Playing" && (string)snapshot["unity_version"] == "6000.5.8f1" &&
                    !string.IsNullOrEmpty((string)snapshot["session_id"]),
                "Same-session S073 scenario 3 / seed 104729 Playing observation required");
            Require((string)snapshot["operation_map_id"] == "opmap.skirmish.industrial_basin" &&
                    (string)snapshot["source_identity_hash"] == Identity &&
                    (string)snapshot["source_content_hash"] == Content &&
                    (bool?)snapshot["physical_source_validated"] == true,
                "Exact validated S073 physical source binding required");
            Require((bool?)snapshot["mission_ready"] == false &&
                    (bool?)snapshot["source_fit_certified"] == false &&
                    (bool?)snapshot["normal_input_playthrough"] == false &&
                    (bool?)snapshot["player_readiness"] == false &&
                    (bool?)snapshot["campaign_gameplay_state_copied"] == false,
                "Water observation must retain its non-readiness, source-only scope");

            JObject grid = snapshot["grid"] as JObject;
            JObject loadedSourceFit = snapshot["loaded_campaign_source_fit_observation"] as JObject;
            JObject sourcePins = snapshot["source_pins_sha256"] as JObject;
            Require(loadedSourceFit != null && sourcePins != null &&
                    (string)loadedSourceFit["kind"] == "S073SameSessionLoadedCampaignSourceFitObservation" &&
                    (string)loadedSourceFit["scope"] == "SamePlayingWorld_LoadedCampaignSceneOwnerAndSpawnInputs_DiagnosticOnly" &&
                    (string)loadedSourceFit["session_id"] == (string)snapshot["session_id"] &&
                    (string)loadedSourceFit["world_name"] == (string)snapshot["world_name"] &&
                    (string)loadedSourceFit["world_sequence"] == (string)snapshot["world_sequence"] &&
                    (string)loadedSourceFit["source_scene_guid"] == BoundCampaignSceneGuid &&
                    (string)loadedSourceFit["source_scene_path"] == BoundCampaignScene &&
                    (string)loadedSourceFit["source_scene_sha256"] == (string)sourcePins[BoundCampaignScene] &&
                    (string)loadedSourceFit["source_scene_meta_sha256"] == (string)sourcePins[BoundCampaignScene + ".meta"] &&
                    (bool?)loadedSourceFit["scene_loaded"] == true &&
                    (bool?)loadedSourceFit["grid_owned_by_campaign_scene"] == true &&
                    (bool?)loadedSourceFit["source_fit_certified"] == false &&
                    (bool?)loadedSourceFit["physical_source_admission_changed"] == false &&
                    (bool?)loadedSourceFit["scheduler_admitted"] == false &&
                    (bool?)loadedSourceFit["mission_ready"] == false &&
                    (bool?)loadedSourceFit["player_readiness"] == false,
                "Exact same-session loaded Campaign scene pin and non-admission source-fit diagnostic required");

            // Keep the native loaded-scene census as a separately scoped input
            // record. It describes a different captured Unity session than this
            // source host, so it can strengthen source correspondence evidence
            // but must never be treated as a loaded scene or admission signal.
            JObject loadedSourceFitEvidence = BuildLoadedSourceFitEvidence(loadedSourceFit, grid);
            bool loadedSourceFitTamperControlsPassed = RunLoadedSourceFitTamperControls(loadedSourceFit, grid);

            Require(grid != null && (int?)grid["width"] == Width && (int?)grid["height"] == Height &&
                    (float?)grid["cell_size"] == 1f && ExactZeroOrigin(grid["origin"] as JArray),
                "Exact S073 2048x1024 / 1m / origin-zero frame required");
            Require((string)snapshot["mask_encoding"] == "uint8-bool01-row-major-xfast" &&
                    (int?)snapshot["mask_cells"] == Width * Height &&
                    (string)snapshot["mask_sha256"] == maskSha256 &&
                    !string.IsNullOrEmpty((string)snapshot["mask_path"]) && Path.IsPathRooted((string)snapshot["mask_path"]),
                "Snapshot must bind a complete row-major native mask artifact");
            Require((bool?)snapshot["provider_reference_unchanged"] == true &&
                    (long?)snapshot["entity_order_version_before"] == (long?)snapshot["entity_order_version_after"] &&
                    ((string)snapshot["provider_type"] ?? "").StartsWith("Game.Rendering.RenderBoundsPresentation+Reader,", StringComparison.Ordinal) &&
                    (int?)snapshot["water_footprint_count"] >= 0,
                "Original provider, footprint census and no-structural-write evidence required");

            string snapshotHash = HashFile(snapshotPath);
            Require((string)receipt["kind"] == "S073NativeWaterMaskCaptureReceipt" &&
                    (int?)receipt["schema_version"] == 1 &&
                    (string)receipt["validation_status"] == "Passed" &&
                    (int?)receipt["wrapper_exit_code"] == 0 &&
                    (bool?)receipt["required_marker_present"] == true &&
                    ((string)receipt["observation_sha256"] == snapshotHash || loadedSourceR2 &&
                     (string)receipt["original_observation_sha256"] == LoadedSourceR2ObservationSha256 &&
                     (string)receipt["normalized_observation_sha256"] == snapshotHash) &&
                    (string)receipt["mask_sha256"] == maskSha256 &&
                    (string)receipt["source_identity_hash"] == Identity &&
                    (string)receipt["source_content_hash"] == Content &&
                    (string)receipt["session_id"] == (string)snapshot["session_id"] &&
                    (string)receipt["world_name"] == (string)snapshot["world_name"] &&
                    (string)receipt["world_sequence"] == (string)snapshot["world_sequence"] &&
                    (string)receipt["operation_map_id"] == "opmap.skirmish.industrial_basin" &&
                    JToken.DeepEquals(receipt["grid"], snapshot["grid"]) &&
                    (string)receipt["provider_type"] == (string)snapshot["provider_type"] &&
                    (string)receipt["provider_assembly_path"] == (string)snapshot["provider_assembly_path"] &&
                    (int?)receipt["water_footprint_count"] == (int?)snapshot["water_footprint_count"] &&
                    (string)receipt["provider_assembly_sha256"] == (string)snapshot["provider_assembly_sha256"] &&
                    (string)receipt["mask_encoding"] == "uint8-bool01-row-major-xfast" &&
                    (int?)receipt["mask_cells"] == Width * Height &&
                    (int?)receipt["water_cells"] == (int?)snapshot["water_cells"] &&
                    JToken.DeepEquals(receipt["loaded_campaign_source_fit_observation"],
                        snapshot["loaded_campaign_source_fit_observation"]) &&
                    IsDigest((string)receipt["editor_log_sha256"]) &&
                    (bool?)receipt["campaign_gameplay_state_copied"] == false &&
                    (bool?)receipt["mission_ready"] == false && (bool?)receipt["source_fit_certified"] == false &&
                    (bool?)receipt["normal_input_playthrough"] == false && (bool?)receipt["player_readiness"] == false,
                "Checked wrapper receipt must bind this exact same-session S073 observation and mask");

            VerifyPins(repositoryRoot, snapshot["source_pins_sha256"] as JObject, receipt["source_pins_sha256"] as JObject);
            string evidenceDirectory = Path.GetDirectoryName(Path.GetFullPath(receiptPath));
            string editorLogPath = Path.Combine(evidenceDirectory, "editor.log");
            if (!File.Exists(editorLogPath) || HashFile(editorLogPath) != (string)receipt["editor_log_sha256"])
                throw new InvalidDataException("Durable S073 water editor log differs from its receipt pin");
            string marker = "[S073NativeWaterMask] result=Recorded cells=2097152 waterCells=" + (int)snapshot["water_cells"] + " path=";
            if (!File.ReadLines(editorLogPath).Any(line => line.StartsWith(marker, StringComparison.Ordinal)))
                throw new InvalidDataException("Exact S073 native water mask pass marker is missing");
            string wrapperExitPath = Path.Combine(evidenceDirectory, "wrapper-exit.json");
            if (loadedSourceR2)
            {
                Require(receipt["wrapper_exit_path"] == null && (int?)receipt["wrapper_exit_code"] == 0 &&
                        (bool?)receipt["required_marker_present"] == true,
                    "Loaded-source r2 receipt must bind wrapper success and required marker without a separate wrapper file");
            }
            else if (!File.Exists(wrapperExitPath) || (int?)ReadJson(wrapperExitPath)["exit_code"] != 0)
                throw new InvalidDataException("Durable checked-wrapper exit evidence is missing or failed");
            string providerPath = (string)snapshot["provider_assembly_path"];
            string providerSha = (string)snapshot["provider_assembly_sha256"];
            Require(!string.IsNullOrEmpty(providerPath) && Path.IsPathRooted(providerPath) && IsDigest(providerSha) &&
                    File.Exists(providerPath) && HashFile(providerPath) == providerSha,
                "Captured original water provider assembly must remain hash-identical");

            byte[] bytes = File.ReadAllBytes(maskPath);
            if (bytes.Length != Width * Height || bytes.Any(value => value > 1))
                throw new InvalidDataException("Complete row-major uint8 bool01 native water mask required");
            int nativeWaterCells = bytes.Count(value => value != 0);
            if ((int?)snapshot["water_cells"] != nativeWaterCells)
                throw new InvalidDataException("Native water mask population differs from its snapshot");
            bool[] nativeMask = new bool[bytes.Length];
            for (int i = 0; i < bytes.Length; i++) nativeMask[i] = bytes[i] != 0;

            if (!domain.WaterDomainKnown)
                throw new InvalidOperationException("SourcePlacementDomain has no admitted water geometry; comparison cannot default to empty");
            bool[] computed = domain.BuildWaterMask(true, Width, Height, 1f, 0f, 0f, 0f);
            if (computed == null || computed.Length != nativeMask.Length)
                throw new InvalidDataException("SourcePlacementDomain did not produce a complete water projection");
            int differingCells = 0, computedWaterCells = 0;
            byte[] computedBytes = new byte[computed.Length];
            for (int i = 0; i < computed.Length; i++)
            {
                computedBytes[i] = computed[i] ? (byte)1 : (byte)0;
                if (computed[i]) computedWaterCells++;
                if (computed[i] != nativeMask[i]) differingCells++;
            }

            bool knownSharedCampaignReference = domain.WaterReferenceSha256 == SharedCampaignWaterReference;
            bool domainReferencePinned = IsDigest(domain.WaterReferenceSha256);
            bool footprintCountsMatch = domain.WaterFootprintCount == (int)snapshot["water_footprint_count"];
            bool masksMatch = differingCells == 0;
            bool comparisonAdmitted = domainReferencePinned && footprintCountsMatch && masksMatch;
            comparison = new JObject
            {
                ["kind"] = "S073NativeWaterMaskDomainComparison",
                ["status"] = !domainReferencePinned ? "Informational_DomainWaterReferenceUnpinned" :
                    (!footprintCountsMatch ? "FootprintCountsDiffer" :
                    (masksMatch ? (knownSharedCampaignReference ? "SharedCampaignSourceMasksMatch" : "MasksMatch") : "MasksDiffer")),
                ["source_comparison_admitted"] = comparisonAdmitted,
                ["comparison_scope"] = "PinnedStaticCampaignWaterGeometryParityOnly_NotMissionReadiness",
                ["native_mask_observed_phase"] = "Playing",
                ["placement_startup_time_equivalence_proven"] = false,
                ["mask_use_scope"] = "PostStartupNativeObservationUsedForSourcePlacementDiagnostic",
                ["native_mask_sha256"] = maskSha256,
                ["snapshot_sha256"] = snapshotHash,
                ["receipt_sha256"] = receiptSha256,
                ["computed_domain_mask_sha256"] = HashBytes(computedBytes),
                ["domain_water_reference_sha256"] = domain.WaterReferenceSha256,
                ["domain_water_reference_is_shared_campaign_capture"] = knownSharedCampaignReference,
                ["native_water_cells"] = nativeWaterCells,
                ["computed_water_cells"] = computedWaterCells,
                ["differing_cells"] = differingCells,
                ["footprint_counts_match"] = footprintCountsMatch,
                ["masks_match"] = masksMatch,
                ["native_footprint_count"] = (int)snapshot["water_footprint_count"],
                ["domain_footprint_count"] = domain.WaterFootprintCount,
                ["domain_query_count"] = domain.WaterQueryCount,
                ["source_identity_hash"] = Identity,
                ["source_content_hash"] = Content,
                ["loaded_campaign_source_fit_evidence"] = loadedSourceFitEvidence,
                ["loaded_campaign_source_fit_tamper_controls_passed"] = loadedSourceFitTamperControlsPassed,
                ["campaign_gameplay_state_copied"] = false,
                ["source_fit_certified"] = false,
                ["mission_ready"] = false,
                ["normal_input_playthrough"] = false,
                ["player_readiness"] = false
            };
            return nativeMask;
        }

        static void VerifyPins(string repositoryRoot, JObject snapshotPins, JObject receiptPins)
        {
            if (snapshotPins == null || receiptPins == null || !JToken.DeepEquals(snapshotPins, receiptPins))
                throw new InvalidDataException("Receipt and snapshot source pins must match exactly");
            foreach (string relative in RequiredPins)
            {
                string expected = (string)snapshotPins[relative];
                if (!IsDigest(expected)) throw new InvalidDataException("Required S073 water source pin missing: " + relative);
                string path = Path.GetFullPath(Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
                string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!path.StartsWith(root, StringComparison.Ordinal) || !File.Exists(path) || HashFile(path) != expected)
                    throw new InvalidDataException("Current S073 water observation source changed: " + relative);
            }
            foreach (JProperty pin in snapshotPins.Properties())
            {
                string relative = pin.Name;
                if (Path.IsPathRooted(relative) || relative.Contains("\\") || relative.Split('/').Any(part => part == ".." || part == "." || part.Length == 0))
                    throw new InvalidDataException("Unsafe S073 water source pin path");
                string path = Path.GetFullPath(Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
                string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!path.StartsWith(root, StringComparison.Ordinal) || !File.Exists(path) || HashFile(path) != (string)pin.Value)
                    throw new InvalidDataException("S073 water source pin changed: " + relative);
            }
        }

        static JObject BuildLoadedSourceFitEvidence(JObject loadedSourceFit, JObject grid)
        {
            JArray blockers = loadedSourceFit?["blocker_contributors"] as JArray;
            JArray buildings = loadedSourceFit?["building_entities"] as JArray;
            JArray initialBuildings = loadedSourceFit?["original_initial_building_spawn_inputs"] as JArray;
            JArray initialUnits = loadedSourceFit?["original_initial_unit_spawn_cells"] as JArray;
            JObject completeness = loadedSourceFit?["capture_completeness"] as JObject;
            int width = (int?)grid?["width"] ?? 0;
            int height = (int?)grid?["height"] ?? 0;
            Require(width == Width && height == Height && completeness != null &&
                    (bool?)completeness["loaded_campaign_scene"] == true &&
                    (bool?)completeness["active_grid_scene_ownership"] == true &&
                    (bool?)completeness["blocker_owner_inputs"] == true &&
                    (bool?)completeness["building_owner_inputs"] == true &&
                    (bool?)completeness["original_initial_spawn_plan"] == true &&
                    (bool?)loadedSourceFit["scene_loaded"] == true &&
                    (bool?)loadedSourceFit["grid_owned_by_campaign_scene"] == true &&
                    blockers != null && blockers.Count == 4964 &&
                    (int?)loadedSourceFit["blocker_contributor_query_count"] == blockers.Count &&
                    buildings != null && buildings.Count == 4931 &&
                    (int?)loadedSourceFit["building_owner_query_count"] == buildings.Count &&
                    initialBuildings != null && initialBuildings.Count == 10 &&
                    initialUnits != null && initialUnits.Count == 2 &&
                    (bool?)loadedSourceFit["original_initial_spawn_plan_available"] == true,
                "Loaded Campaign source-fit census and original startup owner inputs must be complete and internally consistent");

            var blockerIds = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            int sceneOwnedBlockers = 0;
            foreach (JObject row in blockers)
            {
                JArray entity = row?["entity"] as JArray;
                JArray cell = row?["grid_cell"] as JArray;
                JArray size = row?["size_cells"] as JArray;
                int x = (int?)cell?.ElementAtOrDefault(0) ?? -1;
                int y = (int?)cell?.ElementAtOrDefault(1) ?? -1;
                int sx = (int?)size?.ElementAtOrDefault(0) ?? 0;
                int sy = (int?)size?.ElementAtOrDefault(1) ?? 0;
                Require(entity != null && entity.Count == 2 &&
                        cell != null && cell.Count == 2 && size != null && size.Count == 2 &&
                        x >= 0 && y >= 0 && sx > 0 && sy > 0 &&
                        (bool?)row["has_static_blocker_history"] == true,
                    "Loaded Campaign blocker contributor row is malformed or lacks owner history");
                string id = entity[0].ToString(Formatting.None) + ":" + entity[1].ToString(Formatting.None);
                Require(blockerIds.Add(id), "Loaded Campaign blocker contributor entity is duplicated");
                if ((bool?)row["bound_campaign_scene_owned"] == true) sceneOwnedBlockers++;
            }
            Require(sceneOwnedBlockers == 4954 &&
                    (int?)loadedSourceFit["blocker_contributors_owned_by_campaign_scene"] == sceneOwnedBlockers,
                "Loaded Campaign blocker ownership rows do not match the captured scene-owner census");

            var buildingIds = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (JObject row in buildings)
            {
                JArray entity = row?["entity"] as JArray;
                string stable = (string)row?["stable_id"];
                Require(entity != null && entity.Count == 2 &&
                        (string)row["operation_map_id"] == "opmap.skirmish.desert_base_01" &&
                        (bool?)row["bound_campaign_scene_owned"] == true &&
                        !string.IsNullOrWhiteSpace(stable),
                    "Loaded Campaign authored building row is malformed or not scene-owned");
                string id = entity[0].ToString(Formatting.None) + ":" + entity[1].ToString(Formatting.None);
                Require(buildingIds.Add(id), "Loaded Campaign authored building entity is duplicated");
            }
            Require((int?)loadedSourceFit["building_entities_owned_by_campaign_scene"] == buildings.Count,
                "Loaded Campaign building owner count does not equal the row-by-row scene ownership count");

            return new JObject
            {
                ["kind"] = "S073LoadedCampaignSourceFitEvidenceReference",
                ["scope"] = "IndependentCapturedSession_SourceCorrespondenceDiagnosticOnly",
                ["capture_session_id"] = (string)loadedSourceFit["session_id"],
                ["capture_world_name"] = (string)loadedSourceFit["world_name"],
                ["capture_world_sequence"] = (string)loadedSourceFit["world_sequence"],
                ["source_scene_guid"] = (string)loadedSourceFit["source_scene_guid"],
                ["source_scene_path"] = (string)loadedSourceFit["source_scene_path"],
                ["source_scene_sha256"] = (string)loadedSourceFit["source_scene_sha256"],
                ["source_scene_meta_sha256"] = (string)loadedSourceFit["source_scene_meta_sha256"],
                ["grid_width"] = width,
                ["grid_height"] = height,
                ["grid_cell_size"] = (float)grid["cell_size"],
                ["blocker_contributor_count"] = blockers.Count,
                ["campaign_scene_owned_blocker_count"] = sceneOwnedBlockers,
                ["blocker_rows_checked"] = blockers.Count,
                ["building_entity_count"] = buildings.Count,
                ["campaign_scene_owned_building_count"] = buildings.Count,
                ["building_rows_checked"] = buildings.Count,
                ["initial_building_input_count"] = initialBuildings.Count,
                ["initial_unit_spawn_input_count"] = initialUnits.Count,
                ["native_scene_loaded_in_source_host"] = false,
                ["native_gameplay_state_copied"] = false,
                ["physical_source_validated"] = false,
                ["scheduler_admitted"] = false,
                ["mission_ready"] = false,
                ["player_readiness"] = false
            };
        }

        static bool RunLoadedSourceFitTamperControls(JObject loadedSourceFit, JObject grid)
        {
            JObject changedOwner = (JObject)loadedSourceFit.DeepClone();
            changedOwner["blocker_contributors"][0]["bound_campaign_scene_owned"] = false;
            RequireRejected(changedOwner, grid, "blocker owner classification");

            JObject changedCompletion = (JObject)loadedSourceFit.DeepClone();
            changedCompletion["capture_completeness"]["original_initial_spawn_plan"] = false;
            RequireRejected(changedCompletion, grid, "spawn-plan completeness");
            return true;
        }

        static void RequireRejected(JObject evidence, JObject grid, string label)
        {
            try
            {
                BuildLoadedSourceFitEvidence(evidence, grid);
            }
            catch (InvalidDataException)
            {
                return;
            }
            throw new InvalidDataException("Loaded Campaign source-fit tamper control accepted changed " + label);
        }

        static bool ExactZeroOrigin(JArray origin) => origin != null && origin.Count == 3 &&
            (float?)origin[0] == 0f && (float?)origin[1] == 0f && (float?)origin[2] == 0f;

        static void VerifyArtifact(string path, string expectedSha, string label)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !IsDigest(expectedSha) ||
                !File.Exists(path) || HashFile(path) != expectedSha)
                throw new InvalidDataException(label + " descriptor or SHA-256 mismatch");
        }

        static JObject ReadJson(string path)
        {
            using (var file = File.OpenText(path))
            using (var reader = new JsonTextReader(file) { MaxDepth = 64, DateParseHandling = DateParseHandling.None })
                return JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        static bool IsDigest(string value) => value != null && value.Length == 64 &&
            value.All(character => character >= '0' && character <= '9' || character >= 'a' && character <= 'f');

        static string HashFile(string path) => HashBytes(File.ReadAllBytes(path));

        static string HashBytes(byte[] bytes)
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
