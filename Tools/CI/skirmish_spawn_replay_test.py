"""Strict standalone spawn/blocker/overlay integration and native trace comparisons."""
from __future__ import annotations
import argparse,copy,hashlib,json,os,subprocess,sys,tempfile,time,unittest
from pathlib import Path
import skirmish_spawn_replay as replay

BUILD=None;OUT=None;NATIVE_LOG=None

def state(**changes):
    result={k:False for k in 'IsStructure SharedActor CombatSuppressed HasHealth HasUnitAttack HasBuildingWeapon HasUnitCombat HasAirLauncher HasGroundLauncher'.split()}
    result.update({k:0 for k in 'Category AllowedTargets Domain Visible CanAttack AutoEngage HealthCurrent HealthMax AttackDamage BuildingDamage AirDamage GroundDamage'.split()})
    result.update({k:0.0 for k in 'AttackRange AttackCooldown BuildingRange BuildingCooldown AirBaseDetectionRange AirMaxDetectionRange AirMinRange AirSupportRangeBonus GroundMaxRange GroundMinRange'.split()});result.update(changes);return result

def grid(width=8,height=8):return dict(Width=width,Height=height,OriginX=0.0,OriginY=0.0,OriginZ=0.0,CellSize=1.0)
def owner(g=None,**changes):
    result=dict(StorageEpoch=1,AppliedStorageEpoch=1,AppliedRevision=0,WriteInProgress=0,EpochExhausted=0,AppliedGrid=g or grid());result.update(changes);return result

def opening(**changes):
    result={k:0.0 for k in 'CenterX CenterZ RightX RightZ ForwardX ForwardZ HalfX HalfZ'.split()};result.update(changes);return result

def storage(n=64):return dict(walkable=[1]*n,blocked=[0]*n,occupied=[0]*n,reserved=[0]*n,counts=[0]*n,friendly_pass=[255]*n,friendly_pass_created=True)
def search(id='search',**changes):
    s=storage();r=dict(id=id,kind='initial_search',width=8,height=8,walkable=s['walkable'],blocked=s['blocked'],occupied=s['occupied'],reserved=s['reserved'],center=[3,3],radius=0,footprint=[1,1],is_air=False,random_state=12345,emit_arrays=True);r.update(changes);return r

def actor(id='actor',**changes):
    result=dict(id=id,kind='actor_initialization',state=state(SharedActor=True,CombatSuppressed=True,HasUnitAttack=True,HasUnitCombat=True,Category=1),overlay=dict(MaxHealth=23,Damage=4,RangeWorld=7.0,TargetDomains=3),shot_interval=.5);result.update(changes);return result

def blocker(id='building',origin=None,size=None,faction=255,**changes):
    r=dict(id=id,origin=origin or [2,2],size=size or [2,2],faction=faction,has_opening=False,opening=opening(),had_opening=False,previous_opening=opening());r.update(changes);return r

def blockers(id='blockers',**changes):
    r=dict(id=id,kind='blockers',grid=grid(),storage=dict(counts=[0]*64,blocked=[0]*64,friendly_pass=[255]*64,friendly_pass_created=True,unavailable_arrays=['walkable','occupied','reserved']),owner=owner(),grid_identity=dict(Index=1,Version=1,StorageEpoch=1),new=[],existing=[],removed=[],emit_arrays=True);r.update(changes);return r

def sequence(id='sequence'):
    return dict(id=id,kind='placement_sequence',grid=grid(),storage=storage(),blocker_update=None,existing=[],actors=[],surface_profiles=[dict(id='infantry',footprint=[1,1],vehicle=False,admission=[1]*64,provenance='SyntheticExplicitFootprintFacts')],has_match=True,surface_count=1,emit_arrays=True,facts_scope='SyntheticSnapshot',placement_mode='Untyped')
