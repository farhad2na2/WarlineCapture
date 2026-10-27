#!/usr/bin/env python3
"""Compile finite fixed-map parity controls against an existing qualified source build."""
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
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--binding", type=Path, required=True)
    parser.add_argument("--binding-sha256", required=True)
    parser.add_argument("--missing-path", type=Path, required=True)
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
    fixture = args.binding.resolve()
    if sha(fixture) != args.binding_sha256 or args.missing_path.exists():
        raise ValueError("Binding pin or missing path invalid")
    output = args.output.resolve()
    output.mkdir(parents=False, exist_ok=False)
    image = output / "SourceSimulation.exe"
    lines = [json.loads(line) for line in response.read_text().splitlines() if line]
    lines = ["-main:FixedMapParityControls" if x.startswith("-main:") else "-out:" + str(image) if x.startswith("-out:") else x for x in lines]
    lines.append(str(root / "FixedMapParityControls.cs"))
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
    run("controls", [selection["mono"], "--config", str(args.mono_config.resolve()), "--optimize=-float32", str(image), str(fixture), str(args.missing_path.resolve())], 120, env)
    marker = "[FixedMapParityControls] result=Passed checks=12 world=False"
    if marker not in (output / "controls.log").read_text():
        raise ValueError("Missing exact pass marker")
    verify_pins()
    print("[FixedMapPortable] result=Passed checks=12 world=False")

if __name__ == "__main__":
    main()
