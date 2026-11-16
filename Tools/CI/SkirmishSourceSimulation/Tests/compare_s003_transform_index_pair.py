#!/usr/bin/env python3
"""Compare same-build S003 source diagnostics with transform children index off/on."""
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


def normalized(value, world, excluded):
    if isinstance(value, dict):
        return {k: normalized(v, world, excluded) for k, v in value.items() if k not in excluded}
    if isinstance(value, list):
        return [normalized(v, world, excluded) for v in value]
    if isinstance(value, str) and world:
        return value.replace(world, "<source-world>")
    return value


def first_difference(left, right, path="$"):
    """Return a bounded location of the first difference, without printing payloads."""
    if type(left) is not type(right):
        return path
    if isinstance(left, dict):
        for key in sorted(left.keys() | right.keys()):
            if key not in left or key not in right:
                return path + "." + key
            difference = first_difference(left[key], right[key], path + "." + key)
            if difference is not None:
                return difference
    elif isinstance(left, list):
        if len(left) != len(right):
            return path + ".length"
        for index, (a, b) in enumerate(zip(left, right)):
            difference = first_difference(a, b, path + "[" + str(index) + "]")
            if difference is not None:
                return difference
    elif left != right:
        return path
    return None


def compare(baseline, candidate, output):
    dirs = [baseline.resolve(strict=True), candidate.resolve(strict=True)]
    receipts, results, requests = [], [], []
    marker = "[SourceTransformChildrenIndex] enabled=1 scope=ExperimentalSourceOnly"
    for i, directory in enumerate(dirs):
        receipt, result, request = [read(directory / name) for name in
                                    ("run-receipt.json", "result.json", "run-request.json")]
        need(receipt.get("mission") == "S003" and receipt.get("seed") == 104732 and
             receipt.get("status") == "BoundedPrefix" and receipt.get("process_exit") == 0 and
             receipt.get("timed_out") is False and receipt.get("diagnostic_only") is True and
             receipt.get("validation_passed") is False and receipt.get("mission_ready") is False and
             receipt.get("source_result") == result and result.get("source_outcome") == "None" and
             result.get("tick") == 1204 and request.get("max_ticks") == 1200,
             "Completed unqualified 1200-tick S003 prefixes required")
        build = receipt["build_receipt"]; need(sha(Path(build["path"])) == build["sha256"], "Build receipt changed")
        log = (directory / "process.log").read_text()
        need(log.count(marker) == i, "Expected original/candidate digest marker mismatch")
        receipts.append(receipt); results.append(result); requests.append(request)
    need(receipts[0]["build_receipt"]["sha256"] == receipts[1]["build_receipt"]["sha256"], "Same source build required")
    build = read(Path(receipts[0]["build_receipt"]["path"]))
    need(build.get("source_manifest", {}).get("sha256") != build.get("canonical_source_manifest", {}).get("sha256"),
         "Noncanonical experimental build required")
    need(normalized(requests[0], None, {"output_directory", "runtime_config"}) ==
         normalized(requests[1], None, {"output_directory", "runtime_config"}), "Run input descriptors differ")
    traces = [path / "aria-trace.jsonl" for path in dirs]; states = [path / "validator-state.jsonl" for path in dirs]
    trace_hashes = [sha(path) for path in traces]; state_hashes = [sha(path) for path in states]
    observation_exclusions = {"ProjectionTicks", "GoalEvaluationTicks", "AllocatedBytes"}
    keys = ("frame", "time", "action", "decision_kind", "intent", "target_id", "executed", "stopped", "completed_logical_contacts", "receipt")
    worlds = [None, None]; count = 0
    with traces[0].open() as left, traces[1].open() as right:
        for row_a, row_b in itertools.zip_longest(left, right):
            need(row_a is not None and row_b is not None, "ARIA trace lengths differ")
            a, b = json.loads(row_a), json.loads(row_b); count += 1
            if count == 1: worlds = [a.get("world_id"), b.get("world_id")]
            need(all(worlds) and a.get("world_id") == worlds[0] and b.get("world_id") == worlds[1], "World identity changed")
            need(a.get("native_rendered_input") is False and b.get("native_rendered_input") is False and
                 a.get("logical_defense_source_diagnostic") is True and b.get("logical_defense_source_diagnostic") is True,
                 "Explicit non-native source observation required")
            need(all(normalized(a.get(key), worlds[0], set()) == normalized(b.get(key), worlds[1], set()) for key in keys),
                 "Decision or actual source command receipt differs")
            observation_difference = first_difference(
                normalized(a["observation"], worlds[0], observation_exclusions),
                normalized(b["observation"], worlds[1], observation_exclusions))
            need(observation_difference is None,
                 "Source policy observation differs at row " + str(count) +
                 ", frame " + str(a.get("frame")) + ", path " + str(observation_difference))
            need(a.get("s003_defense_site_diagnostics") == b.get("s003_defense_site_diagnostics"), "Source site data differs")
    need(count == 1200, "Exact trace count required")
    snapshots = 0
    with states[0].open() as left, states[1].open() as right:
        for row_a, row_b in itertools.zip_longest(left, right):
            need(row_a is not None and row_b is not None, "Source census lengths differ")
            a, b = json.loads(row_a), json.loads(row_b); snapshots += 1
            need(a.get("world_id") == worlds[0] and b.get("world_id") == worlds[1], "Census source world differs")
            need(normalized(a, worlds[0], {"world_id", "entity_namespace"}) ==
                 normalized(b, worlds[1], {"world_id", "entity_namespace"}), "Sampled source world state differs")
    need(snapshots > 0 and [sha(path) for path in traces] == trace_hashes and
         [sha(path) for path in states] == state_hashes, "Source trace changed during audit")
    need(output.parent.is_dir() and not output.exists(), "Fresh comparison output required")
    evidence = {"kind": "S003TransformChildrenIndexPairComparison", "status": "MatchedBoundedSourceObservation",
                "run_receipt_sha256": [sha(path / "run-receipt.json") for path in dirs],
                "trace_sha256": trace_hashes, "state_sha256": state_hashes,
                "trace_hashes_bound_by_original_receipts": False,
                "decision_and_receipt_rows": count, "sampled_world_state_rows": snapshots,
                "observation_exclusions": sorted(observation_exclusions),
                "advance_wall_seconds": [result["advance_wall_seconds"] for result in results],
                "transform_owner_wall_seconds": [result["cumulative_owner_wall_seconds"]["transforms61"] for result in results],
                "aria_observation_wall_seconds": [result["cumulative_aria_wall_seconds"]["observation"] for result in results],
                "timing_scope": "One same-build off/on pair; cache and scheduler variation not excluded",
                "native_parity_proven": False, "optimization_promoted": False, "mission_ready": False}
    with output.open("x") as stream:
        json.dump(evidence, stream, indent=2); stream.write("\n")
    print(json.dumps(evidence))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("baseline", type=Path); parser.add_argument("candidate", type=Path); parser.add_argument("output", type=Path)
    args = parser.parse_args(); compare(args.baseline, args.candidate, args.output)
