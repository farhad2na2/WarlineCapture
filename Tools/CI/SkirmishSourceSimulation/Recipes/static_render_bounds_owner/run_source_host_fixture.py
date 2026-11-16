#!/usr/bin/env python3
"""Extract, compile, and execute the original static bounds owner in a disposable source host."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import time

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[4]
OWNER = REPO / "Assets/Game/Scripts/Rendering/RenderBoundsPresentation.cs"
SHIM = HERE / "SourceRenderBoundsShim.cs"
FIXTURE = HERE / "SourceStaticRenderBoundsOwnerFixture.cs"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def run(build_path: Path, output: Path) -> dict:
    build_path = build_path.resolve(strict=True)
    output = output.absolute()
    need(not output.exists(), "Fresh isolated output directory required")
    output.mkdir(parents=True)
    build = json.loads(build_path.read_text(encoding="utf-8"))
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
         "Passing checked source-host build receipt required")
    response = build["compiler_response"]
    response_path = Path(response["path"]).resolve(strict=True)
    need(sha(response_path) == response["sha256"], "Pinned compiler response changed")
    pins = build.get("source_and_reference_pins", {})
    owner_key = str(OWNER)
    stale_owner_capture = pins.get(owner_key)
    need(stale_owner_capture is not None and sha(OWNER) == stale_owner_capture,
         "Capture-pinned original static bounds owner changed or is missing")
    for path, expected in pins.items():
        need(sha(Path(path).resolve(strict=True)) == expected, "Pinned source/build input changed: " + path)

    generated_dir = output / "generated"
    generator = HERE / "generate_source_owner.py"
    generated = subprocess.run([sys.executable, str(generator), "--source", str(OWNER), "--output",
                                str(generated_dir)], capture_output=True, text=True, timeout=30)
    (output / "generation.log").write_text(generated.stdout + generated.stderr, encoding="utf-8")
    need(generated.returncode == 0, "Fail-closed owner extraction failed")
    generated_owner = generated_dir / "ExtractedOriginalStaticRenderBoundsOwner.cs"

    args = [json.loads(line) for line in response_path.read_text(encoding="utf-8").splitlines() if line.strip()]
    mains = [i for i, value in enumerate(args) if value == "-main:SourceSimulationProgram"]
    outputs = [i for i, value in enumerate(args) if value.startswith("-out:")]
    need(len(mains) == 1 and len(outputs) == 1, "Expected unique source-host main/output arguments")
    args[mains[0]] = "-main:SourceStaticRenderBoundsOwnerFixture"
    executable = output / "SourceStaticRenderBoundsOwnerFixture.exe"
    args[outputs[0]] = "-out:" + str(executable)
    args.extend(str(path.resolve(strict=True)) for path in (SHIM, generated_owner, FIXTURE))
    fixture_response = output / "fixture.rsp"
    fixture_response.write_text("\n".join(json.dumps(value) for value in args) + "\n", encoding="utf-8")

    pins_tooling = build["source_and_reference_pins"]
    dotnet = next((Path(path) for path in pins_tooling if Path(path).name == "dotnet"), None)
    compiler = next((Path(path) for path in pins_tooling if Path(path).name == "csc.dll"), None)
    need(dotnet is not None and compiler is not None, "Pinned Roslyn compiler missing")
    started = time.monotonic()
    compiled = subprocess.run([str(dotnet), str(compiler), "@" + str(fixture_response)], capture_output=True,
                              text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr, encoding="utf-8")
    need(compiled.returncode == 0, "Extracted owner fixture failed compilation")

    runtime = {key: Path(value).resolve(strict=True) for key, value in build["runtime_selection"].items()}
    mono_helper = runtime["contents"] / "Frameworks/libMonoPosixHelper.dylib"
    mono_native = runtime["mono"].parent.parent / "lib/libmono-native.dylib"
    need(mono_helper.is_file() and mono_native.is_file(), "Pinned Mono support files missing")
    dllmap = '<dllmap dll="MonoPosixHelper" target="' + str(mono_helper) + '" os="!windows"/>'
    dllmap += ''.join('<dllmap dll="' + name + '" target="' + str(mono_native) + '" os="!windows"/>'
                      for name in ("System.Native", "System.Net.Security.Native",
                                   "System.Security.Cryptography.Native.Apple"))
    config = output / "mono-config.xml"
    config.write_text("<configuration>" + dllmap + "</configuration>\n", encoding="utf-8")
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(REPO / "Library/ScriptAssemblies"), str(runtime["engine"]),
        str(runtime["newtonsoft"].parent), str(Path(build["placement_build"]).parent)])
    ran = subprocess.run([str(runtime["mono"]), "--config", str(config), str(executable)], env=env,
                         capture_output=True, text=True, timeout=60)
    (output / "run.log").write_text(ran.stdout + ran.stderr, encoding="utf-8")
    marker = "[SourceStaticRenderBoundsOwnerFixture] result=Passed case_count=9 included=2 excluded=7"
    need(ran.returncode == 0 and marker in ran.stdout, "Extracted owner fixture failed or marker missing")

    owner_sha = sha(OWNER)
    need(owner_sha == stale_owner_capture and owner_sha == json.loads((generated_dir / "extraction.json").read_text())["source_sha256"],
         "Owner source changed during extraction/compile")
    receipt = {
        "schema_version": 1,
        "kind": "SourceHostExtractedOriginalStaticRenderBoundsOwnerFixture",
        "status": "Passed",
        "build_receipt": str(build_path),
        "build_receipt_sha256": sha(build_path),
        "compiler_response": str(response_path),
        "compiler_response_sha256": response["sha256"],
        "baseline_capture_owner_sha256": stale_owner_capture,
        "current_owner_sha256": owner_sha,
        "baseline_owner_capture_stale": stale_owner_capture is not None and stale_owner_capture != owner_sha,
        "shim_sha256": sha(SHIM),
        "fixture_sha256": sha(FIXTURE),
        "generated_owner_sha256": sha(generated_owner),
        "compile_exit": compiled.returncode,
        "run_exit": ran.returncode,
        "case_count": 9,
        "included_count": 2,
        "excluded_count": 7,
        "order_scope": "Source-host EntityManager query/chunk traversal only; native Unity chunk ordering is not claimed",
        "source_scene_admission": False,
        "mission_readiness": False,
        "native_parity": False,
        "unity_run": False,
        "elapsed_seconds": round(time.monotonic() - started, 3),
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = run(args.build, args.output)
    print("[run_source_host_fixture] status=" + result["status"] +
          " cases=" + str(result["case_count"]) + " owner_capture_stale=" +
          str(result["baseline_owner_capture_stale"]).lower())
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
