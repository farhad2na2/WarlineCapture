"""Generate an experimental source-host witness timer around the original digest."""
import hashlib
from pathlib import Path

WITNESS = 'Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs'
OUTPUT = 'SourceExperimentalWitnessTiming.cs'
SIGNATURE = 'public static FixedString128Bytes Digest(DynamicBuffer<GridWalkable> cells){'
WRAPPER = '''static long sourceDigestCalls,sourceDigestTicks;
 public static FixedString128Bytes Digest(DynamicBuffer<GridWalkable> cells){
  long start=System.Diagnostics.Stopwatch.GetTimestamp();
  try{return SourceOriginalDigest(cells);}
  finally{sourceDigestTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;sourceDigestCalls++;
   if(sourceDigestCalls%100==0)Console.WriteLine("[SourceWitnessTiming] calls="+sourceDigestCalls+" ticks="+sourceDigestTicks+" frequency="+System.Diagnostics.Stopwatch.Frequency+" scope=ExperimentalSourceOnly");}}
 static FixedString128Bytes SourceOriginalDigest(DynamicBuffer<GridWalkable> cells){'''


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    path = repo / WITNESS
    raw = path.read_bytes()
    original = raw.decode('utf-8')
    if original.count(SIGNATURE) != 1:
        raise ValueError('Exact original witness digest required')
    generated = original.replace(SIGNATURE, WRAPPER, 1).encode()
    if path.read_bytes() != raw:
        raise ValueError('Witness source changed during generation')
    out = Path(output)
    out.mkdir(parents=True, exist_ok=False)
    (out / OUTPUT).write_bytes(generated)
    return {'source_pins': {str(path): hashlib.sha256(raw).hexdigest()},
            'outputs': {OUTPUT: hashlib.sha256(generated).hexdigest()},
            'experimental_only': True,
            'scope': 'Timing wrapper around original digest, source-host only; no gameplay or readiness claim'}
