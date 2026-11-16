#!/usr/bin/env python3
"""Compile and run the experimental source-only mission binding author behind exact pins."""
from __future__ import annotations
import argparse, hashlib, json, math, os, re, shutil, subprocess, sys, time
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

def owned_cli():
    import types
    path=HERE/'skirmish_owned_source.py'; raw=path.read_bytes()
    if len(raw)>2*1024*1024 or 'skirmish_owned_source' in sys.modules:
        raise RuntimeError('Exact-byte owned loader bootstrap unavailable')
    module=types.ModuleType('skirmish_owned_source'); module.__file__=str(path)
    module.__executed_source_pin__={'path':str(path),'sha256':hashlib.sha256(raw).hexdigest()}
    sys.modules[module.__name__]=module
    exec(compile(raw,str(path),'exec'),module.__dict__)
    if path.read_bytes()!=raw: raise RuntimeError('Owned loader changed during bootstrap')
    module.load_cli(HERE)
    return module

owned=owned_cli()
build=owned.require('skirmish_source_build')
authoring=owned.require('skirmish_source_authoring')

AUTHOR = (HERE / 'SkirmishMissionLogicalBindingAuthor.cs').resolve(strict=True)
REQUIRED = {'schema_version','kind','catalog_id','seed','size','difficulty','graph','profiles','campaign_source_path','layout','setup_matrix'}
OPTIONAL_DESCRIPTORS = {'binding','native_binding','external_pad_candidate'}
PROFILE_KEYS = {'objective','army','start','economy','upgrades','intel','roles'}
S026_ALIAS = 'Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset'
S026_SOURCE = 'Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset'
SOURCE_PARSER = 'Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb'


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def read_json(path: Path):
    value, pin = authoring.read_author_json(path)
    return value, pin


def verify_descriptor(row, key, repo, data_repo):
    build.need(type(row) is dict and set(row) == {'path','sha256'}, f'Exact pinned {key} descriptor required')
    path = Path(row['path']).resolve(strict=True)
    root = repo if path.suffix == '.cs' else data_repo
    build.need(path.is_relative_to(root), f'{key} outside explicit repository roots')
    actual = build.pin(path)
    build.need(actual['sha256'].lower() == row['sha256'].lower(), f'{key} hash mismatch')
    return path, actual


def validate_explicit_s026_alias(draft, layout, repo, alias_path, alias_sha256):
    """Check one explicit logical alias against its actual pinned physical source."""
    root=Path(repo).resolve(strict=True); alias=Path(alias_path).resolve(strict=True)
    build.need(draft.get('catalog_id')=='S026' and draft.get('campaign_source_path')==S026_SOURCE and
               alias==(root/S026_ALIAS).resolve(strict=True) and
               layout.get('operationMapId')=='opmap.skirmish.city_crossroads',
               'Explicit S026 City Crossroads alias and DenseCity source required')
    build.need(isinstance(alias_sha256,str) and re.fullmatch('[0-9a-f]{64}',alias_sha256) is not None,
               'Exact lowercase alias SHA256 required')
    source=(root/S026_SOURCE).resolve(strict=True); parser=(root/SOURCE_PARSER).resolve(strict=True)
    alias_meta=Path(str(alias)+'.meta').resolve(strict=True); source_meta=Path(str(source)+'.meta').resolve(strict=True)
    script_meta=(root/'Assets/Game/Scripts/Configs/OperationMapDefinition.cs.meta').resolve(strict=True)
    build.need(not Path(alias_path).is_symlink() and all(p.is_file() and p.is_relative_to(root) for p in (alias,source,parser,alias_meta,source_meta,script_meta)),
               'Regular repository alias, source, parser and metadata required')
    pins={str(p):sha(p) for p in (alias,source,parser,alias_meta,source_meta,script_meta)}
    build.need(pins[str(alias)]==alias_sha256,'Alias asset hash mismatch')
    parsed=subprocess.run(['ruby',str(parser),str(alias),str(source)],capture_output=True,text=True,timeout=30,check=True)
    def unique(items):
        result={}
        for key,value in items:
            build.need(key not in result,'Duplicate parsed alias key: '+key);result[key]=value
        return result
    data=json.loads(parsed.stdout,object_pairs_hook=unique)
    a=data[str(alias)]['MonoBehaviour']; s=data[str(source)]['MonoBehaviour']; binding=a.get('sourceBinding')
    guids=re.findall(r'^guid:\s*([0-9a-f]{32})\s*$',script_meta.read_text(),re.MULTILINE)
    build.need(a.get('m_EditorClassIdentifier')=='Game.Configs::Game.Configs.OperationMapDefinition' and
               s.get('m_EditorClassIdentifier')=='Game.Configs::Game.Configs.OperationMapDefinition' and
               len(guids)==1 and a.get('m_Script')==s.get('m_Script')=={'fileID':11500000,'guid':guids[0],'type':3} and
               a.get('operationMapId')=='opmap.skirmish.city_crossroads' and
               s.get('operationMapId')=='opmap.skirmish.desert_base_01' and isinstance(binding,dict) and
               binding.get('sourceOperationMapId')==s.get('operationMapId') and
               binding.get('sourceIdentityHash')==s.get('sourceIdentityHash')==a.get('sourceIdentityHash') and
               binding.get('sourceContentHash')==s.get('contentHash')==layout.get('worldBinding',{}).get('SourceHash') and
               all(isinstance(s.get(k),str) and re.fullmatch('[0-9a-f]{64}',s[k]) for k in ('sourceIdentityHash','contentHash')),
               'Alias binding does not match the pinned physical source and layout')
    for path,expected in pins.items():
        build.need(sha(Path(path))==expected,'Alias/source/parser changed during validation: '+path)
    return pins


