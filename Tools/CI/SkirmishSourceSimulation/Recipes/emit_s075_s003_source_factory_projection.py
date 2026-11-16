#!/usr/bin/env python3
"""Emit a source-only S075 factory projection from retained S003 factory facts."""
from __future__ import annotations
import argparse, concurrent.futures, datetime, hashlib, json, os, pathlib, re, subprocess, sys

ROOT = pathlib.Path(__file__).resolve().parents[4]
R2 = pathlib.Path("T Logs/SkirmishProduction/s075-s003-bounded-factory-compatibility-20261114-r2")
CORRECTION = pathlib.Path("T Logs/SkirmishProduction/s075-s003-bounded-factory-compatibility-20261114-r3-audit/receipt.json")
AUDIT_DIR = pathlib.Path("T Logs/SkirmishProduction/s003-retained-metadata-source-pin-audit-20261114-r1")
CAPTURE = pathlib.Path("Logs/SkirmishProduction/s003-current-metadata-refresh-20261106-r3/data/s003-native-metadata.json")
S075 = pathlib.Path("T Logs/SkirmishProduction/s075-provisional-fieldbase-layout-20261114-r1")
AUTHOR_BUILD = pathlib.Path("T Logs/SkirmishProduction/s074-generic-s002-author-build-20261114-r2")
BASE = pathlib.Path("Tools/CI/SkirmishSourceSimulation/Adapters/SourceMetadataCompatibility.cs")
PROJECTOR = pathlib.Path("Tools/CI/SkirmishSourceSimulation/Adapters/SourceMetadataCompatibility.Derived.s003.cs")
SELECTOR = pathlib.Path("Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceMetadataCompatibility.s003.cs")
CSC = pathlib.Path("/usr/local/share/dotnet/sdk/8.0.423/Roslyn/bincore/csc.dll")

def sha_bytes(b): return hashlib.sha256(b).hexdigest()
def sha(p): return sha_bytes(pathlib.Path(p).read_bytes())
def rel(p, root): return str(pathlib.Path(p).resolve().relative_to(root))
def selected_drift(mismatches, selected):
    # Both scans record repository-relative paths; compare like representations.
    return {row['path'] for row in mismatches} & {row['path'] for row in selected}
def file_pin(p, root):
    p = pathlib.Path(p)
    return {"path": rel(p, root), "sha256": sha(p), "bytes": p.stat().st_size}

def extract_method(text, marker):
    start = text.find(marker)
    if start < 0: raise ValueError("Missing exact method marker: " + marker)
    op = text.find("{", start)
    depth, i, state, verbatim = 0, op, "code", False
    while i < len(text):
        c = text[i]; n = text[i + 1] if i + 1 < len(text) else ""
        if state == "code":
            if c == "/" and n == "/": state, i = "line", i + 2; continue
            if c == "/" and n == "*": state, i = "block", i + 2; continue
            if c == '"': state, verbatim, i = "string", i > 0 and text[i - 1] == "@", i + 1; continue
            if c == "'": state, i = "char", i + 1; continue
            if c == "{": depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0: return text[start:i + 1]
        elif state == "line":
            if c == "\n": state = "code"
        elif state == "block":
            if c == "*" and n == "/": state, i = "code", i + 2; continue
        elif state == "string":
            if verbatim and c == '"' and n == '"': i += 2; continue
            if not verbatim and c == "\\": i += 2; continue
            if c == '"': state = "code"
        elif state == "char":
            if c == "\\": i += 2; continue
            if c == "'": state = "code"
        i += 1
    raise ValueError("Unclosed original method: " + marker)

def scan_full_pins(root, capture):
    pins = capture.get("source_pins")
    if not isinstance(pins, dict) or len(pins) != 8848:
        raise ValueError("Expected complete 8,848-entry original source pin map")
    def one(pair):
        path, expected = pair
        try: actual = sha(path)
        except OSError: actual = None
        absolute = pathlib.Path(path)
        try: display_path = str(absolute.resolve().relative_to(root))
        except ValueError: display_path = str(absolute)
        return {"path": display_path, "expected_sha256": expected, "current_sha256": actual,
                "state": "missing" if actual is None else "match" if actual == expected else "changed"}
    with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:
        return list(pool.map(one, sorted(pins.items())))

