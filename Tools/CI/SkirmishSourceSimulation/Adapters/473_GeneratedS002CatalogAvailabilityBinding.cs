using System;using System.IO;using System.Linq;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Unity.Entities;using UnityEngine;using Game.Components;using Game.Configs;
namespace Game.Runtime{
// Same-world fresh-config adapter, not a reconstruction of a native UI serialized registry.
internal sealed class GeneratedS002CatalogAvailabilityBinding{
 internal readonly CatalogPrefabStore Store;internal readonly SourceCatalogRows Rows;internal readonly SourceCampIdentityMap IdentityMap;internal readonly IReadOnlyList<LogicalCatalogPrefab> ConfiguredUnits;
 readonly EntityManager em;readonly Entity session;
 private GeneratedS002CatalogAvailabilityBinding(EntityManager em,Entity session,CatalogPrefabStore store,SourceCatalogRows rows,SourceCampIdentityMap map,List<LogicalCatalogPrefab> units){this.em=em;this.session=session;Store=store;Rows=rows;IdentityMap=map;ConfiguredUnits=units.AsReadOnly();}
 internal static GeneratedS002CatalogAvailabilityBinding Bind(EntityManager em,Entity session,GeneratedUnitProductionMetadata admittedMetadata,string metadataPath,string sha){
  if(em==null||admittedMetadata==null||admittedMetadata.ArtifactSha256!=sha)throw new InvalidDataException("Same admitted metadata artifact required");
  var bytes=AutonomousWorld.PinnedFile(metadataPath,sha,2*1024*1024);
  var root=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
  var units=DecodeConfiguredUnits(root,admittedMetadata,metadataPath);
  var record=em.GetComponentObject<SkirmishProductionCatalogRecord>(session).Catalog;
  SourceGroundMissionFamily.RequireCatalogSetup(record?.Setup);
  var allowed=units.Where(p=>record.TryRole(p.name,out _)).ToArray();
  var store=CatalogPrefabStore.FromResolvedConfiguredUnits(units,allowed);
  var bindings=new List<SourceCampPrefabBinding>();
  for(int i=0;i<units.Count;i++)bindings.Add(new SourceCampPrefabBinding(units[i],admittedMetadata.Ordered[i].Prefab));
  // Full identity/template preflight precedes publishing the once-per-world Rows owner.
  var map=new SourceCampIdentityMap(em,store,bindings,true);
  var rows=new SourceCatalogRows(em,session,store);
  return new GeneratedS002CatalogAvailabilityBinding(em,session,store,rows,map,units);
 }
 internal static List<LogicalCatalogPrefab> DecodeConfiguredUnits(JObject root,GeneratedUnitProductionMetadata admitted,string path){
  if(root==null||admitted==null||(string)root["kind"]!=(admitted.FactoryDomainQualified?"NativeQualifiedSkirmishUnitProductionMetadata":"AuthoredSkirmishUnitProductionMetadata")||(int?)root["schema_version"]!=(admitted.FactoryDomainQualified?2:1)||(bool?)root["ui_catalog_runtime_policy_sampled"]!=false)throw new InvalidDataException("Original static authoring data, not sampled runtime policy, required");
  var rows=root["units"] as JArray;if(rows==null||rows.Count!=admitted.Ordered.Count||(!admitted.FactoryDomainQualified&&admitted.Ordered.Count!=12))throw new InvalidDataException("Complete admitted configured unit order required");
  var codec=new ExactInitialValueCodec(Path.GetDirectoryName(path),new JObject());var sprites=new Dictionary<string,CatalogSpriteHandle>(StringComparer.Ordinal);var spritePaths=new Dictionary<string,string>(StringComparer.Ordinal);var result=new List<LogicalCatalogPrefab>();var ids=new HashSet<string>(StringComparer.Ordinal);
  for(int i=0;i<rows.Count;i++){
   var row=rows[i] as JObject;var source=admitted.Ordered[i];
   if(row==null||(int?)row["index"]!=i||source.Index!=i||(string)row["key"]!=source.Prefab.name||!JToken.DeepEquals(row["prefab"],source.AssetIdentity))throw new InvalidDataException("Ordered canonical prefab identity mismatch");
   var env=row["authoring_catalog_properties"] as JObject;var f=env?["fields"] as JObject;
   string[] fields={"ConfiguredDisplayName","ConfiguredDescription","CanRequest","MaterialsCost","Price","ProductionDurationSeconds","PortraitSprite","PortraitCardSprite","PortraitActionSprite","IsAirUnit","IsProductionTransportUnit","SoldierTransportCapacity","ConfiguredAllowIdleWander","ConfiguredResourceHaulerBarrelCapacity","ConfiguredCanAttack","ConfiguredAttackDamage","ConfiguredAttackRange","ConfiguredSpeed","ConfiguredMaxHealth"};
   if((string)env?["source_type"]!="Game.Authoring.UnitGridAuthoring"||(bool?)env?["runtime_catalog_admission_and_role_overlay_applied"]!=false||f==null||!f.Properties().Select(p=>p.Name).OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(fields.OrderBy(n=>n,StringComparer.Ordinal)))throw new InvalidDataException("Exact19 original resolved getter schema required");
   Func<string,string> S=n=>(string)codec.Decode(f[n],typeof(string),source.Prefab.name+"/"+n);Func<string,int>I=n=>(int)codec.Decode(f[n],typeof(int),source.Prefab.name+"/"+n);Func<string,bool>B=n=>(bool)codec.Decode(f[n],typeof(bool),source.Prefab.name+"/"+n);Func<string,float>F=n=>(float)codec.Decode(f[n],typeof(float),source.Prefab.name+"/"+n);
   Func<string,CatalogSpriteHandle>P=n=>{
    var t=f[n];if((string)t?["codec"]=="null-v1")return null;
    var a=t as JObject;if((string)a?["codec"]!="source-asset-reference-v1"||(string)a?["type"]!="UnityEngine.Sprite")throw new InvalidDataException("Actual sprite source identity required");
    var guid=(string)a["guid"];var assetPath=(string)a["path"];long local=(long?)a["local_id"]??0;var key=(string)a["key"];
    if(guid==null||!System.Text.RegularExpressions.Regex.IsMatch(guid,"^[0-9a-f]{32}$")||local==0||string.IsNullOrEmpty(key)||assetPath==null||!assetPath.StartsWith("Assets/",StringComparison.Ordinal)||assetPath.Split('/').Any(p=>p==".."||p=="."))throw new InvalidDataException("Closed sprite asset identity required");
    string identity=guid+":"+local.ToString(System.Globalization.CultureInfo.InvariantCulture);
    if(spritePaths.TryGetValue(identity,out string old)&&old!=assetPath)throw new InvalidDataException("Conflicting sprite asset path");
    spritePaths[identity]=assetPath;if(!sprites.TryGetValue(identity,out var handle))sprites.Add(identity,handle=new CatalogSpriteHandle(identity+"@"+assetPath));return handle;
   };
   var p=source.Production;
   var unit=new CatalogUnitAuthoringValues(true,S("ConfiguredDisplayName"),S("ConfiguredDescription"),B("CanRequest"),I("MaterialsCost"),F("ProductionDurationSeconds"),p.FootprintCells,P("PortraitSprite"),P("PortraitCardSprite"),P("PortraitActionSprite"),B("IsAirUnit"),B("IsProductionTransportUnit"),I("SoldierTransportCapacity"),B("ConfiguredAllowIdleWander"),I("ConfiguredResourceHaulerBarrelCapacity"),B("ConfiguredCanAttack"),I("ConfiguredAttackDamage"),F("ConfiguredAttackRange"),F("ConfiguredSpeed"),I("ConfiguredMaxHealth"),I("Price"),p.ProductionTransportPrefab,p.ProductionTransportArrivalSeconds,p.ProductionTransportHoldForNextReadySeconds,p.ProductionTransportMaxConcurrent,p.ProductionTransportRequiresAirportRunway,p.ProductionTransportUsesRunwayLanding);
   string id=(string)source.AssetIdentity["guid"]+":"+((long)source.AssetIdentity["local_id"]).ToString(System.Globalization.CultureInfo.InvariantCulture);
   if(!ids.Add(id))throw new InvalidDataException("Duplicate actual configured prefab identity");
   result.Add(LogicalCatalogPrefab.ResolvedConfiguredUnit(id,source.Prefab.name,unit));
  }
  return result;
 }
 internal SourceCampAvailability CreateAvailability(Entity bank,IReadOnlyDictionary<int,RuntimeBuildingEntity> sameLiveBuildings,IReadOnlyList<RuntimeDefinition> definitions,IReadOnlyDictionary<SourceUnitPrefab,RuntimeDefinition> definitionsByPrefab,IReadOnlyList<SourceUnitPrefab> units,IReadOnlyDictionary<string,SourceUnitPrefab> byKey,IReadOnlyDictionary<SourceUnitPrefab,BuildingProductionRecipe> recipes,int globalQueueLimit,BuildingProductionQueueCompositionSystemHelper production,BuildingProductionQueueCompositionSystemHelper.QueueContext context,BuildingProductionQueueCompositionSystemHelper.TryGetPrefabLocalBoundsDelegate bounds,SourceCampRunwayReads runway,bool runwayKnown,bool initialDomainComplete)
 =>new SourceCampAvailability(em,session,bank,Store,Rows,IdentityMap,sameLiveBuildings,definitions,definitionsByPrefab,units,byKey,recipes,globalQueueLimit,production,context,bounds,runway,runwayKnown,initialDomainComplete);
}
}
