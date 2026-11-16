#!/usr/bin/env python3
"""Opt-in S004 LayoutValues JSON serializer, scoped to the captured schema.

This source-host diagnostic is deliberately limited to the exact authored S004
layout and two captured synthetic cases. It does not claim general JsonUtility
compatibility.
"""
import argparse
import hashlib
import json
import re
import struct
from pathlib import Path


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(ok, message):
    if not ok:
        raise ValueError(message)


def f32(value):
    # JsonUtility serializes a C# float as its exact binary32 value promoted to
    # the JSON writer's number representation; normalize signed zero to +0.
    result = struct.unpack("!f", struct.pack("!f", float(value)))[0]
    return 0.0 if result == 0.0 else result


def ordered(obj):
    return json.dumps(obj, ensure_ascii=False, separators=(",", ":"))


def extract(log):
    text = Path(log).read_text(encoding="utf-8", errors="strict")
    need("[S004OriginalLayoutNativeJson] result=Passed compilerCapture=Completed gameplay=NotRun world=NotCreated authoredInputsUnchanged=1 assemblyUnchanged=1" in text,
         "native capture final pass marker missing")
    need(re.search(r"^Batch mode:\s+NO$", text, re.M) is not None, "unexpected Unity execution mode in evidence")
    rows = {}
    for line in text.splitlines():
        marker = "[S004OriginalLayoutNativeJson] "
        if marker not in line:
            continue
        line = line[line.index(marker) + len(marker):]
        m = re.search(r"(?:^| )json=(\{.*\})$", line)
        if m:
            key = "authored" if line.startswith("authored asset=") else re.search(r"synthetic=([^ ]+)", line).group(1)
            rows[key] = m.group(1)
    need(set(rows) == {"authored", "null-empty", "signed-zero-floats-enums-nested-arrays"},
         "native authored and both synthetic JSON cases required")
    return {key: value for key, value in rows.items()}


def layout_graph(graph):
    nodes = [node for node in graph["nodes"] if node["source_type"] == "SkirmishMapLayoutConfig" and
             node["source_path"] == "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishLayout_S004.asset"]
    need(len(nodes) == 1, "exactly one authored S004 layout node required")
    fields = nodes[0]["fields"]
    required = {"layoutId", "operationMapId", "contentVersion", "contentHash", "worldWidthMetres",
                "worldDepthMetres", "originX", "originZ", "cellSize", "anchors", "routes", "pads", "worldBinding"}
    need(set(fields) == required, "unvalidated authored layout field schema")
    return fields


def serialize(layout, base, case):
    # Schema and serialized field order come from the native LayoutValues capture.
    base = json.loads(base)
    frame = layout["worldBinding"]
    frame_obj = {
        "OriginX": f32(frame["OriginX"]), "OriginZ": f32(frame["OriginZ"]),
        "ForwardX": f32(frame["ForwardX"]), "ForwardZ": f32(frame["ForwardZ"]),
        "AcrossX": f32(frame["AcrossX"]), "AcrossZ": f32(frame["AcrossZ"]),
        "ForwardMetres": f32(frame["ForwardMetres"]), "AcrossMetres": f32(frame["AcrossMetres"]),
        "PlayerAnchorId": frame["PlayerAnchorId"], "EnemyAnchorId": frame["EnemyAnchorId"],
        "SourceHash": frame["SourceHash"], "HasMeasuredDeployments": frame.get("HasMeasuredDeployments", False),
        "PlayerDeployment": {"x": f32(frame.get("PlayerDeployment", {}).get("x", 0)),
                             "y": f32(frame.get("PlayerDeployment", {}).get("y", 0))},
        "EnemyDeployment": {"x": f32(frame.get("EnemyDeployment", {}).get("x", 0)),
                            "y": f32(frame.get("EnemyDeployment", {}).get("y", 0))},
        "BindAllConfigurations": frame.get("BindAllConfigurations", False)}
    result = {"Id": layout["layoutId"], "OperationMapId": layout["operationMapId"],
              "ContentHash": layout["contentHash"], "Version": layout["contentVersion"],
              "Width": f32(layout["worldWidthMetres"]), "Depth": f32(layout["worldDepthMetres"]),
              "OriginX": f32(layout["originX"]), "OriginZ": f32(layout["originZ"]),
              "CellSize": f32(layout["cellSize"]), "Frame": frame_obj}
    anchors = layout["anchors"]
    routes = layout["routes"]
    pads = layout["pads"]
    if case == "null-empty":
        anchors, routes, pads = [], [], []
    elif case == "signed-zero-floats-enums-nested-arrays":
        result["OriginX"] = result["OriginZ"] = 0.0
        anchors = [{"AnchorId": "synthetic", "RoleId": "synthetic.role", "NormalizedU": 0.0,
                    "NormalizedV": f32(0.125), "WorldX": 0.0, "WorldZ": f32(1.25)}]
        routes = [{"RouteId": "synthetic.route", "WaypointAnchorIds": ["a", "b"], "Kind": 2,
                   "LengthMetres": f32(12.5), "InfantryTransitSeconds": 0.0, "GroundTransitSeconds": 0.0,
                   "AirTransitSeconds": 0.0, "InfantryFirstContactSeconds": 0.0, "GroundFirstContactSeconds": 0.0,
                   "ProvisionalTimes": True, "WidthMetres": 0.0, "AllowsVehicles": False, "AllowsAircraft": False}]
        pads = [{"PadId": "synthetic.pad", "AnchorId": "synthetic", "RoleId": "synthetic.role", "Kind": 2,
                 "CenterX": f32(2.5), "CenterZ": 0.0, "WidthMetres": f32(3), "DepthMetres": f32(4), "FactionId": 2}]
    def normalized(source, key_fields, float_fields):
        result_arr = []
        for row in source:
            item = {}
            for key in key_fields:
                value = row[key]
                item[key] = f32(value) if key in float_fields else value
            result_arr.append(item)
        return result_arr
    result["Anchors"] = normalized(anchors, ["AnchorId", "RoleId", "NormalizedU", "NormalizedV", "WorldX", "WorldZ"],
                                   {"NormalizedU", "NormalizedV", "WorldX", "WorldZ"})
    route_keys = ["RouteId", "WaypointAnchorIds", "Kind", "LengthMetres", "InfantryTransitSeconds", "GroundTransitSeconds",
                  "AirTransitSeconds", "InfantryFirstContactSeconds", "GroundFirstContactSeconds", "ProvisionalTimes",
                  "WidthMetres", "AllowsVehicles", "AllowsAircraft"]
    result["Routes"] = normalized(routes, route_keys, set(route_keys) - {"RouteId", "WaypointAnchorIds", "Kind", "ProvisionalTimes", "AllowsVehicles", "AllowsAircraft"})
    pad_keys = ["PadId", "AnchorId", "RoleId", "Kind", "CenterX", "CenterZ", "WidthMetres", "DepthMetres", "FactionId"]
    result["Pads"] = normalized(pads, pad_keys, {"CenterX", "CenterZ", "WidthMetres", "DepthMetres"})
    need(list(result) == list(base), "native LayoutValues top-level schema/order drift")
    need(list(frame_obj) == list(base["Frame"]), "native nested frame schema/order drift")
    for key in ("Anchors", "Routes", "Pads"):
        need(not result[key] or list(result[key][0]) == list(base[key][0]), "native array element schema/order drift: " + key)
    return ordered(result)


