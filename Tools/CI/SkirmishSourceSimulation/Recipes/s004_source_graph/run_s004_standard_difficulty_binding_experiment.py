#!/usr/bin/env python3
"""Compile an isolated S004 Standard binding candidate; never change game sources."""
import hashlib
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[5]
OUT = ROOT / 'T Logs/SkirmishProduction/s004-standard-difficulty-binding-experiment-20261105-T2'
PREVIOUS = ROOT / 'T Logs/SkirmishProduction/s004-difficulty-setup-comparison-20261105-T1'
GRAPH = ROOT / 'T Logs/SkirmishProduction/s004-catalog-closure-refresh-20261104-T1/source-config-graph.json'
LAYOUT = ROOT / 'T Logs/SkirmishProduction/s004-current-layout-replay-20261104-T1/replayed-layout.json'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
BUILDER = ROOT / 'Assets/Game/Scripts/Configs/Skirmish/SkirmishMapLayoutBuilder.cs'
CSC = Path('/usr/local/share/dotnet/sdk/10.0.201/Roslyn/bincore/csc.dll')
BUILD = ROOT / 'T Logs/SkirmishProduction/s002-current-merged-guard-build-20261105-T1/build.json'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    OUT.mkdir(exist_ok=False)
    verification = json.loads((PREVIOUS / 'verification.json').read_bytes())
    for name, expected in verification['source_hashes'].items():
        if sha(Path(name)) != expected: raise ValueError('Historical input changed: ' + name)
    graph = json.loads(GRAPH.read_bytes())
    for name, expected in graph['source_pins'].items():
        path = Path(name); path = path if path.is_absolute() else ROOT / path
        if sha(path) != expected: raise ValueError('Graph pin changed: ' + name)
    old = '((setup.DifficultyId == SkirmishDifficultyId.Regular &&\n                   setup.SizeId == SkirmishSizeId.Standard) ||'
    new = '''((setup.SizeId == SkirmishSizeId.Standard &&
                   (setup.DifficultyId == SkirmishDifficultyId.Regular ||
                    (setup.CatalogId == "S004" &&
                     (setup.DifficultyId == SkirmishDifficultyId.Recruit ||
                      setup.DifficultyId == SkirmishDifficultyId.Veteran ||
                      setup.DifficultyId == SkirmishDifficultyId.Commander)))) ||'''
    source = BUILDER.read_text()
    if source.count(old) != 1: raise ValueError('Exact original binding condition required')
    candidate = OUT / 'SkirmishMapLayoutBuilder.experimental.cs'
    candidate.write_text(source.replace(old, new, 1))
    rsp = (PREVIOUS / 'compile.rsp').read_text()
    if rsp.count(str(BUILDER)) != 1: raise ValueError('Exactly one builder source required')
    old_output = str(PREVIOUS / 'fixture.exe')
    if rsp.count(old_output) != 1: raise ValueError('Exact previous compiler output required')
    runtime = json.loads(BUILD.read_bytes())['runtime_selection']
    refs = [Path(line.strip()[4:-1]).parent for line in rsp.splitlines() if line.startswith('"-r:')]
    env = os.environ.copy(); env['MONO_PATH'] = os.pathsep.join(map(str, dict.fromkeys(refs)))
    source_files = [Path(line[1:-1]) for line in rsp.splitlines() if line.startswith('"/') and line.endswith('.cs"')]
    input_hashes = {str(p): sha(p) for p in source_files}
    for variant in ('baseline', 'candidate'):
        executable = OUT / (variant + '.exe')
        text = rsp.replace(old_output, str(executable), 1)
        if variant == 'candidate': text = text.replace(str(BUILDER), str(candidate), 1)
        response = OUT / (variant + '.rsp'); response.write_text(text)
        compile_cmd = ['dotnet', str(CSC), '@' + str(response)]
        result = subprocess.run(compile_cmd, cwd=ROOT, capture_output=True, text=True)
        (OUT / (variant + '-compile.log')).write_text(result.stdout + result.stderr)
        if result.returncode: raise RuntimeError(variant + ' compiler failed')
        result_path = OUT / (variant + '-result.json')
        run_cmd = [runtime['mono'], '--optimize=-float32', str(executable), str(GRAPH), str(MATRIX), sha(MATRIX), str(LAYOUT), str(result_path)]
        result = subprocess.run(run_cmd, cwd=ROOT, env=env, capture_output=True, text=True)
        (OUT / (variant + '-run.log')).write_text(result.stdout + result.stderr)
        if result.returncode or '[SourceS004DifficultyCompilerProbe] result=Collected tuples=12' not in result.stdout:
            raise RuntimeError(variant + ' original compiler probe failed')
    if any(sha(Path(name)) != value for name, value in input_hashes.items()):
        raise ValueError('Compiler source changed during experiment')
    baseline = json.loads((OUT / 'baseline-result.json').read_bytes())
    candidate_result = json.loads((OUT / 'candidate-result.json').read_bytes())
    if baseline != json.loads((PREVIOUS / 'result.json').read_bytes()):
        raise ValueError('Current original compiler no longer matches historical baseline')
    for b, c in zip(baseline['rows'], candidate_result['rows']):
        if (b['size'], b['difficulty']) != (c['size'], c['difficulty']): raise ValueError('Tuple order changed')
        expected = b['size'] == 'Standard' and b['difficulty'] != 'Regular'
        if expected and c['compiled_setup']['MeasuredLayoutBound'] is not True:
            raise ValueError('Candidate did not bind expected Standard difficulty')
        if not expected and b != c: raise ValueError('Outside candidate scope changed')
    receipt = {'status': 'ExperimentalCompilerDataCollected', 'builder_sha256': sha(BUILDER),
               'candidate_sha256': sha(candidate), 'compiler_sources_verified': len(input_hashes),
               'baseline_sha256': sha(OUT / 'baseline-result.json'), 'candidate_result_sha256': sha(OUT / 'candidate-result.json'),
               'graph_pins_verified': len(graph['source_pins']), 'world_initialized': False,
               'source_fit_certified': False, 'runtime_admission': False, 'mission_ready': False}
    (OUT / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))

if __name__ == '__main__': main()
