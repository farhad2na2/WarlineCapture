using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class S005SetupCacheIntegrityFixture
{
    static void Need(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
    static void Reject(Action action, string label)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Expected rejection: " + label);
    }
    public static void Main(string[] args)
    {
        if (args.Length != 6) throw new ArgumentException("Original receipt, two isolated copies, source graph, matrix and captured layout log required");
        string original = Path.GetFullPath(args[0]), copyA = Path.GetFullPath(args[1]), copyB = Path.GetFullPath(args[2]);
        if (original == copyA || original == copyB || copyA == copyB || File.Exists(copyA) || File.Exists(copyB))
            throw new InvalidOperationException("Fresh independent fixture paths required");
        byte[] bytes = File.ReadAllBytes(original);
        File.WriteAllBytes(copyA, bytes); File.WriteAllBytes(copyB, bytes);
        var receipt = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
        var row = ((JArray)receipt["result"]["compiler"]).OfType<JObject>().Single(x => (string)x["size"] == "Standard");
        var graph = JObject.Parse(File.ReadAllText(args[3]));
        var source = new SourceConfigMaterializer(graph);
        var catalog = source.ByPath<SkirmishExpansionCatalogConfig>((string)graph["catalog_path"]);
        var definition = source.ByPath<SkirmishScenarioDefinitionConfig>((string)graph["definition_path"]);
        Need(catalog.TryGetDefinition("S005", definition.ContentVersion, out var selected) && ReferenceEquals(selected, definition), "actual source catalog identity");
        UnityEngine.Resources.BindSourceResources(new Dictionary<string, object> { [SkirmishExpansionCatalogConfig.ResourceName] = catalog });
        var binding = typeof(S005LayoutCaptureReplaySourceBinding);
        var projection = typeof(SkirmishMeasuredLayoutBinder).GetNestedType("LayoutValues", BindingFlags.NonPublic);
        var from = projection.GetMethod("From", BindingFlags.Public | BindingFlags.Static);
        string raw = (string)binding.GetMethod("ExtractJson", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { File.ReadAllText(args[5]), "authored asset=" });
        object captured = JsonConvert.DeserializeObject(raw, projection), authored = from.Invoke(null, new object[] { definition.MapLayout });
        Need((bool)binding.GetMethod("Equal", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new[] { authored, captured, projection }), "source layout matches captured typed values");
        binding.GetField("replayType", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, projection);
        binding.GetField("replayValues", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, captured);
        binding.GetField("replayJson", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, raw);
        Need(SkirmishSetupMatrixTable.TryLoadFromText(File.ReadAllText(args[4]), out List<SkirmishSetupMatrixRow> matrix, out _), "actual matrix parser");
        Need(SkirmishSetupCompiler.TryCompile(definition, SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 1,
            new SkirmishContentManifest { RequiredFeatureIds = definition.RequiredFeatureIds }, matrix, out var setup, out _), "original setup compiler");
        Need(setup.SetupHash == (uint)row["setupHash"], "original setup identity");
        SourceS005CompiledSetupGuard.RequireExact(setup, copyA);
        SourceS005CompiledSetupGuard.RequireExact(setup, copyA);
        var originalForce = setup.Forces[0]; var changedForce = originalForce; changedForce.Quantity++;
        setup.Forces[0] = changedForce;
        Reject(() => SourceS005CompiledSetupGuard.RequireExact(setup, copyA), "nested live force mutation");
        setup.Forces[0] = originalForce;
        int originalSeed = setup.Seed; setup.Seed++;
        Reject(() => SourceS005CompiledSetupGuard.RequireExact(setup, copyA), "live seed mutation");
        setup.Seed = originalSeed;
        File.AppendAllText(copyA, " ");
        Reject(() => SourceS005CompiledSetupGuard.RequireExact(setup, copyA), "valid JSON bytes changed");
        File.WriteAllBytes(copyA, bytes);
        SourceS005CompiledSetupGuard.RequireExact(setup, copyA);
        SourceS005CompiledSetupGuard.RequireExact(setup, copyB);
        Reject(() => SourceS005CompiledSetupGuard.RequireExact(setup, Path.Combine(Path.GetDirectoryName(copyA), "missing-receipt.json")), "missing source file");
        var metrics = SourceS005CompiledSetupGuard.Metrics();
        bool enabled = Environment.GetEnvironmentVariable("WARLINE_S005_SETUP_RECEIPT_CACHE") == "1";
        Need((bool)metrics["opt_in"] == enabled, "mode readback");
        Need(enabled ? (long)metrics["cache_hits"] > 0 : (long)metrics["cache_hits"] == 0, "cache hit isolation");
        Console.WriteLine("[S005SetupCacheIntegrityFixture] result=Passed cases=7 mode=" + (enabled ? "on" : "off") + " scope=FixtureOnly");
    }
}