def run(args):
    need(args.wrapper_exit == 0, "explicit successful wrapper exit code required")
    graph_path = Path(args.graph).resolve()
    need(sha(graph_path) == args.graph_sha256, "explicit source graph hash mismatch")
    graph = json.loads(graph_path.read_text(encoding="utf-8"))
    need(graph.get("authority") == "AuthoredConfigAssets" and graph.get("compiled_setup") is False and
         graph.get("world_initialized") is False and graph.get("native_world_state_used") is False,
         "exact authored uncompiled source graph required")
    for path, digest in graph["source_pins"].items():
        need(sha(path) == digest, "authored graph source pin changed: " + path)
    native = extract(args.native_log)
    native_values = {name: json.loads(value) for name, value in native.items()}
    expected = {"authored": serialize(layout_graph(graph), native["authored"], "authored"),
                "null-empty": serialize(layout_graph(graph), native["authored"], "null-empty"),
                "signed-zero-floats-enums-nested-arrays": serialize(layout_graph(graph), native["authored"], "signed-zero-floats-enums-nested-arrays")}
    # Keep failed candidates and their exact first byte difference. No numeric
    # normalization is permitted because the acceptance condition is byte parity.
    comparisons = {}
    for name, candidate in expected.items():
        observed = native[name]
        mismatch = next((i for i, (a, b) in enumerate(zip(candidate, observed)) if a != b), None)
        if mismatch is None and len(candidate) != len(observed):
            mismatch = min(len(candidate), len(observed))
        comparisons[name] = {"byte_parity": candidate == observed,
                             "candidate_sha256": hashlib.sha256(candidate.encode()).hexdigest(),
                             "observed_sha256": hashlib.sha256(observed.encode()).hexdigest(),
                             "first_difference_offset": mismatch,
                             "candidate_context": candidate[max(0, mismatch - 40):mismatch + 80] if mismatch is not None else None,
                             "observed_context": observed[max(0, mismatch - 40):mismatch + 80] if mismatch is not None else None}
    for path, digest in graph["source_pins"].items():
        need(sha(path) == digest, "authored graph source changed during serialization: " + path)
    out = Path(args.output)
    out.mkdir(parents=True, exist_ok=False)
    for key, value in expected.items():
        (out / (key + ".candidate.json")).write_text(value + "\n", encoding="utf-8")
        (out / (key + ".native.json")).write_text(native[key] + "\n", encoding="utf-8")
    status = "Passed" if all(item["byte_parity"] for item in comparisons.values()) else "Failed"
    receipt = {"kind": "S004LayoutValuesSourceSerializerReceipt", "status": status,
               "scope": "Exact captured S004 LayoutValues graph/schema only; no general JsonUtility compatibility claim",
               "serializer": {"path": str(Path(__file__).resolve()), "sha256": sha(__file__)},
               "graph": {"path": str(graph_path), "sha256": args.graph_sha256},
               "native_log": {"path": str(Path(args.native_log).resolve()), "sha256": sha(args.native_log)},
               "wrapper_exit": args.wrapper_exit, "cases": comparisons,
               "excluded": ["ScriptableObject/general Unity object semantics", "unsupported schemas or source pins",
                            "other Unity versions/builds", "gameplay/world/readiness validation"]}
    need(args.wrapper_exit == 0, "explicit successful wrapper exit code required")
    (out / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"status": status, "cases": len(expected), "scope": receipt["scope"]}))
    return status == "Passed"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--graph", required=True)
    parser.add_argument("--graph-sha256", required=True)
    parser.add_argument("--native-log", required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    try:
        if not run(args):
            raise SystemExit(1)
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError, struct.error) as error:
        raise SystemExit("S004 source JSON serializer failed closed: " + str(error))


if __name__ == "__main__":
    main()
