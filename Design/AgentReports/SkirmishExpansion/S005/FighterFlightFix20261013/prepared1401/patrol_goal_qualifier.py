"""Actual public terrain goal observation; no pose/air state writes."""
import json,math
def qualify(text,patrol,fighter):
 rows=[json.loads(line[len('[S005EnemyAirPublicPatrolGoal] '):]) for line in text.splitlines() if line.startswith('[S005EnemyAirPublicPatrolGoal] ')]
 if len(rows)!=1 or len(patrol)!=2:raise ValueError('Exactly one actual prepared public terrain patrol goal required')
 r=rows[0]
 if any(v.get('recipe')!='PublicPreframedRear240Intermediate120FlyThrough120m_AlertAdmission32m' for v in [r]+patrol):raise ValueError('Explicit public fly-through/alert-admission recipe missing')
 if r['phase']!='PublicTerrainPatrolPrepared' or r['input']!='NativeTouch' or r['poseAirborneInjected'] or r['publicGoalY']!=0 or not all(math.isfinite(r[k]) for k in ('publicGoalX','publicGoalY','publicGoalZ')) or r['moveReceipt']!=0 or r['holdTouchDispatched'] or r['frame']>=patrol[0]['frame'] or (r['actorIndex'],r['actorVersion'])!=(fighter['actorIndex'],fighter['actorVersion']):raise ValueError('Prepared goal must be same actual paid Fighter public ground point before native flight receipt')
 if not patrol[0]['patrolGoalProximityPassed'] or not math.isfinite(patrol[0]['goalHorizontalDistance']) or not 0<=patrol[0]['goalHorizontalDistance']<=32:raise ValueError('Public patrol must reach actual near-goal position before Hold, not merely liftoff displacement')
 if not math.isfinite(patrol[1]['goalHorizontalDistance']) or not 0<=patrol[1]['goalHorizontalDistance']<=32:raise ValueError('Actual held position must remain near public patrol goal')
 for v in patrol:
  if any(v[k]!=r[k] for k in ('publicGoalX','publicGoalY','publicGoalZ')):raise ValueError('Native patrol observations changed prepared public ground goal')
 if 'baseGuardCameraFocusNotObserved' in text or '[SkirmishS003HelipadProbe] result=Failed' in text:raise ValueError('Legacy helper failure cannot qualify new aircraft proof')
 return dict(preparedFrame=r['frame'],groundGoal=[r['publicGoalX'],r['publicGoalY'],r['publicGoalZ']],actorIndex=r['actorIndex'],actorVersion=r['actorVersion'],actorPoseWritten=False)
