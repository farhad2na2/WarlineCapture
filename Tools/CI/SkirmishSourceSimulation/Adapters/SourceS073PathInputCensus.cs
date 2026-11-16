using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Game.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

// Opt-in read-only comparison against the retained native r3 path observation.
// This records source-world evidence; it never imports or applies native state.
internal static class SourceS073PathInputCensus
{
    const string EnableKey = "WARLINE_S073_SOURCE_PATH_CENSUS";
    const string RawEnableKey = "WARLINE_S073_SOURCE_PATH_RAW_EXPORT";
    const string NativeSnapshot = "Logs/SkirmishProduction/s073-native-path-command-20261101-r3/snapshot.json";
    const string NativeReceipt = "Logs/SkirmishProduction/s073-native-path-command-20261101-r3/receipt.json";

    static string Hash(Action<BinaryWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) write(writer);
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
    }

    const string RawKind = "S073BlockerOccupancyRawSnapshot";
    const string RawMagic = "S073BOC1";

    internal static JObject Capture(EntityManager em, Entity gridEntity, int2 sourceStart, int2 sourceGoal, string sourcePrefab)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(EnableKey), "1", StringComparison.Ordinal)) return null;
        string root = Directory.GetCurrentDirectory();
        string nativePath = Path.GetFullPath(Path.Combine(root, NativeSnapshot));
        string receiptPath = Path.GetFullPath(Path.Combine(root, NativeReceipt));
        if (!File.Exists(nativePath) || !File.Exists(receiptPath))
            throw new InvalidDataException("Opt-in S073 source path census requires retained native r3 snapshot and durable receipt");
        JObject native = JObject.Parse(File.ReadAllText(nativePath));
        JObject receipt = JObject.Parse(File.ReadAllText(receiptPath));
        JObject observed = native["pre_enqueue_input_observation"] as JObject;
        JObject postObserved = native["post_apply_input_observation"] as JObject;
        string snapshotHash = FileSha(nativePath);
        string editorLogPath = Path.Combine(Path.GetDirectoryName(receiptPath), "editor.log");
        if ((string)receipt["status"] != "Recorded" || (int?)receipt["wrapper_exit"] != 0 ||
            (string)receipt["snapshot_sha256"] != snapshotHash || !File.Exists(editorLogPath) ||
            (string)receipt["editor_log_sha256"] != FileSha(editorLogPath) ||
            (string)native["kind"] != "S073NativePathfindingCommandDiagnostic" ||
            (string)native["scope"] != "RetainedOriginalS073World_ExplicitSyntheticCommand_NotNormalInputOrReadiness" ||
            (int?)native["schema_version"] != 1 || (int?)native["seed"] != 104729 || (int?)native["scenario_index"] != 3 ||
            (string)native["source_map_id"] != "opmap.skirmish.industrial_basin" ||
            (string)native["source_campaign_map_id"] != "opmap.skirmish.desert_base_01" ||
            (bool?)receipt["mission_ready"] != false || (bool?)native["normal_input_playthrough"] != false ||
            (bool?)native["player_readiness"] != false || observed == null || postObserved == null ||
            (string)native["session_id"] != (string)receipt["session_id"] ||
            (string)observed["session_id"] != (string)receipt["session_id"] ||
            (string)postObserved["session_id"] != (string)receipt["session_id"] ||
            (int?)receipt["source_pin_count"] != 14)
            throw new InvalidDataException("Retained native S073 path snapshot does not match its durable recorded receipt and identity");
        ValidatePins(root, native["session_start_dependency_pins_sha256"] as JObject);
        ValidatePins(root, native["session_finish_dependency_pins_sha256"] as JObject);

        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBlockerComponent blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        var walkable = em.GetBuffer<GridWalkable>(gridEntity, true);
        int cells = checked(grid.Width * grid.Height);
        if (walkable.Length != cells || blockers.GridSize != cells || blockers.Counts.Length != cells ||
            blockers.Blocked.Length != cells || occupancy.GridSize != cells || occupancy.Occupied.Length != cells)
            throw new InvalidDataException("Source S073 path census requires dimensionally complete source grid inputs");

        string walkHash = Hash(w => {
            w.Write(grid.Width); w.Write(grid.Height); w.Write(grid.CellSize);
            w.Write(grid.Origin.x); w.Write(grid.Origin.y); w.Write(grid.Origin.z); w.Write(walkable.Length);
            for (int i = 0; i < walkable.Length; i++) w.Write(walkable[i].Value);
        });
        string blockerHash = Hash(w => {
            w.Write(blockers.GridSize); w.Write(blockers.StorageEpoch);
            w.Write(blockers.AppliedStorageEpoch); w.Write(blockers.AppliedRevision);
            for (int i = 0; i < blockers.GridSize; i++) {
                w.Write(blockers.Blocked.IsSet(i)); w.Write(blockers.Counts[i]);
                w.Write(blockers.FriendlyPassFactionIds[i]);
            }
        });
        string occupancyHash = Hash(w => {
            w.Write(occupancy.GridSize);
            for (int i = 0; i < occupancy.GridSize; i++) w.Write(occupancy.Occupied.IsSet(i));
        });

        int2 observedStart = new int2((int)observed["unit_start_cell_observed"][0], (int)observed["unit_start_cell_observed"][1]);
        int2 observedGoal = new int2((int)observed["request_goal"][0], (int)observed["request_goal"][1]);
        string observedPrefab = (string)observed["unit_source_prefab_key"];
        var nearby = new JArray();
        using (var units = em.CreateEntityQuery(ComponentType.ReadOnly<UnitGrid>(), ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitSourcePrefabKey>()))
        using (var entities = units.ToEntityArray(Unity.Collections.Allocator.Temp))
            foreach (Entity entity in entities) {
                UnitGrid cell = em.GetComponentData<UnitGrid>(entity);
                if (em.GetComponentData<Faction>(entity).Id != FactionIdentity.PlayerFactionId ||
                    math.abs(cell.Cell.x - observedStart.x) > 8 || math.abs(cell.Cell.y - observedStart.y) > 8) continue;
                nearby.Add(new JObject { ["entity"] = new JArray(entity.Index, entity.Version),
                    ["cell"] = new JArray(cell.Cell.x, cell.Cell.y),
                    ["source_prefab_key"] = em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString() });
            }
        return new JObject {
            ["kind"] = "S073SourceWorldPathInputCensus", ["native_reference_path"] = NativeSnapshot,
            ["native_reference_receipt_path"] = NativeReceipt, ["native_reference_receipt_status"] = (string)receipt["status"],
            ["native_reference_wrapper_exit"] = (int)receipt["wrapper_exit"],
            ["native_reference_sha256"] = snapshotHash, ["native_session_id"] = (string)receipt["session_id"],
            ["native_map_id"] = (string)native["source_map_id"], ["campaign_source_map_id"] = (string)native["source_campaign_map_id"],
            ["native_seed"] = (int)native["seed"], ["native_observation"] = "pre_enqueue_input_observation",
            ["grid_width"] = grid.Width, ["grid_height"] = grid.Height, ["grid_cell_size"] = grid.CellSize,
            ["grid_origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z),
            ["grid_walkable_fingerprint_sha256"] = walkHash, ["blocker_fingerprint_sha256"] = blockerHash,
            ["occupancy_fingerprint_sha256"] = occupancyHash,
            ["walkable_fingerprint_matches_native"] = walkHash == (string)observed["grid_walkable_fingerprint_sha256"],
            ["blocker_fingerprint_matches_native"] = blockerHash == (string)observed["blocker_fingerprint_sha256"],
            ["occupancy_fingerprint_matches_native"] = occupancyHash == (string)observed["occupancy_fingerprint_sha256"],
            ["native_start"] = new JArray(observedStart.x, observedStart.y), ["source_start"] = new JArray(sourceStart.x, sourceStart.y),
            ["native_goal"] = new JArray(observedGoal.x, observedGoal.y), ["source_goal"] = new JArray(sourceGoal.x, sourceGoal.y),
            ["native_source_prefab_key"] = observedPrefab, ["source_source_prefab_key"] = sourcePrefab,
            ["spawned_player_units_within_8_cells_of_native_start"] = nearby,
            ["start_goal_prefab_exact_match"] = observedStart.Equals(sourceStart) && observedGoal.Equals(sourceGoal) && observedPrefab == sourcePrefab,
            ["native_campaign_state_transferred"] = false, ["admission_changed"] = false,
            ["simulation_active_changed"] = false, ["parity_claim"] = false,
            ["scope"] = "Source world input census and exact captured-reference comparison only; fingerprints may differ because worlds and observation times differ"
        };
    }

    internal static JObject CaptureRaw(EntityManager em, Entity gridEntity, string outputDirectory, JObject run)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RawEnableKey), "1", StringComparison.Ordinal)) return null;
        if (run == null || (string)run["kind"] != "SkirmishSourceSimulationRun" || (int?)run["schema_version"] != 1 ||
            (string)run["mission"] != "S073" || (int?)run["seed"] != 104729)
            throw new InvalidDataException("Independent raw export requires exact S073 source run request");
        string Descriptor(string key)
        {
            var value = run[key] as JObject ?? throw new InvalidDataException("S073 run build provenance missing: " + key);
            string path = Path.GetFullPath((string)value["path"]); string expected = (string)value["sha256"];
            if (!File.Exists(path) || FileSha(path) != expected) throw new InvalidDataException("S073 run input provenance mismatch: " + key);
            return path;
        }
        string buildReceiptPath = Descriptor("source_build_receipt");
        string executablePath = Descriptor("source_executable");
        string requestPath = Path.Combine(Path.GetFullPath(outputDirectory), "run-request.json");
        if (!File.Exists(requestPath) || FileSha(requestPath) == "") throw new InvalidDataException("Source run request required");
        string rawPath = Path.Combine(Path.GetFullPath(outputDirectory), "source-blocker-occupancy-r1.bin");
        string receiptPath = rawPath + ".json";
        if (File.Exists(rawPath) || File.Exists(receiptPath)) throw new InvalidDataException("Fresh source raw artifact paths required");

        GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
        DynamicBlockerComponent blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancy = em.GetComponentData<DynamicOccupancyComponent>(gridEntity);
        int cells = checked(grid.Width * grid.Height);
        if (cells <= 0 || cells > 2_097_152 || blockers.WriteInProgress != 0 ||
            blockers.GridSize != cells || blockers.Counts.Length != cells || blockers.Blocked.Length != cells ||
            blockers.FriendlyPassFactionIds.Length != cells || occupancy.GridSize != cells || occupancy.Occupied.Length != cells ||
            blockers.AppliedStorageEpoch != blockers.StorageEpoch)
            throw new InvalidDataException("Committed dimensionally complete source blocker/occupancy arrays required");

        int bitBytes = checked((cells + 7) / 8);
        using (var stream = new FileStream(rawPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
        {
            writer.Write(Encoding.ASCII.GetBytes(RawMagic));
            writer.Write((uint)grid.Width); writer.Write((uint)grid.Height);
            writer.Write(grid.CellSize); writer.Write(grid.Origin.x); writer.Write(grid.Origin.y); writer.Write(grid.Origin.z);
            writer.Write((ulong)blockers.StorageEpoch); writer.Write((ulong)blockers.AppliedStorageEpoch);
            writer.Write((ulong)blockers.AppliedRevision); writer.Write((uint)cells);
            var bits = new byte[bitBytes];
            for (int i = 0; i < cells; i++)
            {
                int count = blockers.Counts[i];
                if (count < 0 || blockers.Blocked.IsSet(i) != (count > 0)) throw new InvalidDataException("Source blocker storage is inconsistent");
                if (blockers.Blocked.IsSet(i)) bits[i >> 3] |= (byte)(1 << (i & 7));
            }
            writer.Write(bits);
            for (int i = 0; i < cells; i++) writer.Write(blockers.Counts[i]);
            for (int i = 0; i < cells; i++) writer.Write(blockers.FriendlyPassFactionIds[i]);
            Array.Clear(bits, 0, bits.Length);
            for (int i = 0; i < cells; i++) if (occupancy.Occupied.IsSet(i)) bits[i >> 3] |= (byte)(1 << (i & 7));
            writer.Write(bits);
        }
        string rawHash = FileSha(rawPath);
        using var sessions = em.CreateEntityQuery(ComponentType.ReadOnly<SkirmishMatchState>());
        using var maps = em.CreateEntityQuery(ComponentType.ReadOnly<ActiveOperationMapComponent>(), ComponentType.ReadOnly<OperationMapMetadataComponent>());
        if (sessions.CalculateEntityCount() != 1 || maps.CalculateEntityCount() != 1)
            throw new InvalidDataException("Unique source S073 session and map identity required");
        var session = em.GetComponentData<SkirmishMatchState>(sessions.GetSingletonEntity());
        var active = em.GetComponentData<ActiveOperationMapComponent>(maps.GetSingletonEntity());
        var metadata = em.GetComponentData<OperationMapMetadataComponent>(maps.GetSingletonEntity());
        if (session.Phase != SkirmishPhase.Preparing || session.Seed != 104729 || metadata.PhysicalSourceValidated != 0)
            throw new InvalidDataException("Source raw export requires retained Preparing state with physical validation false");
        string requestHash = FileSha(requestPath);
        var rawReceipt = new JObject {
            ["kind"] = RawKind, ["schema_version"] = 1, ["status"] = "Captured", ["producer"] = "source",
            ["observation"] = "source_host_pre_command", ["raw_path"] = rawPath, ["raw_sha256"] = rawHash,
            ["byte_length"] = new FileInfo(rawPath).Length, ["width"] = grid.Width, ["height"] = grid.Height,
            ["cell_size"] = grid.CellSize, ["origin"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z),
            ["storage_epoch"] = blockers.StorageEpoch.ToString(), ["applied_storage_epoch"] = blockers.AppliedStorageEpoch.ToString(),
            ["applied_revision"] = blockers.AppliedRevision.ToString(), ["mission"] = "S073", ["seed"] = 104729,
            ["source_session_id"] = session.SessionId.ToString(), ["source_map_id"] = active.OperationMapId.ToString(),
            ["source_campaign_map_id"] = metadata.Blob.Value.SourceOperationMapId.ToString(),
            ["source_build_receipt"] = new JObject { ["path"] = buildReceiptPath, ["sha256"] = (string)run["source_build_receipt"]["sha256"] },
            ["source_executable"] = new JObject { ["path"] = executablePath, ["sha256"] = (string)run["source_executable"]["sha256"] },
            ["run_request"] = new JObject { ["path"] = requestPath, ["sha256"] = requestHash },
            ["bundle"] = run["s073_bundle"].DeepClone(), ["source_graph"] = run["selected_operation_map_graph"].DeepClone(),
            ["source_identity_hash"] = metadata.Blob.Value.SourceIdentityHash.ToString(),
            ["source_content_hash"] = metadata.Blob.Value.SourceContentHash.ToString(),
            ["source_campaign_state_transferred"] = false, ["scheduler_admitted"] = false, ["mission_ready"] = false,
            ["scope"] = "IndependentSourceHostRawBlockerOccupancyCapture_NoNativeStateImport_NoAdmission"
        };
        File.WriteAllText(receiptPath, rawReceipt.ToString(Newtonsoft.Json.Formatting.Indented) + "\n");
        return new JObject { ["path"] = rawPath, ["sha256"] = rawHash, ["receipt_path"] = receiptPath,
            ["receipt_sha256"] = FileSha(receiptPath), ["mission_ready"] = false, ["scheduler_admitted"] = false };
    }

    static string FileSha(string path)
    {
        using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    static void ValidatePins(string root, JObject pins)
    {
        if (pins == null || pins.Count != 14) throw new InvalidDataException("Exact retained native S073 path observer dependency pins required");
        foreach (var pin in pins.Properties()) {
            string path = Path.GetFullPath(Path.Combine(root, pin.Name));
            if (!File.Exists(path) || FileSha(path) != (string)pin.Value)
                throw new InvalidDataException("Retained native S073 path observer pin differs from current source: " + pin.Name);
        }
    }
}
