"""Constructed fixtures and corruption controls; no native gameplay evidence."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import skirmish_construction_ledger_admission as admission
from skirmish_native_identity import from_native_census, content_identity, sha256
from skirmish_production_queue import fingerprint
from s005_windows_source_snapshot import candidate_hash


class Fixture:
    def __init__(self):
        self.identity=from_native_census(catalog_id='S002',definition_id='skirmish.s002',definition_version=1,
            difficulty='Regular',size='Standard',seed=1001,native_locale='en',native_setup_hash_uint32=42,native_content_identity='constructed-content')
        self.job={k:self.identity[k] for k in ('catalog_id','definition_id','definition_version','difficulty','size','seed','locale')}
        self.job.update(candidate_sha256='a'*64,build_sha256='b'*64,setup_sha256=sha256(self.identity),
                        content_sha256=sha256(content_identity(self.identity)),source_manifest=[])
        self.claim=dict(job_sha256=fingerprint(self.job),attempt_id='attempt-'+'c'*32,worker='mac-constructed-fixture')
        self.menu=dict(native_setup_identity=self.identity,native_session_id='fixture-session',native_world_sequence='123',native_config_sha256='d'*64)
        self.transactions=[]

    def point(self,faction=1,**overrides):
        e={k:0 for k in admission.ECONOMY};e.update(FactionId=faction,Money=1000,MaterialsOnlyConstruction=0 if faction==1 else 1)
        b={k:0 for k in admission.BANK};b.update(FactionId=faction,Current=1000,Capacity=2000,Version=1)
        p=dict(bank_index=10+faction,bank_version=1,session_index=20,session_version=1,economy=e,bank=b,enemy_intent=None,request=None)
        for k,v in overrides.items():p[k]=v
        return p

    def transaction(self,operation,before,after,**kw):
        f=dict(ledger_schema=1,scope=admission.SCOPE,ledger_sequence=len(self.transactions)+1,operation=operation,
               before=copy.deepcopy(before),after=copy.deepcopy(after),transaction_owner_sequence=1 if operation.startswith('Player') else 0,
               transaction_id=1,credits_quoted=0,materials_quoted=0,prefab_name=None,origin_x=0,origin_y=0,
               rotated=False,committed_count=0,expected_count=0,actor=None,paid_resources_qualified=False,**self.menu)
        f.update(kw);self.transactions.append(f);return f

    def player(self,refund=False,placement=False,policy=0):
        a=self.point();a['economy']['MaterialsOnlyConstruction']=policy
        credits,materials=(40000,90) if policy==2 else (100,30)
        if policy==2:a['economy']['Money']=50000
        b=copy.deepcopy(a)
        b['economy']['Money']-=credits if policy==0 else 0
        amount=120 if policy==2 else materials
        b['bank'].update(Current=1000-amount,LifetimeSpent=amount,LifetimeConstructionSpent=amount,Version=2)
        self.transaction('PlayerReserve',a,b,credits_quoted=credits,materials_quoted=materials)
        if placement:
            self.transaction('PlayerPlacementRequest',b,b,credits_quoted=credits,materials_quoted=materials,prefab_name='barracks',origin_x=5,origin_y=6)
            self.transaction('PlayerPlacementOutcome',b,b,credits_quoted=credits,materials_quoted=materials,prefab_name='barracks',origin_x=5,origin_y=6,committed_count=0 if refund else 1,expected_count=1)
        c=copy.deepcopy(a) if refund else copy.deepcopy(b)
        if refund:c['bank']['Version']=3
        self.transaction('PlayerRollback' if refund else 'PlayerFinalize',b,c,credits_quoted=credits,materials_quoted=materials)
        return a,b,c

    def intent(self,**kw):
        v=dict(version=1,session='fixture-session',phase=1,action='build.barracks',pad='pad.1',building='barracks',
               origin_x=5,origin_y=6,width=2,depth=3,paid=200,request_id=7,candidate=0,desired_count=1,result_stable_id='',retry_after=0)
        v.update(kw);return v

    def request(self,**kw):
        v=dict(id=7,kind=4,status=0,building='barracks',faction=2,materials=200,preferred_x=5,preferred_y=6,
               actual_x=0,actual_y=0,actual_width=0,actual_depth=0,runtime_id=0,plan_index=20,plan_version=1,
               authored_grant=0,allow_nonbuildable=0,require_origin=1)
        v.update(kw);return v

    def enemy(self,outcome='refund'):
        a=self.point(2);b=copy.deepcopy(a)
        b['bank'].update(Current=800,LifetimeSpent=200,LifetimeConstructionSpent=200,Version=2)
        b['enemy_intent']=self.intent();b['request']=self.request()
        self.transaction('EnemyRequest',a,b,transaction_id=7,materials_quoted=200,prefab_name='barracks',origin_x=5,origin_y=6)
        if outcome=='pending':return a,b,b
        if outcome=='refund':
            c=copy.deepcopy(b);c['request']['status']=2
            d=copy.deepcopy(c);d['enemy_intent']['phase']=4
            self.transaction('EnemyRefundDebt',c,d,transaction_id=7,materials_quoted=200)
            e=copy.deepcopy(d);e['request']=None
            f=copy.deepcopy(e);f['enemy_intent'].update(phase=5,paid=0,retry_after=10)
            f['bank'].update(Current=1000,LifetimeSpent=0,LifetimeConstructionSpent=0,Version=3)
            self.transaction('EnemyRefund',e,f,transaction_id=7)
            return a,b,f
        if outcome=='reenqueue':
            c=copy.deepcopy(b);c['request']=None
            d=copy.deepcopy(c);d['enemy_intent']['request_id']=8;d['request']=self.request(id=8)
            self.transaction('EnemyReenqueue',c,d,transaction_id=8,materials_quoted=200)
            return a,b,d
        c=copy.deepcopy(b);c['request'].update(status=1,actual_x=5,actual_y=6,actual_width=2,actual_depth=3,runtime_id=22)
        d=copy.deepcopy(c);d['enemy_intent'].update(phase=3,result_stable_id='constructed.22')
        actor=dict(index=30,version=1,session='fixture-session',stable_id='constructed.22',faction=2,is_structure=1,shared_actor=True,source_key='barracks')
        self.transaction('EnemyDelivered',c,d,transaction_id=7,materials_quoted=200,prefab_name='barracks',origin_x=5,origin_y=6,committed_count=1,expected_count=1,actor=actor)
        return a,b,d

    def events(self):
        facts=[('ContextCaptured',dict(source_sha256=self.job['candidate_sha256'],compiled_dll_manifest_sha256=self.job['build_sha256'])),
               ('NativeConstructionLedgerStarted',dict(ledger_schema=1,scope=admission.SCOPE,paid_resources_qualified=False)),
               ('NormalMenuLaunchObserved',copy.deepcopy(self.menu))]
        facts += [('NativeConstructionTransaction',copy.deepcopy(f)) for f in self.transactions]
        facts += [('NativeConstructionLedgerClosed',dict(ledger_schema=1,scope=admission.SCOPE,transactions=len(self.transactions),capture_failed=False,
                   sink_owned=True,observer_faults=0,uncovered_operations=admission.UNCOVERED,paid_resources_qualified=False)),
                  ('RestoreStarting',{}),('ContextRestored',{})]
        return [dict(schema_version=1,sequence=i,job_sha256=fingerprint(self.job),attempt_id=self.claim['attempt_id'],worker=self.claim['worker'],
                     frame=i,utc='2026-10-18T12:00:00+00:00',kind=kind,facts=facts) for i,(kind,facts) in enumerate(facts,1)]

    def validate(self,events=None):
        return admission.validate_construction_ledger(self.job,self.claim,self.events() if events is None else events)


class AdmissionTests(unittest.TestCase):
    def good(self,f):
        r=f.validate();self.assertEqual(r['result'],'ConsistentRecordedConstructionBoundaries',r)
        self.assertFalse(r['qualified']);self.assertFalse(r['paid_resources_qualified']);self.assertFalse(r['queue_success_enabled'])
        self.assertFalse(r['whole_attempt_conservation']);return r

    def bad(self,f):
        r=f.validate();self.assertEqual(r['result'],'MissingOrFailedAdmission',r);return r

    def test_player_finalize_refund_and_all_cost_policies(self):
        for policy in (0,1,2):
            for refund in (False,True):
                with self.subTest(policy=policy,refund=refund):
                    f=Fixture();f.player(refund=refund,placement=True,policy=policy);self.good(f)

    def test_enemy_refund_delivered_reenqueue_pending(self):
        for op in ('refund','delivered','reenqueue','pending'):
            with self.subTest(op=op):
                f=Fixture();f.enemy(op);r=self.good(f)
                self.assertEqual(r['enemy_outstanding_materials'],200 if op in ('pending','reenqueue') else 0)

    def test_empty_capture_does_not_invent_coverage(self):
        r=self.good(Fixture());self.assertEqual(r['transactions'],0)

    def test_interleaved_other_resource_origins_are_not_global_conservation(self):
        f=Fixture();_,b,_=f.player();f.transactions[-1]['before']['bank']['Current']+=50
        f.transactions[-1]['after']['bank']['Current']+=50
        self.good(f)

    def test_player_duplicate_settlement_or_reservation(self):
        for source in (0,-1):
            f=Fixture();f.player();v=copy.deepcopy(f.transactions[source]);v['ledger_sequence']=3;f.transactions.append(v);self.bad(f)

    def test_player_placement_must_match_and_settle(self):
        for mutate in (lambda f:f.transactions.pop(2),lambda f:f.transactions[2].update(origin_x=99),
                       lambda f:f.transactions[-1].update(operation='PlayerRollback'),lambda f:f.transactions.pop()):
            f=Fixture();f.player(placement=True);mutate(f);self.bad(f)

    def test_deferred_refund_requires_real_failure(self):
        f=Fixture();f.player();f.transactions[-1]['operation']='PlayerRollbackDeferred';self.bad(f)
        f=Fixture();_,b,_=f.player()
        before=copy.deepcopy(b);before['bank']['Current']=2000
        f.transactions.pop();f.transaction('PlayerRollbackDeferred',before,before,credits_quoted=100,materials_quoted=30)
        after=copy.deepcopy(b);after['economy']['Money']=1000;after['bank'].update(Current=1000,LifetimeSpent=0,LifetimeConstructionSpent=0,Version=3)
        f.transaction('PlayerRollback',b,after,credits_quoted=100,materials_quoted=30);self.good(f)

    def test_corrupted_debit_refund_counters_and_version(self):
        for key in ('Current','LifetimeSpent','LifetimeConstructionSpent','LifetimeImported','Version'):
            for who in ('player','enemy'):
                with self.subTest(key=key,who=who):
                    f=Fixture();getattr(f,who)();f.transactions[0]['after']['bank'][key]+=1;self.bad(f)

    def test_saturated_counters_and_wrapped_version(self):
        f=Fixture();f.player()
        for row in f.transactions:
            for side in ('before','after'):
                row[side]['bank']['LifetimeSpent']=admission.I32
                row[side]['bank']['LifetimeConstructionSpent']=admission.I32
        f.transactions[0]['before']['bank']['Version']=admission.U32
        for row in f.transactions:row['after']['bank']['Version']=1
        f.transactions[1]['before']['bank']['Version']=1
        self.good(f)

    def test_enemy_wrong_actor_request_or_geometry(self):
        for key,value in [('session','other'),('faction',1),('stable_id','other'),('source_key','tower'),('shared_actor',False)]:
            f=Fixture();f.enemy('delivered');f.transactions[-1]['actor'][key]=value;self.bad(f)
        f=Fixture();f.enemy('delivered');f.transactions[-1]['before']['request']['actual_x']=99;self.bad(f)
        f=Fixture();f.enemy('delivered');f.transactions[-1]['before']['request']['authored_grant']=1;self.bad(f)

    def test_reenqueue_cannot_charge_twice(self):
        f=Fixture();f.enemy('reenqueue');f.transactions[-1]['after']['bank']['Current']-=200;self.bad(f)

    def test_enemy_duplicate_refund_and_history_splice(self):
        f=Fixture();f.enemy();v=copy.deepcopy(f.transactions[-1]);v['ledger_sequence']=4;f.transactions.append(v);self.bad(f)
        f=Fixture();f.enemy();f.transactions[-1]['before']['enemy_intent']['paid']=201;self.bad(f)

    def test_debt_replacement_spends_then_refunds_once(self):
        f=Fixture();f.enemy();f.transactions.pop() # retain request and debt
        old=copy.deepcopy(f.transactions[-1]['after']);old['request']=None
        old['bank']['Current']=2000 # independent fabrication filled the bank
        new=copy.deepcopy(old);new['enemy_intent']=f.intent(request_id=8,paid=300,action='build.next');new['request']=f.request(id=8,materials=300)
        new['bank'].update(Current=1900,LifetimeSpent=300,LifetimeConstructionSpent=300,Version=4)
        f.transaction('EnemyRequest',old,new,transaction_id=8,materials_quoted=300,prefab_name='barracks',origin_x=5,origin_y=6)
        self.good(f)
        f.transactions[-1]['after']['bank']['Version']=3;self.bad(f)

    def test_policy_cost_change_and_unsupported_version(self):
        f=Fixture();f.player(policy=2);f.transactions[0]['credits_quoted']=40001;self.bad(f)
        f=Fixture();f.player();f.transactions[0]['before']['economy']['MaterialsOnlyConstruction']=3;self.bad(f)

    def test_job_claim_setup_world_config_splices(self):
        for key,value in [('native_session_id','other'),('native_world_sequence','124'),('native_config_sha256','e'*64),('ledger_sequence',2)]:
            f=Fixture();f.player();f.transactions[0][key]=value;self.bad(f)
        f=Fixture();f.player();f.transactions[0]['native_setup_identity']=copy.deepcopy(f.identity);f.transactions[0]['native_setup_identity']['seed']=2;self.bad(f)
        f=Fixture();f.claim['job_sha256']='f'*64;self.bad(f)

    def test_envelope_closure_faults_and_order(self):
        for kind,key,value in [('NativeConstructionLedgerClosed','capture_failed',True),('NativeConstructionLedgerClosed','observer_faults',1),
                               ('NativeConstructionLedgerClosed','sink_owned',False),('NativeConstructionLedgerClosed','transactions',999),
                               ('NativeConstructionLedgerStarted','ledger_schema',2)]:
            f=Fixture();f.player();events=f.events();next(r for r in events if r['kind']==kind)['facts'][key]=value
            self.assertEqual(f.validate(events)['result'],'MissingOrFailedAdmission')
        for key,value in [('worker','other'),('attempt_id','attempt-'+'d'*32),('sequence',True),('frame',-1),('utc','not-a-time'),('schema_version',True)]:
            f=Fixture();f.player();events=f.events();events[3][key]=value
            self.assertEqual(f.validate(events)['result'],'MissingOrFailedAdmission')
        f=Fixture();self.assertEqual(f.validate(f.events()[:-1])['result'],'MissingOrFailedAdmission')

    def test_strict_types_nonfinite_and_unknown_fields(self):
        for path,value in [(('before','bank','Current'),True),(('before','economy','Fuel'),float('inf')),
                           (('after','bank','Version'),2.0),(('before','economy','Money'),-1)]:
            f=Fixture();f.player();a,b,c=path;f.transactions[0][a][b][c]=value;self.bad(f)
        f=Fixture();f.player();f.transactions[0]['unknown']=1;self.bad(f)
        for text in ('{"a":1,"a":2}','{"a":NaN}','{"a":Infinity}'):
            with self.assertRaises(ValueError):admission.parse(text)


class ArtifactTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name).resolve();self.f=Fixture();self.f.player()
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
        (self.root/'alias').symlink_to(self.file('events.jsonl'))
        for path in ('alias','../events.jsonl','/private/tmp/no-such-artifact','//?/C:/events.jsonl'):
            with self.subTest(path=path), self.assertRaises((ValueError,OSError)):admission.Reader(self.root).read(dict(path=path,sha256=self.ref('events.jsonl')['sha256']))

    def test_final_barrier_rejects_change_after_validation(self):
        original=admission.validate_construction_ledger
        def mutate(*args):
            result=original(*args);self.write('events.jsonl','changed');return result
        with patch.object(admission,'validate_construction_ledger',side_effect=mutate):
            with self.assertRaises(ValueError):self.inspect()

    def test_final_barrier_rejects_reparse_replacement(self):
        original=admission.validate_construction_ledger
        def mutate(*args):
            result=original(*args)
            p=self.file('events.jsonl');p.rename(self.root/'saved.jsonl');p.symlink_to(self.root/'saved.jsonl');return result
        with patch.object(admission,'validate_construction_ledger',side_effect=mutate):
            with self.assertRaises(ValueError):self.inspect()

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
        for kind in ('NativeConstructionLedgerFailure','NativeConstructionUnknownOperation'):
            events=self.f.events();events[3]['kind']=kind
            self.assertEqual(self.f.validate(events)['result'],'MissingOrFailedAdmission')

    def test_final_barrier_rechecks_source_and_dll_bytes(self):
        original=admission.validate_construction_ledger
        for name in ('source.cs','Library/ScriptAssemblies/fixture.dll'):
            before=self.file(name).read_bytes()
            def mutate(*args):
                result=original(*args);self.write(name,'late mutation');return result
            with patch.object(admission,'validate_construction_ledger',side_effect=mutate):
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

    def test_streamed_pin_file_and_total_limits(self):
        ref=self.ref('source.cs')
        with self.assertRaises(ValueError):admission.Reader(self.root).verify(ref,byte_limit=2)
        with patch.object(admission,'MAX_PIN_TOTAL_BYTES',2):
            with self.assertRaises(ValueError):admission.Reader(self.root).verify(ref)

    def test_source_row_bound_covers_measured_project(self):
        # Baseline stat audit: 41,733 tracked source rows; journal row bound stays separate.
        self.assertGreaterEqual(admission.MAX_SOURCE_FILES,41733)
        self.assertEqual(admission.MAX_ROWS,25000)

    def test_bounded_oversized_file(self):
        with patch.object(admission,'MAX_BYTES',10):
            with self.assertRaises(ValueError):self.inspect()


if __name__=='__main__':unittest.main(verbosity=2)
