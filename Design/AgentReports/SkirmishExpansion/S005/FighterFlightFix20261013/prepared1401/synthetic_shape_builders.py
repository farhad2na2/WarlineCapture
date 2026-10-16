"""1369 meaningful synthetic schema controls; no native execution."""
import copy,importlib.util,json,pathlib
P=pathlib.Path(__file__).parent
s=importlib.util.spec_from_file_location('iq',P/'intercept_qualifier.py');q=importlib.util.module_from_spec(s);s.loader.exec_module(q)
source=dict(attemptId='same',actorIndex=10,actorVersion=2,reservationId=1003)
airports=[dict(phase='EnemyNativeCompletionCaptured',faction=2,runtimeId=15,originX=99,originZ=0,footprintX=1,footprintZ=1)]
stages=['PublicApproachPrepared','PublicInterceptMoveTouch','NativeInterceptMoveAccepted','NativeRearApproachArrived','PublicIntermediateLegPrepared','PublicInterceptMoveTouch','NativeInterceptMoveAccepted','NativeApproachArrived','PublicAlignedLegPrepared','PublicInterceptMoveTouch','NativeInterceptMoveAccepted','NativeAlignedApproachArrived','HeldAlignedObserved','PublicCameraPreparedAligned','TargetFlightDepartureAligned']
def preframe():
 a=dict(stage='PublicRouteZoomRequested',attempt='same',actorIndex=10,actorVersion=2,reservation=1003,frame=1,acceptedBefore=2,acceptedNow=2,hiddenTargetPoseRead=False)
 b=dict(a,stage='PublicRouteFramed',frame=2,ready=True,selected=True,pointsExposed=True,actualCameraChanged=True,dispatched=3,completed=3,beforeX=0.,beforeY=0.,beforeZ=0.,beforeFov=50.,beforeOrtho=0.,afterX=0.,afterY=30.,afterZ=0.,afterFov=50.,afterOrtho=0.,safeX=0.,safeY=0.,safeWidth=1920.,safeHeight=1080.)
 for leg in ('rear','middle','final'):b.update({leg+'ScreenX':300.,leg+'ScreenY':300.,leg+'ScreenZ':200.})
 return [a,b]
