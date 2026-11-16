#!/usr/bin/env python3
"""Check mission combinations against shared game rules without starting Unity."""
import argparse
import csv
import hashlib
import io
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile
import time
import uuid

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / "Tools/CI"))
import skirmish_offline_sim as sk
from skirmish_offline_sim_cases import case
from skirmish_catalog_diagnostics import diagnose
from build_skirmish_authoring_batches import rows, validate, SIZES

DATA = ROOT / "Design/Roadmap/Skirmish_Expansion"
PACKAGED = ROOT / "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv"
OPS = ROOT / "Design/Roadmap/Operations/MISSION_CATALOG.csv"
WORK = ROOT / "Logs/MissionQuickcheck"
ALL_S = [f"S{i:03}" for i in range(1, 121)]
ALL_O = [f"O{i:03}" for i in range(1, 61)]
PROBES = ("pause", "deadline", "win", "lose", "surrender")


def write(path, value):
    path.write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def load_draft(path):
    def unique(pairs):
        result = {}
        for k, v in pairs:
            if k in result:
                raise ValueError(f"{path}: duplicate field {k}")
            result[k] = v
        return result
    if path.stat().st_size > 2 * 1024 * 1024:
        raise ValueError(f"{path}: draft exceeds 2 MiB")
    return json.loads(path.read_bytes(), object_pairs_hook=unique)


def choices(value, allowed, label):
    items = value.split(",")
    if len(set(items)) != len(items) or not set(items) <= set(allowed):
        raise ValueError(f"Unknown or duplicate {label}: {value}")
    return set(items)


def ops_sources():
    base = ROOT / "Assets/Game/Scripts/Operations"
    return sorted(p for group in ("Contracts", "Tactical", "Strategic", "Loop", "Content")
                  for p in (base / group).glob("*.cs")) + sorted(HERE.glob("*.cs"))


def build_ops(stage, sources):
    stage.mkdir()
    sdk_list = sk.execute(["dotnet", "--list-sdks"], stage, "sdk-list")
    versions = re.findall(r"(?m)^(8\.\d+\.\d+) \[", sdk_list.stdout.decode())
    if not versions:
        raise ValueError("The Operations host needs an installed .NET 8 SDK")
    sdk = max(versions, key=lambda v: tuple(map(int, v.split("."))))
    write(stage / "global.json", {"sdk": {"version": sdk, "rollForward": "disable"}})
    # Snapshot only the production files this executable consumes. No Unity installation or packages.
    for p in sources:
        copy = stage / "src" / p.relative_to(ROOT)
        copy.parent.mkdir(parents=True, exist_ok=True)
        copy.write_bytes(p.read_bytes())
    (stage / "NuGet.Config").write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    (stage / "OperationsHost.csproj").write_text(
        '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType>'
        '<TargetFramework>net8.0</TargetFramework><UseAppHost>false</UseAppHost>'
        '<NuGetAudit>false</NuGetAudit></PropertyGroup></Project>')
    done = sk.execute(["dotnet", "build", "-c", "Release", "--configfile", "NuGet.Config",
                       "--disable-build-servers", "-p:UseSharedCompilation=false", "-o", "bin"],
                      stage, "compile", cwd=stage)
    if done.returncode:
        raise ValueError(f"Operations host compile failed: {stage}/compile.stdout.log")
    if any(sk.sha(stage / "src" / p.relative_to(ROOT)) != sk.sha(p) for p in sources):
        raise ValueError("Operations source changed during compilation")
    write(stage / "binaries.json", {p.name: sk.sha(p) for p in (stage / "bin").iterdir()})


def build_key(sources):
    inputs = [(str(p.relative_to(ROOT)), sk.sha(p)) for p in sources]
    return hashlib.sha256(json.dumps([str(ROOT), inputs]).encode()).hexdigest()


