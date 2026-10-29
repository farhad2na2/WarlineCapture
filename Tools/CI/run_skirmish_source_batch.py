"""Sequential normal-CLI jobs; no simulation, parallelism, caching or resume."""
import argparse, hashlib, json, math, os, re, signal, subprocess, sys, time
from pathlib import Path

def require(ok, message):
    if not ok: raise ValueError(message)
def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
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

def load_jobs(path, repo):
    raw=read_json(path);object_keys(raw,{'schema_version','jobs'})
    require(type(raw['schema_version']) is int and raw['schema_version']==1 and type(raw['jobs']) is list and 0<len(raw['jobs'])<=360, 'schema1 with 1..360 explicit jobs required')
    jobs=[];ids=set();pins={str(path):sha(path)}
    for rawjob in raw['jobs']:
        object_keys(rawjob,{'id','mission','seed','data_bundle','timeout'}, {'build_receipt','max_ticks','configuration_qualification','source_repo','package_root','data_repo'})
        job=dict(rawjob)
        for root_key in ('source_repo','package_root','data_repo'):
            if root_key in job:
                require(type(job[root_key]) is str and Path(job[root_key]).is_absolute(),'Absolute job root required')
                root=Path(job[root_key]).resolve(strict=True);require(root.is_dir(),'Job root directory required');job[root_key]=str(root)
        name=job['id'];require(type(name) is str and re.fullmatch('[A-Za-z0-9][A-Za-z0-9_-]{0,63}',name) and name.casefold() not in ids,'Duplicate/unsafe job ID');ids.add(name.casefold())
        require(type(job['mission']) is str and re.fullmatch('[A-Z][0-9]{3}',job['mission']) and type(job['seed']) is int,'Explicit mission/string and integer seed required')
        require(type(job['timeout']) in (int,float) and math.isfinite(job['timeout']) and job['timeout']>0,'Finite positive timeout required')
        if 'max_ticks' in job: require(type(job['max_ticks']) is int and job['max_ticks']>0,'Positive integer diagnostic max_ticks required')
        for key in ['data_bundle','build_receipt','configuration_qualification']:
            if key not in job: continue
            if key=='configuration_qualification':
                raw=job[key]
                require(type(raw)is dict and type(raw.get('path'))is str,'Qualification path required')
                original=Path(raw['path']);original=original if original.is_absolute() else path.parent/original
                require(not any(part.is_symlink() for part in [original,*original.parents]),'Symlink in qualification path')
            job[key]=descriptor(job[key],path.parent);pins[job[key]['path']]=job[key]['sha256']
            data=read_json(job[key]['path']);require(type(data)is dict,'Input JSON object required')
            if key=='data_bundle':
                require(data.get('mission')==job['mission'] and type(data.get('seed'))is int and data['seed']==job['seed'],'Job/input identity mismatch');bundle=data
            elif key=='build_receipt':checked_build_identity(data,job,Path(job.get('source_repo',repo)),bundle)
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

