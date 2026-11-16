#!/usr/bin/env python3
"""Compile current S003 importer in a disposable, noncanonical source fixture."""
import argparse
import hashlib
import importlib.util
import json
import os
import runpy
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests'))
from finalize_s003_refreshed_prefab_capture import qualify

OLD_BUILD = ROOT / 'T Logs/SkirmishProduction/s003-current-prefab-import-build-20261104-T1/build.json'
CAPTURE = ROOT / 'Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1/data/prefab-reference-data.json'
AUDIT = ROOT / 'T Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1/independent-audit.json'
LOG = ROOT / 'T Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1-editor.log'
MOVEMENT = ROOT / 'T Logs/SkirmishProduction/s003-current-movement-source-input-20261105-T1/unit-movement-source-input.json'
KEYS = ROOT / 'T Logs/SkirmishProduction/s003-refreshed-prefab-keys-20261104-T1/prefab-keys.json'
HARNESS = ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests/S003RefreshedPrefabImportControl.cs'
IMPORTER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/305_GeneratedReferenceTemplateImporter.s003.cs'
AUTHORITY = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceAuthoredUnitMetadataAuthority.cs'
MARKER = '[S003RefreshedPrefabImportControl] result=Passed roots=19 entities=647 unsupported=32 omitted=26 negatives=3 sourceProjection=Passed world=0 readiness=0'


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for data in iter(lambda: f.read(1024 * 1024), b''):
            h.update(data)
    return h.hexdigest()


