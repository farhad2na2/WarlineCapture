"""Build and replay the bounded retained S002 causal prefix without starting Unity."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import re
import stat
import subprocess
import sys
import time

sys.dont_write_bytecode = True
from skirmish_causal_io import read, parse


def digest(path, limit=512*1024*1024):
    """Stream bounded source hashes; large captured map assets are never parsed."""
    path = Path(path).absolute()
    for node in (path, *path.parents):
        need(not stat.S_ISLNK(node.lstat().st_mode), 'Hash symlink/ancestor rejected')
    before = path.lstat()
    need(stat.S_ISREG(before.st_mode) and 0 < before.st_size <= limit, 'Positive bounded regular hash input required')
    identity = lambda s: (s.st_dev, s.st_ino, s.st_size, s.st_mtime_ns, s.st_ctime_ns)
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    try:
        need(identity(os.fstat(fd)) == identity(before), 'Hash descriptor identity changed')
        result, remaining = hashlib.sha256(), before.st_size
        while remaining:
            data = os.read(fd, min(65536, remaining))
            need(data, 'Hash input truncated')
            result.update(data)
            remaining -= len(data)
        need(not os.read(fd, 1), 'Hash input grew')
        need(identity(os.fstat(fd)) == identity(before) == identity(path.lstat()), 'Hash input changed')
        return result.hexdigest()
    finally:
        os.close(fd)


def need(ok, message):
    if not ok:
        raise ValueError(message)


def save(path, value):
    path.write_text(json.dumps(value, indent=2, allow_nan=False)+'\n')


def output_path(project, output):
    project, output = Path(project).resolve(strict=True), Path(output).absolute()
    # Resolve ancestors before creating anything; forbid imported/configuration/Git outputs.
    resolved = output.resolve()
    for name in ('Assets', 'Packages', 'ProjectSettings', '.git'):
        need(not resolved.is_relative_to(project/name), 'Output must be outside imported/configuration/Git trees')
    for node in (output, *output.parents):
        need(not node.is_symlink(), 'Output symlink/ancestor rejected')
    return output


def unique_output(project, output):
    output = output_path(project, output)
    output.mkdir(parents=True, exist_ok=False)
    return output


def config_pins(path):
    raw = read(path, 4*1024*1024)
    config = parse(raw)
    need(config.get('no_dotnet_fallback') is True and config.get('copied_dlls') == [],
         'Installed Mono/engine modules in place required')
    pins = config.get('pins')
    need(type(pins) is dict and 32 <= len(pins) <= 256, 'Complete bounded worker pin closure required')
    for name, pin in pins.items():
        need(Path(name).is_absolute() and type(pin) is str and re.fullmatch('[a-f0-9]{64}', pin), 'Exact absolute source/runtime pins required')
        need(digest(name) == pin, 'Worker source/runtime pin differs: '+name)
    for key in ('mono_path', 'worker_path', 'newtonsoft_path'):
        need(config.get(key) in pins, 'Pinned worker executable/module missing: '+key)
    engine = Path(config['engine_path'])
    for name in ('UnityEngine.CoreModule.dll', 'UnityEngine.MathematicsModule.dll', 'UnityEngine.SharedInternalsModule.dll'):
        need(str(engine/name) in pins, 'Pinned installed engine module missing: '+name)
    return config, dict(pins), hashlib.sha256(raw).hexdigest()


def native_admission(project, trace, receipt_path, receipt_pin):
    need(type(receipt_pin) is str and re.fullmatch('[a-f0-9]{64}', receipt_pin), 'Exact native receipt SHA256 required')
    raw = read(receipt_path, 4*1024*1024)
    need(hashlib.sha256(raw).hexdigest() == receipt_pin, 'Native receipt pin differs')
    receipt = parse(raw)
    for key in ('wrapperReaped', 'ownedTreeGone', 'strictOperationalControlGate', 'dataCaptureGate'):
        need(receipt.get(key) is True, 'Native terminal/source admission missing: '+key)
    need(type(receipt.get('wrapperExitCode')) is int and receipt['wrapperExitCode'] == 0, 'Successful native wrapper exit required')
    baseline_path = Path(receipt['baseline'])
    baseline_raw = read(baseline_path, 16*1024*1024)
    need(hashlib.sha256(baseline_raw).hexdigest() == receipt['baselineSha256'], 'Native baseline pin differs')
    baseline = parse(baseline_raw)
    need(type(baseline.get('pins')) is dict and 1 <= len(baseline['pins']) <= 65536, 'Complete bounded native source pin closure required')
    for name, pin in baseline['pins'].items():
        source = Path(name)
        if not source.is_absolute():
            source = project/source
        need(digest(source) == pin, 'Captured native source changed: '+name)
    trace_pin = digest(trace, 32*1024*1024)
    side = trace.parent/'continuous-causal-window-v8.json'
    need(trace_pin == receipt['traceSha256'] and digest(side, 2*1024*1024) == receipt['sidecarSha256'],
         'Native captured artifact pins differ')
    return receipt, {str(receipt_path): receipt_pin, str(baseline_path): receipt['baselineSha256'],
                     str(trace): trace_pin, str(side): receipt['sidecarSha256']}


def computed_terminal_admission(computed):
    need(computed.get('result') == 'ComputedCausalPrefixThroughOriginalRootExit_FinalStorageAndMaterialBankMatched' and
         computed.get('computed_terminal_material_matched') is True,
         'Computed terminal material prefix assertion missing')
    for name, expected in (('bank_instances', 1), ('bank_reseeds', 0),
                           ('terminal_storage_assertions', 51), ('terminal_profile_assertions', 4)):
        need(type(computed.get(name)) is int and computed[name] == expected,
             'Computed complete terminal bank assertion missing: '+name)


def replay(project, config_path, trace, trace_pin, native_receipt, native_receipt_pin, output):
    project = Path(project).resolve(strict=True)
    config_path, trace, native_receipt = map(lambda p: Path(p).absolute(), (config_path, trace, native_receipt))
    # Capture failures get durable unique logs; no existing output/evidence is overwritten.
    output = unique_output(project, output)
    try:
        config, pins, config_pin = config_pins(config_path)
        receipt, evidence_pins = native_admission(project, trace, native_receipt, native_receipt_pin)
        need(trace_pin == evidence_pins[str(trace)], 'Expected trace SHA256 differs')
        from skirmish_causal_admission import admit_files
        admitted = admit_files(trace, trace_pin, canonical_query_order_inputs=True)
        save(output/'structural-admission.json', admitted)
        pins[str(config_path)] = config_pin
        for name in ('skirmish_causal_replay.py', 'skirmish_causal_io.py', 'skirmish_causal_admission.py', 'skirmish_material_cursor.py'):
            path = Path(__file__).parent/name
            pins[str(path)] = digest(path)
        for path in (Path(__file__).parent/'SkirmishOfflineSim/RetainedCausal/contracts').glob('*.json'):
            pins[str(path)] = digest(path)
        command = ['rtk', 'proxy', config['mono_path'], '--optimize=-float32', config['worker_path'],
                   config['engine_path'], config['newtonsoft_path'], str(trace), str(trace.parent/'continuous-causal-window-v8.json'),
                   trace_pin, str(output/'computed-prefix.json')]
        save(output/'command.json', dict(command=command, scope='RetainedRootExitPrefixOnly', starts_unity=False))
        began = time.perf_counter()
        try:
            process = subprocess.run(command, capture_output=True, timeout=120)
            stdout, stderr, code = process.stdout, process.stderr, process.returncode
        except subprocess.TimeoutExpired as error:
            stdout, stderr, code = error.stdout or b'', error.stderr or b'', None
        (output/'replay.stdout.log').write_bytes(stdout)
        (output/'replay.stderr.log').write_bytes(stderr)
        unchanged = all(digest(p) == h for p, h in {**pins, **evidence_pins}.items())
        need(unchanged, 'Source/runtime/evidence changed during replay')
        # Recheck native captured sources as well; changed sources invalidate the result.
        native_admission(project, trace, native_receipt, native_receipt_pin)
        passed = code == 0 and b'[RetainedV8CausalPrefix] result=Passed' in stdout
        computed = parse(read(output/'computed-prefix.json', 4*1024*1024)) if passed else None
        if passed:
            computed_terminal_admission(computed)
        result = dict(result='RetainedCausalPrefixPassed' if passed else 'RetainedCausalPrefixFailed', process_exit=code,
                      elapsed_seconds=time.perf_counter()-began, starts_unity=False, scope='FirstProductionBeforeThroughFirstOwnedRootExit',
                      source_runtime_evidence_pins_unchanged=unchanged, pins=pins, evidence_pins=evidence_pins,
                      native_clean_lifecycle=receipt.get('cleanLifecycleGate') is True, computed= computed,
                      full_frame_derived=False, full_mission_derived=False, normal_mission_acceptance=False)
        save(output/'report.json', result)
        return result
    except Exception as error:
        save(output/'failure.json', dict(result='CausalAdmissionOrReplayFailed', error=str(error), starts_unity=False,
                                       full_frame_derived=False, normal_mission_acceptance=False))
        raise


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    build = sub.add_parser('build')
    build.add_argument('--project', type=Path, required=True)
    build.add_argument('--output', type=Path, required=True)
    build.add_argument('--source-root', type=Path)
    build.add_argument('--math-module', type=Path)
    run = sub.add_parser('run')
    for name in ('project', 'config', 'trace', 'native-receipt', 'output'):
        run.add_argument('--'+name, type=Path, required=True)
    run.add_argument('--trace-sha256', required=True)
    run.add_argument('--native-receipt-sha256', required=True)
    args = parser.parse_args(argv)
    try:
        need(sys.platform == 'darwin', 'Retained causal execution is qualified only for the installed macOS Mono/engine backend')
        if args.command == 'build':
            from skirmish_causal_worker import build_worker
            output = output_path(args.project, args.output)
            config = build_worker(output, args.project, source_root=args.source_root, math_module=args.math_module)
            print(json.dumps(dict(result='RetainedCausalWorkerBuilt', config=str(config), starts_unity=False)))
            return 0
        result = replay(args.project, args.config, args.trace, args.trace_sha256, args.native_receipt,
                        args.native_receipt_sha256, args.output)
        print(json.dumps({key: result[key] for key in ('result', 'process_exit', 'elapsed_seconds', 'scope', 'full_frame_derived', 'normal_mission_acceptance')}))
        return 0 if result['result'] == 'RetainedCausalPrefixPassed' else 1
    except (OSError, ValueError, RuntimeError, KeyError, TypeError, ImportError) as error:
        print(json.dumps(dict(result='CausalReplayFailed', error=str(error), starts_unity=False, normal_mission_acceptance=False)))
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
