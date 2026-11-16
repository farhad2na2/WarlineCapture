#!/usr/bin/env python3
"""Audit serialized Match day/night provenance; never attest live owner state."""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
FILES = {
    "scene": "Assets/Game/Scenes/Match.unity",
    "scene_view_meta": "Assets/Game/Scripts/Composition/MatchSceneView.cs.meta",
    "config": "Assets/Game/Configs/Scene/Game_DayNight_Config.asset",
    "config_meta": "Assets/Game/Configs/Scene/Game_DayNight_Config.asset.meta",
    "config_script": "Assets/Game/Scripts/Configs/Scene/DayNightSystemSceneConfigAsset.cs",
    "config_script_meta": "Assets/Game/Scripts/Configs/Scene/DayNightSystemSceneConfigAsset.cs.meta",
    "model": "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
    "day_owner": "Assets/Game/Scripts/Environment/DayNightSystem.cs",
    "managed_owner": "Assets/Game/Scripts/Systems/ManagedGameplayStartupSystemHelper.cs",
    "bootstrap": "Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs",
    "resource_owner": "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs",
    "building_owner": "Assets/Game/Scripts/Systems/BuildingGameplayCompositionSystemHelper.cs",
}

def once(pattern, text, label):
    rows = re.findall(pattern, text, re.MULTILINE)
    if len(rows) != 1: raise ValueError("Expected one " + label)
    return rows[0]

def audit(text):
    def guid(name): return once(r"^guid: ([0-9a-f]{32})$", text[name], name + " GUID")
    blocks = re.split(r"(?=^--- !u!)", text["scene"], flags=re.MULTILINE)
    view_guid=guid("scene_view_meta")
    views=[b for b in blocks if re.search(r"^  m_Script: \{fileID: 11500000, guid: " + view_guid + r", type: 3\}$",b,re.MULTILINE)]
    if len(views)!=1: raise ValueError("Expected one serialized MatchSceneView")
    config_guid=guid("config_meta")
    target=once(r"^  dayNightConfig: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}$",views[0],"scene day-night reference")
    if target!=config_guid: raise ValueError("Scene day-night reference changed")
    script=once(r"^  m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}$",text["config"],"config script")
    if script!=guid("config_script_meta") or "class DayNightSystemSceneConfigAsset : DayNightSystemConfig" not in text["config_script"]: raise ValueError("Day-night config type changed")
    minutes=float(once(r"^  fullDayDurationMinutes: ([^\s]+)$",text["config"],"day duration"))
    if not math.isfinite(minutes) or minutes<=0: raise ValueError("Finite positive day duration required")
    requirements={"model":["public float FullDayDurationMinutes => fullDayDurationMinutes;"],"day_owner":["fullDayDurationMinutes = config.FullDayDurationMinutes;"],"managed_owner":["dayNight?.Init(dayNightConfig, directionalLight, globalVolume);"],"bootstrap":["DayNightConfig,", "BindDayNightSystem(managedSystems.DayNight);"],"resource_owner":["context.DayNightSystem.FullDayDurationMinutes * 60f", "typeof(Game.Components.SkirmishMatchState)", "preset.supplyDaySeconds"],"building_owner":["private const float OilBarrelsPerFuelBarrel = 2f;"]}
    for name, anchors in requirements.items():
        for anchor in anchors:
            if text[name].count(anchor)!=1: raise ValueError("Source owner boundary changed: " + name + " / " + anchor)
    return {"scene_view_script_guid":view_guid,"config_guid":config_guid,"authored_minutes":minutes,"conditional_seconds_per_day":max(1.0,minutes*60.0),"oil_barrels_per_fuel_barrel":2.0,"conditions":["the serialized Match scene view is used", "its DayNight system is created and initialized with this config", "no legacy SkirmishMatchState is present"],"managed_owner_initialized":False,"runtime_cadence_admitted":False,"physical_source_validated":False,"mission_ready":False}

def main():
    parser=argparse.ArgumentParser();parser.add_argument("--output",required=True);args=parser.parse_args()
    paths={k:ROOT/v for k,v in FILES.items()};before={k:p.read_bytes() for k,p in paths.items()}
    result=audit({k:v.decode("utf-8") for k,v in before.items()})
    if any(p.read_bytes()!=before[k] for k,p in paths.items()): raise ValueError("Source changed during audit")
    result.update({"status":"PassedConditionalSerializedSourceAudit","pins":{str(paths[k]):hashlib.sha256(v).hexdigest() for k,v in before.items()}})
    with open(args.output,"x") as out: json.dump(result,out,indent=2);out.write("\n")
    print("[S004ResourceCadence] result=Passed scope=conditional_serialized_source runtimeAdmission=0")
if __name__=="__main__": main()
