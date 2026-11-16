#!/usr/bin/env python3
"""Compile the original generic ARIA planner and a read-only S003 tuple probe."""
import argparse
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
BASE = ROOT / 'T Logs/SkirmishProduction/s003-actual-importer-current-fixture-20261105-T7'
OVERLAY = ROOT / 'T Logs/SkirmishProduction/s004-original-planner-world-overlay-20261105-T2'
OLD_BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def replace_once(text, old, new):
    if text.count(old) != 1:
        raise ValueError('Original probe seam changed: ' + old)
    return text.replace(old, new)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output', type=Path, required=True)
    out = p.parse_args().output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must remain in the repository')
    out.mkdir(parents=True, exist_ok=False)
    base = json.loads((BASE / 'receipt.json').read_bytes())
    overlay = json.loads((OVERLAY / 'receipt.json').read_bytes())
    if base.get('status') != 'PassedDisposableS003ImporterFixture' or overlay.get('status') != 'Prepared':
        raise ValueError('Current S003 fixture and original planner overlay required')
    pins = dict(base['source_pins'])
    for group in ('source_sha256', 'original_planner_input_sha256', 'ui_lifecycle_input_sha256'):
        for name, digest in overlay[group].items():
            if name in pins and pins[name] != digest:
                raise ValueError('Conflicting source pin: ' + name)
            pins[name] = digest
    for path in (BASE / 'receipt.json', BASE / 'compile.rsp', OVERLAY / 'receipt.json', OLD_BUILD, Path(__file__).resolve()):
        pins[str(path)] = sha(path)
    def verify():
        for name, digest in pins.items():
            if not Path(name).is_file() or sha(Path(name)) != digest:
                raise ValueError('Source input changed: ' + name)
    verify()
    inspector = (OVERLAY / 'SourceS004OriginalAriaStepDiagnostic.cs').read_text().replace('SourceS004OriginalAriaStepDiagnostic', 'SourceS003OriginalAriaStepDiagnostic').replace('S004OriginalAriaStepSameWorldDiagnostic', 'S003OriginalAriaStepSameWorldDiagnostic').replace('OriginalS004ExpandedAirStepPreconditionsNotMet', 'OriginalS003ExpandedAirStepPreconditionsNotMet')
    inspector = replace_once(inspector, 'view.SourceScenarioIndex != 6 || match.ScenarioIndex != 6', 'view.SourceScenarioIndex != 5 || match.ScenarioIndex != 5')
    inspector_path = out / 'SourceS003OriginalAriaStepDiagnostic.cs'
    inspector_path.write_text(inspector)
    control = (OVERLAY / 'S004OriginalAriaStepSourceHostControls.cs').read_text().replace('S004OriginalAriaStepSourceHostControls', 'S003OriginalAriaStepSourceHostControls').replace('SourceS004OriginalAriaStepDiagnostic', 'SourceS003OriginalAriaStepDiagnostic').replace('WARLINE_S004_ORIGINAL_ARIA_STEP_PROBE', 'WARLINE_S003_ORIGINAL_ARIA_STEP_PROBE')
    control = replace_once(control, 'ScenarioIndex = 6,', 'ScenarioIndex = 5,')
    positive = '''        // Synthetic tuple only: exercise scenario-5 original Step on copies.
        var complete = NewManager();
        Entity completeBoundary = CreateBoundary(complete, 11);
        Entity completeStorage = CreateOwner(complete, completeBoundary, 11);
        complete.AddComponentData(completeStorage, new AriaSkirmishObservationComponent { Value = new AriaSkirmishObservation {
            Active = true, SourceScenarioIndex = 5, ExpandedSession = true, ExpandedBaseAssault = true, AirProfile = true,
            Frame = 20, Time = 1f } });
        complete.AddComponentData(completeStorage, new AriaSkirmishPlanComponent());
        string completeBefore = Snapshot(complete);
        JObject projected = Capture(complete);
        Need((string)projected["status"] == "OriginalStepEvaluatedFromCopiedSourceState" &&
             (bool?)projected["step_invoked"] == true && (bool?)projected["planner_suggestion_accepted"] == false &&
             (bool?)projected["actions_synthesized"] == false && (bool?)projected["mission_ready"] == false,
             "scenario-5 copied original planner evaluation");
        Need(Snapshot(complete) == completeBefore, "scenario-5 copied evaluation mutated source tuple");

'''
    control = replace_once(control, '        Environment.SetEnvironmentVariable(OptIn, null);', positive + '        Environment.SetEnvironmentVariable(OptIn, null);')
    control = replace_once(control, 'cases=4 stepInvoked=0', 'cases=5 stepInvoked=1')
    control_path = out / 'S003OriginalAriaStepSourceHostControls.cs'
    control_path.write_text(control)
    args = [json.loads(s) for s in (BASE / 'compile.rsp').read_text().splitlines() if s.strip()]
    args = [x for x in args if not x.endswith('/Tests/S003RefreshedPrefabImportControl.cs')]
    args = [('-main:S003OriginalAriaStepSourceHostControls' if x.startswith('-main:') else '-out:' + str(out / 'control.exe') if x.startswith('-out:') else x) for x in args]
    args.extend(name for name in overlay['source_files'] if not name.endswith('/SourceS004OriginalAriaStepDiagnostic.cs') and name not in args)
    args.extend((str(inspector_path), str(control_path)))
    rsp = out / 'compile.rsp'
    rsp.write_text(''.join(json.dumps(v) + '\n' for v in args))
    csc = next(Path(name) for name in pins if name.endswith('/Roslyn/bincore/csc.dll'))
    compile_run = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / 'compile.log').write_text(compile_run.stdout + compile_run.stderr)
    if compile_run.returncode:
        raise RuntimeError('S003 original planner closure failed to compile')
    runtime = json.loads(OLD_BUILD.read_bytes())['runtime_selection']
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((str(OLD_BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(out)))
    run = subprocess.run([runtime['mono'], str(out / 'control.exe')], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / 'run.log').write_text(run.stdout + run.stderr)
    marker = '[S003OriginalAriaStepSourceHostControls] result=Passed cases=5 stepInvoked=1 mutation=0 readiness=0 gameplay=0 unity=0 syntheticFixtures=1'
    if run.returncode or run.stdout.splitlines().count(marker) != 1:
        raise RuntimeError('S003 planner tuple guard controls failed')
    verify()
    result = {'kind': 'S003OriginalPlannerSourceHostProbe', 'status': 'PassedSyntheticTupleControls',
              'inspector_path': str(inspector_path), 'inspector_sha256': sha(inspector_path),
              'compile_response_sha256': sha(rsp), 'run_log_sha256': sha(out / 'run.log'),
              'source_pins': pins, 'scope': 'Exact original planner source; synthetic tuple guards and copied scenario-5 Step only',
              'gameplay_run': False, 'planner_step_invoked_in_synthetic_fixture': True, 'native_readiness': False, 'mission_ready': False}
    (out / 'receipt.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: v for k, v in result.items() if k != 'source_pins'}))


if __name__ == '__main__':
    main()
