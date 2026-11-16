#!/usr/bin/env python3
"""Run a provenance-pinned S004 plus seven-root S003 diagnostic in one disposable manager."""
import argparse, hashlib, importlib.util, json, os, shutil, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
S3RAW = ROOT / "Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/data/prefab-reference-data.json"
S3STRICT = ROOT / "Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/strict-receipt.json"
S3KEYS = ROOT / "T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/prefab-keys.json"
S3KEYRECEIPT = ROOT / "T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/receipt.json"
IDENTITY = ROOT / "T Logs/SkirmishProduction/s004-production-prefab-identity-cross-capture-20261103-T1/receipt.json"
PROJECTION = ROOT / "Logs/SkirmishProduction/s004-scoped-prefab-source-only-projection-20261103-T3/projection.json"
PROVENANCE = ROOT / "Logs/SkirmishProduction/s004-scoped-prefab-source-only-projection-20261103-T3/provenance-receipt.json"
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S004S003DualPrefabImportDiagnostic.cs"
PASS = "[S004S003DualPrefabImportDiagnostic] result=Passed rootsS003=7 rootsS004=9 rollback=Passed remaps=Disjoint cleanup=Passed world=0 mission=0 admission=0 readiness=0"

def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def need(ok, msg):
    if not ok: raise ValueError(msg)
def read(p): return json.loads(Path(p).read_text())
def visit(v):
    if isinstance(v, dict):
        if v.get("codec") == "entity-v1": yield v.get("id")
        else:
            for x in v.values(): yield from visit(x)
    elif isinstance(v, list):
        for x in v: yield from visit(x)