def host(lane, run, cache_root):
    if lane == "skirmish":
        sources = [ROOT / p for p in sk.SOURCES + [sk.BASE, sk.HARNESS, "Tools/CI/skirmish_offline_sim.py"]]
    else:
        sources = ops_sources()
    inputs = sources if lane == "skirmish" else [Path(__file__).resolve(), *sources]
    digest = build_key(inputs)
    cache_root.mkdir(parents=True, exist_ok=True)
    cache = cache_root / (lane + "-" + digest)
    hit = cache.is_dir()
    if not hit:
        # Stage on the cache volume. Reports may be on a different drive, including system temp.
        stage = cache_root / (lane + "-building-" + uuid.uuid4().hex)
        write(run / (lane + "-build-location.json"), {"attempt": str(stage), "published_cache": str(cache)})
        if lane == "skirmish":
            sk.build(ROOT, stage)
        else:
            build_ops(stage, sources)
        if build_key(inputs) != digest:
            raise ValueError(f"{lane} source changed during build")
        try:
            stage.rename(cache)
        except OSError:
            if not cache.is_dir():  # Another successful concurrent build may have won publication.
                raise
    return cache, {"key": digest, "cache_hit": hit}


def extra_cases(paths, catalog, picked):
    by_id = {r["scenario_id"]: r["objective_id"] for r in catalog}
    result = []
    for path in paths:
        trace = load_draft(path)
        if set(trace) != {"missions", "sizes", "family", "cases"}:
            raise ValueError(f"{path}: expected missions, sizes, family and cases")
        ids, sizes, family = trace["missions"], trace["sizes"], trace["family"]
        if (not isinstance(ids, list) or not ids or len(set(ids)) != len(ids) or
                not all(i in by_id and by_id[i] == family for i in ids) or not isinstance(sizes, list)
                or not sizes or len(set(sizes)) != len(sizes) or not set(sizes) <= SIZES):
            raise ValueError(f"{path}: unknown/duplicate mission, wrong objective family or unsupported size")
        matching = [r for r in picked if r["catalog_id"] in ids and r["size_id"] in sizes]
        if not matching or not isinstance(trace["cases"], list) or not trace["cases"]:
            raise ValueError(f"{path}: no selected matching mission/size cases")
        for template in trace["cases"]:
            if set(template) != {"name", "area_ids", "original_ids", "steps", "expect"}:
                raise ValueError(f"{path}: invalid trace case shape")
            name = template["name"]
            if not isinstance(name, str) or not re.fullmatch(r"[A-Za-z0-9_-]{1,64}", name):
                raise ValueError(f"{path}: invalid trace case name")
            for row in matching:
                steps = []
                for step in template["steps"]:
                    if family == "BA" and "facts" in step:
                        deadline = int(row["deadline_seconds"])
                        if step["facts"].get("DeadlineSeconds", deadline) != deadline:
                            raise ValueError(f"{path}: BA deadline differs from fixed setup row")
                        step = dict(step, facts=dict(step["facts"], DeadlineSeconds=deadline))
                    steps.append(step)
                result.append(dict(id=f"{row['catalog_id']}.{row['size_id']}.custom.{name}", family=family,
                                   size=row["size_id"], area_ids=template["area_ids"], original_ids=template["original_ids"],
                                   steps=steps, expect=template["expect"]))
                if len(result) > 10000:
                    raise ValueError(f"{path}: exceeds replay case limit")
    return result


