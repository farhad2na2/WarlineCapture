"""Integration and fail-closed controls for captured-order bounded world replay."""
import argparse
import copy
import json
from pathlib import Path
import shutil
import sys
import unittest
sys.dont_write_bytecode = True
import skirmish_world_replay as sim

BUILD = OUTPUT = None
PROJECT = Path(__file__).resolve().parents[2]


def actor(name, index, faction, x, kind='infantry', health=100):
    mobile=kind=='infantry'
    return dict(id=name,kind=kind,index=index,version=3,health=health,faction=faction,x=x,y=0,z=2,cell_x=x,cell_z=2,
        radius=0,has_unit_grid=mobile,combat_suppressed=False,has_animation_order=False,has_engage=False,target='',cover_eligible=mobile,cover_permille=0,
        protected_records=False,weapon=dict(State=dict(CooldownRemaining=0,TraceTimeRemaining=0,TracePhase=0,ShotCounter=0,AnimationTimeRemaining=0),
            Range=10,CooldownSeconds=.1,TraceVisibleSeconds=.1,AttackAnimationSeconds=.1,Damage=100,TracerEveryNthShot=1) if mobile else None,
        acquisition=dict(grid_width=32,grid_height=32,aggro_range_cells=10,auto_engage=1,attack_move=True,manual_move=False,
            path_follow=False,path_request=False,hold_position=True,scan=None,recent='') if mobile else None)


def candidates(actors):
    cells={}
    for a in actors:
        if a['kind']=='infantry' and a['health']>0:
            cells.setdefault(a['cell_x']+32*a['cell_z'],[]).append(a['id'])
    return dict(cells=[dict(key=k,actors=v) for k,v in cells.items()],buildings=[a['id'] for a in actors if a['kind']=='base'],
        policies=[dict(target=a['id'],facts=dict(HasSourcePolicy=False,HasTargetPolicy=False,TargetIsStructure=a['kind']=='base',
            TargetIsAir=False,TargetUsesVehicleMotion=False,SourceAllowedTargets=0,TargetDomain=0,TargetVisible=1,
            PairedFog=False,SameFogSession=False,FogVisibleToObserver=False)) for a in actors])


def frame(actors, elapsed, acquire=(), attack=(), damage=(), dt=.2, paused=False, surrender=False):
    return dict(elapsed_time=elapsed,delta_time=dt,paused=paused,surrender=surrender,has_gameplay_state=True,simulation_active=1,
        acquisition_order=list(acquire),candidate_orders={name:candidates(actors) for name in acquire},attack_order=list(attack),damage_commit_order=list(damage))


def case(name='world'):
    actors=[actor('unit',1,1,2),actor('home',2,1,0,'base'),actor('enemy',3,2,4,'base')]
    return dict(id=name,kind='world',profile='IsolatedStationaryCombatScheduleV1',profile_sha256=sim.common.sha(BUILD/'profile.json'),
        cell_size=1,actors=actors,player_base='home',enemy_base='enemy',deadline_seconds=100,policy_profile='unrestricted_visible_no_fog',
        frames=[frame(actors,0,acquire=['unit']),frame(actors,.2,attack=['unit'],damage=['enemy']),frame(actors,.4,attack=['unit'])],
        expect=dict(frozen=True,freeze_count=1,outcome="Victory",reason="MainBaseDestroyed",boundary='none'))


