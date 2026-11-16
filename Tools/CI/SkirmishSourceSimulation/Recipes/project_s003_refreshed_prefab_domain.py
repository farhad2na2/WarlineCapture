#!/usr/bin/env python3
"""Project exact refreshed S003 prefab data into the source-only template view."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_prefab_capture import qualify

FIELDS = ["schema_version", "kind", "authority", "mission_id", "source_scenes", "source_pins", "unity_version",
          "template_namespace_id", "requested_prefab_keys", "requested_prefab_keys_sha256",
          "source_key_descriptor_receipt_sha256", "native_metadata_capture_sha256", "native_metadata_capture_receipt_sha256",
          "compiler_seed", "compiler_setup_hash", "opening_prefabs", "entities", "unsupported_values",
          "unsupported_values_preserved_verbatim", "diagnostic_status", "replay_ready", "fresh_world_allocator_exported",
          "full_mission_initialized", "utility_world_created", "utility_world_disposed", "mission_world_created",
          "native_visual_readiness", "source_fit_certified", "native_context_qualified", "mission_ready", "geometry_exported", "consumer_rule"]


def desc(path):
    raw = path.read_bytes()
    return {"path": str(path.resolve(strict=True)), "sha256": hashlib.sha256(raw).hexdigest()}


def write(path, value):
    raw = (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode()
    with path.open("xb") as stream: stream.write(raw)
    return desc(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument("--output", required=True); args = parser.parse_args()
    capture = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
    capture_log = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/editor.log"
    capture_receipt = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/receipt.json"
    control_receipt = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-import-control-20261104-T3/receipt.json"
    control_log = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-import-control-20261104-T3/run.log"
    build = ROOT / "T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json"
    graph = ROOT / "Logs/SkirmishProduction/s003-current-config-graph-20261103-r1/graph-r5/source-config-graph.json"
    if qualify(ROOT, capture, capture_log, 0) != json.loads(capture_receipt.read_bytes()): raise ValueError("Current capture receipt required")
    control = json.loads(control_receipt.read_bytes())
    if (control.get("status") != "PassedDisposableImporterControl" or control.get("capture_sha256") != desc(capture)["sha256"]
        or control.get("capture_receipt_sha256") != desc(capture_receipt)["sha256"] or control.get("build_sha256") != desc(build)["sha256"]
        or control.get("movement_projection_compared") is not True or control.get("world_allocated") is not False
        or control.get("runtime_admission") is not False or control.get("mission_ready") is not False
        or "[S003RefreshedPrefabImportControl] result=Passed roots=19 entities=647 unsupported=32 omitted=26 negatives=3 sourceProjection=Passed world=0 readiness=0" not in control_log.read_text()):
        raise ValueError("Actual source-only importer control required")
    source = json.loads(capture.read_bytes())
    out = Path(args.output).absolute(); out.mkdir(parents=True, exist_ok=False)
    proof = {"schema_version": 1, "kind": "S003RefreshedFocusedTemplateCaptureQualification", "source_graph": desc(graph),
             "capture_receipt": desc(capture_receipt), "importer_control_receipt": desc(control_receipt),
             "importer_build": desc(build), "importer_control_log": desc(control_log)}
    domain = {name: source[name] for name in FIELDS if name in source}
    domain.update(source_reference_sha256=desc(capture)["sha256"], derived_read_domain="ExactTypedPrefabClosureOnly")
    slice_path = out / "s003-focused-template-domain.json"; sliced = write(slice_path, domain)
    manifest = {"schema_version": 1, "kind": "PinnedExactTemplateDomainProjection", "source_path": str(capture),
                "source_sha256": desc(capture)["sha256"], "source_bytes": capture.stat().st_size,
                "slice_path": sliced["path"], "slice_sha256": sliced["sha256"], "slice_bytes": slice_path.stat().st_size,
                "selected_fields": FIELDS, "root_count": 19, "entity_count": 647,
                "refreshed_capture_proof": proof, "unsupported_values_preserved": 32, "world_created": False,
                "consumer_read_domain": "ExactTypedPrefabClosureOnly"}
    manifest_desc = write(out / "template-slice-manifest.json", manifest)
    descriptor = {**sliced, "source_reference_path": str(capture), "source_reference_sha256": desc(capture)["sha256"],
                  "projection_manifest_path": manifest_desc["path"], "projection_manifest_sha256": manifest_desc["sha256"],
                  "refreshed_capture_proof": proof}
    result = write(out / "reference-template-descriptor.json", descriptor)
    write(out / "receipt.json", {"status": "PassedSourceOnlyProjection", "descriptor": result,
                                  "import_admitted": False, "source_fit_certified": False, "mission_ready": False})
    print(json.dumps({"status": "PassedSourceOnlyProjection", "descriptor": result, "mission_ready": False}))


if __name__ == "__main__": main()
