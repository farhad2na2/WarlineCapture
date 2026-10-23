"""Read-only launch diagnostics. Never launches Unity or grants mission readiness."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import ntpath
from pathlib import Path
import sys
import time

sys.dont_write_bytecode = True
import skirmish_production_queue as queue

MAX_PARSE_BYTES = 16 * 1024 * 1024
LFS_HEADER = b"version https://git-lfs.github.com/spec/v1"
LIMITATIONS = [
    "Diagnostic observations only; rerun authoritative preflight at dispatch.",
    "Supplied file lists do not prove complete source/build dependency coverage.",
    "No Unity compilation, physical source-fit, gameplay, cleanup or device acceptance.",
    "Does not inspect live Editor/process ownership, settings leases or available memory.",
]


def strict_json(raw):
    def pairs(items):
        result = {}
        for key, value in items:
            if key in result:
                raise ValueError("Duplicate JSON key: " + key)
            result[key] = value
        return result

    def constant(value):
        raise ValueError("Nonfinite JSON constant: " + value)

    def finite_float(value):
        result = float(value)
        if not math.isfinite(result):
            raise ValueError("Nonfinite JSON number: " + value)
        return result

    return json.loads(raw.decode("utf-8-sig"), object_pairs_hook=pairs,
                      parse_constant=constant, parse_float=finite_float)


def read_json(path):
    with Path(path).open("rb") as stream:
        raw = stream.read(MAX_PARSE_BYTES + 1)
    if len(raw) > MAX_PARSE_BYTES:
        raise ValueError("JSON exceeds 16 MiB diagnostic parse limit")
    return strict_json(raw)


def stamp(path):
    value = path.stat()
    return value.st_dev, value.st_ino, value.st_size, value.st_mtime_ns, value.st_ctime_ns


class Doctor:
    def __init__(self, project, windows_root=None, path_limit=240):
        self.project = Path(project).resolve()
        self.windows_root = windows_root
        self.path_limit = path_limit
        self.findings = []
        self.files = []
        self.jobs = []

    def issue(self, code, path, message, owner, severity="error"):
        self.findings.append(dict(code=code, path=str(path), message=message,
                                  owner=owner, severity=severity))

    def check_file(self, path, owner, expected=None, kind="file"):
        path = Path(path)
        record = dict(path=str(path), owner=owner, kind=kind)
        self.files.append(record)
        display = str(path)
        if self.windows_root and path.is_relative_to(self.project):
            display = ntpath.join(self.windows_root, *path.relative_to(self.project).parts)
        if self.windows_root or sys.platform == "win32":
            if len(display) >= self.path_limit:
                self.issue("windows_path_risk", path,
                           f"Projected Windows path is {len(display)} characters: {display}. "
                           "Use a short staging root; this is a conservative warning, not an OS limit.",
                           owner, "warning")
        try:
            before = stamp(path)
            if not path.is_file():
                raise ValueError("Expected a regular file")
            hasher = hashlib.sha256()
            raw = bytearray()
            with path.open("rb") as stream:
                for block in iter(lambda: stream.read(1024 * 1024), b""):
                    hasher.update(block)
                    if len(raw) <= MAX_PARSE_BYTES:
                        raw.extend(block[:MAX_PARSE_BYTES + 1 - len(raw)])
            digest = hasher.hexdigest()
            record["sha256"] = digest
            if stamp(path) != before:
                self.issue("changed_during_read", path, "File changed during inspection; rerun on stable inputs.", owner)
            if raw.startswith(LFS_HEADER):
                self.issue("lfs_pointer", path, "File contains an unresolved Git LFS pointer. No download attempted.", owner)
            if expected is not None and digest != expected:
                hint = ""
                if len(raw) <= MAX_PARSE_BYTES:
                    lf = bytes(raw).replace(b"\r\n", b"\n")
                    crlf = lf.replace(b"\n", b"\r\n")
                    if any(hashlib.sha256(value).hexdigest() == expected for value in (lf, crlf)):
                        hint = " Expected digest matches a newline-normalized variant; preserve the originally pinned bytes."
                self.issue("hash_mismatch", path, f"Expected {expected}; observed {digest}." + hint, owner)
            if kind in ("python", "json"):
                if len(raw) > MAX_PARSE_BYTES:
                    raise ValueError("Source exceeds 16 MiB diagnostic parse limit")
                if kind == "python":
                    compile(bytes(raw), str(path), "exec", dont_inherit=True)
                else:
                    strict_json(bytes(raw))
        except (OSError, ValueError, SyntaxError, TypeError, RecursionError) as exc:
            self.issue("file_check", path, f"{type(exc).__name__}: {exc}", owner)

    def manifest(self, path):
        try:
            value = read_json(path)
            if (not isinstance(value, dict) or set(value) != {"schema_version", "files"}
                    or type(value["schema_version"]) is not int or value["schema_version"] != 1
                    or not isinstance(value["files"], list) or not value["files"]):
                raise ValueError("Expected schema_version=1 and a nonempty files array")
        except (OSError, ValueError, TypeError, RecursionError) as exc:
            self.issue("manifest", path, str(exc), "manifest")
            return
        seen = set()
        for index, entry in enumerate(value["files"]):
            owner = f"manifest[{index}]"
            try:
                if (not isinstance(entry, dict) or "path" not in entry
                        or set(entry) - {"path", "sha256", "kind"}):
                    raise ValueError("File entry needs path and optional sha256/kind only")
                target = queue.relative_file(self.project, entry["path"])
                if target in seen:
                    raise ValueError("Duplicate resolved manifest file")
                seen.add(target)
                if "sha256" in entry and not queue.sha(entry["sha256"]):
                    raise ValueError("sha256 must be lowercase SHA256")
                kind = entry.get("kind", {".py": "python", ".json": "json"}.get(target.suffix.lower(), "file"))
                if kind not in ("file", "python", "json"):
                    raise ValueError("kind must be file, python or json")
                self.check_file(target, owner, entry.get("sha256"), kind)
            except (OSError, ValueError, TypeError, RecursionError) as exc:
                self.issue("manifest_entry", path, str(exc), owner)

    def job(self, path, authority=None, authority_error=False):
        owner = str(path)
        result = dict(path=owner, preflight="failed", native_qualification="pending")
        self.jobs.append(result)
        try:
            job = read_json(path)
            if not isinstance(job, dict):
                raise ValueError("Job must be an object")
            result["catalog_id"] = job.get("catalog_id")
            # Expand file errors so a single failed hash does not hide the next one.
            refs = job.get("source_manifest", [])
            if isinstance(refs, list):
                for entry in refs:
                    if isinstance(entry, dict) and isinstance(entry.get("path"), str):
                        try:
                            target = queue.relative_file(self.project, entry["path"])
                            self.check_file(target, owner, entry.get("sha256"))
                        except (OSError, ValueError, TypeError, RecursionError) as exc:
                            self.issue("job_file", entry["path"], str(exc), owner)
            if isinstance(job.get("packet_path"), str):
                target = queue.relative_file(self.project, job["packet_path"])
                self.check_file(target, owner, job.get("packet_sha256"))
            if authority_error:
                raise ValueError("Requested sampling authority is invalid; job check withheld")
            # Public validator remains authoritative and always re-reads actual bytes.
            result.update(queue.preflight(job, self.project, authority))
        except (OSError, ValueError, TypeError, KeyError, AttributeError, IndexError, RecursionError) as exc:
            self.issue("job_contract", path, f"{type(exc).__name__}: {exc}", owner)

    def report(self, elapsed):
        groups = {}
        for item in self.findings:
            key = (item["severity"], item["code"], item["path"], item["message"])
            group = groups.setdefault(key, {k: v for k, v in item.items() if k != "owner"})
            group.setdefault("affected_inputs", [])
            if item["owner"] not in group["affected_inputs"]:
                group["affected_inputs"].append(item["owner"])
        findings = list(groups.values())
        errors = sum(row["severity"] == "error" for row in findings)
        warnings = sum(row["severity"] == "warning" for row in findings)
        return dict(schema_version=1, result="issues_found" if errors else "diagnostics_clear",
                    errors=errors, warnings=warnings, elapsed_seconds=round(elapsed, 4),
                    file_checks=len(self.files), jobs_checked=len(self.jobs),
                    findings=findings, jobs=self.jobs, files=self.files,
                    native_qualification="pending", player_ready=False,
                    launch_authorized=False, limitations=LIMITATIONS)


def inspect(project, manifest=None, jobs=(), platform="windows", windows_root=None,
            path_limit=240, sampling_plan=None, sampling_sha=None):
    started = time.perf_counter()
    doctor = Doctor(project, windows_root, path_limit)
    required = [("ProjectSettings/ProjectVersion.txt", "file"),
                ("Packages/manifest.json", "json"), ("Packages/packages-lock.json", "json")]
    required += [("Tools/CI/" + name, "file") for name in (
        ("ResolveUnityEditor.ps1", "InvokeUnity.ps1", "InvokeUnityExecuteMethodValidation.ps1")
        if platform == "windows" else ("invoke_unity_macos.sh",))]
    for name, kind in required:
        doctor.check_file(doctor.project / name, "project", kind=kind)
    if manifest:
        doctor.manifest(manifest)
    authority = None
    authority_error = False
    if sampling_plan is not None or sampling_sha is not None:
        try:
            if sampling_plan is None or sampling_sha is None:
                raise ValueError("Both --sampling-plan and --sampling-sha are required")
            from skirmish_production_matrix_authority import SamplingAuthority
            authority = SamplingAuthority(doctor.project, sampling_plan, sampling_sha)
        except (OSError, ValueError, TypeError, KeyError, AttributeError, IndexError, RecursionError) as exc:
            authority_error = True
            doctor.issue("sampling_authority", sampling_plan, str(exc), "sampling")
    for job in jobs:
        doctor.job(job, authority, authority_error)
    return doctor.report(time.perf_counter() - started)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, help="Launch manifest JSON; its file entries are relative to --project")
    parser.add_argument("--job", type=Path, action="append", default=[])
    parser.add_argument("--platform", choices=("windows", "macos"), default="windows")
    parser.add_argument("--windows-root", help="Project's absolute Windows path for projected path warnings")
    parser.add_argument("--path-warning-length", type=int, default=240)
    parser.add_argument("--sampling-plan", type=Path)
    parser.add_argument("--sampling-sha")
    parser.add_argument("--json", action="store_true", help="Full machine-readable observations on stdout")
    args = parser.parse_args(argv)
    if args.path_warning_length < 1:
        parser.error("--path-warning-length must be positive")
    if args.windows_root and not ntpath.isabs(args.windows_root):
        parser.error("--windows-root must be absolute")
    report = inspect(args.project, args.manifest, args.job, args.platform, args.windows_root,
                     args.path_warning_length, args.sampling_plan, args.sampling_sha)
    if args.json:
        print(json.dumps(report, indent=2, ensure_ascii=True))
    else:
        print(f"[SkirmishPreflightDoctor] result={report['result']} errors={report['errors']} "
              f"warnings={report['warnings']} files={report['file_checks']} "
              f"jobs={report['jobs_checked']} seconds={report['elapsed_seconds']}")
        for item in report["findings"]:
            print(f"{item['severity'].upper()} {item['code']}: {item['path']}: {item['message']} "
                  f"(inputs: {', '.join(item['affected_inputs'])})")
        print("Diagnostic only; native qualification pending; no launch authorization.")
    return 1 if report["errors"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
