"""Moving configuration integration, native trace parity, and strict profile controls."""
import argparse
import copy
import json
from pathlib import Path
import sys
import unittest
sys.dont_write_bytecode=True
import skirmish_world_replay as sim
import skirmish_world_replay_test as stationary
BUILD=OUTPUT=NATIVE_LOG=None
PROJECT=Path(__file__).resolve().parents[2]


def map_facts(width=16,height=5):
    n=width*height
    return dict(width=width,height=height,coordinates=dict(OriginX=0,OriginY=0,OriginZ=0,CellSize=1),provenance='synthetic flat single-cell infantry controls',
        walkable=[1]*n,dynamic_blocked=[0]*n,friendly_pass=[0]*n,sidewalk=[0]*n,dirt_road=[0]*n,surface_cell=[1]*n,surface_footprint=[1]*n,cardinal_cost=[10]*n,diagonal_cost=[14]*n)


def unit(name,index,faction,x,z=2,kind='infantry',health=100,width=16,height=5):
    a=stationary.actor(name,index,faction,x,kind,health);a.update(x=x+.5,z=z+.5,cell_z=z)
    if kind=='base':return dict(actor=a,movement=None)
    a['acquisition'].update(grid_width=width,grid_height=height,aggro_range_cells=4,hold_position=False)
    a['weapon'].update(Range=2)
    return dict(actor=a,movement=dict(speed=4,walk_speed=4,road_multiplier=1,arrive_distance=0,chase_break_distance=20,death_seconds=.4,
        manual=False,request=None,unit_target=None,lifecycle_profile='simple_infantry_no_children_no_gpu_no_light_no_carrier',can_attack=1,initial_engagement=None))


def frame(units,index,dt=.25):
    return dict(elapsed_time=(index+1)*dt,delta_time=dt,paused=False,surrender=False,has_gameplay_state=True,simulation_active=1,path_play_requested=True,path_job_completed=True,
        camera_valid=False,inside_viewport={u['actor']['id']:False for u in units if u['actor']['kind']=='infantry'})


def scenario(units,map_value,name='moving',count=60):
    ids=[u['actor']['id'] for u in units]
    return dict(id=name,kind='moving_world',profile='IsolatedMovingCombatScheduleV1',profile_sha256=sim.common.sha(BUILD/'profile.json'),map=map_value,units=units,
        query_orders={n:list(ids) for n in ('acquisition','spatial','buildings','pathfinding','grid_move','attack','damage','death_begin','death_finalize','engagement')},
        player_base='home',enemy_base='enemy',deadline_seconds=1000,frames=[frame(units,i) for i in range(count)],expect=dict(frozen=False))


def canonical(name='moving'):
    units=[unit('actor',1,1,1),unit('guard',2,2,7),unit('home',3,1,0,0,'base'),unit('enemy',4,2,13,2,'base',200)]
    units[0]['movement'].update(request=dict(x=10,z=2),unit_target=dict(x=10,z=2));units[0]['actor']['acquisition']['path_request']=True
    units[1]['movement']['can_attack']=0;units[1]['actor']['acquisition'].update(auto_engage=0,attack_move=False);units[1]['actor']['weapon']['Damage']=0
    c=scenario(units,map_facts(),name);c['expect']=dict(frozen=True,outcome='Victory',reason='MainBaseDestroyed',freeze_count=1);return c


def native_chain(row_count):
    units=[unit('actor',1,1,10,10,health=20,width=32,height=32),unit('first',2,2,17,10,health=3,width=32,height=32),unit('second',3,2,24,10,health=3,width=32,height=32),
        unit('home',4,1,0,0,'base',width=32,height=32),unit('enemy',5,2,31,31,'base',width=32,height=32)]
    for u in units[:3]:
        u['actor']['acquisition'].update(auto_engage=1,attack_move=False,aggro_range_cells=20)
        u['actor']['weapon'].update(Range=2,CooldownSeconds=.5,Damage=3,AttackAnimationSeconds=.25,TraceVisibleSeconds=0,TracerEveryNthShot=0)
        u['movement'].update(speed=3,walk_speed=0,chase_break_distance=30,death_seconds=.1,can_attack=1 if u['actor']['id']=='actor' else 0)
    units[1]['actor'].update(has_engage=True,target='actor');units[1]['actor']['weapon'].update(Range=.1,Damage=0);units[1]['movement'].update(speed=1,initial_engagement=dict(is_commanded=1))
    return scenario(units,map_facts(32,32),'native_chain',row_count)


