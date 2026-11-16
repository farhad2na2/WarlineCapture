"""Extract the current faction-control owner with only external scheduling attributes removed."""
from pathlib import Path
import hashlib


SOURCE = "Assets/Game/Scripts/Systems/AIFactionControlSystem.cs"
ATTRIBUTES = (
    "    [UpdateAfter(typeof(AIEconomySystem))]\n",
    "    [UpdateBefore(typeof(UnitPathfindingSystem))]\n",
)
BUFFER_BINDING = (
    "SystemAPI.GetSingletonBuffer<FactionControlEntry>()",
    "state.EntityManager.GetBuffer<FactionControlEntry>(SystemAPI.GetSingletonEntity<FactionControlConfigTag>())",
)


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def generate(repo, output):
    source_path = Path(repo) / SOURCE
    original = source_path.read_bytes()
    text = original.decode("utf-8").replace("\r\n", "\n")
    if text.count("public partial struct AIFactionControlSystem : ISystem") != 1:
        raise ValueError("AIFactionControlSystem declaration changed")
    if text.count("private partial struct ApplyFactionControlJob : IJobEntity") != 1:
        raise ValueError("ApplyFactionControlJob declaration changed")
    removed = []
    for attribute in ATTRIBUTES:
        count = text.count(attribute)
        if count != 1:
            raise ValueError("Expected exactly one scheduling attribute: " + attribute.strip())
        text = text.replace(attribute, "", 1)
        removed.append({"text": attribute.strip(), "count": count})
    old, new = BUFFER_BINDING
    if text.count(old) != 1:
        raise ValueError("Expected exactly one singleton faction-control buffer read")
    text = text.replace(old, new, 1)
    if source_path.read_bytes() != original:
        raise ValueError("AIFactionControlSystem source changed during extraction")
    out = Path(output)
    out.mkdir(parents=True, exist_ok=True)
    destination = out / "AIFactionControlSystem.s073-control.cs"
    with destination.open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(text)
    return {
        "kind": "S073AIFactionControlCurrentSourceExtraction",
        "source_pins": {str(source_path.resolve()): sha(original)},
        "source_sha256": sha(original),
        "extracted_sha256": sha(text.encode("utf-8")),
        "removed_external_scheduling_attributes": removed,
        "explicit_tagged_buffer_binding": {"from": old, "to": new, "count": 1},
        "scope": "Exact current original system and job bodies; external ordering attributes are omitted and the singleton buffer access is bound to the required tag entity for the isolated owner control.",
    }
