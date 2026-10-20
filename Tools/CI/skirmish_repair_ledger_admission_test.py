"""Constructed fixtures and corruption controls; no native gameplay evidence."""
import copy
import hashlib
import json
import os
import subprocess
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import skirmish_repair_ledger_admission as admission
from skirmish_construction_ledger_admission_test import Fixture as IdentityFixture
from skirmish_native_identity import from_native_census, content_identity, sha256
from skirmish_production_queue import fingerprint
from s005_windows_source_snapshot import candidate_hash


class Fixture(IdentityFixture):
    def state(self,**kw):
        s=dict(session_index=20,session_version=1,bank_index=11,bank_version=1,facility_index=30,facility_version=1,
            attempt_id='fixture-session',actor_stable_id='vehicle.1',facility_stable_id='facility.1',pad_id='pad.1',
            faction=1,request_id=1,setup_hash=42,phase='Working',paid=20,refund_due=0,refunded=0,
            duration_ticks=200000000,remaining_ticks=200000000,fraction_numerator=200,fraction_denominator=800,
            last_clock_seconds=0.0,fractional_ticks=0.0,incoming_damage=0,new_movement=0,
            accepted_position=[0.0,0.0,0.0],pad_position=[0.0,0.0,0.0],facility_position=[0.0,0.0,0.0],last_health=100,last_maximum=200)
        s.update(kw);return s

    def point(self):
        b={k:0 for k in admission.BANK};b.update(FactionId=1,Current=100,Capacity=100,Version=1)
        return dict(actor_index=40,actor_version=1,bank_index=11,bank_version=1,bank=b,state=None,
            health=dict(current=100,maximum=200),owner=dict(attempt_id='fixture-session',stable_id='vehicle.1',faction=1,is_structure=0,capacity_released=0))

    def transaction(self,op,a,b,p):
        f=dict(ledger_schema=1,scope=admission.SCOPE,ledger_sequence=len(self.transactions)+1,operation=op,
            before=copy.deepcopy(a),after=copy.deepcopy(b),policy_state=copy.deepcopy(p),**self.menu)
        f.update({k:False for k in admission.FLAGS});self.transactions.append(f);return f

    def debit(self,a=None,**kw):
        a=self.point() if a is None else copy.deepcopy(a);p=self.state(**kw)
        b=copy.deepcopy(a);b['state']=p;b['bank']=admission.bank_mutation(a['bank'],p['paid'])
        self.transaction('DebitCommitted',a,b,p);return copy.deepcopy(b)

    def complete(self,b):
        a=copy.deepcopy(b);p=copy.deepcopy(a['state']);p.update(remaining_ticks=0,last_clock_seconds=20.0)
        c=copy.deepcopy(a);c['state']=dict(p,phase='Completed');c['health']['current']+=50
        self.transaction('Completed',a,c,p);return c

    def cancel(self,b,partial=True,deferred=False,destroyed=False,request=False):
        a=copy.deepcopy(b);p=copy.deepcopy(a['state'])
        p.update(phase='Destroyed' if destroyed else 'Cancelled',refund_due=0 if destroyed else 15 if partial else 20,
            remaining_ticks=190000000 if partial else 200000000,last_clock_seconds=1.0 if partial else 0.0)
        if request:p['request_id']+=1
        if destroyed:
            a['health']['current']=0
            for k in admission.UNOBSERVED:p[k]=a['state'][k]
        if deferred:a['bank']['Current']=100
        c=copy.deepcopy(a);c['state']=copy.deepcopy(p)
        if destroyed:op='Destroyed'
        elif deferred:op='RefundDeferred';c['state']['phase']='AccountingFailure'
        else:op='RefundCommitted';c['bank']=admission.bank_mutation(a['bank'],p['refund_due'],True);c['state']['refunded']=p['refund_due']
        self.transaction(op,a,c,p);return c

    def retry(self,b):
        a=copy.deepcopy(b);a['bank']['Current']-=15;p=copy.deepcopy(a['state'])
        c=copy.deepcopy(a);c['bank']=admission.bank_mutation(a['bank'],15,True);c['state'].update(phase='Cancelled',refunded=15)
        self.transaction('RefundCommitted',a,c,p);return c

    def events(self):
        common=dict(ledger_schema=1,scope=admission.SCOPE,**{k:False for k in admission.FLAGS})
        facts=[('ContextCaptured',dict(source_sha256=self.job['candidate_sha256'],compiled_dll_manifest_sha256=self.job['build_sha256'])),
            ('NativeRepairLedgerStarted',copy.deepcopy(common)),('NormalMenuLaunchObserved',copy.deepcopy(self.menu))]
        facts += [('NativeRepairTransaction',copy.deepcopy(t)) for t in self.transactions]
        facts += [('NativeRepairLedgerClosed',dict(common,transactions=len(self.transactions),sink_owned=True,capture_failed=False,observer_faults=0)),
            ('RestoreStarting',{}),('ContextRestored',{})]
        return [dict(schema_version=1,sequence=i,job_sha256=fingerprint(self.job),attempt_id=self.claim['attempt_id'],worker=self.claim['worker'],
            frame=i,utc='2026-10-18T12:00:00+00:00',kind=k,facts=f) for i,(k,f) in enumerate(facts,1)]

    def validate(self,events=None):return admission.validate_repair_ledger(self.job,self.claim,self.events() if events is None else events)


