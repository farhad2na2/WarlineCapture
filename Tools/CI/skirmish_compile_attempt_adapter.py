"""Immutable per-attempt compile facet receipts; no qualification or queue mutation.

The schema3 native backend remains unchanged and specialized. This adapter never
converts a clean projection, a current pending closure, or asserted flags into proof.
"""
from pathlib import Path
import hashlib
import json
import os
import re

import skirmish_native_compile_admission as backend
from skirmish_production_operational_inspector import Reader, need, inspect as inspect_operational, _io_path
from skirmish_production_queue import fingerprint

BACKEND_SHA256 = "f27135fb3c7934c3fc151f19c82f5537188043271d48098c1872f54860c5070a"
FLAGS = {"qualified": False, "queue_success_enabled": False,
         "mission_ready": False, "publication_changed": False,
         "sampling_coverage_credit": 0}
ERRORS = (ValueError, OSError, KeyError, TypeError, IndexError, AttributeError, UnicodeError)


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"),
                      ensure_ascii=True, allow_nan=False).encode("utf-8")


def file_hash(path):
    h = hashlib.sha256()
    with _io_path(path).open("rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def merge_proof_pins(read, refs):
    need(isinstance(refs, list) and refs, "Complete proof pins required")
    for ref in refs:
        need(isinstance(ref, dict) and set(ref) == {"path", "sha256"}, "Exact proof pin")
        need(isinstance(ref["sha256"], str) and re.fullmatch("[a-f0-9]{64}", ref["sha256"]), "Exact proof digest")
        p = read.path(ref["path"])
        need(p not in read.pins or read.pins[p] == ref["sha256"], "Conflicting earlier proof pin")
        need(file_hash(p) == ref["sha256"], "Earlier proof bytes changed")
        read.pins[p] = ref["sha256"]


def normalized_ref(read, ref):
    p, value = read.obj(ref)
    return {"path": str(p), "sha256": ref["sha256"]}, value


def attempt_subject(read, item, actual):
    job_ref, job = normalized_ref(read, item["job"])
    claim_ref, claim = normalized_ref(read, item["claim"])
    immutable_ref, immutable = normalized_ref(read, item["immutable_claim"])
    need(canonical(claim) == canonical(immutable), "Canonical and immutable claims differ")
    job_sha = fingerprint(job)
    need(claim["job_sha256"] == actual["job_sha256"] == job_sha, "Actual attempt job differs")
    for field in ("attempt_id", "worker"):
        need(isinstance(claim[field], str) and claim[field], "Missing actual " + field)
    jp, cp, ip = (Path(r["path"]) for r in (job_ref, claim_ref, immutable_ref))
    folder = Path(claim["artifact_directory"])
    need(jp.name == "job.json" and cp == jp.parent / "claim.json" and
         ip == folder / "claim.json" and folder.parent == jp.parent and
         folder.name == claim["attempt_id"] and jp.parent.name == job["job_id"], "Exact owned attempt paths required")
    need(isinstance(actual["native_config_sha256"], str) and
         re.fullmatch("[a-f0-9]{64}", actual["native_config_sha256"]), "Actual inspected config hash required")
    need(isinstance(actual["session"], str) and actual["session"] and
         isinstance(actual["run_id"], str) and actual["run_id"], "Actual session/run identity required")
    subject = dict(job_sha256=job_sha, attempt_id=claim["attempt_id"], worker=claim["worker"],
                   catalog_id=job["catalog_id"], candidate_sha256=job["candidate_sha256"],
                   build_sha256=job["build_sha256"], native_config_sha256=actual["native_config_sha256"],
                   native_session_id=actual["session"], run_id=actual["run_id"],
                   job=job_ref, claim=claim_ref, immutable_claim=immutable_ref)
    return subject, job, folder


def evaluate_attempt(read, item, actual, proof_ref):
    subject, job, folder = attempt_subject(read, item, actual)
    proof_ref, proof = normalized_ref(read, proof_ref)
    validator = Path(backend.__file__).resolve()
    need(file_hash(validator) == BACKEND_SHA256, "Native backend changed; independent review required")
    evidence = backend.admit(read.root, job, proof)
    need(file_hash(validator) == BACKEND_SHA256, "Native backend changed during admission")
    need(isinstance(evidence, dict), "Native backend receipt must be an object")
    for field in ("qualified", "queue_success_enabled", "mission_ready",
                  "android_player_build_proved", "device_accepted"):
        need(evidence.get(field) is False, "Compile backend cannot confer " + field)
    success = evidence.get("result") == "MeasuredNativeEditorCompileForExactCandidate"
    if success:
        need(evidence.get("admission") == "compile" and evidence.get("errors") == [], "Exact successful compile backend receipt required")
        for field in ("job_sha256", "candidate_sha256", "build_sha256", "native_config_sha256"):
            need(evidence.get(field) == subject[field], "Backend/actual subject mismatch: " + field)
        coverage = evidence.get("coverage")
        need(isinstance(coverage, dict) and coverage.get("isolatedWholeCurrentNativeSourceCertified") is False and
             coverage.get("androidPlayerBuilt") is False, "No expanded compiler/build coverage allowed")
        merge_proof_pins(read, evidence.get("proof_pins"))
    else:
        need(evidence.get("result") == "MissingOrFailedAdmission" and
             isinstance(evidence.get("errors"), list) and evidence["errors"], "Missing/failed backend must retain reasons")
    # Normalize only proof pin ORDER, never identities, paths, hashes or coverage.
    evidence = json.loads(canonical(evidence))
    if "proof_pins" in evidence:
        evidence["proof_pins"] = sorted(evidence["proof_pins"], key=lambda p: (p["path"], p["sha256"]))
    receipt = dict(schema_version=1, admission="compile", result=evidence["result"],
                   subject=subject, compile_proof=proof_ref, backend_sha256=BACKEND_SHA256,
                   evidence=evidence, **FLAGS)
    read.stable()
    return receipt, folder


def receipt_path(folder, receipt):
    return folder / ("compile-admission-" + hashlib.sha256(canonical(receipt)).hexdigest() + ".json")


def write_immutable(read, folder, receipt):
    target = receipt_path(folder, receipt)
    data = canonical(receipt) + b"\n"
    # Existing immutable claim was Reader-checked; this fixed name contains no caller path.
    need(folder == Path(receipt["subject"]["immutable_claim"]["path"]).parent,
         "Receipt must remain in actual attempt directory")
    normalized_ref(read, receipt["subject"]["immutable_claim"])
    read.stable()
    try:
        flags = os.O_WRONLY | os.O_CREAT | os.O_EXCL | getattr(os, "O_NOFOLLOW", 0)
        fd = os.open(_io_path(target), flags, 0o600)
    except FileExistsError:
        existing = read.path(target)
        need(_io_path(existing).read_bytes() == data, "Existing immutable receipt differs; preserve it")
    else:
        # A partial write is preserved on failure, never deleted or silently retried.
        with os.fdopen(fd, "wb") as f:
            f.write(data)
            f.flush()
            os.fsync(f.fileno())
    ref = {"path": str(target), "sha256": hashlib.sha256(data).hexdigest()}
    _, stored = read.obj(ref)
    need(canonical(stored) == canonical(receipt), "Stored compile receipt differs")
    read.stable()
    return ref


def checked_operational(project, spec):
    result = inspect_operational(project, spec)
    need(result.get("result") == "NormalCompletedUnqualified", "Actual operational inspection must pass first")
    for field in ("qualified", "queue_success_enabled", "mission_ready", "publication_changed"):
        need(result.get(field) is False, "Operational inspection cannot confer " + field)
    need(isinstance(result.get("attempts"), list) and result["attempts"] and
         len(result["attempts"]) == len(spec["attempts"]), "Exact actual operational attempts required")
    read = Reader(project)
    merge_proof_pins(read, result.get("proof_pins"))
    return result, read


def produce(project, spec):
    """Explicit producer; only new immutable receipt files may be written."""
    need(isinstance(spec, dict) and set(spec) == {"schema_version", "operational", "compile_proof"} and
         type(spec["schema_version"]) is int and spec["schema_version"] == 1, "Exact attempt producer schema1")
    result, read = checked_operational(project, spec["operational"])
    refs = []
    admitted = 0
    for item, actual in zip(spec["operational"]["attempts"], result["attempts"]):
        receipt, folder = evaluate_attempt(read, item, actual, spec["compile_proof"])
        refs.append(write_immutable(read, folder, receipt))
        admitted += receipt["result"] == "MeasuredNativeEditorCompileForExactCandidate"
    read.stable()
    return dict(result="ProducedImmutableCompileAttemptReceipts",
                connector_spec=dict(schema_version=3, operational=spec["operational"], compile_receipts=refs),
                receipt_count=len(refs), admitted_attempts=admitted,
                missing_or_failed_attempts=len(refs)-admitted, **FLAGS)


if __name__ == "__main__":
    import argparse
    from skirmish_production_operational_inspector import parse
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--project", type=Path, required=True)
    p.add_argument("--input", type=Path, required=True)
    a = p.parse_args()
    try:
        answer = produce(a.project, parse(_io_path(a.input).read_text(encoding="utf-8-sig")))
    except ERRORS as error:
        answer = dict(result="MissingOrFailedCompileAttemptProduction", errors=[str(error)], **FLAGS)
    print(json.dumps(answer, indent=2))
    raise SystemExit(0 if answer["result"] == "ProducedImmutableCompileAttemptReceipts" and answer["missing_or_failed_attempts"] == 0 else 1)
