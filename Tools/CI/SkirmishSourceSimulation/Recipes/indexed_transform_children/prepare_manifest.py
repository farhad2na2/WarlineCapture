"""Select only the opt-in indexed transform candidate in a derived manifest."""
import argparse
import json
from pathlib import Path

SOURCE = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-614-SourceMissionTransformBridge.s003.cs'
GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/indexed_transform_children/generate.py'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--verify-each-call', action='store_true')
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_bytes())
    rows = [row for row in manifest['sources'] if row.get('path') == SOURCE]
    if len(rows) != 1 or rows[0].get('kind') != 'finite_adapter':
        raise ValueError('Exact finite source transform adapter required')
    row = rows[0]
    generator = GENERATOR if not args.verify_each_call else 'Tools/CI/SkirmishSourceSimulation/Recipes/indexed_transform_children/generate_verified.py'
    generated = 'SourceMissionTransformBridgeIndexed.cs' if not args.verify_each_call else 'SourceMissionTransformBridgeVerified.cs'
    row.update(kind='current_original', generator=generator, generator_inputs=[SOURCE],
               generator_output=generated, output=generated,
               scope='Opt-in source-only transform child index; no native or readiness claim')
    if args.verify_each_call:
        row['generator_helpers'] = [GENERATOR]
    row.pop('path')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('x') as stream:
        json.dump(manifest, stream, indent=2)
        stream.write('\n')


if __name__ == '__main__':
    main()
