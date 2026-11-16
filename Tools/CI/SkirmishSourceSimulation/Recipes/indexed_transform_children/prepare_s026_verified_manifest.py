"""Derive an experimental S026 manifest with per-call transform parity checks."""
import argparse
import json
from pathlib import Path

SOURCE = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-614-SourceMissionTransformBridge.cs'
ROOT = 'Tools/CI/SkirmishSourceSimulation/Recipes/indexed_transform_children/'

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--manifest', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
args = p.parse_args()
manifest = json.loads(args.manifest.read_bytes())
rows = [r for r in manifest['sources'] if r.get('path') == SOURCE]
if len(rows) != 1 or rows[0].get('kind') != 'finite_adapter':
    raise ValueError('Exact S026 source transform adapter required')
row = rows[0]
row.update(kind='current_original', generator=ROOT+'generate_s026_verified.py',
           generator_inputs=[SOURCE], generator_helpers=[ROOT+'generate_verified.py', ROOT+'generate.py'],
           generator_output='SourceMissionTransformBridgeVerified.cs', output='SourceMissionTransformBridgeVerified.cs',
           scope='Experimental S026 per-call original versus indexed transform check; not readiness')
row.pop('path')
with args.output.open('x') as stream:
    json.dump(manifest, stream, indent=2)
    stream.write('\n')