def member(id='rifle.1.0.0',world=None):return dict(id=id,mission_seed=17,requested_world=world or [3.5,0,3.5],footprint=[1,1],is_air=False,vehicle=False,surface_profile='infantry',typed_occupied=None,has_footprint_component=True,initialization={k:v for k,v in actor().items() if k in ('state','overlay','shot_interval')})

class SpawnReplayTests(unittest.TestCase):
    serial=0
    def call(self,cases,artifacts=None):
        SpawnReplayTests.serial+=1;folder=OUT/f'case-{SpawnReplayTests.serial:03d}';folder.mkdir();source=folder/'scenario.json';source.write_text(json.dumps(dict(schema_version=1,artifacts=artifacts or [],cases=cases),allow_nan=False))
        result=replay.run(BUILD,source,folder/'run');return result
    def passed(self,cases,artifacts=None):
        r=self.call(cases,artifacts);self.assertEqual(r['result'],'passed',r.get('errors'));self.assertEqual(r['process_exit'],0);return r['results']
    def test_actor_overlay_preserves_absent_components_and_resets_shared_suppression(self):
        a=actor();a['state'].update(AttackDamage=99,GroundMinRange=12,HasGroundLauncher=True)
        a['expect']={'state':{'HasHealth':True,'HealthCurrent':23,'HealthMax':23,'AttackDamage':4,'AttackRange':7,'AttackCooldown':.5,'GroundMinRange':3.5,'Domain':1,'AllowedTargets':3,'CanAttack':1,'AutoEngage':1,'CombatSuppressed':False}}
        b=actor('absent',state=state(CombatSuppressed=True,AttackDamage=99));b['expect']={'state':{'AttackDamage':99,'CombatSuppressed':True,'HasUnitAttack':False,'HealthCurrent':23}}
        self.passed([a,b])
    def test_initial_center_footprint_and_ring_fallback(self):
        a=search(footprint=[2,2]);a['expect']={'found':True,'cell':[3,3],'random_state':12345};r=self.passed([a])[0];self.assertEqual([27,28,35,36],[i for i,v in enumerate(r['reserved']) if v])
        b=search('ring');b['blocked'][27]=1;b['expect']={'found':True,'cell':[2,2]};self.passed([b])
    def test_air_blocked_reserved_preference_and_occupied_rejection(self):
        a=search(is_air=True);a['blocked'][27]=1;a['reserved'][27]=1;a['expect']={'found':True,'cell':[3,3],'random_state':12345}
        b=copy.deepcopy(a);b['id']='occupied';b['occupied'][27]=1;b['expect']={'found':True,'cell':[2,2]};self.passed([a,b])
    def test_surface_release_relocation_no_match_and_ambiguous_surface(self):
        a=search();a.update(kind='surface_correction',cell=[3,3],has_match=True,surface_count=1,surface_admission=[0]*64);a['reserved'][27]=1;a['surface_admission'][18]=1
        for key in ('center','radius','is_air','random_state'):del a[key]
        a['expect']={'found':True,'cell':[2,2]};r=self.passed([a])[0];self.assertEqual(0,r['reserved'][27]);self.assertEqual(1,r['reserved'][18])
        for label,match,count,found in [('nomatch',False,0,True),('ambiguous',True,2,False),('missing',True,0,False)]:
            b=copy.deepcopy(a);b.update(id=label,has_match=match,surface_count=count);b['expect']={'found':found,'cell':[3,3],'reserved':a['reserved']};self.passed([b])
    def test_blocker_overlap_opening_and_removal_friendly_order(self):
        a=blockers(new=[blocker('first',faction=1),blocker('second',faction=2)])
        r=self.passed([a])[0];self.assertEqual(2,r['storage']['counts'][18]);self.assertEqual(2,r['storage']['friendly_pass'][18]);self.assertEqual(1,r['update']['owner']['AppliedRevision'])
        changes=r['update']['changes'];b=blockers('remove');b['storage'].update(counts=r['storage']['counts'],blocked=r['storage']['blocked'],friendly_pass=r['storage']['friendly_pass']);b['removed']=[dict(id='second',history=changes[1]['history'],had_opening=False,previous_opening=opening())]
        out=self.passed([b])[0];self.assertEqual(1,out['storage']['counts'][18]);self.assertEqual(255,out['storage']['friendly_pass'][18])
        c=blockers('opening',new=[blocker(has_opening=True,opening=opening(CenterX=2.5,CenterZ=2.5,RightX=1,ForwardZ=1,HalfX=.5,HalfZ=.5))]);out=self.passed([c])[0];self.assertEqual(0,out['storage']['counts'][18]);self.assertEqual(1,out['storage']['counts'][19])
    def test_new_blocker_retains_existing_previous_opening(self):
        previous=opening(CenterX=4.5,CenterZ=3.5,HalfX=.5,HalfZ=.5)
        case=blockers(new=[blocker(had_opening=True,previous_opening=previous)])
        row=self.passed([case])[0]['update']['changes'][0]
        self.assertTrue(row['has_previous_opening']);self.assertEqual(previous,row['previous_opening']);self.assertEqual('None',row['opening_change'])
        ignored=opening(CenterX=2.5,CenterZ=2.5,RightX=1,ForwardZ=1,HalfX=100,HalfZ=100)
        case=blockers(new=[blocker(has_opening=False,opening=ignored,had_opening=False,previous_opening=ignored)])
        row=self.passed([case])[0];self.assertEqual(1,row['storage']['counts'][18]);self.assertFalse(row['update']['changes'][0]['has_previous_opening'])
    def test_rebuild_foreign_epoch_and_exhaustion(self):
        a=blockers(owner=owner(StorageEpoch=0,AppliedStorageEpoch=0),grid_identity=dict(Index=2,Version=3,StorageEpoch=0),new=[blocker()]);a['expect']={'update':{'rebuild':True,'owner':{'StorageEpoch':1,'AppliedStorageEpoch':1,'AppliedRevision':1}}};self.passed([a])
        a=blockers('exhausted',owner=owner(StorageEpoch=2**64-1,AppliedStorageEpoch=0,AppliedRevision=2**64-1),grid_identity=dict(Index=2,Version=3,StorageEpoch=2**64-1));a['expect']={'update':{'owner':{'StorageEpoch':2**64-1,'AppliedRevision':2**64-1,'EpochExhausted':1}}};self.passed([a])
    def test_seeded_composed_sequence_reserves_derives_and_repeats(self):
        case=sequence();case['actors']=[member(),member('rifle.1.0.1')];update=blockers(new=[blocker('wall',origin=[3,3],size=[1,1])]);case['blocker_update']={k:update[k] for k in ('owner','grid_identity','new','existing','removed')}
        a=self.passed([case])[0];b=self.passed([case])[0];self.assertEqual(a,b);self.assertTrue(a['all_requested_cells_placed']);self.assertNotEqual(a['actors'][0]['cell'],[3,3]);self.assertNotEqual(a['actors'][0]['cell'],a['actors'][1]['cell']);self.assertEqual(a['actors'][0]['actor_overlay_state']['HealthCurrent'],23);self.assertFalse(a['grounding_executed']);self.assertFalse(a['full_mission_initialized'])
    def test_missing_friendly_storage_remains_absent_during_rebuild(self):
        case=sequence();case['storage'].update(friendly_pass=[],friendly_pass_created=False);update=blockers(owner=owner(StorageEpoch=0,AppliedStorageEpoch=0),grid_identity=dict(Index=1,Version=1,StorageEpoch=0));case['blocker_update']={k:update[k] for k in ('owner','grid_identity','new','existing','removed')}
        result=self.passed([case])[0];self.assertEqual([],result['storage']['friendly_pass_values']);self.assertTrue(result['blocker_update']['rebuild'])
    def test_missing_footprint_does_not_enter_next_reservation_query(self):
        case=sequence();case['actors']=[member(),member('rifle.1.0.1')]
        case['actors'][0]['has_footprint_component']=False
        result=self.passed([case])[0];self.assertEqual([3,3],result['actors'][0]['cell']);self.assertEqual([3,3],result['actors'][1]['cell'])
        case['actors'][0]['has_footprint_component']=True
        result=self.passed([case])[0];self.assertNotEqual(result['actors'][0]['cell'],result['actors'][1]['cell'])
    def test_actor_specific_typed_occupied_masks(self):
        case=sequence();case['placement_mode']='CapturedTypedOccupancy';case['actors']=[member(),member('rifle.1.0.1')]
        for actor,index in zip(case['actors'],[18,36]):
            actor['typed_occupied']=[1]*64;actor['typed_occupied'][index]=0
        result=self.passed([case])[0]
        self.assertEqual([[2,2],[4,4]],[a['cell'] for a in result['actors']]);self.assertTrue(result['typed_occupied_facts_applied']);self.assertFalse(result['typed_pad_constraints_evaluated'])
        case['storage']['occupied'][18]=1;self.assertEqual('failed',self.call([case])['result'])
        case['actors'][0]['typed_occupied']=None;self.assertEqual('failed',self.call([case])['result'])
    def test_sequence_failure_stops_without_claiming_native_cleanup(self):
        case=sequence();case['storage']['walkable']=[0]*64;case['actors']=[member(),member('rifle.1.0.1')];out=self.passed([case])[0];self.assertFalse(out['all_requested_cells_placed']);self.assertEqual(1,len(out['actors']));self.assertIsNone(out['actors'][0]['actor_overlay_state'])
    def test_strict_schema_missing_profile_nonfinite_and_limits(self):
        cases=[]
        bad=actor();bad['state'].pop('Visible');cases.append(bad)
        bad=sequence('missing-profile');bad['actors']=[member()];bad['surface_profiles']=[];cases.append(bad)
        bad=search('badcount',width=2048,height=1024);cases.append(bad)
        bad=search('zero-rng',random_state=0);cases.append(bad)
        for case in cases:self.assertEqual('failed',self.call([case])['result'])
    def test_binary_inputs_and_tamper_detection(self):
        p=OUT/'mask.bin';p.write_bytes(bytes([1]*64));descriptor=dict(id='mask',path=str(p),sha256=replay.sha(p),bytes=64,encoding='u8');case=search(walkable={'artifact':'mask'});self.passed([case],[descriptor]);p.write_bytes(bytes([0]*64))
        with self.assertRaisesRegex(ValueError,'drift'):self.call([case],[descriptor])
    def test_probe_failed_placement_is_not_success(self):
        from skirmish_spawn_export_probe import summarize_placements
        for rejected in ('walkable','surface'):
            case=sequence();case['actors']=[member(),member('rifle.1.0.1')]
            if rejected=='walkable':case['storage']['walkable']=[0]*64
            else:case['surface_profiles'][0]['admission']=[0]*64
            report=self.call([case]);self.assertEqual('passed',report['result'])
            summarize_placements(report,2)
            self.assertEqual('failed',report['result']);self.assertEqual(0,report['placed_count']);self.assertEqual(2,report['unplaced_count']);self.assertFalse(report['all_requested_cells_placed'])
    def test_native_adapter_traces_exact(self):
        if NATIVE_LOG is None:self.skipTest('Native verification log not supplied')
        comparisons=compare_native(self);(OUT/'native-comparison.json').write_text(json.dumps(comparisons,indent=2)+'\n')


