"""Sequential normal-CLI jobs; no simulation, parallelism, caching or resume."""
import argparse, hashlib, json, math, os, re, signal, subprocess, sys, time
from pathlib import Path

S049_PROJECTION17_BUNDLE_PATH='/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/data-bundle.json'
S049_PROJECTION17_BUNDLE_SHA='098132b31b89f053baa97b900b92bb32dc6050e934adad11fa9ae6dbaece171e'
S049_DIAGNOSTIC_CONTEXT_KIND='S049Projection17NativeDiagnosticV1'
S049_DIAGNOSTIC_PREFLIGHT_KIND='S049NativeDiagnosticPreflightEvidenceV1'
S049_PHYSICAL_WATER_MARKER='[S049PhysicalWaterDiagnostic] active_session_equivalence=0 native_water_qualified=0 source_fit_certified=0'
S049_CAMERA_MARKER='[S049CameraDiagnostic] native_camera_qualified=0 motion_reference_passed=0 exact_mismatch=Consumed camera state=124 pose=0 phase=124 detail=Native value reference vector mismatch actual=float3(1644.26f, 14f, 670.651f) expected=[1644.1658935546875,14.0,670.61224365234375] tolerance=0.025'
S049_IDENTITY_EXPECTED={
 'kind':S049_DIAGNOSTIC_CONTEXT_KIND,'mission':'S049','seed':104734,
 'data_bundle':{'path':S049_PROJECTION17_BUNDLE_PATH,'sha256':S049_PROJECTION17_BUNDLE_SHA},
 'initialization':{'path':'/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/initialization.json','sha256':'562f98cfd275da945c32cedfb66f6f69a27819721d6f721c46a1422651b4b965'},
 'metadata_epoch':{'path':'/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/metadata-epoch-context.json','sha256':'f50e16deb9a6e780f3fbd9c3b1b6ad592fa21ffd18daecf1eeddf9fce756669f'},
 'configuration_qualification':{'path':'/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/configuration-qualification.json','sha256':'e0504d33bd5c553049ad67e8ef847be467622afc919b6b17e67961296a697c13'},
 'fixed_map':{'path':'/Volumes/T7/Projects/WarlineCapture/Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17/fixed-map-context.json','sha256':'6c7f61b3231a1d23ac1262d0c0ce0dff00eac43485984e4f8224843a8da22f38'},
 'native_readback_sha256':'6d90b4576abd359dc09eda7cd05108be5212bcf286283e7cec867a56f85ddae3',
 'source_definition_sha256':'181418e5b316d8a5625913d0251e5f5a9a83ab81f56baec49e4adcb2bb89e3b4',
 'camera_profile_sha256':'0357aacf68cd3a96a8d715775dd838b4d4b32d60cf320caba7810fe0cb12299e',
 'camera_values_sha256':'cb5a40d3aa7b5b125eb509257045e4447baae969f5b325d6b7429449eab61e79',
 'camera_motion_sha256':'18a63db2b9a6d7c39e9898efe5825295143c4b6de4ce714243e51886f12e4765',
 'source_reference_sha256':'83aec3d79c2ac069527c438be1056df9639e40f325b4a3a3c479e090996dd70f',
 'template_slice_sha256':'4f548f40239059a37cfefcab777413cb49dfbbfce7c04757016574382aacb0e2'}

def require(ok, message):
    if not ok: raise ValueError(message)
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def batch_classifier_pin():
    path=Path(__file__).resolve(strict=True)
    return {'path':str(path),'sha256':sha(path)}
def require_batch_classifier_pin(pin):
    require(type(pin)is dict and set(pin)=={'path','sha256'},'Exact batch classifier self-pin required')
    path=Path(__file__).resolve(strict=True)
    require(pin['path']==str(path) and pin['sha256']==sha(path),'Batch classifier changed during execution')
def write(path, value): Path(path).write_text(json.dumps(value, indent=2, sort_keys=True)+"\n")
def object_keys(value, required, optional=()):
    require(type(value) is dict and set(required)<=set(value)<=set(required)|set(optional), "Unexpected or missing fields")
def read_json(path):
    def pairs(rows):
        result={}
        for key,value in rows:
            require(key not in result,'Duplicate JSON key: '+key)
            result[key]=value
        return result
    def constant(value): raise ValueError('Nonfinite JSON constant: '+value)
    def number(value):
        parsed=float(value);require(math.isfinite(parsed),'Nonfinite JSON number: '+value);return parsed
    return json.loads(Path(path).read_text(encoding='utf-8'),object_pairs_hook=pairs,parse_constant=constant,parse_float=number)
def descriptor(value, base):
    object_keys(value, {'path','sha256'})
    require(type(value['path']) is str and type(value['sha256']) is str and re.fullmatch('[0-9a-f]{64}',value['sha256']), 'Exact path/SHA256 descriptor required')
    path=Path(value['path']);path=(base/path).resolve(strict=True) if not path.is_absolute() else path.resolve(strict=True)
    require(path.is_file() and sha(path)==value['sha256'], 'Input digest mismatch: '+str(path))
    return {'path':str(path),'sha256':value['sha256']}
def same_descriptor(actual, expected):
    return type(actual)is dict and actual.get('path')==expected['path'] and actual.get('sha256')==expected['sha256'] and ('bytes'not in actual or (type(actual['bytes'])is int and actual['bytes']==Path(expected['path']).stat().st_size))
def valid_current_s002_package_binding(descriptor_value, receipt_pins):
    try:
        if type(descriptor_value) is not dict or set(descriptor_value) != {'path','sha256'} or type(receipt_pins) is not dict:
            return False
        repo=Path(__file__).resolve(strict=True).parents[2]
        package_reader_path=repo/'Tools/CI/s002_current_source_diagnostic_package.py'
        package_reader_sha=sha(package_reader_path)
        sys.path.insert(0,str(package_reader_path.parent))
        import s002_current_source_diagnostic_package as package_reader
        loaded=package_reader.load_current_s002_package(descriptor_value['path'],descriptor_value['sha256'],repo)
        if sha(package_reader_path)!=package_reader_sha:return False
        return all(receipt_pins.get(path)==digest for path,digest in loaded['source_pins'].items())
    except (OSError,ValueError,KeyError,ImportError):
        return False
def valid_s002_historical_classification(job, raw):
    historical=raw.get('historical_input_classification') if type(raw)is dict else None
    if type(historical)is not dict:return False
    false_fields=('current_source_equivalence','current_blocker_equivalence','current_surface_equivalence','physical_source_fit','water_equivalence','scene_fit_certified','mission_admitted','mission_ready','native_world_created','native_scene_loaded','native_input_observed','player_order_observed','validated_victory')
    hashes={'physical_sha256':'8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8','water_sha256':'acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5','startup_sha256':'ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03','preservation_sha256':'68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297'}
    return (job.get('mission')=='S002' and historical.get('kind')=='HistoricalInputOnlyDiagnostic_Unqualified' and historical.get('historical_input_epoch')=='2026-11-01' and historical.get('historical_producer_digest_bytes_verified') is False and historical.get('historical_producer_digest_only_count')==7 and historical.get('preserved_source_pin_count')==6526 and historical.get('preservation_project_file_count')==6522 and historical.get('preservation_external_pin_count')==4 and historical.get('historical_navigation_row_count')==4954 and all(historical.get(k)is False for k in false_fields) and all(historical.get(k)==v for k,v in hashes.items()))

