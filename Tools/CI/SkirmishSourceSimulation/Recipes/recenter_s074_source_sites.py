#!/usr/bin/env python3
"""Correct S074 provisional pad centers using the recorded source rectangle origins."""
import argparse
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1'
LAYOUT = BASE / 's074-provisional-source-path-layout-r2/layout.json'
SEARCH = BASE / 's074-standard-greedy-source-candidate-r1'
LAYOUT_SHA = '7b624eb5894717c0d2e7a04b42c86643fef637e3fc56a82aa83ab380909b46e8'
REQUEST_SHA = 'b6dfb6c21d7a0246f18a6dd4bc627fd6ee1e32c757afdb72d37b6785562c3b15'
RESULT_SHA = '5b48da0909243248f1e67a42b6bfbf8f52feeec94b5ae4b0a0689563db2073d7'
EXISTING = {'building_barrack':'building.barracks', 'building_ammunition_depot':'building.fabrication_depot',
            'building_oilpump':'building.oil_pump', 'building_refinery':'building.refinery', 'building_fuel_bladder':'building.fuel_bladder'}
ADDITIONAL = {'ground_staging':'building.ground_staging', 'satellite_dish':'building.satellite_dish',
              'watchtower':'building.watchtower', 'watchtower_approach':'building.watchtower.approach', 'helipad':'building.helipad'}

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def project(output):
    request_path, result_path, receipt_path = (SEARCH / n for n in ('request.json','result.json','receipt.json'))
    if (digest(LAYOUT), digest(request_path), digest(result_path)) != (LAYOUT_SHA, REQUEST_SHA, RESULT_SHA):
        raise ValueError('Exact retained layout and original source-search inputs required')
    receipt, request, result = (json.loads(p.read_bytes()) for p in (receipt_path, request_path, result_path))
    if receipt.get('status') != 'SourceSearchReturned' or receipt.get('result_sha256') != RESULT_SHA or receipt.get('mission_ready') is not False:
        raise ValueError('Completed unqualified original search required')
    if len(request['existing']) != 10 or len(request['items']) != 10 or len(result['rows']) != 10:
        raise ValueError('Exact bounded source-search domain required')
    for name, expected in receipt['input_pins'].items():
        if digest(Path(name)) != expected:
            raise ValueError('Source-search input changed: ' + name)
    original = json.loads(LAYOUT.read_bytes()); layout = copy.deepcopy(original)
    if layout['operationMapId'] != 'opmap.skirmish.industrial_basin' or layout['cellSize'] != 1 or len(layout['structureInstancePads']) != 20:
        raise ValueError('Exact Industrial Basin single-configuration layout required')
    bindings = {(x['FactionId'], x['StructureId']): x for x in layout['structureInstancePads']}
    pads = {x['PadId']: x for x in layout['pads']}; anchors = {x['AnchorId']: x for x in layout['anchors']}
    if len(bindings) != 20 or len(pads) != len(layout['pads']) or len(anchors) != len(layout['anchors']):
        raise ValueError('Unique original binding, pad and anchor identities required')
    sites = []
    for item in request['existing']:
        sites.append((item['faction'], EXISTING[item['id']], item['x'], item['z'], item['w'], item['h'], 'prior_source_rectangle'))
    for item, row in zip(request['items'], result['rows']):
        if (row['found'] is not True or row['faction'] != item['faction'] or row['role'] != item['role'] or
                row['footprint'] != [item['w'],item['h']] or row['blocks_cells'] != (item['role'] != 'helipad')):
            raise ValueError('Original search output does not match requested role')
        sites.append((item['faction'], ADDITIONAL[item['role']], row['origin'][0], row['origin'][1], item['w'], item['h'], 'original_source_search'))
    changes = []
    for faction, structure, x, z, width, depth, provenance in sites:
        if any(type(v) is not int for v in (faction,x,z,width,depth)) or width <= 0 or depth <= 0:
            raise ValueError('Integer source rectangle required')
        binding = bindings[(faction,structure)]; pad = pads[binding['PadId']]; anchor = anchors[binding['AnchorId']]
        if pad['AnchorId'] != anchor['AnchorId'] or pad['FactionId'] != faction or pad['WidthMetres'] != width or pad['DepthMetres'] != depth:
            raise ValueError('Original source rectangle and typed pad differ')
        cx, cz = x + width * .5, z + depth * .5
        changes.append({'faction':faction, 'structure':structure, 'source_origin':[x,z], 'footprint':[width,depth],
                        'old_center':[pad['CenterX'],pad['CenterZ']], 'new_center':[cx,cz], 'source':provenance})
        pad['CenterX'], pad['CenterZ'] = cx, cz
        anchor['WorldX'], anchor['WorldZ'] = cx, cz
        anchor['NormalizedU'], anchor['NormalizedV'] = cx / 2048.0, cz / 1024.0
    if len(changes) != 20 or len({(x['faction'],x['structure']) for x in changes}) != 20:
        raise ValueError('Exact full source-site domain required')
    layout['contentHash'] = 'unqualified.s074.source-origin-centers.v1'
    output = Path(output).resolve()
    if not output.is_relative_to(ROOT):
        raise ValueError('Repository-owned output required')
    output.mkdir(parents=True, exist_ok=False)
    path = output / 'layout.json'; path.write_text(json.dumps(layout, indent=2) + '\n')
    for name, expected in receipt['input_pins'].items():
        if digest(Path(name)) != expected:
            raise ValueError('Source-search input changed during projection: ' + name)
    report = {'kind':'S074SourceSiteCenterCandidate', 'status':'PreparedUnqualified',
              'layout':{'path':str(path),'sha256':digest(path)}, 'source_layout':{'path':str(LAYOUT),'sha256':LAYOUT_SHA},
              'source_search_receipt':{'path':str(receipt_path),'sha256':digest(receipt_path)}, 'changes':changes,
              'scope':'Source search rectangle origins converted to typed pad centers; paths remain provisional. No current placement or native parity.',
              'physical_geometry_modified':False, 'runtime_admission':False, 'source_fit_certified':False, 'mission_ready':False}
    (output / 'receipt.json').write_text(json.dumps(report, indent=2) + '\n')
    return report

if __name__ == '__main__':
    p = argparse.ArgumentParser(description=__doc__); p.add_argument('--output', required=True)
    report = project(p.parse_args().output)
    print(json.dumps({'status':report['status'],'source_sites':len(report['changes']),'mission_ready':False}))