def selected_68(root, capture, base_source):
    fs = re.findall(r"\bFactorySources\s*=\s*\{([^}]+)\}", base_source, re.S)
    osrc = re.findall(r"\bOwnerDefinitions\s*=\s*\{([^}]+)\}", base_source, re.S)
    if len(fs) != 1 or len(osrc) != 1: raise ValueError("Exact exporter source/dependency lists required")
    paths = []
    for name in re.findall(r'"([^"\n]+)"', fs[0]) + re.findall(r'"([^"\n]+)"', osrc[0]):
        p = "Assets/Game/Scripts/" + name
        paths.extend([p, p + ".meta"])
    paths.extend(["Packages/manifest.json", "Packages/packages-lock.json"])
    if len(paths) != 68 or len(set(paths)) != 68: raise ValueError("Expected exactly 68 distinct current source/dependency pins")
    allpins = capture["source_pins"]; out = []
    for p in paths:
        absolute = str((root / p).resolve(strict=True))
        expected = allpins.get(absolute, allpins.get(p))
        actual = sha(absolute)
        if expected is None or actual != expected: raise ValueError("Current selected pin mismatch: " + p)
        out.append({"path": p, "expected_sha256": expected, "current_sha256": actual, "status": "Match"})
    return out

def run_mono(root, out, control, exe, capture_path, metadata_out, label):
    mono = pathlib.Path(control["runtime"]["path"])
    cfg = root / control["runtime"]["mono_config"]
    env = os.environ.copy(); env["MONO_PATH"] = os.pathsep.join(control["runtime"]["MONO_PATH_directories"])
    s075 = root / S075
    args = [root / "T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json",
            s075 / "layout.json", root / "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv",
            capture_path, s075 / "authoring-bound/authoring-result.json",
            s075 / "original-owner/result/mission-logical-binding.json", metadata_out]
    cmd = ["rtk", "proxy", str(mono), "--config", str(cfg), "--optimize=-float32", str(exe), *map(str, args)]
    proc = subprocess.run(cmd, cwd=root, env=env, capture_output=True, text=True)
    log = out / ("run-" + label + ".log"); log.write_text(proc.stdout + proc.stderr)
    rows = [x for x in proc.stdout.splitlines() if x.startswith("{")]
    if len(rows) != 1: raise RuntimeError("Expected one harness result; see " + str(log))
    return json.loads(rows[0]), {"exit_code": proc.returncode, "log": file_pin(log, root),
                                "stdout": proc.stdout, "stderr": proc.stderr}

