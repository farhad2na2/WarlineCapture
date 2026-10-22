#!/usr/bin/env python3
"""Offline source admission only. Does not launch Unity or admit a native run."""
import hashlib, json
from pathlib import Path
here = Path(__file__).resolve().parent
root = next(p for p in here.parents if (p / "Assets/Game/Scripts").is_dir())
manifest = json.loads((here / "source-manifest.json").read_text())
failures = []
for relative, expected in manifest["sourcePins"].items():
    path = root / relative
    if not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
        failures.append(relative)
print(json.dumps({"sourcePinsMatched": not failures, "mismatches": failures,
    "nativeRunAdmitted": False, "compilerExecuted": False,
    "next": "Independent compile/review, then separate fresh native admission; see capture-plan.md"}, indent=2))
raise SystemExit(bool(failures))
