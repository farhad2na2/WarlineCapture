using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Runtime.Navigation;
using Game.Runtime.Pathfinding;

// Evaluates complete candidate snapshots, without inventing world/animation facts.
static class NavigationProgram
{
    internal static readonly JsonSerializerOptions Json = new()
    {
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    internal static void Need(bool condition, string message)
    { if (!condition) throw new ArgumentException(message); }

    internal static void Shape(JsonElement value, string[] required, params string[] optional)
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

    internal static void FactsShape(JsonElement value, Type type)
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

    internal static void Expect(JsonElement expected, JsonElement actual, string path)
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


    internal static void FiniteFields(object value)
    {
        foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var v = field.GetValue(value);
            if (v is float f) Need(float.IsFinite(f), "Non-finite input: " + field.Name);
            else if (v is Array array) { foreach (var item in array) FiniteFields(item); }
            else if (v != null && v.GetType().IsValueType && !v.GetType().IsPrimitive && !v.GetType().IsEnum) FiniteFields(v);
            else if (v is byte b) Need(b <= 1, "Flag must be 0 or 1: " + field.Name);
        }
    }


    internal struct OccupancyStorage : IGridOccupancyStorage
    {
        public int[] Counts;
        public bool[] Occupied;
        public int GetCount(int index) => Counts[index];
        public void SetCount(int index, int count) => Counts[index] = count;
        public void SetOccupied(int index, bool occupied) => Occupied[index] = occupied;
    }

    internal static int Integer(JsonElement item, string name, int min, int max)
    {
        int value = item.GetProperty(name).GetInt32();
        Need(value >= min && value <= max, name + " out of supported range");
        return value;
    }

    static long WorkRemaining = 20000000;
    static long OutputCellsRemaining = 2000000;

    internal static void ChargeQuery(long operations, long outputCells)
    {
        WorkRemaining -= operations;
        OutputCellsRemaining -= outputCells;
        Need(WorkRemaining >= 0 && OutputCellsRemaining >= 0, "Replay work/output budget exceeded");
    }

    static object Run(JsonElement row)
    {
        Shape(row, new[] { "id", "width", "height", "steps", "expect" }, "map");
        Need(!string.IsNullOrWhiteSpace(row.GetProperty("id").GetString()), "Case ID required");
        int width = Integer(row, "width", 1, 512), height = Integer(row, "height", 1, 512);
        var storage = new OccupancyStorage { Counts = new int[width * height], Occupied = new bool[width * height] };
        var members = new Dictionary<string, NavigationContext.Member>(StringComparer.Ordinal);
        int nextIdentity = 1;
        var routeResults = new List<object>();
        NavigationContext.Map map = row.TryGetProperty("map", out var mapJson) ? NavigationContext.ReadMap(mapJson, width * height) : null;
        var steps = row.GetProperty("steps");
        Need(steps.ValueKind == JsonValueKind.Array && steps.GetArrayLength() >= 1 && steps.GetArrayLength() <= 10000,
            "1..10000 steps required");
        WorkRemaining -= (long)width * height * steps.GetArrayLength();
        OutputCellsRemaining -= (long)width * height * (1 + steps.EnumerateArray().Count(s => s.TryGetProperty("kind", out var k) && k.GetString() == "snapshot"));
        Need(WorkRemaining >= 0 && OutputCellsRemaining >= 0, "Replay work/output budget exceeded");
        var history = new List<object>();
        foreach (var step in steps.EnumerateArray())
        {
            string kind = step.GetProperty("kind").GetString();
            if (kind == "add" || kind == "update")
            {
                Shape(step, new[] { "kind", "entity", "cell_x", "cell_z", "size_x", "size_z" }, "manual_group");
                string entity = step.GetProperty("entity").GetString();
                Need(!string.IsNullOrWhiteSpace(entity), "Entity ID required");
                int x = Integer(step, "cell_x", -1000000, 1000000), z = Integer(step, "cell_z", -1000000, 1000000);
                int sx = Integer(step, "size_x", 1, 1024), sz = Integer(step, "size_z", 1, 1024);
                var bounds = GridOccupancyKernel.Bounds(width, height, x, z, sx, sz);
                if (kind == "add")
                {
                    Need(!members.ContainsKey(entity), "Entity already registered: " + entity);
                    GridOccupancyKernel.Add(width, ref storage, bounds);
                }
                else
                {
                    Need(members.TryGetValue(entity, out var previous), "Unknown entity: " + entity);
                    GridOccupancyKernel.Update(width, ref storage, previous.Bounds, bounds);
                }
                int identity = kind == "add" ? nextIdentity++ : members[entity].Identity.Index;
                byte group = step.TryGetProperty("manual_group", out _) ? (byte)Integer(step, "manual_group", 0, 1) :
                    kind == "update" ? members[entity].ManualGroup : (byte)0;
                members[entity] = new NavigationContext.Member { Bounds = bounds, Identity = new PlacementIdentity(identity, 1),
                    Cell = new PlacementCell(x, z), Size = new PlacementCell(sx, sz), ManualGroup = group };
            }
            else if (kind == "remove")
            {
                Shape(step, new[] { "kind", "entity" });
                string entity = step.GetProperty("entity").GetString();
                Need(entity != null && members.TryGetValue(entity, out _), "Unknown entity");
                GridOccupancyKernel.Remove(width, ref storage, members[entity].Bounds);
                members.Remove(entity);
            }
            else if (kind == "placement" || kind == "path" || kind == "route")
            {
                Need(map != null, "Explicit map facts required for navigation queries");
                var context = new NavigationContext.Context { GridWidth = width, GridHeight = height, Facts = map,
                    OccupiedCells = storage.Occupied, Members = members.Values.ToArray() };
                routeResults.Add(NavigationContext.Query(step, ref context, members));
            }
            else if (kind == "snapshot")
            {
                Shape(step, new[] { "kind" });
                history.Add(new { counts = (int[])storage.Counts.Clone(), occupied = (bool[])storage.Occupied.Clone(), entities = members.Count });
            }
            else throw new ArgumentException("Unknown step kind: " + kind);
        }
        var snapshot = JsonSerializer.SerializeToElement(new { counts = storage.Counts, occupied = storage.Occupied,
            entities = members.Count, snapshots = history, queries = routeResults }, Json);
        var expected = row.GetProperty("expect");
        Need(expected.ValueKind == JsonValueKind.Object && expected.EnumerateObject().Any(), "Nonempty expectation required");
        Expect(expected, snapshot, "navigation");
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
            Console.WriteLine(JsonSerializer.Serialize(new { schema_version = 1, scope = "ProductionNavigationReplay",
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
