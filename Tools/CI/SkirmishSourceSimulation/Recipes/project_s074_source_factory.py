#!/usr/bin/env python3
"""Project a verified S074 source factory context without claiming native readiness."""
import argparse
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
TARGET = ('S074', 'skirmish.s074', 104803, 'Standard', 'Regular')
QUALIFIED_EXPORTS = {
    ('29df35cbecc71610022be93b95fc95056357e5db0397dc7d5a76b81795ee2df4',
     '7b87a8f8898dfde0bc0ec9da145c32783d4bea9575bfcd47c9eb411d45eaf511'): 977354998,
    ('71a178b5c826f0a79251ba72dcf2940c2bbefd435de45594579b7640eea4e04c',
     '17f3b26c3f1ab27442155e69309f380dccd163be531cafa9cba98b11d1387bc4'): 231709577,
    ('254fa7f2cf9bb76a0cf8e26e73f5cbcfd2118e06f14d5b56a84b0004f2911d6c',
     '5b66466995ea7a27090dee79d073dbedc0e34636eabafa0e25ca015ed77359d6'): 1716123768,
}

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()

def read_descriptor(descriptor):
    if not isinstance(descriptor, dict) or set(descriptor) != {'path', 'sha256'}:
        raise ValueError('Exact input descriptor required')
    path = Path(descriptor['path']).resolve(strict=True)
    if not path.is_relative_to(ROOT) or not path.is_file() or digest(path) != descriptor['sha256']:
        raise ValueError('Pinned repository input changed: ' + str(path))
    return path, json.loads(path.read_bytes())