def project(root, output):
    root = pathlib.Path(root).resolve(strict=True); output = pathlib.Path(output).absolute()
    if output.exists(): raise ValueError("Fresh exclusive output directory required")
    r2 = root / R2; control = json.loads((r2 / "control.json").read_text())
    if control.get("status") != "PassedSourceOnlyFactoryCompatibilityProjection" or control.get("method_invoked") is not True:
        raise ValueError("Pinned successful r2 original projector result required")
    if control["compile"]["exit_code"] != 0 or control["run"]["exit_code"] != 0:
        raise ValueError("Pinned r2 compile and run must have succeeded")
    if sha(r2 / "compile.rsp") != control["compile_response"]["sha256"]:
        raise ValueError("Pinned r2 compiler response changed")
    if sha(r2 / "compile.log") != control["compile"]["log_sha256"] or sha(r2 / "run.log") != control["run"]["log_sha256"]:
        raise ValueError("Pinned r2 logs changed")
    markers = [x for x in (r2 / "run.log").read_text().splitlines() if x.startswith("{")]
    if len(markers) != 1 or json.loads(markers[0]) != control.get("result"):
        raise ValueError("Pinned r2 runtime marker/result mismatch")
    corr = root / CORRECTION; correction = json.loads(corr.read_text())
    if correction.get("status") != "PassedR2EvidenceReconciled":
        raise ValueError("R2 stale-field correction receipt required")
    capture_path = root / CAPTURE; capture_raw = capture_path.read_bytes(); capture_sha = sha_bytes(capture_raw)
    capture = json.loads(capture_raw)
    if capture.get("kind") != "NativeS003OriginalCompilerFactoryMetadataCapture" or capture.get("source_pins") is None:
        raise ValueError("Retained original S003 capture required")
    if (capture["context"]["catalog_id"], capture["context"]["seed"], capture["context"]["setup_hash"]) != ("S003",104732,2394754492):
        raise ValueError("Exact S003 origin context required")
    for key in ("full_mission_initialized","mission_world_created","runtime_state_transferred","source_fit_certified","native_context_qualified","mission_ready"):
        if capture.get(key) is not False: raise ValueError("S003 capture scope flag changed: " + key)
    adir = root / AUDIT_DIR; audit_path = adir / "audit.json"; audit_rec_path = adir / "receipt.json"
    audit = json.loads(audit_path.read_text()); audit_rec = json.loads(audit_rec_path.read_text())
    full_scan = scan_full_pins(root,capture); mismatches = [x for x in full_scan if x["state"] != "match"]
    audit_core = [{k: row[k] for k in ("path", "expected_sha256", "current_sha256", "state")}
                  for row in audit.get("mismatches", [])]
    if len(full_scan) != 8848 or len(mismatches) != 11 or mismatches != audit_core:
        raise ValueError("Current full 8,848-pin rehash differs from the exact 11-row drift audit")
    if audit.get("capture",{}).get("sha256") != capture_sha or audit_rec.get("audit",{}).get("sha256") != sha(audit_path):
        raise ValueError("Full source drift audit is not bound to this capture")
    base_path = root / BASE; base_src = base_path.read_text(); selected = selected_68(root,capture,base_src)
    for key, path in (("projector_source", root / PROJECTOR), ("selector_source", root / SELECTOR),
                      ("host_source", base_path)):
        if sha(path) != control["inputs"][key]["sha256"]:
            raise ValueError("Original r2 source pin changed: " + key)
    if sha(r2 / "CompatibilityProgram.BoundS075.cs") != control["inputs"]["bound_compatibility_harness"]["sha256"]:
        raise ValueError("Original bound S075 harness pin changed")
    if selected_drift(mismatches, selected):
        raise ValueError("Full capture drift intersects current selected 68 pin closure")
    output.mkdir(parents=True, exist_ok=False)

    # Generate an isolated harness from the pinned successful r2 harness.
    bound_harness = (r2 / "CompatibilityProgram.BoundS075.cs").read_text()
    if bound_harness.count("if (args.Length != 6)") != 1: raise ValueError("Pinned bound harness signature changed")
    bound_harness = bound_harness.replace("if (args.Length != 6)", "if (args.Length != 7)")
    old = "Expected S075 graph, layout, setup matrix, S003 native capture, S075 authoring result, and pinned S075 logical binding."
    new = "Expected S075 graph, layout, setup matrix, retained S003 capture, S075 result, logical binding, output path."
    if bound_harness.count(old) != 1: raise ValueError("Bound harness argument description changed")
    bound_harness = bound_harness.replace(old,new)
    method_call = "var projected = SourceMetadataCompatibility.ProjectCoveredFactoryMetadata(native, setup);"
    if bound_harness.count(method_call) != 1: raise ValueError("Original projector call missing")
    bound_harness = bound_harness.replace(method_call, method_call + "\n                File.WriteAllText(args[6], projected.ToString(Newtonsoft.Json.Formatting.None) + Environment.NewLine);")
    harness = output / "CompatibilityProgram.SourceProjection.cs"; harness.write_text(bound_harness)

    host_text = base_src; marker = "internal sealed class SourceMetadataCompatibility"
    if host_text.count(marker) != 1: raise ValueError("Expected one host class declaration")
    host_copy = output / "SourceMetadataCompatibility.Host.partial.cs"
    host_copy.write_text(host_text.replace(marker,"internal sealed partial class SourceMetadataCompatibility",1))
    projector_text = (root / PROJECTOR).read_text(); selector_text = (root / SELECTOR).read_text()
    pmethod = extract_method(projector_text,"internal static JObject ProjectCoveredFactoryMetadata(")
    smethod = extract_method(selector_text,"static List<SourceUnitPrefab> SelectCapturedFactoryPrefabs(")
    if sha_bytes(pmethod.encode()) != control["extraction"]["projector_method_sha256"] or sha_bytes(smethod.encode()) != control["extraction"]["selector_method_sha256"]:
        raise ValueError("Extracted source methods differ from the successful r2 method pins")
    extracted = output / "SourceMetadataCompatibility.ExtractedMethods.cs"
    extracted.write_text("using System;\nusing System.IO;\nusing System.Linq;\nusing System.Collections.Generic;\nusing Newtonsoft.Json.Linq;\nusing Game.Configs;\nnamespace Game.Runtime { internal sealed partial class SourceMetadataCompatibility {\n" + pmethod + "\n" + smethod + "\n}}\n")

    args = [json.loads(x) for x in (r2 / "compile.rsp").read_text().splitlines() if x.strip()]
    rew=[]; counts={"out":0,"host":0,"methods":0,"harness":0}
    for a in args:
        if a.startswith("-out:"): rew.append("-out:"+str(output/"S075S003FactoryProjection.exe")); counts["out"]+=1
        elif a.endswith("SourceMetadataCompatibility.Host.partial.cs"): rew.append(str(host_copy)); counts["host"]+=1
        elif a.endswith("SourceMetadataCompatibility.ExtractedMethods.cs"): rew.append(str(extracted)); counts["methods"]+=1
        elif a.endswith("CompatibilityProgram.BoundS075.cs"): rew.append(str(harness)); counts["harness"]+=1
        else: rew.append(a)
    if counts != {"out":1,"host":1,"methods":1,"harness":1}: raise ValueError("Unexpected r2 compile response layout: "+str(counts))
    rsp=output/"compile.rsp"; rsp.write_text("".join(json.dumps(x)+"\n" for x in rew))
    cp=subprocess.run(["rtk","proxy","dotnet","exec",str(CSC),"@"+str(rsp)],cwd=root,capture_output=True,text=True)
    clog=output/"compile.log"; clog.write_text(cp.stdout+cp.stderr)
    if cp.returncode: raise RuntimeError("Source-only projection harness compile failed")
    exe=output/"S075S003FactoryProjection.exe"

    # Preserve the complete pin inventory and full drift report as separate artifacts.
    inventory=output/"retained-s003-source-pin-inventory.json"
    inventory.write_text(json.dumps(capture["source_pins"],sort_keys=True,separators=(",",":"))+"\n")
    drift=output/"full-capture-drift-audit.json"; drift.write_bytes(audit_path.read_bytes())
    selected_file=output/"current-selected-factory-dependency-pins.json"
    selected_file.write_text(json.dumps({"count":68,"pins":selected},indent=2,sort_keys=True)+"\n")
    projected_path=output/"projected-factory-metadata.json"
    positive,positive_run=run_mono(root,output,control,exe,capture_path,projected_path,"positive")
    if positive_run["exit_code"] or positive.get("status")!="PassedSourceOnlyFactoryCompatibilityProjection" or not projected_path.is_file():
        raise RuntimeError("Original S075 projector did not pass; see run-positive.log")
    if positive.get("projected_building_rows")!=10 or positive.get("world_allocated") is not False or positive.get("mission_ready") is not False:
        raise ValueError("Positive projector result/scope mismatch")
    projected=json.loads(projected_path.read_text())

    # These are synthetic copies only; they are never used as provenance.
    controls_dir=output/"synthetic-controls"; controls_dir.mkdir()
    role_doc=json.loads(capture_raw); field="Game.Skirmish.Contracts.SkirmishRoleOverlay::Damage"
    role_items=role_doc["context"]["metadata"]["catalog_inputs"]["role_overlays"]["items"]
    old_damage=role_items[0]["fields"][field]["value"]; role_items[0]["fields"][field]["value"]=str(int(old_damage)+1)
    role_path=controls_dir/"synthetic-role-overlay-mutated.json"; role_path.write_text(json.dumps(role_doc,indent=2)+"\n")
    prefab_doc=json.loads(capture_raw); removed=prefab_doc["context"]["metadata"]["buildings"].pop()
    prefab_path=controls_dir/"synthetic-required-prefab-removed.json"; prefab_path.write_text(json.dumps(prefab_doc,indent=2)+"\n")
    negatives=[]
    for name,path,guard,mutation in [
        ("tampered_role_overlay",role_path,"Metadata role/recipe input changed: 0/Damage",{"field":field,"from":old_damage,"to":str(int(old_damage)+1)}),
        ("missing_required_building_prefab",prefab_path,"Exact native selected factory coverage required",{"removed_last_building_row":removed,"indices_contiguous":True})]:
        absent=output/(name+"-must-not-be-written.json")
        result,run=run_mono(root,output,control,exe,path,absent,name)
        if run["exit_code"] or result.get("status")!="RejectedByOriginalFactoryCompatibilityGuard" or result.get("world_allocated") is not False or result.get("mission_ready") is not False or guard not in str(result.get("exact_message","")) or absent.exists():
            raise RuntimeError("Original projector negative control failed: "+name)
        negatives.append({"case":name,"synthetic_input":file_pin(path,root),"mutation":mutation,"expected_guard":guard,"run":run,"observed":result,"synthetic_not_native_evidence":True})
    if sha(capture_path)!=capture_sha: raise ValueError("Retained S003 capture changed")
    s075=root/S075; binding=s075/"original-owner/result/mission-logical-binding.json"; owner=s075/"original-owner/receipt.json"
    owner_doc=json.loads(owner.read_text())
    if sha(binding)!=owner_doc["outputs"]["binding"]["sha256"] or sha(binding)!=control["inputs"]["s075_logical_binding"]["sha256"]:
        raise ValueError("S075 logical binding changed or lost owner provenance")
    if len(mismatches)!=11 or len(selected)!=68: raise ValueError("Final audit cardinality mismatch")
    provenance={
        "schema_version":1,"kind":"S075SourceOnlyFactoryMetadataProjection",
        "native_origin":{"kind":capture["kind"],"capture":file_pin(capture_path,root),
            "context":{k:capture["context"][k] for k in ("catalog_id","difficulty","size","seed","setup_hash","metadata_key_sha256")},
            "source_graph":capture["source_graph"],"source_pin_count":8848,
            "source_pin_inventory":file_pin(inventory,root),"full_capture_drift_audit":file_pin(drift,root),
            "disclosure":{"mismatches":11,"selected_68_overlap":0,"all_8848_rehashed":True}},
        "target_context":{"catalog_id":"S075","definition_id":"skirmish.s075","difficulty":"Regular","size":"Standard","seed":104804,"setup_hash":1280564676,
            "binding":file_pin(binding,root),"binding_identity":owner_doc["binding_identity"],"owner_receipt":file_pin(owner,root)},
        "projected_metadata":file_pin(projected_path,root),"selected_current_pins":file_pin(selected_file,root),
        "emitter_source":file_pin(pathlib.Path(__file__),root),
        "projector":{"projector_source":file_pin(root/PROJECTOR,root),"projector_method_sha256":sha_bytes(pmethod.encode()),
            "selector_source":file_pin(root/SELECTOR,root),"selector_method_sha256":sha_bytes(smethod.encode()),
            "host_source":file_pin(root/BASE,root),"extracted_methods":file_pin(extracted,root)},
        "positive":{"run":positive_run,"observed":positive},
        "negative_controls":negatives,
        "gates":{"world_allocated":False,"native_context_qualified":False,"source_fit_certified":False,"runtime_admission":False,"mission_ready":False,"native_s075_capture":False},
        "scope_note":"S003 captured factory facts projected with the exact original projector under a freshly compiled, logically bound S075 setup. Synthetic negative controls are not native evidence."}
    prov=output/"provenance.json"; prov.write_text(json.dumps(provenance,indent=2,sort_keys=True)+"\n")
    receipt={"schema_version":1,"kind":"S075S003SourceOnlyFactoryProjectionReceipt","status":"PassedSourceOnlyProjection",
        "capture_unchanged":True,"binding_unchanged":True,"full_source_pin_scan":{"count":8848,"mismatch_count":11,"matches":8837,"missing":0,"audit_exact":True},
        "current_selected_pin_scan":{"count":68,"matches":68,"mismatch_count":0,"overlap_with_full_capture_drift":0},
        "compile":{"exit_code":cp.returncode,"response":file_pin(rsp,root),"log":file_pin(clog,root),"executable":file_pin(exe,root)},
        "emitter_source":file_pin(pathlib.Path(__file__),root),
        "artifacts":{"projected_metadata":file_pin(projected_path,root),"provenance":file_pin(prov,root),
            "source_pin_inventory":file_pin(inventory,root),"full_capture_drift_audit":file_pin(drift,root),"selected_current_pins":file_pin(selected_file,root)},
        "positive":{"run":positive_run,"observed":positive},"negative_controls":negatives,
        "scope":{"capture_modified":False,"binding_modified":False,"synthetic_controls_are_native_evidence":False,
            "world_allocated":False,"native_context_qualified":False,"source_fit_certified":False,"runtime_admission":False,"mission_ready":False}}
    receipt_path=output/"projection-receipt.json"; receipt_path.write_text(json.dumps(receipt,indent=2,sort_keys=True)+"\n")
    return receipt

def main():
    parser=argparse.ArgumentParser(description=__doc__); parser.add_argument("--repo",type=pathlib.Path,default=ROOT); parser.add_argument("--output",type=pathlib.Path,required=True)
    args=parser.parse_args()
    try:
        receipt=project(args.repo,args.output)
        print(json.dumps({"status":receipt["status"],"artifacts":receipt["artifacts"],"pins":receipt["full_source_pin_scan"],"selected":receipt["current_selected_pin_scan"],"mission_ready":False},indent=2)); return 0
    except (OSError,ValueError,KeyError,TypeError,json.JSONDecodeError,RuntimeError) as error:
        print("S075 S003 source-only factory projection failed: "+str(error),file=sys.stderr); return 2

if __name__=="__main__": raise SystemExit(main())
