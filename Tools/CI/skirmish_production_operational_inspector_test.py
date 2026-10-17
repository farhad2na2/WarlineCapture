"""Portable operational inspector controls; no native or qualification credit."""
import copy,json,shutil,sys,unittest
from pathlib import Path
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE))
from skirmish_operational_test_fixture import (ROOT,OperationalFixture,owned_workspace,cleanup_workspace,build_baseline,write,q)
import skirmish_production_operational_inspector as inspector
if Path(inspector.__file__).resolve().parent!=HERE:raise RuntimeError("Production inspector copy not imported")
class OperationalJoins(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary=owned_workspace("i");cls.work=Path(cls.temporary.name);cls.number=0
        cls.plan=build_baseline(ROOT)
    @classmethod
    def tearDownClass(cls):cleanup_workspace(cls.temporary)
    def setUp(self):
        type(self).number+=1
        self.__dict__.update(OperationalFixture(self.work/str(type(self).number),self.plan).__dict__)
    event=OperationalFixture.event
    ref=OperationalFixture.ref
    materialize=OperationalFixture.materialize
    def check(self,passed=False,needle=None):
        spec=self.materialize();result=inspector.inspect(self.root,spec)
        self.assertEqual('NormalCompletedUnqualified' if passed else 'Incomplete',result['result'],result)
        self.assertEqual(False,result['qualified']);self.assertEqual(False,result['queue_success_enabled'])
        self.assertEqual('MissingAdmission',result['admission']['result']);self.assertEqual(6,len(result['admission']['missing']))
        if needle:self.assertIn(needle,' '.join(result['errors']))
        if not passed:self.assertEqual(0,result['wins'])
        return result
    def test_victory_defeat_draw_and_zero_world_unqualified(self):
        result=self.check(True);self.assertEqual(['Victory','Defeat','Draw'],[r['outcome'] for r in result['attempts']])
        self.assertEqual([1,0,0],[r['wins'] for r in result['attempts']]);self.assertEqual(1,result['wins'])
        self.assertEqual('0',self.records[0][1]['facts']['native_world_sequence'])
    def test_editor_baseline_world_not_native_attempt(self):
        for row in self.records[0]:
            if "native_world_sequence" in row["facts"]:row["facts"]["native_world_sequence"]="7"
        self.check(needle="session/world")
    def test_wrapper_nonzero(self):self.outer['checked_wrapper_exit']=2;self.check(needle='outer wrapper')
    def test_unavailable_exit(self):self.log+='process exit code was unavailable';self.check(needle='unavailable')
    def test_source_after_drift(self):self.sources['after']['observed_candidate_sha256']='0'*64;self.check(needle='endpoint differs')
    def test_actual_claim_mutation(self):
        p=self.paths[0]['claim'];c=q.load(p);c['worker']='foreign';write(p,c);self.check(needle='claim differs')
    def test_attempt_splice(self):self.batch[1]['facts']['attempt_id']='attempt-'+'f'*32;self.check(needle='attempt splice')
    def test_event_owner_splice(self):self.records[0][2]['worker']='foreign';self.check(needle='ownership/order')
    def test_event_sequence(self):self.records[0][2]['sequence']=1;self.check(needle='ownership/order')
    def test_native_config_splice(self):self.records[0][2]['facts']['native_config_sha256']='0'*64;self.check(needle='config')
    def test_native_setup_seed(self):self.records[0][2]['facts']['native_setup_identity']['seed']+=1;self.check(needle='configuration differs')
    def test_native_foreign_session(self):self.records[0][3]['facts']['native_session_id']='foreign';self.check(needle='session')
    def test_frozen_result_splice(self):self.records[0][3]['facts']['result_session_id']='foreign';self.check(needle='Frozen result')
    def test_unfrozen_terminal(self):self.records[0][3]['facts']['result_frozen']=0;self.check(needle='Frozen result')
    def test_abort_stays_incomplete(self):
        self.records[0][3]['facts']['phase']='Playing';self.csvrows[0]['result']='Abort';self.csvrows[0]['end_reason']='ariaStopped:Blocked'
        self.check(needle='Finished terminal')
    def test_diagnostic_window_not_match(self):self.csvrows[0]['result']='Abort';self.csvrows[0]['end_reason']='diagnosticWindowComplete';self.check(needle='terminal row')
    def test_no_normal_menu(self):self.records[0][1]['kind']='NativeResolvedSnapshot';self.check(needle='Menu launch')
    def test_missing_return(self):self.records[0][4]['kind']='Other';self.check(needle='TerminalReturnOwnerCompleted')
    def test_wrong_terminal_driver(self):self.records[0][4]['facts']['driver_device']=999;self.check(needle='driver')
    def test_intervention(self):self.contacts[0][0]['interventions']=1;self.check(needle='contact audit')
    def test_no_terminal_contacts(self):
        for c in self.contacts[0]:
            if c['phase']=='Terminal':c['phase']='Menu'
        self.check(needle='contact audit')
    def test_physical_session_remains(self):self.records[0][4]['facts']['native_terminal_session_count']=1;self.check(needle='return')
    def test_wrong_rendered_locale(self):self.records[0][4]['facts']['rendered_result_presentation']['locale']='foreign';self.check(needle='presentation')
    def test_no_restoration(self):self.records[0][-1]['kind']='RestorePending';self.check(needle='ContextRestored')
    def test_cleanup_leak(self):
        r=next(r for r in self.batch if r['kind']=='CleanupOwnershipCensus');r['facts']['census']['worlds'][0]['owned_actors']=1
        self.check()
    def test_cleanup_root_difference(self):
        r=next(r for r in self.batch if r['kind']=='CleanupOwnershipCensus');r['facts']['census']['scenes'][0]['roots'][0]['active_self']=False
        self.check(needle='cleanup differs')
    def test_runtime_error(self):next(r for r in self.batch if r['kind']=='RuntimeQuittingGate')['facts']['errors']=1;self.check(needle='runtime')
    def test_native_failure_reason_preserved(self):
        self.event('BatchFailed',error='ariaStopped:Blocked')
        result=self.check();self.assertEqual('ariaStopped:Blocked',result['failure_records'][0]['error'])
    def test_wrong_csv_config(self):self.csvrows[0]['config_hash']='0'*64;self.check(needle='terminal row')
    def test_csv_duplicate_run(self):self.csvrows.append(copy.deepcopy(self.csvrows[0]));self.check(needle='Unique actual run')
    def test_caller_truth_flags_refused(self):
        spec=self.materialize();spec['source_fit_passed']=True;result=inspector.inspect(self.root,spec)
        self.assertEqual('Incomplete',result['result']);self.assertFalse(result['qualified'])
    def test_artifact_changed_after_ref(self):
        spec=self.materialize();self.paths[0]['final'].write_text('{}');result=inspector.inspect(self.root,spec)
        self.assertEqual('Incomplete',result['result']);self.assertTrue(any('SHA differs' in e for e in result['errors']))
    def test_final_contacts_spliced_from_other_attempt(self):
        spec=self.materialize();shutil.copyfile(self.paths[1]['contacts'],self.paths[0]['contacts']);spec['attempts'][0]['contacts']=self.ref(self.paths[0]['contacts'])
        result=inspector.inspect(self.root,spec);self.assertEqual('Incomplete',result['result'])
    def test_inspector_never_changes_input_bytes(self):
        spec=self.materialize();paths=[p for p in self.root.rglob('*') if p.is_file()]
        before={p:q.digest(p) for p in paths};result=inspector.inspect(self.root,spec);self.assertEqual('NormalCompletedUnqualified',result['result'])
        self.assertEqual(set(paths),{p for p in self.root.rglob('*') if p.is_file()})
        self.assertEqual(before,{p:q.digest(p) for p in paths})
    def renumber_native(self):
        for n,row in enumerate(self.records[0],1):row["sequence"]=n
    def reorder_native(self,indices):
        self.records[0]=[self.records[0][i] for i in indices]
        self.renumber_native()
    def test_finished_before_menu_renumbered_refused(self):
        self.reorder_native([0,3,1,2,4,5,6]);self.check(needle="precede Finished")
    def test_return_before_finished_renumbered_refused(self):
        self.reorder_native([0,1,2,4,3,5,6]);self.check(needle="snapshot outside")
    def test_restore_before_return_renumbered_refused(self):
        self.reorder_native([0,1,2,3,5,4,6]);self.check(needle="lifecycle order")
    def test_playing_after_finished_renumbered_refused(self):
        self.reorder_native([0,1,3,2,4,5,6]);self.check(needle="regressed")
    def test_duplicate_context_captured_refused(self):
        self.records[0].insert(1,copy.deepcopy(self.records[0][0]))
        self.renumber_native();self.check(needle="ContextCaptured lifecycle")
    def test_duplicate_restore_start_refused(self):
        self.records[0].insert(5,copy.deepcopy(self.records[0][5]))
        self.renumber_native();self.check(needle="RestoreStarting lifecycle")
    def test_missing_terminal_owner_snapshot_refused(self):
        self.records[0][3]["kind"]="NativeResolvedSnapshot";self.check(needle="TerminalSnapshot owner")
    def test_diagnostics_and_repeated_consistent_snapshots_allowed(self):
        diagnostic=copy.deepcopy(self.records[0][0]);diagnostic.update(kind="ReadOnlyDiagnostic",facts={"measured":1})
        self.records[0].insert(1,diagnostic)
        self.records[0].insert(4,copy.deepcopy(self.records[0][3]))
        self.records[0].insert(6,copy.deepcopy(self.records[0][5]))
        self.renumber_native();self.check(True)


class ObserverAdmissions(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.temporary=owned_workspace("o");cls.work=Path(cls.temporary.name);cls.number=0;cls.plan=build_baseline(ROOT)
    @classmethod
    def tearDownClass(cls):cleanup_workspace(cls.temporary)
    def setUp(self):
        type(self).number+=1
        self.__dict__.update(OperationalFixture(self.work/str(type(self).number),self.plan).__dict__)
    materialize=OperationalFixture.materialize
    ref=OperationalFixture.ref
    def add_proofs(self):
        for i,events in enumerate(self.records):
            new=[]
            for row in events:
                new.append(row)
                if row['kind'] not in ('NativeResolvedSnapshot','TerminalSnapshot'):continue
                facts=row['facts']
                if facts['phase'] not in ('Playing','Finished'):continue
                proof=copy.deepcopy(row);proof['kind']='NativeReadOnlyObserverProof'
                proof['facts']=dict(schema='NativeReadOnlyObserverWindow-v1',scope='ExpandedResolvedSnapshot',
                    native_world_sequence=facts['native_world_sequence'],native_session_id=facts['native_session_id'],
                    catalog_id='S026',phase=facts['phase'],status='ComparedEqual',beforeSha256='c'*64,afterSha256='c'*64,
                    capturedEntityCount=5,queriedRelevantEntityCount=5,coverageComplete=True,
                    capturedFrameBefore=row['frame'],capturedFrameAfter=row['frame'],worldEntityCount=10,
                    observerElapsedMilliseconds=2.5,captureQueriesReadOnly=True,worldUpdateBetweenSnapshots=False,
                    diagnosticFactsFedToAria=False,missionReady=False)
                new.append(proof)
            for n,row in enumerate(new,1):row['sequence']=n
            self.records[i]=new
    def receipt(self,index=0):
        return inspector.read_only_observer_receipt(q.load(self.paths[index]['job']),q.load(self.paths[index]['claim']),self.records[index])
    def proof(self,index=0,terminal=False):
        return [r for r in self.records[index] if r['kind']=='NativeReadOnlyObserverProof'][int(terminal)]
    def check_receipt(self,passed=False):
        result=self.receipt();self.assertEqual(passed,result['read_only_observers'],result)
        self.assertFalse(result['qualified']);self.assertFalse(result['queue_success_enabled']);return result
    def test_all_attempts_observer_only_profile_pending(self):
        self.add_proofs();result=inspector.inspect(self.root,self.materialize())
        self.assertEqual('NormalCompletedUnqualified',result['result'],result)
        self.assertNotIn('read_only_observers',result['admission']['missing'])
        self.assertIn('profile_before_restore',result['admission']['missing'])
        self.assertEqual(3,len(result['admission']['observer_receipts']))
        self.assertEqual(3,len(result['admission']['profile_receipts']))
        self.assertEqual(1,result['wins']);self.assertFalse(result['qualified']);self.assertFalse(result['queue_success_enabled'])
    def test_flag_off_existing_evidence_stays_missing(self):
        result=inspector.inspect(self.root,self.materialize())
        self.assertEqual('NormalCompletedUnqualified',result['result'],result)
        self.assertIn('read_only_observers',result['admission']['missing'])
        self.assertTrue(all(r['read_only_observers'] is False for r in result['admission']['observer_receipts']))
    def test_one_missing_among_three_attempts_blocks_whole_facet(self):
        self.add_proofs();self.records[1]=[r for r in self.records[1] if r['kind']!='NativeReadOnlyObserverProof']
        for n,row in enumerate(self.records[1],1):row['sequence']=n
        result=inspector.inspect(self.root,self.materialize())
        self.assertEqual('NormalCompletedUnqualified',result['result'],result)
        self.assertIn('read_only_observers',result['admission']['missing']);self.assertEqual(1,len(result['admission']['observer_errors']))
    def test_unsupported_never_hidden_by_two_positive_rows(self):
        self.add_proofs();row=copy.deepcopy(self.proof());row['kind']='NativeReadOnlyObserverProofUnsupported'
        row['facts']=dict(native_session_id='session-0',phase='Playing',reason='Unsupported component',read_only_observers=False)
        self.records[0].insert(4,row)
        for n,r in enumerate(self.records[0],1):r['sequence']=n
        self.check_receipt()
    def test_window_count_order_and_adjacency(self):
        self.add_proofs();original=copy.deepcopy(self.records[0])
        for mutation in range(4):
            with self.subTest(mutation=mutation):
                self.records[0]=copy.deepcopy(original)
                if mutation==0:self.records[0].remove(self.proof())
                if mutation==1:self.records[0].insert(4,copy.deepcopy(self.proof()))
                if mutation==2:self.proof()['facts']['phase']='Finished'
                if mutation==3:
                    diagnostic=copy.deepcopy(self.proof());diagnostic.update(kind='Other',facts={});self.records[0].insert(3,diagnostic)
                for n,row in enumerate(self.records[0],1):row['sequence']=n
                self.check_receipt()
    def test_numeric_flags_and_equality_refusals(self):
        self.add_proofs();proof=self.proof();original=copy.deepcopy(proof['facts'])
        cases=[('capturedEntityCount',True),('capturedEntityCount',0),('capturedEntityCount',6),('queriedRelevantEntityCount',-1),
            ('worldEntityCount',4),('worldEntityCount',2147483648),('capturedFrameBefore',True),('capturedFrameAfter',999),
            ('coverageComplete',False),('captureQueriesReadOnly',1),('worldUpdateBetweenSnapshots',True),
            ('diagnosticFactsFedToAria',True),('missionReady',True),('observerElapsedMilliseconds',True),
            ('observerElapsedMilliseconds',float('nan')),('observerElapsedMilliseconds',float('inf')),
            ('observerElapsedMilliseconds',-1),('observerElapsedMilliseconds',10**400),('status','MutationObserved'),('afterSha256','d'*64),
            ('beforeSha256','C'*64),('scope','OtherWindow'),('schema','NativeReadOnlyObserverWindow-v2')]
        for key,value in cases:
            with self.subTest(key=key,value=str(value)):
                proof['facts']=copy.deepcopy(original);proof['facts'][key]=value;self.check_receipt()
        proof['facts']=copy.deepcopy(original);proof['facts']['caller_success']=True;self.check_receipt()
    def test_exact_subject_and_source_build(self):
        self.add_proofs();original=copy.deepcopy(self.records[0])
        for mutation in range(7):
            with self.subTest(mutation=mutation):
                self.records[0]=copy.deepcopy(original)
                row=self.proof()
                if mutation==0:row['worker']='foreign'
                if mutation==1:row['attempt_id']='foreign'
                if mutation==2:row['facts']['native_session_id']='foreign'
                if mutation==3:row['facts']['native_world_sequence']='77'
                if mutation==4:row['facts']['catalog_id']='S005'
                if mutation==5:self.records[0][0]['facts']['source_sha256']='0'*64
                if mutation==6:self.records[0][2]['facts']['native_config_sha256']='0'*64
                self.check_receipt()
    def test_pinned_reader_stability_failure_blocks_any_admission(self):
        self.add_proofs();spec=self.materialize();old=inspector.Reader.stable
        def changed(reader):raise ValueError('Synthetic pinned artifact changed at final stability barrier')
        inspector.Reader.stable=changed
        try:result=inspector.inspect(self.root,spec)
        finally:inspector.Reader.stable=old
        self.assertEqual('Incomplete',result['result']);self.assertEqual(0,result['wins'])
        self.assertIn('read_only_observers',result['admission']['missing'])
        self.assertNotIn('observer_receipts',result['admission'])
    def test_later_incomplete_attempt_has_no_partial_credit(self):
        self.add_proofs();self.csvrows[2]['result']='Abort'
        result=inspector.inspect(self.root,self.materialize())
        self.assertEqual('Incomplete',result['result']);self.assertEqual(0,result['wins'])
        self.assertIn('read_only_observers',result['admission']['missing'])
        self.assertNotIn('observer_receipts',result['admission'])
    def test_world_sequence_uint64_refusals(self):
        self.add_proofs()
        for row in self.records[0]:
            if 'native_world_sequence' in row['facts']:row['facts']['native_world_sequence']='18446744073709551616'
        self.check_receipt()
    def test_typed_receipt_identity_splice_refusals(self):
        self.add_proofs();actual=inspector.read_only_observer_receipt
        for key,value in [('job_sha256','f'*64),('attempt_id','foreign'),('native_session_id','foreign'),
                ('native_world_sequence','77'),('native_config_sha256','f'*64),('candidate_sha256','f'*64),
                ('build_sha256','f'*64),('read_only_observers',1),('qualified',True),('queue_success_enabled',True)]:
            with self.subTest(key=key):
                def splice(job,claim,native):
                    receipt=actual(job,claim,native);receipt[key]=value;return receipt
                inspector.read_only_observer_receipt=splice
                try:result=inspector.inspect(self.root,self.materialize())
                finally:inspector.read_only_observer_receipt=actual
                self.assertEqual('NormalCompletedUnqualified',result['result'],result)
                self.assertIn('read_only_observers',result['admission']['missing'])
                self.assertFalse(result['qualified']);self.assertFalse(result['queue_success_enabled'])
    def test_zero_world_is_valid_and_cost_has_no_performance_budget(self):
        self.add_proofs();self.proof()['facts']['observerElapsedMilliseconds']=0
        self.proof(terminal=True)['facts']['observerElapsedMilliseconds']=100000
        result=self.check_receipt(True);self.assertEqual('0',result['native_world_sequence'])

if __name__=='__main__':unittest.main(verbosity=2)
