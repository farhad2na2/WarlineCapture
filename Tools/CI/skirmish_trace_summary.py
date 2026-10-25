#!/usr/bin/env python3
"""Passive, bounded JSONL diagnostic; it never changes simulation inputs or clocks."""
import argparse, hashlib, json, math, os, sys
from collections import Counter,deque
MAX_LINE=8*1024*1024; MAX_FILE=8*1024*1024; MAX_ROWS=250000; MAX_ERRORS=50; MAX_ACTORS=10000; MAX_KEY=256; MAX_KEYS=1024; MAX_WARNINGS=50; LAST=12
STATIONARY_SECONDS=10.; STATIONARY_SNAPSHOTS=3
class DuplicateKey(ValueError): pass
def pairs(xs):
 d={}
 for k,v in xs:
  if k in d: raise DuplicateKey('duplicate key %r'%k)
  d[k]=v
 return d
def constant(x): raise ValueError('nonfinite JSON constant '+x)
def decimal(raw):
 value=float(raw)
 if not math.isfinite(value):raise ValueError('nonfinite JSON number '+raw)
 return value
def load(raw): return json.loads(raw,object_pairs_hook=pairs,parse_constant=constant,parse_float=decimal)
def err(o,s):
 if len(o['input_errors'])<MAX_ERRORS:o['input_errors'].append(s)
def finite(x):
 try:return isinstance(x,(int,float)) and not isinstance(x,bool) and math.isfinite(x)
 except OverflowError:return False
def integer(x): return isinstance(x,int) and not isinstance(x,bool)
def capped(o,group,key):
 d=o['commands'][group]
 if key in d:d[key]+=1
 elif len(d)<MAX_KEYS:d[key]=1
 else:err(o,'%s key cap exceeded'%group)
def warning(o,kind,item):
 if len(o['warnings'][kind])<MAX_WARNINGS:o['warnings'][kind].append(item)
 else:err(o,'%s warning cap exceeded'%kind)
def stable_json(path,name,o,optional=False):
 if optional and not path:return None
 try:
  before=os.stat(path);h=hashlib.sha256();chunks=[];n=0
  with open(path,'rb') as f:
   for b in iter(lambda:f.read(1024*1024),b''):
    n+=len(b);h.update(b)
    if n>MAX_FILE:raise ValueError('file exceeds %d bytes'%MAX_FILE)
    chunks.append(b)
  after=os.stat(path);o['inputs'][name]={'path':os.path.abspath(path),'sha256':h.hexdigest(),'bytes':n,'stable':(before.st_size,before.st_mtime_ns)==(after.st_size,after.st_mtime_ns)}
  if not o['inputs'][name]['stable']:raise ValueError('file changed while read')
  v=load(b''.join(chunks))
  if not isinstance(v,dict):raise ValueError('top-level is not object')
  return v
 except (OSError,ValueError,UnicodeDecodeError,RecursionError) as x:err(o,'%s malformed/unavailable: %s'%(name,x));return None
def stream(path,name,o):
 try: before=os.stat(path);f=open(path,'rb')
 except OSError as x:
  err(o,'%s unavailable: %s'%(name,x));o['inputs'][name]={'path':os.path.abspath(path),'available':False};return
 h=hashlib.sha256();rows=0
 try:
  while True:
   raw=f.readline(MAX_LINE+1)
   if not raw:break
   h.update(raw);rows+=1
   if len(raw)>MAX_LINE and not raw.endswith(b'\n'):
    err(o,'%s line %d exceeds %d bytes'%(name,rows,MAX_LINE))
    while raw and not raw.endswith(b'\n'):raw=f.readline(MAX_LINE+1);h.update(raw)
    continue
   if rows>MAX_ROWS:
    err(o,'%s exceeds %d rows'%(name,MAX_ROWS))
    for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    break
   try:yield rows,load(raw)
   except (ValueError,UnicodeDecodeError,RecursionError) as x:err(o,'%s line %d malformed: %s'%(name,rows,x))
 finally:
  f.close()
  try:after=os.stat(path);stable=(before.st_size,before.st_mtime_ns)==(after.st_size,after.st_mtime_ns)
  except OSError as x:
   err(o,'%s unavailable after read: %s'%(name,x));after=before;stable=False
  o['inputs'][name]={'path':os.path.abspath(path),'sha256':h.hexdigest(),'bytes':after.st_size,'stable':stable,'rows_read':rows}
  if not stable:err(o,'%s changed while read'%name)
