"""Strict scoped normal Transport cruise evidence; synthetic controls are not gameplay."""
import math
from s005_paid_plane_lineage_qualifier import qualify as lineage_qualify
from s005_plane_camera_evidence import inspect as camera_inspect,inspect_png

MARKER='[S005NormalPaidPlaneCruise] result=Passed scope=NormalPaidTransportCruiseCameraOnly fullMissionCertified=0 playerReady=0'
OPENING=('opening-default','opening-public-zoom-out-endstop','opening-public-zoom-in-min','opening-default-restored')


def qualify(lineage,raw,camera,contacts,commands,log,wrapper_exit,source_unchanged):
    errors=[]
    def require(condition,message):
        if not condition:errors.append(message)
    def stage(name):return [r for r in raw if r.get('stage')==name]
    proof=lineage_qualify(lineage,contacts,log,wrapper_exit,source_unchanged)
    require(proof['qualified'],'Normal paid Airport/Plane predecessor failed')
    require(MARKER in log and 'result=Failed' not in log,'Actual native cruise marker absent/failure preserved')
    delivered=[r for r in lineage if r.get('stage')=='PaidPlaneDeliveredAtHome']
    paid=delivered[0] if len(delivered)==1 else {}
    identity=(paid.get('attempt'),paid.get('reservation'),paid.get('actorIndex'),paid.get('actorVersion'),paid.get('stable'))
    require(bool(raw) and all((r.get('attempt'),r.get('reservation'),r.get('actorIndex'),r.get('actorVersion'),r.get('stable'))==identity for r in raw),'Exact paid Transport identity changed')
    require(all(type(r.get('frame')) is int and r['frame']>0 for r in raw) and all(b.get('frame',-1)>=a.get('frame',math.inf) for a,b in zip(raw,raw[1:])),
            'Actual cruise raw frame order invalid')
    require(all(r.get('timeScale')==1 and r.get('normalBudget') is True and r.get('fullMissionCertified') is False and
                r.get('transformSource')=='UnparentedLocalTransform' and r.get('fuelScope')=='FactionPooledFuel' and r.get('aircraftFuelSpendAttributed') is False for r in raw),
                'Normal native speed/budget/world-height/pooled-Fuel scope missing')
    parked=stage('ParkedWorldBaseline');baseline=parked[0] if len(parked)==1 else {}
    require(len(parked)==1 and baseline.get('frame',-1)>=paid.get('frame',math.inf),'Actual parked world baseline absent')
    touches=stage('PublicFriendlyMoveTouch');accepted=stage('NativeFriendlyMoveAccepted');follow=stage('PublicPlaneFollowTouch');done=stage('CruiseCameraScopedComplete')
    require(len(touches)==len(accepted)==len(follow)==len(done)==1,'Unique Move/result/Follow/completion proofs required')
    touch=touches[0] if len(touches)==1 else {};result=accepted[0] if len(accepted)==1 else {};follow_touch=follow[0] if len(follow)==1 else {}
    complete=done[0] if len(done)==1 else {}
    parked_touches=stage('PublicParkedFollowTouch');entered_rows=stage('NativeParkedFollowEntered');exits=stage('PublicParkedExitTouch');restored_rows=stage('ParkedFollowExitedRestored')
    require(len(parked_touches)==len(entered_rows)==len(exits)==len(restored_rows)==1,'Unique parked public Follow/native snapshot/Exit/restore lifecycle absent')
    parked_touch=parked_touches[0] if len(parked_touches)==1 else {};entered=entered_rows[0] if len(entered_rows)==1 else {}
    exit_touch=exits[0] if len(exits)==1 else {};restored=restored_rows[0] if len(restored_rows)==1 else {}
    require(paid.get('frame',math.inf)<parked_touch.get('frame',-1)<=entered.get('nativeFollowEnteredFrame',-2)<=entered.get('frame',-3)<exit_touch.get('frame',-4)<restored.get('frame',-5)<touch.get('frame',-6),
            'Parked Follow/Exit restoration must precede the public flight Move')
    require(entered.get('nativeRestoreValid')==1 and entered.get('followEnabled')==entered.get('panLocked')==1 and
            restored.get('followEnabled')==restored.get('panLocked')==0,'Native parked Follow snapshot/lock/unlock proof absent')
    def vector_distance(a,b):
        a=a or {};b=b or {}
        return math.sqrt(sum((a.get(k,math.inf)-b.get(k,0))**2 for k in ('x','y','z')))
    def rotation_angle(a,b):
        a=a or {};b=b or {}
        try:
            norm=math.sqrt(sum(a[k]**2 for k in ('x','y','z','w'))*sum(b[k]**2 for k in ('x','y','z','w')))
            if not math.isfinite(norm) or norm<=0:return math.inf
            dot=abs(sum(a[k]*b[k] for k in ('x','y','z','w'))/norm)
            return math.degrees(2*math.acos(min(1,dot)))
        except (KeyError,TypeError,ValueError):return math.inf
    require(vector_distance(entered.get('nativeRestorePosition'),restored.get('cameraPosition'))<=.05 and
            rotation_angle(entered.get('nativeRestoreRotation'),restored.get('cameraRotation'))<=.1 and
            abs(entered.get('nativeRestoreFov',math.inf)-restored.get('cameraFieldOfView',0))<=.05 and
            entered.get('nativeRestoreOrthographic')==restored.get('cameraOrthographic') and
            abs(entered.get('nativeRestoreSize',math.inf)-restored.get('cameraOrthoSize',0))<=.05 and
            parked_touch.get('zoomControlsObserved') is True and restored.get('zoomControlsObserved') is True and
            (parked_touch.get('zoomInInteractable'),parked_touch.get('zoomOutInteractable'))==(restored.get('zoomInInteractable'),restored.get('zoomOutInteractable')),
            'Public Exit did not restore native prior pose/FOV/projection/zoom controls')
    require(vector_distance(parked_touch.get('cameraPosition'),entered.get('nativeRestorePosition'))<=.05 and
            rotation_angle(parked_touch.get('cameraRotation'),entered.get('nativeRestoreRotation'))<=.1 and
            abs(parked_touch.get('cameraFieldOfView',math.inf)-entered.get('nativeRestoreFov',0))<=.05 and
            parked_touch.get('cameraOrthographic')==entered.get('nativeRestoreOrthographic') and
            abs(parked_touch.get('cameraOrthoSize',math.inf)-entered.get('nativeRestoreSize',0))<=.05 and
            vector_distance(parked_touch.get('cameraPosition'),restored.get('cameraPosition'))<=.05 and
            rotation_angle(parked_touch.get('cameraRotation'),restored.get('cameraRotation'))<=.1 and
            abs(parked_touch.get('cameraFieldOfView',math.inf)-restored.get('cameraFieldOfView',0))<=.05 and
            parked_touch.get('cameraOrthographic')==restored.get('cameraOrthographic') and
            abs(parked_touch.get('cameraOrthoSize',math.inf)-restored.get('cameraOrthoSize',0))<=.05,
            'Native restore snapshot/Exit pose differs from independent settled pre-Touch baseline')
    plans=[r for r in stage('FriendlyFlightPlan') if r.get('frame',math.inf)<=touch.get('frame',-1)]
    plan=plans[-1] if plans else {}
    require(bool(plans) and plan.get('anchor')=='anchor.skirmish.db.staging_player' and
            plan.get('layoutId')=='layout.skirmish.db.ba' and bool(plan.get('layoutContentHash')) and plan.get('padId')=='pad.skirmish.db.staging_player' and
            plan.get('padKind')=='GroundStaging' and plan.get('padFaction')==1 and
            bool(plan.get('campaignSourceHash')) and plan.get('campaignSourceHash')==plan.get('bindingSourceHash') and
            plan.get('planningFuel',0)>=plan.get('admissionFuel',math.inf)>0 and plan.get('fuel',-1)>=plan.get('planningFuel',math.inf),
            'Friendly typed destination-specific genuine Fuel reserve absent')
    require(all((r.get('goalX'),r.get('goalZ'))==(plan.get('goalX'),plan.get('goalZ')) for r in raw if r.get('stage')!='CruiseNativeSample' and r.get('frame',0)>=touch.get('frame',math.inf)),
            'Actual friendly goal changed between public input and flight')
    require(touch.get('frame',-1)>paid.get('frame',math.inf) and result.get('verified') is True and result.get('issued') is True and
            result.get('nativeResultKind')=='GroupedManual' and result.get('resultAirUnits')==1 and result.get('resultRejection')==0 and
            (result.get('resultActorIndex'),result.get('resultActorVersion'),result.get('resultGoalX'),result.get('resultGoalZ'))==
            (paid.get('actorIndex'),paid.get('actorVersion'),plan.get('goalX'),plan.get('goalZ')) and
            result.get('receipt',0)>0 and result.get('request')==touch.get('baseline',-2)+1 and result.get('request',0)>0 and
            result.get('touchFrame')==touch.get('frame') and touch.get('frame',-1)<=result.get('receiptFrame',-2)<=result.get('frame',-3),
            'Exact public/native Move receipt/request/actor/goal proof absent')
    native=[r for r in commands if r.get('operation')=='Move' and r.get('receipt')==result.get('receipt')]
    require(len(native)==1 and native[0].get('verified') is True and native[0].get('frame')==result.get('receiptFrame'),
            'Actual native command stream Move event mismatch')
    for row,key,boundary in ((touch,'gesture',result),(follow_touch,'followGesture',complete),
                             (parked_touch,'parkedFollowGesture',entered),(exit_touch,'parkedExitGesture',restored)):
        expected=(row.get('device'),row.get('generation'),row.get(key))
        events=[r for r in contacts if (r.get('device'),r.get('generation'),r.get('dispatched'))==expected]
        require(all(type(v) is int and v>0 for v in expected) and [e.get('kind') for e in events]==['Dispatched','Began','Ended'] and
                all(e.get('commandAuditActive') is True and e.get('phase')=='PlayerResume' for e in events) and events[0].get('frame')==row.get('frame'),
                'Exact ordinary Move/Follow gesture identity and paired frames absent')
        ended=events[-1] if len(events)==3 else {}
        require(boundary.get('frame',-1)>=ended.get('frame',math.inf) and boundary.get('completed',-1)>=row.get(key,math.inf),
                'Native effects precede exact Ended or counters regress')
    require(follow_touch.get('frame',-1)>result.get('frame',math.inf),'Follow camera input must follow exact public Move acceptance')
    stable=stage('StableNativeCruise')
    def actual_fit(sample,frame,row=None):
        sample=sample or {};pixels=sample.get('CameraPixels') or {};projected=sample.get('ProjectedPixels') or {}
        position=sample.get('ActorPosition') or {}
        if row is not None and not all(abs(position.get(axis,math.inf)-row.get(field,0))<.001 for axis,field in (('x','actorX'),('y','actorY'),('z','actorZ'))):return False
        return sample.get('Valid') is True and sample.get('Frame')==frame and (sample.get('SessionId'),sample.get('EntityIndex'),sample.get('EntityVersion'),sample.get('StableActorId'))==(identity[0],identity[2],identity[3],identity[4]) and \
            pixels.get('width',0)>0 and pixels.get('height',0)>0 and 0<projected.get('width',0)<=pixels.get('width',0)*.4501 and 0<projected.get('height',0)<=pixels.get('height',0)*.4501 and \
            projected.get('x',-math.inf)>=pixels.get('x',0)+8 and projected.get('y',-math.inf)>=pixels.get('y',0)+8 and \
            projected.get('x',math.inf)+projected.get('width',math.inf)<=pixels.get('x',0)+pixels.get('width',0)-8 and \
            projected.get('y',math.inf)+projected.get('height',math.inf)<=pixels.get('y',0)+pixels.get('height',0)-8
    require(len(stable)>=15 and all(r.get('airborne')==1 and r.get('takeoff')==r.get('landing')==r.get('returning')==0 and
            r.get('cruiseInitialized')==1 and r.get('followEnabled')==r.get('panLocked')==1 and
            r.get('fuel',math.inf)<touch.get('fuelBefore',-1) and abs(r.get('actorY',math.inf)-r.get('cruiseY',0))<=1 for r in stable),
            '15 actual normal-speed cruise/Follow/pooled-Fuel-decrease frames absent')
    require(bool(stable) and all(r.get('consumptionEnabled')==r.get('consumptionInitialized')==1 and r.get('airFuelPerCell',0)>0 and
            (r.get('parkedX'),r.get('parkedZ'))==(baseline.get('actorX'),baseline.get('actorZ')) and
            r.get('horizontalFromParked',0)>=5 and r.get('horizontalStep',0)>=.001 and
            abs(math.hypot(r.get('actorX',0)-r.get('parkedX',0),r.get('actorZ',0)-r.get('parkedZ',0))-r.get('horizontalFromParked',math.inf))<.01 for r in stable),
            'Actual horizontal motion from parked baseline/native enabled consumption-state evidence absent')
    require(bool(stable) and len({(r.get('consumptionCellX'),r.get('consumptionCellZ')) for r in stable[-15:]})>1 and
            sum(math.hypot(b.get('actorX',0)-a.get('actorX',0),b.get('actorZ',0)-a.get('actorZ',0)) for a,b in zip(stable[-15:],stable[-14:]))>.01,
            'Native aircraft consumption cells and actual horizontal cruise positions remained stalled')
    require(all(abs(math.hypot(b.get('actorX',0)-a.get('actorX',0),b.get('actorZ',0)-a.get('actorZ',0))-b.get('horizontalStep',math.inf))<.01 for a,b in zip(stable[-15:],stable[-14:])),
            'Consecutive raw aircraft displacement differs from measured horizontal step')
    require(bool(stable) and all(actual_fit(r.get('framing'),r.get('frame'),r) for r in stable),'Every stable frame needs its exact actual native rendered bounds/position proof')
    parked_stable=stage('ParkedFollowStable')
    require(len(parked_stable)>=15 and all(r.get('airborne')==r.get('takeoff')==r.get('landing')==r.get('returning')==0 and
            r.get('followEnabled')==r.get('panLocked')==1 and r.get('horizontalFromParked',math.inf)<=1.5 and actual_fit(r.get('framing'),r.get('frame'),r) for r in parked_stable),
            '15 actual parked Follow frames with native mesh fit absent')
    require(len(parked_stable)>=15 and all(b.get('frame',-1)==a.get('frame',-2)+1 and
            vector_distance(a.get('cameraPosition'),b.get('cameraPosition'))<.01 and rotation_angle(a.get('cameraRotation'),b.get('cameraRotation'))<.05 and
            abs(a.get('cameraFieldOfView',math.inf)-b.get('cameraFieldOfView',0))<.01 for a,b in zip(parked_stable[-15:],parked_stable[-14:])),
            'Parked Follow capture was not settled over15 distinct consecutive frames')
    parked_images=[r for r in camera if r.get('phase')=='parked-follow-transport']
    parked_image=parked_images[0] if len(parked_images)==1 else {};parked_measurement=parked_image.get('measurement') or {}
    require(len(parked_images)==1 and parked_stable and parked_image.get('frame')==parked_stable[-1].get('frame') and
            parked_image.get('attempt')==paid.get('attempt') and parked_image.get('paidReservation')==paid.get('reservation') and
            parked_image.get('aircraftState')=='Grounded' and actual_fit(parked_measurement,parked_image.get('frame'),parked_stable[-1]) and
            not inspect_png(parked_image) and parked_image.get('frame',math.inf)<exit_touch.get('frame',-1),
            'Fresh settled parked Follow same-frame native PNG absent')
    require(not any(r.get('png')==parked_image.get('png') for r in camera if r.get('phase')!='parked-follow-transport'),
            'Parked Follow PNG must preserve a distinct artifact from default parked/opening/cruise views')
    require(len(stable)>=15 and all(b.get('frame',-1)==a.get('frame',-2)+1 for a,b in zip(stable[-15:],stable[-14:])),
            'Cruise frames must be consecutive actual native frames')
    require(any(r.get('takeoff')==1 and r.get('frame',-1)>touch.get('frame',math.inf) and r.get('frame',math.inf)<(stable[0].get('frame',-1) if stable else -1) for r in raw),
            'Actual native takeoff rolling before cruise absent')
    require(all(not r.get('returning') and not r.get('landing') for r in raw if r.get('frame',-1)>=touch.get('frame',math.inf) and r.get('frame',math.inf)<complete.get('frame',-1)),
            'Native return/landing before cruise capture is a source-fit failure')
    base_camera=camera_inspect([r for r in camera if r.get('phase') in ('opening-infantry','paid-transport')],lineage)
    require(base_camera['evidenceCaptured'],'Original opening/parked camera joins failed')
    for phase in OPENING:
        phase_rows=[r for r in camera if r.get('phase')==phase]
        require(bool(phase_rows) and all(r.get('attempt')==paid.get('attempt') and (r.get('measurement') or {}).get('SessionId')==paid.get('attempt') and
                r.get('frame',0)>0 and r.get('frame')==(r.get('measurement') or {}).get('Frame') and (r.get('measurement') or {}).get('NativeCategory')=='Infantry' and
                not inspect_png(r) for r in phase_rows),'Normal public opening camera view absent/mismatched: '+phase)
    flight=[r for r in camera if r.get('phase')=='cruising-transport']
    sample=flight[0] if len(flight)==1 else {};measurement=sample.get('measurement') or {}
    require(len(flight)==1 and (sample.get('attempt'),sample.get('paidReservation'),measurement.get('EntityIndex'),measurement.get('EntityVersion'),measurement.get('StableActorId'))==identity and
            measurement.get('SessionId')==paid.get('attempt') and measurement.get('NativeRole')=='TransportPlane' and
            sample.get('aircraftState')=='Airborne' and sample.get('actorTransformSource')=='UnparentedLocalTransform' and sample.get('airborne')==1 and
            sample.get('takeoffRolling')==sample.get('landingRolling')==sample.get('returningHome')==0 and sample.get('cruiseYInitialized')==1 and
            measurement.get('Valid') is True and sample.get('frame')==measurement.get('Frame') and stable and sample.get('frame')==stable[-1].get('frame'),
            'Exact same-frame native airborne mesh/PNG sample absent')
    pixels=measurement.get('CameraPixels') or {};projected=measurement.get('ProjectedPixels') or {}
    require(pixels.get('width',0)>0 and pixels.get('height',0)>0 and 0<projected.get('width',0)<=pixels.get('width',0)*.4501 and
            0<projected.get('height',0)<=pixels.get('height',0)*.4501,'Actual rendered aircraft exceeds45percent or missing projection')
    require(projected.get('x',-math.inf)>=pixels.get('x',0)+8 and projected.get('y',-math.inf)>=pixels.get('y',0)+8 and
            projected.get('x',math.inf)+projected.get('width',math.inf)<=pixels.get('x',0)+pixels.get('width',0)-8 and
            projected.get('y',math.inf)+projected.get('height',math.inf)<=pixels.get('y',0)+pixels.get('height',0)-8,
            'Actual native aircraft bounds are offscreen or outside8pixel safe margin')
    require(not inspect_png(sample),'Cruise PNG byte/hash/format proof failed')
    require(bool(stable) and actual_fit(measurement,sample.get('frame'),stable[-1]),'Final PNG-frame native actor position/render fit differs from stable cruise')
    require(not any(r.get('png')==sample.get('png') for r in camera if r.get('phase')!='cruising-transport'),'Cruise PNG must be a fresh artifact, not reused parked/opening image')
    return dict(qualified=not errors,errors=errors,result='PassedScopedNormalTransportCruise' if not errors else 'Failed',
                cameraAccepted=False,playerReady=False,fullMissionCertified=False,aircraftFuelSpendAttributed=False,
                fuelEvidence='FactionPooledFuelDeltaAndNativePlaneConsumptionState',
                pending=['owner readable infantry/aircraft screenshot review','cargo/Return/landing','normal Victory/result/return','player/device acceptance'])
