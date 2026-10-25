"""Finite source execution admission; never changes catalog playability."""
import json,hashlib
import skirmish_owned_source as owned
from pathlib import Path
PATH=Path(__file__).resolve().parent/'SkirmishSourceSimulation/admitted_profiles.json'
MODULE_SHA=owned.module_pin(__name__,__file__)
PROFILE_RAW=PATH.read_bytes()
PROFILE_SHA=hashlib.sha256(PROFILE_RAW).hexdigest()
def _pairs(rows):
 d={}
 for k,v in rows:
  if k in d:raise ValueError('Duplicate profile key: '+k)
  d[k]=v
 return d
def load_profiles():
 owned.pins();raw=PATH.read_bytes()
 if raw!=PROFILE_RAW:raise ValueError('Admission JSON changed since owned module execution')
 d=json.loads(raw,object_pairs_hook=_pairs)
 if set(d)!={'schema_version','numeric_runtime','profiles'} or type(d['schema_version'])is not int or d['schema_version']!=1 or d['numeric_runtime']!='InstalledMono':raise ValueError('Unsupported admission schema/runtime')
 if type(d['profiles'])is not list or not 0<len(d['profiles'])<=16:raise ValueError('Finite profiles required')
 seen=set()
 for r in d['profiles']:
  if type(r)is not dict or set(r)!={'mission_id','difficulty_id','size_id','seeds'}:raise ValueError('Exact profile keys required')
  if r['mission_id']!='S002' or type(r['difficulty_id'])is not int or r['difficulty_id']!=2 or type(r['size_id'])is not int or r['size_id']!=1:raise ValueError('Unqualified mission selection')
  if type(r['seeds'])is not list or not r['seeds'] or any(type(x)is not int or x not in (104731,1) for x in r['seeds']) or len(set(r['seeds']))!=len(r['seeds']):raise ValueError('Unqualified seed')
  key=(r['mission_id'],r['difficulty_id'],r['size_id'])
  if key in seen:raise ValueError('Duplicate profile')
  seen.add(key)
 return d,{str(PATH):PROFILE_SHA,str(Path(__file__).resolve()):MODULE_SHA}
def require_profile(mission,seed,difficulty_id=2,size_id=1):
 d,pins=load_profiles()
 if type(seed)is not int or type(difficulty_id)is not int or type(size_id)is not int or not any((mission,difficulty_id,size_id)==(r['mission_id'],r['difficulty_id'],r['size_id']) and seed in r['seeds'] for r in d['profiles']):raise ValueError('Unqualified source execution profile')
 return pins
def generate_guard():
 d,pins=load_profiles();clauses=[]
 for r in d['profiles']:
  clauses.append('(mission=='+json.dumps(r['mission_id'])+' && difficulty=='+str(r['difficulty_id'])+' && size=='+str(r['size_id'])+' && ('+' || '.join('seed=='+str(s) for s in r['seeds'])+'))')
 if PATH.read_bytes()!=PROFILE_RAW:raise ValueError('Admission JSON changed during guard generation')
 owned.pins()
 return 'using System; internal static class SourceAdmittedProfiles { internal static void Require(string mission,int seed,int difficulty=2,int size=1) { if(!('+' || '.join(clauses)+')) throw new NotSupportedException("Unqualified source execution profile"); } }\n',pins
