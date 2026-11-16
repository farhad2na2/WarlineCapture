#!/usr/bin/env python3
"""Join S004's observed source role order to retained original prefab metadata."""
import argparse
import hashlib
import json
from pathlib import Path

import audit_s004_unit_metadata_coverage as coverage

ROOT = coverage.ROOT
OBSERVATION = ROOT / 'T Logs/SkirmishProduction/s004-dispatcher-probe-20261104-T3/receipt.json'
PREFABS = ROOT / 'Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json'
METADATA = ROOT / coverage.INPUTS['s003']
SNAPSHOT = ROOT / 'T Logs/SkirmishProduction/s004-role-census-source-snapshot-20261104-T1'
HISTORICAL = {
    str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/S004BundleDrivenStartup.cs'): SNAPSHOT / 'S004BundleDrivenStartup.cs',
    str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/s004_unit_metadata_profile.py'): SNAPSHOT / 's004_unit_metadata_profile.py',
    # Exact pre-guard bytes recovered from a retained generated baseline; the
    # observation's own SHA remains authoritative and is checked below.
    str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CompiledSetupInput.cs'):
        ROOT / 'T Logs/SkirmishProduction/s004-historical-setupguard-snapshot-20261105-T1/SourceS004CompiledSetupInput.cs',
}
BUILD = ROOT / 'T Logs/SkirmishProduction/s002-single-query-options-build-20261104-T1/build.json'


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def build(source_build=None, prefabs=None, unit_metadata=None):
    source_build=Path(BUILD if source_build is None else source_build).resolve(strict=True)
    prefabs=Path(PREFABS if prefabs is None else prefabs).resolve(strict=True)
    unit_metadata=Path(METADATA if unit_metadata is None else unit_metadata).resolve(strict=True)
    paths = (OBSERVATION, prefabs, unit_metadata, source_build, *HISTORICAL.values(), Path(__file__).resolve())
    pins = {str(path):sha(path) for path in paths}
    observed=json.loads(OBSERVATION.read_bytes()); prefab=json.loads(prefabs.read_bytes()); metadata=json.loads(unit_metadata.read_bytes())
    if observed.get('status')!='ObservedDiagnosticSourcePlacement' or observed.get('runtime_admission') is not False or observed.get('mission_ready') is not False:
        raise ValueError('Unqualified original S004 source observation required')
    for path, digest in observed['input_sha256'].items():
        actual=HISTORICAL.get(path,Path(path))
        if sha(actual)!=digest: raise ValueError('S004 observation input changed: '+path)
    build=json.loads(source_build.read_bytes())
    if build.get('status')!='Passed': raise ValueError('Passing current source build required')
    for path,digest in build['source_and_reference_pins'].items():
        if sha(path)!=digest: raise ValueError('Current gameplay source pin changed: '+path)
    probe=observed['result']['source_gameplay_diagnostic']['source_bank_diagnostic']['source_dispatcher_probe']
    if probe.get('source_dispatcher_bound') is not False or probe.get('injected_requests')!=0:
        raise ValueError('Exact source role census observation required')
    role_rows=probe['original_role_overlay_order']
    keys=[row['prefab_key'] for row in role_rows]
    if len(keys)!=19 or len(set(keys))!=19 or any(row['index']!=i or row['template_present'] is not True for i,row in enumerate(role_rows)):
        raise ValueError('Complete ordered same-world role/template census required')
    if prefab.get('requested_prefab_keys')!=keys or [row['key'] for row in metadata['units']]!=keys:
        raise ValueError('Source role order and captured prefab/metadata orders differ')
    if prefab.get('full_mission_initialized') is not False or prefab.get('mission_ready') is not False or metadata.get('ui_catalog_runtime_policy_sampled') is not False:
        raise ValueError('Captured source inputs must not claim mission or sampled policy')
    source_pins={}
    for document in (prefab,metadata):
        for name,digest in document['source_pins'].items():
            path=Path(name);path=path if path.is_absolute() else ROOT/path
            key=str(path.resolve(strict=True))
            if key in source_pins and source_pins[key]!=digest: raise ValueError('Conflicting source identity: '+key)
            source_pins[key]=digest
    for path,digest in source_pins.items():
        if sha(path)!=digest: raise ValueError('Retained source capture changed: '+path)
    for row in metadata['units']:
        asset=row['prefab']; path=ROOT/asset['path']; meta=Path(str(path)+'.meta')
        if str(path.resolve()) not in source_pins or str(meta.resolve()) not in source_pins:
            raise ValueError('Prefab source and meta pins required: '+row['key'])
        if ('guid: '+asset['guid']) not in meta.read_text(): raise ValueError('Prefab source GUID differs: '+row['key'])
        if not isinstance(row.get('authoring_catalog_properties'),dict): raise ValueError('Original catalog getter capture required')
    if any(sha(path)!=digest for path,digest in source_pins.items()) or any(sha(path)!=digest for path,digest in pins.items()):
        raise ValueError('Inputs changed during source role join')
    if any(sha(path)!=digest for path,digest in build['source_and_reference_pins'].items()):
        raise ValueError('Current gameplay source changed during role join')
    return {'schema_version':1,'kind':'S004SourceRoleCatalogMetadataCandidate',
            'authority':'ObservedS004SourceRolesAndRetainedPrefabMetadata_DiagnosticOnly',
            'catalog_id':'S004','setup_hash':1130726651,'seed':104733,
            'source_inputs':pins,'source_capture_pin_count':len(source_pins),'role_order':role_rows,
            'historical_census_source_snapshots':{name:str(path) for name,path in HISTORICAL.items()},
            'source_pins':metadata['source_pins'],'units':metadata['units'],'transports':metadata['transports'],
            'unsupported_values':metadata['unsupported_values'],'ui_catalog_runtime_policy_sampled':False,
            'baked_actor_footprint_substituted':False,'runtime_camera_basis_exported':False,
            'scope':'Same source role order and retained captured metadata; no S004 native getter or registry parity',
            'native_prefab_resolution':False,'runtime_admission':False,'mission_ready':False}


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True)
    parser.add_argument('--source-build',type=Path,default=BUILD)
    parser.add_argument('--prefabs',type=Path,default=PREFABS)
    parser.add_argument('--unit-metadata',type=Path,default=METADATA)
    args=parser.parse_args()
    result=build(args.source_build,args.prefabs,args.unit_metadata)
    with args.output.open('x') as output: json.dump(result,output,indent=2);output.write('\n')
    print(json.dumps({'status':'CollectedSourceOnly','roles':len(result['units']),
                      'source_pins_checked':result['source_capture_pin_count'],'mission_ready':False}))


if __name__=='__main__':main()
