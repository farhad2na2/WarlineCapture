"""Additive projection for the Native-11 S049 metadata-only receipt; never updates admission."""
import argparse, copy, hashlib, json, re
from pathlib import Path

RECEIPT_SHA = '1423ad3b5e87cd5ac6f6fc5a2f7eee87e20b0410f0a7bc2df4a2b66a13198132'
PRIOR_PROJECTION_SHA = 'c8b588fd4708b1ccc94dd2d2c4d44ffa89a8510adeffc0d73421773fbe5edaec'
PRIOR_NATIVE_SHA = '5b1852e889ff1e19e9dd5cfdeedc157d253990dc0d5963c82fe78abbd2cce040'
EXPECTED_TARGET = ('S049', 'Standard', 'Regular', 104734, 971644661)
PRIOR_EVIDENCE_ROOT = Path('Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r6')
PRIOR_COMPILER_SHA = '4e0599e7f0ac194e5d175fbbde567dd4cc4c70be006f90ed7a21988d5af9a045'
PRIOR_INITIALIZATION_PATH = Path('Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r8/partial-initialization.json')
PRIOR_INITIALIZATION_SHA = '662d00b896e5a1b9cca36f420ff1e60b9b1de6525a76f55f633b624c8bcdd5d9'
PRIOR_BUNDLE_PATH = Path('Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r8/partial-data-bundle.json')
PRIOR_BUNDLE_SHA = 'ded092c82bfc8db209dbc33dfc50e8a23606eaabfaa633a759dcd29c6fd39224'
PRIOR_FIXED_MAP_SHA = 'ce4dd0e06a572cc9337018ddf161eee248fa5345caaa525e7bec2ae0012eea72'

def sha(raw): return hashlib.sha256(raw).hexdigest()
def jwrite(path, value):
    path.write_text(json.dumps(value, indent=2) + '\n')
    return {'path': str(path), 'sha256': sha(path.read_bytes())}
def source_refs(value):
    if isinstance(value, dict):
        if value.get('codec') == 'source-asset-reference-v1' and isinstance(value.get('path'), str):
            yield value['path']
        for v in value.values(): yield from source_refs(v)
    elif isinstance(value, list):
        for v in value: yield from source_refs(v)

