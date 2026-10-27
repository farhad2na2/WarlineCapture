"""Controlled subprocess protocol fixtures ONLY; no mission validation or World."""
import importlib.util,json,tempfile,hashlib,copy,subprocess,signal,inspect
from pathlib import Path
from unittest.mock import patch
ROOT=Path(__file__).parent
spec=importlib.util.spec_from_file_location('batch',ROOT/'run_skirmish_source_batch.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
checks=[]
def ok(name,condition):
    assert condition,name
    checks.append(name)
def pin(p):return {'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
def dump(p,j):p.write_text(json.dumps(j));return p
def flag(a,name):return a[a.index(name)+1]
def base_receipt(job,outcome='Victory'):
    won=outcome=='Victory'
    return {'schema_version':1,'kind':'SkirmishSourceSimulationRunReceipt','mission':job['mission'],'seed':job['seed'],'process_exit':0,'timed_out':False,'status':'SourceOutcomeObserved','source_result':{'status':'SourceTerminal','source_outcome':outcome},'full_outcome_observed':True,'diagnostic_prefix_completed':False,'validation_passed':won,'controlled_test_fixture':True}
with tempfile.TemporaryDirectory(prefix='controlled-batch-test-')as temp:
    root=Path(temp).resolve();repo=root/'explicit-repo';repo.mkdir();cli=repo/'Tools/CI/run_skirmish_source_simulation.py';cli.parent.mkdir(parents=True);cli.write_text('# CONTROLLED TEST FIXTURE - never launched\n');data=root/'explicit-data';data.mkdir();jobs=[]
    controlled_image=root/'controlled-image.exe';controlled_image.write_bytes(b'CONTROLLED PROTOCOL IMAGE BYTES - NEVER EXECUTED\n')
    def controlled_build(mission,seed,backend):
        return {'schema_version':1,'kind':'SkirmishSourceSimulationBuild','status':'Passed','compile_exit':0,'mission':mission,'seed':seed,'repository_root':str(repo),'runtime_backend':backend,'runtime_configuration_mode':'LegacyBoundProfilesV1','source_runtime_backend':'CONTROLLED_TEST_BACKEND','controlled_protocol_fixture':True,'source_and_reference_pins':{str(cli):pin(cli)['sha256'],str(controlled_image):pin(controlled_image)['sha256']},'executable':pin(controlled_image)}
    for name,mission,seed in [('victory','S002',104731),('defeat','S002',1),('prefix','S026',104755),('unknown','S999',999)]:
        bundle=dump(root/(name+'-bundle.json'),{'mission':mission,'seed':seed,'runtime_backend':'TypedGroundS026V1'if mission=='S026'else'LegacyGroundS002V1','controlled_protocol_fixture':True});job={'id':name,'mission':mission,'seed':seed,'data_bundle':pin(bundle),'timeout':3}
        if name=='prefix':job['max_ticks']=5
        jobs.append(job)
    build=dump(root/'build.json',controlled_build('S002',104731,'LegacyGroundS002V1'));jobs[0]['build_receipt']=pin(build)
    manifest=dump(root/'jobs.json',{'schema_version':1,'jobs':jobs});calls=[];active=0
    def seam(argv,log,timeout):
        global active
        assert active==0;active+=1
        name=Path(flag(argv,'--output')).parent.name;job=next(j for j in jobs if j['id']==name);calls.append(name)
        assert flag(argv,'--mission')==job['mission'] and int(flag(argv,'--seed'))==job['seed'] and flag(argv,'--repo')==str(repo) and flag(argv,'--data-repo')==str(data)
        assert flag(argv,'--data-bundle')==job['data_bundle']['path'] and timeout==job['timeout'];log.write_text('CONTROLLED SUBPROCESS SEAM ONLY\n')
        if name=='unknown':active-=1;return {'cli_exit':2,'outer_timeout':False}
        dest=Path(flag(argv,'--output'));dest=dest if 'build_receipt'in job else dest/'run';dest.mkdir(parents=True)
        r=base_receipt(job,'Defeat'if name=='defeat'else'Victory');r.update(source_repository_root=str(repo),data_repository_root=str(data),data_bundle=dict(job['data_bundle'],bytes=Path(job['data_bundle']['path']).stat().st_size),source_runtime_backend='CONTROLLED_TEST_BACKEND')
        if 'build_receipt'in job:r['build_receipt']=dict(job['build_receipt'],bytes=build.stat().st_size)
        else:
            generated=dump(dest/'controlled-build.json',controlled_build(job['mission'],job['seed'],json.loads(Path(job['data_bundle']['path']).read_text()).get('runtime_backend','LegacyGroundS002V1')));r['build_receipt']=pin(generated)
        if name=='prefix':r.update(status='BoundedPrefix',source_result={'status':'BoundedPrefix','source_outcome':'None'},full_outcome_observed=False,diagnostic_prefix_completed=True,validation_passed=False)
        dump(dest/'run-receipt.json',r);active-=1;return {'cli_exit':1 if name=='defeat'else 0,'outer_timeout':False}
    result=m.execute(manifest,root/'batch',repo,repo,data,seam)
    ok('one-at-a-time identity/root/descriptor/optionalflag propagation',calls==['victory','defeat','prefix','unknown']and active==0)
    ok('mixed aggregate incomplete without invented unsupported success',result['status']=='Incomplete'and not result['all_jobs_execution_complete'])
    a,b,c,d=result['jobs'];ok('actual receipt status/backend recorded',a['status']=='SourceOutcomeObserved'and a['receipt_backend_fields']=={'source_runtime_backend':'CONTROLLED_TEST_BACKEND'}and a['build_backend_fields']['runtime_backend']=='LegacyGroundS002V1'and a['build_backend_fields']['source_runtime_backend']=='CONTROLLED_TEST_BACKEND')
    ok('victory complete/validation distinct',a['execution_complete']and a['mission_victory']and a['validation_passed'])
    ok('defeat retains CLI1 but executioncomplete notVictory',b['execution_complete']and b['process']['cli_exit']==1 and b['mission_victory']is False and b['validation_passed']is False)
    ok('prefix remains diagnostic/nonVictory',c['execution_complete']and c['status']=='BoundedPrefix'and not c['mission_victory'])
    ok('admission rejection/missing receipt never passes',d['status']=='AdmissionRejected'and not d['execution_complete'])
    def rejected(name,func):
        try:func()
        except(ValueError,OSError):checks.append(name);return
        raise AssertionError(name)
    rejected('existing output no overwrite/no launch',lambda:m.execute(manifest,root/'batch',repo,repo,data,lambda*a:(_ for _ in ()).throw(AssertionError('launched'))))
    for name,change in [('duplicate IDs',lambda x:x['jobs'].append(dict(x['jobs'][0]))),('casefold path collision',lambda x:x['jobs'].append(dict(x['jobs'][0],id='VICTORY'))),('path traversal ID',lambda x:x['jobs'][0].update(id='../escape')),('unknown job fields',lambda x:x['jobs'][0].update(hidden_entry=True)),('bool seed',lambda x:x['jobs'][0].update(seed=True)),('bool maxTicks',lambda x:x['jobs'][2].update(max_ticks=True)),('infinite timeout',lambda x:x['jobs'][0].update(timeout=float('inf'))),('digest drift',lambda x:x['jobs'][0]['data_bundle'].update(sha256='0'*64)),('bundle identity mismatch',lambda x:x['jobs'][0].update(mission='S026'))]:
        value={'schema_version':1,'jobs':copy.deepcopy(jobs)};change(value);f=dump(root/(name.replace(' ','-')+'.json'),value);rejected(name,lambda:m.load_jobs(f,repo))
    j=jobs[0];victory=base_receipt(j);good={'cli_exit':0,'outer_timeout':False}
    for name,proc,receipt in [('nonzero CLI Victory',{'cli_exit':1,'outer_timeout':False},victory),('outer timeout',{'cli_exit':0,'outer_timeout':True},victory),('owned descendants remain',{'cli_exit':0,'outer_timeout':False,'owned_descendants_survived_cli':True},victory),('missing receipt',good,None),('malformed receipt',good,[]),('contradictory Victory validation',good,dict(victory,validation_passed=False)),('worker failure',good,dict(victory,process_exit=1)),('worker timeout',good,dict(victory,timed_out=True)),('wrong identity',good,dict(victory,seed=1)),('unsupported status',good,dict(victory,status='Unsupported')),('bool schema',good,dict(victory,schema_version=True)),('false Defeat CLI0',good,base_receipt(j,'Defeat'))]:ok(name+' never complete',not m.classify(j,proc,receipt)['execution_complete'])
    # Production owned timeout branch with fake Popen only; no OS process or signals sent.
    class Child:
        pid=987654321;returncode=-15
        def __init__(self):self.calls=0
        def wait(self,timeout):
            self.calls+=1
            if self.calls==1:raise subprocess.TimeoutExpired(['CONTROLLED'],timeout)
            return self.returncode
    child=Child()
    with patch.object(m.subprocess,'Popen',return_value=child)as popen,patch.object(m.os,'killpg')as kill:
        process=m.launch(['CONTROLLED-TEST-ONLY'],root/'timeout.log',2)
        ok('owned processgroup timeout/reap production branch',process['outer_timeout']and kill.call_args_list[0].args==(child.pid,signal.SIGTERM)and kill.call_args_list[-1].args==(child.pid,signal.SIGKILL)and popen.call_args.kwargs['start_new_session']is True and child.calls==2)
    clean=Child();clean.wait=lambda timeout:0
    with patch.object(m.subprocess,'Popen',return_value=clean),patch.object(m.os,'killpg')as kill:
        process=m.launch(['CONTROLLED-TEST-ONLY'],root/'orphan.log',2)
        ok('unexpected owned descendants cleaned and never pass',process['owned_descendants_survived_cli']and kill.call_args_list[-1].args==(clean.pid,signal.SIGKILL))
    # Full120 catalog /360 size-row manifests: protocol validation only, never dispatch.
    sized=[]
    for catalog in range(1,121):
        for size in (1,2,3):
            mission='S'+str(catalog).zfill(3);bundle=dump(root/(mission+'-size'+str(size)+'.json'),{'mission':mission,'seed':104731,'size_id':size,'controlled_protocol_fixture':True})
            sized.append({'id':mission+'-size'+str(size),'mission':mission,'seed':104731,'data_bundle':pin(bundle),'timeout':600})
    for count,selected in [(120,sized[::3]),(360,sized)]:
        f=dump(root/('catalog'+str(count)+'.json'),{'schema_version':1,'jobs':selected});loaded,_=m.load_jobs(f,repo);ok('valid'+str(count)+'row manifest without admission/dispatch',len(loaded)==count)
    too_many=dump(root/'361.json',{'schema_version':1,'jobs':sized+[dict(sized[0],id='row361')]});output=root/'reject361';rejected('361 rejects before output or spawn',lambda:m.execute(too_many,output,repo,repo,data,lambda*a:(_ for _ in ()).throw(AssertionError('spawned'))));ok('oversize output remains absent',not output.exists())
    def strict_reject(name,text):
        f=root/(name+'.json');f.write_text(text)
        try:m.read_json(f)
        except ValueError as error:ok(name+' strict decode',('Duplicate JSON key'in str(error))or('Nonfinite JSON'in str(error)));return
        raise AssertionError(name)
    for name,text in [('duplicate-root','{"a":1,"a":2}'),('duplicate-nested','{"x":{"a":1,"a":1}}'),('nan','{"x":NaN}'),('infinity','{"x":Infinity}'),('minus-infinity','{"x":-Infinity}'),('finite-overflow','{"x":1e999}')]:strict_reject(name,text)
    for name,where,text in [('jobs-duplicate','jobs','{"schema_version":1,"schema_version":1,"jobs":[]}'),('jobs-nonfinite','jobs','{"schema_version":1,"jobs":[],"x":NaN}'),('bundle-duplicate','data_bundle','{"mission":"S002","mission":"S002","seed":104731}'),('bundle-nonfinite','data_bundle','{"mission":"S002","seed":104731,"x":NaN}'),('build-duplicate','build_receipt','{"mission":"S002","seed":104731,"status":"Passed","status":"Passed"}'),('build-nonfinite','build_receipt','{"mission":"S002","seed":104731,"x":1e999}')]:
        f=root/(name+'.json');f.write_text(text)
        if where!='jobs':job=copy.deepcopy(jobs[0]);job[where]=pin(f);f=dump(root/(name+'-jobs.json'),{'schema_version':1,'jobs':[job]})
        output=root/(name+'-out');rejected(name+' before spawning',lambda:m.execute(f,output,repo,repo,data,lambda*a:(_ for _ in ()).throw(AssertionError('spawned'))));ok(name+' no output created',not output.exists())
    for name,extra_json in [('receipt-duplicate',',"mission":"S002"'),('receipt-nonfinite',',"duration":NaN'),('receipt-overflow',',"duration":1e999')]:
        def bad_receipt(argv,log,timeout):
            r=seam(argv,log,timeout);file=Path(flag(argv,'--output'))/'run-receipt.json';text=file.read_text();file.write_text(text[:-1]+extra_json+'}');return r
        f=dump(root/(name+'-manifest.json'),{'schema_version':1,'jobs':[jobs[0]]});r=m.execute(f,root/(name+'-output'),repo,repo,data,bad_receipt);ok(name+' raw preserved/uncredited',not r['all_jobs_execution_complete']and bool(r['jobs'][0].get('receipt_error')))
    for name,extra_json in [('reported-build-duplicate',',"status":"Passed"'),('reported-build-nonfinite',',"duration":Infinity')]:
        def bad_build(argv,log,timeout):
            r=seam(argv,log,timeout);file=Path(flag(argv,'--output'))/'run/run-receipt.json';receipt=json.loads(file.read_text());generated=Path(receipt['build_receipt']['path']);text=generated.read_text();generated.write_text(text[:-1]+extra_json+'}');receipt['build_receipt']=pin(generated);dump(file,receipt);return r
        f=dump(root/(name+'-manifest.json'),{'schema_version':1,'jobs':[jobs[1]]});r=m.execute(f,root/(name+'-output'),repo,repo,data,bad_build);ok(name+' uncredited',not r['all_jobs_execution_complete']and bool(r['jobs'][0].get('receipt_error')))
    # Optional qualification is a pinned descriptor; only the normal CLI admits it.
    qfile=dump(root/'controlled-qualification.json',{'kind':'CONTROLLED_PROTOCOL_QUALIFICATION_ONLY','schema_version':1})
    qjobs=copy.deepcopy(jobs[:2]);qjobs[0]['configuration_qualification']=pin(qfile)
    qmanifest=dump(root/'qualified-jobs.json',{'schema_version':1,'jobs':qjobs})
    loaded,qpins=m.load_jobs(qmanifest,repo)
    ok('optional qualification descriptor pinned without admission claim',loaded[0]['configuration_qualification']==pin(qfile)and str(qfile)in qpins)
    omitted=dump(root/'qualification-omitted.json',{'schema_version':1,'jobs':[jobs[0]]});loaded,_=m.load_jobs(omitted,repo)
    ok('omitted qualification and legacy continuation defaults preserved','configuration_qualification'not in loaded[0]and inspect.signature(m.execute).parameters['stop_on_failure'].default is False)
    for name,value in [('missing-sha',{'path':str(qfile)}),('null',None),('unknown-key',dict(pin(qfile),alias=True)),('hash-drift',dict(pin(qfile),sha256='0'*64))]:
        qcopy=copy.deepcopy(qjobs);qcopy[0]['configuration_qualification']=value;f=dump(root/('qualification-'+name+'.json'),{'schema_version':1,'jobs':qcopy});rejected('qualification '+name+' rejected before dispatch',lambda:m.load_jobs(f,repo))
    qcopy=copy.deepcopy(qjobs);qcopy[0]['unsupported_alias']=True;f=dump(root/'qualification-unknown-job.json',{'schema_version':1,'jobs':qcopy});rejected('qualification does not relax unknown job keys',lambda:m.load_jobs(f,repo))
    for stop,want in [(True,1),(False,2)]:
        protocol_calls=[]
        def no_worker(argv,log,timeout):
            protocol_calls.append(argv);return {'cli_exit':2,'outer_timeout':False,'owned_descendants_survived_cli':False,'scope':'CONTROLLED FAILED PROTOCOL CALLBACK; no CLI/worker/World'}
        qresult=m.execute(qmanifest,root/('qualified-stop-'+str(stop)),repo,repo,data,no_worker,stop_on_failure=stop)
        ok('explicit stop '+str(stop)+' preserves normal argv and failed receipt',len(protocol_calls)==want and flag(protocol_calls[0],'--configuration-qualification')==str(qfile)and qresult['jobs'][0]['inputs']['configuration_qualification']==pin(qfile)and not qresult['all_jobs_execution_complete']and all(not x['execution_complete']and x['status']=='AdmissionRejected'for x in qresult['jobs']))
    # Hash drift after launch must defeat an otherwise valid controlled receipt.
    def drift(argv,log,timeout):
        r=seam(argv,log,timeout);cli.write_text('# MUTATED CONTROLLED FIXTURE\n');return r
    one=dump(root/'one.json',{'schema_version':1,'jobs':[jobs[0]]});r=m.execute(one,root/'drift',repo,repo,data,drift);ok('input drift after process cannot pass',r['jobs'][0]['status']=='InputDrift'and not r['all_jobs_execution_complete'])
# Optional explicitly supplied retained REAL receipt; no hardcoded private artifact path.
import argparse
parser=argparse.ArgumentParser();parser.add_argument('--real-receipt');parser.add_argument('--real-receipt-sha256');args=parser.parse_args()
if args.real_receipt or args.real_receipt_sha256:
    assert args.real_receipt and args.real_receipt_sha256
    f=Path(args.real_receipt).resolve(strict=True);before=hashlib.sha256(f.read_bytes()).hexdigest();assert before==args.real_receipt_sha256
    receipt=m.read_json(f);assert receipt['mission']=='S002'and receipt['seed']==104731 and receipt['source_result']['source_outcome']=='Victory'
    result=m.classify({'mission':'S002','seed':104731},{'cli_exit':0,'outer_timeout':False},receipt);ok('retained REAL normal S2Victory receipt classification only',result['execution_complete']and result['mission_victory']and result['validation_passed']);assert hashlib.sha256(f.read_bytes()).hexdigest()==before
summary={'kind':'ControlledSubprocessBatchProtocolTests','status':'Passed','checks':len(checks),'case_names':checks,'world_or_unity_launched':False,'real_mission_validation_claimed':False,'fake_receipts':'Explicit controlled subprocess protocol fixtures only; not actual mission evidence.','retained_real_receipt':{'path':str(f),'sha256':before,'classification_only':True}if args.real_receipt else None};print(json.dumps(summary,sort_keys=True));print('[SequentialBatchProtocolTests] result=Passed checks='+str(len(checks))+' world=False controlledSubprocessOnly=True')
