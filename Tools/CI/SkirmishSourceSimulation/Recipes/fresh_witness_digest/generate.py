"""Generate an opt-in source-host fresh-snapshot digest; never caches grid state."""
import hashlib
import re
from pathlib import Path

WITNESS = 'Assets/Game/Scripts/Components/GridWalkableSourceWitness.cs'
GRID = 'Assets/Game/Scripts/Components/GridComponents.cs'
SHIM = 'Tools/CI/SkirmishSourceSimulation/Adapters/S026/058-059_AutonomousEcs.cs'
OUTPUT = 'SourceExperimentalFreshWitnessDigest.cs'
SIGNATURE = 'public static FixedString128Bytes Digest(DynamicBuffer<GridWalkable> cells){'
INSERTION = '''if(SourceFreshWitnessEnabled){
 var snapshot=cells.Copy();var snapshotBytes=System.Runtime.InteropServices.MemoryMarshal.AsBytes(snapshot.AsSpan());
 if(System.Runtime.InteropServices.Marshal.SizeOf<GridWalkable>()!=1||snapshotBytes.Length!=snapshot.Length)throw new InvalidOperationException("Exact one-byte grid layout required");
 using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(snapshotBytes);
 return new FixedString128Bytes(BitConverter.ToString(hash.GetHashAndReset()).Replace("-","").ToLowerInvariant());}
'''
FIELD = '''
 static readonly bool SourceFreshWitnessEnabled=ReadSourceFreshWitnessEnabled();
 static bool ReadSourceFreshWitnessEnabled(){bool enabled=Environment.GetEnvironmentVariable("WARLINE_SOURCE_FRESH_WITNESS_DIGEST")=="1";if(enabled)Console.WriteLine("[SourceFreshWitnessDigest] enabled=1 scope=ExperimentalSourceOnly");return enabled;}
'''


def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    inputs = {name: (repo / name).read_bytes() for name in (WITNESS, GRID, SHIM)}
    witness, grid, shim = (inputs[name].decode('utf-8') for name in (WITNESS, GRID, SHIM))
    match = re.search(r'public struct GridWalkable\s*:\s*IBufferElementData\s*\{([^{}]*)\}', grid)
    if not match or re.sub(r'//[^\n]*|\s+', '', match.group(1)) != 'publicbyteValue;':
        raise ValueError('Exact one-byte authored grid shape required')
    if shim.count('internal T[] Copy()=>rows.ToArray();') != 1 or witness.count(SIGNATURE) != 1:
        raise ValueError('Exact original witness and source buffer seams required')
    declaration = 'public struct GridWalkableSourceWitness:IComponentData {'
    if witness.count(declaration) != 1:
        raise ValueError('Exact witness declaration required')
    result = witness.replace(SIGNATURE, SIGNATURE + INSERTION, 1).replace(declaration, declaration + FIELD, 1).encode()
    for name, raw in inputs.items():
        if (repo / name).read_bytes() != raw:
            raise ValueError('Witness experiment input changed: ' + name)
    out = Path(output)
    out.mkdir(parents=True, exist_ok=False)
    (out / OUTPUT).write_bytes(result)
    return {'source_pins': {str(repo / name): hashlib.sha256(raw).hexdigest() for name, raw in inputs.items()},
            'outputs': {OUTPUT: hashlib.sha256(result).hexdigest()}, 'experimental_only': True,
            'scope': 'Full fresh source-host buffer digest on every call; default original branch retained'}
