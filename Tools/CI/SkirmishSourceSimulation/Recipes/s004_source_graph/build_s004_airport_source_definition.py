#!/usr/bin/env python3
"""Join a retained original airport definition to the observed S004 source catalog."""
import argparse
import hashlib
import json
from pathlib import Path

import audit_s004_unit_metadata_coverage as coverage
import verify_s004_role_catalog_metadata as role_verifier

ROOT=coverage.ROOT
FACTORY=ROOT/coverage.INPUTS['factory']
S003=ROOT/'T Logs/SkirmishProduction/s003-refreshed-factory-projection-20261104-T1/building-definitions.json'
RECEIPT=S003.parent/'projection-receipt.json'
ROLE=ROOT/'T Logs/SkirmishProduction/s004-role-catalog-metadata-20261104-T2.json'
ROLE_SHA='2a35d4b9eedb5ee04b91a212f22417fc4b9586381af031bebf8f778376aa1a0b'


def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def build():
    role_verifier.verify(ROLE,ROLE_SHA)
    files=(FACTORY,S003,RECEIPT,ROLE,Path(__file__).resolve())
    hashes={str(path):sha(path) for path in files}
    factory=json.loads(FACTORY.read_bytes());captured=json.loads(S003.read_bytes());receipt=json.loads(RECEIPT.read_bytes());role=json.loads(ROLE.read_bytes())
    if (receipt.get('status')!='PassedSourceOnlyProjection' or receipt.get('runtime_admission') is not False or
        receipt['artifacts']['building-definitions.json']['sha256']!=sha(S003) or captured.get('kind')!='NativeQualifiedSkirmishBuildingDefinitions' or
        factory.get('setup_hash')!=1130726651 or factory.get('seed')!=104733):
        raise ValueError('Exact source-only original factory artifacts required')
    pinmap={}
    for document in (factory,captured):
        for name,digest in document['source_pins'].items():
            path=Path(name);path=(path if path.is_absolute() else ROOT/path).resolve(strict=True)
            if str(path) in pinmap and pinmap[str(path)]!=digest:raise ValueError('Conflicting source pin: '+str(path))
            pinmap[str(path)]=digest
    for path,digest in pinmap.items():
        if sha(path)!=digest:raise ValueError('Factory source pin changed: '+path)
    source_rows={row['key']:row for row in captured['definitions']}
    actual=factory['buildings']
    if len(actual)!=9 or len(source_rows)!=10 or 'Building_Airport' in [row['prefab']['key'] for row in actual]:
        raise ValueError('Exact original S004 starting factory domain required')
    for row in actual:
        key=row['prefab']['key']
        if key not in source_rows or source_rows[key]['fields']!=row['fields']:
            raise ValueError('Original factory metadata differs for '+key)
    airport=source_rows['Building_Airport'];fields=airport['fields'];prefab=fields['Prefab']
    if (prefab.get('key')!='Building_Airport' or fields['HasRunway']!={'codec':'bool-v1','value':True} or
        fields['IsWall']!={'codec':'bool-v1','value':False}):raise ValueError('Exact airport/runway source identity required')
    slots=[item['fields']['SpawnUnitPrefab']['key'] for item in fields['ProductionSlots']['items']]
    if slots!=['Unit_Veh_Jet_02','Unit_Veh_Jet_01','Unit_Veh_Plane_Transport']:
        raise ValueError('Original airport production slot order changed')
    by_key={row['key']:row for row in role['units']}
    if any(key not in by_key for key in slots):raise ValueError('Airport slots require captured role identities')
    for item in fields['ProductionSlots']['items']:
        source=item['fields']['SpawnUnitPrefab'];target=by_key[source['key']]['prefab']
        if source!=target:raise ValueError('Airport slot/role asset identity differs')
    for key in ('capture','capture_receipt'):
        linked=receipt[key]
        if sha(linked['path'])!=linked['sha256']:raise ValueError('Original source capture link changed: '+key)
        hashes[linked['path']]=linked['sha256']
    if any(sha(path)!=digest for path,digest in pinmap.items()) or any(sha(path)!=digest for path,digest in hashes.items()):
        raise ValueError('Source changed during airport join')
    return {'kind':'S004SourceAirportDefinitionCandidate','authority':'RetainedOriginalFactory_SourceOnly',
            'catalog_id':'S004','setup_hash':1130726651,'seed':104733,'airport':airport,
            'matching_starting_definition_count':9,'source_pin_count':len(pinmap),'source_inputs':hashes,
            'live_runway_registry_validated':False,'source_fit_certified':False,'runtime_admission':False,'mission_ready':False,
            'scope':'Original source factory airport definition only; no placed airport, live runway or native S004 parity'}


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);a=p.parse_args();d=build()
    with a.output.open('x') as f:json.dump(d,f,indent=2);f.write('\n')
    print(json.dumps({'status':'CollectedSourceOnly','matching_starting_definitions':d['matching_starting_definition_count'],
                      'source_pins_checked':d['source_pin_count'],'live_runway_registry_validated':False,'mission_ready':False}))


if __name__=='__main__':main()
