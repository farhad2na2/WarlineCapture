#!/usr/bin/env python3
"""Read finite legacy combat fields from the current S073 serialized preset.

This is source-data evidence only: it does not load a Unity object or establish
mission startup, readiness, or an outcome.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re

FLOAT_FIELDS = ("rifleRange", "rifleCooldown", "armoredCarRange", "armoredCarCooldown",
                "watchtowerRange", "watchtowerCooldown", "firstAttackSeconds", "baseDefenseRadius")
INT_FIELDS = ("rifleDamage", "armoredCarDamage", "watchtowerDamage")
VECTOR_FIELD = "playerReinforcementRallyOffset"


def read_source(path):
    raw = path.read_bytes()
    text = raw.decode("utf-8")

    def field(name):
        rows = re.findall(r"^  " + re.escape(name) + r": ([^\r\n]+)$", text, re.MULTILINE)
        if len(rows) != 1:
            raise ValueError("Expected exactly one serialized field: " + name)
        return rows[0]

    values = {}
    for name in FLOAT_FIELDS:
        value = float(field(name))
        if not math.isfinite(value):
            raise ValueError("Non-finite preset value: " + name)
        values[name] = value
    for name in INT_FIELDS:
        value = field(name)
        if not re.fullmatch(r"-?(?:0|[1-9][0-9]*)", value):
            raise ValueError("Non-integer preset value: " + name)
        values[name] = int(value)
    vector = re.fullmatch(r"\{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}", field(VECTOR_FIELD))
    if vector is None:
        raise ValueError("Expected exact serialized rally vector")
    values[VECTOR_FIELD] = [float(v) for v in vector.groups()]
    if not all(math.isfinite(v) for v in values[VECTOR_FIELD]):
        raise ValueError("Non-finite rally vector")
    return {"schema_version": 1, "kind": "S073LegacyCombatPresetSourceReadback",
            "scope": "SerializedSourceData_NotUnityReadback_NotMissionState",
            "source_asset": {"path": str(path.resolve()), "sha256": hashlib.sha256(raw).hexdigest()},
            "values": values, "world_created": False, "mission_ready": False}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preset", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = read_source(args.preset)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x", encoding="utf-8") as output:
        json.dump(result, output, indent=2, allow_nan=False)
        output.write("\n")
    print("[S073LegacyCombatPresetSourceReadback] result=Passed fields=12 world=0 missionReady=0")


if __name__ == "__main__":
    main()
