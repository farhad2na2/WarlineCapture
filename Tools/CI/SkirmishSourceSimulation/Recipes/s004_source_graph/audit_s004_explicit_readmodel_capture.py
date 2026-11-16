#!/usr/bin/env python3
"""Audit an explicitly named S004 data-only building readmodel capture."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
MARKER = '[SkirmishS004ConfiguredSpawnableReadModel] result=Passed keys=9 scene=0 missionBuildings=0 readiness=0'
METHOD = 'Game.Editor.SkirmishBakedActorSnapshotExporter.RunS004ConfiguredSpawnableReadModelReferenceData'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('raw', 'log', 'finalizer', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--wrapper-exit', type=int, required=True)
    args = parser.parse_args()
    raw, log, finalizer = (getattr(args, n).resolve(strict=True) for n in ('raw', 'log', 'finalizer'))
    before = {str(p): sha(p) for p in (raw, log, finalizer)}
    data = json.loads(raw.read_bytes())
    final = json.loads(finalizer.read_bytes())
    text = log.read_text()
    if (args.wrapper_exit != 0 or METHOD not in text or sum(line.endswith(MARKER) for line in text.splitlines()) != 1 or
        final.get('schema') != 's004-configured-spawnable-readmodel-receipt-v1' or
        final.get('rawExportSha256') != before[str(raw)] or final.get('sourcePinsVerified') is not True or
        final.get('missionReadiness') is not False):
        raise ValueError('Checked wrapper and independent readmodel finalizer required')
    pins = data.get('source_pins')
    if not isinstance(pins, dict) or len(pins) < 9000:
        raise ValueError('Complete source pins required')
    def verify_pins():
        for name, expected in pins.items():
            path = Path(name)
            path = path if path.is_absolute() else ROOT / path
            if not path.is_file() or sha(path) != expected:
                raise ValueError('Readmodel source changed: ' + name)
    verify_pins()
    if any(sha(Path(name)) != expected for name, expected in before.items()):
        raise ValueError('Readmodel evidence changed')
    verify_pins()
    result = {'status': 'Passed', 'scope': 'CurrentDataOnlyReadmodelCapture_NoRuntimeAdmission',
              'capture_sha256': before[str(raw)], 'wrapper_exit_observed': args.wrapper_exit, 'marker_count': 1,
              'source_pins_checked': len(pins), 'finalizer_receipt_sha256': before[str(finalizer)],
              'mission_world_created': False, 'runtime_admission': False, 'mission_ready': False,
              'input_sha256': before}
    with args.output.open('x') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print(json.dumps({'status': result['status'], 'source_pins_checked': len(pins), 'mission_ready': False}))


if __name__ == '__main__':
    main()
