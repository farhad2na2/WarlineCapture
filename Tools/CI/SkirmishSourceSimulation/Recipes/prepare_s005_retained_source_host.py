#!/usr/bin/env python3
"""Attach the real S005 diagnostic startup to the shared same-world host."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
spec = importlib.util.spec_from_file_location("start", HERE / "prepare_s005_source_start_experiment.py")
start = importlib.util.module_from_spec(spec)
spec.loader.exec_module(start)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(out):
    start.prepare(out)
    once = start.roads.groups.epoch.roster.base.once
    session = out / "session.cs"
    session.write_text(once(session.read_text(), "S005StartingGrantExperiment.Run(em, entity, setup, definition.CampaignMap)",
                            "S005StartingGrantExperiment.Run(em, entity, setup, definition)"))
    helper = out / "helper.cs"
    source = helper.read_text()
    source = once(source, "internal static JObject Run(EntityManager em,Entity session,SkirmishResolvedSetup setup,OperationMapDefinition actualMap)",
                  "internal static JObject Run(EntityManager em,Entity session,SkirmishResolvedSetup setup,SkirmishScenarioDefinitionConfig scenarioDefinition)")
    source = once(source, '        Need(setup.CatalogId=="S005"',
                  '        var actualMap = scenarioDefinition.CampaignMap;\n        Need(setup.CatalogId=="S005"')
    anchor = '        var forceRows=new JArray();var counts=new Dictionary<string,int>(StringComparer.Ordinal);var stableIds=new HashSet<string>(StringComparer.Ordinal);'
    block = '''        JObject sourceHost = new JObject { ["attached"] = false };
        if ((bool?)sourceActivation["activated"] == true)
        {
            using var maps=em.CreateEntityQuery(typeof(OperationMapMetadataComponent),typeof(ActiveOperationMapComponent));
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));
            Need(maps.CalculateEntityCount()==1 && surfaces.CalculateEntityCount()==1,"Actual unique map and surface required");
            var root=maps.GetSingletonEntity();var surface=surfaces.GetSingletonEntity();
            bool missingMapRejected=false;
            try { retainedWorld.AttachExpandedSourceStartup(gridEntity,surface,session,boundary,Entity.Null,scenarioDefinition,setup,pipeline,definitions); }
            catch (InvalidOperationException) { missingMapRejected=true; }
            Need(missingMapRejected,"Missing map must fail closed");
            retainedWorld.AttachExpandedSourceStartup(gridEntity,surface,session,boundary,root,scenarioDefinition,setup,pipeline,definitions);
            bool duplicateRejected=false;
            try { retainedWorld.AttachExpandedSourceStartup(gridEntity,surface,session,boundary,root,scenarioDefinition,setup,pipeline,definitions); }
            catch (InvalidOperationException) { duplicateRejected=true; }
            Need(duplicateRejected,"Duplicate world attach must fail closed");
            sourceHost=new JObject { ["attached"]=true,["same_manager"]=Object.ReferenceEquals(retainedWorld.Entities,em),
                ["same_registry"]=Object.ReferenceEquals(retainedWorld.Buildings,registry),["stage"]=retainedWorld.InitializationStage,
                ["missing_map_rejected"]=missingMapRejected,["duplicate_attach_rejected"]=duplicateRejected,
                ["scheduler_admitted"]=false,["physical_scene_validated"]=false };
        }
''' + anchor
    source = once(source, anchor, block)
    source = once(source, 'return new JObject { ["source_activation"]=sourceActivation,',
                  'return new JObject { ["source_host"]=sourceHost, ["source_activation"]=sourceActivation,')
    helper.write_text(source)
    adapter = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/AutonomousWorld.SourceStartup.cs"
    rsp=out / "compile.rsp"
    lines=[json.loads(line) for line in rsp.read_text().splitlines() if line.strip()]
    if str(adapter) in lines: raise ValueError("duplicate shared startup adapter")
    lines.append(str(adapter))
    rsp.write_text("".join(json.dumps(line)+"\n" for line in lines))
    prep=out / "preparation.json"
    doc=json.loads(prep.read_text())
    for path in (helper,session,rsp,adapter,Path(__file__).resolve()): doc["source_pins"][str(path)]=sha(path)
    doc["retained_host_scope"]="Shared source host attachment only; no scheduler or native readiness"
    prep.write_text(json.dumps(doc,indent=2)+"\n")

if __name__ == "__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())
