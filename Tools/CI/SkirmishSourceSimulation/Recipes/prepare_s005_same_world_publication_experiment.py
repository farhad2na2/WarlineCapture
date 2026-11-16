#!/usr/bin/env python3
"""Prepare S005's original metadata publisher inside the startup diagnostic.

The resulting source is experimental: source scene, native UI, water and
mission readiness are not established by metadata publication.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
SESSION = ROOT / "T Logs/SkirmishProduction/s005-terrain-placement-inputs-20261105-T5/session.cs"
ROTATION = ROOT / "T Logs/SkirmishProduction/s005-rotation-adapter-experiment-20261105-T1/SourceOperationMapRotationReference.cs"
REFERENCE = ROOT / "T Logs/SkirmishProduction/s005-rotation-capture-20261105-T1/rotation-reference.json"
RSP = ROOT / "T Logs/SkirmishProduction/s005-source-launch-startup-20261105-T1/compile.rsp"
RECIPE = Path(__file__).resolve()

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def replace_once(text, old, new):
    if text.count(old) != 1:
        raise ValueError("source anchor missing or ambiguous: " + old[:100])
    return text.replace(old, new, 1)

def prepare(out):
    out.mkdir(parents=True, exist_ok=False)
    if sha(REFERENCE) != "e4effc83bed0987eef73fd30015b2555049a4eccbd4ea07273bec086924e8a2f":
        raise ValueError("S005 exact reference changed")
    session = SESSION.read_text()
    anchor = "        JObject grantProbe = setup.SizeId == SkirmishSizeId.Standard ? S005StartingGrantExperiment.Run(em, entity, setup) : null;"
    insertion = '''        Game.Composition.OperationMapRuntimeBootstrapSceneSystemHelper publisher = null;
        Unity.Entities.Entity mapRoot = Unity.Entities.Entity.Null;
        JObject publication = null;
        if (setup.SizeId == SkirmishSizeId.Standard)
        {
            var rotation = SourceOperationMapRotationReference.LoadPinned(
                Path.GetFullPath("T Logs/SkirmishProduction/s005-rotation-capture-20261105-T1/rotation-reference.json"),
                "e4effc83bed0987eef73fd30015b2555049a4eccbd4ea07273bec086924e8a2f", Path.GetFullPath("."));
            publisher = new Game.Composition.OperationMapRuntimeBootstrapSceneSystemHelper(em, rotation);
            var scenarioId = new Unity.Collections.FixedString64Bytes(setup.CatalogId);
            var missionId = new Unity.Collections.FixedString64Bytes(setup.DefinitionId);
            Game.Composition.SourceOperationMapInitialReadiness.ResolveInitialOperationMapReadiness(false, out var ready, out var required);
            if (!publisher.TryPublish(definition.CampaignMap, in scenarioId, in missionId, 1, ready, required, out mapRoot, out string error))
                throw new InvalidDataException("Original S005 map publisher rejected: " + error);
            if (!publisher.TryGetPublishedRoot(em.World, out var retained) || retained != mapRoot)
                throw new InvalidDataException("Original S005 map publisher did not retain its root");
            var active = em.GetComponentData<Game.Components.ActiveOperationMapComponent>(mapRoot);
            var metadata = em.GetComponentData<Game.Components.OperationMapMetadataComponent>(mapRoot);
            if (active.OperationMapId.ToString() != definition.CampaignMap.OperationMapId ||
                setup.OperationMapId != definition.CampaignMap.SourceBinding.SourceOperationMapId || active.ScenarioId.ToString() != setup.CatalogId ||
                active.MissionId.ToString() != setup.DefinitionId || !metadata.Blob.IsCreated || metadata.PhysicalSourceValidated != 0 ||
                metadata.Blob.Value.SourceOperationMapId.ToString() != definition.CampaignMap.SourceBinding.SourceOperationMapId ||
                metadata.Blob.Value.Anchors.Length != 1 || metadata.Blob.Value.Cameras.Length != 2)
                throw new InvalidDataException("Original S005 map publication identity mismatch: active=" + active.OperationMapId +
                    " setup=" + setup.OperationMapId + " authored=" + definition.CampaignMap.OperationMapId +
                    " scenario=" + active.ScenarioId + " expectedScenario=" + setup.CatalogId +
                    " mission=" + active.MissionId + " expectedMission=" + setup.DefinitionId +
                    " blob=" + metadata.Blob.IsCreated + " physical=" + metadata.PhysicalSourceValidated +
                    " source=" + (metadata.Blob.IsCreated ? metadata.Blob.Value.SourceOperationMapId.ToString() : "<absent>") +
                    " expectedSource=" + definition.CampaignMap.SourceBinding.SourceOperationMapId +
                    " anchors=" + (metadata.Blob.IsCreated ? metadata.Blob.Value.Anchors.Length : -1) +
                    " cameras=" + (metadata.Blob.IsCreated ? metadata.Blob.Value.Cameras.Length : -1));
            publication = new JObject { ["owner"]="OperationMapRuntimeBootstrapSceneSystemHelper.TryPublish",
                ["operation_map_id"]=active.OperationMapId.ToString(), ["scenario_id"]=active.ScenarioId.ToString(),
                ["mission_id"]=active.MissionId.ToString(), ["anchors"]=metadata.Blob.Value.Anchors.Length,
                ["cameras"]=metadata.Blob.Value.Cameras.Length, ["physical_source_validated"]=false };
        }
''' + anchor + '''
        if (publisher != null)
        {
            if (!publisher.TryGetPublishedRoot(em.World, out var retained) || retained != mapRoot ||
                !em.Exists(mapRoot) || em.GetComponentData<Game.Components.OperationMapMetadataComponent>(mapRoot).PhysicalSourceValidated != 0)
                throw new InvalidDataException("Original S005 map root not retained throughout startup");
            publication["retained_after_startup"] = true;
            publication["same_manager_as_startup"] = true;
            publisher.Dispose();
        }
'''
    session = replace_once(session, anchor, insertion)
    session = replace_once(session, '["starting_buildings"] = "InitiallyWaitedForBoundaryAndGrid", ["starting_grant_probe"] = grantProbe,',
                           '["starting_buildings"] = "InitiallyWaitedForBoundaryAndGrid", ["starting_grant_probe"] = grantProbe, ["map_publication"] = publication,')
    (out / "session.cs").write_text(session)
    args = [json.loads(line) for line in RSP.read_text().splitlines() if line.strip()]
    substitutions = {
        str(SESSION): str(out / "session.cs"),
        str(ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceOperationMapRotationReference.cs"): str(ROTATION),
    }
    for old, new in substitutions.items():
        if args.count(old) != 1:
            raise ValueError("compile source not unique: " + old)
        args[args.index(old)] = new
    args = ["-out:" + str(out / "source-force-ledger.exe") if a.startswith("-out:") else a for a in args]
    (out / "compile.rsp").write_text("".join(json.dumps(a) + "\n" for a in args))
    pins = {str(p): sha(p) for p in (SESSION, ROTATION, REFERENCE, RSP, RECIPE)}
    (out / "preparation.json").write_text(json.dumps({"status":"PreparedExperimentalOnly", "source_pins":pins,
        "mission_ready":False, "native_scene_validated":False}, indent=2) + "\n")

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("output", type=Path)
    prepare(parser.parse_args().output.resolve())