def s049_diagnostic_evidence_valid(receipt):
    context=receipt.get('s049_diagnostic_context')
    if type(context)is not dict or context.get('kind')!=S049_DIAGNOSTIC_CONTEXT_KIND:return False
    identity=context.get('identity')
    if type(identity)is not dict or identity!=S049_IDENTITY_EXPECTED:return False
    if identity.get('data_bundle')!={'path':S049_PROJECTION17_BUNDLE_PATH,'sha256':S049_PROJECTION17_BUNDLE_SHA}:return False
    if not same_descriptor(receipt.get('data_bundle'),identity['data_bundle']):return False
    if context.get('physical_water_marker')!=S049_PHYSICAL_WATER_MARKER or context.get('camera_marker')!=S049_CAMERA_MARKER:return False
    preflight=context.get('preflight_evidence')
    try:
        if type(preflight)is not dict:return False
        preflight_path=Path(preflight['path']).resolve(strict=True)
        if sha(preflight_path)!=preflight['sha256']:return False
        document=read_json(preflight_path)
        if document.get('kind')!=S049_DIAGNOSTIC_PREFLIGHT_KIND or document.get('identity')!=identity or document.get('mission')!='S049' or document.get('seed')!=104734:return False
        if not same_descriptor(document.get('data_bundle'),identity['data_bundle']):return False
        native=document.get('native_data');selected=native.get('selected_runtime_map_preflight')if type(native)is dict else None
        if type(native)is not dict or type(selected)is not dict or native.get('status')!='Passed' or native.get('world_allocated')is not False or native.get('entity_ids_allocated')!=0:return False
        if selected.get('s049_physical_water_diagnostic')is not True or selected.get('active_mission_water_qualified')is not False or selected.get('source_fit_certified')is not False or selected.get('native_camera_context_qualified')is not False:return False
        if selected.get('actual_camera_profile_sha256')!=S049_IDENTITY_EXPECTED['camera_profile_sha256']:return False
        markers=native.get('s049_diagnostic_markers',{})
        if markers.get('physical_water')!=S049_PHYSICAL_WATER_MARKER or markers.get('camera')!=S049_CAMERA_MARKER:return False
        for key in ('native_preflight_log','native_preflight_process','native_preflight_request','native_preflight_input_pins'):
            descriptor=markers.get(key)
            if type(descriptor)is not dict or sha(Path(descriptor['path']).resolve(strict=True))!=descriptor['sha256']:return False
        native_log=Path(markers['native_preflight_log']['path']).resolve(strict=True)
        lines=native_log.read_text(errors='replace').splitlines()
        if lines.count(S049_PHYSICAL_WATER_MARKER)!=1 or lines.count(S049_CAMERA_MARKER)!=1:return False
        marker='[SourceRunPreflight] ';payload=[line[len(marker):]for line in lines if line.startswith(marker)]
        if len(payload)!=1:return False
        preflight_payload=json.loads(payload[0]);runtime=preflight_payload.get('selected_runtime_map_preflight',{})
        if preflight_payload.get('status')!='Passed' or preflight_payload.get('world_allocated')is not False or preflight_payload.get('entity_ids_allocated')!=0:return False
        if runtime.get('s049_physical_water_diagnostic')is not True or runtime.get('active_mission_water_qualified')is not False or runtime.get('source_fit_certified')is not False or runtime.get('native_camera_context_qualified')is not False:return False
        process=read_json(markers['native_preflight_process']['path']);argv=process.get('argv')
        if process.get('process_exit')!=0 or process.get('timed_out')is not False or type(argv)is not list or len(argv)<7 or argv[-2]!='--source-run-preflight' or argv[-1]!=markers['native_preflight_request']['path']:return False
        request=read_json(markers['native_preflight_request']['path'])
        if request.get('mission')!='S049' or request.get('seed')!=104734 or request.get('kind')!='SkirmishSourceSimulationRun':return False
        if identity.get('initialization',{}).get('path')!=request.get('initialization',{}).get('path') or identity.get('initialization',{}).get('sha256')!=request.get('initialization',{}).get('sha256'):return False
        if identity.get('fixed_map',{}).get('path')!=request.get('static_clearance',{}).get('path') or identity.get('fixed_map',{}).get('sha256')!=request.get('static_clearance',{}).get('sha256'):return False
        build=context.get('build_receipt')
        if type(build)is not dict or sha(Path(build['path']).resolve(strict=True))!=build['sha256']:return False
        if not same_descriptor(build,receipt.get('build_receipt')):return False
        build_value=read_json(build['path'])
        if build_value.get('kind')!='SkirmishSourceSimulationBuild' or build_value.get('status')!='Passed' or build_value.get('mission')!='S049' or build_value.get('seed')!=104734 or argv[-3]!=build_value.get('executable',{}).get('path'):return False
        input_pins=read_json(markers['native_preflight_input_pins']['path'])
        if input_pins.get(identity['initialization']['path'])!=identity['initialization']['sha256'] or input_pins.get(identity['fixed_map']['path'])!=identity['fixed_map']['sha256']:return False
        for driver in (Path('/Volumes/T7/Projects/WarlineCapture/Tools/CI/skirmish_source_build.py'),Path('/Volumes/T7/Projects/WarlineCapture/Tools/CI/run_skirmish_source_simulation.py')):
            driver=driver.resolve(strict=True);driver_hash=sha(driver)
            if input_pins.get(str(driver))!=driver_hash or build_value.get('source_and_reference_pins',{}).get(str(driver))!=driver_hash:return False
        worker=context.get('worker_process_log');worker_path=Path(worker['path']).resolve(strict=True)
        if sha(worker_path)!=worker['sha256']:return False
        worker_lines=worker_path.read_text(errors='replace').splitlines()
        if worker_lines.count(S049_PHYSICAL_WATER_MARKER)!=1 or worker_lines.count(S049_CAMERA_MARKER)!=1:return False
        pins=receipt.get('input_and_runtime_pins',{})
        for driver in (Path('/Volumes/T7/Projects/WarlineCapture/Tools/CI/skirmish_source_build.py'),Path('/Volumes/T7/Projects/WarlineCapture/Tools/CI/run_skirmish_source_simulation.py')):
            driver=driver.resolve(strict=True)
            if pins.get(str(driver))!=sha(driver):return False
        for descriptor in [preflight,*[markers[k] for k in ('native_preflight_log','native_preflight_process','native_preflight_request','native_preflight_input_pins')],build]:
            if pins.get(descriptor['path'])!=descriptor['sha256']:return False
        flags=context.get('runner_flags')
        return (type(flags)is dict and flags.get('physical_water')is True and flags.get('camera')is True and context.get('evidence_valid')is True)
    except (KeyError,ValueError,TypeError,OSError,json.JSONDecodeError):return False
