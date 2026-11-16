using System;
using System.IO;
using System.Linq;
using Game.Runtime;
using Newtonsoft.Json.Linq;

internal static class S073NativeSourceBindingControl
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static JObject ActualSource(JObject observation)
    {
        JObject map = (JObject)observation["map"];
        JArray gridOrigin = (JArray)observation["grid"]["origin"];
        return new JObject
        {
            ["source_metadata"] = new JObject
            {
                ["operation_map_id"] = (string)map["operation_map_id"],
                ["source_identity_hash"] = (string)map["source_identity_hash"],
                ["source_content_hash"] = (string)map["source_content_hash"],
                ["generated_metadata_hash"] = (string)map["generated_metadata_hash"]
            },
            ["grid_frame"] = new JObject
            {
                ["width"] = (int)observation["grid"]["width"],
                ["height"] = (int)observation["grid"]["height"],
                ["cell_size"] = (double)observation["grid"]["cell_size"],
                ["origin"] = gridOrigin.DeepClone()
            }
        };
    }

    private static void CopyEvidence(string repo, string sandbox)
    {
        string evidence = Path.Combine(repo, "Logs", "SkirmishProduction", "s073-native-entity-scene-binding-20261101-r2");
        string target = Path.Combine(sandbox, "Logs", "SkirmishProduction", "s073-native-entity-scene-binding-20261101-r2");
        Directory.CreateDirectory(target);
        foreach (string name in new[] { "receipt-strict-r3.json", "observation.json", "editor.log" })
            File.Copy(Path.Combine(evidence, name), Path.Combine(target, name));
    }

    private static bool Rejects(string repo, JObject source)
    {
        try { S073NativeSourceBinding.Join(repo, source); return false; }
        catch (InvalidDataException) { return true; }
    }

    private static void Main(string[] args)
    {
        Need(args.Length == 2, "Repository root and isolated output directory required");
        string repo = Path.GetFullPath(args[0]);
        string output = Path.GetFullPath(args[1]);
        JObject observation = JObject.Parse(File.ReadAllText(Path.Combine(repo, "Logs", "SkirmishProduction",
            "s073-native-entity-scene-binding-20261101-r2", "observation.json")));
        JObject actual = ActualSource(observation);

        JObject joined = S073NativeSourceBinding.Join(repo, actual);
        Need((bool)joined["evidence_qualified"] && (bool)joined["metadata_matches_current_source"] &&
             (bool)joined["grid_frame_matches_current_source"] && (bool)joined["binding_metadata_matches_source"],
             "Qualified observation did not join to matching current source metadata/frame");
        Need((bool?)joined["mission_ready"] == false && (bool?)joined["player_readiness"] == false &&
             (bool?)joined["state_transferred"] == false, "Diagnostic join claimed readiness or transferred state");

        JObject wrongGrid = (JObject)actual.DeepClone();
        wrongGrid["grid_frame"]["width"] = (int)wrongGrid["grid_frame"]["width"] + 1;
        JObject gridResult = S073NativeSourceBinding.Join(repo, wrongGrid);
        Need((bool)gridResult["evidence_qualified"] && !(bool)gridResult["grid_frame_matches_current_source"] &&
             !(bool)gridResult["binding_metadata_matches_source"], "Mismatched current grid frame was accepted");

        JObject wrongMetadata = (JObject)actual.DeepClone();
        wrongMetadata["source_metadata"]["source_identity_hash"] = new string('0', 64);
        JObject metadataResult = S073NativeSourceBinding.Join(repo, wrongMetadata);
        Need(!(bool)metadataResult["metadata_matches_current_source"] &&
             !(bool)metadataResult["binding_metadata_matches_source"], "Mismatched current source metadata was accepted");

        string receiptTamperRoot = Path.Combine(output, "tampered-receipt-root");
        CopyEvidence(repo, receiptTamperRoot);
        string receiptPath = Path.Combine(receiptTamperRoot, "Logs", "SkirmishProduction",
            "s073-native-entity-scene-binding-20261101-r2", "receipt-strict-r3.json");
        File.AppendAllText(receiptPath, " ");
        Need(Rejects(receiptTamperRoot, actual), "Tampered qualified receipt was accepted");

        string sourceTamperRoot = Path.Combine(output, "tampered-source-root");
        CopyEvidence(repo, sourceTamperRoot);
        JObject receipt = JObject.Parse(File.ReadAllText(Path.Combine(repo, "Logs", "SkirmishProduction",
            "s073-native-entity-scene-binding-20261101-r2", "receipt-strict-r3.json")));
        string firstPin = (string)((JProperty)((JObject)receipt["source_pins_sha256"]).First).Name;
        string targetSource = Path.Combine(sourceTamperRoot, firstPin.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(targetSource));
        File.WriteAllText(targetSource, "tampered source pin");
        Need(Rejects(sourceTamperRoot, actual), "Tampered current source pin was accepted");

        Console.WriteLine("[S073NativeSourceBindingControl] result=Passed positive_join=1 wrong_grid=1 wrong_metadata=1 receipt_tamper_rejected=1 source_tamper_rejected=1 diagnostic_only=1");
    }
}
