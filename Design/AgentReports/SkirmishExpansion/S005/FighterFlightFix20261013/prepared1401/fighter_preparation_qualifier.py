"""Pure1369 flat native public-input/engagement prerequisite proof; no Unity calls."""
import math
def qualify(preparation,readiness,source,target,effect):
 required=['PublicSelectBeforeCamera','PublicFullMapFocusClosed','ActualPublicCameraStable','PublicAttackPreparedBeforeTargetFlight','ActualNativeTargetFlightAccepted']
 if not preparation or not readiness:raise ValueError('Actual public preparation and native engagement rows required')
 steps=[r for r in preparation if r['stage'] not in ('PublicCameraZoomOutTouch','PublicCameraRailPreparationEnded','PublicCameraRailUnavailable','PublicMapSelectionScopeBegin','PublicMapSelectionLost','PublicPaidFighterReselectTouch','PublicPaidFighterReselected','PublicMapSelectionScopeClosed')]
 if [r['stage'] for r in steps]!=required:raise ValueError('Exact ordered public camera/Attack preparation then native target flight required')
 optional=[r for r in preparation if r['stage'] in ('PublicCameraRailPreparationEnded','PublicCameraRailUnavailable')]
 for r in optional:
  if r['stage']=='PublicCameraRailUnavailable' and (r['officialZoomPresented'] or r['zoomAvailability'] not in ('OfficialCameraRailUnavailable_FullMapFocusOnly','CameraOnlyPublicNativeAPI_DisclosedTechnicalScope')):raise ValueError('Missing official rail must be disclosed as actual unavailable, not fake zoom')
  if r['stage']=='PublicCameraRailPreparationEnded' and not r['officialZoomPresented']:raise ValueError('Presented rail preparation needs actual presented control')
 last=-1
 for r in preparation:
  if r['attemptId']!=source['attemptId'] or (r['actorIndex'],r['actorVersion'])!=(source['actorIndex'],source['actorVersion']):raise ValueError('Preparation exact paid actor/current attempt missing')
  if (not r['selected'] and r['stage'] not in ('PublicMapSelectionLost','PublicPaidFighterReselectTouch')) or not r['airborne'] or not r['hold'] or r['acceptedNow']!=r['acceptedBefore']:raise ValueError('Public camera preparation changed troops/selection/airborne Hold')
  if r['frame']<last or r['completed']>r['dispatched']:raise ValueError('Native public preparation chronology/counters invalid')
  last=r['frame']
  for k in ['focusX','focusY','focusZ','cameraX','cameraY','cameraZ','editorTime']:
   if not isinstance(r[k],(int,float)) or not math.isfinite(r[k]):raise ValueError('Nonfinite native public camera state')
 if steps[0]['mode']!='Select' or steps[1]['popupOpen'] or steps[2]['stableFrames']<5:raise ValueError('Real Select/map close/measured camera stability missing')
 prep,flight=steps[-2:]
 if prep['mode']!='Attack' or flight['mode']!='Attack' or not prep['prepared'] or not flight['prepared'] or prep['popupOpen'] or flight['popupOpen'] or prep['frame']>=flight['frame']:raise ValueError('Real public Attack preparation must finish before target flight')
 if effect['attackReceipt']<=0 or not effect['attackAccepted'] or effect['orderFrame']<=flight['frame'] or effect['frame']<effect['orderFrame']:raise ValueError('Actual accepted target Attack must follow flight/preparation')
 seen=set();eligible=[]
 for r in readiness:
  if r['frame'] in seen:raise ValueError('Duplicate native Fighter readiness frame')
  seen.add(r['frame'])
  if r['attemptId']!=source['attemptId'] or (r['sourceIndex'],r['sourceVersion'])!=(source['actorIndex'],source['actorVersion']) or (r['targetIndex'],r['targetVersion'])!=(target['actorIndex'],target['actorVersion']):raise ValueError('Native Fighter readiness paid source/target/current attempt mismatch')
  if r['poseReadAllowed']!=r['targetVisible']:raise ValueError('Hidden target pose read prohibited')
  if r['cooldownPresent'] and not math.isfinite(r['cooldown']):raise ValueError('Nonfinite actual cooldown')
  if r['targetVisible'] and any(not math.isfinite(r[k]) for k in ['horizontalDistance','forwardDot','actualRange','screenX','screenY','screenZ']):raise ValueError('Missing native visible target geometry')
  if effect['orderFrame']<=r['frame']<=effect['frame'] and r['attackReceipt']==effect['attackReceipt'] and r['accepted'] and r['engageMatches'] and r['intentMatches'] and (r['engageIndex'],r['engageVersion'],r['intentIndex'],r['intentVersion'])==(target['actorIndex'],target['actorVersion'],target['actorIndex'],target['actorVersion']) and r['attackRunActive']==1 and r['canAttack']==1 and r['autoEngage'] in (0,1) and not r['hold'] and r['attackMove'] and r['sourceAirborne'] and r['targetAirborne'] and r['targetVisible'] and r['cooldownPresent']:eligible.append(r)
 if not eligible:raise ValueError('Accepted public Attack must lead to actual paid-target Engage/AttackMove/AttackRun prerequisite before native effect')
 return {'preparation':preparation,'firstActualEngagementFrame':min(r['frame'] for r in eligible),'attackReceipt':effect['attackReceipt'],'scope':'Isolated paid native air effect; no wholemission/normal AI orchestration claim'}