def validate_draft(draft, repo, data_repo, pins, alias=None):
    build.need(type(draft) is dict and set(draft) in (REQUIRED, *(REQUIRED | {key} for key in OPTIONAL_DESCRIPTORS), REQUIRED | {'native_binding','external_pad_candidate'}), 'Exact canonical authoring draft fields required')
    build.need(draft.get('schema_version') == 1 and draft.get('kind') == 'SkirmishMissionAuthoringDraft', 'Canonical mission authoring draft required')
    mission, seed = draft.get('catalog_id'), draft.get('seed')
    build.need(isinstance(mission, str) and re.fullmatch(r'S(?:00[1-9]|0[1-9][0-9]|1[01][0-9]|120)', mission) is not None, 'Canonical Skirmish ID required')
    build.need(type(seed) is int and 1 <= seed <= 2147483647, 'Positive bounded seed required')
    build.need(draft.get('size') in {'Standard','War','LargeWar'} and draft.get('difficulty') in {'Recruit','Regular','Veteran','Commander'}, 'Native size/difficulty required')
    profiles = draft.get('profiles')
    build.need(type(profiles) is dict and set(profiles) == PROFILE_KEYS and all(type(v) is str and v for v in profiles.values()), 'Exact profile path set required')
    graph_path, graph_pin = verify_descriptor(draft['graph'], 'graph', repo, data_repo)
    layout_path, layout_pin = verify_descriptor(draft['layout'], 'layout', repo, data_repo)
    matrix_path, matrix_pin = verify_descriptor(draft['setup_matrix'], 'setup_matrix', repo, data_repo)
    graph, _ = read_json(graph_path)
    build.need(type(graph.get('source_pins')) is dict and graph['source_pins'], 'Complete source graph pins required')
    authoring.merge_authoring_graph_pins(pins, graph['source_pins'], repo, data_repo)
    source_asset=(repo/draft['campaign_source_path']).resolve(strict=True)
    source_pin=build.pin(source_asset)
    build.need(graph['source_pins'].get(str(source_asset)) == source_pin['sha256'], 'Requested Campaign source is not pinned by the source graph')
    source_text=source_asset.read_text(errors='replace')
    match=re.search(r'^\s*operationMapId:\s*([^\s]+)',source_text,re.MULTILINE)
    build.need(match is not None, 'Campaign source lacks a typed operationMapId')
    layout=json.loads(layout_path.read_text())
    if layout.get('operationMapId') != match.group(1):
        build.need(alias is not None, 'Logical layout source identity differs from Campaign source')
        alias_pins=validate_explicit_s026_alias(draft,layout,repo,alias['path'],alias['sha256'])
        build.merge_pins(pins,alias_pins)
    else:
        build.need(alias is None, 'Explicit alias is only valid for a distinct logical source')
    build.merge_pins(pins, {str(graph_path):graph_pin['sha256'],str(layout_path):layout_pin['sha256'],str(matrix_path):matrix_pin['sha256'],str(source_asset):source_pin['sha256']})
    for key in OPTIONAL_DESCRIPTORS.intersection(draft):
        descriptor_path, descriptor_pin = verify_descriptor(draft[key], key, repo, data_repo)
        build.merge_pins(pins,{str(descriptor_path):descriptor_pin['sha256']})
    build.need(isinstance(draft.get('campaign_source_path'), str) and draft['campaign_source_path'].startswith('Assets/'), 'Campaign source asset path required')
    return mission, seed, graph_path, layout_path, matrix_path