def logs(rows):return [r[k] for r in rows if r['stage']=='NativeInterceptMoveAccepted' for k in ('nativeQueueLog','nativeSuccessLog')]
def make(cycle=1,off=0):
 rows=[];touch=[]
 for i,stage in enumerate(stages):
  leg=0 if i<4 else 1 if i<8 else 2;d=3 if i==0 else 4 if i<=4 else 5 if i<=8 else 6 if i<=11 else 7;c=d-1 if i in (1,5,9) else d;receipt=0 if i in (0,1,4,5,8,9) else 10 if i<=3 else 11 if i<=7 else 12;cmd=0 if i in (3,7) else 120;alert=0 if i>=11 else 240 if leg==0 else 120
  goal=(-240.,-120.,120.)[leg];actor=goal if i in (3,7) else 0. if i>=11 else goal-120.;cmd=abs(actor-goal);alert=abs(actor);cell=int(goal)-1;resolvedframe=12+off if leg==0 else 16+off if leg==1 else 20+off
  resolve=f'[MoveCommandTrace] issueMoveOrderQueued screen=(300.0, 300.0) frame={resolvedframe} cycle={cycle} leg={leg}'
  success=f'[MoveCommandTrace] selectedMoveSuccess frame={resolvedframe} desiredGoal=int2({cell}, 0)'
  rows.append(dict(stage=stage,attemptId='same',axisSource='ActualPresentedLegalEnemyAirportPreview',input='NativeTouch',reason='',frame=10+i+off,actorIndex=10,actorVersion=2,cycle=cycle,leg=leg,returningHome=0,takeoffRolling=0,landingRolling=0,attackRunActive=0,device=7,generation=1,reservation=1003,airportRuntime=15,airportOriginX=99,airportOriginZ=0,airportFootprintX=1,airportFootprintZ=1,gridOriginX=.5,gridOriginZ=-.5,cellSize=1.,moveReceipt=receipt+off if receipt else 0,rearReceipt=(10+off) if i>=3 else 0,approachReceipt=(11+off) if i>=7 else 0,dispatched=d+off,completed=c+off,publicPreviewObserved=True,selected=True,airborne=i>1,hold=i>=12,holdTouch=i>=12,aligned=True,nearCommandGoal=cmd<=16,nearAlertGoal=alert<=32,targetVisible=False,targetAirborne=False,poseVisibilityInjected=False,publicAirportX=100.,publicAirportZ=0.,alertGoalX=0.,alertGoalZ=0.,commandGoalX=goal,commandGoalZ=0.,axisX=1.,axisZ=0.,forwardX=1.,forwardZ=0.,forwardDot=1.,commandDistance=float(cmd),alertDistance=float(alert),editorTime=20.+i+off,acceptedCommands=2,actorX=actor,actorZ=0.,signedAlong=actor,crossTrack=0.,nativeGoalPresent=True,nativeGoalMatches=True,nativeGoalCellX=cell,nativeGoalCellZ=0,nativeGoalX=goal,nativeGoalZ=0.,nativeGoalDistance=0.,trajectorySamples=2,cameraPreframed=True,resolvedFrame=resolvedframe,successFrame=resolvedframe,resolvedCellX=cell,resolvedCellZ=0,contactDispatch=d+off if i<12 else 6+off,touchScreenX=300.,touchScreenY=300.,nativeResolveLog=resolve,nativeSuccessLog=success,nativeQueueLog=resolve,nativeRequestId=cycle*100+leg+1,nativeRequestFrame=resolvedframe,nativeRequestReceipt=(10+leg+off),requestPreResolved=True,nativeRequestWorldX=goal,nativeRequestWorldY=0.,nativeRequestWorldZ=0.,nativeRequestScreenX=300.,nativeRequestScreenY=300.))
 for d,fr in [(4,11),(5,15),(6,19),(7,21)]:
  touch.extend([dict(device=7,generation=1,dispatched=d+off,completed=d-1+off,kind=k,frame=fr+off,position=dict(x=300.,y=300.)) for k in ('Dispatched','Began')]);touch.append(dict(device=7,generation=1,dispatched=d+off,completed=d+off,kind='Ended',frame=fr+1+off,position=dict(x=300.,y=300.)))
 return rows,touch
def expand(rows):
 out=[]
 for r in rows:
  if r['stage']=='NativeAlignedApproachArrived':
   first=copy.deepcopy(r);first.update(stage='NativeFlyThroughSample',frame=r['frame']-1,editorTime=r['editorTime']-.5,actorX=-4.,signedAlong=-4.,alertDistance=4.,commandDistance=124.,nearCommandGoal=False,trajectorySamples=1)
   last=copy.deepcopy(r);last.update(stage='NativeFlyThroughSample',trajectorySamples=2)
   out.extend([first,last])
  elif r['stage']=='NearGoalWrongHeading':
   first=copy.deepcopy(r);first.update(stage='NativeFlyThroughSample',frame=r['frame']-1,editorTime=r['editorTime']-.5,actorX=29.,signedAlong=29.,alertDistance=29.,commandDistance=91.,nearAlertGoal=True,nearCommandGoal=False,trajectorySamples=1)
   last=copy.deepcopy(r);last.update(stage='NativeFlyThroughSample',actorX=33.,signedAlong=33.,alertDistance=33.,commandDistance=87.,nearAlertGoal=False,nearCommandGoal=False,trajectorySamples=2)
   r=copy.deepcopy(r);r.update(actorX=33.,signedAlong=33.,alertDistance=33.,commandDistance=87.,nearAlertGoal=False,nearCommandGoal=False)
   out.extend([first,last])
  if r['stage']=='HeldAlignedObserved':
   h=copy.deepcopy(r);h.update(stage='PublicAlignedHoldTouch',frame=r['frame']-1,editorTime=r['editorTime']-.2,hold=False,dispatched=r['dispatched'],completed=r['completed']-1);out.append(h)
  out.append(r)
 return out
base,touch=make();checks=[]
def check(name,rows,contacts,aa,good):
 try:
  rows=expand(rows)
  q.qualify(rows,source,contacts,aa,preframe(),logs(rows));ok=True
 except (KeyError,ValueError,TypeError,IndexError):ok=False
 assert ok==good,(name,ok);checks.append(dict(name=name,expectedSuccess=good,passed=True))
