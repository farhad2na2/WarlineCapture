import json,math

def qualify(text):
 rows=[]
 for line in text.splitlines():
  if line.startswith('[S005PlacementFocusScope] '):rows.append(json.loads(line.split(' ',1)[1]))
 if not rows or {r.get('faction') for r in rows}!={1,2}:raise ValueError('Missing owned bilateral placement-focus scopes')
 seen=set()
 for r in rows:
  key=(r.get('faction'),r.get('candidate'))
  if key in seen:raise ValueError('Owned candidate camera scope reset repeatedly')
  seen.add(key)
  if r.get('stage')!='OwnedNewPlacementCandidate' or r.get('ownsNewCandidate') is not True or r.get('normalTouchOnly') is not True or r.get('step')!=0 or r.get('attempts')!=0 or r.get('layoutFrame')!=-1:raise ValueError('New owned QA focus scope differs')
  if not isinstance(r.get('candidate'),int) or r['candidate']<0 or not isinstance(r.get('frame'),int) or r['frame']<=0:raise ValueError('Missing actual native candidate/frame')
  for k in ('editorTime','candidateStarted','deadline','goalX','goalY','goalZ'):
   if not isinstance(r.get(k),(int,float)) or not math.isfinite(r[k]):raise ValueError('Missing/nonfinite native scope '+k)
  if r['goalY']!=0 or abs(r['deadline']-r['candidateStarted']-20)>1e-4 or abs(r['editorTime']-r['candidateStarted'])>1e-4:raise ValueError('Gameplay point/bounded Editor deadline differs')
  if not isinstance(r.get('inheritedAttempts'),int) or not isinstance(r.get('inheritedStep'),int) or not isinstance(r.get('inheritedLayoutFrame'),int):raise ValueError('Original QA focus state provenance absent')
  if not r.get('source') or 'ExistingNativeAirportFullValidator' not in r['source']:raise ValueError('Native candidate authority missing')
 if 'Owned placement candidate exhausted three actual public focus attempts' in text or 'baseGuardCameraFocusNotObserved' in text:raise ValueError('Actual focus failed; no timing waiver')
 return dict(result='Passed',ownedCandidateScopes=len(rows),scope='Initialization of private QA navigation retry scope only; native paid legal preview/Touch/combat gates separately mandatory',inheritedExhaustedScopes=sum(r['inheritedAttempts']>=3 for r in rows),deadlineSeconds=20,retryLimit=3,simulationOrUiStateInjected=False)
