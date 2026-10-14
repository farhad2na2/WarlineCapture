#!/usr/bin/env python3
"""Strict raw S005 north-flank evidence checks; no Unity dispatch or readiness writes."""
import argparse
import json
import math
from pathlib import Path
import re
import hashlib

from audit_skirmish_native_contacts import audit
from s005_windows_source_snapshot import candidate_hash

ANCHORS = ["anchor.skirmish.db.ruins_" + suffix for suffix in
           ("own", "west", "north", "east", "far")]
ROUTE = "route.skirmish.db.flank_a"
EXPECTED = dict(catalog="S005", definition="skirmish.s005", seed=155926,
                locale="fa-IR", size="Standard", difficulty="Regular", army="CombinedArms",
                start="FieldBase", objective="BaseAssault", deadline=1080,
                sharedFog=True, developmentFullVision=False)
MARKERS = ["[S005CandidateAvailability] fixture=EditorMemoryOnly publication=InProgress shippedCatalogModified=0",
           "[S004MenuJourney] result=Passed input=Touch controls=Skirmish,Scroll,S005,Launch seedFixture=155926 nativeSessionObserved=1",
           "[S005RoleLifecycle] phase=PaidDelivery role=ApcHeavy",
           "[S004ManualReceiptAudit] result=Passed measured=1",
           "[S004ManualNative] result=Passed outcome=Victory",
           "[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=MAIN MENU",
           "[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:MAIN MENU",
           "[SkirmishValidationExit] result=Requested code=0"]


