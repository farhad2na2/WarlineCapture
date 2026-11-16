#!/usr/bin/env python3
"""Append an explicitly pinned authored view profile to a source config graph.

This reads serialized data only. It does not decide view validity, establish
native camera equivalence, publish a map, or admit a mission.
"""
import argparse
import hashlib
import json
import math
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
SCRIPT_GUID = "77e34b4584fd4df9aef1cd6c8c8db196"
INTS = set("OpeningFocusPolicy UseSeparateAircraftYaw AuthoredNonBaseAssault AuthoredGenericBaseAssault ObjectiveKind SchemaVersion ContentVersion DefinitionVersion".split())
STRINGS = set("OpeningFocusStableActorId CatalogId DefinitionId CampaignSourceContentHash LayoutContentHash ActiveOperationMapId ActiveMapContentHash ActiveMapSourceIdentityHash".split())
OPTIONAL = set("ActiveOperationMapId ActiveMapContentHash ActiveMapSourceIdentityHash".split())
FLOATS = set("AircraftYaw MinHeight MaxHeight NormalHeight BuildHeight Pitch Yaw NormalFieldOfView BuildFieldOfView GroundReferenceY MaximumGroundCenterSlantDistance InfantryMinimumViewportHeight PlaneMaximumViewportDimension AircraftPitch AircraftFieldOfView AircraftMinimumDistance AircraftMaximumDistance".split())

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def contained(path):
    path = Path(path).absolute()
    for part in (path, *path.parents):
        if part.is_symlink(): raise ValueError("Symlink input ancestry unsupported: "+str(part))
    path = path.resolve(strict=True)
    if not path.is_file() or not path.is_relative_to(ROOT): raise ValueError("Repository file required: "+str(path))
    return path

def parse(text, catalog):
    if len(re.findall(r"(?m)^  m_EditorClassIdentifier: Game\.Configs::Game\.Configs\.SkirmishMissionViewProfile$",text)) != 1:
        raise ValueError("Exact profile class identifier required")
    if len(re.findall(r"(?m)^  m_Script: \{fileID: 11500000, guid: "+SCRIPT_GUID+r", type: 3\}$",text)) != 1:
        raise ValueError("Exact profile script identity required")
    if len(re.findall(r"(?m)^  m_Name: SkirmishMissionViewProfile_"+re.escape(catalog)+r"$",text)) != 1:
        raise ValueError("Exact profile asset name required")
    fields={}
    for line in text.splitlines():
        match=re.fullmatch(r"  ([A-Za-z][A-Za-z0-9_]*): ?(.*)",line)
        if not match: continue
        name,raw=match.groups()
        if name.startswith("m_"): continue
        if name in fields or name not in INTS|STRINGS|FLOATS: raise ValueError("Unknown or duplicate field: "+name)
        if name in STRINGS:
            if not re.fullmatch(r"[A-Za-z0-9_.]*",raw): raise ValueError("Unsupported string spelling: "+name)
            fields[name]=raw
        elif name in INTS:
            if not re.fullmatch(r"-?[0-9]+",raw): raise ValueError("Invalid integer: "+name)
            fields[name]=int(raw)
        else:
            if not re.fullmatch(r"-?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)",raw): raise ValueError("Invalid float: "+name)
            value=float(raw)
            if not math.isfinite(value): raise ValueError("Nonfinite float: "+name)
            fields[name]=value
    if not (INTS|STRINGS|FLOATS)-OPTIONAL <= fields.keys(): raise ValueError("Required serialized field missing")
    if fields["CatalogId"]!=catalog or fields["DefinitionId"]!="skirmish."+catalog.lower(): raise ValueError("Profile catalog identity mismatch")
    return fields

def prepare(graph_path, graph_sha, asset_path, asset_sha, meta_sha, output):
    graph_path=contained(graph_path);asset_path=contained(asset_path);meta_path=contained(str(asset_path)+".meta")
    paths={graph_path:graph_sha,asset_path:asset_sha,meta_path:meta_sha}
    for p,h in paths.items():
        if not re.fullmatch(r"[0-9a-f]{64}",h) or sha(p)!=h: raise ValueError("Explicit source pin mismatch: "+str(p))
    match=re.fullmatch(r"SkirmishMissionViewProfile_(S[0-9]{3})\.asset",asset_path.name)
    if not match or asset_path.parent!=ROOT/"Assets/Game/Resources": raise ValueError("Exact authored Resources profile path required")
    catalog=match.group(1)
    if not 1<=int(catalog[1:])<=120: raise ValueError("Catalog outside mission domain")
    guids=re.findall(r"(?m)^guid: ([0-9a-f]{32})$",meta_path.read_text())
    if len(guids)!=1: raise ValueError("Exact profile GUID required")
    fields=parse(asset_path.read_text(),catalog);graph=json.loads(graph_path.read_text())
    if graph.get("schema_version")!=1 or graph.get("authority")!="AuthoredConfigAssets" or not isinstance(graph.get("source_pins"),dict) or not isinstance(graph.get("nodes"),list):
        raise ValueError("Existing authored source graph required")
    parent_pins={}
    for name,digest in graph["source_pins"].items():
        p=Path(name);p=contained(p if p.is_absolute() else ROOT/p)
        if sha(p)!=digest: raise ValueError("Parent graph source pin changed: "+name)
        if p in parent_pins and parent_pins[p]!=digest: raise ValueError("Conflicting parent graph pin: "+name)
        parent_pins[p]=digest
    relative=str(asset_path.relative_to(ROOT));guid=guids[0]
    if len(graph["nodes"])>=64 or any(n.get("asset_guid")==guid or n.get("source_path")==relative for n in graph["nodes"]):
        raise ValueError("Unique bounded source graph node required")
    graph["nodes"].append({"asset_guid":guid,"source_type":"SkirmishMissionViewProfile","source_path":relative,"source_sha256":asset_sha,"fields":fields})
    graph["source_pins"].update({str(asset_path):asset_sha,str(meta_path):meta_sha})
    graph["view_profile_source"]={"catalog_id":catalog,"parent_graph_sha256":graph_sha,"asset_guid":guid,"scope":"Serialized source data only; no native view or readiness claim"}
    output=Path(output);output.mkdir(parents=True,exist_ok=False);dest=output/"source-config-graph.json"
    dest.write_text(json.dumps(graph,indent=2,ensure_ascii=False)+"\n")
    for p,h in paths.items():
        if sha(p)!=h: raise ValueError("Source pin changed during projection: "+str(p))
    for p,h in parent_pins.items():
        if sha(p)!=h: raise ValueError("Parent source pin changed during projection: "+str(p))
    (output/"receipt.json").write_text(json.dumps({"status":"PreparedSourceDataOnly","catalog_id":catalog,"graph_sha256":sha(dest),"serialized_field_count":len(fields),"source_asset_sha256":asset_sha,"profile_guid":guid,"native_view_proven":False,"mission_ready":False},indent=2)+"\n")

if __name__=="__main__":
    p=argparse.ArgumentParser();p.add_argument("--graph",required=True);p.add_argument("--graph-sha256",required=True);p.add_argument("--asset",required=True);p.add_argument("--asset-sha256",required=True);p.add_argument("--meta-sha256",required=True);p.add_argument("--output",required=True);a=p.parse_args();prepare(a.graph,a.graph_sha256,a.asset,a.asset_sha256,a.meta_sha256,a.output)