class MovingWorldTests(unittest.TestCase):
    def setUp(self):self.output=OUTPUT/self._testMethodName;self.output.mkdir();self.index=0
    def replay(self,cases):
        self.index+=1;p=self.output/f'input-{self.index}.json';p.write_text(json.dumps(dict(schema_version=1,cases=cases)));return sim.run(BUILD,p,self.output/f'run-{self.index}')
    def plan(self,c):
        r=self.replay([c]);self.assertEqual(r.get('failed'),0,r.get('cases',r));return r['cases'][0]['plan']
    @staticmethod
    def events(plan,kind):return [(f,e) for f in plan['frames'] for e in f['events'] if e.get('kind')==kind]
    def test_route_chase_death_continuation_and_derived_base_victory(self):
        p=self.plan(canonical());kinds={e.get('kind') for f in p['frames'] for e in f['events']}
        self.assertTrue({'path_schedule','path_apply','grid_move','chase','attack','damage','death_begin','death_finalize','invalidate'}<=kinds)
        actor=next(u for u in p['units'] if u['id']=='actor');guard=next(u for u in p['units'] if u['id']=='guard')
        self.assertGreater(actor['x'],10);self.assertEqual(guard['health'],0);self.assertTrue(guard['dying']);self.assertEqual(guard['death']['PoseFrozen'],1)
        death_frame=self.events(p,'death_begin')[0][0]['frame'];after=p['frames'][death_frame+1]
        self.assertEqual(sum(after['occupancy_counts']),1);self.assertEqual(p['freeze_count'],1);self.assertGreater(p['consumed_frames'],3)
    def test_independent_config_reset_and_exact_repeat(self):
        a=self.replay([canonical('one'),canonical('two')]);b=self.replay([canonical('one'),canonical('two')]);self.assertEqual(a['failed'],0,a['cases']);self.assertEqual(a['cases'],b['cases']);self.assertEqual(a['cases'][0]['plan'],a['cases'][1]['plan'])
    def test_actual_native_moving_trace_all_observed_rows(self):
        profile=json.loads((BUILD/'profile.json').read_text())['moving'];self.assertEqual(sim.common.sha(NATIVE_LOG),profile['native_log_sha256'])
        traces=[json.loads(line.split('[EngagedMovementNativeTrace] ',1)[1]) for line in NATIVE_LOG.read_text(errors='replace').splitlines() if '[EngagedMovementNativeTrace] ' in line]
        self.assertEqual(len(traces),1);trace=traces[0];self.assertEqual(trace['profile'],'IsolatedEngagementChaseDeathTraceV1');self.assertTrue(trace['rows'][-1]['chainComplete'])
        p=self.plan(native_chain(len(trace['rows'])));self.assertEqual(len(p['frames']),len(trace['rows']));max_error=0.0;first=(trace['rows'][0]['targetIndex'],trace['rows'][0]['targetVersion']);second=next((r['targetIndex'],r['targetVersion']) for r in trace['rows'] if (r['targetIndex'],r['targetVersion']) not in (first,(0,0)))
        for expected,actual in zip(trace['rows'],p['frames']):
            u={u['id']:u for u in actual['units']};a=u['actor'];self.assertEqual(actual['frame'],expected['frame']);self.assertEqual(a['health'],expected['actorHp']);self.assertEqual(u['first']['health'],expected['firstHp']);self.assertEqual(u['second']['health'],expected['secondHp'])
            self.assertEqual([a['cell_x'],a['cell_z']],expected['actorCell']);self.assertEqual(a['has_engage'],expected['engaged']);self.assertEqual(a['has_path_request'],expected['pathRequest'])
            self.assertEqual(u['first']['death']['PoseFrozen']!=0,expected['firstFrozen']);self.assertEqual(u['second']['death']['PoseFrozen']!=0,expected['secondFrozen'])
            target={first:'first',second:'second',(0,0):''}[(expected['targetIndex'],expected['targetVersion'])];self.assertEqual(a['target'],target)
            for observed,want in (([a['x'],a['y'],a['z']],expected['actorPosition']),([u['first']['x'],u['first']['y'],u['first']['z']],expected['firstPosition']),([u['second']['x'],u['second']['y'],u['second']['z']],expected['secondPosition']),(a['cached_target'],expected['cachedTargetPosition'])):
                for x,y in zip(observed,want):
                    max_error=max(max_error,abs(x-y));self.assertAlmostEqual(x,y,delta=.00002,msg=f"frame {actual['frame']}: {observed} != {want}")
        (self.output/'native-comparison.json').write_text(json.dumps(dict(rows=len(trace['rows']),discrete_fields_equal=True,positions_within_tolerance=True,max_position_error=max_error,tolerance=.00002,scope='NativeSortedSubset_NoPathOrObjectives',mission_qualified=False),indent=2))
    def test_pending_job_wait_and_cancel_do_not_apply_early(self):
        c=canonical();c['frames']=c['frames'][:4];c['expect']=dict(frozen=False);c['frames'][1]['path_job_completed']=False;p=self.plan(c)
        self.assertEqual(p['frames'][0]['units'][0]['x'],1.5);self.assertEqual(p['frames'][1]['units'][0]['x'],1.5);self.assertTrue(any(e.get('kind')=='path_gate' and e['gate']=='WaitPending' for e in p['frames'][1]['events']))
        c=canonical('cancel');c['frames']=c['frames'][:4];c['expect']=dict(frozen=False);c['frames'][1]['path_play_requested']=False;p=self.plan(c)
        self.assertEqual(p['frames'][1]['pending_routes'],0);self.assertFalse(any(e.get('kind')=='path_apply' for e in p['frames'][1]['events']));self.assertEqual(p['frames'][2]['units'][0]['x'],1.5)
    def test_acquisition_clears_pending_request_and_stale_apply_is_ignored(self):
        c=canonical();c['units'][1]['actor'].update(x=4.5,cell_x=4);c['frames']=c['frames'][:3];c['expect']=dict(frozen=False);p=self.plan(c)
        self.assertFalse(p['frames'][0]['units'][0]['has_path_request']);self.assertFalse(p['frames'][0]['units'][0]['has_path_follow'])
        applied=[e for _,e in self.events(p,'path_apply')];self.assertTrue(applied);self.assertEqual(applied[0]['intent']['Disposition'],'Ignore')
    def test_corpse_freezes_before_offscreen_destruction(self):
        c=canonical()
        for f in c['frames']:f['camera_valid']=True
        p=self.plan(c);actions=[(f['frame'],e['action']) for f,e in self.events(p,'death_finalize') if e['actor']=='guard'];self.assertEqual([x[1] for x in actions],['Freeze','Remove']);self.assertGreater(actions[1][0],actions[0][0]);self.assertTrue(next(u for u in p['units'] if u['id']=='guard')['destroyed'])
    def test_future_state_unsupported_manual_goals_and_features_reject(self):
        cases=[]
        for i in range(11):
            c=canonical(str(i))
            if i==0:c['frames'][0]['future_positions']=[]
            if i==1:c['units'][0]['movement']['manual']=True
            if i==2:c['units'][0]['movement']['lifecycle_profile']='carrier'
            if i==3:c['map']['surface_cell']=[]
            if i==4:c['units'][0]['actor']['cell_x']=2
            if i==5:c['units'][1]['movement'].update(request=dict(x=9,z=2),unit_target=dict(x=9,z=2));c['units'][1]['actor']['acquisition']['path_request']=True
            if i==6:c['map']['dynamic_blocked'][2*16+10]=1
            if i==7:c['units'][0]['actor']['acquisition']['scan']=dict(CenterX=1,CenterZ=2,RadiusCells=3,HasStarted=1,EngageDetectedTargets=1)
            if i==8:c['profile_sha256']='0'*64
            if i==9:c['query_orders']['spatial'].pop()
            if i==10:c['units'][1]['actor'].update(x=1.5,cell_x=1)
            cases.append(c)
        r=self.replay(cases);self.assertEqual(r['failed'],len(cases),r['cases'])
    def test_unreachable_route_abandons_and_unarmed_nonlethal_hit_rejects_flee(self):
        c=canonical();c['frames']=c['frames'][:4];c['expect']=dict(frozen=False)
        for z in range(1,4):
            for x in range(0,3):
                if (x,z)!=(1,2):c['map']['walkable'][z*16+x]=0
        p=self.plan(c);self.assertTrue(any(e['intent']['Disposition']=='Abandon' for _,e in self.events(p,'path_apply')));self.assertFalse(p['units'][0]['has_path_request']);self.assertFalse(p['units'][0]['has_path_follow'])
        c=canonical('flee');guard=c['units'][1];guard['actor'].update(has_engage=True,target='actor');guard['movement'].update(initial_engagement=dict(is_commanded=1),speed=0);c['units'][0]['actor']['weapon']['Damage']=10
        r=self.replay([c]);self.assertEqual(r['failed'],1);self.assertIn('flee',r['cases'][0]['error'])
    def test_120_synthetic_moving_configs_repeat_identically(self):
        cases=[]
        for i in range(120):
            c=canonical(str(i));c['units'][0]['movement']['speed']=2+(i%4)*.5;c['units'][1]['actor'].update(health=10+(i%10)*10,x=6.5+i%3,cell_x=6+i%3);c['units'][3]['actor']['health']=100+i;cases.append(c)
        a=self.replay(cases);b=self.replay(cases);self.assertEqual(a['passed'],120,a['cases'][:1]);self.assertEqual(a['cases'],b['cases']);self.assertFalse(a['full_battle_simulated']);self.assertFalse(a['mission_qualified'])
        (self.output/'benchmark.json').write_text(json.dumps(dict(synthetic_configs=120,first_seconds=a['elapsed_seconds'],repeat_seconds=b['elapsed_seconds'],identical=True,mission_wins_claimed=0),indent=2))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--build',required=True,type=Path);parser.add_argument('--output',required=True,type=Path);parser.add_argument('--native-log',required=True,type=Path)
    args=parser.parse_args();BUILD=args.build.resolve();NATIVE_LOG=args.native_log.resolve();OUTPUT=sim.common.new_output(PROJECT,args.output)
    if not json.loads((BUILD/'profile.json').read_text()).get('moving',{}).get('enabled'):raise SystemExit('Verified moving profile receipt required; no skipped/fabricated validation')
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(MovingWorldTests));print(f"[SkirmishMovingWorldReplayTests] result={'Passed' if result.wasSuccessful() else 'Failed'} tests={result.testsRun} skipped={len(result.skipped)} native_runs=0");raise SystemExit(0 if result.wasSuccessful() else 1)
