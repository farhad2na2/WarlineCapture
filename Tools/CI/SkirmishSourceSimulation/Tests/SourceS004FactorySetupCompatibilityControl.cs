using System;
using System.Collections.Generic;
using System.Globalization;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;

// Exact original compiler versus a separately pinned historical factory setup.
// This control performs no world, production, or player operation.
internal static class SourceS004FactorySetupCompatibilityControl
{
    static int Main(string[] args)
    {
        if (args.Length != 10) throw new ArgumentException("graph/hash, matrix/hash, root, layout/hash, historical setup/hash and fresh export required");
        var input = SourceExpandedAuthoredSetupInput.Prepare(args[0], args[1], args[2], args[3], args[4], "S004");
        var map = input.Assets.ByPath<OperationMapDefinition>("Assets/Game/Resources/SkirmishS004OperationMap.asset");
        var catalog = input.Assets.ByPath<SkirmishBattleCatalogConfig>("Assets/Game/Resources/SkirmishBattleCatalog.asset");
        UnityEngine.Resources.BindSourceResources(new Dictionary<string, object> {
            [SkirmishExpansionCatalogConfig.ResourceName] = input.Catalog,
            [SkirmishBattleCatalogConfig.ResourceName] = catalog,
            ["SkirmishS004OperationMap"] = map });
        using var replay = SourceValidatedLayoutJson.Open(input.Definition.MapLayout, args[5], args[6]);
        var actual = input.Compile(SkirmishDifficultyId.Regular, SkirmishSizeId.Standard, 104733);
        bool rejected = false; string reason = null;
        try { SourceS004CompiledSetupInput.RequireExact(actual, args[7], args[8]); }
        catch (System.IO.InvalidDataException error) { rejected = true; reason = error.Message; }
        if (!rejected) throw new InvalidOperationException("Historical setup must not authorize changed current compiler output");
        var export = new JObject { ["kind"] = "S004StandardCompiledSetupSourceExport", ["status"] = "Passed",
            ["compiled_setup"] = JObject.FromObject(actual), ["world_initialized"] = false,
            ["physical_source_fit_verified"] = false, ["mission_readiness"] = false,
            ["identity"] = new JObject { ["catalog_id"] = actual.CatalogId, ["definition_id"] = actual.DefinitionId,
                ["scenario_setup_id"] = actual.ScenarioSetupId, ["difficulty_id"] = actual.DifficultyId.ToString(),
                ["size_id"] = actual.SizeId.ToString(), ["seed"] = actual.Seed, ["content_version"] = actual.ContentVersion,
                ["measured_layout_bound"] = actual.MeasuredLayoutBound, ["setup_hash"] = actual.SetupHash },
            ["source_bound_compiler_result"] = new JObject { ["status"] = "Compiled", ["compiled"] = true,
                ["catalog_id"] = actual.CatalogId, ["definition_id"] = actual.DefinitionId },
            ["scope"] = "Current source compiler result; no native or player qualification" };
        using (var stream = new System.IO.FileStream(args[9], System.IO.FileMode.CreateNew, System.IO.FileAccess.Write))
        using (var writer = new System.IO.StreamWriter(stream)) writer.Write(export.ToString());
        var currentBytes = System.IO.File.ReadAllBytes(args[9]);
        SourceS004CompiledSetupInput.RequireExact(actual, args[9], AutonomousWorld.Hash(currentBytes));
        Console.WriteLine("[SourceS004FactorySetupCompatibility] result=RejectedHistoricalMismatch currentHash=" +
            actual.SetupHash.ToString(CultureInfo.InvariantCulture) + " reason=" + reason + " currentExact=1 gameplay=0 readiness=0");
        return 0;
    }
}