def current_s003_structure_fragment(repo, output):
    """Apply the ordinary current S003 manifest's exact original-member recipe."""
    manifest_path = repo/'Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json'
    manifest = json.loads(manifest_path.read_bytes())
    rows = [x for x in manifest['sources'] if x.get('output') == '335_SkirmishScenarioStructureBinding.source.cs']
    if len(rows) != 1 or rows[0].get('kind') != 'current_original':
        raise ValueError('Exact current S003 original structure extraction required')
    boot = runpy.run_path(str(repo/'Tools/CI/run_skirmish_source_simulation.py'), run_name='qualified_test_bootstrap')
    owned = boot['owned']
    resolver = owned.require('skirmish_source_manifest')
    causal = owned.require('skirmish_causal_worker')
    syntax = owned.require('skirmish_source_syntax')
    entry = rows[0]
    pins = {str(manifest_path): sha(manifest_path)}
    text = resolver.read(resolver.relative(repo, entry['template']), pins)
    proof = []
    for member in entry['members']:
        source = resolver.relative(repo, member['path'])
        original = resolver.read(source, pins)
        resolver.shape(original, member)
        projected = causal._shipping(original) if member.get('shipping', True) else original
        piece = resolver.select(projected, member, syntax.method_span)
        piece = resolver.bind(piece, member.get('bindings', []), proof)
        marker = '/*SOURCE:' + member['id'] + '*/'
        if text.count(marker) != 1:
            raise ValueError('Unique current source fragment marker required')
        text = text.replace(marker, piece)
    if '/*SOURCE:' in text:
        raise ValueError('Unresolved current structure extraction marker')
    destination = output/'335_SkirmishScenarioStructureBinding.source.cs'
    destination.write_text(text)
    pins[str(destination)] = sha(destination)
    return destination, pins


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--capture', type=Path, default=CAPTURE)
    parser.add_argument('--audit', type=Path, default=AUDIT)
    parser.add_argument('--log', type=Path, default=LOG)
    parser.add_argument('--movement', type=Path, default=MOVEMENT)
    parser.add_argument('--fresh-shared-build', type=Path)
    options = parser.parse_args()
    capture, audit_path, capture_log, movement = (p.resolve(strict=True) for p in (options.capture, options.audit, options.log, options.movement))
    out = options.output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must be inside the repository')
    out.mkdir(parents=True, exist_ok=False)
    build = json.loads(OLD_BUILD.read_bytes())
    response = Path(build['compiler_response']['path'])
    if build.get('mission') != 'S003' or sha(response) != build['compiler_response']['sha256']:
        raise ValueError('Historical S003 compiler response required')
    args = [json.loads(x) for x in response.read_text().splitlines() if x.strip()]
    extra_current_pins = {}
    if options.fresh_shared_build is not None:
        fresh_path = options.fresh_shared_build.resolve(strict=True)
        fresh = json.loads(fresh_path.read_bytes())
        if fresh.get('status') != 'Passed' or fresh.get('mission') != 'S002' or fresh.get('compile_exit') != 0:
            raise ValueError('Fresh shared source build required')
        fresh_flags = fresh_path.parent / 'source/generated/253_SourceUiPublicFlags.cs'
        fresh_pins = fresh.get('source_and_reference_pins', {})
        if fresh_pins.get(str(fresh_flags)) != sha(fresh_flags):
            raise ValueError('Fresh generated public flags must match the source build')
        flag_indices = [i for i, value in enumerate(args) if value.endswith('/253_SourceUiPublicFlags.cs')]
        if len(flag_indices) != 1:
            raise ValueError('Exact historical public-flags source required')
        args[flag_indices[0]] = str(fresh_flags)
        matrix_owner = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceExternalSetupMatrix.cs'
        if fresh_pins.get(str(matrix_owner)) != sha(matrix_owner) or str(matrix_owner) in args:
            raise ValueError('Fresh external matrix source must match the source build')
        args.append(str(matrix_owner))
        extra_current_pins = {str(fresh_path): sha(fresh_path), str(fresh_flags): sha(fresh_flags),
                              str(matrix_owner): sha(matrix_owner)}
        layout_source = ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceValidatedLayoutJson.cs'
        if fresh_pins.get(str(layout_source)) != sha(layout_source) or str(layout_source) in args:
            raise ValueError('Fresh validated-layout source must match the shared source build')
        args.append(str(layout_source))
        fresh_fragment, fragment_pins = current_s003_structure_fragment(ROOT, out)
        fragment_indices = [i for i, value in enumerate(args) if value.endswith('/335_SkirmishScenarioStructureBinding.source.cs')]
        if len(fragment_indices) != 1:
            raise ValueError('Exact historical structure fragment required')
        args[fragment_indices[0]] = str(fresh_fragment)
        extra_current_pins.update(fragment_pins)
    if args.count(str(IMPORTER)) != 1:
        raise ValueError('Exact original S003 importer source must be compiled')
    # The historical response predates this dependency of the current shared decoder.
    if str(AUTHORITY) not in args:
        args.append(str(AUTHORITY))
    generators = [ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/generate_player_placement.py',
                  ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/generate_source_skirmish_surface_placement.py']
    generated = out / 'generated'
    for i, path in enumerate(generators):
        spec = importlib.util.spec_from_file_location('s003_fixture_generator_' + str(i), path)
        if spec is None or spec.loader is None:
            raise ValueError('Original current source generator missing')
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        module.generate(ROOT, generated)
    for i, arg in enumerate(args):
        name = Path(arg).name
        if name.startswith('S003PlayerPlacement_'):
            replacement = generated / name.removeprefix('S003PlayerPlacement_')
            if not replacement.is_file():
                raise ValueError('Current generated placement source missing: ' + name)
            args[i] = str(replacement)
    args.append(str(generated / 'SourceSkirmishSurfacePlacementOwner.cs'))
    checked = qualify(ROOT, capture, capture_log, 0)
    audit = json.loads(audit_path.read_bytes())
    if audit.get('status') != 'PassedDataOnlyCapture' or audit.get('capture') != checked.get('capture') or audit['capture']['sha256'] != sha(capture):
        raise ValueError('Current independently audited capture required')
    pins = {str(p): sha(p) for p in (OLD_BUILD, response, capture, capture_log, audit_path, movement, KEYS, HARNESS, Path(__file__).resolve())}
    pins.update(extra_current_pins)
    manifest = ROOT / 'Tools/CI/SkirmishSourceSimulation/source_manifest.s003.plan.json'
    manifest_data = json.loads(manifest.read_bytes())
    for path in [manifest, *generators]:
        pins[str(path)] = sha(path)
    for entry in manifest_data['sources']:
        if entry.get('generator') in [str(p.relative_to(ROOT)) for p in generators]:
            for name in entry.get('generator_inputs', []):
                path = ROOT / name
                pins[str(path)] = sha(path)
    for arg in args:
        value = arg[3:] if arg.startswith('-r:') else arg
        if arg.startswith('-r:') and '=' in value:
            value = value.split('=', 1)[1]
        if value.endswith(('.cs', '.dll')) and Path(value).is_file():
            path = Path(value).resolve()
            pins[str(path)] = sha(path)
    for name, digest in json.loads(movement.read_bytes())['source_pins'].items():
        path = str((ROOT / name).resolve())
        if path in pins and pins[path] != digest:
            raise ValueError('Conflicting movement source pin: ' + path)
        pins[path] = digest
    def verify():
        for name, expected in pins.items():
            if not Path(name).is_file() or sha(Path(name)) != expected:
                raise ValueError('Fixture source changed: ' + name)
    verify()
    args = [('-main:S003RefreshedPrefabImportControl' if v.startswith('-main:') else '-out:' + str(out / 'control.exe') if v.startswith('-out:') else v) for v in args]
    args.append(str(HARNESS))
    rsp = out / 'compile.rsp'
    rsp.write_text(''.join(json.dumps(v) + '\n' for v in args))
    csc = next(Path(name) for name in build['source_and_reference_pins'] if name.endswith('/Roslyn/bincore/csc.dll'))
    pins[str(csc)] = sha(csc)
    compile_run = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], capture_output=True, text=True, timeout=300)
    (out / 'compile.log').write_text(compile_run.stdout + compile_run.stderr)
    if compile_run.returncode:
        raise RuntimeError('S003 source fixture compile failed')
    runtime = build['runtime_selection']
    pins[runtime['mono']] = sha(Path(runtime['mono']))
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((str(OLD_BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(out)))
    run = subprocess.run([runtime['mono'], str(out / 'control.exe'), str(capture), str(KEYS), str(movement)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (out / 'run.log').write_text(run.stdout + run.stderr)
    if run.returncode or run.stdout.splitlines().count(MARKER) != 1:
        raise RuntimeError('Actual S003 importer fixture failed')
    verify()
    if qualify(ROOT, capture, capture_log, 0)['capture'] != audit['capture']:
        raise ValueError('Captured inputs changed')
    receipt = {'status': 'PassedDisposableS003ImporterFixture', 'marker': MARKER,
               'compiled_importer': str(IMPORTER), 'source_pins_rechecked': len(pins),
               'source_pins': pins, 'capture_sha256': sha(capture), 'movement_source_projection_checked': True,
               'scope': 'Noncanonical fixture compiled from historical response and current sources; no S003 runtime admission',
               'world_allocated': False, 'native_context_qualified': False, 'mission_ready': False}
    (out / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({k: v for k, v in receipt.items() if k != 'source_pins'}))


if __name__ == '__main__':
    main()
