"""Multi-unit native scheduling and bounded world composition regressions."""
import argparse
import copy
import json
from pathlib import Path
import sys
import struct
import unittest
sys.dont_write_bytecode=True
import skirmish_world_replay as sim
import skirmish_moving_world_replay_test as moving
BUILD=OUTPUT=NATIVE_LOG=None
PROJECT=Path(__file__).resolve().parents[2]

def multi(c):
    c['kind']='multi_unit_world';c['profile']='IsolatedMultiUnitCombatScheduleV1'
    c['query_orders']['manual_pathfinding']=list(c['query_orders']['pathfinding']);c['query_orders']['retry']=list(c['query_orders']['pathfinding'])
    for u in c['units']:
        if u['movement'] is not None:u['movement']['retry']=dict(has_cooldown=False,resume_frame=0,manual_group_member=False)
    for i,f in enumerate(c['frames']):f.update(engine_frame=100+i,path_job_wall_seconds=.001,query_orders=copy.deepcopy(c['query_orders']))
    return c

def routing(name='routing',count=8):
    units=[moving.unit('u'+str(i),i+1,1,1,1+i,width=16,height=12) for i in range(count)]
    units += [moving.unit('home',count+1,1,0,0,'base',width=16,height=12),moving.unit('enemy',count+2,2,15,11,'base',width=16,height=12)]
    c=multi(moving.scenario(units,moving.map_facts(16,12),name,18))
    for u in units[:count]:
        u['movement'].update(manual=True,request=dict(x=12,z=5),unit_target=dict(x=12,z=5));u['actor']['acquisition'].update(manual_move=True,path_request=True,attack_move=False,auto_engage=0)
    for f in c['frames']:f['delta_time']=0
    return c

def battle(name='battle'):
    units=[]
    for i,z in enumerate([2,7,12]):
        a=moving.unit('squad'+str(i),i+1,1,1,z,width=20,height=15);a['movement'].update(request=dict(x=13,z=z),unit_target=dict(x=13,z=z));a['actor']['acquisition']['path_request']=True;units.append(a)
    for i,z in enumerate([2,7,12]):
        a=moving.unit('guard'+str(i),i+4,2,7,z,width=20,height=15);a['movement']['can_attack']=0;a['actor']['acquisition'].update(auto_engage=0,attack_move=False);a['actor']['weapon']['Damage']=0;units.append(a)
    units += [moving.unit('home',7,1,0,0,'base',width=20,height=15),moving.unit('enemy',8,2,16,7,'base',400,width=20,height=15)]
    c=multi(moving.scenario(units,moving.map_facts(20,15),name,100));c['expect']=dict(frozen=True,outcome='Victory',reason='MainBaseDestroyed',freeze_count=1);return c

