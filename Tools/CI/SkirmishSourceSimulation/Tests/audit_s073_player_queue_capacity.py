#!/usr/bin/env python3
"""Verify the observed S073 source producer-capacity and resource boundary."""
import argparse
import hashlib
import json
from pathlib import Path


def read(path):
    raw = path.read_bytes()
    return json.loads(raw), hashlib.sha256(raw).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def audit(directory, output):
    receipt, receipt_sha = read(directory / "run-receipt.json")
    result, result_sha = read(directory / "result.json")
    require(receipt.get("mission") == "S073" and receipt.get("seed") == 104729 and
            receipt.get("process_exit") == 2 and receipt.get("timed_out") is False and
            receipt.get("validation_passed") is False and receipt.get("mission_ready") is False and
            receipt.get("worker_result", {}).get("sha256") == result_sha,
            "Checked diagnostic run and matching result required")
    require(result.get("native_player_input_observed") is False and result.get("mission_ready") is False,
            "Source-only non-native result required")
    trace = result["source_host_aria_policy_step"]["synthetic_player_recruit_diagnostic"]
    require(trace.get("scope") == "FiveExplicitLogicalSourceCommands_NotNativeInput" and
            trace.get("native_input_observed") is False and trace.get("mission_ready") is False,
            "Explicit five-command diagnostic scope required")
    requests = trace["requests"]
    require(len(requests) == 5 and requests[0].get("accepted") is True and
            requests[0].get("result") == "None" and requests[0].get("pending_recorded") is True and
            requests[0].get("materials_before") == 220 and requests[0].get("materials_after") == 200,
            "One observed paid accepted request required")
    require(all(r.get("accepted") is False and r.get("result") == "ProducerQueueFull" and
                r.get("required_building") == "Barracks" and r.get("pending_recorded") is False and
                r.get("materials_before") == r.get("materials_after") == 200 and
                r.get("credits_before") == r.get("credits_after") == 0 and
                r.get("native_input_observed") is False for r in requests[1:]),
            "Four actual producer-full rejections without extra resource mutation required")
    events = result["source_host_original_tactical_owner_scheduler_tick"]["original_production_runtime"]["source_delivery_events"]
    queue = [e for e in events if e.get("kind") == "queue_accepted" and e.get("producer_id") == 1]
    require(len(queue) == 1, "Exactly one player-producer queue event required")
    evidence = {"kind": "S073SyntheticPlayerQueueCapacityAudit", "status": "Passed",
                "run_receipt_sha256": receipt_sha, "worker_result_sha256": result_sha,
                "accepted": 1, "producer_full_rejected": 4, "materials_spent": 20,
                "native_input_observed": False, "native_queue_parity_proven": False, "mission_ready": False}
    require(output.parent.is_dir() and not output.exists(), "Fresh output required")
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
