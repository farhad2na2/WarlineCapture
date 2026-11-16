#!/usr/bin/env python3
"""Rebind four S073 source-only descriptors after independently finalized captures."""
import argparse, copy, hashlib, json
from pathlib import Path

REPLACED=("prefab_capture","source_identity","legacy_config","production_prefabs")

def sha(path):
    h=hashlib.sha256()
    with Path(path).open("rb") as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b""): h.update(chunk)
    return h.hexdigest()

def need(condition,message):
    if not condition: raise ValueError(message)

def read(path):
    with Path(path).open(encoding="utf-8") as stream:return json.load(stream)

def descriptor(path):
    path=Path(path).resolve(strict=True)
    need(path.is_file(),"Regular source input required: "+str(path))
    return {"path":str(path),"sha256":sha(path)}

def validate_values(base,old_config,config,prefab,names,production,prefab_receipt,name_receipt,production_receipt,hashes):
    need(base.get("kind")=="S073LegacyStartupBundle" and base.get("schema_version")==1 and base.get("mission")=="S073" and base.get("seed")==104729,"Exact S073 base identity required")
    for key in ("initial_spawn","preset_values","captured_unit_prefab_roots"):
        need(old_config.get(key)==config.get(key),"Rebind must preserve existing source values: "+key)
    need(config.get("kind")=="S073LegacyInitialSpawnExternalData" and config.get("mission")=="S073" and config.get("world_initialized") is False and config.get("mission_outcome_derived") is False and config.get("native_visual_readiness") is False,"Source-only legacy config required")
    artifacts=config.get("source_artifacts",{})
    need(artifacts.get("prefab_capture_sha256")==hashes["prefab_capture"] and artifacts.get("name_readback_sha256")==hashes["source_identity"] and config.get("captured_unit_prefab_roots")==prefab.get("opening_prefabs"),"Fresh legacy configuration/capture identity differs")
    need(prefab.get("kind")==production.get("kind")=="AuthoredSkirmishPrefabReferenceData" and prefab.get("authority")==production.get("authority")=="ActualSourceAssetsAndNativeBakers_NotMissionState","Original baker reference data required")
    for x in (prefab,production):
        need(x.get("full_mission_initialized") is False and x.get("fresh_world_allocator_exported") is False and x.get("native_visual_readiness") is False,"Source-only capture scope required")
    need(names.get("kind")=="S073LegacyPrefabSourceNameReadback" and names.get("prefab_capture_sha256")==hashes["prefab_capture"] and names.get("mission_world_allocated") is False and names.get("native_visual_readiness") is False,"Matching source-only name readback required")
    for receipt,hash_field,expected in ((prefab_receipt,"capture_sha256",hashes["prefab_capture"]),(name_receipt,"name_readback_sha256",hashes["source_identity"])):
        need(receipt.get("validation_status")=="Passed" and receipt.get("wrapper_exit_code")==0 and receipt.get("required_marker_present") is True and receipt.get(hash_field)==expected and receipt.get("mission_ready") is False,"Passed matching focused capture receipt required")
    need(production_receipt.get("kind")=="S073ProductionPrefabCaptureReceipt" and production_receipt.get("validation_status")=="Passed" and production_receipt.get("capture_wrapper_exit_code")==0 and production_receipt.get("capture_required_marker_present") is True and production_receipt.get("capture_sha256")==hashes["production_prefabs"] and production_receipt.get("key_count")==12 and production_receipt.get("mission_ready") is False,"Passed matching production receipt required")
    need(len(prefab.get("requested_prefab_keys",[]))==4 and len(production.get("requested_prefab_keys",[]))==12 and set(prefab["requested_prefab_keys"]).issubset(production["requested_prefab_keys"]),"Original four opening roots must be contained in the 12 production roots")

def run(a):
    output=Path(a.output).absolute(); receipt_path=Path(a.receipt).absolute()
    need(not output.exists() and not receipt_path.exists(),"Fresh output and receipt paths required")
    paths={"base":Path(a.base).resolve(strict=True),"prefab_capture":Path(a.prefab).resolve(strict=True),"source_identity":Path(a.names).resolve(strict=True),"legacy_config":Path(a.config).resolve(strict=True),"production_prefabs":Path(a.production).resolve(strict=True),"prefab_receipt":Path(a.prefab_receipt).resolve(strict=True),"name_receipt":Path(a.name_receipt).resolve(strict=True),"production_receipt":Path(a.production_receipt).resolve(strict=True)}
    before={k:sha(v) for k,v in paths.items()}; need(before["base"]==a.base_sha256,"Explicit original base bytes differ")
    base=read(paths["base"]); old_path=Path(base["legacy_config"]["path"]).resolve(strict=True)
    need(sha(old_path)==base["legacy_config"]["sha256"],"Original legacy config changed")
    old=read(old_path); config=read(paths["legacy_config"]); prefab=read(paths["prefab_capture"]); names=read(paths["source_identity"]); production=read(paths["production_prefabs"])
    validate_values(base,old,config,prefab,names,production,read(paths["prefab_receipt"]),read(paths["name_receipt"]),read(paths["production_receipt"]),before)
    source_roots=base.get("source_repository_root"),base.get("asset_repository_root")
    need(all(Path(x).resolve(strict=True)==Path(a.repo).resolve(strict=True) for x in source_roots),"Exact source/asset repository root required")
    preserved={}
    for key,value in base.items():
        if key not in REPLACED and isinstance(value,dict) and set(value)=={"path","sha256"}:
            need(sha(value["path"])==value["sha256"],"Preserved direct descriptor changed: "+key);preserved[key]=value
    for capture in (prefab,production,names):
        pins=capture.get("source_pins");need(isinstance(pins,dict) and len(pins)>0,"Fresh capture source pins required")
        for name,expected in pins.items():
            source=Path(name) if Path(name).is_absolute() else Path(a.repo)/name
            need(sha(source)==expected,"Fresh capture source pin changed: "+str(source))
    rebound=copy.deepcopy(base)
    for key in REPLACED:rebound[key]=descriptor(paths[key])
    need(all(rebound[k]==v for k,v in base.items() if k not in REPLACED),"Unexpected bundle mutation")
    need(all(sha(p)==before[k] for k,p in paths.items()) and sha(old_path)==base["legacy_config"]["sha256"],"Source evidence changed during rebind")
    output.parent.mkdir(parents=True,exist_ok=True);receipt_path.parent.mkdir(parents=True,exist_ok=True)
    with output.open("x",encoding="utf-8") as f:json.dump(rebound,f,indent=2);f.write("\n")
    receipt={"kind":"S073FreshPrefabSourceDiagnosticRebind","status":"Passed","base":descriptor(paths["base"]),"output":descriptor(output),"replaced":{key:rebound[key] for key in REPLACED},"capture_receipts":{key:descriptor(paths[key]) for key in ("prefab_receipt","name_receipt","production_receipt")},"preserved_direct_descriptors":sorted(preserved),"same_initial_spawn_preset_and_roots":True,"scope":"Source-only descriptor rebind; preserved inputs may have historical transitive pins","source_execution_qualified":False,"native_parity":False,"mission_ready":False}
    with receipt_path.open("x",encoding="utf-8") as f:json.dump(receipt,f,indent=2);f.write("\n")
    print(json.dumps({"status":"Passed","bundle":str(output),"sha256":sha(output),"mission_ready":False}))

if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__)
    for name in ("repo","base","base-sha256","prefab","names","config","production","prefab-receipt","name-receipt","production-receipt","output","receipt"):p.add_argument("--"+name,required=True)
    run(p.parse_args())