def ident(v,label,no,o):
 if not isinstance(v,str) or not v or len(v)>MAX_KEY:err(o,'%s line %d invalid world_id'%(label,no));return None
 return v
def worlds(row,r,label,no,o,seen):
 a=row.get('world_id');b=r.get('world_id') if r else None
 if a is None and b is None:err(o,'%s line %d missing world identity'%(label,no))
 for x in (a,b):
  if x is not None:
   x=ident(x,label,no,o)
   if x:
    if x not in seen and len(seen)>=MAX_KEYS:err(o,'world ID cap exceeded')
    else:seen.add(x)
 if a is not None and b is not None and a!=b:err(o,'%s line %d row/receipt world mismatch'%(label,no))
def coord(v,label,no,o):
 if not isinstance(v,dict):err(o,'%s line %d coordinate not object'%(label,no));return None
 x,y=v.get('x'),v.get('y')
 if not integer(x) or not integer(y):err(o,'%s line %d coordinate invalid'%(label,no));return None
 return x,y
def pending(actor,no,o):
 comp=actor.get('components')
 if not isinstance(comp,dict):err(o,'validator line %d components not object'%no);return None
 grid=comp.get('Game.Components.UnitGrid')
 if not isinstance(grid,dict):err(o,'validator line %d UnitGrid not object'%no);return None
 cell=coord(grid.get('Cell'),'validator current cell',no,o)
 if cell is None:return None
 goals=[];path=actor.get('path_state')
 if path is not None:
  if not isinstance(path,dict):err(o,'validator line %d path_state not object/null'%no)
  else:
   n=path.get('range_length')
   if not integer(n):err(o,'validator line %d path range_length invalid'%no)
   elif n>0:
    for field in ('next_cell','final_cell'):
     if field in path:
      c=coord(path[field],'validator '+field,no,o)
      if c is not None and c!=cell:goals.append(c)
 req=comp.get('Game.Components.UnitPathRequest')
 if req is not None:
  if not isinstance(req,dict):err(o,'validator line %d UnitPathRequest not object'%no)
  elif 'Goal' not in req:err(o,'validator line %d UnitPathRequest Goal missing'%no)
  else:
   c=coord(req['Goal'],'validator request Goal',no,o)
   if c is not None and c!=cell:goals.append(c)
 return goals[0] if goals else None
def position(comp,no,o):
 t=comp.get('Unity.Transforms.LocalTransform')
 if not isinstance(t,dict):err(o,'validator line %d LocalTransform not object'%no);return None
 p=t.get('Position')
 if not isinstance(p,dict):err(o,'validator line %d Position not object'%no);return None
 xy=p.get('x'),p.get('z')
 if not all(finite(x) for x in xy):err(o,'validator line %d position invalid'%no);return None
 return xy
ACTIVE_RESERVATION_PHASES={1:'Reserved',2:'Producing',3:'Live'}
PHASE_NAMES={1:'Reserved',2:'Producing',3:'Live',4:'Released',5:'Cancelled',6:'Lost'}
PHASE_PIN_SUFFIX='assets/game/scripts/skirmish/contracts/skirmishproductioncontracts.cs'
PHASE_PIN_SHA256='5f8a981caa9873d573697ad035196a33541af39afa3c04f7af01d2ad5e88f73d'
def phase_pin_check(check,receipt):
 pin=check['phase_mapping_pin'];pin['expected_sha256']=PHASE_PIN_SHA256
 if not isinstance(receipt,dict):pin['status']='Unsupported';pin['reason']='missing receipt';return
 maps=[]
 for name in ('source_and_reference_pins','input_and_runtime_pins'):
  value=receipt.get(name)
  if value is not None:
   if not isinstance(value,dict):pin['status']='Unsupported';pin['reason']='invalid '+name;return
   maps.append((name,value))
 matches=[]
 for name,values in maps:
  for path,sha in values.items():
   if isinstance(path,str) and (path.replace('\\','/').lower()==PHASE_PIN_SUFFIX or path.replace('\\','/').lower().endswith('/'+PHASE_PIN_SUFFIX)):matches.append((name,path,sha))
 if len(matches)!=1:pin['status']='Unsupported';pin['reason']='missing or conflicting phase source pin';pin['matches']=len(matches);return
 name,path,sha=matches[0];pin['container']=name;pin['path']=path;pin['observed_sha256']=sha
 if not isinstance(sha,str) or sha.lower()!=PHASE_PIN_SHA256:pin['status']='Unsupported';pin['reason']='phase source hash differs';return
 pin['status']='Passed';pin['reason']='unique expected source pin'
