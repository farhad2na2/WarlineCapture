#!/usr/bin/env python3
"""Compile/run isolated controls for the shared source-host template importer on current S003 data."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys


IMPORTER_RELS = (
    Path("Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.cs"),
    Path("Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.s003.cs"),
)
HARNESS_REL = Path("Tools/CI/SkirmishSourceSimulation/Tests/S003FocusedTemplateSourceHostControls.cs")
RAW_REL = Path("Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/data/prefab-reference-data.json")
STRICT_RECEIPT_REL = Path("Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/strict-receipt.json")
EDITOR_LOG_REL = Path("Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/editor.log")
KEYS_REL = Path("T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/prefab-keys.json")
KEY_RECEIPT_REL = Path("T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/receipt.json")
RAW_SHA = "9b31b6863e80d3bd548cd78631a3385b1c7d0ed007da1785c00f1cb16d7a323e"
STRICT_RECEIPT_SHA = "824c296c3b993e1d18cad419c43ffbd1c97bf91988fd7ba8a4e1071b3ad5b1ee"
KEY_SHA = "38bd886005c62bc121d165e1119a3f8876b456eddaa0ef873a1a5681aa66539b"
KEY_RECEIPT_SHA = "c676b2a726ea8134f72189d37e46ecb552e34eec8524b7b033c2464540eee181"
FINALIZER_REL = Path("Tools/CI/SkirmishSourceSimulation/Tests/finalize_s003_focused_prefab_capture.py")
PASS_MARKER = ("[S003FocusedTemplateSourceHostControls] result=Passed roots=19 entities=647 components=3965 "
               "unsupportedLeaves=32 omittedVfxComponents=26 cleanup=Passed preflightRejected=2 world=0 mission=0 readiness=0")


def sha_path(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def load_json(path):
    def unique(pairs):
        value = {}
        for key, child in pairs:
            if key in value:
                raise ValueError("Duplicate JSON property: " + key)
            value[key] = child
        return value
    return json.loads(Path(path).read_text(encoding="utf-8"), object_pairs_hook=unique)


def verify_pin_map(pins, label, repository=None, external_roots=()):
    need(isinstance(pins, dict) and pins, label + " pin map required")
    for raw_path, expected in pins.items():
        path = Path(raw_path)
        if not path.is_absolute():
            need(repository is not None, label + " relative pin path without repository")
            path = repository / path
        resolved = path.resolve(strict=True)
        if repository is not None:
            need(resolved.is_relative_to(repository) or any(resolved.is_relative_to(root) for root in external_roots),
                 label + " pin escapes declared source/runtime roots: " + raw_path)
        need(isinstance(expected, str) and len(expected) == 64 and sha_path(resolved) == expected,
             label + " live pin mismatch: " + raw_path)
    return len(pins)


def validate_source_build(repo, source_build_dir):
    source_build_dir = Path(source_build_dir).resolve(strict=True)
    need(source_build_dir.is_relative_to(repo), "Explicit source-build directory must remain under repository")
    build_path = source_build_dir / "build.json"
    response_path = source_build_dir / "compile.rsp"
    executable_path = source_build_dir / "SourceSimulation.exe"
    for path in (build_path, response_path, executable_path):
        need(path.is_file(), "Explicit source-build artifact missing: " + str(path))
    build = load_json(build_path)
    runtime = build.get("runtime_selection", {})
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("compile_exit") == 0 and build.get("native_or_player_launched") is False and
         build.get("executable", {}).get("sha256") == sha_path(executable_path) and
         build.get("compiler_response", {}).get("sha256") == sha_path(response_path),
         "Exact passed source-only build receipt required")
    allowed_runtime_roots = [Path(runtime["contents"]).resolve(strict=True), Path("/usr/local/share/dotnet").resolve(strict=True)]
    pins = build.get("source_and_reference_pins")
    pin_count = verify_pin_map(pins, "Source build", repo, allowed_runtime_roots)
    compiled_importers = []
    response_lines = response_path.read_text(encoding="utf-8").splitlines()
    for relative in IMPORTER_RELS:
        path = (repo / relative).resolve()
        count = sum(1 for line in response_lines if str(path) in line)
        if count:
            need(count == 1, "Explicit build response must compile each importer exactly once: " + str(path))
            need(path.is_file() and pins.get(str(path)) == sha_path(path),
                 "Explicit build importer source pin mismatch: " + str(path))
            compiled_importers.append((path, sha_path(path)))
    need(len(compiled_importers) == 1,
         "Explicit source build must compile exactly one recognized GeneratedReferenceTemplateImporter")
    importer_path, importer_sha = compiled_importers[0]
    return {"build_dir": source_build_dir, "build_path": build_path, "build": build,
            "build_pins": pins, "build_pin_count": pin_count, "importer_path": importer_path,
            "importer_sha": importer_sha, "response_path": response_path,
            "executable_path": executable_path, "build_sha": sha_path(build_path)}


def validate_evidence(repo, source_build_dir):
    raw_path, receipt_path, log_path = repo / RAW_REL, repo / STRICT_RECEIPT_REL, repo / EDITOR_LOG_REL
    keys_path, key_receipt_path = repo / KEYS_REL, repo / KEY_RECEIPT_REL
    source_build = validate_source_build(repo, source_build_dir)
    build_dir = source_build["build_dir"]
    build_path = source_build["build_path"]
    for path in (raw_path, receipt_path, log_path, keys_path, key_receipt_path, build_path,
                 source_build["response_path"], source_build["executable_path"],
                 *(repo / relative for relative in IMPORTER_RELS),
                 repo / HARNESS_REL, repo / FINALIZER_REL):
        need(path.is_file(), "Required exact evidence missing: " + str(path))
    need(sha_path(raw_path) == RAW_SHA and sha_path(receipt_path) == STRICT_RECEIPT_SHA and
         sha_path(keys_path) == KEY_SHA and sha_path(key_receipt_path) == KEY_RECEIPT_SHA and
         sha_path(source_build["importer_path"]) == source_build["importer_sha"],
         "Exact current S003 artifacts/importer/source-build required")
    raw, strict, keys, key_receipt, build = map(load_json,
        (raw_path, receipt_path, keys_path, key_receipt_path, build_path))
    need(strict.get("kind") == "S003FocusedPrefabCaptureReceipt" and
         strict.get("validation_status") == "PassedDiagnosticSourceOnly" and
         strict.get("capture_sha256") == RAW_SHA and strict.get("key_descriptor_sha256") == KEY_SHA and
         strict.get("root_count") == 19 and strict.get("entity_count") == 647 and
         strict.get("unsupported_count") == 32 and strict.get("source_pin_count") == 16720 and
         strict.get("utility_world_created") is True and strict.get("utility_world_disposed") is True and
         strict.get("mission_world_created") is False and strict.get("mission_ready") is False,
         "Exact strict source-only S003 finalizer receipt required")
    need(key_receipt.get("kind") == "S003FocusedPrefabKeyDescriptorReceipt" and
         key_receipt.get("status") == "PassedSourceOnly" and key_receipt.get("descriptor", {}).get("sha256") == KEY_SHA and
         key_receipt.get("descriptor", {}).get("key_count") == 19 and
         key_receipt.get("descriptor", {}).get("keys_in_metadata_order") == keys and
         raw.get("requested_prefab_keys") == keys and raw.get("requested_prefab_keys_sha256") == KEY_SHA,
         "Exact current ordered S003 key descriptor required")
    build_pins = source_build["build_pins"]
    build_pin_count = source_build["build_pin_count"]

    finalizer_spec = importlib.util.spec_from_file_location("s003_current_focused_finalizer", repo / FINALIZER_REL)
    finalizer = importlib.util.module_from_spec(finalizer_spec)
    finalizer_spec.loader.exec_module(finalizer)
    revalidated = finalizer.validate(repo, raw_path, log_path, 0)
    need(revalidated == strict, "Stored strict finalizer receipt differs from full current offline revalidation")
    raw_pin_count = verify_pin_map(raw.get("source_pins"), "Raw capture", repo)
    need(raw_pin_count == strict["source_pin_count"], "Raw current source pin count differs from strict receipt")
    return {"repo": repo, "raw_path": raw_path, "receipt_path": receipt_path, "keys_path": keys_path,
            "key_receipt_path": key_receipt_path, "build_dir": build_dir, "build_path": build_path,
            "build": build, "build_pins": build_pins, "build_pin_count": build_pin_count,
            "raw_pin_count": raw_pin_count, "importer_path": source_build["importer_path"],
            "importer_sha": source_build["importer_sha"], "build_sha": source_build["build_sha"],
            "source_response_path": source_build["response_path"],
            "source_executable_path": source_build["executable_path"],
            "harness_path": repo / HARNESS_REL, "strict_receipt": strict, "keys": keys,
            "raw_sha": RAW_SHA, "strict_receipt_sha": STRICT_RECEIPT_SHA,
            "key_sha": KEY_SHA, "key_receipt_sha": KEY_RECEIPT_SHA}


def run(output, repo, source_build_dir):
    output = Path(os.path.abspath(output))
    repo = Path(repo).resolve(strict=True)
    need(output.is_relative_to(repo) and not output.exists(), "Fresh T output directory inside repository required")
    evidence = validate_evidence(repo, source_build_dir)
    output.mkdir(parents=True)
    failure_path = output / "failure-receipt.json"
    stage = "compiler-setup"
    try:
        build = evidence["build"]
        build_dir = evidence["build_dir"]
        source_response = evidence["source_response_path"]
        csc_candidates = [Path(path) for path in evidence["build_pins"] if path.endswith("/Roslyn/bincore/csc.dll")]
        need(csc_candidates, "Pinned Roslyn compiler missing from source build pins")
        csc = next((path for path in csc_candidates if path.is_file() and sha_path(path) == evidence["build_pins"][str(path)]), None)
        need(csc is not None, "No live pinned Roslyn compiler available")
        pinned_dotnet = [Path(path).resolve() for path in evidence["build_pins"]
                         if Path(path).name == "dotnet" and Path(path).is_file()]
        dotnet = shutil.which("dotnet")
        need(dotnet is not None and any(Path(dotnet).resolve() == path for path in pinned_dotnet),
             "Pinned dotnet host required for pinned Roslyn compiler")
        out_exe = output / "S003FocusedTemplateSourceHostControls.exe"
        rsp = output / "compile.rsp"
        lines = source_response.read_text(encoding="utf-8").splitlines()
        changed_out = changed_main = False
        rewritten = []
        for line in lines:
            value = line.strip()
            unquoted = value[1:-1] if len(value) >= 2 and value[0] == value[-1] == '"' else value
            if unquoted.startswith("-out:"):
                line = '"-out:' + str(out_exe) + '"'
                changed_out = True
            elif unquoted.startswith("-main:"):
                line = '"-main:S003FocusedTemplateSourceHostControls"'
                changed_main = True
            rewritten.append(line)
        need(changed_out and changed_main, "Pinned source response lacks unique output/main switches")
        rewritten.append('"' + str(evidence["harness_path"]) + '"')
        rsp.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
        (output / "compiler.log").write_text("", encoding="utf-8")
        runtime_roots = [Path(build["runtime_selection"]["contents"]).resolve(strict=True),
                         Path("/usr/local/share/dotnet").resolve(strict=True)]
        need(verify_pin_map(evidence["build_pins"], "Source build before compile", repo, runtime_roots) == evidence["build_pin_count"],
             "Source build pins drifted before compile")
        compile_run = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=300)
        compile_log = compile_run.stdout + compile_run.stderr
        (output / "compiler.log").write_text(compile_log, encoding="utf-8")
        need(compile_run.returncode == 0 and out_exe.is_file(), "Isolated S003 source-host control compile failed")

        stage = "source-host-import-control"
        runtime = build.get("runtime_selection", {})
        mono, engine, newtonsoft = Path(runtime.get("mono", "")), Path(runtime.get("engine", "")), Path(runtime.get("newtonsoft", ""))
        need(mono.is_file() and engine.is_dir() and newtonsoft.is_file(), "Pinned source-host runtime refs unavailable")
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join((str(build_dir / "placement"), str(engine), str(newtonsoft.parent),
                                            str(repo / "Library/ScriptAssemblies"), str(output)))
        control_path = output / "control.json"
        command = [str(mono), str(out_exe), str(evidence["raw_path"]), str(evidence["keys_path"]),
                   str(evidence["receipt_path"]), str(evidence["raw_path"].parent), str(control_path)]
        control_run = subprocess.run(command, capture_output=True, text=True, env=env, timeout=180)
        run_log = control_run.stdout + control_run.stderr
        (output / "run.log").write_text(run_log, encoding="utf-8")
        need(control_run.returncode == 0 and control_run.stdout.splitlines().count(PASS_MARKER) == 1 and control_path.is_file(),
             "Shared S003 source-host importer controls failed")
        actual = load_json(control_path)
        for name, expected in (("roots_imported", 19), ("entities_imported", 647),
                               ("components_imported", 3965), ("unsupported_leaf_references_in_source", 32),
                               ("omitted_presentation_components", 26)):
            need(actual.get(name) == expected, "Source-host importer result mismatch: " + name)
        for name in ("preflight_no_identity_allocation", "unsupported_non_vfx_rejected_before_allocation",
                     "foreign_required_key_rejected_before_allocation", "successful_import_cleanup"):
            need(actual.get(name) is True, "Source-host negative/lifecycle control failed: " + name)
        for name in ("unity_world_created", "mission_manager_created", "mission_initialized", "source_authority_rewritten",
                     "runtime_replay_ready", "native_visual_readiness", "source_fit_certified",
                     "native_context_qualified", "mission_ready"):
            need(actual.get(name) is False, "No-Unity/no-mission/no-readiness control failed: " + name)

        stage = "post-run-source-integrity"
        verify_pin_map(load_json(evidence["raw_path"]).get("source_pins"), "Raw capture after import", repo)
        verify_pin_map(evidence["build_pins"], "Source build after import", repo, runtime_roots)
        for path, digest in ((evidence["raw_path"], evidence["raw_sha"]),
                             (evidence["receipt_path"], evidence["strict_receipt_sha"]),
                             (evidence["keys_path"], evidence["key_sha"]),
                             (evidence["key_receipt_path"], evidence["key_receipt_sha"]),
                             (evidence["build_path"], evidence["build_sha"]),
                             (evidence["importer_path"], evidence["importer_sha"]),
                             (evidence["harness_path"], sha_path(evidence["harness_path"]))):
            need(sha_path(path) == digest, "Pinned source/build/input changed during source-host run: " + str(path))
        report = {
            "schema_version": 1,
            "kind": "S003FocusedTemplateSourceHostControlsReceipt",
            "status": "Passed_ActualSharedImporter_DisposableLogicalEntityManagerOnly",
            "capture": {"path": str(evidence["raw_path"]), "sha256": evidence["raw_sha"],
                        "strict_receipt_path": str(evidence["receipt_path"]),
                        "strict_receipt_sha256": evidence["strict_receipt_sha"]},
            "key_descriptor": {"path": str(evidence["keys_path"]), "sha256": evidence["key_sha"],
                               "receipt_path": str(evidence["key_receipt_path"]),
                               "receipt_sha256": evidence["key_receipt_sha"], "keys": evidence["keys"]},
            "source_build": {"path": str(evidence["build_path"]), "sha256": evidence["build_sha"],
                             "executable_sha256": build["executable"]["sha256"],
                             "source_response_sha256": build["compiler_response"]["sha256"],
                             "pin_count": evidence["build_pin_count"],
                             "importer_path": str(evidence["importer_path"]), "importer_sha256": evidence["importer_sha"]},
            "raw_capture_source_pin_count": evidence["raw_pin_count"],
            "control_executable": {"path": str(out_exe), "sha256": sha_path(out_exe)},
            "control_compile_response_sha256": sha_path(rsp),
            "control_compile_log_sha256": sha_path(output / "compiler.log"),
            "control_run_log_sha256": sha_path(output / "run.log"),
            "import_result": actual,
            "unity_world_created": False,
            "mission_manager_created": False,
            "source_authority_rewritten": False,
            "runtime_replay_ready": False,
            "native_visual_readiness": False,
            "source_fit_certified": False,
            "native_context_qualified": False,
            "mission_ready": False,
            "scope": "The pinned shared GeneratedReferenceTemplateImporter was preflighted and exercised only in an isolated source-host logical EntityManager. All 32 exact VFX UnityObjectRef leaves remain unconsumed; 26 VFX components are omitted. No Unity World, mission manager, gameplay, replay, source-fit, or readiness authority.",
        }
        (output / "receipt.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        return report
    except Exception as error:
        failure = {"schema_version": 1, "kind": "S003FocusedTemplateSourceHostControlsFailureReceipt",
                   "status": "Failed", "stage": stage, "error": str(error),
                   "existing_files": sorted(path.name for path in output.iterdir() if path.is_file())}
        failure_path.write_text(json.dumps(failure, indent=2) + "\n", encoding="utf-8")
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[4])
    parser.add_argument("--source-build-dir", required=True, type=Path,
                        help="Explicit fresh source-build directory containing build.json, compile.rsp and SourceSimulation.exe")
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    try:
        result = run(args.output, args.repo, args.source_build_dir)
        print(json.dumps({"status": result["status"], "preflight": result["import_result"]["preflight"],
                          "roots": result["import_result"]["roots_imported"],
                          "entities": result["import_result"]["entities_imported"],
                          "unsupported_leaves": result["import_result"]["unsupported_leaf_references_in_source"],
                          "omitted_vfx_components": result["import_result"]["omitted_presentation_components"],
                          "unity_world": False, "mission_ready": False,
                          "receipt": str(args.output.resolve() / "receipt.json")}, indent=2))
        return 0
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError, subprocess.SubprocessError) as error:
        print("S003 focused template source-host controls failed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
