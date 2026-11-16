#!/usr/bin/env python3
"""Run original setup compiler on a disposable S006 config-only candidate."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/s003-actual-importer-current-fixture-20261105-T7'
BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
GRAPH = ROOT / 'T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json'
GRAPH_SHA = 'b784ffc6b36260b8c403362b93a741a06c493ebdc09704ffd828e82e9ee64138'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
INVENTORY = ROOT / 'T Logs/SkirmishProduction/all120-source-config-inventory-20261105-T2/inventory.json'
CONTROL = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/S003RefreshedPrefabImportControl.cs'
JSON_SHIM = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/050_SourceConfigJson.cs'
SOURCE = r'''using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using Game.Configs;using Game.Runtime;using Game.Skirmish.Contracts;using Newtonsoft.Json;using Newtonsoft.Json.Linq;
internal sealed class S006FieldOnlyResolver:Newtonsoft.Json.Serialization.DefaultContractResolver{
 protected override List<MemberInfo> GetSerializableMembers(Type t)=>t.GetFields(BindingFlags.Instance|BindingFlags.Public).Cast<MemberInfo>().ToList();}
internal sealed class S006FloatAsDoubleConverter:JsonConverter{
 public override bool CanConvert(Type type)=>type==typeof(float);public override bool CanRead=>false;
 public override object ReadJson(JsonReader r,Type t,object value,JsonSerializer s)=>throw new NotSupportedException();
 public override void WriteJson(JsonWriter w,object value,JsonSerializer s){double v=(double)(float)value;string text=null;for(int digits=15;digits<=17;digits++){var candidate=v.ToString("G"+digits,System.Globalization.CultureInfo.InvariantCulture);if(BitConverter.DoubleToInt64Bits(double.Parse(candidate,System.Globalization.CultureInfo.InvariantCulture))==BitConverter.DoubleToInt64Bits(v)){text=candidate;break;}}if(text==null)throw new InvalidOperationException("No round-trip double formatting");if(text.IndexOf('.')<0&&text.IndexOf('E')<0&&text.IndexOf('e')<0)text+=".0";w.WriteRawValue(text);}}
internal static class S006SourceCandidateControl{
 static void Main(string[] a){var graph=JObject.Parse(File.ReadAllText(a[0]));var source=new SourceConfigMaterializer(graph);
  var combined=source.ByPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");
  var established=source.ByPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S002/SkirmishScenario_S002.asset");
  var catalog=source.ByPath<SkirmishExpansionCatalogConfig>("Assets/Game/Resources/SkirmishExpansionCatalog.asset");UnityEngine.Resources.BindSourceResources(new Dictionary<string,object>{{"SkirmishExpansionCatalog",catalog}});
  if(combined.CatalogId!="S005"||established.CatalogId!="S002"||combined.CampaignMap==null||combined.MapLayout==null||established.StartPackageConfig==null)throw new Exception("Exact existing source definitions required");
  var candidateLayout=combined.MapLayout;bool binaryCandidate=a[4]=="1";
  if(binaryCandidate){candidateLayout=Activator.CreateInstance<SkirmishMapLayoutConfig>();foreach(var f in typeof(SkirmishMapLayoutConfig).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly))f.SetValue(candidateLayout,f.GetValue(combined.MapLayout));var format=typeof(SkirmishMapLayoutConfig).GetField("bindingValueFormat",BindingFlags.Instance|BindingFlags.NonPublic);if(format==null||format.FieldType!=typeof(SkirmishBindingValueFormat))throw new Exception("Exact value format field required");format.SetValue(candidateLayout,SkirmishBindingValueFormat.CanonicalBinaryV2);if(combined.MapLayout.BindingValueFormat!=SkirmishBindingValueFormat.NativeJsonV1||candidateLayout.BindingValueFormat!=SkirmishBindingValueFormat.CanonicalBinaryV2)throw new Exception("Candidate format isolation failed");}
  var candidate=Activator.CreateInstance<SkirmishScenarioDefinitionConfig>();
  candidate.Configure("S006",combined.ObjectiveConfig,combined.ArmyProfileConfig,established.StartPackageConfig,combined.EconomyConfig,combined.UpgradeCatalog,combined.IntelConfig,combined.RoleCatalog,candidateLayout,featureIds:combined.RequiredFeatureIds);
  candidate.BindCampaignMap(combined.CampaignMap);
  var binder=typeof(SkirmishMeasuredLayoutBinder).GetNestedType("LayoutValues",BindingFlags.NonPublic);var factory=binder?.GetMethod("From",BindingFlags.Public|BindingFlags.Static);if(factory==null)throw new Exception("Original layout value projection unavailable");
  var layoutValues=factory.Invoke(null,new object[]{combined.MapLayout});File.WriteAllText(a[3],JsonConvert.SerializeObject(layoutValues,Formatting.None,new JsonSerializerSettings{ContractResolver=new S006FieldOnlyResolver(),Converters=new JsonConverter[]{new S006FloatAsDoubleConverter()}}));
  if(!SkirmishSetupMatrixTable.TryLoadFromText(File.ReadAllText(a[1]),out var rows,out var error))throw new Exception(error);
  var manifest=new SkirmishContentManifest{RequiredFeatureIds=candidate.RequiredFeatureIds};var results=new JArray();
  foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar}){
   try{bool accepted=SkirmishSetupCompiler.TryCompile(candidate,SkirmishDifficultyId.Regular,size,104735,manifest,rows,out var setup,out var reasons);
    var failures=new JArray();foreach(var reason in reasons)failures.Add(reason.ToString());
    var pads=new JArray();bool needsBinding=SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup);if(setup!=null)for(int i=0;i<setup.Structures.Length;i++){var structure=setup.Structures[i];bool resolved=SkirmishStartingGrantPlacementConstraint.TryResolvePad(candidateLayout,setup,i,out var pad);pads.Add(new JObject{["structure_index"]=i,["faction_id"]=structure.FactionId,["structure_id"]=structure.StructureId,["visual_key"]=SkirmishStructureIds.VisualKey(structure.StructureId),["resolved"]=resolved,["pad_id"]=resolved?pad.PadId:null});}
    results.Add(new JObject{["size"]=size.ToString(),["compiled"]=accepted,["setup_hash"]=setup==null?(JToken)JValue.CreateNull():new JValue(setup.SetupHash),["setup"]=setup==null?(JToken)JValue.CreateNull():JObject.FromObject(setup),["requires_logical_binding"]=needsBinding,["logical_binding_present"]=candidate.MissionLogicalBinding!=null,["source_pad_resolution"]=pads,["reasons"]=failures});}
   catch(NotSupportedException e){results.Add(new JObject{["size"]=size.ToString(),["compiled"]=false,["source_host_boundary"]=e.Message});}}
  var result=new JObject{["kind"]="S006DisposableConfigCandidateOriginalCompiler",["scope"]="Hypothetical source-only candidate using existing S005 layout values and Campaign reference and S002 start profile; no authored S006 definition, native source fit or runtime admission",["layout_value_format"]=candidateLayout.BindingValueFormat.ToString(),["source_layout_value_format"]=combined.MapLayout.BindingValueFormat.ToString(),["format_changed_for_candidate"]=binaryCandidate,["candidate_only"]=true,["mission_ready"]=false,["rows"]=results};
  File.WriteAllText(a[2],result.ToString(Formatting.Indented));Console.WriteLine("[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0");}}
'''


def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);p.add_argument('--fixture',type=Path,default=BASE);p.add_argument('--canonical-v2-candidate',action='store_true');options=p.parse_args();out=options.output.resolve();base=options.fixture.resolve(strict=True)
    if not out.is_relative_to(ROOT):raise ValueError('Output must be inside repository')
    if not base.is_relative_to(ROOT):raise ValueError('Source fixture must be inside repository')
    out.mkdir(parents=True,exist_ok=False)
    if sha(GRAPH)!=GRAPH_SHA:raise ValueError('Exact source graph required')
    fixture=json.loads((base/'receipt.json').read_bytes());graph=json.loads(GRAPH.read_bytes());inventory=json.loads(INVENTORY.read_bytes())
    if fixture.get('status')!='PassedDisposableS003ImporterFixture' or inventory['input_sha256'].get(str(MATRIX))!=sha(MATRIX):raise ValueError('Current source evidence required')
    s006=next(m for m in inventory['missions'] if m['catalog_id']=='S006')
    if s006['catalog']['MapId']!='DB' or s006['catalog']['ArmyProfile']!='C' or s006['catalog']['StartProfile']!='E' or s006['catalog']['ObjectiveId']!='BA' or s006['catalog_definition_bound'] or s006['scenario_directory_present']:raise ValueError('Exact unbound coded S006 identity required')
    pins=dict(fixture['source_pins'])
    for name,digest in graph['source_pins'].items():
        if name in pins and pins[name]!=digest:raise ValueError('Conflicting source pins: '+name)
        pins[name]=digest
    for path in (base/'receipt.json',base/'compile.rsp',BUILD,GRAPH,MATRIX,INVENTORY,JSON_SHIM,Path(__file__).resolve()):pins[str(path)]=sha(path)
    def verify():
        for name,digest in pins.items():
            if not Path(name).is_file() or sha(Path(name))!=digest:raise ValueError('Input changed: '+name)
    verify();control=out/'S006SourceCandidateControl.cs';control.write_text(SOURCE)
    args=[json.loads(line) for line in (base/'compile.rsp').read_text().splitlines() if line.strip()]
    if args.count(str(CONTROL))!=1:raise ValueError('Exact source fixture closure required')
    shim=JSON_SHIM.read_text();seam='public static string ToJson(object value)=>throw new NotSupportedException('
    if shim.count(seam)!=1 or args.count(str(JSON_SHIM))!=1:raise ValueError('Exact original JSON boundary required')
    # Read-only observation before the same original exception; no JSON result is supplied.
    shim=shim.replace(seam,'public static string ToJson(object value){Console.Error.WriteLine("[S006JsonBoundary] value_type="+(value==null?"<null>":value.GetType().FullName));return ThrowJson();} static string ThrowJson()=>throw new NotSupportedException(',1)
    observed=out/'ObservedSourceConfigJson.cs';observed.write_text(shim)
    args=[str(observed) if x==str(JSON_SHIM) else x for x in args]
    args=[str(control) if x==str(CONTROL) else '-main:S006SourceCandidateControl' if x.startswith('-main:') else '-out:'+str(out/'control.exe') if x.startswith('-out:') else x for x in args]
    rsp=out/'compile.rsp';rsp.write_text(''.join(json.dumps(x)+'\n' for x in args))
    csc=next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compiled=subprocess.run(['dotnet',str(csc),'@'+str(rsp)],capture_output=True,text=True,timeout=300);(out/'compile.log').write_text(compiled.stdout+compiled.stderr)
    if compiled.returncode:raise RuntimeError('S006 candidate compiler harness failed to compile')
    runtime=json.loads(BUILD.read_bytes())['runtime_selection'];env=os.environ.copy();env['MONO_PATH']=os.pathsep.join((str(BUILD.parent/'placement'),runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(out)))
    run=subprocess.run([runtime['mono'],str(out/'control.exe'),str(GRAPH),str(MATRIX),str(out/'result.json'),str(out/'layout-newtonsoft.json'),'1' if options.canonical_v2_candidate else '0'],cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(out/'run.log').write_text(run.stdout+run.stderr)
    marker='[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0'
    if run.returncode or run.stdout.splitlines().count(marker)!=1:raise RuntimeError('S006 candidate compiler harness failed')
    verify();result=json.loads((out/'result.json').read_bytes())
    if result.get('candidate_only') is not True or result.get('mission_ready') is not False or len(result['rows'])!=3:raise ValueError('Candidate scope changed')
    matrix_by_size={row['size_id']:row for row in s006['setup_rows']}
    for row in result['rows']:
        if not row.get('compiled'):continue
        setup=row.get('setup');source=matrix_by_size[row['size']]
        if not isinstance(setup,dict) or setup.get('CatalogId')!='S006':raise ValueError('Exact candidate setup identity required')
        by_name={key.lower():value for key,value in setup.items()}
        mismatches=[];checked=0
        for name,value in source.items():
            key=name.replace('_','').lower()
            if key not in by_name or not isinstance(by_name[key],int) or isinstance(by_name[key],bool) or not value.lstrip('-').isdigit():continue
            if by_name[key]!=int(value):mismatches.append({'field':name,'matrix':int(value),'compiled':by_name[key]})
            checked+=1
        row['matrix_scalar_fields_checked']=checked;row['matrix_scalar_mismatches']=mismatches
        row['actual_force_quantities_by_faction']={str(faction):sum(int(f['Quantity']) for f in setup['Forces'] if f['FactionId']==faction) for faction in (1,2)}
        row['actual_structure_rows_by_faction']={str(faction):sum(1 for f in setup['Structures'] if f['FactionId']==faction) for faction in (1,2)}
    (out/'result.json').write_text(json.dumps(result,indent=2)+'\n')
    (out/'receipt.json').write_text(json.dumps({'status':'CollectedSourceCandidate','marker':marker,'result_sha256':sha(out/'result.json'),'source_pins':pins,'mission_ready':False},indent=2)+'\n')
    print(json.dumps({'kind':result['kind'],'candidate_only':True,'mission_ready':False,'rows':[{k:v for k,v in row.items() if k not in ('setup','source_pad_resolution')} for row in result['rows']]}))


if __name__=='__main__':main()
