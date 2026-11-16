#!/usr/bin/env python3
"""Project an independently audited current capture without native admission."""
import argparse
import hashlib
import json
from pathlib import Path

from project_s003_current_factory_metadata import _project_payload_checked, read_factory_sources


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write(path, value):
    raw = (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode()
    with path.open("xb") as stream:
        stream.write(raw)
    return {"path": str(path.resolve()), "sha256": hashlib.sha256(raw).hexdigest()}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ("repo", "capture", "audit", "log", "output"):
        parser.add_argument("--" + name, type=Path, required=True)
    args = parser.parse_args()
    root = args.repo.resolve(strict=True)
    capture, audit, log = (p.resolve(strict=True) for p in (args.capture, args.audit, args.log))
    audit_hash = sha(audit)
    evidence = json.loads(audit.read_bytes())
    if (evidence.get("status") != "Passed" or
        evidence.get("scope") != "CurrentDataOnlyCapture_IndependentAudit_NotHistoricalFinalizerQualification" or
        evidence.get("capture_sha256") != sha(capture) or evidence.get("log_sha256") != sha(log) or
        evidence.get("wrapper_exit_observed") != 0 or evidence.get("marker_count") != 1 or
        evidence.get("source_pin_mismatches") != [] or evidence.get("mission_ready") is not False or
        evidence.get("native_context_qualified") is not False):
        raise ValueError("Matching independently audited data-only capture required")
    marker = ("[SkirmishS003MetadataCapture] result=Passed catalog=S003 seed=104732 "
              "scope=DataOnly native_context_qualified=false mission_ready=false")
    log_text = log.read_text(encoding="utf-8", errors="strict")
    if (sum(line.endswith(marker) for line in log_text.splitlines()) != 1 or
        "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS003MetadataCapture" not in log_text):
        raise ValueError("Original capture method and unique pass marker required")
    value = json.loads(capture.read_bytes())
    if value.get("kind") != "NativeS003OriginalCompilerFactoryMetadataCapture":
        raise ValueError("Original S003 factory capture required")
    pins = value.get("source_pins")
    if not isinstance(pins, dict) or not pins:
        raise ValueError("Capture source closure required")
    def check_pins():
        for name, expected in pins.items():
            path = Path(name)
            path = path if path.is_absolute() else root / path
            if not path.is_file() or path.is_symlink() or sha(path) != expected:
                raise ValueError("Capture source changed: " + name)
    check_pins()
    capture_hash = sha(capture)
    payload = _project_payload_checked(value, capture_hash, pins, read_factory_sources(root))
    output = args.output.absolute()
    output.mkdir(parents=True, exist_ok=False)
    definitions = write(output / "building-definitions.json", payload["building-definitions.json"])
    payload["configured-metadata.json"]["reference"] = definitions
    artifacts = {"building-definitions.json": definitions}
    for name in ("unit-production-metadata.json", "configured-metadata.json"):
        artifacts[name] = write(output / name, payload[name])
    check_pins()
    if sha(capture) != capture_hash or sha(log) != evidence["log_sha256"] or sha(audit) != audit_hash:
        raise ValueError("Capture evidence changed during projection")
    receipt = {"kind": "S003IndependentCurrentFactoryMetadataProjection", "status": "PassedSourceOnlyProjection",
               "scope": "IndependentCaptureAudit_NotHistoricalFinalizerQualification",
               "capture": {"path": str(capture), "sha256": capture_hash},
               "audit": {"path": str(audit), "sha256": audit_hash}, "artifacts": artifacts,
               "counts": {"buildings": len(payload["building-definitions.json"]["definitions"]),
                          "units": len(payload["unit-production-metadata.json"]["units"]),
                          "transports": len(payload["unit-production-metadata.json"]["transports"])},
               "native_context_qualified": False, "source_fit_certified": False,
               "runtime_admission": False, "mission_ready": False}
    write(output / "projection-receipt.json", receipt)
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
