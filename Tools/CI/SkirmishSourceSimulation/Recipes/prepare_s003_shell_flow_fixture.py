#!/usr/bin/env python3
"""Compile original no-input shell flow against the S003 source-host fixture."""
import argparse
import hashlib
import importlib.util
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/s003-shell-owner-fixture-20261105-T2'
RECIPE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_flow_owner'
ADAPTER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs'
FLOW = ROOT / 'Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs'
TEXT = ROOT / 'Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Logs/SkirmishProduction/s049-command-text-r1/command-text.json'
OLD_BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for data in iter(lambda: f.read(1024 * 1024), b''):
            h.update(data)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('--output', type=Path, required=True); args = parser.parse_args()
    out = args.output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must be inside repository')
    out.mkdir(parents=True, exist_ok=False)
    receipt = json.loads((BASE / 'receipt.json').read_bytes())
    if receipt.get('status') != 'PassedSyntheticOriginalShellOwnerFixture':
        raise ValueError('Passing original shell owner fixture required')
    pins = dict(receipt['source_pins'])
    additions = [ROOT / 'Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.Requests.cs',
                 ROOT / 'Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupFlow.cs',
                 ROOT / 'Assets/Game/Scripts/UI/Components/UIAudioEventGateway.cs',
                 RECIPE / 'SourceS073UiShellFlowBoundary.cs', RECIPE / 'SourceS073FlowSystemStateHost.cs']
    files = [BASE / 'receipt.json', BASE / 'compile.rsp', BASE / 'S003ShellOwnerFixture.cs',
             RECIPE / 'run_integration_fixture.py', ADAPTER, FLOW, TEXT, OLD_BUILD, Path(__file__).resolve(), *additions]
    for path in files:
        pins[str(path)] = sha(path)
    def verify():
        for path, digest in pins.items():
            if not Path(path).is_file() or sha(Path(path)) != digest:
                raise ValueError('Source input changed: ' + path)
    verify()
    spec = importlib.util.spec_from_file_location('original_flow_effect_adapter', RECIPE / 'run_integration_fixture.py')
    if spec is None or spec.loader is None:
        raise ValueError('Original flow adapter unavailable')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    flow = out / 'UiShellFlowSystem.source-host.cs'; flow.write_text(module.adapt_flow_source(FLOW.read_text()))
    old = 'public sealed class World{public bool IsCreated{get;internal set;}=true;}'
    new = 'public sealed class World{public bool IsCreated{get;internal set;}=true;public EntityManager EntityManager{get;set;}}'
    source = ADAPTER.read_text()
    if source.count(old) != 1:
        raise ValueError('Exact host World adapter seam required')
    ecs = out / 'S003EcsFlowWorld.cs'; ecs.write_text(source.replace(old, new))
    fixture = (BASE / 'S003ShellOwnerFixture.cs').read_text()
    anchor = 'Environment.SetEnvironmentVariable("WARLINE_S003_ORIGINAL_ARIA_STEP_PROBE","1");'
    if fixture.count(anchor) != 1:
        raise ValueError('Exact shell fixture anchor required')
    flow_check = '''var flowState=new SystemState{EntityManager=em,Enabled=true,World=new World{EntityManager=em}};var flowOwner=new UiShellFlowSystem();flowState.Begin();try{flowOwner.OnCreate(ref flowState);flowOwner.OnUpdate(ref flowState);}finally{flowState.End();}shell=em.GetComponentData<UiShellStateComponent>(root);if(shell.ActiveRoute!=UIRoute.Splash||shell.CurrentMode!=UiShellMode.None||shell.TransitionSequenceId!=0||em.GetBuffer<UiShellRouteRequestComponent>(root).Length!=0||em.GetBuffer<UiShellPresentationCommandComponent>(root).Length!=0||em.GetBuffer<UiShellTransitionCompleteComponent>(root).Length!=0||SourceS073UiShellFlowBoundary.AudioCalls!=0||SourceS073UiShellFlowBoundary.MusicCalls!=0||SourceS073UiShellFlowBoundary.EnsureAudioCalls!=0||SourceS073UiShellFlowBoundary.GatewayCalls!=1)throw new Exception("original no-input flow changed shell or emitted effects");'''
    fixture = fixture.replace(anchor, flow_check + anchor).replace('[S003ShellOwnerFixture] result=Passed originalOnCreate=1 route=Splash plannerInvoked=0 readiness=0 syntheticFixture=1', '[S003ShellFlowFixture] result=Passed originalOwner=1 originalFlow=1 route=Splash requests=0 audio=0 plannerInvoked=0 readiness=0 syntheticFixture=1')
    control = out / 'S003ShellFlowFixture.cs'; control.write_text(fixture.replace('class S003ShellOwnerFixture', 'class S003ShellFlowFixture'))
    args = [json.loads(s) for s in (BASE / 'compile.rsp').read_text().splitlines() if s.strip()]
    if args.count(str(ADAPTER)) != 1 or args.count(str(BASE / 'S003ShellOwnerFixture.cs')) != 1:
        raise ValueError('Exact S003 shell fixture compile inputs required')
    args = [str(ecs) if x == str(ADAPTER) else str(control) if x == str(BASE / 'S003ShellOwnerFixture.cs') else '-main:S003ShellFlowFixture' if x.startswith('-main:') else '-out:' + str(out / 'control.exe') if x.startswith('-out:') else x for x in args]
    args.extend([str(flow), *(str(path) for path in additions)])
    rsp = out / 'compile.rsp'; rsp.write_text(''.join(json.dumps(v) + '\n' for v in args))
    csc = next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compile_run = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / 'compile.log').write_text(compile_run.stdout + compile_run.stderr)
    if compile_run.returncode:
        raise RuntimeError('Original S003 shell flow fixture failed to compile')
    runtime = json.loads(OLD_BUILD.read_bytes())['runtime_selection']; env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((str(OLD_BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(out)))
    run = subprocess.run([runtime['mono'], str(out / 'control.exe'), str(TEXT)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / 'run.log').write_text(run.stdout + run.stderr)
    marker = '[S003ShellFlowFixture] result=Passed originalOwner=1 originalFlow=1 route=Splash requests=0 audio=0 plannerInvoked=0 readiness=0 syntheticFixture=1'
    if run.returncode or run.stdout.splitlines().count(marker) != 1:
        raise RuntimeError('Original S003 shell flow fixture failed')
    verify()
    result = {'status': 'PassedSyntheticOriginalShellFlowFixture', 'source_pins': pins, 'marker': marker,
              'run_log_sha256': sha(out / 'run.log'), 'scope': 'Original OnCreate and one no-input OnUpdate with captured gateway/audio boundaries; synthetic source-host fixture only',
              'gateway_registered': False, 'input_injected': False, 'gameplay_run': False, 'mission_ready': False}
    (out / 'receipt.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: v for k, v in result.items() if k != 'source_pins'}))


if __name__ == '__main__':
    main()
