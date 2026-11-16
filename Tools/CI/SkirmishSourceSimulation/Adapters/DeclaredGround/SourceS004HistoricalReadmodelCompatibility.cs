using System;
using System.IO;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace Game.Runtime
{
    // Source-only compatibility check. Callers must separately verify the
    // historical capture, compiler readback, current pins and diagnostic scope.
    internal static class SourceS004HistoricalReadmodelCompatibility
    {
        internal static void RequireCompatible(SkirmishResolvedSetup historicalRegular, SkirmishResolvedSetup selected, in GridConfig grid)
        {
            if (historicalRegular == null || selected == null || historicalRegular.CatalogId != "S004" || selected.CatalogId != "S004" ||
                historicalRegular.SizeId != SkirmishSizeId.Standard || selected.SizeId != SkirmishSizeId.Standard ||
                historicalRegular.DifficultyId != SkirmishDifficultyId.Regular || historicalRegular.SetupHash != 1130726651u ||
                historicalRegular.Seed != 104733 || selected.Seed != 104733 || !historicalRegular.MeasuredLayoutBound || !selected.MeasuredLayoutBound ||
                grid.Width != 2048 || grid.Height != 1024 || grid.CellSize != 1f || !math.all(grid.Origin == float3.zero))
                throw new InvalidDataException("Exact S004 historical and selected Standard source identities required");
            var left = JObject.FromObject(historicalRegular); var right = JObject.FromObject(selected);
            foreach (string name in new[] { "SetupHash", "DifficultyId", "Report" }) { left.Remove(name); right.Remove(name); }
            var leftRows = left["Structures"] as JArray; var rightRows = right["Structures"] as JArray;
            if (leftRows == null || rightRows == null || leftRows.Count != 20 || rightRows.Count != 20)
                throw new InvalidDataException("Exact S004 structure rows required");
            for (int i = 0; i < leftRows.Count; i++)
            {
                var a = (JObject)leftRows[i]; var b = (JObject)rightRows[i];
                float ax = (float)a["SpawnWorldX"], az = (float)a["SpawnWorldZ"];
                float bx = (float)b["SpawnWorldX"], bz = (float)b["SpawnWorldZ"];
                if (!math.all(math.isfinite(new float4(ax, az, bx, bz))) ||
                    Math.Abs((long)BitConverter.SingleToInt32Bits(ax) - BitConverter.SingleToInt32Bits(bx)) > 1 ||
                    Math.Abs((long)BitConverter.SingleToInt32Bits(az) - BitConverter.SingleToInt32Bits(bz)) > 1)
                    throw new InvalidDataException("Structure coordinate differs by more than one float32 step");
                int2 first = GridUtils.WorldToCell(in grid, new float3(ax, 0f, az));
                int2 second = GridUtils.WorldToCell(in grid, new float3(bx, 0f, bz));
                if (!math.all(first == second) || !GridUtils.InBounds(first, grid.Width, grid.Height))
                    throw new InvalidDataException("Structure origin grid cell differs or is out of bounds");
                b["SpawnWorldX"] = a["SpawnWorldX"].DeepClone(); b["SpawnWorldZ"] = a["SpawnWorldZ"].DeepClone();
            }
            if (!JToken.DeepEquals(left, right)) throw new InvalidDataException("Historical readmodel setup differs in a non-coordinate field");
        }
    }
}
