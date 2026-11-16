#!/usr/bin/env python3
"""Run the original S006 config-only candidate on a current compile closure.

Retained generated code is explicitly unqualified: this does not refresh the
prefab capture or admit a mission, source fit, world, or player readiness.
"""
import hashlib
import importlib.util
import json
import os
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's006-logical-binding-probe-build-20261106-T3'
OUTPUT = LOGS / 's006-current-config-only-candidate-20261106-T22'
CANONICAL = LOGS / 's002-s004-difficulty-current-build-20261106-T1'
RECIPE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/compile_s006_source_candidate.py'
GRAPH = LOGS / 's005-view-profile-source-graph-20261105-T1/source-config-graph.json'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
INVENTORY = LOGS / 'all120-source-config-inventory-20261105-T2/inventory.json'
BUILD = LOGS / 's003-current-prefab-import-build-20261104-T1/build.json'
FRAMING_ASSET = ROOT / 'Assets/Game/Configs/SkirmishExpansion/StagedS049/Logical/Binding_S049.asset'

def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()

def main():
    OUTPUT.mkdir(exist_ok=False)
    prior = json.loads((BASE / 'receipt.json').read_bytes())
    if prior.get('status') != 'PassedCompileOnly' or prior.get('changed_during_compile') != []:
        raise ValueError('Exact compile-only source closure required')
    pins = dict(prior['source_pins'])
    graph = json.loads(GRAPH.read_bytes())
    for name, digest in graph['source_pins'].items():
        if name in pins and pins[name] != digest: raise ValueError('Conflicting graph pin: ' + name)
        pins[name] = digest
    inventory = json.loads(INVENTORY.read_bytes())
    row = next(m for m in inventory['missions'] if m['catalog_id'] == 'S006')
    if inventory['input_sha256'].get(str(MATRIX)) != sha(MATRIX) or row['catalog_definition_bound'] or row['scenario_directory_present']:
        raise ValueError('Exact unbound S006 matrix inventory required')
    if (row['catalog']['MapId'], row['catalog']['ArmyProfile'], row['catalog']['StartProfile'], row['catalog']['ObjectiveId']) != ('DB', 'C', 'E', 'BA'):
        raise ValueError('S006 coded identity changed')
    for path in (Path(__file__), RECIPE, GRAPH, MATRIX, INVENTORY, BUILD, FRAMING_ASSET, BASE / 'receipt.json', BASE / 'compile.rsp'):
        pins[str(path)] = sha(path)
    workspace_files = ('SkirmishMapLayoutConfig.cs', 'SkirmishResolvedSetup.cs', 'SkirmishBindingCanonicalValues.cs', 'SkirmishLegacyLayoutValueContract.cs', 'SkirmishMeasuredLayoutBinder.cs', 'SkirmishSetupCompiler.cs')
    workspace_root = ROOT / 'Assets/Game/Scripts/Configs/Skirmish'
    for name in workspace_files: pins[str(workspace_root / name)] = sha(workspace_root / name)
    canonical_receipt = json.loads((CANONICAL / 'build.json').read_bytes())
    generated_compiler = CANONICAL / 'source/generated/045_SkirmishSetupCompiler.cs'
    canonical_pins = canonical_receipt['source_and_reference_pins']
    if canonical_receipt.get('status') != 'Passed' or canonical_pins.get(str(generated_compiler)) != sha(generated_compiler) or canonical_pins.get(str(workspace_root / 'SkirmishSetupCompiler.cs')) != sha(workspace_root / 'SkirmishSetupCompiler.cs'):
        raise ValueError('Fresh canonical generated compiler and workspace source pins required')
    pins[str(CANONICAL / 'build.json')] = sha(CANONICAL / 'build.json')
    pins[str(generated_compiler)] = sha(generated_compiler)
    partial_probe = Path(__file__).with_name('S006PartialLogicalBindingProbe.cs')
    pins[str(partial_probe)] = sha(partial_probe)
    external_loader = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceUnqualifiedLogicalBindingCandidate.cs'
    pins[str(external_loader)] = sha(external_loader)
    build = json.loads(BUILD.read_bytes())
    csc = next(Path(p) for p in build['source_and_reference_pins'] if p.endswith('/Roslyn/bincore/csc.dll'))
    pins[str(csc)] = sha(csc)
    def verify():
        for name, digest in pins.items():
            if not Path(name).is_file() or sha(name) != digest: raise ValueError('Source input changed: ' + name)
    verify()
    text = FRAMING_ASSET.read_text()
    variant = text.split('  - Size: ', 1)[1].split('  - Size: ', 1)[0]
    def scalar(name):
        values = re.findall(r'^    ' + re.escape(name) + r': ([^\r\n]+)$', variant, re.MULTILINE)
        if len(values) != 1: raise ValueError('Exact staged framing scalar required: '+name)
        return float(values[0])
    blocks = re.findall(r'^    - Role: (\d+)\n((?:^      [^\r\n]+\n)+)', variant, re.MULTILINE)
    framing_rows = []
    fields = ('Stages','ObservePlayer','ObserveEnemy','MinimumViewportHeight','MaximumViewportDimension','SafeEdgeFraction','MinimumSubjectElevation','MaximumSubjectElevation','Evidence')
    for role, body in blocks:
        values = dict(re.findall(r'^      (\w+): ([^\r\n]+)$', body, re.MULTILINE))
        if set(values) != set(fields): raise ValueError('Exact staged role framing fields required')
        row = {'Role':int(role)}
        for field in fields:
            row[field] = bool(int(values[field])) if field in ('ObservePlayer','ObserveEnemy') else int(values[field]) if field in ('Stages','Evidence') else float(values[field])
        framing_rows.append(row)
    if len(framing_rows) != 16 or len({x['Role'] for x in framing_rows}) != 16: raise ValueError('Exact staged role template required')
    template = OUTPUT/'unreviewed-framing-template.json'
    template.write_text(json.dumps({'kind':'UnreviewedS049FramingTemplate','source_asset':str(FRAMING_ASSET),'source_sha256':pins[str(FRAMING_ASSET)],'source_max_elevation':scalar('SourceMaximumElevation'),'opening_focus_smooth_seconds':scalar('OpeningFocusSmoothSeconds'),'rows':framing_rows,'scope':'Diagnostic requirements template only; not S006 visual review or physical evidence'},indent=2)+'\n')
    pins[str(template)] = sha(template)
    spec = importlib.util.spec_from_file_location('s006_candidate', RECIPE)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    source = module.SOURCE
    view_anchor = 'var source=new SourceConfigMaterializer(graph);'
    if source.count(view_anchor) != 1: raise ValueError('Exact materializer seam required')
    source = source.replace(view_anchor, view_anchor+'var existingView=source.ByPath<SkirmishMissionViewProfile>("Assets/Game/Resources/SkirmishMissionViewProfile_S005.asset");', 1)
    loop = '  foreach(var size in new[]{SkirmishSizeId.Standard,SkirmishSizeId.War,SkirmishSizeId.LargeWar}){'
    compile_call = 'SkirmishSetupCompiler.TryCompile(candidate,SkirmishDifficultyId.Regular,size,104735,manifest,rows,out var setup,out var reasons)'
    if source.count(loop) != 1 or source.count(compile_call) != 1: raise ValueError('Exact original size and difficulty loop required')
    source = source.replace(loop, '  foreach(var difficulty in new[]{SkirmishDifficultyId.Recruit,SkirmishDifficultyId.Regular,SkirmishDifficultyId.Veteran,SkirmishDifficultyId.Commander})\n'+loop, 1)
    source = source.replace(compile_call, 'SkirmishSetupCompiler.TryCompile(candidate,difficulty,size,104735,manifest,rows,out var setup,out var reasons)', 1)
    source = source.replace('["size"]=size.ToString()', '["difficulty"]=difficulty.ToString(),["size"]=size.ToString()')
    source = source.replace('sizes=3 candidateOnly=1', 'sizes=3 difficulties=4 candidateOnly=1')
    anchor = '["requires_logical_binding"]=needsBinding'
    if source.count(anchor) != 1: raise ValueError('Exact original result projection required')
    source = source.replace(anchor, '["authoring_inputs"]=setup==null?(JToken)JValue.CreateNull():JObject.FromObject(SkirmishMissionLogicalAuthoringInputs.Capture(candidate,setup)),["logical_probe"]=setup==null?(JToken)JValue.CreateNull():SourceS006LogicalBindingProbe.Probe(candidate,setup,existingView),["partial_binding_probe"]=setup==null?(JToken)JValue.CreateNull():S006PartialLogicalBindingProbe.Probe(candidate,setup,existingView,false),["candidate_view_probe"]=setup==null?(JToken)JValue.CreateNull():S006PartialLogicalBindingProbe.Probe(candidate,setup,existingView,true),["candidate_framing_probe"]=setup==null?(JToken)JValue.CreateNull():S006PartialLogicalBindingProbe.Probe(candidate,setup,existingView,true,JObject.Parse(File.ReadAllText(a[5])),a[6]),'+anchor, 1)
    control = OUTPUT / 'S006SourceCandidateControl.cs'; control.write_text(source)
    options = [json.loads(x) for x in (BASE / 'compile.rsp').read_text().splitlines() if x.strip()]
    temporary_root = '/private/tmp/warline-grid-publication-20261105-T1/Assets/Game/Scripts/Configs/Skirmish/'
    for name in workspace_files[:-1]:
        old_source = temporary_root + name
        if options.count(old_source) != 1: raise ValueError('Exact temporary source seam required: ' + name)
        options = [str(workspace_root / name) if x == old_source else x for x in options]
    derived_compiler = str(LOGS / 's006-authored-origin-hash-census-20261105-T3/MergedSkirmishSetupCompiler.cs')
    if options.count(derived_compiler) != 1: raise ValueError('Exact derived compiler seam required')
    options = [str(generated_compiler) if x == derived_compiler else x for x in options]
    options.append(str(partial_probe))
    options.append(str(external_loader))
    old = str(LOGS / 's006-logical-binding-proposal-probe-20261105-T1/S006LogicalProbeControl.cs')
    if options.count(old) != 1: raise ValueError('Exact retained entry point required')
    options = [str(control) if x == old else '-main:S006SourceCandidateControl' if x.startswith('-main:') else '-out:' + str(OUTPUT / 'control.exe') if x.startswith('-out:') else x for x in options]
    (OUTPUT / 'compile.rsp').write_text(''.join(json.dumps(x) + '\n' for x in options))
    compiled = subprocess.run(['dotnet', str(csc), '@' + str(OUTPUT / 'compile.rsp')], capture_output=True, text=True, timeout=300)
    (OUTPUT / 'compile.log').write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode: raise RuntimeError('Original compiler fixture failed; see compile.log')
    runtime = build['runtime_selection']
    pins[runtime['mono']] = sha(runtime['mono'])
    env = os.environ.copy(); env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(OUTPUT)))
    command = [runtime['mono'], str(OUTPUT / 'control.exe'), str(GRAPH), str(MATRIX), str(OUTPUT / 'result.json'), str(OUTPUT / 'layout.json'), '1', str(template), str(OUTPUT)]
    run = subprocess.run(command, cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (OUTPUT / 'run.log').write_text(run.stdout + run.stderr)
    marker = '[S006SourceCandidate] result=Collected sizes=3 difficulties=4 candidateOnly=1 readiness=0'
    if run.returncode or run.stdout.splitlines().count(marker) != 1: raise RuntimeError('Candidate execution failed; see run.log')
    verify(); result = json.loads((OUTPUT / 'result.json').read_bytes())
    if result.get('candidate_only') is not True or result.get('mission_ready') is not False or len(result.get('rows', [])) != 12:
        raise ValueError('Candidate scope changed')
    if {(r.get('difficulty'), r.get('size')) for r in result['rows']} != {(d, s) for d in ('Recruit', 'Regular', 'Veteran', 'Commander') for s in ('Standard', 'War', 'LargeWar')}:
        raise ValueError('Exact difficulty and size coverage required')
    external = []
    for row in result['rows']:
        probe = row.get('candidate_framing_probe') or {}
        path = OUTPUT / ('binding-' + row['difficulty'] + '-' + row['size'] + '.json')
        if probe.get('external_roundtrip_matches') is not True or json.loads(path.read_bytes()) != probe.get('external_candidate'):
            raise ValueError('External data differs from original compiler round trip: '+str(path))
        external.append({'difficulty':row['difficulty'],'size':row['size'],'path':str(path),'sha256':sha(path)})
        pins[str(path)] = sha(path)
    negative_files = sorted(OUTPUT.glob('negative-*.json'))
    if len(negative_files) != 84: raise ValueError('Exact external negative fixture files required')
    for path in negative_files: pins[str(path)] = sha(path)
    verify()
    receipt = {'status': 'CollectedUnqualifiedSourceCandidate', 'source_pins': pins,
               'result_sha256': sha(OUTPUT / 'result.json'), 'run_log_sha256': sha(OUTPUT / 'run.log'),
               'external_candidate_files': external,
               'prior_build_source_drift': prior['prior_build_source_drift'],
               'scope': 'Fresh canonical generated compiler and workspace layout sources with retained generated fixture; hypothetical config only; prefab capture not refreshed',
               'runtime_admission': False, 'source_fit_certified': False, 'mission_ready': False}
    (OUTPUT / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({'status': receipt['status'], 'rows': [{k: r.get(k) for k in ('difficulty', 'size', 'compiled', 'setup_hash', 'source_host_boundary')} for r in result['rows']], 'mission_ready': False}))

if __name__ == '__main__': main()
