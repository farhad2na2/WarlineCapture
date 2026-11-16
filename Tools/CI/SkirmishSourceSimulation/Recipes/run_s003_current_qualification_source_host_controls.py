#!/usr/bin/env python3
"""Run focused C# S003 qualification and metadata checks against T16 evidence."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
T16 = ROOT / "T Logs/SkirmishProduction/s003-current-source-qualification-descriptor-20261103-T16"
T7 = ROOT / "T Logs/SkirmishProduction/s003-focused-template-source-host-controls-20261103-T7/receipt.json"
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S003CurrentQualificationSourceHostControls.cs"
PASS = "[S003CurrentQualificationSourceHostControls] result=Passed context=ActualCurrentDualGraph metadata=SourceOnly mutants=12 historicalAudit=SchemaFixture capturePaths=PortableFixture historical=Structural world=0 readiness=0"
CONTROL_STATUS = "Passed_CurrentDualGraphS003QualificationAndMetadataSourceOnly"
CONTROL_TRUE_FIELDS = (
    "actual_setup_compiled", "metadata_factory_domain_qualified",
    "wrong_setup_seed_rejected", "wrong_setup_hash_rejected", "wrong_context_target_rejected",
    "wrong_graph_pin_rejected", "wrong_capture_pin_rejected", "wrong_qualification_flag_rejected",
    "historical_audit_schema_fixture_rejected", "capture_path_fixtures_passed",
    "runtime_graph_swap_rejected", "wrong_canonical_catalog_path_rejected",
    "wrong_canonical_catalog_hash_rejected", "wrong_runtime_setup_matrix_rejected",
    "missing_canonical_catalog_node_rejected", "missing_canonical_catalog_meta_pin_rejected",
    "missing_canonical_catalog_augmentation_rejected", "altered_canonical_catalog_row_rejected",
    "unverified_canonical_catalog_origin_rejected", "current_storage_graph_uses_compiler_graph",
    "missing_selection_graph_node_rejected", "tampered_selection_graph_pin_rejected",
    "wrong_selection_extension_receipt_pin_rejected", "altered_selection_extension_added_pin_rejected",
    "altered_selection_extension_added_node_rejected", "altered_selection_extension_required_asset_rejected",
    "altered_selection_extension_reference_guid_rejected",
    "missing_grid_surface_node_rejected", "tampered_grid_surface_script_pin_rejected",
    "wrong_grid_surface_extension_receipt_pin_rejected", "altered_grid_surface_added_script_pin_rejected",
    "altered_grid_surface_node_guid_rejected", "altered_surface_payload_length_rejected",
    "altered_grid_surface_parser_pin_rejected",
    "historical_branch_preserved_structurally",
)
CONTROL_FALSE_FIELDS = (
    "native_context_qualified", "mission_world_created", "runtime_state_transferred",
    "source_fit_certified", "mission_ready", "historical_branch_runtime_tested",
)


def sha(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def need(condition, message):
    if not condition:
        raise RuntimeError(message)


def validate_control_output(report, stdout, expected_compiler_graph_sha256):
    need(isinstance(report, dict) and report.get("kind") == "S003CurrentQualificationSourceHostControls",
         "Focused S003 control report identity required")
    need(report.get("status") == CONTROL_STATUS, "Current dual-graph source-only control status required")
    need(stdout.splitlines().count(PASS) == 1, "Exact current S003 control pass marker required once")
    for key in CONTROL_TRUE_FIELDS:
        need(report.get(key) is True, "Focused control failed: " + key)
    for key in CONTROL_FALSE_FIELDS:
        need(report.get(key) is False, "Source-only or historical evidence scope changed: " + key)
    need(report.get("capture_source_graph_sha256") == "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb" and
         report.get("runtime_compiler_graph_sha256") == expected_compiler_graph_sha256 and
         report.get("capture_graph_is_runtime_graph") is False and
         report.get("building_count") == 10 and report.get("unit_count") == 19,
         "Current S003 graph identities or source-only metadata counts changed")
    return report["status"]


def verify_pins(pins, repository, runtime_roots):
    need(isinstance(pins, dict) and pins, "Pinned source/reference closure required")
    for raw_path, expected in pins.items():
        path = Path(raw_path).resolve(strict=True)
        need(path.is_relative_to(repository) or any(path.is_relative_to(root) for root in runtime_roots),
             "Source build pin escapes declared roots: " + raw_path)
        need(len(expected) == 64 and sha(path) == expected, "Live source build pin mismatch: " + raw_path)
    return len(pins)


def run(output, qualification_path=None, source_build_path=None, importer_receipt_path=None):
    output = Path(output).resolve()
    need(output.is_relative_to(ROOT) and not output.exists(), "Fresh T output directory inside repository required")
    output.mkdir(parents=True)
    failure = output / "failure-receipt.json"
    stage = "evidence"
    try:
        descriptor_path = Path(qualification_path or T16 / "s003-current-source-qualification-descriptor.json").resolve(strict=True)
        context_path = Path(str(descriptor_path).replace("s003-current-source-qualification-descriptor.json", "s003-current-source-only-context.json")).resolve(strict=True)
        build_receipt = load(descriptor_path.parent / "build-receipt.json")
        qualification = load(descriptor_path)
        context = load(context_path)
        need(build_receipt.get("status") == "PassedDescriptorOnly" and
             build_receipt["context"] == {"path": str(context_path), "sha256": sha(context_path)} and
             build_receipt["qualification_descriptor"] == {"path": str(descriptor_path), "sha256": sha(descriptor_path)} and
             qualification["compiler_context"] == build_receipt["context"] and
             context["kind"] == "OriginalS003CurrentSourceOnlyFactoryContext",
             "Exact passed T16 descriptor/context receipt required")
        compiler_graph = context.get("compiler_graph")
        need(type(compiler_graph) is dict and set(compiler_graph) == {"path", "sha256"} and
             isinstance(compiler_graph["path"], str) and isinstance(compiler_graph["sha256"], str) and
             len(compiler_graph["sha256"]) == 64,
             "Exact compiler graph descriptor in hash-checked context required")
        compiler_graph_candidate = Path(compiler_graph["path"])
        need(compiler_graph_candidate.is_absolute(), "Compiler graph path must be canonical and absolute")
        compiler_graph_path = compiler_graph_candidate.resolve(strict=True)
        no_graph_symlink = compiler_graph_path.is_relative_to(ROOT)
        if no_graph_symlink:
            cursor = ROOT
            for part in compiler_graph_path.relative_to(ROOT).parts:
                cursor = cursor / part
                if cursor.is_symlink():
                    no_graph_symlink = False
                    break
        need(no_graph_symlink and compiler_graph_path.is_file() and
             sha(compiler_graph_path) == compiler_graph["sha256"],
             "Compiler graph must match the hash-checked context descriptor")
        extension = context.get("source_selection_extension_receipt")
        extension_path = ROOT / "T Logs/SkirmishProduction/s003-source-selection-graph-extension-20261103-T1/extension-receipt.json"
        need(type(extension) is dict and set(extension) == {"path", "sha256"} and
             Path(extension["path"]).resolve() == extension_path.resolve() and extension_path.is_file() and
             not extension_path.is_symlink() and sha(extension_path) == extension["sha256"] ==
             "2316b5ab709af39fe2df3d3026637f02b3d92342915d4513753c760b4e3aebb1",
             "T16 context must bind the exact strict source-selection graph extension receipt")
        grid_surface_extension = context.get("source_grid_surface_extension_receipt")
        grid_surface_extension_path = ROOT / "T Logs/SkirmishProduction/s003-final-runtime-source-graph-20261103-T1/extension-receipt.json"
        need(type(grid_surface_extension) is dict and set(grid_surface_extension) == {"path", "sha256"} and
             Path(grid_surface_extension["path"]).resolve() == grid_surface_extension_path.resolve() and
             grid_surface_extension_path.is_file() and not grid_surface_extension_path.is_symlink() and
             sha(grid_surface_extension_path) == grid_surface_extension["sha256"] ==
             "4b051d5eaf5c0b936057259b780734fe0063293828784f91f5cfda238be7f434",
             "T16 context must bind the exact strict grid/surface graph extension receipt")
        t7 = load(importer_receipt_path or T7)
        need(t7.get("status") == "Passed_ActualSharedImporter_DisposableLogicalEntityManagerOnly",
             "Exact passed T7 source-host importer receipt required")
        build_path = Path(source_build_path or t7["source_build"]["path"]).resolve(strict=True)
        build = load(build_path)
        need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
             build.get("mission") == "S003" and build.get("compile_exit") == 0 and
             build["executable"]["sha256"] == sha(build["executable"]["path"]) and
             build["compiler_response"]["sha256"] == sha(build["compiler_response"]["path"]),
             "Exact passed S003 source build identity required")
        response_path = Path(build["compiler_response"]["path"]).resolve(strict=True)
        exe_path = Path(build["executable"]["path"]).resolve(strict=True)
        need(sha(response_path) == build["compiler_response"]["sha256"] and
             sha(exe_path) == build["executable"]["sha256"], "S003 build response/executable hash mismatch")
        harness_sha = sha(HARNESS)

        stage = "compiler"
        pins = build["source_and_reference_pins"]
        runtime_roots = [Path(build["runtime_selection"]["contents"]).resolve(strict=True), Path("/usr/local/share/dotnet").resolve(strict=True)]
        need(verify_pins(pins, ROOT, runtime_roots) == len(pins), "S003 source build pin closure failed")
        importer_path = Path(t7["source_build"]["importer_path"]).resolve(strict=True)
        need(pins.get(str(importer_path)) == t7["source_build"]["importer_sha256"],
             "Source build importer differs from T7 actual importer control")
        cscs = [Path(p) for p in pins if p.endswith("/Roslyn/bincore/csc.dll")]
        dots = [Path(p) for p in pins if Path(p).name == "dotnet"]
        need(cscs and dots, "Pinned Roslyn and dotnet unavailable")
        csc = next((p for p in cscs if p.is_file() and sha(p) == pins[str(p)]), None)
        dotnet = shutil.which("dotnet")
        need(csc is not None and dotnet and any(Path(dotnet).resolve() == p.resolve() for p in dots),
             "Pinned compiler or dotnet host mismatch")
        control_exe = output / "S003CurrentQualificationSourceHostControls.exe"
        rsp_path = output / "compile.rsp"
        rewritten = []
        changed_main = changed_out = False
        for line in response_path.read_text(encoding="utf-8").splitlines():
            value = line.strip()
            unquoted = value[1:-1] if len(value) >= 2 and value[0] == value[-1] == '"' else value
            if unquoted.startswith("-main:"):
                line = '"-main:S003CurrentQualificationSourceHostControls"'
                changed_main = True
            elif unquoted.startswith("-out:"):
                line = '"-out:' + str(control_exe) + '"'
                changed_out = True
            rewritten.append(line)
        need(changed_main and changed_out, "T7 response lacks main/output switches")
        rewritten.append('"' + str(HARNESS) + '"')
        rsp_path.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
        compile_result = subprocess.run([dotnet, str(csc), "@" + str(rsp_path)], capture_output=True, text=True, timeout=300)
        (output / "compile.log").write_text(compile_result.stdout + compile_result.stderr, encoding="utf-8")
        need(compile_result.returncode == 0 and control_exe.is_file(), "Focused C# source-host control compile failed")

        stage = "controls"
        runtime = build["runtime_selection"]
        env = os.environ.copy()
        env["MONO_PATH"] = os.pathsep.join((str(Path(build_path).parent / "placement"), runtime["engine"],
                                             str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
        control_path = output / "control.json"
        args = [str(control_exe), str(ROOT), str(descriptor_path), str(output)]
        result = subprocess.run([runtime["mono"], *args], capture_output=True, text=True, env=env, timeout=180)
        (output / "run.log").write_text(result.stdout + result.stderr, encoding="utf-8")
        need(result.returncode == 0 and control_path.is_file(),
             "Focused current qualification controls failed")
        report = load(control_path)
        control_status = validate_control_output(report, result.stdout, compiler_graph["sha256"])
        need(sha(HARNESS) == harness_sha, "Control source changed during run")
        receipt = {
            "schema_version": 1,
            "kind": "S003CurrentQualificationSourceHostControlsReceipt",
            "status": control_status,
            "qualification_descriptor": {"path": str(descriptor_path), "sha256": sha(descriptor_path)},
            "compiler_context": {"path": str(context_path), "sha256": sha(context_path)},
            "source_build": {"path": str(build_path), "sha256": sha(build_path), "executable_sha256": sha(exe_path),
                             "response_sha256": sha(response_path)},
            "control_source": {"path": str(HARNESS), "sha256": harness_sha},
            "control_executable": {"path": str(control_exe), "sha256": sha(control_exe)},
            "compile_response_sha256": sha(rsp_path), "compile_log_sha256": sha(output / "compile.log"),
            "run_log_sha256": sha(output / "run.log"), "control": report,
            "scope": report["scope"]
        }
        (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
        print(PASS)
        return 0
    except Exception as exc:
        failure.write_text(json.dumps({"status": "Failed", "stage": stage, "error": str(exc)}, indent=2) + "\n", encoding="utf-8")
        print(str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True)
    parser.add_argument("--qualification-descriptor")
    parser.add_argument("--source-build-json")
    parser.add_argument("--importer-control-receipt")
    arguments = parser.parse_args()
    raise SystemExit(run(arguments.output, arguments.qualification_descriptor,
                         arguments.source_build_json, arguments.importer_control_receipt))
