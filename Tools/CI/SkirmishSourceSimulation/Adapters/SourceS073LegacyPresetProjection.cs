using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game.Simulation;
using Game.Runtime;
using Unity.Mathematics;
using Newtonsoft.Json.Linq;

namespace Game.Configs
{
    public sealed class SkirmishPresetLegacyBuildingPlacementProjection
    {
        public SkirmishPresetLegacyInitialUnitsProjection InitialUnitsConfig;
        internal IReadOnlyList<LogicalCatalogPrefab> Spawnables;
        internal SourceCatalogRegistry UnitPrefabRegistryConfig;
    }

    public sealed class SkirmishPresetLegacyInitialUnitsProjection
    {
        public int InitialMaterials;
        public int MaterialsCapacity;
        public int InitialAiMaterials;
        public int AiMaterialsCapacity;
        public List<SkirmishPresetLegacyFactionProjection> Factions = new();
    }

    public sealed class SkirmishPresetLegacyFactionProjection
    {
        public int FactionId;
        public int2 SpawnCell;
    }

    // Data-only bridge for the current original S073 legacy preset. The input
    // capture and full-config readback are hash-checked by the harness before
    // this projection is installed. Unsupported presets fail closed.
    internal static class SourceS073LegacyPresetProjection
    {
        private static SkirmishPresetConfig selected;

        internal static bool ReadBuildingCanRequest(
            string sourceRoot,
            string expectedPrefabKey,
            JObject sourceAssetIdentity,
            out JObject evidence)
        {
            if (string.IsNullOrWhiteSpace(sourceRoot) || string.IsNullOrWhiteSpace(expectedPrefabKey) || sourceAssetIdentity == null)
                throw new ArgumentException("Exact source prefab identity required");

            string relativePrefabPath = (string)sourceAssetIdentity["path"];
            string expectedPrefabGuid = (string)sourceAssetIdentity["guid"];
            if (string.IsNullOrWhiteSpace(relativePrefabPath) || !relativePrefabPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                expectedPrefabGuid == null || expectedPrefabGuid.Length != 32)
                throw new InvalidDataException("Complete source prefab asset identity required");

            string fullRoot = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string prefabPath = Path.GetFullPath(Path.Combine(sourceRoot, relativePrefabPath.Replace('/', Path.DirectorySeparatorChar)));
            if (!prefabPath.StartsWith(fullRoot, StringComparison.Ordinal) || !File.Exists(prefabPath))
                throw new InvalidDataException("Source prefab path escaped the selected project or is missing");
            string prefabMetaPath = prefabPath + ".meta";
            string prefabMeta = File.ReadAllText(prefabMetaPath);
            Match prefabMetaGuid = Regex.Match(prefabMeta, @"(?m)^guid: ([0-9a-f]{32})$");
            if (!prefabMetaGuid.Success || prefabMetaGuid.Groups[1].Value != expectedPrefabGuid)
                throw new InvalidDataException("Source prefab GUID differs from captured source identity: " + expectedPrefabKey);

            string prefabYaml = File.ReadAllText(prefabPath);
            var configReferenceMatches = Regex.Matches(prefabYaml,
                @"(?m)^\s*propertyPath: config\s*\r?\n\s*value:.*\r?\n\s*objectReference: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}$");
            if (configReferenceMatches.Count != 1)
                throw new InvalidDataException("Source prefab must have one unambiguous authored building config reference: " + expectedPrefabKey);
            string configGuid = configReferenceMatches[0].Groups[1].Value;

            string configRoot = Path.Combine(sourceRoot, "Assets", "Game", "Configs", "Prefabs");
            string[] matchingMetaPaths = Directory.GetFiles(configRoot, "*.asset.meta", SearchOption.AllDirectories)
                .Where(path => Regex.IsMatch(File.ReadAllText(path), @"(?m)^guid: " + Regex.Escape(configGuid) + @"$"))
                .ToArray();
            if (matchingMetaPaths.Length != 1)
                throw new InvalidDataException("Building config GUID must resolve to exactly one authored config asset: " + expectedPrefabKey);
            string configMetaPath = matchingMetaPaths[0];
            string configPath = configMetaPath.Substring(0, configMetaPath.Length - ".meta".Length);
            string configYaml = File.ReadAllText(configPath);
            Match scriptGuid = Regex.Match(configYaml,
                @"(?m)^\s*m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}$");
            if (!scriptGuid.Success || scriptGuid.Groups[1].Value != "a2010000000000000000000000000004")
                throw new InvalidDataException("Resolved config is not a BuildingDefinitionAuthoringConfig: " + expectedPrefabKey);
            MatchCollection canRequestRows = Regex.Matches(configYaml, @"(?m)^\s*canRequest: ([01])\s*$");
            if (canRequestRows.Count > 1)
                throw new InvalidDataException("Building config contains duplicate canRequest values: " + expectedPrefabKey);
            bool canRequest;
            string defaultSourceHash = null;
            if (canRequestRows.Count == 1)
            {
                canRequest = canRequestRows[0].Groups[1].Value == "1";
            }
            else
            {
                string defaultsSourcePath = Path.Combine(sourceRoot, "Assets", "Game", "Scripts", "Configs", "GameplayConfigModels.cs");
                string defaultsSource = File.ReadAllText(defaultsSourcePath);
                Match defaultField = Regex.Match(defaultsSource,
                    @"(?m)\bprivate\s+bool\s+canRequest\s*=\s*(true|false)\s*;");
                if (!defaultField.Success)
                    throw new InvalidDataException("Omitted canRequest must have one pinned authored C# field default");
                canRequest = defaultField.Groups[1].Value == "true";
                defaultSourceHash = Sha256(defaultsSourcePath);
            }

            evidence = new JObject
            {
                ["prefab_key"] = expectedPrefabKey,
                ["prefab_path"] = relativePrefabPath,
                ["prefab_guid"] = expectedPrefabGuid,
                ["prefab_sha256"] = Sha256(prefabPath),
                ["prefab_meta_sha256"] = Sha256(prefabMetaPath),
                ["config_path"] = Path.GetRelativePath(sourceRoot, configPath).Replace(Path.DirectorySeparatorChar, '/'),
                ["config_guid"] = configGuid,
                ["config_sha256"] = Sha256(configPath),
                ["config_meta_sha256"] = Sha256(configMetaPath),
                ["can_request"] = canRequest,
                ["can_request_serialized"] = canRequestRows.Count == 1,
                ["can_request_default_source"] = defaultSourceHash == null ? null : "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
                ["can_request_default_source_sha256"] = defaultSourceHash
            };
            return canRequest;
        }