def compiler_command(receipt, output, repo):
    rsp_pin = receipt.get('compiler_response')
    build.need(type(rsp_pin) is dict and set(rsp_pin) == {'path','sha256','bytes'}, 'Pinned compiler response required')
    rsp = Path(rsp_pin['path']).resolve(strict=True)
    build.need(build.pin(rsp)['sha256'] == rsp_pin['sha256'] and rsp.stat().st_size == rsp_pin['bytes'], 'Compiler response pin mismatch')
    raw = rsp.read_text()
    build.need('"-main:SourceSimulationProgram"' in raw and '-out:' in raw, 'Expected pinned source closure response format')
    response = raw.replace('"-main:SourceSimulationProgram"','"-main:SkirmishMissionLogicalBindingAuthor"',1)
    response, count = re.subn(r'"-out:[^"\r\n]+"', '"-out:' + str((output/'author.exe').resolve()) + '"', response, count=1)
    build.need(count == 1, 'Could not redirect compiler output safely')
    response += '\n"' + str(AUTHOR) + '"\n'
    response_path = output / 'author.rsp'
    response_path.write_text(response)
    dotnet = Path(shutil.which('dotnet') or '').resolve(strict=True)
    sdk_roots = list((dotnet.parent/'sdk').glob('*/Roslyn/bincore/csc.dll'))
    build.need(sdk_roots, 'Installed Roslyn compiler required')
    csc = sorted(sdk_roots, key=lambda p: tuple(int(x) for x in re.search(r'/sdk/([0-9.]+)/', str(p)).group(1).split('.')))[-1].resolve(strict=True)
    return [str(dotnet),str(csc),'@'+str(response_path)], {'dotnet':str(dotnet),'dotnet_sha256':sha(dotnet),'csc':str(csc),'csc_sha256':sha(csc),'response':str(response_path),'response_sha256':sha(response_path)}


def request_from_draft(draft, repo, data_repo, output, config):
    request=dict(draft)
    request['kind']='SkirmishMissionAuthoringPreflight'
    request['source_repository_root']=str(repo)
    request['data_repository_root']=str(data_repo)
    request['output_directory']=str(output)
    request['runtime_config']={'path':str(config),'sha256':sha(config)}
    return request


def validate_identity_joins(proof, binding, draft, layout, source_map_id):
    mission=draft['catalog_id']; seed=draft['seed']
    expected_definition='skirmish.'+mission.lower()
    build.need(proof.get('catalog_id')==mission and proof.get('request_catalog_id')==mission and proof.get('seed')==seed and proof.get('request_seed')==seed and proof.get('size')==draft['size'] and proof.get('difficulty')==draft['difficulty'], 'Proof identity differs from canonical authoring draft')
    build.need(binding.get('CatalogId')==mission and binding.get('DefinitionId')==expected_definition and binding.get('LayoutId')==layout.get('layoutId') and binding.get('SourceOperationMapId')==source_map_id, 'Logical binding identity differs from canonical compiler context')
    return True

