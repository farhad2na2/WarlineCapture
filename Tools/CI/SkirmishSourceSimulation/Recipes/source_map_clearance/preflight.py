#!/usr/bin/env python3
"""Validate and project generic source-map drop-clearance input for a host build.

This is a source-only package validator. It does not create a world or assert
native presentation activation, occupancy, campaign fit, or mission readiness.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import sys

KIND = "SourceDerivedCampaignMapPackage"
AUTHORITY = "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain"


def fail(message: str) -> None:
    raise ValueError(message)


def read_bytes(path: str | Path) -> bytes:
    try:
        return Path(path).read_bytes()
    except OSError as exc:
        fail(f"cannot read {path}: {exc}")


def sha(path: str | Path) -> str:
    return hashlib.sha256(read_bytes(path)).hexdigest()


def obj(path: str | Path) -> dict:
    try:
        value = json.loads(read_bytes(path))
    except (json.JSONDecodeError, UnicodeDecodeError) as exc:
        fail(f"invalid JSON {path}: {exc}")
    if not isinstance(value, dict):
        fail(f"expected JSON object: {path}")
    return value


def pin(d: object, label: str) -> tuple[str, str]:
    if not isinstance(d, dict) or set(d) != {"path", "sha256"} or not isinstance(d["path"], str):
        fail(f"{label}: exact path/sha256 descriptor required")
    digest = sha(d["path"])
    if d["sha256"] != digest:
        fail(f"{label}: SHA-256 mismatch")
    return d["path"], digest


def finite_vec(value: object, count: int, label: str) -> list[float]:
    if (not isinstance(value, list) or len(value) != count or
            any(type(x) not in (int, float) or not math.isfinite(x) for x in value)):
        fail(f"{label}: expected {count} finite numbers")
    return [float(x) for x in value]


def f32(value: float) -> float:
    """Round at the same points as C# float arithmetic in Reserve."""
    return struct.unpack("<f", struct.pack("<f", float(value)))[0]


def reservation_envelope(frame: dict) -> tuple[float, float, float, float]:
    """Required xMin,zMin,xMax,zMax for all on-grid preferred cells.

    Preserve the original operation order: compute each edge from the
    preferred-cell offset, then add the width of the expanded rectangle.
    """
    cell = f32(frame["cell_size"])
    safe = f32(max(f32(.01), cell))
    origin = [f32(x) for x in frame["origin"]]
    width, height = frame["width"], frame["height"]
    padding = 96 + 2
    span = padding * 2 + 1
    x_min = f32(origin[0] + f32(f32(-padding) * safe))
    z_min = f32(origin[2] + f32(f32(-padding) * safe))
    x_max_local_min = f32(origin[0] + f32(f32((width - 1) - padding) * safe))
    z_max_local_min = f32(origin[2] + f32(f32((height - 1) - padding) * safe))
    x_max = f32(x_max_local_min + f32(f32(span) * safe))
    z_max = f32(z_max_local_min + f32(f32(span) * safe))
    return x_min, z_min, x_max, z_max


