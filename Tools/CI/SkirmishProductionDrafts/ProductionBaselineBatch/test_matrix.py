"""Offline admission/claim controls; synthetic plans never establish native runs."""
import copy,csv,importlib.util,json,re,sys,tempfile,unittest
from pathlib import Path
D=Path(__file__).resolve().parent;ROOT=D.parents[3]
sys.path[:0]=[str(D),str(D/'Sources/Tools/CI'),str(ROOT/'Tools/CI')]
from skirmish_production_matrix_authority import SamplingAuthority,sha,CATALOG,RULES
import skirmish_production_queue as queue

def baseline():
    with (ROOT/CATALOG).open(encoding='utf-8-sig',newline='') as s: rows=list(csv.DictReader(s))
    paths={CATALOG,RULES}|{SamplingAuthority.packet(r) for r in rows};slots=[]
    for r in rows:
        text=(ROOT/SamplingAuthority.packet(r)).read_text(encoding='utf-8');section=re.search(r'^## '+r['scenario_id']+r'\s*$([\s\S]*?)(?=^## |\Z)',text,re.M)[1]
        seeds=map(int,re.findall(r'`(\d+)`',re.search(r'Regular seeds ((?:`\d+`[ ,and]*)+) in both EN and FA at Standard',section)[1]))
        for seed in seeds:
            for locale in ('EN','FA'):slots.append(dict(catalog_id=r['scenario_id'],seed=seed,locale=locale,size='Standard',difficulty='Regular',control_mode='aria'))
    return dict(schema_version=1,kind='Baseline720',source_pins=[dict(path=p,sha256=sha(ROOT/p))for p in sorted(paths)],slots=slots),rows

