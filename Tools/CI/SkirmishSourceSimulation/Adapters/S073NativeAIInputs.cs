using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Explicit inputs observed in a separate instrumented native startup. This
    // neither transfers native entities nor grants source or player readiness.
    internal sealed class S073NativeAIInputs
    {
        internal AISettingsSnapshot Settings;
        internal readonly List<InitialFactionSpawnCellFallbackEntry> Fallbacks = new();
        internal readonly Dictionary<byte, int2> ObservedPostStartupCells = new();
        internal JObject Snapshot;
        private static string Hash(byte[] bytes) { using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        private static float FloatBits(JToken token)
        {
            uint bits = Convert.ToUInt32((string)token["bits_hex"], 16);
            float value = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
            if (float.IsNaN(value) || float.IsInfinity(value) || value != (float)token["value"]) throw new InvalidDataException("Exact finite native AI float required");
            return value;
        }
        private static T EnumValue<T>(JToken value) where T : struct
        {
            int raw = (int)value;
            object candidate = Enum.ToObject(typeof(T), raw);
            if (!Enum.IsDefined(typeof(T), candidate)) throw new InvalidDataException("Unknown native AI setting: " + typeof(T).Name);
            return (T)candidate;
        }
        internal static S073NativeAIInputs Load(string repositoryRoot, string receiptPath, string receiptSha)
        {
            byte[] receiptBytes = File.ReadAllBytes(receiptPath);
            if (Hash(receiptBytes) != receiptSha) throw new InvalidDataException("Native AI receipt changed");
            var receipt = JObject.Parse(System.Text.Encoding.UTF8.GetString(receiptBytes));
            if ((string)receipt["kind"] != "S073NativeAIStartupSnapshotReceipt" || (int?)receipt["schema_version"] != 1 ||
                (string)receipt["validation_status"] != "Passed" || (int?)receipt["wrapper_exit_code"] != 0 ||
                (bool?)receipt["required_marker_present"] != true || (bool?)receipt["normal_input_playthrough"] != false || (bool?)receipt["mission_ready"] != false ||
                (string)receipt["scope"] != "Instrumented same-session startup observation, not player or device acceptance")
                throw new InvalidDataException("Qualified instrumented S073 AI receipt required");
            string directory = Path.GetDirectoryName(Path.GetFullPath(receiptPath));
            byte[] snapshotBytes = File.ReadAllBytes(Path.Combine(directory, "snapshot.json")), log = File.ReadAllBytes(Path.Combine(directory, "editor.log"));
            if (Hash(snapshotBytes) != (string)receipt["snapshot_sha256"] || Hash(log) != (string)receipt["editor_log_sha256"] ||
                !System.Text.Encoding.UTF8.GetString(log).Contains("[S073NativeAIStartupSnapshot] result=Passed scope=instrumented_startup "))
                throw new InvalidDataException("Native AI snapshot or log changed");
            var snapshot = JObject.Parse(System.Text.Encoding.UTF8.GetString(snapshotBytes));
            if ((string)snapshot["kind"] != "S073NativeAIStartupSnapshot" || (int?)snapshot["schema_version"] != 1 ||
                (string)snapshot["scope"] != "OriginalS073InstrumentedStartupSnapshot_NotNormalInputOrReadiness" ||
                (int?)snapshot["scenario_index"] != 3 || (int?)snapshot["seed"] != 104729 ||
                (string)snapshot["phase"] != "Playing" || (string)snapshot["session_id"] != (string)receipt["session_id"] ||
                (string)snapshot["map_id"] != "opmap.skirmish.industrial_basin" || (string)snapshot["source_map_id"] != "opmap.skirmish.desert_base_01" ||
                (string)snapshot["source_identity_hash"] != "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900" ||
                (bool?)snapshot["gameplay_start_complete"] != true || (bool?)snapshot["gameplay_start_failed"] != false ||
                (bool?)snapshot["normal_input_playthrough"] != false || (bool?)snapshot["player_readiness"] != false)
                throw new InvalidDataException("Exact instrumented S073 AI observation required");
            string root = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var pins = snapshot["source_pins_sha256"] as JObject;
            if (pins == null || pins.Count != (int?)receipt["source_pin_count"] || pins.Count < 20) throw new InvalidDataException("Complete native AI source pins required");
            foreach (var pin in pins.Properties())
            {
                string path = Path.GetFullPath(Path.Combine(root, pin.Name));
                if (Path.IsPathRooted(pin.Name) || !path.StartsWith(root, StringComparison.Ordinal) || Hash(File.ReadAllBytes(path)) != (string)pin.Value)
                    throw new InvalidDataException("Native AI source changed: " + pin.Name);
            }
            var value = snapshot["pending_ai_settings_snapshot"] as JObject ?? throw new InvalidDataException("Observed AI settings required");
            var result = new S073NativeAIInputs { Snapshot = snapshot, Settings = new AISettingsSnapshot {
                Difficulty = EnumValue<AIDifficultySetting>(value["Difficulty"]), StartingMoney = EnumValue<AIStartingMoneySetting>(value["StartingMoney"]),
                IncomeMultiplier = FloatBits(value["IncomeMultiplier"]), BuildSpeed = EnumValue<AISpeedSetting>(value["BuildSpeed"]),
                UnitProductionSpeed = EnumValue<AISpeedSetting>(value["UnitProductionSpeed"]), AttackGroupSize = EnumValue<AIAttackGroupSizeSetting>(value["AttackGroupSize"]),
                AttackFrequency = EnumValue<AIAttackFrequencySetting>(value["AttackFrequency"]), Aggression = EnumValue<AIAggressionSetting>(value["Aggression"]),
                Expansion = EnumValue<AIExpansionSetting>(value["Expansion"]), TargetPriority = EnumValue<AITargetPriority>(value["TargetPriority"]),
                PlayerAutoAIEnabled = (bool)value["PlayerAutoAIEnabled"], EnemyAICount = (int)value["EnemyAICount"] } };
            foreach (JObject row in (JArray)snapshot["initial_faction_spawn_cell_fallback_entries"])
            {
                if (row["FactionId"] == null || row["SpawnCell"]?["x"] == null || row["SpawnCell"]?["y"] == null)
                    throw new InvalidDataException("Complete native fallback spawn cell required");
                var cell = row["SpawnCell"]; result.Fallbacks.Add(new InitialFactionSpawnCellFallbackEntry((byte)row["FactionId"], new int2((int)cell["x"], (int)cell["y"])));
            }
            foreach (JObject row in (JArray)snapshot["post_startup_spawn_resolver_observations"])
            {
                if ((bool?)row["resolved"] != true || (string)row["scope"] != "PostStartupOriginalResolverObservation" ||
                    row["faction_id"] == null || !(row["cell"] is JArray observedCell) || observedCell.Count != 2)
                    throw new InvalidDataException("Observed resolver values required");
                result.ObservedPostStartupCells.Add((byte)row["faction_id"], new int2((int)observedCell[0], (int)observedCell[1]));
            }
            if (result.Fallbacks.Count == 0 || result.ObservedPostStartupCells.Count == 0 || Hash(File.ReadAllBytes(receiptPath)) != receiptSha ||
                Hash(File.ReadAllBytes(Path.Combine(directory, "snapshot.json"))) != (string)receipt["snapshot_sha256"])
                throw new InvalidDataException("Native AI inputs incomplete or changed");
            return result;
        }

        internal void RequireObservedCells(AIStartupSystem.TryResolveFactionSpawnCell resolver)
        {
            foreach (var pair in ObservedPostStartupCells)
                if (!resolver(pair.Key, out int2 cell) || cell.x != pair.Value.x || cell.y != pair.Value.y)
                    throw new InvalidDataException("Original source spawn resolver differs from post-start native observation for faction " + pair.Key);
        }
    }
}