def native_case(trace):
    width=trace['width'];height=trace['height'];units=[]
    identities={}
    for i,row in enumerate(trace['frames'][0]['units']):
        u=moving.unit(row['id'],row['index'],1,1,i+1,width=width,height=height);a=u['actor'];a['version']=row['version'];a['acquisition'].update(aggro_range_cells=3,attack_move=True);a['weapon'].update(Range=trace['range'],Damage=trace['damage'],CooldownSeconds=trace['cooldown'],TraceVisibleSeconds=0,TracerEveryNthShot=0,AttackAnimationSeconds=.25)
        u['movement'].update(speed=trace['moveSpeed'],walk_speed=0,arrive_distance=.05,chase_break_distance=30,death_seconds=.4,request=dict(x=trace['requestedX'],z=trace['requestedZ']),unit_target=dict(x=trace['requestedX'],z=trace['requestedZ']));a['acquisition']['path_request']=True;units.append(u);identities[(row['index'],row['version'])]=row['id']
    h=trace['frames'][0]['hostile'];u=moving.unit('hostile',h['index'],2,25,7,width=width,height=height);u['actor']['version']=h['version'];u['actor']['acquisition'].update(auto_engage=0,attack_move=False,aggro_range_cells=3);u['actor']['weapon'].update(Range=2,Damage=100,CooldownSeconds=.1,TraceVisibleSeconds=0,TracerEveryNthShot=0,AttackAnimationSeconds=.25);u['movement'].update(speed=4,walk_speed=0,arrive_distance=.05,chase_break_distance=30,death_seconds=.4,can_attack=0);units.append(u);identities[(h['index'],h['version'])]='hostile'
    free=max(i for i,v in identities)+100
    units += [moving.unit('home',free,1,0,0,'base',width=width,height=height),moving.unit('enemy',free+1,2,31,15,'base',width=width,height=height)]
    c=multi(moving.scenario(units,moving.map_facts(width,height),'native_multi',len(trace['frames'])))
    for z in range(trace['wallZMin'],trace['wallZMax']+1):i=z*width+trace['wallX'];c['map']['walkable'][i]=0;c['map']['dynamic_blocked'][i]=1
    c['map']['friendly_pass']=[255]*(width*height);c['map']['provenance']='native multi-unit trace flat surface fixture; added target-only bases outside acquisition range'
    all_ids=[u['actor']['id'] for u in units]
    def ordered(rows):
        prefix=[identities[(r['index'],r['version'])] for r in rows or []];return prefix+[name for name in all_ids if name not in prefix]
    for f,native in zip(c['frames'],trace['frames']):
        f.update(elapsed_time=native['elapsed'],delta_time=trace['delta'],engine_frame=native['engineFrame'],simulation_active=int(native['simulationActive']),path_play_requested=native['playRequested'])
        admissions=native['admission'];assert len(admissions)==1;f['path_job_completed']=admissions[0]['consumedCompleted'];budgets=native['budget'];assert len(budgets)<=1
        f['path_job_wall_seconds']=struct.unpack('<d',struct.pack('<q',budgets[0]['wallBits']))[0] if budgets else 0
        orders=f['query_orders'];orders['acquisition']=ordered(native['acquisitionOrder']);orders['grid_move']=ordered(native['movementOrder']);orders['attack']=ordered(native['attackOrder'])
        # Safe readonly pending input metadata supplies the actual admitted prefix.
        # Remaining identities are a synthetic tail, not a claimed full native query order.
        after=native['pathAfter'];before=native['pathBefore'];scheduled=admissions[0]['decision'] in ('Schedule','ApplyThenSchedule') and after['hasPending']
        prefix=after['orderedRequests'] if scheduled else native['requestQueryOrderBeforePathfinding']
        orders['pathfinding']=ordered(prefix);orders['manual_pathfinding']=ordered(prefix)
    return c,identities

