"""Project the original ECS-backed resource-production owner into a bounded probe.

The projection preserves native member bodies verbatim. It is source evidence,
not a substitute owner and does not execute Unity or admit S073 readiness.
"""
from pathlib import Path
import hashlib
import json
import re


OWNER = "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.cs"
SYSTEM = "Assets/Game/Scripts/Systems/BuildingResourceProductionEcsSystem.cs"
HELPER = "Assets/Game/Scripts/Systems/BuildingResourceProductionSystemHelper.cs"
KERNEL = "Assets/Game/Scripts/Simulation/BuildingResourceProductionPrecisionKernel.cs"
OUTPUTS = ("FactionResourceProductionOwner.members.cs.txt", "BuildingResourceProductionEcsSystem.cs.txt",
           "BuildingResourceProductionSystemHelper.cs.txt", "BuildingResourceProductionPrecisionKernel.cs.txt")
MEMBERS = (
    (OWNER, "UpdateResourceProduction", 1),
    (OWNER, "UpdateRuntimeBuildingResourceProduction", 0),
    (OWNER, "TryGetEntityResourceStorage", 0),
    (OWNER, "CommitEntityResourceStorage", 0),
    (OWNER, "SyncResourceStorageMetadata", 0),
    (OWNER, "SyncRuntimeBuildingsFromEcsStorage", 0),
    (OWNER, "SyncRuntimeBuildingFromEcsStorage", 0),
)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def member(text, name, occurrence=0):
    hits = list(re.finditer(
        r"\b(?:public|internal|private|protected)\s+(?:(?:static|virtual|override|async)\s+)*"
        r"[\w.<>,?\[\]]+\s+" + re.escape(name) + r"(?:\s*<[^>{}]+>)?\s*\(", text))
    if len(hits) <= occurrence:
        raise ValueError("Native owner member missing: " + name)
    pos = hits[occurrence].start()
    brace = text.find("{", hits[occurrence].end())
    if brace < 0:
        raise ValueError("Native owner body missing: " + name)
    depth = 0
    in_string = False
    escaped = False
    for i in range(brace, len(text)):
        c = text[i]
        if in_string:
            if escaped: escaped = False
            elif c == "\\": escaped = True
            elif c == '"': in_string = False
            continue
        if c == '"': in_string = True
        elif c == "{": depth += 1
        elif c == "}":
            depth -= 1
            if depth == 0:
                return text[pos:i + 1]
    raise ValueError("Unclosed native owner member: " + name)


def generate(repo, output):
    root, out = Path(repo), Path(output)
    paths = [root / OWNER, root / SYSTEM, root / HELPER, root / KERNEL]
    raw = [p.read_bytes() for p in paths]
    if any(len(b) > 1024 * 1024 for b in raw):
        raise ValueError("Resource-production source exceeds 1 MiB bound")
    texts = [b.decode("utf-8").replace("\r\n", "\n") for b in raw]
    selected = []
    for path, name, occurrence in MEMBERS:
        source_index = (OWNER, SYSTEM, HELPER, KERNEL).index(path)
        selected.append(member(texts[source_index], name, occurrence))
    # Fail closed on changed owner-call topology. These are the seams this probe
    # is meant to preserve: query-owned batch path, profile branch, and fallback.
    owner_call = selected[0]
    for token in ("_storageQueryCache.Get(entityManager)",
                  "BuildingResourceProductionEcsSystem.ApplyStorageQuery(",
                  "SyncRuntimeBuildingsFromEcsStorage(entityManager, buildings)",
                  "UpdateRuntimeBuildingResourceProduction("):
        if token not in owner_call:
            raise ValueError("Native resource owner call changed: " + token)
    if "BuildingResourceProductionProfileComponent" not in texts[1]:
        raise ValueError("Profile-backed native production branch missing")
    if out.exists() and any((out / name).exists() for name in OUTPUTS):
        raise FileExistsError("Probe output already exists")
    out.mkdir(parents=True, exist_ok=True)
    # These are evidence extracts, deliberately named .txt: the actual compile
    # is performed by the source-host alternate-manifest build below.
    owner = "\n\n".join(selected)
    (out / OUTPUTS[0]).write_text(owner)
    for name, content in zip(OUTPUTS[1:], texts[1:]):
        (out / name).write_text(content)
    if [p.read_bytes() for p in paths] != raw:
        raise ValueError("Native source changed during probe generation")
    return {
        "schema_version": 1,
        "kind": "S073OriginalFactionResourceProductionOwnerSourceProbe",
        "source_inputs": [{"path": str(p), "sha256": sha(b)} for p, b in zip(paths, raw)],
        "outputs": {n: sha((out / n).read_bytes()) for n in OUTPUTS},
        "native_members": ["FactionResourceCompositionSystemHelper." + name
                           for _, name, _ in MEMBERS],
        "scope": "Exact ECS resource production owner/query path plus original system, helper and precision kernel; no Unity run, startup claim or mission readiness",
        "calls_preserved": ["storage query cache", "ApplyStorageQuery", "runtime building fallback",
                            "profile metadata sync", "post-query runtime mirror sync"],
    }


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(generate(args.repo, args.output), indent=2))