check('Two real-shaped paired native Move contacts+distinct receipts, aligned Hold/camera/departure',base,touch,airports,True)
mutations=[
 ('missingactualrows',lambda x,t,a:x.clear()),('wrongpaidactor',lambda x,t,a:x[11].update(actorIndex=11)),('wrongpaidreservation',lambda x,t,a:x[11].update(reservation=1004)),('oldattempt',lambda x,t,a:x[11].update(attemptId='old')),('hiddenactorposeaxis',lambda x,t,a:x[0].update(axisSource='HiddenEnemyPose')),('publicpreviewmissing',lambda x,t,a:x[0].update(publicPreviewObserved=False)),('poseinjection',lambda x,t,a:x[11].update(poseVisibilityInjected=True)),('APIinsteadofTouch',lambda x,t,a:x[8].update(input='DirectOrder')),('thirdcycle',lambda x,t,a:x[0].update(cycle=3)),('missingfirstleg',lambda x,t,a:x.pop(3)),('legorderchanged',lambda x,t,a:x[8].update(leg=0)),('missingfirstreceipt',lambda x,t,a:x[2].update(moveReceipt=0)),('missingfinalreceipt',lambda x,t,a:x[10].update(moveReceipt=0)),('sameoldreceipt',lambda x,t,a:x[11].update(moveReceipt=11)),('firstreceiptlost',lambda x,t,a:x[8].update(approachReceipt=99)),('differentheadingaxis',lambda x,t,a:x[8].update(publicAirportZ=20)),('nonfiniteheading',lambda x,t,a:x[11].update(forwardDot=float('nan'))),('infinitepublicgeometry',lambda x,t,a:x[0].update(publicAirportX=float('inf'))),('booleangeometry',lambda x,t,a:x[0].update(axisX=True)),('headingdotcontradiction',lambda x,t,a:x[11].update(forwardDot=.9)),('fakealignedflag',lambda x,t,a:x[11].update(forwardX=-1,forwardDot=-1)),('wrongpublicwaypoint',lambda x,t,a:x[0].update(commandGoalX=-239)),('unselected',lambda x,t,a:x[12].update(selected=False)),('IncompleteHoldTouch',lambda x,t,a:x[12].update(dispatched=9)),('holdbeforearrival',lambda x,t,a:x[11].update(hold=True)),('noHoldTag',lambda x,t,a:x[12].update(hold=False)),('noHoldTouch',lambda x,t,a:x[12].update(holdTouch=False)),('groundedfinal',lambda x,t,a:x[11].update(airborne=False)),('automaticreturn',lambda x,t,a:x[11].update(returningHome=1)),('takeoffnotcruising',lambda x,t,a:x[11].update(takeoffRolling=1)),('landingnotcruising',lambda x,t,a:x[11].update(landingRolling=1)),('faractualarrival',lambda x,t,a:x[11].update(actorX=33.,signedAlong=33.,alertDistance=33.,nearAlertGoal=False,commandDistance=87.)),('faractualHold',lambda x,t,a:x[12].update(alertDistance=33,nearAlertGoal=False)),('cameraheadinglost',lambda x,t,a:x[13].update(forwardX=-1,forwardDot=-1,aligned=False)),('departureheadinglost',lambda x,t,a:x[14].update(forwardX=-1,forwardDot=-1,aligned=False)),('departuremissing',lambda x,t,a:x.pop()),('nonmonotonicframes',lambda x,t,a:x[12].update(frame=0)),('missingprimitive',lambda x,t,a:x[11].pop('forwardX')),
 ('missingpaidAirport',lambda x,t,a:a.clear()),('wrongpaidAirportRuntime',lambda x,t,a:x[0].update(airportRuntime=16)),('changedAirportfootprint',lambda x,t,a:x[0].update(airportFootprintX=3)),('fabricatedAirportcenter',lambda x,t,a:x[0].update(gridOriginX=10)),('missingMoveBegan',lambda x,t,a:t.pop(1)),('missingMoveEnded',lambda x,t,a:t.pop(2)),('foreignTouchdevice',lambda x,t,a:t[2].update(device=8)),('foreignTouchgeneration',lambda x,t,a:t[2].update(generation=2)),('lateMovecompletion',lambda x,t,a:t[2].update(frame=99)),('missingHoldcompletion',lambda x,t,a:t.pop()),('resetTouchcounter',lambda x,t,a:x[8].update(dispatched=1,completed=1))]
