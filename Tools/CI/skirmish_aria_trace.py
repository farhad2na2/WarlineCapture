#!/usr/bin/env python3
"""Diagnose executed ARIA action loops from a planner replay or normalized trace.

This checker observes receipts and public state. It does not execute commands,
invent action success, or qualify a mission.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path

MAX_BYTES = 64 * 1024 * 1024  # Match the planner runner's maximum emitted trace.
MAX_FRAMES = 100_000
PROGRESS_FIELDS = (
    "EnemyHealth", "Infantry", "OwnStructureAttackUnits", "OwnAttackAirLive",
    "OwnAttackAirActive", "OwnAirFuel", "Readiness", "OwnDefenseTowers",
    "LogisticsTruckCommitted", "TankersCommitted", "PadReady",
    "ObjectiveSupport.Readiness", "ObjectiveSupport.DismountedCombatGroups",
    "ObjectiveSupport.CaptureMajority", "AircraftService.AtHome",
    "PublicObjective.Finished", "Finished",
)
RECEIPT_STATUSES = {"pending", "accepted", "completed", "rejected", "no_effect"}


def need(condition, message):
    if not condition:
        raise ValueError(message)


def integer(value, name, minimum=0):
    need(type(value) is int and minimum <= value <= 2**63 - 1, name + " must be a bounded integer")
    return value


def number(value, name):
    need(type(value) in (int, float) and math.isfinite(value) and value >= 0, name + " must be finite and nonnegative")
    return float(value)


def field(value, path):
    for part in path.split("."):
        if not isinstance(value, dict) or part not in value:
            return None
        value = value[part]
    need(value is None or type(value) in (bool, int, float, str), "Progress fields must be scalar: " + path)
    need(type(value) is not float or math.isfinite(value), "Nonfinite progress field: " + path)
    return value


def analyze(trace, repeat_limit=5, stall_seconds=15.0, action_limit=12, progress_fields=PROGRESS_FIELDS):
    need(isinstance(trace, dict) and type(trace.get("schema_version")) is int
         and trace["schema_version"] == 1, "Trace schema_version must be 1")
    rows = trace.get("frames")
    need(isinstance(rows, list) and 0 < len(rows) <= MAX_FRAMES, "A bounded nonempty frames list is required")
    need(type(repeat_limit) is int and repeat_limit >= 2, "repeat_limit must be at least 2")
    need(type(action_limit) is int and action_limit >= repeat_limit, "action_limit must be at least repeat_limit")
    stall_seconds = number(stall_seconds, "stall_seconds")
    need(stall_seconds > 0, "stall_seconds must be positive")
    need(progress_fields and len(set(progress_fields)) == len(progress_fields), "Unique progress fields required")
    need(not any(p in {"Time", "Frame"} for p in progress_fields), "Clock/frame are not mission progress")
    issues, reported, receipts = [], set(), {}
    previous_session, previous_progress = None, None
    last_time, last_frame, progress_at = -1.0, -1, 0.0
    repeats, failures, proposal_key, proposal_count = {}, {}, None, 0
    executed = accepted = rejected = no_effect = total_since_progress = 0
    receipt_evidence = False

    def issue(kind, severity, index, now, target, intent, **detail):
        key = (kind, target)
        if key not in reported and len(issues) < 64:
            reported.add(key)
            issues.append(dict(kind=kind, severity=severity, index=index, time=now,
                               target_id=target, intent=intent, **detail))

    for index, row in enumerate(rows):
        need(isinstance(row, dict), "Every frame must be an object")
        observation = row.get("observation")
        need(isinstance(observation, dict), "Each frame must retain its public observation")
        now = number(row.get("time", observation.get("Time")), "Frame time")
        frame = integer(row.get("frame", observation.get("Frame")), "Frame number")
        need(now >= last_time and frame >= last_frame, "Time and frames cannot move backwards")
        need(isinstance(row.get("before"), dict) and isinstance(row.get("after"), dict),
             "Before/after state objects required")
        before = row["before"].get("session")
        after = row["after"].get("session")
        need(isinstance(before, dict) and isinstance(after, dict), "Before/after session objects required")
        before_actions = integer(before.get("Actions"), "Before Actions")
        after_actions = integer(after.get("Actions"), "After Actions")
        need(after_actions >= before_actions, "Actions decreased inside a planner step")
        if previous_session is not None:
            need(before_actions >= previous_session["Actions"], "Session action reset requires a separate trace")
        progress = {p: field(observation, p) for p in progress_fields}
        need(any(v is not None for v in progress.values()), "No recognized mission progress observations")
        if previous_progress is None or progress != previous_progress:
            progress_at, total_since_progress, repeats = now, 0, {}
        intent = str(row.get("intent", ""))
        target = integer(row.get("target_id", 0), "Target", -(2**63))
        completed_target = before.get("LastCompletedTargetId") or before.get("TargetId", 0)
        completed_target = integer(completed_target, "Completed target", -(2**63))
        delta = after_actions - (previous_session["Actions"] if previous_session is not None else before_actions)
        count = delta
        feedback = row.get("feedback")
        feedback = {} if feedback is None else feedback
        need(isinstance(feedback, dict), "Feedback must be an object")
        receipt = feedback.get("receipt")
        receipt_failure = None
        if receipt is not None:
            need(isinstance(receipt, dict), "Receipt must be an object")
            rid, status = receipt.get("id"), receipt.get("status")
            need(type(rid) in (int, str) and not isinstance(rid, bool) and str(rid), "Receipt identity required")
            need(status in RECEIPT_STATUSES, "Unknown receipt status")
            rid = (type(rid).__name__, str(rid))
            seen = receipts.setdefault(rid, set())
            fresh = status not in seen
            receipt_evidence = True
            completed_target = integer(receipt.get("target_id", completed_target), "Receipt target", -(2**63))
            if fresh:
                if status in {"accepted", "completed"}:
                    if not seen.intersection({"accepted", "completed", "rejected", "no_effect"}):
                        count = max(count, 1)
                    if status == "accepted":
                        accepted += 1
                elif status in {"rejected", "no_effect"}:
                    receipt_failure = status
                    rejected += status == "rejected"
                    no_effect += status == "no_effect"
                seen.add(status)
        # The same presented control can cycle through planner intents. Changing
        # that label is neither a new control nor evidence that a rejection ended.
        key = completed_target
        if receipt_failure:
            failures[key] = failures.get(key, 0) + 1
            if failures[key] >= repeat_limit:
                issue("repeated_" + receipt_failure + ("_target_unknown" if not completed_target else ""),
                      "error", index, now, completed_target, intent,
                      count=failures[key], seconds_without_progress=now - progress_at)
        if count > 0:
            if receipt_failure is None:
                failures.pop(key, None)
            executed += count
            total_since_progress += count
            repeats[key] = repeats.get(key, 0) + count
            stalled = now - progress_at
            if completed_target and repeats[key] >= repeat_limit and stalled >= stall_seconds:
                issue("repeated_action_without_progress", "error", index, now, completed_target, intent,
                      count=repeats[key], seconds_without_progress=stalled)
            if total_since_progress >= action_limit and stalled >= stall_seconds:
                issue("action_cycle_without_progress", "error", index, now, completed_target, intent,
                      count=total_since_progress, seconds_without_progress=stalled)
        if (after.get("Phase") == "Blocked" and before.get("Phase") != "Blocked"
                and observation.get("Finished") is not True):
            issue("planner_blocked_before_completion", "error", index, now, completed_target, intent,
                  stop_reason=after.get("StopReason"), executed_actions=executed,
                  seconds_without_progress=now - progress_at)
        proposed = (target, intent)
        proposal_count = proposal_count + 1 if proposed == proposal_key else 1
        if (target and proposal_count >= action_limit and executed == 0
                and now - rows[0].get("time", rows[0]["observation"].get("Time", now)) >= stall_seconds):
            issue("proposal_repeated_without_execution", "inconclusive", index, now, target, intent,
                  count=proposal_count)
        proposal_key, previous_session, previous_progress = proposed, after, progress
        last_time, last_frame = now, frame

    status = "failed" if any(x["severity"] == "error" for x in issues) else "inconclusive" if issues else "passed"
    return dict(schema_version=1, scope="aria-trace-liveness", status=status, frames=len(rows),
                executed_actions=executed, accepted_receipts=accepted, rejected_receipts=rejected,
                no_effect_receipts=no_effect, receipt_evidence=receipt_evidence,
                progress_fields=list(progress_fields), issues=issues,
                thresholds=dict(repeat_limit=repeat_limit, stall_seconds=stall_seconds, action_limit=action_limit),
                full_battle_simulated=False, mission_qualified=False)


def unique_object(pairs):
    value = {}
    for key, item in pairs:
        need(key not in value, "Duplicate JSON key: " + key)
        value[key] = item
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--repeat-limit", type=int, default=5)
    parser.add_argument("--stall-seconds", type=float, default=15)
    parser.add_argument("--action-limit", type=int, default=12)
    args = parser.parse_args()
    need(args.input.is_file() and args.input.stat().st_size <= MAX_BYTES, "Bounded trace input required")
    raw = args.input.read_bytes()
    need(len(raw) <= MAX_BYTES, "Trace input grew beyond limit")
    trace = json.loads(raw, object_pairs_hook=unique_object,
                       parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Nonfinite JSON: " + value)))
    report = analyze(trace, args.repeat_limit, args.stall_seconds, args.action_limit)
    report["input_sha256"] = hashlib.sha256(raw).hexdigest()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x") as destination:
        json.dump(report, destination, indent=2, allow_nan=False)
        destination.write("\n")
    print("[AriaTrace] status=" + report["status"] + " issues=" + str(len(report["issues"])))
    return {"passed": 0, "failed": 1, "inconclusive": 2}[report["status"]]


if __name__ == "__main__":
    raise SystemExit(main())
