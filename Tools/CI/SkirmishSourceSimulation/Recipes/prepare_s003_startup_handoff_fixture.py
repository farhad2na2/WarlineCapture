#!/usr/bin/env python3
"""Exercise original first-launch shell handoff in an isolated S003 ECS fixture."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/s003-shell-flow-fixture-20261105-T1'
OWNER = ROOT / 'Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeShellCompositionSystemHelper.cs'
ENUM = ROOT / 'Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeCompositionSystemHelper.cs'
BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
TEXT = ROOT / 'Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Logs/SkirmishProduction/s049-command-text-r1/command-text.json'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    out = parser.parse_args().output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must be inside repository')
    out.mkdir(parents=True, exist_ok=False)
    receipt = json.loads((BASE / 'receipt.json').read_bytes())
    if receipt['status'] != 'PassedSyntheticOriginalShellFlowFixture':
        raise ValueError('Passing shell flow fixture required')
    pins = dict(receipt['source_pins'])
    for path in (BASE / 'receipt.json', BASE / 'compile.rsp', BASE / 'S003ShellFlowFixture.cs', OWNER, ENUM, BUILD, TEXT, Path(__file__).resolve()):
        pins[str(path)] = sha(path)

    def verify():
        for name, digest in pins.items():
            if sha(Path(name)) != digest:
                raise ValueError('Source input changed: ' + name)

    verify()
    source = ENUM.read_text()
    start = source.index('    internal enum FirstLaunchNarrativeStartupDisposition')
    end = source.index('\n    internal sealed partial class', start)
    enum_file = out / 'OriginalDisposition.cs'
    enum_file.write_text('namespace Game.Composition\n{\n' + source[start:end] + '\n}\n')
    fixture = (BASE / 'S003ShellFlowFixture.cs').read_text()
    anchor = 'Environment.SetEnvironmentVariable("WARLINE_S003_ORIGINAL_ARIA_STEP_PROBE","1");'
    if fixture.count(anchor) != 1:
        raise ValueError('Fixture anchor changed')
    check = '''var handoff=new Game.Composition.FirstLaunchNarrativeShellCompositionSystemHelper();handoff.Apply(em,root);if(em.GetComponentData<UiShellStartupDispositionComponent>(root).Value!=UiShellStartupDisposition.Pending||em.GetBuffer<UiShellRouteRequestComponent>(root).Length!=0)throw new Exception("default pending emitted route");handoff.RequestHandoff();handoff.Apply(em,root);handoff.Apply(em,root);var pendingRoutes=em.GetBuffer<UiShellRouteRequestComponent>(root);if(em.GetComponentData<UiShellStartupDispositionComponent>(root).Value!=UiShellStartupDisposition.EnterMission||pendingRoutes.Length!=1||pendingRoutes[0].Intent!=UiShellRouteIntent.EnterMatch)throw new Exception("handoff route not exactly once");pendingRoutes.Clear();handoff.Reset();handoff.Apply(em,root);if(em.GetComponentData<UiShellStartupDispositionComponent>(root).Value!=UiShellStartupDisposition.Pending||pendingRoutes.Length!=0)throw new Exception("reset did not restore pending");'''
    flow_check = '''flowState.Begin();try{flowOwner.OnUpdate(ref flowState);}finally{flowState.End();}shell=em.GetComponentData<UiShellStateComponent>(root);var commands=em.GetBuffer<UiShellPresentationCommandComponent>(root);if(shell.ActiveRoute!=UIRoute.Match||shell.CurrentMode!=UiShellMode.Loading||shell.Phase!=UiShellTransitionPhase.ShowingLoading||pendingRoutes.Length!=0||commands.Length!=2||commands[0].Kind!=UiShellCommandKind.ShowLoading||commands[1].Kind!=UiShellCommandKind.ExitMenu)throw new Exception("original handoff flow did not remain in loading");'''
    check = check.replace('pendingRoutes.Clear();handoff.Reset();', flow_check + 'handoff.Reset();')
    marker = '[S003StartupHandoffFixture] result=Passed pending=1 handoffOnce=1 loading=1 reset=1 synthetic=1 nativeInput=0 readiness=0'
    fixture = fixture.replace(anchor, check + anchor).replace('class S003ShellFlowFixture', 'class S003StartupHandoffFixture')
    fixture = fixture.replace('"UiRootNotOnMatchRoute"', '"OriginalAriaObservationOwnerAbsent"')
    fixture = fixture.replace('[S003ShellFlowFixture] result=Passed originalOwner=1 originalFlow=1 route=Splash requests=0 audio=0 plannerInvoked=0 readiness=0 syntheticFixture=1', marker)
    control = out / 'S003StartupHandoffFixture.cs'
    control.write_text(fixture)
    args = [json.loads(line) for line in (BASE / 'compile.rsp').read_text().splitlines() if line.strip()]
    startup = ROOT / 'Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupFlow.cs'
    startup_source = startup.read_text()
    for original, replacement in (('EmitRouteMusic(state.World, request);', 'SourceS073UiShellFlowBoundary.MusicCalls++;'), ('EmitRouteAudio(state.World, request);', 'SourceS073UiShellFlowBoundary.AudioCalls++;')):
        if startup_source.count(original) != 1:
            raise ValueError('Exact startup presentation effect seam required')
        startup_source = startup_source.replace(original, replacement)
    startup_copy = out / 'UiShellStartupFlow.source-host.cs'
    startup_copy.write_text(startup_source)
    if args.count(str(startup)) != 1:
        raise ValueError('Original startup flow compile input changed')
    args = [str(startup_copy) if arg == str(startup) else arg for arg in args]
    old = str(BASE / 'S003ShellFlowFixture.cs')
    if args.count(old) != 1:
        raise ValueError('Fixture compile input changed')
    args = [str(control) if arg == old else '-main:S003StartupHandoffFixture' if arg.startswith('-main:') else '-out:' + str(out / 'control.exe') if arg.startswith('-out:') else arg for arg in args]
    args.extend((str(OWNER), str(enum_file)))
    rsp = out / 'compile.rsp'
    rsp.write_text(''.join(json.dumps(arg) + '\n' for arg in args))
    csc = next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compile_run = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / 'compile.log').write_text(compile_run.stdout + compile_run.stderr)
    if compile_run.returncode:
        raise RuntimeError('Fixture compilation failed')
    runtime = json.loads(BUILD.read_bytes())['runtime_selection']
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(out)))
    run = subprocess.run([runtime['mono'], str(out / 'control.exe'), str(TEXT)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / 'run.log').write_text(run.stdout + run.stderr)
    if run.returncode or run.stdout.splitlines().count(marker) != 1:
        raise RuntimeError('Fixture execution failed')
    verify()
    result = {'status': 'PassedSyntheticOriginalStartupHandoffFixture', 'marker': marker, 'source_pins': pins, 'run_log_sha256': sha(out / 'run.log'), 'scope': 'Original shell handoff and flow with explicitly invoked synthetic RequestHandoff; startup route audio/music calls captured as counters; Match route remains Loading with two presentation commands and no observation owner; no live view, native input, gameplay or readiness', 'mission_ready': False}
    (out / 'receipt.json').write_text(json.dumps(result, indent=2) + '\n')
    print(marker)


if __name__ == '__main__':
    main()
