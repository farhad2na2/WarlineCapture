using System;
using System.IO;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

// Original GridUtils on two real source-compiler outputs and captured grid facts.
internal static class SourceS004NumericModeGridControl
{
    static void Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("two compiler results and serialized grid projection required");
        var left = JObject.Parse(File.ReadAllText(args[0]));
        var right = JObject.Parse(File.ReadAllText(args[1]));
        var gridFacts = (JObject)JObject.Parse(File.ReadAllText(args[2]))["grid"];
        var dims = (JArray)gridFacts["dimensions"]; var origin = (JArray)gridFacts["origin"];
        var grid = new GridConfig { Width = (int)dims[0], Height = (int)dims[1], CellSize = (float)gridFacts["cell_size"],
            Origin = new float3((float)origin[0], (float)origin[1], (float)origin[2]) };
        if (grid.Width != 2048 || grid.Height != 1024 || grid.CellSize != 1 || !math.all(grid.Origin == float3.zero))
            throw new InvalidDataException("Exact captured source grid required");
        var a = (JArray)left["rows"]; var b = (JArray)right["rows"];
        if (a.Count != 12 || b.Count != 12) throw new InvalidDataException("Exact original compiler tuple counts required");
        int cells = 0, changedCoordinates = 0;
        for (int row = 0; row < a.Count; row++)
        {
            if ((string)a[row]["size"] != (string)b[row]["size"] || (string)a[row]["difficulty"] != (string)b[row]["difficulty"])
                throw new InvalidDataException("Compiler row ordering differs");
            if ((string)a[row]["size"] != "Standard") continue;
            var sa = (JArray)a[row]["compiled_setup"]["Structures"]; var sb = (JArray)b[row]["compiled_setup"]["Structures"];
            if (sa.Count != 20 || sb.Count != 20) throw new InvalidDataException("Expected original structure rows");
            for (int i = 0; i < sa.Count; i++)
            {
                if ((string)sa[i]["StructureId"] != (string)sb[i]["StructureId"] || (int)sa[i]["FactionId"] != (int)sb[i]["FactionId"])
                    throw new InvalidDataException("Structure identity differs");
                var pa = new float3((float)sa[i]["SpawnWorldX"], 0, (float)sa[i]["SpawnWorldZ"]);
                var pb = new float3((float)sb[i]["SpawnWorldX"], 0, (float)sb[i]["SpawnWorldZ"]);
                if (!math.all(pa == pb)) changedCoordinates++;
                int2 ca = GridUtils.WorldToCell(in grid, pa); int2 cb = GridUtils.WorldToCell(in grid, pb);
                if (!math.all(ca == cb) || !GridUtils.InBounds(ca, grid.Width, grid.Height))
                    throw new InvalidDataException("Original grid cell differs or is out of bounds: " + row + "/" + i);
                cells++;
            }
        }
        if (cells != 80 || changedCoordinates != 16) throw new InvalidDataException("Unexpected observed difference count");
        Console.WriteLine("[SourceS004NumericModeGridControl] result=Passed rows=80 coordinateDifferences=16 cellDifferences=0 scope=SourceGridOnly world=0 readiness=0");
    }
}
