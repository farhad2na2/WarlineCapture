"""Shared modest synthetic batch fixture. Uses tracked complete Baseline720 authority."""
import copy,csv,hashlib,io,json,shutil,tempfile
from pathlib import Path
import sys
HERE=Path(__file__).resolve().parent
ROOT=HERE
while not (ROOT/'Tools/CI/skirmish_production_queue.py').is_file():
    if ROOT.parent==ROOT:raise RuntimeError("Tracked repository root not found")
    ROOT=ROOT.parent
sys.path[:0]=[str(HERE),str(ROOT/'Tools/CI')]
import skirmish_production_operational_inspector as inspector
import skirmish_production_queue as q
from skirmish_production_matrix_authority import SamplingAuthority,build_baseline
from skirmish_native_identity import sha256,content_identity
from s005_windows_source_snapshot import candidate_hash

def write(path,value):
    path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def lines(path,value):
    path.parent.mkdir(parents=True,exist_ok=True);path.write_text(''.join(json.dumps(v)+'\n' for v in value),encoding='utf-8')
def owned_workspace(prefix):
    logs=ROOT/'Logs'
    if logs.is_symlink() or logs.is_junction():
        raise RuntimeError("Refuse reparse fixture parent")
    logs.mkdir(exist_ok=True)
    if logs.resolve().parent!=ROOT.resolve():
        raise RuntimeError("Refuse fixture parent outside workspace")
    return tempfile.TemporaryDirectory(prefix=prefix,dir=logs)
def cleanup_workspace(temporary):
    target=Path(temporary.name)
    logs=(ROOT/'Logs').resolve()
    if target.resolve().parent!=logs or target.is_symlink() or target.is_junction():
        raise RuntimeError("Refuse cleanup outside exact owned Logs child")
    for item in target.rglob('*'):
        if item.is_symlink() or item.is_junction() or not item.resolve().is_relative_to(target.resolve()):
            raise RuntimeError("Refuse reparse/escaped fixture cleanup")
    temporary.cleanup()