def verify(pins):
    for path,digest in pins.items(): require(sha(path)==digest, 'Pinned input changed: '+path)
def is_external_build(data):
    mode=data.get('runtime_configuration_mode');backend=data.get('runtime_backend')
    require(mode in (None,'LegacyBoundProfilesV1','ExternalQualifiedGroundV1'),'Unsupported checked build configuration mode')
    require(mode!='ExternalQualifiedGroundV1' or backend=='TypedGroundS026V1','Generic build requires supported typed backend')
    return mode=='ExternalQualifiedGroundV1' and backend=='TypedGroundS026V1'
def require_build_target(data, mission, seed, expected_backend, derived=None):
    generic=is_external_build(data)
    require(data.get('runtime_backend')==expected_backend,'Bundle/build runtime backend mismatch')
    reusable=(derived is not None and data.get('s002_derived_capability')=='RuntimePinnedS002DerivedV1' and data.get('mission')=='S002' and mission=='S002' and expected_backend=='LegacyGroundS002V1' and derived[0].get('seed')==seed)
    if not generic and not reusable:require(data.get('mission')==mission and type(data.get('seed'))is int and data['seed']==seed,'Job/input identity mismatch')
    return generic

def checked_build_identity(data, job, repo, bundle):
    require(data.get('kind')=='SkirmishSourceSimulationBuild' and data.get('status')=='Passed' and type(data.get('compile_exit'))is int and data['compile_exit']==0 and data.get('repository_root')==str(repo),'Checked matching current-root build required')
    require_build_target(data,job['mission'],job['seed'],bundle.get('runtime_backend','LegacyGroundS002V1'))
    require(type(data.get('source_and_reference_pins'))is dict,'Actual checked build pins required');verify(data['source_and_reference_pins'])
    exe=data.get('executable');require(type(exe)is dict and type(exe.get('path'))is str and type(exe.get('sha256'))is str and sha(exe['path'])==exe['sha256'],'Built image changed')

def checked_s004_diagnostic_build(data, repo):
    require(data.get('kind')=='SkirmishSourceSimulationBuild' and data.get('status')=='Passed' and
            type(data.get('compile_exit')) is int and data['compile_exit']==0 and
            data.get('repository_root')==str(repo) and data.get('mission')=='S002',
            'Checked S004 diagnostic source-host build required')
    require(type(data.get('source_and_reference_pins')) is dict,'S004 diagnostic build pins required')
    verify(data['source_and_reference_pins'])
    exe=data.get('executable');require(type(exe)is dict and type(exe.get('path'))is str and
        type(exe.get('sha256'))is str and sha(exe['path'])==exe['sha256'],'S004 diagnostic image changed')

def load_jobs(path, repo):
    raw=read_json(path);object_keys(raw,{'schema_version','jobs'})
    require(type(raw['schema_version']) is int and raw['schema_version']==1 and type(raw['jobs']) is list and 0<len(raw['jobs'])<=360, 'schema1 with 1..360 explicit jobs required')
    jobs=[];ids=set();pins={str(path):sha(path)}
    for rawjob in raw['jobs']:
        object_keys(rawjob,{'id','mission','seed','data_bundle','timeout'}, {'build_receipt','max_ticks','configuration_qualification','source_repo','package_root','data_repo','s002_historical_input_diagnostic','s002_current_source_diagnostic_package','s004_startup_diagnostic'})
        job=dict(rawjob)
        for root_key in ('source_repo','package_root','data_repo'):
            if root_key in job:
                require(type(job[root_key]) is str and Path(job[root_key]).is_absolute(),'Absolute job root required')
                root=Path(job[root_key]).resolve(strict=True);require(root.is_dir(),'Job root directory required');job[root_key]=str(root)
        name=job['id'];require(type(name) is str and re.fullmatch('[A-Za-z0-9][A-Za-z0-9_-]{0,63}',name) and name.casefold() not in ids,'Duplicate/unsafe job ID');ids.add(name.casefold())
        require(type(job['mission']) is str and re.fullmatch('[A-Z][0-9]{3}',job['mission']) and type(job['seed']) is int,'Explicit mission/string and integer seed required')
        require(type(job['timeout']) in (int,float) and math.isfinite(job['timeout']) and job['timeout']>0,'Finite positive timeout required')
        if 's004_startup_diagnostic' in job:
            require(job['s004_startup_diagnostic'] is True and job['mission']=='S004' and job['seed']==104733 and
                    'build_receipt' in job and job['timeout']<=1800 and
                    not any(k in job for k in ('max_ticks','configuration_qualification','source_repo','package_root','data_repo','s002_historical_input_diagnostic','s002_current_source_diagnostic_package')),
                    'S004 startup diagnostic requires exact mission, seed, build and default roots')
        if 'max_ticks' in job: require(type(job['max_ticks']) is int and job['max_ticks']>0,'Positive integer diagnostic max_ticks required')
        if 's002_historical_input_diagnostic' in job: require(job['s002_historical_input_diagnostic'] is True and job['mission']=='S002' and type(job.get('max_ticks')) is int and 0<job['max_ticks']<=72000,'S002 historical-input diagnostic requires explicit S002 opt-in and bounded max_ticks')
        if 's002_current_source_diagnostic_package' in job:
            require(job.get('s002_historical_input_diagnostic') is True and job['mission']=='S002' and type(job.get('max_ticks')) is int and 0<job['max_ticks']<=72000,'Current S002 owner package requires bounded historical-input diagnostic opt-in')
        for key in ['data_bundle','build_receipt','configuration_qualification','s002_current_source_diagnostic_package']:
            if key not in job: continue
            if key=='configuration_qualification':
                raw=job[key]
                require(type(raw)is dict and type(raw.get('path'))is str,'Qualification path required')
                original=Path(raw['path']);original=original if original.is_absolute() else path.parent/original
                require(not any(part.is_symlink() for part in [original,*original.parents]),'Symlink in qualification path')
            job[key]=descriptor(job[key],path.parent);pins[job[key]['path']]=job[key]['sha256']
            data=read_json(job[key]['path']);require(type(data)is dict,'Input JSON object required')
            if key=='data_bundle':
                if job.get('s004_startup_diagnostic') is True:
                    require(data.get('kind')=='S004SourceDiagnosticInputBundle' and data.get('mission_id')=='S004' and
                            data.get('scope')=='SourceHostDiagnosticOnly' and type(data.get('qualification')) is dict and
                            data['qualification'].get('mission_ready') is False and data['qualification'].get('runtime_admission') is False,
                            'Unqualified S004 diagnostic bundle required')
                else:require(data.get('mission')==job['mission'] and type(data.get('seed'))is int and data['seed']==job['seed'],'Job/input identity mismatch')
                bundle=data
            elif key=='build_receipt':
                if job.get('s004_startup_diagnostic') is True:checked_s004_diagnostic_build(data,Path(repo))
                else:checked_build_identity(data,job,Path(job.get('source_repo',repo)),bundle)
            elif key=='s002_current_source_diagnostic_package':
                require(data.get('kind')=='S002CurrentSourceDiagnosticInputPackage' and data.get('scope')=='CurrentS002OwnerInputs_SourceHostDiagnosticOnly' and data.get('mission')=='S002' and data.get('seed')==104731 and data.get('setup_hash')==3657809016 and all(data.get(k) is False for k in ('native_context_qualified','source_fit_certified','mission_ready')),'Exact unqualified current S002 diagnostic package required')
                require(job['seed']==data['seed'],'Current S002 diagnostic package seed mismatch')
                source_root=Path(job.get('source_repo',repo)).resolve(strict=True)
                import sys as _sys
                if str(Path(__file__).resolve().parent) not in _sys.path:_sys.path.insert(0,str(Path(__file__).resolve().parent))
                import s002_current_source_diagnostic_package as _current_package
                loaded=_current_package.load_current_s002_package(job[key]['path'],job[key]['sha256'],source_root)
                pins.update(loaded['source_pins'])
        jobs.append(job)
    return jobs,pins

