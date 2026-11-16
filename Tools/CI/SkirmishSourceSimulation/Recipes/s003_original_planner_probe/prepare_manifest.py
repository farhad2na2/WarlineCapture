#!/usr/bin/env python3
"""Prepare the noncanonical S003 original-planner same-world probe build."""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_original_planner_probe/generate.py'
OVERLAY = ROOT / 'T Logs/SkirmishProduction/s004-original-planner-world-overlay-20261105-T2'
INSPECTOR = ROOT / 'T Logs/SkirmishProduction/s003-original-planner-probe-20261105-T2'
PROGRAM = 'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceSimulationProgram.s003.cs'
OUTPUTS = ['S003OriginalPlannerProbeProgram.cs', 'SourceS003OriginalAriaStepDiagnostic.cs',
           'AriaSkirmishPlanSystem.DataOnly.extracted.cs', 'AriaSkirmishComponents.extern-alias.extracted.cs',
           'AriaS002NativeAdapter.extern-alias.extracted.cs', 'SourceS073OriginalAriaUiLifecycleTypes.cs']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_bytes())
    overlay = json.loads((OVERLAY / 'receipt.json').read_bytes())
    rows = [r for r in manifest['sources'] if r.get('path') == PROGRAM]
    if len(rows) != 1:
        raise ValueError('Exact S003 source program entry required')
    manifest['sources'].remove(rows[0])
    inputs = [PROGRAM, str((OVERLAY / 'receipt.json').relative_to(ROOT)),
              str((INSPECTOR / 'receipt.json').relative_to(ROOT)),
              str((INSPECTOR / 'SourceS003OriginalAriaStepDiagnostic.cs').relative_to(ROOT))]
    for group in ('source_sha256', 'original_planner_input_sha256', 'ui_lifecycle_input_sha256'):
        for name in overlay[group]:
            path = Path(name).resolve(strict=True)
            if not path.is_relative_to(ROOT):
                raise ValueError('Planner source outside repository')
            inputs.append(str(path.relative_to(ROOT)))
    inputs = list(dict.fromkeys(inputs))
    for name in OUTPUTS:
        manifest['sources'].append({'id': 'S003OriginalPlannerProbe_' + name, 'kind': 'current_original',
                                    'generator': GENERATOR, 'generator_inputs': inputs, 'generator_helpers': [],
                                    'generator_output': name, 'output': name})
    existing = {r.get('path') for r in manifest['sources']}
    for name in overlay['source_files']:
        path = Path(name).resolve(strict=True)
        rel = str(path.relative_to(ROOT))
        if path.parent == OVERLAY or rel in existing:
            continue
        manifest['sources'].append({'id': 'S003OriginalPlannerContract_' + path.name,
                                    'kind': 'native_direct', 'path': rel})
        existing.add(rel)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('x') as f:
        json.dump(manifest, f, indent=2); f.write('\n')
    print('[S003OriginalPlannerProbeManifest] result=Prepared experimental=1')


if __name__ == '__main__':
    main()
