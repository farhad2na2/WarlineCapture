"""Offline contract controls only; synthetic positives never establish native readiness."""
import copy
import json
from pathlib import Path
import subprocess
import unittest

from s005_windows_north_qualification import ANCHORS, EXPECTED, MARKERS, ROUTE, qualify
from s005_windows_source_snapshot import candidate_hash


def synthetic():
    attempt = "synthetic-never-native"
    data = {key: [] for key in ("observation", "home", "north", "fog", "direct", "stop", "scan",
                                "reinforcement", "contacts", "commands", "planner")}
    data["observation"] = [dict(stage="ActualResolvedBinding", attempt=attempt, **EXPECTED),
        dict(stage="NormalDriverFlags", normalDriverFlags=True),
        dict(stage="NativePaidReservation", faction=1, role="ApcHeavy", materialsPaid=280, expectedCost=280,
             deliveredMembers=1, members=1, reservation=1003),
        dict(stage="NativePaidActorState", reservation=1003, actorHealth=360, role="ApcHeavy",
             actorStableId="production.1003.0", actorEntityIndex=2577, actorEntityVersion=151),
        dict(stage="NativeRenderedResultText", frozenVictory=True, renderedVictoryWitnessed=True,
             renderedTitle=True, localizedVictoryCopy=True, outcome="Victory"),
        dict(stage="NativeMainMenuDestination", frozenVictory=True, renderedVictoryWitnessed=True,
             unobscuredDestination=True, destinationSessionAbsent=True, exactMainMenuOwner=True, exactMainMenuTitle=True)]
    data["home"] = [dict(stage="ExactPaidScoutStageCandidate",defender="production.1003.0",defenderRole="ApcHeavy",group=7),
        dict(stage="PaidScoutStageHeld", scoutStaged=True, held=True, selected=True, touchSettled=True,
             passed=True, scoutMoveRequestId=18, scoutMoveBaseline=17,defender="production.1003.0",defenderRole="ApcHeavy",group=7,
             actorEntityIndex=2577,actorEntityVersion=151)]
    originals = ["original-" + str(i) for i in range(4)]
    for leg, anchor in enumerate(ANCHORS):
        data["north"].append(dict(stage="RouteGeometry", anchor=anchor, route=ROUTE, postPaid=True,goalX=10,goalZ=11))
        for ordinal,stable in enumerate(originals):
            data["north"].append(dict(stage="WaypointReached", anchor=anchor, stable=stable, frame=100+leg*10,
                                      receipt=11+leg, alive=True, ownCurrent=True, postPaid=True,leg=leg))
            data["north"].append(dict(stage="RouteActor", anchor=anchor, stable=stable, role="Rocketeer",
                                      alive=True, ownCurrent=True, distance=1, x=10, z=10, goalX=10, goalZ=11,
                                      receipt=11+leg,frame=100+leg*10,leg=leg,sourceIndex=ordinal+10,sourceVersion=1))
        for ordinal in range(4):
            data["reinforcement"].append(dict(stage="ReinforcementRouteActor",anchor=anchor,stable="production.1005."+str(ordinal),
                ownCurrent=True,postPaid=True,distance=1,x=10,z=10,goalX=10,goalZ=11,receipt=11+leg,frame=300+leg*10,leg=leg,round=1))
        data["reinforcement"].append(dict(stage="ReinforcementWaypointReached", anchor=anchor,
            allLivingReached=True, livingMembers=4, reachedMembers=4, receipt=11+leg,frame=300+leg*10,leg=leg,round=1))
    data["north"].extend(dict(stage="RouteCompleted", stable=stable, alive=True, ownCurrent=True, postPaid=True)
                         for stable in originals)
    data["reinforcement"].extend([dict(stage="ReinforcementRouteStarted", stable="production.1005."+str(i),postPaid=True)
                                  for i in range(4)])
    data["reinforcement"].append(dict(stage="ReinforcementRouteCompleted",frame=340,receipt=15,round=1,leg=5,
        livingMembers=4,reachedMembers=4,allLivingReached=True,ownCurrent=True,postPaid=True))
    data["observation"].append(dict(stage="NativePaidReservation",role="Rocketeer",faction=1,materialsPaid=120,
                                   expectedCost=120,deliveredMembers=4,reservation=1005))
    data["planner"]=[dict(stage="VerifiedSelected",paidLive=True,selected=True,reservation=1005)]
    data["stop"] = [dict(stage="StopAcceptedComplete", failedBitMask=0, allOriginalAlive=True,
        allOriginalSelected=True, allActorsNearStill=True, pairedTouch=True, feedbackFresh=True)]
    data["fog"] = [dict(stage="EarlyScanNativeResultBound", verified=True, receipt=31, nativeRequestId=31,
        nativeRequestBaseline=30, scoutIndex=2577, scoutVersion=151)]
    data["scan"] = [dict(requestId=31, orderPresent=True, hasStarted=True, scoutAlive=True, duration=20,
        scoutIndex=2577, scoutVersion=151)]
    data["direct"] = [dict(stable=stable, role="Rocketeer", ownerAttempt=attempt, ownerFaction=1,
        health=100, selected=True, ownerCurrent=True, attackTargetExact=True, attackMovePresent=True,
        pathMatches=True, visible=True, receipt=41, targetIndex=100, targetVersion=2,targetStable="enemy-base",
        entityIndex=i+10,entityVersion=1) for i,stable in enumerate(originals)]
    data["north"].append(dict(stage="FlankBaseDamage", stable=originals[0], qualified=True, damage=20,
        ownCurrent=True, role="Rocketeer", sourceKind="GroundMissile", eventFrame=200, directReceiptFrame=190,
        shotCounter=2, directBaselineShot=1, healthAfter=980, directTargetHp=1000, targetAttempt=attempt,
        targetOwnerFaction=2, targetObjectiveFaction=2, designatedBase=True, targetNativeDesignated=True,
        targetCurrentAttempt=True, targetTypedEnemyBase=True, resolvedBindingExact=True,
        uniqueTypedDesignatedTarget=True, visible=True, receipt=41, targetIndex=100, targetVersion=2,
        targetStable="enemy-base",sourceIndex=10,sourceVersion=1))
    data["commands"] = [dict(receipt=i, verified=True,operation="Move",frame=90) for i in (11,12,13,14,15,31)]
    data["commands"].extend(dict(receipt=41,verified=True,operation=operation,frame=190)
                            for operation in ["AttackMove","ArmyAttack"]*4)
    for contact, phase in enumerate(("Menu", "Manual", "Terminal"), start=1):
        for index, kind in enumerate(("Dispatched", "Began", "Ended")):
            data["contacts"].append(dict(device=1, generation=1, dispatched=contact,
                completed=contact if index==2 else contact-1, kind=kind, phase=phase, commandAuditActive=True))
    for name,stream in data.items():
        for row in stream:
            row["attempt"]=attempt
            row["timeScale"]=1
    return data, "\n".join(MARKERS)