        private static string Sha256(string path)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }

        internal static void Install(
            int scenarioIndex,
            JObject presetFields,
            LegacyInitialSpawnData initial)
        {
            if (scenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex || presetFields == null || initial == null ||
                initial.Factions == null || initial.Factions.Length != 2)
                throw new InvalidOperationException("Exact source-backed S073 preset fields required");

            var initialProjection = new SkirmishPresetLegacyInitialUnitsProjection
            {
                InitialMaterials = initial.InitialMaterials,
                MaterialsCapacity = initial.MaterialsCapacity,
                InitialAiMaterials = initial.InitialAiMaterials,
                AiMaterialsCapacity = initial.AiMaterialsCapacity
            };
            foreach (LegacyInitialFactionData faction in initial.Factions)
            {
                if (faction == null || faction.FactionId is not (1 or 2))
                    throw new InvalidOperationException("Exact source-backed S073 factions required");
                initialProjection.Factions.Add(new SkirmishPresetLegacyFactionProjection
                {
                    FactionId = faction.FactionId,
                    SpawnCell = new int2(faction.SpawnX, faction.SpawnY)
                });
            }

            selected = new SkirmishPresetConfig
            {
                reinforcementInfantryTarget = RequiredInt(presetFields, "reinforcementInfantryTarget"),
                attackSquadSize = RequiredInt(presetFields, "attackSquadSize"),
                rifleRange = RequiredFloat(presetFields, "rifleRange"),
                rifleDamage = RequiredInt(presetFields, "rifleDamage"),
                rifleCooldown = RequiredFloat(presetFields, "rifleCooldown"),
                armoredCarRange = RequiredFloat(presetFields, "armoredCarRange"),
                armoredCarDamage = RequiredInt(presetFields, "armoredCarDamage"),
                armoredCarCooldown = RequiredFloat(presetFields, "armoredCarCooldown"),
                watchtowerRange = RequiredFloat(presetFields, "watchtowerRange"),
                watchtowerDamage = RequiredInt(presetFields, "watchtowerDamage"),
                watchtowerCooldown = RequiredFloat(presetFields, "watchtowerCooldown"),
                firstAttackSeconds = RequiredFloat(presetFields, "firstAttackSeconds"),
                playerReinforcementRallyOffset = ReadOffset(presetFields),
                baseDefenseRadius = RequiredFloat(presetFields, "baseDefenseRadius"),
                buildingPlacement = new SkirmishPresetLegacyBuildingPlacementProjection
                {
                    InitialUnitsConfig = initialProjection
                }
            };
        }

        private static int RequiredInt(JObject fields, string name) =>
            fields[name]?.Type == JTokenType.Integer ? (int)fields[name] : throw new InvalidOperationException("Missing original preset integer " + name);
        private static float RequiredFloat(JObject fields, string name) =>
            fields[name]?.Type is JTokenType.Integer or JTokenType.Float ? (float)fields[name] : throw new InvalidOperationException("Missing original preset number " + name);
        private static float3 ReadOffset(JObject fields)
        {
            JObject offset = fields["playerReinforcementRallyOffset"] as JObject ?? throw new InvalidOperationException("Missing original rally offset");
            return new float3(RequiredFloat(offset, "x"), RequiredFloat(offset, "y"), RequiredFloat(offset, "z"));
        }

        internal static SkirmishPresetConfig Load(int scenarioIndex)
        {
            if (scenarioIndex != SkirmishPresetConfig.IndustrialBasinScenarioIndex || selected == null)
                throw new NotSupportedException("Only the installed source-backed S073 legacy preset is supported");
            return selected;
        }
    }
}
