using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Game.Configs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    // Asset-only capture authority. It cannot admit templates, gameplay or native readiness.
    internal sealed class SourceAuthoredUnitMetadataAuthority
    {
        private readonly string path, sha;
        private readonly uint setupHash;
        private readonly int seed;
        private readonly string[] keys;
        private readonly string identitySha, catalogId;
        private readonly Dictionary<string,JObject> assetIdentities;
        private SourceAuthoredUnitMetadataAuthority(string path, string sha, uint setupHash, int seed, string[] keys,
            string identitySha, string catalogId, Dictionary<string,JObject> assetIdentities)
        { this.path=path; this.sha=sha; this.setupHash=setupHash; this.seed=seed; this.keys=keys;
          this.identitySha=identitySha; this.catalogId=catalogId; this.assetIdentities=assetIdentities; }
        internal int UnitCount => keys.Length;

        internal static SourceAuthoredUnitMetadataAuthority FromCapture(SkirmishResolvedSetup setup,
            string metadataPath, string metadataSha, string receiptPath, string receiptSha, string logPath, string repositoryRoot)
        {
            if (setup==null || !SkirmishMissionDispatch.Canonical(setup.CatalogId) ||
                setup.SetupHash!=SkirmishSetupCompiler.ComputeHash(setup,setup.Seed) ||
                !Path.IsPathRooted(repositoryRoot ?? "")) throw new InvalidDataException("Actual compiled source setup required");
            JObject Parse(byte[] bytes) => JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),
                new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
            var receipt=Parse(AutonomousWorld.PinnedFile(receiptPath,receiptSha,1024*1024));
            if ((string)receipt["status"]!="PassedAssetMetadataOnly" || (int?)receipt["wrapper_exit"]!=0 ||
                (string)receipt["metadata_sha256"]!=metadataSha || (string)receipt["catalog_id"]!=setup.CatalogId ||
                (bool?)receipt["native_mission"]!=false || (bool?)receipt["mission_ready"]!=false)
                throw new InvalidDataException("Exact unqualified asset capture receipt required");
            string marker=(string)receipt["marker"], logSha=(string)receipt["log_sha256"];
            var log=System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(logPath,logSha,64*1024*1024));
            if (String.IsNullOrEmpty(marker) || !marker.StartsWith("[SkirmishAuthoredUnitProductionMetadata] result=Passed ",StringComparison.Ordinal) ||
                log.Split(new[]{marker},StringSplitOptions.None).Length!=2)
                throw new InvalidDataException("Exact capture pass marker required once");
            string identitySha=(string)receipt["identity_sha256"],identityMarker=(string)receipt["identity_marker"];
            if ((identitySha==null)!=(identityMarker==null) || identityMarker!=null &&
                (!identityMarker.StartsWith("[SkirmishAuthoredUnitIdentityAudit] result=Passed ",StringComparison.Ordinal) ||
                 log.Split(new[]{identityMarker},StringSplitOptions.None).Length!=2 || (bool?)receipt["runtime_templates_verified"]!=false))
                throw new InvalidDataException("Exact optional independent asset identity marker required");
            var root=Parse(AutonomousWorld.PinnedFile(metadataPath,metadataSha,16*1024*1024));
            if ((string)root["kind"]!="AuthoredSkirmishUnitProductionMetadata" ||
                (string)root["authority"]!="OriginalConfiguredAuthoringFactories_NotMissionState" ||
                (string)root["catalog_id"]!=setup.CatalogId || (uint?)root["setup_hash"]!=setup.SetupHash ||
                (int?)root["seed"]!=setup.Seed || (string)root["size"]!=setup.SizeId.ToString() ||
                (string)root["difficulty"]!=setup.DifficultyId.ToString() ||
                (bool?)root["full_mission_initialized"]!=false || (bool?)root["native_visual_readiness"]!=false ||
                (root["unsupported_values"] as JArray)?.Count!=0)
                throw new InvalidDataException("Selected source capture does not match compiled setup");
            var units=root["units"] as JArray ?? throw new InvalidDataException("Configured source units required");
            var keys=new List<string>();
            foreach (var token in units)
            {
                var row=token as JObject; string key=(string)row?["key"];
                if (row==null || (int?)row["index"]!=keys.Count || String.IsNullOrWhiteSpace(key) || keys.Contains(key,StringComparer.Ordinal) ||
                    (bool?)row["has_definition_metadata"]!=true || (bool?)row["has_production_metadata"]!=true)
                    throw new InvalidDataException("Unique ordered authored source units required");
                keys.Add(key);
            }
            if (keys.Count<1 || keys.Count>128 || (int?)receipt["units"]!=keys.Count)
                throw new InvalidDataException("Bounded actual source unit count required");
            var identities=new Dictionary<string,JObject>(StringComparer.Ordinal);
            void Record(JToken token)
            {
                var identity=token as JObject;string key=(string)identity?["key"];
                if ((string)identity?["codec"]!="source-asset-reference-v1" || (string)identity?["type"]!="UnityEngine.GameObject" || String.IsNullOrWhiteSpace(key))
                    throw new InvalidDataException("Actual source prefab asset identity required");
                if(identities.TryGetValue(key,out var prior) && !JToken.DeepEquals(prior,identity))
                    throw new InvalidDataException("Conflicting source prefab identity");
                identities[key]=(JObject)identity.DeepClone();
            }
            foreach(JObject row in units)Record(row["prefab"]);
            var transports=root["transports"] as JArray ?? throw new InvalidDataException("Authored transport list required");
            foreach(JObject row in transports)Record(row["prefab"]);
            var pins=root["source_pins"] as JObject;
            if (pins==null || pins.Count==0) throw new InvalidDataException("Captured source pins required");
            foreach (var pin in pins.Properties())
            {
                string absolute=Path.GetFullPath(Path.Combine(repositoryRoot,pin.Name));
                if (!absolute.StartsWith(Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.Ordinal))
                    throw new InvalidDataException("Source pin outside repository");
                AutonomousWorld.PinnedFile(absolute,(string)pin.Value,256L*1024*1024);
            }
            return new SourceAuthoredUnitMetadataAuthority(Path.GetFullPath(metadataPath),metadataSha,setup.SetupHash,setup.Seed,keys.ToArray(),identitySha,setup.CatalogId,identities);
        }

        internal Dictionary<string,JObject> ReadIndependentIdentities(string identityPath)
        {
            if(String.IsNullOrEmpty(identitySha))throw new InvalidDataException("Separate captured asset identity readback required");
            var root=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(identityPath,identitySha,4*1024*1024)),
                new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
            if((string)root["kind"]!="AuthoredUnitPrefabIdentityReadback" || (string)root["catalog_id"]!=catalogId ||
                (uint?)root["setup_hash"]!=setupHash || (int?)root["seed"]!=seed || (string)root["metadata_sha256"]!=sha ||
                (int?)root["units"]!=keys.Length || (bool?)root["runtime_templates_verified"]!=false || (bool?)root["mission_ready"]!=false)
                throw new InvalidDataException("Independent asset identity readback context differs");
            var rows=root["identities"] as JArray ?? throw new InvalidDataException("Independent asset identity rows required");
            if(rows.Count!=assetIdentities.Count)throw new InvalidDataException("Independent asset identity domain differs");
            var result=new Dictionary<string,JObject>(StringComparer.Ordinal);
            foreach(JObject row in rows)
            {
                string key=(string)row["key"];
                if(String.IsNullOrEmpty(key) || result.ContainsKey(key) || !assetIdentities.TryGetValue(key,out var actual) ||
                    (string)row["path"]!=(string)actual["path"] || (string)row["guid"]!=(string)actual["guid"] ||
                    (long?)row["local_id"]!=(long?)actual["local_id"])
                    throw new InvalidDataException("Independent asset identity differs: "+key);
                result.Add(key,(JObject)actual.DeepClone());
            }
            return result;
        }

        internal void Require(string actualPath,string actualSha,uint actualSetupHash,int actualSeed,JObject root)
        {
            var units=root["units"] as JArray;
            if (Path.GetFullPath(actualPath)!=path || actualSha!=sha || actualSetupHash!=setupHash || actualSeed!=seed ||
                units==null || !units.Select(row=>(string)row["key"]).SequenceEqual(keys,StringComparer.Ordinal))
                throw new InvalidDataException("Selected source capture authority differs");
        }
    }
}
