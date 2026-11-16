#!/usr/bin/env python3
"""Check whether current S006 candidate footprint inputs vary by difficulty.

This compares compiled candidate data only; it does not certify placement or admission.
"""
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
CANDIDATE = LOGS / 's006-current-config-only-candidate-20261106-T22'
FOOTPRINT = LOGS / 's006-current-historical-footprint-check-20261106-T2'
OUT = LOGS / 's006-footprint-difficulty-invariance-20261106-T2'
SIZES = ('Standard', 'War', 'LargeWar')
DIFFICULTIES = ('Recruit', 'Regular', 'Veteran', 'Commander')


def digest(data):
    return hashlib.sha256(data).hexdigest()


def compare(rows):
    indexed = {}
    for row in rows:
        key = (row.get('size'), row.get('difficulty'))
        if key in indexed or key[0] not in SIZES or key[1] not in DIFFICULTIES or row.get('compiled') is not True:
            raise ValueError('Unexpected, duplicate or uncompiled candidate row')
        indexed[key] = row
    if len(indexed) != len(SIZES) * len(DIFFICULTIES):
        raise ValueError('Complete difficulty and size matrix required')
    for size in SIZES:
        reference = indexed[size, 'Regular']
        for difficulty in DIFFICULTIES:
            row = indexed[size, difficulty]
            if row['setup']['Structures'] != reference['setup']['Structures'] or row['source_pad_resolution'] != reference['source_pad_resolution']:
                raise ValueError('Footprint source inputs differ: ' + size + '/' + difficulty)
    return [{'size': size, 'difficulty': difficulty, 'setup_hash': indexed[size, difficulty]['setup_hash']} for size in SIZES for difficulty in DIFFICULTIES]


def verify_join(rows, receipt):
    joins = receipt.get('current_candidate_join')
    results = receipt.get('owner_results')
    if not isinstance(joins, list) or not isinstance(results, list) or len(joins) != len(SIZES) or len(results) != len(SIZES):
        raise ValueError('Exactly one owner result and candidate join per size required')
    for size in SIZES:
        matched = [x for x in joins if x.get('size') == size]
        checked = [x for x in results if x.get('size') == size]
        regular = [x for x in rows if x.get('size') == size and x.get('difficulty') == 'Regular']
        if len(matched) != 1 or len(checked) != 1 or len(regular) != 1:
            raise ValueError('Missing or duplicate size join')
        n = len(regular[0]['setup']['Structures'])
        if matched[0].get('setup_hash') != regular[0]['setup_hash'] or matched[0].get('structure_count') != n or checked[0].get('checked') != n or checked[0].get('valid') != n:
            raise ValueError('Original owner evidence does not match Regular candidate')


def main():
    OUT.mkdir(exist_ok=False)
    try:
        paths = [CANDIDATE / 'receipt.json', CANDIDATE / 'result.json', FOOTPRINT / 'receipt.json', FOOTPRINT / 'result.json', Path(__file__)]
        raw = {str(path): path.read_bytes() for path in paths}
        candidate_receipt, candidate, footprint_receipt, footprint = [json.loads(raw[str(path)]) for path in paths[:4]]
        if (candidate_receipt.get('result_sha256') != digest(raw[str(paths[1])]) or footprint_receipt.get('result_sha256') != digest(raw[str(paths[3])]) or footprint_receipt.get('status') != 'CollectedUnqualifiedFootprintChecks' or footprint.get('outside_grid_negative_controls_passed') is not True or any(row['checked'] != row['valid'] for row in footprint_receipt['owner_results'])):
            raise ValueError('Exact successful unqualified input receipts required')
        rows = candidate['rows']
        matrix = compare(rows)
        verify_join(rows, footprint_receipt)
        negatives = []
        for label, mutate in (
            ('structure_changed', lambda x: x[0]['setup']['Structures'].pop()),
            ('pad_changed', lambda x: x[0].__setitem__('source_pad_resolution', None)),
            ('duplicate', lambda x: x.append(copy.deepcopy(x[0]))),
            ('missing', lambda x: x.pop()),
        ):
            test = copy.deepcopy(rows)
            mutate(test)
            try:
                compare(test)
            except (ValueError, KeyError):
                negatives.append(label)
            else:
                raise ValueError('Negative control accepted: ' + label)
        for label, mutate in (
            ('join_hash_changed', lambda x: x['current_candidate_join'][0].__setitem__('setup_hash', -1)),
            ('owner_result_missing', lambda x: x['owner_results'].pop()),
            ('owner_result_incomplete', lambda x: x['owner_results'][0].__setitem__('valid', 0)),
        ):
            test = copy.deepcopy(footprint_receipt)
            mutate(test)
            try:
                verify_join(rows, test)
            except (ValueError, KeyError):
                negatives.append(label)
            else:
                raise ValueError('Negative control accepted: ' + label)
        if any(path.read_bytes() != raw[str(path)] for path in paths):
            raise ValueError('Input changed during comparison')
        result = {'status': 'CandidateFootprintInputsMatchAcrossDifficulties', 'input_sha256': {k: digest(v) for k, v in raw.items()}, 'matrix': matrix, 'negative_controls_passed': negatives, 'scope': 'Data equality only; prior original-owner checks ran once per size using Regular rows. Does not establish live placement, source fit or difficulty gameplay parity.', 'physical_source_validated': False, 'runtime_admission': False, 'mission_ready': False}
        (OUT / 'receipt.json').write_text(json.dumps(result, indent=2) + '\n')
        print('[S006FootprintDifficulty] result=Collected rows=12 negative_controls=7 readiness=0')
    except Exception as error:
        (OUT / 'failure.json').write_text(json.dumps({'status': 'Failed', 'error': repr(error), 'mission_ready': False}, indent=2) + '\n')
        raise


if __name__ == '__main__':
    main()
