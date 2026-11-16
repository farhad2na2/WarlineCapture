#!/usr/bin/env python3
"""Read the S004 attack helicopter's serialized variant chain without Unity.

This is source inventory, not a resolved Unity hierarchy or transport template.
No imported mesh bounds, animation writers, or runtime pose are inferred.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
PATHS = [
    "Assets/Game/Prefabs/Characters/Unit.prefab",
    "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab",
    "Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Attack_Small.prefab",
]
GUIDS = ["5680e7910ed3741beba6f07116b6b03c", "9ce6998c034284d98a89b5badc6e2f24", "fcc8f80798992470f8b8769d3bc4c41a"]


def one(pattern, text, label):
    found = re.findall(pattern, text, re.MULTILINE)
    if len(found) != 1:
        raise ValueError(label + ": expected exactly one")
    return found[0]


def inventory(root):
    raw = [(root / path).read_bytes() for path in PATHS]
    data = [item.decode("utf-8-sig").replace("\r\n", "\n") for item in raw]
    pins = {}
    for path, contents, guid in zip(PATHS, raw, GUIDS):
        meta = (root / (path + ".meta")).read_bytes()
        if one(r"^guid: ([0-9a-f]{32})$", meta.decode("utf-8-sig"), path + " GUID") != guid:
            raise ValueError("Prefab GUID changed: " + path)
        pins[path] = hashlib.sha256(contents).hexdigest()
        pins[path + ".meta"] = hashlib.sha256(meta).hexdigest()
    for index in (1, 2):
        expected = GUIDS[index - 1]
        actual = one(r"^  m_SourcePrefab: \{fileID: 100100000, guid: ([0-9a-f]{32}), type: 3\}$", data[index], "source prefab")
        if actual != expected:
            raise ValueError("Unexpected prefab inheritance")
    root_transform = int(one(r"^--- !u!4 &(\d+)\nTransform:\n(?:(?!^--- )[\s\S])*?^  m_Father: \{fileID: 0\}$", data[0], "root transform"))
    vehicle_instance = int(one(r"^--- !u!1001 &(\d+)$", data[1], "vehicle instance"))
    inherited_root = root_transform ^ vehicle_instance
    helicopter_parent = int(one(r"^--- !u!4 &(\d+) stripped\nTransform:\n  m_CorrespondingSourceObject: \{fileID: " + str(inherited_root) + r", guid: " + GUIDS[1] + r", type: 3\}", data[2], "variant root bridge"))
    added = int(one(r"^      addedObject: \{fileID: (\d+)\}$", data[2], "added model root"))
    headers = list(re.finditer(r"^--- !u!(\d+) &(\d+)(?: stripped)?$", data[2], re.MULTILINE))
    objects = {}
    transforms = {}
    for i, header in enumerate(headers):
        kind, ident = int(header.group(1)), int(header.group(2))
        body = data[2][header.end():headers[i + 1].start() if i + 1 < len(headers) else len(data[2])]
        if kind == 1:
            name = one(r"^  m_Name: (.+)$", body, "game object name")
            active = int(one(r"^  m_IsActive: ([01])$", body, "game object active"))
            objects[ident] = {"name": name, "active": bool(active)}
        elif kind == 4 and " stripped" not in header.group(0):
            def vector(field):
                return one(r"^  " + field + r": \{([^}]+)\}$", body, field)
            owner = int(one(r"^  m_GameObject: \{fileID: (\d+)\}$", body, "transform owner"))
            transforms[ident] = {"owner_file_id": owner,
                "parent_file_id": int(one(r"^  m_Father: \{fileID: (\d+)\}$", body, "transform parent")),
                "local_position_serialized": vector("m_LocalPosition"),
                "local_rotation_serialized": vector("m_LocalRotation"),
                "local_scale_serialized": vector("m_LocalScale")}
    if added not in transforms or transforms[added]["parent_file_id"] != helicopter_parent:
        raise ValueError("Added hierarchy is not linked to the variant root")
    for ident, row in transforms.items():
        if row["owner_file_id"] not in objects or (row["parent_file_id"] != helicopter_parent and row["parent_file_id"] not in transforms):
            raise ValueError("Incomplete added transform inventory")
        row.update(objects[row["owner_file_id"]])
        row["transform_file_id"] = ident
    rotor_rows = [v for v in transforms.values() if v["name"].endswith(("_X", "_Y", "_Z"))]
    if sorted(v["name"] for v in rotor_rows) != ["Blades_Back_X", "Blades_Main_Y"]:
        raise ValueError("Unexpected authored added rotor inventory")
    return {"status": "Collected", "scope": "SerializedPrefabVariantInventory_NotResolvedUnityHierarchy",
        "prefab_asset": PATHS[2], "prefab_guid": GUIDS[2], "source_pins": pins,
        "base_root_transform_file_id": root_transform, "vehicle_instance_file_id": vehicle_instance,
        "derived_vehicle_root_file_id": inherited_root, "attack_stripped_root_file_id": helicopter_parent,
        "added_model_transform_file_id": added, "added_transforms": list(transforms.values()),
        "added_rotor_names": sorted(v["name"] for v in rotor_rows),
        "inherited_removals_or_overrides_resolved": False, "mesh_bounds_captured": False,
        "animation_writer_domain_proven": False, "runtime_template_admitted": False,
        "native_parity": False, "mission_ready": False}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    report = inventory(ROOT)
    args.output.parent.mkdir(parents=True, exist_ok=False)
    args.output.write_text(json.dumps(report, indent=2) + "\n")
    print("[S004AirSelfAuthoredTransformChain] result=Collected runtimeTemplate=0")
