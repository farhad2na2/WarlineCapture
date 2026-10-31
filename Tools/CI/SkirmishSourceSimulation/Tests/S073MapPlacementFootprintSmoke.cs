using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Game.Components;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

internal static class S073MapPlacementFootprintSmoke
{
    private sealed class OpenGrid : IRuntimeGridBlockerPlacementGrid
    {
        public int Width { get; }
        public int Height { get; }
        public OpenGrid(int width, int height) { Width = width; Height = height; }
        public bool IsWalkable(int cellIndex) => true;
        public bool IsRoad(int cellIndex) => false;
        public bool IsBlocked(int cellIndex) => false;
    }

    public static int Main(string[] args)
    {
        JObject result;
        try
        {
            if (args.Length != 6)
                throw new ArgumentException("Expected map capture/path pin, source root, blocker metadata/path pin, and pinned grid cell size");

            string capturePath = Path.GetFullPath(args[0]);
            var definitions = GeneratedBuildingDefinitions.LoadS073MapPlacements(
                capturePath, args[1], args[2], new Dictionary<string, Entity>(StringComparer.Ordinal));
            JObject capture = JObject.Parse(File.ReadAllText(capturePath));
            var rows = capture["placement_rows"] as JArray;
            if (rows == null || rows.Count != 432 || definitions.Definitions.Count != 23)
                throw new InvalidDataException("Exact captured S073 map source domain required");

            string gridText = File.ReadAllText(Path.Combine(args[2], "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset"));
            int gridWidth = ReadInt(gridText, "width");
            int gridHeight = ReadInt(gridText, "height");
            float cellSize = ReadFloat(gridText, "cellSize");
            Match originMatch = Regex.Match(gridText, @"(?m)^  origin: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}$");
            if (!originMatch.Success)
                throw new InvalidDataException("Selected source grid origin is required");
            var grid = new GridConfig
            {
                Width = gridWidth,
                Height = gridHeight,
                CellSize = cellSize,
                Origin = new Unity.Mathematics.float3(
                    float.Parse(originMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                    float.Parse(originMatch.Groups[2].Value, CultureInfo.InvariantCulture),
                    float.Parse(originMatch.Groups[3].Value, CultureInfo.InvariantCulture))
            };
            VerifyOriginModeSemantics(grid);

            var wallFootprints = new Dictionary<string, int>(StringComparer.Ordinal);
            int wallRows = 0, rotatedRows = 0, geometricRows = 0, outsideGridRows = 0, resolvedRows = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                JObject row = (JObject)rows[i];
                if ((int?)row["row_index"] != i)
                    throw new InvalidDataException("S073 placement order changed");
                int index = (int?)row["prefab_index"] ?? -1;
                if (index < 0 || index >= definitions.Definitions.Count)
                    throw new InvalidDataException("S073 placement references an unknown definition");

                RuntimeDefinition definition = definitions.Definitions[index];
                if ((string)row["prefab"]?["key"] != definition.Prefab.name)
                    throw new InvalidDataException("S073 placement prefab identity changed");

                bool rotated = (bool?)row["rotate_vertical"] == true;
                int2 footprint = MapBuildingPlacementOriginKernel.ResolvePlacementFootprint(
                    definition.FootprintCells.x,
                    definition.FootprintCells.y,
                    definition.IsWall,
                    definition.LocalBounds.size.x,
                    definition.LocalBounds.size.z,
                    rotated);
                if (footprint.x <= 0 || footprint.y <= 0)
                    throw new InvalidDataException("S073 source placement resolved an invalid footprint");

                JObject worldCenter = (JObject)row["world_center"];
                int2 centerCell = GridUtils.WorldToCell(grid, new Unity.Mathematics.float3(
                    (float)worldCenter["x"], (float)worldCenter["y"], (float)worldCenter["z"]));
                bool useGeometricCenter = (bool?)row["use_geometric_footprint_center"] == true;
                int2 resolvedOrigin = MapBuildingPlacementOriginKernel.ResolveOriginCell(
                    centerCell, new Vector2Int(footprint.x, footprint.y), grid, useGeometricCenter);
                int2 expectedOrigin = useGeometricCenter
                    ? new int2(centerCell.x - footprint.x / 2, centerCell.y - footprint.y / 2)
                    : MapBuildingPlacementOriginKernel.CenterCellToOrigin(
                        centerCell, new Vector2Int(footprint.x, footprint.y), grid);
                if (!resolvedOrigin.Equals(expectedOrigin))
                    throw new InvalidDataException("Shared placement origin changed source row " + i);
                resolvedRows++;
                if (useGeometricCenter) geometricRows++;
                if (resolvedOrigin.x < 0 || resolvedOrigin.y < 0 ||
                    resolvedOrigin.x + footprint.x > grid.Width || resolvedOrigin.y + footprint.y > grid.Height)
                    outsideGridRows++;
                if (rotated) rotatedRows++;
                if (definition.IsWall)
                {
                    wallRows++;
                    string key = footprint.x.ToString(CultureInfo.InvariantCulture) + "x" +
                                 footprint.y.ToString(CultureInfo.InvariantCulture) + ":" + (rotated ? "vertical" : "horizontal");
                    wallFootprints.TryGetValue(key, out int count);
                    wallFootprints[key] = count + 1;
                }
            }

            Require(wallRows == 188 && rotatedRows == 205, "S073 wall/rotation row totals changed");
            Require(resolvedRows == rows.Count, "Every captured map row must pass through the shared origin resolver");
            Require(wallFootprints.TryGetValue("12x3:horizontal", out int dirt) && dirt == 79,
                "Unrotated dirt walls must use the actual configured footprint");
            Require(wallFootprints.TryGetValue("12x1:horizontal", out int fence) && fence == 24,
                "Unrotated fences must use the actual configured footprint");
            Require(wallFootprints.TryGetValue("2x11:vertical", out int verticalDirt) && verticalDirt == 68,
                "Vertical dirt walls must use the original bounds-derived footprint");
            Require(wallFootprints.TryGetValue("1x12:vertical", out int verticalFence) && verticalFence == 17,
                "Vertical fences must use the original bounds-derived footprint");

            string blockerMetadataPath = Path.GetFullPath(args[3]);
            if (Sha(blockerMetadataPath) != args[4])
                throw new InvalidDataException("Pinned S073 runtime blocker metadata changed");
            JObject blockerMetadata = JObject.Parse(File.ReadAllText(blockerMetadataPath));
            var blockerConfig = blockerMetadata["config"] as JObject;
            var prefabs = blockerMetadata["prefabs"] as JArray;
            if ((string)blockerMetadata["kind"] != "S073OriginalRuntimeBlockerPrefabMetadata" ||
                (string)blockerMetadata["authority"] != "MatchSceneConfig_OriginalRuntimeGridBlockerPresentationSystemHelper.BuildMetadata" ||
                (int?)blockerConfig?["blocker_count"] != 1 || (int?)blockerConfig?["random_seed"] != 24680 ||
                (int?)blockerConfig?["prefab_count"] != 63 || prefabs?.Count != 63 ||
                (bool?)blockerMetadata["random_placement_executed"] != false ||
                (bool?)blockerMetadata["blocker_spawned"] != false)
                throw new InvalidDataException("Exact S073 blocker source metadata/config required; no placement instance is implied");
            var prefabNames = new HashSet<string>(StringComparer.Ordinal);
            var blockerFootprints = new JArray();
            var plannerPrefabs = new List<RuntimeGridBlockerPrefabData>();
            int centerOnlyCount = 0;
            float gridCellSize = float.Parse(args[5], CultureInfo.InvariantCulture);
            if (!(gridCellSize > 0f) || float.IsNaN(gridCellSize) || float.IsInfinity(gridCellSize))
                throw new InvalidDataException("Actual finite S073 grid cell size required");
            foreach (JObject prefab in prefabs)
            {
                string name = (string)prefab["name"];
                if (string.IsNullOrWhiteSpace(name) || !prefabNames.Add(name))
                    throw new InvalidDataException("Unique exact blocker prefab name required");
                string kind = RuntimeGridBlockerPlacementKernel.ClassifyPrefabKind(name).ToString();
                if (kind != (string)prefab["kind"])
                    throw new InvalidDataException("Shared blocker classification differs from captured source metadata: " + name);
                bool centerOnly = (bool?)prefab["blocks_only_center_cell"] == true;
                if (centerOnly) centerOnlyCount++;
                JArray size = prefab["local_bounds_size"] as JArray;
                if (size?.Count != 3 || (bool?)prefab["has_bounds"] != true)
                    throw new InvalidDataException("Exact blocker prefab bounds required: " + name);
                RuntimeGridBlockerPlacementKernel.ComputeFootprintCells(
                    centerOnly, true, (float)size[0], (float)size[2], gridCellSize,
                    out int footprintWidth, out int footprintDepth);
                if (footprintWidth < 1 || footprintDepth < 1 ||
                    centerOnly && (footprintWidth != 1 || footprintDepth != 1))
                    throw new InvalidDataException("Shared blocker footprint is invalid: " + name);
                blockerFootprints.Add(new JObject { ["name"] = name, ["kind"] = kind,
                    ["width"] = footprintWidth, ["depth"] = footprintDepth, ["center_only"] = centerOnly });
                plannerPrefabs.Add(new RuntimeGridBlockerPrefabData { Name = name,
                    Kind = (BlockerPrefabKind)Enum.Parse(typeof(BlockerPrefabKind), kind),
                    BlocksOnlyCenterCell = centerOnly, HasBounds = true,
                    BoundsWidth = (float)size[0], BoundsDepth = (float)size[2] });
            }
            Require(prefabNames.Count == 63 && centerOnlyCount > 0,
                "Complete shared blocker prefab metadata domain required");

            plannerPrefabs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            var plannerGrid = new OpenGrid(2048, 1024);
            var plan = RuntimeGridBlockerPlacementPolicy.Plan(plannerGrid, plannerPrefabs,
                (int)blockerConfig["blocker_count"], (uint)(int)blockerConfig["random_seed"], gridCellSize,
                (float)blockerConfig["tree_spawn_ratio"], (int)blockerConfig["tree_cluster_count"],
                (int)blockerConfig["tree_cluster_spacing_min_cells"], (int)blockerConfig["tree_cluster_spacing_max_cells"],
                (int)blockerConfig["tree_cluster_distance_min_cells"], (int)blockerConfig["tree_cluster_distance_max_cells"]);
            var repeatedPlan = RuntimeGridBlockerPlacementPolicy.Plan(plannerGrid, plannerPrefabs,
                (int)blockerConfig["blocker_count"], (uint)(int)blockerConfig["random_seed"], gridCellSize,
                (float)blockerConfig["tree_spawn_ratio"], (int)blockerConfig["tree_cluster_count"],
                (int)blockerConfig["tree_cluster_spacing_min_cells"], (int)blockerConfig["tree_cluster_spacing_max_cells"],
                (int)blockerConfig["tree_cluster_distance_min_cells"], (int)blockerConfig["tree_cluster_distance_max_cells"]);
            Require(plan.Count == 1 && repeatedPlan.Count == 1 &&
                    plan[0].PrefabIndex == repeatedPlan[0].PrefabIndex &&
                    plan[0].OriginCell.Equals(repeatedPlan[0].OriginCell) &&
                    plan[0].SizeCells.Equals(repeatedPlan[0].SizeCells),
                "Pinned S073 seed must produce one repeatable synthetic-grid placement");
            RuntimeGridBlockerPlacementData fixture = plan[0];
            Require(fixture.Kind == BlockerPrefabKind.Tree && fixture.PrefabIndex >= 0 && fixture.PrefabIndex < plannerPrefabs.Count &&
                    fixture.OriginCell.x >= 0 && fixture.OriginCell.y >= 0 &&
                    fixture.OriginCell.x + fixture.SizeCells.x <= plannerGrid.Width &&
                    fixture.OriginCell.y + fixture.SizeCells.y <= plannerGrid.Height,
                "S073 synthetic seeded plan must remain inside its explicit open-grid fixture");
            JObject seededPlanFixture = new JObject
            {
                ["status"] = "Passed",
                ["synthetic_grid"] = true,
                ["native_grid_predicates_used"] = false,
                ["native_blocker_instance_observed"] = false,
                ["seed"] = (int)blockerConfig["random_seed"],
                ["repeatable_same_seed"] = true,
                ["prefab"] = plannerPrefabs[fixture.PrefabIndex].Name,
                ["origin_cell"] = new JArray(fixture.OriginCell.x, fixture.OriginCell.y),
                ["footprint"] = new JArray(fixture.SizeCells.x, fixture.SizeCells.y)
            };

            result = new JObject
            {
                ["kind"] = "S073MapPlacementFootprintSmoke",
                ["status"] = "Passed",
                ["definitions"] = definitions.Definitions.Count,
                ["placement_rows"] = rows.Count,
                ["placement_origins_resolved"] = rows.Count,
                ["geometric_center_rows"] = geometricRows,
                ["origin_mode_controls"] = 3,
                ["outside_grid_rows_preserved"] = outsideGridRows,
                ["wall_rows"] = wallRows,
                ["rotated_rows"] = rotatedRows,
                ["blocker_prefabs"] = prefabs.Count,
                ["center_only_blockers"] = centerOnlyCount,
                ["blocker_random_placement_executed"] = false,
                ["seeded_plan_fixture"] = seededPlanFixture,
                ["blocker_footprints"] = blockerFootprints,
                ["grid_cell_size"] = gridCellSize,
                ["wall_footprints"] = JObject.FromObject(wallFootprints),
                ["world_allocated"] = false,
                ["actors_registered"] = false,
                ["mission_initialized"] = false
            };
        }
        catch (Exception error)
        {
            result = new JObject
            {
                ["kind"] = "S073MapPlacementFootprintSmoke",
                ["status"] = "Failed",
                ["error"] = error.ToString(),
                ["world_allocated"] = false,
                ["actors_registered"] = false,
                ["mission_initialized"] = false
            };
        }

        Console.WriteLine("[S073MapPlacementFootprintSmoke] " + result.ToString(Newtonsoft.Json.Formatting.None));
        return (string)result["status"] == "Passed" ? 0 : 1;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    private static string Sha(string path)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }

