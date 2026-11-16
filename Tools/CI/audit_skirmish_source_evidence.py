#!/usr/bin/env python3
import hashlib
import json
import pathlib
import sys


def optional_object(parent, name):
    value = parent.get(name)
    if value is None and name not in parent:
        return {}
    if type(value) is not dict:
        raise ValueError(f"{name} must be an object when present")
    return value


def optional_typed(parent, name, expected):
    if name not in parent:
        return None
    value = parent[name]
    if value is not None and type(value) is not expected:
        raise ValueError(f"{name} has an unsupported type")
    return value


def audit(directory):
    d = pathlib.Path(directory)
    receipt = json.loads((d / "run-receipt.json").read_text())
    if type(receipt) is not dict:
        raise ValueError("Receipt must be an object")
    raw = optional_object(receipt, "source_result")
    h = hashlib.sha256()
    count = 0
    first = last_tick = None
    last = None
    monotonic = True
    balance_status_counts = {}
    queue_fault_observations = {}
    queue_flag_observations = {}
    with (d / "validator-state.jsonl").open("rb") as f:
        for line in f:
            h.update(line)
            row = json.loads(line)
            if type(row) is not dict:
                raise ValueError("Validator snapshot must be an object")
            tick = row.get("tick")
            if type(tick) is not int:
                raise ValueError("Validator tick must be an integer")
            if last_tick is not None and tick < last_tick:
                monotonic = False
            if count == 0:
                first = tick
            count += 1
            sample_diagnostic = optional_object(row, "production_diagnostics")
            balance = optional_object(sample_diagnostic, "sampled_material_counter_balance")
            status = optional_typed(balance, "status", str)
            if status == "":
                raise ValueError("Sampled material balance status cannot be empty")
            if status is None:
                status = "Missing"
            balance_status_counts[status] = balance_status_counts.get(status, 0) + 1
            for name, counts in (("queue_diagnostic_faults", queue_fault_observations), ("queue_diagnostic_failure_flags", queue_flag_observations)):
                value = optional_typed(sample_diagnostic, name, int)
                key = "Missing" if value is None else str(value)
                counts[key] = counts.get(key, 0) + 1
            last = row
            last_tick = tick
    if last is None:
        raise ValueError("No validator snapshots")
    diagnostic = optional_object(last, "production_diagnostics")
    terminal = raw.get("status") == "SourceTerminal"
    target = raw.get("tick")
    if terminal and type(target) is not int:
        raise ValueError("Source terminal tick must be an integer")
    return {
        "kind": "SourceRunEvidenceSummary",
        "scope": "Read-only sampled evidence; not same-world pairing, liveness, conservation, native acceptance, or mission readiness proof",
        "receipt_status": optional_typed(receipt, "status", str),
        "source_status": optional_typed(raw, "status", str),
        "source_outcome": optional_typed(raw, "source_outcome", str),
        "validation_passed": optional_typed(receipt, "validation_passed", bool),
        "native_acceptance": optional_typed(receipt, "native_acceptance", bool),
        "validator_file_sha256": h.hexdigest(),
        "snapshot_count": count,
        "first_snapshot_tick": first,
        "last_snapshot_tick": last_tick,
        "snapshot_ticks_monotonic": monotonic,
        "sampled_material_counter_balance_status_counts_reported": balance_status_counts,
        "sampled_queue_fault_counts_reported": queue_fault_observations,
        "sampled_queue_failure_flags_reported": queue_flag_observations,
        "terminal_tick_matches_last_snapshot_tick": terminal and type(target) is int and last_tick == target,
        "queue_diagnostic_faults": optional_typed(diagnostic, "queue_diagnostic_faults", int),
        "queue_diagnostic_failure_flags": optional_typed(diagnostic, "queue_diagnostic_failure_flags", int),
        "fuel_conservation_reported": optional_typed(diagnostic, "fuel_conservation", str),
        "actor_delivery_disposition_reported": optional_typed(diagnostic, "actor_delivery_disposition", str),
        "full_conservation_proved_reported": optional_typed(diagnostic, "full_conservation_proved", bool),
    }


if __name__ == "__main__":
    try:
        print(json.dumps(audit(sys.argv[1]), sort_keys=True))
    except (OSError, ValueError, IndexError, TypeError) as e:
        print("Evidence unavailable: " + str(e), file=sys.stderr)
        sys.exit(1)
