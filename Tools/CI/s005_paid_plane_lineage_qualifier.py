"""Scoped new normal paid Airport->Plane raw qualifier; no full mission certification."""
import math
from audit_skirmish_native_contacts import audit

MARKER="[S005NormalPaidPlaneLineage] result=Passed scope=NormalAirportPlaneDeliveryOnly fullMissionCertified=0 playerReady=0"


def qualify(rows, contacts, log, wrapper_exit, source_unchanged):
    errors=[]
    def require(value,message):
        if not value: errors.append(message)
    def stage(name): return [r for r in rows if r.get("stage")==name]
    require(wrapper_exit==0,"Checked wrapper nonzero")
    require(source_unchanged is True,"Exact source/build guards not satisfied")
    require(MARKER in log and "[S004MenuJourney] result=Passed input=Touch controls=Skirmish,Scroll,S005,Launch seedFixture=155926 nativeSessionObserved=1" in log,
            "Real public Menu launch or scoped native marker absent")
    require(not any(r.get("stage")=="Failure" for r in rows) and "result=Failed" not in log,"Raw failure preserved")
    baseline=stage("AirportPaymentBaseline")
    handoff=stage("PaidAirportHandoff")
    delivered=stage("PaidPlaneDeliveredAtHome")
    require(len(baseline)==len(handoff)==len(delivered)==1,"Unique baseline/handoff/delivered boundaries required")
    before=baseline[0] if len(baseline)==1 else {}
    airport=handoff[0] if len(handoff)==1 else {}
    plane=delivered[0] if len(delivered)==1 else {}
    attempt=plane.get("attempt")
    proofs=[r for r in rows if r.get("stage") not in ("Started","ScopedComplete")]
    require(bool(attempt) and all(r.get("attempt")==attempt for r in proofs),"Exact same native attempt absent")
    require(bool(proofs) and all(r.get("timeScale")==1 and r.get("normalBudget") is True and
            not any(r.get(k,0) for k in ("unexpected","interventions","violations","stockStrikeReservations")) for r in proofs),
            "Normal speed/budget/input invariant absent")
    require(all(r.get("catalog")=="S005" and r.get("definition")=="skirmish.s005" and r.get("seed")==155926 for r in proofs),
            "Actual resolved S005 setup differs")
    require(before.get("airportIndex")==0 and before.get("airportCost",0)>0 and
            before.get("constructionSpent")==before.get("constructionBefore"),"Paid Airport baseline/quote absent")
    require(airport.get("frame",-1)>before.get("frame",math.inf) and
            airport.get("airportCost")==before.get("airportCost") and
            airport.get("constructionSpent",0)-before.get("constructionSpent",0)==before.get("airportCost",-1),
            "Exact real normal-funded Airport construction expenditure absent")
    identity=(airport.get("producerRuntimeId"),airport.get("airportIndex"),airport.get("airportVersion"),airport.get("airportStable"))
    require(all(isinstance(i,int) and i>0 for i in identity[:3]) and identity[3]=="constructed."+str(identity[0]) and
            airport.get("airportHp",0)>0 and airport.get("normalMenu") is True,"Unique constructed living owned Airport absent")
    paid=[r for r in proofs if r.get("reservation",0)>0]
    purchase=stage("PublicPlanePurchaseTouch")
    require(len(purchase)==1 and purchase[0].get("reservation",0)==0 and
            purchase[0].get("frame",-1)>airport.get("frame",math.inf) and
            purchase[0].get("dispatched",0)>airport.get("dispatched",0) and
            all(r.get("frame",-1)>=purchase[0].get("frame",math.inf) for r in paid),
            "Exact presented public Plane purchase Touch/production ordering absent")
    gesture=purchase[0] if len(purchase)==1 else {}
    driver=(gesture.get("device"),gesture.get("generation"))
    touch_key=(*driver,gesture.get("dispatched"))
    events=[r for r in contacts if (r.get("device"),r.get("generation"),r.get("dispatched"))==touch_key]
    require(all(isinstance(i,int) and i>0 for i in touch_key) and
            [r.get("kind") for r in events]==["Dispatched","Began","Ended"] and
            all(r.get("phase") in ("Runtime","Manual","PlayerResume") and r.get("commandAuditActive") is True for r in events) and
            events[0].get("frame")==gesture.get("frame") and
            events[0].get("frame",-1)<=events[1].get("frame",-2)<=events[2].get("frame",-3),
            "Purchase does not join exact ordinary native driver/generation/contact frames")
    ended=events[-1] if len(events)==3 else {}
    require(all((r.get("device"),r.get("generation"))==driver and
                r.get("frame",-1)>=ended.get("frame",math.inf) and
                r.get("completed",-1)>=gesture.get("dispatched",math.inf) and
                r.get("dispatched",-1)>=gesture.get("dispatched",math.inf) for r in paid),
            "Receipt/delivery precedes purchase Ended or regresses its exact driver counters")
    previous={}
    for row in proofs:
        key=(row.get("device"),row.get("generation"))
        current=(row.get("frame",-1),row.get("completed",-1),row.get("dispatched",-1))
        if key in previous:
            require(all(a<=b for a,b in zip(previous[key],current)),"Native driver/frame/contact counters regressed")
        previous[key]=current
    receipt_ids={r.get("reservation") for r in paid}
    require(len(receipt_ids)==1,"Unique actual public Plane reservation absent")
    require(bool(paid) and all(r.get("faction")==1 and r.get("role")=="TransportPlane" and r.get("paid")==440 and
            r.get("members")==1 and (r.get("producerRuntimeId"),r.get("airportIndex"),r.get("airportVersion"),r.get("airportStable"))==identity
            for r in paid),"Paid Plane440 receipt/owned Airport lineage mismatch")
    require(any(r.get("phase") in ("Reserved","Producing") and r.get("frame",-1)<plane.get("frame",-1) for r in paid),
            "Native production lifecycle before delivery absent")
    require(plane.get("frame",-1)>airport.get("frame",math.inf) and plane.get("phase")=="Live" and
            plane.get("delivered")==1 and plane.get("remaining")==1 and plane.get("members")==plane.get("delivered") and
            plane.get("actorIndex",0)>0 and plane.get("actorVersion",0)>0 and
            plane.get("stable")=="production."+str(plane.get("reservation"))+".0" and plane.get("actorHp",0)>0 and
            all(plane.get(k) is True for k in ("actorNativeShared","homeInitialized","usesRunway")) and
            plane.get("transit")==0 and 0<=plane.get("homeDistance",math.inf)<=1.5 and
            plane.get("completed",0)>0 and plane.get("completed")==plane.get("dispatched"),
            "Exact paid native Plane/home/parking/settled delivery absent")
    actor_rows=[r for r in paid if r.get("actorIndex",0)>0]
    require(len({(r.get("actorIndex"),r.get("actorVersion"),r.get("stable")) for r in actor_rows})==1,
            "Paid Plane native actor identity changed")
    require(plane.get("productionSpent",0)-airport.get("productionSpent",0)>=440,"Native production expenditure missing")
    require(all(isinstance(r.get("materials"),int) and 0<=r["materials"]<=r.get("capacity",-1) for r in proofs),
            "Native Materials capacity/budget observations invalid")
    try: touch=audit(contacts)
    except (KeyError,ValueError,TypeError) as error: touch=dict(strictRecordedContacts=False,errors=[str(error)])
    require(touch.get("strictAllContactsEnded") is True and touch.get("nativeMenuContactsRecorded") is True,
            "Strict actual Menu/normal Touch pairing absent")
    require(any(r.get("phase") in ("Runtime","Manual","PlayerResume") for r in contacts),"Native gameplay contact absent")
    return dict(result="PassedScopedNormalAirportPlaneDelivery" if not errors else "Failed",qualified=not errors,
                attempt=attempt,errors=errors,contacts=touch,fullMissionCertified=False,playerReady=False,
                native_qualification="pending_independent_native_canary_review",
                pending=["cargo Board/flight/Return/unload","normal Victory/result/return","device/player acceptance","owner screenshot review"])
