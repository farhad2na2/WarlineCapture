"""Pure proof parser. Missing/empty native evidence is failure, never an index exception."""
import math

def qualify_controls(rows,commands,attempt):
 if not rows or rows[0].get('stage')!='Protocol' or rows[-1].get('stage')!='Closed':raise ValueError('Exact controls Protocol/Closed absent')
 if any(r.get('stage')=='Failure' for r in rows):raise ValueError('Native controls failed')
 def one(stage):
  values=[r for r in rows if r.get('stage')==stage]
  if len(values)!=1:raise ValueError('Exactly one '+stage+' required')
  return values[0]
 stages=['StopDispatched','StopVerified','ManualOwnershipHandoff','SelectedWholeCohort','HoldVerified','PauseOwnershipHandoff','PausedSnapshot','PausedConserved','PauseOwnerRestored','HoldNoResurrection','ControlsPassed','ManualOwnerRestored']
 records={s:one(s) for s in stages}
 if any(records[a]['frame']>records[b]['frame'] for a,b in zip(stages,stages[1:])):raise ValueError('Native control phase order differs')
 relevant=[r for r in rows if r.get('stage')!='Protocol']
 if any(r.get('attempt')!=attempt for r in relevant):raise ValueError('Controls crossed attempt')
 if not all(rows[-1].get(k) is True for k in ('controlsPassed','restoredExact')):raise ValueError('Controls incomplete or QA restoration missing')
 a,b=records['StopDispatched'],records['StopVerified']
 if a.get('owner') not in ('WatchAriaPlay','AriaInstantStop') or a.get('active') is not True or a.get('oldRunning') is not True or a.get('oldBusy') is not False or a.get('pressed') is not False or a.get('gestureRequested') is not False or a.get('dueMargin',0)<.65 or a.get('stopBefore')!=0:raise ValueError('STOP did not start in actual safe native ARIA window')
 if b.get('active') is not False or b.get('oldRunning') is not False or b.get('oldBusy') is not False or b.get('stopBefore')!=0 or b.get('stopAfter')!=1 or b.get('completed')!=a['completed']+1 or b.get('dispatched')!=b['completed'] or b.get('commands')!=a['commands'] or b.get('commands',0)<=0 or b.get('auditMeasured') is not True or b.get('violations')!=0 or any(b.get(k)!=0 for k in ('unexpected','interventions')):raise ValueError('Native deliberateSTOP/audit/order-count conservation absent')
 before={r['groupId']:r['order'] for r in rows if r.get('stage')=='AcceptedOrderBeforeStop'}
 after={r['groupId']:r['order'] for r in rows if r.get('stage')=='AcceptedOrderAfterStop'}
 if not before or before!=after or any(not g or v not in ('Move','Attack') for g,v in before.items()):raise ValueError('STOP changed accepted native group intent')
 cohort=records['SelectedWholeCohort'];group=cohort['groupId'];members=cohort['members']
 if not group or members<=0 or cohort.get('allAlive') is not True or cohort.get('sameAttempt') is not True:raise ValueError('Actual living whole Rifle cohort absent')
 for n in (1,2):
  goals=[r for r in rows if r.get('stage')=='PublicMoveGoal' and r['moveNumber']==n]
  accepted=[r for r in rows if r.get('stage')=='MoveAccepted' and r['moveNumber']==n]
  moves=[r for r in rows if r.get('stage')=='MoveVerified' and r['moveNumber']==n]
  taps=[r for r in rows if r.get('stage')=='NativeMoveTerrainTouch' and r['moveNumber']==n]
  if any(len(x)!=1 for x in (goals,accepted,moves,taps)):raise ValueError('Exact native Move phases absent')
  g,a,m,t=goals[0],accepted[0],moves[0],taps[0]
  dx,dz=g['enemyX']-g['playerX'],g['enemyZ']-g['playerZ'];length=math.hypot(dx,dz)
  if not math.isfinite(length) or length<1:raise ValueError('Public objective axis absent')
  distance=18 if n==1 else 12;side=6 if n==1 else -6
  gx=g['x']+dx/length*distance+dz/length*side;gz=g['z']+dz/length*distance-dx/length*side
  if abs(gx-g['goalX'])>.05 or abs(gz-g['goalZ'])>.05 or t.get('safeArea') is not True or t.get('noUiHit') is not True:raise ValueError('Move did not use actual public-axis native safe terrain input')
  if not t['frame']<=a['frame']<=m['frame'] or a['frame']!=a['receiptFrame'] or a['receipt']!=m['receipt'] or a['receipt']<=0 or any(r['groupId']!=group for r in (a,m,t,g)) or m.get('allMoved') is not True or m.get('members')!=members:raise ValueError('Native Move receipt/cohort/timing differs')
  if not any(c.get('operation')=='Move' and c.get('verified') is True and c.get('receipt')==a['receipt'] and c.get('frame')==a['receiptFrame'] for c in commands):raise ValueError('Move acceptance lacks actual native command trace')
  samples=[r for r in rows if r.get('stage')=='Actor' and r['moveNumber']==n and r['frame']==m['frame']]
  if len(samples)!=members or len({r['id'] for r in samples})!=members or any(r.get('allAlive') is not True or r['role']!='Rifle' or r['allHeld'] is not False or r['receipt']!=a['receipt'] or r['receiptFrame']!=a['receiptFrame'] or not all(math.isfinite(r[k]) for k in ('x','z','startX','startZ','displacement')) or r['displacement']<1 or abs(r['displacement']-math.hypot(r['x']-r['startX'],r['z']-r['startZ']))>.05 for r in samples):raise ValueError('Every original live cohort member must move at least1m from actual receipt-time origin')
  if n==1:first_ids={r['id'] for r in samples};first=m
  else:
   if first_ids!={r['id'] for r in samples} or records['HoldNoResurrection']['frame']>a['frame']:raise ValueError('Replacement Move changed cohort or preceded Hold conservation')
 hold=records['HoldVerified']
 touchHold,endHold=one('HoldTouchDispatched'),one('HoldNativeEnded')
 if hold['groupId']!=group or hold['members']!=members or hold.get('allHeld') is not True or hold.get('oldIntentCleared') is not True or not first['frame']<=touchHold['frame']<=endHold['frame']<=hold['frame'] or hold['receipt']!=0 or hold['receiptFrame']!=endHold['frame'] or endHold['completed']!=touchHold['completed']+1 or endHold['completed']!=endHold['dispatched'] or any(endHold.get(k)!=0 for k in ('unexpected','interventions')) or touchHold.get('owner')!='HoldButton' or endHold.get('owner')!='HoldButton':raise ValueError('Actual paired native Hold touch and held/cleared cohort missing; no synthetic command receipt')
 held={r['id']:r for r in rows if r.get('stage')=='Actor' and r['frame']==hold['frame']}
 if set(held)!=first_ids or any(held[id].get('allHeld') is not True or held[id].get('oldIntentCleared') is not True for id in first_ids):raise ValueError('Actual Hold actor census absent/uncleared')
 snap,pause,res=records['PausedSnapshot'],records['PausedConserved'],records['HoldNoResurrection']
 if pause.get('clockConserved') is not True or pause.get('poseConserved') is not True or pause.get('allHeld') is not True or pause['pausedElapsed']!=snap['pausedElapsed'] or pause['elapsed']!=snap['elapsed'] or pause.get('pausedWallSeconds',0)<1 or pause['timeScale']!=0:raise ValueError('Actual paused clock/pose/Hold conservation missing')
 baseline={r['id']:r for r in rows if r.get('stage')=='Actor' and r['frame']==snap['frame']}
 frozen={r['id']:r for r in rows if r.get('stage')=='Actor' and r['frame']==pause['frame']}
 if set(baseline)!=first_ids or set(frozen)!=first_ids or any(any(baseline[id][k]!=frozen[id][k] for k in ('x','y','z','qx','qy','qz','qw','scale')) or frozen[id].get('allHeld') is not True or frozen[id].get('oldIntentCleared') is not True for id in first_ids):raise ValueError('Actual frozen native transform/wholecohort paused rows differ')
 if res.get('allHeld') is not True or res.get('oldIntentCleared') is not True or res.get('noOldOrderResurrection') is not True or res.get('normalSpeed') is not True or res['elapsed']-res['pausedElapsed']<2:raise ValueError('Two positive simulation seconds of Hold/no resurrection missing')
 for phase in ('PauseOwnershipHandoff','PauseOwnerRestored','ManualOwnershipHandoff','ManualOwnerRestored'):
  if records[phase].get('qaOnly') is not True or records[phase].get('sameAttempt') is not True:raise ValueError('Undisclosed/foreign ownership transfer')
 for phase in ('PauseOwnerRestored','ManualOwnerRestored'):
  if records[phase].get('restoredExact') is not True:raise ValueError('QA owner not restored exactly')
 audit=records['ControlsPassed']
 if audit.get('auditMeasured') is not True or audit.get('violations')!=0 or audit.get('completed',0)<=0 or audit.get('completed')!=audit.get('dispatched') or any(audit.get(k)!=0 for k in ('unexpected','interventions')):raise ValueError('Native manual controls receipt/contact audit failed')
 if any(not math.isfinite(r.get('elapsed',0)) or (abs(r.get('timeScale',1)-1)>.0001 and r.get('stage') not in ('Actor','PausedSnapshot','PausedConserved','Closed','NativeReceipt')) for r in rows if r.get('stage') not in ('Protocol',)):raise ValueError('Normal-speed controls altered')
 return {'scope':'Native deliberateStop/manual MoveHoldPauseResumeMove; not physical takeover/unattendedARIA','plannedExplicitStops':1,'unexpectedPhysicalInterventions':0,'sameAttempt':True,'wholeRifleCohort':sorted(first_ids),'strictNativeReceipts':True,'qaOwnersRestoredExact':True}

