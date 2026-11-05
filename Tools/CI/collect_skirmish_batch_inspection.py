"""Collect actual finite-batch references and inspect read-only. Never launch or finish jobs."""
import argparse
import csv
import io
import json
from pathlib import Path
import re

from skirmish_production_operational_inspector import Reader, inspect, need, parse, digest, HEADER, MISSING
from skirmish_production_queue import fingerprint
from skirmish_native_identity import normalize_native_locale


def build_spec(project, descriptor_ref):
    read = Reader(project)
    descriptor_path, descriptor = read.obj(descriptor_ref)
    output = read.path(descriptor["artifact_directory"] + "/batch.json").parent
    need(descriptor_path == output / "batch.json", "Canonical batch descriptor required")
    jobs = descriptor["jobs"]
    need(isinstance(jobs, list) and 1 <= len(jobs) <= 1200, "Bounded finite batch required")

    def ref(path):
        p = read.derived(path)
        return {"path": str(p), "sha256": read.pins[p]}

    spec = {"schema_version": 1, "descriptor": descriptor_ref,
        "batch_events": ref(output / "batch-events.jsonl"),
        "wrapper_log": ref(output / "unity.log"),
        "outer_wrapper": ref(output / "outer-wrapper-receipt.json"),
        "source_before": ref(output / "before-source-receipt.json"),
        "source_after": ref(output / "after-source-receipt.json"), "attempts": []}
    for entry in jobs:
        job_ref = ref(entry["job_path"]); claim_ref = ref(entry["claim_path"])
        _, job = read.obj(job_ref); _, claim = read.obj(claim_ref)
        need(re.fullmatch("[a-f0-9]{64}", job["candidate_sha256"]), "Malformed candidate hash")
        folder = Path(claim["artifact_directory"])
        immutable_ref = ref(folder / "claim.json")
        _, immutable = read.obj(immutable_ref)
        need(claim == immutable and claim["job_sha256"] == fingerprint(job), "Actual claim ownership mismatch")
        native_ref = ref(folder / "adapter-native-events.jsonl")
        _, native = read.lines(native_ref)
        snapshots = [r["facts"] for r in native if r.get("kind") in
                     ("NormalMenuLaunchObserved", "NativeResolvedSnapshot", "TerminalSnapshot")]
        need(snapshots, "Recorded native setup snapshot required")
        # Derive artifact locations from the real ProductionCanary SelectedReportDirectory/
        # CandidateEvidenceRelative/RunsCsvPath methods, never requested output truth.
        csv_ref = ref(folder / "candidates" / job["candidate_sha256"] / "runs.csv")
        _, csv_text = read.read(csv_ref)
        rows = csv.DictReader(io.StringIO(csv_text))
        need(rows.fieldnames == HEADER, "Actual runs.csv schema differs")
        live = read.derived(snapshots[0]["native_trace_path"])
        live_sha = read.pins[live]
        matches = []
        for row in rows:
            if not (row["catalog_id"] == job["catalog_id"] and row["code_hash"] == job["candidate_sha256"]
                    and row["config_hash"] == snapshots[0]["native_config_sha256"]
                    and row["seed"] == str(job["seed"]) and row["size"] == job["size"]
                    and row["difficulty"] == job["difficulty"] and row["executor"] == job["control_mode"]
                    and normalize_native_locale(row["locale"]) == job["locale"]):
                continue
            trace = read.path(row["trace_path"]); log = read.path(row["log_path"])
            run_id = row["run_id"]
            need(isinstance(run_id, str) and run_id and "/" not in run_id and "\\" not in run_id,
                 "Unsafe actual run ID")
            expected = csv_ref["path"]
            parent = Path(expected).parent
            need(trace == parent / (run_id.lower() + ".jsonl")
                 and log == parent / (run_id.lower() + "-log.txt"), "Native final artifact location mismatch")
            if digest(trace) == live_sha:
                matches.append((row, trace, log))
        need(len(matches) == 1, "Exactly one CSV/live-trace matched native run required")
        row, trace, log = matches[0]
        spec["attempts"].append({"job": job_ref, "claim": claim_ref, "immutable_claim": immutable_ref,
            "native_events": native_ref, "run_csv": csv_ref, "run_id": row["run_id"],
            "trace": ref(trace), "contacts": ref(str(trace) + ".touch.jsonl"), "run_log": ref(log)})
    read.stable()
    return spec


def collect_and_inspect(project, descriptor_ref, *, wrapper_platform='windows'):
    try:
        spec = build_spec(project, descriptor_ref)
        result = (inspect(project, spec) if wrapper_platform == 'windows'
                  else inspect(project, spec, wrapper_platform=wrapper_platform))
        result["collection"] = {"result": "RecordedReferencesJoined", "descriptor": descriptor_ref,
                                "attempts": len(spec["attempts"])}
        return result
    except (ValueError, OSError, KeyError, TypeError, IndexError, AttributeError) as error:
        result = {"schema_version": 1, "result": "Incomplete", "wins": 0, "attempts": [],
            "admission": {"result": "MissingAdmission", "missing": list(MISSING)},
            "queue_success_enabled": False, "qualified": False, "mission_ready": False,
            "publication_changed": False, "errors": [str(error)]}
        # Even when artifact collection stops early, preserve actual BatchFailed facts.
        try:
            read = Reader(project); _, descriptor = read.obj(descriptor_ref)
            output = read.path(descriptor["artifact_directory"] + "/batch.json").parent
            path = read.derived(output / "batch-events.jsonl")
            _, events = read.lines({"path": str(path), "sha256": read.pins[path]})
            result["failure_records"] = [r.get("facts", {}) for r in events if r.get("kind") == "BatchFailed"]
            read.stable()
        except (ValueError, OSError, KeyError, TypeError, AttributeError):
            pass
        return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--descriptor", required=True)
    parser.add_argument("--descriptor-sha256", required=True)
    args = parser.parse_args()
    answer = collect_and_inspect(args.project, {"path": args.descriptor, "sha256": args.descriptor_sha256})
    print(json.dumps(answer, indent=2))
    raise SystemExit(0 if answer["result"] == "NormalCompletedUnqualified" else 1)
