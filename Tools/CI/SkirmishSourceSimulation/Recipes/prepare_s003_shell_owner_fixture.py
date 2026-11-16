#!/usr/bin/env python3
"""Compile the original shell OnCreate against the S003 source-host adapter."""
import argparse
import hashlib
import importlib.util
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/s003-original-planner-probe-20261105-T2'
GENERATOR = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_state_owner/generate.py'
HOST = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/108-112_MetadataSystemTypes.cs'
OLD_BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
TEXT = ROOT / 'Logs/private-candidates/s049-physical-export-qualified-20261028-r1/project/Logs/SkirmishProduction/s049-command-text-r1/command-text.json'


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    out = parser.parse_args().output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must remain in the repository')
    out.mkdir(parents=True, exist_ok=False)
    receipt = json.loads((BASE / 'receipt.json').read_bytes())
    if receipt.get('status') != 'PassedSyntheticTupleControls':
        raise ValueError('Checked original S003 planner closure required')
    pins = dict(receipt['source_pins'])
    if sha(TEXT) != '70c9a983bc71cc86dc2f6112e5e092fc145f1bc831d4719e28ad4ea7747d755e':
        raise ValueError('Exact S003 source-host command text required')
    for path in (BASE / 'receipt.json', BASE / 'compile.rsp', GENERATOR, HOST, OLD_BUILD, TEXT, Path(__file__).resolve()):
        pins[str(path)] = sha(path)
    def verify():
        for name, expected in pins.items():
            if not Path(name).is_file() or sha(Path(name)) != expected:
                raise ValueError('Source input changed: ' + name)
    verify()
    spec = importlib.util.spec_from_file_location('original_shell_source_generator', GENERATOR)
    if spec is None or spec.loader is None:
        raise ValueError('Original shell source generator unavailable')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    generated = out / 'generated'
    proof = module.generate(ROOT, generated)
    for name, digest in proof['source_pins'].items():
        if name in pins and pins[name] != digest:
            raise ValueError('Conflicting shell owner source pin: ' + name)
        pins[name] = digest
    source = HOST.read_text()
    old = 'public struct FixedString4096Bytes{string value;'
    new = old + 'public FixedString4096Bytes(string initial){if(System.Text.Encoding.UTF8.GetByteCount(initial??"")>4093)throw new System.ArgumentException();value=initial;}'
    if source.count(old) != 1:
        raise ValueError('Exact fixed-string source-host seam required')
    host = out / 'S003ShellMetadataSystemTypes.cs'; host.write_text(source.replace(old, new))
    control = out / 'S003ShellOwnerFixture.cs'
    control.write_text('''using System;using System.IO;using Game.Components;using Game.UI.Contracts;using Game.UI.Shell.Contracts.Ecs;using Game.UI.Shell.Ecs;using Unity.Entities;
internal static class S003ShellOwnerFixture {static void Main(string[] args){Game.Configs.SourceEnglishCommandText.Bind(File.ReadAllBytes(args[0]));var em=new EntityManager{LogicalAllocatorAdmitted=true};int next=0;em.AllocateLogicalIdentity=()=>new Entity{Index=++next,Version=1};var state=new SystemState{EntityManager=em,Enabled=true};var owner=new UiShellStateSystem();state.Begin();try{owner.OnCreate(ref state);}finally{state.End();}using var roots=em.CreateEntityQuery(typeof(UiShellRootComponent));if(roots.CalculateEntityCount()!=1||state.Enabled)throw new Exception("original owner root");var root=roots.GetSingletonEntity();var shell=em.GetComponentData<UiShellStateComponent>(root);if(shell.ActiveRoute!=UIRoute.Splash||shell.CurrentMode!=UiShellMode.None||shell.TransitionSequenceId!=0)throw new Exception("original shell defaults");Environment.SetEnvironmentVariable("WARLINE_S003_ORIGINAL_ARIA_STEP_PROBE","1");var match=new SkirmishMatchState{ScenarioIndex=5,Phase=SkirmishPhase.Playing,Outcome=SkirmishOutcome.None};var result=SourceS003OriginalAriaStepDiagnostic.Capture(em,match,"WARLINE_S003_ORIGINAL_ARIA_STEP_PROBE");if((string)result["unavailable_reason"]!="UiRootNotOnMatchRoute"||(bool?)result["step_invoked"]!=false||(bool?)result["mission_ready"]!=false)throw new Exception("probe must respect actual shell route");Console.WriteLine("[S003ShellOwnerFixture] result=Passed originalOnCreate=1 route=Splash plannerInvoked=0 readiness=0 syntheticFixture=1");}}
''')
    args = [json.loads(s) for s in (BASE / 'compile.rsp').read_text().splitlines() if s.strip()]
    args = [x for x in args if not x.endswith('/S003OriginalAriaStepSourceHostControls.cs')]
    if args.count(str(HOST)) != 1:
        raise ValueError('Exact source-host fixed-string adapter required')
    args = [str(host) if x == str(HOST) else '-main:S003ShellOwnerFixture' if x.startswith('-main:') else '-out:' + str(out / 'control.exe') if x.startswith('-out:') else x for x in args]
    args.extend(str(generated / name) for name in ('UiShellStateSystem.source-host.cs', 'UiShellStartupDispositionState.source.cs', 'UiShellStateOwnerComponents.extracted.cs'))
    args.append(str(control))
    rsp = out / 'compile.rsp'; rsp.write_text(''.join(json.dumps(x) + '\n' for x in args))
    csc = next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compiled = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / 'compile.log').write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode:
        raise RuntimeError('Original shell source fixture failed to compile')
    runtime = json.loads(OLD_BUILD.read_bytes())['runtime_selection']
    env = os.environ.copy(); env['MONO_PATH'] = os.pathsep.join((str(OLD_BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(out)))
    run = subprocess.run([runtime['mono'], str(out / 'control.exe'), str(TEXT)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / 'run.log').write_text(run.stdout + run.stderr)
    marker = '[S003ShellOwnerFixture] result=Passed originalOnCreate=1 route=Splash plannerInvoked=0 readiness=0 syntheticFixture=1'
    if run.returncode or run.stdout.splitlines().count(marker) != 1:
        raise RuntimeError('Original shell source fixture failed')
    verify()
    result = {'status': 'PassedSyntheticOriginalShellOwnerFixture', 'source_pins': pins,
              'marker': marker, 'run_log_sha256': sha(out / 'run.log'),
              'gateway_registration_invoked': False, 'gameplay_run': False,
              'native_ui_or_player_readiness': False, 'mission_ready': False,
              'scope': 'Original shell OnCreate except gateway registration; S003 source-host shim, synthetic fixture only'}
    (out / 'receipt.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: v for k, v in result.items() if k != 'source_pins'}))


if __name__ == '__main__':
    main()
