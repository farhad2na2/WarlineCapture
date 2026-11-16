import copy
import json
import os
import pathlib
import unittest
from skirmish_s002_observed_ledger import analyze, trace

RUN = os.environ.get('S002_LEDGER_RECORDED_RUN')

@unittest.skipUnless(RUN, 'set S002_LEDGER_RECORDED_RUN to a retained compatible run')
class RecordedChain(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        run = pathlib.Path(RUN)
        cls.observed = trace(run/'validator-state.jsonl')
        cls.result = json.loads((run/'result.json').read_text())
        cls.terminal = json.loads((run/'source-terminal.json').read_text())
        cls.receipt = json.loads((run/'run-receipt.json').read_text())
        cls.request = json.loads((run/'run-request.json').read_text())

    def check(self, observed, result, terminal, receipt=None, request=None):
        return analyze(observed, result, terminal, receipt or self.receipt, request or self.request)

    def test_actual(self):
        result = self.check(self.observed, self.result, self.terminal)
        self.assertEqual((result['event_count'], result['deliveries']), (34, 4))
        self.assertFalse(result['full_conservation_proved'])
        self.assertFalse(result['validation_passed'])

    def test_tampered_recorded_transitions_fail(self):
        for op, change in [
            ('Queue', lambda e: e['after']['Bank'].__setitem__('LifetimeProductionSpent', 0)),
            ('BindProducer', lambda e: e['before']['Reservation'].__setitem__('MemberCount', 99)),
            ('Deliver', lambda e: e['actor_owner'].__setitem__('ReservationId', 999)),
            ('Deliver', lambda e: e['after']['Reservation'].__setitem__('DeliveredMembers', 99))]:
            with self.subTest(op=op):
                observed = copy.deepcopy(self.observed)
                event = next(x['event'] for x in observed['events'] if x['event']['operation'] == op)
                change(event)
                with self.assertRaises(ValueError): self.check(observed, self.result, self.terminal)

    def test_gap_or_terminal_tampering_fails(self):
        observed = copy.deepcopy(self.observed); observed['observer_faults_or_gaps'].append({'reason':'lost'})
        with self.assertRaises(ValueError): self.check(observed, self.result, self.terminal)
        result = copy.deepcopy(self.result); result['source_produced_units'].pop()
        with self.assertRaises(ValueError): self.check(self.observed, result, self.terminal)
        terminal = copy.deepcopy(self.terminal); terminal['source_outcome'] = 'Defeat'
        with self.assertRaises(ValueError): self.check(self.observed, self.result, terminal)

    def test_produced_faction_and_producer(self):
        for field in ('OwnerFactionId','BuildingRuntimeId','ProductionSlotBuildingRuntimeId'):
            result = copy.deepcopy(self.result); result['source_produced_units'][0][field] = 999
            receipt = copy.deepcopy(self.receipt); receipt['source_result'] = result
            with self.subTest(field=field), self.assertRaises(ValueError):
                self.check(self.observed, result, self.terminal, receipt)

if __name__ == '__main__': unittest.main()