def _project_core(context_path, receipt_path, initialization_path, output, replay_setup_hash=None):
    context_path = Path(context_path).resolve(strict=True)
    receipt_path = Path(receipt_path).resolve(strict=True)
    initialization_path = Path(initialization_path).resolve(strict=True)
    for path in (context_path, receipt_path, initialization_path):
        if not path.is_relative_to(ROOT):
            raise ValueError('Repository input required')
    raw = context_path.read_bytes(); context = json.loads(raw); receipt = json.loads(receipt_path.read_bytes())
    if replay_setup_hash is None:
        setup_hash = QUALIFIED_EXPORTS.get((hashlib.sha256(raw).hexdigest(), digest(receipt_path)))
        if setup_hash is None:
            raise ValueError('Exact retained S074 factory export and receipt required')
    else:
        setup_hash = replay_setup_hash
        if type(setup_hash) is not int or setup_hash != context.get('target_context', {}).get('setup_hash'):
            raise ValueError('Replay setup hash must come from the observed exporter result')
    if receipt.get('status') != 'PassedSourceFactoryContext' or receipt.get('context_sha256') != hashlib.sha256(raw).hexdigest():
        raise ValueError('Actual passed source factory export required')
    if receipt.get('kind') != 'S074OriginalFactoryContextDiagnostic' or receipt.get('process_exit') != 0 or receipt.get('timed_out') is not False or receipt.get('source_only') is not True or any(receipt.get(k) is not False for k in ('world_allocated','native_context_qualified','source_fit_certified','mission_ready')) or not receipt.get('source_inputs'):
        raise ValueError('Completed source factory export required')
    target = context['target_context']
    if tuple(target[k] for k in ('catalog_id','definition_id','seed','size','difficulty')) != TARGET or target['setup_hash'] != setup_hash:
        raise ValueError('Exact compiled S074 target required')
    if context.get('schema_version') != 1 or context.get('kind') != 'OriginalCompilerDerivedNativeFactoryContext' or context.get('authority') != 'ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050' or context.get('identities_allocated') != 0 or any(context.get(k) is not False for k in ('world_allocated','native_S050_capture','source_fit_certified','active_mission_water_qualified','mission_ready','runtime_loader_admitted')):
        raise ValueError('Explicit source-only factory context required')
    if target['content_version'] != 2 or target['operation_map_id'] != 'opmap.skirmish.industrial_basin' or len(context['consumed_input_pins']) != 2082 or len(context['derived_metadata']['recipes']) != 16 or not all(v is True for v in context['checks'].values()):
        raise ValueError('Exact S074 factory domain required')
    pins = dict(context['consumed_input_pins'])
    if not pins:
        raise ValueError('Actual consumed factory pins required')
    initial = json.loads(initialization_path.read_bytes()); old_context_path, old_context = read_descriptor(initial['metadata_compiler_context'])
    _, native_reference = read_descriptor(context['native_factory_reference'])
    template_paths, templates = {}, {}
    for key in ('building_definitions','unit_production_metadata','building_request_metadata'):
        template_paths[key], templates[key] = read_descriptor(initial[key])
    if any(t.get('metadata_compiler_context') != initial['metadata_compiler_context'] for t in templates.values()):
        raise ValueError('Projection templates must bind original compiler context')
    kinds = {'building_definitions':'CompilerContextDerivedSkirmishBuildingDefinitions', 'unit_production_metadata':'CompilerContextDerivedSkirmishUnitProductionMetadata', 'building_request_metadata':'CompilerContextDerivedConfiguredBuildingMetadata'}
    for key, artifact in templates.items():
        if artifact.get('schema_version') != 3 or artifact.get('kind') != kinds[key] or artifact.get('authority') != 'OriginalCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050' or artifact.get('native_S050_capture') is not False or artifact.get('full_mission_initialized') is not False or artifact.get('native_visual_readiness') is not False or artifact.get('unsupported_values') != [] or any(x in artifact for x in ('mission_logical_binding','operation_map_id','layout_id')):
            raise ValueError('Exact source-only template header required: ' + key)
        origin = artifact.get('native_origin', {})
        if origin.get('kind') != native_reference.get('kind') or origin.get('catalog_id') != 'S026' or origin.get('artifact_sha256') != context['native_factory_reference']['sha256'] or any(origin.get(k) != context['native_origin'].get(k) for k in ('catalog_id','size','difficulty','seed','setup_hash','metadata_key_sha256')):
            raise ValueError('Historical native factory origin differs: ' + key)
    metadata = context['derived_metadata']; old = old_context['derived_metadata']
    sections = ('providers','recipes','delivery_seconds','buildings','units','transports')
    if any(metadata[k] != old[k] for k in sections) or context['native_origin'] != old_context['native_origin'] or context['native_factory_reference'] != old_context['native_factory_reference']:
        raise ValueError('Factory domain or native origin differs from pinned projections')
    a, b = copy.deepcopy(metadata['catalog_inputs']), copy.deepcopy(old['catalog_inputs'])
    a.pop('catalog_id'); b.pop('catalog_id')
    if a != b or metadata['catalog_inputs']['catalog_id'] != 'S074':
        raise ValueError('Factory catalog domain differs')
    buildings, units = metadata['buildings'], metadata['units']
    definitions = [{'key': r['prefab']['key'], 'prefab': r['prefab'], 'fields': r['fields']} for r in buildings]
    unit_rows = [dict(copy.deepcopy(r), key=r['prefab']['key']) for r in units]
    rows = [{'definition_index': r['index'], 'key': r['prefab']['key'], 'prefab': r['prefab'],
             'display_name': r['configured_entry']['fields']['DisplayName']['value'],
             'can_request': r['configured_entry']['fields']['CanRequest']['value'],
             'construction_catalog_index': None if r['construction_index'] < 0 else r['construction_index'],
             'scene_catalog_index': None if r['scene_index'] < 0 else r['scene_index']} for r in buildings]
    if len(buildings) != 9 or len(units) != 16 or definitions != templates['building_definitions']['definitions'] or unit_rows != templates['unit_production_metadata']['units'] or metadata['transports'] != templates['unit_production_metadata']['transports'] or rows != templates['building_request_metadata']['rows']:
        raise ValueError('Original factory projection contents differ')
    all_inputs = {str(p): digest(p) for p in (context_path, receipt_path, initialization_path, old_context_path, *template_paths.values(), Path(__file__).resolve())}
    for name, expected in receipt['source_inputs'].items():
        if name in all_inputs and all_inputs[name] != expected:
            raise ValueError('Conflicting source export pin: ' + name)
        all_inputs[name] = expected
    all_inputs[str(receipt_path.parent / 's074/authoring-result.json')] = receipt['result_sha256']
    all_inputs[str(receipt_path.parent / 's074/process.log')] = receipt['process_log_sha256']
    for descriptor in (context['native_factory_reference'], context['native_origin_descriptor']):
        p, _ = read_descriptor(descriptor); all_inputs[str(p)] = digest(p)
    _, native_origin = read_descriptor(context['native_origin_descriptor'])
    if native_origin.get('native_metadata') != context['native_factory_reference']:
        raise ValueError('Native factory and origin descriptors differ')
    def verify():
        for name, expected in {**pins, **all_inputs}.items():
            p = Path(name)
            if not p.is_file() or digest(p) != expected:
                raise ValueError('Consumed input changed: ' + name)
    verify()
    output = Path(output).resolve()
    if not output.is_relative_to(ROOT):
        raise ValueError('Repository-owned output required')
    output.mkdir(parents=True, exist_ok=False)
    context_desc = {'path': str(context_path), 'sha256': hashlib.sha256(raw).hexdigest()}
    def write(name, value):
        path = output / name
        path.write_text(json.dumps(value, indent=2) + '\n')
        return {'path': str(path), 'sha256': digest(path)}
    projections = {}
    for key, filename in (('building_definitions','building-definitions.json'), ('unit_production_metadata','unit-production-metadata.json'), ('building_request_metadata','configured-metadata.json')):
        artifact = copy.deepcopy(templates[key])
        artifact.update(catalog_id=target['catalog_id'], seed=target['seed'], setup_hash=target['setup_hash'], metadata_compiler_context=context_desc)
        if key == 'building_request_metadata':
            if artifact['reference'] != initial['building_definitions']:
                raise ValueError('Original configured definition reference changed')
            artifact['reference'] = projections['building_definitions']
        projections[key] = write(filename, artifact)
    verify()
    report = {'kind':'S074SourceFactoryProjection', 'status':'ProjectedSourceOnly', 'target_context':target,
              'context':context_desc, 'inputs':all_inputs, 'consumed_factory_pins':len(pins), 'projections':projections,
              'native_origin_catalog_id':context['native_origin']['catalog_id'], 'world_allocated':False,
              'runtime_admission':False, 'source_fit_certified':False, 'mission_ready':False,
              'factory_template_baseline':{'path':str(initialization_path),'sha256':digest(initialization_path),
                                           'role':'Validated S026 shared factory equality templates only; never an S074 initializer'},
              'scope':'S074 compiler context and identical shared factory facts; native origin is historical S026; no S074 native capture or gameplay.'}
    write('receipt.json', report)
    return report

def project(context_path, receipt_path, initialization_path, output):
    """Legacy checked projection entry; retained digest allowlist remains in force."""
    return _project_core(context_path, receipt_path, initialization_path, output)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for option in ('context','receipt','initialization','output'):
        parser.add_argument('--' + option, required=True)
    args = parser.parse_args()
    report = project(args.context, args.receipt, args.initialization, args.output)
    print(json.dumps({'status':report['status'], 'projections':len(report['projections']), 'mission_ready':False}))
