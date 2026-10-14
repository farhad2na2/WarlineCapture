"""Independent 1359 source-neutral handoff proof; no Unity calls."""
import math
def finite(x):return type(x) in (int,float) and math.isfinite(x)
def qualify_handoff(controls,home,attempt):
 def one(rows,stage):
  found=[r for r in rows if r.get('stage')==stage]
  if len(found)!=1:raise ValueError('Exactly one '+stage+' required')
  return found[0]
 passed=one(controls,'ControlsPassed');restored=one(controls,'ManualOwnerRestored');handed=one(controls,'TacticalOwnershipHandoff');pump=one(home,'InputPumpOwnershipHandoff')
 for row in (passed,restored,handed,pump):
  if type(row.get('frame')) is not int or row['frame']<0:raise ValueError('Native handoff frame missing')
 if not passed['frame']<=restored['frame']<=pump['frame']<=handed['frame']:raise ValueError('Tactical pump began before complete controls/exact owner restore')
 if any(row.get('attempt')!=attempt for row in (passed,restored,handed)):raise ValueError('Discrete controls handoff crossed attempt')
 if not all(handed.get(k) is True for k in ('qaOnly','sameAttempt','restoredExact')) or passed.get('controlsPassed') is not True or restored.get('restoredExact') is not True:raise ValueError('Actual complete controls/exact QA owner restoration missing')
 if not all(pump.get(k) is True for k in ('sameManualDriver','controlsPassedBeforePump','nativePumpDisabled','passed')) or type(pump.get('pumpFrames')) is not int or pump['pumpFrames']!=1:raise ValueError('Exact single native QA pump retirement/same driver not observed')
 if not finite(pump.get('timeScale')) or pump['timeScale']!=1:raise ValueError('Normal handoff time scale missing')
 for row in (passed,pump):
  if type(row.get('completed')) is not int or type(row.get('dispatched')) is not int or row['completed']<=0 or row['completed']!=row['dispatched'] or any(row.get(k)!=0 for k in ('unexpected','interventions','violations')):raise ValueError('Handoff has pending/unpaired or contaminated Touch')
 if pump['completed']!=passed['completed']:raise ValueError('Input driver/count changed between verified controls and tactical pump ownership')
 active=[r for r in home if r.get('attempt')]
 if not active or any(r['attempt']!=attempt or r['frame']<pump['frame'] for r in active):raise ValueError('Tactical activity before ownership or on another attempt')
 return {'sameAttempt':attempt,'sameManualInputOwner':True,'singleQaInputPump':True,'controlsPassedBeforeTactics':True,'handoffFrame':pump['frame'],'normalVictoryNotInferred':True}
