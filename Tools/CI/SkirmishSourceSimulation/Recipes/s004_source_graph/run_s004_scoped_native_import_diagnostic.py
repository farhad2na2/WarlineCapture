#!/usr/bin/env python3
"""Run the shared S004 importer in an isolated disposable source-simulation manager."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

KEY_SHA = "7bbcbe81369bf86844167355a6ce942c4be10ee3ff133660b90db4bc79214f58"
IMPORTER = "Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.cs"
HARNESS = "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/S004ScopedNativeImportDiagnostic.cs"
PASS_MARKER = ("[S004ScopedNativeImportDiagnostic] result=Passed roots=9 entities=276 "
               "retainedPresentationRefs=16 omittedPresentationComponents=10 cleanup=Passed rollback=Passed "
               "missionReady=False nativeVisualReadiness=False")


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def verify_pin_map(pins, label):
    need(isinstance(pins, dict) and pins, label + " pin map required")
    for raw_path, digest in pins.items():
        path = Path(raw_path)
        need(path.is_file() and isinstance(digest, str) and len(digest) == 64 and sha(path) == digest,
             label + " live pin mismatch: " + raw_path)


def load_json(path):
    return json.loads(Path(path).read_text())


def run(capture_dir, descriptor_dir, build_dir, output_dir, repo):
    capture_dir, descriptor_dir, build_dir, output_dir, repo = map(
        lambda value: Path(value).resolve(), (capture_dir, descriptor_dir, build_dir, output_dir, repo))
    need(not output_dir.exists(), "Fresh T diagnostic output directory required")
    output_dir.mkdir(parents=True)
    failure_path = output_dir / "failure-receipt.json"
    stage = "input-validation"
    try:
        raw_path = capture_dir / "data/prefab-reference-data.json"
        finalizer_path = capture_dir / "finalizer-receipt.json"
        editor_log = capture_dir / "editor.log"
        descriptor_path = descriptor_dir / "prefab-keys.json"
        descriptor_receipt_path = descriptor_dir / "receipt.json"
        build_path = build_dir / "build.json"
        executable_path = build_dir / "SourceSimulation.exe"
        response_path = build_dir / "compile.rsp"
        harness_path = repo / HARNESS
        for path in (raw_path, finalizer_path, editor_log, descriptor_path, descriptor_receipt_path,
                     build_path, executable_path, response_path, harness_path):
            need(path.is_file(), "Required evidence/build input missing: " + str(path))

        raw, finalizer, descriptor, descriptor_receipt, build = map(load_json,
            (raw_path, finalizer_path, descriptor_path, descriptor_receipt_path, build_path))
        key_sha = sha(descriptor_path)
        need(key_sha == KEY_SHA and descriptor_receipt.get("status") == "PassedSourceOnly" and
             descriptor_receipt.get("descriptor_sha256") == key_sha and descriptor_receipt.get("key_count") == 9,
             "Exact source-only nine-key descriptor receipt required")
        need(finalizer.get("kind") == "S004ScopedPrefabCaptureFinalizerReceipt" and
             finalizer.get("status") == "PassedScopedSourceCaptureOnly" and
             finalizer.get("capture", {}).get("raw_sha256") == sha(raw_path) and
             finalizer.get("key_descriptor", {}).get("sha256") == key_sha and
             finalizer.get("replay_ready") is False and finalizer.get("mission_ready") is False,
             "Passed finalizer receipt must bind this raw capture/descriptor and remain source-only")
        need(raw.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
             raw.get("requested_prefab_keys") == descriptor and raw.get("requested_prefab_keys_sha256") == key_sha and
             raw.get("replay_ready") is False and raw.get("native_visual_readiness") is False and
             raw.get("mission_ready") is False and raw.get("geometry_exported") is False,
             "Original scoped capture authority/schema and false readiness flags required")
        verify_pin_map(raw.get("source_pins"), "Capture")

        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("mission") == "S002" and
             build.get("status") == "Passed" and build.get("compile_exit") == 0 and
             build.get("native_or_player_launched") is False,
             "Passed current S002 source-only build required")
        build_pins = build.get("source_and_reference_pins")
        verify_pin_map(build_pins, "Source build")
        importer_path = repo / IMPORTER
        need(build_pins.get(str(importer_path)) == sha(importer_path),
             "S002 build importer source pin differs from current shared importer")
        need(sha(executable_path) == build.get("executable", {}).get("sha256"),
             "Exact compiled source build executable receipt hash required")
        need(sha(response_path) == build.get("compiler_response", {}).get("sha256"),
             "Exact source build compiler response receipt hash required")
        build_sha = sha(build_path)
        raw_sha = sha(raw_path)
        descriptor_sha = key_sha
        finalizer_sha = sha(finalizer_path)
        importer_sha = sha(importer_path)

        # Reuse the exact pinned source build inputs, changing only output path, entry point,
        # and appending the isolated diagnostic Main. The shared importer source stays pinned.
        stage = "compiler-setup"
        csc_candidates = [Path(path) for path in build_pins if path.endswith("/Roslyn/bincore/csc.dll")]
        need(csc_candidates, "Pinned Roslyn compiler missing from source build pins")
        csc = next((path for path in csc_candidates if path.is_file() and sha(path) == build_pins[str(path)]), None)
        need(csc is not None, "No live pinned Roslyn compiler available")
        dotnet = shutil.which("dotnet")
        need(dotnet is not None, "dotnet host required for pinned Roslyn compiler")
        output_executable = output_dir / "S004ScopedNativeImportDiagnostic.exe"
        generated_rsp = output_dir / "compile.rsp"
        response_lines = response_path.read_text().splitlines()
        changed_out = changed_main = False
        rebuilt_lines = []
        for line in response_lines:
            value = line.strip()
            unquoted = value[1:-1] if len(value) >= 2 and value[0] == value[-1] == '"' else value
            if unquoted.startswith("-out:"):
                line = '"-out:' + str(output_executable) + '"'
                changed_out = True
            elif unquoted.startswith("-main:"):
                line = '"-main:S004ScopedNativeImportDiagnostic"'
                changed_main = True
            rebuilt_lines.append(line)
        need(changed_out and changed_main, "Pinned response file lacks unique output or entry point switches")
        rebuilt_lines.append('"' + str(harness_path) + '"')
        generated_rsp.write_text("\n".join(rebuilt_lines) + "\n")
        (output_dir / "compiler.log").write_text("")

        stage = "compile"
        compilation = subprocess.run([dotnet, str(csc), "@" + str(generated_rsp)],
                                     capture_output=True, text=True, timeout=300)
        compile_log = compilation.stdout + compilation.stderr
        (output_dir / "compiler.log").write_text(compile_log)
        need(compilation.returncode == 0 and output_executable.is_file(),
             "Isolated diagnostic compilation failed; compiler.log retained")

        stage = "import-run"
        runtime = build.get("runtime_selection", {})
        mono = Path(runtime.get("mono", ""))
        engine = Path(runtime.get("engine", ""))
        newtonsoft = Path(runtime.get("newtonsoft", ""))
        need(mono.is_file() and engine.is_dir() and newtonsoft.is_file(), "Pinned Unity runtime references unavailable")
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join((
            str(build_dir / "placement"), str(engine), str(newtonsoft.parent),
            str(repo / "Library/ScriptAssemblies"), str(output_dir)))
        imported_output = output_dir / "import-diagnostic.json"
        command = [str(mono), str(output_executable), str(raw_path), str(descriptor_path),
                   str(raw_path.parent), str(imported_output)]
        invocation = subprocess.run(command, capture_output=True, text=True, env=env, timeout=180)
        run_log = invocation.stdout + invocation.stderr
        (output_dir / "run.log").write_text(run_log)
        need(invocation.returncode == 0 and PASS_MARKER in invocation.stdout and imported_output.is_file(),
             "Actual shared importer diagnostic failed; run.log retained")
        actual = load_json(imported_output)
        for field in ("roots_imported", "entities_imported", "unsupported_leaf_references_in_source",
                      "omitted_presentation_components"):
            expected = {"roots_imported": 9, "entities_imported": 276,
                        "unsupported_leaf_references_in_source": 16,
                        "omitted_presentation_components": 10}[field]
            need(actual.get(field) == expected, "Actual importer fact mismatch: " + field)
        for field in ("successful_import_cleanup", "mid_allocation_failure_injected", "mid_allocation_failure_rollback_clean"):
            need(actual.get(field) is True, "Actual importer lifecycle fact false: " + field)
        for field in ("source_authority_rewritten", "unity_world_created", "default_game_object_injection_world_touched",
                      "mission_initialized", "runtime_replay_ready", "native_visual_readiness", "mission_ready"):
            need(actual.get(field) is False, "Diagnostic scope flag must be false: " + field)

        # Recheck every capture/build dependency after execution and prove primary artifacts were immutable.
        stage = "post-run-integrity"
        verify_pin_map(raw.get("source_pins"), "Capture after import")
        verify_pin_map(build_pins, "Source build after import")
        need(sha(raw_path) == raw_sha and sha(descriptor_path) == descriptor_sha and
             sha(build_path) == build_sha and sha(importer_path) == importer_sha and
             sha(finalizer_path) == finalizer_sha,
             "Diagnostic inputs changed during importer run")

        receipt = {
            "schema_version": 1,
            "kind": "S004ScopedNativeImporterDiagnosticReceipt",
            "status": "Passed_ActualSharedImporter_DisposableEntityManagerOnly",
            "capture": {"raw_path": str(raw_path), "sha256": raw_sha,
                        "finalizer_receipt_path": str(finalizer_path), "finalizer_receipt_sha256": finalizer_sha},
            "key_descriptor": {"path": str(descriptor_path), "sha256": descriptor_sha, "keys": descriptor},
            "source_build": {"build_receipt_path": str(build_path), "build_receipt_sha256": build_sha,
                             "executable_path": str(executable_path), "executable_sha256": sha(executable_path),
                             "importer_source_path": str(importer_path), "importer_source_sha256": importer_sha,
                             "pin_count": len(build_pins)},
            "capture_source_pin_count": len(raw["source_pins"]),
            "compiled_diagnostic": {"executable": str(output_executable), "executable_sha256": sha(output_executable),
                                    "compiler_response_sha256": sha(generated_rsp),
                                    "compiler_log_sha256": sha(output_dir / "compiler.log")},
            "runtime_log_sha256": sha(output_dir / "run.log"),
            "import_result": actual,
            "world_allocated": False,
            "source_authority_rewritten": False,
            "runtime_import_attempted": True,
            "runtime_replay_ready": False,
            "native_visual_readiness": False,
            "mission_ready": False,
            "scope": "Actual shared GeneratedReferenceTemplateImporter.Import exercised on the unchanged source/baker capture in a disposable logical EntityManager. Import and cleanup/rollback mechanics only; no Unity world, source closure admission, gameplay, replay, visual, or mission readiness claim.",
        }
        (output_dir / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
        return receipt
    except Exception as error:
        failure = {"schema_version": 1, "kind": "S004ScopedNativeImporterDiagnosticFailureReceipt",
                   "status": "Failed", "stage": stage, "error": str(error),
                   "raw_capture_sha256": sha(capture_dir / "data/prefab-reference-data.json")
                       if (capture_dir / "data/prefab-reference-data.json").is_file() else None,
                   "failure_log_files": [path.name for path in output_dir.iterdir() if path.is_file()]}
        failure_path.write_text(json.dumps(failure, indent=2) + "\n")
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--capture-dir", required=True)
    parser.add_argument("--descriptor-dir", required=True)
    parser.add_argument("--source-build-dir", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--repo", default=str(Path(__file__).resolve().parents[5]))
    args = parser.parse_args()
    try:
        receipt = run(args.capture_dir, args.descriptor_dir, args.source_build_dir, args.output, args.repo)
        print(json.dumps({"status": receipt["status"], "roots": receipt["import_result"]["roots_imported"],
                          "entities": receipt["import_result"]["entities_imported"],
                          "omitted_presentation_components": receipt["import_result"]["omitted_presentation_components"],
                          "cleanup": "Passed", "rollback": "Passed", "mission_ready": False,
                          "receipt": str(Path(args.output).resolve() / "receipt.json")}, indent=2))
        return 0
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError,
            subprocess.SubprocessError) as error:
        print("S004 scoped native importer diagnostic failed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
