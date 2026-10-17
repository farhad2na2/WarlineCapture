"""Whole-run native receipt consistency only; synthetic rows never qualify a run."""
import copy,importlib.util,json,sys,tempfile,unittest
from pathlib import Path
D=Path(__file__).resolve().parent;ROOT=D.parents[3];BASE=D.parent/'ProductionBaselineBatch'
sys.path[:0]=[str(BASE),str(BASE/'Sources/Tools/CI'),str(ROOT/'Tools/CI')]
from test_matrix import baseline
from skirmish_production_matrix_authority import SamplingAuthority,sha
import skirmish_production_queue as queue
spec=importlib.util.spec_from_file_location('runtime_gate_launcher',D/'invoke_skirmish_production_batch.py');launcher=importlib.util.module_from_spec(spec);spec.loader.exec_module(launcher)

class RuntimeGateControls(unittest.TestCase):
    def test_actual_new_launcher_requires_entire_runtime_gate_even_with_wrapper_zero(self):
        scratch=ROOT/'Temp/ProductionBaselineBatchRuntimeGate';scratch.mkdir(parents=True,exist_ok=True)
        with tempfile.TemporaryDirectory(dir=scratch) as temp:
            d=Path(temp);plan,_=baseline();p=d/'plan.json';p.write_text(json.dumps(plan));a=SamplingAuthority(ROOT,p,sha(p));jobs=[];events=[]
            descriptor=dict(schema_version=2,sampling_plan_path=str(p),sampling_plan_sha256=sha(p),jobs=jobs,candidate_sha256='a'*64,build_sha256='b'*64,candidate_manifest_path='synthetic-store.json',candidate_manifest_sha256='c'*64)
            refs=dict(manifest_path=descriptor['candidate_manifest_path'],manifest_sha256=descriptor['candidate_manifest_sha256'])
            def event(kind,**facts):return dict(kind=kind,facts=facts)
            for i,slot in enumerate(list(a.slots.values())[:3]):
                packet=a.packet(a.catalog[slot['catalog_id']]);job=dict(schema_version=1,job_id='synthetic-'+str(i),definition_id='skirmish.'+slot['catalog_id'].lower(),definition_version=1,candidate_sha256='a'*64,build_sha256='b'*64,setup_sha256='c'*64,content_sha256='d'*64,source_manifest=[dict(path=packet,sha256=sha(ROOT/packet))],packet_path=packet,packet_sha256=sha(ROOT/packet),required_stages=sorted(queue.STAGES),timeout_seconds=1800,**slot)
                path=d/('job'+str(i)+'.json');path.write_text(json.dumps(job));jobs.append(dict(job_path=str(path)));key=queue.fingerprint(job)
                events.append(event('Dispatch',job_sha256=key,**refs))
                for endpoint in ('Begin','End'):events.append(event('NativeFingerprint',job_sha256=key,endpoint=endpoint,candidate_sha256='a'*64,build_sha256='b'*64,source_hash_milliseconds=1,dll_hash_milliseconds=1,**refs))
                events.append(event('CleanupOwnershipCensus',job_sha256=key,census=dict(preview_census_complete=True,measured_preview_count=2,actual_preview_count=2,worlds=[dict(native_sessions=0,owned_actors=0,setup_records=0,launch_definition_records=0,logical_provider_records=0)])))
            events += [event('BatchCompleted',completed_jobs=3,queue_success_enabled=False),event('NativeExitInvocation',invocations=1,requested_code=0),event('RuntimeQuittingGate',errors=0,completed_jobs=3,passed=True)]
            marker='[SkirmishProductionRuntimeGate] result=Passed errors=0 jobs=3 actualWrapperExitStillRequired=1'
            raw='\n'.join('[SkirmishCandidateFingerprint] phase=Completed owner='+owner+' sha256='+'a'*64 for owner in ('LaunchCatalog','Finish')*3)
            log=raw+'\n[SkirmishProductionBatch] result=Completed jobs=3 queueSuccessEnabled=0\n'+marker+'\n[UnityInvoke] ExitCode: 0\n'
            self.assertEqual(3,launcher.verify_native_events(descriptor,events,log,ROOT)['native_jobs'])
            negatives=[(events,log.replace(marker,'')),(events,log+'\n'+marker),(events,log+'\n[SkirmishProductionRuntimeGate] result=Failed errors=1 reason=ErrorAfterQuitGate'),(events[:-1],log),(events+[events[-1]],log),(events,log.replace('ExitCode: 0','ExitCode: 37'))]
            for key,value in [('errors',1),('errors',False),('passed',False),('completed_jobs',2)]:
                changed=copy.deepcopy(events);changed[-1]['facts'][key]=value;negatives.append((changed,log))
            for rows,text in negatives:
                with self.assertRaises(ValueError):launcher.verify_native_events(descriptor,rows,text,ROOT)
            compile((D/'invoke_skirmish_production_batch.py').read_text(),str(D/'invoke_skirmish_production_batch.py'),'exec')
            script=(D/'invoke_skirmish_production_batch.py').read_text()
            self.assertIn("Required whole-run runtime gate absent or failed",script)
            self.assertIn("if($global:LASTEXITCODE -ne 0){exit $global:LASTEXITCODE}",script)

if __name__=='__main__':unittest.main(verbosity=2)
