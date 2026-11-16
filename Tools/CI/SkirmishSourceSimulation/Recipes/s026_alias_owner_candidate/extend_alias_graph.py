#!/usr/bin/env python3
"""Add the actual S026 alias node to a pinned authored graph; no runtime admission."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[5]
GENERATOR = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_typed_config_graph.py'
ALIAS = 'Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset'
PHYSICAL = 'Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--graph', type=Path, required=True)
    p.add_argument('--graph-sha256', required=True)
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    graph_path, output = a.graph, a.output
    if not graph_path.is_absolute() or graph_path.resolve(strict=True) != graph_path or graph_path.is_symlink() or not graph_path.is_relative_to(ROOT):
        raise ValueError('Canonical graph inside source repository required')
    if not output.is_absolute() or not output.is_relative_to(ROOT) or output.exists() or output.is_symlink():
        raise ValueError('Fresh repository output required')
    if not re.fullmatch(r'[0-9a-f]{64}', a.graph_sha256) or sha(graph_path) != a.graph_sha256:
        raise ValueError('Input graph hash mismatch')
    spec = importlib.util.spec_from_file_location('s026_strict_graph', GENERATOR)
    h = importlib.util.module_from_spec(spec); spec.loader.exec_module(h)
    graph = json.loads(graph_path.read_text(), object_pairs_hook=lambda pairs: unique(pairs))
    if graph.get('schema_version') != 1 or graph.get('authority') != 'AuthoredConfigAssets' or any(graph.get(k) is not False for k in ('compiled_setup', 'world_initialized', 'native_world_state_used')):
        raise ValueError('Actual uncompiled source graph required')
    pins = graph['source_pins']; nodes = graph['nodes']
    def pin(path):
        path = Path(path)
        if not path.is_absolute() or not path.is_relative_to(ROOT) or path.is_symlink() or path.resolve(strict=True) != path or not path.is_file():
            raise ValueError('Canonical regular source required: ' + str(path))
        value = sha(path)
        if str(path) in pins and pins[str(path)] != value:
            raise ValueError('Existing source pin changed: ' + str(path))
        pins[str(path)] = value
    for path, expected in list(pins.items()):
        pin(path)
        if pins[path] != expected: raise ValueError('Input source changed: ' + path)
    asset = ROOT / ALIAS; assets = ROOT / 'Assets'
    meta, target = h.checked_meta_target(assets, Path(str(asset) + '.meta'))
    if target != asset: raise ValueError('Alias metadata target changed')
    guid = re.findall(r'^guid: ([0-9a-f]{32})\r?$', meta.read_text(), re.M)
    if len(guid) != 1 or len(h.guid_hits(assets, guid[0])) != 1 or any(n.get('asset_guid') == guid[0] or n.get('source_path') == ALIAS for n in nodes):
        raise ValueError('Unique new alias GUID required')
    reader = ROOT / h.READER_REL
    doc = h.strict_read(reader, [asset])[str(asset)]; mono = doc['MonoBehaviour']
    if mono.get('m_EditorClassIdentifier') != 'Game.Configs::Game.Configs.OperationMapDefinition':
        raise ValueError('Actual OperationMapDefinition alias required')
    script = mono.get('m_Script', {}); hits = h.guid_hits(assets, script.get('guid', ''))
    if script.get('fileID') != 11500000 or script.get('type') != 3 or len(hits) != 1:
        raise ValueError('Unique actual MonoScript required')
    script_meta, script_path = h.checked_meta_target(assets, hits[0])
    if script_path != ROOT / 'Assets/Game/Scripts/Configs/OperationMapDefinition.cs':
        raise ValueError('Alias script identity differs')
    fields = {k: v for k, v in mono.items() if not k.startswith('m_')}
    binding = fields.get('sourceBinding', {})
    if h.refs(fields, 'fields') or fields.get('operationMapId') != 'opmap.skirmish.city_crossroads' or binding.get('sourceOperationMapId') != 'opmap.skirmish.desert_base_01':
        raise ValueError('Expected alias and source binding required')
    physical = [n for n in nodes if n.get('source_path') == PHYSICAL and n.get('source_type') == 'OperationMapDefinition' and n.get('fields', {}).get('operationMapId') == binding['sourceOperationMapId'] and n.get('fields', {}).get('contentHash') == binding.get('sourceContentHash') and n.get('fields', {}).get('sourceIdentityHash') == binding.get('sourceIdentityHash')]
    if len(physical) != 1 or fields.get('sourceIdentityHash') != binding.get('sourceIdentityHash'):
        raise ValueError('Alias must join one actual pinned Campaign source')
    for path in (asset, meta, script_path, script_meta, reader, GENERATOR, Path(__file__).resolve()): pin(path)
    spellings = {k[len('MonoBehaviour.'):]: v for k, v in doc['__source_scalar_spellings'].items() if k.startswith('MonoBehaviour.')}
    if not spellings: raise ValueError('Exact serialized scalar spellings required')
    nodes.append({'asset_guid': guid[0], 'source_type': 'OperationMapDefinition', 'source_path': ALIAS, 'source_sha256': pins[str(asset)], 'fields': fields, 'scalar_spellings': spellings})
    nodes.sort(key=lambda n: n['source_path']); graph['source_pins'] = dict(sorted(pins.items()))
    if len(nodes) > 64 or sha(graph_path) != a.graph_sha256 or any(sha(Path(path)) != value for path, value in pins.items()):
        raise ValueError('Graph bounds or source drift')
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.parent.resolve(strict=True) != output.parent or output.exists() or output.is_symlink(): raise ValueError('Canonical fresh output required')
    with output.open('x') as f: json.dump(graph, f, indent=2, ensure_ascii=False); f.write('\n')
    if sha(graph_path) != a.graph_sha256 or any(sha(Path(path)) != value for path, value in pins.items()):
        output.unlink(); raise ValueError('Source drift before completion')
    print(json.dumps({'status': 'PreparedSourceOnly', 'graph_sha256': sha(output), 'alias_sha256': pins[str(asset)], 'physical_source': physical[0]['source_path'], 'runtime_admission': False, 'source_fit': False}))


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result: raise ValueError('Duplicate JSON key: ' + key)
        result[key] = value
    return result


if __name__ == '__main__': main()
