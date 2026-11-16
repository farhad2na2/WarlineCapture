#!/usr/bin/env python3
"""Check one explicit source-host recruit through the retained delivery trace."""
import argparse
import hashlib
import json
from pathlib import Path


def load(path):
    raw = path.read_bytes()
    return json.loads(raw), hashlib.sha256(raw).hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def audit(directory, output):
    receipt_path, result_path = directory / "run-receipt.json", directory / "result.json"
    receipt, receipt_hash = load(receipt_path)
    result, result_hash = load(result_path)
    need(receipt.get("mission") == "S073" and receipt.get("seed") == 104729 and
         receipt.get("timed_out") is False and receipt.get("process_exit") == 2 and
         receipt.get("validation_passed") is False and receipt.get("mission_ready") is False,
         "Checked unqualified S073 source run required")
    need(receipt.get("worker_result", {}).get("sha256") == result_hash and
         result.get("mission_ready") is False and result.get("native_player_input_observed") is False,
         "Actual result hash and non-native admission required")
    command = result["source_host_aria_policy_step"]["synthetic_player_recruit_diagnostic"]
    need(command.get("scope") == "ExplicitLogicalSourceCommand_NotNativeInput" and
         command.get("accepted") is True and command.get("result") == "None" and
         command.get("native_input_observed") is False and command.get("mission_ready") is False,
         "Accepted explicit logical source command required")
    need(command.get("authored_credits_cost") == 10000 and command.get("authored_materials_cost") == 20 and
         command.get("resource_policy") == 1 and command.get("credits_before") == command.get("credits_after") == 0 and
         command.get("materials_before") == 220 and command.get("materials_after") == 200 and
         command.get("pending_refundable_materials") == 20 and command.get("pending_paid_quantity") == 4,
         "Actual source resource and pending readback mismatch")
    runtime = result["source_host_original_tactical_owner_scheduler_tick"]["original_production_runtime"]
    events = runtime["source_delivery_events"]
    queue = [e for e in events if e.get("kind") == "queue_accepted" and
             e.get("source_time_seconds") == 0 and e.get("unit_prefab") == command["unit_prefab"]]
    need(len(queue) == 1, "One observed source queue event for the command required")
    producer = queue[0]["producer_id"]
    arrivals = [e for e in events if e.get("kind") == "helicopter_arrived" and e.get("producer_id") == producer]
    spawned = [e for e in events if e.get("kind") == "unit_spawned_and_placed" and
               e.get("producer_id") == producer and e.get("unit_prefab") == command["unit_prefab"]]
    need(len(arrivals) == 1 and len(spawned) == 4 and all(e.get("spawn_succeeded") is True and
         e.get("placement_observed") is True and e.get("entity_index", -1) >= 0 for e in spawned),
         "Original delivery owner must observe arrival and four placed units for this producer")
    need(output.parent.is_dir() and not output.exists(), "Fresh output in existing evidence directory required")
    evidence = {"kind": "S073SyntheticPlayerRecruitSourceDiagnosticAudit", "status": "Passed",
                "run_receipt_sha256": receipt_hash, "worker_result_sha256": result_hash,
                "producer_id": producer, "queue_events": len(queue), "arrivals": len(arrivals),
                "placed_units": len(spawned), "native_input_observed": False,
                "native_delivery_verified": False, "mission_ready": False}
    with output.open("x") as stream:
        json.dump(evidence, stream, indent=2)
        stream.write("\n")
    print(json.dumps(evidence))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    audit(args.directory, args.output)
