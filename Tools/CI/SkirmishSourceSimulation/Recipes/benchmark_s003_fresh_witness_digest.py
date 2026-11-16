#!/usr/bin/env python3
"""Compare a fresh full-buffer source-host digest with the original witness."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/s003-actual-importer-current-fixture-20261105-T6'
ORIGINAL = ROOT / 'Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs'
CONTROL = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/S003SnapshotCacheOriginalParityControl.cs'
SIGNATURE = 'public static FixedString128Bytes Digest(DynamicBuffer<GridWalkable> cells){'


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for data in iter(lambda: f.read(1024 * 1024), b''):
            h.update(data)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    out = parser.parse_args().output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must be in the repository')
    out.mkdir(parents=True, exist_ok=False)
    receipt = json.loads((BASE / 'receipt.json').read_bytes())
    if receipt.get('status') != 'PassedDisposableS003ImporterFixture':
        raise ValueError('Actual S003 fixture source closure required')
    pins = dict(receipt['source_pins'])
    for path in (BASE / 'receipt.json', BASE / 'compile.rsp', ORIGINAL, CONTROL, Path(__file__).resolve()):
        pins[str(path)] = sha(path)
    def verify():
        for name, expected in pins.items():
            if not Path(name).is_file() or sha(Path(name)) != expected:
                raise ValueError('Source input changed: ' + name)
    verify()
    original = ORIGINAL.read_text()
    if original.count(SIGNATURE) != 1:
        raise ValueError('Exact original digest seam required')
    reference = out / 'OriginalWitnessReference.cs'
    reference.write_text(original.replace('GridWalkableSourceWitness', 'GridWalkableSourceWitnessReference'))
    candidate = out / 'FreshSnapshotWitness.cs'
    insertion = '''if(Environment.GetEnvironmentVariable("WARLINE_FRESH_WITNESS_BENCH") == "1"){
 var snapshot=cells.Copy();var snapshotBytes=System.Runtime.InteropServices.MemoryMarshal.AsBytes(snapshot.AsSpan());
 if(System.Runtime.InteropServices.Marshal.SizeOf<GridWalkable>()!=1||snapshotBytes.Length!=snapshot.Length)throw new InvalidOperationException("Exact one-byte grid layout required");
 using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(snapshotBytes);
 return new FixedString128Bytes(BitConverter.ToString(hash.GetHashAndReset()).Replace("-","").ToLowerInvariant());}'''
    candidate.write_text(original.replace(SIGNATURE, SIGNATURE + insertion))
    control = CONTROL.read_text()
    marker = '        Console.WriteLine("[S003SnapshotCacheOriginalParityControl]'
    if control.count(marker) != 1:
        raise ValueError('Exact control seam required')
    benchmark = '''        var cells = new DynamicBuffer<GridWalkable>(new GridWalkable[2097152]);
        for(int warm=0;warm<3;warm++){GridWalkableSourceWitnessReference.Digest(cells);GridWalkableSourceWitness.Digest(cells);}
        var oldTimes=new double[15];var newTimes=new double[15];
        for(int i=0;i<15;i++){for(int pass=0;pass<2;pass++){bool prior=((i+pass)%2)==0;var watch=System.Diagnostics.Stopwatch.StartNew();if(prior)GridWalkableSourceWitnessReference.Digest(cells);else GridWalkableSourceWitness.Digest(cells);watch.Stop();if(prior)oldTimes[i]=watch.Elapsed.TotalMilliseconds;else newTimes[i]=watch.Elapsed.TotalMilliseconds;}}
        Array.Sort(oldTimes);Array.Sort(newTimes);Console.WriteLine("[FreshWitnessBenchmark] original_ms="+oldTimes[7].ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" candidate_ms="+newTimes[7].ToString("R",System.Globalization.CultureInfo.InvariantCulture)+" pairs=15 cells=2097152 sourceOnly=1");
'''
    control = control.replace(marker, benchmark + marker)
    control_path = out / 'ParityAndBenchmark.cs'
    control_path.write_text(control)
    args = [json.loads(s) for s in (BASE / 'compile.rsp').read_text().splitlines() if s.strip()]
    if args.count(str(ORIGINAL)) != 1:
        raise ValueError('Original witness source must appear once')
    args = [str(candidate) if x == str(ORIGINAL) else '-main:S003SnapshotCacheOriginalParityControl' if x.startswith('-main:') else '-out:' + str(out / 'control.exe') if x.startswith('-out:') else x for x in args if not x.endswith('/Tests/S003RefreshedPrefabImportControl.cs')]
    args.extend((str(reference), str(control_path)))
    rsp = out / 'compile.rsp'
    rsp.write_text(''.join(json.dumps(v) + '\n' for v in args))
    csc = next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compile_run = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / 'compile.log').write_text(compile_run.stdout + compile_run.stderr)
    if compile_run.returncode:
        raise RuntimeError('Fresh witness experiment failed to compile')
    build = json.loads((ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json').read_bytes())
    runtime = build['runtime_selection']
    env = os.environ.copy()
    env['WARLINE_FRESH_WITNESS_BENCH'] = '1'
    env['MONO_PATH'] = os.pathsep.join((str(BASE.parent / 's003-current-prefab-import-build-20261104-T1/placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(out)))
    run = subprocess.run([runtime['mono'], str(out / 'control.exe')], cwd=ROOT, env=env, capture_output=True, text=True, timeout=600)
    (out / 'run.log').write_text(run.stdout + run.stderr)
    if run.returncode or '[S003SnapshotCacheOriginalParityControl] result=Passed' not in run.stdout or '[FreshWitnessBenchmark]' not in run.stdout:
        raise RuntimeError('Fresh witness parity or benchmark failed')
    verify()
    result = {'status': 'PassedSourceFixtureOnly', 'original_source_sha256': sha(ORIGINAL),
              'candidate_sha256': sha(candidate), 'source_pins_rechecked': len(pins),
              'run_log_sha256': sha(out / 'run.log'), 'benchmark_lines': run.stdout.splitlines(),
              'scope': 'Full snapshot digest on every call; source fixture and microbenchmark only',
              'whole_run_speedup_proven': False, 'native_parity_proven': False, 'mission_ready': False}
    (out / 'receipt.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
