#!/usr/bin/env python3
"""Generate a disposable PathfindBatchJob surface-query memo overlay.

The overlay is source-host-only. It does not alter the checked-in runtime source,
Burst path, request admission, route algorithm, or dynamic placement checks.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
SOURCE = Path("Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs")
DEFAULT_RESUME_MANIFEST = Path("T Logs/SkirmishProduction/s026-resume-receipt-overlay-20261113-r1/source-manifest.json")
SNAPSHOT_SOURCES = (
    Path("Assets/Game/Scripts/Systems/UnitPathfindingScheduler.cs"),
    Path("Assets/Game/Scripts/Systems/MapSurfacePathfindingSnapshot.cs"),
    Path("Assets/Game/Scripts/Components/MapSurfaceComponents.cs"),
)
ANCHOR_CONTEXT = '''                    FootprintSize = footprint.Size, IsVehicle = vehicle, ManualMove = manual, FactionId = Factions[index]
                };'''
ANCHOR_ROUTE = '''                GridPathRoutingKernel.Route(ref context, request, ref state);'''
ANCHOR_CELL = '''            public bool IsSurfaceCellPathable(int x, int z) => Job.IsSurfaceCellPathable(new int2(x, z), IsVehicle);'''
ANCHOR_FOOTPRINT = '''            public bool IsSurfaceFootprintPathable(int x, int z) => Job.IsSurfaceFootprintPathable(new int2(x, z), FootprintSize, IsVehicle);'''

CELL_IMPL = '''            public Dictionary<int2,bool> SurfaceCellMemo;
            public Dictionary<int2,bool> SurfaceFootprintMemo;
            public bool ShadowVerifySurfaceMemo;
            private bool IsSurfaceCellPathableOriginal(int x,int z) => Job.IsSurfaceCellPathable(new int2(x,z),IsVehicle);
            private bool IsSurfaceFootprintPathableOriginal(int x,int z) => Job.IsSurfaceFootprintPathable(new int2(x,z),FootprintSize,IsVehicle);
            public bool IsSurfaceCellPathable(int x,int z)
            {
                if(SurfaceCellMemo==null)return IsSurfaceCellPathableOriginal(x,z);
                var key=new int2(x,z);
                if(SurfaceCellMemo.TryGetValue(key,out bool cached))
                {
                    if(ShadowVerifySurfaceMemo){bool actual=IsSurfaceCellPathableOriginal(x,z);if(actual!=cached){SurfaceCellMemo.Clear();SurfaceFootprintMemo.Clear();throw new System.InvalidOperationException("Surface-cell memo shadow mismatch; request caches cleared");}}
                    return cached;
                }
                bool result=IsSurfaceCellPathableOriginal(x,z);
                if(SurfaceCellMemo.Count<4096)SurfaceCellMemo.Add(key,result);
                return result;
            }
            public bool IsSurfaceFootprintPathable(int x,int z)
            {
                if(SurfaceFootprintMemo==null)return IsSurfaceFootprintPathableOriginal(x,z);
                var key=new int2(x,z); // Footprint size and movement class are fixed for this request context.
                if(SurfaceFootprintMemo.TryGetValue(key,out bool cached))
                {
                    if(ShadowVerifySurfaceMemo){bool actual=IsSurfaceFootprintPathableOriginal(x,z);if(actual!=cached){SurfaceCellMemo.Clear();SurfaceFootprintMemo.Clear();throw new System.InvalidOperationException("Surface-footprint memo shadow mismatch; request caches cleared");}}
                    return cached;
                }
                bool result=IsSurfaceFootprintPathableOriginal(x,z);
                if(SurfaceFootprintMemo.Count<4096)SurfaceFootprintMemo.Add(key,result);
                return result;
            }'''


def transform(source: str) -> str:
    anchors = (ANCHOR_CONTEXT, ANCHOR_ROUTE, ANCHOR_CELL, ANCHOR_FOOTPRINT)
    if any(source.count(anchor) != 1 for anchor in anchors):
        raise ValueError("Fail-closed exact PathfindBatchJob surface-memo anchors required")
    if "SurfaceFootprintMemo" in source or "WARLINE_SOURCE_SURFACE_MEMO" in source:
        raise ValueError("Surface memo instrumentation already present")
    if source.count("[BurstCompile]") != 1:
        raise ValueError("Fail-closed single PathfindBatchJob Burst marker required")
    source = source.replace("using Unity.Burst;", "using System.Collections.Generic;", 1)
    source = source.replace("    [BurstCompile]\n    internal struct PathfindBatchJob", "    internal struct PathfindBatchJob", 1)
    init = ANCHOR_CONTEXT.replace("FactionId = Factions[index]\n                };", '''FactionId = Factions[index],
                    SurfaceCellMemo = System.Environment.GetEnvironmentVariable("WARLINE_SOURCE_SURFACE_MEMO") == "1" ? new Dictionary<int2,bool>() : null,
                    SurfaceFootprintMemo = System.Environment.GetEnvironmentVariable("WARLINE_SOURCE_SURFACE_MEMO") == "1" ? new Dictionary<int2,bool>() : null,
                    ShadowVerifySurfaceMemo = System.Environment.GetEnvironmentVariable("WARLINE_SOURCE_SURFACE_MEMO_SHADOW") == "1"
                };''')
    source = source.replace(ANCHOR_CONTEXT, init, 1)
    route = '''                try
                {
                    GridPathRoutingKernel.Route(ref context, request, ref state);
                }
                finally
                {
                    // These exact-input caches live for one Execute/request only.
                    context.SurfaceCellMemo?.Clear();
                    context.SurfaceFootprintMemo?.Clear();
                }'''
    source = source.replace(ANCHOR_ROUTE, route, 1)
    source = source.replace(ANCHOR_CELL, "", 1).replace(ANCHOR_FOOTPRINT, "", 1)
    source = source.replace("            public bool IsWalkable(int index) => Job.Walkable[index].Value != 0;",
                            "            public bool IsWalkable(int index) => Job.Walkable[index].Value != 0;\n" + CELL_IMPL, 1)
    return source


def generate(repo: Path, output: Path) -> dict:
    root = repo.resolve(strict=True)
    source_path = root / SOURCE
    original = source_path.read_bytes()
    candidate = transform(original.decode("utf-8")).encode("utf-8")
    if source_path.read_bytes() != original:
        raise ValueError("Pinned PathfindBatchJob source changed during generation")
    snapshot_pins = {}
    for relative in SNAPSHOT_SOURCES:
        data = (root / relative).read_bytes()
        snapshot_pins[str(relative)] = hashlib.sha256(data).hexdigest()
    output.mkdir(parents=True, exist_ok=False)
    name = "PathfindBatchJob.SurfaceMemo.SourceHost.cs"
    (output / name).write_bytes(candidate)
    receipt = {
        "kind": "experimental-source-host-only-surface-memo",
        "source": str(SOURCE),
        "source_sha256": hashlib.sha256(original).hexdigest(),
        "candidate": name,
        "candidate_sha256": hashlib.sha256(candidate).hexdigest(),
        "output_identity": "sha256(candidate bytes)",
        "opt_in": "WARLINE_SOURCE_SURFACE_MEMO=1",
        "shadow_opt_in": "WARLINE_SOURCE_SURFACE_MEMO_SHADOW=1",
        "capacity_per_cache": 4096,
        "scope": "one synchronous PathfindBatchJob Execute/request; surface predicates only; exact int2 keys; cache saturation bypasses",
        "shadow": "each hit recomputes the original MapSurfaceTraversalValidation result; mismatch clears both caches and throws; Execute finally clears caches",
        "snapshot_lifetime_evidence": {
            "claim": "Scheduler copies the surface context into PathfindBatchJob; map surface fields are read-only job inputs; both memo dictionaries are scoped to one Execute/request and cleared in finally.",
            "source_pins": snapshot_pins,
        },
        "dynamic_placement_touched": False,
        "route_algorithm_changed": False,
        "burst_compile_attribute_in_candidate": False,
        "build_inputs_changed": False,
        "mission_run": False,
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt


def prepare_manifest(repo: Path, resume_manifest: Path, candidate_relative: str, destination: Path, receipt: dict) -> dict:
    root = repo.resolve(strict=True)
    input_path = (root / resume_manifest).resolve(strict=True)
    original_bytes = input_path.read_bytes()
    data = json.loads(original_bytes)
    profile = data.get("profile", {})
    lineage = profile.get("qualification_lineage", {})
    if data.get("runtime_backend") != "TypedGroundS026V1" or lineage.get("mission") != "S026" or lineage.get("seed") != 104755:
        raise ValueError("Pinned S026 resume receipt overlay manifest required")
    rows = [row for row in data.get("sources", []) if row.get("id") == "130_PathfindBatchJob.cs"]
    if len(rows) != 1 or rows[0].get("path") != str(SOURCE):
        raise ValueError("Exact current PathfindBatchJob native-direct row required")
    candidate_path = Path(candidate_relative)
    if candidate_path.is_absolute() or ".." in candidate_path.parts or "\\" in candidate_relative:
        raise ValueError("Repository-relative candidate path required")
    candidate_file = (root / candidate_path).resolve()
    if not candidate_file.is_relative_to(root):
        raise ValueError("Candidate path escapes repository root")
    rows[0]["path"] = candidate_path.as_posix()
    data["experimental_only"] = True
    data["scope"] = "S026 resume receipt overlay plus isolated source-host-only surface memo candidate; compilation only; no mission run or readiness promotion."
    data["source_host_surface_memo_overlay"] = {
        "kind": "S026PathfindingStaticSurfaceRequestMemo",
        "base_manifest": str(resume_manifest),
        "base_manifest_sha256": hashlib.sha256(original_bytes).hexdigest(),
        "replaced_source_row": "130_PathfindBatchJob.cs",
        "candidate_path": candidate_path.as_posix(),
        "candidate_sha256": receipt["candidate_sha256"],
        "surface_snapshot_source_pins": receipt["snapshot_lifetime_evidence"]["source_pins"],
        "dynamic_placement_touched": False,
        "burst_compile_attribute_in_candidate": False,
        "mission_run": False,
        "mission_ready": False,
    }
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(data, indent=2) + "\n")
    if input_path.read_bytes() != original_bytes:
        raise ValueError("Pinned S026 resume manifest changed during preparation")
    manifest_bytes = destination.read_bytes()
    return {
        "path": str(destination.resolve()),
        "sha256": hashlib.sha256(manifest_bytes).hexdigest(),
        "bytes": len(manifest_bytes),
        "base_manifest_sha256": hashlib.sha256(original_bytes).hexdigest(),
    }


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--resume-manifest", type=Path, default=DEFAULT_RESUME_MANIFEST)
    args = parser.parse_args()
    receipt = generate(args.repo, args.output)
    candidate_relative = (args.output.resolve().relative_to(args.repo.resolve()) / receipt["candidate"]).as_posix()
    manifest = prepare_manifest(args.repo, args.resume_manifest, candidate_relative,
                                args.output / "source-manifest.json", receipt)
    receipt["prepared_manifest"] = manifest
    (args.output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps(receipt, indent=2))