class MatrixControls(unittest.TestCase):
    def setUp(self):
        root=ROOT/'Temp/ProductionBaselineBatchControls';root.mkdir(exist_ok=True,parents=True);self.temp=tempfile.TemporaryDirectory(dir=root);self.addCleanup(self.temp.cleanup);self.dir=Path(self.temp.name);self.plan,self.rows=baseline()
    def read(self,plan=None):
        p=self.dir/('plan-'+str(len(list(self.dir.glob('plan-*'))))+'.json');p.write_text(json.dumps(plan or self.plan),encoding='utf-8');return SamplingAuthority(ROOT,p,sha(p))
    def job(self,authority,index=0):
        slot=list(authority.slots.values())[index];packet=authority.packet(authority.catalog[slot['catalog_id']]);return dict(schema_version=1,job_id='synthetic-'+str(index),definition_id='skirmish.'+slot['catalog_id'].lower(),definition_version=1,candidate_sha256='a'*64,build_sha256='b'*64,setup_sha256='c'*64,content_sha256='d'*64,source_manifest=[dict(path=CATALOG,sha256=sha(ROOT/CATALOG))],packet_path=packet,packet_sha256=sha(ROOT/packet),required_stages=sorted(queue.STAGES),timeout_seconds=1800,**slot)
    def test_actual720_packet_slots(self):
        authority=self.read();self.assertEqual(len(authority.slots),720)
        for job in [self.job(authority,0),self.job(authority,719)]:self.assertEqual(queue.preflight(job,ROOT,authority)['native_qualification'],'pending')
    def test_complete_baseline_missing_duplicate_seed_locale_refuse(self):
        for change in ('missing','duplicate','seed','locale'):
            p=copy.deepcopy(self.plan)
            if change=='missing':p['slots'].pop()
            elif change=='duplicate':p['slots'][-1]=p['slots'][0]
            else:p['slots'][0][change]=1 if change=='seed' else 'en'
            with self.subTest(change=change),self.assertRaises(ValueError):self.read(p)
    def test_stress_exact60_by20_and_alternating_starts(self):
        p=copy.deepcopy(self.plan);p['kind']='Stress1200';p['slots']=[];families={}
        for r in self.rows:families.setdefault((r['map_id'],r['objective_id'],r['army_profile']),{})[r['start_profile']]=r['scenario_id']
        for starts in families.values():
            for i in range(20):p['slots'].append(dict(catalog_id=starts['F' if i%2==0 else 'E'],seed=700000+i,locale='EN' if i%2==0 else 'FA',size='Standard',difficulty='Regular',control_mode='aria'))
        a=self.read(p);self.assertEqual(len(a.slots),1200);job=self.job(a);queue.preflight(job,ROOT,a)
        a.validate_batch([self.job(a,i)for i in (0,1,2)])
        for indices in [(0,2),(1,0),(0,0)]:
            with self.assertRaises(ValueError):a.validate_batch([self.job(a,i)for i in indices])
        with self.assertRaises(queue.ContractError):queue.preflight(job,ROOT)
        for field,value in [('catalog_id',p['slots'][0]['catalog_id']),('seed',p['slots'][0]['seed'])]:
            bad=copy.deepcopy(p);bad['slots'][1][field]=value
            with self.assertRaises(ValueError):self.read(bad)
    def test_job_flags_and_packet_join_refuse(self):
        a=self.read();job=self.job(a)
        for key,value in [('size','War'),('difficulty','Commander'),('control_mode','manual'),('seed',42),('packet_path','Design/Roadmap/Skirmish_Expansion/Scenarios/CC_BA.md')]:
            with self.subTest(key=key),self.assertRaises(ValueError):a.validate_job(dict(job,**{key:value}))
    def test_plan_mutation_and_source_pin_refuse(self):
        a=self.read();a.path.write_text('{}')
        with self.assertRaises(ValueError):a.recheck()
        p=copy.deepcopy(self.plan);p['source_pins'][0]['sha256']='0'*64
        with self.assertRaises(ValueError):self.read(p)
    def test_same_canonical_claim_and_no_success_publication(self):
        a=self.read();j=self.job(a);out=self.dir/'queue';q=queue.enqueue(out,j,ROOT,a);claimed=queue.claim(q['job_directory'],'synthetic-worker',ROOT,a)
        with self.assertRaises(queue.ContractError):queue.claim(q['job_directory'],'other',ROOT,a)
        with self.assertRaises(queue.ContractError):queue.finish(q['job_directory'],claimed['token'],'qualified')
        self.assertFalse((Path(claimed['artifact_directory'])/'result.json').exists())
    def test_all_projected_python_parses(self):
        for p in (D/'Sources/Tools/CI').glob('*.py'):compile(p.read_text(encoding='utf-8'),str(p),'exec')
    def test_three_job_matrix_receipts_and_postrun_authority(self):
        from invoke_skirmish_production_batch import verify_native_events
        a=self.read();jobs=[];events=[]
        descriptor=dict(schema_version=2,sampling_plan_path=str(a.path),sampling_plan_sha256=a.expected,jobs=jobs,candidate_sha256='a'*64,build_sha256='b'*64,candidate_manifest_path='synthetic-store.json',candidate_manifest_sha256='c'*64)
        refs=dict(manifest_path=descriptor['candidate_manifest_path'],manifest_sha256=descriptor['candidate_manifest_sha256'])
        def event(kind,**facts):return dict(kind=kind,facts=facts)
        for i in range(3):
            job=self.job(a,i);path=self.dir/('job-'+str(i)+'.json');path.write_text(json.dumps(job));jobs.append(dict(job_path=str(path)));key=queue.fingerprint(job)
            events.append(event('Dispatch',job_sha256=key,**refs))
            for endpoint in ('Begin','End'):events.append(event('NativeFingerprint',job_sha256=key,endpoint=endpoint,candidate_sha256='a'*64,build_sha256='b'*64,source_hash_milliseconds=1,dll_hash_milliseconds=1,**refs))
            events.append(event('CleanupOwnershipCensus',job_sha256=key,census=dict(preview_census_complete=True,measured_preview_count=2,actual_preview_count=2,worlds=[dict(native_sessions=0,owned_actors=0,setup_records=0,launch_definition_records=0,logical_provider_records=0)])))
        events += [event('BatchCompleted',completed_jobs=3,queue_success_enabled=False),event('NativeExitInvocation',invocations=1,requested_code=0)]
        raw='\n'.join('[SkirmishCandidateFingerprint] phase=Completed owner='+owner+' sha256='+'a'*64 for owner in ('LaunchCatalog','Finish')*3)
        log=raw+'\n[SkirmishProductionBatch] result=Completed jobs=3 queueSuccessEnabled=0\n[UnityInvoke] ExitCode: 0\n'
        self.assertEqual(3,verify_native_events(descriptor,events,log,ROOT)['native_jobs'])
        for text in [log.replace('jobs=3','jobs=2'),log.replace('ExitCode: 0','ExitCode: 37'),log.replace(raw.splitlines()[-1],'missing native Finish')]:
            with self.assertRaises(ValueError):verify_native_events(descriptor,events,text,ROOT)
        with self.assertRaises(ValueError):verify_native_events(descriptor,events,log)
        a.path.write_text('{}')
        with self.assertRaises(ValueError):verify_native_events(descriptor,events,log,ROOT)

if __name__=='__main__':unittest.main()
