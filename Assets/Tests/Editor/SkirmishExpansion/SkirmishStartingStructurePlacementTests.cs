using System.Collections.Generic;
using System.IO;
using Game.Configs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Editor
{
    /// <summary>
    /// Every Playable legacy skirmish (S001 Desert Base, S025 City Crossroads, S073 Industrial Basin)
    /// grants its starting structures at exact preferred cells (no search). Each footprint must sit on
    /// free ground of the shared opmap.skirmish.desert_base_01 grid: no authored building/solid prop,
    /// water, road or static blocker, and no overlap with the same side's other structures.
    /// The fixture is the runtime placement mask (authored|water|road|blocker) captured from the
    /// loaded match; regenerate it if the map's authored content changes.
    /// </summary>
    public sealed class SkirmishStartingStructurePlacementTests
    {
        private const string MaskPath = "Assets/Tests/Editor/SkirmishExpansion/Fixtures/skirmish_desert_base_01_placement_mask.txt";

        private static readonly Dictionary<string, Vector2Int> Footprints = new()
        {
            { "Building_Barrack", new Vector2Int(28, 15) },
            { "Building_Ammunition_Depot", new Vector2Int(19, 16) },
            { "Building_OilPump", new Vector2Int(15, 4) },
            { "Building_Refinery", new Vector2Int(25, 15) },
            { "Building_Fuel_Bladder", new Vector2Int(14, 10) },
        };

        private static IEnumerable<TestCaseData> Scenarios()
        {
            yield return new TestCaseData("S001", "Assets/Game/Configs/Skirmish/InitialForces.asset");
            yield return new TestCaseData("S025", "Assets/Game/Configs/Skirmish/CityCrossroads/InitialForces.asset");
            yield return new TestCaseData("S073", "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset");
        }

        [TestCaseSource(nameof(Scenarios))]
        public void StartingStructuresPlaceOnFreeCellsForBothSides(string scenarioId, string configPath)
        {
            var config = AssetDatabase.LoadAssetAtPath<InitialUnitsSpawnerAuthoringConfig>(configPath);
            Assert.NotNull(config, configPath);
            ReadMask(out int width, out int height, out char[] mask);
            var problems = new List<string>();
            int checkedSides = 0;
            foreach (var faction in config.Factions)
            {
                if (faction.Buildings.Count == 0) continue;
                checkedSides++;
                var placed = new List<RectInt>();
                foreach (var building in faction.Buildings)
                {
                    string name = building.Prefab != null ? building.Prefab.name : "<null>";
                    if (!Footprints.TryGetValue(name, out Vector2Int size))
                    {
                        problems.Add(scenarioId + " faction " + faction.FactionId + ": unknown footprint for " + name);
                        continue;
                    }
                    var rect = new RectInt(faction.SpawnCell + building.OriginOffset, size);
                    string where = scenarioId + " faction " + faction.FactionId + " " + name + " at " + rect.position;
                    if (rect.xMin < 0 || rect.yMin < 0 || rect.xMax > width || rect.yMax > height)
                    {
                        problems.Add(where + " is outside the grid");
                        continue;
                    }
                    char hit = '.';
                    for (int y = rect.yMin; y < rect.yMax && hit == '.'; y++)
                        for (int x = rect.xMin; x < rect.xMax && hit == '.'; x++)
                            hit = mask[y * width + x];
                    if (hit != '.')
                        problems.Add(where + " overlaps " + Describe(hit));
                    foreach (RectInt other in placed)
                        if (other.Overlaps(rect)) problems.Add(where + " overlaps another starting structure at " + other.position);
                    placed.Add(rect);
                }
            }
            Assert.AreEqual(2, checkedSides, scenarioId + " must define starting structures for both sides.");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        private static string Describe(char c) => c switch
        {
            'a' => "an authored map building/prop",
            'w' => "water",
            'r' => "a road",
            'b' => "a static blocker",
            _ => "an invalid cell '" + c + "'"
        };

        private static void ReadMask(out int width, out int height, out char[] mask)
        {
            string[] lines = File.ReadAllLines(Path.GetFullPath(MaskPath));
            string[] header = lines[0].Split(' ');
            width = int.Parse(header[0]);
            height = int.Parse(header[1]);
            mask = new char[width * height];
            for (int y = 0; y < height; y++)
            {
                int x = 0;
                foreach (string token in lines[1 + y].Split(','))
                {
                    if (token.Length < 2) continue;
                    int run = int.Parse(token.Substring(1));
                    for (int i = 0; i < run; i++) mask[y * width + x++] = token[0];
                }
                Assert.AreEqual(width, x, "Mask row " + y + " is truncated.");
            }
        }
    }
}
