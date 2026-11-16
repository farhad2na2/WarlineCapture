#!/usr/bin/env python3
"""Compile and run pinned camera adapter/function mismatch controls."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
CONTROL = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/SourceCameraFunctionReferenceControls.cs'
ADAPTER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceNumericCameraEngineAdapterContract.cs'
PROFILE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceDeclaredEngineeringCameraProfile.cs'
PASS = '[SourceCameraFunctionReferenceControls] result=Passed positive=1 negative=3 nativeCameraContextQualified=False missionReady=False'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(value, message):
    if not value:
        raise ValueError(message)


def verify_pins(pins):
    need(isinstance(pins, dict) and pins, 'Checked source pin map required')
    for raw, expected in pins.items():
        path = Path(raw)
        if not path.is_file() or path.is_symlink() or sha(path) != expected:
            raise ValueError('Current build input drift: ' + raw)


def run(build_dir, output):
    build_dir = Path(build_dir).resolve(strict=True)
    output = Path(output).resolve()
    need(build_dir.is_relative_to(ROOT) and output.is_relative_to(ROOT / 'T Logs') and not output.exists(),
         'Checked repository build and fresh T Logs output required')
    output.mkdir(parents=True)
    receipt_path = output / 'receipt.json'
    receipt = {'kind': 'SourceCameraFunctionReferenceControlReceipt', 'status': 'Failed',
               'build_dir': str(build_dir), 'control_source': str(CONTROL),
               'unity_launched': False, 'world_allocated': False, 'mission_ready': False}
    try:
        build_path = build_dir / 'build.json'
        build_raw = build_path.read_bytes()
        build = json.loads(build_raw)
        need(build.get('kind') == 'SkirmishSourceSimulationBuild' and build.get('status') == 'Passed' and
             build.get('compile_exit') == 0 and Path(build.get('repository_root', '')).resolve() == ROOT,
             'Passed current repository source-host build required')
        pins = build.get('source_and_reference_pins', {})
        verify_pins(pins)
        need(pins.get(str(ADAPTER)) == sha(ADAPTER) and pins.get(str(PROFILE)) == sha(PROFILE),
             'Checked S026 build must pin exact current camera source adapters')
        need(pins.get(str(CONTROL)) is None, 'Isolated control source must remain outside production closure')
        response_info = build.get('compiler_response', {})
        response = Path(response_info.get('path', '')).resolve(strict=True)
        runtime = build.get('runtime_selection', {})
        mono = Path(runtime.get('mono', '')).resolve(strict=True)
        dotnet = next(Path(p).resolve(strict=True) for p in pins if Path(p).name == 'dotnet')
        compiler = next(Path(p).resolve(strict=True) for p in pins if p.endswith('/Roslyn/bincore/csc.dll'))
        engine = Path(runtime.get('engine', '')).resolve(strict=True)
        newtonsoft = Path(runtime.get('newtonsoft', '')).resolve(strict=True)
        need(response.is_relative_to(build_dir) and sha(response) == response_info.get('sha256') and
             mono.is_file() and dotnet.is_file() and compiler.is_file() and engine.is_dir() and newtonsoft.is_file(),
             'Pinned response/compiler/Mono dependencies required')
        args = [json.loads(line) for line in response.read_text(encoding='utf-8').splitlines() if line.strip()]
        executable = output / 'SourceCameraFunctionReferenceControls.exe'
        rewritten, out_count, main_count = [], 0, 0
        for arg in args:
            if arg.startswith('-out:'):
                rewritten.append('-out:' + str(executable)); out_count += 1
            elif arg == '-main:SourceSimulationProgram':
                rewritten.append('-main:Game.Runtime.SourceCameraFunctionReferenceControls'); main_count += 1
            else:
                rewritten.append(arg)
        need(out_count == 1 and main_count == 1, 'Checked response entry point/output did not match')
        rewritten.append(str(CONTROL))
        control_response = output / 'control.rsp'
        control_response.write_text(''.join(json.dumps(arg) + '\n' for arg in rewritten), encoding='utf-8')
        compiled = subprocess.run([str(dotnet), str(compiler), '@' + str(control_response)], cwd=ROOT,
                                  text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=180)
        (output / 'compile.log').write_text(compiled.stdout, encoding='utf-8')
        need(compiled.returncode == 0 and executable.is_file(), 'Camera function control compile failed')
        ref_dirs = {str(build_dir), str(engine), str(newtonsoft.parent), str(output)}
        for arg in args:
            if arg.startswith('-r:') and Path(arg[3:]).is_file():
                ref_dirs.add(str(Path(arg[3:]).resolve().parent))
        env = {**os.environ, 'MONO_PATH': os.pathsep.join(sorted(ref_dirs))}
        executed = subprocess.run([str(mono), str(executable), str(ROOT)], cwd=ROOT, env=env,
                                  text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=180)
        (output / 'run.log').write_text(executed.stdout, encoding='utf-8')
        need(executed.returncode == 0 and executed.stdout.splitlines().count(PASS) == 1,
             'Exact positive/mismatch camera control markers required')
        verify_pins(pins)
        need(build_path.read_bytes() == build_raw, 'Checked source build receipt changed during controls')
        receipt.update({'status': 'Passed', 'source_build_sha256': hashlib.sha256(build_raw).hexdigest(),
                        'source_pin_count': len(pins), 'adapter_source_sha256': sha(ADAPTER),
                        'profile_source_sha256': sha(PROFILE), 'control_source_sha256': sha(CONTROL),
                        'compile_log_sha256': sha(output / 'compile.log'), 'run_log_sha256': sha(output / 'run.log'),
                        'negative_count': 3, 'source_pins_rechecked_after': True,
                        'world_allocated': False, 'native_camera_context_qualified': False, 'mission_ready': False})
        print(executed.stdout, end='')
    except Exception as exc:
        receipt['failure'] = str(exc)
        receipt_path.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
        raise
    receipt_path.write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(receipt, indent=2, sort_keys=True))
    print(PASS)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build-dir', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    run(args.build_dir, args.output)


if __name__ == '__main__':
    main()
