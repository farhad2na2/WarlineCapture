"""Describe first observed S026 cross-run drift without assigning a cause."""
import argparse
import collections
import hashlib
import importlib.util
import json
from pathlib import Path


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def changed_fields(left, right, prefix=''):
    if type(left) is not type(right):
        return [prefix + ':type']
    if isinstance(left, dict):
        result = []
        for key in sorted(set(left) | set(right)):
            name = prefix + '/' + key
            if key not in left or key not in right:
                result.append(name + ':missing')
            elif left[key] != right[key]:
                result.extend(changed_fields(left[key], right[key], name))
        return result
    if isinstance(left, list):
        result = [prefix + ':length'] if len(left) != len(right) else []
        for index, (a, b) in enumerate(zip(left, right)):
            if a != b:
                result.extend(changed_fields(a, b, prefix + '/' + str(index)))
        return result
    return [prefix]


p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--baseline', type=Path, required=True)
p.add_argument('--candidate', type=Path, required=True)
p.add_argument('--comparison', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
roots = [a.baseline.resolve(strict=True), a.candidate.resolve(strict=True)]
files = [[root / name for name in ('run-receipt.json', 'result.json', 'aria-trace.jsonl', 'validator-state.jsonl')] for root in roots]
comparison_path = a.comparison.resolve(strict=True)
paths = [x for group in files for x in group] + [comparison_path, Path(__file__).resolve(), Path(__file__).with_name('compare.py').resolve()]
pins = {str(x): sha(x) for x in paths}
comparison = json.loads(comparison_path.read_bytes())
if comparison.get('kind') != 'S026IndexedTransformRunComparison' or any(comparison['input_sha256'].get(str(x)) != pins[str(x)] for group in files for x in group):
    raise ValueError('Exact S026 paired comparison pins required')
spec = importlib.util.spec_from_file_location('source_transform_comparison', Path(__file__).with_name('compare.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
traces = [[json.loads(row) for row in group[2].read_text().splitlines()] for group in files]
snapshots = [[json.loads(row) for row in group[3].read_text().splitlines()] for group in files]
worlds = [rows[0]['world_id'] for rows in traces]
findings = {}
for name, pair in (('trace', traces), ('validator', snapshots)):
    index = comparison['normalized_differences'][name]['first_different_index']
    if index is None:
        findings[name] = None
        continue
    left, right = [module.normalize(rows[index], world) for rows, world in zip(pair, worlds)]
    fields = changed_fields(left, right)
    findings[name] = {'row_index': index, 'tick': left.get('tick'), 'changed_leaf_count': len(fields),
                      'changed_top_level': sorted({f.split('/')[1] for f in fields if f.startswith('/')}),
                      'changed_field_counts': dict(collections.Counter('/'.join(f.split('/')[:3]) for f in fields))}
    if name == 'trace':
        findings[name]['changed_leaf_paths'] = fields
if any(sha(Path(name)) != digest for name, digest in pins.items()):
    raise ValueError('Input evidence changed during drift audit')
report = {'kind': 'S026IndexedTransformFirstDriftAudit', 'input_sha256': pins, 'findings': findings,
          'cause_established': False, 'whole_run_parity': False, 'mission_ready': False,
          'scope': 'First differences in two source-only runs; no causal or native-parity claim'}
with a.output.open('x') as stream:
    json.dump(report, stream, indent=2)
    stream.write('\n')
print(json.dumps({'findings': findings, 'cause_established': False, 'mission_ready': False}))
