#!/usr/bin/env python3
"""Compare captured original-compiler S004 Standard data without binding a layout."""
import argparse
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
BASE = ROOT / 'T Logs/SkirmishProduction/s004-difficulty-setup-comparison-20261105-T1'
GRAPH = ROOT / 'T Logs/SkirmishProduction/s004-catalog-closure-refresh-20261104-T1/source-config-graph.json'
IGNORED = {'SetupHash', 'DifficultyId', 'Report', 'MeasuredLayoutBound',
           'WorldWidthMetres', 'WorldDepthMetres', 'GridOriginX', 'GridOriginZ', 'CellSize',
           'PlayerBaseWorldX', 'PlayerBaseWorldZ', 'EnemyBaseWorldX', 'EnemyBaseWorldZ',
           'PlayerStagingWorldX', 'PlayerStagingWorldZ', 'EnemyStagingWorldX', 'EnemyStagingWorldZ',
           'PlayerSpawnPadX', 'PlayerSpawnPadZ', 'PlayerRallyPadX', 'PlayerRallyPadZ',
           'EnemySpawnPadX', 'EnemySpawnPadZ', 'EnemyRallyPadX', 'EnemyRallyPadZ', 'DefaultRouteId'}

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def normalized(setup):
    value = copy.deepcopy(setup)
    if not IGNORED.issubset(value):
        raise ValueError('Expected compiler fields missing')
    for key in IGNORED:
        del value[key]
    for name in ('Forces', 'Structures'):
        for row in value[name]:
            del row['SpawnWorldX']
            del row['SpawnWorldZ']
    return value

def audit(output):
    verification = json.loads((BASE / 'verification.json').read_bytes())
    for name, expected in verification['source_hashes'].items():
        if sha(Path(name)) != expected:
            raise ValueError('Historical compiler evidence changed: ' + name)
    graph = json.loads(GRAPH.read_bytes())
    for name, expected in graph['source_pins'].items():
        path = Path(name)
        if not path.is_absolute(): path = ROOT / path
        if sha(path) != expected:
            raise ValueError('Compiler graph source changed: ' + name)
    data = json.loads((BASE / 'result.json').read_bytes())
    rows = [r for r in data['rows'] if r['size'] == 'Standard']
    if len(rows) != 4 or {r['difficulty'] for r in rows} != {'Recruit', 'Regular', 'Veteran', 'Commander'}:
        raise ValueError('Exactly four Standard difficulties required')
    if any(not r['compiled'] or r['compiler_exception'] is not None or r['seed'] != 104733 for r in rows):
        raise ValueError('Actual original compiler must succeed for each difficulty')
    regular = next(r['compiled_setup'] for r in rows if r['difficulty'] == 'Regular')
    if regular['MeasuredLayoutBound'] is not True or regular['WorldWidthMetres'] <= 0 or regular['CellSize'] <= 0:
        raise ValueError('Measured Regular baseline required')
    details = []
    for row in rows:
        setup = row['compiled_setup']
        if normalized(setup) != normalized(regular):
            raise ValueError('Non-layout compiler fields differ for ' + row['difficulty'])
        if row['difficulty'] != 'Regular' and (setup['MeasuredLayoutBound'] is not False or
             any(setup[k] != 0 for k in ('WorldWidthMetres', 'WorldDepthMetres', 'CellSize'))):
            raise ValueError('Unexpected other-difficulty layout state')
        details.append({'difficulty': row['difficulty'], 'setup_hash': setup['SetupHash'],
                        'layout_bound': setup['MeasuredLayoutBound'], 'force_rows': len(setup['Forces']),
                        'structure_rows': len(setup['Structures'])})
    result = {'status': 'EqualNonLayoutCompiledData_SourceOnly', 'source_result_sha256': sha(BASE / 'result.json'),
              'verification_sha256': sha(BASE / 'verification.json'), 'graph_source_pins_verified': len(graph['source_pins']),
              'excluded_fields': sorted(IGNORED), 'excluded_row_fields': ['SpawnWorldX', 'SpawnWorldZ'],
              'rows': details, 'layout_reuse_proven': False, 'world_initialized': False,
              'source_fit_certified': False, 'runtime_admission': False, 'mission_ready': False}
    with output.open('x') as stream: json.dump(result, stream, indent=2); stream.write('\n')
    return result

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(audit(args.output)))
