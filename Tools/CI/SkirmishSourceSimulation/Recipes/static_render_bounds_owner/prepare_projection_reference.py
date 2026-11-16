#!/usr/bin/env python3
"""Build a duplicate-aware source-side reference from the pinned projection."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

REPO = Path(__file__).resolve().parents[5]
SOURCE_CAPTURE = Path("Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json")
PROJECTION = Path("Logs/SkirmishProduction/s073-clearance-projection-input-20261102-r1/static-clearance-from-capture.json")
REPORT = Path("Logs/SkirmishProduction/s073-static-clearance-bound-multiset-20261103-r2/report.json")
RENDER_OWNER = Path("Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs")
CAPTURE_SOURCE = Path("Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S073StreamedWorld.cs")
REFERENCE_ALGORITHM = Path("Tools/CI/SkirmishSourceSimulation/Tests/compare_s073_static_clearance_multiset.py")
PINS = {
    SOURCE_CAPTURE.as_posix(): "8407889e4aff5c1f731c429fbae0063c5b6d99e1f7f51ab5c5c5bb0be29fa0b8",
    PROJECTION.as_posix(): "8b5087c8a96ad81113c93fb4fba1632f10c8920bd2ed16fcb168e58d22572065",
    REPORT.as_posix(): "c488ea5c8d70217e21833bf493add8380aaf64f1e2e86a5a8134993c31bb7275",
    RENDER_OWNER.as_posix(): "82bf379f5bb3e5ae55705f608c3e4a26ed915afffeec7488c79b9448cecfb5a4",
    CAPTURE_SOURCE.as_posix(): "f278e4383dfb4ae3ad3f00d7b1da4cedce75625cbdceeaff7bdf60d9d9614346",
    REFERENCE_ALGORITHM.as_posix(): "b29bdd5457b6563706f2d288ebbef61c4d2ebdbb2578cc0ad75d74d9bfbbdf41",
}
EXPECTED_AREA = [903.0, 332.0, 197.0, 197.0]
EXPECTED_MIN_EXTENT = 0.25
EXPECTED_MAX_EXTENT = 20.0


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(value: bool, message: str) -> None:
    if not value:
        raise ValueError(message)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--repo", type=Path, default=REPO)
    args = parser.parse_args()
    repo = args.repo.resolve(strict=True)
    for relative, expected in PINS.items():
        need(sha(repo / relative) == expected, "Pinned projection/reference source changed: " + relative)

    physical = json.loads((repo / SOURCE_CAPTURE).read_text(encoding="utf-8"))
    projection = json.loads((repo / PROJECTION).read_text(encoding="utf-8"))
    retained = json.loads((repo / REPORT).read_text(encoding="utf-8"))
    need(physical.get("kind") == "PinnedCampaignPhysicalInputExport" and
         physical.get("result") == "Collected" and physical.get("worldDisposed") is True and
         physical.get("missionInitialized") is False and physical.get("missionReady") is False and
         physical.get("sourceFitCertified") is False and
         physical.get("operationMapId") == "opmap.skirmish.desert_base_01",
         "Pinned pregame source capture envelope changed")
    need(projection.get("kind") == "GeneratedSourceStaticClearanceProjection" and
         projection.get("source_sha256") == PINS[SOURCE_CAPTURE.as_posix()] and
         projection.get("source_path") == str((repo / SOURCE_CAPTURE).resolve()) and
         projection.get("scope") == "CurrentS002SourceOnlyInactiveFixture_NoActiveProviderEquivalence" and
         projection.get("source_fit_certified") is False and projection.get("mission_ready") is False,
         "Pinned source projection envelope changed")
    record = physical.get("fixedMapReadDomain", {}).get("dropClearanceBounds")
    projected_record = projection.get("source_record")
    need(record == projected_record and record.get("authority") ==
         "OriginalRenderBoundsPresentation.CollectStaticBounds_SourceBakedDomain" and
         record.get("grid_width") == 2048 and record.get("grid_height") == 1024 and
         record.get("grid_origin") == [0.0, 0.0, 0.0] and record.get("cell_size") == 1.0 and
         record.get("query_rect") == [-118.0, -118.0, 2284.0, 1260.0] and
         record.get("authored_transform_projection_count") == 124499 and
         len(record.get("bounds", [])) == 90865,
         "Complete source capture/projection bounds record mismatch")
    filter_record = retained.get("filter", {})
    need(retained.get("kind") == "S073StaticClearanceBoundMultisetDiagnostic" and
         filter_record.get("area_rect_xywh") == EXPECTED_AREA and
         filter_record.get("minimum_extent") == EXPECTED_MIN_EXTENT and
         filter_record.get("maximum_extent") == EXPECTED_MAX_EXTENT and
         filter_record.get("input_bounds") == 90865 and filter_record.get("selected_bounds") == 2400,
         "Retained source replay query envelope changed")

    algorithm_path = repo / REFERENCE_ALGORITHM
    spec = importlib.util.spec_from_file_location("s073_pinned_clearance_reference", algorithm_path)
    need(spec is not None and spec.loader is not None, "Pinned independent source reference loader unavailable")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    expected, metrics = module.filter_source_bounds(record["bounds"], EXPECTED_AREA,
        EXPECTED_MIN_EXTENT, EXPECTED_MAX_EXTENT)
    need(metrics["input_bounds"] == 90865 and metrics["selected_bounds"] == 2400,
         "Independent source reference count differs from retained pinned projection")

    rows = [{"key": ",".join(str(part) for part in key), "count": count}
            for key, count in sorted(expected.items())]
    reference = {
        "schema_version": 1,
        "kind": "PinnedPregameSourceProjectionStaticBoundsReference",
        "source_capture_sha256": PINS[SOURCE_CAPTURE.as_posix()],
        "projection_sha256": PINS[PROJECTION.as_posix()],
        "retained_query_report_sha256": PINS[REPORT.as_posix()],
        "collector_source_sha256": PINS[RENDER_OWNER.as_posix()],
        "capture_producer_source_sha256": PINS[CAPTURE_SOURCE.as_posix()],
        "independent_reference_algorithm_sha256": PINS[REFERENCE_ALGORITHM.as_posix()],
        "scope": "Captured pregame eligible static-bounds projection only; not a current S073 mission world",
        "source_projection_bound_count": metrics["input_bounds"],
        "query_area_rect_xywh": metrics["area_rect_xywh"],
        "query_area_rect_max_xy": metrics["area_rect_max_xy"],
        "minimum_extent": metrics["minimum_extent"],
        "maximum_extent": metrics["maximum_extent"],
        "selected_bound_count": sum(expected.values()),
        "unique_bound_count": len(expected),
        "duplicate_excess_count": sum(count - 1 for count in expected.values() if count > 1),
        "bounds_multiset_bits_minmax": rows,
        "source_entity_identities_are_synthetic": True,
        "native_state_imported": False,
        "mission_world_admission": False,
        "readiness_claim": False,
    }
    output = args.output.absolute()
    need(not output.exists(), "Fresh source reference output required")
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x", encoding="utf-8") as stream:
        json.dump(reference, stream, indent=2, sort_keys=True)
        stream.write("\n")
    print("[prepare_projection_reference] status=Passed input=90865 selected=" +
          str(reference["selected_bound_count"]) + " unique=" + str(reference["unique_bound_count"]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