def run(repo, data_repo, draft_path, receipt_path, output, timeout=120, alias=None):
    repo=Path(repo).resolve(strict=True); data_repo=Path(data_repo).resolve(strict=True)
    draft_path=Path(draft_path).resolve(strict=True); receipt_path=Path(receipt_path).resolve(strict=True); output=Path(output).absolute()
    build.need(math.isfinite(timeout) and timeout > 0 and timeout <= 600, 'Bounded positive timeout required')
    build.need(not output.exists(), 'Fresh isolated output directory required')
    draft, draft_pin=read_json(draft_path); receipt, receipt_pin=build.read_json_pinned(receipt_path)
    build.need(receipt.get('status')=='Passed' and receipt.get('repository_root')==str(repo) and isinstance(receipt.get('source_and_reference_pins'),dict), 'Passed pinned compiler build receipt required')
    build.need(receipt.get('runtime_selection') and receipt.get('compiler_response'), 'Runtime and compiler pins required')
    pins=dict(receipt['source_and_reference_pins'])
    build.merge_pins(pins,{draft_pin['path']:draft_pin['sha256'],receipt_pin['path']:receipt_pin['sha256'],str(AUTHOR):sha(AUTHOR),str(Path(__file__).resolve()):sha(Path(__file__).resolve())})
    mission,seed,graph_path,layout_path,matrix_path=validate_draft(draft,repo,data_repo,pins,alias)
    layout_context=json.loads(layout_path.read_text())
    source_asset=(repo/draft['campaign_source_path']).resolve(strict=True)
    source_text=source_asset.read_text(errors='replace')
    source_map_id=re.search(r'^\s*operationMapId:\s*([^\s]+)',source_text,re.MULTILINE).group(1)
    build.verify(pins)
    output.mkdir(parents=True)
    controls=output/'negative-controls'; controls.mkdir()
    # Stale graph/layout controls exercise the same strict preflight before candidate output exists.
    control_rows=[]
    for key in ('graph','layout'):
        bad=json.loads(json.dumps(draft)); bad[key]['sha256']='0'*64
        try:
            validate_draft(bad,repo,data_repo,dict(pins),alias)
            raise RuntimeError('stale '+key+' unexpectedly passed')
        except (ValueError, RuntimeError) as err:
            if str(err)=='stale '+key+' unexpectedly passed': raise
            control_rows.append({'control':'stale_'+key+'_hash','rejected':True,'candidate_output_exists':(output/'candidate').exists(),'reason':str(err)})
    # Prepare a pinned mono config via the same project-owned setup used by authoring runs.
    tools={k:Path(v).resolve(strict=True) for k,v in receipt['runtime_selection'].items()}
    build.need(set(tools)=={'contents','script','engine','mono','newtonsoft'}, 'Exact Unity Mono runtime selection required')
    config,extra=build.prepare_mono_config(tools,output)
    build.merge_pins(pins,extra)
    build.merge_pins(pins,{str(config.resolve()):sha(config)})
    request=request_from_draft(draft,repo,data_repo,output/'candidate',config)
    reqpath=output/'authoring-request.json'; reqpath.write_text(json.dumps(request,indent=2)+'\n')
    reqpin=build.pin(reqpath); build.merge_pins(pins,{reqpin['path']:reqpin['sha256']})
    command,compiler=compiler_command(receipt,output,repo)
    build.merge_pins(pins,{compiler['dotnet']:compiler['dotnet_sha256'],compiler['csc']:compiler['csc_sha256'],compiler['response']:compiler['response_sha256']})
    build.verify(pins)
    with (output/'compile.log').open('wb') as log:
        cp=subprocess.run(command,cwd=repo,stdout=log,stderr=subprocess.STDOUT,timeout=timeout)
    build.need(cp.returncode==0 and (output/'author.exe').is_file(), 'Generic author Roslyn compile failed; inspect preserved compile.log')
    author_exe=output/'author.exe'; build.merge_pins(pins,{str(author_exe):sha(author_exe)})
    env=os.environ.copy(); env['MONO_PATH']=os.pathsep.join([str(repo/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['executable']['path']).parent)])
    mono=str(tools['mono']); build.need(mono in pins,'Pinned Unity Mono executable required')
    # Dynamic invalid actor/role pair must fail before creating its designated output directory.
    probe=json.loads(reqpath.read_text()); probe['focus_pair_probe']={'role':'Rifle','actor_id':'role.gunner.1.negative-control'}
    probe_out=output/'invalid-focus-output'; probe['output_directory']=str(probe_out)
    probe_path=controls/'invalid-focus-request.json'; probe_path.write_text(json.dumps(probe,indent=2)+'\n')
    build.merge_pins(pins,{str(probe_path.resolve()):sha(probe_path)})
    probe_cmd=[mono,'--config',str(config),'--optimize=-float32',str(author_exe),str(probe_path)]
    with (controls/'invalid-focus.stdout.log').open('wb') as stdout, (controls/'invalid-focus.stderr.log').open('wb') as stderr:
        probe_result=subprocess.run(probe_cmd,cwd=repo,env=env,stdout=stdout,stderr=stderr,timeout=timeout)
    probe_text=(controls/'invalid-focus.stderr.log').read_text(errors='replace')+(controls/'invalid-focus.stdout.log').read_text(errors='replace')
    build.need(probe_result.returncode != 0 and 'Opening focus actor does not match its role.' in probe_text and not probe_out.exists(), 'Invalid actor/role negative control did not fail before output')
    control_rows.append({'control':'invalid_actor_role_pair','rejected':True,'process_exit':probe_result.returncode,'candidate_output_exists':probe_out.exists()})
    # Execute the compiled selector against synthetic camera records, without loading or
    # allocating a map/world. The selected values prove battle-camera choice when planning
    # and battle records differ; the shared case protects legacy sources.
    camera_cases = [
        ('distinct_planning_and_battle', 'battle', [
            {'id':'planning','orthographic':False,'euler':[20,10,0],'field_of_view':40},
            {'id':'battle','orthographic':False,'euler':[55,125,0],'field_of_view':63}], True),
        ('shared_planning_and_battle', 'shared', [
            {'id':'shared','orthographic':False,'euler':[45,15,0],'field_of_view':50}], True),
        ('missing_battle_camera', 'missing', [
            {'id':'planning','orthographic':False,'euler':[20,10,0],'field_of_view':40}], False),
        ('duplicate_battle_camera', 'battle', [
            {'id':'battle','orthographic':False,'euler':[55,125,0],'field_of_view':63},
            {'id':'battle','orthographic':False,'euler':[56,126,0],'field_of_view':64}], False),
        ('orthographic_battle_camera', 'battle', [
            {'id':'battle','orthographic':True,'euler':[55,125,0],'field_of_view':63}], False),
    ]
    camera_controls = controls/'camera-selection'; camera_controls.mkdir()
    for case_name, battle_id, rows, accepted in camera_cases:
        camera_out = camera_controls/(case_name+'.json')
        camera_request = {'camera_selection_probe':True,'battle_camera_id':battle_id,'cameras':rows}
        camera_out.write_text(json.dumps(camera_request,indent=2)+'\n')
        build.merge_pins(pins,{str(camera_out.resolve()):sha(camera_out)})
        camera_log = camera_controls/(case_name+'.log')
        with camera_log.open('wb') as log:
            camera_result = subprocess.run([mono,'--config',str(config),'--optimize=-float32',str(author_exe),str(camera_out)],cwd=repo,env=env,stdout=log,stderr=subprocess.STDOUT,timeout=timeout)
        camera_text = camera_log.read_text(errors='replace')
        if accepted:
            parsed = json.loads(camera_text)
            expected = next(row for row in rows if row['id']==battle_id)
            build.need(camera_result.returncode==0 and parsed.get('result')=='Passed' and parsed.get('selected_camera_id')==battle_id and parsed.get('euler')==expected['euler'] and parsed.get('field_of_view')==expected['field_of_view'], case_name+' selected the wrong source camera')
        else:
            build.need(camera_result.returncode!=0 and 'does not resolve to exactly one' in camera_text if case_name=='missing_battle_camera' else camera_result.returncode!=0 and ('duplicate camera records' in camera_text if case_name=='duplicate_battle_camera' else 'perspective battle camera' in camera_text), case_name+' unexpectedly passed')
        control_rows.append({'control':case_name,'accepted':accepted,'rejected':not accepted,'process_exit':camera_result.returncode,'selected_battle_values_verified':accepted})
    build.verify(pins)
    start=time.monotonic(); timed_out=False
    cmd=[mono,'--config',str(config),'--optimize=-float32',str(author_exe),str(reqpath)]
    with (output/'run.log').open('wb') as log:
        proc=subprocess.Popen(cmd,cwd=repo,env=env,stdout=log,stderr=subprocess.STDOUT)
        try: proc.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            timed_out=True; proc.terminate()
            try: proc.wait(timeout=5)
            except subprocess.TimeoutExpired: proc.kill(); proc.wait(timeout=5)
    build.verify(pins)
    result_path=output/'candidate'/'candidate-proof.json'
    binding_path=output/'candidate'/'mission-logical-binding.json'
    build.need(not timed_out and proc.returncode==0 and result_path.is_file() and binding_path.is_file(), 'Generic source-only author run failed; inspect preserved run.log')
    proof=json.loads(result_path.read_text())
    binding=json.loads(binding_path.read_text())
    validate_identity_joins(proof,binding,draft,layout_context,source_map_id)
    wrong_identity=json.loads(json.dumps(proof)); wrong_identity['catalog_id']=('S001' if mission!='S001' else 'S002')
    try:
        validate_identity_joins(wrong_identity,binding,draft,layout_context,source_map_id)
        raise RuntimeError('wrong_identity unexpectedly passed')
    except ValueError as err:
        if str(err)=='wrong_identity unexpectedly passed': raise
        control_rows.append({'control':'wrong_proof_identity','rejected':True,'candidate_output_exists':True,'reason':str(err)})
    build.need(proof.get('status')=='ExperimentalCompiledSourceOnlyCandidate' and proof.get('production_ready') is False and proof.get('world_allocated') is False and proof.get('placement_performed') is False and proof.get('source_fit_verified') is False and proof.get('runtime_admission') is False and proof.get('mission_ready') is False and proof.get('native_pngs_observed') is False and proof.get('mesh_bounds_observed') is False and proof.get('native_camera_observed') is False and proof.get('camera_observation_claim') is False and proof.get('screen_fit_verified') is False and proof.get('validation_passed') is False and proof.get('native_acceptance') is False and proof.get('source_fit_certified') is False and proof.get('observation_claim') is False and proof.get('source_fit_claim') is False and proof.get('runtime_claim') is False and proof.get('readiness_claim') is False, 'Output crossed a prohibited readiness or observation boundary')
    build.merge_pins(pins,{str(result_path.resolve()):sha(result_path),str(binding_path.resolve()):sha(binding_path)})
    build.verify(pins)
    (output/'negative-controls.json').write_text(json.dumps(control_rows,indent=2)+'\n')
    verified_pin_manifest=output/'verified-input-pins.json'
    verified_pin_manifest.write_text(json.dumps(dict(sorted(pins.items())),indent=2)+'\n')
    manifest={'kind':'ExperimentalSkirmishLogicalBindingAuthorRunner','status':'PassedExperimentalSourceOnly','experimental':True,'production_ready':False,'catalog_id':mission,'seed':seed,'compiler_build_receipt':receipt_pin,'canonical_draft':draft_pin,'verified_pin_count':len(pins),'verified_pin_manifest':{'path':str(verified_pin_manifest),'sha256':sha(verified_pin_manifest)},'runtime':{'mono':mono,'mono_sha256':pins[mono],'config':str(config),'config_sha256':pins[str(config)]},'compiler':compiler,'process_exit':proc.returncode,'timeout_seconds':timeout,'wall_seconds':time.monotonic()-start,'negative_controls':control_rows,'outputs':{'binding_sha256':sha(binding_path),'candidate_proof_sha256':sha(result_path)},'world_allocated':False,'placement_performed':False,'native_observation':False,'source_fit_verified':False,'runtime_admission':False,'mission_ready':False,'native_acceptance':False}
    if alias is not None:manifest['logical_alias']={'path':str(Path(alias['path']).resolve(strict=True)),'sha256':alias['sha256'],'scope':'S026 explicit source-binding check; physical source identity retained'}
    (output/'runner-receipt.json').write_text(json.dumps(manifest,indent=2)+'\n')
    return manifest


def main():
    p=argparse.ArgumentParser()
    p.add_argument('--repo',default=str(HERE.parent.parent))
    p.add_argument('--data-repo')
    p.add_argument('--draft',required=True)
    p.add_argument('--build-receipt',required=True)
    p.add_argument('--output',required=True)
    p.add_argument('--timeout',type=float,default=120)
    p.add_argument('--logical-alias')
    p.add_argument('--logical-alias-sha256')
    a=p.parse_args()
    try:
        if bool(a.logical_alias)!=bool(a.logical_alias_sha256):raise ValueError('Alias path and SHA256 must be supplied together')
        alias={'path':a.logical_alias,'sha256':a.logical_alias_sha256} if a.logical_alias else None
        result=run(a.repo,a.data_repo or a.repo,a.draft,a.build_receipt,a.output,a.timeout,alias)
        print(f"[run-skirmish-logical-binding-author] result={result['status']} mission={result['catalog_id']} seed={result['seed']} pins={result['verified_pin_count']} world=0 readiness=0")
        return 0
    except Exception as exc:
        print(f'[run-skirmish-logical-binding-author] result=Failed error={type(exc).__name__}:{exc}',file=sys.stderr)
        return 1

if __name__=='__main__': raise SystemExit(main())
