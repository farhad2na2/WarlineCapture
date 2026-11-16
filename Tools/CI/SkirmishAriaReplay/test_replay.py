"""Focused CLI regressions: actual production Step, not a mirrored planner."""
import copy,hashlib,json,pathlib,subprocess,sys,tempfile,unittest
SCRIPT=pathlib.Path(__file__).resolve().parents[1]/'skirmish_aria_replay.py'
BUILD=None;EVIDENCE=None
class ReplayTests(unittest.TestCase):
 def invoke(self,data=None,raw=None,success=True):
  folder=pathlib.Path(tempfile.mkdtemp(prefix=self._testMethodName+'-',dir=EVIDENCE));source=folder/'input.json';out=folder/'output.json'
  source.write_text(raw if raw is not None else json.dumps(data,allow_nan=False));before=source.read_bytes()
  result=subprocess.run([sys.executable,str(SCRIPT),'run','--build-dir',str(BUILD),'--input',str(source),'--output',str(out)],capture_output=True,text=True,timeout=30)
  (folder/'stdout.log').write_text(result.stdout);(folder/'stderr.log').write_text(result.stderr)
  self.assertEqual(source.read_bytes(),before,'Input mutated')
  if success:self.assertEqual(result.returncode,0,result.stderr);return json.loads(out.read_text()),folder
  self.assertEqual(result.returncode,2,result.stdout);self.assertFalse(out.exists(),'Partial output accepted');return result.stderr,folder
 def simple(self):return {'schema_version':1,'mission_id':'S002-synthetic-control','provenance':{'kind':'synthetic','detail':'Synthetic planner observation/feedback mechanics; no gameplay outcomes'},'compact_synthetic_defaults':True,'initial':{'plan':{},'session':{'Phase':'Observing'},'output':{}},'frames':[{'observation':{'Active':True,'ExpandedSession':True,'Frame':1,'Time':1}}]}
 def supply(self,actions=False):
  d=self.simple();d['initial']['plan']={'AssaultStarted':1,'LastProgressAt':1,'Infantry':20,'EnemyHealth':800,'PlayerHealth':800};d['initial']['session'].update(LastProgressAt=1,LastObjectiveProgressAt=1)
  obs={'Active':True,'ExpandedSession':True,'AirProfile':True,'Infantry':20,'EnemyHealth':800,'PlayerHealth':800,'CanAffordLogisticsTruck':True,'RecruitLogisticsTruck':{'Available':True,'Id':777,'Position':{'x':50,'y':80}}}
  d['frames']=[]
  for i,t in enumerate([10,20,30,181,182]):
   v=copy.deepcopy(obs);v.update(Time=t,Frame=i+1);row={'observation':v}
   if actions:row['feedback']={'receipt':{'id':'receipt-'+str(i),'status':'no_effect','target_id':777,'detail':'Explicit synthetic completed support contact777 without observed gameplay progress'},'session_patch':{'Actions':i+1,'TargetId':777,'LastCompletedTargetId':777,'LastCompletedFrame':i+1,'Phase':'Observing'}}
   d['frames'].append(row)
  return d
 def test_repeated_support_proposals_do_not_execute(self):
  output,_=self.invoke(self.supply());rows=output['frames'];self.assertEqual([r['target_id']for r in rows[:4]],[777]*4);self.assertTrue(all(r['after']['session']['Actions']==0 for r in rows));self.assertEqual(rows[-1]['after']['session']['Phase'],'Blocked');self.assertEqual(rows[-1]['after']['plan']['LastProgressAt'],1)
  (EVIDENCE/'support-proposals.input.json').write_text(json.dumps(self.supply(),indent=2));(EVIDENCE/'support-proposals.output.json').write_text(json.dumps(output,indent=2))
 def test_completed_support_taps_do_not_fake_objective_progress(self):
  output,_=self.invoke(self.supply(actions=True));rows=output['frames'];self.assertEqual(rows[-1]['after']['session']['Actions'],5);self.assertEqual(rows[-1]['after']['session']['Phase'],'Blocked');self.assertEqual(rows[-1]['after']['session']['LastObjectiveProgressAt'],1);self.assertTrue(all(r['session_actions_delta']==1 for r in rows))
  (EVIDENCE/'support-no-progress.input.json').write_text(json.dumps(self.supply(actions=True),indent=2));(EVIDENCE/'support-no-progress.output.json').write_text(json.dumps(output,indent=2))
 def test_accepted_annotation_alone_does_not_increment_actions(self):
  d=self.simple();d['frames'][0]['feedback']={'receipt':{'id':'a','status':'accepted','detail':'annotation only'}};o,_=self.invoke(d);self.assertEqual(o['frames'][0]['session_actions_delta'],0)
 def test_support_only_survivors_choose_combat_recovery(self):
  d=self.simple();d['frames'][0]['observation'].update(AirProfile=True,CombinedArmsField=True,Infantry=0,OwnStructureAttackUnits=0,ExpandedTotalGroups=2,AvailableSquads=3,CanAffordLogisticsTruck=True,RecruitLogisticsTruck={'Available':True,'Id':777},CanAffordRocketeer=True,RecruitRocketeer={'Available':True,'Id':888})
  nextrow=copy.deepcopy(d['frames'][0]);nextrow['observation'].update(Frame=2,Time=2,RocketeerRecruitPending=True);d['frames'].append(nextrow);o,_=self.invoke(d);self.assertEqual(o['frames'][0]['target_id'],888);self.assertEqual(o['frames'][1]['observation_kind'],'Waiting');self.assertEqual(o['frames'][1]['target_id'],0)
  (EVIDENCE/'support-only-force.input.json').write_text(json.dumps(d,indent=2));(EVIDENCE/'support-only-force.output.json').write_text(json.dumps(o,indent=2))
 def test_finished_uses_original_handback(self):
  d=self.simple();d['frames'][0]['observation']['Finished']=True;o,_=self.invoke(d);self.assertEqual(o['frames'][0]['intent'],'Handback');self.assertEqual(o['frames'][0]['observation_kind'],'Finished')
 def test_touching_gate_preserves_existing_session(self):
  d=self.simple();d['initial']['session'].update(Phase='Touching',TargetId=22,Actions=9);o,_=self.invoke(d);self.assertEqual(o['frames'][0]['after']['session']['TargetId'],22);self.assertEqual(o['frames'][0]['after']['session']['Actions'],9)
 def test_template_roundtrip_complete_captured_shape(self):
  r=subprocess.run([json.loads((BUILD/'manifest.json').read_text())['dotnet'],str(BUILD/'bin/Release/net8.0/AriaReplay.dll'),'template'],capture_output=True,text=True,check=True);d=json.loads(r.stdout);d['provenance']={'kind':'captured','detail':'Shape-only synthetic fixture labelled captured to check strictness, not actual evidence'};o,_=self.invoke(d);self.assertFalse(o['compact_synthetic_defaults']);self.assertIn('observation',o['frames'][0]);self.assertEqual(o['initial']['session']['Phase'],'Observing')
 def test_missing_captured_field_rejects(self):
  d=self.simple();d['compact_synthetic_defaults']=False;self.invoke(d,success=False)
 def test_captured_compact_forbidden(self):
  d=self.simple();d['provenance']['kind']='captured';self.invoke(d,success=False)
 def test_unknown_nested_observation_rejects(self):
  d=self.simple();d['frames'][0]['observation']['AcceptedCommand']=True;self.invoke(d,success=False)
 def test_unknown_plan_field_rejects(self):
  d=self.simple();d['initial']['plan']['FutureWin']=True;self.invoke(d,success=False)
 def test_future_plan_feedback_rejected(self):
  d=self.simple();d['frames'][0]['feedback']={'plan':{'Intent':'Handback'}};self.invoke(d,success=False)
 def test_unknown_receipt_status_rejects(self):
  d=self.simple();d['frames'][0]['feedback']={'receipt':{'id':'a','status':'won','detail':'bad'}};self.invoke(d,success=False)
 def test_repeated_receipt_status_update_no_actions(self):
  d=self.simple();d['frames'][0]['feedback']={'receipt':{'id':'same','status':'accepted','detail':'annotation'}};r=copy.deepcopy(d['frames'][0]);r['observation'].update(Time=2,Frame=2);r['feedback']['receipt']['status']='completed';d['frames'].append(r);o,_=self.invoke(d);self.assertEqual([x['session_actions_delta']for x in o['frames']],[0,0]);self.assertEqual([x['feedback']['receipt']['status']for x in o['frames']],['accepted','completed'])
 def test_actions_reset_explicit_negative_delta(self):
  d=self.simple();d['initial']['session']['Actions']=7;d['frames'][0]['feedback']={'session_patch':{'Actions':0}};o,_=self.invoke(d);row=o['frames'][0];self.assertEqual(row['before']['session']['Actions'],7);self.assertEqual(row['step_input_session']['Actions'],0);self.assertEqual(row['after']['session']['Actions'],0);self.assertEqual(row['session_actions_delta'],-7)
 def test_negative_feedback_actions_rejects(self):
  d=self.simple();d['frames'][0]['feedback']={'session_patch':{'Actions':-1}};self.invoke(d,success=False)
 def test_negative_initial_actions_rejects(self):
  d=self.simple();d['initial']['session']['Actions']=-1;self.invoke(d,success=False)
 def test_actual_production_fixture_drift_rejects(self):
  folder=pathlib.Path(tempfile.mkdtemp(prefix='source-fixture-',dir=EVIDENCE));repo=folder/'repo';m=json.loads((BUILD/'manifest.json').read_text())
  for item in m['source_extraction']:
   target=repo/item['source'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((BUILD/'original'/pathlib.Path(item['source']).name).read_bytes())
  out=folder/'fixture-build';r=subprocess.run([sys.executable,str(SCRIPT),'build','--repo',str(repo),'--engine-dir',m['engine_dir'],'--build-dir',str(out)],capture_output=True,text=True,timeout=60);self.assertEqual(r.returncode,0,r.stderr);(folder/'build.stdout.log').write_text(r.stdout)
  planner=repo/'Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.cs';planner.write_bytes(planner.read_bytes()+b'\n// stale-source fixture edit only\n');inp=folder/'input.json';inp.write_text(json.dumps(self.simple()));output=folder/'result.json';r=subprocess.run([sys.executable,str(SCRIPT),'run','--build-dir',str(out),'--input',str(inp),'--output',str(output)],capture_output=True,text=True,timeout=30);self.assertEqual(r.returncode,2);self.assertIn(str(planner),r.stderr);self.assertFalse(output.exists());(folder/'stale-rejection.stderr.log').write_text(r.stderr)
 def test_actual_host_template_drift_rejects(self):
  host=SCRIPT.parent/'SkirmishAriaReplay/Program.cs';before=host.read_bytes()
  try:host.write_bytes(before+b'\n// temp host drift regression\n');stderr,_=self.invoke(self.simple(),success=False);self.assertIn(str(host),stderr)
  finally:host.write_bytes(before)
 def test_negative_loop_cursor_rejects(self):
  d=self.simple();d['initial']['plan']['Slot']=-2147483648;self.invoke(d,success=False)
 def test_nested_negative_loop_cursor_rejects(self):
  d=self.simple();d['initial']['plan']['PublicScan']={'NextSlot':-1};self.invoke(d,success=False)
 def test_oversized_page_mask_rejects(self):
  d=self.simple();d['frames'][0]['observation']['ExpandedAirMask']=32;self.invoke(d,success=False)
 def test_unknown_enum_rejects(self):
  d=self.simple();d['initial']['session']['Phase']='Accepted';self.invoke(d,success=False)
 def test_integer_overflow_rejects(self):
  d=self.simple();d['initial']['session']['Actions']=1<<40;self.invoke(d,success=False)
 def test_float_overflow_rejects(self):
  d=self.simple();d['frames'][0]['observation']['Time']=1e100;self.invoke(d,success=False)
 def test_nan_literal_rejects(self):self.invoke(raw='{"schema_version":NaN}',success=False)
 def test_duplicate_json_keys_rejects(self):self.invoke(raw='{"schema_version":1,"schema_version":1}',success=False)
 def test_backwards_frame_rejects(self):
  d=self.simple();r=copy.deepcopy(d['frames'][0]);r['observation'].update(Time=2,Frame=0);d['frames'].append(r);self.invoke(d,success=False)
 def test_backwards_clock_rejects(self):
  d=self.simple();r=copy.deepcopy(d['frames'][0]);r['observation'].update(Time=0,Frame=2);d['frames'].append(r);self.invoke(d,success=False)
 def test_oversized_fixedlist_rejects(self):
  d=self.simple();d['initial']['plan']['PrecisionAcceptedGroups']=list(range(128));self.invoke(d,success=False)
 def test_fixedlist_values_persist(self):
  d=self.simple();d['initial']['plan']['PrecisionAcceptedGroups']=[7,8];o,_=self.invoke(d);self.assertEqual(o['frames'][0]['before']['plan']['PrecisionAcceptedGroups'],[7,8]);self.assertEqual(o['frames'][0]['after']['plan']['PrecisionAcceptedGroups'],[7,8])
 def test_frame_limit_rejects(self):
  d=self.simple();d['frames']=[d['frames'][0]]*1025;self.invoke(d,success=False)
 def test_late_invalid_frame_no_prefix_output(self):
  d=self.simple();d['frames'].append({'observation':{'Frame':2,'Time':2,'bad':4}});self.invoke(d,success=False)
 def test_output_immutability(self):
  o,folder=self.invoke(self.simple());before=(folder/'output.json').read_bytes();r=subprocess.run([sys.executable,str(SCRIPT),'run','--build-dir',str(BUILD),'--input',str(folder/'input.json'),'--output',str(folder/'output.json')],capture_output=True,text=True);self.assertEqual(r.returncode,2);self.assertEqual((folder/'output.json').read_bytes(),before)
 def test_pinned_source_tamper_rejects(self):
  file=BUILD/'source/AriaSkirmishPlanSystem.cs';before=file.read_bytes()
  try:file.write_bytes(before+b'\n// intentional temp tamper control\n');self.invoke(self.simple(),success=False)
  finally:file.write_bytes(before)
if __name__=='__main__':
 BUILD=pathlib.Path(sys.argv[1]).resolve();EVIDENCE=pathlib.Path(sys.argv[2]).resolve();EVIDENCE.mkdir();sys.argv=sys.argv[:1]+sys.argv[3:];unittest.main(verbosity=2)
