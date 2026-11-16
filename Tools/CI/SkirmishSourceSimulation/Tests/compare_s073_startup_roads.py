"""Compare exact placement-time native road cells with source-host inputs."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def load(path):
    data = Path(path).read_bytes()
    return json.loads(data), hashlib.sha256(data).hexdigest()


def cells(text):
    return {tuple(map(int, cell.split(":"))) for cell in text.split(",") if cell}


def compare(log, log_hash, receipt, host, host_hash, host_receipt):
    if (receipt.get("validation_status") != "Passed" or receipt.get("wrapper_exit_code") != 0
            or receipt.get("required_marker_present") is not True or receipt.get("editor_log_sha256") != log_hash):
        raise ValueError("Unverified native road trace")
    if (host_receipt.get("worker_result", {}).get("sha256") != host_hash
            or host_receipt.get("status") != "PreparingPhysicalSourceRequired" or host_receipt.get("process_exit") != 2):
        raise ValueError("Unverified host result")
    pattern = re.compile(r'^\[S073StartupRoadTrace\] building=(\S+) origin=int2\((\d+), (\d+)\) rect=\(x:(\d+), y:(\d+), width:(\d+), height:(\d+)\) cached=(\S*) directSurface=(\S*) sidewalk=(\S*)$')
    rows = [pattern.fullmatch(line) for line in log.splitlines() if line.startswith('[S073StartupRoadTrace]')]
    if len(rows) != 2 or any(row is None for row in rows):
        raise ValueError("Exactly two complete native trace lines required")
    samples = host["experimental_physical_blocker_projection"]["rejected_native_origin_road_inputs"]
    by_origin = {tuple(row["origin"]): row for row in samples}
    if len(by_origin) != 2: raise ValueError("Exact host samples required")
    results = []
    for row in rows:
        name, x, y, rx, ry, w, h, cached, surface, sidewalk = row.groups()
        origin = (int(x), int(y)); footprint = [int(w), int(h)]
        if (int(rx), int(ry)) != origin or origin not in by_origin or by_origin[origin]["footprint"] != footprint:
            raise ValueError("Trace rectangle differs from host")
        sample = by_origin[origin]
        native_surface, native_sidewalk = cells(surface), cells(sidewalk)
        host_surface = {tuple(v) for v in sample["surface_road_cells"]}
        host_sidewalk = {tuple(v) for v in sample["sidewalk_cells"]}
        host_captured = {tuple(v) for v in sample["captured_road_cells"]}
        results.append({"building": name, "origin": list(origin), "footprint": footprint,
            "native_cached_cells": len(cells(cached)), "native_surface_cells": len(native_surface),
            "native_sidewalk_cells": len(native_sidewalk), "host_captured_road_cells": len(host_captured),
            "host_surface_cells": len(host_surface), "host_sidewalk_cells": len(host_sidewalk),
            "native_surface_missing_from_host": [list(v) for v in sorted(native_surface - host_surface)],
            "native_sidewalk_missing_from_host": [list(v) for v in sorted(native_sidewalk - host_sidewalk)]})
    return {"kind": "S073StartupRoadInputComparison", "native_log_sha256": log_hash, "host_result_sha256": host_hash,
            "samples": results, "scope": "Two exact early placement rectangles in separate instrumented runs; does not establish why the source inputs differ",
            "source_fit_certified": False, "mission_ready": False}


def main():
    p = argparse.ArgumentParser()
    for name in ("native-log", "native-receipt", "host", "host-receipt", "output"): p.add_argument("--" + name, required=True)
    a = p.parse_args(); raw = Path(a.native_log).read_bytes(); host, hh = load(a.host)
    receipt, _ = load(a.native_receipt); hr, _ = load(a.host_receipt)
    result = compare(raw.decode(), hashlib.sha256(raw).hexdigest(), receipt, host, hh, hr)
    Path(a.output).write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps([{k: v for k, v in r.items() if not isinstance(v, list)} for r in result["samples"]]))


if __name__ == "__main__": main()
