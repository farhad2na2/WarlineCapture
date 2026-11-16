#!/usr/bin/env python3
"""Compile and exercise the actual publication validator without Unity or catalog writes."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
VALIDATOR = ROOT / 'Assets/Game/Scripts/Configs/Skirmish/SkirmishPublicationValidator.cs'
CONTROL = Path(__file__).with_name('SourceGenericPublicationStagingControl.cs')
MARKER = '[GenericPublicationStagingControl] result=Passed generic=116 flipGuard=1 fixturesOnly=1 catalogWrites=0 actualReadiness=0 mission=0'
SHIM = '''using System;
namespace Game.Configs {
 // The evaluator test must never execute unrelated presentation-copy code.
 internal static class SkirmishExpandedCopyProjection {
  internal static void ApplyLibraryCopy(ref SkirmishBattleCatalogEntry entry) => throw new NotSupportedException("presentation outside evaluator control");
  internal static void ApplyLibraryCopyS003(ref SkirmishBattleCatalogEntry entry) => throw new NotSupportedException("presentation outside evaluator control");
  internal static void ApplyLibraryCopyS004(ref SkirmishBattleCatalogEntry entry) => throw new NotSupportedException("presentation outside evaluator control");
 }
}
'''

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build-receipt', type=Path, required=True)
    parser.add_argument('--mono-config', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--compiler', type=Path, default=Path('/usr/local/share/dotnet/sdk/8.0.423/Roslyn/bincore/csc.dll'))
    parser.add_argument('--timeout', type=int, default=300)
    args = parser.parse_args()
    build_file = args.build_receipt.resolve(strict=True)
    config = args.mono_config.resolve(strict=True)
    compiler = args.compiler.resolve(strict=True)
    output = args.output.absolute()
    if args.timeout <= 0 or output.exists(): raise ValueError('Positive timeout and fresh output required')
    output.mkdir(parents=True)
    build = json.loads(build_file.read_bytes())
    if build['status'] != 'Passed' or build['compile_exit'] != 0 or build['repository_root'] != str(ROOT):
        raise ValueError('Passed current-repository source build required')
    pins = dict(build['source_and_reference_pins'])
    source_response = Path(build['compiler_response']['path']).resolve(strict=True)
    if sha(source_response) != build['compiler_response']['sha256']:
        raise ValueError('Source compiler response changed')
    explicit = [build_file, config, compiler, VALIDATOR, CONTROL, Path(__file__).resolve(), source_response]
    for path in explicit:
        digest = sha(path)
        if str(path) in pins and pins[str(path)] != digest: raise ValueError('Conflicting input pin: ' + str(path))
        pins[str(path)] = digest
    def verify():
        for name, digest in pins.items():
            if sha(Path(name)) != digest: raise ValueError('Input changed: ' + name)
    verify()
    lines = [json.loads(line) for line in source_response.read_text().splitlines() if line.strip()]
    if sum(x.startswith('-main:') for x in lines) != 1 or sum(x.startswith('-out:') for x in lines) != 1:
        raise ValueError('Exact source compiler response required')
    lines = ['-main:SourceGenericPublicationStagingControl' if x.startswith('-main:') else
             '-out:' + str(output / 'control.exe') if x.startswith('-out:') else x for x in lines]
    shim = output / 'UnusedCopyProjection.cs'; shim.write_text(SHIM)
    lines.extend(map(str, (VALIDATOR, CONTROL, shim)))
    response = output / 'compile.rsp'; response.write_text(''.join(json.dumps(line) + '\n' for line in lines))
    compiled = subprocess.run(['dotnet', str(compiler), '@' + str(response)], cwd=ROOT,
                              capture_output=True, text=True, timeout=args.timeout)
    (output / 'compile.log').write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise ValueError('Actual validator source did not compile')
    runtime = build['runtime_selection']; env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((str(Path(build['placement_build']).parent), runtime['engine'],
                                     str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(output)))
    run = subprocess.run([runtime['mono'], '--config', str(config), '--optimize=-float32', str(output / 'control.exe')],
                         cwd=ROOT, env=env, capture_output=True, text=True, timeout=args.timeout)
    (output / 'run.log').write_text(run.stdout + run.stderr)
    if run.returncode != 0 or MARKER not in run.stdout: raise ValueError('Publication staging control failed')
    verify()
    report = {'kind': 'GenericPublicationStagingSourceControl', 'status': 'Passed', 'compile_exit': compiled.returncode,
              'run_exit': run.returncode, 'marker': MARKER, 'input_pin_count_verified_before_after': len(pins),
              'validator_sha256': pins[str(VALIDATOR)], 'control_sha256': pins[str(CONTROL)],
              'build_receipt_sha256': pins[str(build_file)], 'compile_response_sha256': sha(response),
              'scope': 'Synthetic evaluator fixtures only; no catalog write, actual evidence, launch or readiness',
              'catalog_mutated': False, 'mission_ready': False}
    with (output / 'receipt.json').open('x') as stream: json.dump(report, stream, indent=2); stream.write('\n')
    print(MARKER)

if __name__ == '__main__':
    main()
