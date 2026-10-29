using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Game.Runtime
{
    internal sealed partial class AutonomousWorld
    {
        internal GeneratedUnitProductionMetadata ActualUnitMetadata { get; private set; }

        internal JObject BindActualUnitMetadata(string path, string sha256, string repositoryRoot)
        {
            if (ActualUnitMetadata != null)
                throw new InvalidOperationException("Source unit metadata binds once per fresh world");
            if (SourceDefinitions == null || Setup == null || StorageBank == null)
                throw new InvalidOperationException("Actual source startup and canonical bank must exist first");

            var templates = SourceDefinitions.Assets
                .Where(pair => pair.Value.Template != Unity.Entities.Entity.Null)
                .ToDictionary(pair => pair.Key, pair => pair.Value.Template, StringComparer.Ordinal);
            ActualUnitMetadata = GeneratedUnitProductionMetadata.Load(
                path, sha256, Setup.SetupHash, Setup.Seed, repositoryRoot,
                SourceDefinitions.Assets, templates, SourceDefinitions.AssetIdentities,MetadataCompatibility);

            return new JObject
            {
                ["world_id"] = WorldId,
                ["tick"] = Tick,
                ["metadata_artifact_sha256"] = ActualUnitMetadata.ArtifactSha256,
                ["configured_unit_count"] = ActualUnitMetadata.Ordered.Count,
                ["actual_template_count"] = ActualUnitMetadata.Ordered.Count(row => row.HasActualTemplate),
                ["missing_templates"] = new JArray(ActualUnitMetadata.Ordered
                    .Where(row => !row.HasActualTemplate).Select(row => row.Prefab.name)),
                ["paid_owner_bound"] = false,
                ["full_mission"] = false
            };
        }

        // The original focused template, the original factory metadata, and the independently pinned
        // registry/asset records must all name the same transport before it gets a logical identity.
        internal void BindS002FocusedTransportIdentity(JObject reference,JObject unitDescriptor,System.Collections.Generic.IDictionary<string,Unity.Entities.Entity> actualTemplates,string sourceRoot)
        {
            if(Setup.CatalogId!="S002"||MetadataCompatibility?.FactoryDomainQualified!=true||(string)reference["source_reference_sha256"]!="fa8933a86f1ada977e9f10b53a0af770c79b43e8c73da49da385a192985f8969")return;
            var native=JObject.Parse(System.Text.Encoding.UTF8.GetString(PinnedFile((string)reference["source_reference_path"],(string)reference["source_reference_sha256"],16*1024*1024)));var projection=JObject.Parse(System.Text.Encoding.UTF8.GetString(Descriptor(unitDescriptor,4*1024*1024)));MetadataCompatibility.RequireUnits((string)unitDescriptor["sha256"],projection);
            var rows=projection["transports"] as JArray;if(rows?.Count!=1)throw new System.IO.InvalidDataException("Exact S002 factory transport domain required");var identity=rows[0]["prefab"] as JObject??throw new System.IO.InvalidDataException("Actual transport identity required");string key=(string)identity["key"],asset=(string)identity["path"],guid=(string)identity["guid"];long local=(long?)identity["local_id"]??0;
            if(key!="Unit_Veh_Helicopter_Transport"||asset!="Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab"||guid==null||guid.Length!=32||local==0||!actualTemplates.TryGetValue(key,out var template)||template==Unity.Entities.Entity.Null||(string)native["opening_prefabs"]?[key]==null||!((JArray)native["requested_prefab_keys"]).Values<string>().Contains(key))throw new System.IO.InvalidDataException("Actual S002 focused transport template required");
            string registry="Assets/Game/Configs/Scene/Game_UnitPrefabRegistry_Config.asset";string ReadPinned(string rel){string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(sourceRoot,rel));if(!path.StartsWith(System.IO.Path.GetFullPath(sourceRoot)+System.IO.Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new System.IO.InvalidDataException("Registry source escaped root");string hash=(string)native["source_pins"]?[rel];if(hash==null)throw new System.IO.InvalidDataException("Native focused source pin missing: "+rel);return System.Text.Encoding.UTF8.GetString(PinnedFile(path,hash,4*1024*1024));}
            ReadPinned(asset);string meta=ReadPinned(asset+".meta");string registryText=ReadPinned(registry);ReadPinned(registry+".meta");RequireS002TransportAssetIdentity(guid,local,meta,registryText);
            if(SourceDefinitions.AssetIdentities.TryGetValue(key,out var prior)&&!JToken.DeepEquals(prior,identity))throw new System.IO.InvalidDataException("Conflicting original transport identity");SourceDefinitions.AssetIdentities[key]=(JObject)identity.DeepClone();if(SourceDefinitions.Assets.TryGetValue(key,out var existing)){if(existing.Template!=template)throw new System.IO.InvalidDataException("Conflicting focused transport template");}else SourceDefinitions.Assets.Add(key,new SourceUnitPrefab(key,template));
        }

        internal static void RequireS002TransportAssetIdentity(string guid,long local,string meta,string registryText)
        {
            var gm=System.Text.RegularExpressions.Regex.Match(meta??"",@"(?m)^guid:\s*([0-9a-f]{32})\s*$");if(!gm.Success||gm.Groups[1].Value!=guid)throw new System.IO.InvalidDataException("Actual prefab GUID mismatch");string expected="{fileID: "+local.ToString(System.Globalization.CultureInfo.InvariantCulture)+", guid: "+guid+", type: 3}";if(local==0||registryText==null||registryText.Split(new[]{expected},StringSplitOptions.None).Length!=2)throw new System.IO.InvalidDataException("Actual registry GUID/local ID mismatch or ambiguity");
        }

        // The paid adapter calls this before the original paid owner. Unknown template
        // support must fail before source slot reservation, payment or queue mutation.
        internal GeneratedUnitProductionMetadata.Row RequireActualProductionUnit(SourceUnitPrefab prefab)
        {
            if (ActualUnitMetadata == null)
                throw new NotSupportedException("Actual source unit metadata has not been bound");
            return ActualUnitMetadata.RequireDeliverable(prefab);
        }
    }
}
