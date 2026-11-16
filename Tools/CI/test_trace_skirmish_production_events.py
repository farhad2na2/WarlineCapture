#!/usr/bin/env python3
import json
import pathlib
import tempfile
import unittest
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from trace_skirmish_production_events import trace


class TraceTests(unittest.TestCase):
    def write(self, directory, rows):
        path = pathlib.Path(directory) / 'state.jsonl'
        path.write_text(''.join(json.dumps(row) + '\n' for row in rows))
        return path

    def observation(self, **overrides):
        result = {'install_status': 'Installed', 'observer_still_owned': True,
                  'global_observer_faults_since_install': 0, 'overflow_count': 0,
                  'events': [{'operation': 'Queue', 'before': {'Materials': 10}, 'after': {'Materials': 5}}]}
        result.update(overrides)
        return {'tick': 5, 'production_diagnostics': {'production_ledger_observation': result}}

    def test_preserves_event_and_flags_fault(self):
        with tempfile.TemporaryDirectory() as d:
            result = trace(self.write(d, [self.observation(overflow_count=1)]))
            self.assertEqual(result['events'][0]['event']['after']['Materials'], 5)
            self.assertEqual(result['events'][0]['snapshot_tick'], 5)
            self.assertEqual(len(result['observer_faults_or_gaps']), 1)

    def test_invalid_counters_and_events_rejected(self):
        with tempfile.TemporaryDirectory() as d:
            for row in [self.observation(overflow_count=False), self.observation(global_observer_faults_since_install=-1), self.observation(events=[{}])]:
                with self.assertRaises(ValueError):
                    trace(self.write(d, [row]))

    def test_empty_and_missing_observation(self):
        with tempfile.TemporaryDirectory() as d:
            with self.assertRaises(ValueError):
                trace(self.write(d, []))
            result = trace(self.write(d, [{'tick': 1}]))
            self.assertEqual(result['observer_faults_or_gaps'][0]['reason'], 'observation missing')


if __name__ == '__main__':
    unittest.main()