def skirmish(selected, sizes, drafts, traces, run, cache_root, candidate=None):
    catalog, matrix = rows(DATA / "SCENARIO_CATALOG.csv"), rows(DATA / "INITIAL_SETUP_MATRIX.csv")
    validate(catalog, matrix)
    packaged = rows(PACKAGED)
    as_dict = lambda rs: {(r["catalog_id"], r["size_id"]): r for r in rs}
    if len(packaged) != len(matrix) or as_dict(matrix) != as_dict(packaged):
        raise ValueError("Packaged Skirmish sizes/budgets differ from the design matrix")
    matrix_input = None
    if candidate is not None:
        raw = candidate.read_bytes()
        reader = csv.DictReader(io.StringIO(raw.decode("utf-8-sig"), newline=""))
        columns = list(matrix[0])
        if reader.fieldnames is None or sorted(reader.fieldnames) != sorted(columns):
            raise ValueError("Candidate setup matrix columns differ from the design matrix")
        experimental = list(reader)
        if any(None in row or None in row.values() for row in experimental):
            raise ValueError("Candidate setup matrix contains a malformed row")
        validate(catalog, experimental)
        baseline = as_dict(matrix)
        changes = []
        for row in experimental:
            key = (row["catalog_id"], row["size_id"])
            if key[0] in selected and key[1] in sizes:
                changes.append(dict(mission=key[0], size=key[1], fields={
                    k: {"before": baseline[key][k], "after": row[k]} for k in columns if row[k] != baseline[key][k]}))
        matrix_input = dict(path=str(candidate.resolve()), sha256=hashlib.sha256(raw).hexdigest(),
                            selected_rows=changes, scope="CSV row selection and structural checks only")
        matrix = experimental
    if drafts:
        diag = diagnose(ROOT, inputs=drafts)
        write(run / "skirmish-authoring.json", diag)
        if diag["errors"]:
            raise ValueError(f"Skirmish authoring diagnostics found {len(diag['errors'])} errors: see skirmish-authoring.json")
    picked = [r for r in matrix if r["catalog_id"] in selected and r["size_id"] in sizes]
    write(run / "skirmish-setups.json", picked)
    cases = [case(r["catalog_id"], r["objective_id"], r["size_id"], int(r["deadline_seconds"]), probe)
             for r in picked for probe in PROBES]
    extra = extra_cases(traces, catalog, picked)
    cases.extend(extra)
    inputs = run / "skirmish-cases.json"
    # Compact serialization keeps the whole catalog within the existing trace runner's size bound.
    inputs.write_text(json.dumps({"schema_version": 1, "cases": cases}, separators=(",", ":")))
    build, info = host("skirmish", run, cache_root)
    detail = sk.run(build, inputs, run / "skirmish-replay")
    ok = detail.get("result") == "passed" and detail["process_exit"] == 0 and detail["passed"] == len(cases)
    return dict(ok=ok, missions=len(selected), setup_rows=len(picked), candidate_matrix=matrix_input, rule_probes=len(cases),
                custom_rule_probes=len(extra), passed=detail.get("passed", 0), failed=detail.get("failed"), build=info,
                scope="Production objective rules on supplied facts; no battles or difficulty/seed simulation")


OPS_FIELDS = ("mission_id", "scenario_id", "operation_map_id", "district_id", "local_slot", "family",
              "force_package", "enemy_profile", "hard_deadline_seconds", "min_intel", "canonical_seed")


def source_metadata(packet, expected_map, roles):
    """Check small, explicitly selected Campaign map descriptors; never read terrain into the rules model."""
    desc = load_draft(packet)
    maps, pins = {}, {}
    for kind in ("campaign_map", "physical_source", "logical_map"):
        ref = desc[kind]
        path = (ROOT / ref["path"]).resolve(strict=True)
        if not path.is_relative_to(ROOT / "Assets") or path.suffix != ".asset" or path.stat().st_size > 4 * 1024 * 1024:
            raise ValueError(f"{packet}: expected a small existing map definition under Assets")
        raw = path.read_bytes()
        digest = hashlib.sha256(raw).hexdigest()
        if digest != ref["sha256"]:
            raise ValueError(f"{kind}: selected map file changed: {ref['path']}")
        maps[kind], pins[kind] = "\n".join(raw.decode("utf-8-sig").splitlines()), ref

    def field(text, pattern):
        found = re.findall(pattern, text, re.M)
        if len(found) != 1 or not found[0].strip():
            raise ValueError(f"Missing/ambiguous map field: {pattern}")
        return found[0].strip()
    def geometry(text):
        return {k: field(text, pat) for k, pat in {
            "scene": r"^  sourceSceneReference:\s*\n    m_AssetGUID: (\w+)$",
            "grid": r"^  gridMetadata:\s*\n    assetGuid: (\w+)$",
            "surface": r"^  surfaceMetadata:\s*\n    assetGuid: (\w+)$",
            "origin": r"^    origin: (\{[^}\n]+\})", "dimensions": r"^    dimensions: (\{[^}\n]+\})",
            "cell_size": r"^    cellSize: (\S+)$"}.items()}
    physical, logical = maps["physical_source"], maps["logical_map"]
    frame = geometry(physical)
    if field(logical, r"^  operationMapId: (\S+)$") != expected_map:
        raise ValueError("Draft catalog map and logical source differ")
    for kind in ("logical_map", "campaign_map"):
        bound = maps[kind]
        for binding, source in (("sourceOperationMapId", "operationMapId"), ("sourceIdentityHash", "sourceIdentityHash"),
                                ("sourceContentHash", "contentHash")):
            if field(bound, "^    " + binding + r": (\S+)$") != field(physical, "^  " + source + r": (\S+)$"):
                raise ValueError(f"{kind}: physical {binding} differs")
        if geometry(bound) != frame:
            raise ValueError(f"{kind}: scene/grid/surface/frame differs from selected Campaign source")
    declared = set(re.findall(r"^  - anchorId: (\S+)$", logical, re.M))
    if not isinstance(roles, dict) or not roles:
        raise ValueError("Draft must identify its required physical roles")
    missing = [name for name, anchor in roles.items() if anchor is None or anchor not in declared]
    return dict(pins=pins, frame=frame, roles=roles, unresolved_roles=missing, physical_fit_verified=False,
                scope="Current serialized source references and anchor names only; placement and route fit untested")


