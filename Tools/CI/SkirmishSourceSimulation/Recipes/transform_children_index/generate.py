#!/usr/bin/env python3
"""Opt-in source-only index for the transform bridge's ordered child projection."""
import hashlib
from pathlib import Path

SOURCE = "Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-614-SourceMissionTransformBridge.s003.cs"
OUTPUT = "SourceExperimentalIndexedTransformBridge.cs"
SIGNATURE = "internal static Dictionary<Entity,List<Entity>> BuildChildren(IEnumerable<Entity> orderedNodes,Dictionary<Entity,Entity> parents,Dictionary<Entity,Entity[]> existing){"
ADDITION = '''
 static readonly bool SourceChildrenIndexEnabled=ReadSourceChildrenIndex();
 static bool ReadSourceChildrenIndex(){bool enabled=Environment.GetEnvironmentVariable("WARLINE_SOURCE_TRANSFORM_CHILDREN_INDEX")=="1";if(enabled)Console.WriteLine("[SourceTransformChildrenIndex] enabled=1 scope=ExperimentalSourceOnly");return enabled;}
 static Dictionary<Entity,List<Entity>> SourceIndexedChildren(IEnumerable<Entity> orderedNodes,Dictionary<Entity,Entity> parents,Dictionary<Entity,Entity[]> existing){
  var byParent=new Dictionary<Entity,List<Entity>>();foreach(var relation in parents){if(!byParent.TryGetValue(relation.Value,out var children))byParent.Add(relation.Value,children=new List<Entity>());children.Add(relation.Key);}
  var result=new Dictionary<Entity,List<Entity>>();foreach(var parent in orderedNodes){var list=new List<Entity>();var included=new HashSet<Entity>();if(existing.TryGetValue(parent,out var old)){foreach(var child in old){Need(!included.Contains(child),"Duplicate existing Child entry");if(parents.TryGetValue(child,out var current)&&current==parent){list.Add(child);included.Add(child);}}}if(byParent.TryGetValue(parent,out var candidates))foreach(var child in candidates)if(included.Add(child))list.Add(child);result.Add(parent,list);}return result;
 }
 '''


def transform(source):
    if source.count(SIGNATURE) != 1:
        raise ValueError("Exact existing child projection required")
    return source.replace(SIGNATURE, ADDITION + SIGNATURE +
                          "if(SourceChildrenIndexEnabled)return SourceIndexedChildren(orderedNodes,parents,existing);", 1)


def generate(repo, output):
    root = Path(repo).resolve(strict=True); path = root / SOURCE
    original = path.read_bytes(); rendered = transform(original.decode("utf-8")).encode("utf-8")
    if path.read_bytes() != original:
        raise ValueError("Transform bridge changed during generation")
    target = Path(output); target.mkdir(parents=True, exist_ok=False)
    (target / OUTPUT).write_bytes(rendered)
    return {"source_pins": {str(path): hashlib.sha256(original).hexdigest()},
            "outputs": {OUTPUT: hashlib.sha256(rendered).hexdigest()},
            "experimental_only": True,
            "scope": "Source-only indexed ordered child projection; original default path retained"}
