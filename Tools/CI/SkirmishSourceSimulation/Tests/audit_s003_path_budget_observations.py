#!/usr/bin/env python3
"""Audit actual bounded source path-budget observations without inferring native behavior."""
import argparse
import hashlib
import json
import re
from pathlib import Path

PREFIX = "[S003PathBudgetObservation] "
KERNEL = Path("Assets/Game/Scripts/Systems/Pathfinding/PathRequestBudgetKernel.cs")


def sha(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def need(value, message):
    if not value:
        raise ValueError(message)


def main():
    parser = argparse.ArgumentParser(); parser.add_argument("off", type=Path); parser.add_argument("on", type=Path); parser.add_argument("output", type=Path)
    args = parser.parse_args(); dirs = [args.off.resolve(strict=True), args.on.resolve(strict=True)]
    receipts = [json.loads((d / "run-receipt.json").read_bytes()) for d in dirs]
    need(all(r.get("status") == "BoundedPrefix" and r.get("process_exit") == 0 and
             r.get("validation_passed") is False and r.get("mission_ready") is False for r in receipts), "Completed diagnostic prefixes required")
    need(receipts[0]["build_receipt"]["sha256"] == receipts[1]["build_receipt"]["sha256"] and
         receipts[0]["data_bundle"]["sha256"] == receipts[1]["data_bundle"]["sha256"], "Same build and source inputs required")
    build_path = Path(receipts[0]["build_receipt"]["path"])
    need(sha(build_path) == receipts[0]["build_receipt"]["sha256"], "Build receipt changed")
    build = json.loads(build_path.read_bytes())
    logs = [(d / "process.log").read_text() for d in dirs]
    marker = "[SourceWalkabilitySnapshotCache] enabled=1 scope=ExperimentalSourceOnly"
    need([log.count(marker) for log in logs] == [0, 1], "Cache lane markers differ")
    rows = []
    for log in logs:
        entries = [line[len(PREFIX):] for line in log.splitlines() if line.startswith(PREFIX)]
        parsed = [dict(part.split("=", 1) for part in line.split()) for line in entries]
        need(parsed and len({r["frame"] for r in parsed}) == len(parsed), "Unique bounded budget observations required")
        rows.append({int(r["frame"]): r for r in parsed})
    common = sorted(rows[0].keys() & rows[1].keys())
    keys = ("pendingFrames", "requests", "manual", "vehicle", "scheduledBudget", "before", "beforeStable", "after", "afterStable")
    different = [frame for frame in common if any(rows[0][frame][key] != rows[1][frame][key] for key in keys)]
    need(different, "No observed budget-state difference")
    first = different[0]; off, on = rows[0][first], rows[1][first]
    kernel = KERNEL.read_bytes(); source = kernel.decode("utf-8")
    need(build["source_and_reference_pins"].get(str(KERNEL.resolve())) == hashlib.sha256(kernel).hexdigest(),
         "Current kernel differs from the executed build")
    match = re.search(r"private const double FreezeLogThresholdSeconds = ([0-9.]+)d;", source)
    need(match is not None and "if (pendingFrames > 1 || pendingWallTime >= FreezeLogThresholdSeconds)" in source,
         "Original budget wall-time branch changed")
    result = {"kind": "S003PathBudgetObservationAudit", "status": "ObservedBudgetStateDifference",
              "first_differing_common_frame": first, "off": off, "on": on,
              "original_freeze_threshold_seconds": float(match.group(1)),
              "observed_rows": [len(x) for x in rows], "common_frames": len(common),
              "source_kernel_sha256": hashlib.sha256(kernel).hexdigest(),
              "receipt_sha256": [sha(d / "run-receipt.json") for d in dirs],
              "process_log_sha256": [sha(d / "process.log") for d in dirs],
              "log_hashes_bound_by_original_receipts": False,
              "scope": "Source-only diagnostics; passive logging can affect wall timing",
              "later_movement_causality_proven": False, "native_parity_proven": False,
              "optimization_promoted": False, "mission_ready": False}
    with args.output.open("x") as out:
        json.dump(result, out, indent=2); out.write("\n")
    print(json.dumps(result))


if __name__ == "__main__":
    main()
