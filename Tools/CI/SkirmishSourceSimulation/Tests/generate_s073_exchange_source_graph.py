#!/usr/bin/env python3
"""Read the Match scene's referenced exchange config; no world is created."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

SCENE = 'Assets/Game/Scenes/Match.unity'
CONFIG = 'Assets/Game/Configs/Scene/Game_ResourceExchange_Config.asset'
SCRIPT_META = 'Assets/Game/Scripts/Configs/ResourceExchangeConfigModels.cs.meta'
VIEW_META = 'Assets/Game/Scripts/Composition/MatchSceneView.cs.meta'
GENERATOR = 'Tools/CI/SkirmishSourceSimulation/Tests/generate_s073_exchange_source_graph.py'
PARSER = 'Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb'
PROVENANCE = 'Tools/CI/SkirmishSourceSimulation/Fixtures/S073/retained/provenance.json'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def guid(path):
    matches = re.findall(r'^guid: ([0-9a-f]{32})$', path.read_text(), re.MULTILINE)
    if len(matches) != 1:
        raise ValueError('Unique asset GUID required: ' + str(path))
    return matches[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    root = args.repo.resolve(strict=True)
    out = args.output.absolute()
    if out.exists():
        raise ValueError('Fresh output directory required')
    paths = [SCENE, CONFIG, CONFIG + '.meta', SCRIPT_META, VIEW_META, PARSER, PROVENANCE, GENERATOR]
    pins = {str(root / path): digest(root / path) for path in paths}
    if digest(Path(__file__).resolve()) != pins[str(root / GENERATOR)]:
        raise ValueError('Executing generator differs from the pinned repository source')
    provenance = json.loads((root / PROVENANCE).read_text())
    if provenance.get('readback_parser') != {'path': PARSER, 'sha256': pins[str(root / PARSER)]}:
        raise ValueError('Source parser differs from retained provenance')
    asset_guid = guid(root / (CONFIG + '.meta'))
    scene = (root / SCENE).read_text()
    scene_refs = re.findall(r'^  resourceExchangeConfig: \{fileID: (\d+), guid: ([0-9a-f]{32}), type: (\d+)\}$', scene, re.MULTILINE)
    if scene_refs != [('11400000', asset_guid, '2')]:
        raise ValueError('Unique Match scene exchange configuration binding required')
    documents = re.split(r'^--- !u!', scene, flags=re.MULTILINE)
    views = [row for row in documents if re.search(r'^  resourceExchangeConfig:', row, re.MULTILINE)]
    if len(views) != 1 or not re.search(r'^  m_Script: \{fileID: 11500000, guid: ' + guid(root / VIEW_META) + r', type: 3\}$', views[0], re.MULTILINE):
        raise ValueError('Exchange configuration must belong to the MatchSceneView component')
    process = subprocess.run(['ruby', str(root / PARSER), str(root / CONFIG)],
                             check=True, capture_output=True, text=True, timeout=30)
    source = json.loads(process.stdout)[str(root / CONFIG)]['MonoBehaviour']
    if source.get('m_Name') != 'Game_ResourceExchange_Config' or source.get('m_EditorClassIdentifier') != 'Game.Configs::Game.Configs.ResourceExchangeRecipeConfigSet' or source.get('m_Script') != {'fileID': 11500000, 'guid': guid(root / SCRIPT_META), 'type': 3}:
        raise ValueError('Scene-bound exchange config script identity changed')
    fields = {key: value for key, value in source.items() if not key.startswith('m_')}
    if not isinstance(fields.get('recipes'), list) or not isinstance(fields.get('scenarioGates'), list):
        raise ValueError('Exchange recipe and scenario gate collections required')
    if any(digest(Path(path)) != value for path, value in pins.items()):
        raise ValueError('Exchange sources changed during readback')
    out.mkdir(parents=True)
    graph = {'schema_version': 1, 'authority': 'AuthoredConfigAssets',
             'nodes': [{'asset_guid': asset_guid, 'source_type': 'ResourceExchangeRecipeConfigSet',
                        'source_path': CONFIG, 'source_sha256': pins[str(root / CONFIG)], 'fields': fields}],
             'source_pins': pins, 'scene_path': SCENE, 'compiled_setup': False,
             'world_initialized': False, 'native_world_state_used': False,
             'scope': 'Scene-bound authored source readback only; no startup or mission readiness'}
    path = out / 'source-exchange-config-graph.json'
    path.write_text(json.dumps(graph, indent=2) + '\n')
    print(json.dumps({'status': 'Passed', 'recipes': len(fields['recipes']),
                      'gates': len(fields['scenarioGates']), 'graph_sha256': digest(path)}))


if __name__ == '__main__':
    main()
