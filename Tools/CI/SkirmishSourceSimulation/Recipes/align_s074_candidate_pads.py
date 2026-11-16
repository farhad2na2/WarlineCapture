#!/usr/bin/env python3
"""Create an S074 diagnostic layout with grid-aligned logical structure pads."""
import argparse
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
SOURCE = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-provisional-source-path-layout-r2/layout.json'
SOURCE_SHA = '7b624eb5894717c0d2e7a04b42c86643fef637e3fc56a82aa83ab380909b46e8'

def project(output):
    raw = SOURCE.read_bytes()
    if hashlib.sha256(raw).hexdigest() != SOURCE_SHA:
        raise ValueError('Exact previously compiled S074 provisional layout required')
    layout = json.loads(raw); corrected = copy.deepcopy(layout)
    if (layout['operationMapId'] != 'opmap.skirmish.industrial_basin' or
            layout['worldBinding']['HasMeasuredDeployments'] is not False or
            layout['worldBinding']['BindAllConfigurations'] is not False or
            layout['cellSize'] != 1 or len(layout['structureInstancePads']) != 20):
        raise ValueError('Exact single-configuration Industrial Basin candidate required')
    ids = [row['PadId'] for row in layout['structureInstancePads']]
    if len(set(ids)) != 20:
        raise ValueError('Unique original structure pads required')
    pads = {row['PadId']: row for row in corrected['pads']}
    if len(pads) != len(corrected['pads']) or any(key not in pads for key in ids):
        raise ValueError('Complete unique typed pad domain required')
    changes = []
    for key in ids:
        pad = pads[key]
        for field in ('WidthMetres', 'DepthMetres'):
            value = pad[field]
            if type(value) not in (int, float) or value <= 0 or value != int(value):
                raise ValueError('Positive whole-cell candidate pad required')
            if int(value) % 2:
                pad[field] = value + 1
                changes.append({'pad_id': key, 'field': field, 'old': value, 'new': value + 1})
    if len(changes) != 18 or len({row['pad_id'] for row in changes}) != 16:
        raise ValueError('Exact observed odd-footprint pad domain changed')
    corrected['contentHash'] = 'unqualified.s074.grid-aligned.v1'
    output = Path(output).resolve()
    if not output.is_relative_to(ROOT):
        raise ValueError('Repository-owned output required')
    output.mkdir(parents=True, exist_ok=False)
    path = output / 'layout.json'
    path.write_text(json.dumps(corrected, indent=2) + '\n')
    if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_SHA:
        raise ValueError('Original layout changed during projection')
    report = {'kind':'S074GridAlignedLogicalPadCandidate', 'status':'PreparedUnqualified',
              'source':{'path':str(SOURCE), 'sha256':SOURCE_SHA},
              'layout':{'path':str(path), 'sha256':hashlib.sha256(path.read_bytes()).hexdigest()},
              'changes':changes, 'structure_pad_count':20, 'changed_pad_count':16,
              'reason':'Exact odd-cell footprints cannot fit a same-sized pad centered on integer grid coordinates.',
              'physical_geometry_modified':False, 'runtime_admission':False, 'source_fit_certified':False, 'mission_ready':False}
    (output / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')
    return report

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True)
    result = project(parser.parse_args().output)
    print(json.dumps({'status':result['status'], 'changed_pads':result['changed_pad_count'], 'mission_ready':False}))