def launch(argv, log, timeout):
    # The normal CLI owns its worker deadline. The outer bound also covers build/cleanup.
    require(os.name=='posix','Owned process-tree timeout currently supports POSIX only')
    started=time.monotonic();timed=False;leftover=False
    with log.open('x') as stream:
        child=subprocess.Popen(argv,stdout=stream,stderr=subprocess.STDOUT,start_new_session=True)
        try: child.wait(timeout=timeout+180)
        except subprocess.TimeoutExpired:
            timed=True
            try: os.killpg(child.pid,signal.SIGTERM)
            except ProcessLookupError: pass
            try: child.wait(timeout=5)
            except subprocess.TimeoutExpired:
                try: os.killpg(child.pid,signal.SIGKILL)
                except ProcessLookupError: pass
                child.wait(timeout=5)
            # Parent reaping alone does not prove its owned descendants stopped.
            try: os.killpg(child.pid,signal.SIGKILL)
            except ProcessLookupError: pass
    if not timed:
        try: os.killpg(child.pid,0);leftover=True
        except ProcessLookupError: pass
        if leftover:
            for sig in (signal.SIGTERM,signal.SIGKILL):
                try: os.killpg(child.pid,sig)
                except ProcessLookupError: pass
    return {'cli_exit':child.returncode,'outer_timeout':timed,'owned_descendants_survived_cli':leftover,'owned_pid':child.pid,'wall_seconds':time.monotonic()-started}

def classify_s073_terminal_diagnostic(job,process,receipt):
    failed={'execution_complete':False,'mission_victory':None,'validation_passed':False,'status':'Failed'}
    if receipt.get('historical_input_diagnostic_requested') is True:
        return dict(failed,reason='HistoricalS073InputIsNotQualifiedTerminalDiagnostic')
    outcome=receipt.get('terminal_outcome_observed')
    if (job.get('mission')!='S073' or process.get('cli_exit')!=0 or
        receipt.get('status')!='DiagnosticTerminalOutcomeNativeReadinessUnverified' or
        receipt.get('process_exit')!=2 or receipt.get('timed_out') is not False or
        receipt.get('source_host_diagnostic_observed') is not True or
        receipt.get('full_outcome_observed') is not True or receipt.get('diagnostic_prefix_completed') is not False or
        receipt.get('terminal_phase_observed') is not True or outcome not in {'Victory','Defeat','Draw'} or
        receipt.get('validation_passed') is not False or receipt.get('mission_ready') is not False or
        receipt.get('map_blocker_parity_verified') is not False):
        return dict(failed,reason='InvalidOrContradictoryS073TerminalDiagnostic')
    descriptor=receipt.get('worker_result')
    if type(descriptor)is not dict or type(descriptor.get('path'))is not str or type(descriptor.get('sha256'))is not str:
        return dict(failed,reason='MissingS073WorkerResultPin')
    try:
        worker_path=Path(descriptor['path']).resolve(strict=True)
        if sha(worker_path)!=descriptor['sha256']:return dict(failed,reason='S073WorkerResultDigestMismatch')
        worker=read_json(worker_path)
    except (ValueError,TypeError,OSError,json.JSONDecodeError):
        return dict(failed,reason='UnreadableS073WorkerResult')
    admission=worker.get('source_host_diagnostic_gameplay_admission') or {}
    scheduler=worker.get('source_host_original_tactical_owner_scheduler_tick') or {}
    counts=scheduler.get('owner_update_counts') or {}
    aria=worker.get('source_host_aria_policy_step') or {}
    actual_aria=scheduler.get('aria_steps')
    ticks=scheduler.get('ticks_executed')
    requested_budget=job.get('max_ticks')
    budget_matches=(scheduler.get('tick_budget')=='until_original_terminal_outcome' if requested_budget is None else
        type(requested_budget)is int and requested_budget>0 and scheduler.get('tick_budget')==requested_budget and
        type(ticks)is int and 0<ticks<=requested_budget)
    if (worker.get('kind')!='SkirmishSourceSimulationRunReceipt' or worker.get('mission')!='S073' or
        worker.get('seed')!=job.get('seed') or worker.get('process_exit')!=2 or
        worker.get('status')!='DiagnosticTerminalOutcomeNativeReadinessUnverified' or
        worker.get('source_host_diagnostic_activation') is not True or
        worker.get('scheduler_tick_executed') is not True or
        worker.get('session_phase')!='Finished' or worker.get('session_outcome')!=outcome or
        worker.get('operation_map_physical_source_validated') is not False or worker.get('world_initialized') is not False or
        worker.get('mission_ready') is not False or worker.get('map_blocker_parity_verified') is not False or
        admission.get('opt_in') is not True or admission.get('shared_start_completion_predicate_passed') is not True or
        admission.get('physical_source_validated_before') is not False or admission.get('physical_source_validated_after') is not False or
        scheduler.get('status')!='RecurringOriginalOwnerTicksExecuted' or
        not budget_matches or type(ticks)is not int or ticks<=0 or
        scheduler.get('terminal_phase')!='Finished' or scheduler.get('terminal_outcome')!=outcome or
        any(counts.get(name)!=ticks for name in ('AIEconomySystem','AISquadSystem','SkirmishRulesSystem')) or
        aria.get('kind')!='S073LegacySharedAriaRecurringPolicySteps'):
        return dict(failed,reason='S073WorkerTerminalEvidenceMismatch')
    steps=aria.get('step_count');last=aria.get('last_step') or {};runtime=scheduler.get('original_production_runtime') or {}
    stopped_terminal=(scheduler.get('aria_stopped') is True and scheduler.get('aria_failure') is None and
        type(steps)is int and 0<steps<=ticks and isinstance(actual_aria,list) and len(actual_aria)==steps and
        type(scheduler.get('session_id')) is str and bool(scheduler.get('session_id')) and
        worker.get('session_id')==scheduler.get('session_id') and last.get('stopped') is True and
        actual_aria[-1]==last and last.get('frame')==steps and
        type(aria.get('accepted_action_count')) is int and scheduler.get('aria_actions_accepted')==aria['accepted_action_count'] and
        runtime.get('source_host_terminal_observed') is True and runtime.get('source_host_terminal_phase')=='Finished' and
        runtime.get('source_host_terminal_outcome')==outcome)
    if (not isinstance(actual_aria,list) or type(steps)is not int or
        steps!=len(actual_aria) or not 0<=steps<=ticks or (not stopped_terminal and ticks-steps not in (0,1)) or
        aria.get('readiness_claimed') is not False or aria.get('mission_outcome_claimed') is not False):
        return dict(failed,reason='S073WorkerTerminalCountsOrQualificationMismatch')
    return {'execution_complete':True,'mission_victory':None,'validation_passed':False,
        'status':'DiagnosticTerminalOutcomeObserved','diagnostic_source_outcome':outcome,
        'reason':'NaturalUnqualifiedS073TerminalOutcome'}

