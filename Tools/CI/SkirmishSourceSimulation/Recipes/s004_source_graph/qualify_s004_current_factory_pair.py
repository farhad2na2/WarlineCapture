#!/usr/bin/env python3
"""Join actual S004 factory and readmodel captures as partial source data only."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
FACTORY = ROOT / 'Logs/SkirmishProduction/s004-current-authoritative-factory-20261105-r1/data/full-factory-runway-definitions.json'
FACTORY_LOG = ROOT / 'T Logs/SkirmishProduction/s004-current-authoritative-factory-20261105-T1/editor.log'
READMODEL = ROOT / 'Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-r3/data/configured-spawnable-readmodel.json'
READ_RECEIPT = ROOT / 'T Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-T3/finalization/capture-receipt.json'
SETUP = ROOT / 'T Logs/SkirmishProduction/s004-current-scope-setup-export-20261105-T1/compiled-setup.json'
MARKER = '[S004FullFactoryRunwayReference] result=Collected buildings=9 unsupported=0 mission=0 readiness=0'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def descriptor(path): return {'path':str(path.resolve(strict=True)),'sha256':sha(path),'bytes':path.stat().st_size}

def qualify(output, wrapper_exit):
    if wrapper_exit != 0 or output.exists(): raise ValueError('Observed successful wrapper and fresh output required')
    paths = (FACTORY, FACTORY_LOG, READMODEL, READ_RECEIPT, SETUP)
    before = {str(path):sha(path) for path in paths}
    if sum(line.endswith(MARKER) for line in FACTORY_LOG.read_text().splitlines()) != 1:
        raise ValueError('Unique original factory pass marker required')
    factory, model, receipt, setup = [json.loads(path.read_bytes()) for path in (FACTORY,READMODEL,READ_RECEIPT,SETUP)]
    if (factory.get('kind') != 'S004OriginalFullFactoryRunwayDefinitions' or factory.get('authority') != 'OriginalS004ProductionOverlayAndDefinitionFactory' or
        factory.get('setup_hash') != 1130726651 or factory.get('seed') != 104733 or factory.get('runway_reader_supplied') is not True or
        factory.get('unsupported_values') != [] or factory.get('mission_world_created') is not False or factory.get('mission_ready') is not False):
        raise ValueError('Exact source-only original factory domain required')
    if (receipt.get('schema') != 's004-configured-spawnable-readmodel-receipt-v1' or receipt.get('rawExportSha256') != sha(READMODEL) or
        receipt.get('wrapper',{}).get('exitCode') != 0 or receipt.get('sourcePinsVerified') is not True or receipt.get('missionReadiness') is not False or
        model.get('setup_hash') != 1130726651 or model.get('mission_initialized') is not False or model.get('mission_readiness') is not False):
        raise ValueError('Independently qualified source-only readmodel required')
    if (setup.get('kind') != 'S004StandardCompiledSetupSourceExport' or setup.get('status') != 'Passed' or
        setup.get('compiled_setup',{}).get('SetupHash') != 1130726651 or setup.get('world_initialized') is not False or
        model.get('setup_evidence',{}).get('sha256') != sha(SETUP)):
        raise ValueError('Exact source compiler setup evidence required')
    pins = factory.get('source_pins'); model_pins = model.get('source_pins')
    if not isinstance(pins,dict) or len(pins) < 9000 or not isinstance(model_pins,dict) or len(model_pins) < 9000:
        raise ValueError('Complete captured source pin maps required')
    checked = {}
    for source in (pins,model_pins):
        for name, expected in source.items():
            path = Path(name); path = (path if path.is_absolute() else ROOT / path).resolve(strict=True)
            actual = checked.setdefault(str(path),sha(path)) if str(path) not in checked else checked[str(path)]
            if actual != expected: raise ValueError('Factory source changed: ' + name)
    buildings = factory.get('buildings'); rows = model.get('entries')
    if not isinstance(buildings,list) or len(buildings)!=9 or not isinstance(rows,list) or len(rows)!=9:
        raise ValueError('Nine factory and publisher rows required')
    by_key = {row['prefab']['key']:row for row in buildings}
    if len(by_key)!=9 or len({row['visual_key'] for row in rows})!=9: raise ValueError('Duplicate factory identity')
    field_count = 0
    for row in rows:
        key = row['visual_key']; building = by_key.get(key); source = row['source_definition']
        if building is None or building['prefab'] != source['prefab']: raise ValueError('Prefab identity mismatch: ' + key)
        projected = source['definition_projection_inputs']
        if set(projected) != {'Prefab','DisplayName','CreditsCost','MaterialsCost','FootprintCells'}:
            raise ValueError('Exact publisher factory input field set required')
        for name, value in projected.items():
            if building['fields'].get(name) != value: raise ValueError('Original factory/publisher field mismatch: ' + key + '/' + name)
            field_count += 1
    for name, expected in checked.items():
        if sha(Path(name)) != expected: raise ValueError('Source changed during factory qualification: ' + name)
    if {str(path):sha(path) for path in paths} != before: raise ValueError('Capture inputs changed during qualification')
    output.mkdir(parents=True)
    package = {'kind':'S004CurrentFactoryPairSourceData','status':'CollectedPartialSourceOnly',
               'factory':descriptor(FACTORY),'factory_log':descriptor(FACTORY_LOG),'readmodel':descriptor(READMODEL),
               'readmodel_receipt':descriptor(READ_RECEIPT),'setup':descriptor(SETUP),'factory_wrapper_exit_observed':wrapper_exit,
               'unique_source_pins_checked':len(checked),'buildings':9,'publisher_fields_compared':field_count,
               'complete_gameplay_package':False,'prefab_templates_included':False,'physical_scene_validated':False,
               'world_initialized':False,'runtime_admission':False,'source_fit_certified':False,'mission_ready':False}
    with (output/'package.json').open('x') as stream: json.dump(package,stream,indent=2);stream.write('\n')
    return package

if __name__ == '__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--wrapper-exit',type=int,required=True)
    parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    result=qualify(args.output.resolve(),args.wrapper_exit)
    print(json.dumps({'status':result['status'],'buildings':result['buildings'],'publisher_fields_compared':result['publisher_fields_compared'],
                      'unique_source_pins_checked':result['unique_source_pins_checked'],'mission_ready':False}))
