"""Conservative cross-check; this does not certify placement or source fit."""
import argparse
import hashlib
import json
from pathlib import Path


def read(path):
    data = Path(path).read_bytes()
    return json.loads(data), hashlib.sha256(data).hexdigest()


def audit(startup, startup_receipt, history, history_sha, comparison):
    if (startup_receipt.get("worker_result", {}).get("sha256") != startup[1]
            or startup_receipt.get("status") != "PreparingPhysicalSourceRequired"
            or startup_receipt.get("process_exit") != 2):
        raise ValueError("Unverified or unexpected startup diagnostic")
    doc = startup[0]
    if (doc.get("mission") != "S073" or doc.get("mission_ready") is not False
            or doc.get("scheduler_tick_executed") is not False):
        raise ValueError("Unexpected startup scope")
    if (comparison.get("status") != "HistoriesMatch"
            or comparison.get("isolated_sha256") != history_sha
            or comparison.get("building_count") != 4931
            or comparison.get("full_grid_parity") is not False):
        raise ValueError("Native comparison does not bind this isolated history")
    roots = history.get("normalized_buildings", [])
    requests = doc.get("building_and_unit_startup_diagnostic", {}).get("requests", [])
    if len(roots) != 4931 or len(requests) != 10:
        raise ValueError("Expected all source buildings and startup requests")
    counts = []
    for request in requests:
        origin, size = request["actual_origin"], request["actual_footprint"]
        if request["status"] != 1 or len(origin) != 2 or len(size) != 2 or min(size) <= 0:
            raise ValueError("Unexpected startup footprint")
        overlap = 0
        for root in roots:
            h = root["static_history"]
            if h.get("bound_to_grid_epoch") is not True:
                raise ValueError("Unbound blocker history")
            # Extend the upper edge by one cell, conservatively encompassing
            # both inclusive and exclusive rectangle conventions.
            if all(origin[i] <= h["max"][i] and origin[i] + size[i] >= h["min"][i] for i in (0, 1)):
                overlap += 1
        counts.append({"request_id": request["request_id"], "potential_source_bounds_overlaps": overlap})
    return {"kind": "S073StartupSceneryBoundsAudit", "startup_sha256": startup[1],
            "isolated_sha256": history_sha, "native_sha256": comparison["native_sha256"],
            "requests": counts, "scope": "Conservative rectangle intersection across separate diagnostic runs; no openings, other entities, terrain, routes, or timing evaluated",
            "source_fit_certified": False, "scheduler_admitted": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser()
    for flag in ("startup", "startup-receipt", "history", "comparison", "output"):
        parser.add_argument("--" + flag, required=True)
    args = parser.parse_args()
    startup = read(args.startup)
    receipt, _ = read(args.startup_receipt)
    history, history_sha = read(args.history)
    comparison, comparison_sha = read(args.comparison)
    result = audit(startup, receipt, history, history_sha, comparison)
    result["history_comparison_sha256"] = comparison_sha
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result))


if __name__ == "__main__":
    main()
