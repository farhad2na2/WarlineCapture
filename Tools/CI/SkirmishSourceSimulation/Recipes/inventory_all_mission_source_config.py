#!/usr/bin/env python3
"""Inventory catalog and setup-matrix facts without granting runtime admission."""
import argparse
import csv
import hashlib
import io
import json
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
CATALOG = ROOT / 'Assets/Game/Resources/SkirmishBattleCatalog.asset'
MATRIX = ROOT / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
SCENARIOS = ROOT / 'Assets/Game/Configs/SkirmishExpansion/Scenarios'


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    out = parser.parse_args().output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must remain inside repository')
    catalog_raw, matrix_raw = CATALOG.read_bytes(), MATRIX.read_bytes()
    text = catalog_raw.decode('utf-8-sig')
    blocks = re.split(r'(?m)^  - ScenarioId: ', text)
    if len(blocks) != 121:
        raise ValueError('Expected exactly 120 source catalog entries')
    catalog = {}
    for block in blocks[1:]:
        lines = block.splitlines(); identity = lines[0].strip()
        if not re.fullmatch(r'S\d{3}', identity) or identity in catalog:
            raise ValueError('Invalid or duplicate catalog identity')
        fields = {}
        for name in ('MapId', 'ObjectiveId', 'ArmyProfile', 'StartProfile', 'Status', 'PlayableScenarioIndex', 'DefinitionId'):
            values = re.findall(r'(?m)^    ' + name + r':[ \t]*([^\r\n]*)$', block)
            if len(values) != 1:
                raise ValueError('Missing or duplicate catalog field: ' + identity + ' ' + name)
            fields[name] = values[0].strip()
        catalog[identity] = fields
    reader = csv.DictReader(io.StringIO(matrix_raw.decode('utf-8-sig'), newline=''))
    if not reader.fieldnames or len(reader.fieldnames) != len(set(reader.fieldnames)):
        raise ValueError('Matrix has missing or duplicate headers')
    rows = {}
    for row in reader:
        if None in row or any(value is None for value in row.values()):
            raise ValueError('Malformed matrix row')
        key = (row['catalog_id'], row['size_id'])
        if key in rows:
            raise ValueError('Duplicate matrix row: ' + str(key))
        rows[key] = row
    sizes = ('Standard', 'War', 'LargeWar')
    expected = {('S%03d' % index, size) for index in range(1, 121) for size in sizes}
    if set(rows) != expected or set(catalog) != {x[0] for x in expected}:
        raise ValueError('Catalog or matrix does not cover exactly 120 missions and three sizes')
    missions = []
    for identity in sorted(catalog):
        fields = catalog[identity]
        selected = [rows[(identity, size)] for size in sizes]
        for row in selected:
            if any(row[matrix] != fields[code] for matrix, code in [('objective_id', 'ObjectiveId'), ('army_profile', 'ArmyProfile'), ('start_profile', 'StartProfile')]):
                raise ValueError('Catalog and matrix mismatch: ' + identity)
        folder = SCENARIOS / identity
        missions.append({'catalog_id': identity, 'catalog': fields, 'setup_rows': selected,
                         'scenario_directory_present': folder.is_dir(),
                         'catalog_definition_bound': bool(fields['DefinitionId']),
                         'runtime_admission_proven': False, 'source_fit_proven': False, 'mission_ready': False})
    if CATALOG.read_bytes() != catalog_raw or MATRIX.read_bytes() != matrix_raw:
        raise ValueError('Catalog or matrix changed during inventory')
    report = {'kind': 'AllSkirmishMissionSourceConfigurationInventory', 'schema_version': 1,
              'scope': 'Exact serialized catalog and matrix data only; no compiler, gameplay, fit or readiness validation',
              'input_sha256': {str(CATALOG): digest(catalog_raw), str(MATRIX): digest(matrix_raw)},
              'mission_count': len(missions), 'setup_row_count': len(rows),
              'scenario_directories_present': sum(m['scenario_directory_present'] for m in missions),
              'catalog_definitions_bound': sum(m['catalog_definition_bound'] for m in missions),
              'map_counts': dict(Counter(m['catalog']['MapId'] for m in missions)),
              'objective_counts': dict(Counter(m['catalog']['ObjectiveId'] for m in missions)),
              'missions': missions, 'mission_ready': False}
    out.parent.mkdir(parents=True, exist_ok=False)
    out.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k not in ('missions', 'input_sha256')}))


if __name__ == '__main__':
    main()
