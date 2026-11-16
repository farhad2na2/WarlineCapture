#!/usr/bin/env python3
"""Audit authored S004 placement inputs and the live managed resolution boundary.

This does not resolve a loaded scene, execute the owner, or admit gameplay.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
FILES = {
    "s004_map": "Assets/Game/Resources/SkirmishS004OperationMap.asset",
    "candidate_map": "Assets/Game/GeneratedOperationMaps/Variants/CityEdgeAirfield/Candidate/Definition.asset",
    "environment": "Assets/Game/Scripts/Runtime/Skirmish/SkirmishS004Environment.cs",
    "scene_view": "Assets/Game/Scripts/Composition/MatchSceneView.cs",
    "bootstrap": "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs",
    "building_owner": "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs",
}


def audit(text):
    for name in ("s004_map", "candidate_map"):
        matches = re.findall(r"^  additionalBuildingPlacements: (.+)$", text[name], re.MULTILINE)
        if matches != ["{fileID: 0}"]:
            raise ValueError("Expected exact authored null additional-building field: " + name)
    anchors = {
        "environment": ["public static MapBuildingPlacementConfig LoadForSession(EntityManager em) => null;",
                        "public static bool UsesCampaignMap(EntityManager em) => LoadMapForSession(em) != null;"],
        "scene_view": ["activeOperationMapSceneView.BuildingPlacements : mapBuildingPlacementConfig",
                       "resolvedOperationMapDefinition?.AdditionalBuildingPlacements",
                       "SkirmishS004Environment.LoadForSession(world.EntityManager)",
                       "neutralizeSource = SkirmishS004Environment.UsesCampaignMap(world.EntityManager);",
                       "activeOperationMapSceneView?.VehiclePlacements"],
        "bootstrap": ["() => MapVehiclePlacementConfig,", "() => MapVehicleAuthoringRoot,",
                      "() => MapBuildingPlacementConfig);"],
        "building_owner": ["resolveMapBuildingPlacementConfig?.Invoke() ?? mapBuildingPlacementConfig;",
                           "resolveMapVehiclePlacementConfig?.Invoke() ?? mapVehiclePlacementConfig;",
                           "resolveMapVehicleAuthoringRoot?.Invoke() ?? mapVehicleAuthoringRoot;"],
    }
    for name, values in anchors.items():
        for value in values:
            if text[name].count(value) != 1:
                raise ValueError("Managed placement boundary changed: " + name + " / " + value)
    return {"s004_additional_building_placements": "authored_null",
            "candidate_additional_building_placements": "authored_null",
            "s004_environment_additions": "source_method_returns_null",
            "building_and_vehicle_config_resolution": "live_scene_view_and_session_dependent",
            "runtime_placement_owners_executed": False, "live_loaded_scene_resolved": False,
            "building_startup_complete": "unknown", "runtime_admission": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser(); parser.add_argument("--output", required=True); args = parser.parse_args()
    paths = {key: ROOT / value for key, value in FILES.items()}
    before = {key: path.read_bytes() for key, path in paths.items()}
    facts = audit({key: value.decode("utf-8") for key, value in before.items()})
    if any(path.read_bytes() != before[key] for key, path in paths.items()):
        raise ValueError("Source changed during managed placement audit")
    doc = {"status": "PassedSourceAndSerializedInputAudit", "facts": facts,
           "pins": {str(paths[k]): hashlib.sha256(v).hexdigest() for k, v in before.items()}}
    with open(args.output, "x") as out:
        json.dump(doc, out, indent=2); out.write("\n")
    print("[S004ManagedPlacementResolution] result=Passed liveResolution=0 admission=0 readiness=0")


if __name__ == "__main__":
    main()
