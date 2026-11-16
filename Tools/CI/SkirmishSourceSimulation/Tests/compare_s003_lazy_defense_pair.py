#!/usr/bin/env python3
"""Compare source-only S003 lazy-site experiment without asserting native parity."""
import argparse
import hashlib
import itertools
import json
from pathlib import Path


def sha(path):
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def read(path):
    return json.loads(path.read_bytes())


def need(condition, message):
    if not condition:
        raise ValueError(message)


def strip(value, excluded):
    if isinstance(value, dict):
        return {key: strip(item, excluded) for key, item in value.items() if key not in excluded}
    if isinstance(value, list):
        return [strip(item, excluded) for item in value]
    return value


def compare(baseline, candidate, output):
    dirs = (baseline.resolve(strict=True), candidate.resolve(strict=True))
    receipts, results, requests, builds = [], [], [], []
    for directory in dirs:
        receipt, result, request = (read(directory / name) for name in
                                    ("run-receipt.json", "result.json", "run-request.json"))
        need(receipt.get("mission") == "S003" and receipt.get("seed") == 104732 and
             receipt.get("status") == "BoundedPrefix" and receipt.get("process_exit") == 0 and
             receipt.get("timed_out") is False and receipt.get("validation_passed") is False and
             receipt.get("native_acceptance") is False and receipt.get("mission_ready") is False and
             receipt.get("source_result") == result and result.get("source_outcome") == "None" and
             result.get("tick") == 1204 and request.get("max_ticks") == 1200,
             "Matching completed unqualified S003 prefixes required")
        build_descriptor = receipt["build_receipt"]
        build_path = Path(build_descriptor["path"])
        need(sha(build_path) == build_descriptor["sha256"], "Build receipt changed")
        build = read(build_path)
        need(build.get("status") == "Passed" and build.get("mission") == "S003", "Passing S003 build required")
        receipts.append(receipt); results.append(result); requests.append(request); builds.append(build)
    need(strip(requests[0], {"output_directory", "runtime_config"}) ==
         strip(requests[1], {"output_directory", "runtime_config"}), "Run inputs differ")
    pins_a, pins_b = (build["source_and_reference_pins"] for build in builds)
    common_drift = [name for name in pins_a.keys() & pins_b.keys() if pins_a[name] != pins_b[name]]
    expected = str(Path(builds[0]["repository_root"]) /
                   "Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.s003.cs")
    same_build = sha(Path(receipts[0]["build_receipt"]["path"])) == sha(Path(receipts[1]["build_receipt"]["path"]))
    need(common_drift == [expected] or same_build and not common_drift,
         "Builds differ in unexpected shared source pins")
    trace_hashes = [sha(directory / "aria-trace.jsonl") for directory in dirs]
    state_hashes = [sha(directory / "validator-state.jsonl") for directory in dirs]
    omitted = observed = rows = 0
    keys = ("frame", "time", "action", "decision_kind", "intent", "target_id", "executed", "stopped", "completed_logical_contacts", "receipt")
    exclude_observation = {"Site0", "Site1", "Site2", "Site3", "Site4", "Site5", "ProjectionTicks", "GoalEvaluationTicks", "AllocatedBytes"}
    with (dirs[0] / "aria-trace.jsonl").open() as left, (dirs[1] / "aria-trace.jsonl").open() as right:
        for raw_a, raw_b in itertools.zip_longest(left, right):
            need(raw_a is not None and raw_b is not None, "Trace lengths differ")
            a, b = json.loads(raw_a), json.loads(raw_b); rows += 1
            need(all(a.get(key) == b.get(key) for key in keys), "Decision or source command receipt differs")
            need(strip(a["observation"], exclude_observation) == strip(b["observation"], exclude_observation),
                 "Non-site observation differs")
            need(a.get("logical_defense_source_diagnostic") is True and b.get("logical_defense_source_diagnostic") is True and
                 a.get("native_rendered_input") is False and b.get("native_rendered_input") is False,
                 "Logical source-only trace required")
            if b.get("s003_site_projection_observed") is True:
                observed += 1
                need(a.get("s003_defense_site_diagnostics") == b.get("s003_defense_site_diagnostics"),
                     "Consumed source site projection differs")
            else:
                omitted += 1
                need(b.get("s003_site_projection_required") is False and b.get("s003_defense_site_diagnostics") is None,
                     "Unobserved site facts must be explicit")
    need(rows == 1200 and observed > 0 and omitted > 0, "Expected both observed and omitted source projections")
    snapshots = 0
    with (dirs[0] / "validator-state.jsonl").open() as left, (dirs[1] / "validator-state.jsonl").open() as right:
        for raw_a, raw_b in itertools.zip_longest(left, right):
            need(raw_a is not None and raw_b is not None, "Census lengths differ")
            need(strip(json.loads(raw_a), {"world_id", "entity_namespace"}) ==
                 strip(json.loads(raw_b), {"world_id", "entity_namespace"}), "Source census differs")
            snapshots += 1
    need(snapshots > 0 and [sha(directory / "aria-trace.jsonl") for directory in dirs] == trace_hashes and
         [sha(directory / "validator-state.jsonl") for directory in dirs] == state_hashes,
         "Source traces changed during comparison")
    need(output.parent.is_dir() and not output.exists(), "Fresh comparison output required")
    value = {"kind": "S003LazyDefenseSitePairComparison", "status": "MatchedBoundedSourceObservation",
             "run_receipt_sha256": [sha(directory / "run-receipt.json") for directory in dirs],
             "trace_sha256": trace_hashes, "state_sha256": state_hashes,
             "trace_hashes_bound_by_original_receipts": False,
             "decision_and_receipt_rows": rows, "sampled_world_state_rows": snapshots,
             "site_projection_rows_observed": observed, "site_projection_rows_not_observed": omitted,
             "same_build": same_build,
             "observation_exclusions": sorted(exclude_observation),
             "advance_wall_seconds": [result["advance_wall_seconds"] for result in results],
             "aria_observation_wall_seconds": [result["cumulative_aria_wall_seconds"]["observation"] for result in results],
             "timing_scope": "One non-alternating bounded pair; cache and scheduler variation not excluded",
             "native_parity_proven": False, "mission_ready": False, "optimization_promoted": False}
    with output.open("x") as stream:
        json.dump(value, stream, indent=2); stream.write("\n")
    print(json.dumps(value))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("baseline", type=Path); parser.add_argument("candidate", type=Path); parser.add_argument("output", type=Path)
    args = parser.parse_args()
    compare(args.baseline, args.candidate, args.output)
