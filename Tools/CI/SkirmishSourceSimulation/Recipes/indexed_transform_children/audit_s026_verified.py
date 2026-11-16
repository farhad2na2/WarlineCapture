"""Record same-call S026 transform evidence without concealing cross-run drift."""
import argparse
import hashlib
import json
import re
from pathlib import Path


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--run', type=Path, required=True)
p.add_argument('--comparison', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
run = a.run.resolve(strict=True)
paths = [run / n for n in ('run-receipt.json', 'result.json', 'process.log')] + [a.comparison.resolve(strict=True)]
pins = {str(x): sha(x) for x in paths}
receipt, result, comparison = (json.loads(x.read_bytes()) for x in (paths[0], paths[1], paths[3]))
provenance = receipt.get('source_provenance_classification') or {}
if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0
        or receipt.get('validation_passed') is not False or receipt.get('source_result') != result
        or provenance.get('experimental_generator_proof') is not True
        or provenance.get('canonical_manifest') is not False
        or comparison.get('kind') != 'S026IndexedTransformRunComparison'
        or comparison['input_sha256'].get(str(paths[0])) != pins[str(paths[0])]
        or comparison['input_sha256'].get(str(paths[1])) != pins[str(paths[1])]):
    raise ValueError('Exact completed diagnostic and experimental comparison required')
markers = re.findall(r'^\[SourceChildParity\] compared=(\d+) originalTicks=(\d+) indexedTicks=(\d+) frequency=(\d+) scope=ActualSourceHostOnly$', paths[2].read_text(), re.MULTILINE)
if len(markers) != 1 or any(int(v) <= 0 for v in markers[0]):
    raise ValueError('Unique positive actual same-call parity marker required')
count, original, indexed, frequency = map(int, markers[0])
if any(sha(Path(name)) != digest for name, digest in pins.items()):
    raise ValueError('Evidence changed during audit')
report = {'kind': 'S026VerifiedTransformChildControl', 'input_sha256': pins,
          'actual_source_comparisons': count, 'tick': result['tick'],
          'same_call_original_seconds': original / frequency, 'same_call_indexed_seconds': indexed / frequency,
          'cross_run_differences': comparison['normalized_differences'],
          'scope': 'One source-only experiment; same-call comparison is not whole-run or native parity',
          'optimization_promoted': False, 'mission_ready': False}
with a.output.open('x') as stream:
    json.dump(report, stream, indent=2)
    stream.write('\n')
print(json.dumps({'compared': count, 'tick': result['tick'], 'cross_run_differences': report['cross_run_differences'], 'mission_ready': False}))
