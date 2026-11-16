#!/usr/bin/env python3
"""Prepare an S005-only compiler probe from the existing authored config graph.

Only harness/catalog identity is specialized. The original gameplay compiler,
matrix, source assets, and physical Campaign map are not modified.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
GRAPH=ROOT/'T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/source-config-graph-with-cityedge-physical-inputs.json'
GRAPH_SHA256='909094a04c6226ecaa58b23934428d3cd704cc430cf5bc38bf7cf2c0a4b4ef9e'
RUNNER=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/run_s004_original_compiler.py'
BINDING=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/S004OriginalCompilerSourceBinding.cs.txt'
SCENARIO='Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset'
LAYOUT='Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishLayout_S005.asset'
MAP='Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishOperationMap_S005.asset'

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def need(value,message):
    if not value: raise ValueError(message)

def prepare(output):
    # Source pins alone do not authenticate the graph's serialized field values.
    # Admit only the retained graph whose provenance was reviewed for this probe.
    need(GRAPH.is_file() and sha(GRAPH)==GRAPH_SHA256,'Retained parent graph identity changed')
    graph=json.loads(GRAPH.read_text());pins=graph.get('source_pins')
    need(graph.get('authority')=='AuthoredConfigAssets' and graph.get('world_initialized') is False and
         graph.get('native_world_state_used') is False and graph.get('compiled_setup') is False and isinstance(pins,dict) and pins,
         'Externally authored, uncompiled source graph required')
    for path,digest in pins.items(): need(Path(path).is_file() and sha(path)==digest,'Source graph pin changed: '+path)
    def node(path,source_type):
        hits=[n for n in graph['nodes'] if n.get('source_path')==path and n.get('source_type')==source_type]
        need(len(hits)==1,'Unique actual S005 source node required: '+path)
        need(pins.get(str(ROOT/path))==hits[0]['source_sha256'],'Current S005 source pin mismatch: '+path)
        return hits[0]
    scenario=node(SCENARIO,'SkirmishScenarioDefinitionConfig')
    layout=node(LAYOUT,'SkirmishMapLayoutConfig')
    operation=node(MAP,'OperationMapDefinition')
    s=scenario['fields'];l=layout['fields'];m=operation['fields'];binding=m.get('sourceBinding',{})
    need(s.get('catalogId')=='S005' and s.get('definitionId')=='skirmish.s005' and
         s.get('scenarioSetupId')=='scenario.skirmish.s005' and s.get('operationMapId')=='opmap.skirmish.desert_base_01' and
         s.get('mapLayout',{}).get('guid')==layout['asset_guid'] and s.get('campaignMap',{}).get('guid')==operation['asset_guid'] and
         l.get('operationMapId')==s['operationMapId'] and l.get('layoutId')==s.get('mapLayoutId') and
         binding.get('sourceOperationMapId')==s['operationMapId'] and
         m.get('operationMapId')=='opmap.skirmish.desert_base_01_s005',
         'Actual S005 scenario/layout/source-binding identities differ')
    initial={str(p):sha(p) for p in (GRAPH,RUNNER,BINDING,Path(__file__).resolve())}
    runner=RUNNER.read_text();harness=BINDING.read_text()
    need('S004OriginalCompilerSourceBinding' in runner and 'S004OriginalCompilerSourceBinding' in harness and
         'SkirmishSetupCompiler.TryCompile' in harness,'Exact original compiler probe source required')
    # Specialize only the existing source-only scaffold and its explicit identity gates.
    runner=runner.replace('S004','S005').replace('s004','s005')
    harness=harness.replace('S004','S005').replace('s004','s005')
    graph['definition_path']=SCENARIO
    graph['scope']='S005 selection from retained authored graph; no Unity, world, native input, physical source fit or readiness.'
    graph['derived_source_graph']={'parent_path':str(GRAPH),'parent_sha256':initial[str(GRAPH)],
        'change':'select existing S005 definition_path; preserve source graph nodes and original pins'}
    for path,digest in initial.items(): pins[path]=digest
    output.mkdir(parents=True,exist_ok=False)
    graph_path=output/'source-config-graph.json';graph_path.write_text(json.dumps(graph,indent=2,ensure_ascii=False)+'\n')
    runner_path=output/'run_s005_original_compiler.py';runner_path.write_text(runner)
    binding_path=output/'S005OriginalCompilerSourceBinding.cs.txt';binding_path.write_text(harness)
    for path,digest in {**pins,**initial}.items(): need(Path(path).is_file() and sha(path)==digest,'Source pin changed during preparation: '+path)
    receipt={'status':'Prepared','scope':'S005SourceOnlyOriginalCompilerProbe_NoWorldOrReadiness',
      'parent_graph_sha256':initial[str(GRAPH)],'graph_sha256':sha(graph_path),
      'runner_sha256':sha(runner_path),'binding_sha256':sha(binding_path),
      'scenario_path':SCENARIO,'layout_path':LAYOUT,'operation_map_path':MAP,
      'operation_map_id':m['operationMapId'],'physical_source_id':binding['sourceOperationMapId'],
      'source_pin_count':len(pins),'compiled_setup':False,'world_initialized':False,'mission_ready':False}
    (output/'preparation-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');return receipt

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);print(json.dumps(prepare(p.parse_args().output.resolve())))
