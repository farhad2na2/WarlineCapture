#!/usr/bin/env python3
"""Create an opt-in S026 host manifest for retry lifecycle observation."""
import argparse
import json
from pathlib import Path

SOURCES = (
    'Assets/Game/Scripts/Systems/UnitPathRetry.cs',
    'Assets/Game/Scripts/Systems/UnitManualMoveRetrySystem.cs',
)
GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/s026_retry_lifecycle_trace/generate.py'
OUTPUTS = ('SourceUnitPathRetryTrace.cs', 'SourceUnitManualMoveRetryTrace.cs')

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_bytes())
    for source, output in zip(SOURCES, OUTPUTS):
        rows = [row for row in manifest['sources'] if row.get('path') == source]
        if len(rows) != 1 or rows[0].get('kind') != 'native_direct':
            raise ValueError('Exact original retry owner entries required: ' + source)
        row = rows[0]
        row.update(kind='current_original', generator=GENERATOR, generator_inputs=list(SOURCES),
                   generator_output=output, output=output,
                   scope='Opt-in source-only retry lifecycle observation; no gameplay policy or readiness')
        row.pop('path')
    with args.output.open('x') as stream:
        json.dump(manifest, stream, indent=2)
        stream.write('\n')

if __name__ == '__main__':
    main()
