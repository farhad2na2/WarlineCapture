#!/usr/bin/env python3
"""Extract observed production events and observer faults, without inferring coverage."""
import hashlib
import json
import pathlib
import sys


def trace(path):
    path = pathlib.Path(path)
    digest = hashlib.sha256()
    events, faults = [], []
    snapshot_count = 0
    with path.open('rb') as source:
        for line_number, line in enumerate(source, 1):
            digest.update(line)
            row = json.loads(line)
            if type(row) is not dict or type(row.get('tick')) is not int:
                raise ValueError(f'invalid snapshot at line {line_number}')
            snapshot_count += 1
            diagnostic = row.get('production_diagnostics', {})
            if type(diagnostic) is not dict:
                raise ValueError(f'invalid diagnostic at line {line_number}')
            observation = diagnostic.get('production_ledger_observation')
            if observation is None:
                faults.append({'line': line_number, 'snapshot_tick': row['tick'], 'reason': 'observation missing'})
                continue
            if type(observation) is not dict or type(observation.get('events')) is not list:
                raise ValueError(f'invalid ledger observation at line {line_number}')
            for name in ('install_status', 'observer_still_owned', 'global_observer_faults_since_install', 'overflow_count'):
                if name not in observation:
                    faults.append({'line': line_number, 'snapshot_tick': row['tick'], 'reason': name + ' missing'})
            for name in ('global_observer_faults_since_install', 'overflow_count'):
                if name in observation and (type(observation[name]) is not int or observation[name] < 0):
                    raise ValueError(f'invalid {name} at line {line_number}')
            if (observation.get('install_status') != 'Installed' or observation.get('observer_still_owned') is not True
                    or observation.get('global_observer_faults_since_install') != 0 or observation.get('overflow_count') != 0):
                faults.append({'line': line_number, 'snapshot_tick': row['tick'], 'observation_health': {k: observation.get(k) for k in ('install_status', 'observer_still_owned', 'global_observer_faults_since_install', 'overflow_count')}})
            for event in observation['events']:
                if (type(event) is not dict or type(event.get('operation')) is not str
                        or not event['operation'] or type(event.get('before')) is not dict
                        or type(event.get('after')) is not dict):
                    raise ValueError(f'invalid event at line {line_number}')
                events.append({'line': line_number, 'snapshot_tick': row['tick'], 'event': event})
    if not snapshot_count:
        raise ValueError('no snapshots')
    return {'kind': 'ObservedProductionEventTrace', 'source_sha256': digest.hexdigest(),
            'scope': 'Events retain source order and the raw before/after objects supplied by the producer; their internal schema is not validated. Snapshot tick is not event time. Missing events, rejected operations, startup, and full conservation are not proved.',
            'events': events, 'observer_faults_or_gaps': faults}


if __name__ == '__main__':
    try:
        if len(sys.argv) != 2:
            raise ValueError('expected validator-state.jsonl path')
        print(json.dumps(trace(sys.argv[1]), sort_keys=True))
    except (OSError, ValueError, TypeError) as error:
        print('Trace unavailable: ' + str(error), file=sys.stderr)
        sys.exit(1)
