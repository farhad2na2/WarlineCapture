"""Prepare noncanonical S003 source-host pooled-digest experiment."""
import argparse
import json
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--manifest', required=True, type=Path)
p.add_argument('--output', required=True, type=Path)
a = p.parse_args(); manifest = json.loads(a.manifest.read_bytes())
base = 'Tools/CI/SkirmishSourceSimulation/Recipes/pooled_witness_digest/'
witness = 'Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs'
grid = 'Assets/Game/Scripts/Components/GridComponents.cs'
shim = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs'
for ident, expected_kind, path, gen, inputs, output in [
    ('Shared_GridWalkableSourceWitness.cs', 'native_direct', witness, 'generate_witness.py', [witness, grid, shim], 'SourceExperimentalPooledWitness.cs'),
    ('S026_059_AutonomousEcs.cs', 'finite_adapter', shim, 'generate_shim.py', [shim], 'SourceExperimentalPooledBuffer.cs')]:
    matches = [row for row in manifest['sources'] if row.get('id') == ident]
    if len(matches) != 1 or matches[0].get('kind') != expected_kind or matches[0].get('path') != path:
        raise ValueError('Exact original source entry required: ' + ident)
    row = matches[0]; row.pop('path')
    row.update(kind='current_original', generator=base + gen, generator_inputs=inputs,
               generator_output=output, output=output, scope='Experimental source-only pooled witness comparison')
a.output.parent.mkdir(parents=True, exist_ok=True)
with a.output.open('x') as f:
    json.dump(manifest, f, indent=2); f.write('\n')
print('[SourcePooledWitnessManifest] result=Prepared experimental=1')
