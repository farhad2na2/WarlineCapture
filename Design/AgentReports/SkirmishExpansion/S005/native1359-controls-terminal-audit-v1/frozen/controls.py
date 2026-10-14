"""Actual-baseline mutations, entirely in memory; immutable native evidence untouched."""
import copy,importlib.util,json,pathlib
P=pathlib.Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('supplement',P/'audit.py');a=importlib.util.module_from_spec(s);s.loader.exec_module(a)
def run():
 data=a.raw_data();attempt=a.read(P/'evidence-pins.json')['attempt'];controls=data['rows'][str(a.REPORT/'native1359-stop-manual-controls.jsonl')];commands=data['rows'][data['failure']['trace']+'.commands.jsonl'];obs=data['rows'][str(a.REPORT/'native1359-normal-stop-manual-observation.jsonl')];q=a.load('qualifier');cases=[]
 def expect(name,fn):
  try:fn()
  except (AssertionError,ValueError):cases.append({'case':name,'rejected':True})
  else:raise AssertionError('Mutant accepted: '+name)
 def ccase(name,stage,key,value):
  c=copy.deepcopy(controls);next(x for x in c if x['stage']==stage)[key]=value;expect(name,lambda:q.qualify_controls(c,commands,attempt))
 assert q.qualify_controls(controls,commands,attempt)==data['failure']['controlProof']
 assert q.qualify_visual(obs,attempt)==data['failure']['visualProof']
 for name,stage,key,v in [('wrong-attempt','ControlsPassed','attempt','other'),('unverified-stop','StopVerified','active',True),('stop-command-increase','StopVerified','commands',999),('hold-cleared-false','HoldVerified','oldIntentCleared',False),('synthetic-hold-receipt','HoldVerified','receipt',99),('missing-hold-ended','HoldNativeEnded','stage','Missing'),('paused-clock-drift','PausedConserved','elapsed',1),('short-pause','PausedConserved','pausedWallSeconds',.1),('unrestored-owner','ManualOwnerRestored','restoredExact',False),('old-order-resurrected','HoldNoResurrection','noOldOrderResurrection',False),('missing-native-move-phase','MoveVerified','stage','Missing'),('input-intervention','ControlsPassed','interventions',1),('unpaired-input','ControlsPassed','dispatched',999),('controls-incomplete','Closed','controlsPassed',False)]:ccase(name,stage,key,v)
 for k in ['x','y','z','qx','qy','qz','qw','scale']:
  c=copy.deepcopy(controls);f=next(x['frame'] for x in c if x['stage']=='PausedConserved');next(x for x in c if x['stage']=='Actor' and x['frame']==f)[k]+=.25;expect('paused-transform-'+k,lambda c=c:q.qualify_controls(c,commands,attempt))
 for n in (1,2):
  for suffix,key,v in [('under1m','displacement',.99),('nonfinite','x',float('nan')),('wrong-origin','startX',-9999),('receipt-recycled','receipt',99999)]:
   c=copy.deepcopy(controls);f=next(x['frame'] for x in c if x['stage']=='MoveVerified' and x['moveNumber']==n);next(x for x in c if x['stage']=='Actor' and x['moveNumber']==n and x['frame']==f)[key]=v;expect(f'move{n}-{suffix}',lambda c=c:q.qualify_controls(c,commands,attempt))
 for stage in ['NativeRenderedResultText','NativeMainMenuDestination','RenderedViewportContext']:
  o=[x for x in obs if x['stage']!=stage];expect('missing-'+stage,lambda o=o:q.qualify_visual(o,attempt))
 o=copy.deepcopy(obs)
 for x in o:
  if x['stage']=='NativeRenderedResultText':x['attempt']='other'
 expect('foreign-result-attempt',lambda:q.qualify_visual(o,attempt))
 for name,fn in [('native-nonzero',lambda d:d.update(native=1)),('wrapper-nonzero',lambda d:d.update(wrapper=1)),('source-equality-drift',lambda d:d['fingerprints'][3].update(sourceHash='0'*64)),('wrong-idle-project',lambda d:d['idle']['data']['target'].update(projectPath='/other')),('wrong-owned-method',lambda d:d['start'].update(method='other')),('fog-failure-before-controls',lambda d:next(x for x in d['rows'][str(a.REPORT/'native1359-manual-fog-fallback.jsonl')] if x['stage']=='Failure').update(frame=2584)),('fog-lease-unrestored',lambda d:next(x for x in d['rows'][str(a.REPORT/'native1359-manual-fog-fallback.jsonl')] if x['stage']=='LeaseRestored').update(restoredExact=False)),('wrong-original-error',lambda d:d['failure'].update(errors=['other'])),('fog-failure-hidden',lambda d:next(x for x in d['rows'][str(a.REPORT/'native1359-manual-fog-fallback.jsonl')] if x['stage']=='Failure').update(stage='Hidden'))]:
  d=copy.deepcopy(data);fn(d);expect(name,lambda d=d:a.qualify(d))
 return {'result':'PassedActualBaselineAndMutations','positiveOriginalControlAndVisualParsers':2,'negativeControls':len(cases),'cases':cases,'nativeEvidenceModified':False,'unityCalled':False}
if __name__=='__main__':print(json.dumps(run(),indent=2))
