#!/usr/bin/env python3
"""Replay the current S074 source compiler/exporter, then project shared factory facts."""
from __future__ import annotations

import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time

from project_s074_source_factory import ROOT, _project_core, digest

RECIPE = Path(__file__).resolve().parent
CANDIDATE_SOURCE = ROOT / 'T Logs/SkirmishProduction/s074-camera-binding-candidate-20261107-r1'
EXPORTER_SOURCE = ROOT / 'T Logs/SkirmishProduction/s074-camera-binding-factory-requalification-20261107-r2'
TEMPLATE_INITIALIZATION = ROOT / 'T Logs/SkirmishProduction/s026-direct-layout-runtime-inputs-20261107-r1/initialization.json'
EXPECTED_CANDIDATE_RUNNER = CANDIDATE_SOURCE / 'run.py'
EXPECTED_CANDIDATE_AUTHOR = CANDIDATE_SOURCE / 'S074CameraBindingAuthor.cs'
EXPECTED_EXPORTER_BUILD = EXPORTER_SOURCE / 'build.py'
EXPECTED_EXPORTER_RUN = EXPORTER_SOURCE / 'run.py'
PASS_KIND = 'S074CameraBindingFactoryReplayProjection'


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def read_json(path: Path):
    return json.loads(path.read_bytes())


def copy_pinned(source: Path, destination: Path) -> str:
    source = source.resolve(strict=True)
    before = digest(source)
    shutil.copyfile(source, destination)
    need(digest(destination) == before and digest(source) == before,
         'Source changed while staging replay input: ' + str(source))
    return before


def verify_pin_map(pins, label: str) -> None:
    need(isinstance(pins, dict) and pins, label + ' pin map required')
    for name, expected in pins.items():
        path = Path(name)
        need(path.is_file() and digest(path) == expected, label + ' input changed: ' + name)


def run_child(command: list[str], cwd: Path, log_path: Path, timeout: int, label: str) -> None:
    started = time.monotonic()
    try:
        child = subprocess.run(command, cwd=cwd, capture_output=True, text=True, timeout=timeout)
        log_path.write_text(child.stdout + child.stderr)
    except subprocess.TimeoutExpired as error:
        stdout = error.stdout or ''
        stderr = error.stderr or ''
        if isinstance(stdout, bytes):
            stdout = stdout.decode('utf-8', errors='replace')
        if isinstance(stderr, bytes):
            stderr = stderr.decode('utf-8', errors='replace')
        log_path.write_text(str(stdout) + str(stderr) + '\nREPLAY_TIMEOUT\n')
        raise ValueError(label + ' timed out after ' + str(timeout) + ' seconds') from error
    need(child.returncode == 0, label + ' failed with exit=' + str(child.returncode) + '; see ' + str(log_path))
    need(time.monotonic() - started <= timeout, label + ' exceeded its bounded duration')


