using System;
using System.Collections.Generic;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class SourceExpandedAuthoredSetupControl
{
    public static int Main(string[] args)
    {
        if (args.Length != 7 && args.Length != 9) throw new ArgumentException("Graph, graph hash, matrix, matrix hash, root, mission and seed required; optional pinned layout path and hash");
        int seed = Int32.Parse(args[6], System.Globalization.CultureInfo.InvariantCulture);
        var input = SourceExpandedAuthoredSetupInput.Prepare(args[0], args[1], args[2], args[3], args[4], args[5]);
        using var replay = args.Length == 9 ? SourceValidatedLayoutJson.Open(input.Definition.MapLayout, args[7], args[8]) : null;
        var resources = new Dictionary<string, object>
        {
            [SkirmishExpansionCatalogConfig.ResourceName] = input.Catalog,
            [SkirmishSetupMatrixTable.PackagedResourceName] = new UnityEngine.TextAsset(System.IO.File.ReadAllText(args[2]))
        };
        if (args[5] == "S004")
        {
            resources["SkirmishS004OperationMap"] = input.Assets.ByPath<OperationMapDefinition>("Assets/Game/Resources/SkirmishS004OperationMap.asset");
            resources[SkirmishBattleCatalogConfig.ResourceName] = input.Assets.ByPath<SkirmishBattleCatalogConfig>("Assets/Game/Resources/SkirmishBattleCatalog.asset");
        }
        UnityEngine.Resources.BindSourceResources(resources);
        int failures = 0;
        foreach (var difficulty in new[] { SkirmishDifficultyId.Recruit, SkirmishDifficultyId.Regular, SkirmishDifficultyId.Veteran, SkirmishDifficultyId.Commander })
        foreach (var size in new[] { SkirmishSizeId.Standard, SkirmishSizeId.War, SkirmishSizeId.LargeWar })
        {
            try
            {
                var actual = input.Compile(difficulty, size, seed);
                if (!SkirmishSetupCompiler.TryCompile(input.Definition, difficulty, size, seed,
                        input.Manifest, input.Matrix, out var expected, out var reasons) || expected == null ||
                    !JToken.DeepEquals(JToken.Parse(JsonConvert.SerializeObject(actual)), JToken.Parse(JsonConvert.SerializeObject(expected))))
                    throw new InvalidOperationException("Original compiler projection differs: " + String.Join(" | ", reasons));
                Console.WriteLine("[SourceExpandedAuthoredSetupControl] result=Passed mission=" + args[5] + " difficulty=" + difficulty + " size=" + size + " sourceOnly=1 readiness=0");
            }
            catch (Exception error)
            {
                failures++;
                Console.WriteLine("[SourceExpandedAuthoredSetupControl] result=Failed mission=" + args[5] + " difficulty=" + difficulty + " size=" + size + " errorType=" + error.GetType().FullName + " error=" + error.Message + " sourceOnly=1 readiness=0");
            }
        }
        return failures == 0 ? 0 : 1;
    }
}
