"""Generate the opt-in pooled digest and an equal timing wrapper for both paths."""
import hashlib
from pathlib import Path

WITNESS = 'Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs'
GRID = 'Assets/Game/Scripts/Components/GridComponents.cs'
SHIM = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs'
OUTPUT = 'SourceExperimentalPooledWitness.cs'
SIGNATURE = 'public static FixedString128Bytes Digest(DynamicBuffer<GridWalkable> cells){'
WRAPPER = '''static readonly bool SourcePooledEnabled=SourcePooledEnabledValue();
 static long SourceDigestCalls,SourceDigestTicks;
 static bool SourcePooledEnabledValue(){bool on=Environment.GetEnvironmentVariable("WARLINE_SOURCE_POOLED_WITNESS_DIGEST")=="1";Console.WriteLine("[SourcePooledWitness] enabled="+(on?"1":"0")+" scope=ExperimentalSourceOnly");return on;}
 public static FixedString128Bytes Digest(DynamicBuffer<GridWalkable> cells){
  long start=System.Diagnostics.Stopwatch.GetTimestamp();try{return SourcePooledEnabled?SourcePooledDigest(cells):SourceOriginalDigest(cells);}
  finally{SourceDigestTicks+=System.Diagnostics.Stopwatch.GetTimestamp()-start;SourceDigestCalls++;
   if(SourceDigestCalls%100==0)Console.WriteLine("[SourcePooledWitnessTiming] calls="+SourceDigestCalls+" ticks="+SourceDigestTicks+" frequency="+System.Diagnostics.Stopwatch.Frequency+" scope=ExperimentalSourceOnly");}}
 static FixedString128Bytes SourcePooledDigest(DynamicBuffer<GridWalkable> cells){
  int length=cells.Length;var snapshot=System.Buffers.ArrayPool<GridWalkable>.Shared.Rent(Math.Max(1,length));
  try{cells.SourceCopyTo(snapshot,length);var snapshotBytes=System.Runtime.InteropServices.MemoryMarshal.AsBytes(snapshot.AsSpan(0,length));
   if(System.Runtime.InteropServices.Marshal.SizeOf<GridWalkable>()!=1||snapshotBytes.Length!=length)throw new InvalidOperationException("Exact one-byte grid layout required");
   using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(snapshotBytes);
   return new FixedString128Bytes(BitConverter.ToString(hash.GetHashAndReset()).Replace("-","").ToLowerInvariant());}
  finally{System.Buffers.ArrayPool<GridWalkable>.Shared.Return(snapshot);}}
 static FixedString128Bytes SourceOriginalDigest(DynamicBuffer<GridWalkable> cells){'''


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    paths = [repo / p for p in (WITNESS, GRID, SHIM)]
    data = [p.read_bytes() for p in paths]
    original, grid, shim = [raw.decode() for raw in data]
    import re
    match = re.search(r'public struct GridWalkable\s*:\s*IBufferElementData\s*\{([^{}]*)\}', grid)
    if not match or re.sub(r'//[^\n]*|\s+', '', match.group(1)) != 'publicbyteValue;':
        raise ValueError('Exact one-byte grid layout required')
    if original.count(SIGNATURE) != 1 or shim.count('internal T[] Copy()=>rows.ToArray();') != 1:
        raise ValueError('Exact witness and buffer sources required')
    generated = original.replace(SIGNATURE, WRAPPER, 1).encode()
    if any(p.read_bytes() != raw for p, raw in zip(paths, data)):
        raise ValueError('Input changed during generation')
    out = Path(output); out.mkdir(parents=True, exist_ok=False)
    (out / OUTPUT).write_bytes(generated)
    return {'source_pins': {str(p): hashlib.sha256(raw).hexdigest() for p, raw in zip(paths, data)},
            'outputs': {OUTPUT: hashlib.sha256(generated).hexdigest()}, 'experimental_only': True,
            'scope': 'Opt-in fresh pooled digest and symmetric source-only timing; no readiness'}
