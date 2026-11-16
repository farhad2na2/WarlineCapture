#!/usr/bin/env python3
"""Derive a labeled S005 source diagnostic from freshly captured inputs.

Historical receipts are never changed. The derived checkpoint accepts only
the four source changes already pinned by the current checked source build.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
CURRENT_BUILD = ROOT / "T Logs/SkirmishProduction/shared-view-profile-s002-build-20261105-T1/build.json"
APPROVED_EXTRA_CHANGES = set()
INPUT_OVERRIDES = {}
spec = importlib.util.spec_from_file_location("roster", HERE / "prepare_s005_roster_owner_experiment.py")
roster = importlib.util.module_from_spec(spec)
spec.loader.exec_module(roster)

def digest(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

def prepare(out):
    old_checkpoint = ROOT / "T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/source-pins-before.json"
    build_path = CURRENT_BUILD
    old = json.loads(old_checkpoint.read_text())
    build = json.loads(build_path.read_text())
    if build["status"] != "Passed" or build["compile_exit"] != 0:
        raise ValueError("Current source build must have passed")
    current = build["source_and_reference_pins"]
    changed = {}
    allowed = {
        str(ROOT / "Assets/Game/Scripts/Composition/SkirmishPresentationSystem.cs"),
        str(ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishPublicObjectiveProjection.cs"),
        str(ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/051_SourceConfigMaterializer.cs"),
        str(ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.cs"),
    }
    allowed.update(APPROVED_EXTRA_CHANGES)
    for group in ("build_sha256", "generated_source_sha256"):
        for path, old_hash in old[group].items():
            actual = digest(path)
            if actual == old_hash:
                continue
            if group != "build_sha256" or path not in allowed or current.get(path) != actual:
                raise ValueError("Unreviewed checkpoint drift: " + path)
            changed[path] = {"historical_sha256": old_hash, "current_build_sha256": actual}
            old[group][path] = actual
    if set(changed) != allowed:
        raise ValueError("Expected four current-build changes")
    # Recheck the entire current build, not only the four changed files.
    for path, value in current.items():
        if digest(path) != value:
            raise ValueError("Current build drift: " + path)
    roster.prepare(out)
    checkpoint = out / "derived-source-checkpoint.json"
    old["scope"] = "Derived current diagnostic checkpoint; historical capture epochs retained separately"
    old["historical_checkpoint_sha256"] = digest(old_checkpoint)
    old["current_build_sha256"] = digest(build_path)
    old["explicit_source_changes"] = changed
    checkpoint.write_text(json.dumps(old, indent=2) + "\n")
    pairs = {
        "T Logs/SkirmishProduction/s005-building-factory-capture-20261105-T2/full-factory-runway-definitions.json": "Logs/SkirmishProduction/s005-current-factory-capture-20261105-T2/full-factory-runway-definitions.json",
        "T Logs/SkirmishProduction/s005-configured-readmodel-capture-20261105-T1/configured-spawnable-readmodel.json": "Logs/SkirmishProduction/s005-current-readmodel-capture-20261105-T1/configured-spawnable-readmodel.json",
        "T Logs/SkirmishProduction/s005-prefab-object-reference-capture-20261105-T2/s005-prefab-object-references.json": "Logs/SkirmishProduction/s005-current-prefab-capture-20261105-T1/s005-prefab-object-references.json",
        "T Logs/SkirmishProduction/s005-prefab-object-projection-20261105-T2/projection.json": "T Logs/SkirmishProduction/s005-current-prefab-projection-20261105-T1/projection.json",
    }
    pairs.update(INPUT_OVERRIDES)
    for new in pairs.values():
        if not (ROOT / new).is_file():
            raise ValueError("Current captured input missing: " + new)
    helper = out / "helper.cs"
    helper.write_text(roster.base.once(helper.read_text(),
        "4d8c71bc75c7db22a3a3f569e0cb172bf9171ea295f00543f31cd82a9a3dd3c6", digest(ROOT / pairs[next(iter(pairs))])))
    runner = out / "run.py"
    text = runner.read_text()
    for before, after in pairs.items():
        text = text.replace(before, after)
    text = text.replace("T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/source-pins-before.json", str(checkpoint))
    text = text.replace("T Logs/SkirmishProduction/s004-current-epoch-s002-build-20261105-T1/build.json", str(build_path))
    marker = "    del inputs[str(PRIOR/'terrain-placement.exe')];inputs[str(exe)]=sha(exe)"
    swaps = "\n".join("    inputs.pop(str(ROOT/" + repr(a) + ")); inputs[str(ROOT/" + repr(b) + ")]=sha(ROOT/" + repr(b) + ")" for a,b in pairs.items())
    text = roster.base.once(text, marker, marker + "\n" + swaps)
    runner.write_text(text)
    prep = out / "preparation.json"
    doc = json.loads(prep.read_text())
    for path in (helper, runner, checkpoint, Path(__file__).resolve(), build_path):
        doc["source_pins"][str(path)] = digest(path)
    doc["fresh_capture_inputs"] = {str(ROOT / v): digest(ROOT / v) for v in pairs.values()}
    doc["historical_capture_sources_replaced"] = list(pairs.keys())
    doc["current_source_epoch"] = "ExplicitDerivedDiagnosticOnly"
    prep.write_text(json.dumps(doc, indent=2) + "\n")

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("output", type=Path)
    prepare(p.parse_args().output.resolve())
