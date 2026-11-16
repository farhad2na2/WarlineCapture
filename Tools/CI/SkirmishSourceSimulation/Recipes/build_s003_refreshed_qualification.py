#!/usr/bin/env python3
"""Build an evidence-bound S003 source-only descriptor; no runtime admission."""
import argparse
import hashlib
import json
from pathlib import Path

from audit_s003_refreshed_evidence_chain import audit

ROOT = Path(__file__).resolve().parents[4]


def descriptor(path):
    raw = path.read_bytes()
    return {"path": str(path.resolve(strict=True)), "sha256": hashlib.sha256(raw).hexdigest()}


def construct(root, audit_receipt, audit_path=None):
    audit_path = Path(audit_path) if audit_path is not None else root / "T Logs/SkirmishProduction/s003-refreshed-source-evidence-audit-20261104-T4.json"
    if not audit_path.is_absolute() or audit_path.is_symlink() or not audit_path.resolve(strict=True).is_relative_to(root.resolve(strict=True)):
        raise ValueError("Canonical repository-owned audit required")
    if audit_receipt != audit(root, audit_receipt["source_build"]["path"], audit_receipt["compiler"]["path"]):
        raise ValueError("Current S003 evidence must exactly reproduce audited receipt")
    if any(audit_receipt.get(flag) is not False for flag in
           ("runtime_admission", "native_context_qualified", "source_fit_certified", "mission_ready")):
        raise ValueError("Evidence scope promotion rejected")
    projection = root / "T Logs/SkirmishProduction/s003-refreshed-factory-projection-20261104-T1/projection-receipt.json"
    artifacts = json.loads(projection.read_bytes())["artifacts"]
    return {"schema_version": 1, "kind": "S003RefreshedSourceOnlyQualificationEvidence",
            "target": {"catalog_id": "S003", "seed": 104732, "difficulty": "Regular", "size": "Standard",
                       "setup_hash": 2394754492},
            "compiler": audit_receipt["compiler"], "source_build": audit_receipt["source_build"],
            "runtime_graph": audit_receipt["runtime_graph"], "native_capture": audit_receipt["native_capture"],
            "capture_receipt": audit_receipt["capture_receipt"], "projection_receipt": audit_receipt["projection_receipt"],
            "factory_artifacts": artifacts,
            "evidence_audit": descriptor(audit_path),
            "evidence_auditor": descriptor(root / "Tools/CI/SkirmishSourceSimulation/Recipes/audit_s003_refreshed_evidence_chain.py"),
            "descriptor_builder": descriptor(Path(__file__).resolve(strict=True)),
            "runtime_admission": False, "native_context_qualified": False, "source_fit_certified": False,
            "mission_ready": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--audit")
    args = parser.parse_args()
    root = Path(args.repo).resolve(strict=True)
    audit_path = Path(args.audit) if args.audit else root / "T Logs/SkirmishProduction/s003-refreshed-source-evidence-audit-20261104-T4.json"
    result = construct(root, json.loads(audit_path.read_bytes()), audit_path)
    output = Path(args.output).absolute()
    output.mkdir(parents=True, exist_ok=False)
    path = output / "source-only-qualification-evidence.json"
    raw = (json.dumps(result, indent=2) + "\n").encode()
    path.write_bytes(raw)
    receipt = {"status": "PassedDescriptorOnly", "evidence": descriptor(path),
               "runtime_admission": False, "native_context_qualified": False,
               "source_fit_certified": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt))


if __name__ == "__main__":
    main()
