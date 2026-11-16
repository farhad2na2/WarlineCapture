"""Create a source-host-only manifest with opt-in witness timing."""
import argparse
import json
from pathlib import Path

WITNESS = 'Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs'
GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/witness_timing/generate.py'
OUTPUT = 'SourceExperimentalWitnessTiming.cs'

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--manifest', required=True, type=Path)
p.add_argument('--output', required=True, type=Path)
args = p.parse_args()
manifest = json.loads(args.manifest.read_bytes())
rows = [row for row in manifest['sources'] if row.get('id') == 'Shared_GridWalkableSourceWitness.cs']
if len(rows) != 1 or rows[0].get('kind') != 'native_direct' or rows[0].get('path') != WITNESS:
    raise ValueError('Exact original witness entry required')
row = rows[0]
row.pop('path')
row.update(kind='current_original', generator=GENERATOR, generator_inputs=[WITNESS],
           generator_output=OUTPUT, output=OUTPUT,
           scope='Experimental source-only timing around unchanged witness digest')
args.output.parent.mkdir(parents=True, exist_ok=True)
with args.output.open('x') as stream:
    json.dump(manifest, stream, indent=2)
    stream.write('\n')
print('[SourceWitnessTimingManifest] result=Prepared experimental=1')
