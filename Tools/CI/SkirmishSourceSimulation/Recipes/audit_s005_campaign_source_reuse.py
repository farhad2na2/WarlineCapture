#!/usr/bin/env python3
"""Audit whether retained Campaign input identities match S005's authored source.

This is a limited input identity check, not loaded-scene or mission validation.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
GRAPH = ROOT / 'T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json'
BUNDLE = ROOT / 'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/data-bundle-r4.json'
GRAPH_SHA = 'b784ffc6b36260b8c403362b93a741a06c493ebdc09704ffd828e82e9ee64138'
MAP_PATH = 'Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishOperationMap_S005.asset'


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()


def need(value, message):
    if not value: raise ValueError(message)


def audit(output):
    need(sha(GRAPH) == GRAPH_SHA, 'S005 authored graph identity changed')
    graph = json.loads(GRAPH.read_text())
    for path, digest in graph['source_pins'].items():
        need(Path(path).is_file() and sha(Path(path)) == digest, 'Graph source changed: '+path)
    maps = [n for n in graph['nodes'] if n.get('source_path') == MAP_PATH and n.get('source_type') == 'OperationMapDefinition']
    need(len(maps) == 1, 'One S005 operation map required')
    selected = maps[0]['fields']; binding = selected['sourceBinding']
    need(selected['operationMapId'] == 'opmap.skirmish.desert_base_01_s005', 'S005 logical map identity changed')
    bundle = json.loads(BUNDLE.read_text()); descriptor = bundle['campaign_physical']
    capture = Path(descriptor['path']); need(capture.is_file() and sha(capture) == descriptor['sha256'], 'Campaign capture digest changed')
    physical = json.loads(capture.read_text()); source = physical['sourceMetadata']['MonoBehaviour']
    need(physical['kind'] == 'PinnedCampaignPhysicalInputExport' and physical['result'] == 'Collected', 'Retained physical input capture required')
    need(binding['sourceOperationMapId'] == physical['operationMapId'] == source['operationMapId'] and
         binding['sourceIdentityHash'] == source['sourceIdentityHash'] and
         binding['sourceContentHash'] == source['contentHash'], 'Campaign physical source identity differs')
    actual = ROOT / physical['sourcePath']
    need(actual.is_file() and sha(actual) == physical['sourceSha256'], 'Campaign source asset changed')
    for key in ('assetGuid', 'contentHash', 'origin', 'dimensions', 'cellSize'):
        need(selected['gridMetadata'][key] == source['gridMetadata'][key], 'Grid metadata differs: '+key)
    for key in ('assetGuid', 'contentHash', 'runtimeBlobHash', 'surfaceCount'):
        need(selected['surfaceMetadata'][key] == source['surfaceMetadata'][key], 'Surface metadata differs: '+key)
    need(physical['runtimeDecodedSurfaceAssetSha256'] == selected['surfaceMetadata']['contentHash'] and
         physical['runtimeDecodedSurfaceBlobHash'] == selected['surfaceMetadata']['runtimeBlobHash'], 'Captured decoded surface differs')
    need(sha(GRAPH) == GRAPH_SHA and sha(capture) == descriptor['sha256'] and sha(actual) == physical['sourceSha256'], 'Input changed during audit')
    output.mkdir(parents=True, exist_ok=False)
    receipt = {'status':'MatchedLimitedCampaignInputIdentity', 'scope':'S005 authored binding and retained Campaign source/grid/surface metadata only',
       'graph_sha256':GRAPH_SHA, 'graph_source_pin_count':len(graph['source_pins']), 'campaign_capture_sha256':descriptor['sha256'],
       'campaign_source_asset':str(actual), 'campaign_source_sha256':physical['sourceSha256'],
       'logical_map_id':selected['operationMapId'], 'campaign_source_id':physical['operationMapId'],
       'grid_metadata_matched':True, 'surface_metadata_matched':True,
       'capture_transitive_pins_verified':False, 'loaded_scene_validated':False, 'mission_fit_validated':False,
       'world_initialized':False, 'mission_ready':False}
    (output/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
    print(json.dumps(receipt))


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__);parser.add_argument('--output', type=Path, required=True)
    audit(parser.parse_args().output.resolve())