class MultiUnitWorldTests(unittest.TestCase):
    def setUp(self):self.output=OUTPUT/self._testMethodName;self.output.mkdir();self.index=0
    def replay(self,cases):
        self.index+=1;p=self.output/f'input-{self.index}.json';p.write_text(json.dumps(dict(schema_version=1,cases=cases)));return sim.run(BUILD,p,self.output/f'run-{self.index}')
    def plan(self,c):
        r=self.replay([c]);self.assertEqual(r.get('failed'),0,r.get('cases',r));return r['cases'][0]['plan']
    events=staticmethod(moving.MovingWorldTests.events)
    def test_manual_priority_budget_growth_and_batch_reservations(self):
        c=routing();c['units'][0]['movement']['manual']=False;c['units'][0]['actor']['acquisition']['manual_move']=False;p=self.plan(c)
        scheduled=self.events(p,'path_schedule');self.assertEqual(scheduled[0][1]['actor'],'u1')
        batches={}
        for f,e in scheduled:batches.setdefault(f['frame'],[]).append(e)
        concurrent=[v for v in batches.values() if len(v)>1];self.assertTrue(concurrent)
        for batch in concurrent:self.assertEqual(len({(e['assigned']['x'],e['assigned']['y']) for e in batch}),len(batch))
        self.assertTrue(any(e['assigned']!=e['requested'] for _,e in scheduled));self.assertTrue(any(f['path_budget']['AdaptiveRequestsPerFrame']>1 for f in p['frames']))
    def test_blocked_desired_goal_uses_shared_seeded_ring_and_repeats(self):
        c=routing();c['map']['walkable'][5*16+12]=0;a=self.replay([c]);b=self.replay([c]);self.assertEqual(a['failed'],0,a['cases']);self.assertEqual(a['cases'],b['cases'])
        for _,e in self.events(a['cases'][0]['plan'],'path_schedule'):self.assertNotEqual(e['assigned'],e['requested'])
    def test_retry_expiry_restores_on_following_update_and_engine_frame(self):
        c=routing(count=1)
        for z in range(0,3):
            for x in range(0,3):
                if (x,z)!=(1,1):c['map']['walkable'][z*16+x]=0
        p=self.plan(c);retry=[(f,e) for f,e in self.events(p,'path_apply') if e['intent']['Disposition']=='Retry'];self.assertTrue(retry)
        resume=retry[0][1]['intent']['ResumeFrame'];cleared=[(f,e) for f,e in self.events(p,'manual_retry') if e['plan']['ClearCooldown']];self.assertEqual(cleared[0][0]['engine_frame'],resume);self.assertFalse(cleared[0][1]['plan']['RestoreRequest'])
        restores=[(f,e) for f,e in self.events(p,'manual_retry') if e['plan']['RestoreRequest']];self.assertEqual(restores[0][0]['engine_frame'],resume+1)
        c['id']='targetless';c['units'][0]['movement']['unit_target']=None;p=self.plan(c);self.assertFalse(p['units'][0]['has_unit_target']);self.assertFalse(any(e['plan']['RestoreRequest'] for _,e in self.events(p,'manual_retry')));self.assertEqual(len(self.events(p,'path_schedule')),1)
    def test_pending_wait_reduces_budget_and_cancel_preserves_requests(self):
        c=routing();c['frames'][3]['path_job_completed']=False;p=self.plan(c);self.assertEqual(p['frames'][3]['path_budget']['AdaptiveRequestsPerFrame'],1);self.assertTrue(p['frames'][3]['path_budget']['PendingBudgetReduced'])
        c=routing('cancel');c['frames'][3]['path_play_requested']=False;p=self.plan(c);self.assertEqual(p['frames'][3]['pending_routes'],0);self.assertFalse(any(e.get('kind')=='path_apply' for e in p['frames'][3]['events']))
    def test_acquisition_stale_apply_and_independent_reset(self):
        c=multi(moving.canonical());c['units'][1]['actor'].update(x=4.5,cell_x=4);c['frames']=c['frames'][:3];c['expect']=dict(frozen=False);p=self.plan(c);self.assertEqual(self.events(p,'path_apply')[0][1]['intent']['Disposition'],'Ignore')
        a=self.replay([routing('one'),routing('two')]);b=self.replay([routing('one'),routing('two')]);self.assertEqual(a['failed'],0,a['cases']);self.assertEqual(a['cases'],b['cases']);self.assertEqual(a['cases'][0]['plan'],a['cases'][1]['plan'])
    def test_several_squads_route_combat_resume_and_derived_objective(self):
        p=self.plan(battle());self.assertEqual(p['freeze_count'],1)
        for name in ['squad0','squad1','squad2']:
            shots=[f['frame'] for f,e in self.events(p,'attack') if e['actor']==name and e['decision']=='Shot'];self.assertTrue(shots)
            restored=[f for f,e in self.events(p,'invalidate') if e['actor']==name and e['intent']['RestorePathRequest'] and f['frame']>shots[0]];self.assertTrue(restored);restored=restored[0]
            rescheduled=[f['frame'] for f,e in self.events(p,'path_schedule') if e['actor']==name and f['frame']>=restored['frame']];self.assertTrue(rescheduled)
            start=next(u for u in restored['units'] if u['id']==name);resumed=[f for f,e in self.events(p,'grid_move') if e['actor']==name and f['frame']>rescheduled[0]];self.assertTrue(resumed)
            self.assertTrue(any(abs(next(u for u in f['units'] if u['id']==name)['x']-start['x'])>.1 for f in resumed))
        for u in p['units']:
            if u['id'].startswith('guard'):self.assertEqual(u['health'],0)
    def test_shared_cell_order_independence_includes_ally_referrals(self):
        units=[moving.unit('source',1,1,2,2),moving.unit('ally1',2,1,2,2),moving.unit('ally2',3,1,2,2),moving.unit('hostile',4,2,4,2),moving.unit('home',5,1,0,0,'base'),moving.unit('enemy',6,2,15,4,'base')]
        for u in units[:4]:u['actor']['acquisition']['attack_move']=False
        for u in units[1:3]:u['actor'].update(has_engage=True,target='hostile');u['movement'].update(can_attack=0,initial_engagement=dict(is_commanded=0))
        units[3]['movement']['can_attack']=0;c=multi(moving.scenario(units,moving.map_facts(),'order',1));c['frames'][0]['delta_time']=0
        a=self.plan(c);self.assertEqual(a['units'][0]['target'],'hostile');c['frames'][0]['query_orders']['spatial'].reverse();b=self.plan(c);self.assertEqual(a['units'],b['units'])
        # Two allied referrals to distinct hostile identities have no such proof.
        c['units'][2]['actor']['target']='enemy';r=self.replay([c]);self.assertEqual(r['failed'],1);self.assertIn('distinct eligible',r['cases'][0]['error'])
    def test_actual_native_multi_unit_trace_every_row_and_applied_path(self):
        profile=json.loads((BUILD/'profile.json').read_text())['multi_unit'];self.assertEqual(sim.common.sha(NATIVE_LOG),profile['native_log_sha256'])
        traces=[json.loads(line.split('[MultiUnitNativeTrace] ',1)[1]) for line in NATIVE_LOG.read_text(errors='replace').splitlines() if '[MultiUnitNativeTrace] ' in line];self.assertEqual(len(traces),1);trace=traces[0]
        self.assertEqual(trace['profile'],'IsolatedMultiUnitCombatTraceV1');self.assertEqual(trace['scope'],'NativeSortedSubset_ExplicitHandleDispatch');self.assertTrue(trace['chainComplete']);self.assertFalse(trace['nativePendingCompletionForced']);c,ids=native_case(trace);p=self.plan(c);self.assertEqual(len(p['frames']),len(trace['frames']));max_error=0.0;paths=0
        for native,actual in zip(trace['frames'],p['frames']):
            self.assertEqual(actual['frame'],native['tick']);self.assertEqual(actual['engine_frame'],native['engineFrame']);after=native['pathAfter'];self.assertEqual(actual['pending_routes'],after['pendingCount']);self.assertEqual(actual['pending_budget'],after['pendingBudget']);self.assertEqual(actual['pending_schedule_frame'],after['scheduleFrame']);self.assertEqual(actual['path_budget']['AdaptiveRequestsPerFrame'],after['adaptiveBudget']);self.assertEqual(actual['pending_requests'],[ids[(r['index'],r['version'])] for r in after['orderedRequests']])
            self.assertEqual([e['gate'] for e in actual['events'] if e.get('kind')=='path_gate'],[a['decision'] for a in native['admission']]);units={u['id']:u for u in actual['units']}
            budget_events=[e for e in actual['events'] if e.get('kind')=='path_budget_completion'];self.assertEqual(len(budget_events),len(native['budget']))
            # JsonUtility omits the non-Serializable nested budget structs in this native receipt.
            # Compare captured adaptive snapshots and exact consumed reducer inputs; do not claim full-state trace parity.
            for observed_budget,expected_budget in zip(budget_events,native['budget']):
                self.assertEqual(observed_budget['pending_frames'],expected_budget['pendingFrames']);self.assertEqual(observed_budget['request_count'],expected_budget['requestCount']);self.assertEqual(observed_budget['manual_count'],expected_budget['manualCount']);self.assertEqual(observed_budget['scheduled_budget'],expected_budget['pendingBudget']);self.assertEqual(observed_budget['wall_seconds'],struct.unpack('<d',struct.pack('<q',expected_budget['wallBits']))[0]);self.assertEqual(expected_budget['vehicleCount'],0)
            for expected in native['units']+[native['hostile']]:
                observed=units[expected['id']];msg=f"frame {native['tick']} {expected['id']}";self.assertEqual(observed['health'],expected['hp'],msg);self.assertEqual([observed['cell_x'],observed['cell_z']],[expected['cellX'],expected['cellZ']],msg)
                for ours,theirs in [('has_path_request','request'),('has_path_follow','follow'),('has_engage','engage'),('has_cooldown','cooldown'),('manual','manual'),('has_long_distance','longDistance')]:self.assertEqual(observed[ours],expected[theirs],msg+' '+ours)
                self.assertEqual(observed['death']['PoseFrozen']!=0,expected['deathFrozen'],msg);target=ids.get((expected['targetIndex'],expected['targetVersion']),'');self.assertEqual(observed['target'] if observed['has_engage'] else '',target,msg)
                if expected['cooldown']:self.assertEqual(observed['resume_frame'],expected['resumeFrame'],msg)
                if expected['request']:self.assertEqual([observed['request_cell']['x'],observed['request_cell']['y']],[expected['requestX'],expected['requestZ']],msg)
                if expected['range']:
                    paths+=1;self.assertTrue(observed['has_path_follow'],msg);self.assertEqual(observed['path_start'],expected['pathStart'],msg);self.assertEqual(observed['path_length'],expected['pathLength'],msg);self.assertEqual(observed['path_cells'],expected['pathCells'],msg)
                if expected['follow']:self.assertTrue(expected['range'],msg);self.assertEqual(observed['path_index'],expected['pathIndex'],msg)
                for ours,theirs in [('x','x'),('y','y'),('z','z'),('current_speed','speed'),('stall_seconds','stall')]:
                    error=abs(observed[ours]-expected[theirs]);max_error=max(max_error,error);self.assertAlmostEqual(observed[ours],expected[theirs],delta=.00002,msg=msg+' '+ours)
            self.assertEqual(units['home']['health'],100);self.assertEqual(units['enemy']['health'],100);self.assertEqual(actual['freeze_count'],0);self.assertFalse(any(u['target'] in ('home','enemy') for u in actual['units']))
        (self.output/'native-comparison.json').write_text(json.dumps(dict(rows=len(trace['frames']),applied_path_observations=paths,discrete_fields_equal=True,positions_within_tolerance=True,max_position_error=max_error,tolerance=.00002,scope=trace['scope'],ordering_provenance='actual admitted prefix; unobserved tail synthetic',native_budget_full_state_serialized=False,mission_qualified=False),indent=2))
    def test_multi_profile_strict_controls(self):
        cases=[]
        for i in range(7):
            c=routing(str(i))
            if i==0:c['frames'][0].pop('engine_frame')
            if i==1:c['frames'][1]['engine_frame']=1
            if i==2:c['frames'][0]['path_job_wall_seconds']=-1
            if i==3:c['frames'][0]['query_orders']['manual_pathfinding'].pop()
            if i==4:c['units'][0]['movement']['retry']['resume_frame']=-1
            if i==5:c['profile_sha256']='0'*64
            if i==6:c['frames'][0]['future_positions']=[]
            cases.append(c)
        r=self.replay(cases);self.assertEqual(r['failed'],len(cases),r['cases'])
    def test_120_varied_synthetic_multi_unit_configs_repeat(self):
        cases=[]
        for i in range(120):
            c=battle(str(i));c['units'][7]['actor']['health']=200+i;c['units'][0]['movement']['speed']=3+(i%4)*.25;cases.append(c)
        first=repeat=0.0;passed=0
        for start in range(0,120,60):
            a=self.replay(cases[start:start+60]);b=self.replay(cases[start:start+60]);self.assertEqual(a['passed'],60,a['cases'][:1]);self.assertEqual(a['cases'],b['cases']);passed+=a['passed'];first+=a['elapsed_seconds'];repeat+=b['elapsed_seconds']
        (self.output/'benchmark.json').write_text(json.dumps(dict(synthetic_configs=passed,batches=2,first_seconds=first,repeat_seconds=repeat,identical=True,mission_wins_claimed=0),indent=2))

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--build',required=True,type=Path);parser.add_argument('--output',required=True,type=Path);parser.add_argument('--native-log',required=True,type=Path)
    args=parser.parse_args();BUILD=args.build.resolve();moving.BUILD=BUILD;NATIVE_LOG=args.native_log.resolve();OUTPUT=sim.common.new_output(PROJECT,args.output)
    if not json.loads((BUILD/'profile.json').read_text()).get('multi_unit',{}).get('enabled'):raise SystemExit('Verified multi-unit receipt required')
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(MultiUnitWorldTests));print(f"[SkirmishMultiUnitWorldReplayTests] result={'Passed' if result.wasSuccessful() else 'Failed'} tests={result.testsRun} skipped={len(result.skipped)} native_runs=0");raise SystemExit(0 if result.wasSuccessful() else 1)
