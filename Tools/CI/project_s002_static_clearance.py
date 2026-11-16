#!/usr/bin/env python3
"""Project exact S002 static-clearance facts from the retained physical capture."""
import argparse
import hashlib
import json
from pathlib import Path

CAPTURE_SHA256 = "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8"
MAP_SOURCE_SHA256 = "8a03d36fd1d4d59a180c65d82f1c708fd70e7c291f49d480c80629a80ec728c0"
MAP_ID = "opmap.skirmish.desert_base_01"
SCOPE = "CurrentS002SourceOnlyInactiveFixture_NoActiveProviderEquivalence"


def no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON field: {key}")
        result[key] = value
    return result


def load_capture(path):
    path = Path(path).resolve(strict=True)
    if not path.is_file() or path.is_symlink():
        raise ValueError("regular S002 physical capture file required")
    raw = path.read_bytes()
    if hashlib.sha256(raw).hexdigest() != CAPTURE_SHA256:
        raise ValueError("exact retained S002 physical capture required")
    capture = json.loads(raw, object_pairs_hook=no_duplicates)
    if (capture.get("result") != "Collected" or capture.get("kind") != "PinnedCampaignPhysicalInputExport"
            or capture.get("worldDisposed") is not True or capture.get("missionInitialized") is not False
            or capture.get("sourceFitCertified") is not False or capture.get("missionReady") is not False
            or capture.get("operationMapId") != MAP_ID or capture.get("sourceSha256") != MAP_SOURCE_SHA256):
        raise ValueError("captured S002 source scope/identity differs")
    record = capture.get("fixedMapReadDomain", {}).get("dropClearanceBounds")
    if not isinstance(record, dict) or record.get("runtime_presentation_activation_sampled") is not False:
        raise ValueError("original inactive-fixture drop-clearance record required")
    if (record.get("authority") != "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain"
            or record.get("grid_width") != 2048 or record.get("grid_height") != 1024
            or record.get("grid_origin") != [0.0, 0.0, 0.0] or record.get("cell_size") != 1.0
            or record.get("minimum_extent") != 0.25 or record.get("maximum_extent") != 20.0
            or record.get("source_search_radius_cells") != 96 or record.get("source_padding_cells") != 2
            or len(record.get("bounds", [])) != 90865):
        raise ValueError("complete original S002 static-clearance record required")
    return path, raw, capture, record


def make_projection(path):
    capture_path, _, _, record = load_capture(path)
    projection = {
        "schema_version": 1,
        "kind": "GeneratedSourceStaticClearanceProjection",
        "source_sha256": CAPTURE_SHA256,
        "source_path": str(capture_path),
        "scope": SCOPE,
        "source_fit_certified": False,
        "mission_ready": False,
        "source_record": record,
    }
    validate_projection(projection, capture_path)
    return projection


def validate_projection(projection, capture_path):
    path, _, _, record = load_capture(capture_path)
    expected_keys = {"schema_version", "kind", "source_sha256", "source_path", "scope",
                     "source_fit_certified", "mission_ready", "source_record"}
    if not isinstance(projection, dict) or set(projection) != expected_keys:
        raise ValueError("exact static-clearance projection schema required")
    if (projection["schema_version"] != 1 or projection["kind"] != "GeneratedSourceStaticClearanceProjection"
            or projection["source_sha256"] != CAPTURE_SHA256 or projection["source_path"] != str(path)
            or projection["scope"] != SCOPE or projection["source_fit_certified"] is not False
            or projection["mission_ready"] is not False or projection["source_record"] != record):
        raise ValueError("projection must preserve exact inactive S002 source facts")


def write_projection(capture_path, output_path):
    projection = make_projection(capture_path)
    output = Path(output_path).resolve()
    raw = (json.dumps(projection, separators=(",", ":"), ensure_ascii=False, allow_nan=False) + "\n").encode()
    if len(raw) > 8 * 1024 * 1024:
        raise ValueError("bounded static-clearance projection required")
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("xb") as stream:
        stream.write(raw)
    validate_projection(json.loads(raw, object_pairs_hook=no_duplicates), capture_path)
    return hashlib.sha256(raw).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capture", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    print(json.dumps({"output": str(Path(args.output).resolve()),
                      "sha256": write_projection(args.capture, args.output),
                      "source_fit_certified": False, "mission_ready": False}, separators=(",", ":")))


if __name__ == "__main__":
    main()
