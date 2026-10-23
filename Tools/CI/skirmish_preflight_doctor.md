# Skirmish launch preflight doctor

A read-only diagnostic command for catching avoidable launch failures before paying
for a Unity startup. It extends the existing queue workflow without changing it.
It never starts Unity, executes supplied scripts, modifies source/settings/claims,
downloads LFS files, or qualifies a mission. Imported Python bytecode writes are disabled.

## Usage

Run on Windows against the actual project or staged project used by the checked wrapper:

```powershell
rtk proxy python -B Tools/CI/skirmish_preflight_doctor.py --project D:/Projects/WarlineCapture --platform windows
rtk proxy python -B Tools/CI/skirmish_preflight_doctor.py --project D:/Projects/WarlineCapture --manifest launch-files.json --job path/to/job.json --json
```

On Mac, use `--platform macos`. To examine paths for a future Windows staging root
from either OS, pass `--windows-root D:/W/N`. This projects project-relative file
paths onto that root; it does not access a remote Windows machine. The default
240-character warning threshold is conservative, not a Windows API limit.

Arguments naming the manifest, job files and sampling plan are relative to the
shell working directory, or absolute. File entries *inside* the manifest are
relative to `--project`, must remain inside it after symlink resolution, and use
forward slashes. Point `--project` at the actual staging root to catch omitted files
such as `ProjectSettings/ProjectVersion.txt` before compilation.

Repeat `--job` to inspect multiple existing jobs and group shared file failures.
Protected schema 2/3 jobs need the existing sampling authority:
`--sampling-plan plan.json --sampling-sha <actual-pinned-sha256>`.
An invalid requested authority prevents job checks from silently falling back to
an unprotected mode. The public queue validator re-reads bytes for each job; this
command does not introduce a hash cache or replace dispatch-time admission.

## Launch file manifest

Supply the dependencies actually required by the intended launcher. The tool does
not invent a complete dependency closure or regenerate a hash from changed bytes
and call it the original expected hash.

```json
{
  "schema_version": 1,
  "files": [
    {"path": "Tools/CI/invoke_skirmish_production_batch.py"},
    {"path": "Logs/Prepared/run_owned.py", "kind": "python"},
    {"path": "Logs/Prepared/JOB.json", "kind": "json"}
  ]
}
```

Each entry accepts `path`, optional `kind` (`file`, `python`, `json`), and optional
`sha256` containing the expected lowercase SHA256 from the prepared launch packet.
Python/JSON checks are inferred for `.py`/`.json` when kind is omitted; use `file`
explicitly for other byte formats. Unknown keys and duplicate resolved paths fail.
The manifest is a diagnostic input, not an approval or launch authority.

Checks include:

- Required ProjectVersion, package JSON files and platform wrapper files exist.
- Supplied dependency files exist, match optional pins, and are not LFS pointers.
- Python compiles syntactically without executing or importing the supplied code.
- JSON parses with duplicate keys, nonfinite numbers and excessive nesting rejected.
- Newline-only hash discrepancies are explained but remain errors; no bytes are fixed.
- Observable changes during a file read fail; missing later files are still reported.
- Optional jobs are checked by the existing public `skirmish_production_queue.preflight`.
- Projected long Windows paths produce warnings alongside other findings.

The text output is concise. `--json` includes observations, elapsed time, grouped
findings, affected inputs and per-job contract results. Exit 1 means errors were
found; exit 0 means only the requested diagnostics were clear (warnings may remain).
Argument errors return 2. Output is stdout only; callers can save it to a new log.
Nothing is enqueued, claimed, repaired, or accepted.

## Intended integration

Call this once after preparing a launch packet and before the checked wrapper.
Use the existing packet's dependency pins, collect all failures, repair them, then
rerun. Preserve the wrapper's own fresh admission, process ownership, memory,
settings-lease, source/build, pass-marker and restoration checks. A valid report
is never permission to skip those checks. No native run is needed to test this tool.

This is a troubleshooting precheck, not a faster substitute for the 720-job
qualification matrix. It currently performs additional reads for better error
reporting; bulk-hashing acceleration is deliberately outside this change.
Python syntax is checked with the interpreter running the command, so Windows
should run it using the same Python version as the intended launcher. The checker
does not validate PowerShell syntax, live ownership, memory, settings leases,
assembly consumption, mission geometry, normal-input gameplay or device acceptance.
Unlisted dependencies can still fail later. File observations are not a locked
snapshot; authoritative checks must be repeated at dispatch.

## Tests

```powershell
rtk proxy python -B Tools/CI/skirmish_preflight_doctor_test.py
```

Tests use synthetic fixtures, exercise real symlink rejection where supported,
verify no input-file mutation, and run subprocess CLI success/failure cases.
They print an explicit pass/fail marker with test and skip counts. None constitutes
native mission evidence.
