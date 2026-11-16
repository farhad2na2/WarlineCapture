#!/usr/bin/env python3
"""Extend the S003 planner probe with the original source shell OnCreate."""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_shell_owner_probe/generate.py'
PLANNER = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_original_planner_probe/generate.py'
SHELL = 'Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_state_owner/generate.py'
HOST = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/108-112_MetadataSystemTypes.cs'
SHELL_INPUTS = ['Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs',
                'Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs',
                'Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupDispositionState.cs',
                'Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs',
                'Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs',
                'Assets/Game/Scripts/UI/Contracts/UiShellPopupContracts.cs',
                'Tools/CI/SkirmishSourceSimulation/Adapters/112_MetadataSystemTypes.cs', HOST]
OUTPUTS = ['S003OriginalPlannerProbeProgram.cs', 'S003ShellOwnerMetadataTypes.cs', 'S003ShellOwnerBootstrap.cs',
           'UiShellStateSystem.source-host.cs', 'UiShellStartupDispositionState.source.cs', 'UiShellStateOwnerComponents.extracted.cs']


def main():
    p = argparse.ArgumentParser(description=__doc__); p.add_argument('--manifest', type=Path, required=True); p.add_argument('--output', type=Path, required=True); args = p.parse_args()
    manifest = json.loads(args.manifest.read_bytes())
    programs = [r for r in manifest['sources'] if r.get('output') == 'S003OriginalPlannerProbeProgram.cs']
    hosts = [r for r in manifest['sources'] if r.get('path') == HOST]
    if len(programs) != 1 or len(hosts) != 1:
        raise ValueError('Exact planner and source-host adapter rows required')
    inputs = list(dict.fromkeys(programs[0]['generator_inputs'] + SHELL_INPUTS))
    manifest['sources'].remove(programs[0]); manifest['sources'].remove(hosts[0])
    for name in OUTPUTS:
        manifest['sources'].append({'id': 'S003ShellOwnerProbe_' + name, 'kind': 'current_original',
                                    'generator': GENERATOR, 'generator_inputs': inputs,
                                    'generator_helpers': [PLANNER, SHELL], 'generator_output': name, 'output': name})
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open('x') as f:
        json.dump(manifest, f, indent=2); f.write('\n')
    print('[S003ShellOwnerProbeManifest] result=Prepared experimental=1')


if __name__ == '__main__':
    main()
