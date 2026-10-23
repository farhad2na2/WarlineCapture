using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Runtime;
using Game.Skirmish.Contracts;

// This is an objective fact-trace harness, not a fact producer or combat engine.
static class Program
{
    static readonly JsonSerializerOptions Json = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    delegate string[] Step<T, F>(ref T state, in F facts);
    delegate bool Surrender<T>(ref T state, bool playing);
    struct BaseState
    {
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind Reason;
        public bool Terminal => Outcome != SkirmishOutcomeKind.None;
    }

    static void Need(bool condition, string message)
    { if (!condition) throw new ArgumentException(message); }

    static void Shape(JsonElement value, string[] required, params string[] optional)
    {
        Need(value.ValueKind == JsonValueKind.Object, "Expected an object");
        var names = value.EnumerateObject().Select(p => p.Name).ToHashSet();
        Need(required.All(names.Contains), "Missing fields: " + string.Join(",", required.Except(names)));
        Need(!names.Except(required.Concat(optional)).Any(), "Unknown fields: " + string.Join(",", names.Except(required.Concat(optional))));
    }

    static void UniqueKeys(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in value.EnumerateObject())
            { Need(seen.Add(item.Name), "Duplicate JSON key: " + item.Name); UniqueKeys(item.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) UniqueKeys(item);
    }

