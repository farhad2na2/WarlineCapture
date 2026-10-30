using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Runtime;
using Game.Simulation;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

internal static class S073MapPlacementFootprintSmoke
{
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

            var wallFootprints = new Dictionary<string, int>(StringComparer.Ordinal);
            int wallRows = 0, rotatedRows = 0;
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
            }
            Require(prefabNames.Count == 63 && centerOnlyCount > 0,
                "Complete shared blocker prefab metadata domain required");

            result = new JObject
            {
                ["kind"] = "S073MapPlacementFootprintSmoke",
                ["status"] = "Passed",
                ["definitions"] = definitions.Definitions.Count,
                ["placement_rows"] = rows.Count,
                ["wall_rows"] = wallRows,
                ["rotated_rows"] = rotatedRows,
                ["blocker_prefabs"] = prefabs.Count,
                ["center_only_blockers"] = centerOnlyCount,
                ["blocker_random_placement_executed"] = false,
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
}
