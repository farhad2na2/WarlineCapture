using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    internal sealed class S073ConfiguredUnitRegistryEvidence
    {
        private static readonly string[] ExpectedKeys =
        {
            "Unit_Chr_Soldier_Male_02_Alt_04",
            "Unit_Veh_Truck_Tray",
            "Unit_Veh_Truck_Tanker"
        };

        private static readonly string[] FactoryCodePaths =
        {
            "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
            "Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs",
            "Assets/Game/Scripts/Composition/BuildingDefinitionUnitAuthoringMetadata.cs",
            "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
            "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs"
        };

        internal readonly string FactoryMetadataPath;
        internal readonly string FactoryMetadataSha;
        internal readonly IReadOnlyList<string> OrderedUnitKeys;
        internal readonly IReadOnlyDictionary<string, JObject> AssetIdentities;

        private S073ConfiguredUnitRegistryEvidence(string metadataPath, string metadataSha, List<string> keys,
            Dictionary<string, JObject> identities)
        {
            FactoryMetadataPath = metadataPath;
            FactoryMetadataSha = metadataSha;
            OrderedUnitKeys = keys.AsReadOnly();
            AssetIdentities = identities;
        }

        internal static S073ConfiguredUnitRegistryEvidence Load(string path, string sha, string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(repoRoot)) throw new ArgumentException("S073 source root required");
            JObject evidence = Parse(path, sha, 4 * 1024 * 1024);
            if ((int?)evidence["schema_version"] != 1 ||
                (string)evidence["kind"] != "S073OriginalConfiguredUnitRegistryEvidence" ||
                (string)evidence["mission"] != "S073" ||
                (string)evidence["authority"] != "SkirmishIndustrialBasin preset -> original Construction asset -> UnitRegistry asset -> original prefab references; rows independently matched to captured original unit factories" ||
                (string)evidence["scope"] != "Source config and authoring metadata only; no World, production tick, outcome, or readiness" ||
                (int?)evidence["configured_count"] != ExpectedKeys.Length ||
                (bool?)evidence["source_factory_order_matches_registry_indices"] != true ||
                (bool?)evidence["all_current_source_pins_match"] != true)
                throw new InvalidDataException("Exact source-only S073 configured-unit registry evidence required");

            string presetPath = "Assets/Game/Resources/SkirmishIndustrialBasin.asset";
            string constructionPath = "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset";
            string registryPath = "Assets/Game/Configs/Skirmish/UnitRegistry.asset";
            RequirePinnedSource(repoRoot, presetPath, (string)evidence["preset_sha256"]);
            RequirePinnedSource(repoRoot, constructionPath, (string)evidence["construction_sha256"]);
            RequirePinnedSource(repoRoot, registryPath, (string)evidence["registry_sha256"]);
            string constructionGuid = ReadMetaGuid(repoRoot, constructionPath + ".meta");
            string registryGuid = ReadMetaGuid(repoRoot, registryPath + ".meta");
            if (constructionGuid != (string)evidence["construction_guid"] ||
                registryGuid != (string)evidence["registry_guid"] ||
                registryGuid != "a4f4b7c4b57f54c968032aca1c24f41d")
                throw new InvalidDataException("Current S073 source asset GUID chain changed");

            string presetText = ReadRepoText(repoRoot, presetPath);
            string constructionText = ReadRepoText(repoRoot, constructionPath);
            if (!ContainsGuidReference(presetText, "buildingPlacement", constructionGuid) ||
                !ContainsGuidReference(constructionText, "unitPrefabRegistryConfig", registryGuid))
                throw new InvalidDataException("S073 preset-to-construction-to-unit-registry source chain changed");

            JArray configured = evidence["configured_units"] as JArray ?? throw new InvalidDataException("S073 configured unit rows missing");
            string registryText = ReadRepoText(repoRoot, registryPath);
            Match section = Regex.Match(registryText, "(?ms)^  unitSpawnPrefabs:\\s*\\r?\\n(?<body>.*?)(?=^  [A-Za-z][A-Za-z0-9_]*:|\\z)");
            if (!section.Success || !Regex.IsMatch(registryText, "(?m)^  m_Name: UnitRegistry\\s*$"))
                throw new InvalidDataException("Original S073 UnitRegistry source list missing");
            MatchCollection refs = Regex.Matches(section.Groups["body"].Value,
                @"(?m)^\s*-\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]{32}),\s*type:\s*\d+\}");
            if (refs.Count != ExpectedKeys.Length || configured.Count != ExpectedKeys.Length)
                throw new InvalidDataException("Exact three-unit S073 configured registry required");

            string factoryPath = (string)evidence["factory_metadata_artifact_path"];
            string factorySha = (string)evidence["factory_metadata_artifact_sha256"];
            JObject factory = Parse(factoryPath, factorySha, 2 * 1024 * 1024);
            if ((int?)factory["schema_version"] != 2 ||
                (string)factory["kind"] != "NativeQualifiedSkirmishUnitProductionMetadata" ||
                (string)factory["authority"] != "OriginalNativeMetadataFactories_NotMissionState" ||
                (factory["unsupported_values"] as JArray)?.Count != 0 ||
                (bool?)factory["baked_actor_footprint_substituted"] != false ||
                (bool?)factory["runtime_camera_basis_exported"] != false)
                throw new InvalidDataException("Complete source-factory metadata artifact required");

            JObject currentCodePins = evidence["factory_code_pins"] as JObject ?? throw new InvalidDataException("S073 factory code pins missing");
            JObject factoryPins = factory["source_pins"] as JObject ?? throw new InvalidDataException("Original source-factory pins missing");
            foreach (string codePath in FactoryCodePaths)
            {
                string current = Hash(ReadRepoBytes(repoRoot, codePath));
                if ((string)currentCodePins[codePath] != current || (string)factoryPins[codePath] != current)
                    throw new InvalidDataException("Current original unit getter/factory source changed: " + codePath);
            }

            JArray factoryRows = factory["units"] as JArray ?? throw new InvalidDataException("Original unit factory rows missing");
            var rowsByKey = factoryRows.OfType<JObject>().ToDictionary(row => (string)row["key"], StringComparer.Ordinal);
            var keys = new List<string>(ExpectedKeys.Length);
            var identities = new Dictionary<string, JObject>(StringComparer.Ordinal);
            for (int i = 0; i < ExpectedKeys.Length; i++)
            {
                JObject row = configured[i] as JObject ?? throw new InvalidDataException("S073 configured unit identity row missing");
                string key = ExpectedKeys[i];
                string rel = (string)row["path"];
                string guid = (string)row["guid"];
                long localId = (long?)row["local_id"] ?? 0;
                if ((int?)row["index"] != i || (string)row["key"] != key ||
                    long.Parse(refs[i].Groups[1].Value) != localId || refs[i].Groups[2].Value != guid ||
                    string.IsNullOrWhiteSpace(rel) || Path.IsPathRooted(rel) || rel.Split('/').Any(part => part == "..") ||
                    Path.GetFileNameWithoutExtension(rel) != key ||
                    ReadMetaGuid(repoRoot, rel + ".meta") != guid)
                    throw new InvalidDataException("S073 registry order or prefab identity changed: " + key);
                RequirePinnedSource(repoRoot, rel, (string)row["prefab_sha256"]);
                RequirePinnedSource(repoRoot, rel + ".meta", (string)row["meta_sha256"]);
                if ((string)factoryPins[rel] != (string)row["prefab_sha256"] ||
                    (string)factoryPins[rel + ".meta"] != (string)row["meta_sha256"] ||
                    !rowsByKey.TryGetValue(key, out JObject factoryRow) ||
                    !JToken.DeepEquals(factoryRow, row["factory_row"]) ||
                    (string)factoryRow["prefab"]?["path"] != rel ||
                    (string)factoryRow["prefab"]?["guid"] != guid ||
                    (long?)factoryRow["prefab"]?["local_id"] != localId)
                    throw new InvalidDataException("S073 configured prefab does not match its original factory row: " + key);
                keys.Add(key);
                identities.Add(key, (JObject)factoryRow["prefab"].DeepClone());
            }

            JObject transportIdentity = rowsByKey[ExpectedKeys[0]]["production_metadata"]?["fields"]?["ProductionTransportPrefab"] as JObject;
            string transportPath = (string)transportIdentity?["path"];
            if (string.IsNullOrWhiteSpace(transportPath))
                throw new InvalidDataException("Original infantry production transport identity missing");
            if ((string)factoryPins[transportPath] != Hash(ReadRepoBytes(repoRoot, transportPath)) ||
                (string)factoryPins[transportPath + ".meta"] != Hash(ReadRepoBytes(repoRoot, transportPath + ".meta")))
                throw new InvalidDataException("Original production transport prefab source changed");
            string transportKey = Path.GetFileNameWithoutExtension(transportPath);
            if (string.IsNullOrWhiteSpace(transportKey) || identities.ContainsKey(transportKey))
                throw new InvalidDataException("Original production transport identity is not distinct");
            identities.Add(transportKey, (JObject)transportIdentity.DeepClone());
            return new S073ConfiguredUnitRegistryEvidence(factoryPath, factorySha, keys, identities);
        }

        private static JObject Parse(string path, string sha, int maxBytes)
        {
            byte[] bytes = AutonomousWorld.PinnedFile(path, sha, maxBytes);
            return JObject.Parse(Encoding.UTF8.GetString(bytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
        }

        private static string ReadMetaGuid(string repoRoot, string rel)
        {
            Match match = Regex.Match(ReadRepoText(repoRoot, rel), "(?m)^guid:\\s*([0-9a-f]{32})\\s*$");
            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static bool ContainsGuidReference(string text, string field, string guid) =>
            Regex.IsMatch(text, "(?m)^  " + Regex.Escape(field) + @": \{fileID: [^,]+, guid: " + Regex.Escape(guid) + @", type: \d+\}$");

        private static void RequirePinnedSource(string repoRoot, string rel, string expected)
        {
            if (string.IsNullOrWhiteSpace(expected) || Hash(ReadRepoBytes(repoRoot, rel)) != expected)
                throw new InvalidDataException("Current source asset hash changed: " + rel);
        }

        private static string ReadRepoText(string repoRoot, string rel) => Encoding.UTF8.GetString(ReadRepoBytes(repoRoot, rel));
        private static byte[] ReadRepoBytes(string repoRoot, string rel) => File.ReadAllBytes(Path.Combine(repoRoot, rel));
        private static string Hash(byte[] bytes)
        {
            using (SHA256 hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
