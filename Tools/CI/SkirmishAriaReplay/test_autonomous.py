"""Synthetic protocol/source controls. No world, hardware or command effect is simulated."""
import argparse,copy,hashlib,importlib.util,json,pathlib,subprocess,time,unittest
SCRIPT=pathlib.Path(__file__).resolve().parents[1]/'skirmish_aria_replay.py'
spec=importlib.util.spec_from_file_location('aria_autonomous',SCRIPT);aria=importlib.util.module_from_spec(spec);spec.loader.exec_module(aria)
BUILD=None;EVIDENCE=None;TEMPLATE=None
class AutonomousTests(unittest.TestCase):
 def setUp(self):self.client=None;self.log=[]
 def tearDown(self):
  if self.client:self.client.close()
  (EVIDENCE/(self._testMethodName+'.json')).write_text(json.dumps(self.log,indent=2,allow_nan=False)+'\n')
 def fresh(self,phase='Observing',due=0):
  initial=copy.deepcopy(TEMPLATE['initial']);initial['plan'].update(AssaultStarted=1,LastProgressAt=1,Infantry=20,EnemyHealth=800,PlayerHealth=800);initial['session'].update(Phase=phase,DueAt=due,LastProgressAt=1,LastObjectiveProgressAt=1)
  self.client=aria.AutonomousClient(BUILD);self.log.append(self.client.initialize(initial));return self.client
 def observation(self,frame,now,drag=False):
  v=copy.deepcopy(TEMPLATE['frames'][0]['observation']);v.update(Frame=frame,Time=now,AirProfile=True,Infantry=20,EnemyHealth=800,PlayerHealth=800,CanAffordLogisticsTruck=True);v['RecruitLogisticsTruck'].update(Available=True,Id=777,Position={'x':50,'y':80},Drag=drag,DragEnd={'x':50,'y':400});return v
 def step(self,frame,now,drag=False,controls=None,**context):
  c=dict(frame=frame,unscaled_time=now,ended=False,route_match=True,focused=True,allow_background=False,time_scale=1);c.update(context);answer=self.client.step(self.observation(frame,now,drag),controls or[dict(target_id=777,control_id='synthetic-source-control-777')],c);self.log.append(answer);return answer
 def released(self):self.step(1,10);self.step(2,11);return self.step(3,11.16)['contact']
 def complete(self,contact,accepted=True,receipt='synthetic-source-result-1'):
  result=self.client.complete_contact(contact,dict(receipt_id=receipt,accepted=accepted,reason='Synthetic receipt boundary control; no world executed'));self.log.append(result);return result
 def test_retained_decision_release_and_accepted_rejected_receipts(self):
  c=self.fresh();pid=c.process.pid
  a=self.step(1,10);self.assertEqual(a['state']['session']['Phase'],'Aiming');self.assertEqual(a['state']['session']['DueAt'],10.9)
  a=self.step(2,11);self.assertEqual(a['state']['session']['Actions'],0);self.assertIsNone(a['contact']);self.assertEqual(a['state']['session']['Pressed'],1)
  a=self.step(3,11.149);self.assertIsNone(a['contact']);self.assertEqual(a['state']['session']['Actions'],0)
  a=self.step(4,11.16);self.assertEqual(a['state']['session']['Actions'],0);contact=a['contact'];self.assertEqual(contact['contact_id'],1)
  a=self.complete(contact);s=a['state']['session'];self.assertEqual(s['Actions'],1);self.assertEqual(s['LastCompletedTargetId'],777);self.assertEqual(s['LastCompletedFrame'],4);self.assertEqual(s['LastCompletedPosition'],dict(x=50,y=80));self.assertEqual(s['Phase'],'Verifying');self.assertAlmostEqual(s['DueAt'],11.81,places=4);self.assertEqual(a['trace_frame']['session_actions_delta'],1);self.assertEqual(a['trace_frame']['feedback']['receipt']['status'],'accepted')
  self.step(5,12);self.step(6,13);b=self.step(7,13.16)['contact'];self.assertEqual(b['contact_id'],2)
  a=self.complete(b,False,'synthetic-rejected-2');self.assertEqual(a['state']['session']['Actions'],2);self.assertFalse(a['source_receipt']['accepted']);self.assertEqual(a['trace_frame']['feedback']['receipt']['status'],'rejected');self.assertFalse(a['trace_frame']['feedback']['source_receipt']['accepted']);self.assertEqual(c.process.pid,pid);self.assertEqual(a['state']['plan']['LastProgressAt'],1)
 def test_drag_hold_travel_and_separate_release_opportunities(self):
  self.fresh();self.step(1,10,True);self.step(2,11,True);self.assertIsNone(self.step(3,11.49,True)['contact']);self.assertIsNone(self.step(4,11.5,True)['contact']);self.assertIsNone(self.step(5,12.39,True)['contact']);self.assertIsNone(self.step(6,12.41,True)['contact']);a=self.step(7,12.42,True);self.assertTrue(a['contact']['drag']);self.assertEqual(a['contact']['position'],dict(x=50,y=400));self.assertEqual(a['state']['session']['Actions'],0);self.assertEqual(self.complete(a['contact'])['state']['session']['Actions'],1)
 def test_contact_keeps_press_time_control_identity(self):
  self.fresh();self.step(1,10);self.step(2,11);a=self.step(3,11.16,controls=[dict(target_id=777,control_id='replacement-same-target')]);self.assertEqual(a['contact']['control_id'],'synthetic-source-control-777');self.complete(a['contact'])
 def test_wrong_receipt_stops_without_completion(self):
  self.fresh();contact=self.released();contact['contact_id']+=1
  with self.assertRaises(aria.AutonomousProtocolError)as e:self.complete(contact)
  self.assertEqual(e.exception.response['state']['session']['Actions'],0);self.assertTrue(e.exception.response['stopped']);self.assertTrue(self.client.failed);self.assertEqual(self.client.process.wait(timeout=5),2)
 def test_replayed_contact_cannot_increment_actions_twice(self):
  self.fresh();contact=self.released();self.complete(contact)
  with self.assertRaises(aria.AutonomousProtocolError)as e:self.complete(contact,receipt='new-receipt-same-contact')
  self.assertEqual(e.exception.response['state']['session']['Actions'],1)
 def test_same_receipt_id_cannot_complete_next_contact(self):
  self.fresh();self.complete(self.released());self.step(4,12);self.step(5,13);contact=self.step(6,13.16)['contact']
  with self.assertRaises(aria.AutonomousProtocolError)as e:self.complete(contact)
  self.assertEqual(e.exception.response['state']['session']['Actions'],1)
 def test_future_step_waits_for_same_frame_world_receipt(self):
  self.fresh();self.released()
  with self.assertRaises(aria.AutonomousProtocolError)as e:self.step(4,12)
  self.assertIn('World receipt',str(e.exception));self.assertEqual(e.exception.response['state']['session']['Actions'],0)
 def test_wrong_receipt_release_frame_and_time_reject(self):
  self.fresh();contact=self.released();contact['frame']+=1
  with self.assertRaises(aria.AutonomousProtocolError)as e:self.complete(contact)
  self.assertEqual(e.exception.response['state']['session']['Actions'],0)
 def test_lifecycle_terminal_cancels_inflight_without_action(self):
  self.fresh();self.step(1,10);self.step(2,11);a=self.step(3,11.1,ended=True);self.assertIsNone(a['contact']);self.assertEqual(a['state']['session']['Actions'],0);self.assertEqual(a['state']['session']['Phase'],'Manual');self.assertEqual(a['state']['session']['Pressed'],0)
 def test_focus_route_and_time_scale_stop(self):
  for change,reason in[({'focused':False},2),({'route_match':False},1),({'time_scale':0},3)]:
   self.fresh();a=self.step(1,10,**change);self.assertEqual(a['state']['session']['Phase'],'Manual');self.assertEqual(a['state']['session']['StopReason'],reason);self.client.close();self.client=None
 def test_source_start_due_delay_and_freshness(self):
  self.fresh('Starting',5);self.assertEqual(self.step(1,1)['state']['session']['Phase'],'Starting');self.assertEqual(self.step(2,5)['state']['session']['Phase'],'Observing');self.assertEqual(self.step(3,6)['state']['session']['Phase'],'Aiming')
 def test_source_watchdog_blocks_without_objective_progress(self):
  self.fresh();self.complete(self.released(),False);a=self.step(4,200);self.assertEqual(a['state']['session']['Phase'],'Blocked');self.assertIsNone(a['contact']);self.assertEqual(a['state']['session']['Actions'],1);self.assertEqual(a['state']['plan']['LastProgressAt'],1)
 def test_duplicate_registry_ids_reject_before_source_step(self):
  self.fresh()
  with self.assertRaises(aria.AutonomousProtocolError)as e:self.step(1,10,controls=[dict(target_id=777,control_id='a'),dict(target_id=777,control_id='b')])
  self.assertEqual(e.exception.response['state']['session']['Actions'],0)
 def test_autonomous_session_patch_is_never_admitted(self):
  self.fresh()
  with self.assertRaises(aria.AutonomousProtocolError):self.client._rpc('step',observation=self.observation(1,10),controls=[],context={},session_patch={'Actions':1})
 def test_second_initialize_cannot_reseed(self):
  self.fresh()
  with self.assertRaises(aria.AutonomousProtocolError):self.client.initialize(TEMPLATE['initial'])
 def test_full_source_extraction_proof(self):
  manifest=json.loads((BUILD/'manifest.json').read_text());by={pathlib.Path(x['source']).name:x for x in manifest['source_extraction']}
  decision=by['AriaPlayDecisionSystem.cs'];raw=(BUILD/'original/AriaPlayDecisionSystem.cs').read_text();a,k=aria.method_span(raw,'public void OnUpdate(ref SystemState state)');a=raw.rfind('        [BurstCompile]',0,a);self.assertEqual(raw[:a]+raw[k:],(BUILD/'source/AriaPlayDecisionSystem.cs').read_text())
  original=(BUILD/'original/AriaPlayInputSystem.cs').read_text();a,k=aria.method_span(original,'if (current.Phase == AriaPlayPhase.Touching && !touch.IsBusy)');body=original[original.index('{',a)+1:k-1];generated=(BUILD/'source/AriaPlayInputSystem.cs').read_text();self.assertIn(body.replace('UnityEngine.Time.frameCount','frame').replace('UnityEngine.Time.unscaledTime','now'),generated);self.assertEqual(hashlib.sha256(body.encode()).hexdigest(),by['AriaPlayInputSystem.cs']['source_completion_sha256'])
  self.log.append(dict(decision=decision,completion=by['AriaPlayInputSystem.cs']))
 def drift(self,path,mutate):
  self.fresh();before=path.read_bytes()
  try:
   mutate(path)
   with self.assertRaisesRegex(ValueError,'identity changed'):self.step(1,10)
   self.assertTrue(self.client.failed);self.assertIsNotNone(self.client.process.poll())
   with self.assertRaisesRegex(ValueError,'changed'):self.client.close()
   self.client=None
  finally:path.write_bytes(before)
 def test_source_drift_stops_before_next_step(self):self.drift(BUILD/'source/AriaPlayDecisionSystem.cs',lambda p:p.write_bytes(p.read_bytes()+b'\n// temp drift\n'))
 def test_binary_drift_stops_before_next_step(self):self.drift(BUILD/'bin/Release/net8.0/AriaReplay.dll',lambda p:p.write_bytes(p.read_bytes()+b'\x00'))
 def test_wrong_receipt_time_rejects_and_exits(self):
  self.fresh();contact=self.released();contact['unscaled_time']+=.01
  with self.assertRaises(aria.AutonomousProtocolError)as e:self.complete(contact)
  self.assertEqual(e.exception.response['state']['session']['Actions'],0);self.assertEqual(self.client.process.wait(timeout=5),2)
 def test_terminal_cancels_released_contact_before_receipt(self):
  self.fresh();contact=self.released();a=self.step(4,12,ended=True);self.assertEqual(a['state']['session']['Actions'],0);self.assertIsNone(a['contact']);self.assertEqual(a['state']['session']['Phase'],'Manual')
  with self.assertRaises(aria.AutonomousProtocolError):self.complete(contact)
 def test_jsonl_cli_transport(self):
  initial=copy.deepcopy(TEMPLATE['initial']);obs=self.observation(1,10);ctx=dict(frame=1,unscaled_time=10,ended=False,route_match=True,focused=True,allow_background=False,time_scale=1)
  requests=[dict(request_id=1,op='initialize',initial=initial),dict(request_id=2,op='step',observation=obs,controls=[dict(target_id=777,control_id='public-control')],context=ctx),dict(request_id=3,op='close')]
  r=subprocess.run([__import__('sys').executable,str(SCRIPT),'serve','--build-dir',str(BUILD)],input=''.join(json.dumps(x)+'\n'for x in requests),capture_output=True,text=True,timeout=30);self.assertEqual(r.returncode,0,r.stderr);rows=[json.loads(x)for x in r.stdout.splitlines()];self.assertEqual([x['request_id']for x in rows],[1,2,3]);self.assertTrue(rows[-1]['closed']);self.assertEqual(rows[1]['trace_frame']['frame'],1);self.log.extend(rows)
 def test_future_public_observation_clock_rejects_before_source_step(self):
  self.fresh();ctx=dict(frame=1,unscaled_time=10,ended=False,route_match=True,focused=True,allow_background=False,time_scale=1)
  with self.assertRaises(aria.AutonomousProtocolError)as e:self.client.step(self.observation(1,11),[dict(target_id=777,control_id='public-control')],ctx)
  self.assertEqual(e.exception.response['state']['session']['Actions'],0);self.assertEqual(e.exception.response['state']['session']['Phase'],'Observing');self.assertEqual(self.client.process.wait(timeout=5),2)
 def test_changed_source_touch_timing_requires_review(self):
  root=EVIDENCE/'timing-fixture-repo';root.mkdir();manifest=json.loads((BUILD/'manifest.json').read_text())
  for item in manifest['source_extraction']:
   f=root/item['source'];f.parent.mkdir(parents=True,exist_ok=True);f.write_bytes((BUILD/'original'/pathlib.Path(item['source']).name).read_bytes())
  f=root/'Assets/Game/Scripts/UI/Shell/Ecs/AriaPlayInputSystem.cs';f.write_text(f.read_text().replace('hasSkirmish ? .15f','hasSkirmish ? .16f'))
  r=subprocess.run([__import__('sys').executable,str(SCRIPT),'build','--repo',str(root),'--engine-dir',manifest['engine_dir'],'--build-dir',str(EVIDENCE/'timing-drift-build')],capture_output=True,text=True,timeout=30);self.assertEqual(r.returncode,2);self.assertIn('Source gesture timing changed',r.stderr);self.log.append(dict(exit_code=r.returncode,stdout=r.stdout,stderr=r.stderr))
 def test_many_idle_steps_keep_single_process_and_measure_rpc_cost(self):
  c=self.fresh('Starting',1000);pid=c.process.pid;start=time.monotonic()
  for i in range(1,101):self.step(i,i*.01)
  elapsed=time.monotonic()-start;self.assertEqual(c.process.pid,pid);self.assertEqual(self.log[-1]['state']['session']['Actions'],0);self.log.append(dict(performance_steps=100,elapsed_seconds=elapsed,seconds_per_rpc=elapsed/100,pin_policy='full SHA startup/end; dev/ino/size/mtime_ns/ctime_ns per RPC'))
if __name__=='__main__':
 parser=argparse.ArgumentParser();parser.add_argument('--build-dir',required=True);parser.add_argument('--evidence',required=True);parser.add_argument('--only',nargs='*');args=parser.parse_args();BUILD=pathlib.Path(args.build_dir);EVIDENCE=pathlib.Path(args.evidence);EVIDENCE.mkdir(parents=True,exist_ok=True);manifest=json.loads((BUILD/'manifest.json').read_text());TEMPLATE=json.loads(subprocess.check_output([manifest['dotnet'],manifest['host'],'template']));suite=unittest.TestSuite(AutonomousTests(n)for n in args.only)if args.only else unittest.defaultTestLoader.loadTestsFromTestCase(AutonomousTests);result=unittest.TextTestRunner(verbosity=2).run(suite);print('[AriaAutonomous] result='+('Passed'if result.wasSuccessful()else'Failed')+' controls='+str(result.testsRun)+' synthetic=True nativeExecution=0 hardwareInput=0');raise SystemExit(0 if result.wasSuccessful()else 2)
