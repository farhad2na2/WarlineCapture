#!/usr/bin/env python3
"""Compile/run the shared UnitCombat/UnitHealth projection over a pinned source build."""
import argparse
import hashlib
import json
import os
import re
import subprocess
from pathlib import Path

REPO = Path("/Volumes/T7/Projects/WarlineCapture")
CONFIG = Path("Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Chr_Soldier_Male_02_Alt_04_Config.asset")
CONFIG_META = Path(str(CONFIG) + ".meta")
PREFAB = Path("Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab")
AUTHORING = Path("Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs")
CONFIG_MODELS = Path("Assets/Game/Scripts/Configs/GameplayConfigModels.cs")
HELPER = Path("Assets/Game/Scripts/Components/UnitCombatHealthAuthoringProjection.cs")
MANIFESTS = [Path("Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json"),
             Path("Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json")]
CONTROL = Path("Tools/CI/SkirmishSourceSimulation/Tests/UnitCombatHealthAuthoringProjectionControl.cs")
PASS = "[UnitCombatHealthAuthoringProjectionControl] result=Passed current=Alt04 boundaries=0,1,MaxValue negativeControls=3 scope=UnitCombat,UnitHealth"
FIELDS = {
    "CanAttack": ("bool", "canAttack", "ConfiguredCanAttack", "config != null ? config.CanAttack : canAttack"),
    "AllowAutoEngage": ("bool", "allowAutoEngage", "ConfiguredAllowAutoEngage", "config != null ? config.AllowAutoEngage : allowAutoEngage"),
    "AggroRangeCells": ("int", "aggroRangeCells", "ConfiguredAggroRangeCells", "Mathf.Max(0, config != null ? config.AggroRangeCells : aggroRangeCells)"),
    "ChaseBreakDistance": ("float", "chaseBreakDistance", "ConfiguredChaseBreakDistance", "Mathf.Max(0f, config != null ? config.ChaseBreakDistance : chaseBreakDistance)"),
    "MaxHealth": ("int", "maxHealth", "ConfiguredMaxHealth", "Mathf.Max(1, config != null ? config.MaxHealth : maxHealth)"),
}


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def read(path):
    return Path(path).read_text(encoding="utf-8")


def class_body(source, declaration):
    start = source.find(declaration)
    require(start >= 0, "missing class declaration " + declaration)
    opening = source.find("{", start + len(declaration))
    depth = 0
    for i in range(opening, len(source)):
        if source[i] == "{": depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0: return source[opening + 1:i]
    raise ValueError("unbalanced class " + declaration)


def method_body(source, signature):
    start = source.find(signature)
    require(start >= 0, "missing method " + signature)
    opening = source.find("{", start + len(signature))
    depth = 0
    for i in range(opening, len(source)):
        if source[i] == "{": depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0: return source[opening + 1:i]
    raise ValueError("unbalanced method " + signature)


def property_expression(source, name):
    matches = re.findall(rf"(?m)^\s*public\s+[^;\r\n]+?\s+{re.escape(name)}\s*=>\s*([^;\r\n]+);", source)
    require(len(matches) == 1, f"expected one property {name}, found {len(matches)}")
    return matches[0].strip()


def raw_value(config, field, kind):
    matches = re.findall(rf"(?m)^\s*{re.escape(field)}:\s*([^\r\n]+?)\s*$", config)
    require(len(matches) == 1, "expected exactly one raw config field " + field)
    value = matches[0]
    if kind == "bool":
        require(value in ("0", "1"), "invalid bool " + field)
        return value == "1"
    if kind == "int":
        require(re.fullmatch(r"-?\d+", value) is not None, "invalid integer " + field)
        return int(value)
    require(re.fullmatch(r"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?", value) is not None,
            "invalid float " + field)
    return float(value)


def csharp_literal(value, kind):
    if kind == "bool": return "1" if value else "0"
    if kind == "int": return str(value)
    return format(value, ".9g")


