"""Tiny real-byte controls; no Unity or live-source observation."""
import inspect,json,os,tempfile,unittest,sys
from collections import Counter
from pathlib import Path
from unittest.mock import patch
HERE=Path(__file__).resolve().parent
ROOT=HERE
while not (ROOT/'Tools/CI/skirmish_production_queue.py').is_file():
 if ROOT.parent==ROOT:raise RuntimeError('Actual dependency root missing')
 ROOT=ROOT.parent
sys.path.insert(1,str(ROOT/'Tools/CI'))
import skirmish_production_queue as q
import prepare_skirmish_production_batch as batch
import prepare_skirmish_batch_canary_jobs as matrix
import s005_windows_source_snapshot as actual
class ClosedPreparationTests(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory(prefix="warline-closed-prep-");self.root=Path(self.temp.name).resolve()
  self.assertTrue(self.root.is_relative_to(Path(tempfile.gettempdir()).resolve()) and self.root.name.startswith("warline-closed-prep-"))
  self.addCleanup(self.temp.cleanup) # Exact verified owned Temp root only.
  for d in ["Assets","Packages","ProjectSettings","Library/ScriptAssemblies","Design/Roadmap/Skirmish_Expansion/Scenarios"]:(self.root/d).mkdir(parents=True)
  self.asset=self.root/"Assets/opaque.asset";self.asset.write_bytes(b"AAAA")
  (self.root/"Packages/manifest.json").write_text("{}");(self.root/"ProjectSettings/test.asset").write_text("settings")
  self.dll=self.root/"Library/ScriptAssemblies/Game.Editor.dll";self.dll.write_bytes(b"tiny DLL")
  self.packet="Design/Roadmap/Skirmish_Expansion/Scenarios/DB_BA.md"
  (self.root/self.packet).write_text("## S005\nRegular seeds ~104734~, ~130368~, ~155926~ in both EN and FA at Standard.\n".replace("~",chr(96)))
  source=actual.snapshot(self.root);builds,bhash=actual.build_snapshot(self.root)
  self.expected=dict(source_manifest=source,candidate_sha256=actual.candidate_hash(source),build_manifest=builds,build_sha256=bhash)
  self.snap=self.root/"snapshot.json";self.snap.write_text(json.dumps(self.expected))
  base=dict(schema_version=1,job_id="job-en",catalog_id="S005",definition_id="skirmish.s005",definition_version=1,
   candidate_sha256=self.expected["candidate_sha256"],build_sha256=bhash,setup_sha256="c"*64,content_sha256="d"*64,
   source_manifest=source,packet_path=self.packet,packet_sha256=q.digest(self.root/self.packet),seed=155926,locale="EN",
   size="Standard",difficulty="Regular",control_mode="aria",required_stages=sorted(q.STAGES),timeout_seconds=3000)
  self.jobs=[base,{**base,"job_id":"job-fa","locale":"FA"}]
 def observe(self):
  obs=matrix._MatrixPreparationObservation(self.root,self.expected);self.addCleanup(obs.close);return obs
 def valid(self,obs,job,project=None,authority=None):return q._preflight(job,project or self.root,authority,_observation=obs)
 def test_one_actual_observation_two_claims_descriptor(self):
  count=Counter();packets=Counter();original=actual._file_row;read=Path.read_bytes
  def track(project,path):count[str(path.resolve())]+=1;return original(project,path)
  def readtrack(path):
   if path.resolve()==(self.root/self.packet).resolve():packets["reads"]+=1
   return read(path)
  with patch.object(actual,"_file_row",side_effect=track),patch.object(Path,"read_bytes",readtrack):
   obs=self.observe()
   for job in self.jobs:self.valid(obs,job)
   obs.bind_jobs(self.jobs);dirs=[]
   for job in self.jobs:
    obs.check_job(job,self.root);d=Path(q._enqueue_preflighted(self.root/"queue",job,self.root)["job_directory"])
    q._claim_checked(d,"worker",self.root,lambda j:self.valid(obs,j));dirs.append(d)
   obs.check_order([q.load(d/"job.json")for d in dirs])
   desc=batch._prepare(self.root,dirs,self.snap,"batch-one",_validate=lambda j,p,a:self.valid(obs,j,p,a))
   self.assertTrue(desc.is_file())
   for d in dirs:
    claim=q.load(d/"claim.json");self.assertEqual(claim,q.load(Path(claim["artifact_directory"])/"claim.json"))
  self.assertEqual(len(count),4);self.assertEqual(set(count.values()),{1});self.assertEqual(packets["reads"],1)
  self.assertIs(self.jobs[0]["source_manifest"],self.jobs[1]["source_manifest"])
 def test_same_size_same_mtime_changed_bytes_refuse(self):
  st=self.asset.stat();self.asset.write_bytes(b"BBBB");os.utime(self.asset,ns=(st.st_atime_ns,st.st_mtime_ns))
  with self.assertRaisesRegex(q.ContractError,"complete actual"):self.observe()
  self.assertFalse((self.root/"queue").exists())
 def test_added_source_refuses(self):
  (self.root/"Assets/extra.cs").write_text("extra")
  with self.assertRaisesRegex(q.ContractError,"complete actual"):self.observe()
 def test_deleted_source_refuses(self):
  self.asset.unlink()
  with self.assertRaisesRegex(q.ContractError,"complete actual"):self.observe()
 def test_changed_build_refuses(self):
  self.dll.write_bytes(b"changed DLL")
  with self.assertRaisesRegex(q.ContractError,"complete actual"):self.observe()
 def test_mutated_job_after_binding_refuses(self):
  obs=self.observe();obs.bind_jobs(self.jobs)
  with self.assertRaisesRegex(q.ContractError,"job changed"):self.valid(obs,{**self.jobs[0],"setup_sha256":"f"*64})
 def test_foreign_root_or_manifest_refuses(self):
  obs=self.observe()
  with self.assertRaisesRegex(q.ContractError,"foreign"):obs.check_job(self.jobs[0],self.root/"other")
  with self.assertRaisesRegex(q.ContractError,"complete actual"):self.valid(obs,{**self.jobs[0],"source_manifest":self.jobs[0]["source_manifest"][:-1]})
 def test_changed_packet_refuses(self):
  obs=self.observe();(self.root/self.packet).write_text("changed")
  with self.assertRaisesRegex(q.ContractError,"packet missing or changed"):self.valid(obs,self.jobs[0])
 def test_closed_observation_no_reuse(self):
  obs=self.observe();obs.close()
  with self.assertRaisesRegex(q.ContractError,"closed"):self.valid(obs,self.jobs[0])
 def test_second_transaction_reobserves_bytes(self):
  counts=Counter();original=actual._file_row
  def track(project,path):counts[str(path.resolve())]+=1;return original(project,path)
  with patch.object(actual,"_file_row",side_effect=track):self.observe().close();self.observe().close()
  self.assertEqual(set(counts.values()),{2})
 def test_public_signatures_cannot_accept_skip_observation(self):
  for api in [q.preflight,q.enqueue,q.claim,batch.prepare]:self.assertNotIn("_observation",inspect.signature(api).parameters)
  with self.assertRaises(TypeError):q.preflight(self.jobs[0],self.root,_observation=object())
 def test_public_enqueue_and_claim_still_each_hash(self):
  with patch.object(q,"digest",wraps=q.digest)as d:
   directory=Path(q.enqueue(self.root/"queue",self.jobs[0],self.root)["job_directory"]);self.assertEqual(d.call_count,4)
   q.claim(directory,"worker",self.root);self.assertEqual(d.call_count,8)
 def test_claim_collision_preserves_original(self):
  obs=self.observe();obs.bind_jobs(self.jobs);d=Path(q._enqueue_preflighted(self.root/"queue",self.jobs[0],self.root)["job_directory"])
  first=q._claim_checked(d,"worker",self.root,lambda j:self.valid(obs,j))
  with self.assertRaisesRegex(q.ContractError,"already claimed"):q._claim_checked(d,"other",self.root,lambda j:self.valid(obs,j))
  self.assertEqual(first,q.load(d/"claim.json"));self.assertFalse((self.root/"Logs/SkirmishProduction/Batches").exists())
 def test_queued_job_mutation_refuses_claim(self):
  obs=self.observe();obs.bind_jobs(self.jobs);d=Path(q._enqueue_preflighted(self.root/"queue",self.jobs[0],self.root)["job_directory"])
  (d/"job.json").write_text(json.dumps({**self.jobs[0],"content_sha256":"f"*64}))
  with self.assertRaisesRegex(q.ContractError,"job changed"):q._claim_checked(d,"worker",self.root,lambda j:self.valid(obs,j))
  self.assertFalse((d/"claim.json").exists())
 def test_late_edit_refused_by_unchanged_fresh_prelaunch(self):
  obs=self.observe();obs.bind_jobs(self.jobs);self.asset.write_bytes(b"BBBB")
  self.valid(obs,self.jobs[0]) # Old observation only, never claimed current.
  self.assertNotEqual(actual.snapshot(self.root),self.expected["source_manifest"])
  with self.assertRaisesRegex(q.ContractError,"source missing or changed"):q.preflight(self.jobs[0],self.root)
 def test_order_chunk_limit_and_protected_schema_refuse(self):
  obs=self.observe();obs.bind_jobs(self.jobs)
  with self.assertRaisesRegex(q.ContractError,"set/order"):obs.check_order(list(reversed(self.jobs)))
  other=self.observe()
  with self.assertRaisesRegex(q.ContractError,"bounded"):other.bind_jobs([{**self.jobs[0],"job_id":"job-"+str(i)}for i in range(17)])
  with self.assertRaisesRegex(q.ContractError,"schema1"):other.check_job({**self.jobs[0],"schema_version":3},self.root)
class ManualPaidPlanePreparationTests(unittest.TestCase):
 setUp=ClosedPreparationTests.setUp
 def manual_fixture(self):
  from types import SimpleNamespace
  import copy
  self.proofs=self.root/'Logs/SkirmishProduction/Manual';self.proofs.mkdir(parents=True)
  identity=dict(schema_version=1,identity_kind='SkirmishNativeSetupIdentity',catalog_id='S005',definition_id='skirmish.s005',definition_version=1,difficulty='Regular',size='Standard',seed=155926,locale='EN',native_setup_hash_uint32=123,native_content_identity='opaque-fixture-content')
  subject=dict(catalog_id='S005',seed=155926,locale='EN',size='Standard',difficulty='Regular',control_mode='manual',packet_path=self.packet,packet_sha256=q.digest(self.root/self.packet))
  self.slots=[{**subject,'control_mode':'aria'}]
  envelope=dict(schema_version=1,collector_kind='SkirmishNativeCompiledSetupCensus',slot=self.slots[0],native_setup_identity=identity,actual_native_locale='en',native_config_sha256='f'*64,definition_asset_path='Assets/opaque.asset',definition_asset_sha256=q.digest(self.asset))
  self.census=dict(candidate_sha256=self.expected['candidate_sha256'],build_sha256=self.expected['build_sha256'],native_compiled_setups=[envelope])
  self.args=SimpleNamespace(project=self.root,worker='manual-worker',matrix_request=self.proofs/'request.json',preparation=self.proofs/'census.json',snapshot=self.snap,integration_manifest=self.proofs/'install.json',preparation_log=self.proofs/'native.log',matrix_exit_file=self.proofs/'exit.txt',manual_paid_plane_authority=self.proofs/'manual-authority.json')
  self.install={'files':[dict(target='Assets/opaque.asset',after_sha256=q.digest(self.asset))]}
  for key,data in [('matrix_request',{}),('preparation',self.census),('integration_manifest',self.install)]:getattr(self.args,key).write_text(json.dumps(data))
  self.args.preparation_log.write_text('fixture checked census log');self.args.matrix_exit_file.write_text('0')
  refs={name:dict(path=str(getattr(self.args,key)),sha256=q.digest(getattr(self.args,key)))for name,key in [('native_request','matrix_request'),('native_census','preparation'),('snapshot','snapshot'),('integration_manifest','integration_manifest'),('preparation_log','preparation_log'),('wrapper_exit','matrix_exit_file')]}
  self.auth=dict(schema_version=1,kind='ScopedManualPaidPlaneSupplementAuthority-v1',approved=True,purpose='NormalPaidTransportCruiseCameraOnly',subject=subject,**refs,candidate_sha256=self.expected['candidate_sha256'],build_sha256=self.expected['build_sha256'],queue_success_enabled=False,normal_mission_accepted=False)
  self.seal()
 def seal(self):
  self.args.manual_paid_plane_authority.write_text(json.dumps(self.auth));self.args.manual_paid_plane_authority_sha256=q.digest(self.args.manual_paid_plane_authority)
 def call_manual(self):
  import contextlib,io
  loaded=(self.root,'manual-worker',{},self.census,self.expected,None,self.slots,self.install)
  with patch.object(matrix,'_load_reviewed_matrix_census',return_value=loaded),contextlib.redirect_stdout(io.StringIO()):return matrix.prepare_manual_paid_plane(self.args)
 def test_manual_real_closed_transaction_one_fresh_claim(self):
  self.manual_fixture();result=self.call_manual()
  self.assertEqual('ManualPaidPlaneSupplementPreparedUnqualified',result['result']);self.assertEqual(1,result['jobs']);self.assertNotIn('descriptor',result)
  job=q.load(Path(result['job_path']));claim=q.load(Path(result['claim_path']))
  self.assertEqual('manual',job['control_mode']);self.assertEqual(3000,job['timeout_seconds']);self.assertEqual(q.fingerprint(job),claim['job_sha256'])
  self.assertEqual(claim,q.load(Path(claim['artifact_directory'])/'claim.json'));self.assertFalse(result['normal_mission_accepted']);self.assertFalse(result['queue_success_enabled'])
  self.assertEqual('aria',self.slots[0]['control_mode']);self.assertFalse((Path(claim['artifact_directory'])/'adapter-started.json').exists())
 def test_authority_subject_and_flags_refuse_before_queue(self):
  import copy
  self.manual_fixture();base=copy.deepcopy(self.auth)
  cases=[('approved',False),('purpose','NormalWin'),('schema_version',True),('queue_success_enabled',True),('normal_mission_accepted',True),('subject.control_mode','aria'),('subject.seed',True),('subject.locale','FA'),('subject.catalog_id','S002'),('subject.size','War')]
  for key,value in cases:
   with self.subTest(key=key):
    self.auth=copy.deepcopy(base)
    if '.'in key:a,b=key.split('.');self.auth[a][b]=value
    else:self.auth[key]=value
    self.seal()
    with self.assertRaises(ValueError):self.call_manual()
    self.assertFalse((self.root/'Logs/SkirmishProduction/Queue').exists())
 def test_each_literal_reference_stale_refuses(self):
  import copy
  self.manual_fixture();base=copy.deepcopy(self.auth)
  for key in ['native_request','native_census','snapshot','integration_manifest','preparation_log','wrapper_exit']:
   with self.subTest(key=key):
    self.auth=copy.deepcopy(base);self.auth[key]['sha256']='0'*64;self.seal()
    with self.assertRaises(ValueError):self.call_manual()
    self.assertFalse((self.root/'Logs/SkirmishProduction/Queue').exists())
 def test_current_source_and_build_changes_refuse(self):
  self.manual_fixture()
  for path in [self.asset,self.dll]:
   with self.subTest(path=path):
    before=path.read_bytes();path.write_bytes(b'changed')
    try:
     with self.assertRaises(ValueError):self.call_manual()
     self.assertFalse((self.root/'Logs/SkirmishProduction/Queue').exists())
    finally:path.write_bytes(before)
 def test_actual_native_identity_or_claimmode_cannot_be_spliced(self):
  import copy
  self.manual_fixture();original=copy.deepcopy(self.census);slot=copy.deepcopy(self.slots)
  for case in ['seed','locale','slotMode','duplicate']:
   with self.subTest(case=case):
    self.census=copy.deepcopy(original);self.slots=copy.deepcopy(slot)
    if case=='duplicate':self.census['native_compiled_setups']*=2
    elif case=='slotMode':self.slots[0]['control_mode']='manual'
    else:self.census['native_compiled_setups'][0]['native_setup_identity'][case]=104734 if case=='seed'else'FA'
    with self.assertRaises(ValueError):self.call_manual()
    self.assertFalse((self.root/'Logs/SkirmishProduction/Queue').exists())

if __name__=="__main__":unittest.main(verbosity=2)