    static void FactsShape(JsonElement value, Type type)
    {
        if (type.IsArray)
        {
            Need(value.ValueKind == JsonValueKind.Array, "Expected facts array: " + type.Name);
            foreach (var element in value.EnumerateArray()) FactsShape(element, type.GetElementType());
        }
        else if (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(decimal))
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            Shape(value, fields.Select(f => f.Name).ToArray());
            foreach (var field in fields) FactsShape(value.GetProperty(field.Name), field.FieldType);
        }
    }

    static void Expect(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind == JsonValueKind.Object)
        {
            Need(actual.ValueKind == JsonValueKind.Object, path + ": expected object");
            foreach (var field in expected.EnumerateObject())
            {
                Need(actual.TryGetProperty(field.Name, out var found), path + ": absent " + field.Name);
                Expect(field.Value, found, path + "." + field.Name);
            }
        }
        else if (expected.ValueKind == JsonValueKind.Array)
        {
            Need(actual.ValueKind == JsonValueKind.Array && expected.GetArrayLength() == actual.GetArrayLength(), path + ": array length differs");
            for (int i = 0; i < expected.GetArrayLength(); i++) Expect(expected[i], actual[i], path + "[" + i + "]");
        }
        else Need(expected.ToString() == actual.ToString() && expected.ValueKind == actual.ValueKind,
                  path + ": expected " + expected + ", actual " + actual);
    }

    static object Replay<T, F>(JsonElement scenario, T state, Step<T, F> step, Surrender<T> surrender)
    {
        var snapshots = new List<object>();
        long iterations = 0;
        int index = 0;
        foreach (var row in scenario.GetProperty("steps").EnumerateArray())
        {
            try
            {
                var transitions = new List<string>();
                if (row.TryGetProperty("surrender_playing", out var playing))
                {
                    Shape(row, new[] { "surrender_playing" }, "expect");
                    surrender(ref state, playing.GetBoolean());
                }
                else
                {
                    Shape(row, new[] { "facts" }, "repeat", "expect");
                    int repeat = row.TryGetProperty("repeat", out var count) ? count.GetInt32() : 1;
                    Need(repeat >= 1 && repeat <= 1000000, "repeat must be 1..1000000");
                    iterations += repeat;
                    Need(iterations <= 1000000, "Per-case iteration limit exceeded");
                    var data = row.GetProperty("facts");
                    FactsShape(data, typeof(F));
                    var facts = data.Deserialize<F>(Json);
                    for (int i = 0; i < repeat; i++)
                        transitions.AddRange(step(ref state, in facts));
                }
                var snapshot = JsonSerializer.SerializeToElement(state, Json);
                if (row.TryGetProperty("expect", out var expected)) Expect(expected, snapshot, "step " + index);
                snapshots.Add(new { step = index, state = snapshot, transitions });
            }
            catch (Exception error) { throw new ArgumentException("step " + index + ": " + error.Message, error); }
            index++;
        }
        var final = JsonSerializer.SerializeToElement(state, Json);
        var expectation = scenario.GetProperty("expect");
        Need(expectation.ValueKind == JsonValueKind.Object && expectation.EnumerateObject().Any(), "Nonempty final expectation required");
        Expect(expectation, final, "final");
        return new { iterations, state = final, snapshots };
    }

    static string[] Strings(JsonElement value, int count, string label)
    {
        Need(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == count, label + ": exact count " + count + " required");
        var result = value.EnumerateArray().Select(v => v.GetString()).ToArray();
        Need(result.All(v => !string.IsNullOrWhiteSpace(v)) && result.Distinct(StringComparer.Ordinal).Count() == count,
             label + ": distinct nonempty IDs required");
        return result;
    }

    static object Run(JsonElement scenario)
    {
        Shape(scenario, new[] { "id", "family", "size", "area_ids", "original_ids", "steps", "expect" });
        Need(!string.IsNullOrWhiteSpace(scenario.GetProperty("id").GetString()), "Case ID required");
        Need(SkirmishIdParsing.TryParseSize(scenario.GetProperty("size").GetString(), out var size), "Invalid size");
        var steps = scenario.GetProperty("steps");
        Need(steps.ValueKind == JsonValueKind.Array && steps.GetArrayLength() > 0 && steps.GetArrayLength() <= 10000, "1..10000 steps required");
        switch (scenario.GetProperty("family").GetString())
        {
            case "FC":
                var zones = Strings(scenario.GetProperty("area_ids"), 3, "area_ids");
                Strings(scenario.GetProperty("original_ids"), 0, "original_ids");
                return Replay<SkirmishFrontlineState, SkirmishFrontlineTickFacts>(scenario,
                    SkirmishFrontlineReducer.Create(size, zones[0], zones[1], zones[2]),
                    (ref SkirmishFrontlineState s, in SkirmishFrontlineTickFacts f) => { SkirmishFrontlineReducer.Step(ref s, in f); return Array.Empty<string>(); },
                    SkirmishFrontlineReducer.TrySurrender);
            case "BT":
                var corridors = Strings(scenario.GetProperty("area_ids"), 2, "area_ids");
                var rifles = Strings(scenario.GetProperty("original_ids"), SkirmishBreakthroughReducer.OriginalCount, "original_ids");
                return Replay<SkirmishBreakthroughState, SkirmishBreakthroughTickFacts>(scenario,
                    SkirmishBreakthroughReducer.Create(size, corridors[0], corridors[1], rifles),
                    SkirmishBreakthroughReducer.Step, SkirmishBreakthroughReducer.TrySurrender);
            case "CE":
                var destination = Strings(scenario.GetProperty("area_ids"), 1, "area_ids");
                var trucks = Strings(scenario.GetProperty("original_ids"), SkirmishConvoyReducer.OriginalCount, "original_ids");
                return Replay<SkirmishConvoyState, SkirmishConvoyTickFacts>(scenario,
                    SkirmishConvoyReducer.Create(size, destination[0], trucks),
                    SkirmishConvoyReducer.Step, SkirmishConvoyReducer.TrySurrender);
            case "BA":
                Strings(scenario.GetProperty("area_ids"), 0, "area_ids");
                Strings(scenario.GetProperty("original_ids"), 0, "original_ids");
                return Replay<BaseState, SkirmishBaseAssaultFacts>(scenario, new BaseState(),
                    (ref BaseState s, in SkirmishBaseAssaultFacts f) => {
                        Need(float.IsFinite(f.ElapsedSeconds) && f.ElapsedSeconds >= 0 && f.DeadlineSeconds >= 0, "Invalid Base Assault clock facts");
                        if (!s.Terminal && OfflineBaseAssaultRule.TryEvaluate(in f, out var outcome, out var reason))
                        { s.Outcome = outcome; s.Reason = reason; }
                        return Array.Empty<string>();
                    },
                    (ref BaseState s, bool playing) => {
                        var f = new SkirmishBaseAssaultFacts { Playing = playing, Surrender = true };
                        if (s.Terminal || !OfflineBaseAssaultRule.TryEvaluate(in f, out var outcome, out var reason)) return false;
                        s.Outcome = outcome; s.Reason = reason; return true;
                    });
            default: throw new ArgumentException("Unknown objective family");
        }
    }

    public static int Main(string[] args)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            Need(args.Length == 1, "Expected trace JSON path");
            Need(new System.IO.FileInfo(args[0]).Length <= 16 * 1024 * 1024, "Trace exceeds 16 MiB");
            using var document = JsonDocument.Parse(System.IO.File.ReadAllBytes(args[0]));
            var root = document.RootElement;
            UniqueKeys(root);
            Shape(root, new[] { "schema_version", "cases" });
            Need(root.GetProperty("schema_version").GetInt32() == 1, "Unsupported schema");
            var cases = root.GetProperty("cases");
            Need(cases.ValueKind == JsonValueKind.Array && cases.GetArrayLength() >= 1 && cases.GetArrayLength() <= 10000, "1..10000 cases required");
            var ids = new HashSet<string>();
            var results = new List<object>();
            int failed = 0;
            foreach (var scenario in cases.EnumerateArray())
            {
                string id = scenario.ValueKind == JsonValueKind.Object && scenario.TryGetProperty("id", out var identifier) ? identifier.ToString() : "<invalid>";
                try
                {
                    Need(ids.Add(id), "Duplicate case ID");
                    results.Add(new { id, status = "passed", replay = Run(scenario) });
                }
                catch (Exception error) { failed++; results.Add(new { id, status = "failed", error = error.Message }); }
            }
            Console.WriteLine(JsonSerializer.Serialize(new { schema_version = 1, scope = "ProductionObjectiveFactReplay",
                result = failed == 0 ? "passed" : "failed", passed = results.Count - failed, failed,
                elapsed_seconds = watch.Elapsed.TotalSeconds, native_runs = 0, full_battle_simulated = false,
                mission_qualified = false, cases = results }, Json));
            return failed == 0 ? 0 : 1;
        }
        catch (Exception error)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { result = "failed", error = error.Message,
                native_runs = 0, full_battle_simulated = false, mission_qualified = false }, Json));
            return 1;
        }
    }
}