    private static int ReadInt(string source, string field)
    {
        Match match = Regex.Match(source, @"(?m)^  " + Regex.Escape(field) + @": ([0-9]+)$");
        if (!match.Success) throw new InvalidDataException("Missing selected source grid field " + field);
        return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static float ReadFloat(string source, string field)
    {
        Match match = Regex.Match(source, @"(?m)^  " + Regex.Escape(field) + @": ([^\r\n]+)$");
        if (!match.Success) throw new InvalidDataException("Missing selected source grid field " + field);
        return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static void VerifyOriginModeSemantics(GridConfig grid)
    {
        var footprint = new Vector2Int(2, 3);
        int2 center = new int2(10, 12);
        int2 legacy = MapBuildingPlacementOriginKernel.ResolveOriginCell(center, footprint, grid, false);
        int2 geometric = MapBuildingPlacementOriginKernel.ResolveOriginCell(center, footprint, grid, true);
        if (!legacy.Equals(new int2(10, 11)) || !geometric.Equals(new int2(9, 11)))
            throw new InvalidDataException("Legacy cell-centre and geometric-footprint centring must retain separate source semantics");

        int2 edgeGeometric = MapBuildingPlacementOriginKernel.ResolveOriginCell(new int2(0, 0), footprint, grid, true);
        if (!edgeGeometric.Equals(new int2(-1, -1)))
            throw new InvalidDataException("Geometric-centre origin must preserve the native no-clamp rule at grid edges");
    }
}