def phase_value(value,no,o):
 if integer(value):return value
 if isinstance(value,dict) and integer(value.get('value')) and isinstance(value.get('name'),str) and PHASE_NAMES.get(value['value'])==value['name']:return value['value']
 err(o,'validator line %d reservation phase invalid'%no);return None
def reservation_check(o,row,actors,no):
 check=o['checks']['reservation_owner_upper_bounds'];facts=row.get('session_outcome_facts')
 if facts is None:check['unsupported_snapshots']+=1;return
 if not isinstance(facts,dict):err(o,'validator line %d session facts not object'%no);check['unsupported_snapshots']+=1;return
 expanded=facts.get('Game.Components.SkirmishExpandedSessionComponent')
 if expanded is None:check['unsupported_snapshots']+=1;return
 if not isinstance(expanded,dict) or not isinstance(expanded.get('SessionId'),str) or not expanded['SessionId'] or len(expanded['SessionId'])>MAX_KEY:
  err(o,'validator line %d expanded-session facts invalid'%no);check['unsupported_snapshots']+=1;return
 session=expanded['SessionId'];receipts=row.get('reservations')
 if receipts is None:check['unsupported_snapshots']+=1;return
 if not isinstance(receipts,list):err(o,'validator line %d reservations not list'%no);check['unsupported_snapshots']+=1;return
 if len(receipts)>MAX_ACTORS:err(o,'validator line %d reservation cap exceeded'%no);check['unsupported_snapshots']+=1;return
 check['input_snapshots']+=1;by_id={};active=[];bad_receipt=False
 for receipt in receipts:
  if not isinstance(receipt,dict):err(o,'validator line %d reservation not object'%no);bad_receipt=True;continue
  rid=receipt.get('ReservationId');faction=receipt.get('FactionId');phase=phase_value(receipt.get('Phase'),no,o)
  values=[receipt.get(k) for k in ('MemberCount','DeliveredMembers','RemainingMembers')]
  if not integer(rid) or rid<=0 or not integer(faction) or phase is None or not all(integer(v) for v in values):err(o,'validator line %d reservation identity/count invalid'%no);bad_receipt=True;continue
  if rid in by_id:
   check['duplicate_receipt_ids']+=1;check['status']='Failed'
   if len(check['violations'])<MAX_WARNINGS:check['violations'].append({'line':no,'kind':'duplicate_receipt_id','reservation_id':rid})
   continue
  by_id[rid]=receipt
  if phase in ACTIVE_RESERVATION_PHASES:active.append(receipt)
  elif phase in (4,5,6):check['skipped_closed_receipts']+=1
  else:check['unsupported_snapshots']+=1
 if bad_receipt:check['unsupported_snapshots']+=1;return
 owners=Counter();bad_owner=False;missing_owner=False
 for actor in actors:
  if not isinstance(actor,dict):continue
  comp=actor.get('components')
  if not isinstance(comp,dict):continue
  owned=comp.get('Game.Components.SkirmishAttemptOwnedComponent')
  if owned is None:missing_owner=True;continue
  if not isinstance(owned,dict):err(o,'validator line %d owned component not object'%no);bad_owner=True;continue
  rid=owned.get('ReservationId')
  if not integer(rid) or rid<0:err(o,'validator line %d owned ReservationId invalid'%no);bad_owner=True;continue
  if rid==0:continue
  structure,released=owned.get('IsStructure'),owned.get('CapacityReleased')
  faction,owner_session=owned.get('FactionId'),owned.get('SessionId')
  if not integer(structure) or not integer(released) or not integer(faction) or not isinstance(owner_session,str) or not owner_session or len(owner_session)>MAX_KEY:err(o,'validator line %d owned identity/release invalid'%no);continue
  if structure!=0 or released!=0:continue
  owners[(owner_session,faction,rid)]+=1
 if bad_owner:check['malformed_owned_actors']+=1;check['unsupported_snapshots']+=1;return
 if missing_owner:check['missing_owned_actors']+=1;check['unsupported_snapshots']+=1;return
 for (owner_session,faction,rid),count in owners.items():
  receipt=by_id.get(rid)
  if receipt is None or owner_session!=session or faction!=receipt.get('FactionId'):
   check['unknown_active_owners']+=1;check['status']='Failed'
   if len(check['violations'])<MAX_WARNINGS:check['violations'].append({'line':no,'kind':'unknown_active_owner','reservation_id':rid,'faction_id':faction,'owner_count':count})
   continue
 for receipt in active:
  member,delivered,remaining=[receipt[k] for k in ('MemberCount','DeliveredMembers','RemainingMembers')];rid=receipt['ReservationId'];faction=receipt['FactionId']
  count=owners[(session,faction,rid)]
  check['checked_receipts']+=1
  if remaining<0 or delivered<0 or member<0 or remaining>member or delivered>member or count>remaining or count>delivered:
   check['status']='Failed'
   if len(check['violations'])<MAX_WARNINGS:check['violations'].append({'line':no,'reservation_id':rid,'faction_id':faction,'member_count':member,'delivered_members':delivered,'remaining_members':remaining,'active_unreleased_nonstructure_owners':count})
