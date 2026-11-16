#!/usr/bin/env python3
"""Remove draft annotations from native S026 values without qualifying them."""
import argparse, hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / 'Logs/SkirmishProduction/s026-standard-provisional-route-draft-20261116-r4/layout.json'
EXPECTED = 'eafe5b2ca69153a35ce50038438509dff54c60cc875c763905370e4b5017d51c'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    output = args.output_dir.resolve()
    if not output.is_relative_to(ROOT) or output.exists():
        raise ValueError('Fresh repository-local output directory required')
    raw = SOURCE.read_bytes()
    if hashlib.sha256(raw).hexdigest() != EXPECTED:
        raise ValueError('Pinned provisional layout changed')
    layout = json.loads(raw)
    removed = {}
    for name in ('anchors', 'pads'):
        count = 0
        for row in layout[name]:
            if 'HypothesisOnly' in row:
                if row.pop('HypothesisOnly') is not True:
                    raise ValueError('Unexpected annotation value')
                count += 1
        removed[name] = count
    if removed != {'anchors': 19, 'pads': 2}:
        raise ValueError('Unexpected annotation census')
    encoded = (json.dumps(layout, indent=2, ensure_ascii=False) + '\n').encode()
    output.mkdir(parents=True)
    (output / 'layout.json').write_bytes(encoded)
    receipt = {'kind': 'S026NativeShapeDraftPreparation', 'status': 'UnqualifiedDraft',
               'sourceSha256': EXPECTED, 'outputSha256': hashlib.sha256(encoded).hexdigest(),
               'removedNonserializedAnnotations': removed,
               'scope': 'Native-shaped data only; provisional geometry and times are unchanged.',
               'compilerAccepted': False, 'sourceFit': False, 'missionReady': False}
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    main()
