using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

internal static class SourceS004HistoricalReadmodelCompatibilityControl
{
    static void Main(string[] args)
    {
        if (args.Length != 10) throw new ArgumentException("baseline and four selected path/hash pairs required");
        var baseline = SourceS004CompiledSetupInput.Read(args[0], args[1]);
        var grid = new GridConfig { Width = 2048, Height = 1024, CellSize = 1f, Origin = float3.zero };
        for (int i = 0; i < 4; i++)
        {
            var selected = SourceS004CompiledSetupInput.Read(args[2 + i * 2], args[3 + i * 2]);
            SourceS004HistoricalReadmodelCompatibility.RequireCompatible(baseline, selected, in grid);
        }
        var badBudget = JObject.FromObject(baseline); badBudget["MaterialsEach"] = (int)badBudget["MaterialsEach"] + 1;
        Reject(baseline, badBudget.ToObject<SkirmishResolvedSetup>(), grid);
        var badPosition = JObject.FromObject(baseline); badPosition["Structures"][0]["SpawnWorldZ"] = 700f;
        Reject(baseline, badPosition.ToObject<SkirmishResolvedSetup>(), grid);
        var badGrid = grid; badGrid.CellSize = 2f; Reject(baseline, baseline, badGrid);
        Console.WriteLine("[SourceS004HistoricalReadmodelCompatibilityControl] result=Passed compatible=4 rejected=3 scope=FixtureOnly world=0 readiness=0");
    }
    static void Reject(SkirmishResolvedSetup baseline, SkirmishResolvedSetup selected, GridConfig grid)
    {
        try { SourceS004HistoricalReadmodelCompatibility.RequireCompatible(baseline, selected, in grid); }
        catch (InvalidDataException) { return; }
        throw new InvalidOperationException("Invalid historical compatibility accepted");
    }
}
