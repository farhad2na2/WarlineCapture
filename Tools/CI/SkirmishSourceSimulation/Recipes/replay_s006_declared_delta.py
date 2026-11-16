#!/usr/bin/env python3
"""Round-trip a source-pinned disposable S006 layout delta through the original compiler."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
GRAPH = ROOT / 'T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'

METHOD = r'''static JObject TestPads(SkirmishMapLayoutConfig original,SkirmishResolvedSetup setup,OperationMapDefinition map,JObject input,SkirmishScenarioDefinitionConfig sourceDefinition,SkirmishContentManifest manifest,IReadOnlyList<SkirmishSetupMatrixRow> matrix){
 var group=((JArray)input["rows"]).OfType<JObject>().Single(r=>(string)r["size"]==setup.SizeId.ToString());
 var prior=(JObject)group["disposable_pad_projection"];var delta=(JObject)prior["candidate_additions"];
 if((string)delta["declared_size"]!=setup.SizeId.ToString()||(string)delta["declared_difficulty"]!=setup.DifficultyId.ToString()||(bool)delta["measured_deployments"]||(bool)delta["bind_all_configurations"])throw new Exception("Exact unmeasured declared tuple required");
 var layout=Activator.CreateInstance<SkirmishMapLayoutConfig>();foreach(var f in typeof(SkirmishMapLayoutConfig).GetFields(BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.DeclaredOnly))f.SetValue(layout,f.GetValue(original));
 var ids=((JArray)delta["removed_pad_ids"]).Values<string>().ToArray();if(ids.Length==0||ids.Distinct().Count()!=ids.Length||ids.Any(id=>original.Pads.Count(p=>p.PadId==id)!=1))throw new Exception("Exact unique removed pad identities required");
 var anchors=original.Anchors.Concat(delta["anchors"].ToObject<SkirmishLayoutAnchorConfig[]>()).ToArray();
 var pads=original.Pads.Where(p=>!ids.Contains(p.PadId)).Concat(delta["pads"].ToObject<SkirmishLegalPadConfig[]>()).ToArray();
 var bindings=delta["instance_bindings"].ToObject<SkirmishStructureInstancePadBinding[]>();
 var af=typeof(SkirmishMapLayoutConfig).GetField("anchors",BindingFlags.Instance|BindingFlags.NonPublic);var pf=typeof(SkirmishMapLayoutConfig).GetField("pads",BindingFlags.Instance|BindingFlags.NonPublic);var df=typeof(SkirmishMapLayoutConfig).GetField("declaredConfigurations",BindingFlags.Instance|BindingFlags.NonPublic);if(af==null||pf==null||df==null)throw new Exception("Exact layout fields required");
 af.SetValue(layout,anchors);pf.SetValue(layout,pads);layout.BindStructureInstancePads(bindings);
 var player=delta["player_deployment"];var enemy=delta["enemy_deployment"];
 df.SetValue(layout,new[]{new SkirmishDeclaredLayoutConfiguration{SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,PlayerDeployment=new Vector2((float)player[0],(float)player[1]),EnemyDeployment=new Vector2((float)enemy[0],(float)enemy[1])}});
 var frame=layout.WorldBinding;frame.HasMeasuredDeployments=false;frame.BindAllConfigurations=false;layout.ApplyWorldBinding(frame);
 var definition=Activator.CreateInstance<SkirmishScenarioDefinitionConfig>();definition.Configure("S006",sourceDefinition.ObjectiveConfig,sourceDefinition.ArmyProfileConfig,sourceDefinition.StartPackageConfig,sourceDefinition.EconomyConfig,sourceDefinition.UpgradeCatalog,sourceDefinition.IntelConfig,sourceDefinition.RoleCatalog,layout,featureIds:sourceDefinition.RequiredFeatureIds);definition.BindCampaignMap(map);
 bool accepted=SkirmishSetupCompiler.TryCompile(definition,setup.DifficultyId,setup.SizeId,setup.Seed,manifest,matrix,out var actual,out var reasons);var errors=new JArray();foreach(var reason in reasons)errors.Add(reason.ToString());
 var projection=new SkirmishResolvedSetup{SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,Structures=(SkirmishResolvedStructureEntry[])setup.Structures.Clone()};bool bound=SkirmishStructureInstanceBinding.TryApply(layout,projection,new List<SkirmishCompileReason>());
 layout.BindStructureInstancePads(bindings.Take(bindings.Length-1).ToArray());var negative=new SkirmishResolvedSetup{SizeId=setup.SizeId,DifficultyId=setup.DifficultyId,Structures=(SkirmishResolvedStructureEntry[])setup.Structures.Clone()};bool missingRejected=!SkirmishStructureInstanceBinding.TryApply(layout,negative,new List<SkirmishCompileReason>());
 return new JObject{["compiled"]=accepted,["setup_hash"]=actual==null?(JToken)JValue.CreateNull():new JValue(actual.SetupHash),["expected_setup_hash"]=prior["bound_compiler"]["setup_hash"],["binding_applied"]=bound,["missing_binding_rejected"]=missingRejected,["reasons"]=errors,["scope"]="Reconstructed serialized source-only delta; no runtime admission or readiness"};}
'''


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--delta', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    candidate = args.candidate.resolve(strict=True)
    output = args.output.resolve()
    if not candidate.is_relative_to(ROOT) or not output.is_relative_to(ROOT):
        raise ValueError('Repository candidate and output required')
    receipt = json.loads((candidate / 'receipt.json').read_bytes())
    result_path = candidate / 'result.json'
    if receipt.get('status') != 'CollectedDisposablePadCandidate' or receipt.get('result_sha256') != sha(result_path) or receipt.get('mission_ready') is not False:
        raise ValueError('Exact disposable candidate receipt required')
    pins = dict(receipt['source_pins'])
    delta_path = result_path
    if args.delta is not None:
        delta_path = args.delta.resolve(strict=True)
        if not delta_path.is_relative_to(ROOT):
            raise ValueError('External candidate delta must be repository-owned')
        delta = json.loads(delta_path.read_bytes())
        if (delta.get('kind') != 'S006ExternalConfigDeltaCandidate' or delta.get('schema_version') != 1 or
                any(delta.get(key) is not False for key in ('runtime_admission', 'source_fit_certified', 'mission_ready'))):
            raise ValueError('Exact unqualified external config delta required')
        for name in ('candidate_receipt', 'candidate_result', 'fixed_data_audit', 'exporter'):
            value = delta.get(name)
            if not isinstance(value, dict) or set(value) != {'path', 'sha256'}:
                raise ValueError('Exact external delta provenance descriptor required')
            path = Path(value['path']).resolve(strict=True)
            if not path.is_relative_to(ROOT) or sha(path) != value['sha256']:
                raise ValueError('External delta provenance changed: ' + name)
            pins[str(path)] = value['sha256']
        if delta['candidate_receipt']['path'] != str(candidate / 'receipt.json') or delta['candidate_result']['path'] != str(result_path):
            raise ValueError('External delta belongs to another candidate')
        baseline = json.loads(result_path.read_bytes())
        expected_rows = [{'size': row['size'], 'disposable_pad_projection': {
            'candidate_additions': row['disposable_pad_projection']['candidate_additions'],
            'bound_compiler': {'setup_hash': row['disposable_pad_projection']['bound_compiler']['setup_hash']}}}
            for row in baseline['rows']]
        if delta.get('rows') != expected_rows:
            raise ValueError('External delta does not equal the original candidate values')
        pins[str(delta_path)] = sha(delta_path)
    for path in (candidate / 'receipt.json', result_path, candidate / 'compile.rsp', candidate / 'S006TypedPadCandidateControl.cs', BUILD, GRAPH, MATRIX, Path(__file__).resolve()):
        pins[str(path)] = sha(path)
    def verify():
        for name, expected in pins.items():
            if not Path(name).is_file() or sha(Path(name)) != expected:
                raise ValueError('Pinned source changed: ' + name)
    verify()
    output.mkdir(parents=True, exist_ok=False)
    original = (candidate / 'S006TypedPadCandidateControl.cs').read_text()
    start, end = original.find('static JObject TestPads('), original.find(' static void Main(string[] a)')
    if start < 0 or end <= start or original.count('static JObject TestPads(') != 1:
        raise ValueError('Exact source candidate method required')
    source = output / 'S006DeclaredDeltaReplay.cs'
    source.write_text(original[:start] + METHOD + original[end:])
    old_source = str(candidate / 'S006TypedPadCandidateControl.cs')
    compiler_args = [json.loads(line) for line in (candidate / 'compile.rsp').read_text().splitlines() if line.strip()]
    if compiler_args.count(old_source) != 1:
        raise ValueError('Exact original compile response required')
    compiler_args = [str(source) if value == old_source else '-out:' + str(output / 'control.exe') if value.startswith('-out:') else value for value in compiler_args]
    response = output / 'compile.rsp'
    response.write_text(''.join(json.dumps(value) + '\n' for value in compiler_args))
    csc = next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compiled = subprocess.run(['dotnet', str(csc), '@' + str(response)], capture_output=True, text=True, timeout=300)
    (output / 'compile.log').write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode:
        raise RuntimeError('Delta replay compilation failed')
    runtime = json.loads(BUILD.read_bytes())['runtime_selection']
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(output)))
    run = subprocess.run([runtime['mono'], str(output / 'control.exe'), str(GRAPH), str(MATRIX), str(output / 'result.json'), str(output / 'layout.json'), '1', str(delta_path)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (output / 'run.log').write_text(run.stdout + run.stderr)
    marker = '[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0'
    if run.returncode or run.stdout.splitlines().count(marker) != 1:
        raise RuntimeError('Delta replay execution failed')
    verify()
    result = json.loads((output / 'result.json').read_bytes())
    summary = []
    for row in result['rows']:
        replay = row.get('disposable_pad_projection') or {}
        summary.append({'size': row['size'], 'compiled': replay.get('compiled'), 'setup_hash': replay.get('setup_hash'),
                        'expected_setup_hash': replay.get('expected_setup_hash'), 'binding_applied': replay.get('binding_applied'),
                        'missing_binding_rejected': replay.get('missing_binding_rejected'), 'reasons': replay.get('reasons')})
    report = {'status': 'CollectedS006DeclaredDeltaReplay', 'source_pins': pins, 'sizes': summary,
              'result_sha256': sha(output / 'result.json'), 'scope': 'Serialized source-only candidate; no runtime admission or source-fit claim', 'mission_ready': False}
    (output / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'status': report['status'], 'sizes': summary, 'mission_ready': False}))
    if any(row['compiled'] is not True or row['setup_hash'] != row['expected_setup_hash'] or
           row['binding_applied'] is not True or row['missing_binding_rejected'] is not True for row in summary):
        raise RuntimeError('Serialized delta failed an original compiler or binding control')


if __name__ == '__main__':
    main()