def markers(log,marker):
    values=[]
    for line in Path(log).read_text().splitlines():
        if marker in line:
            raw=line.split(marker,1)[1].strip()
            if raw.startswith('{'):values.append(json.loads(raw))
    if not values:raise AssertionError('Missing native marker '+marker)
    return values


def compare_native(test):
    text=Path(NATIVE_LOG).read_text();assert '[SharedSpawnInitializationParity] result=Passed' in text
    actor_trace=markers(NATIVE_LOG,'[SpawnInitializationNativeTrace]')[-1];actual=test.passed(actor_trace['cases']);expected={r['id']:r for r in actor_trace['results']};assert len(actual)==len(expected)
    import struct
    for row in actual:
        target=expected[row['id']];assert set(row)==set(target)
        assert row['id']==target['id'] and row['kind']==target['kind']
        assert set(row['state'])==set(target['state'])
        for key,value in row['state'].items():
            if key in state() and isinstance(state()[key],float):assert struct.pack('<f',value)==struct.pack('<f',target['state'][key]),(row['id'],key)
            else:assert value==target['state'][key],(row['id'],key)
    placement=markers(NATIVE_LOG,'[SpawnPlacementNativeTrace]')[-1];cases=[]
    for row in placement['cases']:
        case=dict(id=row['id'],kind=row['stage'],width=row['width'],height=row['height'],walkable=row['walkable'],blocked=row['blocked'],occupied=row['occupied'],reserved=row['reserved_before'],footprint=[row['size_x'],row['size_z']],emit_arrays=True)
        if row['stage']=='initial_search':case.update(center=[row['center_x'],row['center_z']],radius=row['radius'],is_air=row['is_air'],random_state=row['random_state_before'])
        else:case.update(cell=[row['center_x'],row['center_z']],has_match=row['has_match'],surface_count=row['surface_count'],surface_admission=row['surface_admission'])
        cases.append(case)
    actual=test.passed(cases)
    for a,n in zip(actual,placement['cases']):
        assert a['found']==n['found'] and a['cell']==[n['x'],n['z']] and a['reserved']==n['reserved_after'],n['id']
        if n['stage']=='initial_search':assert a['random_state']==n['random_state_after'],n['id']
    native_blockers=markers(NATIVE_LOG,'[StaticBlockerNativeTrace]');blocker_cases=[]
    for entry in native_blockers:
        blocker_cases.extend(entry.get('cases',[entry]))
    inputs=[]
    fields={'new':set('id origin size faction has_opening opening had_opening previous_opening'.split()),'existing':set('id origin size faction has_opening opening history had_opening previous_opening'.split()),'removed':set('id history had_opening previous_opening'.split())}
    for row in blocker_cases:
        converted={k:v for k,v in row.items() if k!='expected'}
        for phase,allowed in fields.items():converted[phase]=[{k:v for k,v in item.items() if k in allowed} for item in row[phase]]
        inputs.append(converted)
    actual=test.passed(inputs);compared_changes=0
    for a,n in zip(actual,blocker_cases):
        exp=n['expected'];assert a['update']['owner']==exp['owner'],n['id']
        for field in ('counts','blocked','friendly_pass'):assert a['storage'][field]==exp['storage'][field],(n['id'],field)
        changes={v['id']:v for v in exp['changes']}
        for change in a['update']['changes']:
            expected_change=changes[change['id']]
            for field in ('has_history','has_previous_opening'):assert change[field]==expected_change[field],(n['id'],field)
            if change['has_history']:assert change['history']==expected_change['history'],n['id']
            if change['has_previous_opening']:assert change['previous_opening']==expected_change['previous_opening'],n['id']
            compared_changes+=1
    return dict(native_log_sha256=replay.sha(NATIVE_LOG),actor_cases=len(expected),placement_cases=len(cases),blocker_cases=len(blocker_cases),blocker_entity_states_compared=compared_changes,comparison='Actor floats compared as float32 bitpatterns; all discrete state/arrays exact; no tolerance',full_mission_initialized=False)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--build',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);parser.add_argument('--native-log',type=Path);args=parser.parse_args();BUILD=args.build.resolve();OUT=args.output.resolve();OUT.mkdir(parents=True,exist_ok=False);NATIVE_LOG=args.native_log
    unittest.main(argv=[sys.argv[0]],verbosity=2)
