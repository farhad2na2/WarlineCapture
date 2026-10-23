using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Runtime.Combat;

// Evaluates complete candidate snapshots, without inventing world/animation facts.
static class CombatProgram
{
    static readonly JsonSerializerOptions Json = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
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
            Need(expected.EnumerateObject().Any(), path + ": empty expectation object");
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
        else if (expected.ValueKind == JsonValueKind.Number && actual.ValueKind == JsonValueKind.Number)
            Need(expected.GetDouble() == actual.GetDouble(), path + ": expected " + expected + ", actual " + actual);
        else Need(expected.ToString() == actual.ToString() && expected.ValueKind == actual.ValueKind,
                  path + ": expected " + expected + ", actual " + actual);
    }


    static void FiniteFields(object value)
    {
        foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var v = field.GetValue(value);
            if (v is float f) Need(float.IsFinite(f), "Non-finite input: " + field.Name);
            else if (v is StandardAttackState) FiniteFields(v);
            else if (v is byte b) Need(b <= 1, "Flag must be 0 or 1: " + field.Name);
        }
    }

    static object Run(JsonElement row)
    {
        Shape(row, new[] { "id", "input", "delta_time", "cell_size", "expect" });
        Need(!string.IsNullOrWhiteSpace(row.GetProperty("id").GetString()), "Case ID required");
        var data = row.GetProperty("input");
        FactsShape(data, typeof(StandardAttackInput));
        var input = data.Deserialize<StandardAttackInput>(Json);
        FiniteFields(input);
        float dt = row.GetProperty("delta_time").GetSingle();
        float cell = row.GetProperty("cell_size").GetSingle();
        Need(float.IsFinite(dt) && dt >= 0, "delta_time must be finite and nonnegative");
        Need(float.IsFinite(cell) && cell > 0, "cell_size must be finite and positive");
        var result = StandardAttackPlanner.Plan(input, dt, cell);
        var snapshot = JsonSerializer.SerializeToElement(result, Json);
        var expected = row.GetProperty("expect");
        Need(expected.ValueKind == JsonValueKind.Object && expected.EnumerateObject().Any(), "Nonempty expectation required");
        Expect(expected, snapshot, "plan");
        return snapshot;
    }

    public static int Main(string[] args)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            Need(args.Length == 1, "Expected candidate JSON path");
            Need(new System.IO.FileInfo(args[0]).Length <= 16 * 1024 * 1024, "Input exceeds 16 MiB");
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
            foreach (var row in cases.EnumerateArray())
            {
                string id = row.ValueKind == JsonValueKind.Object && row.TryGetProperty("id", out var identifier) ? identifier.ToString() : "<invalid>";
                try
                {
                    Need(ids.Add(id), "Duplicate case ID");
                    results.Add(new { id, status = "passed", plan = Run(row) });
                }
                catch (Exception error) { failed++; results.Add(new { id, status = "failed", error = error.Message }); }
            }
            Console.WriteLine(JsonSerializer.Serialize(new { schema_version = 1, scope = "ProductionStandardAttackPlanReplay",
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