class AdmissionTests(unittest.TestCase):
    def good(self,f):
        r=f.validate();self.assertEqual(r['result'],'ConsistentRecordedRepairBoundaries',r)
        for k in ('qualified','queue_success_enabled','paid_resources_qualified','whole_attempt_conservation','normal_mission_ready','progress_history_measured','unchanged_debt_retries_measured'):self.assertIs(r[k],False)
        return r
    def bad(self,f):self.assertEqual(f.validate()['result'],'MissingOrFailedAdmission',f.validate())

    def test_debit_completion_and_partial_work(self):
        f=Fixture();b=f.debit();self.assertEqual(self.good(f)['outstanding_working'],1)
        b['state'].update(remaining_ticks=150000000,last_clock_seconds=5.0);f.complete(b);self.good(f)
    def test_full_partial_cancel_and_explicit_request_increment(self):
        for partial in (False,True):
            for request in (False,True):
                f=Fixture();f.cancel(f.debit(),partial=partial,request=request);self.good(f)
    def test_debt_and_actual_retry_not_reward(self):
        f=Fixture();b=f.cancel(f.debit(),deferred=True);self.assertEqual(self.good(f)['outstanding_refund_debt'],15)
        f.retry(b);self.assertEqual(self.good(f)['outstanding_refund_debt'],0)
    def test_destroyed_working_and_debt(self):
        f=Fixture();f.cancel(f.debit(),destroyed=True);self.good(f)
        f=Fixture();b=f.cancel(f.debit(),deferred=True);p=dict(b['state'],phase='Destroyed',refund_due=0)
        c=copy.deepcopy(b);c['state']=p;f.transaction('Destroyed',b,c,p);self.good(f)
    def test_new_repair_after_settlement(self):
        f=Fixture();b=f.cancel(f.debit());f.debit(b,request_id=2,last_clock_seconds=1.0);self.good(f)
    def test_saturation_and_version_wrap(self):
        f=Fixture();a=f.point();a['bank'].update(Version=admission.U32,LifetimeSpent=admission.I32-1,LifetimeRepairSpent=admission.I32-1)
        b=f.debit(a);self.assertEqual(b['bank']['Version'],1);f.complete(b);self.good(f)
    def test_one_material_quote_and_no_refund(self):
        f=Fixture();a=f.point();a['health']['current']=199
        b=f.debit(a,paid=1,fraction_numerator=4,last_health=199,duration_ticks=4000000,remaining_ticks=4000000)
        p=dict(b['state'],phase='Cancelled',refund_due=0,remaining_ticks=3000000,last_clock_seconds=.1)
        c=copy.deepcopy(b);c['state']=p;f.transaction('CancelledNoRefund',b,c,p);self.good(f)
    def test_settled_actor_generation_cannot_rename_stable_id(self):
        f=Fixture();b=f.cancel(f.debit());b['owner']['stable_id']='vehicle.2'
        f.debit(b,request_id=2,actor_stable_id='vehicle.2',last_clock_seconds=1.0);self.bad(f)
    def test_settled_actor_generation_cannot_change_faction_or_bank(self):
        f=Fixture();b=f.cancel(f.debit());b['owner']['faction']=2;b['bank_index']=12;b['bank']['FactionId']=2
        f.debit(b,request_id=2,faction=2,bank_index=12,last_clock_seconds=1.0);self.bad(f)
    def test_prior_receipt_must_bind_actual_point_even_for_first_observed_debit(self):
        for key,value in [('actor_stable_id','foreign'),('faction',2),('attempt_id','foreign'),('session_version',2),('setup_hash',43),('bank_index',12)]:
            f=Fixture();b=f.cancel(f.debit());f.transactions=[];b['state'][key]=value
            f.debit(b,request_id=2,last_clock_seconds=1.0);self.bad(f)
    def test_new_repair_may_change_facility_positions_and_quote(self):
        f=Fixture();b=f.cancel(f.debit());b['health']['current']=199
        f.debit(b,request_id=2,facility_index=31,facility_version=2,facility_stable_id='facility.2',pad_id='pad.2',
            accepted_position=[1.0,2.0,3.0],pad_position=[1.0,0.0,3.0],facility_position=[4.0,5.0,6.0],
            fraction_numerator=4,paid=1,last_health=199,duration_ticks=4000000,remaining_ticks=4000000,last_clock_seconds=1.0)
        self.good(f)
    def test_completion_damage_cancels_instead_of_healing(self):
        f=Fixture();b=f.debit();b['health']['current']=90;f.complete(b)
        t=f.transactions[-1];t['policy_state']['last_health']=90;t['after']['state']['last_health']=90;self.bad(f)
    def test_completion_health_research_nearest_integer_rescale_is_valid(self):
        for current,maximum in [(150,300),(101,201),(67,133)]:
            f=Fixture();b=f.debit();b['health']=dict(current=current,maximum=maximum);f.complete(b)
            t=f.transactions[-1];t['policy_state'].update(last_health=current,last_maximum=maximum)
            t['after']['state'].update(last_health=current,last_maximum=maximum)
            t['after']['health']=dict(current=current+maximum//4,maximum=maximum);self.good(f)
    def test_completion_damage_uses_rescaled_percentage(self):
        f=Fixture();b=f.debit();b['health']=dict(current=149,maximum=300);f.complete(b)
        t=f.transactions[-1];t['policy_state'].update(last_health=149,last_maximum=300)
        t['after']['state'].update(last_health=149,last_maximum=300);t['after']['health']=dict(current=224,maximum=300);self.bad(f)

    def test_local_boundary_accrual_and_cancel_history(self):
        for k,v in [('remaining_ticks',1),('fractional_ticks',.5),('last_clock_seconds',999.0),('last_health',99),('incoming_damage',1)]:
            f=Fixture();f.cancel(f.debit());f.transactions[-1]['policy_state'][k]=v;self.bad(f)
    def test_completion_does_not_reset_seen_interrupt(self):
        f=Fixture();b=f.debit();b['state']['incoming_damage']=1;f.complete(b);self.bad(f)

    def test_wrong_arithmetic_and_extra_schema(self):
        for field,value in [('paid',19),('duration_ticks',199999999),('fraction_numerator',199),('request_id',2),('setup_hash',43),('faction',2),('refunded',1),('unexpected',0)]:
            f=Fixture();f.debit();f.transactions[0]['policy_state'][field]=value;self.bad(f)
    def test_each_bank_field_unauthorized_mutation(self):
        for k in admission.BANK:
            f=Fixture();f.debit();f.transactions[0]['after']['bank'][k]+=1;self.bad(f)
    def test_foreign_session_actor_bank_facility_generation(self):
        for k in admission.ENTITIES|admission.STRINGS:
            f=Fixture();b=f.debit();f.complete(b);v=f.transactions[-1]['policy_state'][k]
            f.transactions[-1]['policy_state'][k]=v+1 if type(v)is int else v+'x';self.bad(f)
    def test_stable_actor_rebind(self):
        f=Fixture();f.debit();f.debit();t=f.transactions[-1]
        for k in ('before','after'):t[k]['actor_version']=2
        self.bad(f)
    def test_disposition_without_observed_debit(self):
        f=Fixture();f.complete(f.debit());f.transactions.pop(0);f.transactions[0]['ledger_sequence']=1;self.bad(f)
    def test_duplicate_completion_refund_and_debit(self):
        for op in ('complete','cancel'):
            f=Fixture();getattr(f,op)(f.debit());f.transactions.append(copy.deepcopy(f.transactions[-1]));f.transactions[-1]['ledger_sequence']=3;self.bad(f)
        f=Fixture();b=f.debit();f.debit(b);self.bad(f)
    def test_unrecorded_disposition_or_receipt_erasure(self):
        for value in (None,dict(phase='Cancelled')):
            f=Fixture();b=f.debit();f.complete(b);f.transactions[-1]['before']['state']=value;self.bad(f)
    def test_unobserved_progress_regression(self):
        for k,v in [('remaining_ticks',200000001),('last_clock_seconds',-1.0),('fractional_ticks',1.0)]:
            f=Fixture();f.complete(f.debit());f.transactions[-1]['before']['state'][k]=v;self.bad(f)
    def test_possible_or_repeated_debt_rejected(self):
        f=Fixture();b=f.cancel(f.debit(),deferred=True);f.transactions[-1]['before']['bank']['Current']=80;f.transactions[-1]['after']['bank']['Current']=80;self.bad(f)
        f=Fixture();b=f.cancel(f.debit(),deferred=True);f.transaction('RefundDeferred',b,b,b['state']);self.bad(f)
    def test_refund_cannot_create_reward_or_currency(self):
        for key in ('LifetimeRewarded','LifetimeImported','LifetimeFabricated','Current','LifetimeSpent','LifetimeRepairSpent'):
            f=Fixture();f.cancel(f.debit());f.transactions[-1]['after']['bank'][key]+=1;self.bad(f)
    def test_bad_dto_types_numbers_and_missing_owner(self):
        for v in (True,float('nan'),float('inf'),-1,1.1):
            f=Fixture();f.debit();f.transactions[0]['policy_state']['paid']=v;self.bad(f)
        for key in ('owner','bank'):
            f=Fixture();f.debit();f.transactions[0]['before'][key]=None;self.bad(f)
        for v in (.1,float('nan'),float('inf'),1e300):
            f=Fixture();f.debit();f.transactions[0]['policy_state']['pad_position'][0]=v;self.bad(f)
    def test_exact_widened_float_position(self):
        import struct
        f=Fixture();p=f.state();p['accepted_position'][0]=struct.unpack('<f',struct.pack('<f',.1))[0]
        admission.state(p)
    def test_default_none_is_only_exact_zero_receipt(self):
        f=Fixture();a=f.point();s={k:0 for k in admission.STATE};s.update({k:'' for k in admission.STRINGS});s.update({k:[0,0,0] for k in admission.POSITIONS});s['phase']='None';a['state']=s
        f.debit(a);self.good(f);s['paid']=1;f=Fixture();a['state']=s;f.debit(a);self.bad(f)
    def test_envelope_and_scope_corruptions(self):
        for field,value in [('sequence',99),('attempt_id','foreign'),('job_sha256','f'*64),('worker','other'),('utc','2026-10-18T12:00:00')]:
            f=Fixture();f.debit();events=f.events();events[3][field]=value
            self.assertEqual(f.validate(events)['result'],'MissingOrFailedAdmission')
        for key in admission.FLAGS:
            f=Fixture();f.debit();f.transactions[0][key]=True;self.bad(f)
    def test_truncation_failure_and_unknown_event(self):
        for kind in ('NativeRepairLedgerFailure','NativeRepairUnknownOperation'):
            f=Fixture();f.debit();events=f.events();events[3]['kind']=kind
            self.assertEqual(f.validate(events)['result'],'MissingOrFailedAdmission')
        f=Fixture();f.debit();events=f.events()[:-1];self.assertEqual(f.validate(events)['result'],'MissingOrFailedAdmission')
    def test_duplicate_json_keys_and_nonfinite(self):
        for text in ('{"a":1,"a":2}','{"a":NaN}','{"a":Infinity}'):
            with self.assertRaises(ValueError):admission.parse(text)


class ArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name).resolve();self.f=Fixture();self.f.complete(self.f.debit())
        self.folder=self.root/'fixture-job'/self.f.claim['attempt_id'];self.folder.mkdir(parents=True)
        self.names={'job.json':'fixture-job/job.json','claim.json':'fixture-job/claim.json',
                    'immutable.json':'fixture-job/'+self.f.claim['attempt_id']+'/claim.json',
                    'events.jsonl':'fixture-job/'+self.f.claim['attempt_id']+'/adapter-native-events.jsonl'}
        (self.root/'Library/ScriptAssemblies').mkdir(parents=True)
        self.write('Library/ScriptAssemblies/fixture.dll','constructed binary bytes')
        self.write('source.cs','constructed policy bytes')
        self.f.job['source_manifest']=[self.ref('source.cs')]
        self.f.job.update(job_id='fixture-job',candidate_sha256=candidate_hash(self.f.job['source_manifest']),
                          build_sha256=fingerprint([self.ref('Library/ScriptAssemblies/fixture.dll')]))
        self.f.claim.update(job_sha256=fingerprint(self.f.job),artifact_directory=str(self.folder))
        self.write('store.json',json.dumps(dict(source_manifest=self.f.job['source_manifest'],candidate_sha256=self.f.job['candidate_sha256'],
                   build_manifest=[self.ref('Library/ScriptAssemblies/fixture.dll')],build_sha256=self.f.job['build_sha256'])))
        self.write('job.json',json.dumps(self.f.job));self.write('claim.json',json.dumps(self.f.claim));self.write('immutable.json',json.dumps(self.f.claim))
        self.write('events.jsonl',''.join(json.dumps(r)+'\n' for r in self.f.events()))
        self.descriptor=dict(schema_version=1,job=self.ref('job.json'),claim=self.ref('claim.json'),immutable_claim=self.ref('immutable.json'),native_events=self.ref('events.jsonl'),candidate_manifest=self.ref('store.json'))
        self.write('descriptor.json',json.dumps(self.descriptor))
        # Fixture-only policy pins; production CLI keeps the literal reviewed pins.
        self.patch=patch.object(admission,'POLICY_PINS',{'source.cs':self.ref('source.cs')['sha256']});self.patch.start();self.addCleanup(self.patch.stop)

    def file(self,name):return self.root/self.names.get(name,name)

    def write(self,name,text):
        self.file(name).write_text(text,encoding='utf-8')

    def ref(self,name):return dict(path=self.file(name).relative_to(self.root).as_posix(),sha256=hashlib.sha256(self.file(name).read_bytes()).hexdigest())

    def inspect(self):return admission.inspect(self.root,'descriptor.json')

    def refresh(self,key,name):
        self.descriptor[key]=self.ref(name);self.write('descriptor.json',json.dumps(self.descriptor))

    def directory_alias(self,alias,target):
        """Real in-fixture aliases; junction creation needs no Windows symlink privilege."""
        self.assertTrue(target.is_dir())
        self.assertTrue(target.resolve().is_relative_to(self.root))
        self.assertTrue(alias.parent.resolve().is_relative_to(self.root))
        self.assertNotEqual(alias,target)
        self.assertFalse(alias.exists())
        if os.name=='nt':
            subprocess.run(['cmd.exe','/d','/c','mklink','/J',str(alias),str(target)],
                           check=True,capture_output=True,text=True,timeout=15)
        else:
            alias.symlink_to(target,target_is_directory=True)
        def remove_alias():
            if os.name=='nt':
                # Remove the junction itself, never traverse its target.
                if alias.exists():os.rmdir(alias)
            elif alias.is_symlink():alias.unlink()
        self.addCleanup(remove_alias)
        return alias

    def test_pinned_constructed_file_admission(self):
        r=self.inspect();self.assertTrue(r['source_bytes_verified']);self.assertEqual(r['artifact_barrier'],'UnchangedPinnedArtifacts');self.assertFalse(r['paid_resources_qualified'])

    def test_mutated_artifact_or_source(self):
        for name in ('events.jsonl','job.json','source.cs','claim.json','immutable.json'):
            with self.subTest(name=name):
                original=self.file(name).read_bytes();self.file(name).write_bytes(original+b' ')
                with self.assertRaises(ValueError):self.inspect()
                self.file(name).write_bytes(original)

    def test_wrong_immutable_claim_even_rehashed(self):
        v=copy.deepcopy(self.f.claim);v['worker']='other';self.write('immutable.json',json.dumps(v));self.refresh('immutable_claim','immutable.json')
        with self.assertRaises(ValueError):self.inspect()

    def test_truncated_journal_even_rehashed(self):
        self.write('events.jsonl',self.file('events.jsonl').read_text().rstrip());self.refresh('native_events','events.jsonl')
        with self.assertRaises(ValueError):self.inspect()

    def test_literal_policy_pins_cannot_be_caller_replaced(self):
        with patch.object(admission,'POLICY_PINS',{'source.cs':'f'*64}):
            with self.assertRaises(ValueError):self.inspect()

    def test_symlink_traversal_and_escape(self):
        alias=self.directory_alias(self.root/'alias',self.folder)
        with self.assertRaisesRegex(ValueError,'symlink/junction/reparse'):
            admission.Reader(self.root).read(dict(path=str(alias/'adapter-native-events.jsonl'),sha256=self.ref('events.jsonl')['sha256']))
        for path in ('../events.jsonl','/private/tmp/no-such-artifact','//?/C:/events.jsonl'):
            with self.subTest(path=path), self.assertRaises((ValueError,OSError)):admission.Reader(self.root).read(dict(path=path,sha256=self.ref('events.jsonl')['sha256']))

    def test_final_barrier_rejects_change_after_validation(self):
        original=admission.validate_repair_ledger
        def mutate(*args):
            result=original(*args);self.write('events.jsonl','changed');return result
        with patch.object(admission,'validate_repair_ledger',side_effect=mutate):
            with self.assertRaises(ValueError):self.inspect()

    def test_final_barrier_rejects_reparse_replacement(self):
        original=admission.validate_repair_ledger
        def mutate(*args):
            result=original(*args)
            saved=self.folder.with_name('saved-attempt');self.folder.rename(saved)
            self.directory_alias(self.folder,saved)
            return result
        with patch.object(admission,'validate_repair_ledger',side_effect=mutate):
            with self.assertRaisesRegex(ValueError,'symlink/junction/reparse'):self.inspect()

    def test_build_bytes_and_derived_identity_fail_closed(self):
        self.write('Library/ScriptAssemblies/fixture.dll','modified compiled bytes')
        with self.assertRaises(ValueError):self.inspect()

    def test_candidate_hash_cannot_be_caller_only(self):
        self.f.job['candidate_sha256']='e'*64
        self.write('job.json',json.dumps(self.f.job));self.refresh('job','job.json')
        store=json.loads(self.file('store.json').read_text());store['candidate_sha256']='e'*64
        self.write('store.json',json.dumps(store));self.refresh('candidate_manifest','store.json')
        with self.assertRaises(ValueError):self.inspect()

    def test_rehashed_journal_outside_attempt_is_rejected(self):
        self.write('other.jsonl',self.file('events.jsonl').read_text());self.refresh('native_events','other.jsonl')
        with self.assertRaises(ValueError):self.inspect()

    def test_unknown_event_and_capture_fault_are_retained(self):
        for kind in ('NativeRepairLedgerFailure','NativeRepairUnknownOperation'):
            events=self.f.events();events[3]['kind']=kind
            self.assertEqual(self.f.validate(events)['result'],'MissingOrFailedAdmission')

    def test_final_barrier_rechecks_source_and_dll_bytes(self):
        original=admission.validate_repair_ledger
        for name in ('source.cs','Library/ScriptAssemblies/fixture.dll'):
            before=self.file(name).read_bytes()
            def mutate(*args):
                result=original(*args);self.write(name,'late mutation');return result
            with patch.object(admission,'validate_repair_ledger',side_effect=mutate):
                with self.assertRaises(ValueError):self.inspect()
            self.file(name).write_bytes(before)

    def test_large_source_streams_and_final_barrier_rechecks_it(self):
        path=self.root/'large.asset';h=hashlib.sha256();chunk=b'a'*65536
        with path.open('wb') as stream:
            for _ in range(257):stream.write(chunk);h.update(chunk)
        ref=dict(path='large.asset',sha256=h.hexdigest())
        reader=admission.Reader(self.root)
        reader.verify(ref);reader.stable()
        with self.assertRaises(ValueError):reader.blob(ref) # Large pins are not parsed artifacts.
        with path.open('r+b') as stream:stream.write(b'b')
        with self.assertRaises(ValueError):reader.stable()

    def test_empty_source_allowed_but_empty_artifact_or_dll_refused(self):
        self.write('empty.cs','');ref=self.ref('empty.cs');reader=admission.Reader(self.root)
        reader.verify(ref);reader.stable()
        with self.assertRaises(ValueError):reader.blob(ref)
        with self.assertRaises(ValueError):reader.verify(ref,allow_empty=False)

    def test_source_row_bound_covers_measured_project(self):
        # Baseline stat audit: 41,733 tracked source rows; journal row bound stays separate.
        self.assertGreaterEqual(admission.MAX_SOURCE_FILES,41733)
        self.assertEqual(admission.MAX_ROWS,25000)

    def test_bounded_oversized_file(self):
        with patch.object(admission,'MAX_BYTES',10):
            with self.assertRaises(ValueError):self.inspect()


if __name__=='__main__':unittest.main(verbosity=2)
