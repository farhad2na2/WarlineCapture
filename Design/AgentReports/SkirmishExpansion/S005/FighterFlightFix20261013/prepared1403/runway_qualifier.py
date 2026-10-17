"""Pure native runway census qualification; no Editor calls or gameplay state."""
import json,math

def require(condition,message):
    if not condition:raise ValueError(message)
def qualify(rows,text):
    queues=[json.loads(s.split('] ',1)[1]) for s in text.splitlines() if s.startswith('[S005EnemyAirPaidQueue] ')]
    payments=[json.loads(s.split('] ',1)[1]) for s in text.splitlines() if s.startswith('[S005BilateralAirportReceipt] ')]
    paid={r['faction']:r['runtimeId'] for r in payments if r['phase'] in ('PlayerNativeTouchPaid','EnemyNativeCompletionCaptured')}
    air=[r for r in queues if r['role'] in ('TransportPlane','Fighter')]
    require(len(air)==3 and len(paid)==2,'Three exact paid aircraft and bilateral paid Airport records required')
    groups={}
    for r in rows:
        require(r['stage'] in ('NativeBindingObserved','BindingPending'),'Unexpected binding stage')
        require(all(not isinstance(v,float) or math.isfinite(v) for v in r.values()),'Nonfinite native runway field')
        require(r['selectionRule']=='NearestSameFactionCenterFirstBufferTie' and r['homePoseWritten'] is False,'Native rule/provenance differs')
        key=tuple(r[k] for k in ('frame','actorIndex','actorVersion','reservationId','stage'))
        groups.setdefault(key,[]).append(r)
    bindings=[]
    for key,rr in groups.items():
        n=rr[0]['rowCount'];require(n>0 and len(rr)==n and [r['rowIndex'] for r in rr]==list(range(n)),'Incomplete/duplicate native runway buffer snapshot')
        header=('stage','role','attemptId','frame','faction','producerId','actorIndex','actorVersion','reservationId','rowCount','nearestRowIndex','homeInitialized','usesRunway','airborne','takeoffRolling','landingRolling','actorX','actorY','actorZ','actorTakeoffX','actorTakeoffZ','actorLandingX','actorLandingZ','actorTakeoffWorldX','actorTakeoffWorldY','actorTakeoffWorldZ','actorLandingWorldX','actorLandingWorldY','actorLandingWorldZ','worldTime','worldDelta','producerRunwayPresent','bindingMatch')
        require(all(all(r[k]==rr[0][k] for k in header) for r in rr),'Native snapshot header changed within buffer')
        h=rr[0];matches=[q for q in air if all(q[k]==h[k] for k in ('actorIndex','actorVersion','reservationId','faction','role','attemptId'))]
        require(len(matches)==1,'Runway actor is not exact actual paid aircraft')
        q=matches[0];require(h['producerId']==q['producerId']==paid[q['faction']] and h['producerRunwayPresent'] is True,'Unpaid/wrong paid producer identity')
        require(any(r['runwayRuntimeId']==h['producerId'] and r['runwayFaction']==h['faction'] for r in rr),'Paid producer runway absent')
        eligible=[]
        for r in rr:
            d=(r['centerX']-h['actorX'])**2+(r['centerZ']-h['actorZ'])**2
            require(abs(r['centerDistanceSq']-d)<=max(.02,d*2e-6),'Native horizontal runway distance inconsistent')
            require(r['sameFaction']==(r['runwayFaction']==h['faction']),'Native faction mask differs')
            if r['sameFaction']:eligible.append((d,r['rowIndex'],r))
        require(eligible,'No native same-faction runway')
        chosen=min(eligible,key=lambda a:(a[0],a[1]))[2]
        require(h['nearestRowIndex']==chosen['rowIndex'] and all(r['nearest']==(r['rowIndex']==chosen['rowIndex']) for r in rr),'Wrong nearest same-faction/first-tie runway')
        if h['stage']=='BindingPending':
            require(h['bindingMatch'] is False and h['frame']<=q['frame'],'Pending binding falsely accepted/after queued actor')
            continue
        require(h['frame']==q['frame'] and h['homeInitialized']==1 and h['usesRunway']==1 and h['airborne']==h['takeoffRolling']==h['landingRolling']==0 and h['bindingMatch'] is True,'Exact grounded native binding/queue frame absent')
        distance=lambda prefix:sum((chosen[prefix+'World'+c]-h['actor'+c])**2 for c in ('X','Z'))
        reverse=distance('landing')<distance('takeoff')
        first,second=('landing','takeoff') if reverse else ('takeoff','landing')
        for actorprefix,nativeprefix in (('actorTakeoff',first),('actorLanding',second)):
            require(all(h[actorprefix+c]==chosen['runway'+nativeprefix.title()+c] for c in ('X','Z')),'Native threshold cells differ from selected runway/orientation')
            require(sum((h[actorprefix+'World'+c]-chosen[nativeprefix+'World'+c])**2 for c in ('X','Y','Z'))<=.0025,'Native threshold world position differs')
        bindings.append(dict(frame=h['frame'],actorIndex=h['actorIndex'],actorVersion=h['actorVersion'],reservationId=h['reservationId'],faction=h['faction'],paidProducer=h['producerId'],nativeHomeRunway=chosen['runwayRuntimeId'],nativeRowIndex=chosen['rowIndex'],sameAsPaidProducer=chosen['runwayRuntimeId']==h['producerId'],orientationReversed=reverse))
    require(len(bindings)==3 and len({b['reservationId'] for b in bindings})==3,'Exactly three unique current native aircraft bindings required')
    return {'result':'PassedNativeNearestSameFactionRunwayBindings','bindings':bindings,'paidProducerStrict':True,'homePoseWritten':False,'normalMission':False}
