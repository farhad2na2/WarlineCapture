"""Offline job preparation and append-only attempts. Never launches or certifies Unity.

Standard library only. Native adapter integration remains a separate acceptance gate.
"""
from __future__ import annotations

import argparse
from contextlib import contextmanager
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import uuid

from audit_skirmish_native_contacts import audit


VERSION = 1
STAGES = {"compile", "live_api", "source_fit", "normal_input", "terminal", "return"}
CATEGORIES = {"schema", "driver", "game", "infrastructure"}


class ContractError(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise ContractError(message)


def digest(path):
    # Bound memory independently of source size; hash every byte without caching.
    with Path(path).open("rb") as stream:
        if hasattr(hashlib, "file_digest"):
            return hashlib.file_digest(stream, "sha256").hexdigest()
        hasher = hashlib.sha256()
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            hasher.update(block)
        return hasher.hexdigest()


def fingerprint(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def write_new(path, value):
    """Exclusive creation: artifacts are never replaced, even after interruption."""
    with Path(path).open("x", encoding="utf-8") as stream:
        json.dump(value, stream, indent=2, sort_keys=True)
        stream.write("\n")


def now():
    return datetime.now(timezone.utc).isoformat()


def identifier(value):
    require(isinstance(value, str) and re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{0,79}", value), "invalid job/worker identifier")
    return value


def sha(value):
    return isinstance(value, str) and re.fullmatch(r"[a-f0-9]{64}", value) is not None


def relative_file(root, value):
    require(isinstance(value, str) and value and "\\" not in value, "use nonempty relative forward-slash paths")
    path = Path(value)
    require(not path.is_absolute() and ":" not in value and ".." not in path.parts, "path must stay within its root")
    resolved = (Path(root) / path).resolve()
    require(resolved.is_relative_to(Path(root).resolve()), "path escapes root (possibly symlink)")
    return resolved


def outside_assets(path, project):
    require(not Path(path).resolve().is_relative_to((Path(project) / "Assets").resolve()), "queue/artifacts must remain outside imported Assets")


def preflight(job, project, sampling_authority=None):
    # Standalone callers always re-observe actual bytes.
    return _preflight(job, project, sampling_authority)


def _preflight(job, project, sampling_authority=None, *, _observation=None):
    require(isinstance(job, dict), "job must be an object")
    fields = {"schema_version", "job_id", "catalog_id", "definition_id", "definition_version", "candidate_sha256", "build_sha256", "setup_sha256", "content_sha256", "source_manifest", "packet_path", "packet_sha256", "seed", "locale", "size", "difficulty", "control_mode", "required_stages", "timeout_seconds"}
    legacy = type(job.get("schema_version")) is int and job["schema_version"] == 2
    counterpart = type(job.get("schema_version")) is int and job["schema_version"] == 3
    if counterpart: fields = fields | {"counterpart_authority"}
    bindings = {"S001": (0, "SkirmishBaseAssault"), "S025": (1, "SkirmishCityCrossroads"), "S073": (3, "SkirmishIndustrialBasin")}
    if legacy:
        fields = fields - {"definition_id", "definition_version"} | {"legacy_preset_index", "legacy_preset_resource", "legacy_preset_sha256"}
    require(set(job) == fields, "job fields missing or unknown: " + str(sorted(set(job) ^ fields)))
    require(type(job["schema_version"]) is int and job["schema_version"] in (1, 2, 3), "unsupported schema_version")
    identifier(job["job_id"])
    catalog = job["catalog_id"]
    require(isinstance(catalog, str) and re.fullmatch(r"S\d{3}", catalog) and 1 <= int(catalog[1:]) <= 120, "catalog must be S001..S120")
    if legacy:
        require(sampling_authority is not None and catalog in bindings, "legacy requires protected preset and complete canonical sampling authority")
        index, resource = bindings[catalog]
        require(type(job["legacy_preset_index"]) is int and job["legacy_preset_index"] == index and job["legacy_preset_resource"] == resource, "legacy coded binding differs")
        preset = relative_file(project, "Assets/Game/Resources/" + resource + ".asset")
        require(sha(job["legacy_preset_sha256"]) and preset.is_file() and digest(preset) == job["legacy_preset_sha256"], "legacy actual preset bytes differ")
        require(job["seed"] > 0 and job["size"] == "Standard" and job["difficulty"] == "Regular", "requested legacy sampling labels differ; native settings remain separate")
    else:
        require((sampling_authority is not None and catalog in bindings) if counterpart else catalog not in bindings, "schema3 requires exact protected counterpart and complete canonical sampling authority")
        require(job["definition_id"] == "skirmish." + catalog.lower(), "catalog/definition mismatch")
        require(type(job["definition_version"]) is int and job["definition_version"] > 0, "definition_version must be positive integer")
    for field in ("candidate_sha256", "build_sha256", "setup_sha256", "content_sha256", "packet_sha256"):
        require(sha(job[field]), field + " must be lowercase SHA256")
    require(type(job["seed"]) is int and 0 <= job["seed"] <= 2147483647, "seed must be int32, never bool/string")
    require(job["locale"] in ("EN", "FA"), "unsupported locale")
    require(job["size"] in ("Standard", "War", "LargeWar"), "unsupported size")
    require(job["difficulty"] in ("Recruit", "Regular", "Veteran", "Commander"), "unsupported difficulty")
    require(job["control_mode"] in ("manual", "aria"), "unsupported control mode")
    require(type(job["timeout_seconds"]) is int and 1 <= job["timeout_seconds"] <= 14400, "timeout must be 1..14400 integer seconds")
    stages = job["required_stages"]
    require(isinstance(stages, list) and all(isinstance(s, str) for s in stages) and len(stages) == len(set(stages)) and set(stages) == STAGES, "all six required stages must occur exactly once")
    manifest = job["source_manifest"]
    if _observation is None:
        require(isinstance(manifest, list) and bool(manifest), "source_manifest must be nonempty")
        seen = set()
        for entry in manifest:
            require(isinstance(entry, dict) and set(entry) == {"path", "sha256"}, "invalid source manifest entry")
            path = relative_file(project, entry["path"])
            require(entry["path"] not in seen and sha(entry["sha256"]), "duplicate path or invalid source SHA256")
            seen.add(entry["path"])
            require(path.is_file() and digest(path) == entry["sha256"], "source missing or changed: " + entry["path"])
        packet = relative_file(project, job["packet_path"])
        require(packet.is_file() and digest(packet) == job["packet_sha256"], "packet missing or changed")
        text = packet.read_text(encoding="utf-8")
    else:
        # Reachable only from the closed matrix preparer, never public skip/cache input.
        _observation.check_job(job, project)
        text = _observation.packet(job["packet_path"], job["packet_sha256"])
    match = re.search(r"^## " + catalog + r"\s*$([\s\S]*?)(?=^## |\Z)", text, re.MULTILINE)
    require(match is not None, "catalog absent from packet")
    section = match.group(1)
    baseline = re.search(r"Regular seeds ((?:`\d+`[ ,and]*)+) in both EN and FA at Standard", section)
    require(baseline is not None, "packet baseline seed grammar unsupported")
    if job["difficulty"] == "Regular" and job["size"] == "Standard":
        allowed = [int(s) for s in re.findall(r"`(\d+)`", baseline.group(1))]
        require(len(allowed) == 3 and len(set(allowed)) == 3, "packet must supply three distinct baseline seeds")
    else:
        require(job["difficulty"] == "Regular" or job["size"] == "Standard", "no packet seed for additional size AND difficulty")
        label = {"LargeWar": "Large War"}.get(job["size"], job["size"]) if job["size"] != "Standard" else job["difficulty"]
        selected = re.search(re.escape(label) + r"(?: seed)? `(\d+)`", section)
        require(selected is not None, "packet seed not found for selected configuration")
        require(job["difficulty"] == "Regular" or job["locale"] == "EN", "additional difficulty packet seeds are EN only")
        allowed = [int(selected.group(1))]
    if sampling_authority is None:
        require(job["seed"] in allowed, "seed does not match packet/configuration")
    else:
        sampling_authority.validate_job(job)
    if counterpart:
        from skirmish_counterpart_authority import validate_counterpart_authority
        validate_counterpart_authority(job, project, require)
    return {"schema_version": job["schema_version"], "job_sha256": fingerprint(job), "preflight": "passed", "native_qualification": "pending"}


def enqueue(queue, job, project, sampling_authority=None):
    preflight(job, project, sampling_authority)
    return _enqueue_preflighted(queue, job, project)


def _enqueue_preflighted(queue, job, project):
    outside_assets(queue, project)
    directory = Path(queue) / identifier(job["job_id"])
    directory.mkdir(parents=True, exist_ok=False)
    write_new(directory / "job.json", job)
    return {"job_directory": str(directory.resolve()), "status": "pending"}


def validate_completion(job, record, evidence, artifacts, project, sampling_authority=None):
    """Check the proposed adapter contract, without assigning readiness/publication.

    The adapter must independently capture these native facts. This routine checks
    consistency and actual artifact bytes; caller-provided facts alone are not a
    replacement for wrapper ownership or normal-input observation.
    """
    preflight(job, project, sampling_authority)
    required = {"schema_version", "job_sha256", "attempt_id", "worker", "source_start", "source_end", "candidate_sha256", "build_sha256", "setup_sha256", "content_sha256", "stages", "outcome", "simulation_speed", "human_interventions", "artifacts", "wrapper"}
    require(isinstance(evidence, dict) and set(evidence) == required, "completion fields missing/unknown")
    require(type(evidence["schema_version"]) is int and evidence["schema_version"] == VERSION, "completion schema mismatch")
    require(evidence["job_sha256"] == fingerprint(job) == record["job_sha256"], "job provenance mismatch")
    for key in ("attempt_id", "worker"):
        require(evidence[key] == record[key], key + " ownership mismatch")
    for key in ("candidate_sha256", "build_sha256", "setup_sha256", "content_sha256"):
        require(evidence[key] == job[key], key + " mismatch")
    require(evidence["source_start"] == evidence["source_end"] == job["source_manifest"], "start/end source manifest mismatch")
    require(evidence["stages"] == {stage: "passed" for stage in STAGES}, "missing/failed completion stage")
    require(evidence["outcome"] in ("Victory", "Defeat", "Draw"), "missing/unknown terminal outcome")
    require(type(evidence["simulation_speed"]) in (int, float) and evidence["simulation_speed"] == 1, "normal simulation speed required")
    require(type(evidence["human_interventions"]) is int and evidence["human_interventions"] == 0, "human intervention invalidates automated journey")
    references = evidence["artifacts"]
    require(isinstance(references, dict) and set(references) == {"wrapper_log", "contacts", "terminal", "return", "journey"}, "all five artifact references required")
    paths = {}
    for kind, ref in references.items():
        require(isinstance(ref, dict) and set(ref) == {"path", "sha256"} and sha(ref["sha256"]), "invalid artifact reference")
        path = relative_file(artifacts, ref["path"])
        require(path.is_file() and path.stat().st_size > 0 and digest(path) == ref["sha256"], "artifact absent/empty/changed: " + kind)
        paths[kind] = path
    require(len(set(paths.values())) == 5, "proof artifacts must be distinct")
    wrapper = evidence["wrapper"]
    require(isinstance(wrapper, dict) and set(wrapper) == {"attempt_id", "exit_code", "timed_out", "required_pass_marker", "execute_method"}, "invalid wrapper response")
    require(wrapper["attempt_id"] == record["attempt_id"] and type(wrapper["exit_code"]) is int and wrapper["exit_code"] == 0 and wrapper["timed_out"] is False, "wrapper failed/timed out/ownership mismatch")
    require(isinstance(wrapper["required_pass_marker"], str) and wrapper["required_pass_marker"].strip() and isinstance(wrapper["execute_method"], str) and wrapper["execute_method"].strip(), "wrapper marker/method missing")
    require(wrapper["required_pass_marker"] in paths["wrapper_log"].read_text(encoding="utf-8"), "wrapper pass marker missing")
    rows = [json.loads(line) for line in paths["contacts"].read_text(encoding="utf-8").splitlines()]
    contacts = audit(rows)
    require(contacts["strictRecordedContacts"] and contacts["nativeMenuContactsRecorded"] and contacts["terminalContactsRecorded"], "native input contact audit failed")
    for kind in ("terminal", "return", "journey"):
        receipt = load(paths[kind])
        expected = {"attempt_id": record["attempt_id"], "job_sha256": fingerprint(job), "catalog_id": job["catalog_id"]}
        require(isinstance(receipt, dict) and all(receipt.get(k) == v for k, v in expected.items()), kind + " provenance mismatch")
        if kind == "terminal":
            require(receipt.get("outcome") == evidence["outcome"] and receipt.get("rendered") is True, "terminal result not rendered/mismatched")
        elif kind == "return":
            require(receipt.get("returned_to_menu") is True and receipt.get("profile_restored") is True and receipt.get("session_clean") is True, "return/session/profile cleanup missing")
        else:
            for key in ("seed", "locale", "size", "difficulty", "control_mode"):
                require(type(receipt.get(key)) is type(job[key]) and receipt.get(key) == job[key], "journey parameter mismatch: " + key)
            require(receipt.get("normal_menu_launch") is True and receipt.get("read_only_observers") is True and receipt.get("paid_resources") is True and receipt.get("forced_outcome") is False, "normal journey invariants missing")
    return {"completion_contract": "passed", "outcome": evidence["outcome"], "native_qualification": "pending_adapter_canary", "player_device_acceptance": "pending"}


@contextmanager
def guard(directory):
    """Only protects short metadata transactions; never hold during a Unity run.

    Crash leaves the guard in place, fail closed. Operator may archive it after
    proving no metadata writer remains; it is never reclaimed by age alone.
    """
    lock = Path(directory) / ".metadata-lock"
    lock.mkdir()
    try:
        yield
    finally:
        lock.rmdir()


def claim(directory, worker, project, sampling_authority=None, *, before_reserve=None):
    validate = lambda job: preflight(job, project, sampling_authority)
    if before_reserve is None:
        return _claim_checked(directory, worker, project, validate)
    return _claim_checked(directory, worker, project, validate, before_reserve=before_reserve)


def _claim_checked(directory, worker, project, validate, *, before_reserve=None):
    directory = Path(directory)
    worker = identifier(worker)
    outside_assets(directory, project)
    with guard(directory):
        job = load(directory / "job.json")
        validate(job)
        require(not (directory / "claim.json").exists(), "job already claimed; explicit interruption required")
        attempts = sorted(directory.glob("attempt-*/result.json"))
        results = [load(p) for p in attempts]
        require(not any(r["status"] == "qualified" for r in results), "job already qualified")
        signatures = [r.get("failure_signature") for r in results if r["status"] == "failed"]
        require(not any(signatures.count(s) >= 2 for s in signatures), "two identical failure signatures; repair/revise job before retry")
        # Optional refusal-only final admission after fresh source/history IO.
        # Existing callers retain their complete validation and locking rules.
        if before_reserve is not None:
            before_reserve()
        attempt = "attempt-" + uuid.uuid4().hex
        artifacts = directory / attempt
        artifacts.mkdir()
        record = {"attempt_id": attempt, "token": uuid.uuid4().hex, "worker": worker, "started_utc": now(), "job_sha256": fingerprint(job), "artifact_directory": str(artifacts.resolve())}
        write_new(artifacts / "claim.json", record)
        write_new(directory / "claim.json", record)
        return record


def finish(directory, token, status, category=None, signature=None):
    """First slice accepts failure/interruption only; native proof cannot be asserted."""
    require(status in ("failed", "interrupted"), "native completion adapter is not integrated; cannot qualify a job")
    require(category in CATEGORIES and isinstance(signature, str) and signature.strip(), "explicit failure category/signature required")
    directory = Path(directory)
    with guard(directory):
        record = load(directory / "claim.json")
        require(record["token"] == token, "claim ownership mismatch")
        require(fingerprint(load(directory / "job.json")) == record["job_sha256"], "queued job changed during attempt")
        result_path = directory / record["attempt_id"] / "result.json"
        if result_path.exists():
            # A crash after result creation but before claim release can be resumed.
            result = load(result_path)
            require(result.get("token") == token and result.get("status") == status and result.get("failure_category") == category and result.get("failure_signature") == signature, "existing result conflicts; preserve attempt and investigate")
        else:
            result = {**record, "finished_utc": now(), "status": status, "failure_category": category, "failure_signature": signature, "native_qualification": "pending"}
            write_new(result_path, result)
        (directory / "claim.json").unlink()
        return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("preflight", "enqueue", "claim", "finish"))
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--job", type=Path)
    parser.add_argument("--queue", type=Path)
    parser.add_argument("--directory", type=Path)
    parser.add_argument("--worker")
    parser.add_argument("--token")
    parser.add_argument("--status", choices=("failed", "interrupted"))
    parser.add_argument("--category", choices=sorted(CATEGORIES))
    parser.add_argument("--signature")
    parser.add_argument("--sampling-plan", type=Path)
    parser.add_argument("--sampling-sha")
    args = parser.parse_args()
    try:
        require((args.sampling_plan is None)==(args.sampling_sha is None), "Both sampling plan/path SHA required")
        authority=None
        if args.sampling_plan is not None:
            from skirmish_production_matrix_authority import SamplingAuthority
            authority=SamplingAuthority(args.project,args.sampling_plan,args.sampling_sha)
        if args.command in ("preflight", "enqueue"):
            require(args.job is not None, "--job required")
            job = load(args.job)
            if args.command == "preflight":
                result = preflight(job, args.project, authority)
            else:
                require(args.queue is not None, "--queue required")
                result = enqueue(args.queue, job, args.project, authority)
        elif args.command == "claim":
            require(args.directory is not None and args.worker is not None, "--directory and --worker required")
            result = claim(args.directory, args.worker, args.project, authority)
        else:
            require(args.directory is not None, "--directory required")
            result = finish(args.directory, args.token, args.status, args.category, args.signature)
        print(json.dumps(result, indent=2))
        return 0
    except (ContractError, OSError, ValueError, TypeError, KeyError) as exc:
        print(json.dumps({"status": "rejected", "error": str(exc), "native_qualification": "pending"}))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
