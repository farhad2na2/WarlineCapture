"""Qualify actual external normal-Touch fog approach. Synthetic parser controls do not certify gameplay."""
import math

def qualify(rows,commands,attempt,required=False,handoff_proof=None,terminal_proof=None):
 if not rows or rows[0].get('stage')!='Protocol' or rows[-1].get('stage')!='Closed':raise ValueError('Exact fallback Protocol/Closed lifecycle absent')
 if any(r['stage']=='Failure' for r in rows) or not rows[-1]['restoredExact'] or (rows[-1].get('sameDriver') is not True and not any(r.get('stage') in ('SaveHandoff','TerminalClosure') for r in rows)):raise ValueError('Fallback companion failed or QA lease unclosed')
 known={'Protocol','LeaseStarted','NativeReceipt','NativeReceiptAlias','CameraTouch','CameraClosed','GroundTouch','RejectedApproach','AdvanceAccepted','Actor','NaturalVisibility','LeaseRestored','SaveHandoff','TerminalClosure','Closed'}
 if any(r['stage'] not in known for r in rows):raise ValueError('Unknown fallback stage')
 starts=[r for r in rows if r['stage']=='LeaseStarted'];restores=[r for r in rows if r['stage'] in ('LeaseRestored','SaveHandoff','TerminalClosure')]
 if [r['round'] for r in starts]!=list(range(1,len(starts)+1)) or [r['round'] for r in restores]!=[r['round'] for r in starts]:raise ValueError('Exact bounded fallback round/restore ordering absent')
 if len(starts)>12:raise ValueError('Fallback bound altered')
 if required and not starts:raise ValueError('Actual meaningful hidden-marker fallback did not occur')
 proved=[]
 for start,restore in zip(starts,restores):
  group=[r for r in rows if r['round']==start['round'] and r['stage'] not in ('Closed','Protocol')]
  if start['attempt']!=attempt or any(r['attempt']!=attempt for r in group) or not start['sameDriver'] or (start['markerVisible'] and start['targetVisible']) or start['reason']!='WaitingForUnavailablePublicTarget' or start['members']<=0:raise ValueError('Fallback did not start from real selected hidden-marker public state')
  handoff=restore['stage']=='SaveHandoff'
  terminal=restore['stage']=='TerminalClosure'
  if restore['originalDue']!=start['originalDue'] or restore.get('restoredExact') is not True:raise ValueError('Exact QA due restoration missing')
  if handoff:
   if restore.get('sameDriver') is not False or restore.get('reason')!='VerifiedNativeCombatSaveHandoff' or restore.get('resumeStep')!=1 or not all(restore.get(k) is True for k in ('resumeActive','manualAbsent','oldStopped','newRunning')) or any(restore.get(k) is not False for k in ('oldBusy','newBusy')) or restore.get('oldCompleted',0)<=0 or restore.get('oldCompleted')!=restore.get('oldDispatched'):raise ValueError('Unverified or busy native Save input handoff')
   if not handoff_proof or handoff_proof.get('nativeSaveContinuePassed') is not True or handoff_proof.get('sameAttempt') is not True or handoff_proof.get('strictContactAudit') is not True or handoff_proof.get('oldPhysicalGone',0)<=0:raise ValueError('Save handoff lacks independent actual native recovery/contact proof')
  elif terminal:
   if restore.get('reason')!='VerifiedNativeFrozenVictory' or not all(restore.get(k) is True for k in ('nativeFinished','nativeVictory','resultFrozen')) or restore.get('oldBusy') is not False or restore.get('oldCompleted',0)<=0 or restore.get('oldCompleted')!=restore.get('oldDispatched'):raise ValueError('Terminal closure lacks native frozenVictory/complete input')
   if not terminal_proof or not all(terminal_proof.get(k) is True for k in ('renderedVictoryPassed','sameAttemptReturnPassed','strictContactAudit')):raise ValueError('Terminal closure lacks independent renderedVictory/same-attemptReturn/contact proof')
  elif restore.get('sameDriver') is not True:raise ValueError('Exact same-driver QA due restoration missing')
  if any(not math.isfinite(r['timeScale']) or (abs(r['timeScale']-1)>1e-4 and not (terminal and r['stage']=='TerminalClosure' and r['timeScale']==0)) for r in group):raise ValueError('Fallback normal speed absent')
  for r in group:
   if not 0<=r['candidate']<3:raise ValueError('Public candidate bound altered')
  for r in [r for r in group if r['stage'] in ('GroundTouch','AdvanceAccepted','CameraTouch')]:
   ex,ez,px,pz=(start[k] for k in ('enemyObjectiveX','enemyObjectiveZ','playerObjectiveX','playerObjectiveZ'));dx,dz=ex-px,ez-pz;n=math.hypot(dx,dz)
   if n<1:raise ValueError('Actual public objective vector absent')
   fx,fz=dx/n,dz/n;distance=12+r['candidate']*6;gx,gz=ex-fx*distance+fz*6,ez-fz*distance-fx*6
   # Vector3.Cross(up,forward)=(forward.z,0,-forward.x).
   if abs(r['goalX']-gx)>.05 or abs(r['goalZ']-gz)>.05:raise ValueError('Approach derived from hidden/unauthored alternative rather than public objective')
  cameras=[r for r in group if r['stage']=='CameraClosed'];cameraTouches=[r for r in group if r['stage']=='CameraTouch']
  if len(cameras)!=len(cameraTouches) or any(not r['cameraOnly'] or r['commandsBefore']!=r['commandsAfter'] for r in cameras):raise ValueError('Camera touched troop orders/unclosed camera')
  receipts=[r for r in group if r['stage']=='NativeReceipt'];advances=[r for r in group if r['stage']=='AdvanceAccepted'];naturals=[r for r in group if r['stage']=='NaturalVisibility']
  if any(not r['verified'] or r['operation']!='AttackMove' or not r['receipt'] for r in receipts):raise ValueError('Native fallback command receipt violated')
  for r in receipts:
   if not any(c['frame']==r['frame'] and c['receipt']==r['receipt'] and c['verified'] is True and c['operation']=='AttackMove' for c in commands):raise ValueError('Fallback receipt not in real native command trace')
  aliases=[r for r in group if r['stage']=='NativeReceiptAlias']
  for alias in aliases:
   if alias.get('operation')!='ArmyAttack' or alias.get('verified') is not True or alias.get('reason')!='PairedSameFrameAttackMoveScope' or not any(r['operation']=='AttackMove' and r['verified'] is True and r['receipt']==alias['receipt'] and r['frame']==alias['frame'] for r in receipts) or not any(c['operation']=='ArmyAttack' and c['verified'] is True and c['receipt']==alias['receipt'] and c['frame']==alias['frame'] for c in commands) or not any(r['stage']=='GroundTouch' and r['candidate']==alias['candidate'] and r['safeArea'] and r['noUiHit'] and r['frame']<=alias['frame'] for r in group):raise ValueError('ArmyAttack alias lacks same-groundTouch/frame/receipt accepted native AttackMove')
  for a in advances:
   source=[r for r in receipts if r['receipt']==a['receipt']];touches=[r for r in group if r['stage']=='GroundTouch' and r['candidate']==a['candidate']]
   if len(source)!=1 or len(touches)!=1 or not touches[0]['safeArea'] or not touches[0]['noUiHit'] or not touches[0]['frame']<=source[0]['frame']<=a['frame']:raise ValueError('Actual presented terrainTouch→accepted native advance sequence absent')
  actors=[r for r in group if r['stage']=='Actor'];ids={r['id'] for r in actors}
  if len(ids)!=start['members']:raise ValueError('Actual selected native cohort census absent')
  for r in actors:
   if not all(math.isfinite(r[k]) for k in ('elapsed','x','z','startX','startZ','displacement','speed')) or r['speed']<=0 or abs(r['displacement']-math.hypot(r['x']-r['startX'],r['z']-r['startZ']))>.05:raise ValueError('Actual own-cohort finite displacement inconsistent')
  for id in ids:
   seq=[r for r in actors if r['id']==id]
   for a,b in zip(seq,seq[1:]):
    if b['frame']<a['frame'] or b['elapsed']<a['elapsed'] or math.hypot(b['x']-a['x'],b['z']-a['z'])>max(a['speed'],b['speed'])*(b['elapsed']-a['elapsed'])+8:raise ValueError('Native movement regressed/teleported')
  if naturals:
   if len(naturals)!=1 or len(advances)!=1 or not naturals[0]['markerVisible'] or not naturals[0]['targetVisible'] or naturals[0]['receipt']!=advances[0]['receipt'] or not advances[0]['frame']<=naturals[0]['frame']<=restore['frame'] or restore['reason']!='NaturalVisibleMarkerThenUnchangedManualAttack':raise ValueError('Natural visible marker must follow actual public advance and precede exact restore')
   last=[r for r in actors if r['frame']==naturals[0]['frame'] and r['alive'] and r['selected'] and r['markerVisible'] and r['targetVisible'] and r['receipt']==advances[0]['receipt'] and r['displacement']>=5]
   if not last:raise ValueError('No actual samecohort positive movement plus naturally visible marker')
   proved.append({'round':start['round'],'receipt':advances[0]['receipt'],'actualSelectedIds':sorted(ids),'visibleFrame':naturals[0]['frame'],'actualPositiveMovement':last})
  elif handoff or terminal:
   if len(advances)!=1 or advances[0]['frame']>restore['frame']:raise ValueError('Save handoff must follow actual accepted public advance')
  elif restore['reason']!='CohortLost':raise ValueError('Advance did not establish natural marker visibility; retain failure')
 if starts and not proved:raise ValueError('No successful native fog approach; cohort losses are not passes')
 return {'fogFallbackQualified':bool(proved),'fallbackWasNeeded':bool(starts),'normalInput':True,'humanAcceptance':False,'attempt':attempt,'attempts':len(starts),'successfulNaturalVisibility':proved,'sourceOfPlanning':'Actual public HUD objective position only; fixed12/18/24m+side6 candidate terrain','cameraOrderCount':0,'qaDueRestoredExact':True,'verifiedSaveHandoffs':len([r for r in restores if r['stage']=='SaveHandoff']),'verifiedTerminalClosures':len([r for r in restores if r['stage']=='TerminalClosure']),'noVisionOrderOrOutcomeInjection':True}
