#!/usr/bin/env python3
"""Audit retained S006 candidate definition coverage without granting admission."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
CAPTURE = ROOT / 'Logs/SkirmishProduction/s005-production-factory-capture-20261105-T2/production-factory-runway-definitions.json'
SEARCH = LOGS / 's006-original-initial-search-20261106-T1'
PROPOSAL = LOGS / 's006-static-footprint-proposals-20261105-T1/report.json'
OUT = LOGS / 's006-registration-input-audit-20261106-T1'


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    OUT.mkdir(exist_ok=False)
    try:
        pins = {str(p): sha(p) for p in (Path(__file__), CAPTURE, SEARCH / 'receipt.json', SEARCH / 'result.json', PROPOSAL)}
        capture = json.loads(CAPTURE.read_bytes()); receipt = json.loads((SEARCH / 'receipt.json').read_bytes()); proposals = json.loads(PROPOSAL.read_bytes())
        if receipt.get('status') != 'CollectedUnqualifiedOriginalInitialSearch' or receipt.get('result_sha256') != pins[str(SEARCH / 'result.json')] or receipt['source_pins'].get(str(PROPOSAL)) != pins[str(PROPOSAL)] or receipt['source_pins'].get(str(CAPTURE)) != pins[str(CAPTURE)]:
            raise ValueError('Exact linked unqualified search evidence required')
        if capture.get('mission_ready') is not False or not isinstance(capture.get('source_pins'), dict):
            raise ValueError('Exact non-ready retained source definition capture required')
        captured = {}
        for row in capture['buildings']:
            key = row['prefab']['key']
            if key in captured:
                raise ValueError('Duplicate captured prefab key')
            captured[key] = row
        needed = sorted({p['visual_key'] for group in proposals['sizes'] for p in group['proposals']})
        missing = [key for key in needed if key not in captured]
        mismatches = []
        for path, expected in capture['source_pins'].items():
            file = Path(path)
            actual = sha(file) if file.is_file() else None
            if actual != expected:
                mismatches.append({'path': path, 'expected': expected, 'actual': actual})
        if any(sha(path) != expected for path, expected in pins.items()):
            raise ValueError('Input changed while auditing')
        report = {'status': 'AuditedUnqualifiedRegistrationInputs', 'input_sha256': pins, 'needed_prefab_keys': needed, 'captured_prefab_keys': sorted(captured), 'missing_prefab_keys': missing, 'capture_source_pin_count': len(capture['source_pins']), 'capture_source_pin_mismatches': mismatches, 'capture_current': not mismatches, 'scope': 'Name coverage and current file hash audit only; no identity equivalence, importer, definition registration, live World or source-fit proof.', 'runtime_admission': False, 'mission_ready': False}
        (OUT / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')
        print(json.dumps({'status': report['status'], 'needed': len(needed), 'missing': len(missing), 'source_pins': len(capture['source_pins']), 'pin_mismatches': len(mismatches), 'mission_ready': False}))
    except Exception as error:
        (OUT / 'failure.json').write_text(json.dumps({'status': 'Failed', 'error': repr(error), 'mission_ready': False}, indent=2) + '\n')
        raise


if __name__ == '__main__':
    main()
