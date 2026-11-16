using System;using System.IO;using System.Collections.Generic;using System.Linq;using System.Security.Cryptography;using System.Globalization;using Newtonsoft.Json.Linq;
namespace Game.Runtime{internal sealed class SourceNativeDeathReadModel{
 internal const string S074NoCameraRawReadsetSha256="5e1bf6de50b045dd6f5a380a63aa1dcfcfcba2c13d33a3fcf4eb3d94374f3664";
 internal sealed class Row{internal readonly string Key;internal readonly uint ConfiguredBits;internal readonly byte DeathIndex;internal readonly bool HasSelection,ConfiguredWreck,LegacyWreck;readonly UnitDeathMechanicalClipInput[][] inputs;internal UnitDeathMechanicalClipInput[][] Inputs=>inputs.Select(a=>a.ToArray()).ToArray();internal Row(string key,uint bits,byte index,bool selected,bool configuredWreck,bool legacyWreck,UnitDeathMechanicalClipInput[][] values){Key=key;ConfiguredBits=bits;DeathIndex=index;HasSelection=selected;ConfiguredWreck=configuredWreck;LegacyWreck=legacyWreck;inputs=values;}}
 readonly Dictionary<string,Row> rows;private SourceNativeDeathReadModel(Dictionary<string,Row> value){rows=value;}
 internal static SourceNativeDeathReadModel LoadAtBoundary(string path){var info=new FileInfo(path);if(!Path.IsPathRooted(path)||!info.Exists||info.Length<1||info.Length>1024*1024||(info.Attributes&FileAttributes.ReparsePoint)!=0)throw new NotSupportedException("Bounded regular qualified native death input required");return LoadAtBoundary(File.ReadAllBytes(path));}internal static SourceNativeDeathReadModel LoadAtBoundary(byte[] bytes){if(bytes==null||bytes.Length<1||bytes.Length>1024*1024)throw new NotSupportedException("Bounded qualified native death inputs required");using var hash=SHA256.Create();string digest=BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();bool s049=digest=="d15289b4888e514145abf44272da142fed9d8a0ed8c5543cc411dfb7b15c2dfe";if(!s049&&digest!="2bd03c905c8eb7f50b71bcb6078615258289a9862cc36704d09ea2ff16437df5")throw new NotSupportedException("Changed qualified native death inputs");var j=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));if((string)j["kind"]!="NativeQualifiedDeathMechanicalInputs"||(string)j["native_origin_sha256"]!=(s049?"f7377f9a11d7cc1ebbe968a68068d60859ad02a8b57c74b4ea18e961188c037f":"e13f969712c45975b3a8bdbd39f136da8767c20b502ad42fed7124bb2d876cfd")||s049&&((bool?)j["full_mission_initialized"]!=false||(bool?)j["renderer_qualified"]!=false||(bool?)j["runtime_admitted"]!=false))throw new NotSupportedException("Wrong native death authority");return Decode(j);} static SourceNativeDeathReadModel Decode(JObject j){var map=new Dictionary<string,Row>(StringComparer.Ordinal);foreach(var token in (JArray)j["rows"]){var input=new List<UnitDeathMechanicalClipInput[]>();foreach(var link in (JArray)token["linked_animation_inputs"]){if(!(bool)link["animator_exists"]||!(bool)link["blob_created"])continue;var clips=(JArray)link["clips"];if(clips.Count!=(int)link["animation_count"]||clips.Count>4096)throw new NotSupportedException("Changed qualified clip domain");input.Add(clips.Select(c=>new UnitDeathMechanicalClipInput((int)c["frames"],checked((byte)(int)c["fps"]),checked((byte)(int)c["speed"]))).ToArray());}var row=new Row((string)token["key"],Convert.ToUInt32((string)token["configured_duration_bits"],16),checked((byte)(int)token["death_animation_index"]),(bool)token["has_death_animation_selection"],(bool)token["configured_wreck_bypasses_prepare"],(bool)token["legacy_wreck_bypasses_prepare"],input.ToArray());map.Add(row.Key,row);}if(map.Count!=16)throw new NotSupportedException("Exact16 qualified native death inputs required");return new SourceNativeDeathReadModel(map);}
 internal static SourceNativeDeathReadModel LoadS026Diagnostic(JObject descriptor){
 const string originSha="8a8a5bafd860d512b77500205b987233555209d35c5465a72f2a176ae3feb9ec", projectedSha="b8dfa566c3ca35a809abf9e716142bc78902128a5b3ca06c3f6440f129e95e98";
 string[] fields={"path","sha256","native_path","native_sha256","capture_root"};if(descriptor==null||descriptor.Count!=5||descriptor.Properties().Any(p=>!fields.Contains(p.Name)||p.Value.Type!=JTokenType.String)||(string)descriptor["sha256"]!=projectedSha||(string)descriptor["native_sha256"]!=originSha)throw new InvalidDataException("Exact diagnostic death source descriptors required");
 var native=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)descriptor["native_path"],originSha,4*1024*1024)));var projection=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)descriptor["path"],projectedSha,1024*1024)));
 if((bool?)native["full_mission_initialized"]!=false||(bool?)native["renderer_qualified"]!=false||(bool?)native["runtime_admitted"]!=false||(string)native["keys_sha256"]!="2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7"||(string)projection["native_origin_sha256"]!=originSha||(string)projection["kind"]!="NativeQualifiedDeathMechanicalInputs"||(bool?)projection["full_mission_initialized"]!=false||(bool?)projection["renderer_qualified"]!=false||(bool?)projection["runtime_admitted"]!=false)throw new InvalidDataException("Diagnostic death authority differs");
 GeneratedReferenceTemplateView.VerifyS026RelativePins((JObject)native["source_pins"],(string)descriptor["capture_root"]);
 var originals=(JArray)native["rows"];var rows=(JArray)projection["rows"];if(originals.Count!=16||rows.Count!=16)throw new InvalidDataException("Exact S026 death rows required");string[] columns={"key","configured_duration_bits","death_animation_index","has_death_animation_selection","configured_wreck_bypasses_prepare","legacy_wreck_bypasses_prepare"};for(int i=0;i<16;i++){var a=originals[i];var b=rows[i];foreach(var column in columns)if(!JToken.DeepEquals(a[column],b[column]))throw new InvalidDataException("Death projection field changed");if(!JToken.DeepEquals(a["actual_original_prepare_bits"],a["reversed_child_original_prepare_bits"]))throw new InvalidDataException("Death owner child-order control failed");var links=((JArray)a["visual_readset"]).Where(v=>(bool?)v["has_animator_link"]==true).Select(v=>new JObject{["animator_exists"]=v["animator_exists"].DeepClone(),["blob_created"]=v["blob_created"].DeepClone(),["animation_count"]=v["animation_count"]?.DeepClone(),["clips"]=v["clips"]?.DeepClone()}).ToArray();if(!JToken.DeepEquals(new JArray(links),b["linked_animation_inputs"]))throw new InvalidDataException("Death original linked animation projection changed");}return Decode(projection);
 }
 internal static SourceNativeDeathReadModel LoadS050SourceDiagnostic(JObject descriptor){
 const string root="/Volumes/T7/Projects/WarlineCapture";
 const string keySha="2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7";
 const string marker="[SkirmishDeathMechanicalReadset] result=Passed prefabs=16 originalPrepare=16 childPermutation=16 fullMission=0";
 if(descriptor==null||descriptor.Count!=2||descriptor.Properties().Any(p=>p.Name!="projection"&&p.Name!="receipt")||
  (descriptor["projection"] as JObject)?.Count!=2||(descriptor["receipt"] as JObject)?.Count!=2)
  throw new InvalidDataException("Exact S050 source diagnostic projection/receipt descriptors required");
 var projectionDesc=(JObject)descriptor["projection"];var receiptDesc=(JObject)descriptor["receipt"];
 void RequireRepoFile(string path,string name){if(!Path.IsPathRooted(path)||Path.GetFullPath(path)!=path||!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new InvalidDataException("Exact repository-owned S050 "+name+" path required");for(var parent=new DirectoryInfo(Path.GetDirectoryName(path));parent!=null&&parent.FullName.Length>=root.Length;parent=parent.Parent)if((parent.Attributes&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("S050 "+name+" path symlink forbidden");}
 JObject Exact(JObject d,string name,long max){if(d.Properties().Any(p=>p.Name!="path"&&p.Name!="sha256")||d["path"]?.Type!=JTokenType.String||d["sha256"]?.Type!=JTokenType.String)throw new InvalidDataException("Exact pinned S050 "+name+" descriptor required");RequireRepoFile((string)d["path"],name);return JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)d["path"],(string)d["sha256"],max)),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});}
 var receipt=Exact(receiptDesc,"death projection receipt",1024*1024);var projection=Exact(projectionDesc,"death projection",1024*1024);
 if((int?)receipt["schema_version"]!=1||(string)receipt["kind"]!="S050SourceDiagnosticDeathProjectionReceipt"||
  (string)receipt["status"]!="ProjectedSourceDiagnosticOnly"||(int?)receipt["rows"]!=16||
  (long?)receipt["projection_bytes"]<1||(long?)receipt["projection_bytes"]>1024*1024||
  (string)receipt["projection_sha256"]!=(string)projectionDesc["sha256"]||
  (string)receipt["projection"]?["path"]!=(string)projectionDesc["path"]||
  (string)receipt["projection"]?["sha256"]!=(string)projectionDesc["sha256"]||
  (bool?)receipt["full_mission_initialized"]!=false||(bool?)receipt["renderer_qualified"]!=false||
  (bool?)receipt["runtime_admitted"]!=false||(bool?)receipt["mission_ready"]!=false)
  throw new InvalidDataException("S050 death projection receipt scope/hash changed");
 var sourceDesc=receipt["source_capture"] as JObject;var auditDesc=receipt["source_audit"] as JObject;var keysDesc=receipt["prefab_key_set"] as JObject;
 if(sourceDesc==null||sourceDesc.Count!=2||auditDesc==null||auditDesc.Count!=2||keysDesc==null||keysDesc.Count!=2||
  (string)receipt["capture_root"]!=root||(string)keysDesc["sha256"]!=keySha||
  (string)projection["kind"]!="S050SourceDiagnosticDeathMechanicalProjection"||
  (string)projection["authority"]!="OriginalNativePrepare_OriginalParentSystem_OriginalKernel_NotMissionState"||
  (string)projection["keys_sha256"]!=keySha||(int?)projection["schema_version"]!=1||
  (bool?)projection["full_mission_initialized"]!=false||(bool?)projection["renderer_qualified"]!=false||
  (bool?)projection["runtime_admitted"]!=false||(bool?)projection["mission_ready"]!=false||
  !JToken.DeepEquals(projection["source_capture"],sourceDesc)||!JToken.DeepEquals(projection["source_audit"],auditDesc)||
  !JToken.DeepEquals(projection["prefab_key_set"],keysDesc))
  throw new InvalidDataException("S050 death projection provenance/scope differs");
 RequireRepoFile((string)sourceDesc["path"],"owner capture");var raw=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)sourceDesc["path"],(string)sourceDesc["sha256"],4*1024*1024)),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
 var audit=Exact(auditDesc,"death owner audit",1024*1024);
 if((int?)raw["schema_version"]!=1||(string)raw["kind"]!="NativeSelectedPrefabDeathMechanicalReadset"||
  (string)raw["authority"]!="OriginalNativePrepare_OriginalParentSystem_OriginalKernel_NotMissionState"||
  (string)raw["keys_sha256"]!=keySha||(string)raw["selected_prefab_set_mode"]!="ExplicitHashPinnedGenericSet"||
  (bool?)raw["full_mission_initialized"]!=false||(bool?)raw["renderer_qualified"]!=false||(bool?)raw["runtime_admitted"]!=false||
  (int?)raw["rows"]?.Count()!=16||(int?)raw["native_cleanup_rows"]?.Count()!=16)
  throw new InvalidDataException("S050 source death capture is not the exact diagnostic selected-16 owner readset");
 if((int?)receipt["source_pin_count"]<1||(int?)receipt["source_pin_count"]>10000||
  (int?)audit["source_pin_audit"]?["count"]!=(int?)receipt["source_pin_count"]||
  (int?)audit["source_pin_audit"]?["mismatches"]!=0||
  (string)audit["kind"]!="S050DeathMechanicalReadsetAudit"||(string)audit["status"]!="PassedForMechanicalReadsetOnly"||
  (string)audit["exact_pass_marker"]!=marker||(int?)audit["exact_pass_marker_count"]!=1||
  !JToken.DeepEquals(audit["readset"],sourceDesc)||(string)audit["prefab_key_set"]?["path"]!=(string)keysDesc["path"]||
  (string)audit["prefab_key_set"]?["sha256"]!=keySha||
  (int?)audit["prefab_key_set"]?["prefabs"]!=16||
  (bool?)audit["flags"]?["full_mission_initialized"]!=false||(bool?)audit["flags"]?["renderer_qualified"]!=false||
  (bool?)audit["flags"]?["runtime_admitted"]!=false||(string)audit["unity"]?["batch_mode"]!="NO")
  throw new InvalidDataException("Current S050 source-only death owner audit required");
 var unityLog=audit["unity_log"] as JObject;if(unityLog==null||unityLog.Count!=3||
  unityLog.Properties().Any(p=>p.Name!="path"&&p.Name!="source_path"&&p.Name!="sha256"))
  throw new InvalidDataException("Pinned S050 owner Unity log descriptor required");
 RequireRepoFile((string)unityLog["path"],"preserved Unity log");var log=System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)unityLog["path"],(string)unityLog["sha256"],32*1024*1024));
 if(log.Split(new[]{marker},StringSplitOptions.None).Length!=2)throw new InvalidDataException("Exact S050 death owner pass marker count required");
 RequireRepoFile((string)keysDesc["path"],"prefab key descriptor");var keys=JArray.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.PinnedFile((string)keysDesc["path"],keySha,4096)),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
 if(keys.Count!=16||keys.Any(k=>k.Type!=JTokenType.String)||keys.Values<string>().Distinct(StringComparer.Ordinal).Count()!=16||
  (string)raw["source_pins"]?[(string)keysDesc["path"]]!=keySha)
  throw new InvalidDataException("Exact current S050 selected prefab key descriptor required");
 var sourcePins=raw["source_pins"] as JObject;if(sourcePins==null||sourcePins.Count!=(int)receipt["source_pin_count"])
  throw new InvalidDataException("S050 full source pin closure count changed");
 GeneratedReferenceTemplateView.VerifyGenericFocusedSourcePins(sourcePins,root);
 var originals=raw["rows"] as JArray;var cleanups=raw["native_cleanup_rows"] as JArray;
 for(int i=0;i<16;i++){
  var a=originals[i] as JObject;var c=cleanups[i] as JObject;
  if((string)a?["key"]!=(string)keys[i]||(string)c?["key"]!=(string)keys[i]||
   !JToken.DeepEquals(a["actual_original_prepare_bits"],a["reversed_child_original_prepare_bits"])||
   !JToken.DeepEquals(a["actual_original_prepare_bits"],a["original_fold_bits"])||
   !JToken.DeepEquals(a["actual_original_prepare_bits"],a["unit_death_time_remaining_assignment_bits"])||
   (bool?)c["mission_lifecycle_hooks_executed"]!=false||
   (c["after_original_parent_cleanup"] as JArray)?.Count==0||
   ((JArray)c["after_original_parent_cleanup"]).Any(x=>(bool?)x["exists"]!=false))
   throw new InvalidDataException("S050 original death Prepare/cleanup evidence changed");
 }
 var expectedRows=new JArray();
 foreach(var token in originals){var links=((JArray)token["visual_readset"]).Where(v=>(bool?)v["has_animator_link"]==true).Select(v=>new JObject{["animator_exists"]=v["animator_exists"]?.DeepClone(),["blob_created"]=v["blob_created"]?.DeepClone(),["animation_count"]=v["animation_count"]?.DeepClone(),["clips"]=v["clips"]?.DeepClone()}).ToArray();
  expectedRows.Add(new JObject{["key"]=token["key"].DeepClone(),["configured_duration_bits"]=token["configured_duration_bits"].DeepClone(),["death_animation_index"]=token["death_animation_index"].DeepClone(),["has_death_animation_selection"]=token["has_death_animation_selection"].DeepClone(),["configured_wreck_bypasses_prepare"]=token["configured_wreck_bypasses_prepare"].DeepClone(),["legacy_wreck_bypasses_prepare"]=token["legacy_wreck_bypasses_prepare"].DeepClone(),["linked_animation_inputs"]=new JArray(links)});
 }
 if(!JToken.DeepEquals(projection["rows"],expectedRows))throw new InvalidDataException("S050 compact death projection changed captured original owner facts");
 var model=Decode(new JObject{["kind"]="S050SourceDiagnosticDeathMechanicalInputs",["native_origin_sha256"]=(string)sourceDesc["sha256"],["rows"]=expectedRows});
 for(int i=0;i<16;i++){
  var original=(JObject)originals[i];string key=(string)keys[i];uint bits=uint.Parse((string)original["configured_duration_bits"],NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture);byte index=checked((byte)(int)original["death_animation_index"]);var row=model.Require(key,bits,index);float configured=BitConverter.ToSingle(BitConverter.GetBytes((int)bits),0);float computed=row.HasSelection?UnitDeathAnimationPlaybackUtility.ResolveQualifiedMechanicalPlayback(configured,row.DeathIndex,row.Inputs):UnitDeathLifecycleKernel.ClampPlaybackDuration(configured);string result=BitConverter.ToUInt32(BitConverter.GetBytes(computed),0).ToString("x8",CultureInfo.InvariantCulture);
  if(result!=(string)original["actual_original_prepare_bits"]||result!=(string)original["original_fold_bits"]||result!=(string)original["unit_death_time_remaining_assignment_bits"])throw new InvalidDataException("S050 captured original Prepare/fold/death-assignment bits differ: "+key);
 }
 return model;
 }
 internal static SourceNativeDeathReadModel LoadS074NoCameraDiagnostic(JObject descriptor){
 const string rawPath="/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/s074-owner-data-capture-20261107-r1/death-16/death-mechanical-readset.json";
 const string rawSha=S074NoCameraRawReadsetSha256;
 const string captureRoot="/Volumes/T7/Projects/WarlineCapture";
 const string keyPath="/Volumes/T7/Projects/WarlineCapture/T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/s074-unit-16-keys.json";
 const string keySha="2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7";
 string[] fields={"path","sha256","capture_root"};if(descriptor==null||descriptor.Count!=fields.Length||descriptor.Properties().Any(p=>!fields.Contains(p.Name)||p.Value.Type!=JTokenType.String)||(string)descriptor["path"]!=rawPath||(string)descriptor["sha256"]!=rawSha||(string)descriptor["capture_root"]!=captureRoot)throw new InvalidDataException("Exact captured S074 death readset descriptor required");
 return LoadS074NoCameraDiagnosticBytes(AutonomousWorld.PinnedFile(rawPath,rawSha,4*1024*1024),descriptor);
 }
 internal static SourceNativeDeathReadModel LoadS074NoCameraDiagnosticBytes(byte[] rawBytes,JObject descriptor){
 const string rawPath="/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/s074-owner-data-capture-20261107-r1/death-16/death-mechanical-readset.json";
 const string rawSha=S074NoCameraRawReadsetSha256;
 const string captureRoot="/Volumes/T7/Projects/WarlineCapture";
 const string keyPath="/Volumes/T7/Projects/WarlineCapture/T Logs/SkirmishProduction/s074-owner-data-capture-inputs-20261107-r1/s074-unit-16-keys.json";
 const string keySha="2e207fab1547fd55d56ddbb9919c73da4c942ccf2b85dd6d8619a5cf6bc481f7";
 string[] fields={"path","sha256","capture_root"};if(descriptor==null||descriptor.Count!=fields.Length||descriptor.Properties().Any(p=>!fields.Contains(p.Name)||p.Value.Type!=JTokenType.String)||(string)descriptor["path"]!=rawPath||(string)descriptor["sha256"]!=rawSha||(string)descriptor["capture_root"]!=captureRoot)throw new InvalidDataException("Exact captured S074 death readset descriptor required");
 if(rawBytes==null||rawBytes.Length<1||rawBytes.Length>4*1024*1024||AutonomousWorld.Hash(rawBytes)!=rawSha)throw new InvalidDataException("Captured S074 death readset bytes changed");var raw=JObject.Parse(System.Text.Encoding.UTF8.GetString(rawBytes),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
 if((int?)raw["schema_version"]!=1||(string)raw["kind"]!="NativeSelectedPrefabDeathMechanicalReadset"||(string)raw["authority"]!="OriginalNativePrepare_OriginalParentSystem_OriginalKernel_NotMissionState"||(string)raw["keys_sha256"]!=keySha||(bool?)raw["full_mission_initialized"]!=false||(bool?)raw["renderer_qualified"]!=false||(bool?)raw["runtime_admitted"]!=false)throw new InvalidDataException("S074 death capture scope/authority changed");
 var pins=raw["source_pins"] as JObject;if(pins==null||pins.Count!=8091||(string)pins[keyPath]!=keySha)throw new InvalidDataException("Exact S074 death source pin closure required");GeneratedReferenceTemplateView.VerifyS074SourcePins(pins,captureRoot,8091);
 var keyBytes=AutonomousWorld.PinnedFile(keyPath,keySha,4096);var keys=JArray.Parse(System.Text.Encoding.UTF8.GetString(keyBytes),new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});var originals=raw["rows"] as JArray;if(keys.Count!=16||keys.Any(k=>k.Type!=JTokenType.String)||keys.Select(k=>(string)k).Distinct(StringComparer.Ordinal).Count()!=16||originals==null||originals.Count!=16)throw new InvalidDataException("Exact S074 selected-16 death domain required");
 var projectedRows=new JArray();for(int i=0;i<16;i++){
  var original=originals[i] as JObject;var originalKey=original?["key"];if(original==null||originalKey==null||originalKey.Type!=JTokenType.String||(string)originalKey!=(string)keys[i])throw new InvalidDataException("S074 death row/key order mismatch");
  if(!JToken.DeepEquals(original["actual_original_prepare_bits"],original["reversed_child_original_prepare_bits"]))throw new InvalidDataException("S074 original death child-order control failed");
  var visual=original["visual_readset"] as JArray;if(visual==null)throw new InvalidDataException("S074 native visual readset absent");var links=visual.Where(v=>(bool?)v["has_animator_link"]==true).Select(v=>new JObject{["animator_exists"]=v["animator_exists"]?.DeepClone(),["blob_created"]=v["blob_created"]?.DeepClone(),["animation_count"]=v["animation_count"]?.DeepClone(),["clips"]=v["clips"]?.DeepClone()}).ToArray();
  projectedRows.Add(new JObject{["key"]=original["key"].DeepClone(),["configured_duration_bits"]=original["configured_duration_bits"].DeepClone(),["death_animation_index"]=original["death_animation_index"].DeepClone(),["has_death_animation_selection"]=original["has_death_animation_selection"].DeepClone(),["configured_wreck_bypasses_prepare"]=original["configured_wreck_bypasses_prepare"].DeepClone(),["legacy_wreck_bypasses_prepare"]=original["legacy_wreck_bypasses_prepare"].DeepClone(),["linked_animation_inputs"]=new JArray(links)});
 }
 var projection=new JObject{["kind"]="NativeQualifiedDeathMechanicalInputs",["native_origin_sha256"]=rawSha,["full_mission_initialized"]=false,["renderer_qualified"]=false,["runtime_admitted"]=false,["rows"]=projectedRows};var model=Decode(projection);
 for(int i=0;i<16;i++){
  var original=(JObject)originals[i];string key=(string)keys[i];uint bits=uint.Parse((string)original["configured_duration_bits"],NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture);byte index=checked((byte)(int)original["death_animation_index"]);var row=model.Require(key,bits,index);float configured=BitConverter.ToSingle(BitConverter.GetBytes((int)bits),0);float computed=row.HasSelection?UnitDeathAnimationPlaybackUtility.ResolveQualifiedMechanicalPlayback(configured,row.DeathIndex,row.Inputs):UnitDeathLifecycleKernel.ClampPlaybackDuration(configured);string computedBits=BitConverter.ToUInt32(BitConverter.GetBytes(computed),0).ToString("x8",CultureInfo.InvariantCulture);
  if(computedBits!=(string)original["actual_original_prepare_bits"]||computedBits!=(string)original["original_fold_bits"]||computedBits!=(string)original["unit_death_time_remaining_assignment_bits"])throw new InvalidDataException("S074 captured original Prepare/fold/death-assignment bits differ: "+key);
 }
 return model;
 }
 internal string[] QualifiedPrefabKeys=>rows.Keys.OrderBy(k=>k,StringComparer.Ordinal).ToArray();
 internal Row Require(string key,uint configuredBits,byte deathIndex){if(!rows.TryGetValue(key,out var row)||row.ConfiguredBits!=configuredBits||row.DeathIndex!=deathIndex)throw new NotSupportedException("Actual prefab/death authoring input changed: key="+key+" actualConfiguredBits="+configuredBits.ToString("x8")+" actualDeathIndex="+deathIndex+" expectedConfiguredBits="+(row==null?"<unknown-prefab>":row.ConfiguredBits.ToString("x8"))+" expectedDeathIndex="+(row==null?"<unknown-prefab>":row.DeathIndex.ToString()));return row;}
}}
