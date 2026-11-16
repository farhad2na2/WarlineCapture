#!/usr/bin/env python3
"""Compare S074 source-owner binding with the actual source reader decoder/compiler."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HOST = ROOT / 'T Logs/SkirmishProduction/source-map-alias-s074-build-20261107-r11'
PROJECTION = ROOT / 'T Logs/SkirmishProduction/s074-declared-logical-binding-portable-20261107-r6'
R9 = ROOT / 'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9'
CODEC = ROOT / 'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(ok, message):
    if not ok:
        raise ValueError(message)


def extract_method(source, signature):
    start = source.find(signature)
    need(start >= 0 and source.find(signature, start + len(signature)) < 0,
         'Exact original PortableLogicalValue method required')
    brace = source.find('{', start + len(signature))
    depth = 0
    for index in range(brace, len(source)):
        if source[index] == '{':
            depth += 1
        elif source[index] == '}':
            depth -= 1
            if depth == 0:
                return source[start:index + 1]
    raise ValueError('Unclosed original codec method')


CSHARP = r'''using System;using System.IO;using System.Linq;using System.Collections.Generic;
using Game.Configs;using Game.Runtime;using Game.Skirmish.Contracts;using Newtonsoft.Json.Linq;using UnityEngine;
internal static class S074DeclaredBindingDifferentialControl {
 static string FirstDiff(JToken left,JToken right,string path){if(JToken.DeepEquals(left,right))return null;if(left is JObject a&&right is JObject b){foreach(var name in a.Properties().Select(p=>p.Name).Union(b.Properties().Select(p=>p.Name)).OrderBy(x=>x,StringComparer.Ordinal)){if(a[name]==null||b[name]==null)return path+"."+name;var diff=FirstDiff(a[name],b[name],path+"."+name);if(diff!=null)return diff;}}if(left is JArray x&&right is JArray y){if(x.Count!=y.Count)return path+".length";for(int i=0;i<x.Count;i++){var diff=FirstDiff(x[i],y[i],path+"["+i+"]");if(diff!=null)return diff;}}return path;}
 static JArray Rows(Array rows){var output=new JArray();if(rows==null)return output;foreach(var row in rows){var fields=new JObject();foreach(var field in row.GetType().GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.DeclaredOnly).OrderBy(x=>x.Name,StringComparer.Ordinal)){object value=field.GetValue(row);fields[field.Name]=value==null?JValue.CreateNull():field.FieldType.IsEnum?new JValue(Convert.ToInt32(value)):JToken.FromObject(value);}output.Add(fields);}return output;}
 static JArray LoadedAssemblies(){var output=new JArray();foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x=>!x.IsDynamic).OrderBy(x=>x.FullName,StringComparer.Ordinal)){string path=assembly.Location;string hash=null;if(!String.IsNullOrEmpty(path)&&File.Exists(path)){using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}output.Add(new JObject{["full_name"]=assembly.FullName,["location"]=path,["sha256"]=hash});}return output;}
 static SkirmishScenarioDefinitionConfig Definition(SourceConfigMaterializer values,JObject profiles,SkirmishMapLayoutConfig layout,OperationMapDefinition source,SkirmishMissionLogicalBindingConfig binding){var d=new SkirmishScenarioDefinitionConfig();d.Configure("S074",values.ByPath<SkirmishObjectiveConfig>((string)profiles["objective"]),values.ByPath<SkirmishArmyProfileConfig>((string)profiles["army"]),values.ByPath<SkirmishStartPackageConfig>((string)profiles["start"]),values.ByPath<SkirmishEconomyConfig>((string)profiles["economy"]),values.ByPath<SkirmishUpgradeConfig>((string)profiles["upgrades"]),values.ByPath<SkirmishIntelConfig>((string)profiles["intel"]),values.ByPath<SkirmishRoleCatalogConfig>((string)profiles["roles"]),layout);d.BindCampaignMap(source);if(binding!=null)d.BindMissionLogicalBinding(binding);return d;}
 static JObject Compile(string name,SkirmishScenarioDefinitionConfig definition,List<SkirmishSetupMatrixRow> matrix){var manifest=new SkirmishContentManifest{RequiredFeatureIds=definition.RequiredFeatureIds};if(!SkirmishSetupCompiler.TryCompile(definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104803,manifest,matrix,out var setup,out var reasons))return new JObject{["owner"]=name,["compiled"]=false,["reasons"]=new JArray(reasons.Select(x=>x.ToString()))};bool logicalOk=SkirmishMissionLogicalBindingCompiler.TryCompile(definition,setup,out var logical,out var error);string fullSource=SkirmishBindingCanonicalValues.FullSourceFingerprint(definition.CampaignMap);string layout=SkirmishBindingCanonicalValues.LayoutFingerprint(definition.MapLayout);string actors=SkirmishBindingCanonicalValues.ProjectedActorsFingerprint(setup.Forces,setup.Structures,null,false);return new JObject{["owner"]=name,["compiled"]=true,["setup_hash"]=(long)setup.SetupHash,["seed"]=setup.Seed,["catalog_id"]=setup.CatalogId,["definition_id"]=setup.DefinitionId,["layout_binding_fingerprint"]=setup.LayoutBindingFingerprint,["full_source_fingerprint"]=fullSource,["layout_fingerprint"]=layout,["projected_actors_fingerprint"]=actors,["forces"]=Rows(setup.Forces),["structures"]=Rows(setup.Structures),["logical_binding_fingerprint"]=setup.MissionLogicalBindingFingerprint,["logical_compile_succeeded"]=logicalOk,["compiled_logical_fingerprint"]=logical?.Fingerprint,["logical_compile_error"]=error,["content_hash"]=setup.ContentHash,["player_infantry"]=setup.PlayerInfantry,["player_ground"]=setup.PlayerGround,["tank_count"]=setup.PlayerTankCount(),["materials_each"]=setup.MaterialsEach,["deadline_seconds"]=setup.DeadlineSeconds,["force_count"]=setup.Forces?.Length??0,["structure_count"]=setup.Structures?.Length??0};}
 static int Main(string[] args){try{if(args.Length!=7)throw new ArgumentException("Expected graph,layout,matrix,raw binding,portable binding,output directory,context.");var graph=JObject.Parse(File.ReadAllText(args[0]));var profiles=new JObject{["objective"]="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishObjective_BA.asset",["army"]="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishArmy_GroundManeuver.asset",["start"]="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishStart_Established.asset",["economy"]="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishEconomy_V1.asset",["upgrades"]="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishUpgrade_V1.asset",["intel"]="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishIntel_SharedFog.asset",["roles"]="Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishRoleCatalog_V1.asset"};var raw=JObject.Parse(File.ReadAllText(args[3]));var portable=JObject.Parse(File.ReadAllText(args[4]));var context=JObject.Parse(File.ReadAllText(args[6]));var target=context["target_context"];
 var ownerValues=new SourceConfigMaterializer(graph);var ownerLayout=ownerValues.LogicalLayout(JObject.Parse(File.ReadAllText(args[1])));var ownerSource=ownerValues.ByPath<OperationMapDefinition>("Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");var ownerBinding=ownerValues.LogicalBinding(raw);
 var decoded=SourceNativeMissionLogicalBindingReference.DecodeDeclaredFields(portable["fields"]);var ownerPortable=PortableLogicalValue(ownerBinding,0);var decodedPortable=PortableLogicalValue(decoded,0);string portableDiff=FirstDiff(ownerPortable,decodedPortable,"fields");
 var expansion=ownerValues.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset");UnityEngine.Resources.BindSourceResources(new Dictionary<string,object>{{SkirmishExpansionCatalogConfig.ResourceName,expansion},{SkirmishSetupMatrixTable.PackagedResourceName,new TextAsset(File.ReadAllText(args[2]))}});if(!SkirmishSetupMatrixTable.TryLoadPackaged(out var matrix,out var matrixError))throw new InvalidOperationException(matrixError);
 var seqValues=new SourceConfigMaterializer(graph);var seqLayout=seqValues.LogicalLayout(JObject.Parse(File.ReadAllText(args[1])));var seqSource=seqValues.ByPath<OperationMapDefinition>("Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");var seqDefinition=Definition(seqValues,profiles,seqLayout,seqSource,null);var seqManifest=new SkirmishContentManifest{RequiredFeatureIds=seqDefinition.RequiredFeatureIds};bool seqPrecompiled=SkirmishSetupCompiler.TryCompile(seqDefinition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104803,seqManifest,matrix,out var seqPreSetup,out var seqPreReasons);if(!seqPrecompiled)throw new InvalidOperationException("Exact same-sequence initial compile failed: "+String.Join(";",seqPreReasons.Select(x=>x.ToString())));SkirmishMissionLogicalAuthoringInputs seqInputs=null;bool seqCaptured=SkirmishSetupCompiler.TryCaptureMissionLogicalAuthoringInputs(seqDefinition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104803,seqManifest,matrix,out seqInputs,out var seqCaptureReasons)&&seqInputs!=null;if(!seqCaptured)throw new InvalidOperationException("Exact same-sequence authoring-input capture failed: "+String.Join(";",seqCaptureReasons.Select(x=>x.ToString())));var seqBinding=seqValues.LogicalBinding(raw);seqDefinition.BindMissionLogicalBinding(seqBinding);var seqResult=Compile("owner-sequence-precompile-capture-then-bind",seqDefinition,matrix);var ownerDefinition=Definition(ownerValues,profiles,ownerLayout,ownerSource,ownerBinding);var decodedValues=new SourceConfigMaterializer(graph);var decodedLayout=decodedValues.LogicalLayout(JObject.Parse(File.ReadAllText(args[1])));var decodedSource=decodedValues.ByPath<OperationMapDefinition>("Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");var decodedDefinition=Definition(decodedValues,profiles,decodedLayout,decodedSource,decoded);var ownerResult=Compile("source-owner-materialized-raw",ownerDefinition,matrix);var decodedResult=Compile("actual-reader-decoded-portable",decodedDefinition,matrix);
 var tampered=(JObject)portable.DeepClone();string bits=(string)tampered["fields"]?["Variants"]?[0]?["View"]?["Pitch"]?["float_bits"];if(string.IsNullOrEmpty(bits))throw new InvalidDataException("Exact portable view pitch bits required");tampered["fields"]["Variants"][0]["View"]["Pitch"]["float_bits"]=bits.Substring(0,7)+(bits[7]=='0'?'1':'0');SkirmishMissionLogicalBindingConfig negativeBinding=null;bool negativeRejected=false;string negativeError=null;try{negativeBinding=SourceNativeMissionLogicalBindingReference.DecodeDeclaredFields(tampered["fields"]);var negativeValues=new SourceConfigMaterializer(graph);var negativeLayout=negativeValues.LogicalLayout(JObject.Parse(File.ReadAllText(args[1])));var negativeSource=negativeValues.ByPath<OperationMapDefinition>("Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset");var negativeDefinition=Definition(negativeValues,profiles,negativeLayout,negativeSource,negativeBinding);var manifest=new SkirmishContentManifest{RequiredFeatureIds=negativeDefinition.RequiredFeatureIds};negativeRejected=!SkirmishSetupCompiler.TryCompile(negativeDefinition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,104803,manifest,matrix,out _,out var negativeReasons);negativeError=String.Join(";",negativeReasons.Select(x=>x.ToString()));}catch(Exception error){negativeRejected=true;negativeError=error.GetType().Name+":"+error.Message;}
 var output=new JObject{["kind"]="S074DeclaredLogicalBindingDifferentialControl",["status"]="PassedObservedDifferential",["portable_owner_vs_reader_first_difference"]=portableDiff,["portable_owner_and_reader_fields_equal"]=portableDiff==null,["owner_source_binding_sha256"]=AutonomousWorld.Hash(File.ReadAllBytes(args[3])),["portable_document_sha256"]=AutonomousWorld.Hash(File.ReadAllBytes(args[4])),["owner_compiler"]=ownerResult,["reader_decoded_compiler"]=decodedResult,["same_sequence_prebind_setup_hash"]=(long)seqPreSetup.SetupHash,["same_sequence_prebind_layout_binding_fingerprint"]=seqPreSetup.LayoutBindingFingerprint,["same_sequence_prebind_full_source_fingerprint"]=SkirmishBindingCanonicalValues.FullSourceFingerprint(seqSource),["same_sequence_prebind_layout_fingerprint"]=SkirmishBindingCanonicalValues.LayoutFingerprint(seqLayout),["same_sequence_prebind_projected_actors_fingerprint"]=SkirmishBindingCanonicalValues.ProjectedActorsFingerprint(seqPreSetup.Forces,seqPreSetup.Structures,null,false),["same_sequence_prebind_forces"]=Rows(seqPreSetup.Forces),["same_sequence_prebind_structures"]=Rows(seqPreSetup.Structures),["same_sequence_capture_succeeded"]=seqCaptured,["same_sequence_capture_source_fingerprint"]=seqInputs.SourceSemanticFingerprint,["same_sequence_capture_layout_fingerprint"]=seqInputs.LayoutSemanticFingerprint,["same_sequence_bound_compiler"]=seqResult,["same_sequence_matches_direct_bound_hash"]=(long)seqResult["setup_hash"]==(long)ownerResult["setup_hash"],["loaded_assemblies"]=LoadedAssemblies(),["candidate_target_setup_hash"]=(uint?)target?["setup_hash"],["candidate_target_layout_binding_fingerprint"]=(string)target?["layout_binding_fingerprint"],["candidate_target_logical_fingerprint"]=(string)target?["mission_logical_binding_fingerprint"],["single_float_bit_tamper_rejected"]=negativeRejected,["single_float_bit_tamper_error"]=negativeError,["world_allocated"]=false,["native_capture"]=false,["source_fit_certified"]=false,["mission_ready"]=false};Directory.CreateDirectory(args[5]);File.WriteAllText(Path.Combine(args[5],"differential.json"),output.ToString(Newtonsoft.Json.Formatting.Indented)+"\n");Console.WriteLine("[S074DeclaredBindingDifferentialControl] result=PassedObservedDifferential firstDifference="+(portableDiff??"none")+" directHash="+ownerResult["setup_hash"]+" sequentialHash="+seqResult["setup_hash"]+" prebindHash="+seqPreSetup.SetupHash+" tamperRejected="+negativeRejected+" world=0");return 0;}catch(Exception error){Console.Error.WriteLine(error);return 1;}}}
''' 


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    out = args.output_dir.resolve()
    need(out.is_relative_to(ROOT) and not out.exists(), 'Fresh T output directory required')
    build = json.loads((HOST / 'build.json').read_bytes())
    pins = build['source_and_reference_pins']
    need(build.get('status') == 'Passed' and build.get('mission') == 'S074' and build.get('seed') == 104803,
         'Current proof-bearing S074 host build required')
    for path, expected in pins.items():
        need(sha(path) == expected, f'Host source pin drift: {path}')
    csc = next(Path(path) for path in pins if path.endswith('/Roslyn/bincore/csc.dll'))
    owner_dir = PROJECTION / 'owner'
    raw_path = owner_dir / 'mission-logical-binding.json'
    portable_path = owner_dir / 'portable-binding.json'
    context_path = R9 / 'exporter/s074/source-factory-context.json'
    author = json.loads((R9 / 'exporter/s074/authoring-request.json').read_bytes())
    graph_path = Path(author['graph']['path']).resolve(strict=True)
    layout_path = Path(author['layout']['path']).resolve(strict=True)
    graph = json.loads(graph_path.read_bytes())
    matrix_path = Path(graph['setup_matrix']['path']).resolve(strict=True)
    for path in (raw_path, portable_path, context_path, graph_path, layout_path, matrix_path):
        need(path.is_file(), f'Exact differential input required: {path}')
    need(sha(raw_path) == json.loads((R9 / 'candidate/receipt.json').read_bytes())['binding_sha256'],
         'S074 owner raw binding differs from retained r9')
    need(json.loads((PROJECTION / 'receipt.json').read_bytes())['status'] == 'ProjectedSourceOnly',
         'Successful exact-codec portable projection required')

    out.mkdir(parents=True)
    source = out / 'S074DeclaredBindingDifferentialControl.cs'
    codec_method = extract_method(CODEC.read_text(), 'static JToken PortableLogicalValue(')
    source.write_text(CSHARP.replace(' static string FirstDiff', ' ' + codec_method + '\n static string FirstDiff', 1))
    response = [json.loads(line) for line in (HOST / 'compile.rsp').read_text().splitlines() if line.strip()]
    old_main = '-main:SourceSimulationProgram'
    old_output = '-out:' + str(HOST / 'SourceSimulation.exe')
    need(response.count(old_main) == 1 and response.count(old_output) == 1,
         'Exact r11 Roslyn response required')
    exe = out / 'S074DeclaredBindingDifferentialControl.exe'
    response = [('-main:S074DeclaredBindingDifferentialControl' if row == old_main else
                 '-out:' + str(exe) if row == old_output else row) for row in response]
    response.append(str(source))
    rsp = out / 'compile.rsp'
    rsp.write_text(''.join(json.dumps(row) + '\n' for row in response))
    compile_result = subprocess.run(['/usr/local/share/dotnet/dotnet', str(csc), '@' + str(rsp)],
                                    cwd=ROOT, capture_output=True, text=True, timeout=300)
    (out / 'compile.log').write_text(compile_result.stdout + compile_result.stderr)
    need(compile_result.returncode == 0 and exe.is_file(), 'Differential control Roslyn compile failed')
    runtime = build['runtime_selection']
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((runtime['engine'], str(Path(runtime['newtonsoft']).parent),
                                        str(ROOT / 'Library/ScriptAssemblies'), str(HOST / 'placement'), str(out)))
    run = subprocess.run([runtime['mono'], '--optimize=-float32', str(exe), str(graph_path), str(layout_path), str(matrix_path),
                          str(raw_path), str(portable_path), str(out), str(context_path)],
                         cwd=ROOT, env=env, capture_output=True, text=True, timeout=600)
    (out / 'control.log').write_text(run.stdout + run.stderr)
    need(run.returncode == 0 and '[S074DeclaredBindingDifferentialControl] result=PassedObservedDifferential' in run.stdout,
         'Differential control did not produce complete evidence')

    # Instrument the original owner source without changing its compile or bind sequence.
    owner_out = out / 'owner-replay'
    owner_out.mkdir()
    owner_source = PROJECTION / 'owner/S074CameraBindingAuthor.cs'
    owner_text = owner_source.read_text()
    helper_marker = '    private static JObject Serialize('
    need(owner_text.count(helper_marker) == 1, 'Exact owner serializer insertion anchor required')
    helpers = r'''    private static JArray FingerprintRows(Array rows){var output=new JArray();if(rows==null)return output;foreach(var row in rows){var fields=new JObject();foreach(var field in row.GetType().GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).OrderBy(x=>x.Name,StringComparer.Ordinal)){object value=field.GetValue(row);fields[field.Name]=value==null?JValue.CreateNull():field.FieldType.IsEnum?new JValue(Convert.ToInt32(value)):JToken.FromObject(value);}output.Add(fields);}return output;}
    private static JObject Fingerprints(SkirmishScenarioDefinitionConfig definition,OperationMapDefinition source,SkirmishMapLayoutConfig layout,SkirmishResolvedSetup setup){return new JObject{["setup_hash"]=(long)setup.SetupHash,["layout_binding_fingerprint"]=setup.LayoutBindingFingerprint,["full_source_fingerprint"]=SkirmishBindingCanonicalValues.FullSourceFingerprint(source),["layout_fingerprint"]=SkirmishBindingCanonicalValues.LayoutFingerprint(layout),["projected_actors_fingerprint"]=SkirmishBindingCanonicalValues.ProjectedActorsFingerprint(setup.Forces,setup.Structures,null,false),["forces"]=FingerprintRows(setup.Forces),["structures"]=FingerprintRows(setup.Structures)};}
    private static JArray LoadedAssemblies(){var output=new JArray();foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(x=>!x.IsDynamic).OrderBy(x=>x.FullName,StringComparer.Ordinal)){string path=assembly.Location;string hash=null;if(!String.IsNullOrEmpty(path)&&File.Exists(path)){using(var sha=System.Security.Cryptography.SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}output.Add(new JObject{["full_name"]=assembly.FullName,["location"]=path,["sha256"]=hash});}return output;}
'''
    owner_text = owner_text.replace(helper_marker, helpers + '\n' + helper_marker, 1)
    owner_anchor = '            var outDir = Path.GetFullPath(args[3]);'
    need(owner_text.count(owner_anchor) == 1, 'Exact original owner post-bind insertion anchor required')
    owner_text = owner_text.replace(owner_anchor,
        '            var ownerDiagnostics = new JObject { ["unbound"] = Fingerprints(definition, source, layout, setup), ["bound"] = Fingerprints(definition, source, layout, bound), ["loaded_assemblies"] = LoadedAssemblies() };\n' + owner_anchor, 1)
    write_anchor = '            Directory.CreateDirectory(outDir);'
    need(owner_text.count(write_anchor) == 1, 'Exact owner output directory anchor required')
    owner_text = owner_text.replace(write_anchor, write_anchor +
        '\n            File.WriteAllText(Path.Combine(outDir, "owner-diagnostics.json"), ownerDiagnostics.ToString(Newtonsoft.Json.Formatting.Indented) + "\\n");', 1)
    instrumented_owner_source = owner_out / 'S074CameraBindingAuthor.instrumented.cs'
    instrumented_owner_source.write_text(owner_text)
    original_response = [json.loads(line) for line in (PROJECTION / 'owner/compile.rsp').read_text().splitlines() if line.strip()]
    original_exe = str((PROJECTION / 'owner/source-owner.exe').resolve())
    need(original_response.count('-out:' + original_exe) == 1 and
         original_response.count(str(owner_source.resolve())) == 1,
         'Exact r6 original owner compile response required')
    owner_exe = owner_out / 'source-owner-instrumented.exe'
    owner_response = [('-out:' + str(owner_exe) if row == '-out:' + original_exe else
                       str(instrumented_owner_source) if row == str(owner_source.resolve()) else row)
                      for row in original_response]
    owner_rsp = owner_out / 'compile.rsp'
    owner_rsp.write_text(''.join(json.dumps(row) + '\n' for row in owner_response))
    owner_compile = subprocess.run(['/usr/local/share/dotnet/dotnet', str(csc), '@' + str(owner_rsp)],
                                   cwd=ROOT, capture_output=True, text=True, timeout=300)
    (owner_out / 'compile.log').write_text(owner_compile.stdout + owner_compile.stderr)
    need(owner_compile.returncode == 0 and owner_exe.is_file(), 'Instrumented original-owner compile failed')
    owner_env = os.environ.copy()
    owner_env['MONO_PATH'] = os.pathsep.join((runtime['engine'], str(Path(runtime['newtonsoft']).parent),
                                              str(ROOT / 'Library/ScriptAssemblies'), str(HOST / 'placement'), str(owner_out)))
    owner_run = subprocess.run([runtime['mono'], '--optimize=-float32', str(owner_exe),
                                 str(graph_path), str(layout_path), str(matrix_path), str(owner_out)],
                                cwd=ROOT, env=owner_env, capture_output=True, text=True, timeout=600)
    (owner_out / 'run.stdout.log').write_text(owner_run.stdout)
    (owner_out / 'run.stderr.log').write_text(owner_run.stderr)
    need(owner_run.returncode == 0 and '[S074CameraBindingAuthor] result=PassedSourceOnlyCandidate' in owner_run.stdout,
         'Instrumented exact original-owner replay did not pass')
    actual_owner_diagnostics = json.loads((owner_out / 'owner-diagnostics.json').read_bytes())
    need(actual_owner_diagnostics['bound']['setup_hash'] == 1298808707,
         'Instrumented exact owner did not reproduce retained r6 target setup hash')

    differential = json.loads((out / 'differential.json').read_bytes())
    receipt = {'kind': 'S074DeclaredLogicalBindingDifferentialControlReceipt', 'status': differential['status'],
               'build': {'path': str((HOST / 'build.json').resolve()), 'sha256': sha(HOST / 'build.json')},
               'source_pins': pins, 'owner_binding': {'path': str(raw_path), 'sha256': sha(raw_path)},
               'portable_binding': {'path': str(portable_path), 'sha256': sha(portable_path)},
               'context': {'path': str(context_path), 'sha256': sha(context_path)},
               'differential': {'path': str((out / 'differential.json').resolve()), 'sha256': sha(out / 'differential.json')},
               'owner_diagnostics': {'path': str((owner_out / 'owner-diagnostics.json').resolve()), 'sha256': sha(owner_out / 'owner-diagnostics.json')},
               'control_executable': {'path': str(exe.resolve()), 'sha256': sha(exe)},
               'compile_exit': compile_result.returncode, 'control_exit': run.returncode,
               'owner_compile_exit': owner_compile.returncode, 'owner_replay_exit': owner_run.returncode,
               'world_allocated': False, 'native_capture': False, 'mission_ready': False}
    (out / 'receipt.json').write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n')
    print(json.dumps({'status': differential['status'], 'first_difference': differential['portable_owner_vs_reader_first_difference'],
                      'owner_setup_hash': differential['owner_compiler'].get('setup_hash'),
                      'decoded_setup_hash': differential['reader_decoded_compiler'].get('setup_hash'),
                      'instrumented_owner_setup_hash': actual_owner_diagnostics['bound']['setup_hash'],
                      'owner_fingerprint': differential['owner_compiler'].get('logical_binding_fingerprint'),
                      'decoded_fingerprint': differential['reader_decoded_compiler'].get('logical_binding_fingerprint'),
                      'tamper_rejected': differential['single_float_bit_tamper_rejected']}))


if __name__ == '__main__':
    main()