def verify_getters(repo):
    config_path, meta_path, prefab_path, authoring_path, model_path = [repo / p for p in
        (CONFIG, CONFIG_META, PREFAB, AUTHORING, CONFIG_MODELS)]
    cfg, meta, prefab, authoring, models = [read(p) for p in (config_path, meta_path, prefab_path, authoring_path, model_path)]
    guid = re.search(r"(?m)^guid: ([0-9a-f]{32})$", meta)
    require(guid and prefab.count("guid: " + guid.group(1)) == 1, "Alt04 prefab does not uniquely bind config GUID")
    cfg_class = class_body(models, "public class UnitGridAuthoringConfig : ScriptableObject")
    apply = method_body(authoring, "private void ApplyConfigIfAvailable()")
    values = {}
    expected_getters = {}
    for prop, (kind, field, author_getter, author_expr) in FIELDS.items():
        require(property_expression(authoring, author_getter) == author_expr,
                "original authoring getter changed: " + author_getter)
        require(property_expression(cfg_class, prop) == field,
                "Config property is no longer raw field passthrough: " + prop)
        assignment = f"{field} = config.{prop};"
        assignment_matches = re.findall(rf"(?m)^\s*{re.escape(assignment)}\s*$", apply)
        require(len(assignment_matches) == 1,
                "ApplyConfigIfAvailable copy changed: " + assignment)
        value = raw_value(cfg, field, kind)
        if kind != "bool":
            # The exact getter source above sets its clamp. Require this authored value
            # to lie inside that getter's unchanged output region; then raw == getter.
            threshold = re.search(r"Mathf\.Max\(([-+]?(?:\d+(?:\.\d*)?|\.\d+)),", author_expr)
            if threshold:
                require(value >= float(threshold.group(1)),
                        "Alt04 raw config would be changed by getter clamp: " + prop)
        values[prop] = value
        expected_getters[author_getter] = author_expr

    bake = method_body(authoring, "public override void Bake(UnitGridAuthoring authoring)")
    compact = re.sub(r"\s+", "", bake)
    exact_calls = [
        "UnitCombatHealthAuthoringProjection.FromConfiguredCombatValues(authoring.ConfiguredAggroRangeCells,authoring.ConfiguredChaseBreakDistance,authoring.ConfiguredCanAttack,authoring.ConfiguredAllowAutoEngage)",
        "AddComponent(entity,UnitCombatHealthAuthoringProjection.FromConfiguredMaxHealth(authoring.ConfiguredMaxHealth));",
    ]
    require(all(call in compact for call in exact_calls), "current Baker no longer uses shared projection at original component sites")
    require(bake.count("FromConfiguredCombatValues(") == 1 and bake.count("FromConfiguredMaxHealth(") == 1,
            "Baker shared projection call must be unique")
    # Negative controls test fail-closed source extraction and ownership binding.
    duplicate = authoring + "\npublic int ConfiguredAggroRangeCells => 99;\n"
    duplicate_rejected = False
    try: property_expression(duplicate, "ConfiguredAggroRangeCells")
    except ValueError: duplicate_rejected = True
    exact_call = "UnitCombatHealthAuthoringProjection.FromConfiguredCombatValues(authoring.ConfiguredAggroRangeCells,authoring.ConfiguredChaseBreakDistance,authoring.ConfiguredCanAttack,authoring.ConfiguredAllowAutoEngage)"
    mutated_bake = compact.replace("authoring.ConfiguredAggroRangeCells", "1", 1)
    drift_rejected = exact_call in compact and exact_call not in mutated_bake
    require(duplicate_rejected and drift_rejected, "source-binding negative controls failed")
    pins = {str(p): sha(p) for p in (config_path, meta_path, prefab_path, authoring_path, model_path,
                                      repo / HELPER, *[repo / p for p in MANIFESTS], repo / CONTROL)}
    return values, pins, duplicate_rejected, drift_rejected


