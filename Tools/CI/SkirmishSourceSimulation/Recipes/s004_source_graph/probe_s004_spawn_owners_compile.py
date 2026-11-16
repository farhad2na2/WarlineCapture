#!/usr/bin/env python3
"""Opt-in compile-only binding probe for the original S004 spawn owners.

This extracts the current owner method bodies verbatim and compiles them against
an existing source-host response. It never creates or runs an ECS world.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
from pathlib import Path

REPO = Path(__file__).resolve().parents[5]
SPAWN = REPO / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs"
SESSION = REPO / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishSessionInitializationSystem.cs"
LAUNCH = REPO / "Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchProjection.cs"
REGISTRY_CONFIG = REPO / "Assets/Game/Scripts/Configs/UnitPrefabRegistryAuthoringConfig.cs"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise RuntimeError(message)


def code_mask(source: str) -> str:
    """Blank comments and literals, preserving offsets and braces."""
    out = list(source)
    i = 0
    state = "code"
    while i < len(source):
        c = source[i]
        n = source[i + 1] if i + 1 < len(source) else ""
        if state == "code":
            if c == "/" and n in ("/", "*"):
                state = "line" if n == "/" else "block"
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c == "@" and n == '"':
                state = "verbatim"
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c in ('"', "'"):
                state = "string" if c == '"' else "char"
                out[i] = " "
                i += 1
                continue
        elif state == "line":
            if c == "\n":
                state = "code"
            else:
                out[i] = " "
        elif state == "block":
            if c == "*" and n == "/":
                out[i] = out[i + 1] = " "
                i += 2
                state = "code"
                continue
            if c != "\n":
                out[i] = " "
        elif state == "verbatim":
            if c == '"' and n == '"':
                out[i] = out[i + 1] = " "
                i += 2
                continue
            if c == '"':
                state = "code"
            if c != "\n":
                out[i] = " "
        else:
            if c == "\\":
                out[i] = " "
                i += 1
            elif c == ('"' if state == "string" else "'"):
                state = "code"
            if i < len(source) and source[i] != "\n":
                out[i] = " "
        i += 1
    need(state in ("code", "line"), "Unclosed lexical value while extracting owner source")
    return "".join(out)


def extract_method(source: str, name: str) -> str:
    masked = code_mask(source)
    pattern = re.compile(
        r"\b(?:public|private|internal|protected)\s+"
        r"(?:(?:static|readonly|unsafe|virtual|override|async|sealed|partial)\s+)*"
        r"[^\n{};=]+?\b" + re.escape(name) + r"\s*\("
    )
    hits = list(pattern.finditer(masked))
    need(len(hits) == 1, f"Expected one current owner method {name}; found {len(hits)}")
    start = hits[0].start()
    paren = masked.find("(", hits[0].start())
    depth = 0
    cursor = paren
    while cursor < len(masked):
        if masked[cursor] == "(":
            depth += 1
        elif masked[cursor] == ")":
            depth -= 1
            if depth == 0:
                break
        cursor += 1
    need(depth == 0, f"Unclosed signature for {name}")
    brace = masked.find("{", cursor)
    need(brace >= 0, f"Method body missing for {name}")
    depth = 1
    end = brace + 1
    while end < len(masked) and depth:
        if masked[end] == "{":
            depth += 1
        elif masked[end] == "}":
            depth -= 1
        end += 1
    need(depth == 0, f"Unclosed method body for {name}")
    return source[start:end]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-receipt", type=Path, required=True)
    parser.add_argument("--compiled-setup", type=Path, required=True)
    parser.add_argument("--exclude-launch-owner", action="store_true",
                        help="Probe exact spawn/session owners without the separate managed launch registry path")
    parser.add_argument("--session-only", action="store_true",
                        help="Probe the exact session initializer independently of launch and spawning")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.session_only:
        args.exclude_launch_owner = True
    build_path = args.build_receipt.resolve(strict=True)
    setup_path = args.compiled_setup.resolve(strict=True)
    build = json.loads(build_path.read_text())
    setup = json.loads(setup_path.read_text())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
         "A passed source-host build receipt is required")
    need(setup.get("kind") == "S004StandardCompiledSetupSourceExport" and setup.get("status") == "Passed",
         "A passed original S004 Standard compiled setup export is required")
    need(setup.get("identity", {}).get("catalog_id") == "S004" and
         setup.get("identity", {}).get("size_id") == "Standard" and
         setup.get("identity", {}).get("difficulty_id") == "Regular",
         "Expected exact S004 Regular/Standard compiled input")

    pins = build.get("source_and_reference_pins", {})
    need(isinstance(pins, dict) and pins, "Source-host input pins are missing")
    need(not args.output.exists(), "Fresh isolated output directory required")
    args.output.mkdir(parents=True)
    pin_mismatches = []
    for raw_path, expected in pins.items():
        path = Path(raw_path)
        if not path.is_file():
            pin_mismatches.append({"path": raw_path, "expected": expected, "actual": "missing"})
        else:
            actual = sha(path)
            if actual != expected:
                pin_mismatches.append({"path": raw_path, "expected": expected, "actual": actual})
    response_info = build.get("compiler_response", {})
    response = Path(response_info.get("path", "")).resolve(strict=True)
    if sha(response) != response_info.get("sha256"):
        pin_mismatches.append({"path": str(response), "expected": response_info.get("sha256"),
                               "actual": sha(response)})
    if pin_mismatches:
        log = "Source-host build pins do not match current inputs; compilation not attempted.\n" + "\n".join(
            f"{item['path']}: expected={item['expected']} actual={item['actual']}" for item in pin_mismatches)
        log_path = args.output / "preflight.log"
        log_path.write_text(log + "\n")
        receipt = {
            "kind": "S004OriginalSpawnOwnersCompileProbe", "status": "InputRejected",
            "scope": "No owner compile or runtime invocation; stale source-host response rejected.",
            "build_receipt": str(build_path), "build_sha256": sha(build_path),
            "compiled_setup": str(setup_path), "compiled_setup_sha256": sha(setup_path),
            "compiler_response_path": str(response), "pin_mismatches": pin_mismatches,
            "preflight_log": str(log_path), "preflight_log_sha256": sha(log_path),
            "runtime_invoked": False, "unity_invoked": False, "world_initialized": False,
            "spawn_complete": False, "physical_source_fit_verified": False, "mission_readiness": False,
        }
        (args.output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
        print("[S004OriginalSpawnOwnersCompileProbe] status=InputRejected pin_mismatches=" + str(len(pin_mismatches)))
        return 1

    inputs = [SPAWN, SESSION] if args.exclude_launch_owner else [SPAWN, SESSION, LAUNCH, REGISTRY_CONFIG]
    for path in inputs:
        need(str(path) in pins and sha(path) == pins[str(path)],
             "Original owner source is not part of this exact source-host build: " + str(path))
    owner_fragments = {
        "spawn": ["MapAndRegistryReady", "TrySpawn", "TrySpawnLedgers"],
        "session": ["OnUpdate", "TryInitialize", "ProjectRoles", "Fail"] if args.exclude_launch_owner else ["OnUpdate"],
    }
    if not args.exclude_launch_owner:
        owner_fragments["launch"] = ["TryQueue"]
    if args.session_only:
        del owner_fragments["spawn"]
    source_by_key = {"spawn": SPAWN, "session": SESSION, "launch": LAUNCH}
    extracted = []
    extraction_index = {}
    for key, names in owner_fragments.items():
        source = source_by_key[key].read_text()
        for name in names:
            fragment = extract_method(source, name)
            extracted.append((key, name, fragment))
            extraction_index[f"{key}.{name}"] = hashlib.sha256(fragment.encode()).hexdigest()
    # Method bodies are retained verbatim as a compile-only source unit. The outer
    # declaration provides no state, fake components, or alternate behavior.
    probe_source = args.output / "S004OriginalOwnerFragments.cs"
    fragments = {key: "\n".join(fragment for found_key, _, fragment in extracted if found_key == key)
                 for key in owner_fragments}
    probe_source.write_text(
        "using System;\nusing System.Collections.Generic;\nusing Game.Components;\n"
        "using Game.Configs;\nusing Game.Skirmish.Contracts;\nusing Unity.Entities;\n"
        "using Unity.Collections;\nusing UnityEngine;\n"
        "namespace Game.Runtime {\npublic partial struct SkirmishScenarioSpawnSystem {\n"
        + fragments.get("spawn", "") + "\n}\n"
        "public partial struct SkirmishSessionInitializationSystem {\n" + fragments["session"] + "\n}\n}\n"
        + ("namespace Game.Composition { public static class SkirmishExpandedLaunchProjection {\n"
           + fragments["launch"] + "\n}\n}\n" if not args.exclude_launch_owner else "")
    )
    raw_args = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    outputs = [i for i, value in enumerate(raw_args) if value.startswith("-out:")]
    targets = [i for i, value in enumerate(raw_args) if value == "-target:exe"]
    mains = [i for i, value in enumerate(raw_args) if value.startswith("-main:")]
    need(len(outputs) == 1 and len(targets) == 1 and len(mains) == 1,
         "Expected unique exe target, output and entry point in host response")
    raw_args[outputs[0]] = "-out:" + str(args.output / "S004OwnerCompileProbe.dll")
    raw_args[targets[0]] = "-target:library"
    raw_args.pop(mains[0])
    raw_args.append(str(probe_source))
    # The launch owner's exact serialized registry type is a data-only compile
    # dependency; include its original declaration without fabricating a registry.
    if not args.exclude_launch_owner:
        raw_args.append(str(REGISTRY_CONFIG))
    rsp = args.output / "compile.rsp"
    rsp.write_text("\n".join(json.dumps(value) for value in raw_args) + "\n")
    dotnet = next((Path(path) for path in pins if Path(path).name == "dotnet"), None)
    csc = next((Path(path) for path in pins if Path(path).name == "csc.dll"), None)
    need(dotnet is not None and csc is not None, "Pinned dotnet/Roslyn compiler missing")
    compiled = subprocess.run([str(dotnet), str(csc), "@" + str(rsp)], capture_output=True,
                              text=True, timeout=180)
    compile_log = compiled.stdout + compiled.stderr
    (args.output / "compile.log").write_text(compile_log)
    first_compile_error = next((line.strip() for line in compile_log.splitlines()
                                if re.search(r"\berror CS\d+:", line)), None)
    first_missing_domain = None
    if first_compile_error:
        missing_type = re.search(r"error CS0246: The type or namespace name '([^']+)'", first_compile_error)
        first_missing_domain = missing_type.group(1) if missing_type else first_compile_error
    compile_errors = [line.strip() for line in compile_log.splitlines() if re.search(r"\berror CS\d+:", line)]
    missing_domains = sorted({
        match.group(1) for line in compile_errors
        for match in [re.search(r"error CS0246: The type or namespace name '([^']+)'", line)] if match
    })

    compiled_setup = setup.get("compiled_setup", {})
    receipt = {
        "kind": "S004OriginalSpawnOwnersCompileProbe",
        "status": "Passed" if compiled.returncode == 0 else "CompileFailed",
        "scope": "Original owner extraction and source-host compile only; no runtime invocation or mission claim.",
        "build_receipt": str(build_path), "build_sha256": sha(build_path),
        "compiled_setup": str(setup_path), "compiled_setup_sha256": sha(setup_path),
        "compiler_response_sha256": response_info["sha256"],
        "source_pins": {str(path): sha(path) for path in inputs},
        "extracted_original_methods": extraction_index,
        "source_bodies_copied_verbatim": True,
        "launch_owner_excluded": args.exclude_launch_owner,
        "spawn_owner_excluded": args.session_only,
        "compile_exit": compiled.returncode,
        "compile_log": str(args.output / "compile.log"),
        "compile_log_sha256": sha(args.output / "compile.log"),
        "runtime_invoked": False, "unity_invoked": False,
        "world_initialized": False, "spawn_complete": False,
        "first_compile_error": first_compile_error,
        "first_missing_domain": first_missing_domain,
        "compile_errors": compile_errors,
        "missing_domains": missing_domains,
        "runtime_input_boundary": "Compiled setup JSON contains no EntityManager/world or session entities; runtime owners were not invoked.",
        "physical_source_fit_verified": False, "mission_readiness": False,
    }
    (args.output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print("[S004OriginalSpawnOwnersCompileProbe] status=" + receipt["status"] +
          " compile_exit=" + str(compiled.returncode) + " first_missing_domain=" + str(first_missing_domain))
    if compiled.returncode:
        print(compile_log)
    return 0 if compiled.returncode == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
