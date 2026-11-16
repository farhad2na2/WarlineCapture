"""Controlled batch classifier checks against a retained source-only worker result."""
import copy
import hashlib
import importlib.util
import json
import tempfile
from pathlib import Path

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('batch', root / 'Tools/CI/run_skirmish_source_batch.py')
batch = importlib.util.module_from_spec(spec); spec.loader.exec_module(batch)
source = root / 'Logs/SkirmishProduction/s073-terminal-gate-run-20261103-r1/result.json'
worker = json.loads(source.read_text())
job = {'mission': 'S073', 'seed': worker['seed']}
process = {'cli_exit': 0}
wrapper = {'status': 'DiagnosticTerminalOutcomeNativeReadinessUnverified', 'process_exit': 2,
           'timed_out': False, 'source_host_diagnostic_observed': True, 'full_outcome_observed': True,
           'diagnostic_prefix_completed': False, 'terminal_phase_observed': True,
           'terminal_outcome_observed': 'Draw', 'validation_passed': False, 'mission_ready': False,
           'map_blocker_parity_verified': False}

with tempfile.TemporaryDirectory(prefix='s073-batch-stopped-') as tmp:
    path = Path(tmp).resolve() / 'worker.json'
    def classify(value):
        path.write_text(json.dumps(value))
        receipt = dict(wrapper, worker_result={'path': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
        return batch.classify_s073_terminal_diagnostic(job, process, receipt)
    result = classify(worker)
    assert result['status'] == 'DiagnosticTerminalOutcomeObserved' and result['diagnostic_source_outcome'] == 'Draw'
    assert result['mission_victory'] is None and result['validation_passed'] is False
    for key, mutate in (
        ('session', lambda d: d.__setitem__('session_id', 'different')),
        ('failure', lambda d: d['source_host_original_tactical_owner_scheduler_tick'].__setitem__('aria_failure', 'failed')),
        ('not_stopped', lambda d: d['source_host_original_tactical_owner_scheduler_tick'].__setitem__('aria_stopped', False)),
        ('terminal', lambda d: d['source_host_original_tactical_owner_scheduler_tick']['original_production_runtime'].__setitem__('source_host_terminal_outcome', 'Victory')),
        ('actions', lambda d: d['source_host_original_tactical_owner_scheduler_tick'].__setitem__('aria_actions_accepted', 999)),
        ('last_frame', lambda d: d['source_host_aria_policy_step']['last_step'].__setitem__('frame', -1)),
    ):
        altered = copy.deepcopy(worker); mutate(altered)
        assert classify(altered)['status'] == 'Failed', key
print('[S073StoppedAriaBatchControl] result=Passed historicalWorker=1 syntheticWrapper=1 negativeCases=6 readiness=0')
