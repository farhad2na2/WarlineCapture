#!/usr/bin/env python3
"""Check draft identity and source against the BA catalog and example schema."""
import csv
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
MISSIONS = ROOT / 'Tools/Missions'
SOURCES = {
    'CC': 'Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset',
    'MP': 'Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/Definition.asset',
    'IB': 'Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset',
}


def main():
    example = json.loads((MISSIONS / 'examples/s006-desert-base-ba-combined-established.json').read_text())
    with (ROOT / 'Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv').open(newline='') as stream:
        catalog = {row['scenario_id']: row for row in csv.DictReader(stream)}
    errors = []
    files = sorted((MISSIONS / 'drafts').glob('*.json'))
    if not files:
        errors.append('No draft JSON files found')
    for path in files:
        try:
            draft = json.loads(path.read_text())
            if not isinstance(draft, dict) or set(draft) != set(example):
                raise ValueError('top-level keys differ from example')
            row = catalog.get(draft['catalog_id'])
            if row is None or row['objective_id'] != 'BA':
                raise ValueError('catalog_id does not identify a BA mission')
            if row['map_id'] not in SOURCES or draft['campaign_source'] != SOURCES[row['map_id']]:
                raise ValueError('campaign_source does not match map')
            # An air-return pad alone can describe a helipad; require an explicitly
            # identified Airport role, which the authoring entry accepts under air.*.
            if ('-air-' in path.name or '-combined-' in path.name) and not any(
                role.startswith('air.') and 'airport' in role.lower().split('.')
                for role in draft['pads']
            ):
                print(f'WARNING: {path.name}: no Airport pad')
        except (OSError, ValueError, TypeError, KeyError) as exc:
            errors.append(f'{path.name}: {exc}')
    if errors:
        print('\n'.join(errors), file=sys.stderr)
        return 1
    print(f'PASS: {len(files)} drafts parse and match the example keys, BA catalog, and map sources')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
