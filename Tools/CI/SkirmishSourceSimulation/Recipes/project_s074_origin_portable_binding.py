#!/usr/bin/env python3
"""Replay S074 origin source owner with original portable codec; source-only."""
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
ORIGIN = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-authoring-20261107-r1'
ORIGIN_DRAFT = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-authoring-draft-20261107-r1/binding.json'
ORIGIN_FACTORY = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-factory-export-20261107-r1'
ORIGIN_QUALIFICATION = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-runtime-inputs-20261107-r1/qualification.json'
HOST_BUILD = ROOT / 'T Logs/SkirmishProduction/s074-origin-source-build-20261113-r5'
AUTHOR_SOURCE = R9 / 'candidate/S074CameraBindingAuthor.cs'
AUTHOR_RECEIPT = R9 / 'candidate/receipt.json'
GRAPH = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json'
LAYOUT = ROOT / 'T Logs/SkirmishProduction/s074-source-origin-centered-candidate-20261107-r1/layout.json'
MATRIX = ROOT / 'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/experimental-opening-mix-matrix.csv'
CODEC_SOURCE = ROOT / 'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs'
CONTEXT = ORIGIN_FACTORY / 's074/source-factory-context.json'
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
    retained_author_receipt = json.loads(AUTHOR_RECEIPT.read_bytes())
    need(retained_author_receipt.get('status') == 'PassedSourceOwnerCompile' and
         retained_author_receipt.get('source_inputs_sha256', {}).get(str(AUTHOR_SOURCE.resolve())) == sha(AUTHOR_SOURCE),
         'Pinned source-owner author implementation required')
    host_build = json.loads((HOST_BUILD / 'build.json').read_bytes())
    need(host_build.get('status') == 'Passed' and host_build.get('compile_exit') == 0 and
         host_build.get('mission') == 'S074' and host_build.get('seed') == 104803,
         'Current passed S074 source host build required')
    host_pins = host_build.get('source_and_reference_pins')
    need(isinstance(host_pins, dict), 'Exact current S074 host build source pins required')
    for path, expected in host_pins.items():
        need(sha(path) == expected, f'Current S074 host pin drift: {path}')
    origin_author = json.loads((ORIGIN / 'authoring-result.json').read_bytes())
    origin_receipt = json.loads((ORIGIN / 'authoring-receipt.json').read_bytes())
    origin_factory_receipt = json.loads((ORIGIN_FACTORY / 'factory-context-receipt.json').read_bytes())
    qualification = json.loads(ORIGIN_QUALIFICATION.read_bytes())
    need(origin_author.get('status') == 'ConfigurationCompiled' and
         origin_author.get('catalog_id') == 'S074' and
         origin_receipt.get('mission_ready') is False and
         origin_factory_receipt.get('status') == 'PassedSourceFactoryContext' and
         qualification.get('mission_ready') is False and
         qualification.get('native_context_qualified') is False,
         'Pinned S074 origin authoring and source-only qualification required')
    need(sha(LAYOUT) == 'de320fa46ea026faf43cf325507d3f6fae70cb8a2fdb4762de7ada51ff24ae27',
         'Exact retained S074 source-origin layout required')
    matrix_rows = [row for row in __import__('csv').DictReader(MATRIX.open(newline=''))
                   if row.get('catalog_id') == 'S074' and row.get('size_id') == 'Standard']
    need(len(matrix_rows) == 1 and matrix_rows[0].get('objective_id') == 'BA' and
         matrix_rows[0].get('player_rifle') == '10' and matrix_rows[0].get('player_gunner') == '6' and
         matrix_rows[0].get('enemy_rifle') == '10' and matrix_rows[0].get('enemy_gunner') == '6',
         'Exact S074 Standard BA matrix row 10/6 required')
    context = json.loads(CONTEXT.read_bytes())
    target = context.get('target_context', {})
    need(context.get('kind') == 'OriginalCompilerDerivedNativeFactoryContext' and
         target.get('catalog_id') == 'S074' and target.get('seed') == 104803 and
         target.get('setup_hash') == 1716123768 and context.get('world_allocated') is False and
         context.get('source_fit_certified') is False and context.get('mission_ready') is False,
         'Exact source-origin S074 compiler context required')

    owner = out / 'owner'
    owner.mkdir(parents=True)
    author = owner / 'S074CameraBindingAuthor.cs'
    runner = owner / 'run.py'
    author_text = AUTHOR_SOURCE.read_text()
    codec_text = CODEC_SOURCE.read_text()
    owner_start = '            var camera = source.Cameras.ToArray().Single(x => x.CameraId == source.BattleCameraId);'
    owner_end = '            var bindingJson = Serialize(binding, variant, view, rows);'
    need(author_text.count(owner_start) == 1 and author_text.count(owner_end) == 1,
         'Exact original owner authoring section required')
    owner_start_index = author_text.index(owner_start)
    owner_end_index = author_text.index(owner_end, owner_start_index) + len(owner_end)
    origin_owner = '''            
            var camera = source.Cameras.ToArray().Single(x => x.CameraId == source.BattleCameraId);
            if (camera.CameraId != source.PlanningCameraId || camera.Orthographic)
                throw new InvalidOperationException("Expected one shared perspective planning/battle camera.");
            string focusId = inputs.ActualOpeningPlayerActorIds.First(x => x.StartsWith("role.rifle.1.", StringComparison.Ordinal));
            var retainedDraft = JObject.Parse(File.ReadAllText(args[3]));
            var retainedBinding = materializer.LogicalBinding(retainedDraft);
            var variant = retainedBinding.Variants.Single();
            var view = variant.View;
            var rows = variant.Framing;
            if (view == null || rows == null || rows.Length != inputs.RequiredFramingRoles.Length)
                throw new InvalidOperationException("Retained origin binding does not cover exact compiler roles.");
            var binding = ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();
            binding.SchemaVersion = 1;
            binding.ContentVersion = 1;
            binding.DefinitionVersion = definition.ContentVersion;
            binding.ValueFormat = inputs.ValueFormat;
            binding.CatalogId = definition.CatalogId;
            binding.DefinitionId = definition.DefinitionId;
            binding.SourceOperationMapId = source.OperationMapId;
            binding.SourceSceneGuid = source.SourceSceneReference.AssetGUID;
            binding.SourceContentHash = source.ContentHash;
            binding.SourceSemanticFingerprint = inputs.SourceSemanticFingerprint;
            binding.LayoutId = definition.MapLayout.LayoutId;
            binding.ReviewedLayoutSemanticFingerprint = inputs.LayoutSemanticFingerprint;
            binding.ObjectiveKind = setup.ObjectiveKind;
            binding.Variants = new[] { variant };
            var bindingJson = Serialize(binding, variant, view, rows);'''
    author_text = author_text[:owner_start_index] + origin_owner + author_text[owner_end_index:]
    author_text = author_text.replace('if (args.Length != 4)', 'if (args.Length != 5)', 1)
    author_text = author_text.replace('var outDir = Path.GetFullPath(args[3]);',
                                      'var outDir = Path.GetFullPath(args[4]);', 1)
    bound_anchor = '            if (!SkirmishMissionLogicalBindingCompiler.TryCompile(definition, bound, out var compiledBinding, out var bindingError))'
    need(author_text.count(bound_anchor) == 1, 'Exact bound original compiler gate required')
    author_text = author_text.replace(bound_anchor,
        '            if (bound.SetupHash != 1716123768) throw new InvalidOperationException("Origin bound setup hash changed: " + bound.SetupHash);\n' + bound_anchor, 1)
    # The original outer exporter method intentionally enforces S026's
    # twelve-variant domain. Reuse its exact lower-level field codec instead;
    # the S074 source owner has one compiler-selected variant.
    codec_methods = extract_method(codec_text, 'static JToken PortableLogicalValue(')
    marker = '    private static JObject Serialize('
    need(author_text.count(marker) == 1, 'Exact source-owner method insertion point required')
    author_text = author_text.replace(marker, codec_methods + '\n\n' + marker, 1)
    run_anchor = '            var outDir = Path.GetFullPath(args[4]);'
    need(author_text.count(run_anchor) == 1, 'Exact actual-binding export anchor required')
    author_text = author_text.replace(run_anchor,
        '            var portableBinding = new JObject { ["schema_version"] = 1, ["kind"] = "DeclaredOriginalLogicalBindingFieldsWithInlineViews", ["float_encoding"] = "IEEE754_float32_hex_bits", ["unity_object_ids_exported"] = false, ["fields"] = PortableLogicalValue(roundTrippedBinding, 0) };\n' +
        '            var portableDecoded = SourceNativeMissionLogicalBindingReference.DecodeDeclaredFields(portableBinding["fields"]);\n' +
        '            definition.BindMissionLogicalBinding(portableDecoded);\n' +
        '            if (!SkirmishSetupCompiler.TryCompile(definition, bound.DifficultyId, bound.SizeId, bound.Seed, manifest, matrix, out var portableBound, out var portableReasons)) throw new InvalidOperationException("Original reader-decoded portable binding rejected: " + string.Join(";", portableReasons.Select(x => x.Code + ":" + x.Field + ":" + x.Detail)));\n' +
        '            if (portableBound.SetupHash != 1716123768) throw new InvalidOperationException("Original portable reader changed S074 setup hash: " + portableBound.SetupHash);\n' +
        '            if (!SkirmishMissionLogicalBindingCompiler.TryCompile(definition, portableBound, out var portableCompiled, out var portableError) || portableCompiled.Fingerprint != compiledBinding.Fingerprint) throw new InvalidOperationException("Original portable reader/compiler roundtrip changed S074 binding: " + portableError);\n' + run_anchor, 1)
    author_text = author_text.replace('["evidence_requirements_only"] = true, ["native_pngs_observed"] = false,',
        '["original_reader_decode_and_compiler_roundtrip"] = true, ["portable_roundtrip_setup_hash"] = 1716123768,\n                ["evidence_requirements_only"] = true, ["native_pngs_observed"] = false,', 1)
    author_text = author_text.replace('"S074CameraSeededLogicalBindingCandidate"', '"S074SourceOriginLogicalBindingReplay"', 1)
    author_text = author_text.replace('"CompiledSourceOnlyCandidate"', '"CompiledSourceOnlyOriginBinding"', 1)
    author_text = author_text.replace('"AuthoredCandidate_PresentationTargets_NotObserved"',
                                      '"RetainedOriginDraft_PresentationTargets_NotObserved"', 1)
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
                          str(GRAPH), str(LAYOUT), str(MATRIX), str(ORIGIN_DRAFT), str(owner)],
                         cwd=ROOT, env=env, capture_output=True, text=True, timeout=600)
    (owner / 'run.stdout.log').write_text(run.stdout)
    (owner / 'run.stderr.log').write_text(run.stderr)
    need(run.returncode == 0 and PASS in run.stdout,
         f'Actual S074 compiler author replay failed (exit={run.returncode}); preserve logs')
    proof = json.loads((owner / 'candidate-proof.json').read_bytes())
    need(proof.get('setup_hash_with_binding') == 1716123768 and
         proof.get('original_reader_decode_and_compiler_roundtrip') is True and
         proof.get('portable_roundtrip_setup_hash') == 1716123768 and
         proof.get('world_allocated') is False and proof.get('mission_ready') is False,
         'Exact source-only S074 origin reader/compiler roundtrip proof required')
    for path, expected in host_pins.items():
        need(sha(path) == expected, f'Current S074 host pin drift during owner replay: {path}')

    owner_binding = owner / 'mission-logical-binding.json'
    portable_path = owner / 'portable-binding.json'
    actual_raw = json.loads(owner_binding.read_bytes())
    portable = json.loads(portable_path.read_bytes())
    previous_raw = ORIGIN_DRAFT
    def semantic(token):
        if isinstance(token, dict):
            return {key: semantic(value) for key, value in token.items()
                    if key not in ('name', 'hideFlags')}
        if isinstance(token, list):
            return [semantic(value) for value in token]
        if isinstance(token, float):
            import struct
            return struct.unpack('!f', struct.pack('!f', token))[0]
        return token
    actual_semantic = semantic(actual_raw.get('Variants'))
    expected_semantic = semantic(json.loads(previous_raw.read_bytes()).get('Variants'))
    need(actual_semantic == expected_semantic,
         'Fresh compiler-author binding differs semantically from retained S074 origin binding')
    need(actual_raw.get('CatalogId') == 'S074' and
         actual_raw.get('SourceOperationMapId') == target.get('operation_map_id') and
         actual_raw.get('SourceContentHash') == target.get('source_content_hash') and
         actual_raw.get('LayoutId') == origin_author.get('layout_id'),
         'Materialized origin binding compiler identity differs from exact S074 source context')
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
        'world_allocated': False,
        'native_capture': False,
        'native_context_qualified': False,
        'runtime_admission': False,
        'source_fit_certified': False,
        'mission_ready': False,
        'source_owner_binding': {'path': str(owner_binding.resolve()), 'sha256': sha(owner_binding)},
    }
    wrapper_path = out / 'logical-binding.json'
    wrapper_path.write_text(json.dumps(wrapper, indent=2, sort_keys=True) + '\n')
    owner_receipt = {
        'kind': 'S074OriginCompilerAuthoredLogicalBindingReplay',
        'status': 'PassedSourceOwnerCompile',
        'compile_exit': compiled.returncode,
        'owner_exit': run.returncode,
        'compiler': 'Current SkirmishSetupCompiler + SkirmishMissionLogicalBindingCompiler',
        'host_build': {'path': str((HOST_BUILD / 'build.json').resolve()), 'sha256': sha(HOST_BUILD / 'build.json')},
        'source_inputs_sha256': host_pins | {str(CODEC_SOURCE.resolve()): sha(CODEC_SOURCE)},
        'source_owner_author': {'path': str(AUTHOR_SOURCE.resolve()), 'sha256': sha(AUTHOR_SOURCE),
                                'retained_receipt': str(AUTHOR_RECEIPT.resolve()),
                                'retained_receipt_sha256': sha(AUTHOR_RECEIPT)},
        'origin_inputs_sha256': {str(path.resolve()): sha(path) for path in (LAYOUT, MATRIX, ORIGIN / 'authoring-request.json', ORIGIN / 'authoring-result.json', ORIGIN / 'authoring-receipt.json', ORIGIN_DRAFT, CONTEXT, ORIGIN_QUALIFICATION)},
        'binding_sha256': sha(owner_binding),
        'binding_semantic_equal_to_origin': True,
        'origin_binding_sha256': sha(previous_raw),
        'portable_binding_sha256': sha(portable_path),
        'world_allocated': False, 'runtime_admission': False,
        'native_context_qualified': False, 'native_capture': False,
        'source_fit_certified': False, 'mission_ready': False,
    }
    owner_receipt_path = owner / 'receipt.json'
    owner_receipt_path.write_text(json.dumps(owner_receipt, indent=2, sort_keys=True) + '\n')
    receipt = {
        'kind': 'S074OriginCompilerDeclaredLogicalBindingPortableProjection',
        'status': 'ProjectedSourceOnly',
        'source_owner_replay': {'path': str(owner_receipt_path.resolve()), 'sha256': sha(owner_receipt_path)},
        'source_owner_binding': wrapper['source_owner_binding'],
        'original_portable_codec_source': {'path': str(CODEC_SOURCE.resolve()), 'sha256': sha(CODEC_SOURCE)},
        'compiler_context': context_pin,
        'logical_binding': {'path': str(wrapper_path.resolve()), 'sha256': sha(wrapper_path)},
        'world_allocated': False, 'native_capture': False,
        'native_context_qualified': False, 'runtime_admission': False,
        'source_fit_certified': False, 'mission_ready': False,
        'original_reader_decode_and_compiler_roundtrip': 'PassedSourceOnly',
        'binding_semantic_equal_to_origin': True,
        'origin_binding': {'path': str(previous_raw.resolve()), 'sha256': sha(previous_raw)},
    }
    receipt_path = out / 'receipt.json'
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + '\n')
    print(json.dumps({'status': receipt['status'], 'logical_binding': str(wrapper_path),
                      'sha256': sha(wrapper_path), 'compiler_roundtrip': receipt['original_reader_decode_and_compiler_roundtrip']}))


if __name__ == '__main__':
    main()
