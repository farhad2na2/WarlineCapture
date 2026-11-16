#!/usr/bin/env python3
"""Project verified historical S003 factory facts for a compiled source-only target."""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASELINE = Path(__file__).with_name('project_s003_current_factory_metadata.py')
CAPTURE_SHA = 'b614fe9c80d1c975115b9952454a68d230e0420504c2970ef5ba9fa7dfe39aa4'


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def read_pin(descriptor):
    if not isinstance(descriptor, dict) or set(descriptor) != {'path', 'sha256'}:
        raise ValueError('Exact file descriptor required')
    path = Path(descriptor['path']).resolve(strict=True)
    if not path.is_relative_to(ROOT) or not path.is_file() or sha(path) != descriptor['sha256']:
        raise ValueError('Pinned repository input changed: ' + str(path))
    return path, json.loads(path.read_bytes())


def verify_pins(pins):
    if not isinstance(pins, dict) or not pins:
        raise ValueError('Consumed factory closure required')
    for name, expected in pins.items():
        path = Path(name)
        if not path.is_absolute() or not path.is_file() or sha(path) != expected:
            raise ValueError('Consumed factory input changed: ' + name)


def project(receipt_path, output):
    receipt_path = Path(receipt_path).resolve(strict=True)
    if not receipt_path.is_relative_to(ROOT):
        raise ValueError('Repository-owned receipt required')
    receipt = json.loads(receipt_path.read_bytes())
    flags = ('world_allocated', 'native_s075_capture', 'runtime_loader_admitted', 'source_fit_certified', 'mission_ready')
    if receipt.get('kind') != 'S003FactoryExporterSourceControl' or receipt.get('status') != 'PassedSourceOnlyCompilerContext' or any(receipt.get(k) is not False for k in flags):
        raise ValueError('Completed source-only original exporter receipt required')
    context_path, context = read_pin(receipt['source_context'])
    capture_path, capture = read_pin(receipt['origin_capture'])
    if receipt['origin_capture']['sha256'] != CAPTURE_SHA or context.get('native_factory_reference') != receipt['origin_capture']:
        raise ValueError('Exact immutable S003 factory capture required')
    if (context.get('schema_version'), context.get('kind'), context.get('authority')) != (2, 'OriginalS003DerivedNativeFactoryContext', 'ActualCompilerContext_HistoricalS003FactoryFacts_SourceOnly'):
        raise ValueError('Exact S003 source-only compiler projection required')
    if any(context.get(k) is not False for k in ('world_allocated', 'runtime_loader_admitted', 'source_fit_certified', 'mission_ready')) or context.get('identities_allocated') != 0:
        raise ValueError('Factory compiler context scope changed')
    target = context['target_context']
    if target != receipt['target'] or receipt.get('origin_catalog_id') != 'S003':
        raise ValueError('Original compiled target or captured origin changed')
    prior_path, prior = read_pin(receipt['previous_projection'])
    metadata = context['derived_metadata']
    if metadata != prior:
        raise ValueError('Original compiled factory projection differs from prior control')
    for key in ('exporter_source', 'compile_response', 'compile_log', 'run_log', 'baseline_context', 'previous_baseline_context', 'baseline_run_log'):
        descriptor = receipt[key]
        path = Path(descriptor['path']).resolve(strict=True)
        if not path.is_relative_to(ROOT) or sha(path) != descriptor['sha256']:
            raise ValueError('Exporter evidence changed: ' + key)
    native = capture['context']['metadata']
    for name in ('providers', 'recipes', 'delivery_seconds', 'units', 'transports'):
        if metadata[name] != native[name]:
            raise ValueError('Captured factory section changed: ' + name)
    native_rows = {row['prefab']['key']: row for row in native['buildings']}
    rows = metadata['buildings']
    if len(native_rows) != len(native['buildings']) or len(rows) != len(native_rows):
        raise ValueError('Exact captured building domain required')
    for i, row in enumerate(rows):
        original = copy.deepcopy(native_rows.get(row['prefab']['key']))
        if original is None:
            raise ValueError('Projected building missing from capture')
        original['index'] = i
        if row != original:
            raise ValueError('Projected building differs from captured factory row')
    if len({row['prefab']['key'] for row in rows}) != len(rows):
        raise ValueError('Duplicate projected building')
    pins = context['consumed_input_pins']
    verify_pins(pins)
    spec = importlib.util.spec_from_file_location('s003_factory_base', BASELINE)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    payload = module._project_payload_checked(capture, CAPTURE_SHA, capture['source_pins'], module.read_factory_sources(ROOT))
    by_definition = {row['key']: row for row in payload['building-definitions.json']['definitions']}
    by_configured = {row['key']: row for row in payload['configured-metadata.json']['rows']}
    payload['building-definitions.json']['definitions'] = [copy.deepcopy(by_definition[row['prefab']['key']]) for row in rows]
    configured = []
    for i, row in enumerate(rows):
        item = copy.deepcopy(by_configured[row['prefab']['key']])
        item['definition_index'] = i
        configured.append(item)
    payload['configured-metadata.json']['rows'] = configured
    origin = {key: context['native_origin'][key] for key in ('catalog_id', 'size', 'difficulty', 'seed', 'setup_hash', 'metadata_key_sha256')}
    origin.update(kind=capture['kind'], artifact_sha256=CAPTURE_SHA)
    context_descriptor = {'path': str(context_path), 'sha256': sha(context_path)}
    relative_pins = {str(Path(name).relative_to(ROOT)): digest for name, digest in pins.items()}
    kinds = {'building-definitions.json': 'CompilerContextDerivedSkirmishBuildingDefinitions',
             'unit-production-metadata.json': 'CompilerContextDerivedSkirmishUnitProductionMetadata',
             'configured-metadata.json': 'CompilerContextDerivedConfiguredBuildingMetadata'}
    for name, artifact in payload.items():
        artifact.update(schema_version=3, kind=kinds[name], authority='OriginalCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050',
                        catalog_id=target['catalog_id'], seed=target['seed'], setup_hash=target['setup_hash'],
                        metadata_compiler_context=context_descriptor, native_origin=origin, native_S050_capture=False)
        if name != 'configured-metadata.json':
            artifact['source_pins'] = relative_pins
    output = Path(output).absolute()
    if not output.is_relative_to(ROOT / 'T Logs') or output.exists():
        raise ValueError('Fresh repository T Logs output required')
    output.mkdir(parents=True)
    def write(name, value):
        path = output / name
        with path.open('x') as stream:
            json.dump(value, stream, indent=2, ensure_ascii=False)
            stream.write('\n')
        return {'path': str(path), 'sha256': sha(path)}
    definitions = write('building-definitions.json', payload['building-definitions.json'])
    payload['configured-metadata.json']['reference'] = definitions
    units = write('unit-production-metadata.json', payload['unit-production-metadata.json'])
    configured_descriptor = write('configured-metadata.json', payload['configured-metadata.json'])
    verify_pins(pins)
    if sha(context_path) != context_descriptor['sha256'] or sha(capture_path) != CAPTURE_SHA or sha(prior_path) != receipt['previous_projection']['sha256']:
        raise ValueError('Projection input changed during generation')
    report = {'kind': 'S003DerivedFactoryConsumerProjection', 'status': 'ProjectedSourceOnly',
              'target_context': target, 'native_origin': origin, 'compiler_context': context_descriptor,
              'source_receipt': {'path': str(receipt_path), 'sha256': sha(receipt_path)},
              'artifacts': {'building_definitions': definitions, 'unit_production_metadata': units, 'building_request_metadata': configured_descriptor},
              'counts': {'buildings': len(rows), 'units': len(metadata['units']), 'transports': len(metadata['transports'])},
              'consumed_factory_pins': len(pins), 'world_allocated': False, 'native_target_capture': False,
              'runtime_admission': False, 'source_fit_certified': False, 'mission_ready': False}
    write('receipt.json', report)
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--receipt', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    print(json.dumps(project(args.receipt, args.output)))