def replay(output: Path, exporter_host_path: Path) -> dict:
    output = output.absolute()
    need(output.is_relative_to(ROOT / 'T Logs'), 'Replay output must be under repository T Logs')
    exporter_host_path = exporter_host_path.resolve(strict=True)
    need(exporter_host_path.name == 'build.json', 'Exporter host build receipt must be named build.json')
    output.mkdir(parents=True, exist_ok=False)
    candidate_out = output / 'candidate'
    exporter_out = output / 'exporter'
    candidate_out.mkdir()
    exporter_out.mkdir()
    try:
        # Verify the source-host builds through the normal canonical source provenance validator.
        runner_path = ROOT / 'Tools/CI/run_skirmish_source_simulation.py'
        runner_spec = importlib.util.spec_from_file_location('s074_factory_replay_source_cli', runner_path)
        need(runner_spec is not None and runner_spec.loader is not None,
             'Checked direct-entry source build loader unavailable')
        runner = importlib.util.module_from_spec(runner_spec)
        runner_spec.loader.exec_module(runner)
        skirmish_source_build = runner.owned.require('skirmish_source_build')

        candidate_build_dir = output / 'candidate-host-build'
        run_child([sys.executable, str(ROOT / 'Tools/CI/run_skirmish_source_simulation.py'), 'build',
                   '--repo', str(ROOT), '--package-root', str(ROOT), '--output', str(candidate_build_dir),
                   '--mission', 'S002', '--seed', '104731'], ROOT,
                  output / 'candidate-host-build.log', 900, 'Fresh canonical S002 source host build')
        candidate_build_path = candidate_build_dir / 'build.json'
        candidate_build = read_json(candidate_build_path)
        candidate_provenance = skirmish_source_build.source_build_provenance(candidate_build)
        need(candidate_provenance.get('trusted') is True and candidate_provenance.get('canonical') is True,
             'Fresh canonical candidate source host build required')
        verify_pin_map(candidate_build.get('source_and_reference_pins'), 'Fresh candidate host build')

        exporter_host = read_json(exporter_host_path)
        exporter_provenance = skirmish_source_build.source_build_provenance(exporter_host)
        need(exporter_provenance.get('trusted') is True,
             'Trusted pinned source-map host build required')
        verify_pin_map(exporter_host.get('source_and_reference_pins'), 'Exporter source host build')

        # Re-run the candidate source owner into a fresh directory; no prior success receipt is consumed.
        candidate_runner = candidate_out / 'run.py'
        candidate_author = candidate_out / 'S074CameraBindingAuthor.cs'
        candidate_runner_source_sha = digest(EXPECTED_CANDIDATE_RUNNER)
        candidate_runner_bytes = EXPECTED_CANDIDATE_RUNNER.read_bytes()
        old_candidate_build = "BUILD = BASE / 'shared-resource-snapshot-s002-build-r1/build.json'"
        need(candidate_runner_bytes.count(old_candidate_build.encode()) == 1,
             'Exact candidate source host binding point required')
        candidate_runner_bytes = candidate_runner_bytes.replace(
            old_candidate_build.encode(),
            ("BUILD = pathlib.Path(" + repr(str(candidate_build_path)) + ")").encode())
        candidate_runner.write_bytes(candidate_runner_bytes)
        candidate_runner_sha = hashlib.sha256(candidate_runner_bytes).hexdigest()
        candidate_author_sha = copy_pinned(EXPECTED_CANDIDATE_AUTHOR, candidate_author)
        run_child([sys.executable, str(candidate_runner)], ROOT, candidate_out / 'driver.log', 900,
                  'S074 candidate compiler/source owner replay')
        candidate_receipt_path = candidate_out / 'receipt.json'
        candidate_proof_path = candidate_out / 'candidate-proof.json'
        candidate_binding_path = candidate_out / 'mission-logical-binding.json'
        candidate_receipt = read_json(candidate_receipt_path)
        candidate_proof = read_json(candidate_proof_path)
        candidate_binding = read_json(candidate_binding_path)
        need(candidate_receipt.get('status') == 'PassedSourceOwnerCompile' and
             candidate_receipt.get('compile_exit') == 0 and candidate_receipt.get('owner_exit') == 0 and
             candidate_receipt.get('mission_ready') is False,
             'Replayed source-only candidate owner did not pass')
        need(candidate_proof.get('status') == 'CompiledSourceOnlyCandidate' and
             candidate_proof.get('catalog_id') == 'S074' and candidate_proof.get('seed') == 104803 and
             candidate_proof.get('size') == 'Standard' and candidate_proof.get('difficulty') == 'Regular' and
             candidate_proof.get('mission_ready') is False and candidate_proof.get('world_allocated') is False,
             'Replayed S074 candidate context differs')
        need(len(candidate_binding.get('Variants', [])) == 1 and candidate_binding.get('CatalogId') == 'S074',
             'Replayed candidate binding differs')
        verify_pin_map(candidate_receipt.get('source_inputs_sha256'), 'Replayed candidate compiler')
        need(digest(EXPECTED_CANDIDATE_RUNNER) == candidate_runner_source_sha,
             'Original candidate compiler runner changed during replay')
        need(candidate_receipt.get('binding_sha256') == digest(candidate_binding_path) and
             candidate_receipt.get('candidate_proof_sha256') == digest(candidate_proof_path),
             'Replayed candidate receipt does not bind actual proof/binding outputs')

        # Stage exact exporter source scripts. The sole runner substitution points it at the freshly
        # produced candidate above; both original and effective script bytes are retained and pinned.
        exporter_build = exporter_out / 'build.py'
        exporter_run_source = EXPECTED_EXPORTER_RUN.resolve(strict=True)
        exporter_run_bytes = exporter_run_source.read_bytes()
        exporter_run_sha = hashlib.sha256(exporter_run_bytes).hexdigest()
        old_candidate = "CANDIDATE = ROOT / 'T Logs/SkirmishProduction/s074-camera-binding-candidate-20261107-r1'"
        old_run_host = "HOST = ROOT / 'T Logs/SkirmishProduction/source-map-host-build-20261107-r7'"
        need(exporter_run_bytes.count(old_candidate.encode()) == 1,
             'Exact current exporter runner candidate binding point required')
        need(exporter_run_bytes.count(old_run_host.encode()) == 1,
             'Exact current exporter runner host binding point required')
        patched_run = exporter_run_bytes.replace(
            old_candidate.encode(),
            ("CANDIDATE = pathlib.Path(" + repr(str(candidate_out)) + ")").encode(),
        )
        patched_run = patched_run.replace(
            old_run_host.encode(),
            ("HOST = pathlib.Path(" + repr(str(exporter_host_path.parent)) + ")").encode(),
        )
        exporter_run = exporter_out / 'run.py'
        exporter_build_source_sha = digest(EXPECTED_EXPORTER_BUILD)
        exporter_build_bytes = EXPECTED_EXPORTER_BUILD.read_bytes()
        old_host = "HOST = ROOT / 'T Logs/SkirmishProduction/source-map-host-build-20261107-r7'"
        need(exporter_build_bytes.count(old_host.encode()) == 1,
             'Exact original exporter host binding point required')
        exporter_build_bytes = exporter_build_bytes.replace(
            old_host.encode(),
            ("HOST = pathlib.Path(" + repr(str(exporter_host_path.parent)) + ")").encode(),
        )
        exporter_build.write_bytes(exporter_build_bytes)
        exporter_build_sha = hashlib.sha256(exporter_build_bytes).hexdigest()
        need(digest(EXPECTED_EXPORTER_BUILD) == exporter_build_source_sha and
             digest(exporter_build) == exporter_build_sha,
             'Original exporter build changed while staging fresh host')
        exporter_run.write_bytes(patched_run)
        effective_run_sha = hashlib.sha256(patched_run).hexdigest()
        need(digest(exporter_run_source) == exporter_run_sha and digest(exporter_run) == effective_run_sha,
             'Exporter runner changed while staging replay')

        run_child([sys.executable, str(exporter_build)], ROOT, exporter_out / 'build-driver.log', 300,
                  'Original S074 factory exporter build replay')
        build_receipt_path = exporter_out / 'build-receipt.json'
        exporter_executable = exporter_out / 'SourceSimulation.export.exe'
        build_receipt = read_json(build_receipt_path)
        need(build_receipt.get('kind') == 'S074CameraBindingFreshOriginalOwnerExporterBuild' and
             build_receipt.get('status') == 'Passed' and build_receipt.get('compile_exit') == 0 and
             build_receipt.get('executable_sha256') == digest(exporter_executable),
             'Fresh original exporter build did not pass')
        verify_pin_map(build_receipt.get('source_inputs_sha256'), 'Original exporter build')
        build_inputs = build_receipt['source_inputs_sha256']
        host_build = read_json(exporter_host_path)
        verify_pin_map(host_build.get('source_and_reference_pins'), 'Original exporter source host')

        run_child([sys.executable, str(exporter_run)], ROOT, exporter_out / 'run-driver.log', 900,
                  'Original S074 compiler/factory exporter replay')
        producer_run_receipt_path = exporter_out / 'receipt.json'
        producer_run = read_json(producer_run_receipt_path)
        context_path = exporter_out / 's074/source-factory-context.json'
        result_path = exporter_out / 's074/authoring-result.json'
        process_log_path = exporter_out / 's074/process.log'
        context = read_json(context_path)
        result = read_json(result_path)
        target = context['target_context']
        observed_setup_hash = result['resolved_setup']['setup_hash']
        need(producer_run.get('kind') == 'S074CameraBindingFactoryRequalification' and
             producer_run.get('status') == 'PassedOriginalOwnerExport' and producer_run.get('process_exit') == 0 and
             producer_run.get('timed_out') is False and result.get('compiler_accepted') is True and
             target.get('setup_hash') == observed_setup_hash,
             'Observed original compiler/exporter result did not pass its bound target')
        need(target.get('catalog_id') == 'S074' and target.get('seed') == 104803 and
             target.get('size') == 'Standard' and target.get('difficulty') == 'Regular' and
             target.get('operation_map_id') == 'opmap.skirmish.industrial_basin',
             'Observed original compiler/exporter target differs from S074')
        need(context.get('world_allocated') is False and context.get('identities_allocated') == 0 and
             context.get('runtime_loader_admitted') is False and context.get('source_fit_certified') is False and
             context.get('mission_ready') is False,
             'Observed factory export crossed its source-only boundary')
        need(producer_run.get('context_sha256') == digest(context_path) and
             producer_run.get('authoring_result_sha256') == digest(result_path) and
             producer_run.get('process_log_sha256') == digest(process_log_path) and
             producer_run.get('setup_hash') == observed_setup_hash,
             'Observed exporter receipt does not bind actual process outputs')

        # Construct the projector receipt from this invocation's verified child outputs, never from
        # a caller-supplied success receipt. The S026 initialization below supplies templates only.
        source_inputs = {}
        for pin_set in (candidate_receipt['source_inputs_sha256'], build_inputs,
                        exporter_host['source_and_reference_pins']):
            for name, expected in pin_set.items():
                need(name not in source_inputs or source_inputs[name] == expected,
                     'Conflicting replay source pin: ' + name)
                source_inputs[name] = expected
        source_inputs.update({
            str(candidate_runner): candidate_runner_sha,
            str(candidate_author): candidate_author_sha,
            str(EXPECTED_EXPORTER_BUILD): exporter_build_source_sha,
            str(exporter_build): exporter_build_sha,
            str(exporter_run): effective_run_sha,
            str(exporter_run_source): exporter_run_sha,
            str(Path(__file__).resolve()): digest(Path(__file__).resolve()),
            str(RECIPE / 'project_s074_source_factory.py'): digest(RECIPE / 'project_s074_source_factory.py'),
            str(producer_run_receipt_path): digest(producer_run_receipt_path),
            str(candidate_receipt_path): digest(candidate_receipt_path),
            str(candidate_proof_path): digest(candidate_proof_path),
            str(candidate_binding_path): digest(candidate_binding_path),
            str(exporter_executable): digest(exporter_executable),
        })
        for path in (context_path, result_path, process_log_path):
            source_inputs[str(path)] = digest(path)
        for name, expected in source_inputs.items():
            need(Path(name).is_file() and digest(Path(name)) == expected,
                 'Replay source input changed before projection: ' + name)

        replay_receipt_path = exporter_out / 'factory-context-replay-receipt.json'
        replay_receipt = {
            'kind': 'S074OriginalFactoryContextDiagnostic',
            'status': 'PassedSourceFactoryContext',
            'process_exit': 0,
            'timed_out': False,
            'source_only': True,
            'world_allocated': False,
            'native_context_qualified': False,
            'source_fit_certified': False,
            'mission_ready': False,
            'source_inputs': source_inputs,
            'result_sha256': digest(result_path),
            'context_sha256': digest(context_path),
            'process_log_sha256': digest(process_log_path),
            'replay_evidence': {
                'candidate_owner_receipt_sha256': digest(candidate_receipt_path),
                'candidate_host_build_sha256': digest(candidate_build_path),
                'exporter_build_receipt_sha256': digest(build_receipt_path),
                'exporter_run_receipt_sha256': digest(producer_run_receipt_path),
                'observed_setup_hash': observed_setup_hash,
            },
            'scope': 'Fresh replay of the current S074 source compiler and original factory exporter; S026 data is used only as the validated shared-factory template baseline. No S074 initializer, world, gameplay, source-fit or readiness claim.',
        }
        replay_receipt_path.write_text(json.dumps(replay_receipt, indent=2, sort_keys=True) + '\n')

        report = _project_core(context_path, replay_receipt_path, TEMPLATE_INITIALIZATION,
                               output / 'projection', replay_setup_hash=observed_setup_hash)
        need(report.get('status') == 'ProjectedSourceOnly' and report.get('target_context', {}).get('setup_hash') == observed_setup_hash and
             report.get('world_allocated') is False and report.get('runtime_admission') is False and
             report.get('source_fit_certified') is False and report.get('mission_ready') is False,
             'Source-only replay projection failed its output boundary')
        final = {
            'kind': PASS_KIND,
            'status': 'ReplayProjectedSourceOnly',
            'candidate_compiler': {'receipt': str(candidate_receipt_path), 'proof': str(candidate_proof_path),
                                   'binding': str(candidate_binding_path),
                                   'fresh_host_build': str(candidate_build_path)},
            'exporter': {'build_receipt': str(build_receipt_path), 'run_receipt': str(producer_run_receipt_path),
                         'context': str(context_path), 'authoring_result': str(result_path),
                         'process_log': str(process_log_path)},
            'projection_receipt': {'path': str(output / 'projection/receipt.json'),
                                   'sha256': digest(output / 'projection/receipt.json')},
            'template_baseline': {'path': str(TEMPLATE_INITIALIZATION),
                                  'sha256': digest(TEMPLATE_INITIALIZATION),
                                  'scope': 'S026 shared factory equality templates only; never an S074 initializer'},
            'setup_hash': observed_setup_hash,
            'world_allocated': False,
            'runtime_admission': False,
            'source_fit_certified': False,
            'mission_ready': False,
        }
        (output / 'replay-projection-receipt.json').write_text(json.dumps(final, indent=2, sort_keys=True) + '\n')
        print(json.dumps({'status': final['status'], 'setup_hash': observed_setup_hash,
                          'projection': final['projection_receipt'], 'mission_ready': False}))
        return final
    except Exception as error:
        (output / 'failure.json').write_text(json.dumps({
            'kind': PASS_KIND, 'status': 'Failed', 'error': str(error),
            'world_allocated': False, 'mission_ready': False,
            'artifacts_preserved': True,
        }, indent=2, sort_keys=True) + '\n')
        raise


def main() -> int:
    if len(sys.argv) != 3:
        print('usage: replay_s074_source_factory.py <fresh-output-under-T-Logs> <fresh-trusted-exporter-host-build.json>', file=sys.stderr)
        return 2
    try:
        replay(Path(sys.argv[1]), Path(sys.argv[2]))
        return 0
    except Exception as error:
        print('[S074FactoryReplay] status=Failed ' + str(error), file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
