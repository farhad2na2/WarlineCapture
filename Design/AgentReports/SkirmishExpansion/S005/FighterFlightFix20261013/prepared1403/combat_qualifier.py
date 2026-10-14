"""Pure1284 qualifier; actual native inputs supplied by guarded runner, no Unity."""
import re,json,math
def qualify_deferred_aa_launch(e,frames,attempt):
    by={}
    for row in frames:
        key=(row['caseIndex'],row['frame'])
        if key in by:raise ValueError('Duplicate native missile frame')
        by[key]=row
    launch=by[(0,e['launchFrame'])];prior=by[(0,e['launchFrame']-1)]
    if e['qualificationTiming']!='VisibleEligibleNativeProjectileLaunchThenExactDeferredImpact' or not e['orderFrame']<=launch['frame']<e['frame']:raise ValueError('Native delayed missile launch chronology missing')
    for row in (launch,prior):
        if row['attemptId']!=attempt:raise ValueError('Native launch belongs to foreign attempt')
        if (row['sourceIndex'],row['sourceVersion'],row['targetIndex'],row['targetVersion'],row['ownReservation'],row['targetReservation'])!=(e['sourceIndex'],e['sourceVersion'],e['targetIndex'],e['targetVersion'],e['ownReservation'],e['targetReservation']):raise ValueError('Launch/prior exact paid source-target mismatch')
        if not row['targetAirborne'] or not row['targetVisible'] or not row['domainAllows'] or row['eligibleShooters']!=1 or (not row['lockObserved'] and not(row is prior and row['launcherPhase']==1)):raise ValueError('Native missile launch/prior target must be airborne visible eligible locked or timer-completed Tracking')
    if not launch['previousFrameMatched'] or launch['previousFrame']!=prior['frame'] or not launch['previousTargetVisible'] or not launch['previousTargetAirborne']:raise ValueError('Exact preceding native launch frame missing')
    if launch['matchingProjectiles']!=1 or launch['projectileIndex']<=0 or prior['matchingProjectiles']!=0 or launch['projectileFaction']!=1 or launch['projectileTargetKind']!=1 or launch['projectileDamage']<=0 or e['damageApplied']>launch['projectileDamage']:raise ValueError('Actual matching newly emitted native paid-source missile missing')
    tracking_completed=False
    if prior['launcherPhase']==1:
        timer=prior.get('launcherTimer');delta=launch.get('worldDelta')
        tracking_completed=isinstance(timer,(int,float)) and not isinstance(timer,bool) and math.isfinite(timer) and timer>0 and isinstance(delta,(int,float)) and not isinstance(delta,bool) and math.isfinite(delta) and delta>0 and delta+.00001>=timer and all(prior.get(k) is True for k in ('nativeLauncherTargetPresent','nativeLauncherTargetMatches')) and prior.get('nativeDebugFireTarget') is False and all(launch.get(k) is True for k in ('nativeLauncherTargetPresent','nativeLauncherTargetMatches','nativeLauncherAimAllows')) and launch.get('nativeDebugFireTarget') is False
    if launch['launcherPhase']!=4 or launch['launcherTargetIndex']!=e['targetIndex'] or launch['launcherTargetVersion']!=e['targetVersion'] or not(tracking_completed or prior['launcherPhase'] in (2,3)) or prior['launcherTargetIndex']!=e['targetIndex'] or prior['launcherTargetVersion']!=e['targetVersion']:raise ValueError('Actual native timer-completed or locked-to-fired launcher target provenance missing')
    if not all(math.isfinite(v) for v in (launch['worldElapsed'],launch['projectileElapsed'],launch['projectileLifetime'],e['observedAt'])) or launch['projectileElapsed']<0 or launch['projectileLifetime']<=launch['projectileElapsed'] or not launch['worldElapsed']-.01<=e['observedAt']<=launch['worldElapsed']+launch['projectileLifetime']-launch['projectileElapsed']+.01:raise ValueError('Delayed native missile hit outside observed lifetime')
    if e['damageApplied']<=0 or not e['exactPaidSource'] or not e['exactPaidEnemyTarget'] or e['kind']!='AirMissile':raise ValueError('Delayed hit exact native paid attribution/positive damage absent')
    return launch

