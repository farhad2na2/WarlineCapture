#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEditor;using UnityEngine;using Newtonsoft.Json.Linq;
namespace Game.Editor {public static partial class SkirmishBakedActorSnapshotExporter {
 const string EpochReadsetSha="3659cba61a42bae281de94d59e821ef1699576ec427a68160e741bf2767e01a4";
 static string EpochArg(string name){var a=Environment.GetCommandLineArgs();int at=Array.IndexOf(a,name);if(at<0||at+1>=a.Length||!Path.IsPathRooted(a[at+1]))throw new ArgumentException("Explicit absolute "+name+" required");return Path.GetFullPath(a[at+1]);}
 static JObject EpochHashes(IEnumerable<string> names){var j=new JObject();foreach(string p in names.OrderBy(v=>v,StringComparer.Ordinal)){if(j[p]!=null)throw new InvalidDataException("Duplicate epoch input");j[p]=SkirmishOfflineDataExporter.HashFile(p);}return j;}
 public static void RunPipelineEpochMetadataQualification(){
 if(EditorApplication.isPlayingOrWillChangePlaymode||Application.unityVersion!="6000.5.8f1")throw new InvalidOperationException("Idle original qualified Editor6000.5.8f1 required");
 string readset=EpochArg("-skirmishEpochReadset"),output=EpochArg("-skirmishEpochOutput"),s2=EpochArg("-skirmishTwoSeedMetadata"),s26=EpochArg("-skirmishS026LogicalBindingOutput"),layout=EpochArg("-skirmishS026MetadataLayout");
 if(Directory.Exists(output)||Directory.Exists(s2)||Directory.Exists(s26)||output==s2||output==s26||s2==s26)throw new IOException("Distinct fresh epoch/output directories required");
 if(SkirmishOfflineDataExporter.HashFile(readset)!=EpochReadsetSha)throw new InvalidDataException("Exact finite original native source readset required");var manifest=JObject.Parse(File.ReadAllText("Packages/manifest.json"));if((string)manifest["dependencies"]?["com.unity.pipeline"]!="0.9.1-exp.1")throw new InvalidDataException("Actual resolved authorized Pipeline0.9.1-exp.1 required");
 var locked=JObject.Parse(File.ReadAllText("Packages/packages-lock.json"));if((string)locked["dependencies"]?["com.unity.pipeline"]?["version"]!="0.9.1-exp.1")throw new InvalidDataException("Actual resolved Pipeline lock required");var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.pipeline/package.json");if(package==null||package.version!="0.9.1-exp.1"||!Directory.Exists(package.resolvedPath))throw new InvalidDataException("Actual PackageManager resolved Pipeline required");var packageFiles=Directory.GetFiles(package.resolvedPath,"*",SearchOption.AllDirectories);if(packageFiles.Length<1||packageFiles.Length>4096)throw new InvalidDataException("Bounded actual Pipeline package readset required");
 var list=JObject.Parse(File.ReadAllText(readset));if((string)list["kind"]!="FinitePipelineEpochCaptureReadset"||(list["paths"]as JArray)?.Count!=8547)throw new InvalidDataException("Exact bounded readset required");
 var paths=((JArray)list["paths"]).Select(v=>(string)v).Concat(new[]{readset,layout,"Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.PipelineEpoch.cs","Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.PipelineEpoch.cs.meta","Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs","Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs.meta"}).Concat(packageFiles).Distinct(StringComparer.Ordinal).ToArray();
 var before=EpochHashes(paths);Directory.CreateDirectory(output);WriteCameraReferenceFile(Path.Combine(output,"before-input-pins.json"),before);
 RunTwoSeedMetadataQualification();
 RunS026LogicalBindingQualification();
 var after=EpochHashes(paths);WriteCameraReferenceFile(Path.Combine(output,"after-input-pins.json"),after);if(!JToken.DeepEquals(before,after))throw new InvalidDataException("Epoch capture source/package drift");
 var files=new[]{Path.Combine(s2,"two-seed-metadata.json"),Path.Combine(s26,"logical-binding.json"),Path.Combine(s26,"s026-metadata.json"),Path.Combine(s26,"engineering-profile.json"),Path.Combine(s26,"motion-input.json"),Path.Combine(s26,"native-layout-fields.json"),Path.Combine(s26,"native-layout-float-bits.json")};
 WriteCameraReferenceFile(Path.Combine(output,"epoch-capture.json"),new JObject{["kind"]="OriginalPipelineEpochMetadataCapture",["pipeline_version"]="0.9.1-exp.1",["editor_version"]=Application.unityVersion,["before_after_inputs_equal"]=true,["outputs_sha256"]=EpochHashes(files),["s002_contexts"]=2,["s026_contexts"]=12,["one_s026_definition_within_original_qualifier"]=true,["mission_initialized"]=false,["public_admission_changed"]=false,["qualification_sealed"]=false});
 Debug.Log("[SkirmishPipelineEpochMetadata] result=Passed S002Contexts=2 S026Contexts=12 originalEntries=True sourcePinsUnchanged=True mission=False");
 }
}}
#endif