def run(build_dir, output):
    build_dir, output = Path(build_dir).resolve(), Path(output).resolve()
    need(output.is_relative_to(ROOT) and not output.exists(), "New T output path inside repository required")
    build = read(build_dir / "build.json")
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("mission") == "S002" and build.get("status") == "Passed" and build.get("compile_exit") == 0, "Passed S002 source build required")
    for path, digest in build.get("source_and_reference_pins", {}).items():
        need(Path(path).is_file() and sha(path) == digest, "Source build pin drift: " + path)
    need(sha(build["executable"]["path"]) == build["executable"]["sha256"], "Build executable pin drift")
    prov = read(PROVENANCE); projection = read(PROJECTION)
    need(prov.get("status") == "PassedSourceOnlyProvenanceGate_NoImportAdmission" and prov.get("projection_sha256") == sha(PROJECTION), "Exact T3 S004 provenance required")
    validator_path = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/project_s004_passed_scoped_prefab_capture.py"
    spec = importlib.util.spec_from_file_location("s004_projection_validator", validator_path); validator = importlib.util.module_from_spec(spec); spec.loader.exec_module(validator)
    validator.validate_projection(projection, ROOT)
    idr = read(IDENTITY)
    for rel, pin in idr["inputs"].items(): need((ROOT / rel).is_file() and sha(ROOT / rel) == pin["sha256"], "Identity receipt input pin drift: " + rel)
    need(idr.get("status") == "PassedSourceOnlyIdentityComparison" and idr.get("scope_limits", {}).get("runtime_import") is False, "Identity comparison source-only receipt required")
    selected_rows = [r for r in idr["s004_absent_root_records"] if r.get("s003_descriptor_identity_match") is True and r.get("s003_root_requested_by_exact_key") is True and r.get("s003_capture_pin_matches_current") is True and r.get("current_prefab_sha256") == r.get("s003_descriptor_prefab_sha256") == r.get("s003_capture_source_pin_sha256")]
    keys3 = [r["key"] for r in selected_rows]
    need(len(keys3) == 7 and len(set(keys3)) == 7, "Exactly seven descriptor-bound roots required")
    s3, strict, descriptor, keyreceipt = map(read, (S3RAW, S3STRICT, S3KEYS, S3KEYRECEIPT))
    need(sha(S3RAW) == "9b31b6863e80d3bd548cd78631a3385b1c7d0ed007da1785c00f1cb16d7a323e" and sha(S3STRICT) == "824c296c3b993e1d18cad419c43ffbd1c97bf91988fd7ba8a4e1071b3ad5b1ee" and sha(S3KEYS) == "38bd886005c62bc121d165e1119a3f8876b456eddaa0ef873a1a5681aa66539b" and sha(S3KEYRECEIPT) == "c676b2a726ea8134f72189d37e46ecb552e34eec8524b7b033c2464540eee181", "Exact S003 capture and descriptor chain required")
    need(strict.get("validation_status") == "PassedDiagnosticSourceOnly" and strict.get("capture_sha256") == sha(S3RAW) and strict.get("key_descriptor_sha256") == sha(S3KEYS) and descriptor == s3.get("requested_prefab_keys") and keyreceipt.get("descriptor", {}).get("keys_in_metadata_order") == descriptor, "S003 strict capture/descriptor provenance mismatch")
    need(isinstance(s3.get("source_pins"), dict) and len(s3["source_pins"]) == strict.get("source_pin_count"), "Complete S003 strict source pin map required")
    for rel, digest in s3["source_pins"].items(): need((ROOT / rel).is_file() and sha(ROOT / rel) == digest, "S003 live source pin drift: " + rel)
    need(set(keys3) <= set(descriptor) and not set(keys3).intersection(projection["requested_prefab_keys"]), "S003 root set mismatch or overlap with S004")
    entity_map = {r["id"]: r for r in s3["entities"]}; need(len(entity_map) == len(s3["entities"]), "Duplicate S003 source entity ids")
    roots = {k: s3["opening_prefabs"][k] for k in keys3}; need(len(roots) == 7, "Seven exact S003 roots absent")
    closure = set(roots.values()); queue = list(closure)
    while queue:
        eid = queue.pop(); need(eid in entity_map, "Dangling source Entity reference/root: " + eid)
        for ref in visit(entity_map[eid]):
            if ref not in (None, "0:0") and ref not in closure: closure.add(ref); queue.append(ref)
    sub = dict(s3); sub["opening_prefabs"] = roots; sub["requested_prefab_keys"] = keys3
    sub["entities"] = [r for r in s3["entities"] if r["id"] in closure]
    sub["unsupported_values"] = [x for x in s3["unsupported_values"] if x["path"].split("/",1)[0] in closure]
    subset_keys_path = output / "s003-seven-keys.json"; subset_path = output / "s003-seven-root-closure.json"
    output.mkdir(parents=True); subset_keys_path.write_text(json.dumps(keys3, indent=2)+"\n"); subset_path.write_text(json.dumps(sub, indent=2)+"\n")
    s4raw = ROOT / projection["capture"]["path"]
    need(sha(s4raw) == projection["capture"]["sha256"], "S004 scoped raw capture drift")
    s4keys_path = output / "s004-nine-keys.json"; s4keys_path.write_text(json.dumps(projection["requested_prefab_keys"], indent=2)+"\n")
    pins = build["source_and_reference_pins"]; cscs = [Path(p) for p in pins if p.endswith("/Roslyn/bincore/csc.dll")]; csc = next((p for p in cscs if p.is_file() and sha(p)==pins[str(p)]), None)
    need(csc is not None, "Pinned Roslyn compiler missing"); dotnet = shutil.which("dotnet"); need(dotnet and any(Path(dotnet).resolve()==Path(p).resolve() for p in pins if Path(p).name=="dotnet"), "Pinned dotnet host required")
    rsp_src = build_dir / "compile.rsp"; out_exe = output / "S004S003DualPrefabImportDiagnostic.exe"; rsp=output / "compile.rsp"
    lines=[]
    for line in rsp_src.read_text().splitlines():
        v=line.strip().strip('"')
        if v.startswith("-out:"): line='"-out:'+str(out_exe)+'"'
        elif v.startswith("-main:"): line='"-main:S004S003DualPrefabImportDiagnostic"'
        lines.append(line)
    lines.append('"'+str(HARNESS)+'"'); rsp.write_text("\n".join(lines)+"\n")
    comp=subprocess.run([dotnet,str(csc),"@"+str(rsp)],capture_output=True,text=True,timeout=300); (output/"compile.log").write_text(comp.stdout+comp.stderr)
    need(comp.returncode==0 and out_exe.is_file(), "Dual-import harness compilation failed")
    runtime=build["runtime_selection"]; mono=Path(runtime["mono"]); eng=Path(runtime["engine"]); newt=Path(runtime["newtonsoft"]); placement=Path(build["placement_build"]).parent; need(placement.is_dir(), "Pinned placement assembly directory required"); env=os.environ.copy(); env["MONO_PATH"]=os.pathsep.join((str(ROOT/"Library/ScriptAssemblies"),str(eng),str(newt.parent),str(placement),str(output)))
    control=output/"control.json"; cmd=[str(mono),str(out_exe),str(subset_path),str(s4raw),str(subset_keys_path),str(s4keys_path),str(S3RAW.parent),str(control)]
    runres=subprocess.run(cmd,capture_output=True,text=True,env=env,timeout=180); log=runres.stdout+runres.stderr; (output/"run.log").write_text(log)
    need(runres.returncode==0 and runres.stdout.splitlines().count(PASS)==1 and control.is_file(), "Compiled dual import diagnostic failed")
    facts=read(control); need(facts.get("status")=="PassedTwoSequentialSourceImports_DisposableLogicalEntityManagerOnly" and facts.get("s003_roots")==7 and facts.get("s004_roots")==9 and facts.get("shared_source_entity_ids",0)>0 and facts.get("s003_remapped_entity_references",0)>0 and facts.get("s004_remapped_entity_references",0)>0 and all(facts.get(k) is True for k in ("logical_identity_sets_disjoint","all_component_and_buffer_entity_references_stay_in_own_result_map","cross_map_substitution_rejected_by_result_domain","second_import_partial_allocation_rollback","rollback_preserved_first_import","s003_cleanup_preserved_s004","all_imports_cleaned_up")), "Import diagnostic facts mismatch")
    receipt={"kind":"S004S003DualPrefabImportDiagnosticReceipt","status":"PassedDiagnosticOnly_DisposableLogicalEntityManager","identity_receipt":{"path":str(IDENTITY.relative_to(ROOT)),"sha256":sha(IDENTITY),"selected_keys":keys3},"s003_capture":{"path":str(S3RAW.relative_to(ROOT)),"sha256":sha(S3RAW),"strict_receipt_sha256":sha(S3STRICT),"descriptor_sha256":sha(S3KEYS),"subset_entity_count":len(closure),"subset_closure_sha256":sha(subset_path)},"s004_projection":{"path":str(PROJECTION.relative_to(ROOT)),"sha256":sha(PROJECTION),"provenance_receipt_sha256":sha(PROVENANCE),"source_capture_sha256":sha(s4raw)},"source_build":{"path":str((build_dir/"build.json").relative_to(ROOT)),"sha256":sha(build_dir/"build.json"),"executable_sha256":build["executable"]["sha256"]},"harness":{"path":str(HARNESS.relative_to(ROOT)),"sha256":sha(HARNESS),"executable_sha256":sha(out_exe)},"result":facts,"run_log_sha256":sha(output/"run.log"),"compile_log_sha256":sha(output/"compile.log"),"unity_world_created":False,"mission_manager_created":False,"mission_admission_attempted":False,"runtime_replay_ready":False,"mission_ready":False}
    (output/"receipt.json").write_text(json.dumps(receipt,indent=2)+"\n"); print(json.dumps({"status":receipt["status"],"s003_roots":7,"s003_entities":len(closure),"s004_roots":9,"output":str(output)}))

def main():
    p=argparse.ArgumentParser(description=__doc__); p.add_argument("--build-dir",required=True); p.add_argument("--output",required=True); a=p.parse_args()
    try: run(a.build_dir,a.output)
    except (ValueError,OSError,KeyError,TypeError,json.JSONDecodeError,subprocess.SubprocessError) as e: print("S004/S003 dual-import diagnostic failed closed: "+str(e),file=sys.stderr); return 2
    return 0
if __name__=="__main__": raise SystemExit(main())
