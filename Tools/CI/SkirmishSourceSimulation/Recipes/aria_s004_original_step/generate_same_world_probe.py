#!/usr/bin/env python3
"""Prepare the original S004 planner and strict same-world inspector overlay."""
import hashlib
import importlib.util
import json
from pathlib import Path
import argparse

ROOT = Path(__file__).resolve().parents[5]
BASE = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/aria_s073_original_step"
FIXTURE = ROOT / "T Logs/SkirmishProduction/aria-air-opening-s004-control-20261104-T1"


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def module(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    value = importlib.util.module_from_spec(spec); spec.loader.exec_module(value)
    return value


def generate(out):
    proof = json.loads((FIXTURE / "receipt.json").read_bytes())
    if proof.get("kind") != "AriaS004OriginalStepCompileProbe" or proof.get("status") != "Passed" or \
       proof.get("run_exit") != 0 or proof.get("gameplay_run") is not False or \
       "[S004OriginalAriaStepFixture] result=Passed scenario=6 adapter=Bypassed" not in (FIXTURE / "run.log").read_text():
        raise ValueError("Current original S004 planner fixture required")
    for name, digest in proof["source_pins"].items():
        if sha(name) != digest: raise ValueError("Original planner source changed: " + name)
    probe = module(BASE / "run_compile_probe.py", "s004_original_probe")
    lifecycle = module(BASE / "extract_ui_lifecycle_types.py", "s004_ui_lifecycle")
    out.mkdir(parents=True, exist_ok=False)
    sources = [probe.emit_data_only_planner(out)]
    for source in probe.SOURCES[:-1]:
        original = source.read_text(encoding="utf-8")
        if "UnityEngine." in original:
            destination = out / (source.stem + ".extern-alias.extracted.cs")
            destination.write_text("extern alias ActualCore;\n" + original.replace("UnityEngine.", "ActualCore::UnityEngine."))
            sources.append(destination)
        else: sources.append(source)
    ui = lifecycle.generate(ROOT, out)
    sources.append(out / lifecycle.OUTPUT)
    original_inspector = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073OriginalAriaStepDiagnostic.cs"
    text = original_inspector.read_text()
    changes = [
        ("SourceS073OriginalAriaStepDiagnostic", "SourceS004OriginalAriaStepDiagnostic"),
        ("S073OriginalAriaStepSameWorldDiagnostic", "S004OriginalAriaStepSameWorldDiagnostic"),
        ("view.SourceScenarioIndex != 3 || match.ScenarioIndex != 3", "view.SourceScenarioIndex != 6 || match.ScenarioIndex != 6 || !view.ExpandedSession || !view.ExpandedBaseAssault || !view.AirProfile"),
        ("OriginalS073LegacyStepPreconditionsNotMet", "OriginalS004ExpandedAirStepPreconditionsNotMet"),
    ]
    for old, new in changes:
        if text.count(old) != 1: raise ValueError("Inspector source anchor changed: " + old)
        text = text.replace(old, new)
    inspector = out / "SourceS004OriginalAriaStepDiagnostic.cs"; inspector.write_text(text); sources.append(inspector)
    fixture_source = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S073OriginalAriaStepSourceHostControls.cs"
    fixture_text = fixture_source.read_text()
    for old, new in (("S073OriginalAriaStepSourceHostControls", "S004OriginalAriaStepSourceHostControls"),
                     ("SourceS073OriginalAriaStepDiagnostic", "SourceS004OriginalAriaStepDiagnostic"),
                     ("WARLINE_S073_ORIGINAL_ARIA_STEP_PROBE", "WARLINE_S004_ORIGINAL_ARIA_STEP_PROBE"),
                     ("ScenarioIndex = 3", "ScenarioIndex = 6")):
        if old not in fixture_text: raise ValueError("Fixture source anchor changed: " + old)
        fixture_text = fixture_text.replace(old, new)
    fixture = out / "S004OriginalAriaStepSourceHostControls.cs"; fixture.write_text(fixture_text)
    all_sources = list(dict.fromkeys(sources))
    receipt = {"status": "Prepared", "kind": "S004OriginalPlannerSameWorldProbeOverlay", "fixture_only": False,
        "scope": "ExactOriginalGenericStep_RequiresActualUiOwnedTuple_NoSyntheticInput", "source_files": [str(p) for p in all_sources],
        "source_sha256": {str(p): sha(p) for p in all_sources}, "original_planner_input_sha256": proof["source_pins"],
        "ui_lifecycle_input_sha256": ui["source_pins"], "original_inspector_sha256": sha(original_inspector),
        "guard_fixture_source": str(fixture), "guard_fixture_source_sha256": sha(fixture), "guard_fixture_original_sha256": sha(fixture_source),
        "fixture_receipt_sha256": sha(FIXTURE / "receipt.json"), "fixture_run_sha256": sha(FIXTURE / "run.log"),
        "planner_executed": False, "native_readiness": False, "mission_ready": False}
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("--output", type=Path, required=True)
    print(json.dumps(generate(parser.parse_args().output.resolve())))
