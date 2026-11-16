using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    // An explicitly historical, source-only diagnostic. No live scene or
    // current native blocker parity is inferred from this isolated capture.
    internal sealed class SourceHistoricalStaticBlockers
    {
        private sealed class Row { internal int2 Cell, Size, Origin; internal bool Building; }
        private readonly List<Row> rows;
        private readonly Dictionary<string, string> currentPins;
        private SourceHistoricalStaticBlockers(List<Row> rows, Dictionary<string, string> pins)
        { this.rows = rows; currentPins = pins; }

        private static JObject Read(JToken token, int maximum)
        {
            var descriptor = token as JObject;
            if (descriptor == null || descriptor.Count != 2 || descriptor["path"]?.Type != JTokenType.String ||
                descriptor["sha256"]?.Type != JTokenType.String) throw new InvalidDataException("Exact historical descriptor required");
            var raw = AutonomousWorld.PinnedFile((string)descriptor["path"], (string)descriptor["sha256"], maximum);
            return JObject.Parse(new UTF8Encoding(false, true).GetString(raw),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }
        private static int2 Pair(JToken token)
        {
            var pair = token as JArray;
            if (pair == null || pair.Count != 2 || pair.Any(v => v.Type != JTokenType.Integer))
                throw new InvalidDataException("Exact integer blocker pair required");
            return new int2((int)pair[0], (int)pair[1]);
        }
        private static string FileHash(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length > 128L * 1024 * 1024)
                throw new InvalidDataException("Bounded regular historical source pin required");
            using var input = File.OpenRead(path);
            using var hash = System.Security.Cryptography.SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
        }

        internal static SourceHistoricalStaticBlockers Load(JToken token, string root, OperationMapDefinition map)
        {
            var spec = token as JObject;
            if (spec == null || spec.Count != 5 || (string)spec["kind"] != "HistoricalIsolatedStaticBlockers" ||
                spec["pin_drift"] is not JArray declaredDrift || map == null || !Path.IsPathRooted(root))
                throw new InvalidDataException("Exact explicitly historical blocker descriptor required");
            var capture = Read(spec["capture"], 4 * 1024 * 1024);
            var receipt = Read(spec["receipt"], 1024 * 1024);
            var log = spec["log"] as JObject;
            if (log == null || log.Count != 2 || log["path"]?.Type != JTokenType.String || log["sha256"]?.Type != JTokenType.String)
                throw new InvalidDataException("Exact historical capture log required");
            string logText = new UTF8Encoding(false, true).GetString(AutonomousWorld.PinnedFile((string)log["path"], (string)log["sha256"], 16 * 1024 * 1024));
            int expected = map.NavigationMetadata.StaticGridBlockerCount;
            if ((string)capture["kind"] != "S004IsolatedStaticBlockerSourceCapture" || (string)capture["status"] != "CollectedSourceOnly" ||
                (string)capture["scene_guid"] != map.NavigationMetadata.AuthoredSubSceneGuid ||
                (bool?)capture["gameplay_state_copied"] != false || (bool?)capture["live_session_observed"] != false ||
                (bool?)capture["physical_source_validated"] != false || (bool?)capture["source_fit_certified"] != false ||
                (bool?)capture["mission_ready"] != false || (string)receipt["status"] != "PassedIsolatedBakedBlockerCapture" ||
                (int?)receipt["wrapper_exit_code"] != 0 || (bool?)receipt["required_marker_present"] != true ||
                (string)receipt["capture_sha256"] != (string)spec["capture"]?["sha256"] ||
                (string)receipt["log_sha256"] != (string)log["sha256"] || (int?)receipt["blocker_count"] != expected ||
                !logText.Contains("[S004StaticBlockerSourceCapture] result=Collected blockers=" + expected + " "))
                throw new InvalidDataException("Historical blocker capture, map or wrapper receipt mismatch");
            var recorded = capture["source_pins"] as JObject ?? throw new InvalidDataException("Historical source pins required");
            if ((int?)receipt["source_pins_checked"] != recorded.Count || recorded.Count < 1 || recorded.Count > 20000)
                throw new InvalidDataException("Complete bounded historical source pin set required");
            var pins = new Dictionary<string, string>(StringComparer.Ordinal); var drift = new JArray();
            foreach (var pin in recorded.Properties().OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                string path = Path.GetFullPath(Path.IsPathRooted(pin.Name) ? pin.Name : Path.Combine(root, pin.Name));
                string current = FileHash(path), captured = (string)pin.Value;
                if (!pins.TryAdd(path, current)) throw new InvalidDataException("Duplicate historical source pin path");
                if (current != captured) drift.Add(new JObject { ["path"] = pin.Name, ["captured_sha256"] = captured, ["current_sha256"] = current });
            }
            if (!JToken.DeepEquals(drift, declaredDrift)) throw new InvalidDataException("Historical source drift must be declared exactly");
            foreach (string key in new[] { "capture", "receipt", "log" })
            {
                var descriptor = (JObject)spec[key];
                string path = Path.GetFullPath((string)descriptor["path"]), digest = (string)descriptor["sha256"];
                if (pins.TryGetValue(path, out string prior) && prior != digest) throw new InvalidDataException("Conflicting historical evidence pin");
                pins[path] = digest;
            }
            var inputRows = capture["blockers"] as JArray;
            if (inputRows == null || inputRows.Count != expected || (int?)capture["blocker_count"] != expected || expected < 1 || expected > 16384)
                throw new InvalidDataException("Complete bounded historical blocker rows required");
            var result = new List<Row>(); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var tokenRow in inputRows)
            {
                var row = tokenRow as JObject;
                if (row == null || (bool?)row["source_section_owned"] != true || (bool?)row["friendly_pass_present"] != false ||
                    (bool?)row["opening_present"] != false || row["building"]?.Type != JTokenType.Boolean ||
                    String.IsNullOrEmpty((string)row["source_entity"]) || !ids.Add((string)row["source_entity"]))
                    throw new InvalidDataException("Unique supported historical blocker row required");
                var cell = Pair(row["unit_grid"]); var size = Pair(row["size"]); var origin = Pair(row["origin"]);
                if (cell.x < 0 || cell.y < 0 || size.x < 1 || size.y < 1 ||
                    (long)cell.x + size.x > map.GridMetadata.Dimensions.x || (long)cell.y + size.y > map.GridMetadata.Dimensions.y)
                    throw new InvalidDataException("Historical blocker lies outside selected grid");
                result.Add(new Row { Cell = cell, Size = size, Origin = origin, Building = (bool)row["building"] });
            }
            return new SourceHistoricalStaticBlockers(result, pins);
        }
        internal int Materialize(EntityManager em)
        {
            foreach (var row in rows)
            {
                var entity = em.CreateEntity();
                em.AddComponentData(entity, new UnitGrid { Cell = row.Cell });
                em.AddComponentData(entity, new GridBlockerSize { Size = row.Size });
                em.AddComponentData(entity, new StaticGridBlocker());
                if (row.Building) { em.AddComponentData(entity, new OperationMapBuildingComponent()); em.AddComponentData(entity, new RuntimeBuildingCombatInfo { OriginCell = row.Origin }); }
            }
            return rows.Count;
        }
        internal void VerifyInputs() { foreach (var pin in currentPins) if (FileHash(pin.Key) != pin.Value) throw new InvalidDataException("Historical blocker source changed during run: " + pin.Key); }
    }
}
