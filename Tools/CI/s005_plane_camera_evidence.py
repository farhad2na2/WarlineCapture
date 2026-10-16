"""Validate raw camera capture joins only; never infer readability/owner acceptance."""
import hashlib
import math
import struct
from pathlib import Path


def inspect_png(row):
    errors=[]
    try:
        data=Path(row['png']).read_bytes()
        if hashlib.sha256(data).hexdigest()!=row.get('pngSha256'):errors.append('PNG bytes changed')
        if not (data[:8]==b'\x89PNG\r\n\x1a\n' and len(data)>32 and data[-12:]==bytes.fromhex('0000000049454e44ae426082')):errors.append('PNG incomplete')
        width,height=struct.unpack('>II',data[16:24])
        if width<=0 or height<=0:errors.append('PNG geometry invalid')
    except (OSError,KeyError,ValueError,struct.error): errors.append('PNG unavailable or malformed')
    return errors


def inspect(rows, lineage):
    errors=[]
    def require(condition, message):
        if not condition: errors.append(message)
    attempts={r.get('attempt') for r in lineage if r.get('stage')=='PaidPlaneDeliveredAtHome'}
    require(len(attempts)==1 and bool(next(iter(attempts),None)), 'Unique delivered lineage attempt absent')
    planes=[r for r in lineage if r.get('stage')=='PaidPlaneDeliveredAtHome']
    plane=planes[0] if len(planes)==1 else {}
    require(len(planes)==1,'Unique delivered Plane proof absent')
    require(bool(rows),'Camera rows absent')
    phases={r.get('phase') for r in rows}
    require(phases=={'opening-infantry','paid-transport'},'Both camera view phases required')
    for row in rows:
        sample=row.get('measurement') or {}
        require(row.get('attempt') in attempts and sample.get('SessionId')==row.get('attempt'),'Camera attempt mismatch')
        require(type(row.get('frame')) is int and row['frame']>0 and sample.get('Frame')==row['frame'],'Camera exact frame mismatch')
        require(row.get('scope')=='RawCameraMeasurementOnly' and row.get('cameraAccepted') is False and
                row.get('fullMissionCertified') is False,'Camera scope must remain unaccepted')
        require(not row.get('failure'),'Camera observer failed')
        require(sample.get('EntityIndex',0)>0 and sample.get('EntityVersion',0)>0 and bool(sample.get('StableActorId')),'Native camera actor identity missing')
        # Invalid/Far/offscreen samples are valuable calibration evidence, not passes.
        require(type(sample.get('Valid')) is bool and (sample['Valid'] or bool(sample.get('Failure'))),'Invalid sample must preserve failure')
        errors.extend(inspect_png(row))
        if row.get('phase')=='paid-transport':
            require((sample.get('EntityIndex'),sample.get('EntityVersion'),sample.get('StableActorId'),row.get('paidReservation'))==
                    (plane.get('actorIndex'),plane.get('actorVersion'),plane.get('stable'),plane.get('reservation')),
                    'Camera exact paid Plane lineage mismatch')
            require(sample.get('NativeRole')=='TransportPlane' and row.get('frame',0)<=plane.get('frame',-1),'Camera Plane role/order differs')
            if 'aircraftState' in row:
                require(row.get('actorTransformSource')=='UnparentedLocalTransform',
                        'Aircraft world height lacks explicit unparented-root transform proof')
                flags=[row.get(key) for key in ('airborne','takeoffRolling','landingRolling','returningHome')]
                require(all(type(value) is int and value==0 for value in flags) and row['aircraftState']=='Grounded',
                        'Delivered-only camera phase must explicitly remain grounded; no cruise inference')
                require(all(type(row.get(key)) in (int,float) and math.isfinite(row[key]) for key in ('nativeCruiseY','actorY','cameraY','verticalCameraSeparation')),
                        'Native aircraft/camera altitude facts invalid')
                require(abs(row.get('cameraY',math.inf)-row.get('actorY',0)-row.get('verticalCameraSeparation',0))<.001,
                        'Actual aircraft/camera vertical separation differs')
        else:
            require(sample.get('NativeCategory')=='Infantry' and row.get('infantryCount',0)>0 and row.get('paidReservation')==0,'Opening Infantry census absent')
    opening=[r for r in rows if r.get('phase')=='opening-infantry']
    require(bool(opening) and all(r.get('infantryCount')==len(opening) for r in opening),'Opening Infantry count mismatch')
    require(len([r for r in rows if r.get('phase')=='paid-transport'])==1,'Unique Transport camera sample required')
    invalid=[dict(phase=r.get('phase'),frame=r.get('frame'),failure=(r.get('measurement') or {}).get('Failure') or r.get('failure'))
             for r in rows if (r.get('measurement') or {}).get('Valid') is not True]
    return dict(evidenceCaptured=not errors,errors=errors,invalidMeasurements=invalid,
                allMeasurementsValid=bool(rows) and not invalid,
                aircraftStates=[dict(state=r.get('aircraftState','NotRecordedV2'),nativeCruiseY=r.get('nativeCruiseY'),actorY=r.get('actorY'),cameraY=r.get('cameraY'))
                                for r in rows if r.get('phase')=='paid-transport'],
                cameraAccepted=False,playerReady=False)