def classify(job, process, receipt):
    failed={'execution_complete':False,'mission_victory':None,'validation_passed':False,'status':'Failed'}
    if process.get('owned_descendants_survived_cli',False):return dict(failed,reason='UnexpectedOwnedDescendantsAfterCLI')
    if process.get('outer_timeout') is not False:return dict(failed,reason='OuterTimeoutOrMissingProcessEvidence')
    if type(process.get('cli_exit')) is not int:return dict(failed,reason='MissingProcessExit')
    if type(receipt) is not dict:return dict(failed,status='AdmissionRejected' if process['cli_exit']==2 else 'MissingReceipt',reason='NoNormalRunReceipt')
    if 'diagnostic_only' in receipt and type(receipt['diagnostic_only']) is not bool:return dict(failed,reason='MalformedDiagnosticFlag')
    if receipt.get("diagnostic_only") is True:
        # Older S003 bounded-prefix receipts omitted two unqualified fields.
        raw_prefix=receipt.get("source_result")
        legacy_s003_prefix=(job.get("mission")=="S003" and receipt.get("status")=="BoundedPrefix" and type(raw_prefix)is dict and raw_prefix.get("status")=="BoundedPrefix" and raw_prefix.get("source_outcome") in (None,"None") and receipt.get("diagnostic_prefix_completed") is True and receipt.get("full_outcome_observed") is False and "max_ticks" in job and process["cli_exit"]==0)
        legacy_s003_terminal=(job.get("mission")=="S003" and receipt.get("status")=="SourceOutcomeObserved" and type(raw_prefix)is dict and raw_prefix.get("status")=="SourceTerminal" and raw_prefix.get("source_outcome") in ("Victory","Defeat") and receipt.get("diagnostic_prefix_completed") is False and receipt.get("full_outcome_observed") is True and process["cli_exit"]==1)
        required=("camera_qualified","active_mission_water_qualified","source_fit_certified","native_acceptance")
        optional_legacy=("runtime_presentation_qualified","mission_ready")
        if receipt.get("validation_passed") is not False or any(receipt.get(k) is not False for k in required) or any((k in receipt and receipt[k] is not False) or (k not in receipt and not (legacy_s003_prefix or legacy_s003_terminal)) for k in optional_legacy):return dict(failed,reason="ContradictoryDiagnosticQualification")
    if receipt.get('kind')!='SkirmishSourceSimulationRunReceipt' or receipt.get('schema_version')!=1 or type(receipt.get('schema_version')) is not int:return dict(failed,reason='MalformedReceipt')
    if receipt.get('mission')!=job['mission'] or type(receipt.get('seed')) is not int or receipt['seed']!=job['seed']:return dict(failed,reason='ReceiptIdentityMismatch')
    if type(receipt.get('process_exit')) is not int or receipt['process_exit']!=0 or receipt.get('timed_out') is not False:return dict(failed,reason='SourceProcessFailureOrTimeout')
    raw=receipt.get('source_result');status=receipt.get('status')
    if type(raw) is not dict:return dict(failed,reason='MissingSourceResult')
    if status=='BoundedPrefix' and 'max_ticks' in job and raw.get('status')=='BoundedPrefix' and raw.get('source_outcome') in (None,'None') and receipt.get('diagnostic_prefix_completed') is True and receipt.get('full_outcome_observed') is False and receipt.get('validation_passed') is False and process['cli_exit']==0:
        return {'execution_complete':True,'mission_victory':False,'validation_passed':False,'status':'BoundedPrefix','reason':'ActualNormalDiagnosticPrefix'}
    outcome=raw.get('source_outcome')
    if status=='SourceOutcomeObserved' and raw.get('status')=='SourceTerminal' and outcome in ('Victory','Defeat') and receipt.get('full_outcome_observed') is True and receipt.get('diagnostic_prefix_completed') is False:
        won=outcome=='Victory'
        # A source-only diagnostic may complete with a terminal result while
        # intentionally returning exit 1 and keeping validation false.
        if receipt.get('diagnostic_only') is True and receipt.get('validation_passed') is False and process['cli_exit']==1 :
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
    jobs,pins=load_jobs(jobs_file,repo);pins[str(cli)]=sha(cli)
    for job in jobs:
        job_cli=Path(job.get('package_root',package))/'Tools/CI/run_skirmish_source_simulation.py';require(job_cli.is_file(),'Normal job CLI entry required');pins[str(job_cli)]=sha(job_cli)
    verify(pins);output.mkdir(parents=True);write(output/'input-pins.json',pins)
    summary={'kind':'SequentialNormalSkirmishSourceBatch','schema_version':1,'status':'Running','jobs':[],'parallel':False,'native_or_player_acceptance':False}
    write(output/'batch-receipt.json',summary)
    default_repo,default_package,default_data_repo=repo,package,data_repo
    for job in jobs:
        repo=Path(job.get('source_repo',default_repo));package=Path(job.get('package_root',default_package));data_repo=Path(job.get('data_repo',default_data_repo));cli=package/'Tools/CI/run_skirmish_source_simulation.py'
        container=output/job['id'];container.mkdir();cli_output=container/'cli';argv=[sys.executable,'-B',str(cli),'run','--repo',str(repo),'--package-root',str(package),'--data-repo',str(data_repo),'--mission',job['mission'],'--seed',str(job['seed']),'--data-bundle',job['data_bundle']['path'],'--output',str(cli_output),'--timeout',str(job['timeout'])]
        if 'build_receipt' in job:argv+=['--build-receipt',job['build_receipt']['path']]
        if 'max_ticks' in job:argv+=['--max-ticks',str(job['max_ticks'])]
        if 'configuration_qualification' in job:argv+=['--configuration-qualification',job['configuration_qualification']['path']]
        record={'id':job['id'],'mission':job['mission'],'seed':job['seed'],'job':job,'argv':argv,'cwd':str(Path.cwd()),'normal_cli_pin':{'path':str(cli),'sha256':pins[str(cli)]},'inputs':{k:job[k]for k in ('data_bundle','build_receipt','configuration_qualification')if k in job},'log':str(container/'cli-process.log')}
        write(container/'reproduce.json',record)
        try:
            verify(pins)
            process=run_process(argv,container/'cli-process.log',job['timeout'])
        except Exception as error:process={'cli_exit':None,'outer_timeout':False,'launch_error':str(error)}
        record['process']=process;paths=[x for x in [cli_output/'run-receipt.json',cli_output/'run/run-receipt.json']if x.is_file()];receipt=None
        try:
            require(len(paths)==1,'Exactly one normal run receipt required')
            receipt=read_json(paths[0]);record['run_receipt']={'path':str(paths[0]),'sha256':sha(paths[0])};record['normal_receipt_status']=receipt.get('status') if type(receipt)is dict else None;record['source_result_status']=receipt.get('source_result',{}).get('status') if type(receipt)is dict and type(receipt.get('source_result'))is dict else None
            require(type(receipt)is dict and receipt.get('source_repository_root')==str(repo) and receipt.get('data_repository_root')==str(data_repo),'Receipt repository roots mismatch')
            require(same_descriptor(receipt.get('data_bundle'),job['data_bundle']),'Receipt bundle identity mismatch')
            if 'build_receipt' in job:require(same_descriptor(receipt.get('build_receipt'),job['build_receipt']),'Receipt build identity mismatch')
            record['receipt_backend_fields']={k:v for k,v in receipt.items()if 'backend'in k}
            actual_build=receipt.get('build_receipt');require(type(actual_build)is dict and type(actual_build.get('path'))is str and type(actual_build.get('sha256'))is str,'Normal receipt must identify checked build')
            build_path=Path(actual_build['path']).resolve(strict=True);require(sha(build_path)==actual_build['sha256'],'Reported build receipt digest mismatch')
            build=read_json(build_path);require(type(build)is dict,'Reported build object required');checked_build_identity(build,job,repo,read_json(job['data_bundle']['path']))
            record['reported_build_receipt']=actual_build;record['build_backend_fields']={k:v for k,v in build.items()if 'backend'in k}
        except (ValueError,TypeError,KeyError,OSError,json.JSONDecodeError) as error:record['receipt_error']=str(error);receipt=None
        record.update(classify(job,process,receipt))
        try:verify(pins)
        except (ValueError,OSError)as error:record.update(execution_complete=False,mission_victory=None,validation_passed=False,status='InputDrift',reason=str(error))
        write(container/'job-receipt.json',record);summary['jobs'].append(record);write(output/'batch-receipt.json',summary)
    summary['all_jobs_execution_complete']=all(x['execution_complete']for x in summary['jobs']);summary['all_missions_victorious']=all(x['mission_victory']is True for x in summary['jobs']);summary['status']='Completed'if summary['all_jobs_execution_complete']else'Incomplete';summary['counts']={name:sum(x['status']==name for x in summary['jobs'])for name in sorted({x['status']for x in summary['jobs']})};write(output/'batch-receipt.json',summary);return summary

def main():
    p=argparse.ArgumentParser(description=__doc__)
    for name in ['jobs','output','repo','package-root','data-repo']:p.add_argument('--'+name,required=True)
    a=p.parse_args()
    try:r=execute(a.jobs,a.output,a.repo,a.package_root,a.data_repo);print(json.dumps({k:r[k]for k in ['status','counts','all_jobs_execution_complete','all_missions_victorious']}));return 0 if r['all_jobs_execution_complete']else 1
    except (ValueError,OSError,json.JSONDecodeError)as error:print('Batch input rejected: '+str(error),file=sys.stderr);return 2
if __name__=='__main__':raise SystemExit(main())
