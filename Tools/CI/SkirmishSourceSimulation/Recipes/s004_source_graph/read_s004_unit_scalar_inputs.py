#!/usr/bin/env python3
"""Read finite S004 serialized scalar inputs; does not claim Unity prefab resolution."""
import argparse, hashlib, json, re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
BASE = "Assets/Game/Prefabs/Characters/Unit.prefab"
VEHICLE = "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab"
AUTHORING = "Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs"
POLICY = "Assets/Game/Scripts/Components/UnitProductionMetadataScalarPolicy.cs"
OWNER = "Assets/Game/Scripts/Composition/BuildingProductionUnitMetadataPrefabSystemHelper.cs"
DEFINITION_OWNER = "Assets/Game/Scripts/Composition/BuildingDefinitionUnitAuthoringMetadata.cs"
MATERIAL_POLICY = "Assets/Game/Scripts/Configs/UnitMaterialsCostPolicy.cs"
CONFIGS = {
 "Unit_Veh_APC_Heavy": "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_APC_Heavy_Config.asset",
 "Unit_Veh_Missle_Launcher_Ground": "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Missle_Launcher_Ground_Config.asset",
 "Unit_Veh_Truck_Canopy": "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Truck_Canopy.asset",
}
FIELDS = ("isAirUnit", "productionDurationSeconds", "productionTransportArrivalSeconds",
 "productionTransportHoldForNextReadySeconds", "productionTransportMaxConcurrent",
 "productionTransportRequiresAirportRunway", "productionTransportUsesRunwayLanding")

def sha(data): return hashlib.sha256(data).hexdigest()
def one(pattern, text, label):
    values = re.findall(pattern, text, re.MULTILINE)
    if len(values) != 1: raise ValueError("Exact unique " + label + " required")
    return values[0]
def field(text, name): return one(r"^  " + re.escape(name) + r":\s*([^\r\n]+)\s*$", text, name).strip()
def guid(meta): return one(r"^guid:\s*([0-9a-f]{32})\s*$", meta, "GUID")
def plain_folded(text, name):
    match = re.search(r"(?m)^  " + re.escape(name) + r":\s*([^\r\n]+)(?:\r?\n((?:    [^\r\n]+\r?\n)*))?", text)
    if not match or len(re.findall(r"(?m)^  " + re.escape(name) + r":", text)) != 1: raise ValueError("Unique plain text required: "+name)
    parts = [match.group(1).strip()] + [line.strip() for line in (match.group(2) or "").splitlines()]
    if any(not value or any(character in value for character in ("'", '"', "#", "\\", "\t")) for value in parts): raise ValueError("Unsupported serialized plain text: "+name)
    return " ".join(parts)

