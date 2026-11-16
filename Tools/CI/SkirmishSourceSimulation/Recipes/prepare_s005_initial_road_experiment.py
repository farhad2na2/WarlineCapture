#!/usr/bin/env python3
"""Run original fresh-road sync on S005's source grid before its actors spawn."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("groups", HERE / "prepare_s005_group_owner_experiment.py")
groups = importlib.util.module_from_spec(spec)
spec.loader.exec_module(groups)
ROOT = HERE.parents[3]

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(out):
    groups.prepare(out)
    capture = ROOT / "Logs/SkirmishProduction/s005-current-factory-capture-20261105-T2/full-factory-runway-definitions.json"
    document = json.loads(capture.read_text())
    scene = ROOT / "Assets/Game/Scenes/Match.unity"
    scene_hash = document["source_pins"]["Assets/Game/Scenes/Match.unity"]
    if sha(scene) != scene_hash: raise ValueError("Captured Match scene changed")
    helper = out / "helper.cs"
    text = helper.read_text()
    anchor = '        var registry=new Dictionary<int,RuntimeBuildingEntity>();'
    block = '''        var registry=new Dictionary<int,RuntimeBuildingEntity>();
        var sidewalkBuffer = em.AddBuffer<GridRoadSidewalk>(gridEntity);
        var dirtBuffer = em.AddBuffer<GridRoadDirt>(gridEntity);
        for (int i = 0; i < checked(grid.Width * grid.Height); i++) { sidewalkBuffer.Add(default); dirtBuffer.Add(default); }
        var retainedWorld = new AutonomousWorld(em, registry);
        var roadOutput = Environment.GetEnvironmentVariable("WARLINE_S005_ROAD_RECEIPT_DIR");
        Need(!String.IsNullOrEmpty(roadOutput) && Path.IsPathRooted(roadOutput) && Directory.Exists(roadOutput), "Owned road output directory required");
        var roadRun = new JObject { ["kind"] = "SkirmishSourceSimulationRun", ["schema_version"] = 1,
            ["repository_root"] = Path.GetFullPath("."), ["source_repository_root"] = Path.GetFullPath("."),
            ["output_directory"] = roadOutput,
            ["mission_scene"] = new JObject { ["path"] = Path.GetFullPath("Assets/Game/Scenes/Match.unity"),
                ["sha256"] = "SCENE_HASH" } };
        var roadReceipt = retainedWorld.PrepareS004InitialRoadDiagnostic(gridEntity, roadRun);
'''.replace("SCENE_HASH", scene_hash)
    text = groups.epoch.roster.base.once(text, anchor, block)
    text = groups.epoch.roster.base.once(text, 'return new JObject { ["group_owners"]=groupOwners,',
        'retainedWorld.CompleteInitialRoadInputs();\n        return new JObject { ["initial_roads"]=roadReceipt, ["group_owners"]=groupOwners,')
    helper.write_text(text)
    runner = out / "run.py"
    source = runner.read_text()
    anchor = "    b=json.loads(BUILD.read_text());args=["
    source = groups.epoch.roster.base.once(source, anchor,
        "    env['WARLINE_S005_ROAD_RECEIPT_DIR']=str(out)\n" + anchor)
    runner.write_text(source)
    prep = out / "preparation.json"
    doc = json.loads(prep.read_text())
    for path in (helper, runner, Path(__file__).resolve(), capture):
        doc["source_pins"][str(path)] = sha(path)
    doc["initial_road_scope"] = "Original fresh Match road sync on same S005 source grid before starting structures and actors"
    prep.write_text(json.dumps(doc, indent=2) + "\n")

if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("output", type=Path)
    prepare(p.parse_args().output.resolve())
