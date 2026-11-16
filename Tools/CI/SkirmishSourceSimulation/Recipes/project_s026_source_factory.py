#!/usr/bin/env python3
"""Project verified fresh S026 source factory facts into pinned schema-3 templates."""
import argparse
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
EXPECTED_KIND = 'OriginalCompilerDerivedNativeFactoryContext'
TEMPLATE_KINDS = {
    'building_definitions': 'CompilerContextDerivedSkirmishBuildingDefinitions',
    'unit_production_metadata': 'CompilerContextDerivedSkirmishUnitProductionMetadata',
    'building_request_metadata': 'CompilerContextDerivedConfiguredBuildingMetadata',
}


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def read_descriptor(descriptor):
    if not isinstance(descriptor, dict) or set(descriptor) != {'path', 'sha256'}:
        raise ValueError('Exact path+sha256 input descriptor required')
    path = Path(descriptor['path']).resolve(strict=True)
    if not path.is_relative_to(ROOT) or not path.is_file() or digest(path) != descriptor['sha256']:
        raise ValueError('Pinned repository input changed: ' + str(path))
    return path, json.loads(path.read_bytes())


def project(context_path, receipt_path, initialization_path, output):
    context_path = Path(context_path).resolve(strict=True)
    receipt_path = Path(receipt_path).resolve(strict=True)
    initialization_path = Path(initialization_path).resolve(strict=True)
    for path in (context_path, receipt_path, initialization_path):
        if not path.is_relative_to(ROOT):
            raise ValueError('Repository input required')
    context_raw = context_path.read_bytes()
    context = json.loads(context_raw)
    receipt = json.loads(receipt_path.read_bytes())
    context_sha = hashlib.sha256(context_raw).hexdigest()
    if (receipt.get('kind') != 'S026BoundAuthorFactoryContextExport'
            or receipt.get('status') != 'PassedOriginalOwnerExport'
            or receipt.get('process_exit') != 0 or receipt.get('timed_out') is not False
            or receipt.get('source_only') is not True or receipt.get('world_allocated') is not False
            or receipt.get('source_fit_verified') is not False or receipt.get('mission_ready') is not False
            or receipt.get('context_sha256') != context_sha):
        raise ValueError('Completed source-only fresh S026 compiler export required')
    export_directory = receipt_path.parent / 's026'
    authoring_result_path = export_directory / 'authoring-result.json'
    process_log_path = export_directory / 'process.log'
    if (not authoring_result_path.is_file() or not process_log_path.is_file()
            or digest(authoring_result_path) != receipt.get('authoring_result_sha256')
            or digest(process_log_path) != receipt.get('process_log_sha256')):
        raise ValueError('Fresh export result or process log no longer matches its receipt')
    if (context.get('schema_version') != 1 or context.get('kind') != EXPECTED_KIND
            or context.get('authority') != 'ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050'
            or context.get('identities_allocated') != 0
            or any(context.get(k) is not False for k in
                   ('world_allocated', 'native_S050_capture', 'source_fit_certified',
                    'active_mission_water_qualified', 'mission_ready', 'runtime_loader_admitted'))):
        raise ValueError('Explicit source-only immutable factory context required')
    target = context['target_context']
    if (target.get('catalog_id') != 'S026' or target.get('definition_id') != 'skirmish.s026'
            or target.get('content_version') != 2 or target.get('seed') != 104755
            or target.get('size') != 'Standard' or target.get('difficulty') != 'Regular'
            or target.get('setup_hash') != receipt.get('target_setup_hash')
            or target.get('operation_map_id') != 'opmap.skirmish.city_crossroads'):
        raise ValueError('Fresh alias-bound S026 target context mismatch')
    if not context.get('checks') or not all(value is True for value in context['checks'].values()):
        raise ValueError('Factory exporter did not pass all equality checks')
    metadata = context['derived_metadata']
    if len(metadata.get('buildings', [])) != 9 or len(metadata.get('units', [])) != 16 or len(metadata.get('transports', [])) != 1 or len(metadata.get('recipes', [])) != 16:
        raise ValueError('Unexpected native factory domain cardinality')

    initial_path = initialization_path
    initial = json.loads(initial_path.read_bytes())
    old_context_path, old_context = read_descriptor(initial['metadata_compiler_context'])
    _, native_reference = read_descriptor(context['native_factory_reference'])
    template_paths, templates = {}, {}
    for key in TEMPLATE_KINDS:
        template_paths[key], templates[key] = read_descriptor(initial[key])
    if any(t.get('metadata_compiler_context') != initial['metadata_compiler_context'] for t in templates.values()):
        raise ValueError('Projection templates must bind the pinned original compiler context')
    if (old_context.get('kind') != EXPECTED_KIND
            or old_context.get('native_factory_reference') != context['native_factory_reference']
            or old_context.get('native_origin') != context['native_origin']):
        raise ValueError('Fresh context and template compiler context have different native provenance')
    for key, artifact in templates.items():
        if (artifact.get('schema_version') != 3 or artifact.get('kind') != TEMPLATE_KINDS[key]
                or artifact.get('authority') != 'OriginalCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050'
                or artifact.get('native_S050_capture') is not False
                or artifact.get('full_mission_initialized') is not False
                or artifact.get('native_visual_readiness') is not False
                or artifact.get('unsupported_values') != []
                or any(x in artifact for x in ('mission_logical_binding', 'operation_map_id', 'layout_id'))):
            raise ValueError('Exact source-only schema-3 template header required: ' + key)
        origin = artifact.get('native_origin', {})
        if (origin.get('kind') != native_reference.get('kind') or origin.get('catalog_id') != 'S026'
                or origin.get('artifact_sha256') != context['native_factory_reference']['sha256']
                or any(origin.get(k) != context['native_origin'].get(k) for k in
                       ('catalog_id', 'size', 'difficulty', 'seed', 'setup_hash', 'metadata_key_sha256'))):
            raise ValueError('Historical S026 native factory origin differs: ' + key)

    old = old_context['derived_metadata']
    sections = ('providers', 'recipes', 'delivery_seconds', 'buildings', 'units', 'transports')
    if any(metadata[k] != old[k] for k in sections):
        raise ValueError('Fresh compiler factory section differs from pinned template compiler context')
    fresh_catalog = copy.deepcopy(metadata['catalog_inputs'])
    old_catalog = copy.deepcopy(old['catalog_inputs'])
    fresh_catalog.pop('catalog_id', None)
    old_catalog.pop('catalog_id', None)
    if fresh_catalog != old_catalog or metadata['catalog_inputs'].get('catalog_id') != 'S026':
        raise ValueError('S026 factory catalog inputs differ from pinned source template')

    buildings, units = metadata['buildings'], metadata['units']
    definitions = [{'key': row['prefab']['key'], 'prefab': row['prefab'], 'fields': row['fields']}
                   for row in buildings]
    unit_rows = [dict(copy.deepcopy(row), key=row['prefab']['key']) for row in units]
    configured_rows = [
        {'definition_index': row['index'], 'key': row['prefab']['key'], 'prefab': row['prefab'],
         'display_name': row['configured_entry']['fields']['DisplayName']['value'],
         'can_request': row['configured_entry']['fields']['CanRequest']['value'],
         'construction_catalog_index': None if row['construction_index'] < 0 else row['construction_index'],
         'scene_catalog_index': None if row['scene_index'] < 0 else row['scene_index']}
        for row in buildings]
    if (definitions != templates['building_definitions']['definitions']
            or unit_rows != templates['unit_production_metadata']['units']
            or metadata['transports'] != templates['unit_production_metadata']['transports']
            or configured_rows != templates['building_request_metadata']['rows']):
        raise ValueError('Factory projection differs from pinned building/unit/transport/configured templates')

    source_inputs = {
        str(path): digest(path) for path in
        (context_path, receipt_path, initialization_path, old_context_path,
         authoring_result_path, process_log_path, *template_paths.values(), Path(__file__).resolve())
    }
    pins = context.get('consumed_input_pins', {})
    if not pins:
        raise ValueError('Fresh factory export must include consumed source pins')
    for name, expected in pins.items():
        path = Path(name)
        if not path.is_file() or digest(path) != expected:
            raise ValueError('Fresh consumed source pin changed: ' + name)
        source_inputs[name] = expected
    for descriptor in (context['native_factory_reference'], context['native_origin_descriptor']):
        path, _ = read_descriptor(descriptor)
        source_inputs[str(path)] = digest(path)
    _, native_origin = read_descriptor(context['native_origin_descriptor'])
    if native_origin.get('native_metadata') != context['native_factory_reference']:
        raise ValueError('Native factory and origin descriptors differ')
    if digest(context_path) != context_sha:
        raise ValueError('Source context or receipt changed during validation')
    for name, expected in source_inputs.items():
        path = Path(name)
        if not path.is_file() or digest(path) != expected:
            raise ValueError('Consumed input changed before projection: ' + name)

    output = Path(output).resolve()
    if not output.is_relative_to(ROOT):
        raise ValueError('Repository-owned output required')
    output.mkdir(parents=True, exist_ok=False)
    context_descriptor = {'path': str(context_path), 'sha256': context_sha}

    def write(name, value):
        path = output / name
        path.write_text(json.dumps(value, indent=2) + '\n')
        return {'path': str(path), 'sha256': digest(path)}

    projections = {}
    filenames = {'building_definitions': 'building-definitions.json',
                 'unit_production_metadata': 'unit-production-metadata.json',
                 'building_request_metadata': 'configured-metadata.json'}
    for key, filename in filenames.items():
        artifact = copy.deepcopy(templates[key])
        artifact.update(catalog_id='S026', seed=target['seed'], setup_hash=target['setup_hash'],
                       metadata_compiler_context=context_descriptor)
        if key == 'building_request_metadata':
            if artifact['reference'] != initial['building_definitions']:
                raise ValueError('Original configured definition reference changed')
            artifact['reference'] = projections['building_definitions']
        projections[key] = write(filename, artifact)
    for name, expected in source_inputs.items():
        path = Path(name)
        if not path.is_file() or digest(path) != expected:
            raise ValueError('Consumed input changed after projection: ' + name)
    report = {
        'kind': 'S026SourceFactoryProjection', 'schema_version': 1,
        'status': 'ProjectedSourceOnly', 'target_context': target,
        'context': context_descriptor, 'inputs': source_inputs,
        'consumed_factory_pins': len(pins), 'projections': projections,
        'factory_equality': {'building_definitions': len(definitions), 'units': len(unit_rows),
                             'transports': len(metadata['transports']),
                             'configured_rows': len(configured_rows), 'recipes': len(metadata['recipes'])},
        'native_origin_catalog_id': context['native_origin']['catalog_id'],
        'world_allocated': False, 'runtime_admission': False,
        'source_fit_certified': False, 'mission_ready': False,
        'factory_template_baseline': {'path': str(initialization_path), 'sha256': digest(initialization_path),
                                      'role': 'S026 shared-factory comparison templates only; never an initializer'},
        'scope': 'Fresh S026 compiler context and matching historical native factory facts only; no layout, mission initialization, native S050 capture, gameplay, or readiness claim.'
    }
    write('receipt.json', report)
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    for option in ('context', 'receipt', 'initialization', 'output'):
        parser.add_argument('--' + option, required=True)
    args = parser.parse_args()
    result = project(args.context, args.receipt, args.initialization, args.output)
    print(json.dumps({'status': result['status'], 'projections': len(result['projections']),
                      'factory_equality': result['factory_equality'], 'mission_ready': False}))
