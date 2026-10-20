"""Checked Windows no-quit two-canary launcher. Defaults to plan; never qualifies queue jobs."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import time

from skirmish_production_queue import fingerprint, load, write_new
from s005_windows_source_snapshot import snapshot, build_snapshot, candidate_hash
from skirmish_production_matrix_authority import SamplingAuthority


METHOD = 'Game.Editor.SkirmishS002AriaRunHarness.RunProductionBatchCanariesFromEnvironment'
MARKER = '[SkirmishProductionBatch] result=Completed jobs=2 queueSuccessEnabled=0'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def observe_endpoint(project, descriptor, output, phase):
    began = time.perf_counter()
    store = Path(descriptor['candidate_manifest_path'])
    expected = load(store)
    if sha(store) != descriptor['candidate_manifest_sha256']:
        raise ValueError('Shared immutable manifest changed')
    rows = snapshot(project)
    builds, build = build_snapshot(project)
    actual = dict(source_manifest=rows, candidate_sha256=candidate_hash(rows), build_manifest=builds, build_sha256=build)
    matches = actual == expected and actual['candidate_sha256'] == descriptor['candidate_sha256'] and build == descriptor['build_sha256']
    receipt = dict(schema_version=1, phase=phase, observed_candidate_sha256=actual['candidate_sha256'],
                   observed_build_sha256=build, observed_source_files=len(rows), observed_dlls=len(builds),
                   manifest_path=str(store), manifest_sha256=sha(store), complete_manifest_equality=matches,
                   elapsed_seconds=time.perf_counter()-began, native_qualification='Pending two native canaries')
    write_new(output / (phase + '-source-receipt.json'), receipt)
    if not matches:
        write_new(output / (phase + '-changed-complete-manifest.json'), actual)
        raise ValueError('Complete source/DLL census changed; full changed rows preserved')
    return receipt


def verify_mac_wrapper(descriptor, receipt, project):
    """Read-only receipt consistency, never independent process/mission qualification."""
    if project is None or not isinstance(receipt, dict):
        raise ValueError('Actual Mac project and original-child receipt required')
    root = str(Path(project).resolve())
    expected = dict(schema_version=1, platform='macos', execute_method=METHOD,
                    checked_wrapper='Tools/CI/invoke_unity_macos.sh',
                    descriptor_fingerprint=fingerprint(descriptor), project=root,
                    checked_wrapper_exit=0, wrapper_reaped=True,
                    parent_environment_unchanged=True, source_settings_unchanged=True,
                    observed_target_editor=True, queue_success_enabled=False)
    if not all(type(receipt.get(k)) is type(v) and receipt[k] == v for k, v in expected.items()):
        raise ValueError('Mac original-child wrapper receipt differs')
    birth = receipt.get('wrapper_birth')
    if not (type(receipt.get('wrapper_pid')) is int and receipt['wrapper_pid'] > 0
            and isinstance(birth, list) and len(birth) == 3
            and all(type(v) is int and v >= 0 for v in birth)
            and birth[0] == receipt['wrapper_pid'] and birth[1] > 0):
        raise ValueError('Mac wrapper kernel birth missing')
    shutdown = receipt.get('owned_shutdown')
    if not (isinstance(shutdown, dict) and shutdown.get('closed') is True
            and type(shutdown.get('observed_absence_samples')) is int
            and shutdown['observed_absence_samples'] >= 2 and shutdown.get('survivors') == []):
        raise ValueError('Mac full owned shutdown missing')
    if receipt.get('lifecycle_errors') or receipt.get('controller_error'):
        raise ValueError('Mac lifecycle failure remains latched')
    if not re.fullmatch('[a-f0-9]{64}', receipt.get('grant_sha256', '')):
        raise ValueError('Mac reviewed grant binding missing')
    command = receipt.get('argv')
    timeout = receipt.get('timeout_seconds')
    if type(timeout) is not int or not 1 <= timeout <= 28800:
        raise ValueError('Mac bounded timeout missing')
    expected_command = ['rtk', 'proxy', str(Path(root)/'Tools/CI/invoke_unity_macos.sh'),
                        '--project', root, '--timeout', str(timeout), '--log',
                        str(Path(descriptor['artifact_directory'])/'unity.log'),
                        '--', '-buildTarget', 'Android', '-executeMethod', METHOD]
    if command != expected_command:
        raise ValueError('Mac checked GUI/no-quit command differs')


def verify_native_events(descriptor, events, log, project=None, *, mac_wrapper_receipt=None):
    """Consistency of actual wrapper/native rows only, never gameplay certification."""
    if descriptor.get('schema_version')==2:
        if project is None: raise ValueError('Actual project required to revalidate matrix authority')
        SamplingAuthority(project,descriptor['sampling_plan_path'],descriptor['sampling_plan_sha256']).validate_batch([load(Path(row['job_path'])) for row in descriptor['jobs']])
    count=len(descriptor['jobs'])
    runtime_marker='[SkirmishProductionRuntimeGate] result=Passed errors=0 jobs='+str(count)+' actualWrapperExitStillRequired=1'
    runtime=[row['facts'] for row in events if row['kind']=='RuntimeQuittingGate']
    if log.count(runtime_marker)!=1 or '[SkirmishProductionRuntimeGate] result=Failed' in log or len(runtime)!=1 or runtime[0].get('passed') is not True or type(runtime[0].get('errors')) is not int or runtime[0]['errors']!=0 or runtime[0].get('completed_jobs')!=count:
        raise ValueError('Whole-run runtime Error/Exception/Assert gate missing or failed')
    marker='[SkirmishProductionBatch] result=Completed jobs='+str(count)+' queueSuccessEnabled=0'
    if mac_wrapper_receipt is not None:
        verify_mac_wrapper(descriptor, mac_wrapper_receipt, project)
    if marker not in log or (mac_wrapper_receipt is None and re.search(r'\[UnityInvoke\] ExitCode: 0\s*$', log, re.M) is None):
        raise ValueError('Actual checked wrapper exit0 or two-job marker absent')
    if 'process exit code was unavailable' in log.lower():
        raise ValueError('Actual process exit was unavailable; log fallback cannot qualify this benchmark')
    if any(row['kind'] == 'BatchFailed' for row in events):
        raise ValueError('Native batch failure retained')
    completed = [row['facts'] for row in events if row['kind'] == 'BatchCompleted']
    if len(completed) != 1 or completed[0]['completed_jobs'] != count or completed[0]['queue_success_enabled'] is not False:
        raise ValueError('One exact native batch completion required')
    expected_jobs = [fingerprint(load(Path(row['job_path']))) for row in descriptor['jobs']]
    dispatch = [row['facts']['job_sha256'] for row in events if row['kind'] == 'Dispatch']
    if dispatch != expected_jobs or len(set(dispatch)) != count:
        raise ValueError('Two exact ordered native job dispatches required')
    for row in events:
        if row['kind'] in ('Dispatch', 'NativeFingerprint') and (
                row['facts']['manifest_path'] != descriptor['candidate_manifest_path'] or
                row['facts']['manifest_sha256'] != descriptor['candidate_manifest_sha256']):
            raise ValueError('Per-job immutable candidate-store reference differs')
    fingerprints = [row['facts'] for row in events if row['kind'] == 'NativeFingerprint']
    if [(row['job_sha256'], row['endpoint']) for row in fingerprints] != [(job, endpoint) for job in expected_jobs for endpoint in ('Begin', 'End')]:
        raise ValueError('Native Begin/End fingerprint ownership/order incomplete')
    for row in fingerprints:
        if row['candidate_sha256'] != descriptor['candidate_sha256'] or row['build_sha256'] != descriptor['build_sha256']:
            raise ValueError('Actual native candidate/DLL mismatch')
        if not isinstance(row['source_hash_milliseconds'], (int, float)) or isinstance(row['source_hash_milliseconds'], bool) or row['source_hash_milliseconds'] < 0:
            raise ValueError('Actual native hash duration missing')
    raw = re.findall(r'\[SkirmishCandidateFingerprint\] phase=Completed owner=(LaunchCatalog|Finish)[^\r\n]*sha256=([a-f0-9]{64})', log)
    if raw != [(owner, descriptor['candidate_sha256']) for owner in ('LaunchCatalog', 'Finish') * count]:
        raise ValueError('Raw native LaunchCatalog/Finish SHA joins incomplete')
    cleanup = [row['facts'] for row in events if row['kind'] == 'CleanupOwnershipCensus']
    if [row['job_sha256'] for row in cleanup] != expected_jobs:
        raise ValueError('Per-job actual cleanup census absent')
    for row in cleanup:
        census = row['census']
        if census['preview_census_complete'] is not True or census['measured_preview_count'] != census['actual_preview_count']:
            raise ValueError('Unknown preview ownership after native return')
        for world in census['worlds']:
            if any(world[name] != 0 for name in ('native_sessions', 'owned_actors', 'setup_records', 'launch_definition_records', 'logical_provider_records')):
                raise ValueError('Mission ownership survived teardown')
    exits = [row['facts'] for row in events if row['kind'] == 'NativeExitInvocation']
    if len(exits) != 1 or exits[0]['invocations'] != 1 or exits[0]['requested_code'] != 0:
        raise ValueError('Single successful native exit invocation required')
    return dict(native_jobs=count, native_canaries=(count if descriptor.get("schema_version",1)==1 else 0), native_source_hash_seconds=sum(row['source_hash_milliseconds'] for row in fingerprints)/1000,
                native_dll_hash_seconds=sum(row['dll_hash_milliseconds'] for row in fingerprints)/1000,
                source_hash_endpoint_seconds=[row['source_hash_milliseconds']/1000 for row in fingerprints],
                queue_success_enabled=False, mission_acceptance='Pending independent normal-input evidence review')


def quoted(value):
    return "'" + str(value).replace("'", "''") + "'"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--descriptor', type=Path, required=True)
    parser.add_argument('--timeout', type=int, default=7200)
    parser.add_argument('--run', action='store_true')
    args = parser.parse_args()
    project = args.project.resolve(); descriptor_path = args.descriptor.resolve(); descriptor = load(descriptor_path)
    matrix=descriptor.get('schema_version')==2
    authority=SamplingAuthority(project,descriptor['sampling_plan_path'],descriptor['sampling_plan_sha256']) if matrix else None
    if not 1<=len(descriptor['jobs'])<=(1200 if matrix else 2) or (not matrix and len(descriptor['jobs'])!=2) or not 1 <= args.timeout <= 28800:
        raise ValueError('Exactly two development canaries and bounded timeout required')
    if authority is not None:
        authority.validate_batch([load(Path(row['job_path'])) for row in descriptor['jobs']])
    output = Path(descriptor['artifact_directory']).resolve()
    allowed = (project / 'Logs/SkirmishProduction/Batches').resolve()
    if not output.is_relative_to(allowed) or output.name != descriptor['batch_id'] or descriptor_path != output / 'batch.json':
        raise ValueError('Use canonical prepared durable batch descriptor')
    plan = dict(result='PlanOnly', execute_method=METHOD, checked_wrapper='Tools/CI/InvokeUnity.ps1',
                gui_licensing=True, quit_argument=False, timeout_seconds=args.timeout,
                descriptor_sha256=sha(descriptor_path), queue_success_enabled=False)
    if not args.run:
        print(json.dumps(plan, indent=2)); return 0
    if os.name != 'nt':
        raise ValueError('This launcher is the checked Windows route only')
    write_new(output / 'outer-started.json', plan)
    env = {key: value for key, value in os.environ.items() if not key.upper().startswith('WARLINE_')}
    env['WARLINE_SKIRMISH_PRODUCTION_BATCH'] = str(descriptor_path)
    # All shell values are literal single-quoted strings. No user text is executable.
    check = output / 'checked-project-lane.ps1'
    check.write_text("$ErrorActionPreference='Stop'\nif(-not(Get-Process -Name 'Unity Hub' -ErrorAction SilentlyContinue)){throw 'Keep Unity Hub open and signed in'}\n" +
        "$taskProject=" + quoted(str(project).replace('/', '\\')) + "\nforeach($taskEditor in @(Get-CimInstance Win32_Process -Filter \"Name = 'Unity.exe'\")){\n" +
        "if(-not $taskEditor.CommandLine){throw 'Cannot inspect existing Editor ownership'}\n" +
        "if($taskEditor.CommandLine.Replace('/','\\').IndexOf($taskProject,[StringComparison]::OrdinalIgnoreCase)-ge 0){throw 'Existing Editor owns this project; preserve it'}\n}\n", encoding='utf-8-sig')
    shell = ['rtk', 'proxy', 'powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File']
    subprocess.run(shell + [str(check)], check=True, env=env)
    version = re.search(r'^m_EditorVersion:\s*(\S+)', (project / 'ProjectSettings/ProjectVersion.txt').read_text(), re.M).group(1).replace('\r', '')
    resolved = output / 'resolved-editor.txt'
    subprocess.run(shell + [str(project / 'Tools/CI/ResolveUnityEditor.ps1'), '-UnityVersion', version, '-OutputPath', str(resolved)],
                   check=True, env=env, stdout=subprocess.PIPE)
    unity = resolved.read_text(encoding='ascii')
    before = observe_endpoint(project, descriptor, output, 'before')
    launch = output / 'checked-no-quit-launch.ps1'; log = output / 'unity.log'
    launch.write_text("$ErrorActionPreference='Stop'\n& " + quoted(check) + "\n& " + quoted(project / 'Tools/CI/InvokeUnity.ps1') +
        ' -UnityExe ' + quoted(unity) + ' -ProjectPath ' + quoted(project) + ' -LogFile ' + quoted(log) +
        ' -TimeoutSeconds ' + str(args.timeout) + " -GuiLicensing -NoProcessExit -UnityArguments @('-executeMethod'," + quoted(METHOD) + ")\nif($global:LASTEXITCODE -ne 0){exit $global:LASTEXITCODE}\n$runtimeLog=[IO.File]::ReadAllText(" + quoted(log) + ")\nif(-not $runtimeLog.Contains(" + quoted('[SkirmishProductionRuntimeGate] result=Passed errors=0 jobs='+str(len(descriptor['jobs']))+' actualWrapperExitStillRequired=1') + ") -or $runtimeLog.Contains('[SkirmishProductionRuntimeGate] result=Failed')){throw 'Required whole-run runtime gate absent or failed'}\nexit 0\n", encoding='utf-8-sig')
    began = time.perf_counter(); result = None; after = None; wrapper_wall = None
    try:
        with (output / 'checked-wrapper-console.log').open('x', encoding='utf-8') as console:
            result = subprocess.run(shell + [str(launch)], cwd=project, env=env, stdout=console, stderr=subprocess.STDOUT)
    finally:
        wrapper_wall = time.perf_counter()-began
        write_new(output / 'outer-wrapper-receipt.json', dict(schema_version=1, descriptor_sha256=sha(descriptor_path),
            checked_wrapper_exit=None if result is None else result.returncode, elapsed_seconds=wrapper_wall,
            execute_method=METHOD, queue_success_enabled=False))
        after = observe_endpoint(project, descriptor, output, 'after')
    if result.returncode != 0:
        raise ValueError('Actual checked wrapper failed; retain all partial evidence')
    events = [json.loads(line) for line in (output / 'batch-events.jsonl').read_text(encoding='utf-8-sig').splitlines() if line.strip()]
    measured = verify_native_events(descriptor, events, log.read_text(encoding='utf-8-sig', errors='replace'), project)
    captured = next(row for row in events if row['kind'] == 'BatchCaptured')
    completed = next(row for row in events if row['kind'] == 'BatchCompleted')
    exit_request = next(row for row in events if row['kind'] == 'NativeExitInvocation')
    entry = next(row for row in events if row['kind'] == 'BeforeNativeScenePreparation')
    measured.update(result='FiniteBatchOperationallyCompletedUnqualified', wrapper_wall_seconds=wrapper_wall,
                    external_before_hash_seconds=before['elapsed_seconds'], external_after_hash_seconds=after['elapsed_seconds'],
                    entry_process_age_seconds=entry['facts']['entry_process_age_seconds'],
                    scene_preparation_seconds=captured['facts']['scene_preparation_milliseconds']/1000,
                    batch_context_to_completion_seconds=completed['elapsed_seconds'],
                    batch_context_to_exit_invocation_seconds=exit_request['elapsed_seconds'],
                    wrapper_overhead_and_shutdown_residual_seconds=wrapper_wall-exit_request['elapsed_seconds']-entry['facts']['entry_process_age_seconds'],
                    throughput_acceptance='Pending comparison to isolated runs; no throughput claim')
    write_new(output / 'batch-benchmark.json', measured)
    print(json.dumps(measured, indent=2)); return 0


if __name__ == '__main__':
    raise SystemExit(main())