def classify_s004_startup_diagnostic(job, process, receipt, cli_output):
    failed={'execution_complete':False,'mission_victory':None,'validation_passed':False,'status':'Failed'}
    try:
        require(job.get('s004_startup_diagnostic') is True and job.get('mission')=='S004' and job.get('seed')==104733,
                'Explicit S004 startup diagnostic identity required')
        require(process.get('outer_timeout') is False and process.get('owned_descendants_survived_cli') is False and
                type(process.get('cli_exit')) is int and process['cli_exit']==2,'Completed S004 CLI diagnostic required')
        require(type(receipt) is dict and receipt.get('status')=='SourceStartupDiagnosticObserved' and
                receipt.get('mission')=='S004' and type(receipt.get('worker_exit')) is int and receipt['worker_exit']==0 and
                receipt.get('bundle_sha256')==job['data_bundle']['sha256'] and receipt.get('resource_owner_diagnostic') is None and
                receipt.get('scheduler_executed') is False and receipt.get('validation_passed') is False and
                receipt.get('mission_victory') is None and receipt.get('mission_ready') is False,
                'Contradictory S004 startup receipt')
        worker_descriptor=receipt.get('worker_receipt');require(type(worker_descriptor) is dict and set(worker_descriptor)=={'path','sha256'},'S004 worker receipt pin required')
        worker_candidate=Path(cli_output)/'worker/receipt.json'
        require(not worker_candidate.is_symlink() and not worker_candidate.parent.is_symlink(),'S004 worker receipt must not be symlinked')
        worker_path=worker_candidate.resolve(strict=True)
        require(worker_descriptor.get('path')==str(worker_path) and worker_descriptor.get('sha256')==sha(worker_path),
                'S004 worker receipt identity mismatch')
        worker=read_json(worker_path);pins=worker.get('input_sha256');result=worker.get('result')
        require(worker.get('status')=='ObservedDiagnosticSourcePlacement' and worker.get('physical_source_validated') is False and
                worker.get('runtime_admission') is False and worker.get('mission_ready') is False and
                type(pins) is dict and pins.get(job['data_bundle']['path'])==job['data_bundle']['sha256'] and
                pins.get(job['build_receipt']['path'])==job['build_receipt']['sha256'] and type(result) is dict and
                result.get('source_bundle_preflight_passed_before_world') is True and
                type(result.get('source_simulation_active')) is int and result['source_simulation_active']==0 and result.get('retained_world_scheduler_admitted') is False and
                type(result.get('registered')) is int and result['registered']==20 and type(result.get('actors_created')) is int and result['actors_created']==54 and
                result.get('direct_resource_owner_diagnostic') is None and result.get('resource_cadence_diagnostic') is None,
                'S004 worker evidence differs')
        verify(pins)
        return dict(failed,status='S004SourceStartupDiagnosticObserved',diagnostic_stage_complete=True,
                    reason='StartupOnly_NoMissionOutcomeOrSchedulerAdmission')
    except (ValueError,TypeError,KeyError,OSError,json.JSONDecodeError) as error:
        return dict(failed,reason='InvalidS004StartupDiagnostic: '+str(error))

