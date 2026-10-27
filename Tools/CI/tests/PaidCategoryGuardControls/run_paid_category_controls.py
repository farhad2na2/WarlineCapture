#!/usr/bin/env python3
"""Compile finite catalog controls against an existing qualified source build."""
import argparse, hashlib, json, os, runpy, shutil, subprocess
from pathlib import Path

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--cli", type=Path, required=True)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--build-receipt", type=Path, required=True)
    parser.add_argument("--mono-config", type=Path, required=True)
    parser.add_argument("--initialization", type=Path, required=True)
    parser.add_argument("--initialization-sha256", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--candidate", action="store_true")
    args = parser.parse_args()
    root = Path(__file__).resolve().parent
    receipt = json.loads(args.build_receipt.read_text())
    if receipt.get("status") != "Passed":
        raise ValueError("Qualified build receipt must be Passed")
    namespace = runpy.run_path(str(args.cli.resolve()), run_name="qualified_test_bootstrap")
    runtime = namespace["owned"].require("skirmish_source_build").runtime_paths(args.repo.resolve())
    pins = receipt["source_and_reference_pins"]
    def verify_pins():
        for path, expected in pins.items():
            if not Path(path).is_file() or sha(path) != expected:
                raise ValueError("Build source/reference pin changed: " + path)
    verify_pins()
    for name in ("csc", "dotnet"):
        path = str(runtime[name])
        if sha(path) != pins[path]:
            raise ValueError("Compiler pin changed: " + path)
    response = Path(receipt["compiler_response"]["path"])
    if sha(response) != receipt["compiler_response"]["sha256"]:
        raise ValueError("Compiler response pin changed")
    readset = Path(receipt["transform_readset"]["path"])
    if sha(readset) != receipt["transform_readset"]["sha256"]:
        raise ValueError("Transform readset pin changed")
    fixture = args.initialization.resolve()
    fixture_sha = args.initialization_sha256.lower()
    if len(fixture_sha) != 64 or any(c not in "0123456789abcdef" for c in fixture_sha):
        raise ValueError("Initialization SHA-256 must be 64 hexadecimal characters")
    if sha(fixture) != fixture_sha:
        raise ValueError("Fixture pin changed")
    output = args.output.resolve()
    output.mkdir(parents=False, exist_ok=False)
    image = output / "SourceSimulation.exe"
    lines = [json.loads(line) for line in response.read_text().splitlines() if line]
    lines = ["-main:PaidCategoryGuardControls" if x.startswith("-main:") else "-out:" + str(image) if x.startswith("-out:") else x + ",PAID_OBSERVED_CATEGORY_CANDIDATE" if args.candidate and x.startswith("-define:") else x for x in lines]
    lines.append(str(root / "PaidCategoryGuardControls.cs"))
    rsp = output / "controls.rsp"
    rsp.write_text("\n".join(json.dumps(x) for x in lines) + "\n")
    def run(name, command, timeout, env=None):
        with (output / (name + ".log")).open("x") as log:
            result = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, timeout=timeout, env=env)
        if result.returncode:
            raise RuntimeError(name + " failed: " + str(result.returncode))
    run("compile", [str(runtime["dotnet"]), str(runtime["csc"]), "@" + str(rsp)], 120)
    shutil.copyfile(readset, str(image) + ".transform-readset.json")
    selection = receipt["runtime_selection"]
    env = os.environ.copy()
    env["MONO_PATH"] = os.pathsep.join([str(Path(receipt["repository_root"]) / "Library/ScriptAssemblies"), selection["engine"], str(Path(selection["newtonsoft"]).parent), str(Path(receipt["placement_build"]).parent)])
    result_path = output / "result.json"
    run("controls", [selection["mono"], "--config", str(args.mono_config.resolve()), "--optimize=-float32", str(image), str(fixture), fixture_sha, str(result_path)], 90, env)
    data = json.loads(result_path.read_text())
    count = 15 if args.candidate else 7
    marker = "[PaidCategoryGuardControls] result=Passed checks=" + str(count) + " world=False"
    if data.get("status") != "Passed" or data.get("world_created") is not False or len(data["cases"]) != count or marker not in (output / "controls.log").read_text():
        raise ValueError("Missing exact control results or pass marker")
    verify_pins()
    print("[PaidCategoryPortable] result=Passed checks=" + str(count) + " world=False")

if __name__ == "__main__":
    main()
