"""Constructed recorded-boundary controls. No native run, Touch or gameplay credit."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import skirmish_paid_transport_join_admission as a
from skirmish_native_identity import from_native_census, content_identity, sha256


class Fixture:
    def __init__(self):
        self.identity=from_native_census(catalog_id='S005',definition_id='skirmish.s005',definition_version=1,
            difficulty='Regular',size='Standard',seed=155926,native_locale='en',native_setup_hash_uint32=42,native_content_identity='constructed-content')
        self.job={k:self.identity[k] for k in ('catalog_id','definition_id','definition_version','difficulty','size','seed','locale')}
        self.job.update(control_mode='manual',candidate_sha256='a'*64,build_sha256='b'*64,
            setup_sha256=sha256(self.identity),content_sha256=sha256(content_identity(self.identity)),source_manifest=[])
        self.claim=dict(job_sha256=a.fingerprint(self.job),attempt_id='attempt-'+'c'*32,worker='constructed-only')
        self.subject=dict(native_setup_identity=self.identity,native_session_id='fixture-session',native_world_sequence='123',native_config_sha256='d'*64)
        bank={k:0 for k in a.BANK}; bank.update(FactionId=1,Current=1000,Capacity=2000,Version=1)
        point=dict(faction=1,bank_index=11,bank_version=1,materials=1000,fuel=100,bank=bank,reservation=None)
        self.tx=[]
        queued=copy.deepcopy(point); queued.update(materials=560,bank=a.production_debit(bank,440),
            reservation=dict(id=7,role='TransportPlane',faction=1,phase='Reserved',producer=0,paid=440,members=1,delivered=0,remaining=1,supply=5))
        self.add('Queue',point,queued,decision=dict(materials=440,fuel=0,refund=0,accepted=True,reason='None',reservation=7))
        bound=copy.deepcopy(queued); bound['reservation']['producer']=34; self.add('BindProducer',queued,bound)
        started=copy.deepcopy(bound); started['reservation']['phase']='Producing'; self.add('StartNativeQueue',bound,started)
        delivered=copy.deepcopy(started); delivered['reservation'].update(phase='Live',delivered=1)
        actor=dict(index=33,version=2,session='fixture-session',stable_id='production.7.0',faction=1,reservation=7,
            shared_actor=True,is_structure=0,capacity_released=0,role='TransportPlane',source_key='Unit_Veh_Plane_Transport')
        self.add('Deliver',started,delivered,actor=actor)
        base=dict(attempt='fixture-session',catalog='S005',definition='skirmish.s005',seed=155926,timeScale=1,normalBudget=True,
            fullMissionCertified=False,unexpected=0,interventions=0,violations=0,producerRuntimeId=34,airportIndex=22,
            airportVersion=3,airportStable='constructed.34',normalMenu=True)
        self.lineage=[dict(base,stage='PaidAirportHandoff',frame=3),dict(base,stage='PaidPlaneDeliveredAtHome',frame=8,
            reservation=7,role='TransportPlane',phase='Live',paid=440,members=1,delivered=1,remaining=1,actorIndex=33,
            actorVersion=2,stable='production.7.0',actorNativeShared=True,actorHp=100)]

    def add(self,op,before,after,decision=None,actor=None):
        self.tx.append(dict(ledger_schema=1,scope=a.SCOPE,ledger_sequence=len(self.tx)+1,operation=op,member_ordinal=-1,
            before=copy.deepcopy(before),after=copy.deepcopy(after),decision=decision,actor=actor,paid_resources_qualified=False,**copy.deepcopy(self.subject)))

    def events(self):
        common=dict(ledger_schema=1,scope=a.SCOPE,paid_resources_qualified=False)
        rows=[('ContextCaptured',dict(source_sha256=self.job['candidate_sha256'],compiled_dll_manifest_sha256=self.job['build_sha256'])),
            ('NativeProductionLedgerStarted',dict(common)),('NormalMenuLaunchObserved',copy.deepcopy(self.subject))]
        rows += [('NativeProductionTransaction',copy.deepcopy(t)) for t in self.tx]
        rows += [('NativeProductionLedgerClosed',dict(common,transactions=len(self.tx),capture_failed=False,observer_faults=0,sink_owned=True,uncovered_operations=a.UNCOVERED)),('RestoreStarting',{}),('ContextRestored',{})]
        return [dict(schema_version=1,sequence=i,job_sha256=a.fingerprint(self.job),attempt_id=self.claim['attempt_id'],worker=self.claim['worker'],
            frame=i,utc='2026-10-18T12:00:00+00:00',kind=k,facts=v) for i,(k,v) in enumerate(rows,1)]

    def validate(self,events=None,lineage=None):
        return a.validate(self.job,self.claim,self.events() if events is None else events,self.lineage if lineage is None else lineage)


class Tests(unittest.TestCase):
    def reject(self,f,events=None,lineage=None):
        result=f.validate(events,lineage); self.assertFalse(result['recorded_join_consistent'],result)

    def test_consistent_constructed_chain_is_only_recorded_math(self):
        r=Fixture().validate(); self.assertTrue(r['recorded_join_consistent'],r)
        for key in ('qualified','paid_resources_qualified','normal_mission_ready','queue_success_enabled','whole_attempt_conservation','player_device_accepted','source_bytes_verified','build_bytes_verified','aircraft_fuel_spend_attributed'): self.assertIs(r[key],False)
        self.assertEqual(r['normal_720_credit'],0)

    def test_aggregate_spend_without_atomic_debit_is_rejected(self):
        f=Fixture(); f.tx[0]['before']['bank'].update(LifetimeSpent=900,LifetimeProductionSpent=900)
        f.tx[0]['after']['bank']=copy.deepcopy(f.tx[0]['before']['bank']); f.tx[0]['after']['materials']=1000; self.reject(f)

    def test_other_cost_family_cannot_pay_for_plane(self):
        f=Fixture(); f.tx[0]['after']['bank'].update(LifetimeProductionSpent=0,LifetimeConstructionSpent=440); self.reject(f)

    def test_wrong_bank_generation_even_if_both_sides_match(self):
        f=Fixture()
        for p in ('before','after'): f.tx[1][p]['bank_version']=2
        self.reject(f)

    def test_wrong_actor_generation_and_replayed_stable_id(self):
        for key,value in [('version',3),('index',44),('stable_id','production.8.0'),('session','other'),('reservation',8),('faction',2),('source_key','Unit_Veh_Plane_Strike'),('role','Strike'),('is_structure',1),('capacity_released',1)]:
            f=Fixture(); f.tx[3]['actor'][key]=value
            with self.subTest(key=key): self.reject(f)

    def test_wrong_airport_binding_through_entire_production_suffix(self):
        f=Fixture()
        for row in f.tx[1:]:
            for side in ('before','after'):
                if row[side]['reservation']['producer']: row[side]['reservation']['producer']=35
        self.reject(f)

    def test_missing_reordered_and_duplicate_lifecycle_boundaries(self):
        for mode in ('missing','reordered','duplicate'):
            f=Fixture()
            if mode=='missing': f.tx.pop(2)
            elif mode=='reordered': f.tx[1],f.tx[2]=f.tx[2],f.tx[1]
            else: f.tx.insert(2,copy.deepcopy(f.tx[1]))
            for n,row in enumerate(f.tx,1): row['ledger_sequence']=n
            with self.subTest(mode=mode): self.reject(f)

    def test_cancel_before_delivery_is_not_cherry_picked_away(self):
        f=Fixture(); c=copy.deepcopy(f.tx[1]); c['operation']='Cancel'; f.tx.insert(1,c)
        for n,row in enumerate(f.tx,1): row['ledger_sequence']=n
        self.reject(f)

    def test_non_debit_stage_cannot_spend_or_refund(self):
        for stage in (1,2,3):
            f=Fixture(); f.tx[stage]['after']['materials']-=1; f.tx[stage]['after']['bank']['Current']-=1
            with self.subTest(stage=stage): self.reject(f)

    def test_actor_does_not_exist_at_queue_boundary(self):
        f=Fixture(); f.tx[0]['actor']=copy.deepcopy(f.tx[3]['actor']); self.reject(f)

    def test_closed_capture_must_be_fault_free_owned_and_complete(self):
        for key,value in [('observer_faults',1),('capture_failed',True),('sink_owned',False),('transactions',3),('uncovered_operations',[])]:
            f=Fixture(); events=f.events(); next(r for r in events if r['kind']=='NativeProductionLedgerClosed')['facts'][key]=value
            with self.subTest(key=key): self.reject(f,events)

    def test_envelope_and_native_subject_splices(self):
        for key,value in [('native_session_id','other'),('native_world_sequence','124'),('native_config_sha256','e'*64)]:
            f=Fixture(); f.tx[2][key]=value
            with self.subTest(key=key): self.reject(f)
        f=Fixture(); events=f.events(); events[4]['job_sha256']='f'*64; self.reject(f,events)

    def test_equal_numeric_setup_value_does_not_bypass_native_type_contract(self):
        f=Fixture(); f.tx[2]['native_setup_identity']['definition_version']=1.0; self.reject(f)

    def test_boolean_as_integer_and_float_as_integer_rejected(self):
        for value in (True,440.0,'440'):
            f=Fixture(); f.tx[0]['decision']['materials']=value
            with self.subTest(value=value): self.reject(f)
        f=Fixture(); f.lineage[1]['actorHp']=True; self.reject(f)
        f=Fixture(); f.lineage[1]['timeScale']=True; self.reject(f)

    def test_fabrication_between_atomic_boundaries_is_not_false_spending(self):
        f=Fixture()
        for row in f.tx[1:]:
            for side in ('before','after'):
                row[side]['materials']+=20; row[side]['bank'].update(Current=580,LifetimeFabricated=20,Version=3)
        self.assertTrue(f.validate()['recorded_join_consistent'])

    def test_counter_saturation_and_version_wrap_match_existing_bank_owner(self):
        f=Fixture(); b=f.tx[0]['before']['bank']; b.update(LifetimeSpent=a.I32-10,LifetimeProductionSpent=a.I32-10,Version=a.U32)
        after=a.production_debit(b,440)
        for i,row in enumerate(f.tx):
            for side in ('before','after'):
                if i or side=='after': row[side]['bank']=copy.deepcopy(after)
        self.assertTrue(f.validate()['recorded_join_consistent'])

    def test_explicit_failure_and_duplicate_lineage_are_rejected(self):
        f=Fixture(); self.reject(f,lineage=f.lineage+[dict(stage='Failure')])
        f=Fixture(); self.reject(f,lineage=f.lineage+[copy.deepcopy(f.lineage[1])])

    def test_late_death_is_preserved_but_never_promoted_to_full_lifecycle(self):
        f=Fixture(); tail=copy.deepcopy(f.tx[-1]); tail.update(operation='DeathRelease',ledger_sequence=5)
        f.tx.append(tail); f.lineage[1]['frame']=9
        r=f.validate(); self.assertTrue(r['recorded_join_consistent'],r)
        self.assertEqual(r['observed_post_delivery_rows'],1); self.assertFalse(r['whole_attempt_conservation']); self.assertFalse(r['qualified'])

    def test_second_acquisition_cannot_hide_after_first_delivery(self):
        f=Fixture(); tail=copy.deepcopy(f.tx[0]); tail['ledger_sequence']=5; f.tx.append(tail); self.reject(f)

    def test_foreign_actor_in_excluded_tail_is_still_a_subject_splice(self):
        f=Fixture(); tail=copy.deepcopy(f.tx[3]); tail.update(operation='DeathRelease',ledger_sequence=5)
        tail['actor']['session']='foreign-session'; f.tx.append(tail); self.reject(f)

    def test_uint32_reservation_identity_is_not_truncated_to_int32(self):
        f=Fixture(); target=a.U32
        for row in f.tx:
            for side in ('before','after'):
                if row[side]['reservation'] is not None: row[side]['reservation']['id']=target
            if row['decision'] is not None: row['decision']['reservation']=target
            if row['actor'] is not None: row['actor'].update(reservation=target,stable_id='production.'+str(target)+'.0')
        f.lineage[1].update(reservation=target,stable='production.'+str(target)+'.0')
        self.assertTrue(f.validate()['recorded_join_consistent'])

    def test_bank_entity_slot_cannot_rebind_to_another_faction_generation(self):
        f=Fixture(); row=copy.deepcopy(f.tx[1]); row.update(ledger_sequence=5,operation='StartQueued')
        for side in ('before','after'):
            p=row[side]; p.update(faction=2,bank_version=2); p['bank']['FactionId']=2
            p['reservation'].update(faction=2,id=8,role='Tanker')
        f.tx.append(row); self.reject(f)


class ArtifactFixture:
    def __init__(self,root):
        self.root=Path(root).resolve(); self.f=Fixture()
        self.source='Assets/producer.cs'; self.write(self.source,b'constructed source bytes, not a native producer')
        dll='Library/ScriptAssemblies/Game.Editor.dll'; self.write(dll,b'constructed DLL bytes, not a native assembly')
        self.pins={self.source:self.digest(self.root/self.source)}
        sources=[dict(path=self.source,sha256=self.pins[self.source])]
        builds=[dict(path=dll,sha256=self.digest(self.root/dll))]
        self.f.job.update(job_id='constructed-paid-join',source_manifest=sources,candidate_sha256=a.candidate_hash(sources),build_sha256=a.fingerprint(builds))
        directory=self.root/'Logs/SkirmishProduction/Queue'/self.f.job['job_id']
        self.folder=directory/self.f.claim['attempt_id']; self.folder.mkdir(parents=True)
        self.f.claim.update(job_sha256=a.fingerprint(self.f.job),token='e'*32,started_utc='2026-10-18T12:00:00+00:00',artifact_directory=str(self.folder))
        self.jp=directory/'job.json'; self.cp=directory/'claim.json'; self.ip=self.folder/'claim.json'
        self.ep=self.folder/'adapter-native-events.jsonl'; self.lp=self.folder/'paid-plane.jsonl'; self.store=self.folder/'candidate.json'
        self.obj(self.jp,self.f.job); self.obj(self.cp,self.f.claim); self.obj(self.ip,self.f.claim)
        self.write(self.ep,''.join(json.dumps(r)+'\n' for r in self.f.events()).encode())
        self.write(self.lp,''.join(json.dumps(r)+'\n' for r in self.f.lineage).encode())
        self.obj(self.store,dict(source_manifest=sources,candidate_sha256=self.f.job['candidate_sha256'],build_manifest=builds,build_sha256=self.f.job['build_sha256']))
        self.descriptor=self.folder/'descriptor.json'
        self.refresh()

    @staticmethod
    def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()

    def write(self,path,data):
        p=Path(path); p=p if p.is_absolute() else self.root/p
        p.parent.mkdir(parents=True,exist_ok=True); p.write_bytes(data)

    def obj(self,path,value): self.write(path,(json.dumps(value,allow_nan=False)+'\n').encode())

    def ref(self,path): return dict(path=str(path),sha256=self.digest(path))

    def refresh(self):
        self.obj(self.descriptor,dict(schema_version=1,job=self.ref(self.jp),claim=self.ref(self.cp),immutable_claim=self.ref(self.ip),native_events=self.ref(self.ep),candidate_manifest=self.ref(self.store),lineage=self.ref(self.lp)))

    def inspect(self):
        with patch.object(a,'POLICY_PINS',self.pins): return a.inspect(self.root,self.descriptor)


class ArtifactTests(unittest.TestCase):
    def test_real_bounded_files_and_claim_copy_are_checked_without_native_credit(self):
        with tempfile.TemporaryDirectory() as td:
            f=ArtifactFixture(td); r=f.inspect(); self.assertTrue(r['recorded_join_consistent'],r)
            self.assertTrue(r['source_bytes_verified']); self.assertTrue(r['build_bytes_verified'])
            self.assertFalse(r['native_capture_authenticated']); self.assertFalse(r['source_manifest_completeness_measured']); self.assertFalse(r['qualified'])

    def test_immutable_claim_and_owned_lineage_path_splices_rejected(self):
        with tempfile.TemporaryDirectory() as td:
            f=ArtifactFixture(td); claim=dict(f.f.claim,worker='other'); f.obj(f.ip,claim); f.refresh()
            with self.assertRaisesRegex(ValueError,'Immutable claim'): f.inspect()
        with tempfile.TemporaryDirectory() as td:
            f=ArtifactFixture(td); other=f.root/'elsewhere.jsonl'; other.write_bytes(f.lp.read_bytes())
            d=json.loads(f.descriptor.read_text()); d['lineage']=f.ref(other); f.obj(f.descriptor,d)
            with self.assertRaisesRegex(ValueError,'Lineage outside'): f.inspect()

    def test_late_artifact_drift_is_not_laundered_by_a_positive_pure_join(self):
        with tempfile.TemporaryDirectory() as td:
            f=ArtifactFixture(td); original=a.validate
            def mutate(*args):
                result=original(*args); f.lp.write_text(f.lp.read_text()+'{}\n'); return result
            with patch.object(a,'validate',side_effect=mutate):
                with self.assertRaisesRegex(ValueError,'hash mismatch'): f.inspect()

    def test_lineage_symlink_and_parent_symlink_refused(self):
        for parent in (False,True):
            with self.subTest(parent=parent),tempfile.TemporaryDirectory() as td:
                f=ArtifactFixture(td)
                try:
                    if parent:
                        target=f.folder.with_name(f.folder.name+'-actual'); f.folder.rename(target); f.folder.symlink_to(target,target_is_directory=True)
                    else:
                        target=f.folder/'actual.jsonl'; f.lp.rename(target); f.lp.symlink_to(target)
                except OSError as error: self.skipTest('Host symlink privilege unavailable: '+str(error))
                with self.assertRaisesRegex(ValueError,'symlink/junction/reparse'): f.inspect()

    def test_truncated_duplicate_key_and_nan_lineage_never_admitted(self):
        for suffix in (b'{}',b'{"x":1,"x":2}\n',b'{"x":NaN}\n'):
            with self.subTest(suffix=suffix),tempfile.TemporaryDirectory() as td:
                f=ArtifactFixture(td); f.lp.write_bytes(f.lp.read_bytes()+suffix); f.refresh()
                self.assertFalse(f.inspect()['recorded_join_consistent'])

    def test_actual_source_and_dll_bytes_not_only_manifest_labels(self):
        for rel in ('Assets/producer.cs','Library/ScriptAssemblies/Game.Editor.dll'):
            with self.subTest(path=rel),tempfile.TemporaryDirectory() as td:
                f=ArtifactFixture(td); (f.root/rel).write_bytes(b'changed bytes')
                with self.assertRaisesRegex(ValueError,'hash mismatch'): f.inspect()

    def test_real_unreviewed_producer_contract_refused(self):
        with tempfile.TemporaryDirectory() as td:
            f=ArtifactFixture(td)
            with self.assertRaisesRegex(ValueError,'typed-producer pins'): a.inspect(f.root,f.descriptor)



class WindowsRepairTests(unittest.TestCase):
    def test_same_entity_generation_cannot_be_owned_by_foreign_reservation(self):
        f=Fixture(); other=copy.deepcopy(f.tx[3]); other.update(operation='DirectMember',member_ordinal=0)
        for side in ('before','after'): other[side]['reservation'].update(id=8,phase='Live',delivered=1)
        other['actor'].update(reservation=8,stable_id='production.8.0')
        f.tx.insert(0,other)
        for n,row in enumerate(f.tx,1): row['ledger_sequence']=n
        f.lineage[1]['frame']=9
        result=f.validate()
        self.assertFalse(result['recorded_join_consistent'],result)
        self.assertIn('entity generation ownership rebound',result['error'])

    def test_same_owned_entity_may_not_change_its_stable_identity(self):
        f=Fixture(); tail=copy.deepcopy(f.tx[3]); tail.update(operation='DeathRelease',ledger_sequence=5)
        tail['actor']['stable_id']='production.7.forged'; f.tx.append(tail)
        result=f.validate()
        self.assertFalse(result['recorded_join_consistent'],result)
        self.assertIn('entity generation ownership rebound',result['error'])

    def test_other_entity_generation_does_not_alias_target_delivery(self):
        f=Fixture(); other=copy.deepcopy(f.tx[3]); other.update(operation='DirectMember',member_ordinal=0)
        for side in ('before','after'): other[side]['reservation'].update(id=8,phase='Live',delivered=1)
        other['actor'].update(reservation=8,stable_id='production.8.0',version=1)
        f.tx.insert(0,other)
        for n,row in enumerate(f.tx,1): row['ledger_sequence']=n
        f.lineage[1]['frame']=9
        result=f.validate()
        self.assertTrue(result['recorded_join_consistent'],result)
        self.assertFalse(result['qualified']); self.assertFalse(result['whole_attempt_conservation'])

    def test_running_lineage_budget_detects_actual_growth_after_precheck(self):
        with tempfile.TemporaryDirectory() as td:
            root=Path(td).resolve(); path=root/'lineage.jsonl'; path.write_bytes(b'{}\n')
            reader=a.Reader(root)
            reader.verify(dict(path=str(path),sha256=self_digest(path)),4096,False)
            original=Path.open; grew=[]
            def open_then_grow(p,*args,**kwargs):
                stream=original(p,*args,**kwargs)
                if p==path and args and args[0]=='rb' and not grew:
                    with original(path,'ab') as append: append.write(b'{}\n'*1500)
                    grew.append(True)
                return stream
            with patch.object(a,'MAX_LINEAGE_BYTES',4096),patch.object(Path,'open',new=open_then_grow):
                with self.assertRaisesRegex(ValueError,'running byte budget'):
                    list(a._lineage_rows(reader,path))
            self.assertEqual(grew,[True]); self.assertGreater(path.stat().st_size,4096)
            with self.assertRaisesRegex(ValueError,'oversized|hash mismatch'):
                reader.stable()

    def test_binary_line_budget_counts_utf8_bytes_not_unicode_characters(self):
        with tempfile.TemporaryDirectory() as td:
            root=Path(td).resolve(); path=root/'lineage.jsonl'
            path.write_bytes((json.dumps({'text':'\u00e9'*20},ensure_ascii=False)+'\n').encode('utf-8'))
            with patch.object(a,'MAX_LINEAGE_LINE_BYTES',40):
                with self.assertRaisesRegex(ValueError,'Bounded complete lineage'):
                    list(a._lineage_rows(a.Reader(root),path))

    def test_lineage_allows_only_one_initial_bom_and_still_counts_it(self):
        with tempfile.TemporaryDirectory() as td:
            root=Path(td).resolve(); path=root/'lineage.jsonl'; path.write_bytes(b'\xef\xbb\xbf{}\n')
            self.assertEqual(list(a._lineage_rows(a.Reader(root),path)),[{}])
            path.write_bytes(b'{}\n\xef\xbb\xbf{}\n')
            with self.assertRaises(ValueError): list(a._lineage_rows(a.Reader(root),path))

    def test_lineage_refuses_invalid_utf8(self):
        with tempfile.TemporaryDirectory() as td:
            root=Path(td).resolve(); path=root/'lineage.jsonl'; path.write_bytes(b'\xff\n')
            with self.assertRaises(UnicodeError): list(a._lineage_rows(a.Reader(root),path))

    def test_actual_lineage_producer_is_an_explicit_seventh_literal_pin(self):
        self.assertEqual(a.POLICY_PINS['Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.NormalPaidPlaneLineageCanary.cs'],
                         '1679fdb1289e18bb081d245260c15ce770228c809fb8e3a4f6559c9039e9ab40')
        self.assertEqual(len(a.POLICY_PINS),7)


def self_digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

if __name__=='__main__': unittest.main()
