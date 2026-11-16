"""Generate an opt-in source-host child index without changing gameplay sources."""
import hashlib
from pathlib import Path

SOURCE = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-614-SourceMissionTransformBridge.s003.cs'
OUTPUT = 'SourceMissionTransformBridgeIndexed.cs'
PREFIX = ' internal static Dictionary<Entity,List<Entity>> BuildChildren('
NEXT = ' internal static void RequireTransportFactionBinding('

CANDIDATE = ''' internal static Dictionary<Entity,List<Entity>> BuildChildren(IEnumerable<Entity> orderedNodes,Dictionary<Entity,Entity> parents,Dictionary<Entity,Entity[]> existing){var byParent=new Dictionary<Entity,List<Entity>>();foreach(var relation in parents){if(!byParent.TryGetValue(relation.Value,out var children)){children=new List<Entity>();byParent.Add(relation.Value,children);}children.Add(relation.Key);}var result=new Dictionary<Entity,List<Entity>>();foreach(var parent in orderedNodes){var list=new List<Entity>();if(existing.TryGetValue(parent,out var old)){foreach(var child in old){Need(!list.Contains(child),"Duplicate existing Child entry");if(parents.TryGetValue(child,out var current)&&current==parent)list.Add(child);}}if(byParent.TryGetValue(parent,out var linked))foreach(var child in linked)if(!list.Contains(child))list.Add(child);result.Add(parent,list);}return result;}
'''


def transform(raw):
    text = raw.decode('utf-8')
    if text.count(PREFIX) != 1 or text.count(NEXT) != 1:
        raise ValueError('Exact original source transform boundary required')
    start, end = text.index(PREFIX), text.index(NEXT)
    original = text[start:end]
    if 'foreach(var relation in parents)if(relation.Value==parent&&!list.Contains(relation.Key))' not in original:
        raise ValueError('Exact original ordered child scan required')
    return original, text[:start] + CANDIDATE + text[end:]


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    source = repo / SOURCE
    raw = source.read_bytes()
    _, candidate = transform(raw)
    if source.read_bytes() != raw:
        raise ValueError('Source changed during generation')
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    data = candidate.encode('utf-8')
    (output / OUTPUT).write_bytes(data)
    return {'source_pins': {str(source): hashlib.sha256(raw).hexdigest()},
            'outputs': {OUTPUT: hashlib.sha256(data).hexdigest()}, 'experimental_only': True,
            'scope': 'Opt-in source-host transform candidate; no native or mission readiness claim'}
