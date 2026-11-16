"""Finalize narrow S073 synthetic loading-transition evidence; never readiness evidence."""
import argparse
import hashlib
import json
from pathlib import Path
import re


SCOPE = "SyntheticPlacementProbe_SameNativeSessionLoadingTransition_NotNormalInputOrMissionReadiness"
PIN_PATHS = (
    "Assets/Game/Scripts/Editor/Skirmish/S073SyntheticLoadingActivationProbeDriver.cs",
    "Assets/Game/Scripts/Editor/Skirmish/S073NativeLoadingActivationObserver.cs",
)
START_MARKER = re.compile(
    r"^\[S073SyntheticLoadingActivationProbe\] result=Started scope=SyntheticPlacementProbe normalInput=0 output=(\S+)$"
)
PASS_MARKER = re.compile(
    r"^\[S073SyntheticLoadingActivationProbe\] result=Passed scope=SyntheticPlacementProbe normalInput=0 reason=Observed output=(\S+)$"
)
INCOMPLETE_MARKERS = ("result=TimedOut", "TimedOutWithoutObserverOutput", "wrapper_exit=124")


def sha(data):
    return hashlib.sha256(data).hexdigest()


def no_duplicate_keys(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


def require(condition, message):
    if not condition:
        raise ValueError(message)


def _ordered_samples(samples):
    require(isinstance(samples, list) and samples, "Observer samples are required")
    identity = None
    queued = waiting = loaded = starting = complete = started = False
    for sample in samples:
        require(isinstance(sample, dict), "Malformed observer sample")
        key = (sample.get("session_id"), sample.get("scenario_index"), sample.get("seed"),
               tuple(sample.get("session_entity", [])))
        if identity is None:
            identity = key
        require(key == identity, "Samples changed session, scenario, seed, or entity")
        status = sample.get("match_start_status")
        phase = sample.get("phase")
        queued |= phase == "Queued"
        if queued and status == "WaitingForMatchLoaded":
            waiting = True
        if waiting and sample.get("match_scene_loaded") is True:
            loaded = True
        if loaded and status == "Starting":
            starting = True
        if starting and sample.get("gameplay_start_complete") is True:
            complete = True
        if (complete and status == "Started" and phase == "Playing" and
                type(sample.get("match_start_has_started")) is int and
                sample.get("match_start_has_started") == 1 and
                sample.get("simulation_active") == 1 and sample.get("play_requested") == 1):
            started = True
    require(identity is not None and identity[0], "Pinned session identity required")
    require(identity[1:3] == (3, 104729), "S073 scenario 3 seed 104729 required")
    require(queued and waiting and loaded and starting and complete and started,
            "Ordered native queued/waiting/loaded/starting/complete/started transition required")
    return identity


def validate(repo, directory, wrapper_exit, observer_path=None, log_path=None, pins_path=None,
             original_output_path=None):
    repo = Path(repo).resolve(strict=True)
    directory = Path(directory).resolve(strict=True)
    observer_path = Path(observer_path).resolve(strict=True) if observer_path else directory / "observer.json"
    original_output_path = (Path(original_output_path).resolve(strict=True)
                            if original_output_path else observer_path)
    log_path = Path(log_path).resolve(strict=True) if log_path else directory / "editor.log"
    pins_path = Path(pins_path).resolve(strict=True) if pins_path else directory / "source-pins.json"
    observer_raw = observer_path.read_bytes()
    original_raw = original_output_path.read_bytes()
    log_raw, pins_raw = log_path.read_bytes(), pins_path.read_bytes()
    log = log_raw.decode("utf-8", errors="strict")
    pin_manifest = json.loads(pins_raw, object_pairs_hook=no_duplicate_keys)

    # An interrupted wrapper is preserved as incomplete/failed evidence and never
    # upgraded based on a partial observer file or an old successful candidate.
    if wrapper_exit != 0:
        status = "Incomplete" if wrapper_exit == 124 or any(x in log for x in INCOMPLETE_MARKERS) else "Failed"
        return _receipt(status, wrapper_exit, observer_raw, original_raw, log_raw, pins_raw,
                        observer_path, original_output_path, log_path, pins_path, {}, None,
                        "Checked Unity wrapper did not exit with code 0")

    require(observer_raw == original_raw,
            "Durable observer copy must be byte-identical to the original output")
    observer = json.loads(observer_raw, object_pairs_hook=no_duplicate_keys)
    require(observer.get("kind") == "S073NativeLoadingActivationObservation" and
            observer.get("schema_version") == 1 and observer.get("result") == "Observed",
            "Successful S073 native loading observer output required")
    require(observer.get("scope") ==
            "SameNativeSessionMatchStartAndAutomaticLoadingGateTransition_NotNormalInputAttributionOrMissionReadiness",
            "Observer scope must retain normal-input/readiness exclusions")
    require(observer.get("normal_input_invoked_by_observer") is False and
            observer.get("launch_input_attribution") == "NotObserved",
            "Normal-input attribution must remain explicitly unobserved")
    session_identity = _ordered_samples(observer.get("samples"))
    require(observer.get("session_id") == session_identity[0], "Top-level session differs from samples")

    start_matches = [START_MARKER.fullmatch(line) for line in log.splitlines()
                     if line.startswith("[S073SyntheticLoadingActivationProbe]") and "result=Started" in line]
    pass_matches = [PASS_MARKER.fullmatch(line) for line in log.splitlines()
                    if line.startswith("[S073SyntheticLoadingActivationProbe]") and "result=Passed" in line]
    require(len(start_matches) == 1 and start_matches[0] is not None,
            "One exact synthetic-probe Started marker required")
    require(len(pass_matches) == 1 and pass_matches[0] is not None,
            "One exact synthetic-probe Passed marker required")
    original_resolved = original_output_path.resolve()
    require(Path(start_matches[0].group(1)).resolve() == original_resolved and
            Path(pass_matches[0].group(1)).resolve() == original_resolved,
            "Synthetic markers must point to the original observer output")

    require(isinstance(pin_manifest, dict) and
            pin_manifest.get("provenance") in ("PreRunCapture", "PostCaptureCurrentTree") and
            isinstance(pin_manifest.get("source_pins_sha256"), dict) and
            set(pin_manifest["source_pins_sha256"]) == set(PIN_PATHS),
            "Exact source pins and explicit pre-run or post-capture provenance required")
    pins = pin_manifest["source_pins_sha256"]
    verified_pins = {}
    for relative in PIN_PATHS:
        expected = pins[relative]
        require(isinstance(expected, str) and re.fullmatch(r"[0-9a-f]{64}", expected),
                "Malformed source pin: " + relative)
        path = (repo / relative).resolve(strict=True)
        require(path.is_relative_to(repo) and path.is_file(), "Invalid source pin path: " + relative)
        actual = sha(path.read_bytes())
        require(actual == expected, "Capture source changed: " + relative)
        verified_pins[relative] = actual

    require(observer_path.read_bytes() == observer_raw and
            original_output_path.read_bytes() == original_raw and log_path.read_bytes() == log_raw and
            pins_path.read_bytes() == pins_raw, "Capture inputs changed during validation")
    return _receipt("Passed", wrapper_exit, observer_raw, original_raw, log_raw, pins_raw,
                    observer_path, original_output_path, log_path, pins_path, verified_pins, session_identity, SCOPE,
                    pin_manifest["provenance"])


def _receipt(status, wrapper_exit, observer_raw, original_raw, log_raw, pins_raw,
             observer_path, original_output_path, log_path, pins_path, source_pins, identity, scope,
             pin_provenance="Unavailable"):
    return {
        "kind": "S073SyntheticLoadingCaptureReceipt",
        "schema_version": 1,
        "validation_status": status,
        "wrapper_exit_code": wrapper_exit,
        "observer_path": str(observer_path),
        "observer_sha256": sha(observer_raw),
        "original_observer_output_path": str(original_output_path),
        "original_observer_output_sha256": sha(original_raw),
        "editor_log_path": str(log_path),
        "editor_log_sha256": sha(log_raw),
        "source_pins_manifest_path": str(pins_path),
        "source_pins_manifest_sha256": sha(pins_raw),
        "source_pins_sha256": source_pins,
        "source_pin_provenance": pin_provenance,
        "session_id": identity[0] if identity else None,
        "scenario_index": identity[1] if identity else None,
        "seed": identity[2] if identity else None,
        "session_entity": list(identity[3]) if identity else None,
        "scope": scope or "Synthetic capture did not complete; no transition or readiness claim",
        "synthetic_probe": True,
        "normal_input_playthrough": False,
        "launch_input_attribution": "NotObserved",
        "mission_ready": False,
        "player_readiness": False,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--directory", required=True)
    parser.add_argument("--wrapper-exit", required=True, type=int)
    parser.add_argument("--observer")
    parser.add_argument("--original-output", help="Original path named by Unity markers; defaults to --observer")
    parser.add_argument("--log")
    parser.add_argument("--source-pins")
    parser.add_argument("--receipt", required=True, help="New output path; existing evidence is never overwritten")
    args = parser.parse_args()
    root = Path(args.directory)
    receipt = validate(args.repo, root, args.wrapper_exit, args.observer, args.log,
                       args.source_pins, args.original_output)
    receipt_path = Path(args.receipt)
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    with receipt_path.open("x", encoding="utf-8") as output:
        json.dump(receipt, output, indent=2)
        output.write("\n")
    print(json.dumps({"validation_status": receipt["validation_status"],
                      "wrapper_exit_code": receipt["wrapper_exit_code"],
                      "session_id": receipt["session_id"], "scope": receipt["scope"]}))


if __name__ == "__main__":
    main()