for stageIndex in (12,13,14):
 mutations.append(('Finalstagechangedreceipt:'+stages[stageIndex],lambda x,t,a,i=stageIndex:x[i].update(moveReceipt=999)))
for name,fn in mutations:
 x,t,a=copy.deepcopy(base),copy.deepcopy(touch),copy.deepcopy(airports);fn(x,t,a);check(name,x,t,a,False)
retry=copy.deepcopy(base[:11]);bad=copy.deepcopy(base[11]);bad.update(stage='NearGoalWrongHeading',forwardX=-1.,forwardDot=-1.,aligned=False);retry.append(bad);second,secondTouch=make(2,20);retry+=second
check('Actual wrong-heading cycle has bounded second public approach',retry,touch[:9]+secondTouch,airports,True)
x=copy.deepcopy(retry);x[11].update(moveReceipt=999);check('Cannotinventretrywithforeignreceipt',x,touch[:9]+secondTouch,airports,False)
x=copy.deepcopy(retry);x[11].update(moveReceipt=0);check('CannotretrywithoutactualfinalacceptedMove',x,touch[:9]+secondTouch,airports,False)
x=copy.deepcopy(retry)
for row in x[12:]:
 if row['moveReceipt']==30:row['moveReceipt']=10
 if row['approachReceipt']==30:row['approachReceipt']=10
check('Secondcyclecannotrecyclefirstcycleacceptedreceipt',x,touch[:9]+secondTouch,airports,False)

actual=expand(copy.deepcopy(base))
def rawcheck(name,rows,good):
 try:q.qualify(rows,source,touch,airports,preframe(),logs(rows));ok=True
 except (KeyError,ValueError,TypeError,IndexError):ok=False
 assert ok==good,(name,ok);checks.append(dict(name=name,expectedSuccess=good,passed=True))
rawcheck('Explicit actual resolved goal and advancing fly-through samples',actual,True)
for name,change in [
 ('MissingSamples',lambda x:x.__setitem__(slice(None),[r for r in x if r['stage']!='NativeFlyThroughSample'])),
 ('WrongSignedAlong',lambda x:x[11].update(signedAlong=90.)),
 ('WrongCrossTrack',lambda x:x[11].update(crossTrack=40.)),
 ('NonfinitePose',lambda x:x[11].update(actorX=float('nan'))),
 ('WrongNativeGoalCell',lambda x:x[12].update(nativeGoalCellX=8)),
 ('MissingNativeTarget',lambda x:x[13].update(nativeGoalPresent=False,nativeGoalMatches=False)),
 ('FalseResolvedGoalMatch',lambda x:x[13].update(nativeGoalMatches=False)),
 ('WrongResolvedGoalReceipt',lambda x:x[12].update(moveReceipt=900)),
 ('MissingPriorTrajectory',lambda x:x.pop(7)),
 ('DuplicateFrame',lambda x:x[12].update(frame=x[11]['frame'])),
 ('NonadvancingTrajectory',lambda x:x[11].update(actorX=4.,signedAlong=4.,alertDistance=4.,commandDistance=116.)),
 ('StaleTrajectoryCount',lambda x:x[12].update(trajectorySamples=1)),
 ('WrongFinalRecipe',lambda x:x[13].update(commandGoalX=0.)),
 ('TrajectoryForeignActor',lambda x:x[11].update(actorIndex=99)),
 ('NativeGoalDistanceMismatch',lambda x:x[13].update(nativeGoalDistance=100.)),
 ('NativeGoalCellBoolean',lambda x:x[13].update(nativeGoalCellX=True)),
 ('LiveGoalMissingSchema',lambda x:x[13].pop('nativeGoalPresent'))]:
 x=copy.deepcopy(actual);change(x);rawcheck(name,x,False)
for name,change in [
 ('MissingBeforeHoldAdmission',lambda x:x.__setitem__(slice(None),[r for r in x if r['stage']!='PublicAlignedHoldTouch'])),
 ('PreHoldFarAlert',lambda x:x[14].update(nearAlertGoal=False,alertDistance=33.)),
 ('PreHoldHeadingLost',lambda x:x[14].update(forwardX=-1.,forwardDot=-1.,aligned=False)),
 ('PreHoldNativeGoalLost',lambda x:x[14].update(nativeGoalMatches=False)),
 ('PreHoldWrongReceipt',lambda x:x[14].update(moveReceipt=999)),
 ('PreHoldUnpairedTouch',lambda x:x[14].update(dispatched=99))]:
 x=copy.deepcopy(actual);change(x);rawcheck(name,x,False)
