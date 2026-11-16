#!/usr/bin/env python3
"""Generate an opt-in later window for the existing passive source path trace."""
import hashlib
from pathlib import Path

SOURCE = "Tools/CI/SkirmishSourceSimulation/Adapters/117_OriginalGroundOwners.cs"
OUTPUT = "SourceS003PathThresholdTrace.cs"
ANCHOR = "  const int PathTraceFirstTick=240,PathTraceLastTick=320,PathTraceCapacity=96;"
REPLACEMENT = '''  const int PathTraceCapacity=96;
  readonly int PathTraceFirstTick=Environment.GetEnvironmentVariable("WARLINE_SOURCE_S003_PATH_THRESHOLD_TRACE")=="1"?2150:240;
  readonly int PathTraceLastTick=Environment.GetEnvironmentVariable("WARLINE_SOURCE_S003_PATH_THRESHOLD_TRACE")=="1"?2245:320;'''


def transform(source):
    if source.count(ANCHOR) != 1:
        raise ValueError("Exact existing passive path-trace window required")
    return source.replace(ANCHOR, REPLACEMENT, 1)


def generate(repo, output):
    root = Path(repo).resolve(strict=True)
    path = root / SOURCE
    original = path.read_bytes()
    rendered = transform(original.decode("utf-8")).encode("utf-8")
    if path.read_bytes() != original:
        raise ValueError("Path owner adapter changed during generation")
    target = Path(output); target.mkdir(parents=True, exist_ok=False)
    (target / OUTPUT).write_bytes(rendered)
    return {"source_pins": {str(path): hashlib.sha256(original).hexdigest()},
            "outputs": {OUTPUT: hashlib.sha256(rendered).hexdigest()},
            "experimental_only": True,
            "scope": "Source-only passive pending-state window; no scheduling change"}
