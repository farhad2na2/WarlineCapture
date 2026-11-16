#!/usr/bin/env python3
"""Join S026 compiler and actual source-world roster without claiming launch admission."""
import argparse
import collections
import hashlib
import json
import re
from pathlib import Path


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('author-result', 'admission-result', 'admission-receipt', 'run', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    run = args.run.resolve(strict=True)
    author, admission, admission_receipt = (p.resolve(strict=True) for p in
                                            (args.author_result, args.admission_result, args.admission_receipt))
    paths = [author, admission, admission_receipt, run/'run-request.json', run/'run-receipt.json',
             run/'result.json', run/'validator-state.jsonl', run/'aria-trace.jsonl', Path(__file__).resolve()]
    pins = {str(p): sha(p) for p in paths}
    auth, selected, control, request, receipt, result = (json.loads(p.read_bytes()) for p in paths[:6])
    initial_path = Path(request['initialization']['path']).resolve(strict=True)
    if sha(initial_path) != request['initialization']['sha256']:
        raise ValueError('Actual source initialization changed')
    initial = json.loads(initial_path.read_bytes())
    matrix = Path(initial['setup_matrix']['path']).resolve(strict=True)
    candidate = Path(initial['s026_declared_pad_candidate']['path']).resolve(strict=True)
    for path, expected in ((matrix, initial['setup_matrix']['sha256']),
                           (candidate, initial['s026_declared_pad_candidate']['sha256'])):
        if sha(path) != expected or receipt['input_and_runtime_pins'].get(str(path)) != expected:
            raise ValueError('Runtime did not pin the selected authoring input: ' + str(path))
        pins[str(path)] = expected
    pins[str(initial_path)] = sha(initial_path)
    author_inputs = auth.get('input_pins', {})
    if (not any(v.get('sha256') == initial['setup_matrix']['sha256'] for v in author_inputs.values()) or
            author_inputs.get(str(candidate), {}).get('sha256') != initial['s026_declared_pad_candidate']['sha256']):
        raise ValueError('Original compiler did not pin the same matrix and candidate')
    if (auth.get('compiler_accepted') is not True or auth.get('catalog_id') != 'S026' or
            auth.get('source_only_candidate_logical_compiler_passed') is not True or
            auth.get('world_allocated') is not False or auth.get('mission_ready') is not False or
            selected.get('kind') != 'S026OriginalCatalogAdmissionControl' or
            selected.get('mission') != 'S026' or selected.get('original_generic_admission') is not False or
            selected.get('catalog_status') != 'Planned' or selected.get('catalog_playable') is not False or
            selected.get('catalog_scenario_index') != -1 or selected.get('mission_ready') is not False or
            selected.get('launch_or_payload_injected') is not False or selected.get('world_allocated') is not False or
            selected.get('matrix_sha256') != initial['setup_matrix']['sha256'] or
            selected.get('setup_hash') != auth.get('resolved_setup', {}).get('setup_hash') or
            selected.get('seed') != request.get('seed') or selected.get('definition') != auth.get('definition_id') or
            selected.get('layout') != auth.get('layout_id') or
            control.get('status') != 'PassedOriginalPlannedRejection' or
            control.get('result_sha256') != pins[str(admission)] or
            control.get('source_inputs', {}).get(str(run/'run-request.json')) != pins[str(run/'run-request.json')]):
        raise ValueError('Matching actual compiler, input and original admission control required')
    if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0 or
            receipt.get('validation_passed') is not False or receipt.get('diagnostic_only') is not True or
            receipt.get('source_result') != result or result.get('source_outcome') != 'None'):
        raise ValueError('Completed unqualified source prefix required')
    validator = [json.loads(row) for row in (run/'validator-state.jsonl').read_text().splitlines()]
    trace = [json.loads(row) for row in (run/'aria-trace.jsonl').read_text().splitlines()]
    if len(validator) != 3 or len(trace) != 2 or any(row['world_id'] != validator[0]['world_id']
                                                      for row in validator + trace):
        raise ValueError('Actual same-world two-tick source snapshots required')
    expected = {}
    for row in auth['resolved_setup']['forces']:
        key = (row['faction_id'], row['role_id'])
        if (key in expected or key[0] not in (1, 2) or not re.fullmatch(r'role\.[a-z0-9_]+', key[1]) or
                type(row['quantity']) is not int or row['quantity'] < 1):
            raise ValueError('Unique original compiler force rows required')
        expected[key] = row['quantity']
    selected_forces = {}
    for row in selected.get('forces', []):
        key = (row.get('faction'), row.get('role'))
        if key in selected_forces or type(row.get('quantity')) is not int:
            raise ValueError('Unique same-input original compiler readback required')
        selected_forces[key] = row['quantity']
    if selected_forces != expected:
        raise ValueError('Authoring force rows differ from the original compiler readback')
    actual, identities, entities = collections.Counter(), set(), set()
    for actor in validator[0]['actors']:
        owner = actor.get('components', {}).get('Game.Components.SkirmishAttemptOwnedComponent')
        if owner is None or owner.get('IsStructure') != 0:
            continue
        stable = owner.get('StableObjectId', '')
        matches = [key for key in expected if stable.startswith(key[1] + '.' + str(key[0]) + '.')]
        if (len(matches) != 1 or stable in identities or actor['entity'] in entities or
                owner.get('FactionId') != actor.get('faction') or owner.get('FactionId') != matches[0][0] or
                not re.fullmatch(r'\d+\.\d+', stable[len(matches[0][1]) + len(str(matches[0][0])) + 2:])):
            raise ValueError('Unique actual source-world force identity required: ' + stable)
        identities.add(stable)
        entities.add(actor['entity'])
        actual[matches[0]] += 1
    if dict(actual) != expected:
        raise ValueError('Actual source-world roster differs from original compiler')
    if any(sha(Path(name)) != digest for name, digest in pins.items()):
        raise ValueError('Evidence changed during source roster audit')
    rows = [{'faction': faction, 'role': role, 'compiled': count, 'observed': actual[(faction, role)]}
            for (faction, role), count in sorted(expected.items())]
    report = {'kind': 'S026CompiledSourceRosterAudit', 'status': 'PassedSourceWorldReadback',
              'setup_hash': selected['setup_hash'], 'seed': selected['seed'], 'matrix_sha256': selected['matrix_sha256'],
              'snapshot_tick': validator[0]['tick'], 'world_id': validator[0]['world_id'],
              'force_rows': rows, 'actual_units': sum(actual.values()), 'input_sha256': pins,
              'catalog_admission': 'RejectedPlanned', 'native_player_ready': False, 'mission_outcome': None,
              'scope': 'Compiler-to-source-world starting roster only; no native launch, source-fit or resource conservation claim.'}
    with args.output.open('x') as output:
        json.dump(report, output, indent=2)
        output.write('\n')
    print(json.dumps({'status': report['status'], 'actual_units': report['actual_units'], 'force_rows': len(rows)}))


if __name__ == '__main__':
    main()
