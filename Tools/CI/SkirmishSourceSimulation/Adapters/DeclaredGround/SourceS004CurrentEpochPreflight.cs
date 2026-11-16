using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// A separate finite data-only preflight. Historical startup is not current-world evidence.
internal static class SourceS004CurrentEpochPreflight
{
    const string BundleSha = "7e02f0fdbdbeacb098e00827ef5994a4551a59940ce3d7f8b76647402fae5cfc";
    const string EvidenceSha = "452076ef52b6582858e599efa503027b1815641844ee956f593db7a660039f45";
    const string BundleRelative = "T Logs/SkirmishProduction/s004-current-epoch-inputs-20261105-T2/bundle.json";
    const string EvidenceRelative = "T Logs/SkirmishProduction/s004-current-epoch-inputs-20261105-T2/input-evidence.json";
    static readonly string[] ReplacementKeys = { "prefab_shared", "prefab_s004", "building_definitions", "configured_building_readmodel", "static_blockers" };
    static void Need(bool value, string message) { if (!value) throw new InvalidDataException(message); }
    static string Hash(string path) { using var stream = File.OpenRead(path); using var sha = SHA256.Create(); return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
    static JObject Read(string path) { using var stream = File.OpenText(path); using var reader = new JsonTextReader(stream) { DateParseHandling = DateParseHandling.None }; var value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }); Need(!reader.Read(), "Trailing JSON"); return value; }
    static void CheckFile(string path, string sha, long? size = null)
    {
        Need(Path.IsPathRooted(path) && File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0, "Regular absolute file required: " + path);
        Need((!size.HasValue || new FileInfo(path).Length == size.Value) && Hash(path) == sha, "File identity mismatch: " + path);
    }
    static string CheckDescriptor(JToken value)
    {
        var item = value as JObject;
        Need(item != null && item.Count == 3 && item["path"]?.Type == JTokenType.String && item["sha256"]?.Type == JTokenType.String && item["bytes"]?.Type == JTokenType.Integer, "Exact file descriptor required");
        string path = (string)item["path"]; CheckFile(path, (string)item["sha256"], (long)item["bytes"]); return path;
    }
    static int CheckPins(string root, JObject pins)
    {
        Need(pins != null && pins.Count > 0, "Current source pins required");
        foreach (var pin in pins.Properties()) {
            Need(pin.Value.Type == JTokenType.String, "Source pin type invalid");
            string path = Path.IsPathRooted(pin.Name) ? pin.Name : Path.GetFullPath(Path.Combine(root, pin.Name));
            CheckFile(path, (string)pin.Value);
        }
        return pins.Count;
    }
    public static int Main(string[] args)
    {
        try {
            Need(args.Length == 4, "usage: repository bundle sha256 fresh-output");
            string root = Path.GetFullPath(args[0]), bundlePath = Path.GetFullPath(args[1]), output = Path.GetFullPath(args[3]);
            Need(bundlePath == Path.Combine(root, BundleRelative) && args[2] == BundleSha, "Exact current bundle identity required");
            CheckFile(bundlePath, BundleSha); var bundle = Read(bundlePath);
            Need((string)bundle["kind"] == "S004SourceDiagnosticInputBundle" && (int?)bundle["schema_version"] == 1 &&
                (string)bundle["mission_id"] == "S004" && (string)bundle["scope"] == "SourceHostDiagnosticOnly", "Bundle identity differs");
            string evidencePath = CheckDescriptor(bundle["evidence"]);
            Need(evidencePath == Path.Combine(root, EvidenceRelative) && (string)bundle["evidence"]?["sha256"] == EvidenceSha, "Exact current evidence required");
            var evidence = Read(evidencePath);
            Need((string)evidence["kind"] == "S004CurrentSourceInputEvidence" &&
                (string)evidence["scope"] == "CurrentCaptures_HistoricalStartup_NoCurrentWorld" &&
                (bool?)evidence["current_world_observed"] == false && (bool?)evidence["runtime_admission"] == false &&
                (bool?)evidence["mission_ready"] == false, "Evidence scope differs");
            var old = Read(CheckDescriptor(evidence["historical_bundle"]));
            var startup = Read(CheckDescriptor(evidence["historical_startup"]));
            string requestsPath = CheckDescriptor(evidence["historical_requests"]);
            Need((string)startup["requests_sha256"] == Hash(requestsPath) && (string)startup["status"] == "ObservedDiagnosticSourcePlacement" &&
                (bool?)startup["runtime_admission"] == false && (bool?)startup["mission_ready"] == false, "Historical startup evidence differs");
            var inputs = bundle["inputs"] as JObject; var oldInputs = old["inputs"] as JObject; var captures = evidence["current_captures"] as JObject;
            Need(inputs != null && oldInputs != null && inputs.Count == 11 && oldInputs.Count == 11 && captures != null && captures.Count == 5, "Input domain differs");
            int pinCount = 0;
            foreach (var row in inputs.Properties()) {
                string path = CheckDescriptor(row.Value);
                if (ReplacementKeys.Contains(row.Name)) {
                    var capture = captures[row.Name] as JObject;
                    Need(capture != null && JToken.DeepEquals(capture["capture"], row.Value), "Capture descriptor differs: " + row.Name);
                    var audit = Read(CheckDescriptor(capture["audit"]));
                    bool wrapperPassed = row.Name == "building_definitions" || row.Name == "configured_building_readmodel"
                        ? (int?)audit["wrapper_exit_observed"] == 0 && (int?)audit["marker_count"] == 1
                        : (int?)audit["wrapper_exit_code"] == 0 &&
                          (row.Name == "prefab_s004" ? (bool?)audit["marker_present"] == true : (bool?)audit["required_marker_present"] == true);
                    Need((bool?)audit["mission_ready"] == false && wrapperPassed, "Capture audit scope differs");
                } else Need(JToken.DeepEquals(oldInputs[row.Name], row.Value), "Retained input differs: " + row.Name);
                if (path.EndsWith(".json", StringComparison.Ordinal)) {
                    var value = Read(path); if (value["source_pins"] is JObject pins && pins.Count > 0) pinCount += CheckPins(root, pins);
                }
            }
            var builds = evidence["source_builds"] as JObject; Need(builds != null && builds.Count == 2, "Two source builds required");
            foreach (var entry in builds.Properties()) {
                var build = Read(CheckDescriptor(entry.Value));
                Need((string)build["status"] == "Passed" && (int?)build["compile_exit"] == 0, "Source build failed");
                pinCount += CheckPins(root, build["source_and_reference_pins"] as JObject);
            }
            foreach (string name in new[] { "role_catalog", "airport_definition", "builder" }) CheckDescriptor(evidence[name]);
            var qualifications = bundle["qualification"] as JObject;
            Need(qualifications != null && qualifications.Count == 6 && JToken.DeepEquals(qualifications, old["qualification"]) &&
                JToken.DeepEquals(bundle["missing_or_assumed"], old["missing_or_assumed"]) &&
                qualifications.Properties().All(p => p.Value.Type == JTokenType.Boolean && !(bool)p.Value), "Readiness and assumptions must remain unchanged");
            CheckFile(bundlePath, BundleSha); CheckFile(evidencePath, EvidenceSha);
            var result = new JObject { ["status"] = "PassedCurrentSourceOnlyBundlePreflight", ["mission"] = "S004",
                ["input_count"] = inputs.Count, ["source_pins_rechecked"] = pinCount, ["historical_startup_is_current"] = false,
                ["bundle_sha256"] = BundleSha, ["world_created"] = false, ["runtime_admission"] = false, ["mission_ready"] = false };
            using (var file = new FileStream(output, FileMode.CreateNew, FileAccess.Write)) using (var writer = new StreamWriter(file)) writer.Write(result.ToString(Formatting.Indented) + "\n");
            Console.WriteLine("[S004CurrentSourceBundlePreflight] result=Passed inputs=11 world=0 admission=0 readiness=0");
            return 0;
        } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
