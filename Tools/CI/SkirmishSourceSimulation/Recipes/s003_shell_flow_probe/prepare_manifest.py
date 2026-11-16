#!/usr/bin/env python3
"""Add an opt-in original shell-flow update to the S003 shell-owner probe."""
import argparse
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_shell_flow_probe/generate.py'
BASE = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_shell_owner_probe/generate.py'
PLANNER = 'Tools/CI/SkirmishSourceSimulation/Recipes/s003_original_planner_probe/generate.py'
SHELL = 'Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_state_owner/generate.py'
ECS = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs'
FLOW = 'Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs'
ADDITIONS = ['Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.Requests.cs',
             'Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupFlow.cs',
             'Assets/Game/Scripts/UI/Components/UIAudioEventGateway.cs',
             'Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_flow_owner/SourceS073UiShellFlowBoundary.cs',
             'Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_flow_owner/SourceS073FlowSystemStateHost.cs']
OUTPUTS = ['S003OriginalPlannerProbeProgram.cs', 'S003ShellFlowEcs.cs', 'S003ShellFlowBootstrap.cs', 'S003OriginalShellFlow.source.cs']


def main():
    p = argparse.ArgumentParser(description=__doc__); p.add_argument('--manifest', type=Path, required=True); p.add_argument('--output', type=Path, required=True); a = p.parse_args()
    manifest = json.loads(a.manifest.read_bytes())
    programs = [r for r in manifest['sources'] if r.get('output') == 'S003OriginalPlannerProbeProgram.cs']
    ecs = [r for r in manifest['sources'] if r.get('path') == ECS]
    if len(programs) != 1 or len(ecs) != 1:
        raise ValueError('Exact shell owner program and ECS adapter required')
    inputs = list(dict.fromkeys(programs[0]['generator_inputs'] + [ECS, FLOW, *ADDITIONS]))
    manifest['sources'].remove(programs[0]); manifest['sources'].remove(ecs[0])
    for name in OUTPUTS:
        manifest['sources'].append({'id': 'S003ShellFlowProbe_' + name, 'kind': 'current_original',
                                    'generator': GENERATOR, 'generator_inputs': inputs,
                                    'generator_helpers': [BASE, PLANNER, SHELL], 'generator_output': name, 'output': name})
    for name in ADDITIONS:
        if any(r.get('path') == name for r in manifest['sources']):
            raise ValueError('Duplicate original flow dependency: ' + name)
        manifest['sources'].append({'id': 'S003ShellFlowDependency_' + Path(name).name, 'kind': 'native_direct', 'path': name})
    a.output.parent.mkdir(parents=True, exist_ok=True)
    with a.output.open('x') as f:
        json.dump(manifest, f, indent=2); f.write('\n')
    print('[S003ShellFlowProbeManifest] result=Prepared experimental=1')


if __name__ == '__main__':
    main()
