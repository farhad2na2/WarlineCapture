using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Runtime.Combat;

// Evaluates complete candidate snapshots, without inventing world/animation facts.
static class WorldJson
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

    internal static void UniqueKeys(JsonElement value)
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

        }
    }

}
