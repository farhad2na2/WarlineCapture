using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Game.Configs;
using Game.Skirmish.Contracts;
namespace Game.Runtime
{
 // Immutable source metadata only. No mission, opening placement or runtime state authority.
 internal sealed class SourceMetadataCompatibility
 {
  const string NativeSha="d9b2dbfc12a8cfd12e385a3b3fa9266fd96a3d05939bc244992fafc6e21e1e54";
  const string DefinitionsSha="5a4524435171c46738b782ad296ba84a26d306f6bf9012ff1a15bf84f602488e";
  const string UnitsSha="8eafcc82034c2857eb050c46c7de7e8f0bfa70bdbc505efe579ff2807700e74d";
  const string ConfiguredSha="413c28c09e82c639fad78ec60c00fbf3608cffb7e5ab1d5663ca0cc45253f553";
  readonly JObject metadata;
  readonly Dictionary<string,string> pins=new Dictionary<string,string>(StringComparer.Ordinal);
  internal readonly int TargetSeed;
  internal readonly uint TargetSetupHash;
  static readonly string[] FactorySources={
   "Authorings/BuildingDefinitionAuthoring.cs","Authorings/UnitGridAuthoring.cs",
   "Configs/GameplayConfigModels.cs","Configs/BuildingProductionRecipe.cs",
   "Configs/Prefabs/BuildingDefinitionAuthoringPrefabConfigAsset.cs","Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs",
   "Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs","Composition/BuildingDefinitionUnitAuthoringMetadata.cs",
   "Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs","RuntimeState/BuildingDefinition.cs",
   "Runtime/Skirmish/SkirmishProductionCatalog.cs","Systems/BuildingDefinitionPrefabSystemHelper.cs",
   "Systems/BuildingDefinitionProductionRecipes.cs","Systems/BuildingDefinitionUnitProductionResources.cs",
   "Systems/BuildingDefinitionFootprintCloneSystemHelper.cs","Systems/BuildingUiCommandSystemHelper.cs",
   "Systems/BuildingProductionQueueCompositionSystemHelper.TransportMetadata.cs",
   "Systems/BuildingModelBounds.cs","Systems/SpawnableKeyText.cs","Systems/BuildingRunwaySystem.cs","Systems/BuildingRunwaySystem.PrefabGeometry.cs",
   "Configs/Skirmish/SkirmishArmyProfileConfig.cs","Configs/Skirmish/SkirmishRoleCatalogConfig.cs","Skirmish/Contracts/SkirmishRosterContracts.cs",
   "Configs/GameplayConfigValueTypes.cs","Components/UnitCombatComponents.cs","Components/MaterialFabricationComponents.cs"};
  static readonly string[] OwnerDefinitions={"Game.Runtime.asmdef","Authorings/Game.Authoring.asmdef","Configs/Game.Configs.asmdef","Composition/Game.Composition.asmdef","Components/Game.Components.asmdef","Skirmish/Contracts/Game.Skirmish.Contracts.asmdef"};
  SourceMetadataCompatibility(JObject value,SkirmishResolvedSetup setup){metadata=(JObject)value.DeepClone();TargetSeed=setup.Seed;TargetSetupHash=setup.SetupHash;}
  static JObject Parse(byte[] raw)=>JObject.Parse(System.Text.Encoding.UTF8.GetString(raw),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
  static JObject Read(JToken descriptor,string required,long bound){var d=descriptor as JObject??throw new InvalidDataException("Explicit metadata reference descriptor required");if(d.Properties().Count()!=2||d["path"]==null||(string)d["sha256"]!=required)throw new InvalidDataException("Unqualified metadata reference identity");return Parse(AutonomousWorld.PinnedFile((string)d["path"],required,bound));}
  internal static SourceMetadataCompatibility Load(JObject request,SkirmishResolvedSetup setup,string sourceRepo,string dataRepo)
  {
   if(request["metadata_compatibility"]==null)return null;
   if(string.IsNullOrEmpty(sourceRepo)||string.IsNullOrEmpty(dataRepo)||setup==null||setup.CatalogId!="S002"||setup.SetupHash!=SkirmishSetupCompiler.ComputeHash(setup,setup.Seed))throw new InvalidDataException("Actual compiled context and explicit metadata roots required");
   var native=Read(request["metadata_compatibility"],NativeSha,64*1024*1024);var contexts=native["contexts"]as JArray??throw new InvalidDataException("Native contexts missing");
   if((string)native["kind"]!="NativeTwoSeedMetadataQualification"||contexts.Count!=2||(bool?)native["metadata_equal"]!=true)throw new InvalidDataException("Exact native two-context qualification required");
   var target=contexts.OfType<JObject>().SingleOrDefault(c=>(int?)c["seed"]==setup.Seed&&(uint?)c["setup_hash"]==setup.SetupHash)??throw new NotSupportedException("Metadata context not qualified by native compiler");
   if(!JToken.DeepEquals(contexts[0]["metadata"],contexts[1]["metadata"]))throw new InvalidDataException("Native metadata contexts differ");
   var result=new SourceMetadataCompatibility((JObject)target["metadata"],setup);result.RequireSetup(setup);var nativePins=(JObject)native["source_pins"];
   foreach(string suffix in FactorySources){string rel="Assets/Game/Scripts/"+suffix;result.Admit(sourceRepo,rel,(string)nativePins[rel]);result.Admit(sourceRepo,rel+".meta",(string)nativePins[rel+".meta"]);}
   foreach(string suffix in OwnerDefinitions){string rel="Assets/Game/Scripts/"+suffix;result.Admit(sourceRepo,rel,(string)nativePins[rel]);result.Admit(sourceRepo,rel+".meta",(string)nativePins[rel+".meta"]);}
   foreach(string name in new[]{"metadata_compatibility","building_definitions","unit_production_metadata","building_request_metadata"}){var d=request[name];string path=Path.GetFullPath((string)d["path"]);string hash=(string)d["sha256"];if(result.pins.TryGetValue(path,out var old)&&old!=hash)throw new InvalidDataException("Conflicting metadata artifact pin");RequireFile(path,hash);result.pins[path]=hash;}
   // This conservative scene/provider asset closure was read by the native exporter.
   // Other project code remains historical provenance rather than an unrelated gameplay gate.
   foreach(var pin in nativePins.Properties())if(pin.Name.StartsWith("Assets/",StringComparison.Ordinal)&&!pin.Name.StartsWith("Assets/Game/Scripts/",StringComparison.Ordinal)&&!pin.Name.EndsWith(".cs",StringComparison.Ordinal)&&!pin.Name.EndsWith(".cs.meta",StringComparison.Ordinal))result.Admit(dataRepo,pin.Name,(string)pin.Value);
   foreach(string rel in new[]{"Packages/manifest.json","Packages/packages-lock.json"})result.Admit(sourceRepo,rel,(string)nativePins[rel]);
   var definitions=Read(request["building_definitions"],DefinitionsSha,4*1024*1024);result.RequireDefinitions(DefinitionsSha,definitions);
   result.RequireUnits(UnitsSha,Read(request["unit_production_metadata"],UnitsSha,2*1024*1024));result.RequireConfigured(definitions,Read(request["building_request_metadata"],ConfiguredSha,4*1024*1024));
   result.RequireSelection(setup);result.VerifyUnchanged();return result;
  }
  void Admit(string repo,string relative,string expected){if(expected==null||expected.Length!=64||relative.Split('/').Any(x=>x==".."||x=="."||x==""))throw new InvalidDataException("Canonical metadata dependency required: "+relative);string path=Path.GetFullPath(Path.Combine(repo,relative));if(!path.StartsWith(Path.GetFullPath(repo)+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Metadata dependency escaped root");if(pins.TryGetValue(path,out var old)&&old!=expected)throw new InvalidDataException("Conflicting metadata epoch");pins[path]=expected;RequireFile(path,expected);}
  static void RequireFile(string path,string expected){var f=new FileInfo(path);if(!f.Exists||(f.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Regular metadata dependency required: "+path);using(var stream=File.OpenRead(path))using(var hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=expected)throw new InvalidDataException("Metadata dependency changed: "+path);}
  internal void RequireTarget(uint setupHash,int? seed=null){if(setupHash!=TargetSetupHash||(seed.HasValue&&seed.Value!=TargetSeed))throw new InvalidDataException("Compatibility binding differs from actual reader target setup/seed");}
  internal void RequireConfigured(JObject reference,JObject root,uint setupHash,int seed){RequireTarget(setupHash,seed);RequireConfigured(reference,root);}
  internal void VerifyUnchanged(){foreach(var pin in pins)RequireFile(pin.Key,pin.Value);}
  void RequireSetup(SkirmishResolvedSetup setup)
  {
   var inputs=metadata["catalog_inputs"];if((string)inputs["catalog_id"]!=setup.CatalogId||(string)inputs["army_profile"]!=setup.ArmyProfileId.ToString()||!JToken.DeepEquals(inputs["structure_ids"],new JArray(setup.Structures.Select(x=>x.StructureId))))throw new InvalidDataException("Metadata catalog/grant context changed");
   var expected=(JArray)inputs["role_overlays"]["items"];if(expected.Count!=setup.RoleOverlays.Length)throw new InvalidDataException("Metadata role domain changed");var codec=new ExactInitialValueCodec(".",new JObject());var fields=typeof(SkirmishRoleOverlay).GetFields(BindingFlags.Public|BindingFlags.Instance);
   for(int i=0;i<expected.Count;i++){object reference=codec.Decode(expected[i],typeof(SkirmishRoleOverlay),"metadata/role/"+i);object actual=setup.RoleOverlays[i];foreach(var field in fields){object a=field.GetValue(reference),b=field.GetValue(actual);bool equal=field.FieldType==typeof(float)?BitConverter.GetBytes((float)a).SequenceEqual(BitConverter.GetBytes((float)b)):Equals(a,b);if(!equal)throw new InvalidDataException("Metadata role/recipe input changed: "+i+"/"+field.Name);}}
  }
  static void Equal(JToken a,JToken b,string field){if(!JToken.DeepEquals(a,b))throw new InvalidDataException("Native-qualified metadata differs: "+field);}
  static void RequireHistoricalContext(JObject root){if((int?)root["seed"]!=104731||(uint?)root["setup_hash"]!=3657809016u)throw new InvalidDataException("Original captured metadata provenance must remain unchanged");}
  internal void RequireDefinitions(string sha,JObject root)
  {
   if(sha!=DefinitionsSha)throw new InvalidDataException("Unqualified building metadata artifact");RequireHistoricalContext(root);var native=((JArray)metadata["buildings"]).OfType<JObject>().ToDictionary(x=>(string)x["prefab"]["key"],StringComparer.Ordinal);var rows=(JArray)root["definitions"];if(rows.Count!=native.Count)throw new InvalidDataException("Complete building metadata required");var seen=new HashSet<string>(StringComparer.Ordinal);
   foreach(JObject row in rows){string key=(string)row["key"];if(!seen.Add(key)||!native.TryGetValue(key,out var n))throw new InvalidDataException("Unknown/duplicate building metadata");Equal(n["prefab"],row["prefab"],key+"/prefab");Equal(n["fields"],row["fields"],key+"/fields");}
  }
  internal void RequireUnits(string sha,JObject root)
  {
   if(sha!=UnitsSha)throw new InvalidDataException("Unqualified unit metadata artifact");RequireHistoricalContext(root);var rows=(JArray)root["units"];var native=(JArray)metadata["units"];if(rows.Count!=native.Count)throw new InvalidDataException("Complete unit metadata required");
   for(int i=0;i<rows.Count;i++){if((int?)rows[i]["index"]!=i||(string)rows[i]["key"]!=(string)native[i]["prefab"]["key"])throw new InvalidDataException("Native unit order changed");foreach(string field in new[]{"prefab","has_definition_metadata","has_production_metadata","definition_metadata","production_metadata","authoring_catalog_properties"})Equal(native[i][field],rows[i][field],"units/"+i+"/"+field);}Equal(metadata["transports"],root["transports"],"transports");
  }
  internal void RequireConfigured(JObject reference,JObject root)
  {
   RequireHistoricalContext(reference);if((string)root["kind"]!="CurrentSourceBuildingRequestMetadataSupplement"||(string)root["reference"]["sha256"]!=DefinitionsSha)throw new InvalidDataException("Configured witness identity changed");var rows=(JArray)root["rows"];var native=((JArray)metadata["buildings"]).OfType<JObject>().ToDictionary(x=>(string)x["prefab"]["key"],StringComparer.Ordinal);var seen=new HashSet<string>(StringComparer.Ordinal);if(rows.Count!=native.Count)throw new InvalidDataException("Complete requestability domain required");
   foreach(JObject row in rows){string key=(string)row["key"];if(!seen.Add(key)||!native.TryGetValue(key,out var n))throw new InvalidDataException("Unknown/duplicate configured witness");Equal(n["prefab"],row["prefab"],key+"/configured-prefab");var f=n["configured_entry"]["fields"];if((bool?)row["can_request"]!=(bool?)f["CanRequest"]["value"]||(string)row["display_name"]!=(string)f["DisplayName"]["value"])throw new InvalidDataException("Native requestability changed: "+key);foreach(var pair in new[]{new[]{"construction_catalog_index","construction_index"},new[]{"scene_catalog_index","scene_index"}}){int expected=(int)n[pair[1]];int? actual=(int?)row[pair[0]];if(actual!=(expected<0?(int?)null:expected))throw new InvalidDataException("Native provider index changed: "+key);}}
  }
  void RequireSelection(SkirmishResolvedSetup setup)
  {
   var tokens=new Dictionary<string,SourceUnitPrefab>(StringComparer.Ordinal);SourceUnitPrefab Token(JToken t){if((string)t["codec"]=="null-v1")return null;string key=(string)t["key"];if(tokens.TryGetValue(key,out var old)){if(old.SourceAssetGuid!=(string)t["guid"]||old.SourceAssetLocalId!=(long)t["local_id"]||old.SourceAssetPath!=(string)t["path"])throw new InvalidDataException("Ambiguous provider prefab identity");return old;}var value=SourceUnitPrefab.FromAssetIdentity(key,(string)t["path"],(string)t["guid"],(long)t["local_id"]);tokens.Add(key,value);return value;}
   var p=metadata["providers"];var baseline=((JArray)p["baseline"]).Select(Token).ToArray();var scene=((JArray)p["scene"]).Select(Token).ToArray();var units=((JArray)p["units"]).Select(Token).Where(x=>x!=null).ToDictionary(x=>x.name,StringComparer.Ordinal);var selected=SourceConfiguredBuildingSelection.Select(setup,new SourceBuildingProvider(baseline),new SourceBuildingProvider(scene),units);var expected=((JArray)metadata["buildings"]).Select(x=>(string)x["prefab"]["key"]);if(!selected.Select(x=>x.name).SequenceEqual(expected))throw new InvalidDataException("Current original provider selection differs from native qualification");
  }
  internal JObject Receipt()=>new JObject{["native_reference_sha256"]=NativeSha,["captured_seed"]=104731,["captured_setup_hash"]=3657809016u,["target_seed"]=TargetSeed,["target_setup_hash"]=TargetSetupHash,["dependency_count"]=pins.Count,["runtime_state_transferred"]=false};
 }
}