def rows(path):
    return [json.loads(line) for line in Path(path).read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def qualify(data, log, wrapper_exit, source_unchanged, run_id):
    errors = []

    def require(condition, reason):
        if not condition:
            errors.append(reason)

    def stage(stream, name):
        return [r for r in data.get(stream, []) if r.get("stage") == name]

    require(wrapper_exit == 0, "Checked Windows wrapper did not exit zero")
    require(source_unchanged is True, "Candidate source bytes changed or were not checked")
    for marker in MARKERS:
        require(marker in log, "Missing native marker: " + marker)
    require(not re.search(r"error CS\d+|Compilation failed|observerFailure=1|result=Failed", log),
            "Actual native log contains compile/observer/validation failure")
    for name, stream in data.items():
        require(not any(r.get("stage") in ("Failure", "StopGateFailed") or r.get("failed") is True
                        for r in stream), "Raw failure retained in " + name)
    bindings = stage("observation", "ActualResolvedBinding")
    require(len(bindings) == 1, "Exactly one actual S005 binding required")
    binding = bindings[0] if len(bindings) == 1 else {}
    attempt = binding.get("attempt")
    require(bool(attempt) and all(binding.get(k) == v for k, v in EXPECTED.items()),
            "Native setup/attempt differs from pinned S005 north configuration")
    require(any(r.get("normalDriverFlags") is True for r in stage("observation", "NormalDriverFlags")),
            "Normal public input driver flags absent")
    for name, stream in data.items():
        require(all(not r.get("attempt") or r["attempt"] == attempt for r in stream),
                "Cross-attempt raw evidence in " + name)
        require(all(not any(r.get(k, 0) for k in ("unexpected", "interventions", "violations")) for r in stream),
                "Input intervention/violation in " + name)
    proof_stages = {"NativePaidReservation", "NativePaidActorState", "ExactPaidScoutStageCandidate", "PaidScoutStageHeld",
        "RouteGeometry", "RouteActor", "WaypointReached", "RouteCompleted", "StopAcceptedComplete", "EarlyScanNativeResultBound",
        "FlankBaseDamage", "ReinforcementRouteStarted", "ReinforcementRouteActor", "ReinforcementWaypointReached",
        "ReinforcementRouteCompleted", "VerifiedSelected", "NativeRenderedResultText", "NativeMainMenuDestination"}
    for name, stream in data.items():
        require(all(r.get("attempt") == attempt and bool(attempt) for r in stream if r.get("stage") in proof_stages),
                "Key raw proof lacks exact attempt in " + name)
    speed_rows = [r for stream in data.values() for r in stream if "timeScale" in r]
    require(bool(speed_rows) and all(r["timeScale"] == 1 for r in speed_rows), "Raw normal-speed samples absent or changed")
    commands = data.get("commands", [])
    require(bool(commands) and all(r.get("verified") is True and r.get("receipt", 0) > 0 for r in commands),
            "Native verified command receipts missing")
    receipts = {r.get("receipt") for r in commands if r.get("verified") is True}
    try:
        contacts = audit(data.get("contacts", []))
    except (KeyError, TypeError, ValueError) as error:
        contacts = {"strictRecordedContacts": False, "errors": [str(error)]}
    require(contacts.get("strictRecordedContacts") is True and contacts.get("nativeMenuContactsRecorded") is True
            and contacts.get("terminalContactsRecorded") is True, "Native contact audit/Menu/return failed")
    paid = [r for r in stage("observation", "NativePaidReservation") if
            r.get("faction") == 1 and r.get("role") == "ApcHeavy" and
            r.get("materialsPaid") == 280 and r.get("expectedCost") == 280 and
            r.get("deliveredMembers") == 1 and r.get("members") == 1]
    paid_receipts = {r.get("reservation") for r in paid if r.get("reservation", 0) > 0}
    require(len(paid_receipts) == 1, "Unique genuinely paid Heavy APC reservation absent")
    paid_actors = [r for r in stage("observation", "NativePaidActorState") if
                   r.get("reservation") in paid_receipts and r.get("actorHealth", 0) > 0 and
                   r.get("role") == "ApcHeavy" and r.get("actorStableId")]
    actor_ids = {(r.get("actorEntityIndex"), r.get("actorEntityVersion")) for r in paid_actors}
    actor_stables = {r.get("actorStableId") for r in paid_actors}
    require(bool(actor_ids), "Living exact paid Heavy APC actor absent")
    staged = stage("home", "ExactPaidScoutStageCandidate")
    require(len(staged) == 1 and staged[0].get("defender") in actor_stables and
            staged[0].get("defenderRole") == "ApcHeavy" and staged[0].get("group", 0) > 0,
            "Unique paid scout stage candidate identity absent")
    held = stage("home", "PaidScoutStageHeld")
    require(any(r.get("scoutStaged") is True and r.get("held") is True and r.get("selected") is True
                and r.get("touchSettled") is True and r.get("passed") is True
                and r.get("defender") in actor_stables and r.get("defenderRole") == "ApcHeavy"
                and (r.get("actorEntityIndex"),r.get("actorEntityVersion")) in actor_ids
                and staged and r.get("group") == staged[0].get("group")
                and r.get("scoutMoveRequestId", 0) == r.get("scoutMoveBaseline", 0) + 1 for r in held),
            "Paid scout public Move/path/stable Hold proof absent")
    geometry = stage("north", "RouteGeometry")
    require(len(geometry) == 5 and {r.get("anchor") for r in geometry} == set(ANCHORS)
            and all(r.get("route") == ROUTE and r.get("postPaid") is True for r in geometry),
            "Exact existing five-waypoint north route geometry absent")
    completed = stage("north", "RouteCompleted")
    originals = {r.get("stable") for r in completed if r.get("alive") is True and
                 r.get("ownCurrent") is True and r.get("postPaid") is True and r.get("stable")}
    require(len(originals) == 4, "Four continuous original living Rocketeers must complete route")
    for stable in originals:
        identities = {(r.get("sourceIndex"),r.get("sourceVersion")) for r in stage("north","RouteActor") if r.get("stable") == stable}
        require(len(identities) == 1 and all(isinstance(i,int) and i>0 for pair in identities for i in pair),
                "Original actor entity identity changed: "+stable)
    route_frames = []
    for anchor in ANCHORS:
        goal = next((r for r in geometry if r.get("anchor") == anchor), {})
        reached = [r for r in stage("north", "WaypointReached") if r.get("anchor") == anchor and
                   r.get("alive") is True and r.get("ownCurrent") is True and r.get("postPaid") is True
                   and r.get("receipt") in receipts]
        require({r.get("stable") for r in reached} == originals and len(originals) == 4,
                "Original four exact accepted waypoint receipts absent: " + anchor)
        actors = [r for r in stage("north", "RouteActor") if r.get("anchor") == anchor and
                  r.get("stable") in originals and r.get("alive") is True and r.get("ownCurrent") is True and
                  r.get("role") == "Rocketeer" and r.get("distance", math.inf) <= 12 and
                  r.get("goalX") == goal.get("goalX") and r.get("goalZ") == goal.get("goalZ") and
                  any(w.get("stable") == r.get("stable") and w.get("frame") == r.get("frame") and
                      w.get("receipt") == r.get("receipt") and w.get("leg") == r.get("leg") for w in reached) and
                  any(c.get("receipt") == r.get("receipt") and c.get("operation") in ("Move", "AttackMove") and
                      c.get("frame", math.inf) <= r.get("frame", -1) for c in commands) and
                  math.hypot(r.get("x", math.inf)-r.get("goalX", 0), r.get("z", math.inf)-r.get("goalZ", 0)) <= 12]
        require({r.get("stable") for r in actors} == originals and len(originals) == 4,
                "Measured original actor positions absent: " + anchor)
        route_frames.append(min((r.get("frame", -1) for r in reached), default=-1))
    require(all(a < b for a, b in zip(route_frames, route_frames[1:])), "Waypoint frames are not ordered")
    stop = stage("stop", "StopAcceptedComplete")
    require(any(r.get("failedBitMask") == 0 and all(r.get(k) is True for k in
                ("allOriginalAlive", "allOriginalSelected", "allActorsNearStill", "pairedTouch", "feedbackFresh"))
                for r in stop), "Four original actors lack public Stop/selection/fresh feedback proof")
    scans = stage("fog", "EarlyScanNativeResultBound")
    require(any(r.get("verified") is True and r.get("receipt", 0) > 0 and
                r.get("nativeRequestId") == r.get("receipt") and
                r.get("nativeRequestId", 0) > r.get("nativeRequestBaseline", 0) and
                (r.get("scoutIndex"), r.get("scoutVersion")) in actor_ids for r in scans),
            "Accepted public Scan does not bind exact paid scout/native request")
    scan_ids = {r.get("receipt") for r in scans}
    require(any(r.get("requestId") in scan_ids and r.get("orderPresent") is True and
                r.get("hasStarted") is True and r.get("scoutAlive") is True and r.get("duration", 0) > 0 and
                (r.get("scoutIndex"), r.get("scoutVersion")) in actor_ids for r in data.get("scan", [])),
            "Actual living paid scout Scan lifecycle absent")
    attacks = [r for r in data.get("direct", []) if r.get("stable") in originals and
               r.get("role") == "Rocketeer" and r.get("ownerAttempt") == attempt and
               r.get("ownerFaction") == 1 and r.get("health", 0) > 0 and r.get("selected") is True and
               r.get("ownerCurrent") is True and r.get("attackTargetExact") is True and
               r.get("attackMovePresent") is True and r.get("pathMatches") is True and
               r.get("visible") is True and r.get("receipt") in receipts and
               any(a.get("stable") == r.get("stable") and
                   (a.get("sourceIndex"),a.get("sourceVersion")) == (r.get("entityIndex"),r.get("entityVersion"))
                   for a in stage("north", "RouteActor"))]
    require({r.get("stable") for r in attacks} == originals and len(originals) == 4,
            "Four exact original public Attack actor/target/path proofs absent")
    damage = [r for r in stage("north", "FlankBaseDamage") if r.get("stable") in originals and
              r.get("qualified") is True and r.get("damage", 0) > 0 and r.get("ownCurrent") is True and
              r.get("role") == "Rocketeer" and r.get("sourceKind") in ("GroundMissile", "DirectFire") and
              r.get("eventFrame", -1) > r.get("directReceiptFrame", math.inf) and
              r.get("shotCounter", 0) > r.get("directBaselineShot", math.inf) and
              r.get("healthAfter", math.inf) < r.get("directTargetHp", 0) and
              r.get("targetAttempt") == attempt and r.get("targetOwnerFaction") == 2 and
              r.get("targetObjectiveFaction") == 2 and
              all(r.get(k) is True for k in ("designatedBase", "targetNativeDesignated", "targetCurrentAttempt",
                  "targetTypedEnemyBase", "resolvedBindingExact", "uniqueTypedDesignatedTarget")) and
              (r.get("visible") is True or (r.get("visibleBeforeLethalHit") is True and r.get("healthAfter", 1) <= 0))]
    require(any(any(a.get("receipt") == d.get("receipt") and a.get("stable") == d.get("stable") and
                    (a.get("entityIndex"),a.get("entityVersion")) == (d.get("sourceIndex"),d.get("sourceVersion")) and
                    a.get("targetStable") == d.get("targetStable") and
                    (a.get("targetIndex"), a.get("targetVersion")) == (d.get("targetIndex"), d.get("targetVersion"))
                    for a in attacks) and
                [c.get("operation") for c in commands if c.get("receipt") == d.get("receipt") and
                 c.get("frame") == d.get("directReceiptFrame")] == ["AttackMove","ArmyAttack"]*4
                for d in damage), "Exact post-receipt designated-base native damage absent")
    fresh = stage("reinforcement", "ReinforcementRouteStarted")
    fresh_ids = {r.get("stable") for r in fresh if r.get("stable") and r.get("postPaid") is True}
    require(bool(fresh_ids) and not originals.intersection(fresh_ids), "Distinct paid fresh reinforcement absent")
    planner = stage("planner", "VerifiedSelected")
    fresh_receipts = {r.get("reservation") for r in planner if r.get("paidLive") is True and
                      r.get("selected") is True and r.get("reservation", 0) > 0}
    paid_fresh = {r.get("reservation") for r in stage("observation", "NativePaidReservation") if
                 r.get("role") == "Rocketeer" and r.get("faction") == 1 and r.get("materialsPaid") == 120 and
                 r.get("expectedCost") == 120 and r.get("deliveredMembers", 0) > 0}
    require(len(fresh_receipts) == 1 and fresh_receipts.issubset(paid_fresh) and
            all(any(stable.startswith("production."+str(receipt)+".") for receipt in fresh_receipts) for stable in fresh_ids),
            "Fresh cohort not bound to actual public paid reservation")
    fresh_frames = []
    surviving = set(fresh_ids)
    for anchor in ANCHORS:
        fresh_waypoints = [r for r in stage("reinforcement", "ReinforcementWaypointReached") if
                    r.get("anchor") == anchor and r.get("allLivingReached") is True and
                    r.get("livingMembers", 0) > 0 and r.get("livingMembers") == r.get("reachedMembers") and
                    r.get("receipt") in receipts]
        require(bool(fresh_waypoints),
                "Fresh reinforcement waypoint incomplete: " + anchor)
        goal = next((r for r in geometry if r.get("anchor") == anchor), {})
        measured = [r for r in stage("reinforcement", "ReinforcementRouteActor") if
                    r.get("anchor") == anchor and r.get("stable") in surviving and
                    r.get("ownCurrent") is True and r.get("postPaid") is True and r.get("distance", math.inf) <= 12 and
                    r.get("goalX") == goal.get("goalX") and r.get("goalZ") == goal.get("goalZ") and
                    math.hypot(r.get("x", math.inf)-r.get("goalX", 0),r.get("z", math.inf)-r.get("goalZ", 0)) <= 12 and
                    any(w.get("receipt") == r.get("receipt") and w.get("frame") == r.get("frame") and
                        w.get("round") == r.get("round") and w.get("leg") == r.get("leg") for w in fresh_waypoints)]
        surviving = {r.get("stable") for r in measured}
        require(bool(surviving) and any(w.get("livingMembers") == len(surviving) for w in fresh_waypoints),
                "Fresh actor continuity/positions absent: " + anchor)
        fresh_frames.append(min((w.get("frame", -1) for w in fresh_waypoints), default=-1))
    require(all(a < b for a,b in zip(fresh_frames,fresh_frames[1:])), "Fresh waypoint frames not ordered")
    require(any(c.get("frame") == w.get("frame") and c.get("receipt") == w.get("receipt") and
                c.get("round") == w.get("round") and c.get("leg") == 5 and
                c.get("livingMembers") == w.get("livingMembers") and c.get("reachedMembers") == w.get("reachedMembers") and
                all(c.get(k) is True for k in ("allLivingReached","ownCurrent","postPaid"))
                for c in stage("reinforcement", "ReinforcementRouteCompleted")
                for w in stage("reinforcement","ReinforcementWaypointReached") if w.get("anchor") == ANCHORS[-1]),
            "Fresh route completion lacks exact last native waypoint identity")
    require(any(r.get("frozenVictory") is True and r.get("renderedVictoryWitnessed") is True and
                r.get("attempt") == attempt and bool(attempt) and
                r.get("renderedTitle") is True and r.get("localizedVictoryCopy") is True and
                r.get("outcome") == "Victory" for r in stage("observation", "NativeRenderedResultText")),
            "Rendered frozen same-attempt Victory absent")
    require(any(r.get("attempt") == attempt and bool(attempt) and all(r.get(k) is True for k in ("frozenVictory", "renderedVictoryWitnessed", "unobscuredDestination",
                "destinationSessionAbsent", "exactMainMenuOwner", "exactMainMenuTitle"))
                for r in stage("observation", "NativeMainMenuDestination")), "Real rendered result-to-MainMenu return absent")
    return dict(result="PassedScopedNormalNorthFlank" if not errors else "Failed", qualified=not errors,
                run_id=str(run_id), attempt=attempt, errors=errors, contacts=contacts,
                native_qualification="pending_independent_native_review", playerReady=False,
                deviceAcceptance=False, realPlayerAcceptance=False)


def read_evidence(report, run_id):
    suffixes = dict(observation="normal-paid-north-flank-observation", north="north-flank",
                    fog="manual-fog-fallback", direct="direct-attack-actors", stop="stop-frames",
                    scan="scan-lifecycle", home="manual-home-defense", reinforcement="reinforcement-north-route",
                    planner="fresh-rocketeer-planner")
    data = {name: rows(report / ("native" + run_id + "-" + suffix + ".jsonl"))
            for name, suffix in suffixes.items()}
    bindings = [r for r in data["observation"] if r.get("stage") == "ActualResolvedBinding"]
    if len(bindings) != 1:
        raise ValueError("Exactly one trace binding required")
    trace = Path(bindings[0]["tracePath"])
    if trace.resolve().parent != report.resolve() or not re.fullmatch(r"s005-aria-live-155926-fa-[0-9a-f]{32}\.jsonl", trace.name):
        raise ValueError("Trace path escapes exact S005 report scope")
    data["commands"] = rows(Path(str(trace) + ".commands.jsonl"))
    data["contacts"] = rows(Path(str(trace) + ".touch.jsonl"))
    return data


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--log", type=Path, required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--source-before", type=Path, required=True)
    parser.add_argument("--source-after", type=Path, required=True)
    parser.add_argument("--job", type=Path, required=True)
    parser.add_argument("--claim", type=Path, required=True)
    parser.add_argument("--wrapper-receipt", type=Path, required=True)
    args = parser.parse_args()
    try:
        before = json.loads(args.source_before.read_text())
        after = json.loads(args.source_after.read_text())
        job = json.loads(args.job.read_text(encoding="utf-8-sig"))
        claim = json.loads(args.claim.read_text(encoding="utf-8-sig"))
        receipt = json.loads(args.wrapper_receipt.read_text(encoding="utf-8-sig"))
        digest = hashlib.sha256(json.dumps(job,sort_keys=True,separators=(",",":")).encode()).hexdigest()
        if not (receipt.get("job_sha256") == claim.get("job_sha256") == digest and
                receipt.get("attempt_id") == claim.get("attempt_id") and receipt.get("wrapperExit") == args.wrapper_exit and
                receipt.get("executeMethod") == "S005NormalNorthFlank"+args.run_id+"Observer.RunValidation"):
            raise ValueError("Wrapper/queue job/attempt ownership mismatch")
        expected_manifest = sorted(job["source_manifest"], key=lambda r:r["path"])
        for snapshot in (before, after):
            if not snapshot.get("source_manifest") or snapshot["source_manifest"] != expected_manifest:
                raise ValueError("Complete source manifest differs from job")
            if snapshot.get("candidate_sha256") != job["candidate_sha256"] or candidate_hash(expected_manifest) != job["candidate_sha256"]:
                raise ValueError("Actual native candidate hash differs")
            build = snapshot.get("build_manifest")
            build_hash = hashlib.sha256(json.dumps(build,sort_keys=True,separators=(",",":")).encode()).hexdigest()
            if not build or build_hash != snapshot.get("build_sha256") or build_hash != job["build_sha256"]:
                raise ValueError("Compiled assembly byte manifest differs")
        result = qualify(read_evidence(args.report, args.run_id), args.log.read_text(errors="replace"),
                         args.wrapper_exit, bool(before) and before == after, args.run_id)
        result.update(job_sha256=digest,attempt_id=claim["attempt_id"],candidate_sha256=job["candidate_sha256"],build_sha256=job["build_sha256"])
    except (OSError, KeyError, TypeError, ValueError) as error:
        result = dict(result="Failed", qualified=False, run_id=args.run_id,
                      errors=["Raw evidence unavailable or malformed: " + str(error)],
                      native_qualification="pending_independent_native_review", playerReady=False)
    print(json.dumps(result, indent=2))
    raise SystemExit(0 if result["qualified"] else 2)
