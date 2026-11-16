#!/usr/bin/env python3
"""Audit serialized S004 loading inputs; never assert live managed-owner readiness."""
import hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[5]
FILES={"map": "Assets/Game/Resources/SkirmishS004OperationMap.asset", "match":"Assets/Game/Scenes/Match.unity", "scene":"Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/RuntimeBinding.unity", "scene_meta":"Assets/Game/Scenes/OperationMaps/Variants/CityEdgeAirfield/RuntimeBinding.unity.meta", "city":"Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset", "city_meta":"Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset.meta"}
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def one(text,pattern,label):
 rows=re.findall(pattern,text,re.MULTILINE)
 if len(rows)!=1:raise ValueError("Unique serialized "+label+" required")
 return rows[0]
def audit(values):
 scene_guid=one(values["scene_meta"],r"^guid: ([0-9a-f]{32})$","scene GUID")
 map_guid=one(values["map"],r"(?ms)^  sourceSceneReference:\n    m_AssetGUID: ([0-9a-f]{32})$","map source scene GUID")
 if scene_guid!=map_guid or scene_guid!="915718fcf0cd7422db987b388cd0d925":raise ValueError("Selected scene GUID differs")
 for label,key in (("match building", "mapBuildingPlacementConfig"),("match vehicle","mapVehiclePlacementConfig")):
  if one(values["match"],r"^  "+key+r": (.+)$",label)!="{fileID: 0}":raise ValueError(label+" is not authored null")
 for label,key in (("scene building","buildingPlacements"),("scene vehicle","vehiclePlacements")):
  if one(values["scene"],r"^  "+key+r": (.+)$",label)!="{fileID: 0}":raise ValueError(label+" is not authored null")
 for label,key in (("map building","buildingPlacementsReference"),("map vehicle","vehiclePlacementsReference")):
  if one(values["map"],r"(?ms)^  "+key+r":\n    m_AssetGUID:([^\n]*)$",label).strip():raise ValueError(label+" has authored GUID")
 if one(values["scene"],r"^  operationMapId: (.+)$","scene operation map")!="opmap.skirmish.cityedgeairfield_prepared":raise ValueError("Selected scene map differs")
 city_guid=one(values["city_meta"],r"^guid: ([0-9a-f]{32})$","city GUID")
 city_ref=one(values["match"],r"^  runtimeCitySpawnerConfig: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}$","match city reference")
 if city_guid!=city_ref:raise ValueError("City reference differs")
 count=int(one(values["city"],r"^  cityCount: (\d+)$","city count"));spawn=int(one(values["city"],r"^  spawnOnStart: ([01])$","city spawn"))
 return {"source_scene_guid":scene_guid,"match_building_config":"authored_null","match_vehicle_config":"authored_null","map_building_reference":"authored_null","map_vehicle_reference":"authored_null","scene_building_placements":"authored_null","scene_vehicle_placements":"authored_null","city_count":count,"city_spawn_on_start":spawn,"live_managed_resolution":"unknown","building_startup_complete":"unknown","native_readiness":False}
def main():
 import argparse
 ap=argparse.ArgumentParser();ap.add_argument("--output",required=True);args=ap.parse_args(); paths={k:ROOT/v for k,v in FILES.items()}
 for p in paths.values():
  if not p.is_file() or p.is_symlink():raise ValueError("regular source required: "+str(p))
 before={str(p):sha(p) for p in paths.values()}; result=audit({k:p.read_text() for k,p in paths.items()})
 if before!={str(p):sha(p) for p in paths.values()}:raise ValueError("source changed during audit")
 receipt={"status":"PassedSerializedLoadingInputAudit","source_hashes":before,"authored_facts":result,"scope":"Serialized inputs only; dynamic scene overrides, managed owner completion and loading admission unverified","mission_ready":False}
 with open(args.output,"x") as f:json.dump(receipt,f,indent=2);f.write("\n")
 print(json.dumps(receipt))
if __name__=="__main__":main()
