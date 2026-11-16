#!/usr/bin/env python3
"""Record bounded paired source timing and semantic differences without promotion."""
import argparse
import hashlib
import itertools
import json
from pathlib import Path
from compare_pair import normalized, first_difference


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for part in iter(lambda: f.read(1024 * 1024), b''):
            h.update(part)
    return h.hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('off', type=Path); p.add_argument('on', type=Path); p.add_argument('output', type=Path)
    a = p.parse_args()
    directories = [a.off.resolve(strict=True), a.on.resolve(strict=True)]
    receipts = [json.loads((d / 'run-receipt.json').read_bytes()) for d in directories]
    results = [json.loads((d / 'result.json').read_bytes()) for d in directories]
    marker = '[SourceFreshWitnessDigest] enabled=1 scope=ExperimentalSourceOnly'
    for i, (d, receipt, result) in enumerate(zip(directories, receipts, results)):
        if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0 or
                receipt.get('timed_out') is not False or receipt.get('diagnostic_only') is not True or
                receipt.get('validation_passed') is not False or receipt.get('mission_ready') is not False or
                receipt.get('source_result') != result or result.get('tick') != 2404 or
                (d / 'process.log').read_text().count(marker) != i):
            raise ValueError('Exact source-only bounded off/on run required')
    if receipts[0]['build_receipt']['sha256'] != receipts[1]['build_receipt']['sha256'] or receipts[0]['data_bundle']['sha256'] != receipts[1]['data_bundle']['sha256']:
        raise ValueError('Same build and bundle required')
    files = [d / n for d in directories for n in ('run-receipt.json', 'process.log', 'aria-trace.jsonl', 'validator-state.jsonl')]
    pins = {str(f): sha(f) for f in files}
    differences = []
    counts = {'trace': 0, 'state': 0, 'decision_or_receipt': 0, 'observation': 0, 'state_difference': 0}
    worlds = [None, None]
    keys = ('frame', 'time', 'action', 'decision_kind', 'intent', 'target_id', 'executed', 'stopped', 'completed_logical_contacts', 'receipt')
    for label, name in [('trace', 'aria-trace.jsonl'), ('state', 'validator-state.jsonl')]:
        with (directories[0] / name).open() as left, (directories[1] / name).open() as right:
            for la, lb in itertools.zip_longest(left, right):
                if la is None or lb is None:
                    raise ValueError('Trace length differs for ' + label)
                x, y = json.loads(la), json.loads(lb)
                counts[label] += 1
                if label == 'trace':
                    if counts[label] == 1:
                        worlds = [x.get('world_id'), y.get('world_id')]
                    if not all(worlds) or x.get('world_id') != worlds[0] or y.get('world_id') != worlds[1]:
                        raise ValueError('Source world identity changed')
                    paths = [('decision_or_receipt', {k: x.get(k) for k in keys}, {k: y.get(k) for k in keys}, set()),
                             ('observation', x['observation'], y['observation'], {'ProjectionTicks', 'GoalEvaluationTicks', 'AllocatedBytes'})]
                else:
                    paths = [('state_difference', x, y, {'world_id', 'entity_namespace'})]
                for category, v1, v2, excluded in paths:
                    path = first_difference(normalized(v1, worlds[0], excluded), normalized(v2, worlds[1], excluded))
                    if path is not None:
                        counts[category] += 1
                        if len(differences) < 12:
                            differences.append({'category': category, 'frame': x.get('frame'), 'tick': x.get('tick'), 'path': path})
    if counts['trace'] != 2400 or any(sha(Path(path)) != digest for path, digest in pins.items()):
        raise ValueError('Trace count or source files changed')
    report = {'kind': 'S003FreshWitnessDigestPairAudit', 'status': 'ComparedDiagnosticPrefixes', 'input_sha256': pins,
              'counts': counts, 'first_differences': differences,
              'advance_wall_seconds': [r['advance_wall_seconds'] for r in results],
              'aria_observation_wall_seconds': [r['cumulative_aria_wall_seconds']['observation'] for r in results],
              'scope': 'One sequential pair, source-only proxy planner and unqualified physical/camera data. Differences cannot be attributed to a cause and timing is not a whole-simulator speed proof.',
              'semantic_parity': not any(counts[k] for k in ('decision_or_receipt', 'observation', 'state_difference')),
              'optimization_promoted': False, 'native_parity_proven': False, 'mission_ready': False}
    a.output.parent.mkdir(parents=True, exist_ok=True)
    with a.output.open('x') as f:
        json.dump(report, f, indent=2); f.write('\n')
    print(json.dumps({k: v for k, v in report.items() if k != 'input_sha256'}))


if __name__ == '__main__':
    main()
