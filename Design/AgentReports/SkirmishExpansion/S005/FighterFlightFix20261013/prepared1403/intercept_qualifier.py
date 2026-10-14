"""1369 independent actual public approach/heading admission. No native calls."""
import math,re

def number(v):
 return type(v) in (int,float) and math.isfinite(v)
def require(ok,why):
 if not ok:raise ValueError(why)
def qualify(rows,source,touch,airports,preframe,native_log):
 require(bool(rows),'Missing actual public intercept rows')
 stages=['PublicApproachPrepared','PublicInterceptMoveTouch','NativeInterceptMoveAccepted','NativeRearApproachArrived','PublicIntermediateLegPrepared','PublicInterceptMoveTouch','NativeInterceptMoveAccepted','NativeApproachArrived','PublicAlignedLegPrepared','PublicInterceptMoveTouch','NativeInterceptMoveAccepted','NativeAlignedApproachArrived','HeldAlignedObserved','PublicCameraPreparedAligned','TargetFlightDepartureAligned']
 framing=_preframe(preframe,source)
 require(preframe[1]['frame']<=rows[0]['frame'],'Actual settled public camera zoom must precede route')
 airport=[a for a in airports if a.get('phase')=='EnemyNativeCompletionCaptured' and a.get('faction')==2]
 require(len(airport)==1,'Exactly one actual paid enemy Airport completion required');airport=airport[0]
 contact=None;acceptedReceipt=0;seenReceipts=set()
 current=None;sequence=[];cycle=0;lastFrame=-1;lastTime=-1;axis=None;previousReceipt=0;rearReceipt=0;lastCompleted=-1;lastDispatched=-1;samples=[];holdContact=None;preparedFrames={}
 for r in rows:
  require(r['attemptId']==source['attemptId'] and (r['actorIndex'],r['actorVersion'],r['reservation'])==(source['actorIndex'],source['actorVersion'],source['reservationId']),'Exact paid Fighter/current attempt absent')
  require(r['axisSource']=='ActualPresentedLegalEnemyAirportPreview' and r['publicPreviewObserved'] is True and r['input']=='NativeTouch' and r['poseVisibilityInjected'] is False,'Public paid Airport axis provenance missing')
  require(type(r['cycle']) is int and 1<=r['cycle']<=2 and type(r['leg']) is int and r['leg'] in (0,1,2),'Bounded two-cycle public approach missing')
  require(type(r['frame']) is int and r['frame']>=lastFrame and number(r['editorTime']) and r['editorTime']>=lastTime,'Actual approach chronology invalid');lastFrame=r['frame'];lastTime=r['editorTime']
  fields=['gridOriginX','gridOriginZ','cellSize','publicAirportX','publicAirportZ','alertGoalX','alertGoalZ','commandGoalX','commandGoalZ','axisX','axisZ','forwardX','forwardZ','forwardDot','commandDistance','alertDistance']
  require(all(number(r[k]) for k in fields),'Finite native/public heading geometry missing')
  require(r['airportRuntime']==airport['runtimeId'] and tuple(r[k] for k in ('airportOriginX','airportOriginZ','airportFootprintX','airportFootprintZ'))==tuple(airport[k] for k in ('originX','originZ','footprintX','footprintZ')),'Public preview must match exact paid Airport runtime and actual placement')
  require(r['cellSize']>0 and abs(r['publicAirportX']-(r['gridOriginX']+(r['airportOriginX']+r['airportFootprintX']//2+.5)*r['cellSize']))<1e-4 and abs(r['publicAirportZ']-(r['gridOriginZ']+(r['airportOriginZ']+r['airportFootprintZ']//2+.5)*r['cellSize']))<1e-4,'Airport axis center does not match captured native preview grid')
  dx=r['publicAirportX']-r['alertGoalX'];dz=r['publicAirportZ']-r['alertGoalZ'];ln=math.hypot(dx,dz)
  require(ln>=10 and abs(r['axisX']-dx/ln)<1e-5 and abs(r['axisZ']-dz/ln)<1e-5,'Axis must derive from public Airport to public alert goal')
  actualAxis=tuple(r[k] for k in ('gridOriginX','gridOriginZ','cellSize','publicAirportX','publicAirportZ','alertGoalX','alertGoalZ','axisX','axisZ'))
  require(axis is None or actualAxis==axis,'Public route axis changed');axis=actualAxis
  require(abs(math.hypot(r['forwardX'],r['forwardZ'])-1)<1e-4 and abs(r['forwardDot']-(r['forwardX']*r['axisX']+r['forwardZ']*r['axisZ']))<1e-4,'Heading must be actual normalized native forward vector')
  require(r['aligned']==(r['forwardDot']>=.88) and r['nearCommandGoal']==(0<=r['commandDistance']<=16) and r['nearAlertGoal']==(0<=r['alertDistance']<=32),'Native heading/proximity flags contradict primitive measurements')
  offset=(-240,-120,60)[r['leg']];expected=(r['alertGoalX']+r['axisX']*offset,r['alertGoalZ']+r['axisZ']*offset)
  require(abs(r['commandGoalX']-expected[0])<1e-4 and abs(r['commandGoalZ']-expected[1])<1e-4,'Actual public command goal differs from bounded three-leg recipe')
  if r['stage'] not in ('PublicApproachPrepared','PublicIntermediateLegPrepared','PublicAlignedLegPrepared'):
   require(r['cameraPreframed'] is True,'Public camera framing lost before native input or flight')
  require(r['selected'] is True and type(r['completed']) is int and type(r['dispatched']) is int and 0<=r['completed']<=r['dispatched'] and r['completed']>=lastCompleted and r['dispatched']>=lastDispatched,'Actual selected public Touch counters absent/reset');lastCompleted=r['completed'];lastDispatched=r['dispatched']
  # Departure is before real native target flight, so current enemy visibility is
  # neither fabricated nor required by this own-public-geometry preparation proof.
  if r['stage']=='PublicApproachPrepared':
   require(r['cycle']==cycle+1 and (not sequence or sequence==stages[:11]+['NearGoalWrongHeading']),'New cycle only after first native wrong-heading attempt')
   cycle=r['cycle'];sequence=[];samples=[]
  require(r['cycle']==cycle,'Approach cycle mismatch')
  if r['stage'] in ('NativeRearApproachArrived','NativeApproachArrived','NativeFlyThroughSample','NativeAlignedApproachArrived','NearGoalWrongHeading','PublicAlignedHoldTouch'):
   require(all(number(r[k]) for k in ('actorX','actorZ','signedAlong','crossTrack','nativeGoalX','nativeGoalZ','nativeGoalDistance')),'Finite actual trajectory/native goal fields missing')
   ox=r['actorX']-r['alertGoalX'];oz=r['actorZ']-r['alertGoalZ']
   require(abs(r['signedAlong']-(ox*r['axisX']+oz*r['axisZ']))<1e-3 and abs(r['crossTrack']-abs(ox*r['axisZ']-oz*r['axisX']))<1e-3,'Signed along/cross-track must derive from actual own pose')
   require(abs(r['alertDistance']-math.hypot(ox,oz))<1e-3 and abs(r['commandDistance']-math.hypot(r['actorX']-r['commandGoalX'],r['actorZ']-r['commandGoalZ']))<1e-3,'Actual trajectory distance contradicts own pose')
   require(type(r['nativeGoalCellX']) is int and type(r['nativeGoalCellZ']) is int and type(r['nativeGoalPresent']) is bool and type(r['nativeGoalMatches']) is bool,'Actual UnitTarget schema missing')
   require(abs(r['nativeGoalX']-(r['gridOriginX']+(r['nativeGoalCellX']+.5)*r['cellSize']))<1e-3 and abs(r['nativeGoalZ']-(r['gridOriginZ']+(r['nativeGoalCellZ']+.5)*r['cellSize']))<1e-3,'Resolved native goal must use actual captured preview grid')
   gd=math.hypot(r['nativeGoalX']-r['commandGoalX'],r['nativeGoalZ']-r['commandGoalZ'])
   require(abs(r['nativeGoalDistance']-gd)<1e-3 and r['nativeGoalMatches']==(r['nativeGoalPresent'] and r['resolvedFrame']>=0 and r['successFrame']>=r['resolvedFrame'] and (r['nativeGoalCellX'],r['nativeGoalCellZ'])==(r['resolvedCellX'],r['resolvedCellZ'])),'Native goal match contradicts actual UnitTarget cell')
  if r['stage']=='NativeFlyThroughSample':
   require(sequence==stages[:11] and r['leg']==2 and r['moveReceipt']==acceptedReceipt and r['approachReceipt']==previousReceipt and r['airborne'] is True and r['returningHome']==0 and r['takeoffRolling']==0 and r['landingRolling']==0,'Trajectory requires exact accepted current own flight')
   require(type(r['trajectorySamples']) is int and r['trajectorySamples']==len(samples)+1 and (not samples or r['frame']>samples[-1]['frame']),'Trajectory samples must be distinct actual frames/counts')
   samples.append(r);continue
  if r['stage']=='PublicAlignedHoldTouch':
   require(sequence==stages[:12] and holdContact is None and r['leg']==2 and r['moveReceipt']==acceptedReceipt and r['approachReceipt']==previousReceipt and r['selected'] is True and r['airborne'] is True and r['returningHome']==0 and r['takeoffRolling']==0 and r['landingRolling']==0 and r['nearAlertGoal'] is True and r['aligned'] is True and r['nativeGoalMatches'] is True and r['hold'] is False and r['holdTouch'] is True and r['dispatched']==r['completed']+1,'Exact near-alert aligned actual Hold Touch admission missing')
   holdContact=r;continue
  if r['stage']=='NearGoalWrongHeading':
   require(sequence==stages[:11] and r['leg']==2 and bool(samples) and samples[-1]['frame']==r['frame'] and abs(samples[-1]['signedAlong']-r['signedAlong'])<1e-3 and (r['signedAlong']>32 or r['nearCommandGoal'] is True) and r['moveReceipt']>0 and r['approachReceipt']>0 and r['moveReceipt']!=r['approachReceipt'] and r['moveReceipt']==acceptedReceipt and r['approachReceipt']==previousReceipt,'Retry cannot invent failure or bypass real three-leg receipts');sequence.append(r['stage']);continue
  require(len(sequence)<len(stages) and r['stage'] in stages and r['stage']==stages[len(sequence)],'Exact public approach/Hold/camera/departure order missing');sequence.append(r['stage'])
  if r['stage'] in ('PublicApproachPrepared','PublicIntermediateLegPrepared','PublicAlignedLegPrepared'):
   require(r['moveReceipt']==0 and r['holdTouch'] is False,'New public Move must begin before native acceptance')
   preparedFrames[(r['cycle'],r['leg'])]=r['frame']
  if r['stage']=='PublicInterceptMoveTouch':
   key=(r['cycle'],r['leg']);require(key in preparedFrames and key in framing and preparedFrames[key]<=framing[key]['requestFrame']<framing[key]['framedFrame']<=r['frame'],'Each actual waypoint must be separately exposed by completed public camera control before Touch')
   require(r['moveReceipt']==0 and r['dispatched']==r['completed']+1,'Each public Move must start one new actual paired gesture before acceptance')
   contact=r
  if r['stage']=='NativeInterceptMoveAccepted':
   require(contact is not None and r['moveReceipt']>0 and r['completed']==r['dispatched']==contact['dispatched'] and (r['device'],r['generation'],r['cycle'],r['leg'])==(contact['device'],contact['generation'],contact['cycle'],contact['leg']),'Native Move acceptance must follow exact current own paired contact')
   rr=[a for a in touch if (a.get('device'),a.get('generation'))==(r['device'],r['generation']) and a.get('dispatched')==r['dispatched'] and a.get('kind') in ('Dispatched','Began','Ended')]
   require([a['kind'] for a in rr]==['Dispatched','Began','Ended'] and rr[0]['frame']<=contact['frame'] and rr[-1]['frame']<=r['frame'] and rr[-1]['completed']==r['completed'],'Move receipt lacks exact same-device completed public Touch range')
   require(r['contactDispatch']==contact['dispatched'] and all(number(r[k]) for k in ('touchScreenX','touchScreenY')),'Actual click screen/counter missing')
   require(type(r['resolvedFrame']) is int and type(r['successFrame']) is int and contact['frame']<=r['resolvedFrame']<=r['successFrame']==r['frame'],'Native click-cell must resolve and succeed within actual receipt frame')
   require(type(r['nativeRequestId']) is int and r['nativeRequestId']>0 and type(r['nativeRequestReceipt']) is int and r['nativeRequestReceipt']==r['moveReceipt'] and r['nativeRequestFrame']==r['resolvedFrame'],'Exact native request/receipt/frame correlation absent')
   require(type(r['requestPreResolved']) is bool and all(number(r[k]) for k in ('nativeRequestWorldX','nativeRequestWorldY','nativeRequestWorldZ','nativeRequestScreenX','nativeRequestScreenY')),'Typed native queued request schema missing')
   require(abs(r['nativeRequestScreenX']-r['touchScreenX'])<=.25 and abs(r['nativeRequestScreenY']-r['touchScreenY'])<=.25,'Request screen differs from own exact contact')
   queued=r['nativeQueueLog'];require(isinstance(queued,str) and queued.startswith('[MoveCommandTrace]') and 'issueMoveOrderQueued ' in queued and native_log.count(queued)==1,'Original synchronous queue admission log absent/duplicated')
   frame=re.search(r'frame=(\d+)',queued);require(frame is not None and int(frame.group(1))==r['nativeRequestFrame'],'Original queued request frame differs')
   success=r['nativeSuccessLog'];require(isinstance(success,str) and success.startswith('[MoveCommandTrace]') and 'selectedMoveSuccess ' in success and native_log.count(success)==1,'Actual native success log absent/duplicated')
   match=re.search(r'desiredGoal=int2\((-?\d+),\s*(-?\d+)\)',success);frame=re.search(r'frame=(\d+)',success)
   require(match is not None and tuple(map(int,match.groups()))==(r['resolvedCellX'],r['resolvedCellZ']) and frame is not None and int(frame.group(1))==r['successFrame'],'Original native success cell/frame contradict actual request')
   resolved=r['nativeResolveLog']
   if r['requestPreResolved']:
    require(resolved==queued,'Pre-resolved request authority must be captured at native synchronous queue admission')
   else:
    require(isinstance(resolved,str) and resolved.startswith('[MoveCommandTrace]') and 'selectedMoveTarget ' in resolved and native_log.count(resolved)==1,'Unpre-resolved fallback lacks actual native resolution')
    match=re.search(r'desiredGoal=int2\((-?\d+),\s*(-?\d+)\)',resolved);require(match is not None and tuple(map(int,match.groups()))==(r['resolvedCellX'],r['resolvedCellZ']),'Native fallback resolved cell differs')
   require(r['nativeGoalPresent'] is True and r['nativeGoalMatches'] is True and (r['nativeGoalCellX'],r['nativeGoalCellZ'])==(r['resolvedCellX'],r['resolvedCellZ']),'Actual native Move result not retained on exact paid actor')
   require(all(a.get('position',{})=={'x':r['touchScreenX'],'y':r['touchScreenY']} for a in rr),'Native resolver proof must retain actual same-device Touch position')
   require(type(r['moveReceipt']) is int and r['moveReceipt'] not in seenReceipts,'Native approach cannot recycle any accepted receipt');seenReceipts.add(r['moveReceipt']);acceptedReceipt=r['moveReceipt']
  if r['stage']=='NativeRearApproachArrived':
   require(r['leg']==0 and r['moveReceipt']==acceptedReceipt and r['rearReceipt']==r['moveReceipt'] and r['nearCommandGoal'] is True and r['nativeGoalMatches'] is True and r['airborne'] is True and r['returningHome']==0,'Rear waypoint must follow actual native paid flight');rearReceipt=r['moveReceipt']
  if r['stage']=='PublicIntermediateLegPrepared':require(r['leg']==1 and r['rearReceipt']==rearReceipt and rearReceipt>0,'Intermediate leg lost actual rear receipt')
  if r['stage']=='NativeApproachArrived':
   require(r['moveReceipt']==acceptedReceipt,'Approach arrival changed accepted receipt')
   require(r['leg']==1 and r['rearReceipt']==rearReceipt and r['moveReceipt']!=rearReceipt and r['nativeGoalMatches'] is True and r['moveReceipt']>0 and r['approachReceipt']==r['moveReceipt'] and r['nearCommandGoal'] is True and r['airborne'] is True and r['returningHome']==0,'First approach needs actual accepted native airborne near-goal Move');previousReceipt=r['moveReceipt']
  if r['stage']=='PublicAlignedLegPrepared':require(r['leg']==2 and r['approachReceipt']==previousReceipt,'Final public Move loses actual first approach receipt')
  if r['stage'] in ('NativeAlignedApproachArrived','HeldAlignedObserved','PublicCameraPreparedAligned','TargetFlightDepartureAligned'):
   require(r['leg']==2 and r['moveReceipt']==acceptedReceipt and r['moveReceipt']>0 and r['approachReceipt']==previousReceipt and r['moveReceipt']!=r['approachReceipt'] and r['aligned'] is True and r['airborne'] is True and r['returningHome']==0 and r['takeoffRolling']==0 and r['landingRolling']==0,'Final approach must retain both receipts, real flight and forward firing arc')
   require(r['completed']==r['dispatched'],'Completed public approach Touch missing')
  if r['stage']=='NativeAlignedApproachArrived':require(r['moveReceipt']==acceptedReceipt,'Aligned arrival changed accepted receipt');require(r['nearAlertGoal'] is True and r['nativeGoalPresent'] is True and r['nativeGoalMatches'] is True and len(samples)>=2 and samples[-1]['frame']==r['frame'] and samples[-1]['moveReceipt']==r['moveReceipt'] and samples[-1]['signedAlong']>samples[-2]['signedAlong']+.01 and abs(samples[-1]['signedAlong']-r['signedAlong'])<1e-3 and r['hold'] is False and r['holdTouch'] is False,'Aligned fly-through must retain actual goal and advancing trajectory before Hold')
  if r['stage'] in ('HeldAlignedObserved','PublicCameraPreparedAligned','TargetFlightDepartureAligned'):require(r['nearAlertGoal'] is True and r['hold'] is True and r['holdTouch'] is True,'Actual native airborne Hold/near-goal conservation missing')
  if r['stage']=='HeldAlignedObserved':
   ends=[a for a in touch if (a.get('device'),a.get('generation'))==(r['device'],r['generation']) and a.get('kind')=='Ended' and a.get('completed')==r['completed']]
   require(holdContact is not None and r['completed']==holdContact['dispatched'] and len(ends)==1 and ends[0]['frame']<=r['frame'] and r['completed']>contact['dispatched'],'Hold must complete a later actual same-device public Touch')
 require(sequence==stages,'Actual bounded aligned departure missing')
 return {'cycles':cycle,'publicAirportAxis':list(axis),'stages':rows,'scope':'Isolated paid native air effect; public Touch approach, no wholemission certificate'}

def _preframe(rows,source):
 require(len(rows)>=8 and [r.get('stage') for r in rows[:2]]==['PublicRouteZoomRequested','PublicRouteZoomSettled'] and (len(rows)-2)%2==0,'Actual public zoom convergence and per-leg camera request/framed evidence missing')
 a,settled=rows[:2];framed={};lastFrame=settled['frame']
 require(all(r.get('attempt')==source['attemptId'] and (r.get('actorIndex'),r.get('actorVersion'),r.get('reservation'))==(source['actorIndex'],source['actorVersion'],source['reservationId']) for r in rows),'Preframe exact paid actor/attempt missing')
 require(a['frame']<settled['frame'] and all(r['hiddenTargetPoseRead'] is False and r['acceptedNow']==r['acceptedBefore']==a['acceptedBefore'] and r['dispatched']==r['completed'] for r in (a,settled)) and settled['zoomElapsed']>=3 and settled['zoomStableFrames']>=6,'Public zoom did not converge without troop command changes')
 fields=['beforeX','beforeY','beforeZ','beforeFov','beforeOrtho','afterX','afterY','afterZ','afterFov','afterOrtho','safeX','safeY','safeWidth','safeHeight']
 for i in range(2,len(rows),2):
  request,b=rows[i:i+2];key=(request['cycle'],request['leg'])
  require(request['stage']=='PublicRouteLegFocusRequested' and b['stage']=='PublicRouteLegFramed' and key==(b['cycle'],b['leg']) and key not in framed and key[0] in (1,2) and key[1] in (0,1,2),'Distinct bounded public waypoint focus pair missing')
  require(lastFrame<=request['frame']<b['frame'] and all(r['hiddenTargetPoseRead'] is False and r['acceptedNow']==r['acceptedBefore']==request['acceptedBefore'] and r['dispatched']==r['completed'] and r['selected'] is True for r in (request,b)),'Public camera action changed troop receipts, selection, or Touch ownership')
  require(b['ready'] is True and b['goalExposed'] is True and (b['actualCameraChanged'] is True or b['goalExposedBefore'] is True),'Actual current waypoint not exposed by completed public camera action')
  require(all(number(b[k]) for k in fields+['goalScreenX','goalScreenY','goalScreenZ']) and b['safeWidth']>0 and b['safeHeight']>0,'Actual finite camera/safe-area missing')
  x,y,z=(b['goalScreenX'],b['goalScreenY'],b['goalScreenZ'])
  require(z>0 and b['safeX']<=x<=b['safeX']+b['safeWidth'] and b['safeY']<=y<=b['safeY']+b['safeHeight'],'Current route waypoint must be camera-forward and inside safe area')
  label=('rear','middle','final')[key[1]]
  require(all(abs(b[label+'Screen'+k]-b['goalScreen'+k])<.01 for k in ('X','Y','Z')),'Framed point differs from exact bounded leg')
  framed[key]={'requestFrame':request['frame'],'framedFrame':b['frame'],'cameraChanged':b['actualCameraChanged'],'previouslyExposed':b['goalExposedBefore']};lastFrame=b['frame']
 require(all((1,leg) in framed for leg in (0,1,2)),'Three real public route legs missing')
 return framed
