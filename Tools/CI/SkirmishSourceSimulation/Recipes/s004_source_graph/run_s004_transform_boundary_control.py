#!/usr/bin/env python3
"""Run transform boundary negatives against the exact compiled S004 probe sources."""
import argparse, hashlib, json, os, shlex, subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
HERE = Path(__file__).resolve().parent
HARNESS = HERE / "S004TransformBoundaryControl.cs"
SETUP = ROOT / "T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json"
SETUP_SHA = "237062c97d534052418e00f9897c02f060034bf271509262c91c3dc510678e87"
MARKER = "[S004TransformBoundaryControl] result=Passed readset=1 setup=1 negatives=3 world=0 native=0 readiness=0"

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def main():
    parser = argparse.ArgumentParser(); parser.add_argument("--probe", type=Path, required=True); parser.add_argument("--build", type=Path, required=True); parser.add_argument("--output", type=Path, required=True); args = parser.parse_args()
    probe = args.probe.resolve(strict=True); buildpath = args.build.resolve(strict=True); out = args.output.resolve(); out.mkdir(parents=True, exist_ok=False)
    receipt = json.loads((probe / "receipt.json").read_bytes()); build = json.loads(buildpath.read_bytes())
    if receipt.get("mission_ready") is not False or build.get("status") != "Passed" or sha(SETUP) != SETUP_SHA: raise ValueError("Exact source-only probe and setup required")
    source_readset = probe / "S004BundleDrivenStartup.exe.transform-readset.json"; readset = json.loads(source_readset.read_bytes()); pins = readset["source_pins"]
    for path, expected in pins.items():
        if sha(path) != expected: raise ValueError("Changed probe source: " + path)
    args_rsp = [shlex.split(line)[0] for line in (probe / "compile.rsp").read_text().splitlines() if line.strip()]
    exe = out / "S004TransformBoundaryControl.exe"; mains = [i for i,v in enumerate(args_rsp) if v.startswith("-main:")]; outputs = [i for i,v in enumerate(args_rsp) if v.startswith("-out:")]
    if len(mains) != 1 or len(outputs) != 1: raise ValueError("Exact compiler entry points required")
    args_rsp[mains[0]] = "-main:S004TransformBoundaryControl"; args_rsp[outputs[0]] = "-out:" + str(exe); args_rsp.append(str(HARNESS)); rsp = out / "compile.rsp"; rsp.write_text("\n".join(json.dumps(v) for v in args_rsp) + "\n")
    buildpins = build["source_and_reference_pins"]; dotnet = next(p for p in buildpins if Path(p).name == "dotnet"); csc = next(p for p in buildpins if p.endswith("/Roslyn/bincore/csc.dll"))
    compile_result = subprocess.run([dotnet, csc, "@" + str(rsp)], capture_output=True, text=True, timeout=300); (out / "compile.log").write_text(compile_result.stdout + compile_result.stderr)
    if compile_result.returncode: raise RuntimeError("Source control compile failed")
    runtime = build["runtime_selection"]; env = os.environ.copy(); env["MONO_PATH"] = os.pathsep.join((str(buildpath.parent / "placement"), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(out)))
    mono_config = ROOT / "T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/mono-config.xml"
    run = subprocess.run([runtime["mono"], "--config", str(mono_config), str(exe), str(source_readset), str(SETUP), SETUP_SHA, str(out / "tampered-readset.json")], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300); (out / "run.log").write_text(run.stdout + run.stderr)
    if run.returncode or MARKER not in run.stdout: raise RuntimeError("Source control failed or marker missing")
    for path, expected in pins.items():
        if sha(path) != expected: raise ValueError("Probe source changed during control: " + path)
    result = {"status":"PassedSourceOnlyBoundaryControl","readset_sha256":sha(source_readset),"source_pins_checked":len(pins),"positive_checks":2,"negative_checks":3,"world_allocated":False,"native_parity":False,"mission_ready":False}
    (out / "receipt.json").write_text(json.dumps(result, indent=2) + "\n"); print(json.dumps(result))

if __name__ == "__main__": main()
