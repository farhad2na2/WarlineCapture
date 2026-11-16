"""Generate a read-only S003 original-planner probe in the source-host program."""
import hashlib
import json
from pathlib import Path

OVERLAY = 'T Logs/SkirmishProduction/s004-original-planner-world-overlay-20261105-T2'
INSPECTOR = 'T Logs/SkirmishProduction/s003-original-planner-probe-20261105-T2/SourceS003OriginalAriaStepDiagnostic.cs'
INSPECTOR_RECEIPT = 'T Logs/SkirmishProduction/s003-original-planner-probe-20261105-T2/receipt.json'
PROGRAM = 'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceSimulationProgram.s003.cs'
COPIES = ['AriaSkirmishPlanSystem.DataOnly.extracted.cs', 'AriaSkirmishComponents.extern-alias.extracted.cs',
          'AriaS002NativeAdapter.extern-alias.extracted.cs', 'SourceS073OriginalAriaUiLifecycleTypes.cs']


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    receipt = json.loads((repo / OVERLAY / 'receipt.json').read_bytes())
    probe = json.loads((repo / INSPECTOR_RECEIPT).read_bytes())
    if receipt.get('status') != 'Prepared' or probe.get('status') != 'PassedSyntheticTupleControls' or probe.get('inspector_sha256') != digest(repo / INSPECTOR):
        raise ValueError('Checked original planner overlay and S003 tuple probe required')
    pins = {}
    for group in ('source_sha256', 'original_planner_input_sha256', 'ui_lifecycle_input_sha256'):
        for raw, expected in receipt[group].items():
            path = Path(raw).resolve(strict=True)
            if not path.is_relative_to(repo) or digest(path) != expected:
                raise ValueError('Original planner source changed: ' + raw)
            pins[str(path)] = expected
    program_path = repo / PROGRAM
    program = program_path.read_text()
    anchor = 'var row=world.StepSourceS002Aria();'
    if program.count(anchor) != 1:
        raise ValueError('Exact source scheduler probe seam required')
    code = '''if(i==0){var originalProbe=SourceS003OriginalAriaStepDiagnostic.Capture(world.Entities,world.Entities.GetComponentData<SkirmishMatchState>(world.Session),"WARLINE_S003_ORIGINAL_ARIA_STEP_PROBE");if(originalProbe!=null)File.WriteAllText(Path.Combine(dir,"s003-original-aria-same-world-probe.json"),originalProbe.ToString(Formatting.Indented));}'''
    program = program.replace(anchor, code + anchor)
    out = Path(output)
    out.mkdir(parents=True, exist_ok=False)
    files = {'S003OriginalPlannerProbeProgram.cs': program.encode(),
             'SourceS003OriginalAriaStepDiagnostic.cs': (repo / INSPECTOR).read_bytes()}
    for name in COPIES:
        files[name] = (repo / OVERLAY / name).read_bytes()
    for name, raw in files.items():
        (out / name).write_bytes(raw)
    return {'source_pins': pins, 'outputs': {name: hashlib.sha256(raw).hexdigest() for name, raw in files.items()},
            'experimental_only': True, 'scope': 'Opt-in read-only original UI tuple probe; no input or gameplay mutation'}