class QualificationControls(unittest.TestCase):
    def test_windows_ordinal_binarywriter_oracle(self):
        # Independently produced by .NET StringComparer.Ordinal + BinaryWriter,
        # including the path order that normalized slash sorting gets wrong.
        manifest=[dict(path="Assets/A/Z.cs",sha256="0"*64),dict(path="Assets/AB.cs",sha256="0"*64)]
        self.assertEqual(candidate_hash(manifest),"8896ec53bbb6e6a2de1dcfb5c6730ba6c172ab36d47ac02f4e8a4c5e5f365e4c")

    def test_synthetic_shape_has_scoped_pass_but_no_readiness(self):
        data, log = synthetic()
        result = qualify(data, log, 0, True, "1632")
        self.assertEqual(result["errors"], [])
        self.assertTrue(result["qualified"])
        self.assertFalse(result["playerReady"])
        self.assertEqual(result["native_qualification"], "pending_independent_native_review")

    def test_corrupted_raw_gates_reject(self):
        base, log = synthetic()
        mutations = [
            ("paid receipt", lambda d: d["observation"][3].update(reservation=1004)),
            ("paid budget", lambda d: d["observation"][2].update(materialsPaid=0)),
            ("paid scout", lambda d: d["scan"][0].update(scoutIndex=99)),
            ("actor target", lambda d: d["direct"][0].update(targetVersion=99)),
            ("pre-receipt damage", lambda d: d["north"][-1].update(eventFrame=180)),
            ("non-designated target", lambda d: d["north"][-1].update(targetNativeDesignated=False)),
            ("hidden target", lambda d: d["north"][-1].update(visible=False)),
            ("stale attempt", lambda d: d["north"][-1].update(attempt="old-attempt")),
            ("missing route", lambda d: d["north"].clear()),
            ("original casualty", lambda d: d["north"].append(dict(stage="RouteCompleted",stable="replacement",alive=True,ownCurrent=True,postPaid=True))),
            ("fresh cohort substitution", lambda d: next(r for r in d["reinforcement"] if r["stage"]=="ReinforcementRouteStarted").update(stable="original-0")),
            ("foreign staged APC",lambda d:d["home"][0].update(defender="foreign")),
            ("spliced route goals",lambda d:[r.update(goalX=99999,x=99999,receipt=999,frame=99999) for r in d["north"] if r["stage"]=="RouteActor"]),
            ("missing fresh positions",lambda d:d.update(reinforcement=[r for r in d["reinforcement"] if r["stage"]!="ReinforcementRouteActor"])),
            ("accelerated raw gameplay",lambda d:[r.update(timeScale=8) for stream in d.values() for r in stream]),
            ("unattributed result",lambda d:d["observation"][4].pop("attempt")),
            ("foreign damage actor",lambda d:d["north"][-1].update(sourceVersion=99)),
            ("wrong command operation",lambda d:[r.update(operation="Move") for r in d["commands"] if r["receipt"]==41]),
            ("later route actor swapped",lambda d:[r.update(sourceIndex=999) for r in d["north"] if r["stage"]=="RouteActor" and r["leg"]>0]),
            ("unattributed route actor",lambda d:[r.pop("attempt") for r in d["north"] if r["stage"]=="RouteActor"]),
            ("isolated route acceleration",lambda d:[r.update(timeScale=8) for r in d["north"]]),
            ("stage-only reinforcement completion",lambda d:d.update(reinforcement=[dict(stage=r["stage"]) if r["stage"]=="ReinforcementRouteCompleted" else r for r in d["reinforcement"]])),
            ("unpaired contact", lambda d: d["contacts"].pop()),
            ("physical takeover", lambda d: d["contacts"][0].update(interventions=1)),
            ("stale result", lambda d: d["observation"][4].update(frozenVictory=False)),
            ("session not cleaned", lambda d: d["observation"][5].update(destinationSessionAbsent=False)),
            ("raw failure", lambda d: d["home"].append(dict(stage="Failure"))),
        ]
        for name, mutate in mutations:
            with self.subTest(name=name):
                data = copy.deepcopy(base)
                mutate(data)
                self.assertFalse(qualify(data, log, 0, True, "1632")["qualified"])
        self.assertFalse(qualify(base, log, 1, True, "1632")["qualified"])
        self.assertFalse(qualify(base, log, 0, False, "1632")["qualified"])
        self.assertFalse(qualify(base, log.replace(MARKERS[-1], ""), 0, True, "1632")["qualified"])

    def test_actual_native1628_failure_cannot_be_grafted_into_success(self):
        root = Path(__file__).resolve().parents[2]
        prefix = "HEAD:Design/AgentReports/SkirmishExpansion/S005/"
        archived = {}
        for name, suffix in (("observation", "normal-paid-north-flank-observation"),
                             ("home", "manual-home-defense")):
            raw = subprocess.run(["rtk", "proxy", "git", "show", prefix+"native1628-"+suffix+".jsonl"],
                                 cwd=root, check=True, stdout=subprocess.PIPE).stdout.decode("utf-8")
            archived[name] = [json.loads(line) for line in raw.splitlines() if line.strip()]
        self.assertTrue(any("reservation1004" in r.get("reason", "") for r in archived["home"]))
        for synthetic_graft in (False, True):
            data, log = synthetic() if synthetic_graft else ({}, "")
            data.update(archived)
            result = qualify(data, log, 0, True, "1628")
            self.assertFalse(result["qualified"])
            self.assertIn("Raw failure retained in home", result["errors"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
