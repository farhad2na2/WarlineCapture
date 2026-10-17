"""Public-camera-only contract proof; neither Touch-camera nor normal mission certification."""
import math
def qualify(rows,source):
 stages=['PublicNativeCameraZoomRequested','PublicZoomChangedBeforeCorridorFocus','PublicAirCorridorFramed']
 if [r['stage'] for r in rows]!=stages:raise ValueError('Exact native camera request/change/FullMap framed sequence required')
 previous=-1
 for r in rows:
  if (r['actorIndex'],r['actorVersion'])!=(source['actorIndex'],source['actorVersion']) or r['attempt']!=source['attemptId']:raise ValueError('Camera proof lacks exact paid current own actor')
  if r['frame']<=previous or not r['publicApiAccepted'] or r['hiddenEnemyPoseRead'] or r['acceptedNow']!=r['acceptedBefore']:raise ValueError('Camera request chronology/authority/accepted-count invalid')
  previous=r['frame']
  if r['provenance']!='PublicHUDCameraContractZoom_ThenFullMapTouch_AuthoredSharedPlaneCruiseHeightPlusSelectedOwnPosition_NotHiddenTargetPose':raise ValueError('Camera provenance not exact disclosed contract')
  for k in ['beforeX','beforeY','beforeZ','afterX','afterY','afterZ','beforeFov','afterFov','beforeOrtho','afterOrtho','probeX','probeY','probeZ','focusX','focusY','focusZ','screenX','screenY','screenZ']:
   if not isinstance(r[k],(float,int)) or not math.isfinite(r[k]):raise ValueError('Camera native geometry missing/nonfinite')
 for r in rows[1:]:
  changed=sum((r['after'+k]-r['before'+k])**2 for k in ['X','Y','Z'])>.0001 or abs(r['afterFov']-r['beforeFov'])>.01 or abs(r['afterOrtho']-r['beforeOrtho'])>.01
  if not r['actualCameraChanged'] or not changed:raise ValueError('Actual camera movement/projection change not demonstrated')
 last=rows[-1]
 if not last['probeSafeArea'] or not last['probeForward'] or last['screenZ']<=0 or last['afterY']<=last['probeY'] or last['focusY']!=0:raise ValueError('Actual public camera cannot frame authored airborne probe')
 return {'publicNativeCameraOnly':True,'normalInputMission':False,'actualCameraChanged':True,'framedProbe':[last['probeX'],last['probeY'],last['probeZ']],'targetPoseObservedBeforeVisibility':False,'rows':rows}