def operations(selected, offsets, drafts, run, cache_root):
    catalog = rows(OPS)
    if len(catalog) != 60 or {r["code"] for r in catalog} != set(ALL_O):
        raise ValueError("Operations requires the 60 distinct canonical mission codes")
    if any(r["mission_id"] != "operation." + r["code"].lower() for r in catalog):
        raise ValueError("Operations catalog code/mission identity mismatch")
    draft_rows = [load_draft(p) for p in drafts]
    draft_ids = [d["mission"] for d in draft_rows]
    if len(set(draft_ids)) != len(draft_ids) or not set(draft_ids) <= selected:
        raise ValueError("Every Operations draft must target a distinct selected mission")
    map_reports = {}
    map_ids = {r["code"]: r["operation_map_id"] for r in catalog}
    for path, d in zip(drafts, draft_rows):
        source = d.pop("campaign_source")
        map_reports[d["mission"]] = source_metadata(path.parent / source["packet"], map_ids[d["mission"]], source["roles"])
    if map_reports:
        write(run / "operations-source-bindings.json", map_reports)
    build, info = host("operations", run, cache_root)
    bin_dir = build / "bin"
    def check_bins():
        expected = json.loads((build / "binaries.json").read_bytes())
        if {p.name: sk.sha(p) for p in bin_dir.iterdir()} != expected:
            raise ValueError(f"Changed Operations build in {build}")
    check_bins()
    request = run / "operations-input.json"
    write(request, dict(selected=sorted(selected), seed_offsets=offsets, drafts=draft_rows))
    process = sk.execute(["dotnet", bin_dir / "OperationsHost.dll", request], run, "operations")
    check_bins()
    if build_key([Path(__file__).resolve(), *ops_sources()]) != info["key"]:
        raise ValueError("Operations source changed during this run")
    detail = json.loads(process.stdout)
    write(run / "operations.json", detail)
    if "catalog" not in detail:
        raise ValueError(detail.get("error", "Operations host failed; see operations logs"))
    actual = {r["mission_id"]: r for r in detail["catalog"]}
    if len(actual) != len(detail["catalog"]) or len(actual) != 60:
        raise ValueError("Operations production catalog must have 60 distinct entries")
    for r in catalog:
        for key in OPS_FIELDS:
            if str(actual.get(r["mission_id"], {}).get(key)) != r[key]:
                raise ValueError(f"{r['code']}: design/production catalog mismatch for {key}")
    records = detail["missions"]
    if {r["code"] for r in records} != selected or len(records) != len(selected):
        raise ValueError("Incomplete Operations host response")
    return dict(ok=process.returncode == 0 and not any(r["status"] == "failed" for r in records),
                catalog_checked=len(actual), catalog_only=[r["code"] for r in records if r["status"] == "catalog_only"],
                host_runs=sum(len(r.get("runs", [])) for r in records if r["status"] == "host_model"),
                draft_cases=sum(len(r.get("runs", [])) for r in records if r["status"] == "draft_model"),
                unresolved_physical_roles={code: row["unresolved_roles"] for code, row in map_reports.items()},
                failures=[r["code"] + ": " + r["error"] for r in records if r["status"] == "failed"],
                build=info, scope="Authored Operations loop or explicitly supplied tactical draft; no native world")


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--missions", default="all", help="all, skirmish, operations, or comma-separated stable IDs")
    p.add_argument("--sizes", default="Standard,War,LargeWar", help="Skirmish sizes (comma-separated)")
    p.add_argument("--operation-seed-offsets", default="0", help="Run authored Operations from canonical seed + each offset")
    p.add_argument("--skirmish-draft", type=Path, action="append", default=[], help="Existing logical authoring JSON for catalog diagnostics")
    p.add_argument("--skirmish-matrix", type=Path, help="External 360-row candidate setup CSV for structural checks and selected-row readback")
    p.add_argument("--skirmish-trace", type=Path, action="append", default=[], help="External objective fact traces, expanded over selected matching Skirmish IDs/sizes")
    p.add_argument("--operation-draft", type=Path, action="append", default=[], help="External tactical graph and regression cases")
    p.add_argument("--strict-coverage", action="store_true", help="Exit 2 if a selected Operation has no authored graph")
    p.add_argument("--out", type=Path, help="New report directory (default Logs/MissionQuickcheck/runs/...)")
    p.add_argument("--build-cache", type=Path, default=WORK / "builds",
                   help="Build directory under project Logs, T Logs or system temp; use an empty one to measure cold builds")
    p.add_argument("--json", action="store_true", help="Print the full summary as JSON")
    args = p.parse_args()
    start = time.perf_counter()
    aliases = {"all": ALL_S + ALL_O, "skirmish": ALL_S, "operations": ALL_O}
    selected = (set(aliases[args.missions]) if args.missions in aliases else
                choices(args.missions, ALL_S + ALL_O, "mission ID"))
    sizes = choices(args.sizes, SIZES, "Skirmish size")
    offsets = [int(v) for v in args.operation_seed_offsets.split(",")]
    if not 1 <= len(set(offsets)) == len(offsets) <= 32 or not all(0 <= v <= 2**31-1-1161 for v in offsets):
        raise ValueError("Expected 1..32 distinct nonnegative Operations seed offsets in range")
    ss, oo = selected & set(ALL_S), selected & set(ALL_O)
    if ((args.skirmish_matrix or args.skirmish_draft or args.skirmish_trace) and not ss) or (args.operation_draft and not oo):
        raise ValueError("A draft must have a selected mission of that mode")
    dest = args.out or WORK / "runs" / (time.strftime("%Y%m%d-%H%M%S") + "-" + uuid.uuid4().hex[:8])
    cache = args.build_cache.resolve()
    if not any(cache.is_relative_to(p.resolve()) for p in (ROOT / "Logs", ROOT / "T Logs", Path(tempfile.gettempdir()))):
        raise ValueError("Build cache must be under project Logs, T Logs or system temp")
    run = sk.new_output(ROOT, dest)
    report = dict(native_runs=0, player_ready=False, physical_source_fit_verified=False, errors=[])
    for lane, ids, fn in (("skirmish", ss, lambda: skirmish(ss, sizes, args.skirmish_draft, args.skirmish_trace, run, cache, args.skirmish_matrix)),
                          ("operations", oo, lambda: operations(oo, offsets, args.operation_draft, run, cache))):
        if ids:
            began = time.perf_counter()
            try:
                report[lane] = fn()
                if not report[lane]["ok"]:
                    report["errors"].append(f"{lane} checks failed; see the lane report")
            except (OSError, ValueError, TypeError, KeyError, RuntimeError, subprocess.SubprocessError) as e:
                report["errors"].append(f"{lane}: {e}")
            report[lane + "_seconds"] = round(time.perf_counter() - began, 3)
    gaps = report.get("operations", {}).get("catalog_only", [])
    strict = args.strict_coverage or args.missions not in aliases
    code = 1 if report["errors"] else 2 if gaps and strict else 0
    report.update(elapsed_seconds=round(time.perf_counter() - start, 3), exit_code=code,
                  status={0: "checked", 1: "failed", 2: "missing_graph"}[code])
    write(run / "summary.json", report)
    if args.json:
        print(json.dumps(dict(report, report=str(run / "summary.json"))))
    else:
        if "skirmish" in report:
            r = report["skirmish"]
            print(f"Skirmish: {r['passed']}/{r['rule_probes']} objective probes on {r['setup_rows']} size rows; no battles played.")
        if "operations" in report:
            r = report["operations"]
            print(f"Operations: {r['catalog_checked']} catalog entries checked; {r['host_runs']} authored runs, "
                  f"{r['draft_cases']} draft cases passed; {len(gaps)} selected entries catalog only.")
            for mission, roles in r["unresolved_physical_roles"].items():
                if roles:
                    print(f"{mission}: unbound physical roles: {', '.join(roles)}")
            for reason in r["failures"]:
                print(reason, file=sys.stderr)
        for reason in report["errors"]:
            print(reason, file=sys.stderr)
        print(f"[MissionQuickcheck] result={report['status']} elapsed={report['elapsed_seconds']}s native_runs=0 "
              f"report={run / 'summary.json'}")
    return code


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (OSError, ValueError, KeyError) as e:
        sys.exit(str(e))
