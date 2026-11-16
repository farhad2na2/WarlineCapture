"""Controlled receipt tests; no S004 gameplay or mission readiness is asserted."""
import copy
import hashlib
import importlib.util
import json
import tempfile
from pathlib import Path
from unittest.mock import patch

ROOT = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("source_batch", ROOT / "run_skirmish_source_batch.py")
batch = importlib.util.module_from_spec(spec); spec.loader.exec_module(batch)


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


with tempfile.TemporaryDirectory(prefix="s004-batch-controlled-") as directory:
    root = Path(directory).resolve(); cli = root / "cli"; worker_dir = cli / "worker"; worker_dir.mkdir(parents=True)
    bundle = root / "bundle.json"; bundle.write_text("{}\n")
    build = root / "build.json"; build.write_text("{}\n")
    job = {"mission": "S004", "seed": 104733, "s004_startup_diagnostic": True,
           "data_bundle": {"path": str(bundle), "sha256": sha(bundle)},
           "build_receipt": {"path": str(build), "sha256": sha(build)}}
    worker = {"status": "ObservedDiagnosticSourcePlacement", "physical_source_validated": False,
              "runtime_admission": False, "mission_ready": False,
              "input_sha256": {str(bundle): sha(bundle), str(build): sha(build)},
              "result": {"source_bundle_preflight_passed_before_world": True, "source_simulation_active": 0,
                         "retained_world_scheduler_admitted": False, "registered": 20, "actors_created": 54,
                         "direct_resource_owner_diagnostic": None, "resource_cadence_diagnostic": None}}
    worker_path = worker_dir / "receipt.json"; worker_path.write_text(json.dumps(worker))
    receipt = {"status": "SourceStartupDiagnosticObserved", "mission": "S004", "worker_exit": 0,
               "bundle_sha256": sha(bundle), "resource_owner_diagnostic": None,
               "scheduler_executed": False, "validation_passed": False, "mission_victory": None,
               "mission_ready": False, "worker_receipt": {"path": str(worker_path), "sha256": sha(worker_path)}}
    process = {"cli_exit": 2, "outer_timeout": False, "owned_descendants_survived_cli": False}
    result = batch.classify_s004_startup_diagnostic(job, process, receipt, cli)
    assert result["status"] == "S004SourceStartupDiagnosticObserved", result
    assert result["execution_complete"] is False and result["mission_victory"] is None and result["validation_passed"] is False
    for name, mutate in (
        ("victory", lambda r: r.__setitem__("mission_victory", True)),
        ("ready", lambda r: r.__setitem__("mission_ready", True)),
        ("scheduler", lambda r: r.__setitem__("scheduler_executed", True)),
        ("owner", lambda r: r.__setitem__("resource_owner_diagnostic", "ResourceCadenceAndOwner")),
        ("worker_hash", lambda r: r["worker_receipt"].__setitem__("sha256", "0" * 64)),
    ):
        altered = copy.deepcopy(receipt); mutate(altered)
        assert batch.classify_s004_startup_diagnostic(job, process, altered, cli)["status"] == "Failed", name
    altered_worker = copy.deepcopy(worker); altered_worker["result"]["source_simulation_active"] = False
    worker_path.write_text(json.dumps(altered_worker)); altered = copy.deepcopy(receipt); altered["worker_receipt"]["sha256"] = sha(worker_path)
    assert batch.classify_s004_startup_diagnostic(job, process, altered, cli)["status"] == "Failed"
    bundle.write_text(json.dumps({"kind": "S004SourceDiagnosticInputBundle", "mission_id": "S004",
                                  "scope": "SourceHostDiagnosticOnly", "qualification": {"mission_ready": False, "runtime_admission": False}}))
    admission_job = {"id": "s004", "mission": "S004", "seed": 104733, "timeout": 300,
                     "s004_startup_diagnostic": True,
                     "data_bundle": {"path": str(bundle), "sha256": sha(bundle)},
                     "build_receipt": {"path": str(build), "sha256": sha(build)}}
    jobs_path = root / "jobs.json"
    def load(candidate):
        jobs_path.write_text(json.dumps({"schema_version": 1, "jobs": [candidate]}))
        return batch.load_jobs(jobs_path, root)
    with patch.object(batch, "checked_s004_diagnostic_build"):
        assert len(load(admission_job)[0]) == 1
        for name, mutate in (
            ("seed", lambda j: j.__setitem__("seed", 104734)),
            ("mission", lambda j: j.__setitem__("mission", "S003")),
            ("flag", lambda j: j.__setitem__("s004_startup_diagnostic", 1)),
            ("missing_build", lambda j: j.pop("build_receipt")),
            ("ticks", lambda j: j.__setitem__("max_ticks", 1)),
            ("timeout", lambda j: j.__setitem__("timeout", 1801)),
            ("root_override", lambda j: j.__setitem__("source_repo", str(root))),
        ):
            altered_job = copy.deepcopy(admission_job); mutate(altered_job)
            try:
                load(altered_job)
            except (ValueError, RuntimeError):
                pass
            else:
                raise AssertionError("S004 admission accepted " + name)
        bad_bundle = json.loads(bundle.read_text()); bad_bundle["qualification"]["mission_ready"] = True
        bundle.write_text(json.dumps(bad_bundle)); altered_job = copy.deepcopy(admission_job)
        altered_job["data_bundle"]["sha256"] = sha(bundle)
        try:
            load(altered_job)
        except (ValueError, RuntimeError):
            pass
        else:
            raise AssertionError("S004 admission accepted claimed readiness")
    print("[S004SourceBatchDiagnosticControls] result=Passed receiptPositive=1 receiptRejected=6 admissionPositive=1 admissionRejected=8 missionComplete=0 victory=unknown controlledOnly=1")