class OperationalFixture:
    def __init__(self,root,plan):
        self.root=Path(root);self.root.mkdir()
        self.plan=copy.deepcopy(plan)
        for pin in self.plan['source_pins']:
            dest=self.root/pin['path'];dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/pin['path'],dest)
        self.e=self.root/'Logs/SkirmishProduction/E';self.e.mkdir(parents=True)
        self.planpath=self.e/'plan.json';write(self.planpath,self.plan)
        authority=SamplingAuthority(self.root,self.planpath,q.digest(self.planpath))
        src=self.root/'Assets/A.cs';src.parent.mkdir();src.write_text('// synthetic source fixture')
        sm=[dict(path='Assets/A.cs',sha256=q.digest(src))];bm=[dict(path='Library/ScriptAssemblies/F.dll',sha256='b'*64)]
        self.store=dict(source_manifest=sm,candidate_sha256=candidate_hash(sm),build_manifest=bm,build_sha256=q.fingerprint(bm))
        self.storepath=self.e/'manifest.json';write(self.storepath,self.store)
        self.refs=dict(manifest_path=str(self.storepath),manifest_sha256=q.digest(self.storepath))
        self.baseline=dict(worlds=[dict(sequence='7',native_sessions=0,owned_actors=0,setup_records=0,launch_definition_records=0,logical_provider_records=0)],
            scenes=[dict(preview=False,handle='123',path='Assets/Scene.unity',name='Scene',roots=[dict(native_entity_id='42',name='Root',active_self=True)])],
            input_devices=[],preview_census_complete=True,actual_preview_count=0,measured_preview_count=0,companion_package_owner_fields=[])
        self.batch=[]
        self.event('BatchCaptured',baseline_worlds=['7'],baseline_scenes='fixture-scenes',baseline_preview_count=0,baseline_ownership_census=copy.deepcopy(self.baseline))
        self.jobs=[];self.attempts=[];self.csvrows=[];self.records=[];self.contacts=[];self.traces=[];self.paths=[]
        slots=[s for s in self.plan['slots'] if s['catalog_id']=='S026'][:3]
        for i,(slot,outcome) in enumerate(zip(slots,['Victory','Defeat','Draw'])):
            identity=dict(schema_version=1,identity_kind='SkirmishNativeSetupIdentity',catalog_id='S026',definition_id='skirmish.s026',
                definition_version=1,size='Standard',difficulty='Regular',seed=slot['seed'],locale=slot['locale'],
                native_setup_hash_uint32=123,native_content_identity='synthetic-content')
            packet=SamplingAuthority.packet(authority.catalog['S026'])
            job=dict(schema_version=1,job_id='synthetic-'+str(i),catalog_id='S026',definition_id='skirmish.s026',definition_version=1,
                candidate_sha256=self.store['candidate_sha256'],build_sha256=self.store['build_sha256'],
                setup_sha256=sha256(identity),content_sha256=sha256(content_identity(identity)),source_manifest=sm,
                packet_path=packet,packet_sha256=authority.pins[packet],seed=slot['seed'],locale=slot['locale'],size='Standard',difficulty='Regular',
                control_mode='aria',required_stages=sorted(q.STAGES),timeout_seconds=1000)
            d=Path(q.enqueue(self.root/'Logs/Q',job,self.root,authority)['job_directory']);claim=q.claim(d,'synthetic',self.root,authority);folder=Path(claim['artifact_directory'])
            self.jobs.append(dict(job_path=str(d/'job.json'),claim_path=str(d/'claim.json')))
            h=q.fingerprint(job);self.event('Dispatch',job_sha256=h,attempt_id=claim['attempt_id'],**self.refs)
            for endpoint in ['Begin','End']:self.event('NativeFingerprint',job_sha256=h,endpoint=endpoint,candidate_sha256=self.store['candidate_sha256'],
                build_sha256=self.store['build_sha256'],source_hash_milliseconds=1,dll_hash_milliseconds=1,**self.refs)
            self.event('CleanupOwnershipCensus',job_sha256=h,census=copy.deepcopy(self.baseline))
            self.event('CleanupMeasured',job_sha256=h,worlds=['7'],scenes='fixture-scenes',preview_count=0,queue_success_enabled=False)
            live=self.e/('live'+str(i)+'.jsonl');final=self.root/'Design/AgentReports'/('run'+str(i)+'.jsonl')
            runid='S026-aria-matrix-'+claim['attempt_id'];config=hashlib.sha256(('config'+str(i)).encode()).hexdigest()
            snap=dict(native_world_sequence=str(i),native_session_id='session-'+str(i),native_setup_identity=identity,native_config_sha256=config,
                actual_native_locale='en' if slot['locale']=='EN' else 'fa-IR',native_trace_path=str(live),
                actual_save_root=str(folder/'save-profile'),phase='Playing',outcome='None',simulation_active=True)
            terminal=copy.deepcopy(snap);terminal.update(phase='Finished',outcome=outcome,result_outcome=outcome,result_session_id=snap['native_session_id'],
                result_frozen=1,normal_speed_latch=True,simulation_active=False)
            proof=dict(attempt=snap['native_session_id'],frame=20,rendered=True,copyMatches=True,locale=snap['actual_native_locale'],
                expectedTitle=outcome,actualTitle=outcome,nativeOutcome=outcome,presented=True,pushHistory=True)
            ret=dict(native_terminal_session_id=snap['native_session_id'],native_terminal_outcome=outcome,owner_probe_passed=True,
                terminal_action='MAIN MENU',actual_shell_route='MainMenu',actual_shell_transition_running=False,native_match_session_present=False,
                native_terminal_session_count=0,native_outcome_matches_rendered_title=True,rendered_result_presentation=copy.deepcopy(proof),
                localized_main_menu_presentation=copy.deepcopy(proof),driver_device=30,generation=1,driver_generation=i+1,
                completed_gestures=1,unexpected_samples=0,physical_interventions=0)
            events=[('ContextCaptured',dict(source_sha256=self.store['candidate_sha256'],compiled_dll_manifest_sha256=self.store['build_sha256'],scene_count=1)),
                ('NormalMenuLaunchObserved',snap),('NativeResolvedSnapshot',copy.deepcopy(snap)),('TerminalSnapshot',terminal),
                ('TerminalReturnOwnerCompleted',ret),('RestoreStarting',{}),('ContextRestored',dict(native_qualification='pending_adapter_canary',
                    actual_save_root=None,actual_enter_play_options_enabled=False,actual_enter_play_options=0,actual_scene_count=1))]
            native=[dict(schema_version=1,sequence=n+1,job_sha256=h,attempt_id=claim['attempt_id'],worker=claim['worker'],frame=n+1,
                utc='2026-10-17T00:00:00Z',kind=k,facts=copy.deepcopy(v)) for n,(k,v) in enumerate(events)]
            contacts=[]
            for phase,device in [('Menu',10),('Runtime',20),('Terminal',30)]:
                for kind,completed in [('Dispatched',0),('Began',0),('Ended',1)]:
                    contacts.append(dict(kind=kind,phase=phase,device=device,generation=i+1,dispatched=1,completed=completed,
                        frame=10,unexpected=0,interventions=0,commandAuditActive=phase=='Runtime',acceptedCommands=0,receiptViolations=0))
            trace=[dict(phase='Playing',simulationActive=1,elapsed=1),dict(phase='Finished',simulationActive=0,outcome=outcome,elapsed=100)]
            run=dict(zip(inspector.HEADER,['']*len(inspector.HEADER)))
            run.update(run_id=runid,catalog_id='S026',definition_version='1',code_hash=job['candidate_sha256'],config_hash=config,
                size='Standard',difficulty='Regular',seed=str(job['seed']),locale=snap['actual_native_locale'],device='Editor',executor='aria',
                normal_speed='1',started_at='2026-10-17T00:00:00Z',result=outcome,end_reason='Objective',duration_seconds='100',
                input_violations='0',human_interventions='0',trace_path=final.relative_to(self.root).as_posix(),
                log_path=(final.parent/(final.stem+'-log.txt')).relative_to(self.root).as_posix())
            self.records.append(native);self.contacts.append(contacts);self.traces.append(trace);self.csvrows.append(run)
            self.paths.append(dict(native=folder/'adapter-native-events.jsonl',live=live,final=final,
                contacts=Path(str(final)+'.touch.jsonl'),log=self.root/run['log_path'],job=d/'job.json',claim=d/'claim.json',immutable=folder/'claim.json'))
        self.event('RuntimeQuittingGate',passed=True,errors=0,completed_jobs=3)
        self.event('BatchCompleted',completed_jobs=3,queue_success_enabled=False);self.event('NativeExitInvocation',requested_code=0,invocations=1)
        self.descriptorpath=self.e/'batch.json'
        self.descriptor=dict(schema_version=2,batch_id='E',artifact_directory=str(self.e),candidate_sha256=self.store['candidate_sha256'],
            build_sha256=self.store['build_sha256'],candidate_manifest_path=str(self.storepath),candidate_manifest_sha256=q.digest(self.storepath),jobs=self.jobs,
            sampling_plan_path=str(self.planpath),sampling_plan_sha256=q.digest(self.planpath))
        self.log='\n'.join('[SkirmishCandidateFingerprint] phase=Completed owner='+o+' sha256='+self.store['candidate_sha256'] for o in ['LaunchCatalog','Finish']*3)
        self.log+='\n[SkirmishProductionBatch] result=Completed jobs=3 queueSuccessEnabled=0\n[SkirmishProductionRuntimeGate] result=Passed errors=0 jobs=3 actualWrapperExitStillRequired=1\n[UnityInvoke] ExitCode: 0\n'
        self.outer=dict(schema_version=1,checked_wrapper_exit=0,execute_method=inspector.METHOD,queue_success_enabled=False)
        self.sources={phase:dict(phase=phase,complete_manifest_equality=True,observed_candidate_sha256=self.store['candidate_sha256'],
            observed_build_sha256=self.store['build_sha256'],**self.refs) for phase in ['before','after']}
    def event(self,kind,**facts):self.batch.append(dict(schema_version=1,kind=kind,facts=facts,utc='2026-10-17T00:00:00Z',elapsed_seconds=1))
    def ref(self,path):return dict(path=str(path),sha256=q.digest(path))
    def materialize(self):
        write(self.descriptorpath,self.descriptor);self.outer['descriptor_sha256']=q.digest(self.descriptorpath)
        logpath=self.e/'unity.log';logpath.write_text(self.log);write(self.e/'outer.json',self.outer)
        lines(self.e/'events.jsonl',self.batch)
        for phase,s in self.sources.items():write(self.e/(phase+'.json'),s)
        csvpath=self.e/'runs.csv';buf=io.StringIO();writer=csv.DictWriter(buf,fieldnames=inspector.HEADER);writer.writeheader();writer.writerows(self.csvrows);csvpath.write_text(buf.getvalue())
        attempts=[]
        for i,p in enumerate(self.paths):
            lines(p['native'],self.records[i]);lines(p['live'],self.traces[i]);lines(Path(str(p['live'])+'.touch.jsonl'),self.contacts[i])
            lines(p['final'],self.traces[i]);lines(p['contacts'],self.contacts[i])
            run=self.csvrows[i];buf=io.StringIO();csv.DictWriter(buf,fieldnames=inspector.HEADER).writerow(run)
            p['log'].write_text('[SkirmishS002AriaRun] appended=1 countedWin='+('1' if run['result']=='Victory' else '0')+' error=\n'+buf.getvalue())
            attempts.append(dict(job=self.ref(p['job']),claim=self.ref(p['claim']),immutable_claim=self.ref(p['immutable']),
                native_events=self.ref(p['native']),contacts=self.ref(p['contacts']),trace=self.ref(p['final']),
                run_csv=self.ref(csvpath),run_id=run['run_id'],run_log=self.ref(p['log'])))
        return dict(schema_version=1,descriptor=self.ref(self.descriptorpath),batch_events=self.ref(self.e/'events.jsonl'),wrapper_log=self.ref(logpath),
            outer_wrapper=self.ref(self.e/'outer.json'),source_before=self.ref(self.e/'before.json'),source_after=self.ref(self.e/'after.json'),attempts=attempts)
