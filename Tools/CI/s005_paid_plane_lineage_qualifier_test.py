"""Synthetic lineage contract controls; no Unity/native readiness evidence."""
import copy
import unittest
from pathlib import Path
from s005_paid_plane_lineage_qualifier import qualify,MARKER


def fixture():
    base=dict(attempt="synthetic-only",timeScale=1,normalBudget=True,catalog="S005",definition="skirmish.s005",seed=155926,
              faction=1,materials=1000,capacity=1200,constructionSpent=100,constructionBefore=100,airportCost=300,
              productionSpent=0,airportIndex=0,completed=1,dispatched=1,device=1,generation=1)
    baseline=dict(base,stage="AirportPaymentBaseline",frame=1)
    airport=dict(base,stage="PaidAirportHandoff",frame=20,constructionSpent=400,producerRuntimeId=55,airportIndex=80,
                 airportVersion=2,airportStable="constructed.55",airportHp=1000,normalMenu=True)
    pending=dict(airport,stage="NativeSample",frame=31,reservation=1003,role="TransportPlane",phase="Producing",paid=440,members=1,completed=2,dispatched=2)
    delivered=dict(pending,stage="PaidPlaneDeliveredAtHome",frame=200,phase="Live",delivered=1,remaining=1,actorIndex=90,
        actorVersion=3,stable="production.1003.0",actorHp=340,actorNativeShared=True,homeInitialized=True,usesRunway=True,transit=0,
        homeDistance=1,productionSpent=440)
    touches=[]
    for number,phase in enumerate(("Menu","Runtime"),1):
        for step,kind in enumerate(("Dispatched","Began","Ended")):
            touches.append(dict(device=1,generation=1,dispatched=number,completed=number if kind=="Ended" else number-1,
                                kind=kind,phase=phase,commandAuditActive=True,frame=(29+(step==2)) if number==2 else 0))
    log=MARKER+"\n[S004MenuJourney] result=Passed input=Touch controls=Skirmish,Scroll,S005,Launch seedFixture=155926 nativeSessionObserved=1"
    purchase=dict(airport,stage="PublicPlanePurchaseTouch",frame=29,dispatched=2,reservation=0)
    return [baseline,airport,purchase,pending,delivered],touches,log


class LineageControls(unittest.TestCase):
    def test_reachable_scoped_contract_has_no_full_readiness(self):
        result=qualify(*fixture(),0,True)
        self.assertEqual(result["errors"],[])
        self.assertTrue(result["qualified"])
        self.assertFalse(result["fullMissionCertified"])
        self.assertFalse(result["playerReady"])

    def test_corrupted_raw_lineage_rejects(self):
        originals,touches,log=fixture()
        mutations=[(0,"airportIndex",80),(0,"airportCost",0),(1,"constructionSpent",399),
            (1,"normalMenu",False),(2,"paid",0),(2,"producerRuntimeId",999),(2,"reservation",1004),
            (3,"actorVersion",0),(3,"stable","production.999.0"),(3,"actorHp",0),(3,"actorNativeShared",False),
            (3,"homeInitialized",False),(3,"usesRunway",False),(3,"homeDistance",10),(3,"transit",1),
            (3,"productionSpent",0),(3,"attempt","old"),(3,"timeScale",8),(3,"interventions",1),
            (3,"completed",9),(3,"phase","Producing"),(3,"materials",1300),(2,"phase","Live"),
            (3,"catalog","S006"),(3,"normalBudget",False)]
        mutations.extend([(4,"dispatched",10),(4,"frame",201),(4,"reservation",1003)])
        mutations=[({2:3,3:4,4:2}.get(i,i),key,value) for i,key,value in mutations]
        mutations.extend([(4,"completed",1),(3,"completed",1),(3,"dispatched",1),(2,"device",99),
                          (2,"generation",99),(3,"frame",28),(4,"generation",99),(4,"remaining",0),
                          (1,"stockStrikeReservations",1),(3,"stockStrikeReservations",1)])
        for index,key,value in mutations:
            with self.subTest(key=key,value=value):
                rows=copy.deepcopy(originals);rows[index][key]=value
                self.assertFalse(qualify(rows,touches,log,0,True)["qualified"])
        self.assertFalse(qualify(originals,touches[:-1],log,0,True)["qualified"])
        self.assertFalse(qualify(originals,touches,log,1,True)["qualified"])
        self.assertFalse(qualify(originals,touches,log,0,False)["qualified"])
        self.assertFalse(qualify(originals,touches,"",0,True)["qualified"])

    def test_new_source_has_no_gameplay_mutation_or_isolated_controller(self):
        root=Path(__file__).resolve().parents[2]
        source=(root/"Design/AgentReports/SkirmishExpansion/S005/windows-paid-plane-canary/NormalPaidPlaneLineageCanary.cs.txt").read_text()
        for forbidden in ("SetComponentData(","AddComponentData(","SkirmishMaterialsService.Write", ".Enabled = false",
                          "S005PaidTransportPlane1564","S005PaidEnemyAirEffects1548","SkirmishResearchService."):
            self.assertNotIn(forbidden,source)
        observation=source.split("static NormalPlaneLineageRow ObserveNormalPlaneLineage",1)[1].split("// Actuation",1)[0]
        for forbidden in ("PlayerResumeTap(","TouchFixedWingCatalog(",".Tick(",".SetValue("):
            self.assertNotIn(forbidden,observation)


if __name__=="__main__":unittest.main(verbosity=2)
