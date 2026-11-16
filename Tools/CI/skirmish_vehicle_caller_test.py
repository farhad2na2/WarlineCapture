"""Validate captured caller admission and assertion independence in the existing WorldReplay executable."""
import argparse,copy,hashlib,json,os,shutil,subprocess
from pathlib import Path
parser=argparse.ArgumentParser(description=__doc__)
for name in ('project','capture','build','output'):parser.add_argument('--'+name,type=Path,required=True)
args=parser.parse_args();root=args.project.resolve();source=args.capture.resolve();build=args.build.resolve();out=args.output.resolve();out.mkdir(exist_ok=False,parents=True);base=json.loads(source.read_text())
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
original_sha=sha(source)
for spec in base['before_grid_borrowed_facts']['buffers'].values():
 src=source.parent/spec['path'];assert sha(src)==spec['sha256'];shutil.copyfile(src,out/spec['path'])
key=str(base['grid_job_query_order_and_utility_calls'][0]['entity']['index'])+':'+str(base['grid_job_query_order_and_utility_calls'][0]['entity']['version'])
def actor(t,name):return next(a for a in t[name] if a['query_id']==key)
def nofield(t):del actor(t,'before_grid_roster')['components']['UnitMove']['Speed']
def badexpected(t):actor(t,'before_fuel_roster')['components']['LocalTransform']['Position']['x']+=2
def badbefore(t):actor(t,'before_grid_roster')['components']['LocalTransform']['Position']['x']+=.2
def nolookup(t):del t['before_grid_borrowed_facts']['full_live_units_snapshot'][0]['lookups']['UnitTarget']
def badmask(t):t['before_grid_borrowed_facts']['buffers']['walkable']['sha256']='0'*64
def nocleanup(t):t['cleanup_query_order'].pop()
def badindices(t):t['grid_job_query_order_and_utility_calls'][1]['query_index']=0
def poolcount(t):t['before_grid_borrowed_facts']['path_pool_count']+=1
def emptycommands(t):t['accepted_normal_commands']=[]
def badreceipt(t):t['accepted_normal_commands'][0]['verified_touch_release']=False
def snapshotfalse(t):t['before_grid_borrowed_facts']['live_query_snapshot_complete']=False
def barrierfalse(t):t['before_grid_borrowed_facts']['cleanup_scheduled_after_move']=False
def badcollision(t):
 name='control-blocked.u8';p=out/name;p.write_bytes(bytes([1])*(2048*1024));t['before_grid_borrowed_facts']['buffers']['dynamic_blocked']=dict(path=name,bytes=p.stat().st_size,sha256=sha(p))
def fifo(t):
 p=out/'fifo-mask';os.mkfifo(p);t['before_grid_borrowed_facts']['buffers']['walkable']=dict(path=p.name,bytes=2048*1024,sha256='1'*64)
def symlink(t):
 p=out/'symlink-mask';p.symlink_to(out/'forward-walkable.u8');t['before_grid_borrowed_facts']['buffers']['walkable']=dict(path=p.name,bytes=2048*1024,sha256=sha(out/'forward-walkable.u8'))
