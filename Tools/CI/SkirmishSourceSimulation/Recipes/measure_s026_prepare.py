"""Measure an actual source-only S026 prepare, preserving its full output and pins."""
import argparse
import hashlib
import json
import subprocess
import sys
import time
from pathlib import Path


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--build-receipt', type=Path, required=True)
p.add_argument('--bundle', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
repo = Path(__file__).resolve().parents[4]
build, bundle = a.build_receipt.resolve(strict=True), a.bundle.resolve(strict=True)
out = a.output.absolute()
out.mkdir(parents=True, exist_ok=False)
inputs = {str(p): sha(p) for p in (build, bundle, Path(__file__).resolve(),
    repo/'Tools/CI/skirmish_source_build.py', repo/'Tools/CI/run_skirmish_source_simulation.py')}
cmd = [sys.executable, '-B', str(repo/'Tools/CI/run_skirmish_source_simulation.py'), 'run',
       '--repo', str(repo), '--mission', 'S026', '--seed', '104755', '--build-receipt', str(build),
       '--data-bundle', str(bundle), '--prepare-only', '--max-ticks', '2', '--timeout', '300',
       '--output', str(out/'prepared')]
start = time.monotonic()
with (out/'process.log').open('w') as log:
    result = subprocess.run(cmd, stdout=log, stderr=subprocess.STDOUT, timeout=900)
elapsed = time.monotonic() - start
if result.returncode != 0:
    raise RuntimeError('Preparation failed; see preserved process.log')
prepared_path = out/'prepared/prepared-run.json'
prepared = json.loads(prepared_path.read_bytes())
if prepared.get('status') != 'PreparedOnly' or prepared.get('world_allocated') is not False:
    raise ValueError('Actual prepared-only result required')
if any(sha(Path(name)) != digest for name, digest in inputs.items()):
    raise ValueError('Measurement inputs changed')
receipt = {'kind': 'S026SourcePrepareWallTime', 'elapsed_seconds': elapsed,
           'inputs_sha256': inputs, 'prepared_receipt_sha256': sha(prepared_path),
           'input_pins_sha256': sha(out/'prepared/input-pins.json'),
           'scope': 'One source CLI prepare only; no world, gameplay speed or readiness claim',
           'mission_ready': False}
(out/'measurement.json').write_text(json.dumps(receipt, indent=2) + '\n')
print(json.dumps({'elapsed_seconds': elapsed, 'world_allocated': False, 'mission_ready': False}))
