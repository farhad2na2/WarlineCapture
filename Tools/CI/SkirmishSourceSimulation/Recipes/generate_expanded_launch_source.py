#!/usr/bin/env python3
"""Generate the original expanded launch owner with a null-only visual seam."""
import argparse
import hashlib
from pathlib import Path

SOURCE = "Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchProjection.cs"
OUTPUT = "SourceExpandedLaunchProjection.cs"


def once(text, before, after):
    if text.count(before) != 1:
        raise ValueError("Original expanded launch seam changed")
    return text.replace(before, after, 1)


def generate(repo, output):
    repo, output = Path(repo), Path(output)
    source = repo / SOURCE
    raw = source.read_bytes()
    text = raw.decode("utf-8").replace("\r\n", "\n")
    text = once(text, "UnitPrefabRegistryAuthoringConfig registry = null,", "object registry = null,")
    text = once(text,
        "        {\n            if (em == default || payload == null || setup == null || payload.IsLegacy)",
        "        {\n            if (registry != null) throw new NotSupportedException(\"Source-only launch requires an absent managed visual registry\");\n            if (em == default || payload == null || setup == null || payload.IsLegacy)")
    text = once(text, "            if (registry != null)\n                SkirmishVisualSpawnService.BindSceneRegistry(em, entity, registry);", "")
    if source.read_bytes() != raw:
        raise ValueError("Original expanded launch source changed during extraction")
    output.mkdir(parents=True, exist_ok=True)
    generated = output / OUTPUT
    with generated.open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(text)
    return {"source_pins": {SOURCE: hashlib.sha256(raw).hexdigest()},
            "outputs": {OUTPUT: hashlib.sha256(generated.read_bytes()).hexdigest()},
            "scope": "Original expanded launch body; managed visual registry must be null and visual binding is unsupported",
            "native_input_claimed": False, "mission_ready": False}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    generate(args.repo, args.output)
