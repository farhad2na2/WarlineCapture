"""Generate a source-only per-call original-versus-indexed child-scan control."""
import hashlib
from pathlib import Path

HELPER = Path(__file__).with_name('generate.py')
HELPER_RAW = HELPER.read_bytes()
base = {'__file__': str(HELPER), '__name__': 'indexed_transform_generator'}
exec(compile(HELPER_RAW, str(HELPER), 'exec'), base)

OUTPUT = 'SourceMissionTransformBridgeVerified.cs'


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    source = repo / base['SOURCE']
    raw = source.read_bytes()
    original, candidate = base['transform'](raw)
    baseline = original.replace('BuildChildren(', 'BuildChildrenOriginal(', 1)
    indexed = base['CANDIDATE'].replace('BuildChildren(', 'BuildChildrenIndexed(', 1)
    wrapper = ''' static long SourceChildParityCount,SourceChildOriginalTicks,SourceChildIndexedTicks;
 static SourceMissionTransformBridge(){AppDomain.CurrentDomain.ProcessExit+=(sender,args)=>Console.Error.WriteLine("[SourceChildParity] compared="+SourceChildParityCount+" originalTicks="+SourceChildOriginalTicks+" indexedTicks="+SourceChildIndexedTicks+" frequency="+System.Diagnostics.Stopwatch.Frequency+" scope=ActualSourceHostOnly");}
 internal static Dictionary<Entity,List<Entity>> BuildChildren(IEnumerable<Entity> orderedNodes,Dictionary<Entity,Entity> parents,Dictionary<Entity,Entity[]> existing){
  Dictionary<Entity,List<Entity>> expected,actual;long started;
  if((SourceChildParityCount&1)==0){started=System.Diagnostics.Stopwatch.GetTimestamp();expected=BuildChildrenOriginal(orderedNodes,parents,existing);SourceChildOriginalTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;started=System.Diagnostics.Stopwatch.GetTimestamp();actual=BuildChildrenIndexed(orderedNodes,parents,existing);SourceChildIndexedTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;}
  else{started=System.Diagnostics.Stopwatch.GetTimestamp();actual=BuildChildrenIndexed(orderedNodes,parents,existing);SourceChildIndexedTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;started=System.Diagnostics.Stopwatch.GetTimestamp();expected=BuildChildrenOriginal(orderedNodes,parents,existing);SourceChildOriginalTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-started;}
  Need(expected.Count==actual.Count,"Indexed child count differs from original");
  using(var a=expected.GetEnumerator())using(var b=actual.GetEnumerator())while(a.MoveNext()){Need(b.MoveNext()&&a.Current.Key==b.Current.Key&&a.Current.Value.SequenceEqual(b.Current.Value),"Indexed child order differs from original");}SourceChildParityCount++;return actual;}
'''
    if candidate.count(base['CANDIDATE']) != 1:
        raise ValueError('Exact candidate method required')
    candidate = candidate.replace(base['CANDIDATE'], baseline + indexed + wrapper, 1)
    if source.read_bytes() != raw or HELPER.read_bytes() != HELPER_RAW:
        raise ValueError('Source changed during generation')
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    data = candidate.encode('utf-8')
    (output / OUTPUT).write_bytes(data)
    return {'source_pins': {str(source): hashlib.sha256(raw).hexdigest(),
                            str(HELPER.resolve()): hashlib.sha256(HELPER_RAW).hexdigest()},
            'outputs': {OUTPUT: hashlib.sha256(data).hexdigest()}, 'experimental_only': True,
            'scope': 'Same-host per-call original-versus-indexed control; no native or readiness claim'}