cases=[('actual-native-caller',None,None),('missing-nested-speed',nofield,'Missing fields: Speed'),('mutated-native-after-is-assertion',badexpected,'Derived caller differs'),('mutated-before-is-input',badbefore,'Derived caller differs'),('missing-lookup-presence',nolookup,'Missing fields: UnitTarget'),('mask-hash-drift',badmask,'Import bytes differ'),('missing-cleanup-index',nocleanup,'cleanup indices'),('duplicate-movement-index',badindices,'movement indices'),('path-pool-count',poolcount,'Path pool descriptor count'),('missing-normal-command',emptycommands,'ordinary movement'),('unverified-receipt',badreceipt,'ordinary movement'),('incomplete-live-snapshot',snapshotfalse,'borrowed snapshot'),('missing-cleanup-barrier',barrierfalse,'borrowed snapshot'),('blocked-before-mask-is-input',badcollision,'Derived caller differs'),('fifo-mask-rejected',fifo,'Bounded regular'),('symlink-mask-rejected',symlink,'symlink rejected')]
records=[]
for name,mutate,error in cases:
 t=copy.deepcopy(base)
 if mutate:mutate(t)
 trace=out/(name+'.trace.json');trace.write_text(json.dumps(t,separators=(',',':'))+'\n')
 case=dict(id=name,kind='vehicle_caller',capture=dict(path=str(trace),sha256=sha(trace)),require_ecb_playback=False,work_limit=100000)
 input=out/(name+'.input.json');input.write_text(json.dumps(dict(schema_version=1,cases=[case]))+'\n')
 result=subprocess.run(['rtk','proxy','python3',str(root/'Tools/CI/skirmish_world_replay.py'),'run','--build',str(build),'--input',str(input),'--output',str(out/(name+'-run'))],capture_output=True,text=True)
 (out/(name+'.log')).write_text(result.stdout+result.stderr);report=json.loads((out/(name+'-run/report.json')).read_text());row=report['cases'][0];passed=result.returncode==0 and row['status']=='passed' if error is None else result.returncode!=0 and row['status']=='failed' and error.lower() in json.dumps(row).lower()
 records.append(dict(id=name,passed=passed,exit=result.returncode,status=row['status'],error=row.get('error'),trace_sha256=sha(trace)))
# Explicit global playback and aggregate-work gates, without altering native evidence.
for name,modify,error in [('global-ecb-playback',lambda c:c.update(require_ecb_playback=True),'playback remains unsupported'),('insufficient-work',lambda c:c.update(work_limit=1),'work bound exceeded')]:
 case=dict(id=name,kind='vehicle_caller',capture=dict(path=str(source),sha256=sha(source)),require_ecb_playback=False,work_limit=100000);modify(case);input=out/(name+'.input.json');input.write_text(json.dumps(dict(schema_version=1,cases=[case]))+'\n');r=subprocess.run(['rtk','proxy','python3',str(root/'Tools/CI/skirmish_world_replay.py'),'run','--build',str(build),'--input',str(input),'--output',str(out/(name+'-run'))],capture_output=True,text=True);(out/(name+'.log')).write_text(r.stdout+r.stderr);report=json.loads((out/(name+'-run/report.json')).read_text());row=report['cases'][0];records.append(dict(id=name,passed=r.returncode!=0 and error.lower() in json.dumps(row).lower(),exit=r.returncode,error=row.get('error')))
# Aggregate reservation is checked before opening trace artifacts.
for name,missing,error in [('aggregate-caller-work',False,'Aggregate world work budget exceeded'),('missing-work-reservation',True,'key')]:
 case=dict(id=name,kind='vehicle_caller',capture=dict(path=str(source),sha256=sha(source)),require_ecb_playback=False,work_limit=1500000)
 if missing:del case['work_limit']
 payload=[case] if missing else [case,dict(case,id=name+'-second')]
 input=out/(name+'.input.json');input.write_text(json.dumps(dict(schema_version=1,cases=payload))+'\n')
 r=subprocess.run(['rtk','proxy','python3',str(root/'Tools/CI/skirmish_world_replay.py'),'run','--build',str(build),'--input',str(input),'--output',str(out/(name+'-run'))],capture_output=True,text=True)
 (out/(name+'.log')).write_text(r.stdout+r.stderr)
 report=out/(name+'-run/report.json');text=r.stdout+r.stderr+(report.read_text() if report.exists() else '')
 records.append(dict(id=name,passed=r.returncode!=0 and error.lower() in text.lower(),exit=r.returncode))
result=dict(result='Passed' if all(r['passed'] for r in records) else 'Failed',controls=len(records),records=records,native_runs=0,original_trace_unchanged=sha(source)==original_sha);(out/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result));raise SystemExit(0 if result['result']=='Passed' else 1)