def analyze(a):
 o={'schema_version':4,'kind':'SkirmishPassiveTraceSummary','analyzer_success':False,'source_result':None,'source_receipt':None,'inputs':{},'input_errors':[],'commands':{'executed':0,'accepted':0,'rejected':0,'unknown_acceptance':0,'reasons':{},'source_apis':{},'last':[]},'checks':{'reservation_owner_upper_bounds':{'status':'Unsupported','input_snapshots':0,'unsupported_snapshots':0,'checked_receipts':0,'skipped_closed_receipts':0,'duplicate_receipt_ids':0,'unknown_active_owners':0,'malformed_owned_actors':0,'missing_owned_actors':0,'phase_mapping_pin':{'status':'Unsupported','expected_sha256':PHASE_PIN_SHA256,'observed_sha256':None,'container':None,'path':None,'reason':'not checked'},'violations':[],'limitations':['Upper-bound only; does not prove full conservation, accepted counts, or treat RemainingMembers as undelivered. Dead actors count until CapacityReleased=1. Closed Cancelled/Lost/Released receipts are skipped. Phase mapping is only covered while the optional run receipt has exactly one matching SkirmishProductionContracts.cs pin with the expected hash.']}},'warnings':{'repeated_consecutive_executed_actions':[],'stationary_with_pending_movement':[],'note':'Observational only; warnings are not automatic gameplay-failure verdicts.'},'scope':{'passive':True,'does_not_infer':['accepted unit counts','full conservation proof','RemainingMembers as undelivered']}}
 result=stable_json(a.result,'result',o);receipt=stable_json(a.receipt,'run_receipt',o,True)
 phase_pin_check(o['checks']['reservation_owner_upper_bounds'],receipt)
 if result:o['source_result']={k:result.get(k) for k in ('status','source_outcome','tick','simulation_seconds')}
 if receipt:o['source_receipt']={k:receipt.get(k) for k in ('schema_version','kind','mission','seed','process_exit','timed_out','wall_seconds')}
 seen=set();last=None;streak=None;aria_rows=0;c=o['commands']
 for no,row in stream(a.aria,'aria_trace',o):
  aria_rows+=1
  if not isinstance(row,dict):err(o,'aria line %d not object'%no);continue
  r=row.get('receipt')
  if r is not None and not isinstance(r,dict):err(o,'aria line %d receipt not object'%no);r={}
  r=r or {};worlds(row,r,'aria',no,o,seen);t=row.get('time')
  if not finite(t):err(o,'aria line %d nonfinite/missing time'%no);continue
  if last is not None and t<last:err(o,'aria clock decreases at line %d'%no)
  last=t
  if not isinstance(row.get('executed'),bool):err(o,'aria line %d executed not bool'%no);continue
  if not row['executed']:continue
  action=row.get('action') or row.get('intent');target=row.get('target_id',r.get('control_id'));api=r.get('source_api')
  if not all(isinstance(x,(str,int)) and not isinstance(x,bool) and len(str(x))<=MAX_KEY for x in (action,target)) or (api is not None and (not isinstance(api,str) or len(api)>MAX_KEY)):err(o,'aria line %d invalid command key'%no);continue
  c['executed']+=1;accepted=r.get('accepted')
  if accepted is True:c['accepted']+=1
  elif accepted is False:c['rejected']+=1
  elif accepted is None:c['unknown_acceptance']+=1
  else:err(o,'aria line %d accepted not bool/null'%no)
  for group,v in (('reasons',r.get('reason')),('source_apis',api)):
   if v is not None:
    if not isinstance(v,str) or len(v)>MAX_KEY:err(o,'aria line %d invalid %s'%(no,group))
    else:capped(o,group,v)
  reason=r.get('reason')
  if not isinstance(reason,str) or len(reason)>MAX_KEY:reason=None
  item={'line':no,'time':t,'action':action,'target':target,'accepted':accepted if isinstance(accepted,bool) else None,'reason':reason,'source_api':api};c['last'].append(item);c['last']=c['last'][-LAST:]
  key=str(action),str(target),api or '<unreported>'
  if not streak or streak['key']!=key:streak={'key':key,'times':deque(maxlen=a.repeat),'reported':False}
  times=streak['times']
  while times and t-times[0]>a.repeat_window:times.popleft()
  if not times:streak['reported']=False
  times.append(t)
  if len(times)>=a.repeat and not streak['reported']:
   warning(o,'repeated_consecutive_executed_actions',{'action':key[0],'target':key[1],'source_api':key[2],'count':len(times),'start_seconds':times[0],'end_seconds':t,'window_seconds':a.repeat_window});streak['reported']=True
 if not aria_rows:err(o,'aria_trace empty')
 tracks={};last=None;snap=0;validator_rows=0
 for no,row in stream(a.validator,'validator_state',o):
  validator_rows+=1;snap+=1;active=set()
  if not isinstance(row,dict):err(o,'validator line %d not object'%no);continue
  worlds(row,{},'validator',no,o,seen);t=row.get('simulation_seconds')
  if not finite(t):err(o,'validator line %d nonfinite/missing simulation_seconds'%no);continue
  if last is not None and t<last:err(o,'validator clock decreases at line %d'%no)
  last=t;actors=row.get('actors')
  if not isinstance(actors,list):err(o,'validator line %d actors not list'%no);continue
  if len(actors)>MAX_ACTORS:err(o,'validator line %d actor cap exceeded'%no);actors=actors[:MAX_ACTORS]
  reservation_check(o,row,actors,no)
  actor_ids=set()
  for actor in actors:
   if not isinstance(actor,dict):err(o,'validator line %d actor not object'%no);continue
   entity=actor.get('entity')
   if not isinstance(entity,str) or not entity or len(entity)>MAX_KEY:err(o,'validator line %d invalid entity'%no);continue
   if entity in actor_ids:err(o,'validator line %d duplicate actor identity'%no);continue
   actor_ids.add(entity)
   g=pending(actor,no,o)
   if g is None:tracks.pop(entity,None);continue
   xy=position(actor['components'],no,o)
   if xy is None:tracks.pop(entity,None);continue
   active.add(entity);old=tracks.get(entity)
   if old is None and len(tracks)>=MAX_ACTORS:err(o,'stationary tracker cap exceeded');continue
   if old and old['snap']==snap-1 and old['xy']==xy and old['goal']==g:
    old['count']+=1;old['snap']=snap
    if old['count']>=STATIONARY_SNAPSHOTS and t-old['start']>=STATIONARY_SECONDS and not old['reported']:
     warning(o,'stationary_with_pending_movement',{'entity':entity,'start_seconds':old['start'],'end_seconds':t,'snapshots':old['count'],'goal_cell':{'x':g[0],'y':g[1]}});old['reported']=True
   else:tracks[entity]={'xy':xy,'goal':g,'start':t,'count':1,'snap':snap,'reported':False}
  for entity in tuple(tracks):
   if entity not in active:del tracks[entity]
 if not validator_rows:err(o,'validator_state empty')
 if len(seen)>1:err(o,'mixed world IDs (%d): %s'%(len(seen),sorted(seen)[:8]))
 check=o['checks']['reservation_owner_upper_bounds']
 if check['phase_mapping_pin']['status']!='Passed':check['status']='Unsupported'
 elif check['status']!='Failed':check['status']='Passed' if check['input_snapshots'] and not check['unsupported_snapshots'] and not o['input_errors'] else 'Unsupported'
 o['analyzer_success']=not o['input_errors'];return o
def main():
 p=argparse.ArgumentParser();p.add_argument('--aria',required=True);p.add_argument('--validator',required=True);p.add_argument('--result',required=True);p.add_argument('--receipt');p.add_argument('--out',required=True);p.add_argument('--repeat',type=int,default=3);p.add_argument('--repeat-window',type=float,default=5.);a=p.parse_args()
 if not 2<=a.repeat<=MAX_ROWS or not finite(a.repeat_window) or a.repeat_window<=0:p.error('bounded repeat >=2 and finite positive repeat-window required')
 if os.path.exists(a.out):p.error('fresh output path required; input/evidence files are never overwritten')
 o=analyze(a)
 with open(a.out,'x') as f:json.dump(o,f,indent=2,sort_keys=True,allow_nan=False);f.write('\n')
 return 2 if not o['analyzer_success'] else (1 if o['checks']['reservation_owner_upper_bounds']['status']=='Failed' else 0)
if __name__=='__main__':sys.exit(main())
