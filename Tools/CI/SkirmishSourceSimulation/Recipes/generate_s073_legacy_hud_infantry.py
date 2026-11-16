"""Extract the original legacy HUD infantry query for the source host."""
import hashlib
from pathlib import Path

SOURCE = "Assets/Game/Scripts/UI/Shell/Ecs/UiShellEcsGateway.Skirmish.cs"
START = "using(var units=em.CreateEntityQuery(typeof(SkirmishSquadMember),typeof(UnitHealth)))"
EXPECTED = "foreach(var unit in entities)if(em.GetComponentData<SkirmishSquadMember>(unit).Slot<4&&em.GetComponentData<UnitHealth>(unit).Current>0)infantry++;"


def extract(text):
    if text.count(START) != 1:
        raise ValueError("Unique original legacy infantry query required")
    start = text.index(START)
    opening = text.index("{", start)
    depth = 0
    for index in range(opening, len(text)):
        depth += (text[index] == "{") - (text[index] == "}")
        if depth == 0:
            body = text[start:index + 1]
            if body.count(EXPECTED) != 1:
                raise ValueError("Exact original legacy infantry count required")
            return body
    raise ValueError("Unclosed original legacy infantry query")


def generate(repo, output):
    repo, output = Path(repo), Path(output)
    source = repo / SOURCE
    raw = source.read_bytes()
    body = extract(raw.decode("utf-8"))
    output.mkdir(parents=True, exist_ok=True)
    generated = ("using Unity.Entities;using Game.Components;namespace Game.Runtime {"
                 "internal static class SourceS073LegacyHudInfantry {internal static int Count(EntityManager em){"
                 "int infantry=0;\n" + body + "\nreturn infantry;}}}\n")
    (output / "SourceS073LegacyHudInfantry.cs").write_text(generated)
    if source.read_bytes() != raw:
        raise ValueError("Legacy HUD source changed during extraction")
    return {"source_inputs": {str(source): hashlib.sha256(raw).hexdigest()},
            "scope": "Original legacy HUD infantry query only; no native UI or readiness claim"}
