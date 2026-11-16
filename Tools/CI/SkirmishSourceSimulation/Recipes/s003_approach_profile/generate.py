"""Generate a source-only timing probe around the original S003 approach owner."""
import hashlib
from pathlib import Path

SOURCE = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishPublicSelectedApproachProjection.cs"


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    raw = (repo / SOURCE).read_bytes()
    text = raw.decode("utf-8").replace("\r\n", "\n")
    marker = "        static SkirmishPublicSelectedApproach ReadS003OrdinaryCore("
    if text.count(marker) != 1:
        raise ValueError("Exact S003 approach owner required")
    prefix, body = text.split(marker, 1)
    anchors = [
        ("            var state = em.GetComponentData<SkirmishExpandedSessionComponent>(session);", "begin"),
        ("            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable),", "identity"),
        ("            var publishedObjective = SkirmishPublicObjectiveProjection.Read(em, session);", "grid"),
        ("            if (kind == AriaGroundApproachKind.Ordinary)\n            {\n                using var sourceRoots", "selected_query"),
        ("            using var fighters = new NativeList<Entity>(Allocator.Temp);", "witness"),
        ("            var members = new List<SkirmishPublicApproachMember>(fighters.Length);", "fighters"),
        ("            var result = AriaGroundApproachPolicy.ObserveSelectedOwnForce(", "selection"),
        ("            using var surfaces = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());", "members"),
        ("            long goalBegan = Stopwatch.GetTimestamp();", "surface"),
        ("            result.GoalEvaluationTicks = Stopwatch.GetTimestamp() - goalBegan;", "goal"),
        ("            var formation = new int2[fighters.Length];", "capability"),
    ]
    for anchor, name in anchors:
        if body.count(anchor) != 1:
            raise ValueError("Exact profiling anchor required: " + name)
        insertion = "            long sourceProfileTick = Stopwatch.GetTimestamp();\n" if name == "begin" else '            sourceProfileTick = SourceApproachProfile.Mark("' + name + '", sourceProfileTick);\n'
        body = body.replace(anchor, insertion + anchor, 1)
    helper = '''\nnamespace Game.Runtime { internal static class SourceApproachProfile {
 static readonly System.Collections.Generic.Dictionary<string,(long ticks,long count)> totals = new System.Collections.Generic.Dictionary<string,(long,long)>();
 static SourceApproachProfile() { System.AppDomain.CurrentDomain.ProcessExit += (s,e) => { foreach(var p in totals) System.Console.Error.WriteLine("[SourceApproachProfile] stage="+p.Key+" ticks="+p.Value.ticks+" count="+p.Value.count+" frequency="+System.Diagnostics.Stopwatch.Frequency); }; }
 internal static long Mark(string stage,long start) { long now=System.Diagnostics.Stopwatch.GetTimestamp(); totals.TryGetValue(stage,out var v); totals[stage]=(v.ticks+now-start,v.count+1); return now; }
} }\n'''
    generated = prefix + marker + body + helper
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    data = generated.encode("utf-8")
    (output / "S003ApproachProfile.cs").write_bytes(data)
    return {"source_pins": {str(repo / SOURCE): hashlib.sha256(raw).hexdigest()},
            "outputs": {"S003ApproachProfile.cs": hashlib.sha256(data).hexdigest()},
            "experimental_only": True, "scope": "Source-only inclusive stage timing; no native or readiness claim"}
