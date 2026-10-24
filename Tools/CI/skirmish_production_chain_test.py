"""Validate continuous production state and reject the first unmodelled writer.

Uses the existing WorldReplay executable. Native later state is assertion-only;
mutating it must leave the worker request and computed output unchanged.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import subprocess

parser=argparse.ArgumentParser(description=__doc__)
for name in ('project','build','trace','baseline','output'):
    parser.add_argument('--'+name,type=Path,required=True)
args=parser.parse_args()
out=args.output.resolve();out.mkdir(parents=True,exist_ok=False)
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
trace_raw=args.trace.read_bytes();baseline_raw=args.baseline.read_bytes()
trace=json.loads(trace_raw)
base=dict(id='actual-continuous-production',kind='physical_production_chain',
    trace_path=str(args.trace.resolve()),trace_sha256=sha(args.trace),
    baseline_path=str(args.baseline.resolve()),baseline_sha256=sha(args.baseline),pairs=2,work_limit=102)
def invoke(name,row):
    existing=set(args.build.glob('production-evidence-*'))
    payload=out/(name+'.input.json');payload.write_text(json.dumps(dict(schema_version=1,cases=[row]),indent=2)+'\n')
    command=['rtk','proxy','python3',str(args.project/'Tools/CI/skirmish_world_replay.py'),'run',
        '--build',str(args.build),'--input',str(payload),'--output',str(out/(name+'-run'))]
    result=subprocess.run(command,capture_output=True,text=True,timeout=150)
    (out/(name+'.command.json')).write_text(json.dumps(command,indent=2)+'\n')
    (out/(name+'.log')).write_text(result.stdout+result.stderr)
    report=json.loads((out/(name+'-run/report.json')).read_text())
    evidence=set(args.build.glob('production-evidence-*'))-existing
    wire=None
    if evidence:
        assert len(evidence)==1
        directory=evidence.pop()
        wire=dict(request=sha(directory/'input.bin'),derived=sha(directory/'output.bin'))
    return result,report,wire

r,report,wire=invoke('actual-prefix-two',base)
assert r.returncode==0 and report['passed']==1
plan=report['cases'][0]['plan']
assert plan['pairs']==2 and plan['storage_transitions']==102 and plan['profile_transitions']==8
assert plan['worker_state_reseed_count']==0 and plan['later_native_states_are_assertions_only'] is True
assert plan['next_native_before_matches_computed_state'] is False and plan['remaining_pairs']==24
controls=[]
def reject(name,row,expected,same_wire=False,different_wire=False):
    r,report,actual=invoke(name,row)
    case=report.get('cases',[{}])[0];error=case.get('error',report.get('error',''))
    assert r.returncode!=0 and expected in error,(name,error)
    if same_wire:assert actual==wire,(name,'Later native assertions contaminated computation')
    if different_wire:assert actual and actual['request']!=wire['request'] and actual['derived']!=wire['derived']
    controls.append(dict(id=name,passed=True,error=error,computed_wire_unchanged=actual==wire))

row=copy.deepcopy(base);row.update(pairs=3,work_limit=153)
reject('third-tick-missing-ordered-writer',row,'before tick 2')
for name,index,same_wire in [('after-first-assertion',1,True),('later-before-assertion',2,True),('after-second-assertion',3,True),('initial-before-input',0,False)]:
    changed=copy.deepcopy(trace);store=changed['production_boundary_history'][index]['readonly_query_snapshot'][0]['storage'];store['Version']=(store['Version']+1)&0xffffffff
    path=out/(name+'.trace.json');path.write_text(json.dumps(changed)+'\n')
    row=copy.deepcopy(base);row.update(trace_path=str(path),trace_sha256=sha(path))
    reject(name,row,'Production state assertion differs',same_wire=same_wire,different_wire=not same_wire)
row=copy.deepcopy(base);row['work_limit']=101
reject('understated-work',row,'Production work budget exceeded')
row=copy.deepcopy(base);row['trace_sha256']='0'*64
reject('trace-hash',row,'Production trace hash differs')
changed=copy.deepcopy(trace);changed['production_boundary_history'][0]['actual_loop_order_proven']=True
path=out/'invented-order.trace.json';path.write_text(json.dumps(changed)+'\n')
row=copy.deepcopy(base);row.update(trace_path=str(path),trace_sha256=sha(path))
reject('invented-order',row,'Actual unordered production pair required')
changed=copy.deepcopy(trace);del changed['production_boundary_history'][0]['readonly_query_snapshot'][0]['storage']['Version']
path=out/'missing-field.trace.json';path.write_text(json.dumps(changed)+'\n')
row=copy.deepcopy(base);row.update(trace_path=str(path),trace_sha256=sha(path))
reject('missing-initial-component-field',row,'Missing fields: Version')
assert args.trace.read_bytes()==trace_raw and args.baseline.read_bytes()==baseline_raw
result=dict(result='Passed',continuous_pairs=2,storage_transitions=102,profile_transitions=8,
    controls=controls,request_sha256=wire['request'],derived_sha256=wire['derived'],
    native_snapshots_reseeded=0,next_writer_gap_rejected=True,original_trace_baseline_unchanged=True,
    native_runs=0,full_startup_derived=False,complete_frame_derived=False,mission_qualified=False)
(out/'result.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(result='Passed',continuous_pairs=2,rejection_controls=len(controls),storage_transitions=102,native_runs=0)))
