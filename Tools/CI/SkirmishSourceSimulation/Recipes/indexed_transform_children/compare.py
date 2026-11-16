#!/usr/bin/env python3
"""Compare two actual bounded source runs without converting differences to parity."""
import argparse
import hashlib
import json
from pathlib import Path

TIMING = {'aria_profile_seconds', 'phase_wall_seconds', 'ProjectionTicks', 'GoalEvaluationTicks', 'AllocatedBytes', 'TimerFrequency'}


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def normalize(value, world, session=None):
    if isinstance(value, dict):
        return {key: normalize(item, world, session) for key, item in value.items() if key not in TIMING}
    if isinstance(value, list):
        return [normalize(item, world, session) for item in value]
    if isinstance(value, str):
        if world:
            value = value.replace(world, '<source-world>')
        return value.replace(session, '<source-session>') if session else value
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', type=Path, required=True)
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--mission', choices=('S003', 'S026'), default='S003')
    args = parser.parse_args()
    roots = [args.baseline.resolve(strict=True), args.candidate.resolve(strict=True)]
    files = [[root / name for name in ('run-receipt.json', 'result.json', 'aria-trace.jsonl', 'validator-state.jsonl')] for root in roots]
    pins = {str(path): sha(path) for group in files for path in group}
    receipts = [json.loads(group[0].read_bytes()) for group in files]
    results = [json.loads(group[1].read_bytes()) for group in files]
    for receipt, result in zip(receipts, results):
        if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0 or
                receipt.get('timed_out') is not False or receipt.get('validation_passed') is not False or
                receipt.get('source_result') != result):
            raise ValueError('Completed source-only bounded runs required')
        if not str(result.get('scope', '')).startswith('ActualGenerated'+args.mission+'_'):
            raise ValueError('Actual result mission does not match comparison scope')
    if results[0]['tick'] != results[1]['tick']:
        raise ValueError('Same completed tick required')
    traces = [[json.loads(line) for line in group[2].read_text().splitlines()] for group in files]
    snapshots = [[json.loads(line) for line in group[3].read_text().splitlines()] for group in files]
    worlds = [trace[0]['world_id'] for trace in traces]
    sessions = [rows[0]['session_outcome_facts']['Game.Components.SkirmishMatchState']['SessionId']
                for rows in snapshots]
    if any(not isinstance(session, str) or not session for session in sessions):
        raise ValueError('Observed source session IDs required for comparison')
    differences = {}
    for name, pair in (('trace', traces), ('validator', snapshots)):
        left, right = (normalize(value, world, session) for value, world, session in zip(pair, worlds, sessions))
        count = sum(a != b for a, b in zip(left, right)) + abs(len(left) - len(right))
        first = next((i for i, (a, b) in enumerate(zip(left, right)) if a != b), None)
        differences[name] = {'baseline_rows': len(left), 'candidate_rows': len(right),
                             'different_rows': count, 'first_different_index': first,
                             'exact_after_declared_normalization': count == 0}
    if any(sha(Path(name)) != expected for name, expected in pins.items()):
        raise ValueError('Run inputs changed during comparison')
    timings = [{'advance_seconds': result.get('advance_wall_seconds'),
                'transform_seconds': result.get('cumulative_owner_wall_seconds', {}).get('transforms61'),
                'cli_seconds': receipt.get('wall_seconds')} for result, receipt in zip(results, receipts)]
    report = {'kind': args.mission+'IndexedTransformRunComparison', 'input_sha256': pins, 'tick': results[0]['tick'],
              'normalized_differences': differences, 'timings': timings,
              'normalization': 'Replace observed run world and session IDs; omit only named timing/allocation instrumentation fields',
              'scope': 'One source-only paired comparison; no native parity, mission readiness or generalized speed claim',
              'mission_ready': False}
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=False)
    output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'tick': report['tick'], 'differences': differences, 'timings': timings, 'mission_ready': False}))


if __name__ == '__main__':
    main()
