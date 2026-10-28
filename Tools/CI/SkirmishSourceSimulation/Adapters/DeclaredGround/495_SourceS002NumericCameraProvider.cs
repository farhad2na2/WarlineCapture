using System;using System.IO;using System.Linq;using System.Globalization;using System.Reflection;using System.Collections.Generic;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Game.Components;using Game.Configs;using Game.Runtime.TransportPose;using Unity.Mathematics;using UnityEngine;
namespace Game.Runtime{
// Generated data camera owner. Basis only: no native streaming state, Camera object,
// visual proof, position, viewport, projection matrix or RuntimeCameraSnapshotComponent.
internal sealed partial class SourceS002NumericCameraProvider{
 readonly SourceNumericCameraEngineAdapterContract engineAdapter;
 internal const string Scope="FreshConfigInitializedMission_SourceMissionViewBasis";
 internal readonly string OperationMapId,LayoutId,CompiledContentHash,ProfileSha256,SourceMapSha256,ConfigGraphSha256;
 internal readonly uint SetupHash;internal readonly int GridWidth,GridHeight;internal readonly float CellSize;internal readonly float3 Origin;
 readonly string catalogId;readonly string profilePath;readonly long profileLength,profileWriteTicks;readonly SkirmishMissionViewProfile profile;
 quaternion rotation;float lastTime,nextCheck;bool hasTime,poisoned;
 internal float3 Right{get{Ready();return math.rotate(rotation,new float3(1,0,0));}}
 internal float3 Forward{get{Ready();return math.rotate(rotation,new float3(0,0,1));}}
 internal quaternion Rotation{get{Ready();return rotation;}}
 internal bool SnapshotAvailable=>poseEnabled&&nativeValuesAdmitted&&motionValuesAdmitted&&!poisoned;
 private SourceS002NumericCameraProvider(SkirmishResolvedSetup setup,GridConfig grid,SkirmishMissionViewProfile profile,string path,string profileSha,string mapSha,string graphSha,SourceNumericCameraEngineAdapterContract adapter){engineAdapter=adapter??throw new ArgumentNullException(nameof(adapter));if(adapter.ProfileSha!=profileSha)throw new InvalidDataException("Engine adapter/profile differs");
  catalogId=setup.CatalogId;this.profile=profile;profilePath=path;ProfileSha256=profileSha;SourceMapSha256=mapSha;ConfigGraphSha256=graphSha;OperationMapId=setup.OperationMapId;LayoutId=setup.LayoutId;CompiledContentHash=setup.ContentHash;SetupHash=setup.SetupHash;GridWidth=grid.Width;GridHeight=grid.Height;CellSize=grid.CellSize;Origin=grid.Origin;var f=new FileInfo(path);profileLength=f.Length;profileWriteTicks=f.LastWriteTimeUtc.Ticks;
  // Original Play transition queues normal perspective, whose terminal writer uses
  // Quaternion.Euler(pitch,yaw,0). PoseMath is the already-qualified same numeric lane.
  rotation=PoseMath.Euler(new float3(profile.Pitch,profile.Yaw,0));PoseMath.Unit(rotation);
 }
 internal static void VerifyGeneratedDefinitionSource(JObject request,string repositoryRoot){var identity=ReadDefinitionSourceIdentity(request,repositoryRoot);VerifyGraphPin(identity.pins,identity.path);}
 static (JObject pins,string path,string sha) ReadDefinitionSourceIdentity(JObject request,string repositoryRoot){
  var graphRef=request["source_config_graph"] as JObject??throw new InvalidDataException("Source graph descriptor missing");
  string graphPath=(string)graphRef["path"],graphSha=(string)graphRef["sha256"];
  var graph=Json(AutonomousWorld.PinnedFile(graphPath,graphSha,request["metadata_compiler_context"]!=null?16*1024*1024:4*1024*1024));
  var pins=graph["source_pins"] as JObject??throw new InvalidDataException("Source graph pins missing");
  string definitionPath=request["metadata_compiler_context"]!=null?null:Path.GetFullPath(Path.Combine(repositoryRoot,(string)graph["definition_path"]));
  return(pins,definitionPath,graphSha);
 }
 internal static SourceS002NumericCameraProvider CreateGenerated(SkirmishResolvedSetup setup,SkirmishScenarioDefinitionConfig actualDefinition,GridConfig actualGrid,string initializationPath,string initializationSha,string repositoryRoot){
  PoseMath.Ready();
  if(setup==null||actualDefinition==null||(setup.CatalogId!="S002"&&setup.CatalogId!="S026"&&actualDefinition.MapLayout?.DeclaredConfiguration==null&&!(setup.CatalogId=="S049"&&actualDefinition.MapLayout!=null&&actualDefinition.MapLayout.WorldBinding.HasMeasuredDeployments&&actualDefinition.MapLayout.WorldBinding.BindAllConfigurations))||!setup.MeasuredLayoutBound||actualGrid.Width<1||actualGrid.Height<1||!math.isfinite(actualGrid.CellSize)||actualGrid.CellSize<=0||!math.all(math.isfinite(actualGrid.Origin)))throw new NotSupportedException("Actual compiled measured source/grid constructor identity required");
  var request=Json(AutonomousWorld.PinnedFile(initializationPath,initializationSha,2*1024*1024));
  if(setup.CatalogId=="S049"&&!SourceDeclaredMissionConfiguration.RequireLayoutSelection(actualDefinition.MapLayout,SourceConfigurationQualification.Context(request)))throw new InvalidDataException("Exact S049 all-configuration camera layout required");
  var (pins,definitionPath,graphSha)=ReadDefinitionSourceIdentity(request,repositoryRoot);
  if(setup.CatalogId=="S002")VerifyGraphPin(pins,definitionPath);else if(request["metadata_compiler_context"]==null)SourceS026TypedConfiguration.RequireFrozenMeasuredLayout(actualDefinition.MapLayout);
  var mapRef=request["static_map"]?["definition"] as JObject??throw new InvalidDataException("Actual source map descriptor missing");
  string mapPath=(string)mapRef["path"],mapSha=(string)mapRef["sha256"];
  string mapYaml=System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(mapPath,mapSha,1024*1024));var map=TopFields(mapYaml);
  if((string)request["static_map"]?["operation_map_id"]!=setup.OperationMapId)throw new InvalidDataException("Generated map/setup identity mismatch");
  // The compiler graph's logical CampaignMap may be a source-bound wrapper.
  // The separately pinned generated constructor static_map is the actual current
  // physical data identity. Do not conflate these or invent a native active-map record.
  var gm=SectionFields(mapYaml,"gridMetadata");var sm=SectionFields(mapYaml,"surfaceMetadata");
  var dims=Inline(Need(gm,"dimensions"));var origin=Inline(Need(gm,"origin"));
  if(actualGrid.Width!=int.Parse(Need(dims,"x"),CultureInfo.InvariantCulture)||actualGrid.Height!=int.Parse(Need(dims,"y"),CultureInfo.InvariantCulture)||actualGrid.CellSize!=float.Parse(Need(gm,"cellSize"),CultureInfo.InvariantCulture)||actualGrid.Origin.x!=float.Parse(Need(origin,"x"),CultureInfo.InvariantCulture)||actualGrid.Origin.y!=float.Parse(Need(origin,"y"),CultureInfo.InvariantCulture)||actualGrid.Origin.z!=float.Parse(Need(origin,"z"),CultureInfo.InvariantCulture))throw new InvalidDataException("Actual grid disagrees with pinned constructor map metadata");
  if((string)request["static_map"]?["grid_config"]?["sha256"]!=Need(gm,"contentHash")||(string)request["static_map"]?["surface_data"]?["sha256"]!=Need(sm,"contentHash"))throw new InvalidDataException("Generated source grid/surface hashes mismatch current pinned map");
  foreach(string name in new[]{"grid_config","surface_data"}){var r=request["static_map"]?[name] as JObject??throw new InvalidDataException("Actual static map data missing");AutonomousWorld.PinnedFile((string)r["path"],(string)r["sha256"],4*1024*1024);}
  if(request["metadata_compiler_context"]!=null){var derived=SourceDeclaredEngineeringCameraProfile.Create(setup,actualDefinition,request,out string declaredPath,out string declaredSha,out var functionAdapter);return new SourceS002NumericCameraProvider(setup,actualGrid,derived,declaredPath,declaredSha,mapSha,graphSha,functionAdapter);}
  if(setup.CatalogId=="S026"){
   if(mapSha!="68a631ca108e01c9a17839d2359e409226cdb009193f68ba910fa6f0f0a082b8")throw new InvalidDataException("Exact S026 DenseCity physical map bytes required");
   var engineering=SourceEngineeringCameraProfile.Create(setup,actualDefinition,request,out string headerPath,out string headerSha);
   return new SourceS002NumericCameraProvider(setup,actualGrid,engineering,headerPath,headerSha,mapSha,graphSha,SourceNumericCameraEngineAdapterContract.ReadEngineering(request,headerSha));
  }
  string p=Path.GetFullPath(Path.Combine(repositoryRoot,"Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset"));byte[] bytes=File.ReadAllBytes(p);string profileSha=AutonomousWorld.Hash(bytes);
  var fields=TopFields(System.Text.Encoding.UTF8.GetString(bytes));var profile=ScriptableObject.CreateInstance<SkirmishMissionViewProfile>();
  foreach(var field in typeof(SkirmishMissionViewProfile).GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly)){
   if(!fields.TryGetValue(field.Name,out string value))throw new InvalidDataException("Complete actual view field missing: "+field.Name);
   object parsed=field.FieldType==typeof(string)?(object)value:field.FieldType==typeof(bool)?(value=="1"?(object)true:value=="0"?(object)false:throw new InvalidDataException("Source bool")):field.FieldType==typeof(int)?int.Parse(value,CultureInfo.InvariantCulture):field.FieldType==typeof(float)?float.Parse(value,CultureInfo.InvariantCulture):field.FieldType.IsEnum?Enum.ToObject(field.FieldType,int.Parse(value,CultureInfo.InvariantCulture)):throw new NotSupportedException("Unexpected view field");
   field.SetValue(profile,parsed);
  }
  // Execute the ORIGINAL complete profile policy, using independently pinned map values.
  // This deliberately does not call native MatchesSource/TryResolve and fabricates no
  // ActiveOperationMap generation, metadata blob or PhysicalSourceValidated marker.
  if(!profile.MatchesActiveMap(setup,actualDefinition,Need(map,"operationMapId"),Need(map,"contentHash"),Need(map,"sourceIdentityHash")))throw new NotSupportedException("Original mission view identity policy rejected generated source identity");
  return new SourceS002NumericCameraProvider(setup,actualGrid,profile,p,profileSha,mapSha,graphSha,SourceNumericCameraEngineAdapterContract.Legacy(profileSha));
 }
 internal void AdvanceSourceUnscaledTime(float now){
  Ready();if(!math.isfinite(now)||(hasTime&&now<lastTime)){poisoned=true;throw new NotSupportedException("Monotonic actual source unscaled camera time required");}lastTime=now;hasTime=true;
  if(now<nextCheck)return;nextCheck=now+.25f;var f=new FileInfo(profilePath);
  if(!f.Exists||f.Length!=profileLength||f.LastWriteTimeUtc.Ticks!=profileWriteTicks){poisoned=true;throw new InvalidDataException("Mission camera source profile changed during generated session");}
 }
 internal void ObserveLogicalCameraAction(string action){
  Ready();
  switch(action){
   // Actual public-map overlay and public-objective inspection affect position only.
   case "OpenMap":case "CloseMap":case "InspectPublicObjective":case "PanGroundCenter":return;
   // Source normal/build profiles both receive the same authored pitch/yaw.
   case "NormalPerspective":case "BuildPerspective":case "FocusPlayerBaseFallbackInstant":case "FocusEnemyBaseFallbackInstant":
    rotation=PoseMath.Euler(new float3(profile.Pitch,profile.Yaw,0));if(poseEnabled)ApplySourcePerspective(action=="BuildPerspective");return;
   default:poisoned=true;throw new NotSupportedException("Unmodeled source camera writer: "+action);
  }
 }
 internal void RequireSnapshot(){Ready();if(!SnapshotAvailable)throw new NotSupportedException("Numeric camera pose/projection/oracle admission required");}
 void Ready(){PoseMath.Ready();if(poisoned)throw new InvalidOperationException("Camera owner is terminal after unsupported writer/identity failure");}
 static JObject Json(byte[] bytes)=>JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
 static void VerifyGraphPin(JObject pins,string path){string hash=(string)pins[path];if(hash==null)throw new InvalidDataException("Compiler definition lacks source graph identity");AutonomousWorld.PinnedFile(path,hash,2*1024*1024);}
 static string Need(Dictionary<string,string>d,string n)=>d.TryGetValue(n,out var s)&&!string.IsNullOrEmpty(s)?s:throw new InvalidDataException("Required source map field "+n);
 static Dictionary<string,string> SectionFields(string yaml,string name){
  var d=new Dictionary<string,string>(StringComparer.Ordinal);bool inside=false;foreach(string line in yaml.Split('\n')){
   if(line.TrimEnd('\r')=="  "+name+":"){inside=true;continue;}if(!inside)continue;
   if(line.StartsWith("  ",StringComparison.Ordinal)&&line.Length>2&&line[2]!=' ')break;
   if(!line.StartsWith("    ",StringComparison.Ordinal)||line.Length<5||line[4]==' ')continue;
   int c=line.IndexOf(':',4);if(c<0)continue;var k=line.Substring(4,c-4);if(!d.TryAdd(k,line.Substring(c+1).Trim()))throw new InvalidDataException("Duplicate map metadata field "+k);
  }if(!inside)throw new InvalidDataException("Missing map metadata "+name);return d;
 }
 static Dictionary<string,string> Inline(string value){
  if(value.Length<2||value[0]!='{'||value[value.Length-1]!='}')throw new InvalidDataException("Expected numeric source inline record");
  var d=new Dictionary<string,string>(StringComparer.Ordinal);foreach(var part in value.Substring(1,value.Length-2).Split(',')){int c=part.IndexOf(':');if(c<1||!d.TryAdd(part.Substring(0,c).Trim(),part.Substring(c+1).Trim()))throw new InvalidDataException("Duplicate/bad inline source record");}return d;
 }
 static Dictionary<string,string> TopFields(string yaml){
  var d=new Dictionary<string,string>(StringComparer.Ordinal);foreach(string raw in yaml.Split('\n')){
   if(!raw.StartsWith("  ",StringComparison.Ordinal)||raw.Length<3||(raw[2]==' '||raw[2]=='-'))continue;
   int colon=raw.IndexOf(':',2);if(colon<0)continue;string k=raw.Substring(2,colon-2),v=raw.Substring(colon+1).TrimEnd('\r').Trim();
   if(!d.TryAdd(k,v))throw new InvalidDataException("Duplicate source YAML field "+k);
  }return d;
 }
}
}
