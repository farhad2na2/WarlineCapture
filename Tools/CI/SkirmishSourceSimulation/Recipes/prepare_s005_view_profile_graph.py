#!/usr/bin/env python3
"""Add the exact serialized S005 view profile to an external source graph."""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
GRAPH = ROOT / "T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json"
ASSET = ROOT / "Assets/Game/Resources/SkirmishMissionViewProfile_S005.asset"
META = Path(str(ASSET) + ".meta")
PINS = {GRAPH:"b784ffc6b36260b8c403362b93a741a06c493ebdc09704ffd828e82e9ee64138",
        ASSET:"51197d45dd96e086321765ec4d12180eecf5c9cff6d57c6b480f0cda590e2e9d",
        META:"f00ad15ce7e965efdf92f7ddc0e77cf07aa80fd32acc6c29b6c192f831c158be"}
GUID = "325f1c3764b22e44b84e18d8f7547458"
INTS = set("OpeningFocusPolicy UseSeparateAircraftYaw AuthoredNonBaseAssault AuthoredGenericBaseAssault ObjectiveKind SchemaVersion ContentVersion DefinitionVersion".split())
STRINGS = set("OpeningFocusStableActorId CatalogId DefinitionId CampaignSourceContentHash LayoutContentHash".split())
FLOATS = set("AircraftYaw MinHeight MaxHeight NormalHeight BuildHeight Pitch Yaw NormalFieldOfView BuildFieldOfView GroundReferenceY MaximumGroundCenterSlantDistance InfantryMinimumViewportHeight PlaneMaximumViewportDimension AircraftPitch AircraftFieldOfView AircraftMinimumDistance AircraftMaximumDistance".split())

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def verify():
    for path, expected in PINS.items():
        if sha(path) != expected:
            raise ValueError("source pin changed: " + str(path))

def read_fields(text):
    fields = {}
    for line in text.splitlines():
        match = re.fullmatch(r"  ([A-Za-z][A-Za-z0-9_]*): (.*)", line)
        if not match:
            continue
        name, raw = match.groups()
        if name.startswith("m_"):
            continue
        if name in fields or name not in INTS | STRINGS | FLOATS:
            raise ValueError("unknown or duplicate serialized field: " + name)
        if name in STRINGS:
            if not raw or not re.fullmatch(r"[A-Za-z0-9_.]+",raw):
                raise ValueError("unexpected serialized string: " + name)
            fields[name] = raw
        elif name in INTS:
            if not re.fullmatch(r"-?[0-9]+",raw):
                raise ValueError("unexpected serialized integer: " + name)
            fields[name] = int(raw)
        else:
            if not re.fullmatch(r"-?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)",raw):
                raise ValueError("unexpected serialized float: " + name)
            value = float(raw)
            if not math.isfinite(value):
                raise ValueError("nonfinite serialized float: " + name)
            fields[name] = value
    if set(fields) != INTS | STRINGS | FLOATS:
        raise ValueError("serialized fields missing: " + str(sorted((INTS | STRINGS | FLOATS) - set(fields))))
    return fields

def prepare(out):
    verify()
    if not re.search(r"(?m)^guid: " + GUID + r"$", META.read_text()):
        raise ValueError("profile GUID changed")
    fields = read_fields(ASSET.read_text())
    graph = json.loads(GRAPH.read_text())
    if any(n.get("asset_guid") == GUID or n.get("source_path") == str(ASSET.relative_to(ROOT)) for n in graph["nodes"]):
        raise ValueError("profile already present")
    graph["nodes"].append({"asset_guid":GUID, "source_type":"SkirmishMissionViewProfile",
        "source_path":str(ASSET.relative_to(ROOT)), "source_sha256":PINS[ASSET], "fields":fields})
    graph["source_pins"].update({str(ASSET):PINS[ASSET],str(META):PINS[META]})
    graph["view_profile_source"]={"parent_graph_sha256":PINS[GRAPH],"asset_guid":GUID,
        "scope":"Serialized source fields only; no native view or readiness claim"}
    out.mkdir(parents=True, exist_ok=False)
    result = out / "source-config-graph.json"
    result.write_text(json.dumps(graph,indent=2,ensure_ascii=False)+"\n")
    verify()
    (out / "receipt.json").write_text(json.dumps({"status":"PreparedSourceDataOnly", "graph_sha256":sha(result),
        "serialized_field_count":len(fields),"source_asset_sha256":PINS[ASSET],"profile_guid":GUID,
        "native_view_proven":False,"mission_ready":False},indent=2)+"\n")

if __name__ == "__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())