def validate(package: dict) -> dict:
    if package.get("kind") != KIND or package.get("schema_version") != 1:
        fail("unsupported package kind/schema")
    for flag in ("mission_initialized", "mission_ready", "source_fit_certified", "live_occupancy_measured"):
        if package.get(flag) is not False:
            fail(f"{flag} must explicitly be false")
    capture_path, capture_sha = pin(package.get("capture"), "capture")
    capture = obj(capture_path)
    if capture.get("result") != "Collected" or capture.get("worldDisposed") is not True:
        fail("disposed collected source capture required")
    if any(capture.get(k) is not False for k in ("missionInitialized", "missionReady", "sourceFitCertified")):
        fail("capture must explicitly remain pregame and unqualified")
    source_root = package.get("source_root")
    if not isinstance(source_root, str) or not Path(source_root).is_dir():
        fail("declared source_root must exist")
    ext = package.get("external_inputs")
    if not isinstance(ext, dict):
        fail("external_inputs must be an object")
    for original, expected in capture.get("readPins", {}).items():
        if not isinstance(original, str) or not isinstance(expected, str) or len(expected) != 64:
            fail("malformed capture source pin")
        captured_root = Path(capture.get("capturedProjectRoot", "")).resolve()
        candidate = Path(original)
        try:
            if candidate.is_absolute():
                candidate = Path(source_root) / candidate.resolve().relative_to(captured_root)
            else:
                candidate = Path(source_root) / candidate
            if candidate.is_file():
                if sha(candidate) != expected:
                    fail(f"source pin mismatch: {original}")
                continue
        except ValueError:
            pass
        if original not in ext:
            if "/Library/ScriptAssemblies/" in original and original.endswith(".dll"):
                continue
            fail(f"source pin unavailable: {original}")
        path, actual = pin(ext[original], f"external input {original}")
        if actual != expected:
            fail(f"external input differs from capture: {original}")

    sm, frame = package.get("static_map"), package.get("frame")
    if not isinstance(sm, dict) or not isinstance(frame, dict):
        fail("static_map/frame missing")
    pin(sm.get("definition"), "static map definition")
    cf = capture["fixedMapReadDomain"]["gridFrame"]
    captured_frame = {"width": cf["width"], "height": cf["height"],
                      "cell_size": cf["cellSize"], "origin": [cf["origin"][k] for k in ("x", "y", "z")]}
    origin = finite_vec(frame.get("origin"), 3, "package frame origin")
    if (frame.get("width"), frame.get("height"), frame.get("cell_size"), origin) != (
            captured_frame["width"], captured_frame["height"], captured_frame["cell_size"],
            [float(x) for x in captured_frame["origin"]]):
        fail("package frame differs from captured grid frame")
    if sm.get("operation_map_id") != capture.get("operationMapId"):
        fail("static map operation id differs from capture")

    record = capture.get("fixedMapReadDomain", {}).get("dropClearanceBounds")
    if not isinstance(record, dict) or record.get("authority") != AUTHORITY:
        fail("original static-bounds collector authority required")
    if record.get("runtime_presentation_activation_sampled") is not False:
        fail("source-only collector activation must be explicitly false")
    width, height, cell = frame["width"], frame["height"], frame["cell_size"]
    if (record.get("grid_width"), record.get("grid_height"), record.get("cell_size")) != (width, height, cell):
        fail("clearance grid frame differs from package frame")
    record_origin = finite_vec(record.get("grid_origin"), 3, "clearance origin")
    if record_origin != origin:
        fail("clearance origin differs from package frame")
    if (record.get("minimum_extent"), record.get("maximum_extent"),
            record.get("source_search_radius_cells"), record.get("source_padding_cells")) != (0.25, 20.0, 96, 2):
        fail("original collector reservation parameters differ")
    rect = finite_vec(record.get("query_rect"), 4, "clearance query rect")
    # Reserve expands every preferred cell by 96+2 cells before querying the
    # original bounds. For any on-grid preferred cell, the complete query
    # rectangle must cover the map plus 98 cells on each side.
    min_x, min_z, extent_x, extent_z = rect
    max_x, max_z = min_x + extent_x, min_z + extent_z
    map_min_x, map_min_z, map_max_x, map_max_z = reservation_envelope(frame)
    # Match the collector query's C# float high-edge addition.
    max_x, max_z = f32(f32(min_x) + f32(extent_x)), f32(f32(min_z) + f32(extent_z))
    min_x, min_z = f32(min_x), f32(min_z)
    if extent_x <= 0 or extent_z <= 0 or min_x > map_min_x or min_z > map_min_z or max_x < map_max_x or max_z < map_max_z:
        fail("clearance query envelope does not cover map plus 98-cell reservation margin")
    bounds = record.get("bounds")
    if not isinstance(bounds, list) or not bounds:
        fail("complete original collector bounds required")
    for i, bound in enumerate(bounds):
        if not isinstance(bound, dict):
            fail(f"clearance bound {i}: object required")
        center = finite_vec(bound.get("center"), 3, f"clearance bound {i} center")
        ext = finite_vec(bound.get("extents"), 3, f"clearance bound {i} extents")
        # Collector rows are AABBs selected by overlap; legitimate edge rows
        # can extend beyond the query rectangle. Require finite rows and an
        # actual intersection, matching the original collector's semantics.
        if (min(ext) < 0 or center[0] + ext[0] < min_x or center[0] - ext[0] >= max_x or
                center[2] + ext[2] < min_z or center[2] - ext[2] >= max_z):
            fail(f"clearance bound {i} does not intersect the complete query envelope")

    water_path, water_sha = pin(package.get("water_descriptor"), "water descriptor")
    return {
        "schema_version": 1,
        "kind": "SourceDerivedCampaignMapDropClearanceProjection",
        "package_capture": {"path": str(Path(capture_path).resolve()), "sha256": capture_sha},
        "static_map": {"operation_map_id": sm["operation_map_id"], "definition": sm["definition"]},
        "frame": {"width": width, "height": height, "cell_size": cell, "origin": origin},
        "collector": {"authority": AUTHORITY, "grid_width": width, "grid_height": height,
                      "cell_size": cell, "grid_origin": record_origin, "query_rect": rect,
                      "record_sha256": hashlib.sha256(json.dumps(record, sort_keys=True, separators=(",", ":"), allow_nan=False).encode()).hexdigest()},
        "water_descriptor": {"path": str(Path(water_path).resolve()), "sha256": water_sha},
        "scope": "Source-only original captured static-bounds rows; no live occupancy or presentation activation",
        "live_occupancy_qualified": False, "native_presentation_qualified": False,
        "source_fit_certified": False, "mission_ready": False,
    }


