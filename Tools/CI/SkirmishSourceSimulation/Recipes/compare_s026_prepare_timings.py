"""Compare two checked S026 prepares and disclose all changed pin domains."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--baseline', type=Path, required=True)
p.add_argument('--reordered', type=Path, required=True)
p.add_argument('--baseline-build-dir', type=Path, required=True)
p.add_argument('--reordered-build-dir', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
roots = [a.baseline.resolve(strict=True), a.reordered.resolve(strict=True)]
builds = [a.baseline_build_dir.resolve(strict=True), a.reordered_build_dir.resolve(strict=True)]
files = [[root/'measurement.json', root/'prepared/prepared-run.json', root/'prepared/input-pins.json', root/'prepared/run-request.json'] for root in roots]
pins = {str(path): sha(path) for group in files for path in group}
measurements = [json.loads(group[0].read_bytes()) for group in files]
prepared = [json.loads(group[1].read_bytes()) for group in files]
sets = [json.loads(group[2].read_bytes()) for group in files]
requests = [json.loads(group[3].read_bytes()) for group in files]
if any(row.get('kind') != 'S026SourcePrepareWallTime' or row.get('mission_ready') is not False for row in measurements):
    raise ValueError('Two actual source-only timing receipts required')
if any(row.get('status') != 'PreparedOnly' or row.get('world_allocated') is not False for row in prepared):
    raise ValueError('Two actual completed prepares required')
if prepared[0]['data_bundle']['sha256'] != prepared[1]['data_bundle']['sha256']:
    raise ValueError('Data bundle changed')
request_differences = sorted(k for k in set(requests[0]) | set(requests[1]) if requests[0].get(k) != requests[1].get(k))
shared = set(sets[0]) & set(sets[1])
changed = sorted(k for k in shared if sets[0][k] != sets[1][k])
build_sets = [{k[len(str(build))+1:]: v for k, v in rows.items() if k.startswith(str(build)+'/')} for rows, build in zip(sets, builds)]
build_common = set(build_sets[0]) & set(build_sets[1])
build_changed = sorted(k for k in build_common if build_sets[0][k] != build_sets[1][k])
nonbuild_only = [sorted(k for k in set(sets[i])-set(sets[1-i]) if not k.startswith(str(builds[i])+'/')) for i in range(2)]
if any(sha(Path(name)) != digest for name, digest in pins.items()):
    raise ValueError('Measured evidence changed during comparison')
report = {'kind': 'S026SourcePreparePinOrderTimingComparison', 'input_sha256': pins,
          'elapsed_seconds': [row['elapsed_seconds'] for row in measurements],
          'pin_counts': [len(row) for row in sets], 'shared_pin_count': len(shared),
          'shared_changed_paths': changed, 'nonbuild_only_paths': nonbuild_only,
          'build_relative_counts': [len(row) for row in build_sets],
          'build_relative_paths_equal': set(build_sets[0]) == set(build_sets[1]),
          'build_relative_changed_paths': build_changed, 'request_changed_fields': request_differences,
          'same_bundle_sha256': prepared[0]['data_bundle']['sha256'],
          'scope': 'One before/after prepare pair with fresh builds; cache variation possible, not game speed',
          'mission_ready': False}
with a.output.open('x') as stream:
    json.dump(report, stream, indent=2)
    stream.write('\n')
print(json.dumps({'elapsed_seconds': report['elapsed_seconds'], 'pin_counts': report['pin_counts'],
                  'shared_changed': len(changed), 'build_changed': len(build_changed),
                  'request_changed_fields': request_differences, 'mission_ready': False}))