def qualify(text,inputs):
    errors=[]
    airport_rows=[];placement_rows=[];runway_rows=[]
    try:
        airport_rows=[json.loads(l[len('[S005BilateralAirportReceipt] '):]) for l in text.splitlines() if l.startswith('[S005BilateralAirportReceipt] ')]
        if [r['phase'] for r in airport_rows]!=['PlayerNativeTouchPaid','EnemyNativePendingPaidCaptured','EnemyNativeCompletionCaptured']:raise ValueError('Exactly three actual native payment/receipt lifecycle rows required')
        own,pending,enemy=airport_rows
        if own['faction']!=1 or own['runtimeId']<=0 or own['materialsCost']<=0 or own['materialsSpentDelta']!=own['materialsCost'] or not own['fullPreviewValid']:raise ValueError('Actual player Touch native construction payment/runtime/preview absent')
        for row in [pending,enemy]:
            if row['faction']!=2 or row['ownerFlag']!=1 or row['requestId']<=0 or row['materialsCost']!=own['materialsCost'] or row['materialsSpentDelta']!=row['materialsCost'] or row['cost']!=0 or row['granted'] or row['allowNonbuildable'] or row['rotated'] or row['relocated'] or not row['fullPreviewValid'] or row['ownershipRelabeled'] or row['sourceGeometryChanged']:raise ValueError('Native independently paid faction2 request/flags/price differs gate')
        if pending['requestId']!=enemy['requestId'] or pending['configuredKey']!=enemy['configuredKey'] or not pending['configuredKey'] or pending['status']!=0 or enemy['status']!=1 or enemy['nativeResult']!=0 or enemy['runtimeId']<=0 or enemy['runtimeId']==own['runtimeId']:raise ValueError('Actual pending→completed native request/runtime identity mismatch')
        if pending['beforeMaterials']-pending['afterMaterials']!=pending['materialsCost']:raise ValueError('Atomic native planner material spend missing')
        for key in ['originX','originZ','rectX','rectZ','rectWidth','rectHeight']:
            if enemy[key]!=pending[key]:raise ValueError('Actual native runtime relocated outside exact validated candidate')
        if min(own['footprintX'],own['footprintZ'],enemy['footprintX'],enemy['footprintZ'],own['rectWidth'],own['rectHeight'],enemy['rectWidth'],enemy['rectHeight'])<=0:raise ValueError('Actual positive native footprint/effective runway dimensions required')
        overlap=own['rectX']<enemy['rectX']+enemy['rectWidth'] and enemy['rectX']<own['rectX']+own['rectWidth'] and own['rectZ']<enemy['rectZ']+enemy['rectHeight'] and enemy['rectZ']<own['rectZ']+own['rectHeight']
        if overlap:raise ValueError('Actual validated bilateral effective runway rectangles overlap')
        runway_rows=re.findall(r'\[S005BilateralAirportRunway\] faction=(\d+) runtimeId=(\d+) .* ownerReadModelNative=1',text)
        if [(int(f),int(i)) for f,i in runway_rows]!=[(1,own['runtimeId']),(2,enemy['runtimeId'])]:raise ValueError('Exactly two independently owned native runway readmodels required')
        placement_rows=inputs["placement"]
        legal=[p for p in placement_rows if p['phase']=='AfterCompletedGroundTouch' and p['canConfirmNative'] and p['confirmInteractable']]
        if len(legal)<2:raise ValueError('Both first paid player and separate unrotated native enemy candidate preview Touch legality observations required')
    except (OSError,KeyError,ValueError,TypeError) as e:errors.append('Malformed/failed native bilateral prerequisite evidence: '+str(e))
    queues=[];effects=[];raw_damage=[];states=[];patrol=[]
    try:
        queues=[json.loads(l[len('[S005EnemyAirPaidQueue] '):]) for l in text.splitlines() if l.startswith('[S005EnemyAirPaidQueue] ')]
        expected=[('AntiAir',1,9201,False,220),('TransportPlane',2,9202,True,440),('Fighter',1,9203,False,480),('TransportPlane',2,9204,True,440)]
        if [(r['role'],r['faction'],r['requestId'],r['target'],r['cost']) for r in queues]!=expected:raise ValueError('Exactly four actual paid allowed native requests AA/Plane/Fighter/Plane required')
        if len({r['reservationId'] for r in queues})!=4 or len({(r['actorIndex'],r['actorVersion']) for r in queues})!=4:raise ValueError('Four distinct actual paid native reservations/actors required')
        for r in queues:
            if r['cost']!=r['actualSpent'] or not r['nativePaid'] or r['synthesized'] or r['frame']<=0 or r['actorIndex']<=0 or not r['attemptId'] or r['producerId']<=0:raise ValueError('Native payment/actor/producer provenance invalid')
            if r['role']=='Fighter' and r['producerId']!=own['runtimeId'] or r['target'] and r['producerId']!=enemy['runtimeId']:raise ValueError('Actual paid aircraft producer differs truly paid owned Airport')
        effects=inputs["effects"]
        raw_damage=inputs["raw"]
        states=inputs["states"]
        patrol=[json.loads(l[len('[S005EnemyAirFighterPatrol] '):]) for l in text.splitlines() if l.startswith('[S005EnemyAirFighterPatrol] ')]
        if [r['phase'] for r in patrol]!=['NativeTouchPatrolAirborne','NativeTouchAirborneHoldObserved']:raise ValueError('Actual native Touch Fighter patrol flight/airborne Hold observations required')
        fighter=queues[2]
        for r in patrol:
            if not r['airborne'] or r['moveReceipt']<=0 or r['displacement']<5 or r['input']!='NativeTouch' or r['poseAirborneInjected'] or (r['actorIndex'],r['actorVersion'])!=(fighter['actorIndex'],fighter['actorVersion']):raise ValueError('Actual paid Fighter native patrol receipt/airborne/effect failed')
        if not patrol[1]['holdTag'] or not patrol[1]['holdTouchDispatched'] or patrol[0]['holdTag'] or patrol[1]['frame']<=patrol[0]['frame']:raise ValueError('Actual native airborne Hold after real Touch/frame progression failed')
        if any(r['eligibleShooters'] not in (0,1) for r in states):raise ValueError('At most one actual target-compatible source across timeline; qualified hit must have exactly one')
        if not any(r['phase']=='ReadOnlyRenderedFramePhaseOrShotTransition' for r in states):raise ValueError('Read-only rendered-frame phase/shot timeline missing')
        if [(r['role'],r['kind']) for r in effects]!=[('AntiAir','AirMissile'),('Fighter','DirectFire')]:raise ValueError('Exactly AA native missile and Fighter native direct-fire effects required')
        for i,r in enumerate(effects):
            shooter,target=queues[i*2:i*2+2]
            if r['ownReservation']!=shooter['reservationId'] or r['targetReservation']!=target['reservationId'] or (r['sourceIndex'],r['sourceVersion'])!=(shooter['actorIndex'],shooter['actorVersion']) or (r['targetIndex'],r['targetVersion'])!=(target['actorIndex'],target['actorVersion']):raise ValueError('Actual raw damage not exact paid native source/target')
            if not all(r[k] for k in ['qualified','exactPaidSource','exactPaidEnemyTarget','targetAirborne','targetRollObserved']) or any(r[k] for k in ['damageInjected','airFlagsInjected','poseInjected','visibilityInjected']):raise ValueError('Actual airborne observed legal native effect/provenance failed')
            if r['damageApplied']<=0 or r['targetHealthAfter']>=r['targetHealthBefore'] or r['observedCurrentHealth']>=r['targetHealthBefore'] or r['observedCurrentHealth']>r['targetHealthAfter'] or r['frame']<r['orderFrame'] or r['orderFrame']<target['frame']:raise ValueError('Native positive damage/real HP/frame conservation failed')
            if r['targetTaxiRequired'] and (not r['targetTaxiObserved'] or r['targetTaxiDistance']<=.5):raise ValueError('Positive native pre-roll taxi absent when actual threshold gap required it')
            if r['role']=='AntiAir' and (not r['projectileObserved'] or not r['lockObserved']):raise ValueError('AA actual native lock/projectile missing')
            if r['attribution']!=('ExactPaidSourceEntitySingleEligibleShooterAtLaunch' if i==0 else 'ExactPaidSourceEntitySingleEligibleShooter'):raise ValueError('Exact raw source must retain single eligible shooter attribution label')
            if r['role']=='Fighter' and (r['currentShotCounter']<=r['baselineShotCounter'] or r['orderFrame']<=patrol[1]['frame']):raise ValueError('Real Fighter native shot counter progression/patrol before Attack required')
            if r['role']=='Fighter' and (not r['targetVisible'] or not r['domainAllows'] or not r['attackAccepted'] or not r['shooterAirborne'] or r['attackReceipt']<=0):raise ValueError('Fighter native visible target Attack/airborne missing')
            if r not in raw_damage:raise ValueError('Qualified effect absent from unchanged raw native event stream')
        if any(r['damageApplied']>0 and r['exactPaidEnemyTarget'] and not r['exactPaidSource'] for r in raw_damage):raise ValueError('Foreign actual positive damage contaminated paid target')
        deaths=re.findall(r'\[S005EnemyAirCaseDeath\] result=Passed role=(\w+) bothFactionAllLiveReservedZero=1 idempotent=1 stimulus=HealthZero pendingProjectiles=0',text)
        if deaths!=['AntiAir','Fighter']:raise ValueError('Exact both-case native actor capacity release/old projectile cleanup required')
        if len(re.findall(r'\[S005EnemyAirMove\] result=Accepted',text))!=2:raise ValueError('Two actual native faction2 paid target Moves required')
        accepted_attacks=re.findall(r'\[S005EnemyAirFighterAttack\] result=Accepted input=NativeTouch selectedPaidFighter=1 actualVisibleAirborneTarget=1 exactNativeIntentTarget=1 nativeReceipt=(\d+)',text)
        if len(accepted_attacks)!=1 or int(accepted_attacks[0])<=0 or effects[1]['attackReceipt']!=int(accepted_attacks[0]):raise ValueError('Exactly one actual native Touch Fighter ArmyAttack receipt for exact paid airborne visible target required')
        if '[S005EnemyAirReceiptMismatch]' in text:raise ValueError('Actual Touch ArmyAttack native target mismatch')
    except (OSError,KeyError,ValueError,TypeError) as e:errors.append('Malformed/failed actual paid enemy-Air combat evidence: '+str(e))
    try:
        frames=inputs['frames'];census=inputs['census']
        if not frames or not census:raise ValueError('Native rendered frames/pre-isolation source census missing')
        by={}
        for r in frames:
            key=(r['caseIndex'],r['frame'])
            if key in by:raise ValueError('Duplicate actual rendered-frame key')
            by[key]=r
            if r['eligibleShooters'] not in (0,1):raise ValueError('Foreign or multiple target-compatible sources in actual frame')
        for i,e in enumerate(effects):
            key=(i,e['frame']);f=by[key];prior=by[(i,e['frame']-1)]
            if not e['eventFrameCensusMatched'] or e['eventCensusFrame']!=e['frame']:raise ValueError('Damage event lacks exact real frame census')
            for row in (f,prior):
                if row['caseIndex']!=i or (row['sourceIndex'],row['sourceVersion'])!=(e['sourceIndex'],e['sourceVersion']) or (row['targetIndex'],row['targetVersion'])!=(e['targetIndex'],e['targetVersion']) or row['ownReservation']!=e['ownReservation'] or row['targetReservation']!=e['targetReservation']:raise ValueError('Current/prior frame exact paid identity mismatch')
                if not row['targetAirborne'] or (i==1 and not row['targetVisible']):raise ValueError('Current/prior airborne target or immediate Fighter visibility absent')
                if i==1 and not row['shooterAirborne']:raise ValueError('Current/prior actual Fighter airborne missing')
            if not f['previousFrameMatched'] or f['previousFrame']!=prior['frame'] or not f['previousTargetAirborne'] or (i==1 and (f['eligibleShooters']!=1 or not f['domainAllows'] or not f['previousTargetVisible'] or not f['previousShooterAirborne'])):raise ValueError('Actual frame attribution/airborne or immediate Fighter observation failed')
            if i==0:qualify_deferred_aa_launch(e,frames,queues[0]['attemptId'])
            if i==0 and not(f['lockObserved'] and f['projectileObserved']):raise ValueError('Actual hit frame native missile/lock missing')
            if i==1 and not(f['attackAccepted'] and f['attackReceipt']==e['attackReceipt'] and not f['shooterHold'] and f['currentShotCounter']>e['baselineShotCounter']):raise ValueError('Actual hit frame native Attack/shot/Hold replacement failed')
            pre=[r for r in census if r['caseIndex']==i and r['phase']=='BeforeIsolation']
            post=[r for r in census if r['caseIndex']==i and r['phase']=='AfterIsolation']
            if not pre or not post:raise ValueError('Both native pre/post isolation census phases required')
            paid=[r for r in pre if r['paidSource']]
            if len(paid)!=1 or not paid[0]['owned'] or not paid[0]['currentAttempt'] or paid[0]['faction']!=1 or (paid[0]['sourceIndex'],paid[0]['sourceVersion'])!=(e['sourceIndex'],e['sourceVersion']) or not paid[0]['sourceAttempt'] or paid[0]['sourceAttempt']!=queues[i*2]['attemptId'] or paid[0]['attempt']!=paid[0]['sourceAttempt']:raise ValueError('Native census paid source ownership/attempt mismatch')
            added=[r for r in census if r['caseIndex']==i and r['addedSuppression']]
            for r in added:
                if r['phase']!='SuppressionAdded' or not r['suppressed'] or r['paidSource'] or (r['sourceIndex'],r['sourceVersion'])==(e['targetIndex'],e['targetVersion']):raise ValueError('Bad actual isolated suppression provenance')
                if not any((a['sourceIndex'],a['sourceVersion'])==(r['sourceIndex'],r['sourceVersion']) and not a['suppressed'] for a in pre):raise ValueError('Suppression lacks immutable pre-census actor')
            if any(r['targetCompatible'] and not r['paidSource'] for r in post):raise ValueError('Foreign actual target-compatible source survives isolation')
    except (KeyError,ValueError,TypeError,IndexError) as e:errors.append('Native frame/source attribution failed: '+str(e))
    try:
        aa=[s for s in states if s.get('role')=='AntiAir']
        if not aa:raise ValueError('Actual AA launcher diagnostic rows absent')
        required=('worldElapsedSeconds','worldDeltaSeconds','launcherTimer','effectiveRange','effectiveLockSeconds','launchDelaySeconds','aimToleranceDegrees','desiredYawDegrees','currentYawDegrees','aimErrorDegrees','flightGoalX','flightGoalZ')
        for row in aa:
            for field in required:
                if not isinstance(row[field],(int,float)) or not math.isfinite(row[field]):raise ValueError('Missing/nonfinite actual launcher diagnostic '+field)
            if row['worldDeltaSeconds']<0 or row['launcherTimer']<0 or row['effectiveLockSeconds']<=0 or row['effectiveRange']<=row['launcherMinRange']:raise ValueError('Invalid actual native launcher timing/range')
            for field in ('targetComponentPresent','turretReferencePresent','turretTransformPresent','aimedByNativeGeometry'):
                if not isinstance(row[field],bool):raise ValueError('Missing native launcher diagnostic flag '+field)
    except (KeyError,ValueError,TypeError) as e:errors.append('Native launcher diagnostics failed: '+str(e))
    if errors:raise ValueError('; '.join(errors))
    return dict(queues=queues,effects=effects,rawDamage=raw_damage,states=states,patrol=patrol,airportReceipts=airport_rows,runwayRows=runway_rows,placementRows=placement_rows,renderedFrames=frames,sourceCensus=census)