class WorldReplayTests(unittest.TestCase):
    def setUp(self):
        self.output=OUTPUT/self._testMethodName;self.output.mkdir();self.index=0
    def replay(self,cases,build=None):
        self.index+=1;p=self.output/f'input-{self.index}.json';p.write_text(json.dumps(dict(schema_version=1,cases=cases)))
        return sim.run(build or BUILD,p,self.output/f'run-{self.index}')
    def plan(self,c):
        r=self.replay([c]);self.assertEqual(r['failed'],0,r.get('cases',[])[:2]);self.assertEqual(r['process_exit'],0);return r['cases'][0]['plan']
    def test_acquire_playback_shot_projection_and_freeze(self):
        p=self.plan(case());f=p['frames'];self.assertEqual(p['consumed_frames'],3)
        self.assertEqual(f[0]['attacks'],[]);self.assertEqual(f[0]['actors'][0]['target'],'enemy')
        self.assertEqual(f[0]['actors'][2]['health'],100);self.assertEqual(f[1]['actors'][2]['health'],0)
        self.assertFalse(f[1]['frozen']);self.assertTrue(f[2]['frozen']);self.assertTrue(f[1]['observed_ba_facts']['EnemyDesignatedAlive'])
        self.assertFalse(f[2]['observed_ba_facts']['EnemyDesignatedAlive']);self.assertEqual(f[2]['actors'][0]['target'],'')
        self.assertEqual(f[1]['attacks'][0]['plan']['Kind'],'Shot')
    def test_two_independent_missions_reset_and_repeat(self):
        cases=[case('one'),case('two')];a=self.replay(cases);b=self.replay(cases)
        self.assertEqual(a['passed'],2,a.get('cases',[])[:2]);self.assertEqual(a['cases'],b['cases'])
        self.assertEqual(a['cases'][0]['plan'],a['cases'][1]['plan'])
        self.assertFalse(a['full_battle_simulated']);self.assertFalse(a['mission_qualified'])
    def test_mutually_lethal_delayed_commit_retains_dead_actors(self):
        c=case();a=c['actors'];other=actor('other',4,2,3);a.append(other)
        a[0].update(has_engage=True,target='other');other.update(has_engage=True,target='unit')
        c['frames']=[frame(a,0,attack=['unit','other'],damage=['other','unit'])]
        c['expect']=dict(frozen=False,freeze_count=0,boundary='mobile_death_requires_native_teardown')
        p=self.plan(c);self.assertEqual([x['health'] for x in p['actors']],[0,100,100,0]);self.assertEqual(len(p['actors']),4)
        self.assertEqual([x['plan']['Kind'] for x in p['frames'][0]['attacks']],['Shot','Shot'])
    def test_paused_zero_delta_acquisition_and_ready_shot_keep_native_gates(self):
        c=case();a=c['actors'];c['frames']=[frame(a,0,acquire=['unit'],dt=0,paused=True),frame(a,.2,attack=['unit'],damage=['enemy'],dt=0,paused=True)]
        c['expect']=dict(frozen=False,elapsed=0,boundary='none');p=self.plan(c)
        self.assertTrue(p['frames'][0]['acquisition_admitted']);self.assertTrue(p['frames'][1]['attack_admitted']);self.assertEqual(p['actors'][2]['health'],0)
    def test_projection_terminal_precedes_same_frame_surrender(self):
        c=case();a=c['actors'];a[2]['health']=0;a[0]['acquisition']['auto_engage']=0;a[0]['acquisition']['attack_move']=False
        c['frames']=[frame(a,0,acquire=['unit'],surrender=True)];c['expect']=dict(frozen=True,outcome="Victory",reason="MainBaseDestroyed",freeze_count=1)
        self.plan(c)
    def test_acquisition_cadence_and_unengaged_timer_recovery(self):
        c=case();a=c['actors'];a[0]['acquisition'].update(auto_engage=0,attack_move=False);a[0]['weapon']['State'].update(CooldownRemaining=.5,TraceTimeRemaining=.5)
        c['frames']=[frame(a,0,acquire=['unit'],dt=.1),frame(a,.05,acquire=['unit'],dt=.1),frame(a,.12,acquire=['unit'],dt=.1)]
        c['expect']=dict(frozen=False,boundary='none');p=self.plan(c)
        self.assertEqual([f['acquisition_admitted'] for f in p['frames']],[True,False,True]);self.assertAlmostEqual(p['actors'][0]['attack']['CooldownRemaining'],.2,places=5)
    def test_recent_retaliation_is_deferred_and_unknown_recent_rejected(self):
        c=case();c['actors'][0]['acquisition']['recent']='enemy';p=self.plan(c)
        self.assertTrue(p['frames'][0]['acquisition'][0]['result']['Retaliation']);self.assertEqual(p['frames'][0]['actors'][0]['recent'],'')
        c=case('unknown');c['actors'][0]['acquisition']['recent']='missing';r=self.replay([c]);self.assertEqual(r['failed'],1)
    def test_acquisition_recent_removal_follows_local_attack_playback(self):
        c=case();a=c['actors'];other=actor('other',4,2,3);a.append(other)
        a[0]['acquisition']['recent']='other';other.update(has_engage=True,target='unit')
        c['frames']=[frame(a,0,acquire=['unit'],attack=['other'],damage=['unit'])]
        c['expect']=dict(frozen=False,boundary='mobile_death_requires_native_teardown')
        p=self.plan(c);unit=p['actors'][0]
        self.assertEqual(unit['health'],0);self.assertEqual(unit['recent'],'');self.assertEqual(unit['target'],'other')
        self.assertTrue(p['frames'][0]['acquisition'][0]['result']['RemoveRecentAttacker'])
    def test_hold_range_policy_grid_identity_and_animation_rejections(self):
        cases=[]
        for i in range(13):
            c=case(str(i));a=c['actors'][0]
            if i==0:a['acquisition']['hold_position']=False
            if i==1:a['acquisition']['path_follow']=True
            if i==2:a.update(has_engage=True,target='enemy');a['weapon']['Range']=1
            if i==3:c['profile_sha256']='0'*64
            if i==4:c['actors'][2]['has_unit_grid']=True
            if i==5:a['has_animation_order']=True
            if i==6:a['weapon']['State']['AnimationTimeRemaining']=1
            if i==7:a['acquisition']['scan']=dict(CenterX=2,CenterZ=2,RadiusCells=4,HasStarted=1,EngageDetectedTargets=1)
            if i==8:c['frames'][0]['candidate_orders']['unit']['policies'][0]['facts']['PairedFog']=True
            if i==9:c['actors'][2]['index']=a['index']
            if i==10:c['frames'][0]['candidate_orders']['unit']['cells']=[]
            if i==11:del a['weapon']['State']['ShotCounter']
            if i==12:c['actors'][2]['acquisition']={}
            cases.append(c)
        r=self.replay(cases);self.assertEqual(r['failed'],len(cases),r)
    def test_inconsistent_shared_candidate_order_and_grid_fail(self):
        cases=[]
        for i in range(2):
            c=case(str(i));a=c['actors'];a.append(actor('ally',4,1,2));c['frames']=[frame(a,0,acquire=['unit','ally'])];c['expect']=dict(frozen=False)
            if i==0:c['frames'][0]['candidate_orders']['ally']['cells'][0]['actors'].reverse()
            else:a[3]['acquisition']['grid_width']=31
            cases.append(c)
        r=self.replay(cases);self.assertEqual(r['failed'],2,r)
    def test_captured_profile_and_binary_tamper_fail_before_replay(self):
        for i,relative in enumerate(('profile.json','bin/WorldReplay.dll')):
            clone=self.output/f'build-{i}';shutil.copytree(BUILD,clone);p=clone/relative;p.write_bytes(p.read_bytes()+b' ')
            with self.assertRaisesRegex(ValueError,'Built artifact changed'):self.replay([case()],clone)
    def test_disabled_world_profile_cannot_claim_world_validation(self):
        clone=self.output/'build';shutil.copytree(BUILD,clone);profile=clone/'profile.json';profile.write_text(json.dumps(dict(enabled=False,reason='No native receipt')))
        manifest=clone/'build.json';m=json.loads(manifest.read_text());m['build_files']['profile.json']=sim.common.sha(profile);manifest.write_text(json.dumps(m))
        r=self.replay([case()],clone);self.assertEqual(r['failed'],1);self.assertIn('Verified native schedule receipt',r['cases'][0]['error'])
    def test_120_synthetic_world_configs_repeat_identically(self):
        cases=[case(str(i)) for i in range(120)];a=self.replay(cases);b=self.replay(cases)
        self.assertEqual(a['passed'],120,a.get('cases',[])[:2]);self.assertEqual(a['cases'],b['cases'])
        (self.output/'benchmark.json').write_text(json.dumps(dict(synthetic_configs=120,first_seconds=a['elapsed_seconds'],repeat_seconds=b['elapsed_seconds'],identical=True,mission_wins_claimed=0),indent=2))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--build',type=Path,required=True);parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args();BUILD=args.build.resolve();OUTPUT=sim.common.new_output(PROJECT,args.output)
    profile=json.loads((BUILD/'profile.json').read_text())
    if not profile.get('enabled'):raise SystemExit('Verified native schedule receipt required; no world tests skipped or fabricated')
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(WorldReplayTests))
    print(f"[SkirmishWorldReplayTests] result={'Passed' if result.wasSuccessful() else 'Failed'} tests={result.testsRun} skipped={len(result.skipped)} native_runs=0")
    raise SystemExit(0 if result.wasSuccessful() else 1)
