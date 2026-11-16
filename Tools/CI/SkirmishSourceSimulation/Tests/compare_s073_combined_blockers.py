"""Compare committed static domains without treating a mismatch as readiness."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path


def load(path):
    raw = Path(path).read_bytes()
    return json.loads(raw), hashlib.sha256(raw).hexdigest()


def compare(native, receipt, host, host_receipt, identity):
    n, nh = native
    h, hh = host
    if (receipt.get("validation_status") != "Passed" or receipt.get("wrapper_exit_code") != 0
            or receipt.get("required_marker_present") is not True or receipt.get("census_sha256") != nh
            or n.get("status") != "Passed"):
        raise ValueError("Unverified native census")
    if (host_receipt.get("worker_result", {}).get("sha256") != hh
            or host_receipt.get("status") != "PreparingPhysicalSourceRequired"
            or host_receipt.get("process_exit") != 2 or h.get("mission_ready") is not False):
        raise ValueError("Unverified source-host diagnostic")
    if identity.get("status") != "IdentitySetsMatch" or identity.get("native_sha256") != nh:
        raise ValueError("Native source building identities are not bound")
    a = n["post_stream"]["complete_static_blocker_observation"]
    b = h["experimental_physical_blocker_projection"]["combined_after_starting_buildings"]
    for item in (a, b):
        if (item["histories_bound_to_grid"] != item["static_domain_count"]
                or item["source_building_count"] != 4931
                or len(item["other_blockers"]) != item["static_domain_count"] - 4931):
            raise ValueError("Incomplete bound static blocker domain")
    def key(row):
        if row.get("history_bound") is not True:
            raise ValueError("Unbound non-source blocker")
        return (tuple(row["min"]), tuple(row["max"]), row["friendly_pass_faction"])
    left = Counter(key(row) for row in a["other_blockers"])
    right = Counter(key(row) for row in b["other_blockers"])
    def rows(counter):
        return [{"min": list(k[0]), "max": list(k[1]), "friendly_pass_faction": k[2], "count": v}
                for k, v in sorted(counter.items())]
    return {"kind": "S073CombinedStaticBlockerComparison", "status": "Match" if a["blocked_mask_sha256"] == b["blocked_mask_sha256"] and left == right else "Mismatch",
            "native_census_sha256": nh, "source_host_sha256": hh,
            "native_static_domain_count": a["static_domain_count"], "host_static_domain_count": b["static_domain_count"],
            "native_blocked_mask_sha256": a["blocked_mask_sha256"], "host_blocked_mask_sha256": b["blocked_mask_sha256"],
            "matched_non_source_histories": sum((left & right).values()),
            "native_only_histories": rows(left - right), "host_only_histories": rows(right - left),
            "scope": "Separate instrumented native and source-host observations; rectangle multisets do not establish entity identity or timing",
            "source_fit_certified": False, "scheduler_admitted": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser()
    for name in ("native", "native-receipt", "host", "host-receipt", "identity", "output"):
        parser.add_argument("--" + name, required=True)
    args = parser.parse_args()
    native = load(args.native); host = load(args.host)
    receipt, _ = load(args.native_receipt); host_receipt, _ = load(args.host_receipt); identity, _ = load(args.identity)
    result = compare(native, receipt, host, host_receipt, identity)
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({k: v for k, v in result.items() if not k.endswith("histories")}))


if __name__ == "__main__":
    main()
