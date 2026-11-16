"""Strict current-epoch candidate checks for source-only S004 diagnostics."""
import hashlib
import json
from pathlib import Path

import build_s004_current_role_catalog_metadata as roles
import build_s004_current_airport_source_definition as airports

ROLE_SHA = "dac6d0dd5e2b836f4a41f649467815f6fdd1983f95c79fae76e76a884d16cf5a"
AIRPORT_SHA = "2cd2e72caf81fcfc827267954e6edddef2331dadd7e5915ef7a219ba23650fb9"


def verify(path, expected, kind):
    path = Path(path)
    actual_path = roles.ROOT / ("T Logs/SkirmishProduction/s004-current-role-catalog-20261105-T2.json"
                                if kind == "role" else "T Logs/SkirmishProduction/s004-current-airport-definition-20261105-T2.json")
    fixed = ROLE_SHA if kind == "role" else AIRPORT_SHA
    if not path.is_absolute() or path.is_symlink() or path != actual_path or expected != fixed:
        raise ValueError("Exact current source-only candidate required")
    raw = path.read_bytes()
    if hashlib.sha256(raw).hexdigest() != fixed:
        raise ValueError("Current source-only candidate hash changed")
    actual = json.loads(raw)
    rebuilt = roles.build() if kind == "role" else airports.build()
    if json.dumps(actual, sort_keys=True, separators=(",", ":")) != json.dumps(rebuilt, sort_keys=True, separators=(",", ":")):
        raise ValueError("Current source-only candidate join changed")
    return actual