def classify(job, process, receipt):
    failed={'execution_complete':False,'mission_victory':None,'validation_passed':False,'status':'Failed'}
    if process.get('owned_descendants_survived_cli',False):return dict(failed,reason='UnexpectedOwnedDescendantsAfterCLI')
    if process.get('outer_timeout') is not False:return dict(failed,reason='OuterTimeoutOrMissingProcessEvidence')
    if type(process.get('cli_exit')) is not int:return dict(failed,reason='MissingProcessExit')
    if type(receipt) is not dict:return dict(failed,status='AdmissionRejected' if process['cli_exit']==2 else 'MissingReceipt',reason='NoNormalRunReceipt')
    if 'diagnostic_only' in receipt and type(receipt['diagnostic_only']) is not bool:return dict(failed,reason='MalformedDiagnosticFlag')
    receipt_bundle=receipt.get('data_bundle')
    job_bundle=job.get('data_bundle')
    receipt_pins=receipt.get('input_and_runtime_pins')
    epoch=S049_IDENTITY_EXPECTED['metadata_epoch']
    initialization=S049_IDENTITY_EXPECTED['initialization']
    pinned_epoch=(type(receipt_pins)is dict and (receipt_pins.get(epoch['path'])==epoch['sha256'] or receipt_pins.get(initialization['path'])==initialization['sha256']))
    s049_projection17_path=(job.get('mission')=='S049' and (
        type(receipt_bundle)is dict and (receipt_bundle.get('path')==S049_PROJECTION17_BUNDLE_PATH or receipt_bundle.get('sha256')==S049_PROJECTION17_BUNDLE_SHA)
        or type(job_bundle)is dict and (job_bundle.get('path')==S049_PROJECTION17_BUNDLE_PATH or job_bundle.get('sha256')==S049_PROJECTION17_BUNDLE_SHA)
        or pinned_epoch))
    s049_projection17=(s049_projection17_path and type(receipt_bundle)is dict and receipt_bundle.get('sha256')==S049_PROJECTION17_BUNDLE_SHA)
    if s049_projection17_path and not s049_projection17:return dict(failed,reason='S049Projection17BundleDigestMismatch')
    if s049_projection17 and receipt.get('diagnostic_only') is not True:return dict(failed,reason='S049Projection17DiagnosticClassificationMissing')
    if receipt.get("diagnostic_only") is True:
        # Older S003 bounded-prefix receipts omitted two unqualified fields.
        raw_prefix=receipt.get("source_result")
        legacy_s003_prefix=(job.get("mission")=="S003" and receipt.get("status")=="BoundedPrefix" and type(raw_prefix)is dict and raw_prefix.get("status")=="BoundedPrefix" and raw_prefix.get("source_outcome") in (None,"None") and receipt.get("diagnostic_prefix_completed") is True and receipt.get("full_outcome_observed") is False and "max_ticks" in job and process["cli_exit"]==0)
        legacy_s003_terminal=(job.get("mission")=="S003" and receipt.get("status")=="SourceOutcomeObserved" and type(raw_prefix)is dict and raw_prefix.get("status")=="SourceTerminal" and raw_prefix.get("source_outcome") in ("Victory","Defeat") and receipt.get("diagnostic_prefix_completed") is False and receipt.get("full_outcome_observed") is True and process["cli_exit"]==1)
        required=("camera_qualified","active_mission_water_qualified","source_fit_certified","native_acceptance")
        optional_legacy=("runtime_presentation_qualified","mission_ready")
        if receipt.get("validation_passed") is not False or any(receipt.get(k) is not False for k in required) or any((k in receipt and receipt[k] is not False) or (k not in receipt and not (legacy_s003_prefix or legacy_s003_terminal)) for k in optional_legacy):return dict(failed,reason="ContradictoryDiagnosticQualification")
        if job.get('mission')=='S049' and (not s049_projection17 or not s049_diagnostic_evidence_valid(receipt) or any(receipt.get(k)is not False for k in ('motion_reference_passed','placement_qualified','runtime_presentation_qualified','mission_ready','qualified_source_mission'))):return dict(failed,reason='MissingOrContradictoryS049DiagnosticEvidence')
    if receipt.get('kind')!='SkirmishSourceSimulationRunReceipt' or receipt.get('schema_version')!=1 or type(receipt.get('schema_version')) is not int:return dict(failed,reason='MalformedReceipt')
    if receipt.get('mission')!=job['mission'] or type(receipt.get('seed')) is not int or receipt['seed']!=job['seed']:return dict(failed,reason='ReceiptIdentityMismatch')
    raw_candidate=receipt.get('source_result')
    package_job=job.get('s002_current_source_diagnostic_package')
    package_receipt=receipt.get('s002_current_source_diagnostic_package')
    package_worker=raw_candidate.get('s002_current_source_diagnostic_package') if type(raw_candidate)is dict else None
    if package_job is not None or package_receipt is not None or package_worker is not None:
        if package_job is None or not same_descriptor(package_receipt,package_job) or not same_descriptor(package_worker,package_job):return dict(failed,reason='CurrentS002DiagnosticPackageMissingOrAliased')
        if not valid_current_s002_package_binding(package_job,receipt.get('input_and_runtime_pins')):return dict(failed,reason='CurrentS002DiagnosticPackageSourceOwnerMismatch')
        if job.get('mission')!='S002' or job.get('s002_historical_input_diagnostic') is not True or type(job.get('max_ticks')) is not int or receipt.get('diagnostic_only') is not True or receipt.get('validation_passed') is not False or receipt.get('mission_ready') is not False or receipt.get('source_fit_certified') is not False or receipt.get('camera_qualified') is not False or receipt.get('active_mission_water_qualified') is not False or receipt.get('native_acceptance') is not False or receipt.get('runtime_presentation_qualified') is not False:
            return dict(failed,reason='ContradictoryCurrentS002DiagnosticPackageClassification')
        if receipt.get('process_exit')!=0 or receipt.get('timed_out') is not False or process.get('outer_timeout') is not False or process.get('cli_exit') not in (0,1):return dict(failed,reason='CurrentS002DiagnosticExecutionIncomplete')
        if 's002_current_source_diagnostic_package_bound_by_worker' in receipt and receipt.get('s002_current_source_diagnostic_package_bound_by_worker') is not True:return dict(failed,reason='CurrentS002WorkerPackageBindingFalse')
        data_root=receipt.get('data_repository_root')
        if type(data_root)is not str or not Path(data_root).is_absolute():return dict(failed,reason='CurrentS002HistoricalDataRootMissing')
        expected_historical_pins={
            str(Path(data_root)/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json'):'8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8',
            str(Path(data_root)/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-water-capture.json'):'acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5',
            str(Path(data_root)/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-startup-observation.json'):'ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03',
            str(Path(data_root)/'Logs/SkirmishProduction/s073-captured-source-preservation-20261101-r1/receipt.json'):'68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297',
        }
        receipt_pins=receipt.get('input_and_runtime_pins')
        if type(receipt_pins)is not dict or any(receipt_pins.get(path)!=digest for path,digest in expected_historical_pins.items()):return dict(failed,reason='CurrentS002HistoricalInputPinsMissingOrMismatched')
        if not valid_s002_historical_classification(job,raw_candidate):return dict(failed,reason='CurrentS002HistoricalClassificationMissingOrMismatched')
        bounded=(receipt.get('status')=='BoundedPrefix' and raw_candidate.get('status')=='BoundedPrefix' and raw_candidate.get('source_outcome') in (None,'None') and receipt.get('diagnostic_prefix_completed') is True and receipt.get('full_outcome_observed') is False and process.get('cli_exit')==0)
        terminal=(receipt.get('status')=='SourceOutcomeObserved' and raw_candidate.get('status')=='SourceTerminal' and raw_candidate.get('source_outcome') in ('Victory','Defeat') and receipt.get('diagnostic_prefix_completed') is False and receipt.get('full_outcome_observed') is True and process.get('cli_exit')==1)
        if not (bounded or terminal):return dict(failed,reason='CurrentS002DiagnosticStatusOutcomeOrCliExitMismatch')
        return {'execution_complete':True,'mission_victory':None,'validation_passed':False,'status':'S002CurrentOwnerInputsHistoricalPhysicalDiagnostic','source_outcome':raw_candidate.get('source_outcome'),'reason':'Current owner inputs with retained historical physical inputs are diagnostic only'}
    historical=raw_candidate.get('historical_input_classification') if type(raw_candidate)is dict else None
    if historical is not None:
        false_fields=('current_source_equivalence','current_blocker_equivalence','current_surface_equivalence','physical_source_fit','water_equivalence','scene_fit_certified','mission_admitted','mission_ready','native_world_created','native_scene_loaded','native_input_observed','player_order_observed','validated_victory')
        capture_hashes={'physical_sha256':'8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8','water_sha256':'acb0711c4e7046b70a696ee8fa935eef8a88c00d4dc23929ccb2b7812fcc15f5','startup_sha256':'ceed295e16b473fbcba7444c6daa6347b5940c39dd6e80496321cb479c554c03','preservation_sha256':'68b2a130fe08e3f952b1a8c1c0603b082ec38c7feb8b4581e1153cba7665e297'}
        if job.get('mission')!='S002' or historical.get('kind')!='HistoricalInputOnlyDiagnostic_Unqualified' or historical.get('historical_input_epoch')!='2026-11-01' or historical.get('historical_producer_digest_bytes_verified') is not False or historical.get('historical_producer_digest_only_count')!=7 or historical.get('preserved_source_pin_count')!=6526 or historical.get('preservation_project_file_count')!=6522 or historical.get('preservation_external_pin_count')!=4 or historical.get('historical_navigation_row_count')!=4954 or any(historical.get(k)is not False for k in false_fields) or any(historical.get(k)!=v for k,v in capture_hashes.items()):return dict(failed,reason='MalformedHistoricalInputOnlyClassification')
        if receipt.get('diagnostic_only') is not True or receipt.get('validation_passed') is not False or any(receipt.get(k)is not False for k in ('camera_qualified','active_mission_water_qualified','source_fit_certified','native_acceptance','runtime_presentation_qualified','mission_ready')) or receipt.get('process_exit')!=0 or process.get('outer_timeout') is not False or receipt.get('timed_out') is not False:return dict(failed,reason='ContradictoryHistoricalInputOnlyReceipt')
        if receipt.get('status') not in ('SourceOutcomeObserved','BoundedPrefix') or raw_candidate.get('status') not in ('SourceTerminal','BoundedPrefix'):return dict(failed,status='Failed',reason='HistoricalInputOnlySourceRunDidNotComplete')
        expected_cli_exit=0 if raw_candidate.get('status')=='BoundedPrefix' else 1
        if process['cli_exit']!=expected_cli_exit:return dict(failed,reason='HistoricalInputOnlyCLIExitMismatch')
        return {'execution_complete':True,'mission_victory':None,'validation_passed':False,'status':'HistoricalInputOnlyDiagnostic','historical_source_outcome':raw_candidate.get('source_outcome'),'reason':'Retained historical inputs are unqualified; source-host outcome cannot validate victory'}
    if job.get('mission')=='S073' and (receipt.get('source_host_diagnostic_observed') is True or receipt.get('status')=='DiagnosticTerminalOutcomeNativeReadinessUnverified'):
        return classify_s073_terminal_diagnostic(job,process,receipt)
    if type(receipt.get('process_exit')) is not int or receipt['process_exit']!=0 or receipt.get('timed_out') is not False:return dict(failed,reason='SourceProcessFailureOrTimeout')
    raw=receipt.get('source_result');status=receipt.get('status')
    if type(raw) is not dict:return dict(failed,reason='MissingSourceResult')
    if status=='BoundedPrefix' and 'max_ticks' in job and raw.get('status')=='BoundedPrefix' and raw.get('source_outcome') in (None,'None') and receipt.get('diagnostic_prefix_completed') is True and receipt.get('full_outcome_observed') is False and receipt.get('validation_passed') is False and process['cli_exit']==0:
        return {'execution_complete':True,'mission_victory':False,'validation_passed':False,'status':'BoundedPrefix','reason':'ActualNormalDiagnosticPrefix'}
    outcome=raw.get('source_outcome')
    if status in ('SourceOutcomeObserved','DiagnosticSourceOutcomeObserved') and raw.get('status')=='SourceTerminal' and outcome in ('Victory','Defeat') and receipt.get('full_outcome_observed') is True and receipt.get('diagnostic_prefix_completed') is False:
        won=outcome=='Victory'
        # A source-only diagnostic may complete with a terminal result while
        # intentionally returning exit 1 and keeping validation false.
        if receipt.get('diagnostic_only') is True and receipt.get('validation_passed') is False and process['cli_exit']==1 :
            if job.get('mission')=='S049' and status!='DiagnosticSourceOutcomeObserved':return dict(failed,reason='S049DiagnosticTerminalStatusRequired')
            return {'execution_complete':True,'mission_victory':None,'validation_passed':False,'status':'DiagnosticSourceOutcomeObserved','diagnostic_source_outcome':outcome,'unqualified_fields_missing':[k for k in ('runtime_presentation_qualified','mission_ready') if k not in receipt],'reason':'ActualUnqualifiedSourceTerminal'}
        if receipt.get('diagnostic_only') is True:return dict(failed,reason='ContradictoryDiagnosticQualification')
        # Existing normal CLI returns1 for a validDefeat because validation_passed meansVictory.
        if receipt.get('validation_passed') is won and process['cli_exit']==(0 if won else 1):
            return {'execution_complete':True,'mission_victory':won,'validation_passed':won,'status':status,'mission_outcome':outcome,'reason':'ActualNormalSourceTerminal'}
    return dict(failed,status=status if status in ('Unknown','Unsupported')else 'Failed',reason='FailedUnsupportedOrContradictoryReceipt')

def execute(jobs_file, output, repo, package, data_repo, run_process=launch):
    jobs_file=Path(jobs_file).resolve(strict=True);repo=Path(repo).resolve(strict=True);package=Path(package).resolve(strict=True);data_repo=Path(data_repo).resolve(strict=True);output=Path(output).absolute()
    require(not output.exists() and not output.is_symlink(),'Fresh batch output required; no overwrite/resume')
    cli=package/'Tools/CI/run_skirmish_source_simulation.py';require(cli.is_file(),'Normal CLI entry required')
    jobs,pins=load_jobs(jobs_file,repo);classifier_pin=batch_classifier_pin();pins[classifier_pin['path']]=classifier_pin['sha256'];pins[str(cli)]=sha(cli)
    if any(job.get('s004_startup_diagnostic') is True for job in jobs):
        require(repo==package==data_repo,'S004 startup diagnostic requires one current source root')
        require(os.environ.get('WARLINE_S004_DIRECT_RESOURCE_OWNER_DIAGNOSTIC')!='1' and
                os.environ.get('WARLINE_S004_RESOURCE_CADENCE_DIAGNOSTIC')!='1',
                'S004 startup batch cannot inherit a separate resource-owner diagnostic')
        for relative in ('Tools/CI/run_skirmish_s004_source_diagnostic.py',
                         'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/run_s004_bundle_driven_startup.py',
                         'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004BundlePreflight.cs'):
            path=repo/relative;require(path.is_file(),'S004 startup owner source required');pins[str(path)]=sha(path)
    for job in jobs:
        job_cli=Path(job.get('package_root',package))/'Tools/CI/run_skirmish_source_simulation.py';require(job_cli.is_file(),'Normal job CLI entry required');pins[str(job_cli)]=sha(job_cli)
    verify(pins);output.mkdir(parents=True);write(output/'input-pins.json',pins)
    summary={'kind':'SequentialNormalSkirmishSourceBatch','schema_version':1,'status':'Running','jobs':[],'parallel':False,'native_or_player_acceptance':False,'batch_classifier_source':classifier_pin}
    write(output/'batch-receipt.json',summary)
    default_repo,default_package,default_data_repo=repo,package,data_repo
    for job in jobs:
        repo=Path(job.get('source_repo',default_repo));package=Path(job.get('package_root',default_package));data_repo=Path(job.get('data_repo',default_data_repo));cli=package/'Tools/CI/run_skirmish_source_simulation.py'
        container=output/job['id'];container.mkdir();cli_output=container/'cli'
        if job.get('s004_startup_diagnostic') is True:
            argv=[sys.executable,'-B',str(cli),'s004-diagnostic','--mission','S004','--seed',str(job['seed']),
                  '--data-bundle',job['data_bundle']['path'],'--data-bundle-sha256',job['data_bundle']['sha256'],
                  '--output',str(cli_output),'--timeout',str(job['timeout'])]
        else:
            argv=[sys.executable,'-B',str(cli),'run','--repo',str(repo),'--package-root',str(package),'--data-repo',str(data_repo),'--mission',job['mission'],'--seed',str(job['seed']),'--data-bundle',job['data_bundle']['path'],'--output',str(cli_output),'--timeout',str(job['timeout'])]
        if 'build_receipt' in job:argv+=['--build-receipt',job['build_receipt']['path']]
        if 'max_ticks' in job:argv+=['--max-ticks',str(job['max_ticks'])]
        if 'configuration_qualification' in job:argv+=['--configuration-qualification',job['configuration_qualification']['path']]
        if job.get('s002_historical_input_diagnostic') is True:argv+=['--s002-historical-input-diagnostic']
        if 's002_current_source_diagnostic_package' in job:argv+=['--s002-current-source-diagnostic-package',job['s002_current_source_diagnostic_package']['path'],'--s002-current-source-diagnostic-package-sha256',job['s002_current_source_diagnostic_package']['sha256']]
        record={'id':job['id'],'mission':job['mission'],'seed':job['seed'],'job':job,'argv':argv,'cwd':str(Path.cwd()),'normal_cli_pin':{'path':str(cli),'sha256':pins[str(cli)]},'batch_classifier_pin':classifier_pin,'inputs':{k:job[k]for k in ('data_bundle','build_receipt','configuration_qualification','s002_current_source_diagnostic_package')if k in job},'s002_historical_input_diagnostic':job.get('s002_historical_input_diagnostic',False),'log':str(container/'cli-process.log')}
        write(container/'reproduce.json',record)
        try:
            verify(pins)
            process=run_process(argv,container/'cli-process.log',job['timeout'])
        except Exception as error:process={'cli_exit':None,'outer_timeout':False,'launch_error':str(error)}
        record['process']=process;paths=[x for x in [cli_output/'run-receipt.json',cli_output/'run/run-receipt.json']if x.is_file()];receipt=None
        try:
            require(len(paths)==1,'Exactly one normal run receipt required')
            receipt=read_json(paths[0]);record['run_receipt']={'path':str(paths[0]),'sha256':sha(paths[0])};record['normal_receipt_status']=receipt.get('status') if type(receipt)is dict else None;record['source_result_status']=receipt.get('source_result',{}).get('status') if type(receipt)is dict and type(receipt.get('source_result'))is dict else None
            if job.get('s004_startup_diagnostic') is True:
                checked_s004_diagnostic_build(read_json(job['build_receipt']['path']),repo)
                record['reported_build_receipt']=job['build_receipt']
            else:
                require(type(receipt)is dict and receipt.get('source_repository_root')==str(repo) and receipt.get('data_repository_root')==str(data_repo),'Receipt repository roots mismatch')
                require(same_descriptor(receipt.get('data_bundle'),job['data_bundle']),'Receipt bundle identity mismatch')
                if 'build_receipt' in job:require(same_descriptor(receipt.get('build_receipt'),job['build_receipt']),'Receipt build identity mismatch')
                record['receipt_backend_fields']={k:v for k,v in receipt.items()if 'backend'in k}
                actual_build=receipt.get('build_receipt');require(type(actual_build)is dict and type(actual_build.get('path'))is str and type(actual_build.get('sha256'))is str,'Normal receipt must identify checked build')
                build_path=Path(actual_build['path']).resolve(strict=True);require(sha(build_path)==actual_build['sha256'],'Reported build receipt digest mismatch')
                build=read_json(build_path);require(type(build)is dict,'Reported build object required');checked_build_identity(build,job,repo,read_json(job['data_bundle']['path']))
                record['reported_build_receipt']=actual_build;record['build_backend_fields']={k:v for k,v in build.items()if 'backend'in k}
        except (ValueError,TypeError,KeyError,OSError,json.JSONDecodeError) as error:record['receipt_error']=str(error);receipt=None
        require_batch_classifier_pin(classifier_pin)
        try:
            verify(pins);record.update(classify_s004_startup_diagnostic(job,process,receipt,cli_output) if job.get('s004_startup_diagnostic') is True else classify(job,process,receipt));verify(pins)
        except (ValueError,OSError)as error:record.update(execution_complete=False,mission_victory=None,validation_passed=False,status='InputDrift',reason=str(error))
        write(container/'job-receipt.json',record);summary['jobs'].append(record);write(output/'batch-receipt.json',summary)
    require_batch_classifier_pin(classifier_pin)
    try:verify(pins)
    except (ValueError,OSError) as error:
        summary['final_input_drift']=str(error)
        for record in summary['jobs']:
            record.update(execution_complete=False,mission_victory=None,validation_passed=False,status='InputDrift',reason=str(error))
            write(output/record['id']/'job-receipt.json',record)
    summary['all_jobs_execution_complete']=all(x['execution_complete']for x in summary['jobs']);summary['all_missions_victorious']=all(x['mission_victory']is True for x in summary['jobs']);summary['status']='Completed'if summary['all_jobs_execution_complete']else'Incomplete';summary['counts']={name:sum(x['status']==name for x in summary['jobs'])for name in sorted({x['status']for x in summary['jobs']})};write(output/'batch-receipt.json',summary);return summary

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['jobs','output','repo','package-root','data-repo']:p.add_argument('--'+name,required=True)
    a=p.parse_args()
    try:r=execute(a.jobs,a.output,a.repo,a.package_root,a.data_repo);print(json.dumps({k:r[k]for k in ['status','counts','all_jobs_execution_complete','all_missions_victorious']}));return 0 if r['all_jobs_execution_complete']else 1
    except (ValueError,OSError,json.JSONDecodeError)as error:print('Batch input rejected: '+str(error),file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
