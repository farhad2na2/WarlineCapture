#!/usr/bin/env python3
"""Join the current audited prefab/factory captures to the retained S004 role census."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

import build_s004_role_catalog_metadata as original

ROOT = original.ROOT
PREFAB = ROOT / "Logs/SkirmishProduction/s003-post-planner-prefabs-20261105-r1/data/prefab-reference-data.json"
PREFAB_AUDIT = ROOT / "T Logs/SkirmishProduction/s003-post-planner-metadata-capture-20261104-T1/prefab-capture-audit-r1.json"
PROJECTION = ROOT / "T Logs/SkirmishProduction/s003-post-planner-factory-projection-20261105-T4/projection-receipt.json"
METADATA = PROJECTION.parent / "unit-production-metadata.json"
BUILD = ROOT / "T Logs/SkirmishProduction/s004-current-epoch-s002-build-20261105-T1/build.json"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def build(source_build=None, prefab=None, prefab_audit_path=None, projection_path=None, metadata=None):
    source_build=Path(BUILD if source_build is None else source_build).resolve(strict=True)
    prefab=Path(PREFAB if prefab is None else prefab).resolve(strict=True)
    prefab_audit_path=Path(PREFAB_AUDIT if prefab_audit_path is None else prefab_audit_path).resolve(strict=True)
    projection_path=Path(PROJECTION if projection_path is None else projection_path).resolve(strict=True)
    metadata=Path(METADATA if metadata is None else metadata).resolve(strict=True)
    evidence_before = {str(path): sha(path) for path in (prefab, prefab_audit_path, metadata, projection_path, source_build)}
    prefab_audit = json.loads(prefab_audit_path.read_bytes())
    projection = json.loads(projection_path.read_bytes())
    if (prefab_audit.get("status") != "PassedDataOnlyCapture" or
        prefab_audit.get("scope") != "CurrentDataOnlyPrefabCapture_IndependentlyAuditedMetadata_NotRuntimeAdmission" or
        prefab_audit.get("wrapper_exit_code") != 0 or prefab_audit.get("required_marker_present") is not True or
        prefab_audit.get("capture") != {"path":str(prefab),"sha256":sha(prefab)} or
        prefab_audit.get("mission_ready") is not False or prefab_audit.get("import_admitted") is not False):
        raise ValueError("Current audited prefab capture required")
    if (projection.get("kind") != "S003IndependentCurrentFactoryMetadataProjection" or
        projection.get("status") != "PassedSourceOnlyProjection" or
        projection.get("artifacts", {}).get("unit-production-metadata.json") != {"path":str(metadata),"sha256":sha(metadata)} or
        projection.get("capture", {}).get("sha256") != prefab_audit.get("metadata_capture_sha256") or
        projection.get("audit", {}).get("sha256") != prefab_audit.get("independent_metadata_audit_sha256") or
        projection.get("runtime_admission") is not False or projection.get("mission_ready") is not False):
        raise ValueError("Current same-epoch factory projection required")
    # The original join rechecks the historical census, every current asset pin,
    # role order, prefab identity and the complete current source-build closure.
    result = original.build(source_build, prefab, metadata)
    if any(sha(path) != expected for path, expected in evidence_before.items()):
        raise ValueError("Current role evidence changed during join")
    result["authority"] = "ObservedHistoricalS004RolesAndCurrentAuditedPrefabMetadata_DiagnosticOnly"
    result["scope"] = "Historical source role census joined to current audited templates and factory metadata; no native S004 parity or runtime admission"
    result["current_capture_evidence"] = {
        "prefab_audit": {"path": str(prefab_audit_path), "sha256": sha(prefab_audit_path)},
        "factory_projection": {"path": str(projection_path), "sha256": sha(projection_path)},
        "builder": {"path": str(Path(__file__).resolve()), "sha256": sha(Path(__file__).resolve())},
    }
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--source-build", type=Path)
    parser.add_argument("--prefab", type=Path)
    parser.add_argument("--prefab-audit", type=Path)
    parser.add_argument("--projection", type=Path)
    parser.add_argument("--metadata", type=Path)
    args = parser.parse_args()
    values=(args.source_build,args.prefab,args.prefab_audit,args.projection,args.metadata)
    if any(value is not None for value in values) and not all(value is not None for value in values):
        raise ValueError("All five explicit current evidence inputs are required together")
    result = build(*values)
    with args.output.open("x") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": "CollectedSourceOnly", "roles": len(result["units"]),
                      "source_pins_checked": result["source_capture_pin_count"], "mission_ready": False}))


if __name__ == "__main__":
    sys.exit(main())
