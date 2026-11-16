#!/usr/bin/env python3
"""Create an opt-in S026 host manifest for the original path admission observer."""
import argparse
import json
from pathlib import Path

SOURCE = 'Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs'
GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/s026_path_admission_trace/generate.py'
OUTPUT = 'SourcePathfindingAdmissionObservation.cs'
BUDGET_SOURCE = 'Assets/Game/Scripts/Systems/UnitPathfindingBudget.cs'
BUDGET_OUTPUT = 'SourcePathfindingBudgetObservation.cs'

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_bytes())
    for source, output in ((SOURCE, OUTPUT), (BUDGET_SOURCE, BUDGET_OUTPUT)):
        rows = [row for row in manifest['sources'] if row.get('path') == source]
        if len(rows) != 1 or rows[0].get('kind') != 'native_direct':
            raise ValueError('Exact original path owner and budget entries required')
        row = rows[0]
        row.update(kind='current_original', generator=GENERATOR, generator_inputs=[SOURCE, BUDGET_SOURCE],
                   generator_output=output, output=output,
                   scope='Experimental observation of original admission and budget; no readiness')
        row.pop('path')
    with args.output.open('x') as stream:
        json.dump(manifest, stream, indent=2)
        stream.write('\n')

if __name__ == '__main__':
    main()
