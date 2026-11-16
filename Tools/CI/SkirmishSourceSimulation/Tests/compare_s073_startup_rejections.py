"""Compare explicit native and source-host startup rejections; no readiness claim."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def digest(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser()
    for name in ("native-log", "native-receipt", "host", "host-receipt", "output"):
        parser.add_argument("--" + name, required=True)
    args = parser.parse_args()
    nr = json.loads(Path(args.native_receipt).read_text()); hr = json.loads(Path(args.host_receipt).read_text())
    host = json.loads(Path(args.host).read_text()); log = Path(args.native_log).read_text()
    if (nr.get("validation_status") != "Passed" or nr.get("wrapper_exit_code") != 0
            or nr.get("editor_log_sha256") != digest(args.native_log)
            or hr.get("worker_result", {}).get("sha256") != digest(args.host)
            or hr.get("validation_passed") is not False or hr.get("process_exit") != 2
            or host.get("status") != "PreparingPlacementRejected" or host.get("mission_ready") is not False):
        raise ValueError("Verified native capture and failed source placement receipt required")
    pattern = re.compile(r'^\[InitialSpawn\] initial building request failed\. faction=(\d+) buildingId=(\S+) result=(\S+) origin=\((\d+),(\d+)\)$')
    native = set()
    for line in log.splitlines():
        m = pattern.fullmatch(line)
        if m: native.add((int(m[1]), m[2], m[3], (int(m[4]), int(m[5]))))
    rows = host["building_and_unit_startup_diagnostic"]["requests"]
    failed = {(int(r["faction_id"]), r["building_id"], "Blocked", tuple(r["preferred_origin"]))
              for r in rows if r["status"] == 2 and r["result_code"] == 2}
    if len(rows) != 10 or any((r["status"], r["result_code"]) not in ((1, 0), (2, 2)) for r in rows):
        raise ValueError("Complete terminal startup requests required")
    result = {"kind": "S073StartupRejectionComparison", "status": "RejectionsMatch" if native == failed else "RejectionsDiffer",
              "native_log_sha256": digest(args.native_log), "host_result_sha256": digest(args.host),
              "native_rejections": sorted([list(x[:3]) + [list(x[3])] for x in native]),
              "host_rejections": sorted([list(x[:3]) + [list(x[3])] for x in failed]),
              "source_registered_buildings": host["building_and_unit_startup_diagnostic"]["registered_buildings"],
              "scope": "Instrumented native logs versus source-host results; no normal-input or readiness claim",
              "source_fit_certified": False, "mission_ready": False}
    Path(args.output).write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result))


if __name__ == "__main__": main()
