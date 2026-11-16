#!/usr/bin/env python3
"""Replay the shared binding author with the game's original portable field codec."""
import argparse
import concurrent.futures
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[4]
AUTHOR = ROOT / 'Tools/CI/SkirmishMissionLogicalBindingAuthor.cs'
CODEC = ROOT / 'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs'
CODEC_SHA = '9d72990488994613ad98b3713d351461cd984315834973f4643aedec26e443fa'
EXTRACTOR = Path(__file__).with_name('emit_s075_s003_source_factory_projection.py')


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def pin(path):
    path = Path(path).resolve(strict=True)
    return {'path': str(path), 'sha256': sha(path)}


def verify(pins):
    def one(row):
        name, expected = row
        if not Path(name).is_file() or sha(name) != expected:
            raise ValueError('Pinned author input changed: ' + name)
    with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
        list(pool.map(one, pins.items()))


def project(runner_dir, context_path, output):
    runner_dir = Path(runner_dir).resolve(strict=True)
    context_path = Path(context_path).resolve(strict=True)
    output = Path(output).absolute()
    if not runner_dir.is_relative_to(ROOT / 'T Logs') or not context_path.is_relative_to(ROOT / 'T Logs') or not output.is_relative_to(ROOT / 'T Logs') or output.exists():
        raise ValueError('Existing pinned inputs and fresh repository T Logs output required')
    receipt_path = runner_dir / 'runner-receipt.json'
    receipt = json.loads(receipt_path.read_bytes())
    if receipt.get('kind') != 'ExperimentalSkirmishLogicalBindingAuthorRunner' or receipt.get('status') != 'PassedExperimentalSourceOnly' or receipt.get('process_exit') != 0 or any(receipt.get(k) is not False for k in ('world_allocated', 'placement_performed', 'native_observation', 'source_fit_verified', 'runtime_admission', 'mission_ready')):
        raise ValueError('Passed original source-only binding owner required')
    context_sha = sha(context_path)
    receipt_sha = sha(receipt_path)
    context = json.loads(context_path.read_bytes())
    target = context['target_context']
    if target['catalog_id'] != receipt['catalog_id'] or target['seed'] != receipt['seed'] or any(context.get(k) is not False for k in ('world_allocated', 'source_fit_certified', 'mission_ready', 'runtime_loader_admitted')):
        raise ValueError('Source-only compiler context must match authored target')
    manifest_path = Path(receipt['verified_pin_manifest']['path'])
    if sha(manifest_path) != receipt['verified_pin_manifest']['sha256']:
        raise ValueError('Original owner input manifest changed')
    pins = json.loads(manifest_path.read_bytes())
    if len(pins) != receipt['verified_pin_count'] or pins.get(str(AUTHOR)) != sha(AUTHOR) or sha(CODEC) != CODEC_SHA:
        raise ValueError('Exact original author and portable codec pins required')
    compiler = receipt['compiler']
    for name in ('dotnet', 'csc', 'response'):
        if sha(compiler[name]) != compiler[name + '_sha256']:
            raise ValueError('Original compiler input changed: ' + name)
    runtime = receipt['runtime']
    if sha(runtime['mono']) != runtime['mono_sha256'] or sha(runtime['config']) != runtime['config_sha256']:
        raise ValueError('Original runtime changed')
    raw_binding = runner_dir / 'candidate/mission-logical-binding.json'
    proof_path = runner_dir / 'candidate/candidate-proof.json'
    if sha(raw_binding) != receipt['outputs']['binding_sha256'] or sha(proof_path) != receipt['outputs']['candidate_proof_sha256']:
        raise ValueError('Original compiled binding or proof changed')
    proof = json.loads(proof_path.read_bytes())
    if (proof.get('catalog_id'), proof.get('seed'), proof.get('setup_hash_with_binding'), proof.get('logical_binding_fingerprint')) != (target['catalog_id'], target['seed'], target['setup_hash'], target['mission_logical_binding_fingerprint']):
        raise ValueError('Original binding compiler proof differs from target')
    verify(pins)
    spec = importlib.util.spec_from_file_location('source_method_extractor', EXTRACTOR)
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    method = extractor.extract_method(CODEC.read_text(), 'static JToken PortableLogicalValue(')
    text = AUTHOR.read_text()
    insertion = '    private static JObject Serialize('
    write = '            File.WriteAllText(Path.Combine(outDir, "mission-logical-binding.json"), bindingJson.ToString(Newtonsoft.Json.Formatting.Indented) + "\\n");'
    if text.count(insertion) != 1 or text.count(write) != 1:
        raise ValueError('Exact original author insertion points required')
    text = text.replace(insertion, method + '\n\n' + insertion, 1)
    text = text.replace(write, write + '\n            var portable = new JObject { ["schema_version"] = 1, ["kind"] = "DeclaredOriginalLogicalBindingFieldsWithInlineViews", ["float_encoding"] = "IEEE754_float32_hex_bits", ["unity_object_ids_exported"] = false, ["fields"] = PortableLogicalValue(roundTrippedBinding, 0) };\n            File.WriteAllText(Path.Combine(outDir, "portable-binding.json"), portable.ToString(Newtonsoft.Json.Formatting.Indented) + "\\n");', 1)
    output.mkdir(parents=True)
    generated = output / 'PortableBindingAuthor.cs'
    generated.write_text(text)
    response = [json.loads(line) for line in Path(compiler['response']).read_text().splitlines() if line.strip()]
    if response.count(str(AUTHOR)) != 1 or sum(x.startswith('-out:') for x in response) != 1:
        raise ValueError('Exact author compiler response required')
    executable = output / 'author.exe'
    response = [str(generated) if x == str(AUTHOR) else '-out:' + str(executable) if x.startswith('-out:') else x for x in response]
    rsp = output / 'compile.rsp'
    rsp.write_text(''.join(json.dumps(x) + '\n' for x in response))
    result = subprocess.run(['rtk', 'proxy', compiler['dotnet'], compiler['csc'], '@' + str(rsp)], cwd=ROOT, capture_output=True, text=True, timeout=300)
    (output / 'compile.log').write_text(result.stdout + result.stderr)
    if result.returncode != 0 or not executable.is_file():
        raise RuntimeError('Portable original author did not compile; see compile.log')
    source_request = runner_dir / 'authoring-request.json'
    source_request_sha = sha(source_request)
    request = json.loads(source_request.read_bytes())
    candidate = output / 'candidate'
    candidate.mkdir()
    request['output_directory'] = str(candidate)
    request_path = output / 'authoring-request.json'
    request_path.write_text(json.dumps(request, indent=2) + '\n')
    host = json.loads(Path(receipt['compiler_build_receipt']['path']).read_bytes())
    if sha(receipt['compiler_build_receipt']['path']) != receipt['compiler_build_receipt']['sha256']:
        raise ValueError('Host build receipt changed')
    selected = host['runtime_selection']
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join((selected['engine'], str(Path(selected['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(Path(receipt['compiler_build_receipt']['path']).parent / 'placement'), str(output)))
    run = subprocess.run(['rtk', 'proxy', runtime['mono'], '--config', runtime['config'], '--optimize=-float32', str(executable), str(request_path)], cwd=ROOT, env=env, capture_output=True, text=True, timeout=300)
    (output / 'run.log').write_text(run.stdout + run.stderr)
    if run.returncode != 0 or not (candidate / 'portable-binding.json').is_file():
        raise RuntimeError('Original portable author failed; see run.log')
    if sha(candidate / 'mission-logical-binding.json') != sha(raw_binding):
        raise ValueError('Fresh compiler owner binding differs from pinned binding')
    portable = json.loads((candidate / 'portable-binding.json').read_bytes())
    if portable.get('kind') != 'DeclaredOriginalLogicalBindingFieldsWithInlineViews' or portable.get('float_encoding') != 'IEEE754_float32_hex_bits' or portable.get('unity_object_ids_exported') is not False or portable['fields'].get('CatalogId') != target['catalog_id'] or len(portable['fields'].get('Variants', [])) != 1:
        raise ValueError('Original portable field codec target differs')
    verify(pins)
    if sha(context_path) != context_sha or sha(receipt_path) != receipt_sha or sha(source_request) != source_request_sha or sha(CODEC) != CODEC_SHA:
        raise ValueError('Author replay inputs changed during generation')
    wrapper = {'schema_version': 1, 'kind': 'OriginalCompilerDeclaredLogicalBinding', 'metadata_compiler_context': pin(context_path),
               'target_context': target, 'portable_binding': portable, 'native_capture': False, 'native_context_qualified': False,
               'runtime_admission': False, 'source_fit_certified': False,
               'mission_ready': False, 'source_owner_binding': pin(candidate / 'mission-logical-binding.json')}
    wrapper_path = output / 'logical-binding.json'
    wrapper_path.write_text(json.dumps(wrapper, indent=2, sort_keys=True) + '\n')
    report = {'kind': 'OriginalCompilerPortableLogicalBindingProjection', 'status': 'ProjectedSourceOnly',
              'target_context': target, 'logical_binding': pin(wrapper_path), 'owner_binding': pin(raw_binding),
              'author_receipt': pin(receipt_path), 'source_request': pin(source_request), 'candidate_proof': pin(proof_path),
              'codec_source': pin(CODEC), 'projector_source': pin(Path(__file__)), 'generated_author': pin(generated),
              'compile_response': pin(rsp), 'compile_log': pin(output / 'compile.log'), 'run_log': pin(output / 'run.log'),
              'world_allocated': False, 'native_capture': False, 'native_context_qualified': False, 'runtime_admission': False,
              'source_fit_certified': False, 'mission_ready': False,
              'original_reader_decode_and_compiler_roundtrip': 'pending-source-run-preflight'}
    (output / 'receipt.json').write_text(json.dumps(report, indent=2, sort_keys=True) + '\n')
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--runner-dir', required=True)
    parser.add_argument('--context', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    print(json.dumps(project(args.runner_dir, args.context, args.output)))