def qualify_visual(obs,attempt):
 if not obs:raise ValueError('Missing native rendered observer')
 if any(r.get('stage')=='ObserverFailure' for r in obs):raise ValueError('Native observer failed')
 for role in ('SetupTitle','SetupObjective','HudPurposeTitle','HudPurposeBody','PlayerBaseCaption','EnemyBaseCaption','CurrentMatchClock','AriaControl','ResultText','MainMenuSkirmishControl'):
  if not any(r.get('stage')=='NativeBoundText' and r.get('role')==role and r.get('exactBoundReference') is True and r.get('samplingPhase')=='CanvasWillRenderCanvases' and r.get('activeText') is True and r.get('culledText') is False and r.get('textAlpha',0)>.01 and r.get('ancestorAlpha',0)>.01 and r.get('visibleGlyphs',0)>0 and r.get('safeAreaOverlap') is True and r.get('renderedBody') is True for r in obs):raise ValueError('Missing actual rendered native bound text '+role)
 if not any(r.get('stage')=='RenderedViewportContext' and r.get('nativeViewportMatched') is True and abs(r['canvasWidth']-r['screenWidth'])<=1 and abs(r['canvasHeight']-r['screenHeight'])<=1 for r in obs):raise ValueError('Native render viewport absent')
 if not any(r.get('stage')=='NativeAriaControl' and r.get('ariaActive') is True and r.get('localizedStopCopy') is True and r.get('presentedControl') is True and r.get('renderedBody') is True for r in obs):raise ValueError('Actual visibleSTOP absent')
 if not any(r.get('stage')=='NativeAriaControl' and r.get('ariaActive') is False and r.get('presentedControl') is True and r.get('renderedBody') is True for r in obs):raise ValueError('Actual inactivePlay absent')
 if not any(r.get('stage')=='NativeObjectiveStrip' and r.get('finished') is False and all(r.get(k) is True for k in ('renderedPlayerBase','renderedEnemyBase','renderedClock','exactPlayerBinding','exactEnemyBinding','exactClockBinding','clockNumberPresentAndCurrent')) and r.get('expectedPlayerCaption')=='YOUR BASE' and r.get('expectedEnemyCaption')=='ENEMY BASE' and r.get('modelClockNumber') and r.get('nativeClockNumber')==r['modelClockNumber'] for r in obs):raise ValueError('Typed native base/current clock absent')
 if not any(r.get('stage')=='NativeSetupBriefing' and r.get('seed')==104734 and r.get('renderedTitle') is True and r.get('renderedObjective') is True and r.get('localizedObjectiveCopy') is True for r in obs):raise ValueError('Actual setup purpose/failure copy absent')
 if not any(r.get('stage')=='NativeHudPurpose' and r.get('renderedTitle') is True and r.get('renderedBody') is True and r.get('localizedPurposeCopy') is True for r in obs):raise ValueError('Actual HUD purpose absent')
 victory=[r for r in obs if r.get('stage')=='NativeRenderedResultText' and r.get('finished') is True and r.get('frozenVictory') is True and r.get('renderedTitle') is True and r.get('renderedVictoryWitnessed') is True and r.get('outcome')=='Victory' and r.get('attempt')==attempt and r.get('title','').upper()=='VICTORY']
 if not victory:raise ValueError('Actual same-attempt native frozen rendered Victory absent')
 dest=[r for r in obs if r.get('stage')=='NativeMainMenuDestination']
 if not dest or any(r['frame']<min(v['frame'] for v in victory) or r.get('attempt')!=attempt or r.get('shellRoute')!='MainMenu' or r.get('outcome')!='Victory' or not all(r.get(k) is True for k in ('finished','frozenVictory','renderedVictoryWitnessed','presentedControl','renderedBody','unobscuredDestination','destinationSessionAbsent','exactMainMenuOwner','exactMainMenuTitle')) or r.get('mainMenuIntent')!='OpenMenuRoute' or r.get('mainMenuRoute')!='QuickCustomSetup' or not r.get('controlPath','').endswith('/Card_Skirmish/Hotspot') or not r.get('mainMenuTitlePath','').endswith('/Card_Skirmish/Title') for r in dest):raise ValueError('Strict native same-attempt MainMenu return absent')
 closed=[r for r in obs if r.get('stage')=='ClosedObserver']
 if len(closed)!=1 or not all(closed[0].get(k) is True for k in ('setupSeen','bindingSeen','purposeSeen','activeControlSeen','manualPlaySeen','resultSeen','returnSeen')):raise ValueError('Observer closure incomplete')
 return {'nativeRenderedVictory':True,'sameAttemptMainMenuReturn':True,'strictBoundText':True}
