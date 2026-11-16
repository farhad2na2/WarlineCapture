"""Generate an experimental source-only mutation-tracked witness cache."""
import hashlib
from pathlib import Path

ECS = "Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs"
OWNER = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishPublicSelectedApproachProjection.cs"

def once(text, old, new):
    if text.count(old) != 1:
        raise ValueError("Expected one exact source anchor: " + old[:100])
    return text.replace(old, new, 1)

def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    originals = {p: (repo / p).read_bytes() for p in (ECS, OWNER)}
    ecs = originals[ECS].decode().replace("\r\n", "\n")
    start = ecs.index(" public sealed class DynamicBuffer<T>")
    end = ecs.index("\n", start)
    buffer = ecs[start:end]
    buffer = once(buffer, "readonly List<T> rows;", "readonly List<T> rows; ulong sourceRevision; public ulong SourceRevision=>sourceRevision; void SourceTouch(){if(typeof(T)==typeof(Game.Components.GridWalkable))sourceRevision=checked(sourceRevision+1);}")
    buffer = once(buffer, "set=>rows[i]=value;", "set{SourceTouch();rows[i]=value;}")
    for old, new in [("rows.Add(x);}", "SourceTouch();rows.Add(x);}"),
                     ("rows.Insert(index,value);}", "SourceTouch();rows.Insert(index,value);}"),
                     ("public void RemoveAt(int i)=>rows.RemoveAt(i);", "public void RemoveAt(int i){SourceTouch();rows.RemoveAt(i);}"),
                     ("rows[i]=rows[rows.Count-1];rows.RemoveAt(rows.Count-1);", "SourceTouch();rows[i]=rows[rows.Count-1];rows.RemoveAt(rows.Count-1);"),
                     ("public void Clear()=>rows.Clear();", "public void Clear(){SourceTouch();rows.Clear();}"),
                     ("if(rows.Count>length)rows.RemoveRange", "SourceTouch();if(rows.Count>length)rows.RemoveRange"),
                     ("AsNativeArray()=>Unity.Collections.NativeArray<T>.BorrowList(rows);", "AsNativeArray()=>typeof(T)==typeof(Game.Components.GridWalkable)?new Unity.Collections.NativeArray<T>(rows.Count,i=>rows[i],(i,value)=>{SourceTouch();rows[i]=value;}):Unity.Collections.NativeArray<T>.BorrowList(rows);")]:
        buffer = once(buffer, old, new)
    ecs = ecs[:start] + buffer + ecs[end:]
    owner = originals[OWNER].decode().replace("\r\n", "\n")
    owner = once(owner, "!witness.Matches(source, walkable)", "!SourceWalkabilityWitnessCache.Matches(witness, source, walkable)")
    owner += '''\nnamespace Game.Runtime { internal static class SourceWalkabilityWitnessCache {
 sealed class State { internal ulong Revision; internal bool Valid; internal Unity.Collections.FixedString128Bytes Digest; }
 static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Unity.Entities.DynamicBuffer<Game.Components.GridWalkable>,State> States = new System.Runtime.CompilerServices.ConditionalWeakTable<Unity.Entities.DynamicBuffer<Game.Components.GridWalkable>,State>();
 internal static bool Matches(Game.Components.GridWalkableSourceWitness witness, Game.Components.OperationMapGridSourceRecord source, Unity.Entities.DynamicBuffer<Game.Components.GridWalkable> cells) {
  if(!witness.AssetGuid.Equals(source.GridGuid)||!witness.AssetHash.Equals(source.GridHash)||witness.Generation!=source.Generation||witness.WorldSequence==0||witness.WorldSequence!=source.WorldSequence)return false;
  var state=States.GetOrCreateValue(cells); lock(state) { ulong revision=cells.SourceRevision; if(!state.Valid||state.Revision!=revision) { var digest=Game.Components.GridWalkableSourceWitness.Digest(cells); if(cells.SourceRevision!=revision)throw new System.InvalidOperationException("Walkability changed while hashing"); state.Digest=digest; state.Revision=revision; state.Valid=true; } return witness.WalkableHash.Equals(state.Digest); }
 }
} }\n'''
    output = Path(output); output.mkdir(parents=True, exist_ok=False)
    generated = {"TrackedS026Ecs.cs": ecs.encode(), "CachedS003Approach.cs": owner.encode()}
    for name, data in generated.items(): (output / name).write_bytes(data)
    return {"source_pins": {str(repo / p): hashlib.sha256(data).hexdigest() for p, data in originals.items()},
            "outputs": {p: hashlib.sha256(data).hexdigest() for p, data in generated.items()},
            "experimental_only": True, "scope": "Mutation-tracked source-host witness experiment; no native or readiness claim"}
