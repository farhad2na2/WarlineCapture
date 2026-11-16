#!/usr/bin/env python3
"""Replay S074 source owner with the original editor portable binding codec."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
R9 = ROOT / 'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9'
HOST_BUILD = ROOT / 'T Logs/SkirmishProduction/source-map-alias-s074-build-20261107-r11'
AUTHOR_SOURCE = R9 / 'candidate/S074CameraBindingAuthor.cs'
GRAPH = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json'
LAYOUT = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-provisional-source-path-layout-r2/layout.json'
MATRIX = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/experimental-opening-mix-matrix.csv'
CODEC_SOURCE = ROOT / 'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs'
CONTEXT = R9 / 'exporter/s074/source-factory-context.json'
CODEC_SHA = '9d72990488994613ad98b3713d351461cd984315834973f4643aedec26e443fa'
PASS = '[S074CameraBindingAuthor] result=PassedSourceOnlyCandidate'


def sha(path):
    digest = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def need(ok, reason):
    if not ok:
        raise ValueError(reason)


def extract_method(source, signature):
    start = source.find(signature)
    need(start >= 0 and source.find(signature, start + len(signature)) < 0,
         f'Exact single original portable codec method required: {signature}')
    brace = source.find('{', start + len(signature))
    need(brace >= 0, 'Original codec method body required')
    depth = 0
    for index in range(brace, len(source)):
        if source[index] == '{':
            depth += 1
        elif source[index] == '}':
            depth -= 1
            if depth == 0:
                return source[start:index + 1]
    raise ValueError('Unclosed original portable codec method')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output-dir', type=Path, required=True)
    args = parser.parse_args()
    out = args.output_dir.resolve()
    need(out.is_relative_to(ROOT) and not out.exists(), 'Fresh repository-owned T output directory required')
    need(sha(CODEC_SOURCE) == CODEC_SHA,
         'Original portable codec source pin changed')
    host_build = json.loads((HOST_BUILD / 'build.json').read_bytes())
    need(host_build.get('status') == 'Passed' and host_build.get('compile_exit') == 0 and
         host_build.get('mission') == 'S074' and host_build.get('seed') == 104803,
         'Current passed S074 source host build required')
    host_pins = host_build.get('source_and_reference_pins')
    need(isinstance(host_pins, dict), 'Exact current S074 host build source pins required')
    for path, expected in host_pins.items():
        need(sha(path) == expected, f'Current S074 host pin drift: {path}')
    candidate_receipt = json.loads((R9 / 'candidate/receipt.json').read_bytes())
    need(candidate_receipt.get('status') == 'PassedSourceOwnerCompile' and
         candidate_receipt.get('runtime_admission') is False and
         candidate_receipt.get('world_allocated') is False,
         'Retained actual S074 source-owner candidate required')
    context = json.loads(CONTEXT.read_bytes())
    target = context.get('target_context', {})
    need(context.get('kind') == 'OriginalCompilerDerivedNativeFactoryContext' and
         target.get('catalog_id') == 'S074' and target.get('seed') == 104803 and
         target.get('setup_hash') == 1298808707 and context.get('world_allocated') is False and
         context.get('source_fit_certified') is False and context.get('mission_ready') is False,
         'Exact source-only S074 r9 compiler context required')

    owner = out / 'owner'
    owner.mkdir(parents=True)
    author = owner / 'S074CameraBindingAuthor.cs'
    runner = owner / 'run.py'
    author_text = AUTHOR_SOURCE.read_text()
    codec_text = CODEC_SOURCE.read_text()
    # The original outer exporter method intentionally enforces S026's
    # twelve-variant domain. Reuse its exact lower-level field codec instead;
    # the S074 source owner has one compiler-selected variant.
    codec_methods = extract_method(codec_text, 'static JToken PortableLogicalValue(')
    marker = '    private static JObject Serialize('
    need(author_text.count(marker) == 1, 'Exact source-owner method insertion point required')
    author_text = author_text.replace(marker, codec_methods + '\n\n' + marker, 1)
    run_anchor = '            var outDir = Path.GetFullPath(args[3]);'
    need(author_text.count(run_anchor) == 1, 'Exact actual-binding export anchor required')
    author_text = author_text.replace(run_anchor,
        '            var portableBinding = new JObject { ["schema_version"] = 1, ["kind"] = "DeclaredOriginalLogicalBindingFieldsWithInlineViews", ["float_encoding"] = "IEEE754_float32_hex_bits", ["unity_object_ids_exported"] = false, ["fields"] = PortableLogicalValue(roundTrippedBinding, 0) };\n' + run_anchor, 1)
    write_anchor = '            File.WriteAllText(Path.Combine(outDir, "mission-logical-binding.json"), bindingJson.ToString(Newtonsoft.Json.Formatting.Indented) + "\\n");'
    need(author_text.count(write_anchor) == 1, 'Exact source-owner raw binding export required')
    author_text = author_text.replace(write_anchor, write_anchor +
        '\n            File.WriteAllText(Path.Combine(outDir, "portable-binding.json"), portableBinding.ToString(Newtonsoft.Json.Formatting.Indented) + "\\n");', 1)
    author.write_text(author_text)

    csc = next((Path(path) for path in host_pins if path.endswith('/Roslyn/bincore/csc.dll')), None)
    need(csc is not None and sha(csc) == host_pins[str(csc)], 'Current pinned Roslyn compiler required')
    response = [json.loads(line) for line in (HOST_BUILD / 'compile.rsp').read_text().splitlines() if line.strip()]
    need(response.count('-main:SourceSimulationProgram') == 1 and
         response.count('-out:' + str(HOST_BUILD / 'SourceSimulation.exe')) == 1,
         'Exact current host compiler response required')
    response = [('-main:S074CameraBindingAuthor' if row == '-main:SourceSimulationProgram' else
                 '-out:' + str(owner / 'source-owner.exe') if row == '-out:' + str(HOST_BUILD / 'SourceSimulation.exe') else row)
                for row in response]
    need(str(author) not in response, 'Candidate author must be appended exactly once')
    response.append(str(author))
    rsp = owner / 'compile.rsp'
    rsp.write_text(''.join(json.dumps(row) + '\n' for row in response))
    exe = owner / 'source-owner.exe'
    compiled = subprocess.run(['/usr/local/share/dotnet/dotnet', str(csc), '@' + str(rsp)],
                              cwd=ROOT, capture_output=True, text=True, timeout=300)
    (owner / 'compile.log').write_text(compiled.stdout + compiled.stderr)
    need(compiled.returncode == 0 and exe.is_file(),
         f'Current S074 source-owner replay compilation failed (exit={compiled.returncode})')
    runtime = host_build['runtime_selection']
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((runtime['engine'], str(Path(runtime['newtonsoft']).parent),
                                        str(ROOT / 'Library/ScriptAssemblies'),
                                        str(HOST_BUILD / 'placement'), str(owner)))
    run = subprocess.run([runtime['mono'], '--optimize=-float32', str(exe),
                          str(GRAPH), str(LAYOUT), str(MATRIX), str(owner)],
                         cwd=ROOT, env=env, capture_output=True, text=True, timeout=600)
    (owner / 'run.stdout.log').write_text(run.stdout)
    (owner / 'run.stderr.log').write_text(run.stderr)
    need(run.returncode == 0 and PASS in run.stdout,
         f'Actual S074 compiler author replay failed (exit={run.returncode}); preserve logs')
    for path, expected in host_pins.items():
        need(sha(path) == expected, f'Current S074 host pin drift during owner replay: {path}')

    owner_binding = owner / 'mission-logical-binding.json'
    portable_path = owner / 'portable-binding.json'
    actual_raw = json.loads(owner_binding.read_bytes())
    portable = json.loads(portable_path.read_bytes())
    previous_raw = R9 / 'candidate/mission-logical-binding.json'
    need(sha(owner_binding) == sha(previous_raw),
         'Fresh compiler-author binding drifted from retained r9 S074 binding')
    need(portable.get('kind') == 'DeclaredOriginalLogicalBindingFieldsWithInlineViews' and
         portable.get('float_encoding') == 'IEEE754_float32_hex_bits' and
         portable.get('unity_object_ids_exported') is False and isinstance(portable.get('fields'), dict),
         'Original portable codec output required')
    need(portable['fields'].get('CatalogId') == actual_raw.get('CatalogId') == 'S074' and
         len(portable['fields'].get('Variants', [])) == 1,
         'Portable binding identity differs from actual source owner')

    context_pin = {'path': str(CONTEXT.resolve()), 'sha256': sha(CONTEXT)}
    wrapper = {
        'schema_version': 1,
        'kind': 'OriginalCompilerDeclaredLogicalBinding',
        'metadata_compiler_context': context_pin,
        'target_context': target,
        'portable_binding': portable,
        'native_capture': False,
        'source_fit_certified': False,
        'mission_ready': False,
        'source_owner_binding': {'path': str(owner_binding.resolve()), 'sha256': sha(owner_binding)},
    }
    wrapper_path = out / 'logical-binding.json'
    wrapper_path.write_text(json.dumps(wrapper, indent=2, sort_keys=True) + '\n')
    owner_receipt = {
        'kind': 'S074CurrentCompilerAuthoredLogicalBindingReplay',
        'status': 'PassedSourceOwnerCompile',
        'compile_exit': compiled.returncode,
        'owner_exit': run.returncode,
        'compiler': 'Current SkirmishSetupCompiler + SkirmishMissionLogicalBindingCompiler',
        'host_build': {'path': str((HOST_BUILD / 'build.json').resolve()), 'sha256': sha(HOST_BUILD / 'build.json')},
        'source_inputs_sha256': host_pins | {str(CODEC_SOURCE.resolve()): sha(CODEC_SOURCE)},
        'binding_sha256': sha(owner_binding),
        'portable_binding_sha256': sha(portable_path),
        'world_allocated': False, 'runtime_admission': False,
        'source_fit_certified': False, 'mission_ready': False,
    }
    owner_receipt_path = owner / 'receipt.json'
    owner_receipt_path.write_text(json.dumps(owner_receipt, indent=2, sort_keys=True) + '\n')
    receipt = {
        'kind': 'S074OriginalCompilerDeclaredLogicalBindingPortableProjection',
        'status': 'ProjectedSourceOnly',
        'source_owner_replay': {'path': str(owner_receipt_path.resolve()), 'sha256': sha(owner_receipt_path)},
        'source_owner_binding': wrapper['source_owner_binding'],
        'original_portable_codec_source': {'path': str(CODEC_SOURCE.resolve()), 'sha256': sha(CODEC_SOURCE)},
        'compiler_context': context_pin,
        'logical_binding': {'path': str(wrapper_path.resolve()), 'sha256': sha(wrapper_path)},
        'world_allocated': False, 'native_capture': False,
        'source_fit_certified': False, 'mission_ready': False,
        'original_reader_decode_and_compiler_roundtrip': 'pending-source-run-preflight',
    }
    receipt_path = out / 'receipt.json'
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n')
    print(json.dumps({'status': receipt['status'], 'logical_binding': str(wrapper_path),
                      'sha256': sha(wrapper_path), 'compiler_roundtrip': 'pending-source-run-preflight'}))


if __name__ == '__main__':
    main()
