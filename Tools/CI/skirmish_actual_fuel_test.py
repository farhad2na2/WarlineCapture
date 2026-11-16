"""Test actual fuel boundary no-op certificates and exhaustive two-tail invariance in WorldReplay."""
import argparse,copy,hashlib,itertools,json,subprocess
from pathlib import Path
parser=argparse.ArgumentParser(description=__doc__)
for name in ('project','build','input','trace','output'):parser.add_argument('--'+name,type=Path,required=True)
args=parser.parse_args();root=args.project.resolve();out=args.output.resolve();out.mkdir(parents=True,exist_ok=False)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
packet_sha=sha(args.input);trace_sha=sha(args.trace);packet=json.loads(args.input.read_text());cases=packet['cases'];assert len(cases)==2
base=cases[0];prefix=base['storage_order_proof']['observed_prefix'];tail=base['storage_order_proof']['unordered_unvisited_tail']
assert len(prefix)==49 and len(tail)==2 and len(set(prefix+tail))==51
assert {tuple(c['storage_query_order']) for c in cases}=={tuple(prefix)+p for p in itertools.permutations(tail)}
assert {c['storage_order_proof']['completion_index'] for c in cases}=={0,1}
def normalized(case):
 c=copy.deepcopy(case);c.pop('id');c.pop('storage_query_order');c['storage_order_proof'].pop('completion_index');c['stores'].sort(key=lambda r:r['id']);c['expected']['stores'].sort(key=lambda r:r['id']);return c
assert normalized(cases[0])==normalized(cases[1]),'Only the unknown tail completion may differ'
assert all(c['authority']['evidence_sha256']==trace_sha and not c['storage_order_proof']['native_full_order_proven'] for c in cases)
def invoke(name,payload):
 path=out/(name+'.input.json');path.write_text(json.dumps(dict(schema_version=1,cases=payload),separators=(',',':'))+'\n')
 r=subprocess.run(['rtk','proxy','python3',str(root/'Tools/CI/skirmish_world_replay.py'),'run','--build',str(args.build),'--input',str(path),'--output',str(out/(name+'-run'))],capture_output=True,text=True,timeout=60)
 (out/(name+'.log')).write_text(r.stdout+r.stderr);report=json.loads((out/(name+'-run/report.json')).read_text());return r,report
r,actual=invoke('paired-exhaustive-tail-completions',cases);assert r.returncode==0 and actual['passed']==2
plans=[c['plan'] for c in actual['cases']]
assert all(p['native_full_storage_order_proven'] is False and p['universal_tail_invariance_proven'] is False for p in plans)
assert plans[0]['requested_bits']==plans[1]['requested_bits'] and plans[0]['unit_states']==plans[1]['unit_states']
state=lambda p:dict(requested_bits=p['requested_bits'],unit_states=p['unit_states'],stores=sorted(p['stores'],key=lambda s:s['id']))
assert state(plans[0])==state(plans[1])
air=next(i for i,a in enumerate(base['roster']) if a['components']['UnitAirMovement'] is not None)
foreign=next(i for i,a in enumerate(base['roster']) if a['role'] is None)
checks=[]
def run(name,mutate,expected):
 case=copy.deepcopy(base);mutate(case);r,d=invoke(name,[case]);error=d['cases'][0].get('error','');ok=r.returncode!=0 and expected in error
 checks.append(dict(id=name,passed=ok,error=error));assert ok,(name,error)
run('aircraft-lastcell-changed',lambda c:c['roster'][air]['components']['UnitFuelConsumptionState']['LastCell'].__setitem__('x',c['roster'][air]['components']['UnitFuelConsumptionState']['LastCell']['x']+1),'Moving aircraft fuel unsupported')
run('missing-actual-grid',lambda c:c.__setitem__('fuel_grids',[]),'Unique actual before-consumer grid')
run('ambiguous-actual-grid',lambda c:c['fuel_grids'].append(copy.deepcopy(c['fuel_grids'][0])),'Unique actual before-consumer grid')
run('aircraft-uninitialized',lambda c:c['roster'][air]['components']['UnitFuelConsumptionState'].__setitem__('Initialized',0),'Uninitialized aircraft')
run('moving-aircraft',lambda c:c['roster'][air]['components']['LocalTransform']['Position'].__setitem__('x',c['roster'][air]['components']['LocalTransform']['Position']['x']+2*c['captured_grid_config']['CellSize']),'Moving aircraft fuel unsupported')
run('missing-aircraft-transform',lambda c:c['roster'][air]['components'].__setitem__('LocalTransform',None),'Captured body prestate missing LocalTransform')
run('invented-foreign-role',lambda c:c['roster'][foreign].__setitem__('role',0),'Foreign role metadata must remain absent')
run('requested-fuel-assertion',lambda c:c['expected']['requested_bits'].__setitem__(1,c['expected']['requested_bits'][1]^1),'Expected poststate assertion differs: requested fuel')
run('aircraft-poststate-assertion',lambda c:c['expected']['unit_states'][air]['last_cell'].__setitem__(0,c['expected']['unit_states'][air]['last_cell'][0]+1),'Expected poststate assertion differs: LastCell')
run('store-poststate-assertion',lambda c:c['expected']['stores'][0]['value'].__setitem__('StoredFuelBarrels',c['expected']['stores'][0]['value']['StoredFuelBarrels']+1),'Expected storage assertion differs: StoredFuelBarrels')
run('unproven-full-order-promotion',lambda c:c['storage_order_proof'].__setitem__('native_full_order_proven',True),'Explicit partial native storage order proof')
run('changed-proven-prefix',lambda c:c['storage_order_proof']['observed_prefix'].__setitem__(0,c['storage_order_proof']['observed_prefix'][1]),'Exact observed prefix')
run('incomplete-global-query',lambda c:c['unit_query_order'].pop(),'Complete global fuel query membership/order')
run('unsupported-steelpush',lambda c:c['authority'].__setitem__('steel_push',True),'only migrated single-session ground physical fuel')
run('unsupported-legacy',lambda c:c['authority'].__setitem__('shared_supply_initialized',False),'only migrated single-session ground physical fuel')
run('lost-global-aircraft-fact',lambda c:c['authority'].__setitem__('contains_aircraft',False),'Global aircraft membership fact must be retained')
assert sha(args.input)==packet_sha and sha(args.trace)==trace_sha
report=dict(result='Passed',rejection_controls=checks,paired_exhaustive_tail_invariance_proven=True,tail_completions=2,full_native_storage_order_proven=False,individual_cases_claim_universal_invariance=False,query_rows=len(plans[0]['unit_states']),stores=len(plans[0]['stores']),aircraft_noop_certificates=len(plans[0]['certified_aircraft_noop_ids']),ground_rows=plans[0]['ground_kernel_rows'],derived_common_state_sha256=hashlib.sha256(json.dumps(state(plans[0]),sort_keys=True,separators=(',',':')).encode()).hexdigest(),original_trace_bytes_unchanged=True,native_runs=0,complete_frame_derived=False,mission_qualified=False)
(out/'result.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(dict(result='Passed',rejection_controls=len(checks),tail_completions=2,query_rows=report['query_rows'],stores=report['stores'],aircraft_noop_certificates=report['aircraft_noop_certificates'],native_full_order_proven=False)))
