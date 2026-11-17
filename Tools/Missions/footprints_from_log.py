#!/usr/bin/env python3
"""Extract configured building footprints from a complete Skirmish trace."""
import json
import re
import sys
from decimal import Decimal, InvalidOperation
from pathlib import Path

PREFIX = "[SkirmishFootprintTrace] "
ROW = re.compile(r"structure=(\S+) width=(\S+) depth=(\S+) rotatable=([01]) source=(\S+)$")
DONE = re.compile(r"result=Completed count=(\d+)$")


def extract(lines):
    values = {}
    batch_count = 0
    completed = False
    for line in lines:
        if PREFIX not in line:
            continue
        record = line.split(PREFIX, 1)[1].strip()
        row = ROW.fullmatch(record)
        done = DONE.fullmatch(record)
        if row:
            structure, width, depth, rotatable, source = row.groups()
            try:
                dimensions = (Decimal(width), Decimal(depth))
            except InvalidOperation as exc:
                raise ValueError(f"invalid dimensions for {structure}") from exc
            if any(not n.is_finite() or n <= 0 for n in dimensions):
                raise ValueError(f"invalid dimensions for {structure}")
            value = (dimensions, rotatable, source)
            if structure in values and values[structure] != value:
                raise ValueError(f"conflicting trace lines for {structure}")
            values[structure] = value
            batch_count += 1
        elif done:
            if batch_count != int(done.group(1)):
                raise ValueError("completed count does not match trace lines")
            batch_count = 0
            completed = True
        else:
            raise ValueError(f"invalid footprint trace: {record}")
    if not completed or batch_count:
        raise ValueError("completed footprint trace is missing or incomplete")
    return {key: [float(v) for v in values[key][0]] for key in sorted(values)}


# SkirmishStructureIds.VisualKey (SkirmishRosterContracts.cs:286-303): these ids share the model of the measured id.
ALIASES = {
    "building.refinery.module": "building.refinery",
    "building.watchtower.approach": "building.watchtower",
    "building.watchtower.defense_approach_a": "building.watchtower",
    "building.watchtower.defense_approach_b": "building.watchtower",
}


def with_aliases(footprints):
    result = dict(footprints)
    for alias, source in ALIASES.items():
        if alias not in result and source in result:
            result[alias] = list(result[source])
    return {key: result[key] for key in sorted(result)}


def main():
    if len(sys.argv) != 2:
        print(f"usage: {sys.argv[0]} <unity.log>", file=sys.stderr)
        return 1
    try:
        with Path(sys.argv[1]).open(encoding="utf-8", errors="replace") as log:
            footprints = with_aliases(extract(log))
        print(json.dumps(footprints, indent=2, sort_keys=True, allow_nan=False))
    except (OSError, ValueError, OverflowError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
