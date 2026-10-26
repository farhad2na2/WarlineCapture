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
 internal sealed partial class SourceMetadataCompatibility
 {
  const string NativeSha="d9b2dbfc12a8cfd12e385a3b3fa9266fd96a3d05939bc244992fafc6e21e1e54";
  const string NativeLogicalS026Sha="6a55787a770c82623030907518e87343fad72b5c35d91ae293a5199128ba986a";
  string nativeReferenceKind="NativeS026MetadataQualification";
  const string NativeS026Sha="06ff2cf07f069988a04f9e1740cbc9d29461207014b566fe3f2845c205e7be17";
  const string DefinitionsSha="5a4524435171c46738b782ad296ba84a26d306f6bf9012ff1a15bf84f602488e";
  const string UnitsSha="8eafcc82034c2857eb050c46c7de7e8f0bfa70bdbc505efe579ff2807700e74d";
  const string ConfiguredSha="413c28c09e82c639fad78ec60c00fbf3608cffb7e5ab1d5663ca0cc45253f553";
  readonly JObject metadata;
  readonly string nativeReferenceSha;
  readonly JObject nativeOrigin;
  string definitionsArtifactSha,unitsArtifactSha;
  internal readonly string TargetCatalogId;
  string derivedContextSha; JObject derivedContextDescriptor;
  internal bool NativeQualified=>TargetCatalogId=="S026";
  internal bool FactoryDomainQualified=>derivedContextSha!=null||NativeQualified;
  internal bool DerivedCompilerContext=>derivedContextSha!=null;
  internal int ProjectionSchemaVersion=>derivedContextSha!=null?3:NativeQualified?2:1;
  internal string ProjectionAuthority=>derivedContextSha!=null?"OriginalCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050":"OriginalNativeMetadataFactories_NotMissionState";
  internal string ProjectionKind(string nativeKind)=>derivedContextSha!=null?nativeKind.Replace("NativeQualified","CompilerContextDerived"):nativeKind;
  internal int BuildingCount=>((JArray)metadata["buildings"]).Count;
  internal IEnumerable<string> RequiredUnitKeys=>((JArray)metadata["units"]).Select(r=>(string)r["prefab"]["key"]);
  internal int UnitCount=>((JArray)metadata["units"]).Count;
  readonly Dictionary<string,string> pins=new Dictionary<string,string>(StringComparer.Ordinal);
  readonly Dictionary<string,string> originPins=new Dictionary<string,string>(StringComparer.Ordinal);
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
  SourceMetadataCompatibility(JObject value,SkirmishResolvedSetup setup,string nativeSha,JObject context){nativeReferenceSha=nativeSha;nativeOrigin=(JObject)context.DeepClone();nativeOrigin.Remove("metadata");TargetCatalogId=setup.CatalogId;metadata=(JObject)value.DeepClone();TargetSeed=setup.Seed;TargetSetupHash=setup.SetupHash;}
  static JObject Parse(byte[] raw)=>JObject.Parse(System.Text.Encoding.UTF8.GetString(raw),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
  static JObject Read(JToken descriptor,string required,long bound){var d=descriptor as JObject??throw new InvalidDataException("Explicit metadata reference descriptor required");if(d.Properties().Count()!=2||d["path"]==null||(string)d["sha256"]!=required)throw new InvalidDataException("Unqualified metadata reference identity");return Parse(AutonomousWorld.PinnedFile((string)d["path"],required,bound));}
  internal static SourceMetadataCompatibility Load(JObject request,SkirmishResolvedSetup setup,string sourceRepo,string dataRepo)
  {
   if(request["metadata_compiler_context"]!=null)return LoadDerived(request,setup,sourceRepo,dataRepo);
   if(request["metadata_compatibility"]==null){if(request["metadata_source_origin"]!=null)throw new InvalidDataException("Origin requires native metadata authority");return null;}
   string currentSourceRepo=sourceRepo;JObject origin=null;string originPath=null,originSha=null;
   if(request["metadata_source_origin"]!=null){
    var descriptor=request["metadata_source_origin"]as JObject;
    if(descriptor==null||descriptor.Properties().Count()!=2||descriptor["path"]?.Type!=JTokenType.String||descriptor["sha256"]?.Type!=JTokenType.String)throw new InvalidDataException("Exact metadata origin descriptor required");
    originPath=(string)descriptor["path"];originSha=(string)descriptor["sha256"];origin=Parse(AutonomousWorld.PinnedFile(originPath,originSha,1024*1024));
    string[] fields={"schema_version","kind","native_metadata","source_root","data_root","input_pins"};
    if(origin.Properties().Count()!=fields.Length||origin.Properties().Any(x=>!fields.Contains(x.Name))||origin["schema_version"]?.Type!=JTokenType.Integer||(int?)origin["schema_version"]!=1||(string)origin["kind"]!="QualifiedNativeMetadataOrigin"||!(origin["input_pins"]is JObject)||!JToken.DeepEquals(origin["native_metadata"],request["metadata_compatibility"]))throw new InvalidDataException("Bounded native metadata origin schema/artifact mismatch");
    sourceRepo=RequireOriginRoot(origin["source_root"]);dataRepo=RequireOriginRoot(origin["data_root"]);
   }
   if(string.IsNullOrEmpty(sourceRepo)||string.IsNullOrEmpty(dataRepo)||setup==null||(setup.CatalogId!="S002"&&setup.CatalogId!="S026")||setup.SetupHash!=SkirmishSetupCompiler.ComputeHash(setup,setup.Seed))throw new InvalidDataException("Actual compiled context and explicit metadata roots required");
   bool qualified=setup.CatalogId=="S026",logicalBound=qualified&&request["mission_logical_binding"]!=null;var epoch=SourceNativeMetadataEpoch.Require((string)request["metadata_compatibility"]?["sha256"],setup.CatalogId,logicalBound);string nativeSha=epoch.MetadataSha;
   var native=Read(request["metadata_compatibility"],nativeSha,64*1024*1024);var contexts=native["contexts"]as JArray??throw new InvalidDataException("Native contexts missing");
   JObject target;
   if(qualified){
    if(logicalBound){var logical=Read(request["mission_logical_binding"],epoch.LogicalSha,4*1024*1024);if((string)native["kind"]!="NativeS026LogicalBoundMetadataQualification"||(string)native["logical_binding_reference_sha256"]!=epoch.LogicalSha||(string)logical["kind"]!="NativeAuthoredS026LogicalBindingReference"||(bool?)logical["positions_or_state_transferred"]!=false||(bool?)logical["mission_initialized"]!=false)throw new InvalidDataException("Exact actual logical-bound native metadata context required");}
    else if((string)native["kind"]!="NativeS026MetadataQualification"||(bool?)native["opening_placement_transferred"]!=false)throw new InvalidDataException("Exact historical unbound S026 metadata-only reference required");
    if(contexts.Count!=12||(bool?)native["full_mission_initialized"]!=false||(bool?)native["runtime_state_transferred"]!=false||(bool?)native["public_admission_changed"]!=false)throw new InvalidDataException("Exact native S026 metadata-only qualification required");
    var keys=new HashSet<string>(StringComparer.Ordinal);foreach(JObject context in contexts){string size=(string)context["size"],difficulty=(string)context["difficulty"];if((string)context["catalog_id"]!="S026"||(int?)context["seed"]!=104755||!new[]{"Standard","War","LargeWar"}.Contains(size)||!new[]{"Recruit","Regular","Veteran","Commander"}.Contains(difficulty)||!keys.Add(size+"/"+difficulty)||(bool?)context["wrong_context_hash_rejected"]!=true||(bool?)context["wrong_target_seed_rejected"]!=true)throw new InvalidDataException("Exact finite native S026 compiler contexts required");}
    target=contexts.OfType<JObject>().SingleOrDefault(c=>(int?)c["seed"]==setup.Seed&&(uint?)c["setup_hash"]==setup.SetupHash&&(string)c["size"]==setup.SizeId.ToString()&&(string)c["difficulty"]==setup.DifficultyId.ToString())??throw new NotSupportedException("S026 metadata context not qualified by native compiler");
    var domain=target["selected_native_domain"]as JObject??throw new InvalidDataException("Actual selected native domain required");int buildings=((JArray)target["metadata"]["buildings"]).Count,units=((JArray)target["metadata"]["units"]).Count;if(buildings<1||buildings>64||units<1||units>64||(int?)domain["building_count"]!=buildings||(int?)domain["unit_count"]!=units||(bool?)domain["all_recipe_producers_and_units_in_selected_domain"]!=true)throw new InvalidDataException("Bounded actual selected native metadata domain required");
   }else{
    if((string)native["kind"]!="NativeTwoSeedMetadataQualification"||contexts.Count!=2||(bool?)native["metadata_equal"]!=true)throw new InvalidDataException("Exact native two-context qualification required");
    target=contexts.OfType<JObject>().SingleOrDefault(c=>(int?)c["seed"]==setup.Seed&&(uint?)c["setup_hash"]==setup.SetupHash)??throw new NotSupportedException("Metadata context not qualified by native compiler");
    if(!JToken.DeepEquals(contexts[0]["metadata"],contexts[1]["metadata"]))throw new InvalidDataException("Native metadata contexts differ");
   }
   var result=new SourceMetadataCompatibility((JObject)target["metadata"],setup,nativeSha,target);result.nativeReferenceKind=(string)native["kind"];result.definitionsArtifactSha=qualified?(string)request["building_definitions"]["sha256"]:DefinitionsSha;result.unitsArtifactSha=qualified?(string)request["unit_production_metadata"]["sha256"]:UnitsSha;result.RequireSetup(setup);var nativePins=(JObject)native["source_pins"];
   foreach(string suffix in FactorySources){string rel="Assets/Game/Scripts/"+suffix;result.Admit(sourceRepo,rel,(string)nativePins[rel]);result.Admit(sourceRepo,rel+".meta",(string)nativePins[rel+".meta"]);}
   foreach(string suffix in OwnerDefinitions){string rel="Assets/Game/Scripts/"+suffix;result.Admit(sourceRepo,rel,(string)nativePins[rel]);result.Admit(sourceRepo,rel+".meta",(string)nativePins[rel+".meta"]);}
   foreach(string name in new[]{"metadata_compatibility","building_definitions","unit_production_metadata","building_request_metadata"}.Concat(logicalBound?new[]{"mission_logical_binding"}:Array.Empty<string>())){var d=request[name];string path=Path.GetFullPath((string)d["path"]);string hash=(string)d["sha256"];if(result.pins.TryGetValue(path,out var old)&&old!=hash)throw new InvalidDataException("Conflicting metadata artifact pin");RequireFile(path,hash);result.pins[path]=hash;}
   // This conservative scene/provider asset closure was read by the native exporter.
   // Other project code remains historical provenance rather than an unrelated gameplay gate.
   foreach(var pin in nativePins.Properties())if(pin.Name.StartsWith("Assets/",StringComparison.Ordinal)&&!pin.Name.StartsWith("Assets/Game/Scripts/",StringComparison.Ordinal)&&!pin.Name.EndsWith(".cs",StringComparison.Ordinal)&&!pin.Name.EndsWith(".cs.meta",StringComparison.Ordinal))result.Admit(dataRepo,pin.Name,(string)pin.Value);
   foreach(string rel in new[]{"Packages/manifest.json","Packages/packages-lock.json"})result.Admit(sourceRepo,rel,(string)nativePins[rel]);
   var definitions=Read(request["building_definitions"],result.definitionsArtifactSha,4*1024*1024);result.RequireDefinitions(result.definitionsArtifactSha,definitions);
   result.RequireUnits(result.unitsArtifactSha,Read(request["unit_production_metadata"],result.unitsArtifactSha,2*1024*1024));result.RequireConfigured(definitions,Read(request["building_request_metadata"],qualified?(string)request["building_request_metadata"]["sha256"]:ConfiguredSha,4*1024*1024));
   if(origin!=null){
    foreach(string suffix in FactorySources){string rel="Assets/Game/Scripts/"+suffix;result.Admit(currentSourceRepo,rel,(string)nativePins[rel],false);result.Admit(currentSourceRepo,rel+".meta",(string)nativePins[rel+".meta"],false);}
    foreach(string suffix in OwnerDefinitions){string rel="Assets/Game/Scripts/"+suffix;
     if(suffix!="Composition/Game.Composition.asmdef")result.Admit(currentSourceRepo,rel,(string)nativePins[rel],false);
     else {string current=FileHash(Path.Combine(currentSourceRepo,rel)),captured=(string)nativePins[rel];
      if(current!=captured&&!(current=="8f41d8cd74821639e2628fb77ad32fdd89db88111af3af800ebb16d109753cfe"&&captured=="b6db33e0e266aea3c4f63a70677b92a70188f101ad149b91560f897e466d3c6b"))throw new InvalidDataException("Unqualified current Composition packaging definition");
      result.Admit(currentSourceRepo,rel,current,false);}
     result.Admit(currentSourceRepo,rel+".meta",(string)nativePins[rel+".meta"],false);
    }
    foreach(string rel in new[]{"Packages/manifest.json","Packages/packages-lock.json"})result.Admit(currentSourceRepo,rel,(string)nativePins[rel],false);
    var declared=(JObject)origin["input_pins"];
    if(declared.Count!=result.originPins.Count||declared.Properties().Any(x=>x.Value.Type!=JTokenType.String||!result.originPins.TryGetValue(x.Name,out var expected)||(string)x.Value!=expected))throw new InvalidDataException("Metadata origin consumed-file coverage mismatch");
    result.pins[originPath]=originSha;
   }
   result.RequireSelection(setup);result.VerifyUnchanged();return result;
  }
  static string RequireOriginRoot(JToken token){if(token?.Type!=JTokenType.String||!Path.IsPathRooted((string)token))throw new InvalidDataException("Absolute metadata origin root required");var dir=new DirectoryInfo(Path.GetFullPath((string)token));if(!dir.Exists)throw new InvalidDataException("Existing metadata origin root required");for(var d=dir;d!=null;d=d.Parent)if((d.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Symlink metadata origin root forbidden");return dir.FullName;}
  static string FileHash(string path){using(var stream=File.OpenRead(path))using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
  void Admit(string repo,string relative,string expected,bool capturedOrigin=true){if(expected==null||expected.Length!=64||relative.Split('/').Any(x=>x==".."||x=="."||x==""))throw new InvalidDataException("Canonical metadata dependency required: "+relative);string path=Path.GetFullPath(Path.Combine(repo,relative));if(!path.StartsWith(Path.GetFullPath(repo)+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Metadata dependency escaped root");if(pins.TryGetValue(path,out var old)&&old!=expected)throw new InvalidDataException("Conflicting metadata epoch");pins[path]=expected;if(capturedOrigin)originPins[path]=expected;RequireFile(path,expected);}
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
  void RequireNativeProjection(JObject root,string kind){if(derivedContextSha!=null){RequireDerivedProjection(root,kind);return;}if((int?)root["schema_version"]!=2||(string)root["kind"]!=kind||(string)root["authority"]!="OriginalNativeMetadataFactories_NotMissionState"||(string)root["catalog_id"]!=TargetCatalogId||(int?)root["seed"]!=TargetSeed||(uint?)root["setup_hash"]!=TargetSetupHash||(bool?)root["full_mission_initialized"]!=false||(bool?)root["native_visual_readiness"]!=false||(root["unsupported_values"]as JArray)?.Count!=0)throw new InvalidDataException("Exact native-qualified projection header required");var origin=root["native_origin"]as JObject??throw new InvalidDataException("Native projection provenance required");if((string)origin["kind"]!=nativeReferenceKind||(string)origin["artifact_sha256"]!=nativeReferenceSha)throw new InvalidDataException("Native projection artifact differs");foreach(string field in new[]{"catalog_id","size","difficulty","seed","setup_hash","metadata_key_sha256"})Equal(origin[field],nativeOrigin[field],"native-origin/"+field);}
  static void Equal(JToken a,JToken b,string field){if(!JToken.DeepEquals(a,b))throw new InvalidDataException("Native-qualified metadata differs: "+field);}
  static void RequireHistoricalContext(JObject root){if((int?)root["seed"]!=104731||(uint?)root["setup_hash"]!=3657809016u)throw new InvalidDataException("Original captured metadata provenance must remain unchanged");}
  internal void RequireDefinitions(string sha,JObject root)
  {
   if(FactoryDomainQualified){if(sha!=definitionsArtifactSha)throw new InvalidDataException("Native building projection artifact differs");RequireNativeProjection(root,"NativeQualifiedSkirmishBuildingDefinitions");}else{if(sha!=DefinitionsSha)throw new InvalidDataException("Unqualified building metadata artifact");RequireHistoricalContext(root);}var native=((JArray)metadata["buildings"]).OfType<JObject>().ToDictionary(x=>(string)x["prefab"]["key"],StringComparer.Ordinal);var rows=(JArray)root["definitions"];if(rows.Count!=native.Count)throw new InvalidDataException("Complete building metadata required");var seen=new HashSet<string>(StringComparer.Ordinal);
   foreach(JObject row in rows){string key=(string)row["key"];if(!seen.Add(key)||!native.TryGetValue(key,out var n))throw new InvalidDataException("Unknown/duplicate building metadata");Equal(n["prefab"],row["prefab"],key+"/prefab");Equal(n["fields"],row["fields"],key+"/fields");}
  }
  internal void RequireUnits(string sha,JObject root)
  {
   if(FactoryDomainQualified){if(sha!=unitsArtifactSha)throw new InvalidDataException("Native unit projection artifact differs");RequireNativeProjection(root,"NativeQualifiedSkirmishUnitProductionMetadata");}else{if(sha!=UnitsSha)throw new InvalidDataException("Unqualified unit metadata artifact");RequireHistoricalContext(root);}var rows=(JArray)root["units"];var native=(JArray)metadata["units"];if(rows.Count!=native.Count)throw new InvalidDataException("Complete unit metadata required");
   for(int i=0;i<rows.Count;i++){if((int?)rows[i]["index"]!=i||(string)rows[i]["key"]!=(string)native[i]["prefab"]["key"])throw new InvalidDataException("Native unit order changed");foreach(string field in new[]{"prefab","has_definition_metadata","has_production_metadata","definition_metadata","production_metadata","authoring_catalog_properties"})Equal(native[i][field],rows[i][field],"units/"+i+"/"+field);}Equal(metadata["transports"],root["transports"],"transports");
  }
  internal void RequireConfigured(JObject reference,JObject root)
  {
   if(FactoryDomainQualified){RequireNativeProjection(reference,"NativeQualifiedSkirmishBuildingDefinitions");RequireNativeProjection(root,"NativeQualifiedConfiguredBuildingMetadata");if((string)root["reference"]["sha256"]!=definitionsArtifactSha)throw new InvalidDataException("Native configured reference differs");}else{RequireHistoricalContext(reference);if((string)root["kind"]!="CurrentSourceBuildingRequestMetadataSupplement"||(string)root["reference"]["sha256"]!=DefinitionsSha)throw new InvalidDataException("Configured witness identity changed");}var rows=(JArray)root["rows"];var native=((JArray)metadata["buildings"]).OfType<JObject>().ToDictionary(x=>(string)x["prefab"]["key"],StringComparer.Ordinal);var seen=new HashSet<string>(StringComparer.Ordinal);if(rows.Count!=native.Count)throw new InvalidDataException("Complete requestability domain required");
   foreach(JObject row in rows){string key=(string)row["key"];if(!seen.Add(key)||!native.TryGetValue(key,out var n))throw new InvalidDataException("Unknown/duplicate configured witness");Equal(n["prefab"],row["prefab"],key+"/configured-prefab");var f=n["configured_entry"]["fields"];if((bool?)row["can_request"]!=(bool?)f["CanRequest"]["value"]||(string)row["display_name"]!=(string)f["DisplayName"]["value"])throw new InvalidDataException("Native requestability changed: "+key);foreach(var pair in new[]{new[]{"construction_catalog_index","construction_index"},new[]{"scene_catalog_index","scene_index"}}){int expected=(int)n[pair[1]];int? actual=(int?)row[pair[0]];if(actual!=(expected<0?(int?)null:expected))throw new InvalidDataException("Native provider index changed: "+key);}}
  }
  internal void RequireProductionFacts(SkirmishResolvedSetup setup,SkirmishProductionCatalog catalog,GeneratedUnitProductionMetadata units,GeneratedBuildingDefinitions definitions,IReadOnlyDictionary<SourceUnitPrefab,BuildingProductionRecipe> recipes){
   if(!FactoryDomainQualified)return;RequireTarget(setup.SetupHash,setup.Seed);RequireSetup(setup);if(catalog==null||!ReferenceEquals(catalog.Setup,setup)||units==null||!units.FactoryDomainQualified||definitions==null||recipes==null)throw new InvalidDataException("Same actual qualified source catalog metadata required");var expected=(JArray)metadata["recipes"];if(expected.Count!=recipes.Count)throw new InvalidDataException("Native recipe domain changed");
   foreach(JObject n in expected){string unitKey=(string)n["UnitPrefab"]["key"],producerKey=(string)n["ProducerPrefab"]["key"];var row=units.Ordered.SingleOrDefault(r=>r.Prefab.name==unitKey)??throw new InvalidDataException("Native recipe unit missing");if(!definitions.Assets.TryGetValue(producerKey,out var producer)||!recipes.TryGetValue(row.Prefab,out var actual)||!ReferenceEquals(actual.ProducerPrefab,producer)||!ReferenceEquals(actual.UnitPrefab,row.Prefab)||!definitions.AssetIdentities.TryGetValue(producerKey,out var producerIdentity))throw new InvalidDataException("Native recipe prefab identity changed");Equal(producerIdentity,n["ProducerPrefab"],"recipe/producer");Equal(row.AssetIdentity,n["UnitPrefab"],"recipe/unit");if(actual.Quantity!=(int)n["Quantity"]||actual.MaterialsCost!=(int)n["MaterialsCost"]||actual.CreditsCost!=(int)n["CreditsCost"]||actual.UseProducerGroundExit!=(bool)n["UseProducerGroundExit"])throw new InvalidDataException("Actual role recipe differs from native metadata");}
   var roles=new Dictionary<string,SkirmishRoleOverlay>(StringComparer.Ordinal);foreach(var overlay in setup.RoleOverlays){string key=SkirmishRoleCatalogConfig.RuntimePrefabKey(overlay.RoleKind);if(catalog.TryRole(key,out var role))roles.Add(key,role);}var timing=new SkirmishProductionCatalog(setup,roles,true);timing.BindSourceDeliveryMetadata(units.Ordered.ToDictionary(r=>r.Prefab.name,r=>r.Prefab,StringComparer.Ordinal),units.TryMetadata);var codec=new ExactInitialValueCodec(".",new JObject());foreach(JObject n in (JArray)metadata["delivery_seconds"]){var role=(SkirmishRoleKind)Enum.Parse(typeof(SkirmishRoleKind),(string)n["role"]);bool found=timing.TryReadProductionDeliverySeconds(role,out float seconds);float expectedSeconds=(float)codec.Decode(n["seconds"],typeof(float),"native/delivery-seconds");if(found!=(bool)n["found"]||!BitConverter.GetBytes(seconds).SequenceEqual(BitConverter.GetBytes(expectedSeconds)))throw new InvalidDataException("Actual source delivery timing differs from native metadata");}VerifyUnchanged();
  }
  void RequireSelection(SkirmishResolvedSetup setup)
  {
   var tokens=new Dictionary<string,SourceUnitPrefab>(StringComparer.Ordinal);SourceUnitPrefab Token(JToken t){if((string)t["codec"]=="null-v1")return null;string key=(string)t["key"];if(tokens.TryGetValue(key,out var old)){if(old.SourceAssetGuid!=(string)t["guid"]||old.SourceAssetLocalId!=(long)t["local_id"]||old.SourceAssetPath!=(string)t["path"])throw new InvalidDataException("Ambiguous provider prefab identity");return old;}var value=SourceUnitPrefab.FromAssetIdentity(key,(string)t["path"],(string)t["guid"],(long)t["local_id"]);tokens.Add(key,value);return value;}
   var p=metadata["providers"];var baseline=((JArray)p["baseline"]).Select(Token).ToArray();var scene=((JArray)p["scene"]).Select(Token).ToArray();var units=((JArray)p["units"]).Select(Token).Where(x=>x!=null).ToDictionary(x=>x.name,StringComparer.Ordinal);var selected=SourceConfiguredBuildingSelection.Select(setup,new SourceBuildingProvider(baseline),new SourceBuildingProvider(scene),units);var expected=((JArray)metadata["buildings"]).Select(x=>(string)x["prefab"]["key"]);if(!selected.Select(x=>x.name).SequenceEqual(expected))throw new InvalidDataException("Current original provider selection differs from native qualification");
  }
  internal JObject Receipt()=>derivedContextSha!=null?DerivedReceipt():new JObject{["native_reference_sha256"]=nativeReferenceSha,["target_catalog_id"]=TargetCatalogId,["native_qualified_projection"]=NativeQualified,["building_count"]=BuildingCount,["unit_count"]=UnitCount,["captured_catalog_id"]=NativeQualified?TargetCatalogId:"S002",["captured_seed"]=NativeQualified?TargetSeed:104731,["captured_setup_hash"]=NativeQualified?TargetSetupHash:3657809016u,["target_seed"]=TargetSeed,["target_setup_hash"]=TargetSetupHash,["dependency_count"]=pins.Count,["runtime_state_transferred"]=false};
 }
}
