"""Passive own-Fighter preparation operands. No causal or gameplay certification."""
import math
KEYS=('frame','phase','prepareStage','actorIndex','actorVersion','attemptId','faction','acceptedBefore','acceptedNow','selected','airborne','returningHome','hold','guardPassed','failedOperands','usableFuel','storedFuel','reservedFuel','civilianReserve','usableStores','hasIntent','intentKind','intentStatus','hasFuelConsumption','fuelEnabled','airFuelPerCell','groundFuelPerCell','dispatched','completed')
def qualify(rows,require_pass=False,preparation=None):
 allowed=[]
 if preparation is not None:
  import importlib.util,pathlib
  spec=importlib.util.spec_from_file_location("map_selection",pathlib.Path(__file__).with_name("fighter_reselection_qualifier.py"));q=importlib.util.module_from_spec(spec);spec.loader.exec_module(q)
  allowed=q.qualify(preparation)["temporarySelectionLossRanges"]
 if not rows:raise ValueError('Exact passive preparation guard rows missing')
 identity=None;prior=-1
 for r in rows:
  if any(k not in r for k in KEYS):raise ValueError('Native flat guard schema missing')
  current=(r['attemptId'],r['actorIndex'],r['actorVersion'],r['faction'])
  if not current[0] or current[1]<=0 or current[2]<0 or current[3]!=1:raise ValueError('Actual own actor identity missing')
  if identity is not None and current!=identity:raise ValueError('Guard actor/attempt changed')
  identity=current
  if r['frame']<prior or r['completed']>r['dispatched']:raise ValueError('Guard chronology/paired counter invalid')
  prior=r['frame']
  for k in ('usableFuel','storedFuel','reservedFuel','civilianReserve','airFuelPerCell','groundFuelPerCell'):
   if not isinstance(r[k],(float,int)) or not math.isfinite(r[k]):raise ValueError('Invalid actual own fuel operand '+k)
  failures=[]
  if not r['selected']:failures.append('Selection')
  if r['airborne']==0:failures.append('Airborne')
  if r['returningHome']!=0:failures.append('ReturningHome')
  if not r['hold']:failures.append('Hold')
  if r['acceptedNow']!=r['acceptedBefore']:failures.append('AcceptedCommands')
  if r['failedOperands']!='|'.join(failures) or r['guardPassed']!=(not failures):raise ValueError('Exact preparation guard operands contradict outcome')
  if require_pass and failures:
   if failures!=['Selection'] or not any(a['lossFrame']<=r['frame']<a['restoredFrame'] and r['prepareStage'] in a['stages'] and (r['attemptId'],r['actorIndex'],r['actorVersion'])==tuple(a['identity']) for a in allowed):raise ValueError('Actual native preparation guard failed outside exact public map/reselection proof')
 return dict(scope='Passive actual own-state census; causal attribution still requires native source/context',rows=len(rows),identity=identity,failedOperands=[r['failedOperands'] for r in rows if not r['guardPassed']])
