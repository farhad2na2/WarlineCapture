using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Game.Components;
using Newtonsoft.Json.Linq;
using Unity.Entities;

namespace Game.Runtime
{
    internal sealed partial class GeneratedBuildingDefinitions
    {
        internal JObject AddS004SourceOnlyAirport(string path,string hash,EntityManager em,
            System.Collections.Generic.IReadOnlyDictionary<int,RuntimeBuildingEntity> liveBuildings)
        {
            if(em==null || liveBuildings==null || liveBuildings.Count!=20 || Definitions.Count!=9 || Definitions.Any(d=>d?.Prefab?.name=="Building_Airport"))
                throw new InvalidOperationException("Fresh same-world S004 starting definition domain required");
            foreach(var building in liveBuildings.Values)
                if(building==null || (!building.IsDestroyed && (building.Definition==null || building.Definition.HasRunway || !em.Exists(building.CombatEntity))))
                    throw new NotSupportedException("Live runway state is outside the source airport definition probe");
            var data=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(path,hash,1024*1024)),
                new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
#if WARLINE_S004_CURRENT_EPOCH
            const string expectedAuthority="CurrentAuditedFactories_SourceOnly";
#else
            const string expectedAuthority="RetainedOriginalFactory_SourceOnly";
#endif
            if((string)data["kind"]!="S004SourceAirportDefinitionCandidate" ||
                (string)data["authority"]!=expectedAuthority || (string)data["catalog_id"]!="S004" ||
                (uint?)data["setup_hash"]!=1130726651u || (int?)data["seed"]!=104733 ||
                (bool?)data["live_runway_registry_validated"]!=false || (bool?)data["runtime_admission"]!=false || (bool?)data["mission_ready"]!=false)
                throw new InvalidDataException("Exact source-only unplaced airport candidate required");
            var row=(JObject)data["airport"];var fields=(JObject)row?["fields"];
            if((string)row?["key"]!="Building_Airport" || fields==null)throw new InvalidDataException("Original airport fields required");
            var definition=new RuntimeDefinition();
            foreach(var field in typeof(RuntimeDefinition).GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)) {
                if(field.Name=="SourceMetadataComplete" || field.Name=="SourceRecipeAuthority")continue;
                if(!fields.TryGetValue(field.Name,out var value))throw new InvalidDataException("Missing actual airport definition field: "+field.Name);
                if(PresentationOnly.Contains(field.Name))continue;
                field.SetValue(definition,Decode(value,field.FieldType,"Building_Airport/"+field.Name));
            }
            if(definition.Prefab==null || definition.Prefab.name!="Building_Airport" || !definition.HasRunway || definition.IsWall)
                throw new InvalidDataException("Original runway-bearing airport definition required");
            string[] expected={"Unit_Veh_Jet_02","Unit_Veh_Jet_01","Unit_Veh_Plane_Transport"};
            if(definition.ProductionSlots==null || !definition.ProductionSlots.Select(slot=>slot?.SpawnUnitPrefab?.name).SequenceEqual(expected))
                throw new InvalidDataException("Original airport production slot order changed");
            foreach(var name in PresentationOnly)if(fields[name]!=null)PresentationAssetBindings["Building_Airport/"+name]=(JObject)fields[name].DeepClone();
            SourceBuildingProducerCoreProjection.Apply(definition,AssetIdentities);
            definition.SourceMetadataComplete=true; Definitions.Add(definition);
            return new JObject { ["source_definition_added"]=true,["has_runway"]=definition.HasRunway,
                ["starting_live_buildings"]=liveBuildings.Count,["placed_airport"]=false,
                ["production_slots"]=JArray.FromObject(expected),["live_runway_owner_bound"]=false,
                ["native_source_fit"]=false,["mission_ready"]=false };
        }
    }
}
