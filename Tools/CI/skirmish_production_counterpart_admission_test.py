"""Live schema3 admission; synthetic graphs and explicit test stress slots earn no mission credit."""
import copy,hashlib,json,shutil,sys,tempfile,unittest
import skirmish_production_queue as queue
from skirmish_production_matrix_authority import SamplingAuthority,build_baseline
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
class Admission(unittest.TestCase):
 CATALOG='S001'
 def setUp(self):
  scratch=ROOT/'Logs/SkirmishProduction/Offline';scratch.mkdir(parents=True,exist_ok=True)
  self.temp=tempfile.TemporaryDirectory(prefix='counterpart-admission-fixture-',dir=scratch);self.root=Path(self.temp.name)
  self.addCleanup(self.temp.cleanup)
  def write(path,value):
   p=self.root/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(value),encoding='utf-8');return {'path':path,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}
  self.write=write
  canonical=build_baseline(ROOT)
  for pin in canonical['source_pins']:
   target=self.root/pin['path'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/pin['path'],target)
  self.plan_path=self.root/'baseline720.json';self.plan_path.write_text(json.dumps(canonical),encoding='utf-8')
  self.matrix=SamplingAuthority(self.root,self.plan_path,queue.digest(self.plan_path))
  from skirmish_counterpart_authority import BINDINGS
  ident=self.CATALOG;index,preset,_=BINDINGS[ident]
  seeds=[r['seed'] for r in canonical['slots'] if r['catalog_id']==ident and r['locale']=='EN']
  prefix='Assets/Game/Configs/SkirmishExpansion/Scenarios/'+ident+'/'
  paths={'development_catalog':prefix+'Resources/SkirmishProtectedCounterpart_'+ident+'.asset','definition':prefix+'SkirmishScenario_'+ident+'.asset',
   'layout':prefix+'Logical/Layout_'+ident+'.asset','binding':prefix+'Logical/Binding_'+ident+'.asset','practice_preset':'Assets/Game/Resources/'+preset+'.asset',
   'main_catalog':'Assets/Game/Resources/SkirmishBattleCatalog.asset','main_expansion_catalog':'Assets/Game/Resources/SkirmishExpansionCatalog.asset'}
  assets={k:write(v,{'synthetic_fixture':k}) for k,v in paths.items()};assets['views']=[write(prefix+'Logical/View_'+ident+'_'+s+'_Regular.asset',{'fixture':s}) for s in ('Standard','War','LargeWar')]
  row={'catalogId':ident,'result':'CollectedUnqualified','measurement':{'strictThreeSizePlacementPass':2,'normalThreeSizeCompilerPassed':True,
   'passes':[{'pass':2,'sizes':[{'size':s,'completeFacilities':True,'allActorsInsidePads':True,'routeQueriesWithSameFacilityReservations':{'queries':[{'status':'NativeQueryFoundExactGoal','failureCode':0}]}} for s in ('Standard','War','LargeWar')]}]}}
  source_files=[write('Assets/Fixture/'+role+'.asset',{'synthetic_source_fixture':role}) for role in ('source','grid','surface')]
  source_pins={}
  for role,pin in zip(('source','grid','surface'),source_files):source_pins[role+'Path']=pin['path'];source_pins[role+'AssetSha256']=pin['sha256']
  row['measurement'].update(sourcePins=source_pins,proposedLayoutSemanticSha256='e'*64)
  artifact=write('Logs/fixture-row.json',row)
  report=write('Logs/fixture-report.json',{'cleanupVerified':True,'result':'CollectedUnqualified','sourcePins':source_pins,'missions':[row],'missionArtifacts':[dict(catalogId=ident,**artifact)]})
  receipt=write('Logs/fixture-receipt.json',{'catalogId':ident,'result':'MaterializedDevelopmentOnly','reviewedReportSha256':report['sha256'],'publicationManifestChanged':False,'developmentOnly':True,'normalMissionReady':False,
   'counterpartDevelopmentResource':dict(assets['development_catalog'],mainCatalogChanged=False,practiceBindingChanged=False),
   'developmentCatalogMaterialization':{'definitionPath':paths['definition'],'definitionSha256':assets['definition']['sha256'],'layoutPath':paths['layout'],'bindingPath':paths['binding'],'runtimeIndex':index,'catalogPlayableChanged':False}})
  self.authority={'schema_version':1,'kind':'MeasuredExpandedCounterpartDevelopmentAuthority','catalog_id':ident,'definition_id':'skirmish.'+ident.lower(),'definition_version':2,'runtime_index':index,
   'candidate_sha256':'a'*64,'build_sha256':'b'*64,'assets':assets,'measurement_report':report,'materialization_receipt':receipt,
   'normal_selection':{'kind':'ExpandedDefinition','driver':'PublicSetupMethods_EditModeFixture','actual_normal_input_play':False,'baseline_credit':0,'readbacks':[
    {'seed':s,'scenario_index':index,'catalog_id':ident,'definition_id':'skirmish.'+ident.lower(),'definition_version':2,'size':'Standard','difficulty':'Regular',
     'native_session_id':'synthetic-session-'+str(i),'native_world_sequence':str(i+1),'native_setup_hash_uint32':12+i,'native_content_identity':'synthetic-content'} for i,s in enumerate(seeds)]}}
  packet=SamplingAuthority.packet(self.matrix.catalog[ident]);p=self.root/packet
  self.job={'schema_version':3,'job_id':'synthetic-admission','catalog_id':ident,'definition_id':'skirmish.'+ident.lower(),'definition_version':2,'candidate_sha256':'a'*64,'build_sha256':'b'*64,
   'setup_sha256':'c'*64,'content_sha256':'d'*64,'source_manifest':[assets[k] for k in paths]+assets['views'],'packet_path':packet,'packet_sha256':hashlib.sha256(p.read_bytes()).hexdigest(),
   'seed':seeds[0],'locale':'EN','size':'Standard','difficulty':'Regular','control_mode':'aria','required_stages':list(queue.STAGES),'timeout_seconds':900}
  self.authority.update(source_pins=source_pins,layout_semantic_sha256='e'*64)
  self.refresh()
 def refresh(self):self.job['counterpart_authority']=self.write('Logs/fixture-authority.json',self.authority)
 def preflight(self):return queue.preflight(self.job,self.root,self.matrix)
 def test_positive_synthetic_contract(self):
  self.assertEqual('passed',self.preflight()['preflight'])
  self.job['locale']='FA';self.assertEqual('passed',self.preflight()['preflight'])
  self.assertEqual('pending',self.preflight()['native_qualification'])

 def stress_plan(self):
  # Explicit isolated TEST plan: canonical source bytes, real 1200-slot validator.
  # These seeds are not a production stress plan or authorization to run missions.
  plan=build_baseline(self.root);plan['kind']='Stress1200';plan['slots']=[]
  groups={}
  for ident,row in self.matrix.catalog.items():
   groups.setdefault((row['map_id'],row['objective_id'],row['army_profile']),{})[row['start_profile']]=ident
  for group_index,(_,starts) in enumerate(sorted(groups.items())):
   for i in range(20):
    plan['slots'].append(dict(catalog_id=starts['F' if i%2==0 else 'E'],seed=1000000+group_index*100+i,
     locale='EN' if (i//2)%2==0 else 'FA',size='Standard',difficulty='Regular',control_mode='aria'))
  return plan

 def test_complete_stress_nonbaseline_both_locales(self):
  plan=self.stress_plan();path=self.root/'stress1200.json';path.write_text(json.dumps(plan),encoding='utf-8')
  self.matrix=SamplingAuthority(self.root,path,queue.digest(path))
  self.assertEqual(1200,len(self.matrix.slots))
  baseline={r['seed'] for r in self.authority['normal_selection']['readbacks']}
  for locale in ('EN','FA'):
   slot=next(r for r in plan['slots'] if r['catalog_id']==self.CATALOG and r['locale']==locale)
   self.assertNotIn(slot['seed'],baseline);self.job.update(slot)
   self.assertEqual('passed',self.preflight()['preflight'])
   self.assertEqual('pending',self.preflight()['native_qualification'])

 def test_missing_tampered_incomplete_plans(self):
  with self.assertRaises(FileNotFoundError):SamplingAuthority(self.root,self.root/'absent.json','a'*64)
  expected=queue.digest(self.plan_path);self.plan_path.write_text('{}',encoding='utf-8')
  with self.assertRaisesRegex(ValueError,'Sampling plan hash mismatch'):SamplingAuthority(self.root,self.plan_path,expected)
  with self.assertRaisesRegex(ValueError,'Sampling plan changed'):self.preflight()
  for plan in (build_baseline(self.root),self.stress_plan()):
   plan['slots'].pop();path=self.root/'incomplete.json';path.write_text(json.dumps(plan),encoding='utf-8')
   with self.subTest(kind=plan['kind']),self.assertRaises(ValueError):SamplingAuthority(self.root,path,queue.digest(path))

 def test_wrong_slot_packet_and_schema_boundaries(self):
  original=copy.deepcopy(self.job)
  self.job['seed']=42
  with self.assertRaisesRegex(ValueError,'sampling slot'):self.preflight()
  self.job=copy.deepcopy(original);self.job['schema_version']=1;self.job.pop('counterpart_authority')
  with self.assertRaisesRegex(queue.ContractError,'schema3'):self.preflight()
  self.job=copy.deepcopy(original);self.job.update(catalog_id='S002',definition_id='skirmish.s002')
  with self.assertRaisesRegex(queue.ContractError,'schema3'):self.preflight()
  self.job=copy.deepcopy(original)
  wrong=self.root/'wrong-packet.md';shutil.copyfile(self.root/self.job['packet_path'],wrong)
  self.job.update(packet_path='wrong-packet.md',packet_sha256=queue.digest(wrong))
  with self.assertRaisesRegex(ValueError,'canonical family'):self.preflight()
 def test_requires_schema3_matrix_and_authority(self):
  with self.assertRaises(queue.ContractError):queue.preflight(self.job,self.root)
  for schema in (1,2,True,4):
   bad=copy.deepcopy(self.job);bad['schema_version']=schema
   with self.subTest(schema=schema),self.assertRaises(queue.ContractError):queue.preflight(bad,self.root,self.matrix)
  del self.job['counterpart_authority']
  with self.assertRaises(queue.ContractError):self.preflight()
 def test_authority_identity_provenance_and_readbacks(self):
  original=copy.deepcopy(self.authority)
  edits=[lambda a:a.update(catalog_id='S002'),lambda a:a.update(definition_version=3),lambda a:a.update(runtime_index=1001),lambda a:a.update(candidate_sha256='f'*64),
   lambda a:a['normal_selection'].update(actual_normal_input_play=True),lambda a:a['normal_selection'].update(baseline_credit=1),lambda a:a['normal_selection'].update(driver='NativeTouch'),
   lambda a:a['normal_selection']['readbacks'][0].update(seed=1),lambda a:a['normal_selection']['readbacks'][0].update(native_world_sequence='0'),
   lambda a:a['normal_selection']['readbacks'][0].update(definition_version=5),lambda a:a['normal_selection']['readbacks'][0].update(native_session_id=a['normal_selection']['readbacks'][1]['native_session_id'])]
  for i,edit in enumerate(edits):
   self.authority=copy.deepcopy(original);edit(self.authority);self.refresh()
   with self.subTest(corruption=i),self.assertRaises(queue.ContractError):self.preflight()
 def test_missing_changed_graph_and_evidence(self):
  for role in ('definition','layout','binding','development_catalog','practice_preset','main_catalog','main_expansion_catalog'):
   p=self.root/self.authority['assets'][role]['path'];raw=p.read_bytes();p.write_bytes(raw+b'changed')
   with self.subTest(role=role),self.assertRaises(queue.ContractError):self.preflight()
   p.write_bytes(raw)
  for role in ('measurement_report','materialization_receipt'):
   p=self.root/self.authority[role]['path'];raw=p.read_bytes();p.unlink()
   with self.subTest(role=role),self.assertRaises(queue.ContractError):self.preflight()
   p.write_bytes(raw)

 def test_large_source_pin_detects_last_byte_change(self):
  # Physical source pins are checked by counterpart authority, not job manifest.
  report=json.loads((self.root/self.authority['measurement_report']['path']).read_text())
  row=report['missions'][0];source=self.authority['source_pins']
  path=self.root/source['sourcePath']
  path.write_bytes(b'%YAML 1.1\n'+b'  terrainHeight: 0\n'*12000+b'end: 0\n')
  self.assertGreater(path.stat().st_size,64*1024)
  self.assertNotIn(source['sourcePath'],{pin['path'] for pin in self.job['source_manifest']})
  source['sourceAssetSha256']=queue.digest(path)
  report['sourcePins']=copy.deepcopy(source);row['measurement']['sourcePins']=copy.deepcopy(source)
  report['missionArtifacts'][0].update(self.write('Logs/fixture-row.json',row))
  self.authority['measurement_report']=self.write('Logs/fixture-report.json',report)
  receipt=json.loads((self.root/self.authority['materialization_receipt']['path']).read_text())
  receipt['reviewedReportSha256']=self.authority['measurement_report']['sha256']
  self.authority['materialization_receipt']=self.write('Logs/fixture-receipt.json',receipt);self.refresh()
  self.assertEqual('passed',self.preflight()['preflight'])
  with path.open('r+b') as stream:
   stream.seek(-1,2);stream.write(b'!')
  with self.assertRaisesRegex(queue.ContractError,'counterpart authority file missing/changed'):
   self.preflight()
 def test_strict_measurement_and_main_preservation(self):
  original=copy.deepcopy(self.authority)
  for flag in ('cleanup','facility','route','main','ready'):
   self.authority=copy.deepcopy(original)
   if flag in ('cleanup','facility','route'):
    report=json.loads((self.root/self.authority['measurement_report']['path']).read_text())
    if flag=='cleanup':report['cleanupVerified']=False
    else:
     row=report['missions'][0]
     if flag=='facility':row['measurement']['passes'][0]['sizes'][0]['completeFacilities']=False
     else:row['measurement']['passes'][0]['sizes'][0]['routeQueriesWithSameFacilityReservations']['queries'][0]['failureCode']=1
     report['missionArtifacts'][0].update(self.write('Logs/fixture-row.json',row))
    self.authority['measurement_report']=self.write('Logs/fixture-report.json',report)
   else:
    receipt=json.loads((self.root/self.authority['materialization_receipt']['path']).read_text())
    if flag=='main':receipt['counterpartDevelopmentResource']['mainCatalogChanged']=True
    else:receipt['normalMissionReady']=True
    self.authority['materialization_receipt']=self.write('Logs/fixture-receipt.json',receipt)
   self.refresh()
   with self.subTest(flag=flag),self.assertRaises(queue.ContractError):self.preflight()
 def test_duplicate_keys_and_graph_manifest_omission(self):
  p=self.root/self.job['counterpart_authority']['path'];p.write_text('{"schema_version":1,"schema_version":1}')
  self.job['counterpart_authority']['sha256']=hashlib.sha256(p.read_bytes()).hexdigest()
  with self.assertRaises(queue.ContractError):self.preflight()
  self.refresh();self.job['source_manifest']=self.job['source_manifest'][1:]
  with self.assertRaises(queue.ContractError):self.preflight()
 def test_internally_rewritten_wrong_report_layout_or_source(self):
  for mutation in ('layout','source'):
   report=json.loads((self.root/self.authority['measurement_report']['path']).read_text())
   row=report['missions'][0]
   if mutation=='layout':row['measurement']['proposedLayoutSemanticSha256']='f'*64
   else:row['measurement']['sourcePins']['sourceIdentityHash']='foreign-source';report['sourcePins']['sourceIdentityHash']='foreign-source'
   report['missionArtifacts'][0].update(self.write('Logs/fixture-row.json',row))
   self.authority['measurement_report']=self.write('Logs/fixture-report.json',report)
   receipt=json.loads((self.root/self.authority['materialization_receipt']['path']).read_text())
   receipt['reviewedReportSha256']=self.authority['measurement_report']['sha256']
   self.authority['materialization_receipt']=self.write('Logs/fixture-receipt.json',receipt);self.refresh()
   with self.subTest(mutation=mutation),self.assertRaises(queue.ContractError):self.preflight()

class CityAdmission(Admission):
 CATALOG='S025'

class IndustrialAdmission(Admission):
 CATALOG='S073'

if __name__=='__main__':unittest.main(verbosity=2)
