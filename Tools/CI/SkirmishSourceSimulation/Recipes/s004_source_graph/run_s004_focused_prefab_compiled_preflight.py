#!/usr/bin/env python3
"""Run the compiled S004 prefab projection decoder without importing entities."""
import argparse
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(ok, message):
    if not ok:
        raise ValueError(message)


def verify_pins(pins):
    need(isinstance(pins, dict) and pins, "Complete current source build pins required")
    for path, expected in pins.items():
        need(Path(path).is_file() and sha(path) == expected, "Current source build pin changed: " + path)


def run(build_path, projection_path, binary_dir, output):
    build_path, projection_path, output = map(lambda x: Path(x).resolve(),
                                               (build_path, projection_path, output))
    binary_dir = Path(binary_dir).resolve()
    need(not output.exists(), "Fresh compiled-preflight output directory required")
    build = json.loads(build_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("mission") == "S002" and
         build.get("status") == "Passed" and build.get("compile_exit") == 0,
         "Passing current S002 source build receipt required")
    pins = build.get("source_and_reference_pins")
    verify_pins(pins)
    exe = build.get("executable", {})
    need(Path(exe.get("path", "")).is_file() and sha(exe["path"]) == exe.get("sha256"),
         "Pinned source simulation executable required")
    projection_sha = sha(projection_path)
    projection = json.loads(projection_path.read_text())
    runtime = build["runtime_selection"]
    mono, engine, newtonsoft = map(Path, (runtime["mono"], runtime["engine"], runtime["newtonsoft"]))
    need(mono.is_file() and engine.is_dir() and newtonsoft.is_file() and binary_dir.is_dir(),
         "Pinned runtime or captured binary directory unavailable")
    env = os.environ.copy()
    placement = Path(build["placement_build"]).parent
    env["MONO_PATH"] = os.pathsep.join((str(Path(build["repository_root"]) / "Library/ScriptAssemblies"),
                                         str(engine), str(newtonsoft.parent), str(placement)))
    command = [str(mono), exe["path"], "--s004-prefab-projection-preflight", str(projection_path), str(binary_dir)]
    output.mkdir(parents=True)
    invocation = subprocess.run(command, capture_output=True, text=True, env=env, timeout=120)
    log = invocation.stdout + invocation.stderr
    (output / "preflight.log").write_text(log)
    marker = "[S004PrefabProjectionPreflight] "
    need(invocation.returncode == 0 and marker in invocation.stdout,
         "Compiled S004 preflight failed; see " + str(output / "preflight.log"))
    facts = json.loads(invocation.stdout.split(marker, 1)[1].strip().splitlines()[0])
    need(facts.get("status") == "Decoded_SourceOnly_NotImportAdmission" and
         facts.get("compiled_importer_type_and_value_preflight_passed") is True and
         facts.get("roots") == 9 and facts.get("entities") == 276 and
         facts.get("excluded_vfx_references") == 16 and facts.get("omitted_vfx_components") == 10 and
         facts.get("world_allocated") is False and facts.get("entity_identities_allocated") == 0 and
         facts.get("runtime_import_attempted") is False and facts.get("mission_ready") is False,
         "Compiled importer receipt facts changed")

    controls = []
    def rejected(name, mutate):
        probe = copy.deepcopy(projection)
        mutate(probe)
        path = output / (name + ".json")
        path.write_text(json.dumps(probe))
        result = subprocess.run(command[:3] + [str(path), str(binary_dir)], capture_output=True,
                                text=True, env=env, timeout=120)
        need(result.returncode != 0, "Compiled importer accepted negative control: " + name)
        controls.append({"case": name, "result": "Passed"})

    rejected("unknown-field-rejected", lambda x: x["excluded_presentation_object_refs"][0].update(field="UnreviewedPrefab"))
    rejected("missing-field-rejected", lambda x: x["excluded_presentation_object_refs"].pop())
    rejected("duplicate-field-rejected", lambda x: x["excluded_presentation_object_refs"].__setitem__(-1, copy.deepcopy(x["excluded_presentation_object_refs"][0])))
    verify_pins(pins)
    need(sha(projection_path) == projection_sha, "Projection changed during compiled preflight")
    receipt = {"schema_version": 1, "kind": "S004FocusedPrefabCompiledProjectionPreflightReceipt",
               "status": "PassedSourceOnlyCompiledPreflight",
               "build_receipt": {"path": str(build_path), "sha256": sha(build_path)},
               "executable": exe, "projection": {"path": str(projection_path), "sha256": projection_sha},
               "binary_directory": str(binary_dir), "preflight_result": facts,
               "negative_controls": controls, "preflight_log_sha256": sha(output / "preflight.log"),
               "world_allocated": False, "entity_identities_allocated": 0,
               "runtime_import_attempted": False, "runtime_replay_ready": False,
               "mission_ready": False, "native_visual_readiness": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": receipt["status"], "roots": facts["roots"], "entities": facts["entities"],
                      "excluded_refs": facts["excluded_vfx_references"], "world_allocated": False,
                      "mission_ready": False, "output": str(output)}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-receipt", required=True)
    parser.add_argument("--projection", required=True)
    parser.add_argument("--binary-directory", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    try:
        run(args.build_receipt, args.projection, args.binary_directory, args.output)
    except (ValueError, OSError, KeyError, subprocess.SubprocessError, json.JSONDecodeError) as error:
        print("S004 compiled prefab projection preflight failed closed: " + str(error), file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
