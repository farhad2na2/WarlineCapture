"""Validate a completed checked-wrapper capture before producing a road-mask receipt."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def sha(data):
    return hashlib.sha256(data).hexdigest()


def validate(log, census, surface, sidewalk, wrapper_exit):
    if wrapper_exit != 0:
        raise ValueError("Unity wrapper did not complete successfully")
    marker = "[S073PostStreamCensus] result=Passed ecsLoaded=1 ownedGrid=1 entities=4931"
    if marker not in log or "[S073StartupRoadMasks] result=Captured cells=2097152" not in log:
        raise ValueError("Required native capture markers missing")
    post = census.get("post_stream") or {}
    if (census.get("status") != "Passed" or census.get("mission_ready") is not False
            or post.get("source_identity_hash") != "2a2a791e8292a4f458bd603ceb86598aa0ba2ca82db41323bf5bf7c748ec6900"
            or post.get("source_content_hash") != "2713962f0faa2dae49805e1b7e3a1673199a2cca915334d11421b354cd8f591c"
            or post.get("ecs_scene_loaded") is not True or post.get("authored_grid_section_owned") is not True
            or post.get("physical_source_validated") != 1 or post.get("entity_count") != 4931):
        raise ValueError("Exact loaded S073 native source identity required")
    if len(surface) != 2048 * 1024 or len(sidewalk) != len(surface) or max(surface) > 1 or max(sidewalk) > 1:
        raise ValueError("Complete row-major bool masks required")
    pattern = re.compile(r'^\[S073StartupRoadTrace\] building=(\S+) origin=int2\((\d+), (\d+)\) rect=\(x:(\d+), y:(\d+), width:(\d+), height:(\d+)\) cached=(\S*) directSurface=(\S*) sidewalk=(\S*)$')
    lines = [pattern.fullmatch(line) for line in log.splitlines() if line.startswith('[S073StartupRoadTrace]')]
    if len(lines) != 2 or any(row is None for row in lines): raise ValueError("Two complete native road trace rows required")
    expected = {(950, 438): (28, 15), (887, 444): (19, 16)}
    for line in lines:
        _, x, y, rx, ry, w, h, _, actual_surface, actual_sidewalk = line.groups()
        x, y, rx, ry, w, h = map(int, (x, y, rx, ry, w, h))
        if (x, y) not in expected or (rx, ry) != (x, y) or expected.pop((x, y)) != (w, h):
            raise ValueError("Unexpected trace rectangle")
        def trace_cells(raw):
            return {tuple(map(int, item.split(":"))) for item in raw.split(",") if item}
        for mask, trace in ((surface, actual_surface), (sidewalk, actual_sidewalk)):
            actual = {(cx, cy) for cy in range(y, y + h) for cx in range(x, x + w) if mask[cy * 2048 + cx]}
            if actual != trace_cells(trace): raise ValueError("Whole mask differs from exact placement trace")
    if expected: raise ValueError("Missing target trace")
    return post


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--directory", required=True); parser.add_argument("--repo", required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    args = parser.parse_args(); directory = Path(args.directory); repo = Path(args.repo).resolve()
    log_raw = (directory / "editor.log").read_bytes(); census_raw = (directory / "census.json").read_bytes()
    census = json.loads(census_raw); surface = (directory / "road.surface.bin").read_bytes(); sidewalk = (directory / "road.sidewalk.bin").read_bytes()
    post = validate(log_raw.decode(), census, surface, sidewalk, args.wrapper_exit)
    pins = census.get("source_pins")
    if not isinstance(pins, dict) or len(pins) < 5: raise ValueError("Census source pins required")
    if post.get("source_scene_path") not in pins:
        raise ValueError("The native runtime scene supplying the road overlays was not pinned during capture")
    pins = dict(pins)
    trace = "Assets/Game/Scripts/Systems/BuildingRuntimeProcessingCompositionSystemHelper.cs"
    pins[trace] = sha((repo / trace).read_bytes())
    for path, digest in pins.items():
        resolved = (repo / path).resolve()
        if not resolved.is_relative_to(repo) or sha(resolved.read_bytes()) != digest:
            raise ValueError("Native capture source pin changed: " + path)
    result = {"kind": "S073PhysicalRoadMaskCaptureReceipt", "validation_status": "Passed", "wrapper_exit_code": 0,
              "required_markers_present": True, "source_identity_hash": post["source_identity_hash"],
              "source_content_hash": post["source_content_hash"], "surface_mask_sha256": sha(surface),
              "sidewalk_mask_sha256": sha(sidewalk), "editor_log_sha256": sha(log_raw), "census_sha256": sha(census_raw),
              "source_pins": pins, "campaign_gameplay_state_copied": False, "mission_ready": False,
              "scope": "Instrumented physical placement masks at one native startup epoch; not complete surface geometry or readiness"}
    path = directory / "road-receipt.json"
    with path.open("x") as f: json.dump(result, f, indent=2); f.write("\n")
    print(json.dumps({k: result[k] for k in ("kind", "validation_status", "surface_mask_sha256", "sidewalk_mask_sha256")}))


if __name__ == "__main__": main()
