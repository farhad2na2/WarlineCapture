#!/usr/bin/env python3
"""Prepare a source-only focus projection using Unity.Mathematics.

The profile angle has no current native readback. This is not native camera
parity, visual acceptance, or an admission to qualified mission execution.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / "T Logs/SkirmishProduction/s005-opening-view-owner-compile-20261105-T6"
CONTROL = ROOT / "T Logs/SkirmishProduction/s005-opening-view-owner-control-20261105-T1"
MATH = ROOT / "T Logs/SkirmishProduction/s005-source-camera-quaternion-comparison-20261105-T1"

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def once(source, old, new):
    if source.count(old) != 1:
        raise ValueError("exact source anchor required: " + old[:100])
    return source.replace(old, new, 1)

def prepare(out):
    out.mkdir(parents=True, exist_ok=False)
    original = BASE / "SourceMissionLogicalBindingProvider.cs"
    source = original.read_text()
    source = once(source, 'var forward=Quaternion.Euler(pitch,yaw,0)*Vector3.forward;',
        'var sourceRotation=Unity.Mathematics.quaternion.EulerZXY(Unity.Mathematics.math.radians(new Unity.Mathematics.float3(pitch,yaw,0)));'
        'var sourceForward=Unity.Mathematics.math.mul(sourceRotation,new Unity.Mathematics.float3(0,0,1));'
        'var forward=new Vector3(sourceForward.x,sourceForward.y,sourceForward.z);')
    (out / "SourceMissionLogicalBindingProvider.cs").write_text(source)
    control = (CONTROL / "control.cs").read_text()
    control = once(control, 'new Vector3(800,0,600)', 'new Vector3(800,4,600)')
    (out / "control.cs").write_text(control)
    args = [json.loads(line) for line in (CONTROL / "compile.rsp").read_text().splitlines() if line.strip()]
    for old, new in ((str(original), str(out / "SourceMissionLogicalBindingProvider.cs")),
                     (str(CONTROL / "control.cs"), str(out / "control.cs"))):
        if args.count(old) != 1:
            raise ValueError("exact compiler input required: " + old)
        args[args.index(old)] = new
    args = ["-out:" + str(out / "control.exe") if a.startswith("-out:") else a for a in args]
    (out / "compile.rsp").write_text("".join(json.dumps(a) + "\n" for a in args))
    inputs = [original, CONTROL / "control.cs", CONTROL / "compile.rsp", MATH / "run.log", Path(__file__).resolve()]
    (out / "preparation.json").write_text(json.dumps({"status":"PreparedExperimentalOnly",
        "source_pins":{str(p):sha(p) for p in inputs}, "profile_angle_native_parity":False,
        "native_camera_claim":False,"mission_ready":False}, indent=2) + "\n")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    prepare(parser.parse_args().output.resolve())