def read():
    paths = [BASE, BASE+".meta", VEHICLE, VEHICLE+".meta", AUTHORING, AUTHORING+".meta", POLICY, OWNER, DEFINITION_OWNER, MATERIAL_POLICY,
             "Assets/Game/Scripts/Configs/GameplayConfigModels.cs", "Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs", "Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs.meta",
             "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/read_s004_unit_scalar_inputs.py"]
    for key, config in CONFIGS.items(): paths.extend(("Assets/Game/Prefabs/Vehicles/"+key+".prefab", "Assets/Game/Prefabs/Vehicles/"+key+".prefab.meta", config, config+".meta"))
    raw = {name: (ROOT/name).read_bytes() for name in paths}; pins = {str(ROOT/name): sha(data) for name,data in raw.items()}
    text = {name:data.decode("utf-8-sig") for name,data in raw.items()}
    if guid(text[BASE+".meta"]) != "5680e7910ed3741beba6f07116b6b03c" or guid(text[VEHICLE+".meta"]) != "9ce6998c034284d98a89b5badc6e2f24": raise ValueError("Exact inherited prefab chain required")
    block = re.search(r"(?ms)^--- !u!114 &106347591433935587\n(.*?)(?=^--- !u!|\Z)",text[BASE])
    if block is None: raise ValueError("Base authoring block required")
    block = block.group(1)
    if "guid: "+guid(text[AUTHORING+".meta"]) not in block: raise ValueError("Base source is not UnitGridAuthoring")
    if "m_SourcePrefab: {fileID: 100100000, guid: 5680e7910ed3741beba6f07116b6b03c, type: 3}" not in text[VEHICLE]: raise ValueError("Intermediate source chain changed")
    instance = int(one(r"^--- !u!1001 &(\d+)$", text[VEHICLE], "intermediate instance"))
    if (instance ^ 106347591433935587) != 391336776749024018: raise ValueError("Inherited target local identity changed")
    if "UnitProductionMetadataScalarPolicy.Project(authoring.CaptureProductionMetadataScalarSource())" not in text[OWNER]: raise ValueError("Original metadata owner seam changed")
    for expression in ("DisplayName = authoring.ConfiguredDisplayName", "Description = authoring.ConfiguredDescription", "FootprintCells = authoring.GetConfiguredFootprintCells()", "CanRequest = authoring.CanRequest", "Price = authoring.MaterialsCost", "CreditsCost = authoring.Price"):
        if text[DEFINITION_OWNER].count(expression) != 1: raise ValueError("Original definition projection changed: "+expression)
    if text["Assets/Game/Scripts/Configs/GameplayConfigModels.cs"].count("private int materialsCost;") < 1: raise ValueError("Original omitted materials default changed")
    if "class UnitGridAuthoringPrefabConfigAsset : UnitGridAuthoringConfig" not in text["Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs"]: raise ValueError("Exact config inheritance required")
    source = {name:field(block,name) for name in FIELDS}
    if field(block,"productionTransportPrefab") != "{fileID: 0}": raise ValueError("Unsupported base transport reference")
    rows=[]
    for key, config in CONFIGS.items():
        prefab="Assets/Game/Prefabs/Vehicles/"+key+".prefab"; data=text[prefab]; cfg=text[config]; config_guid=guid(text[config+".meta"])
        if "m_SourcePrefab: {fileID: 100100000, guid: 9ce6998c034284d98a89b5badc6e2f24, type: 3}" not in data: raise ValueError("Wrong variant source: "+key)
        target=r"- target: \{fileID: 391336776749024018, guid: 9ce6998c034284d98a89b5badc6e2f24, type: 3\}\s+propertyPath: ([^\r\n]+)\s+value: ?([^\r\n]*)\s+objectReference: ([^\r\n]+)"
        overrides=re.findall(target,data); mapping={}
        for name,value,reference in overrides:
            if name in mapping: raise ValueError("Duplicate authoring override: "+key+"/"+name)
            mapping[name]=(value.strip(),reference.strip())
        if mapping.get("config",("", ""))[1] != "{fileID: 11400000, guid: "+config_guid+", type: 2}": raise ValueError("Wrong authoring config binding: "+key)
        if field(cfg,"m_Script") != "{fileID: 11500000, guid: "+guid(text["Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs.meta"])+", type: 3}": raise ValueError("Wrong config source type")
        scalars={}
        for name in FIELDS:
            authoring=mapping.get(name,(source[name],""))[0]; configured=field(cfg,name)
            if authoring != configured: raise ValueError("Authoring/config scalar difference requires native readback: "+key+"/"+name)
            scalars[name]=authoring
        if field(cfg,"productionTransportPrefab") != "{fileID: 0}" or mapping.get("productionTransportPrefab",("", "{fileID: 0}"))[1] != "{fileID: 0}": raise ValueError("Non-null transport requires exact asset resolution")
        authored_price = mapping.get("price",(field(block,"price"),""))[0]
        configured_price = field(cfg,"price")
        if authored_price != configured_price or not configured_price.isdecimal() or int(configured_price) <= 0: raise ValueError("Exact positive matching source price required: "+key)
        request = mapping.get("canRequest",(field(block,"canRequest"),""))[0]
        if request not in ("0","1") or request != field(cfg,"canRequest"): raise ValueError("Authoring/config requestability mismatch: "+key)
        materials = re.findall(r"(?m)^  materialsCost:\s*(\d+)\s*$",cfg)
        if len(materials)>1: raise ValueError("Duplicate materials cost")
        serialized_materials = int(materials[0]) if materials else 0
        size=one(r"^  footprintCells:\s*\{x:\s*(\d+),\s*y:\s*(\d+)\}\s*$",cfg,"config footprint")
        for axis,actual in zip(("x","y"),size):
            if mapping.get("footprintCells."+axis,("", ""))[0] != actual: raise ValueError("Authoring/config footprint mismatch: "+key)
        rows.append({"key":key,"prefab":prefab,"config":config,"config_guid":config_guid,"scalars":scalars,"footprint":[int(size[0]),int(size[1])],"transport_serialized_null":True,
                     "definition_source":{"display_name":plain_folded(cfg,"displayName"),"description":plain_folded(cfg,"description"),"can_request":request=="1", "credits_price":int(configured_price),
                                          "serialized_materials_cost":serialized_materials,"materials_field_present":bool(materials)}})
    if any(sha((ROOT/name).read_bytes())!=pins[str(ROOT/name)] for name in paths): raise ValueError("Source changed during readback")
    return {"kind":"S004SerializedProductionScalarInputs","status":"CollectedSourceOnly","source_pins":pins,"rows":rows,
            "scope":"Serialized source chain and candidate authoring scalars; no native prefab resolution, runtime admission or readiness.","mission_ready":False}

def main():
    p=argparse.ArgumentParser();p.add_argument("--output",type=Path,required=True);a=p.parse_args();d=read()
    with a.output.open("x") as f:json.dump(d,f,indent=2);f.write("\n")
    print(json.dumps({"status":d["status"],"rows":len(d["rows"]),"source_pins":len(d["source_pins"])}))
if __name__=="__main__":main()
