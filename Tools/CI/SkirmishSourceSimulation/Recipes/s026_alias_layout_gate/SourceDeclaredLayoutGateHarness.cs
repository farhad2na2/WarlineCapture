using System;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;

internal static class SourceDeclaredLayoutGateHarness
{
    private static void Need(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private static JObject Target(JObject projection)
    {
        var source = (JObject)projection["target"];
        return new JObject
        {
            ["operation_map_id"] = (string)source["operation_map_id"],
            ["source_content_hash"] = (string)source["source_content_hash"],
            ["layout_fingerprint"] = (string)source["layout_fingerprint"],
            ["size"] = (string)source["size"],
            ["difficulty"] = (string)source["difficulty"]
        };
    }

    private static bool Rejected(SkirmishMapLayoutConfig layout, JObject context, OperationMapDefinition source)
    {
        try { SourceDeclaredMissionConfiguration.RequireLayoutSelection(layout, context, source); return false; }
        catch (InvalidDataException) { return true; }
    }

    private static void Main(string[] args)
    {
        Need(args.Length == 6, "Expected alias and direct-source graph/layout/projection paths");
        var graph = JObject.Parse(File.ReadAllText(args[0]));
        var layoutJson = JObject.Parse(File.ReadAllText(args[1]));
        var projection = JObject.Parse(File.ReadAllText(args[2]));
        var materializer = new SourceConfigMaterializer(graph);
        var sourcePath = (string)projection["typed_configuration"]["campaign_source_path"];
        var alias = materializer.ByPath<OperationMapDefinition>(sourcePath);
        var layout = materializer.LogicalLayout(layoutJson);
        var target = Target(projection);
        var context = new JObject { ["target_context"] = target };

        // A valid single declared alias is accepted as a selection gate only. The false
        // return means no measured all-configuration layout was selected.
        Need(!SourceDeclaredMissionConfiguration.RequireLayoutSelection(layout, context, alias),
            "Valid single declared alias unexpectedly selected measured layout");

        var cases = new JArray();
        cases.Add(new JObject { ["case"] = "valid_alias", ["accepted"] = true, ["measured_layout_selected"] = false });

        var wrongSourceHash = (JObject)target.DeepClone();
        wrongSourceHash["source_content_hash"] = "0000000000000000000000000000000000000000000000000000000000000000";
        Need(Rejected(layout, new JObject { ["target_context"] = wrongSourceHash }, alias), "Wrong source content hash was accepted");
        cases.Add(new JObject { ["case"] = "wrong_source_hash", ["rejected"] = true });

        var wrongTargetId = (JObject)target.DeepClone();
        wrongTargetId["operation_map_id"] = "opmap.invalid.target";
        Need(Rejected(layout, new JObject { ["target_context"] = wrongTargetId }, alias), "Wrong target operation map ID was accepted");
        cases.Add(new JObject { ["case"] = "wrong_target_id", ["rejected"] = true });

        var wrongTargetHash = (JObject)target.DeepClone();
        wrongTargetHash["layout_fingerprint"] = "0000000000000000000000000000000000000000000000000000000000000000";
        Need(Rejected(layout, new JObject { ["target_context"] = wrongTargetHash }, alias), "Wrong target layout hash was accepted");
        cases.Add(new JObject { ["case"] = "wrong_target_hash", ["rejected"] = true });

        Need(Rejected(layout, context, null), "Null source definition was accepted");
        cases.Add(new JObject { ["case"] = "null_source", ["rejected"] = true });

        // The aliased CityCrossroads definition resolves to the Desert Base physical source.
        // Supplying that physical source directly must fail the alias target identity check.
        var physical = materializer.ByPath<OperationMapDefinition>("Assets/Game/Configs/OperationMaps/OperationMap_Compatibility_DesertBase01.asset");
        Need(Rejected(layout, context, physical), "Direct physical-source regression unexpectedly passed as alias");
        cases.Add(new JObject { ["case"] = "direct_source_regression", ["rejected"] = true });

        var directGraph = JObject.Parse(File.ReadAllText(args[3]));
        var directLayoutJson = JObject.Parse(File.ReadAllText(args[4]));
        var directProjection = JObject.Parse(File.ReadAllText(args[5]));
        Need((string)directProjection["target"]["mission_id"] == "S050" &&
             (string)directProjection["target"]["operation_map_id"] == "opmap.ch05.citywide_alert",
             "Exact S050 direct-source projection required");
        var directMaterializer = new SourceConfigMaterializer(directGraph);
        var directSourcePath = (string)directProjection["typed_configuration"]["campaign_source_path"];
        var directSource = directMaterializer.ByPath<OperationMapDefinition>(directSourcePath);
        var directLayout = directMaterializer.LogicalLayout(directLayoutJson);
        var directTarget = Target(directProjection);
        Need(directSource.OperationMapId == directTarget["operation_map_id"].Value<string>() &&
             directSource.SourceBinding.IsConfigured == false,
             "Direct source must be the definition's unbound authored source");
        Need(!SourceDeclaredMissionConfiguration.RequireLayoutSelection(directLayout,
            new JObject { ["target_context"] = directTarget }, directSource),
            "Valid direct-source single declared layout unexpectedly selected measured layout");
        cases.Add(new JObject { ["case"] = "direct_source_positive", ["accepted"] = true,
            ["measured_layout_selected"] = false, ["source_path"] = directSourcePath });

        Console.WriteLine(new JObject
        {
            ["kind"] = "SourceDeclaredLayoutGateHarness",
            ["status"] = "Passed",
            ["source_path"] = sourcePath,
            ["layout_operation_map_id"] = layout.OperationMapId,
            ["results"] = cases,
            ["source_fit_certified"] = false,
            ["mission_ready"] = false
        }.ToString(Newtonsoft.Json.Formatting.None));
    }
}