def run(build_dir, output):
    repo = REPO.resolve(strict=True)
    build_dir, output = build_dir.resolve(strict=True), output.resolve()
    require(build_dir.is_relative_to(repo) and output.is_relative_to(repo) and not output.exists(),
            "explicit build and fresh output must remain within repository")
    values, input_pins, duplicate_rejected, drift_rejected = verify_getters(repo)
    build_path, response, exe = build_dir / "build.json", build_dir / "compile.rsp", build_dir / "SourceSimulation.exe"
    build = json.loads(read(build_path))
    source_pins = build.get("source_and_reference_pins", {})
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("compile_exit") == 0 and source_pins,
            "passed fresh source-host build with exact pin map required")
    drifts = [p for p, digest in source_pins.items() if not Path(p).is_file() or sha(p) != digest]
    require(not drifts, "source-build pin drift: " + ", ".join(drifts[:4]))
    helper_path = repo / HELPER
    response_lines = response.read_text(encoding="utf-8").splitlines()
    require(sum(1 for line in response_lines if str(helper_path) in line) == 1 and
            source_pins.get(str(helper_path)) == sha(helper_path),
            "fresh source build must compile and pin the shared helper exactly once")
    selected_manifest = Path("Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json") \
        if "s003" in build_dir.name.lower() else Path("Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json")
    for rel in (AUTHORING, HELPER, selected_manifest):
        path = repo / rel
        require(source_pins.get(str(path)) == sha(path), "build source pin does not match current file " + str(rel))

    runtime = build.get("runtime_selection", {})
    compiler = Path(next(p for p in source_pins if p.endswith("/Roslyn/bincore/csc.dll")))
    dotnet = Path(next(p for p in source_pins if Path(p).name == "dotnet"))
    mono, engine, newtonsoft = Path(runtime.get("mono", "")), Path(runtime.get("engine", "")), Path(runtime.get("newtonsoft", ""))
    require(all(p.is_file() for p in (compiler, dotnet, mono, newtonsoft)) and engine.is_dir(),
            "pinned compiler/runtime dependencies missing")
    output.mkdir(parents=True)
    out_exe = output / "UnitCombatHealthAuthoringProjectionControl.exe"
    test_source = repo / CONTROL
    rewritten, out_seen, main_seen = [], False, False
    for line in response_lines:
        raw = line.strip().strip('"')
        if raw.startswith("-out:"):
            rewritten.append('"-out:' + str(out_exe) + '"'); out_seen = True
        elif raw == "-main:SourceSimulationProgram":
            rewritten.append('"-main:UnitCombatHealthAuthoringProjectionControl"'); main_seen = True
        else:
            rewritten.append(line)
    require(out_seen and main_seen, "source response output/main switches missing")
    rewritten.append('"' + str(test_source) + '"')
    test_response = output / "control.rsp"
    test_response.write_text("\n".join(rewritten) + "\n", encoding="utf-8")
    compile_run = subprocess.run([str(dotnet), str(compiler), "@" + str(test_response)], cwd=repo,
                                 text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / "compile.log").write_text(compile_run.stdout, encoding="utf-8")
    require(compile_run.returncode == 0 and out_exe.is_file(), "focused compiled control failed; compile.log retained")
    # Only the Baker's required five getter values are passed to the compiled control.
    focused_args = [str(values["AggroRangeCells"]), format(values["ChaseBreakDistance"], ".9g"),
                    "1" if values["CanAttack"] else "0", "1" if values["AllowAutoEngage"] else "0",
                    str(values["MaxHealth"])]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join((str(build_dir / "placement"), str(engine), str(newtonsoft.parent),
                                        str(repo / "Library/ScriptAssemblies"), str(output)))
    execution = subprocess.run([str(mono), str(out_exe), *focused_args], cwd=repo, env=env,
                               text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    (output / "run.log").write_text(execution.stdout, encoding="utf-8")
    require(execution.returncode == 0 and PASS in execution.stdout.splitlines(),
            "focused control failed or pass marker missing; run.log retained")
    drifts_after = [p for p, digest in source_pins.items() if not Path(p).is_file() or sha(p) != digest]
    require(not drifts_after, "source build drift after control run: " + ", ".join(drifts_after[:4]))
    receipt = {
        "kind": "UnitCombatHealthAuthoringProjectionControlReceipt", "status": "Passed",
        "source_build": str(build_path), "source_build_sha256": sha(build_path),
        "source_build_executable_sha256": sha(exe), "source_closure_count": len(source_pins),
        "source_inputs": input_pins, "source_pins_after_run": len(source_pins),
        "component_inputs_from_current_Config_getters": values,
        "scope": "Shared data-only projection called by native Baker and compiled source control; UnitCombat/UnitHealth only.",
        "negative_controls": ["duplicate authoring getter rejected", "Baker call drift rejected",
                              "wrong expected aggro/auto-engage/health rejected at runtime"],
        "unsupported": ["source-host importer integration", "faction resolved model-child branch", "surface height", "UnitAttack", "complete prefab closure", "mission admission"],
        "compile_exit": compile_run.returncode, "run_exit": execution.returncode, "pass_marker": PASS,
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--build-dir", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        print(json.dumps(run(args.build_dir, args.output), indent=2))
        return 0
    except Exception as exc:
        print(f"[UnitCombatHealthAuthoringProjectionControl] result=Failed error={exc}")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
