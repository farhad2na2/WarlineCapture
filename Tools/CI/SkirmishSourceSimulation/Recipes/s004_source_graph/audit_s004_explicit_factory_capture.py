#!/usr/bin/env python3
"""Audit an explicitly named data-only S004 factory capture."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
MARKER = '[S004FullFactoryRunwayReference] result=Collected buildings=9 unsupported=0 mission=0 readiness=0'
METHOD = 'Game.Editor.SkirmishBakedActorSnapshotExporter.RunS004FullFactoryRunwayReferenceData'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('capture', 'log', 'historical', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--wrapper-exit', type=int, required=True)
    args = parser.parse_args()
    capture, log, historical = (getattr(args, n).resolve(strict=True) for n in ('capture', 'log', 'historical'))
    inputs = {str(p): sha(p) for p in (capture, log, historical)}
    data = json.loads(capture.read_bytes())
    old = json.loads(historical.read_bytes())
    text = log.read_text()
    if args.wrapper_exit != 0 or METHOD not in text or sum(line.endswith(MARKER) for line in text.splitlines()) != 1:
        raise ValueError('Observed wrapper exit, method and unique marker required')
    if (data.get('kind') != 'S004OriginalFullFactoryRunwayDefinitions' or
        data.get('authority') != 'OriginalS004ProductionOverlayAndDefinitionFactory' or
        data.get('seed') != 104733 or data.get('setup_hash') != 1130726651 or
        data.get('unsupported_values') != [] or data.get('runway_reader_supplied') is not True or
        data.get('mission_world_created') is not False or data.get('mission_ready') is not False):
        raise ValueError('Exact data-only source factory scope required')
    if len(data.get('buildings', [])) != 9 or data['buildings'] != old.get('buildings'):
        raise ValueError('Current and historical source factory rows differ')
    pins = data.get('source_pins')
    if not isinstance(pins, dict) or len(pins) < 9000:
        raise ValueError('Complete source pin set required')
    def verify_pins():
        for name, expected in pins.items():
            path = Path(name)
            path = path if path.is_absolute() else ROOT / path
            if not path.is_file() or sha(path) != expected:
                raise ValueError('Factory source pin changed: ' + name)
    verify_pins()
    if any(sha(Path(name)) != digest for name, digest in inputs.items()):
        raise ValueError('Capture evidence changed during audit')
    verify_pins()
    result = {'status': 'Passed', 'scope': 'CurrentDataOnlyFactoryCapture_NoRuntimeAdmission',
              'capture_sha256': inputs[str(capture)], 'wrapper_exit_observed': args.wrapper_exit,
              'marker_count': 1, 'mismatches': [], 'source_pins_checked': len(pins),
              'factory_buildings_equal_historical': True, 'historical_capture_current_qualified': False,
              'mission_world_created': False, 'runtime_admission': False, 'mission_ready': False,
              'input_sha256': inputs}
    with args.output.open('x') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print(json.dumps({'status': result['status'], 'source_pins_checked': len(pins), 'mission_ready': False}))


if __name__ == '__main__':
    main()