def controls(projection: dict) -> dict:
    """Focused fail-closed controls for frame and incomplete envelope mutation."""
    accepted = validate_projection_record(projection)
    wrong_frame = json.loads(json.dumps(projection))
    wrong_frame["frame"]["width"] += 1
    frame_rejected = _rejects(lambda: validate_projection_record(wrong_frame))
    insufficient = json.loads(json.dumps(projection))
    # This mutant covers the map exactly, but omits the original owner's
    # 98-cell reservation margin on all sides.
    insufficient["collector"]["query_rect"] = [
        insufficient["frame"]["origin"][0],
        insufficient["frame"]["origin"][2],
        insufficient["frame"]["width"] * insufficient["frame"]["cell_size"],
        insufficient["frame"]["height"] * insufficient["frame"]["cell_size"],
    ]
    envelope_rejected = _rejects(lambda: validate_projection_record(insufficient))
    small_safe = small_cell_projection(projection, safe_margin=True)
    small_safe_accepted = not _rejects(lambda: validate_projection_record(small_safe))
    small_raw = small_cell_projection(projection, safe_margin=False)
    small_raw_rejected = _rejects(lambda: validate_projection_record(small_raw))
    if not accepted or not frame_rejected or not envelope_rejected or not small_safe_accepted or not small_raw_rejected:
        fail("focused clearance controls failed")
    return {"wrong_frame_rejected": frame_rejected, "insufficient_envelope_rejected": envelope_rejected,
            "small_cell_safe_margin_accepted": small_safe_accepted,
            "small_cell_raw_margin_rejected": small_raw_rejected}


def small_cell_projection(projection: dict, safe_margin: bool) -> dict:
    p = json.loads(json.dumps(projection))
    frame, collector = p["frame"], p["collector"]
    frame.update({"width": 64, "height": 32, "cell_size": .005, "origin": [0, 0, 0]})
    collector.update({"grid_width": 64, "grid_height": 32, "cell_size": .005, "grid_origin": [0, 0, 0]})
    # Safe-envelope path models Reserve's safe=max(.01f, cellSize). The raw
    # path deliberately uses cellSize itself and must be rejected.
    cell = .01 if safe_margin else .005
    min_edge = f32(f32(-98) * f32(cell))
    xmax_low = f32(f32(64 - 1 - 98) * f32(cell))
    xmax = f32(xmax_low + f32(197 * f32(cell)))
    zmax_low = f32(f32(32 - 1 - 98) * f32(cell))
    zmax = f32(zmax_low + f32(197 * f32(cell)))
    collector["query_rect"] = [min_edge, min_edge, f32(xmax - min_edge), f32(zmax - min_edge)]
    return p


def validate_projection_record(p: dict) -> bool:
    f, c = p["frame"], p["collector"]
    if (c["grid_width"], c["grid_height"], c["cell_size"], c["grid_origin"]) != (
            f["width"], f["height"], f["cell_size"], f["origin"]):
        fail("projection frame differs from captured collector frame")
    r = finite_vec(c["query_rect"], 4, "clearance query rect")
    required_min_x, required_min_z, required_max_x, required_max_z = reservation_envelope(f)
    query_min_x, query_min_z = f32(r[0]), f32(r[1])
    query_max_x, query_max_z = f32(query_min_x + f32(r[2])), f32(query_min_z + f32(r[3]))
    if (query_min_x > required_min_x or query_min_z > required_min_z or
            query_max_x < required_max_x or query_max_z < required_max_z):
        fail("clearance query envelope omits the 98-cell reservation margin")
    return True


def _rejects(fn) -> bool:
    try:
        fn()
    except (ValueError, KeyError, TypeError):
        return True
    return False


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--package", required=True)
    ap.add_argument("--output", required=True)
    ap.add_argument("--controls", action="store_true")
    args = ap.parse_args(argv)
    try:
        projection = validate(obj(args.package))
        report = {"status": "SourceOnlyClearancePreflightPassed", "projection": projection}
        if args.controls:
            report["controls"] = controls(projection)
        out = Path(args.output)
        if out.exists():
            fail("fresh output path required")
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text(json.dumps(report, indent=2, sort_keys=True, allow_nan=False) + "\n", encoding="utf-8")
        print(f"[SourceMapClearance] result=Passed bounds=capture-pinned wrong_frame_rejected={report.get('controls', {}).get('wrong_frame_rejected', False)} insufficient_envelope_rejected={report.get('controls', {}).get('insufficient_envelope_rejected', False)} small_cell_safe_accepted={report.get('controls', {}).get('small_cell_safe_margin_accepted', False)} small_cell_raw_rejected={report.get('controls', {}).get('small_cell_raw_margin_rejected', False)} world=0 mission_ready=0")
        return 0
    except (ValueError, KeyError, TypeError) as exc:
        print(json.dumps({"status": "Rejected", "error": str(exc)}, sort_keys=True), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
