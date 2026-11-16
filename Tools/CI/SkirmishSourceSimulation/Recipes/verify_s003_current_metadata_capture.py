#!/usr/bin/env python3
"""Audit a fresh S003 data-only capture against actual pins and historical context."""
import argparse
import hashlib
import json
from pathlib import Path

MARKER = '[SkirmishS003MetadataCapture] result=Passed catalog=S003 seed=104732 scope=DataOnly native_context_qualified=false mission_ready=false'
METHOD = 'Game.Editor.SkirmishBakedActorSnapshotExporter.RunS003MetadataCapture'


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def unique(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise ValueError('Duplicate field: ' + key)
        value[key] = item
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('repo', 'capture', 'log', 'historical', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--wrapper-exit', type=int, required=True)
    args = parser.parse_args()
    root = args.repo.resolve(strict=True)
    capture = args.capture.resolve(strict=True)
    log = args.log.resolve(strict=True)
    historical = args.historical.resolve(strict=True)
    inputs = {str(path): digest(path) for path in (capture, log, historical)}
    data = json.loads(capture.read_bytes(), object_pairs_hook=unique)
    old = json.loads(historical.read_bytes(), object_pairs_hook=unique)
    log_text = log.read_text(errors='strict')
    if args.wrapper_exit != 0 or METHOD not in log_text or sum(line.endswith(MARKER) for line in log_text.splitlines()) != 1:
        raise ValueError('Checked wrapper, method and unique pass marker required')
    if data.get('kind') != 'NativeS003OriginalCompilerFactoryMetadataCapture' or data.get('schema_version') != 1:
        raise ValueError('Exact native metadata capture required')
    for key in ('full_mission_initialized', 'mission_world_created', 'runtime_state_transferred', 'source_fit_certified', 'native_context_qualified', 'mission_ready'):
        if data.get(key) is not False:
            raise ValueError('Data-only scope mismatch: ' + key)
    if data.get('utility_world_created') is not True or data.get('utility_world_disposed') is not True:
        raise ValueError('Disposed utility world required')
    context = data.get('context', {})
    if (context.get('catalog_id'), context.get('seed'), context.get('setup_hash')) != ('S003', 104732, 2394754492):
        raise ValueError('Exact S003 setup required')
    if context.get('wrong_context_hash_rejected') is not True or context.get('wrong_target_seed_rejected') is not True:
        raise ValueError('Original negative controls required')
    if context != old.get('context'):
        raise ValueError('Captured native context differs from retained context')
    graph = data['source_graph']
    graph_path = Path(graph['path']).resolve(strict=True)
    if not graph_path.is_relative_to(root) or digest(graph_path) != graph['sha256']:
        raise ValueError('Graph source changed')
    pins = data.get('source_pins')
    if not isinstance(pins, dict) or len(pins) < 8000:
        raise ValueError('Complete source pins required')
    for name, expected in pins.items():
        path = Path(name)
        path = (path if path.is_absolute() else root / path).resolve(strict=True)
        if not path.is_relative_to(root) or not path.is_file() or digest(path) != expected:
            raise ValueError('Current source differs: ' + name)
    if any(digest(Path(path)) != expected for path, expected in inputs.items()):
        raise ValueError('Capture evidence changed during audit')
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    result = {'status': 'Passed', 'scope': 'CurrentDataOnlyCapture_IndependentAudit_NotHistoricalFinalizerQualification',
              'capture_sha256': inputs[str(capture)], 'log_sha256': inputs[str(log)],
              'historical_capture_sha256': inputs[str(historical)], 'wrapper_exit_observed': args.wrapper_exit,
              'marker_count': 1, 'source_pin_entries': len(pins), 'source_pin_mismatches': [],
              'context_exact_match_to_historical': True, 'utility_world_disposed': True,
              'native_context_qualified': False, 'mission_ready': False}
    with output.open('x') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
