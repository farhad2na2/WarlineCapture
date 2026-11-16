#!/usr/bin/env python3
"""Locate exact historical source bytes for a stale factory capture."""
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
AUDIT = LOGS / 's006-registration-input-audit-20261106-T1/receipt.json'
CHECKPOINT = LOGS / 's003-off-source-checkpoint-20261104-T1/compiled-source'
OUT = LOGS / 's006-factory-capture-drift-20261106-T1'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(exist_ok=False)
    try:
        initial = sha(AUDIT); audit = json.loads(AUDIT.read_bytes())
        if audit.get('status') != 'AuditedUnqualifiedRegistrationInputs' or len(audit['capture_source_pin_mismatches']) != 3:
            raise ValueError('Exact three-drift capture audit required')
        results = []
        for index, item in enumerate(audit['capture_source_pin_mismatches']):
            relative = Path(item['path']); current = ROOT / relative; old = CHECKPOINT / relative
            if not current.is_file() or sha(current) != item['actual']:
                raise ValueError('Current source changed since capture audit')
            row = {'source': str(relative), 'expected_sha256': item['expected'], 'current_sha256': item['actual'], 'exact_historical_bytes_found': False}
            if old.is_file() and sha(old) == item['expected']:
                diff = subprocess.run(['git', 'diff', '--no-index', '--', str(old), str(current)], capture_output=True, text=True)
                if diff.returncode != 1:
                    raise ValueError('Expected a successful nonempty source diff')
                path = OUT / ('drift-' + str(index + 1) + '.diff'); path.write_text(diff.stdout)
                row.update({'exact_historical_bytes_found': True, 'historical_source': str(old), 'diff': str(path), 'diff_sha256': sha(path)})
                if sha(old) != item['expected'] or sha(current) != item['actual']:
                    raise ValueError('Source changed during comparison')
            results.append(row)
        if sha(AUDIT) != initial:
            raise ValueError('Capture audit changed during inspection')
        report = {'status': 'InspectedStaleCaptureSource', 'audit_path': str(AUDIT), 'audit_sha256': initial, 'source_rows': results, 'scope': 'Exact-byte source comparisons where retained; absence in this checkpoint is not a global search. These diffs do not requalify capture or prove factory effects.', 'capture_current': False, 'runtime_admission': False, 'mission_ready': False}
        (OUT / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps({'status': report['status'], 'exact_old_sources': sum(x['exact_historical_bytes_found'] for x in results), 'unresolved': sum(not x['exact_historical_bytes_found'] for x in results), 'mission_ready': False}))
    except Exception as error:
        (OUT / 'failure.json').write_text(json.dumps({'status': 'Failed', 'error': repr(error), 'mission_ready': False}, indent=2) + '\n')
        raise


if __name__ == '__main__':
    main()
