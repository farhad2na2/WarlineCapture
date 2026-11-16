#!/usr/bin/env python3
"""Expose an already-captured canonical catalog without changing its graph node."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError('Duplicate JSON key: ' + key)
        result[key] = value
    return result


def regular_in_repo(path, repo):
    if not path.is_absolute() or path.is_symlink() or path.resolve(strict=True) != path or not path.is_file():
        raise ValueError('Canonical regular file required: ' + str(path))
    if not path.is_relative_to(repo):
        raise ValueError('File outside repository: ' + str(path))
    return path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--graph', type=Path, required=True)
    parser.add_argument('--graph-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--repo', type=Path, required=True)
    args = parser.parse_args()
    repo = args.repo.resolve(strict=True)
    source = regular_in_repo(args.graph, repo)
    output = args.output
    if not output.is_absolute() or output.is_symlink() or output.exists() or not output.is_relative_to(repo):
        raise ValueError('Existing output or non-regular source graph')
    source_sha = digest(source)
    if not re.fullmatch(r'[0-9a-f]{64}', args.graph_sha256) or source_sha != args.graph_sha256:
        raise ValueError('Pinned graph hash mismatch')
    graph = json.loads(source.read_text(), object_pairs_hook=unique,
                       parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Nonfinite JSON: ' + value)))
    if graph.get('schema_version') != 1 or not isinstance(graph.get('nodes'), list) or not isinstance(graph.get('source_pins'), dict) or graph.get('authority') != 'AuthoredConfigAssets' or any(
        graph.get(k) is not False for k in ('compiled_setup', 'world_initialized', 'native_world_state_used')
    ) or 'battle_catalog_path' in graph:
        raise ValueError('Unexpected source graph authority or existing catalog path')
    relative = 'Assets/Game/Resources/SkirmishBattleCatalog.asset'
    asset = (repo / relative).resolve(strict=True)
    nodes = [n for n in graph['nodes'] if n.get('source_path') == relative]
    if len(nodes) != 1 or nodes[0].get('source_type') != 'SkirmishBattleCatalogConfig':
        raise ValueError('Unique captured canonical battle catalog required')
    pins = graph['source_pins']
    if pins.get(str(asset)) != digest(asset) or nodes[0].get('source_sha256') != pins[str(asset)]:
        raise ValueError('Catalog node, source pin and current asset differ')
    for path, sha in pins.items():
        if not isinstance(sha, str) or not re.fullmatch(r'[0-9a-f]{64}', sha) or digest(regular_in_repo(Path(path), repo)) != sha:
            raise ValueError('Graph source changed: ' + path)
    graph['battle_catalog_path'] = relative
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.parent.resolve(strict=True) != output.parent or output.is_symlink() or output.exists():
        raise ValueError('Canonical fresh output required')
    output.write_text(json.dumps(graph, indent=2, ensure_ascii=False) + '\n')
    if digest(source) != source_sha or any(digest(Path(p)) != s for p, s in pins.items()):
        output.unlink()
        raise ValueError('Graph or source changed during catalog declaration')
    print(json.dumps({'status': 'Passed', 'source_graph_sha256': source_sha,
                      'output_sha256': digest(output), 'catalog_sha256': pins[str(asset)],
                      'scope': 'Existing typed catalog reference only; no world or source fit'}))


if __name__ == '__main__':
    main()