# Original log evidence is held independent of each mutated serialized observer row.
for name,change in [
 ('MissingNativeQueuedRequestId',lambda x:x[2].update(nativeRequestId=0)),
 ('BooleanNativeRequestId',lambda x:x[2].update(nativeRequestId=True)),
 ('ForeignNativeInputReceipt',lambda x:x[2].update(nativeRequestReceipt=999)),
 ('WrongNativeRequestFrame',lambda x:x[2].update(nativeRequestFrame=99)),
 ('MissingNativeWorldPoint',lambda x:x[2].pop('nativeRequestWorldX')),
 ('NonfiniteNativeWorldPoint',lambda x:x[2].update(nativeRequestWorldY=float('nan'))),
 ('ForeignQueuedScreen',lambda x:x[2].update(nativeRequestScreenX=999.)),
 ('MissingQueuedRawLog',lambda x:x[2].update(nativeQueueLog='')),
 ('InventedQueuedRawLog',lambda x:x[2].update(nativeQueueLog=x[2]['nativeQueueLog']+' altered')),
 ('PreResolvedWrongAuthority',lambda x:x[2].update(nativeResolveLog='[MoveCommandTrace] selectedMoveTarget desiredGoal=int2(-241, 0)')),
 ('UnresolvedWithoutNativeTargetLog',lambda x:x[2].update(requestPreResolved=False)),
 ('InventedNativeSuccessRaw',lambda x:x[2].update(nativeSuccessLog=x[2]['nativeSuccessLog']+' altered')),
 ('RearNearGoalWithoutActualPose',lambda x:x[3].update(actorX=-200.)),
 ('IntermediateNearGoalWithoutActualPose',lambda x:x[7].update(actorX=-80.))]:
 x=copy.deepcopy(actual);change(x)
 try:q.qualify(x,source,touch,airports,preframe(),logs(actual));ok=True
 except (KeyError,ValueError,TypeError,IndexError):ok=False
 assert not ok,name;checks.append(dict(name=name,expectedSuccess=False,passed=True))
for name,change in [
 ('MissingActualPreframe',lambda x:x.clear()),
 ('ForeignPreframePaidActor',lambda x:x[1].update(actorIndex=11)),
 ('ForeignPreframeAttempt',lambda x:x[1].update(attempt='old')),
 ('CameraTroopCommand',lambda x:x[1].update(acceptedNow=3)),
 ('HiddenTargetFraming',lambda x:x[1].update(hiddenTargetPoseRead=True)),
 ('NoActualCameraChange',lambda x:x[1].update(afterY=0.)),
 ('NotReadyPreframe',lambda x:x[1].update(ready=False)),
 ('PreframeLostSelection',lambda x:x[1].update(selected=False)),
 ('InFlightCameraTouch',lambda x:x[1].update(completed=2)),
 ('RearProjectionBehindCamera',lambda x:x[1].update(rearScreenZ=-1.)),
 ('MiddleProjectionOffscreen',lambda x:x[1].update(middleScreenY=2000.)),
 ('FinalProjectionNonfinite',lambda x:x[1].update(finalScreenX=float('nan'))),
 ('BooleanCameraCoordinate',lambda x:x[1].update(afterY=True)),
 ('MissingPreframeProjection',lambda x:x[1].pop('rearScreenX')),
 ('InvalidSafeArea',lambda x:x[1].update(safeWidth=0.)),
 ('LateCameraPreframe',lambda x:x[1].update(frame=100))]:
 x=preframe();change(x)
 try:q.qualify(actual,source,touch,airports,x,logs(actual));ok=True
 except (KeyError,ValueError,TypeError,IndexError):ok=False
 assert not ok,name;checks.append(dict(name=name,expectedSuccess=False,passed=True))
(P/'intercept-controls.json').write_text(json.dumps(dict(result='PassedSyntheticOnly',count=len(checks),checks=checks,nativeRun=False),indent=2)+'\n');print('PASS',len(checks),'public-intercept negative controls')
