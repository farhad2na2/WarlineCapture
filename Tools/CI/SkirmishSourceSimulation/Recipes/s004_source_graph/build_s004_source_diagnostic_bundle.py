#!/usr/bin/env python3
"""Bind checked S004 source inputs for unqualified, opt-in source-host development."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[5]
EVIDENCE=ROOT/"T Logs/SkirmishProduction/s004-authored-loading-projection-20261104-T1/receipt.json"
EVIDENCE_SHA="5e5a8d368b9d7a2e1198572f2771fc1d5b5fdafed77ececbc34d85d2f43d9164"
RESULT=EVIDENCE.parent/"requests.json"
PATHS={
 "compiled_setup":"T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json",
 "prefab_shared":"Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json",
 "prefab_s004":"Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-r3/data/prefab-reference-data.json",
 "building_definitions":"Logs/SkirmishProduction/s004-current-full-factory-runway-20261104-r1/data/full-factory-runway-definitions.json",
 "configured_building_readmodel":"Logs/SkirmishProduction/s004-current-configured-readmodel-20261104-r1/data/configured-spawnable-readmodel.json",
 "authored_grid":"T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/authored-grid-walkable-emulation.u8",
 "physical_config_graph":"T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/source-config-graph-with-cityedge-physical-inputs.json",
 "static_blockers":"Logs/SkirmishProduction/s004-static-blocker-source-20261104-r1/data/static-blockers.json",
 "base_surface_roads":"T Logs/SkirmishProduction/s004-current-source-road-mask-20261104-T2/base-surface-roads.u8",
 "map_rotation":"Logs/SkirmishProduction/s004-operation-map-rotation-capture-20261103-r2/rotation-reference.json",
 "authored_loading_audit":"T Logs/SkirmishProduction/s004-authored-loading-input-audit-20261104-T1/receipt.json"}
def sha(path):
 h=hashlib.sha256()
 with open(path,"rb") as f:
  for chunk in iter(lambda:f.read(1024*1024),b""):h.update(chunk)
 return h.hexdigest()
def descriptor(path,expected):
 path=Path(path)
 if not path.is_absolute() or not path.is_file() or path.is_symlink() or sha(path)!=expected:raise ValueError("Missing, symlinked or changed source input: "+str(path))
 return {"path":str(path),"sha256":expected,"bytes":path.stat().st_size}
def build(evidence=EVIDENCE,evidence_sha=EVIDENCE_SHA,source_build=None):
 evidence=Path(evidence)
 if not evidence.is_absolute() or evidence.is_symlink() or sha(evidence)!=evidence_sha:raise ValueError("Exact checked S004 evidence required")
 result_path=evidence.parent/"requests.json"
 receipt=json.loads(evidence.read_text()); pins=receipt["input_sha256"]
 if receipt.get("status")!="ObservedDiagnosticSourcePlacement" or receipt.get("physical_source_validated") is not False or receipt.get("runtime_admission") is not False or receipt.get("mission_ready") is not False:raise ValueError("Source evidence scope differs")
 descriptor(result_path,receipt["requests_sha256"]);result=json.loads(result_path.read_text());gate=result["loading_gate"]
 if result.get("registered")!=20 or result.get("actors_created")!=54 or gate.get("gate_complete") is not False or gate.get("source_activation_result") is not False:raise ValueError("Source startup evidence differs")
 descriptors={k:descriptor(ROOT/path,pins[str(ROOT/path)]) for k,path in PATHS.items()}
 build_path=Path(source_build) if source_build else next(Path(p) for p in pins if p.endswith("/s004-building-definition-source-build-20261104-T1/build.json"))
 if not build_path.is_absolute() or str(build_path) not in pins:raise ValueError("Evidence-bound source build required")
 descriptor(build_path,pins[str(build_path)]);build=json.loads(build_path.read_text())
 if build.get("status")!="Passed" or build.get("compile_exit")!=0:raise ValueError("Passed source build required")
 for path,digest in build["source_and_reference_pins"].items():descriptor(path,digest)
 for path,digest in pins.items():descriptor(path,digest)
 return {"kind":"S004SourceDiagnosticInputBundle","schema_version":1,"mission_id":"S004","scope":"SourceHostDiagnosticOnly","evidence":descriptor(evidence,evidence_sha),"inputs":descriptors,
  "missing_or_assumed":{"scene_road_overlays":"unknown","sidewalk":"unknown","water":"unknown","authored_building_mask":"unknown","native_scene_loaded":False,"view_profile":"source-host assumption absent","native_managed_loading_facts":"unknown"},
  "qualification":{"source_fit":False,"physical_source":False,"native_input":False,"runtime_admission":False,"mission_ready":False,"terminal_outcome_validated":False}}
def main():
 import argparse
 ap=argparse.ArgumentParser();ap.add_argument("--output",required=True);ap.add_argument("--evidence");ap.add_argument("--evidence-sha256");ap.add_argument("--build");a=ap.parse_args()
 if any((a.evidence,a.evidence_sha256,a.build)) and not all((a.evidence,a.evidence_sha256,a.build)):raise ValueError("Evidence, SHA and source build must be supplied together")
 doc=build(Path(a.evidence),a.evidence_sha256,Path(a.build)) if a.evidence else build()
 with open(a.output,"x") as f:json.dump(doc,f,indent=2);f.write("\n")
 print(json.dumps({"status":"PassedSourceDiagnosticBundle","path":str(Path(a.output).resolve()),"sha256":sha(a.output),"input_count":len(doc["inputs"]),"mission_ready":False}))
if __name__=="__main__":main()
