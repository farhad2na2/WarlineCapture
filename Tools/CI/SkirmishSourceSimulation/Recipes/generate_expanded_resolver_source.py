#!/usr/bin/env python3
"""Extract the original expanded resolver with an explicitly absent visual registry."""
import hashlib
from pathlib import Path

SOURCE = 'Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchResolver.cs'
OUTPUT = 'SourceExpandedLaunchResolver.cs'

def generate(repo, output):
    repo, output = Path(repo).resolve(strict=True), Path(output)
    source = repo / SOURCE
    raw = source.read_bytes()
    text = raw.decode('utf-8').replace('\r\n', '\n')
    original = 'UnitPrefabRegistryAuthoringConfig registry = null,'
    if text.count(original) != 1:
        raise ValueError('Original resolver registry signature changed')
    text = text.replace(original, 'object registry = null,', 1)
    anchor = '            setup = null;\n            payload = null;'
    if text.count(anchor) != 1:
        raise ValueError('Original resolver body anchor changed')
    text = text.replace(anchor, '            if (registry != null) throw new System.NotSupportedException("Source-only resolver requires an absent managed visual registry");\n' + anchor, 1)
    if source.read_bytes() != raw:
        raise ValueError('Original resolver changed during extraction')
    output.mkdir(parents=True, exist_ok=False)
    data = text.encode('utf-8')
    (output / OUTPUT).write_bytes(data)
    return {'source_pins': {SOURCE: hashlib.sha256(raw).hexdigest()},
            'outputs': {OUTPUT: hashlib.sha256(data).hexdigest()},
            'scope': 'Original resolver body with absent managed visual registry; no native input or readiness',
            'native_input_claimed': False, 'mission_ready': False}
