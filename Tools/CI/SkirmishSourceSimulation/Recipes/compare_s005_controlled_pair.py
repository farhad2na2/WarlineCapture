#!/usr/bin/env python3
"""Compare retained same-binary S005 diagnostic traces without inferring parity."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
LOGS = ROOT / 'T Logs/SkirmishProduction'
PAIR = LOGS / 's005-controlled-observation-pair-20261106-T1'
OUTPUT = PAIR / 'comparison.json'

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for data in iter(lambda: f.read(1024 * 1024), b''): h.update(data)
    return h.hexdigest()

def main():
    pair = json.loads((PAIR / 'receipt.json').read_bytes())
    if pair.get('status') != 'CompletedBoth' or [x['mode'] for x in pair['modes']] != ['off', 'on']:
        raise ValueError('Both recorded source-only modes required')
    binary = Path(pair['binary'])
    if sha(binary) != pair['binary_sha256']: raise ValueError('Controlled binary changed')
    directories = [Path(x['output']) for x in pair['modes']]
    evidence = [PAIR / 'receipt.json', binary]
    pin_sets = []
    for directory, mode in zip(directories, pair['modes']):
        receipt = directory / 'run-receipt.json'
        if sha(receipt) != mode['run_receipt_sha256'] or json.loads(receipt.read_bytes())['exit_code'] != 0:
            raise ValueError('Exact successful run receipt required')
        paths = [receipt, directory / 'input-pins-before.json', directory / 'launch.json', directory / 'source-result.json', directory / 'source-policy-trace.jsonl', directory / 'production-scheduler-trace.jsonl']
        evidence.extend(paths)
        pin_sets.append(json.loads(paths[1].read_bytes())['pins'])
    def find_metrics(value):
        if isinstance(value, dict):
            for key, child in value.items():
                if key == 'observation_scope_metrics': yield child
                else: yield from find_metrics(child)
        elif isinstance(value, list):
            for child in value: yield from find_metrics(child)
    metrics = [list(find_metrics(json.loads((d / 'source-result.json').read_bytes()))) for d in directories]
    if any(len(m) != 1 for m in metrics) or metrics[0][0].get('enabled') is not False or metrics[0][0].get('scopes') != 0 or metrics[1][0].get('enabled') is not True or metrics[1][0].get('scopes') != 2400:
        raise ValueError('Actual scope-mode metrics differ from the requested pair')
    shared = set(pin_sets[0]) & set(pin_sets[1])
    conflicts = [p for p in shared if pin_sets[0][p] != pin_sets[1][p]]
    if conflicts: raise ValueError('Paired source pins differ: ' + repr(conflicts[:5]))
    def events(directory):
        seen, unique, samples = set(), [], []
        for row in map(json.loads, (directory / 'production-scheduler-trace.jsonl').read_text().splitlines()):
            if 'events' not in row:
                samples.append(row); continue
            for event in row['events']:
                key = json.dumps(event, sort_keys=True, separators=(',', ':'))
                if key not in seen:
                    seen.add(key); unique.append({'first_sample_step': row['step'], **event})
        return unique, samples
    def actions(directory):
        rows = [json.loads(x) for x in (directory / 'source-policy-trace.jsonl').read_text().splitlines()]
        return rows, [{k: row[k] for k in ('frame','decision_kind','intent','action','receipt')} for row in rows if row['receipt'] is not None]
    def first_difference(a, b):
        for i in range(max(len(a), len(b))):
            x = a[i] if i < len(a) else None; y = b[i] if i < len(b) else None
            if x != y: return {'index': i, 'off': x, 'on': y}
        return None
    event_pairs = [events(x) for x in directories]
    policy_pairs = [actions(x) for x in directories]
    rows_a, rows_b = policy_pairs[0][0], policy_pairs[1][0]
    fields = ('frame', 'decision_kind', 'intent', 'action', 'target_id', 'executed', 'stopped')
    decision_a = [{k: row[k] for k in fields} for row in rows_a]
    decision_b = [{k: row[k] for k in fields} for row in rows_b]
    before = {str(path): sha(path) for path in evidence}
    report = {'status': 'ComparedSourceDiagnostics', 'artifact_sha256': before, 'common_input_pins': len(shared),
              'observed_scope_metrics': [m[0] for m in metrics],
              'conflicting_common_pins': len(conflicts), 'first_unique_event_difference': first_difference(event_pairs[0][0], event_pairs[1][0]),
              'first_receipt_difference': first_difference(policy_pairs[0][1], policy_pairs[1][1]),
              'first_selected_decision_difference': first_difference(decision_a, decision_b),
              'unique_event_counts': [len(x[0]) for x in event_pairs], 'receipt_counts': [len(x[1]) for x in policy_pairs],
              'full_actor_state_compared': False, 'enemy_resources_compared': False,
              'cause': 'Unproven', 'candidate_promoted': False, 'mission_ready': False,
              'scope': 'Source-only same-binary diagnostic; cumulative event snapshots deduplicated exactly, no native or full-world parity claim'}
    if any(sha(Path(name)) != digest for name, digest in before.items()): raise ValueError('Evidence changed during comparison')
    with OUTPUT.open('x') as stream: json.dump(report, stream, indent=2); stream.write('\n')
    print(json.dumps({k: v for k, v in report.items() if k != 'artifact_sha256'}))

if __name__ == '__main__': main()
