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
    'DB': 'Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset',
    'AP': 'Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset',
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
            # instance_pads and structure_footprints are optional (per-building pads, see check_draft.py).
            optional = {'instance_pads', 'structure_footprints', 'pad_slack_m'}
            # FC/BT/CE candidates (-candidate.json): typed areas, plus area_measurement once a Unity measurement is attached.
            candidate = path.name.endswith('-candidate.json')
            if candidate:
                optional |= {'objective_areas', 'area_measurement'}
            if not isinstance(draft, dict) or set(draft) - optional != set(example) - optional:
                raise ValueError('top-level keys differ from example')
            row = catalog.get(draft['catalog_id'])
            if candidate:
                if row is None or row['objective_id'] not in ('FC', 'BT', 'CE'):
                    raise ValueError('candidate catalog_id does not identify an FC/BT/CE mission')
                # Same rule as test_check_draft: measured candidates carry area_measurement, unmeasured ones do not.
                marker = 'candidate=1, measured' if 'area_measurement' in draft else 'candidate=1, unmeasured'
                if marker not in draft['notes']:
                    raise ValueError('candidate draft must be marked ' + marker + ' in notes')
            elif row is None or row['objective_id'] != 'BA':
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
