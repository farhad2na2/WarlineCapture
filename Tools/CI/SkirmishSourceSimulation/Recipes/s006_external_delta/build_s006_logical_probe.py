#!/usr/bin/env python3
"""Compile the retained S006 probe against currently visible source; no runtime admission."""
import argparse, hashlib, json, subprocess, time
from pathlib import Path

ROOT = Path('/Volumes/T7/Projects/WarlineCapture')
LOGS = ROOT / 'T Logs/SkirmishProduction'
PRIOR = LOGS / 's006-logical-binding-proposal-probe-20261105-T1'
PRIOR_RECEIPT = LOGS / 's006-verified-compiler-replay-20261105-T4/receipt.json'

def digest(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    out = args.output.resolve()
    if not out.is_relative_to(LOGS.resolve()): raise ValueError('Fresh T: evidence directory required')
    out.mkdir(exist_ok=False)
    started = time.monotonic()
    try:
        options = [json.loads(x) for x in (PRIOR / 'compile.rsp').read_text().splitlines() if x.strip()]
        current_receipt = json.loads((LOGS / 's005-terminal-read-build-20261106-T1/receipt.json').read_text())
        if current_receipt['status'] != 'PassedCompileProbe': raise ValueError('Passed current dependency build required')
        current = current_receipt['input_sha256']
        replacements = {
            str(LOGS / 's003-current-prefab-import-build-20261104-T1/source/generated/335_SkirmishScenarioStructureBinding.source.cs'):
                str(LOGS / 's005-player-placement-seam-build-20261106-T3/S005StructureBinding.candidate.cs'),
            str(LOGS / 's003-current-prefab-import-build-20261104-T1/source/generated/253_SourceUiPublicFlags.cs'):
                str(LOGS / 's002-external-matrix-host-build-20261106-T1/source/generated/253_SourceUiPublicFlags.cs')
        }
        for prior, replacement in replacements.items():
            if options.count(prior) != 1 or replacement not in current or digest(replacement) != current[replacement]:
                raise ValueError('Exact current compiled replacement required: ' + replacement)
        options = [replacements.get(x, x) for x in options]
        matrix_source = str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceExternalSetupMatrix.cs')
        if matrix_source in options or matrix_source not in current or digest(matrix_source) != current[matrix_source]:
            raise ValueError('Exact current compiled matrix owner required')
        options.append(matrix_source)
        options = [('-out:' + str(out / 'control.exe')) if x.startswith('-out:') else x for x in options]
        pins = {}
        for option in options:
            if option.startswith('-r:'):
                name = option[3:]
                if '=' in name: name = name.split('=', 1)[1]
            elif not option.startswith('-'): name = option
            else: continue
            path = Path(name).resolve()
            if not path.is_file(): raise FileNotFoundError(path)
            pins[str(path)] = digest(path)
        previous = json.loads(PRIOR_RECEIPT.read_text())['source_pins']
        drift = {p: {'previous': h, 'current': pins.get(p)} for p, h in previous.items()
                 if p in pins and pins[p] != h}
        (out / 'compile.rsp').write_text(''.join(json.dumps(x) + '\n' for x in options))
        with (out / 'compile.log').open('w') as log:
            result = subprocess.run(['dotnet', '/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll',
                                     '@' + str(out / 'compile.rsp')], stdout=log, stderr=subprocess.STDOUT, timeout=120)
        changed = [p for p, h in pins.items() if not Path(p).is_file() or digest(p) != h]
        receipt = {'status': 'PassedCompileOnly' if result.returncode == 0 and not changed else 'FailedCompileOnly',
                   'compile_exit': result.returncode, 'source_pins': pins, 'changed_during_compile': changed,
                   'prior_build_source_drift': drift, 'compile_log_sha256': digest(out / 'compile.log'),
                   'scope': 'Current direct sources and retained generated source; no runtime or current evidence admission',
                   'runtime_admission': False, 'source_fit_certified': False, 'mission_ready': False,
                   'wall_seconds': time.monotonic() - started}
        (out / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
        print(json.dumps({'status': receipt['status'], 'compile_exit': result.returncode,
                          'source_pins': len(pins), 'prior_drift': len(drift), 'changed_during_compile': len(changed)}))
        return 0 if receipt['status'] == 'PassedCompileOnly' else 1
    except Exception as e:
        (out / 'failure.json').write_text(json.dumps({'error': repr(e), 'mission_ready': False}, indent=2) + '\n')
        raise

if __name__ == '__main__': raise SystemExit(main())
