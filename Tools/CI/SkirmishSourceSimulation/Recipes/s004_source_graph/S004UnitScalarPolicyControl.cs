using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class S004UnitScalarPolicyControl
{
    static bool Flag(JObject row, string name)
    {
        string text = (string)row[name];
        if (text == "0") return false;
        if (text == "1") return true;
        throw new InvalidDataException("Exact serialized boolean required: " + name);
    }
    static float Number(JObject row, string name)
    {
        if (!float.TryParse((string)row[name], System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value))
            throw new InvalidDataException("Finite serialized scalar required: " + name);
        return value;
    }
    static int Main(string[] args)
    {
        try
        {
            var input = JObject.Parse(File.ReadAllText(args[0]));
            if ((string)input["kind"] != "S004SerializedProductionScalarInputs" || (bool?)input["mission_ready"] != false)
                throw new InvalidDataException("Exact source-only scalar input required");
            var output = new JArray();
            foreach (JObject row in (JArray)input["rows"])
            {
                var values = (JObject)row["scalars"]; var size = (JArray)row["footprint"];
                bool air = Flag(values,"isAirUnit"), runway = Flag(values,"productionTransportRequiresAirportRunway"), landing = Flag(values,"productionTransportUsesRunwayLanding");
                int concurrent = checked((int)Number(values,"productionTransportMaxConcurrent"));
                var source = new UnitProductionMetadataScalarSource(true, false, air, air, runway, runway, landing, landing,
                    Number(values,"productionDurationSeconds"), Number(values,"productionTransportArrivalSeconds"),
                    Number(values,"productionTransportHoldForNextReadySeconds"), concurrent, (int)size[0], (int)size[1]);
                var projected = UnitProductionMetadataScalarPolicy.Project(source);
                var definition = (JObject)row["definition_source"];
                int credits = (int)definition["credits_price"], rawMaterials = (int)definition["serialized_materials_cost"];
                int materialCost = rawMaterials > 0 ? rawMaterials : UnitMaterialsCostPolicy.ResolveLegacyMaterialsCost(credits);
                output.Add(new JObject { ["key"] = (string)row["key"], ["is_air_unit"] = projected.IsAirUnit,
                    ["production_duration_seconds"] = projected.ProductionDurationSeconds,
                    ["transport_arrival_seconds"] = projected.ProductionTransportArrivalSeconds,
                    ["transport_hold_seconds"] = projected.ProductionTransportHoldForNextReadySeconds,
                    ["transport_max_concurrent"] = projected.ProductionTransportMaxConcurrent,
                    ["requires_runway"] = projected.ProductionTransportRequiresAirportRunway,
                    ["uses_runway_landing"] = projected.ProductionTransportUsesRunwayLanding,
                    ["footprint"] = new JArray(projected.FootprintWidth, projected.FootprintDepth),
                    ["serialized_transport_null"] = (bool)row["transport_serialized_null"],
                    ["definition_projection"] = new JObject { ["display_name"] = (string)definition["display_name"],
                        ["description"] = (string)definition["description"], ["can_request"] = (bool)definition["can_request"],
                        ["materials_cost"] = materialCost, ["credits_cost"] = credits } });
            }
            if (output.Count != 3) throw new InvalidDataException("Exactly three retained source rows required");
            File.WriteAllText(args[1], new JObject { ["kind"] = "S004OriginalUnitScalarPolicyProjection", ["scope"] = "SourceOnly_NoNativePrefabResolution_NoRuntimeAdmission", ["rows"] = output,
                ["native_parity"] = false, ["runtime_admission"] = false, ["mission_ready"] = false }.ToString(Formatting.Indented));
            Console.WriteLine("[S004UnitScalarPolicyControl] result=Passed rows=3 world=0 native=0 readiness=0");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
