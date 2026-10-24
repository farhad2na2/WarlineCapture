using System;using System.IO;using System.Linq;using System.Reflection;using System.Text;using System.Security.Cryptography;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Game.Configs;using Game.Editor;using Game.Skirmish.Contracts;using UnityEngine;using Inputs=Game.Editor.SkirmishGroundSourceFitSurvey.RequestedLogicalSourceLayouts;
static class RequestedInputChecks
{
 static int checks;static void Check(bool yes,string name){if(!yes)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Reject(Action action,string name){bool refused=false;try{action();}catch{refused=true;}Check(refused,name);}
 static T Old<T>(string method,params object[] args)=>(T)typeof(LogicalLayoutChecks).GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
 static string Hash(string text)=>Hash(Encoding.UTF8.GetBytes(text));
 static string Hash(byte[] bytes){using var hash=SHA256.Create();return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 static Inputs Parse(JObject input){string text=input.ToString(Formatting.None);return Inputs.FromRaw(Encoding.UTF8.GetBytes(text),Hash(text));}
 static T Asset<T>(string path)where T:UnityEngine.Object,new(){var x=new T();x.AssetPath=path;return x;}
 static JObject Input(JObject authority,SkirmishMapLayoutConfig layout,JObject pins)
 {
  var rects=new JArray(layout.FactionCompounds.Select(x=>new JObject{["faction"]=x.FactionId,["world_rect"]=new JArray(x.MinX,x.MinZ,x.MaxX,x.MaxZ)}));
  return new JObject{["schemaVersion"]=1,["sourcePins"]=pins.DeepClone(),["missions"]=new JArray(new JObject{["authority"]=authority.DeepClone(),["layoutJson"]=JsonUtility.ToJson(layout),["layoutSemanticSha256"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(layout),["layout_input"]=new JObject{["pilot_candidate"]=new JObject{["typed_pad_world_axis_candidates"]=rects}}})};
 }
 static int Main(){try{Run();Console.WriteLine("[RequestedLogicalInputPortable] result=Passed checks="+checks+" actualHelper=True measuredFacts=Synthetic native=UNRUN owningEditorCompile=UNRUN actualBudgetCompiler=UNRUN");return 0;}catch(Exception e){Console.Error.WriteLine(e);return 2;}}
 static void Run()
 {
  var source=Old<OperationMapDefinition>("Source");var layout=Old<SkirmishMapLayoutConfig>("Layout");var authority=Old<JObject>("Mission","S050");var pins=Old<JObject>("Pins",source);
  var profiles=new SkirmishExpansionAuthoredSet{ObjectiveBa=Asset<SkirmishObjectiveConfig>("fixture/objective"),ArmyGround=Asset<SkirmishArmyProfileConfig>("fixture/army"),StartField=Asset<SkirmishStartPackageConfig>("fixture/field"),StartEstablished=Asset<SkirmishStartPackageConfig>("fixture/established"),Economy=Asset<SkirmishEconomyConfig>("fixture/economy"),Upgrades=Asset<SkirmishUpgradeConfig>("fixture/upgrades"),Intel=Asset<SkirmishIntelConfig>("fixture/intel"),Roles=Asset<SkirmishRoleCatalogConfig>("fixture/roles")};
  var catalog=Asset<SkirmishExpansionCatalogConfig>("fixture/catalog");catalog.Profiles=profiles;Resources.Values[SkirmishExpansionCatalogConfig.ResourceName]=catalog;
  SkirmishSetupCompiler.ExpectedUnion=layout.FacilityPlacements.Select(x=>new SkirmishResolvedStructureEntry{FactionId=x.FactionId,StructureId=x.StructureId,DesignatedBase=x.StructureId==SkirmishStructureIds.Barracks}).ToArray();
  var raw=Input(authority,layout,pins);var input=Parse(raw);var all=(JArray)JObject.Parse(File.ReadAllText("Design/AgentReports/SkirmishExpansion/Full120/source-fit-authoring-batches-20261015-v2.json"))["missions"];
  Check(Inputs.ReadOptional(null,null)==null,"absent optional input delegates legacy routing");Reject(()=>Inputs.ReadOptional(null,new string('0',64)),"partial optional input rejects");
  var rows=input.ResolveRequestedRows(all,"MP","S050");Check(rows.Length==1&&JToken.DeepEquals(rows[0],authority),"exact canonical requested group/order resolves without old FC survey");
  {var other=all.OfType<JObject>().First(x=>(string)x["catalog_id"]!="S050"&&(string)x["tuple"]["map_id"]=="MP"&&(string)x["tuple"]["army_profile"]=="G"&&(string)x["tuple"]["objective_id"]=="BA"&&x["protected_binding"].Type==JTokenType.Null);var multiple=(JObject)raw.DeepClone();var entry=(JObject)multiple["missions"][0].DeepClone();entry["authority"]=other.DeepClone();((JArray)multiple["missions"]).Add(entry);var ordered=Parse(multiple);string otherId=(string)other["catalog_id"];Check(ordered.ResolveRequestedRows(all,"MP","S050,"+otherId).Length==2,"two canonical rows resolve in exact recorded order");Reject(()=>ordered.ResolveRequestedRows(all,"MP",otherId+",S050"),"same requested set in reversed order rejects");}
  input.Resolve(authority,out var candidate,out var template);Check(JToken.DeepEquals(candidate,authority)&&authority.Properties().All(x=>JToken.DeepEquals(template[x.Name],x.Value))&&(string)template["collector_source_group"]=="MP"&&JToken.DeepEquals(template["source_candidate"],pins),"complete immutable authority metadata and explicit physical source survive template resolution");
  Check(template["layout_input"]["source_envelope_world"] is JArray,"source envelope derived from actual typed logical layout");
  var failedRecord=input.RecordFor(authority,null);Check(failedRecord["rawJson"]!=null&&failedRecord["initialOwnedLayoutJson"].Type==JTokenType.Null,"original raw/pins/layout preserved before candidate creation or measurement failure");
  Check(input.CreateCandidate(authority,source,out var owner,out var error),"actual explicit typed candidate factory succeeds: "+error);
  using(owner)
  {
   var record=input.RecordFor(authority,owner.Layout);Inputs.VerifyReconstructedLayout(record,owner.Layout);Check(record["initialOwnedLayoutRawSha256"]?.Type==JTokenType.String,"initial owned catalog-identity clone recorded and verified");
   var measurement=new JObject{["sourcePins"]=input.SourcePins,["proposedLayoutJson"]=JsonUtility.ToJson(owner.Layout),["proposedLayoutSemanticSha256"]=SkirmishMeasuredLayoutBinder.ComputeLayoutSemanticFingerprint(owner.Layout),["authoredDefinitionLogicalBindingGraph"]=new JObject{["floatScalar"]=.1f,["variants"]=new JArray(new JObject{["size"]=1,["value"]=.3f})}};
   Inputs.AttachMeasurementJoin(record,measurement);Inputs.VerifyMeasurementJoin(record,measurement);Check(measurement["logicalSourceLayoutJoin"] is JObject,"raw original/initial/final layout and binding graph join attaches");
   var report=new JObject{["logicalSourceLayoutInput"]=input.Record()};var row=new JObject{["logicalSourceLayoutInput"]=record,["measurement"]=measurement};
   report=JObject.Parse(report.ToString(Formatting.None));row=JObject.Parse(row.ToString(Formatting.None));Check(Inputs.FromReport(report,row,authority)!=null,"record-only reconstruction survives float-to-double report roundtrip");
   Check(Inputs.FromReport(new JObject(),new JObject(),authority)==null,"all-absent report input/join preserves legacy materializer path");
   var mutated=(JObject)row.DeepClone();mutated["measurement"]["proposedLayoutJson"]=(string)mutated["measurement"]["proposedLayoutJson"]+" ";Reject(()=>Inputs.FromReport(report,mutated,authority),"semantically equal but raw-replaced final layout rejects existing join");
   mutated=(JObject)row.DeepClone();mutated["measurement"]["authoredDefinitionLogicalBindingGraph"]["floatScalar"]=.2;Reject(()=>Inputs.FromReport(report,mutated,authority),"binding graph mutation rejects immutable join");
   mutated=(JObject)row.DeepClone();mutated["logicalSourceLayoutInput"]["initialOwnedLayoutRawSha256"]=new string('0',64);Reject(()=>Inputs.FromReport(report,mutated,authority),"initial-owned record perturbation rejects");
   mutated=(JObject)row.DeepClone();mutated["measurement"]["sourcePins"]["contentHash"]=new string('0',64);Reject(()=>Inputs.FromReport(report,mutated,authority),"measurement source-pin perturbation rejects");
   mutated=(JObject)row.DeepClone();((JObject)mutated["measurement"]).Remove("logicalSourceLayoutJoin");Reject(()=>Inputs.FromReport(report,mutated,authority),"partial input/evidence without join fails closed");
   var altered=(JObject)report.DeepClone();altered["logicalSourceLayoutInput"]["rawJson"]=(string)altered["logicalSourceLayoutInput"]["rawJson"]+" ";Reject(()=>Inputs.FromReport(altered,row,authority),"global raw input hash drift rejects");
   var changed=UnityEngine.Object.Instantiate(owner.Layout);try{var pad=changed.Pads[0];pad.WidthMetres+=1;changed.Pads[0]=pad;Reject(()=>Inputs.VerifyReconstructedLayout(record,changed),"initial layout cannot be replaced by a different typed recipe");}finally{UnityEngine.Object.DestroyImmediate(changed);}
   Reject(()=>Inputs.AttachMeasurementJoin(record,measurement),"measurement join cannot be silently overwritten");
  }
  Reject(()=>input.ResolveRequestedRows(all,"IB","S050"),"logical group mismatch rejects");Reject(()=>input.ResolveRequestedRows(all,"MP","S050,S049"),"requested set/order differs from recorded input");
  void Mutant(Action<JObject> mutate,string name){var x=(JObject)raw.DeepClone();mutate(x);Reject(()=>Parse(x),name);}
  Mutant(x=>x["missions"][0]["authority"]["canonical_size_rows"][0]["player_rifle"]="999","actual immutable authority rejects budget mutation");
  Mutant(x=>x["missions"][0]["authority"]=Old<JObject>("Mission","S002"),"actual protected BA authority rejects alternate input");
  Mutant(x=>((JArray)x["missions"]).Add(x["missions"][0].DeepClone()),"duplicate requested catalog input rejects");
  Mutant(x=>x["missions"][0]["layout_input"]["pilot_candidate"]["source_path"]="Assets/old-source.asset","contradictory legacy pilot source alias rejects");
  Mutant(x=>x["sourcePins"]["gridAssetSha256"]="bad","malformed source pin rejects");
  Mutant(x=>x["missions"][0]["layout_input"]["pilot_candidate"]["typed_pad_world_axis_candidates"][0]["world_rect"][0]=900,"search rectangle cannot exceed actual typed compound");
  Mutant(x=>x["missions"][0]["layout_input"]["source_envelope_world"]=new JArray(0,0,1,1),"supplied source envelope cannot replace typed envelope");
  string text=raw.ToString(Formatting.None);Reject(()=>Inputs.FromRaw(Encoding.UTF8.GetBytes(text),new string('0',64)),"raw SHA mismatch rejects");
  File.WriteAllText("valid.input.json",text,new UTF8Encoding(false));Check(Inputs.ReadOptional("valid.input.json",Hash(text))!=null,"bounded regular UTF8 file reads exact pinned input");
  Reject(()=>Inputs.ReadOptional("oversize.input",new string('0',64)),"oversize real file rejects before open");
  if(Environment.OSVersion.Platform==PlatformID.Unix){var watch=System.Diagnostics.Stopwatch.StartNew();Reject(()=>Inputs.ReadOptional("fifo.input",new string('0',64)),"real FIFO rejects before opening a stream");Check(watch.ElapsedMilliseconds<1000,"real FIFO rejection returns without waiting for a writer");}
  string duplicate=text.Insert(1,"\"schemaVersion\":1,");Reject(()=>Inputs.FromRaw(Encoding.UTF8.GetBytes(duplicate),Hash(duplicate)),"duplicate JSON property rejects");
  Reject(()=>Inputs.FromRaw(new byte[]{255},Hash(new byte[]{255})),"invalid UTF8 with matching raw hash rejects");
  var exposed=input.SourcePins;exposed["contentHash"]=new string('0',64);Check(JToken.DeepEquals(input.SourcePins,pins),"source pin property returns independently owned snapshot");
  input.Resolve(authority,out candidate,out template);template["layout_input"]["pilot_candidate"]=null;input.Resolve(authority,out candidate,out template);Check(template["layout_input"]["pilot_candidate"] is JObject,"resolver outputs cannot mutate captured request");
  UnityEngine.Object.DestroyImmediate(layout);
 }
}
