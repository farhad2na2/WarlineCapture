#!/usr/bin/env python3
"""Prepare a noncanonical S003 manifest for the fresh witness experiment."""
import argparse
import json
from pathlib import Path

GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/fresh_witness_digest/generate.py'
INPUTS = ['Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs',
          'Assets/Game/Scripts/Components/GridComponents.cs',
          'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs']
OUTPUT = 'SourceExperimentalFreshWitnessDigest.cs'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_bytes())
    rows = [row for row in manifest['sources'] if row.get('id') == 'Shared_GridWalkableSourceWitness.cs']
    if len(rows) != 1 or rows[0].get('kind') != 'native_direct' or rows[0].get('path') != INPUTS[0]:
        raise ValueError('Exact original witness entry required')
    row = rows[0]
    row.pop('path')
    row.update(kind='current_original', generator=GENERATOR, generator_inputs=INPUTS,
               generator_output=OUTPUT, output=OUTPUT,
               scope='Opt-in source-only fresh digest experiment; original default branch retained')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('x') as stream:
        json.dump(manifest, stream, indent=2)
        stream.write('\n')
    print('[S003FreshWitnessDigestManifest] result=Prepared experimental=1')


if __name__ == '__main__':
    main()
