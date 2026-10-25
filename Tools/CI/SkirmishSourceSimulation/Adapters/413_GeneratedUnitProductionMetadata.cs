using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // Asset-data materializer. It does not initialize a world, pay, queue, or deliver anything.
    internal sealed class GeneratedUnitProductionMetadata
    {
        internal sealed class Row
        {
            internal readonly int Index;
            internal readonly SourceUnitPrefab Prefab;
            internal readonly BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata Definition;
            internal readonly BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata Production;
            internal readonly JObject AssetIdentity;
            internal bool HasActualTemplate => Prefab.Template != Entity.Null;
            internal Row(int index, SourceUnitPrefab prefab,
                BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata definition,
                BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata production,
                JObject identity)
            { Index = index; Prefab = prefab; Definition = definition; Production = production; AssetIdentity = identity; }
        }

        internal readonly IReadOnlyList<Row> Ordered;
        internal readonly IReadOnlyDictionary<SourceUnitPrefab, JObject> TransportStaticData;
        internal readonly IReadOnlyDictionary<string, string> MetadataReadIdentity;
        internal readonly string ArtifactSha256;
        private readonly Dictionary<SourceUnitPrefab, Row> byPrefab;
        private GeneratedUnitProductionMetadata(List<Row> rows, Dictionary<SourceUnitPrefab, JObject> transports,
            Dictionary<string, string> readIdentity, string sha)
        {
            Ordered = rows.AsReadOnly(); TransportStaticData = transports;
            MetadataReadIdentity = readIdentity; ArtifactSha256 = sha;
            byPrefab = rows.ToDictionary(row => row.Prefab);
        }

        internal bool TryMetadata(SourceUnitPrefab prefab,
            out BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata metadata)
        {
            if (prefab != null && byPrefab.TryGetValue(prefab, out Row row))
            { metadata = row.Production; return true; }
            metadata = default; return false;
        }

        internal Row RequireDeliverable(SourceUnitPrefab prefab)
        {
            if (prefab == null || !byPrefab.TryGetValue(prefab, out Row row))
                throw new NotSupportedException("Unknown source configured unit identity");
            if (!row.HasActualTemplate)
                throw new NotSupportedException("Actual ECS template has not been baked/imported: " + prefab.name);
            return row;
        }

        internal static GeneratedUnitProductionMetadata Load(string path, string sha,
            uint setupHash, int seed, string repoRoot,
            IDictionary<string, SourceUnitPrefab> canonicalAssets,
            IDictionary<string, Entity> actualTemplates, IDictionary<string,JObject> canonicalIdentities, SourceMetadataCompatibility compatibility=null)
        {
            if (canonicalAssets == null || canonicalIdentities == null || actualTemplates == null || string.IsNullOrEmpty(repoRoot))
                throw new ArgumentException("Canonical source identities/templates and source root required");
            byte[] bytes = AutonomousWorld.PinnedFile(path, sha, 2 * 1024 * 1024);
            JObject root = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if(compatibility!=null){compatibility.RequireTarget(setupHash,seed);compatibility.RequireUnits(sha,root);}
            if ((int?)root["schema_version"] != 1 || (string)root["kind"] != "AuthoredSkirmishUnitProductionMetadata" ||
                (string)root["authority"] != "OriginalConfiguredAuthoringFactories_NotMissionState" ||
                (compatibility==null&&((uint?)root["setup_hash"] != setupHash || (int?)root["seed"] != seed)) ||
                (root["unsupported_values"] as JArray)?.Count != 0 ||
                (bool?)root["baked_actor_footprint_substituted"] != false ||
                (bool?)root["runtime_camera_basis_exported"] != false)
                throw new InvalidDataException("Setup-bound complete original unit metadata required");

            // Only authoring/getter normalization inputs are operational cache guards.
            // All other generation source hashes remain provenance, not per-gameplay-fix rebake gates.
            string[] metadataSources = {
                "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs",
                "Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs",
                "Assets/Game/Scripts/Composition/BuildingDefinitionUnitAuthoringMetadata.cs",
                "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs",
                "Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs"
            };
            JObject sourcePins = root["source_pins"] as JObject ?? throw new InvalidDataException("Metadata source identity missing");
            var readIdentity = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string rel in metadataSources)
            {
                string expected = (string)sourcePins[rel];
                if (expected == null || AutonomousWorld.Hash(File.ReadAllBytes(Path.Combine(repoRoot, rel))) != expected)
                    throw new InvalidDataException("Relevant metadata getter/factory source changed: " + rel);
                readIdentity.Add(rel, expected);
            }
            var stagedAssets = new Dictionary<string, SourceUnitPrefab>(canonicalAssets, StringComparer.Ordinal);
            var assetIdentities = canonicalIdentities.ToDictionary(pair=>pair.Key,pair=>(JObject)pair.Value.DeepClone(),StringComparer.Ordinal);
            bool releasedReference=string.Equals(sha,"e66d620a89f991daaedadb71dde8e6a88d63ac98be1f291a7d6344f98013cf21",StringComparison.Ordinal)||string.Equals(sha,"8eafcc82034c2857eb050c46c7de7e8f0bfa70bdbc505efe579ff2807700e74d",StringComparison.Ordinal);
            var codec = new ExactInitialValueCodec(Path.GetDirectoryName(path), new JObject());
            JArray units = root["units"] as JArray;
            JArray transportRows = root["transports"] as JArray;
            if (units == null || units.Count != 12 || transportRows == null || transportRows.Count > 16)
                throw new InvalidDataException("Bounded complete original configured unit order required");
            var rows = new List<Row>();
            foreach (JObject unit in units)
            {
                if ((int?)unit["index"] != rows.Count || (bool?)unit["has_definition_metadata"] != true ||
                    (bool?)unit["has_production_metadata"] != true)
                    throw new InvalidDataException("Actual contiguous configured unit order and metadata presence required");
                JObject asset = unit["prefab"] as JObject ?? throw new InvalidDataException("Unit asset identity missing");
                SourceUnitPrefab prefab = ResolveAsset(asset, stagedAssets, actualTemplates, assetIdentities, releasedReference);
                if ((string)unit["key"] != prefab.name || rows.Any(prior => ReferenceEquals(prior.Prefab, prefab)))
                    throw new InvalidDataException("Unique configured identity/key required");
                JObject definition = Fields(unit["definition_metadata"],
                    "Game.Runtime.BuildingDefinitionPrefabSystemHelper+UnitDefinitionMetadata",
                    typeof(BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata));
                var d = new BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata();
                foreach (FieldInfo field in typeof(BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata).GetFields())
                {
                    object boxed = d;
                    field.SetValue(boxed, codec.Decode(definition[field.Name], field.FieldType, prefab.name + "/definition/" + field.Name));
                    d = (BuildingDefinitionPrefabSystemHelper.UnitDefinitionMetadata)boxed;
                }
                JObject p = Fields(unit["production_metadata"],
                    "Game.Runtime.BuildingProductionQueueCompositionSystemHelper+UnitProductionMetadata",
                    typeof(BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata));
                Func<string, float> f = name => (float)codec.Decode(p[name], typeof(float), prefab.name + "/" + name);
                Func<string, bool> b = name => (bool)codec.Decode(p[name], typeof(bool), prefab.name + "/" + name);
                SourceUnitPrefab transport = (string)p["ProductionTransportPrefab"]?["codec"] == "null-v1" ? null :
                    ResolveAsset((JObject)p["ProductionTransportPrefab"], stagedAssets, actualTemplates, assetIdentities, releasedReference);
                var metadata = new BuildingProductionQueueCompositionSystemHelper.UnitProductionMetadata(
                    f("ProductionDurationSeconds"), transport, b("IsAirUnit"), f("ProductionTransportArrivalSeconds"),
                    f("ProductionTransportHoldForNextReadySeconds"),
                    (int)codec.Decode(p["ProductionTransportMaxConcurrent"], typeof(int), prefab.name + "/concurrent"),
                    b("ProductionTransportRequiresAirportRunway"), b("ProductionTransportUsesRunwayLanding"),
                    (Vector2Int)codec.Decode(p["FootprintCells"], typeof(Vector2Int), prefab.name + "/configured-footprint"));
                if (float.IsNaN(metadata.ProductionDurationSeconds) || float.IsInfinity(metadata.ProductionDurationSeconds) ||
                    float.IsNaN(metadata.ProductionTransportArrivalSeconds) || float.IsInfinity(metadata.ProductionTransportArrivalSeconds) ||
                    float.IsNaN(metadata.ProductionTransportHoldForNextReadySeconds) || float.IsInfinity(metadata.ProductionTransportHoldForNextReadySeconds) ||
                    metadata.ProductionDurationSeconds < 0.01f || metadata.ProductionTransportArrivalSeconds < 0.01f ||
                    metadata.ProductionTransportHoldForNextReadySeconds < 0.01f ||
                    metadata.FootprintCells.x < 1 || metadata.FootprintCells.y < 1 || metadata.ProductionTransportMaxConcurrent < 1)
                    throw new InvalidDataException("Invalid native normalized metadata");
                rows.Add(new Row(rows.Count, prefab, d, metadata, (JObject)asset.DeepClone()));
            }
            var transports = new Dictionary<SourceUnitPrefab, JObject>();
            foreach (JObject transport in transportRows)
            {
                var token = ResolveAsset((JObject)transport["prefab"], stagedAssets, actualTemplates, assetIdentities, releasedReference);
                JArray nodes = transport["nodes"] as JArray;
                if (nodes == null || nodes.Count < 1 || nodes.Count > 128 || transports.ContainsKey(token))
                    throw new InvalidDataException("Bounded unique authored transport hierarchy required");
                transports.Add(token, (JObject)transport.DeepClone());
            }
            foreach (Row row in rows)
                if (row.Production.ProductionTransportPrefab != null && !transports.ContainsKey(row.Production.ProductionTransportPrefab))
                    throw new InvalidDataException("Referenced transport static hierarchy missing");
            // Commit token aliases only after complete validation. No EM or bank changes occur here.
            foreach (var pair in stagedAssets)
                if (!canonicalAssets.ContainsKey(pair.Key)) canonicalAssets.Add(pair.Key, pair.Value);
            foreach(var pair in assetIdentities)if(!canonicalIdentities.ContainsKey(pair.Key))canonicalIdentities.Add(pair.Key,(JObject)pair.Value.DeepClone());
            return new GeneratedUnitProductionMetadata(rows, transports, readIdentity, sha);
        }

        private static JObject Fields(JToken envelope, string typeName, Type type)
        {
            JObject fields = envelope?["fields"] as JObject;
            string[] expected = type.GetFields(BindingFlags.Instance | BindingFlags.Public).Select(field => field.Name).OrderBy(name => name).ToArray();
            if ((string)envelope?["type"] != typeName || fields == null ||
                !fields.Properties().Select(field => field.Name).OrderBy(name => name).SequenceEqual(expected))
                throw new InvalidDataException("Exact current metadata field schema required: " + typeName);
            return fields;
        }

        private static SourceUnitPrefab ResolveAsset(JObject identity, Dictionary<string, SourceUnitPrefab> assets,
            IDictionary<string, Entity> templates, Dictionary<string, JObject> identities, bool releasedReference)
        {
            if ((string)identity?["codec"] != "source-asset-reference-v1" || (string)identity["type"] != "UnityEngine.GameObject")
                throw new InvalidDataException("Exact authored prefab asset identity required");
            string key = (string)identity["key"], path = (string)identity["path"], guid = (string)identity["guid"];
            long id = (long?)identity["local_id"] ?? 0;
            // Existing strict token validator verifies the full authored path/GUID/local-ID domain.
            SourceUnitPrefab identityToken = SourceUnitPrefab.FromAssetIdentity(key, path, guid, id);
            if(!identities.ContainsKey(key)&&!releasedReference)throw new InvalidDataException("No independent prior identity for unqualified metadata asset: "+key);
            if (identities.TryGetValue(key, out JObject priorIdentity) && !JToken.DeepEquals(priorIdentity, identity))
                throw new InvalidDataException("Conflicting asset identity for source key " + key);
            identities[key] = (JObject)identity.DeepClone();
            if (assets.TryGetValue(key, out SourceUnitPrefab prior))
            {
                if (prior.name != key || (prior.SourceAssetPath != null &&
                    (prior.SourceAssetPath != path || prior.SourceAssetGuid != guid || prior.SourceAssetLocalId != id)))
                    throw new InvalidDataException("Canonical asset identity mismatch: " + key);
                if (prior.Template != Entity.Null && (!templates.TryGetValue(key, out Entity actual) || prior.Template != actual))
                    throw new InvalidDataException("Canonical actual template mismatch: " + key);
                return prior;
            }
            SourceUnitPrefab result = templates.TryGetValue(key, out Entity template) ? new SourceUnitPrefab(key, template) : identityToken;
            assets.Add(key, result); return result;
        }
    }
}