def project(receipt_path, output, root):
    root=Path(root).resolve(strict=True)
    receipt_path=Path(receipt_path).resolve(strict=True)
    raw=receipt_path.read_bytes()
    if sha(raw)!=RECEIPT_SHA: raise ValueError('Exact Native-11 receipt SHA required')
    report=json.loads(raw)
    keys={'kind','scope','context','historical_metadata_parity','campaign_source','candidate_asset_origin','physical_map_created_or_changed','mission_initialized','runtime_state_transferred','native_launch_provider_selected','public_admission_changed','source_pins'}
    if set(report)!=keys or report['kind']!='NativeS049MetadataOnlyRecapture' or report['scope']!='CurrentCompilerAndOriginalMetadataFactoriesOnly':
        raise ValueError('Exact metadata-only receipt shape required')
    if any(report[k] is not False for k in ('physical_map_created_or_changed','mission_initialized','runtime_state_transferred','native_launch_provider_selected','public_admission_changed')):
        raise ValueError('Metadata-only and no-map flags required')
    c=report['context']; target=(c.get('catalog_id'),c.get('size'),c.get('difficulty'),c.get('seed'),c.get('setup_hash'))
    if target!=EXPECTED_TARGET or c.get('wrong_context_hash_rejected') is not True or c.get('wrong_target_seed_rejected') is not True:
        raise ValueError('Exact S049 compiler target and negative checks required')
    if report['campaign_source'].get('mission_source_fingerprint')!='c001f6c1849acf24b7b54bdcfbd3a5bc75679c98f071f12924fa5ad6e59a1020':
        raise ValueError('Exact pinned S049 mission-source fingerprint required')
    parity=report['historical_metadata_parity']
    historical_path=(root/parity['artifact']).resolve(strict=True)
    historical_raw=historical_path.read_bytes()
    if sha(historical_raw)!=PRIOR_PROJECTION_SHA or parity.get('artifact_sha256')!=PRIOR_PROJECTION_SHA or parity.get('native_artifact_sha256')!=PRIOR_NATIVE_SHA:
        raise ValueError('Exact immutable prior S049 epoch required')
    old=json.loads(historical_raw); oldc=old['context']
    if c.get('metadata')!=oldc.get('metadata') or c.get('metadata_key_sha256')!=oldc.get('metadata_key_sha256') or c.get('selected_native_domain')!=oldc.get('selected_native_domain'):
        raise ValueError('New S049 metadata semantics differ from prior epoch')
    prior_compiler_path=(root/PRIOR_EVIDENCE_ROOT/'compiler-context.json').resolve(strict=True)
    prior_initialization_path=(root/PRIOR_INITIALIZATION_PATH).resolve(strict=True)
    prior_bundle_path=(root/PRIOR_BUNDLE_PATH).resolve(strict=True)
    prior_fixed_map_path=(root/PRIOR_EVIDENCE_ROOT/'fixed-map-context.json').resolve(strict=True)
    lineage_raw={str(prior_compiler_path):prior_compiler_path.read_bytes(),str(prior_initialization_path):prior_initialization_path.read_bytes(),str(prior_bundle_path):prior_bundle_path.read_bytes(),str(prior_fixed_map_path):prior_fixed_map_path.read_bytes()}
    for path,expected in ((prior_compiler_path,PRIOR_COMPILER_SHA),(prior_initialization_path,PRIOR_INITIALIZATION_SHA),(prior_bundle_path,PRIOR_BUNDLE_SHA),(prior_fixed_map_path,PRIOR_FIXED_MAP_SHA)):
        if sha(lineage_raw[str(path)])!=expected: raise ValueError('Exact preserved S049 r6 source descriptor required: '+str(path))
    prior_compiler=json.loads(lineage_raw[str(prior_compiler_path)])
    prior_target=prior_compiler.get('target_context',{})
    if prior_compiler.get('kind')!='OriginalS049DirectNativeFactoryContext' or (prior_target.get('catalog_id'),prior_target.get('size'),prior_target.get('difficulty'),prior_target.get('seed'),prior_target.get('setup_hash'))!=EXPECTED_TARGET or prior_target.get('source_content_hash')!=report['campaign_source'].get('content_hash') or prior_target.get('source_fingerprint')!=report['campaign_source'].get('mission_source_fingerprint'):
        raise ValueError('Prior S049 compiler context does not match new metadata epoch')
    pins=report['source_pins']
    if not isinstance(pins,dict) or len(pins)!=8681: raise ValueError('Exact complete Native-11 source-pin closure required')
    relative={}
    for source,digest in pins.items():
        source_path=Path(source)
        if source_path.is_symlink() or '..' in source_path.parts:
            raise ValueError('Canonical S049 source pin required: '+source)
        path=(source_path if source_path.is_absolute() else root/source_path).resolve(strict=True)
        if (path!=root and root not in path.parents) or not re.fullmatch('[0-9a-f]{64}',digest) or sha(path.read_bytes())!=digest:
            raise ValueError('Native-11 source pin mismatch: '+source)
        rel=path.relative_to(root).as_posix()
        if rel in relative: raise ValueError('Duplicate canonical source pin: '+rel)
        relative[rel]=digest
    # This current receipt pins the complete source closure directly; the old
    # bridge-subset path is not needed for this new immutable epoch.
    assets=set(source_refs(c['metadata']))
    for path in assets:
        p=Path(path)
        rel=p.relative_to(root).as_posix() if p.is_absolute() else p.as_posix()
    if not assets: raise ValueError('Native metadata asset references required')
    for path in assets:
        p=Path(path)
        rel=p.relative_to(root).as_posix() if p.is_absolute() else p.as_posix()
        if rel not in relative or rel+'.meta' not in relative: raise ValueError('Native metadata asset references missing from source-pin closure: '+rel)
    out=Path(output).absolute()
    if out.exists(): raise ValueError('Fresh immutable S049 epoch output directory required')
    # Projection provenance is immutable and excludes the runtime executor:
    # executor/CLI bytes are bound independently by each build and run receipt.
    tooling_paths=[Path(__file__).resolve(),Path(__file__).with_name('project_native_qualified_metadata.py').resolve(strict=True),root/'Tools/CI/skirmish_source_profiles.py',root/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceMetadataCompatibility.cs',root/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceNativeMetadataEpoch.cs']
    projector_inputs={str(path):path.read_bytes() for path in tooling_paths}
    projector_hashes={path:sha(raw_bytes) for path,raw_bytes in projector_inputs.items()}
    out.mkdir(parents=True)
    native_sha=sha(raw)
    origin={k:copy.deepcopy(c[k]) for k in ('catalog_id','size','difficulty','seed','setup_hash','metadata_key_sha256')}
    origin.update(kind=report['kind'],artifact_sha256=native_sha,previous_projection_sha256=PRIOR_PROJECTION_SHA,previous_native_artifact_sha256=PRIOR_NATIVE_SHA)
    common={'schema_version':2,'authority':'OriginalNativeMetadataFactories_NotMissionState','catalog_id':'S049','setup_hash':971644661,'seed':104734,'native_origin':origin,'unsupported_values':[],'full_mission_initialized':False,'native_visual_readiness':False}
    m=c['metadata']
    definitions=copy.deepcopy(common); definitions.update(kind='NativeQualifiedSkirmishBuildingDefinitions',definitions=[{'key':r['prefab']['key'],'prefab':r['prefab'],'fields':r['fields']} for r in m['buildings']],source_pins=relative)
    d=jwrite(out/'building-definitions.json',definitions)
    units=copy.deepcopy(common); units.update(kind='NativeQualifiedSkirmishUnitProductionMetadata',baked_actor_footprint_substituted=False,runtime_camera_basis_exported=False,source_pins=relative,transports=m['transports'],units=[dict(copy.deepcopy(r),key=r['prefab']['key']) for r in m['units']],ui_catalog_runtime_policy_sampled=False)
    u=jwrite(out/'unit-production-metadata.json',units)
    configured_rows=[]
    for r in m['buildings']:
        entry=r['configured_entry']['fields']
        configured_rows.append({'definition_index':r['index'],'key':r['prefab']['key'],'prefab':r['prefab'],'display_name':entry['DisplayName']['value'],'can_request':entry['CanRequest']['value'],'construction_catalog_index':None if r['construction_index']<0 else r['construction_index'],'scene_catalog_index':None if r['scene_index']<0 else r['scene_index']})
    configured=copy.deepcopy(common); configured.update(kind='NativeQualifiedConfiguredBuildingMetadata',reference=d,source_pins=relative,rows=configured_rows)
    s=jwrite(out/'configured-metadata.json',configured)
    r8_initialization=json.loads(lineage_raw[str(prior_initialization_path)])
    if r8_initialization.get('world_profile')!={'identity_backend':'FreshLogicalEntitiesV1','system_initialization':'OriginalOnCreate','future_native_inputs':False} or r8_initialization.get('source_epoch')!='OriginalCompilerDerivedNativeFactoryContext/v1' or r8_initialization.get('metadata_compiler_context')!={'path':str(prior_compiler_path),'sha256':PRIOR_COMPILER_SHA}:
        raise ValueError('Exact retained r8 fresh-world profile and r6 compiler provenance required')
    write_epoch={'kind':'S049NativeMetadataEpoch','schema_version':1,'epoch_id':'Native-11','projection_id':'Projection-17','receipt':str(receipt_path),'receipt_sha256':native_sha,'prior_epoch':{'artifact':str(historical_path),'sha256':PRIOR_PROJECTION_SHA,'native_artifact_sha256':PRIOR_NATIVE_SHA},'metadata_semantics_equal_to_prior':True,'source_pin_count':len(relative),'source_pin_closure_sha256':sha(json.dumps(relative,sort_keys=True,separators=(',',':')).encode()),'projector_inputs':projector_hashes,'preserved_lineage_inputs':{path:sha(raw_bytes) for path,raw_bytes in lineage_raw.items()},'source_lineage':{'initialization_path':str(prior_initialization_path),'initialization_sha256':PRIOR_INITIALIZATION_SHA,'data_bundle_path':str(prior_bundle_path),'data_bundle_sha256':PRIOR_BUNDLE_SHA,'source_epoch':'OriginalCompilerDerivedNativeFactoryContext/v1','world_profile':copy.deepcopy(r8_initialization['world_profile'])},'runtime_admission':False,'mission_ready':False}
    epoch=jwrite(out/'metadata-epoch.json',write_epoch)
    def descriptor(path): return {'path':str(Path(path).resolve()),'sha256':sha(Path(path).read_bytes())}
    prior_descriptor=descriptor(prior_compiler_path)
    metadata_context={'schema_version':1,'kind':'OriginalS049NativeMetadataEpochContext','authority':'CurrentS049CompilerAndNativeMetadataEpoch_SourceOnly','target_context':prior_target,'prior_compiler_context':prior_descriptor,'native_metadata_receipt':descriptor(receipt_path),'metadata_epoch':epoch,'projected_artifacts':{'building_definitions':d,'unit_production_metadata':u,'building_request_metadata':s},'source_root':str(root),'world_allocated':False,'source_fit_certified':False,'mission_ready':False,'runtime_loader_admitted':False}
    context_file=out/'metadata-epoch-context.json';context_descriptor=jwrite(context_file,metadata_context)
    qualification={'schema_version':1,'kind':'ExperimentalGroundConfigurationQualification','runtime_backend':'TypedGroundS026V1','compiler_context':context_descriptor,'scope':'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly','native_context_qualified':False,'mission_ready':False}
    qualification_file=out/'configuration-qualification.json';qualification_descriptor=jwrite(qualification_file,qualification)
    fixed_map=json.loads(lineage_raw[str(prior_fixed_map_path)])
    if fixed_map.get('kind')!='OriginalCapturedFixedMapCompilerContext' or fixed_map.get('native_mission_context_qualified') is not False or fixed_map.get('runtime_admitted') is not False:
        raise ValueError('Exact preserved S049 fixed-map diagnostic context required')
    fixed_map['metadata_compiler_context']=context_descriptor
    fixed_map['configuration_qualification']=qualification_descriptor
    fixed_map_file=out/'fixed-map-context.json';fixed_map_descriptor=jwrite(fixed_map_file,fixed_map)
    initialization=json.loads(lineage_raw[str(prior_initialization_path)])
    initialization['metadata_compatibility']=descriptor(receipt_path)
    initialization['building_definitions']=d
    initialization['unit_production_metadata']=u
    initialization['building_request_metadata']=s
    initialization['metadata_compiler_context']=context_descriptor
    initialization['configuration_qualification']=qualification_descriptor
    initialization['navigation_reference']=fixed_map_descriptor
    initialization_file=out/'initialization.json';initialization_descriptor=jwrite(initialization_file,initialization)
    bundle=json.loads(lineage_raw[str(prior_bundle_path)])
    bundle['initialization']=initialization_descriptor
    bundle['unit_metadata']=u
    bundle['static_clearance']=fixed_map_descriptor
    bundle_file=out/'data-bundle.json';bundle_descriptor=jwrite(bundle_file,bundle)
    index={'kind':'NativeS049MetadataOnlyProjectionIndex','native_artifact_sha256':native_sha,'contexts':[{'selection':{'catalog_id':'S049','size':'Standard','difficulty':'Regular','seed':104734,'setup_hash':971644661}}],'metadata_epoch':epoch,'metadata_epoch_context':context_descriptor,'configuration_qualification':qualification_descriptor,'source_initialization':initialization_descriptor,'source_data_bundle':bundle_descriptor,'prior_native_artifact_sha256':PRIOR_NATIVE_SHA,'prior_projection_sha256':PRIOR_PROJECTION_SHA,'metadata_semantics_equal_to_prior':True,'building_definitions':d,'unit_production_metadata':u,'building_request_metadata':s,'runtime_admission':False,'native_launch_provider_selected':False,'physical_map_created_or_changed':False,'mission_ready':False}
    if receipt_path.read_bytes()!=raw or historical_path.read_bytes()!=historical_raw: raise ValueError('S049 epoch input changed during projection')
    if any(Path(path).read_bytes()!=raw_bytes for path,raw_bytes in lineage_raw.items()): raise ValueError('Preserved S049 r6 inputs changed during projection')
    for rel,digest in relative.items():
        if sha((root/rel).read_bytes())!=digest: raise ValueError('S049 source changed during projection: '+rel)
    for path,raw_bytes in projector_inputs.items():
        if Path(path).read_bytes()!=raw_bytes: raise ValueError('S049 projection code changed during projection: '+path)
    jwrite(out/'projection-index.json',index)
    return index

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--receipt',required=True);p.add_argument('--output',required=True);p.add_argument('--root',default='/Volumes/T7/Projects/WarlineCapture');a=p.parse_args()
    result=project(a.receipt,a.output,a.root)
    epoch=json.loads(Path(result['metadata_epoch']['path']).read_text())
    print('Projected additive S049 metadata-only epoch; source pin count='+str(epoch['source_pin_count']))
